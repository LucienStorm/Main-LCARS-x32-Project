' LCARSTerminal/Rdp/RdpWorkspace.vb
Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Remote mode container: centered connect form XOR live session tabs (RDP + VNC).
''' </summary>
Public Class RdpWorkspace
    Inherits Panel

    Public Event StatusChanged As EventHandler(Of String)
    Public Event RequestCredentials As EventHandler(Of RdpCredentialRequest)
    Public Event ShowingListChanged As EventHandler(Of Boolean)

    Private ReadOnly _store As RdpProfileStore
    Private ReadOnly _credStore As New RdpCredentialStore()
    Private ReadOnly _historyStore As New RemoteHistoryStore()
    Private ReadOnly _listPanel As RdpConnectionListPanel
    Private ReadOnly _sessionTabs As New TabControl()

    Public Sub New(ByVal store As RdpProfileStore)
        _store = store
        BackColor = Color.Black
        ' Do not Dock=Fill — frmTerminal sizes this to the content hole only.

        _listPanel = New RdpConnectionListPanel(_store)
        _listPanel.Dock = DockStyle.Fill
        AddHandler _listPanel.ConnectRequested, AddressOf OnConnectRequested
        AddHandler _listPanel.StatusMessage, AddressOf OnListStatus

        _sessionTabs.Font = New Font("Arial Narrow", 11.0F, FontStyle.Bold)
        _sessionTabs.BackColor = Color.Black
        _sessionTabs.Dock = DockStyle.Fill
        _sessionTabs.Visible = False

        Controls.Add(_listPanel)
        Controls.Add(_sessionTabs)
        ShowList()
    End Sub

    Public ReadOnly Property ListPanel As RdpConnectionListPanel
        Get
            Return _listPanel
        End Get
    End Property

    Public ReadOnly Property ShowingList As Boolean
        Get
            Return _listPanel.Visible
        End Get
    End Property

    Public ReadOnly Property SessionCount As Integer
        Get
            Return _sessionTabs.TabPages.Count
        End Get
    End Property

    Public Sub ShowList()
        _sessionTabs.Visible = False
        _listPanel.Visible = True
        _listPanel.BringToFront()
        _listPanel.Reload()
        RaiseEvent ShowingListChanged(Me, True)
    End Sub

    Public Sub ShowSessions()
        _listPanel.Visible = False
        _sessionTabs.Visible = True
        _sessionTabs.BringToFront()
        RaiseEvent ShowingListChanged(Me, False)
    End Sub

    Public Sub FocusNewConnection()
        ShowList()
        RaiseEvent StatusChanged(Me, "REMOTE — enter host and CONNECT")
    End Sub

    Public Sub RailConnect()
        ShowList()
        _listPanel.DoConnect()
    End Sub

    Public Sub RailNew()
        ShowList()
        _listPanel.DoNew()
    End Sub

    Public Sub RailDelete()
        ShowList()
        _listPanel.DoDelete()
    End Sub

    Public Sub RailOpen()
        ShowList()
        _listPanel.DoOpenRdp()
    End Sub

    Public Sub RailSave()
        ShowList()
        _listPanel.DoSaveRdp()
    End Sub

    Public Sub DisconnectCurrent()
        Dim rdp As RdpClientHost = CurrentRdpHost()
        If rdp IsNot Nothing Then
            rdp.Disconnect()
            Return
        End If
        Dim vnc As VncClientHost = CurrentVncHost()
        If vnc IsNot Nothing Then vnc.Disconnect()
    End Sub

    ''' <summary>Forward OSK character input into the active remote session or connect form.</summary>
    Public Sub InjectChar(ByVal ch As Char)
        Dim rdp As RdpClientHost = CurrentRdpHost()
        If rdp IsNot Nothing Then
            rdp.InjectChar(ch)
            Return
        End If
        Dim vnc As VncClientHost = CurrentVncHost()
        If vnc IsNot Nothing Then
            vnc.InjectChar(ch)
            Return
        End If
        InjectIntoFocusedControl(ch, Keys.None)
    End Sub

    ''' <summary>Forward OSK virtual-key input into the active remote session or connect form.</summary>
    Public Sub InjectVirtualKey(ByVal key As Keys)
        Dim rdp As RdpClientHost = CurrentRdpHost()
        If rdp IsNot Nothing Then
            rdp.InjectVirtualKey(key)
            Return
        End If
        Dim vnc As VncClientHost = CurrentVncHost()
        If vnc IsNot Nothing Then
            vnc.InjectVirtualKey(key)
            Return
        End If
        InjectIntoFocusedControl(ChrW(0), key)
    End Sub

    Private Sub InjectIntoFocusedControl(ByVal ch As Char, ByVal key As Keys)
        Try
            Dim form As Form = FindForm()
            Dim target As Control = Nothing
            If form IsNot Nothing Then target = form.ActiveControl
            If target Is Nothing Then target = _listPanel
            If target Is Nothing Then Return
            target.Focus()
            If key <> Keys.None Then
                RdpClientHost.SendVirtualKeyStroke(CInt(key) And &HFF)
            ElseIf ch <> ChrW(0) Then
                RdpClientHost.SendUnicodeChar(ch)
            End If
        Catch
        End Try
    End Sub

    Public Sub CloseCurrentSession()
        If _sessionTabs.SelectedTab Is Nothing Then
            ShowList()
            Return
        End If
        DisposeSessionPage(_sessionTabs.SelectedTab)
        If _sessionTabs.TabPages.Count = 0 Then
            ShowList()
            RaiseEvent StatusChanged(Me, "REMOTE — no sessions")
        End If
    End Sub

    Public Sub CloseAllSessions()
        While _sessionTabs.TabPages.Count > 0
            DisposeSessionPage(_sessionTabs.TabPages(0))
        End While
        ShowList()
    End Sub

    Private Sub DisposeSessionPage(ByVal page As TabPage)
        If page Is Nothing Then Return
        Dim rdp As RdpClientHost = TryCast(page.Tag, RdpClientHost)
        If rdp IsNot Nothing Then
            rdp.Disconnect()
            rdp.Dispose()
        End If
        Dim vnc As VncClientHost = TryCast(page.Tag, VncClientHost)
        If vnc IsNot Nothing Then
            vnc.Disconnect()
            vnc.Dispose()
        End If
        _sessionTabs.TabPages.Remove(page)
    End Sub

    Private Function CurrentRdpHost() As RdpClientHost
        If _sessionTabs.SelectedTab Is Nothing Then Return Nothing
        Return TryCast(_sessionTabs.SelectedTab.Tag, RdpClientHost)
    End Function

    Private Function CurrentVncHost() As VncClientHost
        If _sessionTabs.SelectedTab Is Nothing Then Return Nothing
        Return TryCast(_sessionTabs.SelectedTab.Tag, VncClientHost)
    End Function

    Private Sub OnListStatus(ByVal sender As Object, ByVal message As String)
        RaiseEvent StatusChanged(Me, message)
    End Sub

    Private Sub OnConnectRequested(ByVal sender As Object, ByVal profile As RdpConnectionProfile)
        Dim password As String = _listPanel.EnteredPassword
        Dim remember As Boolean = profile.RememberPassword
        Dim user As String = profile.Username
        Dim isVnc As Boolean = (profile.ProtocolKind() = RemoteProtocol.Vnc)

        If String.IsNullOrEmpty(password) AndAlso remember Then
            _credStore.TryGetPassword(profile.Id, password)
        End If

        Dim needsPrompt As Boolean
        If isVnc Then
            ' VNC often has password only; username is optional.
            needsPrompt = String.IsNullOrEmpty(password)
        Else
            needsPrompt = String.IsNullOrEmpty(password) OrElse String.IsNullOrWhiteSpace(user)
        End If

        If needsPrompt Then
            Dim req As New RdpCredentialRequest(profile)
            req.Password = password
            RaiseEvent RequestCredentials(Me, req)
            If Not req.Accepted Then
                RaiseEvent StatusChanged(Me, "REMOTE connect cancelled")
                Return
            End If
            user = req.UserName
            password = req.Password
            remember = req.RememberPassword
            profile.Username = user
            profile.RememberPassword = remember
        End If

        Try
            If remember AndAlso Not String.IsNullOrEmpty(password) Then
                _credStore.SavePassword(profile.Id, If(user, ""), password)
            ElseIf Not remember Then
                _credStore.DeletePassword(profile.Id)
            End If
        Catch ex As Exception
            RaiseEvent StatusChanged(Me, "Credential Manager: " & ex.Message)
        End Try

        If isVnc Then
            StartVncSession(profile, password)
        Else
            StartRdpSession(profile, password)
        End If
    End Sub

    Private Sub StartRdpSession(ByVal profile As RdpConnectionProfile, ByVal password As String)
        Dim err As String = Nothing
        Dim host As New RdpClientHost()
        If Not host.TryCreateClient(err) Then
            RaiseEvent StatusChanged(Me, "RDP ActiveX unavailable: " & err)
            host.Dispose()
            Return
        End If

        Dim page As New TabPage(profile.TabLabel())
        page.BackColor = Color.Black
        page.Controls.Add(host)
        page.Tag = host
        _sessionTabs.TabPages.Add(page)
        _sessionTabs.SelectedTab = page
        ShowSessions()

        AddHandler host.SessionConnected, Sub()
                                              RecordHistory(profile)
                                              RaiseEvent StatusChanged(Me, "RDP CONNECTED — " & profile.TabLabel())
                                          End Sub
        AddHandler host.SessionDisconnected, Sub(s As Object, reason As String) RaiseEvent StatusChanged(Me, "RDP " & reason)
        AddHandler host.LoginError, Sub(s As Object, message As String) RaiseEvent StatusChanged(Me, "RDP ERROR: " & message)

        host.ApplyProfile(profile, password)
        BeginInvoke(New MethodInvoker(Sub()
                                          host.NotifyLayoutSize()
                                          host.Connect()
                                      End Sub))
        RaiseEvent StatusChanged(Me, "RDP CONNECTING — " & profile.TabLabel())
    End Sub

    Private Sub StartVncSession(ByVal profile As RdpConnectionProfile, ByVal password As String)
        Dim host As New VncClientHost()
        Dim page As New TabPage(profile.TabLabel())
        page.BackColor = Color.Black
        host.Dock = DockStyle.Fill
        page.Controls.Add(host)
        page.Tag = host
        _sessionTabs.TabPages.Add(page)
        _sessionTabs.SelectedTab = page
        ShowSessions()

        AddHandler host.SessionConnected, Sub()
                                              RecordHistory(profile)
                                              RaiseEvent StatusChanged(Me, "VNC CONNECTED — " & profile.TabLabel())
                                          End Sub
        AddHandler host.SessionDisconnected, Sub(s As Object, reason As String) RaiseEvent StatusChanged(Me, "VNC " & reason)
        AddHandler host.LoginError, Sub(s As Object, message As String) RaiseEvent StatusChanged(Me, "VNC ERROR: " & message)

        Dim port As Integer = profile.Port
        If port <= 0 Then port = 5900
        BeginInvoke(New MethodInvoker(Sub() host.Connect(profile.Hostname, port, password)))
        RaiseEvent StatusChanged(Me, "VNC CONNECTING — " & profile.TabLabel())
    End Sub

    Private Sub RecordHistory(ByVal profile As RdpConnectionProfile)
        Try
            _historyStore.Record(profile)
            If _listPanel IsNot Nothing Then _listPanel.ReloadHistory()
        Catch
        End Try
    End Sub
End Class

''' <summary>
''' Credential prompt result carrier for RdpWorkspace.
''' </summary>
Public Class RdpCredentialRequest
    Public ReadOnly Profile As RdpConnectionProfile
    Public Accepted As Boolean
    Public UserName As String
    Public Password As String
    Public RememberPassword As Boolean

    Public Sub New(ByVal profile As RdpConnectionProfile)
        Me.Profile = profile
        Me.UserName = If(profile.Username, "")
        Me.RememberPassword = profile.RememberPassword
    End Sub
End Class
