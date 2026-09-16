' Lcars Web Browser/Canvas/BoardViewport.vb
Option Strict On
Option Explicit On

Imports System.Diagnostics
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.IO
Imports System.Windows.Forms

''' <summary>
''' Pan/zoom canvas surface for BoardModel.
''' </summary>
Public Class BoardViewport
    Inherits Control

    Public Enum ToolMode
        SelectMove = 0
        Pan = 1
        AddNote = 2
        AddLink = 3
        Connect = 4
        AddImage = 5
        AddFile = 6
        Ink = 7
    End Enum

    Private ReadOnly _model As BoardModel
    Private _tool As ToolMode = ToolMode.SelectMove
    Private _selected As BoardNode
    Private _connectFrom As BoardNode
    Private _dragging As Boolean
    Private _panning As Boolean
    Private _inking As Boolean
    Private _activeStroke As BoardStroke
    Private _lastScreen As Point
    Private _dragOffset As PointF

    Public Event ModelChanged As EventHandler

    Public Sub New(ByVal model As BoardModel)
        _model = model
        DoubleBuffered = True
        BackColor = Color.Black
        Dock = DockStyle.Fill
        TabStop = True
        AddHandler _model.Changed, Sub() Invalidate()
    End Sub

    Public Property Tool As ToolMode
        Get
            Return _tool
        End Get
        Set(ByVal value As ToolMode)
            _tool = value
            _connectFrom = Nothing
            _inking = False
            _activeStroke = Nothing
        End Set
    End Property

    Public ReadOnly Property Model As BoardModel
        Get
            Return _model
        End Get
    End Property

    Public Function ScreenToWorld(ByVal screen As Point) As PointF
        Return New PointF((screen.X / _model.Zoom) + _model.CameraX, (screen.Y / _model.Zoom) + _model.CameraY)
    End Function

    Public Function WorldToScreen(ByVal world As PointF) As PointF
        Return New PointF((world.X - _model.CameraX) * _model.Zoom, (world.Y - _model.CameraY) * _model.Zoom)
    End Function

    Protected Overrides Sub OnPaint(ByVal e As PaintEventArgs)
        MyBase.OnPaint(e)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.Clear(Color.Black)

        ' Grid
        Using gridPen As New Pen(Color.FromArgb(40, 255, 153, 0), 1)
            Dim stepSize As Integer = CInt(80 * _model.Zoom)
            If stepSize < 20 Then stepSize = 20
            Dim ox As Integer = CInt((-_model.CameraX * _model.Zoom) Mod stepSize)
            Dim oy As Integer = CInt((-_model.CameraY * _model.Zoom) Mod stepSize)
            For x As Integer = ox To Width Step stepSize
                g.DrawLine(gridPen, x, 0, x, Height)
            Next
            For y As Integer = oy To Height Step stepSize
                g.DrawLine(gridPen, 0, y, Width, y)
            Next
        End Using

        For Each stroke As BoardStroke In _model.Strokes
            If stroke.Points.Count < 2 Then Continue For
            Using pen As New Pen(Color.FromArgb(stroke.ColorArgb), Math.Max(1.0F, stroke.Width * _model.Zoom))
                pen.StartCap = LineCap.Round
                pen.EndCap = LineCap.Round
                pen.LineJoin = LineJoin.Round
                Dim pts(stroke.Points.Count - 1) As PointF
                For i As Integer = 0 To stroke.Points.Count - 1
                    pts(i) = WorldToScreen(stroke.Points(i))
                Next
                g.DrawLines(pen, pts)
            End Using
        Next

        Using edgePen As New Pen(Color.FromArgb(255, 204, 102), 2)
            For Each edge As BoardEdge In _model.Edges
                Dim a As BoardNode = _model.FindNode(edge.FromId)
                Dim b As BoardNode = _model.FindNode(edge.ToId)
                If a Is Nothing OrElse b Is Nothing Then Continue For
                Dim pa As PointF = WorldToScreen(New PointF(a.X + a.Width / 2, a.Y + a.Height / 2))
                Dim pb As PointF = WorldToScreen(New PointF(b.X + b.Width / 2, b.Y + b.Height / 2))
                g.DrawLine(edgePen, pa, pb)
            Next
        End Using

        For Each n As BoardNode In _model.Nodes
            Dim tl As PointF = WorldToScreen(New PointF(n.X, n.Y))
            Dim size As New SizeF(n.Width * _model.Zoom, n.Height * _model.Zoom)
            Dim rect As New RectangleF(tl, size)
            Dim fill As Color = Color.FromArgb(80, 255, 153, 0)
            If n.Kind = "link" Then fill = Color.FromArgb(80, 100, 180, 255)
            If n.Kind = "image" Then fill = Color.FromArgb(80, 180, 80, 120)
            If n.Kind = "file" Then fill = Color.FromArgb(80, 120, 200, 140)
            If Object.ReferenceEquals(n, _selected) Then fill = Color.FromArgb(140, 255, 200, 80)
            Using brush As New SolidBrush(fill)
                g.FillRectangle(brush, rect)
            End Using
            If n.Kind = "image" AndAlso File.Exists(n.PathOrUrl) Then
                Try
                    Using img As Image = Image.FromFile(n.PathOrUrl)
                        g.DrawImage(img, rect)
                    End Using
                Catch
                End Try
            End If
            Using border As New Pen(Color.FromArgb(255, 153, 0), 2)
                g.DrawRectangle(border, rect.X, rect.Y, rect.Width, rect.Height)
            End Using
            If n.Kind <> "image" Then
                Using font As New Font("Segoe UI", Math.Max(8.0F, 10.0F * _model.Zoom))
                    Using textBrush As New SolidBrush(Color.FromArgb(255, 255, 204, 102))
                        Dim label As String = If(n.Text, "")
                        If n.Kind = "file" Then label = "FILE: " & label
                        Dim textRect As New RectangleF(rect.X + 6, rect.Y + 6, Math.Max(10, rect.Width - 12), Math.Max(10, rect.Height - 12))
                        g.DrawString(label, font, textBrush, textRect)
                    End Using
                End Using
            End If
        Next
    End Sub

    Protected Overrides Sub OnMouseDown(ByVal e As MouseEventArgs)
        MyBase.OnMouseDown(e)
        Focus()
        _lastScreen = e.Location
        Dim world As PointF = ScreenToWorld(e.Location)

        If e.Button = MouseButtons.Middle OrElse _tool = ToolMode.Pan OrElse (e.Button = MouseButtons.Right) Then
            _panning = True
            Capture = True
            Return
        End If

        If e.Button <> MouseButtons.Left Then Return

        Select Case _tool
            Case ToolMode.Ink
                _inking = True
                _activeStroke = _model.BeginStroke(world.X, world.Y)
                Capture = True
                Invalidate()
            Case ToolMode.AddNote
                _selected = _model.AddNote(world.X, world.Y)
                RaiseEvent ModelChanged(Me, EventArgs.Empty)
            Case ToolMode.AddLink
                Dim url As String = InputBox("URL or path:", "Canvas Link", "https://")
                If Not String.IsNullOrWhiteSpace(url) Then
                    _selected = _model.AddLink(world.X, world.Y, url.Trim())
                    RaiseEvent ModelChanged(Me, EventArgs.Empty)
                End If
            Case ToolMode.AddImage
                Using dlg As New OpenFileDialog()
                    dlg.Filter = "Images|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp|All files|*.*"
                    If dlg.ShowDialog(FindForm()) = DialogResult.OK Then
                        _selected = _model.AddImage(world.X, world.Y, dlg.FileName)
                        RaiseEvent ModelChanged(Me, EventArgs.Empty)
                    End If
                End Using
            Case ToolMode.AddFile
                Using dlg As New OpenFileDialog()
                    dlg.Filter = "All files|*.*|Documents|*.txt;*.md;*.rtf;*.docx;*.pdf|Sheets|*.csv;*.xlsx"
                    If dlg.ShowDialog(FindForm()) = DialogResult.OK Then
                        _selected = _model.AddFile(world.X, world.Y, dlg.FileName)
                        RaiseEvent ModelChanged(Me, EventArgs.Empty)
                    End If
                End Using
            Case ToolMode.Connect
                Dim hit As BoardNode = _model.HitTest(world.X, world.Y)
                If hit Is Nothing Then Return
                If _connectFrom Is Nothing Then
                    _connectFrom = hit
                    _selected = hit
                Else
                    _model.Connect(_connectFrom.Id, hit.Id)
                    _connectFrom = Nothing
                    RaiseEvent ModelChanged(Me, EventArgs.Empty)
                End If
                Invalidate()
            Case Else
                Dim hit As BoardNode = _model.HitTest(world.X, world.Y)
                _selected = hit
                If hit IsNot Nothing Then
                    _dragging = True
                    _dragOffset = New PointF(world.X - hit.X, world.Y - hit.Y)
                    Capture = True
                End If
                Invalidate()
        End Select
    End Sub

    Protected Overrides Sub OnMouseMove(ByVal e As MouseEventArgs)
        MyBase.OnMouseMove(e)
        If _panning Then
            Dim dx As Single = (e.X - _lastScreen.X) / _model.Zoom
            Dim dy As Single = (e.Y - _lastScreen.Y) / _model.Zoom
            _model.CameraX -= dx
            _model.CameraY -= dy
            _lastScreen = e.Location
            Invalidate()
            Return
        End If

        If _inking AndAlso _activeStroke IsNot Nothing Then
            Dim world As PointF = ScreenToWorld(e.Location)
            _model.ContinueStroke(_activeStroke, world.X, world.Y)
            Invalidate()
            Return
        End If

        If _dragging AndAlso _selected IsNot Nothing Then
            Dim world As PointF = ScreenToWorld(e.Location)
            _selected.X = world.X - _dragOffset.X
            _selected.Y = world.Y - _dragOffset.Y
            Invalidate()
            RaiseEvent ModelChanged(Me, EventArgs.Empty)
        End If
    End Sub

    Protected Overrides Sub OnMouseUp(ByVal e As MouseEventArgs)
        MyBase.OnMouseUp(e)
        If _inking Then
            _inking = False
            If _activeStroke IsNot Nothing AndAlso _activeStroke.Points.Count >= 2 Then
                RaiseEvent ModelChanged(Me, EventArgs.Empty)
                _model.NotifyChanged()
            ElseIf _activeStroke IsNot Nothing Then
                _model.Strokes.Remove(_activeStroke)
            End If
            _activeStroke = Nothing
        End If
        _panning = False
        _dragging = False
        Capture = False
        If _selected IsNot Nothing AndAlso Not _inking Then _model.NotifyChanged()
    End Sub

    Protected Overrides Sub OnMouseWheel(ByVal e As MouseEventArgs)
        MyBase.OnMouseWheel(e)
        Dim before As PointF = ScreenToWorld(e.Location)
        Dim factor As Single = If(e.Delta > 0, 1.1F, 0.9F)
        Dim z As Single = _model.Zoom * factor
        If z < 0.25F Then z = 0.25F
        If z > 3.5F Then z = 3.5F
        _model.Zoom = z
        Dim after As PointF = ScreenToWorld(e.Location)
        _model.CameraX += before.X - after.X
        _model.CameraY += before.Y - after.Y
        Invalidate()
    End Sub

    Protected Overrides Sub OnMouseDoubleClick(ByVal e As MouseEventArgs)
        MyBase.OnMouseDoubleClick(e)
        Dim world As PointF = ScreenToWorld(e.Location)
        Dim hit As BoardNode = _model.HitTest(world.X, world.Y)
        If hit Is Nothing Then Return
        If hit.Kind = "file" AndAlso Not String.IsNullOrEmpty(hit.PathOrUrl) AndAlso File.Exists(hit.PathOrUrl) Then
            Try
                Process.Start(New ProcessStartInfo() With {.FileName = hit.PathOrUrl, .UseShellExecute = True})
            Catch
            End Try
            Return
        End If
        Dim edited As String = InputBox("Edit node text:", "Canvas", hit.Text)
        If edited IsNot Nothing Then
            hit.Text = edited
            If hit.Kind = "link" Then hit.PathOrUrl = edited
            _model.NotifyChanged()
            RaiseEvent ModelChanged(Me, EventArgs.Empty)
        End If
    End Sub

    Protected Overrides Function IsInputKey(ByVal keyData As Keys) As Boolean
        If keyData = Keys.Delete Then Return True
        Return MyBase.IsInputKey(keyData)
    End Function

    Protected Overrides Sub OnKeyDown(ByVal e As KeyEventArgs)
        MyBase.OnKeyDown(e)
        If e.KeyCode = Keys.Delete AndAlso _selected IsNot Nothing Then
            Dim id As String = _selected.Id
            _model.Nodes.Remove(_selected)
            _model.Edges.RemoveAll(Function(ed) ed.FromId = id OrElse ed.ToId = id)
            _selected = Nothing
            _model.NotifyChanged()
            RaiseEvent ModelChanged(Me, EventArgs.Empty)
        End If
    End Sub
End Class
