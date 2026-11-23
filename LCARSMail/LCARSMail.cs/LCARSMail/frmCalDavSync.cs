using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using LCARS;
using LCARS.Controls;

namespace LCARSMail;

public class frmCalDavSync : LCARSForm
{
	private TextBox txtUrl;

	private TextBox txtUser;

	private TextBox txtPassword;

	private ListBox lstEvents;

	private Label lblStatus;

	public frmCalDavSync()
	{
		BackColor = Color.Black;
		Size clientSize = new Size(640, 480);
		ClientSize = clientSize;
		StartPosition = FormStartPosition.CenterParent;
		BuildUi();
		TryLoadStoredCalDav();
		RefreshLocalEventList();
	}

	private void RefreshLocalEventList()
	{
		lstEvents.Items.Clear();
		foreach (LcarsCalendarBridge.SharedCalendarEvent item in LcarsCalendarBridge.LoadEvents())
		{
			if (item != null)
			{
				string text = (string.IsNullOrWhiteSpace(item.CalendarName) ? "" : (item.CalendarName.Trim() + " · "));
				string text2 = (string.IsNullOrWhiteSpace(item.RecurrenceRule) ? "" : " RRULE");
				string text3 = (string.IsNullOrWhiteSpace(item.Category) ? "" : (" [" + item.Category + "]"));
				ListBox.ObjectCollection items = lstEvents.Items;
				LcarsCalendarBridge.SharedCalendarEvent sharedCalendarEvent = item;
				items.Add(text + ((sharedCalendarEvent.Summary != null) ? sharedCalendarEvent.Summary : item.Uid) + text2 + text3);
			}
		}
		int num = LcarsCalendarBridge.CountCalDavDirtyEvents();
		lblStatus.Text = lstEvents.Items.Count + " LOCAL · DIRTY " + num + " · CAL " + LcarsCalendarBridge.CountDistinctCalendarNames() + " · EWS-LINK " + LcarsCalendarBridge.CountEventsLinkedToEws();
	}

	private void BuildUi()
	{
		AddLabel("CALENDAR URL", 12, 12);
		txtUrl = AddField(12, 32, 610);
		AddLabel("USERNAME", 12, 64);
		txtUser = AddField(12, 84, 610);
		AddLabel("PASSWORD", 12, 116);
		txtPassword = AddField(12, 136, 610);
		txtPassword.UseSystemPasswordChar = true;
		FlatButton flatButton = new FlatButton();
		flatButton.ButtonText = "SYNC";
		flatButton.Text = "SYNC";
		flatButton.Color = LCARScolorStyles.PrimaryFunction;
		Size size = new Size(90, 28);
		flatButton.Size = size;
		Point location = new Point(12, 168);
		flatButton.Location = location;
		flatButton.Click += OnSync;
		Controls.Add(flatButton);
		FlatButton flatButton2 = new FlatButton();
		flatButton2.ButtonText = "PUSH";
		flatButton2.Text = "PUSH";
		flatButton2.Color = LCARScolorStyles.SystemFunction;
		size = new Size(72, 28);
		flatButton2.Size = size;
		location = new Point(108, 168);
		flatButton2.Location = location;
		flatButton2.Click += OnPush;
		Controls.Add(flatButton2);
		FlatButton flatButton3 = new FlatButton();
		flatButton3.ButtonText = "2-WAY";
		flatButton3.Text = "2-WAY";
		flatButton3.Color = LCARScolorStyles.CriticalFunction;
		size = new Size(72, 28);
		flatButton3.Size = size;
		location = new Point(268, 168);
		flatButton3.Location = location;
		flatButton3.Click += OnBidirectionalSync;
		Controls.Add(flatButton3);
		FlatButton flatButton4 = new FlatButton();
		flatButton4.ButtonText = "CAL UI";
		flatButton4.Text = "CAL UI";
		flatButton4.Color = LCARScolorStyles.MiscFunction;
		size = new Size(72, 28);
		flatButton4.Size = size;
		location = new Point(188, 168);
		flatButton4.Location = location;
		flatButton4.Click += OnOpenCalendar;
		Controls.Add(flatButton4);
		lstEvents = new ListBox();
		ListBox listBox = lstEvents;
		location = new Point(12, 204);
		listBox.Location = location;
		ListBox listBox2 = lstEvents;
		size = new Size(610, 220);
		listBox2.Size = size;
		lstEvents.BackColor = Color.FromArgb(30, 30, 30);
		lstEvents.ForeColor = Color.Orange;
		Controls.Add(lstEvents);
		lblStatus = new Label();
		lblStatus.ForeColor = Color.Orange;
		Label label = lblStatus;
		location = new Point(12, 440);
		label.Location = location;
		Label label2 = lblStatus;
		size = new Size(610, 24);
		label2.Size = size;
		Controls.Add(lblStatus);
	}

	private void TryLoadStoredCalDav()
	{
		string calendarUrl = null;
		string username = null;
		string password = null;
		if (LcarsCalendarBridge.TryLoadCalDavSettings(ref calendarUrl, ref username, ref password))
		{
			if (string.IsNullOrWhiteSpace(txtUrl.Text))
			{
				txtUrl.Text = calendarUrl;
			}
			if (string.IsNullOrWhiteSpace(txtUser.Text))
			{
				txtUser.Text = username;
			}
			if (password != null)
			{
				txtPassword.Text = password;
			}
		}
	}

	private void OnSync(object sender, EventArgs e)
	{
		lstEvents.Items.Clear();
		List<CalDavEvent> list = CalDavSyncService.FetchEvents(txtUrl.Text, txtUser.Text, txtPassword.Text);
		if (list == null)
		{
			lblStatus.Text = ((CalDavSyncService.LastError != null) ? CalDavSyncService.LastError : "SYNC FAILED").ToUpperInvariant();
			return;
		}
		int num = LcarsCalendarBridge.MergeCalDavPull(list);
		LcarsCalendarBridge.SaveCalDavSettings(txtUrl.Text, txtUser.Text, txtPassword.Text);
		foreach (CalDavEvent item in list)
		{
			ListBox.ObjectCollection items = lstEvents.Items;
			CalDavEvent calDavEvent = item;
			items.Add((calDavEvent.Summary != null) ? calDavEvent.Summary : item.Uid);
		}
		lblStatus.Text = list.Count + " REMOTE · " + num + " NEW IN LCARS CAL";
	}

	private void OnBidirectionalSync(object sender, EventArgs e)
	{
		LcarsCalendarBridge.SaveCalDavSettings(txtUrl.Text, txtUser.Text, txtPassword.Text);
		int num = LcarsCalendarBridge.CountCalDavDirtyEvents();
		int num2 = LcarsCalendarBridge.TrySyncStoredCalDavBidirectional();
		if (num2 < 0)
		{
			lblStatus.Text = "2-WAY SYNC FAILED";
			return;
		}
		RefreshLocalEventList();
		lblStatus.Text = num2 + " MERGED · PRE-PUSH " + LcarsCalendarBridge.LastCalDavPrePushCount + " · PUSH " + LcarsCalendarBridge.LastCalDavPushCount + " · DIRTY " + num + " · CAL " + LcarsCalendarBridge.CountDistinctCalendarNames() + " · RRULE " + LcarsCalendarBridge.CountEventsWithRecurrenceRule() + " · CAT " + LcarsCalendarBridge.CountEventsWithCategory() + ((LcarsCalendarBridge.LastCalDavPrimaryEventsTagged > 0) ? (" · TAG " + LcarsCalendarBridge.LastCalDavPrimaryEventsTagged) : "") + ((LcarsCalendarBridge.LastCalDavSecondaryEventsTagged > 0) ? (" · 2ND-TAG " + LcarsCalendarBridge.LastCalDavSecondaryEventsTagged) : "") + ((LcarsCalendarBridge.LastCalDavTaskEventsTagged > 0) ? (" · TASK-TAG " + LcarsCalendarBridge.LastCalDavTaskEventsTagged) : "") + ((LcarsCalendarBridge.LastCalDavPullMergeAdded > 0) ? (" · PULL " + LcarsCalendarBridge.LastCalDavPullMergeAdded) : "") + ((LcarsCalendarBridge.LastCalDavSecondaryPullMergeAdded > 0) ? (" · 2ND-PULL " + LcarsCalendarBridge.LastCalDavSecondaryPullMergeAdded) : "") + ((LcarsCalendarBridge.LastCalDavUrlLabelsStored > 0) ? (" · PROP " + LcarsCalendarBridge.LastCalDavUrlLabelsStored) : "") + ((LcarsCalendarBridge.LastCalDavPrimaryPullMergeAdded > 0) ? (" · 1ST-PULL " + LcarsCalendarBridge.LastCalDavPrimaryPullMergeAdded) : "") + ((LcarsCalendarBridge.LastCalDavTaskPullMergeAdded > 0) ? (" · TASK-PULL " + LcarsCalendarBridge.LastCalDavTaskPullMergeAdded) : "");
	}

	private void OnPush(object sender, EventArgs e)
	{
		int num = LcarsCalendarBridge.PushLocalEventsToCalDav(txtUrl.Text, txtUser.Text, txtPassword.Text);
		if (num <= 0 && !string.IsNullOrEmpty(CalDavSyncService.LastError))
		{
			lblStatus.Text = CalDavSyncService.LastError.ToUpperInvariant();
		}
		else
		{
			lblStatus.Text = num + " EVENT(S) PUSHED";
		}
	}

	private void OnOpenCalendar(object sender, EventArgs e)
	{
		lblStatus.Text = "CALENDAR MODE";
	}

	private void AddLabel(string text, int x, int y)
	{
		Label label = new Label();
		label.Text = text;
		label.ForeColor = Color.Orange;
		label.AutoSize = true;
		Point location = new Point(x, y);
		label.Location = location;
		Controls.Add(label);
	}

	private TextBox AddField(int x, int y, int w)
	{
		TextBox textBox = new TextBox();
		Point location = new Point(x, y);
		textBox.Location = location;
		Size size = new Size(w, 24);
		textBox.Size = size;
		textBox.BackColor = Color.FromArgb(40, 40, 40);
		textBox.ForeColor = Color.Orange;
		Controls.Add(textBox);
		return textBox;
	}
}
