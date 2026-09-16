' LCARSTerminal/frmShellPicker.vb
Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Windows.Forms
Imports LCARS.Controls

''' <summary>
''' LCARS flyout listing the four shell kinds for a new tab.
''' </summary>
Public Class frmShellPicker
    Inherits LCARS.LCARSForm

    Public Event ShellChosen(ByVal kind As ShellKind, ByVal elevated As Boolean)

    Private Const BtnW As Integer = 140
    Private Const BtnH As Integer = 28
    Private Const Gap As Integer = 4

    Public Sub New()
        BindToWorkingArea = False
        FormBorderStyle = FormBorderStyle.None
        ControlBox = False
        ShowInTaskbar = False
        StartPosition = FormStartPosition.Manual
        BackColor = Color.Black
        TopMost = True

        Dim items As New List(Of Tuple(Of String, ShellKind, Boolean)) From {
            Tuple.Create("CMD", ShellKind.Cmd, False),
            Tuple.Create("CMD ADMIN", ShellKind.Cmd, True),
            Tuple.Create("POWERSHELL", ShellKind.PowerShell, False),
            Tuple.Create("PS ADMIN", ShellKind.PowerShell, True)
        }

        Dim y As Integer = Gap
        For Each item In items
            Dim btn As FlatButton = MakeButton(item.Item1, y)
            Dim kind As ShellKind = item.Item2
            Dim elev As Boolean = item.Item3
            AddHandler btn.Click, Sub(sender As Object, e As EventArgs)
                                      RaiseEvent ShellChosen(kind, elev)
                                      Me.Close()
                                  End Sub
            Controls.Add(btn)
            y += BtnH + Gap
        Next

        ClientSize = New Size(BtnW + Gap * 2, y + Gap)
        AddHandler Me.Deactivate, Sub(sender As Object, e As EventArgs) Me.Close()
    End Sub

    Private Function MakeButton(ByVal caption As String, ByVal top As Integer) As FlatButton
        Dim btn As New FlatButton()
        btn.ButtonText = caption
        btn.Text = caption
        btn.Beeping = True
        btn.Color = LCARS.LCARScolorStyles.PrimaryFunction
        btn.ButtonTextAlign = ContentAlignment.MiddleCenter
        btn.Size = New Size(BtnW, BtnH)
        btn.Location = New Point(Gap, top)
        Return btn
    End Function

    ''' <summary>
    ''' Positions the flyout to the right of the anchor and shows it owned by the parent.
    ''' </summary>
    Public Sub ShowBeside(ByVal owner As Form, ByVal anchor As Control)
        Dim screenPt As Point = anchor.PointToScreen(New Point(anchor.Width, 0))
        Location = screenPt
        Show(owner)
    End Sub
End Class
