' Lcars Web Browser/Canvas/BoardViewport.vb
' Obsidian-style pan/zoom board: groups, colors, labeled arrows, resize, multi-select.
Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.IO
Imports System.Windows.Forms

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
        AddGroup = 8
    End Enum

    Private Enum ResizeHandle
        None = 0
        SE = 1
        E = 2
        S = 3
    End Enum

    Private ReadOnly _model As BoardModel
    Private _history As BoardHistory
    Private _tool As ToolMode = ToolMode.SelectMove
    Private ReadOnly _selection As New List(Of BoardNode)
    Private _selectedEdge As BoardEdge
    Private _connectFrom As BoardNode
    Private _dragging As Boolean
    Private _panning As Boolean
    Private _inking As Boolean
    Private _boxSelecting As Boolean
    Private _resizing As Boolean
    Private _resizeHandle As ResizeHandle = ResizeHandle.None
    Private _activeStroke As BoardStroke
    Private _lastScreen As Point
    Private _dragStartWorld As PointF
    Private _boxStart As PointF
    Private _boxCurrent As PointF
    Private _inkColorArgb As Integer = Color.FromArgb(255, 255, 153, 0).ToArgb()
    Private _dragCaptured As Boolean
    Private _clipboardJson As String = ""
    Private _inlineHost As Panel
    Private _inlineLineMargin As LineNumberMargin
    Private _inlineEdit As RichTextBox
    Private _inlineNode As BoardNode
    Private _inlineCancel As Boolean
    Private _inlineCommitting As Boolean
    Private _inlineSingleLine As Boolean
    Private _suppressLostFocusCommit As Boolean
    Private _pressScreen As Point
    Private _pressNode As BoardNode
    Private _pressEdge As BoardEdge
    Private _awaitingSecondTap As Boolean
    Private _dragArmed As Boolean
    Private _dragMoved As Boolean
    Private Const DragThresholdPx As Integer = 8

    Public Event ModelChanged As EventHandler
    Public Event OpenUrlRequested As EventHandler(Of String)

    Public Sub New(ByVal model As BoardModel)
        _model = model
        _history = New BoardHistory(model)
        DoubleBuffered = True
        BackColor = Color.Black
        Dock = DockStyle.Fill
        TabStop = True
        AddHandler _model.Changed, Sub()
                                       LayoutInlineEditor()
                                       Invalidate()
                                   End Sub
    End Sub

    Public ReadOnly Property History As BoardHistory
        Get
            Return _history
        End Get
    End Property

    Private Sub CaptureHistory()
        If _history IsNot Nothing Then _history.CaptureBeforeChange()
    End Sub

    Public Property Tool As ToolMode
        Get
            Return _tool
        End Get
        Set(ByVal value As ToolMode)
            CommitInlineEdit(True)
            _tool = value
            _connectFrom = Nothing
            _inking = False
            _activeStroke = Nothing
            _selectedEdge = Nothing
        End Set
    End Property

    Public Property InkColorArgb As Integer
        Get
            Return _inkColorArgb
        End Get
        Set(ByVal value As Integer)
            _inkColorArgb = value
        End Set
    End Property

    Public ReadOnly Property Model As BoardModel
        Get
            Return _model
        End Get
    End Property

    Public ReadOnly Property SelectedNodes As IList(Of BoardNode)
        Get
            Return _selection
        End Get
    End Property

    Public ReadOnly Property SelectedEdge As BoardEdge
        Get
            Return _selectedEdge
        End Get
    End Property

    Public Function PrimarySelection() As BoardNode
        If _selection.Count = 0 Then Return Nothing
        Return _selection(_selection.Count - 1)
    End Function

    Public Sub SelectOnly(ByVal node As BoardNode)
        _selection.Clear()
        _selectedEdge = Nothing
        If node IsNot Nothing Then _selection.Add(node)
        Invalidate()
    End Sub

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
        g.TextRenderingHint = Drawing.Text.TextRenderingHint.ClearTypeGridFit
        g.Clear(Color.Black)
        DrawGrid(g)
        DrawStrokes(g)
        DrawEdges(g)
        DrawNodes(g)
        If _boxSelecting Then DrawBoxSelect(g)
        If _connectFrom IsNot Nothing Then
            Dim tl As PointF = WorldToScreen(New PointF(_connectFrom.X, _connectFrom.Y))
            Using pen As New Pen(Color.White, 2) With {.DashStyle = DashStyle.Dash}
                g.DrawRectangle(pen, tl.X, tl.Y, _connectFrom.Width * _model.Zoom, _connectFrom.Height * _model.Zoom)
            End Using
        End If
    End Sub

    Private Sub DrawGrid(ByVal g As Graphics)
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
    End Sub

    Private Sub DrawStrokes(ByVal g As Graphics)
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
    End Sub

    Private Sub DrawEdges(ByVal g As Graphics)
        For Each edge As BoardEdge In _model.Edges
            Dim a As BoardNode = _model.FindNode(edge.FromId)
            Dim b As BoardNode = _model.FindNode(edge.ToId)
            If a Is Nothing OrElse b Is Nothing Then Continue For
            Dim p1w As PointF = a.AnchorPoint(edge.FromSide)
            Dim p2w As PointF = b.AnchorPoint(edge.ToSide)
            Dim p1 As PointF = WorldToScreen(p1w)
            Dim p2 As PointF = WorldToScreen(p2w)
            Dim border As Color = CanvasColors.Resolve(edge.Color)
            Dim width As Single = If(Object.ReferenceEquals(edge, _selectedEdge), 3.5F, 2.0F)
            Using pen As New Pen(border, width)
                If String.Equals(edge.ToEnd, "arrow", StringComparison.OrdinalIgnoreCase) Then
                    pen.CustomEndCap = New AdjustableArrowCap(5, 7, True)
                Else
                    pen.EndCap = LineCap.Round
                End If
                If String.Equals(edge.FromEnd, "arrow", StringComparison.OrdinalIgnoreCase) Then
                    pen.CustomStartCap = New AdjustableArrowCap(5, 7, True)
                Else
                    pen.StartCap = LineCap.Round
                End If
                g.DrawLine(pen, p1, p2)
            End Using
            If Not String.IsNullOrWhiteSpace(edge.Label) Then
                Dim mid As New PointF((p1.X + p2.X) / 2.0F, (p1.Y + p2.Y) / 2.0F)
                Using font As New Font("Segoe UI", Math.Max(8.0F, 9.0F * _model.Zoom), FontStyle.Bold)
                    Dim sz As SizeF = g.MeasureString(edge.Label, font)
                    Dim labelRect As New RectangleF(mid.X - sz.Width / 2.0F - 4, mid.Y - sz.Height / 2.0F - 2, sz.Width + 8, sz.Height + 4)
                    Using bg As New SolidBrush(Color.FromArgb(200, 0, 0, 0))
                        g.FillRectangle(bg, labelRect)
                    End Using
                    Using textBrush As New SolidBrush(border)
                        g.DrawString(edge.Label, font, textBrush, labelRect.X + 4, labelRect.Y + 2)
                    End Using
                End Using
            End If
        Next
    End Sub

    Private Sub DrawNodes(ByVal g As Graphics)
        For Each n As BoardNode In _model.Nodes
            Dim tl As PointF = WorldToScreen(New PointF(n.X, n.Y))
            Dim size As New SizeF(n.Width * _model.Zoom, n.Height * _model.Zoom)
            Dim rect As New RectangleF(tl, size)
            Dim border As Color = CanvasColors.Resolve(n.Color)
            Dim selected As Boolean = _selection.Contains(n)
            Dim fillAlpha As Integer = If(n.IsGroup, 35, 90)
            If selected Then fillAlpha = Math.Min(180, fillAlpha + 50)
            Dim fill As Color = CanvasColors.FillFromBorder(border, fillAlpha)

            Using brush As New SolidBrush(fill)
                If n.IsGroup Then
                    g.FillRectangle(brush, rect)
                Else
                    Using path As GraphicsPath = RoundedRect(rect, 6.0F * _model.Zoom)
                        g.FillPath(brush, path)
                    End Using
                End If
            End Using

            If n.Kind = "image" AndAlso File.Exists(n.PathOrUrl) Then
                Try
                    Using img As Image = Image.FromFile(n.PathOrUrl)
                        Dim imgRect As New RectangleF(rect.X + 4, rect.Y + 4, Math.Max(4, rect.Width - 8), Math.Max(4, rect.Height - 8))
                        g.DrawImage(img, imgRect)
                    End Using
                Catch
                End Try
            End If

            Dim borderWidth As Single = If(n.IsGroup, 3.0F, 2.0F) * Math.Max(0.75F, _model.Zoom)
            If selected Then borderWidth += 1.5F
            Using pen As New Pen(border, borderWidth)
                If n.IsGroup Then
                    g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height)
                Else
                    Using path As GraphicsPath = RoundedRect(rect, 6.0F * _model.Zoom)
                        g.DrawPath(pen, path)
                    End Using
                End If
            End Using

            Dim title As String = NodeTitle(n)
            If Not String.IsNullOrEmpty(title) AndAlso Not Object.ReferenceEquals(n, _inlineNode) Then
                Dim textColor As Color = Color.FromArgb(255, 255, 220, 160)
                Dim accent As Color = Color.FromArgb(255, 255, 153, 0)
                Dim textRect As RectangleF
                If n.IsGroup Then
                    textRect = New RectangleF(rect.X + 8, rect.Y + 6, Math.Max(10, rect.Width - 16), Math.Max(14, 22 * _model.Zoom))
                    Using font As New Font("Segoe UI", Math.Max(8.0F, 11.0F * _model.Zoom), FontStyle.Bold)
                        Using textBrush As New SolidBrush(textColor)
                            DrawLeftAlignedText(g, title, font, textBrush, textRect, showLineNumbers:=False)
                        End Using
                    End Using
                ElseIf n.Kind = "image" Then
                    textRect = New RectangleF(rect.X + 6, rect.Bottom - 22 * _model.Zoom, Math.Max(10, rect.Width - 12), 18 * _model.Zoom)
                    Using font As New Font("Segoe UI", Math.Max(8.0F, 10.0F * _model.Zoom), FontStyle.Regular)
                        Using textBrush As New SolidBrush(textColor)
                            DrawLeftAlignedText(g, title, font, textBrush, textRect, showLineNumbers:=False)
                        End Using
                    End Using
                ElseIf String.Equals(n.Kind, "note", StringComparison.OrdinalIgnoreCase) Then
                    ' Idle note cards show rendered Markdown; edit mode keeps the source editor.
                    textRect = New RectangleF(rect.X + 8, rect.Y + 8, Math.Max(10, rect.Width - 16), Math.Max(10, rect.Height - 16))
                    Dim state As GraphicsState = g.Save()
                    Try
                        g.SetClip(textRect, CombineMode.Intersect)
                        CanvasMarkdownPainter.Draw(g, If(n.Text, ""), textRect, _model.Zoom, textColor, accent)
                    Finally
                        g.Restore(state)
                    End Try
                Else
                    textRect = New RectangleF(rect.X + 8, rect.Y + 8, Math.Max(10, rect.Width - 16), Math.Max(10, rect.Height - 16))
                    Using font As New Font("Segoe UI", Math.Max(8.0F, 10.0F * _model.Zoom), FontStyle.Regular)
                        Using textBrush As New SolidBrush(textColor)
                            DrawLeftAlignedText(g, title, font, textBrush, textRect, showLineNumbers:=False)
                        End Using
                    End Using
                End If
            End If

            If selected AndAlso Not n.IsGroup Then
                DrawResizeHandles(g, rect)
            End If
        Next
    End Sub

    Private Shared Function NodeTitle(ByVal n As BoardNode) As String
        If n.IsGroup Then
            Dim lbl As String = If(Not String.IsNullOrEmpty(n.Label), n.Label, n.Text)
            Return If(String.IsNullOrEmpty(lbl), "Group", lbl)
        End If
        If n.Kind = "file" Then Return "FILE: " & If(n.Text, "")
        If n.Kind = "link" Then Return "LINK: " & If(n.Text, "")
        If n.Kind = "image" Then Return If(n.Text, "")
        Return If(n.Text, "")
    End Function

    ''' <summary>Draws note/markdown body left-justified, optionally with a line-number gutter.</summary>
    Private Shared Sub DrawLeftAlignedText(ByVal g As Graphics, ByVal text As String, ByVal font As Font, ByVal textBrush As Brush, ByVal bounds As RectangleF, ByVal showLineNumbers As Boolean)
        If String.IsNullOrEmpty(text) OrElse bounds.Width < 4 OrElse bounds.Height < 4 Then Return

        Dim gutter As Single = 0.0F
        If showLineNumbers Then
            gutter = Math.Max(18.0F, Math.Min(bounds.Width * 0.28F, font.Size * 2.6F))
            Dim normalized As String = text.Replace(vbCrLf, vbLf).Replace(vbCr, vbLf)
            Dim lines() As String = normalized.Split(ControlChars.Lf)
            Dim lineHeight As Single = font.GetHeight(g)
            Using numBrush As New SolidBrush(Color.FromArgb(170, 255, 153, 0))
                Using numFormat As New StringFormat() With {
                    .Alignment = StringAlignment.Far,
                    .LineAlignment = StringAlignment.Near,
                    .FormatFlags = StringFormatFlags.NoWrap
                }
                    Dim y As Single = bounds.Y
                    Dim maxLines As Integer = Math.Max(1, CInt(Math.Floor(bounds.Height / Math.Max(1.0F, lineHeight))))
                    Dim count As Integer = Math.Min(lines.Length, maxLines)
                    For i As Integer = 0 To count - 1
                        Dim numRect As New RectangleF(bounds.X, y, gutter - 4.0F, lineHeight)
                        g.DrawString((i + 1).ToString(), font, numBrush, numRect, numFormat)
                        y += lineHeight
                        If y > bounds.Bottom Then Exit For
                    Next
                End Using
            End Using
        End If

        Dim content As New RectangleF(bounds.X + gutter, bounds.Y, Math.Max(4.0F, bounds.Width - gutter), bounds.Height)
        Using sf As New StringFormat(StringFormatFlags.LineLimit) With {
            .Alignment = StringAlignment.Near,
            .LineAlignment = StringAlignment.Near,
            .Trimming = StringTrimming.EllipsisCharacter
        }
            g.DrawString(text, font, textBrush, content, sf)
        End Using
    End Sub

    Private Sub DrawResizeHandles(ByVal g As Graphics, ByVal rect As RectangleF)
        Dim hs As Single = 8.0F
        Dim gripPts() As PointF = {
            New PointF(rect.Right - hs / 2, rect.Bottom - hs / 2),
            New PointF(rect.Right - hs / 2, rect.Top + rect.Height / 2 - hs / 2),
            New PointF(rect.Left + rect.Width / 2 - hs / 2, rect.Bottom - hs / 2)
        }
        Using brush As New SolidBrush(Color.FromArgb(255, 255, 200, 80))
            For Each gp As PointF In gripPts
                g.FillRectangle(brush, gp.X, gp.Y, hs, hs)
            Next
        End Using
    End Sub

    Private Sub DrawBoxSelect(ByVal g As Graphics)
        Dim a As PointF = WorldToScreen(_boxStart)
        Dim b As PointF = WorldToScreen(_boxCurrent)
        Dim r As New RectangleF(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y))
        Using brush As New SolidBrush(Color.FromArgb(40, 100, 180, 255))
            g.FillRectangle(brush, r)
        End Using
        Using pen As New Pen(Color.FromArgb(200, 100, 180, 255), 1) With {.DashStyle = DashStyle.Dot}
            g.DrawRectangle(pen, r.X, r.Y, r.Width, r.Height)
        End Using
    End Sub

    Private Shared Function RoundedRect(ByVal bounds As RectangleF, ByVal radius As Single) As GraphicsPath
        Dim path As New GraphicsPath()
        If radius < 1 Then
            path.AddRectangle(bounds)
            Return path
        End If
        Dim d As Single = radius * 2
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90)
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90)
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90)
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90)
        path.CloseFigure()
        Return path
    End Function

    Private Function HitResizeHandle(ByVal screen As Point, ByVal node As BoardNode) As ResizeHandle
        If node Is Nothing OrElse node.IsGroup Then Return ResizeHandle.None
        Dim tl As PointF = WorldToScreen(New PointF(node.X, node.Y))
        Dim rect As New RectangleF(tl, New SizeF(node.Width * _model.Zoom, node.Height * _model.Zoom))
        Dim hs As Single = 10.0F
        Dim se As New RectangleF(rect.Right - hs, rect.Bottom - hs, hs * 1.5F, hs * 1.5F)
        Dim ee As New RectangleF(rect.Right - hs, rect.Top + rect.Height / 2 - hs, hs * 1.5F, hs * 1.5F)
        Dim ss As New RectangleF(rect.Left + rect.Width / 2 - hs, rect.Bottom - hs, hs * 1.5F, hs * 1.5F)
        If se.Contains(screen) Then Return ResizeHandle.SE
        If ee.Contains(screen) Then Return ResizeHandle.E
        If ss.Contains(screen) Then Return ResizeHandle.S
        Return ResizeHandle.None
    End Function

    Protected Overrides Sub OnMouseDown(ByVal e As MouseEventArgs)
        MyBase.OnMouseDown(e)
        Focus()
        _lastScreen = e.Location
        Dim world As PointF = ScreenToWorld(e.Location)
        _dragStartWorld = world

        ' Click outside the in-place editor commits (Obsidian-style).
        If _inlineHost IsNot Nothing AndAlso _inlineHost.Visible Then
            If Not _inlineHost.Bounds.Contains(e.Location) Then
                CommitInlineEdit(True)
            Else
                Return
            End If
        End If

        If e.Button = MouseButtons.Middle OrElse _tool = ToolMode.Pan OrElse e.Button = MouseButtons.Right Then
            _panning = True
            Capture = True
            Return
        End If
        If e.Button <> MouseButtons.Left Then Return

        Select Case _tool
            Case ToolMode.Ink
                CaptureHistory()
                _inking = True
                _activeStroke = _model.BeginStroke(world.X, world.Y, _inkColorArgb)
                Capture = True
                Invalidate()
            Case ToolMode.AddNote
                CaptureHistory()
                SelectOnly(_model.AddNote(world.X, world.Y))
                RaiseEvent ModelChanged(Me, EventArgs.Empty)
            Case ToolMode.AddGroup
                CaptureHistory()
                SelectOnly(_model.AddGroup(world.X, world.Y))
                RaiseEvent ModelChanged(Me, EventArgs.Empty)
            Case ToolMode.AddLink
                Dim url As String = InputBox("URL:", "Canvas Link", "https://")
                If Not String.IsNullOrWhiteSpace(url) Then
                    CaptureHistory()
                    SelectOnly(_model.AddLink(world.X, world.Y, url.Trim()))
                    RaiseEvent ModelChanged(Me, EventArgs.Empty)
                End If
            Case ToolMode.AddImage
                Using dlg As New OpenFileDialog()
                    dlg.Filter = "Images|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp|All files|*.*"
                    If dlg.ShowDialog(FindForm()) = DialogResult.OK Then
                        CaptureHistory()
                        SelectOnly(_model.AddImage(world.X, world.Y, dlg.FileName))
                        RaiseEvent ModelChanged(Me, EventArgs.Empty)
                    End If
                End Using
            Case ToolMode.AddFile
                Using dlg As New OpenFileDialog()
                    dlg.Filter = "All files|*.*|Documents|*.txt;*.md;*.rtf;*.docx;*.pdf"
                    If dlg.ShowDialog(FindForm()) = DialogResult.OK Then
                        CaptureHistory()
                        SelectOnly(_model.AddFile(world.X, world.Y, dlg.FileName))
                        RaiseEvent ModelChanged(Me, EventArgs.Empty)
                    End If
                End Using
            Case ToolMode.Connect
                Dim hit As BoardNode = _model.HitTest(world.X, world.Y, includeGroups:=False)
                If hit Is Nothing Then Return
                If _connectFrom Is Nothing Then
                    _connectFrom = hit
                    SelectOnly(hit)
                Else
                    CaptureHistory()
                    Dim edge = _model.Connect(_connectFrom.Id, hit.Id)
                    _connectFrom = Nothing
                    _selectedEdge = edge
                    RaiseEvent ModelChanged(Me, EventArgs.Empty)
                End If
                Invalidate()
            Case Else
                _pressScreen = e.Location
                _pressNode = Nothing
                _pressEdge = Nothing
                _awaitingSecondTap = False
                _dragArmed = False
                _dragMoved = False
                _dragging = False

                Dim primary As BoardNode = PrimarySelection()
                Dim rh As ResizeHandle = HitResizeHandle(e.Location, primary)
                If rh <> ResizeHandle.None AndAlso primary IsNot Nothing Then
                    CaptureHistory()
                    _resizing = True
                    _resizeHandle = rh
                    Capture = True
                    Return
                End If

                Dim hitNode As BoardNode = _model.HitTest(world.X, world.Y)
                If hitNode IsNot Nothing Then
                    _selectedEdge = Nothing
                    If (ModifierKeys And Keys.Shift) = Keys.Shift Then
                        If _selection.Contains(hitNode) Then
                            _selection.Remove(hitNode)
                        Else
                            _selection.Add(hitNode)
                        End If
                        _pressNode = hitNode
                        _dragArmed = _selection.Count > 0
                        Capture = _dragArmed
                    ElseIf _selection.Count = 1 AndAlso Object.ReferenceEquals(PrimarySelection(), hitNode) Then
                        ' Already selected — second tap edits unless the pointer moves enough to drag.
                        _awaitingSecondTap = True
                        _pressNode = hitNode
                        _dragArmed = True
                        Capture = True
                    Else
                        SelectOnly(hitNode)
                        _pressNode = hitNode
                        _dragArmed = True
                        Capture = True
                    End If
                    Invalidate()
                    Return
                End If

                Dim hitEdge As BoardEdge = _model.HitTestEdge(world.X, world.Y, 10.0F / _model.Zoom)
                If hitEdge IsNot Nothing Then
                    _selection.Clear()
                    If Object.ReferenceEquals(_selectedEdge, hitEdge) Then
                        _awaitingSecondTap = True
                        _pressEdge = hitEdge
                    Else
                        _selectedEdge = hitEdge
                    End If
                    Invalidate()
                    Return
                End If

                If (ModifierKeys And Keys.Shift) <> Keys.Shift Then
                    _selection.Clear()
                    _selectedEdge = Nothing
                End If
                _boxSelecting = True
                _boxStart = world
                _boxCurrent = world
                Capture = True
                Invalidate()
        End Select
    End Sub

    Protected Overrides Sub OnMouseMove(ByVal e As MouseEventArgs)
        MyBase.OnMouseMove(e)
        Dim world As PointF = ScreenToWorld(e.Location)

        If _panning Then
            Dim dx As Single = (e.X - _lastScreen.X) / _model.Zoom
            Dim dy As Single = (e.Y - _lastScreen.Y) / _model.Zoom
            _model.CameraX -= dx
            _model.CameraY -= dy
            _lastScreen = e.Location
            LayoutInlineEditor()
            Invalidate()
            Return
        End If

        If _inking AndAlso _activeStroke IsNot Nothing Then
            _model.ContinueStroke(_activeStroke, world.X, world.Y)
            Invalidate()
            Return
        End If

        If _resizing AndAlso PrimarySelection() IsNot Nothing Then
            Dim n As BoardNode = PrimarySelection()
            If _resizeHandle = ResizeHandle.SE OrElse _resizeHandle = ResizeHandle.E Then
                n.Width = Math.Max(60.0F, world.X - n.X)
            End If
            If _resizeHandle = ResizeHandle.SE OrElse _resizeHandle = ResizeHandle.S Then
                n.Height = Math.Max(40.0F, world.Y - n.Y)
            End If
            Invalidate()
            RaiseEvent ModelChanged(Me, EventArgs.Empty)
            Return
        End If

        If _boxSelecting Then
            _boxCurrent = world
            Invalidate()
            Return
        End If

        If _dragArmed AndAlso Not _dragging AndAlso _selection.Count > 0 Then
            Dim sdx As Integer = e.X - _pressScreen.X
            Dim sdy As Integer = e.Y - _pressScreen.Y
            If (sdx * sdx + sdy * sdy) >= (DragThresholdPx * DragThresholdPx) Then
                _awaitingSecondTap = False
                _dragging = True
                _dragMoved = True
                _dragCaptured = True
                CaptureHistory()
                _dragStartWorld = world
            End If
        End If

        If _dragging AndAlso _selection.Count > 0 Then
            Dim dx As Single = world.X - _dragStartWorld.X
            Dim dy As Single = world.Y - _dragStartWorld.Y
            _dragStartWorld = world
            Dim movedGroups As New List(Of BoardNode)
            For Each n As BoardNode In _selection
                n.X += dx
                n.Y += dy
                If n.IsGroup Then movedGroups.Add(n)
            Next
            For Each grp As BoardNode In movedGroups
                For Each child As BoardNode In _model.NodesInsideGroup(grp)
                    If _selection.Contains(child) Then Continue For
                    child.X += dx
                    child.Y += dy
                Next
            Next
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
        If _boxSelecting Then
            _boxSelecting = False
            Dim left As Single = Math.Min(_boxStart.X, _boxCurrent.X)
            Dim top As Single = Math.Min(_boxStart.Y, _boxCurrent.Y)
            Dim right As Single = Math.Max(_boxStart.X, _boxCurrent.X)
            Dim bottom As Single = Math.Max(_boxStart.Y, _boxCurrent.Y)
            If (ModifierKeys And Keys.Shift) <> Keys.Shift Then _selection.Clear()
            For Each n As BoardNode In _model.Nodes
                Dim cx As Single = n.X + n.Width / 2.0F
                Dim cy As Single = n.Y + n.Height / 2.0F
                If cx >= left AndAlso cx <= right AndAlso cy >= top AndAlso cy <= bottom Then
                    If Not _selection.Contains(n) Then _selection.Add(n)
                End If
            Next
        End If
        If _resizing Then
            _resizing = False
            _resizeHandle = ResizeHandle.None
            _model.NotifyChanged()
        End If

        Dim doSecondTap As Boolean = _awaitingSecondTap AndAlso Not _dragMoved
        Dim tapNode As BoardNode = _pressNode
        Dim tapEdge As BoardEdge = _pressEdge

        _panning = False
        _dragging = False
        _dragArmed = False
        _dragMoved = False
        _awaitingSecondTap = False
        _pressNode = Nothing
        _pressEdge = Nothing
        Capture = False

        If doSecondTap Then
            If tapNode IsNot Nothing Then
                ActivateSelectedCard(tapNode)
            ElseIf tapEdge IsNot Nothing Then
                BeginInlineEdgeEdit(tapEdge)
            End If
        End If
        Invalidate()
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
        LayoutInlineEditor()
        Invalidate()
    End Sub

    ''' <summary>Edit is second-tap-on-selected, not double-click.</summary>
    Protected Overrides Sub OnMouseDoubleClick(ByVal e As MouseEventArgs)
        MyBase.OnMouseDoubleClick(e)
    End Sub

    ''' <summary>Second tap: edit notes/groups; open files/links.</summary>
    Private Sub ActivateSelectedCard(ByVal hit As BoardNode)
        If hit Is Nothing Then Return
        If hit.Kind = "file" AndAlso Not String.IsNullOrEmpty(hit.PathOrUrl) AndAlso File.Exists(hit.PathOrUrl) Then
            Try
                Process.Start(New ProcessStartInfo() With {.FileName = hit.PathOrUrl, .UseShellExecute = True})
            Catch
            End Try
            Return
        End If
        If hit.Kind = "link" AndAlso Not String.IsNullOrEmpty(hit.PathOrUrl) Then
            RaiseEvent OpenUrlRequested(Me, hit.PathOrUrl)
            Return
        End If
        If hit.Kind = "image" Then Return
        ' Defer off the mouse-up stack — editing during MouseUp/capture is a crash source.
        Dim nodeRef As BoardNode = hit
        If IsHandleCreated Then
            BeginInvoke(New MethodInvoker(Sub() BeginInlineNodeEdit(nodeRef)))
        Else
            BeginInlineNodeEdit(nodeRef)
        End If
    End Sub

    Private Sub EnsureInlineEditControl()
        If _inlineHost IsNot Nothing AndAlso Not _inlineHost.IsDisposed Then Return

        _inlineHost = New Panel() With {
            .Visible = False,
            .BackColor = Color.FromArgb(28, 22, 12)
        }
        _inlineLineMargin = New LineNumberMargin() With {
            .Dock = DockStyle.Left,
            .Width = 32,
            .Visible = False,
            .BackColor = Color.FromArgb(18, 14, 8),
            .ForeColor = Color.FromArgb(255, 255, 153, 0)
        }
        _inlineEdit = New RichTextBox() With {
            .Dock = DockStyle.Fill,
            .BorderStyle = BorderStyle.None,
            .ScrollBars = RichTextBoxScrollBars.None,
            .WordWrap = True,
            .AcceptsTab = True,
            .DetectUrls = False,
            .HideSelection = False,
            .Visible = True,
            .BackColor = Color.FromArgb(28, 22, 12),
            .ForeColor = Color.FromArgb(255, 255, 220, 160)
        }
        Try
            _inlineEdit.Font = New Font("Segoe UI", 10.0F)
        Catch
            _inlineEdit.Font = SystemFonts.DefaultFont
        End Try
        _inlineLineMargin.Editor = _inlineEdit
        _inlineHost.Controls.Add(_inlineEdit)
        _inlineHost.Controls.Add(_inlineLineMargin)
        Controls.Add(_inlineHost)
        _inlineHost.BringToFront()
        AddHandler _inlineEdit.KeyDown, AddressOf InlineEdit_KeyDown
        AddHandler _inlineEdit.LostFocus, AddressOf InlineEdit_LostFocus
        AddHandler _inlineEdit.TextChanged, AddressOf InlineEdit_TextChanged
        AddHandler _inlineEdit.ContentsResized, AddressOf InlineEdit_ContentsResized
    End Sub

    Private Sub InlineEdit_TextChanged(ByVal sender As Object, ByVal e As EventArgs)
        UpdateInlineScrollBars()
        If _inlineLineMargin IsNot Nothing AndAlso _inlineLineMargin.Visible Then
            _inlineLineMargin.Invalidate()
        End If
    End Sub

    Private Sub InlineEdit_ContentsResized(ByVal sender As Object, ByVal e As ContentsResizedEventArgs)
        UpdateInlineScrollBars()
    End Sub

    ''' <summary>Show a vertical scrollbar only when the note body actually overflows.</summary>
    Private Sub UpdateInlineScrollBars()
        If _inlineEdit Is Nothing OrElse _inlineHost Is Nothing OrElse Not _inlineHost.Visible Then Return
        If _inlineSingleLine OrElse Not _inlineEdit.WordWrap Then
            If _inlineEdit.ScrollBars <> RichTextBoxScrollBars.None Then
                _inlineEdit.ScrollBars = RichTextBoxScrollBars.None
            End If
            Return
        End If
        Dim needsScroll As Boolean = False
        Try
            If _inlineEdit.TextLength > 0 AndAlso _inlineEdit.ClientSize.Height > 0 Then
                Dim last As Point = _inlineEdit.GetPositionFromCharIndex(_inlineEdit.TextLength)
                Dim lineH As Integer = Math.Max(12, _inlineEdit.Font.Height + 2)
                needsScroll = (last.Y + lineH) > _inlineEdit.ClientSize.Height
            End If
        Catch
            needsScroll = False
        End Try
        Dim nextBars As RichTextBoxScrollBars = If(needsScroll, RichTextBoxScrollBars.Vertical, RichTextBoxScrollBars.None)
        If _inlineEdit.ScrollBars <> nextBars Then
            _inlineEdit.ScrollBars = nextBars
        End If
    End Sub

    Private Sub BeginInlineNodeEdit(ByVal node As BoardNode)
        Try
            If node Is Nothing OrElse IsDisposed Then Return
            If node.Kind = "image" Then Return
            Capture = False
            _suppressLostFocusCommit = True
            CommitInlineEdit(True)
            SelectOnly(node)
            Try
                CaptureHistory()
            Catch exHist As Exception
                LogCanvasError("CaptureHistory", exHist)
            End Try
            EnsureInlineEditControl()
            _inlineNode = node
            _inlineCancel = False
            _inlineSingleLine = node.IsGroup
            _inlineEdit.Tag = Nothing

            Dim showLines As Boolean = Not node.IsGroup
            _inlineLineMargin.Visible = showLines
            _inlineLineMargin.Width = Math.Max(28, CInt(32 * Math.Max(0.75F, _model.Zoom)))
            _inlineEdit.WordWrap = Not node.IsGroup
            ' Start with no scrollbar; UpdateInlineScrollBars enables Vertical only when needed.
            _inlineEdit.ScrollBars = RichTextBoxScrollBars.None

            Dim body As String
            If node.IsGroup Then
                body = If(Not String.IsNullOrEmpty(node.Label), node.Label, If(node.Text, ""))
            Else
                body = If(node.Text, "")
            End If
            ApplyInlineEditorText(body, leftAlign:=True)

            Try
                Dim sz As Single = Math.Max(8.0F, If(node.IsGroup, 11.0F, 10.0F) * Math.Max(0.5F, _model.Zoom))
                _inlineEdit.Font = New Font("Segoe UI", sz, If(node.IsGroup, FontStyle.Bold, FontStyle.Regular))
            Catch
                _inlineEdit.Font = SystemFonts.DefaultFont
            End Try

            Dim border As Color = CanvasColors.Resolve(node.Color)
            Dim bg As Color = Color.FromArgb(255,
                Math.Max(20, CInt(border.R * 0.25F)),
                Math.Max(16, CInt(border.G * 0.2F)),
                Math.Max(8, CInt(border.B * 0.15F)))
            _inlineEdit.ForeColor = Color.FromArgb(255, 255, 220, 160)
            _inlineEdit.BackColor = bg
            _inlineHost.BackColor = bg
            _inlineLineMargin.BackColor = Color.FromArgb(255,
                Math.Max(12, bg.R - 10),
                Math.Max(10, bg.G - 8),
                Math.Max(6, bg.B - 4))

            _inlineHost.Visible = True
            _inlineHost.BringToFront()
            LayoutInlineEditor()
            If _inlineLineMargin.Visible Then _inlineLineMargin.Invalidate()
            Invalidate()
            If IsHandleCreated Then
                BeginInvoke(New MethodInvoker(AddressOf FocusInlineEditorSafe))
            Else
                FocusInlineEditorSafe()
            End If
        Catch ex As Exception
            _suppressLostFocusCommit = False
            LogCanvasError("BeginInlineNodeEdit", ex)
            Try
                MessageBox.Show("Canvas edit failed:" & Environment.NewLine & ex.Message, "LCARS Canvas")
            Catch
            End Try
        End Try
    End Sub

    Private Sub BeginInlineEdgeEdit(ByVal edge As BoardEdge)
        Try
            If edge Is Nothing OrElse IsDisposed Then Return
            Capture = False
            _suppressLostFocusCommit = True
            CommitInlineEdit(True)
            _selectedEdge = edge
            _selection.Clear()
            Try
                CaptureHistory()
            Catch exHist As Exception
                LogCanvasError("CaptureHistory(edge)", exHist)
            End Try
            EnsureInlineEditControl()
            _inlineNode = Nothing
            _inlineSingleLine = True
            _inlineEdit.Tag = edge
            _inlineCancel = False
            _inlineLineMargin.Visible = False
            _inlineEdit.WordWrap = False
            _inlineEdit.ScrollBars = RichTextBoxScrollBars.None
            ApplyInlineEditorText(If(edge.Label, ""), leftAlign:=True)
            Try
                _inlineEdit.Font = New Font("Segoe UI", Math.Max(8.0F, 9.0F * Math.Max(0.5F, _model.Zoom)), FontStyle.Bold)
            Catch
                _inlineEdit.Font = SystemFonts.DefaultFont
            End Try
            Dim border As Color = CanvasColors.Resolve(edge.Color)
            _inlineEdit.ForeColor = Color.FromArgb(255, border.R, border.G, border.B)
            _inlineEdit.BackColor = Color.FromArgb(255, 10, 10, 10)
            _inlineHost.BackColor = _inlineEdit.BackColor
            _inlineHost.Visible = True
            _inlineHost.BringToFront()
            LayoutInlineEdgeEditor(edge)
            Invalidate()
            If IsHandleCreated Then
                BeginInvoke(New MethodInvoker(AddressOf FocusInlineEditorSafe))
            Else
                FocusInlineEditorSafe()
            End If
        Catch ex As Exception
            _suppressLostFocusCommit = False
            LogCanvasError("BeginInlineEdgeEdit", ex)
            Try
                MessageBox.Show("Canvas edge edit failed:" & Environment.NewLine & ex.Message, "LCARS Canvas")
            Catch
            End Try
        End Try
    End Sub

    Private Sub ApplyInlineEditorText(ByVal text As String, ByVal leftAlign As Boolean)
        If _inlineEdit Is Nothing Then Return
        _inlineEdit.Text = If(text, "")
        If leftAlign Then
            _inlineEdit.SelectAll()
            _inlineEdit.SelectionAlignment = HorizontalAlignment.Left
            _inlineEdit.SelectionLength = 0
        End If
    End Sub

    Private Sub FocusInlineEditorSafe()
        Try
            If _inlineHost Is Nothing OrElse Not _inlineHost.Visible OrElse _inlineHost.IsDisposed Then Return
            If _inlineEdit Is Nothing OrElse _inlineEdit.IsDisposed Then Return
            If Not _inlineEdit.IsHandleCreated Then _inlineEdit.CreateControl()
            _inlineEdit.Focus()
            _inlineEdit.SelectAll()
            If _inlineEdit.SelectionLength > 0 Then
                _inlineEdit.SelectionAlignment = HorizontalAlignment.Left
            End If
        Catch ex As Exception
            LogCanvasError("FocusInlineEditorSafe", ex)
        Finally
            _suppressLostFocusCommit = False
        End Try
    End Sub

    Private Shared Sub LogCanvasError(ByVal where As String, ByVal ex As Exception)
        Try
            Dim dir As String = IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LCARS x32")
            If Not IO.Directory.Exists(dir) Then IO.Directory.CreateDirectory(dir)
            Dim path As String = IO.Path.Combine(dir, "canvas-error.txt")
            IO.File.AppendAllText(path, DateTime.Now.ToString("s") & " [" & where & "] " & ex.ToString() & Environment.NewLine & Environment.NewLine)
        Catch
        End Try
    End Sub

    Private Sub LayoutInlineEditor()
        If _inlineHost Is Nothing OrElse Not _inlineHost.Visible Then Return
        Dim edge As BoardEdge = TryCast(If(_inlineEdit IsNot Nothing, _inlineEdit.Tag, Nothing), BoardEdge)
        If edge IsNot Nothing AndAlso _inlineNode Is Nothing Then
            LayoutInlineEdgeEditor(edge)
            Return
        End If
        If _inlineNode Is Nothing Then Return
        Dim pad As Single = 6.0F * _model.Zoom
        Dim tl As PointF = WorldToScreen(New PointF(_inlineNode.X, _inlineNode.Y))
        Dim w As Integer = Math.Max(40, CInt(_inlineNode.Width * _model.Zoom - pad * 2))
        Dim h As Integer
        Dim x As Integer = CInt(tl.X + pad)
        Dim y As Integer
        If _inlineNode.IsGroup Then
            h = Math.Max(22, CInt(24 * _model.Zoom))
            y = CInt(tl.Y + pad)
        Else
            h = Math.Max(40, CInt(_inlineNode.Height * _model.Zoom - pad * 2))
            y = CInt(tl.Y + pad)
        End If
        ' Keep editor on-screen.
        If x < 0 Then x = 0
        If y < 0 Then y = 0
        If x + w > Width Then w = Math.Max(40, Width - x)
        If y + h > Height Then h = Math.Max(22, Height - y)
        _inlineHost.SetBounds(x, y, w, h)
        UpdateInlineScrollBars()
        If _inlineLineMargin IsNot Nothing AndAlso _inlineLineMargin.Visible Then
            _inlineLineMargin.Invalidate()
        End If
    End Sub

    Private Sub LayoutInlineEdgeEditor(ByVal edge As BoardEdge)
        If _inlineHost Is Nothing OrElse edge Is Nothing Then Return
        Dim a As BoardNode = _model.FindNode(edge.FromId)
        Dim b As BoardNode = _model.FindNode(edge.ToId)
        If a Is Nothing OrElse b Is Nothing Then Return
        Dim p1 As PointF = WorldToScreen(a.AnchorPoint(edge.FromSide))
        Dim p2 As PointF = WorldToScreen(b.AnchorPoint(edge.ToSide))
        Dim mid As New PointF((p1.X + p2.X) / 2.0F, (p1.Y + p2.Y) / 2.0F)
        Dim w As Integer = Math.Max(80, CInt(140 * _model.Zoom))
        Dim h As Integer = Math.Max(22, CInt(24 * _model.Zoom))
        _inlineHost.SetBounds(CInt(mid.X - w / 2.0F), CInt(mid.Y - h / 2.0F), w, h)
    End Sub

    Private Sub InlineEdit_KeyDown(ByVal sender As Object, ByVal e As KeyEventArgs)
        If e.KeyCode = Keys.Escape Then
            _inlineCancel = True
            CommitInlineEdit(False)
            e.Handled = True
            e.SuppressKeyPress = True
            Return
        End If
        ' Ctrl+Enter commits multiline notes (Enter alone inserts newline).
        If e.Control AndAlso e.KeyCode = Keys.Enter Then
            CommitInlineEdit(True)
            e.Handled = True
            e.SuppressKeyPress = True
            Return
        End If
        ' Single-line editors (group / edge): Enter commits.
        If e.KeyCode = Keys.Enter AndAlso _inlineSingleLine Then
            CommitInlineEdit(True)
            e.Handled = True
            e.SuppressKeyPress = True
        End If
    End Sub

    Private Sub InlineEdit_LostFocus(ByVal sender As Object, ByVal e As EventArgs)
        If _inlineCommitting OrElse _suppressLostFocusCommit Then Return
        If Not IsHandleCreated OrElse IsDisposed Then Return
        BeginInvoke(New MethodInvoker(Sub()
                                          If _suppressLostFocusCommit OrElse _inlineCommitting Then Return
                                          If _inlineHost IsNot Nothing AndAlso _inlineHost.Visible AndAlso
                                             _inlineEdit IsNot Nothing AndAlso Not _inlineEdit.Focused Then
                                              CommitInlineEdit(Not _inlineCancel)
                                          End If
                                      End Sub))
    End Sub

    Private Sub CommitInlineEdit(ByVal save As Boolean)
        If _inlineCommitting Then Return
        If _inlineHost Is Nothing OrElse Not _inlineHost.Visible Then Return
        _inlineCommitting = True
        Try
            Dim edge As BoardEdge = TryCast(If(_inlineEdit IsNot Nothing, _inlineEdit.Tag, Nothing), BoardEdge)
            Dim text As String = If(_inlineEdit IsNot Nothing, _inlineEdit.Text, "")
            If _inlineEdit IsNot Nothing Then _inlineEdit.Tag = Nothing
            _inlineHost.Visible = False
            Dim node As BoardNode = _inlineNode
            _inlineNode = Nothing
            _inlineSingleLine = False
            If save AndAlso Not _inlineCancel Then
                If edge IsNot Nothing AndAlso node Is Nothing Then
                    edge.Label = text
                    _model.NotifyChanged()
                    RaiseEvent ModelChanged(Me, EventArgs.Empty)
                ElseIf node IsNot Nothing Then
                    If node.IsGroup Then
                        node.Label = text
                        node.Text = text
                    Else
                        node.Text = text
                        If node.Kind = "link" Then node.PathOrUrl = text
                    End If
                    _model.NotifyChanged()
                    RaiseEvent ModelChanged(Me, EventArgs.Empty)
                End If
            End If
            _inlineCancel = False
            Focus()
            Invalidate()
        Finally
            _inlineCommitting = False
        End Try
    End Sub

    Protected Overrides Function IsInputKey(ByVal keyData As Keys) As Boolean
        If _inlineHost IsNot Nothing AndAlso _inlineHost.Visible AndAlso _inlineEdit IsNot Nothing AndAlso _inlineEdit.Focused Then
            Return False
        End If
        If keyData = Keys.Delete OrElse keyData = Keys.Back Then Return True
        Dim code As Keys = keyData And Keys.KeyCode
        If code = Keys.A OrElse code = Keys.Z OrElse code = Keys.Y OrElse code = Keys.C OrElse code = Keys.V Then
            If (keyData And Keys.Control) = Keys.Control Then Return True
        End If
        Return MyBase.IsInputKey(keyData)
    End Function

    Protected Overrides Sub OnKeyDown(ByVal e As KeyEventArgs)
        MyBase.OnKeyDown(e)
        If _inlineHost IsNot Nothing AndAlso _inlineHost.Visible AndAlso _inlineEdit IsNot Nothing AndAlso _inlineEdit.Focused Then
            Return
        End If
        If e.Control AndAlso e.KeyCode = Keys.Z Then
            CommitInlineEdit(True)
            If _history IsNot Nothing AndAlso _history.Undo() Then
                _selection.Clear()
                _selectedEdge = Nothing
                RaiseEvent ModelChanged(Me, EventArgs.Empty)
                Invalidate()
            End If
            e.Handled = True
            Return
        End If
        If e.Control AndAlso e.KeyCode = Keys.Y Then
            CommitInlineEdit(True)
            If _history IsNot Nothing AndAlso _history.Redo() Then
                _selection.Clear()
                _selectedEdge = Nothing
                RaiseEvent ModelChanged(Me, EventArgs.Empty)
                Invalidate()
            End If
            e.Handled = True
            Return
        End If
        If e.Control AndAlso e.KeyCode = Keys.C Then
            CopySelection()
            e.Handled = True
            Return
        End If
        If e.Control AndAlso e.KeyCode = Keys.V Then
            PasteClipboard()
            e.Handled = True
            Return
        End If
        If e.Control AndAlso e.KeyCode = Keys.A Then
            _selection.Clear()
            _selection.AddRange(_model.Nodes)
            Invalidate()
            e.Handled = True
            Return
        End If
        If e.KeyCode = Keys.Delete OrElse e.KeyCode = Keys.Back Then
            If _selectedEdge IsNot Nothing Then
                CaptureHistory()
                _model.Edges.Remove(_selectedEdge)
                _selectedEdge = Nothing
                _model.NotifyChanged()
                RaiseEvent ModelChanged(Me, EventArgs.Empty)
                Invalidate()
                e.Handled = True
                Return
            End If
            If _selection.Count > 0 Then
                CaptureHistory()
                Dim ids As New List(Of String)
                For Each n As BoardNode In _selection
                    ids.Add(n.Id)
                Next
                For Each id As String In ids
                    Dim node As BoardNode = _model.FindNode(id)
                    If node IsNot Nothing Then _model.Nodes.Remove(node)
                    _model.Edges.RemoveAll(Function(ed) ed.FromId = id OrElse ed.ToId = id)
                Next
                _selection.Clear()
                _model.NotifyChanged()
                RaiseEvent ModelChanged(Me, EventArgs.Empty)
                Invalidate()
                e.Handled = True
            End If
        End If
    End Sub

    Public Sub Undo()
        If _history IsNot Nothing AndAlso _history.Undo() Then
            _selection.Clear()
            _selectedEdge = Nothing
            RaiseEvent ModelChanged(Me, EventArgs.Empty)
            Invalidate()
        End If
    End Sub

    Public Sub Redo()
        If _history IsNot Nothing AndAlso _history.Redo() Then
            _selection.Clear()
            _selectedEdge = Nothing
            RaiseEvent ModelChanged(Me, EventArgs.Empty)
            Invalidate()
        End If
    End Sub

    Public Sub CopySelection()
        If _selection.Count = 0 Then Return
        _clipboardJson = _model.ExportSelectionJson(_selection)
        Try
            Clipboard.SetText(_clipboardJson)
        Catch
        End Try
    End Sub

    Public Sub PasteClipboard()
        Dim json As String = _clipboardJson
        Try
            Dim clip As String = Clipboard.GetText()
            If Not String.IsNullOrWhiteSpace(clip) AndAlso clip.Contains("""nodes""") Then json = clip
        Catch
        End Try
        If String.IsNullOrWhiteSpace(json) Then Return
        CaptureHistory()
        Dim pasted = _model.ImportSelectionJson(json, 40.0F, 40.0F)
        _selection.Clear()
        _selectedEdge = Nothing
        If pasted IsNot Nothing Then _selection.AddRange(pasted)
        RaiseEvent ModelChanged(Me, EventArgs.Empty)
        Invalidate()
    End Sub

    Public Sub CycleSelectionColor()
        CaptureHistory()
        If _selectedEdge IsNot Nothing Then
            _model.CycleEdgeColor(_selectedEdge)
            RaiseEvent ModelChanged(Me, EventArgs.Empty)
            Return
        End If
        For Each n As BoardNode In _selection
            _model.CycleNodeColor(n)
        Next
        If _selection.Count > 0 Then RaiseEvent ModelChanged(Me, EventArgs.Empty)
    End Sub

    Public Sub EditSelectionLabel()
        If _selectedEdge IsNot Nothing Then
            BeginInlineEdgeEdit(_selectedEdge)
            Return
        End If
        Dim n As BoardNode = PrimarySelection()
        If n Is Nothing Then Return
        BeginInlineNodeEdit(n)
    End Sub
End Class
