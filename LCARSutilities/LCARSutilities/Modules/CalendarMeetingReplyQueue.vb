Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Text
Imports System.Web.Script.Serialization
Imports Microsoft.VisualBasic.CompilerServices

    <StandardModule>
    Public NotInheritable Class CalendarMeetingReplyQueue
        Public Class ReplyRequest
            Public EventUid As String

            Public PartStat As String

            Public Organizer As String

            Public Summary As String

            Public AttendeeMailto As String

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Private Const QueueFileName As String = "calendar-meeting-reply-queue.json"

        Public Shared Sub Enqueue(eventUid As String, partStat As String, organizer As String, summary As String, attendeeMailto As String)
            Dim text As String = If(Not Equals(eventUid, Nothing), eventUid, "").Trim()
            Dim text2 As String = If(Not Equals(partStat, Nothing), partStat, "").Trim().ToUpperInvariant()
            If text.Length <> 0 AndAlso text2.Length <> 0 Then
                Dim list As List(Of ReplyRequest) = LoadQueue()
                list.Add(New ReplyRequest With {
        .EventUid = text,
        .PartStat = text2,
        .Organizer = If(Not Equals(organizer, Nothing), organizer, "").Trim(),
        .Summary = If(Not Equals(summary, Nothing), summary, "").Trim(),
        .AttendeeMailto = If(Not Equals(attendeeMailto, Nothing), attendeeMailto, "").Trim()
    })
                SaveQueue(list)
            End If
        End Sub

        Private Shared Function QueuePath() As String
            Return Path.Combine(IcsCalendarStore.DataRoot(), "calendar-meeting-reply-queue.json")
        End Function

        Private Shared Function LoadQueue() As List(Of ReplyRequest)
            Dim path As String = QueuePath()
            Dim result As List(Of ReplyRequest)
            If Not File.Exists(path) Then
                result = New List(Of ReplyRequest)()
            Else
                Try
                    Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                    Dim list = javaScriptSerializer.Deserialize(Of List(Of ReplyRequest))(File.ReadAllText(path, Encoding.UTF8))
                    result = (If(list IsNot Nothing, list, New List(Of ReplyRequest)()))
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    result = New List(Of ReplyRequest)()
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Private Shared Sub SaveQueue(items As List(Of ReplyRequest))
            If items Is Nothing Then
                Return
            End If
            Try
                Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                Call File.WriteAllText(QueuePath(), javaScriptSerializer.Serialize(items), Encoding.UTF8)
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
        End Sub
    End Class
