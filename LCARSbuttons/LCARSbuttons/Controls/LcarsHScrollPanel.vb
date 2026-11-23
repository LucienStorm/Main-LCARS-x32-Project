' LCARSbuttons/LCARSbuttons/Controls/LcarsHScrollPanel.vb
Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Reflection
Imports System.Windows.Forms

Namespace Controls
    ''' <summary>
    ''' Touch-friendly horizontal scroll host. The track sits at the bottom and stays overlaid.
    ''' </summary>
    Public Class LcarsHScrollPanel
        Inherits Panel

        Private ReadOnly content As New Panel()
        Private ReadOnly track As New Panel()
        Private thumbLeft As Integer = 0
        Private thumbWidth As Integer = 48
        Private dragging As Boolean = False
        Private dragOffset As Integer = 0
        Private contentDragging As Boolean = False
        Private contentDragScreenX As Integer = 0
        Private contentStartLeft As Integer = 0
        Private scrollNeeded As Boolean = False
        Private thumbColor As Color = Color.Orange
        Private trackColor As Color = Color.FromArgb(60, 40, 20)
        Private Const TrackHeight As Integer = 28
        Private Const DragThreshold As Integer = 6

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
            track.Height = TrackHeight
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
            If TypeOf c Is ScrollBar Then Return False
            If TypeOf c Is ComboBox Then Return False
            Dim tName As String = c.GetType().Name
            If tName.IndexOf("Button", StringComparison.OrdinalIgnoreCase) >= 0 Then Return False
            Return True
        End Function

        Public ReadOnly Property ContentPanel As Panel
            Get
                Return content
            End Get
        End Property

        Public Sub Relayout()
            Dim maxRight As Integer = 0
            For Each c As Control In content.Controls
                Dim r As Integer = c.Right
                If r > maxRight Then maxRight = r
            Next
            Dim viewW As Integer = Math.Max(1, ClientSize.Width)
            Dim viewH As Integer = Math.Max(1, ClientSize.Height)
            Dim contentW As Integer = Math.Max(viewW, maxRight + 8)
            Dim contentH As Integer = Math.Max(0, viewH - TrackHeight)
            content.Size = New Size(contentW, contentH)
            content.Top = 0
            If Not contentDragging AndAlso Not dragging Then
                If content.Left > 0 Then content.Left = 0
                If content.Left < viewW - contentW Then content.Left = viewW - contentW
            End If
            track.Bounds = New Rectangle(0, viewH - TrackHeight, viewW, TrackHeight)
            track.BringToFront()
            UpdateThumb()
            track.Invalidate()
        End Sub

        Private Sub UpdateThumb()
            Dim viewW As Integer = Math.Max(1, ClientSize.Width)
            Dim contentW As Integer = Math.Max(viewW, content.Width)
            scrollNeeded = (contentW > viewW + 2)
            Dim ratio As Double = CDbl(viewW) / CDbl(contentW)
            thumbWidth = Math.Max(48, CInt(viewW * ratio))
            Dim travel As Integer = Math.Max(1, viewW - thumbWidth)
            Dim scrollRange As Integer = Math.Max(1, contentW - viewW)
            Dim scrolled As Integer = -content.Left
            thumbLeft = CInt((CDbl(scrolled) / CDbl(scrollRange)) * travel)
            If thumbLeft < 0 Then thumbLeft = 0
            If thumbLeft > travel Then thumbLeft = travel
        End Sub

        Private Sub Track_Paint(ByVal sender As Object, ByVal e As PaintEventArgs)
            Dim g As Graphics = e.Graphics
            g.SmoothingMode = SmoothingMode.AntiAlias
            Using bg As New SolidBrush(Color.Black)
                g.FillRectangle(bg, track.ClientRectangle)
            End Using
            Dim pad As Integer = 3
            Dim th As Integer = track.Height - pad * 2
            Using br As New SolidBrush(trackColor)
                g.FillRectangle(br, 0, pad, track.Width, th)
            End Using
            If Not scrollNeeded Then Return
            Using path As New GraphicsPath()
                Dim r As New Rectangle(thumbLeft, pad, thumbWidth, th)
                Dim rad As Integer = Math.Min(10, th \ 2)
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
            If e.X >= thumbLeft AndAlso e.X <= thumbLeft + thumbWidth Then
                dragging = True
                dragOffset = e.X - thumbLeft
                track.Capture = True
            Else
                Dim viewW As Integer = Math.Max(1, ClientSize.Width)
                If e.X < thumbLeft Then
                    content.Left = Math.Min(0, content.Left + viewW \ 2)
                Else
                    content.Left = Math.Max(viewW - content.Width, content.Left - viewW \ 2)
                End If
                UpdateThumb()
                track.Invalidate()
            End If
        End Sub

        Private Sub Track_MouseMove(ByVal sender As Object, ByVal e As MouseEventArgs)
            If Not dragging Then Return
            Dim viewW As Integer = Math.Max(1, ClientSize.Width)
            Dim travel As Integer = Math.Max(1, viewW - thumbWidth)
            Dim x As Integer = e.X - dragOffset
            If x < 0 Then x = 0
            If x > travel Then x = travel
            Dim scrollRange As Integer = Math.Max(1, content.Width - viewW)
            content.Left = -CInt((CDbl(x) / CDbl(travel)) * scrollRange)
            thumbLeft = x
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
            contentDragScreenX = Control.MousePosition.X
            contentStartLeft = content.Left
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
            Dim screenX As Integer = Control.MousePosition.X
            Dim delta As Integer = screenX - contentDragScreenX
            If Not contentDragging Then
                If Math.Abs(delta) < DragThreshold Then Return
                contentDragging = True
            End If
            Dim viewW As Integer = Math.Max(1, ClientSize.Width)
            Dim newLeft As Integer = contentStartLeft + delta
            If newLeft > 0 Then newLeft = 0
            If newLeft < viewW - content.Width Then newLeft = viewW - content.Width
            If content.Left <> newLeft Then
                content.Left = newLeft
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
            Dim viewW As Integer = Math.Max(1, ClientSize.Width)
            content.Left = Math.Max(viewW - content.Width, Math.Min(0, content.Left + e.Delta \ 4))
            UpdateThumb()
            track.Invalidate()
        End Sub
    End Class
End Namespace
