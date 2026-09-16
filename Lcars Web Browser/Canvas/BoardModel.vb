' Lcars Web Browser/Canvas/BoardModel.vb
Option Strict On
Option Explicit On

Imports System.Collections
Imports System.Collections.Generic
Imports System.Drawing
Imports System.IO
Imports System.Text
Imports System.Web.Script.Serialization

''' <summary>
''' Obsidian-style board: nodes, edges, ink strokes, camera.
''' </summary>
Public Class BoardModel
    Public Property Nodes As New List(Of BoardNode)
    Public Property Edges As New List(Of BoardEdge)
    Public Property Strokes As New List(Of BoardStroke)
    Public Property CameraX As Single = 0
    Public Property CameraY As Single = 0
    Public Property Zoom As Single = 1.0F

    Public Event Changed As EventHandler

    Public Sub NotifyChanged()
        RaiseEvent Changed(Me, EventArgs.Empty)
    End Sub

    Public Function AddNote(ByVal worldX As Single, ByVal worldY As Single, Optional ByVal text As String = "Note") As BoardNode
        Dim n As New BoardNode() With {
            .Id = Guid.NewGuid().ToString("N"),
            .Kind = "note",
            .X = worldX,
            .Y = worldY,
            .Width = 180,
            .Height = 100,
            .Text = text
        }
        Nodes.Add(n)
        NotifyChanged()
        Return n
    End Function

    Public Function AddLink(ByVal worldX As Single, ByVal worldY As Single, ByVal url As String) As BoardNode
        Dim n As New BoardNode() With {
            .Id = Guid.NewGuid().ToString("N"),
            .Kind = "link",
            .X = worldX,
            .Y = worldY,
            .Width = 200,
            .Height = 60,
            .Text = url,
            .PathOrUrl = url
        }
        Nodes.Add(n)
        NotifyChanged()
        Return n
    End Function

    Public Function AddImage(ByVal worldX As Single, ByVal worldY As Single, ByVal imagePath As String) As BoardNode
        Dim n As New BoardNode() With {
            .Id = Guid.NewGuid().ToString("N"),
            .Kind = "image",
            .X = worldX,
            .Y = worldY,
            .Width = 220,
            .Height = 160,
            .Text = System.IO.Path.GetFileName(imagePath),
            .PathOrUrl = imagePath
        }
        Nodes.Add(n)
        NotifyChanged()
        Return n
    End Function

    Public Function AddFile(ByVal worldX As Single, ByVal worldY As Single, ByVal filePath As String) As BoardNode
        Dim n As New BoardNode() With {
            .Id = Guid.NewGuid().ToString("N"),
            .Kind = "file",
            .X = worldX,
            .Y = worldY,
            .Width = 220,
            .Height = 72,
            .Text = System.IO.Path.GetFileName(filePath),
            .PathOrUrl = filePath
        }
        Nodes.Add(n)
        NotifyChanged()
        Return n
    End Function

    Public Function BeginStroke(ByVal worldX As Single, ByVal worldY As Single) As BoardStroke
        Dim s As New BoardStroke() With {
            .Id = Guid.NewGuid().ToString("N"),
            .ColorArgb = Color.FromArgb(255, 255, 153, 0).ToArgb(),
            .Width = 3.0F
        }
        s.Points.Add(New PointF(worldX, worldY))
        Strokes.Add(s)
        Return s
    End Function

    Public Sub ContinueStroke(ByVal stroke As BoardStroke, ByVal worldX As Single, ByVal worldY As Single)
        If stroke Is Nothing Then Return
        If stroke.Points.Count > 0 Then
            Dim last As PointF = stroke.Points(stroke.Points.Count - 1)
            Dim dx As Single = worldX - last.X
            Dim dy As Single = worldY - last.Y
            If (dx * dx + dy * dy) < 0.25F Then Return
        End If
        stroke.Points.Add(New PointF(worldX, worldY))
    End Sub

    Public Sub Connect(ByVal fromId As String, ByVal toId As String)
        If String.IsNullOrEmpty(fromId) OrElse String.IsNullOrEmpty(toId) OrElse fromId = toId Then Return
        For Each e As BoardEdge In Edges
            If e.FromId = fromId AndAlso e.ToId = toId Then Return
        Next
        Edges.Add(New BoardEdge() With {.FromId = fromId, .ToId = toId})
        NotifyChanged()
    End Sub

    Public Function FindNode(ByVal id As String) As BoardNode
        For Each n As BoardNode In Nodes
            If n.Id = id Then Return n
        Next
        Return Nothing
    End Function

    Public Function HitTest(ByVal worldX As Single, ByVal worldY As Single) As BoardNode
        For i As Integer = Nodes.Count - 1 To 0 Step -1
            Dim n As BoardNode = Nodes(i)
            If worldX >= n.X AndAlso worldX <= n.X + n.Width AndAlso worldY >= n.Y AndAlso worldY <= n.Y + n.Height Then
                Return n
            End If
        Next
        Return Nothing
    End Function

    Public Sub Save(ByVal path As String)
        Dim ser As New JavaScriptSerializer()
        Dim dto As New Dictionary(Of String, Object)
        dto("cameraX") = CameraX
        dto("cameraY") = CameraY
        dto("zoom") = Zoom
        Dim nodeList As New List(Of Dictionary(Of String, Object))
        For Each n As BoardNode In Nodes
            Dim d As New Dictionary(Of String, Object)
            d("id") = n.Id
            d("kind") = n.Kind
            d("x") = n.X
            d("y") = n.Y
            d("w") = n.Width
            d("h") = n.Height
            d("text") = n.Text
            d("path") = n.PathOrUrl
            nodeList.Add(d)
        Next
        dto("nodes") = nodeList
        Dim edgeList As New List(Of Dictionary(Of String, Object))
        For Each e As BoardEdge In Edges
            Dim d As New Dictionary(Of String, Object)
            d("from") = e.FromId
            d("to") = e.ToId
            edgeList.Add(d)
        Next
        dto("edges") = edgeList
        Dim strokeList As New List(Of Dictionary(Of String, Object))
        For Each s As BoardStroke In Strokes
            Dim d As New Dictionary(Of String, Object)
            d("id") = s.Id
            d("color") = s.ColorArgb
            d("width") = s.Width
            Dim pts As New List(Of Dictionary(Of String, Object))
            For Each p As PointF In s.Points
                Dim pd As New Dictionary(Of String, Object)
                pd("x") = p.X
                pd("y") = p.Y
                pts.Add(pd)
            Next
            d("points") = pts
            strokeList.Add(d)
        Next
        dto("strokes") = strokeList
        File.WriteAllText(path, ser.Serialize(dto), New UTF8Encoding(False))
    End Sub

    Public Sub Load(ByVal path As String)
        Dim ser As New JavaScriptSerializer()
        Dim dto As Dictionary(Of String, Object) = ser.Deserialize(Of Dictionary(Of String, Object))(File.ReadAllText(path))
        Nodes.Clear()
        Edges.Clear()
        Strokes.Clear()
        CameraX = CSng(Convert.ToDouble(dto("cameraX")))
        CameraY = CSng(Convert.ToDouble(dto("cameraY")))
        Zoom = CSng(Convert.ToDouble(dto("zoom")))
        If Zoom < 0.2F Then Zoom = 0.2F
        If Zoom > 4.0F Then Zoom = 4.0F

        Dim nodeRaw As Object = dto("nodes")
        If TypeOf nodeRaw Is IList Then
            For Each item As Object In CType(nodeRaw, IList)
                Dim d As Dictionary(Of String, Object) = TryCast(item, Dictionary(Of String, Object))
                If d Is Nothing Then Continue For
                Nodes.Add(New BoardNode() With {
                    .Id = CStr(d("id")),
                    .Kind = CStr(d("kind")),
                    .X = CSng(Convert.ToDouble(d("x"))),
                    .Y = CSng(Convert.ToDouble(d("y"))),
                    .Width = CSng(Convert.ToDouble(d("w"))),
                    .Height = CSng(Convert.ToDouble(d("h"))),
                    .Text = If(d.ContainsKey("text"), CStr(d("text")), ""),
                    .PathOrUrl = If(d.ContainsKey("path"), CStr(d("path")), "")
                })
            Next
        End If

        If dto.ContainsKey("edges") AndAlso TypeOf dto("edges") Is IList Then
            For Each item As Object In CType(dto("edges"), IList)
                Dim d As Dictionary(Of String, Object) = TryCast(item, Dictionary(Of String, Object))
                If d Is Nothing Then Continue For
                Edges.Add(New BoardEdge() With {.FromId = CStr(d("from")), .ToId = CStr(d("to"))})
            Next
        End If

        If dto.ContainsKey("strokes") AndAlso TypeOf dto("strokes") Is IList Then
            For Each item As Object In CType(dto("strokes"), IList)
                Dim d As Dictionary(Of String, Object) = TryCast(item, Dictionary(Of String, Object))
                If d Is Nothing Then Continue For
                Dim stroke As New BoardStroke() With {
                    .Id = If(d.ContainsKey("id"), CStr(d("id")), Guid.NewGuid().ToString("N")),
                    .ColorArgb = If(d.ContainsKey("color"), Convert.ToInt32(d("color")), Color.FromArgb(255, 255, 153, 0).ToArgb()),
                    .Width = If(d.ContainsKey("width"), CSng(Convert.ToDouble(d("width"))), 3.0F)
                }
                If d.ContainsKey("points") AndAlso TypeOf d("points") Is IList Then
                    For Each ptObj As Object In CType(d("points"), IList)
                        Dim pd As Dictionary(Of String, Object) = TryCast(ptObj, Dictionary(Of String, Object))
                        If pd IsNot Nothing AndAlso pd.ContainsKey("x") AndAlso pd.ContainsKey("y") Then
                            stroke.Points.Add(New PointF(CSng(Convert.ToDouble(pd("x"))), CSng(Convert.ToDouble(pd("y")))))
                            Continue For
                        End If
                        Dim arr As ArrayList = TryCast(ptObj, ArrayList)
                        If arr IsNot Nothing AndAlso arr.Count >= 2 Then
                            stroke.Points.Add(New PointF(CSng(Convert.ToDouble(arr(0))), CSng(Convert.ToDouble(arr(1)))))
                        End If
                    Next
                End If
                If stroke.Points.Count > 0 Then Strokes.Add(stroke)
            Next
        End If
        NotifyChanged()
    End Sub
End Class

Public Class BoardNode
    Public Property Id As String
    Public Property Kind As String = "note"
    Public Property X As Single
    Public Property Y As Single
    Public Property Width As Single = 160
    Public Property Height As Single = 90
    Public Property Text As String = ""
    Public Property PathOrUrl As String = ""
End Class

Public Class BoardEdge
    Public Property FromId As String
    Public Property ToId As String
End Class

Public Class BoardStroke
    Public Property Id As String
    Public Property ColorArgb As Integer
    Public Property Width As Single = 3.0F
    Public Property Points As New List(Of PointF)
End Class
