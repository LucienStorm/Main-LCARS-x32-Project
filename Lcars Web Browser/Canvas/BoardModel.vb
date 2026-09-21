' Lcars Web Browser/Canvas/BoardModel.vb
' Obsidian / JSON Canvas 1.0 compatible board model.
Option Strict On
Option Explicit On

Imports System.Collections
Imports System.Collections.Generic
Imports System.Drawing
Imports System.IO
Imports System.Text
Imports System.Web.Script.Serialization

''' <summary>
''' Obsidian-style board: nodes (text/file/link/group/image), labeled directed edges, ink, camera.
''' Saves/loads .lcarscanvas (native) and Obsidian .canvas (JSON Canvas 1.0).
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
            .Width = 220,
            .Height = 140,
            .Text = text,
            .Color = "2"
        }
        Nodes.Add(n)
        NotifyChanged()
        Return n
    End Function

    Public Function AddGroup(ByVal worldX As Single, ByVal worldY As Single, Optional ByVal label As String = "Group") As BoardNode
        Dim n As New BoardNode() With {
            .Id = Guid.NewGuid().ToString("N"),
            .Kind = "group",
            .X = worldX,
            .Y = worldY,
            .Width = 420,
            .Height = 300,
            .Text = label,
            .Color = "6"
        }
        Nodes.Insert(0, n)
        NotifyChanged()
        Return n
    End Function

    Public Function AddLink(ByVal worldX As Single, ByVal worldY As Single, ByVal url As String) As BoardNode
        Dim n As New BoardNode() With {
            .Id = Guid.NewGuid().ToString("N"),
            .Kind = "link",
            .X = worldX,
            .Y = worldY,
            .Width = 240,
            .Height = 72,
            .Text = url,
            .PathOrUrl = url,
            .Color = "5"
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
            .Width = 240,
            .Height = 180,
            .Text = System.IO.Path.GetFileName(imagePath),
            .PathOrUrl = imagePath,
            .Color = "4"
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
            .Width = 240,
            .Height = 80,
            .Text = System.IO.Path.GetFileName(filePath),
            .PathOrUrl = filePath,
            .Color = "3"
        }
        Nodes.Add(n)
        NotifyChanged()
        Return n
    End Function

    Public Function BeginStroke(ByVal worldX As Single, ByVal worldY As Single, Optional ByVal colorArgb As Integer = 0) As BoardStroke
        Dim s As New BoardStroke() With {
            .Id = Guid.NewGuid().ToString("N"),
            .ColorArgb = If(colorArgb <> 0, colorArgb, Color.FromArgb(255, 255, 153, 0).ToArgb()),
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

    Public Function Connect(ByVal fromId As String, ByVal toId As String,
                            Optional ByVal label As String = "",
                            Optional ByVal color As String = "2") As BoardEdge
        If String.IsNullOrEmpty(fromId) OrElse String.IsNullOrEmpty(toId) OrElse fromId = toId Then Return Nothing
        For Each e As BoardEdge In Edges
            If e.FromId = fromId AndAlso e.ToId = toId Then Return e
        Next
        Dim a As BoardNode = FindNode(fromId)
        Dim b As BoardNode = FindNode(toId)
        Dim fromSide As String = "right"
        Dim toSide As String = "left"
        If a IsNot Nothing AndAlso b IsNot Nothing Then
            Dim sides = InferSides(a, b)
            fromSide = sides.Item1
            toSide = sides.Item2
        End If
        Dim edge As New BoardEdge() With {
            .Id = Guid.NewGuid().ToString("N"),
            .FromId = fromId,
            .ToId = toId,
            .FromSide = fromSide,
            .ToSide = toSide,
            .FromEnd = "none",
            .ToEnd = "arrow",
            .Label = If(label, ""),
            .Color = If(String.IsNullOrEmpty(color), "2", color)
        }
        Edges.Add(edge)
        NotifyChanged()
        Return edge
    End Function

    Public Shared Function InferSides(ByVal a As BoardNode, ByVal b As BoardNode) As Tuple(Of String, String)
        Dim acx As Single = a.X + a.Width / 2.0F
        Dim acy As Single = a.Y + a.Height / 2.0F
        Dim bcx As Single = b.X + b.Width / 2.0F
        Dim bcy As Single = b.Y + b.Height / 2.0F
        Dim dx As Single = bcx - acx
        Dim dy As Single = bcy - acy
        If Math.Abs(dx) >= Math.Abs(dy) Then
            If dx >= 0 Then
                Return Tuple.Create("right", "left")
            End If
            Return Tuple.Create("left", "right")
        End If
        If dy >= 0 Then
            Return Tuple.Create("bottom", "top")
        End If
        Return Tuple.Create("top", "bottom")
    End Function

    Public Function FindNode(ByVal id As String) As BoardNode
        For Each n As BoardNode In Nodes
            If n.Id = id Then Return n
        Next
        Return Nothing
    End Function

    Public Function FindEdge(ByVal id As String) As BoardEdge
        For Each e As BoardEdge In Edges
            If e.Id = id Then Return e
        Next
        Return Nothing
    End Function

    ''' <summary>Prefer cards over groups so groups act as backgrounds.</summary>
    Public Function HitTest(ByVal worldX As Single, ByVal worldY As Single, Optional ByVal includeGroups As Boolean = True) As BoardNode
        Dim groupHit As BoardNode = Nothing
        For i As Integer = Nodes.Count - 1 To 0 Step -1
            Dim n As BoardNode = Nodes(i)
            If Not n.ContainsPoint(worldX, worldY) Then Continue For
            If n.IsGroup Then
                If includeGroups AndAlso groupHit Is Nothing Then groupHit = n
            Else
                Return n
            End If
        Next
        Return groupHit
    End Function

    Public Function HitTestEdge(ByVal worldX As Single, ByVal worldY As Single, Optional ByVal tolerance As Single = 8.0F) As BoardEdge
        Dim best As BoardEdge = Nothing
        Dim bestDist As Single = tolerance
        For Each edge As BoardEdge In Edges
            Dim a As BoardNode = FindNode(edge.FromId)
            Dim b As BoardNode = FindNode(edge.ToId)
            If a Is Nothing OrElse b Is Nothing Then Continue For
            Dim p1 As PointF = a.AnchorPoint(edge.FromSide)
            Dim p2 As PointF = b.AnchorPoint(edge.ToSide)
            Dim d As Single = DistanceToSegment(worldX, worldY, p1.X, p1.Y, p2.X, p2.Y)
            If d < bestDist Then
                bestDist = d
                best = edge
            End If
        Next
        Return best
    End Function

    Private Shared Function DistanceToSegment(ByVal px As Single, ByVal py As Single,
                                              ByVal x1 As Single, ByVal y1 As Single,
                                              ByVal x2 As Single, ByVal y2 As Single) As Single
        Dim dx As Single = x2 - x1
        Dim dy As Single = y2 - y1
        If Math.Abs(dx) < 0.0001F AndAlso Math.Abs(dy) < 0.0001F Then
            dx = px - x1
            dy = py - y1
            Return CSng(Math.Sqrt(dx * dx + dy * dy))
        End If
        Dim t As Single = ((px - x1) * dx + (py - y1) * dy) / (dx * dx + dy * dy)
        If t < 0.0F Then t = 0.0F
        If t > 1.0F Then t = 1.0F
        Dim qx As Single = x1 + t * dx
        Dim qy As Single = y1 + t * dy
        dx = px - qx
        dy = py - qy
        Return CSng(Math.Sqrt(dx * dx + dy * dy))
    End Function

    Public Function NodesInsideGroup(ByVal group As BoardNode) As List(Of BoardNode)
        Dim list As New List(Of BoardNode)
        If group Is Nothing OrElse Not group.IsGroup Then Return list
        For Each n As BoardNode In Nodes
            If Object.ReferenceEquals(n, group) Then Continue For
            Dim cx As Single = n.X + n.Width / 2.0F
            Dim cy As Single = n.Y + n.Height / 2.0F
            If group.ContainsPoint(cx, cy) Then list.Add(n)
        Next
        Return list
    End Function

    Public Sub CycleNodeColor(ByVal node As BoardNode)
        If node Is Nothing Then Return
        node.Color = CanvasColors.NextPreset(node.Color)
        NotifyChanged()
    End Sub

    Public Sub CycleEdgeColor(ByVal edge As BoardEdge)
        If edge Is Nothing Then Return
        edge.Color = CanvasColors.NextPreset(edge.Color)
        NotifyChanged()
    End Sub

    Public Function ExportSelectionJson(ByVal selected As IList(Of BoardNode)) As String
        Dim ser As New JavaScriptSerializer() With {.MaxJsonLength = Integer.MaxValue}
        Dim idSet As New HashSet(Of String)
        For Each n As BoardNode In selected
            idSet.Add(n.Id)
        Next
        Dim dto As New Dictionary(Of String, Object)
        Dim nodeList As New List(Of Dictionary(Of String, Object))
        For Each n As BoardNode In Nodes
            If Not idSet.Contains(n.Id) Then Continue For
            Dim d As New Dictionary(Of String, Object)
            d("id") = n.Id
            d("kind") = n.Kind
            d("x") = n.X
            d("y") = n.Y
            d("w") = n.Width
            d("h") = n.Height
            d("text") = n.Text
            d("path") = n.PathOrUrl
            d("color") = If(n.Color, "")
            d("label") = If(n.Label, "")
            nodeList.Add(d)
        Next
        Dim edgeList As New List(Of Dictionary(Of String, Object))
        For Each e As BoardEdge In Edges
            If Not idSet.Contains(e.FromId) OrElse Not idSet.Contains(e.ToId) Then Continue For
            Dim d As New Dictionary(Of String, Object)
            d("id") = e.Id
            d("from") = e.FromId
            d("to") = e.ToId
            d("fromSide") = e.FromSide
            d("toSide") = e.ToSide
            d("fromEnd") = e.FromEnd
            d("toEnd") = e.ToEnd
            d("label") = If(e.Label, "")
            d("color") = If(e.Color, "")
            edgeList.Add(d)
        Next
        dto("nodes") = nodeList
        dto("edges") = edgeList
        Return ser.Serialize(dto)
    End Function

    Public Function ImportSelectionJson(ByVal json As String, ByVal offsetX As Single, ByVal offsetY As Single) As List(Of BoardNode)
        Dim ser As New JavaScriptSerializer() With {.MaxJsonLength = Integer.MaxValue}
        Dim dto As Dictionary(Of String, Object) = ser.Deserialize(Of Dictionary(Of String, Object))(json)
        Dim result As New List(Of BoardNode)
        If dto Is Nothing OrElse Not dto.ContainsKey("nodes") OrElse Not TypeOf dto("nodes") Is IList Then Return result
        Dim idMap As New Dictionary(Of String, String)
        For Each item As Object In CType(dto("nodes"), IList)
            Dim d As Dictionary(Of String, Object) = TryCast(item, Dictionary(Of String, Object))
            If d Is Nothing Then Continue For
            Dim oldId As String = DictString(d, "id", Guid.NewGuid().ToString("N"))
            Dim n As BoardNode = ParseNodeDict(d, lcarsNative:=True)
            Dim newId As String = Guid.NewGuid().ToString("N")
            idMap(oldId) = newId
            n.Id = newId
            n.X += offsetX
            n.Y += offsetY
            If n.IsGroup Then
                Nodes.Insert(0, n)
            Else
                Nodes.Add(n)
            End If
            result.Add(n)
        Next
        If dto.ContainsKey("edges") AndAlso TypeOf dto("edges") Is IList Then
            For Each item As Object In CType(dto("edges"), IList)
                Dim d As Dictionary(Of String, Object) = TryCast(item, Dictionary(Of String, Object))
                If d Is Nothing Then Continue For
                Dim e As BoardEdge = ParseEdgeDict(d, lcarsNative:=True)
                If Not idMap.ContainsKey(e.FromId) OrElse Not idMap.ContainsKey(e.ToId) Then Continue For
                e.Id = Guid.NewGuid().ToString("N")
                e.FromId = idMap(e.FromId)
                e.ToId = idMap(e.ToId)
                Edges.Add(e)
            Next
        End If
        NotifyChanged()
        Return result
    End Function

    Public Sub Save(ByVal path As String)
        Dim ext As String = System.IO.Path.GetExtension(path).ToLowerInvariant()
        If ext = ".canvas" Then
            SaveObsidianCanvas(path)
        Else
            SaveLcarsCanvas(path)
        End If
    End Sub

    ''' <summary>Full board JSON for undo snapshots (includes camera + ink).</summary>
    Public Function ExportSnapshotJson() As String
        Dim ser As New JavaScriptSerializer() With {.MaxJsonLength = Integer.MaxValue}
        Dim dto As New Dictionary(Of String, Object)
        dto("format") = "lcarscanvas"
        dto("version") = 2
        dto("cameraX") = CameraX
        dto("cameraY") = CameraY
        dto("zoom") = Zoom
        dto("nodes") = SerializeNodesForLcars()
        dto("edges") = SerializeEdgesForLcars()
        dto("strokes") = SerializeStrokes()
        Return ser.Serialize(dto)
    End Function

    Public Sub ImportSnapshotJson(ByVal json As String)
        Dim ser As New JavaScriptSerializer() With {.MaxJsonLength = Integer.MaxValue}
        Dim dto As Dictionary(Of String, Object) = ser.Deserialize(Of Dictionary(Of String, Object))(json)
        If dto Is Nothing Then Return
        LoadLcarsDto(dto)
        NotifyChanged()
    End Sub

    Public Sub Load(ByVal path As String)
        Dim text As String = File.ReadAllText(path)
        Dim ser As New JavaScriptSerializer() With {.MaxJsonLength = Integer.MaxValue}
        Dim dto As Dictionary(Of String, Object) = ser.Deserialize(Of Dictionary(Of String, Object))(text)
        If dto Is Nothing Then Throw New InvalidDataException("Invalid canvas JSON.")

        ' Obsidian JSON Canvas has no cameraX; LCARS native does.
        If dto.ContainsKey("cameraX") OrElse dto.ContainsKey("strokes") Then
            LoadLcarsDto(dto)
        Else
            LoadObsidianDto(dto)
        End If
        NotifyChanged()
    End Sub

    Private Sub SaveLcarsCanvas(ByVal path As String)
        Dim ser As New JavaScriptSerializer() With {.MaxJsonLength = Integer.MaxValue}
        Dim dto As New Dictionary(Of String, Object)
        dto("format") = "lcarscanvas"
        dto("version") = 2
        dto("cameraX") = CameraX
        dto("cameraY") = CameraY
        dto("zoom") = Zoom
        dto("nodes") = SerializeNodesForLcars()
        dto("edges") = SerializeEdgesForLcars()
        dto("strokes") = SerializeStrokes()
        File.WriteAllText(path, ser.Serialize(dto), New UTF8Encoding(False))
    End Sub

    Private Sub SaveObsidianCanvas(ByVal path As String)
        Dim ser As New JavaScriptSerializer() With {.MaxJsonLength = Integer.MaxValue}
        Dim dto As New Dictionary(Of String, Object)
        dto("nodes") = SerializeNodesForObsidian()
        dto("edges") = SerializeEdgesForObsidian()
        File.WriteAllText(path, ser.Serialize(dto), New UTF8Encoding(False))
    End Sub

    Private Function SerializeNodesForLcars() As List(Of Dictionary(Of String, Object))
        Dim nodeList As New List(Of Dictionary(Of String, Object))
        For Each n As BoardNode In Nodes
            Dim d As New Dictionary(Of String, Object)
            d("id") = n.Id
            d("kind") = n.Kind
            d("type") = n.JsonCanvasType
            d("x") = n.X
            d("y") = n.Y
            d("w") = n.Width
            d("h") = n.Height
            d("text") = n.Text
            d("path") = n.PathOrUrl
            d("color") = If(n.Color, "")
            d("label") = If(n.Label, "")
            nodeList.Add(d)
        Next
        Return nodeList
    End Function

    Private Function SerializeEdgesForLcars() As List(Of Dictionary(Of String, Object))
        Dim edgeList As New List(Of Dictionary(Of String, Object))
        For Each e As BoardEdge In Edges
            Dim d As New Dictionary(Of String, Object)
            d("id") = e.Id
            d("from") = e.FromId
            d("to") = e.ToId
            d("fromSide") = e.FromSide
            d("toSide") = e.ToSide
            d("fromEnd") = e.FromEnd
            d("toEnd") = e.ToEnd
            d("label") = If(e.Label, "")
            d("color") = If(e.Color, "")
            edgeList.Add(d)
        Next
        Return edgeList
    End Function

    Private Function SerializeStrokes() As List(Of Dictionary(Of String, Object))
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
        Return strokeList
    End Function

    Private Function SerializeNodesForObsidian() As List(Of Dictionary(Of String, Object))
        Dim nodeList As New List(Of Dictionary(Of String, Object))
        For Each n As BoardNode In Nodes
            If n.Kind = "image" Then
                ' Obsidian uses type=file for images; keep path in file.
            End If
            Dim d As New Dictionary(Of String, Object)
            d("id") = n.Id
            d("type") = n.JsonCanvasType
            d("x") = CInt(Math.Round(n.X))
            d("y") = CInt(Math.Round(n.Y))
            d("width") = CInt(Math.Round(n.Width))
            d("height") = CInt(Math.Round(n.Height))
            If Not String.IsNullOrEmpty(n.Color) Then d("color") = n.Color
            Select Case n.JsonCanvasType
                Case "text"
                    d("text") = If(n.Text, "")
                Case "file"
                    d("file") = If(n.PathOrUrl, "")
                Case "link"
                    d("url") = If(String.IsNullOrEmpty(n.PathOrUrl), n.Text, n.PathOrUrl)
                Case "group"
                    Dim lbl As String = If(Not String.IsNullOrEmpty(n.Label), n.Label, n.Text)
                    If Not String.IsNullOrEmpty(lbl) Then d("label") = lbl
            End Select
            nodeList.Add(d)
        Next
        Return nodeList
    End Function

    Private Function SerializeEdgesForObsidian() As List(Of Dictionary(Of String, Object))
        Dim edgeList As New List(Of Dictionary(Of String, Object))
        For Each e As BoardEdge In Edges
            Dim d As New Dictionary(Of String, Object)
            d("id") = If(String.IsNullOrEmpty(e.Id), Guid.NewGuid().ToString("N"), e.Id)
            d("fromNode") = e.FromId
            d("toNode") = e.ToId
            If Not String.IsNullOrEmpty(e.FromSide) Then d("fromSide") = e.FromSide
            If Not String.IsNullOrEmpty(e.ToSide) Then d("toSide") = e.ToSide
            If Not String.IsNullOrEmpty(e.FromEnd) Then d("fromEnd") = e.FromEnd
            If Not String.IsNullOrEmpty(e.ToEnd) Then d("toEnd") = e.ToEnd
            If Not String.IsNullOrEmpty(e.Label) Then d("label") = e.Label
            If Not String.IsNullOrEmpty(e.Color) Then d("color") = e.Color
            edgeList.Add(d)
        Next
        Return edgeList
    End Function

    Private Sub LoadLcarsDto(ByVal dto As Dictionary(Of String, Object))
        Nodes.Clear()
        Edges.Clear()
        Strokes.Clear()
        CameraX = DictSingle(dto, "cameraX", 0)
        CameraY = DictSingle(dto, "cameraY", 0)
        Zoom = DictSingle(dto, "zoom", 1.0F)
        ClampZoom()

        If dto.ContainsKey("nodes") AndAlso TypeOf dto("nodes") Is IList Then
            For Each item As Object In CType(dto("nodes"), IList)
                Dim d As Dictionary(Of String, Object) = TryCast(item, Dictionary(Of String, Object))
                If d Is Nothing Then Continue For
                Nodes.Add(ParseNodeDict(d, lcarsNative:=True))
            Next
        End If

        If dto.ContainsKey("edges") AndAlso TypeOf dto("edges") Is IList Then
            For Each item As Object In CType(dto("edges"), IList)
                Dim d As Dictionary(Of String, Object) = TryCast(item, Dictionary(Of String, Object))
                If d Is Nothing Then Continue For
                Edges.Add(ParseEdgeDict(d, lcarsNative:=True))
            Next
        End If

        If dto.ContainsKey("strokes") AndAlso TypeOf dto("strokes") Is IList Then
            For Each item As Object In CType(dto("strokes"), IList)
                Dim d As Dictionary(Of String, Object) = TryCast(item, Dictionary(Of String, Object))
                If d Is Nothing Then Continue For
                Dim stroke As New BoardStroke() With {
                    .Id = DictString(d, "id", Guid.NewGuid().ToString("N")),
                    .ColorArgb = If(d.ContainsKey("color"), Convert.ToInt32(d("color")), Color.FromArgb(255, 255, 153, 0).ToArgb()),
                    .Width = DictSingle(d, "width", 3.0F)
                }
                If d.ContainsKey("points") AndAlso TypeOf d("points") Is IList Then
                    For Each ptObj As Object In CType(d("points"), IList)
                        Dim pd As Dictionary(Of String, Object) = TryCast(ptObj, Dictionary(Of String, Object))
                        If pd IsNot Nothing AndAlso pd.ContainsKey("x") AndAlso pd.ContainsKey("y") Then
                            stroke.Points.Add(New PointF(DictSingle(pd, "x", 0), DictSingle(pd, "y", 0)))
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
    End Sub

    Private Sub LoadObsidianDto(ByVal dto As Dictionary(Of String, Object))
        Nodes.Clear()
        Edges.Clear()
        Strokes.Clear()
        CameraX = 0
        CameraY = 0
        Zoom = 1.0F

        If dto.ContainsKey("nodes") AndAlso TypeOf dto("nodes") Is IList Then
            For Each item As Object In CType(dto("nodes"), IList)
                Dim d As Dictionary(Of String, Object) = TryCast(item, Dictionary(Of String, Object))
                If d Is Nothing Then Continue For
                Nodes.Add(ParseNodeDict(d, lcarsNative:=False))
            Next
        End If

        If dto.ContainsKey("edges") AndAlso TypeOf dto("edges") Is IList Then
            For Each item As Object In CType(dto("edges"), IList)
                Dim d As Dictionary(Of String, Object) = TryCast(item, Dictionary(Of String, Object))
                If d Is Nothing Then Continue For
                Edges.Add(ParseEdgeDict(d, lcarsNative:=False))
            Next
        End If
    End Sub

    Private Function ParseNodeDict(ByVal d As Dictionary(Of String, Object), ByVal lcarsNative As Boolean) As BoardNode
        Dim n As New BoardNode()
        n.Id = DictString(d, "id", Guid.NewGuid().ToString("N"))
        Dim typeOrKind As String = DictString(d, If(lcarsNative AndAlso d.ContainsKey("kind"), "kind", "type"), "text")
        If lcarsNative AndAlso d.ContainsKey("kind") Then
            n.Kind = DictString(d, "kind", "note")
        Else
            n.Kind = BoardNode.KindFromJsonType(typeOrKind, DictString(d, "file", ""))
        End If
        If lcarsNative Then
            n.X = DictSingle(d, "x", 0)
            n.Y = DictSingle(d, "y", 0)
            n.Width = DictSingle(d, "w", DictSingle(d, "width", 180))
            n.Height = DictSingle(d, "h", DictSingle(d, "height", 100))
            n.Text = DictString(d, "text", "")
            n.PathOrUrl = DictString(d, "path", "")
        Else
            n.X = DictSingle(d, "x", 0)
            n.Y = DictSingle(d, "y", 0)
            n.Width = DictSingle(d, "width", 180)
            n.Height = DictSingle(d, "height", 100)
            Select Case n.JsonCanvasType
                Case "text"
                    n.Text = DictString(d, "text", "")
                Case "file"
                    n.PathOrUrl = DictString(d, "file", "")
                    n.Text = System.IO.Path.GetFileName(n.PathOrUrl)
                    If IsImagePath(n.PathOrUrl) Then n.Kind = "image"
                Case "link"
                    n.PathOrUrl = DictString(d, "url", "")
                    n.Text = n.PathOrUrl
                Case "group"
                    n.Label = DictString(d, "label", "")
                    n.Text = n.Label
            End Select
        End If
        n.Color = DictString(d, "color", "")
        n.Label = DictString(d, "label", n.Label)
        If n.IsGroup AndAlso String.IsNullOrEmpty(n.Text) Then n.Text = n.Label
        If n.Width < 40 Then n.Width = 40
        If n.Height < 30 Then n.Height = 30
        Return n
    End Function

    Private Function ParseEdgeDict(ByVal d As Dictionary(Of String, Object), ByVal lcarsNative As Boolean) As BoardEdge
        Dim e As New BoardEdge()
        e.Id = DictString(d, "id", Guid.NewGuid().ToString("N"))
        If lcarsNative Then
            e.FromId = DictString(d, "from", DictString(d, "fromNode", ""))
            e.ToId = DictString(d, "to", DictString(d, "toNode", ""))
        Else
            e.FromId = DictString(d, "fromNode", "")
            e.ToId = DictString(d, "toNode", "")
        End If
        e.FromSide = DictString(d, "fromSide", "right")
        e.ToSide = DictString(d, "toSide", "left")
        e.FromEnd = DictString(d, "fromEnd", "none")
        e.ToEnd = DictString(d, "toEnd", "arrow")
        e.Label = DictString(d, "label", "")
        e.Color = DictString(d, "color", "2")
        Return e
    End Function

    Private Sub ClampZoom()
        If Zoom < 0.2F Then Zoom = 0.2F
        If Zoom > 4.0F Then Zoom = 4.0F
    End Sub

    Private Shared Function IsImagePath(ByVal path As String) As Boolean
        If String.IsNullOrEmpty(path) Then Return False
        Dim ext As String = System.IO.Path.GetExtension(path).ToLowerInvariant()
        Return ext = ".png" OrElse ext = ".jpg" OrElse ext = ".jpeg" OrElse ext = ".gif" OrElse ext = ".bmp" OrElse ext = ".webp"
    End Function

    Private Shared Function DictString(ByVal d As Dictionary(Of String, Object), ByVal key As String, ByVal fallback As String) As String
        If d Is Nothing OrElse Not d.ContainsKey(key) OrElse d(key) Is Nothing Then Return fallback
        Return Convert.ToString(d(key))
    End Function

    Private Shared Function DictSingle(ByVal d As Dictionary(Of String, Object), ByVal key As String, ByVal fallback As Single) As Single
        If d Is Nothing OrElse Not d.ContainsKey(key) OrElse d(key) Is Nothing Then Return fallback
        Return CSng(Convert.ToDouble(d(key)))
    End Function
End Class

Public Class BoardNode
    Public Property Id As String
    ''' <summary>note | link | image | file | group</summary>
    Public Property Kind As String = "note"
    Public Property X As Single
    Public Property Y As Single
    Public Property Width As Single = 160
    Public Property Height As Single = 90
    Public Property Text As String = ""
    Public Property PathOrUrl As String = ""
    ''' <summary>JSON Canvas color: "1"-"6" or "#RRGGBB".</summary>
    Public Property Color As String = ""
    ''' <summary>Group label (Obsidian); notes use Text.</summary>
    Public Property Label As String = ""

    Public ReadOnly Property IsGroup As Boolean
        Get
            Return String.Equals(Kind, "group", StringComparison.OrdinalIgnoreCase)
        End Get
    End Property

    Public ReadOnly Property JsonCanvasType As String
        Get
            Select Case Kind.ToLowerInvariant()
                Case "note", "text" : Return "text"
                Case "link" : Return "link"
                Case "group" : Return "group"
                Case "image", "file" : Return "file"
                Case Else : Return "text"
            End Select
        End Get
    End Property

    Public Shared Function KindFromJsonType(ByVal jsonType As String, ByVal filePath As String) As String
        Select Case If(jsonType, "").ToLowerInvariant()
            Case "text" : Return "note"
            Case "link" : Return "link"
            Case "group" : Return "group"
            Case "file"
                Dim ext As String = System.IO.Path.GetExtension(If(filePath, "")).ToLowerInvariant()
                If ext = ".png" OrElse ext = ".jpg" OrElse ext = ".jpeg" OrElse ext = ".gif" OrElse ext = ".bmp" OrElse ext = ".webp" Then
                    Return "image"
                End If
                Return "file"
            Case Else
                Return "note"
        End Select
    End Function

    Public Function ContainsPoint(ByVal worldX As Single, ByVal worldY As Single) As Boolean
        Return worldX >= X AndAlso worldX <= X + Width AndAlso worldY >= Y AndAlso worldY <= Y + Height
    End Function

    Public Function AnchorPoint(ByVal side As String) As PointF
        Select Case If(side, "").ToLowerInvariant()
            Case "top" : Return New PointF(X + Width / 2.0F, Y)
            Case "bottom" : Return New PointF(X + Width / 2.0F, Y + Height)
            Case "left" : Return New PointF(X, Y + Height / 2.0F)
            Case Else : Return New PointF(X + Width, Y + Height / 2.0F)
        End Select
    End Function
End Class

Public Class BoardEdge
    Public Property Id As String = ""
    Public Property FromId As String
    Public Property ToId As String
    Public Property FromSide As String = "right"
    Public Property ToSide As String = "left"
    Public Property FromEnd As String = "none"
    Public Property ToEnd As String = "arrow"
    Public Property Label As String = ""
    Public Property Color As String = "2"
End Class

Public Class BoardStroke
    Public Property Id As String
    Public Property ColorArgb As Integer
    Public Property Width As Single = 3.0F
    Public Property Points As New List(Of PointF)
End Class

''' <summary>JSON Canvas preset colors mapped to LCARS-friendly ARGB.</summary>
Public Module CanvasColors
    Public Function Resolve(ByVal colorToken As String) As Color
        Dim fallback As Color = Color.FromArgb(255, 255, 153, 0)
        If String.IsNullOrWhiteSpace(colorToken) Then Return fallback
        Dim t As String = colorToken.Trim()
        If t.StartsWith("#") AndAlso (t.Length = 7 OrElse t.Length = 9) Then
            Try
                Return ColorTranslator.FromHtml(t)
            Catch
                Return fallback
            End Try
        End If
        Select Case t
            Case "1" : Return Color.FromArgb(255, 220, 60, 60)      ' red
            Case "2" : Return Color.FromArgb(255, 255, 153, 0)     ' orange (LCARS)
            Case "3" : Return Color.FromArgb(255, 240, 220, 80)    ' yellow
            Case "4" : Return Color.FromArgb(255, 80, 180, 100)    ' green
            Case "5" : Return Color.FromArgb(255, 80, 180, 220)    ' cyan
            Case "6" : Return Color.FromArgb(255, 160, 100, 220)   ' purple
            Case Else : Return fallback
        End Select
    End Function

    Public Function NextPreset(ByVal current As String) As String
        Dim order() As String = {"1", "2", "3", "4", "5", "6"}
        Dim idx As Integer = Array.IndexOf(order, If(current, ""))
        If idx < 0 Then Return "2"
        Return order((idx + 1) Mod order.Length)
    End Function

    Public Function FillFromBorder(ByVal border As Color, ByVal alpha As Integer) As Color
        Return Color.FromArgb(alpha, border.R, border.G, border.B)
    End Function
End Module
