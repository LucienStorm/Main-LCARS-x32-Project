' LCARSbuttons/LCARSbuttons/Diagnostics/PowerPanel.vb
Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms
Imports LCARS.Controls

Namespace Diagnostics

''' <summary>Battery status and power plan selection.</summary>
Public Class PowerPanel
    Inherits Panel

    Private ReadOnly lblSummary As New Label()
    Private ReadOnly lstPlans As New ListBox()
    Private ReadOnly status As New Label()
    Private fbRefresh As FlatButton
    Private fbApply As FlatButton
    Private fbOpen As FlatButton

    Public Sub New()
        BackColor = Color.Black
        Dock = DockStyle.Fill

        lblSummary.ForeColor = Color.Orange
        lblSummary.BackColor = Color.Black
        lblSummary.Font = New Font("Arial Narrow", 12.0!, FontStyle.Bold)
        lblSummary.AutoSize = False

        lstPlans.BackColor = Color.Black
        lstPlans.ForeColor = Color.Orange
        lstPlans.BorderStyle = BorderStyle.FixedSingle
        lstPlans.Font = New Font("Arial Narrow", 11.0!, FontStyle.Bold)
        lstPlans.IntegralHeight = False

        status.ForeColor = Color.Orange
        status.BackColor = Color.Black
        status.Font = New Font("Arial Narrow", 10.0!, FontStyle.Bold)
        status.Text = "READY"
        status.AutoSize = False

        fbRefresh = MakeButton("REFRESH", LCARS.LCARScolorStyles.PrimaryFunction)
        AddHandler fbRefresh.Click, Sub() Reload()
        fbApply = MakeButton("SET ACTIVE", LCARS.LCARScolorStyles.PrimaryFunction)
        AddHandler fbApply.Click, AddressOf OnApply
        fbOpen = MakeButton("POWER CPL", LCARS.LCARScolorStyles.MiscFunction)
        AddHandler fbOpen.Click, Sub() ShellProcess.StartControlPanel("powercfg.cpl")

        Controls.Add(lblSummary)
        Controls.Add(lstPlans)
        Controls.Add(status)
        Controls.Add(fbRefresh)
        Controls.Add(fbApply)
        Controls.Add(fbOpen)

        AddHandler Me.Resize, Sub() LayoutChildren()
        AddHandler Me.VisibleChanged, Sub()
                                          If Visible Then Reload()
                                      End Sub
        LayoutChildren()
    End Sub

    Private Function MakeButton(ByVal caption As String, ByVal color As LCARS.LCARScolorStyles) As FlatButton
        Dim b As New FlatButton()
        b.ButtonText = caption
        b.Text = caption
        b.Clickable = True
        b.Beeping = True
        b.Color = color
        b.ButtonTextAlign = ContentAlignment.BottomRight
        b.Size = New Size(110, 26)
        Return b
    End Function

    Private Sub LayoutChildren()
        Dim pad As Integer = 6
        Dim y As Integer = pad
        fbRefresh.Location = New Point(pad, y)
        fbApply.Location = New Point(pad + 118, y)
        fbOpen.Location = New Point(pad + 236, y)
        y += 32
        lblSummary.SetBounds(pad, y, Math.Max(80, ClientSize.Width - pad * 2), 28)
        y += 32
        status.SetBounds(pad, Math.Max(y + 20, ClientSize.Height - 22), Math.Max(80, ClientSize.Width - pad * 2), 18)
        lstPlans.SetBounds(pad, y, Math.Max(80, ClientSize.Width - pad * 2), Math.Max(40, status.Top - y - 4))
    End Sub

    Public Sub Reload()
        lblSummary.Text = PowerService.GetBatterySummary().ToUpperInvariant()
        Dim activeGuid As String = PowerService.GetActivePlanGuid()
        Dim plans As List(Of PowerPlanInfo) = PowerService.ListPlans()
        lstPlans.BeginUpdate()
        lstPlans.Items.Clear()
        Dim selectIndex As Integer = -1
        For i As Integer = 0 To plans.Count - 1
            Dim label As String = plans(i).Name
            If String.Equals(plans(i).Guid, activeGuid, StringComparison.OrdinalIgnoreCase) Then
                label &= "  [ACTIVE]"
                selectIndex = i
            End If
            lstPlans.Items.Add(New PlanRow(plans(i), label))
        Next
        lstPlans.EndUpdate()
        If selectIndex >= 0 Then lstPlans.SelectedIndex = selectIndex
        status.Text = plans.Count.ToString() & " POWER PLANS"
        LayoutChildren()
    End Sub

    Private Sub OnApply(ByVal sender As Object, ByVal e As EventArgs)
        Dim row As PlanRow = TryCast(lstPlans.SelectedItem, PlanRow)
        If row Is Nothing Then
            status.Text = "SELECT A PLAN"
            Return
        End If
        status.Text = PowerService.SetActivePlan(row.Plan.Guid)
        Reload()
    End Sub

    Private Class PlanRow
        Public Plan As PowerPlanInfo
        Public Label As String
        Public Sub New(ByVal p As PowerPlanInfo, ByVal l As String)
            Plan = p
            Label = l
        End Sub
        Public Overrides Function ToString() As String
            Return Label
        End Function
    End Class
End Class

End Namespace
