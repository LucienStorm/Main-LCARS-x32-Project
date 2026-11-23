' LCARSbuttons/LCARSbuttons/Diagnostics/EventLogDetailForm.vb
Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Windows.Forms
Imports LCARS.Controls

Namespace Diagnostics

''' <summary>LCARS-themed popup showing a full event log entry.</summary>
Public Class EventLogDetailForm
    Inherits LCARS.LCARSForm

    Private ReadOnly scroll As LcarsScrollPanel
    Private ReadOnly bodyLabel As New Label()
    Private ReadOnly fbClose As FlatButton

    Public Sub New(ByVal title As String, ByVal body As String)
        MyBase.New()
        Text = If(title, "EVENT DETAIL")
        FormBorderStyle = FormBorderStyle.None
        BackColor = Color.Black
        StartPosition = FormStartPosition.CenterParent
        Size = New Size(560, 420)
        BindToWorkingArea = False

        Dim fbTitle As New FlatButton()
        fbTitle.ButtonText = "EVENT DETAIL"
        fbTitle.Text = "EVENT DETAIL"
        fbTitle.Clickable = False
        fbTitle.Color = LCARS.LCARScolorStyles.StaticTan
        fbTitle.ButtonTextAlign = ContentAlignment.MiddleLeft
        fbTitle.Location = New Point(12, 12)
        fbTitle.Size = New Size(420, 28)
        Controls.Add(fbTitle)

        fbClose = New FlatButton()
        fbClose.ButtonText = "CLOSE"
        fbClose.Text = "CLOSE"
        fbClose.Clickable = True
        fbClose.Beeping = True
        fbClose.Color = LCARS.LCARScolorStyles.Orange
        fbClose.ButtonTextAlign = ContentAlignment.BottomRight
        fbClose.Size = New Size(100, 28)
        fbClose.Location = New Point(Width - 120, 12)
        AddHandler fbClose.Click, Sub() Close()
        Controls.Add(fbClose)

        scroll = New LcarsScrollPanel()
        scroll.Location = New Point(12, 48)
        scroll.Size = New Size(Width - 24, Height - 60)
        scroll.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        Controls.Add(scroll)

        bodyLabel.AutoSize = True
        bodyLabel.MaximumSize = New Size(Math.Max(100, scroll.ContentPanel.Width - 16), 0)
        bodyLabel.ForeColor = Color.Orange
        bodyLabel.BackColor = Color.Black
        bodyLabel.Font = New Font("Consolas", 10.0!, FontStyle.Regular)
        bodyLabel.Text = If(body, "")
        bodyLabel.Location = New Point(4, 4)
        scroll.ContentPanel.Controls.Add(bodyLabel)
        scroll.Relayout()

        AddHandler Me.Resize, Sub()
                                  fbClose.Location = New Point(ClientSize.Width - 120, 12)
                                  scroll.Size = New Size(ClientSize.Width - 24, ClientSize.Height - 60)
                                  bodyLabel.MaximumSize = New Size(Math.Max(100, scroll.ContentPanel.Width - 16), 0)
                                  scroll.Relayout()
                              End Sub
    End Sub
End Class

End Namespace
