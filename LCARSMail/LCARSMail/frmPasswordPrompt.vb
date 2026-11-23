Imports System
Imports System.Drawing
Imports System.Runtime.CompilerServices
Imports System.Windows.Forms
Imports LCARS
Imports LCARS.Controls
    Public Class frmPasswordPrompt
        Inherits LCARSForm
        Private ReadOnly promptText As String

        Private txtPassword As TextBox

        Private fbShow As FlatButton

        Private fbOk As FlatButton

        Private fbCancel As FlatButton

        Private obscured As Boolean

        Public Property Password As String

        Public Sub New(prompt As String, Optional title As String = "LCARS COMMS — PASSWORD")
            obscured = True
            promptText = If(Not Equals(prompt, Nothing), prompt, "")
            CType(CObj(Me), Form).Text = (If(String.IsNullOrWhiteSpace(title), "LCARS COMMS — PASSWORD", title.Trim()))
            CType(CObj(Me), Form).BackColor = Color.Black
            CType(CObj(Me), Control).ForeColor = Color.Orange
            CType(CObj(Me), Form).FormBorderStyle = FormBorderStyle.None
            CType(CObj(Me), Form).ControlBox = False
            Me.DoubleBuffered = True
            CType(Me, LCARSForm).BindToWorkingArea = False
            CType(CObj(Me), Form).StartPosition = FormStartPosition.CenterParent
            Dim clientSize As Size = New Size(440, 220)
            CType(CObj(Me), Form).ClientSize = clientSize
            CType(CObj(Me), Form).ShowInTaskbar = False
            Dim val As FlatButton = New FlatButton()
            CType(val, LCARSbuttonClass).ButtonText = CType(CObj(Me), Form).Text.ToUpperInvariant()
            CType(val, LCARSbuttonClass).Text = CType(val, LCARSbuttonClass).ButtonText
            CType(val, LCARSbuttonClass).Clickable = False
            CType(val, LCARSbuttonClass).Color = CType(8, LCARScolorStyles)
            CType(val, LCARSbuttonClass).ButtonTextAlign = ContentAlignment.BottomRight
            clientSize = New Size(416, 32)
            CType(CObj(val), Control).Size = clientSize
            Dim location As Point = New Point(12, 8)
            CType(CObj(val), Control).Location = location
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val), Control))
            Dim label As Label = New Label With {
        .AutoSize = False,
        .ForeColor = Color.Orange,
        .BackColor = Color.Black,
        .Font = New Font("LCARS", 14F, FontStyle.Regular)
    }
            location = New Point(12, 48)
            label.Location = location
            clientSize = New Size(416, 56)
            label.Size = clientSize
            label.Text = promptText
            CType(CObj(Me), Control).Controls.Add(label)
            txtPassword = New TextBox()
            Dim textBox = txtPassword
            location = New Point(12, 112)
            textBox.Location = location
            Dim textBox2 = txtPassword
            clientSize = New Size(320, 28)
            textBox2.Size = clientSize
            txtPassword.BackColor = Color.Black
            txtPassword.ForeColor = Color.Orange
            txtPassword.Font = New Font("LCARS", 14F, FontStyle.Regular)
            txtPassword.BorderStyle = BorderStyle.FixedSingle
            txtPassword.UseSystemPasswordChar = True
            CType(CObj(Me), Control).Controls.Add(txtPassword)
            location = New Point(340, 110)
            fbShow = Me.MakeBtn("SHOW", CType(4, LCARScolorStyles), location, 88)
            AddHandler CType(CObj(fbShow), Control).Click, AddressOf OnToggleObscure
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbShow), Control))
            location = New Point(248, 168)
            fbOk = Me.MakeBtn("OK", CType(6, LCARScolorStyles), location, 88)
            AddHandler CType(CObj(fbOk), Control).Click, AddressOf OnOk
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbOk), Control))
            location = New Point(340, 168)
            fbCancel = Me.MakeBtn("CANCEL", CType(3, LCARScolorStyles), location, 88)
            AddHandler CType(CObj(fbCancel), Control).Click, AddressOf OnCancel
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbCancel), Control))
            CType(CObj(Me), Form).AcceptButton = Nothing
            CType(CObj(Me), Form).CancelButton = Nothing
            AddHandler txtPassword.KeyDown, AddressOf OnPasswordKeyDown
        End Sub

        Public Shared Function Prompt(owner As IWin32Window, promptText As String, Optional title As String = "LCARS COMMS — PASSWORD") As String
            Dim frmPasswordPrompt2 As frmPasswordPrompt = New frmPasswordPrompt(promptText, title)
            Try
                Dim dialogResult As DialogResult = (If(owner Is Nothing, CType(CObj(frmPasswordPrompt2), Form).ShowDialog(), CType(CObj(frmPasswordPrompt2), Form).ShowDialog(owner)))
                If dialogResult <> DialogResult.OK Then
                    Return Nothing
                End If
                Return frmPasswordPrompt2.Password
            Finally
                If frmPasswordPrompt2 IsNot Nothing Then CType(frmPasswordPrompt2, IDisposable).Dispose()
            End Try
        End Function

        Protected Overrides Sub OnShown(e As EventArgs)
            CType(Me, Form).OnShown(e)
            txtPassword.Focus()
        End Sub

        Private Function MakeBtn(caption As String, color As LCARScolorStyles, loc As Point, width As Integer) As FlatButton
            Dim val As FlatButton = New FlatButton()
            CType(val, LCARSbuttonClass).ButtonText = caption
            CType(val, LCARSbuttonClass).Text = caption
            CType(val, LCARSbuttonClass).Color = color
            CType(val, LCARSbuttonClass).ButtonTextAlign = ContentAlignment.MiddleCenter
            Dim size As Size = New Size(width, 32)
            CType(CObj(val), Control).Size = size
            CType(CObj(val), Control).Location = loc
            Return val
        End Function

        Private Sub OnToggleObscure(sender As Object, e As EventArgs)
            obscured = Not obscured
            txtPassword.UseSystemPasswordChar = obscured
            If obscured Then
                CType(fbShow, LCARSbuttonClass).ButtonText = "SHOW"
                CType(fbShow, LCARSbuttonClass).Text = "SHOW"
            Else
                CType(fbShow, LCARSbuttonClass).ButtonText = "HIDE"
                CType(fbShow, LCARSbuttonClass).Text = "HIDE"
            End If
        End Sub

        Private Sub OnPasswordKeyDown(sender As Object, e As KeyEventArgs)
            If e.KeyCode = Keys.Return Then
                e.Handled = True
                e.SuppressKeyPress = True
                OnOk(RuntimeHelpers.GetObjectValue(sender), e)
            ElseIf e.KeyCode = Keys.Escape Then
                e.Handled = True
                e.SuppressKeyPress = True
                OnCancel(RuntimeHelpers.GetObjectValue(sender), e)
            End If
        End Sub

        Private Sub OnOk(sender As Object, e As EventArgs)
            Dim text = txtPassword.Text
            Password = If(Not Equals(text, Nothing), text, "")
            CType(CObj(Me), Form).DialogResult = DialogResult.OK
            Call CType(CObj(Me), Form).Close()
        End Sub

        Private Sub OnCancel(sender As Object, e As EventArgs)
            Password = Nothing
            CType(CObj(Me), Form).DialogResult = DialogResult.Cancel
            Call CType(CObj(Me), Form).Close()
        End Sub
    End Class

