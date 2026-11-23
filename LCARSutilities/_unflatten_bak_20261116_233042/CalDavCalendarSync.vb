' LCARSutilities/LCARSutilities/Modules/CalDavCalendarSync.vb
Option Strict On
Option Explicit On
Imports
System.Collections.Generic
Imports
System.Globalization
Imports
System.IO
Imports
System.Net
Imports
System.Security.Cryptography
Imports
System.Text
Imports
System.Text.RegularExpressions
Imports
System.Web.Script.Serialization
''' <summary>CalDAV VEVENT pull/push/delete
for LCARSCalendar.</summary>
Public Module CalDavCalendarSync
Private Const MailRootSubdir As String = "LCARS x32\Mail"
Private Const CalDavSettingsFile As String = "caldav-settings.json"
Private Const CalDavSecretKey As String = "caldav"
Public LastBidirectionalSyncCount As Integer = 0
Public LastBidirectionalPushCount As Integer = 0
Public LastPrimaryEventsTagged As Integer = 0
Public LastSecondaryEventsTagged As Integer = 0
Public LastTaskEventsTagged As Integer = 0
Public LastPullMergeAdded As Integer = 0
Public LastSecondaryPullMergeAdded As Integer = 0
Public LastUrlLabelsAtPull As Integer = 0
Public LastPrimaryPullMergeAdded As Integer = 0
Public LastTaskPullMergeAdded As Integer = 0
Public Function IsTaskEvent(ByVal ev As CalendarEvent) As Boolean
If ev Is Nothing Then Return False
Return If(ev.Summary, "").IndexOf("[TASK]", StringComparison.OrdinalIgnoreCase) >= 0
End Function
Public Function TryPushEvent(ByVal ev As CalendarEvent) As Boolean
If ev Is Nothing OrElse String.IsNullOrWhiteSpace(ev.Uid) Then Return False
If IsTaskEvent(ev) Then Return
CalDavTaskPush.TryPushTask(ev)
Dim url As String = Nothing
Dim user As String = Nothing
Dim pw As String = Nothing
If Not TryLoadCalDavCredentials(url, user, pw) Then Return False
Dim base As String = ResolveCalendarPushUrl(url.Trim(), ev).Trim()
If Not base.EndsWith("/"c) Then base &= "/"
Dim ok As Boolean = DavPut(base & ev.Uid.Trim() & ".ics", user, pw, BuildEventIcs(ev))
If ok Then ev.CalDavDirty = False
Return ok
End Function
Public Function TryDeleteEvent(ByVal ev As CalendarEvent) As Boolean
If ev Is Nothing OrElse String.IsNullOrWhiteSpace(ev.Uid) Then Return False
Dim url As String = Nothing
Dim user As String = Nothing
Dim pw As String = Nothing
If Not TryLoadCalDavCredentials(url, user, pw) Then Return False
Dim base As String = url.Trim()
If Not base.EndsWith("/"c) Then base &= "/"
Return DavDelete(base & ev.Uid.Trim() & ".ics", user, pw)
End Function
Public Function TryPullAndMerge() As Integer
Dim url As String = Nothing
Dim user As String = Nothing
Dim pw As String = Nothing
If Not TryLoadCalDavCredentials(url, user, pw) Then Return -1
Dim xml As String = DavReport(url, user, pw)
If
xml.Length = 0 Then Return -1
Dim remote As List(Of CalendarEvent) = ParseCalendarReport(xml)
Dim primaryLabel As String = TryGetPrimaryCalendarFriendlyLabel() LastPrimaryEventsTagged = 0
For Each rev As CalendarEvent In remote
If rev IsNot Nothing AndAlso String.IsNullOrWhiteSpace(rev.CalendarName) AndAlso primaryLabel.Length > 0 Then rev.CalendarName = primaryLabel LastPrimaryEventsTagged += 1
Next
Dim local As List(Of CalendarEvent) = IcsCalendarStore.LoadEvents()
Dim byUid As New Dictionary(Of String, CalendarEvent)(StringComparer.OrdinalIgnoreCase)
For Each ev As CalendarEvent In local
If ev Is Nothing OrElse String.IsNullOrWhiteSpace(ev.Uid) Then Continue
For byUid(ev.Uid) = ev
Next
Dim added As Integer = 0 LastPrimaryPullMergeAdded = 0
For Each rev As CalendarEvent In remote
If rev Is Nothing OrElse String.IsNullOrWhiteSpace(rev.Uid) Then Continue
For
Dim existing As CalendarEvent = Nothing
If
byUid.ContainsKey(rev.Uid) Then existing = byUid(rev.Uid)
If existing IsNot Nothing AndAlso existing.CalDavDirty Then Continue
For
If existing IsNot Nothing Then rev.EwsItemId = existing.EwsItemId
rev.EwsChangeKey = existing.EwsChangeKey
rev.EwsDirty = existing.EwsDirty
rev.EwsLastModifiedUtc = existing.EwsLastModifiedUtc
If
String.IsNullOrWhiteSpace(rev.RecurrenceRule) Then rev.RecurrenceRule = existing.RecurrenceRule
If
String.IsNullOrWhiteSpace(rev.Category) Then rev.Category = existing.Category
If
String.IsNullOrWhiteSpace(rev.CalendarName) Then rev.CalendarName = existing.CalendarName
If
String.IsNullOrWhiteSpace(rev.Location) Then rev.Location = existing.Location
If
String.IsNullOrWhiteSpace(rev.Organizer) Then rev.Organizer = existing.Organizer
If
String.IsNullOrWhiteSpace(rev.AttendeeMailto) Then rev.AttendeeMailto = existing.AttendeeMailto
If
String.IsNullOrWhiteSpace(rev.MyPartStat) Then rev.MyPartStat = existing.MyPartStat
If Not rev.IsMeetingInvitation Then rev.IsMeetingInvitation = existing.IsMeetingInvitation
If Not byUid.ContainsKey(rev.Uid) Then added += 1 LastPrimaryPullMergeAdded += 1
byUid(rev.Uid) = rev
Next
IcsCalendarStore.SaveEvents(New List(Of CalendarEvent)(byUid.Values))
Dim taskUrl As String = TryLoadTaskCollectionUrl() LastTaskEventsTagged = 0 LastTaskPullMergeAdded = 0
If
taskUrl.Length > 0 AndAlso Not String.Equals(taskUrl, url.Trim(), StringComparison.OrdinalIgnoreCase) Then Dim taskXml As String = DavReport(taskUrl, user, pw)
If
taskXml.Length > 0 Then Dim taskLabel As String = TryGetCalendarFriendlyLabel(taskUrl.Trim())
If
taskLabel.Length = 0 Then
taskLabel = "VTODO"
For Each rev As CalendarEvent In ParseCalendarReport(taskXml)
If rev Is Nothing OrElse String.IsNullOrWhiteSpace(rev.Uid) Then
Continue For
If
String.IsNullOrWhiteSpace(rev.CalendarName) AndAlso taskLabel.Length > 0 Then rev.CalendarName = taskLabel LastTaskEventsTagged += 1
ElseIf
String.IsNullOrWhiteSpace(rev.CalendarName) Then rev.CalendarName = taskLabel
If Not byUid.ContainsKey(rev.Uid) Then added += 1 LastTaskPullMergeAdded += 1
byUid(rev.Uid) = rev
Next
IcsCalendarStore.SaveEvents(New List(Of CalendarEvent)(byUid.Values))
End If
End If LastSecondaryEventsTagged = 0 LastSecondaryPullMergeAdded = 0
For Each calUrl As String In TryLoadSecondaryCalendarUrls()
If
String.IsNullOrWhiteSpace(calUrl) Then Continue
For
If
String.Equals(calUrl.Trim(), url.Trim(), StringComparison.OrdinalIgnoreCase) Then Continue
For
Dim extraXml As String = DavReport(calUrl, user, pw)
If
extraXml.Length = 0 Then Continue
For
Dim calLabel As String = TryGetCalendarFriendlyLabel(calUrl.Trim())
If
calLabel.Length = 0 Then calLabel = IO.Path.GetFileName(calUrl.Trim().TrimEnd("/"c))
For Each rev As CalendarEvent In ParseCalendarReport(extraXml)
If rev Is Nothing OrElse String.IsNullOrWhiteSpace(rev.Uid) Then Continue
For
If
String.IsNullOrWhiteSpace(rev.CalendarName) AndAlso calLabel.Length > 0 Then rev.CalendarName = calLabel LastSecondaryEventsTagged += 1
ElseIf
String.IsNullOrWhiteSpace(rev.CalendarName) Then rev.CalendarName = calLabel
If Not byUid.ContainsKey(rev.Uid) Then added += 1 LastSecondaryPullMergeAdded += 1
byUid(rev.Uid) = rev
Next
IcsCalendarStore.SaveEvents(New List(Of CalendarEvent)(byUid.Values))
Next LastPullMergeAdded = Math.Max(0, added) LastUrlLabelsAtPull = CountKnownCalendarUrlLabels()
Return added
End Function
Private Function TryLoadSecondaryCalendarUrls() As List(Of String)
Dim path As String = IO.Path.Combine(MailDataRoot(), CalDavSettingsFile)
If Not File.Exists(path) Then Return New List(Of String)()
Try
Dim ser As New JavaScriptSerializer()
Dim dto As CalDavDto = ser.Deserialize(Of CalDavDto)(File.ReadAllText(path, Encoding.UTF8))
If dto Is Nothing OrElse dto.SecondaryCalendarUrls Is Nothing Then Return New List(Of String)()
Return
dto.SecondaryCalendarUrls
Catch
Return New List(Of String)()
End Try
End Function
Private Function TryLoadTaskCollectionUrl() As String
Dim path As String = IO.Path.Combine(MailDataRoot(), CalDavSettingsFile)
If Not File.Exists(path) Then Return ""
Try
Dim ser As New JavaScriptSerializer()
Dim dto As CalDavDto = ser.Deserialize(Of CalDavDto)(File.ReadAllText(path, Encoding.UTF8))
If dto Is Nothing Then Return ""
Return If(dto.TaskCollectionUrl, "").Trim()
Catch
Return ""
End Try
End Function
Private Function ResolveCalendarPushUrl(ByVal primaryUrl As String, ByVal ev As CalendarEvent) As String
Dim primary As String = If(primaryUrl, "").Trim()
If ev Is Nothing Then Return primary
If IsTaskEvent(ev) Then Dim taskUrl As String = TryLoadTaskCollectionUrl()
If
taskUrl.Length > 0 Then Return taskUrl
Dim calName As String = If(ev.CalendarName, "").Trim()
If
calName.Length = 0 Then Return primary
For Each calUrl As String In TryLoadSecondaryCalendarUrls()
If
String.IsNullOrWhiteSpace(calUrl) Then Continue
For
Dim leaf As String = IO.Path.GetFileName(calUrl.Trim().TrimEnd("/"c))
If
calName.Equals(leaf, StringComparison.OrdinalIgnoreCase) Then Return
calUrl.Trim()
Next
Return primary
End Function
Public Function TrySyncBidirectional() As Integer
Dim pulled As Integer = TryPullAndMerge()
If pulled < 0 Then Return pulled
Dim pushed As Integer = 0
For Each ev As CalendarEvent In IcsCalendarStore.LoadEvents()
If ev Is Nothing OrElse String.IsNullOrWhiteSpace(ev.Uid) Then Continue
For
If Not ev.CalDavDirty Then Continue
For
If TryPushEvent(ev) Then pushed += 1
Next LastBidirectionalPushCount = Math.Max(0, pushed) LastBidirectionalSyncCount = Math.Max(pulled, LastBidirectionalPushCount)
Return LastBidirectionalSyncCount
End Function
Private Function BuildEventIcs(ByVal ev As CalendarEvent) As String
Dim sb As New StringBuilder()
sb.AppendLine("BEGIN:VCALENDAR")
sb.AppendLine("VERSION:2.0")
sb.AppendLine("PRODID:-//LCARS Calendar//CalDAV//EN")
sb.AppendLine("BEGIN:VEVENT")
sb.AppendLine("UID:" & ev.Uid)
If
ev.RecurrenceIdUtc <>
DateTime.MinValue Then sb.AppendLine("RECURRENCE-ID:" & ev.RecurrenceIdUtc.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture))
sb.AppendLine("SUMMARY:" & If(ev.Summary, ""))
If Not String.IsNullOrWhiteSpace(ev.Location) Then sb.AppendLine("LOCATION:" & ev.Location)
sb.AppendLine("DTSTART:" & ev.StartUtc.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture))
sb.AppendLine("DTEND:" & ev.EndUtc.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture))
If Not String.IsNullOrWhiteSpace(ev.RecurrenceRule) Then sb.AppendLine("RRULE:" & ev.RecurrenceRule.Trim())
AppendExDateLines(sb, ev)
If Not String.IsNullOrWhiteSpace(ev.Category) Then sb.AppendLine("CATEGORIES:" & ev.Category.Trim())
If Not String.IsNullOrWhiteSpace(ev.CalendarName) Then sb.AppendLine("X-LCARS-CALNAME:" & ev.CalendarName.Trim())
If
ev.AlarmMinutesBefore > 0 Then
sb.AppendLine("BEGIN:VALARM")
sb.AppendLine("TRIGGER:-PT" & ev.AlarmMinutesBefore.ToString() & "M")
sb.AppendLine("ACTION:DISPLAY")
sb.AppendLine("DESCRIPTION:LCARS")
sb.AppendLine("END:VALARM")
End If AppendMeetingIcsLines(sb, ev)
sb.AppendLine("END:VEVENT")
sb.AppendLine("END:VCALENDAR")
Return
sb.ToString()
End Function
Private Sub AppendExDateLines(ByVal sb As StringBuilder, ByVal ev As CalendarEvent)
If ev Is Nothing Then Return
Dim csv As String = If(ev.ExDateUtcCsv, "").Trim()
If
csv.Length = 0 Then Return
For Each part As String In csv.Split(New Char() {","c}, StringSplitOptions.RemoveEmptyEntries)
Dim stamp As String = part.Trim()
If
stamp.Length = 0 Then Continue
For
sb.AppendLine("EXDATE:" & stamp)
Next
End Sub
Private Sub AppendMeetingIcsLines(ByVal sb As StringBuilder, ByVal ev As CalendarEvent)
If ev Is Nothing Then Return
If Not String.IsNullOrWhiteSpace(ev.Organizer) Then sb.AppendLine("ORGANIZER:" & ev.Organizer.Trim())
If Not String.IsNullOrWhiteSpace(ev.AttendeeMailto) Then Dim partStat As String = If(ev.MyPartStat, "NEEDS-ACTION").Trim().ToUpperInvariant()
If
partStat.Length = 0 Then partStat = "NEEDS-ACTION"
sb.AppendLine("ATTENDEE;CUTYPE=INDIVIDUAL;ROLE=REQ-PARTICIPANT;PARTSTAT=" & partStat & ";RSVP=TRUE:" & ev.AttendeeMailto.Trim())
ElseIf
ev.IsMeetingInvitation AndAlso Not String.IsNullOrWhiteSpace(ev.MyPartStat) Then sb.AppendLine("ATTENDEE;PARTSTAT=" & ev.MyPartStat.Trim().ToUpperInvariant() & ":mailto:unknown@invalid")
End Sub
Private Function ParseCalendarReport(ByVal xml As String) As List(Of CalendarEvent)
Dim list As New List(Of CalendarEvent)()
Dim rx As New Regex("BEGIN:VEVENT[\s\S]*?END:VEVENT", RegexOptions.IgnoreCase)
For Each block As Match In rx.Matches(xml)
list.Add(ParseVEventBlock(block.Value))
Next
Dim rxTodo As New Regex("BEGIN:VTODO[\s\S]*?END:VTODO", RegexOptions.IgnoreCase)
For Each block As Match In rxTodo.Matches(xml)
Dim taskEv As CalendarEvent = ParseVEventBlock(block.Value)
If taskEv Is Nothing Then Continue
For
Dim summary As String = If(taskEv.Summary, "")
If
summary.IndexOf("[TASK]", StringComparison.OrdinalIgnoreCase) < 0 Then taskEv.Summary = "[TASK] " & summary
Dim status As String = ExtractIcs(block.Value, "STATUS")
If
status.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase) AndAlso summary.IndexOf("[DONE]", StringComparison.OrdinalIgnoreCase) < 0 Then taskEv.Summary = "[DONE] " & taskEv.Summary
list.Add(taskEv)
Next
Return list
End Function
Private Function ParseVEventBlock(ByVal ics As String) As CalendarEvent
Dim ev As New CalendarEvent()
ev.Uid = ExtractIcs(ics, "UID")
ev.Summary = ExtractIcs(ics, "SUMMARY")
ev.Location = ExtractIcs(ics, "LOCATION")
ev.StartUtc = ParseIcsDate(ExtractIcs(ics, "DTSTART"))
ev.EndUtc = ParseIcsDate(ExtractIcs(ics, "DTEND"))
If
ev.StartUtc = DateTime.MinValue Then ev.StartUtc = ParseIcsDate(ExtractIcs(ics, "DUE"))
If
ev.EndUtc = DateTime.MinValue AndAlso ev.StartUtc <>
DateTime.MinValue Then ev.EndUtc = ev.StartUtc.AddHours(1)
Dim trigger As String = ExtractIcs(ics, "TRIGGER")
If
trigger.StartsWith("-PT", StringComparison.OrdinalIgnoreCase) AndAlso trigger.EndsWith("M", StringComparison.OrdinalIgnoreCase) Then Dim mins As String = trigger.Substring(3, trigger.Length - 4)
Dim n As Integer = 0
If
Integer.TryParse(mins, n) Then ev.AlarmMinutesBefore = n
Dim priRaw As String = ExtractIcs(ics, "PRIORITY")
Dim pri As Integer = 0
If
Integer.TryParse(priRaw, pri) Then ev.TaskPriority = Math.Max(0, Math.Min(9, pri))
Dim pctRaw As String = ExtractIcs(ics, "PERCENT-COMPLETE")
Dim pct As Integer = 0
If
Integer.TryParse(pctRaw, pct) Then ev.TaskPercentComplete = Math.Max(0, Math.Min(100, pct))
ev.RecurrenceRule = ExtractIcs(ics, "RRULE")
ev.ExDateUtcCsv = ExtractExDateCsv(ics)
ev.Category = ExtractIcs(ics, "CATEGORIES")
Dim calName As String = ExtractIcs(ics, "X-LCARS-CALNAME")
If
calName.Length = 0 Then calName = ExtractIcs(ics, "X-WR-CALNAME")
If
calName.Length > 0 Then ev.CalendarName = calName.Trim()
ElseIf Not String.IsNullOrWhiteSpace(ev.Category) Then ev.CalendarName = ev.Category.Trim()
Dim status As String = ExtractIcs(ics, "STATUS")
If
status.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase) Then ev.TaskCompleted = True ParseMeetingInvitationLines(ics, ev)
Return ev
End Function
Private Sub ParseMeetingInvitationLines(ByVal ics As String, ByVal ev As CalendarEvent)
If ev Is Nothing OrElse String.IsNullOrEmpty(ics) Then Return
For Each rawLine As String In ics.Split(New Char() {ChrW(10), ChrW(13)}, StringSplitOptions.RemoveEmptyEntries)
Dim line As String = rawLine.Trim()
If
line.StartsWith("ORGANIZER", StringComparison.OrdinalIgnoreCase) Then ev.Organizer = ParseIcsLineValue(line)
ev.IsMeetingInvitation = True
ElseIf
line.StartsWith("ATTENDEE", StringComparison.OrdinalIgnoreCase) Then ev.AttendeeMailto = ParseIcsLineValue(line)
Dim partStat As String = ParseIcsLineParam(line, "PARTSTAT")
If
partStat.Length > 0 Then ev.MyPartStat = partStat
ev.IsMeetingInvitation = True
End If
Next
End Sub
Private Function ParseIcsLineValue(ByVal line As String) As String
Dim colon As Integer = line.IndexOf(":"c)
If colon < 0 Then Return
line.Trim()
Return
line.Substring(colon + 1).Trim()
End Function
Private Function ParseIcsLineParam(ByVal line As String, ByVal paramName As String) As String
Dim upper As String = line.ToUpperInvariant()
Dim token As String = paramName.ToUpperInvariant() & "="
Dim idx As Integer = upper.IndexOf(";" & token, StringComparison.Ordinal)
If idx < 0 Then idx = upper.IndexOf(token, StringComparison.Ordinal)
If idx < 0 Then Return "" idx += token.Length
Dim endIdx As Integer = upper.IndexOf(";"c, idx)
If endIdx < 0 Then endIdx = upper.IndexOf(":"c, idx)
If endIdx < 0 Then endIdx = upper.Length
Return
line.Substring(idx, endIdx - idx).Trim()
End Function
Private Function ExtractExDateCsv(ByVal ics As String) As String
If
String.IsNullOrEmpty(ics) Then Return ""
Dim parts As New List(Of String)()
For Each line As String In ics.Split(New Char() {ChrW(10), ChrW(13)}, StringSplitOptions.RemoveEmptyEntries)
Dim t As String = line.Trim()
If
t.StartsWith("EXDATE:", StringComparison.OrdinalIgnoreCase) Then Dim stamp As String = t.Substring(7).Trim()
If
stamp.Length > 0 Then parts.Add(stamp)
Next
If
parts.Count = 0 Then Return ""
Return
String.Join(",", parts.ToArray())
End Function
Private Function ExtractIcs(ByVal ics As String, ByVal key As String) As String
For Each line As String In ics.Split(New Char() {ChrW(10), ChrW(13)}, StringSplitOptions.RemoveEmptyEntries)
Dim t As String = line.Trim()
If
t.StartsWith(key & ":", StringComparison.OrdinalIgnoreCase) Then
Return
t.Substring(key.Length + 1).Trim()
End If
Next
Return ""
End Function
Private Function ParseIcsDate(ByVal raw As String) As DateTime
Dim s As String = If(raw, "").Trim()
If
s.Length = 0 Then Return
DateTime.MinValue
Try
If
s.EndsWith("Z", StringComparison.OrdinalIgnoreCase) Then
Return
DateTime.ParseExact(s, "yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToUniversalTime()
End If
Return
DateTime.ParseExact(s, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal).ToUniversalTime()
Catch
Return
DateTime.MinValue
End Try
End Function
Private Function DavReport(ByVal url As String, ByVal username As String, ByVal password As String) As String
Dim body As String = "<?xml version=""1.0"" encoding=""utf-8""?>" & "<C:calendar-query xmlns:D=""DAV:"" xmlns:C=""urn:ietf:params:xml:ns:caldav"">" & "<D:prop><D:getetag/><C:calendar-data/></D:prop>" & "<C:filter><C:comp-filter name=""VCALENDAR"">" & "<C:comp-filter name=""VEVENT""/><C:comp-filter name=""VTODO""/>" & "</C:comp-filter></C:filter>" & "</C:calendar-query>"
Try
Dim request As HttpWebRequest = CType(WebRequest.Create(url), HttpWebRequest)
request.Method = "REPORT"
request.Credentials = New NetworkCredential(username, password)
request.PreAuthenticate = True
request.Timeout = 45000
request.ContentType = "application/xml; charset=utf-8"
request.Headers.Add("Depth", "1")
Dim bytes As Byte() = Encoding.UTF8.GetBytes(body)
request.ContentLength = bytes.Length
Using stream As Stream = request.GetRequestStream()
stream.Write(bytes, 0, bytes.Length)
End Using
Using response As HttpWebResponse = CType(request.GetResponse(), HttpWebResponse)
Using reader As New StreamReader(response.GetResponseStream(), Encoding.UTF8)
Return
reader.ReadToEnd()
End Using
End Using
Catch
Return ""
End Try
End Function
Private Function DavPut(ByVal url As String, ByVal username As String, ByVal password As String, ByVal ics As String) As Boolean
Try
Dim request As HttpWebRequest = CType(WebRequest.Create(url), HttpWebRequest)
request.Method = "PUT"
request.Credentials = New NetworkCredential(username, password)
request.ContentType = "text/calendar; charset=utf-8"
Dim bytes As Byte() = Encoding.UTF8.GetBytes(ics)
request.ContentLength = bytes.Length
Using stream As Stream = request.GetRequestStream()
stream.Write(bytes, 0, bytes.Length)
End Using
Using response As HttpWebResponse = CType(request.GetResponse(), HttpWebResponse)
Return CInt(response.StatusCode) >= 200 AndAlso CInt(response.StatusCode) < 300
End Using
Catch
Return False
End Try
End Function
Private Function DavDelete(ByVal url As String, ByVal username As String, ByVal password As String) As Boolean
Try
Dim request As HttpWebRequest = CType(WebRequest.Create(url), HttpWebRequest)
request.Method = "DELETE"
request.Credentials = New NetworkCredential(username, password)
Using response As HttpWebResponse = CType(request.GetResponse(), HttpWebResponse)
Dim code As Integer = CInt(response.StatusCode)
Return code >= 200 AndAlso code < 300 OrElse code = 404
End Using
Catch
Return False
End Try
End Function
Private Function TryLoadCalDavCredentials(ByRef calendarUrl As String, ByRef username As String, ByRef password As String) As Boolean calendarUrl = Nothing username = Nothing password = Nothing
Dim path As String = IO.Path.Combine(MailDataRoot(), CalDavSettingsFile)
If Not File.Exists(path) Then Return False
Try
Dim ser As New JavaScriptSerializer()
Dim dto As CalDavDto = ser.Deserialize(Of CalDavDto)(File.ReadAllText(path, Encoding.UTF8))
If dto Is Nothing OrElse String.IsNullOrWhiteSpace(dto.CalendarUrl) Then Return False calendarUrl = dto.CalendarUrl.Trim() username = If(dto.Username, "").Trim()
Return TryReadMailSecret(CalDavSecretKey, password)
Catch
Return False
End Try
End Function
Private Function MailDataRoot() As String
Return
IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), MailRootSubdir)
End Function
Private Function TryReadMailSecret(ByVal secretKey As String, ByRef password As String) As Boolean password = Nothing
Dim path As String = IO.Path.Combine(MailDataRoot(), "secrets", SanitizeToken(secretKey) & ".dpapi")
If Not File.Exists(path) Then Return False
Try
Dim plain As Byte() = ProtectedData.Unprotect(File.ReadAllBytes(path), Nothing, DataProtectionScope.CurrentUser) password = Encoding.UTF8.GetString(plain)
Return True
Catch
Return False
End Try
End Function
Private Function SanitizeToken(ByVal token As String) As String
Dim invalid As Char() = IO.Path.GetInvalidFileNameChars()
Dim sb As New StringBuilder(token.Length)
For Each ch As Char In token
If
Array.IndexOf(invalid, ch) >= 0 Then sb.Append("_"c)
Else sb.Append(ch)
Next
Dim s As String = sb.ToString()
If
String.IsNullOrWhiteSpace(s) Then Return "account"
Return s
End Function
''' <summary>Count PROPFIND displayname labels stored
for multi-cal UX.</summary>
Public Function TryGetPrimaryCalendarFriendlyLabel() As String
Dim path As String = IO.Path.Combine(MailDataRoot(), CalDavSettingsFile)
If Not File.Exists(path) Then Return ""
Try
Dim ser As New JavaScriptSerializer()
Dim dto As CalDavDto = ser.Deserialize(Of CalDavDto)(File.ReadAllText(path, Encoding.UTF8))
If dto Is Nothing Then Return ""
Dim name As String = If(dto.PrimaryCalendarDisplayName, "").Trim()
If
name.Length > 0 Then Return
name.ToUpperInvariant()
Dim url As String = If(dto.CalendarUrl, "").Trim()
If
url.Length > 0 Then Return TryGetCalendarFriendlyLabel(url)
Catch
End Try
Return ""
End Function
Public Function CountKnownCalendarUrlLabels() As Integer
Dim labels As Dictionary(Of String, String) = TryLoadCalendarUrlLabels()
If labels Is Nothing Then Return 0
Return
labels.Count
End Function
''' <summary>Map stored calendar leaf/url to PROPFIND displayname.</summary>
Public Function TryGetCalendarFriendlyLabel(ByVal calendarNameOrUrl As String) As String
Dim key As String = If(calendarNameOrUrl, "").Trim()
If
key.Length = 0 Then Return ""
Dim labels As Dictionary(Of String, String) = TryLoadCalendarUrlLabels()
If labels Is Nothing OrElse labels.Count = 0 Then Return key
For Each pair As KeyValuePair(Of String, String) In labels
Dim url As String = If(pair.Key, "").Trim()
If
url.Length = 0 Then Continue
For
Dim leaf As String = IO.Path.GetFileName(url.Trim().TrimEnd("/"c))
If
key.Equals(url, StringComparison.OrdinalIgnoreCase) OrElse key.Equals(leaf, StringComparison.OrdinalIgnoreCase) Then Dim label As String = If(pair.Value, "").Trim()
If
label.Length > 0 Then
Return
label.ToUpperInvariant()
End If
Next
Return
key.ToUpperInvariant()
End Function
Private Function TryLoadCalendarUrlLabels() As Dictionary(Of String, String)
Dim path As String = IO.Path.Combine(MailDataRoot(), CalDavSettingsFile)
If Not File.Exists(path) Then Return New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
Try
Dim ser As New JavaScriptSerializer()
Dim dto As CalDavDto = ser.Deserialize(Of CalDavDto)(File.ReadAllText(path, Encoding.UTF8))
If dto Is Nothing OrElse dto.CalendarUrlLabels Is Nothing Then Return New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
Return
dto.CalendarUrlLabels
Catch
Return New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
End Try
End Function
Private Class CalDavDto
Public CalendarUrl As String
Public Username As String
Public TaskCollectionUrl As String
Public SecondaryCalendarUrls As List(Of String)
Public CalendarUrlLabels As Dictionary(Of String, String)
Public PrimaryCalendarDisplayName As String
End Class
End Module
