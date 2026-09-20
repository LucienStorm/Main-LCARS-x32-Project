' LCARSexplorer/NetworkAccess.vb
Option Strict On
Option Explicit On

Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Windows.Forms

Public Module NetworkAccess
    Private Const RESOURCETYPE_DISK As Integer = 1
    Private Const CONNECT_TEMPORARY As Integer = 4

    <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Unicode)>
    Private Structure NETRESOURCE
        Public dwScope As Integer
        Public dwType As Integer
        Public dwDisplayType As Integer
        Public dwUsage As Integer
        Public lpLocalName As String
        Public lpRemoteName As String
        Public lpComment As String
        Public lpProvider As String
    End Structure

    <DllImport("mpr.dll", CharSet:=CharSet.Unicode)>
    Private Function WNetAddConnection2(ByRef netResource As NETRESOURCE, ByVal password As String, ByVal username As String, ByVal flags As Integer) As Integer
    End Function

    Public Function HostFromUncOrPath(ByVal path As String) As String
        If (String.IsNullOrEmpty(path) OrElse path.Trim().Length = 0) Then Return ""
        Dim p As String = path.Trim()
        If p.StartsWith("\\") Then
            Dim rest As String = p.TrimStart("\"c)
            Dim slash As Integer = rest.IndexOf("\"c)
            If slash > 0 Then Return rest.Substring(0, slash)
            Return rest
        End If
        Return ""
    End Function

    Public Function IsUnauthorizedOrNetworkError(ByVal ex As Exception) As Boolean
        If TypeOf ex Is UnauthorizedAccessException Then Return True
        If TypeOf ex Is IOException Then
            Dim msg As String = If(ex.Message, "").ToLowerInvariant()
            If msg.Contains("access") OrElse msg.Contains("logon") OrElse msg.Contains("credential") OrElse msg.Contains("denied") OrElse msg.Contains("network path") Then
                Return True
            End If
        End If
        Return False
    End Function

    Public Function TryConnectWithCredentials(ByVal uncRoot As String, ByVal username As String, ByVal password As String) As Boolean
        Dim nr As New NETRESOURCE()
        nr.dwType = RESOURCETYPE_DISK
        nr.lpRemoteName = uncRoot
        Dim result As Integer = WNetAddConnection2(nr, password, username, CONNECT_TEMPORARY)
        Return result = 0 OrElse result = 1219 ' already connected
    End Function

    ''' <summary>
    ''' Enumerate directory; on auth failure prompt once (optional remember) and retry.
    ''' </summary>
    Public Function TryGetFileSystemInfos(ByVal path As String, ByVal owner As IWin32Window, ByRef infos() As FileSystemInfo) As Boolean
        infos = Nothing
        Try
            infos = New DirectoryInfo(path).GetFileSystemInfos()
            Return True
        Catch ex As Exception
            If Not IsUnauthorizedOrNetworkError(ex) Then
                MsgBox(String.Format("Unable to access path: {0}" & vbCrLf & ex.Message, path), MsgBoxStyle.OkOnly Or MsgBoxStyle.Exclamation, "Access Denied")
                Return False
            End If
        End Try

        Dim host As String = HostFromUncOrPath(path)
        Dim store As New NetworkCredentialStore()
        Dim user As String = ""
        Dim pass As String = ""
        Dim connected As Boolean = False

        If store.TryGet(host, user, pass) Then
            Dim uncRoot As String = "\\" & host
            If TryConnectWithCredentials(uncRoot, user, pass) Then
                Try
                    infos = New DirectoryInfo(path).GetFileSystemInfos()
                    Return True
                Catch
                End Try
            End If
        End If

        Using dlg As New frmNetworkCredentials(host)
            dlg.UserName = If(user, "")
            If dlg.ShowDialog(owner) <> DialogResult.OK Then
                Return False
            End If
            user = dlg.UserName
            pass = dlg.Password
            Dim uncRoot As String = "\\" & host
            If (String.IsNullOrEmpty(host) OrElse host.Trim().Length = 0) Then uncRoot = path
            connected = TryConnectWithCredentials(uncRoot, user, pass)
            If Not connected Then
                MsgBox("Could not connect with the provided credentials.", MsgBoxStyle.OkOnly Or MsgBoxStyle.Exclamation, "Access Denied")
                Return False
            End If
            If dlg.RememberPassword Then
                Try
                    store.Save(host, user, pass)
                Catch
                End Try
            End If
        End Using

        Try
            infos = New DirectoryInfo(path).GetFileSystemInfos()
            Return True
        Catch ex As Exception
            MsgBox(String.Format("Unable to access path: {0}" & vbCrLf & ex.Message, path), MsgBoxStyle.OkOnly Or MsgBoxStyle.Exclamation, "Access Denied")
            Return False
        End Try
    End Function
End Module
