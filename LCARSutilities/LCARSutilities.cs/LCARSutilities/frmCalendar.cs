using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using LCARS;
using Microsoft.VisualBasic;

namespace LCARSutilities;

public class frmCalendar : LCARSForm
{
	private const int FormW = 760;

	private const int FormH = 560;

	private ListBox lstEvents;

	private ListView lstTasks;

	private Label lblMonth;

	private Label lblStatus;

	private DateTime viewMonth;

	private Timer alarmTimer;

	private readonly HashSet<string> firedAlarmKeys;

	private readonly List<CalendarEvent> monthEvents;

	private bool showTasksOnly;

	private bool showWeekView;

	private bool showInvitesOnly;

	private bool showRecurringOnly;

	private bool showAlarmsOnly;

	private CalendarMonthGrid monthGrid;

	private ComboBox cboCalendar;

	private ComboBox cboCategory;

	private Timer calDavLiveTimer;

	private string selectedCalendarFilter;

	private string selectedCategoryFilter;

	private string lastCalendarFilterSignature;

	private string lastCategoryFilterSignature;

	private bool rebuildingCalendarFilter;

	private bool rebuildingCategoryFilter;

	public frmCalendar()
	{
		viewMonth = DateTime.Today;
		firedAlarmKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		monthEvents = new List<CalendarEvent>();
		selectedCalendarFilter = "";
		selectedCategoryFilter = "";
		lastCalendarFilterSignature = "";
		lastCategoryFilterSignature = "";
		modUtilities.InitUtilityFormFloating(this, 760, 560);
		BuildUi();
		RefreshEvents();
		StartAlarmTimer();
		StartCalDavLiveTimer();
	}

	private void BuildUi()
	{
		modUtilities.MakeUtilityTitle(this, "CALENDAR", 340);
		modUtilities.MakeUtilityButton(this, "IMPORT ICS", 338, 12, 120, 28, LCARScolorStyles.PrimaryFunction, OnImportIcs);
		modUtilities.MakeUtilityButton(this, "ADD", 468, 12, 80, 28, LCARScolorStyles.MiscFunction, OnAddEvent);
		modUtilities.MakeUtilityButton(this, "ADD TASK", 128, 48, 100, 28, LCARScolorStyles.CriticalFunction, OnAddTask);
		modUtilities.MakeUtilityButton(this, "TASKS", 238, 48, 80, 28, LCARScolorStyles.NavigationFunction, OnToggleTasksView);
		modUtilities.MakeUtilityButton(this, "WEEK", 328, 48, 72, 28, LCARScolorStyles.NavigationFunction, OnToggleWeekView);
		modUtilities.MakeUtilityButton(this, "INVITES", 128, 84, 88, 28, LCARScolorStyles.CriticalFunction, OnToggleInvitesView);
		modUtilities.MakeUtilityButton(this, "RECUR", 228, 84, 72, 28, LCARScolorStyles.NavigationFunction, OnToggleRecurringView);
		modUtilities.MakeUtilityButton(this, "ALARM", 308, 84, 72, 28, LCARScolorStyles.SystemFunction, OnToggleAlarmsView);
		modUtilities.MakeUtilityButton(this, "EDIT", 218, 48, 80, 28, LCARScolorStyles.PrimaryFunction, OnEditEvent);
		modUtilities.MakeUtilityButton(this, "DELETE", 308, 48, 90, 28, LCARScolorStyles.Orange, OnDeleteEvent);
		modUtilities.MakeUtilityButton(this, "CALDAV", 408, 48, 90, 28, LCARScolorStyles.NavigationFunction, OnCalDavSync);
		modUtilities.MakeUtilityButton(this, "ACCEPT", 508, 48, 72, 28, LCARScolorStyles.MiscFunction, [SpecialName] (object s, EventArgs e) =>
		{
			ApplyMeetingRsvp("ACCEPTED");
		});
		modUtilities.MakeUtilityButton(this, "TENT", 588, 48, 72, 28, LCARScolorStyles.SystemFunction, [SpecialName] (object s, EventArgs e) =>
		{
			ApplyMeetingRsvp("TENTATIVE");
		});
		modUtilities.MakeUtilityButton(this, "DECLINE", 668, 48, 72, 28, LCARScolorStyles.Orange, [SpecialName] (object s, EventArgs e) =>
		{
			ApplyMeetingRsvp("DECLINED");
		});
		modUtilities.MakeUtilityButton(this, "DETACH", 128, 120, 88, 28, LCARScolorStyles.SystemFunction, OnDetachRecurrenceInstance);
		modUtilities.MakeUtilityButton(this, "DONE", 248, 12, 80, 28, LCARScolorStyles.SystemFunction, OnToggleTaskDone);
		modUtilities.MakeUtilityButton(this, "PREV", 558, 12, 80, 28, LCARScolorStyles.NavigationFunction, [SpecialName] (object s, EventArgs e) =>
		{
			ShiftMonth(-1);
		});
		modUtilities.MakeUtilityButton(this, "NEXT", 648, 12, 80, 28, LCARScolorStyles.NavigationFunction, [SpecialName] (object s, EventArgs e) =>
		{
			ShiftMonth(1);
		});
		modUtilities.MakeUtilityButton(this, "CLOSE", 628, 48, 110, 28, LCARScolorStyles.Orange, [SpecialName] (object s, EventArgs e) =>
		{
			Close();
		});
		lblMonth = modUtilities.MakeUtilityLabel(this, 12, 48, 480, 24, 16f);
		cboCalendar = new ComboBox();
		cboCalendar.DropDownStyle = ComboBoxStyle.DropDownList;
		ComboBox comboBox = cboCalendar;
		Point location = new Point(488, 48);
		comboBox.Location = location;
		ComboBox comboBox2 = cboCalendar;
		Size size = new Size(250, 24);
		comboBox2.Size = size;
		cboCalendar.BackColor = Color.FromArgb(40, 40, 40);
		cboCalendar.ForeColor = Color.Orange;
		cboCalendar.SelectedIndexChanged += OnCalendarFilterChanged;
		Controls.Add(cboCalendar);
		cboCategory = new ComboBox();
		cboCategory.DropDownStyle = ComboBoxStyle.DropDownList;
		ComboBox comboBox3 = cboCategory;
		location = new Point(488, 72);
		comboBox3.Location = location;
		ComboBox comboBox4 = cboCategory;
		size = new Size(250, 24);
		comboBox4.Size = size;
		cboCategory.BackColor = Color.FromArgb(40, 40, 40);
		cboCategory.ForeColor = Color.Orange;
		cboCategory.SelectedIndexChanged += OnCategoryFilterChanged;
		Controls.Add(cboCategory);
		monthGrid = new CalendarMonthGrid();
		CalendarMonthGrid calendarMonthGrid = monthGrid;
		location = new Point(12, 76);
		calendarMonthGrid.Location = location;
		CalendarMonthGrid calendarMonthGrid2 = monthGrid;
		size = new Size(200, 160);
		calendarMonthGrid2.Size = size;
		Controls.Add(monthGrid);
		lstEvents = new ListBox();
		ListBox listBox = lstEvents;
		location = new Point(222, 76);
		listBox.Location = location;
		ListBox listBox2 = lstEvents;
		size = new Size(526, 434);
		listBox2.Size = size;
		lstEvents.BackColor = Color.FromArgb(30, 30, 30);
		lstEvents.ForeColor = Color.Orange;
		lstEvents.Font = new Font("Arial Narrow", 11f);
		lstEvents.DoubleClick += OnEditEvent;
		Controls.Add(lstEvents);
		lstTasks = new ListView();
		lstTasks.View = View.Details;
		lstTasks.FullRowSelect = true;
		lstTasks.HeaderStyle = ColumnHeaderStyle.Nonclickable;
		lstTasks.BackColor = Color.FromArgb(30, 30, 30);
		lstTasks.ForeColor = Color.Orange;
		lstTasks.Font = new Font("Arial Narrow", 11f);
		lstTasks.Location = lstEvents.Location;
		lstTasks.Size = lstEvents.Size;
		lstTasks.Columns.Add("DUE", 130);
		lstTasks.Columns.Add("PRI", 36);
		lstTasks.Columns.Add("%", 36);
		lstTasks.Columns.Add("SUMMARY", 480);
		lstTasks.Visible = false;
		lstTasks.DoubleClick += OnEditEvent;
		Controls.Add(lstTasks);
		lblStatus = modUtilities.MakeUtilityLabel(this, 12, 520, 736, 20, 10f);
	}

	private int SelectedMonthEventIndex()
	{
		if (showTasksOnly && lstTasks != null && lstTasks.Visible)
		{
			if (lstTasks.SelectedIndices.Count == 0)
			{
				return -1;
			}
			return lstTasks.SelectedIndices[0];
		}
		if (lstEvents == null)
		{
			return -1;
		}
		return lstEvents.SelectedIndex;
	}

	private void ShiftMonth(int delta)
	{
		viewMonth = viewMonth.AddMonths(delta);
		RefreshEvents();
	}

	private void RefreshEvents()
	{
		string text = viewMonth.ToString("MMMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant();
		string text2 = CalDavCalendarSync.TryGetPrimaryCalendarFriendlyLabel();
		if (text2.Length > 0 && !showTasksOnly)
		{
			text = text + " · " + text2;
		}
		lblMonth.Text = text;
		lstEvents.Items.Clear();
		if (lstTasks != null)
		{
			lstTasks.Items.Clear();
		}
		monthEvents.Clear();
		List<CalendarEvent> list = IcsCalendarStore.LoadEvents();
		MaybeRebuildCalendarFilter(list);
		MaybeRebuildCategoryFilter(list);
		string text3 = ((selectedCalendarFilter != null) ? selectedCalendarFilter : "").Trim();
		bool flag = ((text3.Length > 0 && !text3.Equals("ALL CALENDARS", StringComparison.OrdinalIgnoreCase)) ? true : false);
		string text4 = ((selectedCategoryFilter != null) ? selectedCategoryFilter : "").Trim();
		bool flag2 = ((text4.Length > 0 && !text4.Equals("ALL CATEGORIES", StringComparison.OrdinalIgnoreCase)) ? true : false);
		DateTime t = new DateTime(viewMonth.Year, viewMonth.Month, 1, 0, 0, 0, DateTimeKind.Utc);
		DateTime t2 = t.AddMonths(1);
		int num = 0;
		DateTime date = DateTime.UtcNow.Date;
		DateTime t3 = date.AddDays(7.0);
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (CalendarEvent item in list)
		{
			if (item != null)
			{
				hashSet.Add(EventCalendarLabel(item));
			}
		}
		bool flag3 = ((!flag && hashSet.Count > 1) ? true : false);
		foreach (CalendarEvent item2 in list.OrderBy([SpecialName] (CalendarEvent x) => x.StartUtc))
		{
			if ((flag && !string.Equals(EventCalendarLabel(item2), text3, StringComparison.OrdinalIgnoreCase)) || (flag2 && !string.Equals(EventCategoryLabel(item2), text4, StringComparison.OrdinalIgnoreCase)))
			{
				continue;
			}
			if (showInvitesOnly)
			{
				if (!item2.IsMeetingInvitation)
				{
					continue;
				}
			}
			else if (showRecurringOnly)
			{
				if (string.IsNullOrWhiteSpace(item2.RecurrenceRule))
				{
					continue;
				}
			}
			else if (showAlarmsOnly)
			{
				if (item2.AlarmMinutesBefore <= 0)
				{
					continue;
				}
			}
			else if (showTasksOnly)
			{
				if (!CalDavCalendarSync.IsTaskEvent(item2))
				{
					continue;
				}
			}
			else if (showWeekView)
			{
				if (DateTime.Compare(item2.StartUtc, date) < 0 || DateTime.Compare(item2.StartUtc, t3) >= 0)
				{
					continue;
				}
			}
			else if (DateTime.Compare(item2.StartUtc, t) < 0 || DateTime.Compare(item2.StartUtc, t2) >= 0)
			{
				continue;
			}
			monthEvents.Add(item2);
			CalendarEvent calendarEvent = item2;
			string text5 = ((calendarEvent.Summary != null) ? calendarEvent.Summary : "");
			if (item2.TaskCompleted && !text5.StartsWith("[DONE]", StringComparison.OrdinalIgnoreCase))
			{
				text5 = "[DONE] " + text5;
			}
			if (!string.IsNullOrWhiteSpace(item2.Category))
			{
				text5 = "[" + item2.Category.Trim().ToUpperInvariant() + "] " + text5;
			}
			if (!string.IsNullOrWhiteSpace(item2.RecurrenceRule))
			{
				text5 = text5 + " ⟳ " + item2.RecurrenceRule.Trim();
			}
			if (item2.IsMeetingInvitation)
			{
				calendarEvent = item2;
				string text6 = ((calendarEvent.MyPartStat != null) ? calendarEvent.MyPartStat : "NEEDS-ACTION").Trim().ToUpperInvariant();
				text5 = "✉ " + text6 + " " + text5;
			}
			if (showTasksOnly && lstTasks != null)
			{
				string text7 = item2.StartUtc.ToLocalTime().ToString("ddd MMM d HH:mm");
				ListViewItem listViewItem = new ListViewItem(text7);
				listViewItem.SubItems.Add(item2.TaskPriority.ToString());
				listViewItem.SubItems.Add(item2.TaskPercentComplete.ToString());
				if (item2.AlarmMinutesBefore > 0)
				{
					text5 = "⏰-" + item2.AlarmMinutesBefore + "m " + text5;
				}
				listViewItem.SubItems.Add(text5);
				lstTasks.Items.Add(listViewItem);
			}
			else
			{
				string text8 = "";
				if (item2.AlarmMinutesBefore > 0)
				{
					text8 = "⏰-" + item2.AlarmMinutesBefore + "m ";
				}
				string text9 = "";
				if (flag3)
				{
					text9 = "[" + EventCalendarLabel(item2) + "] ";
				}
				lstEvents.Items.Add(item2.StartUtc.ToLocalTime().ToString("ddd MMM d HH:mm") + "  " + text8 + text9 + text5);
			}
			num = checked(num + 1);
		}
		if (lstTasks != null)
		{
			lstTasks.Visible = showTasksOnly;
		}
		if (lstEvents != null)
		{
			lstEvents.Visible = !showTasksOnly;
		}
		string text10 = " EVENT(S) THIS MONTH";
		if (showInvitesOnly)
		{
			text10 = " MEETING INVITE(S) — LIGHTNING VIEW";
		}
		if (showRecurringOnly)
		{
			text10 = " RECURRING SERIES — LIGHTNING VIEW";
		}
		if (showAlarmsOnly)
		{
			text10 = " REMINDER(S) — LIGHTNING VIEW";
		}
		if (showTasksOnly)
		{
			text10 = " TASK(S) — LIGHTNING GRID";
		}
		if (showWeekView)
		{
			text10 = " EVENT(S) THIS WEEK";
		}
		HashSet<string> hashSet2 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (CalendarEvent monthEvent in monthEvents)
		{
			if (monthEvent != null)
			{
				hashSet2.Add(EventCalendarLabel(monthEvent));
			}
		}
		string text11 = ((hashSet2.Count > 1) ? (" · " + hashSet2.Count + " CALS") : "");
		int num2 = CalDavCalendarSync.CountKnownCalendarUrlLabels();
		if (num2 > 0)
		{
			text11 = text11 + " · DAV-LABELS " + num2;
		}
		if (flag2 && text4.Length > 0)
		{
			text11 = text11 + " · CAT " + text4;
		}
		if (flag && text3.Length > 0 && !text3.Equals("ALL CALENDARS", StringComparison.OrdinalIgnoreCase))
		{
			text11 = text11 + " · CAL " + text3;
		}
		lblStatus.Text = num + text10 + text11;
		if (monthGrid != null)
		{
			monthGrid.SetMonth(viewMonth, IcsCalendarStore.LoadEvents());
		}
	}

	private static void MarkEventDirty(CalendarEvent ev)
	{
		if (ev != null)
		{
			ev.EwsDirty = true;
			ev.CalDavDirty = true;
		}
	}

	private void OnDetachRecurrenceInstance(object sender, EventArgs e)
	{
		int num = SelectedMonthEventIndex();
		if (num < 0 || num >= monthEvents.Count)
		{
			lblStatus.Text = "SELECT A RECURRING EVENT";
			return;
		}
		CalendarEvent calendarEvent = monthEvents[num];
		if (calendarEvent == null || string.IsNullOrWhiteSpace(calendarEvent.RecurrenceRule))
		{
			lblStatus.Text = "NOT A RECURRING SERIES";
			return;
		}
		CalendarEvent calendarEvent2 = CalendarRecurrence.TryCreateRecurrenceException(calendarEvent, calendarEvent.StartUtc);
		if (calendarEvent2 == null)
		{
			lblStatus.Text = "DETACH FAILED";
			return;
		}
		List<CalendarEvent> list = IcsCalendarStore.LoadEvents();
		CalendarRecurrence.TryAppendExDateOnSeriesMaster(list, calendarEvent, calendarEvent.StartUtc);
		CalendarRecurrence.TryDecrementRecurrenceCountOnSeries(calendarEvent);
		CalendarRecurrence.TryClampRecurrenceUntilBeforeInstance(calendarEvent, calendarEvent.StartUtc);
		list.Add(calendarEvent2);
		IcsCalendarStore.SaveEvents(list);
		CalDavCalendarSync.TryPushEvent(calendarEvent);
		CalDavCalendarSync.TryPushEvent(calendarEvent2);
		RefreshEvents();
		lblStatus.Text = "RECURRENCE EXCEPTION + EXDATE ON SERIES (SLICE 15 PARTIAL)";
	}

	private void ApplyMeetingRsvp(string partStat)
	{
		int num = SelectedMonthEventIndex();
		if (num < 0 || num >= monthEvents.Count)
		{
			lblStatus.Text = "SELECT A MEETING INVITE";
			return;
		}
		CalendarEvent calendarEvent = monthEvents[num];
		if (calendarEvent == null || string.IsNullOrWhiteSpace(calendarEvent.Uid))
		{
			return;
		}
		string text = ((partStat != null) ? partStat : "").Trim().ToUpperInvariant();
		if (text.Length == 0)
		{
			return;
		}
		List<CalendarEvent> list = IcsCalendarStore.LoadEvents();
		bool flag = false;
		foreach (CalendarEvent item in list)
		{
			if (item == null || !string.Equals(item.Uid, calendarEvent.Uid, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			item.MyPartStat = text;
			item.IsMeetingInvitation = true;
			if (string.IsNullOrWhiteSpace(item.AttendeeMailto))
			{
				item.AttendeeMailto = MailAttendeeHint.TryGetPrimaryAttendeeMailto();
			}
			MarkEventDirty(item);
			flag = true;
			break;
		}
		if (!flag)
		{
			lblStatus.Text = "RSVP TARGET NOT FOUND";
			return;
		}
		IcsCalendarStore.SaveEvents(list);
		CalendarEvent calendarEvent2 = null;
		foreach (CalendarEvent item2 in list)
		{
			if (item2 != null && string.Equals(item2.Uid, calendarEvent.Uid, StringComparison.OrdinalIgnoreCase))
			{
				calendarEvent2 = item2;
				break;
			}
		}
		bool flag2 = false;
		if (calendarEvent2 != null)
		{
			calendarEvent2.CalDavDirty = true;
			flag2 = CalDavCalendarSync.TryPushEvent(calendarEvent2);
			if (flag2)
			{
				IcsCalendarStore.SaveEvents(list);
			}
		}
		if (calendarEvent2 != null)
		{
			EwsCalendarRsvpQueue.Enqueue(calendarEvent2.Uid, text);
			CalendarMeetingReplyQueue.Enqueue(calendarEvent2.Uid, text, calendarEvent2.Organizer, calendarEvent2.Summary, calendarEvent2.AttendeeMailto);
		}
		bool flag3 = false;
		if (calendarEvent2 != null)
		{
			flag3 = MailAttendeeHint.TryLaunchOrganizerRsvpMail(calendarEvent2.Organizer, calendarEvent2.Summary, text);
		}
		RefreshEvents();
		string text2 = (flag3 ? " · ORGANIZER MAILTO" : " · MAIL RSVP PENDING");
		string text3 = " · EWS+IMIP QUEUED";
		lblStatus.Text = "RSVP " + text + (flag2 ? " — CALDAV ATTENDEE PUSHED" : " — CALDAV PUSH PENDING") + text2 + text3;
	}

	private void OnToggleInvitesView(object sender, EventArgs e)
	{
		showInvitesOnly = !showInvitesOnly;
		if (showInvitesOnly)
		{
			showTasksOnly = false;
			showWeekView = false;
			showRecurringOnly = false;
			showAlarmsOnly = false;
		}
		RefreshEvents();
	}

	private void OnToggleRecurringView(object sender, EventArgs e)
	{
		showRecurringOnly = !showRecurringOnly;
		if (showRecurringOnly)
		{
			showTasksOnly = false;
			showWeekView = false;
			showInvitesOnly = false;
			showAlarmsOnly = false;
		}
		RefreshEvents();
		lblStatus.Text = (showRecurringOnly ? "RECURRING SERIES VIEW" : "MONTH VIEW");
	}

	private void OnToggleAlarmsView(object sender, EventArgs e)
	{
		showAlarmsOnly = !showAlarmsOnly;
		if (showAlarmsOnly)
		{
			showTasksOnly = false;
			showWeekView = false;
			showInvitesOnly = false;
			showRecurringOnly = false;
		}
		RefreshEvents();
		lblStatus.Text = (showAlarmsOnly ? "REMINDER EVENTS VIEW" : "MONTH VIEW");
	}

	private void StartCalDavLiveTimer()
	{
		calDavLiveTimer = new Timer();
		calDavLiveTimer.Interval = 4000;
		calDavLiveTimer.Tick += OnCalDavLiveTick;
		calDavLiveTimer.Start();
	}

	private void OnCalDavLiveTick(object sender, EventArgs e)
	{
		int num = CalDavCalendarSync.TrySyncBidirectional();
		if (lblStatus != null)
		{
			string text = BuildCalDavStatusHint();
			if (num < 0)
			{
				lblStatus.Text = "CALDAV LIVE SYNC FAILED" + text;
			}
			else if (num > 0)
			{
				string text2 = "";
				if (CalDavCalendarSync.LastBidirectionalPushCount > 0)
				{
					text2 = " · PUSH " + CalDavCalendarSync.LastBidirectionalPushCount;
				}
				lblStatus.Text = "CALDAV LIVE " + num + " ITEM(S) SYNCED" + text2 + text;
			}
		}
		if (num != 0)
		{
			RefreshEvents();
		}
	}

	private static string BuildCalDavStatusHint()
	{
		string text = "";
		string text2 = CalDavCalendarSync.TryGetPrimaryCalendarFriendlyLabel();
		if (text2.Length > 0)
		{
			text = text + " · " + text2;
		}
		int num = CalDavCalendarSync.CountKnownCalendarUrlLabels();
		if (num > 0)
		{
			text = text + " · LABELS " + num;
		}
		if (CalDavCalendarSync.LastPrimaryEventsTagged > 0)
		{
			text = text + " · TAG " + CalDavCalendarSync.LastPrimaryEventsTagged;
		}
		if (CalDavCalendarSync.LastSecondaryEventsTagged > 0)
		{
			text = text + " · 2ND-TAG " + CalDavCalendarSync.LastSecondaryEventsTagged;
		}
		if (CalDavCalendarSync.LastTaskEventsTagged > 0)
		{
			text = text + " · TASK-TAG " + CalDavCalendarSync.LastTaskEventsTagged;
		}
		if (CalDavCalendarSync.LastPullMergeAdded > 0)
		{
			text = text + " · PULL " + CalDavCalendarSync.LastPullMergeAdded;
		}
		if (CalDavCalendarSync.LastSecondaryPullMergeAdded > 0)
		{
			text = text + " · 2ND-PULL " + CalDavCalendarSync.LastSecondaryPullMergeAdded;
		}
		if (CalDavCalendarSync.LastUrlLabelsAtPull > 0)
		{
			text = text + " · PROP " + CalDavCalendarSync.LastUrlLabelsAtPull;
		}
		if (CalDavCalendarSync.LastPrimaryPullMergeAdded > 0)
		{
			text = text + " · 1ST-PULL " + CalDavCalendarSync.LastPrimaryPullMergeAdded;
		}
		if (CalDavCalendarSync.LastTaskPullMergeAdded > 0)
		{
			text = text + " · TASK-PULL " + CalDavCalendarSync.LastTaskPullMergeAdded;
		}
		return text;
	}

	private void OnCalendarFilterChanged(object sender, EventArgs e)
	{
		if (!rebuildingCalendarFilter && cboCalendar != null && cboCalendar.SelectedItem != null)
		{
			selectedCalendarFilter = cboCalendar.SelectedItem.ToString().Trim();
			RefreshEvents();
		}
	}

	private void OnCategoryFilterChanged(object sender, EventArgs e)
	{
		if (!rebuildingCategoryFilter && cboCategory != null && cboCategory.SelectedItem != null)
		{
			selectedCategoryFilter = cboCategory.SelectedItem.ToString().Trim();
			RefreshEvents();
		}
	}

	private void MaybeRebuildCalendarFilter(IList<CalendarEvent> events)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		hashSet.Add("ALL CALENDARS");
		if (events != null)
		{
			foreach (CalendarEvent @event in events)
			{
				hashSet.Add(EventCalendarLabel(@event));
			}
		}
		string a = string.Join("|", hashSet.OrderBy([SpecialName] (string x) => x));
		if (!string.Equals(a, lastCalendarFilterSignature, StringComparison.Ordinal))
		{
			lastCalendarFilterSignature = a;
			RebuildCalendarFilter(hashSet);
		}
	}

	private void RebuildCalendarFilter(IEnumerable<string> names)
	{
		if (cboCalendar == null)
		{
			return;
		}
		string text = selectedCalendarFilter;
		rebuildingCalendarFilter = true;
		cboCalendar.Items.Clear();
		foreach (string item in names.OrderBy([SpecialName] (string x) => x))
		{
			cboCalendar.Items.Add(item);
		}
		rebuildingCalendarFilter = false;
		if (text.Length > 0 && cboCalendar.Items.Contains(text))
		{
			cboCalendar.SelectedItem = text;
		}
		else if (cboCalendar.Items.Count > 0)
		{
			cboCalendar.SelectedIndex = 0;
			selectedCalendarFilter = cboCalendar.SelectedItem.ToString();
		}
	}

	private string EventCalendarLabel(CalendarEvent ev)
	{
		if (ev == null)
		{
			return "DEFAULT";
		}
		string text = ((ev.CalendarName != null) ? ev.CalendarName : "").Trim();
		if (text.Length == 0)
		{
			string text2 = CalDavCalendarSync.TryGetPrimaryCalendarFriendlyLabel();
			if (text2.Length > 0)
			{
				return text2;
			}
			return "DEFAULT";
		}
		return CalDavCalendarSync.TryGetCalendarFriendlyLabel(text);
	}

	private static string EventCategoryLabel(CalendarEvent ev)
	{
		if (ev == null)
		{
			return "UNCATEGORIZED";
		}
		string text = ((ev.Category != null) ? ev.Category : "").Trim();
		if (text.Length == 0)
		{
			return "UNCATEGORIZED";
		}
		return text.ToUpperInvariant();
	}

	private void MaybeRebuildCategoryFilter(IList<CalendarEvent> events)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		hashSet.Add("ALL CATEGORIES");
		if (events != null)
		{
			foreach (CalendarEvent @event in events)
			{
				hashSet.Add(EventCategoryLabel(@event));
			}
		}
		string a = string.Join("|", hashSet.OrderBy([SpecialName] (string x) => x));
		if (!string.Equals(a, lastCategoryFilterSignature, StringComparison.Ordinal))
		{
			lastCategoryFilterSignature = a;
			RebuildCategoryFilter(hashSet);
		}
	}

	private void RebuildCategoryFilter(IEnumerable<string> names)
	{
		if (cboCategory == null)
		{
			return;
		}
		string text = selectedCategoryFilter;
		rebuildingCategoryFilter = true;
		cboCategory.Items.Clear();
		foreach (string item in names.OrderBy([SpecialName] (string x) => x))
		{
			cboCategory.Items.Add(item);
		}
		rebuildingCategoryFilter = false;
		if (text.Length > 0 && cboCategory.Items.Contains(text))
		{
			cboCategory.SelectedItem = text;
		}
		else if (cboCategory.Items.Count > 0)
		{
			cboCategory.SelectedIndex = 0;
			selectedCategoryFilter = cboCategory.SelectedItem.ToString();
		}
	}

	private void OnToggleTasksView(object sender, EventArgs e)
	{
		showTasksOnly = !showTasksOnly;
		if (showTasksOnly)
		{
			showWeekView = false;
		}
		RefreshEvents();
		lblStatus.Text = (showTasksOnly ? "TASK LIST VIEW" : "MONTH VIEW");
	}

	private void OnToggleWeekView(object sender, EventArgs e)
	{
		showWeekView = !showWeekView;
		if (showWeekView)
		{
			showTasksOnly = false;
		}
		RefreshEvents();
		lblStatus.Text = (showWeekView ? "WEEK VIEW (7 DAYS)" : "MONTH VIEW");
	}

	private void OnToggleTaskDone(object sender, EventArgs e)
	{
		int num = SelectedMonthEventIndex();
		if (num < 0 || num >= monthEvents.Count)
		{
			lblStatus.Text = "SELECT A TASK";
			return;
		}
		CalendarEvent calendarEvent = monthEvents[num];
		CalendarEvent calendarEvent2 = calendarEvent;
		string text = ((calendarEvent2.Summary != null) ? calendarEvent2.Summary : "");
		if (text.IndexOf("[TASK]", StringComparison.OrdinalIgnoreCase) < 0)
		{
			lblStatus.Text = "NOT A TASK";
			return;
		}
		bool flag = !calendarEvent.TaskCompleted;
		string summary = text;
		if (flag)
		{
			if (!text.StartsWith("[DONE]", StringComparison.OrdinalIgnoreCase))
			{
				summary = "[DONE] " + text;
			}
		}
		else if (text.StartsWith("[DONE]", StringComparison.OrdinalIgnoreCase))
		{
			summary = text.Substring(6).TrimStart();
		}
		List<CalendarEvent> list = IcsCalendarStore.LoadEvents();
		foreach (CalendarEvent item in list)
		{
			if (item == null || !string.Equals(item.Uid, calendarEvent.Uid, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			item.TaskCompleted = flag;
			item.Summary = summary;
			item.EwsDirty = true;
			break;
		}
		IcsCalendarStore.SaveEvents(list);
		bool flag2 = false;
		foreach (CalendarEvent item2 in list)
		{
			if (item2 == null || !string.Equals(item2.Uid, calendarEvent.Uid, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			flag2 = CalDavTaskPush.TryPushTask(item2);
			break;
		}
		RefreshEvents();
		if (flag2)
		{
			lblStatus.Text = (flag ? "TASK DONE — CALDAV PUSHED" : "TASK REOPENED — CALDAV PUSHED");
		}
		else
		{
			lblStatus.Text = (flag ? "TASK DONE (LOCAL)" : "TASK REOPENED (LOCAL)");
		}
	}

	private void OnImportIcs(object sender, EventArgs e)
	{
		using OpenFileDialog openFileDialog = new OpenFileDialog();
		openFileDialog.Filter = "iCalendar (*.ics)|*.ics";
		if (openFileDialog.ShowDialog(this) == DialogResult.OK)
		{
			int num = IcsCalendarStore.ImportIcsFile(openFileDialog.FileName);
			lblStatus.Text = "IMPORTED " + num + " EVENT(S)";
			RefreshEvents();
		}
	}

	private void StartAlarmTimer()
	{
		alarmTimer = new Timer();
		alarmTimer.Interval = 60000;
		alarmTimer.Tick += OnAlarmTick;
		alarmTimer.Start();
		OnAlarmTick(null, EventArgs.Empty);
	}

	private void OnAlarmTick(object sender, EventArgs e)
	{
		DateTime utcNow = DateTime.UtcNow;
		foreach (CalendarEvent item2 in IcsCalendarStore.LoadEvents())
		{
			if (item2 == null || item2.AlarmMinutesBefore <= 0)
			{
				continue;
			}
			DateTime t = item2.StartUtc.AddMinutes(checked(-item2.AlarmMinutesBefore));
			if (DateTime.Compare(t, utcNow) <= 0 && DateTime.Compare(t, utcNow.AddMinutes(-2.0)) >= 0)
			{
				string item = item2.Uid + "|" + t.ToString("o");
				if (firedAlarmKeys.Add(item))
				{
					CalendarEvent calendarEvent = item2;
					MessageBox.Show(((calendarEvent.Summary != null) ? calendarEvent.Summary : "EVENT").ToUpperInvariant() + "\r\n" + item2.StartUtc.ToLocalTime().ToString("g"), "LCARS CALENDAR ALARM", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
				}
			}
		}
	}

	protected override void OnFormClosed(FormClosedEventArgs e)
	{
		if (alarmTimer != null)
		{
			alarmTimer.Stop();
			alarmTimer.Dispose();
		}
		if (calDavLiveTimer != null)
		{
			calDavLiveTimer.Stop();
			calDavLiveTimer.Dispose();
		}
		base.OnFormClosed(e);
	}

	private void OnAddTask(object sender, EventArgs e)
	{
		using frmTaskEditor frmTaskEditor2 = new frmTaskEditor();
		if (frmTaskEditor2.ShowDialog(this) == DialogResult.OK && frmTaskEditor2.TaskResult != null)
		{
			CalendarEvent taskResult = frmTaskEditor2.TaskResult;
			MarkEventDirty(taskResult);
			List<CalendarEvent> list = IcsCalendarStore.LoadEvents();
			list.Add(taskResult);
			IcsCalendarStore.SaveEvents(list);
			bool flag = CalDavTaskPush.TryPushTask(taskResult);
			RefreshEvents();
			lblStatus.Text = (flag ? "TASK ADDED — CALDAV PUSHED" : "TASK ADDED (LOCAL)");
		}
	}

	private void OnEditTask(CalendarEvent ev)
	{
		if (ev == null)
		{
			return;
		}
		using frmTaskEditor frmTaskEditor2 = new frmTaskEditor(ev);
		if (frmTaskEditor2.ShowDialog(this) != DialogResult.OK || frmTaskEditor2.TaskResult == null)
		{
			return;
		}
		CalendarEvent calendarEvent = frmTaskEditor2.TaskResult;
		calendarEvent.Uid = ev.Uid;
		List<CalendarEvent> list = IcsCalendarStore.LoadEvents();
		foreach (CalendarEvent item in list)
		{
			if (item == null || !string.Equals(item.Uid, ev.Uid, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			item.Summary = calendarEvent.Summary;
			item.StartUtc = calendarEvent.StartUtc;
			item.EndUtc = calendarEvent.EndUtc;
			item.AlarmMinutesBefore = calendarEvent.AlarmMinutesBefore;
			item.TaskCompleted = calendarEvent.TaskCompleted;
			item.TaskPriority = calendarEvent.TaskPriority;
			item.TaskPercentComplete = calendarEvent.TaskPercentComplete;
			item.Category = calendarEvent.Category;
			item.RecurrenceRule = calendarEvent.RecurrenceRule;
			item.CalendarName = calendarEvent.CalendarName;
			item.CalDavDirty = true;
			item.EwsDirty = true;
			calendarEvent = item;
			break;
		}
		IcsCalendarStore.SaveEvents(list);
		bool flag = CalDavTaskPush.TryPushTask(calendarEvent);
		RefreshEvents();
		lblStatus.Text = (flag ? "TASK UPDATED — CALDAV PUSHED" : "TASK UPDATED (LOCAL)");
	}

	private void OnAddEvent(object sender, EventArgs e)
	{
		string text = Interaction.InputBox("Event title:", "LCARS CALENDAR", "MEETING");
		if (!string.IsNullOrWhiteSpace(text))
		{
			string text2 = Interaction.InputBox("Category (optional):", "LCARS CALENDAR", "PERSONAL");
			string text3 = Interaction.InputBox("Calendar name (optional):", "LCARS CALENDAR", (text2 != null) ? text2 : "PERSONAL");
			string text4 = Interaction.InputBox("Recurrence RRULE (optional, e.g. FREQ=WEEKLY):", "LCARS CALENDAR");
			CalendarEvent calendarEvent = new CalendarEvent();
			calendarEvent.Summary = text.Trim();
			calendarEvent.StartUtc = DateTime.UtcNow;
			calendarEvent.EndUtc = DateTime.UtcNow.AddHours(1.0);
			calendarEvent.EwsDirty = true;
			calendarEvent.CalDavDirty = true;
			calendarEvent.Category = ((text2 != null) ? text2 : "").Trim();
			calendarEvent.CalendarName = ((text3 != null) ? text3 : "").Trim();
			calendarEvent.RecurrenceRule = ((text4 != null) ? text4 : "").Trim();
			CalendarEvent calendarEvent2 = calendarEvent;
			List<CalendarEvent> list = IcsCalendarStore.LoadEvents();
			list.Add(calendarEvent2);
			IcsCalendarStore.SaveEvents(list);
			bool flag = CalDavCalendarSync.TryPushEvent(calendarEvent2);
			RefreshEvents();
			lblStatus.Text = (flag ? "EVENT ADDED — CALDAV PUSHED" : "EVENT ADDED (LOCAL)");
		}
	}

	private void OnEditEvent(object sender, EventArgs e)
	{
		int num = SelectedMonthEventIndex();
		if (num < 0 || num >= monthEvents.Count)
		{
			lblStatus.Text = "SELECT AN EVENT";
			return;
		}
		CalendarEvent calendarEvent = monthEvents[num];
		if (CalDavCalendarSync.IsTaskEvent(calendarEvent))
		{
			OnEditTask(calendarEvent);
			return;
		}
		CalendarEvent calendarEvent2 = calendarEvent;
		string text = Interaction.InputBox("Event title:", "LCARS CALENDAR", (calendarEvent2.Summary != null) ? calendarEvent2.Summary : "");
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		string s = Interaction.InputBox("Start (local):", "LCARS CALENDAR", calendarEvent.StartUtc.ToLocalTime().ToString("g"));
		string s2 = Interaction.InputBox("End (local):", "LCARS CALENDAR", calendarEvent.EndUtc.ToLocalTime().ToString("g"));
		string text2 = Interaction.InputBox("Alarm minutes before (0=none):", "LCARS CALENDAR", calendarEvent.AlarmMinutesBefore.ToString());
		int result = calendarEvent.AlarmMinutesBefore;
		int.TryParse(((text2 != null) ? text2 : "").Trim(), out result);
		DateTime startUtc = calendarEvent.StartUtc;
		DateTime endUtc = calendarEvent.EndUtc;
		if (DateTime.TryParse(s, out var result2))
		{
			startUtc = result2.ToUniversalTime();
		}
		if (DateTime.TryParse(s2, out var result3))
		{
			endUtc = result3.ToUniversalTime();
		}
		List<CalendarEvent> list = IcsCalendarStore.LoadEvents();
		foreach (CalendarEvent item in list)
		{
			if (item == null || !string.Equals(item.Uid, calendarEvent.Uid, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			item.Summary = text.Trim();
			item.StartUtc = startUtc;
			item.EndUtc = endUtc;
			item.AlarmMinutesBefore = result;
			item.EwsDirty = true;
			calendarEvent = item;
			break;
		}
		IcsCalendarStore.SaveEvents(list);
		bool flag = CalDavCalendarSync.TryPushEvent(calendarEvent);
		RefreshEvents();
		lblStatus.Text = (flag ? "EVENT UPDATED — CALDAV PUSHED" : "EVENT UPDATED (LOCAL)");
	}

	private void OnDeleteEvent(object sender, EventArgs e)
	{
		int num = SelectedMonthEventIndex();
		if (num < 0 || num >= monthEvents.Count)
		{
			lblStatus.Text = "SELECT AN EVENT";
			return;
		}
		CalendarEvent calendarEvent = monthEvents[num];
		if (MessageBox.Show(this, "Delete this event?", "LCARS CALENDAR", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes)
		{
			if (!string.IsNullOrWhiteSpace(calendarEvent.EwsItemId))
			{
				EwsCalendarDeleteQueue.Enqueue(calendarEvent.EwsItemId);
			}
			List<CalendarEvent> list = IcsCalendarStore.LoadEvents();
			list.RemoveAll([SpecialName] (CalendarEvent x) => (x != null && string.Equals(x.Uid, calendarEvent.Uid, StringComparison.OrdinalIgnoreCase)) ? true : false);
			IcsCalendarStore.SaveEvents(list);
			bool flag = CalDavCalendarSync.TryDeleteEvent(calendarEvent);
			RefreshEvents();
			lblStatus.Text = (flag ? "EVENT DELETED — CALDAV" : "EVENT DELETED (LOCAL)");
		}
	}

	private void OnCalDavSync(object sender, EventArgs e)
	{
		int num = CalDavCalendarSync.TryPullAndMerge();
		RefreshEvents();
		if (num < 0)
		{
			lblStatus.Text = "CALDAV PULL FAILED";
		}
		else
		{
			lblStatus.Text = "CALDAV MERGED " + num + " NEW EVENT(S)" + BuildCalDavStatusHint();
		}
	}
}
