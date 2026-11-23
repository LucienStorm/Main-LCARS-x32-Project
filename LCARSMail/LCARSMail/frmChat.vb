Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports LCARS
Imports LCARS.Controls
    Public Class frmChat
        Inherits LCARSForm
        Private txtHomeserver As TextBox

        Private txtUser As TextBox

        Private txtPassword As TextBox

        Private txtToken As TextBox

        Private txtRoom As TextBox

        Private txtMessage As TextBox

        Private txtLog As TextBox

        Private lstRooms As ListBox

        Private lblStatus As Label

        Private txtXmppHost As TextBox

        Private txtXmppPort As TextBox

        Private txtXmppJid As TextBox

        Private txtXmppPassword As TextBox

        Private txtXmppTo As TextBox

        Private txtXmppMessage As TextBox

        Public Sub New()
            CType(CObj(Me), Form).BackColor = Color.Black
            Dim clientSize As Size = New Size(640, 648)
            CType(CObj(Me), Form).ClientSize = clientSize
            CType(CObj(Me), Form).StartPosition = FormStartPosition.CenterParent
            BuildUi()
            TryLoadSavedSession()
            TryLoadXmppAccount()
        End Sub

        Private Sub BuildUi()
            AddLabel("HOMESERVER", 12, 12)
            txtHomeserver = AddField(12, 32, 610)
            AddLabel("MATRIX USER", 12, 64)
            txtUser = AddField(12, 84, 280)
            AddLabel("PASSWORD", 300, 64)
            txtPassword = AddField(300, 84, 322)
            txtPassword.UseSystemPasswordChar = True
            Dim val As FlatButton = New FlatButton()
            CType(val, LCARSbuttonClass).ButtonText = "LOGIN"
            CType(val, LCARSbuttonClass).Text = "LOGIN"
            CType(val, LCARSbuttonClass).Color = CType(6, LCARScolorStyles)
            Dim size As Size = New Size(90, 28)
            CType(CObj(val), Control).Size = size
            Dim location As Point = New Point(532, 82)
            CType(CObj(val), Control).Location = location
            AddHandler CType(CObj(val), Control).Click, AddressOf OnLogin
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val), Control))
            AddLabel("ACCESS TOKEN", 12, 116)
            txtToken = AddField(12, 136, 610)
            txtToken.UseSystemPasswordChar = True
            AddLabel("ROOM ID", 12, 168)
            txtRoom = AddField(12, 188, 400)
            lstRooms = New ListBox()
            Dim listBox = lstRooms
            location = New Point(420, 188)
            listBox.Location = location
            Dim listBox2 = lstRooms
            size = New Size(202, 80)
            listBox2.Size = size
            lstRooms.BackColor = Color.FromArgb(30, 30, 30)
            lstRooms.ForeColor = Color.Orange
            AddHandler lstRooms.SelectedIndexChanged, AddressOf OnRoomSelected
            CType(CObj(Me), Control).Controls.Add(lstRooms)
            txtMessage = AddField(12, 220, 500)
            Dim val2 As FlatButton = New FlatButton()
            CType(val2, LCARSbuttonClass).ButtonText = "SEND"
            CType(val2, LCARSbuttonClass).Text = "SEND"
            CType(val2, LCARSbuttonClass).Color = CType(6, LCARScolorStyles)
            size = New Size(90, 28)
            CType(CObj(val2), Control).Size = size
            location = New Point(532, 218)
            CType(CObj(val2), Control).Location = location
            AddHandler CType(CObj(val2), Control).Click, AddressOf OnSend
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val2), Control))
            Dim val3 As FlatButton = New FlatButton()
            CType(val3, LCARSbuttonClass).ButtonText = "PULL"
            CType(val3, LCARSbuttonClass).Text = "PULL"
            CType(val3, LCARSbuttonClass).Color = CType(1, LCARScolorStyles)
            size = New Size(90, 28)
            CType(CObj(val3), Control).Size = size
            location = New Point(532, 252)
            CType(CObj(val3), Control).Location = location
            AddHandler CType(CObj(val3), Control).Click, AddressOf OnPull
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val3), Control))
            Dim val4 As FlatButton = New FlatButton()
            CType(val4, LCARSbuttonClass).ButtonText = "ROOMS"
            CType(val4, LCARSbuttonClass).Text = "ROOMS"
            CType(val4, LCARSbuttonClass).Color = CType(2, LCARScolorStyles)
            size = New Size(90, 28)
            CType(CObj(val4), Control).Size = size
            location = New Point(532, 286)
            CType(CObj(val4), Control).Location = location
            AddHandler CType(CObj(val4), Control).Click, AddressOf OnListRooms
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val4), Control))
            txtLog = New TextBox()
            txtLog.Multiline = True
            txtLog.ScrollBars = ScrollBars.Vertical
            txtLog.ReadOnly = True
            Dim textBox = txtLog
            location = New Point(12, 276)
            textBox.Location = location
            Dim textBox2 = txtLog
            size = New Size(510, 120)
            textBox2.Size = size
            txtLog.BackColor = Color.FromArgb(30, 30, 30)
            txtLog.ForeColor = Color.Orange
            CType(CObj(Me), Control).Controls.Add(txtLog)
            AddLabel("XMPP HOST", 12, 408)
            txtXmppHost = AddField(12, 428, 200)
            AddLabel("PORT", 220, 408)
            txtXmppPort = AddField(220, 428, 60)
            txtXmppPort.Text = "5222"
            AddLabel("JID", 290, 408)
            txtXmppJid = AddField(290, 428, 332)
            AddLabel("XMPP PASSWORD", 12, 456)
            txtXmppPassword = AddField(12, 476, 280)
            txtXmppPassword.UseSystemPasswordChar = True
            AddLabel("TO JID", 300, 456)
            txtXmppTo = AddField(300, 476, 322)
            txtXmppMessage = AddField(12, 508, 500)
            Dim val5 As FlatButton = New FlatButton()
            CType(val5, LCARSbuttonClass).ButtonText = "XMPP SEND"
            CType(val5, LCARSbuttonClass).Text = "XMPP SEND"
            CType(val5, LCARSbuttonClass).Color = CType(3, LCARScolorStyles)
            size = New Size(110, 28)
            CType(CObj(val5), Control).Size = size
            location = New Point(520, 506)
            CType(CObj(val5), Control).Location = location
            AddHandler CType(CObj(val5), Control).Click, AddressOf OnXmppSend
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val5), Control))
            Dim val6 As FlatButton = New FlatButton()
            CType(val6, LCARSbuttonClass).ButtonText = "XMPP PULL"
            CType(val6, LCARSbuttonClass).Text = "XMPP PULL"
            CType(val6, LCARSbuttonClass).Color = CType(2, LCARScolorStyles)
            size = New Size(110, 28)
            CType(CObj(val6), Control).Size = size
            location = New Point(520, 474)
            CType(CObj(val6), Control).Location = location
            AddHandler CType(CObj(val6), Control).Click, AddressOf OnXmppPull
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val6), Control))
            Dim val7 As FlatButton = New FlatButton()
            CType(val7, LCARSbuttonClass).ButtonText = "ROSTER"
            CType(val7, LCARSbuttonClass).Text = "ROSTER"
            CType(val7, LCARSbuttonClass).Color = CType(4, LCARScolorStyles)
            size = New Size(110, 28)
            CType(CObj(val7), Control).Size = size
            location = New Point(400, 506)
            CType(CObj(val7), Control).Location = location
            AddHandler CType(CObj(val7), Control).Click, AddressOf OnXmppRoster
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val7), Control))
            Dim label As Label = New Label()
            label.ForeColor = Color.Gray
            label.Font = New Font(label.Font.FontFamily, 8F)
            location = New Point(12, 540)
            label.Location = location
            size = New Size(610, 36)
            label.Size = size
            label.Text = "OTR SLICE 18 OPEN — Ruling: no libotr on net48 x86 (open-slice-acceptance-gaps-317.md). Matrix / TLS XMPP are substitutes only."
            CType(CObj(Me), Control).Controls.Add(label)
            lblStatus = New Label()
            lblStatus.ForeColor = Color.Orange
            Dim label2 = lblStatus
            location = New Point(12, 578)
            label2.Location = location
            Dim label3 = lblStatus
            size = New Size(610, 24)
            label3.Size = size
            CType(CObj(Me), Control).Controls.Add(lblStatus)
        End Sub

        Private Sub TryLoadSavedSession()
            Dim homeserver As String = Nothing
            Dim userId As String = Nothing
            Dim accessToken As String = Nothing
            If MatrixAccountStore.TryLoadSession(homeserver, userId, accessToken) Then
                txtHomeserver.Text = homeserver
                txtUser.Text = userId
                txtToken.Text = accessToken
            End If
        End Sub

        Private Sub OnLogin(sender As Object, e As EventArgs)
            Dim accessToken As String = Nothing
            Dim userId As String = Nothing
            If Not MatrixChatService.TryPasswordLogin(txtHomeserver.Text, txtUser.Text, txtPassword.Text, accessToken, userId) Then
                lblStatus.Text = If(Not Equals(MatrixChatService.LastError, Nothing), MatrixChatService.LastError, "LOGIN FAILED").ToUpperInvariant()
                Return
            End If
            txtToken.Text = accessToken
            If Not String.IsNullOrWhiteSpace(userId) Then
                txtUser.Text = userId
            End If
            MatrixAccountStore.SaveSession(txtHomeserver.Text, txtUser.Text, accessToken)
            lblStatus.Text = "LOGGED IN"
            OnListRooms(Nothing, EventArgs.Empty)
        End Sub

        Private Sub OnListRooms(sender As Object, e As EventArgs)
            lstRooms.Items.Clear()
            Dim list = MatrixChatService.TryListJoinedRooms(txtHomeserver.Text, txtToken.Text)
            If list Is Nothing Then
                lblStatus.Text = If(Not Equals(MatrixChatService.LastError, Nothing), MatrixChatService.LastError, "ROOMS FAILED").ToUpperInvariant()
                Return
            End If
            For Each item In list
                If item IsNot Nothing Then
                    lstRooms.Items.Add(item)
                End If
            Next
            lblStatus.Text = list.Count.ToString() & " ROOM(S)"
        End Sub

        Private Sub OnRoomSelected(sender As Object, e As EventArgs)
            Dim matrixRoomSummary As MatrixChatService.MatrixRoomSummary = Nothing

            If CSharpImpl.__Assign(matrixRoomSummary, TryCast(lstRooms.SelectedItem, MatrixChatService.MatrixRoomSummary)) IsNot Nothing Then
                txtRoom.Text = matrixRoomSummary.RoomId
            End If
        End Sub

        Private Sub OnSend(sender As Object, e As EventArgs)
            If MatrixChatService.TrySendText(txtHomeserver.Text, txtToken.Text, txtRoom.Text, txtMessage.Text) Then
                lblStatus.Text = "SENT"
                txtMessage.Clear()
            Else
                lblStatus.Text = If(Not Equals(MatrixChatService.LastError, Nothing), MatrixChatService.LastError, "SEND FAILED").ToUpperInvariant()
            End If
        End Sub

        Private Sub OnPull(sender As Object, e As EventArgs)
            Dim text = MatrixChatService.TryFetchRecentMessages(txtHomeserver.Text, txtToken.Text, txtRoom.Text)
            txtLog.Text = MatrixChatService.FormatRecentMessages(If(Not Equals(text, Nothing), text, ""))
            lblStatus.Text = "MESSAGES PULLED"
        End Sub

        Private Sub TryLoadXmppAccount()
            Dim host As String = Nothing
            Dim jid As String = Nothing
            Dim password As String = Nothing
            Dim port = 5222
            If XmppAccountStore.TryLoadAccount(host, port, jid, password) Then
                txtXmppHost.Text = host
                txtXmppPort.Text = port.ToString()
                txtXmppJid.Text = jid
                If Not Equals(password, Nothing) Then
                    txtXmppPassword.Text = password
                End If
            End If
        End Sub

        Private Sub OnXmppRoster(sender As Object, e As EventArgs)
            Dim result = 5222
            Call Integer.TryParse(txtXmppPort.Text.Trim(), result)
            If result <= 0 Then
                result = 5222
            End If
            Dim list = XmppChatService.TryFetchRosterJids(txtXmppHost.Text, result, txtXmppJid.Text, txtXmppPassword.Text)
            If list Is Nothing OrElse list.Count = 0 Then
                lblStatus.Text = If(Not Equals(XmppChatService.LastError, Nothing), XmppChatService.LastError, "ROSTER FAILED").ToUpperInvariant()
                Return
            End If
            txtLog.Text = String.Join(vbCrLf, list.ToArray())
            lblStatus.Text = list.Count.ToString() & " ROSTER ENTR(Y/IES)"
        End Sub

        Private Sub OnXmppPull(sender As Object, e As EventArgs)
            Dim result = 5222
            Call Integer.TryParse(txtXmppPort.Text.Trim(), result)
            If result <= 0 Then
                result = 5222
            End If
            XmppAccountStore.SaveAccount(txtXmppHost.Text, result, txtXmppJid.Text, txtXmppPassword.Text)
            Dim text = XmppChatService.TryPullIncomingMessages(txtXmppHost.Text, result, txtXmppJid.Text, txtXmppPassword.Text, 8)
            If Equals(text, Nothing) OrElse text.Length = 0 Then
                lblStatus.Text = If(Not Equals(XmppChatService.LastError, Nothing), XmppChatService.LastError, "XMPP PULL FAILED").ToUpperInvariant()
                Return
            End If
            txtLog.Text = text
            lblStatus.Text = "XMPP MESSAGES"
        End Sub

        Private Sub OnXmppSend(sender As Object, e As EventArgs)
            Dim result = 5222
            Call Integer.TryParse(txtXmppPort.Text.Trim(), result)
            If result <= 0 Then
                result = 5222
            End If
            XmppAccountStore.SaveAccount(txtXmppHost.Text, result, txtXmppJid.Text, txtXmppPassword.Text)
            If XmppChatService.TrySendMessage(txtXmppHost.Text, result, txtXmppJid.Text, txtXmppPassword.Text, txtXmppTo.Text, txtXmppMessage.Text) Then
                lblStatus.Text = "XMPP SENT"
                txtXmppMessage.Clear()
            Else
                lblStatus.Text = If(Not Equals(XmppChatService.LastError, Nothing), XmppChatService.LastError, "XMPP FAILED").ToUpperInvariant()
            End If
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

        Private Class CSharpImpl
            Shared Function __Assign(Of T)(ByRef target As T, value As T) As T
                target = value
                Return value
            End Function
        End Class
    End Class

