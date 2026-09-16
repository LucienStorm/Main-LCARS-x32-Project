Imports System.Collections.Generic
Imports System.Net
Imports Microsoft.Web.WebView2.Core
Imports Microsoft.Web.WebView2.WinForms
Imports System.IO
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Windows.Forms

''' <summary>
''' IBrowserTab host for Markdown editing with an optional WebView2 HTML preview.
''' </summary>
Public Class MarkdownEditTab
    Implements IBrowserTab

    Private Shared runtimeWarningShown As Boolean = False

    Private ReadOnly _hostPanel As Panel
    Private ReadOnly _toolStrip As ToolStrip
    Private ReadOnly _splitContainer As SplitContainer
    Private ReadOnly _textBox As TextBox
    Private ReadOnly _webView As WebView2
    Private ReadOnly _previewButton As ToolStripButton

    Private _filePath As String = ""
    Private _dirty As Boolean = False
    Private _suppressDirty As Boolean = False
    Private _previewReady As Boolean = False

    Public Event ContentChanged As EventHandler

    Public Sub New()
        _hostPanel = New Panel()
        _hostPanel.Dock = DockStyle.Fill

        _toolStrip = New ToolStrip()
        _toolStrip.Dock = DockStyle.Top
        _toolStrip.GripStyle = ToolStripGripStyle.Hidden

        _previewButton = New ToolStripButton("PREVIEW")
        _previewButton.CheckOnClick = True
        _toolStrip.Items.Add(_previewButton)

        _splitContainer = New SplitContainer()
        _splitContainer.Dock = DockStyle.Fill
        _splitContainer.Orientation = Orientation.Vertical
        _splitContainer.Panel2Collapsed = True
        _splitContainer.SplitterDistance = 400

        _textBox = New TextBox()
        _textBox.Multiline = True
        _textBox.Dock = DockStyle.Fill
        _textBox.ScrollBars = ScrollBars.Both
        _textBox.WordWrap = True
        _textBox.Font = New Drawing.Font("Consolas", 10.0F)
        _textBox.AcceptsReturn = True
        _textBox.AcceptsTab = True
        _textBox.HideSelection = False
        _textBox.BackColor = Drawing.Color.Black
        _textBox.ForeColor = Drawing.Color.FromArgb(255, 153, 0)
        _hostPanel.BackColor = Drawing.Color.Black
        _toolStrip.BackColor = Drawing.Color.Black
        _toolStrip.ForeColor = Drawing.Color.FromArgb(255, 153, 0)
        _splitContainer.BackColor = Drawing.Color.Black
        _splitContainer.Panel1.BackColor = Drawing.Color.Black
        _splitContainer.Panel2.BackColor = Drawing.Color.Black

        _webView = New WebView2()
        _webView.Name = "MarkdownPreviewWebView2"
        _webView.Dock = DockStyle.Fill

        _splitContainer.Panel1.Controls.Add(_textBox)
        _splitContainer.Panel2.Controls.Add(_webView)

        _hostPanel.Controls.Add(_splitContainer)
        _hostPanel.Controls.Add(_toolStrip)

        AddHandler _textBox.TextChanged, AddressOf OnEditorTextChanged
        AddHandler _previewButton.CheckedChanged, AddressOf OnPreviewToggled
    End Sub

    Public ReadOnly Property TabKind As String Implements IBrowserTab.TabKind
        Get
            Return "Markdown"
        End Get
    End Property

    Public ReadOnly Property Title As String Implements IBrowserTab.Title
        Get
            Dim name As String = If(String.IsNullOrEmpty(_filePath), "NEW MARKDOWN", System.IO.Path.GetFileName(_filePath))
            If _dirty Then Return name & "*"
            Return name
        End Get
    End Property

    Public ReadOnly Property IsDirty As Boolean Implements IBrowserTab.IsDirty
        Get
            Return _dirty
        End Get
    End Property

    Public ReadOnly Property ContentControl As Control Implements IBrowserTab.ContentControl
        Get
            Return _hostPanel
        End Get
    End Property

    Public Function GetPathOrUrl() As String Implements IBrowserTab.GetPathOrUrl
        Return _filePath
    End Function

    Public Sub NavigateOrOpen(ByVal pathOrUrl As String) Implements IBrowserTab.NavigateOrOpen
        Dim path As String = ResolveLocalPath(pathOrUrl)
        If String.IsNullOrEmpty(path) Then Return

        If File.Exists(path) Then
            LoadFromFile(path)
        Else
            _filePath = path
            SetEditorText("")
        End If
    End Sub

    Public Function Save(ByVal path As String) As Boolean Implements IBrowserTab.Save
        If String.IsNullOrWhiteSpace(path) Then Return False

        Try
            Dim parentDir As String = System.IO.Path.GetDirectoryName(path)
            If Not String.IsNullOrEmpty(parentDir) AndAlso Not Directory.Exists(parentDir) Then
                Directory.CreateDirectory(parentDir)
            End If

            File.WriteAllText(path, _textBox.Text, New UTF8Encoding(False))
            _filePath = path
            _dirty = False
            RaiseEvent ContentChanged(Me, EventArgs.Empty)
            Return True
        Catch
            Return False
        End Try
    End Function

    Public Sub FocusContent() Implements IBrowserTab.FocusContent
        _textBox.Focus()
    End Sub

    ''' <summary>
    ''' Clears the editor for a new untitled Markdown document.
    ''' </summary>
    Public Sub NewDocument()
        _filePath = ""
        SetEditorText("")
    End Sub

    ''' <summary>
    ''' Creates the WebView2 environment used for HTML preview.
    ''' </summary>
    Public Async Function InitializeAsync() As Task
        If Not WebView2RuntimeInstaller.EnsureInstalled() Then
            If Not runtimeWarningShown Then
                runtimeWarningShown = True
                MessageBox.Show(
                    "WebView2 Runtime is not available. LCARS tried to install it from Microsoft automatically.",
                    "LCARS Web Browser", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            End If
            Return
        End If

        Try
            Dim env As CoreWebView2Environment
            If WebView2RuntimeInstaller.EvergreenRuntimeAvailable() Then
                env = Await CoreWebView2Environment.CreateAsync()
            Else
                env = Await CoreWebView2Environment.CreateAsync(WebView2RuntimeInstaller.GetBundledRuntimeFolder())
            End If
            Await _webView.EnsureCoreWebView2Async(env)
            _previewReady = True
            If _previewButton.Checked Then
                UpdatePreview()
            End If
        Catch ex As Exception
            If Not runtimeWarningShown Then
                runtimeWarningShown = True
                MessageBox.Show("Unable to start WebView2 preview." & vbCrLf & ex.Message, "LCARS Web Browser", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If
        End Try
    End Function

    Private Sub LoadFromFile(ByVal path As String)
        Try
            Dim text As String = File.ReadAllText(path, New UTF8Encoding(False))
            _filePath = path
            SetEditorText(text)
        Catch ex As Exception
            MessageBox.Show("Unable to open file:" & vbCrLf & ex.Message, "LCARS Web Browser", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub SetEditorText(ByVal text As String)
        _suppressDirty = True
        _textBox.Text = text
        _dirty = False
        _suppressDirty = False
        UpdatePreview()
        RaiseEvent ContentChanged(Me, EventArgs.Empty)
    End Sub

    Private Sub OnEditorTextChanged(ByVal sender As Object, ByVal e As EventArgs)
        If _suppressDirty Then Return
        If Not _dirty Then
            _dirty = True
            RaiseEvent ContentChanged(Me, EventArgs.Empty)
        End If
        UpdatePreview()
    End Sub

    Private Sub OnPreviewToggled(ByVal sender As Object, ByVal e As EventArgs)
        _splitContainer.Panel2Collapsed = Not _previewButton.Checked
        If _previewButton.Checked Then
            UpdatePreview()
        End If
    End Sub

    Private Sub UpdatePreview()
        If Not _previewButton.Checked OrElse Not _previewReady Then Return
        If _webView.CoreWebView2 Is Nothing Then Return

        Dim body As String = MarkdownToHtml.Convert(_textBox.Text)
        Dim html As String = "<!DOCTYPE html><html><head><meta charset=""utf-8""/>" &
            "<style>body{font-family:Segoe UI,sans-serif;margin:1em;line-height:1.5;}" &
            "pre{background:#f4f4f4;padding:0.75em;overflow:auto;}" &
            "code{font-family:Consolas,monospace;}" &
            "a{color:#0066cc;}</style></head><body>" & body & "</body></html>"
        _webView.CoreWebView2.NavigateToString(html)
    End Sub

    Private Shared Function ResolveLocalPath(ByVal pathOrUrl As String) As String
        Dim text As String = If(pathOrUrl, "").Trim()
        If text = "" Then Return ""

        If text.StartsWith("file://", StringComparison.OrdinalIgnoreCase) Then
            Try
                Return New Uri(text).LocalPath
            Catch
                Return text
            End Try
        End If

        Return text
    End Function

    ''' <summary>
    ''' Releases hosted controls.
    ''' </summary>
    Public Sub Dispose()
        If _webView IsNot Nothing Then
            _webView.Dispose()
        End If
        If _hostPanel IsNot Nothing Then
            _hostPanel.Dispose()
        End If
    End Sub

    ''' <summary>
    ''' Lightweight Markdown to HTML converter (headers, emphasis, lists, links, code fences).
    ''' </summary>
    Private NotInheritable Class MarkdownToHtml
        Private Sub New()
        End Sub

        Public Shared Function Convert(ByVal markdown As String) As String
            If String.IsNullOrEmpty(markdown) Then Return ""

            Dim parts As String() = markdown.Split(New String() {"```"}, StringSplitOptions.None)
            Dim output As New StringBuilder()

            For i As Integer = 0 To parts.Length - 1
                Dim segment As String = parts(i)
                If i Mod 2 = 1 Then
                    Dim code As String = segment
                    Dim newline As Integer = code.IndexOf(ControlChars.Lf)
                    If newline < 0 Then newline = code.IndexOf(ControlChars.Cr)
                    If newline >= 0 Then
                        code = code.Substring(newline + 1)
                    End If
                    output.Append("<pre><code>")
                    output.Append(HtmlEncode(code.TrimEnd(ControlChars.Cr, ControlChars.Lf)))
                    output.Append("</code></pre>")
                Else
                    output.Append(ConvertBlock(segment))
                End If
            Next

            Return output.ToString()
        End Function

        Private Shared Function ConvertBlock(ByVal text As String) As String
            Dim lines As String() = text.Replace(vbCrLf, vbLf).Split(ControlChars.Lf)
            Dim output As New StringBuilder()
            Dim paragraph As New List(Of String)()
            Dim listType As String = Nothing

            Dim flushParagraph As Action = Sub()
                                             If paragraph.Count = 0 Then Return
                                             output.Append("<p>")
                                             output.Append(ConvertInline(String.Join(" ", paragraph)))
                                             output.Append("</p>")
                                             paragraph.Clear()
                                         End Sub

            Dim closeList As Action = Sub()
                                          If listType Is Nothing Then Return
                                          output.Append("</" & listType & ">")
                                          listType = Nothing
                                      End Sub

            For Each rawLine As String In lines
                Dim line As String = rawLine
                If line Is Nothing Then line = ""

                If String.IsNullOrWhiteSpace(line) Then
                    flushParagraph()
                    closeList()
                    Continue For
                End If

                Dim headerMatch As Match = Regex.Match(line, "^(#{1,6})\s+(.+)$")
                If headerMatch.Success Then
                    flushParagraph()
                    closeList()
                    Dim level As Integer = headerMatch.Groups(1).Value.Length
                    output.Append("<h" & level.ToString() & ">")
                    output.Append(ConvertInline(headerMatch.Groups(2).Value))
                    output.Append("</h" & level.ToString() & ">")
                    Continue For
                End If

                Dim ulMatch As Match = Regex.Match(line, "^[\-\*]\s+(.+)$")
                If ulMatch.Success Then
                    flushParagraph()
                    If listType <> "ul" Then
                        closeList()
                        listType = "ul"
                        output.Append("<ul>")
                    End If
                    output.Append("<li>")
                    output.Append(ConvertInline(ulMatch.Groups(1).Value))
                    output.Append("</li>")
                    Continue For
                End If

                Dim olMatch As Match = Regex.Match(line, "^\d+\.\s+(.+)$")
                If olMatch.Success Then
                    flushParagraph()
                    If listType <> "ol" Then
                        closeList()
                        listType = "ol"
                        output.Append("<ol>")
                    End If
                    output.Append("<li>")
                    output.Append(ConvertInline(olMatch.Groups(1).Value))
                    output.Append("</li>")
                    Continue For
                End If

                closeList()
                paragraph.Add(line.Trim())
            Next

            flushParagraph()
            closeList()
            Return output.ToString()
        End Function

        Private Shared Function ConvertInline(ByVal text As String) As String
            If String.IsNullOrEmpty(text) Then Return ""

            Dim encoded As String = HtmlEncode(text)
            encoded = Regex.Replace(encoded, "\[([^\]]+)\]\(([^)]+)\)", AddressOf LinkMatchEvaluator)
            encoded = Regex.Replace(encoded, "`([^`]+)`", "<code>$1</code>")
            encoded = Regex.Replace(encoded, "\*\*([^*]+)\*\*", "<strong>$1</strong>")
            encoded = Regex.Replace(encoded, "__([^_]+)__", "<strong>$1</strong>")
            encoded = Regex.Replace(encoded, "\*([^*]+)\*", "<em>$1</em>")
            encoded = Regex.Replace(encoded, "(?<!\w)_([^_]+)_(?!\w)", "<em>$1</em>")
            Return encoded
        End Function

        Private Shared Function LinkMatchEvaluator(ByVal match As Match) As String
            Return BuildSafeLink(match.Groups(1).Value, match.Groups(2).Value)
        End Function

        Private Shared Function BuildSafeLink(ByVal encodedLinkText As String, ByVal encodedUrl As String) As String
            Dim url As String = WebUtility.HtmlDecode(encodedUrl).Trim()
            If Not IsSafeHref(url) Then
                Return encodedLinkText
            End If

            Dim safeHref As String = WebUtility.HtmlEncode(url)
            Return "<a href=""" & safeHref & """>" & encodedLinkText & "</a>"
        End Function

        Private Shared Function IsSafeHref(ByVal url As String) As Boolean
            If String.IsNullOrWhiteSpace(url) Then Return False

            If url.IndexOfAny({ControlChars.Tab, ControlChars.Lf, ControlChars.Cr, ControlChars.NullChar}) >= 0 Then
                Return False
            End If

            Dim colonIndex As Integer = url.IndexOf(":")
            If colonIndex < 0 Then
                If url.StartsWith("//") Then Return False
                Return True
            End If

            Dim scheme As String = url.Substring(0, colonIndex).Trim().ToLowerInvariant()
            If scheme.Length = 0 Then Return False

            Return scheme = "http" Or scheme = "https" Or scheme = "file" Or scheme = "mailto"
        End Function

        Private Shared Function HtmlEncode(ByVal text As String) As String
            If String.IsNullOrEmpty(text) Then Return ""
            Return WebUtility.HtmlEncode(text)
        End Function
    End Class
End Class
