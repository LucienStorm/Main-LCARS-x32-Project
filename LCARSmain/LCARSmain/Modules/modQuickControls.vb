Option Strict On

Imports System.Diagnostics
Imports System.IO
Imports System.Management
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Windows.Forms
Imports Microsoft.Win32
Imports LCARS.Controls

''' <summary>
''' Attaches the Quick Controls launcher beside the tray button and hosts system helpers.
''' </summary>
Public Module modQuickControls

    Private quickButtonByScreen As New Dictionary(Of Integer, LCARS.LCARSbuttonClass)
    Private quickBusinessByScreen As New Dictionary(Of Integer, modBusiness)
    Private quickButtonHostByScreen As New Dictionary(Of Integer, Control)
    Private batteryHudByScreen As New HashSet(Of Integer)
    Private activePanel As frmQuickControls = Nothing
    Private syncingQuickByScreen As New HashSet(Of Integer)
    Private pendingQuickSyncByScreen As New HashSet(Of Integer)

    ''' <summary>
    ''' Adds a QUICK button left of the tray panel, matching ShowTrayButton style,
    ''' and keeps it glued to the tray as LCARS layouts move/resize.
    ''' </summary>
    Public Sub AttachToTray(ByVal b As modBusiness)
        If b.myTrayPanel Is Nothing Then Return
        Dim host As Control = ResolveQuickButtonHost(b)
        If host Is Nothing Then Return
        If quickButtonByScreen.ContainsKey(b.ScreenIndex) Then
            EnsureQuickButtonOnHost(b, quickButtonByScreen(b.ScreenIndex))
            SyncQuickButton(b)
            Return
        End If

        Dim btn As LCARS.LCARSbuttonClass = CreateQuickBarButton()
        btn.ButtonText = "QUICK"
        btn.Text = "QUICK"
        btn.Color = LCARS.LCARScolorStyles.SystemFunction
        btn.Size = New Size(72, 20)
        btn.Anchor = AnchorStyles.None
        btn.Name = "btnQuickControls" & b.ScreenIndex.ToString()
        btn.Beeping = True
        btn.ButtonTextAlign = ContentAlignment.MiddleCenter
        EnsureQuickButtonOnHost(b, btn)
        btn.BringToFront()
        AddHandler btn.Click, Sub(sender As Object, e As EventArgs)
                                  TogglePanel(b.myForm, btn)
                              End Sub
        quickButtonByScreen(b.ScreenIndex) = btn
        quickBusinessByScreen(b.ScreenIndex) = b
        quickButtonHostByScreen(b.ScreenIndex) = host

        AddHandler b.myTrayPanel.LocationChanged, Sub(sender As Object, e As EventArgs) ScheduleSyncQuickButton(b)
        AddHandler b.myTrayPanel.SizeChanged, Sub(sender As Object, e As EventArgs) ScheduleSyncQuickButton(b)
        AddHandler b.myTrayPanel.Move, Sub(sender As Object, e As EventArgs) ScheduleSyncQuickButton(b)
        If b.myAppsPanel IsNot Nothing Then
            AddHandler b.myAppsPanel.SizeChanged, Sub(sender As Object, e As EventArgs) ScheduleSyncQuickButton(b)
            AddHandler b.myAppsPanel.LocationChanged, Sub(sender As Object, e As EventArgs) ScheduleSyncQuickButton(b)
        End If
        If host IsNot Nothing Then
            AddHandler host.Resize, Sub(sender As Object, e As EventArgs) ScheduleSyncQuickButton(b)
        End If
        If b.myShowTrayButton IsNot Nothing Then
            AddHandler b.myShowTrayButton.VisibleChanged, Sub(sender As Object, e As EventArgs) ScheduleSyncQuickButton(b)
        End If
        If b.myUserButtons IsNot Nothing Then
            AddHandler b.myUserButtons.LocationChanged, Sub(sender As Object, e As EventArgs) ScheduleSyncQuickButton(b)
            AddHandler b.myUserButtons.SizeChanged, Sub(sender As Object, e As EventArgs) ScheduleSyncQuickButton(b)
        End If
        Dim mainBar As Control = FindMainBarPanel(b.myForm)
        If mainBar IsNot Nothing Then
            AddHandler mainBar.LocationChanged, Sub(sender As Object, e As EventArgs) ScheduleSyncQuickButton(b)
            AddHandler mainBar.SizeChanged, Sub(sender As Object, e As EventArgs) ScheduleSyncQuickButton(b)
        End If

        SyncQuickButton(b)
    End Sub

    Private Function ResolveQuickButtonHost(ByVal b As modBusiness) As Control
        If String.Equals(b.myForm.Name, "frmMainscreen1", StringComparison.OrdinalIgnoreCase) Then
            Dim mainBar As Control = FindMainBarPanel(b.myForm)
            If mainBar IsNot Nothing AndAlso b.myUserButtons IsNot Nothing Then
                Return mainBar
            End If
        End If

        If IsPersonalProgramsPanelOpen(b) Then
            Dim mainBarOpen As Control = FindMainBarPanel(b.myForm)
            If mainBarOpen IsNot Nothing Then Return mainBarOpen
        End If

        Dim mainBarDefault As Control = FindMainBarPanel(b.myForm)
        If mainBarDefault IsNot Nothing AndAlso b.myUserButtons IsNot Nothing Then
            Return mainBarDefault
        End If
        If b.myTrayPanel IsNot Nothing AndAlso b.myTrayPanel.Parent IsNot Nothing Then
            Return b.myTrayPanel.Parent
        End If
        Return Nothing
    End Function

    Private Sub EnsureQuickButtonOnHost(ByVal b As modBusiness, ByVal btn As Control)
        Dim host As Control = ResolveQuickButtonHost(b)
        If host Is Nothing OrElse btn Is Nothing Then Return
        quickButtonHostByScreen(b.ScreenIndex) = host
        If btn.Parent Is host Then Return
        If btn.Parent IsNot Nothing Then
            btn.Parent.Controls.Remove(btn)
        End If
        host.Controls.Add(btn)
    End Sub

    ''' <summary>
    ''' Defers QUICK reposition so Personal Programs / main-panel resizes cannot re-enter layout.
    ''' </summary>
    Public Sub ScheduleSyncQuickButton(ByVal b As modBusiness)
        If b Is Nothing OrElse b.myForm Is Nothing OrElse b.myForm.IsDisposed Then Return
        If Not quickButtonByScreen.ContainsKey(b.ScreenIndex) Then Return
        If pendingQuickSyncByScreen.Contains(b.ScreenIndex) Then
            modDiagnostics.LogInfo("modQuickControls.ScheduleSyncQuickButton", "screen=" & b.ScreenIndex & " already pending")
            Return
        End If
        modDiagnostics.LogInfo("modQuickControls.ScheduleSyncQuickButton", "screen=" & b.ScreenIndex & " queued")
        pendingQuickSyncByScreen.Add(b.ScreenIndex)
        b.myForm.BeginInvoke(New MethodInvoker(Sub()
                                                   pendingQuickSyncByScreen.Remove(b.ScreenIndex)
                                                   SyncQuickButton(b)
                                               End Sub))
    End Sub

    ''' <summary>
    ''' Repositions QUICK beside the tray after any LCARS bar layout change.
    ''' </summary>
    Public Sub SyncQuickButton(ByVal b As modBusiness)
        If b Is Nothing Then Return
        If Not quickButtonByScreen.ContainsKey(b.ScreenIndex) Then Return
        If syncingQuickByScreen.Contains(b.ScreenIndex) Then
            modDiagnostics.LogWarn("modQuickControls.SyncQuickButton", "screen=" & b.ScreenIndex & " re-entry blocked")
            Return
        End If

        Using diag As DiagnosticScope = modDiagnostics.BeginScope("modQuickControls.SyncQuickButton", "screen=" & b.ScreenIndex)
            Dim btn As LCARS.LCARSbuttonClass = quickButtonByScreen(b.ScreenIndex)
            If btn Is Nothing OrElse btn.IsDisposed Then Return
            If b.myTrayPanel Is Nothing OrElse b.myTrayPanel.IsDisposed Then Return

            Dim host As Control = GetQuickButtonHost(b)
            If host Is Nothing Then Return
            EnsureQuickButtonOnHost(b, btn)

            syncingQuickByScreen.Add(b.ScreenIndex)
            Try
                ApplyQuickButtonSize(b, btn)
                Dim loc As Point = GetQuickButtonPosition(b, host, btn)
                modDiagnostics.LogInfo("modQuickControls.SyncQuickButton", "screen=" & b.ScreenIndex & " loc=" & loc.ToString() & " size=" & btn.Size.ToString())
                btn.Location = loc
                btn.Visible = True
                btn.BringToFront()
                Dim view1 As frmMainscreen1 = TryCast(b.myForm, frmMainscreen1)
                If view1 IsNot Nothing Then
                    view1.EnsureSpeechRowFill()
                    modHudWeather.SyncWeatherLayoutPublic(b)
                End If
            Catch ex As Exception
                modDiagnostics.LogException("modQuickControls.SyncQuickButton", ex, "screen=" & b.ScreenIndex)
                Throw
            Finally
                syncingQuickByScreen.Remove(b.ScreenIndex)
            End Try
        End Using
    End Sub

    Private Function IsPersonalProgramsPanelOpen(ByVal b As modBusiness) As Boolean
        Return b.UserButtonsPanel IsNot Nothing AndAlso b.UserButtonsPanel.Visible
    End Function

    Private Function IsView1PersonalBarQuick(ByVal b As modBusiness) As Boolean
        Return String.Equals(b.myForm.Name, "frmMainscreen1", StringComparison.OrdinalIgnoreCase) AndAlso
               b.myUserButtons IsNot Nothing AndAlso Not b.myUserButtons.IsDisposed
    End Function

    Private Sub ApplyQuickButtonSize(ByVal b As modBusiness, ByVal btn As LCARS.LCARSbuttonClass)
        If IsView1PersonalBarQuick(b) OrElse (IsPersonalProgramsPanelOpen(b) AndAlso b.myUserButtons IsNot Nothing AndAlso Not b.myUserButtons.IsDisposed) Then
            btn.Height = Math.Max(18, b.myUserButtons.Height)
            btn.Width = Math.Max(58, TextRenderer.MeasureText("QUICK", btn.Font).Width + 12)
            btn.ButtonTextAlign = ContentAlignment.MiddleCenter
            Return
        End If

        Dim template As LCARS.LCARSbuttonClass = b.myShowTrayButton
        If template IsNot Nothing AndAlso template.Visible Then
            btn.Height = template.Height
            btn.Width = Math.Max(58, Math.Max(template.Width, TextRenderer.MeasureText("QUICK", btn.Font).Width + 12))
        ElseIf b.myHideTrayButton IsNot Nothing AndAlso b.myHideTrayButton.Visible Then
            btn.Height = b.myHideTrayButton.Height
            btn.Width = Math.Max(58, Math.Max(b.myHideTrayButton.Width, TextRenderer.MeasureText("QUICK", btn.Font).Width + 12))
        Else
            btn.Height = Math.Max(20, b.myTrayPanel.Height)
            btn.Width = Math.Max(58, TextRenderer.MeasureText("QUICK", btn.Font).Width + 12)
        End If
        btn.ButtonTextAlign = ContentAlignment.MiddleCenter
    End Sub

    ''' <summary>
    ''' Tray-row fallback when Personal Programs is unavailable; otherwise QUICK sits on the blue bar beside it.
    ''' </summary>
    Private Function GetTrayRowInHost(ByVal b As modBusiness, ByVal host As Control) As Point
        Dim topOffset As Integer = 0
        If b.myShowTrayButton IsNot Nothing AndAlso b.myShowTrayButton.Visible Then
            topOffset = b.myShowTrayButton.Top
        ElseIf b.myHideTrayButton IsNot Nothing AndAlso b.myHideTrayButton.Visible Then
            topOffset = b.myHideTrayButton.Top
        End If

        If b.myTrayPanel.Parent Is host Then
            Return New Point(b.myTrayPanel.Left, b.myTrayPanel.Top + topOffset)
        End If

        Return host.PointToClient(b.myTrayPanel.PointToScreen(New Point(0, topOffset)))
    End Function

    Private Function GetQuickButtonPosition(ByVal b As modBusiness, ByVal host As Control, ByVal btn As LCARS.LCARSbuttonClass) As Point
        If IsView1PersonalBarQuick(b) OrElse (IsPersonalProgramsPanelOpen(b) AndAlso b.myUserButtons IsNot Nothing AndAlso Not b.myUserButtons.IsDisposed) Then
            Return GetPersonalProgramsAnchor(b, host, btn.Width)
        End If

        Dim trayRow As Point = GetTrayRowInHost(b, host)
        Return New Point(Math.Max(0, trayRow.X - btn.Width - 2), trayRow.Y)
    End Function

    Private Function GetPersonalProgramsAnchor(ByVal b As modBusiness, ByVal host As Control, ByVal btnWidth As Integer) As Point
        Dim personal As LCARS.LCARSbuttonClass = b.myUserButtons
        If personal.Parent Is host Then
            Return New Point(Math.Max(0, personal.Left - btnWidth - 2), personal.Top)
        End If

        Dim personalScreen As Point = personal.PointToScreen(Point.Empty)
        Dim hostPoint As Point = host.PointToClient(personalScreen)
        Return New Point(Math.Max(0, hostPoint.X - btnWidth - 2), hostPoint.Y)
    End Function

    Private Function GetQuickButtonHost(ByVal b As modBusiness) As Control
        If quickButtonHostByScreen.ContainsKey(b.ScreenIndex) Then
            Return quickButtonHostByScreen(b.ScreenIndex)
        End If
        Return ResolveQuickButtonHost(b)
    End Function

    Private Function FindMainBarPanel(ByVal root As Control) As Control
        Dim matches() As Control = root.Controls.Find("pnlMainBar", True)
        If matches.Length > 0 Then Return matches(0)
        Return Nothing
    End Function

    Private Sub EnsureQuickButtonParent(ByVal btn As Control, ByVal parent As Control)
        If parent Is Nothing OrElse btn Is Nothing Then Return
        If btn.Parent Is parent Then Return
        If btn.Parent IsNot Nothing Then
            btn.Parent.Controls.Remove(btn)
        End If
        parent.Controls.Add(btn)
    End Sub

    Private Function CreateQuickBarButton() As LCARS.LCARSbuttonClass
        Dim fb As New FlatButton()
        fb.ButtonTextAlign = ContentAlignment.MiddleCenter
        Return fb
    End Function

    Private Sub ReserveQuickButtonSpace(ByVal b As modBusiness, ByVal extraWidth As Integer)
        If b.myAppsPanel Is Nothing OrElse b.myTrayPanel Is Nothing Then Return
        If extraWidth <= 0 Then Return
        Try
            b.myAppsPanel.Width = Math.Max(0, b.myAppsPanel.Width - extraWidth)
            b.myTrayPanel.Left = Math.Max(0, b.myTrayPanel.Left - extraWidth)
        Catch
        End Try
    End Sub

    ''' <summary>
    ''' Makes the entire battery HUD panel cycle Windows power plans when tapped.
    ''' </summary>
    Public Sub AttachBatteryHud(ByVal b As modBusiness)
        If Not b.hasPowerMonitor Then Return
        If b.myBattPanel Is Nothing Then Return
        If batteryHudByScreen.Contains(b.ScreenIndex) Then Return
        batteryHudByScreen.Add(b.ScreenIndex)
        WireBatteryHudClick(b.myBattPanel, b)
    End Sub

    Private Sub WireBatteryHudClick(ByVal root As Control, ByVal b As modBusiness)
        root.Cursor = Cursors.Hand
        AddHandler root.Click, Sub(sender As Object, e As EventArgs)
                                   HandleBatteryHudClick(b)
                               End Sub
        For Each child As Control In root.Controls
            WireBatteryHudClick(child, b)
        Next
    End Sub

    Private Sub HandleBatteryHudClick(ByVal b As modBusiness)
        QuickControlsPower.CyclePowerPlan()
        CommonScreen.RefreshPowerPlanDisplay()
    End Sub

    ''' <summary>
    ''' Shows or hides the quick controls drop-down overlay under the QUICK button.
    ''' </summary>
    Public Sub TogglePanel(ByVal owner As Form, ByVal anchor As Control)
        If activePanel IsNot Nothing AndAlso Not activePanel.IsDisposed Then
            If activePanel.Visible Then
                activePanel.Close()
                activePanel = Nothing
                Return
            End If
        End If
        activePanel = New frmQuickControls()
        AddHandler activePanel.FormClosed, Sub(sender As Object, e As FormClosedEventArgs)
                                               activePanel = Nothing
                                           End Sub
        activePanel.Show(owner)
        activePanel.PositionAsDropdown(anchor)
        activePanel.Activate()
    End Sub

    Private Function CreateMatchingTrayStyleButton(ByVal template As LCARS.LCARSbuttonClass) As LCARS.LCARSbuttonClass
        If TypeOf template Is HalfPillButton Then
            Dim hp As New HalfPillButton()
            hp.ButtonTextAlign = template.ButtonTextAlign
            Return hp
        End If
        If TypeOf template Is FlatButton Then
            Dim fb As New FlatButton()
            fb.ButtonTextAlign = template.ButtonTextAlign
            Return fb
        End If
        Return New FlatButton()
    End Function

    ''' <summary>
    ''' Runs a hidden process and returns merged stdout/stderr.
    ''' </summary>
    Friend Function RunCommand(ByVal fileName As String, ByVal arguments As String) As String
        Dim unusedExit As Integer = 0
        Return RunCommandEx(fileName, arguments, unusedExit)
    End Function

    ''' <summary>
    ''' Runs a hidden process and returns merged stdout/stderr plus the process exit code.
    ''' </summary>
    Friend Function RunCommandEx(ByVal fileName As String, ByVal arguments As String, ByRef exitCode As Integer) As String
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
            If output = "" Then Return err
            Return output
        End Using
    End Function

    Friend Function RunPowerShell(ByVal script As String) As String
        Return RunCommand("powershell.exe", "-NoProfile -ExecutionPolicy Bypass -Command " & Quote(script))
    End Function

    Private Function Quote(ByVal text As String) As String
        Return """" & text.Replace("""", "'") & """"
    End Function

End Module

''' <summary>
''' Master volume via Core Audio (IAudioEndpointVolume). Hardware keys and Mute use the same endpoint.
''' </summary>
Friend Class QuickControlsAudio
    Private Const CLSID_MMDeviceEnumerator As String = "BCDE0395-E52F-467C-8E3D-C4579291692E"
    Private Const IID_IMMDevice As String = "D666063F-1587-4E43-81F1-B948E807363F"
    Private Const IID_IMMDeviceEnumerator As String = "A95664D2-9614-4F35-A746-DE8DB63617E6"
    Private Const IID_IAudioEndpointVolume As String = "5CDF2C82-841E-4546-9722-0CF74078229A"
    Private Const IID_IAudioEndpointVolumeCallback As String = "657804FA-D6AD-4496-8A60-352752AF4F89"
    Private Const CLSCTX_INPROC_SERVER As Integer = 1
    Private Const eRender As Integer = 0
    Private Const eConsole As Integer = 0
    Private Const eMultimedia As Integer = 1
    Private Const WM_APPCOMMAND As Integer = &H319
    Private Const APPCOMMAND_VOLUME_MUTE As Integer = 8
    Private Const APPCOMMAND_VOLUME_DOWN As Integer = 9
    Private Const APPCOMMAND_VOLUME_UP As Integer = 10
    Private Const HWND_BROADCAST As Integer = &HFFFF

    Private Shared ReadOnly volumeLock As New Object()
    Private Shared cachedVolume As IAudioEndpointVolume
    Private Shared volumeCallback As EndpointVolumeCallback
    Private Shared volumeCallbackPtr As IntPtr
    Private Shared registeredVolume As IAudioEndpointVolume
    Private Shared volumeListenerCount As Integer = 0
    Private Shared lastLoggedVolume As Integer = -1

    Public Shared Event VolumeChanged As EventHandler

    <ComImport(), Guid(IID_IAudioEndpointVolumeCallback), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Private Interface IAudioEndpointVolumeCallback
        <PreserveSig()> Function OnNotify(ByVal pNotify As IntPtr) As Integer
    End Interface

    <ComVisible(True), ClassInterface(ClassInterfaceType.None)>
    Private NotInheritable Class EndpointVolumeCallback
        Implements IAudioEndpointVolumeCallback

        Public Event VolumeChanged As EventHandler

        Public Function OnNotify(ByVal pNotify As IntPtr) As Integer Implements IAudioEndpointVolumeCallback.OnNotify
            RaiseEvent VolumeChanged(Me, EventArgs.Empty)
            Return 0
        End Function
    End Class

    <ComImport(), Guid(IID_IAudioEndpointVolume), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Private Interface IAudioEndpointVolume
        <PreserveSig()> Function RegisterControlChangeNotify(ByVal pNotify As IntPtr) As Integer
        <PreserveSig()> Function UnregisterControlChangeNotify(ByVal pNotify As IntPtr) As Integer
        <PreserveSig()> Function GetChannelCount(<Out()> ByRef pnChannelCount As UInteger) As Integer
        <PreserveSig()> Function SetMasterVolumeLevel(ByVal fLevelDB As Single, ByRef pguidEventContext As Guid) As Integer
        <PreserveSig()> Function SetMasterVolumeLevelScalar(ByVal fLevel As Single, ByRef pguidEventContext As Guid) As Integer
        <PreserveSig()> Function GetMasterVolumeLevel(<Out()> ByRef pfLevelDB As Single) As Integer
        <PreserveSig()> Function GetMasterVolumeLevelScalar(<Out()> ByRef pfLevel As Single) As Integer
        <PreserveSig()> Function SetChannelVolumeLevel(ByVal nChannel As UInteger, ByVal fLevelDB As Single, ByRef pguidEventContext As Guid) As Integer
        <PreserveSig()> Function SetChannelVolumeLevelScalar(ByVal nChannel As UInteger, ByVal fLevel As Single, ByRef pguidEventContext As Guid) As Integer
        <PreserveSig()> Function GetChannelVolumeLevel(ByVal nChannel As UInteger, <Out()> ByRef pfLevelDB As Single) As Integer
        <PreserveSig()> Function GetChannelVolumeLevelScalar(ByVal nChannel As UInteger, <Out()> ByRef pfLevel As Single) As Integer
        ' Win32 BOOL is 4 bytes — use Integer, not Boolean.
        <PreserveSig()> Function SetMute(ByVal bMute As Integer, ByRef pguidEventContext As Guid) As Integer
        <PreserveSig()> Function GetMute(<Out()> ByRef pbMute As Integer) As Integer
    End Interface

    <ComImport(), Guid(IID_IMMDeviceEnumerator), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Private Interface IMMDeviceEnumerator
        <PreserveSig()> Function EnumAudioEndpoints(ByVal dataFlow As Integer, ByVal dwStateMask As Integer, <Out()> ByRef ppDevices As Object) As Integer
        <PreserveSig()> Function GetDefaultAudioEndpoint(ByVal dataFlow As Integer, ByVal role As Integer, <Out()> ByRef ppDevice As IMMDevice) As Integer
    End Interface

    <ComImport(), Guid(IID_IMMDevice), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Private Interface IMMDevice
        <PreserveSig()> Function Activate(ByRef iid As Guid, ByVal dwClsCtx As Integer, ByVal pActivationParams As IntPtr, <Out(), MarshalAs(UnmanagedType.IUnknown)> ByRef ppInterface As Object) As Integer
    End Interface

    <DllImport("user32.dll")>
    Private Shared Function SendMessage(ByVal hWnd As IntPtr, ByVal msg As Integer, ByVal wParam As IntPtr, ByVal lParam As IntPtr) As IntPtr
    End Function

    Private Shared Function CreateVolumeInterface(ByVal role As Integer) As IAudioEndpointVolume
        Try
            Dim enumType As Type = Type.GetTypeFromCLSID(New Guid(CLSID_MMDeviceEnumerator))
            If enumType Is Nothing Then Return Nothing
            Dim enumerator As IMMDeviceEnumerator = CType(Activator.CreateInstance(enumType), IMMDeviceEnumerator)
            Dim device As IMMDevice = Nothing
            Dim hr As Integer = enumerator.GetDefaultAudioEndpoint(eRender, role, device)
            If hr <> 0 OrElse device Is Nothing Then Return Nothing
            Dim iid As Guid = New Guid(IID_IAudioEndpointVolume)
            Dim obj As Object = Nothing
            hr = device.Activate(iid, CLSCTX_INPROC_SERVER, IntPtr.Zero, obj)
            If hr <> 0 OrElse obj Is Nothing Then Return Nothing
            Return CType(obj, IAudioEndpointVolume)
        Catch ex As Exception
            modDiagnostics.LogException("QuickControlsAudio.CreateVolumeInterface", ex)
            Return Nothing
        End Try
    End Function

    Private Shared Function TryGetVolumeInterface() As IAudioEndpointVolume
        SyncLock volumeLock
            If cachedVolume IsNot Nothing Then Return cachedVolume
            ' Console role matches hardware volume keys on most tablets.
            cachedVolume = CreateVolumeInterface(eConsole)
            If cachedVolume Is Nothing Then cachedVolume = CreateVolumeInterface(eMultimedia)
            Return cachedVolume
        End SyncLock
    End Function

    Private Shared Sub InvalidateVolumeCache()
        SyncLock volumeLock
            cachedVolume = Nothing
        End SyncLock
    End Sub

    Public Shared Function GetVolumePercent() As Integer
        Try
            Dim vol As IAudioEndpointVolume = TryGetVolumeInterface()
            If vol IsNot Nothing Then
                Dim level As Single
                If vol.GetMasterVolumeLevelScalar(level) = 0 Then
                    Return Math.Max(0, Math.Min(100, CInt(Math.Round(level * 100.0F))))
                End If
                InvalidateVolumeCache()
            End If
        Catch
            InvalidateVolumeCache()
        End Try
        Return -1
    End Function

    Public Shared Sub SetVolumePercent(ByVal percent As Integer)
        Dim target As Integer = Math.Max(0, Math.Min(100, percent))
        Try
            Dim vol As IAudioEndpointVolume = TryGetVolumeInterface()
            If vol IsNot Nothing Then
                Dim ctx As Guid = Guid.Empty
                Dim scalar As Single = target / 100.0F
                If vol.SetMasterVolumeLevelScalar(scalar, ctx) = 0 Then
                    LogVolumeIfChanged(target, "set")
                    Return
                End If
                InvalidateVolumeCache()
            End If
        Catch
            InvalidateVolumeCache()
        End Try
        NudgeVolumeToPercent(target / 100.0F)
    End Sub

    ''' <summary>
    ''' Subscribes to endpoint volume change notifications (hardware keys, system mixer).
    ''' </summary>
    Public Shared Sub AddVolumeListener()
        volumeListenerCount += 1
        If volumeListenerCount > 1 Then Return
        EnsureVolumeNotifications()
    End Sub

    ''' <summary>
    ''' Releases endpoint volume notifications when the last listener closes.
    ''' </summary>
    Public Shared Sub RemoveVolumeListener()
        If volumeListenerCount <= 0 Then Return
        volumeListenerCount -= 1
        If volumeListenerCount > 0 Then Return
        ReleaseVolumeNotifications()
        InvalidateVolumeCache()
    End Sub

    Private Shared Sub EnsureVolumeNotifications()
        If volumeCallback IsNot Nothing Then Return
        Try
            Dim vol As IAudioEndpointVolume = TryGetVolumeInterface()
            If vol Is Nothing Then
                modDiagnostics.LogInfo("QuickControlsAudio", "volume notifications unavailable (no endpoint)")
                Return
            End If
            volumeCallback = New EndpointVolumeCallback()
            AddHandler volumeCallback.VolumeChanged, AddressOf ForwardVolumeChanged
            volumeCallbackPtr = Marshal.GetComInterfaceForObject(volumeCallback, GetType(IAudioEndpointVolumeCallback))
            Dim hr As Integer = vol.RegisterControlChangeNotify(volumeCallbackPtr)
            If hr = 0 Then
                registeredVolume = vol
                modDiagnostics.LogInfo("QuickControlsAudio", "volume notifications registered")
            Else
                modDiagnostics.LogInfo("QuickControlsAudio", "RegisterControlChangeNotify hr=" & hr.ToString())
                ReleaseVolumeNotifications()
            End If
        Catch ex As Exception
            modDiagnostics.LogException("QuickControlsAudio.EnsureVolumeNotifications", ex)
            ReleaseVolumeNotifications()
        End Try
    End Sub

    Private Shared Sub ReleaseVolumeNotifications()
        Try
            If registeredVolume IsNot Nothing AndAlso volumeCallbackPtr <> IntPtr.Zero Then
                registeredVolume.UnregisterControlChangeNotify(volumeCallbackPtr)
            End If
        Catch
        End Try
        If volumeCallback IsNot Nothing Then
            RemoveHandler volumeCallback.VolumeChanged, AddressOf ForwardVolumeChanged
        End If
        If volumeCallbackPtr <> IntPtr.Zero Then
            Try
                Marshal.Release(volumeCallbackPtr)
            Catch
            End Try
        End If
        volumeCallback = Nothing
        volumeCallbackPtr = IntPtr.Zero
        registeredVolume = Nothing
    End Sub

    Private Shared Sub ForwardVolumeChanged(ByVal sender As Object, ByVal e As EventArgs)
        RaiseEvent VolumeChanged(sender, e)
    End Sub

    Friend Shared Sub LogVolumeIfChanged(ByVal percent As Integer, ByVal source As String)
        If percent = lastLoggedVolume Then Return
        lastLoggedVolume = percent
        modDiagnostics.LogInfo("QuickControlsAudio", source & " volume=" & percent.ToString())
    End Sub

    Public Shared Function GetMute() As Boolean
        Try
            Dim vol As IAudioEndpointVolume = TryGetVolumeInterface()
            If vol Is Nothing Then Return False
            Dim muted As Integer = 0
            If vol.GetMute(muted) = 0 Then Return muted <> 0
            InvalidateVolumeCache()
        Catch
            InvalidateVolumeCache()
        End Try
        Return False
    End Function

    ''' <summary>
    ''' Sets absolute mute via Core Audio. Falls back to APPCOMMAND only if COM fails
    ''' and the desired state still does not match after a toggle.
    ''' </summary>
    Public Shared Function SetMute(ByVal mute As Boolean) As Boolean
        Try
            Dim vol As IAudioEndpointVolume = TryGetVolumeInterface()
            If vol IsNot Nothing Then
                Dim ctx As Guid = Guid.Empty
                If vol.SetMute(If(mute, 1, 0), ctx) = 0 Then
                    Dim verify As Integer = 0
                    If vol.GetMute(verify) = 0 AndAlso ((verify <> 0) = mute) Then Return True
                End If
                InvalidateVolumeCache()
            End If
        Catch
            InvalidateVolumeCache()
        End Try

        ' Last resort: toggle once if system mute does not already match.
        If GetMute() = mute Then Return True
        SendAppCommand(APPCOMMAND_VOLUME_MUTE)
        Return GetMute() = mute
    End Function

    Private Shared Sub NudgeVolumeToPercent(ByVal target As Single)
        Dim currentPct As Integer = GetVolumePercent()
        If currentPct < 0 Then Return
        Dim current As Single = currentPct / 100.0F
        If Math.Abs(current - target) < 0.02F Then Return

        Dim maxSteps As Integer = 50
        Dim stepCount As Integer = 0
        While current < target - 0.01F AndAlso stepCount < maxSteps
            SendAppCommand(APPCOMMAND_VOLUME_UP)
            currentPct = GetVolumePercent()
            If currentPct < 0 Then Return
            current = currentPct / 100.0F
            stepCount += 1
        End While
        stepCount = 0
        While current > target + 0.01F AndAlso stepCount < maxSteps
            SendAppCommand(APPCOMMAND_VOLUME_DOWN)
            currentPct = GetVolumePercent()
            If currentPct < 0 Then Return
            current = currentPct / 100.0F
            stepCount += 1
        End While
    End Sub

    Private Shared Sub SendAppCommand(ByVal command As Integer)
        Dim lParam As IntPtr = New IntPtr(command << 16)
        SendMessage(New IntPtr(HWND_BROADCAST), WM_APPCOMMAND, IntPtr.Zero, lParam)
    End Sub
End Class

''' <summary>
''' Monitor brightness through WMI when supported.
''' </summary>
Friend Class QuickControlsBrightness
    Public Shared Function GetBrightnessPercent() As Integer
        Try
            Using s As ManagementObjectSearcher = New ManagementObjectSearcher("root\WMI", "SELECT CurrentBrightness FROM WmiMonitorBrightness")
                For Each o As ManagementObject In s.Get()
                    Return CInt(o("CurrentBrightness"))
                Next
            End Using
        Catch
        End Try
        Return 80
    End Function

    Public Shared Function SetBrightnessPercent(ByVal percent As Integer) As Boolean
        Try
            Dim level As UInteger = CUInt(Math.Max(0, Math.Min(100, percent)))
            Using s As ManagementObjectSearcher = New ManagementObjectSearcher("root\WMI", "SELECT * FROM WmiMonitorBrightnessMethods")
                For Each o As ManagementObject In s.Get()
                    o.InvokeMethod("WmiSetBrightness", New Object() {1UI, level})
                    Return True
                Next
            End Using
        Catch
        End Try
        Return False
    End Function
End Class

''' <summary>
''' Auto-rotation lock — matches the tablet hardware / Action Center toggle.
''' Writes HKCU + native (and HKLM when permitted); reads prefer HKCU / native over stale HKLM.
''' </summary>
Friend Class QuickControlsRotation
    Private Const KeyPath As String = "SOFTWARE\Microsoft\Windows\CurrentVersion\AutoRotation"
    Private Const ValueName As String = "Enable"
    Private Const AR_DISABLED As Integer = &H1

    ' Present on many Win8+/Win10 builds; used when registry alone is ignored live.
    <DllImport("user32.dll", EntryPoint:="SetAutoRotation", SetLastError:=True)> _
    Private Shared Function NativeSetAutoRotation(ByVal enable As Boolean) As Boolean
    End Function

    <DllImport("user32.dll", EntryPoint:="GetAutoRotationState", SetLastError:=True)> _
    Private Shared Function NativeGetAutoRotationState(ByRef pState As Integer) As Boolean
    End Function

    ''' <summary>
    ''' True when auto-rotation is disabled (rotation lock ON).
    ''' </summary>
    Public Shared Function IsRotationLocked() As Boolean
        Return ReadEnableValue() = 0
    End Function

    Public Shared Sub SetRotationLocked(ByVal locked As Boolean)
        Dim enable As Integer = If(locked, 0, 1)
        Try
            NativeSetAutoRotation(enable <> 0)
        Catch
        End Try
        ' Prefer writable HKCU first so UI can re-read what we actually set.
        WriteEnableValue(Registry.CurrentUser, enable)
        WriteEnableValue(Registry.LocalMachine, enable)
        ' Some builds also keep an ImmersiveControlPanel mirror of the Action Center toggle.
        Try
            Dim icp As RegistryKey = Registry.CurrentUser.CreateSubKey( _
                "SOFTWARE\Microsoft\Windows\CurrentVersion\ImmersiveControlPanel\Settings")
            If icp IsNot Nothing Then
                ' 1 = rotation lock on (auto-rotate off), matching Settings UI naming
                icp.SetValue("SystemSettings_Display_RotationLock", If(locked, 1, 0), RegistryValueKind.DWord)
                icp.Close()
            End If
        Catch
        End Try
    End Sub

    ''' <summary>
    ''' AutoRotation Enable: 1 = rotate allowed, 0 = locked. Prefer HKCU / native over HKLM.
    ''' </summary>
    Private Shared Function ReadEnableValue() As Integer
        Dim hkcu As Integer = ReadEnableFromHive(Registry.CurrentUser, -1)
        If hkcu >= 0 Then Return hkcu
        Dim icpLock As Integer = ReadImmersiveRotationLock()
        If icpLock >= 0 Then Return If(icpLock <> 0, 0, 1)
        Dim nativeEnable As Integer = ReadNativeEnable()
        If nativeEnable >= 0 Then Return nativeEnable
        Dim hklm As Integer = ReadEnableFromHive(Registry.LocalMachine, -1)
        If hklm >= 0 Then Return hklm
        Return 1
    End Function

    Private Shared Function ReadNativeEnable() As Integer
        Try
            Dim state As Integer = 0
            If Not NativeGetAutoRotationState(state) Then Return -1
            If (state And AR_DISABLED) <> 0 Then Return 0
            Return 1
        Catch
            Return -1
        End Try
    End Function

    Private Shared Function ReadImmersiveRotationLock() As Integer
        Try
            Dim key As RegistryKey = Registry.CurrentUser.OpenSubKey( _
                "SOFTWARE\Microsoft\Windows\CurrentVersion\ImmersiveControlPanel\Settings", False)
            If key Is Nothing Then Return -1
            Dim val As Object = key.GetValue("SystemSettings_Display_RotationLock")
            key.Close()
            If val Is Nothing Then Return -1
            Return CInt(val)
        Catch
            Return -1
        End Try
    End Function

    Private Shared Function ReadEnableFromHive(ByVal hive As RegistryKey, ByVal defaultValue As Integer) As Integer
        Try
            Dim key As RegistryKey = hive.OpenSubKey(KeyPath, False)
            If key Is Nothing Then Return defaultValue
            Dim val As Object = key.GetValue(ValueName)
            key.Close()
            If val Is Nothing Then Return defaultValue
            Return CInt(val)
        Catch
            Return defaultValue
        End Try
    End Function

    Private Shared Sub WriteEnableValue(ByVal hive As RegistryKey, ByVal enable As Integer)
        Try
            Dim key As RegistryKey = hive.CreateSubKey(KeyPath)
            If key Is Nothing Then Return
            key.SetValue(ValueName, enable, RegistryValueKind.DWord)
            key.Close()
        Catch
            ' HKLM often needs elevation; HKCU / native API may still apply.
        End Try
    End Sub
End Class

''' <summary>
''' Wi-Fi scan/connect through netsh on Win10.
''' </summary>
Friend Class QuickControlsWifi
    Public Class WifiNetwork
        Public SSID As String
        Public Signal As Integer
        Public Auth As String
        Public Overrides Function ToString() As String
            Return SSID & "  " & Signal.ToString() & "%  " & Auth
        End Function
    End Class

    Public Shared Function IsWifiRadioOn() As Boolean
        Dim text As String = modQuickControls.RunCommand("netsh", "interface show interface")
        For Each line As String In text.Split(New String() {vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
            If line.IndexOf("Wi-Fi", StringComparison.OrdinalIgnoreCase) >= 0 Then
                Return line.IndexOf("Disabled", StringComparison.OrdinalIgnoreCase) < 0
            End If
        Next
        Return False
    End Function

    Public Shared Sub SetWifiRadioOn(ByVal enabled As Boolean)
        Dim state As String = If(enabled, "enabled", "disabled")
        modQuickControls.RunCommand("netsh", "interface set interface name=""Wi-Fi"" admin=" & state)
    End Sub

    Public Shared Function ScanNetworks() As List(Of WifiNetwork)
        Dim list As New List(Of WifiNetwork)
        Dim text As String = modQuickControls.RunCommand("netsh", "wlan show networks mode=bssid")
        Dim current As WifiNetwork = Nothing
        For Each line As String In text.Split(New String() {vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
            Dim trimmed As String = line.Trim()
            If trimmed.StartsWith("SSID ", StringComparison.OrdinalIgnoreCase) AndAlso trimmed.Contains(":") Then
                If current IsNot Nothing Then list.Add(current)
                current = New WifiNetwork()
                current.SSID = trimmed.Substring(trimmed.IndexOf(":"c) + 1).Trim()
                current.Signal = 0
                current.Auth = ""
            ElseIf current IsNot Nothing Then
                If trimmed.StartsWith("Signal", StringComparison.OrdinalIgnoreCase) Then
                    Dim pct As Integer = 0
                    Integer.TryParse(trimmed.Replace("%", "").Split(":"c)(1).Trim(), pct)
                    If pct > current.Signal Then current.Signal = pct
                ElseIf trimmed.StartsWith("Authentication", StringComparison.OrdinalIgnoreCase) Then
                    current.Auth = trimmed.Substring(trimmed.IndexOf(":"c) + 1).Trim()
                End If
            End If
        Next
        If current IsNot Nothing Then list.Add(current)
        Return list
    End Function

    Public Shared Function Connect(ByVal ssid As String, ByVal password As String) As String
        Return Connect(ssid, password, "")
    End Function

    ''' <summary>
    ''' Connects to SSID. Open networks get an open-auth profile. Secured nets with an empty
    ''' password try an existing profile first; returns NEED_PASSWORD:… when a passphrase is required.
    ''' </summary>
    Public Shared Function Connect(ByVal ssid As String, ByVal password As String, ByVal auth As String) As String
        If String.IsNullOrEmpty(ssid) Then Return "no SSID"
        If IsOpenAuth(auth) Then
            Return AddProfileAndConnect(ssid, BuildOpenProfileXml(ssid))
        End If

        If Not String.IsNullOrEmpty(password) Then
            Return AddProfileAndConnect(ssid, BuildWpaProfileXml(ssid, password))
        End If

        Dim result As String = modQuickControls.RunCommand("netsh", "wlan connect name=" & QuoteNetsh(ssid))
        If ConnectLooksSuccessful(result) Then Return result
        If LooksLikeMissingProfile(result) Then
            Return "NEED_PASSWORD:" & FirstLine(result)
        End If
        ' Empty-password connect often fails quietly when no profile exists.
        If String.IsNullOrEmpty(result) OrElse LooksLikeConnectFailure(result) Then
            Return "NEED_PASSWORD:" & FirstLine(If(String.IsNullOrEmpty(result), "no profile / connect failed", result))
        End If
        Return result
    End Function

    Private Shared Function AddProfileAndConnect(ByVal ssid As String, ByVal profileXml As String) As String
        Dim profilePath As String = Path.Combine(Path.GetTempPath(), "lcars-wifi-" & Guid.NewGuid().ToString("N") & ".xml")
        Try
            File.WriteAllText(profilePath, profileXml, Encoding.UTF8)
            Dim addResult As String = modQuickControls.RunCommand("netsh", "wlan add profile filename=" & QuoteNetsh(profilePath))
            Dim connectResult As String = modQuickControls.RunCommand("netsh", "wlan connect name=" & QuoteNetsh(ssid))
            If String.IsNullOrEmpty(connectResult) Then Return addResult
            Return connectResult
        Finally
            Try
                File.Delete(profilePath)
            Catch
            End Try
        End Try
    End Function

    Private Shared Function IsOpenAuth(ByVal auth As String) As Boolean
        If String.IsNullOrEmpty(auth) Then Return False
        Dim a As String = auth.Trim()
        Return a.Equals("Open", StringComparison.OrdinalIgnoreCase) OrElse _
               a.IndexOf("Open", StringComparison.OrdinalIgnoreCase) >= 0
    End Function

    Private Shared Function LooksLikeMissingProfile(ByVal text As String) As Boolean
        If String.IsNullOrEmpty(text) Then Return False
        Dim t As String = text.ToLowerInvariant()
        Return t.Contains("there is no profile") OrElse _
               t.Contains("no profile") OrElse _
               (t.Contains("profile") AndAlso t.Contains("is not found")) OrElse _
               t.Contains("cannot find") OrElse _
               t.Contains("not found")
    End Function

    Private Shared Function LooksLikeConnectFailure(ByVal text As String) As Boolean
        If String.IsNullOrEmpty(text) Then Return True
        Dim t As String = text.ToLowerInvariant()
        Return t.Contains("failed") OrElse t.Contains("error") OrElse t.Contains("unable")
    End Function

    Private Shared Function ConnectLooksSuccessful(ByVal text As String) As Boolean
        If String.IsNullOrEmpty(text) Then Return False
        Dim t As String = text.ToLowerInvariant()
        Return t.Contains("connection request was completed successfully") OrElse _
               t.Contains("successfully") AndAlso Not LooksLikeConnectFailure(text)
    End Function

    Private Shared Function FirstLine(ByVal text As String) As String
        If text Is Nothing Then Return ""
        For Each line As String In text.Split(New String() {vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
            Dim t As String = line.Trim()
            If t <> "" Then Return t
        Next
        Return text.Trim()
    End Function

    Private Shared Function QuoteNetsh(ByVal value As String) As String
        Return """" & value.Replace("""", "") & """"
    End Function

    Private Shared Function BuildOpenProfileXml(ByVal ssid As String) As String
        Dim escapedSsid As String = System.Security.SecurityElement.Escape(ssid)
        Return "<?xml version=""1.0""?>" & _
               "<WLANProfile xmlns=""http://www.microsoft.com/networking/WLAN/profile/v1"">" & _
               "<name>" & escapedSsid & "</name>" & _
               "<SSIDConfig><SSID><name>" & escapedSsid & "</name></SSID></SSIDConfig>" & _
               "<connectionType>ESS</connectionType><connectionMode>auto</connectionMode>" & _
               "<MSM><security><authEncryption><authentication>open</authentication>" & _
               "<encryption>none</encryption><useOneX>false</useOneX></authEncryption>" & _
               "</security></MSM></WLANProfile>"
    End Function

    Private Shared Function BuildWpaProfileXml(ByVal ssid As String, ByVal password As String) As String
        Dim escapedSsid As String = System.Security.SecurityElement.Escape(ssid)
        Dim escapedPwd As String = System.Security.SecurityElement.Escape(password)
        Return "<?xml version=""1.0""?>" & _
               "<WLANProfile xmlns=""http://www.microsoft.com/networking/WLAN/profile/v1"">" & _
               "<name>" & escapedSsid & "</name>" & _
               "<SSIDConfig><SSID><name>" & escapedSsid & "</name></SSID></SSIDConfig>" & _
               "<connectionType>ESS</connectionType><connectionMode>auto</connectionMode>" & _
               "<MSM><security><authEncryption><authentication>WPA2PSK</authentication>" & _
               "<encryption>AES</encryption><useOneX>false</useOneX></authEncryption>" & _
               "<sharedKey><keyType>passPhrase</keyType><protected>false</protected>" & _
               "<keyMaterial>" & escapedPwd & "</keyMaterial></sharedKey></security></MSM></WLANProfile>"
    End Function
End Class

''' <summary>
''' Bluetooth radio and devices via PowerShell WinRT helpers on Win10.
''' </summary>
Friend Class QuickControlsBluetooth
    Public Class BtDevice
        Public Name As String
        Public Id As String
        Public DeviceClass As String   ' Audio | Hid | Other
        Public State As String         ' Connected | Paired | Disconnected | Unknown
        Public Overrides Function ToString() As String
            Return Name & " · " & DeviceClass & " · " & State
        End Function
    End Class

    Public Shared Function IsBluetoothOn() As Boolean
        Dim result As String = modQuickControls.RunPowerShell(GetRadioStateScript())
        Return result.Trim().Equals("True", StringComparison.OrdinalIgnoreCase)
    End Function

    ''' <summary>
    ''' Turns Bluetooth radio on/off after RequestAccessAsync. Returns "OK" or a failure reason.
    ''' Callers must not flip UI unless the result is OK.
    ''' </summary>
    Public Shared Function SetBluetoothOn(ByVal enabled As Boolean) As String
        Dim result As String = modQuickControls.RunPowerShell( _
            GetRadioScript() & "; Set-BtRadio " & If(enabled, "$true", "$false"))
        Dim trimmed As String = FirstStatusLine(result)
        If trimmed = "" Then Return "bluetooth radio failed"
        Return trimmed
    End Function

    Public Shared Function ListDevices() As List(Of BtDevice)
        Dim list As New List(Of BtDevice)
        Dim result As String = modQuickControls.RunPowerShell(GetListScript())
        For Each line As String In result.Split(New String() {vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
            Dim parts() As String = line.Split("|"c)
            If parts.Length >= 4 Then
                Dim d As New BtDevice()
                d.Name = parts(0).Trim()
                d.Id = parts(1).Trim()
                d.DeviceClass = parts(2).Trim()
                d.State = parts(3).Trim()
                list.Add(d)
            End If
        Next
        Return list
    End Function

    Public Shared Sub StartPairingWizard()
        Dim wizard As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "DevicePairingWizard.exe")
        If File.Exists(wizard) Then
            Process.Start(wizard)
        Else
            Process.Start("ms-settings:bluetooth")
        End If
    End Sub

    ''' <summary>
    ''' Connect: Audio tries helper first then WinRT; HID/Other use WinRT only. Never throws to UI.
    ''' </summary>
    Public Shared Function Connect(ByVal device As BtDevice) As String
        Try
            If device Is Nothing OrElse String.IsNullOrEmpty(device.Id) Then Return "no device"
            If IsAudioClass(device) Then
                Dim audioStatus As String = TryAudioConnect(device)
                If IsOkStatus(audioStatus) Then Return audioStatus
                Dim winrtStatus As String = ConnectWinRt(device)
                If IsOkStatus(winrtStatus) Then Return winrtStatus
                Return CombineStatuses(audioStatus, winrtStatus)
            End If
            Return ConnectWinRt(device)
        Catch ex As Exception
            Return "connect failed: " & ex.Message
        End Try
    End Function

    ''' <summary>
    ''' Disconnect: Audio tries helper first then WinRT; HID/Other use WinRT only. Never throws to UI.
    ''' </summary>
    Public Shared Function Disconnect(ByVal device As BtDevice) As String
        Try
            If device Is Nothing OrElse String.IsNullOrEmpty(device.Id) Then Return "no device"
            If IsAudioClass(device) Then
                Dim audioStatus As String = TryAudioDisconnect(device)
                If IsOkStatus(audioStatus) Then Return audioStatus
                Dim winrtStatus As String = DisconnectWinRt(device)
                If IsOkStatus(winrtStatus) Then Return winrtStatus
                Return CombineStatuses(audioStatus, winrtStatus)
            End If
            Return DisconnectWinRt(device)
        Catch ex As Exception
            Return "disconnect failed: " & ex.Message
        End Try
    End Function

    ''' <summary>
    ''' Unpair via DeviceInformation.Pairing.UnpairAsync. Returns OK or a short status; never throws to UI.
    ''' </summary>
    Public Shared Function Unpair(ByVal device As BtDevice) As String
        Try
            If device Is Nothing OrElse String.IsNullOrEmpty(device.Id) Then Return "no device"
            Dim result As String = modQuickControls.RunPowerShell(GetUnpairScript(device.Id))
            Dim trimmed As String = result.Trim()
            If trimmed = "" Then Return "unpair failed"
            Return FirstStatusLine(trimmed)
        Catch ex As Exception
            Return "unpair failed: " & ex.Message
        End Try
    End Function

    Private Shared Function IsAudioClass(ByVal device As BtDevice) As Boolean
        If device Is Nothing OrElse device.DeviceClass Is Nothing Then Return False
        Return device.DeviceClass.Equals("Audio", StringComparison.OrdinalIgnoreCase)
    End Function

    Private Shared Function IsOkStatus(ByVal status As String) As Boolean
        If status Is Nothing Then Return False
        Dim t As String = status.Trim()
        Return t.Equals("OK", StringComparison.OrdinalIgnoreCase) OrElse t.StartsWith("OK ", StringComparison.OrdinalIgnoreCase)
    End Function

    Private Shared Function CombineStatuses(ByVal primary As String, ByVal secondary As String) As String
        Dim a As String = If(primary, "").Trim()
        Dim b As String = If(secondary, "").Trim()
        If a = "" Then Return If(b = "", "failed", b)
        If b = "" OrElse b.Equals(a, StringComparison.OrdinalIgnoreCase) Then Return a
        Return a & "; " & b
    End Function

    Private Shared Function ConnectWinRt(ByVal device As BtDevice) As String
        Dim result As String = modQuickControls.RunPowerShell(GetConnectScript(device.Id))
        Dim trimmed As String = result.Trim()
        If trimmed = "" Then Return "connect failed"
        Return FirstStatusLine(trimmed)
    End Function

    Private Shared Function DisconnectWinRt(ByVal device As BtDevice) As String
        Dim result As String = modQuickControls.RunPowerShell(GetDisconnectScript(device.Id))
        Dim trimmed As String = result.Trim()
        If trimmed = "" Then Return "disconnect failed"
        Return FirstStatusLine(trimmed)
    End Function

    ''' <summary>
    ''' Audio-first connect: btcom if present, else safe PnP enable by friendly-name match.
    ''' </summary>
    Private Shared Function TryAudioConnect(ByVal device As BtDevice) As String
        Try
            Dim name As String = If(device.Name, "").Trim()
            If name = "" Then Return "audio helper: no device name"
            Dim result As String = modQuickControls.RunPowerShell(GetAudioConnectScript(name, device.Id))
            Dim trimmed As String = result.Trim()
            If trimmed = "" Then Return "audio helper: connect failed"
            Return FirstStatusLine(trimmed)
        Catch ex As Exception
            Return "audio helper: " & ex.Message
        End Try
    End Function

    ''' <summary>
    ''' Audio-first disconnect: btcom if present, else safe PnP disable of matching audio endpoints only.
    ''' </summary>
    Private Shared Function TryAudioDisconnect(ByVal device As BtDevice) As String
        Try
            Dim name As String = If(device.Name, "").Trim()
            If name = "" Then Return "audio helper: no device name"
            Dim result As String = modQuickControls.RunPowerShell(GetAudioDisconnectScript(name, device.Id))
            Dim trimmed As String = result.Trim()
            If trimmed = "" Then Return "audio helper: disconnect failed"
            Return FirstStatusLine(trimmed)
        Catch ex As Exception
            Return "audio helper: " & ex.Message
        End Try
    End Function

    Private Shared Function FirstStatusLine(ByVal text As String) As String
        For Each line As String In text.Split(New String() {vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
            Dim t As String = line.Trim()
            If t <> "" Then Return t
        Next
        Return text.Trim()
    End Function

    Private Shared Function EscapePsSingleQuoted(ByVal value As String) As String
        If value Is Nothing Then Return ""
        Return value.Replace("'", "''")
    End Function

    Private Shared Function GetAwaitBootstrap() As String
        Return "Add-Type -AssemblyName System.Runtime.WindowsRuntime; " & _
               "$asTaskGeneric = ([System.WindowsRuntimeSystemExtensions].GetMethods() | ? { $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1' })[0]; " & _
               "function Await($WinRtTask, $ResultType) { $asTask = $asTaskGeneric.MakeGenericMethod($ResultType); $netTask = $asTask.Invoke($null, @($WinRtTask)); $netTask.Wait(-1) | Out-Null; $netTask.Result }; "
    End Function

    Private Shared Function GetRadioStateScript() As String
        Return GetAwaitBootstrap() & _
               "[Windows.Devices.Radios.Radio,Windows.Devices.Radios,ContentType=WindowsRuntime] | Out-Null; " & _
               "try { " & _
               "$access = Await ([Windows.Devices.Radios.Radio]::RequestAccessAsync()) ([Windows.Devices.Radios.RadioAccessStatus]); " & _
               "if ($access -ne 'Allowed') { Write-Output 'False'; return }; " & _
               "$radios = Await ([Windows.Devices.Radios.Radio]::GetRadiosAsync()) ([System.Collections.Generic.IReadOnlyList[Windows.Devices.Radios.Radio]]); " & _
               "$bt = ($radios | ? { $_.Kind -eq 'Bluetooth' } | select -first 1); " & _
               "if ($bt) { Write-Output ($bt.State -eq 'On') } else { Write-Output 'False' } " & _
               "} catch { Write-Output 'False' }"
    End Function

    Private Shared Function GetRadioScript() As String
        Return GetAwaitBootstrap() & _
               "[Windows.Devices.Radios.Radio,Windows.Devices.Radios,ContentType=WindowsRuntime] | Out-Null; " & _
               "function Set-BtRadio($on) { " & _
               "try { " & _
               "$access = Await ([Windows.Devices.Radios.Radio]::RequestAccessAsync()) ([Windows.Devices.Radios.RadioAccessStatus]); " & _
               "if ($access -ne 'Allowed') { Write-Output ('access denied: ' + [string]$access); return }; " & _
               "$radios = Await ([Windows.Devices.Radios.Radio]::GetRadiosAsync()) ([System.Collections.Generic.IReadOnlyList[Windows.Devices.Radios.Radio]]); " & _
               "$bt = ($radios | ? { $_.Kind -eq 'Bluetooth' } | select -first 1); " & _
               "if ($bt -eq $null) { Write-Output 'no bluetooth radio'; return }; " & _
               "$target = if ($on) { [Windows.Devices.Radios.RadioState]::On } else { [Windows.Devices.Radios.RadioState]::Off }; " & _
               "$status = Await ($bt.SetStateAsync($target)) ([Windows.Devices.Radios.RadioAccessStatus]); " & _
               "if ($status -eq 'Allowed') { Write-Output 'OK' } else { Write-Output ('radio denied: ' + [string]$status) } " & _
               "} catch { Write-Output ('bluetooth failed: ' + $_.Exception.Message) } }"
    End Function

    Private Shared Function GetListScript() As String
        Return GetAwaitBootstrap() & _
               "function Get-BtClass($name, $classHint) { $probe = (($name + ' ' + $classHint)).ToLowerInvariant(); " & _
               "if ($probe -match 'headset|audio|headphones|a2dp|hfp|audiovideo') { return 'Audio' }; " & _
               "if ($probe -match 'mouse|keyboard|hid|peripheral') { return 'Hid' }; return 'Other' }; " & _
               "[Windows.Devices.Enumeration.DeviceInformation,Windows.Devices.Enumeration,ContentType=WindowsRuntime] | Out-Null; " & _
               "[Windows.Devices.Bluetooth.BluetoothDevice,Windows.Devices.Bluetooth,ContentType=WindowsRuntime] | Out-Null; " & _
               "$devs = Await ([Windows.Devices.Enumeration.DeviceInformation]::FindAllAsync([Windows.Devices.Bluetooth.BluetoothDevice]::GetDeviceSelectorFromPairingState($true))) ([System.Collections.Generic.IReadOnlyList[Windows.Devices.Enumeration.DeviceInformation]]); " & _
               "foreach ($d in $devs) { $name = $d.Name; $id = $d.Id; $class = 'Other'; $state = 'Paired'; " & _
               "try { $bt = Await ([Windows.Devices.Bluetooth.BluetoothDevice]::FromIdAsync($id)) ([Windows.Devices.Bluetooth.BluetoothDevice]); " & _
               "if ($bt -ne $null) { " & _
               "if ($bt.ConnectionStatus -eq 'Connected') { $state = 'Connected' } " & _
               "elseif ($bt.ConnectionStatus -eq 'Disconnected') { $state = 'Disconnected' } " & _
               "else { $state = 'Unknown' }; " & _
               "$hint = ''; if ($bt.ClassOfDevice -ne $null) { $hint = [string]$bt.ClassOfDevice.MajorClass }; " & _
               "$class = Get-BtClass $name $hint } else { $class = Get-BtClass $name ''; $state = 'Paired' } } " & _
               "catch { $class = Get-BtClass $name ''; $state = 'Paired' }; " & _
               "Write-Output ($name + '|' + $id + '|' + $class + '|' + $state) }"
    End Function

    ''' <summary>
    ''' Resolve btcom.exe if installed; extract remote MAC from Bluetooth device Id when possible.
    ''' </summary>
    Private Shared Function GetAudioHelperBootstrap() As String
        Return "function Get-BtMac($id) { if (-not $id) { return $null }; " & _
               "$m = [regex]::Match($id, '([0-9A-Fa-f]{2}([:-])[0-9A-Fa-f]{2}(\2[0-9A-Fa-f]{2}){4})-([0-9A-Fa-f]{2}(\2[0-9A-Fa-f]{2}){5})'); " & _
               "if ($m.Success) { return ($m.Groups[4].Value -replace '-', ':').ToUpperInvariant() }; " & _
               "$m2 = [regex]::Match($id, '([0-9A-Fa-f]{2}([:-])[0-9A-Fa-f]{2}(\2[0-9A-Fa-f]{2}){4})'); " & _
               "if ($m2.Success) { return ($m2.Groups[1].Value -replace '-', ':').ToUpperInvariant() }; return $null }; " & _
               "function Find-BtCom { $cmd = Get-Command btcom.exe -ErrorAction SilentlyContinue; if ($cmd) { return $cmd.Source }; " & _
               "$pf = [Environment]::GetFolderPath('ProgramFiles'); $pf86 = [Environment]::GetFolderPath('ProgramFilesX86'); " & _
               "$paths = @(); if ($pf) { $paths += (Join-Path $pf 'Bluetooth Command Line Tools\bin\btcom.exe') }; " & _
               "if ($pf86) { $paths += (Join-Path $pf86 'Bluetooth Command Line Tools\bin\btcom.exe') }; " & _
               "foreach ($p in $paths) { if ($p -and (Test-Path -LiteralPath $p)) { return $p } }; return $null }; " & _
               "function Test-FriendlyMatch($friendly, $want) { if (-not $friendly -or -not $want) { return $false }; " & _
               "if ($friendly -eq $want) { return $true }; " & _
               "if ($friendly.StartsWith($want + ' ', [StringComparison]::OrdinalIgnoreCase)) { return $true }; " & _
               "if ($friendly.StartsWith($want + '(', [StringComparison]::OrdinalIgnoreCase)) { return $true }; return $false }; " & _
               "function Test-AudioishPnp($d) { " & _
               "$cls = [string]$d.Class; $fn = [string]$d.FriendlyName; " & _
               "if ($cls -eq 'AudioEndpoint' -or $cls -eq 'MEDIA' -or $cls -eq 'Headphones') { return $true }; " & _
               "if ($cls -eq 'Bluetooth' -and ($fn -match 'Headset|Headphones|Hands-Free|Stereo|Audio|A2DP|HFP')) { return $true }; " & _
               "if ($fn -match 'Hands-Free|Headset|Headphones|Stereo') { return $true }; return $false }; "
    End Function

    ''' <summary>
    ''' Audio connect: btcom A2DP/HFP if available, else enable matching audio/BT PnP nodes only.
    ''' </summary>
    Private Shared Function GetAudioConnectScript(ByVal deviceName As String, ByVal deviceId As String) As String
        Dim nameLit As String = EscapePsSingleQuoted(deviceName)
        Dim idLit As String = EscapePsSingleQuoted(deviceId)
        Return GetAudioHelperBootstrap() & _
               "$name = '" & nameLit & "'; $id = '" & idLit & "'; " & _
               "try { " & _
               "$btcom = Find-BtCom; $mac = Get-BtMac $id; $btcomTried = $false; " & _
               "if ($btcom -and $mac) { " & _
               "$btcomTried = $true; $ok = $false; foreach ($svc in @('110b','111e')) { " & _
               "$p = Start-Process -FilePath $btcom -ArgumentList @('-c','-b',$mac,'-s' + $svc) -Wait -PassThru -WindowStyle Hidden -ErrorAction SilentlyContinue; " & _
               "if ($p -ne $null -and $p.ExitCode -eq 0) { $ok = $true } }; " & _
               "if ($ok) { Write-Output 'OK'; return } }; " & _
               "$all = @(Get-PnpDevice -ErrorAction SilentlyContinue); " & _
               "$matches = @($all | Where-Object { (Test-FriendlyMatch $_.FriendlyName $name) -and (Test-AudioishPnp $_) }); " & _
               "if ($matches.Count -eq 0) { " & _
               "if ($btcomTried) { Write-Output 'audio helper: btcom failed; no matching audio PnP device'; return }; " & _
               "Write-Output 'audio helper: no matching audio PnP device'; return }; " & _
               "$changed = 0; $already = 0; $errors = @(); " & _
               "foreach ($d in $matches) { " & _
               "if ($d.Status -eq 'OK') { $already++; continue }; " & _
               "try { Enable-PnpDevice -InstanceId $d.InstanceId -Confirm:$false -ErrorAction Stop; $changed++ } " & _
               "catch { $errors += $_.Exception.Message } }; " & _
               "if ($changed -gt 0 -or $already -gt 0) { Write-Output 'OK'; return }; " & _
               "if ($errors.Count -gt 0) { Write-Output ('audio helper: ' + $errors[0]); return }; " & _
               "Write-Output 'audio helper: connect failed' " & _
               "} catch { Write-Output ('audio helper: ' + $_.Exception.Message) }"
    End Function

    ''' <summary>
    ''' Audio disconnect: btcom release if available, else disable matching AudioEndpoint/Hands-Free nodes only (never adapter-wide).
    ''' </summary>
    Private Shared Function GetAudioDisconnectScript(ByVal deviceName As String, ByVal deviceId As String) As String
        Dim nameLit As String = EscapePsSingleQuoted(deviceName)
        Dim idLit As String = EscapePsSingleQuoted(deviceId)
        Return GetAudioHelperBootstrap() & _
               "$name = '" & nameLit & "'; $id = '" & idLit & "'; " & _
               "try { " & _
               "$btcom = Find-BtCom; $mac = Get-BtMac $id; $btcomTried = $false; " & _
               "if ($btcom -and $mac) { " & _
               "$btcomTried = $true; $ok = $false; foreach ($svc in @('110b','111e')) { " & _
               "$p = Start-Process -FilePath $btcom -ArgumentList @('-r','-b',$mac,'-s' + $svc) -Wait -PassThru -WindowStyle Hidden -ErrorAction SilentlyContinue; " & _
               "if ($p -ne $null -and $p.ExitCode -eq 0) { $ok = $true } }; " & _
               "if ($ok) { Write-Output 'OK'; return } }; " & _
               "$all = @(Get-PnpDevice -ErrorAction SilentlyContinue); " & _
               "$matches = @($all | Where-Object { " & _
               "(Test-FriendlyMatch $_.FriendlyName $name) -and " & _
               "($_.Class -eq 'AudioEndpoint' -or $_.Class -eq 'MEDIA' -or $_.Class -eq 'Headphones' -or " & _
               "(($_.Class -eq 'Bluetooth') -and ($_.FriendlyName -match 'Hands-Free|Headset|Headphones|Stereo|Audio'))) }); " & _
               "if ($matches.Count -eq 0) { " & _
               "if ($btcomTried) { Write-Output 'audio helper: btcom failed; no matching audio PnP device'; return }; " & _
               "Write-Output 'audio helper: no matching audio PnP device'; return }; " & _
               "$changed = 0; $already = 0; $errors = @(); " & _
               "foreach ($d in $matches) { " & _
               "if ($d.Status -eq 'Error' -or $d.Problem -gt 0) { $already++; continue }; " & _
               "try { Disable-PnpDevice -InstanceId $d.InstanceId -Confirm:$false -ErrorAction Stop; $changed++ } " & _
               "catch { $errors += $_.Exception.Message } }; " & _
               "if ($changed -gt 0 -or $already -gt 0) { Write-Output 'OK'; return }; " & _
               "if ($errors.Count -gt 0) { Write-Output ('audio helper: ' + $errors[0]); return }; " & _
               "Write-Output 'audio helper: disconnect failed' " & _
               "} catch { Write-Output ('audio helper: ' + $_.Exception.Message) }"
    End Function

    ''' <summary>
    ''' WinRT connect: FromIdAsync + uncached RFCOMM discovery (forces link when supported).
    ''' </summary>
    Private Shared Function GetConnectScript(ByVal deviceId As String) As String
        Dim idLit As String = EscapePsSingleQuoted(deviceId)
        Return GetAwaitBootstrap() & _
               "[Windows.Devices.Bluetooth.BluetoothDevice,Windows.Devices.Bluetooth,ContentType=WindowsRuntime] | Out-Null; " & _
               "[Windows.Devices.Bluetooth.Rfcomm.RfcommDeviceServicesResult,Windows.Devices.Bluetooth,ContentType=WindowsRuntime] | Out-Null; " & _
               "$id = '" & idLit & "'; " & _
               "try { " & _
               "$bt = Await ([Windows.Devices.Bluetooth.BluetoothDevice]::FromIdAsync($id)) ([Windows.Devices.Bluetooth.BluetoothDevice]); " & _
               "if ($bt -eq $null) { Write-Output 'device not found'; return }; " & _
               "if ($bt.ConnectionStatus -eq 'Connected') { Write-Output 'OK'; return }; " & _
               "try { Await ($bt.RequestAccessAsync()) ([Windows.Devices.Enumeration.DeviceAccessStatus]) | Out-Null } catch { }; " & _
               "try { " & _
               "$svc = Await ($bt.GetRfcommServicesAsync([Windows.Devices.Bluetooth.BluetoothCacheMode]::Uncached)) ([Windows.Devices.Bluetooth.Rfcomm.RfcommDeviceServicesResult]); " & _
               "if ($bt.ConnectionStatus -eq 'Connected') { Write-Output 'OK'; return }; " & _
               "if ($svc -ne $null -and $svc.Services -ne $null -and $svc.Services.Count -gt 0) { Write-Output 'OK'; return } " & _
               "} catch { }; " & _
               "Write-Output 'WinRT connect unsupported for this device' " & _
               "} catch { Write-Output ('connect failed: ' + $_.Exception.Message) }"
    End Function

    ''' <summary>
    ''' WinRT disconnect: close BluetoothDevice when possible; else clear unsupported status.
    ''' </summary>
    Private Shared Function GetDisconnectScript(ByVal deviceId As String) As String
        Dim idLit As String = EscapePsSingleQuoted(deviceId)
        Return GetAwaitBootstrap() & _
               "[Windows.Devices.Bluetooth.BluetoothDevice,Windows.Devices.Bluetooth,ContentType=WindowsRuntime] | Out-Null; " & _
               "[Windows.Devices.Bluetooth.BluetoothLEDevice,Windows.Devices.Bluetooth,ContentType=WindowsRuntime] | Out-Null; " & _
               "$id = '" & idLit & "'; " & _
               "try { " & _
               "$bt = Await ([Windows.Devices.Bluetooth.BluetoothDevice]::FromIdAsync($id)) ([Windows.Devices.Bluetooth.BluetoothDevice]); " & _
               "if ($bt -eq $null) { " & _
               "try { $le = Await ([Windows.Devices.Bluetooth.BluetoothLEDevice]::FromIdAsync($id)) ([Windows.Devices.Bluetooth.BluetoothLEDevice]); " & _
               "if ($le -ne $null) { $le.Dispose(); Write-Output 'OK'; return } } catch { }; " & _
               "Write-Output 'device not found'; return }; " & _
               "if ($bt.ConnectionStatus -eq 'Disconnected') { Write-Output 'OK'; return }; " & _
               "try { $bt.Dispose() } catch { try { $bt.Close() } catch { } }; " & _
               "Start-Sleep -Milliseconds 400; " & _
               "$bt2 = $null; try { $bt2 = Await ([Windows.Devices.Bluetooth.BluetoothDevice]::FromIdAsync($id)) ([Windows.Devices.Bluetooth.BluetoothDevice]) } catch { }; " & _
               "if ($bt2 -eq $null -or $bt2.ConnectionStatus -eq 'Disconnected') { Write-Output 'OK'; return }; " & _
               "Write-Output 'WinRT disconnect unsupported for this device' " & _
               "} catch { Write-Output ('disconnect failed: ' + $_.Exception.Message) }"
    End Function

    ''' <summary>
    ''' Unpair via DeviceInformation.Pairing.UnpairAsync.
    ''' </summary>
    Private Shared Function GetUnpairScript(ByVal deviceId As String) As String
        Dim idLit As String = EscapePsSingleQuoted(deviceId)
        Return GetAwaitBootstrap() & _
               "[Windows.Devices.Enumeration.DeviceInformation,Windows.Devices.Enumeration,ContentType=WindowsRuntime] | Out-Null; " & _
               "$id = '" & idLit & "'; " & _
               "try { " & _
               "$di = Await ([Windows.Devices.Enumeration.DeviceInformation]::CreateFromIdAsync($id)) ([Windows.Devices.Enumeration.DeviceInformation]); " & _
               "if ($di -eq $null) { Write-Output 'device not found'; return }; " & _
               "if ($di.Pairing -eq $null) { Write-Output 'unpair unsupported'; return }; " & _
               "$res = Await ($di.Pairing.UnpairAsync()) ([Windows.Devices.Enumeration.DeviceUnpairingResult]); " & _
               "if ($res -eq $null) { Write-Output 'unpair failed'; return }; " & _
               "if ($res.Status -eq 'Unpaired' -or $res.Status -eq 'AlreadyUnpaired') { Write-Output 'OK' } " & _
               "else { Write-Output ('unpair failed: ' + [string]$res.Status) } " & _
               "} catch { Write-Output ('unpair failed: ' + $_.Exception.Message) }"
    End Function
End Class

''' <summary>
''' Battery status and power plan cycling.
''' </summary>
Friend Class QuickControlsPower
    Private Shared planGuids As New List(Of String)
    Private Shared planNames As New List(Of String)
    Private Shared lastLoaded As DateTime = DateTime.MinValue

    ' Built-in Windows scheme GUIDs (may be hidden until duplicated on tablets).
    Private Shared ReadOnly StandardSchemeGuids As String() = {
        "381b4222-f694-41f0-9685-ff5bb260df2e",
        "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c",
        "a1841308-3541-4fab-bc81-f71556f20b4a"
    }

    Public Shared Function GetBatterySummary() As String
        Dim ps As PowerStatus = SystemInformation.PowerStatus
        Dim pct As Integer = CInt(Math.Round(ps.BatteryLifePercent * 100))
        Dim src As String = If(ps.PowerLineStatus = PowerLineStatus.Online, "AC", "Battery")
        Return pct.ToString() & "% " & src & " — " & GetActivePlanName()
    End Function

    Public Shared Function GetActivePlanHudText() As String
        Dim name As String = GetActivePlanName()
        If name.IndexOf("saver", StringComparison.OrdinalIgnoreCase) >= 0 Then Return "SAVER"
        If name.IndexOf("balanced", StringComparison.OrdinalIgnoreCase) >= 0 Then Return "BALANCED"
        If name.IndexOf("performance", StringComparison.OrdinalIgnoreCase) >= 0 Then Return "HIGH PERF"
        If name.Length > 12 Then Return name.Substring(0, 12).ToUpperInvariant()
        Return name.ToUpperInvariant()
    End Function

    ''' <summary>
    ''' Cycles to the next power plan by GUID. Returns the new plan name, or a failure message.
    ''' </summary>
    Public Shared Function CyclePowerPlan() As String
        EnsureStandardPlansVisible()
        LoadPlansIfNeeded(True)
        If planGuids.Count = 0 Then Return "no power plans found"
        If planGuids.Count = 1 Then
            Return "only one plan: " & If(planNames.Count > 0, planNames(0), planGuids(0))
        End If

        Dim activeGuid As String = GetActivePlanGuid()
        Dim activeIndex As Integer = 0
        Dim matched As Boolean = False
        If Not String.IsNullOrEmpty(activeGuid) Then
            For i As Integer = 0 To planGuids.Count - 1
                If String.Equals(planGuids(i), activeGuid, StringComparison.OrdinalIgnoreCase) Then
                    activeIndex = i
                    matched = True
                    Exit For
                End If
            Next
        End If
        If Not matched Then
            ' Fall back to index 0 so cycling still advances from an unknown active plan.
            activeIndex = 0
        End If
        Dim nextIndex As Integer = (activeIndex + 1) Mod planGuids.Count
        Dim exitCode As Integer = -1
        Dim output As String = modQuickControls.RunCommandEx("powercfg", "/setactive " & planGuids(nextIndex), exitCode)
        lastLoaded = DateTime.MinValue
        If exitCode <> 0 Then
            Dim detail As String = If(String.IsNullOrEmpty(output), "exit " & exitCode.ToString(), output.Trim())
            Return "powercfg failed: " & detail
        End If
        Dim newName As String = GetActivePlanName()
        If String.IsNullOrEmpty(newName) OrElse newName = "Unknown" Then
            If nextIndex < planNames.Count AndAlso Not String.IsNullOrEmpty(planNames(nextIndex)) Then
                newName = planNames(nextIndex)
            Else
                newName = "switched"
            End If
        End If
        Return "OK:" & newName
    End Function

    ''' <summary>
    ''' Unhides Balanced / High performance / Power saver when the device only exposes one scheme.
    ''' </summary>
    Private Shared Sub EnsureStandardPlansVisible()
        LoadPlansIfNeeded(True)
        If planGuids.Count >= 2 Then Return
        For Each schemeGuid As String In StandardSchemeGuids
            Dim already As Boolean = False
            For Each g As String In planGuids
                If String.Equals(g, schemeGuid, StringComparison.OrdinalIgnoreCase) Then
                    already = True
                    Exit For
                End If
            Next
            If already Then Continue For
            Dim exitCode As Integer = -1
            modQuickControls.RunCommandEx("powercfg", "-duplicatescheme " & schemeGuid, exitCode)
        Next
        lastLoaded = DateTime.MinValue
    End Sub

    Public Shared Function GetActivePlanName() As String
        Dim text As String = modQuickControls.RunCommand("powercfg", "/getactivescheme")
        For Each line As String In text.Split(New String() {vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
            If line.IndexOf("Power Scheme GUID", StringComparison.OrdinalIgnoreCase) >= 0 Then
                Dim p As Integer = line.IndexOf("("c)
                Dim q As Integer = line.LastIndexOf(")"c)
                If p >= 0 AndAlso q > p Then Return line.Substring(p + 1, q - p - 1).Trim()
            End If
        Next
        Return "Unknown"
    End Function

    Public Shared Function GetActivePlanGuid() As String
        Dim text As String = modQuickControls.RunCommand("powercfg", "/getactivescheme")
        For Each line As String In text.Split(New String() {vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
            If line.IndexOf("Power Scheme GUID", StringComparison.OrdinalIgnoreCase) >= 0 Then
                Dim guidStart As Integer = line.IndexOf(":"c) + 1
                Dim guidEnd As Integer = line.IndexOf("("c)
                If guidEnd < 0 Then guidEnd = line.Length
                If guidStart > 0 AndAlso guidEnd > guidStart Then
                    Return line.Substring(guidStart, guidEnd - guidStart).Trim()
                End If
            End If
        Next
        Return ""
    End Function

    Private Shared Sub LoadPlansIfNeeded(Optional ByVal force As Boolean = False)
        If Not force AndAlso (DateTime.Now - lastLoaded).TotalSeconds < 5 AndAlso planGuids.Count > 0 Then Return
        planGuids.Clear()
        planNames.Clear()
        Dim text As String = modQuickControls.RunCommand("powercfg", "/list")
        For Each line As String In text.Split(New String() {vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
            If line.IndexOf("Power Scheme GUID", StringComparison.OrdinalIgnoreCase) >= 0 Then
                Dim guidStart As Integer = line.IndexOf(":"c) + 1
                Dim guidEnd As Integer = line.IndexOf("("c)
                If guidEnd < 0 Then guidEnd = line.Length
                Dim guid As String = line.Substring(guidStart, guidEnd - guidStart).Trim()
                Dim name As String = ""
                Dim p As Integer = line.IndexOf("("c)
                Dim q As Integer = line.LastIndexOf(")"c)
                If p >= 0 AndAlso q > p Then name = line.Substring(p + 1, q - p - 1).Trim()
                If String.IsNullOrEmpty(guid) Then Continue For
                planGuids.Add(guid)
                planNames.Add(name)
            End If
        Next
        lastLoaded = DateTime.Now
    End Sub
End Class

''' <summary>
''' Primary IPv4, adapter/SSID, MAC, gateway/CIDR, VPN, and live RX/TX bytes/sec.
''' </summary>
Friend Class QuickControlsNetworkInfo
    Public Class Snapshot
        Public Ip As String = ""
        Public AdapterOrSsid As String = ""
        Public Mac As String = ""
        Public Gateway As String = ""
        Public CidrPrefix As String = ""
        Public SubnetMask As String = ""
        Public Vpn As String = "OFF"
        Public RxBytesPerSec As Long = 0
        Public TxBytesPerSec As Long = 0
    End Class

    Private Shared lastInterfaceId As String = ""
    Private Shared lastBytesReceived As Long = -1
    Private Shared lastBytesSent As Long = -1
    Private Shared lastSampleUtc As DateTime = DateTime.MinValue

    Public Shared Function GetSnapshot() As Snapshot
        Dim snap As New Snapshot()
        Try
            Dim primary As System.Net.NetworkInformation.NetworkInterface = FindPrimaryInterface()
            If primary IsNot Nothing Then
                FillFromInterface(primary, snap)
                UpdateThroughput(primary, snap)
            End If
            Dim ssid As String = GetConnectedWifiSsid()
            If Not String.IsNullOrEmpty(ssid) Then
                snap.AdapterOrSsid = ssid
            ElseIf primary IsNot Nothing Then
                Dim name As String = primary.Name
                If String.IsNullOrEmpty(name) Then name = primary.Description
                snap.AdapterOrSsid = name
            End If
            snap.Vpn = GetVpnStatus()
        Catch
        End Try
        Return snap
    End Function

    Public Shared Function GetPrimaryIPv4() As String
        Return GetSnapshot().Ip
    End Function

    Public Shared Function GetCurrentNetworkName() As String
        Return GetSnapshot().AdapterOrSsid
    End Function

    Private Shared Function PrefixLengthToMask(ByVal prefix As Integer) As String
        If prefix <= 0 OrElse prefix > 32 Then Return ""
        ' Build dotted-quad from prefix length (avoid VB UInteger shift quirks).
        Dim bits As Long = If(prefix = 32, &HFFFFFFFFL, (&HFFFFFFFFL << (32 - prefix)) And &HFFFFFFFFL)
        Return String.Format("{0}.{1}.{2}.{3}",
                             CInt((bits >> 24) And &HFFL),
                             CInt((bits >> 16) And &HFFL),
                             CInt((bits >> 8) And &HFFL),
                             CInt(bits And &HFFL))
    End Function

    ''' <summary>
    ''' Converts a "/22"-style prefix string to a dotted subnet mask.
    ''' </summary>
    Friend Shared Function MaskFromCidrPrefix(ByVal cidrPrefix As String) As String
        If String.IsNullOrEmpty(cidrPrefix) Then Return ""
        Dim digits As String = cidrPrefix.Trim().TrimStart("/"c)
        Dim prefix As Integer = 0
        If Not Integer.TryParse(digits, prefix) Then Return ""
        Return PrefixLengthToMask(prefix)
    End Function

    Private Shared Sub FillFromInterface(ByVal ni As System.Net.NetworkInformation.NetworkInterface, ByVal snap As Snapshot)
        Try
            Dim macBytes() As Byte = ni.GetPhysicalAddress().GetAddressBytes()
            If macBytes IsNot Nothing AndAlso macBytes.Length > 0 Then
                Dim parts As New List(Of String)
                For Each b As Byte In macBytes
                    parts.Add(b.ToString("X2"))
                Next
                snap.Mac = String.Join(":", parts.ToArray())
            End If
        Catch
        End Try

        Dim props As System.Net.NetworkInformation.IPInterfaceProperties = ni.GetIPProperties()
        If props Is Nothing Then Return

        If props.GatewayAddresses IsNot Nothing Then
            For Each gw As System.Net.NetworkInformation.GatewayIPAddressInformation In props.GatewayAddresses
                If gw.Address Is Nothing Then Continue For
                If gw.Address.AddressFamily <> System.Net.Sockets.AddressFamily.InterNetwork Then Continue For
                Dim g As String = gw.Address.ToString()
                If g = "0.0.0.0" Then Continue For
                snap.Gateway = g
                Exit For
            Next
        End If

        If props.UnicastAddresses Is Nothing Then Return
        For Each addr As System.Net.NetworkInformation.UnicastIPAddressInformation In props.UnicastAddresses
            If addr.Address Is Nothing Then Continue For
            If addr.Address.AddressFamily <> System.Net.Sockets.AddressFamily.InterNetwork Then Continue For
            Dim ip As String = addr.Address.ToString()
            If ip.StartsWith("169.254.", StringComparison.Ordinal) Then Continue For
            snap.Ip = ip
            Dim prefix As Integer = 0
            Try
                prefix = addr.PrefixLength
            Catch
                prefix = 0
            End Try
            If prefix > 0 Then
                snap.CidrPrefix = "/" & prefix.ToString()
                snap.SubnetMask = PrefixLengthToMask(prefix)
            End If
            If String.IsNullOrEmpty(snap.SubnetMask) AndAlso addr.IPv4Mask IsNot Nothing Then
                snap.SubnetMask = addr.IPv4Mask.ToString()
            End If
            Exit For
        Next
    End Sub

    Private Shared Sub UpdateThroughput(ByVal ni As System.Net.NetworkInformation.NetworkInterface, ByVal snap As Snapshot)
        Try
            Dim stats As System.Net.NetworkInformation.IPv4InterfaceStatistics = ni.GetIPv4Statistics()
            Dim nowUtc As DateTime = DateTime.UtcNow
            Dim rx As Long = stats.BytesReceived
            Dim tx As Long = stats.BytesSent
            Dim id As String = ni.Id
            If id = lastInterfaceId AndAlso lastBytesReceived >= 0 AndAlso lastSampleUtc <> DateTime.MinValue Then
                Dim seconds As Double = (nowUtc - lastSampleUtc).TotalSeconds
                If seconds > 0.05R Then
                    snap.RxBytesPerSec = CLng(Math.Max(0, (rx - lastBytesReceived) / seconds))
                    snap.TxBytesPerSec = CLng(Math.Max(0, (tx - lastBytesSent) / seconds))
                End If
            End If
            lastInterfaceId = id
            lastBytesReceived = rx
            lastBytesSent = tx
            lastSampleUtc = nowUtc
        Catch
        End Try
    End Sub

    Private Shared Function FindPrimaryInterface() As System.Net.NetworkInformation.NetworkInterface
        Dim fallback As System.Net.NetworkInformation.NetworkInterface = Nothing
        For Each ni As System.Net.NetworkInformation.NetworkInterface In System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
            If ni.OperationalStatus <> System.Net.NetworkInformation.OperationalStatus.Up Then Continue For
            If ni.NetworkInterfaceType = System.Net.NetworkInformation.NetworkInterfaceType.Loopback Then Continue For
            If ni.NetworkInterfaceType = System.Net.NetworkInformation.NetworkInterfaceType.Tunnel Then Continue For
            Dim props As System.Net.NetworkInformation.IPInterfaceProperties = ni.GetIPProperties()
            If props Is Nothing OrElse props.UnicastAddresses Is Nothing Then Continue For
            Dim hasIpv4 As Boolean = False
            For Each addr As System.Net.NetworkInformation.UnicastIPAddressInformation In props.UnicastAddresses
                If addr.Address Is Nothing Then Continue For
                If addr.Address.AddressFamily <> System.Net.Sockets.AddressFamily.InterNetwork Then Continue For
                Dim ip As String = addr.Address.ToString()
                If ip.StartsWith("169.254.", StringComparison.Ordinal) Then Continue For
                hasIpv4 = True
                Exit For
            Next
            If Not hasIpv4 Then Continue For
            If ni.NetworkInterfaceType = System.Net.NetworkInformation.NetworkInterfaceType.Wireless80211 OrElse _
               ni.NetworkInterfaceType = System.Net.NetworkInformation.NetworkInterfaceType.Ethernet OrElse _
               ni.NetworkInterfaceType = System.Net.NetworkInformation.NetworkInterfaceType.GigabitEthernet Then
                Return ni
            End If
            If fallback Is Nothing Then fallback = ni
        Next
        Return fallback
    End Function

    Private Shared Function GetVpnStatus() As String
        Try
            For Each ni As System.Net.NetworkInformation.NetworkInterface In System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                If ni.OperationalStatus <> System.Net.NetworkInformation.OperationalStatus.Up Then Continue For
                Dim desc As String = (If(ni.Description, "") & " " & If(ni.Name, "")).ToLowerInvariant()
                Dim isVpnish As Boolean = ni.NetworkInterfaceType = System.Net.NetworkInformation.NetworkInterfaceType.Tunnel OrElse _
                                          desc.Contains("vpn") OrElse desc.Contains("tap") OrElse _
                                          desc.Contains("tun") OrElse desc.Contains("wintun") OrElse _
                                          desc.Contains("wireguard") OrElse desc.Contains("ppp")
                If Not isVpnish Then Continue For
                Dim props As System.Net.NetworkInformation.IPInterfaceProperties = ni.GetIPProperties()
                If props Is Nothing OrElse props.UnicastAddresses Is Nothing Then Continue For
                For Each addr As System.Net.NetworkInformation.UnicastIPAddressInformation In props.UnicastAddresses
                    If addr.Address Is Nothing Then Continue For
                    If addr.Address.AddressFamily = System.Net.Sockets.AddressFamily.InterNetwork Then
                        Dim name As String = ni.Name
                        If String.IsNullOrEmpty(name) Then name = ni.Description
                        Return "ON — " & name
                    End If
                Next
            Next
        Catch
        End Try
        Return "OFF"
    End Function

    Private Shared Function GetConnectedWifiSsid() As String
        Try
            Dim text As String = modQuickControls.RunCommand("netsh", "wlan show interfaces")
            For Each line As String In text.Split(New String() {vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
                Dim trimmed As String = line.Trim()
                If trimmed.StartsWith("SSID", StringComparison.OrdinalIgnoreCase) AndAlso _
                   Not trimmed.StartsWith("BSSID", StringComparison.OrdinalIgnoreCase) AndAlso _
                   trimmed.Contains(":") Then
                    Dim value As String = trimmed.Substring(trimmed.IndexOf(":"c) + 1).Trim()
                    If Not String.IsNullOrEmpty(value) Then Return value
                End If
            Next
        Catch
        End Try
        Return ""
    End Function
End Class
