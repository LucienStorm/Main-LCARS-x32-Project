' LCARSTerminal/Rdp/RdpClientHost.vb
Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Windows.Forms
Imports AxMSTSCLib
Imports MSTSCLib

''' <summary>
''' Hosts an embedded MsRdpClient ActiveX session inside a panel.
''' </summary>
Public Class RdpClientHost
    Inherits Panel

    Public Event SessionConnected As EventHandler
    Public Event SessionDisconnected As EventHandler(Of String)
    Public Event LoginError As EventHandler(Of String)

    Private _client As AxMsRdpClient9NotSafeForScripting
    Private _profile As RdpConnectionProfile
    Private _pendingConnect As Boolean

    Public Sub New()
        BackColor = Color.Black
        Dock = DockStyle.Fill
    End Sub

    Public Function TryCreateClient(ByRef errorMessage As String) As Boolean
        errorMessage = Nothing
        If _client IsNot Nothing Then Return True
        Try
            Dim c As New AxMsRdpClient9NotSafeForScripting()
            CType(c, System.ComponentModel.ISupportInitialize).BeginInit()
            c.Dock = DockStyle.Fill
            c.Enabled = True
            Controls.Add(c)
            CType(c, System.ComponentModel.ISupportInitialize).EndInit()
            AddHandler c.OnConnected, AddressOf HandleConnected
            AddHandler c.OnDisconnected, AddressOf HandleDisconnected
            AddHandler c.OnLoginComplete, AddressOf HandleLoginComplete
            _client = c
            Return True
        Catch ex As Exception
            errorMessage = ex.Message
            _client = Nothing
            Return False
        End Try
    End Function

    Public Sub ApplyProfile(ByVal profile As RdpConnectionProfile, Optional ByVal password As String = Nothing)
        If profile Is Nothing Then Throw New ArgumentNullException("profile")
        _profile = profile
        Dim err As String = Nothing
        If Not TryCreateClient(err) Then
            RaiseEvent LoginError(Me, err)
            Return
        End If

        Dim user As String = If(profile.Username, "").Trim()
        Dim domain As String = If(profile.Domain, "").Trim()
        SplitUserAndDomain(user, domain)

        Dim host As String = profile.Hostname.Trim()
        ' Port goes on AdvancedSettings2.RDPPort — do not append :port to Server.
        _client.Server = host
        _client.UserName = user
        _client.Domain = domain

        NotifyLayoutSize()
        ApplyAdvancedSettings(profile, password)
    End Sub

    ''' <summary>
    ''' Starts the session after the host has a real client size (BeginInvoke from caller is fine).
    ''' </summary>
    Public Sub Connect()
        If _client Is Nothing Then
            RaiseEvent LoginError(Me, "RDP ActiveX control was not created.")
            Return
        End If
        If ClientSize.Width < 32 OrElse ClientSize.Height < 32 Then
            _pendingConnect = True
            Return
        End If
        _pendingConnect = False
        NotifyLayoutSize()
        Try
            _client.Connect()
        Catch ex As Exception
            RaiseEvent LoginError(Me, ex.Message)
        End Try
    End Sub

    Public Sub Disconnect()
        _pendingConnect = False
        If _client Is Nothing Then Return
        Try
            If _client.Connected <> 0 Then
                _client.Disconnect()
            End If
        Catch
        End Try
    End Sub

    Public Sub NotifyLayoutSize()
        If _client Is Nothing Then Return
        Dim w As Integer = Math.Max(800, ClientSize.Width)
        Dim h As Integer = Math.Max(600, ClientSize.Height)
        Try
            If _client.Connected = 0 Then
                _client.DesktopWidth = w
                _client.DesktopHeight = h
            End If
        Catch
        End Try
    End Sub

    Protected Overrides Sub OnResize(ByVal e As EventArgs)
        MyBase.OnResize(e)
        NotifyLayoutSize()
        If _pendingConnect AndAlso ClientSize.Width >= 32 AndAlso ClientSize.Height >= 32 Then
            Connect()
        End If
    End Sub

    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        If disposing Then
            _pendingConnect = False
            Try
                Disconnect()
            Catch
            End Try
            If _client IsNot Nothing Then
                RemoveHandler _client.OnConnected, AddressOf HandleConnected
                RemoveHandler _client.OnDisconnected, AddressOf HandleDisconnected
                RemoveHandler _client.OnLoginComplete, AddressOf HandleLoginComplete
                Controls.Remove(_client)
                _client.Dispose()
                _client = Nothing
            End If
        End If
        MyBase.Dispose(disposing)
    End Sub

    Private Sub ApplyAdvancedSettings(ByVal profile As RdpConnectionProfile, ByVal password As String)
        Try
            Dim adv As IMsRdpClientAdvancedSettings = TryCast(_client.AdvancedSettings2, IMsRdpClientAdvancedSettings)
            If adv Is Nothing Then adv = TryCast(_client.AdvancedSettings, IMsRdpClientAdvancedSettings)
            If adv IsNot Nothing Then
                adv.SmartSizing = profile.SmartSizing
                adv.RedirectDrives = profile.RedirectDrives
                adv.RedirectPrinters = profile.RedirectPrinters
                If profile.Port > 0 AndAlso profile.Port <> 3389 Then
                    adv.RDPPort = profile.Port
                End If
            End If
        Catch
        End Try

        ' CredSSP/NLA is required for current Windows hosts; without it the ActiveX
        ' often shows a white surface then OnDisconnected almost immediately.
        Try
            Dim adv7 As IMsRdpClientAdvancedSettings7 = TryCast(_client.AdvancedSettings7, IMsRdpClientAdvancedSettings7)
            If adv7 IsNot Nothing Then
                adv7.EnableCredSspSupport = True
            End If
        Catch
        End Try

        Try
            Dim adv5 As IMsRdpClientAdvancedSettings5 = TryCast(_client.AdvancedSettings5, IMsRdpClientAdvancedSettings5)
            If adv5 IsNot Nothing Then
                adv5.RedirectClipboard = profile.RedirectClipboard
                ' 0 = never authenticate, 1 = auth if available, 2 = require
                adv5.AuthenticationLevel = 2
            End If
        Catch
        End Try

        Try
            Dim adv6 As IMsRdpClientAdvancedSettings6 = TryCast(_client.AdvancedSettings6, IMsRdpClientAdvancedSettings6)
            If adv6 IsNot Nothing Then
                If profile.RedirectAudio Then
                    adv6.AudioRedirectionMode = 0
                Else
                    adv6.AudioRedirectionMode = 1
                End If
            End If
        Catch
        End Try

        If password IsNot Nothing Then
            Try
                Dim nonScript As IMsTscNonScriptable = TryCast(_client.GetOcx(), IMsTscNonScriptable)
                If nonScript IsNot Nothing Then
                    nonScript.ClearTextPassword = password
                End If
            Catch
            End Try
            Try
                Dim adv As IMsRdpClientAdvancedSettings = TryCast(_client.AdvancedSettings2, IMsRdpClientAdvancedSettings)
                If adv IsNot Nothing Then
                    adv.ClearTextPassword = password
                End If
            Catch
            End Try
        End If
    End Sub

    Private Shared Sub SplitUserAndDomain(ByRef user As String, ByRef domain As String)
        If String.IsNullOrEmpty(user) Then Return
        Dim slash As Integer = user.IndexOf("\"c)
        If slash > 0 AndAlso slash < user.Length - 1 Then
            If String.IsNullOrEmpty(domain) Then domain = user.Substring(0, slash)
            user = user.Substring(slash + 1)
            Return
        End If
        ' user@domain UPN — leave as-is; CredSSP accepts it with empty Domain.
    End Sub

    Private Sub HandleConnected(ByVal sender As Object, ByVal e As EventArgs)
        RaiseEvent SessionConnected(Me, EventArgs.Empty)
    End Sub

    Private Sub HandleLoginComplete(ByVal sender As Object, ByVal e As EventArgs)
        RaiseEvent SessionConnected(Me, EventArgs.Empty)
    End Sub

    Private Sub HandleDisconnected(ByVal sender As Object, ByVal e As AxMSTSCLib.IMsTscAxEvents_OnDisconnectedEvent)
        Dim reason As String = FormatDisconnectReason(e.discReason)
        RaiseEvent SessionDisconnected(Me, reason)
    End Sub

    Private Function FormatDisconnectReason(ByVal discReason As Integer) As String
        Dim detail As String = Nothing
        Try
            If _client IsNot Nothing Then
                detail = _client.GetErrorDescription(CUInt(discReason), CUInt(_client.ExtendedDisconnectReason))
            End If
        Catch
        End Try
        If Not String.IsNullOrWhiteSpace(detail) Then
            Return "Disconnected (" & discReason.ToString() & "): " & detail.Trim()
        End If
        Return "Disconnected (" & discReason.ToString() & ")"
    End Function
End Class
