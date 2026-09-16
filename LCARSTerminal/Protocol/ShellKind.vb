' LCARSTerminal/Protocol/ShellKind.vb
Option Strict On
Option Explicit On

Public Enum ShellKind
    Cmd = 0
    PowerShell = 1
End Enum

Public Module ShellKindUtil
    ''' <summary>
    ''' Real cmd / PowerShell invoked under a genuine conhost console.
    ''' </summary>
    Public Function GetExeAndArgs(ByVal kind As ShellKind) As String()
        If kind = ShellKind.PowerShell Then
            Return New String() {"powershell.exe", "-NoLogo"}
        End If
        Return New String() {"cmd.exe", "/K"}
    End Function

    ''' <summary>
    ''' Tab strip label including optional admin marker.
    ''' </summary>
    Public Function TabLabel(ByVal kind As ShellKind, ByVal elevated As Boolean) As String
        Dim baseName As String = If(kind = ShellKind.PowerShell, "POWERSHELL", "CMD")
        If elevated Then Return baseName & " · ADMIN"
        Return baseName
    End Function
End Module
