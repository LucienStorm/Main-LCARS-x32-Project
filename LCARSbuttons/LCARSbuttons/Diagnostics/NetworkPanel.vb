' LCARSbuttons/LCARSbuttons/Diagnostics/NetworkPanel.vb
Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Drawing
Imports System.Windows.Forms
Imports LCARS.Controls

Namespace Diagnostics

''' <summary>All network adapters + per-device admin, IPv4, and Wi-Fi controls.</summary>
Public Class NetworkPanel
    Inherits Panel

    Private ReadOnly lstAdapters As New ListBox()
    Private ReadOnly lstWifi As New ListBox()
    Private ReadOnly lblDetail As New Label()
    Private ReadOnly status As New Label()
    Private ReadOnly chkDhcp As New CheckBox()
    Private ReadOnly txtIp As New TextBox()
    Private ReadOnly txtMask As New TextBox()
    Private ReadOnly txtGw As New TextBox()
    Private ReadOnly txtDns1 As New TextBox()
    Private ReadOnly txtDns2 As New TextBox()
    Private ReadOnly lblIp As New Label()
    Private ReadOnly lblMask As New Label()
    Private ReadOnly lblGw As New Label()
    Private ReadOnly lblDns1 As New Label()
    Private ReadOnly lblDns2 As New Label()

    Private fbRefresh As FlatButton
    Private fbEnable As FlatButton
    Private fbDisable As FlatButton
    Private fbApplyIp As FlatButton
    Private fbAdvanced As FlatButton
    Private fbWifiOn As FlatButton
    Private fbWifiOff As FlatButton
    Private fbScan As FlatButton
    Private fbConnect As FlatButton
    Private fbDisconnect As FlatButton
    Private fbForget As FlatButton

    Private adapters As New List(Of NetworkAdapterInfo)()
    Private wifiNets As New List(Of WifiNetwork)()
    Private selected As NetworkAdapterInfo
    Private _busy As Boolean = False

    Public Sub New()
        BackColor = Color.Black
        Dock = DockStyle.Fill

        StyleList(lstAdapters)
        StyleList(lstWifi)
        AddHandler lstAdapters.SelectedIndexChanged, AddressOf OnAdapterSelected

        lblDetail.ForeColor = Color.Orange
        lblDetail.BackColor = Color.Black
        lblDetail.Font = New Font("Arial Narrow", 10.0!, FontStyle.Bold)
        lblDetail.AutoSize = False

        status.ForeColor = Color.Orange
        status.BackColor = Color.Black
        status.Font = New Font("Arial Narrow", 10.0!, FontStyle.Bold)
        status.Text = "READY"
        status.AutoSize = False

        chkDhcp.Text = "DHCP"
        chkDhcp.ForeColor = Color.Orange
        chkDhcp.BackColor = Color.Black
        chkDhcp.Font = New Font("Arial Narrow", 11.0!, FontStyle.Bold)
        chkDhcp.AutoSize = True
        AddHandler chkDhcp.CheckedChanged, Sub()
                                               Dim en As Boolean = Not chkDhcp.Checked
                                               txtIp.Enabled = en
                                               txtMask.Enabled = en
                                               txtGw.Enabled = en
                                               txtDns1.Enabled = en
                                               txtDns2.Enabled = en
                                           End Sub

        StyleField(lblIp, "IP")
        StyleField(lblMask, "MASK")
        StyleField(lblGw, "GW")
        StyleField(lblDns1, "DNS1")
        StyleField(lblDns2, "DNS2")
        StyleBox(txtIp)
        StyleBox(txtMask)
        StyleBox(txtGw)
        StyleBox(txtDns1)
        StyleBox(txtDns2)

        fbRefresh = MakeButton("REFRESH", LCARS.LCARScolorStyles.PrimaryFunction)
        AddHandler fbRefresh.Click, Sub() Reload()
        fbEnable = MakeButton("ENABLE", LCARS.LCARScolorStyles.PrimaryFunction)
        AddHandler fbEnable.Click, Sub() SetAdmin(True)
        fbDisable = MakeButton("DISABLE", LCARS.LCARScolorStyles.Orange)
        AddHandler fbDisable.Click, Sub() SetAdmin(False)
        fbApplyIp = MakeButton("APPLY IP", LCARS.LCARScolorStyles.PrimaryFunction)
        AddHandler fbApplyIp.Click, AddressOf OnApplyIp
        fbAdvanced = MakeButton("ADVANCED", LCARS.LCARScolorStyles.MiscFunction)
        AddHandler fbAdvanced.Click, Sub()
                                         Try
                                             Process.Start("ncpa.cpl")
                                         Catch
                                             ShellProcess.StartControlPanel("ncpa.cpl")
                                         End Try
                                     End Sub

        fbWifiOn = MakeButton("WIFI ON", LCARS.LCARScolorStyles.PrimaryFunction)
        AddHandler fbWifiOn.Click, Sub()
                                       If selected Is Nothing Then Return
                                       status.Text = WifiService.SetRadioOn(selected.Name, True)
                                       Reload()
                                   End Sub
        fbWifiOff = MakeButton("WIFI OFF", LCARS.LCARScolorStyles.Orange)
        AddHandler fbWifiOff.Click, Sub()
                                        If selected Is Nothing Then Return
                                        status.Text = WifiService.SetRadioOn(selected.Name, False)
                                        Reload()
                                    End Sub
        fbScan = MakeButton("SCAN", LCARS.LCARScolorStyles.PrimaryFunction)
        AddHandler fbScan.Click, AddressOf OnScan
        fbConnect = MakeButton("CONNECT", LCARS.LCARScolorStyles.PrimaryFunction)
        AddHandler fbConnect.Click, AddressOf OnConnect
        fbDisconnect = MakeButton("DISCONNECT", LCARS.LCARScolorStyles.Orange)
        AddHandler fbDisconnect.Click, Sub()
                                           status.Text = WifiService.Disconnect()
                                           Reload()
                                       End Sub
        fbForget = MakeButton("FORGET", LCARS.LCARScolorStyles.Orange)
        AddHandler fbForget.Click, AddressOf OnForget

        Controls.Add(lstAdapters)
        Controls.Add(lstWifi)
        Controls.Add(lblDetail)
        Controls.Add(status)
        Controls.Add(chkDhcp)
        Controls.Add(lblIp)
        Controls.Add(txtIp)
        Controls.Add(lblMask)
        Controls.Add(txtMask)
        Controls.Add(lblGw)
        Controls.Add(txtGw)
        Controls.Add(lblDns1)
        Controls.Add(txtDns1)
        Controls.Add(lblDns2)
        Controls.Add(txtDns2)
        Controls.Add(fbRefresh)
        Controls.Add(fbEnable)
        Controls.Add(fbDisable)
        Controls.Add(fbApplyIp)
        Controls.Add(fbAdvanced)
        Controls.Add(fbWifiOn)
        Controls.Add(fbWifiOff)
        Controls.Add(fbScan)
        Controls.Add(fbConnect)
        Controls.Add(fbDisconnect)
        Controls.Add(fbForget)

        AddHandler Me.Resize, Sub() LayoutChildren()
        AddHandler Me.VisibleChanged, Sub()
                                          If Visible Then Reload()
                                      End Sub
        LayoutChildren()
    End Sub

    Private Sub StyleList(ByVal lst As ListBox)
        lst.BackColor = Color.Black
        lst.ForeColor = Color.Orange
        lst.BorderStyle = BorderStyle.FixedSingle
        lst.Font = New Font("Arial Narrow", 10.0!, FontStyle.Bold)
        lst.IntegralHeight = False
    End Sub

    Private Sub StyleBox(ByVal t As TextBox)
        t.BackColor = Color.FromArgb(20, 20, 20)
        t.ForeColor = Color.Orange
        t.BorderStyle = BorderStyle.FixedSingle
        t.Font = New Font("Arial Narrow", 10.0!, FontStyle.Bold)
    End Sub

    Private Sub StyleField(ByVal l As Label, ByVal caption As String)
        l.Text = caption
        l.ForeColor = Color.Orange
        l.BackColor = Color.Black
        l.Font = New Font("Arial Narrow", 9.0!, FontStyle.Bold)
        l.AutoSize = False
        l.TextAlign = ContentAlignment.MiddleLeft
    End Sub

    Private Function MakeButton(ByVal caption As String, ByVal color As LCARS.LCARScolorStyles) As FlatButton
        Dim b As New FlatButton()
        b.ButtonText = caption
        b.Text = caption
        b.Clickable = True
        b.Beeping = True
        b.Color = color
        b.ButtonTextAlign = ContentAlignment.BottomRight
        b.Size = New Size(96, 26)
        Return b
    End Function

    Private Sub LayoutChildren()
        Dim pad As Integer = 4
        Dim w As Integer = Math.Max(200, ClientSize.Width)
        Dim h As Integer = Math.Max(200, ClientSize.Height)
        Dim y As Integer = pad

        Dim btnW As Integer = 96
        Dim gap As Integer = 4
        Dim x As Integer = pad
        For Each b As FlatButton In New FlatButton() {fbRefresh, fbEnable, fbDisable, fbApplyIp, fbAdvanced}
            b.Location = New Point(x, y)
            b.Size = New Size(btnW, 26)
            x += btnW + gap
        Next
        y += 30

        Dim listH As Integer = Math.Max(70, h \ 5)
        lstAdapters.SetBounds(pad, y, Math.Max(80, w - pad * 2), listH)
        y += listH + 4

        lblDetail.SetBounds(pad, y, Math.Max(80, w - pad * 2), 48)
        y += 52

        chkDhcp.Location = New Point(pad, y + 2)
        Dim fieldY As Integer = y
        Dim col1 As Integer = pad + 70
        Dim fieldW As Integer = Math.Max(90, (w - col1 - pad * 2) \ 2 - 8)
        PlaceField(lblIp, txtIp, col1, fieldY, fieldW)
        PlaceField(lblMask, txtMask, col1 + fieldW + 8, fieldY, fieldW)
        fieldY += 26
        PlaceField(lblGw, txtGw, col1, fieldY, fieldW)
        PlaceField(lblDns1, txtDns1, col1 + fieldW + 8, fieldY, fieldW)
        fieldY += 26
        PlaceField(lblDns2, txtDns2, col1, fieldY, fieldW)
        y = fieldY + 28

        x = pad
        For Each b As FlatButton In New FlatButton() {fbWifiOn, fbWifiOff, fbScan, fbConnect, fbDisconnect, fbForget}
            b.Location = New Point(x, y)
            b.Size = New Size(btnW, 26)
            x += btnW + gap
        Next
        y += 30

        status.SetBounds(pad, Math.Max(y + 4, h - 20), Math.Max(80, w - pad * 2), 18)
        Dim wifiH As Integer = Math.Max(40, status.Top - y - 4)
        lstWifi.SetBounds(pad, y, Math.Max(80, w - pad * 2), wifiH)
    End Sub

    Private Sub PlaceField(ByVal lbl As Label, ByVal box As TextBox, ByVal x As Integer, ByVal y As Integer, ByVal fieldW As Integer)
        lbl.SetBounds(x, y, 40, 22)
        box.SetBounds(x + 42, y, Math.Max(60, fieldW - 42), 22)
    End Sub

    Public Sub Reload()
        If _busy Then Return
        _busy = True
        Try
            Dim keepName As String = If(selected IsNot Nothing, selected.Name, "")
            adapters = NetworkAdapterService.ListAdapters()
            lstAdapters.BeginUpdate()
            lstAdapters.Items.Clear()
            Dim selectIndex As Integer = -1
            For i As Integer = 0 To adapters.Count - 1
                lstAdapters.Items.Add(adapters(i))
                If keepName <> "" AndAlso String.Equals(adapters(i).Name, keepName, StringComparison.OrdinalIgnoreCase) Then
                    selectIndex = i
                End If
            Next
            lstAdapters.EndUpdate()
            If selectIndex >= 0 Then
                lstAdapters.SelectedIndex = selectIndex
            ElseIf lstAdapters.Items.Count > 0 Then
                lstAdapters.SelectedIndex = 0
            Else
                selected = Nothing
                lblDetail.Text = "NO ADAPTERS"
            End If
            status.Text = adapters.Count.ToString() & " ADAPTERS"
        Finally
            _busy = False
        End Try
        LayoutChildren()
    End Sub

    Private Sub OnAdapterSelected(ByVal sender As Object, ByVal e As EventArgs)
        selected = TryCast(lstAdapters.SelectedItem, NetworkAdapterInfo)
        If selected Is Nothing Then Return
        Dim fresh As NetworkAdapterInfo = NetworkAdapterService.FindByName(selected.Name)
        If fresh IsNot Nothing Then selected = fresh

        Dim ssid As String = ""
        If selected.IsWireless Then ssid = WifiService.GetConnectedSsid()

        lblDetail.Text =
            selected.Name.ToUpperInvariant() & " — " & selected.TypeLabel & " — " & selected.StatusLabel & ControlChars.CrLf &
            selected.Description & ControlChars.CrLf &
            "MAC " & selected.Mac &
            If(selected.SpeedMbps > 0, "  ·  " & selected.SpeedMbps.ToString() & " Mbps", "") &
            If(ssid <> "", "  ·  SSID " & ssid, "")

        chkDhcp.Checked = selected.DhcpEnabled
        txtIp.Text = selected.Ip
        txtMask.Text = selected.SubnetMask
        txtGw.Text = selected.Gateway
        txtDns1.Text = selected.Dns1
        txtDns2.Text = selected.Dns2
        Dim staticOn As Boolean = Not chkDhcp.Checked
        txtIp.Enabled = staticOn
        txtMask.Enabled = staticOn
        txtGw.Enabled = staticOn
        txtDns1.Enabled = staticOn
        txtDns2.Enabled = staticOn

        Dim wifi As Boolean = selected.IsWireless
        fbWifiOn.Visible = wifi
        fbWifiOff.Visible = wifi
        fbScan.Visible = wifi
        fbConnect.Visible = wifi
        fbDisconnect.Visible = wifi
        fbForget.Visible = wifi
        lstWifi.Visible = wifi
        If Not wifi Then
            lstWifi.Items.Clear()
            wifiNets.Clear()
        End If
        LayoutChildren()
    End Sub

    Private Sub SetAdmin(ByVal enabled As Boolean)
        If selected Is Nothing Then
            status.Text = "SELECT AN ADAPTER"
            Return
        End If
        status.Text = NetworkAdapterService.SetAdminEnabled(selected.Name, enabled)
        Reload()
    End Sub

    Private Sub OnApplyIp(ByVal sender As Object, ByVal e As EventArgs)
        If selected Is Nothing Then
            status.Text = "SELECT AN ADAPTER"
            Return
        End If
        If chkDhcp.Checked Then
            status.Text = NetworkAdapterService.ApplyDhcp(selected.Name)
        Else
            status.Text = NetworkAdapterService.ApplyStatic(
                selected.Name, txtIp.Text, txtMask.Text, txtGw.Text, txtDns1.Text, txtDns2.Text)
        End If
        Reload()
    End Sub

    Private Sub OnScan(ByVal sender As Object, ByVal e As EventArgs)
        status.Text = "SCANNING…"
        status.Refresh()
        wifiNets = WifiService.ScanNetworks()
        lstWifi.BeginUpdate()
        lstWifi.Items.Clear()
        For Each n As WifiNetwork In wifiNets
            lstWifi.Items.Add(n)
        Next
        lstWifi.EndUpdate()
        status.Text = wifiNets.Count.ToString() & " NETWORKS FOUND"
    End Sub

    Private Sub OnConnect(ByVal sender As Object, ByVal e As EventArgs)
        Dim net As WifiNetwork = TryCast(lstWifi.SelectedItem, WifiNetwork)
        If net Is Nothing OrElse String.IsNullOrEmpty(net.SSID) Then
            status.Text = "SELECT A WIFI NETWORK"
            Return
        End If
        Dim result As String = WifiService.Connect(net.SSID, "", net.Auth)
        If result.StartsWith("NEED_PASSWORD:", StringComparison.OrdinalIgnoreCase) Then
            Dim pwd As String = InputBox("Passphrase for " & net.SSID, "WI-FI PASSWORD", "")
            If String.IsNullOrEmpty(pwd) Then
                status.Text = "CONNECT CANCELLED"
                Return
            End If
            result = WifiService.Connect(net.SSID, pwd, net.Auth)
        End If
        status.Text = FirstLine(result)
        Reload()
    End Sub

    Private Sub OnForget(ByVal sender As Object, ByVal e As EventArgs)
        Dim net As WifiNetwork = TryCast(lstWifi.SelectedItem, WifiNetwork)
        Dim ssid As String = If(net IsNot Nothing, net.SSID, WifiService.GetConnectedSsid())
        If String.IsNullOrEmpty(ssid) Then
            status.Text = "SELECT A NETWORK OR CONNECT FIRST"
            Return
        End If
        status.Text = WifiService.ForgetProfile(ssid)
        OnScan(sender, e)
    End Sub

    Private Function FirstLine(ByVal text As String) As String
        If text Is Nothing Then Return ""
        For Each line As String In text.Split(New String() {ControlChars.CrLf, ControlChars.Lf}, StringSplitOptions.RemoveEmptyEntries)
            Dim t As String = line.Trim()
            If t <> "" Then Return t
        Next
        Return text.Trim()
    End Function
End Class

End Namespace
