Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms
Imports LCARS.x32

''' <summary>
''' Mode 5: Klingon Empire Tactical Interface ("tlhIngan wo' ra'wI'").
''' Includes an animated Red Alert -> Subspace Virus Takeover transition on entry,
''' angular crimson/amber beveled geometry, Klingon trefoil & disruptor motifs,
''' pIqaD glyphs for Klingonese, and optional Terran English with phonetics in parentheses.
''' </summary>
Public Class frmMainscreen5
    Implements IAutohide

    Private myBusiness As modBusiness
    Private layoutBusy As Boolean = False

    ' Takeover animation state
    Private takeoverOverlay As TakeoverOverlayPanel
    Private takeoverTimer As Timer
    Private takeoverTickCount As Integer = 0
    Private Const TakeoverTotalTicks As Integer = 76 ' ~3.0 seconds at 40ms
    Private Const TakeoverPhase1Ticks As Integer = 24 ' ~1.0 second Red Alert intrusion warning
    Private takeoverCompleted As Boolean = False
    Private lastLeftButtonsBottom As Integer = 560
    Private Shared ReadOnly KlingonDesktopScreens As New HashSet(Of Integer)()
    Private Shared ReadOnly KlingonDesktopImages As New Dictionary(Of Integer, Image)()

    Public ReadOnly Property IsTakeoverInProgress() As Boolean
        Get
            Return Not takeoverCompleted
        End Get
    End Property

    ' Klingon vocabulary map (Control Name -> (Klingon, Terran))
    Private Structure KlingonLexEntry
        Public KlingonText As String
        Public TerranText As String
        Public IsCritical As Boolean
        Public Sub New(ByVal klingon As String, ByVal terran As String, Optional ByVal critical As Boolean = False)
            Me.KlingonText = klingon
            Me.TerranText = terran
            Me.IsCritical = critical
        End Sub
    End Structure

    Private Shared ReadOnly Lexicon As Dictionary(Of String, KlingonLexEntry) = BuildLexicon()

    Private Shared Function BuildLexicon() As Dictionary(Of String, KlingonLexEntry)
        Dim d As New Dictionary(Of String, KlingonLexEntry)(StringComparer.OrdinalIgnoreCase)
        d("myStartMenu") = New KlingonLexEntry("ghunmeH", "PROGRAMS", True)
        d("myUserButtons") = New KlingonLexEntry("nuv mIw", "PERSONAL", False)
        d("myButtonManager") = New KlingonLexEntry("nuv vu'", "MANAGE PROGRAMS", True)
        d("ShowTrayButton") = New KlingonLexEntry("De'Hom", "TRAY ICONS", False)
        d("mySpeech") = New KlingonLexEntry("ghogh", "SPEECH", False)
        d("myOSK") = New KlingonLexEntry("pIqaD", "KEYBOARD", False)
        d("myHelp") = New KlingonLexEntry("QaH", "MANUAL", False)
        d("MyComp") = New KlingonLexEntry("De'wI'", "COMPUTER", True)
        d("mySettings") = New KlingonLexEntry("DuH", "SETTINGS", False)
        d("myEngineering") = New KlingonLexEntry("jonwI'", "ENGINEERING", False)
        d("fbWebBrowser") = New KlingonLexEntry("QumwI'", "SUBSPACE NET", False)
        d("fbTerminal") = New KlingonLexEntry("ra'wI'", "TERMINAL", True)
        d("myPhoto") = New KlingonLexEntry("HaSta", "MEDIA VIEWER", False)
        d("myModeSelect") = New KlingonLexEntry("Segh wIv", "MODE SELECT", False)
        d("myAlert") = New KlingonLexEntry("ghuH Doq", "BATTLE ALERT", True)
        d("myDestruct") = New KlingonLexEntry("Qaw'", "SELF DESTRUCT", True)
        d("myDeactivate") = New KlingonLexEntry("mev", "DEACTIVATE", True)
        d("myDocuments") = New KlingonLexEntry("ghItlh", "DOCUMENTS", False)
        d("myPictures") = New KlingonLexEntry("nagh beQ", "PICTURES", False)
        d("myMusic") = New KlingonLexEntry("QoQ", "MUSIC", False)
        d("myVideos") = New KlingonLexEntry("jIH", "VIDEOS", False)
        d("fbDesktop") = New KlingonLexEntry("raS De'", "DESKTOP", False)
        d("fbMyNetwork") = New KlingonLexEntry("Qum De'", "NETWORK", False)
        d("myRun") = New KlingonLexEntry("chu'", "RUN COMMAND", True)
        d("myProgsUp") = New KlingonLexEntry("chap", "UP DIRECTORY", False)
        d("fbStartEdit") = New KlingonLexEntry("choH", "EDIT PINS", False)
        d("fbStartMode") = New KlingonLexEntry("Segh", "VIEW MODE", False)
        Return d
    End Function

    Public Function getAutohideEdges() As IAutohide.AutohideEdges Implements IAutohide.getAutohideEdges
        Return IAutohide.AutohideEdges.Top Or IAutohide.AutohideEdges.Bottom Or IAutohide.AutohideEdges.Left
    End Function

    Public Sub New(ByVal b As modBusiness)
        InitializeComponent()
        Me.myBusiness = b
        setDoubleBuffered(Me)
        setDoubleBuffered(pnlMainBar)
        setDoubleBuffered(pnlStart)
        setDoubleBuffered(pnlBatt)
        Try
            SetupKlingonChrome()
        Catch ex As Exception
            LogCrash("SetupKlingonChrome", ex)
            Throw
        End Try
        AddHandler Me.Shown, AddressOf OnKlingonShown
        AddHandler Me.Resize, AddressOf OnKlingonResize
        AddHandler pnlMainBar.Resize, AddressOf OnKlingonResize
        AddHandler Me.FormClosed, AddressOf OnClosedCleanup
    End Sub

    Private Shared Sub LogCrash(ByVal location As String, ByVal ex As Exception)
        Try
            Dim logPath As String = System.IO.Path.Combine(Application.StartupPath, "LCARS_Crash.log")
            System.IO.File.AppendAllText(logPath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & " [" & location & "] " & ex.Message & vbNewLine & ex.StackTrace & vbNewLine & vbNewLine)
        Catch
        End Try
    End Sub

    Public Shared ReadOnly Property ScreenImage() As Image
        Get
            Return CreateCorruptedPreviewImage()
        End Get
    End Property

    ''' <summary>
    ''' Refreshes all open frmMainscreen5 instances when the "Terran translations" setting changes.
    ''' </summary>
    Public Shared Sub RefreshAllKlingonTranslationStates()
        Try
            For Each f As Form In Application.OpenForms
                Dim kForm As frmMainscreen5 = TryCast(f, frmMainscreen5)
                If kForm IsNot Nothing AndAlso Not kForm.IsDisposed Then
                    kForm.ApplyKlingonLabels()
                    kForm.pnlMainBar.Invalidate(True)
                    kForm.pnlStart.Invalidate(True)
                End If
            Next
        Catch
        End Try
    End Sub

    Private Sub SetupKlingonChrome()
        ' Wire custom Klingon tactical painting on main containers and all buttons
        AddHandler pnlMainBar.Paint, AddressOf OnMainBarPaint
        AddHandler pnlStart.Paint, AddressOf OnStartPanelPaint
        AddHandler pnlBatt.Paint, AddressOf OnBattPanelPaint

        AttachKlingonButtonPainters(pnlMainBar)
        AttachKlingonButtonPainters(pnlStart)
        AttachKlingonButtonPainters(pnlTray)

        ' Also catch dynamically added buttons (such as QUICK or fbStartEdit / fbStartMode)
        AddHandler pnlMainBar.ControlAdded, AddressOf OnContainerControlAdded
        AddHandler pnlStart.ControlAdded, AddressOf OnContainerControlAdded

        ApplyKlingonLabels()
    End Sub

    Private Sub OnContainerControlAdded(ByVal sender As Object, ByVal e As ControlEventArgs)
        If e.Control Is Nothing Then Return
        Dim btn As LCARS.LCARSbuttonClass = TryCast(e.Control, LCARS.LCARSbuttonClass)
        If btn IsNot Nothing Then
            AttachSingleKlingonButtonPainter(btn)
            ApplyKlingonLabels()
        End If
    End Sub

    Private Sub AttachKlingonButtonPainters(ByVal parent As Control)
        If parent Is Nothing Then Return
        For Each c As Control In parent.Controls
            Dim btn As LCARS.LCARSbuttonClass = TryCast(c, LCARS.LCARSbuttonClass)
            If btn IsNot Nothing Then
                AttachSingleKlingonButtonPainter(btn)
            End If
        Next
    End Sub

    Private Sub AttachSingleKlingonButtonPainter(ByVal btn As LCARS.LCARSbuttonClass)
        If btn Is Nothing Then Return
        ' Skip tiny battery bars
        If btn.Name.StartsWith("fbBatt", StringComparison.OrdinalIgnoreCase) Then Return
        RemoveHandler btn.Paint, AddressOf OnKlingonButtonPaint
        AddHandler btn.Paint, AddressOf OnKlingonButtonPaint
    End Sub

    ''' <summary>
    ''' Applies Klingon vocabulary so loadLanguage() doesn't overwrite Mode 5 with Federation English.
    ''' </summary>
    Public Sub ApplyKlingonLabels()
        For Each kvp As KeyValuePair(Of String, KlingonLexEntry) In Lexicon
            Dim found() As Control = Me.Controls.Find(kvp.Key, True)
            If found IsNot Nothing AndAlso found.Length > 0 Then
                Dim lcarsBtn As LCARS.LCARSbuttonClass = TryCast(found(0), LCARS.LCARSbuttonClass)
                If lcarsBtn IsNot Nothing Then
                    lcarsBtn._ForceCaps = False
                    Dim caption As String = kvp.Value.KlingonText
                    If modSettings.KlingonTerranTranslations Then
                        caption = TerranWithPhonetics(kvp.Value.TerranText, kvp.Value.KlingonText)
                    End If
                    lcarsBtn.ButtonText = caption
                    lcarsBtn.Text = caption
                    lcarsBtn.Invalidate()
                End If
            End If
        Next

        ' Also style QUICK button if attached
        For Each ctrl As Control In pnlMainBar.Controls
            If ctrl IsNot Nothing AndAlso ctrl.Name.StartsWith("btnQuickControls", StringComparison.OrdinalIgnoreCase) Then
                Dim qBtn As LCARS.LCARSbuttonClass = TryCast(ctrl, LCARS.LCARSbuttonClass)
                If qBtn IsNot Nothing Then
                    qBtn._ForceCaps = False
                    AttachSingleKlingonButtonPainter(qBtn)
                    qBtn.Invalidate()
                End If
            End If
        Next
    End Sub

Private Sub OnKlingonShown(ByVal sender As Object, ByVal e As EventArgs)
        BeginInvoke(New MethodInvoker(Sub()
                                           Try
                                               ApplyKlingonLabels()
                                               EnsureKlingonLayout()
                                               ApplyKlingonDesktopWallpaper()
                                               StartVirusTakeoverSequence()
                                           Catch ex As Exception
                                               LogCrash("OnKlingonShown", ex)
                                           End Try
                                       End Sub))
    End Sub

    Private Sub OnKlingonResize(ByVal sender As Object, ByVal e As EventArgs)
        If layoutBusy Then Return
        EnsureKlingonLayout()
    End Sub

    ''' <summary>
    ''' Keeps all Klingon tactical panels, left warrior column, top command bar, bottom taskbar,
    ''' and central desktop hole cleanly aligned at any resolution.
    ''' </summary>
    Public Sub EnsureKlingonLayout()
        If layoutBusy OrElse Me.IsDisposed Then Return
        If Me.ClientSize.Width < 300 OrElse Me.ClientSize.Height < 240 Then Return
        layoutBusy = True
        Try
            Dim W As Integer = Me.ClientSize.Width
            Dim H As Integer = Me.ClientSize.Height

            If pnlMainBar.Bounds <> New Rectangle(0, 0, W, H) Then
                pnlMainBar.SetBounds(0, 0, W, H)
            End If

            Dim topBarH As Integer = 48
            Dim bottomBarH As Integer = 44
            Dim leftColW As Integer = 152
            Dim rightFramePad As Integer = 12
            Dim innerTop As Integer = topBarH + 4
            Dim innerBottom As Integer = H - bottomBarH - 4
            Dim innerH As Integer = Math.Max(120, innerBottom - innerTop)

            ' Top bar controls: left emblem [0..200], then Speech, OSK, Help, Terran Toggle, Weather/Clock on right
            mySpeech.SetBounds(212, 9, 86, 30)
            myOSK.SetBounds(304, 9, 86, 30)
            myHelp.SetBounds(396, 9, 86, 30)
            myClock.SetBounds(W - 216, 8, 204, 32)

            ' Left tactical column: Battery HUD at top, then 10 tactical buttons filling down to innerBottom
            pnlBatt.SetBounds(8, innerTop, leftColW - 14, 68)

            Dim leftButtons As Control() = {
                MyComp, mySettings, myEngineering, fbWebBrowser, fbTerminal,
                myPhoto, myModeSelect, myAlert, myDestruct, myDeactivate
            }
            Dim availBtnTop As Integer = pnlBatt.Bottom + 6
            Dim availBtnH As Integer = Math.Max(220, innerBottom - availBtnTop)
            Dim slotH As Integer = availBtnH \ leftButtons.Length
            Dim btnH As Integer = Math.Max(22, Math.Min(38, slotH - 4))
            Dim curY As Integer = availBtnTop
            For i As Integer = 0 To leftButtons.Length - 1
                Dim ctrl As Control = leftButtons(i)
                If ctrl Is Nothing Then Continue For
                If Object.ReferenceEquals(ctrl, myAlert) Then
                    ctrl.SetBounds(8, curY, leftColW - 44, btnH)
                    myAlertListButton.SetBounds(8 + leftColW - 40, curY, 26, btnH)
                Else
                    ctrl.SetBounds(8, curY, leftColW - 14, btnH)
                End If
                curY += btnH + 4
            Next
            lastLeftButtonsBottom = curY

            ' Bottom tactical bar: Start Menu (left), Apps strip (center), Quick + Tray + Personal (right)
            Dim botY As Integer = H - bottomBarH + 5
            Dim botBtnH As Integer = 34
            myStartMenu.SetBounds(8, botY, 146, botBtnH)
            myUserButtons.SetBounds(W - 150, botY, 142, botBtnH)

            Dim trayW As Integer = Math.Max(124, pnlTray.Width)
            pnlTray.SetBounds(myUserButtons.Left - trayW - 6, botY, trayW, botBtnH)
            ShowTrayButton.SetBounds(0, 0, trayW, botBtnH)
            HideTrayButton.SetBounds(trayW - 30, 2, 28, botBtnH - 4)

            ' Leave room for QUICK button between pnlApps and pnlTray/myUserButtons
            Dim quickReserve As Integer = 82
            Dim appsLeft As Integer = myStartMenu.Right + 10
            Dim appsWidth As Integer = Math.Max(100, pnlTray.Left - appsLeft - quickReserve)
            pnlApps.SetBounds(appsLeft, botY + 2, appsWidth, botBtnH - 4)

            ' Start Menu panel & Personal Programs grid
            Dim startW As Integer = Math.Min(356, Math.Max(280, (W - leftColW) \ 2))
            pnlStart.SetBounds(leftColW, innerTop, startW, innerH)
            LayoutStartPanelInternals(startW, innerH)

            Dim userGridW As Integer = 220
            gridUserButtons.SetBounds(W - userGridW - rightFramePad, innerTop, userGridW, innerH - 34)
            myButtonManager.SetBounds(W - userGridW - rightFramePad, innerBottom - 30, userGridW, 28)

            ' Central Desktop Hole (pnlMain)
            Dim mainLeft As Integer = leftColW
            If pnlStart.Visible Then
                mainLeft = pnlStart.Right + 6
            End If
            Dim mainRight As Integer = W - rightFramePad
            If gridUserButtons.Visible Then
                mainRight = gridUserButtons.Left - 6
            End If
            Dim mainW As Integer = Math.Max(100, mainRight - mainLeft)
            pnlMain.SetBounds(mainLeft, innerTop, mainW, innerH)

            If takeoverOverlay IsNot Nothing AndAlso Not takeoverOverlay.IsDisposed Then
                takeoverOverlay.SetBounds(0, 0, W, H)
                takeoverOverlay.BringToFront()
            End If

            pnlMainBar.Invalidate()
        Finally
            layoutBusy = False
        End Try

        If takeoverCompleted AndAlso myBusiness IsNot Nothing AndAlso myBusiness.isInit Then
            myBusiness.UpdateRegion()
        End If
    End Sub

    Private Sub LayoutStartPanelInternals(ByVal startW As Integer, ByVal startH As Integer)
        Dim rightColW As Integer = 126
        Dim progW As Integer = Math.Max(130, startW - rightColW - 18)
        Dim headerTop As Integer = 34
        Dim bottomNavH As Integer = 68
        Dim progH As Integer = Math.Max(100, startH - headerTop - bottomNavH)

        pnlPrograms.SetBounds(6, headerTop, progW, progH)
        myProgsUp.SetBounds(6, headerTop + progH + 4, progW, 28)
        myProgsBack.SetBounds(6, headerTop + progH + 36, 32, 28)
        fbProgramPages.SetBounds(40, headerTop + progH + 36, progW - 68, 28)
        myProgsNext.SetBounds(6 + progW - 32, headerTop + progH + 36, 32, 28)

        Dim rightX As Integer = progW + 12
        Dim startLaunchers As Control() = {
            myDocuments, myPictures, myMusic, myVideos, fbDesktop, fbMyNetwork, myRun
        }
        Dim ry As Integer = headerTop
        For Each c As Control In startLaunchers
            If c Is Nothing Then Continue For
            c.SetBounds(rightX, ry, rightColW, 34)
            ry += 38
        Next
    End Sub

#Region " Virus Takeover Animation Sequence (Red Alert -> Glitch -> Klingon Shell) "

    Private Sub StartVirusTakeoverSequence()
        If takeoverOverlay IsNot Nothing Then Return

        takeoverCompleted = False
        takeoverTickCount = 0

        ' While the full-screen takeover animation runs, clear the Form.Region so the entire
        ' monitor displays the Red Alert & Glitch takeover over the central area too.
        Me.Region = Nothing

        takeoverOverlay = New TakeoverOverlayPanel(Me)
        takeoverOverlay.Bounds = New Rectangle(0, 0, Me.ClientSize.Width, Me.ClientSize.Height)
        takeoverOverlay.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        Me.Controls.Add(takeoverOverlay)
        takeoverOverlay.BringToFront()

        AddHandler takeoverOverlay.MouseDown, AddressOf OnSkipTakeoverInput
        AddHandler Me.KeyDown, AddressOf OnSkipTakeoverKey

        takeoverTimer = New Timer()
        takeoverTimer.Interval = 40
        AddHandler takeoverTimer.Tick, AddressOf OnTakeoverTick
        takeoverTimer.Start()
    End Sub

    Private Sub OnSkipTakeoverInput(ByVal sender As Object, ByVal e As MouseEventArgs)
        ' Allow skipping after initial 12 ticks (~0.5s) if user clicks
        If takeoverTickCount > 12 Then
            CompleteTakeoverSequence()
        End If
    End Sub

    Private Sub OnSkipTakeoverKey(ByVal sender As Object, ByVal e As KeyEventArgs)
        If e.KeyCode = Keys.Escape OrElse e.KeyCode = Keys.Space OrElse e.KeyCode = Keys.Enter Then
            CompleteTakeoverSequence()
        End If
    End Sub

    Private Sub OnTakeoverTick(ByVal sender As Object, ByVal e As EventArgs)
        takeoverTickCount += 1
        If takeoverTickCount >= TakeoverTotalTicks Then
            CompleteTakeoverSequence()
            Return
        End If
        If takeoverOverlay IsNot Nothing AndAlso Not takeoverOverlay.IsDisposed Then
            takeoverOverlay.AdvanceTick(takeoverTickCount, TakeoverPhase1Ticks, TakeoverTotalTicks)
        End If
    End Sub

    Private Sub CompleteTakeoverSequence()
        If takeoverTimer IsNot Nothing Then
            takeoverTimer.Stop()
            RemoveHandler takeoverTimer.Tick, AddressOf OnTakeoverTick
            takeoverTimer.Dispose()
            takeoverTimer = Nothing
        End If
        RemoveHandler Me.KeyDown, AddressOf OnSkipTakeoverKey

        If takeoverOverlay IsNot Nothing Then
            Try
                Me.Controls.Remove(takeoverOverlay)
                takeoverOverlay.Dispose()
            Catch
            End Try
            takeoverOverlay = Nothing
        End If

        takeoverCompleted = True
        ApplyKlingonLabels()
        EnsureKlingonLayout()
        ApplyKlingonDesktopWallpaper()
        If myBusiness IsNot Nothing AndAlso myBusiness.isInit Then
            myBusiness.UpdateRegion()
            myBusiness.UpdateWorkingArea()
            modHudWeather.SyncWeatherLayoutPublic(myBusiness)
            modQuickControls.ScheduleSyncQuickButton(myBusiness)
        End If
        pnlMainBar.Invalidate(True)
    End Sub

    ''' <summary>
    ''' Fullscreen double-buffered panel that renders Phase 1 (LCARS Red Alert intrusion warning)
    ''' and Phase 2 (progressive chunk-by-chunk Klingon virus corruption & replacement).
    ''' </summary>
    Private Class TakeoverOverlayPanel
        Inherits Panel

        Private ReadOnly hostForm As frmMainscreen5
        Private curTick As Integer = 0
        Private phase1Max As Integer = 24
        Private totalMax As Integer = 76
        Private ReadOnly rng As New Random(471701)
        Private ReadOnly cellOrder As New List(Of Integer)()
        Private Const GridCols As Integer = 8
        Private Const GridRows As Integer = 6

        Public Sub New(ByVal owner As frmMainscreen5)
            Me.hostForm = owner
            Me.DoubleBuffered = True
            Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or ControlStyles.OptimizedDoubleBuffer, True)
            Me.BackColor = Color.Black

            ' Deterministic shuffled order of the 48 screen chunks for progressive virus takeover
            Dim totalCells As Integer = GridCols * GridRows
            For i As Integer = 0 To totalCells - 1
                cellOrder.Add(i)
            Next
            For i As Integer = totalCells - 1 To 1 Step -1
                Dim swapIdx As Integer = rng.Next(0, i + 1)
                Dim tmp As Integer = cellOrder(i)
                cellOrder(i) = cellOrder(swapIdx)
                cellOrder(swapIdx) = tmp
            Next
        End Sub

        Public Sub AdvanceTick(ByVal tick As Integer, ByVal p1 As Integer, ByVal total As Integer)
            Me.curTick = tick
            Me.phase1Max = p1
            Me.totalMax = total
            Me.Invalidate()
        End Sub

Protected Overrides Sub OnPaint(ByVal e As PaintEventArgs)
        Try
            Dim g As Graphics = e.Graphics
            g.SmoothingMode = SmoothingMode.AntiAlias
            Dim W As Integer = Math.Max(1, Me.Width)
            Dim H As Integer = Math.Max(1, Me.Height)
            g.Clear(Color.Black)

            Dim isPhase1 As Boolean = (curTick <= phase1Max)
            Dim phase2Progress As Single = 0.0F
            If Not isPhase1 Then
                phase2Progress = CSng(curTick - phase1Max) / CSng(Math.Max(1, totalMax - phase1Max))
                phase2Progress = Math.Max(0.0F, Math.Min(1.0F, phase2Progress))
            End If

            ' 1. Draw baseline LCARS Red Alert frame (flashing red/white LCARS elbows & bars)
            Dim flashWhite As Boolean = ((curTick \ 3) Mod 2 = 1)
            DrawLcarsRedAlertState(g, W, H, flashWhite)

            ' 2. In Phase 2, progressively replace chunks with Klingon tactical chrome + glitch artifacts
            If Not isPhase1 Then
                Dim totalCells As Integer = GridCols * GridRows
                Dim infectedCount As Integer = CInt(Math.Floor(phase2Progress * totalCells))
                Dim cellW As Single = CSng(W) / GridCols
                Dim cellH As Single = CSng(H) / GridRows

                For idx As Integer = 0 To infectedCount - 1
                    Dim cellId As Integer = cellOrder(idx)
                    Dim cx As Integer = cellId Mod GridCols
                    Dim cy As Integer = cellId \ GridCols
                    Dim cellRect As New RectangleF(cx * cellW, cy * cellH, cellW + 1, cellH + 1)

                    Dim st As GraphicsState = g.Save()
                    g.SetClip(cellRect)
                    DrawKlingonInfectedScene(g, W, H, curTick)
                    g.Restore(st)

                    ' Draw glitch border around newly infected cells
                    If idx >= infectedCount - 6 Then
                        Using glitchPen As New Pen(Color.FromArgb(220, 255, 200, 0), 2.0F)
                            g.DrawRectangle(glitchPen, cellRect.X, cellRect.Y, cellRect.Width, cellRect.Height)
                        End Using
                    End If
                Next

                ' Scanline / block-corruption / jitter bars across screen
                Dim glitchCount As Integer = 10 + CInt((1.0F - Math.Abs(phase2Progress - 0.5F) * 2.0F) * 28)
                For i As Integer = 0 To glitchCount - 1
                    Dim gy As Integer = rng.Next(0, H)
                    Dim gh As Integer = rng.Next(2, 18)
                    Dim gx As Integer = rng.Next(0, W \ 2)
                    Dim gw As Integer = rng.Next(W \ 4, W)
                    Dim col As Color
                    Select Case i Mod 4
                        Case 0
                            col = Color.FromArgb(190, 255, 20, 0)
                        Case 1
                            col = Color.FromArgb(170, 255, 180, 0)
                        Case 2
                            col = Color.FromArgb(200, 255, 255, 255)
                        Case Else
                            col = Color.FromArgb(210, 40, 0, 0)
                    End Select
                    Using sb As New SolidBrush(col)
                        g.FillRectangle(sb, gx, gy, gw, gh)
                    End Using
                Next

                ' Center takeover status banner
                DrawTakeoverBanner(g, W, H, False, phase2Progress, flashWhite)
            Else
                ' Phase 1: Pure LCARS Red Alert Intrusion Warning
                DrawTakeoverBanner(g, W, H, True, 0.0F, flashWhite)
            End If
        Catch ex As Exception
            LogCrash("TakeoverOverlayPanel.OnPaint", ex)
        End Try
    End Sub

        Private Sub DrawLcarsRedAlertState(ByVal g As Graphics, ByVal W As Integer, ByVal H As Integer, ByVal flashWhite As Boolean)
            Dim primaryAlert As Color = If(flashWhite, Color.White, Color.FromArgb(255, 230, 20, 20))
            Dim secondaryAlert As Color = If(flashWhite, Color.FromArgb(255, 230, 20, 20), Color.FromArgb(255, 150, 0, 0))

            Using bPrimary As New SolidBrush(primaryAlert), bSecondary As New SolidBrush(secondaryAlert)
                ' Top LCARS bar & elbow
                g.FillRectangle(bPrimary, 0, 0, 140, 96)
                g.FillRectangle(bPrimary, 140, 0, W - 140, 36)
                g.FillRectangle(bSecondary, 0, 102, 140, H - 156)
                g.FillRectangle(bSecondary, 140, 42, W - 140, 20)
                ' Bottom LCARS bar
                g.FillRectangle(bPrimary, 0, H - 48, W, 42)

                ' Red alert klaxon bars inside viewport
                For i As Integer = 0 To 5
                    Dim bx As Integer = 170 + i * ((W - 200) \ 6)
                    Dim c As Color = If((i + (If(flashWhite, 1, 0))) Mod 2 = 0, primaryAlert, secondaryAlert)
                    Using barB As New SolidBrush(c)
                        g.FillRectangle(barB, bx, 76, ((W - 220) \ 6) - 12, 18)
                    End Using
                Next
            End Using
        End Sub

        Private Sub DrawKlingonInfectedScene(ByVal g As Graphics, ByVal W As Integer, ByVal H As Integer, ByVal tick As Integer)
            g.Clear(Color.Black)

            ' Triangular tactical grid in center
            Dim gridStep As Integer = 44
            Using gridPen As New Pen(Color.FromArgb(55, 190, 15, 0), 1.0F)
                For y As Integer = 48 To H - 48 Step gridStep
                    g.DrawLine(gridPen, 150, y, W - 16, y)
                Next
                For x As Integer = 150 To W - 16 Step gridStep
                    g.DrawLine(gridPen, x, 48, x + (H - 96) \ 2, H - 48)
                    g.DrawLine(gridPen, x, 48, x - (H - 96) \ 2, H - 48)
                Next
            End Using

            ' Outer crimson/amber chamfered Klingon frame
            DrawKlingonChamferFrame(g, New Rectangle(8, 6, W - 16, H - 12), Color.FromArgb(255, 204, 17, 0), Color.FromArgb(255, 255, 165, 0), 18)
            DrawKlingonChamferFrame(g, New Rectangle(154, 52, W - 168, H - 100), Color.FromArgb(255, 180, 10, 0), Color.FromArgb(255, 255, 140, 0), 22)

            ' Large glowing Klingon Trefoil Emblem in center
            DrawKlingonTrefoil(g, New PointF(W * 0.54F, H * 0.46F), Math.Min(W, H) * 0.22F,
                               Color.FromArgb(240, 230, 25, 0), Color.FromArgb(255, 255, 165, 0))
        End Sub

        Private Sub DrawTakeoverBanner(ByVal g As Graphics, ByVal W As Integer, ByVal H As Integer,
                                       ByVal isPhase1 As Boolean, ByVal phase2Progress As Single, ByVal flashWhite As Boolean)
            Dim boxW As Integer = Math.Min(760, W - 80)
            Dim boxH As Integer = 170
            Dim bx As Integer = (W - boxW) \ 2
            Dim by As Integer = (H - boxH) \ 2
            Dim rect As New Rectangle(bx, by, boxW, boxH)

            Using bg As New SolidBrush(Color.FromArgb(230, 12, 0, 0))
                g.FillRectangle(bg, rect)
            End Using

            Dim borderCol As Color = If(isPhase1,
                                        If(flashWhite, Color.White, Color.Red),
                                        Color.FromArgb(255, 255, 153, 0))
            DrawKlingonChamferFrame(g, rect, Color.FromArgb(255, 210, 20, 0), borderCol, 16)

            Dim titleFont As Font = Nothing
            Dim subFont As Font = Nothing
            Try
                titleFont = New Font("LCARS", 24.0F, FontStyle.Bold, GraphicsUnit.Point)
                subFont = New Font("LCARS", 15.0F, FontStyle.Regular, GraphicsUnit.Point)
            Catch
                titleFont = New Font(FontFamily.GenericSansSerif, 18.0F, FontStyle.Bold)
                subFont = New Font(FontFamily.GenericSansSerif, 12.0F, FontStyle.Bold)
            End Try

            Using titleFont
                Using subFont
                    Dim fmt As New StringFormat()
                    fmt.Alignment = StringAlignment.Center
                    fmt.LineAlignment = StringAlignment.Center

                    If isPhase1 Then
                        Using bTitle As New SolidBrush(If(flashWhite, Color.White, Color.FromArgb(255, 255, 50, 30))),
                              bSub As New SolidBrush(Color.FromArgb(255, 255, 180, 0))
                            g.DrawString("▲  RED ALERT // INTRUSION DETECTED  ▲", titleFont, bTitle,
                                         New RectangleF(bx + 12, by + 18, boxW - 24, 52), fmt)
                            g.DrawString("SUBSPACE ANOMALY // UNAUTHORIZED CARRIER WAVE INJECTED" & vbCrLf &
                                         "ATTEMPTING LCARS SECURITY CONTAINMENT...", subFont, bSub,
                                         New RectangleF(bx + 12, by + 76, boxW - 24, 72), fmt)
                        End Using
                    Else
                        Dim pct As Integer = CInt(Math.Round(phase2Progress * 100))
                        Using bTitle As New SolidBrush(Color.FromArgb(255, 255, 175, 0)),
                              bSub As New SolidBrush(Color.FromArgb(255, 235, 45, 20)),
                              fPiqad As Font = CreatePiqadFont(22.0F, FontStyle.Regular)
                            g.DrawString(ToPiqad("tlhIngan wo' De'wI'"), fPiqad, bTitle,
                                         New RectangleF(bx + 12, by + 16, boxW - 24, 48), fmt)
                            g.DrawString(ToPiqad("SYSTEM OVERRIDE // LCARS CORE ASSIMILATED [" & pct.ToString() & "%]") & vbCrLf &
                                         ToPiqad("Qapla' // INITIALIZING KLINGON DEFENSE INTERFACE..."), fPiqad, bSub,
                                         New RectangleF(bx + 12, by + 68, boxW - 24, 56), fmt)
                        End Using

                        ' Progress bar
                        Dim barRect As New Rectangle(bx + 40, by + boxH - 32, boxW - 80, 14)
                        Using penBar As New Pen(Color.FromArgb(255, 204, 17, 0), 1.5F)
                            g.DrawRectangle(penBar, barRect)
                        End Using
                        Dim fillW As Integer = CInt((barRect.Width - 4) * phase2Progress)
                        If fillW > 0 Then
                            Using bFill As New SolidBrush(Color.FromArgb(255, 255, 153, 0))
                                g.FillRectangle(bFill, barRect.X + 2, barRect.Y + 2, fillW, barRect.Height - 4)
                            End Using
                        End If
                    End If
                End Using
            End Using
        End Sub
    End Class

#End Region

#Region " Custom Klingon Tactical Painting (Frames, Trefoil Emblem, Disruptor & Dual-Language Buttons) "

    Private Sub OnMainBarPaint(ByVal sender As Object, ByVal e As PaintEventArgs)
        Try
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim W As Integer = pnlMainBar.ClientSize.Width
        Dim H As Integer = pnlMainBar.ClientSize.Height
        If W <= 0 OrElse H <= 0 Then Return

        Dim crimson As Color = Color.FromArgb(255, 204, 17, 0)
        Dim darkCrimson As Color = Color.FromArgb(255, 110, 8, 0)
        Dim amber As Color = Color.FromArgb(255, 255, 165, 0)

        ' 1. Top Imperial Command Header Bar
        Dim topHeaderPoly As Point() = {
            New Point(4, 4),
            New Point(W - 4, 4),
            New Point(W - 4, 38),
            New Point(W - 14, 46),
            New Point(152, 46),
            New Point(142, 52),
            New Point(4, 52)
        }
        Using bgBrush As New SolidBrush(Color.FromArgb(255, 22, 2, 2))
            g.FillPolygon(bgBrush, topHeaderPoly)
        End Using
        Using borderPen As New Pen(crimson, 2.2F)
            g.DrawPolygon(borderPen, topHeaderPoly)
        End Using

        ' Klingon Trefoil + Title in top-left header block [0..204]
        DrawKlingonTrefoil(g, New PointF(28.0F, 26.0F), 17.0F, crimson, amber)
        DrawHeaderBannerText(g, New RectangleF(50, 4, 158, 42))

            ' 2. Viewport Chamfered Tactical Surround (around pnlMain)
            '    plus Klingon defense force header bar & disruptor flanking panels
            If pnlMain IsNot Nothing AndAlso pnlMain.Width > 10 AndAlso pnlMain.Height > 10 Then
                Dim vpFrame As New Rectangle(pnlMain.Left - 3, pnlMain.Top - 3, pnlMain.Width + 6, pnlMain.Height + 6)
                Using vpPen As New Pen(crimson, 2.5F), innerAmberPen As New Pen(amber, 1.0F)
                    g.DrawRectangle(vpPen, vpFrame)
                    Dim cSize As Integer = 16
                    ' Corner tactical chevrons on the viewport frame
                    DrawCornerChevron(g, vpFrame.Left, vpFrame.Top, cSize, 1, 1, amber)
                    DrawCornerChevron(g, vpFrame.Right, vpFrame.Top, cSize, -1, 1, amber)
                    DrawCornerChevron(g, vpFrame.Left, vpFrame.Bottom, cSize, 1, -1, amber)
                    DrawCornerChevron(g, vpFrame.Right, vpFrame.Bottom, cSize, -1, -1, amber)
                End Using
            End If

            ' 3. Left Tactical Spine Frame & Disruptor Silhouette Accent
            Dim leftSpineRect As New Rectangle(4, 48, 144, H - 94)
            DrawKlingonChamferFrame(g, leftSpineRect, crimson, amber, 10)
            If lastLeftButtonsBottom + 24 < H - 46 Then
                DrawKlingonDisruptorSilhouette(g, New Rectangle(12, lastLeftButtonsBottom + 2, 128, Math.Min(34, (H - 48) - lastLeftButtonsBottom)), crimson, amber)
            End If

            ' 4. Bottom Tactical Bar Frame & Hazard Triangles
            Dim botRect As New Rectangle(4, H - 42, W - 8, 38)
            DrawKlingonChamferFrame(g, botRect, crimson, darkCrimson, 8)

        ' Draw Klingon disruptor / triangular tactical cluster accents along top & bottom
        Dim triStartX As Integer = Math.Max(620, W - 380)
        Dim triEndX As Integer = W - 230
        Dim triIdx As Integer = 0
        Dim tx As Integer = triStartX
        While tx + 16 < triEndX
            Dim isUp As Boolean = (triIdx Mod 2 = 0)
            Dim pts As Point()
            If isUp Then
                pts = {New Point(tx + 7, 12), New Point(tx + 14, 28), New Point(tx, 28)}
            Else
                pts = {New Point(tx, 12), New Point(tx + 14, 12), New Point(tx + 7, 28)}
            End If
            Using tb As New SolidBrush(If(isUp, amber, crimson))
                g.FillPolygon(tb, pts)
            End Using
            triIdx += 1
            tx += 16
        End While
        Catch ex As Exception
            LogCrash("OnMainBarPaint", ex)
        End Try
    End Sub

    Private Sub DrawHeaderBannerText(ByVal g As Graphics, ByVal rect As RectangleF)
        Try
        Dim showTerran As Boolean = modSettings.KlingonTerranTranslations
        Dim fmt As New StringFormat()
        fmt.Alignment = StringAlignment.Near
        fmt.LineAlignment = StringAlignment.Center
        fmt.Trimming = StringTrimming.EllipsisCharacter
        fmt.FormatFlags = StringFormatFlags.NoWrap
        g.TextRenderingHint = Drawing.Text.TextRenderingHint.AntiAlias
        Using bAmber As New SolidBrush(Color.FromArgb(255, 255, 175, 0))
            If showTerran Then
                Using fTerran As Font = CreateTerranFont(8.0F, FontStyle.Bold)
                    g.DrawString(TerranWithPhonetics("KLINGON EMPIRE", "tlhIngan wo'"), fTerran, bAmber, rect, fmt)
                End Using
            Else
                Using fPiqad As Font = CreatePiqadFont(14.0F, FontStyle.Regular)
                    g.DrawString(ToPiqad("tlhIngan wo' ra'wI'"), fPiqad, bAmber, rect, fmt)
                End Using
            End If
        End Using
        Catch ex As Exception
            LogCrash("DrawHeaderBannerText", ex)
        End Try
    End Sub

    Private Sub OnStartPanelPaint(ByVal sender As Object, ByVal e As PaintEventArgs)
        Try
            Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim W As Integer = pnlStart.ClientSize.Width
        Dim H As Integer = pnlStart.ClientSize.Height
        If W <= 0 OrElse H <= 0 Then Return

        Dim crimson As Color = Color.FromArgb(255, 204, 17, 0)
        Dim amber As Color = Color.FromArgb(255, 255, 165, 0)
        DrawKlingonChamferFrame(g, New Rectangle(1, 1, W - 2, H - 2), crimson, amber, 12)

        ' Header strip inside Start Menu
        Dim showTerran As Boolean = modSettings.KlingonTerranTranslations
        Dim hdrRect As New RectangleF(140, 4, W - 144, 24)
        Dim fmt As New StringFormat() With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center}
        g.TextRenderingHint = Drawing.Text.TextRenderingHint.AntiAlias
        Using b As New SolidBrush(amber)
            If showTerran Then
                Using f As Font = CreateTerranFont(9.0F, FontStyle.Bold)
                    g.DrawString(TerranWithPhonetics("IMPERIAL PROGRAM MATRIX", "ghunmeH tetlh"), f, b, hdrRect, fmt)
                End Using
            Else
                Using f As Font = CreatePiqadFont(12.0F, FontStyle.Regular)
                    g.DrawString(ToPiqad("ghunmeH tetlh"), f, b, hdrRect, fmt)
                End Using
            End If
        End Using
        Catch ex As Exception
            LogCrash("OnStartPanelPaint", ex)
        End Try
    End Sub

    Private Sub OnBattPanelPaint(ByVal sender As Object, ByVal e As PaintEventArgs)
        Try
            Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim W As Integer = pnlBatt.ClientSize.Width
        Dim H As Integer = pnlBatt.ClientSize.Height
        If W <= 0 OrElse H <= 0 Then Return
        DrawKlingonChamferFrame(g, New Rectangle(1, 1, W - 2, H - 2),
                                Color.FromArgb(255, 180, 15, 0),
                                Color.FromArgb(255, 255, 153, 0), 8)
        Catch ex As Exception
            LogCrash("OnBattPanelPaint", ex)
        End Try
    End Sub

    ''' <summary>
    ''' Custom owner-draw overlay for every LCARS button on frmMainscreen5:
    ''' Paints a sharp 45-degree chamfered Klingon tactical plate with triangular accents,
    ''' pIqaD Klingonese, and when `KlingonTerranTranslations` is ON, English plus
    ''' Klingon phonetics in parentheses.
    ''' </summary>
    Private Sub OnKlingonButtonPaint(ByVal sender As Object, ByVal e As PaintEventArgs)
        Try
            Dim btn As LCARS.LCARSbuttonClass = TryCast(sender, LCARS.LCARSbuttonClass)
        If btn Is Nothing OrElse btn.Width <= 2 OrElse btn.Height <= 2 Then Return

        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.TextRenderingHint = Drawing.Text.TextRenderingHint.AntiAlias

        Dim w As Integer = btn.Width
        Dim h As Integer = btn.Height
        g.Clear(Color.Black)

        ' Determine Klingon & Terran strings
        Dim klingonCaption As String = If(btn.ButtonText, btn.Text)
        Dim terranCaption As String = ""
        Dim isCritical As Boolean = (btn.Color = LCARS.LCARScolorStyles.CriticalFunction OrElse
                                     btn.Color = LCARS.LCARScolorStyles.FunctionOffline)

        Dim lex As KlingonLexEntry = Nothing
        If Lexicon.TryGetValue(btn.Name, lex) Then
            klingonCaption = lex.KlingonText
            terranCaption = lex.TerranText
            isCritical = lex.IsCritical
        ElseIf btn.Name.StartsWith("btnQuickControls", StringComparison.OrdinalIgnoreCase) Then
            klingonCaption = "nom"
            terranCaption = "QUICK"
        ElseIf String.Equals(btn.Name, "myClock", StringComparison.OrdinalIgnoreCase) Then
            klingonCaption = If(btn.ButtonText, "")
            terranCaption = "ovjaj // STARDATE"
            isCritical = True
        ElseIf String.Equals(btn.Name, "fbProgramPages", StringComparison.OrdinalIgnoreCase) Then
            klingonCaption = If(btn.ButtonText, "")
            terranCaption = "PAGES"
        End If

        Dim fillCol As Color
        Dim borderCol As Color
        Dim primaryTextCol As Color
        Dim subTextCol As Color

        If btn.RedAlert = LCARS.LCARSalert.White Then
            fillCol = Color.FromArgb(255, 220, 220, 220)
            borderCol = Color.White
            primaryTextCol = Color.Black
            subTextCol = Color.FromArgb(255, 60, 0, 0)
        ElseIf isCritical Then
            fillCol = Color.FromArgb(255, 135, 12, 8)
            borderCol = Color.FromArgb(255, 255, 50, 20)
            primaryTextCol = Color.FromArgb(255, 255, 215, 120)
            subTextCol = Color.FromArgb(255, 255, 165, 60)
        Else
            fillCol = Color.FromArgb(255, 48, 8, 4)
            borderCol = Color.FromArgb(255, 210, 30, 0)
            primaryTextCol = Color.FromArgb(255, 255, 175, 0)
            subTextCol = Color.FromArgb(255, 230, 90, 50)
        End If

        If Not btn.Lit Then
            fillCol = Color.FromArgb(255, fillCol.R \ 2, fillCol.G \ 2, fillCol.B \ 2)
        End If

        ' Chamfered polygon button shape
        Dim cut As Integer = Math.Max(4, Math.Min(8, h \ 4))
        Dim pts As Point() = {
            New Point(cut, 1),
            New Point(w - 2, 1),
            New Point(w - 2, h - cut - 1),
            New Point(w - cut - 2, h - 2),
            New Point(1, h - 2),
            New Point(1, cut)
        }
        Using bFill As New SolidBrush(fillCol), pBorder As New Pen(borderCol, 1.6F)
            g.FillPolygon(bFill, pts)
            g.DrawPolygon(pBorder, pts)
        End Using

        ' Left tactical triangle marker (or ArrowButton directional glyph)
        Dim arrowBtn As LCARS.Controls.ArrowButton = TryCast(btn, LCARS.Controls.ArrowButton)
        If arrowBtn IsNot Nothing Then
            DrawKlingonArrowGlyph(g, w, h, arrowBtn.ArrowDirection, primaryTextCol)
            Return
        End If

        Dim triSize As Integer = Math.Max(4, Math.Min(7, h \ 5))
        Dim markerPts As Point() = {
            New Point(5, h \ 2 - triSize),
            New Point(5 + triSize + 2, h \ 2),
            New Point(5, h \ 2 + triSize)
        }
        Using bMarker As New SolidBrush(borderCol)
            g.FillPolygon(bMarker, markerPts)
        End Using

        Dim showTerran As Boolean = modSettings.KlingonTerranTranslations AndAlso Not String.IsNullOrEmpty(terranCaption)
        Dim textRect As New RectangleF(14, 1, Math.Max(10, w - 18), h - 2)
        Dim fmt As New StringFormat()
        fmt.Alignment = StringAlignment.Center
        fmt.LineAlignment = StringAlignment.Center
        fmt.Trimming = StringTrimming.EllipsisCharacter
        fmt.FormatFlags = StringFormatFlags.NoWrap

        Using bCaption As New SolidBrush(primaryTextCol)
            If showTerran Then
                Dim caption As String = TerranWithPhonetics(terranCaption, klingonCaption)
                If String.Equals(btn.Name, "myClock", StringComparison.OrdinalIgnoreCase) Then
                    caption = TerranWithPhonetics(If(btn.ButtonText, ""), "ovjaj")
                ElseIf String.Equals(btn.Name, "fbProgramPages", StringComparison.OrdinalIgnoreCase) Then
                    caption = TerranWithPhonetics("PAGES", If(btn.ButtonText, ""))
                End If
                Using fTerran As Font = CreateTerranFont(Math.Max(7.0F, Math.Min(10.0F, h * 0.30F)), FontStyle.Bold)
                    g.DrawString(caption, fTerran, bCaption, textRect, fmt)
                End Using
            ElseIf String.Equals(btn.Name, "myClock", StringComparison.OrdinalIgnoreCase) OrElse
                   String.Equals(btn.Name, "fbProgramPages", StringComparison.OrdinalIgnoreCase) Then
                Using fLatin As Font = CreateTerranFont(Math.Max(8.0F, Math.Min(12.0F, h * 0.34F)), FontStyle.Bold)
                    g.DrawString(If(btn.ButtonText, klingonCaption), fLatin, bCaption, textRect, fmt)
                End Using
            Else
                Using fPiqad As Font = CreatePiqadFont(Math.Max(9.0F, Math.Min(14.0F, h * 0.38F)), FontStyle.Regular)
                    g.DrawString(ToPiqad(klingonCaption), fPiqad, bCaption, textRect, fmt)
                End Using
            End If
        End Using
        Catch ex As Exception
            LogCrash("OnKlingonButtonPaint", ex)
        End Try
    End Sub

    Private Shared Sub DrawKlingonArrowGlyph(ByVal g As Graphics, ByVal w As Integer, ByVal h As Integer,
                                             ByVal dir As LCARS.LCARSarrowDirection, ByVal col As Color)
        Dim cx As Integer = w \ 2
        Dim cy As Integer = h \ 2
        Dim r As Integer = Math.Max(4, Math.Min(w, h) \ 3)
        Dim pts As Point()
        Select Case dir
            Case LCARS.LCARSarrowDirection.Up
                pts = {New Point(cx, cy - r), New Point(cx + r, cy + r), New Point(cx - r, cy + r)}
            Case LCARS.LCARSarrowDirection.Down
                pts = {New Point(cx - r, cy - r), New Point(cx + r, cy - r), New Point(cx, cy + r)}
            Case LCARS.LCARSarrowDirection.Left
                pts = {New Point(cx - r, cy), New Point(cx + r, cy - r), New Point(cx + r, cy + r)}
            Case Else
                pts = {New Point(cx + r, cy), New Point(cx - r, cy - r), New Point(cx - r, cy + r)}
        End Select
        Using b As New SolidBrush(col)
            g.FillPolygon(b, pts)
        End Using
    End Sub

    Private Shared Sub DrawKlingonChamferFrame(ByVal g As Graphics, ByVal r As Rectangle,
                                               ByVal outerCol As Color, ByVal innerCol As Color, ByVal chamfer As Integer)
        If r.Width <= chamfer * 2 OrElse r.Height <= chamfer * 2 Then Return
        Dim c As Integer = Math.Max(4, chamfer)
        Dim pts As Point() = {
            New Point(r.Left + c, r.Top),
            New Point(r.Right - c, r.Top),
            New Point(r.Right, r.Top + c),
            New Point(r.Right, r.Bottom - c),
            New Point(r.Right - c, r.Bottom),
            New Point(r.Left + c, r.Bottom),
            New Point(r.Left, r.Bottom - c),
            New Point(r.Left, r.Top + c)
        }
        Using pOuter As New Pen(outerCol, 2.0F)
            g.DrawPolygon(pOuter, pts)
        End Using
        Dim ir As New Rectangle(r.Left + 3, r.Top + 3, r.Width - 6, r.Height - 6)
        If ir.Width > c * 2 AndAlso ir.Height > c * 2 Then
            Dim ic As Integer = Math.Max(2, c - 2)
            Dim ipts As Point() = {
                New Point(ir.Left + ic, ir.Top),
                New Point(ir.Right - ic, ir.Top),
                New Point(ir.Right, ir.Top + ic),
                New Point(ir.Right, ir.Bottom - ic),
                New Point(ir.Right - ic, ir.Bottom),
                New Point(ir.Left + ic, ir.Bottom),
                New Point(ir.Left, ir.Bottom - ic),
                New Point(ir.Left, ir.Top + ic)
            }
            Using pInner As New Pen(innerCol, 1.0F)
                g.DrawPolygon(pInner, ipts)
            End Using
        End If
    End Sub

    Private Shared Sub DrawCornerChevron(ByVal g As Graphics, ByVal x As Integer, ByVal y As Integer,
                                         ByVal size As Integer, ByVal dx As Integer, ByVal dy As Integer, ByVal col As Color)
        Using p As New Pen(col, 2.5F)
            g.DrawLine(p, x, y, x + dx * size, y)
            g.DrawLine(p, x, y, x, y + dy * size)
        End Using
    End Sub

    Private Shared Sub DrawKlingonDisruptorSilhouette(ByVal g As Graphics, ByVal bounds As Rectangle,
                                                      ByVal crimson As Color, ByVal amber As Color)
        If bounds.Width < 40 OrElse bounds.Height < 12 Then Return
        Dim midY As Integer = bounds.Top + bounds.Height \ 2
        Using pCrimson As New Pen(crimson, 2.0F), pAmber As New Pen(amber, 1.2F), bCrimson As New SolidBrush(crimson), bAmber As New SolidBrush(amber)
            ' Barrel & emitter tip
            g.DrawLine(pAmber, bounds.Left + 4, midY - 2, bounds.Right - 28, midY - 2)
            g.FillRectangle(bCrimson, bounds.Left + 14, midY - 5, bounds.Width - 48, 6)
            ' Cooling fins along barrel
            For fx As Integer = bounds.Left + 18 To bounds.Right - 42 Step 8
                g.DrawLine(pAmber, fx, midY - 7, fx, midY + 3)
            Next
            ' Angled grip stock
            Dim stockPts As Point() = {
                New Point(bounds.Right - 34, midY - 4),
                New Point(bounds.Right - 6, midY - 2),
                New Point(bounds.Right - 2, midY + 8),
                New Point(bounds.Right - 24, midY + 6)
            }
            g.FillPolygon(bAmber, stockPts)
            g.DrawPolygon(pCrimson, stockPts)
        End Using
    End Sub

    Public Shared Sub DrawKlingonTrefoil(ByVal g As Graphics, ByVal center As PointF, ByVal radius As Single,
                                         ByVal primaryCol As Color, ByVal ringCol As Color)
        ' Symmetrical Klingon Empire trefoil: central spike + two mirror-image curved blades + circular ring
        Dim cx As Single = center.X
        Dim cy As Single = center.Y

        ' Circular ring (perfectly symmetrical)
        Using ringPen As New Pen(ringCol, Math.Max(1.5F, radius * 0.11F))
            g.DrawEllipse(ringPen, cx - radius, cy - radius, radius * 2.0F, radius * 2.0F)
        End Using

        ' Central upward spike (isosceles triangle, symmetrical about cx)
        Dim spike As PointF() = {
            New PointF(cx, cy - radius * 1.30F),
            New PointF(cx + radius * 0.22F, cy + radius * 0.28F),
            New PointF(cx, cy + radius * 0.42F),
            New PointF(cx - radius * 0.22F, cy + radius * 0.28F)
        }

        ' Left blade (curved downward and outward)
        Dim leftBlade As PointF() = {
            New PointF(cx - radius * 0.06F, cy + radius * 0.10F),
            New PointF(cx - radius * 0.92F, cy + radius * 0.72F),
            New PointF(cx - radius * 0.34F, cy + radius * 0.44F),
            New PointF(cx - radius * 0.02F, cy + radius * 0.32F)
        }

        ' Right blade (mirror image of left blade)
        Dim rightBlade As PointF() = {
            New PointF(cx + radius * 0.06F, cy + radius * 0.10F),
            New PointF(cx + radius * 0.92F, cy + radius * 0.72F),
            New PointF(cx + radius * 0.34F, cy + radius * 0.44F),
            New PointF(cx + radius * 0.02F, cy + radius * 0.32F)
        }

        Using bFill As New SolidBrush(primaryCol), pOutline As New Pen(ringCol, 1.0F)
            g.FillPolygon(bFill, spike)
            g.FillPolygon(bFill, leftBlade)
            g.FillPolygon(bFill, rightBlade)
            g.DrawPolygon(pOutline, spike)
            g.DrawPolygon(pOutline, leftBlade)
            g.DrawPolygon(pOutline, rightBlade)
        End Using
    End Sub

#End Region

#Region " Interactive Handlers (Start Menu, Personal Programs, Launchers) "

    Private Sub myStartMenu_Click(ByVal sender As Object, ByVal e As EventArgs) Handles myStartMenu.Click
        pnlStart.Visible = Not pnlStart.Visible
        EnsureKlingonLayout()
        If pnlStart.Visible AndAlso myBusiness IsNot Nothing Then
            myBusiness.RefreshStartMenuPrograms()
        End If
        If myBusiness IsNot Nothing AndAlso myBusiness.isInit Then
            myBusiness.UpdateRegion()
        End If
    End Sub

    Private Sub startMenuItem_Click(ByVal sender As Object, ByVal e As EventArgs) _
        Handles myDocuments.Click, myPictures.Click, myMusic.Click, myVideos.Click,
                fbDesktop.Click, fbMyNetwork.Click, myRun.Click
        If pnlStart.Visible Then
            myStartMenu.doClick(sender, e)
        End If
    End Sub

    Private Sub myUserButtons_Click(ByVal sender As Object, ByVal e As EventArgs) Handles myUserButtons.Click
        If gridUserButtons Is Nothing OrElse myBusiness Is Nothing Then Return
        Dim opening As Boolean = Not gridUserButtons.Visible
        myBusiness.TogglePersonalPrograms(opening, gridUserButtons.Width)
    End Sub

    Private Sub OnClosedCleanup(ByVal sender As Object, ByVal e As EventArgs)
        If takeoverTimer IsNot Nothing Then
            takeoverTimer.Stop()
            takeoverTimer.Dispose()
            takeoverTimer = Nothing
        End If
        RestoreKlingonDesktopWallpaper()
    End Sub

    ''' <summary>
    ''' Puts the Klingon Empire trefoil on the desktop hole for this screen only.
    ''' Saved wallpaper settings are left alone so Modes 1–4 can restore them.
    ''' </summary>
    Private Sub ApplyKlingonDesktopWallpaper()
        If myBusiness Is Nothing OrElse My.Application.IsSettingsMode Then Return
        Dim screenIndex As Integer = myBusiness.ScreenIndex
        Try
            Dim bmp As Image = CreateKlingonEmpireWallpaper(1920, 1080)
            SetWallpaper(bmp, screenIndex)
            setWallpaperSizeMode(ImageLayout.Zoom, screenIndex)
            Dim previous As Image = Nothing
            If KlingonDesktopImages.TryGetValue(screenIndex, previous) Then
                If previous IsNot Nothing AndAlso Not Object.ReferenceEquals(previous, bmp) Then
                    previous.Dispose()
                End If
            End If
            KlingonDesktopImages(screenIndex) = bmp
            KlingonDesktopScreens.Add(screenIndex)
        Catch
        End Try
    End Sub

    ''' <summary>
    ''' Restores the user's Federation or custom wallpaper after leaving Mode 5.
    ''' </summary>
    Friend Sub RestoreDesktopForShutdown()
        RestoreKlingonDesktopWallpaper()
    End Sub

    Private Sub RestoreKlingonDesktopWallpaper()
        If myBusiness Is Nothing Then Return
        Dim screenIndex As Integer = myBusiness.ScreenIndex
        If Not KlingonDesktopScreens.Contains(screenIndex) Then Return
        Dim generated As Image = Nothing
        KlingonDesktopImages.TryGetValue(screenIndex, generated)
        Try
            ApplySavedWallpaper(screenIndex)
            KlingonDesktopScreens.Remove(screenIndex)
            KlingonDesktopImages.Remove(screenIndex)
            If generated IsNot Nothing Then
                generated.Dispose()
            End If
        Catch
        End Try
    End Sub

    ''' <summary>
    ''' Builds a black-field Klingon Empire wallpaper: tactical grid plus centered trefoil.
    ''' </summary>
    Public Shared Function CreateKlingonEmpireWallpaper(ByVal width As Integer, ByVal height As Integer) As Image
        Dim w As Integer = Math.Max(320, width)
        Dim h As Integer = Math.Max(240, height)
        Dim bmp As New Bitmap(w, h)
        Using g As Graphics = Graphics.FromImage(bmp)
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.Clear(Color.Black)

            Using glow As New GraphicsPath()
                Dim glowRect As New Rectangle(w \ 2 - w \ 5, h \ 2 - h \ 5, w \ 2, h \ 2)
                glow.AddEllipse(glowRect)
                Using pgb As New PathGradientBrush(glow)
                    pgb.CenterColor = Color.FromArgb(90, 140, 12, 0)
                    pgb.SurroundColors = {Color.FromArgb(0, 0, 0, 0)}
                    g.FillPath(pgb, glow)
                End Using
            End Using

            Dim gridStep As Integer = Math.Max(36, Math.Min(w, h) \ 18)
            Using gridPen As New Pen(Color.FromArgb(40, 190, 15, 0), 1.0F)
                For y As Integer = 0 To h Step gridStep
                    g.DrawLine(gridPen, 0, y, w, y)
                Next
                For x As Integer = 0 To w Step gridStep
                    g.DrawLine(gridPen, x, 0, x, h)
                Next
            End Using

            Dim crimson As Color = Color.FromArgb(255, 204, 17, 0)
            Dim amber As Color = Color.FromArgb(255, 255, 165, 0)
            Dim radius As Single = Math.Min(w, h) * 0.28F
            DrawKlingonTrefoil(g, New PointF(w / 2.0F, h / 2.0F), radius, crimson, amber)

            Using ringPen As New Pen(Color.FromArgb(160, 255, 165, 0), Math.Max(2.0F, radius * 0.035F))
                Dim ringPad As Single = radius * 1.55F
                g.DrawEllipse(ringPen, (w / 2.0F) - ringPad, (h / 2.0F) - ringPad, ringPad * 2.0F, ringPad * 2.0F)
            End Using
        End Using
        Return bmp
    End Function

#End Region

End Class
