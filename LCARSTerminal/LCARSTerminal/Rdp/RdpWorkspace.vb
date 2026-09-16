' LCARSTerminal/Rdp/RdpWorkspace.vb
Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' RDP mode container: centered connect form XOR live session tabs.
''' </summary>
Public Class RdpWorkspace
    Inherits Panel

    Public Event StatusChanged As EventHandler(Of String)
    Public Event RequestCredentials As EventHandler(Of RdpCredentialRequest)
    Public Event ShowingListChanged As EventHandler(Of Boolean)

    Private ReadOnly _store As RdpProfileStore
    Private ReadOnly _credStore As New RdpCredentialStore()
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
        RaiseEvent StatusChanged(Me, "RDP — enter host and CONNECT")
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
        Dim host As RdpClientHost = CurrentHost()
        If host IsNot Nothing Then host.Disconnect()
    End Sub

    Public Sub CloseCurrentSession()
        If _sessionTabs.SelectedTab Is Nothing Then
            ShowList()
            Return
        End If
        Dim page As TabPage = _sessionTabs.SelectedTab
        Dim host As RdpClientHost = TryCast(page.Tag, RdpClientHost)
        If host IsNot Nothing Then
            host.Disconnect()
            host.Dispose()
        End If
        _sessionTabs.TabPages.Remove(page)
        If _sessionTabs.TabPages.Count = 0 Then
            ShowList()
            RaiseEvent StatusChanged(Me, "RDP — no sessions")
        End If
    End Sub

    Public Sub CloseAllSessions()
        While _sessionTabs.TabPages.Count > 0
            Dim page As TabPage = _sessionTabs.TabPages(0)
            Dim host As RdpClientHost = TryCast(page.Tag, RdpClientHost)
            If host IsNot Nothing Then
                host.Disconnect()
                host.Dispose()
            End If
            _sessionTabs.TabPages.Remove(page)
        End While
        ShowList()
    End Sub

    Private Function CurrentHost() As RdpClientHost
        If _sessionTabs.SelectedTab Is Nothing Then Return Nothing
        Return TryCast(_sessionTabs.SelectedTab.Tag, RdpClientHost)
    End Function

    Private Sub OnListStatus(ByVal sender As Object, ByVal message As String)
        RaiseEvent StatusChanged(Me, message)
    End Sub

    Private Sub OnConnectRequested(ByVal sender As Object, ByVal profile As RdpConnectionProfile)
        Dim password As String = _listPanel.EnteredPassword
        Dim remember As Boolean = profile.RememberPassword
        Dim user As String = profile.Username

        If String.IsNullOrEmpty(password) AndAlso remember Then
            _credStore.TryGetPassword(profile.Id, password)
        End If

        If String.IsNullOrEmpty(password) OrElse String.IsNullOrWhiteSpace(user) Then
            Dim req As New RdpCredentialRequest(profile)
            req.Password = password
            RaiseEvent RequestCredentials(Me, req)
            If Not req.Accepted Then
                RaiseEvent StatusChanged(Me, "RDP connect cancelled")
                Return
            End If
            user = req.UserName
            password = req.Password
            remember = req.RememberPassword
            profile.Username = user
            profile.RememberPassword = remember
        End If

        Try
            If remember Then
                _credStore.SavePassword(profile.Id, user, password)
            Else
                _credStore.DeletePassword(profile.Id)
            End If
        Catch ex As Exception
            RaiseEvent StatusChanged(Me, "Credential Manager: " & ex.Message)
        End Try

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

        AddHandler host.SessionConnected, Sub() RaiseEvent StatusChanged(Me, "RDP CONNECTED — " & profile.TabLabel())
        AddHandler host.SessionDisconnected, Sub(s As Object, reason As String) RaiseEvent StatusChanged(Me, "RDP " & reason)
        AddHandler host.LoginError, Sub(s As Object, message As String) RaiseEvent StatusChanged(Me, "RDP ERROR: " & message)

        host.ApplyProfile(profile, password)
        ' Defer Connect until the tab has a real client size (avoids white flash + early drop).
        BeginInvoke(New MethodInvoker(Sub()
                                          host.NotifyLayoutSize()
                                          host.Connect()
                                      End Sub))
        RaiseEvent StatusChanged(Me, "RDP CONNECTING — " & profile.TabLabel())
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
