Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Globalization
Imports System.IO
Imports System.Net
Imports System.Security.Cryptography
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Web.Script.Serialization
Imports Microsoft.VisualBasic.CompilerServices

    <StandardModule>
    Public NotInheritable Class CalDavCalendarSync
        Private Class CalDavDto
            Public CalendarUrl As String

            Public Username As String

            Public TaskCollectionUrl As String

            Public SecondaryCalendarUrls As List(Of String)

            Public CalendarUrlLabels As Dictionary(Of String, String)

            Public PrimaryCalendarDisplayName As String

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Private Const MailRootSubdir As String = "LCARS x32\Mail"

        Private Const CalDavSettingsFile As String = "caldav-settings.json"

        Private Const CalDavSecretKey As String = "caldav"

        Public Shared LastBidirectionalSyncCount As Integer = 0

        Public Shared LastBidirectionalPushCount As Integer = 0

        Public Shared LastPrimaryEventsTagged As Integer = 0

        Public Shared LastSecondaryEventsTagged As Integer = 0

        Public Shared LastTaskEventsTagged As Integer = 0

        Public Shared LastPullMergeAdded As Integer = 0

        Public Shared LastSecondaryPullMergeAdded As Integer = 0

        Public Shared LastUrlLabelsAtPull As Integer = 0

        Public Shared LastPrimaryPullMergeAdded As Integer = 0

        Public Shared LastTaskPullMergeAdded As Integer = 0

        Public Shared Function IsTaskEvent(ev As CalendarEvent) As Boolean
            If ev Is Nothing Then
                Return False
            End If
            Return If(Not Equals(ev.Summary, Nothing), ev.Summary, "").IndexOf("[TASK]", StringComparison.OrdinalIgnoreCase) >= 0
        End Function

        Public Shared Function TryPushEvent(ev As CalendarEvent) As Boolean
            If ev Is Nothing OrElse String.IsNullOrWhiteSpace(ev.Uid) Then
                Return False
            End If
            If IsTaskEvent(ev) Then
                Return CalDavTaskPush.TryPushTask(ev)
            End If
            Dim calendarUrl As String = Nothing
            Dim username As String = Nothing
            Dim password As String = Nothing
            If Not TryLoadCalDavCredentials(calendarUrl, username, password) Then
                Return False
            End If
            Dim text As String = ResolveCalendarPushUrl(calendarUrl.Trim(), ev).Trim()
            If Not text.EndsWith("/") Then
                text += "/"
            End If
            Dim flag As Boolean = DavPut(text & ev.Uid.Trim() & ".ics", username, password, BuildEventIcs(ev))
            If flag Then
                ev.CalDavDirty = False
            End If
            Return flag
        End Function

        Public Shared Function TryDeleteEvent(ev As CalendarEvent) As Boolean
            If ev Is Nothing OrElse String.IsNullOrWhiteSpace(ev.Uid) Then
                Return False
            End If
            Dim calendarUrl As String = Nothing
            Dim username As String = Nothing
            Dim password As String = Nothing
            If Not TryLoadCalDavCredentials(calendarUrl, username, password) Then
                Return False
            End If
            Dim text As String = calendarUrl.Trim()
            If Not text.EndsWith("/") Then
                text += "/"
            End If
            Return DavDelete(text & ev.Uid.Trim() & ".ics", username, password)
        End Function

        Public Shared Function TryPullAndMerge() As Integer
            Dim calendarUrl As String = Nothing
            Dim username As String = Nothing
            Dim password As String = Nothing
            If Not TryLoadCalDavCredentials(calendarUrl, username, password) Then
                Return -1
            End If
            Dim text = DavReport(calendarUrl, username, password)
            If text.Length = 0 Then
                Return -1
            End If
            Dim list = ParseCalendarReport(text)
            Dim text2 As String = TryGetPrimaryCalendarFriendlyLabel()
            LastPrimaryEventsTagged = 0
            For Each item In list
                If item IsNot Nothing AndAlso String.IsNullOrWhiteSpace(item.CalendarName) AndAlso text2.Length > 0 Then
                    item.CalendarName = text2
                    LastPrimaryEventsTagged += 1
                End If
            Next
            Dim list2 As List(Of CalendarEvent) = IcsCalendarStore.LoadEvents()
            Dim dictionary As Dictionary(Of String, CalendarEvent) = New Dictionary(Of String, CalendarEvent)(StringComparer.OrdinalIgnoreCase)
            For Each item2 In list2
                If item2 IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(item2.Uid) Then
                    dictionary(item2.Uid) = item2
                End If
            Next
            Dim num = 0
            LastPrimaryPullMergeAdded = 0
            For Each item3 In list
                If item3 Is Nothing OrElse String.IsNullOrWhiteSpace(item3.Uid) Then
                    Continue For
                End If
                Dim calendarEvent As CalendarEvent = Nothing
                If dictionary.ContainsKey(item3.Uid) Then
                    calendarEvent = dictionary(item3.Uid)
                End If
                If calendarEvent IsNot Nothing AndAlso calendarEvent.CalDavDirty Then
                    Continue For
                End If
                If calendarEvent IsNot Nothing Then
                    item3.EwsItemId = calendarEvent.EwsItemId
                    item3.EwsChangeKey = calendarEvent.EwsChangeKey
                    item3.EwsDirty = calendarEvent.EwsDirty
                    item3.EwsLastModifiedUtc = calendarEvent.EwsLastModifiedUtc
                    If String.IsNullOrWhiteSpace(item3.RecurrenceRule) Then
                        item3.RecurrenceRule = calendarEvent.RecurrenceRule
                    End If
                    If String.IsNullOrWhiteSpace(item3.Category) Then
                        item3.Category = calendarEvent.Category
                    End If
                    If String.IsNullOrWhiteSpace(item3.CalendarName) Then
                        item3.CalendarName = calendarEvent.CalendarName
                    End If
                    If String.IsNullOrWhiteSpace(item3.Location) Then
                        item3.Location = calendarEvent.Location
                    End If
                    If String.IsNullOrWhiteSpace(item3.Organizer) Then
                        item3.Organizer = calendarEvent.Organizer
                    End If
                    If String.IsNullOrWhiteSpace(item3.AttendeeMailto) Then
                        item3.AttendeeMailto = calendarEvent.AttendeeMailto
                    End If
                    If String.IsNullOrWhiteSpace(item3.MyPartStat) Then
                        item3.MyPartStat = calendarEvent.MyPartStat
                    End If
                    If Not item3.IsMeetingInvitation Then
                        item3.IsMeetingInvitation = calendarEvent.IsMeetingInvitation
                    End If
                End If
                If Not dictionary.ContainsKey(item3.Uid) Then
                    num += 1
                    LastPrimaryPullMergeAdded += 1
                End If
                dictionary(item3.Uid) = item3
            Next
            IcsCalendarStore.SaveEvents(New List(Of CalendarEvent)(dictionary.Values))
            Dim text3 As String = TryLoadTaskCollectionUrl()
            LastTaskEventsTagged = 0
            LastTaskPullMergeAdded = 0
            If text3.Length > 0 AndAlso Not String.Equals(text3, calendarUrl.Trim(), StringComparison.OrdinalIgnoreCase) Then
                Dim text4 = DavReport(text3, username, password)
                If text4.Length > 0 Then
                    Dim text5 As String = TryGetCalendarFriendlyLabel(text3.Trim())
                    If text5.Length = 0 Then
                        text5 = "VTODO"
                    End If
                    For Each item4 In ParseCalendarReport(text4)
                        If item4 IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(item4.Uid) Then
                            If String.IsNullOrWhiteSpace(item4.CalendarName) AndAlso text5.Length > 0 Then
                                item4.CalendarName = text5
                                LastTaskEventsTagged += 1
                            ElseIf String.IsNullOrWhiteSpace(item4.CalendarName) Then
                                item4.CalendarName = text5
                            End If
                            If Not dictionary.ContainsKey(item4.Uid) Then
                                num += 1
                                LastTaskPullMergeAdded += 1
                            End If
                            dictionary(item4.Uid) = item4
                        End If
                    Next
                    IcsCalendarStore.SaveEvents(New List(Of CalendarEvent)(dictionary.Values))
                End If
            End If
            LastSecondaryEventsTagged = 0
            LastSecondaryPullMergeAdded = 0
            For Each item5 In TryLoadSecondaryCalendarUrls()
                If String.IsNullOrWhiteSpace(item5) OrElse String.Equals(item5.Trim(), calendarUrl.Trim(), StringComparison.OrdinalIgnoreCase) Then
                    Continue For
                End If
                Dim text6 = DavReport(item5, username, password)
                If text6.Length = 0 Then
                    Continue For
                End If
                Dim text7 As String = TryGetCalendarFriendlyLabel(item5.Trim())
                If text7.Length = 0 Then
                    text7 = Path.GetFileName(item5.Trim().TrimEnd("/"c))
                End If
                For Each item6 In ParseCalendarReport(text6)
                    If item6 IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(item6.Uid) Then
                        If String.IsNullOrWhiteSpace(item6.CalendarName) AndAlso text7.Length > 0 Then
                            item6.CalendarName = text7
                            LastSecondaryEventsTagged += 1
                        ElseIf String.IsNullOrWhiteSpace(item6.CalendarName) Then
                            item6.CalendarName = text7
                        End If
                        If Not dictionary.ContainsKey(item6.Uid) Then
                            num += 1
                            LastSecondaryPullMergeAdded += 1
                        End If
                        dictionary(item6.Uid) = item6
                    End If
                Next
                IcsCalendarStore.SaveEvents(New List(Of CalendarEvent)(dictionary.Values))
            Next
            LastPullMergeAdded = Math.Max(0, num)
            LastUrlLabelsAtPull = CountKnownCalendarUrlLabels()
            Return num
        End Function

        Private Shared Function TryLoadSecondaryCalendarUrls() As List(Of String)
            Dim path As String = IO.Path.Combine(MailDataRoot(), "caldav-settings.json")
            Dim result As List(Of String)
            If Not File.Exists(path) Then
                result = New List(Of String)()
            Else
                Try
                    Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                    Dim calDavDto = javaScriptSerializer.Deserialize(Of CalDavDto)(File.ReadAllText(path, Encoding.UTF8))
                    result = (If(calDavDto IsNot Nothing AndAlso calDavDto.SecondaryCalendarUrls IsNot Nothing, calDavDto.SecondaryCalendarUrls, New List(Of String)()))
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    result = New List(Of String)()
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Private Shared Function TryLoadTaskCollectionUrl() As String
            Dim path As String = IO.Path.Combine(MailDataRoot(), "caldav-settings.json")
            Dim result As String
            If Not File.Exists(path) Then
                result = ""
            Else
                Try
                    Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                    Dim calDavDto = javaScriptSerializer.Deserialize(Of CalDavDto)(File.ReadAllText(path, Encoding.UTF8))
                    If calDavDto Is Nothing Then
                        result = ""
                    Else
                        Dim calDavDto2 = calDavDto
                        result = If(Not Equals(calDavDto2.TaskCollectionUrl, Nothing), calDavDto2.TaskCollectionUrl, "").Trim()
                    End If
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    result = ""
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Private Shared Function ResolveCalendarPushUrl(primaryUrl As String, ev As CalendarEvent) As String
            Dim result As String = If(Not Equals(primaryUrl, Nothing), primaryUrl, "").Trim()
            If ev Is Nothing Then
                Return result
            End If
            If IsTaskEvent(ev) Then
                Dim text As String = TryLoadTaskCollectionUrl()
                If text.Length > 0 Then
                    Return text
                End If
            End If
            Dim text2 As String = If(Not Equals(ev.CalendarName, Nothing), ev.CalendarName, "").Trim()
            If text2.Length = 0 Then
                Return result
            End If
            For Each item In TryLoadSecondaryCalendarUrls()
                If Not String.IsNullOrWhiteSpace(item) Then
                    Dim fileName As String = Path.GetFileName(item.Trim().TrimEnd("/"c))
                    If text2.Equals(fileName, StringComparison.OrdinalIgnoreCase) Then
                        Return item.Trim()
                    End If
                End If
            Next
            Return result
        End Function

        Public Shared Function TrySyncBidirectional() As Integer
            Dim num As Integer = TryPullAndMerge()
            If num < 0 Then
                Return num
            End If
            Dim num2 = 0
            For Each item In IcsCalendarStore.LoadEvents()
                If item IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(item.Uid) AndAlso item.CalDavDirty AndAlso TryPushEvent(item) Then
                    num2 = num2 + 1
                End If
            Next
            LastBidirectionalPushCount = Math.Max(0, num2)
            LastBidirectionalSyncCount = Math.Max(num, LastBidirectionalPushCount)
            Return LastBidirectionalSyncCount
        End Function

        Private Shared Function BuildEventIcs(ev As CalendarEvent) As String
            Dim stringBuilder As StringBuilder = New StringBuilder()
            stringBuilder.AppendLine("BEGIN:VCALENDAR")
            stringBuilder.AppendLine("VERSION:2.0")
            stringBuilder.AppendLine("PRODID:-//LCARS Calendar//CalDAV//EN")
            stringBuilder.AppendLine("BEGIN:VEVENT")
            stringBuilder.AppendLine("UID:" & ev.Uid)
            If Date.Compare(ev.RecurrenceIdUtc, Date.MinValue) <> 0 Then
                stringBuilder.AppendLine("RECURRENCE-ID:" & ev.RecurrenceIdUtc.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture))
            End If
            stringBuilder.AppendLine("SUMMARY:" & If(Not Equals(ev.Summary, Nothing), ev.Summary, ""))
            If Not String.IsNullOrWhiteSpace(ev.Location) Then
                stringBuilder.AppendLine("LOCATION:" & ev.Location)
            End If
            stringBuilder.AppendLine("DTSTART:" & ev.StartUtc.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture))
            stringBuilder.AppendLine("DTEND:" & ev.EndUtc.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture))
            If Not String.IsNullOrWhiteSpace(ev.RecurrenceRule) Then
                stringBuilder.AppendLine("RRULE:" & ev.RecurrenceRule.Trim())
            End If
            AppendExDateLines(stringBuilder, ev)
            If Not String.IsNullOrWhiteSpace(ev.Category) Then
                stringBuilder.AppendLine("CATEGORIES:" & ev.Category.Trim())
            End If
            If Not String.IsNullOrWhiteSpace(ev.CalendarName) Then
                stringBuilder.AppendLine("X-LCARS-CALNAME:" & ev.CalendarName.Trim())
            End If
            If ev.AlarmMinutesBefore > 0 Then
                stringBuilder.AppendLine("BEGIN:VALARM")
                stringBuilder.AppendLine("TRIGGER:-PT" & ev.AlarmMinutesBefore.ToString() & "M")
                stringBuilder.AppendLine("ACTION:DISPLAY")
                stringBuilder.AppendLine("DESCRIPTION:LCARS")
                stringBuilder.AppendLine("END:VALARM")
            End If
            AppendMeetingIcsLines(stringBuilder, ev)
            stringBuilder.AppendLine("END:VEVENT")
            stringBuilder.AppendLine("END:VCALENDAR")
            Return stringBuilder.ToString()
        End Function

        Private Shared Sub AppendExDateLines(sb As StringBuilder, ev As CalendarEvent)
            If ev Is Nothing Then
                Return
            End If
            Dim text As String = If(Not Equals(ev.ExDateUtcCsv, Nothing), ev.ExDateUtcCsv, "").Trim()
            If text.Length = 0 Then
                Return
            End If
            Dim array = text.Split(New Char(0) {","c}, StringSplitOptions.RemoveEmptyEntries)
            For Each text2 In array
                Dim text3 As String = text2.Trim()
                If text3.Length <> 0 Then
                    sb.AppendLine("EXDATE:" & text3)
                End If
            Next
        End Sub

        Private Shared Sub AppendMeetingIcsLines(sb As StringBuilder, ev As CalendarEvent)
            If ev Is Nothing Then
                Return
            End If
            If Not String.IsNullOrWhiteSpace(ev.Organizer) Then
                sb.AppendLine("ORGANIZER:" & ev.Organizer.Trim())
            End If
            If Not String.IsNullOrWhiteSpace(ev.AttendeeMailto) Then
                Dim text As String = If(Not Equals(ev.MyPartStat, Nothing), ev.MyPartStat, "NEEDS-ACTION").Trim().ToUpperInvariant()
                If text.Length = 0 Then
                    text = "NEEDS-ACTION"
                End If
                sb.AppendLine("ATTENDEE;CUTYPE=INDIVIDUAL;ROLE=REQ-PARTICIPANT;PARTSTAT=" & text & ";RSVP=TRUE:" & ev.AttendeeMailto.Trim())
            ElseIf ev.IsMeetingInvitation AndAlso Not String.IsNullOrWhiteSpace(ev.MyPartStat) Then
                sb.AppendLine("ATTENDEE;PARTSTAT=" & ev.MyPartStat.Trim().ToUpperInvariant() & ":mailto:unknown@invalid")
            End If
        End Sub

        Private Shared Function ParseCalendarReport(xml As String) As List(Of CalendarEvent)
            Dim list As List(Of CalendarEvent) = New List(Of CalendarEvent)()
            Dim regex As Regex = New Regex("BEGIN:VEVENT[\s\S]*?END:VEVENT", RegexOptions.IgnoreCase)
            For Each item As Match In regex.Matches(xml)
                list.Add(ParseVEventBlock(item.Value))
            Next
            Dim regex2 As Regex = New Regex("BEGIN:VTODO[\s\S]*?END:VTODO", RegexOptions.IgnoreCase)
            For Each item2 As Match In regex2.Matches(xml)
                Dim calendarEvent = ParseVEventBlock(item2.Value)
                If calendarEvent IsNot Nothing Then
                    Dim calendarEvent2 = calendarEvent
                    Dim text = If(Not Equals(calendarEvent2.Summary, Nothing), calendarEvent2.Summary, "")
                    If text.IndexOf("[TASK]", StringComparison.OrdinalIgnoreCase) < 0 Then
                        calendarEvent.Summary = "[TASK] " & text
                    End If
                    Dim text2 = ExtractIcs(item2.Value, "STATUS")
                    If text2.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase) AndAlso text.IndexOf("[DONE]", StringComparison.OrdinalIgnoreCase) < 0 Then
                        calendarEvent.Summary = "[DONE] " & calendarEvent.Summary
                    End If
                    list.Add(calendarEvent)
                End If
            Next
            Return list
        End Function

        Private Shared Function ParseVEventBlock(ics As String) As CalendarEvent
            Dim calendarEvent As CalendarEvent = New CalendarEvent()
            calendarEvent.Uid = ExtractIcs(ics, "UID")
            calendarEvent.Summary = ExtractIcs(ics, "SUMMARY")
            calendarEvent.Location = ExtractIcs(ics, "LOCATION")
            calendarEvent.StartUtc = ParseIcsDate(ExtractIcs(ics, "DTSTART"))
            calendarEvent.EndUtc = ParseIcsDate(ExtractIcs(ics, "DTEND"))
            If Date.Compare(calendarEvent.StartUtc, Date.MinValue) = 0 Then
                calendarEvent.StartUtc = ParseIcsDate(ExtractIcs(ics, "DUE"))
            End If
            If Date.Compare(calendarEvent.EndUtc, Date.MinValue) = 0 AndAlso Date.Compare(calendarEvent.StartUtc, Date.MinValue) <> 0 Then
                calendarEvent.EndUtc = calendarEvent.StartUtc.AddHours(1.0)
            End If
            Dim text = ExtractIcs(ics, "TRIGGER")
            If text.StartsWith("-PT", StringComparison.OrdinalIgnoreCase) AndAlso text.EndsWith("M", StringComparison.OrdinalIgnoreCase) Then
                Dim s = text.Substring(3, text.Length - 4)
                Dim result = 0
                If Integer.TryParse(s, result) Then
                    calendarEvent.AlarmMinutesBefore = result
                End If
            End If
            Dim s2 = ExtractIcs(ics, "PRIORITY")
            Dim result2 = 0
            If Integer.TryParse(s2, result2) Then
                calendarEvent.TaskPriority = Math.Max(0, Math.Min(9, result2))
            End If
            Dim s3 = ExtractIcs(ics, "PERCENT-COMPLETE")
            Dim result3 = 0
            If Integer.TryParse(s3, result3) Then
                calendarEvent.TaskPercentComplete = Math.Max(0, Math.Min(100, result3))
            End If
            calendarEvent.RecurrenceRule = ExtractIcs(ics, "RRULE")
            calendarEvent.ExDateUtcCsv = ExtractExDateCsv(ics)
            calendarEvent.Category = ExtractIcs(ics, "CATEGORIES")
            Dim text2 = ExtractIcs(ics, "X-LCARS-CALNAME")
            If text2.Length = 0 Then
                text2 = ExtractIcs(ics, "X-WR-CALNAME")
            End If
            If text2.Length > 0 Then
                calendarEvent.CalendarName = text2.Trim()
            ElseIf Not String.IsNullOrWhiteSpace(calendarEvent.Category) Then
                calendarEvent.CalendarName = calendarEvent.Category.Trim()
            End If
            Dim text3 = ExtractIcs(ics, "STATUS")
            If text3.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase) Then
                calendarEvent.TaskCompleted = True
            End If
            ParseMeetingInvitationLines(ics, calendarEvent)
            Return calendarEvent
        End Function

        Private Shared Sub ParseMeetingInvitationLines(ics As String, ev As CalendarEvent)
            If ev Is Nothing OrElse String.IsNullOrEmpty(ics) Then
                Return
            End If
            Dim array = ics.Split(New Char(1) {ChrW(10), ChrW(13)}, StringSplitOptions.RemoveEmptyEntries)
            For Each text In array
                Dim text2 As String = text.Trim()
                If text2.StartsWith("ORGANIZER", StringComparison.OrdinalIgnoreCase) Then
                    ev.Organizer = ParseIcsLineValue(text2)
                    ev.IsMeetingInvitation = True
                ElseIf text2.StartsWith("ATTENDEE", StringComparison.OrdinalIgnoreCase) Then
                    ev.AttendeeMailto = ParseIcsLineValue(text2)
                    Dim text3 = ParseIcsLineParam(text2, "PARTSTAT")
                    If text3.Length > 0 Then
                        ev.MyPartStat = text3
                    End If
                    ev.IsMeetingInvitation = True
                End If
            Next
        End Sub

        Private Shared Function ParseIcsLineValue(line As String) As String
            Dim num = line.IndexOf(":"c)
            If num < 0 Then
                Return line.Trim()
            End If
            Return line.Substring(num + 1).Trim()
        End Function

        Private Shared Function ParseIcsLineParam(line As String, paramName As String) As String
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

        Private Shared Function ExtractExDateCsv(ics As String) As String
            If String.IsNullOrEmpty(ics) Then
                Return ""
            End If
            Dim list As List(Of String) = New List(Of String)()
            Dim array = ics.Split(New Char(1) {ChrW(10), ChrW(13)}, StringSplitOptions.RemoveEmptyEntries)
            For Each text In array
                Dim text2 As String = text.Trim()
                If text2.StartsWith("EXDATE:", StringComparison.OrdinalIgnoreCase) Then
                    Dim text3 As String = text2.Substring(7).Trim()
                    If text3.Length > 0 Then
                        list.Add(text3)
                    End If
                End If
            Next
            If list.Count = 0 Then
                Return ""
            End If
            Return String.Join(",", list.ToArray())
        End Function

        Private Shared Function ExtractIcs(ics As String, key As String) As String
            Dim array = ics.Split(New Char(1) {ChrW(10), ChrW(13)}, StringSplitOptions.RemoveEmptyEntries)
            For Each text In array
                Dim text2 As String = text.Trim()
                If text2.StartsWith(key & ":", StringComparison.OrdinalIgnoreCase) Then
                    Return text2.Substring(key.Length + 1).Trim()
                End If
            Next
            Return ""
        End Function

        Private Shared Function ParseIcsDate(raw As String) As Date
            Dim text As String = If(Not Equals(raw, Nothing), raw, "").Trim()
            Dim result As Date
            If text.Length = 0 Then
                result = Date.MinValue
            Else
                Try
                    result = (If(Not text.EndsWith("Z", StringComparison.OrdinalIgnoreCase), Date.ParseExact(text, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal).ToUniversalTime(), Date.ParseExact(text, "yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToUniversalTime()))
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    result = Date.MinValue
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Private Shared Function DavReport(url As String, username As String, password As String) As String
            Dim s = "<?xml version=""1.0"" encoding=""utf-8""?><C:calendar-query xmlns:D=""DAV:"" xmlns:C=""urn:ietf:params:xml:ns:caldav""><D:prop><D:getetag/><C:calendar-data/></D:prop><C:filter><C:comp-filter name=""VCALENDAR""><C:comp-filter name=""VEVENT""/><C:comp-filter name=""VTODO""/></C:comp-filter></C:filter></C:calendar-query>"
            Dim result As String
            Try
                Dim httpWebRequest = CType(WebRequest.Create(url), HttpWebRequest)
                httpWebRequest.Method = "REPORT"
                httpWebRequest.Credentials = New NetworkCredential(username, password)
                httpWebRequest.PreAuthenticate = True
                httpWebRequest.Timeout = 45000
                httpWebRequest.ContentType = "application/xml; charset=utf-8"
                httpWebRequest.Headers.Add("Depth", "1")
                Dim bytes = Encoding.UTF8.GetBytes(s)
                httpWebRequest.ContentLength = bytes.Length
                Using stream As Stream = httpWebRequest.GetRequestStream()
                    stream.Write(bytes, 0, bytes.Length)
                End Using
                Dim httpWebResponse As HttpWebResponse = CType(httpWebRequest.GetResponse(), HttpWebResponse)
                Dim streamReader As StreamReader = New StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8)
                result = streamReader.ReadToEnd()
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                result = ""
                Call ProjectData.ClearProjectError()
            End Try
            Return result
        End Function

        Private Shared Function DavPut(url As String, username As String, password As String, ics As String) As Boolean
            Dim result As Boolean
            Try
                Dim httpWebRequest = CType(WebRequest.Create(url), HttpWebRequest)
                httpWebRequest.Method = "PUT"
                httpWebRequest.Credentials = New NetworkCredential(username, password)
                httpWebRequest.ContentType = "text/calendar; charset=utf-8"
                Dim bytes = Encoding.UTF8.GetBytes(ics)
                httpWebRequest.ContentLength = bytes.Length
                Using stream As Stream = httpWebRequest.GetRequestStream()
                    stream.Write(bytes, 0, bytes.Length)
                End Using
                Dim httpWebResponse As HttpWebResponse = CType(httpWebRequest.GetResponse(), HttpWebResponse)
                result = If(httpWebResponse.StatusCode >= HttpStatusCode.OK AndAlso httpWebResponse.StatusCode < HttpStatusCode.MultipleChoices, True, False)
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                result = False
                Call ProjectData.ClearProjectError()
            End Try
            Return result
        End Function

        Private Shared Function DavDelete(url As String, username As String, password As String) As Boolean
            Dim result As Boolean
            Try
                Dim httpWebRequest = CType(WebRequest.Create(url), HttpWebRequest)
                httpWebRequest.Method = "DELETE"
                httpWebRequest.Credentials = New NetworkCredential(username, password)
                Dim httpWebResponse As HttpWebResponse = CType(httpWebRequest.GetResponse(), HttpWebResponse)
                Dim statusCode As Integer = httpWebResponse.StatusCode
                result = If(statusCode >= 200 AndAlso statusCode < 300 OrElse statusCode = 404, True, False)
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                result = False
                Call ProjectData.ClearProjectError()
            End Try
            Return result
        End Function

        Private Shared Function TryLoadCalDavCredentials(ByRef calendarUrl As String, ByRef username As String, ByRef password As String) As Boolean
            calendarUrl = Nothing
            username = Nothing
            password = Nothing
            Dim path As String = IO.Path.Combine(MailDataRoot(), "caldav-settings.json")
            Dim result As Boolean
            If Not File.Exists(path) Then
                result = False
            Else
                Try
                    Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                    Dim calDavDto = javaScriptSerializer.Deserialize(Of CalDavDto)(File.ReadAllText(path, Encoding.UTF8))
                    If calDavDto Is Nothing OrElse String.IsNullOrWhiteSpace(calDavDto.CalendarUrl) Then
                        result = False
                    Else
                        calendarUrl = calDavDto.CalendarUrl.Trim()
                        Dim calDavDto2 = calDavDto
                        username = If(Not Equals(calDavDto2.Username, Nothing), calDavDto2.Username, "").Trim()
                        result = TryReadMailSecret("caldav", password)
                    End If
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    result = False
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Private Shared Function MailDataRoot() As String
            Return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LCARS x32\Mail")
        End Function

        Private Shared Function TryReadMailSecret(secretKey As String, ByRef password As String) As Boolean
            password = Nothing
            Dim path As String = IO.Path.Combine(MailDataRoot(), "secrets", SanitizeToken(secretKey) & ".dpapi")
            Dim result As Boolean
            If Not File.Exists(path) Then
                result = False
            Else
                Try
                    Dim bytes = ProtectedData.Unprotect(File.ReadAllBytes(path), Nothing, DataProtectionScope.CurrentUser)
                    password = Encoding.UTF8.GetString(bytes)
                    result = True
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    result = False
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Private Shared Function SanitizeToken(token As String) As String
            Dim invalidFileNameChars As Char() = Path.GetInvalidFileNameChars()
            Dim stringBuilder As StringBuilder = New StringBuilder(token.Length)
            Dim i = 0
            Dim length = token.Length

            While i < length
                Dim value = token(i)
                If Array.IndexOf(invalidFileNameChars, value) >= 0 Then
                    stringBuilder.Append("_"c)
                Else
                    stringBuilder.Append(value)
                End If

                i = i + 1
            End While
            Dim text As String = stringBuilder.ToString()
            If String.IsNullOrWhiteSpace(text) Then
                Return "account"
            End If
            Return text
        End Function

        Public Shared Function TryGetPrimaryCalendarFriendlyLabel() As String
            Dim path As String = IO.Path.Combine(MailDataRoot(), "caldav-settings.json")
            If Not File.Exists(path) Then
                Return ""
            End If
            Try
                Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                Dim calDavDto = javaScriptSerializer.Deserialize(Of CalDavDto)(File.ReadAllText(path, Encoding.UTF8))
                If calDavDto Is Nothing Then
                    Return ""
                End If
                Dim calDavDto2 = calDavDto
                Dim text As String = If(Not Equals(calDavDto2.PrimaryCalendarDisplayName, Nothing), calDavDto2.PrimaryCalendarDisplayName, "").Trim()
                If text.Length > 0 Then
                    Return text.ToUpperInvariant()
                End If
                calDavDto2 = calDavDto
                Dim text2 As String = If(Not Equals(calDavDto2.CalendarUrl, Nothing), calDavDto2.CalendarUrl, "").Trim()
                If text2.Length > 0 Then
                    Return TryGetCalendarFriendlyLabel(text2)
                End If
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
            Return ""
        End Function

        Public Shared Function CountKnownCalendarUrlLabels() As Integer
            Return If(TryLoadCalendarUrlLabels()?.Count, 0)
        End Function

        Public Shared Function TryGetCalendarFriendlyLabel(calendarNameOrUrl As String) As String
            Dim text As String = If(Not Equals(calendarNameOrUrl, Nothing), calendarNameOrUrl, "").Trim()
            If text.Length = 0 Then
                Return ""
            End If
            Dim dictionary As Dictionary(Of String, String) = TryLoadCalendarUrlLabels()
            If dictionary Is Nothing OrElse dictionary.Count = 0 Then
                Return text
            End If
            For Each item In dictionary
                Dim key = item.Key
                Dim text2 As String = If(Not Equals(key, Nothing), key, "").Trim()
                If text2.Length = 0 Then
                    Continue For
                End If
                Dim fileName As String = Path.GetFileName(text2.Trim().TrimEnd("/"c))
                If text.Equals(text2, StringComparison.OrdinalIgnoreCase) OrElse text.Equals(fileName, StringComparison.OrdinalIgnoreCase) Then
                    key = item.Value
                    Dim text3 As String = If(Not Equals(key, Nothing), key, "").Trim()
                    If text3.Length > 0 Then
                        Return text3.ToUpperInvariant()
                    End If
                End If
            Next
            Return text.ToUpperInvariant()
        End Function

        Private Shared Function TryLoadCalendarUrlLabels() As Dictionary(Of String, String)
            Dim path As String = IO.Path.Combine(MailDataRoot(), "caldav-settings.json")
            Dim result As Dictionary(Of String, String)
            If Not File.Exists(path) Then
                result = New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            Else
                Try
                    Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                    Dim calDavDto = javaScriptSerializer.Deserialize(Of CalDavDto)(File.ReadAllText(path, Encoding.UTF8))
                    result = (If(calDavDto IsNot Nothing AndAlso calDavDto.CalendarUrlLabels IsNot Nothing, calDavDto.CalendarUrlLabels, New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)))
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    result = New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function
    End Class
