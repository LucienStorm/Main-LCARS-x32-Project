' LCARSbuttons/LCARSbuttons/Diagnostics/DisplayPanel.vb
Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Drawing
Imports System.Diagnostics
Imports System.Windows.Forms
Imports LCARS.Controls

Namespace Diagnostics

''' <summary>Live brightness, rotation lock, resolution, orientation, Display CPL.</summary>
Public Class DisplayPanel
    Inherits Panel
    Implements IEngineeringPanelRefresh

    Private ReadOnly lblBright As New Label()
    Private ReadOnly trkBright As New System.Windows.Forms.TrackBar()
    Private ReadOnly chkLock As New CheckBox()
    Private ReadOnly lblMode As New Label()
    Private ReadOnly cboModes As New ComboBox()
    Private ReadOnly lblOrient As New Label()
    Private ReadOnly status As New Label()
    Private ReadOnly poll As New Timer()
    Private fbRefresh As FlatButton
    Private fbApplyMode As FlatButton
    Private fbOrient0 As FlatButton
    Private fbOrient90 As FlatButton
    Private fbOrient180 As FlatButton
    Private fbOrient270 As FlatButton
    Private fbProject As FlatButton
    Private fbOpen As FlatButton
    Private _suppress As Boolean = False
    Private _loaded As Boolean = False

    Public Sub New()
        BackColor = Color.Black
        Dock = DockStyle.Fill

        lblBright.ForeColor = Color.Orange
        lblBright.BackColor = Color.Black
        lblBright.Font = New Font("Arial Narrow", 12.0!, FontStyle.Bold)
        lblBright.Text = "BRIGHTNESS"
        lblBright.AutoSize = False

        trkBright.Minimum = 0
        trkBright.Maximum = 100
        trkBright.TickFrequency = 10
        trkBright.BackColor = Color.Black
        trkBright.AutoSize = False
        AddHandler trkBright.ValueChanged, AddressOf OnBrightnessChanged

        chkLock.Text = "ROTATION LOCK"
        chkLock.ForeColor = Color.Orange
        chkLock.BackColor = Color.Black
        chkLock.Font = New Font("Arial Narrow", 12.0!, FontStyle.Bold)
        chkLock.AutoSize = True
        AddHandler chkLock.CheckedChanged, AddressOf OnLockChanged

        lblMode.ForeColor = Color.Orange
        lblMode.BackColor = Color.Black
        lblMode.Font = New Font("Arial Narrow", 10.0!, FontStyle.Bold)
        lblMode.Text = "RESOLUTION"
        lblMode.AutoSize = False

        cboModes.DropDownStyle = ComboBoxStyle.DropDownList
        cboModes.BackColor = Color.Black
        cboModes.ForeColor = Color.Orange
        cboModes.Font = New Font("Arial Narrow", 10.0!, FontStyle.Bold)

        lblOrient.ForeColor = Color.Orange
        lblOrient.BackColor = Color.Black
        lblOrient.Font = New Font("Arial Narrow", 10.0!, FontStyle.Bold)
        lblOrient.Text = "ORIENTATION"
        lblOrient.AutoSize = False

        status.ForeColor = Color.Orange
        status.BackColor = Color.Black
        status.Font = New Font("Arial Narrow", 10.0!, FontStyle.Bold)
        status.Text = "READY"
        status.AutoSize = False

        fbRefresh = MakeButton("REFRESH", LCARS.LCARScolorStyles.PrimaryFunction)
        AddHandler fbRefresh.Click, Sub() SyncFromSystem(True)
        fbApplyMode = MakeButton("APPLY MODE", LCARS.LCARScolorStyles.PrimaryFunction)
        AddHandler fbApplyMode.Click, AddressOf OnApplyMode
        fbOrient0 = MakeButton("0°", LCARS.LCARScolorStyles.PrimaryFunction)
        AddHandler fbOrient0.Click, Sub() ApplyOrient(0)
        fbOrient90 = MakeButton("90°", LCARS.LCARScolorStyles.PrimaryFunction)
        AddHandler fbOrient90.Click, Sub() ApplyOrient(90)
        fbOrient180 = MakeButton("180°", LCARS.LCARScolorStyles.PrimaryFunction)
        AddHandler fbOrient180.Click, Sub() ApplyOrient(180)
        fbOrient270 = MakeButton("270°", LCARS.LCARScolorStyles.PrimaryFunction)
        AddHandler fbOrient270.Click, Sub() ApplyOrient(270)
        fbProject = MakeButton("PROJECT", LCARS.LCARScolorStyles.MiscFunction)
        AddHandler fbProject.Click, Sub()
                                        Try
                                            Process.Start("DisplaySwitch.exe")
                                        Catch
                                            status.Text = "PROJECT UNAVAILABLE"
                                        End Try
                                    End Sub
        fbOpen = MakeButton("DISPLAY CPL", LCARS.LCARScolorStyles.MiscFunction)
        AddHandler fbOpen.Click, Sub()
                                     Try
                                         Process.Start("ms-settings:display")
                                     Catch
                                         ShellProcess.StartControlPanel("desk.cpl")
                                     End Try
                                 End Sub

        poll.Interval = 500
        AddHandler poll.Tick, Sub()
                                  If Not IsHandleCreated OrElse Not Visible OrElse Height < 40 Then Return
                                  If trkBright.Capture Then Return
                                  SyncFromSystem(False)
                              End Sub

        Controls.Add(lblBright)
        Controls.Add(trkBright)
        Controls.Add(chkLock)
        Controls.Add(lblMode)
        Controls.Add(cboModes)
        Controls.Add(lblOrient)
        Controls.Add(status)
        Controls.Add(fbRefresh)
        Controls.Add(fbApplyMode)
        Controls.Add(fbOrient0)
        Controls.Add(fbOrient90)
        Controls.Add(fbOrient180)
        Controls.Add(fbOrient270)
        Controls.Add(fbProject)
        Controls.Add(fbOpen)

        AddHandler Me.Resize, Sub() LayoutChildren()
        AddHandler Me.VisibleChanged, Sub()
                                          If Visible AndAlso Height > 40 Then OnSectionShown()
                                      End Sub
        AddHandler Me.HandleDestroyed, Sub() poll.Stop()
        LayoutChildren()
    End Sub

    Public Sub OnSectionShown() Implements IEngineeringPanelRefresh.OnSectionShown
        SyncFromSystem(True)
        poll.Start()
    End Sub

    Private Function MakeButton(ByVal caption As String, ByVal color As LCARS.LCARScolorStyles) As FlatButton
        Dim b As New FlatButton()
        b.ButtonText = caption
        b.Text = caption
        b.Clickable = True
        b.Beeping = True
        b.Color = color
        b.ButtonTextAlign = ContentAlignment.BottomRight
        b.Size = New Size(100, 26)
        Return b
    End Function

    Private Sub LayoutChildren()
        Dim pad As Integer = 6
        Dim y As Integer = pad
        Dim x As Integer = pad
        For Each b As FlatButton In New FlatButton() {fbRefresh, fbApplyMode, fbProject, fbOpen}
            b.Location = New Point(x, y)
            b.Size = New Size(100, 26)
            x += 104
        Next
        y += 32
        lblBright.SetBounds(pad, y, Math.Max(80, ClientSize.Width - pad * 2), 22)
        y += 24
        trkBright.SetBounds(pad, y, Math.Max(80, ClientSize.Width - pad * 2), 40)
        y += 42
        chkLock.Location = New Point(pad, y)
        y += 28
        lblMode.SetBounds(pad, y, Math.Max(80, ClientSize.Width - pad * 2), 18)
        y += 20
        cboModes.SetBounds(pad, y, Math.Max(80, ClientSize.Width - pad * 2), 24)
        y += 30
        lblOrient.SetBounds(pad, y, Math.Max(80, ClientSize.Width - pad * 2), 18)
        y += 20
        x = pad
        For Each b As FlatButton In New FlatButton() {fbOrient0, fbOrient90, fbOrient180, fbOrient270}
            b.Location = New Point(x, y)
            b.Size = New Size(70, 26)
            x += 74
        Next
        status.SetBounds(pad, Math.Max(y + 34, ClientSize.Height - 20), Math.Max(80, ClientSize.Width - pad * 2), 18)
    End Sub

    Private Sub SyncFromSystem(ByVal full As Boolean)
        _suppress = True
        Try
            Dim pct As Integer = DisplayService.GetBrightnessPercent()
            If pct < 0 Then
                lblBright.Text = "BRIGHTNESS  UNAVAILABLE"
                trkBright.Enabled = False
            Else
                trkBright.Enabled = True
                If trkBright.Value <> pct Then trkBright.Value = pct
                lblBright.Text = "BRIGHTNESS  " & pct.ToString() & "%"
            End If

            Dim locked As Boolean = DisplayService.IsRotationLocked()
            If chkLock.Checked <> locked Then chkLock.Checked = locked

            Dim cur As DisplayModeInfo = DisplayService.GetCurrentMode()
            Dim orient As Integer = DisplayService.GetOrientationDegrees()
            lblOrient.Text = "ORIENTATION  " & orient.ToString() & "°"

            If full OrElse Not _loaded Then
                Dim modes As List(Of DisplayModeInfo) = DisplayService.ListModes()
                cboModes.BeginUpdate()
                cboModes.Items.Clear()
                Dim selectIndex As Integer = -1
                For i As Integer = 0 To modes.Count - 1
                    cboModes.Items.Add(modes(i))
                    If cur IsNot Nothing AndAlso modes(i).Width = cur.Width AndAlso modes(i).Height = cur.Height AndAlso modes(i).Frequency = cur.Frequency Then
                        selectIndex = i
                    End If
                Next
                cboModes.EndUpdate()
                If selectIndex >= 0 Then cboModes.SelectedIndex = selectIndex
                If cur IsNot Nothing Then
                    lblMode.Text = "RESOLUTION  " & cur.ToString().ToUpperInvariant()
                End If
            End If

            status.Text = If(full, "SYNCED", "LIVE")
            _loaded = True
        Finally
            _suppress = False
        End Try
        LayoutChildren()
    End Sub

    Private Sub OnBrightnessChanged(ByVal sender As Object, ByVal e As EventArgs)
        If _suppress Then Return
        If DisplayService.SetBrightnessPercent(trkBright.Value) Then
            lblBright.Text = "BRIGHTNESS  " & trkBright.Value.ToString() & "%"
            status.Text = "BRIGHTNESS SET"
        Else
            status.Text = "BRIGHTNESS UNAVAILABLE"
        End If
    End Sub

    Private Sub OnLockChanged(ByVal sender As Object, ByVal e As EventArgs)
        If _suppress Then Return
        If DisplayService.SetRotationLocked(chkLock.Checked) Then
            status.Text = If(chkLock.Checked, "ROTATION LOCKED", "ROTATION UNLOCKED")
        Else
            status.Text = "ROTATION LOCK FAILED"
        End If
    End Sub

    Private Sub OnApplyMode(ByVal sender As Object, ByVal e As EventArgs)
        Dim mode As DisplayModeInfo = TryCast(cboModes.SelectedItem, DisplayModeInfo)
        If mode Is Nothing Then
            status.Text = "SELECT A RESOLUTION"
            Return
        End If
        status.Text = DisplayService.ApplyMode(mode)
        SyncFromSystem(True)
    End Sub

    Private Sub ApplyOrient(ByVal degrees As Integer)
        status.Text = DisplayService.SetOrientationDegrees(degrees)
        SyncFromSystem(True)
    End Sub
End Class

End Namespace
