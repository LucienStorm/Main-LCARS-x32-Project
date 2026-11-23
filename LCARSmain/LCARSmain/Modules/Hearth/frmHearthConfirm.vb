' LCARSmain/LCARSmain/Modules/Hearth/frmHearthConfirm.vb
Option Strict On
Option Explicit On
Imports System.Drawing
Imports System.Windows.Forms
Imports LCARS.Controls
''' <summary>LCARS yes/no popup
for Hearth actions
with confirm:true.</summary>
Public Class frmHearthConfirm
Inherits Form
Public Sub New(ByVal actionName As String)
MyBase.New() FormBorderStyle = FormBorderStyle.None BackColor = Color.Black StartPosition = FormStartPosition.CenterScreen Size = New Size(420, 160) TopMost = True ShowInTaskbar = False Text = "CONFIRM"
Dim title As New FlatButton()
title.ButtonText = "CONFIRM ACTION" title.Text = "CONFIRM ACTION" title.Clickable = False title.Color = LCARS.LCARScolorStyles.StaticTan title.ButtonTextAlign = ContentAlignment.MiddleLeft title.Location = New Point(12, 12)
title.Size = New Size(396, 28)
Controls.Add(title)
Dim lbl As New Label()
lbl.ForeColor = Color.Orange lbl.BackColor = Color.Black lbl.Font = New Font("LCARS", 14.0!, FontStyle.Regular, GraphicsUnit.Point)
lbl.Text = "Execute: " & actionName.Replace("_"c, " "c).ToUpperInvariant() & "?" & ControlChars.CrLf & "(Say CONFIRMED or CANCEL)" lbl.Location = New Point(12, 48)
lbl.Size = New Size(396, 50)
Controls.Add(lbl)
Dim fbYes As New FlatButton()
fbYes.ButtonText = "YES" fbYes.Text = "YES" fbYes.Clickable = True fbYes.Beeping = True fbYes.Color = LCARS.LCARScolorStyles.PrimaryFunction fbYes.Size = New Size(100, 28)
fbYes.Location = New Point(100, 110)
AddHandler fbYes.Click, Sub() DialogResult = DialogResult.Yes Close()
End Sub Controls.Add(fbYes)
Dim fbNo As New FlatButton()
fbNo.ButtonText = "NO" fbNo.Text = "NO" fbNo.Clickable = True fbNo.Beeping = True fbNo.Color = LCARS.LCARScolorStyles.Orange fbNo.Size = New Size(100, 28)
fbNo.Location = New Point(220, 110)
AddHandler fbNo.Click, Sub() DialogResult = DialogResult.No Close()
End Sub Controls.Add(fbNo)
End Sub
End Class
