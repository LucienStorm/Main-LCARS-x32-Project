' LCARSutilities/LCARSutilities/Modules/EwsCalendarDeleteQueue.vb
Option Strict On
Option Explicit On
Imports System.Collections.Generic
Imports System.IO
Imports System.Text
Imports System.Web.Script.Serialization
''' <summary>Queue EWS calendar item deletes
for LCARSMail push.</summary>
Public Module EwsCalendarDeleteQueue
Private Const QueueFileName As String = "ews-delete-queue.json"
Public Sub Enqueue(ByVal ewsItemId As String)
Dim id As String = If(ewsItemId, "").Trim()
If id.Length = 0 Then Return
Dim list As List(Of String) = LoadQueue()
If list.Contains(id) Then Return list.Add(id) SaveQueue(list)
End Sub
Private Function QueuePath() As String
Return Path.Combine(IcsCalendarStore.DataRoot(), QueueFileName)
End Function
Private Function LoadQueue() As List(Of String)
Dim path As String = QueuePath()
If Not File.Exists(path) Then Return New List(Of String)()
Try
Dim ser As New JavaScriptSerializer()
Dim list As List(Of String) = ser.Deserialize(Of List(Of String))(File.ReadAllText(path, Encoding.UTF8))
If list Is Nothing Then Return New List(Of String)()
Return list
Catch
Return New List(Of String)()
End Try
End Function
Private Sub SaveQueue(ByVal ids As List(Of String))
If ids Is Nothing Then Return
Try
Dim ser As New JavaScriptSerializer()
File.WriteAllText(QueuePath(), ser.Serialize(ids), Encoding.UTF8)
Catch
End Try
End Sub
End Module
