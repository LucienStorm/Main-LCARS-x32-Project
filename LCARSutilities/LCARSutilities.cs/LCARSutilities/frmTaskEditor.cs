using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using LCARS;
using LCARS.Controls;

namespace LCARSutilities;

public class frmTaskEditor : LCARSForm
{
	private TextBox txtSummary;

	private TextBox txtDue;

	private TextBox txtAlarm;

	private TextBox txtCategory;

	private TextBox txtRecurrence;

	private TextBox txtCalendar;

	private ComboBox cboPriority;

	private NumericUpDown numPercent;

	private CheckBox chkCompleted;

	public CalendarEvent TaskResult
	{
		[DebuggerNonUserCode]
		get;
		[DebuggerNonUserCode]
		set;
	}

	public frmTaskEditor()
		: this(null)
	{
	}

	public frmTaskEditor(CalendarEvent existing)
	{
		BackColor = Color.Black;
		Size clientSize = new Size(520, 400);
		ClientSize = clientSize;
		StartPosition = FormStartPosition.CenterParent;
		BuildUi();
		if (existing != null)
		{
			TextBox textBox = txtSummary;
			CalendarEvent calendarEvent = existing;
			textBox.Text = StripTaskDecorators((calendarEvent.Summary != null) ? calendarEvent.Summary : "");
			txtDue.Text = existing.StartUtc.ToLocalTime().ToString("g");
			txtAlarm.Text = existing.AlarmMinutesBefore.ToString();
			numPercent.Value = new decimal(Math.Max(0, Math.Min(100, existing.TaskPercentComplete)));
			chkCompleted.Checked = existing.TaskCompleted;
			TextBox textBox2 = txtCategory;
			calendarEvent = existing;
			textBox2.Text = ((calendarEvent.Category != null) ? calendarEvent.Category : "");
			TextBox textBox3 = txtRecurrence;
			calendarEvent = existing;
			textBox3.Text = ((calendarEvent.RecurrenceRule != null) ? calendarEvent.RecurrenceRule : "");
			TextBox textBox4 = txtCalendar;
			calendarEvent = existing;
			textBox4.Text = ((calendarEvent.CalendarName != null) ? calendarEvent.CalendarName : "");
			SelectPriority(existing.TaskPriority);
		}
		else
		{
			txtDue.Text = DateTime.Now.AddDays(1.0).ToString("g");
			txtAlarm.Text = "15";
			cboPriority.SelectedIndex = 3;
		}
	}

	private void BuildUi()
	{
		AddLabel("TASK TITLE", 12, 12);
		txtSummary = AddField(12, 32, 490);
		AddLabel("DUE (LOCAL)", 12, 64);
		txtDue = AddField(12, 84, 240);
		AddLabel("REMINDER MIN", 260, 64);
		txtAlarm = AddField(260, 84, 242);
		AddLabel("CATEGORY", 12, 168);
		txtCategory = AddField(12, 188, 240);
		AddLabel("RRULE (FREQ=...)", 260, 168);
		txtRecurrence = AddField(260, 188, 242);
		AddLabel("CALENDAR NAME", 12, 216);
		txtCalendar = AddField(12, 236, 490);
		AddLabel("PRIORITY (0=HIGH 9=LOW)", 12, 116);
		cboPriority = new ComboBox();
		cboPriority.DropDownStyle = ComboBoxStyle.DropDownList;
		ComboBox comboBox = cboPriority;
		Point location = new Point(12, 136);
		comboBox.Location = location;
		ComboBox comboBox2 = cboPriority;
		Size size = new Size(240, 24);
		comboBox2.Size = size;
		cboPriority.BackColor = Color.FromArgb(30, 30, 30);
		cboPriority.ForeColor = Color.Orange;
		int num = 0;
		do
		{
			cboPriority.Items.Add(num.ToString());
			num = checked(num + 1);
		}
		while (num <= 9);
		Controls.Add(cboPriority);
		AddLabel("PERCENT DONE", 260, 116);
		numPercent = new NumericUpDown();
		NumericUpDown numericUpDown = numPercent;
		location = new Point(260, 136);
		numericUpDown.Location = location;
		NumericUpDown numericUpDown2 = numPercent;
		size = new Size(80, 24);
		numericUpDown2.Size = size;
		numPercent.Minimum = 0m;
		numPercent.Maximum = 100m;
		numPercent.BackColor = Color.FromArgb(30, 30, 30);
		numPercent.ForeColor = Color.Orange;
		Controls.Add(numPercent);
		chkCompleted = new CheckBox();
		chkCompleted.Text = "COMPLETED";
		chkCompleted.ForeColor = Color.Orange;
		CheckBox checkBox = chkCompleted;
		location = new Point(360, 136);
		checkBox.Location = location;
		chkCompleted.AutoSize = true;
		Controls.Add(chkCompleted);
		FlatButton flatButton = new FlatButton();
		flatButton.ButtonText = "SAVE";
		flatButton.Text = "SAVE";
		flatButton.Color = LCARScolorStyles.PrimaryFunction;
		size = new Size(72, 28);
		flatButton.Size = size;
		location = new Point(320, 340);
		flatButton.Location = location;
		flatButton.Click += OnSave;
		Controls.Add(flatButton);
		FlatButton flatButton2 = new FlatButton();
		flatButton2.ButtonText = "CANCEL";
		flatButton2.Text = "CANCEL";
		flatButton2.Color = LCARScolorStyles.NavigationFunction;
		size = new Size(80, 28);
		flatButton2.Size = size;
		location = new Point(400, 340);
		flatButton2.Location = location;
		flatButton2.Click += [SpecialName] [DebuggerStepThrough] (object a0, EventArgs a1) =>
		{
			((VB_0024AnonymousDelegate_2)([SpecialName] () =>
			{
				DialogResult = DialogResult.Cancel;
			}))();
		};
		Controls.Add(flatButton2);
	}

	private void OnSave(object sender, EventArgs e)
	{
		string text = txtSummary.Text;
		string text2 = ((text != null) ? text : "").Trim();
		if (text2.Length != 0)
		{
			DateTime startUtc = DateTime.UtcNow.AddDays(1.0);
			if (DateTime.TryParse(txtDue.Text, out var result))
			{
				startUtc = result.ToUniversalTime();
			}
			int result2 = 0;
			text = txtAlarm.Text;
			int.TryParse(((text != null) ? text : "").Trim(), out result2);
			int taskPriority = 5;
			if (cboPriority.SelectedIndex >= 0)
			{
				taskPriority = cboPriority.SelectedIndex;
			}
			string text3 = text2;
			if (text3.IndexOf("[TASK]", StringComparison.OrdinalIgnoreCase) < 0)
			{
				text3 = "[TASK] " + text3;
			}
			if (chkCompleted.Checked && text3.IndexOf("[DONE]", StringComparison.OrdinalIgnoreCase) < 0)
			{
				text3 = "[DONE] " + text3;
			}
			CalendarEvent calendarEvent = new CalendarEvent
			{
				Summary = text3,
				StartUtc = startUtc,
				EndUtc = startUtc.AddHours(1.0),
				AlarmMinutesBefore = Math.Max(0, result2),
				TaskCompleted = chkCompleted.Checked,
				TaskPriority = taskPriority,
				TaskPercentComplete = Convert.ToInt32(numPercent.Value)
			};
			text = txtCategory.Text;
			calendarEvent.Category = ((text != null) ? text : "").Trim();
			string text4 = txtRecurrence.Text;
			calendarEvent.RecurrenceRule = ((text4 != null) ? text4 : "").Trim();
			string text5 = txtCalendar.Text;
			calendarEvent.CalendarName = ((text5 != null) ? text5 : "").Trim();
			TaskResult = calendarEvent;
			DialogResult = DialogResult.OK;
		}
	}

	private static string StripTaskDecorators(string summary)
	{
		string text = ((summary != null) ? summary : "").Trim();
		if (text.StartsWith("[DONE]", StringComparison.OrdinalIgnoreCase))
		{
			text = text.Substring(6).TrimStart();
		}
		if (text.StartsWith("[TASK]", StringComparison.OrdinalIgnoreCase))
		{
			text = text.Substring(6).TrimStart();
		}
		return text;
	}

	private void SelectPriority(int priority)
	{
		int num = Math.Max(0, Math.Min(9, priority));
		if (num < cboPriority.Items.Count)
		{
			cboPriority.SelectedIndex = num;
		}
	}

	private void AddLabel(string text, int x, int y)
	{
		Label label = new Label();
		label.Text = text;
		label.ForeColor = Color.Orange;
		Point location = new Point(x, y);
		label.Location = location;
		label.AutoSize = true;
		Controls.Add(label);
	}

	private TextBox AddField(int x, int y, int w)
	{
		TextBox textBox = new TextBox();
		Point location = new Point(x, y);
		textBox.Location = location;
		Size size = new Size(w, 22);
		textBox.Size = size;
		textBox.BackColor = Color.FromArgb(30, 30, 30);
		textBox.ForeColor = Color.Orange;
		Controls.Add(textBox);
		return textBox;
	}
}
