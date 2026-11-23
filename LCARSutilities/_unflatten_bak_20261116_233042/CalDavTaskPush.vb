' LCARSutilities/LCARSutilities/Modules/CalDavTaskPush.vb
Option Strict On
Option Explicit On
Imports
System.IO
Imports
System.Net
Imports
System.Security.Cryptography
Imports
System.Text
Imports
System.Web.Script.Serialization
''' <summary>Push VTODO completion to CalDAV
using Mail app settings.</summary>
Public Module CalDavTaskPush
Private Const MailRootSubdir As String = "LCARS x32\Mail"
Private Const CalDavSettingsFile As String = "caldav-settings.json"
Private Const CalDavSecretKey As String = "caldav"
Public Function TryPushTask(ByVal ev As CalendarEvent) As Boolean
If ev Is Nothing OrElse String.IsNullOrWhiteSpace(ev.Uid) Then Return False
Dim summary As String = If(ev.Summary, "")
If
summary.IndexOf("[TASK]", StringComparison.OrdinalIgnoreCase) < 0 Then Return False
Dim url As String = Nothing
Dim user As String = Nothing
Dim pw As String = Nothing
If Not TryLoadCalDavCredentials(url, user, pw) Then Return False
Dim base As String = url.Trim()
If Not base.EndsWith("/"c) Then base &= "/"
Dim resource As String = base & ev.Uid.Trim() & ".ics"
Dim ics As String = BuildTaskIcs(ev)
Dim ok As Boolean = DavPut(resource, user, pw, ics)
If ok Then ev.CalDavDirty = False
Return ok
End Function
Private Function BuildTaskIcs(ByVal ev As CalendarEvent) As String
Dim summary As String = StripDecorators(If(ev.Summary, ""))
Dim sb As New StringBuilder()
sb.AppendLine("BEGIN:VCALENDAR")
sb.AppendLine("VERSION:2.0")
sb.AppendLine("PRODID:-//LCARS Calendar//CalDAV//EN")
sb.AppendLine("BEGIN:VTODO")
sb.AppendLine("UID:" & ev.Uid)
sb.AppendLine("SUMMARY:" & summary)
If
ev.TaskCompleted Then sb.AppendLine("STATUS:COMPLETED")
sb.AppendLine("COMPLETED:" & DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'"))
Else sb.AppendLine("STATUS:NEEDS-ACTION")
End If
Dim pct As Integer = Math.Max(0, Math.Min(100, ev.TaskPercentComplete))
If
ev.TaskCompleted AndAlso pct < 100 Then pct = 100
sb.AppendLine("PERCENT-COMPLETE:" & pct.ToString())
Dim pri As Integer = Math.Max(0, Math.Min(9, ev.TaskPriority))
sb.AppendLine("PRIORITY:" & pri.ToString())
If
ev.StartUtc <>
DateTime.MinValue Then sb.AppendLine("DUE:" & ev.StartUtc.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'"))
If
ev.AlarmMinutesBefore > 0 Then
sb.AppendLine("BEGIN:VALARM")
sb.AppendLine("TRIGGER:-PT" & ev.AlarmMinutesBefore.ToString() & "M")
sb.AppendLine("ACTION:DISPLAY")
sb.AppendLine("DESCRIPTION:Task reminder")
sb.AppendLine("END:VALARM")
End If
If Not String.IsNullOrWhiteSpace(ev.Category) Then sb.AppendLine("CATEGORIES:" & ev.Category.Trim())
If Not String.IsNullOrWhiteSpace(ev.RecurrenceRule) Then sb.AppendLine("RRULE:" & ev.RecurrenceRule.Trim())
If Not String.IsNullOrWhiteSpace(ev.CalendarName) Then sb.AppendLine("X-LCARS-CALNAME:" & ev.CalendarName.Trim())
sb.AppendLine("END:VTODO")
sb.AppendLine("END:VCALENDAR")
Return
sb.ToString()
End Function
Private Function StripDecorators(ByVal summary As String) As String
Dim s As String = If(summary, "").Trim()
If
s.StartsWith("[DONE]", StringComparison.OrdinalIgnoreCase) Then s = s.Substring(6).TrimStart()
If
s.StartsWith("[TASK]", StringComparison.OrdinalIgnoreCase) Then s = s.Substring(6).TrimStart()
Return s
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
If
String.IsNullOrWhiteSpace(secretKey) Then Return False
Dim path As String = IO.Path.Combine(MailDataRoot(), "secrets", SanitizeToken(secretKey) & ".dpapi")
If Not File.Exists(path) Then Return False
Try
Dim protectedBytes As Byte() = File.ReadAllBytes(path)
Dim plain As Byte() = ProtectedData.Unprotect(protectedBytes, Nothing, DataProtectionScope.CurrentUser) password = Encoding.UTF8.GetString(plain)
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
Private Class CalDavDto
Public CalendarUrl As String
Public Username As String
End Class
End Module
