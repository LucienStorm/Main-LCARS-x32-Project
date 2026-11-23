Imports System
Imports System.Drawing
Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports Microsoft.VisualBasic.CompilerServices
Imports Microsoft.Web.WebView2.Core
Imports Microsoft.Web.WebView2.WinForms
    Public Class MailHtmlComposePane
        Inherits UserControl
        Private toolPanel As Panel

        Private webView As WebView2

        Private fallbackBox As TextBox

        Private initStarted As Boolean

        Private useFallback As Boolean

        Public Property PlainText As String
            Get
                If useFallback OrElse webView Is Nothing OrElse webView.CoreWebView2 Is Nothing Then
                    Dim text = fallbackBox.Text
                    Return If(Not Equals(text, Nothing), text, "")
                End If
                Return GetPlainTextSync()
            End Get
            Set(value As String)
                Dim text = If(Not Equals(value, Nothing), value, "")
                If useFallback OrElse webView Is Nothing OrElse webView.CoreWebView2 Is Nothing Then
                    fallbackBox.Text = text
                Else
                    SetDesignModeHtml("<p>" & EscapeHtml(text).Replace(vbCrLf, "<br/>") & "</p>")
                End If
            End Set
        End Property

        Public ReadOnly Property HtmlBody As String
            Get
                If useFallback OrElse webView Is Nothing OrElse webView.CoreWebView2 Is Nothing Then
                    Return "<p>" & EscapeHtml(fallbackBox.Text).Replace(vbCrLf, "<br/>") & "</p>"
                End If
                Return GetInnerHtmlSync()
            End Get
        End Property

        Public ReadOnly Property UsesRichEditor As Boolean
            Get
                Return If(Not useFallback AndAlso webView IsNot Nothing AndAlso webView.CoreWebView2 IsNot Nothing, True, False)
            End Get
        End Property

        Public Sub New()
            MyBase.BackColor = Color.Black
            MyBase.DoubleBuffered = True
            toolPanel = New Panel()
            toolPanel.Dock = DockStyle.Top
            toolPanel.Height = 32
            toolPanel.BackColor = Color.FromArgb(30, 30, 30)
            BuildToolbar()
            Controls.Add(toolPanel)
            fallbackBox = New TextBox()
            fallbackBox.Multiline = True
            fallbackBox.Dock = DockStyle.Fill
            fallbackBox.BackColor = Color.FromArgb(40, 40, 40)
            fallbackBox.ForeColor = Color.Orange
            fallbackBox.BorderStyle = BorderStyle.FixedSingle
            fallbackBox.Font = New Font("Consolas", 9.5F, FontStyle.Regular)
            fallbackBox.AcceptsReturn = True
            fallbackBox.AcceptsTab = True
            fallbackBox.Visible = True
            Controls.Add(fallbackBox)
            AddHandler HandleCreated, AddressOf OnHandleCreatedAsync
        End Sub

        Private Sub BuildToolbar()
            Dim num = 4
            AddToolButton("B", num, New EventHandler(AddressOf OnBoldClick))
            num += 36
            AddToolButton("I", num, New EventHandler(AddressOf OnItalicClick))
            num += 36
            AddToolButton("U", num, New EventHandler(AddressOf OnUnderlineClick))
            num += 36
            AddToolButton("LINK", num, New EventHandler(AddressOf OnLinkClick))
            num += 56
            AddToolButton("IMG", num, New EventHandler(AddressOf OnImageClick))
        End Sub

        Private Sub AddToolButton(text As String, x As Integer, handler As EventHandler)
            Dim button As Button = New Button()
            button.Text = text
            button.FlatStyle = FlatStyle.Flat
            button.ForeColor = Color.Orange
            button.BackColor = Color.FromArgb(50, 50, 50)
            Dim size As Size = New Size(If(text.Length > 2, 52, 32), 24)
            button.Size = size
            Dim location As Point = New Point(x, 4)
            button.Location = location
            AddHandler button.Click, handler
            toolPanel.Controls.Add(button)
        End Sub

        Public Sub SetHtmlContent(html As String)
            Dim text = If(Not Equals(html, Nothing), html, "")
            If useFallback OrElse webView Is Nothing OrElse webView.CoreWebView2 Is Nothing Then
                fallbackBox.Text = StripTagsLight(text)
            Else
                SetDesignModeHtml(text)
            End If
        End Sub

        Private Sub OnBoldClick(sender As Object, e As EventArgs)
            RunEditorCommand("bold")
        End Sub

        Private Sub OnItalicClick(sender As Object, e As EventArgs)
            RunEditorCommand("italic")
        End Sub

        Private Sub OnUnderlineClick(sender As Object, e As EventArgs)
            RunEditorCommand("underline")
        End Sub

        Private Sub OnLinkClick(sender As Object, e As EventArgs)
            Dim text = LcarsCommsUi.Prompt("Link URL:", "LCARS COMMS — INSERT LINK", "https://")
            If Not String.IsNullOrWhiteSpace(text) Then
                RunEditorCommand("createLink", text.Trim())
            End If
        End Sub

        Private Sub OnImageClick(sender As Object, e As EventArgs)
            Dim openFileDialog As OpenFileDialog = New OpenFileDialog()
            openFileDialog.Filter = "Images|*.png;*.jpg;*.jpeg;*.gif;*.bmp|All|*.*"
            If openFileDialog.ShowDialog() <> DialogResult.OK Then
                Return
            End If
            Try
                Dim inArray = File.ReadAllBytes(openFileDialog.FileName)
                Dim text As String = Path.GetExtension(openFileDialog.FileName).Trim("."c).ToLowerInvariant()
                If text.Length = 0 Then
                    text = "png"
                End If
                Dim text2 = Convert.ToBase64String(inArray)
                Dim text3 = "<img src=""data:image/" & text & ";base64," & text2 & """ alt=""inline""/>"
                RunEditorCommand("insertHTML", text3.Replace("'", "\'"))
            Catch ex As Exception
                ProjectData.SetProjectError(ex)
                Dim ex2 = ex
                LcarsCommsUi.Alert(ex2.Message, "LCARS COMMS — IMAGE ERROR")
                Call ProjectData.ClearProjectError()
            End Try
        End Sub

        Private Sub RunEditorCommand(command As String, Optional value As String = Nothing)
            If webView Is Nothing OrElse webView.CoreWebView2 Is Nothing Then
                Return
            End If
            Try
                If Equals(value, Nothing) Then
                    webView.CoreWebView2.ExecuteScriptAsync("document.execCommand('" & command & "', false, null);")
                    Return
                End If
                Dim text = value.Replace("\", "\\").Replace("'", "\'")
                webView.CoreWebView2.ExecuteScriptAsync("document.execCommand('" & command & "', false, '" & text & "');")
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
        End Sub

        Private Async Sub OnHandleCreatedAsync(sender As Object, e As EventArgs)
            Await EnsureWebViewAsync()
        End Sub

        Private Async Function EnsureWebViewAsync() As Task
            If initStarted OrElse Not IsHandleCreated Then
                Return
            End If
            initStarted = True
            Try
                webView = New WebView2()
                CType(CObj(webView), Control).Dock = DockStyle.Fill
                webView.DefaultBackgroundColor = Color.Black
                Controls.Add(CType(CObj(webView), Control))
                Call CType(CObj(webView), Control).BringToFront()
                toolPanel.BringToFront()
                Await webView.EnsureCoreWebView2Async(CType(Nothing, CoreWebView2Environment))
                webView.CoreWebView2.NavigateToString(BuildEditorShell(""))
                fallbackBox.Visible = False
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                useFallback = True
                initStarted = False
                Call ProjectData.ClearProjectError()
            End Try
        End Function

        Private Sub SetDesignModeHtml(innerHtml As String)
            If webView IsNot Nothing AndAlso webView.CoreWebView2 IsNot Nothing Then
                webView.CoreWebView2.NavigateToString(BuildEditorShell(innerHtml))
            End If
        End Sub

        Private Shared Function BuildEditorShell(innerHtml As String) As String
            Return "<!DOCTYPE html><html><head><meta charset=""utf-8""/><style>body{background:#1a1a1a;color:#f90;font-family:Segoe UI,Arial,sans-serif;font-size:14px;margin:8px;}</style></head><body contenteditable=""true"" spellcheck=""true"">" & innerHtml & "</body></html>"
        End Function

        Private Function GetInnerHtmlSync() As String
            Dim result As String
            Try
                Dim task As Task(Of String) = webView.CoreWebView2.ExecuteScriptAsync("document.body.innerHTML")
                task.Wait(3000)
                result = UnquoteJsonString(task.Result)
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                result = fallbackBox.Text
                Call ProjectData.ClearProjectError()
            End Try
            Return result
        End Function

        Private Function GetPlainTextSync() As String
            Dim result As String
            Try
                Dim task As Task(Of String) = webView.CoreWebView2.ExecuteScriptAsync("document.body.innerText")
                task.Wait(3000)
                result = UnquoteJsonString(task.Result)
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                result = fallbackBox.Text
                Call ProjectData.ClearProjectError()
            End Try
            Return result
        End Function

        Private Shared Function UnquoteJsonString(jsonQuoted As String) As String
            If String.IsNullOrEmpty(jsonQuoted) Then
                Return ""
            End If
            Dim text As String = jsonQuoted.Trim()
            If text.StartsWith("""", StringComparison.Ordinal) AndAlso text.EndsWith("""", StringComparison.Ordinal) AndAlso text.Length >= 2 Then
                text = text.Substring(1, text.Length - 2)
                text = text.Replace("\n", vbLf).Replace("\r", "").Replace("\""", """").Replace("\\", "\")
            End If
            Return text
        End Function

        Private Shared Function StripTagsLight(html As String) As String
            If String.IsNullOrEmpty(html) Then
                Return ""
            End If
            Return Regex.Replace(html, "<[^>]+>", " ").Trim()
        End Function

        Private Shared Function EscapeHtml(text As String) As String
            If String.IsNullOrEmpty(text) Then
                Return ""
            End If
            Return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
        End Function
    End Class

