' LCARSpic/Media/RailScrollPanel.vb
Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Clipped right-rail viewport with touch-friendly vertical drag (no scrollbar).
''' Inner content is taller than the viewport when zoom/transport overflow.
''' </summary>
Public Class RailScrollPanel
    Private ReadOnly viewport As Panel
    Private ReadOnly inner As Panel
    Private dragging As Boolean
    Private dragStartScreenY As Integer
    Private dragStartInnerTop As Integer
    Private movedPx As Integer
    Private Const DragThreshold As Integer = 10
    Private wired As Boolean

    Public Sub New(ByVal host As Control)
        viewport = New Panel()
        viewport.Name = "pnlRailViewport"
        viewport.BackColor = Color.Black
        viewport.Visible = True
        host.Controls.Add(viewport)

        inner = New Panel()
        inner.Name = "pnlRailInner"
        inner.BackColor = Color.Black
        inner.Location = New Point(0, 0)
        viewport.Controls.Add(inner)

        WireDrag(viewport)
        WireDrag(inner)
        wired = True
    End Sub

    Public ReadOnly Property ViewportControl As Panel
        Get
            Return viewport
        End Get
    End Property

    Public ReadOnly Property InnerControl As Panel
        Get
            Return inner
        End Get
    End Property

    Public ReadOnly Property SuppressClick As Boolean
        Get
            Return movedPx >= DragThreshold
        End Get
    End Property

    Public Sub Adopt(ByVal c As Control)
        If c Is Nothing Then Return
        c.Anchor = AnchorStyles.None
        If c.Parent Is inner Then
            WireDrag(c)
            Return
        End If
        inner.Controls.Add(c)
        WireDrag(c)
    End Sub

    Public Sub PlaceViewport(ByVal left As Integer, ByVal top As Integer, ByVal width As Integer, ByVal height As Integer)
        If height < 40 Then height = 40
        viewport.Location = New Point(left, top)
        viewport.Size = New Size(width, height)
        viewport.BringToFront()
    End Sub

    ''' <summary>Set inner height and clamp scroll so content sits above the fixed CLOSE/BROWSE strip.</summary>
    Public Sub SetContentHeight(ByVal contentHeight As Integer)
        Dim h As Integer = Math.Max(viewport.Height, contentHeight)
        inner.Width = viewport.Width
        inner.Height = h
        ClampScroll()
    End Sub

    Public Sub ResetScrollToBottom()
        ' Show the bottom of the stack (NAV / primary controls) first.
        inner.Top = Math.Min(0, viewport.Height - inner.Height)
    End Sub

    Private Sub ClampScroll()
        Dim minTop As Integer = Math.Min(0, viewport.Height - inner.Height)
        If inner.Top < minTop Then inner.Top = minTop
        If inner.Top > 0 Then inner.Top = 0
    End Sub

    Private Sub WireDrag(ByVal c As Control)
        If c Is Nothing Then Return
        RemoveHandler c.MouseDown, AddressOf OnDown
        RemoveHandler c.MouseMove, AddressOf OnMove
        RemoveHandler c.MouseUp, AddressOf OnUp
        AddHandler c.MouseDown, AddressOf OnDown
        AddHandler c.MouseMove, AddressOf OnMove
        AddHandler c.MouseUp, AddressOf OnUp
        For Each child As Control In c.Controls
            WireDrag(child)
        Next
    End Sub

    Private Sub OnDown(ByVal sender As Object, ByVal e As MouseEventArgs)
        If e.Button <> MouseButtons.Left Then Return
        dragging = True
        movedPx = 0
        dragStartScreenY = Cursor.Position.Y
        dragStartInnerTop = inner.Top
        viewport.Capture = True
    End Sub

    Private Sub OnMove(ByVal sender As Object, ByVal e As MouseEventArgs)
        If Not dragging Then Return
        Dim dy As Integer = Cursor.Position.Y - dragStartScreenY
        movedPx = Math.Max(movedPx, Math.Abs(dy))
        If movedPx < DragThreshold Then Return
        inner.Top = dragStartInnerTop + dy
        ClampScroll()
    End Sub

    Private Sub OnUp(ByVal sender As Object, ByVal e As MouseEventArgs)
        dragging = False
        viewport.Capture = False
        ClampScroll()
    End Sub
End Class
