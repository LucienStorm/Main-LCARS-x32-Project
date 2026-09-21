Option Strict On

Imports System.IO
Imports System.Runtime.InteropServices
Imports LCARS
Imports LCARS.x32
Imports LCARS.Controls

public Class modBusiness

#Region " Structures "
    Public Structure UserButtonInfo
        Dim color As String
        Dim Name As String
        Dim Location As String
    End Structure
#End Region

#Region " Global Variables "

#Region " Public Global Variables "
    'Required components
    Public myForm As Form
    Public myMainPanel As Panel
    Public myModeSelect As LCARS.LCARSbuttonClass

    'Programs list components
    Public ProgramsPanel As LCARS.Controls.WindowlessContainer
    Public myProgramPagesDisplay As LCARS.LCARSbuttonClass
    Public myProgsUp As LCARS.LCARSbuttonClass
    Public myProgsBack As LCARS.LCARSbuttonClass
    Public myProgsNext As LCARS.LCARSbuttonClass

    'Userbuttons components
    Public UserButtonsPanel As LCARS.Controls.ButtonGrid
    Public myButtonManager As LCARS.LCARSbuttonClass
    Public myUserButtons As LCARS.LCARSbuttonClass
    Private myUserButtonEndcap As LCARS.LCARSbuttonClass

    'Taskbar components
    Public myAppsPanel As Panel
    Public myTrayPanel As Panel
    Public myShowTrayButton As LCARSbuttonClass
    Public myHideTrayButton As LCARSbuttonClass

    'Power monitor components
    Public bars() As LCARSbuttonClass
    Public myBattPercent As Control
    Public myPowerSource As Control
    Public myBattPanel As Panel

    'Common Buttons
    Public myStartMenu As LCARSbuttonClass
    Public myComputer As LCARSbuttonClass
    Public mySettings As LCARSbuttonClass
    Public myEngineering As LCARSbuttonClass
    Public myDeactivate As LCARSbuttonClass
    Public myAlert As LCARSbuttonClass
    Public myDestruct As LCARSbuttonClass
    Public myClock As Control
    Public myWeather As Control
    Public myPhoto As LCARSbuttonClass
    Public myWebBrowser As LCARSbuttonClass
    Public myTerminal As LCARSbuttonClass
    Public myDocuments As LCARSbuttonClass
    Public myPictures As LCARSbuttonClass
    Public myVideos As LCARSbuttonClass
    Public myMusic As LCARSbuttonClass
    Public myOSK As LCARSbuttonClass
    Public mySpeech As LCARSbuttonClass
    Public myHelp As LCARSbuttonClass
    Public myRun As LCARSbuttonClass
    Public myAlertListButton As LCARSbuttonClass
    Public myDesktopFiles As LCARSbuttonClass
    Public myNetworkPlaces As LCARSbuttonClass

    'Public state
    'TODO: Find a better way to handle this
    Public myUserButtonCollection As New List(Of UserButtonInfo)
#End Region

#Region " Private Global Variables "
    'State
    Dim _screenIndex As Integer
    Dim _isInit As Boolean = False
    Private personalProgramsLayoutBusy As Boolean = False
    Private updateRegionDepth As Integer = 0
    Dim _hasProgramsList As Boolean = False
    Dim _hasUserButtons As Boolean = False
    Dim _hasTaskbar As Boolean = False
    Dim _hasPowerMonitor As Boolean = False
    Dim _hasClock As Boolean = False
    Dim _hasWeather As Boolean = False
    Dim _hasSpeechIndicator As Boolean = False

    'Program Pages
    Dim MyPrograms As DirectoryStartItem
    Dim ClassicPrograms As DirectoryStartItem
    Dim ProgDir As New List(Of Integer)
    Dim ProgPageSize As Integer
    Dim curProgPage As Integer = 1
    Dim pageCount As Integer
    Dim curProgIndex As Integer
    Dim startEditMode As Boolean = False
    Dim btnStartEdit As LCARS.LCARSbuttonClass = Nothing
    Dim btnStartMode As LCARS.LCARSbuttonClass = Nothing
    Dim startSelectedPinIndex As Integer = -1
    Dim startDragPinIndex As Integer = -1
    Dim startDragOrigin As Point = Point.Empty
    Dim startDragging As Boolean = False
    Dim startDragHandlersWired As Boolean = False
    Dim startDragGhost As Label = Nothing
    Dim startDropMarker As Label = Nothing
    Dim startDragLabel As String = ""

    'External application management
    Dim leftArrow As ArrowButton
    Dim rightArrow As ArrowButton
    Dim windowMap As New Dictionary(Of ExternalApp, TaskbarItem)()
    Dim taskbarList As New List(Of TaskbarItem)()
    Dim taskbarOffset As Integer = 0

    'On Screen Keyboard (OSK)
    Dim OSKproc As Process = Nothing
    Dim OSKhwnd As IntPtr = IntPtr.Zero
    Dim isVisible As Boolean = False
    Dim oskPrewarmStarted As Boolean = False

    'Autohide
    Dim WithEvents tmrAutohide As New Timer()
    Dim autohide As IAutohide.AutoHideModes
    Dim hideCount As Integer = 0
#End Region

#End Region

    Public Sub New(ByVal screenIndex As Integer)
        _screenIndex = screenIndex
        AddHandler SpeechEnableChanged, AddressOf Me.speech_EnableChanged
    End Sub

#Region " Properties "
    Public ReadOnly Property ScreenIndex() As Integer
        Get
            Return _screenIndex
        End Get
    End Property

    Public ReadOnly Property isInit() As Boolean
        Get
            Return _isInit
        End Get
    End Property

    Public ReadOnly Property hasProgramsList() As Boolean
        Get
            Return _hasProgramsList
        End Get
    End Property

    Public ReadOnly Property hasUserButtons() As Boolean
        Get
            Return _hasUserButtons
        End Get
    End Property

    Public ReadOnly Property hasTaskbar() As Boolean
        Get
            Return _hasTaskbar
        End Get
    End Property

    Public ReadOnly Property hasPowerMonitor() As Boolean
        Get
            Return _hasPowerMonitor
        End Get
    End Property

    Public ReadOnly Property hasClock() As Boolean
        Get
            Return _hasClock
        End Get
    End Property

    Public ReadOnly Property hasWeather() As Boolean
        Get
            Return _hasWeather
        End Get
    End Property

    Public ReadOnly Property hasSpeechIndicator() As Boolean
        Get
            Return _hasSpeechIndicator
        End Get
    End Property
#End Region

    Public Sub ShutdownScreen()
        tmrAutohide.Stop()
        ReturnTray(Me)
        _isInit = False
        ' Do not DoEvents here — nested inside WM_COPYDATA/SendMessage it can deadlock shutdown.
        If Not myForm Is Nothing Then
            DeregisterAlertForm(myForm)
            myForm.Dispose()
        End If
    End Sub

    Public Sub myStartMenu_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        ' Start menu layout is screen-specific; handled on each mainscreen form.
    End Sub

    Friend Sub TraceStartMenuMouseDown(ByVal sender As Object, ByVal e As MouseEventArgs)
        modDiagnostics.LogTouch("StartMenu", "MouseDown screen=" & ScreenIndex & " btn=" & If(myStartMenu Is Nothing, "null", myStartMenu.Name))
    End Sub

    Friend Sub TracePersonalProgramsMouseDown(ByVal sender As Object, ByVal e As MouseEventArgs)
        modDiagnostics.LogTouch("PersonalPrograms", "MouseDown screen=" & ScreenIndex & " btn=" & If(myUserButtons Is Nothing, "null", myUserButtons.Name))
    End Sub

    Public Sub myCompButton_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim myProcess As New Process()
        myProcess.StartInfo.FileName = Application.StartupPath & "\LCARSexplorer.exe"
        launchProcessOnScreen(myProcess)
    End Sub

    Public Sub mySettingsButton_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim mySettings As New frmSettings()
        MoveToScreen(mySettings.Handle)
        mySettings.Show()
    End Sub

    Public Sub myEngineeringButton_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim myProcess As New Process()
        myProcess.StartInfo.FileName = Application.StartupPath & "\LCARSengineering.exe"
        launchProcessOnScreen(myProcess)
    End Sub

    Public Sub myModeSelectButton_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        'Check that the images directory exists. If not, create it.
        If Not Directory.Exists(System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "\LCARS x32\Images") Then
            Directory.CreateDirectory(System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "\LCARS x32\Images")
        End If
        'Save screenshot and show the selection form
        Dim screenImage As New Bitmap(myForm.Width, myForm.Height)
        Dim g As System.Drawing.Graphics = System.Drawing.Graphics.FromImage(screenImage)
        g.CopyFromScreen(myForm.PointToScreen(Point.Empty), Point.Empty, myForm.Size)
        Try
            screenImage.Save(System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "\LCARS x32\Images\" & myForm.Name.ToLower() & "_" & ScreenIndex & ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg)
        Catch ex As Exception
            MsgBox("Error saving image for interface " & myForm.Name & " on screen " & ScreenIndex & ".")
        End Try
        Dim myChoice As New ScreenChooserDialog(ScreenIndex)
        MoveToScreen(myChoice.Handle)
        myChoice.Show()
    End Sub

    Public Sub myDeactivateButton_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        ' Keep registry handle fresh — stale MainWindowHandle makes LCARSshutdown SendMessage hang.
        If myDesktop IsNot Nothing AndAlso Not myDesktop.IsDisposed Then
            SaveSetting("LCARS x32", "Application", "MainWindowHandle", myDesktop.Handle.ToString())
        End If
        Dim myProcess As New Process()
        myProcess.StartInfo.FileName = Application.StartupPath & "\LCARSshutdown.exe"
        myProcess.StartInfo.Arguments = "/" & myDesktop.Handle.ToString()
        launchProcessOnScreen(myProcess)
    End Sub

    Public Sub myAlertButton_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        If AlertActive Then
            CancelAlert()
        Else
            GeneralAlert(0)
        End If
    End Sub

    Public Sub myYellowAlertButton_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        If AlertActive Then
            CancelAlert()
        Else
            GeneralAlert(1)
        End If
    End Sub

    Public Sub myDestructButton_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim myProcess As New Process()
        myProcess.StartInfo.FileName = Application.StartupPath & "\LCARSdestruct.exe"
        launchProcessOnScreen(myProcess)
    End Sub

    Public Sub myPhoto_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim myProcess As New Process()
        myProcess.StartInfo.FileName = Application.StartupPath & "\LCARSmedia.exe"
        launchProcessOnScreen(myProcess)
    End Sub

    Public Sub myWebBrowser_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim myProcess As New Process()
        myProcess.StartInfo.FileName = Application.StartupPath & "\LCARSWebBrowser.exe"
        launchProcessOnScreen(myProcess)
    End Sub

    Public Sub myTerminal_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim myProcess As New Process()
        myProcess.StartInfo.FileName = Application.StartupPath & "\LCARSTerminal.exe"
        launchProcessOnScreen(myProcess)
    End Sub

    Public Sub myButtonManager_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Static myUserButtons As New frmManageButtons(Me)
        If myUserButtons.IsDisposed Then
            myUserButtons = New frmManageButtons(Me)
        End If
        myUserButtons.Show()
    End Sub

    Public Sub TogglePersonalPrograms(ByVal opening As Boolean, ByVal panelWidth As Integer)
        If UserButtonsPanel Is Nothing OrElse myMainPanel Is Nothing Then
            modDiagnostics.LogWarn("modBusiness.TogglePersonalPrograms", "missing panel components")
            Return
        End If
        If myForm Is Nothing OrElse myForm.IsDisposed Then Return

        modDiagnostics.LogTouch("PersonalPrograms", "TogglePersonalPrograms queued screen=" & ScreenIndex & " opening=" & opening)
        myForm.BeginInvoke(New MethodInvoker(Sub()
                                                 ApplyPersonalProgramsToggle(opening, panelWidth)
                                             End Sub))
    End Sub

    Private Sub ApplyPersonalProgramsToggle(ByVal opening As Boolean, ByVal panelWidth As Integer)
        Using diag As DiagnosticScope = modDiagnostics.BeginScope("modBusiness.ApplyPersonalProgramsToggle",
            "screen=" & ScreenIndex & " opening=" & opening & " panelWidth=" & panelWidth)
            personalProgramsLayoutBusy = True
            Try
                Dim delta As Integer = panelWidth + 6
                If delta > 0 Then
                    If opening Then
                        myMainPanel.Width = Math.Max(100, myMainPanel.Width - delta)
                    Else
                        myMainPanel.Width += delta
                    End If
                End If

                UserButtonsPanel.Visible = opening
                If myButtonManager IsNot Nothing Then
                    myButtonManager.Visible = opening
                End If
                modDiagnostics.LogInfo("modBusiness.ApplyPersonalProgramsToggle",
                    "panelVisible=" & UserButtonsPanel.Visible &
                    " pnlMain=" & myMainPanel.Bounds.ToString())
            Catch ex As Exception
                modDiagnostics.LogException("modBusiness.ApplyPersonalProgramsToggle", ex, "screen=" & ScreenIndex)
                Throw
            Finally
                personalProgramsLayoutBusy = False
                If isInit Then
                    UpdateRegion()
                End If
            End Try
        End Using
    End Sub

    Public Sub myDocuments_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        LaunchMediaExplorer(MediaFolderKind.Documents)
    End Sub

    Public Sub myPictures_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        LaunchMediaExplorer(MediaFolderKind.Pictures)
    End Sub

    Public Sub myVideos_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        LaunchMediaExplorer(MediaFolderKind.Videos)
    End Sub

    Public Sub myDesktopFiles_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim myProcess As New Process()
        myProcess.StartInfo.FileName = Application.StartupPath & "\LCARSexplorer.exe"
        myProcess.StartInfo.Arguments = """" & System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop) & """"
        launchProcessOnScreen(myProcess)
    End Sub

    Public Sub myMusic_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        LaunchMediaExplorer(MediaFolderKind.Music)
    End Sub

    Public Sub myNetworkPlaces_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim myProcess As New Process()
        myProcess.StartInfo.FileName = Application.StartupPath & "\LCARSexplorer.exe"
        myProcess.StartInfo.Arguments = "NETWORK:"
        launchProcessOnScreen(myProcess)
    End Sub

    Private Sub LaunchMediaExplorer(ByVal kind As MediaFolderKind)
        Dim path As String = GetMediaFolderPath(kind)
        If String.IsNullOrWhiteSpace(path) Then
            LCARS.UI.MsgBox("No folder path is set for this button. Set it in Settings → SCREEN-SPECIFIC → Folders.", MsgBoxStyle.OkOnly, "ERROR:")
            Return
        End If
        If Not Directory.Exists(path) Then
            ' UNC may fail Exists when offline — still attempt launch; explorer/auth handles denial.
            If Not path.StartsWith("\\") Then
                LCARS.UI.MsgBox("Folder not found:" & vbCrLf & path, MsgBoxStyle.OkOnly, "ERROR:")
                Return
            End If
        End If
        Dim myProcess As New Process()
        myProcess.StartInfo.FileName = Application.StartupPath & "\LCARSexplorer.exe"
        myProcess.StartInfo.Arguments = """" & path & """"
        launchProcessOnScreen(myProcess)
    End Sub

    Public Sub myOSK_Click(ByVal Sender As Object, ByVal e As System.EventArgs)
        Dim alreadyRunning As Boolean = False
        Try
            Dim procs() As Process = Process.GetProcessesByName("OnScreenKeyboard")
            alreadyRunning = (procs IsNot Nothing AndAlso procs.Length > 0)
        Catch
        End Try

        ' First press with nothing running: start visible and leave it up (do not toggle-hide).
        If Not alreadyRunning Then
            EnsureOskProcess(startHidden:=False)
            Dim fresh As IntPtr = ResolveOskHwnd(waitMs:=3000)
            If fresh = IntPtr.Zero Then
                modDiagnostics.LogWarn("modBusiness.myOSK_Click", "OSK failed to start")
                Return
            End If
            SendOskCommand(fresh, show:=True)
            ShowWindow(fresh, SW_SHOWNOACTIVATE)
            isVisible = True
            Return
        End If

        EnsureOskProcess(startHidden:=False)
        Dim hwnd As IntPtr = ResolveOskHwnd(waitMs:=2500)
        If hwnd = IntPtr.Zero Then
            RestartOskVisible()
            hwnd = ResolveOskHwnd(waitMs:=3000)
        End If
        If hwnd = IntPtr.Zero Then
            modDiagnostics.LogWarn("modBusiness.myOSK_Click", "OSK HWND not found after restart")
            Return
        End If

        ' Minimized (old Hide button) still reports IsWindowVisible=True — treat as hidden.
        Dim currentlyVisible As Boolean = False
        Dim isMinimized As Boolean = False
        Try
            Dim hwndInt As Integer = CInt(hwnd.ToInt32())
            isMinimized = IsIconic(hwndInt)
            currentlyVisible = IsWindowVisible(hwndInt) AndAlso Not isMinimized
        Catch
            currentlyVisible = isVisible
        End Try

        If currentlyVisible Then
            SendOskCommand(hwnd, show:=False)
            ShowWindow(hwnd, SW_HIDE)
            isVisible = False
        Else
            If isMinimized Then ShowWindow(hwnd, SW_RESTORE)
            ' Synchronous so WinForms Visible=True before we re-check.
            SendOskCommand(hwnd, show:=True)
            ShowWindow(hwnd, SW_SHOWNOACTIVATE)
            isVisible = True
            Threading.Thread.Sleep(50)
            Dim stillHidden As Boolean = False
            Try
                Dim hwndInt As Integer = CInt(hwnd.ToInt32())
                stillHidden = (Not IsWindowVisible(hwndInt)) OrElse IsIconic(hwndInt)
            Catch
            End Try
            If stillHidden Then
                ' Last resort — do not loop: one clean visible restart.
                RestartOskVisible()
                hwnd = ResolveOskHwnd(waitMs:=3000)
                If hwnd <> IntPtr.Zero Then
                    SendOskCommand(hwnd, show:=True)
                    ShowWindow(hwnd, SW_SHOWNOACTIVATE)
                    isVisible = True
                End If
            End If
        End If
    End Sub

    Private Shared ReadOnly WM_LCARS_OSK_CMD As Integer = NativeRegisterWindowMessage("LCARS_OSK_CMD")

    Private Declare Auto Function NativeRegisterWindowMessage Lib "user32.dll" Alias "RegisterWindowMessageA" (ByVal lpString As String) As Integer

    ''' <summary>PostMessage so a slow/blocked OSK UI thread cannot hang the shell on SendMessage.</summary>
    Private Sub SendOskCommand(ByVal hwnd As IntPtr, ByVal show As Boolean)
        If hwnd = IntPtr.Zero OrElse WM_LCARS_OSK_CMD = 0 Then Return
        Try
            PostMessage(hwnd, CUInt(WM_LCARS_OSK_CMD), New IntPtr(If(show, 1, 0)), IntPtr.Zero)
        Catch
            Try
                SendMessage(hwnd, CUInt(WM_LCARS_OSK_CMD), If(show, 1, 0), IntPtr.Zero)
            Catch
            End Try
        End Try
    End Sub

    Private Sub RestartOskVisible()
        Try
            Dim procs() As Process = Process.GetProcessesByName("OnScreenKeyboard")
            If procs IsNot Nothing Then
                For Each p As Process In procs
                    Try
                        If Not p.HasExited Then p.Kill()
                    Catch
                    End Try
                    Try
                        p.Dispose()
                    Catch
                    End Try
                Next
            End If
        Catch
        End Try
        OSKproc = Nothing
        OSKhwnd = IntPtr.Zero
        isVisible = False
        Threading.Thread.Sleep(200)
        EnsureOskProcess(startHidden:=False)
    End Sub

    ''' <summary>
    ''' Attach to a running OSK or start one. Pre-warm uses --hidden so Load work is done once.
    ''' </summary>
    Private Sub EnsureOskProcess(ByVal startHidden As Boolean)
        Try
            If modShellFallback.IsUpdateInProgress() Then
                modDiagnostics.LogInfo("modBusiness.EnsureOskProcess", "skipped — update-in-progress.flag present")
                Return
            End If
            If TryAttachExistingOsk() Then Return

            Dim exePath As String = System.IO.Path.Combine(Application.StartupPath, "OnScreenKeyboard.exe")
            If Not System.IO.File.Exists(exePath) Then
                modDiagnostics.LogWarn("modBusiness.EnsureOskProcess", "missing " & exePath)
                Return
            End If

            Dim psi As New ProcessStartInfo()
            psi.FileName = exePath
            psi.WorkingDirectory = Application.StartupPath
            psi.UseShellExecute = True
            If startHidden Then
                psi.Arguments = "--hidden"
            End If
            OSKproc = Process.Start(psi)
            OSKhwnd = IntPtr.Zero
            isVisible = Not startHidden
            ResolveOskHwnd(waitMs:=If(startHidden, 5000, 2500))
        Catch ex As Exception
            modDiagnostics.LogException("modBusiness.EnsureOskProcess", ex, "hidden=" & startHidden.ToString())
        End Try
    End Sub

    ''' <summary>
    ''' Prefer an already-running OSK (warm or elevate-restarted) over spawning another.
    ''' If the on-disk exe is newer than the running process, kill it — otherwise updates
    ''' leave an old OSK in memory and scale/size fixes never appear.
    ''' </summary>
    Private Function TryAttachExistingOsk() As Boolean
        KillStaleOskProcesses()

        Try
            If OSKproc IsNot Nothing Then
                Dim alive As Boolean = False
                Try
                    alive = (OSKproc.Id > 0) AndAlso (Not OSKproc.HasExited)
                Catch
                    alive = False
                End Try
                If alive Then
                    ResolveOskHwnd(waitMs:=500)
                    Return True
                End If
            End If
        Catch
            ' Process handle may be invalid — fall through to scan.
        End Try

        Dim procs() As Process = Process.GetProcessesByName("OnScreenKeyboard")
        If procs Is Nothing OrElse procs.Length = 0 Then Return False

        OSKproc = procs(0)
        For i As Integer = 1 To procs.Length - 1
            Try
                procs(i).Dispose()
            Catch
            End Try
        Next
        OSKhwnd = IntPtr.Zero
        ResolveOskHwnd(waitMs:=1000)
        Return True
    End Function

    ''' <summary>
    ''' Kill OnScreenKeyboard processes that started before the current exe was written.
    ''' </summary>
    Private Sub KillStaleOskProcesses()
        Try
            Dim exePath As String = System.IO.Path.Combine(Application.StartupPath, "OnScreenKeyboard.exe")
            If Not System.IO.File.Exists(exePath) Then Return
            Dim exeUtc As DateTime = System.IO.File.GetLastWriteTimeUtc(exePath)
            Dim procs() As Process = Process.GetProcessesByName("OnScreenKeyboard")
            If procs Is Nothing Then Return
            Dim killedAny As Boolean = False
            For Each p As Process In procs
                Dim stale As Boolean = False
                Try
                    stale = p.StartTime.ToUniversalTime() < exeUtc.AddSeconds(-2)
                Catch
                    stale = True
                End Try
                If stale Then
                    Try
                        If Not p.HasExited Then p.Kill()
                        killedAny = True
                    Catch
                    End Try
                End If
                Try
                    p.Dispose()
                Catch
                End Try
            Next
            If killedAny Then
                OSKproc = Nothing
                OSKhwnd = IntPtr.Zero
                Threading.Thread.Sleep(250)
                modDiagnostics.LogInfo("modBusiness.KillStaleOskProcesses", "killed OSK older than " & exeUtc.ToString("s") & "Z")
            End If
        Catch ex As Exception
            modDiagnostics.LogException("modBusiness.KillStaleOskProcesses", ex, "")
        End Try
    End Sub

    Private Delegate Function EnumWindowsProc(ByVal hWnd As IntPtr, ByVal lParam As IntPtr) As Boolean
    Private Declare Auto Function EnumWindows Lib "user32" (ByVal lpEnumFunc As EnumWindowsProc, ByVal lParam As IntPtr) As Boolean
    Private Declare Auto Function GetWindowThreadProcessId Lib "user32" (ByVal hWnd As IntPtr, ByRef lpdwProcessId As Integer) As Integer
    ' Must be Unicode (W). Declare Auto + Alias GetWindowTextA garbles the title on NT,
    ' so IsOskFormHwnd never matched and the shell could not show/hide the OSK.
    Private Declare Unicode Function GetWindowTextW Lib "user32" (ByVal hWnd As IntPtr, ByVal lpString As System.Text.StringBuilder, ByVal nMaxCount As Integer) As Integer
    Private Declare Unicode Function GetClassNameW Lib "user32" (ByVal hWnd As IntPtr, ByVal lpClassName As System.Text.StringBuilder, ByVal nMaxCount As Integer) As Integer
    Private Shared _enumOskPid As Integer
    Private Shared _enumOskHwnd As IntPtr
    Private Const OskWindowTitle As String = "On Screen Keyboard"

    ''' <summary>
    ''' True only for the real OSK form — never WinForms .NET-BroadcastEventWindow junk.
    ''' FindWindow cannot see this window (WS_EX_TOOLWINDOW); resolve via PID enum instead.
    ''' </summary>
    Private Function IsOskFormHwnd(ByVal hWnd As IntPtr) As Boolean
        If hWnd = IntPtr.Zero OrElse Not IsWindow(hWnd) Then Return False
        Try
            Dim cls As New System.Text.StringBuilder(260)
            GetClassNameW(hWnd, cls, cls.Capacity)
            Dim className As String = cls.ToString()
            If className.IndexOf("BroadcastEventWindow", StringComparison.OrdinalIgnoreCase) >= 0 Then Return False
            If className.IndexOf("IME", StringComparison.OrdinalIgnoreCase) >= 0 Then Return False
            If className.IndexOf("GDI+", StringComparison.OrdinalIgnoreCase) >= 0 Then Return False

            Dim sb As New System.Text.StringBuilder(260)
            GetWindowTextW(hWnd, sb, sb.Capacity)
            Dim title As String = sb.ToString()
            If String.Equals(title, OskWindowTitle, StringComparison.OrdinalIgnoreCase) Then Return True

            ' Fallback: main WinForms window of OnScreenKeyboard (title can lag on first create).
            If className.IndexOf("WindowsForms10.Window", StringComparison.OrdinalIgnoreCase) >= 0 Then
                Dim pid As Integer = 0
                GetWindowThreadProcessId(hWnd, pid)
                If pid > 0 Then
                    Using p As Process = Process.GetProcessById(pid)
                        Return String.Equals(p.ProcessName, "OnScreenKeyboard", StringComparison.OrdinalIgnoreCase)
                    End Using
                End If
            End If
            Return False
        Catch
            Return False
        End Try
    End Function

    Private Shared Function EnumOskWindowCallback(ByVal hWnd As IntPtr, ByVal lParam As IntPtr) As Boolean
        Dim pid As Integer = 0
        GetWindowThreadProcessId(hWnd, pid)
        If pid <> _enumOskPid Then Return True

        Dim cls As New System.Text.StringBuilder(260)
        GetClassNameW(hWnd, cls, cls.Capacity)
        Dim className As String = cls.ToString()
        If className.IndexOf("BroadcastEventWindow", StringComparison.OrdinalIgnoreCase) >= 0 Then Return True
        If className.IndexOf("IME", StringComparison.OrdinalIgnoreCase) >= 0 Then Return True
        If className.IndexOf("GDI+", StringComparison.OrdinalIgnoreCase) >= 0 Then Return True

        Dim sb As New System.Text.StringBuilder(260)
        GetWindowTextW(hWnd, sb, sb.Capacity)
        Dim title As String = sb.ToString()
        If String.Equals(title, OskWindowTitle, StringComparison.OrdinalIgnoreCase) OrElse _
           className.IndexOf("WindowsForms10.Window", StringComparison.OrdinalIgnoreCase) >= 0 Then
            _enumOskHwnd = hWnd
            Return False
        End If
        Return True
    End Function

    Private Function FindOskHwndByProcessId(ByVal pid As Integer) As IntPtr
        If pid <= 0 Then Return IntPtr.Zero
        _enumOskPid = pid
        _enumOskHwnd = IntPtr.Zero
        Try
            EnumWindows(AddressOf EnumOskWindowCallback, IntPtr.Zero)
        Catch
        End Try
        Return _enumOskHwnd
    End Function

    ''' <summary>
    ''' Resolve OSK form HWND only. MainWindowHandle/Enum can hit .NET-BroadcastEventWindow —
    ''' never ShowWindow that or it appears as ".Net Broadcast" on the taskbar.
    ''' Do not use FindWindow — WS_EX_TOOLWINDOW makes FindWindow always miss this form.
    ''' </summary>
    Private Function ResolveOskHwnd(Optional ByVal waitMs As Integer = 0) As IntPtr
        If IsOskFormHwnd(OSKhwnd) Then
            Return OSKhwnd
        End If
        OSKhwnd = IntPtr.Zero

        Dim deadline As DateTime = DateTime.UtcNow.AddMilliseconds(Math.Max(0, waitMs))
        Do
            Try
                If OSKproc IsNot Nothing AndAlso Not OSKproc.HasExited Then
                    OSKproc.Refresh()
                    Dim mainHwnd As IntPtr = OSKproc.MainWindowHandle
                    If IsOskFormHwnd(mainHwnd) Then
                        OSKhwnd = mainHwnd
                        Return OSKhwnd
                    End If
                    Dim byPid As IntPtr = FindOskHwndByProcessId(OSKproc.Id)
                    If byPid <> IntPtr.Zero AndAlso IsOskFormHwnd(byPid) Then
                        OSKhwnd = byPid
                        Return OSKhwnd
                    End If
                Else
                    ' No tracked process — scan by process name.
                    Dim procs() As Process = Process.GetProcessesByName("OnScreenKeyboard")
                    If procs IsNot Nothing AndAlso procs.Length > 0 Then
                        OSKproc = procs(0)
                        Dim byPid As IntPtr = FindOskHwndByProcessId(OSKproc.Id)
                        If byPid <> IntPtr.Zero AndAlso IsOskFormHwnd(byPid) Then
                            OSKhwnd = byPid
                            Return OSKhwnd
                        End If
                    End If
                End If
            Catch
            End Try

            If DateTime.UtcNow >= deadline Then Exit Do
            Threading.Thread.Sleep(50)
            Application.DoEvents()
        Loop

        Return IntPtr.Zero
    End Function

    ''' <summary>
    ''' After primary screen init, spawn OSK hidden once so the OSK button is warm show/hide.
    ''' Always kills any existing OSK first so an in-memory process cannot keep old scale code
    ''' after an update. Within a session, show/hide still stays warm/fast.
    ''' </summary>
    Private Sub PrewarmOskIfNeeded()
        If oskPrewarmStarted Then Return
        If ScreenIndex <> 0 Then Return
        If modShellFallback.IsUpdateInProgress() Then
            modDiagnostics.LogInfo("modBusiness.PrewarmOskIfNeeded", "skipped — update-in-progress.flag present")
            Return
        End If
        oskPrewarmStarted = True
        Try
            KillAllOskProcesses()

            Dim exePath As String = System.IO.Path.Combine(Application.StartupPath, "OnScreenKeyboard.exe")
            If Not System.IO.File.Exists(exePath) Then
                modDiagnostics.LogWarn("modBusiness.PrewarmOskIfNeeded", "missing " & exePath)
                Return
            End If

            Dim psi As New ProcessStartInfo()
            psi.FileName = exePath
            psi.WorkingDirectory = Application.StartupPath
            psi.UseShellExecute = True
            psi.Arguments = "--hidden"
            OSKproc = Process.Start(psi)
            OSKhwnd = IntPtr.Zero
            isVisible = False
            modDiagnostics.LogInfo("modBusiness.PrewarmOskIfNeeded", "OSK pre-warm process started pid=" & _
                                   If(OSKproc Is Nothing, 0, OSKproc.Id).ToString())

            ' Resolve HWND without freezing chrome: poll a few times via timer.
            Dim resolveAttempts As Integer = 0
            Dim tmr As New Timer()
            tmr.Interval = 200
            AddHandler tmr.Tick, Sub(s As Object, args As EventArgs)
                                     resolveAttempts += 1
                                     Dim hwnd As IntPtr = ResolveOskHwnd(waitMs:=0)
                                     If hwnd <> IntPtr.Zero OrElse resolveAttempts >= 25 Then
                                         tmr.Stop()
                                         tmr.Dispose()
                                         If hwnd <> IntPtr.Zero Then
                                             modDiagnostics.LogInfo("modBusiness.PrewarmOskIfNeeded", "OSK HWND ready")
                                         Else
                                             modDiagnostics.LogWarn("modBusiness.PrewarmOskIfNeeded", "OSK HWND not found after start")
                                         End If
                                     End If
                                 End Sub
            tmr.Start()
        Catch ex As Exception
            modDiagnostics.LogException("modBusiness.PrewarmOskIfNeeded", ex, "")
        End Try
    End Sub

    ''' <summary>
    ''' Kill every OnScreenKeyboard process so the next start loads the on-disk exe.
    ''' </summary>
    Private Sub KillAllOskProcesses()
        Try
            Dim procs() As Process = Process.GetProcessesByName("OnScreenKeyboard")
            If procs Is Nothing Then Return
            For Each p As Process In procs
                Try
                    If Not p.HasExited Then p.Kill()
                Catch
                End Try
                Try
                    p.Dispose()
                Catch
                End Try
            Next
            OSKproc = Nothing
            OSKhwnd = IntPtr.Zero
            Threading.Thread.Sleep(300)
        Catch ex As Exception
            modDiagnostics.LogException("modBusiness.KillAllOskProcesses", ex, "")
        End Try
    End Sub

    Private Sub speech_EnableChanged(ByVal sender As Object, ByVal e As EventArgs)
        If isInit And hasSpeechIndicator Then
            mySpeech.Lit = modSpeech.SpeechEnabled
        End If
    End Sub

    Public Sub mySpeech_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        If console.Visible Then
            If MonitorFromWindow(myForm.Handle, MONITOR_DEFAULTTONEAREST) = MonitorFromWindow(console.Handle, MONITOR_DEFAULTTONEAREST) Then
                console.Hide()
            Else
                MoveToScreen(console.Handle)
            End If
        Else
            modSpeech.ShowConsole()
            MoveToScreen(console.Handle)
        End If
    End Sub

    Public Sub myHelp_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim myProcess As New Process()
        myProcess.StartInfo.FileName = Application.StartupPath & "\Lcarsx32 Manual.exe"
        myProcess.StartInfo.Arguments = Application.StartupPath & "\LCARS x32 Manual"
        launchProcessOnScreen(myProcess)
    End Sub

    Public Sub myRun_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim myRunDialog As New frmRunProgram
        MoveToScreen(myRunDialog.Handle)
        myRunDialog.Show()
    End Sub

    Public Sub myAlertListButton_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        If frmAlerts.Visible Then
            frmAlerts.Hide()
        Else
            frmAlerts.Show()
        End If
    End Sub

    Private Sub myForm_Load(ByVal sender As Object, ByVal e As EventArgs)
        modDiagnostics.LogInfo("modBusiness.myForm_Load", "screen=" & ScreenIndex & " form=" & myForm.Name)
        myForm.Bounds = Screen.AllScreens(ScreenIndex).Bounds
        myForm.Show()
        _isInit = True
        Dim view1 As frmMainscreen1 = TryCast(myForm, frmMainscreen1)
        If view1 IsNot Nothing Then view1.EnsureView1ChromeLayout()
        UpdateRegion()
        initCommonComponents(Me)
        ' initCommonComponents can snap View1 panels back to designer sizes — repair before desktop area sticks.
        If view1 IsNot Nothing Then
            view1.EnsureView1ChromeLayout()
            view1.EnsureSpeechRowFill()
            UpdateRegion()
        End If
        modDiagnostics.LogInfo("modBusiness.myForm_Load", "screen=" & ScreenIndex & " init complete")
        ' Pre-warm OSK after chrome is up so the first OSK button press is show/hide only.
        If ScreenIndex = 0 Then
            myForm.BeginInvoke(New MethodInvoker(AddressOf PrewarmOskIfNeeded))
        End If
    End Sub

    ''' <summary>
    ''' Try to find a component with the given name
    ''' </summary>
    ''' <param name="componentName">Name of component to find</param>
    ''' <returns>Component found</returns>
    ''' <remarks>
    ''' If the component is not found, NULL is returned and no exceptions are thrown.
    ''' </remarks>
    Private Function tryLoadComponent(ByVal componentName As String) As Control
        Dim foundControls As Control() = myForm.Controls.Find(componentName, True)
        If foundControls.Length <> 1 Then Return Nothing
        Return foundControls(0)
    End Function

    ''' <summary>
    ''' Try associating a button with a handler
    ''' </summary>
    ''' <param name="componentName">Name of control to find</param>
    ''' <param name="var">Variable to assign to</param>
    ''' <param name="handler">Click handler to add</param>
    ''' <remarks>
    ''' If the control is not found, or is of incorrect type, no handler will be
    ''' added, and the variable will be initialized to NULL. No exceptions should
    ''' be thrown.
    ''' </remarks>
    Private Sub tryAssocButton(ByVal componentName As String, ByRef var As LCARSbuttonClass, ByVal handler As EventHandler)
        var = TryCast(tryLoadComponent(componentName), LCARSbuttonClass)
        If var IsNot Nothing Then
            AddHandler var.Click, handler
        End If
    End Sub

    Public Sub init(ByVal curForm As Form)
        'When a mainscreen is loaded, this sub is called to let LCARS x32 know
        'that it is now the mainscreen.  Since most of the functions of the
        'mainscreen are done through this module, it is imperative that it be
        'called as soon as they are created.


        'Get required components: Main screen cannot function without these
        myForm = curForm
        myMainPanel = TryCast(tryLoadComponent("pnlMain"), Panel)
        myModeSelect = TryCast(tryLoadComponent("myModeSelect"), LCARSbuttonClass)

        If myForm Is Nothing Or myMainPanel Is Nothing Or myModeSelect Is Nothing Then
            'No way we can use this form for ANYTHING; abort
            MsgBox(String.Format("Invalid interface on screen {0}. Loading default.", ScreenIndex))
            curForm.Dispose()
            init(New frmMainscreen1(Me))
            Return
        Else
            AddHandler myForm.Load, AddressOf myForm_Load
            AddHandler myForm.FormClosing, AddressOf myForm_Closing
            AddHandler myMainPanel.Resize, AddressOf myMainPanel_Resize
            AddHandler myModeSelect.Click, AddressOf myModeSelectButton_Click

            'Set the form's extended style to "WS_EX_TOOLWINDOW" which allows it
            'to stay fullscreen instead of being resized by the working area.
            Dim currentStyle As Integer = GetWindowLong_Safe(myForm.Handle, GWL_EXSTYLE)
            currentStyle = currentStyle Or (WS_EX_TOOLWINDOW)
            SetWindowLong_Safe(myForm.Handle, GWL_EXSTYLE, currentStyle)
        End If

        'Get programs list components: All components needed for program list in start menu
        ProgramsPanel = TryCast(tryLoadComponent("pnlPrograms"), WindowlessContainer)
        myProgramPagesDisplay = TryCast(tryLoadComponent("fbProgramPages"), LCARSbuttonClass)
        myProgsUp = TryCast(tryLoadComponent("myProgsUp"), LCARSbuttonClass)
        myProgsBack = TryCast(tryLoadComponent("myProgsBack"), LCARSbuttonClass)
        myProgsNext = TryCast(tryLoadComponent("myProgsNext"), LCARSbuttonClass)
        _hasProgramsList = Not (ProgramsPanel Is Nothing Or _
                                myProgramPagesDisplay Is Nothing Or _
                                myProgsUp Is Nothing Or _
                                myProgsBack Is Nothing Or _
                                myProgsNext Is Nothing)
        If _hasProgramsList Then
            'Only add handlers if we actually have all components
            AddHandler myForm.MouseWheel, AddressOf myform_MouseScroll
            AddHandler ProgramsPanel.Resize, AddressOf ProgramsPanel_Resize
            AddHandler myProgsUp.Click, AddressOf ProgBack
            AddHandler myProgsBack.Click, AddressOf previousProgPage
            AddHandler myProgsNext.Click, AddressOf nextProgPage
            EnsureStartLayoutButtons()
            ReloadStartMenuData(rebuildFromDisk:=True)
            loadProgList()
        End If

        'Get userbutton components: All components required for userbutton handling
        UserButtonsPanel = TryCast(tryLoadComponent("gridUserButtons"), ButtonGrid)
        myButtonManager = TryCast(tryLoadComponent("myButtonManager"), LCARSbuttonClass)
        myUserButtons = TryCast(tryLoadComponent("myUserButtons"), LCARSbuttonClass)
        myUserButtonEndcap = TryCast(tryLoadComponent("fbUBEndcap"), LCARS.LCARSbuttonClass)
        _hasUserButtons = Not (UserButtonsPanel Is Nothing Or myButtonManager Is Nothing Or myUserButtons Is Nothing)
        If _hasUserButtons Then
            AddHandler myButtonManager.Click, AddressOf myButtonManager_Click
            myUserButtonCollection.Clear()
            loadUserButtons()
        End If

        'Get taskbar components: All components required for taskbar
        'TODO: Separate window list and tray icons
        myAppsPanel = TryCast(tryLoadComponent("pnlApps"), Panel)
        myTrayPanel = TryCast(tryLoadComponent("pnlTray"), Panel)
        myShowTrayButton = TryCast(tryLoadComponent("ShowTrayButton"), LCARSbuttonClass)
        myHideTrayButton = TryCast(tryLoadComponent("HideTrayButton"), LCARSbuttonClass)
        _hasTaskbar = Not (myAppsPanel Is Nothing Or _
                           myTrayPanel Is Nothing Or _
                           myShowTrayButton Is Nothing Or _
                           myHideTrayButton Is Nothing)
        If _hasTaskbar Then
            'Only add handlers if we actually have all components
            AddHandler myShowTrayButton.Click, AddressOf myShowTrayButton_Click
            AddHandler myHideTrayButton.Click, AddressOf myHideTrayButton_Click

            'Create arrows for window list — both pinned to the far right of the apps strip
            leftArrow = New LCARS.Controls.ArrowButton()
            leftArrow.ArrowDirection = LCARS.LCARSarrowDirection.Left
            leftArrow.Size = New Size(25, 25)
            leftArrow.Anchor = AnchorStyles.Top Or AnchorStyles.Right
            leftArrow.Location = New Point(myAppsPanel.Width - 50, 0)
            leftArrow.Lit = False
            leftArrow.Name = "leftArrow"
            rightArrow = New LCARS.Controls.ArrowButton()
            AddHandler leftArrow.Click, AddressOf leftArrow_Click
            myAppsPanel.Controls.Add(leftArrow)
            rightArrow.ArrowDirection = LCARS.LCARSarrowDirection.Right
            rightArrow.Size = leftArrow.Size
            rightArrow.Anchor = AnchorStyles.Top Or AnchorStyles.Right
            rightArrow.Lit = False
            rightArrow.Name = "rightArrow"
            rightArrow.Location = New Point(myAppsPanel.Width - rightArrow.Width, 0)
            AddHandler rightArrow.Click, AddressOf rightArrow_Click
            myAppsPanel.Controls.Add(rightArrow)

            windowMap.Clear()
            taskbarList.Clear()
            taskbarOffset = 0
            If modSettings.ShowTrayIcons(ScreenIndex) Then
                myShowTrayButton_Click(Nothing, Nothing)
            End If
            modQuickControls.AttachToTray(Me)
        End If

        'Get power monitor components
        bars = New LCARSbuttonClass(9) { _
                TryCast(tryLoadComponent("fbBatt1"), LCARSbuttonClass), _
                TryCast(tryLoadComponent("fbBatt2"), LCARSbuttonClass), _
                TryCast(tryLoadComponent("fbBatt3"), LCARSbuttonClass), _
                TryCast(tryLoadComponent("fbBatt4"), LCARSbuttonClass), _
                TryCast(tryLoadComponent("fbBatt5"), LCARSbuttonClass), _
                TryCast(tryLoadComponent("fbBatt6"), LCARSbuttonClass), _
                TryCast(tryLoadComponent("fbBatt7"), LCARSbuttonClass), _
                TryCast(tryLoadComponent("fbBatt8"), LCARSbuttonClass), _
                TryCast(tryLoadComponent("fbBatt9"), LCARSbuttonClass), _
                TryCast(tryLoadComponent("fbBatt10"), LCARSbuttonClass)}
        myBattPercent = tryLoadComponent("lblBatt")
        myPowerSource = tryLoadComponent("lblPowerSource")
        myBattPanel = TryCast(tryLoadComponent("pnlBatt"), Panel)
        _hasPowerMonitor = Not (myBattPercent Is Nothing Or myPowerSource Is Nothing)
        For Each myBar As LCARSbuttonClass In bars
            _hasPowerMonitor = _hasPowerMonitor And myBar IsNot Nothing
        Next
        If _hasPowerMonitor Then
            modQuickControls.AttachBatteryHud(Me)
        End If

        'Get clock: This is a single component, but needs extra handling
        myClock = tryLoadComponent("myClock")
        _hasClock = myClock IsNot Nothing
        If _hasClock Then
            setDoubleBuffered(myClock)
            modHudWeather.AttachWeatherHud(Me)
            _hasWeather = myWeather IsNot Nothing
        End If

        'Get speech indicator: This is a single component, but needs extra handling
        mySpeech = TryCast(tryLoadComponent("mySpeech"), LCARSbuttonClass)
        _hasSpeechIndicator = mySpeech IsNot Nothing
        If _hasSpeechIndicator Then
            AddHandler mySpeech.Click, AddressOf mySpeech_Click
            mySpeech.Lit = modSpeech.SpeechEnabled
        End If

        'Common Buttons: Only have a click event to associate, and that's it.
        If myStartMenu IsNot Nothing Then
            AddHandler myStartMenu.MouseDown, AddressOf TraceStartMenuMouseDown
        End If
        If myUserButtons IsNot Nothing Then
            AddHandler myUserButtons.MouseDown, AddressOf TracePersonalProgramsMouseDown
        End If
        tryAssocButton("MyComp", myComputer, AddressOf myCompButton_Click)
        tryAssocButton("mySettings", mySettings, AddressOf mySettingsButton_Click)
        tryAssocButton("myEngineering", myEngineering, AddressOf myEngineeringButton_Click)
        tryAssocButton("myDeactivate", myDeactivate, AddressOf myDeactivateButton_Click)
        tryAssocButton("myAlert", myAlert, AddressOf myAlertButton_Click)
        tryAssocButton("myDestruct", myDestruct, AddressOf myDestructButton_Click)
        tryAssocButton("myPhoto", myPhoto, AddressOf myPhoto_Click)
        tryAssocButton("fbWebBrowser", myWebBrowser, AddressOf myWebBrowser_Click)
        tryAssocButton("fbTerminal", myTerminal, AddressOf myTerminal_Click)
        tryAssocButton("myDocuments", myDocuments, AddressOf myDocuments_Click)
        tryAssocButton("myPictures", myPictures, AddressOf myPictures_Click)
        tryAssocButton("myVideos", myVideos, AddressOf myVideos_Click)
        tryAssocButton("myMusic", myMusic, AddressOf myMusic_Click)
        tryAssocButton("myOSK", myOSK, AddressOf myOSK_Click)
        tryAssocButton("myHelp", myHelp, AddressOf myHelp_Click)
        tryAssocButton("myRun", myRun, AddressOf myRun_Click)
        tryAssocButton("myAlertListButton", myAlertListButton, AddressOf myAlertListButton_Click)
        tryAssocButton("fbDesktop", myDesktopFiles, AddressOf myDesktopFiles_Click)
        tryAssocButton("fbMyNetwork", myNetworkPlaces, AddressOf myNetworkPlaces_Click)

        'Final setup
        loadLanguage()
        LCARS.SetBeeping(myForm, modSettings.ButtonBeep)
        RegisterAlertForm(myForm)

        'load Autohide settings:
        tmrAutohide.Interval = 100
        If modSettings.AutoHide(ScreenIndex) Then
            SetAutoHide(IAutohide.AutoHideModes.Visible)
        Else
            SetAutoHide(IAutohide.AutoHideModes.Disabled)
        End If
    End Sub

    Public Sub loadLanguage()
        Try
            Dim strinput As String = ""
            Dim split() As String
            Dim filename As String = modSettings.LanguageFileName(ScreenIndex)

            FileOpen(1, Application.StartupPath() & "\lang\" & filename, OpenMode.Input)
            Input(1, strinput)
            If strinput.ToLower = "lcars x32 language file" Then
                Do Until EOF(1)
                    Input(1, strinput)
                    If strinput.Contains("=") Then
                        split = strinput.Split("="c)
                        'Makes sure that one bad line doesn't stop loading the whole language file.
                        If myForm.Controls.Find(split(0).Trim, True).Length > 0 Then
                            CType(myForm.Controls.Find(split(0).Trim, True)(0), LCARS.LCARSbuttonClass).ButtonText = split(1).Trim.Replace(Chr(34), "")
                        End If
                    End If
                Loop
            Else
                MsgBox("The file '" & filename & "' is not a valid LCARS x32 language file." _
                       & vbNewLine & vbNewLine & "LCARS x32 will use the default button text instead.")
            End If
            FileClose(1)
        Catch ex As Exception
            MsgBox("error" & vbNewLine & ex.ToString())
            FileClose(1)
        End Try

        EnsureMediaViewerButtonLabel()
        ApplyMediaFolderLabels(myForm)
    End Sub

    ''' <summary>
    ''' Language files historically labeled this control "PHOTO VIEWER" / "IMAGING DATABANK".
    ''' Always apply the Media Viewer label after language load so Start Menu stays correct
    ''' even when Lang\Standard.lng on the tablet is still the old file.
    ''' </summary>
    Private Sub EnsureMediaViewerButtonLabel()
        If myPhoto Is Nothing Then Return
        Dim cur As String = If(myPhoto.ButtonText, "").Trim()
        If cur.Equals("IMAGING DATABANK", StringComparison.OrdinalIgnoreCase) OrElse
           cur.Equals("MEDIA DATABANK", StringComparison.OrdinalIgnoreCase) Then
            myPhoto.ButtonText = "MEDIA DATABANK"
        Else
            myPhoto.ButtonText = "Media Viewer"
        End If
        myPhoto.Text = myPhoto.ButtonText
    End Sub

#Region " Tray Icon Handling "
    Public Sub UpdateTray()
        'Deal with resizing the tray icon panel if necessary
        Dim myPlacement As New WINDOWPLACEMENT
        GetWindowPlacement(hTrayIcons, myPlacement)
        Dim myWidth As Integer = myPlacement.rcNormalPosition.Right - myPlacement.rcNormalPosition.Left
        If myWidth <> myWidth + myHideTrayButton.Width Then
            myAppsPanel.Width -= (myWidth + myHideTrayButton.Width) - myTrayPanel.Width
            myTrayPanel.Left -= (myWidth + myHideTrayButton.Width) - myTrayPanel.Width

            myTrayPanel.Width = myWidth + myHideTrayButton.Width
        End If
        modQuickControls.SyncQuickButton(Me)
    End Sub

    Public Sub myShowTrayButton_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        GetTray(Me)
        modSettings.ShowTrayIcons(ScreenIndex) = True
        Dim myPlacement As New WINDOWPLACEMENT
        GetWindowPlacement(hTrayIcons, myPlacement)
        Dim myWidth As Integer = myPlacement.rcNormalPosition.Right - myPlacement.rcNormalPosition.Left

        myAppsPanel.Width -= (myWidth + myHideTrayButton.Width) - myTrayPanel.Width
        myTrayPanel.Left -= (myWidth + myHideTrayButton.Width) - myTrayPanel.Width

        myTrayPanel.Width = myWidth + myHideTrayButton.Width

        myShowTrayButton.Visible = False
        myHideTrayButton.Visible = True
        modQuickControls.SyncQuickButton(Me)
    End Sub

    Public Sub myHideTrayButton_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        modSettings.ShowTrayIcons(ScreenIndex) = False
        ReturnTray(Me)
        Dim mywidth As Integer = myShowTrayButton.Width - myTrayPanel.Width
        myTrayPanel.Width = myShowTrayButton.Width
        myTrayPanel.Left -= mywidth
        myAppsPanel.Width -= mywidth
        myHideTrayButton.Visible = False
        myShowTrayButton.Visible = True
        myShowTrayButton.BringToFront()
        modQuickControls.SyncQuickButton(Me)
    End Sub
#End Region

#Region " Taskbar buttons "
    Public Sub AddWindow(ByVal window As ExternalApp)
        Dim newItem As New TaskbarItem(window, Me, taskbarList.Count)
        newItem.Offset = taskbarOffset
        taskbarList.Add(newItem)
        windowMap.Add(window, newItem)
    End Sub

    Public Sub RemoveWindow(ByVal window As ExternalApp)
        Dim oldItem As TaskbarItem = windowMap(window)
        windowMap.Remove(window)
        For i As Integer = oldItem.Index + 1 To taskbarList.Count - 1
            taskbarList(i).Index = i - 1
        Next
        taskbarList.RemoveAt(oldItem.Index)
        oldItem.Remove()
    End Sub

    Public Sub UpdateWindow(ByVal window As ExternalApp, ByVal flags As WindowUpdateFlags)
        windowMap(window).Update(flags)
    End Sub

    'Moves the taskbar buttons to the right
    Private Sub rightArrow_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        taskbarOffset += 1
        For Each myTaskbarItem As TaskbarItem In taskbarList
            myTaskbarItem.Offset = taskbarOffset
        Next
    End Sub

    'Moves the taskbar buttons to the left
    Private Sub leftArrow_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        taskbarOffset -= 1
        For Each myTaskbarItem As TaskbarItem In taskbarList
            myTaskbarItem.Offset = taskbarOffset
        Next
    End Sub

#End Region

    Public Sub loadUserButtons()

        If UserButtonsPanel Is Nothing Then Return

        Using diag As DiagnosticScope = modDiagnostics.BeginScope("modBusiness.loadUserButtons", "screen=" & ScreenIndex)
            Try
                Dim myReg As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser

                UserButtonsPanel.Clear()
                myUserButtonCollection.Clear()

                myReg = myReg.OpenSubKey("Software\VB and VBA Program Settings\LCARS x32\UserButtons", False)

                If myReg IsNot Nothing Then
                    For intloop As Integer = 0 To myReg.ValueCount - 1
                        Dim valueName As String = myReg.GetValueNames(intloop)
                        If String.IsNullOrEmpty(valueName) Then Continue For

                        Dim mybutton As New LCARS.LightweightControls.LCFlatButton
                        Dim myUserButtonInfo As New UserButtonInfo

                        mybutton.Beeping = False
                        mybutton.Color = LCARS.LCARScolorStyles.MiscFunction

                        AddHandler mybutton.Click, AddressOf myfile_click

                        If valueName.Length >= 2 AndAlso IsNumeric(valueName.Substring(0, 2)) Then
                            mybutton.Text = valueName.Substring(2)
                        Else
                            mybutton.Text = valueName
                        End If

                        myUserButtonInfo.Name = mybutton.Text
                        Dim rawData As Object = myReg.GetValue(valueName)
                        If rawData IsNot Nothing Then
                            mybutton.Data = rawData
                            myUserButtonInfo.Location = CStr(rawData)
                        Else
                            mybutton.Data = ""
                            myUserButtonInfo.Location = ""
                        End If

                        UserButtonsPanel.Add(mybutton)

                        AddUserButton(myUserButtonInfo, True)
                    Next
                End If
                modDiagnostics.LogInfo("modBusiness.loadUserButtons", "screen=" & ScreenIndex & " loaded=" & myUserButtonCollection.Count)
            Catch ex As Exception
                modDiagnostics.LogException("modBusiness.loadUserButtons", ex, "screen=" & ScreenIndex)
                Debug.WriteLine("loadUserButtons: " & ex.ToString())
            End Try
        End Using

    End Sub

    Public Sub AddUserButton(ByVal button As UserButtonInfo, Optional ByVal DontSave As Boolean = False)
        myUserButtonCollection.Add(button)
        If DontSave = False Then
            SaveUserButtons()
            loadUserButtons()
        End If

    End Sub

    Public Sub RemoveUserButton(ByVal index As Integer)
        myUserButtonCollection.RemoveAt(index)
        SaveUserButtons()
        loadUserButtons()
    End Sub

    Public Sub EditUserButton(ByVal button As UserButtonInfo, ByVal index As Integer)
        myUserButtonCollection(index) = button
        SaveUserButtons()
        loadUserButtons()
    End Sub

    Public Sub SaveUserButtons()
        Try
            'remove current userbuttons
            Dim myReg As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser
            myReg = myReg.OpenSubKey("Software\VB and VBA Program Settings\LCARS x32\UserButtons", True)
            Dim myValues() As String = myReg.GetValueNames()

            For Each myValue As String In myValues
                myReg.DeleteValue(myValue)
            Next
        Catch ex As Exception

        End Try

        Dim intCount As Integer = 0
        Dim index As Integer
        For index = 0 To myUserButtonCollection.Count - 1
            Dim myobject As Object = myUserButtonCollection(index)
            Dim myButton As UserButtonInfo
            myButton = CType(myobject, UserButtonInfo)
            Try
                SaveSetting("LCARS x32", "UserButtons", intCount.ToString("D2") & myButton.Name, myButton.Location)
            Catch ex As Exception
                'MsgBox(ex.ToString())
            End Try
            intCount += 1
        Next
    End Sub

#Region " Start Menu Handlers "
    Private Function IsProgramsPanelHierarchyVisible() As Boolean
        If ProgramsPanel Is Nothing Then Return False
        Dim c As Control = ProgramsPanel
        While c IsNot Nothing
            If Not c.Visible Then Return False
            c = c.Parent
        End While
        Return True
    End Function

    Public Sub RefreshStartMenuPrograms()
        If Not _hasProgramsList Then Return
        If Not IsProgramsPanelHierarchyVisible() Then Return
        If myForm Is Nothing OrElse myForm.IsDisposed Then Return
        modDiagnostics.LogTouch("StartMenu", "RefreshStartMenuPrograms queued screen=" & ScreenIndex)
        myForm.BeginInvoke(New MethodInvoker(Sub()
                                                 If Not IsProgramsPanelHierarchyVisible() Then Return
                                                 modDiagnostics.LogInfo("modBusiness.RefreshStartMenuPrograms", "screen=" & ScreenIndex)
                                                 ReloadStartMenuData(rebuildFromDisk:=True)
                                                 loadProgList(curProgIndex)
                                             End Sub))
    End Sub

    ''' <summary>
    ''' Re-reads classic Programs folders and applies LCARS start-layout store.
    ''' </summary>
    Public Sub ReloadStartMenuData(Optional ByVal rebuildFromDisk As Boolean = False)
        StartMenuLayout.InvalidateCache()
        If rebuildFromDisk OrElse ClassicPrograms Is Nothing Then
            ClassicPrograms = GetAllPrograms
        End If
        MyPrograms = StartMenuLayout.BuildDisplayRoot(ClassicPrograms)
        UpdateStartLayoutButtonLabels()
    End Sub

    Private Sub EnsureStartLayoutButtons()
        Dim startPanel As Control = tryLoadComponent("pnlStart")
        If startPanel Is Nothing Then Return
        If btnStartEdit Is Nothing Then
            Dim existingEdit As Control = tryLoadComponent("fbStartEdit")
            If existingEdit IsNot Nothing Then
                btnStartEdit = TryCast(existingEdit, LCARS.LCARSbuttonClass)
            Else
                Dim fb As New LCARS.Controls.FlatButton()
                fb.Name = "fbStartEdit"
                fb.ButtonText = "EDIT"
                fb.Text = "EDIT"
                fb.Size = New Size(70, 22)
                fb.Location = New Point(3, 3)
                fb.Anchor = AnchorStyles.Top Or AnchorStyles.Left
                fb.Color = LCARS.LCARScolorStyles.Orange
                startPanel.Controls.Add(fb)
                fb.BringToFront()
                btnStartEdit = fb
            End If
            If btnStartEdit IsNot Nothing Then
                btnStartEdit.Color = LCARS.LCARScolorStyles.Orange
                btnStartEdit.Anchor = AnchorStyles.Top Or AnchorStyles.Left
                btnStartEdit.Location = New Point(3, 3)
                btnStartEdit.Size = New Size(Math.Max(60, btnStartEdit.Width), Math.Max(20, btnStartEdit.Height))
                AddHandler btnStartEdit.Click, AddressOf StartEdit_Click
            End If
        Else
            btnStartEdit.Color = LCARS.LCARScolorStyles.Orange
            btnStartEdit.Anchor = AnchorStyles.Top Or AnchorStyles.Left
            btnStartEdit.Location = New Point(3, 3)
        End If
        If btnStartMode Is Nothing Then
            Dim existingMode As Control = tryLoadComponent("fbStartMode")
            If existingMode IsNot Nothing Then
                btnStartMode = TryCast(existingMode, LCARS.LCARSbuttonClass)
            Else
                Dim fb As New LCARS.Controls.FlatButton()
                fb.Name = "fbStartMode"
                fb.ButtonText = "MODE"
                fb.Text = "MODE"
                fb.Size = New Size(70, 20)
                fb.Color = LCARS.LCARScolorStyles.NavigationFunction
                startPanel.Controls.Add(fb)
                fb.BringToFront()
                btnStartMode = fb
            End If
            If btnStartMode IsNot Nothing Then
                AddHandler btnStartMode.Click, AddressOf StartMode_Click
            End If
        End If
        ' MODE stays bottom-left next to RUN PROGRAM.
        If btnStartMode IsNot Nothing Then
            Dim runBtn As Control = tryLoadComponent("myRun")
            Dim modeY As Integer = startPanel.Height - 46
            Dim modeX As Integer = 66
            If runBtn IsNot Nothing Then
                modeY = runBtn.Top
                modeX = runBtn.Right + 4
                btnStartMode.Height = Math.Max(18, runBtn.Height)
            End If
            btnStartMode.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
            btnStartMode.Location = New Point(modeX, modeY)
            btnStartMode.Width = 70
            btnStartMode.BringToFront()
        End If
        If btnStartEdit IsNot Nothing Then btnStartEdit.BringToFront()
        WireStartEditInputHandlers()
        UpdateStartLayoutButtonLabels()
    End Sub

    Private Sub WireStartEditInputHandlers()
        If startDragHandlersWired Then Return
        If ProgramsPanel IsNot Nothing Then
            AddHandler ProgramsPanel.MouseDown, AddressOf StartPrograms_MouseDown
            AddHandler ProgramsPanel.MouseMove, AddressOf StartPrograms_MouseMove
            AddHandler ProgramsPanel.MouseUp, AddressOf StartPrograms_MouseUp
        End If
        If myForm IsNot Nothing Then
            myForm.KeyPreview = True
            AddHandler myForm.KeyDown, AddressOf StartEdit_KeyDown
        End If
        startDragHandlersWired = True
    End Sub

    Private Sub UpdateStartLayoutButtonLabels()
        If btnStartEdit IsNot Nothing Then
            Dim t As String = If(startEditMode, "DONE", "EDIT")
            btnStartEdit.ButtonText = t
            btnStartEdit.Text = t
        End If
        If btnStartMode IsNot Nothing Then
            Dim t As String = If(StartMenuLayout.IsAppFocused(), "APPS", "ALL")
            btnStartMode.ButtonText = t
            btnStartMode.Text = t
        End If
    End Sub

    Private Sub StartEdit_Click(ByVal sender As Object, ByVal e As EventArgs)
        startEditMode = Not startEditMode
        startSelectedPinIndex = -1
        startDragging = False
        startDragPinIndex = -1
        startDragLabel = ""
        HideStartDragChrome()
        UpdateStartLayoutButtonLabels()
        loadProgList(curProgIndex)
    End Sub

    Private Sub StartMode_Click(ByVal sender As Object, ByVal e As EventArgs)
        StartMenuLayout.ToggleMode()
        ProgDir.Clear()
        ReloadStartMenuData(rebuildFromDisk:=False)
        loadProgList(0)
    End Sub

    Private Sub StartPrograms_MouseDown(ByVal sender As Object, ByVal e As MouseEventArgs)
        startDragging = False
        startDragPinIndex = -1
        startDragLabel = ""
        HideStartDragChrome()
        If Not startEditMode OrElse Not IsBrowsingPinnedFolder() OrElse ProgramsPanel Is Nothing Then Return
        Dim ctrl As LightweightControls.ILightweightControl = ProgramsPanel.ControlFromPoint(e.Location)
        Dim flat As LightweightControls.LCFlatButton = TryCast(ctrl, LightweightControls.LCFlatButton)
        If flat Is Nothing Then Return
        Dim pinIdx As Integer = -1
        If TypeOf flat.Data Is Integer Then
            pinIdx = CInt(flat.Data)
        ElseIf flat.Data IsNot Nothing Then
            Dim state As StartMenuLayout.LayoutState = StartMenuLayout.LoadLayout()
            Dim path As String = CStr(flat.Data)
            For i As Integer = 0 To state.Pins.Count - 1
                If String.Equals(state.Pins(i).Path, path, StringComparison.OrdinalIgnoreCase) Then
                    pinIdx = i
                    Exit For
                End If
            Next
        End If
        If pinIdx < 0 Then Return
        startDragPinIndex = pinIdx
        startDragOrigin = e.Location
        startSelectedPinIndex = pinIdx
        startDragLabel = flat.Text.Replace("  [X]", "").Trim()
    End Sub

    Private Sub StartPrograms_MouseMove(ByVal sender As Object, ByVal e As MouseEventArgs)
        If startDragPinIndex < 0 OrElse Not startEditMode Then Return
        If e.Button <> MouseButtons.Left Then Return
        Dim dx As Integer = Math.Abs(e.X - startDragOrigin.X)
        Dim dy As Integer = Math.Abs(e.Y - startDragOrigin.Y)
        If dx > 8 OrElse dy > 8 Then
            startDragging = True
            ShowStartDragGhost(e.Location)
            ShowStartDropMarker(e.Y)
        End If
    End Sub

    Private Sub StartPrograms_MouseUp(ByVal sender As Object, ByVal e As MouseEventArgs)
        If startDragging AndAlso startDragPinIndex >= 0 AndAlso IsBrowsingPinnedFolder() Then
            Dim targetRow As Integer = Math.Max(0, e.Y \ 30)
            Dim state As StartMenuLayout.LayoutState = StartMenuLayout.LoadLayout()
            If state.Pins.Count > 0 Then
                Dim newIndex As Integer = Math.Min(state.Pins.Count - 1, targetRow)
                Dim delta As Integer = newIndex - startDragPinIndex
                If delta <> 0 Then
                    StartMenuLayout.MovePin(startDragPinIndex, delta)
                    startSelectedPinIndex = newIndex
                    ReloadStartMenuData(rebuildFromDisk:=False)
                    loadProgList(curProgIndex)
                End If
            End If
        End If
        HideStartDragChrome()
        startDragging = False
        startDragPinIndex = -1
        startDragLabel = ""
    End Sub

    Private Sub ShowStartDragGhost(ByVal panelPoint As Point)
        If ProgramsPanel Is Nothing OrElse ProgramsPanel.Parent Is Nothing Then Return
        Dim host As Control = ProgramsPanel.Parent
        If startDragGhost Is Nothing Then
            startDragGhost = New Label()
            startDragGhost.AutoSize = False
            startDragGhost.Height = 26
            startDragGhost.BackColor = Color.FromArgb(220, 255, 140, 0)
            startDragGhost.ForeColor = Color.Black
            Try
                startDragGhost.Font = New Font("LCARS", 12.0F, FontStyle.Regular)
            Catch
                startDragGhost.Font = New Font(FontFamily.GenericSansSerif, 10.0F, FontStyle.Bold)
            End Try
            startDragGhost.TextAlign = ContentAlignment.MiddleLeft
            startDragGhost.BorderStyle = BorderStyle.FixedSingle
            host.Controls.Add(startDragGhost)
        End If
        startDragGhost.Text = "  " & startDragLabel
        startDragGhost.Width = Math.Max(80, ProgramsPanel.Width - 8)
        Dim screenPt As Point = ProgramsPanel.PointToScreen(panelPoint)
        Dim hostPt As Point = host.PointToClient(screenPt)
        startDragGhost.Location = New Point(ProgramsPanel.Left + 4, hostPt.Y - 12)
        startDragGhost.Visible = True
        startDragGhost.BringToFront()
    End Sub

    Private Sub ShowStartDropMarker(ByVal panelY As Integer)
        If ProgramsPanel Is Nothing OrElse ProgramsPanel.Parent Is Nothing Then Return
        Dim host As Control = ProgramsPanel.Parent
        If startDropMarker Is Nothing Then
            startDropMarker = New Label()
            startDropMarker.AutoSize = False
            startDropMarker.Height = 3
            startDropMarker.BackColor = Color.Orange
            startDropMarker.BorderStyle = BorderStyle.None
            host.Controls.Add(startDropMarker)
        End If
        Dim row As Integer = Math.Max(0, panelY \ 30)
        startDropMarker.Width = Math.Max(40, ProgramsPanel.Width)
        startDropMarker.Location = New Point(ProgramsPanel.Left, ProgramsPanel.Top + (row * 30) - 1)
        startDropMarker.Visible = True
        startDropMarker.BringToFront()
        If startDragGhost IsNot Nothing Then startDragGhost.BringToFront()
    End Sub

    Private Sub HideStartDragChrome()
        If startDragGhost IsNot Nothing Then startDragGhost.Visible = False
        If startDropMarker IsNot Nothing Then startDropMarker.Visible = False
    End Sub

    Private Sub StartEdit_KeyDown(ByVal sender As Object, ByVal e As KeyEventArgs)
        If Not startEditMode OrElse Not IsBrowsingPinnedFolder() Then Return
        If startSelectedPinIndex < 0 Then Return
        If e.KeyCode = Keys.Up Then
            StartMenuLayout.MovePin(startSelectedPinIndex, -1)
            startSelectedPinIndex = Math.Max(0, startSelectedPinIndex - 1)
            ReloadStartMenuData(rebuildFromDisk:=False)
            loadProgList(curProgIndex)
            e.Handled = True
        ElseIf e.KeyCode = Keys.Down Then
            Dim state As StartMenuLayout.LayoutState = StartMenuLayout.LoadLayout()
            StartMenuLayout.MovePin(startSelectedPinIndex, 1)
            startSelectedPinIndex = Math.Min(state.Pins.Count - 1, startSelectedPinIndex + 1)
            ReloadStartMenuData(rebuildFromDisk:=False)
            loadProgList(curProgIndex)
            e.Handled = True
        End If
    End Sub

    Private Sub ProgramsPanel_Resize(ByVal sender As Object, ByVal e As System.EventArgs)
        modDiagnostics.LogInfo("modBusiness.ProgramsPanel_Resize",
            "screen=" & ScreenIndex &
            " panel=" & If(ProgramsPanel Is Nothing, "null", ProgramsPanel.Size.ToString()) &
            " hierarchyVisible=" & IsProgramsPanelHierarchyVisible())
        If myForm.WindowState = FormWindowState.Minimized Then Return
        If Not IsProgramsPanelHierarchyVisible() Then Return
        loadProgList(curProgIndex)
    End Sub

    Private Sub loadProgList(Optional ByVal index As Integer = 0)
        Using diag As DiagnosticScope = modDiagnostics.BeginScope("modBusiness.loadProgList",
            "screen=" & ScreenIndex & " index=" & index & " progDirDepth=" & ProgDir.Count)
            Try
                Dim itemCount As Integer = 0
        Dim myDir As DirectoryStartItem
        Dim pageMax As Integer

        ProgPageSize = ProgramsPanel.Height \ 30
        index = index - (index Mod ProgPageSize)
        curProgIndex = index

        myDir = MyPrograms
        For Each myindex As Integer In ProgDir
            myDir = CType(myDir.subItems(myindex), DirectoryStartItem)
        Next
        ProgramsPanel.Clear()

        pageCount = CInt(Int(myDir.subItems.Count / ProgPageSize))

        If myDir.subItems.Count Mod ProgPageSize > 0 Then
            pageCount += 1
        End If

        curProgPage = (index \ ProgPageSize) + 1

        myProgramPagesDisplay.Text = "PAGES " & curProgPage & " of " & pageCount


        pageMax = ProgPageSize + (index - 1)

        If pageMax > myDir.subItems.Count - 1 Then
            pageMax = myDir.subItems.Count - 1
        End If

        For intloop As Integer = index To pageMax
            If myDir.subItems(intloop).GetType Is GetType(DirectoryStartItem) Then
                With CType(myDir.subItems(intloop), DirectoryStartItem)
                    Dim myButton As New LCARS.LightweightControls.LCComplexButton
                    myButton.HoldDraw = True
                    myButton.Width = ProgramsPanel.Width
                    myButton.Height = 25
                    myButton.Left = 0
                    myButton.Color = LCARS.LCARScolorStyles.NavigationFunction
                    myButton.Text = .Name
                    myButton.SideText = .subItems.Count.ToString()
                    myButton.TextHeight = 14
                    myButton.TextAlign = ContentAlignment.BottomRight
                    myButton.Data = intloop
                    myButton.Top = itemCount * 30
                    myButton.Beeping = False
                    myButton.Data2 = ((pageMax - index) - (intloop - index)).ToString
                    ProgramsPanel.Add(myButton)
                    myButton.HoldDraw = False
                    AddHandler myButton.Click, AddressOf myDir_click
                    itemCount += 1
                End With
            Else
                With CType(myDir.subItems(intloop), FileStartItem)
                    Dim pinIndex As Integer = -1
                    Dim editingPinned As Boolean = startEditMode AndAlso IsBrowsingPinnedFolder()
                    If editingPinned Then
                        Dim state As StartMenuLayout.LayoutState = StartMenuLayout.LoadLayout()
                        For i As Integer = 0 To state.Pins.Count - 1
                            If String.Equals(state.Pins(i).Path, .Link.Executable, StringComparison.OrdinalIgnoreCase) Then
                                pinIndex = i
                                Exit For
                            End If
                        Next
                    End If

                    Dim rowTop As Integer = itemCount * 30
                    Dim upW As Integer = If(editingPinned AndAlso pinIndex >= 0, 28, 0)
                    Dim downW As Integer = upW
                    Dim gap As Integer = If(upW > 0, 2, 0)
                    Dim mainLeft As Integer = upW + gap
                    Dim mainW As Integer = ProgramsPanel.Width - upW - downW - (gap * 2)
                    If mainW < 40 Then mainW = ProgramsPanel.Width

                    If upW > 0 Then
                        Dim upBtn As New LCARS.LightweightControls.LCStandardButton
                        upBtn.HoldDraw = True
                        upBtn.Width = upW
                        upBtn.Height = 25
                        upBtn.Left = 0
                        upBtn.Top = rowTop
                        upBtn.Color = LCARS.LCARScolorStyles.NavigationFunction
                        upBtn.Text = ChrW(&H25B2) ' ▲
                        upBtn.Data = pinIndex
                        upBtn.Beeping = False
                        ProgramsPanel.Add(upBtn)
                        upBtn.HoldDraw = False
                        AddHandler upBtn.Click, AddressOf StartPinMoveUp_Click
                    End If

                    Dim myButton As New LCARS.LightweightControls.LCStandardButton
                    myButton.HoldDraw = True
                    myButton.Width = mainW
                    myButton.Height = 25
                    myButton.Left = mainLeft
                    myButton.Color = If(startEditMode AndAlso pinIndex = startSelectedPinIndex,
                                        LCARS.LCARScolorStyles.PrimaryFunction,
                                        If(startEditMode, LCARS.LCARScolorStyles.FunctionOffline, LCARS.LCARScolorStyles.MiscFunction))
                    myButton.Text = Path.GetFileNameWithoutExtension(.Name) & If(startEditMode, "  [X]", "")
                    myButton.Data = .Link.Executable
                    myButton.Top = rowTop
                    myButton.Beeping = False
                    If pinIndex >= 0 Then
                        myButton.Data2 = pinIndex.ToString()
                    Else
                        myButton.Data2 = ((pageMax - index) - (intloop - index)).ToString
                    End If
                    ProgramsPanel.Add(myButton)
                    myButton.HoldDraw = False
                    AddHandler myButton.Click, AddressOf startItem_click

                    If downW > 0 Then
                        Dim downBtn As New LCARS.LightweightControls.LCStandardButton
                        downBtn.HoldDraw = True
                        downBtn.Width = downW
                        downBtn.Height = 25
                        downBtn.Left = mainLeft + mainW + gap
                        downBtn.Top = rowTop
                        downBtn.Color = LCARS.LCARScolorStyles.NavigationFunction
                        downBtn.Text = ChrW(&H25BC) ' ▼
                        downBtn.Data = pinIndex
                        downBtn.Beeping = False
                        ProgramsPanel.Add(downBtn)
                        downBtn.HoldDraw = False
                        AddHandler downBtn.Click, AddressOf StartPinMoveDown_Click
                    End If
                    itemCount += 1
                End With
            End If
        Next
        ProgramsPanel.Tag = (pageMax - index).ToString

        'Update buttons
        myProgsNext.Lit = curProgPage < pageCount
        myProgsBack.Lit = curProgPage > 1
        myProgsUp.Lit = ProgDir.Count > 0
                modDiagnostics.LogInfo("modBusiness.loadProgList",
                    "screen=" & ScreenIndex &
                    " items=" & itemCount &
                    " page=" & curProgPage & "/" & pageCount &
                    " pageSize=" & ProgPageSize)
            Catch ex As Exception
                modDiagnostics.LogException("modBusiness.loadProgList", ex, "screen=" & ScreenIndex)
                Throw
            End Try
        End Using

    End Sub

    Public Sub nextProgPage(ByVal sender As Object, ByVal e As EventArgs)
        If curProgPage < pageCount Then
            curProgPage += 1
            loadProgList((curProgPage * ProgPageSize) - (ProgPageSize - 1))
        End If
    End Sub

    Public Sub previousProgPage(ByVal sender As Object, ByVal e As EventArgs)
        If curProgPage > 1 Then
            curProgPage -= 1
            loadProgList((curProgPage * ProgPageSize) - (ProgPageSize - 1))
        End If
    End Sub

    Public Sub ProgBack(ByVal sender As Object, ByVal e As EventArgs)
        If ProgDir.Count > 0 Then
            Dim index As Integer = ProgDir(ProgDir.Count - 1)
            ProgDir.RemoveAt(ProgDir.Count - 1)
            loadProgList(index)
        End If
    End Sub

    Private Function IsBrowsingPinnedFolder() As Boolean
        If ProgDir.Count < 1 Then Return False
        Dim rootIdx As Integer = ProgDir(0)
        If rootIdx < 0 OrElse rootIdx >= MyPrograms.subItems.Count Then Return False
        Dim top As programList.StartItem = MyPrograms.subItems(rootIdx)
        Return TypeOf top Is programList.DirectoryStartItem AndAlso _
               String.Equals(CType(top, programList.DirectoryStartItem).Name, "Pinned", StringComparison.OrdinalIgnoreCase)
    End Function

    Private Function IsStructuralStartFolder(ByVal name As String) As Boolean
        If String.IsNullOrEmpty(name) Then Return False
        Return String.Equals(name, "Pinned", StringComparison.OrdinalIgnoreCase) OrElse _
               String.Equals(name, "Newly Installed", StringComparison.OrdinalIgnoreCase) OrElse _
               String.Equals(name, "All Programs", StringComparison.OrdinalIgnoreCase) OrElse _
               String.Equals(name, "System", StringComparison.OrdinalIgnoreCase)
    End Function

    Private Sub StartPinMoveUp_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim idx As Integer = CInt(CType(sender, LightweightControls.LCFlatButton).Data)
        StartMenuLayout.MovePin(idx, -1)
        ReloadStartMenuData(rebuildFromDisk:=False)
        loadProgList(curProgIndex)
    End Sub

    Private Sub StartPinMoveDown_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim idx As Integer = CInt(CType(sender, LightweightControls.LCFlatButton).Data)
        StartMenuLayout.MovePin(idx, 1)
        ReloadStartMenuData(rebuildFromDisk:=False)
        loadProgList(curProgIndex)
    End Sub

    Private Sub myDir_click(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim folderIndex As Integer = CInt(CType(sender, LightweightControls.LCFlatButton).Data)
        If startEditMode Then
            Dim myDir As DirectoryStartItem = MyPrograms
            For Each myindex As Integer In ProgDir
                myDir = CType(myDir.subItems(myindex), DirectoryStartItem)
            Next
            If folderIndex >= 0 AndAlso folderIndex < myDir.subItems.Count AndAlso _
               TypeOf myDir.subItems(folderIndex) Is DirectoryStartItem Then
                Dim folderName As String = CType(myDir.subItems(folderIndex), DirectoryStartItem).Name
                If IsStructuralStartFolder(folderName) Then
                    ' Still allow entering Pinned / Newly Installed / All Programs while editing.
                    ProgDir.Add(folderIndex)
                    loadProgList()
                    Return
                End If
                StartMenuLayout.HideItem(folderName)
                ReloadStartMenuData(rebuildFromDisk:=False)
                loadProgList(curProgIndex)
                Return
            End If
        End If
        ProgDir.Add(folderIndex)
        loadProgList()
    End Sub

    Private Sub startItem_click(ByVal sender As Object, ByVal e As System.EventArgs)
        If startDragging Then Return
        Dim btn As LCARS.LightweightControls.LCFlatButton = CType(sender, LCARS.LightweightControls.LCFlatButton)
        Dim target As String = CStr(btn.Data)
        If startEditMode Then
            ' Edit mode: remove pin or hide classic item instead of launching.
            If IsBrowsingPinnedFolder() Then
                Dim pinIdx As Integer = -1
                Integer.TryParse(CStr(btn.Data2), pinIdx)
                Dim state As StartMenuLayout.LayoutState = StartMenuLayout.LoadLayout()
                For i As Integer = 0 To state.Pins.Count - 1
                    If String.Equals(state.Pins(i).Path, target, StringComparison.OrdinalIgnoreCase) Then
                        pinIdx = i
                        Exit For
                    End If
                Next
                ' First tap selects for arrow-key reorder; second tap on the same pin removes it.
                If pinIdx >= 0 AndAlso pinIdx <> startSelectedPinIndex Then
                    startSelectedPinIndex = pinIdx
                    loadProgList(curProgIndex)
                    Return
                End If
                If pinIdx >= 0 Then
                    StartMenuLayout.UnpinAt(pinIdx)
                    startSelectedPinIndex = -1
                End If
            Else
                StartMenuLayout.HideItem(target)
            End If
            ReloadStartMenuData(rebuildFromDisk:=False)
            loadProgList(curProgIndex)
            Return
        End If
        If ProgramsPanel.Visible Then myStartMenu.doClick(sender, e)
        Application.DoEvents()
        Dim myprocess As New System.Diagnostics.Process()
        myprocess.StartInfo.FileName = target
        launchProcessOnScreen(myprocess)
    End Sub
#End Region

    'Used for buttons in Personal Programs (userbuttons)
    Private Sub myfile_click(ByVal sender As Object, ByVal e As System.EventArgs)
        If UserButtonsPanel.Visible Then
            myUserButtons.doClick(sender, e)
        End If
        Application.DoEvents()
        Dim cmdLine As String = CType(CType(sender, LCARS.LightweightControls.LCFlatButton).Data, String).Trim()
        Dim myprocess As New Process()
        If File.Exists(cmdLine) Then
            'The command string is an absolute path.
            myprocess.StartInfo.FileName = cmdLine
            myprocess.StartInfo.WorkingDirectory = Path.GetDirectoryName(cmdLine)
            launchProcessOnScreen(myprocess)
        Else
            'The command will be interpreted as a command followed by arguments
            Try
                If (cmdLine.Substring(0, 1) = """") Then
                    Dim splitIndex As Integer = cmdLine.Substring(1).IndexOf("""") + 2
                    myprocess.StartInfo.FileName = cmdLine.Substring(0, splitIndex)
                    myprocess.StartInfo.Arguments = cmdLine.Substring(splitIndex + 1)
                Else
                    myprocess.StartInfo.FileName = cmdLine.Split(" "c)(0)
                    myprocess.StartInfo.Arguments = cmdLine.Substring(myprocess.StartInfo.FileName.Length + 1)
                End If
                'If full path specified, set working directory to containing folder
                If File.Exists(myprocess.StartInfo.FileName) Then
                    myprocess.StartInfo.WorkingDirectory = Path.GetDirectoryName(myprocess.StartInfo.FileName)
                End If
                launchProcessOnScreen(myprocess)
            Catch ex As Exception
                Debug.Print("Failed to interpret command line")
                'Throw it to shell and see what happens.
                Try
                    Dim myID As Integer
                    myID = Shell(cmdLine, AppWinStyle.NormalFocus)
                    myprocess = Process.GetProcessById(myID)
                    launchProcessOnScreen(myprocess, False)
                Catch ex2 As ArgumentException
                    'Process already exited before we could get it.
                Catch ex2 As FileNotFoundException
                    MsgBox("Bad command string: " & cmdLine)
                End Try
            End Try
        End If
    End Sub

    Public Sub launchProcessOnScreen(ByVal myProcess As Process, Optional ByVal needsStart As Boolean = True)
        If needsStart Then
            Try
                If Not myProcess.Start() Then Return
            Catch ex As System.ComponentModel.Win32Exception
                MsgBox("Unable to start process")
                Return
            End Try
        End If
        Dim sw As New Stopwatch()
        sw.Start()
        Do Until myProcess.HasExited OrElse _
                 myProcess.MainWindowHandle <> IntPtr.Zero OrElse _
                 sw.ElapsedMilliseconds > 15000L
            myProcess.Refresh()
            Threading.Thread.Sleep(50)
        Loop
        sw.Stop()
        If Not myProcess.HasExited AndAlso _
               myProcess.MainWindowHandle <> IntPtr.Zero Then
            MoveToScreen(myProcess.MainWindowHandle)
        End If
    End Sub


    Private Sub MoveToScreen(ByVal hWnd As IntPtr)
        If MonitorFromWindow(hWnd, MONITOR_DEFAULTTONEAREST) _
                = MonitorFromWindow(myForm.Handle, MONITOR_DEFAULTTONEAREST) Then
            'Already on correct screen
            Return
        End If
        Dim myScreen As Screen = Screen.FromHandle(myForm.Handle)
        Dim myPlacement As New WINDOWPLACEMENT
        Dim isMax As Boolean = False

        myPlacement.Length = Marshal.SizeOf(myPlacement)

        GetWindowPlacement(hWnd, myPlacement)

        myPlacement.ptMaxPosition.X = myForm.Left
        myPlacement.ptMaxPosition.Y = myForm.Top

        myPlacement.rcNormalPosition.Right -= myPlacement.rcNormalPosition.Left - myForm.Left
        myPlacement.rcNormalPosition.Bottom -= myPlacement.rcNormalPosition.Top - myForm.Top
        myPlacement.rcNormalPosition.Left = myForm.Location.X
        myPlacement.rcNormalPosition.Top = myForm.Location.Y

        If myPlacement.ShowCmd = WindowStates.MAXIMIZED Then
            isMax = True
            myPlacement.ShowCmd = WindowStates.NORMAL
        End If

        SetWindowPlacement(hWnd, myPlacement)

        If isMax Then
            myPlacement.ShowCmd = WindowStates.MAXIMIZED
            SetWindowPlacement(hWnd, myPlacement)
        End If
    End Sub

    Public Sub myForm_Closing(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs)
        e.Cancel = True
        myDeactivateButton_Click(sender, e)
    End Sub

    Private Function FindRoot(ByVal hWnd As Integer) As Integer
        Do
            Dim parent_hwnd As Int32 = GetParent(hWnd)
            If parent_hwnd = 0 Then Return hWnd
            hWnd = parent_hwnd
        Loop
    End Function

    Public Sub SetAutoHide(ByVal value As IAutohide.AutoHideModes)

        autohide = value
        If autohide = IAutohide.AutoHideModes.Disabled Then
            tmrAutohide.Enabled = False
            hideCount = 0
            myForm.Visible = True
            UpdateWorkingArea()
        Else
            autohide = IAutohide.AutoHideModes.Visible
            tmrAutohide.Enabled = True
        End If
    End Sub

    Private Sub tmrAutoHide_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles tmrAutohide.Tick
        If Not autohide = IAutohide.AutoHideModes.Disabled Then
            Dim myPoint As POINTAPI
            myPoint.X = Cursor.Position.X
            myPoint.Y = Cursor.Position.Y

            Dim rootHwnd As IntPtr = New IntPtr(FindRoot(WindowFromPoint(myPoint).ToInt32()))

            'The mouse must be within this many pixels of the edge to show the screen
            Const edgeWidth As Integer = 1

            Dim edges As IAutohide.AutohideEdges = CType(myForm, IAutohide).getAutohideEdges()
            Dim isAtEdge As Boolean = False

            If myForm.Bounds.Contains(myPoint.X, myPoint.Y) Then
                If (edges And IAutohide.AutohideEdges.Top) = IAutohide.AutohideEdges.Top Then
                    isAtEdge = isAtEdge Or (myPoint.Y < myForm.Top + edgeWidth And myPoint.Y >= myForm.Top)
                End If
                If (edges And IAutohide.AutohideEdges.Left) = IAutohide.AutohideEdges.Left Then
                    isAtEdge = isAtEdge Or (myPoint.X < myForm.Left + edgeWidth And myPoint.X >= myForm.Left)
                End If
                If (edges And IAutohide.AutohideEdges.Bottom) = IAutohide.AutohideEdges.Bottom Then
                    isAtEdge = isAtEdge Or (myPoint.Y >= myForm.Bottom - edgeWidth And myPoint.Y <= myForm.Bottom)
                End If
                If (edges And IAutohide.AutohideEdges.Right) = IAutohide.AutohideEdges.Right Then
                    isAtEdge = isAtEdge Or (myPoint.X >= myForm.Right - edgeWidth And myPoint.X <= myForm.Right)
                End If
            End If
            If rootHwnd = myForm.Handle Or isAtEdge Or _
                    (_hasProgramsList AndAlso ProgramsPanel IsNot Nothing AndAlso ProgramsPanel.Visible) Or _
                    (_hasUserButtons AndAlso UserButtonsPanel IsNot Nothing AndAlso UserButtonsPanel.Visible) Then
                hideCount = 0

                If Not autohide = IAutohide.AutoHideModes.Visible Or myForm.Visible = False Then
                    myForm.Visible = True
                    autohide = IAutohide.AutoHideModes.Visible
                    UpdateWorkingArea()
                End If
            End If

            If hideCount <= 30 Then
                hideCount += 1
            Else
                autohide = IAutohide.AutoHideModes.Hidden
                myForm.Visible = False
                UpdateWorkingArea()
            End If
        Else
            tmrAutohide.Enabled = False
        End If
    End Sub

    Public Sub myform_MouseScroll(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs)
        If Not hasProgramsList Then Return
        If Not ProgramsPanel.Visible Then Return
        If e.Delta > 0 Then
            previousProgPage(Nothing, Nothing)
        Else
            nextProgPage(Nothing, Nothing)
        End If
    End Sub

    Public Sub myMainPanel_Resize(ByVal sender As Object, ByVal e As EventArgs)
        modDiagnostics.LogInfo("modBusiness.myMainPanel_Resize",
            "screen=" & ScreenIndex &
            " isInit=" & isInit &
            " layoutBusy=" & personalProgramsLayoutBusy &
            " size=" & If(myMainPanel Is Nothing, "null", myMainPanel.Size.ToString()))
        If isInit AndAlso Not personalProgramsLayoutBusy Then
            Dim view1 As frmMainscreen1 = TryCast(myForm, frmMainscreen1)
            If view1 IsNot Nothing AndAlso myForm.ClientSize.Width > 1000 AndAlso myMainPanel IsNot Nothing Then
                If myMainPanel.Width < CInt(myForm.ClientSize.Width * 0.5) Then
                    view1.EnsureView1ChromeLayout()
                End If
            End If
            UpdateRegion()
        End If
    End Sub

    Public Sub UpdateRegion()
        updateRegionDepth += 1
        Using diag As DiagnosticScope = modDiagnostics.BeginScope("modBusiness.UpdateRegion",
            "screen=" & ScreenIndex &
            " depth=" & updateRegionDepth &
            " form=" & myForm.Size.ToString() &
            " panel=" & If(myMainPanel Is Nothing, "null", myMainPanel.Bounds.ToString()))
            If updateRegionDepth > 8 Then
                modDiagnostics.LogWarn("modBusiness.UpdateRegion", "HIGH REENTRANCY depth=" & updateRegionDepth & " screen=" & ScreenIndex)
            End If
            Try
                Dim myRegion As Region = New Region(New RectangleF(0, 0, myForm.Width, myForm.Height))
                Dim mainRect As New Rectangle(myForm.PointToClient(myMainPanel.PointToScreen(Drawing.Point.Empty)), myMainPanel.Size)
                modDiagnostics.LogInfo("modBusiness.UpdateRegion", "excludeRect=" & mainRect.ToString())
                myRegion.Exclude(mainRect)
                Dim oldRegion As Region = myForm.Region
                myForm.Region = myRegion
                If oldRegion IsNot Nothing Then
                    oldRegion.Dispose()
                End If
                UpdateWorkingArea()
            Catch ex As Exception
                modDiagnostics.LogException("modBusiness.UpdateRegion", ex, "screen=" & ScreenIndex & " depth=" & updateRegionDepth)
                Throw
            Finally
                updateRegionDepth -= 1
            End Try
        End Using
    End Sub

    Public Sub UpdateWorkingArea()
        Using diag As DiagnosticScope = modDiagnostics.BeginScope("modBusiness.UpdateWorkingArea",
            "screen=" & ScreenIndex &
            " autohide=" & autohide.ToString() &
            " linkedWindows=" & LinkedWindows.Count)
            Try
                Dim adjustedBounds As Rectangle
                If autohide = IAutohide.AutoHideModes.Hidden Then
                    adjustedBounds = Screen.FromHandle(myForm.Handle).Bounds
                Else
                    adjustedBounds = New Rectangle(myMainPanel.PointToScreen(Drawing.Point.Empty), myMainPanel.Size)
                End If
                modDiagnostics.LogInfo("modBusiness.UpdateWorkingArea", "bounds=" & adjustedBounds.ToString())
                resizeWorkingArea(adjustedBounds.X, adjustedBounds.Y, adjustedBounds.Width, adjustedBounds.Height)
                updateDesktopBounds(ScreenIndex, adjustedBounds)
                modDiagnostics.LogInfo("modBusiness.UpdateWorkingArea", "scheduling quick sync + weather")
                modQuickControls.ScheduleSyncQuickButton(Me)
                If hasWeather Then modHudWeather.SyncWeatherLayoutPublic(Me)
                If LinkedWindows.Count > 0 Then
                    Dim boundsCopy As Rectangle = adjustedBounds
                    Dim sourceMonitor As Integer = MonitorFromWindow(myForm.Handle, MONITOR_DEFAULTTONEAREST)
                    If myForm IsNot Nothing AndAlso Not myForm.IsDisposed Then
                        myForm.BeginInvoke(New MethodInvoker(Sub()
                                                                 NotifyLinkedWindowsWorkingArea(boundsCopy, sourceMonitor)
                                                             End Sub))
                    Else
                        NotifyLinkedWindowsWorkingArea(boundsCopy, sourceMonitor)
                    End If
                End If
            Catch ex As Exception
                modDiagnostics.LogException("modBusiness.UpdateWorkingArea", ex, "screen=" & ScreenIndex)
                Throw
            End Try
        End Using
    End Sub
End Class
