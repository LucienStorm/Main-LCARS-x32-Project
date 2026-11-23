' LCARSbuttons/LCARSbuttons/Diagnostics/ShellProcess.vb
Option Strict On
Option Explicit On

Imports System.Diagnostics

Namespace Diagnostics

''' <summary>Hidden process runner for netsh / powercfg / control panel helpers.</summary>
Public Module ShellProcess

    Public Function RunCommand(ByVal fileName As String, ByVal arguments As String) As String
        Dim unused As Integer = 0
        Return RunCommandEx(fileName, arguments, unused)
    End Function

    Public Function RunCommandEx(ByVal fileName As String, ByVal arguments As String, ByRef exitCode As Integer) As String
        Dim psi As New ProcessStartInfo()
        psi.FileName = fileName
        psi.Arguments = arguments
        psi.UseShellExecute = False
        psi.RedirectStandardOutput = True
        psi.RedirectStandardError = True
        psi.CreateNoWindow = True
        Using proc As Process = Process.Start(psi)
            Dim output As String = proc.StandardOutput.ReadToEnd()
            Dim err As String = proc.StandardError.ReadToEnd()
            proc.WaitForExit()
            exitCode = proc.ExitCode
            If String.IsNullOrEmpty(output) Then Return err
            Return output
        End Using
    End Function

    Public Function Quote(ByVal value As String) As String
        If value Is Nothing Then Return """"""
        Return """" & value.Replace("""", "") & """"
    End Function

    Public Sub StartControlPanel(ByVal target As String)
        Try
            Process.Start(target)
        Catch
            Try
                Process.Start("control.exe", target)
            Catch
            End Try
        End Try
    End Sub

End Module

End Namespace
