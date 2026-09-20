Public Class frmMainscreen1
    Implements IAutohide

    Private myBusiness As modBusiness
    Private startMenuTogglePending As Boolean = False
    Private personalProgramsTogglePending As Boolean = False
    Private speechRowFillBusy As Boolean = False
    Private chromeLayoutBusy As Boolean = False

    Public Function getAutohideEdges() As IAutohide.AutohideEdges Implements IAutohide.getAutohideEdges
        Return IAutohide.AutohideEdges.Top Or IAutohide.AutohideEdges.Left
    End Function

    Public Sub New(ByVal b As modBusiness)
        InitializeComponent()
        Me.myBusiness = b
        AddHandler Me.Shown, AddressOf OnView1Shown
        AddHandler pnlMainBar.Resize, AddressOf OnView1MainBarResize
    End Sub

    Private Sub OnView1Shown(ByVal sender As Object, ByVal e As EventArgs)
        ' Init can snap panels back to designer 800x600 sizes; repair after layout settles.
        BeginInvoke(New MethodInvoker(Sub()
                                          EnsureView1ChromeLayout()
                                          EnsureSpeechRowFill()
                                          modHudWeather.SyncWeatherLayoutPublic(myBusiness)
                                          modQuickControls.ScheduleSyncQuickButton(myBusiness)
                                          If myBusiness IsNot Nothing AndAlso myBusiness.isInit Then
                                              myBusiness.UpdateRegion()
                                          End If
                                      End Sub))
    End Sub

    Private Sub OnView1MainBarResize(ByVal sender As Object, ByVal e As EventArgs)
        If chromeLayoutBusy OrElse speechRowFillBusy Then Return
        EnsureSpeechRowFill()
    End Sub

    ''' <summary>
    ''' Keeps container / main bar / desktop hole sized to the form. Init occasionally
    ''' restores designer 800×600 child sizes after a correct fullscreen layout.
    ''' Also repairs designer bar Height (474) so expanded mode is not a short hole.
    ''' </summary>
    Public Sub EnsureView1ChromeLayout()
        If chromeLayoutBusy Then Return
        If Me.ClientSize.Width <= 0 OrElse Me.ClientSize.Height <= 0 Then Return
        If pnlMainContainer Is Nothing OrElse pnlMainBar Is Nothing OrElse pnlMain Is Nothing Then Return

        chromeLayoutBusy = True
        Try
            Dim startOffset As Integer = 0
            If pnlStart IsNot Nothing AndAlso pnlStart.Visible Then startOffset = pnlStart.Width
            Dim targetLeft As Integer = startOffset
            Dim targetW As Integer = Me.ClientSize.Width - startOffset
            Dim targetH As Integer = Me.ClientSize.Height
            If targetW < 100 OrElse targetH < 100 Then Return

            If pnlMainContainer.Left <> targetLeft OrElse pnlMainContainer.Width <> targetW OrElse pnlMainContainer.Height <> targetH Then
                pnlMainContainer.SetBounds(targetLeft, 0, targetW, targetH)
                modDiagnostics.LogInfo("frmMainscreen1.EnsureView1ChromeLayout",
                    "container->" & pnlMainContainer.Bounds.ToString())
            End If

            Dim barW As Integer = pnlMainContainer.ClientSize.Width
            Dim barH As Integer = Math.Max(100, pnlMainContainer.ClientSize.Height - pnlMainBar.Top)
            If pnlMainBar.Width <> barW OrElse pnlMainBar.Height <> barH Then
                pnlMainBar.SetBounds(pnlMainBar.Left, pnlMainBar.Top, barW, barH)
                modDiagnostics.LogInfo("frmMainscreen1.EnsureView1ChromeLayout",
                    "pnlMainBar->" & pnlMainBar.Bounds.ToString())
            End If

            ' Right-edge speech-row chrome (PERSONAL / end cap) must track full bar width.
            ' Designer Left values stick if Width was assigned without a parent-size anchor pass.
            PinSpeechRowRightEdge()
            PinHeaderRowRightEdge()
            PinAppsRowToTray()

            Dim expectedMainW As Integer = Math.Max(100, pnlMainBar.ClientSize.Width - pnlMain.Left)
            If gridUserButtons IsNot Nothing AndAlso gridUserButtons.Visible Then
                expectedMainW = Math.Max(100, gridUserButtons.Left - pnlMain.Left - 6)
            End If
            Dim expectedMainH As Integer = Math.Max(100, pnlMainBar.ClientSize.Height - pnlMain.Top)
            If Math.Abs(pnlMain.Width - expectedMainW) > 20 OrElse Math.Abs(pnlMain.Height - expectedMainH) > 20 Then
                pnlMain.SetBounds(pnlMain.Left, pnlMain.Top, expectedMainW, expectedMainH)
                modDiagnostics.LogInfo("frmMainscreen1.EnsureView1ChromeLayout",
                    "pnlMain->" & pnlMain.Bounds.ToString())
            End If
        Finally
            chromeLayoutBusy = False
        End Try
    End Sub

    ''' <summary>
    ''' Pins PERSONAL + end-cap to the right of pnlMainBar after fullscreen width repair.
    ''' </summary>
    Private Sub PinSpeechRowRightEdge()
        If HalfPillButton3 Is Nothing OrElse myUserButtons Is Nothing Then Return
        Dim barClientW As Integer = pnlMainBar.ClientSize.Width
        Dim pillLeft As Integer = Math.Max(0, barClientW - HalfPillButton3.Width)
        If HalfPillButton3.Left <> pillLeft Then
            HalfPillButton3.Left = pillLeft
        End If
        Dim personalLeft As Integer = Math.Max(0, pillLeft - myUserButtons.Width)
        If myUserButtons.Left <> personalLeft Then
            myUserButtons.Left = personalLeft
        End If
    End Sub

    ''' <summary>
    ''' Pins the header secondary-row end cap and stretches FlatButton7 to meet it.
    ''' </summary>
    Private Sub PinHeaderRowRightEdge()
        If HalfPillButton2 Is Nothing OrElse FlatButton7 Is Nothing Then Return
        If pnlMainContainer Is Nothing Then Return
        Dim containerW As Integer = pnlMainContainer.ClientSize.Width
        Dim pillLeft As Integer = Math.Max(0, containerW - HalfPillButton2.Width)
        If HalfPillButton2.Left <> pillLeft Then
            HalfPillButton2.Left = pillLeft
        End If
        Dim fillLeft As Integer = FlatButton7.Left
        Dim fillW As Integer = Math.Max(40, pillLeft - fillLeft)
        If FlatButton7.Width <> fillW Then
            FlatButton7.Width = fillW
        End If
    End Sub

    ''' <summary>
    ''' Expanded header only: pin SHOW TRAY to the container's far right, then stretch
    ''' the apps strip so the scroll arrows sit immediately left of it.
    ''' </summary>
    Private Sub PinAppsRowToTray()
        If pnlApps Is Nothing OrElse pnlTray Is Nothing Then Return
        If pnlMainContainer Is Nothing Then Return
        ' Expanded = large myClock visible (main bar slid down). Collapsed leaves this alone.
        If myClock Is Nothing OrElse Not myClock.Visible Then Return

        Dim trayLeft As Integer = Math.Max(0, pnlMainContainer.ClientSize.Width - pnlTray.Width)
        If pnlTray.Left <> trayLeft Then
            pnlTray.Left = trayLeft
        End If

        Dim gap As Integer = 6
        Dim appsW As Integer = Math.Max(50, pnlTray.Left - pnlApps.Left - gap)
        If pnlApps.Width <> appsW Then
            pnlApps.Width = appsW
        End If
    End Sub

    ''' <summary>
    ''' Keeps fbClock spanning from after SPEECH to before PERSONAL/QUICK so the speech-row chrome
    ''' has no black gap.
    ''' </summary>
    Public Sub EnsureSpeechRowFill()
        If speechRowFillBusy Then Return
        If fbClock Is Nothing OrElse fbClock.IsDisposed Then Return
        If mySpeech Is Nothing OrElse myUserButtons Is Nothing Then Return
        If pnlMainBar Is Nothing OrElse pnlMainBar.ClientSize.Width <= 0 Then Return

        speechRowFillBusy = True
        Try
            fbClock.Visible = True
            fbClock.Color = LCARS.LCARScolorStyles.StaticTan
            ' Left-only + explicit width: Left|Right + SetBounds was fighting parent layout
            ' and helping snap the bar back to designer width during init.
            fbClock.Anchor = AnchorStyles.Top Or AnchorStyles.Left

            Dim leftEdge As Integer = mySpeech.Right + 6
            Dim rightEdge As Integer = myUserButtons.Left - 2

            For Each ctrl As Control In pnlMainBar.Controls
                If ctrl Is Nothing OrElse Not ctrl.Visible Then Continue For
                If Not ctrl.Name.StartsWith("btnQuickControls", StringComparison.OrdinalIgnoreCase) Then Continue For
                If ctrl.Left > leftEdge + 40 AndAlso ctrl.Left < myUserButtons.Left Then
                    rightEdge = Math.Min(rightEdge, ctrl.Left - 2)
                End If
            Next

            If rightEdge <= leftEdge + 50 Then
                rightEdge = Math.Max(leftEdge + 80, pnlMainBar.ClientSize.Width - 180)
            End If

            Dim fillWidth As Integer = Math.Max(80, rightEdge - leftEdge)
            If fbClock.Left <> leftEdge OrElse fbClock.Width <> fillWidth OrElse fbClock.Height <> 20 Then
                fbClock.SetBounds(leftEdge, 0, fillWidth, 20)
            End If
        Finally
            speechRowFillBusy = False
        End Try
    End Sub

    Private Sub myStart_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles myStartMenu.Click
        modDiagnostics.LogTouch("StartMenu", "Click event frmMainscreen1 before=" & pnlStart.Visible)
        If startMenuTogglePending Then
            modDiagnostics.LogWarn("StartMenu", "toggle already pending")
            Return
        End If
        startMenuTogglePending = True
        BeginInvoke(New MethodInvoker(AddressOf ApplyStartMenuToggle))
    End Sub

    Private Sub ApplyStartMenuToggle()
        Using diag As DiagnosticScope = modDiagnostics.BeginScope("frmMainscreen1.ApplyStartMenuToggle",
            "before visible=" & pnlStart.Visible &
            " container=" & pnlMainContainer.Bounds.ToString() &
            " pnlMain=" & pnlMain.Bounds.ToString())
            Try
                pnlStart.Visible = Not pnlStart.Visible
                fbBlock.Visible = Not pnlStart.Visible
                elbStart.Visible = pnlStart.Visible
                If pnlStart.Visible Then
                    modDiagnostics.LogTouch("StartMenu", "opening start menu")
                    pnlMainContainer.Left = pnlStart.Width
                    pnlMainContainer.Width = Me.Width - pnlStart.Width
                    myBusiness.RefreshStartMenuPrograms()
                Else
                    modDiagnostics.LogTouch("StartMenu", "closing start menu")
                    pnlMainContainer.Left = 0
                    pnlMainContainer.Width = Me.Width
                End If
                EnsureView1ChromeLayout()
                EnsureSpeechRowFill()
                modDiagnostics.LogInfo("frmMainscreen1.ApplyStartMenuToggle",
                    "after visible=" & pnlStart.Visible &
                    " container=" & pnlMainContainer.Bounds.ToString())
            Catch ex As Exception
                modDiagnostics.LogException("frmMainscreen1.ApplyStartMenuToggle", ex)
                Throw
            Finally
                startMenuTogglePending = False
            End Try
        End Using
    End Sub

    Private Sub ArrowButton1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ArrowButton1.Click
        If Not pnlMainBar.Top = 0 Then
            pnlMainBar.Top = 0
            pnlMainBar.Height = Me.Height
            ArrowButton1.ArrowDirection = LCARS.LCARSarrowDirection.Down
            myBusiness.myClock = fbClock
            myClock.Visible = False
        Else
            pnlMainBar.Top = Elbow2.Bottom + 6
            pnlMainBar.Height = Me.Height - Elbow2.Bottom - 6
            ArrowButton1.ArrowDirection = LCARS.LCARSarrowDirection.Up
            myBusiness.myClock = myClock
            fbClock.ButtonText = "X32"
            myClock.Visible = True
        End If
        ' Keep full-width chrome when Top/Height are assigned (can drop designer Width).
        If pnlMainContainer IsNot Nothing Then
            pnlMainBar.Width = pnlMainContainer.ClientSize.Width
        End If
        EnsureView1ChromeLayout()
        EnsureSpeechRowFill()
        CommonScreen.RefreshClockBinding(myBusiness)
        EnsureSpeechRowFill()
        modHudWeather.SyncWeatherLayoutPublic(myBusiness)
        modQuickControls.ScheduleSyncQuickButton(myBusiness)
        BeginInvoke(New MethodInvoker(Sub()
                                          EnsureView1ChromeLayout()
                                          EnsureSpeechRowFill()
                                          modHudWeather.SyncWeatherLayoutPublic(myBusiness)
                                          If myBusiness IsNot Nothing AndAlso myBusiness.isInit Then
                                              myBusiness.UpdateRegion()
                                          End If
                                      End Sub))
        If myBusiness IsNot Nothing AndAlso myBusiness.isInit Then
            myBusiness.UpdateRegion()
        End If
    End Sub

    Private Sub startMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles myPictures.Click, myMusic.Click, myDocuments.Click, myRun.Click, fbWebBrowser.Click, fbTerminal.Click, myVideos.Click, fbDesktop.Click, fbMyNetwork.Click
        If pnlStart.Visible Then myStartMenu.doClick(sender, e)
    End Sub

    Public Shared ReadOnly Property ScreenImage() As Image
        Get
            Return My.Resources.frmmainscreen1
        End Get
    End Property

    Private Sub myUserButtons_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles myUserButtons.Click
        If gridUserButtons Is Nothing Then
            modDiagnostics.LogWarn("PersonalPrograms", "gridUserButtons is Nothing")
            Return
        End If
        Dim opening As Boolean = Not gridUserButtons.Visible
        modDiagnostics.LogTouch("PersonalPrograms", "Click event frmMainscreen1 opening=" & opening)
        If personalProgramsTogglePending Then
            modDiagnostics.LogWarn("PersonalPrograms", "toggle already pending")
            Return
        End If
        personalProgramsTogglePending = True
        BeginInvoke(New MethodInvoker(Sub()
                                          Try
                                              myBusiness.TogglePersonalPrograms(opening, gridUserButtons.Width)
                                          Finally
                                              personalProgramsTogglePending = False
                                          End Try
                                      End Sub))
    End Sub

End Class
