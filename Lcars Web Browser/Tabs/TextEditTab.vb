Imports System.IO
Imports System.Text
Imports System.Windows.Forms

''' <summary>
''' IBrowserTab host for plain-text editing with UTF-8 load/save.
''' </summary>
Public Class TextEditTab
    Implements IBrowserTab

    Private ReadOnly _hostPanel As Panel
    Private ReadOnly _toolStrip As ToolStrip
    Private ReadOnly _textBox As TextBox
    Private ReadOnly _findButton As ToolStripButton
    Private ReadOnly _replaceButton As ToolStripButton
    Private ReadOnly _wrapButton As ToolStripButton

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

        _findButton = New ToolStripButton("FIND")
        _replaceButton = New ToolStripButton("REPLACE")
        _wrapButton = New ToolStripButton("WORD WRAP")
        _wrapButton.CheckOnClick = True
        _wrapButton.Checked = True

        _toolStrip.Items.Add(_findButton)
        _toolStrip.Items.Add(_replaceButton)
        _toolStrip.Items.Add(_wrapButton)

        _textBox = New TextBox()
        _textBox.Multiline = True
        _textBox.Dock = DockStyle.Fill
        _textBox.ScrollBars = ScrollBars.Both
        _textBox.WordWrap = True
        _textBox.Font = New Drawing.Font("Consolas", 10.0F)
        _textBox.AcceptsReturn = True
        _textBox.AcceptsTab = True
        _textBox.HideSelection = False
        _textBox.MaxLength = 0
        _textBox.BackColor = Drawing.Color.Black
        _textBox.ForeColor = Drawing.Color.FromArgb(255, 153, 0)
        _hostPanel.BackColor = Drawing.Color.Black
        _toolStrip.BackColor = Drawing.Color.Black
        _toolStrip.ForeColor = Drawing.Color.FromArgb(255, 153, 0)

        _hostPanel.Controls.Add(_textBox)
        _hostPanel.Controls.Add(_toolStrip)

        AddHandler _textBox.TextChanged, AddressOf OnEditorTextChanged
        AddHandler _findButton.Click, AddressOf OnFindClick
        AddHandler _replaceButton.Click, AddressOf OnReplaceClick
        AddHandler _wrapButton.CheckedChanged, AddressOf OnWrapToggled
    End Sub

    Public ReadOnly Property TabKind As String Implements IBrowserTab.TabKind
        Get
            Return "Text"
        End Get
    End Property

    Public ReadOnly Property Title As String Implements IBrowserTab.Title
        Get
            Dim name As String = If(String.IsNullOrEmpty(_filePath), "NEW TEXT", System.IO.Path.GetFileName(_filePath))
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
    ''' Clears the editor for a new untitled document.
    ''' </summary>
    Public Sub NewDocument()
        _filePath = ""
        SetEditorText("")
    End Sub

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
        RaiseEvent ContentChanged(Me, EventArgs.Empty)
    End Sub

    Private Sub OnEditorTextChanged(ByVal sender As Object, ByVal e As EventArgs)
        If _suppressDirty Then Return
        If Not _dirty Then
            _dirty = True
            RaiseEvent ContentChanged(Me, EventArgs.Empty)
        End If
    End Sub

    Private Sub OnWrapToggled(ByVal sender As Object, ByVal e As EventArgs)
        _textBox.WordWrap = _wrapButton.Checked
        If _textBox.WordWrap Then
            _textBox.ScrollBars = ScrollBars.Vertical
        Else
            _textBox.ScrollBars = ScrollBars.Both
        End If
    End Sub

    Private Sub OnFindClick(ByVal sender As Object, ByVal e As EventArgs)
        ShowFindReplaceDialog(replaceMode:=False)
    End Sub

    Private Sub OnReplaceClick(ByVal sender As Object, ByVal e As EventArgs)
        ShowFindReplaceDialog(replaceMode:=True)
    End Sub

    Private Sub ShowFindReplaceDialog(ByVal replaceMode As Boolean)
        Using dlg As New TextFindReplaceDialog(replaceMode)
            If dlg.ShowDialog(_hostPanel.FindForm()) <> DialogResult.OK Then Return

            Dim needle As String = dlg.FindText
            If String.IsNullOrEmpty(needle) Then Return

            Dim start As Integer = _textBox.SelectionStart + _textBox.SelectionLength
            Dim index As Integer = _textBox.Text.IndexOf(needle, start, StringComparison.OrdinalIgnoreCase)
            If index < 0 AndAlso start > 0 Then
                index = _textBox.Text.IndexOf(needle, 0, StringComparison.OrdinalIgnoreCase)
            End If

            If index < 0 Then
                MessageBox.Show("Text not found.", "LCARS Web Browser", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            _textBox.Select(index, needle.Length)
            If replaceMode Then
                _textBox.SelectedText = dlg.ReplaceText
            End If
            _textBox.Focus()
        End Using
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

    Private Class TextFindReplaceDialog
        Inherits Form

        Private ReadOnly _findBox As TextBox
        Private ReadOnly _replaceBox As TextBox
        Private ReadOnly _replaceLabel As Label

        Public Sub New(ByVal replaceMode As Boolean)
            Text = If(replaceMode, "Replace", "Find")
            FormBorderStyle = FormBorderStyle.FixedDialog
            StartPosition = FormStartPosition.CenterParent
            MinimizeBox = False
            MaximizeBox = False
            ShowInTaskbar = False
            ClientSize = New Drawing.Size(360, If(replaceMode, 150, 110))

            Dim findLabel As New Label()
            findLabel.Text = "Find:"
            findLabel.Location = New Drawing.Point(12, 15)
            findLabel.AutoSize = True

            _findBox = New TextBox()
            _findBox.Location = New Drawing.Point(80, 12)
            _findBox.Size = New Drawing.Size(260, 23)

            _replaceLabel = New Label()
            _replaceLabel.Text = "Replace:"
            _replaceLabel.Location = New Drawing.Point(12, 48)
            _replaceLabel.AutoSize = True
            _replaceLabel.Visible = replaceMode

            _replaceBox = New TextBox()
            _replaceBox.Location = New Drawing.Point(80, 45)
            _replaceBox.Size = New Drawing.Size(260, 23)
            _replaceBox.Visible = replaceMode

            Dim okButton As New Button()
            okButton.Text = If(replaceMode, "Replace", "Find")
            okButton.DialogResult = DialogResult.OK
            okButton.Location = New Drawing.Point(184, If(replaceMode, 88, 48))
            okButton.Size = New Drawing.Size(75, 27)

            Dim cancelButton As New Button()
            cancelButton.Text = "Cancel"
            cancelButton.DialogResult = DialogResult.Cancel
            cancelButton.Location = New Drawing.Point(265, If(replaceMode, 88, 48))
            cancelButton.Size = New Drawing.Size(75, 27)

            Controls.Add(findLabel)
            Controls.Add(_findBox)
            Controls.Add(_replaceLabel)
            Controls.Add(_replaceBox)
            Controls.Add(okButton)
            Controls.Add(cancelButton)

            AcceptButton = okButton
            CancelButton = cancelButton
        End Sub

        Public ReadOnly Property FindText As String
            Get
                Return _findBox.Text
            End Get
        End Property

        Public ReadOnly Property ReplaceText As String
            Get
                Return _replaceBox.Text
            End Get
        End Property
    End Class
End Class
