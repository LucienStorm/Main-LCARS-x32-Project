' LCARSutilities/LCARSutilities/Forms/frmChat.vb
Option Strict On
Option Explicit On
Imports System.Drawing
Imports System.Windows.Forms
Public Class frmChat
Inherits LCARS.LCARSForm
Private Const FormW As Integer = 720
Private Const FormH As Integer = 520
Private txtServer As TextBox
Private txtNick As TextBox
Private txtChannel As TextBox
Private txtTranscript As TextBox
Private txtSend As TextBox
Private lblStatus As Label
Private irc As IrcClient
Public Sub New()
MyBase.New()
modUtilities.InitUtilityFormFloating(Me, FormW, FormH) BuildUi()
End Sub
Private Sub BuildUi()
modUtilities.MakeUtilityTitle(Me, "CHAT IRC", FormW - 300)
modUtilities.MakeUtilityButton(Me, "CONNECT", FormW - modUtilities.UtilityMargin - 290, modUtilities.UtilityMargin, 110, 28, LCARS.LCARScolorStyles.PrimaryFunction, AddressOf OnConnect)
modUtilities.MakeUtilityButton(Me, "CLOSE", FormW - modUtilities.UtilityMargin - 120, modUtilities.UtilityMargin, 110, 28, LCARS.LCARScolorStyles.Orange, Sub(s, e) Close()) txtServer = MakeField(48, "irc.libera.chat:6667") txtNick = MakeField(78, "lcars" & (New Random().Next(1000, 9999)).ToString()) txtChannel = MakeField(108, "#test") txtTranscript = New TextBox()
txtTranscript.Multiline = True txtTranscript.ReadOnly = True txtTranscript.ScrollBars = ScrollBars.Vertical txtTranscript.Location = New Point(modUtilities.UtilityMargin, 140)
txtTranscript.Size = New Size(FormW - modUtilities.UtilityMargin * 2, FormH - 220)
txtTranscript.BackColor = Color.FromArgb(20, 20, 20)
txtTranscript.ForeColor = Color.Orange Controls.Add(txtTranscript) txtSend = MakeField(FormH - 68, "")
txtSend.Width = FormW - modUtilities.UtilityMargin * 2 - 110
AddHandler txtSend.KeyDown, AddressOf OnSendKey modUtilities.MakeUtilityButton(Me, "SEND", FormW - modUtilities.UtilityMargin - 110, FormH - 72, 100, 28, LCARS.LCARScolorStyles.MiscFunction, AddressOf OnSendClick) lblStatus = modUtilities.MakeUtilityLabel(Me, modUtilities.UtilityMargin, FormH - 36, FormW - 24, 20, 10.0F)
lblStatus.Text = "DISCONNECTED"
End Sub
Private Function MakeField(ByVal y As Integer, ByVal defaultText As String) As TextBox
Dim txt As New TextBox()
txt.Location = New Point(modUtilities.UtilityMargin, y)
txt.Width = FormW - modUtilities.UtilityMargin * 2 txt.BackColor = Color.FromArgb(40, 40, 40)
txt.ForeColor = Color.White txt.Text = defaultText Controls.Add(txt)
Return txt
End Function
Private Sub OnConnect(ByVal sender As Object, ByVal e As EventArgs)
Try
If irc IsNot Nothing Then irc.Dispose() irc = New IrcClient()
AddHandler irc.LineReceived, AddressOf OnIrcLine
Dim hostPort As String() = txtServer.Text.Split(":"c)
Dim host As String = hostPort(0).Trim()
Dim port As Integer = 6667
If hostPort.Length > 1 Then Integer.TryParse(hostPort(1), port)
irc.Connect(host, port, txtNick.Text.Trim(), txtChannel.Text.Trim())
lblStatus.Text = "CONNECTED " & host
Catch ex As Exception lblStatus.Text = ex.Message.ToUpperInvariant()
End Try
End Sub
Private Sub OnIrcLine(ByVal line As String)
If InvokeRequired Then BeginInvoke(New Action(Of String)(AddressOf OnIrcLine), line)
Return
End If txtTranscript.AppendText(line & vbCrLf)
End Sub
Private Sub OnSendKey(ByVal sender As Object, ByVal e As KeyEventArgs)
If e.KeyCode = Keys.Enter Then e.SuppressKeyPress = True OnSendClick(sender, EventArgs.Empty)
End If
End Sub
Private Sub OnSendClick(ByVal sender As Object, ByVal e As EventArgs)
If irc Is Nothing Then Return
Dim target As String = txtChannel.Text.Trim()
If target.Length = 0 Then Return
Dim msg As String = txtSend.Text.Trim()
If msg.Length = 0 Then Return irc.SendChannelMessage(target, msg)
txtSend.Clear()
End Sub
Protected Overrides Sub OnFormClosed(ByVal e As FormClosedEventArgs)
If irc IsNot Nothing Then irc.Dispose()
MyBase.OnFormClosed(e)
End Sub
End Class
