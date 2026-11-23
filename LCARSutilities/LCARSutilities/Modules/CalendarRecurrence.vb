Imports System
Imports System.Collections.Generic
Imports System.Globalization
Imports Microsoft.VisualBasic.CompilerServices

    <StandardModule>
    Public NotInheritable Class CalendarRecurrence
        Public Shared Sub AddOccurrenceDaysInMonth(ev As CalendarEvent, month As Date, days As HashSet(Of Integer))
            If ev Is Nothing OrElse days Is Nothing Then
                Return
            End If
            Dim dateTime As Date = New DateTime(month.Year, month.Month, 1, 0, 0, 0, DateTimeKind.Utc)
            Dim t = dateTime.AddMonths(1)
            If Date.Compare(ev.StartUtc, dateTime) >= 0 AndAlso Date.Compare(ev.StartUtc, t) < 0 AndAlso Not IsOccurrenceExcluded(ev, ev.StartUtc) Then
                days.Add(ev.StartUtc.Day)
            End If
            Dim text As String = If(Not Equals(ev.RecurrenceRule, Nothing), ev.RecurrenceRule, "").Trim()
            If text.Length = 0 Then
                Return
            End If
            Dim t2 = ParseUntilUtc(text)
            Dim num = ParseCount(text, 0)
            If text.IndexOf("FREQ=DAILY", StringComparison.OrdinalIgnoreCase) >= 0 Then
                Dim num2 = ParseInterval(text, 1)
                Dim dateTime2 = ev.StartUtc
                While Date.Compare(dateTime2, dateTime) < 0
                    dateTime2 = dateTime2.AddDays(num2)
                End While
                Dim num3 As Long = Math.Round((dateTime2.Date - ev.StartUtc.Date).TotalDays)
                Dim num4 As Integer = Math.Max(0L, num3 / Math.Max(1, num2))
                While Date.Compare(dateTime2, t) < 0 AndAlso Date.Compare(dateTime2, t2) <= 0 AndAlso (num <= 0 OrElse num4 + 1 <= num)
                    If Date.Compare(dateTime2, dateTime) >= 0 AndAlso Not IsOccurrenceExcluded(ev, dateTime2) Then
                        days.Add(dateTime2.Day)
                    End If
                    num4 += 1
                    dateTime2 = dateTime2.AddDays(num2)
                End While
            ElseIf text.IndexOf("FREQ=WEEKLY", StringComparison.OrdinalIgnoreCase) >= 0 Then
                Dim hashSet = ParseByDaySet(text)
                If hashSet.Count = 0 Then
                    hashSet.Add(ev.StartUtc.DayOfWeek)
                End If
                Dim num5 = ParseInterval(text, 1)
                Dim wkst = ParseWkst(text, ev.StartUtc.DayOfWeek)
                Dim t3 = dateTime
                While Date.Compare(t3, t) < 0
                    If hashSet.Contains(t3.DayOfWeek) Then
                        Dim t4 = t3.Date.Add(ev.StartUtc.TimeOfDay)
                        If Date.Compare(t4, t2) <= 0 AndAlso Date.Compare(t4, ev.StartUtc) >= 0 AndAlso Date.Compare(t4, dateTime) >= 0 AndAlso Date.Compare(t4, t) < 0 Then
                            Dim num6 = WeeksSinceStart(ev.StartUtc.Date, t4.Date, wkst)
                            If num6 >= 0 AndAlso num6 Mod num5 = 0 Then
                                days.Add(t4.Day)
                            End If
                        End If
                    End If
                    t3 = t3.AddDays(1.0)
                End While
            ElseIf text.IndexOf("FREQ=MONTHLY", StringComparison.OrdinalIgnoreCase) >= 0 Then
                Dim num7 = ParseInterval(text, 1)
                Dim num8 = ParseByMonthDay(text)
                If num8 <> 0 Then
                    Dim day = ResolveMonthDay(month.Year, month.Month, num8)
                    Dim t5 As Date = New DateTime(month.Year, month.Month, day, ev.StartUtc.Hour, ev.StartUtc.Minute, ev.StartUtc.Second, DateTimeKind.Utc)
                    If Date.Compare(t5, ev.StartUtc) >= 0 AndAlso Date.Compare(t5, dateTime) >= 0 AndAlso Date.Compare(t5, t) < 0 AndAlso Date.Compare(t5, t2) <= 0 AndAlso (num <= 0 OrElse (t5.Year - ev.StartUtc.Year) * 12 + (t5.Month - ev.StartUtc.Month) + 1 <= num) Then
                        days.Add(t5.Day)
                    End If
                    Return
                End If
                Dim list = ParseByDaySetPosList(text)
                If list.Count > 0 Then
                    Dim num9 = (month.Year - ev.StartUtc.Year) * 12 + (month.Month - ev.StartUtc.Month)
                    If num9 < 0 OrElse num9 Mod num7 <> 0 Then
                        Return
                    End If
                    If True Then
                        For Each item In list
                            Dim t6 = NthWeekdayInMonth(month.Year, month.Month, item.Value, item.Key)
                            If Date.Compare(t6, Date.MinValue) <> 0 Then
                                Dim t7 = t6.Date.Add(ev.StartUtc.TimeOfDay)
                                If Date.Compare(t7, t2) <= 0 AndAlso Date.Compare(t7, ev.StartUtc) >= 0 AndAlso Date.Compare(t7, dateTime) >= 0 AndAlso Date.Compare(t7, t) < 0 AndAlso (num <= 0 OrElse num9 + 1 <= num) Then
                                    days.Add(t7.Day)
                                End If
                            End If
                        Next
                        Return
                    End If
                End If
                Dim hashSet2 = ParseByDaySet(text)
                If hashSet2.Count > 0 Then
                    Dim t8 = dateTime
                    While Date.Compare(t8, t) < 0
                        If hashSet2.Contains(t8.DayOfWeek) Then
                            Dim t9 = t8.Date.Add(ev.StartUtc.TimeOfDay)
                            If Date.Compare(t9, t2) <= 0 AndAlso Date.Compare(t9, ev.StartUtc) >= 0 AndAlso Date.Compare(t9, dateTime) >= 0 AndAlso Date.Compare(t9, t) < 0 Then
                                Dim num10 = (t9.Year - ev.StartUtc.Year) * 12 + (t9.Month - ev.StartUtc.Month)
                                If num10 >= 0 AndAlso num10 Mod num7 = 0 Then
                                    days.Add(t9.Day)
                                End If
                            End If
                        End If
                        t8 = t8.AddDays(1.0)
                    End While
                    Return
                End If
                Dim t10 = ev.StartUtc
                While Date.Compare(t10, dateTime) < 0
                    t10 = t10.AddMonths(num7)
                End While
                While Date.Compare(t10, t) < 0 AndAlso Date.Compare(t10, t2) <= 0
                    If num > 0 Then
                        Dim num11 = (t10.Year - ev.StartUtc.Year) * 12 + (t10.Month - ev.StartUtc.Month) + 1
                        If num11 > num Then
                            Exit While
                        End If
                    End If
                    If Date.Compare(t10, dateTime) >= 0 Then
                        days.Add(t10.Day)
                    End If
                    t10 = t10.AddMonths(num7)
                End While
            Else
                If text.IndexOf("FREQ=YEARLY", StringComparison.OrdinalIgnoreCase) < 0 Then
                    Return
                End If
                Dim num12 = ParseInterval(text, 1)
                Dim num13 = ParseByMonth(text)
                Dim num14 = ParseByMonthDay(text)
                If num13 > 0 AndAlso num14 <> 0 Then
                    If month.Month <> num13 Then
                        Return
                    End If
                    Dim day2 = ResolveMonthDay(month.Year, num13, num14)
                    Dim t11 As Date = New DateTime(month.Year, num13, day2, ev.StartUtc.Hour, ev.StartUtc.Minute, ev.StartUtc.Second, DateTimeKind.Utc)
                    If Date.Compare(t11, ev.StartUtc) >= 0 AndAlso Date.Compare(t11, dateTime) >= 0 AndAlso Date.Compare(t11, t) < 0 AndAlso Date.Compare(t11, t2) <= 0 Then
                        Dim num15 = t11.Year - ev.StartUtc.Year
                        If num15 >= 0 AndAlso num15 Mod num12 = 0 AndAlso (num <= 0 OrElse num15 / num12 + 1 <= num) Then
                            days.Add(t11.Day)
                        End If
                    End If
                    Return
                End If
                Dim list2 = ParseByDaySetPosList(text)
                If list2.Count > 0 Then
                    Dim num16 = If(num13 > 0, num13, ev.StartUtc.Month)
                    If month.Month <> num16 Then
                        Return
                    End If
                    Dim num17 = month.Year - ev.StartUtc.Year
                    If num17 < 0 OrElse num17 Mod num12 <> 0 Then
                        Return
                    End If
                    If True Then
                        For Each item2 In list2
                            Dim t12 = NthWeekdayInMonth(month.Year, month.Month, item2.Value, item2.Key)
                            If Date.Compare(t12, Date.MinValue) <> 0 Then
                                Dim t13 = t12.Date.Add(ev.StartUtc.TimeOfDay)
                                If Date.Compare(t13, t2) <= 0 AndAlso Date.Compare(t13, ev.StartUtc) >= 0 AndAlso Date.Compare(t13, dateTime) >= 0 AndAlso Date.Compare(t13, t) < 0 AndAlso (num <= 0 OrElse num17 / num12 + 1 <= num) Then
                                    days.Add(t13.Day)
                                End If
                            End If
                        Next
                        Return
                    End If
                End If
                If num13 > 0 AndAlso month.Month <> num13 Then
                    Return
                End If
                Dim t14 = ev.StartUtc
                While Date.Compare(t14, dateTime) < 0
                    t14 = t14.AddYears(num12)
                End While
                While Date.Compare(t14, t) < 0 AndAlso Date.Compare(t14, t2) <= 0
                    If num13 > 0 AndAlso t14.Month <> num13 Then
                        t14 = t14.AddYears(num12)
                        Continue While
                    End If
                    If num > 0 Then
                        Dim num18 = t14.Year - ev.StartUtc.Year + 1
                        If num18 > num Then
                            Exit While
                        End If
                    End If
                    If Date.Compare(t14, dateTime) >= 0 Then
                        days.Add(t14.Day)
                    End If
                    t14 = t14.AddYears(num12)
                End While
            End If
        End Sub

        Private Shared Function ParseUntilUtc(rule As String) As Date
            Dim text = If(Not Equals(rule, Nothing), rule, "")
            Dim num = text.IndexOf("UNTIL=", StringComparison.OrdinalIgnoreCase)
            If num < 0 Then
                Return Date.MaxValue
            End If
            Dim text2 = text.Substring(num + 6)
            Dim num2 = text2.IndexOf(";"c)
            If num2 >= 0 Then
                text2 = text2.Substring(0, num2)
            End If
            text2 = text2.Trim()
            If text2.Length >= 8 Then
                Dim s = text2.Substring(0, 8)
                Dim result = Date.MinValue
                If Date.TryParseExact(s, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, result) Then
                    Return result.ToUniversalTime()
                End If
            End If
            Return Date.MaxValue
        End Function

        Private Shared Function ParseWkst(rule As String, defaultDay As DayOfWeek) As DayOfWeek
            Dim text = If(Not Equals(rule, Nothing), rule, "")
            Dim num = text.IndexOf("WKST=", StringComparison.OrdinalIgnoreCase)
            If num < 0 Then
                Return defaultDay
            End If
            Dim text2 = text.Substring(num + 5)
            Dim num2 = text2.IndexOf(";"c)
            If num2 >= 0 Then
                text2 = text2.Substring(0, num2)
            End If
            Dim text3 As String = text2.Trim().ToUpperInvariant()
            If text3.Length > 2 Then
                text3 = text3.Substring(text3.Length - 2)
            End If
            Dim dayOfWeek = DayCodeToDayOfWeek(text3)
            If dayOfWeek.HasValue Then
                Return dayOfWeek.Value
            End If
            Return defaultDay
        End Function

        Private Shared Function WeeksSinceStart(startDate As Date, candidateDate As Date, wkst As DayOfWeek) As Integer
            Dim dateTime = StartOfWeek(startDate, wkst)
            Dim dateTime2 = StartOfWeek(candidateDate, wkst)
            If Date.Compare(dateTime2, dateTime) < 0 Then
                Return -1
            End If
            Return Math.Round((dateTime2 - dateTime).TotalDays / 7.0)
        End Function

        Private Shared Function StartOfWeek(dt As Date, wkst As DayOfWeek) As Date
            Dim result = dt.Date
            While result.DayOfWeek <> wkst
                result = result.AddDays(-1.0)
            End While
            Return result
        End Function

        Private Shared Function ParseByMonth(rule As String) As Integer
            Dim text = If(Not Equals(rule, Nothing), rule, "")
            Dim num = text.IndexOf("BYMONTH=", StringComparison.OrdinalIgnoreCase)
            If num < 0 Then
                Return 0
            End If
            Dim text2 = text.Substring(num + 8)
            Dim num2 = text2.IndexOf(";"c)
            If num2 >= 0 Then
                text2 = text2.Substring(0, num2)
            End If
            Dim result = 0
            If Integer.TryParse(text2.Trim(), result) AndAlso result >= 1 AndAlso result <= 12 Then
                Return result
            End If
            Return 0
        End Function

        Private Shared Function ParseByMonthDay(rule As String) As Integer
            Dim text = If(Not Equals(rule, Nothing), rule, "")
            Dim num = text.IndexOf("BYMONTHDAY=", StringComparison.OrdinalIgnoreCase)
            If num < 0 Then
                Return 0
            End If
            Dim text2 = text.Substring(num + 11)
            Dim num2 = text2.IndexOf(";"c)
            If num2 >= 0 Then
                text2 = text2.Substring(0, num2)
            End If
            Dim result = 0
            If Integer.TryParse(text2.Trim(), result) Then
                Select Case result
                    Case -1
                        Return -1
                    Case 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31
                        Return result
                End Select
            End If
            Return 0
        End Function

        Private Shared Function ResolveMonthDay(year As Integer, month As Integer, byMonthDay As Integer) As Integer
            Dim num = Date.DaysInMonth(year, month)
            If byMonthDay = -1 Then
                Return num
            End If
            Return Math.Min(byMonthDay, num)
        End Function

        Private Shared Function ParseCount(rule As String, defaultValue As Integer) As Integer
            Dim text = If(Not Equals(rule, Nothing), rule, "")
            Dim num = text.IndexOf("COUNT=", StringComparison.OrdinalIgnoreCase)
            If num < 0 Then
                Return defaultValue
            End If
            Dim text2 = text.Substring(num + 6)
            Dim num2 = text2.IndexOf(";"c)
            If num2 >= 0 Then
                text2 = text2.Substring(0, num2)
            End If
            Dim result = 0
            If Integer.TryParse(text2.Trim(), result) AndAlso result > 0 Then
                Return result
            End If
            Return defaultValue
        End Function

        Private Shared Function ParseInterval(rule As String, defaultValue As Integer) As Integer
            Dim text = If(Not Equals(rule, Nothing), rule, "")
            Dim num = text.IndexOf("INTERVAL=", StringComparison.OrdinalIgnoreCase)
            If num < 0 Then
                Return defaultValue
            End If
            Dim text2 = text.Substring(num + 9)
            Dim num2 = text2.IndexOf(";"c)
            If num2 >= 0 Then
                text2 = text2.Substring(0, num2)
            End If
            Dim result = 0
            If Integer.TryParse(text2.Trim(), result) AndAlso result > 0 Then
                Return result
            End If
            Return defaultValue
        End Function

        Private Shared Function NthWeekdayInMonth(year As Integer, month As Integer, dow As DayOfWeek, n As Integer) As Date
            If n = 0 Then
                Return Date.MinValue
            End If
            Dim num = Date.DaysInMonth(year, month)
            If n < 0 Then
                Dim result = Date.MinValue
                Dim num2 = num
                For i = 1 To num2
                    Dim dateTime As Date = New DateTime(year, month, i, 0, 0, 0, DateTimeKind.Utc)
                    If dateTime.DayOfWeek = dow Then
                        result = dateTime
                    End If
                Next
                Return result
            End If
            Dim num3 = 0
            Dim num4 = num
            For j = 1 To num4
                Dim result2 As Date = New DateTime(year, month, j, 0, 0, 0, DateTimeKind.Utc)
                If result2.DayOfWeek = dow Then
                    num3 += 1
                    If num3 = n Then
                        Return result2
                    End If
                End If
            Next
            Return Date.MinValue
        End Function

        Private Shared Function ParseByDaySetPosList(rule As String) As List(Of KeyValuePair(Of Integer, DayOfWeek))
            Dim list As List(Of KeyValuePair(Of Integer, DayOfWeek)) = New List(Of KeyValuePair(Of Integer, DayOfWeek))()
            Dim text = If(Not Equals(rule, Nothing), rule, "")
            Dim num = text.IndexOf("BYDAY=", StringComparison.OrdinalIgnoreCase)
            If num < 0 Then
                Return list
            End If
            Dim text2 = text.Substring(num + 6)
            Dim num2 = text2.IndexOf(";"c)
            If num2 >= 0 Then
                text2 = text2.Substring(0, num2)
            End If
            Dim array = text2.Split(","c)
            For Each text3 In array
                Dim text4 As String = text3.Trim().ToUpperInvariant()
                If text4.Length < 3 Then
                    Continue For
                End If
                Dim text5 = ""
                Dim j = 0
                If j < text4.Length AndAlso text4(j) = "-"c Then
                    text5 = "-"
                    j += 1
                End If
                While j < text4.Length AndAlso Char.IsDigit(text4(j))
                    text5 += Conversions.ToString(text4(j))
                    j += 1
                End While
                If text5.Length = 0 Then
                    Continue For
                End If
                Dim result = 0
                If Integer.TryParse(text5, result) AndAlso result <> 0 Then
                    Dim text6 = text4.Substring(j)
                    If text6.Length > 2 Then
                        text6 = text6.Substring(text6.Length - 2)
                    End If
                    Dim dayOfWeek = DayCodeToDayOfWeek(text6)
                    If dayOfWeek.HasValue Then
                        Dim item As KeyValuePair(Of Integer, DayOfWeek) = New KeyValuePair(Of Integer, DayOfWeek)(result, dayOfWeek.Value)
                        list.Add(item)
                    End If
                End If
            Next
            Return list
        End Function

        Private Shared Function DayCodeToDayOfWeek(code As String) As DayOfWeek?
            Dim dayOfWeek As DayOfWeek? = Nothing
            Return
        End Function

        Private Shared Function ParseByDaySet(rule As String) As HashSet(Of DayOfWeek)
            Dim hashSet As HashSet(Of DayOfWeek) = New HashSet(Of DayOfWeek)()
            Dim text = If(Not Equals(rule, Nothing), rule, "")
            Dim num = text.IndexOf("BYDAY=", StringComparison.OrdinalIgnoreCase)
            If num < 0 Then
                Return hashSet
            End If
            Dim text2 = text.Substring(num + 6)
            Dim num2 = text2.IndexOf(";"c)
            If num2 >= 0 Then
                text2 = text2.Substring(0, num2)
            End If
            Dim array = text2.Split(","c)
            For Each text3 In array
                Dim text4 As String = text3.Trim().ToUpperInvariant()
                If text4.Length >= 2 Then
                    Dim text5 = text4
                    If text5.Length > 2 Then
                        text5 = text5.Substring(text5.Length - 2)
                    End If
                    Select Case text5
                        Case "SU"
                            hashSet.Add(DayOfWeek.Sunday)
                        Case "MO"
                            hashSet.Add(DayOfWeek.Monday)
                        Case "TU"
                            hashSet.Add(DayOfWeek.Tuesday)
                        Case "WE"
                            hashSet.Add(DayOfWeek.Wednesday)
                        Case "TH"
                            hashSet.Add(DayOfWeek.Thursday)
                        Case "FR"
                            hashSet.Add(DayOfWeek.Friday)
                        Case "SA"
                            hashSet.Add(DayOfWeek.Saturday)
                    End Select
                End If
            Next
            Return hashSet
        End Function

        Public Shared Function IsOccurrenceExcluded(ev As CalendarEvent, occurrenceUtc As Date) As Boolean
            If ev Is Nothing OrElse Date.Compare(occurrenceUtc, Date.MinValue) = 0 Then
                Return False
            End If
            Dim text As String = If(Not Equals(ev.ExDateUtcCsv, Nothing), ev.ExDateUtcCsv, "").Trim()
            If text.Length = 0 Then
                Return False
            End If
            Dim b As String = occurrenceUtc.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture)
            Dim array = text.Split(New Char(0) {","c}, StringSplitOptions.RemoveEmptyEntries)
            For Each text2 In array
                If String.Equals(text2.Trim(), b, StringComparison.OrdinalIgnoreCase) Then
                    Return True
                End If
            Next
            Return False
        End Function

        Public Shared Function TryClampRecurrenceUntilBeforeInstance(series As CalendarEvent, instanceStartUtc As Date) As Boolean
            If series Is Nothing OrElse Date.Compare(instanceStartUtc, Date.MinValue) = 0 Then
                Return False
            End If
            Dim text As String = If(Not Equals(series.RecurrenceRule, Nothing), series.RecurrenceRule, "").Trim()
            If text.Length = 0 Then
                Return False
            End If
            Dim num = text.IndexOf("UNTIL=", StringComparison.OrdinalIgnoreCase)
            If num < 0 Then
                Return False
            End If
            Dim num2 = num + 6
            Dim num3 = text.IndexOf(";"c, num2)
            If num3 < 0 Then
                num3 = text.Length
            End If
            Dim text2 As String = text.Substring(num2, num3 - num2).Trim()
            Dim dateTime = ParseUntilUtc(series.RecurrenceRule)
            If Date.Compare(dateTime, Date.MaxValue) = 0 Then
                Return False
            End If
            If Date.Compare(instanceStartUtc.ToUniversalTime(), dateTime) >= 0 Then
                Return False
            End If
            Dim text3 As String = instanceStartUtc.ToUniversalTime().AddDays(-1.0).ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture)
            series.RecurrenceRule = text.Substring(0, num2) & text3 & text.Substring(num3)
            series.CalDavDirty = True
            series.EwsDirty = True
            Return True
        End Function

        Public Shared Function TryDecrementRecurrenceCountOnSeries(series As CalendarEvent) As Boolean
            If series Is Nothing Then
                Return False
            End If
            Dim text As String = If(Not Equals(series.RecurrenceRule, Nothing), series.RecurrenceRule, "").Trim()
            If text.Length = 0 Then
                Return False
            End If
            Dim num = text.IndexOf("COUNT=", StringComparison.OrdinalIgnoreCase)
            If num < 0 Then
                Return False
            End If
            Dim num2 = num + 6
            Dim num3 = text.IndexOf(";"c, num2)
            If num3 < 0 Then
                num3 = text.Length
            End If
            Dim s As String = text.Substring(num2, num3 - num2).Trim()
            Dim result = 0
            If Not Integer.TryParse(s, result) OrElse result <= 1 Then
                Return False
            End If
            Dim recurrenceRule As String = text.Substring(0, num2) & (result - 1).ToString() & text.Substring(num3)
            series.RecurrenceRule = recurrenceRule
            series.CalDavDirty = True
            series.EwsDirty = True
            Return True
        End Function

        Public Shared Function TryAppendExDateOnSeriesMaster(all As List(Of CalendarEvent), series As CalendarEvent, instanceStartUtc As Date) As Boolean
            If all Is Nothing OrElse series Is Nothing OrElse String.IsNullOrWhiteSpace(series.Uid) Then
                Return False
            End If
            Dim text As String = instanceStartUtc.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture)
            For Each item In all
                If item Is Nothing OrElse Not String.Equals(item.Uid, series.Uid, StringComparison.OrdinalIgnoreCase) Then
                    Continue For
                End If
                Dim calendarEvent = item
                Dim text2 As String = If(Not Equals(calendarEvent.ExDateUtcCsv, Nothing), calendarEvent.ExDateUtcCsv, "").Trim()
                If text2.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0 Then
                    Return True
                End If
                item.ExDateUtcCsv = If(text2.Length = 0, text, text2 & "," & text)
                item.CalDavDirty = True
                item.EwsDirty = True
                Return True
            Next
            Return False
        End Function

        Public Shared Function TryCreateRecurrenceException(series As CalendarEvent, instanceStartUtc As Date) As CalendarEvent
            If series Is Nothing Then
                Return Nothing
            End If
            If String.IsNullOrWhiteSpace(series.RecurrenceRule) Then
                Return Nothing
            End If
            Dim calendarEvent As CalendarEvent = New CalendarEvent()
            calendarEvent.Uid = Guid.NewGuid().ToString()
            calendarEvent.Summary = series.Summary
            calendarEvent.Location = series.Location
            calendarEvent.StartUtc = instanceStartUtc
            calendarEvent.EndUtc = instanceStartUtc.Add(series.EndUtc - series.StartUtc)
            calendarEvent.AlarmMinutesBefore = series.AlarmMinutesBefore
            calendarEvent.Category = series.Category
            calendarEvent.CalendarName = series.CalendarName
            calendarEvent.RecurrenceParentUid = series.Uid
            calendarEvent.RecurrenceIdUtc = instanceStartUtc
            calendarEvent.CalDavDirty = True
            calendarEvent.EwsDirty = True
            Return calendarEvent
        End Function
    End Class
