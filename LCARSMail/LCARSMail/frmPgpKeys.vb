Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports LCARS
Imports LCARS.Controls
    Public Class frmPgpKeys
        Inherits LCARSForm
        Private lstKeys As ListBox

        Private lblStatus As Label

        Public Sub New()
            Dim clientSize As Size = New Size(640, 400)
            CType(CObj(Me), Form).ClientSize = clientSize
            CType(CObj(Me), Form).StartPosition = FormStartPosition.CenterParent
            CType(CObj(Me), Form).BackColor = Color.Black
            BuildUi()
            RefreshKeys()
        End Sub

        Private Sub BuildUi()
            Dim val As FlatButton = New FlatButton()
            CType(val, LCARSbuttonClass).ButtonText = "PGP KEYS"
            CType(val, LCARSbuttonClass).Text = "PGP KEYS"
            CType(val, LCARSbuttonClass).Clickable = False
            CType(val, LCARSbuttonClass).Color = CType(8, LCARScolorStyles)
            Dim size As Size = New Size(120, 32)
            CType(CObj(val), Control).Size = size
            Dim location As Point = New Point(12, 8)
            CType(CObj(val), Control).Location = location
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val), Control))
            Dim val2 As FlatButton = New FlatButton()
            CType(val2, LCARSbuttonClass).ButtonText = "IMPORT"
            CType(val2, LCARSbuttonClass).Text = "IMPORT"
            CType(val2, LCARSbuttonClass).Color = CType(6, LCARScolorStyles)
            size = New Size(100, 28)
            CType(CObj(val2), Control).Size = size
            location = New Point(140, 10)
            CType(CObj(val2), Control).Location = location
            AddHandler CType(CObj(val2), Control).Click, AddressOf OnImport
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val2), Control))
            Dim val3 As FlatButton = New FlatButton()
            CType(val3, LCARSbuttonClass).ButtonText = "WKD"
            CType(val3, LCARSbuttonClass).Text = "WKD"
            CType(val3, LCARSbuttonClass).Color = CType(1, LCARScolorStyles)
            size = New Size(72, 28)
            CType(CObj(val3), Control).Size = size
            location = New Point(248, 10)
            CType(CObj(val3), Control).Location = location
            AddHandler CType(CObj(val3), Control).Click, AddressOf OnWkdLookup
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val3), Control))
            Dim val4 As FlatButton = New FlatButton()
            CType(val4, LCARSbuttonClass).ButtonText = "CLOSE"
            CType(val4, LCARSbuttonClass).Text = "CLOSE"
            CType(val4, LCARSbuttonClass).Color = CType(9, LCARScolorStyles)
            size = New Size(100, 28)
            CType(CObj(val4), Control).Size = size
            location = New Point(520, 10)
            CType(CObj(val4), Control).Location = location
            AddHandler CType(CObj(val4), Control).Click, Sub(s, e) Call CType(CObj(Me), Form).Close()
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val4), Control))
            lstKeys = New ListBox()
            Dim listBox = lstKeys
            location = New Point(12, 48)
            listBox.Location = location
            Dim listBox2 = lstKeys
            size = New Size(616, 300)
            listBox2.Size = size
            lstKeys.BackColor = Color.FromArgb(30, 30, 30)
            lstKeys.ForeColor = Color.Orange
            CType(CObj(Me), Control).Controls.Add(lstKeys)
            lblStatus = New Label()
            lblStatus.ForeColor = Color.Orange
            Dim label = lblStatus
            location = New Point(12, 360)
            label.Location = location
            Dim label2 = lblStatus
            size = New Size(616, 24)
            label2.Size = size
            CType(CObj(Me), Control).Controls.Add(lblStatus)
        End Sub

        Private Sub RefreshKeys()
            lstKeys.Items.Clear()
            For Each item In MailPgpService.ListPublicKeyLabels()
                lstKeys.Items.Add(item)
            Next
            lblStatus.Text = lstKeys.Items.Count.ToString() & " PUBLIC KEY(S) — RING: " & MailPgpService.GnupgHomeDirectory()
        End Sub

        Private Sub OnWkdLookup(sender As Object, e As EventArgs)
            Dim text = LcarsCommsUi.Prompt("Email for WKD lookup:", "LCARS COMMS — WKD")
            If Not String.IsNullOrWhiteSpace(text) Then
                If MailAutocryptService.TryImportWkdForEmail(text.Trim()) Then
                    RefreshKeys()
                    lblStatus.Text = "WKD KEY IMPORTED"
                Else
                    Dim text2 As String = If(Not Equals(MailAutocryptService.LastError, Nothing), MailAutocryptService.LastError, "WKD FAILED").ToUpperInvariant()
                    lblStatus.Text = text2
                    LcarsCommsUi.Alert(text2, "LCARS COMMS — PGP ERROR")
                End If
            End If
        End Sub

        Private Sub OnImport(sender As Object, e As EventArgs)
            Dim openFileDialog As OpenFileDialog = New OpenFileDialog()
            openFileDialog.Filter = "PGP keys (*.asc;*.gpg)|*.asc;*.gpg|All files|*.*"
            If openFileDialog.ShowDialog(CType(Me, IWin32Window)) = DialogResult.OK Then
                Dim num = MailPgpService.ImportArmoredKeyFile(openFileDialog.FileName)
                If num < 0 Then
                    lblStatus.Text = If(Not Equals(MailPgpService.LastError, Nothing), MailPgpService.LastError, "IMPORT FAILED").ToUpperInvariant()
                    Return
                End If
                RefreshKeys()
                lblStatus.Text = "KEY IMPORTED"
            End If
        End Sub
    End Class

