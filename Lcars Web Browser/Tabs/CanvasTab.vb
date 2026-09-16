Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms

''' <summary>
''' IBrowserTab host for a blank ink canvas with .lcarsink save and PNG export.
''' </summary>
Public Class CanvasTab
    Implements IBrowserTab

    Private ReadOnly _hostPanel As Panel
    Private ReadOnly _inkOverlay As InkOverlayControl

    Private _filePath As String = ""
    Private _dirty As Boolean = False

    Public Event ContentChanged As EventHandler

    Public Sub New()
        _hostPanel = New Panel()
        _hostPanel.Dock = DockStyle.Fill
        _hostPanel.BackColor = Color.Black

        _inkOverlay = New InkOverlayControl()
        _inkOverlay.Document.BackgroundColor = Color.Black
        _inkOverlay.PenColor = Color.FromArgb(255, 153, 0)
        _hostPanel.Controls.Add(_inkOverlay)

        AddHandler _inkOverlay.StrokeCompleted, AddressOf OnInkStrokeCompleted
        AddHandler _inkOverlay.Document.DocumentChanged, AddressOf OnInkDocumentChanged
    End Sub

    Public ReadOnly Property InkOverlay As InkOverlayControl
        Get
            Return _inkOverlay
        End Get
    End Property

    Public ReadOnly Property TabKind As String Implements IBrowserTab.TabKind
        Get
            Return "Canvas"
        End Get
    End Property

    Public ReadOnly Property Title As String Implements IBrowserTab.Title
        Get
            Dim name As String = If(String.IsNullOrEmpty(_filePath), "NEW CANVAS", System.IO.Path.GetFileName(_filePath))
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
            If ext = ".png" Then
                ExportPng(path)
            Else
                SaveInk(path)
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
        _inkOverlay.Focus()
    End Sub

    ''' <summary>
    ''' Clears strokes for a new untitled canvas.
    ''' </summary>
    Public Sub NewDocument()
        _filePath = ""
        _dirty = False
        _inkOverlay.Document.BackgroundColor = Color.Black
        _hostPanel.BackColor = Color.Black
        _inkOverlay.Document.Clear()
        RaiseEvent ContentChanged(Me, EventArgs.Empty)
    End Sub

    ''' <summary>
    ''' Saves editable ink strokes to a .lcarsink file.
    ''' </summary>
    Public Sub SaveInk(ByVal path As String)
        EnsureParentDirectory(path)
        SyncDocumentSize()
        _inkOverlay.Document.Save(path)
    End Sub

    ''' <summary>
    ''' Renders the canvas ink layer to a PNG file.
    ''' </summary>
    Public Sub ExportPng(ByVal path As String)
        EnsureParentDirectory(path)
        SyncDocumentSize()
        _inkOverlay.Document.ExportPng(path)
    End Sub

    Private Sub LoadFromFile(ByVal path As String)
        Try
            _filePath = path
            _inkOverlay.Document.Load(path)
            _hostPanel.BackColor = _inkOverlay.Document.BackgroundColor
            _dirty = False
            _inkOverlay.RefreshInkSurface()
            RaiseEvent ContentChanged(Me, EventArgs.Empty)
        Catch ex As Exception
            MessageBox.Show("Unable to open canvas:" & vbCrLf & ex.Message, "LCARS Web Browser", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub SyncDocumentSize()
        If _hostPanel.ClientSize.Width > 0 AndAlso _hostPanel.ClientSize.Height > 0 Then
            _inkOverlay.Document.CanvasWidth = _hostPanel.ClientSize.Width
            _inkOverlay.Document.CanvasHeight = _hostPanel.ClientSize.Height
        End If
    End Sub

    Private Sub OnInkStrokeCompleted(ByVal sender As Object, ByVal e As EventArgs)
        MarkDirty()
    End Sub

    Private Sub OnInkDocumentChanged(ByVal sender As Object, ByVal e As EventArgs)
        _hostPanel.BackColor = _inkOverlay.Document.BackgroundColor
    End Sub

    Private Sub MarkDirty()
        If _dirty Then Return
        _dirty = True
        RaiseEvent ContentChanged(Me, EventArgs.Empty)
    End Sub

    ''' <summary>
    ''' Marks ink edits (including clear) as unsaved changes.
    ''' </summary>
    Public Sub MarkInkModified()
        MarkDirty()
    End Sub

    Private Shared Sub EnsureParentDirectory(ByVal path As String)
        Dim parentDir As String = System.IO.Path.GetDirectoryName(path)
        If Not String.IsNullOrEmpty(parentDir) AndAlso Not Directory.Exists(parentDir) Then
            Directory.CreateDirectory(parentDir)
        End If
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
        If _inkOverlay IsNot Nothing Then
            _inkOverlay.Dispose()
        End If
        If _hostPanel IsNot Nothing Then
            _hostPanel.Dispose()
        End If
    End Sub
End Class
