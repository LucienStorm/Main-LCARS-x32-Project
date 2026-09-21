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
    Private lblNetworkMac As Label
    Private lblNetworkGateway As Label
    Private lblNetworkVpn As Label
    Private lblNetworkSpeed As Label
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
    Private syncingMute As Boolean = False
    Private networkShowCidr As Boolean = True
    Private lastNetworkSnap As QuickControlsNetworkInfo.Snapshot = Nothing

    Public Sub New()
        InitializeQuickControlsUi()
        BindToWorkingArea = False
        ' Fast path only — Wi-Fi/BT scans run after the panel is visible.
        RefreshFast()
        AddHandler Me.Shown, AddressOf frmQuickControls_Shown
        tmrVolumePoll = New Timer()
        tmrVolumePoll.Interval = 250
        AddHandler tmrVolumePoll.Tick, AddressOf tmrVolumePoll_Tick
        AddHandler Me.FormClosed, AddressOf frmQuickControls_FormClosed
        tmrNetworkInfo = New Timer()
        tmrNetworkInfo.Interval = 1000
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
        If current < 0 Then Return
        If trkVolume.Value <> current Then
            syncingVolumeSlider = True
            Try
                trkVolume.Value = Math.Max(trkVolume.Minimum, Math.Min(trkVolume.Maximum, current))
            Finally
                syncingVolumeSlider = False
            End Try
            QuickControlsAudio.LogVolumeIfChanged(current, "notify")
        End If
        If syncingMute OrElse sbMute Is Nothing Then Return
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
        tmrVolumePoll.Interval = 250
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
    ''' Shows IP, SSID/adapter, MAC, gateway/CIDR, VPN, and live RX/TX under Rotation Lock.
    ''' Tap the gateway line to switch CIDR vs Gateway/Subnet mask.
    ''' </summary>
    Private Sub RefreshNetworkInfo()
        If lblNetworkIp Is Nothing Then Return
        Dim snap As QuickControlsNetworkInfo.Snapshot = QuickControlsNetworkInfo.GetSnapshot()
        lastNetworkSnap = snap
        ApplyNetworkAddressLabels(snap)
        If lblNetworkName IsNot Nothing Then
            lblNetworkName.Text = "NET  " & If(String.IsNullOrEmpty(snap.AdapterOrSsid), "(unknown)", snap.AdapterOrSsid)
        End If
        If lblNetworkMac IsNot Nothing Then
            lblNetworkMac.Text = "MAC  " & If(String.IsNullOrEmpty(snap.Mac), "(n/a)", snap.Mac)
        End If
        If lblNetworkVpn IsNot Nothing Then
            lblNetworkVpn.Text = "VPN  " & If(String.IsNullOrEmpty(snap.Vpn), "OFF", snap.Vpn)
        End If
        If lblNetworkSpeed IsNot Nothing Then
            lblNetworkSpeed.Text = "RX  " & FormatBytesPerSec(snap.RxBytesPerSec) & "  TX  " & FormatBytesPerSec(snap.TxBytesPerSec)
        End If
    End Sub

    Private Sub ApplyNetworkAddressLabels(ByVal snap As QuickControlsNetworkInfo.Snapshot)
        If snap Is Nothing Then Return
        Dim ip As String = If(String.IsNullOrEmpty(snap.Ip), "(none)", snap.Ip)
        Dim gw As String = If(String.IsNullOrEmpty(snap.Gateway), "(n/a)", snap.Gateway)
        Dim mask As String = snap.SubnetMask
        If String.IsNullOrEmpty(mask) Then
            mask = QuickControlsNetworkInfo.MaskFromCidrPrefix(snap.CidrPrefix)
        End If
        If networkShowCidr Then
            Dim cidrIp As String = ip
            If ip <> "(none)" AndAlso Not String.IsNullOrEmpty(snap.CidrPrefix) Then cidrIp = ip & snap.CidrPrefix
            lblNetworkIp.Text = "IP  " & cidrIp
            If lblNetworkGateway IsNot Nothing Then
                ' Single line — CIDR already on the IP row; keep GW short so it fits.
                lblNetworkGateway.Height = 18
                lblNetworkGateway.Text = "GW  " & gw
            End If
        Else
            lblNetworkIp.Text = "IP  " & ip
            If lblNetworkGateway IsNot Nothing Then
                If String.IsNullOrEmpty(mask) Then mask = "(n/a)"
                ' Two lines: full "MASK 255.x.x.x" does not fit beside GW in this column.
                lblNetworkGateway.Height = 36
                lblNetworkGateway.Text = "GW  " & gw & vbCrLf & "MASK  " & mask
            End If
        End If
        LayoutNetworkInfoRows()
    End Sub

    ''' <summary>
    ''' Keeps VPN/speed rows clear of the two-line gateway/mask block.
    ''' </summary>
    Private Sub LayoutNetworkInfoRows()
        If lblNetworkGateway Is Nothing Then Return
        Dim belowGateway As Integer = lblNetworkGateway.Bottom + 2
        If lblNetworkVpn IsNot Nothing Then
            lblNetworkVpn.Top = belowGateway
            belowGateway = lblNetworkVpn.Bottom + 2
        End If
        If lblNetworkSpeed IsNot Nothing Then
            lblNetworkSpeed.Top = belowGateway
        End If
    End Sub

    Private Sub lblNetworkGateway_Click(ByVal sender As Object, ByVal e As EventArgs)
        networkShowCidr = Not networkShowCidr
        If lastNetworkSnap IsNot Nothing Then
            ApplyNetworkAddressLabels(lastNetworkSnap)
        Else
            RefreshNetworkInfo()
        End If
    End Sub

    Private Function FormatBytesPerSec(ByVal bytesPerSec As Long) As String
        If bytesPerSec < 1024 Then Return bytesPerSec.ToString() & " B/s"
        If bytesPerSec < 1024L * 1024L Then Return (bytesPerSec / 1024.0R).ToString("0.0") & " KB/s"
        Return (bytesPerSec / (1024.0R * 1024.0R)).ToString("0.00") & " MB/s"
    End Function

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
        lblBatteryDetail.Height = 36
        ' Keep CYCLE PLAN clear of wrapped battery text (was overlapping at y+182).
        sbPowerCycle = MakeButton("CYCLE PLAN", xLeft, y + 198, ctrlW)

        lblRotation = MakeLabel("ROTATION LOCK", xLeft, y + 236)
        sbRotation = MakeButton("TOGGLE", xLeft, y + 258, ctrlW)
        lblNetworkIp = MakeWrappedLabel(xLeft, y + 292, ctrlW)
        lblNetworkIp.Height = 18
        lblNetworkName = MakeWrappedLabel(xLeft, y + 310, ctrlW)
        lblNetworkName.Height = 18
        lblNetworkMac = MakeWrappedLabel(xLeft, y + 328, ctrlW)
        lblNetworkMac.Height = 18
        lblNetworkGateway = MakeWrappedLabel(xLeft, y + 346, ctrlW)
        lblNetworkGateway.Height = 18
        lblNetworkGateway.Cursor = Cursors.Hand
        AddHandler lblNetworkGateway.Click, AddressOf lblNetworkGateway_Click
        lblNetworkIp.Cursor = Cursors.Hand
        AddHandler lblNetworkIp.Click, AddressOf lblNetworkGateway_Click
        lblNetworkVpn = MakeWrappedLabel(xLeft, y + 364, ctrlW)
        lblNetworkVpn.Height = 18
        lblNetworkSpeed = MakeWrappedLabel(xLeft, y + 382, ctrlW)
        lblNetworkSpeed.Height = 18

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
        sbClose.Color = LCARS.LCARScolorStyles.FunctionOffline
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
        sb.Beeping = True
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
        syncingVolumeSlider = True
        Try
            Dim volPct As Integer = QuickControlsAudio.GetVolumePercent()
            If volPct >= 0 Then
                trkVolume.Value = Math.Max(0, Math.Min(100, volPct))
                QuickControlsAudio.LogVolumeIfChanged(volPct, "open")
            End If
        Finally
            syncingVolumeSlider = False
        End Try
        syncingMute = True
        Try
            sbMute.ButtonText = If(QuickControlsAudio.GetMute(), "UNMUTE", "MUTE")
            sbMute.Text = sbMute.ButtonText
        Finally
            syncingMute = False
        End Try

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
        If syncingMute Then Return
        syncingMute = True
        Dim wantMute As Boolean = Not QuickControlsAudio.GetMute()
        Dim ok As Boolean = QuickControlsAudio.SetMute(wantMute)
        Dim muted As Boolean = QuickControlsAudio.GetMute()
        sbMute.ButtonText = If(muted, "UNMUTE", "MUTE")
        sbMute.Text = sbMute.ButtonText
        If Not ok OrElse muted <> wantMute Then
            lblVolume.Text = "VOLUME — mute failed"
        Else
            lblVolume.Text = "VOLUME"
        End If
        syncingMute = False
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
        Dim result As String = QuickControlsWifi.Connect(net.SSID, pwd, net.Auth)
        If result.StartsWith("NEED_PASSWORD:", StringComparison.OrdinalIgnoreCase) Then
            Dim prompted As String = Interaction.InputBox( _
                "Password required for """ & net.SSID & """." & vbCrLf & _
                "Enter the Wi-Fi passphrase (WPA2):", _
                "Quick Controls — Wi-Fi", _
                pwd)
            If String.IsNullOrEmpty(prompted) Then
                lblWifiStatus.Text = "WI-FI — password required"
                txtWifiPassword.Focus()
                Return
            End If
            txtWifiPassword.Text = prompted
            result = QuickControlsWifi.Connect(net.SSID, prompted, net.Auth)
        End If
        lblWifiStatus.Text = "WI-FI — " & FirstStatusLine(result)
        MessageBox.Show("Connect result:" & vbCrLf & result, "Quick Controls", MessageBoxButtons.OK, MessageBoxIcon.Information)
        RefreshNetworkInfo()
    End Sub

    Private Function FirstStatusLine(ByVal text As String) As String
        If text Is Nothing Then Return ""
        For Each line As String In text.Split(New String() {vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
            Dim t As String = line.Trim()
            If t <> "" Then Return t
        Next
        Return text.Trim()
    End Function

    Private Sub sbBtOn_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim wantOn As Boolean = Not QuickControlsBluetooth.IsBluetoothOn()
        lblBtStatus.Text = "BLUETOOTH — " & If(wantOn, "enabling…", "disabling…")
        Dim worker As New System.ComponentModel.BackgroundWorker()
        AddHandler worker.DoWork, Sub(s As Object, args As System.ComponentModel.DoWorkEventArgs)
                                      Dim status As String = QuickControlsBluetooth.SetBluetoothOn(wantOn)
                                      Dim nowOn As Boolean = QuickControlsBluetooth.IsBluetoothOn()
                                      args.Result = New Object() {status, nowOn}
                                  End Sub
        AddHandler worker.RunWorkerCompleted, Sub(s As Object, args As System.ComponentModel.RunWorkerCompletedEventArgs)
                                                  If Me.IsDisposed Then Return
                                                  If args.Error IsNot Nothing OrElse args.Result Is Nothing Then
                                                      lblBtStatus.Text = "BLUETOOTH — radio failed"
                                                      Return
                                                  End If
                                                  Dim parts() As Object = CType(args.Result, Object())
                                                  Dim status As String = CStr(parts(0))
                                                  Dim nowOn As Boolean = CBool(parts(1))
                                                  Dim ok As Boolean = status.Trim().Equals("OK", StringComparison.OrdinalIgnoreCase)
                                                  If ok Then
                                                      sbBtOn.ButtonText = If(nowOn, "BT ON", "BT OFF")
                                                      sbBtOn.Text = sbBtOn.ButtonText
                                                      lblBtStatus.Text = "BLUETOOTH — " & If(nowOn, "ON", "OFF")
                                                  Else
                                                      ' Do not flip the radio button when access was denied.
                                                      sbBtOn.ButtonText = If(QuickControlsBluetooth.IsBluetoothOn(), "BT ON", "BT OFF")
                                                      sbBtOn.Text = sbBtOn.ButtonText
                                                      lblBtStatus.Text = "BLUETOOTH — " & status
                                                  End If
                                              End Sub
        worker.RunWorkerAsync()
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
        Dim result As String = QuickControlsPower.CyclePowerPlan()
        UpdateBatteryDisplay()
        CommonScreen.RefreshPowerPlanDisplay()
        If result IsNot Nothing AndAlso result.StartsWith("OK:", StringComparison.OrdinalIgnoreCase) Then
            Dim planName As String = result.Substring(3).Trim()
            sbPowerCycle.ButtonText = planName.ToUpperInvariant()
            sbPowerCycle.Text = sbPowerCycle.ButtonText
            lblBatteryDetail.Text = QuickControlsPower.GetBatterySummary()
            Dim reset As New Timer()
            reset.Interval = 1600
            AddHandler reset.Tick, Sub(s As Object, ev As EventArgs)
                                       reset.Stop()
                                       reset.Dispose()
                                       If Me.IsDisposed OrElse sbPowerCycle Is Nothing Then Return
                                       sbPowerCycle.ButtonText = "CYCLE PLAN"
                                       sbPowerCycle.Text = "CYCLE PLAN"
                                   End Sub
            reset.Start()
        Else
            lblBatteryDetail.Text = If(String.IsNullOrEmpty(result), "powercfg failed", result)
        End If
    End Sub
End Class
