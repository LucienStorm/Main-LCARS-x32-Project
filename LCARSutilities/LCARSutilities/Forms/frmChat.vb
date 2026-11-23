Imports System
Imports System.Drawing
Imports System.Runtime.CompilerServices
Imports System.Windows.Forms
Imports LCARS
Imports Microsoft.VisualBasic.CompilerServices

    Public Class frmChat
        Inherits LCARSForm
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
            modUtilities.InitUtilityFormFloating(Me, 720, 520)
            BuildUi()
        End Sub

        Private Sub BuildUi()
            modUtilities.MakeUtilityTitle(Me, "CHAT IRC", 420)
            modUtilities.MakeUtilityButton(Me, "CONNECT", 418, 12, 110, 28, LCARScolorStyles.PrimaryFunction, New EventHandler(AddressOf OnConnect))
            modUtilities.MakeUtilityButton(Me, "CLOSE", 588, 12, 110, 28, LCARScolorStyles.Orange, Sub(s, e) Close())
            txtServer = MakeField(48, "irc.libera.chat:6667")
            txtNick = MakeField(78, "lcars" & New Random().Next(1000, 9999).ToString())
            txtChannel = MakeField(108, "#test")
            txtTranscript = New TextBox()
            txtTranscript.Multiline = True
            txtTranscript.ReadOnly = True
            txtTranscript.ScrollBars = ScrollBars.Vertical
            Dim textBox = txtTranscript
            Dim location As Point = New Point(12, 140)
            textBox.Location = location
            Dim textBox2 = txtTranscript
            Dim size As Size = New Size(696, 300)
            textBox2.Size = size
            txtTranscript.BackColor = Color.FromArgb(20, 20, 20)
            txtTranscript.ForeColor = Color.Orange
            Controls.Add(txtTranscript)
            txtSend = MakeField(452, "")
            txtSend.Width = 586
            AddHandler txtSend.KeyDown, AddressOf OnSendKey
            modUtilities.MakeUtilityButton(Me, "SEND", 598, 448, 100, 28, LCARScolorStyles.MiscFunction, New EventHandler(AddressOf OnSendClick))
            lblStatus = modUtilities.MakeUtilityLabel(Me, 12, 484, 696, 20, 10F)
            lblStatus.Text = "DISCONNECTED"
        End Sub

        Private Function MakeField(y As Integer, defaultText As String) As TextBox
            Dim textBox As TextBox = New TextBox()
            Dim location As Point = New Point(12, y)
            textBox.Location = location
            textBox.Width = 696
            textBox.BackColor = Color.FromArgb(40, 40, 40)
            textBox.ForeColor = Color.White
            textBox.Text = defaultText
            Controls.Add(textBox)
            Return textBox
        End Function

        Private Sub OnConnect(sender As Object, e As EventArgs)
            Try
                If irc IsNot Nothing Then
                    irc.Dispose()
                End If
                irc = New IrcClient()
                AddHandler irc.LineReceived, AddressOf Me.OnIrcLine
                Dim array = txtServer.Text.Split(":"c)
                Dim text As String = array(0).Trim()
                Dim result = 6667
                If array.Length > 1 Then
                    Integer.TryParse(array(1), result)
                End If
                irc.Connect(text, result, txtNick.Text.Trim(), txtChannel.Text.Trim())
                lblStatus.Text = "CONNECTED " & text
            Catch ex As Exception
                ProjectData.SetProjectError(ex)
                Dim ex2 = ex
                lblStatus.Text = ex2.Message.ToUpperInvariant()
                Call ProjectData.ClearProjectError()
            End Try
        End Sub

        Private Sub OnIrcLine(line As String)
            If InvokeRequired Then
                BeginInvoke(New Action(Of String)(AddressOf OnIrcLine), line)
            Else
                txtTranscript.AppendText(line & vbCrLf)
            End If
        End Sub

        Private Sub OnSendKey(sender As Object, e As KeyEventArgs)
            If e.KeyCode = Keys.Return Then
                e.SuppressKeyPress = True
                OnSendClick(RuntimeHelpers.GetObjectValue(sender), EventArgs.Empty)
            End If
        End Sub

        Private Sub OnSendClick(sender As Object, e As EventArgs)
            If irc Is Nothing Then
                Return
            End If
            Dim text As String = txtChannel.Text.Trim()
            If text.Length <> 0 Then
                Dim text2 As String = txtSend.Text.Trim()
                If text2.Length <> 0 Then
                    irc.SendChannelMessage(text, text2)
                    txtSend.Clear()
                End If
            End If
        End Sub

        Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
            If irc IsNot Nothing Then
                irc.Dispose()
            End If
            MyBase.OnFormClosed(e)
        End Sub
    End Class
