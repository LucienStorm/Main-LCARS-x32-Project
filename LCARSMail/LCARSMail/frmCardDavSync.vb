Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms
Imports LCARS
Imports LCARS.Controls
    Public Class frmCardDavSync
        Inherits LCARSForm
        Private txtUrl As TextBox

        Private txtUser As TextBox

        Private txtPassword As TextBox

        Private lblStatus As Label

        Private cboTbAccounts As ComboBox

        Private cbo412Conflict As ComboBox

        Private lastProfileDir As String

        Private tbAccounts As List(Of ThunderbirdCardDavAccount)

        Public Sub New()
            CType(CObj(Me), Form).BackColor = Color.Black
            Dim clientSize As Size = New Size(520, 320)
            CType(CObj(Me), Form).ClientSize = clientSize
            CType(CObj(Me), Form).StartPosition = FormStartPosition.CenterParent
            BuildUi()
        End Sub

        Public Sub New(profileDir As String)
            Me.New()
            lastProfileDir = If(Not Equals(profileDir, Nothing), profileDir, "").Trim()
            TryLoadThunderbirdCardDav()
            TryLoadStoredCardDav()
        End Sub

        Private Sub BuildUi()
            AddLabel("SERVER URL", 12, 16)
            txtUrl = AddField(12, 36, 490)
            AddLabel("USERNAME", 12, 68)
            txtUser = AddField(12, 88, 490)
            AddLabel("PASSWORD", 12, 120)
            txtPassword = AddField(12, 140, 490)
            txtPassword.UseSystemPasswordChar = True
            AddLabel("TB CARDDAV BOOK", 12, 168)
            cboTbAccounts = New ComboBox()
            cboTbAccounts.DropDownStyle = ComboBoxStyle.DropDownList
            Dim comboBox = cboTbAccounts
            Dim location As Point = New Point(12, 188)
            comboBox.Location = location
            Dim comboBox2 = cboTbAccounts
            Dim size As Size = New Size(360, 24)
            comboBox2.Size = size
            cboTbAccounts.BackColor = Color.FromArgb(40, 40, 40)
            cboTbAccounts.ForeColor = Color.Orange
            AddHandler cboTbAccounts.SelectedIndexChanged, AddressOf OnTbAccountChanged
            CType(CObj(Me), Control).Controls.Add(cboTbAccounts)
            AddLabel("412 CONFLICT", 380, 168)
            cbo412Conflict = New ComboBox()
            cbo412Conflict.DropDownStyle = ComboBoxStyle.DropDownList
            cbo412Conflict.Items.AddRange(New Object(1) {"ClientMerge", "ServerWins"})
            Dim comboBox3 = cbo412Conflict
            location = New Point(380, 188)
            comboBox3.Location = location
            Dim comboBox4 = cbo412Conflict
            size = New Size(122, 24)
            comboBox4.Size = size
            cbo412Conflict.BackColor = Color.FromArgb(40, 40, 40)
            cbo412Conflict.ForeColor = Color.Orange
            CType(CObj(Me), Control).Controls.Add(cbo412Conflict)
            lblStatus = New Label()
            lblStatus.ForeColor = Color.Orange
            Dim label = lblStatus
            location = New Point(12, 220)
            label.Location = location
            Dim label2 = lblStatus
            size = New Size(490, 24)
            label2.Size = size
            CType(CObj(Me), Control).Controls.Add(lblStatus)
            Dim val As FlatButton = New FlatButton()
            CType(val, LCARSbuttonClass).ButtonText = "SYNC"
            CType(val, LCARSbuttonClass).Text = "SYNC"
            CType(val, LCARSbuttonClass).Color = CType(6, LCARScolorStyles)
            size = New Size(72, 28)
            CType(CObj(val), Control).Size = size
            location = New Point(200, 252)
            CType(CObj(val), Control).Location = location
            AddHandler CType(CObj(val), Control).Click, AddressOf OnSyncClick
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val), Control))
            Dim val2 As FlatButton = New FlatButton()
            CType(val2, LCARSbuttonClass).ButtonText = "PUSH"
            CType(val2, LCARSbuttonClass).Text = "PUSH"
            CType(val2, LCARSbuttonClass).Color = CType(1, LCARScolorStyles)
            size = New Size(72, 28)
            CType(CObj(val2), Control).Size = size
            location = New Point(278, 252)
            CType(CObj(val2), Control).Location = location
            AddHandler CType(CObj(val2), Control).Click, AddressOf OnPushClick
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val2), Control))
            Dim val3 As FlatButton = New FlatButton()
            CType(val3, LCARSbuttonClass).ButtonText = "PUSH ALL"
            CType(val3, LCARSbuttonClass).Text = "PUSH ALL"
            CType(val3, LCARSbuttonClass).Color = CType(4, LCARScolorStyles)
            size = New Size(88, 28)
            CType(CObj(val3), Control).Size = size
            location = New Point(356, 252)
            CType(CObj(val3), Control).Location = location
            AddHandler CType(CObj(val3), Control).Click, AddressOf OnPushAllClick
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val3), Control))
            Dim val4 As FlatButton = New FlatButton()
            CType(val4, LCARSbuttonClass).ButtonText = "EXPORT"
            CType(val4, LCARSbuttonClass).Text = "EXPORT"
            CType(val4, LCARSbuttonClass).Color = CType(2, LCARScolorStyles)
            size = New Size(80, 28)
            CType(CObj(val4), Control).Size = size
            location = New Point(450, 252)
            CType(CObj(val4), Control).Location = location
            AddHandler CType(CObj(val4), Control).Click, AddressOf OnExportClick
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val4), Control))
            Dim val5 As FlatButton = New FlatButton()
            CType(val5, LCARSbuttonClass).ButtonText = "CLOSE"
            CType(val5, LCARSbuttonClass).Text = "CLOSE"
            CType(val5, LCARSbuttonClass).Color = CType(9, LCARScolorStyles)
            size = New Size(72, 28)
            CType(CObj(val5), Control).Size = size
            location = New Point(12, 288)
            CType(CObj(val5), Control).Location = location
            AddHandler CType(CObj(val5), Control).Click, AddressOf OnCloseClick
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val5), Control))
            Dim val6 As FlatButton = New FlatButton()
            CType(val6, LCARSbuttonClass).ButtonText = "412 FIX"
            CType(val6, LCARSbuttonClass).Text = "412 FIX"
            CType(val6, LCARSbuttonClass).Color = CType(9, LCARScolorStyles)
            size = New Size(72, 28)
            CType(CObj(val6), Control).Size = size
            location = New Point(92, 288)
            CType(CObj(val6), Control).Location = location
            AddHandler CType(CObj(val6), Control).Click, AddressOf OnResolve412Click
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val6), Control))
        End Sub

        Private Sub TryLoadThunderbirdCardDav()
            tbAccounts = Nothing
            cboTbAccounts.Items.Clear()
            If Equals(lastProfileDir, Nothing) OrElse lastProfileDir.Length = 0 Then
                tbAccounts = AddressBookStore.TryLoadThunderbirdCardDavSecondaryAccounts()
            Else
                tbAccounts = ThunderbirdCardDavImporter.TryLoadFromProfile(lastProfileDir)
                If tbAccounts IsNot Nothing AndAlso tbAccounts.Count > 0 Then
                    AddressBookStore.SaveThunderbirdCardDavSecondaryAccounts(tbAccounts)
                End If
            End If
            If tbAccounts Is Nothing OrElse tbAccounts.Count = 0 Then
                Return
            End If
            Dim num = 0
            For Each tbAccount In tbAccounts
                If tbAccount IsNot Nothing Then
                    num = num + 1
                    If String.IsNullOrWhiteSpace(tbAccount.BookKey) Then
                        tbAccount.BookKey = "carddav-tb-" & num.ToString()
                    End If
                    Dim item As String = num.ToString() & " · " & tbAccount.ServerUrl
                    cboTbAccounts.Items.Add(item)
                End If
            Next
            cboTbAccounts.SelectedIndex = 0
            ApplyTbAccountFields(tbAccounts(0))
        End Sub

        Private Sub OnTbAccountChanged(sender As Object, e As EventArgs)
            If tbAccounts IsNot Nothing AndAlso cboTbAccounts.SelectedIndex >= 0 AndAlso cboTbAccounts.SelectedIndex < tbAccounts.Count Then
                ApplyTbAccountFields(tbAccounts(cboTbAccounts.SelectedIndex))
            End If
        End Sub

        Private Sub ApplyTbAccountFields(acct As ThunderbirdCardDavAccount)
            If acct IsNot Nothing Then
                txtUrl.Text = acct.ServerUrl
                txtUser.Text = acct.Username
            End If
        End Sub

        Private Sub TryLoadStoredCardDav()
            Dim serverUrl As String = Nothing
            Dim username As String = Nothing
            Dim password As String = Nothing
            If AddressBookStore.TryLoadCardDavSettings(serverUrl, username, password) Then
                If String.IsNullOrWhiteSpace(txtUrl.Text) Then
                    txtUrl.Text = serverUrl
                End If
                If String.IsNullOrWhiteSpace(txtUser.Text) Then
                    txtUser.Text = username
                End If
                If Not Equals(password, Nothing) Then
                    txtPassword.Text = password
                End If
                Dim text = CardDavSyncService.ConflictOn412Mode
                If String.IsNullOrWhiteSpace(text) Then
                    text = "ClientMerge"
                End If
                cbo412Conflict.SelectedItem = text
                If cbo412Conflict.SelectedIndex < 0 Then
                    cbo412Conflict.SelectedIndex = 0
                End If
            End If
        End Sub

        Private Sub PersistCardDavSettingsFromForm()
            Dim conflictOn412Mode = "ClientMerge"
            If cbo412Conflict.SelectedItem IsNot Nothing Then
                conflictOn412Mode = cbo412Conflict.SelectedItem.ToString().Trim()
            End If
            AddressBookStore.SaveCardDavSettings(txtUrl.Text, txtUser.Text, txtPassword.Text, conflictOn412Mode)
            If tbAccounts Is Nothing OrElse cboTbAccounts.SelectedIndex < 0 OrElse cboTbAccounts.SelectedIndex >= tbAccounts.Count Then
                Return
            End If
            Dim thunderbirdCardDavAccount = tbAccounts(cboTbAccounts.SelectedIndex)
            If thunderbirdCardDavAccount IsNot Nothing Then
                Dim thunderbirdCardDavAccount2 = thunderbirdCardDavAccount
                Dim text As String = If(Not Equals(thunderbirdCardDavAccount2.BookKey, Nothing), thunderbirdCardDavAccount2.BookKey, "").Trim()
                If text.Length = 0 Then
                    text = "carddav-tb-" & cboTbAccounts.SelectedIndex + 1.ToString()
                End If
                thunderbirdCardDavAccount.BookKey = text
                If txtPassword.Text.Length > 0 Then
                    AccountStore.SavePassword(text, txtPassword.Text)
                End If
            End If
            AddressBookStore.SaveThunderbirdCardDavSecondaryAccounts(tbAccounts)
        End Sub

        Private Sub EnsureMissingBookPasswords()
            If tbAccounts Is Nothing OrElse tbAccounts.Count = 0 Then
                Return
            End If
            For Each tbAccount In tbAccounts
                If tbAccount Is Nothing Then
                    Continue For
                End If
                Dim thunderbirdCardDavAccount = tbAccount
                Dim text As String = If(Not Equals(thunderbirdCardDavAccount.BookKey, Nothing), thunderbirdCardDavAccount.BookKey, "").Trim()
                If text.Length = 0 Then
                    Continue For
                End If
                Dim password As String = Nothing
                If Not AccountStore.TryGetPassword(text, password) OrElse String.IsNullOrEmpty(password) Then
                    Dim text2 = LcarsCommsUi.Prompt("Password for CardDAV book:" & vbCrLf & tbAccount.ServerUrl, "LCARS COMMS — BOOK PASSWORD", txtPassword.Text)
                    If Not String.IsNullOrWhiteSpace(text2) Then
                        AccountStore.SavePassword(text, text2)
                    End If
                End If
            Next
            AddressBookStore.SaveThunderbirdCardDavSecondaryAccounts(tbAccounts)
        End Sub

        Private Sub OnResolve412Click(sender As Object, e As EventArgs)
            Dim text = LcarsCommsUi.Prompt("Contact email with CardDAV 412 conflict:", "LCARS COMMS — 412 RESOLVE", CardDavSyncService.Last412ConflictEmail)
            If String.IsNullOrWhiteSpace(text) Then
                Return
            End If
            Dim text2 = LcarsCommsUi.Prompt("Mode: ClientMerge or ServerWins", "LCARS COMMS — 412 RESOLVE", CardDavSyncService.ConflictOn412Mode)
            If Not String.IsNullOrWhiteSpace(text2) Then
                PersistCardDavSettingsFromForm()
                If AddressBookStore.TryResolveCardDav412ConflictForEmail(text, txtUrl.Text, txtUser.Text, txtPassword.Text, text2) Then
                    lblStatus.Text = "412 RESOLVED FOR " & text.Trim().ToUpperInvariant()
                    Return
                End If
                Dim text3 As String = If(Not Equals(AddressBookStore.LastError, Nothing), AddressBookStore.LastError, "412 RESOLVE FAILED").ToUpperInvariant()
                lblStatus.Text = text3
                LcarsCommsUi.Alert(text3, "LCARS COMMS — CARDDAV ERROR")
            End If
        End Sub

        Private Sub OnPushAllClick(sender As Object, e As EventArgs)
            Dim num = AddressBookStore.TryPushAllLocalContactsCardDav(txtUrl.Text, txtUser.Text, txtPassword.Text)
            If num <= 0 AndAlso Not String.IsNullOrEmpty(AddressBookStore.LastError) Then
                lblStatus.Text = If(Not Equals(AddressBookStore.LastError, Nothing), AddressBookStore.LastError, "PUSH ALL FAILED").ToUpperInvariant()
            Else
                lblStatus.Text = num.ToString() & " CONTACT(S) PUSHED"
            End If
        End Sub

        Private Sub OnPushClick(sender As Object, e As EventArgs)
            Dim text = LcarsCommsUi.Prompt("Contact email to upload:", "LCARS COMMS — PUSH VCARD")
            If Not String.IsNullOrWhiteSpace(text) Then
                Dim text2 = LcarsCommsUi.Prompt("Display name:", "LCARS COMMS — PUSH VCARD", text)
                Dim mailContact As MailContact = New MailContact()
                mailContact.Email = text.Trim()
                mailContact.DisplayName = If(Not Equals(text2, Nothing), text2, "").Trim()
                Dim contact = mailContact
                If AddressBookStore.TryPushContactCardDav(txtUrl.Text, txtUser.Text, txtPassword.Text, contact) Then
                    lblStatus.Text = "CONTACT PUSHED"
                Else
                    lblStatus.Text = If(Not Equals(AddressBookStore.LastError, Nothing), AddressBookStore.LastError, "PUSH FAILED").ToUpperInvariant()
                End If
            End If
        End Sub

        Private Sub OnExportClick(sender As Object, e As EventArgs)
            Dim saveFileDialog As SaveFileDialog = New SaveFileDialog()
            saveFileDialog.Filter = "vCard (*.vcf)|*.vcf|LDIF (*.ldif)|*.ldif|Mork (*.mab)|*.mab"
            If saveFileDialog.ShowDialog(CType(Me, IWin32Window)) <> DialogResult.OK Then
                Return
            End If
            Dim flag = False
            flag = If(saveFileDialog.FileName.EndsWith(".ldif", StringComparison.OrdinalIgnoreCase), AddressBookStore.TryExportContactsLdif(saveFileDialog.FileName), If(Not saveFileDialog.FileName.EndsWith(".mab", StringComparison.OrdinalIgnoreCase), AddressBookStore.TryExportContactsVCard(saveFileDialog.FileName), AddressBookStore.TryExportContactsMork(saveFileDialog.FileName)))
            If flag AndAlso saveFileDialog.FileName.EndsWith(".mab", StringComparison.OrdinalIgnoreCase) Then
                If MorkAbookParser.LastMabExportByteIdentical OrElse MorkAbookParser.LastMabExportVerifiedAgainstSource Then
                    lblStatus.Text = "EXPORT OK — BYTE-IDENTICAL .MAB (UNCHANGED REAL GATE)"
                ElseIf Not String.IsNullOrEmpty(AddressBookStore.LastError) Then
                    lblStatus.Text = AddressBookStore.LastError.ToUpperInvariant()
                Else
                    lblStatus.Text = "EXPORT OK — EDITED ENCODE (SLICE 13 OPEN)"
                End If
            Else
                lblStatus.Text = If(flag, "EXPORT OK", If(Not Equals(AddressBookStore.LastError, Nothing), AddressBookStore.LastError, "EXPORT FAILED")).ToUpperInvariant()
            End If
        End Sub

        Private Sub OnCloseClick(sender As Object, e As EventArgs)
            Call CType(CObj(Me), Form).Close()
        End Sub

        Private Sub OnSyncClick(sender As Object, e As EventArgs)
            PersistCardDavSettingsFromForm()
            EnsureMissingBookPasswords()
            AddressBookStore.TryRefreshDiscoveredCardDavBooksFromServer(txtUrl.Text, txtUser.Text, txtPassword.Text)
            Dim num As Integer = AddressBookStore.TrySyncStoredCardDavBidirectional()
            If num < 0 Then
                lblStatus.Text = If(Not Equals(AddressBookStore.LastError, Nothing), AddressBookStore.LastError, "SYNC FAILED").ToUpperInvariant()
                Return
            End If
            Dim text = ""
            If AddressBookStore.LastCardDavDiscoveredBooks > 0 Then
                text = " · DISC " & AddressBookStore.LastCardDavDiscoveredBooks.ToString()
            End If
            If AddressBookStore.LastCardDavSecondaryBooksMerged > 0 Then
                text = text & " · TB BOOKS " & AddressBookStore.LastCardDavSecondaryBooksMerged.ToString()
            End If
            lblStatus.Text = num.ToString() & " CONTACT(S) 2-WAY" & text & " · 412=" & CardDavSyncService.ConflictOn412Mode.ToUpperInvariant()
        End Sub

        Private Sub AddLabel(text As String, x As Integer, y As Integer)
            Dim label As Label = New Label()
            label.Text = text
            label.ForeColor = Color.Orange
            label.BackColor = Color.Black
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

