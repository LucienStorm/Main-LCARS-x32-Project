' LCARSTerminal/Rdp/RdpConnectionListPanel.vb
Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms
Imports LCARS.Controls

''' <summary>
''' Remote connect UI: History | Connect form | Network discovery.
''' </summary>
Public Class RdpConnectionListPanel
    Inherits Panel

    Public Event ConnectRequested As EventHandler(Of RdpConnectionProfile)
    Public Event StatusMessage As EventHandler(Of String)

    Private ReadOnly _store As RdpProfileStore
    Private ReadOnly _historyStore As New RemoteHistoryStore()
    Private ReadOnly _credStore As New RdpCredentialStore()
    Private ReadOnly _profiles As New List(Of RdpConnectionProfile)()
    Private ReadOnly _history As New List(Of RemoteHistoryEntry)()
    Private ReadOnly _discovered As New List(Of RemoteDiscoveredHost)()

    Private ReadOnly _card As New Panel()
    Private ReadOnly lblTitle As New Label()
    Private ReadOnly lblHost As New Label()
    Private ReadOnly lblUser As New Label()
    Private ReadOnly lblPass As New Label()
    Private ReadOnly txtHost As New TextBox()
    Private ReadOnly txtUser As New TextBox()
    Private ReadOnly txtPass As New TextBox()
    Private ReadOnly chkRemember As New CheckBox()
    Private ReadOnly fbConnect As FlatButton
    Private ReadOnly fbProtoRdp As FlatButton
    Private ReadOnly fbProtoVnc As FlatButton

    Private ReadOnly lblHistory As New Label()
    Private ReadOnly lstHistory As New ListBox()
    Private ReadOnly lblSaved As New Label()
    Private ReadOnly _list As New ListBox()
    Private ReadOnly lblDiscover As New Label()
    Private ReadOnly lstDiscover As New ListBox()
    Private ReadOnly fbRefreshNet As FlatButton

    Private _protocol As RemoteProtocol = RemoteProtocol.Rdp
    Private _redirectDrives As Boolean
    Private _redirectPrinters As Boolean
    Private _redirectClipboard As Boolean = True
    Private _redirectAudio As Boolean = True
    Private _smartSizing As Boolean = True
    Private _discoverBusy As Boolean

    Public Sub New(ByVal store As RdpProfileStore)
        _store = store
        BackColor = Color.Black

        _card.BackColor = Color.FromArgb(18, 18, 18)

        lblTitle.Text = "REMOTE SESSION"
        lblTitle.ForeColor = Color.Orange
        lblTitle.Font = New Font("Arial Narrow", 16.0F, FontStyle.Bold)
        lblTitle.AutoSize = True
        lblTitle.Location = New Point(16, 12)

        fbProtoRdp = MakeProtoButton("RDP", 16, 44)
        fbProtoVnc = MakeProtoButton("VNC", 100, 44)
        AddHandler fbProtoRdp.Click, Sub() SetProtocolUi(RemoteProtocol.Rdp)
        AddHandler fbProtoVnc.Click, Sub() SetProtocolUi(RemoteProtocol.Vnc)

        StyleLabel(lblHost, "HOSTNAME", 16, 84)
        StyleField(txtHost, 16, 104, 280)
        StyleLabel(lblUser, "USERNAME", 16, 140)
        StyleField(txtUser, 16, 160, 280)
        StyleLabel(lblPass, "PASSWORD", 16, 196)
        StyleField(txtPass, 16, 216, 280)
        txtPass.UseSystemPasswordChar = True

        chkRemember.Text = "REMEMBER PASSWORD"
        chkRemember.ForeColor = Color.Orange
        chkRemember.BackColor = Color.FromArgb(18, 18, 18)
        chkRemember.AutoSize = True
        chkRemember.Location = New Point(16, 252)

        fbConnect = New FlatButton()
        fbConnect.ButtonText = "CONNECT"
        fbConnect.Text = "CONNECT"
        fbConnect.Color = LCARS.LCARScolorStyles.PrimaryFunction
        fbConnect.Beeping = True
        fbConnect.ButtonTextAlign = ContentAlignment.MiddleCenter
        fbConnect.Size = New Size(160, 36)
        fbConnect.Location = New Point(16, 286)
        AddHandler fbConnect.Click, AddressOf OnCardConnect

        AddHandler txtHost.KeyDown, AddressOf OnFieldKeyDown
        AddHandler txtUser.KeyDown, AddressOf OnFieldKeyDown
        AddHandler txtPass.KeyDown, AddressOf OnFieldKeyDown

        StyleSideLabel(lblHistory, "HISTORY")
        StyleList(lstHistory)
        AddHandler lstHistory.SelectedIndexChanged, AddressOf OnHistorySelected
        AddHandler lstHistory.DoubleClick, AddressOf OnHistoryConnect

        StyleSideLabel(lblSaved, "SAVED")
        StyleList(_list)
        AddHandler _list.SelectedIndexChanged, AddressOf OnSelected
        AddHandler _list.DoubleClick, AddressOf OnListConnect

        StyleSideLabel(lblDiscover, "ON THIS NETWORK")
        StyleList(lstDiscover)
        AddHandler lstDiscover.SelectedIndexChanged, AddressOf OnDiscoverSelected
        AddHandler lstDiscover.DoubleClick, AddressOf OnDiscoverConnect

        fbRefreshNet = New FlatButton()
        fbRefreshNet.ButtonText = "SCAN"
        fbRefreshNet.Text = "SCAN"
        fbRefreshNet.Color = LCARS.LCARScolorStyles.NavigationFunction
        fbRefreshNet.Beeping = True
        fbRefreshNet.ButtonTextAlign = ContentAlignment.MiddleCenter
        fbRefreshNet.Size = New Size(72, 24)
        AddHandler fbRefreshNet.Click, AddressOf OnRefreshNetwork

        _card.Controls.Add(lblTitle)
        _card.Controls.Add(fbProtoRdp)
        _card.Controls.Add(fbProtoVnc)
        _card.Controls.Add(lblHost)
        _card.Controls.Add(txtHost)
        _card.Controls.Add(lblUser)
        _card.Controls.Add(txtUser)
        _card.Controls.Add(lblPass)
        _card.Controls.Add(txtPass)
        _card.Controls.Add(chkRemember)
        _card.Controls.Add(fbConnect)

        Controls.Add(lblHistory)
        Controls.Add(lstHistory)
        Controls.Add(lblSaved)
        Controls.Add(_list)
        Controls.Add(_card)
        Controls.Add(lblDiscover)
        Controls.Add(fbRefreshNet)
        Controls.Add(lstDiscover)

        AddHandler Me.Resize, AddressOf OnPanelResize
        AddHandler Me.HandleCreated, AddressOf OnHandleCreatedOnce
        SetProtocolUi(RemoteProtocol.Rdp)
        LayoutChildren()
        Reload()
    End Sub

    Private Sub OnHandleCreatedOnce(ByVal sender As Object, ByVal e As EventArgs)
        RemoveHandler Me.HandleCreated, AddressOf OnHandleCreatedOnce
        BeginInvoke(New MethodInvoker(AddressOf StartNetworkScan))
    End Sub

    Public Property RedirectDrives As Boolean
        Get
            Return _redirectDrives
        End Get
        Set(ByVal value As Boolean)
            _redirectDrives = value
        End Set
    End Property

    Public Property RedirectPrinters As Boolean
        Get
            Return _redirectPrinters
        End Get
        Set(ByVal value As Boolean)
            _redirectPrinters = value
        End Set
    End Property

    Public Property RedirectClipboard As Boolean
        Get
            Return _redirectClipboard
        End Get
        Set(ByVal value As Boolean)
            _redirectClipboard = value
        End Set
    End Property

    Public Property RedirectAudio As Boolean
        Get
            Return _redirectAudio
        End Get
        Set(ByVal value As Boolean)
            _redirectAudio = value
        End Set
    End Property

    Public Property SmartSizing As Boolean
        Get
            Return _smartSizing
        End Get
        Set(ByVal value As Boolean)
            _smartSizing = value
        End Set
    End Property

    Public ReadOnly Property EnteredPassword As String
        Get
            Return txtPass.Text
        End Get
    End Property

    Public Sub Reload()
        _profiles.Clear()
        _profiles.AddRange(_store.LoadAll())
        _list.Items.Clear()
        For Each p As RdpConnectionProfile In _profiles
            _list.Items.Add(FormatItem(p))
        Next
        ReloadHistory()
    End Sub

    Public Sub ReloadHistory()
        _history.Clear()
        _history.AddRange(_historyStore.LoadAll())
        lstHistory.Items.Clear()
        For Each h As RemoteHistoryEntry In _history
            lstHistory.Items.Add(h.DisplayText())
        Next
    End Sub

    Public Sub DoConnect()
        Dim profile As RdpConnectionProfile = Nothing
        If _list.SelectedIndex >= 0 AndAlso _list.SelectedIndex < _profiles.Count Then
            profile = _profiles(_list.SelectedIndex)
            If Not String.IsNullOrWhiteSpace(txtHost.Text) Then profile.Hostname = txtHost.Text.Trim()
            If Not String.IsNullOrWhiteSpace(txtUser.Text) Then profile.Username = txtUser.Text.Trim()
            profile.SetProtocolKind(_protocol)
            ApplyOptionsToProfile(profile)
            profile.RememberPassword = chkRemember.Checked
        Else
            If String.IsNullOrWhiteSpace(txtHost.Text) Then
                RaiseEvent StatusMessage(Me, "Enter a hostname to connect.")
                Return
            End If
            profile = BuildQuickProfile()
        End If
        RaiseEvent ConnectRequested(Me, profile)
    End Sub

    Public Sub DoNew()
        If String.IsNullOrWhiteSpace(txtHost.Text) Then
            RaiseEvent StatusMessage(Me, "Enter a hostname before saving NEW.")
            Return
        End If
        Dim p As RdpConnectionProfile = BuildQuickProfile()
        _profiles.Add(p)
        _store.SaveAll(_profiles)
        Reload()
        _list.SelectedIndex = _profiles.Count - 1
        RaiseEvent StatusMessage(Me, "Saved profile " & p.TabLabel())
    End Sub

    Public Sub DoDelete()
        If _list.SelectedIndex < 0 OrElse _list.SelectedIndex >= _profiles.Count Then
            RaiseEvent StatusMessage(Me, "Select a saved connection to delete.")
            Return
        End If
        Dim p As RdpConnectionProfile = _profiles(_list.SelectedIndex)
        Try
            _credStore.DeletePassword(p.Id)
        Catch
        End Try
        _profiles.RemoveAt(_list.SelectedIndex)
        _store.SaveAll(_profiles)
        Reload()
        RaiseEvent StatusMessage(Me, "Deleted profile")
    End Sub

    Public Sub DoOpenRdp()
        Using dlg As New OpenFileDialog()
            dlg.Filter = "Remote Desktop (*.rdp)|*.rdp|All files (*.*)|*.*"
            dlg.Title = "Open .rdp"
            If dlg.ShowDialog(FindForm()) <> DialogResult.OK Then Return
            Dim text As String = File.ReadAllText(dlg.FileName)
            Dim p As RdpConnectionProfile = Nothing
            If Not RdpFileCodec.TryParse(text, p) Then
                RaiseEvent StatusMessage(Me, "Could not parse .rdp file")
                Return
            End If
            p.SetProtocolKind(RemoteProtocol.Rdp)
            _profiles.Add(p)
            _store.SaveAll(_profiles)
            Reload()
            _list.SelectedIndex = _profiles.Count - 1
            LoadProfileIntoForm(p)
            RaiseEvent StatusMessage(Me, "Imported " & p.TabLabel())
        End Using
    End Sub

    Public Sub DoSaveRdp()
        Dim p As RdpConnectionProfile = Nothing
        If _list.SelectedIndex >= 0 AndAlso _list.SelectedIndex < _profiles.Count Then
            p = _profiles(_list.SelectedIndex)
            If Not String.IsNullOrWhiteSpace(txtHost.Text) Then p.Hostname = txtHost.Text.Trim()
            If Not String.IsNullOrWhiteSpace(txtUser.Text) Then p.Username = txtUser.Text.Trim()
            ApplyOptionsToProfile(p)
            _store.SaveAll(_profiles)
        ElseIf Not String.IsNullOrWhiteSpace(txtHost.Text) Then
            p = BuildQuickProfile()
        Else
            RaiseEvent StatusMessage(Me, "Select or enter a host to save .rdp")
            Return
        End If
        If p.ProtocolKind() <> RemoteProtocol.Rdp Then
            RaiseEvent StatusMessage(Me, "SAVE .RDP is only for RDP profiles.")
            Return
        End If
        Using dlg As New SaveFileDialog()
            dlg.Filter = "Remote Desktop (*.rdp)|*.rdp"
            dlg.FileName = p.TabLabel() & ".rdp"
            If dlg.ShowDialog(FindForm()) <> DialogResult.OK Then Return
            File.WriteAllText(dlg.FileName, RdpFileCodec.FromProfile(p))
            RaiseEvent StatusMessage(Me, "Saved " & Path.GetFileName(dlg.FileName))
        End Using
    End Sub

    Public Function BuildQuickProfile() As RdpConnectionProfile
        Dim p As New RdpConnectionProfile()
        p.Id = Guid.NewGuid()
        p.Hostname = txtHost.Text.Trim()
        p.DisplayName = p.Hostname
        p.Username = txtUser.Text.Trim()
        p.SetProtocolKind(_protocol)
        p.RememberPassword = chkRemember.Checked
        ApplyOptionsToProfile(p)
        Return p
    End Function

    Private Sub ApplyOptionsToProfile(ByVal p As RdpConnectionProfile)
        p.RedirectDrives = _redirectDrives
        p.RedirectPrinters = _redirectPrinters
        p.RedirectClipboard = _redirectClipboard
        p.RedirectAudio = _redirectAudio
        p.SmartSizing = _smartSizing
    End Sub

    Private Sub LoadProfileIntoForm(ByVal p As RdpConnectionProfile)
        txtHost.Text = p.Hostname
        txtUser.Text = p.Username
        chkRemember.Checked = p.RememberPassword
        _redirectDrives = p.RedirectDrives
        _redirectPrinters = p.RedirectPrinters
        _redirectClipboard = p.RedirectClipboard
        _redirectAudio = p.RedirectAudio
        _smartSizing = p.SmartSizing
        SetProtocolUi(p.ProtocolKind())
        TryLoadPassword(p.Id)
    End Sub

    Private Sub TryLoadPassword(ByVal profileId As Guid)
        Dim pwd As String = Nothing
        If _credStore.TryGetPassword(profileId, pwd) Then
            txtPass.Text = pwd
        End If
    End Sub

    Private Sub SetProtocolUi(ByVal kind As RemoteProtocol)
        _protocol = kind
        fbProtoRdp.Color = If(kind = RemoteProtocol.Rdp, LCARS.LCARScolorStyles.PrimaryFunction, LCARS.LCARScolorStyles.LCARSDisplayOnly)
        fbProtoVnc.Color = If(kind = RemoteProtocol.Vnc, LCARS.LCARScolorStyles.PrimaryFunction, LCARS.LCARScolorStyles.LCARSDisplayOnly)
        If kind = RemoteProtocol.Vnc Then
            lblUser.Text = "USERNAME (optional)"
            lblPass.Text = "VNC PASSWORD"
        Else
            lblUser.Text = "USERNAME"
            lblPass.Text = "PASSWORD"
        End If
    End Sub

    Private Function MakeProtoButton(ByVal caption As String, ByVal x As Integer, ByVal y As Integer) As FlatButton
        Dim btn As New FlatButton()
        btn.ButtonText = caption
        btn.Text = caption
        btn.Beeping = True
        btn.ButtonTextAlign = ContentAlignment.MiddleCenter
        btn.Size = New Size(72, 26)
        btn.Location = New Point(x, y)
        btn.Color = LCARS.LCARScolorStyles.LCARSDisplayOnly
        Return btn
    End Function

    Private Sub StyleLabel(ByVal lbl As Label, ByVal text As String, ByVal x As Integer, ByVal y As Integer)
        lbl.Text = text
        lbl.ForeColor = Color.Orange
        lbl.AutoSize = True
        lbl.Location = New Point(x, y)
    End Sub

    Private Sub StyleSideLabel(ByVal lbl As Label, ByVal text As String)
        lbl.Text = text
        lbl.ForeColor = Color.Orange
        lbl.AutoSize = True
        lbl.Font = New Font("Arial Narrow", 11.0F, FontStyle.Bold)
    End Sub

    Private Sub StyleField(ByVal box As TextBox, ByVal x As Integer, ByVal y As Integer, ByVal width As Integer)
        box.Location = New Point(x, y)
        box.Size = New Size(width, 24)
        box.BackColor = Color.FromArgb(40, 40, 40)
        box.ForeColor = Color.White
        box.BorderStyle = BorderStyle.FixedSingle
    End Sub

    Private Sub StyleList(ByVal box As ListBox)
        box.BackColor = Color.Black
        box.ForeColor = Color.Orange
        box.BorderStyle = BorderStyle.FixedSingle
        box.Font = New Font("Arial Narrow", 11.0F, FontStyle.Bold)
    End Sub

    Private Function FormatItem(ByVal p As RdpConnectionProfile) As String
        Dim tag As String = If(p.ProtocolKind() = RemoteProtocol.Vnc, "VNC ", "RDP ")
        Dim title As String = p.TabLabel()
        If title.StartsWith("VNC ", StringComparison.Ordinal) Then title = title.Substring(4)
        If Not String.IsNullOrWhiteSpace(p.Username) Then
            Return tag & title & "  —  " & p.Username
        End If
        Return tag & title
    End Function

    Private Sub LayoutChildren()
        Dim gap As Integer = 8
        Dim sideW As Integer = Math.Max(160, Math.Min(220, (ClientSize.Width - 320) \ 2))
        Dim cardW As Integer = Math.Max(300, ClientSize.Width - sideW * 2 - gap * 4)
        Dim cardH As Integer = 340
        Dim topPad As Integer = 8

        lblHistory.Location = New Point(gap, topPad)
        Dim leftTop As Integer = topPad + 20
        Dim leftH As Integer = Math.Max(80, ClientSize.Height - leftTop - gap)
        Dim histH As Integer = CInt(leftH * 0.45)
        lstHistory.Location = New Point(gap, leftTop)
        lstHistory.Size = New Size(sideW, histH)

        lblSaved.Location = New Point(gap, lstHistory.Bottom + 6)
        _list.Location = New Point(gap, lblSaved.Bottom + 2)
        _list.Size = New Size(sideW, Math.Max(60, ClientSize.Height - _list.Top - gap))

        _card.Size = New Size(cardW, cardH)
        _card.Location = New Point(gap * 2 + sideW, Math.Max(topPad, (ClientSize.Height - cardH) \ 2))
        txtHost.Width = cardW - 32
        txtUser.Width = cardW - 32
        txtPass.Width = cardW - 32
        fbConnect.Width = Math.Min(180, cardW - 32)

        Dim rightX As Integer = _card.Right + gap
        lblDiscover.Location = New Point(rightX, topPad)
        fbRefreshNet.Location = New Point(rightX + sideW - fbRefreshNet.Width, topPad)
        lstDiscover.Location = New Point(rightX, topPad + 22)
        lstDiscover.Size = New Size(sideW, Math.Max(80, ClientSize.Height - lstDiscover.Top - gap))
    End Sub

    Private Sub OnPanelResize(ByVal sender As Object, ByVal e As EventArgs)
        LayoutChildren()
    End Sub

    Private Sub OnSelected(ByVal sender As Object, ByVal e As EventArgs)
        If _list.SelectedIndex < 0 OrElse _list.SelectedIndex >= _profiles.Count Then Return
        lstHistory.ClearSelected()
        lstDiscover.ClearSelected()
        LoadProfileIntoForm(_profiles(_list.SelectedIndex))
        RaiseEvent StatusMessage(Me, "Loaded " & _profiles(_list.SelectedIndex).TabLabel())
    End Sub

    Private Sub OnHistorySelected(ByVal sender As Object, ByVal e As EventArgs)
        If lstHistory.SelectedIndex < 0 OrElse lstHistory.SelectedIndex >= _history.Count Then Return
        _list.ClearSelected()
        lstDiscover.ClearSelected()
        Dim h As RemoteHistoryEntry = _history(lstHistory.SelectedIndex)
        txtHost.Text = h.Hostname
        txtUser.Text = h.Username
        SetProtocolUi(h.ProtocolKind())
        Dim pid As Guid
        If Guid.TryParseExact(If(h.ProfileId, ""), "N", pid) OrElse Guid.TryParse(If(h.ProfileId, ""), pid) Then
            TryLoadPassword(pid)
            chkRemember.Checked = True
        End If
        RaiseEvent StatusMessage(Me, "History — " & h.DisplayText())
    End Sub

    Private Sub OnDiscoverSelected(ByVal sender As Object, ByVal e As EventArgs)
        If lstDiscover.SelectedIndex < 0 OrElse lstDiscover.SelectedIndex >= _discovered.Count Then Return
        _list.ClearSelected()
        lstHistory.ClearSelected()
        Dim d As RemoteDiscoveredHost = _discovered(lstDiscover.SelectedIndex)
        Dim preferVnc As Boolean = d.VncOpen AndAlso Not d.RdpOpen
        If d.RdpOpen AndAlso d.VncOpen Then preferVnc = (_protocol = RemoteProtocol.Vnc)
        SetProtocolUi(If(preferVnc, RemoteProtocol.Vnc, RemoteProtocol.Rdp))
        txtHost.Text = If(String.IsNullOrWhiteSpace(d.Hostname), d.IpAddress, d.Hostname)
        ' Prefer saved creds matching this host.
        For Each p As RdpConnectionProfile In _profiles
            If String.Equals(p.Hostname, txtHost.Text, StringComparison.OrdinalIgnoreCase) OrElse
               String.Equals(p.Hostname, d.IpAddress, StringComparison.OrdinalIgnoreCase) Then
                If p.ProtocolKind() = _protocol Then
                    txtUser.Text = p.Username
                    TryLoadPassword(p.Id)
                    chkRemember.Checked = p.RememberPassword
                    Exit For
                End If
            End If
        Next
        RaiseEvent StatusMessage(Me, "Network — " & d.DisplayText())
    End Sub

    Private Sub OnListConnect(ByVal sender As Object, ByVal e As EventArgs)
        DoConnect()
    End Sub

    Private Sub OnHistoryConnect(ByVal sender As Object, ByVal e As EventArgs)
        DoConnect()
    End Sub

    Private Sub OnDiscoverConnect(ByVal sender As Object, ByVal e As EventArgs)
        DoConnect()
    End Sub

    Private Sub OnCardConnect(ByVal sender As Object, ByVal e As EventArgs)
        DoConnect()
    End Sub

    Private Sub OnFieldKeyDown(ByVal sender As Object, ByVal e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            e.SuppressKeyPress = True
            DoConnect()
        End If
    End Sub

    Private Sub OnRefreshNetwork(ByVal sender As Object, ByVal e As EventArgs)
        StartNetworkScan()
    End Sub

    Private Sub StartNetworkScan()
        If _discoverBusy Then Return
        _discoverBusy = True
        fbRefreshNet.ButtonText = "…"
        fbRefreshNet.Text = "…"
        RaiseEvent StatusMessage(Me, "Scanning LAN for RDP/VNC…")
        Dim worker As New BackgroundWorker()
        AddHandler worker.DoWork, Sub(s As Object, args As DoWorkEventArgs)
                                      args.Result = RemoteNetworkDiscovery.ScanLocalSubnet(300)
                                  End Sub
        AddHandler worker.RunWorkerCompleted, Sub(s As Object, args As RunWorkerCompletedEventArgs)
                                                  _discoverBusy = False
                                                  fbRefreshNet.ButtonText = "SCAN"
                                                  fbRefreshNet.Text = "SCAN"
                                                  If args.Error IsNot Nothing Then
                                                      RaiseEvent StatusMessage(Me, "Scan failed: " & args.Error.Message)
                                                      Return
                                                  End If
                                                  _discovered.Clear()
                                                  lstDiscover.Items.Clear()
                                                  Dim list As List(Of RemoteDiscoveredHost) = TryCast(args.Result, List(Of RemoteDiscoveredHost))
                                                  If list Is Nothing Then list = New List(Of RemoteDiscoveredHost)()
                                                  _discovered.AddRange(list)
                                                  For Each d As RemoteDiscoveredHost In _discovered
                                                      lstDiscover.Items.Add(d.DisplayText())
                                                  Next
                                                  RaiseEvent StatusMessage(Me, "Found " & _discovered.Count.ToString() & " host(s) on the network")
                                              End Sub
        worker.RunWorkerAsync()
    End Sub
End Class
