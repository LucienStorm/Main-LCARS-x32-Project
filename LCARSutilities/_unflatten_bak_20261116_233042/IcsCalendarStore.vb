' LCARSutilities/LCARSutilities/Modules/IcsCalendarStore.vb
Option Strict On
Option Explicit On
Imports
System.Collections.Generic
Imports
System.Globalization
Imports
System.IO
Imports
System.Text
Imports
System.Web.Script.Serialization
Public Class CalendarEvent
Public Uid As String = Guid.NewGuid().ToString()
Public Summary As String = String.Empty
Public StartUtc As DateTime = DateTime.UtcNow
Public EndUtc As DateTime = DateTime.UtcNow.AddHours(1)
Public Location As String = String.Empty
Public AlarmMinutesBefore As Integer
Public TaskCompleted As Boolean
''' <summary>VTODO PRIORITY (0 highest, 9 lowest).</summary>
Public TaskPriority As Integer = 5
''' <summary>VTODO PERCENT-COMPLETE (0–100).</summary>
Public TaskPercentComplete As Integer
Public EwsItemId As String = String.Empty
Public EwsChangeKey As String = String.Empty
Public EwsDirty As Boolean
Public EwsLastModifiedUtc As DateTime
Public Category As String = String.Empty
Public RecurrenceRule As String = String.Empty
''' <summary>RECURRENCE-ID
for exception instance.</summary>
Public RecurrenceIdUtc As DateTime
Public RecurrenceParentUid As String = String.Empty
''' <summary>Comma-separated UTC EXDATE stamps (yyyyMMdd'T'HHmmss'Z') on series master.</summary>
Public ExDateUtcCsv As String = String.Empty
''' <summary>Lightning-style calendar name.</summary>
Public CalendarName As String = String.Empty
Public CalDavDirty As Boolean
Public CalDavEtag As String = String.Empty
''' <summary>Meeting invitation RSVP state (Lightning PARTSTAT progress, slice 15).</summary>
Public Organizer As String = String.Empty
Public AttendeeMailto As String = String.Empty
Public MyPartStat As String = String.Empty
Public IsMeetingInvitation As Boolean
End Class
Public Module IcsCalendarStore
Private Const EventsFile As String = "events.json"
Public Function DataRoot() As String
Dim dir As String = IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LCARS", "Calendar")
If Not Directory.Exists(dir) Then Directory.CreateDirectory(dir)
Return dir
End Function
Public Function LoadEvents() As List(Of CalendarEvent)
Dim path As String = IO.Path.Combine(DataRoot(), EventsFile)
If Not File.Exists(path) Then Return New List(Of CalendarEvent)()
Try
Dim ser As New JavaScriptSerializer()
Dim list As List(Of CalendarEvent) = ser.Deserialize(Of List(Of CalendarEvent))(File.ReadAllText(path, Encoding.UTF8))
If list Is Nothing Then Return New List(Of CalendarEvent)()
Return list
Catch
Return New List(Of CalendarEvent)()
End Try
End Function
Public Sub SaveEvents(ByVal events As List(Of CalendarEvent))
Dim ser As New JavaScriptSerializer()
File.WriteAllText(IO.Path.Combine(DataRoot(), EventsFile), ser.Serialize(events), Encoding.UTF8)
End Sub
Public Function ImportIcsFile(ByVal path As String) As Integer
If Not File.Exists(path) Then Return 0
Dim text As String = File.ReadAllText(path, Encoding.UTF8)
Dim imported As List(Of CalendarEvent) = ParseIcs(text)
Dim merged As List(Of CalendarEvent) = LoadEvents()
merged.AddRange(imported) SaveEvents(merged)
Return
imported.Count
End Function
Public Function ParseIcs(ByVal ics As String) As List(Of CalendarEvent)
Dim result As New List(Of CalendarEvent)()
Dim current As CalendarEvent = Nothing
For Each rawLine As String In ics.Split(New String() {vbCrLf, vbLf}, StringSplitOptions.None)
Dim line As String = UnfoldIcsLine(rawLine).Trim()
If
line.Length = 0 Then Continue
For
If line = "BEGIN:VEVENT" Then
current = New CalendarEvent()
Continue For
End If
If line = "END:VEVENT" Then If current IsNot Nothing Then
result.Add(current) current = Nothing
Continue For
End If
If current Is Nothing Then Continue
For
If
line.StartsWith("UID:", StringComparison.OrdinalIgnoreCase) Then current.Uid = line.Substring(4).Trim()
If
line.StartsWith("SUMMARY:", StringComparison.OrdinalIgnoreCase) Then current.Summary = line.Substring(8).Trim()
If
line.StartsWith("LOCATION:", StringComparison.OrdinalIgnoreCase) Then current.Location = line.Substring(9).Trim()
If
line.StartsWith("DTSTART", StringComparison.OrdinalIgnoreCase) Then current.StartUtc = ParseIcsDate(line)
If
line.StartsWith("DTEND", StringComparison.OrdinalIgnoreCase) Then current.EndUtc = ParseIcsDate(line)
If
line.StartsWith("ORGANIZER", StringComparison.OrdinalIgnoreCase) Then current.Organizer = ParseIcsValue(line)
current.IsMeetingInvitation = True
End If
If
line.StartsWith("ATTENDEE", StringComparison.OrdinalIgnoreCase) Then current.AttendeeMailto = ParseIcsValue(line)
Dim partStat As String = ParseIcsParam(line, "PARTSTAT")
If
partStat.Length > 0 Then current.MyPartStat = partStat
current.IsMeetingInvitation = True
End If
Next
Return result
End Function
Private Function ParseIcsValue(ByVal line As String) As String
Dim colon As Integer = line.IndexOf(":"c)
If colon < 0 Then Return
line.Trim()
Return
line.Substring(colon + 1).Trim()
End Function
Private Function ParseIcsParam(ByVal line As String, ByVal paramName As String) As String
Dim upper As String = line.ToUpperInvariant()
Dim token As String = paramName.ToUpperInvariant() & "="
Dim idx As Integer = upper.IndexOf(token, StringComparison.Ordinal)
If idx < 0 Then Return ""
Dim tail As String = line.Substring(idx + token.Length)
Dim semi As Integer = tail.IndexOf(";"c)
If semi >= 0 Then tail = tail.Substring(0, semi)
Return
tail.Trim()
End Function
Private Function UnfoldIcsLine(ByVal line As String) As String
Return If(line, "").Replace("\n", "").Replace("\r", "")
End Function
Private Function ParseIcsDate(ByVal line As String) As DateTime
Dim value As String = line
Dim colon As Integer = value.IndexOf(":"c)
If colon >= 0 Then value = value.Substring(colon + 1).Trim()
If
value.EndsWith("Z", StringComparison.OrdinalIgnoreCase) Then
value = value.Substring(0, value.Length - 1)
Dim dt As DateTime
If
DateTime.TryParseExact(value, "yyyyMMdd'T'HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, dt) Then
Return
dt.ToUniversalTime()
End If
End If
Dim localDt As DateTime
If
DateTime.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, localDt) Then
Return
localDt.ToUniversalTime()
End If
Return
DateTime.UtcNow
End Function
End Module
