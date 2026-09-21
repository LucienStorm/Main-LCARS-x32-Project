' LCARSTerminal/Rdp/RdpConnectionProfile.vb
Option Strict On
Option Explicit On

''' <summary>
''' Saved remote connection fields (no password — Credential Manager owns secrets).
''' </summary>
Public Class RdpConnectionProfile
    Public Property Id As Guid = Guid.NewGuid()
    Public Property DisplayName As String = ""
    Public Property Hostname As String = ""
    Public Property Username As String = ""
    Public Property Domain As String = ""
    Public Property Port As Integer = 3389
    ''' <summary>0 = RDP, 1 = VNC (see RemoteProtocol).</summary>
    Public Property Protocol As Integer = CInt(RemoteProtocol.Rdp)
    Public Property RememberPassword As Boolean = False
    Public Property RedirectDrives As Boolean = False
    Public Property RedirectPrinters As Boolean = False
    Public Property RedirectClipboard As Boolean = True
    Public Property RedirectAudio As Boolean = True
    Public Property SmartSizing As Boolean = True

    Public Function ProtocolKind() As RemoteProtocol
        If Protocol = CInt(RemoteProtocol.Vnc) Then Return RemoteProtocol.Vnc
        Return RemoteProtocol.Rdp
    End Function

    Public Sub SetProtocolKind(ByVal kind As RemoteProtocol)
        Protocol = CInt(kind)
        If kind = RemoteProtocol.Vnc AndAlso (Port = 3389 OrElse Port <= 0) Then
            Port = 5900
        ElseIf kind = RemoteProtocol.Rdp AndAlso (Port = 5900 OrElse Port <= 0) Then
            Port = 3389
        End If
    End Sub

    Public Function TabLabel() As String
        Dim tag As String = If(ProtocolKind() = RemoteProtocol.Vnc, "VNC ", "")
        If Not String.IsNullOrWhiteSpace(DisplayName) Then Return tag & DisplayName.Trim()
        If Not String.IsNullOrWhiteSpace(Hostname) Then Return tag & Hostname.Trim()
        Return If(ProtocolKind() = RemoteProtocol.Vnc, "VNC", "RDP")
    End Function

    Public Function ServerAddress() As String
        Dim host As String = If(Hostname, "").Trim()
        Dim defaultPort As Integer = If(ProtocolKind() = RemoteProtocol.Vnc, 5900, 3389)
        If Port > 0 AndAlso Port <> defaultPort Then
            Return host & ":" & Port.ToString()
        End If
        Return host
    End Function
End Class
