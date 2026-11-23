Public Class frmShutdown
    Inherits LCARS.LCARSForm

    Dim closingLCARS As Boolean = False

    Protected Overrides Sub OnLCARSClosing()
        If Not closingLCARS Then
            MyBase.OnLCARSClosing()
        End If
    End Sub

    Protected Overrides Sub OnShellChromeLayout()
        PlaceShellAlignedCloseButton(sbExitMyComp)
    End Sub
#Region " API "
    Declare Function RegisterWindowMessageA Lib "user32.dll" (ByVal lpString As String) As Integer
    Public Declare Auto Function SendMessage Lib "user32.dll" (ByVal hWnd As IntPtr, ByVal msg As Integer, ByVal wParam As IntPtr, ByVal lParam As IntPtr) As IntPtr
    Private Declare Function PostMessage Lib "user32.dll" Alias "PostMessageA" (ByVal hwnd As Integer, ByVal wMsg As Integer, ByVal wParam As Integer, ByVal lParam As Integer) As Integer
    Private Declare Function IsWindow Lib "user32.dll" (ByVal hWnd As IntPtr) As Boolean

    Public InterMsgID As Integer
    Const WM_COPYDATA As Integer = &H4A
    Dim x32Handle As IntPtr = IntPtr.Zero
    Public Const HWND_BROADCAST As Integer = &HFFFF

    Structure COPYDATASTRUCT
        Public dwData As IntPtr
        Public cdData As Integer
        Public lpData As IntPtr
    End Structure

#End Region

    Dim shutdownOptions As New cWrapExitWindows


    Private Sub sbShutdown_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles sbShutdown.Click
        closingLCARS = True
        Me.Hide()
        CloseLCARS()
        shutdownOptions.ExitWindows(cWrapExitWindows.Action.Shutdown)
    End Sub

    Private Sub sbExit_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles sbExit.Click
        closingLCARS = True
        Me.Hide()
        CloseLCARS()
        Dim waitedMs As Integer = 0
        While Process.GetProcessesByName("LCARSmain").Length > 0 AndAlso waitedMs < 15000
            Application.DoEvents()
            Threading.Thread.Sleep(100)
            waitedMs += 100
        End While
        If Not Process.GetProcessesByName("explorer").Length > 0 Then
            Process.Start("explorer.exe")
        End If
        End
    End Sub

    Private Sub sbRestart_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles sbRestart.Click
        closingLCARS = True
        Me.Hide()
        CloseLCARS()
        shutdownOptions.ExitWindows(cWrapExitWindows.Action.Restart)
    End Sub

    Private Sub sbLogOff_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles sbLogOff.Click
        closingLCARS = True
        Me.Hide()
        CloseLCARS()
        shutdownOptions.ExitWindows(cWrapExitWindows.Action.LogOff)
    End Sub

    Private Sub sbSuspend_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles sbSuspend.Click
        Application.SetSuspendState(PowerState.Suspend, True, False)
    End Sub

    Private Sub sbHibernate_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles sbHibernate.Click
        Application.SetSuspendState(PowerState.Hibernate, True, False)
    End Sub

    Private Function TryParseHandle(ByVal text As String) As IntPtr
        Dim value As Long
        If Long.TryParse(text, value) AndAlso value <> 0 Then
            Return New IntPtr(value)
        End If
        Return IntPtr.Zero
    End Function

    Private Function ResolveX32Handle(ByVal commandParts() As String) As IntPtr
        ' Prefer the handle passed on the command line by DEACTIVATE.
        If commandParts IsNot Nothing AndAlso commandParts.GetUpperBound(0) >= 1 Then
            Dim fromArgs As IntPtr = TryParseHandle(commandParts(1).Trim())
            If fromArgs <> IntPtr.Zero AndAlso IsWindow(fromArgs) Then
                Return fromArgs
            End If
        End If

        Dim fromSettings As IntPtr = TryParseHandle(GetSetting("LCARS x32", "Application", "MainWindowHandle", "0"))
        If fromSettings <> IntPtr.Zero AndAlso IsWindow(fromSettings) Then
            Return fromSettings
        End If

        Return IntPtr.Zero
    End Function

    Private Sub frmShutdown_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        InterMsgID = RegisterWindowMessageA("LCARS_X32_MSG")
        Dim myCommands() As String = System.Environment.CommandLine.Split("/"c)

        x32Handle = ResolveX32Handle(myCommands)
        If x32Handle <> IntPtr.Zero Then
            ' Non-blocking register — SendMessage to a bad/busy hwnd was hanging DEACTIVATE.
            PostMessage(CInt(x32Handle), InterMsgID, CInt(Me.Handle), 1)
        End If

        If myCommands.GetUpperBound(0) > 1 Then
            If myCommands(2).Trim() = "c" Then
                sbExit.doClick(New Object, New System.EventArgs)
            End If
        End If
    End Sub

    Private Sub sbExitMyComp_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles sbExitMyComp.Click
        Me.Close()
    End Sub

    Private Sub CloseLCARS()
        If x32Handle = IntPtr.Zero OrElse Not IsWindow(x32Handle) Then
            x32Handle = ResolveX32Handle(System.Environment.CommandLine.Split("/"c))
        End If
        If x32Handle = IntPtr.Zero OrElse Not IsWindow(x32Handle) Then
            Return
        End If

        Dim myData As New COPYDATASTRUCT
        myData.dwData = 5

        Dim MyCopyData As IntPtr = System.Runtime.InteropServices.Marshal.AllocCoTaskMem(System.Runtime.InteropServices.Marshal.SizeOf(GetType(COPYDATASTRUCT)))
        System.Runtime.InteropServices.Marshal.StructureToPtr(myData, MyCopyData, False)

        Try
            ' Still SendMessage so COPYDATA memory stays valid until processed;
            ' mainscreen now BeginInvokes CloseLCARS so this returns quickly.
            SendMessage(x32Handle, WM_COPYDATA, Me.Handle, MyCopyData)
        Finally
            System.Runtime.InteropServices.Marshal.FreeCoTaskMem(MyCopyData)
        End Try
    End Sub


    Private Sub sbRestartLcars_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles sbRestartLcars.Click
        closingLCARS = True
        Me.Hide()
        MarkShellRestartPending()
        Dim previousIds As New System.Collections.Generic.List(Of Integer)
        For Each running As Process In Process.GetProcessesByName("LCARSmain")
            previousIds.Add(running.Id)
        Next
        CloseLCARS()
        Dim waitedMs As Integer = 0
        While ListedProcessesStillRunning(previousIds) AndAlso waitedMs < 8000
            Application.DoEvents()
            Threading.Thread.Sleep(100)
            waitedMs += 100
        End While
        For Each running As Process In Process.GetProcessesByName("LCARSmain")
            If previousIds.Contains(running.Id) Then
                Try
                    running.Kill()
                Catch
                End Try
            End If
        Next
        waitedMs = 0
        While ListedProcessesStillRunning(previousIds) AndAlso waitedMs < 4000
            Threading.Thread.Sleep(100)
            waitedMs += 100
        End While
        ' Winlogon relaunches a registered shell on its own. Otherwise start it here.
        Threading.Thread.Sleep(750)
        If Process.GetProcessesByName("LCARSmain").Length = 0 Then
            Try
                Dim mainExe As String = System.IO.Path.Combine(Application.StartupPath, "LCARSmain.exe")
                Dim startInfo As New ProcessStartInfo(mainExe)
                startInfo.WorkingDirectory = Application.StartupPath
                startInfo.UseShellExecute = True
                Process.Start(startInfo)
            Catch
                If Not IsLcarsRegisteredShell() AndAlso Process.GetProcessesByName("explorer").Length = 0 Then
                    Process.Start("explorer.exe")
                End If
            End Try
        End If
        End
    End Sub

    Private Shared Function ListedProcessesStillRunning(ByVal ids As System.Collections.Generic.List(Of Integer)) As Boolean
        For Each running As Process In Process.GetProcessesByName("LCARSmain")
            If ids.Contains(running.Id) Then Return True
        Next
        Return False
    End Function

    Private Shared Sub MarkShellRestartPending()
        Try
            Dim dir As String = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "LCARS x32")
            If Not System.IO.Directory.Exists(dir) Then System.IO.Directory.CreateDirectory(dir)
            System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "pending-shell-restart.flag"), DateTime.UtcNow.ToString("o"))
        Catch
        End Try
    End Sub

    Private Shared Function IsLcarsRegisteredShell() As Boolean
        Try
            Dim shellKey As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon")
            If shellKey Is Nothing Then Return False
            Dim shellCommand As String = CStr(shellKey.GetValue("Shell", "explorer.exe"))
            shellKey.Close()
            Return shellCommand.ToLower().Contains("lcarsmain")
        Catch
            Return False
        End Try
    End Function

    Private Sub sbLock_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles sbLock.Click
        Try
            Process.Start(Application.StartupPath & "\LCARSLock.exe")
        Catch ex As Exception
        End Try
    End Sub
End Class
