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

    ''' <summary>\\server\share\folder → \\server\share (WNet needs the share, not just the host).</summary>
    Public Function ShareRootFromUnc(ByVal path As String) As String
        If String.IsNullOrEmpty(path) Then Return ""
        Dim p As String = path.Trim()
        If Not p.StartsWith("\\") Then Return ""
        Dim rest As String = p.TrimStart("\"c)
        Dim parts As String() = rest.Split({"\"c}, StringSplitOptions.RemoveEmptyEntries)
        If parts.Length >= 2 Then Return "\\" & parts(0) & "\" & parts(1)
        If parts.Length = 1 Then Return "\\" & parts(0)
        Return p
    End Function

    ''' <summary>Normalize username: convert / to \, trim, drop leading .\ </summary>
    Public Function NormalizeUserName(ByVal username As String) As String
        If String.IsNullOrEmpty(username) Then Return ""
        Dim u As String = username.Trim().Replace("/"c, "\"c)
        If u.StartsWith(".\") Then u = u.Substring(2)
        Return u.TrimEnd("\"c)
    End Function

    Public Function DescribeConnectError(ByVal code As Integer) As String
        Select Case code
            Case 0 : Return "OK"
            Case 5 : Return "Access denied (5) — wrong user/password, or share permissions."
            Case 53 : Return "Network path not found (53) — check host name / IP."
            Case 67 : Return "Network name not found (67) — use \\host\sharename (not just \\host)."
            Case 86 : Return "Invalid password (86)."
            Case 1219 : Return "Already connected with different credentials (1219) — disconnect the share in Windows first, then retry."
            Case 1326 : Return "Logon failure (1326) — try HOST\username or DOMAIN\username."
            Case Else : Return "WNet error " & code.ToString()
        End Select
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

    Public Function TryConnectWithCredentials(ByVal uncTarget As String, ByVal username As String, ByVal password As String, ByRef errCode As Integer) As Boolean
        errCode = -1
        Dim user As String = NormalizeUserName(username)
        Dim nr As New NETRESOURCE()
        nr.dwType = RESOURCETYPE_DISK
        nr.lpRemoteName = uncTarget
        errCode = WNetAddConnection2(nr, password, user, CONNECT_TEMPORARY)
        Return errCode = 0 OrElse errCode = 1219
    End Function

    Public Function TryConnectWithCredentials(ByVal uncTarget As String, ByVal username As String, ByVal password As String) As Boolean
        Dim ignored As Integer = 0
        Return TryConnectWithCredentials(uncTarget, username, password, ignored)
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
        Dim shareRoot As String = ShareRootFromUnc(path)
        If String.IsNullOrEmpty(shareRoot) Then shareRoot = If(String.IsNullOrEmpty(host), path, "\\" & host)

        Dim store As New NetworkCredentialStore()
        Dim user As String = ""
        Dim pass As String = ""
        Dim errCode As Integer = 0

        If store.TryGet(host, user, pass) Then
            If TryConnectWithCredentials(shareRoot, user, pass, errCode) Then
                Try
                    infos = New DirectoryInfo(path).GetFileSystemInfos()
                    Return True
                Catch
                End Try
            End If
        End If

        Using dlg As New frmNetworkCredentials(host)
            If Not String.IsNullOrEmpty(user) Then dlg.UserName = user
            If dlg.ShowDialog(owner) <> DialogResult.OK Then
                Return False
            End If
            user = NormalizeUserName(dlg.UserName)
            pass = dlg.Password
            Dim connected As Boolean = TryConnectWithCredentials(shareRoot, user, pass, errCode)
            If Not connected Then
                MsgBox("Could not connect to " & shareRoot & vbCrLf & DescribeConnectError(errCode) & vbCrLf & vbCrLf &
                       "Tried user: " & user & vbCrLf &
                       "Tip: Samba often wants HOST\username (pre-filled). Plain username also works on some shares.",
                       MsgBoxStyle.OkOnly Or MsgBoxStyle.Exclamation, "Access Denied")
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
