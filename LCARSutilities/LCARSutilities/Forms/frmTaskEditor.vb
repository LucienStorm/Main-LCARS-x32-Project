Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports LCARS
Imports LCARS.Controls

    Public Class frmTaskEditor
        Inherits LCARSForm
        Private txtSummary As TextBox

        Private txtDue As TextBox

        Private txtAlarm As TextBox

        Private txtCategory As TextBox

        Private txtRecurrence As TextBox

        Private txtCalendar As TextBox

        Private cboPriority As ComboBox

        Private numPercent As NumericUpDown

        Private chkCompleted As CheckBox

        Public Property TaskResult As CalendarEvent

        Public Sub New()
            Me.New(Nothing)
        End Sub

        Public Sub New(existing As CalendarEvent)
            BackColor = Color.Black
            Dim clientSize As Size = New Size(520, 400)
            MyBase.ClientSize = clientSize
            StartPosition = FormStartPosition.CenterParent
            BuildUi()
            If existing IsNot Nothing Then
                Dim textBox = txtSummary
                Dim calendarEvent = existing
                textBox.Text = StripTaskDecorators(If(Not Equals(calendarEvent.Summary, Nothing), calendarEvent.Summary, ""))
                txtDue.Text = existing.StartUtc.ToLocalTime().ToString("g")
                txtAlarm.Text = existing.AlarmMinutesBefore.ToString()
                numPercent.Value = New Decimal(Math.Max(0, Math.Min(100, existing.TaskPercentComplete)))
                chkCompleted.Checked = existing.TaskCompleted
                Dim textBox2 = txtCategory
                calendarEvent = existing
                textBox2.Text = If(Not Equals(calendarEvent.Category, Nothing), calendarEvent.Category, "")
                Dim textBox3 = txtRecurrence
                calendarEvent = existing
                textBox3.Text = If(Not Equals(calendarEvent.RecurrenceRule, Nothing), calendarEvent.RecurrenceRule, "")
                Dim textBox4 = txtCalendar
                calendarEvent = existing
                textBox4.Text = If(Not Equals(calendarEvent.CalendarName, Nothing), calendarEvent.CalendarName, "")
                SelectPriority(existing.TaskPriority)
            Else
                txtDue.Text = Date.Now.AddDays(1.0).ToString("g")
                txtAlarm.Text = "15"
                cboPriority.SelectedIndex = 3
            End If
        End Sub

        Private Sub BuildUi()
            AddLabel("TASK TITLE", 12, 12)
            txtSummary = AddField(12, 32, 490)
            AddLabel("DUE (LOCAL)", 12, 64)
            txtDue = AddField(12, 84, 240)
            AddLabel("REMINDER MIN", 260, 64)
            txtAlarm = AddField(260, 84, 242)
            AddLabel("CATEGORY", 12, 168)
            txtCategory = AddField(12, 188, 240)
            AddLabel("RRULE (FREQ=...)", 260, 168)
            txtRecurrence = AddField(260, 188, 242)
            AddLabel("CALENDAR NAME", 12, 216)
            txtCalendar = AddField(12, 236, 490)
            AddLabel("PRIORITY (0=HIGH 9=LOW)", 12, 116)
            cboPriority = New ComboBox()
            cboPriority.DropDownStyle = ComboBoxStyle.DropDownList
            Dim comboBox = cboPriority
            Dim location As Point = New Point(12, 136)
            comboBox.Location = location
            Dim comboBox2 = cboPriority
            Dim size As Size = New Size(240, 24)
            comboBox2.Size = size
            cboPriority.BackColor = Color.FromArgb(30, 30, 30)
            cboPriority.ForeColor = Color.Orange
            Dim num = 0
            Do
                cboPriority.Items.Add(num.ToString())
                num = num + 1
            Loop While num <= 9
            Controls.Add(cboPriority)
            AddLabel("PERCENT DONE", 260, 116)
            numPercent = New NumericUpDown()
            Dim numericUpDown = numPercent
            location = New Point(260, 136)
            numericUpDown.Location = location
            Dim numericUpDown2 = numPercent
            size = New Size(80, 24)
            numericUpDown2.Size = size
            numPercent.Minimum = 0D
            numPercent.Maximum = 100D
            numPercent.BackColor = Color.FromArgb(30, 30, 30)
            numPercent.ForeColor = Color.Orange
            Controls.Add(numPercent)
            chkCompleted = New CheckBox()
            chkCompleted.Text = "COMPLETED"
            chkCompleted.ForeColor = Color.Orange
            Dim checkBox = chkCompleted
            location = New Point(360, 136)
            checkBox.Location = location
            chkCompleted.AutoSize = True
            Controls.Add(chkCompleted)
            Dim flatButton As FlatButton = New FlatButton()
            flatButton.ButtonText = "SAVE"
            flatButton.Text = "SAVE"
            flatButton.Color = LCARScolorStyles.PrimaryFunction
            size = New Size(72, 28)
            flatButton.Size = size
            location = New Point(320, 340)
            flatButton.Location = location
            AddHandler flatButton.Click, AddressOf OnSave
            Controls.Add(flatButton)
            Dim flatButton2 As FlatButton = New FlatButton()
            flatButton2.ButtonText = "CANCEL"
            flatButton2.Text = "CANCEL"
            flatButton2.Color = LCARScolorStyles.NavigationFunction
            size = New Size(80, 28)
            flatButton2.Size = size
            location = New Point(400, 340)
            flatButton2.Location = location
            AddHandler flatButton2.Click, Sub(a0, a1)
                                              Call (Sub() DialogResult = DialogResult.Cancel)()
                                          End Sub
            Controls.Add(flatButton2)
        End Sub

        Private Sub OnSave(sender As Object, e As EventArgs)
            Dim text = txtSummary.Text
            Dim text2 As String = If(Not Equals(text, Nothing), text, "").Trim()
            Dim result As Date = Nothing
            If text2.Length <> 0 Then
                Dim startUtc = Date.UtcNow.AddDays(1.0)

                If Date.TryParse(txtDue.Text, result) Then
                    startUtc = result.ToUniversalTime()
                End If
                Dim result2 = 0
                text = txtAlarm.Text
                Call Integer.TryParse(If(Not Equals(text, Nothing), text, "").Trim(), result2)
                Dim taskPriority = 5
                If cboPriority.SelectedIndex >= 0 Then
                    taskPriority = cboPriority.SelectedIndex
                End If
                Dim text3 = text2
                If text3.IndexOf("[TASK]", StringComparison.OrdinalIgnoreCase) < 0 Then
                    text3 = "[TASK] " & text3
                End If
                If chkCompleted.Checked AndAlso text3.IndexOf("[DONE]", StringComparison.OrdinalIgnoreCase) < 0 Then
                    text3 = "[DONE] " & text3
                End If
                Dim calendarEvent As CalendarEvent = New CalendarEvent With {
        .Summary = text3,
        .StartUtc = startUtc,
        .EndUtc = startUtc.AddHours(1.0),
        .AlarmMinutesBefore = Math.Max(0, result2),
        .TaskCompleted = chkCompleted.Checked,
        .TaskPriority = taskPriority,
        .TaskPercentComplete = Convert.ToInt32(numPercent.Value)
    }
                text = txtCategory.Text
                calendarEvent.Category = If(Not Equals(text, Nothing), text, "").Trim()
                Dim text4 = txtRecurrence.Text
                calendarEvent.RecurrenceRule = If(Not Equals(text4, Nothing), text4, "").Trim()
                Dim text5 = txtCalendar.Text
                calendarEvent.CalendarName = If(Not Equals(text5, Nothing), text5, "").Trim()
                TaskResult = calendarEvent
                DialogResult = DialogResult.OK
            End If
        End Sub

        Private Shared Function StripTaskDecorators(summary As String) As String
            Dim text As String = If(Not Equals(summary, Nothing), summary, "").Trim()
            If text.StartsWith("[DONE]", StringComparison.OrdinalIgnoreCase) Then
                text = text.Substring(6).TrimStart()
            End If
            If text.StartsWith("[TASK]", StringComparison.OrdinalIgnoreCase) Then
                text = text.Substring(6).TrimStart()
            End If
            Return text
        End Function

        Private Sub SelectPriority(priority As Integer)
            Dim num = Math.Max(0, Math.Min(9, priority))
            If num < cboPriority.Items.Count Then
                cboPriority.SelectedIndex = num
            End If
        End Sub

        Private Sub AddLabel(text As String, x As Integer, y As Integer)
            Dim label As Label = New Label()
            label.Text = text
            label.ForeColor = Color.Orange
            Dim location As Point = New Point(x, y)
            label.Location = location
            label.AutoSize = True
            Controls.Add(label)
        End Sub

        Private Function AddField(x As Integer, y As Integer, w As Integer) As TextBox
            Dim textBox As TextBox = New TextBox()
            Dim location As Point = New Point(x, y)
            textBox.Location = location
            Dim size As Size = New Size(w, 22)
            textBox.Size = size
            textBox.BackColor = Color.FromArgb(30, 30, 30)
            textBox.ForeColor = Color.Orange
            Controls.Add(textBox)
            Return textBox
        End Function
    End Class
