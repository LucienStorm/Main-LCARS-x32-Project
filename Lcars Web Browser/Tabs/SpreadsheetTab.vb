Imports System.IO
Imports System.Windows.Forms

''' <summary>
''' IBrowserTab host for spreadsheet editing with formulas and CSV/XLSX I/O.
''' </summary>
Public Class SpreadsheetTab
    Implements IBrowserTab

    Private Const DefaultRows As Integer = 50
    Private Const DefaultCols As Integer = 20

    Private ReadOnly _hostPanel As Panel
    Private ReadOnly _grid As DataGridView
    Private ReadOnly _model As SheetModel

    Private _filePath As String = ""
    Private _dirty As Boolean = False
    Private _suppressDirty As Boolean = False
    Private _editingAddress As String = ""

    Public Event ContentChanged As EventHandler

    Public Sub New()
        _model = New SheetModel()
        AddHandler _model.ModelChanged, AddressOf OnModelChanged

        _hostPanel = New Panel()
        _hostPanel.Dock = DockStyle.Fill

        _grid = New DataGridView()
        _grid.Dock = DockStyle.Fill
        _grid.AllowUserToAddRows = False
        _grid.AllowUserToDeleteRows = False
        _grid.RowHeadersWidth = 48
        _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
        _grid.SelectionMode = DataGridViewSelectionMode.CellSelect
        _grid.EditMode = DataGridViewEditMode.EditOnEnter
        _grid.StandardTab = True
        _grid.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText
        _grid.BackgroundColor = Drawing.Color.Black
        _grid.GridColor = Drawing.Color.FromArgb(80, 60, 20)
        _grid.DefaultCellStyle.BackColor = Drawing.Color.Black
        _grid.DefaultCellStyle.ForeColor = Drawing.Color.FromArgb(255, 153, 0)
        _grid.DefaultCellStyle.SelectionBackColor = Drawing.Color.FromArgb(80, 50, 0)
        _grid.DefaultCellStyle.SelectionForeColor = Drawing.Color.FromArgb(255, 200, 100)
        _grid.ColumnHeadersDefaultCellStyle.BackColor = Drawing.Color.FromArgb(30, 20, 0)
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Drawing.Color.FromArgb(255, 153, 0)
        _grid.RowHeadersDefaultCellStyle.BackColor = Drawing.Color.FromArgb(30, 20, 0)
        _grid.RowHeadersDefaultCellStyle.ForeColor = Drawing.Color.FromArgb(255, 153, 0)
        _grid.EnableHeadersVisualStyles = False
        _hostPanel.BackColor = Drawing.Color.Black

        InitializeGridStructure()
        _hostPanel.Controls.Add(_grid)

        AddHandler _grid.CellBeginEdit, AddressOf OnCellBeginEdit
        AddHandler _grid.CellEndEdit, AddressOf OnCellEndEdit
        AddHandler _grid.CellFormatting, AddressOf OnCellFormatting
    End Sub

    Public ReadOnly Property TabKind As String Implements IBrowserTab.TabKind
        Get
            Return "Sheet"
        End Get
    End Property

    Public ReadOnly Property Title As String Implements IBrowserTab.Title
        Get
            Dim name As String = If(String.IsNullOrEmpty(_filePath), "NEW SHEET", System.IO.Path.GetFileName(_filePath))
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
            NewDocument()
        End If
    End Sub

    Public Function Save(ByVal path As String) As Boolean Implements IBrowserTab.Save
        If String.IsNullOrWhiteSpace(path) Then Return False

        Try
            Dim ext As String = System.IO.Path.GetExtension(path).ToLowerInvariant()
            If ext = ".xlsx" Then
                XlsxIo.Save(_model, path)
            Else
                CsvIo.Save(_model, path)
            End If

            _filePath = path
            _dirty = False
            RaiseEvent ContentChanged(Me, EventArgs.Empty)
            Return True
        Catch
            Return False
        End Try
    End Function

    Public Sub FocusContent() Implements IBrowserTab.FocusContent
        _grid.Focus()
    End Sub

    ''' <summary>
    ''' Clears the sheet for a new untitled document.
    ''' </summary>
    Public Sub NewDocument()
        _suppressDirty = True
        _model.Clear()
        RefreshGridFromModel()
        _dirty = False
        _suppressDirty = False
        RaiseEvent ContentChanged(Me, EventArgs.Empty)
    End Sub

    Private Sub LoadFromFile(ByVal path As String)
        Try
            Dim ext As String = System.IO.Path.GetExtension(path).ToLowerInvariant()
            Dim loaded As SheetModel
            If ext = ".xlsx" Then
                loaded = XlsxIo.Load(path)
            Else
                loaded = CsvIo.Load(path)
            End If

            _suppressDirty = True
            _model.ImportRawCells(loaded.ExportRawCells())
            _filePath = path
            EnsureGridFitsModel()
            RefreshGridFromModel()
            _dirty = False
            _suppressDirty = False
            RaiseEvent ContentChanged(Me, EventArgs.Empty)
        Catch ex As Exception
            MessageBox.Show("Unable to open spreadsheet:" & vbCrLf & ex.Message, "LCARS Web Browser", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub InitializeGridStructure()
        _grid.Columns.Clear()
        _grid.Rows.Clear()

        For col As Integer = 0 To DefaultCols - 1
            Dim column As New DataGridViewTextBoxColumn()
            column.Name = "Col" & col.ToString()
            column.HeaderText = SheetModel.ColToLetters(col)
            column.Width = 72
            _grid.Columns.Add(column)
        Next

        For row As Integer = 0 To DefaultRows - 1
            Dim index As Integer = _grid.Rows.Add()
            _grid.Rows(index).HeaderCell.Value = (row + 1).ToString()
        Next
    End Sub

    ''' <summary>
    ''' Grows the grid when loaded data exceeds the default dimensions.
    ''' </summary>
    Private Sub EnsureGridFitsModel()
        Dim bounds As Tuple(Of Integer, Integer) = _model.GetUsedBounds()
        Dim neededRows As Integer = Math.Max(DefaultRows, bounds.Item1 + 1)
        Dim neededCols As Integer = Math.Max(DefaultCols, bounds.Item2 + 1)

        While _grid.Columns.Count < neededCols
            Dim colIndex As Integer = _grid.Columns.Count
            Dim column As New DataGridViewTextBoxColumn()
            column.Name = "Col" & colIndex.ToString()
            column.HeaderText = SheetModel.ColToLetters(colIndex)
            column.Width = 72
            _grid.Columns.Add(column)
        End While

        While _grid.Rows.Count < neededRows
            Dim index As Integer = _grid.Rows.Add()
            _grid.Rows(index).HeaderCell.Value = (_grid.Rows.Count).ToString()
        End While
    End Sub

    Private Sub RefreshGridFromModel()
        _grid.SuspendLayout()
        For row As Integer = 0 To _grid.Rows.Count - 1
            For col As Integer = 0 To _grid.Columns.Count - 1
                Dim addr As String = SheetModel.AddressFrom(row, col)
                Dim raw As String = _model.GetRaw(addr)
                _grid.Rows(row).Cells(col).Value = If(raw <> "", raw, _model.GetDisplay(addr))
            Next
        Next
        _grid.ResumeLayout()
        _grid.Refresh()
    End Sub

    Private Sub OnCellBeginEdit(ByVal sender As Object, ByVal e As DataGridViewCellCancelEventArgs)
        If e.RowIndex < 0 OrElse e.ColumnIndex < 0 Then Return
        _editingAddress = SheetModel.AddressFrom(e.RowIndex, e.ColumnIndex)
        Dim raw As String = _model.GetRaw(_editingAddress)
        If raw <> "" Then
            _grid.Rows(e.RowIndex).Cells(e.ColumnIndex).Value = raw
        End If
    End Sub

    Private Sub OnCellEndEdit(ByVal sender As Object, ByVal e As DataGridViewCellEventArgs)
        If e.RowIndex < 0 OrElse e.ColumnIndex < 0 Then Return

        Dim addr As String = SheetModel.AddressFrom(e.RowIndex, e.ColumnIndex)
        Dim cell As DataGridViewCell = _grid.Rows(e.RowIndex).Cells(e.ColumnIndex)
        Dim input As String = If(cell.Value, "").ToString()

        _model.SetRaw(addr, input)
        _editingAddress = ""
        RefreshGridFromModel()

        If Not _suppressDirty AndAlso Not _dirty Then
            _dirty = True
            RaiseEvent ContentChanged(Me, EventArgs.Empty)
        End If
    End Sub

    Private Sub OnCellFormatting(ByVal sender As Object, ByVal e As DataGridViewCellFormattingEventArgs)
        If e.RowIndex < 0 OrElse e.ColumnIndex < 0 Then Return
        Dim addr As String = SheetModel.AddressFrom(e.RowIndex, e.ColumnIndex)
        If addr = _editingAddress Then Return

        Dim raw As String = _model.GetRaw(addr)
        If raw.StartsWith("=", StringComparison.Ordinal) Then
            e.Value = _model.GetDisplay(addr)
            e.FormattingApplied = True
        End If
    End Sub

    Private Sub OnModelChanged(ByVal sender As Object, ByVal e As EventArgs)
        If _editingAddress <> "" Then Return
        RefreshGridFromModel()
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
