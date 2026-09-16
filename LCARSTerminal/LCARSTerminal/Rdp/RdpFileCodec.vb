' LCARSTerminal/Rdp/RdpFileCodec.vb
Option Strict On
Option Explicit On

Imports System.Globalization
Imports System.Text

''' <summary>
''' Minimal .rdp key=value codec for core connection fields (no passwords).
''' </summary>
Public Module RdpFileCodec
    Public Function FromProfile(ByVal p As RdpConnectionProfile) As String
        If p Is Nothing Then Throw New ArgumentNullException("p")
        Dim sb As New StringBuilder()
        Dim host As String = If(p.Hostname, "").Trim()
        sb.AppendLine("full address:s:" & host)
        sb.AppendLine("server port:i:" & p.Port.ToString(CultureInfo.InvariantCulture))
        If Not String.IsNullOrWhiteSpace(p.Username) Then
            sb.AppendLine("username:s:" & p.Username.Trim())
        End If
        If Not String.IsNullOrWhiteSpace(p.Domain) Then
            sb.AppendLine("domain:s:" & p.Domain.Trim())
        End If
        sb.AppendLine("redirectclipboard:i:" & If(p.RedirectClipboard, "1", "0"))
        sb.AppendLine("redirectdrives:i:" & If(p.RedirectDrives, "1", "0"))
        sb.AppendLine("redirectprinters:i:" & If(p.RedirectPrinters, "1", "0"))
        sb.AppendLine("audiomode:i:" & If(p.RedirectAudio, "0", "1"))
        sb.AppendLine("smart sizing:i:" & If(p.SmartSizing, "1", "0"))
        Return sb.ToString()
    End Function

    Public Function TryParse(ByVal text As String, ByRef profile As RdpConnectionProfile) As Boolean
        profile = Nothing
        If String.IsNullOrWhiteSpace(text) Then Return False

        Dim p As New RdpConnectionProfile()
        p.Id = Guid.NewGuid()
        p.Port = 3389
        Dim sawHost As Boolean = False

        For Each rawLine As String In text.Replace(vbCr, "").Split(ControlChars.Lf)
            Dim line As String = rawLine.Trim()
            If line.Length = 0 Then Continue For
            If line.StartsWith("password 51:b:", StringComparison.OrdinalIgnoreCase) Then Continue For

            Dim firstColon As Integer = line.IndexOf(":"c)
            If firstColon < 1 Then Continue For
            Dim secondColon As Integer = line.IndexOf(":"c, firstColon + 1)
            If secondColon < 0 Then Continue For

            Dim key As String = line.Substring(0, firstColon).Trim().ToLowerInvariant()
            Dim value As String = line.Substring(secondColon + 1)

            Select Case key
                Case "full address"
                    Dim hostPart As String = value.Trim()
                    Dim portSep As Integer = hostPart.LastIndexOf(":"c)
                    If portSep > 0 Then
                        Dim portText As String = hostPart.Substring(portSep + 1)
                        Dim parsedPort As Integer
                        If Integer.TryParse(portText, NumberStyles.Integer, CultureInfo.InvariantCulture, parsedPort) Then
                            p.Port = parsedPort
                            hostPart = hostPart.Substring(0, portSep)
                        End If
                    End If
                    p.Hostname = hostPart
                    If String.IsNullOrWhiteSpace(p.DisplayName) Then p.DisplayName = hostPart
                    sawHost = Not String.IsNullOrWhiteSpace(hostPart)
                Case "server port"
                    Dim portVal As Integer
                    If Integer.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, portVal) Then
                        p.Port = portVal
                    End If
                Case "username"
                    p.Username = value.Trim()
                Case "domain"
                    p.Domain = value.Trim()
                Case "redirectclipboard"
                    p.RedirectClipboard = ParseBool01(value, True)
                Case "redirectdrives"
                    p.RedirectDrives = ParseBool01(value, False)
                Case "redirectprinters"
                    p.RedirectPrinters = ParseBool01(value, False)
                Case "audiomode"
                    ' 0 = play on this computer, 1 = do not play, 2 = leave at remote
                    Dim audio As Integer
                    If Integer.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, audio) Then
                        p.RedirectAudio = (audio = 0)
                    End If
                Case "smart sizing"
                    p.SmartSizing = ParseBool01(value, True)
            End Select
        Next

        If Not sawHost Then Return False
        profile = p
        Return True
    End Function

    Private Function ParseBool01(ByVal value As String, ByVal defaultValue As Boolean) As Boolean
        Dim n As Integer
        If Integer.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, n) Then
            Return n <> 0
        End If
        Return defaultValue
    End Function
End Module
