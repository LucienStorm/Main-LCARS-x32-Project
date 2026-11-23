Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Drawing
Imports System.Threading
Imports System.Windows.Forms
Imports LCARS
Imports LCARS.Controls
    Public Class frmEwsSync
        Inherits LCARSForm
        Private Enum EwsListMode
            Mail
            Calendar
            Contacts
        End Enum

        Private Class DevicePollArgs
            Public Tenant As String

            Public ClientId As String

            Public DeviceCode As String

            Public IntervalSeconds As Integer

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Private txtUrl As TextBox

        Private txtUser As TextBox

        Private txtPassword As TextBox

        Private txtOAuth As TextBox

        Private txtTenant As TextBox

        Private txtClientId As TextBox

        Private lblDeviceHint As Label

        Private ReadOnly oauthPollWorker As BackgroundWorker

        Private lstItems As ListBox

        Private txtBody As TextBox

        Private txtSendTo As TextBox

        Private txtSendSubject As TextBox

        Private txtSendBody As TextBox

        Private lblStatus As Label

        Private summaries As List(Of EwsMailService.EwsMailSummary)

        Private calendarSummaries As List(Of EwsMailService.EwsCalendarSummary)

        Private resolvedContacts As List(Of EwsMailService.EwsResolvedContact)

        Private listMode As EwsListMode

        Public Sub New()
            listMode = EwsListMode.Mail
            CType(CObj(Me), Form).BackColor = Color.Black
            Dim clientSize As Size = New Size(640, 688)
            CType(CObj(Me), Form).ClientSize = clientSize
            CType(CObj(Me), Form).StartPosition = FormStartPosition.CenterParent
            summaries = New List(Of EwsMailService.EwsMailSummary)()
            calendarSummaries = New List(Of EwsMailService.EwsCalendarSummary)()
            resolvedContacts = New List(Of EwsMailService.EwsResolvedContact)()
            oauthPollWorker = New BackgroundWorker()
            oauthPollWorker.WorkerSupportsCancellation = True
            AddHandler oauthPollWorker.DoWork, AddressOf OAuthPoll_DoWork
            AddHandler oauthPollWorker.RunWorkerCompleted, AddressOf OAuthPoll_Completed
            BuildUi()
            TryLoadStoredOAuth()
            TryLoadOAuthAppSettings()
            Dim num As Integer = EwsGalSyncStore.TryRefreshLastGalMergedCountFromDisk()
            If num > 0 Then
                Dim t As Date = EwsGalSyncStore.TryGetLastDeepSyncUtc()
                Dim text = ""
                If Date.Compare(t, Date.MinValue) <> 0 Then
                    text = " · DEEP " & t.ToLocalTime().ToString("yyyy-MM-dd")
                End If
                lblStatus.Text = "LAST GAL MERGE: " & num.ToString() & " CONTACT(S) (PARTIAL GAL)" & text
            End If
        End Sub

        Private Sub BuildUi()
            AddLabel("EWS URL", 12, 12)
            txtUrl = AddField(12, 32, 610)
            txtUrl.Text = "https://outlook.office365.com/EWS/Exchange.asmx"
            AddLabel("USERNAME", 12, 64)
            txtUser = AddField(12, 84, 610)
            AddLabel("PASSWORD", 12, 116)
            txtPassword = AddField(12, 136, 610)
            txtPassword.UseSystemPasswordChar = True
            AddLabel("OAUTH TENANT", 12, 168)
            txtTenant = AddField(12, 188, 280)
            txtTenant.Text = "organizations"
            AddLabel("CLIENT ID", 300, 168)
            txtClientId = AddField(300, 188, 322)
            txtClientId.Text = "94933e60-ee10-4250-a050-3efd588b0718"
            AddLabel("OAUTH ACCESS TOKEN", 12, 216)
            txtOAuth = AddField(12, 236, 500)
            txtOAuth.UseSystemPasswordChar = True
            Dim val As FlatButton = New FlatButton()
            CType(val, LCARSbuttonClass).ButtonText = "DEVICE"
            CType(val, LCARSbuttonClass).Text = "DEVICE"
            CType(val, LCARSbuttonClass).Color = CType(4, LCARScolorStyles)
            Dim size As Size = New Size(90, 28)
            CType(CObj(val), Control).Size = size
            Dim location As Point = New Point(520, 234)
            CType(CObj(val), Control).Location = location
            AddHandler CType(CObj(val), Control).Click, AddressOf OnDeviceOAuth
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val), Control))
            lblDeviceHint = New Label()
            lblDeviceHint.ForeColor = Color.Orange
            Dim label = lblDeviceHint
            location = New Point(12, 264)
            label.Location = location
            Dim label2 = lblDeviceHint
            size = New Size(610, 32)
            label2.Size = size
            lblDeviceHint.Text = "DEVICE LOGIN USES MICROSOFT OAUTH (EWS SCOPE)."
            CType(CObj(Me), Control).Controls.Add(lblDeviceHint)
            Dim val2 As FlatButton = New FlatButton()
            CType(val2, LCARSbuttonClass).ButtonText = "INBOX"
            CType(val2, LCARSbuttonClass).Text = "INBOX"
            CType(val2, LCARSbuttonClass).Color = CType(6, LCARScolorStyles)
            size = New Size(90, 28)
            CType(CObj(val2), Control).Size = size
            location = New Point(12, 300)
            CType(CObj(val2), Control).Location = location
            AddHandler CType(CObj(val2), Control).Click, AddressOf OnSync
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val2), Control))
            Dim val3 As FlatButton = New FlatButton()
            CType(val3, LCARSbuttonClass).ButtonText = "CAL"
            CType(val3, LCARSbuttonClass).Text = "CAL"
            CType(val3, LCARSbuttonClass).Color = CType(4, LCARScolorStyles)
            size = New Size(72, 28)
            CType(CObj(val3), Control).Size = size
            location = New Point(108, 300)
            CType(CObj(val3), Control).Location = location
            AddHandler CType(CObj(val3), Control).Click, AddressOf OnCalendar
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val3), Control))
            Dim val4 As FlatButton = New FlatButton()
            CType(val4, LCARSbuttonClass).ButtonText = "CONTACTS"
            CType(val4, LCARSbuttonClass).Text = "CONTACTS"
            CType(val4, LCARSbuttonClass).Color = CType(4, LCARScolorStyles)
            size = New Size(90, 28)
            CType(CObj(val4), Control).Size = size
            location = New Point(186, 300)
            CType(CObj(val4), Control).Location = location
            AddHandler CType(CObj(val4), Control).Click, AddressOf OnContacts
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val4), Control))
            Dim val5 As FlatButton = New FlatButton()
            CType(val5, LCARSbuttonClass).ButtonText = "MERGE CAL"
            CType(val5, LCARSbuttonClass).Text = "MERGE CAL"
            CType(val5, LCARSbuttonClass).Color = CType(1, LCARScolorStyles)
            size = New Size(100, 28)
            CType(CObj(val5), Control).Size = size
            location = New Point(284, 300)
            CType(CObj(val5), Control).Location = location
            AddHandler CType(CObj(val5), Control).Click, AddressOf OnMergeCalendar
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val5), Control))
            Dim val6 As FlatButton = New FlatButton()
            CType(val6, LCARSbuttonClass).ButtonText = "MERGE ABOOK"
            CType(val6, LCARSbuttonClass).Text = "MERGE ABOOK"
            CType(val6, LCARSbuttonClass).Color = CType(1, LCARScolorStyles)
            size = New Size(110, 28)
            CType(CObj(val6), Control).Size = size
            location = New Point(392, 300)
            CType(CObj(val6), Control).Location = location
            AddHandler CType(CObj(val6), Control).Click, AddressOf OnMergeAddressBook
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val6), Control))
            Dim val7 As FlatButton = New FlatButton()
            CType(val7, LCARSbuttonClass).ButtonText = "SYNC GAL"
            CType(val7, LCARSbuttonClass).Text = "SYNC GAL"
            CType(val7, LCARSbuttonClass).Color = CType(1, LCARScolorStyles)
            size = New Size(90, 28)
            CType(CObj(val7), Control).Size = size
            location = New Point(106, 332)
            CType(CObj(val7), Control).Location = location
            AddHandler CType(CObj(val7), Control).Click, AddressOf OnSyncGal
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val7), Control))
            Dim val8 As FlatButton = New FlatButton()
            CType(val8, LCARSbuttonClass).ButtonText = "CAL FIX"
            CType(val8, LCARSbuttonClass).Text = "CAL FIX"
            CType(val8, LCARSbuttonClass).Color = CType(9, LCARScolorStyles)
            size = New Size(80, 28)
            CType(CObj(val8), Control).Size = size
            location = New Point(202, 332)
            CType(CObj(val8), Control).Location = location
            AddHandler CType(CObj(val8), Control).Click, AddressOf OnCalendarConflictFix
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val8), Control))
            Dim val9 As FlatButton = New FlatButton()
            CType(val9, LCARSbuttonClass).ButtonText = "ADD MAIL"
            CType(val9, LCARSbuttonClass).Text = "ADD MAIL"
            CType(val9, LCARSbuttonClass).Color = CType(6, LCARScolorStyles)
            size = New Size(90, 28)
            CType(CObj(val9), Control).Size = size
            location = New Point(508, 300)
            CType(CObj(val9), Control).Location = location
            AddHandler CType(CObj(val9), Control).Click, AddressOf OnAddToMail
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val9), Control))
            lstItems = New ListBox()
            Dim listBox = lstItems
            location = New Point(12, 368)
            listBox.Location = location
            Dim listBox2 = lstItems
            size = New Size(300, 160)
            listBox2.Size = size
            lstItems.BackColor = Color.FromArgb(30, 30, 30)
            lstItems.ForeColor = Color.Orange
            AddHandler lstItems.SelectedIndexChanged, AddressOf OnItemSelected
            CType(CObj(Me), Control).Controls.Add(lstItems)
            txtBody = New TextBox()
            txtBody.Multiline = True
            txtBody.ScrollBars = ScrollBars.Vertical
            Dim textBox = txtBody
            location = New Point(322, 368)
            textBox.Location = location
            Dim textBox2 = txtBody
            size = New Size(300, 160)
            textBox2.Size = size
            txtBody.BackColor = Color.FromArgb(30, 30, 30)
            txtBody.ForeColor = Color.Orange
            CType(CObj(Me), Control).Controls.Add(txtBody)
            AddLabel("SEND TO", 12, 504)
            txtSendTo = AddField(12, 524, 610)
            AddLabel("SUBJECT", 12, 552)
            txtSendSubject = AddField(12, 572, 610)
            AddLabel("BODY", 12, 600)
            txtSendBody = AddField(12, 620, 450)
            Dim val10 As FlatButton = New FlatButton()
            CType(val10, LCARSbuttonClass).ButtonText = "SEND"
            CType(val10, LCARSbuttonClass).Text = "SEND"
            CType(val10, LCARSbuttonClass).Color = CType(1, LCARScolorStyles)
            size = New Size(72, 28)
            CType(CObj(val10), Control).Size = size
            location = New Point(470, 618)
            CType(CObj(val10), Control).Location = location
            AddHandler CType(CObj(val10), Control).Click, AddressOf OnSend
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val10), Control))
            lblStatus = New Label()
            lblStatus.ForeColor = Color.Orange
            Dim label3 = lblStatus
            location = New Point(12, 656)
            label3.Location = location
            Dim label4 = lblStatus
            size = New Size(610, 24)
            label4.Size = size
            CType(CObj(Me), Control).Controls.Add(lblStatus)
        End Sub

        Private Sub OnAddToMail(sender As Object, e As EventArgs)
            Dim text As String = OAuthToken()
            If Not String.IsNullOrWhiteSpace(text) Then
                EwsOAuthStore.SaveToken(text)
            End If
            Dim mailAccount = EwsMailBridge.TryRegisterAccount(txtUrl.Text, txtUser.Text, txtPassword.Text, txtUser.Text)
            If mailAccount Is Nothing Then
                lblStatus.Text = If(Not Equals(AccountStore.LastError, Nothing), AccountStore.LastError, "ADD MAIL FAILED").ToUpperInvariant()
            Else
                lblStatus.Text = "EWS ACCOUNT ADDED — OPEN MAIL REFRESH"
            End If
        End Sub

        Private Sub OnSync(sender As Object, e As EventArgs)
            listMode = EwsListMode.Mail
            lstItems.Items.Clear()
            txtBody.Clear()
            summaries = EwsMailService.TryFindInboxSummaries(txtUrl.Text, txtUser.Text, txtPassword.Text, OAuthToken())
            If summaries Is Nothing Then
                lblStatus.Text = If(Not Equals(EwsMailService.LastError, Nothing), EwsMailService.LastError, "EWS FAILED").ToUpperInvariant()
                Return
            End If
            For Each summary In summaries
                Dim items = lstItems.Items
                Dim ewsMailSummary = summary
                items.Add(If(Not Equals(ewsMailSummary.Subject, Nothing), ewsMailSummary.Subject, summary.ItemId))
            Next
            lblStatus.Text = summaries.Count.ToString() & " MESSAGE(S)"
        End Sub

        Private Sub OnCalendar(sender As Object, e As EventArgs)
            listMode = EwsListMode.Calendar
            lstItems.Items.Clear()
            txtBody.Clear()
            calendarSummaries = EwsMailService.TryFindCalendarSummaries(txtUrl.Text, txtUser.Text, txtPassword.Text, OAuthToken())
            If calendarSummaries Is Nothing Then
                lblStatus.Text = If(Not Equals(EwsMailService.LastError, Nothing), EwsMailService.LastError, "EWS CALENDAR FAILED").ToUpperInvariant()
                Return
            End If
            For Each calendarSummary In calendarSummaries
                Dim ewsCalendarSummary = calendarSummary
                Dim text = If(Not Equals(ewsCalendarSummary.Subject, Nothing), ewsCalendarSummary.Subject, calendarSummary.ItemId)
                If Not String.IsNullOrWhiteSpace(calendarSummary.StartText) Then
                    text = text & " @ " & calendarSummary.StartText
                End If
                lstItems.Items.Add(text)
            Next
            lblStatus.Text = calendarSummaries.Count.ToString() & " CALENDAR ITEM(S)"
        End Sub

        Private Sub OnMergeCalendar(sender As Object, e As EventArgs)
            Dim num As Integer = EwsLcarsBridge.TrySyncEwsCalendarBidirectional(txtUrl.Text, txtUser.Text, txtPassword.Text, OAuthToken())
            If num < 0 Then
                lblStatus.Text = If(Not Equals(EwsLcarsBridge.LastError, Nothing), EwsLcarsBridge.LastError, "CAL 2-WAY FAILED").ToUpperInvariant()
                Return
            End If
            Dim text = ""
            If EwsLcarsBridge.LastCalendarPushCount > 0 Then
                text = " · PUSHED " & EwsLcarsBridge.LastCalendarPushCount.ToString()
            End If
            Dim text2 = ""
            If EwsLcarsBridge.LastCalendarConflictOverwrites > 0 Then
                text2 = " · OVERWRITES " & EwsLcarsBridge.LastCalendarConflictOverwrites.ToString()
            End If
            Dim num2 As Integer = EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents()
            If EwsLcarsBridge.LastCalendarAutoServerWins > 0 Then
                text2 = text2 & " · SRV-WIN " & EwsLcarsBridge.LastCalendarAutoServerWins.ToString()
            End If
            If EwsLcarsBridge.LastCalendarAutoClientWins > 0 Then
                text2 = text2 & " · CL-WIN " & EwsLcarsBridge.LastCalendarAutoClientWins.ToString()
            End If
            If EwsLcarsBridge.LastCalendarAutoReconcileCleared > 0 Then
                text2 = text2 & " · RECON-CLEARED " & EwsLcarsBridge.LastCalendarAutoReconcileCleared.ToString()
            End If
            If EwsBackgroundSync.LastBackgroundCalAutoReconcile > 0 Then
                text2 = text2 & " · BG-RECON " & EwsBackgroundSync.LastBackgroundCalAutoReconcile.ToString()
            End If
            If EwsBackgroundSync.LastBackgroundCalChangeKeyRefreshes > 0 Then
                text2 = text2 & " · BG-CK " & EwsBackgroundSync.LastBackgroundCalChangeKeyRefreshes.ToString()
            End If
            If EwsBackgroundSync.LastBackgroundCalPushAfterCk > 0 Then
                text2 = text2 & " · BG-PUSH-CAL " & EwsBackgroundSync.LastBackgroundCalPushAfterCk.ToString()
            End If
            If EwsBackgroundSync.LastBackgroundCalBidirectionalThirdPass > 0 Then
                text2 = text2 & " · BG-CAL-3 " & EwsBackgroundSync.LastBackgroundCalBidirectionalThirdPass.ToString()
            End If
            If EwsBackgroundSync.LastBackgroundCalPushAfterThirdPass > 0 Then
                text2 = text2 & " · BG-PUSH-CAL-2 " & EwsBackgroundSync.LastBackgroundCalPushAfterThirdPass.ToString()
            End If
            If EwsBackgroundSync.LastBackgroundCalBidirectionalFourthPass > 0 Then
                text2 = text2 & " · BG-CAL-4 " & EwsBackgroundSync.LastBackgroundCalBidirectionalFourthPass.ToString()
            End If
            If EwsBackgroundSync.LastBackgroundCalAutoReconcileSecondPass > 0 Then
                text2 = text2 & " · BG-RECON-2 " & EwsBackgroundSync.LastBackgroundCalAutoReconcileSecondPass.ToString()
            End If
            If EwsBackgroundSync.LastBackgroundCalChangeKeyRefreshesSecondPass > 0 Then
                text2 = text2 & " · BG-CK-2 " & EwsBackgroundSync.LastBackgroundCalChangeKeyRefreshesSecondPass.ToString()
            End If
            If EwsBackgroundSync.LastBackgroundCalPushAfterFourthPass > 0 Then
                text2 = text2 & " · BG-PUSH-CAL-3 " & EwsBackgroundSync.LastBackgroundCalPushAfterFourthPass.ToString()
            End If
            If EwsBackgroundSync.LastBackgroundCalBidirectionalFifthPass > 0 Then
                text2 = text2 & " · BG-CAL-5 " & EwsBackgroundSync.LastBackgroundCalBidirectionalFifthPass.ToString()
            End If
            If EwsBackgroundSync.LastBackgroundCalAutoReconcileThirdPass > 0 Then
                text2 = text2 & " · BG-RECON-3 " & EwsBackgroundSync.LastBackgroundCalAutoReconcileThirdPass.ToString()
            End If
            If EwsBackgroundSync.LastBackgroundCalPushAfterFifthPass > 0 Then
                text2 = text2 & " · BG-PUSH-CAL-4 " & EwsBackgroundSync.LastBackgroundCalPushAfterFifthPass.ToString()
            End If
            If EwsBackgroundSync.LastBackgroundCalBidirectionalSixthPass > 0 Then
                text2 = text2 & " · BG-CAL-6 " & EwsBackgroundSync.LastBackgroundCalBidirectionalSixthPass.ToString()
            End If
            If EwsBackgroundSync.LastBackgroundCalAutoReconcileFourthPass > 0 Then
                text2 = text2 & " · BG-RECON-4 " & EwsBackgroundSync.LastBackgroundCalAutoReconcileFourthPass.ToString()
            End If
            If EwsBackgroundSync.LastBackgroundCalChangeKeyRefreshesThirdPass > 0 Then
                text2 = text2 & " · BG-CK-3 " & EwsBackgroundSync.LastBackgroundCalChangeKeyRefreshesThirdPass.ToString()
            End If
            If EwsBackgroundSync.LastBackgroundCalPushAfterSixthPass > 0 Then
                text2 = text2 & " · BG-PUSH-CAL-5 " & EwsBackgroundSync.LastBackgroundCalPushAfterSixthPass.ToString()
            End If
            If EwsBackgroundSync.LastBackgroundCalBidirectionalSeventhPass > 0 Then
                text2 = text2 & " · BG-CAL-7 " & EwsBackgroundSync.LastBackgroundCalBidirectionalSeventhPass.ToString()
            End If
            If EwsBackgroundSync.LastBackgroundCalBidirectionalEighthPass > 0 Then
                text2 = text2 & " · BG-CAL-8 " & EwsBackgroundSync.LastBackgroundCalBidirectionalEighthPass.ToString()
            End If
            If num2 > 0 Then
                text2 = text2 & " · EWS-DIRTY " & num2.ToString() & " (NOT CONFLICT-SAFE REAL)"
            End If
            If EwsLcarsBridge.LastCalendarRsvpPushCount > 0 Then
                text = text & " · RSVP " & EwsLcarsBridge.LastCalendarRsvpPushCount.ToString()
            End If
            If EwsLcarsBridge.LastCalendarChangeKeyRefreshes > 0 Then
                text = text & " · CK-REF " & EwsLcarsBridge.LastCalendarChangeKeyRefreshes.ToString()
            End If
            Dim num3 As Integer = AddressBookStore.CountGalPushDirtyContacts()
            If num3 > 0 Then
                text = text & " · GAL-DIRTY " & num3.ToString()
            End If
            lblStatus.Text = num.ToString() & " CAL 2-WAY (PARTIAL — SEE open-slice-acceptance-gaps-340.md)" & text & text2
        End Sub

        Private Sub OnCalendarConflictFix(sender As Object, e As EventArgs)
            Dim num As Integer = EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents()
            Dim text As String = LcarsCommsUi.Prompt("Calendar conflict merge (partial). Type SERVER to pull EWS over local dirty events, or LOCAL to force push.", "LCARS COMMS — EWS CAL FIX", If(num > 0, "SERVER", "LOCAL")).Trim()
            If text.Length <> 0 Then
                If text.Equals("SERVER", StringComparison.OrdinalIgnoreCase) Then
                    Dim num2 As Integer = EwsCalendarConflictHelper.TryRefreshAllDirtyFromServer(txtUrl.Text, txtUser.Text, txtPassword.Text, OAuthToken())
                    lblStatus.Text = num2.ToString() & " EVENT(S) REFRESHED FROM SERVER (PARTIAL MERGE UX)"
                ElseIf text.Equals("LOCAL", StringComparison.OrdinalIgnoreCase) Then
                    Dim num3 As Integer = EwsLcarsBridge.TryPushDirtyCalendarEventsToEws(txtUrl.Text, txtUser.Text, txtPassword.Text, OAuthToken())
                    lblStatus.Text = num3.ToString() & " LOCAL PUSH(ES) · OVERWRITES " & EwsLcarsBridge.LastCalendarConflictOverwrites.ToString()
                Else
                    lblStatus.Text = "CAL FIX: TYPE SERVER OR LOCAL"
                    LcarsCommsUi.Alert("Type SERVER or LOCAL.", "LCARS COMMS — EWS CAL FIX")
                End If
            End If
        End Sub

        Private Sub OnMergeAddressBook(sender As Object, e As EventArgs)
            Dim text = LcarsCommsUi.Prompt("Search name or email to merge:", "LCARS COMMS — EWS ABOOK", If(txtUser IsNot Nothing, txtUser.Text, ""))
            If Not String.IsNullOrWhiteSpace(text) Then
                Dim num As Integer = EwsLcarsBridge.TryMergeContactsFromEws(txtUrl.Text, txtUser.Text, txtPassword.Text, text, OAuthToken())
                If num < 0 Then
                    Dim text2 As String = If(Not Equals(EwsLcarsBridge.LastError, Nothing), EwsLcarsBridge.LastError, "MERGE ABOOK FAILED").ToUpperInvariant()
                    lblStatus.Text = text2
                    LcarsCommsUi.Alert(text2, "LCARS COMMS — EWS ERROR")
                Else
                    lblStatus.Text = num.ToString() & " CONTACT(S) MERGED TO ABOOK"
                End If
            End If
        End Sub

        Private Sub OnSyncGal(sender As Object, e As EventArgs)
            lblStatus.Text = "GAL HARVEST RUNNING…"
            Call Application.DoEvents()
            Dim num As Integer = EwsLcarsBridge.TrySyncGalDirectoryBidirectional(txtUrl.Text, txtUser.Text, txtPassword.Text, OAuthToken(), deepGalHarvest:=True)
            If num < 0 Then
                lblStatus.Text = If(Not Equals(EwsLcarsBridge.LastError, Nothing), EwsLcarsBridge.LastError, "SYNC GAL FAILED").ToUpperInvariant()
                Return
            End If
            Dim text = ""
            If EwsLcarsBridge.LastGalPersonalPushCount > 0 Then
                text = " · PUSH " & EwsLcarsBridge.LastGalPersonalPushCount.ToString()
            End If
            If EwsLcarsBridge.LastGalPersonaUpdateAttempts > 0 Then
                text = text & " · PERSONA API " & EwsLcarsBridge.LastGalPersonaUpdateAttempts.ToString()
                If EwsMailService.LastGalPersonaUpdateDenied Then
                    text += " (DENIED)"
                End If
            End If
            If EwsLcarsBridge.LastGalDirectoryUpdateAttempts > 0 Then
                text = text & " · DIR CONTACT " & EwsLcarsBridge.LastGalDirectoryUpdateAttempts.ToString()
                If EwsMailService.LastGalDirectoryUpdateDenied Then
                    text += " (DENIED)"
                End If
            End If
            If EwsLcarsBridge.LastGalChangeKeyRefreshes > 0 Then
                text = text & " · GAL-CK " & EwsLcarsBridge.LastGalChangeKeyRefreshes.ToString()
            End If
            If EwsLcarsBridge.LastGalPersonaChangeKeyRefreshes > 0 Then
                text = text & " · PERSONA-CK " & EwsLcarsBridge.LastGalPersonaChangeKeyRefreshes.ToString()
            End If
            If EwsLcarsBridge.LastGalDirectoryChangeKeyRefreshes > 0 Then
                text = text & " · DIR-CK " & EwsLcarsBridge.LastGalDirectoryChangeKeyRefreshes.ToString()
            End If
            If EwsLcarsBridge.LastGalDirectoryResolveSuccess > 0 Then
                text = text & " · RESOLVE " & EwsLcarsBridge.LastGalDirectoryResolveSuccess.ToString()
            End If
            If EwsBackgroundSync.LastBackgroundGalPushRetry > 0 Then
                text = text & " · BG-PUSH " & EwsBackgroundSync.LastBackgroundGalPushRetry.ToString()
            End If
            If EwsBackgroundSync.LastBackgroundGalPushSecondRetry > 0 Then
                text = text & " · BG-GAL-2 " & EwsBackgroundSync.LastBackgroundGalPushSecondRetry.ToString()
            End If
            If EwsBackgroundSync.LastBackgroundGalPushThirdRetry > 0 Then
                text = text & " · BG-GAL-3 " & EwsBackgroundSync.LastBackgroundGalPushThirdRetry.ToString()
            End If
            If EwsBackgroundSync.LastBackgroundGalPushFourthRetry > 0 Then
                text = text & " · BG-GAL-4 " & EwsBackgroundSync.LastBackgroundGalPushFourthRetry.ToString()
            End If
            If EwsBackgroundSync.LastBackgroundGalPushFifthRetry > 0 Then
                text = text & " · BG-GAL-5 " & EwsBackgroundSync.LastBackgroundGalPushFifthRetry.ToString()
            End If
            If EwsBackgroundSync.LastBackgroundGalPushSixthRetry > 0 Then
                text = text & " · BG-GAL-6 " & EwsBackgroundSync.LastBackgroundGalPushSixthRetry.ToString()
            End If
            If EwsBackgroundSync.LastBackgroundGalPushSeventhRetry > 0 Then
                text = text & " · BG-GAL-7 " & EwsBackgroundSync.LastBackgroundGalPushSeventhRetry.ToString()
            End If
            If EwsBackgroundSync.LastBackgroundGalPushEighthRetry > 0 Then
                text = text & " · BG-GAL-8 " & EwsBackgroundSync.LastBackgroundGalPushEighthRetry.ToString()
            End If
            If EwsBackgroundSync.LastBackgroundGalPushNinthRetry > 0 Then
                text = text & " · BG-GAL-9 " & EwsBackgroundSync.LastBackgroundGalPushNinthRetry.ToString()
            End If
            Dim text2 As String = EwsGalWriteProofStore.TryLoadLastProofSummary()
            If text2.Length > 0 Then
                text2 = " · " & text2
            End If
            Dim text3 As String = EwsGalWriteProofStore.TryGetDominantWritePath()
            If text3.Length > 0 Then
                text2 = text2 & " · DOM " & text3.ToUpperInvariant()
            End If
            If EwsGalWriteProofStore.LastProof IsNot Nothing Then
                Dim num2 = EwsGalWriteProofStore.LastProof.ShadowUpdateSuccess + EwsGalWriteProofStore.LastProof.ShadowCreateSuccess
                If num2 > 0 Then
                    text2 = text2 & " · SHADOW " & num2.ToString()
                End If
            End If
            lblStatus.Text = num.ToString() & " GAL 2-WAY (PERSONA UPDATE + SHADOW — NOT FULL GAL WRITE)" & text & text2
        End Sub

        Private Sub OnContacts(sender As Object, e As EventArgs)
            listMode = EwsListMode.Contacts
            Dim text = LcarsCommsUi.Prompt("Search name or email:", "LCARS COMMS — EWS CONTACTS", If(txtUser IsNot Nothing, txtUser.Text, ""))
            If String.IsNullOrWhiteSpace(text) Then
                Return
            End If
            lstItems.Items.Clear()
            txtBody.Clear()
            resolvedContacts = EwsMailService.TryResolveContacts(txtUrl.Text, txtUser.Text, txtPassword.Text, text, OAuthToken())
            If resolvedContacts Is Nothing Then
                Dim text2 As String = If(Not Equals(EwsMailService.LastError, Nothing), EwsMailService.LastError, "EWS CONTACTS FAILED").ToUpperInvariant()
                lblStatus.Text = text2
                LcarsCommsUi.Alert(text2, "LCARS COMMS — EWS ERROR")
                Return
            End If
            For Each resolvedContact In resolvedContacts
                Dim items = lstItems.Items
                Dim ewsResolvedContact = resolvedContact
                items.Add(If(Not Equals(ewsResolvedContact.DisplayName, Nothing), ewsResolvedContact.DisplayName, resolvedContact.EmailAddress) & " <" & resolvedContact.EmailAddress & ">")
            Next
            lblStatus.Text = resolvedContacts.Count.ToString() & " CONTACT(S)"
        End Sub

        Private Sub OnItemSelected(sender As Object, e As EventArgs)
            txtBody.Clear()
            Dim selectedIndex = lstItems.SelectedIndex
            If selectedIndex < 0 Then
                Return
            End If
            If listMode = EwsListMode.Calendar Then
                If calendarSummaries IsNot Nothing AndAlso selectedIndex < calendarSummaries.Count Then
                    Dim ewsCalendarSummary = calendarSummaries(selectedIndex)
                    Dim textBox = txtBody
                    Dim ewsCalendarSummary2 = ewsCalendarSummary
                    textBox.Text = If(Not Equals(ewsCalendarSummary2.StartText, Nothing), ewsCalendarSummary2.StartText, "") & vbCrLf & ewsCalendarSummary.ItemId
                    If txtSendSubject IsNot Nothing Then
                        Dim textBox2 = txtSendSubject
                        ewsCalendarSummary2 = ewsCalendarSummary
                        textBox2.Text = If(Not Equals(ewsCalendarSummary2.Subject, Nothing), ewsCalendarSummary2.Subject, "")
                    End If
                    lblStatus.Text = "CALENDAR ITEM"
                End If
            ElseIf listMode = EwsListMode.Contacts Then
                If resolvedContacts IsNot Nothing AndAlso selectedIndex < resolvedContacts.Count Then
                    Dim ewsResolvedContact = resolvedContacts(selectedIndex)
                    txtBody.Text = ewsResolvedContact.EmailAddress
                    If txtSendTo IsNot Nothing Then
                        txtSendTo.Text = ewsResolvedContact.EmailAddress
                    End If
                    lblStatus.Text = "CONTACT SELECTED"
                End If
            Else
                If summaries Is Nothing OrElse selectedIndex >= summaries.Count Then
                    Return
                End If
                Dim ewsMailSummary = summaries(selectedIndex)
                If ewsMailSummary Is Nothing OrElse String.IsNullOrWhiteSpace(ewsMailSummary.ItemId) Then
                    Return
                End If
                Dim ewsMessageBody As EwsMailService.EwsMessageBody = EwsMailService.TryGetItemBody(txtUrl.Text, txtUser.Text, txtPassword.Text, ewsMailSummary.ItemId, OAuthToken())
                If ewsMessageBody Is Nothing Then
                    lblStatus.Text = If(Not Equals(EwsMailService.LastError, Nothing), EwsMailService.LastError, "GET ITEM FAILED").ToUpperInvariant()
                    Return
                End If
                Dim textBox3 = txtBody
                Dim ewsMessageBody2 = ewsMessageBody
                textBox3.Text = If(Not Equals(ewsMessageBody2.BodyText, Nothing), ewsMessageBody2.BodyText, "")
                If txtSendSubject IsNot Nothing AndAlso txtSendSubject.Text.Length = 0 Then
                    Dim textBox4 = txtSendSubject
                    ewsMessageBody2 = ewsMessageBody
                    textBox4.Text = If(Not Equals(ewsMessageBody2.Subject, Nothing), ewsMessageBody2.Subject, ewsMailSummary.Subject)
                End If
                lblStatus.Text = "MESSAGE LOADED"
            End If
        End Sub

        Private Sub OnSend(sender As Object, e As EventArgs)
            If EwsMailService.TrySendMessage(txtUrl.Text, txtUser.Text, txtPassword.Text, txtSendTo.Text, txtSendSubject.Text, txtSendBody.Text, OAuthToken()) Then
                lblStatus.Text = "SENT"
                OnSync(Nothing, EventArgs.Empty)
            Else
                lblStatus.Text = If(Not Equals(EwsMailService.LastError, Nothing), EwsMailService.LastError, "SEND FAILED").ToUpperInvariant()
            End If
        End Sub

        Private Sub TryLoadStoredOAuth()
            Dim token As String = Nothing
            If EwsOAuthStore.TryLoadToken(token) AndAlso txtOAuth IsNot Nothing Then
                txtOAuth.Text = token
            End If
        End Sub

        Private Sub TryLoadOAuthAppSettings()
            Dim tenant As String = Nothing
            Dim clientId As String = Nothing
            EwsOAuthClientStore.TryLoadSettings(tenant, clientId)
            If txtTenant IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(tenant) Then
                txtTenant.Text = tenant
            End If
            If txtClientId IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(clientId) Then
                txtClientId.Text = clientId
            End If
        End Sub

        Private Sub OnDeviceOAuth(sender As Object, e As EventArgs)
            If oauthPollWorker.IsBusy Then
                lblStatus.Text = "DEVICE LOGIN IN PROGRESS"
                Return
            End If
            EwsOAuthClientStore.SaveSettings(txtTenant.Text, txtClientId.Text)
            Dim deviceStartResult = EwsOAuthDeviceFlow.TryStartDeviceFlow(txtTenant.Text, txtClientId.Text)
            If deviceStartResult Is Nothing Then
                Dim text As String = If(Not Equals(EwsOAuthDeviceFlow.LastError, Nothing), EwsOAuthDeviceFlow.LastError, "DEVICE START FAILED").ToUpperInvariant()
                lblStatus.Text = text
                LcarsCommsUi.Alert(text, "LCARS COMMS — EWS DEVICE LOGIN")
                Return
            End If
            Dim deviceStartResult2 = deviceStartResult
            Dim text2 = If(Not Equals(deviceStartResult2.Message, Nothing), deviceStartResult2.Message, "")
            If text2.Length = 0 Then
                text2 = "Go to " & deviceStartResult.VerificationUri & " and enter code: " & deviceStartResult.UserCode
            End If
            lblDeviceHint.Text = text2.ToUpperInvariant()
            If Not String.IsNullOrWhiteSpace(deviceStartResult.VerificationUri) AndAlso Not LcarsCommsUi.OpenInLcarsBrowser(deviceStartResult.VerificationUri) Then
                lblStatus.Text = "BROWSER OPEN FAILED — USE URL BELOW"
            End If
            deviceStartResult2 = deviceStartResult
            Dim text3 = "DEVICE CODE: " & If(Not Equals(deviceStartResult2.UserCode, Nothing), deviceStartResult2.UserCode, "")
            If Not String.IsNullOrWhiteSpace(deviceStartResult.UserCode) Then
                LcarsCommsUi.Alert(text3 & vbCrLf & vbCrLf & "LCARS Web Browser should open the sign-in page." & vbCrLf & "Enter the code above if it is not filled in automatically." & vbCrLf & vbCrLf & text2, "LCARS COMMS — EWS DEVICE LOGIN")
            Else
                LcarsCommsUi.Alert(text2, "LCARS COMMS — EWS DEVICE LOGIN")
            End If
            lblStatus.Text = "WAITING FOR DEVICE APPROVAL…"
            oauthPollWorker.RunWorkerAsync(New DevicePollArgs With {
        .Tenant = txtTenant.Text,
        .ClientId = txtClientId.Text,
        .DeviceCode = deviceStartResult.DeviceCode,
        .IntervalSeconds = deviceStartResult.IntervalSeconds
    })
        End Sub

        Private Sub OAuthPoll_DoWork(sender As Object, e As DoWorkEventArgs)
            Dim devicePollArgs As DevicePollArgs = Nothing

            If Not (CSharpImpl.__Assign(devicePollArgs, TryCast(e.Argument, DevicePollArgs)) IsNot Nothing) Then
                Return
            End If
            Dim backgroundWorker As BackgroundWorker = TryCast(sender, BackgroundWorker)
            Dim num = Math.Max(5, devicePollArgs.IntervalSeconds)
            Dim num2 = 0
            Do
                If backgroundWorker IsNot Nothing AndAlso backgroundWorker.CancellationPending Then
                    e.Cancel = True
                    Return
                End If
                Dim accessToken As String = Nothing
                Dim refreshToken As String = Nothing
                Select Case EwsOAuthDeviceFlow.TryPollAccessToken(devicePollArgs.Tenant, devicePollArgs.ClientId, devicePollArgs.DeviceCode, accessToken, refreshToken)
                    Case EwsOAuthDeviceFlow.TokenPollResult.Success
                        If Not String.IsNullOrWhiteSpace(refreshToken) Then
                            EwsOAuthStore.SaveRefreshToken(refreshToken)
                        End If
                        e.Result = accessToken
                        Return
                    Case EwsOAuthDeviceFlow.TokenPollResult.Failed
                        e.Result = Nothing
                        Return
                End Select
                Thread.Sleep(num * 1000)
                num2 += 1
            Loop While num2 <= 180
            EwsOAuthDeviceFlow.LastError = "Device login timed out."
            e.Result = Nothing
        End Sub

        Private Sub OAuthPoll_Completed(sender As Object, e As RunWorkerCompletedEventArgs)
            If e.Cancelled Then
                lblStatus.Text = "DEVICE LOGIN CANCELLED"
                Return
            End If
            Dim text As String = TryCast(e.Result, String)
            If String.IsNullOrWhiteSpace(text) Then
                Dim text2 As String = If(Not Equals(EwsOAuthDeviceFlow.LastError, Nothing), EwsOAuthDeviceFlow.LastError, "DEVICE LOGIN FAILED").ToUpperInvariant()
                lblStatus.Text = text2
                LcarsCommsUi.Alert(text2, "LCARS COMMS — EWS DEVICE LOGIN")
            Else
                txtOAuth.Text = text
                EwsOAuthStore.SaveToken(text)
                lblStatus.Text = "OAUTH TOKEN RECEIVED"
                LcarsCommsUi.Alert("Device login complete.", "LCARS COMMS — EWS DEVICE LOGIN")
            End If
        End Sub

        Private Function OAuthToken() As String
            If txtOAuth Is Nothing Then
                Return Nothing
            End If
            Dim text As String = txtOAuth.Text.Trim()
            If text.Length > 0 Then
                EwsOAuthStore.SaveToken(text)
                Return text
            End If
            Dim accessToken As String = Nothing
            If EwsOAuthStore.TryEnsureAccessToken(accessToken, txtTenant.Text, txtClientId.Text) Then
                txtOAuth.Text = accessToken
                Return accessToken
            End If
            Return Nothing
        End Function

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

        Private Class CSharpImpl
            Shared Function __Assign(Of T)(ByRef target As T, value As T) As T
                target = value
                Return value
            End Function
        End Class
    End Class

