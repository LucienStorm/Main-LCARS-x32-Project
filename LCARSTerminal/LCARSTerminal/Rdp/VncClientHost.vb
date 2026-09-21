' LCARSTerminal/Rdp/VncClientHost.vb
Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Threading
Imports System.Windows.Forms
Imports RemoteViewing.Vnc
Imports RemoteViewing.Windows.Forms

''' <summary>
''' Embedded VNC (RFB) session using RemoteViewing inside a Terminal tab.
''' </summary>
Public Class VncClientHost
    Inherits Panel

    Public Event SessionConnected As EventHandler
    Public Event SessionDisconnected As EventHandler(Of String)
    Public Event LoginError As EventHandler(Of String)

    Private ReadOnly _vnc As VncControl
    Private _connecting As Boolean

    Public Sub New()
        BackColor = Color.Black
        _vnc = New VncControl()
        _vnc.Dock = DockStyle.Fill
        _vnc.AllowInput = True
        _vnc.AllowRemoteCursor = True
        _vnc.BackColor = Color.Black
        Controls.Add(_vnc)
        AddHandler _vnc.Connected, AddressOf HandleConnected
        AddHandler _vnc.ConnectionFailed, AddressOf HandleFailed
        AddHandler _vnc.Closed, AddressOf HandleClosed
    End Sub

    Public Sub Connect(ByVal hostname As String, ByVal port As Integer, ByVal password As String)
        If String.IsNullOrWhiteSpace(hostname) Then
            RaiseEvent LoginError(Me, "VNC hostname is empty.")
            Return
        End If
        If port <= 0 Then port = 5900
        If _connecting Then Return
        _connecting = True

        Dim host As String = hostname.Trim()
        Dim pwd As String = If(password, "")
        Dim opts As New VncClientConnectOptions()
        opts.ShareDesktop = True
        If pwd.Length > 0 Then
            opts.Password = pwd.ToCharArray()
        End If

        ThreadPool.QueueUserWorkItem(
            Sub(state As Object)
                Try
                    _vnc.Client.Connect(host, port, opts)
                Catch ex As Exception
                    _connecting = False
                    BeginInvokeSafe(Sub() RaiseEvent LoginError(Me, ex.Message))
                End Try
            End Sub)
    End Sub

    Public Sub Disconnect()
        Try
            If _vnc IsNot Nothing AndAlso _vnc.Client IsNot Nothing AndAlso _vnc.Client.IsConnected Then
                _vnc.Client.Close()
            End If
        Catch
        End Try
        _connecting = False
    End Sub

    Public Sub InjectChar(ByVal ch As Char)
        Try
            _vnc.Focus()
            RdpClientHost.SendUnicodeChar(ch)
        Catch
        End Try
    End Sub

    Public Sub InjectVirtualKey(ByVal key As Keys)
        Try
            _vnc.Focus()
            RdpClientHost.SendVirtualKeyStroke(CInt(key) And &HFF)
        Catch
        End Try
    End Sub

    Private Sub HandleConnected(ByVal sender As Object, ByVal e As EventArgs)
        _connecting = False
        BeginInvokeSafe(Sub() RaiseEvent SessionConnected(Me, EventArgs.Empty))
    End Sub

    Private Sub HandleFailed(ByVal sender As Object, ByVal e As EventArgs)
        _connecting = False
        BeginInvokeSafe(Sub() RaiseEvent LoginError(Me, "VNC connection failed."))
    End Sub

    Private Sub HandleClosed(ByVal sender As Object, ByVal e As EventArgs)
        _connecting = False
        BeginInvokeSafe(Sub() RaiseEvent SessionDisconnected(Me, "Disconnected"))
    End Sub

    Private Sub BeginInvokeSafe(ByVal action As MethodInvoker)
        If IsDisposed Then Return
        Try
            If InvokeRequired Then
                BeginInvoke(action)
            Else
                action()
            End If
        Catch
        End Try
    End Sub

    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        If disposing Then
            Try
                Disconnect()
            Catch
            End Try
            RemoveHandler _vnc.Connected, AddressOf HandleConnected
            RemoveHandler _vnc.ConnectionFailed, AddressOf HandleFailed
            RemoveHandler _vnc.Closed, AddressOf HandleClosed
            _vnc.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub
End Class
