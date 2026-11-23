' LCARSutilities/LCARSutilities/Forms/frmTaskEditor.vb
Option Strict On
Option Explicit On
Imports System.Drawing
Imports System.Windows.Forms
Imports LCARS.Controls
''' <summary>Lightning-style task editor.</summary>
Public Class frmTaskEditor
Inherits LCARS.LCARSForm
Public Property TaskResult As CalendarEvent
Private txtSummary As TextBox
Private txtDue As TextBox
Private txtAlarm As TextBox
Private txtCategory As TextBox
Private txtRecurrence As TextBox
Private txtCalendar As TextBox
Private cboPriority As ComboBox
Private numPercent As NumericUpDown
Private chkCompleted As CheckBox
Public Sub New()
Me.New(Nothing)
End Sub
Public Sub New(ByVal existing As CalendarEvent) BackColor = Color.Black ClientSize = New Size(520, 400) StartPosition = FormStartPosition.CenterParent BuildUi()
If existing IsNot Nothing Then txtSummary.Text = StripTaskDecorators(If(existing.Summary, ""))
txtDue.Text = existing.StartUtc.ToLocalTime().ToString("g")
txtAlarm.Text = existing.AlarmMinutesBefore.ToString()
numPercent.Value = Math.Max(0, Math.Min(100, existing.TaskPercentComplete))
chkCompleted.Checked = existing.TaskCompleted txtCategory.Text = If(existing.Category, "")
txtRecurrence.Text = If(existing.RecurrenceRule, "")
txtCalendar.Text = If(existing.CalendarName, "") SelectPriority(existing.TaskPriority)
Else txtDue.Text = DateTime.Now.AddDays(1).ToString("g")
txtAlarm.Text = "15" cboPriority.SelectedIndex = 3
End If
End Sub
Private Sub BuildUi() AddLabel("TASK TITLE", 12, 12) txtSummary = AddField(12, 32, 490) AddLabel("DUE (LOCAL)", 12, 64) txtDue = AddField(12, 84, 240) AddLabel("REMINDER MIN", 260, 64) txtAlarm = AddField(260, 84, 242) AddLabel("CATEGORY", 12, 168) txtCategory = AddField(12, 188, 240) AddLabel("RRULE (FREQ=...)", 260, 168) txtRecurrence = AddField(260, 188, 242) AddLabel("CALENDAR NAME", 12, 216) txtCalendar = AddField(12, 236, 490) AddLabel("PRIORITY (0=HIGH 9=LOW)", 12, 116) cboPriority = New ComboBox()
cboPriority.DropDownStyle = ComboBoxStyle.DropDownList cboPriority.Location = New Point(12, 136)
cboPriority.Size = New Size(240, 24)
cboPriority.BackColor = Color.FromArgb(30, 30, 30)
cboPriority.ForeColor = Color.Orange
For p As Integer = 0 To 9 cboPriority.Items.Add(p.ToString())
Next Controls.Add(cboPriority) AddLabel("PERCENT DONE", 260, 116) numPercent = New NumericUpDown()
numPercent.Location = New Point(260, 136)
numPercent.Size = New Size(80, 24)
numPercent.Minimum = 0 numPercent.Maximum = 100 numPercent.BackColor = Color.FromArgb(30, 30, 30)
numPercent.ForeColor = Color.Orange Controls.Add(numPercent) chkCompleted = New CheckBox()
chkCompleted.Text = "COMPLETED" chkCompleted.ForeColor = Color.Orange chkCompleted.Location = New Point(360, 136)
chkCompleted.AutoSize = True Controls.Add(chkCompleted)
Dim fbOk As New FlatButton()
fbOk.ButtonText = "SAVE" fbOk.Text = "SAVE" fbOk.Color = LCARS.LCARScolorStyles.PrimaryFunction fbOk.Size = New Size(72, 28)
fbOk.Location = New Point(320, 340)
AddHandler fbOk.Click, AddressOf OnSave Controls.Add(fbOk)
Dim fbCancel As New FlatButton()
fbCancel.ButtonText = "CANCEL" fbCancel.Text = "CANCEL" fbCancel.Color = LCARS.LCARScolorStyles.NavigationFunction fbCancel.Size = New Size(80, 28)
fbCancel.Location = New Point(400, 340)
AddHandler fbCancel.Click, Sub() DialogResult = DialogResult.Cancel Controls.Add(fbCancel)
End Sub
Private Sub OnSave(ByVal sender As Object, ByVal e As EventArgs)
Dim title As String = If(txtSummary.Text, "").Trim()
If title.Length = 0 Then Return
Dim dueUtc As DateTime = DateTime.UtcNow.AddDays(1)
Dim parsed As DateTime
If DateTime.TryParse(txtDue.Text, parsed) Then dueUtc = parsed.ToUniversalTime()
Dim alarm As Integer = 0 Integer.TryParse(If(txtAlarm.Text, "").Trim(), alarm)
Dim priority As Integer = 5
If cboPriority.SelectedIndex >= 0 Then priority = cboPriority.SelectedIndex
Dim summary As String = title
If summary.IndexOf("[TASK]", StringComparison.OrdinalIgnoreCase) < 0 Then summary = "[TASK] " & summary
End If
If chkCompleted.Checked AndAlso summary.IndexOf("[DONE]", StringComparison.OrdinalIgnoreCase) < 0 Then summary = "[DONE] " & summary
End If TaskResult = New CalendarEvent() With { .Summary = summary, .StartUtc = dueUtc, .EndUtc = dueUtc.AddHours(1), .AlarmMinutesBefore = Math.Max(0, alarm), .TaskCompleted = chkCompleted.Checked, .TaskPriority = priority, .TaskPercentComplete = CInt(numPercent.Value), .Category = If(txtCategory.Text, "").Trim(), .RecurrenceRule = If(txtRecurrence.Text, "").Trim(), .CalendarName = If(txtCalendar.Text, "").Trim() } DialogResult = DialogResult.OK
End Sub
Private Shared Function StripTaskDecorators(ByVal summary As String) As String
Dim s As String = If(summary, "").Trim()
If s.StartsWith("[DONE]", StringComparison.OrdinalIgnoreCase) Then s = s.Substring(6).TrimStart()
If s.StartsWith("[TASK]", StringComparison.OrdinalIgnoreCase) Then s = s.Substring(6).TrimStart()
Return s
End Function
Private Sub SelectPriority(ByVal priority As Integer)
Dim idx As Integer = Math.Max(0, Math.Min(9, priority))
If idx < cboPriority.Items.Count Then cboPriority.SelectedIndex = idx
End Sub
Private Sub AddLabel(ByVal text As String, ByVal x As Integer, ByVal y As Integer)
Dim lbl As New Label()
lbl.Text = text lbl.ForeColor = Color.Orange lbl.Location = New Point(x, y)
lbl.AutoSize = True Controls.Add(lbl)
End Sub
Private Function AddField(ByVal x As Integer, ByVal y As Integer, ByVal w As Integer) As TextBox
Dim tb As New TextBox()
tb.Location = New Point(x, y)
tb.Size = New Size(w, 22)
tb.BackColor = Color.FromArgb(30, 30, 30)
tb.ForeColor = Color.Orange Controls.Add(tb)
Return tb
End Function
End Class
