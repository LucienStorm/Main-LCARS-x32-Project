Imports System.IO
Imports System.Windows.Forms

''' <summary>
''' IBrowserTab host for RTF editing with basic formatting controls.
''' </summary>
Public Class RichTextEditTab
    Implements IBrowserTab

    Private ReadOnly _hostPanel As Panel
    Private ReadOnly _toolStrip As ToolStrip
    Private ReadOnly _richTextBox As RichTextBox
    Private ReadOnly _boldButton As ToolStripButton
    Private ReadOnly _italicButton As ToolStripButton
    Private ReadOnly _fontButton As ToolStripButton

    Private _filePath As String = ""
    Private _dirty As Boolean = False
    Private _suppressDirty As Boolean = False

    Public Event ContentChanged As EventHandler

    Public Sub New()
        _hostPanel = New Panel()
        _hostPanel.Dock = DockStyle.Fill

        _toolStrip = New ToolStrip()
        _toolStrip.Dock = DockStyle.Top
        _toolStrip.GripStyle = ToolStripGripStyle.Hidden

        _boldButton = New ToolStripButton("BOLD")
        _boldButton.CheckOnClick = True
        _boldButton.Font = New Drawing.Font(_boldButton.Font, Drawing.FontStyle.Bold)

        _italicButton = New ToolStripButton("ITALIC")
        _italicButton.CheckOnClick = True
        _italicButton.Font = New Drawing.Font(_italicButton.Font, Drawing.FontStyle.Italic)

        _fontButton = New ToolStripButton("FONT")

        _toolStrip.Items.Add(_boldButton)
        _toolStrip.Items.Add(_italicButton)
        _toolStrip.Items.Add(_fontButton)

        _richTextBox = New RichTextBox()
        _richTextBox.Dock = DockStyle.Fill
        _richTextBox.Font = New Drawing.Font("Segoe UI", 11.0F)
        _richTextBox.HideSelection = False
        _richTextBox.AcceptsTab = True
        _richTextBox.BackColor = Drawing.Color.Black
        _richTextBox.ForeColor = Drawing.Color.FromArgb(255, 153, 0)
        _hostPanel.BackColor = Drawing.Color.Black
        _toolStrip.BackColor = Drawing.Color.Black
        _toolStrip.ForeColor = Drawing.Color.FromArgb(255, 153, 0)

        _hostPanel.Controls.Add(_richTextBox)
        _hostPanel.Controls.Add(_toolStrip)

        AddHandler _richTextBox.TextChanged, AddressOf OnEditorTextChanged
        AddHandler _richTextBox.SelectionChanged, AddressOf OnSelectionChanged
        AddHandler _boldButton.Click, AddressOf OnBoldClick
        AddHandler _italicButton.Click, AddressOf OnItalicClick
        AddHandler _fontButton.Click, AddressOf OnFontClick
    End Sub

    Public ReadOnly Property TabKind As String Implements IBrowserTab.TabKind
        Get
            Return "RichText"
        End Get
    End Property

    Public ReadOnly Property Title As String Implements IBrowserTab.Title
        Get
            Dim name As String = If(String.IsNullOrEmpty(_filePath), "NEW RICH TEXT", System.IO.Path.GetFileName(_filePath))
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
            ClearEditor()
        End If
    End Sub

    Public Function Save(ByVal path As String) As Boolean Implements IBrowserTab.Save
        If String.IsNullOrWhiteSpace(path) Then Return False

        Try
            Dim parentDir As String = System.IO.Path.GetDirectoryName(path)
            If Not String.IsNullOrEmpty(parentDir) AndAlso Not Directory.Exists(parentDir) Then
                Directory.CreateDirectory(parentDir)
            End If

            _richTextBox.SaveFile(path, RichTextBoxStreamType.RichText)
            _filePath = path
            _dirty = False
            RaiseEvent ContentChanged(Me, EventArgs.Empty)
            Return True
        Catch
            Return False
        End Try
    End Function

    Public Sub FocusContent() Implements IBrowserTab.FocusContent
        _richTextBox.Focus()
    End Sub

    ''' <summary>
    ''' Clears the editor for a new untitled document.
    ''' </summary>
    Public Sub NewDocument()
        _filePath = ""
        ClearEditor()
    End Sub

    Private Sub LoadFromFile(ByVal path As String)
        Try
            _suppressDirty = True
            _richTextBox.LoadFile(path, RichTextBoxStreamType.RichText)
            _filePath = path
            _dirty = False
            _suppressDirty = False
            RaiseEvent ContentChanged(Me, EventArgs.Empty)
        Catch ex As Exception
            _suppressDirty = False
            MessageBox.Show("Unable to open file:" & vbCrLf & ex.Message, "LCARS Web Browser", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ClearEditor()
        _suppressDirty = True
        _richTextBox.Clear()
        _dirty = False
        _suppressDirty = False
        RaiseEvent ContentChanged(Me, EventArgs.Empty)
    End Sub

    Private Sub OnEditorTextChanged(ByVal sender As Object, ByVal e As EventArgs)
        If _suppressDirty Then Return
        If Not _dirty Then
            _dirty = True
            RaiseEvent ContentChanged(Me, EventArgs.Empty)
        End If
    End Sub

    Private Function GetSelectionFont() As Drawing.Font
        Dim current As Drawing.Font = _richTextBox.SelectionFont
        If current Is Nothing Then Return _richTextBox.Font
        Return current
    End Function

    Private Sub OnSelectionChanged(ByVal sender As Object, ByVal e As EventArgs)
        Dim style As Drawing.FontStyle = GetSelectionFont().Style
        _boldButton.Checked = (style And Drawing.FontStyle.Bold) = Drawing.FontStyle.Bold
        _italicButton.Checked = (style And Drawing.FontStyle.Italic) = Drawing.FontStyle.Italic
    End Sub

    Private Sub OnBoldClick(ByVal sender As Object, ByVal e As EventArgs)
        Dim style As Drawing.FontStyle = GetSelectionFont().Style
        If _boldButton.Checked Then
            style = style Or Drawing.FontStyle.Bold
        Else
            style = style And Not Drawing.FontStyle.Bold
        End If
        ApplySelectionFont(style)
    End Sub

    Private Sub OnItalicClick(ByVal sender As Object, ByVal e As EventArgs)
        Dim style As Drawing.FontStyle = GetSelectionFont().Style
        If _italicButton.Checked Then
            style = style Or Drawing.FontStyle.Italic
        Else
            style = style And Not Drawing.FontStyle.Italic
        End If
        ApplySelectionFont(style)
    End Sub

    Private Sub OnFontClick(ByVal sender As Object, ByVal e As EventArgs)
        Using dlg As New FontDialog()
            dlg.Font = GetSelectionFont()
            dlg.ShowEffects = True
            If dlg.ShowDialog(_hostPanel.FindForm()) <> DialogResult.OK Then Return
            _richTextBox.SelectionFont = dlg.Font
            _boldButton.Checked = (dlg.Font.Style And Drawing.FontStyle.Bold) = Drawing.FontStyle.Bold
            _italicButton.Checked = (dlg.Font.Style And Drawing.FontStyle.Italic) = Drawing.FontStyle.Italic
        End Using
    End Sub

    Private Sub ApplySelectionFont(ByVal style As Drawing.FontStyle)
        Dim current As Drawing.Font = GetSelectionFont()
        _richTextBox.SelectionFont = New Drawing.Font(current.FontFamily, current.Size, style)
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
        If _hostPanel IsNot Nothing Then
            _hostPanel.Dispose()
        End If
    End Sub
End Class
