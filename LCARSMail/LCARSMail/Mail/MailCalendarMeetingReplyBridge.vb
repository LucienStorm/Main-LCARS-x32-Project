Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Net
Imports System.Net.Mail
Imports System.Text
Imports System.Web.Script.Serialization
Imports Microsoft.VisualBasic.CompilerServices
    Public NotInheritable Class MailCalendarMeetingReplyBridge
        Private Class ReplyDto
            Public EventUid As String

            Public PartStat As String

            Public Organizer As String

            Public Summary As String

            Public AttendeeMailto As String

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Public Shared LastSentCount As Integer = 0

        Private Const QueueFileName As String = "calendar-meeting-reply-queue.json"

        Private Sub New()
        End Sub

        Public Shared Function TryFlushQueuedMeetingReplies(smtpHost As String, smtpPort As Integer, fromAddress As String, smtpUser As String, smtpPassword As String) As Integer
            LastSentCount = 0
            Dim text As String = If(Not Equals(smtpHost, Nothing), smtpHost, "").Trim()
            Dim text2 As String = If(Not Equals(fromAddress, Nothing), fromAddress, "").Trim()
            If text.Length = 0 OrElse text2.Length = 0 Then
                Return 0
            End If
            Dim list As List(Of ReplyDto) = TakePendingReplies()
            If list Is Nothing OrElse list.Count = 0 Then
                Return 0
            End If
            Dim num = 0
            For Each item In list
                If item IsNot Nothing AndAlso TrySendOneReply(item, text, smtpPort, text2, smtpUser, smtpPassword) Then
                    num = num + 1
                End If
            Next
            LastSentCount = num
            Return num
        End Function

        Private Shared Function TakePendingReplies() As List(Of ReplyDto)
            Dim path As String = IO.Path.Combine(LcarsCalendarBridge.CalendarDataRoot(), "calendar-meeting-reply-queue.json")
            Dim result As List(Of ReplyDto)
            If Not File.Exists(path) Then
                result = New List(Of ReplyDto)()
            Else
                Try
                    Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                    Dim list = javaScriptSerializer.Deserialize(Of List(Of ReplyDto))(File.ReadAllText(path, Encoding.UTF8))
                    File.Delete(path)
                    result = (If(list IsNot Nothing, list, New List(Of ReplyDto)()))
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    result = New List(Of ReplyDto)()
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Private Shared Function TrySendOneReply(req As ReplyDto, smtpHost As String, smtpPort As Integer, fromAddress As String, smtpUser As String, smtpPassword As String) As Boolean
            Dim text = NormalizeSmtp(MailAttendeeHintBridge.NormalizeOrganizerSmtp(req.Organizer))
            Dim result As Boolean
            If text.Length = 0 Then
                result = False
            Else
                Dim replyDto = req
                Dim text2 As String = If(Not Equals(replyDto.PartStat, Nothing), replyDto.PartStat, "ACCEPTED").Trim().ToUpperInvariant()
                replyDto = req
                Dim subject As String = "RE: " & If(Not Equals(replyDto.Summary, Nothing), replyDto.Summary, "Meeting").Trim() & " — " & text2
                Dim s = BuildReplyIcs(req, text2)
                Try
                    Using mailMessage As MailMessage = New MailMessage(fromAddress, text)
                        mailMessage.Subject = subject
                        mailMessage.Body = "PARTSTAT=" & text2
                        Dim item As AlternateView = New AlternateView(New MemoryStream(Encoding.UTF8.GetBytes(s)), "text/calendar; method=REPLY")
                        mailMessage.AlternateViews.Add(item)
                        Dim smtpClient As SmtpClient = New SmtpClient(smtpHost, Math.Max(1, smtpPort))
                        smtpClient.EnableSsl = If(smtpPort = 587 OrElse smtpPort = 465, True, False)
                        If Not String.IsNullOrEmpty(smtpUser) Then
                            smtpClient.Credentials = New NetworkCredential(smtpUser, smtpPassword)
                        End If
                        smtpClient.Send(mailMessage)
                    End Using
                    result = True
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    result = False
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Private Shared Function BuildReplyIcs(req As ReplyDto, partStat As String) As String
            Dim replyDto = req
            Dim text As String = (If(Not Equals(replyDto.EventUid, Nothing), replyDto.EventUid, Guid.NewGuid().ToString())).Trim()
            replyDto = req
            Dim text2 As String = If(Not Equals(replyDto.AttendeeMailto, Nothing), replyDto.AttendeeMailto, "").Trim()
            If text2.Length = 0 Then
                text2 = "mailto:unknown@invalid"
            End If
            Dim stringBuilder As StringBuilder = New StringBuilder()
            stringBuilder.AppendLine("BEGIN:VCALENDAR")
            stringBuilder.AppendLine("VERSION:2.0")
            stringBuilder.AppendLine("METHOD:REPLY")
            stringBuilder.AppendLine("BEGIN:VEVENT")
            stringBuilder.AppendLine("UID:" & text)
            replyDto = req
            stringBuilder.AppendLine("SUMMARY:" & If(Not Equals(replyDto.Summary, Nothing), replyDto.Summary, ""))
            stringBuilder.AppendLine("ATTENDEE;PARTSTAT=" & partStat & ":" & text2)
            stringBuilder.AppendLine("END:VEVENT")
            stringBuilder.AppendLine("END:VCALENDAR")
            Return stringBuilder.ToString()
        End Function

        Private Shared Function NormalizeSmtp(addr As String) As String
            Dim text As String = If(Not Equals(addr, Nothing), addr, "").Trim()
            If text.Length = 0 Then
                Return ""
            End If
            If text.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) Then
                text = text.Substring(7)
            End If
            Return text.Trim()
        End Function
    End Class

