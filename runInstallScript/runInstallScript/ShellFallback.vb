Imports Microsoft.Win32
Imports System.IO

Friend Module ShellFallback

    Private Function ProgramDataDir() As String
        Return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "LCARS x32")
    End Function

    Private Function PendingShellRestartFlagPath() As String
        Return Path.Combine(ProgramDataDir(), "pending-shell-restart.flag")
    End Function

    Private Function UpdateInProgressFlagPath() As String
        Return Path.Combine(ProgramDataDir(), "update-in-progress.flag")
    End Function

    Private Sub WriteFlag(ByVal flagPath As String)
        Try
            Dim dir As String = Path.GetDirectoryName(flagPath)
            If Not Directory.Exists(dir) Then
                Directory.CreateDirectory(dir)
            End If
            File.WriteAllText(flagPath, DateTime.UtcNow.ToString("u"))
        Catch
        End Try
    End Sub

    Private Sub ClearFlag(ByVal flagPath As String)
        Try
            If File.Exists(flagPath) Then
                File.Delete(flagPath)
            End If
        Catch
        End Try
    End Sub

    Public Sub MarkPendingShellRestart()
        WriteFlag(PendingShellRestartFlagPath())
    End Sub

    Public Sub ClearPendingShellRestartFlag()
        ClearFlag(PendingShellRestartFlagPath())
    End Sub

    Public Function IsPendingShellRestart() As Boolean
        Try
            Return File.Exists(PendingShellRestartFlagPath())
        Catch
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Set while CONTINUE is installing so a Winlogon-respawned LCARS does not
    ''' prewarm OSK and re-lock OnScreenKeyboard.exe on USB mid-copy.
    ''' </summary>
    Public Sub MarkUpdateInProgress()
        WriteFlag(UpdateInProgressFlagPath())
    End Sub

    Public Sub ClearUpdateInProgressFlag()
        ClearFlag(UpdateInProgressFlagPath())
    End Sub

    Public Function IsUpdateInProgress() As Boolean
        Try
            Return File.Exists(UpdateInProgressFlagPath())
        Catch
            Return False
        End Try
    End Function

    Public Function IsLcarsRegisteredShell() As Boolean
        Try
            Using shellKey As RegistryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon")
                If shellKey Is Nothing Then
                    Return False
                End If
                Dim shellCommand As String = CStr(shellKey.GetValue("Shell", "explorer.exe"))
                Return shellCommand.ToLower().Contains("lcarsmain")
            End Using
        Catch
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Starts Explorer when it is not running. Use for recovery when LCARS cannot restart.
    ''' </summary>
    Public Sub EnsureExplorerRunning()
        Try
            If Process.GetProcessesByName("explorer").Length > 0 Then
                Return
            End If
            Dim winDir As String = Environment.GetEnvironmentVariable("windir")
            If String.IsNullOrEmpty(winDir) Then winDir = "C:\Windows"
            Dim explorerPath As String = Path.Combine(winDir, "explorer.exe")
            If File.Exists(explorerPath) Then
                Process.Start(explorerPath)
            Else
                Process.Start("explorer.exe")
            End If
        Catch
            Try
                Process.Start("explorer.exe")
            Catch
            End Try
        End Try
    End Sub

    ''' <summary>
    ''' Optional Explorer start for non-shell installs. When LCARS is the Winlogon shell,
    ''' starting Explorer during an in-place update restart leaves orphan desktop windows
    ''' and a stuck ShellExperienceHost task.
    ''' </summary>
    Public Sub EnsureExplorerRunningIfNeeded()
        If IsLcarsRegisteredShell() Then
            Return
        End If
        EnsureExplorerRunning()
    End Sub

End Module