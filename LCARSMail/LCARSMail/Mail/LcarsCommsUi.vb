' LCARSMail/LCARSMail/Mail/LcarsCommsUi.vb
Option Strict On
Option Infer On
Option Explicit On

Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Reflection
Imports System.Windows.Forms
Imports Microsoft.VisualBasic

''' <summary>LCARS-styled prompts and LCARS Web Browser URL open helpers.</summary>
Public NotInheritable Class LcarsCommsUi

    Private Sub New()
    End Sub

    Public Shared Function Alert(
        ByVal prompt As String,
        Optional ByVal title As String = "LCARS COMMS",
        Optional ByVal buttons As MsgBoxStyle = MsgBoxStyle.OkOnly) As MsgBoxResult

        Return LCARS.UI.MsgBox(If(prompt, ""), buttons, If(title, "LCARS COMMS"))
    End Function

    Public Shared Function Confirm(
        ByVal prompt As String,
        Optional ByVal title As String = "LCARS COMMS") As Boolean

        Return Alert(prompt, title, MsgBoxStyle.YesNo Or MsgBoxStyle.Critical) = MsgBoxResult.Yes
    End Function

    ''' <summary>LCARS input box. Cancel returns empty string.</summary>
    Public Shared Function Prompt(
        ByVal promptText As String,
        Optional ByVal title As String = "LCARS COMMS",
        Optional ByVal defaultResponse As String = "") As String

        Return LCARS.UI.inputbox(If(promptText, ""), If(title, "LCARS COMMS"), If(defaultResponse, ""))
    End Function

    ''' <summary>Password prompt with obscure toggle. Cancel returns Nothing.</summary>
    Public Shared Function PromptPassword(
        ByVal owner As IWin32Window,
        ByVal promptText As String,
        Optional ByVal title As String = "LCARS COMMS — PASSWORD") As String

        Return frmPasswordPrompt.Prompt(owner, If(promptText, ""), If(title, "LCARS COMMS — PASSWORD"))
    End Function

    ''' <summary>Folder picker. Cancel returns Nothing.</summary>
    Public Shared Function PickFolder(
        ByVal owner As IWin32Window,
        Optional ByVal title As String = "SELECT FOLDER",
        Optional ByVal startPath As String = Nothing) As String

        Return frmFolderSelect.Pick(owner, title, startPath)
    End Function

    ''' <summary>Opens a URL in LCARSWebBrowser.exe.</summary>
    Public Shared Function OpenInLcarsBrowser(ByVal url As String) As Boolean
        Dim target As String = If(url, "").Trim()
        If target.Length = 0 Then
            Alert("No URL to open.", "LCARS COMMS — ERROR")
            Return False
        End If

        If Not target.StartsWith("http://", StringComparison.OrdinalIgnoreCase) AndAlso
           Not target.StartsWith("https://", StringComparison.OrdinalIgnoreCase) Then
            target = "https://" & target
        End If

        Dim exe As String = ResolveLcarsBrowserExe()
        If String.IsNullOrEmpty(exe) Then
            Alert("LCARS Web Browser was not found next to Comms." & vbCrLf &
                  "Expected LCARSWebBrowser.exe in the LCARS install folder.",
                  "LCARS COMMS — ERROR")
            Return False
        End If

        Try
            Dim psi As New ProcessStartInfo()
            psi.FileName = exe
            psi.Arguments = """" & target & """"
            psi.WorkingDirectory = Path.GetDirectoryName(exe)
            psi.UseShellExecute = True
            Process.Start(psi)
            Return True
        Catch ex As Exception
            Alert("Could not start LCARS Web Browser." & vbCrLf & vbCrLf & ex.Message, "LCARS COMMS — ERROR")
            Return False
        End Try
    End Function

    Private Shared Function ResolveLcarsBrowserExe() As String
        Dim candidates As New List(Of String)()
        candidates.Add(Path.Combine(Application.StartupPath, "LCARSWebBrowser.exe"))

        Try
            Dim asmDir As String = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            If Not String.IsNullOrEmpty(asmDir) Then
                candidates.Add(Path.Combine(asmDir, "LCARSWebBrowser.exe"))
            End If
        Catch
        End Try

        Try
            Dim installPath As String = GetSetting("LCARS x32", "Application", "InstallPath", "")
            If Not String.IsNullOrEmpty(installPath) Then
                candidates.Add(Path.Combine(installPath, "LCARSWebBrowser.exe"))
            End If
        Catch
        End Try

        For Each pathCandidate As String In candidates
            If Not String.IsNullOrEmpty(pathCandidate) AndAlso File.Exists(pathCandidate) Then
                Return pathCandidate
            End If
        Next

        Return Nothing
    End Function

End Class
