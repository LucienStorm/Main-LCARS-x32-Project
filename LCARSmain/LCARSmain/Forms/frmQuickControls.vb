Option Strict On

Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms
Imports LCARS.Controls

''' <summary>
''' Action-center overlay for volume, brightness, Wi-Fi, Bluetooth, rotation, and power.
''' </summary>
Public Class frmQuickControls
    Inherits LCARS.LCARSForm

    Private trkVolume As System.Windows.Forms.TrackBar
    Private trkBrightness As System.Windows.Forms.TrackBar
    Private lblVolume As Label
    Private lblBrightness As Label
    Private lblBattery As Label
    Private lblBatteryDetail As Label
    Private lblWifiStatus As Label
    Private lblBtStatus As Label
    Private lblRotation As Label
    Private lblNetworkIp As Label
    Private lblNetworkName As Label
    Private tmrNetworkInfo As Timer
    Private lstWifi As ListBox
    Private lstBluetooth As ListBox
    Private txtWifiPassword As TextBox
    Private sbClose As StandardButton
    Private sbMute As StandardButton
    Private sbWifiOn As StandardButton
    Private sbWifiRefresh As StandardButton
    Private sbWifiConnect As StandardButton
    Private sbBtOn As StandardButton
    Private sbBtRefresh As StandardButton
    Private sbBtPair As StandardButton
    Private sbBtConnect As StandardButton
    Private sbBtDisconnect As StandardButton
    Private sbBtUnpair As StandardButton
    Private sbRotation As StandardButton
    Private sbPowerCycle As StandardButton
    Private btActionBusy As Boolean
    Private tmrVolumePoll As Timer
    Private syncingVolumeSlider As Boolean = False

    Public Sub New()
        InitializeQuickControlsUi()
        BindToWorkingArea = False
        ' Fast path only — Wi-Fi/BT scans run after the panel is visible.
        RefreshFast()
        AddHandler Me.Shown, AddressOf frmQuickControls_Shown
        tmrVolumePoll = New Timer()
        tmrVolumePoll.Interval = 1000
        AddHandler tmrVolumePoll.Tick, AddressOf tmrVolumePoll_Tick
        AddHandler Me.FormClosed, AddressOf frmQuickControls_FormClosed
        tmrNetworkInfo = New Timer()
        tmrNetworkInfo.Interval = 5000
        AddHandler tmrNetworkInfo.Tick, AddressOf tmrNetworkInfo_Tick
    End Sub

    Private Sub frmQuickControls_FormClosed(ByVal sender As Object, ByVal e As FormClosedEventArgs)
        tmrVolumePoll.Stop()
        If tmrNetworkInfo IsNot Nothing Then tmrNetworkInfo.Stop()
        RemoveHandler QuickControlsAudio.VolumeChanged, AddressOf QuickControlsAudio_VolumeChanged
        QuickControlsAudio.RemoveVolumeListener()
    End Sub

    Private Sub QuickControlsAudio_VolumeChanged(ByVal sender As Object, ByVal e As EventArgs)
        If Me.IsDisposed Then Return
        If Me.InvokeRequired Then
            BeginInvoke(New MethodInvoker(AddressOf SyncVolumeFromSystem))
            Return
        End If
        SyncVolumeFromSystem()
    End Sub

    Private Sub SyncVolumeFromSystem()
        If syncingVolumeSlider OrElse trkVolume Is Nothing Then Return
        Dim current As Integer = QuickControlsAudio.GetVolumePercent()
        If trkVolume.Value <> current Then
            syncingVolumeSlider = True
            trkVolume.Value = Math.Max(trkVolume.Minimum, Math.Min(trkVolume.Maximum, current))
            syncingVolumeSlider = False
            QuickControlsAudio.LogVolumeIfChanged(current, "notify")
        End If
        Dim muted As Boolean = QuickControlsAudio.GetMute()
        Dim muteText As String = If(muted, "UNMUTE", "MUTE")
        If sbMute.ButtonText <> muteText Then
            sbMute.ButtonText = muteText
            sbMute.Text = muteText
        End If
    End Sub

    Private Sub tmrVolumePoll_Tick(ByVal sender As Object, ByVal e As EventArgs)
        SyncVolumeFromSystem()
    End Sub

    Private Sub frmQuickControls_Shown(ByVal sender As Object, ByVal e As EventArgs)
        AddHandler QuickControlsAudio.VolumeChanged, AddressOf QuickControlsAudio_VolumeChanged
        QuickControlsAudio.AddVolumeListener()
        tmrVolumePoll.Start()
        SyncVolumeFromSystem()
        RefreshNetworkInfo()
        If tmrNetworkInfo IsNot Nothing Then tmrNetworkInfo.Start()
        RefreshNetworkAsync()
    End Sub

    Private Sub tmrNetworkInfo_Tick(ByVal sender As Object, ByVal e As EventArgs)
        RefreshNetworkInfo()
    End Sub

    ''' <summary>
    ''' Shows primary IPv4 and current network name under Rotation Lock.
    ''' </summary>
    Private Sub RefreshNetworkInfo()
        If lblNetworkIp Is Nothing OrElse lblNetworkName Is Nothing Then Return
        Dim ip As String = QuickControlsNetworkInfo.GetPrimaryIPv4()
        Dim netName As String = QuickControlsNetworkInfo.GetCurrentNetworkName()
        lblNetworkIp.Text = "IP  " & If(String.IsNullOrEmpty(ip), "(none)", ip)
        lblNetworkName.Text = "NET  " & If(String.IsNullOrEmpty(netName), "(unknown)", netName)
    End Sub

    Private Sub InitializeQuickControlsUi()
        Me.Text = ""
        Me.BackColor = Color.Black
        Me.FormBorderStyle = FormBorderStyle.None
        Me.ControlBox = False
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.ShowInTaskbar = False
        Me.StartPosition = FormStartPosition.Manual
        Me.TopMost = True
        Me.KeyPreview = True

        Dim margin As Integer = 12
        Dim y As Integer = 12
        Dim ctrlW As Integer = 160
        Dim colGap As Integer = 4
        Dim xLeft As Integer = margin
        Dim xRight As Integer = xLeft + ctrlW + colGap
        Dim listW As Integer = 288
        Dim btnGap As Integer = 4
        Dim btnThird As Integer = (listW - btnGap * 2) \ 3
        Me.ClientSize = New Size(xRight + listW + margin, 560)

        lblVolume = MakeLabel("VOLUME", xLeft, y)
        trkVolume = MakeTrackBar(xLeft, y + 22, ctrlW)
        AddHandler trkVolume.ValueChanged, AddressOf trkVolume_ValueChanged
        sbMute = MakeButton("MUTE", xLeft, y + 48, 96)

        lblBrightness = MakeLabel("BRIGHTNESS", xLeft, y + 82)
        trkBrightness = MakeTrackBar(xLeft, y + 104, ctrlW)
        AddHandler trkBrightness.ValueChanged, AddressOf trkBrightness_ValueChanged

        lblBattery = MakeLabel("BATTERY / POWER", xLeft, y + 136)
        lblBatteryDetail = MakeWrappedLabel(xLeft, y + 154, ctrlW)
        sbPowerCycle = MakeButton("CYCLE PLAN", xLeft, y + 182, ctrlW)

        lblRotation = MakeLabel("ROTATION LOCK", xLeft, y + 222)
        sbRotation = MakeButton("TOGGLE", xLeft, y + 244, ctrlW)
        lblNetworkIp = MakeWrappedLabel(xLeft, y + 278, ctrlW)
        lblNetworkIp.Height = 20
        lblNetworkName = MakeWrappedLabel(xLeft, y + 298, ctrlW)
        lblNetworkName.Height = 36

        lblWifiStatus = MakeLabel("WI-FI", xRight, y)
        sbWifiOn = MakeButton("RADIO", xRight, y + 28, btnThird)
        sbWifiRefresh = MakeButton("SCAN", xRight + btnThird + btnGap, y + 28, btnThird)
        sbWifiConnect = MakeButton("CONNECT", xRight + (btnThird + btnGap) * 2, y + 28, btnThird)
        lstWifi = MakeListBox(xRight, y + 64, listW, 140)
        txtWifiPassword = MakeTextBox(xRight, y + 210, listW)

        lblBtStatus = MakeLabel("BLUETOOTH", xRight, y + 250)
        sbBtOn = MakeButton("RADIO", xRight, y + 278, btnThird)
        sbBtRefresh = MakeButton("REFRESH", xRight + btnThird + btnGap, y + 278, btnThird)
        sbBtPair = MakeButton("PAIR NEW", xRight + (btnThird + btnGap) * 2, y + 278, btnThird)
        sbBtConnect = MakeButton("CONNECT", xRight, y + 310, btnThird)
        sbBtDisconnect = MakeButton("DISCONNECT", xRight + btnThird + btnGap, y + 310, btnThird + 8)
        sbBtUnpair = MakeButton("UNPAIR", xRight + (btnThird + btnGap) * 2 + 8, y + 310, btnThird - 8)
        lstBluetooth = MakeListBox(xRight, y + 342, listW, 100)

        sbClose = MakeButton("CLOSE", xRight + listW - 96, 510, 96)
        AddHandler sbClose.Click, Sub() Me.Close()
        AddHandler sbMute.Click, AddressOf sbMute_Click
        AddHandler sbWifiOn.Click, AddressOf sbWifiOn_Click
        AddHandler sbWifiRefresh.Click, AddressOf sbWifiRefresh_Click
        AddHandler sbWifiConnect.Click, AddressOf sbWifiConnect_Click
        AddHandler sbBtOn.Click, AddressOf sbBtOn_Click
        AddHandler sbBtRefresh.Click, AddressOf sbBtRefresh_Click
        AddHandler sbBtPair.Click, AddressOf sbBtPair_Click
        AddHandler sbBtConnect.Click, AddressOf sbBtConnect_Click
        AddHandler sbBtDisconnect.Click, AddressOf sbBtDisconnect_Click
        AddHandler sbBtUnpair.Click, AddressOf sbBtUnpair_Click
        AddHandler sbRotation.Click, AddressOf sbRotation_Click
        AddHandler sbPowerCycle.Click, AddressOf sbPowerCycle_Click
        AddHandler Me.KeyDown, AddressOf frmQuickControls_KeyDown
    End Sub

    ''' <summary>
    ''' Places the overlay as a drop-down under the QUICK button; right edge flush with the screen.
    ''' </summary>
    Public Sub PositionAsDropdown(ByVal anchor As Control)
        Dim scr As Screen
        If anchor Is Nothing OrElse anchor.IsDisposed Then
            scr = Screen.PrimaryScreen
            Me.StartPosition = FormStartPosition.Manual
            Me.Location = New Point(scr.Bounds.Right - Me.Width, scr.Bounds.Top + 40)
            Return
        End If
        Dim anchorRect As New Rectangle(anchor.PointToScreen(Point.Empty), anchor.Size)
        scr = Screen.FromControl(anchor)
        Dim bounds As Rectangle = scr.Bounds

        Dim x As Integer = bounds.Right - Me.Width
        Dim y As Integer = anchorRect.Bottom + 4
        If x < bounds.Left Then x = bounds.Left
        If y + Me.Height > bounds.Bottom Then
            y = Math.Max(bounds.Top, anchorRect.Top - Me.Height - 4)
        End If
        Me.Location = New Point(x, y)
    End Sub

    Private Sub frmQuickControls_KeyDown(ByVal sender As Object, ByVal e As KeyEventArgs)
        If e.KeyCode = Keys.Escape Then
            Me.Close()
        End If
    End Sub

    Private Function MakeLabel(ByVal text As String, ByVal x As Integer, ByVal y As Integer) As Label
        Dim lbl As New Label()
        lbl.Text = text
        lbl.ForeColor = Color.Orange
        lbl.Font = New Font("LCARS", 14.0F, FontStyle.Regular)
        lbl.BackColor = Color.Black
        lbl.Location = New Point(x, y)
        lbl.AutoSize = True
        Me.Controls.Add(lbl)
        Return lbl
    End Function

    Private Function MakeButton(ByVal text As String, ByVal x As Integer, ByVal y As Integer, ByVal w As Integer) As StandardButton
        Dim sb As New StandardButton()
        sb.ButtonText = text
        sb.Text = text
        sb.Color = LCARS.LCARScolorStyles.NavigationFunction
        sb.Location = New Point(x, y)
        sb.Size = New Size(w, 28)
        Me.Controls.Add(sb)
        Return sb
    End Function

    Private Function MakeTrackBar(ByVal x As Integer, ByVal y As Integer, ByVal width As Integer) As System.Windows.Forms.TrackBar
        Dim trk As New System.Windows.Forms.TrackBar()
        trk.Minimum = 0
        trk.Maximum = 100
        trk.TickFrequency = 10
        trk.Location = New Point(x, y)
        trk.Size = New Size(width, 22)
        trk.AutoSize = False
        Me.Controls.Add(trk)
        Return trk
    End Function

    Private Function MakeListBox(ByVal x As Integer, ByVal y As Integer, ByVal w As Integer, ByVal h As Integer) As ListBox
        Dim lst As New ListBox()
        lst.BackColor = Color.Black
        lst.ForeColor = Color.Orange
        lst.Font = New Font("Microsoft Sans Serif", 9.0F)
        lst.Location = New Point(x, y)
        lst.Size = New Size(w, h)
        Me.Controls.Add(lst)
        Return lst
    End Function

    Private Function MakeWrappedLabel(ByVal x As Integer, ByVal y As Integer, ByVal width As Integer) As Label
        Dim lbl As New Label()
        lbl.ForeColor = Color.Orange
        lbl.Font = New Font("LCARS", 12.0F, FontStyle.Regular)
        lbl.BackColor = Color.Black
        lbl.Location = New Point(x, y)
        lbl.Size = New Size(width, 32)
        lbl.AutoSize = False
        lbl.TextAlign = ContentAlignment.TopLeft
        Me.Controls.Add(lbl)
        Return lbl
    End Function

    Private Sub UpdateBatteryDisplay()
        lblBattery.Text = "BATTERY / POWER"
        lblBatteryDetail.Text = QuickControlsPower.GetBatterySummary()
    End Sub

    Private Function MakeTextBox(ByVal x As Integer, ByVal y As Integer, ByVal w As Integer) As TextBox
        Dim txt As New TextBox()
        txt.BackColor = Color.Black
        txt.ForeColor = Color.Orange
        txt.BorderStyle = BorderStyle.FixedSingle
        txt.Location = New Point(x, y)
        txt.Size = New Size(w, 28)
        txt.UseSystemPasswordChar = True
        Me.Controls.Add(txt)
        Return txt
    End Function

    ''' <summary>
    ''' Instant UI state — no Wi-Fi scan or Bluetooth enumeration.
    ''' </summary>
    Private Sub RefreshFast()
        trkVolume.Value = Math.Max(0, Math.Min(100, QuickControlsAudio.GetVolumePercent()))
        sbMute.ButtonText = If(QuickControlsAudio.GetMute(), "UNMUTE", "MUTE")
        sbMute.Text = sbMute.ButtonText

        trkBrightness.Value = Math.Max(0, Math.Min(100, QuickControlsBrightness.GetBrightnessPercent()))
        UpdateBatteryDisplay()

        Dim locked As Boolean = QuickControlsRotation.IsRotationLocked()
        sbRotation.ButtonText = If(locked, "UNLOCK", "LOCK")
        sbRotation.Text = sbRotation.ButtonText
        lblRotation.Text = "ROTATION LOCK — " & If(locked, "ON", "OFF")

        RefreshNetworkInfo()

        lblWifiStatus.Text = "WI-FI — …"
        lblBtStatus.Text = "BLUETOOTH — …"
    End Sub

    ''' <summary>
    ''' Loads radio status and scans off the UI thread so the panel appears immediately.
    ''' </summary>
    Private Sub RefreshNetworkAsync()
        lblWifiStatus.Text = "WI-FI — scanning…"
        lblBtStatus.Text = "BLUETOOTH — loading…"
        Dim worker As New System.ComponentModel.BackgroundWorker()
        AddHandler worker.DoWork, Sub(sender As Object, e As System.ComponentModel.DoWorkEventArgs)
                                      Dim wifiOn As Boolean = QuickControlsWifi.IsWifiRadioOn()
                                      Dim btOn As Boolean = QuickControlsBluetooth.IsBluetoothOn()
                                      Dim nets As List(Of QuickControlsWifi.WifiNetwork) = QuickControlsWifi.ScanNetworks()
                                      Dim devices As List(Of QuickControlsBluetooth.BtDevice) = QuickControlsBluetooth.ListDevices()
                                      e.Result = New Object() {wifiOn, btOn, nets, devices}
                                  End Sub
        AddHandler worker.RunWorkerCompleted, Sub(sender As Object, e As System.ComponentModel.RunWorkerCompletedEventArgs)
                                                  If Me.IsDisposed OrElse e.Error IsNot Nothing OrElse e.Result Is Nothing Then
                                                      If Not Me.IsDisposed Then
                                                          lblWifiStatus.Text = "WI-FI — scan failed"
                                                          lblBtStatus.Text = "BLUETOOTH — load failed"
                                                      End If
                                                      Return
                                                  End If
                                                  Dim parts() As Object = CType(e.Result, Object())
                                                  Dim wifiOn As Boolean = CBool(parts(0))
                                                  Dim btOn As Boolean = CBool(parts(1))
                                                  sbWifiOn.ButtonText = If(wifiOn, "WIFI ON", "WIFI OFF")
                                                  sbWifiOn.Text = sbWifiOn.ButtonText
                                                  sbBtOn.ButtonText = If(btOn, "BT ON", "BT OFF")
                                                  sbBtOn.Text = sbBtOn.ButtonText
                                                  lstWifi.Items.Clear()
                                                  For Each net As QuickControlsWifi.WifiNetwork In CType(parts(2), List(Of QuickControlsWifi.WifiNetwork))
                                                      lstWifi.Items.Add(net)
                                                  Next
                                                  lblWifiStatus.Text = "WI-FI — " & lstWifi.Items.Count.ToString() & " network(s)"
                                                  ApplyBluetoothDeviceList(CType(parts(3), List(Of QuickControlsBluetooth.BtDevice)))
                                                  lblBtStatus.Text = "BLUETOOTH — " & lstBluetooth.Items.Count.ToString() & " device(s)"
                                              End Sub
        worker.RunWorkerAsync()
    End Sub

    Private Sub RefreshAll()
        RefreshFast()
        sbWifiRefresh_Click(Nothing, EventArgs.Empty)
        sbBtRefresh_Click(Nothing, EventArgs.Empty)
    End Sub

    Private Sub trkVolume_ValueChanged(ByVal sender As Object, ByVal e As EventArgs)
        If syncingVolumeSlider Then Return
        QuickControlsAudio.SetVolumePercent(trkVolume.Value)
    End Sub

    Private Sub trkBrightness_ValueChanged(ByVal sender As Object, ByVal e As EventArgs)
        If Not QuickControlsBrightness.SetBrightnessPercent(trkBrightness.Value) Then
            lblBrightness.Text = "BRIGHTNESS — WMI unavailable on this display"
        End If
    End Sub

    Private Sub sbMute_Click(ByVal sender As Object, ByVal e As EventArgs)
        QuickControlsAudio.SetMute(Not QuickControlsAudio.GetMute())
        sbMute.ButtonText = If(QuickControlsAudio.GetMute(), "UNMUTE", "MUTE")
        sbMute.Text = sbMute.ButtonText
    End Sub

    Private Sub sbWifiOn_Click(ByVal sender As Object, ByVal e As EventArgs)
        QuickControlsWifi.SetWifiRadioOn(Not QuickControlsWifi.IsWifiRadioOn())
        sbWifiOn.ButtonText = If(QuickControlsWifi.IsWifiRadioOn(), "WIFI ON", "WIFI OFF")
        sbWifiOn.Text = sbWifiOn.ButtonText
    End Sub

    Private Sub sbWifiRefresh_Click(ByVal sender As Object, ByVal e As EventArgs)
        lstWifi.Items.Clear()
        For Each net As QuickControlsWifi.WifiNetwork In QuickControlsWifi.ScanNetworks()
            lstWifi.Items.Add(net)
        Next
        lblWifiStatus.Text = "WI-FI — " & lstWifi.Items.Count.ToString() & " network(s)"
    End Sub

    Private Sub sbWifiConnect_Click(ByVal sender As Object, ByVal e As EventArgs)
        If lstWifi.SelectedItem Is Nothing Then
            MessageBox.Show("Select a Wi-Fi network first.", "Quick Controls", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If
        Dim net As QuickControlsWifi.WifiNetwork = CType(lstWifi.SelectedItem, QuickControlsWifi.WifiNetwork)
        Dim pwd As String = txtWifiPassword.Text
        Dim result As String = QuickControlsWifi.Connect(net.SSID, pwd)
        MessageBox.Show("Connect result:" & vbCrLf & result, "Quick Controls", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub sbBtOn_Click(ByVal sender As Object, ByVal e As EventArgs)
        QuickControlsBluetooth.SetBluetoothOn(Not QuickControlsBluetooth.IsBluetoothOn())
        sbBtOn.ButtonText = If(QuickControlsBluetooth.IsBluetoothOn(), "BT ON", "BT OFF")
        sbBtOn.Text = sbBtOn.ButtonText
    End Sub

    Private Sub sbBtRefresh_Click(ByVal sender As Object, ByVal e As EventArgs)
        RefreshBluetoothListAsync()
    End Sub

    Private Sub sbBtPair_Click(ByVal sender As Object, ByVal e As EventArgs)
        QuickControlsBluetooth.StartPairingWizard()
    End Sub

    ''' <summary>
    ''' Returns the selected Bluetooth device, or Nothing if none selected.
    ''' </summary>
    Private Function SelectedBtDevice() As QuickControlsBluetooth.BtDevice
        If lstBluetooth.SelectedItem Is Nothing Then Return Nothing
        Return CType(lstBluetooth.SelectedItem, QuickControlsBluetooth.BtDevice)
    End Function

    Private Sub sbBtConnect_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim device As QuickControlsBluetooth.BtDevice = SelectedBtDevice()
        If device Is Nothing Then
            lblBtStatus.Text = "BLUETOOTH — select a device"
            Return
        End If
        RunBtDeviceAction("connect", device)
    End Sub

    Private Sub sbBtDisconnect_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim device As QuickControlsBluetooth.BtDevice = SelectedBtDevice()
        If device Is Nothing Then
            lblBtStatus.Text = "BLUETOOTH — select a device"
            Return
        End If
        RunBtDeviceAction("disconnect", device)
    End Sub

    Private Sub sbBtUnpair_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim device As QuickControlsBluetooth.BtDevice = SelectedBtDevice()
        If device Is Nothing Then
            lblBtStatus.Text = "BLUETOOTH — select a device"
            Return
        End If
        RunBtDeviceAction("unpair", device)
    End Sub

    ''' <summary>
    ''' Runs Connect / Disconnect / Unpair off the UI thread; status label only (no MessageBox).
    ''' </summary>
    Private Sub RunBtDeviceAction(ByVal action As String, ByVal device As QuickControlsBluetooth.BtDevice)
        If btActionBusy Then
            lblBtStatus.Text = "BLUETOOTH — busy"
            Return
        End If
        If device Is Nothing Then
            lblBtStatus.Text = "BLUETOOTH — select a device"
            Return
        End If
        btActionBusy = True
        lblBtStatus.Text = "BLUETOOTH — " & action & "…"
        Dim worker As New System.ComponentModel.BackgroundWorker()
        AddHandler worker.DoWork, Sub(sender As Object, e As System.ComponentModel.DoWorkEventArgs)
                                      Dim status As String
                                      Select Case action
                                          Case "connect"
                                              status = QuickControlsBluetooth.Connect(device)
                                          Case "disconnect"
                                              status = QuickControlsBluetooth.Disconnect(device)
                                          Case "unpair"
                                              status = QuickControlsBluetooth.Unpair(device)
                                          Case Else
                                              status = "unknown action"
                                      End Select
                                      Dim devices As List(Of QuickControlsBluetooth.BtDevice) = QuickControlsBluetooth.ListDevices()
                                      e.Result = New Object() {status, devices}
                                  End Sub
        AddHandler worker.RunWorkerCompleted, Sub(sender As Object, e As System.ComponentModel.RunWorkerCompletedEventArgs)
                                                  btActionBusy = False
                                                  If Me.IsDisposed Then Return
                                                  If e.Error IsNot Nothing OrElse e.Result Is Nothing Then
                                                      lblBtStatus.Text = "BLUETOOTH — " & action & " failed"
                                                      Return
                                                  End If
                                                  Dim parts() As Object = CType(e.Result, Object())
                                                  Dim status As String = CStr(parts(0))
                                                  ApplyBluetoothDeviceList(CType(parts(1), List(Of QuickControlsBluetooth.BtDevice)))
                                                  lblBtStatus.Text = "BLUETOOTH — " & status
                                              End Sub
        worker.RunWorkerAsync()
    End Sub

    ''' <summary>
    ''' Reloads the Bluetooth list off the UI thread; items use BtDevice.ToString().
    ''' </summary>
    Private Sub RefreshBluetoothListAsync()
        If btActionBusy Then
            lblBtStatus.Text = "BLUETOOTH — busy"
            Return
        End If
        btActionBusy = True
        lblBtStatus.Text = "BLUETOOTH — loading…"
        Dim worker As New System.ComponentModel.BackgroundWorker()
        AddHandler worker.DoWork, Sub(sender As Object, e As System.ComponentModel.DoWorkEventArgs)
                                      e.Result = QuickControlsBluetooth.ListDevices()
                                  End Sub
        AddHandler worker.RunWorkerCompleted, Sub(sender As Object, e As System.ComponentModel.RunWorkerCompletedEventArgs)
                                                  btActionBusy = False
                                                  If Me.IsDisposed Then Return
                                                  If e.Error IsNot Nothing OrElse e.Result Is Nothing Then
                                                      lblBtStatus.Text = "BLUETOOTH — load failed"
                                                      Return
                                                  End If
                                                  ApplyBluetoothDeviceList(CType(e.Result, List(Of QuickControlsBluetooth.BtDevice)))
                                                  lblBtStatus.Text = "BLUETOOTH — " & lstBluetooth.Items.Count.ToString() & " device(s)"
                                              End Sub
        worker.RunWorkerAsync()
    End Sub

    ''' <summary>
    ''' Replaces list contents; ListBox uses BtDevice.ToString() (Name · DeviceClass · State).
    ''' </summary>
    Private Sub ApplyBluetoothDeviceList(ByVal devices As List(Of QuickControlsBluetooth.BtDevice))
        lstBluetooth.Items.Clear()
        If devices Is Nothing Then Return
        For Each dev As QuickControlsBluetooth.BtDevice In devices
            lstBluetooth.Items.Add(dev)
        Next
    End Sub

    Private Sub sbRotation_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim wantLocked As Boolean = Not QuickControlsRotation.IsRotationLocked()
        QuickControlsRotation.SetRotationLocked(wantLocked)
        Dim locked As Boolean = QuickControlsRotation.IsRotationLocked()
        sbRotation.ButtonText = If(locked, "UNLOCK", "LOCK")
        sbRotation.Text = sbRotation.ButtonText
        If locked = wantLocked Then
            lblRotation.Text = "ROTATION LOCK — " & If(locked, "ON", "OFF")
        Else
            lblRotation.Text = "ROTATION LOCK — could not change (needs system permission)"
        End If
    End Sub

    Private Sub sbPowerCycle_Click(ByVal sender As Object, ByVal e As EventArgs)
        QuickControlsPower.CyclePowerPlan()
        UpdateBatteryDisplay()
        CommonScreen.RefreshPowerPlanDisplay()
    End Sub
End Class
