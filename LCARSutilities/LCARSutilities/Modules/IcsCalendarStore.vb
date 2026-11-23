Imports System
Imports System.Collections.Generic
Imports System.Globalization
Imports System.IO
Imports System.Text
Imports System.Web.Script.Serialization
Imports Microsoft.VisualBasic.CompilerServices

    <StandardModule>
    Public NotInheritable Class IcsCalendarStore
        Private Const EventsFile As String = "events.json"

        Public Shared Function DataRoot() As String
            Dim text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LCARS", "Calendar")
            If Not Directory.Exists(text) Then
                Directory.CreateDirectory(text)
            End If
            Return text
        End Function

        Public Shared Function LoadEvents() As List(Of CalendarEvent)
            Dim path As String = IO.Path.Combine(DataRoot(), "events.json")
            Dim result As List(Of CalendarEvent)
            If Not File.Exists(path) Then
                result = New List(Of CalendarEvent)()
            Else
                Try
                    Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                    Dim list = javaScriptSerializer.Deserialize(Of List(Of CalendarEvent))(File.ReadAllText(path, Encoding.UTF8))
                    result = (If(list IsNot Nothing, list, New List(Of CalendarEvent)()))
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    result = New List(Of CalendarEvent)()
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Public Shared Sub SaveEvents(events As List(Of CalendarEvent))
            Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
            Call File.WriteAllText(Path.Combine(DataRoot(), "events.json"), javaScriptSerializer.Serialize(events), Encoding.UTF8)
        End Sub

        Public Shared Function ImportIcsFile(path As String) As Integer
            If Not File.Exists(path) Then
                Return 0
            End If
            Dim ics = File.ReadAllText(path, Encoding.UTF8)
            Dim list = ParseIcs(ics)
            Dim list2 As List(Of CalendarEvent) = LoadEvents()
            list2.AddRange(list)
            SaveEvents(list2)
            Return list.Count
        End Function

        Public Shared Function ParseIcs(ics As String) As List(Of CalendarEvent)
            Dim list As List(Of CalendarEvent) = New List(Of CalendarEvent)()
            Dim calendarEvent As CalendarEvent = Nothing
            Dim array = ics.Split(New String(1) {vbCrLf, vbLf}, StringSplitOptions.None)
            For Each line In array
                Dim text As String = UnfoldIcsLine(line).Trim()
                If text.Length = 0 Then
                    Continue For
                End If
                If Operators.CompareString(text, "BEGIN:VEVENT", TextCompare:=False) = 0 Then
                    calendarEvent = New CalendarEvent()
                ElseIf Operators.CompareString(text, "END:VEVENT", TextCompare:=False) = 0 Then
                    If calendarEvent IsNot Nothing Then
                        list.Add(calendarEvent)
                    End If
                    calendarEvent = Nothing
                Else
                    If calendarEvent Is Nothing Then
                        Continue For
                    End If
                    If text.StartsWith("UID:", StringComparison.OrdinalIgnoreCase) Then
                        calendarEvent.Uid = text.Substring(4).Trim()
                    End If
                    If text.StartsWith("SUMMARY:", StringComparison.OrdinalIgnoreCase) Then
                        calendarEvent.Summary = text.Substring(8).Trim()
                    End If
                    If text.StartsWith("LOCATION:", StringComparison.OrdinalIgnoreCase) Then
                        calendarEvent.Location = text.Substring(9).Trim()
                    End If
                    If text.StartsWith("DTSTART", StringComparison.OrdinalIgnoreCase) Then
                        calendarEvent.StartUtc = ParseIcsDate(text)
                    End If
                    If text.StartsWith("DTEND", StringComparison.OrdinalIgnoreCase) Then
                        calendarEvent.EndUtc = ParseIcsDate(text)
                    End If
                    If text.StartsWith("ORGANIZER", StringComparison.OrdinalIgnoreCase) Then
                        calendarEvent.Organizer = ParseIcsValue(text)
                        calendarEvent.IsMeetingInvitation = True
                    End If
                    If text.StartsWith("ATTENDEE", StringComparison.OrdinalIgnoreCase) Then
                        calendarEvent.AttendeeMailto = ParseIcsValue(text)
                        Dim text2 = ParseIcsParam(text, "PARTSTAT")
                        If text2.Length > 0 Then
                            calendarEvent.MyPartStat = text2
                        End If
                        calendarEvent.IsMeetingInvitation = True
                    End If
                End If
            Next
            Return list
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
            Dim num = text.IndexOf(text2, StringComparison.Ordinal)
            If num < 0 Then
                Return ""
            End If
            Dim text3 = line.Substring(num + text2.Length)
            Dim num2 = text3.IndexOf(";"c)
            If num2 >= 0 Then
                text3 = text3.Substring(0, num2)
            End If
            Return text3.Trim()
        End Function

        Private Shared Function UnfoldIcsLine(line As String) As String
            Return If(Not Equals(line, Nothing), line, "").Replace("\n", "").Replace("\r", "")
        End Function

        Private Shared Function ParseIcsDate(line As String) As Date
            Dim text = line
            Dim num = text.IndexOf(":"c)
            Dim result As Date = Nothing, result2 As Date = Nothing
            If num >= 0 Then
                text = text.Substring(num + 1).Trim()
            End If
            If text.EndsWith("Z", StringComparison.OrdinalIgnoreCase) Then
                text = text.Substring(0, text.Length - 1)

                If Date.TryParseExact(text, "yyyyMMdd'T'HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, result) Then
                    Return result.ToUniversalTime()
                End If
            End If

            If Date.TryParseExact(text, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, result2) Then
                Return result2.ToUniversalTime()
            End If
            Return Date.UtcNow
        End Function
    End Class
