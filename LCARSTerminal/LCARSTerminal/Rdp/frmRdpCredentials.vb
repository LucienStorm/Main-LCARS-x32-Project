' LCARSTerminal/Rdp/frmRdpCredentials.vb
Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Windows.Forms
Imports LCARS.Controls

''' <summary>
''' Simple LCARS-styled credential prompt for RDP connect.
''' </summary>
Public Class frmRdpCredentials
    Inherits Form

    Private ReadOnly txtUser As New TextBox()
    Private ReadOnly txtPass As New TextBox()
    Private ReadOnly chkRemember As New CheckBox()
    Private ReadOnly fbOk As New FlatButton()
    Private ReadOnly fbCancel As New FlatButton()

    Public Property UserName As String
        Get
            Return txtUser.Text.Trim()
        End Get
        Set(ByVal value As String)
            txtUser.Text = If(value, "")
        End Set
    End Property

    Public Property Password As String
        Get
            Return txtPass.Text
        End Get
        Set(ByVal value As String)
            txtPass.Text = If(value, "")
        End Set
    End Property

    Public Property RememberPassword As Boolean
        Get
            Return chkRemember.Checked
        End Get
        Set(ByVal value As Boolean)
            chkRemember.Checked = value
        End Set
    End Property

    Public Sub New()
        Text = "RDP CREDENTIALS"
        FormBorderStyle = FormBorderStyle.FixedDialog
        StartPosition = FormStartPosition.CenterParent
        BackColor = Color.Black
        ForeColor = Color.Orange
        ClientSize = New Size(420, 220)
        MaximizeBox = False
        MinimizeBox = False
        ShowInTaskbar = False

        Dim lblUser As New Label() With {.Text = "USERNAME", .ForeColor = Color.Orange, .Location = New Point(20, 20), .AutoSize = True}
        Dim lblPass As New Label() With {.Text = "PASSWORD", .ForeColor = Color.Orange, .Location = New Point(20, 70), .AutoSize = True}
        txtUser.Location = New Point(20, 40)
        txtUser.Size = New Size(380, 24)
        txtUser.BackColor = Color.FromArgb(40, 40, 40)
        txtUser.ForeColor = Color.White
        txtPass.Location = New Point(20, 90)
        txtPass.Size = New Size(380, 24)
        txtPass.UseSystemPasswordChar = True
        txtPass.BackColor = Color.FromArgb(40, 40, 40)
        txtPass.ForeColor = Color.White
        chkRemember.Text = "REMEMBER (WINDOWS CREDENTIAL MANAGER)"
        chkRemember.ForeColor = Color.Orange
        chkRemember.Location = New Point(20, 125)
        chkRemember.AutoSize = True
        chkRemember.BackColor = Color.Black

        fbOk.ButtonText = "CONNECT"
        fbOk.Text = "CONNECT"
        fbOk.Color = LCARS.LCARScolorStyles.PrimaryFunction
        fbOk.Size = New Size(120, 28)
        fbOk.Location = New Point(160, 170)
        fbOk.Beeping = True
        AddHandler fbOk.Click, AddressOf OnOk

        fbCancel.ButtonText = "CANCEL"
        fbCancel.Text = "CANCEL"
        fbCancel.Color = LCARS.LCARScolorStyles.NavigationFunction
        fbCancel.Size = New Size(120, 28)
        fbCancel.Location = New Point(290, 170)
        fbCancel.Beeping = True
        AddHandler fbCancel.Click, AddressOf OnCancel

        Controls.Add(lblUser)
        Controls.Add(txtUser)
        Controls.Add(lblPass)
        Controls.Add(txtPass)
        Controls.Add(chkRemember)
        Controls.Add(fbOk)
        Controls.Add(fbCancel)
        AcceptButton = Nothing
    End Sub

    Private Sub OnOk(ByVal sender As Object, ByVal e As EventArgs)
        DialogResult = DialogResult.OK
        Close()
    End Sub

    Private Sub OnCancel(ByVal sender As Object, ByVal e As EventArgs)
        DialogResult = DialogResult.Cancel
        Close()
    End Sub
End Class
