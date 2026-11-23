' LCARSutilities/LCARSutilities/Modules/CalendarRecurrence.vb
Option Strict On
Option Explicit On
Imports
System.Collections.Generic
Imports
System.Globalization
''' <summary>Lightning-style recurrence day hints
for month grid.</summary>
Public Module CalendarRecurrence
Public Sub AddOccurrenceDaysInMonth( ByVal ev As CalendarEvent, ByVal month As DateTime, ByVal days As HashSet(Of Integer))
If ev Is Nothing OrElse days Is Nothing Then Return
Dim monthStart As New DateTime(month.Year, month.Month, 1, 0, 0, 0, DateTimeKind.Utc)
Dim monthEnd As DateTime = monthStart.AddMonths(1)
If
ev.StartUtc >= monthStart AndAlso ev.StartUtc < monthEnd AndAlso Not IsOccurrenceExcluded(ev, ev.StartUtc) Then days.Add(ev.StartUtc.Day)
Dim rule As String = If(ev.RecurrenceRule, "").Trim()
If
rule.Length = 0 Then Return
Dim untilUtc As DateTime = ParseUntilUtc(rule)
Dim maxCount As Integer = ParseCount(rule, 0)
If
rule.IndexOf("FREQ=DAILY", StringComparison.OrdinalIgnoreCase) >= 0 Then Dim interval As Integer = ParseInterval(rule, 1)
Dim daily As DateTime = ev.StartUtc
While daily < monthStart daily = daily.AddDays(interval)
End While
Dim daysFromStart As Long = CLng((daily.Date - ev.StartUtc.Date).TotalDays)
Dim occIndex As Integer = CInt(Math.Max(0L, daysFromStart \
Math.Max(1, interval)))
While daily < monthEnd
If daily > untilUtc Then Exit
While
If maxCount > 0 AndAlso occIndex + 1 > maxCount Then Exit
While
If daily >= monthStart AndAlso Not IsOccurrenceExcluded(ev, daily) Then
days.Add(daily.Day) occIndex += 1 daily = daily.AddDays(interval)
End While
Return
End If
If
rule.IndexOf("FREQ=WEEKLY", StringComparison.OrdinalIgnoreCase) >= 0 Then Dim byDay As HashSet(Of DayOfWeek) = ParseByDaySet(rule)
If
byDay.Count = 0 Then byDay.Add(ev.StartUtc.DayOfWeek)
Dim interval As Integer = ParseInterval(rule, 1)
Dim wkst As DayOfWeek = ParseWkst(rule, ev.StartUtc.DayOfWeek)
Dim cursor As DateTime = monthStart
While cursor < monthEnd
If
byDay.Contains(cursor.DayOfWeek) Then
Dim candidate As DateTime = cursor.Date.Add(ev.StartUtc.TimeOfDay)
If candidate <= untilUtc AndAlso candidate >= ev.StartUtc AndAlso candidate >= monthStart AndAlso candidate < monthEnd Then
Dim weekIndex As Integer = WeeksSinceStart(ev.StartUtc.Date, candidate.Date, wkst)
If weekIndex >= 0 AndAlso weekIndex Mod interval = 0 Then days.Add(candidate.Day)
End If cursor = cursor.AddDays(1)
End While
Return
End If
If
rule.IndexOf("FREQ=MONTHLY", StringComparison.OrdinalIgnoreCase) >= 0 Then Dim interval As Integer = ParseInterval(rule, 1)
Dim byMonthDay As Integer = ParseByMonthDay(rule)
If byMonthDay <> 0 Then
Dim dayNum As Integer = ResolveMonthDay(month.Year, month.Month, byMonthDay)
Dim monthDayOcc As DateTime = New DateTime(month.Year, month.Month, dayNum, ev.StartUtc.Hour, ev.StartUtc.Minute, ev.StartUtc.Second, DateTimeKind.Utc)
If monthDayOcc >= ev.StartUtc AndAlso monthDayOcc >= monthStart AndAlso monthDayOcc < monthEnd AndAlso monthDayOcc <= untilUtc Then If maxCount <= 0 OrElse ((monthDayOcc.Year - ev.StartUtc.Year) * 12 + (monthDayOcc.Month - ev.StartUtc.Month) + 1) <= maxCount Then days.Add(monthDayOcc.Day)
End If
Return
End If
Dim bySetPos As List(Of KeyValuePair(Of Integer, DayOfWeek)) = ParseByDaySetPosList(rule)
If
bySetPos.Count > 0 Then Dim monthsBetween As Integer = (month.Year - ev.StartUtc.Year) * 12 + (month.Month - ev.StartUtc.Month)
If monthsBetween >= 0 AndAlso monthsBetween Mod interval = 0 Then For Each spec As KeyValuePair(Of Integer, DayOfWeek) In bySetPos
Dim nth As DateTime = NthWeekdayInMonth(month.Year, month.Month, spec.Value, spec.Key)
If nth = DateTime.MinValue Then
Continue For
Dim candidate As DateTime = nth.Date.Add(ev.StartUtc.TimeOfDay)
If candidate <= untilUtc AndAlso candidate >= ev.StartUtc AndAlso candidate >= monthStart AndAlso candidate < monthEnd Then
If maxCount <= 0 OrElse (monthsBetween + 1) <= maxCount Then days.Add(candidate.Day)
Next
End If
Return
End If
Dim byDayMonthly As HashSet(Of DayOfWeek) = ParseByDaySet(rule)
If
byDayMonthly.Count > 0 Then Dim cursor As DateTime = monthStart
While cursor < monthEnd
If
byDayMonthly.Contains(cursor.DayOfWeek) Then
Dim candidate As DateTime = cursor.Date.Add(ev.StartUtc.TimeOfDay)
If candidate <= untilUtc AndAlso candidate >= ev.StartUtc AndAlso candidate >= monthStart AndAlso candidate < monthEnd Then
Dim monthsBetween As Integer = (candidate.Year - ev.StartUtc.Year) * 12 + (candidate.Month - ev.StartUtc.Month)
If monthsBetween >= 0 AndAlso monthsBetween Mod interval = 0 Then days.Add(candidate.Day)
End If cursor = cursor.AddDays(1)
End While
Return
End If
Dim monthly As DateTime = ev.StartUtc
While monthly < monthStart monthly = monthly.AddMonths(interval)
End While
While monthly < monthEnd
If monthly > untilUtc Then Exit
While
If maxCount > 0 Then Dim monthOcc As Integer = (monthly.Year - ev.StartUtc.Year) * 12 + (monthly.Month - ev.StartUtc.Month) + 1
If monthOcc > maxCount Then
Exit While
End If
If monthly >= monthStart Then
days.Add(monthly.Day) monthly = monthly.AddMonths(interval)
End While
Return
End If
If
rule.IndexOf("FREQ=YEARLY", StringComparison.OrdinalIgnoreCase) >= 0 Then Dim interval As Integer = ParseInterval(rule, 1)
Dim byMonth As Integer = ParseByMonth(rule)
Dim byMonthDayYear As Integer = ParseByMonthDay(rule)
If byMonth > 0 AndAlso byMonthDayYear <> 0 Then If
month.Month <> byMonth Then Return
Dim yearDay As Integer = ResolveMonthDay(month.Year, byMonth, byMonthDayYear)
Dim yearOcc As DateTime = New DateTime( month.Year, byMonth, yearDay, ev.StartUtc.Hour, ev.StartUtc.Minute, ev.StartUtc.Second, DateTimeKind.Utc)
If yearOcc >= ev.StartUtc AndAlso yearOcc >= monthStart AndAlso yearOcc < monthEnd AndAlso yearOcc <= untilUtc Then
Dim yearsBetween As Integer = yearOcc.Year - ev.StartUtc.Year
If yearsBetween >= 0 AndAlso yearsBetween Mod interval = 0 Then If maxCount <= 0 OrElse (yearsBetween \ interval + 1) <= maxCount Then days.Add(yearOcc.Day)
End If
Return
End If
Dim bySetPosYear As List(Of KeyValuePair(Of Integer, DayOfWeek)) = ParseByDaySetPosList(rule)
If
bySetPosYear.Count > 0 Then Dim targetMonth As Integer = If(byMonth > 0, byMonth, ev.StartUtc.Month)
If
month.Month <> targetMonth Then Return
Dim yearsBetween As Integer = month.Year - ev.StartUtc.Year
If yearsBetween < 0 OrElse yearsBetween Mod interval <> 0 Then Return
For Each spec As KeyValuePair(Of Integer, DayOfWeek) In bySetPosYear
Dim nth As DateTime = NthWeekdayInMonth(month.Year, month.Month, spec.Value, spec.Key)
If nth = DateTime.MinValue Then Continue
For
Dim candidate As DateTime = nth.Date.Add(ev.StartUtc.TimeOfDay)
If candidate <= untilUtc AndAlso candidate >= ev.StartUtc AndAlso candidate >= monthStart AndAlso candidate < monthEnd Then
If maxCount <= 0 OrElse (yearsBetween \ interval + 1) <= maxCount Then days.Add(candidate.Day)
Next
Return
End If
If byMonth > 0 AndAlso month.Month <> byMonth Then Return
Dim yearly As DateTime = ev.StartUtc
While yearly < monthStart yearly = yearly.AddYears(interval)
End While
While yearly < monthEnd
If yearly > untilUtc Then Exit
While
If byMonth > 0 AndAlso yearly.Month <> byMonth Then
yearly = yearly.AddYears(interval)
Continue While
End If
If maxCount > 0 Then Dim yearOcc As Integer = yearly.Year - ev.StartUtc.Year + 1
If yearOcc > maxCount Then
Exit While
End If
If yearly >= monthStart Then days.Add(yearly.Day) yearly = yearly.AddYears(interval)
End While
End If
End Sub
Private Function ParseUntilUtc(ByVal rule As String) As DateTime
Dim r As String = If(rule, "")
Dim idx As Integer = r.IndexOf("UNTIL=", StringComparison.OrdinalIgnoreCase)
If idx < 0 Then Return
DateTime.MaxValue
Dim tail As String = r.Substring(idx + 6)
Dim semi As Integer = tail.IndexOf(";"c)
If semi >= 0 Then tail = tail.Substring(0, semi) tail = tail.Trim()
If
tail.Length >= 8 Then
Dim datePart As String = tail.Substring(0, 8)
Dim parsed As DateTime = DateTime.MinValue
If
DateTime.TryParseExact(datePart, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, parsed) Then
Return
parsed.ToUniversalTime()
End If
End If
Return
DateTime.MaxValue
End Function
Private Function ParseWkst(ByVal rule As String, ByVal defaultDay As DayOfWeek) As DayOfWeek
Dim r As String = If(rule, "")
Dim idx As Integer = r.IndexOf("WKST=", StringComparison.OrdinalIgnoreCase)
If idx < 0 Then Return defaultDay
Dim tail As String = r.Substring(idx + 5)
Dim semi As Integer = tail.IndexOf(";"c)
If semi >= 0 Then tail = tail.Substring(0, semi)
Dim code As String = tail.Trim().ToUpperInvariant()
If
code.Length > 2 Then code = code.Substring(code.Length - 2)
Dim parsed As DayOfWeek? = DayCodeToDayOfWeek(code)
If
parsed.HasValue Then Return
parsed.Value
Return defaultDay
End Function
Private Function WeeksSinceStart(ByVal startDate As DateTime, ByVal candidateDate As DateTime, ByVal wkst As DayOfWeek) As Integer
Dim startWeek As DateTime = StartOfWeek(startDate, wkst)
Dim candWeek As DateTime = StartOfWeek(candidateDate, wkst)
If candWeek < startWeek Then Return -1
Return CInt((candWeek - startWeek).TotalDays /
7.0)
End Function
Private Function StartOfWeek(ByVal dt As DateTime, ByVal wkst As DayOfWeek) As DateTime
Dim d As DateTime = dt.Date
While
d.DayOfWeek <> wkst d = d.AddDays(-1)
End While
Return d
End Function
Private Function ParseByMonth(ByVal rule As String) As Integer
Dim r As String = If(rule, "")
Dim idx As Integer = r.IndexOf("BYMONTH=", StringComparison.OrdinalIgnoreCase)
If idx < 0 Then Return 0
Dim tail As String = r.Substring(idx + 8)
Dim semi As Integer = tail.IndexOf(";"c)
If semi >= 0 Then tail = tail.Substring(0, semi)
Dim n As Integer = 0
If
Integer.TryParse(tail.Trim(), n) AndAlso n >= 1 AndAlso n <= 12 Then Return n
Return 0
End Function
Private Function ParseByMonthDay(ByVal rule As String) As Integer
Dim r As String = If(rule, "")
Dim idx As Integer = r.IndexOf("BYMONTHDAY=", StringComparison.OrdinalIgnoreCase)
If idx < 0 Then Return 0
Dim tail As String = r.Substring(idx + 11)
Dim semi As Integer = tail.IndexOf(";"c)
If semi >= 0 Then tail = tail.Substring(0, semi)
Dim n As Integer = 0
If
Integer.TryParse(tail.Trim(), n) Then If n = -1 Then Return -1
If n > 0 AndAlso n <= 31 Then Return n
Return 0
End Function
Private Function ResolveMonthDay(ByVal year As Integer, ByVal month As Integer, ByVal byMonthDay As Integer) As Integer
Dim dimMonth As Integer = DateTime.DaysInMonth(year, month)
If byMonthDay = -1 Then Return dimMonth
Return
Math.Min(byMonthDay, dimMonth)
End Function
Private Function ParseCount(ByVal rule As String, ByVal defaultValue As Integer) As Integer
Dim r As String = If(rule, "")
Dim idx As Integer = r.IndexOf("COUNT=", StringComparison.OrdinalIgnoreCase)
If idx < 0 Then Return defaultValue
Dim tail As String = r.Substring(idx + 6)
Dim semi As Integer = tail.IndexOf(";"c)
If semi >= 0 Then tail = tail.Substring(0, semi)
Dim n As Integer = 0
If
Integer.TryParse(tail.Trim(), n) AndAlso n > 0 Then Return n
Return defaultValue
End Function
Private Function ParseInterval(ByVal rule As String, ByVal defaultValue As Integer) As Integer
Dim r As String = If(rule, "")
Dim idx As Integer = r.IndexOf("INTERVAL=", StringComparison.OrdinalIgnoreCase)
If idx < 0 Then Return defaultValue
Dim tail As String = r.Substring(idx + 9)
Dim semi As Integer = tail.IndexOf(";"c)
If semi >= 0 Then tail = tail.Substring(0, semi)
Dim n As Integer = 0
If
Integer.TryParse(tail.Trim(), n) AndAlso n > 0 Then Return n
Return defaultValue
End Function
Private Function NthWeekdayInMonth(ByVal year As Integer, ByVal month As Integer, ByVal dow As DayOfWeek, ByVal n As Integer) As DateTime
If n = 0 Then Return
DateTime.MinValue
Dim daysInMonth As Integer = DateTime.DaysInMonth(year, month)
If n < 0 Then Dim lastMatch As DateTime = DateTime.MinValue
For day As Integer = 1 To daysInMonth
Dim dt As New DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc)
If
dt.DayOfWeek = dow Then
lastMatch = dt
Next
Return lastMatch
End If
Dim seen As Integer = 0
For day As Integer = 1 To daysInMonth
Dim dt As New DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc)
If
dt.DayOfWeek <> dow Then Continue
For seen += 1
If seen = n Then Return dt
Next
Return
DateTime.MinValue
End Function
Private Function ParseByDaySetPosList(ByVal rule As String) As List(Of KeyValuePair(Of Integer, DayOfWeek))
Dim list As New List(Of KeyValuePair(Of Integer, DayOfWeek))()
Dim r As String = If(rule, "")
Dim idx As Integer = r.IndexOf("BYDAY=", StringComparison.OrdinalIgnoreCase)
If idx < 0 Then Return list
Dim tail As String = r.Substring(idx + 6)
Dim semi As Integer = tail.IndexOf(";"c)
If semi >= 0 Then tail = tail.Substring(0, semi)
For Each token As String In tail.Split(","c)
Dim t As String = token.Trim().ToUpperInvariant()
If
t.Length < 3 Then Continue
For
Dim posStr As String = ""
Dim i As Integer = 0
If i <
t.Length AndAlso t(i) = "-"c Then posStr = "-" i += 1
While i <
t.Length AndAlso Char.IsDigit(t(i)) posStr &= t(i) i += 1
End While
If
posStr.Length = 0 Then Continue
For
Dim n As Integer = 0
If Not Integer.TryParse(posStr, n) OrElse n = 0 Then Continue
For
Dim dowCode As String = t.Substring(i)
If
dowCode.Length > 2 Then dowCode = dowCode.Substring(dowCode.Length - 2)
Dim dow As DayOfWeek? = DayCodeToDayOfWeek(dowCode)
If
dow.HasValue Then
list.Add(New KeyValuePair(Of Integer, DayOfWeek)(n, dow.Value))
Next
Return list
End Function
Private Function DayCodeToDayOfWeek(ByVal code As String) As DayOfWeek?
Select Case code
Case "SU" :
Return
DayOfWeek.Sunday
Case "MO" :
Return
DayOfWeek.Monday
Case "TU" :
Return
DayOfWeek.Tuesday
Case "WE" :
Return
DayOfWeek.Wednesday
Case "TH" :
Return
DayOfWeek.Thursday
Case "FR" :
Return
DayOfWeek.Friday
Case "SA" :
Return
DayOfWeek.Saturday
Case
Else :
Return Nothing
End Select
End Function
Private Function ParseByDaySet(ByVal rule As String) As HashSet(Of DayOfWeek)
Dim setDays As New HashSet(Of DayOfWeek)()
Dim r As String = If(rule, "")
Dim idx As Integer = r.IndexOf("BYDAY=", StringComparison.OrdinalIgnoreCase)
If idx < 0 Then Return setDays
Dim tail As String = r.Substring(idx + 6)
Dim semi As Integer = tail.IndexOf(";"c)
If semi >= 0 Then tail = tail.Substring(0, semi)
For Each token As String In tail.Split(","c)
Dim t As String = token.Trim().ToUpperInvariant()
If
t.Length < 2 Then Continue
For
Dim dow As String = t
If
dow.Length > 2 Then dow = dow.Substring(dow.Length - 2)
Select Case dow
Case "SU" :
setDays.Add(DayOfWeek.Sunday)
Case "MO" :
setDays.Add(DayOfWeek.Monday)
Case "TU" :
setDays.Add(DayOfWeek.Tuesday)
Case "WE" :
setDays.Add(DayOfWeek.Wednesday)
Case "TH" :
setDays.Add(DayOfWeek.Thursday)
Case "FR" :
setDays.Add(DayOfWeek.Friday)
Case "SA" :
setDays.Add(DayOfWeek.Saturday)
End Select
Next
Return setDays
End Function
Public Function IsOccurrenceExcluded(ByVal ev As CalendarEvent, ByVal occurrenceUtc As DateTime) As Boolean
If ev Is Nothing OrElse occurrenceUtc = DateTime.MinValue Then Return False
Dim csv As String = If(ev.ExDateUtcCsv, "").Trim()
If
csv.Length = 0 Then Return False
Dim stamp As String = occurrenceUtc.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture)
For Each part As String In csv.Split(New Char() {","c}, StringSplitOptions.RemoveEmptyEntries)
If
String.Equals(part.Trim(), stamp, StringComparison.OrdinalIgnoreCase) Then Return True
Next
Return False
End Function
''' <summary>Append EXDATE on series master when detaching an instance.</summary>
''' <summary>Set RRULE UNTIL to day before detached instance when UNTIL is later.</summary>
Public Function TryClampRecurrenceUntilBeforeInstance( ByVal series As CalendarEvent, ByVal instanceStartUtc As DateTime) As Boolean
If series Is Nothing OrElse instanceStartUtc = DateTime.MinValue Then Return False
Dim rule As String = If(series.RecurrenceRule, "").Trim()
If
rule.Length = 0 Then Return False
Dim untilIdx As Integer = rule.IndexOf("UNTIL=", StringComparison.OrdinalIgnoreCase)
If untilIdx < 0 Then Return False
Dim start As Integer = untilIdx + 6
Dim endIdx As Integer = rule.IndexOf(";"c, start)
If endIdx < 0 Then endIdx = rule.Length
Dim untilText As String = rule.Substring(start, endIdx - start).Trim()
Dim untilUtc As DateTime = ParseUntilUtc(series.RecurrenceRule)
If untilUtc = DateTime.MaxValue Then Return False
If
instanceStartUtc.ToUniversalTime() >= untilUtc Then Return False
Dim newUntil As String = instanceStartUtc.ToUniversalTime().AddDays(-1).ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture)
series.RecurrenceRule = rule.Substring(0, start) & newUntil & rule.Substring(endIdx)
series.CalDavDirty = True
series.EwsDirty = True
Return True
End Function
''' <summary>When RRULE uses COUNT=, decrement count after detaching one instance.</summary>
Public Function TryDecrementRecurrenceCountOnSeries(ByVal series As CalendarEvent) As Boolean
If series Is Nothing Then Return False
Dim rule As String = If(series.RecurrenceRule, "").Trim()
If
rule.Length = 0 Then Return False
Dim countIdx As Integer = rule.IndexOf("COUNT=", StringComparison.OrdinalIgnoreCase)
If countIdx < 0 Then Return False
Dim start As Integer = countIdx + 6
Dim endIdx As Integer = rule.IndexOf(";"c, start)
If endIdx < 0 Then endIdx = rule.Length
Dim countText As String = rule.Substring(start, endIdx - start).Trim()
Dim countVal As Integer = 0
If Not Integer.TryParse(countText, countVal) OrElse countVal <= 1 Then Return False
Dim newRule As String = rule.Substring(0, start) & (countVal - 1).ToString() & rule.Substring(endIdx)
series.RecurrenceRule = newRule
series.CalDavDirty = True
series.EwsDirty = True
Return True
End Function
Public Function TryAppendExDateOnSeriesMaster( ByVal all As List(Of CalendarEvent), ByVal series As CalendarEvent, ByVal instanceStartUtc As DateTime) As Boolean
If all Is Nothing OrElse series Is Nothing OrElse String.IsNullOrWhiteSpace(series.Uid) Then Return False
Dim stamp As String = instanceStartUtc.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture)
For Each ev As CalendarEvent In all
If ev Is Nothing Then Continue
For
If Not String.Equals(ev.Uid, series.Uid, StringComparison.OrdinalIgnoreCase) Then Continue
For
Dim csv As String = If(ev.ExDateUtcCsv, "").Trim()
If
csv.IndexOf(stamp, StringComparison.OrdinalIgnoreCase) >= 0 Then Return True
ev.ExDateUtcCsv = If(csv.Length = 0, stamp, csv & "," & stamp)
ev.CalDavDirty = True
ev.EwsDirty = True
Return True
Next
Return False
End Function
''' <summary>Detach one recurring instance as an exception event.</summary>
Public Function TryCreateRecurrenceException( ByVal series As CalendarEvent, ByVal instanceStartUtc As DateTime) As CalendarEvent
If series Is Nothing Then Return Nothing
If
String.IsNullOrWhiteSpace(series.RecurrenceRule) Then Return Nothing
Dim ex As New CalendarEvent()
With { .Uid = Guid.NewGuid().ToString(), .Summary = series.Summary, .Location = series.Location, .StartUtc = instanceStartUtc, .EndUtc = instanceStartUtc.Add(series.EndUtc - series.StartUtc), .AlarmMinutesBefore = series.AlarmMinutesBefore, .Category = series.Category, .CalendarName = series.CalendarName, .RecurrenceParentUid = series.Uid, .RecurrenceIdUtc = instanceStartUtc, .CalDavDirty = True, .EwsDirty = True }
Return ex
End Function
End Module
