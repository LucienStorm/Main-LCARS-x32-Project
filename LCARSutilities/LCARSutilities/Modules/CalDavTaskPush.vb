Imports System
Imports System.Diagnostics
Imports System.IO
Imports System.Net
Imports System.Security.Cryptography
Imports System.Text
Imports System.Web.Script.Serialization
Imports Microsoft.VisualBasic.CompilerServices

    <StandardModule>
    Public NotInheritable Class CalDavTaskPush
        Private Class CalDavDto
            Public CalendarUrl As String

            Public Username As String

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Private Const MailRootSubdir As String = "LCARS x32\Mail"

        Private Const CalDavSettingsFile As String = "caldav-settings.json"

        Private Const CalDavSecretKey As String = "caldav"

        Public Shared Function TryPushTask(ev As CalendarEvent) As Boolean
            If ev Is Nothing OrElse String.IsNullOrWhiteSpace(ev.Uid) Then
                Return False
            End If
            Dim text = If(Not Equals(ev.Summary, Nothing), ev.Summary, "")
            If text.IndexOf("[TASK]", StringComparison.OrdinalIgnoreCase) < 0 Then
                Return False
            End If
            Dim calendarUrl As String = Nothing
            Dim username As String = Nothing
            Dim password As String = Nothing
            If Not TryLoadCalDavCredentials(calendarUrl, username, password) Then
                Return False
            End If
            Dim text2 As String = calendarUrl.Trim()
            If Not text2.EndsWith("/") Then
                text2 += "/"
            End If
            Dim url As String = text2 & ev.Uid.Trim() & ".ics"
            Dim ics = BuildTaskIcs(ev)
            Dim flag = DavPut(url, username, password, ics)
            If flag Then
                ev.CalDavDirty = False
            End If
            Return flag
        End Function

        Private Shared Function BuildTaskIcs(ev As CalendarEvent) As String
            Dim text = StripDecorators(If(Not Equals(ev.Summary, Nothing), ev.Summary, ""))
            Dim stringBuilder As StringBuilder = New StringBuilder()
            stringBuilder.AppendLine("BEGIN:VCALENDAR")
            stringBuilder.AppendLine("VERSION:2.0")
            stringBuilder.AppendLine("PRODID:-//LCARS Calendar//CalDAV//EN")
            stringBuilder.AppendLine("BEGIN:VTODO")
            stringBuilder.AppendLine("UID:" & ev.Uid)
            stringBuilder.AppendLine("SUMMARY:" & text)
            If ev.TaskCompleted Then
                stringBuilder.AppendLine("STATUS:COMPLETED")
                stringBuilder.AppendLine("COMPLETED:" & Date.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'"))
            Else
                stringBuilder.AppendLine("STATUS:NEEDS-ACTION")
            End If
            Dim num = Math.Max(0, Math.Min(100, ev.TaskPercentComplete))
            If ev.TaskCompleted AndAlso num < 100 Then
                num = 100
            End If
            stringBuilder.AppendLine("PERCENT-COMPLETE:" & num.ToString())
            stringBuilder.AppendLine("PRIORITY:" & Math.Max(0, Math.Min(9, ev.TaskPriority)).ToString())
            If Date.Compare(ev.StartUtc, Date.MinValue) <> 0 Then
                stringBuilder.AppendLine("DUE:" & ev.StartUtc.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'"))
            End If
            If ev.AlarmMinutesBefore > 0 Then
                stringBuilder.AppendLine("BEGIN:VALARM")
                stringBuilder.AppendLine("TRIGGER:-PT" & ev.AlarmMinutesBefore.ToString() & "M")
                stringBuilder.AppendLine("ACTION:DISPLAY")
                stringBuilder.AppendLine("DESCRIPTION:Task reminder")
                stringBuilder.AppendLine("END:VALARM")
            End If
            If Not String.IsNullOrWhiteSpace(ev.Category) Then
                stringBuilder.AppendLine("CATEGORIES:" & ev.Category.Trim())
            End If
            If Not String.IsNullOrWhiteSpace(ev.RecurrenceRule) Then
                stringBuilder.AppendLine("RRULE:" & ev.RecurrenceRule.Trim())
            End If
            If Not String.IsNullOrWhiteSpace(ev.CalendarName) Then
                stringBuilder.AppendLine("X-LCARS-CALNAME:" & ev.CalendarName.Trim())
            End If
            stringBuilder.AppendLine("END:VTODO")
            stringBuilder.AppendLine("END:VCALENDAR")
            Return stringBuilder.ToString()
        End Function

        Private Shared Function StripDecorators(summary As String) As String
            Dim text As String = If(Not Equals(summary, Nothing), summary, "").Trim()
            If text.StartsWith("[DONE]", StringComparison.OrdinalIgnoreCase) Then
                text = text.Substring(6).TrimStart()
            End If
            If text.StartsWith("[TASK]", StringComparison.OrdinalIgnoreCase) Then
                text = text.Substring(6).TrimStart()
            End If
            Return text
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
            Dim result As Boolean
            If String.IsNullOrWhiteSpace(secretKey) Then
                result = False
            Else
                Dim path As String = IO.Path.Combine(MailDataRoot(), "secrets", SanitizeToken(secretKey) & ".dpapi")
                If Not File.Exists(path) Then
                    result = False
                Else
                    Try
                        Dim encryptedData = File.ReadAllBytes(path)
                        Dim bytes = ProtectedData.Unprotect(encryptedData, Nothing, DataProtectionScope.CurrentUser)
                        password = Encoding.UTF8.GetString(bytes)
                        result = True
                    Catch projectError As Exception
                        ProjectData.SetProjectError(projectError)
                        result = False
                        Call ProjectData.ClearProjectError()
                    End Try
                End If
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
    End Class
