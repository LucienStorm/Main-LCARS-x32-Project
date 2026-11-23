' LCARSutilities/LCARSutilities/Modules/CalendarMeetingReplyQueue.vb
Option Strict On
Option Explicit On
Imports System.Collections.Generic
Imports System.IO
Imports System.Text
Imports System.Web.Script.Serialization
''' <summary>Queue meeting RSVP
for mail transport.</summary>
Public Module CalendarMeetingReplyQueue
Public Class ReplyRequest
Public EventUid As String
Public PartStat As String
Public Organizer As String
Public Summary As String
Public AttendeeMailto As String
End Class
Private Const QueueFileName As String = "calendar-meeting-reply-queue.json"
Public Sub Enqueue( ByVal eventUid As String, ByVal partStat As String, ByVal organizer As String, ByVal summary As String, ByVal attendeeMailto As String)
Dim uid As String = If(eventUid, "").Trim()
Dim stat As String = If(partStat, "").Trim().ToUpperInvariant()
If uid.Length = 0 OrElse stat.Length = 0 Then Return
Dim list As List(Of ReplyRequest) = LoadQueue()
list.Add(New ReplyRequest() With { .EventUid = uid, .PartStat = stat, .Organizer = If(organizer, "").Trim(), .Summary = If(summary, "").Trim(), .AttendeeMailto = If(attendeeMailto, "").Trim() }) SaveQueue(list)
End Sub
Private Function QueuePath() As String
Return Path.Combine(IcsCalendarStore.DataRoot(), QueueFileName)
End Function
Private Function LoadQueue() As List(Of ReplyRequest)
Dim path As String = QueuePath()
If Not File.Exists(path) Then Return New List(Of ReplyRequest)()
Try
Dim ser As New JavaScriptSerializer()
Dim list As List(Of ReplyRequest) = ser.Deserialize(Of List(Of ReplyRequest))(File.ReadAllText(path, Encoding.UTF8))
If list Is Nothing Then Return New List(Of ReplyRequest)()
Return list
Catch
Return New List(Of ReplyRequest)()
End Try
End Function
Private Sub SaveQueue(ByVal items As List(Of ReplyRequest))
If items Is Nothing Then Return
Try
Dim ser As New JavaScriptSerializer()
File.WriteAllText(QueuePath(), ser.Serialize(items), Encoding.UTF8)
Catch
End Try
End Sub
End Module
