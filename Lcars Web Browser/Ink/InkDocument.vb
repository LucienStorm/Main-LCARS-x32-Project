Imports System.Collections
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging
Imports System.Globalization
Imports System.IO
Imports System.Text
Imports System.Web.Script.Serialization

''' <summary>
''' Stroke store for ink overlay: JSON save/load and PNG export.
''' </summary>
Public Class InkDocument

    Public Const FormatVersion As Integer = 1

    Private ReadOnly _strokes As New List(Of InkStroke)
    Private ReadOnly _serializer As New JavaScriptSerializer()

    Public Property CanvasWidth As Integer = 800
    Public Property CanvasHeight As Integer = 600
    Public Property BackgroundColor As Color = Color.Black

    Public ReadOnly Property Strokes As IList(Of InkStroke)
        Get
            Return _strokes
        End Get
    End Property

    Public Event DocumentChanged As EventHandler

    ''' <summary>
    ''' Clears all strokes and notifies listeners.
    ''' </summary>
    Public Sub Clear()
        _strokes.Clear()
        RaiseDocumentChanged()
    End Sub

    ''' <summary>
    ''' Adds a completed stroke to the document.
    ''' </summary>
    Public Sub AddStroke(ByVal stroke As InkStroke)
        If stroke Is Nothing OrElse stroke.Points.Count = 0 Then Return
        _strokes.Add(stroke)
        RaiseDocumentChanged()
    End Sub

    ''' <summary>
    ''' Serializes strokes to a .lcarsink JSON file.
    ''' </summary>
    Public Sub Save(ByVal filePath As String)
        Dim payload As New Dictionary(Of String, Object)()
        payload("version") = FormatVersion
        payload("width") = CanvasWidth
        payload("height") = CanvasHeight
        payload("background") = ColorToHex(BackgroundColor)

        Dim strokeList As New List(Of Object)()
        For Each stroke In _strokes
            strokeList.Add(SerializeStroke(stroke))
        Next
        payload("strokes") = strokeList

        Dim json As String = _serializer.Serialize(payload)
        File.WriteAllText(filePath, json, Encoding.UTF8)
    End Sub

    ''' <summary>
    ''' Loads strokes from a .lcarsink JSON file.
    ''' </summary>
    Public Sub Load(ByVal filePath As String)
        Dim json As String = File.ReadAllText(filePath, Encoding.UTF8)
        Dim payload As Dictionary(Of String, Object) = _serializer.Deserialize(Of Dictionary(Of String, Object))(json)

        _strokes.Clear()

        If payload.ContainsKey("width") Then
            CanvasWidth = Convert.ToInt32(payload("width"))
        End If
        If payload.ContainsKey("height") Then
            CanvasHeight = Convert.ToInt32(payload("height"))
        End If
        If payload.ContainsKey("background") Then
            BackgroundColor = HexToColor(Convert.ToString(payload("background")))
        End If

        If payload.ContainsKey("strokes") Then
            Dim strokeList As Object = payload("strokes")
            If TypeOf strokeList Is IList Then
                Dim strokeItems As IList = CType(strokeList, IList)
                For Each item In strokeItems
                    If TypeOf item Is Dictionary(Of String, Object) Then
                        Dim strokeDict As Dictionary(Of String, Object) = CType(item, Dictionary(Of String, Object))
                        _strokes.Add(DeserializeStroke(strokeDict))
                    End If
                Next
            End If
        End If

        RaiseDocumentChanged()
    End Sub

    ''' <summary>
    ''' Renders ink to a PNG file using the document canvas size.
    ''' </summary>
    Public Sub ExportPng(ByVal filePath As String)
        Using image As Bitmap = RenderToBitmap()
            image.Save(filePath, ImageFormat.Png)
        End Using
    End Sub

    ''' <summary>
    ''' Renders ink onto a bitmap sized to the document canvas.
    ''' </summary>
    Public Function RenderToBitmap() As Bitmap
        Dim width As Integer = Math.Max(1, CanvasWidth)
        Dim height As Integer = Math.Max(1, CanvasHeight)
        Dim bitmap As New Bitmap(width, height, PixelFormat.Format32bppArgb)

        Using graphics As Graphics = Graphics.FromImage(bitmap)
            graphics.SmoothingMode = SmoothingMode.AntiAlias
            graphics.Clear(BackgroundColor)
            DrawStrokes(graphics, width, height)
        End Using

        Return bitmap
    End Function

    ''' <summary>
    ''' Draws one stroke onto the supplied graphics context.
    ''' </summary>
    Public Sub DrawSingleStroke(ByVal graphics As Graphics, ByVal stroke As InkStroke, ByVal width As Integer, ByVal height As Integer)
        If graphics Is Nothing OrElse stroke Is Nothing Then Return
        graphics.SmoothingMode = SmoothingMode.AntiAlias

        Dim scaleX As Single = 1.0F
        Dim scaleY As Single = 1.0F
        If CanvasWidth > 0 Then scaleX = width / CanvasWidth
        If CanvasHeight > 0 Then scaleY = height / CanvasHeight

        Dim state As Drawing2D.GraphicsState = graphics.Save()
        If scaleX <> 1.0F OrElse scaleY <> 1.0F Then
            graphics.ScaleTransform(scaleX, scaleY)
        End If
        DrawStroke(graphics, stroke, CanvasWidth, CanvasHeight)
        graphics.Restore(state)
    End Sub

    ''' <summary>
    ''' Draws all strokes onto the supplied graphics context.
    ''' </summary>
    Public Sub DrawStrokes(ByVal graphics As Graphics, ByVal width As Integer, ByVal height As Integer, Optional ByVal includeEraser As Boolean = True)
        If graphics Is Nothing Then Return

        graphics.SmoothingMode = SmoothingMode.AntiAlias

        Dim scaleX As Single = 1.0F
        Dim scaleY As Single = 1.0F
        If CanvasWidth > 0 Then scaleX = width / CanvasWidth
        If CanvasHeight > 0 Then scaleY = height / CanvasHeight

        Dim state As Drawing2D.GraphicsState = graphics.Save()
        If scaleX <> 1.0F OrElse scaleY <> 1.0F Then
            graphics.ScaleTransform(scaleX, scaleY)
        End If

        For Each stroke In _strokes
            If stroke.IsEraser AndAlso Not includeEraser Then Continue For
            DrawStroke(graphics, stroke, CanvasWidth, CanvasHeight)
        Next

        graphics.Restore(state)
    End Sub

    Private Sub DrawStroke(ByVal graphics As Graphics, ByVal stroke As InkStroke, ByVal width As Integer, ByVal height As Integer)
        If stroke.Points.Count = 0 Then Return

        If stroke.IsEraser Then
            graphics.CompositingMode = CompositingMode.SourceCopy
            Using pen As New Pen(Color.Transparent, stroke.Width)
                pen.StartCap = LineCap.Round
                pen.EndCap = LineCap.Round
                pen.LineJoin = LineJoin.Round
                DrawStrokePath(graphics, stroke, pen)
            End Using
            graphics.CompositingMode = CompositingMode.SourceOver
            Return
        End If

        Using pen As New Pen(stroke.Color, stroke.Width)
            pen.StartCap = LineCap.Round
            pen.EndCap = LineCap.Round
            pen.LineJoin = LineJoin.Round
            DrawStrokePath(graphics, stroke, pen)
        End Using
    End Sub

    Private Sub DrawStrokePath(ByVal graphics As Graphics, ByVal stroke As InkStroke, ByVal pen As Pen)
        If stroke.Points.Count = 1 Then
            Dim point As InkPoint = stroke.Points(0)
            Dim radius As Single = Math.Max(pen.Width / 2.0F, 1.0F)
            graphics.FillEllipse(pen.Brush, point.X - radius, point.Y - radius, radius * 2.0F, radius * 2.0F)
            Return
        End If

        Using path As New GraphicsPath()
            path.StartFigure()
            path.AddLine(stroke.Points(0).X, stroke.Points(0).Y, stroke.Points(1).X, stroke.Points(1).Y)

            For i As Integer = 2 To stroke.Points.Count - 1
                Dim previous As InkPoint = stroke.Points(i - 1)
                Dim current As InkPoint = stroke.Points(i)
                Dim midX As Single = (previous.X + current.X) / 2.0F
                Dim midY As Single = (previous.Y + current.Y) / 2.0F
                path.AddLine(previous.X, previous.Y, midX, midY)
            Next

            graphics.DrawPath(pen, path)
        End Using
    End Sub

    Private Function SerializeStroke(ByVal stroke As InkStroke) As Dictionary(Of String, Object)
        Dim points As New List(Of Object)()
        For Each point In stroke.Points
            Dim pointData As New Dictionary(Of String, Object)
            pointData("x") = point.X
            pointData("y") = point.Y
            pointData("p") = point.Pressure
            points.Add(pointData)
        Next

        Dim strokeData As New Dictionary(Of String, Object)
        strokeData("color") = ColorToHex(stroke.Color)
        strokeData("width") = stroke.Width
        strokeData("eraser") = stroke.IsEraser
        strokeData("points") = points
        Return strokeData
    End Function

    Private Function DeserializeStroke(ByVal data As Dictionary(Of String, Object)) As InkStroke
        Dim stroke As New InkStroke()
        If data.ContainsKey("color") Then
            stroke.Color = HexToColor(Convert.ToString(data("color")))
        End If
        If data.ContainsKey("width") Then
            stroke.Width = Convert.ToSingle(data("width"))
        End If
        If data.ContainsKey("eraser") Then
            stroke.IsEraser = Convert.ToBoolean(data("eraser"))
        End If
        If data.ContainsKey("points") Then
            Dim pointList As Object = data("points")
            If TypeOf pointList Is IList Then
                Dim pointItems As IList = CType(pointList, IList)
                For Each item In pointItems
                    If TypeOf item Is Dictionary(Of String, Object) Then
                        Dim pointData As Dictionary(Of String, Object) = CType(item, Dictionary(Of String, Object))
                        Dim x As Single = Convert.ToSingle(pointData("x"))
                        Dim y As Single = Convert.ToSingle(pointData("y"))
                        Dim pressure As Single = 0.5F
                        If pointData.ContainsKey("p") Then
                            pressure = Convert.ToSingle(pointData("p"))
                        End If
                        stroke.Points.Add(New InkPoint(x, y, pressure))
                    End If
                Next
            End If
        End If
        Return stroke
    End Function

    Private Shared Function ColorToHex(ByVal color As Color) As String
        Return "#" & color.R.ToString("X2") & color.G.ToString("X2") & color.B.ToString("X2")
    End Function

    Private Shared Function HexToColor(ByVal hex As String) As Color
        If String.IsNullOrEmpty(hex) Then Return Color.Black
        Dim text As String = hex.Trim()
        If text.StartsWith("#") Then text = text.Substring(1)
        If text.Length = 6 Then
            Dim r As Integer = Int32.Parse(text.Substring(0, 2), NumberStyles.HexNumber)
            Dim g As Integer = Int32.Parse(text.Substring(2, 2), NumberStyles.HexNumber)
            Dim b As Integer = Int32.Parse(text.Substring(4, 2), NumberStyles.HexNumber)
            Return Color.FromArgb(r, g, b)
        End If
        Return Color.Black
    End Function

    Private Sub RaiseDocumentChanged()
        RaiseEvent DocumentChanged(Me, EventArgs.Empty)
    End Sub
End Class

''' <summary>
''' One ink sample with optional pressure (0..1).
''' </summary>
Public Class InkPoint
    Public Property X As Single
    Public Property Y As Single
    Public Property Pressure As Single

    Public Sub New(ByVal x As Single, ByVal y As Single, ByVal pressure As Single)
        Me.X = x
        Me.Y = y
        Me.Pressure = pressure
    End Sub
End Class

''' <summary>
''' Polyline stroke with tool metadata.
''' </summary>
Public Class InkStroke
    Public Property Points As New List(Of InkPoint)
    Public Property Color As Color = Color.Black
    Public Property Width As Single = 3.0F
    Public Property IsEraser As Boolean = False
End Class
