Public Class Installing

    Public Event ProgressChanged(ByVal CurrentProgress As Decimal)
    Public Event DisplayMessage(ByVal ComponentName As String)
    Public Event UpdateComplete()

    Dim x32Closed As Boolean = False
    Dim fileList As New Collection
    Dim runList As New Collection
    Dim extractList As New Collection
    Dim customList As New Collection
    Dim version As String
    Dim InstallThread As System.Threading.Thread
    Dim path As String = ""
    Dim stagingDir As String = ""
    Dim failed As Boolean = False
    Dim lastInstallError As String = ""
    Dim restartLcarsAfterClose As Boolean = False
    Private pillTimer As Timer = Nothing
    Private pills As New System.Collections.Generic.List(Of AnimPill)
    Private pillSpawnCooldown As Integer = 0
    Private chromePulseTick As Integer = 0
    Private scanBarX As Integer = 0
    Private finishMenuSlide As Double = 0.0 ' 0 = hidden below, 1 = fully up
    Private finishMenuActive As Boolean = False
    Private Shared ReadOnly ExitWindowsHelper As New cWrapExitWindows()
    Private Const PillW As Integer = 120
    Private Const PillH As Integer = 28
    Private Const PillGap As Integer = 8
    Private Const PillMargin As Integer = 10 ' inset so pills never hug chrome edges
    ' Keep left backlog short so parked pills stay below the top travel lane (no Y clamp).
    Private Const MaxLeftStack As Integer = 4
    ' ~2 accept-queue pills before the oldest exits off-screen.
    Private Const MaxRightStack As Integer = 2
    Private Const RightDwellTicks As Integer = 22
    Private Const ChromeInset As Integer = 18
    Private Const ChromeRail As Integer = 14
    Private Const FinishMenuSlideStep As Double = 0.07
    Private Shared ReadOnly LcarsOrange As Color = Color.FromArgb(255, 153, 0)
    Private Shared ReadOnly LcarsBlue As Color = Color.FromArgb(51, 102, 204)
    Private Shared ReadOnly LcarsTan As Color = Color.FromArgb(204, 153, 102)
    Private Shared ReadOnly LcarsPurple As Color = Color.FromArgb(153, 102, 204)

    Private Enum PillPhase
        Entering = 0   ' slide in from left into bottom of left stack
        LeftStack = 1  ' parked in left backlog (slot 0 = bottom)
        DepartUp = 2   ' top of left stack rises toward top rail
        Across = 3     ' travel left→right along top
        RightStack = 4 ' short accept buffer on upper right (slot 0 = intake)
        Exiting = 5    ' push off screen to the right
    End Enum

    Private Class AnimPill
        Public Panel As Panel
        Public Phase As PillPhase = PillPhase.Entering
        Public Slot As Integer = 0
        Public Anim As Double = 0.0 ' 0..1 within current phase
        Public DepartFromY As Integer = 0 ' actual panel Y when DepartUp begins
        Public RegionApplied As Boolean = False
        Public ColorWave As Integer = 0
    End Class

    Private Shared ReadOnly ProtectedStagingFiles As String() = {"runInstallScript.exe", "Ionic.Zip.Reduced.dll"}

    Public Structure FileEntry
        Dim name As String
        Dim version As String
        Dim custom As String
    End Structure

#Region " Window Resizing "
    Declare Function RegisterWindowMessageA Lib "user32.dll" (ByVal lpString As String) As Integer
    Public Declare Auto Function SendMessage Lib "user32.dll" (ByVal hWnd As IntPtr, ByVal msg As Integer, ByVal wParam As IntPtr, ByVal lParam As IntPtr) As IntPtr
    Private Declare Function PostMessage Lib "user32.dll" Alias "PostMessageA" (ByVal hwnd As Integer, ByVal wMsg As Integer, ByVal wParam As Integer, ByVal lParam As Integer) As Integer
    Private Declare Function SystemParametersInfo Lib "user32" Alias "SystemParametersInfoA" (ByVal uAction As Integer, ByVal uParam As Integer, ByVal lpvParam As IntPtr, ByVal fuWinIni As Integer) As Integer

    Public InterMsgID As Integer
    Const WM_COPYDATA As Integer = &H4A
    Const SPI_SETWORKAREA As Integer = 47
    Const SPIF_SENDCHANGE As Integer = &H2
    Dim x32Handle As IntPtr = IntPtr.Zero
    Public Const HWND_BROADCAST As Integer = &HFFFF
    ''' <summary>Installer must ignore LCARS working-area pushes or Finish falls under the menu chrome.</summary>
    Private ignoreWorkingAreaResize As Boolean = True

    Structure COPYDATASTRUCT
        Public dwData As IntPtr
        Public cdData As Integer
        Public lpData As IntPtr
    End Structure

    <Runtime.InteropServices.StructLayout(Runtime.InteropServices.LayoutKind.Sequential)> _
    Private Structure RECT
        Public Left As Integer
        Public Top As Integer
        Public Right As Integer
        Public Bottom As Integer
    End Structure
#End Region

    Protected Overrides Sub WndProc(ByRef m As System.Windows.Forms.Message)
        If m.Msg = InterMsgID And m.LParam = 13 Then
            x32Closed = True
        ElseIf m.Msg = WM_COPYDATA And m.WParam = x32Handle And Not x32Handle = IntPtr.Zero Then
            ' LCARS sends the shell working area here. Applying it insets the installer by the
            ' menu bars and often clips Finish off the bottom of the tablet. Stay fullscreen.
            If ignoreWorkingAreaResize Then
                m.Result = New IntPtr(1)
                Return
            End If
            Try
                Dim myData As New COPYDATASTRUCT
                myData = System.Runtime.InteropServices.Marshal.PtrToStructure(m.LParam, GetType(COPYDATASTRUCT))

                Dim myRect As New Rectangle
                myRect = System.Runtime.InteropServices.Marshal.PtrToStructure(myData.lpData, GetType(Rectangle))

                If Not Me.Bounds = myRect Then
                    Me.Bounds = myRect
                End If
            Catch ex As Exception
                WriteInstallCrashLog("WndProc resize failed: " & ex.ToString())
            End Try

        Else
            MyBase.WndProc(m)

        End If

    End Sub

    ''' <summary>Cover the full physical monitor — never the LCARS-shrunk working area.</summary>
    Private Sub ApplyFullScreenBounds()
        Dim scr As Screen = Screen.FromControl(Me)
        If scr Is Nothing Then scr = Screen.PrimaryScreen
        Dim b As Rectangle = scr.Bounds
        If Not Me.Bounds.Equals(b) Then
            Me.Bounds = b
        End If
        Me.TopMost = True
        Me.BringToFront()
    End Sub

    ''' <summary>Restore the system work area to the full monitor so the desktop is usable after LCARS exits.</summary>
    Private Sub RestoreSystemWorkArea()
        Try
            Dim scr As Screen = Screen.PrimaryScreen
            Dim r As New RECT()
            r.Left = scr.Bounds.Left
            r.Top = scr.Bounds.Top
            r.Right = scr.Bounds.Right
            r.Bottom = scr.Bounds.Bottom
            Dim ptr As IntPtr = Runtime.InteropServices.Marshal.AllocHGlobal(Runtime.InteropServices.Marshal.SizeOf(GetType(RECT)))
            Try
                Runtime.InteropServices.Marshal.StructureToPtr(r, ptr, False)
                ' Do not SPIF_SENDCHANGE — that broadcast often wakes SystemSettings and leaves a
                ' cloaked "Settings" ghost on the LCARS taskbar until reboot.
                SystemParametersInfo(SPI_SETWORKAREA, Runtime.InteropServices.Marshal.SizeOf(GetType(RECT)), ptr, 0)
            Finally
                Runtime.InteropServices.Marshal.FreeHGlobal(ptr)
            End Try
            ForceKillProcesses(New String() {"SystemSettings"})
        Catch ex As Exception
            WriteInstallLog("RestoreSystemWorkArea failed: " & ex.Message)
        End Try
    End Sub

    Private Sub sbCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles sbCancel.Click
        ShellFallback.ClearUpdateInProgressFlag()
        ClearTempDirectory()
        ShellFallback.EnsureExplorerRunningIfNeeded()
        Me.Close()
    End Sub

    Private Sub Installing_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        WriteInstallLog("Installer loading. StartupPath=" & Application.StartupPath)
        AddHandler Application.ThreadException, AddressOf OnUiThreadException
        AddHandler AppDomain.CurrentDomain.UnhandledException, AddressOf OnUnhandledException
        Try
            EnsureInstallChrome()
            stagingDir = ResolveStagingDirectory()
            WriteInstallLog("Staging folder: " & stagingDir)
            'Code for x32 messages
            InterMsgID = RegisterWindowMessageA("LCARS_X32_MSG")
            Dim handleText As String = GetSetting("LCARS x32", "Application", "MainWindowHandle", "0")
            Dim handleValue As Integer = 0
            Integer.TryParse(handleText, handleValue)
            x32Handle = New IntPtr(handleValue)
            If x32Handle <> IntPtr.Zero Then
                SendMessage(x32Handle, InterMsgID, Me.Handle, 1)
            End If
            ' Full physical screen — WorkingArea is inset by LCARS menus and clips Finish.
            ignoreWorkingAreaResize = True
            ApplyFullScreenBounds()
            LoadInstallScript()
            ' Do not animate while waiting for user Continue/Cancel ack.
            StopPillAnimation()
        Catch ex As Exception
            WriteInstallCrashLog(ex.ToString())
            MsgBox("runInstallScript could not start." & vbNewLine & vbNewLine & ex.Message & vbNewLine & vbNewLine & _
                   "Details saved to:" & vbNewLine & CrashLogPath() & vbNewLine & _
                   "Also check: C:\ProgramData\LCARS x32\lcars-install-log.txt")
            ShellFallback.EnsureExplorerRunningIfNeeded()
            Me.Close()
        End Try
    End Sub

    ''' <summary>
    ''' Lightweight LCARS borders + multi-pill corridor (animation only during install work).
    ''' Lower-left stack (max 6) → travel top → upper-right (max 2) → exit off-screen.
    ''' </summary>
    Private Sub EnsureInstallChrome()
        Me.DoubleBuffered = True
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or ControlStyles.OptimizedDoubleBuffer, True)
        Me.UpdateStyles()
        LayoutInstallContentAwayFromAnimCorridor()
        BringInstallButtonsToFront()
    End Sub

    ''' <summary>Keep status text in the center well; pills travel only on left/top chrome rails.</summary>
    Private Sub LayoutInstallContentAwayFromAnimCorridor()
        Dim contentLeft As Integer = ChromeInset + ChromeRail + PillW + PillMargin * 2 + 24
        Dim contentTop As Integer = ChromeInset + ChromeRail + PillH + PillMargin * 2 + 28
        Dim contentRightPad As Integer = ChromeInset + ChromeRail + PillW + PillMargin * 2 + 16
        Dim contentWidth As Integer = Math.Max(200, Me.ClientSize.Width - contentLeft - contentRightPad)
        If lblTitle IsNot Nothing Then
            lblTitle.Left = contentLeft
            lblTitle.Top = contentTop
            lblTitle.Width = Math.Min(lblTitle.Width, contentWidth)
        End If
        If lblMessage IsNot Nothing Then
            lblMessage.Left = contentLeft
            lblMessage.Top = contentTop + 44
            lblMessage.Width = contentWidth
            lblMessage.Height = Math.Max(80, Me.ClientSize.Height - lblMessage.Top - ChromeInset - ChromeRail - 120)
        End If
        If pnlInstalling IsNot Nothing Then
            pnlInstalling.Left = contentLeft
            pnlInstalling.Top = contentTop + 44
            pnlInstalling.Width = contentWidth
            pnlInstalling.Height = Math.Max(120, Me.ClientSize.Height - pnlInstalling.Top - ChromeInset - ChromeRail - 100)
        End If
    End Sub

    Private Sub BringInstallButtonsToFront()
        If sbContinue IsNot Nothing AndAlso sbContinue.Visible Then sbContinue.BringToFront()
        If sbCancel IsNot Nothing AndAlso sbCancel.Visible Then sbCancel.BringToFront()
        If pnlFinishMenu IsNot Nothing AndAlso pnlFinishMenu.Visible Then pnlFinishMenu.BringToFront()
    End Sub

    ''' <summary>Apply rounded region once per pill — recreating every tick flashes chrome behind.</summary>
    Private Sub EnsurePillRoundedRegion(ByVal ap As AnimPill)
        If ap Is Nothing OrElse ap.Panel Is Nothing OrElse ap.RegionApplied Then Return
        Try
            Dim pill As Panel = ap.Panel
            Dim rect As New Rectangle(0, 0, pill.Width, pill.Height)
            Dim radius As Integer = Math.Max(2, pill.Height \ 2)
            Dim path As New System.Drawing.Drawing2D.GraphicsPath()
            Dim diameter As Integer = radius * 2
            path.AddArc(rect.X, rect.Y, diameter, diameter, 90, 180)
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 180)
            path.CloseFigure()
            If pill.Region IsNot Nothing Then pill.Region.Dispose()
            pill.Region = New Region(path)
            ap.RegionApplied = True
        Catch
        End Try
    End Sub

    Private Function CreateAnimPillPanel() As Panel
        Dim pill As New Panel()
        pill.Name = "pnlAnimPill"
        pill.BackColor = LcarsOrange
        pill.Size = New Size(PillW, PillH)
        ' Stay invisible until first layout sets color + position (avoids 0,0 flash).
        pill.Visible = False
        Me.Controls.Add(pill)
        Return pill
    End Function

    Private Sub SetPillColor(ByVal panel As Panel, ByVal c As Color)
        If panel Is Nothing Then Return
        If panel.BackColor.ToArgb() <> c.ToArgb() Then
            panel.BackColor = c
        End If
    End Sub

    Private Sub SetPillLocation(ByVal panel As Panel, ByVal x As Integer, ByVal y As Integer)
        If panel Is Nothing Then Return
        If panel.Left <> x OrElse panel.Top <> y Then
            panel.Location = New Point(x, y)
        End If
    End Sub

    Private Sub BeginDepartUp(ByVal top As AnimPill)
        If top Is Nothing Then Return
        Dim bottomY As Integer = Me.ClientSize.Height - ChromeInset - ChromeRail - PillMargin - PillH
        Dim stackPitchY As Integer = PillH + PillGap
        Dim fallbackY As Integer = bottomY - (MaxLeftStack - 1) * stackPitchY
        If top.Panel IsNot Nothing AndAlso top.Panel.Visible AndAlso top.Panel.Top > ChromeInset Then
            top.DepartFromY = top.Panel.Top
        Else
            top.DepartFromY = fallbackY
        End If
        top.Phase = PillPhase.DepartUp
        top.Anim = 0.0
        top.Slot = 0
        SetPillColor(top.Panel, LcarsBlue)
    End Sub

    Private Sub StartPillAnimation()
        If pillTimer IsNot Nothing Then Return
        LayoutInstallContentAwayFromAnimCorridor()
        ClearAllPills()
        pillSpawnCooldown = 0
        chromePulseTick = 0
        scanBarX = ChromeInset
        SpawnPillEntering()
        pillTimer = New Timer()
        pillTimer.Interval = 60
        AddHandler pillTimer.Tick, AddressOf PillTimer_Tick
        pillTimer.Start()
        InvalidateChromeOnly()
    End Sub

    Private Sub StopPillAnimation()
        If pillTimer IsNot Nothing Then
            Try
                pillTimer.Stop()
                RemoveHandler pillTimer.Tick, AddressOf PillTimer_Tick
                pillTimer.Dispose()
            Catch
            End Try
            pillTimer = Nothing
        End If
        ClearAllPills()
    End Sub

    Private Sub ClearAllPills()
        For Each p As AnimPill In pills
            Try
                If p.Panel IsNot Nothing Then
                    Me.Controls.Remove(p.Panel)
                    If p.Panel.Region IsNot Nothing Then p.Panel.Region.Dispose()
                    p.Panel.Dispose()
                End If
            Catch
            End Try
        Next
        pills.Clear()
    End Sub

    Private Function CountLeftStack() As Integer
        Dim n As Integer = 0
        For Each p As AnimPill In pills
            If p.Phase = PillPhase.LeftStack OrElse p.Phase = PillPhase.Entering Then n += 1
        Next
        Return n
    End Function

    Private Function CountRightStack() As Integer
        Dim n As Integer = 0
        For Each p As AnimPill In pills
            If p.Phase = PillPhase.RightStack Then n += 1
        Next
        Return n
    End Function

    Private Function CountDepartingOrAcross() As Integer
        Dim n As Integer = 0
        For Each p As AnimPill In pills
            If p.Phase = PillPhase.DepartUp OrElse p.Phase = PillPhase.Across Then n += 1
        Next
        Return n
    End Function

    Private Sub SpawnPillEntering()
        ' If the left backlog is full, the top pill must leave before a new one can enter.
        If CountLeftStack() >= MaxLeftStack Then
            Dim top As AnimPill = Nothing
            For Each p As AnimPill In pills
                If p.Phase = PillPhase.LeftStack AndAlso (top Is Nothing OrElse p.Slot > top.Slot) Then
                    top = p
                End If
            Next
            If top Is Nothing Then Return
            BeginDepartUp(top)
            Dim lefts As New System.Collections.Generic.List(Of AnimPill)
            For Each p As AnimPill In pills
                If p.Phase = PillPhase.LeftStack Then lefts.Add(p)
            Next
            lefts.Sort(Function(a, b) a.Slot.CompareTo(b.Slot))
            For s As Integer = 0 To lefts.Count - 1
                lefts(s).Slot = s
            Next
        End If

        ' Incoming from the left pushes the existing left stack up one slot.
        For Each p As AnimPill In pills
            If p.Phase = PillPhase.LeftStack Then
                p.Slot += 1
            End If
        Next
        PromoteOverflowLeftTop()

        Dim ap As New AnimPill()
        ap.Panel = CreateAnimPillPanel()
        ap.Phase = PillPhase.Entering
        ap.Slot = 0
        ap.Anim = 0.0
        ap.ColorWave = chromePulseTick Mod 40
        pills.Add(ap)
        EnsurePillRoundedRegion(ap)
        SetPillColor(ap.Panel, LcarsOrange)
        LayoutAllPills()
        If ap.Panel IsNot Nothing Then ap.Panel.Visible = True
    End Sub

    Private Sub PillTimer_Tick(ByVal sender As Object, ByVal e As EventArgs)
        If Me.IsDisposed Then Return
        Try
            PillTimer_TickCore()
        Catch ex As Exception
            WriteInstallLog("PillTimer overflow/guard: " & ex.Message)
        End Try
    End Sub

    Private Sub PillTimer_TickCore()
        Const enterStep As Double = 0.045
        Const departStep As Double = 0.038
        Const acrossStep As Double = 0.030
        Const exitStep As Double = 0.042
        ' Slightly slower spawn so ~2 stay queued on the right before exit.
        Const spawnEvery As Integer = 24

        chromePulseTick += 1
        If chromePulseTick > 1000000 Then chromePulseTick = 0
        scanBarX += 14
        If scanBarX > Me.ClientSize.Width Then scanBarX = ChromeInset

        If finishMenuActive AndAlso finishMenuSlide < 1.0 Then
            finishMenuSlide = Math.Min(1.0, finishMenuSlide + FinishMenuSlideStep)
            LayoutFinishMenu()
        End If

        pillSpawnCooldown += 1
        If pillSpawnCooldown >= spawnEvery Then
            pillSpawnCooldown = 0
            ' Prefer departing when travel lane is quiet so stack drains smoothly.
            If CountLeftStack() >= MaxLeftStack - 1 AndAlso CountDepartingOrAcross() < 2 Then
                Dim top As AnimPill = Nothing
                For Each p As AnimPill In pills
                    If p.Phase = PillPhase.LeftStack AndAlso (top Is Nothing OrElse p.Slot > top.Slot) Then
                        top = p
                    End If
                Next
                If top IsNot Nothing Then BeginDepartUp(top)
            End If
            SpawnPillEntering()
        End If

        Dim i As Integer = 0
        While i < pills.Count
            Dim p As AnimPill = pills(i)
            Select Case p.Phase
                Case PillPhase.Entering
                    p.Anim += enterStep
                    If p.Anim >= 1.0 Then
                        p.Phase = PillPhase.LeftStack
                        p.Slot = 0
                        p.Anim = 0.0
                        PromoteOverflowLeftTop()
                    End If
                Case PillPhase.LeftStack
                    ' Parked until pushed up / promoted by a new entrant.
                    p.ColorWave = (p.ColorWave + 1) Mod 4000
                Case PillPhase.DepartUp
                    p.Anim += departStep
                    If p.Anim >= 1.0 Then
                        p.Phase = PillPhase.Across
                        p.Anim = 0.0
                    End If
                Case PillPhase.Across
                    p.Anim += acrossStep
                    If p.Anim >= 1.0 Then
                        For Each q As AnimPill In pills
                            If q.Phase = PillPhase.RightStack Then q.Slot += 1
                        Next
                        For Each q As AnimPill In pills
                            If q.Phase = PillPhase.RightStack AndAlso q.Slot >= MaxRightStack Then
                                q.Phase = PillPhase.Exiting
                                q.Anim = 0.0
                                q.Slot = 0
                            End If
                        Next
                        p.Phase = PillPhase.RightStack
                        p.Slot = 0
                        p.Anim = 0.0 ' dwell counter (ticks via RightDwellTicks)
                    End If
                Case PillPhase.RightStack
                    ' Buffer at the accept gate; overflow exit is handled when a new Across arrives.
                    p.Anim += 1.0
                    If p.Anim >= RightDwellTicks Then
                        p.Phase = PillPhase.Exiting
                        p.Anim = 0.0
                        p.Slot = 0
                    End If
                Case PillPhase.Exiting
                    p.Anim += exitStep
                    If p.Anim >= 1.0 Then
                        Try
                            Me.Controls.Remove(p.Panel)
                            If p.Panel.Region IsNot Nothing Then p.Panel.Region.Dispose()
                            p.Panel.Dispose()
                        Catch
                        End Try
                        pills.RemoveAt(i)
                        Continue While
                    End If
            End Select
            i += 1
        End While

        LayoutAllPills()
        InvalidateChromeOnly()
        BringInstallButtonsToFront()
    End Sub

    Private Sub PromoteOverflowLeftTop()
        For Each p As AnimPill In pills
            If p.Phase = PillPhase.LeftStack AndAlso p.Slot >= MaxLeftStack Then
                BeginDepartUp(p)
            End If
        Next
    End Sub

    Private Function PillWaveColor(ByVal baseOrange As Boolean, ByVal wave As Integer) As Color
        ' Soft secondary pulse between orange and tan / blue and purple — no full-form flash.
        Dim phase As Integer = Math.Abs(wave Mod 40)
        If phase > 20 Then phase = 40 - phase
        Dim t As Double = phase / 20.0
        If baseOrange Then
            Return BlendColor(LcarsOrange, LcarsTan, t * 0.35)
        End If
        Return BlendColor(LcarsBlue, LcarsPurple, t * 0.28)
    End Function

    Private Shared Function BlendColor(ByVal a As Color, ByVal b As Color, ByVal t As Double) As Color
        If t <= 0 Then Return a
        If t >= 1 Then Return b
        ' Cast Bytes to Integer first — VB Byte arithmetic overflows (checked) on orange↔tan blends.
        Dim ar As Integer = a.R
        Dim ag As Integer = a.G
        Dim ab As Integer = a.B
        Dim br As Integer = b.R
        Dim bg As Integer = b.G
        Dim bb As Integer = b.B
        Dim r As Integer = CInt(ar + (br - ar) * t)
        Dim g As Integer = CInt(ag + (bg - ag) * t)
        Dim bl As Integer = CInt(ab + (bb - ab) * t)
        If r < 0 Then r = 0
        If r > 255 Then r = 255
        If g < 0 Then g = 0
        If g > 255 Then g = 255
        If bl < 0 Then bl = 0
        If bl > 255 Then bl = 255
        Return Color.FromArgb(r, g, bl)
    End Function

    Private Sub LayoutAllPills()
        Dim leftX As Integer = ChromeInset + ChromeRail + PillMargin
        Dim bottomY As Integer = Me.ClientSize.Height - ChromeInset - ChromeRail - PillMargin - PillH
        ' Sit below the painted top rail with a clear margin (avoids upper-left chrome overlap).
        Dim topY As Integer = ChromeInset + ChromeRail + PillMargin
        Dim rightClusterLeft As Integer = Me.ClientSize.Width - ChromeInset - ChromeRail - PillMargin - (MaxRightStack * (PillW + PillGap) - PillGap)
        Dim stackPitchY As Integer = PillH + PillGap
        Dim stackPitchX As Integer = PillW + PillGap

        For Each p As AnimPill In pills
            If p.Panel Is Nothing Then Continue For
            EnsurePillRoundedRegion(p)
            Select Case p.Phase
                Case PillPhase.Entering
                    Dim startX As Integer = leftX - PillW - PillMargin
                    Dim x As Integer = CInt(startX + (leftX - startX) * Math.Min(1.0, p.Anim))
                    SetPillLocation(p.Panel, x, bottomY)
                    SetPillColor(p.Panel, LcarsOrange)
                Case PillPhase.LeftStack
                    ' Do NOT clamp into the top travel lane — that made one pill "stop short".
                    Dim y As Integer = bottomY - p.Slot * stackPitchY
                    SetPillLocation(p.Panel, leftX, y)
                    SetPillColor(p.Panel, PillWaveColor(True, p.ColorWave + p.Slot * 3))
                Case PillPhase.DepartUp
                    Dim fromY As Integer = p.DepartFromY
                    If fromY <= 0 Then fromY = bottomY - (MaxLeftStack - 1) * stackPitchY
                    Dim y As Integer = CInt(fromY + (topY - fromY) * Math.Min(1.0, p.Anim))
                    SetPillLocation(p.Panel, leftX, y)
                    SetPillColor(p.Panel, LcarsBlue)
                Case PillPhase.Across
                    Dim x As Integer = CInt(leftX + (rightClusterLeft - leftX) * Math.Min(1.0, p.Anim))
                    SetPillLocation(p.Panel, x, topY)
                    SetPillColor(p.Panel, LcarsBlue)
                Case PillPhase.RightStack
                    Dim x As Integer = rightClusterLeft + p.Slot * stackPitchX
                    SetPillLocation(p.Panel, x, topY)
                    SetPillColor(p.Panel, PillWaveColor(False, CInt(Math.Min(p.Anim, 10000.0)) + p.Slot * 5))
                Case PillPhase.Exiting
                    Dim startX As Integer = rightClusterLeft + (MaxRightStack - 1) * stackPitchX
                    Dim endX As Integer = Me.ClientSize.Width + PillMargin
                    Dim x As Integer = CInt(startX + (endX - startX) * Math.Min(1.0, p.Anim))
                    SetPillLocation(p.Panel, x, topY)
                    SetPillColor(p.Panel, LcarsBlue)
            End Select
            If Not p.Panel.Visible Then p.Panel.Visible = True
        Next
    End Sub

    Private Sub InvalidateChromeOnly()
        Dim w As Integer = Me.ClientSize.Width
        Dim h As Integer = Me.ClientSize.Height
        Dim inset As Integer = ChromeInset
        Dim rail As Integer = ChromeRail
        ' Top rail + elbow corner only — avoid full-form Invalidate (yellow flash).
        Me.Invalidate(New Rectangle(inset, inset, w - inset * 2, rail + 8))
        Me.Invalidate(New Rectangle(inset, inset, rail + 8, Math.Min(h - inset * 2, 120)))
        Me.Invalidate(New Rectangle(inset, h - inset - rail - 4, w - inset * 2, rail + 8))
    End Sub

    Protected Overrides Sub OnPaint(ByVal e As PaintEventArgs)
        MyBase.OnPaint(e)
        Dim g As Graphics = e.Graphics
        Dim w As Integer = Me.ClientSize.Width
        Dim h As Integer = Me.ClientSize.Height
        Dim inset As Integer = ChromeInset
        Dim rail As Integer = ChromeRail

        ' Pulsing elbow / rail brightness
        Dim pulse As Double = (Math.Sin(chromePulseTick * 0.12) + 1.0) * 0.5
        Dim railOrange As Color = BlendColor(LcarsOrange, Color.FromArgb(255, 200, 80), pulse * 0.45)
        Dim elbowBlue As Color = BlendColor(LcarsBlue, Color.FromArgb(90, 150, 255), pulse * 0.4)

        Using brushOrange As New SolidBrush(railOrange)
            Using brushBlue As New SolidBrush(elbowBlue)
                ' Top rail (starts after curved elbow)
                Dim elbowSize As Integer = Math.Max(36, rail * 3)
                g.FillRectangle(brushOrange, inset + elbowSize - 4, inset, w - inset * 2 - elbowSize - 70, rail)
                g.FillRectangle(brushBlue, w - inset - 70, inset, 70, rail)
                ' Left rail under elbow
                g.FillRectangle(brushOrange, inset, inset + elbowSize - 4, rail, h - inset * 2 - elbowSize - rail - 40)
                ' Curved upper-left elbow (not a square block)
                FillLcarsElbow(g, brushBlue, New Rectangle(inset, inset, elbowSize, elbowSize), ElbowCorner.UpperLeft, rail, rail)
                ' Bottom rail — clear left entry lane (no elbow block in the pill path)
                g.FillRectangle(brushOrange, inset, h - inset - rail, w - inset * 2 - 220, rail)
                ' Bottom-right accent near buttons
                g.FillRectangle(brushBlue, w - inset - 110, h - inset - rail, 110, rail)
            End Using
        End Using

        ' Scanning bar along top rail
        Dim scanW As Integer = 48
        Dim scanLeft As Integer = Math.Max(inset + 40, Math.Min(scanBarX, w - inset - scanW - 80))
        Using brushScan As New SolidBrush(Color.FromArgb(180, 255, 220, 120))
            g.FillRectangle(brushScan, scanLeft, inset + 2, scanW, Math.Max(2, rail - 4))
        End Using
    End Sub

    Private Enum ElbowCorner
        UpperLeft = 0
        UpperRight = 1
        LowerLeft = 2
        LowerRight = 3
    End Enum

    ''' <summary>
    ''' Draws a curved LCARS elbow (same geometry as LCARS.Controls.Elbow), not a square block.
    ''' </summary>
    Private Shared Sub FillLcarsElbow(ByVal g As Graphics, ByVal brush As Brush, ByVal bounds As Rectangle, ByVal corner As ElbowCorner, ByVal barW As Integer, ByVal barH As Integer)
        If bounds.Width < 8 OrElse bounds.Height < 8 Then Return
        Dim w As Integer = bounds.Width
        Dim h As Integer = bounds.Height
        barW = Math.Max(4, Math.Min(barW, w - 2))
        barH = Math.Max(4, Math.Min(barH, h - 2))

        Using bmp As New Bitmap(w, h)
            Using bg As Graphics = Graphics.FromImage(bmp)
                bg.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias
                bg.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality
                bg.Clear(Color.Black)
                ' Native UpperLeft elbow construction (matches Elbow.DrawButton)
                bg.FillEllipse(brush, 0, 0, h, h)
                bg.FillRectangle(brush, 0, h \ 2, barW, (h \ 2) + (h \ 4))
                bg.FillRectangle(brush, h \ 2, 0, w - (h \ 2), barH)
                bg.FillRectangle(brush, h \ 2, barH, Math.Max(0, barW - (h \ 2)), h - barH)
                bg.FillRectangle(Brushes.Black, barW, barH, w - barW, h - barH)
                bg.FillRectangle(brush, barW, barH, h \ 4, h \ 4)
                bg.FillEllipse(Brushes.Black, barW, barH, h \ 2, h \ 2)
            End Using

            Select Case corner
                Case ElbowCorner.UpperLeft
                    g.DrawImageUnscaled(bmp, bounds.X, bounds.Y)
                Case Else
                    Dim pts(2) As Point
                    Select Case corner
                        Case ElbowCorner.UpperRight
                            pts(0) = New Point(bounds.Right, bounds.Top)
                            pts(1) = New Point(bounds.Left, bounds.Top)
                            pts(2) = New Point(bounds.Right, bounds.Bottom)
                        Case ElbowCorner.LowerRight
                            pts(0) = New Point(bounds.Right, bounds.Bottom)
                            pts(1) = New Point(bounds.Left, bounds.Bottom)
                            pts(2) = New Point(bounds.Right, bounds.Top)
                        Case ElbowCorner.LowerLeft
                            pts(0) = New Point(bounds.Left, bounds.Bottom)
                            pts(1) = New Point(bounds.Right, bounds.Bottom)
                            pts(2) = New Point(bounds.Left, bounds.Top)
                    End Select
                    g.DrawImage(bmp, pts)
            End Select
        End Using
    End Sub

    Protected Overrides Sub OnResize(ByVal e As EventArgs)
        MyBase.OnResize(e)
        LayoutInstallContentAwayFromAnimCorridor()
        If finishMenuActive Then LayoutFinishMenu()
        InvalidateChromeOnly()
    End Sub

    Private Function ResolveStagingDirectory() As String
        ' Application.StartupPath is NOT reliable under UAC / host processes (can be
        ' System32, PowerShell, etc.). Prefer an explicit handoff file, then the EXE
        ' location from Assembly.Location, then any folder that actually has script.txt.
        Dim candidates As New System.Collections.Generic.List(Of String)

        Dim handoff As String = ReadStagingHandoffPath()
        If Not String.IsNullOrEmpty(handoff) Then
            candidates.Add(handoff)
        End If

        Try
            Dim asmPath As String = System.Reflection.Assembly.GetExecutingAssembly().Location
            If Not String.IsNullOrEmpty(asmPath) Then
                candidates.Add(System.IO.Path.GetDirectoryName(asmPath))
            End If
        Catch
        End Try

        candidates.Add(Application.StartupPath)

        ' Do NOT treat CLI args as staging — arg1 is the install folder (USB/portable), passed by LCARSUpdate.

        Dim localStaging As String = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LCARS x32")
        candidates.Add(System.IO.Path.Combine(localStaging, "UpdateStaging"))

        For Each candidate As String In candidates
            If Not String.IsNullOrEmpty(candidate) AndAlso _
               System.IO.Directory.Exists(candidate) AndAlso _
               System.IO.File.Exists(System.IO.Path.Combine(candidate, "script.txt")) Then
                Return candidate
            End If
        Next

        Dim tried As New System.Text.StringBuilder()
        For Each candidate As String In candidates
            If tried.Length > 0 Then tried.Append(" | ")
            If candidate Is Nothing Then
                tried.Append("(null)")
            Else
                tried.Append(candidate)
            End If
        Next
        Throw New System.IO.FileNotFoundException( _
            "Update script (script.txt) not found in any staging candidate." & vbNewLine & _
            "Tried: " & tried.ToString())
    End Function

    Private Function ReadStagingHandoffPath() As String
        Try
            Dim handoffFile As String = System.IO.Path.Combine( _
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), _
                "LCARS x32")
            handoffFile = System.IO.Path.Combine(handoffFile, "current-staging.txt")
            If Not System.IO.File.Exists(handoffFile) Then
                Return ""
            End If
            Dim staged As String = System.IO.File.ReadAllText(handoffFile).Trim()
            If staged <> "" AndAlso System.IO.Directory.Exists(staged) Then
                Return staged
            End If
        Catch
        End Try
        Return ""
    End Function

    Private Function StagingFile(ByVal fileName As String) As String
        Return System.IO.Path.Combine(stagingDir, fileName)
    End Function

    Private Sub LoadInstallScript()
        Dim scriptPath As String = StagingFile("script.txt")
        If Not System.IO.File.Exists(scriptPath) Then
            Throw New System.IO.FileNotFoundException( _
                "Update script not found. LCARSUpdate and runInstallScript are using different folders." & vbNewLine & scriptPath, _
                scriptPath)
        End If
        Dim myReader As New System.IO.StreamReader(scriptPath)
        Dim mode As String = myReader.ReadLine()
        Dim line As String = mode
        While mode <> "End Script" AndAlso mode IsNot Nothing
            Select Case mode
                Case "Program Version"
                    version = myReader.ReadLine()
                    myReader.ReadLine()
                    mode = myReader.ReadLine()
                Case "Install Path"
                    path = myReader.ReadLine()
                    myReader.ReadLine()
                    mode = myReader.ReadLine()
                Case "File List"
                    line = myReader.ReadLine()
                    Do While line <> "End File List"
                        Dim myEntry As New FileEntry
                        myEntry.name = line
                        myEntry.version = myReader.ReadLine()
                        fileList.Add(myEntry)
                        line = myReader.ReadLine()
                    Loop
                    mode = myReader.ReadLine()
                Case "Run List"
                    line = myReader.ReadLine()
                    Do While line <> "End Run List"
                        Dim myEntry As New FileEntry
                        myEntry.name = line
                        myEntry.version = myReader.ReadLine()
                        runList.Add(myEntry)
                        line = myReader.ReadLine()
                    Loop
                    mode = myReader.ReadLine()
                Case "Extract List"
                    line = myReader.ReadLine()
                    Do While line <> "End Extract List"
                        Dim myEntry As New FileEntry
                        myEntry.name = line
                        myEntry.version = myReader.ReadLine()
                        extractList.Add(myEntry)
                        line = myReader.ReadLine()
                    Loop
                    mode = myReader.ReadLine()
                Case Else
                    WriteInstallCrashLog("Unknown script section: " & mode)
                    Throw New InvalidOperationException("Problem reading update script at section: " & mode)
            End Select
        End While
        myReader.Close()
        path = ResolveInstallPath(path)
        WriteInstallLog("Install folder: " & path & "  Target version: " & version)
    End Sub

    Private Function ResolveInstallPath(ByVal scriptPath As String) As String
        ' Prefer explicit CLI arg from LCARSUpdate (portable/USB installs under UAC).
        Try
            Dim args() As String = Environment.GetCommandLineArgs()
            If args.Length >= 2 AndAlso Not String.IsNullOrEmpty(args(1)) Then
                Dim cliPath As String = args(1).Trim().Trim(""""c)
                If System.IO.File.Exists(System.IO.Path.Combine(cliPath, "LCARSmain.exe")) Then
                    WriteInstallLog("Using install folder from command line: " & cliPath)
                    Return cliPath
                End If
            End If
        Catch
        End Try

        ' Trust the path LCARSUpdate wrote into script.txt when that folder has LCARSmain.
        If Not String.IsNullOrEmpty(scriptPath) AndAlso System.IO.File.Exists(System.IO.Path.Combine(scriptPath, "LCARSmain.exe")) Then
            Return scriptPath
        End If
        Dim registryPath As String = GetSetting("LCARS x32", "Application", "InstallPath", "")
        If Not String.IsNullOrEmpty(registryPath) AndAlso System.IO.File.Exists(System.IO.Path.Combine(registryPath, "LCARSmain.exe")) Then
            WriteInstallLog("Script install path missing LCARSmain.exe; using registry InstallPath: " & registryPath)
            Return registryPath
        End If
        If Not String.IsNullOrEmpty(scriptPath) Then
            Return scriptPath
        End If
        If Not String.IsNullOrEmpty(registryPath) Then
            Return registryPath
        End If
        WriteInstallLog("WARNING: falling back to C:\Program Files\LCARS x32 — USB/portable install path was not resolved.")
        Return "C:\Program Files\LCARS x32"
    End Function

    Private Sub sbContinue_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles sbContinue.Click
        If sbContinue.Text = "CONTINUE" Then
            sbCancel.Visible = False
            sbContinue.Visible = False
            lblMessage.Text = "Waiting for all LCARS programs to close."
            ignoreWorkingAreaResize = True
            ApplyFullScreenBounds()
            StartPillAnimation()
            Application.DoEvents()

            ' Deactivate / OSK must not block the update wait — kill UI helpers first.
            Dim uiBlockers As String() = New String() {"LCARSshutdown", "OnScreenKeyboard", "LCARSLock"}
            ForceKillProcesses(uiBlockers)

            Dim myData As New COPYDATASTRUCT
            myData.dwData = 5 'Code to close LCARS
            Dim MyCopyData As IntPtr = System.Runtime.InteropServices.Marshal.AllocCoTaskMem(System.Runtime.InteropServices.Marshal.SizeOf(GetType(COPYDATASTRUCT)))
            System.Runtime.InteropServices.Marshal.StructureToPtr(myData, MyCopyData, False)
            ShellFallback.MarkPendingShellRestart()
            ShellFallback.MarkUpdateInProgress()
            WriteInstallLog("Marked update-in-progress.flag (blocks OSK prewarm during copy).")
            Dim res As Integer = SendMessage(x32Handle, WM_COPYDATA, Me.Handle, MyCopyData)
            System.Runtime.InteropServices.Marshal.FreeCoTaskMem(MyCopyData)

            Dim lcarsApps As String() = New String() {
                "LCARSmain", "LCARSUpdate", "LCARSWebBrowser", "OnScreenKeyboard",
                "LCARSTerminal", "LCARSexplorer", "LCARSshutdown",
                "LCARSengineering", "LCARSmedia", "LCARSpic", "LCARSdestruct"
            }
            ' Short polite wait, then force — never sit on Deactivate for 45s.
            If x32Handle <> IntPtr.Zero Then
                If Not WaitForProcessesToExit(New String() {"LCARSmain"}, 12) Then
                    WriteInstallLog("LCARSmain still running after polite wait; force-closing LCARS apps.")
                End If
                ForceKillProcesses(lcarsApps)
                If Not WaitForProcessesToExit(lcarsApps, 8) Then
                    ShellFallback.ClearPendingShellRestartFlag()
                    MsgBox("LCARS did not close in time. Some files may be locked." & vbNewLine & _
                           "If the desktop does not return, sign out and back in.")
                End If
            Else
                ForceKillProcesses(lcarsApps)
                WaitForProcessesToExit(lcarsApps, 8)
            End If

            RestoreSystemWorkArea()
            pnlInstalling.Visible = True
            pnlInstalling.BringToFront()
            ApplyFullScreenBounds()
            'Initialize the update thread
            InstallThread = New System.Threading.Thread(AddressOf InstallComponents)
            'Start the thread
            InstallThread.Start()
        End If
    End Sub

    Private Sub Installing_FormClosed(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosedEventArgs) Handles Me.FormClosed
        StopPillAnimation()
        ShellFallback.ClearUpdateInProgressFlag()
        If restartLcarsAfterClose Then
            Try
                RestartLcarsUnelevated()
            Catch ex As Exception
                WriteInstallLog("LCARS restart failed: " & ex.ToString())
                ShellFallback.ClearPendingShellRestartFlag()
                Try
                    Dim psi As New ProcessStartInfo()
                    psi.FileName = path & "\LCARSmain.exe"
                    psi.WorkingDirectory = path
                    psi.Arguments = "-u"
                    psi.UseShellExecute = True
                    Process.Start(psi)
                Catch ex2 As Exception
                    ShellFallback.EnsureExplorerRunningIfNeeded()
                    MsgBox("LCARS could not restart after the update." & vbNewLine & vbNewLine & ex2.ToString())
                End Try
            End Try
        End If
    End Sub

    ''' <summary>
    ''' Starts LCARSmain without elevating it and without using explorer.exe
    ''' (explorer.exe "app.exe" often opens folders instead of launching the app).
    ''' </summary>
    Private Sub RestartLcarsUnelevated()
        Dim lcarsExe As String = path & "\LCARSmain.exe"
        If Not System.IO.File.Exists(lcarsExe) Then
            Throw New System.IO.FileNotFoundException("LCARSmain.exe not found after install.", lcarsExe)
        End If
        Dim psi As New ProcessStartInfo()
        psi.FileName = lcarsExe
        psi.WorkingDirectory = path
        psi.Arguments = "-u"
        psi.UseShellExecute = True
        Process.Start(psi)
        WriteInstallLog("LCARS restart requested: " & lcarsExe & " -u")
    End Sub

    Private Sub RestartLcarsAfterUpdate()
        ' Kept for compatibility; restart now happens in FormClosed after the installer exits.
        restartLcarsAfterClose = True
        Me.Close()
    End Sub

    ''' <summary>
    ''' Waits until named processes exit, or deadline passes.
    ''' </summary>
    Private Function WaitForProcessesToExit(ByVal processNames As String(), Optional ByVal seconds As Integer = 45) As Boolean
        Dim deadline As Date = Date.Now.AddSeconds(seconds)
        Do
            Dim stillRunning As Boolean = False
            For Each processName As String In processNames
                If Process.GetProcessesByName(processName).Length > 0 Then
                    stillRunning = True
                    Exit For
                End If
            Next
            If Not stillRunning Then
                Return True
            End If
            If Date.Now > deadline Then
                Return False
            End If
            Threading.Thread.Sleep(200)
        Loop
    End Function

    ''' <summary>
    ''' Force-ends remaining LCARS apps so install files are not locked (Terminal was missing from the wait list).
    ''' </summary>
    Private Sub ForceKillProcesses(ByVal processNames As String())
        For Each processName As String In processNames
            For Each proc As Process In Process.GetProcessesByName(processName)
                Try
                    If Not proc.HasExited Then
                        WriteInstallLog("Force-closing process: " & processName & " pid=" & proc.Id.ToString())
                        proc.Kill()
                    End If
                Catch ex As Exception
                    WriteInstallLog("Force-close failed for " & processName & ": " & ex.Message)
                End Try
            Next
        Next
    End Sub

    Private Function CopyUpdateFile(ByVal sourceFile As String, ByVal targetFile As String) As Boolean
        If Not System.IO.File.Exists(sourceFile) Then
            lastInstallError = "Source missing: " & sourceFile
            WriteInstallCrashLog(lastInstallError)
            Return False
        End If

        Dim ext As String = System.IO.Path.GetExtension(targetFile).ToLowerInvariant()
        Dim isBinary As Boolean = (ext = ".exe" OrElse ext = ".dll")

        Dim attempt As Integer
        For attempt = 1 To 8
            Try
                ' Re-kill OSK/main helpers each attempt — USB locks and warm-prewarm can reappear.
                If isBinary Then
                    ForceKillProcesses(New String() {"OnScreenKeyboard", "LCARSmain", "LCARSWebBrowser", "LCARSTerminal", "LCARSexplorer"})
                    Threading.Thread.Sleep(200)
                End If

                If isBinary AndAlso System.IO.File.Exists(targetFile) Then
                    ' Windows allows renaming many in-use executables; then we can write a new file
                    ' at the original name. Critical for USB installs where overwrite-in-place fails.
                    Dim bak As String = targetFile & ".bak-update"
                    Try
                        If System.IO.File.Exists(bak) Then
                            System.IO.File.Delete(bak)
                        End If
                    Catch
                    End Try
                    Try
                        System.IO.File.Move(targetFile, bak)
                        WriteInstallCrashLog("Renamed locked target aside: " & targetFile)
                    Catch renameEx As Exception
                        WriteInstallCrashLog("Rename aside failed (will try overwrite): " & renameEx.Message)
                    End Try
                End If

                My.Computer.FileSystem.CopyFile(sourceFile, targetFile, True)

                If Not System.IO.File.Exists(targetFile) Then
                    lastInstallError = "Copy reported success but target missing: " & targetFile
                    WriteInstallCrashLog(lastInstallError)
                ElseIf New System.IO.FileInfo(sourceFile).Length <> New System.IO.FileInfo(targetFile).Length Then
                    lastInstallError = "Copy size mismatch for " & targetFile
                    WriteInstallCrashLog(lastInstallError)
                ElseIf Not FilesHaveSameMd5(sourceFile, targetFile) Then
                    lastInstallError = "Copy MD5 mismatch for " & targetFile
                    WriteInstallCrashLog(lastInstallError)
                    Try
                        System.IO.File.Delete(targetFile)
                    Catch
                    End Try
                Else
                    WriteInstallCrashLog("Copied OK (verified): " & targetFile)
                    ' Best-effort cleanup of renamed old binary
                    Try
                        Dim bakClean As String = targetFile & ".bak-update"
                        If System.IO.File.Exists(bakClean) Then
                            System.IO.File.Delete(bakClean)
                        End If
                    Catch
                    End Try
                    Return True
                End If
            Catch ex As Exception
                lastInstallError = ex.ToString()
                WriteInstallCrashLog("Copy attempt " & attempt.ToString() & " failed for " & targetFile & ": " & ex.Message)
                Threading.Thread.Sleep(400 + (attempt * 200))
            End Try
        Next
        Return False
    End Function

    ''' <summary>
    ''' Unpack a runtime zip (e.g. lib-vlc.zip) into the install folder. The zip file itself stays on disk for MD5 skip checks.
    ''' </summary>
    Private Function ExtractZipIntoInstall(ByVal zipPath As String, ByVal installPath As String) As Boolean
        Try
            RaiseEvent DisplayMessage("Extracting " & System.IO.Path.GetFileName(zipPath))
            Using zip As Ionic.Zip.ZipFile = Ionic.Zip.ZipFile.Read(zipPath)
                zip.ExtractAll(installPath, Ionic.Zip.ExtractExistingFileAction.OverwriteSilently)
            End Using
            WriteInstallLog("Extracted zip into install folder: " & zipPath)
            Return True
        Catch ex As Exception
            failed = True
            lastInstallError = "Zip extract failed for " & zipPath & vbNewLine & ex.ToString()
            WriteInstallCrashLog(lastInstallError)
            Return False
        End Try
    End Function

    ''' <summary>
    ''' After the main copy loop: kill OSK + LCARSmain, wait for exit, then force-replace
    ''' OnScreenKeyboard.exe with rename-aside + MD5 verify and many retries.
    ''' </summary>
    Private Function ForceReplaceOnScreenKeyboard() As Boolean
        Dim oskName As String = "OnScreenKeyboard.exe"
        Dim sourceFile As String = StagingFile(oskName)
        Dim oskWasInThisUpdate As Boolean = False
        For Each myComponent As FileEntry In fileList
            If String.Equals(myComponent.name, oskName, StringComparison.OrdinalIgnoreCase) Then
                oskWasInThisUpdate = True
                Exit For
            End If
        Next

        If Not System.IO.File.Exists(sourceFile) Then
            If oskWasInThisUpdate Then
                lastInstallError = "OnScreenKeyboard.exe was in the update list but missing from staging: " & sourceFile
                WriteInstallLog(lastInstallError)
                RaiseEvent DisplayMessage("OSK missing from staging — update incomplete")
                Return False
            End If
            WriteInstallLog("OSK force-replace skipped (not in this update).")
            Return True
        End If

        Dim targetFile As String = System.IO.Path.Combine(path, oskName)
        RaiseEvent DisplayMessage("Force-replacing OnScreenKeyboard.exe")
        WriteInstallLog("OSK force-replace start. target=" & targetFile & " staging=" & sourceFile & _
                        " stagingMD5=" & ComputeFileMd5(sourceFile))

        Dim blockers As String() = New String() {"OnScreenKeyboard", "LCARSmain"}
        Dim attempt As Integer
        For attempt = 1 To 18
            Try
                ForceKillProcesses(blockers)
                If Not WaitForProcessesToExit(blockers, 5) Then
                    WriteInstallCrashLog("OSK force-replace attempt " & attempt.ToString() & ": processes still running after WaitForExit")
                    ForceKillProcesses(blockers)
                    Threading.Thread.Sleep(500)
                End If

                If System.IO.File.Exists(targetFile) Then
                    Dim bak As String = targetFile & ".bak-update"
                    Try
                        If System.IO.File.Exists(bak) Then
                            System.IO.File.Delete(bak)
                        End If
                    Catch
                    End Try
                    Try
                        System.IO.File.Move(targetFile, bak)
                        WriteInstallCrashLog("OSK renamed aside: " & bak)
                    Catch renameEx As Exception
                        WriteInstallCrashLog("OSK rename aside failed (will try overwrite): " & renameEx.Message)
                        ' Last resort on USB: delete then copy (fails if still locked).
                        Try
                            System.IO.File.Delete(targetFile)
                        Catch
                        End Try
                    End Try
                End If

                My.Computer.FileSystem.CopyFile(sourceFile, targetFile, True)

                If Not System.IO.File.Exists(targetFile) Then
                    lastInstallError = "OSK force-replace: target missing after copy"
                    WriteInstallCrashLog(lastInstallError)
                ElseIf New System.IO.FileInfo(sourceFile).Length <> New System.IO.FileInfo(targetFile).Length Then
                    lastInstallError = "OSK force-replace: size mismatch"
                    WriteInstallCrashLog(lastInstallError)
                ElseIf Not FilesHaveSameMd5(sourceFile, targetFile) Then
                    lastInstallError = "OSK force-replace: MD5 mismatch"
                    WriteInstallCrashLog(lastInstallError)
                    Try
                        System.IO.File.Delete(targetFile)
                    Catch
                    End Try
                Else
                    Dim landedMd5 As String = ComputeFileMd5(targetFile)
                    WriteInstallCrashLog("Copied OK (verified): " & targetFile & " MD5=" & landedMd5 & _
                                        " [OSK force-replace attempt " & attempt.ToString() & "]")
                    Try
                        Dim bakClean As String = targetFile & ".bak-update"
                        If System.IO.File.Exists(bakClean) Then
                            System.IO.File.Delete(bakClean)
                        End If
                    Catch
                    End Try
                    ' Also copy build stamp when present in staging.
                    Dim stampName As String = "OnScreenKeyboard.build.txt"
                    Dim stampSrc As String = StagingFile(stampName)
                    If System.IO.File.Exists(stampSrc) Then
                        Try
                            My.Computer.FileSystem.CopyFile(stampSrc, System.IO.Path.Combine(path, stampName), True)
                        Catch
                        End Try
                    End If
                    ' Persist proof next to EXE for tablet diagnostics.
                    Try
                        System.IO.File.WriteAllText(System.IO.Path.Combine(path, "OnScreenKeyboard.installed-md5.txt"), _
                            landedMd5 & vbCrLf & targetFile & vbCrLf & DateTime.UtcNow.ToString("o") & vbCrLf)
                    Catch
                    End Try
                    Return True
                End If
            Catch ex As Exception
                lastInstallError = ex.ToString()
                WriteInstallCrashLog("OSK force-replace attempt " & attempt.ToString() & " failed: " & ex.Message)
            End Try
            Threading.Thread.Sleep(300 + (attempt * 150))
        Next

        lastInstallError = "OnScreenKeyboard.exe could not be replaced after " & attempt.ToString() & " attempts. Install folder: " & path
        WriteInstallLog(lastInstallError)
        RaiseEvent DisplayMessage("OSK force-replace FAILED — update incomplete")
        Return False
    End Function

    Private Function FilesHaveSameMd5(ByVal a As String, ByVal b As String) As Boolean
        Try
            Return String.Equals(ComputeFileMd5(a), ComputeFileMd5(b), StringComparison.OrdinalIgnoreCase)
        Catch
            Return False
        End Try
    End Function

    Private Function ComputeFileMd5(ByVal filePath As String) As String
        Dim hashBytes As Byte()
        Using stream As New System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite)
            Using md5 As New System.Security.Cryptography.MD5CryptoServiceProvider()
                hashBytes = md5.ComputeHash(stream)
            End Using
        End Using
        Dim builder As New System.Text.StringBuilder()
        Dim b As Byte
        For Each b In hashBytes
            builder.Append(String.Format("{0:x2}", b))
        Next
        Return builder.ToString()
    End Function

    Private Sub InstallComponents()
        Dim fileCopyFailed As Boolean = False
        Try
            If String.IsNullOrEmpty(path) Then
                failed = True
                lastInstallError = "Install path was not set in the update script."
                RaiseEvent DisplayMessage(lastInstallError)
                Return
            End If
            If Not System.IO.Directory.Exists(path) Then
                System.IO.Directory.CreateDirectory(path)
            End If
            Dim versionsPath As String = path & "\versions.txt"
            EnsureVersionsFile(versionsPath)
            Dim localVersions As New ProgramVersions(versionsPath)
            Dim componentsInstalled As Integer = 0
            Dim totalComponents As Integer = fileList.Count + customList.Count + extractList.Count + runList.Count
            If totalComponents < 1 Then
                totalComponents = 1
            End If
            Dim section As Decimal = 1D / totalComponents

            For Each myComponent As FileEntry In fileList
                RaiseEvent DisplayMessage("Copying " & myComponent.name)
                Try
                    Dim sourceFile As String = StagingFile(myComponent.name)
                    Dim targetFile As String = path & "\" & myComponent.name
                    If Not CopyUpdateFile(sourceFile, targetFile) Then
                        fileCopyFailed = True
                        failed = True
                        RaiseEvent DisplayMessage("Copying failed: " & myComponent.name)
                        lastInstallError = "Could not copy " & myComponent.name & " to " & targetFile & _
                            If(String.IsNullOrEmpty(lastInstallError), "", vbNewLine & lastInstallError)
                    Else
                        localVersions.UpdateVersion(myComponent.name, myComponent.version)
                        localVersions.SaveFile()
                        ' lib-vlc.zip (and any future runtime zips): keep the archive for MD5 skip checks, unpack payload.
                        If myComponent.name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) Then
                            If Not ExtractZipIntoInstall(targetFile, path) Then
                                failed = True
                                fileCopyFailed = True
                                RaiseEvent DisplayMessage("Extracting failed: " & myComponent.name)
                            End If
                        End If
                    End If
                Catch ex As Exception
                    failed = True
                    lastInstallError = ex.ToString()
                    RaiseEvent DisplayMessage("Copying failed: " & myComponent.name)
                End Try
                componentsInstalled += 1
                RaiseEvent ProgressChanged(componentsInstalled * section)
            Next

            ' Dedicated OSK force-replace after the main copy loop. Winlogon may have
            ' respawned LCARS mid-loop; kill again, wait for exit, then rename-aside + verify
            ' with many retries. Hard-fail if OnScreenKeyboard.exe does not land.
            If Not ForceReplaceOnScreenKeyboard() Then
                fileCopyFailed = True
                failed = True
            End If

            ' Hard-fail if critical binaries in THIS update did not land — partial updates
            ' previously left OnScreenKeyboard.exe on old code while LCARSmain updated.
            Dim criticalNames As New System.Collections.Generic.Dictionary(Of String, Boolean)(StringComparer.OrdinalIgnoreCase)
            criticalNames("LCARSmain.exe") = True
            criticalNames("LCARS.dll") = True
            criticalNames("OnScreenKeyboard.exe") = True
            criticalNames("LCARSUpdate.exe") = True
            criticalNames("runInstallScript.exe") = True
            Dim missingCritical As New System.Text.StringBuilder()
            For Each myComponent As FileEntry In fileList
                If Not criticalNames.ContainsKey(myComponent.name) Then Continue For
                Dim critPath As String = System.IO.Path.Combine(path, myComponent.name)
                Dim staged As String = StagingFile(myComponent.name)
                If Not System.IO.File.Exists(staged) Then
                    missingCritical.AppendLine(myComponent.name & " (missing from staging)")
                ElseIf Not System.IO.File.Exists(critPath) Then
                    missingCritical.AppendLine(myComponent.name & " (missing after copy)")
                ElseIf Not FilesHaveSameMd5(staged, critPath) Then
                    missingCritical.AppendLine(myComponent.name & " (MD5 mismatch after copy)")
                End If
            Next
            If missingCritical.Length > 0 Then
                failed = True
                fileCopyFailed = True
                lastInstallError = "Critical files did not update correctly:" & vbNewLine & missingCritical.ToString() & _
                    "Install folder: " & path
                WriteInstallLog(lastInstallError)
                RaiseEvent DisplayMessage("Critical file verify failed — re-run update. " & path)
            End If

            For Each myComponent As FileEntry In extractList
                RaiseEvent DisplayMessage("Extracting " & myComponent.name)
                Try
                    Using zip As Ionic.Zip.ZipFile = Ionic.Zip.ZipFile.Read(StagingFile(myComponent.name))
                        zip.ExtractAll(path, Ionic.Zip.ExtractExistingFileAction.OverwriteSilently)
                    End Using
                    ' Keep the zip in the install folder so LCARSUpdate MD5 checks can skip re-download.
                    Dim zipKeep As String = System.IO.Path.Combine(path, myComponent.name)
                    Dim zipSrc As String = StagingFile(myComponent.name)
                    If System.IO.File.Exists(zipSrc) Then
                        Try
                            System.IO.File.Copy(zipSrc, zipKeep, True)
                        Catch
                        End Try
                    End If
                    localVersions.UpdateVersion(myComponent.name, myComponent.version)
                    localVersions.SaveFile()
                Catch ex As Exception
                    failed = True
                    lastInstallError = ex.ToString()
                    RaiseEvent DisplayMessage("Extraction failed: " & myComponent.name)
                End Try
                componentsInstalled += 1
                RaiseEvent ProgressChanged(componentsInstalled * section)
            Next

            For Each myComponent As FileEntry In runList
                RaiseEvent DisplayMessage("Executing " & myComponent.name)
                Try
                    Shell(StagingFile(myComponent.name), AppWinStyle.NormalFocus, True)
                    localVersions.UpdateVersion(myComponent.name, myComponent.version)
                    localVersions.SaveFile()
                Catch ex As Exception
                    failed = True
                    lastInstallError = ex.ToString()
                    RaiseEvent DisplayMessage("Execution failed: " & myComponent.name)
                End Try
                componentsInstalled += 1
                RaiseEvent ProgressChanged(componentsInstalled * section)
            Next

            If Not fileCopyFailed Then
                localVersions.UpdateGlobalVersion(version)
                localVersions.SaveFile()
                SaveSetting("LCARS x32", "Application", "InstallPath", path)
                WriteInstallLog("Install complete. versions.txt global set to " & version & " in " & path)
            Else
                ' Never advance global version when critical binaries failed (e.g. OSK locked on USB).
                ' That previously left Settings on the new version while OnScreenKeyboard.exe stayed old.
                WriteInstallLog("Install finished with copy errors. Global version NOT updated. Folder: " & path & _
                    If(String.IsNullOrEmpty(lastInstallError), "", vbNewLine & lastInstallError))
            End If
        Catch ex As Exception
            failed = True
            lastInstallError = ex.ToString()
            WriteInstallLog("Install crashed: " & ex.ToString())
            RaiseEvent DisplayMessage("Install crashed: " & ex.Message)
        End Try

        Try
            WriteCleanupList()
            ' Do NOT clear staging here. Deleting payloads while this process is still
            ' running (and before Finish) both hides diagnostics and can crash the EXE.
        Catch ex As Exception
            WriteInstallCrashLog("Cleanup list failed: " & ex.ToString())
        End Try
        RaiseEvent UpdateComplete()
    End Sub

    Private Function IsProtectedStagingFile(ByVal fileName As String) As Boolean
        For Each protectedName As String In ProtectedStagingFiles
            If String.Equals(fileName, protectedName, StringComparison.OrdinalIgnoreCase) Then
                Return True
            End If
        Next
        Return False
    End Function

    Private Sub WriteCleanupList()
        Dim cleanupPath As String = StagingFile("cleanup.txt")
        Dim lines As New System.Collections.Generic.List(Of String)
        AddCleanupEntries(lines, fileList)
        AddCleanupEntries(lines, customList)
        AddCleanupEntries(lines, extractList)
        AddCleanupEntries(lines, runList)
        System.IO.File.WriteAllLines(cleanupPath, lines.ToArray())
    End Sub

    Private Sub AddCleanupEntries(ByVal lines As System.Collections.Generic.List(Of String), ByVal entries As Collection)
        For Each myFile As FileEntry In entries
            If Not IsProtectedStagingFile(myFile.name) Then
                lines.Add(myFile.name)
            End If
        Next
    End Sub

    ''' <summary>
    ''' Creates a minimal versions.txt when missing so ProgramVersions can load.
    ''' </summary>
    Private Sub EnsureVersionsFile(ByVal versionsPath As String)
        If System.IO.File.Exists(versionsPath) Then
            Return
        End If
        Dim installDir As String = System.IO.Path.GetDirectoryName(versionsPath)
        If Not String.IsNullOrEmpty(installDir) AndAlso Not System.IO.Directory.Exists(installDir) Then
            System.IO.Directory.CreateDirectory(installDir)
        End If
        System.IO.File.WriteAllText(versionsPath, If(String.IsNullOrEmpty(version), "0.0.0.0", version) & Environment.NewLine)
    End Sub

    Private Sub ClearTempDirectory()
        RaiseEvent DisplayMessage("Cleaning temp folder")
        TryDeleteTempFileEntries(fileList)
        TryDeleteTempFileEntries(customList)
        TryDeleteTempFileEntries(extractList)
        TryDeleteTempFileEntries(runList)
        TryDeleteFile(StagingFile("script.txt"))
    End Sub

    Private Sub TryDeleteTempFileEntries(ByVal entries As Collection)
        For Each myFile As FileEntry In entries
            If Not IsProtectedStagingFile(myFile.name) Then
                TryDeleteFile(StagingFile(myFile.name))
            End If
        Next
    End Sub

    Private Sub TryDeleteFile(ByVal filePath As String)
        Try
            If System.IO.File.Exists(filePath) Then
                My.Computer.FileSystem.DeleteFile(filePath)
            End If
        Catch
        End Try
    End Sub

    Private Sub Me_UpdateComplete() Handles Me.UpdateComplete
        If Me.InvokeRequired Then
            Me.BeginInvoke(New MethodInvoker(AddressOf Me_UpdateCompleteUi))
            Return
        End If
        Me_UpdateCompleteUi()
    End Sub

    Private Sub Me_UpdateCompleteUi()
        pnlInstalling.Visible = False
        sbContinue.Visible = False
        sbCancel.Visible = False
        ' Keep pill corridor running while the finish submenu is available.
        ApplyFullScreenBounds()
        If Not failed Then
            Dim oskNote As String = ""
            Try
                Dim oskPath As String = System.IO.Path.Combine(path, "OnScreenKeyboard.exe")
                Dim stampPath As String = System.IO.Path.Combine(path, "OnScreenKeyboard.build.txt")
                If System.IO.File.Exists(stampPath) Then
                    oskNote = vbNewLine & "OSK stamp: " & System.IO.File.ReadAllText(stampPath).Trim() & " @ " & oskPath
                ElseIf System.IO.File.Exists(oskPath) Then
                    oskNote = vbNewLine & "OSK path: " & oskPath
                End If
            Catch
            End Try
            If lblTitle IsNot Nothing Then
                lblTitle.Text = "Data Transfer complete"
            End If
            lblMessage.Text = "Update complete. You are now running version " & version & vbNewLine & _
                              "Choose an option from the system menu." & oskNote & vbNewLine & _
                              "Log: " & CrashLogPath()
        Else
            If lblTitle IsNot Nothing Then
                lblTitle.Text = "Data Transfer incomplete"
            End If
            lblMessage.Text = "Some components failed to update. Please re-run LCARSUpdate.exe to correct this problem." & vbNewLine & _
                              "Log: " & CrashLogPath()
            If Not String.IsNullOrEmpty(lastInstallError) Then
                lstStatus.Items.Add(lastInstallError)
            End If
            ShellFallback.EnsureExplorerRunning()
        End If
        ShowFinishMenu()
    End Sub

    Private Sub ShowFinishMenu()
        finishMenuActive = True
        finishMenuSlide = 0.0
        If pnlFinishMenu IsNot Nothing Then
            LayoutFinishMenuContents()
            ApplyFinishMenuPillShapes()
            pnlFinishMenu.Visible = True
            LayoutFinishMenu()
            pnlFinishMenu.BringToFront()
        End If
        ' Ensure the animation timer is running so the slide + pills continue.
        If pillTimer Is Nothing Then
            StartPillAnimation()
        End If
    End Sub

    ''' <summary>
    ''' Sizes the finish panel and stacks options with a double-height primary FINISHED choice.
    ''' </summary>
    Private Sub LayoutFinishMenuContents()
        If pnlFinishMenu Is Nothing Then Return
        Const chromePad As Integer = 38
        Const sidePad As Integer = 18
        Const btnH As Integer = 32
        Const finishH As Integer = 64
        Const gap As Integer = 6
        Const menuInnerW As Integer = 220

        Dim menuW As Integer = chromePad + menuInnerW + sidePad
        Dim y As Integer = chromePad + 2
        If lblFinishMenuTitle IsNot Nothing Then
            lblFinishMenuTitle.Location = New Point(chromePad, y)
            lblFinishMenuTitle.Size = New Size(menuInnerW, 22)
            lblFinishMenuTitle.TextAlign = ContentAlignment.MiddleLeft
            y += 26
        End If

        Dim optionBtns() As Button = {
            btnHibernate, btnSuspend, btnLock, btnLogOff,
            btnRestart, btnShutDown, btnCloseLcars
        }
        For Each b As Button In optionBtns
            If b Is Nothing Then Continue For
            b.Location = New Point(chromePad, y)
            b.Size = New Size(menuInnerW, btnH)
            b.TextAlign = ContentAlignment.MiddleRight
            b.Padding = New Padding(0, 0, 12, 0)
            y += btnH + gap
        Next

        ' Blank spacer, then the primary (default) choice — twice as tall.
        y += 8
        If btnFinished IsNot Nothing Then
            btnFinished.Location = New Point(chromePad, y)
            btnFinished.Size = New Size(menuInnerW, finishH)
            btnFinished.Text = ""
            btnFinished.TextAlign = ContentAlignment.MiddleCenter
            btnFinished.Padding = New Padding(8, 4, 8, 4)
            y += finishH
        End If

        Dim menuH As Integer = y + chromePad
        pnlFinishMenu.Size = New Size(menuW, menuH)
        pnlFinishMenu.BorderStyle = BorderStyle.None
        pnlFinishMenu.Padding = New Padding(0)
    End Sub

    ''' <summary>LCARS pill silhouette on finish-menu buttons (WinForms Button + Region).</summary>
    Private Sub ApplyFinishMenuPillShapes()
        Dim btns() As Button = {
            btnHibernate, btnSuspend, btnLock, btnLogOff,
            btnRestart, btnShutDown, btnCloseLcars, btnFinished
        }
        For Each b As Button In btns
            If b Is Nothing Then Continue For
            b.FlatStyle = FlatStyle.Flat
            b.FlatAppearance.BorderSize = 0
            Try
                Dim rect As New Rectangle(0, 0, Math.Max(1, b.Width), Math.Max(1, b.Height))
                Dim radius As Integer = Math.Max(2, Math.Min(rect.Height \ 2, 28))
                Dim diameter As Integer = radius * 2
                Dim path As New System.Drawing.Drawing2D.GraphicsPath()
                path.AddArc(rect.X, rect.Y, diameter, diameter, 90, 180)
                path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 180)
                path.CloseFigure()
                If b.Region IsNot Nothing Then b.Region.Dispose()
                b.Region = New Region(path)
            Catch
            End Try
        Next
    End Sub

    Private Sub LayoutFinishMenu()
        If pnlFinishMenu Is Nothing Then Return
        LayoutFinishMenuContents()
        ApplyFinishMenuPillShapes()
        Dim menuW As Integer = pnlFinishMenu.Width
        Dim menuH As Integer = pnlFinishMenu.Height
        Dim margin As Integer = ChromeInset + ChromeRail + 8
        Dim targetLeft As Integer = Me.ClientSize.Width - menuW - margin
        Dim targetTop As Integer = Me.ClientSize.Height - menuH - margin
        If targetLeft < margin Then targetLeft = margin
        If targetTop < margin Then targetTop = margin
        Dim startTop As Integer = Me.ClientSize.Height + 8
        Dim y As Integer = CInt(startTop + (targetTop - startTop) * Math.Min(1.0, finishMenuSlide))
        pnlFinishMenu.Location = New Point(targetLeft, y)
    End Sub

    ''' <summary>Draws LCARS rails and curved elbows around the finish options panel.</summary>
    Private Sub pnlFinishMenu_Paint(ByVal sender As Object, ByVal e As PaintEventArgs) Handles pnlFinishMenu.Paint
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias
        Dim w As Integer = pnlFinishMenu.ClientSize.Width
        Dim h As Integer = pnlFinishMenu.ClientSize.Height
        If w < 40 OrElse h < 40 Then Return

        Const rail As Integer = 10
        Const elbow As Integer = 36
        Const endCap As Integer = 10

        Using brushOrange As New SolidBrush(LcarsOrange)
            Using brushBlue As New SolidBrush(LcarsBlue)
                Using brushTan As New SolidBrush(LcarsTan)
                    ' Top rail + right end cap (elbow drawn after so it covers the join)
                    g.FillRectangle(brushOrange, elbow - 4, 0, Math.Max(0, w - elbow - endCap + 4), rail)
                    g.FillRectangle(brushTan, w - endCap, 0, endCap, rail)

                    ' Left vertical rail under the elbow
                    g.FillRectangle(brushOrange, 0, elbow - 4, rail, Math.Max(0, h - elbow - elbow + 8))

                    ' Bottom rail
                    g.FillRectangle(brushOrange, rail, h - rail, Math.Max(0, w - rail - elbow + 4), rail)

                    ' Right vertical accent
                    g.FillRectangle(brushTan, w - rail, elbow, rail, Math.Max(0, h - elbow * 2))

                    ' Curved elbows (not square blocks)
                    FillLcarsElbow(g, brushBlue, New Rectangle(0, 0, elbow, elbow), ElbowCorner.UpperLeft, rail, rail)
                    FillLcarsElbow(g, brushBlue, New Rectangle(w - elbow, h - elbow, elbow, elbow), ElbowCorner.LowerRight, rail, rail)
                End Using
            End Using
        End Using

        ' Thin inner guide line so content reads as inset from the chrome
        Using pen As New Pen(Color.FromArgb(90, LcarsOrange), 1)
            Dim inset As Integer = rail + 4
            g.DrawRectangle(pen, inset, inset, w - inset * 2 - 1, h - inset * 2 - 1)
        End Using
    End Sub

    Private Sub btnFinished_Paint(ByVal sender As Object, ByVal e As PaintEventArgs) Handles btnFinished.Paint
        Dim b As Button = TryCast(sender, Button)
        If b Is Nothing Then Return
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias
        Using brush As New SolidBrush(b.BackColor)
            g.FillRectangle(brush, b.ClientRectangle)
        End Using
        Dim main As String = "FINISHED"
        Dim subText As String = "(Return to LCARS)"
        Using fMain As New Font("Segoe UI", 12.0F, FontStyle.Bold)
            Using fSub As New Font("Segoe UI", 8.25F, FontStyle.Regular)
                Using brushText As New SolidBrush(b.ForeColor)
                    Dim mainSize As SizeF = g.MeasureString(main, fMain)
                    Dim subSize As SizeF = g.MeasureString(subText, fSub)
                    Dim totalH As Single = mainSize.Height + subSize.Height + 2.0F
                    Dim top As Single = (b.ClientSize.Height - totalH) / 2.0F
                    Dim mainX As Single = (b.ClientSize.Width - mainSize.Width) / 2.0F
                    Dim subX As Single = (b.ClientSize.Width - subSize.Width) / 2.0F
                    g.DrawString(main, fMain, brushText, mainX, top)
                    g.DrawString(subText, fSub, brushText, subX, top + mainSize.Height + 1.0F)
                End Using
            End Using
        End Using
    End Sub

    Private Sub WaitInstallThreadThen(ByVal action As MethodInvoker)
        If InstallThread IsNot Nothing AndAlso InstallThread.IsAlive Then
            InstallThread.Join(30000)
        End If
        If action IsNot Nothing Then action()
    End Sub

    Private Sub btnFinished_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnFinished.Click
        WaitInstallThreadThen(AddressOf DoFinishedReturnToLcars)
    End Sub

    Private Sub DoFinishedReturnToLcars()
        ClearTempDirectory()
        restartLcarsAfterClose = True
        Me.Close()
    End Sub

    Private Sub btnHibernate_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnHibernate.Click
        Try
            Application.SetSuspendState(PowerState.Hibernate, True, False)
        Catch ex As Exception
            WriteInstallLog("Hibernate failed: " & ex.Message)
        End Try
    End Sub

    Private Sub btnSuspend_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnSuspend.Click
        Try
            Application.SetSuspendState(PowerState.Suspend, True, False)
        Catch ex As Exception
            WriteInstallLog("Suspend failed: " & ex.Message)
        End Try
    End Sub

    Private Sub btnLock_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnLock.Click
        Try
            Dim lockExe As String = ""
            If Not String.IsNullOrEmpty(path) Then
                lockExe = System.IO.Path.Combine(path, "LCARSLock.exe")
            End If
            If String.IsNullOrEmpty(lockExe) OrElse Not System.IO.File.Exists(lockExe) Then
                lockExe = System.IO.Path.Combine(Application.StartupPath, "LCARSLock.exe")
            End If
            If System.IO.File.Exists(lockExe) Then
                Process.Start(lockExe)
            Else
                WriteInstallLog("LCARSLock.exe not found for Lock.")
            End If
        Catch ex As Exception
            WriteInstallLog("Lock failed: " & ex.Message)
        End Try
    End Sub

    Private Sub btnLogOff_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnLogOff.Click
        WaitInstallThreadThen(AddressOf DoLogOff)
    End Sub

    Private Sub DoLogOff()
        restartLcarsAfterClose = False
        ClearTempDirectory()
        ShellFallback.ClearUpdateInProgressFlag()
        ExitWindowsHelper.ExitWindows(cWrapExitWindows.Action.LogOff)
    End Sub

    Private Sub btnRestart_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnRestart.Click
        WaitInstallThreadThen(AddressOf DoRestart)
    End Sub

    Private Sub DoRestart()
        restartLcarsAfterClose = False
        ClearTempDirectory()
        ShellFallback.ClearUpdateInProgressFlag()
        ExitWindowsHelper.ExitWindows(cWrapExitWindows.Action.Restart)
    End Sub

    Private Sub btnShutDown_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnShutDown.Click
        WaitInstallThreadThen(AddressOf DoShutDown)
    End Sub

    Private Sub DoShutDown()
        restartLcarsAfterClose = False
        ClearTempDirectory()
        ShellFallback.ClearUpdateInProgressFlag()
        ExitWindowsHelper.ExitWindows(cWrapExitWindows.Action.Shutdown)
    End Sub

    Private Sub btnCloseLcars_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnCloseLcars.Click
        WaitInstallThreadThen(AddressOf DoCloseToDesktop)
    End Sub

    Private Sub DoCloseToDesktop()
        restartLcarsAfterClose = False
        ClearTempDirectory()
        RestoreSystemWorkArea()
        ShellFallback.EnsureExplorerRunningIfNeeded()
        Me.Close()
    End Sub

    Private Sub Me_ShowMessage(ByVal Message As String) Handles Me.DisplayMessage
        If Me.InvokeRequired Then
            Me.BeginInvoke(New StringHandler(AddressOf AppendStatusMessage), Message)
            Return
        End If
        AppendStatusMessage(Message)
    End Sub

    Private Delegate Sub StringHandler(ByVal message As String)

    Private Sub AppendStatusMessage(ByVal message As String)
        lstStatus.Items.Add(message)
    End Sub

    Private Sub Me_ProgressChanged(ByVal current As Decimal) Handles Me.ProgressChanged
        If Me.InvokeRequired Then
            Me.BeginInvoke(New ProgressHandler(AddressOf ApplyProgress), current)
            Return
        End If
        ApplyProgress(current)
    End Sub

    Private Delegate Sub ProgressHandler(ByVal current As Decimal)

    Private Sub ApplyProgress(ByVal current As Decimal)
        Dim pct As Integer = CInt(Math.Max(0D, Math.Min(100D, current * 100D)))
        Progress.Value = pct
        lblProgress.Text = pct.ToString() & "% complete"
    End Sub

    Private Sub OnUiThreadException(ByVal sender As Object, ByVal e As Threading.ThreadExceptionEventArgs)
        WriteInstallCrashLog(e.Exception.ToString())
        MsgBox("Installer error." & vbNewLine & vbNewLine & e.Exception.Message & vbNewLine & vbNewLine & _
               "Log: " & CrashLogPath())
    End Sub

    Private Sub OnUnhandledException(ByVal sender As Object, ByVal e As UnhandledExceptionEventArgs)
        Dim ex As Exception = TryCast(e.ExceptionObject, Exception)
        If ex IsNot Nothing Then
            WriteInstallCrashLog(ex.ToString())
        End If
    End Sub

    Private Function CrashLogPath() As String
        Dim dir As String = stagingDir
        If String.IsNullOrEmpty(dir) Then
            dir = Application.StartupPath
        End If
        Return System.IO.Path.Combine(dir, "lcars-install-log.txt")
    End Function

    Private Sub WriteInstallLog(ByVal text As String)
        Dim line As String = DateTime.Now.ToString("u") & "  " & text & vbCrLf
        ' Always try several locations so a missing log is never a mystery.
        AppendLogLine(CrashLogPath(), line)
        Try
            If Not String.IsNullOrEmpty(path) AndAlso System.IO.Directory.Exists(path) Then
                AppendLogLine(System.IO.Path.Combine(path, "lcars-install-log.txt"), line)
            End If
        Catch
        End Try
        Try
            Dim pub As String = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "LCARS x32")
            If Not System.IO.Directory.Exists(pub) Then
                System.IO.Directory.CreateDirectory(pub)
            End If
            AppendLogLine(System.IO.Path.Combine(pub, "lcars-install-log.txt"), line)
        Catch
        End Try
    End Sub

    Private Sub AppendLogLine(ByVal filePath As String, ByVal line As String)
        Try
            System.IO.File.AppendAllText(filePath, line)
        Catch
        End Try
    End Sub

    Private Sub WriteInstallCrashLog(ByVal text As String)
        WriteInstallLog(text)
    End Sub
End Class
