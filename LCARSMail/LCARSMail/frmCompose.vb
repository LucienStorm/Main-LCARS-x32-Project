Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms
Imports LCARS
Imports LCARS.Controls
Imports Microsoft.VisualBasic.CompilerServices
Imports MimeKit
    Public Class frmCompose
        Inherits LCARSForm
        Private Class SendWorkerArgs
            Public PlainBody As String

            Public HtmlBody As String

            Public SignPgp As Boolean

            Public EncryptPgp As Boolean

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Private Class SendWorkerResult
            Public ErrorText As String

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Private NotInheritable Class ComposeContactPickerDialog
            Inherits LCARSForm
            Private Const TopChrome As Integer = 44

            Private Const BottomBar As Integer = 44

            Private ReadOnly lstContacts As ListBox

            Private ReadOnly allContacts As List(Of MailContact)

            Private liveRefreshTimer As Timer

            Public ReadOnly Property SelectedContacts As IList(Of MailContact)
                Get
                    Dim list As List(Of MailContact) = New List(Of MailContact)()
                    If lstContacts Is Nothing OrElse allContacts Is Nothing Then
                        Return list
                    End If
                    For Each selectedIndex In lstContacts.SelectedIndices
                        Dim num = Conversions.ToInteger(selectedIndex)
                        If num >= 0 AndAlso num < allContacts.Count Then
                            Dim mailContact = allContacts(num)
                            If mailContact IsNot Nothing Then
                                list.Add(mailContact)
                            End If
                        End If
                    Next
                    Return list
                End Get
            End Property

            Public Sub New(contacts As IList(Of MailContact))
                allContacts = New List(Of MailContact)()
                If contacts IsNot Nothing Then
                    For Each contact In contacts
                        If contact IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(contact.Email) Then
                            allContacts.Add(contact)
                        End If
                    Next
                End If
                CType(CObj(Me), Form).Text = "LCARS CONTACTS"
                CType(CObj(Me), Form).BackColor = Color.Black
                CType(CObj(Me), Control).ForeColor = Color.Orange
                CType(CObj(Me), Form).FormBorderStyle = FormBorderStyle.None
                CType(CObj(Me), Form).ControlBox = False
                Me.DoubleBuffered = True
                CType(Me, LCARSForm).BindToWorkingArea = False
                CType(CObj(Me), Form).StartPosition = FormStartPosition.CenterParent
                Dim clientSize As Size = New Size(480, 520)
                CType(CObj(Me), Form).ClientSize = clientSize
                CType(CObj(Me), Form).ShowInTaskbar = False
                Dim val As FlatButton = New FlatButton()
                CType(val, LCARSbuttonClass).ButtonText = "PICK CONTACTS"
                CType(val, LCARSbuttonClass).Text = CType(val, LCARSbuttonClass).ButtonText
                CType(val, LCARSbuttonClass).Clickable = False
                CType(val, LCARSbuttonClass).Color = CType(8, LCARScolorStyles)
                CType(val, LCARSbuttonClass).ButtonTextAlign = ContentAlignment.BottomRight
                clientSize = New Size(180, 32)
                CType(CObj(val), Control).Size = clientSize
                Dim location As Point = New Point(12, 6)
                CType(CObj(val), Control).Location = location
                CType(CObj(Me), Control).Controls.Add(CType(CObj(val), Control))
                lstContacts = New ListBox()
                lstContacts.BorderStyle = BorderStyle.FixedSingle
                lstContacts.BackColor = Color.FromArgb(40, 40, 40)
                lstContacts.ForeColor = Color.White
                lstContacts.Font = LcarsUiFont(9.5F, FontStyle.Regular)
                lstContacts.SelectionMode = SelectionMode.MultiExtended
                Dim listBox = lstContacts
                location = New Point(12, 44)
                listBox.Location = location
                Dim listBox2 = lstContacts
                Dim size As Size = New Size(CType(CObj(Me), Form).ClientSize.Width - 24, CType(CObj(Me), Form).ClientSize.Height - 44 - 44)
                listBox2.Size = size
                For Each allContact In allContacts
                    lstContacts.Items.Add(allContact.ListCaption().ToUpperInvariant())
                Next
                CType(CObj(Me), Control).Controls.Add(lstContacts)
                Dim val2 As FlatButton = New FlatButton()
                CType(val2, LCARSbuttonClass).ButtonText = "CANCEL"
                CType(val2, LCARSbuttonClass).Text = "CANCEL"
                CType(val2, LCARSbuttonClass).Color = CType(9, LCARScolorStyles)
                size = New Size(100, 28)
                CType(CObj(val2), Control).Size = size
                location = (CSharpImpl.__Assign(CType(CObj(val2), Control).Location, New Point(CType(CObj(Me), Form).ClientSize.Width - CType(CObj(val2), Control).Width - 12, CType(CObj(Me), Form).ClientSize.Height - 36)))
                AddHandler CType(CObj(val2), Control).Click, Sub(a0, a1)
                                                                 Call (Sub() CType(CObj(Me), Form).DialogResult = DialogResult.Cancel)()
                                                             End Sub
                CType(CObj(Me), Control).Controls.Add(CType(CObj(val2), Control))
                Dim val3 As FlatButton = New FlatButton()
                CType(val3, LCARSbuttonClass).ButtonText = "ADD NEW"
                CType(val3, LCARSbuttonClass).Text = CType(val3, LCARSbuttonClass).ButtonText
                CType(val3, LCARSbuttonClass).Color = CType(2, LCARScolorStyles)
                size = New Size(90, 28)
                CType(CObj(val3), Control).Size = size
                location = (CSharpImpl.__Assign(CType(CObj(val3), Control).Location, New Point(12, CType(CObj(Me), Form).ClientSize.Height - 36)))
                AddHandler CType(CObj(val3), Control).Click, AddressOf OnAddContact
                CType(CObj(Me), Control).Controls.Add(CType(CObj(val3), Control))
                Dim val4 As FlatButton = New FlatButton()
                CType(val4, LCARSbuttonClass).ButtonText = "EDIT"
                CType(val4, LCARSbuttonClass).Text = CType(val4, LCARSbuttonClass).ButtonText
                CType(val4, LCARSbuttonClass).Color = CType(1, LCARScolorStyles)
                size = New Size(72, 28)
                CType(CObj(val4), Control).Size = size
                location = (CSharpImpl.__Assign(CType(CObj(val4), Control).Location, New Point(CType(CObj(val3), Control).Right + 8, CType(CObj(Me), Form).ClientSize.Height - 36)))
                AddHandler CType(CObj(val4), Control).Click, AddressOf OnEditContact
                CType(CObj(Me), Control).Controls.Add(CType(CObj(val4), Control))
                Dim val5 As FlatButton = New FlatButton()
                CType(val5, LCARSbuttonClass).ButtonText = "ADD TO TO"
                CType(val5, LCARSbuttonClass).Text = CType(val5, LCARSbuttonClass).ButtonText
                CType(val5, LCARSbuttonClass).Color = CType(6, LCARScolorStyles)
                size = New Size(120, 28)
                CType(CObj(val5), Control).Size = size
                location = (CSharpImpl.__Assign(CType(CObj(val5), Control).Location, New Point(CType(CObj(val2), Control).Left - 8 - CType(CObj(val5), Control).Width, CType(CObj(Me), Form).ClientSize.Height - 36)))
                AddHandler CType(CObj(val5), Control).Click, Sub(a0, a1)
                                                                 Call Sub()
                                                                          If lstContacts.SelectedIndices.Count <> 0 Then
                                                                              CType(CObj(Me), Form).DialogResult = DialogResult.OK
                                                                          End If
                                                                      End Sub()
                                                             End Sub
                CType(CObj(Me), Control).Controls.Add(CType(CObj(val5), Control))
                Dim val6 As FlatButton = New FlatButton()
                CType(val6, LCARSbuttonClass).ButtonText = "LIVE"
                CType(val6, LCARSbuttonClass).Text = "LIVE"
                CType(val6, LCARSbuttonClass).Color = CType(1, LCARScolorStyles)
                size = New Size(64, 28)
                CType(CObj(val6), Control).Size = size
                location = (CSharpImpl.__Assign(CType(CObj(val6), Control).Location, New Point(CType(CObj(val4), Control).Right + 8, CType(CObj(Me), Form).ClientSize.Height - 36)))
                AddHandler CType(CObj(val6), Control).Click, Sub(a0, a1)
                                                                 Call CType((Sub()
                                                                                 CardDavLiveBookCache.RefreshNow()
                                                                                 ReloadContactList()
                                                                             End Sub), VB_0024AnonymousDelegate_1)()
                                                             End Sub
                CType(CObj(Me), Control).Controls.Add(CType(CObj(val6), Control))
                liveRefreshTimer = New Timer()
                liveRefreshTimer.Interval = 20000
                AddHandler liveRefreshTimer.Tick, Sub(s, ev)
                                                      CardDavLiveBookCache.QueueBackgroundRefresh()
                                                      ReloadContactList()
                                                  End Sub
                AddHandler CType(CObj(Me), Form).Load, Sub(a0, a1)
                                                           Call CType((Sub()
                                                                           CardDavLiveBookCache.QueueBackgroundRefresh()
                                                                           liveRefreshTimer.Start()
                                                                       End Sub), VB_0024AnonymousDelegate_1)()
                                                       End Sub
                AddHandler CType(CObj(Me), Form).FormClosed, Sub(s, ev As EventArgs)
                                                                 If liveRefreshTimer IsNot Nothing Then
                                                                     liveRefreshTimer.Stop()
                                                                 End If
                                                             End Sub
            End Sub

            Private Sub OnAddContact(sender As Object, e As EventArgs)
                Dim frmContactEdit2 As frmContactEdit = New frmContactEdit()
                Try
                    If CType(CObj(frmContactEdit2), Form).ShowDialog(CType(CObj(Me), IWin32Window)) <> DialogResult.OK Then
                        Return
                    End If

                Finally
                    If frmContactEdit2 IsNot Nothing Then CType(frmContactEdit2, IDisposable).Dispose()
                End Try
                ReloadContactList()
            End Sub

            Private Sub OnEditContact(sender As Object, e As EventArgs)
                If lstContacts.SelectedIndex < 0 Then
                    Return
                End If
                Dim selectedIndex = lstContacts.SelectedIndex
                If selectedIndex < 0 OrElse selectedIndex >= allContacts.Count Then
                    Return
                End If
                Dim frmContactEdit2 As frmContactEdit = New frmContactEdit(allContacts(selectedIndex))
                Try
                    If CType(CObj(frmContactEdit2), Form).ShowDialog(CType(CObj(Me), IWin32Window)) <> DialogResult.OK Then
                        Return
                    End If

                Finally
                    If frmContactEdit2 IsNot Nothing Then CType(frmContactEdit2, IDisposable).Dispose()
                End Try
                ReloadContactList()
            End Sub

            Private Sub ReloadContactList()
                allContacts.Clear()
                For Each item In AddressBookStore.LoadLiveContacts()
                    If item IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(item.Email) Then
                        allContacts.Add(item)
                    End If
                Next
                lstContacts.Items.Clear()
                For Each allContact In allContacts
                    lstContacts.Items.Add(allContact.ListCaption().ToUpperInvariant())
                Next
            End Sub

            Private Class CSharpImpl
                Shared Function __Assign(Of T)(ByRef target As T, value As T) As T
                    target = value
                    Return value
                End Function
            End Class
        End Class

        Private Const MarginX As Integer = 12

        Private Const LabelW As Integer = 72

        Private Const FieldH As Integer = 24

        Private Const RowGap As Integer = 6

        Private Const TopChrome As Integer = 40

        Private Const BottomBar As Integer = 44

        Private Const AttachmentRowH As Integer = 72

        Private Const ContactsBtnW As Integer = 96

        Private Const ContactsBtnGap As Integer = 6

        Private ReadOnly mailAccount As MailAccount

        Private ReadOnly mailPassword As String

        Private ReadOnly smtpService As SmtpMailService

        Private ReadOnly imapService As ImapMailService

        Private ReadOnly composeAttachmentPaths As List(Of String)

        Private ReadOnly sendWorker As BackgroundWorker

        Private htmlComposePane As MailHtmlComposePane

        Private draftId As String

        Private imapDraftFolder As String

        Private imapDraftUid As ULong

        Private fbTitle As FlatButton

        Private lblTo As Label

        Private lblCc As Label

        Private lblSubject As Label

        Private txtTo As TextBox

        Private fbContacts As FlatButton

        Private txtCc As TextBox

        Private txtSubject As TextBox

        Private txtBody As TextBox

        Private lblAttachments As Label

        Private lstAttachments As ListBox

        Private fbAddAttachment As FlatButton

        Private fbRemoveAttachment As FlatButton

        Private lblStatus As Label

        Private fbSend As FlatButton

        Private fbSaveDraft As FlatButton

        Private fbCancel As FlatButton

        Private chkSignPgp As CheckBox

        Private chkEncryptPgp As CheckBox

        Public ReadOnly Property AttachmentPaths As IList(Of String)
            Get
                Return composeAttachmentPaths
            End Get
        End Property

        Public Property ComposeTo As String
            Get
                Return If(txtTo Is Nothing, "", txtTo.Text)
            End Get
            Set(value As String)
                If txtTo IsNot Nothing Then
                    txtTo.Text = If(Not Equals(value, Nothing), value, "")
                End If
            End Set
        End Property

        Public Property ComposeCc As String
            Get
                Return If(txtCc Is Nothing, "", txtCc.Text)
            End Get
            Set(value As String)
                If txtCc IsNot Nothing Then
                    txtCc.Text = If(Not Equals(value, Nothing), value, "")
                End If
            End Set
        End Property

        Public Property ComposeSubject As String
            Get
                Return If(txtSubject Is Nothing, "", txtSubject.Text)
            End Get
            Set(value As String)
                If txtSubject IsNot Nothing Then
                    txtSubject.Text = If(Not Equals(value, Nothing), value, "")
                End If
            End Set
        End Property

        Public Property ComposeBodyHtml As String
            Get
                If htmlComposePane IsNot Nothing Then
                    Return htmlComposePane.HtmlBody
                End If
                Return ""
            End Get
            Set(value As String)
                If htmlComposePane IsNot Nothing Then
                    htmlComposePane.SetHtmlContent(If(Not Equals(value, Nothing), value, ""))
                End If
            End Set
        End Property

        Public Property ComposeBody As String
            Get
                If htmlComposePane IsNot Nothing Then
                    Return htmlComposePane.PlainText
                End If
                Return If(txtBody Is Nothing, "", txtBody.Text)
            End Get
            Set(value As String)
                If htmlComposePane IsNot Nothing Then
                    htmlComposePane.PlainText = If(Not Equals(value, Nothing), value, "")
                ElseIf txtBody IsNot Nothing Then
                    txtBody.Text = If(Not Equals(value, Nothing), value, "")
                End If
            End Set
        End Property

        Public Property SendStatusReporter As Action(Of String)

        Public Sub ApplyDraftLink(draft As MailDraft)
            If draft Is Nothing Then
                Return
            End If
            Dim mailDraft = draft
            draftId = If(Not Equals(mailDraft.Id, Nothing), mailDraft.Id, "")
            mailDraft = draft
            imapDraftFolder = If(Not Equals(mailDraft.ImapDraftFolder, Nothing), mailDraft.ImapDraftFolder, "")
            imapDraftUid = draft.ImapDraftUid
            mailDraft = draft
            ComposeTo = If(Not Equals(mailDraft.ToAddresses, Nothing), mailDraft.ToAddresses, "")
            mailDraft = draft
            ComposeCc = If(Not Equals(mailDraft.CcAddresses, Nothing), mailDraft.CcAddresses, "")
            mailDraft = draft
            ComposeSubject = If(Not Equals(mailDraft.Subject, Nothing), mailDraft.Subject, "")
            If Not String.IsNullOrWhiteSpace(draft.BodyHtml) Then
                ComposeBodyHtml = draft.BodyHtml
            Else
                mailDraft = draft
                ComposeBody = If(Not Equals(mailDraft.BodyPlain, Nothing), mailDraft.BodyPlain, "")
            End If
            composeAttachmentPaths.Clear()
            If draft.AttachmentPaths IsNot Nothing Then
                composeAttachmentPaths.AddRange(draft.AttachmentPaths)
            End If
            If lstAttachments Is Nothing Then
                Return
            End If
            lstAttachments.Items.Clear()
            For Each composeAttachmentPath In composeAttachmentPaths
                lstAttachments.Items.Add(Path.GetFileName(composeAttachmentPath))
            Next
        End Sub

        Public Sub ApplyImapDraftLink(folderFullName As String, uid As ULong)
            imapDraftFolder = If(Not Equals(folderFullName, Nothing), folderFullName, "").Trim()
            imapDraftUid = uid
        End Sub

        Public Sub New(account As MailAccount, password As String)
            smtpService = New SmtpMailService()
            imapService = New ImapMailService()
            composeAttachmentPaths = New List(Of String)()
            sendWorker = New BackgroundWorker()
            mailAccount = account
            mailPassword = If(Not Equals(password, Nothing), password, "")
            CType(CObj(Me), Form).Text = "LCARS COMPOSE"
            CType(CObj(Me), Form).BackColor = Color.Black
            CType(CObj(Me), Control).ForeColor = Color.Orange
            CType(CObj(Me), Form).FormBorderStyle = FormBorderStyle.None
            CType(CObj(Me), Form).ControlBox = False
            Me.DoubleBuffered = True
            CType(Me, LCARSForm).BindToWorkingArea = False
            CType(CObj(Me), Form).StartPosition = FormStartPosition.CenterParent
            Dim clientSize As Size = New Size(640, 520)
            CType(CObj(Me), Form).ClientSize = clientSize
            CType(CObj(Me), Form).ShowInTaskbar = False
            sendWorker.WorkerSupportsCancellation = False
            AddHandler sendWorker.DoWork, AddressOf SendWorker_DoWork
            AddHandler sendWorker.RunWorkerCompleted, AddressOf SendWorker_Completed
            BuildChrome()
            CardDavLiveBookCache.EnsureAlwaysOnStarted()
            AddHandler CType(CObj(Me), Control).Resize, AddressOf OnComposeResize
            AddHandler CType(CObj(Me), Form).Shown, AddressOf OnComposeShown
        End Sub

        Private Sub BuildChrome()
            fbTitle = New FlatButton()
            CType(CObj(fbTitle), Control).Name = "fbTitle"
            CType(fbTitle, LCARSbuttonClass).ButtonText = "COMPOSE"
            CType(fbTitle, LCARSbuttonClass).Text = "COMPOSE"
            CType(fbTitle, LCARSbuttonClass).Clickable = False
            CType(fbTitle, LCARSbuttonClass).Color = CType(8, LCARScolorStyles)
            CType(fbTitle, LCARSbuttonClass).ButtonTextAlign = ContentAlignment.BottomRight
            CType(fbTitle, LCARSbuttonClass).ButtonTextHeight = 14
            Dim obj As FlatButton = fbTitle
            Dim size As Size = New Size(120, 32)
            CType(CObj(obj), Control).Size = size
            Dim obj2 As FlatButton = fbTitle
            Dim location As Point = New Point(12, 4)
            CType(CObj(obj2), Control).Location = location
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbTitle), Control))
            lblTo = MakeFieldLabel("TO")
            lblCc = MakeFieldLabel("CC")
            lblSubject = MakeFieldLabel("SUBJECT")
            txtTo = MakeFieldTextBox()
            fbContacts = frmCompose.MakeActionButton("fbContacts", "CONTACTS", CType(2, LCARScolorStyles))
            Dim obj3 As FlatButton = fbContacts
            size = New Size(96, 24)
            CType(CObj(obj3), Control).Size = size
            AddHandler CType(CObj(fbContacts), Control).Click, AddressOf OnContactsClick
            txtCc = MakeFieldTextBox()
            txtSubject = MakeFieldTextBox()
            txtBody = MakeFieldTextBox()
            txtBody.Visible = False
            htmlComposePane = New MailHtmlComposePane()
            htmlComposePane.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
            lblAttachments = MakeFieldLabel("FILES")
            lblAttachments.TextAlign = ContentAlignment.TopRight
            lstAttachments = New ListBox()
            lstAttachments.BorderStyle = BorderStyle.FixedSingle
            lstAttachments.BackColor = Color.FromArgb(40, 40, 40)
            lstAttachments.ForeColor = Color.White
            lstAttachments.Font = LcarsUiFont(9F, FontStyle.Regular)
            fbAddAttachment = frmCompose.MakeActionButton("fbAddAttachment", "ADD FILE", CType(1, LCARScolorStyles))
            Dim obj4 As FlatButton = fbAddAttachment
            size = New Size(100, 28)
            CType(CObj(obj4), Control).Size = size
            AddHandler CType(CObj(fbAddAttachment), Control).Click, AddressOf OnAddAttachmentClick
            fbRemoveAttachment = frmCompose.MakeActionButton("fbRemoveAttachment", "REMOVE", CType(9, LCARScolorStyles))
            Dim obj5 As FlatButton = fbRemoveAttachment
            size = New Size(100, 28)
            CType(CObj(obj5), Control).Size = size
            AddHandler CType(CObj(fbRemoveAttachment), Control).Click, AddressOf OnRemoveAttachmentClick
            lblStatus = New Label()
            lblStatus.Name = "lblStatus"
            lblStatus.AutoSize = False
            lblStatus.TextAlign = ContentAlignment.MiddleLeft
            lblStatus.ForeColor = Color.FromArgb(200, 140, 60)
            lblStatus.BackColor = Color.Black
            lblStatus.Font = LcarsUiFont(9F)
            lblStatus.Text = ""
            fbSend = frmCompose.MakeActionButton("fbSend", "SEND", CType(6, LCARScolorStyles))
            AddHandler CType(CObj(fbSend), Control).Click, AddressOf OnSendClick
            fbSaveDraft = frmCompose.MakeActionButton("fbSaveDraft", "SAVE DRAFT", CType(1, LCARScolorStyles))
            AddHandler CType(CObj(fbSaveDraft), Control).Click, AddressOf OnSaveDraftClick
            fbCancel = frmCompose.MakeActionButton("fbCancel", "CANCEL", CType(4, LCARScolorStyles))
            AddHandler CType(CObj(fbCancel), Control).Click, AddressOf OnCancelClick
            chkSignPgp = New CheckBox()
            chkSignPgp.Text = "SIGN PGP"
            chkSignPgp.ForeColor = Color.Orange
            chkSignPgp.BackColor = Color.Black
            chkSignPgp.Font = LcarsUiFont(9F)
            chkSignPgp.AutoSize = True
            chkEncryptPgp = New CheckBox()
            chkEncryptPgp.Text = "ENCRYPT PGP"
            chkEncryptPgp.ForeColor = Color.Orange
            chkEncryptPgp.BackColor = Color.Black
            chkEncryptPgp.Font = LcarsUiFont(9F)
            chkEncryptPgp.AutoSize = True
            CType(CObj(Me), Control).Controls.Add(lblTo)
            CType(CObj(Me), Control).Controls.Add(lblCc)
            CType(CObj(Me), Control).Controls.Add(lblSubject)
            CType(CObj(Me), Control).Controls.Add(txtTo)
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbContacts), Control))
            CType(CObj(Me), Control).Controls.Add(txtCc)
            CType(CObj(Me), Control).Controls.Add(txtSubject)
            CType(CObj(Me), Control).Controls.Add(lblAttachments)
            CType(CObj(Me), Control).Controls.Add(lstAttachments)
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbAddAttachment), Control))
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbRemoveAttachment), Control))
            CType(CObj(Me), Control).Controls.Add(txtBody)
            CType(CObj(Me), Control).Controls.Add(htmlComposePane)
            CType(CObj(Me), Control).Controls.Add(lblStatus)
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbSend), Control))
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbSaveDraft), Control))
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbCancel), Control))
            CType(CObj(Me), Control).Controls.Add(chkSignPgp)
            CType(CObj(Me), Control).Controls.Add(chkEncryptPgp)
            Call CType(CObj(fbSend), Control).BringToFront()
            Call CType(CObj(fbCancel), Control).BringToFront()
            Call CType(CObj(fbTitle), Control).BringToFront()
        End Sub

        Private Sub OnComposeShown(sender As Object, e As EventArgs)
            LayoutFields()
            If txtTo IsNot Nothing Then
                txtTo.Focus()
            End If
        End Sub

        Private Sub OnComposeResize(sender As Object, e As EventArgs)
            LayoutFields()
        End Sub

        Private Sub LayoutFields()
            If txtTo IsNot Nothing Then
                Dim num = 92
                Dim num2 = Math.Max(120, CType(CObj(Me), Form).ClientSize.Width - num - 12)
                Dim fieldW = Math.Max(80, num2 - 96 - 6)
                Dim num3 = 40
                PlaceLabelRow(lblTo, txtTo, num3, num, fieldW)
                Dim location As Point
                If fbContacts IsNot Nothing AndAlso txtTo IsNot Nothing Then
                    Dim obj As FlatButton = fbContacts
                    location = New Point(txtTo.Right + 6, num3)
                    CType(CObj(obj), Control).Location = location
                    Dim obj2 As FlatButton = fbContacts
                    Dim size As Size = New Size(96, 24)
                    CType(CObj(obj2), Control).Size = size
                End If
                num3 += 30
                PlaceLabelRow(lblCc, txtCc, num3, num, num2)
                num3 += 30
                PlaceLabelRow(lblSubject, txtSubject, num3, num, num2)
                num3 += 34
                If lblAttachments IsNot Nothing Then
                    location = (CSharpImpl.__Assign(lblAttachments.Location, New Point(12, num3 + 2)))
                    Dim label = lblAttachments
                    Dim size As Size = New Size(72, 72)
                    label.Size = size
                End If
                If lstAttachments IsNot Nothing Then
                    location = (CSharpImpl.__Assign(lstAttachments.Location, New Point(num, num3)))
                    Dim listBox = lstAttachments
                    Dim size As Size = New Size(Math.Max(120, num2 - 216), 72)
                    listBox.Size = size
                End If
                If fbAddAttachment IsNot Nothing AndAlso lstAttachments IsNot Nothing Then
                    location = (CSharpImpl.__Assign(CType(CObj(fbAddAttachment), Control).Location, New Point(lstAttachments.Right + 8, num3)))
                End If
                If fbRemoveAttachment IsNot Nothing AndAlso fbAddAttachment IsNot Nothing Then
                    location = (CSharpImpl.__Assign(CType(CObj(fbRemoveAttachment), Control).Location, New Point(CType(CObj(fbAddAttachment), Control).Left, CType(CObj(fbAddAttachment), Control).Bottom + 6)))
                End If
                num3 += 78
                Dim num4 = CType(CObj(Me), Form).ClientSize.Height - 44
                Dim height = Math.Max(80, num4 - num3 - 8)
                If htmlComposePane IsNot Nothing Then
                    location = (CSharpImpl.__Assign(htmlComposePane.Location, New Point(num, num3)))
                    Dim mailHtmlComposePane = htmlComposePane
                    Dim size As Size = New Size(num2, height)
                    mailHtmlComposePane.Size = size
                End If
                If txtBody IsNot Nothing Then
                    location = (CSharpImpl.__Assign(txtBody.Location, New Point(num, num3)))
                    Dim textBox = txtBody
                    Dim size As Size = New Size(num2, height)
                    textBox.Size = size
                End If
                If lblStatus IsNot Nothing Then
                    location = (CSharpImpl.__Assign(lblStatus.Location, New Point(12, num4 + 4)))
                    Dim label2 = lblStatus
                    Dim size As Size = New Size(Math.Max(80, num2 + 72 - CType(CObj(fbSend), Control).Width - CType(CObj(fbCancel), Control).Width - CType(CObj(fbSaveDraft), Control).Width - 16), 28)
                    label2.Size = size
                End If
                If fbCancel IsNot Nothing Then
                    location = (CSharpImpl.__Assign(CType(CObj(fbCancel), Control).Location, New Point(CType(CObj(Me), Form).ClientSize.Width - 12 - CType(CObj(fbCancel), Control).Width, num4 + 4)))
                End If
                If fbSend IsNot Nothing AndAlso fbCancel IsNot Nothing Then
                    Dim obj3 As FlatButton = fbSend
                    location = New Point(CType(CObj(fbCancel), Control).Left - 8 - CType(CObj(fbSend), Control).Width, num4 + 4)
                    CType(CObj(obj3), Control).Location = location
                End If
                If fbSaveDraft IsNot Nothing AndAlso fbSend IsNot Nothing Then
                    Dim obj4 As FlatButton = fbSaveDraft
                    location = New Point(CType(CObj(fbSend), Control).Left - 8 - CType(CObj(fbSaveDraft), Control).Width, num4 + 4)
                    CType(CObj(obj4), Control).Location = location
                End If
                If chkEncryptPgp IsNot Nothing AndAlso fbSend IsNot Nothing Then
                    Dim checkBox = chkEncryptPgp
                    location = New Point(CType(CObj(fbSend), Control).Left - 130, num4 + 6)
                    checkBox.Location = location
                End If
                If chkSignPgp IsNot Nothing AndAlso chkEncryptPgp IsNot Nothing Then
                    Dim checkBox2 = chkSignPgp
                    location = New Point(chkEncryptPgp.Left - 110, num4 + 6)
                    checkBox2.Location = location
                End If
            End If
        End Sub

        Private Shared Sub PlaceLabelRow(lbl As Label, field As TextBox, y As Integer, fieldLeft As Integer, fieldW As Integer)
            If lbl IsNot Nothing Then
                Dim location As Point = New Point(12, y + 4)
                lbl.Location = location
            End If
            If field IsNot Nothing Then
                Dim location As Point = New Point(fieldLeft, y)
                field.Location = location
                Dim size As Size = New Size(fieldW, 24)
                field.Size = size
            End If
        End Sub

        Private Sub OnSendClick(sender As Object, e As EventArgs)
            If Not sendWorker.IsBusy Then
                If fbSend IsNot Nothing Then
                    CType(fbSend, LCARSbuttonClass).Clickable = False
                End If
                SetStatus("SENDING…")
                ReportSendStatus("SENDING…")
                sendWorker.RunWorkerAsync()
            End If
        End Sub

        Private Sub SendWorker_DoWork(sender As Object, e As DoWorkEventArgs)
            Dim sendWorkerArgs As SendWorkerArgs = New SendWorkerArgs()
            sendWorkerArgs.PlainBody = ComposeBody
            sendWorkerArgs.HtmlBody = If(htmlComposePane IsNot Nothing, htmlComposePane.HtmlBody, "")
            sendWorkerArgs.SignPgp = If(chkSignPgp IsNot Nothing AndAlso chkSignPgp.Checked, True, False)
            sendWorkerArgs.EncryptPgp = If(chkEncryptPgp IsNot Nothing AndAlso chkEncryptPgp.Checked, True, False)
            Dim sendWorkerArgs2 = sendWorkerArgs
            Dim sendWorkerResult As SendWorkerResult = New SendWorkerResult()
            If EwsMailBridge.IsEwsAccount(mailAccount) Then
                Dim oauthToken As String = EwsMailBridge.ResolveOAuthToken()
                If Not EwsMailService.TrySendMessageHtml(mailAccount.EwsUrl, mailAccount.Username, mailPassword, ComposeTo, ComposeSubject, sendWorkerArgs2.HtmlBody, sendWorkerArgs2.PlainBody, oauthToken) Then
                    sendWorkerResult.ErrorText = If(Not Equals(EwsMailService.LastError, Nothing), EwsMailService.LastError, "EWS send failed.")
                End If
            Else
                sendWorkerResult.ErrorText = smtpService.Send(mailAccount, mailPassword, ComposeTo, ComposeCc, ComposeSubject, sendWorkerArgs2.PlainBody, composeAttachmentPaths, sendWorkerArgs2.SignPgp, sendWorkerArgs2.EncryptPgp, sendWorkerArgs2.HtmlBody)
            End If
            e.Result = sendWorkerResult
        End Sub

        Private Sub SendWorker_Completed(sender As Object, e As RunWorkerCompletedEventArgs)
            If fbSend IsNot Nothing Then
                CType(fbSend, LCARSbuttonClass).Clickable = True
            End If
            If e.Error IsNot Nothing Then
                SetStatus(e.Error.Message)
                ReportSendStatus("SEND ERROR: " & e.Error.Message)
                Return
            End If
            Dim sendWorkerResult As SendWorkerResult = Nothing

            If CSharpImpl.__Assign(sendWorkerResult, TryCast(e.Result, SendWorkerResult)) IsNot Nothing AndAlso Not String.IsNullOrEmpty(sendWorkerResult.ErrorText) Then
                SetStatus(sendWorkerResult.ErrorText)
                ReportSendStatus("SEND ERROR: " & sendWorkerResult.ErrorText)
                Return
            End If
            If Not String.IsNullOrEmpty(draftId) Then
                DraftStore.DeleteDraft(draftId)
            End If
            ReportSendStatus("MESSAGE SENT")
            CType(CObj(Me), Form).DialogResult = DialogResult.OK
            Call CType(CObj(Me), Form).Close()
        End Sub

        Private Sub OnSaveDraftClick(sender As Object, e As EventArgs)
            If mailAccount Is Nothing Then
                Return
            End If
            Dim mailDraft As MailDraft = New MailDraft()
            mailDraft.Id = draftId
            mailDraft.AccountId = If(Not Equals(mailAccount.Id, Nothing), mailAccount.Id, "")
            mailDraft.ToAddresses = ComposeTo
            mailDraft.CcAddresses = ComposeCc
            mailDraft.Subject = ComposeSubject
            mailDraft.BodyPlain = ComposeBody
            mailDraft.BodyHtml = If(htmlComposePane IsNot Nothing, htmlComposePane.HtmlBody, "")
            mailDraft.ImapDraftFolder = imapDraftFolder
            mailDraft.ImapDraftUid = imapDraftUid
            Dim mailDraft2 = mailDraft
            mailDraft2.AttachmentPaths.AddRange(composeAttachmentPaths)
            draftId = DraftStore.SaveDraft(mailDraft2)
            Try
                Dim message As MimeMessage = smtpService.CreateMimeMessage(mailAccount, ComposeTo, ComposeCc, ComposeSubject, ComposeBody, composeAttachmentPaths, If(htmlComposePane IsNot Nothing, htmlComposePane.HtmlBody, Nothing))
                Dim newUid = 0UL
                Dim text = ""
                If imapDraftUid <> 0 AndAlso Not String.IsNullOrWhiteSpace(imapDraftFolder) Then
                    text = imapService.ReplaceDraft(mailAccount, mailPassword, imapDraftFolder, imapDraftUid, message, newUid)
                Else
                    text = imapService.AppendDraft(mailAccount, mailPassword, message, newUid)
                    If newUid <> 0 AndAlso String.IsNullOrWhiteSpace(imapDraftFolder) Then
                        imapDraftFolder = imapService.ResolveDraftsFolderFullName(mailAccount, mailPassword)
                    End If
                End If
                If Not Equals(text, Nothing) AndAlso text.Length > 0 Then
                    SetStatus("DRAFT SAVED LOCALLY — IMAP: " & text)
                    Return
                End If
                If newUid <> 0 Then
                    imapDraftUid = newUid
                    mailDraft2.ImapDraftFolder = imapDraftFolder
                    mailDraft2.ImapDraftUid = newUid
                    DraftStore.SaveDraft(mailDraft2)
                End If
            Catch ex As Exception
                ProjectData.SetProjectError(ex)
                Dim ex2 = ex
                SetStatus("DRAFT SAVED LOCALLY — IMAP: " & ex2.Message)
                Call ProjectData.ClearProjectError()
                Return
            End Try
            SetStatus("DRAFT SAVED — LOCAL + IMAP UID " & imapDraftUid.ToString())
        End Sub

        Private Sub ReportSendStatus(text As String)
            Dim sendStatusReporter = Me.SendStatusReporter
            If sendStatusReporter Is Nothing Then
                Return
            End If
            Try
                sendStatusReporter(text)
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
        End Sub

        Private Sub OnCancelClick(sender As Object, e As EventArgs)
            CType(CObj(Me), Form).DialogResult = DialogResult.Cancel
            Call CType(CObj(Me), Form).Close()
        End Sub

        Private Sub OnAddAttachmentClick(sender As Object, e As EventArgs)
            Dim openFileDialog As OpenFileDialog = New OpenFileDialog()
            openFileDialog.Title = "ADD ATTACHMENT"
            openFileDialog.Multiselect = True
            openFileDialog.CheckFileExists = True
            If openFileDialog.ShowDialog(CType(Me, IWin32Window)) <> DialogResult.OK Then
                Return
            End If
            Dim fileNames = openFileDialog.FileNames
            For Each text In fileNames
                If Not String.IsNullOrWhiteSpace(text) AndAlso File.Exists(text) AndAlso Not composeAttachmentPaths.Contains(text) Then
                    composeAttachmentPaths.Add(text)
                    If lstAttachments IsNot Nothing Then
                        lstAttachments.Items.Add(Path.GetFileName(text))
                    End If
                End If
            Next
        End Sub

        Private Sub OnContactsClick(sender As Object, e As EventArgs)
            CardDavLiveBookCache.EnsureAlwaysOnStarted()
            CardDavLiveBookCache.RefreshNow()
            Dim list As List(Of MailContact) = AddressBookStore.LoadLiveContacts()
            If list Is Nothing OrElse list.Count = 0 Then
                SetStatus("NO CONTACTS — IMPORT PROFILE WITH ABOOK")
                Return
            End If
            Dim composeContactPickerDialog As ComposeContactPickerDialog = New ComposeContactPickerDialog(list)
            Try
                If CType(CObj(composeContactPickerDialog), Form).ShowDialog(CType(Me, IWin32Window)) <> DialogResult.OK Then
                    Return
                End If
                Dim selectedContacts = composeContactPickerDialog.SelectedContacts
                If selectedContacts Is Nothing OrElse selectedContacts.Count = 0 Then
                    Return
                End If
                Dim list2 As List(Of String) = New List(Of String)()
                For Each item In selectedContacts
                    If item IsNot Nothing Then
                        Dim text As String = item.FormatAddress()
                        If text.Length <> 0 Then
                            list2.Add(text)
                        End If
                    End If
                Next
                If list2.Count <> 0 Then
                    Dim text2 As String = String.Join(", ", list2.ToArray())
                    Dim text3 As String = If(txtTo Is Nothing, "", txtTo.Text).Trim()
                    If text3.Length = 0 Then
                        ComposeTo = text2
                    Else
                        ComposeTo = text3 & ", " & text2
                    End If
                    SetStatus("")
                    If txtTo IsNot Nothing Then
                        txtTo.Focus()
                    End If
                End If

            Finally
                If CObj(composeContactPickerDialog) IsNot Nothing Then CType(CObj(composeContactPickerDialog), IDisposable).Dispose()
            End Try
        End Sub

        Private Sub OnRemoveAttachmentClick(sender As Object, e As EventArgs)
            If lstAttachments IsNot Nothing AndAlso lstAttachments.SelectedIndex >= 0 Then
                Dim selectedIndex = lstAttachments.SelectedIndex
                If selectedIndex >= 0 AndAlso selectedIndex < composeAttachmentPaths.Count Then
                    composeAttachmentPaths.RemoveAt(selectedIndex)
                    lstAttachments.Items.RemoveAt(selectedIndex)
                End If
            End If
        End Sub

        Private Sub SetStatus(text As String)
            If lblStatus IsNot Nothing Then
                lblStatus.Text = If(Not Equals(text, Nothing), text, "").Trim().ToUpperInvariant()
            End If
        End Sub

        Private Shared Function MakeFieldLabel(caption As String) As Label
            Dim label As Label = New Label()
            label.AutoSize = False
            Dim size As Size = New Size(72, 24)
            label.Size = size
            label.TextAlign = ContentAlignment.MiddleRight
            label.ForeColor = Color.Orange
            label.BackColor = Color.Black
            label.Font = LcarsUiFont(10F)
            label.Text = caption.ToUpperInvariant()
            Return label
        End Function

        Private Shared Function MakeFieldTextBox() As TextBox
            Dim textBox As TextBox = New TextBox()
            textBox.BorderStyle = BorderStyle.FixedSingle
            textBox.BackColor = Color.FromArgb(40, 40, 40)
            textBox.ForeColor = Color.White
            Return textBox
        End Function

        Private Shared Function MakeActionButton(name As String, caption As String, color As LCARScolorStyles) As FlatButton
            Dim val As FlatButton = New FlatButton()
            CType(CObj(val), Control).Name = name
            CType(val, LCARSbuttonClass).ButtonText = caption
            CType(val, LCARSbuttonClass).Text = caption
            CType(val, LCARSbuttonClass).Clickable = True
            CType(val, LCARSbuttonClass).Beeping = True
            CType(val, LCARSbuttonClass).Color = color
            CType(val, LCARSbuttonClass).ButtonTextAlign = ContentAlignment.BottomRight
            CType(val, LCARSbuttonClass).ButtonTextHeight = 12
            Dim size As Size = New Size(100, 28)
            CType(CObj(val), Control).Size = size
            Return val
        End Function

        Private Shared Function LcarsUiFont(size As Single, Optional style As FontStyle = FontStyle.Bold) As Font
            Dim result As Font
            Try
                result = New Font("Arial Narrow", size, style, GraphicsUnit.Point)
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                result = New Font("Segoe UI", size, style, GraphicsUnit.Point)
                Call ProjectData.ClearProjectError()
            End Try
            Return result
        End Function

        Private Class CSharpImpl
            Shared Function __Assign(Of T)(ByRef target As T, value As T) As T
                target = value
                Return value
            End Function
        End Class
    End Class

