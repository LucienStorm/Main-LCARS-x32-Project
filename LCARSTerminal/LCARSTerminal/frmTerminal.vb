' LCARSTerminal/frmTerminal.vb
Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Runtime.InteropServices
Imports System.Windows.Forms
Imports LCARS.Controls

''' <summary>
''' LCARS Terminal — thin left border, Settings-style right tabs/controls.
''' </summary>
Public Class frmTerminal
    Inherits LCARS.LCARSForm

    Private Const RailLeft As Integer = 8
    Private Const LeftBorder As Integer = 8
    Private Const LeftElbowArm As Integer = 22
    Private Const RailWidth As Integer = 100
    Private Const TopChrome As Integer = 52
    Private Const BottomChrome As Integer = 30
    Private Const ContentGap As Integer = 8
    Private Const ModeTerminalWidth As Integer = 100
    Private Const ModeRdpWidth As Integer = 100
    Private Const TabHeaderWidth As Integer = 88

    Private Enum TerminalUiMode
        Terminal = 0
        Rdp = 1
    End Enum

    Private ReadOnly tabStrip As New TabControl()
    Private ReadOnly status As New Label()
    Private ReadOnly terminalHost As New Panel()
    Private rdpWorkspace As RdpWorkspace
    Private fbModeTerminal As FlatButton
    Private fbModeRdp As FlatButton
    Private fbNewTab As FlatButton
    Private abNewMenu As ArrowButton
    Private fbCloseTab As FlatButton
    Private fbDisconnect As FlatButton
    Private fbRdpConnect As FlatButton
    Private fbRdpNew As FlatButton
    Private fbRdpDelete As FlatButton
    Private fbRdpOpen As FlatButton
    Private fbRdpSave As FlatButton
    Private fbClose As FlatButton
    Private shellPicker As frmShellPicker
    Private _uiMode As TerminalUiMode = TerminalUiMode.Terminal
    Private ReadOnly chkDrives As New CheckBox()
    Private ReadOnly chkPrinters As New CheckBox()
    Private ReadOnly chkClipboard As New CheckBox()
    Private ReadOnly chkAudio As New CheckBox()
    Private ReadOnly chkSmart As New CheckBox()

    Private lastKind As ShellKind = ShellKind.Cmd
    ''' <summary>
    ''' True only for the separate Terminal process started with /admin (UAC).
    ''' Not the same as "this process happens to be elevated" — that would label every
    ''' normal tab as ADMIN and make a regular prompt unreachable.
    ''' </summary>
    Private ReadOnly isAdminWindow As Boolean
    ''' <summary>Optional shell requested via /admin cmd|powershell for a freshly elevated instance.</summary>
    Private startupAdminKind As Nullable(Of ShellKind) = Nothing

    <DllImport("user32.dll")> _
    Private Shared Function ReleaseCapture() As Boolean
    End Function

    <DllImport("user32.dll")> _
    Private Shared Function SendMessage(ByVal hWnd As IntPtr, ByVal msg As Integer, ByVal wParam As Integer, ByVal lParam As Integer) As Integer
    End Function

    <DllImport("user32.dll", SetLastError:=True)> _
    Private Shared Function ChangeWindowMessageFilterEx(ByVal hWnd As IntPtr, ByVal message As Integer, ByVal action As Integer, ByVal pChangeFilterStruct As IntPtr) As Boolean
    End Function

    Private Const WM_NCLBUTTONDOWN As Integer = &HA1
    Private Const HT_CAPTION As Integer = 2
    Private Const MSGFLT_ALLOW As Integer = 1

    ''' <summary>OSK posts keys here so input does not depend on tablet focus/SendKeys.</summary>
    Private Shared ReadOnly WM_LCARS_OSK_INPUT As Integer = NativeRegisterWindowMessage("LCARS_OSK_INPUT")
    Private Declare Function NativeRegisterWindowMessage Lib "user32.dll" Alias "RegisterWindowMessageA" (ByVal lpString As String) As Integer
    ' wParam 1 = unicode char in lParam; wParam 2 = Keys virtual-key in lParam
    Private Const OskInputChar As Integer = 1
    Private Const OskInputVk As Integer = 2

    Public Sub New()
        InstallCrashLogging()
        ParseStartupArgs()
        isAdminWindow = startupAdminKind.HasValue
        Text = If(isAdminWindow, "LCARS TERMINAL · ADMIN", "LCARS TERMINAL")
        BackColor = Color.Black
        FormBorderStyle = FormBorderStyle.None
        ControlBox = False
        Size = New Size(960, 640)
        StartPosition = FormStartPosition.CenterScreen
        DoubleBuffered = True
        BindToWorkingArea = True
        ' Maximize after handle creation (Shown) so linked-window registration sticks.

        LoadLastShell()
        If isAdminWindow AndAlso startupAdminKind.HasValue Then
            lastKind = startupAdminKind.Value
        End If
        BuildChrome()
        BuildContent()
        LayoutChrome()
        If isAdminWindow AndAlso fbModeTerminal IsNot Nothing Then
            fbModeTerminal.ButtonText = "ADMIN"
            fbModeTerminal.Text = "ADMIN"
        End If

        AddHandler Me.Resize, AddressOf OnTerminalResize
        AddHandler Me.FormClosing, AddressOf OnTerminalClosing
        AddHandler Me.Shown, AddressOf OnTerminalShown
        AddHandler Me.HandleCreated, AddressOf OnTerminalHandleCreated
        AddHandler Me.Activated, AddressOf OnTerminalActivated
        AddHandler Me.KeyDown, AddressOf OnFormKeyDown
        AddHandler Me.KeyPress, AddressOf OnFormKeyPress
        KeyPreview = True
    End Sub

    ''' <summary>/admin [cmd|powershell] — used when a non-elevated Terminal relaunches us elevated.</summary>
    Private Sub ParseStartupArgs()
        Dim args() As String = Environment.GetCommandLineArgs()
        For i As Integer = 1 To args.Length - 1
            If String.Equals(args(i), "/admin", StringComparison.OrdinalIgnoreCase) Then
                startupAdminKind = ShellKind.Cmd
                If i + 1 < args.Length Then
                    Dim shellArg As String = args(i + 1)
                    If shellArg.StartsWith("ps", StringComparison.OrdinalIgnoreCase) OrElse _
                       shellArg.StartsWith("power", StringComparison.OrdinalIgnoreCase) Then
                        startupAdminKind = ShellKind.PowerShell
                    End If
                End If
                Return
            End If
        Next
    End Sub

    ''' <summary>Let the medium-integrity OSK PostMessage into this elevated admin window.</summary>
    Private Sub OnTerminalHandleCreated(ByVal sender As Object, ByVal e As EventArgs)
        If Not isAdminWindow Then Return
        If WM_LCARS_OSK_INPUT = 0 Then Return
        Try
            ChangeWindowMessageFilterEx(Me.Handle, WM_LCARS_OSK_INPUT, MSGFLT_ALLOW, IntPtr.Zero)
        Catch
        End Try
    End Sub

    Private Sub BuildChrome()
        Dim elbTop As New Elbow()
        elbTop.Name = "elbTop"
        elbTop.ElbowStyle = Elbow.LCARSelbowStyles.UpperLeft
        elbTop.Color = LCARS.LCARScolorStyles.LCARSDisplayOnly
        elbTop.ButtonWidth = LeftBorder
        elbTop.ButtonHeight = 36
        elbTop.Clickable = False
        elbTop.ButtonText = ""
        elbTop.Text = ""
        elbTop.Size = New Size(LeftBorder + LeftElbowArm, TopChrome)
        elbTop.Location = New Point(RailLeft, 4)
        WireDrag(elbTop)
        Controls.Add(elbTop)

        ' Settings-style upper-right elbow covering the right *control* column (not tabs).
        Dim elbRight As New Elbow()
        elbRight.Name = "elbRightTop"
        elbRight.ElbowStyle = Elbow.LCARSelbowStyles.UpperRight
        elbRight.Color = LCARS.LCARScolorStyles.LCARSDisplayOnly
        elbRight.ButtonWidth = RailWidth
        elbRight.ButtonHeight = 20
        elbRight.Clickable = False
        elbRight.ButtonText = ""
        elbRight.Text = ""
        elbRight.Size = New Size(RailWidth + 20, TopChrome - 8)
        WireDrag(elbRight)
        Controls.Add(elbRight)

        Dim fbTitle As New FlatButton()
        fbTitle.Name = "fbModeTerminal"
        fbTitle.ButtonText = "TERMINAL"
        fbTitle.Text = "TERMINAL"
        fbTitle.Clickable = True
        fbTitle.Beeping = True
        fbTitle.Color = LCARS.LCARScolorStyles.StaticTan
        fbTitle.ButtonTextAlign = ContentAlignment.BottomRight
        fbTitle.ButtonTextHeight = 14
        fbTitle.Size = New Size(ModeTerminalWidth, 36)
        AddHandler fbTitle.Click, AddressOf OnModeTerminal
        Controls.Add(fbTitle)
        fbModeTerminal = fbTitle

        Dim fbRdp As New FlatButton()
        fbRdp.Name = "fbModeRdp"
        fbRdp.ButtonText = "RDP"
        fbRdp.Text = "RDP"
        fbRdp.Clickable = True
        fbRdp.Beeping = True
        fbRdp.Color = LCARS.LCARScolorStyles.LCARSDisplayOnly
        fbRdp.ButtonTextAlign = ContentAlignment.BottomRight
        fbRdp.ButtonTextHeight = 14
        fbRdp.Size = New Size(ModeRdpWidth, 36)
        AddHandler fbRdp.Click, AddressOf OnModeRdp
        Controls.Add(fbRdp)
        fbModeRdp = fbRdp

        Dim fbTopFill As New FlatButton()
        fbTopFill.Name = "fbTopFill"
        fbTopFill.ButtonText = ""
        fbTopFill.Text = ""
        fbTopFill.Clickable = False
        fbTopFill.Color = LCARS.LCARScolorStyles.StaticTan
        fbTopFill.Height = 10
        fbTopFill.Top = 4
        WireDrag(fbTopFill)
        Controls.Add(fbTopFill)

        Dim fbTopEnd As New HalfPillButton()
        fbTopEnd.Name = "fbTopEnd"
        fbTopEnd.ButtonText = ""
        fbTopEnd.Text = ""
        fbTopEnd.Clickable = False
        fbTopEnd.Color = LCARS.LCARScolorStyles.StaticTan
        fbTopEnd.ButtonStyle = HalfPillButton.LCARSbuttonStyles.PillRight
        fbTopEnd.Size = New Size(40, 36)
        fbTopEnd.Top = 4
        fbTopEnd.Visible = False
        WireDrag(fbTopEnd)
        Controls.Add(fbTopEnd)

        fbModeTerminal.BringToFront()
        fbModeRdp.BringToFront()

        fbNewTab = MakeRailButton("fbNewTab", ShellKindUtil.TabLabel(lastKind, isAdminWindow), LCARS.LCARScolorStyles.PrimaryFunction)
        AddHandler fbNewTab.Click, AddressOf NewTabLastUsed
        Controls.Add(fbNewTab)

        abNewMenu = New ArrowButton()
        abNewMenu.Name = "abNewMenu"
        abNewMenu.ArrowDirection = LCARS.LCARSarrowDirection.Left
        abNewMenu.Color = LCARS.LCARScolorStyles.PrimaryFunction
        abNewMenu.Beeping = True
        abNewMenu.Size = New Size(28, 28)
        AddHandler abNewMenu.Click, AddressOf ToggleShellPicker
        Controls.Add(abNewMenu)

        fbCloseTab = MakeRailButton("fbCloseTab", "CLOSE TAB", LCARS.LCARScolorStyles.NavigationFunction)
        AddHandler fbCloseTab.Click, AddressOf CloseCurrent
        Controls.Add(fbCloseTab)

        fbRdpConnect = MakeRailButton("fbRdpConnect", "CONNECT", LCARS.LCARScolorStyles.PrimaryFunction)
        AddHandler fbRdpConnect.Click, AddressOf OnRdpConnect
        fbRdpConnect.Visible = False
        Controls.Add(fbRdpConnect)

        fbRdpNew = MakeRailButton("fbRdpNew", "NEW", LCARS.LCARScolorStyles.StaticTan)
        AddHandler fbRdpNew.Click, AddressOf OnRdpNew
        fbRdpNew.Visible = False
        Controls.Add(fbRdpNew)

        fbRdpDelete = MakeRailButton("fbRdpDelete", "DELETE", LCARS.LCARScolorStyles.NavigationFunction)
        AddHandler fbRdpDelete.Click, AddressOf OnRdpDelete
        fbRdpDelete.Visible = False
        Controls.Add(fbRdpDelete)

        fbRdpOpen = MakeRailButton("fbRdpOpen", "OPEN .RDP", LCARS.LCARScolorStyles.StaticTan)
        AddHandler fbRdpOpen.Click, AddressOf OnRdpOpen
        fbRdpOpen.Visible = False
        Controls.Add(fbRdpOpen)

        fbRdpSave = MakeRailButton("fbRdpSave", "SAVE .RDP", LCARS.LCARScolorStyles.StaticTan)
        AddHandler fbRdpSave.Click, AddressOf OnRdpSave
        fbRdpSave.Visible = False
        Controls.Add(fbRdpSave)

        fbDisconnect = MakeRailButton("fbDisconnect", "DISCONNECT", LCARS.LCARScolorStyles.Orange)
        AddHandler fbDisconnect.Click, AddressOf OnRdpDisconnect
        fbDisconnect.Visible = False
        Controls.Add(fbDisconnect)

        InitTopOpt(chkDrives, "DRIVES", False)
        InitTopOpt(chkPrinters, "PRINTERS", False)
        InitTopOpt(chkClipboard, "CLIPBOARD", True)
        InitTopOpt(chkAudio, "AUDIO", True)
        InitTopOpt(chkSmart, "SMART SIZE", True)

        Dim fbRailFill As New FlatButton()
        fbRailFill.Name = "fbRailFill"
        fbRailFill.ButtonText = ""
        fbRailFill.Text = ""
        fbRailFill.Clickable = False
        fbRailFill.Color = LCARS.LCARScolorStyles.LCARSDisplayOnly
        fbRailFill.Width = LeftBorder
        Controls.Add(fbRailFill)

        fbClose = MakeRailButton("fbClose", "CLOSE", LCARS.LCARScolorStyles.Orange)
        AddHandler fbClose.Click, AddressOf CloseApp
        Controls.Add(fbClose)

        Dim elbBottom As New Elbow()
        elbBottom.Name = "elbBottom"
        elbBottom.ElbowStyle = Elbow.LCARSelbowStyles.LowerLeft
        elbBottom.Color = LCARS.LCARScolorStyles.LCARSDisplayOnly
        elbBottom.ButtonWidth = LeftBorder
        elbBottom.ButtonHeight = 20
        elbBottom.Clickable = False
        elbBottom.ButtonText = ""
        elbBottom.Text = ""
        elbBottom.Size = New Size(LeftBorder + LeftElbowArm, 40)
        Controls.Add(elbBottom)

        Dim fbBottomFill As New FlatButton()
        fbBottomFill.Name = "fbBottomFill"
        fbBottomFill.ButtonText = ""
        fbBottomFill.Text = ""
        fbBottomFill.Clickable = False
        fbBottomFill.Color = LCARS.LCARScolorStyles.StaticTan
        fbBottomFill.Height = 10
        Controls.Add(fbBottomFill)
    End Sub

    Private Sub InitTopOpt(ByVal box As CheckBox, ByVal caption As String, ByVal checkedDefault As Boolean)
        box.Text = caption
        box.ForeColor = Color.Orange
        box.BackColor = Color.Black
        box.AutoSize = True
        box.Checked = checkedDefault
        box.Visible = False
        AddHandler box.CheckedChanged, AddressOf OnRdpOptionChanged
        Controls.Add(box)
    End Sub

    Private Function MakeRailButton(ByVal name As String, ByVal caption As String, ByVal color As LCARS.LCARScolorStyles) As FlatButton
        Dim btn As New FlatButton()
        btn.Name = name
        btn.ButtonText = caption
        btn.Text = caption
        btn.Beeping = True
        btn.Color = color
        btn.ButtonTextAlign = ContentAlignment.MiddleCenter
        btn.Size = New Size(RailWidth, 28)
        Return btn
    End Function

    Private Sub BuildContent()
        status.AutoSize = False
        status.Height = 22
        status.ForeColor = Color.Orange
        status.BackColor = Color.Black
        status.Text = "READY"
        status.TextAlign = ContentAlignment.MiddleLeft
        Controls.Add(status)

        terminalHost.BackColor = Color.Black
        tabStrip.Font = New Font("Arial Narrow", 11.0F, FontStyle.Bold)
        tabStrip.BackColor = Color.Black
        tabStrip.Dock = DockStyle.Fill
        ' Stay on the TOP edge; RightToLeft packs the tab headers toward the right.
        tabStrip.Alignment = TabAlignment.Top
        tabStrip.SizeMode = TabSizeMode.Normal
        tabStrip.Multiline = False
        tabStrip.RightToLeft = RightToLeft.Yes
        tabStrip.RightToLeftLayout = True
        AddHandler tabStrip.SelectedIndexChanged, AddressOf OnTabSelected
        terminalHost.Controls.Add(tabStrip)
        Controls.Add(terminalHost)

        rdpWorkspace = New RdpWorkspace(New RdpProfileStore())
        rdpWorkspace.Visible = False
        AddHandler rdpWorkspace.StatusChanged, AddressOf OnRdpStatus
        AddHandler rdpWorkspace.RequestCredentials, AddressOf OnRdpCredentials
        Controls.Add(rdpWorkspace)
        SyncRdpOptionsToPanel()
    End Sub

    Private Sub LayoutChrome()
        Dim contentLeft As Integer = RailLeft + LeftBorder + LeftElbowArm + ContentGap
        Dim contentRightPad As Integer = RailWidth + ContentGap + 8
        Dim rightRail As Integer = ClientSize.Width - RailWidth - RailLeft
        Dim rdpMode As Boolean = (_uiMode = TerminalUiMode.Rdp)
        Dim contentTop As Integer = If(rdpMode, TopChrome + 28, TopChrome + 6)

        Dim fbTopFill As Control = FindNamed("fbTopFill")
        Dim fbTopEnd As Control = FindNamed("fbTopEnd")
        Dim fbRailFill As Control = FindNamed("fbRailFill")
        Dim elbBottom As Control = FindNamed("elbBottom")
        Dim elbTop As Control = FindNamed("elbTop")
        Dim elbRight As Control = FindNamed("elbRightTop")
        Dim fbBottomFill As Control = FindNamed("fbBottomFill")

        If elbTop IsNot Nothing Then
            elbTop.Location = New Point(RailLeft, 4)
            elbTop.Size = New Size(LeftBorder + LeftElbowArm, TopChrome - 8)
        End If
        If elbRight IsNot Nothing Then
            elbRight.Size = New Size(RailWidth + 20, TopChrome - 8)
            elbRight.Location = New Point(ClientSize.Width - elbRight.Width - RailLeft, 4)
        End If

        ' Mode switches stay on the TOP border, right-justified against the right elbow.
        Dim modeTop As Integer = 4
        Dim modeRightEdge As Integer = If(elbRight IsNot Nothing, elbRight.Left - 6, rightRail - 6)
        If fbModeRdp IsNot Nothing Then
            fbModeRdp.Size = New Size(ModeRdpWidth, 36)
            fbModeRdp.Location = New Point(modeRightEdge - ModeRdpWidth, modeTop)
            fbModeRdp.BringToFront()
            modeRightEdge = fbModeRdp.Left - 6
        End If
        If fbModeTerminal IsNot Nothing Then
            fbModeTerminal.Size = New Size(ModeTerminalWidth, 36)
            fbModeTerminal.Location = New Point(modeRightEdge - ModeTerminalWidth, modeTop)
            fbModeTerminal.BringToFront()
            modeRightEdge = fbModeTerminal.Left - 6
        End If

        Dim railY As Integer = TopChrome + 8

        fbNewTab.Visible = Not rdpMode
        abNewMenu.Visible = Not rdpMode
        fbRdpConnect.Visible = rdpMode
        fbRdpNew.Visible = rdpMode
        fbRdpDelete.Visible = rdpMode
        fbRdpOpen.Visible = rdpMode
        fbRdpSave.Visible = rdpMode
        fbDisconnect.Visible = rdpMode
        fbCloseTab.Visible = True

        chkDrives.Visible = rdpMode
        chkPrinters.Visible = rdpMode
        chkClipboard.Visible = rdpMode
        chkAudio.Visible = rdpMode
        chkSmart.Visible = rdpMode

        Dim lastRail As Control = Nothing
        If Not rdpMode Then
            fbNewTab.Location = New Point(rightRail, railY)
            fbNewTab.Width = RailWidth - 30
            abNewMenu.Location = New Point(rightRail + fbNewTab.Width + 2, railY)
            abNewMenu.Height = fbNewTab.Height
            fbCloseTab.Location = New Point(rightRail, railY + 34)
            fbCloseTab.Width = RailWidth
            fbCloseTab.ButtonText = "CLOSE TAB"
            fbCloseTab.Text = "CLOSE TAB"
            lastRail = fbCloseTab
        Else
            Dim y As Integer = railY
            PlaceRightRail(fbRdpConnect, rightRail, y) : y += 30
            PlaceRightRail(fbRdpNew, rightRail, y) : y += 30
            PlaceRightRail(fbRdpDelete, rightRail, y) : y += 30
            PlaceRightRail(fbRdpOpen, rightRail, y) : y += 30
            PlaceRightRail(fbRdpSave, rightRail, y) : y += 30
            PlaceRightRail(fbDisconnect, rightRail, y) : y += 30
            PlaceRightRail(fbCloseTab, rightRail, y)
            fbCloseTab.ButtonText = "CLOSE TAB"
            fbCloseTab.Text = "CLOSE TAB"
            lastRail = fbCloseTab
        End If

        ' CLOSE shares the Start Menu horizontal row on the right.
        PlaceShellAlignedCloseButton(fbClose, RailLeft)
        fbClose.ButtonText = "CLOSE"
        fbClose.Text = "CLOSE"
        lastRail = fbClose

        If fbTopFill IsNot Nothing Then
            Dim fillLeft As Integer = contentLeft
            Dim fillRight As Integer = modeRightEdge - 4
            fbTopFill.Top = 4
            fbTopFill.Height = 10
            fbTopFill.Left = fillLeft
            fbTopFill.Width = Math.Max(20, fillRight - fillLeft)
            If rdpMode Then
                Dim optX As Integer = fillLeft
                Dim optY As Integer = 42
                PlaceTopOpt(chkDrives, optX, optY)
                PlaceTopOpt(chkPrinters, optX, optY)
                PlaceTopOpt(chkClipboard, optX, optY)
                PlaceTopOpt(chkAudio, optX, optY)
                PlaceTopOpt(chkSmart, optX, optY)
                chkDrives.BringToFront()
                chkPrinters.BringToFront()
                chkClipboard.BringToFront()
                chkAudio.BringToFront()
                chkSmart.BringToFront()
            End If
        End If
        If fbTopEnd IsNot Nothing Then fbTopEnd.Visible = False

        If elbBottom IsNot Nothing Then
            elbBottom.Size = New Size(LeftBorder + LeftElbowArm, 40)
            elbBottom.Location = New Point(RailLeft, ClientSize.Height - elbBottom.Height - 4)
        End If

        If fbBottomFill IsNot Nothing AndAlso elbBottom IsNot Nothing Then
            fbBottomFill.Location = New Point(elbBottom.Right + 4, ClientSize.Height - 14)
            fbBottomFill.Width = Math.Max(40, ClientSize.Width - fbBottomFill.Left - contentRightPad)
        End If
        If fbRailFill IsNot Nothing Then
            Dim fillTop As Integer = TopChrome + 8
            Dim fillBottom As Integer = If(elbBottom IsNot Nothing, elbBottom.Top - 6, ClientSize.Height - BottomChrome - 6)
            fbRailFill.Location = New Point(RailLeft, fillTop)
            fbRailFill.Width = LeftBorder
            fbRailFill.Height = Math.Max(20, fillBottom - fillTop)
        End If

        Dim statusLeft As Integer = contentLeft
        If elbBottom IsNot Nothing Then
            statusLeft = elbBottom.Right + 6
        End If
        Dim statusTop As Integer = ClientSize.Height - BottomChrome - 2
        If fbBottomFill IsNot Nothing Then
            statusTop = fbBottomFill.Top - 22
        End If
        status.SetBounds(statusLeft, statusTop, Math.Max(100, ClientSize.Width - statusLeft - contentRightPad), 20)

        Dim contentBounds As New Rectangle(contentLeft, contentTop, Math.Max(100, ClientSize.Width - contentLeft - contentRightPad), Math.Max(80, status.Top - contentTop - 4))
        terminalHost.Bounds = contentBounds
        If rdpWorkspace IsNot Nothing Then
            rdpWorkspace.Bounds = contentBounds
            rdpWorkspace.BringToFront()
        End If
        If fbModeTerminal IsNot Nothing Then fbModeTerminal.BringToFront()
        If fbModeRdp IsNot Nothing Then fbModeRdp.BringToFront()
        If elbRight IsNot Nothing Then elbRight.BringToFront()
    End Sub

    Private Sub PlaceRail(ByVal btn As FlatButton, ByVal y As Integer)
        btn.Location = New Point(RailLeft, y)
        btn.Width = RailWidth
        btn.BringToFront()
    End Sub

    Private Sub PlaceRightRail(ByVal btn As FlatButton, ByVal x As Integer, ByVal y As Integer)
        btn.Location = New Point(x, y)
        btn.Width = RailWidth
        btn.BringToFront()
    End Sub

    Private Sub PlaceTopOpt(ByVal box As CheckBox, ByRef x As Integer, ByVal y As Integer)
        box.Location = New Point(x, y)
        x = box.Right + 10
    End Sub

    Private Sub SyncRdpOptionsToPanel()
        If rdpWorkspace Is Nothing OrElse rdpWorkspace.ListPanel Is Nothing Then Return
        Dim p As RdpConnectionListPanel = rdpWorkspace.ListPanel
        p.RedirectDrives = chkDrives.Checked
        p.RedirectPrinters = chkPrinters.Checked
        p.RedirectClipboard = chkClipboard.Checked
        p.RedirectAudio = chkAudio.Checked
        p.SmartSizing = chkSmart.Checked
    End Sub

    Private Sub OnRdpOptionChanged(ByVal sender As Object, ByVal e As EventArgs)
        SyncRdpOptionsToPanel()
    End Sub

    Private Sub OnRdpConnect(ByVal sender As Object, ByVal e As EventArgs)
        SyncRdpOptionsToPanel()
        If rdpWorkspace IsNot Nothing Then rdpWorkspace.RailConnect()
    End Sub

    Private Sub OnRdpNew(ByVal sender As Object, ByVal e As EventArgs)
        SyncRdpOptionsToPanel()
        If rdpWorkspace IsNot Nothing Then rdpWorkspace.RailNew()
    End Sub

    Private Sub OnRdpDelete(ByVal sender As Object, ByVal e As EventArgs)
        If rdpWorkspace IsNot Nothing Then rdpWorkspace.RailDelete()
    End Sub

    Private Sub OnRdpOpen(ByVal sender As Object, ByVal e As EventArgs)
        If rdpWorkspace IsNot Nothing Then rdpWorkspace.RailOpen()
        SyncCheckboxesFromPanel()
    End Sub

    Private Sub OnRdpSave(ByVal sender As Object, ByVal e As EventArgs)
        SyncRdpOptionsToPanel()
        If rdpWorkspace IsNot Nothing Then rdpWorkspace.RailSave()
    End Sub

    Private Sub SyncCheckboxesFromPanel()
        If rdpWorkspace Is Nothing OrElse rdpWorkspace.ListPanel Is Nothing Then Return
        Dim p As RdpConnectionListPanel = rdpWorkspace.ListPanel
        chkDrives.Checked = p.RedirectDrives
        chkPrinters.Checked = p.RedirectPrinters
        chkClipboard.Checked = p.RedirectClipboard
        chkAudio.Checked = p.RedirectAudio
        chkSmart.Checked = p.SmartSizing
    End Sub

    Private Function FindNamed(ByVal name As String) As Control
        Dim found() As Control = Controls.Find(name, False)
        If found.Length > 0 Then Return found(0)
        Return Nothing
    End Function

    Private Sub WireDrag(ByVal c As Control)
        AddHandler c.MouseDown, AddressOf Chrome_MouseDown
    End Sub

    Private Sub Chrome_MouseDown(ByVal sender As Object, ByVal e As MouseEventArgs)
        If e.Button <> MouseButtons.Left Then Return
        ReleaseCapture()
        SendMessage(Me.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0)
    End Sub

    Private Sub OnTerminalResize(ByVal sender As Object, ByVal e As EventArgs)
        LayoutChrome()
    End Sub

    Private Sub OnTerminalShown(ByVal sender As Object, ByVal e As EventArgs)
        ' Re-join linked-window list (shell drops timed-out hwnds) and snap to desktop hole.
        EnsureRegisteredWithLcarsShell()
        If BindToWorkingArea Then
            Dim wa As Rectangle = Screen.FromHandle(Me.Handle).WorkingArea
            Try
                ' Prefer SPI work area already adjusted by LCARS.
                ApplyWorkingAreaBounds(wa)
            Catch
            End Try
        End If
        LayoutChrome()
        If startupAdminKind.HasValue Then
            ' Elevated instance launched for a specific admin shell — open that tab once.
            Dim kind As ShellKind = startupAdminKind.Value
            startupAdminKind = Nothing
            AddTab(kind, True)
        Else
            NewTabLastUsed(Nothing, Nothing)
        End If
        FocusCurrentTerminalView()
    End Sub

    Private Sub OnTerminalActivated(ByVal sender As Object, ByVal e As EventArgs)
        ' OSK is WS_EX_NOACTIVATE; reclaim console focus whenever Terminal is foreground.
        FocusCurrentTerminalView()
    End Sub

    Private Sub FocusCurrentTerminalView()
        If _uiMode <> TerminalUiMode.Terminal Then Return
        If tabStrip.SelectedTab Is Nothing Then Return
        Dim view As ConsoleHostView = TryCast(tabStrip.SelectedTab.Tag, ConsoleHostView)
        If view IsNot Nothing Then view.FocusInput()
    End Sub

    Private Function CurrentConsoleHost() As ConsoleHostView
        If _uiMode <> TerminalUiMode.Terminal Then Return Nothing
        If tabStrip.SelectedTab Is Nothing Then Return Nothing
        Return TryCast(tabStrip.SelectedTab.Tag, ConsoleHostView)
    End Function

    Private Sub OnFormKeyDown(ByVal sender As Object, ByVal e As KeyEventArgs)
        Dim view As ConsoleHostView = CurrentConsoleHost()
        If view Is Nothing Then Return
        If view.InputHasFocus Then Return
        view.ProcessExternalKeyDown(e)
    End Sub

    Private Sub OnFormKeyPress(ByVal sender As Object, ByVal e As KeyPressEventArgs)
        Dim view As ConsoleHostView = CurrentConsoleHost()
        If view Is Nothing Then Return
        If view.InputHasFocus Then Return
        view.ProcessExternalKeyPress(e)
        e.Handled = True
    End Sub

    Private Shared crashLoggingInstalled As Boolean = False

    ''' <summary>Record terminal faults to disk; tablet crashes otherwise leave no evidence.</summary>
    Private Shared Sub InstallCrashLogging()
        If crashLoggingInstalled Then Return
        crashLoggingInstalled = True
        AddHandler Application.ThreadException, _
            Sub(s As Object, args As System.Threading.ThreadExceptionEventArgs) WriteCrashLog("ThreadException", args.Exception)
        AddHandler AppDomain.CurrentDomain.UnhandledException, _
            Sub(s As Object, args As UnhandledExceptionEventArgs) WriteCrashLog("UnhandledException", TryCast(args.ExceptionObject, Exception))
    End Sub

    Private Shared Sub WriteCrashLog(ByVal source As String, ByVal ex As Exception)
        Try
            Dim dir As String = System.IO.Path.Combine( _
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LCARS")
            System.IO.Directory.CreateDirectory(dir)
            Dim line As String = DateTime.Now.ToString("s") & "  " & source & vbCrLf & _
                                 If(ex Is Nothing, "(no exception object)", ex.ToString()) & vbCrLf & vbCrLf
            System.IO.File.AppendAllText(System.IO.Path.Combine(dir, "terminal-crash.log"), line)
        Catch
        End Try
    End Sub

    Protected Overrides Sub WndProc(ByRef m As Message)
        If WM_LCARS_OSK_INPUT <> 0 AndAlso m.Msg = WM_LCARS_OSK_INPUT Then
            Dim kind As Integer = m.WParam.ToInt32()
            Dim payload As Integer = m.LParam.ToInt32() And &HFFFF
            If _uiMode = TerminalUiMode.Rdp AndAlso rdpWorkspace IsNot Nothing Then
                If kind = OskInputChar Then
                    rdpWorkspace.InjectChar(ChrW(payload))
                ElseIf kind = OskInputVk Then
                    rdpWorkspace.InjectVirtualKey(CType(payload, Keys))
                End If
            Else
                Dim view As ConsoleHostView = CurrentConsoleHost()
                If view IsNot Nothing Then
                    If kind = OskInputChar Then
                        view.InjectChar(ChrW(payload))
                    ElseIf kind = OskInputVk Then
                        view.InjectVirtualKey(CType(payload, Keys))
                    End If
                End If
            End If
            m.Result = New IntPtr(1)
            Return
        End If
        MyBase.WndProc(m)
    End Sub

    Private Sub NewTabLastUsed(ByVal sender As Object, ByVal e As EventArgs)
        CloseShellPicker()
        If _uiMode = TerminalUiMode.Rdp Then
            If rdpWorkspace IsNot Nothing Then rdpWorkspace.FocusNewConnection()
            Return
        End If
        ' New Tab never auto-elevates. ADMIN is only via the picker (or this already being the admin window).
        AddTab(lastKind, isAdminWindow)
    End Sub

    Private Sub OnModeTerminal(ByVal sender As Object, ByVal e As EventArgs)
        SetUiMode(TerminalUiMode.Terminal)
    End Sub

    Private Sub OnModeRdp(ByVal sender As Object, ByVal e As EventArgs)
        SetUiMode(TerminalUiMode.Rdp)
    End Sub

    Private Sub SetUiMode(ByVal mode As TerminalUiMode)
        CloseShellPicker()
        _uiMode = mode
        Dim terminalActive As Boolean = (mode = TerminalUiMode.Terminal)
        terminalHost.Visible = terminalActive
        If rdpWorkspace IsNot Nothing Then rdpWorkspace.Visible = Not terminalActive
        If fbModeTerminal IsNot Nothing Then
            fbModeTerminal.Color = If(terminalActive, LCARS.LCARScolorStyles.StaticTan, LCARS.LCARScolorStyles.LCARSDisplayOnly)
        End If
        If fbModeRdp IsNot Nothing Then
            fbModeRdp.Color = If(terminalActive, LCARS.LCARScolorStyles.LCARSDisplayOnly, LCARS.LCARScolorStyles.StaticTan)
        End If
        If terminalActive Then
            UpdateNewTabCaption()
            status.Text = "READY"
        Else
            status.Text = "REMOTE READY"
            SyncRdpOptionsToPanel()
        End If
        LayoutChrome()
    End Sub

    Private Sub OnRdpStatus(ByVal sender As Object, ByVal message As String)
        status.Text = message
        SyncCheckboxesFromPanel()
    End Sub

    Private Sub OnRdpCredentials(ByVal sender As Object, ByVal req As RdpCredentialRequest)
        Using dlg As New frmRdpCredentials()
            dlg.UserName = req.UserName
            dlg.Password = req.Password
            dlg.RememberPassword = req.RememberPassword
            If dlg.ShowDialog(Me) = DialogResult.OK Then
                req.Accepted = True
                req.UserName = dlg.UserName
                req.Password = dlg.Password
                req.RememberPassword = dlg.RememberPassword
            Else
                req.Accepted = False
            End If
        End Using
    End Sub

    Private Sub OnRdpDisconnect(ByVal sender As Object, ByVal e As EventArgs)
        If rdpWorkspace IsNot Nothing Then rdpWorkspace.DisconnectCurrent()
    End Sub

    Private Sub ToggleShellPicker(ByVal sender As Object, ByVal e As EventArgs)
        If shellPicker IsNot Nothing AndAlso Not shellPicker.IsDisposed AndAlso shellPicker.Visible Then
            CloseShellPicker()
            Return
        End If
        CloseShellPicker()
        shellPicker = New frmShellPicker()
        AddHandler shellPicker.ShellChosen, AddressOf OnShellChosen
        AddHandler shellPicker.FormClosed, Sub(s As Object, args As FormClosedEventArgs) shellPicker = Nothing
        shellPicker.ShowBeside(Me, abNewMenu)
    End Sub

    Private Sub OnShellChosen(ByVal kind As ShellKind, ByVal elevated As Boolean)
        lastKind = kind
        SaveLastShell()
        UpdateNewTabCaption()
        ' In the admin window every tab is already elevated — ignore picker ADMIN flag for relaunch.
        If isAdminWindow Then
            AddTab(kind, True)
        Else
            AddTab(kind, elevated)
        End If
    End Sub

    Private Sub CloseShellPicker()
        If shellPicker IsNot Nothing AndAlso Not shellPicker.IsDisposed Then
            shellPicker.Close()
            shellPicker = Nothing
        End If
    End Sub

    Private Sub UpdateNewTabCaption()
        Dim caption As String = ShellKindUtil.TabLabel(lastKind, isAdminWindow)
        fbNewTab.ButtonText = caption
        fbNewTab.Text = caption
    End Sub

    Private Sub LoadLastShell()
        Try
            Dim kindVal As Integer = CInt(Val(GetSetting("LCARS x32", "Terminal", "LastShell", "0")))
            If kindVal = CInt(ShellKind.PowerShell) Then
                lastKind = ShellKind.PowerShell
            Else
                lastKind = ShellKind.Cmd
            End If
        Catch
            lastKind = ShellKind.Cmd
        End Try
        ' Do not restore LastElevated — that trapped every New Tab / startup in ADMIN.
        Try
            SaveSetting("LCARS x32", "Terminal", "LastElevated", "False")
        Catch
        End Try
    End Sub

    Private Sub SaveLastShell()
        Try
            SaveSetting("LCARS x32", "Terminal", "LastShell", CInt(lastKind).ToString())
            SaveSetting("LCARS x32", "Terminal", "LastElevated", "False")
        Catch
        End Try
    End Sub

    Private Sub AddTab(ByVal kind As ShellKind, ByVal elevated As Boolean)
        status.Text = "OPENING " & ShellKindUtil.TabLabel(kind, elevated OrElse isAdminWindow) & "..."
        Application.DoEvents()

        ' Normal window + ADMIN pick: spawn a second LCARS Terminal elevated (real console, LCARS chrome).
        If elevated AndAlso Not isAdminWindow Then
            Dim err As String = ConsoleHostView.LaunchElevatedTerminal(kind)
            If err Is Nothing Then
                status.Text = "LAUNCHED — " & ShellKindUtil.TabLabel(kind, True) & "  [ADMIN WINDOW]"
            Else
                status.Text = "ERROR: " & err
            End If
            Return
        End If

        Dim showAdmin As Boolean = isAdminWindow
        Dim page As New TabPage(ShellKindUtil.TabLabel(kind, showAdmin))
        page.BackColor = Color.Black
        tabStrip.TabPages.Add(page)
        tabStrip.SelectedTab = page
        Application.DoEvents()

        Dim host As New ConsoleHostView()
        page.Controls.Add(host)
        Application.DoEvents()

        Dim started As Boolean = False
        Dim failReason As String = "unknown"
        Try
            started = host.StartShell(kind, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))
            If Not started Then failReason = host.LastError
        Catch ex As Exception
            started = False
            failReason = ex.Message
        End Try

        If Not started Then
            page.Controls.Remove(host)
            host.Dispose()
            tabStrip.TabPages.Remove(page)
            status.Text = "ERROR: " & failReason
            Return
        End If

        AddHandler host.SessionExited, AddressOf OnConsoleSessionExited
        page.Tag = host
        status.Text = "READY — " & ShellKindUtil.TabLabel(kind, showAdmin) & "  [CONSOLE]"
        host.FocusInput()
    End Sub

    ''' <summary>Shell exited on its own (e.g. the user typed "exit") — retire the tab.</summary>
    Private Sub OnConsoleSessionExited(ByVal sender As Object, ByVal e As EventArgs)
        Dim host As ConsoleHostView = TryCast(sender, ConsoleHostView)
        If host Is Nothing Then Return
        For Each page As TabPage In tabStrip.TabPages
            If page.Tag Is host Then
                host.Detach()
                tabStrip.TabPages.Remove(page)
                status.Text = "SESSION ENDED"
                Exit For
            End If
        Next
    End Sub

    Private Sub OnTabSelected(ByVal sender As Object, ByVal e As EventArgs)
        If tabStrip.SelectedTab Is Nothing Then Return
        Dim view As ConsoleHostView = TryCast(tabStrip.SelectedTab.Tag, ConsoleHostView)
        If view IsNot Nothing Then view.FocusInput()
    End Sub

    Private Sub CloseCurrent(ByVal sender As Object, ByVal e As EventArgs)
        CloseShellPicker()
        If _uiMode = TerminalUiMode.Rdp Then
            If rdpWorkspace IsNot Nothing Then rdpWorkspace.CloseCurrentSession()
            Return
        End If
        If tabStrip.SelectedTab Is Nothing Then Return
        Dim page As TabPage = tabStrip.SelectedTab
        Dim view As ConsoleHostView = TryCast(page.Tag, ConsoleHostView)
        If view IsNot Nothing Then view.Detach()
        tabStrip.TabPages.Remove(page)
        status.Text = "TAB CLOSED"
    End Sub

    Private Sub CloseApp(ByVal sender As Object, ByVal e As EventArgs)
        Close()
    End Sub

    Private Sub OnTerminalClosing(ByVal sender As Object, ByVal e As FormClosingEventArgs)
        CloseShellPicker()
        If rdpWorkspace IsNot Nothing Then rdpWorkspace.CloseAllSessions()
        For Each page As TabPage In tabStrip.TabPages
            Dim view As ConsoleHostView = TryCast(page.Tag, ConsoleHostView)
            If view IsNot Nothing Then view.Detach()
        Next
    End Sub
End Class
