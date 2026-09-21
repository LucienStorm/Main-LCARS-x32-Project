' LCARSexplorer/NetworkPlacesRoot.vb
Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.IO
Imports System.Net
Imports System.Net.NetworkInformation
Imports System.Net.Sockets
Imports System.Runtime.InteropServices
Imports System.Threading

Public Class SmbDiscoveredHost
    Public Property IpAddress As String = ""
    Public Property Hostname As String = ""
    Public Property IsLocal As Boolean

    Public Function DisplayText() As String
        If IsLocal Then
            Dim localName As String = If(String.IsNullOrEmpty(Hostname), Environment.MachineName, Hostname)
            Return localName & "  (THIS PC)"
        End If
        If String.IsNullOrEmpty(Hostname) OrElse Hostname.Trim().Length = 0 Then Return IpAddress
        If String.Equals(Hostname, IpAddress, StringComparison.OrdinalIgnoreCase) Then Return IpAddress
        Return Hostname & "  (" & IpAddress & ")"
    End Function
End Class

Public Module NetworkPlacesRoot
    Public Const NetworkRootToken As String = "NETWORK:"
    Private Const PerHostTimeoutMs As Integer = 500
    Private Const MaxScanWaitMs As Integer = 180000

    Public Function IsNetworkRoot(ByVal path As String) As Boolean
        If String.IsNullOrEmpty(path) OrElse path.Trim().Length = 0 Then Return False
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

    ''' <summary>
    ''' Scan the LAN for SMB (445/139). Prefers the default-gateway NIC, scans the local /24
    ''' (plus ARP neighbors), waits for all probes to finish, and always includes this PC.
    ''' </summary>
    Public Function ScanSmbHosts(Optional ByVal timeoutMs As Integer = PerHostTimeoutMs) As List(Of SmbDiscoveredHost)
        If timeoutMs <= 0 Then timeoutMs = PerHostTimeoutMs
        Dim results As New List(Of SmbDiscoveredHost)()
        Dim local As UnicastIPAddressInformation = FindPreferredUnicast()
        If local Is Nothing OrElse local.Address Is Nothing Then
            Return results
        End If

        Dim localBytes() As Byte = local.Address.GetAddressBytes()
        If localBytes.Length <> 4 Then Return results
        Dim localIp As String = local.Address.ToString()

        Dim selfHost As New SmbDiscoveredHost()
        selfHost.IpAddress = localIp
        selfHost.Hostname = Environment.MachineName
        selfHost.IsLocal = True
        results.Add(selfHost)

        Dim targets As New Dictionary(Of String, Boolean)(StringComparer.OrdinalIgnoreCase)
        Try
            For Each neighborIp As String In GetArpIpv4Neighbors()
                If Not String.Equals(neighborIp, localIp, StringComparison.OrdinalIgnoreCase) Then
                    targets(neighborIp) = True
                End If
            Next
        Catch
            ' ARP table is optional seed data.
        End Try
        Try
            For Each gwIp As String In GetGatewayIpv4Addresses(local)
                If Not String.Equals(gwIp, localIp, StringComparison.OrdinalIgnoreCase) Then
                    targets(gwIp) = True
                End If
            Next
        Catch
        End Try
        ' Cover the /24 containing this host (works for /22+/16 home LANs too).
        Dim subnetBase As UInteger = ToUInt(New Byte() {localBytes(0), localBytes(1), localBytes(2), 0})
        For offset As UInteger = 1UI To 254UI
            Dim ip As String = FromUInt(subnetBase + offset)
            If Not String.Equals(ip, localIp, StringComparison.OrdinalIgnoreCase) Then
                targets(ip) = True
            End If
        Next

        Dim bag As New Dictionary(Of String, SmbDiscoveredHost)(StringComparer.OrdinalIgnoreCase)
        Dim bagLock As New Object()
        Dim pending As Integer = 0
        Dim done As New ManualResetEvent(False)

        For Each ip As String In targets.Keys
            Interlocked.Increment(pending)
            Dim captureIp As String = ip
            ThreadPool.QueueUserWorkItem(
                Sub(state As Object)
                    Try
                        Dim smb445 As Boolean = ProbePort(captureIp, 445, timeoutMs)
                        Dim smb139 As Boolean = If(smb445, False, ProbePort(captureIp, 139, timeoutMs))
                        If smb445 OrElse smb139 Then
                            Dim host As New SmbDiscoveredHost()
                            host.IpAddress = captureIp
                            host.Hostname = TryResolveName(captureIp)
                            SyncLock bagLock
                                bag(captureIp) = host
                            End SyncLock
                        End If
                    Finally
                        If Interlocked.Decrement(pending) = 0 Then done.Set()
                    End Try
                End Sub)
        Next

        If pending > 0 Then
            ' Previous bug: WaitOne(5000) returned while ThreadPool probes were still running,
            ' so the result list was almost always empty.
            done.WaitOne(MaxScanWaitMs)
        End If

        Dim sorted As New List(Of SmbDiscoveredHost)()
        SyncLock bagLock
            sorted.AddRange(bag.Values)
        End SyncLock
        sorted.Sort(Function(a, b) String.Compare(a.DisplayText(), b.DisplayText(), StringComparison.OrdinalIgnoreCase))
        results.AddRange(sorted)
        Return results
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
            If entry IsNot Nothing AndAlso Not String.IsNullOrEmpty(entry.HostName) Then
                Dim name As String = entry.HostName
                Dim dot As Integer = name.IndexOf("."c)
                If dot > 0 Then name = name.Substring(0, dot)
                Return name
            End If
        Catch
        End Try
        Return ""
    End Function

    Private Function FindPreferredUnicast() As UnicastIPAddressInformation
        Dim fallback As UnicastIPAddressInformation = Nothing
        For Each ni As NetworkInterface In NetworkInterface.GetAllNetworkInterfaces()
            If ni.OperationalStatus <> OperationalStatus.Up Then Continue For
            If ni.NetworkInterfaceType = NetworkInterfaceType.Loopback Then Continue For
            If IsVirtualAdapterName(ni.Name) OrElse IsVirtualAdapterName(ni.Description) Then Continue For

            Dim props As IPInterfaceProperties = ni.GetIPProperties()
            If props Is Nothing OrElse props.UnicastAddresses Is Nothing Then Continue For
            Dim hasGateway As Boolean = HasIpv4Gateway(props)

            For Each addr As UnicastIPAddressInformation In props.UnicastAddresses
                If addr.Address Is Nothing Then Continue For
                If addr.Address.AddressFamily <> AddressFamily.InterNetwork Then Continue For
                Dim ip As String = addr.Address.ToString()
                If ip.StartsWith("169.254.", StringComparison.Ordinal) Then Continue For
                If hasGateway Then Return addr
                If fallback Is Nothing Then fallback = addr
            Next
        Next
        Return fallback
    End Function

    Private Function HasIpv4Gateway(ByVal props As IPInterfaceProperties) As Boolean
        If props Is Nothing OrElse props.GatewayAddresses Is Nothing Then Return False
        For Each gw As GatewayIPAddressInformation In props.GatewayAddresses
            If gw.Address Is Nothing Then Continue For
            If gw.Address.AddressFamily <> AddressFamily.InterNetwork Then Continue For
            Dim g As String = gw.Address.ToString()
            If Not String.IsNullOrEmpty(g) AndAlso Not g.StartsWith("0.") Then Return True
        Next
        Return False
    End Function

    Private Function GetGatewayIpv4Addresses(ByVal local As UnicastIPAddressInformation) As List(Of String)
        Dim list As New List(Of String)()
        If local Is Nothing Then Return list
        Try
            For Each ni As NetworkInterface In NetworkInterface.GetAllNetworkInterfaces()
                Dim props As IPInterfaceProperties = ni.GetIPProperties()
                If props Is Nothing OrElse props.UnicastAddresses Is Nothing Then Continue For
                Dim match As Boolean = False
                For Each addr As UnicastIPAddressInformation In props.UnicastAddresses
                    If addr.Address IsNot Nothing AndAlso addr.Address.Equals(local.Address) Then
                        match = True
                        Exit For
                    End If
                Next
                If Not match OrElse props.GatewayAddresses Is Nothing Then Continue For
                For Each gw As GatewayIPAddressInformation In props.GatewayAddresses
                    If gw.Address Is Nothing Then Continue For
                    If gw.Address.AddressFamily <> AddressFamily.InterNetwork Then Continue For
                    Dim g As String = gw.Address.ToString()
                    If Not String.IsNullOrEmpty(g) AndAlso Not g.StartsWith("0.") Then list.Add(g)
                Next
            Next
        Catch
        End Try
        Return list
    End Function

    Private Function IsVirtualAdapterName(ByVal name As String) As Boolean
        If String.IsNullOrEmpty(name) Then Return False
        Dim n As String = name.ToLowerInvariant()
        If n.Contains("vethernet") Then Return True
        If n.Contains("hyper-v") Then Return True
        If n.Contains("virtualbox") Then Return True
        If n.Contains("vmware") Then Return True
        If n.Contains("docker") Then Return True
        If n.Contains("wsl") Then Return True
        If n.Contains("loopback") Then Return True
        If n.Contains("yggdrasil") Then Return True
        Return False
    End Function

#Region " ARP neighbors (iphlpapi GetIpNetTable) "
    <StructLayout(LayoutKind.Sequential)>
    Private Structure MIB_IPNETROW
        Public dwIndex As Integer
        Public dwPhysAddrLen As Integer
        Public mac0 As Byte
        Public mac1 As Byte
        Public mac2 As Byte
        Public mac3 As Byte
        Public mac4 As Byte
        Public mac5 As Byte
        Public mac6 As Byte
        Public mac7 As Byte
        Public dwAddr As Integer
        Public dwType As Integer
    End Structure

    <DllImport("iphlpapi.dll", SetLastError:=True)>
    Private Function GetIpNetTable(ByVal pIpNetTable As IntPtr, ByRef pdwSize As Integer, ByVal bOrder As Boolean) As Integer
    End Function

    Private Function GetArpIpv4Neighbors() As List(Of String)
        Dim list As New List(Of String)()
        Dim size As Integer = 0
        GetIpNetTable(IntPtr.Zero, size, False)
        If size <= 0 Then Return list
        Dim buffer As IntPtr = Marshal.AllocHGlobal(size)
        Try
            Dim ret As Integer = GetIpNetTable(buffer, size, False)
            If ret <> 0 Then Return list
            Dim entryCount As Integer = Marshal.ReadInt32(buffer)
            Dim rowSize As Integer = Marshal.SizeOf(GetType(MIB_IPNETROW))
            Dim seen As New Dictionary(Of String, Boolean)(StringComparer.OrdinalIgnoreCase)
            For i As Integer = 0 To entryCount - 1
                Dim rowPtr As New IntPtr(buffer.ToInt64() + 4 + CLng(i) * rowSize)
                Dim row As MIB_IPNETROW = CType(Marshal.PtrToStructure(rowPtr, GetType(MIB_IPNETROW)), MIB_IPNETROW)
                ' dwType: 1=other, 2=invalid, 3=dynamic, 4=static — skip invalid
                If row.dwType = 2 Then Continue For
                ' dwAddr is little-endian IPv4 stored as signed DWORD — avoid CUInt overflow.
                Dim raw As Byte() = BitConverter.GetBytes(row.dwAddr)
                Dim ip As String = String.Format("{0}.{1}.{2}.{3}", raw(0), raw(1), raw(2), raw(3))
                If ip.EndsWith(".255") OrElse ip.StartsWith("224.") OrElse ip.StartsWith("239.") Then Continue For
                If ip.StartsWith("169.254.") Then Continue For
                If Not seen.ContainsKey(ip) Then
                    seen(ip) = True
                    list.Add(ip)
                End If
            Next
        Finally
            Marshal.FreeHGlobal(buffer)
        End Try
        Return list
    End Function
#End Region

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
