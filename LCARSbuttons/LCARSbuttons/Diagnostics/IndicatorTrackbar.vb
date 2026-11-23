' LCARSbuttons/LCARSbuttons/Diagnostics/IndicatorTrackbar.vb
Option Strict On
Option Explicit On

Imports System.ComponentModel
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

Namespace Diagnostics

''' <summary>
''' Display-only LCARS trackbar: dotted guides, thin track, pinched triangle markers.
''' Matches the classic TRACKBARS demo (horizontal or vertical).
''' </summary>
Public Class IndicatorTrackbar
    Inherits Control

    Private _value As Integer = 0
    Private _minimum As Integer = 0
    Private _maximum As Integer = 100
    Private _orientation As Orientation = Orientation.Vertical
    Private _trackColor As Color = Color.FromArgb(153, 204, 255)
    Private _markerColor As Color = Color.Orange
    Private _showValue As Boolean = True

    Public Sub New()
        SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
        BackColor = Color.Black
        ForeColor = Color.Orange
        Size = New Size(36, 160)
    End Sub

    <DefaultValue(0)>
    Public Property Value As Integer
        Get
            Return _value
        End Get
        Set(ByVal value As Integer)
            Dim v As Integer = Math.Max(_minimum, Math.Min(_maximum, value))
            If _value = v Then Return
            _value = v
            Invalidate()
        End Set
    End Property

    <DefaultValue(0)>
    Public Property Minimum As Integer
        Get
            Return _minimum
        End Get
        Set(ByVal value As Integer)
            _minimum = value
            If _maximum < _minimum Then _maximum = _minimum
            Value = _value
        End Set
    End Property

    <DefaultValue(100)>
    Public Property Maximum As Integer
        Get
            Return _maximum
        End Get
        Set(ByVal value As Integer)
            _maximum = value
            If _maximum < _minimum Then _minimum = _maximum
            Value = _value
        End Set
    End Property

    <DefaultValue(GetType(Orientation), "Vertical")>
    Public Property Orientation As Orientation
        Get
            Return _orientation
        End Get
        Set(ByVal value As Orientation)
            _orientation = value
            Invalidate()
        End Set
    End Property

    Public Property TrackColor As Color
        Get
            Return _trackColor
        End Get
        Set(ByVal value As Color)
            _trackColor = value
            Invalidate()
        End Set
    End Property

    Public Property MarkerColor As Color
        Get
            Return _markerColor
        End Get
        Set(ByVal value As Color)
            _markerColor = value
            Invalidate()
        End Set
    End Property

    <DefaultValue(True)>
    Public Property ShowValueText As Boolean
        Get
            Return _showValue
        End Get
        Set(ByVal value As Boolean)
            _showValue = value
            Invalidate()
        End Set
    End Property

    Protected Overrides Sub OnPaint(ByVal e As PaintEventArgs)
        MyBase.OnPaint(e)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.Clear(BackColor)

        Dim range As Integer = Math.Max(1, _maximum - _minimum)
        Dim pct As Single = CSng(_value - _minimum) / CSng(range)

        If _orientation = Orientation.Vertical Then
            PaintVertical(g, pct)
        Else
            PaintHorizontal(g, pct)
        End If
    End Sub

    Private Sub PaintVertical(ByVal g As Graphics, ByVal pct As Single)
        Dim topPad As Integer = If(_showValue, 18, 6)
        Dim bottomPad As Integer = 6
        Dim trackTop As Integer = topPad
        Dim trackBottom As Integer = Height - bottomPad
        Dim trackLen As Integer = Math.Max(10, trackBottom - trackTop)
        Dim cx As Integer = Width \ 2

        If _showValue Then
            Using f As New Font("Arial Narrow", 10.0!, FontStyle.Bold)
                Dim t As String = _value.ToString()
                Dim sz As SizeF = g.MeasureString(t, f)
                Using br As New SolidBrush(ForeColor)
                    g.DrawString(t, f, br, (Width - sz.Width) / 2.0F, 1.0F)
                End Using
            End Using
        End If

        ' Guide dots left/right of track
        Dim stepY As Integer = Math.Max(4, trackLen \ 24)
        Using dotBr As New SolidBrush(Color.FromArgb(180, 220, 220, 220))
            For y As Integer = trackTop To trackBottom Step stepY
                g.FillEllipse(dotBr, cx - 10, y - 1, 2, 2)
                g.FillEllipse(dotBr, cx + 8, y - 1, 2, 2)
            Next
        End Using

        Using pen As New Pen(_trackColor, 3.0F)
            pen.StartCap = LineCap.Round
            pen.EndCap = LineCap.Round
            g.DrawLine(pen, cx, trackTop, cx, trackBottom)
        End Using

        Dim markerY As Integer = trackBottom - CInt(pct * trackLen)
        DrawPinchMarker(g, cx, markerY, True)
    End Sub

    Private Sub PaintHorizontal(ByVal g As Graphics, ByVal pct As Single)
        Dim leftPad As Integer = 6
        Dim rightPad As Integer = If(_showValue, 36, 6)
        Dim trackLeft As Integer = leftPad
        Dim trackRight As Integer = Width - rightPad
        Dim trackLen As Integer = Math.Max(10, trackRight - trackLeft)
        Dim cy As Integer = Height \ 2

        Dim stepX As Integer = Math.Max(4, trackLen \ 24)
        Using dotBr As New SolidBrush(Color.FromArgb(180, 220, 220, 220))
            For x As Integer = trackLeft To trackRight Step stepX
                g.FillEllipse(dotBr, x - 1, cy - 10, 2, 2)
                g.FillEllipse(dotBr, x - 1, cy + 8, 2, 2)
            Next
        End Using

        Using pen As New Pen(_trackColor, 3.0F)
            pen.StartCap = LineCap.Round
            pen.EndCap = LineCap.Round
            g.DrawLine(pen, trackLeft, cy, trackRight, cy)
        End Using

        Dim markerX As Integer = trackLeft + CInt(pct * trackLen)
        DrawPinchMarker(g, markerX, cy, False)

        If _showValue Then
            Using f As New Font("Arial Narrow", 10.0!, FontStyle.Bold)
                Using br As New SolidBrush(ForeColor)
                    g.DrawString(_value.ToString(), f, br, trackRight + 4, cy - 8)
                End Using
            End Using
        End If
    End Sub

    Private Sub DrawPinchMarker(ByVal g As Graphics, ByVal x As Integer, ByVal y As Integer, ByVal verticalTrack As Boolean)
        Using br As New SolidBrush(_markerColor)
            If verticalTrack Then
                ' Left triangle pointing right, right triangle pointing left
                Dim left() As Point = {New Point(x - 11, y - 6), New Point(x - 2, y), New Point(x - 11, y + 6)}
                Dim right() As Point = {New Point(x + 11, y - 6), New Point(x + 2, y), New Point(x + 11, y + 6)}
                g.FillPolygon(br, left)
                g.FillPolygon(br, right)
            Else
                Dim top() As Point = {New Point(x - 6, y - 11), New Point(x, y - 2), New Point(x + 6, y - 11)}
                Dim bot() As Point = {New Point(x - 6, y + 11), New Point(x, y + 2), New Point(x + 6, y + 11)}
                g.FillPolygon(br, top)
                g.FillPolygon(br, bot)
            End If
        End Using
    End Sub
End Class

End Namespace
