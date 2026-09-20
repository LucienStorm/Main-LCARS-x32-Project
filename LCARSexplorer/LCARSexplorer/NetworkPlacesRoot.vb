' LCARSexplorer/NetworkPlacesRoot.vb
Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.IO
Imports System.Net
Imports System.Net.NetworkInformation
Imports System.Net.Sockets
Imports System.Threading

Public Class SmbDiscoveredHost
    Public Property IpAddress As String = ""
    Public Property Hostname As String = ""

    Public Function DisplayText() As String
        If (String.IsNullOrEmpty(Hostname) OrElse Hostname.Trim().Length = 0) Then Return IpAddress
        If String.Equals(Hostname, IpAddress, StringComparison.OrdinalIgnoreCase) Then Return IpAddress
        Return Hostname & "  (" & IpAddress & ")"
    End Function
End Class

Public Module NetworkPlacesRoot
    Public Const NetworkRootToken As String = "NETWORK:"

    Public Function IsNetworkRoot(ByVal path As String) As Boolean
        If (String.IsNullOrEmpty(path) OrElse path.Trim().Length = 0) Then Return False
        Return String.Equals(path.Trim().Trim(""""c), NetworkRootToken, StringComparison.OrdinalIgnoreCase)
    End Function

    Public Function GetMappedNetworkDrives() As List(Of DriveInfo)
        Dim result As New List(Of DriveInfo)()
        For Each d As DriveInfo In DriveInfo.GetDrives()
            If d.DriveType = DriveType.Network Then
                result.Add(d)
            End If
        Next
        Return result
    End Function

    Public Function ScanSmbHosts(Optional ByVal timeoutMs As Integer = 350) As List(Of SmbDiscoveredHost)
        Dim results As New List(Of SmbDiscoveredHost)()
        Dim local As UnicastIPAddressInformation = FindPrimaryUnicast()
        If local Is Nothing OrElse local.Address Is Nothing OrElse local.IPv4Mask Is Nothing Then
            Return results
        End If

        Dim baseBytes() As Byte = local.Address.GetAddressBytes()
        Dim maskBytes() As Byte = local.IPv4Mask.GetAddressBytes()
        If baseBytes.Length <> 4 OrElse maskBytes.Length <> 4 Then Return results

        Dim network As UInteger = ToUInt(baseBytes) And ToUInt(maskBytes)
        Dim broadcast As UInteger = network Or (Not ToUInt(maskBytes))
        Dim hostCount As Long = CLng(broadcast) - CLng(network) - 1
        If hostCount <= 0 OrElse hostCount > 1024 Then
            hostCount = 254
            network = ToUInt(New Byte() {baseBytes(0), baseBytes(1), baseBytes(2), 0})
        End If

        Dim bag As New Dictionary(Of String, SmbDiscoveredHost)()
        Dim bagLock As New Object()
        Dim pending As Integer = 0
        Dim done As New ManualResetEvent(False)

        For offset As UInteger = 1UI To CUInt(Math.Min(hostCount, 254))
            Dim ipNum As UInteger = network + offset
            If ipNum = ToUInt(baseBytes) Then Continue For
            Dim ip As String = FromUInt(ipNum)
            Interlocked.Increment(pending)
            ThreadPool.QueueUserWorkItem(
                Sub(state As Object)
                    Try
                        Dim smb445 As Boolean = ProbePort(ip, 445, timeoutMs)
                        Dim smb139 As Boolean = If(smb445, False, ProbePort(ip, 139, timeoutMs))
                        If smb445 OrElse smb139 Then
                            Dim host As New SmbDiscoveredHost()
                            host.IpAddress = ip
                            host.Hostname = TryResolveName(ip)
                            SyncLock bagLock
                                bag(ip) = host
                            End SyncLock
                        End If
                    Finally
                        If Interlocked.Decrement(pending) = 0 Then done.Set()
                    End Try
                End Sub)
        Next

        If pending = 0 Then Return results
        done.WaitOne(Math.Max(5000, timeoutMs * 4))
        Dim sorted As New List(Of SmbDiscoveredHost)()
        SyncLock bagLock
            sorted.AddRange(bag.Values)
        End SyncLock
        sorted.Sort(Function(a, b) String.Compare(a.DisplayText(), b.DisplayText(), StringComparison.OrdinalIgnoreCase))
        Return sorted
    End Function

    Private Function ProbePort(ByVal ip As String, ByVal port As Integer, ByVal timeoutMs As Integer) As Boolean
        Try
            Using client As New TcpClient()
                Dim ar As IAsyncResult = client.BeginConnect(ip, port, Nothing, Nothing)
                If Not ar.AsyncWaitHandle.WaitOne(timeoutMs) Then
                    Try
                        client.Close()
                    Catch
                    End Try
                    Return False
                End If
                client.EndConnect(ar)
                Return client.Connected
            End Using
        Catch
            Return False
        End Try
    End Function

    Private Function TryResolveName(ByVal ip As String) As String
        Try
            Dim entry As IPHostEntry = Dns.GetHostEntry(ip)
            If entry IsNot Nothing AndAlso Not (String.IsNullOrEmpty(entry.HostName) OrElse entry.HostName.Trim().Length = 0) Then
                Dim name As String = entry.HostName
                Dim dot As Integer = name.IndexOf("."c)
                If dot > 0 Then name = name.Substring(0, dot)
                Return name
            End If
        Catch
        End Try
        Return ""
    End Function

    Private Function FindPrimaryUnicast() As UnicastIPAddressInformation
        For Each ni As NetworkInterface In NetworkInterface.GetAllNetworkInterfaces()
            If ni.OperationalStatus <> OperationalStatus.Up Then Continue For
            If ni.NetworkInterfaceType = NetworkInterfaceType.Loopback Then Continue For
            Dim props As IPInterfaceProperties = ni.GetIPProperties()
            If props Is Nothing OrElse props.UnicastAddresses Is Nothing Then Continue For
            For Each addr As UnicastIPAddressInformation In props.UnicastAddresses
                If addr.Address Is Nothing Then Continue For
                If addr.Address.AddressFamily <> AddressFamily.InterNetwork Then Continue For
                Dim ip As String = addr.Address.ToString()
                If ip.StartsWith("169.254.", StringComparison.Ordinal) Then Continue For
                Return addr
            Next
        Next
        Return Nothing
    End Function

    Private Function ToUInt(ByVal bytes() As Byte) As UInteger
        Return (CUInt(bytes(0)) << 24) Or (CUInt(bytes(1)) << 16) Or (CUInt(bytes(2)) << 8) Or CUInt(bytes(3))
    End Function

    Private Function FromUInt(ByVal value As UInteger) As String
        Return String.Format("{0}.{1}.{2}.{3}",
                             (value >> 24) And &HFFUI,
                             (value >> 16) And &HFFUI,
                             (value >> 8) And &HFFUI,
                             value And &HFFUI)
    End Function
End Module
