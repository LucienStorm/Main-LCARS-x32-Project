' LCARSTerminal/Rdp/RemoteNetworkDiscovery.vb
Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Net
Imports System.Net.NetworkInformation
Imports System.Net.Sockets
Imports System.Threading

''' <summary>
''' Scans the local IPv4 subnet for hosts listening on RDP (3389) and/or VNC (5900).
''' </summary>
Public Class RemoteDiscoveredHost
    Public Property IpAddress As String = ""
    Public Property Hostname As String = ""
    Public Property RdpOpen As Boolean
    Public Property VncOpen As Boolean

    Public Function DisplayText() As String
        Dim name As String = If(String.IsNullOrWhiteSpace(Hostname), IpAddress, Hostname)
        Dim tags As New List(Of String)
        If RdpOpen Then tags.Add("RDP")
        If VncOpen Then tags.Add("VNC")
        Dim tag As String = If(tags.Count = 0, "?", String.Join("/", tags.ToArray()))
        If String.Equals(name, IpAddress, StringComparison.OrdinalIgnoreCase) Then
            Return tag & "  " & IpAddress
        End If
        Return tag & "  " & name & "  (" & IpAddress & ")"
    End Function
End Class

Public Class RemoteNetworkDiscovery
    Public Shared Function ScanLocalSubnet(Optional ByVal perHostTimeoutMs As Integer = 350) As List(Of RemoteDiscoveredHost)
        Dim results As New List(Of RemoteDiscoveredHost)()
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
            ' Cap oversized / weird masks; scan last octet of /24-style nets only.
            hostCount = 254
            network = ToUInt(New Byte() {baseBytes(0), baseBytes(1), baseBytes(2), 0})
        End If

        Dim bag As New System.Collections.Concurrent.ConcurrentDictionary(Of String, RemoteDiscoveredHost)()
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
                        Dim rdp As Boolean = ProbePort(ip, 3389, perHostTimeoutMs)
                        Dim vnc As Boolean = ProbePort(ip, 5900, perHostTimeoutMs)
                        If rdp OrElse vnc Then
                            Dim host As New RemoteDiscoveredHost()
                            host.IpAddress = ip
                            host.RdpOpen = rdp
                            host.VncOpen = vnc
                            host.Hostname = TryResolveName(ip)
                            bag(ip) = host
                        End If
                    Finally
                        If Interlocked.Decrement(pending) = 0 Then done.Set()
                    End Try
                End Sub)
        Next

        If pending = 0 Then Return results
        ' Wait until all probes finish (was Math.Max(5000, timeout*4) which dropped late hits).
        done.WaitOne(Math.Max(120000, perHostTimeoutMs * 260))
        Dim sorted As New List(Of RemoteDiscoveredHost)(bag.Values)
        sorted.Sort(Function(a, b) String.Compare(a.DisplayText(), b.DisplayText(), StringComparison.OrdinalIgnoreCase))
        Return sorted
    End Function

    Private Shared Function ProbePort(ByVal ip As String, ByVal port As Integer, ByVal timeoutMs As Integer) As Boolean
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

    Private Shared Function TryResolveName(ByVal ip As String) As String
        Try
            Dim entry As IPHostEntry = Dns.GetHostEntry(ip)
            If entry IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(entry.HostName) Then
                Dim name As String = entry.HostName
                Dim dot As Integer = name.IndexOf("."c)
                If dot > 0 Then name = name.Substring(0, dot)
                Return name
            End If
        Catch
        End Try
        Return ""
    End Function

    Private Shared Function FindPrimaryUnicast() As UnicastIPAddressInformation
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

    Private Shared Function ToUInt(ByVal bytes() As Byte) As UInteger
        Return (CUInt(bytes(0)) << 24) Or (CUInt(bytes(1)) << 16) Or (CUInt(bytes(2)) << 8) Or CUInt(bytes(3))
    End Function

    Private Shared Function FromUInt(ByVal value As UInteger) As String
        Return String.Format("{0}.{1}.{2}.{3}",
                             (value >> 24) And &HFFUI,
                             (value >> 16) And &HFFUI,
                             (value >> 8) And &HFFUI,
                             value And &HFFUI)
    End Function
End Class
