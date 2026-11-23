Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports LCARS
Imports LCARS.Controls
    Public Class frmCalDavSync
        Inherits LCARSForm
        Private txtUrl As TextBox

        Private txtUser As TextBox

        Private txtPassword As TextBox

        Private lstEvents As ListBox

        Private lblStatus As Label

        Public Sub New()
            CType(CObj(Me), Form).BackColor = Color.Black
            Dim clientSize As Size = New Size(640, 480)
            CType(CObj(Me), Form).ClientSize = clientSize
            CType(CObj(Me), Form).StartPosition = FormStartPosition.CenterParent
            BuildUi()
            TryLoadStoredCalDav()
            RefreshLocalEventList()
        End Sub

        Private Sub RefreshLocalEventList()
            lstEvents.Items.Clear()
            For Each item In LcarsCalendarBridge.LoadEvents()
                If item IsNot Nothing Then
                    Dim text As String = (If(String.IsNullOrWhiteSpace(item.CalendarName), "", (item.CalendarName.Trim() & " · ")))
                    Dim text2 = If(String.IsNullOrWhiteSpace(item.RecurrenceRule), "", " RRULE")
                    Dim text3 = If(String.IsNullOrWhiteSpace(item.Category), "", " [" & item.Category & "]")
                    Dim items = lstEvents.Items
                    Dim sharedCalendarEvent = item
                    items.Add(text & If(Not Equals(sharedCalendarEvent.Summary, Nothing), sharedCalendarEvent.Summary, item.Uid) & text2 & text3)
                End If
            Next
            Dim num As Integer = LcarsCalendarBridge.CountCalDavDirtyEvents()
            lblStatus.Text = lstEvents.Items.Count.ToString() & " LOCAL · DIRTY " & num.ToString() & " · CAL " & LcarsCalendarBridge.CountDistinctCalendarNames().ToString() & " · EWS-LINK " & LcarsCalendarBridge.CountEventsLinkedToEws().ToString()
        End Sub

        Private Sub BuildUi()
            AddLabel("CALENDAR URL", 12, 12)
            txtUrl = AddField(12, 32, 610)
            AddLabel("USERNAME", 12, 64)
            txtUser = AddField(12, 84, 610)
            AddLabel("PASSWORD", 12, 116)
            txtPassword = AddField(12, 136, 610)
            txtPassword.UseSystemPasswordChar = True
            Dim val As FlatButton = New FlatButton()
            CType(val, LCARSbuttonClass).ButtonText = "SYNC"
            CType(val, LCARSbuttonClass).Text = "SYNC"
            CType(val, LCARSbuttonClass).Color = CType(6, LCARScolorStyles)
            Dim size As Size = New Size(90, 28)
            CType(CObj(val), Control).Size = size
            Dim location As Point = New Point(12, 168)
            CType(CObj(val), Control).Location = location
            AddHandler CType(CObj(val), Control).Click, AddressOf OnSync
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val), Control))
            Dim val2 As FlatButton = New FlatButton()
            CType(val2, LCARSbuttonClass).ButtonText = "PUSH"
            CType(val2, LCARSbuttonClass).Text = "PUSH"
            CType(val2, LCARSbuttonClass).Color = CType(1, LCARScolorStyles)
            size = New Size(72, 28)
            CType(CObj(val2), Control).Size = size
            location = New Point(108, 168)
            CType(CObj(val2), Control).Location = location
            AddHandler CType(CObj(val2), Control).Click, AddressOf OnPush
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val2), Control))
            Dim val3 As FlatButton = New FlatButton()
            CType(val3, LCARSbuttonClass).ButtonText = "2-WAY"
            CType(val3, LCARSbuttonClass).Text = "2-WAY"
            CType(val3, LCARSbuttonClass).Color = CType(3, LCARScolorStyles)
            size = New Size(72, 28)
            CType(CObj(val3), Control).Size = size
            location = New Point(268, 168)
            CType(CObj(val3), Control).Location = location
            AddHandler CType(CObj(val3), Control).Click, AddressOf OnBidirectionalSync
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val3), Control))
            Dim val4 As FlatButton = New FlatButton()
            CType(val4, LCARSbuttonClass).ButtonText = "CAL UI"
            CType(val4, LCARSbuttonClass).Text = "CAL UI"
            CType(val4, LCARSbuttonClass).Color = CType(2, LCARScolorStyles)
            size = New Size(72, 28)
            CType(CObj(val4), Control).Size = size
            location = New Point(188, 168)
            CType(CObj(val4), Control).Location = location
            AddHandler CType(CObj(val4), Control).Click, AddressOf OnOpenCalendar
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val4), Control))
            lstEvents = New ListBox()
            Dim listBox = lstEvents
            location = New Point(12, 204)
            listBox.Location = location
            Dim listBox2 = lstEvents
            size = New Size(610, 220)
            listBox2.Size = size
            lstEvents.BackColor = Color.FromArgb(30, 30, 30)
            lstEvents.ForeColor = Color.Orange
            CType(CObj(Me), Control).Controls.Add(lstEvents)
            lblStatus = New Label()
            lblStatus.ForeColor = Color.Orange
            Dim label = lblStatus
            location = New Point(12, 440)
            label.Location = location
            Dim label2 = lblStatus
            size = New Size(610, 24)
            label2.Size = size
            CType(CObj(Me), Control).Controls.Add(lblStatus)
        End Sub

        Private Sub TryLoadStoredCalDav()
            Dim calendarUrl As String = Nothing
            Dim username As String = Nothing
            Dim password As String = Nothing
            If LcarsCalendarBridge.TryLoadCalDavSettings(calendarUrl, username, password) Then
                If String.IsNullOrWhiteSpace(txtUrl.Text) Then
                    txtUrl.Text = calendarUrl
                End If
                If String.IsNullOrWhiteSpace(txtUser.Text) Then
                    txtUser.Text = username
                End If
                If Not Equals(password, Nothing) Then
                    txtPassword.Text = password
                End If
            End If
        End Sub

        Private Sub OnSync(sender As Object, e As EventArgs)
            lstEvents.Items.Clear()
            Dim list = CalDavSyncService.FetchEvents(txtUrl.Text, txtUser.Text, txtPassword.Text)
            If list Is Nothing Then
                lblStatus.Text = If(Not Equals(CalDavSyncService.LastError, Nothing), CalDavSyncService.LastError, "SYNC FAILED").ToUpperInvariant()
                Return
            End If
            Dim num = LcarsCalendarBridge.MergeCalDavPull(list)
            LcarsCalendarBridge.SaveCalDavSettings(txtUrl.Text, txtUser.Text, txtPassword.Text)
            For Each item In list
                Dim items = lstEvents.Items
                Dim calDavEvent = item
                items.Add(If(Not Equals(calDavEvent.Summary, Nothing), calDavEvent.Summary, item.Uid))
            Next
            lblStatus.Text = list.Count.ToString() & " REMOTE · " & num.ToString() & " NEW IN LCARS CAL"
        End Sub

        Private Sub OnBidirectionalSync(sender As Object, e As EventArgs)
            LcarsCalendarBridge.SaveCalDavSettings(txtUrl.Text, txtUser.Text, txtPassword.Text)
            Dim num As Integer = LcarsCalendarBridge.CountCalDavDirtyEvents()
            Dim num2 As Integer = LcarsCalendarBridge.TrySyncStoredCalDavBidirectional()
            If num2 < 0 Then
                lblStatus.Text = "2-WAY SYNC FAILED"
                Return
            End If
            RefreshLocalEventList()
            lblStatus.Text = num2.ToString() & " MERGED · PRE-PUSH " & LcarsCalendarBridge.LastCalDavPrePushCount.ToString() & " · PUSH " & LcarsCalendarBridge.LastCalDavPushCount.ToString() & " · DIRTY " & num.ToString() & " · CAL " & LcarsCalendarBridge.CountDistinctCalendarNames().ToString() & " · RRULE " & LcarsCalendarBridge.CountEventsWithRecurrenceRule().ToString() & " · CAT " & LcarsCalendarBridge.CountEventsWithCategory().ToString() & (If(LcarsCalendarBridge.LastCalDavPrimaryEventsTagged > 0, (" · TAG " & LcarsCalendarBridge.LastCalDavPrimaryEventsTagged.ToString()), "")) & (If(LcarsCalendarBridge.LastCalDavSecondaryEventsTagged > 0, (" · 2ND-TAG " & LcarsCalendarBridge.LastCalDavSecondaryEventsTagged.ToString()), "")) & (If(LcarsCalendarBridge.LastCalDavTaskEventsTagged > 0, (" · TASK-TAG " & LcarsCalendarBridge.LastCalDavTaskEventsTagged.ToString()), "")) & (If(LcarsCalendarBridge.LastCalDavPullMergeAdded > 0, (" · PULL " & LcarsCalendarBridge.LastCalDavPullMergeAdded.ToString()), "")) & (If(LcarsCalendarBridge.LastCalDavSecondaryPullMergeAdded > 0, (" · 2ND-PULL " & LcarsCalendarBridge.LastCalDavSecondaryPullMergeAdded.ToString()), "")) & (If(LcarsCalendarBridge.LastCalDavUrlLabelsStored > 0, (" · PROP " & LcarsCalendarBridge.LastCalDavUrlLabelsStored.ToString()), "")) & (If(LcarsCalendarBridge.LastCalDavPrimaryPullMergeAdded > 0, (" · 1ST-PULL " & LcarsCalendarBridge.LastCalDavPrimaryPullMergeAdded.ToString()), "")) & (If(LcarsCalendarBridge.LastCalDavTaskPullMergeAdded > 0, (" · TASK-PULL " & LcarsCalendarBridge.LastCalDavTaskPullMergeAdded.ToString()), ""))
        End Sub

        Private Sub OnPush(sender As Object, e As EventArgs)
            Dim num = LcarsCalendarBridge.PushLocalEventsToCalDav(txtUrl.Text, txtUser.Text, txtPassword.Text)
            If num <= 0 AndAlso Not String.IsNullOrEmpty(CalDavSyncService.LastError) Then
                lblStatus.Text = CalDavSyncService.LastError.ToUpperInvariant()
            Else
                lblStatus.Text = num.ToString() & " EVENT(S) PUSHED"
            End If
        End Sub

        Private Sub OnOpenCalendar(sender As Object, e As EventArgs)
            lblStatus.Text = "CALENDAR MODE"
        End Sub

        Private Sub AddLabel(text As String, x As Integer, y As Integer)
            Dim label As Label = New Label()
            label.Text = text
            label.ForeColor = Color.Orange
            label.AutoSize = True
            Dim location As Point = New Point(x, y)
            label.Location = location
            CType(CObj(Me), Control).Controls.Add(label)
        End Sub

        Private Function AddField(x As Integer, y As Integer, w As Integer) As TextBox
            Dim textBox As TextBox = New TextBox()
            Dim location As Point = New Point(x, y)
            textBox.Location = location
            Dim size As Size = New Size(w, 24)
            textBox.Size = size
            textBox.BackColor = Color.FromArgb(40, 40, 40)
            textBox.ForeColor = Color.Orange
            CType(CObj(Me), Control).Controls.Add(textBox)
            Return textBox
        End Function
    End Class

