Option Strict On

Imports System.Drawing
Imports LCARS.LightweightControls

Public Class LCScreenImage
    Inherits LCARS.LightweightControls.LCFlatButton

    Private _image As Bitmap
    Private _selected As Boolean = False
    Private _corrupted As Boolean = False
    Private _BoxWidth As Integer = 10
    Private _padding As Integer = 3
    Private elbow As Bitmap

#Region " Properties "
    Public Property Image() As Bitmap
        Get
            Return _image
        End Get
        Set(ByVal value As Bitmap)
            _image = value
            Redraw()
        End Set
    End Property

    Public Property Selected() As Boolean
        Get
            Return _selected
        End Get
        Set(ByVal value As Boolean)
            If value <> _selected Then
                _selected = value
                Redraw()
            End If
        End Set
    End Property

    ''' <summary>Draws static / offline overlay. Used for the reserved Klingon view slot.</summary>
    Public Property Corrupted() As Boolean
        Get
            Return _corrupted
        End Get
        Set(ByVal value As Boolean)
            If value <> _corrupted Then
                _corrupted = value
                If _corrupted Then
                    _selected = False
                    Me.Clickable = False
                    Me.Color = LCARS.LCARScolorStyles.FunctionUnavailable
                End If
                Redraw()
            End If
        End Set
    End Property

    Public Property SelectionBoxWidth() As Integer
        Get
            Return _BoxWidth
        End Get
        Set(ByVal value As Integer)
            If _BoxWidth <> value Then
                _BoxWidth = value
                updateElbow()
                Redraw()
            End If
        End Set
    End Property
#End Region

    Public Sub New()
        updateElbow()
    End Sub

    Private Function GetBrushNoChange() As SolidBrush
        Return New SolidBrush(_colorsAvailable.getColor(_color))
    End Function

    Public Overrides Sub Redraw()
        If (Not _holdDraw) And _bounds.Width > 0 And _bounds.Height > 0 Then
            'Set up graphics
            Dim myBrush As Drawing.SolidBrush = GetBrushNoChange()
            myBitmap = New Bitmap(_bounds.Width, _bounds.Height)
            Dim g As Graphics = Graphics.FromImage(myBitmap)
            g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
            g.PixelOffsetMode = Drawing2D.PixelOffsetMode.HighQuality
            g.Clear(System.Drawing.Color.Black)

            'Draw image
            If Not _image Is Nothing Then
                Dim NewHeight As Integer = _bounds.Height - _BoxWidth * 2
                Dim NewWidth As Integer = CInt(NewHeight / _image.Height * _image.Width)
                If NewWidth > _bounds.Width - _BoxWidth * 4 Then
                    NewWidth = _bounds.Width - _BoxWidth * 4
                    NewHeight = CInt(NewWidth / _image.Width * _image.Height)
                End If
                Dim centerX As Integer = CInt(_BoxWidth * 2 + ((_bounds.Width - 4 * _BoxWidth) - NewWidth) / 2)
                Dim centerY As Integer = CInt(_BoxWidth * 1.5 + ((_bounds.Height - 3 * _BoxWidth) - NewHeight) / 2)
                g.DrawImage(_image, centerX, centerY, NewWidth, NewHeight)
            Else
                Dim format As New StringFormat
                format.Alignment = StringAlignment.Center
                format.LineAlignment = StringAlignment.Center
                g.DrawString("NO IMAGE", _font, myBrush, New PointF(_bounds.Width / 2.0F, _bounds.Height / 2.0F), format)
            End If

            If _corrupted Then
                DrawCorruptionOverlay(g, myBrush)
            End If

            If _selected AndAlso Not _corrupted Then
                'Draw vertical bars
                g.FillRectangle(myBrush, 0, CInt(_BoxWidth * 1.5 + _padding), _BoxWidth, _bounds.Height - 3 * _BoxWidth - 2 * _padding)
                g.FillRectangle(myBrush, _bounds.Width - _BoxWidth, CInt(_BoxWidth * 1.5 + _padding), _BoxWidth, _bounds.Height - 3 * _BoxWidth - 2 * _padding)

                'Draw elbows
                g.DrawImage(elbow, 0, 0)
                elbow.RotateFlip(RotateFlipType.RotateNoneFlipY)
                g.DrawImage(elbow, 0, _bounds.Height - CInt(_BoxWidth * 1.5))
                elbow.RotateFlip(RotateFlipType.RotateNoneFlipX)
                g.DrawImage(elbow, _bounds.Width - _BoxWidth * 2, _bounds.Height - CInt(_BoxWidth * 1.5))
                elbow.RotateFlip(RotateFlipType.RotateNoneFlipY)
                g.DrawImage(elbow, _bounds.Width - _BoxWidth * 2, 0)
                elbow.RotateFlip(RotateFlipType.RotateNoneFlipX)
            End If

            'Cleanup
            g.Dispose()
            MyBase.doEvent(ILightweightControl.LightweightEvents.Update)
        End If

    End Sub

    Private Sub updateElbow()
        'Setup
        elbow = New Bitmap(_BoxWidth * 2, CInt(_BoxWidth * 1.5))
        Dim myBrush As Drawing.SolidBrush = GetBrushNoChange()
        Dim g As Graphics = Graphics.FromImage(elbow)
        g.Clear(Drawing.Color.Black)
        g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
        g.PixelOffsetMode = Drawing2D.PixelOffsetMode.HighQuality
        g.Clear(System.Drawing.Color.Black)

        g.FillEllipse(myBrush, 0, 0, _BoxWidth * 2, _BoxWidth * 2)
        g.FillRectangle(Brushes.Black, _BoxWidth, _BoxWidth, _BoxWidth, _BoxWidth)
        g.FillRectangle(Brushes.Black, CInt(3 * _BoxWidth / 2), CInt(_BoxWidth / 2), CInt(_BoxWidth / 2), CInt(_BoxWidth / 2))

        g.FillRectangle(myBrush, 0, _BoxWidth, _BoxWidth, _BoxWidth)
        g.FillRectangle(myBrush, _BoxWidth, 0, _BoxWidth, CInt(_BoxWidth / 2))

        g.FillEllipse(Brushes.Black, _BoxWidth, CInt(_BoxWidth / 2), _BoxWidth, _BoxWidth)
        g.Dispose()
    End Sub

    Private Sub DrawCorruptionOverlay(ByVal g As Graphics, ByVal accent As SolidBrush)
        Using veil As New SolidBrush(Drawing.Color.FromArgb(170, 0, 0, 0))
            g.FillRectangle(veil, 0, 0, _bounds.Width, _bounds.Height)
        End Using

        Dim rng As New Random(CInt(_bounds.Width * 31 + _bounds.Height))
        For i As Integer = 0 To 18
            Dim y As Integer = rng.Next(0, Math.Max(1, _bounds.Height))
            Dim h As Integer = rng.Next(1, 4)
            Using bar As New SolidBrush(Drawing.Color.FromArgb(90 + rng.Next(0, 80), 40 + rng.Next(0, 40), 180 + rng.Next(0, 40), 40))
                g.FillRectangle(bar, 0, y, _bounds.Width, h)
            End Using
        Next
        For i As Integer = 0 To 40
            Dim x As Integer = rng.Next(0, Math.Max(1, _bounds.Width))
            Dim y As Integer = rng.Next(0, Math.Max(1, _bounds.Height))
            Using speck As New SolidBrush(Drawing.Color.FromArgb(140, 120 + rng.Next(0, 80), 20, 20))
                g.FillRectangle(speck, x, y, rng.Next(1, 3), rng.Next(1, 3))
            End Using
        Next

        Dim fmt As New StringFormat()
        fmt.Alignment = StringAlignment.Center
        fmt.LineAlignment = StringAlignment.Center
        Using warn As New SolidBrush(Drawing.Color.FromArgb(255, 220, 60, 40))
            g.DrawString("▓▓ SIGNAL LOST ▓▓", _font, warn, New RectangleF(0, _bounds.Height * 0.28F, _bounds.Width, _bounds.Height * 0.22F), fmt)
            g.DrawString("INTERFACE OFFLINE", _font, accent, New RectangleF(0, _bounds.Height * 0.52F, _bounds.Width, _bounds.Height * 0.22F), fmt)
        End Using
    End Sub

End Class
