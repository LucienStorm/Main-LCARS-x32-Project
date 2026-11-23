' LCARSbuttons/LCARSbuttons/Controls/LcarsScrollPanel.vb
Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Reflection
Imports System.Windows.Forms

Namespace Controls
    ''' <summary>
    ''' Touch-friendly vertical scroll host. The track is overlaid so ClientSize stays stable.
    ''' </summary>
    Public Class LcarsScrollPanel
        Inherits Panel

        Private ReadOnly content As New Panel()
        Private ReadOnly track As New Panel()
        Private thumbTop As Integer = 0
        Private thumbHeight As Integer = 48
        Private dragging As Boolean = False
        Private dragOffset As Integer = 0
        Private contentDragging As Boolean = False
        Private contentDragScreenY As Integer = 0
        Private contentStartTop As Integer = 0
        Private scrollNeeded As Boolean = False
        Private thumbColor As Color = Color.Orange
        Private trackColor As Color = Color.FromArgb(60, 40, 20)
        Private Const TrackWidth As Integer = 32
        Private Const DragThreshold As Integer = 6

        ''' <summary>When True, an unused track collapses to zero width.</summary>
        Public Property CollapseTrackWhenIdle As Boolean = False

        Public Sub New()
            SetStyle(ControlStyles.OptimizedDoubleBuffer Or ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or ControlStyles.ResizeRedraw, True)
            BackColor = Color.Black
            Try
                Dim map As New LCARS.LCARScolor()
                thumbColor = map.getColor(LCARS.LCARScolorStyles.Orange)
                Dim baseTan As Color = map.getColor(LCARS.LCARScolorStyles.StaticTan)
                trackColor = Color.FromArgb(70, baseTan.R \ 3, baseTan.G \ 3, baseTan.B \ 3)
            Catch
            End Try
            content.BackColor = Color.Black
            content.Location = New Point(0, 0)
            Controls.Add(content)
            track.BackColor = Color.Black
            track.Width = TrackWidth
            EnableDoubleBuffer(track)
            AddHandler track.Paint, AddressOf Track_Paint
            AddHandler track.MouseDown, AddressOf Track_MouseDown
            AddHandler track.MouseMove, AddressOf Track_MouseMove
            AddHandler track.MouseUp, AddressOf Track_MouseUp
            Controls.Add(track)
            track.BringToFront()
            AddHandler content.ControlAdded, AddressOf OnContentChildChanged
            AddHandler content.ControlRemoved, AddressOf OnContentChildChanged
            AddHandler Me.Resize, Sub() Relayout()
            AddHandler Me.MouseWheel, AddressOf OnWheel
            HookScrollGesture(content)
        End Sub

        Private Shared Sub EnableDoubleBuffer(ByVal c As Control)
            Try
                Dim pi As PropertyInfo = GetType(Control).GetProperty("DoubleBuffered", BindingFlags.Instance Or BindingFlags.NonPublic)
                If pi IsNot Nothing Then pi.SetValue(c, True, Nothing)
            Catch
            End Try
        End Sub

        Private Sub OnContentChildChanged(ByVal sender As Object, ByVal e As ControlEventArgs)
            Relayout()
            If e IsNot Nothing AndAlso e.Control IsNot Nothing Then HookScrollGesture(e.Control)
        End Sub

        ''' <summary>Wire drag-to-scroll onto non-interactive children.</summary>
        Public Sub HookScrollGesture(ByVal root As Control)
            If root Is Nothing Then Return
            If Not IsScrollGestureHost(root) Then Return
            RemoveHandler root.MouseDown, AddressOf Content_MouseDown
            RemoveHandler root.MouseMove, AddressOf Content_MouseMove
            RemoveHandler root.MouseUp, AddressOf Content_MouseUp
            AddHandler root.MouseDown, AddressOf Content_MouseDown
            AddHandler root.MouseMove, AddressOf Content_MouseMove
            AddHandler root.MouseUp, AddressOf Content_MouseUp
            For Each child As Control In root.Controls
                HookScrollGesture(child)
            Next
        End Sub

        Private Shared Function IsScrollGestureHost(ByVal c As Control) As Boolean
            If TypeOf c Is TextBoxBase Then Return False
            If TypeOf c Is ListControl Then Return False
            If TypeOf c Is ListView Then Return False
            If TypeOf c Is TrackBar Then Return False
            If TypeOf c Is ScrollBar Then Return False
            If TypeOf c Is ComboBox Then Return False
            Dim tName As String = c.GetType().Name
            If tName.IndexOf("Button", StringComparison.OrdinalIgnoreCase) >= 0 Then Return False
            If tName.IndexOf("Trackbar", StringComparison.OrdinalIgnoreCase) >= 0 Then Return False
            If tName.IndexOf("Indicator", StringComparison.OrdinalIgnoreCase) >= 0 Then Return False
            Return True
        End Function

        ''' <summary>Panel that hosts child controls.</summary>
        Public ReadOnly Property ContentPanel As Panel
            Get
                Return content
            End Get
        End Property

        Public Sub Relayout()
            Dim maxBottom As Integer = 0
            For Each c As Control In content.Controls
                Dim b As Integer = c.Bottom
                If b > maxBottom Then maxBottom = b
            Next
            Dim viewH As Integer = Math.Max(1, ClientSize.Height)
            Dim contentH As Integer = Math.Max(viewH, maxBottom + 8)
            Dim needScroll As Boolean = (contentH > viewH + 2)
            Dim reserveTrack As Boolean = needScroll OrElse dragging OrElse contentDragging OrElse Not CollapseTrackWhenIdle
            Dim tw As Integer = If(reserveTrack, TrackWidth, 0)
            content.Size = New Size(Math.Max(0, ClientSize.Width - tw), contentH)
            content.Left = 0
            If Not contentDragging AndAlso Not dragging Then
                If content.Top > 0 Then content.Top = 0
                If content.Top < viewH - contentH Then content.Top = viewH - contentH
            End If
            If tw > 0 Then
                track.Bounds = New Rectangle(ClientSize.Width - TrackWidth, 0, TrackWidth, viewH)
                track.Visible = True
                track.BringToFront()
            Else
                track.Visible = False
                track.Bounds = New Rectangle(ClientSize.Width, 0, 0, viewH)
            End If
            UpdateThumb()
            If track.Visible Then track.Invalidate()
        End Sub

        Private Sub UpdateThumb()
            Dim viewH As Integer = Math.Max(1, ClientSize.Height)
            Dim contentH As Integer = Math.Max(viewH, content.Height)
            scrollNeeded = (contentH > viewH + 2)
            Dim ratio As Double = CDbl(viewH) / CDbl(contentH)
            thumbHeight = Math.Max(48, CInt(viewH * ratio))
            Dim travel As Integer = Math.Max(1, viewH - thumbHeight)
            Dim scrollRange As Integer = Math.Max(1, contentH - viewH)
            Dim scrolled As Integer = -content.Top
            thumbTop = CInt((CDbl(scrolled) / CDbl(scrollRange)) * travel)
            If thumbTop < 0 Then thumbTop = 0
            If thumbTop > travel Then thumbTop = travel
        End Sub

        Private Sub Track_Paint(ByVal sender As Object, ByVal e As PaintEventArgs)
            Dim g As Graphics = e.Graphics
            g.SmoothingMode = SmoothingMode.AntiAlias
            Using bg As New SolidBrush(Color.Black)
                g.FillRectangle(bg, track.ClientRectangle)
            End Using
            Dim pad As Integer = 3
            Dim tw As Integer = track.Width - pad * 2
            Using br As New SolidBrush(trackColor)
                g.FillRectangle(br, pad, 0, tw, track.Height)
            End Using
            If Not scrollNeeded Then Return
            Using path As New GraphicsPath()
                Dim r As New Rectangle(pad, thumbTop, tw, thumbHeight)
                Dim rad As Integer = Math.Min(10, tw \ 2)
                path.AddArc(r.X, r.Y, rad * 2, rad * 2, 180, 90)
                path.AddArc(r.Right - rad * 2, r.Y, rad * 2, rad * 2, 270, 90)
                path.AddArc(r.Right - rad * 2, r.Bottom - rad * 2, rad * 2, rad * 2, 0, 90)
                path.AddArc(r.X, r.Bottom - rad * 2, rad * 2, rad * 2, 90, 90)
                path.CloseFigure()
                Using br As New SolidBrush(thumbColor)
                    g.FillPath(br, path)
                End Using
            End Using
        End Sub

        Private Sub Track_MouseDown(ByVal sender As Object, ByVal e As MouseEventArgs)
            If e.Button <> MouseButtons.Left Then Return
            If Not scrollNeeded Then Return
            If e.Y >= thumbTop AndAlso e.Y <= thumbTop + thumbHeight Then
                dragging = True
                dragOffset = e.Y - thumbTop
                track.Capture = True
            Else
                Dim viewH As Integer = Math.Max(1, ClientSize.Height)
                If e.Y < thumbTop Then
                    content.Top = Math.Min(0, content.Top + viewH \ 2)
                Else
                    content.Top = Math.Max(viewH - content.Height, content.Top - viewH \ 2)
                End If
                UpdateThumb()
                track.Invalidate()
            End If
        End Sub

        Private Sub Track_MouseMove(ByVal sender As Object, ByVal e As MouseEventArgs)
            If Not dragging Then Return
            Dim viewH As Integer = Math.Max(1, ClientSize.Height)
            Dim travel As Integer = Math.Max(1, viewH - thumbHeight)
            Dim y As Integer = e.Y - dragOffset
            If y < 0 Then y = 0
            If y > travel Then y = travel
            Dim scrollRange As Integer = Math.Max(1, content.Height - viewH)
            content.Top = -CInt((CDbl(y) / CDbl(travel)) * scrollRange)
            thumbTop = y
            track.Invalidate()
        End Sub

        Private Sub Track_MouseUp(ByVal sender As Object, ByVal e As MouseEventArgs)
            dragging = False
            Try
                track.Capture = False
            Catch
            End Try
        End Sub

        Private Sub Content_MouseDown(ByVal sender As Object, ByVal e As MouseEventArgs)
            If e.Button <> MouseButtons.Left Then Return
            If Not scrollNeeded Then Return
            contentDragging = False
            contentDragScreenY = Control.MousePosition.Y
            contentStartTop = content.Top
            Try
                DirectCast(sender, Control).Capture = True
            Catch
            End Try
        End Sub

        Private Sub Content_MouseMove(ByVal sender As Object, ByVal e As MouseEventArgs)
            If (Control.MouseButtons And MouseButtons.Left) = 0 Then
                contentDragging = False
                Return
            End If
            Dim screenY As Integer = Control.MousePosition.Y
            Dim delta As Integer = screenY - contentDragScreenY
            If Not contentDragging Then
                If Math.Abs(delta) < DragThreshold Then Return
                contentDragging = True
            End If
            Dim viewH As Integer = Math.Max(1, ClientSize.Height)
            Dim newTop As Integer = contentStartTop + delta
            If newTop > 0 Then newTop = 0
            If newTop < viewH - content.Height Then newTop = viewH - content.Height
            If content.Top <> newTop Then
                content.Top = newTop
                UpdateThumb()
                track.Invalidate()
            End If
        End Sub

        Private Sub Content_MouseUp(ByVal sender As Object, ByVal e As MouseEventArgs)
            contentDragging = False
            Try
                DirectCast(sender, Control).Capture = False
            Catch
            End Try
        End Sub

        Private Sub OnWheel(ByVal sender As Object, ByVal e As MouseEventArgs)
            Dim viewH As Integer = Math.Max(1, ClientSize.Height)
            content.Top = Math.Max(viewH - content.Height, Math.Min(0, content.Top + e.Delta \ 4))
            UpdateThumb()
            track.Invalidate()
        End Sub

        ''' <summary>Scroll so the top of a child stays in view.</summary>
        Public Sub EnsureChildVisibleTop(ByVal child As Control, ByVal preferHeight As Integer)
            If child Is Nothing Then Return
            Dim y As Integer = 0
            If content.Controls.Contains(child) Then
                y = child.Top
            Else
                Dim p As Control = child
                While p IsNot Nothing AndAlso Not Object.ReferenceEquals(p, content)
                    y += p.Top
                    p = p.Parent
                End While
                If p Is Nothing Then Return
            End If
            Dim h As Integer = Math.Max(40, preferHeight)
            EnsureRangeVisible(y, y + h)
        End Sub

        ''' <summary>Scroll so that a child is within the viewport.</summary>
        Public Sub EnsureChildVisible(ByVal child As Control)
            If child Is Nothing Then Return
            If Not content.Controls.Contains(child) Then
                Dim p As Control = child
                Dim y As Integer = 0
                Dim h As Integer = child.Height
                While p IsNot Nothing AndAlso Not Object.ReferenceEquals(p, content)
                    y += p.Top
                    p = p.Parent
                End While
                If p Is Nothing Then Return
                EnsureRangeVisible(y, y + h)
                Return
            End If
            EnsureRangeVisible(child.Top, child.Bottom)
        End Sub

        Private Sub EnsureRangeVisible(ByVal top As Integer, ByVal bottom As Integer)
            Dim viewH As Integer = Math.Max(1, ClientSize.Height)
            Dim curTop As Integer = -content.Top
            Dim curBottom As Integer = curTop + viewH
            If top < curTop Then
                content.Top = -top
            ElseIf bottom > curBottom Then
                content.Top = -(bottom - viewH)
            End If
            Dim minTop As Integer = viewH - content.Height
            If content.Top > 0 Then content.Top = 0
            If content.Top < minTop Then content.Top = minTop
            UpdateThumb()
            track.Invalidate()
        End Sub
    End Class
End Namespace
