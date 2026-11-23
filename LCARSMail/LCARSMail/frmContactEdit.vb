Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms
Imports LCARS
Imports LCARS.Controls
    Public Class frmContactEdit
        Inherits LCARSForm
        Private txtEmail As TextBox

        Private txtName As TextBox

        Private txtCompany As TextBox

        Private txtWorkPhone As TextBox

        Private txtHomePhone As TextBox

        Private txtNotes As TextBox

        Private lblStatus As Label

        Private ReadOnly originalEmail As String

        Private cardDavEtag As String

        Private isGalEntry As Boolean

        Private galPersonaId As String

        Private galPersonaChangeKey As String

        Private galDirectoryItemId As String

        Private galDirectoryChangeKey As String

        Private ewsPersonalItemId As String

        Public Sub New()
            Me.New(Nothing)
        End Sub

        Public Sub New(existing As MailContact)
            cardDavEtag = ""
            galPersonaId = ""
            galPersonaChangeKey = ""
            galDirectoryItemId = ""
            galDirectoryChangeKey = ""
            ewsPersonalItemId = ""
            CType(CObj(Me), Form).BackColor = Color.Black
            Dim clientSize As Size = New Size(520, 360)
            CType(CObj(Me), Form).ClientSize = clientSize
            CType(CObj(Me), Form).StartPosition = FormStartPosition.CenterParent
            originalEmail = If(existing IsNot Nothing, existing.Email, Nothing)
            If existing IsNot Nothing Then
                Dim mailContact = existing
                cardDavEtag = If(Not Equals(mailContact.CardDavEtag, Nothing), mailContact.CardDavEtag, "")
                isGalEntry = existing.IsGalEntry
                mailContact = existing
                galPersonaId = If(Not Equals(mailContact.GalPersonaId, Nothing), mailContact.GalPersonaId, "")
                mailContact = existing
                galPersonaChangeKey = If(Not Equals(mailContact.GalPersonaChangeKey, Nothing), mailContact.GalPersonaChangeKey, "")
                mailContact = existing
                galDirectoryItemId = If(Not Equals(mailContact.GalDirectoryItemId, Nothing), mailContact.GalDirectoryItemId, "")
                mailContact = existing
                galDirectoryChangeKey = If(Not Equals(mailContact.GalDirectoryChangeKey, Nothing), mailContact.GalDirectoryChangeKey, "")
                mailContact = existing
                ewsPersonalItemId = If(Not Equals(mailContact.EwsPersonalItemId, Nothing), mailContact.EwsPersonalItemId, "")
            End If
            BuildUi()
            If existing IsNot Nothing Then
                Dim textBox = txtEmail
                Dim mailContact = existing
                textBox.Text = If(Not Equals(mailContact.Email, Nothing), mailContact.Email, "")
                Dim textBox2 = txtName
                mailContact = existing
                textBox2.Text = If(Not Equals(mailContact.DisplayName, Nothing), mailContact.DisplayName, "")
                Dim textBox3 = txtCompany
                mailContact = existing
                textBox3.Text = If(Not Equals(mailContact.Company, Nothing), mailContact.Company, "")
                Dim textBox4 = txtWorkPhone
                mailContact = existing
                textBox4.Text = If(Not Equals(mailContact.WorkPhone, Nothing), mailContact.WorkPhone, "")
                Dim textBox5 = txtHomePhone
                mailContact = existing
                textBox5.Text = If(Not Equals(mailContact.HomePhone, Nothing), mailContact.HomePhone, "")
                Dim textBox6 = txtNotes
                mailContact = existing
                textBox6.Text = If(Not Equals(mailContact.Notes, Nothing), mailContact.Notes, "")
            End If
        End Sub

        Private Sub BuildUi()
            AddLabel("EMAIL", 12, 12)
            txtEmail = AddField(12, 32, 490)
            AddLabel("NAME", 12, 64)
            txtName = AddField(12, 84, 490)
            AddLabel("COMPANY", 12, 116)
            txtCompany = AddField(12, 136, 490)
            AddLabel("WORK PHONE", 12, 168)
            txtWorkPhone = AddField(12, 188, 240)
            AddLabel("HOME PHONE", 260, 168)
            txtHomePhone = AddField(260, 188, 242)
            AddLabel("NOTES", 12, 220)
            txtNotes = AddField(12, 240, 490)
            txtNotes.Multiline = True
            txtNotes.Height = 56
            lblStatus = New Label()
            lblStatus.ForeColor = Color.Orange
            Dim label = lblStatus
            Dim location As Point = New Point(12, 304)
            label.Location = location
            Dim label2 = lblStatus
            Dim size As Size = New Size(320, 24)
            label2.Size = size
            CType(CObj(Me), Control).Controls.Add(lblStatus)
            Dim val As FlatButton = New FlatButton()
            CType(val, LCARSbuttonClass).ButtonText = "SAVE"
            CType(val, LCARSbuttonClass).Text = "SAVE"
            CType(val, LCARSbuttonClass).Color = CType(6, LCARScolorStyles)
            size = New Size(72, 28)
            CType(CObj(val), Control).Size = size
            location = New Point(250, 300)
            CType(CObj(val), Control).Location = location
            AddHandler CType(CObj(val), Control).Click, AddressOf OnSave
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val), Control))
            If Not String.IsNullOrWhiteSpace(originalEmail) Then
                Dim val2 As FlatButton = New FlatButton()
                CType(val2, LCARSbuttonClass).ButtonText = "DELETE"
                CType(val2, LCARSbuttonClass).Text = "DELETE"
                CType(val2, LCARSbuttonClass).Color = CType(3, LCARScolorStyles)
                size = New Size(72, 28)
                CType(CObj(val2), Control).Size = size
                location = New Point(330, 300)
                CType(CObj(val2), Control).Location = location
                AddHandler CType(CObj(val2), Control).Click, AddressOf OnDelete
                CType(CObj(Me), Control).Controls.Add(CType(CObj(val2), Control))
            End If
            Dim val3 As FlatButton = New FlatButton()
            CType(val3, LCARSbuttonClass).ButtonText = "CANCEL"
            CType(val3, LCARSbuttonClass).Text = "CANCEL"
            CType(val3, LCARSbuttonClass).Color = CType(9, LCARScolorStyles)
            size = New Size(72, 28)
            CType(CObj(val3), Control).Size = size
            location = New Point(410, 300)
            CType(CObj(val3), Control).Location = location
            AddHandler CType(CObj(val3), Control).Click, Sub(a0, a1)
                                                             Call (Sub() CType(CObj(Me), Form).DialogResult = DialogResult.Cancel)()
                                                         End Sub
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val3), Control))
        End Sub

        Private Sub OnDelete(sender As Object, e As EventArgs)
            Dim text As String = If(Not Equals(originalEmail, Nothing), originalEmail, "").Trim()
            If text.Length = 0 Then
                Dim text2 = txtEmail.Text
                text = If(Not Equals(text2, Nothing), text2, "").Trim()
            End If
            If text.Length <> 0 AndAlso LcarsCommsUi.Confirm("Delete contact " & text & "?", "LCARS COMMS — CONTACT") Then
                If Not AddressBookStore.TryRemoveContact(text) Then
                    Dim text3 As String = If(Not Equals(AddressBookStore.LastError, Nothing), AddressBookStore.LastError, "DELETE FAILED").ToUpperInvariant()
                    lblStatus.Text = text3
                    LcarsCommsUi.Alert(text3, "LCARS COMMS — CONTACT ERROR")
                Else
                    CType(CObj(Me), Form).DialogResult = DialogResult.OK
                    Call CType(CObj(Me), Form).Close()
                End If
            End If
        End Sub

        Private Sub OnSave(sender As Object, e As EventArgs)
            Dim text = txtEmail.Text
            Dim text2 As String = If(Not Equals(text, Nothing), text, "").Trim()
            If text2.Length = 0 Then
                lblStatus.Text = "EMAIL REQUIRED"
                Return
            End If
            Dim mailContact As MailContact = New MailContact()
            mailContact.Email = text2
            text = txtName.Text
            mailContact.DisplayName = If(Not Equals(text, Nothing), text, "").Trim()
            Dim text3 = txtCompany.Text
            mailContact.Company = If(Not Equals(text3, Nothing), text3, "").Trim()
            Dim text4 = txtWorkPhone.Text
            mailContact.WorkPhone = If(Not Equals(text4, Nothing), text4, "").Trim()
            Dim text5 = txtHomePhone.Text
            mailContact.HomePhone = If(Not Equals(text5, Nothing), text5, "").Trim()
            Dim text6 = txtNotes.Text
            mailContact.Notes = If(Not Equals(text6, Nothing), text6, "").Trim()
            mailContact.CardDavEtag = cardDavEtag
            mailContact.CardDavDirty = True
            mailContact.IsGalEntry = isGalEntry
            mailContact.GalPersonaId = galPersonaId
            mailContact.GalPersonaChangeKey = galPersonaChangeKey
            mailContact.GalDirectoryItemId = galDirectoryItemId
            mailContact.GalDirectoryChangeKey = galDirectoryChangeKey
            mailContact.EwsPersonalItemId = ewsPersonalItemId
            Dim mailContact2 = mailContact
            If mailContact2.DisplayName.Length = 0 Then
                mailContact2.DisplayName = text2
            End If
            If Not String.IsNullOrWhiteSpace(originalEmail) AndAlso Not String.Equals(originalEmail, text2, StringComparison.OrdinalIgnoreCase) Then
                AddressBookStore.TryRemoveContact(originalEmail, deleteRemoteCardDav:=False)
            End If
            If mailContact2.IsGalEntry Then
                mailContact2.GalPushDirty = True
            End If
            Dim list As List(Of MailContact) = New List(Of MailContact)()
            list.Add(mailContact2)
            Dim incoming = list
            If AddressBookStore.TryMergeContacts(incoming) < 0 Then
                lblStatus.Text = If(Not Equals(AddressBookStore.LastError, Nothing), AddressBookStore.LastError, "SAVE FAILED").ToUpperInvariant()
                Return
            End If
            Dim serverUrl As String = Nothing
            Dim username As String = Nothing
            Dim password As String = Nothing
            If AddressBookStore.TryLoadCardDavSettings(serverUrl, username, password) Then
                Dim num As Integer = AddressBookStore.TrySyncStoredCardDavBidirectional()
                If num < 0 Then
                    lblStatus.Text = "LOCAL OK — CARDDAV 2-WAY: " & If(Not Equals(AddressBookStore.LastError, Nothing), AddressBookStore.LastError, "SYNC FAILED").ToUpperInvariant()
                    CType(CObj(Me), Form).DialogResult = DialogResult.OK
                    Call CType(CObj(Me), Form).Close()
                    Return
                End If
                If mailContact2.CardDavDirty AndAlso AddressBookStore.TryResolveCardDav412ConflictForEmail(mailContact2.Email, serverUrl, username, password, CardDavSyncService.ConflictOn412Mode) Then
                    mailContact2.CardDavDirty = False
                End If
            End If
            CardDavLiveBookCache.QueueBackgroundRefresh()
            CardDavLiveBookCache.RefreshNow()
            Dim text7 = "CONTACT SAVED"
            If CardDavSyncService.LastMergedAfter412 Then
                text7 += " — 412 MERGED"
            End If
            If mailContact2.IsGalEntry AndAlso mailContact2.GalPushDirty Then
                text7 += " — GAL PUSH QUEUED"
                Dim text8 As String = EwsGalWriteProofStore.TryLoadLastProofSummary()
                If text8.Length > 0 AndAlso text8.Length <= 80 Then
                    text7 = text7 & " · " & text8
                End If
            End If
            lblStatus.Text = text7.ToUpperInvariant()
            CType(CObj(Me), Form).DialogResult = DialogResult.OK
            Call CType(CObj(Me), Form).Close()
        End Sub

        Private Sub AddLabel(text As String, x As Integer, y As Integer)
            Dim label As Label = New Label()
            label.ForeColor = Color.Orange
            Dim location As Point = New Point(x, y)
            label.Location = location
            Dim size As Size = New Size(200, 18)
            label.Size = size
            label.Text = text
            CType(CObj(Me), Control).Controls.Add(label)
        End Sub

        Private Function AddField(x As Integer, y As Integer, w As Integer) As TextBox
            Dim textBox As TextBox = New TextBox()
            Dim location As Point = New Point(x, y)
            textBox.Location = location
            Dim size As Size = New Size(w, 22)
            textBox.Size = size
            textBox.BackColor = Color.FromArgb(30, 30, 30)
            textBox.ForeColor = Color.Orange
            CType(CObj(Me), Control).Controls.Add(textBox)
            Return textBox
        End Function
    End Class

