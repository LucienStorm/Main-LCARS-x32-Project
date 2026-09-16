' LCARSTerminal/ConsoleHostView.vb
Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Runtime.InteropServices
Imports System.Windows.Forms
Imports Microsoft.Win32

''' <summary>
''' Hosts a real Windows console (conhost) inside the LCARS tab.
''' The shell owns a genuine console — line editing, history, F7 and tab completion
''' are handled by cmd / PowerShell themselves, not by an emulated pipe layer.
''' </summary>
Public Class ConsoleHostView
    Inherits Panel

    Public Const ConsoleTitle As String = "LCARS TERMINAL"

    ''' <summary>Cell height in pixels; the tablet is small, so bigger than the console default.</summary>
    Private Const ConsoleFontHeight As Integer = 18
    Private Const ConsoleFaceName As String = "Consolas"
    ''' <summary>FF_MODERN | TMPF_VECTOR | TMPF_TRUETYPE — required for a TrueType console face.</summary>
    Private Const ConsoleFontFamily As Integer = 54
    Private Const ConsoleFontWeight As Integer = 400
    Private Const ScrollbackRows As Integer = 3000
    Private Const WindowWaitMs As Integer = 4000
    Private Const PollIntervalMs As Integer = 25
    Private Const ExitPollMs As Integer = 750

    ''' <summary>LCARS palette in COLORREF (0x00BBGGRR) order; index 7 is the shells' default text.</summary>
    Private Shared ReadOnly LcarsPalette As UInteger() = New UInteger() { _
        Rgb(0, 0, 0), Rgb(102, 102, 255), Rgb(102, 204, 153), Rgb(153, 204, 255), _
        Rgb(204, 68, 68), Rgb(204, 102, 204), Rgb(255, 204, 102), Rgb(255, 153, 0), _
        Rgb(102, 102, 102), Rgb(153, 153, 255), Rgb(153, 255, 153), Rgb(153, 221, 255), _
        Rgb(255, 102, 102), Rgb(255, 153, 204), Rgb(255, 255, 153), Rgb(255, 221, 170)}

    ''' <summary>
    ''' Kill-on-close job shared by every hosted shell. Without it a force-killed Terminal
    ''' leaves invisible orphan shells behind.
    ''' </summary>
    Private Shared _jobHandle As IntPtr = IntPtr.Zero
    Private Shared _jobResolved As Boolean

    Private _consoleHwnd As IntPtr = IntPtr.Zero
    Private _proc As Process
    ''' <summary>conhost.exe PID — owns the window and is what we kill/job-assign.</summary>
    Private _hostPid As Integer
    ''' <summary>Shell PID (cmd/powershell) — AttachConsole / WriteConsoleInput target.</summary>
    Private _pid As Integer
    Private _exitTimer As System.Windows.Forms.Timer
    Private _closing As Boolean
    Private _lastError As String = ""

    ''' <summary>Raised on the UI thread when the hosted shell exits (e.g. the user typed "exit").</summary>
    Public Event SessionExited As EventHandler

    Shared Sub New()
        ' Injection attaches this process to the shell's console; without this a Ctrl+C
        ' in that console would also signal LCARS Terminal.
        Try
            ConsoleNative.SetConsoleCtrlHandler(IntPtr.Zero, True)
        Catch
        End Try
        EnsureConsoleTheme()
    End Sub

    Private Shared Function Rgb(ByVal r As Integer, ByVal g As Integer, ByVal b As Integer) As UInteger
        Return CUInt(r) Or (CUInt(g) << 8) Or (CUInt(b) << 16)
    End Function

    ''' <summary>
    ''' Writes HKCU\Console\LCARS TERMINAL so conhost picks up LCARS colors/font at create time.
    ''' Also pins classic Conhost as the CreateProcess console host so Windows Terminal cannot
    ''' steal the window. Elevated floating consoles read the same hive.
    ''' </summary>
    Public Shared Sub EnsureConsoleTheme()
        Try
            Using key As RegistryKey = Registry.CurrentUser.CreateSubKey("Console\" & ConsoleTitle)
                If key Is Nothing Then Return
                For i As Integer = 0 To 15
                    key.SetValue("ColorTable" & i.ToString("00"), CInt(LcarsPalette(i)), RegistryValueKind.DWord)
                Next
                ' ScreenColors: low nibble = FG (7 = amber), high nibble = BG (0 = black).
                key.SetValue("ScreenColors", &H7, RegistryValueKind.DWord)
                key.SetValue("PopupColors", &HF5, RegistryValueKind.DWord)
                key.SetValue("FaceName", ConsoleFaceName, RegistryValueKind.String)
                key.SetValue("FontFamily", ConsoleFontFamily, RegistryValueKind.DWord)
                key.SetValue("FontWeight", ConsoleFontWeight, RegistryValueKind.DWord)
                ' FontSize: height in the high word, width 0 (auto) in the low word.
                key.SetValue("FontSize", ConsoleFontHeight << 16, RegistryValueKind.DWord)
                key.SetValue("HistoryBufferSize", 50, RegistryValueKind.DWord)
                key.SetValue("NumberOfHistoryBuffers", 4, RegistryValueKind.DWord)
            End Using
        Catch
        End Try
        ' {B23D10C0-E52E-411E-9D25-6606ECB14B34} = classic Conhost GUID.
        Try
            Using startup As RegistryKey = Registry.CurrentUser.CreateSubKey("Console\%%Startup")
                If startup IsNot Nothing Then
                    Const ConhostGuid As String = "{B23D10C0-E52E-411E-9D25-6606ECB14B34}"
                    startup.SetValue("DelegationConsole", ConhostGuid, RegistryValueKind.String)
                    startup.SetValue("DelegationTerminal", ConhostGuid, RegistryValueKind.String)
                End If
            End Using
        Catch
        End Try
    End Sub

    ''' <summary>
    ''' Creates (once) the job whose closure kills every hosted shell. The handle is
    ''' deliberately never closed — the OS closes it on exit, which is the trigger.
    ''' </summary>
    Private Shared Function EnsureKillOnCloseJob() As IntPtr
        If _jobResolved Then Return _jobHandle
        _jobResolved = True
        Try
            Dim h As IntPtr = ConsoleNative.CreateJobObject(IntPtr.Zero, Nothing)
            If h = IntPtr.Zero Then Return IntPtr.Zero
            Dim info As New ConsoleNative.JOBOBJECT_EXTENDED_LIMIT_INFORMATION()
            info.BasicLimitInformation.LimitFlags = ConsoleNative.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
            If Not ConsoleNative.SetInformationJobObject(h, ConsoleNative.JobObjectExtendedLimitInformation, _
                                                         info, Marshal.SizeOf(GetType(ConsoleNative.JOBOBJECT_EXTENDED_LIMIT_INFORMATION))) Then
                ConsoleNative.CloseHandle(h)
                Return IntPtr.Zero
            End If
            _jobHandle = h
        Catch
            _jobHandle = IntPtr.Zero
        End Try
        Return _jobHandle
    End Function

    Public Sub New()
        Me.BackColor = Color.Black
        Me.Dock = DockStyle.Fill
        Me.TabStop = False
        AddHandler Me.Resize, AddressOf OnHostResize
    End Sub

    Public ReadOnly Property ProcessId As Integer
        Get
            Return _pid
        End Get
    End Property

    ''' <summary>Why the last StartShell failed — empty when the last call succeeded.</summary>
    Public ReadOnly Property LastError As String
        Get
            Return If(_lastError, "")
        End Get
    End Property

    ''' <summary>
    ''' Starts a second LCARS Terminal elevated (runas). That instance can embed a real
    ''' console under LCARS chrome because it shares the shell's integrity level.
    ''' </summary>
    Public Shared Function LaunchElevatedTerminal(ByVal kind As ShellKind) As String
        EnsureConsoleTheme()

        Dim exe As String = Application.ExecutablePath
        Dim args As String = "/admin " & If(kind = ShellKind.PowerShell, "powershell", "cmd")

        Dim psi As New ProcessStartInfo()
        psi.FileName = exe
        psi.Arguments = args
        psi.UseShellExecute = True
        psi.Verb = "runas"
        Try
            Process.Start(psi)
            Return Nothing
        Catch ex As Exception
            Return ex.Message
        End Try
    End Function

    ''' <summary>
    ''' Launches the shell with CREATE_NEW_CONSOLE and adopts that console's window into this panel.
    ''' There is no pipe fallback — a False return means the tab must report the error and close.
    ''' </summary>
    Public Function StartShell(ByVal kind As ShellKind, ByVal cwd As String) As Boolean
        _lastError = ""
        If Not IsHandleCreated Then CreateControl()
        EnsureConsoleTheme()

        Dim workDir As String = cwd
        If String.IsNullOrEmpty(workDir) OrElse Not System.IO.Directory.Exists(workDir) Then
            workDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
        End If

        Dim shell() As String = ShellKindUtil.GetExeAndArgs(kind)
        Dim cmdLine As String = """" & shell(0) & """"
        If Not String.IsNullOrEmpty(shell(1)) Then cmdLine &= " " & shell(1)

        Dim si As New ConsoleNative.STARTUPINFO()
        si.cb = Marshal.SizeOf(GetType(ConsoleNative.STARTUPINFO))
        si.lpTitle = ConsoleTitle
        ' Start hidden: the console must not flash as a loose desktop window before adoption.
        si.dwFlags = ConsoleNative.STARTF_USESHOWWINDOW
        si.wShowWindow = ConsoleNative.SW_HIDE

        Dim pi As New ConsoleNative.PROCESS_INFORMATION()
        ' CREATE_NEW_CONSOLE gives the shell a genuine conhost. DelegationConsole (written by
        ' EnsureConsoleTheme) forces classic conhost even when Windows Terminal is the default.
        If Not ConsoleNative.CreateProcess(Nothing, cmdLine, IntPtr.Zero, IntPtr.Zero, False, _
                                           ConsoleNative.CREATE_NEW_CONSOLE Or ConsoleNative.CREATE_UNICODE_ENVIRONMENT, _
                                           IntPtr.Zero, workDir, si, pi) Then
            _lastError = "CreateProcess failed: " & Marshal.GetLastWin32Error().ToString() & " cmd=" & cmdLine
            Return False
        End If

        _hostPid = pi.dwProcessId
        _pid = pi.dwProcessId
        Dim job As IntPtr = EnsureKillOnCloseJob()
        If job <> IntPtr.Zero Then ConsoleNative.AssignProcessToJobObject(job, pi.hProcess)
        ConsoleNative.CloseHandle(pi.hThread)
        ConsoleNative.CloseHandle(pi.hProcess)
        Try
            _proc = Process.GetProcessById(_pid)
        Catch
            _proc = Nothing
        End Try

        _consoleHwnd = WaitForConsoleWindow(WindowWaitMs)
        If _consoleHwnd = IntPtr.Zero Then
            KillShell()
            _lastError = "Console window did not appear within " & WindowWaitMs.ToString() & " ms"
            Return False
        End If

        ConfigureConsole()
        If Not AdoptConsoleWindow() Then
            KillShell()
            _consoleHwnd = IntPtr.Zero
            _lastError = "SetParent failed — could not adopt console into LCARS tab"
            Return False
        End If

        StartExitMonitor()
        Return True
    End Function

    ''' <summary>
    ''' Attach to the shell's console and read its window handle.
    ''' </summary>
    Private Function WaitForConsoleWindow(ByVal timeoutMs As Integer) As IntPtr
        Dim deadline As DateTime = DateTime.UtcNow.AddMilliseconds(timeoutMs)
        While DateTime.UtcNow < deadline
            If _proc IsNot Nothing Then
                Try
                    If _proc.HasExited Then Return IntPtr.Zero
                Catch
                End Try
            End If
            If AttachToConsole() Then
                Dim h As IntPtr = ConsoleNative.GetConsoleWindow()
                ConsoleNative.FreeConsole()
                If h <> IntPtr.Zero Then Return h
            End If
            System.Threading.Thread.Sleep(PollIntervalMs)
        End While
        Return IntPtr.Zero
    End Function

    Private Function AttachToConsole() As Boolean
        ConsoleNative.FreeConsole()
        Return ConsoleNative.AttachConsole(CUInt(_pid))
    End Function

    Private Function AdoptConsoleWindow() As Boolean
        Dim style As Integer = ConsoleNative.GetWindowLong(_consoleHwnd, ConsoleNative.GWL_STYLE)
        Dim keepScroll As Integer = style And (ConsoleNative.WS_VSCROLL Or ConsoleNative.WS_HSCROLL)
        ConsoleNative.SetWindowLong(_consoleHwnd, ConsoleNative.GWL_EXSTYLE, 0)
        ConsoleNative.SetWindowLong(_consoleHwnd, ConsoleNative.GWL_STYLE, _
            ConsoleNative.WS_CHILD Or ConsoleNative.WS_VISIBLE Or ConsoleNative.WS_CLIPSIBLINGS Or keepScroll)
        ConsoleNative.SetParent(_consoleHwnd, Me.Handle)
        If ConsoleNative.GetParent(_consoleHwnd) <> Me.Handle Then Return False
        ConsoleNative.ShowWindow(_consoleHwnd, ConsoleNative.SW_SHOWNA)
        LayoutConsoleWindow()
        Return True
    End Function

    Private Sub LayoutConsoleWindow()
        If _consoleHwnd = IntPtr.Zero OrElse Not ConsoleNative.IsWindow(_consoleHwnd) Then Return
        ConsoleNative.SetWindowPos(_consoleHwnd, IntPtr.Zero, 0, 0, ClientSize.Width, ClientSize.Height, _
            ConsoleNative.SWP_NOZORDER Or ConsoleNative.SWP_NOACTIVATE Or ConsoleNative.SWP_FRAMECHANGED)
    End Sub

    Private Sub OnHostResize(ByVal sender As Object, ByVal e As EventArgs)
        If _consoleHwnd = IntPtr.Zero OrElse _closing Then Return
        LayoutConsoleWindow()
        ConfigureGridOnly()
    End Sub

    Private Sub ConfigureConsole()
        WithConsoleOutput(Sub(hOut As IntPtr)
                              ApplyFont(hOut)
                              ApplyPalette(hOut)
                              ApplyGridSize(hOut)
                          End Sub)
    End Sub

    Private Sub ConfigureGridOnly()
        WithConsoleOutput(Sub(hOut As IntPtr) ApplyGridSize(hOut))
    End Sub

    Private Sub WithConsoleOutput(ByVal action As Action(Of IntPtr))
        If _pid = 0 Then Return
        If Not AttachToConsole() Then Return
        Try
            Dim hOut As IntPtr = ConsoleNative.CreateFile("CONOUT$", _
                ConsoleNative.GENERIC_READ Or ConsoleNative.GENERIC_WRITE, _
                ConsoleNative.FILE_SHARE_READ Or ConsoleNative.FILE_SHARE_WRITE, _
                IntPtr.Zero, ConsoleNative.OPEN_EXISTING, 0UI, IntPtr.Zero)
            If hOut = ConsoleNative.INVALID_HANDLE_VALUE OrElse hOut = IntPtr.Zero Then Return
            Try
                action(hOut)
            Finally
                ConsoleNative.CloseHandle(hOut)
            End Try
        Catch
        Finally
            ConsoleNative.FreeConsole()
        End Try
    End Sub

    Private Shared Sub ApplyFont(ByVal hOut As IntPtr)
        Dim fi As New ConsoleNative.CONSOLE_FONT_INFOEX()
        fi.cbSize = Marshal.SizeOf(GetType(ConsoleNative.CONSOLE_FONT_INFOEX))
        If Not ConsoleNative.GetCurrentConsoleFontEx(hOut, False, fi) Then Return
        fi.cbSize = Marshal.SizeOf(GetType(ConsoleNative.CONSOLE_FONT_INFOEX))
        fi.FaceName = ConsoleFaceName
        fi.FontFamily = ConsoleFontFamily
        fi.FontWeight = ConsoleFontWeight
        fi.dwFontSize = New ConsoleNative.COORD(0S, CShort(ConsoleFontHeight))
        ConsoleNative.SetCurrentConsoleFontEx(hOut, False, fi)
    End Sub

    Private Shared Sub ApplyPalette(ByVal hOut As IntPtr)
        Dim info As ConsoleNative.CONSOLE_SCREEN_BUFFER_INFOEX = NewBufferInfo()
        If Not ConsoleNative.GetConsoleScreenBufferInfoEx(hOut, info) Then Return
        For i As Integer = 0 To 15
            info.ColorTable(i) = LcarsPalette(i)
        Next
        info.srWindow.Right = CShort(info.srWindow.Right + 1)
        info.srWindow.Bottom = CShort(info.srWindow.Bottom + 1)
        info.cbSize = Marshal.SizeOf(GetType(ConsoleNative.CONSOLE_SCREEN_BUFFER_INFOEX))
        ConsoleNative.SetConsoleScreenBufferInfoEx(hOut, info)
    End Sub

    Private Sub ApplyGridSize(ByVal hOut As IntPtr)
        Dim fi As New ConsoleNative.CONSOLE_FONT_INFOEX()
        fi.cbSize = Marshal.SizeOf(GetType(ConsoleNative.CONSOLE_FONT_INFOEX))
        If Not ConsoleNative.GetCurrentConsoleFontEx(hOut, False, fi) Then Return
        Dim cellWidth As Integer = Math.Max(1, CInt(fi.dwFontSize.X))
        Dim cellHeight As Integer = Math.Max(1, CInt(fi.dwFontSize.Y))

        Dim info As ConsoleNative.CONSOLE_SCREEN_BUFFER_INFOEX = NewBufferInfo()
        If Not ConsoleNative.GetConsoleScreenBufferInfoEx(hOut, info) Then Return

        Dim cols As Integer = Math.Max(20, Math.Min(CInt(info.dwMaximumWindowSize.X), ClientSize.Width \ cellWidth))
        Dim rows As Integer = Math.Max(5, Math.Min(CInt(info.dwMaximumWindowSize.Y), ClientSize.Height \ cellHeight))

        Dim minRect As New ConsoleNative.SMALL_RECT()
        minRect.Left = 0S
        minRect.Top = 0S
        minRect.Right = 0S
        minRect.Bottom = 0S
        ConsoleNative.SetConsoleWindowInfo(hOut, True, minRect)
        ConsoleNative.SetConsoleScreenBufferSize(hOut, _
            New ConsoleNative.COORD(CShort(cols), CShort(Math.Max(rows, ScrollbackRows))))

        Dim rect As New ConsoleNative.SMALL_RECT()
        rect.Left = 0S
        rect.Top = 0S
        rect.Right = CShort(cols - 1)
        rect.Bottom = CShort(rows - 1)
        ConsoleNative.SetConsoleWindowInfo(hOut, True, rect)
    End Sub

    Private Shared Function NewBufferInfo() As ConsoleNative.CONSOLE_SCREEN_BUFFER_INFOEX
        Dim info As New ConsoleNative.CONSOLE_SCREEN_BUFFER_INFOEX()
        info.cbSize = Marshal.SizeOf(GetType(ConsoleNative.CONSOLE_SCREEN_BUFFER_INFOEX))
        info.ColorTable = New UInteger(15) {}
        Return info
    End Function

    Public Sub FocusInput()
        If _consoleHwnd = IntPtr.Zero OrElse Not ConsoleNative.IsWindow(_consoleHwnd) Then Return
        Dim targetPid As Integer
        Dim targetThread As Integer = ConsoleNative.GetWindowThreadProcessId(_consoleHwnd, targetPid)
        Dim ownThread As Integer = ConsoleNative.GetCurrentThreadId()
        If targetThread = 0 Then Return
        Dim attached As Boolean = (targetThread = ownThread)
        If Not attached Then attached = ConsoleNative.AttachThreadInput(ownThread, targetThread, True)
        Try
            ConsoleNative.SetFocus(_consoleHwnd)
        Finally
            If attached AndAlso targetThread <> ownThread Then
                ConsoleNative.AttachThreadInput(ownThread, targetThread, False)
            End If
        End Try
    End Sub

    ''' <summary>
    ''' Always False: when the console child holds focus its keystrokes never reach WinForms,
    ''' so anything arriving on the form's KeyPreview genuinely needs to be injected.
    ''' </summary>
    Public ReadOnly Property InputHasFocus As Boolean
        Get
            Return False
        End Get
    End Property

    Public Sub ProcessExternalKeyDown(ByVal e As KeyEventArgs)
        Select Case e.KeyCode
            Case Keys.Enter, Keys.Back, Keys.Tab, Keys.Escape, Keys.Up, Keys.Down, Keys.Left, Keys.Right, _
                 Keys.Home, Keys.End, Keys.Delete, Keys.Insert, Keys.PageUp, Keys.PageDown, _
                 Keys.F1, Keys.F2, Keys.F3, Keys.F4, Keys.F5, Keys.F6, Keys.F7, Keys.F8, Keys.F9
                InjectVirtualKey(e.KeyCode)
                e.SuppressKeyPress = True
                e.Handled = True
        End Select
    End Sub

    Public Sub ProcessExternalKeyPress(ByVal e As KeyPressEventArgs)
        If Char.IsControl(e.KeyChar) Then Return
        InjectChar(e.KeyChar)
        e.Handled = True
    End Sub

    Public Sub InjectChar(ByVal ch As Char)
        If ch = ChrW(0) Then Return
        Dim vk As Short = 0S
        Dim scan As Short = ConsoleNative.VkKeyScan(ch)
        If scan <> -1S Then vk = CShort(scan And &HFFS)
        SendKeyStroke(vk, ch)
    End Sub

    Public Sub InjectVirtualKey(ByVal key As Keys)
        SendKeyStroke(CShort(CInt(key) And &HFF), ControlCharFor(key))
    End Sub

    Private Shared Function ControlCharFor(ByVal key As Keys) As Char
        Select Case key
            Case Keys.Enter
                Return ChrW(13)
            Case Keys.Back
                Return ChrW(8)
            Case Keys.Tab
                Return ChrW(9)
            Case Keys.Escape
                Return ChrW(27)
            Case Keys.Space
                Return " "c
            Case Else
                Return ChrW(0)
        End Select
    End Function

    Private Sub SendKeyStroke(ByVal virtualKey As Short, ByVal ch As Char)
        If _pid = 0 Then Return
        Dim scanCode As Short = CShort(ConsoleNative.MapVirtualKey(CInt(virtualKey), 0) And &HFFFF)

        Dim records(1) As ConsoleNative.INPUT_RECORD
        records(0).EventType = ConsoleNative.KEY_EVENT
        records(0).KeyEvent.bKeyDown = 1
        records(0).KeyEvent.wRepeatCount = 1S
        records(0).KeyEvent.wVirtualKeyCode = virtualKey
        records(0).KeyEvent.wVirtualScanCode = scanCode
        records(0).KeyEvent.UnicodeChar = ch
        records(0).KeyEvent.dwControlKeyState = 0
        records(1) = records(0)
        records(1).KeyEvent.bKeyDown = 0

        If Not AttachToConsole() Then Return
        Try
            Dim hIn As IntPtr = ConsoleNative.CreateFile("CONIN$", _
                ConsoleNative.GENERIC_READ Or ConsoleNative.GENERIC_WRITE, _
                ConsoleNative.FILE_SHARE_READ Or ConsoleNative.FILE_SHARE_WRITE, _
                IntPtr.Zero, ConsoleNative.OPEN_EXISTING, 0UI, IntPtr.Zero)
            If hIn = ConsoleNative.INVALID_HANDLE_VALUE OrElse hIn = IntPtr.Zero Then Return
            Try
                Dim written As UInteger = 0UI
                ConsoleNative.WriteConsoleInput(hIn, records, 2UI, written)
            Finally
                ConsoleNative.CloseHandle(hIn)
            End Try
        Catch
        Finally
            ConsoleNative.FreeConsole()
        End Try
    End Sub

    Private Sub StartExitMonitor()
        _exitTimer = New System.Windows.Forms.Timer()
        _exitTimer.Interval = ExitPollMs
        AddHandler _exitTimer.Tick, AddressOf OnExitTick
        _exitTimer.Start()
    End Sub

    Private Sub OnExitTick(ByVal sender As Object, ByVal e As EventArgs)
        If _closing Then Return
        Dim gone As Boolean = False
        Try
            gone = (_proc Is Nothing) OrElse _proc.HasExited
        Catch
            gone = True
        End Try
        If Not gone Then Return
        StopExitMonitor()
        RaiseEvent SessionExited(Me, EventArgs.Empty)
    End Sub

    Private Sub StopExitMonitor()
        If _exitTimer Is Nothing Then Return
        Try
            _exitTimer.Stop()
            RemoveHandler _exitTimer.Tick, AddressOf OnExitTick
            _exitTimer.Dispose()
        Catch
        End Try
        _exitTimer = Nothing
    End Sub

    Public Sub Detach()
        If _closing Then Return
        _closing = True
        StopExitMonitor()
        If _consoleHwnd <> IntPtr.Zero AndAlso ConsoleNative.IsWindow(_consoleHwnd) Then
            Try
                ConsoleNative.ShowWindow(_consoleHwnd, 0)
                ConsoleNative.SetParent(_consoleHwnd, IntPtr.Zero)
            Catch
            End Try
        End If
        _consoleHwnd = IntPtr.Zero
        KillShell()
    End Sub

    Private Sub KillShell()
        ' Prefer killing conhost — that tears down the shell child with it.
        Dim killPid As Integer = If(_hostPid <> 0, _hostPid, _pid)
        Try
            If killPid <> 0 Then
                Dim p As Process = Process.GetProcessById(killPid)
                If Not p.HasExited Then p.Kill()
                p.Dispose()
            End If
        Catch
        End Try
        Try
            If _proc IsNot Nothing Then _proc.Dispose()
        Catch
        End Try
        _proc = Nothing
        _pid = 0
        _hostPid = 0
    End Sub

    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        If disposing Then Detach()
        MyBase.Dispose(disposing)
    End Sub
End Class
