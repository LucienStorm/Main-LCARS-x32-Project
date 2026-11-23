Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports LCARS
Imports LCARS.Controls
    Public Class frmOfflineSettings
        Inherits LCARSForm
        Private txtBodyDays As TextBox

        Private txtHeaderDays As TextBox

        Private lblStatus As Label

        Private ReadOnly purgeAccountId As String

        Public Sub New()
            Me.New(Nothing)
        End Sub

        Public Sub New(accountId As String)
            purgeAccountId = If(Not Equals(accountId, Nothing), accountId, "").Trim()
            CType(CObj(Me), Form).BackColor = Color.Black
            Dim clientSize As Size = New Size(420, 240)
            CType(CObj(Me), Form).ClientSize = clientSize
            CType(CObj(Me), Form).StartPosition = FormStartPosition.CenterParent
            BuildUi()
            txtBodyDays.Text = MailOfflinePolicy.BodyRetentionDays().ToString()
            txtHeaderDays.Text = MailOfflinePolicy.HeaderRetentionDays().ToString()
        End Sub

        Private Sub BuildUi()
            AddLabel("BODY RETENTION (DAYS)", 12, 16)
            txtBodyDays = AddField(12, 36, 380)
            AddLabel("HEADER RETENTION (DAYS)", 12, 68)
            txtHeaderDays = AddField(12, 88, 380)
            Dim val As FlatButton = New FlatButton()
            CType(val, LCARSbuttonClass).ButtonText = "SAVE"
            CType(val, LCARSbuttonClass).Text = "SAVE"
            CType(val, LCARSbuttonClass).Color = CType(6, LCARScolorStyles)
            Dim size As Size = New Size(90, 28)
            CType(CObj(val), Control).Size = size
            Dim location As Point = New Point(12, 128)
            CType(CObj(val), Control).Location = location
            AddHandler CType(CObj(val), Control).Click, AddressOf OnSave
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val), Control))
            Dim val2 As FlatButton = New FlatButton()
            CType(val2, LCARSbuttonClass).ButtonText = "PURGE NOW"
            CType(val2, LCARSbuttonClass).Text = "PURGE NOW"
            CType(val2, LCARSbuttonClass).Color = CType(1, LCARScolorStyles)
            size = New Size(110, 28)
            CType(CObj(val2), Control).Size = size
            location = New Point(112, 128)
            CType(CObj(val2), Control).Location = location
            AddHandler CType(CObj(val2), Control).Click, AddressOf OnPurgeNow
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val2), Control))
            lblStatus = New Label()
            lblStatus.ForeColor = Color.Orange
            Dim label = lblStatus
            location = New Point(12, 200)
            label.Location = location
            Dim label2 = lblStatus
            size = New Size(380, 24)
            label2.Size = size
            CType(CObj(Me), Control).Controls.Add(lblStatus)
        End Sub

        Private Sub OnSave(sender As Object, e As EventArgs)
            Dim result = 0
            Dim result2 = 0
            If Not Integer.TryParse(txtBodyDays.Text.Trim(), result) Then
                result = 90
            End If
            If Not Integer.TryParse(txtHeaderDays.Text.Trim(), result2) Then
                result2 = 365
            End If
            MailOfflinePolicy.SavePolicy(result, result2)
            lblStatus.Text = "POLICY SAVED — BODY " & result.ToString() & "D / HDR " & result2.ToString() & "D"
        End Sub

        Private Sub OnPurgeNow(sender As Object, e As EventArgs)
            OnSave(Nothing, EventArgs.Empty)
            If purgeAccountId.Length = 0 Then
                lblStatus.Text = "OPEN CACHE FROM MAIL WITH ACCOUNT"
                Return
            End If
            MailOfflinePolicy.ApplyBodyRetention(purgeAccountId)
            MailOfflinePolicy.ApplyHeaderRetention(purgeAccountId)
            lblStatus.Text = "RETENTION PURGE APPLIED FOR ACCOUNT"
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

