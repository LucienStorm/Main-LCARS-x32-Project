Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Globalization
Imports System.Linq
Imports System.Windows.Forms
Imports LCARS
Imports Microsoft.VisualBasic

    Public Class frmCalendar
        Inherits LCARSForm
        Private Const FormW As Integer = 760

        Private Const FormH As Integer = 560

        Private lstEvents As ListBox

        Private lstTasks As ListView

        Private lblMonth As Label

        Private lblStatus As Label

        Private viewMonth As Date

        Private alarmTimer As Timer

        Private ReadOnly firedAlarmKeys As HashSet(Of String)

        Private ReadOnly monthEvents As List(Of CalendarEvent)

        Private showTasksOnly As Boolean

        Private showWeekView As Boolean

        Private showInvitesOnly As Boolean

        Private showRecurringOnly As Boolean

        Private showAlarmsOnly As Boolean

        Private monthGrid As CalendarMonthGrid

        Private cboCalendar As ComboBox

        Private cboCategory As ComboBox

        Private calDavLiveTimer As Timer

        Private selectedCalendarFilter As String

        Private selectedCategoryFilter As String

        Private lastCalendarFilterSignature As String

        Private lastCategoryFilterSignature As String

        Private rebuildingCalendarFilter As Boolean

        Private rebuildingCategoryFilter As Boolean

        Public Sub New()
            viewMonth = Date.Today
            firedAlarmKeys = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            monthEvents = New List(Of CalendarEvent)()
            selectedCalendarFilter = ""
            selectedCategoryFilter = ""
            lastCalendarFilterSignature = ""
            lastCategoryFilterSignature = ""
            modUtilities.InitUtilityFormFloating(Me, 760, 560)
            BuildUi()
            RefreshEvents()
            StartAlarmTimer()
            StartCalDavLiveTimer()
        End Sub

        Private Sub BuildUi()
            modUtilities.MakeUtilityTitle(Me, "CALENDAR", 340)
            modUtilities.MakeUtilityButton(Me, "IMPORT ICS", 338, 12, 120, 28, LCARScolorStyles.PrimaryFunction, New EventHandler(AddressOf OnImportIcs))
            modUtilities.MakeUtilityButton(Me, "ADD", 468, 12, 80, 28, LCARScolorStyles.MiscFunction, New EventHandler(AddressOf OnAddEvent))
            modUtilities.MakeUtilityButton(Me, "ADD TASK", 128, 48, 100, 28, LCARScolorStyles.CriticalFunction, New EventHandler(AddressOf OnAddTask))
            modUtilities.MakeUtilityButton(Me, "TASKS", 238, 48, 80, 28, LCARScolorStyles.NavigationFunction, New EventHandler(AddressOf OnToggleTasksView))
            modUtilities.MakeUtilityButton(Me, "WEEK", 328, 48, 72, 28, LCARScolorStyles.NavigationFunction, New EventHandler(AddressOf OnToggleWeekView))
            modUtilities.MakeUtilityButton(Me, "INVITES", 128, 84, 88, 28, LCARScolorStyles.CriticalFunction, New EventHandler(AddressOf OnToggleInvitesView))
            modUtilities.MakeUtilityButton(Me, "RECUR", 228, 84, 72, 28, LCARScolorStyles.NavigationFunction, New EventHandler(AddressOf OnToggleRecurringView))
            modUtilities.MakeUtilityButton(Me, "ALARM", 308, 84, 72, 28, LCARScolorStyles.SystemFunction, New EventHandler(AddressOf OnToggleAlarmsView))
            modUtilities.MakeUtilityButton(Me, "EDIT", 218, 48, 80, 28, LCARScolorStyles.PrimaryFunction, New EventHandler(AddressOf OnEditEvent))
            modUtilities.MakeUtilityButton(Me, "DELETE", 308, 48, 90, 28, LCARScolorStyles.Orange, New EventHandler(AddressOf OnDeleteEvent))
            modUtilities.MakeUtilityButton(Me, "CALDAV", 408, 48, 90, 28, LCARScolorStyles.NavigationFunction, New EventHandler(AddressOf OnCalDavSync))
            modUtilities.MakeUtilityButton(Me, "ACCEPT", 508, 48, 72, 28, LCARScolorStyles.MiscFunction, Sub(s, e) ApplyMeetingRsvp("ACCEPTED"))
            modUtilities.MakeUtilityButton(Me, "TENT", 588, 48, 72, 28, LCARScolorStyles.SystemFunction, Sub(s, e) ApplyMeetingRsvp("TENTATIVE"))
            modUtilities.MakeUtilityButton(Me, "DECLINE", 668, 48, 72, 28, LCARScolorStyles.Orange, Sub(s, e) ApplyMeetingRsvp("DECLINED"))
            modUtilities.MakeUtilityButton(Me, "DETACH", 128, 120, 88, 28, LCARScolorStyles.SystemFunction, New EventHandler(AddressOf OnDetachRecurrenceInstance))
            modUtilities.MakeUtilityButton(Me, "DONE", 248, 12, 80, 28, LCARScolorStyles.SystemFunction, New EventHandler(AddressOf OnToggleTaskDone))
            modUtilities.MakeUtilityButton(Me, "PREV", 558, 12, 80, 28, LCARScolorStyles.NavigationFunction, Sub(s, e) ShiftMonth(-1))
            modUtilities.MakeUtilityButton(Me, "NEXT", 648, 12, 80, 28, LCARScolorStyles.NavigationFunction, Sub(s, e) ShiftMonth(1))
            modUtilities.MakeUtilityButton(Me, "CLOSE", 628, 48, 110, 28, LCARScolorStyles.Orange, Sub(s, e) Close())
            lblMonth = modUtilities.MakeUtilityLabel(Me, 12, 48, 480, 24, 16F)
            cboCalendar = New ComboBox()
            cboCalendar.DropDownStyle = ComboBoxStyle.DropDownList
            Dim comboBox = cboCalendar
            Dim location As Point = New Point(488, 48)
            comboBox.Location = location
            Dim comboBox2 = cboCalendar
            Dim size As Size = New Size(250, 24)
            comboBox2.Size = size
            cboCalendar.BackColor = Color.FromArgb(40, 40, 40)
            cboCalendar.ForeColor = Color.Orange
            AddHandler cboCalendar.SelectedIndexChanged, AddressOf OnCalendarFilterChanged
            Controls.Add(cboCalendar)
            cboCategory = New ComboBox()
            cboCategory.DropDownStyle = ComboBoxStyle.DropDownList
            Dim comboBox3 = cboCategory
            location = New Point(488, 72)
            comboBox3.Location = location
            Dim comboBox4 = cboCategory
            size = New Size(250, 24)
            comboBox4.Size = size
            cboCategory.BackColor = Color.FromArgb(40, 40, 40)
            cboCategory.ForeColor = Color.Orange
            AddHandler cboCategory.SelectedIndexChanged, AddressOf OnCategoryFilterChanged
            Controls.Add(cboCategory)
            monthGrid = New CalendarMonthGrid()
            Dim calendarMonthGrid = monthGrid
            location = New Point(12, 76)
            calendarMonthGrid.Location = location
            Dim calendarMonthGrid2 = monthGrid
            size = New Size(200, 160)
            calendarMonthGrid2.Size = size
            Controls.Add(monthGrid)
            lstEvents = New ListBox()
            Dim listBox = lstEvents
            location = New Point(222, 76)
            listBox.Location = location
            Dim listBox2 = lstEvents
            size = New Size(526, 434)
            listBox2.Size = size
            lstEvents.BackColor = Color.FromArgb(30, 30, 30)
            lstEvents.ForeColor = Color.Orange
            lstEvents.Font = New Font("Arial Narrow", 11F)
            AddHandler lstEvents.DoubleClick, AddressOf OnEditEvent
            Controls.Add(lstEvents)
            lstTasks = New ListView()
            lstTasks.View = View.Details
            lstTasks.FullRowSelect = True
            lstTasks.HeaderStyle = ColumnHeaderStyle.Nonclickable
            lstTasks.BackColor = Color.FromArgb(30, 30, 30)
            lstTasks.ForeColor = Color.Orange
            lstTasks.Font = New Font("Arial Narrow", 11F)
            lstTasks.Location = lstEvents.Location
            lstTasks.Size = lstEvents.Size
            lstTasks.Columns.Add("DUE", 130)
            lstTasks.Columns.Add("PRI", 36)
            lstTasks.Columns.Add("%", 36)
            lstTasks.Columns.Add("SUMMARY", 480)
            lstTasks.Visible = False
            AddHandler lstTasks.DoubleClick, AddressOf OnEditEvent
            Controls.Add(lstTasks)
            lblStatus = modUtilities.MakeUtilityLabel(Me, 12, 520, 736, 20, 10F)
        End Sub

        Private Function SelectedMonthEventIndex() As Integer
            If showTasksOnly AndAlso lstTasks IsNot Nothing AndAlso lstTasks.Visible Then
                If lstTasks.SelectedIndices.Count = 0 Then
                    Return -1
                End If
                Return lstTasks.SelectedIndices(0)
            End If
            If lstEvents Is Nothing Then
                Return -1
            End If
            Return lstEvents.SelectedIndex
        End Function

        Private Sub ShiftMonth(delta As Integer)
            viewMonth = viewMonth.AddMonths(delta)
            RefreshEvents()
        End Sub

        Private Sub RefreshEvents()
            Dim text As String = viewMonth.ToString("MMMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant()
            Dim text2 As String = CalDavCalendarSync.TryGetPrimaryCalendarFriendlyLabel()
            If text2.Length > 0 AndAlso Not showTasksOnly Then
                text = text & " · " & text2
            End If
            lblMonth.Text = text
            lstEvents.Items.Clear()
            If lstTasks IsNot Nothing Then
                lstTasks.Items.Clear()
            End If
            monthEvents.Clear()
            Dim list As List(Of CalendarEvent) = IcsCalendarStore.LoadEvents()
            MaybeRebuildCalendarFilter(list)
            MaybeRebuildCategoryFilter(list)
            Dim text3 As String = If(Not Equals(selectedCalendarFilter, Nothing), selectedCalendarFilter, "").Trim()
            Dim flag = If(text3.Length > 0 AndAlso Not text3.Equals("ALL CALENDARS", StringComparison.OrdinalIgnoreCase), True, False)
            Dim text4 As String = If(Not Equals(selectedCategoryFilter, Nothing), selectedCategoryFilter, "").Trim()
            Dim flag2 = If(text4.Length > 0 AndAlso Not text4.Equals("ALL CATEGORIES", StringComparison.OrdinalIgnoreCase), True, False)
            Dim t As Date = New DateTime(viewMonth.Year, viewMonth.Month, 1, 0, 0, 0, DateTimeKind.Utc)
            Dim t2 = t.AddMonths(1)
            Dim num = 0
            Dim [date] = Date.UtcNow.Date
            Dim t3 = [date].AddDays(7.0)
            Dim hashSet As HashSet(Of String) = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            For Each item In list
                If item IsNot Nothing Then
                    hashSet.Add(EventCalendarLabel(item))
                End If
            Next
            Dim flag3 = If(Not flag AndAlso hashSet.Count > 1, True, False)
            For Each item2 In list.OrderBy(Function(x) x.StartUtc)
                If flag AndAlso Not String.Equals(EventCalendarLabel(item2), text3, StringComparison.OrdinalIgnoreCase) OrElse flag2 AndAlso Not String.Equals(EventCategoryLabel(item2), text4, StringComparison.OrdinalIgnoreCase) Then
                    Continue For
                End If
                If showInvitesOnly Then
                    If Not item2.IsMeetingInvitation Then
                        Continue For
                    End If
                ElseIf showRecurringOnly Then
                    If String.IsNullOrWhiteSpace(item2.RecurrenceRule) Then
                        Continue For
                    End If
                ElseIf showAlarmsOnly Then
                    If item2.AlarmMinutesBefore <= 0 Then
                        Continue For
                    End If
                ElseIf showTasksOnly Then
                    If Not CalDavCalendarSync.IsTaskEvent(item2) Then
                        Continue For
                    End If
                ElseIf showWeekView Then
                    If Date.Compare(item2.StartUtc, [date]) < 0 OrElse Date.Compare(item2.StartUtc, t3) >= 0 Then
                        Continue For
                    End If
                ElseIf Date.Compare(item2.StartUtc, t) < 0 OrElse Date.Compare(item2.StartUtc, t2) >= 0 Then
                    Continue For
                End If
                monthEvents.Add(item2)
                Dim calendarEvent = item2
                Dim text5 = If(Not Equals(calendarEvent.Summary, Nothing), calendarEvent.Summary, "")
                If item2.TaskCompleted AndAlso Not text5.StartsWith("[DONE]", StringComparison.OrdinalIgnoreCase) Then
                    text5 = "[DONE] " & text5
                End If
                If Not String.IsNullOrWhiteSpace(item2.Category) Then
                    text5 = "[" & item2.Category.Trim().ToUpperInvariant() & "] " & text5
                End If
                If Not String.IsNullOrWhiteSpace(item2.RecurrenceRule) Then
                    text5 = text5 & " ⟳ " & item2.RecurrenceRule.Trim()
                End If
                If item2.IsMeetingInvitation Then
                    calendarEvent = item2
                    Dim text6 As String = If(Not Equals(calendarEvent.MyPartStat, Nothing), calendarEvent.MyPartStat, "NEEDS-ACTION").Trim().ToUpperInvariant()
                    text5 = "✉ " & text6 & " " & text5
                End If
                If showTasksOnly AndAlso lstTasks IsNot Nothing Then
                    Dim text7 As String = item2.StartUtc.ToLocalTime().ToString("ddd MMM d HH:mm")
                    Dim listViewItem As ListViewItem = New ListViewItem(text7)
                    listViewItem.SubItems.Add(item2.TaskPriority.ToString())
                    listViewItem.SubItems.Add(item2.TaskPercentComplete.ToString())
                    If item2.AlarmMinutesBefore > 0 Then
                        text5 = "⏰-" & item2.AlarmMinutesBefore.ToString() & "m " & text5
                    End If
                    listViewItem.SubItems.Add(text5)
                    lstTasks.Items.Add(listViewItem)
                Else
                    Dim text8 = ""
                    If item2.AlarmMinutesBefore > 0 Then
                        text8 = "⏰-" & item2.AlarmMinutesBefore.ToString() & "m "
                    End If
                    Dim text9 = ""
                    If flag3 Then
                        text9 = "[" & EventCalendarLabel(item2) & "] "
                    End If
                    lstEvents.Items.Add(item2.StartUtc.ToLocalTime().ToString("ddd MMM d HH:mm") & "  " & text8 & text9 & text5)
                End If
                num = num + 1
            Next
            If lstTasks IsNot Nothing Then
                lstTasks.Visible = showTasksOnly
            End If
            If lstEvents IsNot Nothing Then
                lstEvents.Visible = Not showTasksOnly
            End If
            Dim text10 = " EVENT(S) THIS MONTH"
            If showInvitesOnly Then
                text10 = " MEETING INVITE(S) — LIGHTNING VIEW"
            End If
            If showRecurringOnly Then
                text10 = " RECURRING SERIES — LIGHTNING VIEW"
            End If
            If showAlarmsOnly Then
                text10 = " REMINDER(S) — LIGHTNING VIEW"
            End If
            If showTasksOnly Then
                text10 = " TASK(S) — LIGHTNING GRID"
            End If
            If showWeekView Then
                text10 = " EVENT(S) THIS WEEK"
            End If
            Dim hashSet2 As HashSet(Of String) = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            For Each monthEvent In monthEvents
                If monthEvent IsNot Nothing Then
                    hashSet2.Add(EventCalendarLabel(monthEvent))
                End If
            Next
            Dim text11 As String = (If(hashSet2.Count > 1, (" · " & hashSet2.Count.ToString() & " CALS"), ""))
            Dim num2 As Integer = CalDavCalendarSync.CountKnownCalendarUrlLabels()
            If num2 > 0 Then
                text11 = text11 & " · DAV-LABELS " & num2.ToString()
            End If
            If flag2 AndAlso text4.Length > 0 Then
                text11 = text11 & " · CAT " & text4
            End If
            If flag AndAlso text3.Length > 0 AndAlso Not text3.Equals("ALL CALENDARS", StringComparison.OrdinalIgnoreCase) Then
                text11 = text11 & " · CAL " & text3
            End If
            lblStatus.Text = num.ToString() & text10 & text11
            If monthGrid IsNot Nothing Then
                monthGrid.SetMonth(viewMonth, IcsCalendarStore.LoadEvents())
            End If
        End Sub

        Private Shared Sub MarkEventDirty(ev As CalendarEvent)
            If ev IsNot Nothing Then
                ev.EwsDirty = True
                ev.CalDavDirty = True
            End If
        End Sub

        Private Sub OnDetachRecurrenceInstance(sender As Object, e As EventArgs)
            Dim num As Integer = SelectedMonthEventIndex()
            If num < 0 OrElse num >= monthEvents.Count Then
                lblStatus.Text = "SELECT A RECURRING EVENT"
                Return
            End If
            Dim calendarEvent = monthEvents(num)
            If calendarEvent Is Nothing OrElse String.IsNullOrWhiteSpace(calendarEvent.RecurrenceRule) Then
                lblStatus.Text = "NOT A RECURRING SERIES"
                Return
            End If
            Dim calendarEvent2 = CalendarRecurrence.TryCreateRecurrenceException(calendarEvent, calendarEvent.StartUtc)
            If calendarEvent2 Is Nothing Then
                lblStatus.Text = "DETACH FAILED"
                Return
            End If
            Dim list As List(Of CalendarEvent) = IcsCalendarStore.LoadEvents()
            CalendarRecurrence.TryAppendExDateOnSeriesMaster(list, calendarEvent, calendarEvent.StartUtc)
            CalendarRecurrence.TryDecrementRecurrenceCountOnSeries(calendarEvent)
            CalendarRecurrence.TryClampRecurrenceUntilBeforeInstance(calendarEvent, calendarEvent.StartUtc)
            list.Add(calendarEvent2)
            IcsCalendarStore.SaveEvents(list)
            CalDavCalendarSync.TryPushEvent(calendarEvent)
            CalDavCalendarSync.TryPushEvent(calendarEvent2)
            RefreshEvents()
            lblStatus.Text = "RECURRENCE EXCEPTION + EXDATE ON SERIES (SLICE 15 PARTIAL)"
        End Sub

        Private Sub ApplyMeetingRsvp(partStat As String)
            Dim num As Integer = SelectedMonthEventIndex()
            If num < 0 OrElse num >= monthEvents.Count Then
                lblStatus.Text = "SELECT A MEETING INVITE"
                Return
            End If
            Dim calendarEvent = monthEvents(num)
            If calendarEvent Is Nothing OrElse String.IsNullOrWhiteSpace(calendarEvent.Uid) Then
                Return
            End If
            Dim text As String = If(Not Equals(partStat, Nothing), partStat, "").Trim().ToUpperInvariant()
            If text.Length = 0 Then
                Return
            End If
            Dim list As List(Of CalendarEvent) = IcsCalendarStore.LoadEvents()
            Dim flag = False
            For Each item In list
                If item Is Nothing OrElse Not String.Equals(item.Uid, calendarEvent.Uid, StringComparison.OrdinalIgnoreCase) Then
                    Continue For
                End If
                item.MyPartStat = text
                item.IsMeetingInvitation = True
                If String.IsNullOrWhiteSpace(item.AttendeeMailto) Then
                    item.AttendeeMailto = MailAttendeeHint.TryGetPrimaryAttendeeMailto()
                End If
                MarkEventDirty(item)
                flag = True
                Exit For
            Next
            If Not flag Then
                lblStatus.Text = "RSVP TARGET NOT FOUND"
                Return
            End If
            IcsCalendarStore.SaveEvents(list)
            Dim calendarEvent2 As CalendarEvent = Nothing
            For Each item2 In list
                If item2 IsNot Nothing AndAlso String.Equals(item2.Uid, calendarEvent.Uid, StringComparison.OrdinalIgnoreCase) Then
                    calendarEvent2 = item2
                    Exit For
                End If
            Next
            Dim flag2 = False
            If calendarEvent2 IsNot Nothing Then
                calendarEvent2.CalDavDirty = True
                flag2 = CalDavCalendarSync.TryPushEvent(calendarEvent2)
                If flag2 Then
                    IcsCalendarStore.SaveEvents(list)
                End If
            End If
            If calendarEvent2 IsNot Nothing Then
                EwsCalendarRsvpQueue.Enqueue(calendarEvent2.Uid, text)
                CalendarMeetingReplyQueue.Enqueue(calendarEvent2.Uid, text, calendarEvent2.Organizer, calendarEvent2.Summary, calendarEvent2.AttendeeMailto)
            End If
            Dim flag3 = False
            If calendarEvent2 IsNot Nothing Then
                flag3 = MailAttendeeHint.TryLaunchOrganizerRsvpMail(calendarEvent2.Organizer, calendarEvent2.Summary, text)
            End If
            RefreshEvents()
            Dim text2 = If(flag3, " · ORGANIZER MAILTO", " · MAIL RSVP PENDING")
            Dim text3 = " · EWS+IMIP QUEUED"
            lblStatus.Text = "RSVP " & text & If(flag2, " — CALDAV ATTENDEE PUSHED", " — CALDAV PUSH PENDING") & text2 & text3
        End Sub

        Private Sub OnToggleInvitesView(sender As Object, e As EventArgs)
            showInvitesOnly = Not showInvitesOnly
            If showInvitesOnly Then
                showTasksOnly = False
                showWeekView = False
                showRecurringOnly = False
                showAlarmsOnly = False
            End If
            RefreshEvents()
        End Sub

        Private Sub OnToggleRecurringView(sender As Object, e As EventArgs)
            showRecurringOnly = Not showRecurringOnly
            If showRecurringOnly Then
                showTasksOnly = False
                showWeekView = False
                showInvitesOnly = False
                showAlarmsOnly = False
            End If
            RefreshEvents()
            lblStatus.Text = If(showRecurringOnly, "RECURRING SERIES VIEW", "MONTH VIEW")
        End Sub

        Private Sub OnToggleAlarmsView(sender As Object, e As EventArgs)
            showAlarmsOnly = Not showAlarmsOnly
            If showAlarmsOnly Then
                showTasksOnly = False
                showWeekView = False
                showInvitesOnly = False
                showRecurringOnly = False
            End If
            RefreshEvents()
            lblStatus.Text = If(showAlarmsOnly, "REMINDER EVENTS VIEW", "MONTH VIEW")
        End Sub

        Private Sub StartCalDavLiveTimer()
            calDavLiveTimer = New Timer()
            calDavLiveTimer.Interval = 4000
            AddHandler calDavLiveTimer.Tick, AddressOf OnCalDavLiveTick
            calDavLiveTimer.Start()
        End Sub

        Private Sub OnCalDavLiveTick(sender As Object, e As EventArgs)
            Dim num As Integer = CalDavCalendarSync.TrySyncBidirectional()
            If lblStatus IsNot Nothing Then
                Dim text As String = BuildCalDavStatusHint()
                If num < 0 Then
                    lblStatus.Text = "CALDAV LIVE SYNC FAILED" & text
                ElseIf num > 0 Then
                    Dim text2 = ""
                    If CalDavCalendarSync.LastBidirectionalPushCount > 0 Then
                        text2 = " · PUSH " & CalDavCalendarSync.LastBidirectionalPushCount.ToString()
                    End If
                    lblStatus.Text = "CALDAV LIVE " & num.ToString() & " ITEM(S) SYNCED" & text2 & text
                End If
            End If
            If num <> 0 Then
                RefreshEvents()
            End If
        End Sub

        Private Shared Function BuildCalDavStatusHint() As String
            Dim text = ""
            Dim text2 As String = CalDavCalendarSync.TryGetPrimaryCalendarFriendlyLabel()
            If text2.Length > 0 Then
                text = text & " · " & text2
            End If
            Dim num As Integer = CalDavCalendarSync.CountKnownCalendarUrlLabels()
            If num > 0 Then
                text = text & " · LABELS " & num.ToString()
            End If
            If CalDavCalendarSync.LastPrimaryEventsTagged > 0 Then
                text = text & " · TAG " & CalDavCalendarSync.LastPrimaryEventsTagged.ToString()
            End If
            If CalDavCalendarSync.LastSecondaryEventsTagged > 0 Then
                text = text & " · 2ND-TAG " & CalDavCalendarSync.LastSecondaryEventsTagged.ToString()
            End If
            If CalDavCalendarSync.LastTaskEventsTagged > 0 Then
                text = text & " · TASK-TAG " & CalDavCalendarSync.LastTaskEventsTagged.ToString()
            End If
            If CalDavCalendarSync.LastPullMergeAdded > 0 Then
                text = text & " · PULL " & CalDavCalendarSync.LastPullMergeAdded.ToString()
            End If
            If CalDavCalendarSync.LastSecondaryPullMergeAdded > 0 Then
                text = text & " · 2ND-PULL " & CalDavCalendarSync.LastSecondaryPullMergeAdded.ToString()
            End If
            If CalDavCalendarSync.LastUrlLabelsAtPull > 0 Then
                text = text & " · PROP " & CalDavCalendarSync.LastUrlLabelsAtPull.ToString()
            End If
            If CalDavCalendarSync.LastPrimaryPullMergeAdded > 0 Then
                text = text & " · 1ST-PULL " & CalDavCalendarSync.LastPrimaryPullMergeAdded.ToString()
            End If
            If CalDavCalendarSync.LastTaskPullMergeAdded > 0 Then
                text = text & " · TASK-PULL " & CalDavCalendarSync.LastTaskPullMergeAdded.ToString()
            End If
            Return text
        End Function

        Private Sub OnCalendarFilterChanged(sender As Object, e As EventArgs)
            If Not rebuildingCalendarFilter AndAlso cboCalendar IsNot Nothing AndAlso cboCalendar.SelectedItem IsNot Nothing Then
                selectedCalendarFilter = cboCalendar.SelectedItem.ToString().Trim()
                RefreshEvents()
            End If
        End Sub

        Private Sub OnCategoryFilterChanged(sender As Object, e As EventArgs)
            If Not rebuildingCategoryFilter AndAlso cboCategory IsNot Nothing AndAlso cboCategory.SelectedItem IsNot Nothing Then
                selectedCategoryFilter = cboCategory.SelectedItem.ToString().Trim()
                RefreshEvents()
            End If
        End Sub

        Private Sub MaybeRebuildCalendarFilter(events As IList(Of CalendarEvent))
            Dim hashSet As HashSet(Of String) = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            hashSet.Add("ALL CALENDARS")
            If events IsNot Nothing Then
                For Each [event] In events
                    hashSet.Add(EventCalendarLabel([event]))
                Next
            End If
            Dim a = String.Join("|", hashSet.OrderBy(Function(x) x))
            If Not String.Equals(a, lastCalendarFilterSignature, StringComparison.Ordinal) Then
                lastCalendarFilterSignature = a
                RebuildCalendarFilter(hashSet)
            End If
        End Sub

        Private Sub RebuildCalendarFilter(names As IEnumerable(Of String))
            If cboCalendar Is Nothing Then
                Return
            End If
            Dim text = selectedCalendarFilter
            rebuildingCalendarFilter = True
            cboCalendar.Items.Clear()
            For Each item In names.OrderBy(Function(x) x)
                cboCalendar.Items.Add(item)
            Next
            rebuildingCalendarFilter = False
            If text.Length > 0 AndAlso cboCalendar.Items.Contains(text) Then
                cboCalendar.SelectedItem = text
            ElseIf cboCalendar.Items.Count > 0 Then
                cboCalendar.SelectedIndex = 0
                selectedCalendarFilter = cboCalendar.SelectedItem.ToString()
            End If
        End Sub

        Private Function EventCalendarLabel(ev As CalendarEvent) As String
            If ev Is Nothing Then
                Return "DEFAULT"
            End If
            Dim text As String = If(Not Equals(ev.CalendarName, Nothing), ev.CalendarName, "").Trim()
            If text.Length = 0 Then
                Dim text2 As String = CalDavCalendarSync.TryGetPrimaryCalendarFriendlyLabel()
                If text2.Length > 0 Then
                    Return text2
                End If
                Return "DEFAULT"
            End If
            Return CalDavCalendarSync.TryGetCalendarFriendlyLabel(text)
        End Function

        Private Shared Function EventCategoryLabel(ev As CalendarEvent) As String
            If ev Is Nothing Then
                Return "UNCATEGORIZED"
            End If
            Dim text As String = If(Not Equals(ev.Category, Nothing), ev.Category, "").Trim()
            If text.Length = 0 Then
                Return "UNCATEGORIZED"
            End If
            Return text.ToUpperInvariant()
        End Function

        Private Sub MaybeRebuildCategoryFilter(events As IList(Of CalendarEvent))
            Dim hashSet As HashSet(Of String) = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            hashSet.Add("ALL CATEGORIES")
            If events IsNot Nothing Then
                For Each [event] In events
                    hashSet.Add(EventCategoryLabel([event]))
                Next
            End If
            Dim a = String.Join("|", hashSet.OrderBy(Function(x) x))
            If Not String.Equals(a, lastCategoryFilterSignature, StringComparison.Ordinal) Then
                lastCategoryFilterSignature = a
                RebuildCategoryFilter(hashSet)
            End If
        End Sub

        Private Sub RebuildCategoryFilter(names As IEnumerable(Of String))
            If cboCategory Is Nothing Then
                Return
            End If
            Dim text = selectedCategoryFilter
            rebuildingCategoryFilter = True
            cboCategory.Items.Clear()
            For Each item In names.OrderBy(Function(x) x)
                cboCategory.Items.Add(item)
            Next
            rebuildingCategoryFilter = False
            If text.Length > 0 AndAlso cboCategory.Items.Contains(text) Then
                cboCategory.SelectedItem = text
            ElseIf cboCategory.Items.Count > 0 Then
                cboCategory.SelectedIndex = 0
                selectedCategoryFilter = cboCategory.SelectedItem.ToString()
            End If
        End Sub

        Private Sub OnToggleTasksView(sender As Object, e As EventArgs)
            showTasksOnly = Not showTasksOnly
            If showTasksOnly Then
                showWeekView = False
            End If
            RefreshEvents()
            lblStatus.Text = If(showTasksOnly, "TASK LIST VIEW", "MONTH VIEW")
        End Sub

        Private Sub OnToggleWeekView(sender As Object, e As EventArgs)
            showWeekView = Not showWeekView
            If showWeekView Then
                showTasksOnly = False
            End If
            RefreshEvents()
            lblStatus.Text = If(showWeekView, "WEEK VIEW (7 DAYS)", "MONTH VIEW")
        End Sub

        Private Sub OnToggleTaskDone(sender As Object, e As EventArgs)
            Dim num As Integer = SelectedMonthEventIndex()
            If num < 0 OrElse num >= monthEvents.Count Then
                lblStatus.Text = "SELECT A TASK"
                Return
            End If
            Dim calendarEvent = monthEvents(num)
            Dim calendarEvent2 = calendarEvent
            Dim text = If(Not Equals(calendarEvent2.Summary, Nothing), calendarEvent2.Summary, "")
            If text.IndexOf("[TASK]", StringComparison.OrdinalIgnoreCase) < 0 Then
                lblStatus.Text = "NOT A TASK"
                Return
            End If
            Dim flag = Not calendarEvent.TaskCompleted
            Dim summary = text
            If flag Then
                If Not text.StartsWith("[DONE]", StringComparison.OrdinalIgnoreCase) Then
                    summary = "[DONE] " & text
                End If
            ElseIf text.StartsWith("[DONE]", StringComparison.OrdinalIgnoreCase) Then
                summary = text.Substring(6).TrimStart()
            End If
            Dim list As List(Of CalendarEvent) = IcsCalendarStore.LoadEvents()
            For Each item In list
                If item Is Nothing OrElse Not String.Equals(item.Uid, calendarEvent.Uid, StringComparison.OrdinalIgnoreCase) Then
                    Continue For
                End If
                item.TaskCompleted = flag
                item.Summary = summary
                item.EwsDirty = True
                Exit For
            Next
            IcsCalendarStore.SaveEvents(list)
            Dim flag2 = False
            For Each item2 In list
                If item2 Is Nothing OrElse Not String.Equals(item2.Uid, calendarEvent.Uid, StringComparison.OrdinalIgnoreCase) Then
                    Continue For
                End If
                flag2 = CalDavTaskPush.TryPushTask(item2)
                Exit For
            Next
            RefreshEvents()
            If flag2 Then
                lblStatus.Text = If(flag, "TASK DONE — CALDAV PUSHED", "TASK REOPENED — CALDAV PUSHED")
            Else
                lblStatus.Text = If(flag, "TASK DONE (LOCAL)", "TASK REOPENED (LOCAL)")
            End If
        End Sub

        Private Sub OnImportIcs(sender As Object, e As EventArgs)
            Dim openFileDialog As OpenFileDialog = New OpenFileDialog()
            openFileDialog.Filter = "iCalendar (*.ics)|*.ics"
            If openFileDialog.ShowDialog(Me) = DialogResult.OK Then
                Dim num = IcsCalendarStore.ImportIcsFile(openFileDialog.FileName)
                lblStatus.Text = "IMPORTED " & num.ToString() & " EVENT(S)"
                RefreshEvents()
            End If
        End Sub

        Private Sub StartAlarmTimer()
            alarmTimer = New Timer()
            alarmTimer.Interval = 60000
            AddHandler alarmTimer.Tick, AddressOf OnAlarmTick
            alarmTimer.Start()
            OnAlarmTick(Nothing, EventArgs.Empty)
        End Sub

        Private Sub OnAlarmTick(sender As Object, e As EventArgs)
            Dim utcNow = Date.UtcNow
            For Each item2 In IcsCalendarStore.LoadEvents()
                If item2 Is Nothing OrElse item2.AlarmMinutesBefore <= 0 Then
                    Continue For
                End If
                Dim t = item2.StartUtc.AddMinutes(-item2.AlarmMinutesBefore)
                If Date.Compare(t, utcNow) <= 0 AndAlso Date.Compare(t, utcNow.AddMinutes(-2.0)) >= 0 Then
                    Dim item = item2.Uid & "|" & t.ToString("o")
                    If firedAlarmKeys.Add(item) Then
                        Dim calendarEvent = item2
                        Call MessageBox.Show(If(Not Equals(calendarEvent.Summary, Nothing), calendarEvent.Summary, "EVENT").ToUpperInvariant() & vbCrLf & item2.StartUtc.ToLocalTime().ToString("g"), "LCARS CALENDAR ALARM", MessageBoxButtons.OK, MessageBoxIcon.Asterisk)
                    End If
                End If
            Next
        End Sub

        Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
            If alarmTimer IsNot Nothing Then
                alarmTimer.Stop()
                alarmTimer.Dispose()
            End If
            If calDavLiveTimer IsNot Nothing Then
                calDavLiveTimer.Stop()
                calDavLiveTimer.Dispose()
            End If
            MyBase.OnFormClosed(e)
        End Sub

        Private Sub OnAddTask(sender As Object, e As EventArgs)
            Dim frmTaskEditor2 As frmTaskEditor = New frmTaskEditor()
            If frmTaskEditor2.ShowDialog(Me) = DialogResult.OK AndAlso frmTaskEditor2.TaskResult IsNot Nothing Then
                Dim taskResult = frmTaskEditor2.TaskResult
                MarkEventDirty(taskResult)
                Dim list As List(Of CalendarEvent) = IcsCalendarStore.LoadEvents()
                list.Add(taskResult)
                IcsCalendarStore.SaveEvents(list)
                Dim flag = CalDavTaskPush.TryPushTask(taskResult)
                RefreshEvents()
                lblStatus.Text = If(flag, "TASK ADDED — CALDAV PUSHED", "TASK ADDED (LOCAL)")
            End If
        End Sub

        Private Sub OnEditTask(ev As CalendarEvent)
            If ev Is Nothing Then
                Return
            End If
            Dim frmTaskEditor2 As frmTaskEditor = New frmTaskEditor(ev)
            If frmTaskEditor2.ShowDialog(Me) <> DialogResult.OK OrElse frmTaskEditor2.TaskResult Is Nothing Then
                Return
            End If
            Dim calendarEvent = frmTaskEditor2.TaskResult
            calendarEvent.Uid = ev.Uid
            Dim list As List(Of CalendarEvent) = IcsCalendarStore.LoadEvents()
            For Each item In list
                If item Is Nothing OrElse Not String.Equals(item.Uid, ev.Uid, StringComparison.OrdinalIgnoreCase) Then
                    Continue For
                End If
                item.Summary = calendarEvent.Summary
                item.StartUtc = calendarEvent.StartUtc
                item.EndUtc = calendarEvent.EndUtc
                item.AlarmMinutesBefore = calendarEvent.AlarmMinutesBefore
                item.TaskCompleted = calendarEvent.TaskCompleted
                item.TaskPriority = calendarEvent.TaskPriority
                item.TaskPercentComplete = calendarEvent.TaskPercentComplete
                item.Category = calendarEvent.Category
                item.RecurrenceRule = calendarEvent.RecurrenceRule
                item.CalendarName = calendarEvent.CalendarName
                item.CalDavDirty = True
                item.EwsDirty = True
                calendarEvent = item
                Exit For
            Next
            IcsCalendarStore.SaveEvents(list)
            Dim flag = CalDavTaskPush.TryPushTask(calendarEvent)
            RefreshEvents()
            lblStatus.Text = If(flag, "TASK UPDATED — CALDAV PUSHED", "TASK UPDATED (LOCAL)")
        End Sub

        Private Sub OnAddEvent(sender As Object, e As EventArgs)
            Dim text = InputBox("Event title:", "LCARS CALENDAR", "MEETING")
            If Not String.IsNullOrWhiteSpace(text) Then
                Dim text2 = InputBox("Category (optional):", "LCARS CALENDAR", "PERSONAL")
                Dim text3 = InputBox("Calendar name (optional):", "LCARS CALENDAR", If(Not Equals(text2, Nothing), text2, "PERSONAL"))
                Dim text4 = InputBox("Recurrence RRULE (optional, e.g. FREQ=WEEKLY):", "LCARS CALENDAR")
                Dim calendarEvent As CalendarEvent = New CalendarEvent()
                calendarEvent.Summary = text.Trim()
                calendarEvent.StartUtc = Date.UtcNow
                calendarEvent.EndUtc = Date.UtcNow.AddHours(1.0)
                calendarEvent.EwsDirty = True
                calendarEvent.CalDavDirty = True
                calendarEvent.Category = If(Not Equals(text2, Nothing), text2, "").Trim()
                calendarEvent.CalendarName = If(Not Equals(text3, Nothing), text3, "").Trim()
                calendarEvent.RecurrenceRule = If(Not Equals(text4, Nothing), text4, "").Trim()
                Dim calendarEvent2 = calendarEvent
                Dim list As List(Of CalendarEvent) = IcsCalendarStore.LoadEvents()
                list.Add(calendarEvent2)
                IcsCalendarStore.SaveEvents(list)
                Dim flag = CalDavCalendarSync.TryPushEvent(calendarEvent2)
                RefreshEvents()
                lblStatus.Text = If(flag, "EVENT ADDED — CALDAV PUSHED", "EVENT ADDED (LOCAL)")
            End If
        End Sub

        Private Sub OnEditEvent(sender As Object, e As EventArgs)
            Dim num As Integer = SelectedMonthEventIndex()
            If num < 0 OrElse num >= monthEvents.Count Then
                lblStatus.Text = "SELECT AN EVENT"
                Return
            End If
            Dim calendarEvent = monthEvents(num)
            If CalDavCalendarSync.IsTaskEvent(calendarEvent) Then
                OnEditTask(calendarEvent)
                Return
            End If
            Dim calendarEvent2 = calendarEvent
            Dim text = InputBox("Event title:", "LCARS CALENDAR", If(Not Equals(calendarEvent2.Summary, Nothing), calendarEvent2.Summary, ""))
            If String.IsNullOrWhiteSpace(text) Then
                Return
            End If
            Dim s As String = InputBox("Start (local):", "LCARS CALENDAR", calendarEvent.StartUtc.ToLocalTime().ToString("g"))
            Dim s2 As String = InputBox("End (local):", "LCARS CALENDAR", calendarEvent.EndUtc.ToLocalTime().ToString("g"))
            Dim text2 As String = InputBox("Alarm minutes before (0=none):", "LCARS CALENDAR", calendarEvent.AlarmMinutesBefore.ToString())
            Dim result = calendarEvent.AlarmMinutesBefore
            Call Integer.TryParse(If(Not Equals(text2, Nothing), text2, "").Trim(), result)
            Dim startUtc = calendarEvent.StartUtc
            Dim endUtc = calendarEvent.EndUtc
            Dim result2 As Date = Nothing

            If Date.TryParse(s, result2) Then
                startUtc = result2.ToUniversalTime()
            End If
            Dim result3 As Date = Nothing

            If Date.TryParse(s2, result3) Then
                endUtc = result3.ToUniversalTime()
            End If
            Dim list As List(Of CalendarEvent) = IcsCalendarStore.LoadEvents()
            For Each item In list
                If item Is Nothing OrElse Not String.Equals(item.Uid, calendarEvent.Uid, StringComparison.OrdinalIgnoreCase) Then
                    Continue For
                End If
                item.Summary = text.Trim()
                item.StartUtc = startUtc
                item.EndUtc = endUtc
                item.AlarmMinutesBefore = result
                item.EwsDirty = True
                calendarEvent = item
                Exit For
            Next
            IcsCalendarStore.SaveEvents(list)
            Dim flag = CalDavCalendarSync.TryPushEvent(calendarEvent)
            RefreshEvents()
            lblStatus.Text = If(flag, "EVENT UPDATED — CALDAV PUSHED", "EVENT UPDATED (LOCAL)")
        End Sub

        Private Sub OnDeleteEvent(sender As Object, e As EventArgs)
            Dim num As Integer = SelectedMonthEventIndex()
            If num < 0 OrElse num >= monthEvents.Count Then
                lblStatus.Text = "SELECT AN EVENT"
                Return
            End If
            Dim calendarEvent = monthEvents(num)
            If MessageBox.Show(Me, "Delete this event?", "LCARS CALENDAR", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) = DialogResult.Yes Then
                If Not String.IsNullOrWhiteSpace(calendarEvent.EwsItemId) Then
                    EwsCalendarDeleteQueue.Enqueue(calendarEvent.EwsItemId)
                End If
                Dim list As List(Of CalendarEvent) = IcsCalendarStore.LoadEvents()
                list.RemoveAll(Function(x) If(x IsNot Nothing AndAlso String.Equals(x.Uid, calendarEvent.Uid, StringComparison.OrdinalIgnoreCase), True, False))
                IcsCalendarStore.SaveEvents(list)
                Dim flag = CalDavCalendarSync.TryDeleteEvent(calendarEvent)
                RefreshEvents()
                lblStatus.Text = If(flag, "EVENT DELETED — CALDAV", "EVENT DELETED (LOCAL)")
            End If
        End Sub

        Private Sub OnCalDavSync(sender As Object, e As EventArgs)
            Dim num As Integer = CalDavCalendarSync.TryPullAndMerge()
            RefreshEvents()
            If num < 0 Then
                lblStatus.Text = "CALDAV PULL FAILED"
            Else
                lblStatus.Text = "CALDAV MERGED " & num.ToString() & " NEW EVENT(S)" & BuildCalDavStatusHint()
            End If
        End Sub
    End Class
