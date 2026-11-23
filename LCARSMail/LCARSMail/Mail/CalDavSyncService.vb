Imports System
Imports System.Collections.Generic
Imports System.Globalization
Imports System.IO
Imports System.Net
Imports System.Text
Imports System.Text.RegularExpressions
Imports Microsoft.VisualBasic.CompilerServices
    Public NotInheritable Class CalDavSyncService
        Public Shared LastError As String

        Private Sub New()
        End Sub

        Public Shared Function TryDiscoverVtodoCollectionUrl(calendarUrl As String, username As String, password As String) As String
            LastError = Nothing
            Dim text As String = If(Not Equals(calendarUrl, Nothing), calendarUrl, "").Trim()
            If text.Length = 0 Then
                Return ""
            End If
            Dim body = "<?xml version=""1.0"" encoding=""utf-8""?><D:propfind xmlns:D=""DAV:"" xmlns:C=""urn:ietf:params:xml:ns:caldav""><D:prop><D:resourcetype/><C:supported-calendar-component-set/></D:prop></D:propfind>"
            Dim list As List(Of String) = New List(Of String)()
            list.Add(text)
            Dim text2 = text.TrimEnd("/"c)
            Dim num = text2.LastIndexOf("/"c)
            If num > 0 Then
                list.Add(text2.Substring(0, num + 1))
            End If
            For Each item In list
                Dim text3 = DavPropfind(item, username, password, body)
                If text3.Length = 0 Then
                    Continue For
                End If
                For Each item2 As Match In New Regex("<D:response[\s\S]*?</D:response>", RegexOptions.IgnoreCase).Matches(text3)
                    Dim value = item2.Value
                    If value.IndexOf("VTODO", StringComparison.OrdinalIgnoreCase) < 0 Then
                        Continue For
                    End If
                    Dim match2 = Regex.Match(value, "<D:href[^>]*>([^<]+)</D:href>", RegexOptions.IgnoreCase)
                    If Not match2.Success Then
                        Continue For
                    End If
                    Dim text4 As String = match2.Groups(1).Value.Trim()
                    If text4.Length = 0 Then
                        Continue For
                    End If
                    If Not text4.EndsWith("/") Then
                        text4 += "/"
                    End If
                    Return CombineCalDavUrl(text, text4)
                Next
            Next
            Return ""
        End Function

        Public Shared Function EnumerateVeventCollectionUrls(calendarUrl As String, username As String, password As String) As List(Of String)
            Dim list = EnumerateVeventCalendarDescriptors(calendarUrl, username, password)
            Dim list2 As List(Of String) = New List(Of String)()
            If list Is Nothing Then
                Return list2
            End If
            For Each item In list
                If item IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(item.Url) Then
                    list2.Add(item.Url.Trim())
                End If
            Next
            Return list2
        End Function

        Public Shared Function EnumerateVeventCalendarDescriptors(calendarUrl As String, username As String, password As String) As List(Of CalDavCollectionDescriptor)
            Dim list As List(Of CalDavCollectionDescriptor) = New List(Of CalDavCollectionDescriptor)()
            LastError = Nothing
            Dim text As String = If(Not Equals(calendarUrl, Nothing), calendarUrl, "").Trim()
            If text.Length = 0 Then
                Return list
            End If
            Dim body = "<?xml version=""1.0"" encoding=""utf-8""?><D:propfind xmlns:D=""DAV:"" xmlns:C=""urn:ietf:params:xml:ns:caldav""><D:prop><D:resourcetype/><D:displayname/><C:calendar-description/><C:supported-calendar-component-set/></D:prop></D:propfind>"
            Dim list2 As List(Of String) = New List(Of String)()
            list2.Add(text)
            Dim text2 = text.TrimEnd("/"c)
            Dim num = text2.LastIndexOf("/"c)
            If num > 0 Then
                list2.Add(text2.Substring(0, num + 1))
            End If
            Dim hashSet As HashSet(Of String) = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            For Each item In list2
                Dim text3 = DavPropfind(item, username, password, body)
                If text3.Length = 0 Then
                    Continue For
                End If
                For Each item2 As Match In New Regex("<D:response[\s\S]*?</D:response>", RegexOptions.IgnoreCase).Matches(text3)
                    Dim value = item2.Value
                    If value.IndexOf("VEVENT", StringComparison.OrdinalIgnoreCase) < 0 Then
                        Continue For
                    End If
                    Dim match2 = Regex.Match(value, "<D:href[^>]*>([^<]+)</D:href>", RegexOptions.IgnoreCase)
                    If Not match2.Success Then
                        Continue For
                    End If
                    Dim text4 As String = match2.Groups(1).Value.Trim()
                    If text4.Length = 0 Then
                        Continue For
                    End If
                    If Not text4.EndsWith("/") Then
                        text4 += "/"
                    End If
                    Dim text5 = CombineCalDavUrl(text, text4)
                    If hashSet.Add(text5) Then
                        Dim text6 = ExtractDavTagText(value, "displayname")
                        If text6.Length = 0 Then
                            text6 = ExtractDavTagText(value, "calendar-description")
                        End If
                        If text6.Length = 0 Then
                            text6 = Path.GetFileName(text5.Trim().TrimEnd("/"c))
                        End If
                        list.Add(New CalDavCollectionDescriptor With {
        .Url = text5,
        .DisplayName = text6
    })
                    End If
                Next
            Next
            Return list
        End Function

        Public Shared Function TryGetCollectionDisplayName(calendarUrl As String, username As String, password As String) As String
            LastError = Nothing
            Dim text As String = If(Not Equals(calendarUrl, Nothing), calendarUrl, "").Trim()
            If text.Length = 0 Then
                Return ""
            End If
            Dim body = "<?xml version=""1.0"" encoding=""utf-8""?><D:propfind xmlns:D=""DAV:"" xmlns:C=""urn:ietf:params:xml:ns:caldav""><D:prop><D:displayname/><C:calendar-description/></D:prop></D:propfind>"
            Dim text2 = DavPropfind(text, username, password, body)
            If text2.Length = 0 Then
                Return ""
            End If
            Dim text3 = ExtractDavTagText(text2, "displayname")
            If text3.Length = 0 Then
                text3 = ExtractDavTagText(text2, "calendar-description")
            End If
            Return text3
        End Function

        Private Shared Function ExtractDavTagText(chunk As String, localName As String) As String
            If String.IsNullOrWhiteSpace(chunk) OrElse String.IsNullOrWhiteSpace(localName) Then
                Return ""
            End If
            Dim pattern = "<(?:D:|d:)?" & Regex.Escape(localName) & "[^>]*>([^<]*)</(?:D:|d:)?" & Regex.Escape(localName) & ">"
            Dim match = Regex.Match(chunk, pattern, RegexOptions.IgnoreCase)
            If Not match.Success Then
                Return ""
            End If
            Return match.Groups(1).Value.Trim()
        End Function

        Private Shared Function DavPropfind(url As String, username As String, password As String, body As String) As String
            Dim result As String
            Try
                Dim httpWebRequest = CType(WebRequest.Create(url), HttpWebRequest)
                httpWebRequest.Method = "PROPFIND"
                httpWebRequest.Credentials = New NetworkCredential(username, password)
                httpWebRequest.PreAuthenticate = True
                httpWebRequest.Timeout = 45000
                httpWebRequest.ContentType = "application/xml; charset=utf-8"
                httpWebRequest.Headers.Add("Depth", "1")
                Dim bytes = Encoding.UTF8.GetBytes(body)
                httpWebRequest.ContentLength = bytes.Length
                Using stream As Stream = httpWebRequest.GetRequestStream()
                    stream.Write(bytes, 0, bytes.Length)
                End Using
                Dim httpWebResponse As HttpWebResponse = CType(httpWebRequest.GetResponse(), HttpWebResponse)
                Dim streamReader As StreamReader = New StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8)
                result = streamReader.ReadToEnd()
            Catch ex As Exception
                ProjectData.SetProjectError(ex)
                Dim ex2 = ex
                LastError = ex2.Message
                result = ""
                Call ProjectData.ClearProjectError()
            End Try
            Return result
        End Function

        Private Shared Function CombineCalDavUrl(baseUrl As String, href As String) As String
            If href.StartsWith("http", StringComparison.OrdinalIgnoreCase) Then
                Return href
            End If
            Dim text As String = If(Not Equals(baseUrl, Nothing), baseUrl, "").Trim()
            If Not text.EndsWith("/") Then
                text += "/"
            End If
            If href.StartsWith("/") Then
                Dim result As Uri = Nothing
                If Uri.TryCreate(text, UriKind.Absolute, result) Then
                    Return result.Scheme & "://" & result.Host & (If(result.IsDefaultPort, "", (":" & result.Port.ToString()))) & href
                End If
            End If
            Return text & href.TrimStart("/"c)
        End Function

        Public Shared Function FetchEvents(calendarUrl As String, username As String, password As String) As List(Of CalDavEvent)
            LastError = Nothing
            Dim text As String = If(Not Equals(calendarUrl, Nothing), calendarUrl, "").Trim()
            If text.Length = 0 Then
                LastError = "CalDAV calendar URL is required."
                Return Nothing
            End If
            Dim body = "<?xml version=""1.0"" encoding=""utf-8""?><C:calendar-query xmlns:D=""DAV:"" xmlns:C=""urn:ietf:params:xml:ns:caldav""><D:prop><D:getetag/><C:calendar-data/></D:prop><C:filter><C:comp-filter name=""VCALENDAR""><C:comp-filter name=""VEVENT""/><C:comp-filter name=""VTODO""/></C:comp-filter></C:filter></C:calendar-query>"
            Dim text2 = DavReport(text, username, password, body)
            If text2.Length = 0 Then
                Return Nothing
            End If
            Return ParseCalendarDataBlocks(text2)
        End Function

        Public Shared Function DeleteEvent(calendarUrl As String, username As String, password As String, uid As String) As Boolean
            LastError = Nothing
            Dim text As String = If(Not Equals(uid, Nothing), uid, "").Trim()
            If text.Length = 0 Then
                LastError = "Event UID required."
                Return False
            End If
            Dim text2 As String = If(Not Equals(calendarUrl, Nothing), calendarUrl, "").Trim()
            If Not text2.EndsWith("/") Then
                text2 += "/"
            End If
            Return DavDelete(text2 & text & ".ics", username, password)
        End Function

        Public Shared Function PutEvent(calendarUrl As String, username As String, password As String, ev As CalDavEvent) As Boolean
            LastError = Nothing
            If ev Is Nothing OrElse String.IsNullOrWhiteSpace(ev.Uid) Then
                LastError = "Event UID required."
                Return False
            End If
            Dim text As String = If(Not Equals(calendarUrl, Nothing), calendarUrl, "").Trim()
            If Not text.EndsWith("/") Then
                text += "/"
            End If
            Dim url = text & ev.Uid & ".ics"
            Dim ics = BuildIcs(ev)
            Return DavPut(url, username, password, ics)
        End Function

        Public Shared Function PutTask(calendarUrl As String, username As String, password As String, ev As CalDavEvent) As Boolean
            LastError = Nothing
            If ev Is Nothing OrElse String.IsNullOrWhiteSpace(ev.Uid) Then
                LastError = "Task UID required."
                Return False
            End If
            Dim text As String = If(Not Equals(calendarUrl, Nothing), calendarUrl, "").Trim()
            If Not text.EndsWith("/") Then
                text += "/"
            End If
            Dim url = text & ev.Uid & ".ics"
            Dim ics = BuildTaskIcs(ev)
            Return DavPut(url, username, password, ics)
        End Function

        Private Shared Function ParseCalendarDataBlocks(xml As String) As List(Of CalDavEvent)
            Dim list As List(Of CalDavEvent) = New List(Of CalDavEvent)()
            Dim regex As Regex = New Regex("BEGIN:VEVENT[\s\S]*?END:VEVENT", RegexOptions.IgnoreCase)
            For Each item As Match In regex.Matches(xml)
                list.Add(ParseVEvent(item.Value))
            Next
            Dim regex2 As Regex = New Regex("BEGIN:VTODO[\s\S]*?END:VTODO", RegexOptions.IgnoreCase)
            For Each item2 As Match In regex2.Matches(xml)
                Dim calDavEvent = ParseVEvent(item2.Value)
                If calDavEvent IsNot Nothing Then
                    Dim calDavEvent2 = calDavEvent
                    Dim text = If(Not Equals(calDavEvent2.Summary, Nothing), calDavEvent2.Summary, "")
                    If Not text.StartsWith("[TASK]", StringComparison.OrdinalIgnoreCase) Then
                        calDavEvent.Summary = "[TASK] " & text
                    End If
                    If calDavEvent.TaskCompleted AndAlso Not text.StartsWith("[DONE]", StringComparison.OrdinalIgnoreCase) Then
                        calDavEvent.Summary = "[DONE] " & calDavEvent.Summary
                    End If
                    list.Add(calDavEvent)
                End If
            Next
            Return list
        End Function

        Private Shared Function ParseVEvent(ics As String) As CalDavEvent
            Dim calDavEvent As CalDavEvent = New CalDavEvent()
            calDavEvent.Summary = ExtractIcsLine(ics, "SUMMARY")
            calDavEvent.Uid = ExtractIcsLine(ics, "UID")
            Dim text = ExtractIcsLine(ics, "STATUS")
            calDavEvent.TaskCompleted = text.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase)
            Dim text2 = ExtractIcsLine(ics, "DTSTART")
            If text2.Length = 0 Then
                text2 = ExtractIcsLine(ics, "DUE")
            End If
            Dim raw = ExtractIcsLine(ics, "DTEND")
            calDavEvent.StartUtc = ParseIcsDate(text2)
            calDavEvent.EndUtc = ParseIcsDate(raw)
            If Date.Compare(calDavEvent.EndUtc, Date.MinValue) = 0 AndAlso Date.Compare(calDavEvent.StartUtc, Date.MinValue) <> 0 Then
                calDavEvent.EndUtc = calDavEvent.StartUtc.AddHours(1.0)
            End If
            calDavEvent.Location = ExtractIcsLine(ics, "LOCATION")
            calDavEvent.RecurrenceRule = ExtractIcsLine(ics, "RRULE")
            calDavEvent.ExDateUtcCsv = ExtractExDateCsvFromIcs(ics)
            calDavEvent.Category = ExtractIcsLine(ics, "CATEGORIES")
            If Not String.IsNullOrWhiteSpace(calDavEvent.Category) Then
                calDavEvent.CalendarName = calDavEvent.Category.Trim()
            End If
            Dim text3 = ExtractIcsLine(ics, "TRIGGER")
            If text3.StartsWith("-PT", StringComparison.OrdinalIgnoreCase) AndAlso text3.EndsWith("M", StringComparison.OrdinalIgnoreCase) Then
                Dim s = text3.Substring(3, text3.Length - 4)
                Dim result = 0
                If Integer.TryParse(s, result) Then
                    calDavEvent.AlarmMinutesBefore = result
                End If
            End If
            ParseMeetingLines(ics, calDavEvent)
            Return calDavEvent
        End Function

        Private Shared Sub ParseMeetingLines(ics As String, ev As CalDavEvent)
            If ev Is Nothing OrElse String.IsNullOrEmpty(ics) Then
                Return
            End If
            Dim array = ics.Split(New Char(1) {ChrW(10), ChrW(13)}, StringSplitOptions.RemoveEmptyEntries)
            For Each text In array
                Dim text2 As String = text.Trim()
                If text2.StartsWith("ORGANIZER", StringComparison.OrdinalIgnoreCase) Then
                    ev.Organizer = ParseIcsValueAfterColon(text2)
                    ev.IsMeetingInvitation = True
                ElseIf text2.StartsWith("ATTENDEE", StringComparison.OrdinalIgnoreCase) Then
                    ev.AttendeeMailto = ParseIcsValueAfterColon(text2)
                    Dim text3 = ParseIcsParam(text2, "PARTSTAT")
                    If text3.Length > 0 Then
                        ev.MyPartStat = text3
                    End If
                    ev.IsMeetingInvitation = True
                End If
            Next
        End Sub

        Private Shared Function ParseIcsValueAfterColon(line As String) As String
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

        Private Shared Sub AppendMeetingIcsLines(sb As StringBuilder, ev As CalDavEvent)
            If ev Is Nothing Then
                Return
            End If
            If Not String.IsNullOrWhiteSpace(ev.Organizer) Then
                sb.AppendLine("ORGANIZER:" & ev.Organizer.Trim())
            End If
            If Not String.IsNullOrWhiteSpace(ev.AttendeeMailto) Then
                Dim text As String = ev.AttendeeMailto.Trim()
                Dim text2 As String = If(Not Equals(ev.MyPartStat, Nothing), ev.MyPartStat, "NEEDS-ACTION").Trim().ToUpperInvariant()
                If text2.Length = 0 Then
                    text2 = "NEEDS-ACTION"
                End If
                sb.AppendLine("ATTENDEE;CUTYPE=INDIVIDUAL;ROLE=REQ-PARTICIPANT;PARTSTAT=" & text2 & ";RSVP=TRUE:" & text)
            ElseIf ev.IsMeetingInvitation AndAlso Not String.IsNullOrWhiteSpace(ev.MyPartStat) Then
                Dim text3 As String = ev.MyPartStat.Trim().ToUpperInvariant()
                sb.AppendLine("ATTENDEE;PARTSTAT=" & text3 & ":mailto:unknown@invalid")
            End If
        End Sub

        Private Shared Function BuildIcs(ev As CalDavEvent) As String
            Dim stringBuilder As StringBuilder = New StringBuilder()
            stringBuilder.AppendLine("BEGIN:VCALENDAR")
            stringBuilder.AppendLine("VERSION:2.0")
            stringBuilder.AppendLine("PRODID:-//LCARS Mail//CalDAV//EN")
            stringBuilder.AppendLine("BEGIN:VEVENT")
            stringBuilder.AppendLine("UID:" & ev.Uid)
            If Date.Compare(ev.RecurrenceIdUtc, Date.MinValue) <> 0 Then
                stringBuilder.AppendLine("RECURRENCE-ID:" & ev.RecurrenceIdUtc.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'"))
            End If
            stringBuilder.AppendLine("SUMMARY:" & If(Not Equals(ev.Summary, Nothing), ev.Summary, ""))
            If Not String.IsNullOrWhiteSpace(ev.Location) Then
                stringBuilder.AppendLine("LOCATION:" & ev.Location.Trim())
            End If
            If Not String.IsNullOrWhiteSpace(ev.RecurrenceRule) Then
                stringBuilder.AppendLine("RRULE:" & ev.RecurrenceRule.Trim())
            End If
            If Not String.IsNullOrWhiteSpace(ev.ExDateUtcCsv) Then
                Dim array = ev.ExDateUtcCsv.Split(New Char(0) {","c}, StringSplitOptions.RemoveEmptyEntries)
                For Each text In array
                    Dim text2 As String = text.Trim()
                    If text2.Length > 0 Then
                        stringBuilder.AppendLine("EXDATE:" & text2)
                    End If
                Next
            End If
            If Not String.IsNullOrWhiteSpace(ev.Category) Then
                stringBuilder.AppendLine("CATEGORIES:" & ev.Category.Trim())
            End If
            stringBuilder.AppendLine("DTSTART:" & ev.StartUtc.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'"))
            stringBuilder.AppendLine("DTEND:" & ev.EndUtc.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'"))
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

        Private Shared Function BuildTaskIcs(ev As CalDavEvent) As String
            Dim text = StripTaskDecorators(If(Not Equals(ev.Summary, Nothing), ev.Summary, ""))
            Dim stringBuilder As StringBuilder = New StringBuilder()
            stringBuilder.AppendLine("BEGIN:VCALENDAR")
            stringBuilder.AppendLine("VERSION:2.0")
            stringBuilder.AppendLine("PRODID:-//LCARS Mail//CalDAV//EN")
            stringBuilder.AppendLine("BEGIN:VTODO")
            stringBuilder.AppendLine("UID:" & ev.Uid)
            stringBuilder.AppendLine("SUMMARY:" & text)
            If ev.TaskCompleted Then
                stringBuilder.AppendLine("STATUS:COMPLETED")
                stringBuilder.AppendLine("COMPLETED:" & Date.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'"))
            Else
                stringBuilder.AppendLine("STATUS:NEEDS-ACTION")
            End If
            If Date.Compare(ev.StartUtc, Date.MinValue) <> 0 Then
                stringBuilder.AppendLine("DUE:" & ev.StartUtc.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'"))
            End If
            stringBuilder.AppendLine("END:VTODO")
            stringBuilder.AppendLine("END:VCALENDAR")
            Return stringBuilder.ToString()
        End Function

        Friend Shared Function StripTaskDecorators(summary As String) As String
            Dim text As String = If(Not Equals(summary, Nothing), summary, "").Trim()
            If text.StartsWith("[DONE]", StringComparison.OrdinalIgnoreCase) Then
                text = text.Substring(6).TrimStart()
            End If
            If text.StartsWith("[TASK]", StringComparison.OrdinalIgnoreCase) Then
                text = text.Substring(6).TrimStart()
            End If
            Return text
        End Function

        Private Shared Function ExtractExDateCsvFromIcs(ics As String) As String
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

        Private Shared Function ExtractIcsLine(ics As String, key As String) As String
            Dim array = ics.Split(New Char(1) {ChrW(10), ChrW(13)}, StringSplitOptions.RemoveEmptyEntries)
            For Each text In array
                Dim text2 As String = text.Trim()
                If text2.StartsWith(key & ":", StringComparison.OrdinalIgnoreCase) OrElse text2.StartsWith(key & ";", StringComparison.OrdinalIgnoreCase) Then
                    Dim num = text2.IndexOf(":"c)
                    If num >= 0 Then
                        Return text2.Substring(num + 1).Trim()
                    End If
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
                    result = (If(Not text.EndsWith("Z", StringComparison.OrdinalIgnoreCase), Date.ParseExact(text, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal), Date.ParseExact(text, "yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToUniversalTime()))
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    result = Date.MinValue
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Private Shared Function DavReport(url As String, username As String, password As String, body As String) As String
            Dim result As String
            Try
                Dim httpWebRequest = CType(WebRequest.Create(url), HttpWebRequest)
                httpWebRequest.Method = "REPORT"
                httpWebRequest.Credentials = New NetworkCredential(username, password)
                httpWebRequest.PreAuthenticate = True
                httpWebRequest.Timeout = 45000
                httpWebRequest.ContentType = "application/xml; charset=utf-8"
                httpWebRequest.Headers.Add("Depth", "1")
                Dim bytes = Encoding.UTF8.GetBytes(body)
                httpWebRequest.ContentLength = bytes.Length
                Using stream As Stream = httpWebRequest.GetRequestStream()
                    stream.Write(bytes, 0, bytes.Length)
                End Using
                Dim httpWebResponse As HttpWebResponse = CType(httpWebRequest.GetResponse(), HttpWebResponse)
                Dim streamReader As StreamReader = New StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8)
                result = streamReader.ReadToEnd()
            Catch ex As Exception
                ProjectData.SetProjectError(ex)
                Dim ex2 = ex
                LastError = ex2.Message
                result = ""
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
            Catch ex As Exception
                ProjectData.SetProjectError(ex)
                Dim ex2 = ex
                LastError = ex2.Message
                result = False
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
            Catch ex As Exception
                ProjectData.SetProjectError(ex)
                Dim ex2 = ex
                LastError = ex2.Message
                result = False
                Call ProjectData.ClearProjectError()
            End Try
            Return result
        End Function
    End Class

