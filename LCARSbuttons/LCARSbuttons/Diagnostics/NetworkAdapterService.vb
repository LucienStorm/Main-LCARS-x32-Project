' LCARSbuttons/LCARSbuttons/Diagnostics/NetworkAdapterService.vb
Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Net
Imports System.Net.NetworkInformation
Imports System.Net.Sockets

Namespace Diagnostics

''' <summary>Enumerates NICs and applies enable/disable + IPv4/DNS via netsh.</summary>
Public Class NetworkAdapterInfo
    Public Id As String = ""
    Public Name As String = ""
    Public Description As String = ""
    Public TypeLabel As String = ""
    Public StatusLabel As String = ""
    Public Mac As String = ""
    Public SpeedMbps As Long = 0
    Public IsWireless As Boolean = False
    Public IsEthernet As Boolean = False
    Public Ip As String = ""
    Public SubnetMask As String = ""
    Public Gateway As String = ""
    Public Dns1 As String = ""
    Public Dns2 As String = ""
    Public DhcpEnabled As Boolean = True
    Public OperationalStatus As OperationalStatus = OperationalStatus.Unknown

    Public Overrides Function ToString() As String
        Return Name & "  [" & TypeLabel & "]  " & StatusLabel
    End Function
End Class

Public Module NetworkAdapterService

    Public Function ListAdapters() As List(Of NetworkAdapterInfo)
        Dim list As New List(Of NetworkAdapterInfo)()
        For Each ni As NetworkInterface In NetworkInterface.GetAllNetworkInterfaces()
            If ni Is Nothing Then Continue For
            If ni.NetworkInterfaceType = NetworkInterfaceType.Loopback Then Continue For
            Dim info As New NetworkAdapterInfo()
            info.Id = ni.Id
            info.Name = If(ni.Name, "")
            info.Description = If(ni.Description, "")
            info.OperationalStatus = ni.OperationalStatus
            info.StatusLabel = ni.OperationalStatus.ToString().ToUpperInvariant()
            info.IsWireless = (ni.NetworkInterfaceType = NetworkInterfaceType.Wireless80211)
            info.IsEthernet = IsEthernetType(ni)
            info.TypeLabel = TypeLabelFor(ni)
            Try
                Dim mac() As Byte = ni.GetPhysicalAddress().GetAddressBytes()
                If mac IsNot Nothing AndAlso mac.Length > 0 Then
                    Dim parts As New List(Of String)()
                    For Each b As Byte In mac
                        parts.Add(b.ToString("X2"))
                    Next
                    info.Mac = String.Join(":", parts.ToArray())
                End If
            Catch
            End Try
            Try
                If ni.Speed > 0 Then info.SpeedMbps = ni.Speed \ 1000000L
            Catch
            End Try
            FillIp(ni, info)
            list.Add(info)
        Next
        list.Sort(Function(a, b) String.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase))
        Return list
    End Function

    Public Function FindByName(ByVal name As String) As NetworkAdapterInfo
        If String.IsNullOrEmpty(name) Then Return Nothing
        For Each a As NetworkAdapterInfo In ListAdapters()
            If String.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase) Then Return a
        Next
        Return Nothing
    End Function

    Public Function SetAdminEnabled(ByVal interfaceName As String, ByVal enabled As Boolean) As String
        If String.IsNullOrEmpty(interfaceName) Then Return "no interface"
        Dim state As String = If(enabled, "enabled", "disabled")
        Dim exitCode As Integer = -1
        Dim out As String = ShellProcess.RunCommandEx(
            "netsh",
            "interface set interface name=" & ShellProcess.Quote(interfaceName) & " admin=" & state,
            exitCode)
        If exitCode = 0 Then Return "OK"
        Return FirstLine(If(String.IsNullOrEmpty(out), "exit " & exitCode.ToString(), out))
    End Function

    Public Function ApplyDhcp(ByVal interfaceName As String) As String
        If String.IsNullOrEmpty(interfaceName) Then Return "no interface"
        Dim q As String = ShellProcess.Quote(interfaceName)
        Dim exitCode As Integer = -1
        Dim a As String = ShellProcess.RunCommandEx("netsh", "interface ip set address name=" & q & " source=dhcp", exitCode)
        If exitCode <> 0 Then Return FirstLine(If(String.IsNullOrEmpty(a), "dhcp address failed", a))
        exitCode = -1
        Dim d As String = ShellProcess.RunCommandEx("netsh", "interface ip set dns name=" & q & " source=dhcp", exitCode)
        If exitCode <> 0 Then Return FirstLine(If(String.IsNullOrEmpty(d), "dhcp dns failed", d))
        Return "OK"
    End Function

    Public Function ApplyStatic(
        ByVal interfaceName As String,
        ByVal ip As String,
        ByVal mask As String,
        ByVal gateway As String,
        ByVal dns1 As String,
        ByVal dns2 As String) As String

        If String.IsNullOrEmpty(interfaceName) Then Return "no interface"
        If String.IsNullOrEmpty(ip) OrElse String.IsNullOrEmpty(mask) Then Return "ip and mask required"
        Dim q As String = ShellProcess.Quote(interfaceName)
        Dim args As String = "interface ip set address name=" & q & " static " & ip.Trim() & " " & mask.Trim()
        If Not String.IsNullOrEmpty(gateway) Then args &= " " & gateway.Trim() & " 1"
        Dim exitCode As Integer = -1
        Dim a As String = ShellProcess.RunCommandEx("netsh", args, exitCode)
        If exitCode <> 0 Then Return FirstLine(If(String.IsNullOrEmpty(a), "static address failed", a))

        If String.IsNullOrEmpty(dns1) Then
            exitCode = -1
            ShellProcess.RunCommandEx("netsh", "interface ip set dns name=" & q & " source=dhcp", exitCode)
        Else
            exitCode = -1
            Dim d1 As String = ShellProcess.RunCommandEx(
                "netsh",
                "interface ip set dns name=" & q & " static " & dns1.Trim() & " primary",
                exitCode)
            If exitCode <> 0 Then Return FirstLine(If(String.IsNullOrEmpty(d1), "dns1 failed", d1))
            If Not String.IsNullOrEmpty(dns2) Then
                exitCode = -1
                Dim d2 As String = ShellProcess.RunCommandEx(
                    "netsh",
                    "interface ip add dns name=" & q & " " & dns2.Trim() & " index=2",
                    exitCode)
                If exitCode <> 0 Then
                    ' Some builds use "set dns … index=2" — try once more
                    ShellProcess.RunCommandEx(
                        "netsh",
                        "interface ip set dns name=" & q & " static " & dns2.Trim() & " index=2",
                        exitCode)
                End If
            End If
        End If
        Return "OK"
    End Function

    Public Function IsAdminEnabled(ByVal interfaceName As String) As Boolean
        If String.IsNullOrEmpty(interfaceName) Then Return False
        Dim text As String = ShellProcess.RunCommand("netsh", "interface show interface")
        For Each line As String In text.Split(New String() {ControlChars.CrLf, ControlChars.Lf}, StringSplitOptions.RemoveEmptyEntries)
            If line.IndexOf(interfaceName, StringComparison.OrdinalIgnoreCase) < 0 Then Continue For
            Return line.IndexOf("Disabled", StringComparison.OrdinalIgnoreCase) < 0
        Next
        Return True
    End Function

    Private Function IsEthernetType(ByVal ni As NetworkInterface) As Boolean
        If ni.NetworkInterfaceType = NetworkInterfaceType.Ethernet Then Return True
        Dim t As String = ni.NetworkInterfaceType.ToString()
        If t.IndexOf("Ethernet", StringComparison.OrdinalIgnoreCase) >= 0 Then Return True
        Dim d As String = If(ni.Description, "") & " " & If(ni.Name, "")
        Return d.IndexOf("Ethernet", StringComparison.OrdinalIgnoreCase) >= 0 AndAlso
               ni.NetworkInterfaceType <> NetworkInterfaceType.Wireless80211
    End Function

    Private Function TypeLabelFor(ByVal ni As NetworkInterface) As String
        If ni.NetworkInterfaceType = NetworkInterfaceType.Wireless80211 Then Return "WI-FI"
        If IsEthernetType(ni) Then Return "ETHERNET"
        Select Case ni.NetworkInterfaceType
            Case NetworkInterfaceType.Tunnel, NetworkInterfaceType.Ppp
                Return "VPN/TUNNEL"
            Case Else
                Return ni.NetworkInterfaceType.ToString().ToUpperInvariant()
        End Select
    End Function

    Private Sub FillIp(ByVal ni As NetworkInterface, ByVal info As NetworkAdapterInfo)
        Try
            Dim props As IPInterfaceProperties = ni.GetIPProperties()
            If props Is Nothing Then Return
            Try
                Dim v4 As IPv4InterfaceProperties = props.GetIPv4Properties()
                If v4 IsNot Nothing Then info.DhcpEnabled = v4.IsDhcpEnabled
            Catch
            End Try
            If props.GatewayAddresses IsNot Nothing Then
                For Each gw As GatewayIPAddressInformation In props.GatewayAddresses
                    If gw.Address Is Nothing Then Continue For
                    If gw.Address.AddressFamily <> AddressFamily.InterNetwork Then Continue For
                    Dim g As String = gw.Address.ToString()
                    If g = "0.0.0.0" Then Continue For
                    info.Gateway = g
                    Exit For
                Next
            End If
            If props.DnsAddresses IsNot Nothing Then
                Dim dnsList As New List(Of String)()
                For Each d As IPAddress In props.DnsAddresses
                    If d Is Nothing OrElse d.AddressFamily <> AddressFamily.InterNetwork Then Continue For
                    dnsList.Add(d.ToString())
                Next
                If dnsList.Count > 0 Then info.Dns1 = dnsList(0)
                If dnsList.Count > 1 Then info.Dns2 = dnsList(1)
            End If
            If props.UnicastAddresses Is Nothing Then Return
            For Each addr As UnicastIPAddressInformation In props.UnicastAddresses
                If addr.Address Is Nothing Then Continue For
                If addr.Address.AddressFamily <> AddressFamily.InterNetwork Then Continue For
                Dim ip As String = addr.Address.ToString()
                If ip.StartsWith("169.254.", StringComparison.Ordinal) Then Continue For
                info.Ip = ip
                Try
                    If addr.IPv4Mask IsNot Nothing Then info.SubnetMask = addr.IPv4Mask.ToString()
                Catch
                End Try
                Exit For
            Next
        Catch
        End Try
    End Sub

    Private Function FirstLine(ByVal text As String) As String
        If text Is Nothing Then Return ""
        For Each line As String In text.Split(New String() {ControlChars.CrLf, ControlChars.Lf}, StringSplitOptions.RemoveEmptyEntries)
            Dim t As String = line.Trim()
            If t <> "" Then Return t
        Next
        Return text.Trim()
    End Function

End Module

End Namespace
