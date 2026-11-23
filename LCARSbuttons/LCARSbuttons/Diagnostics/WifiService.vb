' LCARSbuttons/LCARSbuttons/Diagnostics/WifiService.vb
Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.IO
Imports System.Text

Namespace Diagnostics

''' <summary>Wi-Fi radio / scan / connect via netsh (ported from Quick Controls).</summary>
Public Class WifiNetwork
    Public SSID As String = ""
    Public Signal As Integer = 0
    Public Auth As String = ""

    Public Overrides Function ToString() As String
        Return SSID & "  " & Signal.ToString() & "%  " & Auth
    End Function
End Class

Public Module WifiService

    Public Function IsRadioOn(ByVal interfaceName As String) As Boolean
        Dim name As String = If(String.IsNullOrEmpty(interfaceName), "Wi-Fi", interfaceName)
        Dim text As String = ShellProcess.RunCommand("netsh", "interface show interface")
        For Each line As String In text.Split(New String() {ControlChars.CrLf, ControlChars.Lf}, StringSplitOptions.RemoveEmptyEntries)
            If line.IndexOf(name, StringComparison.OrdinalIgnoreCase) < 0 Then Continue For
            Return line.IndexOf("Disabled", StringComparison.OrdinalIgnoreCase) < 0
        Next
        Return False
    End Function

    Public Function SetRadioOn(ByVal interfaceName As String, ByVal enabled As Boolean) As String
        Return NetworkAdapterService.SetAdminEnabled(
            If(String.IsNullOrEmpty(interfaceName), "Wi-Fi", interfaceName),
            enabled)
    End Function

    Public Function ScanNetworks() As List(Of WifiNetwork)
        Dim list As New List(Of WifiNetwork)()
        Dim text As String = ShellProcess.RunCommand("netsh", "wlan show networks mode=bssid")
        Dim current As WifiNetwork = Nothing
        For Each line As String In text.Split(New String() {ControlChars.CrLf, ControlChars.Lf}, StringSplitOptions.RemoveEmptyEntries)
            Dim trimmed As String = line.Trim()
            If trimmed.StartsWith("SSID ", StringComparison.OrdinalIgnoreCase) AndAlso trimmed.Contains(":") Then
                If current IsNot Nothing Then list.Add(current)
                current = New WifiNetwork()
                current.SSID = trimmed.Substring(trimmed.IndexOf(":"c) + 1).Trim()
            ElseIf current IsNot Nothing Then
                If trimmed.StartsWith("Signal", StringComparison.OrdinalIgnoreCase) Then
                    Dim pct As Integer = 0
                    Integer.TryParse(trimmed.Replace("%", "").Split(":"c)(1).Trim(), pct)
                    If pct > current.Signal Then current.Signal = pct
                ElseIf trimmed.StartsWith("Authentication", StringComparison.OrdinalIgnoreCase) Then
                    current.Auth = trimmed.Substring(trimmed.IndexOf(":"c) + 1).Trim()
                End If
            End If
        Next
        If current IsNot Nothing Then list.Add(current)
        Return list
    End Function

    Public Function Connect(ByVal ssid As String, ByVal password As String, ByVal auth As String) As String
        If String.IsNullOrEmpty(ssid) Then Return "no SSID"
        If IsOpenAuth(auth) Then
            Return AddProfileAndConnect(ssid, BuildOpenProfileXml(ssid))
        End If
        If Not String.IsNullOrEmpty(password) Then
            Return AddProfileAndConnect(ssid, BuildWpaProfileXml(ssid, password))
        End If
        Dim result As String = ShellProcess.RunCommand("netsh", "wlan connect name=" & ShellProcess.Quote(ssid))
        If ConnectLooksSuccessful(result) Then Return result
        If LooksLikeMissingProfile(result) OrElse String.IsNullOrEmpty(result) OrElse LooksLikeConnectFailure(result) Then
            Return "NEED_PASSWORD:" & FirstLine(If(String.IsNullOrEmpty(result), "passphrase required", result))
        End If
        Return result
    End Function

    Public Function Disconnect() As String
        Dim exitCode As Integer = -1
        Dim out As String = ShellProcess.RunCommandEx("netsh", "wlan disconnect", exitCode)
        If exitCode = 0 Then Return "OK"
        Return FirstLine(If(String.IsNullOrEmpty(out), "disconnect failed", out))
    End Function

    Public Function ForgetProfile(ByVal ssid As String) As String
        If String.IsNullOrEmpty(ssid) Then Return "no SSID"
        Dim exitCode As Integer = -1
        Dim out As String = ShellProcess.RunCommandEx("netsh", "wlan delete profile name=" & ShellProcess.Quote(ssid), exitCode)
        If exitCode = 0 Then Return "OK"
        Return FirstLine(If(String.IsNullOrEmpty(out), "forget failed", out))
    End Function

    Public Function GetConnectedSsid() As String
        Dim text As String = ShellProcess.RunCommand("netsh", "wlan show interfaces")
        For Each line As String In text.Split(New String() {ControlChars.CrLf, ControlChars.Lf}, StringSplitOptions.RemoveEmptyEntries)
            Dim trimmed As String = line.Trim()
            If trimmed.StartsWith("SSID", StringComparison.OrdinalIgnoreCase) AndAlso
               Not trimmed.StartsWith("BSSID", StringComparison.OrdinalIgnoreCase) AndAlso
               trimmed.Contains(":") Then
                Return trimmed.Substring(trimmed.IndexOf(":"c) + 1).Trim()
            End If
        Next
        Return ""
    End Function

    Private Function AddProfileAndConnect(ByVal ssid As String, ByVal profileXml As String) As String
        Dim profilePath As String = Path.Combine(Path.GetTempPath(), "lcars-wifi-" & Guid.NewGuid().ToString("N") & ".xml")
        Try
            File.WriteAllText(profilePath, profileXml, Encoding.UTF8)
            ShellProcess.RunCommand("netsh", "wlan add profile filename=" & ShellProcess.Quote(profilePath))
            Dim connectResult As String = ShellProcess.RunCommand("netsh", "wlan connect name=" & ShellProcess.Quote(ssid))
            If String.IsNullOrEmpty(connectResult) Then Return "OK"
            Return connectResult
        Finally
            Try
                File.Delete(profilePath)
            Catch
            End Try
        End Try
    End Function

    Private Function IsOpenAuth(ByVal auth As String) As Boolean
        If String.IsNullOrEmpty(auth) Then Return False
        Return auth.IndexOf("Open", StringComparison.OrdinalIgnoreCase) >= 0
    End Function

    Private Function LooksLikeMissingProfile(ByVal text As String) As Boolean
        If String.IsNullOrEmpty(text) Then Return False
        Dim t As String = text.ToLowerInvariant()
        Return t.Contains("there is no profile") OrElse t.Contains("no profile") OrElse
               t.Contains("not found") OrElse t.Contains("cannot find")
    End Function

    Private Function LooksLikeConnectFailure(ByVal text As String) As Boolean
        If String.IsNullOrEmpty(text) Then Return True
        Dim t As String = text.ToLowerInvariant()
        Return t.Contains("failed") OrElse t.Contains("error") OrElse t.Contains("unable")
    End Function

    Private Function ConnectLooksSuccessful(ByVal text As String) As Boolean
        If String.IsNullOrEmpty(text) Then Return False
        Dim t As String = text.ToLowerInvariant()
        Return t.Contains("successfully") AndAlso Not LooksLikeConnectFailure(text)
    End Function

    Private Function FirstLine(ByVal text As String) As String
        If text Is Nothing Then Return ""
        For Each line As String In text.Split(New String() {ControlChars.CrLf, ControlChars.Lf}, StringSplitOptions.RemoveEmptyEntries)
            Dim t As String = line.Trim()
            If t <> "" Then Return t
        Next
        Return text.Trim()
    End Function

    Private Function BuildOpenProfileXml(ByVal ssid As String) As String
        Dim escapedSsid As String = Security.SecurityElement.Escape(ssid)
        Return "<?xml version=""1.0""?>" &
               "<WLANProfile xmlns=""http://www.microsoft.com/networking/WLAN/profile/v1"">" &
               "<name>" & escapedSsid & "</name>" &
               "<SSIDConfig><SSID><name>" & escapedSsid & "</name></SSID></SSIDConfig>" &
               "<connectionType>ESS</connectionType><connectionMode>auto</connectionMode>" &
               "<MSM><security><authEncryption><authentication>open</authentication>" &
               "<encryption>none</encryption><useOneX>false</useOneX></authEncryption>" &
               "</security></MSM></WLANProfile>"
    End Function

    Private Function BuildWpaProfileXml(ByVal ssid As String, ByVal password As String) As String
        Dim escapedSsid As String = Security.SecurityElement.Escape(ssid)
        Dim escapedPwd As String = Security.SecurityElement.Escape(password)
        Return "<?xml version=""1.0""?>" &
               "<WLANProfile xmlns=""http://www.microsoft.com/networking/WLAN/profile/v1"">" &
               "<name>" & escapedSsid & "</name>" &
               "<SSIDConfig><SSID><name>" & escapedSsid & "</name></SSID></SSIDConfig>" &
               "<connectionType>ESS</connectionType><connectionMode>auto</connectionMode>" &
               "<MSM><security><authEncryption><authentication>WPA2PSK</authentication>" &
               "<encryption>AES</encryption><useOneX>false</useOneX></authEncryption>" &
               "<sharedKey><keyType>passPhrase</keyType><protected>false</protected>" &
               "<keyMaterial>" & escapedPwd & "</keyMaterial></sharedKey></security></MSM></WLANProfile>"
    End Function

End Module

End Namespace
