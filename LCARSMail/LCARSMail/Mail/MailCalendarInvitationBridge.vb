Imports System
Imports System.Collections.Generic
Imports System.Globalization
Imports System.Text.RegularExpressions
Imports System.Threading
Imports Microsoft.VisualBasic.CompilerServices
    Public NotInheritable Class MailCalendarInvitationBridge
        Private Shared ReadOnly IcsBlockRx As Regex = New Regex("BEGIN:VCALENDAR[\s\S]*?END:VCALENDAR", RegexOptions.IgnoreCase Or RegexOptions.Compiled)

        Public Shared LastImportedCount As Integer = 0

        Public Shared LastCalDavPushFromInvite As Integer = 0

        Public Shared LastImportedWithAlarm As Integer = 0

        Public Shared LastImportedWithRecurrence As Integer = 0

        Public Shared LastImportedWithCategory As Integer = 0

        Private Sub New()
        End Sub

        Public Shared Function TryImportFromMessageBody(body As MailMessageBody) As Integer
            LastImportedCount = 0
            If body Is Nothing Then
                Return 0
            End If
            Dim num = 0
            num += ImportInvitationBlocksFromText(body.TextBody)
            If body.IcsCalendarParts IsNot Nothing Then
                For Each icsCalendarPart In body.IcsCalendarParts
                    num += ImportInvitationBlocksFromText(icsCalendarPart)
                Next
            End If
            LastImportedCount = num
            Return num
        End Function

        Public Shared Function TryImportInvitationBlocksFromText(mailBody As String) As Integer
            Return CSharpImpl.__Assign(LastImportedCount, ImportInvitationBlocksFromText(mailBody))
        End Function

        Private Shared Function ImportInvitationBlocksFromText(mailBody As String) As Integer
            If String.IsNullOrWhiteSpace(mailBody) Then
                Return 0
            End If
            Dim num = 0
            For Each item As Match In IcsBlockRx.Matches(mailBody)
                If item IsNot Nothing AndAlso item.Success Then
                    Dim value = item.Value
                    If value.IndexOf("BEGIN:VEVENT", StringComparison.OrdinalIgnoreCase) >= 0 AndAlso value.IndexOf("METHOD:CANCEL", StringComparison.OrdinalIgnoreCase) < 0 AndAlso (value.IndexOf("METHOD:REQUEST", StringComparison.OrdinalIgnoreCase) >= 0 OrElse value.IndexOf("ORGANIZER", StringComparison.OrdinalIgnoreCase) >= 0) AndAlso TryMergeIcsIntoSharedCalendar(value) Then
                        num = num + 1
                    End If
                End If
            Next
            Return num
        End Function

        Public Shared Function TryScanIndexedBodiesForInvitations(accountId As String, Optional maxMessages As Integer = 48) As Integer
            If String.IsNullOrWhiteSpace(accountId) Then
                Return 0
            End If
            Dim list = MailSearchIndex.ListEntriesWithBodyPreview(accountId, maxMessages)
            Dim num = 0
            For Each item In list
                If item IsNot Nothing Then
                    Dim text = MailSearchIndex.TryGetBodyPreviewSnippet(accountId, item.Folder, item.Uid, 4096)
                    If text.Length <> 0 Then
                        num = num + TryImportInvitationBlocksFromText(text)
                    End If
                End If
            Next
            LastImportedCount = num
            Return num
        End Function

        Private Shared Function TryMergeIcsIntoSharedCalendar(ics As String) As Boolean
            If String.IsNullOrWhiteSpace(ics) Then
                Return False
            End If
            Dim list = ParseIcsToSharedEvents(ics)
            If list Is Nothing OrElse list.Count = 0 Then
                Return False
            End If
            Dim list2 As List(Of LcarsCalendarBridge.SharedCalendarEvent) = LcarsCalendarBridge.LoadEvents()
            Dim dictionary As Dictionary(Of String, LcarsCalendarBridge.SharedCalendarEvent) = New Dictionary(Of String, LcarsCalendarBridge.SharedCalendarEvent)(StringComparer.OrdinalIgnoreCase)
            For Each item In list2
                If item IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(item.Uid) Then
                    dictionary(item.Uid) = item
                End If
            Next
            Dim flag = False
            LastImportedWithRecurrence = 0
            LastImportedWithCategory = 0
            For Each item2 In list
                If item2 Is Nothing OrElse String.IsNullOrWhiteSpace(item2.Uid) Then
                    Continue For
                End If
                item2.IsMeetingInvitation = True
                item2.CalDavDirty = True
                If Not String.IsNullOrWhiteSpace(item2.RecurrenceRule) Then
                    LastImportedWithRecurrence += 1
                End If
                If Not String.IsNullOrWhiteSpace(item2.Category) Then
                    LastImportedWithCategory += 1
                End If
                If item2.AlarmMinutesBefore > 0 Then
                    LastImportedWithAlarm += 1
                End If
                Dim sharedCalendarEvent As LcarsCalendarBridge.SharedCalendarEvent = Nothing
                If dictionary.ContainsKey(item2.Uid) Then
                    sharedCalendarEvent = dictionary(item2.Uid)
                    If sharedCalendarEvent.CalDavDirty Then
                        Continue For
                    End If
                    item2.EwsItemId = sharedCalendarEvent.EwsItemId
                    item2.EwsChangeKey = sharedCalendarEvent.EwsChangeKey
                    item2.EwsDirty = sharedCalendarEvent.EwsDirty
                    item2.EwsLastModifiedUtc = sharedCalendarEvent.EwsLastModifiedUtc
                End If
                dictionary(item2.Uid) = item2
                flag = True
            Next
            If Not flag Then
                Return False
            End If
            LcarsCalendarBridge.SaveEvents(New List(Of LcarsCalendarBridge.SharedCalendarEvent)(dictionary.Values))
            Call QueueCalDavSyncFromInvitations()
            Return True
        End Function

        Public Shared Sub QueueCalDavSyncFromInvitations()
            Call ThreadPool.QueueUserWorkItem(Sub(state)
                                                  Try
                                                      LastCalDavPushFromInvite = LcarsCalendarBridge.TrySyncStoredCalDavBidirectional()
                                                  Catch projectError As Exception
                                                      ProjectData.SetProjectError(projectError)
                                                      LastCalDavPushFromInvite = -1
                                                      Call ProjectData.ClearProjectError()
                                                  End Try
                                              End Sub)
        End Sub

        Private Shared Function ParseIcsToSharedEvents(ics As String) As List(Of LcarsCalendarBridge.SharedCalendarEvent)
            Dim list As List(Of LcarsCalendarBridge.SharedCalendarEvent) = New List(Of LcarsCalendarBridge.SharedCalendarEvent)()
            Dim text = ""
            Dim sharedCalendarEvent As LcarsCalendarBridge.SharedCalendarEvent = Nothing
            Dim flag = False
            Dim array = ics.Split(New String(1) {vbCrLf, vbLf}, StringSplitOptions.None)
            For Each text2 In array
                Dim text3 As String = text2.Trim()
                If text3.Length = 0 Then
                    Continue For
                End If
                If text3.StartsWith("X-WR-CALNAME:", StringComparison.OrdinalIgnoreCase) Then
                    text = text3.Substring(13).Trim()
                ElseIf text3.Equals("BEGIN:VALARM", StringComparison.OrdinalIgnoreCase) Then
                    flag = True
                ElseIf text3.Equals("END:VALARM", StringComparison.OrdinalIgnoreCase) Then
                    flag = False
                ElseIf text3.Equals("BEGIN:VEVENT", StringComparison.OrdinalIgnoreCase) Then
                    sharedCalendarEvent = New LcarsCalendarBridge.SharedCalendarEvent()
                    If text.Length > 0 Then
                        sharedCalendarEvent.CalendarName = text
                    End If
                    flag = False
                ElseIf text3.Equals("END:VEVENT", StringComparison.OrdinalIgnoreCase) Then
                    If sharedCalendarEvent IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(sharedCalendarEvent.Uid) Then
                        list.Add(sharedCalendarEvent)
                    End If
                    sharedCalendarEvent = Nothing
                    flag = False
                ElseIf flag AndAlso sharedCalendarEvent IsNot Nothing AndAlso text3.StartsWith("TRIGGER:", StringComparison.OrdinalIgnoreCase) Then
                    Dim num As Integer = ParseIcsTriggerMinutes(text3.Substring(8).Trim())
                    If num > 0 Then
                        sharedCalendarEvent.AlarmMinutesBefore = num
                    End If
                Else
                    If sharedCalendarEvent Is Nothing Then
                        Continue For
                    End If
                    If text3.StartsWith("UID:", StringComparison.OrdinalIgnoreCase) Then
                        sharedCalendarEvent.Uid = text3.Substring(4).Trim()
                    End If
                    If text3.StartsWith("SUMMARY:", StringComparison.OrdinalIgnoreCase) Then
                        sharedCalendarEvent.Summary = text3.Substring(8).Trim()
                    End If
                    If text3.StartsWith("LOCATION:", StringComparison.OrdinalIgnoreCase) Then
                        sharedCalendarEvent.Location = text3.Substring(9).Trim()
                    End If
                    If text3.StartsWith("DTSTART", StringComparison.OrdinalIgnoreCase) Then
                        sharedCalendarEvent.StartUtc = ParseIcsUtc(text3)
                    End If
                    If text3.StartsWith("DTEND", StringComparison.OrdinalIgnoreCase) Then
                        sharedCalendarEvent.EndUtc = ParseIcsUtc(text3)
                    End If
                    If text3.StartsWith("RRULE:", StringComparison.OrdinalIgnoreCase) Then
                        sharedCalendarEvent.RecurrenceRule = text3.Substring(6).Trim()
                    End If
                    If text3.StartsWith("CATEGORIES:", StringComparison.OrdinalIgnoreCase) Then
                        Dim text4 As String = text3.Substring(11).Trim()
                        If text4.Length > 0 Then
                            sharedCalendarEvent.Category = text4.Split(","c)(0).Trim()
                        End If
                    End If
                    If text3.StartsWith("ORGANIZER", StringComparison.OrdinalIgnoreCase) Then
                        sharedCalendarEvent.Organizer = ParseIcsValue(text3)
                        sharedCalendarEvent.IsMeetingInvitation = True
                    End If
                    If text3.StartsWith("ATTENDEE", StringComparison.OrdinalIgnoreCase) Then
                        sharedCalendarEvent.AttendeeMailto = ParseIcsValue(text3)
                        Dim text5 = ParseIcsParam(text3, "PARTSTAT")
                        If text5.Length > 0 Then
                            sharedCalendarEvent.MyPartStat = text5
                        End If
                        sharedCalendarEvent.IsMeetingInvitation = True
                    End If
                End If
            Next
            Return list
        End Function

        Private Shared Function ParseIcsTriggerMinutes(trigger As String) As Integer
            Dim text As String = If(Not Equals(trigger, Nothing), trigger, "").Trim().ToUpperInvariant()
            If text.Length = 0 Then
                Return 0
            End If
            If text.StartsWith("-PT", StringComparison.Ordinal) AndAlso text.EndsWith("M", StringComparison.Ordinal) Then
                Dim s = text.Substring(3, text.Length - 4)
                Dim result = 0
                If Integer.TryParse(s, result) AndAlso result > 0 Then
                    Return result
                End If
            End If
            If text.StartsWith("-P", StringComparison.Ordinal) AndAlso text.IndexOf("T", StringComparison.Ordinal) > 0 Then
                Dim result2 = 0
                Dim result3 = 0
                Dim result4 = 0
                Dim num = text.IndexOf("D", StringComparison.Ordinal)
                If num > 1 Then
                    Integer.TryParse(text.Substring(2, num - 2), result2)
                End If
                Dim num2 = text.IndexOf("T", StringComparison.Ordinal)
                If num2 >= 0 Then
                    Dim num3 = text.IndexOf("H", num2, StringComparison.Ordinal)
                    If num3 > num2 + 1 Then
                        Integer.TryParse(text.Substring(num2 + 1, num3 - num2 - 1), result3)
                    End If
                    Dim num4 = text.IndexOf("M", StringComparison.Ordinal)
                    If num4 > num3 + 1 Then
                        Integer.TryParse(text.Substring(num3 + 1, num4 - num3 - 1), result4)
                    End If
                End If
                Return result2 * 24 * 60 + result3 * 60 + result4
            End If
            Return 0
        End Function

        Private Shared Function ParseIcsUtc(line As String) As Date
            Dim num = line.IndexOf(":"c)
            If num < 0 Then
                Return Date.UtcNow
            End If
            Dim text As String = line.Substring(num + 1).Trim()
            Try
                If text.EndsWith("Z", StringComparison.OrdinalIgnoreCase) Then
                    Return Date.ParseExact(text, "yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToUniversalTime()
                End If
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
            Return Date.UtcNow
        End Function

        Private Shared Function ParseIcsValue(line As String) As String
            Dim num = line.IndexOf(":"c)
            If num < 0 Then
                Return line.Trim()
            End If
            Return line.Substring(num + 1).Trim()
        End Function

        Private Shared Function ParseIcsParam(line As String, paramName As String) As String
            Dim text As String = line.ToUpperInvariant()
            Dim text2 As String = paramName.ToUpperInvariant() & "="
            Dim num = text.IndexOf(";" & text2, StringComparison.Ordinal)
            If num < 0 Then
                num = text.IndexOf(text2, StringComparison.Ordinal)
            End If
            If num < 0 Then
                Return ""
            End If
            num += text2.Length
            Dim num2 = text.IndexOf(";"c, num)
            If num2 < 0 Then
                num2 = text.IndexOf(":"c, num)
            End If
            If num2 < 0 Then
                num2 = text.Length
            End If
            Return line.Substring(num, num2 - num).Trim()
        End Function

        Private Class CSharpImpl
            Shared Function __Assign(Of T)(ByRef target As T, value As T) As T
                target = value
                Return value
            End Function
        End Class
    End Class

