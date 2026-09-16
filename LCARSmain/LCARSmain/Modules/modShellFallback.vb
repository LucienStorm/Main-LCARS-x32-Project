Imports System.Diagnostics
Imports System.IO
Imports Microsoft.Win32

''' <summary>
''' Restores Windows Explorer when LCARS exits or fails as the login shell.
''' </summary>
Public Module modShellFallback

    Private shellCleanupTimer As System.Windows.Forms.Timer = Nothing

    Private Function ProgramDataDir() As String
        Return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "LCARS x32")
    End Function

    Private Function PendingShellRestartFlagPath() As String
        Return Path.Combine(ProgramDataDir(), "pending-shell-restart.flag")
    End Function

    Private Function UpdateInProgressFlagPath() As String
        Return Path.Combine(ProgramDataDir(), "update-in-progress.flag")
    End Function

    Public Function IsPendingShellRestart() As Boolean
        Try
            Return File.Exists(PendingShellRestartFlagPath())
        Catch
            Return False
        End Try
    End Function

    Public Sub ClearPendingShellRestart()
        Try
            Dim flagPath As String = PendingShellRestartFlagPath()
            If File.Exists(flagPath) Then
                File.Delete(flagPath)
            End If
        Catch
        End Try
    End Sub

    ''' <summary>
    ''' True while runInstallScript is copying update files. Skip OSK prewarm/start
    ''' so Winlogon-respawned LCARS does not re-lock OnScreenKeyboard.exe on USB.
    ''' </summary>
    Public Function IsUpdateInProgress() As Boolean
        Try
            Return File.Exists(UpdateInProgressFlagPath())
        Catch
            Return False
        End Try
    End Function

    Public Sub ClearUpdateInProgress()
        Try
            Dim flagPath As String = UpdateInProgressFlagPath()
            If File.Exists(flagPath) Then
                File.Delete(flagPath)
            End If
        Catch
        End Try
    End Sub

    ''' <summary>
    ''' True when Winlogon is configured to launch LCARSmain instead of Explorer.
    ''' </summary>
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
    ''' Starts Explorer if it is not already running. Use on crashes when the user
    ''' needs a recoverable desktop.
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
    ''' Optional Explorer start when LCARS runs beside Explorer (not as Winlogon shell).
    ''' </summary>
    Public Sub EnsureExplorerRunningIfNeeded()
        If IsLcarsRegisteredShell() Then
            Return
        End If
        EnsureExplorerRunning()
    End Sub

    ''' <summary>
    ''' Whether MyApplication_Shutdown should spawn Explorer. Skip during update restarts
    ''' so a second desktop shell is not left running beside the new LCARS instance.
    ''' </summary>
    Public Function ShouldStartExplorerOnShutdown() As Boolean
        If IsPendingShellRestart() Then
            Return False
        End If
        If IsLcarsRegisteredShell() Then
            Return False
        End If
        Return True
    End Function

    ''' <summary>
    ''' When LCARS is the Winlogon shell, stray Explorer / ShellExperienceHost processes
    ''' from update recovery leave an unclosable taskbar entry. Terminate them quietly.
    ''' </summary>
    Public Sub CleanupOrphanShellProcesses()
        If Not IsLcarsRegisteredShell() Then Return

        Try
            For Each proc As Process In Process.GetProcessesByName("explorer")
                Try
                    proc.Kill()
                Catch
                End Try
            Next
        Catch
        End Try

        For Each processName As String In New String() {"ShellExperienceHost", "StartMenuExperienceHost", "ApplicationFrameHost"}
            Try
                For Each proc As Process In Process.GetProcessesByName(processName)
                    Try
                        proc.Kill()
                        proc.WaitForExit(500)
                    Catch
                    End Try
                Next
            Catch
            End Try
        Next
    End Sub

    ''' <summary>
    ''' ShellExperienceHost respawns after updates; keep sweeping while LCARS is the shell.
    ''' </summary>
    Public Sub ScheduleOrphanShellCleanup()
        If Not IsLcarsRegisteredShell() Then Return

        CleanupOrphanShellProcesses()

        Dim delays() As Integer = New Integer() {1000, 3000, 10000, 30000, 60000, 120000}
        For Each delayMs As Integer In delays
            Dim timer As New System.Windows.Forms.Timer()
            timer.Interval = delayMs
            AddHandler timer.Tick, Sub(sender As Object, e As EventArgs)
                                       timer.Stop()
                                       timer.Dispose()
                                       CleanupOrphanShellProcesses()
                                   End Sub
            timer.Start()
        Next

        If shellCleanupTimer IsNot Nothing Then
            shellCleanupTimer.Stop()
            shellCleanupTimer.Dispose()
        End If
        shellCleanupTimer = New System.Windows.Forms.Timer()
        shellCleanupTimer.Interval = 15000
        AddHandler shellCleanupTimer.Tick, Sub(sender As Object, e As EventArgs)
                                               If Not IsLcarsRegisteredShell() Then
                                                   shellCleanupTimer.Stop()
                                                   Return
                                               End If
                                               CleanupOrphanShellProcesses()
                                           End Sub
        shellCleanupTimer.Start()
    End Sub

End Module
