' Lcars Web Browser/Tabs/DocumentEditorTab.vb
Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.IO
Imports System.Net
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Windows.Forms
Imports Microsoft.Web.WebView2.WinForms

''' <summary>
''' Unified Word-class document editor: txt / md / rtf / docx in one tab.
''' </summary>
Public Class DocumentEditorTab
    Implements IBrowserTab

    Private ReadOnly _hostPanel As Panel
    Private ReadOnly _toolStrip As ToolStrip
    Private ReadOnly _editorHost As Panel
    Private ReadOnly _lineMargin As LineNumberMargin
    Private ReadOnly _richText As RichTextBox
    Private ReadOnly _split As SplitContainer
    Private ReadOnly _preview As WebView2

    Private ReadOnly _btnBold As ToolStripButton
    Private ReadOnly _btnItalic As ToolStripButton
    Private ReadOnly _btnUnderline As ToolStripButton
    Private ReadOnly _btnFont As ToolStripButton
    Private ReadOnly _btnAlignLeft As ToolStripButton
    Private ReadOnly _btnAlignCenter As ToolStripButton
    Private ReadOnly _btnAlignRight As ToolStripButton
    Private ReadOnly _btnBullets As ToolStripButton
    Private ReadOnly _btnFind As ToolStripButton
    Private ReadOnly _btnReplace As ToolStripButton
    Private ReadOnly _btnWrap As ToolStripButton
    Private ReadOnly _btnLines As ToolStripButton
    Private ReadOnly _btnAutoCap As ToolStripButton
    Private ReadOnly _btnPreview As ToolStripButton
    Private ReadOnly _btnTemplate As ToolStripDropDownButton

    Private _filePath As String = ""
    Private _format As DocumentFormatKind = DocumentFormatKind.RichText
    Private _dirty As Boolean = False
    Private _suppressDirty As Boolean = False
    Private _autoCap As Boolean = True
    Private _previewReady As Boolean = False
    Private _headerText As String = ""
    Private _footerText As String = ""

    Public Event ContentChanged As EventHandler

    Public Sub New()
        _hostPanel = New Panel() With {.Dock = DockStyle.Fill, .BackColor = Color.Black}

        _toolStrip = New ToolStrip() With {
            .Dock = DockStyle.Top,
            .GripStyle = ToolStripGripStyle.Hidden,
            .BackColor = Color.Black,
            .ForeColor = Color.FromArgb(255, 153, 0)
        }

        _btnBold = MakeToggle("B")
        _btnBold.Font = New Font(_btnBold.Font, FontStyle.Bold)
        _btnItalic = MakeToggle("I")
        _btnItalic.Font = New Font(_btnItalic.Font, FontStyle.Italic)
        _btnUnderline = MakeToggle("U")
        _btnFont = MakeButton("FONT")
        _btnAlignLeft = MakeButton("LEFT")
        _btnAlignCenter = MakeButton("CENTER")
        _btnAlignRight = MakeButton("RIGHT")
        _btnBullets = MakeButton("LIST")
        Dim btnTable As ToolStripButton = MakeButton("TABLE")
        Dim btnStyles As New ToolStripDropDownButton("STYLE")
        btnStyles.DropDownItems.Add("Normal", Nothing, AddressOf OnStyleNormal)
        btnStyles.DropDownItems.Add("Heading 1", Nothing, AddressOf OnStyleH1)
        btnStyles.DropDownItems.Add("Heading 2", Nothing, AddressOf OnStyleH2)
        btnStyles.DropDownItems.Add("Quote", Nothing, AddressOf OnStyleQuote)
        btnStyles.DropDownItems.Add("Code", Nothing, AddressOf OnStyleCode)
        Dim btnHeader As ToolStripButton = MakeButton("HEADER")
        Dim btnFooter As ToolStripButton = MakeButton("FOOTER")
        _btnFind = MakeButton("FIND")
        _btnReplace = MakeButton("REPLACE")
        _btnWrap = MakeToggle("WRAP")
        _btnWrap.Checked = True
        _btnLines = MakeToggle("LINES")
        _btnLines.Checked = True
        _btnAutoCap = MakeToggle("AUTO CAP")
        _btnAutoCap.Checked = True
        _btnPreview = MakeToggle("PREVIEW")
        Dim btnPrint As ToolStripButton = MakeButton("PRINT")

        _btnTemplate = New ToolStripDropDownButton("TEMPLATE")
        _btnTemplate.DropDownItems.Add("Blank", Nothing, AddressOf OnTemplateBlank)
        _btnTemplate.DropDownItems.Add("Cover Page", Nothing, Sub(s, e) ApplyRtfTemplate(DocumentTemplates.CoverPageRtf()))
        _btnTemplate.DropDownItems.Add("Professional Letter", Nothing, Sub(s, e) ApplyRtfTemplate(DocumentTemplates.ProfessionalLetterRtf()))
        _btnTemplate.DropDownItems.Add("Formal Letter", Nothing, Sub(s, e) ApplyRtfTemplate(DocumentTemplates.FormalLetterRtf()))
        _btnTemplate.DropDownItems.Add("Personal Letter", Nothing, Sub(s, e) ApplyRtfTemplate(DocumentTemplates.PersonalLetterRtf()))
        _btnTemplate.DropDownItems.Add("Memo", Nothing, Sub(s, e) ApplyRtfTemplate(DocumentTemplates.MemoRtf()))
        _btnTemplate.DropDownItems.Add("Meeting Agenda", Nothing, Sub(s, e) ApplyRtfTemplate(DocumentTemplates.MeetingAgendaRtf()))
        _btnTemplate.DropDownItems.Add("Meeting Notes (Markdown)", Nothing, Sub(s, e) ApplyMarkdownTemplate(DocumentTemplates.MeetingNotesMarkdown()))
        _btnTemplate.DropDownItems.Add("Report", Nothing, Sub(s, e) ApplyRtfTemplate(DocumentTemplates.ReportRtf()))
        _btnTemplate.DropDownItems.Add("Project Proposal", Nothing, Sub(s, e) ApplyRtfTemplate(DocumentTemplates.ProposalRtf()))
        _btnTemplate.DropDownItems.Add("Resume", Nothing, Sub(s, e) ApplyRtfTemplate(DocumentTemplates.ResumeRtf()))
        _btnTemplate.DropDownItems.Add("Invoice", Nothing, Sub(s, e) ApplyRtfTemplate(DocumentTemplates.InvoiceRtf()))
        _btnTemplate.DropDownItems.Add("To-Do List (Markdown)", Nothing, Sub(s, e) ApplyMarkdownTemplate(DocumentTemplates.ToDoListMarkdown()))

        _toolStrip.Items.AddRange(New ToolStripItem() {
            _btnBold, _btnItalic, _btnUnderline, _btnFont,
            New ToolStripSeparator(),
            _btnAlignLeft, _btnAlignCenter, _btnAlignRight, _btnBullets, btnTable, btnStyles, btnHeader, btnFooter,
            New ToolStripSeparator(),
            _btnFind, _btnReplace, _btnWrap, _btnLines, _btnAutoCap, _btnPreview, btnPrint, _btnTemplate
        })

        _split = New SplitContainer() With {
            .Dock = DockStyle.Fill,
            .Orientation = Orientation.Vertical,
            .Panel2Collapsed = True,
            .BackColor = Color.Black
        }
        _split.Panel1.BackColor = Color.Black
        _split.Panel2.BackColor = Color.Black

        _editorHost = New Panel() With {.Dock = DockStyle.Fill, .BackColor = Color.Black}
        _lineMargin = New LineNumberMargin()
        _richText = New RichTextBox() With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.Black,
            .ForeColor = Color.FromArgb(255, 153, 0),
            .Font = New Font("Segoe UI", 11.0F),
            .AcceptsTab = True,
            .HideSelection = False,
            .WordWrap = True,
            .DetectUrls = True,
            .ScrollBars = RichTextBoxScrollBars.Both
        }
        _lineMargin.Editor = _richText
        _editorHost.Controls.Add(_richText)
        _editorHost.Controls.Add(_lineMargin)

        _preview = New WebView2() With {.Dock = DockStyle.Fill, .Name = "DocumentPreviewWebView2"}
        _split.Panel1.Controls.Add(_editorHost)
        _split.Panel2.Controls.Add(_preview)

        _hostPanel.Controls.Add(_split)
        _hostPanel.Controls.Add(_toolStrip)

        AddHandler _richText.TextChanged, AddressOf OnTextChanged
        AddHandler _richText.SelectionChanged, AddressOf OnSelectionChanged
        AddHandler _richText.KeyPress, AddressOf OnKeyPress
        AddHandler _richText.MouseWheel, AddressOf OnEditorScroll
        AddHandler _richText.KeyUp, AddressOf OnEditorScroll

        AddHandler _btnBold.Click, AddressOf OnBold
        AddHandler _btnItalic.Click, AddressOf OnItalic
        AddHandler _btnUnderline.Click, AddressOf OnUnderline
        AddHandler _btnFont.Click, AddressOf OnFont
        AddHandler _btnAlignLeft.Click, Sub() _richText.SelectionAlignment = HorizontalAlignment.Left
        AddHandler _btnAlignCenter.Click, Sub() _richText.SelectionAlignment = HorizontalAlignment.Center
        AddHandler _btnAlignRight.Click, Sub() _richText.SelectionAlignment = HorizontalAlignment.Right
        AddHandler _btnBullets.Click, Sub() _richText.SelectionBullet = Not _richText.SelectionBullet
        AddHandler btnTable.Click, AddressOf OnInsertTable
        AddHandler btnHeader.Click, AddressOf OnEditHeader
        AddHandler btnFooter.Click, AddressOf OnEditFooter
        AddHandler _btnFind.Click, AddressOf OnFind
        AddHandler _btnReplace.Click, AddressOf OnReplace
        AddHandler _btnWrap.CheckedChanged, Sub() _richText.WordWrap = _btnWrap.Checked
        AddHandler _btnLines.CheckedChanged, AddressOf OnLinesToggled
        AddHandler _btnAutoCap.CheckedChanged, Sub() _autoCap = _btnAutoCap.Checked
        AddHandler _btnPreview.CheckedChanged, AddressOf OnPreviewToggled
        AddHandler btnPrint.Click, AddressOf OnPrint
    End Sub

    Public ReadOnly Property TabKind As String Implements IBrowserTab.TabKind
        Get
            Return "Document"
        End Get
    End Property

    Public ReadOnly Property Title As String Implements IBrowserTab.Title
        Get
            Dim name As String = If(String.IsNullOrEmpty(_filePath), "NEW DOCUMENT", Path.GetFileName(_filePath))
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

    Public ReadOnly Property CurrentFormat As DocumentFormatKind
        Get
            Return _format
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
            _format = InferFormat(path)
            NewDocument(_format)
        End If
    End Sub

    Public Function Save(ByVal path As String) As Boolean Implements IBrowserTab.Save
        If String.IsNullOrWhiteSpace(path) Then Return False
        Try
            Dim parentDir As String = System.IO.Path.GetDirectoryName(path)
            If Not String.IsNullOrEmpty(parentDir) AndAlso Not Directory.Exists(parentDir) Then
                Directory.CreateDirectory(parentDir)
            End If

            _format = InferFormat(path)
            Select Case _format
                Case DocumentFormatKind.RichText
                    _richText.SaveFile(path, RichTextBoxStreamType.RichText)
                Case DocumentFormatKind.Docx
                    DocxDocumentAdapter.SaveFrom(_richText, path)
                Case Else
                    ' Plain text / Markdown
                    File.WriteAllText(path, _richText.Text, New UTF8Encoding(False))
            End Select

            _filePath = path
            _dirty = False
            ApplyFormatUi()
            RaiseEvent ContentChanged(Me, EventArgs.Empty)
            Return True
        Catch
            Return False
        End Try
    End Function

    Public Sub FocusContent() Implements IBrowserTab.FocusContent
        _richText.Focus()
    End Sub

    Public Sub NewDocument(Optional ByVal format As DocumentFormatKind = DocumentFormatKind.RichText)
        _filePath = ""
        _format = format
        _suppressDirty = True
        If format = DocumentFormatKind.Markdown Then
            _richText.Text = DocumentTemplates.BlankMarkdown()
        ElseIf format = DocumentFormatKind.PlainText Then
            _richText.Clear()
            _richText.Font = New Font("Consolas", 11.0F)
        Else
            _richText.Rtf = DocumentTemplates.BlankRtf()
            _richText.Font = New Font("Segoe UI", 11.0F)
        End If
        _dirty = False
        _suppressDirty = False
        ApplyFormatUi()
        RaiseEvent ContentChanged(Me, EventArgs.Empty)
    End Sub

    Public Sub Dispose()
        If _hostPanel IsNot Nothing Then _hostPanel.Dispose()
    End Sub

    Private Sub LoadFromFile(ByVal path As String)
        Try
            _suppressDirty = True
            _format = InferFormat(path)
            Select Case _format
                Case DocumentFormatKind.RichText
                    _richText.LoadFile(path, RichTextBoxStreamType.RichText)
                Case DocumentFormatKind.Docx
                    DocxDocumentAdapter.LoadInto(_richText, path)
                Case Else
                    _richText.Text = File.ReadAllText(path, Encoding.UTF8)
            End Select
            _filePath = path
            _dirty = False
            _suppressDirty = False
            ApplyFormatUi()
            RaiseEvent ContentChanged(Me, EventArgs.Empty)
        Catch ex As Exception
            _suppressDirty = False
            MessageBox.Show("Unable to open file:" & vbCrLf & ex.Message, "LCARS Web Browser", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ApplyFormatUi()
        Dim rich As Boolean = (_format = DocumentFormatKind.RichText OrElse _format = DocumentFormatKind.Docx)
        _btnBold.Enabled = rich
        _btnItalic.Enabled = rich
        _btnUnderline.Enabled = rich
        _btnFont.Enabled = rich
        _btnAlignLeft.Enabled = rich
        _btnAlignCenter.Enabled = rich
        _btnAlignRight.Enabled = rich
        _btnBullets.Enabled = rich
        _btnPreview.Enabled = (_format = DocumentFormatKind.Markdown)
        If _format <> DocumentFormatKind.Markdown Then
            _btnPreview.Checked = False
            _split.Panel2Collapsed = True
        End If
        If _format = DocumentFormatKind.PlainText OrElse _format = DocumentFormatKind.Markdown Then
            _richText.Font = New Font("Consolas", 11.0F)
        End If
    End Sub

    Private Shared Function InferFormat(ByVal path As String) As DocumentFormatKind
        Dim ext As String = System.IO.Path.GetExtension(path).ToLowerInvariant()
        Select Case ext
            Case ".md" : Return DocumentFormatKind.Markdown
            Case ".rtf" : Return DocumentFormatKind.RichText
            Case ".docx" : Return DocumentFormatKind.Docx
            Case Else : Return DocumentFormatKind.PlainText
        End Select
    End Function

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

    Private Function MakeButton(ByVal caption As String) As ToolStripButton
        Dim b As New ToolStripButton(caption)
        b.DisplayStyle = ToolStripItemDisplayStyle.Text
        Return b
    End Function

    Private Function MakeToggle(ByVal caption As String) As ToolStripButton
        Dim b As ToolStripButton = MakeButton(caption)
        b.CheckOnClick = True
        Return b
    End Function

    Private Sub OnTextChanged(ByVal sender As Object, ByVal e As EventArgs)
        If _suppressDirty Then Return
        If Not _dirty Then
            _dirty = True
            RaiseEvent ContentChanged(Me, EventArgs.Empty)
        End If
        _lineMargin.Invalidate()
        If _btnPreview.Checked Then UpdateMarkdownPreview()
    End Sub

    Private Sub OnEditorScroll(ByVal sender As Object, ByVal e As EventArgs)
        _lineMargin.Invalidate()
    End Sub

    Private Sub OnSelectionChanged(ByVal sender As Object, ByVal e As EventArgs)
        Dim font As Font = If(_richText.SelectionFont, _richText.Font)
        _btnBold.Checked = (font.Style And FontStyle.Bold) = FontStyle.Bold
        _btnItalic.Checked = (font.Style And FontStyle.Italic) = FontStyle.Italic
        _btnUnderline.Checked = (font.Style And FontStyle.Underline) = FontStyle.Underline
    End Sub

    Private Sub OnKeyPress(ByVal sender As Object, ByVal e As KeyPressEventArgs)
        TypingAids.TryAutoCapitalize(_richText, e, _autoCap)
    End Sub

    Private Sub ToggleFontStyle(ByVal style As FontStyle, ByVal enable As Boolean)
        Dim current As Font = If(_richText.SelectionFont, _richText.Font)
        Dim nextStyle As FontStyle = current.Style
        If enable Then
            nextStyle = nextStyle Or style
        Else
            nextStyle = nextStyle And Not style
        End If
        _richText.SelectionFont = New Font(current.FontFamily, current.Size, nextStyle)
    End Sub

    Private Sub OnBold(ByVal sender As Object, ByVal e As EventArgs)
        ToggleFontStyle(FontStyle.Bold, _btnBold.Checked)
    End Sub

    Private Sub OnItalic(ByVal sender As Object, ByVal e As EventArgs)
        ToggleFontStyle(FontStyle.Italic, _btnItalic.Checked)
    End Sub

    Private Sub OnUnderline(ByVal sender As Object, ByVal e As EventArgs)
        ToggleFontStyle(FontStyle.Underline, _btnUnderline.Checked)
    End Sub

    Private Sub OnFont(ByVal sender As Object, ByVal e As EventArgs)
        Using dlg As New FontDialog()
            dlg.Font = If(_richText.SelectionFont, _richText.Font)
            dlg.ShowEffects = True
            If dlg.ShowDialog(_hostPanel.FindForm()) = DialogResult.OK Then
                _richText.SelectionFont = dlg.Font
            End If
        End Using
    End Sub

    Private Sub OnInsertTable(ByVal sender As Object, ByVal e As EventArgs)
        Dim rowsText As String = InputBox("Rows:", "Insert Table", "3")
        Dim colsText As String = InputBox("Columns:", "Insert Table", "3")
        Dim rows As Integer
        Dim cols As Integer
        If Not Integer.TryParse(rowsText, rows) Then rows = 3
        If Not Integer.TryParse(colsText, cols) Then cols = 3
        _richText.SelectedRtf = RtfTableBuilder.BuildTable(rows, cols)
    End Sub

    Private Sub OnStyleNormal(ByVal sender As Object, ByVal e As EventArgs)
        ApplyParagraphStyle(New Font("Segoe UI", 11.0F, FontStyle.Regular), Color.FromArgb(255, 153, 0), 0)
    End Sub

    Private Sub OnStyleH1(ByVal sender As Object, ByVal e As EventArgs)
        ApplyParagraphStyle(New Font("Segoe UI", 20.0F, FontStyle.Bold), Color.FromArgb(255, 204, 102), 0)
    End Sub

    Private Sub OnStyleH2(ByVal sender As Object, ByVal e As EventArgs)
        ApplyParagraphStyle(New Font("Segoe UI", 16.0F, FontStyle.Bold), Color.FromArgb(255, 204, 102), 0)
    End Sub

    Private Sub OnStyleQuote(ByVal sender As Object, ByVal e As EventArgs)
        ApplyParagraphStyle(New Font("Georgia", 11.0F, FontStyle.Italic), Color.FromArgb(200, 180, 140), 36)
    End Sub

    Private Sub OnStyleCode(ByVal sender As Object, ByVal e As EventArgs)
        ApplyParagraphStyle(New Font("Consolas", 10.0F, FontStyle.Regular), Color.FromArgb(180, 255, 180), 0)
    End Sub

    Private Sub ApplyParagraphStyle(ByVal font As Font, ByVal color As Color, ByVal indent As Integer)
        _richText.SelectionFont = font
        _richText.SelectionColor = color
        _richText.SelectionIndent = indent
    End Sub

    Private Sub OnEditHeader(ByVal sender As Object, ByVal e As EventArgs)
        Dim value As String = InputBox("Header text (printed at top of each page):", "Document Header", _headerText)
        If value IsNot Nothing Then
            _headerText = value
            _dirty = True
            RaiseEvent ContentChanged(Me, EventArgs.Empty)
        End If
    End Sub

    Private Sub OnEditFooter(ByVal sender As Object, ByVal e As EventArgs)
        Dim value As String = InputBox("Footer text (printed at bottom of each page):", "Document Footer", _footerText)
        If value IsNot Nothing Then
            _footerText = value
            _dirty = True
            RaiseEvent ContentChanged(Me, EventArgs.Empty)
        End If
    End Sub

    Private Sub OnFind(ByVal sender As Object, ByVal e As EventArgs)
        Dim needle As String = InputBox("Find:", "LCARS Document")
        If String.IsNullOrEmpty(needle) Then Return
        Dim start As Integer = _richText.Find(needle, _richText.SelectionStart + _richText.SelectionLength, RichTextBoxFinds.None)
        If start < 0 Then
            start = _richText.Find(needle, 0, RichTextBoxFinds.None)
        End If
        If start < 0 Then MessageBox.Show("Text not found.", "LCARS Web Browser")
    End Sub

    Private Sub OnReplace(ByVal sender As Object, ByVal e As EventArgs)
        Dim find As String = InputBox("Find:", "LCARS Document")
        If String.IsNullOrEmpty(find) Then Return
        Dim repl As String = InputBox("Replace with:", "LCARS Document")
        _richText.Text = _richText.Text.Replace(find, If(repl, ""))
    End Sub

    Private Sub OnLinesToggled(ByVal sender As Object, ByVal e As EventArgs)
        _lineMargin.Visible = _btnLines.Checked
    End Sub

    Private Async Sub OnPreviewToggled(ByVal sender As Object, ByVal e As EventArgs)
        _split.Panel2Collapsed = Not _btnPreview.Checked
        If _btnPreview.Checked Then
            If Not _previewReady Then
                Try
                    Await _preview.EnsureCoreWebView2Async(Nothing)
                    _previewReady = True
                Catch
                    MessageBox.Show("Markdown preview requires WebView2.", "LCARS Web Browser")
                    _btnPreview.Checked = False
                    Return
                End Try
            End If
            UpdateMarkdownPreview()
        End If
    End Sub

    Private Sub UpdateMarkdownPreview()
        If Not _previewReady OrElse _preview.CoreWebView2 Is Nothing Then Return
        Dim html As String = MarkdownToHtml(_richText.Text)
        _preview.NavigateToString(html)
    End Sub

    Private Shared Function MarkdownToHtml(ByVal md As String) As String
        Dim text As String = WebUtility.HtmlEncode(If(md, ""))
        text = Regex.Replace(text, "(?m)^### (.+)$", "<h3>$1</h3>")
        text = Regex.Replace(text, "(?m)^## (.+)$", "<h2>$1</h2>")
        text = Regex.Replace(text, "(?m)^# (.+)$", "<h1>$1</h1>")
        text = Regex.Replace(text, "\*\*(.+?)\*\*", "<strong>$1</strong>")
        text = Regex.Replace(text, "(?m)^- (.+)$", "<li>$1</li>")
        text = text.Replace(vbCrLf, "<br/>").Replace(vbLf, "<br/>")
        Return "<!DOCTYPE html><html><head><meta charset='utf-8'><style>body{background:#111;color:#f90;font-family:Segoe UI,sans-serif;padding:16px} h1,h2,h3{color:#fc6}</style></head><body>" & text & "</body></html>"
    End Function

    Private Sub OnPrint(ByVal sender As Object, ByVal e As EventArgs)
        Using doc As New Printing.PrintDocument()
            Dim body As String = _richText.Text
            Dim header As String = _headerText
            Dim footer As String = _footerText
            AddHandler doc.PrintPage, Sub(s, pe)
                                          Dim bounds As Rectangle = pe.MarginBounds
                                          Dim y As Single = bounds.Top
                                          Using font As New Font("Segoe UI", 10.0F)
                                              If Not String.IsNullOrEmpty(header) Then
                                                  pe.Graphics.DrawString(header, font, Brushes.Black, bounds.Left, y)
                                                  y += font.GetHeight(pe.Graphics) + 8
                                              End If
                                              Dim bodyRect As New RectangleF(bounds.Left, y, bounds.Width, bounds.Bottom - y - 24)
                                              pe.Graphics.DrawString(body, _richText.Font, Brushes.Black, bodyRect)
                                              If Not String.IsNullOrEmpty(footer) Then
                                                  pe.Graphics.DrawString(footer, font, Brushes.Black, bounds.Left, bounds.Bottom - 18)
                                              End If
                                          End Using
                                      End Sub
            Using dlg As New PrintDialog()
                dlg.Document = doc
                If dlg.ShowDialog(_hostPanel.FindForm()) = DialogResult.OK Then
                    doc.Print()
                End If
            End Using
        End Using
    End Sub

    Private Sub OnTemplateBlank(ByVal sender As Object, ByVal e As EventArgs)
        NewDocument(DocumentFormatKind.RichText)
    End Sub

    Private Sub ApplyRtfTemplate(ByVal rtf As String)
        _suppressDirty = True
        _format = DocumentFormatKind.RichText
        _richText.Rtf = rtf
        _filePath = ""
        _dirty = True
        _suppressDirty = False
        ApplyFormatUi()
        RaiseEvent ContentChanged(Me, EventArgs.Empty)
    End Sub

    Private Sub ApplyMarkdownTemplate(ByVal markdown As String)
        _suppressDirty = True
        _format = DocumentFormatKind.Markdown
        _richText.Text = markdown
        _filePath = ""
        _dirty = True
        _suppressDirty = False
        ApplyFormatUi()
        RaiseEvent ContentChanged(Me, EventArgs.Empty)
    End Sub
End Class
