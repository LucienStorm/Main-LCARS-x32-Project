' Lcars Web Browser/Tabs/BoardCanvasTab.vb
Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms

''' <summary>
''' Obsidian-style canvas board tab (replaces ink-only canvas as the product).
''' </summary>
Public Class BoardCanvasTab
    Implements IBrowserTab

    Private ReadOnly _host As Panel
    Private ReadOnly _tools As ToolStrip
    Private ReadOnly _model As BoardModel
    Private ReadOnly _viewport As BoardViewport
    Private _filePath As String = ""
    Private _dirty As Boolean = False

    Public Event ContentChanged As EventHandler

    Public Sub New()
        _host = New Panel() With {.Dock = DockStyle.Fill, .BackColor = Color.Black}
        _tools = New ToolStrip() With {
            .Dock = DockStyle.Top,
            .GripStyle = ToolStripGripStyle.Hidden,
            .BackColor = Color.Black,
            .ForeColor = Color.FromArgb(255, 153, 0)
        }

        Dim btnSelect As New ToolStripButton("SELECT") With {.CheckOnClick = True, .Checked = True}
        Dim btnPan As New ToolStripButton("PAN") With {.CheckOnClick = True}
        Dim btnNote As New ToolStripButton("ADD NOTE") With {.CheckOnClick = True}
        Dim btnLink As New ToolStripButton("ADD LINK") With {.CheckOnClick = True}
        Dim btnImage As New ToolStripButton("ADD IMAGE") With {.CheckOnClick = True}
        Dim btnFile As New ToolStripButton("ADD FILE") With {.CheckOnClick = True}
        Dim btnInk As New ToolStripButton("INK") With {.CheckOnClick = True}
        Dim btnConnect As New ToolStripButton("CONNECT") With {.CheckOnClick = True}
        Dim btnZoomIn As New ToolStripButton("ZOOM +")
        Dim btnZoomOut As New ToolStripButton("ZOOM -")
        Dim hint As New ToolStripLabel("  Wheel=zoom  Middle/Right-drag=pan  Double-click=edit/open  Del=delete")

        _tools.Items.AddRange(New ToolStripItem() {
            btnSelect, btnPan, btnNote, btnLink, btnImage, btnFile, btnInk, btnConnect, btnZoomIn, btnZoomOut, hint
        })

        _model = New BoardModel()
        _viewport = New BoardViewport(_model)
        _host.Controls.Add(_viewport)
        _host.Controls.Add(_tools)

        Dim all As ToolStripButton() = {btnSelect, btnPan, btnNote, btnLink, btnImage, btnFile, btnInk, btnConnect}
        AddHandler btnSelect.Click, Sub() SetTool(BoardViewport.ToolMode.SelectMove, btnSelect, all)
        AddHandler btnPan.Click, Sub() SetTool(BoardViewport.ToolMode.Pan, btnPan, all)
        AddHandler btnNote.Click, Sub() SetTool(BoardViewport.ToolMode.AddNote, btnNote, all)
        AddHandler btnLink.Click, Sub() SetTool(BoardViewport.ToolMode.AddLink, btnLink, all)
        AddHandler btnImage.Click, Sub() SetTool(BoardViewport.ToolMode.AddImage, btnImage, all)
        AddHandler btnFile.Click, Sub() SetTool(BoardViewport.ToolMode.AddFile, btnFile, all)
        AddHandler btnInk.Click, Sub() SetTool(BoardViewport.ToolMode.Ink, btnInk, all)
        AddHandler btnConnect.Click, Sub() SetTool(BoardViewport.ToolMode.Connect, btnConnect, all)
        AddHandler btnZoomIn.Click, Sub()
                                       _model.Zoom = Math.Min(3.5F, _model.Zoom * 1.15F)
                                       _viewport.Invalidate()
                                   End Sub
        AddHandler btnZoomOut.Click, Sub()
                                         _model.Zoom = Math.Max(0.25F, _model.Zoom / 1.15F)
                                         _viewport.Invalidate()
                                     End Sub
        AddHandler _viewport.ModelChanged, AddressOf OnBoardChanged
        AddHandler _model.Changed, AddressOf OnBoardChanged

        ' Seed one note so the board is obviously interactive.
        _model.AddNote(40, 40, "Double-click to edit")
        _dirty = False
    End Sub

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
            Return _host
        End Get
    End Property

    Public Function GetPathOrUrl() As String Implements IBrowserTab.GetPathOrUrl
        Return _filePath
    End Function

    Public Sub NavigateOrOpen(ByVal pathOrUrl As String) Implements IBrowserTab.NavigateOrOpen
        Dim path As String = If(pathOrUrl, "").Trim()
        If path = "" Then Return
        If path.StartsWith("file://", StringComparison.OrdinalIgnoreCase) Then
            Try
                path = New Uri(path).LocalPath
            Catch
            End Try
        End If
        If File.Exists(path) Then
            Dim ext As String = System.IO.Path.GetExtension(path).ToLowerInvariant()
            If ext = ".lcarscanvas" Then
                _model.Load(path)
                _filePath = path
                _dirty = False
                RaiseEvent ContentChanged(Me, EventArgs.Empty)
            ElseIf ext = ".lcarsink" Then
                MessageBox.Show("Legacy ink files open in the old ink surface is retired. Create a board and add notes/images instead.", "LCARS Canvas")
            End If
        Else
            _filePath = path
        End If
    End Sub

    Public Function Save(ByVal path As String) As Boolean Implements IBrowserTab.Save
        Try
            Dim ext As String = System.IO.Path.GetExtension(path).ToLowerInvariant()
            If ext = ".png" Then
                ExportPng(path)
            Else
                If String.IsNullOrEmpty(ext) Then path = path & ".lcarscanvas"
                _model.Save(path)
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
        _viewport.Focus()
    End Sub

    Public Sub NewDocument()
        _filePath = ""
        _model.Nodes.Clear()
        _model.Edges.Clear()
        _model.Strokes.Clear()
        _model.CameraX = 0
        _model.CameraY = 0
        _model.Zoom = 1.0F
        _model.AddNote(40, 40, "Double-click to edit")
        _dirty = False
        _viewport.Invalidate()
        RaiseEvent ContentChanged(Me, EventArgs.Empty)
    End Sub

    Public Sub Dispose()
        If _host IsNot Nothing Then _host.Dispose()
    End Sub

    Private Sub ExportPng(ByVal path As String)
        Using bmp As New Bitmap(Math.Max(1, _viewport.Width), Math.Max(1, _viewport.Height))
            _viewport.DrawToBitmap(bmp, New Rectangle(0, 0, bmp.Width, bmp.Height))
            bmp.Save(path, Imaging.ImageFormat.Png)
        End Using
    End Sub

    Private Sub OnBoardChanged(ByVal sender As Object, ByVal e As EventArgs)
        If Not _dirty Then
            _dirty = True
            RaiseEvent ContentChanged(Me, EventArgs.Empty)
        End If
    End Sub

    Private Sub SetTool(ByVal mode As BoardViewport.ToolMode, ByVal active As ToolStripButton, ByVal all() As ToolStripButton)
        _viewport.Tool = mode
        For Each b As ToolStripButton In all
            b.Checked = Object.ReferenceEquals(b, active)
        Next
    End Sub
End Class
