' LCARSTerminal/Rdp/RdpConnectionProfile.vb
Option Strict On
Option Explicit On

''' <summary>
''' Saved RDP connection fields (no password — Credential Manager owns secrets).
''' </summary>
Public Class RdpConnectionProfile
    Public Property Id As Guid = Guid.NewGuid()
    Public Property DisplayName As String = ""
    Public Property Hostname As String = ""
    Public Property Username As String = ""
    Public Property Domain As String = ""
    Public Property Port As Integer = 3389
    Public Property RememberPassword As Boolean = False
    Public Property RedirectDrives As Boolean = False
    Public Property RedirectPrinters As Boolean = False
    Public Property RedirectClipboard As Boolean = True
    Public Property RedirectAudio As Boolean = True
    Public Property SmartSizing As Boolean = True

    Public Function TabLabel() As String
        If Not String.IsNullOrWhiteSpace(DisplayName) Then Return DisplayName.Trim()
        If Not String.IsNullOrWhiteSpace(Hostname) Then Return Hostname.Trim()
        Return "RDP"
    End Function

    Public Function ServerAddress() As String
        Dim host As String = If(Hostname, "").Trim()
        If Port > 0 AndAlso Port <> 3389 Then
            Return host & ":" & Port.ToString()
        End If
        Return host
    End Function
End Class
