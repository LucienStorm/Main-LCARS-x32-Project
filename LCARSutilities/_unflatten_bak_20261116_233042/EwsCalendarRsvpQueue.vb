' LCARSutilities/LCARSutilities/Modules/EwsCalendarRsvpQueue.vb
Option Strict On
Option Explicit On
Imports System.Collections.Generic
Imports System.IO
Imports System.Text
Imports System.Web.Script.Serialization
''' <summary>Queue meeting RSVP
for LCARSMail EWS push.</summary>
Public Module EwsCalendarRsvpQueue
Private Const QueueFileName As String = "ews-rsvp-queue.json"
Public Class RsvpRequest
Public Uid As String
Public PartStat As String
End Class
Public Sub Enqueue(ByVal eventUid As String, ByVal partStat As String)
Dim uid As String = If(eventUid, "").Trim()
Dim stat As String = If(partStat, "").Trim().ToUpperInvariant()
If uid.Length = 0 OrElse stat.Length = 0 Then Return
Dim list As List(Of RsvpRequest) = LoadQueue()
list.Add(New RsvpRequest() With {.Uid = uid, .PartStat = stat}) SaveQueue(list)
End Sub
Private Function QueuePath() As String
Return Path.Combine(IcsCalendarStore.DataRoot(), QueueFileName)
End Function
Private Function LoadQueue() As List(Of RsvpRequest)
Dim path As String = QueuePath()
If Not File.Exists(path) Then Return New List(Of RsvpRequest)()
Try
Dim ser As New JavaScriptSerializer()
Dim list As List(Of RsvpRequest) = ser.Deserialize(Of List(Of RsvpRequest))(File.ReadAllText(path, Encoding.UTF8))
If list Is Nothing Then Return New List(Of RsvpRequest)()
Return list
Catch
Return New List(Of RsvpRequest)()
End Try
End Function
Private Sub SaveQueue(ByVal items As List(Of RsvpRequest))
If items Is Nothing Then Return
Try
Dim ser As New JavaScriptSerializer()
File.WriteAllText(QueuePath(), ser.Serialize(items), Encoding.UTF8)
Catch
End Try
End Sub
End Module
