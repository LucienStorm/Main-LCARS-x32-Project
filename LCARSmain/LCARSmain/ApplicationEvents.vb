Option Strict On

<Assembly: System.Security.Permissions.PermissionSet(Security.Permissions.SecurityAction.RequestMinimum, name:="FullTrust")> 
Namespace My

    ' The following events are available for MyApplication:
    ' 
    ' Startup: Raised when the application starts, before the startup form is created.
    ' Shutdown: Raised after all application forms are closed.  This event is not raised if the application terminates abnormally.
    ' UnhandledException: Raised if the application encounters an unhandled exception.
    ' StartupNextInstance: Raised when launching a single-instance application and the application is already active. 
    ' NetworkAvailabilityChanged: Raised when the network connection is connected or disconnected.
    Partial Friend Class MyApplication
        Dim hasfailed As Boolean = False
        Private isSettings As Boolean = False

        Private Sub MyApplication_Startup(ByVal sender As Object, ByVal e As Microsoft.VisualBasic.ApplicationServices.StartupEventArgs) Handles Me.Startup
            modDiagnostics.LogStartupBanner()
            AddHandler System.Windows.Forms.Application.ThreadException, AddressOf Application_ThreadException
            ' Winlogon may respawn LCARS mid-install. Exit immediately so we do not
            ' prewarm OSK / lock USB binaries while runInstallScript is copying.
            ' A hard reboot leaves the flag behind with no installer — that used to
            ' cancel startup with no Explorer fallback (black desktop). Only block
            ' while runInstallScript is actually running and the flag is fresh.
            If modShellFallback.IsUpdateInProgress() AndAlso Not Command().ToLower().Contains("-u") Then
                If modShellFallback.ShouldHonorUpdateInProgressBlock() Then
                    modDiagnostics.LogInfo("MyApplication_Startup", "update-in-progress.flag + installer running — exiting without shell init; starting Explorer so desktop is not blank")
                    modShellFallback.EnsureExplorerRunning()
                    e.Cancel = True
                    Return
                End If
                modDiagnostics.LogInfo("MyApplication_Startup", "stale update-in-progress.flag (no installer) — clearing and continuing shell start")
                modShellFallback.ClearUpdateInProgress()
                modShellFallback.ClearPendingShellRestart()
            End If
            If Command().ToLower().Contains("--settings") Then
                'Load settings
                If Process.GetProcessesByName("LCARSmain").Length = 1 Then
                    MainForm = frmSettings
                    isSettings = True
                Else
                    'Send message current instance to load settings.
                    InterMsgID = frmStartup.RegisterWindowMessage("LCARS_X32_MSG")
                    SendMessage(HWND_BROADCAST, InterMsgID, 0, New IntPtr(2))
                    End
                End If
            Else
                'Run as shell
                Dim x32Processes() As Process = Process.GetProcessesByName("LCARSmain")
                If x32Processes.Length > 1 Then
                    InterMsgID = frmStartup.RegisterWindowMessage("LCARS_X32_MSG")
                    If Debugger.IsAttached Then
                        Dim other As Process = Nothing
                        For Each p As Process In x32Processes
                            If p.Id <> Process.GetCurrentProcess().Id Then
                                other = p
                                Exit For
                            End If
                        Next
                        If other Is Nothing Then
                            MsgBox("Unable to find other process")
                        End If
                        Dim handle As IntPtr = New IntPtr(CInt(GetSetting("LCARS x32", "Application", "MainWindowHandle", "0")))
                        While Not other.HasExited And handle <> IntPtr.Zero
                            Try
                                SendMessage(handle, WM_EXPLORER_CLOSE, 0, IntPtr.Zero)
                            Catch ex As Exception
                                MsgBox(ex.ToString & vbNewLine & ex.StackTrace, Title:="Error taking control")
                            End Try
                            Threading.Thread.Sleep(1000)
                        End While
                    Else
                        'Send message to current instance to switch to shell mode
                        SendMessage(HWND_BROADCAST, InterMsgID, 0, New IntPtr(3))
                    End If
                End If
            End If
        End Sub
        Private Sub MyApplication_UnhandledException(ByVal sender As Object, ByVal e As Microsoft.VisualBasic.ApplicationServices.UnhandledExceptionEventArgs) Handles Me.UnhandledException
            modDiagnostics.LogException("UnhandledException", e.Exception, "ExitApplication=" & e.ExitApplication.ToString())
            ' Always restore a desktop first — before MsgBox / LCARSshutdown, which can hang on a tablet.
            If modShellFallback.IsLcarsRegisteredShell() Then
                modShellFallback.CleanupOrphanShellProcesses()
            Else
                modShellFallback.EnsureExplorerRunningIfNeeded()
            End If
            If Not hasfailed Then
                hasfailed = True
                Try
                    Using mywriter As New System.IO.StreamWriter(My.Computer.FileSystem.SpecialDirectories.Desktop & "\LCARSError.txt", True)
                        mywriter.WriteLine(Now.ToLongDateString() & " " & Now.ToShortTimeString())
                        mywriter.WriteLine(e.Exception.ToString())
                        If Not e.Exception.InnerException Is Nothing Then
                            mywriter.WriteLine(e.Exception.InnerException.ToString())
                        End If
                        mywriter.WriteLine()
                        mywriter.WriteLine(My.Computer.Info.OSFullName)
                        mywriter.WriteLine()
                    End Using
                Catch
                End Try
                If Not IsSettingsMode Then
                    Try
                        e.ExitApplication = False
                        doDeactivate(Nothing)
                    Catch
                        If modShellFallback.IsLcarsRegisteredShell() Then
                            modShellFallback.CleanupOrphanShellProcesses()
                        Else
                            modShellFallback.EnsureExplorerRunningIfNeeded()
                        End If
                    End Try
                End If
                If modShellFallback.IsLcarsRegisteredShell() Then
                    modShellFallback.CleanupOrphanShellProcesses()
                Else
                    modShellFallback.EnsureExplorerRunningIfNeeded()
                End If
            Else
                If modShellFallback.IsLcarsRegisteredShell() Then
                    modShellFallback.CleanupOrphanShellProcesses()
                Else
                    modShellFallback.EnsureExplorerRunningIfNeeded()
                End If
                End
            End If
        End Sub

        Private Sub Application_ThreadException(ByVal sender As Object, ByVal e As Threading.ThreadExceptionEventArgs)
            modDiagnostics.LogException("ThreadException", e.Exception)
            If modShellFallback.IsLcarsRegisteredShell() Then
                modShellFallback.CleanupOrphanShellProcesses()
            Else
                modShellFallback.EnsureExplorerRunningIfNeeded()
            End If
        End Sub

        Private Sub MyApplication_Shutdown(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Shutdown
            If modShellFallback.ShouldStartExplorerOnShutdown() Then
                modShellFallback.EnsureExplorerRunning()
            End If
        End Sub

        Public Sub SwitchToShellFromSettings()
            If isSettings Then
                Application.MainForm = frmStartup
                MainForm.Show()
            End If
        End Sub

        Public ReadOnly Property IsSettingsMode() As Boolean
            Get
                Return isSettings
            End Get
        End Property
    End Class

End Namespace

