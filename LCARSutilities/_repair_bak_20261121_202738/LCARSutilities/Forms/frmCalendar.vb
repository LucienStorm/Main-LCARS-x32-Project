' LCARSutilities/LCARSutilities/Forms/frmCalendar.vb
Option Strict On
Option Explicit On
Imports System.Drawing
Imports System.Globalization
Imports System.Linq
Imports System.Windows.Forms
Imports LCARS.Controls
Public Class frmCalendar
Inherits LCARS.LCARSForm
Private Const FormW As Integer = 760
Private Const FormH As Integer = 560
Private lstEvents As ListBox
Private lstTasks As ListView
Private lblMonth As Label
Private lblStatus As Label
Private viewMonth As DateTime = DateTime.Today
Private alarmTimer As Timer
Private ReadOnly firedAlarmKeys As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
Private ReadOnly monthEvents As New List(Of CalendarEvent)()
Private showTasksOnly As Boolean
Private showWeekView As Boolean
Private showInvitesOnly As Boolean
Private showRecurringOnly As Boolean
Private showAlarmsOnly As Boolean
Private monthGrid As CalendarMonthGrid
Private cboCalendar As ComboBox
Private cboCategory As ComboBox
Private calDavLiveTimer As Timer
Private selectedCalendarFilter As String = ""
Private selectedCategoryFilter As String = ""
Private lastCalendarFilterSignature As String = ""
Private lastCategoryFilterSignature As String = ""
Private rebuildingCalendarFilter As Boolean
Private rebuildingCategoryFilter As Boolean
Public Sub New()
MyBase.New()
modUtilities.InitUtilityFormFloating(Me, FormW, FormH) BuildUi() RefreshEvents() StartAlarmTimer() StartCalDavLiveTimer()
End Sub
Private Sub BuildUi()
modUtilities.MakeUtilityTitle(Me, "CALENDAR", FormW - 420)
modUtilities.MakeUtilityButton(Me, "IMPORT ICS", FormW - modUtilities.UtilityMargin - 410, modUtilities.UtilityMargin, 120, 28, LCARS.LCARScolorStyles.PrimaryFunction, AddressOf OnImportIcs)
modUtilities.MakeUtilityButton(Me, "ADD", FormW - modUtilities.UtilityMargin - 280, modUtilities.UtilityMargin, 80, 28, LCARS.LCARScolorStyles.MiscFunction, AddressOf OnAddEvent)
modUtilities.MakeUtilityButton(Me, "ADD TASK", FormW - modUtilities.UtilityMargin - 620, modUtilities.UtilityMargin + 36, 100, 28, LCARS.LCARScolorStyles.CriticalFunction, AddressOf OnAddTask)
modUtilities.MakeUtilityButton(Me, "TASKS", FormW - modUtilities.UtilityMargin - 510, modUtilities.UtilityMargin + 36, 80, 28, LCARS.LCARScolorStyles.NavigationFunction, AddressOf OnToggleTasksView)
modUtilities.MakeUtilityButton(Me, "WEEK", FormW - modUtilities.UtilityMargin - 420, modUtilities.UtilityMargin + 36, 72, 28, LCARS.LCARScolorStyles.NavigationFunction, AddressOf OnToggleWeekView)
modUtilities.MakeUtilityButton(Me, "INVITES", FormW - modUtilities.UtilityMargin - 620, modUtilities.UtilityMargin + 72, 88, 28, LCARS.LCARScolorStyles.CriticalFunction, AddressOf OnToggleInvitesView)
modUtilities.MakeUtilityButton(Me, "RECUR", FormW - modUtilities.UtilityMargin - 520, modUtilities.UtilityMargin + 72, 72, 28, LCARS.LCARScolorStyles.NavigationFunction, AddressOf OnToggleRecurringView)
modUtilities.MakeUtilityButton(Me, "ALARM", FormW - modUtilities.UtilityMargin - 440, modUtilities.UtilityMargin + 72, 72, 28, LCARS.LCARScolorStyles.SystemFunction, AddressOf OnToggleAlarmsView)
modUtilities.MakeUtilityButton(Me, "EDIT", FormW - modUtilities.UtilityMargin - 530, modUtilities.UtilityMargin + 36, 80, 28, LCARS.LCARScolorStyles.PrimaryFunction, AddressOf OnEditEvent)
modUtilities.MakeUtilityButton(Me, "DELETE", FormW - modUtilities.UtilityMargin - 440, modUtilities.UtilityMargin + 36, 90, 28, LCARS.LCARScolorStyles.Orange, AddressOf OnDeleteEvent)
modUtilities.MakeUtilityButton(Me, "CALDAV", FormW - modUtilities.UtilityMargin - 340, modUtilities.UtilityMargin + 36, 90, 28, LCARS.LCARScolorStyles.NavigationFunction, AddressOf OnCalDavSync)
modUtilities.MakeUtilityButton(Me, "ACCEPT", FormW - modUtilities.UtilityMargin - 240, modUtilities.UtilityMargin + 36, 72, 28, LCARS.LCARScolorStyles.MiscFunction, Sub(s, e) ApplyMeetingRsvp("ACCEPTED"))
modUtilities.MakeUtilityButton(Me, "TENT", FormW - modUtilities.UtilityMargin - 160, modUtilities.UtilityMargin + 36, 72, 28, LCARS.LCARScolorStyles.SystemFunction, Sub(s, e) ApplyMeetingRsvp("TENTATIVE"))
modUtilities.MakeUtilityButton(Me, "DECLINE", FormW - modUtilities.UtilityMargin - 80, modUtilities.UtilityMargin + 36, 72, 28, LCARS.LCARScolorStyles.Orange, Sub(s, e) ApplyMeetingRsvp("DECLINED"))
modUtilities.MakeUtilityButton(Me, "DETACH", FormW - modUtilities.UtilityMargin - 620, modUtilities.UtilityMargin + 108, 88, 28, LCARS.LCARScolorStyles.SystemFunction, AddressOf OnDetachRecurrenceInstance)
modUtilities.MakeUtilityButton(Me, "DONE", FormW - modUtilities.UtilityMargin - 500, modUtilities.UtilityMargin, 80, 28, LCARS.LCARScolorStyles.SystemFunction, AddressOf OnToggleTaskDone)
modUtilities.MakeUtilityButton(Me, "PREV", FormW - modUtilities.UtilityMargin - 190, modUtilities.UtilityMargin, 80, 28, LCARS.LCARScolorStyles.NavigationFunction, Sub(s, e) ShiftMonth(-1))
modUtilities.MakeUtilityButton(Me, "NEXT", FormW - modUtilities.UtilityMargin - 100, modUtilities.UtilityMargin, 80, 28, LCARS.LCARScolorStyles.NavigationFunction, Sub(s, e) ShiftMonth(1))
modUtilities.MakeUtilityButton(Me, "CLOSE", FormW - modUtilities.UtilityMargin - 120, modUtilities.UtilityMargin + 36, 110, 28, LCARS.LCARScolorStyles.Orange, Sub(s, e) Close()) lblMonth = modUtilities.MakeUtilityLabel(Me, modUtilities.UtilityMargin, 48, FormW - 280, 24, 16.0F) cboCalendar = New ComboBox()
cboCalendar.DropDownStyle = ComboBoxStyle.DropDownList cboCalendar.Location = New Point(FormW - modUtilities.UtilityMargin - 260, 48)
cboCalendar.Size = New Size(250, 24)
cboCalendar.BackColor = Color.FromArgb(40, 40, 40)
cboCalendar.ForeColor = Color.Orange
AddHandler cboCalendar.SelectedIndexChanged, AddressOf OnCalendarFilterChanged Controls.Add(cboCalendar) cboCategory = New ComboBox()
cboCategory.DropDownStyle = ComboBoxStyle.DropDownList cboCategory.Location = New Point(FormW - modUtilities.UtilityMargin - 260, 72)
cboCategory.Size = New Size(250, 24)
cboCategory.BackColor = Color.FromArgb(40, 40, 40)
cboCategory.ForeColor = Color.Orange
AddHandler cboCategory.SelectedIndexChanged, AddressOf OnCategoryFilterChanged Controls.Add(cboCategory) monthGrid = New CalendarMonthGrid()
monthGrid.Location = New Point(modUtilities.UtilityMargin, 76)
monthGrid.Size = New Size(200, 160)
Controls.Add(monthGrid) lstEvents = New ListBox()
lstEvents.Location = New Point(modUtilities.UtilityMargin + 210, 76)
lstEvents.Size = New Size(FormW - modUtilities.UtilityMargin * 2 - 210, FormH - 126)
lstEvents.BackColor = Color.FromArgb(30, 30, 30)
lstEvents.ForeColor = Color.Orange lstEvents.Font = New Font("Arial Narrow", 11.0F)
AddHandler lstEvents.DoubleClick, AddressOf OnEditEvent Controls.Add(lstEvents) lstTasks = New ListView()
lstTasks.View = View.Details lstTasks.FullRowSelect = True lstTasks.HeaderStyle = ColumnHeaderStyle.Nonclickable lstTasks.BackColor = Color.FromArgb(30, 30, 30)
lstTasks.ForeColor = Color.Orange lstTasks.Font = New Font("Arial Narrow", 11.0F)
lstTasks.Location = lstEvents.Location lstTasks.Size = lstEvents.Size lstTasks.Columns.Add("DUE", 130)
lstTasks.Columns.Add("PRI", 36)
lstTasks.Columns.Add("%", 36)
lstTasks.Columns.Add("SUMMARY", 480)
lstTasks.Visible = False
AddHandler lstTasks.DoubleClick, AddressOf OnEditEvent Controls.Add(lstTasks) lblStatus = modUtilities.MakeUtilityLabel(Me, modUtilities.UtilityMargin, FormH - 40, FormW - 24, 20, 10.0F)
End Sub
Private Function SelectedMonthEventIndex() As Integer
If showTasksOnly AndAlso lstTasks IsNot Nothing AndAlso lstTasks.Visible Then If lstTasks.SelectedIndices.Count = 0 Then Return -1
Return lstTasks.SelectedIndices(0)
End If
If lstEvents Is Nothing Then Return -1
Return lstEvents.SelectedIndex
End Function
Private Sub ShiftMonth(ByVal delta As Integer) viewMonth = viewMonth.AddMonths(delta) RefreshEvents()
End Sub
Private Sub RefreshEvents()
Dim monthText As String = viewMonth.ToString("MMMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant()
Dim primaryCal As String = CalDavCalendarSync.TryGetPrimaryCalendarFriendlyLabel()
If primaryCal.Length > 0 AndAlso Not showTasksOnly Then monthText &= " · " & primaryCal lblMonth.Text = monthText lstEvents.Items.Clear()
If lstTasks IsNot Nothing Then lstTasks.Items.Clear()
monthEvents.Clear()
Dim events As List(Of CalendarEvent) = IcsCalendarStore.LoadEvents() MaybeRebuildCalendarFilter(events) MaybeRebuildCategoryFilter(events)
Dim calFilter As String = If(selectedCalendarFilter, "").Trim()
Dim filterCal As Boolean = calFilter.Length > 0 AndAlso Not calFilter.Equals("ALL CALENDARS", StringComparison.OrdinalIgnoreCase)
Dim catFilter As String = If(selectedCategoryFilter, "").Trim()
Dim filterCat As Boolean = catFilter.Length > 0 AndAlso Not catFilter.Equals("ALL CATEGORIES", StringComparison.OrdinalIgnoreCase)
Dim monthStart As New DateTime(viewMonth.Year, viewMonth.Month, 1, 0, 0, 0, DateTimeKind.Utc)
Dim monthEnd As DateTime = monthStart.AddMonths(1)
Dim shown As Integer = 0
Dim weekStart As DateTime = DateTime.UtcNow.Date
Dim weekEnd As DateTime = weekStart.AddDays(7)
Dim allCalendarLabels As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
For Each scan As CalendarEvent In events
If scan Is Nothing Then Continue For allCalendarLabels.Add(EventCalendarLabel(scan))
Next
Dim showCalPrefixInList As Boolean = Not filterCal AndAlso allCalendarLabels.Count > 1
For Each ev As CalendarEvent In events.OrderBy(Function(x)
x.StartUtc)
If filterCal AndAlso Not String.Equals(EventCalendarLabel(ev), calFilter, StringComparison.OrdinalIgnoreCase) Then Continue For
If filterCat AndAlso Not String.Equals(EventCategoryLabel(ev), catFilter, StringComparison.OrdinalIgnoreCase) Then Continue For
If showInvitesOnly Then If Not ev.IsMeetingInvitation Then Continue For
ElseIf showRecurringOnly Then If String.IsNullOrWhiteSpace(ev.RecurrenceRule) Then Continue For
ElseIf showAlarmsOnly Then If ev.AlarmMinutesBefore <= 0 Then Continue For
ElseIf showTasksOnly Then If Not CalDavCalendarSync.IsTaskEvent(ev) Then Continue For
ElseIf showWeekView Then If ev.StartUtc < weekStart OrElse ev.StartUtc >= weekEnd Then Continue For
ElseIf ev.StartUtc < monthStart OrElse ev.StartUtc >= monthEnd Then Continue For
End If monthEvents.Add(ev)
Dim summary As String = If(ev.Summary, "")
If ev.TaskCompleted AndAlso Not summary.StartsWith("[DONE]", StringComparison.OrdinalIgnoreCase) Then summary = "[DONE] " & summary
End If
If Not String.IsNullOrWhiteSpace(ev.Category) Then summary = "[" & ev.Category.Trim().ToUpperInvariant() & "] " & summary
End If
If Not String.IsNullOrWhiteSpace(ev.RecurrenceRule) Then summary &= " ⟳ " & ev.RecurrenceRule.Trim()
End If
If ev.IsMeetingInvitation Then Dim ps As String = If(ev.MyPartStat, "NEEDS-ACTION").Trim().ToUpperInvariant() summary = "✉ " & ps & " " & summary
End If
If showTasksOnly AndAlso lstTasks IsNot Nothing Then Dim dueText As String = ev.StartUtc.ToLocalTime().ToString("ddd MMM d HH:mm")
Dim item As New ListViewItem(dueText)
item.SubItems.Add(ev.TaskPriority.ToString())
item.SubItems.Add(ev.TaskPercentComplete.ToString())
If ev.AlarmMinutesBefore > 0 Then summary = "⏰-" & ev.AlarmMinutesBefore.ToString() & "m " & summary item.SubItems.Add(summary)
lstTasks.Items.Add(item)
Else Dim alarmPrefix As String = ""
If ev.AlarmMinutesBefore > 0 Then alarmPrefix = "⏰-" & ev.AlarmMinutesBefore.ToString() & "m "
Dim calPrefix As String = ""
If showCalPrefixInList Then calPrefix = "[" & EventCalendarLabel(ev) & "] "
End If lstEvents.Items.Add(ev.StartUtc.ToLocalTime().ToString("ddd MMM d HH:mm") & " " & alarmPrefix & calPrefix & summary)
End If shown += 1
Next
If lstTasks IsNot Nothing Then lstTasks.Visible = showTasksOnly
End If
If lstEvents IsNot Nothing Then lstEvents.Visible = Not showTasksOnly
End If
Dim label As String = " EVENT(S) THIS MONTH"
If showInvitesOnly Then label = " MEETING INVITE(S) — LIGHTNING VIEW"
If showRecurringOnly Then label = " RECURRING SERIES — LIGHTNING VIEW"
If showAlarmsOnly Then label = " REMINDER(S) — LIGHTNING VIEW"
If showTasksOnly Then label = " TASK(S) — LIGHTNING GRID"
If showWeekView Then label = " EVENT(S) THIS WEEK"
Dim calNames As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
For Each ev As CalendarEvent In monthEvents
If ev Is Nothing Then Continue For calNames.Add(EventCalendarLabel(ev))
Next
Dim multiCalHint As String = If(calNames.Count > 1, " · " & calNames.Count.ToString() & " CALS", "")
Dim labelCount As Integer = CalDavCalendarSync.CountKnownCalendarUrlLabels()
If labelCount > 0 Then multiCalHint &= " · DAV-LABELS " & labelCount.ToString()
If filterCat AndAlso catFilter.Length > 0 Then multiCalHint &= " · CAT " & catFilter
If filterCal AndAlso calFilter.Length > 0 AndAlso Not calFilter.Equals("ALL CALENDARS", StringComparison.OrdinalIgnoreCase) Then multiCalHint &= " · CAL " & calFilter
End If lblStatus.Text = shown.ToString() & label & multiCalHint
If monthGrid IsNot Nothing Then monthGrid.SetMonth(viewMonth, IcsCalendarStore.LoadEvents())
End Sub
Private Shared Sub MarkEventDirty(ByVal ev As CalendarEvent)
If ev Is Nothing Then Return ev.EwsDirty = True ev.CalDavDirty = True
End Sub
Private Sub OnDetachRecurrenceInstance(ByVal sender As Object, ByVal e As EventArgs)
Dim idx As Integer = SelectedMonthEventIndex()
If idx < 0 OrElse idx >= monthEvents.Count Then lblStatus.Text = "SELECT A RECURRING EVENT"
Return
End If
Dim selected As CalendarEvent = monthEvents(idx)
If selected Is Nothing OrElse String.IsNullOrWhiteSpace(selected.RecurrenceRule) Then lblStatus.Text = "NOT A RECURRING SERIES"
Return
End If
Dim ex As CalendarEvent = CalendarRecurrence.TryCreateRecurrenceException(selected, selected.StartUtc)
If ex Is Nothing Then lblStatus.Text = "DETACH FAILED"
Return
End If
Dim all As List(Of CalendarEvent) = IcsCalendarStore.LoadEvents()
CalendarRecurrence.TryAppendExDateOnSeriesMaster(all, selected, selected.StartUtc)
CalendarRecurrence.TryDecrementRecurrenceCountOnSeries(selected)
CalendarRecurrence.TryClampRecurrenceUntilBeforeInstance(selected, selected.StartUtc)
all.Add(ex)
IcsCalendarStore.SaveEvents(all)
CalDavCalendarSync.TryPushEvent(selected)
CalDavCalendarSync.TryPushEvent(ex) RefreshEvents()
lblStatus.Text = "RECURRENCE EXCEPTION + EXDATE ON SERIES (SLICE 15 PARTIAL)"
End Sub
Private Sub ApplyMeetingRsvp(ByVal partStat As String)
Dim idx As Integer = SelectedMonthEventIndex()
If idx < 0 OrElse idx >= monthEvents.Count Then lblStatus.Text = "SELECT A MEETING INVITE"
Return
End If
Dim selected As CalendarEvent = monthEvents(idx)
If selected Is Nothing OrElse String.IsNullOrWhiteSpace(selected.Uid) Then Return
Dim stat As String = If(partStat, "").Trim().ToUpperInvariant()
If stat.Length = 0 Then Return
Dim all As List(Of CalendarEvent) = IcsCalendarStore.LoadEvents()
Dim updated As Boolean = False
For Each ev As CalendarEvent In all
If ev Is Nothing Then Continue For
If Not String.Equals(ev.Uid, selected.Uid, StringComparison.OrdinalIgnoreCase) Then Continue For ev.MyPartStat = stat ev.IsMeetingInvitation = True
If String.IsNullOrWhiteSpace(ev.AttendeeMailto) Then ev.AttendeeMailto = MailAttendeeHint.TryGetPrimaryAttendeeMailto()
End If MarkEventDirty(ev) updated = True
Exit For
Next
If Not updated Then lblStatus.Text = "RSVP TARGET NOT FOUND"
Return
End If IcsCalendarStore.SaveEvents(all)
Dim pushEv As CalendarEvent = Nothing
For Each ev As CalendarEvent In all
If ev IsNot Nothing AndAlso String.Equals(ev.Uid, selected.Uid, StringComparison.OrdinalIgnoreCase) Then pushEv = ev
Exit For
End If
Next
Dim pushed As Boolean = False
If pushEv IsNot Nothing Then pushEv.CalDavDirty = True pushed = CalDavCalendarSync.TryPushEvent(pushEv)
If pushed Then IcsCalendarStore.SaveEvents(all)
End If
If pushEv IsNot Nothing Then EwsCalendarRsvpQueue.Enqueue(pushEv.Uid, stat)
CalendarMeetingReplyQueue.Enqueue( pushEv.Uid, stat, pushEv.Organizer, pushEv.Summary, pushEv.AttendeeMailto)
End If
Dim mailLaunched As Boolean = False
If pushEv IsNot Nothing Then mailLaunched = MailAttendeeHint.TryLaunchOrganizerRsvpMail( pushEv.Organizer, pushEv.Summary, stat)
End If RefreshEvents()
Dim mailHint As String = If(mailLaunched, " · ORGANIZER MAILTO", " · MAIL RSVP PENDING")
Dim ewsHint As String = " · EWS+IMIP QUEUED" lblStatus.Text = "RSVP " & stat & If(pushed, " — CALDAV ATTENDEE PUSHED", " — CALDAV PUSH PENDING") & mailHint & ewsHint
End Sub
Private Sub OnToggleInvitesView(ByVal sender As Object, ByVal e As EventArgs) showInvitesOnly = Not showInvitesOnly
If showInvitesOnly Then showTasksOnly = False showWeekView = False showRecurringOnly = False showAlarmsOnly = False
End If RefreshEvents()
End Sub
Private Sub OnToggleRecurringView(ByVal sender As Object, ByVal e As EventArgs) showRecurringOnly = Not showRecurringOnly
If showRecurringOnly Then showTasksOnly = False showWeekView = False showInvitesOnly = False showAlarmsOnly = False
End If RefreshEvents()
lblStatus.Text = If(showRecurringOnly, "RECURRING SERIES VIEW", "MONTH VIEW")
End Sub
Private Sub OnToggleAlarmsView(ByVal sender As Object, ByVal e As EventArgs) showAlarmsOnly = Not showAlarmsOnly
If showAlarmsOnly Then showTasksOnly = False showWeekView = False showInvitesOnly = False showRecurringOnly = False
End If RefreshEvents()
lblStatus.Text = If(showAlarmsOnly, "REMINDER EVENTS VIEW", "MONTH VIEW")
End Sub
Private Sub StartCalDavLiveTimer() calDavLiveTimer = New Timer()
calDavLiveTimer.Interval = 4 * 1000
AddHandler calDavLiveTimer.Tick, AddressOf OnCalDavLiveTick calDavLiveTimer.Start()
End Sub
Private Sub OnCalDavLiveTick(ByVal sender As Object, ByVal e As EventArgs)
Dim n As Integer = CalDavCalendarSync.TrySyncBidirectional()
If lblStatus IsNot Nothing Then Dim calHint As String = BuildCalDavStatusHint()
If n < 0 Then lblStatus.Text = "CALDAV LIVE SYNC FAILED" & calHint
ElseIf n > 0 Then Dim pushHint As String = ""
If CalDavCalendarSync.LastBidirectionalPushCount > 0 Then pushHint = " · PUSH " & CalDavCalendarSync.LastBidirectionalPushCount.ToString()
End If lblStatus.Text = "CALDAV LIVE " & n.ToString() & " ITEM(S) SYNCED" & pushHint & calHint
End If
End If
If n <> 0 Then RefreshEvents()
End Sub
Private Shared Function BuildCalDavStatusHint() As String
Dim hint As String = ""
Dim primary As String = CalDavCalendarSync.TryGetPrimaryCalendarFriendlyLabel()
If primary.Length > 0 Then hint &= " · " & primary
Dim labels As Integer = CalDavCalendarSync.CountKnownCalendarUrlLabels()
If labels > 0 Then hint &= " · LABELS " & labels.ToString()
If CalDavCalendarSync.LastPrimaryEventsTagged > 0 Then hint &= " · TAG " & CalDavCalendarSync.LastPrimaryEventsTagged.ToString()
End If
If CalDavCalendarSync.LastSecondaryEventsTagged > 0 Then hint &= " · 2ND-TAG " & CalDavCalendarSync.LastSecondaryEventsTagged.ToString()
End If
If CalDavCalendarSync.LastTaskEventsTagged > 0 Then hint &= " · TASK-TAG " & CalDavCalendarSync.LastTaskEventsTagged.ToString()
End If
If CalDavCalendarSync.LastPullMergeAdded > 0 Then hint &= " · PULL " & CalDavCalendarSync.LastPullMergeAdded.ToString()
End If
If CalDavCalendarSync.LastSecondaryPullMergeAdded > 0 Then hint &= " · 2ND-PULL " & CalDavCalendarSync.LastSecondaryPullMergeAdded.ToString()
End If
If CalDavCalendarSync.LastUrlLabelsAtPull > 0 Then hint &= " · PROP " & CalDavCalendarSync.LastUrlLabelsAtPull.ToString()
End If
If CalDavCalendarSync.LastPrimaryPullMergeAdded > 0 Then hint &= " · 1ST-PULL " & CalDavCalendarSync.LastPrimaryPullMergeAdded.ToString()
End If
If CalDavCalendarSync.LastTaskPullMergeAdded > 0 Then hint &= " · TASK-PULL " & CalDavCalendarSync.LastTaskPullMergeAdded.ToString()
End If
Return hint
End Function
Private Sub OnCalendarFilterChanged(ByVal sender As Object, ByVal e As EventArgs)
If rebuildingCalendarFilter Then Return
If cboCalendar Is Nothing OrElse cboCalendar.SelectedItem Is Nothing Then Return selectedCalendarFilter = cboCalendar.SelectedItem.ToString().Trim() RefreshEvents()
End Sub
Private Sub OnCategoryFilterChanged(ByVal sender As Object, ByVal e As EventArgs)
If rebuildingCategoryFilter Then Return
If cboCategory Is Nothing OrElse cboCategory.SelectedItem Is Nothing Then Return selectedCategoryFilter = cboCategory.SelectedItem.ToString().Trim() RefreshEvents()
End Sub
Private Sub MaybeRebuildCalendarFilter(ByVal events As IList(Of CalendarEvent))
Dim names As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
names.Add("ALL CALENDARS")
If events IsNot Nothing Then For Each ev As CalendarEvent In events names.Add(EventCalendarLabel(ev))
Next
End If
Dim sig As String = String.Join("|", names.OrderBy(Function(x) x))
If String.Equals(sig, lastCalendarFilterSignature, StringComparison.Ordinal) Then Return lastCalendarFilterSignature = sig RebuildCalendarFilter(names)
End Sub
Private Sub RebuildCalendarFilter(ByVal names As IEnumerable(Of String))
If cboCalendar Is Nothing Then Return
Dim prior As String = selectedCalendarFilter rebuildingCalendarFilter = True cboCalendar.Items.Clear()
For Each n As String In names.OrderBy(Function(x) x)
cboCalendar.Items.Add(n)
Next rebuildingCalendarFilter = False
If prior.Length > 0 AndAlso cboCalendar.Items.Contains(prior) Then cboCalendar.SelectedItem = prior
ElseIf cboCalendar.Items.Count > 0 Then cboCalendar.SelectedIndex = 0 selectedCalendarFilter = cboCalendar.SelectedItem.ToString()
End If
End Sub
Private Function EventCalendarLabel(ByVal ev As CalendarEvent) As String
If ev Is Nothing Then Return "DEFAULT"
Dim name As String = If(ev.CalendarName, "").Trim()
If name.Length = 0 Then Dim primary As String = CalDavCalendarSync.TryGetPrimaryCalendarFriendlyLabel()
If primary.Length > 0 Then Return primary
Return "DEFAULT"
End If
Return CalDavCalendarSync.TryGetCalendarFriendlyLabel(name)
End Function
Private Shared Function EventCategoryLabel(ByVal ev As CalendarEvent) As String
If ev Is Nothing Then Return "UNCATEGORIZED"
Dim cat As String = If(ev.Category, "").Trim()
If cat.Length = 0 Then Return "UNCATEGORIZED"
Return cat.ToUpperInvariant()
End Function
Private Sub MaybeRebuildCategoryFilter(ByVal events As IList(Of CalendarEvent))
Dim names As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
names.Add("ALL CATEGORIES")
If events IsNot Nothing Then For Each ev As CalendarEvent In events names.Add(EventCategoryLabel(ev))
Next
End If
Dim sig As String = String.Join("|", names.OrderBy(Function(x) x))
If String.Equals(sig, lastCategoryFilterSignature, StringComparison.Ordinal) Then Return lastCategoryFilterSignature = sig RebuildCategoryFilter(names)
End Sub
Private Sub RebuildCategoryFilter(ByVal names As IEnumerable(Of String))
If cboCategory Is Nothing Then Return
Dim prior As String = selectedCategoryFilter rebuildingCategoryFilter = True cboCategory.Items.Clear()
For Each n As String In names.OrderBy(Function(x) x)
cboCategory.Items.Add(n)
Next rebuildingCategoryFilter = False
If prior.Length > 0 AndAlso cboCategory.Items.Contains(prior) Then cboCategory.SelectedItem = prior
ElseIf cboCategory.Items.Count > 0 Then cboCategory.SelectedIndex = 0 selectedCategoryFilter = cboCategory.SelectedItem.ToString()
End If
End Sub
Private Sub OnToggleTasksView(ByVal sender As Object, ByVal e As EventArgs) showTasksOnly = Not showTasksOnly
If showTasksOnly Then showWeekView = False RefreshEvents()
lblStatus.Text = If(showTasksOnly, "TASK LIST VIEW", "MONTH VIEW")
End Sub
Private Sub OnToggleWeekView(ByVal sender As Object, ByVal e As EventArgs) showWeekView = Not showWeekView
If showWeekView Then showTasksOnly = False RefreshEvents()
lblStatus.Text = If(showWeekView, "WEEK VIEW (7 DAYS)", "MONTH VIEW")
End Sub
Private Sub OnToggleTaskDone(ByVal sender As Object, ByVal e As EventArgs)
Dim idx As Integer = SelectedMonthEventIndex()
If idx < 0 OrElse idx >= monthEvents.Count Then lblStatus.Text = "SELECT A TASK"
Return
End If
Dim ev As CalendarEvent = monthEvents(idx)
Dim summary As String = If(ev.Summary, "")
If summary.IndexOf("[TASK]", StringComparison.OrdinalIgnoreCase) < 0 Then lblStatus.Text = "NOT A TASK"
Return
End If
Dim newDone As Boolean = Not ev.TaskCompleted
Dim newSummary As String = summary
If newDone Then If Not summary.StartsWith("[DONE]", StringComparison.OrdinalIgnoreCase) Then newSummary = "[DONE] " & summary
End If
ElseIf summary.StartsWith("[DONE]", StringComparison.OrdinalIgnoreCase) Then newSummary = summary.Substring(6).TrimStart()
End If
Dim all As List(Of CalendarEvent) = IcsCalendarStore.LoadEvents()
For Each item As CalendarEvent In all
If item Is Nothing Then Continue For
If Not String.Equals(item.Uid, ev.Uid, StringComparison.OrdinalIgnoreCase) Then Continue For item.TaskCompleted = newDone item.Summary = newSummary item.EwsDirty = True
Exit For
Next IcsCalendarStore.SaveEvents(all)
Dim pushed As Boolean = False
For Each item As CalendarEvent In all
If item Is Nothing Then Continue For
If Not String.Equals(item.Uid, ev.Uid, StringComparison.OrdinalIgnoreCase) Then Continue For pushed = CalDavTaskPush.TryPushTask(item)
Exit For
Next RefreshEvents()
If pushed Then lblStatus.Text = If(newDone, "TASK DONE — CALDAV PUSHED", "TASK REOPENED — CALDAV PUSHED")
Else lblStatus.Text = If(newDone, "TASK DONE (LOCAL)", "TASK REOPENED (LOCAL)")
End If
End Sub
Private Sub OnImportIcs(ByVal sender As Object, ByVal e As EventArgs)
Using dlg As New OpenFileDialog()
dlg.Filter = "iCalendar (*.ics)|*.ics"
If dlg.ShowDialog(Me) <> DialogResult.OK Then Return
Dim n As Integer = IcsCalendarStore.ImportIcsFile(dlg.FileName)
lblStatus.Text = "IMPORTED " & n.ToString() & " EVENT(S)" RefreshEvents()
End Using
End Sub
Private Sub StartAlarmTimer() alarmTimer = New Timer()
alarmTimer.Interval = 60000
AddHandler alarmTimer.Tick, AddressOf OnAlarmTick alarmTimer.Start() OnAlarmTick(Nothing, EventArgs.Empty)
End Sub
Private Sub OnAlarmTick(ByVal sender As Object, ByVal e As EventArgs)
Dim nowUtc As DateTime = DateTime.UtcNow
For Each ev As CalendarEvent In IcsCalendarStore.LoadEvents()
If ev Is Nothing OrElse ev.AlarmMinutesBefore <= 0 Then Continue For
Dim fireUtc As DateTime = ev.StartUtc.AddMinutes(-ev.AlarmMinutesBefore)
If fireUtc > nowUtc OrElse fireUtc < nowUtc.AddMinutes(-2) Then Continue For
Dim key As String = ev.Uid & "|" & fireUtc.ToString("o")
If Not firedAlarmKeys.Add(key) Then Continue For MessageBox.Show( If(ev.Summary, "EVENT").ToUpperInvariant() & vbCrLf & ev.StartUtc.ToLocalTime().ToString("g"), "LCARS CALENDAR ALARM", MessageBoxButtons.OK, MessageBoxIcon.Information)
Next
End Sub
Protected Overrides Sub OnFormClosed(ByVal e As FormClosedEventArgs)
If alarmTimer IsNot Nothing Then alarmTimer.Stop()
alarmTimer.Dispose()
End If
If calDavLiveTimer IsNot Nothing Then calDavLiveTimer.Stop()
calDavLiveTimer.Dispose()
End If MyBase.OnFormClosed(e)
End Sub
Private Sub OnAddTask(ByVal sender As Object, ByVal e As EventArgs)
Using editor As New frmTaskEditor()
If editor.ShowDialog(Me) <> DialogResult.OK OrElse editor.TaskResult Is Nothing Then Return
Dim ev As CalendarEvent = editor.TaskResult MarkEventDirty(ev)
Dim events As List(Of CalendarEvent) = IcsCalendarStore.LoadEvents()
events.Add(ev)
IcsCalendarStore.SaveEvents(events)
Dim pushed As Boolean = CalDavTaskPush.TryPushTask(ev) RefreshEvents()
lblStatus.Text = If(pushed, "TASK ADDED — CALDAV PUSHED", "TASK ADDED (LOCAL)")
End Using
End Sub
Private Sub OnEditTask(ByVal ev As CalendarEvent)
If ev Is Nothing Then Return
Using editor As New frmTaskEditor(ev)
If editor.ShowDialog(Me) <> DialogResult.OK OrElse editor.TaskResult Is Nothing Then Return
Dim edited As CalendarEvent = editor.TaskResult edited.Uid = ev.Uid
Dim all As List(Of CalendarEvent) = IcsCalendarStore.LoadEvents()
For Each item As CalendarEvent In all
If item Is Nothing Then Continue For
If Not String.Equals(item.Uid, ev.Uid, StringComparison.OrdinalIgnoreCase) Then Continue For item.Summary = edited.Summary item.StartUtc = edited.StartUtc item.EndUtc = edited.EndUtc item.AlarmMinutesBefore = edited.AlarmMinutesBefore item.TaskCompleted = edited.TaskCompleted item.TaskPriority = edited.TaskPriority item.TaskPercentComplete = edited.TaskPercentComplete item.Category = edited.Category item.RecurrenceRule = edited.RecurrenceRule item.CalendarName = edited.CalendarName item.CalDavDirty = True item.EwsDirty = True edited = item
Exit For
Next IcsCalendarStore.SaveEvents(all)
Dim pushed As Boolean = CalDavTaskPush.TryPushTask(edited) RefreshEvents()
lblStatus.Text = If(pushed, "TASK UPDATED — CALDAV PUSHED", "TASK UPDATED (LOCAL)")
End Using
End Sub
Private Sub OnAddEvent(ByVal sender As Object, ByVal e As EventArgs)
Dim summary As String = InputBox("Event title:", "LCARS CALENDAR", "MEETING")
If String.IsNullOrWhiteSpace(summary) Then Return
Dim category As String = InputBox("Category (optional):", "LCARS CALENDAR", "PERSONAL")
Dim calName As String = InputBox("Calendar name (optional):", "LCARS CALENDAR", If(category, "PERSONAL"))
Dim recur As String = InputBox("Recurrence RRULE (optional, e.g. FREQ=WEEKLY):", "LCARS CALENDAR", "")
Dim ev As New CalendarEvent() With { .Summary = summary.Trim(), .StartUtc = DateTime.UtcNow, .EndUtc = DateTime.UtcNow.AddHours(1), .EwsDirty = True, .CalDavDirty = True, .Category = If(category, "").Trim(), .CalendarName = If(calName, "").Trim(), .RecurrenceRule = If(recur, "").Trim() }
Dim events As List(Of CalendarEvent) = IcsCalendarStore.LoadEvents()
events.Add(ev)
IcsCalendarStore.SaveEvents(events)
Dim pushed As Boolean = CalDavCalendarSync.TryPushEvent(ev) RefreshEvents()
lblStatus.Text = If(pushed, "EVENT ADDED — CALDAV PUSHED", "EVENT ADDED (LOCAL)")
End Sub
Private Sub OnEditEvent(ByVal sender As Object, ByVal e As EventArgs)
Dim idx As Integer = SelectedMonthEventIndex()
If idx < 0 OrElse idx >= monthEvents.Count Then lblStatus.Text = "SELECT AN EVENT"
Return
End If
Dim ev As CalendarEvent = monthEvents(idx)
If CalDavCalendarSync.IsTaskEvent(ev) Then OnEditTask(ev)
Return
End If
Dim summary As String = InputBox("Event title:", "LCARS CALENDAR", If(ev.Summary, ""))
If String.IsNullOrWhiteSpace(summary) Then Return
Dim startLocal As String = InputBox("Start (local):", "LCARS CALENDAR", ev.StartUtc.ToLocalTime().ToString("g"))
Dim endLocal As String = InputBox("End (local):", "LCARS CALENDAR", ev.EndUtc.ToLocalTime().ToString("g"))
Dim alarmRaw As String = InputBox("Alarm minutes before (0=none):", "LCARS CALENDAR", ev.AlarmMinutesBefore.ToString())
Dim alarm As Integer = ev.AlarmMinutesBefore Integer.TryParse(If(alarmRaw, "").Trim(), alarm)
Dim startUtc As DateTime = ev.StartUtc
Dim endUtc As DateTime = ev.EndUtc
Dim parsedStart As DateTime
If DateTime.TryParse(startLocal, parsedStart) Then startUtc = parsedStart.ToUniversalTime()
Dim parsedEnd As DateTime
If DateTime.TryParse(endLocal, parsedEnd) Then endUtc = parsedEnd.ToUniversalTime()
Dim all As List(Of CalendarEvent) = IcsCalendarStore.LoadEvents()
For Each item As CalendarEvent In all
If item Is Nothing Then Continue For
If Not String.Equals(item.Uid, ev.Uid, StringComparison.OrdinalIgnoreCase) Then Continue For item.Summary = summary.Trim()
item.StartUtc = startUtc item.EndUtc = endUtc item.AlarmMinutesBefore = alarm item.EwsDirty = True ev = item
Exit For
Next IcsCalendarStore.SaveEvents(all)
Dim pushed As Boolean = CalDavCalendarSync.TryPushEvent(ev) RefreshEvents()
lblStatus.Text = If(pushed, "EVENT UPDATED — CALDAV PUSHED", "EVENT UPDATED (LOCAL)")
End Sub
Private Sub OnDeleteEvent(ByVal sender As Object, ByVal e As EventArgs)
Dim idx As Integer = SelectedMonthEventIndex()
If idx < 0 OrElse idx >= monthEvents.Count Then lblStatus.Text = "SELECT AN EVENT"
Return
End If
Dim ev As CalendarEvent = monthEvents(idx)
If MessageBox.Show(Me, "Delete this event?", "LCARS CALENDAR", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) <> DialogResult.Yes Then Return
If Not String.IsNullOrWhiteSpace(ev.EwsItemId) Then EwsCalendarDeleteQueue.Enqueue(ev.EwsItemId)
Dim all As List(Of CalendarEvent) = IcsCalendarStore.LoadEvents()
all.RemoveAll(Function(x) x IsNot Nothing AndAlso String.Equals(x.Uid, ev.Uid, StringComparison.OrdinalIgnoreCase))
IcsCalendarStore.SaveEvents(all)
Dim deletedRemote As Boolean = CalDavCalendarSync.TryDeleteEvent(ev) RefreshEvents()
lblStatus.Text = If(deletedRemote, "EVENT DELETED — CALDAV", "EVENT DELETED (LOCAL)")
End Sub
Private Sub OnCalDavSync(ByVal sender As Object, ByVal e As EventArgs)
Dim n As Integer = CalDavCalendarSync.TryPullAndMerge() RefreshEvents()
If n < 0 Then lblStatus.Text = "CALDAV PULL FAILED"
Else lblStatus.Text = "CALDAV MERGED " & n.ToString() & " NEW EVENT(S)" & BuildCalDavStatusHint()
End If
End Sub
End Class
