' LCARSTerminal/Rdp/RdpConnectionListPanel.vb
Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms
Imports LCARS.Controls

''' <summary>
''' Centered RDP connect form + saved profile list (rail also has actions).
''' </summary>
Public Class RdpConnectionListPanel
    Inherits Panel

    Public Event ConnectRequested As EventHandler(Of RdpConnectionProfile)
    Public Event StatusMessage As EventHandler(Of String)

    Private ReadOnly _store As RdpProfileStore
    Private ReadOnly _profiles As New List(Of RdpConnectionProfile)()
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
    Private ReadOnly _list As New ListBox()
    Private ReadOnly lblSaved As New Label()

    ' Option flags owned by top chrome; updated via ApplyOptions / read via getters.
    Private _redirectDrives As Boolean
    Private _redirectPrinters As Boolean
    Private _redirectClipboard As Boolean = True
    Private _redirectAudio As Boolean = True
    Private _smartSizing As Boolean = True

    Public Sub New(ByVal store As RdpProfileStore)
        _store = store
        BackColor = Color.Black

        _card.BackColor = Color.FromArgb(18, 18, 18)
        _card.Size = New Size(420, 320)

        lblTitle.Text = "REMOTE DESKTOP"
        lblTitle.ForeColor = Color.Orange
        lblTitle.Font = New Font("Arial Narrow", 16.0F, FontStyle.Bold)
        lblTitle.AutoSize = True
        lblTitle.Location = New Point(24, 16)

        StyleLabel(lblHost, "HOSTNAME", 24, 56)
        StyleField(txtHost, 24, 76, 372)
        StyleLabel(lblUser, "USERNAME", 24, 112)
        StyleField(txtUser, 24, 132, 372)
        StyleLabel(lblPass, "PASSWORD", 24, 168)
        StyleField(txtPass, 24, 188, 372)
        txtPass.UseSystemPasswordChar = True

        chkRemember.Text = "REMEMBER PASSWORD"
        chkRemember.ForeColor = Color.Orange
        chkRemember.BackColor = Color.FromArgb(18, 18, 18)
        chkRemember.AutoSize = True
        chkRemember.Location = New Point(24, 228)

        fbConnect = New FlatButton()
        fbConnect.ButtonText = "CONNECT"
        fbConnect.Text = "CONNECT"
        fbConnect.Color = LCARS.LCARScolorStyles.PrimaryFunction
        fbConnect.Beeping = True
        fbConnect.ButtonTextAlign = ContentAlignment.MiddleCenter
        fbConnect.Size = New Size(160, 32)
        fbConnect.Location = New Point(24, 262)
        AddHandler fbConnect.Click, AddressOf OnCardConnect

        AddHandler txtHost.KeyDown, AddressOf OnFieldKeyDown
        AddHandler txtUser.KeyDown, AddressOf OnFieldKeyDown
        AddHandler txtPass.KeyDown, AddressOf OnFieldKeyDown

        lblSaved.Text = "SAVED CONNECTIONS"
        lblSaved.ForeColor = Color.Orange
        lblSaved.AutoSize = True

        _list.BackColor = Color.Black
        _list.ForeColor = Color.Orange
        _list.BorderStyle = BorderStyle.FixedSingle
        _list.Font = New Font("Arial Narrow", 12.0F, FontStyle.Bold)
        AddHandler _list.SelectedIndexChanged, AddressOf OnSelected
        AddHandler _list.DoubleClick, AddressOf OnListConnect

        _card.Controls.Add(lblTitle)
        _card.Controls.Add(lblHost)
        _card.Controls.Add(txtHost)
        _card.Controls.Add(lblUser)
        _card.Controls.Add(txtUser)
        _card.Controls.Add(lblPass)
        _card.Controls.Add(txtPass)
        _card.Controls.Add(chkRemember)
        _card.Controls.Add(fbConnect)

        Controls.Add(_card)
        Controls.Add(lblSaved)
        Controls.Add(_list)

        AddHandler Me.Resize, AddressOf OnPanelResize
        LayoutChildren()
        Reload()
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
    End Sub

    Public Sub DoConnect()
        Dim profile As RdpConnectionProfile = Nothing
        If _list.SelectedIndex >= 0 AndAlso _list.SelectedIndex < _profiles.Count Then
            profile = _profiles(_list.SelectedIndex)
            If Not String.IsNullOrWhiteSpace(txtHost.Text) Then profile.Hostname = txtHost.Text.Trim()
            If Not String.IsNullOrWhiteSpace(txtUser.Text) Then profile.Username = txtUser.Text.Trim()
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
            Dim creds As New RdpCredentialStore()
            creds.DeletePassword(p.Id)
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
        p.Port = 3389
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
    End Sub

    Private Sub StyleLabel(ByVal lbl As Label, ByVal text As String, ByVal x As Integer, ByVal y As Integer)
        lbl.Text = text
        lbl.ForeColor = Color.Orange
        lbl.AutoSize = True
        lbl.Location = New Point(x, y)
    End Sub

    Private Sub StyleField(ByVal box As TextBox, ByVal x As Integer, ByVal y As Integer, ByVal width As Integer)
        box.Location = New Point(x, y)
        box.Size = New Size(width, 24)
        box.BackColor = Color.FromArgb(40, 40, 40)
        box.ForeColor = Color.White
        box.BorderStyle = BorderStyle.FixedSingle
    End Sub

    Private Function FormatItem(ByVal p As RdpConnectionProfile) As String
        Dim title As String = p.TabLabel()
        If Not String.IsNullOrWhiteSpace(p.Username) Then
            Return title & "  —  " & p.Username
        End If
        Return title
    End Function

    Private Sub LayoutChildren()
        Dim cardW As Integer = Math.Min(420, Math.Max(280, ClientSize.Width - 40))
        Dim cardH As Integer = 320
        _card.Size = New Size(cardW, cardH)
        _card.Location = New Point(Math.Max(0, (ClientSize.Width - cardW) \ 2), Math.Max(8, (ClientSize.Height \ 2) - cardH + 20))

        txtHost.Width = cardW - 48
        txtUser.Width = cardW - 48
        txtPass.Width = cardW - 48
        fbConnect.Width = Math.Min(180, cardW - 48)

        Dim listTop As Integer = _card.Bottom + 16
        lblSaved.Location = New Point(8, listTop)
        _list.Location = New Point(8, listTop + 22)
        _list.Size = New Size(Math.Max(100, ClientSize.Width - 16), Math.Max(40, ClientSize.Height - listTop - 30))
    End Sub

    Private Sub OnPanelResize(ByVal sender As Object, ByVal e As EventArgs)
        LayoutChildren()
    End Sub

    Private Sub OnSelected(ByVal sender As Object, ByVal e As EventArgs)
        If _list.SelectedIndex < 0 OrElse _list.SelectedIndex >= _profiles.Count Then Return
        LoadProfileIntoForm(_profiles(_list.SelectedIndex))
        RaiseEvent StatusMessage(Me, "Loaded " & _profiles(_list.SelectedIndex).TabLabel())
    End Sub

    Private Sub OnListConnect(ByVal sender As Object, ByVal e As EventArgs)
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
End Class
