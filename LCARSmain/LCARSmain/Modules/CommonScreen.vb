Option Strict On

Imports System.Runtime.InteropServices
Imports System.Diagnostics

Public Module CommonScreen
    Public curBusiness As New List(Of modBusiness)

    Public WithEvents mainTimer As New Timer With {.Interval = 100}

    Public Sub initCommonComponents(ByVal b As modBusiness)
        If b.hasClock Then initClock(b)
        If b.hasWeather Then modHudWeather.InitDisplay(b)
        If b.hasPowerMonitor Then initPowerStatus(b)
        If b.hasTaskbar Then initWindows(b)
    End Sub

    Private Sub mainTimer_tick(ByVal sender As Object, ByVal e As EventArgs) Handles mainTimer.Tick
        updateClock()
        modHudWeather.TickWeather()
        updatePowerStatus()
        updateWindows()
        UpdateTray()
    End Sub

#Region " Clock "
    Private clockText As String = ""

    Private Sub initClock(ByVal b As modBusiness)
        If clockText = "" Then clockText = LCARS.ClockDisplay.FormatClock(Now)
        ApplyClockText(b, clockText)
        If String.Equals(b.myForm.Name, "frmMainscreen1", StringComparison.OrdinalIgnoreCase) Then
            modHudWeather.SyncWeatherLayoutPublic(b)
        End If
    End Sub

    ''' <summary>
    ''' Re-applies the cached stardate to the active clock control after view #1 expand/collapse.
    ''' </summary>
    Public Sub RefreshClockBinding(ByVal b As modBusiness)
        If Not b.hasClock OrElse b.myClock Is Nothing OrElse Not b.isInit Then Return
        If clockText = "" Then clockText = LCARS.ClockDisplay.FormatClock(Now)
        ApplyClockText(b, clockText)
        If String.Equals(b.myForm.Name, "frmMainscreen1", StringComparison.OrdinalIgnoreCase) Then
            modHudWeather.SyncWeatherLayoutPublic(b)
        End If
    End Sub

    Private Sub ApplyClockText(ByVal b As modBusiness, ByVal text As String)
        b.myClock.Text = text
        Dim barClock As LCARS.Controls.FlatButton = TryCast(b.myClock, LCARS.Controls.FlatButton)
        If barClock IsNot Nothing Then barClock.ButtonText = text
    End Sub

    Private Sub updateClock()
        Dim newText As String = LCARS.ClockDisplay.FormatClock(Now)
        If clockText <> newText Then
            clockText = newText
            For Each myBusiness As modBusiness In curBusiness
                If myBusiness.isInit And myBusiness.hasClock Then
                    ApplyClockText(myBusiness, clockText)
                    If String.Equals(myBusiness.myForm.Name, "frmMainscreen1", StringComparison.OrdinalIgnoreCase) Then
                        modHudWeather.SyncWeatherLayoutPublic(myBusiness)
                    End If
                End If
            Next
        End If
    End Sub
#End Region

#Region " Power Status "
    Private lineStatus As PowerLineStatus = PowerLineStatus.Unknown
    Private battPercent As Short = -1
    Private barNumber As Short = -1
    Private powerPlanHudText As String = ""
    Private powerPlanCheckTick As Integer = 0

    ' Tablet battery → LCARS alert. Defaults: Yellow ≤20%, Red ≤10% or Critical flag.
    ' Fire once per severity crossing; respect CancelAlert / mute (no re-spam until recovered).
    Private Const BatteryYellowPercent As Short = 20
    Private Const BatteryRedPercent As Short = 10
    Private batteryAlertArmedLevel As Integer = 0 ' 0 none, 1 yellow fired, 2 red fired
    Private batteryAlertDismissedLevel As Integer = 0
    Private batteryWasAlerting As Boolean = False

    Public Sub RefreshPowerPlanDisplay()
        powerPlanHudText = QuickControlsPower.GetActivePlanHudText()
        For Each myBusiness As modBusiness In curBusiness
            If myBusiness.isInit And myBusiness.hasPowerMonitor AndAlso myBusiness.myPowerSource IsNot Nothing Then
                myBusiness.myPowerSource.Text = powerPlanHudText
            End If
        Next
    End Sub

    Private Sub initPowerStatus(ByVal b As modBusiness)
        If powerPlanHudText = "" Then
            powerPlanHudText = QuickControlsPower.GetActivePlanHudText()
        End If
        b.myBattPercent.Text = battPercent & "%"
        b.myPowerSource.Text = powerPlanHudText
        For i As Integer = 0 To b.bars.Length - 1
            b.bars(i).Lit = i < barNumber
        Next
    End Sub

    Private Sub updatePowerStatus()
        ' SystemInformation.PowerStatus is effectively a static object. However,
        ' every time we read a property, it updates its internal values, so we
        ' should do that as little as possible
        Dim ps As PowerStatus = SystemInformation.PowerStatus
        Dim newBattPercent As Short = CShort(ps.BatteryLifePercent * 100)
        Dim newLineStatus As PowerLineStatus = ps.PowerLineStatus
        Dim newBarNumber As Short = CShort(Math.Ceiling(newBattPercent / 10.0F))
        Dim charge As BatteryChargeStatus = ps.BatteryChargeStatus

        If battPercent <> newBattPercent Then
            battPercent = newBattPercent
            For Each myBusiness As modBusiness In curBusiness
                If myBusiness.isInit And myBusiness.hasPowerMonitor Then
                    myBusiness.myBattPercent.Text = battPercent & "%"
                End If
            Next
        End If

        lineStatus = newLineStatus

        If newBarNumber <> barNumber Then
            For Each myBusiness As modBusiness In curBusiness
                If myBusiness.isInit And myBusiness.hasPowerMonitor Then
                    For i As Integer = 0 To myBusiness.bars.Length - 1
                        myBusiness.bars(i).Lit = i < newBarNumber
                    Next
                End If
            Next
            barNumber = newBarNumber
        End If

        CheckBatteryAlerts(newBattPercent, newLineStatus, charge)

        powerPlanCheckTick += 1
        If powerPlanCheckTick >= 50 Then
            powerPlanCheckTick = 0
            Dim newPlanText As String = QuickControlsPower.GetActivePlanHudText()
            If newPlanText <> powerPlanHudText Then
                powerPlanHudText = newPlanText
                For Each myBusiness As modBusiness In curBusiness
                    If myBusiness.isInit And myBusiness.hasPowerMonitor AndAlso myBusiness.myPowerSource IsNot Nothing Then
                        myBusiness.myPowerSource.Text = powerPlanHudText
                    End If
                Next
            End If
        End If
    End Sub

    ''' <summary>
    ''' Yellow Alert at ≤20% on battery; Red Alert at ≤10% or Critical. Desktop/AC/no-battery: no-op.
    ''' </summary>
    Private Sub CheckBatteryAlerts(ByVal pct As Short, ByVal line As PowerLineStatus, ByVal charge As BatteryChargeStatus)
        If (charge And BatteryChargeStatus.NoSystemBattery) = BatteryChargeStatus.NoSystemBattery Then
            batteryAlertArmedLevel = 0
            batteryAlertDismissedLevel = 0
            batteryWasAlerting = False
            Return
        End If

        ' On AC / charging: clear so the next discharge can alert again after recovery.
        If line = PowerLineStatus.Online Then
            If pct > BatteryYellowPercent Then
                batteryAlertArmedLevel = 0
                batteryAlertDismissedLevel = 0
            End If
            batteryWasAlerting = False
            Return
        End If

        Dim wantLevel As Integer = 0
        Dim isCritical As Boolean = (charge And BatteryChargeStatus.Critical) = BatteryChargeStatus.Critical
        If pct <= BatteryRedPercent OrElse isCritical Then
            wantLevel = 2
        ElseIf pct <= BatteryYellowPercent Then
            wantLevel = 1
        End If

        If wantLevel = 0 Then
            batteryAlertArmedLevel = 0
            batteryAlertDismissedLevel = 0
            batteryWasAlerting = False
            Return
        End If

        ' User dismissed (CancelAlert) while still low — do not re-fire until recovered above yellow.
        If batteryWasAlerting AndAlso Not AlertActive Then
            batteryAlertDismissedLevel = Math.Max(batteryAlertDismissedLevel, batteryAlertArmedLevel)
            batteryWasAlerting = False
        End If

        If batteryAlertDismissedLevel >= wantLevel Then Return
        If wantLevel <= batteryAlertArmedLevel AndAlso AlertActive Then Return

        If wantLevel > batteryAlertArmedLevel OrElse (wantLevel > 0 AndAlso batteryAlertArmedLevel = 0) Then
            batteryAlertArmedLevel = wantLevel
            batteryWasAlerting = True
            ' GeneralAlert(0)=Red, (1)=Yellow — same as voice / alert buttons.
            If wantLevel >= 2 Then
                GeneralAlert(0)
            Else
                GeneralAlert(1)
            End If
        End If
    End Sub
#End Region

#Region " Window tracking "
    Private windowList As New Dictionary(Of Integer, ExternalApp)
    Private newWindowList As New Dictionary(Of Integer, ExternalApp)
    Private topmostWindow As Integer = 0

    <Flags()> _
    Public Enum WindowUpdateFlags As Integer
        None = 0
        Text = 1
        State = 2
        Topmost = 4
    End Enum

    Private Sub initWindows(ByVal b As modBusiness)
        Dim bScreen As Integer = MonitorFromWindow(b.myForm.Handle.ToInt32(), MONITOR_DEFAULTTONEAREST)
        For Each app As ExternalApp In windowList.Values
            If app.hScreen = bScreen Then
                b.AddWindow(app)
            End If
        Next
    End Sub

    Private Sub updateWindows()
        'Get new window information
        newWindowList.Clear()
        EnumWindows(New EnumCallBack(AddressOf fEnumWindowsCallBack), 0)
        Dim newTopmost As Integer = GetForegroundWindow()

        'See which screens are available
        Dim screenDict As New Dictionary(Of Integer, modBusiness)
        For Each b As modBusiness In curBusiness
            If b.isInit And b.hasTaskbar Then
                screenDict.Add(MonitorFromWindow(b.myForm.Handle.ToInt32(), MONITOR_DEFAULTTONEAREST), b)
            End If
        Next

        'Get the list of windows to remove
        Dim removeList As New List(Of Integer)
        For Each myKey As Integer In windowList.Keys
            If Not newWindowList.ContainsKey(myKey) Then
                removeList.Add(myKey)
            End If
        Next
        For Each myKey As Integer In removeList
            Dim removedApp As ExternalApp = windowList(myKey)
            If screenDict.ContainsKey(removedApp.hScreen) Then
                screenDict(removedApp.hScreen).RemoveWindow(removedApp)
                'If not in the screen list, assume that it's already gone
            End If
            windowList.Remove(myKey)
        Next

        'Update or add remaining windows
        For Each mykey As Integer In newWindowList.Keys
            Dim newApp As ExternalApp = newWindowList(mykey)
            If windowList.ContainsKey(mykey) Then
                Dim oldApp As ExternalApp = windowList(mykey)
                If newApp.hScreen = oldApp.hScreen Then
                    'Update window properties
                    Dim flags As WindowUpdateFlags = WindowUpdateFlags.None
                    If newApp.Text <> oldApp.Text Then
                        oldApp.Text = newApp.Text
                        flags = WindowUpdateFlags.Text
                    End If
                    If newApp.Minimized <> oldApp.Minimized Then
                        oldApp.Minimized = newApp.Minimized
                        flags = flags Or WindowUpdateFlags.State
                    End If
                    If flags <> WindowUpdateFlags.None AndAlso _
                            screenDict.ContainsKey(oldApp.hScreen) Then
                        screenDict(oldApp.hScreen).UpdateWindow(oldApp, flags)
                    End If
                Else
                    'Remove from old screen, then add to new screen
                    If screenDict.ContainsKey(oldApp.hScreen) Then
                        screenDict(oldApp.hScreen).RemoveWindow(oldApp)
                    End If
                    windowList(mykey) = newApp
                    If screenDict.ContainsKey(newApp.hScreen) Then
                        screenDict(newApp.hScreen).AddWindow(newApp)
                    Else
                        'TODO: Add default handler
                    End If
                End If
            Else
                windowList.Add(mykey, newWindowList(mykey))
                If screenDict.ContainsKey(newApp.hScreen) Then
                    screenDict(newApp.hScreen).AddWindow(newApp)
                Else
                    'TODO: Add default handler
                End If
            End If
        Next
        If newTopmost <> topmostWindow Then
            If windowList.ContainsKey(topmostWindow) Then
                Dim oldTop As ExternalApp = windowList(topmostWindow)
                oldTop.topmost = False
                If screenDict.ContainsKey(oldTop.hScreen) Then
                    screenDict(oldTop.hScreen).UpdateWindow(oldTop, WindowUpdateFlags.Topmost)
                End If
            End If
            If windowList.ContainsKey(newTopmost) Then
                Dim newTop As ExternalApp = windowList(newTopmost)
                newTop.topmost = True
                If screenDict.ContainsKey(newTop.hScreen) Then
                    screenDict(newTop.hScreen).UpdateWindow(newTop, WindowUpdateFlags.Topmost)
                End If
            End If
            topmostWindow = newTopmost
        End If
    End Sub

    Private Function fEnumWindowsCallBack(ByVal hwnd As Integer, ByVal lParam As Integer) As Integer
        Const continueEnumeration As Integer = 1 ' True
        Const stopEnumration As Integer = 0 ' False
        'Stop if main timer disabled
        If Not mainTimer.Enabled Then Return stopEnumration
        'Hidden windows should not be shown
        If Not IsWindowVisible(hwnd) Then Return continueEnumeration
        'Windows with a parent should not be shown
        If GetParent(hwnd) <> 0 Then Return continueEnumeration

        'Windows spawned by the real Windows shell must not appear on the LCARS taskbar.
        If IsBlockedShellWindow(hwnd) Then Return continueEnumeration

        Dim bNoOwner As Boolean = (GetWindow(hwnd, GW_OWNER) = 0)
        Dim lExStyle As Integer = GetWindowLong_Safe(hwnd, GWL_EXSTYLE)
        Dim toolWindow As Boolean = (lExStyle And WS_EX_TOOLWINDOW) = 0
        Dim appWindow As Boolean = (lExStyle And WS_EX_APPWINDOW) = WS_EX_APPWINDOW
        Dim modernApp As Boolean = (lExStyle And WS_EX_NOREDIRECTIONBITMAP) = WS_EX_NOREDIRECTIONBITMAP

        If ((toolWindow And bNoOwner) Or (appWindow And Not bNoOwner)) Then
            If modernApp Then
                'Filters out suspended apps (fairly quickly, but not immediately)
                If IsHungAppWindow(hwnd) Then
                    Return continueEnumeration
                End If
            End If
            'Take out windows with no caption
            If GetWindowTextLength(hwnd) = 0 Then
                Return continueEnumeration
            End If

            'Check to see if it's on the current virtual desktop
            Try
                If Not VirtualDesktops.IsWindowOnCurrentVirtualDesktop(New IntPtr(hwnd)) Then
                    Return continueEnumeration
                End If
            Catch ex As COMException
            End Try

            Dim myApp As New ExternalApp(hwnd)
            newWindowList.Add(hwnd, myApp)
        End If
        Return continueEnumeration
    End Function

    Private Function IsBlockedShellWindow(ByVal hwnd As Integer) As Boolean
        Dim pid As Integer = 0
        GetWindowThreadProcessId(hwnd, pid)
        If pid > 0 Then
            Try
                Using proc As Process = Process.GetProcessById(pid)
                    Select Case proc.ProcessName.ToLowerInvariant()
                        Case "shellexperiencehost", "startmenuexperiencehost", "applicationframehost"
                            Return True
                        Case "systemsettings", "searchui", "searchapp"
                            ' Work-area / SPI broadcasts leave these as sticky taskbar ghosts.
                            Return True
                        Case "explorer"
                            If modShellFallback.IsLcarsRegisteredShell() Then Return True
                    End Select
                End Using
            Catch
            End Try
        End If

        ' Cloaked UWP windows report IsWindowVisible=True but have no real UI — classic
        ' unreclaimable "Settings" remnant after updates.
        If IsWindowCloaked(hwnd) Then Return True

        Dim titleLen As Integer = GetWindowTextLength(hwnd)
        If titleLen > 0 Then
            Dim title As String = New String(ChrW(0), titleLen)
            GetWindowText(hwnd, title, titleLen + 1)
            title = title.TrimEnd(ChrW(0))
            If title.IndexOf("Shell Experience Host", StringComparison.OrdinalIgnoreCase) >= 0 Then
                Return True
            End If
        End If
        Return False
    End Function

    Private Const DWMWA_CLOAKED As Integer = 14

    <DllImport("dwmapi.dll")> _
    Private Function DwmGetWindowAttribute(ByVal hwnd As IntPtr, ByVal dwAttribute As Integer, ByRef pvAttribute As Integer, ByVal cbAttribute As Integer) As Integer
    End Function

    Private Function IsWindowCloaked(ByVal hwnd As Integer) As Boolean
        Try
            Dim cloaked As Integer = 0
            Dim hr As Integer = DwmGetWindowAttribute(New IntPtr(hwnd), DWMWA_CLOAKED, cloaked, 4)
            Return (hr = 0 AndAlso cloaked <> 0)
        Catch
            Return False
        End Try
    End Function
#End Region
End Module
