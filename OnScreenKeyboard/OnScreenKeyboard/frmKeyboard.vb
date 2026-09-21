Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Drawing
Imports System.IO
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports System.Security.Principal
Imports System.Text
Imports System.Windows.Forms
Imports Microsoft.VisualBasic.CompilerServices
Imports LCARS
Imports LCARS.Controls

Partial Public Class frmKeyboard
        Public Structure RECT
            Public Left_Renamed As Integer

            Public Top_Renamed As Integer

            Public Right_Renamed As Integer

            Public Bottom_Renamed As Integer
        End Structure

        Private Class ButtonLayout
        Public Left As Integer
        Public Top As Integer
        Public Width As Integer
        Public Height As Integer
        Public Control As Control
        Public InPanel2 As Boolean
    End Class

    ' Schema 20 = original defaults + SplitContainer Top|Bottom|Left|Right (required for panel-ratio scale).
    ' Bump clears poisoned compact sizes from schemas 14-19.
    Private Const CurrentSizeSchema As String = "20"
    ' Internal only (scale logs); title bar is always "MOVE KEYPAD".
    Private Const OskBuildId As String = "175"
    Private _scaleBusy As Boolean
    Private _snapEnabled As Boolean = True
    Private _startupInitDone As Boolean = False
    Private _hiddenInitQueued As Boolean = False
    Private _snapBusy As Boolean
    Private _snapPreviewPoint As Point = Point.Empty


































































































































        Private Const WM_KEYDOWN As Integer = 256

        Private Const WM_KEYUP As Integer = 257

        Private Const VK_RETURN As Integer = 13

        Private Const KEYEVENTF_EXTENDEDKEY As Long = 1L

        Private Const KEYEVENTF_KEYUP As Long = 2L

        Private Const VK_LWIN As Byte = 91

        Private Const VK_SCROLL As Integer = 145

        Private Const VK_NUMLOCK As Integer = 144

        Private Const VK_CAPITAL As Integer = 20

        Private Const VK_ALT As Integer = 18

        Private Const VK_TAB As Integer = 9

        Private Const VK_0 As Integer = 48

        Private Const VK_PRINT As Integer = 42

        Private Const VK_BACK As Integer = 8

        Private Const VK_DELETE As Integer = 46

        Private Const VK_RETURN_BYTE As Byte = 13

        Private Const VK_ESCAPE As Integer = 27

        Private Const GA_ROOT As Integer = 2

        Private Shared ReadOnly WM_LCARS_OSK_INPUT As Integer

        Private Const OskInputChar As Integer = 1

        Private Const OskInputVk As Integer = 2

        Private _lastInputTarget As IntPtr

        Private Const GWL_EXSTYLE As Integer = -20

        Private Const WS_EX_NOACTIVATE As Integer = 134217728

        Private Const WS_EX_TOOLWINDOW As Integer = 128

        Public Const SPI_SETWORKAREA As Integer = 47

        Public Const SPIF_SENDWININICHANGE As Integer = 2

        Public Const SPIF_UPDATEINIFILE As Integer = 1

        Public Const SPIF_change As Integer = 3

        Private SHIFT As Boolean

        Private CAPS As Boolean

        Private CTRL As Boolean

        Private ALT As Boolean

        Private WIN As Boolean

        Private tab As Boolean

        Private buttons As New List(Of ButtonLayout)

        Private oWidth As Integer

        Private oHeight As Integer

        Private o2Width As Integer

        Private o2Height As Integer

        Private isInit As Boolean

        Private uppercase As Boolean

        Private numLockShift As Boolean

        Private increment As Integer

        Private oLoc As Point

        Private isMoving As Boolean

        Private _startHidden As Boolean

        Private _hiddenStartupApplied As Boolean

        Private _allowProcessExit As Boolean

        Private Shared ReadOnly WM_LCARS_OSK_CMD As Integer

































































































































        Protected Overrides ReadOnly Property ShowWithoutActivation As Boolean
            Get
                Return True
            End Get
        End Property

        Protected Overrides ReadOnly Property CreateParams As CreateParams
            Get
                Dim lCreateParams = MyBase.CreateParams
                lCreateParams.ExStyle = lCreateParams.ExStyle Or &H8000000 Or &H80
                Return lCreateParams
            End Get
        End Property

        Shared Sub New()
            Dim lpString = "LCARS_OSK_INPUT"
            WM_LCARS_OSK_INPUT = RegisterWindowMessageA(lpString)
            lpString = "LCARS_OSK_CMD"
            WM_LCARS_OSK_CMD = RegisterWindowMessageA(lpString)
    End Sub

        <DllImport("user32", CharSet:=CharSet.Ansi, ExactSpelling:=True, SetLastError:=True)>
        Private Shared Function GetKeyState(nVirtKey As Long) As Integer
        End Function

        <DllImport("user32", CharSet:=CharSet.Ansi, ExactSpelling:=True, SetLastError:=True)>
        Private Shared Sub keybd_event(bVk As Byte, bScan As Byte, dwFlags As Long, dwExtraInfo As Long)
        End Sub

        <DllImport("user32", CharSet:=CharSet.Ansi, ExactSpelling:=True, SetLastError:=True)>
        Private Shared Function GetForegroundWindow() As IntPtr
        End Function

        <DllImport("user32", CharSet:=CharSet.Ansi, ExactSpelling:=True, SetLastError:=True)>
        Private Shared Function SetForegroundWindow(hWnd As IntPtr) As Boolean
        End Function

        <DllImport("user32", CharSet:=CharSet.Ansi, ExactSpelling:=True, SetLastError:=True)>
        Private Shared Function IsWindow(hWnd As IntPtr) As Boolean
        End Function

        <DllImport("user32", CharSet:=CharSet.Ansi, ExactSpelling:=True, SetLastError:=True)>
        Private Shared Function GetAncestor(hwnd As IntPtr, gaFlags As Integer) As IntPtr
        End Function

        <DllImport("user32", CharSet:=CharSet.Ansi, ExactSpelling:=True, SetLastError:=True)>
        Private Shared Function GetWindowThreadProcessId(hWnd As IntPtr, ByRef lpdwProcessId As Integer) As Integer
        End Function

        <DllImport("user32", CharSet:=CharSet.Ansi, EntryPoint:="GetWindowTextLengthA", ExactSpelling:=True, SetLastError:=True)>
        Private Shared Function GetWindowTextLength(hwnd As IntPtr) As Integer
        End Function

        <DllImport("user32", CharSet:=CharSet.Ansi, ExactSpelling:=True, SetLastError:=True)>
        Private Shared Function GetWindowTextA(hWnd As IntPtr, lpString As StringBuilder, nMaxCount As Integer) As Integer
        End Function

        <DllImport("user32", CharSet:=CharSet.Ansi, ExactSpelling:=True, SetLastError:=True)>
        Private Shared Function PostMessageA(hWnd As IntPtr, Msg As Integer, wParam As IntPtr, lParam As IntPtr) As Boolean
        End Function

        <DllImport("user32", CharSet:=CharSet.Ansi, ExactSpelling:=True, SetLastError:=True)>
        Private Shared Function FindWindowA(
        <MarshalAs(UnmanagedType.VBByRefStr)> ByRef lpClassName As String,
        <MarshalAs(UnmanagedType.VBByRefStr)> ByRef lpWindowName As String) As IntPtr
        End Function

        <DllImport("user32.dll", CharSet:=CharSet.Ansi, ExactSpelling:=True, SetLastError:=True)>
        Private Shared Function RegisterWindowMessageA(
        <MarshalAs(UnmanagedType.VBByRefStr)> ByRef lpString As String) As Integer
        End Function

        <DllImport("user32.dll")>
        Public Shared Function SetWindowLong(hWnd As IntPtr, nIndex As Integer, dwNewLong As IntPtr) As Integer
        End Function

        <DllImport("user32.dll", SetLastError:=True)>
        Public Shared Function GetWindowLong(hWnd As IntPtr, nIndex As Integer) As UInteger
        End Function

        <DllImport("user32.dll", SetLastError:=True)>
        Private Shared Function SetWindowPos(hWnd As IntPtr, hWndInsertAfter As IntPtr,
                                             X As Integer, Y As Integer, cx As Integer, cy As Integer,
                                             uFlags As UInteger) As Boolean
        End Function
        Private Const SWP_NOZORDER As UInteger = &H4UI
        Private Const SWP_NOACTIVATE As UInteger = &H10UI
        Private Const SWP_SHOWWINDOW As UInteger = &H40UI

        Private Sub ApplyNoActivateStyle()
            Dim windowLong As UInteger = GetWindowLong(MyBase.Handle, -20)
            Dim num As UInteger = windowLong Or &H8000000UI Or &H80UI
            SetWindowLong(MyBase.Handle, -20, New IntPtr(CLng(num)))
    End Sub

        <DllImport("user32", CharSet:=CharSet.Ansi, EntryPoint:="SystemParametersInfoA", ExactSpelling:=True, SetLastError:=True)>
        Public Shared Function SystemParametersInfo(uAction As Integer, uParam As Integer, lpvParam As IntPtr, fuWinIni As Integer) As Integer
        End Function

        Public Sub New()
            _lastInputTarget = IntPtr.Zero
            SHIFT = False
            CAPS = False
            CTRL = False
            ALT = False
            WIN = False
            tab = False
            buttons = New List(Of ButtonLayout)()
            isInit = False
            uppercase = False
            numLockShift = False
            increment = 20
            isMoving = False
            _startHidden = False
            _hiddenStartupApplied = False
            _allowProcessExit = False
            InitializeComponent()
            AutoScaleMode = AutoScaleMode.None
            ' Designer ClientSize 1350x542 is the capture baseline (original OSK).
            ApplyOskTitleStamp()
            Dim commandLineArgs As String() = Environment.GetCommandLineArgs()
            For Each arg As String In commandLineArgs
                Dim a As String = arg.Trim()
                If String.Equals(a, "--hidden", StringComparison.OrdinalIgnoreCase) OrElse String.Equals(a, "/hidden", StringComparison.OrdinalIgnoreCase) Then
                    _startHidden = True
                    Exit For
                End If
            Next
        End Sub

        Private Sub ApplyOskTitleStamp()
            Try
                If sbTitle IsNot Nothing Then
                    sbTitle.ButtonText = "MOVE KEYPAD"
                    sbTitle.Text = "MOVE KEYPAD"
                End If
            Catch
                End Try
        End Sub

        Protected Overrides Sub SetVisibleCore(value As Boolean)
            If Not IsHandleCreated Then
                CreateHandle()
            End If
            If _startHidden AndAlso Not _hiddenStartupApplied Then
                _hiddenStartupApplied = True
                ' Stay hidden for prewarm. Do NOT run heavy init inside SetVisibleCore —
                ' that re-enters WinForms during Application.Run startup and can leave the
                ' form unable to become visible later.
                MyBase.SetVisibleCore(False)
                If Not _hiddenInitQueued Then
                    _hiddenInitQueued = True
                    Try
                        BeginInvoke(New MethodInvoker(Sub() PerformStartupInit("hidden-prewarm")))
                    Catch
                        ' Handle not ready to invoke yet — Load/show will init.
                        _hiddenInitQueued = False
                    End Try
        End If
                Return
            End If
            MyBase.SetVisibleCore(value)
    End Sub

        Protected Overrides Sub WndProc(ByRef m As Message)
            If WM_LCARS_OSK_CMD <> 0 AndAlso m.Msg = WM_LCARS_OSK_CMD Then
                If m.WParam.ToInt32() <> 0 Then
                    ' Keep this handler FAST — LCARS uses SendMessage and then ShowWindow.
                    ' Heavy scale here deadlocks/races and the keyboard never appears.
                    Try
                        If WindowState = FormWindowState.Minimized Then
                            WindowState = FormWindowState.Normal
                        End If
                        PerformStartupInit("show-cmd")
                        Try
                            Opacity = 1.0R
                        Catch
                        End Try
                        ClampOskOnScreen("show-cmd")
                        Show()
                        Visible = True
                        TopMost = True
                        BringToFront()
                        ApplyNoActivateStyle()
                        ApplyOskTitleStamp()
                        WriteScaleLog("show-cmd visible=" & Visible.ToString() &
                                      " loc=" & Left.ToString() & "," & Top.ToString() &
                                      " size=" & Width.ToString() & "x" & Height.ToString())
                Catch ex As Exception
                        WriteScaleLog("show-cmd EX " & ex.ToString())
                End Try
                Else
                    Hide()
            End If
                m.Result = New IntPtr(1)
                Return
            Else
                MyBase.WndProc(m)
        End If
        End Sub

        Private Sub frmKeyboard_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
            If Not _allowProcessExit AndAlso e.CloseReason <> CloseReason.WindowsShutDown AndAlso e.CloseReason <> CloseReason.TaskManagerClosing AndAlso e.CloseReason <> CloseReason.ApplicationExitCall Then
                e.Cancel = True
                Hide()
            End If
    End Sub

        Private Sub frmKeyboard_Load(sender As Object, e As EventArgs) Handles MyBase.Load
            PerformStartupInit("Load")
            If Not _startHidden Then
                ClampOskOnScreen("Load")
                Show()
            End If
        End Sub

        ''' <summary>Keep the keyboard on the working area — bad saved Location made it look like it "didn't open".</summary>
        Private Sub ClampOskOnScreen(reason As String)
            Try
                Dim scr As Screen = Screen.FromPoint(New Point(Left + Width \ 2, Top + Height \ 2))
                If Not scr.Bounds.Contains(New Point(Left + 8, Top + 8)) Then
                    scr = Screen.FromPoint(Cursor.Position)
                End If
                Dim wa As Rectangle = scr.WorkingArea
                Dim w As Integer = Math.Max(200, Math.Min(Width, wa.Width - 8))
                Dim h As Integer = Math.Max(110, Math.Min(Height, wa.Height - 8))
                Dim x As Integer = Left
                Dim y As Integer = Top
                If x + w > wa.Right Then x = wa.Right - w
                If y + h > wa.Bottom Then y = wa.Bottom - h
                If x < wa.Left Then x = wa.Left + 4
                If y < wa.Top Then y = wa.Top + 4
                If x <> Left OrElse y <> Top OrElse w <> Width OrElse h <> Height Then
                    WriteScaleLog(reason & " clamp-on-screen " & Left.ToString() & "," & Top.ToString() &
                                  " " & Width.ToString() & "x" & Height.ToString() & " -> " &
                                  x.ToString() & "," & y.ToString() & " " & w.ToString() & "x" & h.ToString())
                    Left = x
                    Top = y
                    If w <> Width OrElse h <> Height Then
                        Width = w
                        Height = h
                    End If
                End If
                Catch ex As Exception
                WriteScaleLog(reason & " clamp EX " & ex.Message)
                End Try
        End Sub

        ''' <summary>
        ''' Original OSK init: capture design panel/button sizes, apply saved or default Size
        ''' (0.75*screenWidth x 250), then scale keys by Panel1 ratios. Keeps MOVE/SNAP/CLOSE/etc.
        ''' Safe to call multiple times; only the first call does the work.
        ''' </summary>
        Private Sub PerformStartupInit(reason As String)
            If _startupInitDone Then
                WriteScaleLog(reason & " init-skip alreadyDone build=" & OskBuildId)
                Return
            End If
            _startupInitDone = True
            Try
                Dim exePath As String = ""
                Try
                    exePath = Process.GetCurrentProcess().MainModule.FileName
                Catch
                    exePath = Application.ExecutablePath
                End Try
                WriteScaleLog(reason & " enter pid=" & Process.GetCurrentProcess().Id.ToString() &
                              " exe=" & exePath & " build=" & OskBuildId)

                AutoScaleMode = AutoScaleMode.None
                Try
                    Me.MaximumSize = Size.Empty
                    Me.MinimumSize = New Size(200, 110)
                Catch
                End Try

                ' Original designer: dock-like anchors so panels resize with the form.
                ' Top|Left-only (from compact-size experiments) left keys at design size in a small window.
                SplitContainer1.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right

                ' Original: capture live panel sizes at design ClientSize before applying saved size.
                oWidth = SplitContainer1.Panel1.Width
                oHeight = SplitContainer1.Panel1.Height
                o2Width = SplitContainer1.Panel2.Width
                o2Height = SplitContainer1.Panel2.Height
                If oWidth < 100 OrElse oHeight < 50 Then
                    ' Fallback if capture happened after a prior shrink.
                    oWidth = 1048
                    oHeight = 476
                    o2Width = 281
                    o2Height = 476
                End If
                ApplyNoActivateStyle()
                CaptureButtonLayouts()

                Dim screenObj As Screen = Screen.FromPoint(New Point(Me.Left, Me.Top))

                ' Always clear Size when schema changes so compact/poisoned values cannot stick.
                Dim prevSchema As String = GetSetting("x32_OSK", "Settings", "SizeSchema", "0")
                If Not String.Equals(prevSchema, CurrentSizeSchema, StringComparison.Ordinal) Then
                    Try
                        DeleteSetting("x32_OSK", "Settings", "Size")
                    Catch
                End Try
            End If

                Dim sizeDefault As String = CStr(screenObj.Bounds.Width * 0.75R) & ", " & "250"
                Dim tmpStr As String = GetSetting("x32_OSK", "Settings", "Size", sizeDefault)
                Dim sizeParts() As String = tmpStr.Split(","c)
                Dim newW As Integer = CInt(Math.Round(screenObj.Bounds.Width * 0.75R))
                Dim newH As Integer = 250
                If sizeParts.Length >= 2 Then
                    Integer.TryParse(sizeParts(0).Trim(), newW)
                    Integer.TryParse(sizeParts(1).Trim(), newH)
        End If
                ' Reject leftover compact sizes even if schema write failed previously.
                If newW < CInt(screenObj.Bounds.Width * 0.4R) OrElse newH < 180 Then
                    newW = CInt(Math.Round(screenObj.Bounds.Width * 0.75R))
                    newH = 250
                End If
                If newW < 200 Then newW = 200
                If newH < 110 Then newH = 110
                Me.Width = newW
                Me.Height = newH
                Me.PerformLayout()
                SplitContainer1.PerformLayout()

                Dim locDefault As String = CStr(screenObj.Bounds.Left + screenObj.Bounds.Width * 0.125R) & ", " &
                    CStr(screenObj.Bounds.Height - Me.Height)
                tmpStr = GetSetting("x32_OSK", "Settings", "Location", locDefault)
                Dim locParts() As String = tmpStr.Split(","c)
                Dim newLeft As Integer = screenObj.Bounds.Left + CInt(Math.Round(screenObj.Bounds.Width * 0.125R))
                Dim newTop As Integer = screenObj.Bounds.Height - Me.Height
                If locParts.Length >= 2 Then
                    Integer.TryParse(locParts(0).Trim(), newLeft)
                    Integer.TryParse(locParts(1).Trim(), newTop)
                End If
                Me.Left = newLeft
                Me.Top = newTop
                ClampOskOnScreen(reason)
                Me.PerformLayout()
                SplitContainer1.PerformLayout()

                isInit = True
                ApplyOskTitleStamp()
                _snapEnabled = String.Equals(GetSetting("x32_OSK", "Settings", "SnapEnabled", "1"), "1", StringComparison.Ordinal)
                ScaleButtonsFromPanelRatios(reason)
                EnsureCloseKeyChrome()
                PositionCloseButton()
                SaveSetting("x32_OSK", "Settings", "Size", Me.Width.ToString() & ", " & Me.Height.ToString())
                SaveSetting("x32_OSK", "Settings", "SizeSchema", CurrentSizeSchema)
                Try
            My.Settings.Save()
                Catch
                End Try
                Try
                    UserButtons.Hide()
                Catch
                End Try
                sbFn.Clickable = False
                sbFn.Color = LCARScolorStyles.FunctionUnavailable
                Try
                    loadFNbuttons()
                Catch exFn As Exception
                    WriteScaleLog(reason & " loadFNbuttons EX " & exFn.Message)
                End Try
                UpdateElevateButtonState()
                If GetKeyState(144L) = 1 Then
                    btnnumlock.Color = LCARScolorStyles.PrimaryFunction
                Else
                    btnnumlock.Color = LCARScolorStyles.SystemFunction
            End If
                If GetKeyState(145L) = 1 Then
                    btnScrollLock.Color = LCARScolorStyles.PrimaryFunction
                Else
                    btnScrollLock.Color = LCARScolorStyles.SystemFunction
        End If
                HideDedicatedPageKeys()
                ApplyNumLockPadLabels()
                If My.Settings.Mode Then
                    Try
                        sbNum_Click(Me, EventArgs.Empty)
                    Catch
                    End Try
                End If
                Try
                    Dim stampDir As String = IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LCARS x32")
                    If Not IO.Directory.Exists(stampDir) Then IO.Directory.CreateDirectory(stampDir)
                    IO.File.WriteAllText(IO.Path.Combine(stampDir, "osk-running-build.txt"), OskBuildId & " " & exePath)
                Catch
                End Try
                WriteScaleLog(reason & " exit ok size=" & Me.Width.ToString() & "x" & Me.Height.ToString() &
                              " build=" & OskBuildId & " keys=" & buttons.Count.ToString() &
                              " oPanel1=" & oWidth.ToString() & "x" & oHeight.ToString())
            Catch ex As Exception
                WriteScaleLog(reason & " EX " & ex.ToString())
                _startupInitDone = False
            End Try
        End Sub

        Private Sub CaptureButtonLayouts()
            buttons.Clear()
            For Each myButton As Control In SplitContainer1.Panel1.Controls
                Dim item As New ButtonLayout()
                item.Left = myButton.Left
                item.Top = myButton.Top
                item.Width = myButton.Width
                item.Height = myButton.Height
                item.Control = myButton
                item.InPanel2 = False
                buttons.Add(item)
            Next
            For Each myButton As Control In SplitContainer1.Panel2.Controls
                Dim item As New ButtonLayout()
                item.Left = myButton.Left
                item.Top = myButton.Top
                item.Width = myButton.Width
                item.Height = myButton.Height
                item.Control = myButton
                item.InPanel2 = True
                buttons.Add(item)
            Next
            WriteScaleLog("capture buttons=" & buttons.Count.ToString() &
                          " oPanel1=" & oWidth.ToString() & "x" & oHeight.ToString())
    End Sub

    Private Sub WriteScaleLog(line As String)
        Try
            Dim dir As String = IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LCARS x32")
            If Not IO.Directory.Exists(dir) Then IO.Directory.CreateDirectory(dir)
            IO.File.AppendAllText(IO.Path.Combine(dir, "osk-scale.log"), DateTime.Now.ToString("s") & " " & line & Environment.NewLine)
        Catch
        End Try
        Try
            IO.File.AppendAllText(IO.Path.Combine(IO.Path.GetTempPath(), "lcars-osk-scale.log"), DateTime.Now.ToString("s") & " " & line & Environment.NewLine)
        Catch
        End Try
    End Sub

    Private Sub frmKeyboard_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        Try
            PerformStartupInit("shown")
            Try
                Opacity = 1.0R
            Catch
            End Try
            ClampOskOnScreen("shown")
                Catch ex As Exception
            WriteScaleLog("shown EX " & ex.Message)
                End Try
    End Sub

    ''' <summary>SNAP sits beside NUM LOCK on the numpad (not far-right chrome — that clipped).</summary>
    Private Sub PositionCloseButton()
        UpdateSnapButtonChrome()
    End Sub

    Private Sub UpdateSnapButtonChrome()
        If StandardButton1 Is Nothing OrElse StandardButton1.IsDisposed Then Return
        If btnnumlock Is Nothing OrElse btnnumlock.IsDisposed Then Return

        ' Keep SNAP on the numpad panel so it scales/moves with NUM LOCK.
        If Not Object.ReferenceEquals(StandardButton1.Parent, SplitContainer1.Panel2) Then
            Try
                If StandardButton1.Parent IsNot Nothing Then
                    StandardButton1.Parent.Controls.Remove(StandardButton1)
                End If
                SplitContainer1.Panel2.Controls.Add(StandardButton1)
            Catch
            End Try
        End If

        StandardButton1.Visible = True
        StandardButton1.Anchor = AnchorStyles.Top Or AnchorStyles.Left
        StandardButton1.ButtonStyle = StandardButton.LCARSbuttonStyles.RoundedSquare
        StandardButton1.ButtonTextAlign = ContentAlignment.MiddleCenter
        Dim onText As String = If(_snapEnabled, "SNAP ON", "SNAP OFF")
        StandardButton1.ButtonText = onText
        StandardButton1.Text = onText
        StandardButton1.Color = If(_snapEnabled, LCARScolorStyles.PrimaryFunction, LCARScolorStyles.FunctionUnavailable)

        ' Immediately left of NUM LOCK, matching its height; width fits the label.
        Dim snapH As Integer = Math.Max(20, btnnumlock.Height)
        Dim snapW As Integer = Math.Max(56, Math.Min(btnnumlock.Width, 90))
        Dim gap As Integer = Math.Max(4, snapH \ 10)
        Dim snapLeft As Integer = btnnumlock.Left - gap - snapW
        If snapLeft < 2 Then snapLeft = 2
        StandardButton1.SetBounds(snapLeft, btnnumlock.Top, snapW, snapH)
        StandardButton1.BringToFront()
    End Sub

    Private Sub EnsureCloseKeyChrome()
        If StandardButton2 Is Nothing OrElse StandardButton2.IsDisposed Then Return
        StandardButton2.ButtonText = "CLOSE"
        StandardButton2.Text = "CLOSE"
        StandardButton2.Data = "CLOSE"
        StandardButton2.Data2 = "CLOSE"
        StandardButton2.Color = LCARScolorStyles.MiscFunction
        StandardButton2.ButtonStyle = StandardButton.LCARSbuttonStyles.RoundedSquare
        StandardButton2.ButtonTextAlign = ContentAlignment.MiddleCenter
    End Sub

    Private Function BuildSnapCandidates() As List(Of Point)
        Dim scr As Screen = Screen.FromControl(Me)
        Dim wa As Rectangle = scr.WorkingArea
        Dim margin As Integer = 6
        Dim maxX As Integer = Math.Max(wa.Left + margin, wa.Right - Width - margin)
        Dim maxY As Integer = Math.Max(wa.Top + margin, wa.Bottom - Height - margin)
        Dim spanX As Integer = Math.Max(0, maxX - (wa.Left + margin))
        Dim spanY As Integer = Math.Max(0, maxY - (wa.Top + margin))
        Dim candidates As New List(Of Point)
        For i As Integer = 0 To 4
            Dim x As Integer = wa.Left + margin + CInt(spanX * (i / 4.0))
            candidates.Add(New Point(x, maxY))
            candidates.Add(New Point(x, wa.Top + margin))
        Next
        For i As Integer = 0 To 4
            Dim y As Integer = wa.Top + margin + CInt(spanY * (i / 4.0))
            candidates.Add(New Point(wa.Left + margin, y))
            candidates.Add(New Point(maxX, y))
        Next
        Return candidates
    End Function

    Private Function GetNearestSnapPoint() As Point
        Dim candidates As List(Of Point) = BuildSnapCandidates()
        Dim best As Point = candidates(0)
        Dim bestDist As Double = Double.MaxValue
        Dim cur As New Point(Left, Top)
        For Each p As Point In candidates
            Dim dx As Double = p.X - cur.X
            Dim dy As Double = p.Y - cur.Y
            Dim d As Double = dx * dx + dy * dy
            If d < bestDist Then
                bestDist = d
                best = p
            End If
        Next
        Return best
    End Function

    ''' <summary>Snap OSK to nearest screen slot when SNAP is on.</summary>
    Private Sub SnapOskIfEnabled()
        If Not _snapEnabled OrElse _snapBusy OrElse Not isInit Then Return
        _snapBusy = True
        Try
            Dim best As Point = GetNearestSnapPoint()
            If Left <> best.X OrElse Top <> best.Y Then
                Left = best.X
                Top = best.Y
            End If
            SaveSetting("x32_OSK", "Settings", "Location", Left.ToString() & ", " & Top.ToString())
        Finally
            _snapBusy = False
        End Try
    End Sub

    ''' <summary>Original OSK scale: button positions/sizes = design layout * (Panel1 size / oWidth,oHeight).</summary>
    Private Sub ScaleButtonsFromPanelRatios(reason As String)
        If _scaleBusy Then
            WriteScaleLog(reason & " BUSY-skip")
            Return
            End If
        If buttons Is Nothing OrElse buttons.Count = 0 Then
            WriteScaleLog(reason & " EMPTY buttons — cannot scale without design capture")
            Return
        End If
        If oWidth <= 0 OrElse oHeight <= 0 Then
            WriteScaleLog(reason & " bad oPanel " & oWidth.ToString() & "x" & oHeight.ToString())
            Return
        End If

        _scaleBusy = True
        Try
            Dim newWidth As Double = SplitContainer1.Panel1.Width / CDbl(oWidth)
            Dim newHeight As Double = SplitContainer1.Panel1.Height / CDbl(oHeight)
            Dim sample As Control = buttons(0).Control
            Dim sampleBefore As String = If(sample Is Nothing, "null", sample.Name & sample.Bounds.ToString())

            SplitContainer1.Visible = False
            For Each btn As ButtonLayout In buttons
                If btn.Control Is Nothing OrElse btn.Control.IsDisposed Then Continue For
                btn.Control.Left = CInt(Math.Round(btn.Left * newWidth))
                btn.Control.Top = CInt(Math.Round(btn.Top * newHeight))
                btn.Control.Width = CInt(Math.Round(btn.Width * newWidth))
                btn.Control.Height = CInt(Math.Round(btn.Height * newHeight))
            Next
            SplitContainer1.Visible = True

            Dim sampleAfter As String = If(sample Is Nothing, "null", sample.Name & sample.Bounds.ToString())
            WriteScaleLog(reason & " build=" & OskBuildId &
                          " n=" & buttons.Count.ToString() &
                          " form=" & Width.ToString() & "x" & Height.ToString() &
                          " p1=" & SplitContainer1.Panel1.Width.ToString() & "x" & SplitContainer1.Panel1.Height.ToString() &
                          " sx=" & newWidth.ToString("0.###") & " sy=" & newHeight.ToString("0.###") &
                          " before=" & sampleBefore & " after=" & sampleAfter)
            PositionCloseButton()
            HideDedicatedPageKeys()
        Catch ex As Exception
            WriteScaleLog(reason & " EX " & ex.GetType().Name & " " & ex.Message)
            Try
                SplitContainer1.Visible = True
            Catch
            End Try
        Finally
            _scaleBusy = False
        End Try
    End Sub

    Private Sub LayoutKeyboardSurface()
        ScaleButtonsFromPanelRatios("layout")
    End Sub

    Private Sub ScaleKeyboardButtons()
        ScaleButtonsFromPanelRatios("scale")
    End Sub

        Public Sub loadFNbuttons()
            If Operators.CompareString(My.Settings.FN1Name, "", TextCompare:=False) = 0 Then
                sbFn1.ButtonText = "FN1"
            ElseIf Operators.CompareString(My.Settings.FN1Name, "", TextCompare:=False) <> 0 Then
                sbFn1.ButtonText = My.Settings.FN1Name
        End If
            If Operators.CompareString(My.Settings.FN2Name, "", TextCompare:=False) = 0 Then
                sbFn2.ButtonText = "FN2"
            ElseIf Operators.CompareString(My.Settings.FN2Name, "", TextCompare:=False) <> 0 Then
                sbFn2.ButtonText = My.Settings.FN2Name
            End If
            If Operators.CompareString(My.Settings.FN3Name, "", TextCompare:=False) = 0 Then
                sbFN3.ButtonText = "FN3"
            ElseIf Operators.CompareString(My.Settings.FN3Name, "", TextCompare:=False) <> 0 Then
                sbFN3.ButtonText = My.Settings.FN3Name
            End If
            If Operators.CompareString(My.Settings.FN4Name, "", TextCompare:=False) = 0 Then
                sbFn4.ButtonText = "FN4"
            ElseIf Operators.CompareString(My.Settings.FN4Name, "", TextCompare:=False) <> 0 Then
                sbFn4.ButtonText = My.Settings.FN4Name
            End If
            If Operators.CompareString(My.Settings.FN5Name, "", TextCompare:=False) = 0 Then
                sbFn5.ButtonText = "FN5"
            ElseIf Operators.CompareString(My.Settings.FN5Name, "", TextCompare:=False) <> 0 Then
                sbFn5.ButtonText = My.Settings.FN5Name
            End If
            If Operators.CompareString(My.Settings.FN6Name, "", TextCompare:=False) = 0 Then
                sbFn6.ButtonText = "FN6"
            ElseIf Operators.CompareString(My.Settings.FN6Name, "", TextCompare:=False) <> 0 Then
                sbFn6.ButtonText = My.Settings.FN6Name
            End If
            If Operators.CompareString(My.Settings.FN7Name, "", TextCompare:=False) = 0 Then
                sbFn7.ButtonText = "FN7"
            ElseIf Operators.CompareString(My.Settings.FN7Name, "", TextCompare:=False) <> 0 Then
                sbFn7.ButtonText = My.Settings.FN7Name
            End If
            If Operators.CompareString(My.Settings.FN8Name, "", TextCompare:=False) = 0 Then
                sbFn8.ButtonText = "FN8"
            ElseIf Operators.CompareString(My.Settings.FN8Name, "", TextCompare:=False) <> 0 Then
                sbFn8.ButtonText = My.Settings.FN8Name
            End If
            If Operators.CompareString(My.Settings.FN9Name, "", TextCompare:=False) = 0 Then
                sbFn9.ButtonText = "FN9"
            ElseIf Operators.CompareString(My.Settings.FN9Name, "", TextCompare:=False) <> 0 Then
                sbFn9.ButtonText = My.Settings.FN9Name
            End If
            If Operators.CompareString(My.Settings.FN10Name, "", TextCompare:=False) = 0 Then
                sbFn10.ButtonText = "FN10"
            ElseIf Operators.CompareString(My.Settings.FN10Name, "", TextCompare:=False) <> 0 Then
                sbFn10.ButtonText = My.Settings.FN10Name
            End If
            If Operators.CompareString(My.Settings.FN11Name, "", TextCompare:=False) = 0 Then
                sbFn11.ButtonText = "FN11"
            ElseIf Operators.CompareString(My.Settings.FN11Name, "", TextCompare:=False) <> 0 Then
                sbFn11.ButtonText = My.Settings.FN11Name
            End If
            If Operators.CompareString(My.Settings.FN12Name, "", TextCompare:=False) = 0 Then
                sbFn12.ButtonText = "FN12"
            ElseIf Operators.CompareString(My.Settings.FN12Name, "", TextCompare:=False) <> 0 Then
                sbFn12.ButtonText = My.Settings.FN12Name
            End If
        End Sub

        Public Sub frmKeyboard_ResizeEnd(sender As Object, e As EventArgs) Handles Me.ResizeEnd
            ScaleButtonsFromPanelRatios("resize-end")
            SaveSetting("x32_OSK", "Settings", "Size", Conversions.ToString(Width) & ", " & Conversions.ToString(Height))
            My.Settings.Save()
    End Sub

        Private Sub StandardKey_Click(sender As Object, e As EventArgs) Handles sb1.Click, sb2.Click, sb3.Click, sb4.Click, sb5.Click, sb6.Click, sb7.Click, sb8.Click, sb9.Click, sb0.Click, sbSpace.Click, sbTilde.Click, sbMinus.Click, sbEqual.Click, sbBackSlash.Click, sbForwardSlash.Click, sbLBracket.Click, sbRBracket.Click, sbSemiColon.Click, sbQuote.Click, sbComma.Click, sbPeriod.Click, sbBack.Click, sbEnter.Click, sbESC.Click, sbF1.Click, sbF2.Click, sbF3.Click, sbF4.Click, sbF5.Click, sbF6.Click, sbF7.Click, sbF8.Click, sbF9.Click, sbF10.Click, sbF11.Click, sbF12.Click, sbDEL.Click, sbREnter.Click
            Dim text = ""
            Dim text2 = ""
            Try
                text2 = Conversions.ToString(NewLateBinding.LateGet(sender, Nothing, "buttontext", New Object(-1) {}, Nothing, Nothing, Nothing))
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                text2 = ""
                Call ProjectData.ClearProjectError()
            End Try
            If String.IsNullOrEmpty(text2) Then
                Try
                    text2 = Conversions.ToString(NewLateBinding.LateGet(sender, Nothing, "ButtonText", New Object(-1) {}, Nothing, Nothing, Nothing))
                Catch projectError2 As Exception
                    ProjectData.SetProjectError(projectError2)
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Dim num = ResolveNavigationVk(text2)
            If num > 0 AndAlso Not CTRL AndAlso Not ALT AndAlso Not SHIFT Then
                OskSendVirtualKey(num)
                Return
            End If
            If CTRL Then
                text += "^"
            End If
            If ALT Then
                text += "%"
            End If
            If SHIFT Then
                text += "+"
            End If
            Try
                Dim text3 = ResolveSendKeysToken(text2)
                If Operators.CompareString(text, "", TextCompare:=False) <> 0 Then
                    OskSendKeys(text & "{" & text3 & "}")
                Else
                    OskSendKeys("{" & text3 & "}")
        End If
            Catch projectError3 As Exception
                ProjectData.SetProjectError(projectError3)
                If num > 0 Then
                    OskSendVirtualKey(num)
                Else
                    OskSendKeys(text2)
        End If
                Call ProjectData.ClearProjectError()
            End Try
            If CTRL Then
                Ctrl_Click(RuntimeHelpers.GetObjectValue(sender), e)
            End If
            If SHIFT Then
                Shift_Click(RuntimeHelpers.GetObjectValue(sender), e)
            End If
    End Sub

        Private Function ResolveNavigationVk(buttonText As String) As Integer
            'Discarded unreachable code: IL_0056, IL_0090, IL_00b6, IL_00dc, IL_00f4, IL_00fb
            If String.IsNullOrEmpty(buttonText) Then
                Return 0
            End If
            Select Case buttonText.Trim().ToUpperInvariant()
                Case "BACKSPACE", "BKSP", "BS"
                    Return 8
                Case "ENTER", "RETURN", "ENT"
                    Return 13
                Case "ESC", "ESCAPE"
                    Return 27
                Case "DEL", "DELETE"
                    Return 46
                Case "TAB"
                    Return 9
                Case Else
                    Return 0
            End Select
        End Function

        Private Function ResolveSendKeysToken(buttonText As String) As String
            'Discarded unreachable code: IL_004d, IL_008a, IL_00b3, IL_00dc, IL_00e8
            If String.IsNullOrEmpty(buttonText) Then
                Return "BS"
        End If
            Select Case buttonText.Trim().ToUpperInvariant()
                Case "BACKSPACE", "BKSP"
                    Return "BS"
                Case "ENTER", "RETURN", "ENT"
                    Return "ENTER"
                Case "ESC", "ESCAPE"
                    Return "ESC"
                Case "DEL", "DELETE"
                    Return "DEL"
                Case Else
                    Return buttonText.Trim()
            End Select
        End Function

        Private Sub InjectVirtualKey(vk As Byte)
            OskSendVirtualKey(vk)
        End Sub

        Private Sub TrackInputTarget()
            Try
                Dim foregroundWindow As IntPtr = GetForegroundWindow()
                If Not foregroundWindow = IntPtr.Zero AndAlso Not foregroundWindow = Handle Then
                    Dim intPtr = GetAncestor(foregroundWindow, 2)
                    If intPtr = IntPtr.Zero Then
                        intPtr = foregroundWindow
        End If
                    If Not intPtr = Handle AndAlso Not IsIgnoredInputProcess(intPtr) Then
                        _lastInputTarget = intPtr
                    End If
                End If
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
    End Sub

        Private Function IsIgnoredInputProcess(hwnd As IntPtr) As Boolean
            Dim lpdwProcessId = 0
            GetWindowThreadProcessId(hwnd, lpdwProcessId)
            If lpdwProcessId <= 0 Then
                Return False
            End If
            Try
                Dim process = Process.GetProcessById(lpdwProcessId)
                Select Case process.ProcessName.ToLowerInvariant()
                    Case "onscreenkeyboard", "lcarsmain", "lcarsupdate", "runinstallscript", "lcarsshutdown"
                        Return True
                End Select
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
            Return False
        End Function

        Private Function GetInputTargetHwnd() As IntPtr
            TrackInputTarget()
            If _lastInputTarget <> System.IntPtr.Zero AndAlso IsWindow(_lastInputTarget) Then
                Return _lastInputTarget
            End If
            Dim lpClassName As String = Nothing
            Dim lpWindowName = "LCARS TERMINAL"
            Dim foundHwnd As IntPtr = FindWindowA(lpClassName, lpWindowName)
            If foundHwnd <> System.IntPtr.Zero AndAlso IsWindow(foundHwnd) Then
                Return foundHwnd
            End If
            Return System.IntPtr.Zero
        End Function

        Private Function IsLcarsTerminalHwnd(hwnd As IntPtr) As Boolean
            'Discarded unreachable code: IL_0083, IL_0094, IL_00a6, IL_00ad
            If hwnd = System.IntPtr.Zero OrElse Not IsWindow(hwnd) Then
                Return False
            End If
            Try
                Dim windowTextLength As Integer = GetWindowTextLength(hwnd)
                If windowTextLength > 0 Then
                    Dim stringBuilder As New StringBuilder(windowTextLength + 1)
                    GetWindowTextA(hwnd, stringBuilder, stringBuilder.Capacity)
                    If stringBuilder.ToString().IndexOf("LCARS TERMINAL", StringComparison.OrdinalIgnoreCase) >= 0 Then
                        Return True
                    End If
                End If
                Dim lpdwProcessId = 0
                GetWindowThreadProcessId(hwnd, lpdwProcessId)
                Dim process = Process.GetProcessById(lpdwProcessId)
                Return String.Equals(process.ProcessName, "LCARSTerminal", StringComparison.OrdinalIgnoreCase)
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Dim result = False
                Call ProjectData.ClearProjectError()
                Return result
            End Try
        End Function

        Private Function TryPostToTerminal(target As IntPtr, kind As Integer, payload As Integer) As Boolean
            'Discarded unreachable code: IL_0043, IL_0054, IL_005b
            If WM_LCARS_OSK_INPUT = 0 Then
                Return False
            End If
            If Not IsLcarsTerminalHwnd(target) Then
                Return False
        End If
            Try
                Dim wM_LCARS_OSK_INPUT = frmKeyboard.WM_LCARS_OSK_INPUT
                Dim wParam As IntPtr = New IntPtr(kind)
                Dim lParam As IntPtr = New IntPtr(payload And &HFFFF)
                Return PostMessageA(target, wM_LCARS_OSK_INPUT, wParam, lParam)
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Dim result = False
                Call ProjectData.ClearProjectError()
                Return result
            End Try
        End Function

        Private Sub OskSendVirtualKey(vk As Integer)
            Dim inputTargetHwnd As IntPtr = GetInputTargetHwnd()
            If TryPostToTerminal(inputTargetHwnd, 2, vk) Then
                Return
            End If
            TryActivateTarget(inputTargetHwnd)
            Try
                keybd_event(vk And &HFF, 0, 0L, 0L)
                keybd_event(vk And &HFF, 0, 2L, 0L)
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
    End Sub

        Private Sub OskSendKeys(keys As String)
            If Not String.IsNullOrEmpty(keys) Then
                Dim inputTargetHwnd As IntPtr = GetInputTargetHwnd()
                If Not IsLcarsTerminalHwnd(inputTargetHwnd) OrElse Not TryDeliverSendKeysToTerminal(inputTargetHwnd, keys) Then
                    TryActivateTarget(inputTargetHwnd)
                    SendKeys.Send(keys)
        End If
            End If
        End Sub

        Private Function TryDeliverSendKeysToTerminal(target As IntPtr, keys As String) As Boolean
            Dim text = keys
            Dim text2 = ""
            While text.Length > 0 AndAlso "^%+".IndexOf(text(0)) >= 0
                text2 += Conversions.ToString(text(0))
                text = text.Substring(1)
            End While
            If text.StartsWith("{", StringComparison.Ordinal) AndAlso text.EndsWith("}", StringComparison.Ordinal) AndAlso text.Length >= 3 Then
                Dim text3 As String = text.Substring(1, text.Length - 2).Trim()
                Dim text4 As String = text3.ToUpperInvariant()
                Dim num = ResolveNavigationVk(text4)
                If num = 0 Then
                    Select Case text4
                        Case "UP"
                            num = 38
                        Case "DOWN"
                            num = 40
                        Case "LEFT"
                            num = 37
                        Case "RIGHT"
                            num = 39
                        Case "HOME"
                            num = 36
                        Case "END"
                            num = 35
                        Case "PGUP"
                            num = 33
                        Case "PGDN"
                            num = 34
                        Case "INSERT"
                            num = 45
                        Case "DEL", "DELETE"
                            num = 46
                    End Select
        End If
                If num > 0 AndAlso text2.Length = 0 Then
                    Return TryPostToTerminal(target, 2, num)
                End If
                If text3.Length = 1 AndAlso text2.Length = 0 Then
                    Return Me.TryPostToTerminal(target, 1, AscW(text3(0)))
                End If
                Return False
            End If
            If text.Length = 1 AndAlso text2.Length = 0 Then
                Return Me.TryPostToTerminal(target, 1, AscW(text(0)))
            End If
            Return False
        End Function

        Private Sub TryActivateTarget(target As IntPtr)
            If target = IntPtr.Zero OrElse Not IsWindow(target) Then
                Return
            End If
            Try
                SetForegroundWindow(target)
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
    End Sub

        Private Sub Shift_Click(sender As Object, e As EventArgs) Handles sbLShift.Click, sbRShift.Click
            SHIFT = Not SHIFT
            If SHIFT Then
                If Not CAPS Then
                    Dim enumerator As IEnumerator = Nothing
                    Try
                        enumerator = SplitContainer1.Panel1.Controls.GetEnumerator()
                        While enumerator.MoveNext()
                            Dim lCARSbuttonClass = CType(enumerator.Current, LCARSbuttonClass)
                            lCARSbuttonClass.ButtonText = Conversions.ToString(lCARSbuttonClass.Data2)
                        End While

                    Finally
                        If TypeOf enumerator Is IDisposable Then
                            TryCast(enumerator, IDisposable).Dispose()
                        End If
                    End Try
                Else
                    Dim enumerator2 As IEnumerator = Nothing
                    Try
                        enumerator2 = SplitContainer1.Panel1.Controls.GetEnumerator()
                        While enumerator2.MoveNext()
                            Dim lCARSbuttonClass2 = CType(enumerator2.Current, LCARSbuttonClass)
                            If Not (lCARSbuttonClass2.ButtonText.Length = 1 And Char.IsLetter(Conversions.ToChar(lCARSbuttonClass2.ButtonText))) Then
                                lCARSbuttonClass2.ButtonText = Conversions.ToString(lCARSbuttonClass2.Data2)
        End If
                        End While

                    Finally
                        If TypeOf enumerator2 Is IDisposable Then
                            TryCast(enumerator2, IDisposable).Dispose()
        End If
                    End Try
                End If
                sbLShift.Color = LCARScolorStyles.PrimaryFunction
                sbRShift.Color = LCARScolorStyles.PrimaryFunction
            Else
                If Not CAPS Then
                    Dim enumerator3 As IEnumerator = Nothing
                    Try
                        enumerator3 = SplitContainer1.Panel1.Controls.GetEnumerator()
                        While enumerator3.MoveNext()
                            Dim lCARSbuttonClass3 = CType(enumerator3.Current, LCARSbuttonClass)
                            lCARSbuttonClass3.ButtonText = Conversions.ToString(lCARSbuttonClass3.Data)
                        End While

                    Finally
                        If TypeOf enumerator3 Is IDisposable Then
                            TryCast(enumerator3, IDisposable).Dispose()
                        End If
                    End Try
                Else
                    Dim enumerator4 As IEnumerator = Nothing
                    Try
                        enumerator4 = SplitContainer1.Panel1.Controls.GetEnumerator()
                        While enumerator4.MoveNext()
                            Dim lCARSbuttonClass4 = CType(enumerator4.Current, LCARSbuttonClass)
                            If Not (lCARSbuttonClass4.ButtonText.Length = 1 And Char.IsLetter(Conversions.ToChar(lCARSbuttonClass4.ButtonText))) Then
                                lCARSbuttonClass4.ButtonText = Conversions.ToString(lCARSbuttonClass4.Data)
        End If
                        End While

                    Finally
                        If TypeOf enumerator4 Is IDisposable Then
                            TryCast(enumerator4, IDisposable).Dispose()
                        End If
                    End Try
                End If
                sbLShift.Color = LCARScolorStyles.SystemFunction
                sbRShift.Color = LCARScolorStyles.SystemFunction
            End If
            ' Numpad labels follow Num Lock only (not Shift).
            ApplyNumLockPadLabels()
        End Sub

        Private Sub sbCaps_Click(sender As Object, e As EventArgs) Handles sbCaps.Click
            If sbCaps.Color = LCARScolorStyles.SystemFunction Then
                keybd_event(20, 0, 1L, 0L)
                keybd_event(20, 0, 3L, 0L)
                sbCaps.Color = LCARScolorStyles.PrimaryFunction
            ElseIf sbCaps.Color = LCARScolorStyles.PrimaryFunction Then
                keybd_event(20, 0, 1L, 0L)
                keybd_event(20, 0, 3L, 0L)
                sbCaps.Color = LCARScolorStyles.SystemFunction
        End If
        End Sub

        Private Sub sbTab_Click(sender As Object, e As EventArgs) Handles sbTab.Click
            tab = Not tab
            keybd_event(9, 0, 1L, 0L)
            keybd_event(9, 0, 3L, 0L)
    End Sub

        Private Sub sbRwin_MouseDown(sender As Object, e As MouseEventArgs) Handles sbRwin.MouseDown, sbLWin.MouseDown
            Timer3.Enabled = True
        End Sub

        Private Sub sblwin_MouseUp(sender As Object, e As MouseEventArgs) Handles sbRwin.MouseUp, sbLWin.MouseUp
            'Discarded unreachable code: IL_003d, IL_0075
            WIN = Not WIN
            If Timer3.Enabled Then
                keybd_event(91, 0, 1L, 0L)
                keybd_event(91, 0, 3L, 0L)
            ElseIf WIN Then
                keybd_event(91, 0, 1L, 0L)
                sbLWin.Color = LCARScolorStyles.PrimaryFunction
                sbRwin.Color = LCARScolorStyles.PrimaryFunction
            ElseIf sbLWin.Color = LCARScolorStyles.PrimaryFunction Then
                keybd_event(42, 0, 1L, 0L)
                keybd_event(42, 0, 3L, 0L)
                keybd_event(91, 0, 3L, 0L)
                sbRwin.Color = LCARScolorStyles.SystemFunction
                sbLWin.Color = LCARScolorStyles.SystemFunction
        End If
        End Sub

        Private Sub Timer3_Tick(sender As Object, e As EventArgs) Handles Timer3.Tick
            Timer3.Enabled = False
        End Sub

        Private Sub Ctrl_Click(sender As Object, e As EventArgs) Handles sbRCtrl.Click, sbLCtrl.Click
            CTRL = Not CTRL
            If CTRL Then
                sbLCtrl.Color = LCARScolorStyles.PrimaryFunction
                sbRCtrl.Color = LCARScolorStyles.PrimaryFunction
            Else
                sbLCtrl.Color = LCARScolorStyles.SystemFunction
                sbRCtrl.Color = LCARScolorStyles.SystemFunction
        End If
    End Sub

        Private Sub Alt_Click(sender As Object, e As EventArgs) Handles sbLAlt.Click, sbRAlt.Click
            ALT = Not ALT
            If ALT Then
                keybd_event(18, 0, 1L, 0L)
                sbLAlt.Color = LCARScolorStyles.PrimaryFunction
                sbRAlt.Color = LCARScolorStyles.PrimaryFunction
                Timer2.Enabled = True
            Else
                keybd_event(18, 0, 3L, 0L)
                sbLAlt.Color = LCARScolorStyles.SystemFunction
                sbRAlt.Color = LCARScolorStyles.SystemFunction
        End If
        End Sub

        Private Sub frmKeyboard_Move(sender As Object, e As EventArgs) Handles Me.Move
            If Not isMoving AndAlso isInit Then
                SaveSetting("x32_OSK", "Settings", "Location", Conversions.ToString(Left) & ", " & Conversions.ToString(Top))
        End If
        End Sub

        Private Sub frmKeyboard_Resize(sender As Object, e As EventArgs) Handles Me.Resize
            ' Intentionally empty. Original OSK only scaled on ResizeEnd / explicit keypad calls.
            ' Scaling during Resize breaks SplitterDistance and leaves keys clipped.
    End Sub

        Private Sub StandardButton1_Click(sender As Object, e As EventArgs)
            ' Do not mutate design baseline oWidth (that broke scale factors).
            frmKeyboard_ResizeEnd(RuntimeHelpers.GetObjectValue(sender), e)
        End Sub

        Private Sub pnlKeyboard_Paint(sender As Object, e As PaintEventArgs)
        End Sub

        Private Sub sbDEL_Click(sender As Object, e As EventArgs)
            Dim standardButton As StandardButton = New StandardButton()
            standardButton.ButtonText = "DELETE"
            StandardKey_Click(standardButton, e)
        End Sub

        Private Sub StandardButton1_Click_1(sender As Object, e As EventArgs) Handles sbTitle.Click
        End Sub

        Private Sub sbTitle_MouseDown(sender As Object, e As MouseEventArgs) Handles sbTitle.MouseDown
            isMoving = True
            oLoc = New Point(MousePosition.X, MousePosition.Y)
        End Sub

        Private Sub sbTitle_MouseMove(sender As Object, e As MouseEventArgs) Handles sbTitle.MouseMove
            If MouseButtons = MouseButtons.Left AndAlso isMoving Then
                Left += MousePosition.X - oLoc.X
                Top += MousePosition.Y - oLoc.Y
                oLoc = New Point(MousePosition.X, MousePosition.Y)
                If _snapEnabled Then
                    Try
                        Dim preview As Point = GetNearestSnapPoint()
                        If preview <> _snapPreviewPoint Then
                            _snapPreviewPoint = preview
                            Dim dx As Integer = Math.Abs(Left - preview.X)
                            Dim dy As Integer = Math.Abs(Top - preview.Y)
                            Opacity = If(dx + dy < 80, 0.92, 1.0)
        End If
                    Catch
                    End Try
                End If
            End If
        End Sub

        Private Sub sbTitle_MouseUp(sender As Object, e As MouseEventArgs) Handles sbTitle.MouseUp
            isMoving = False
            Try
                Opacity = 1.0
            Catch
            End Try
            _snapPreviewPoint = Point.Empty
            SnapOskIfEnabled()
            frmKeyboard_Move(RuntimeHelpers.GetObjectValue(sender), e)
    End Sub

        Public Sub sbIncrementPlus_Click(sender As Object, e As EventArgs)
            increment += 2
            lblIncrement.Text = Conversions.ToString(increment) & " PIXELS"
        End Sub

        Public Sub sbIncrementMinus_Click(sender As Object, e As EventArgs)
            If increment > 2 Then
                increment -= 2
        End If
            lblIncrement.Text = Conversions.ToString(increment) & " PIXELS"
        End Sub

        Public Sub sbHeightPlus_Click(sender As Object, e As EventArgs)
            Height += increment
            Top = CInt(Math.Round(Top - increment / 2.0))
            frmKeyboard_ResizeEnd(RuntimeHelpers.GetObjectValue(sender), e)
        End Sub

        Public Sub sbHeightMinus_Click(sender As Object, e As EventArgs)
            Height -= increment
            Top = CInt(Math.Round(Top + increment / 2.0))
            frmKeyboard_ResizeEnd(RuntimeHelpers.GetObjectValue(sender), e)
        End Sub

        Public Sub sbWidthPlus_Click(sender As Object, e As EventArgs)
            Width += increment
            Left = CInt(Math.Round(Left - increment / 2.0))
            frmKeyboard_ResizeEnd(RuntimeHelpers.GetObjectValue(sender), e)
    End Sub

        Public Sub sbWidthMinus_Click(sender As Object, e As EventArgs)
            Width -= increment
            Left = CInt(Math.Round(Left + increment / 2.0))
            frmKeyboard_ResizeEnd(RuntimeHelpers.GetObjectValue(sender), e)
        End Sub

        Private Sub sbDone_Click(sender As Object, e As EventArgs)
            frmKeyboard_ResizeEnd(RuntimeHelpers.GetObjectValue(sender), e)
            sbNum.Color = LCARScolorStyles.MiscFunction
            sbNum.Clickable = True
        End Sub

        Private Sub sbChangeSize_Click(sender As Object, e As EventArgs) Handles sbChangeSize.Click
            If sbNum.Color = LCARScolorStyles.StaticBlue Then
                sbNum_Click(RuntimeHelpers.GetObjectValue(sender), e)
        End If
            Resize_Keypad.TopMost = True
            Resize_Keypad.Show(Me)
            Resize_Keypad.BringToFront()
        End Sub

        Private Sub StandardButton1_Click_2(sender As Object, e As EventArgs) Handles StandardButton1.Click
            _snapEnabled = Not _snapEnabled
            SaveSetting("x32_OSK", "Settings", "SnapEnabled", If(_snapEnabled, "1", "0"))
            UpdateSnapButtonChrome()
            If _snapEnabled Then SnapOskIfEnabled()
        End Sub

        Private Sub StandardButton2_Click(sender As Object, e As EventArgs) Handles StandardButton2.Click
            If sbNum.Color = LCARScolorStyles.MiscFunction Then
                My.Settings.Mode = False
            ElseIf sbNum.Color = LCARScolorStyles.StaticBlue Then
                My.Settings.Mode = True
        End If
            My.Settings.Save()
            Hide()
        End Sub

        Private Sub sbNum_Click(sender As Object, e As EventArgs) Handles sbNum.Click
            If sbNum.Color = LCARScolorStyles.MiscFunction Then
                Label1.Text = Conversions.ToString(SplitContainer1.SplitterDistance)
                Label2.Text = Conversions.ToString(SplitContainer1.Panel2.Width)
                Label3.Text = Conversions.ToString(Width)
                Width = CInt(Math.Round(Width * 0.79))
                SplitContainer1.Panel2Collapsed = True
                sbNum.Color = LCARScolorStyles.StaticBlue
            ElseIf sbNum.Color = LCARScolorStyles.StaticBlue Then
                Width = Conversions.ToInteger(Label3.Text)
                sbNum.Color = LCARScolorStyles.MiscFunction
                SplitContainer1.SplitterDistance = Conversions.ToInteger(Label1.Text)
                SplitContainer1.Panel2Collapsed = False
            End If
    End Sub

        Private Sub sbPgUp_Click(sender As Object, e As EventArgs)
            OskSendKeys("{pgup}")
        End Sub

        Private Sub sbPgDown_Click(sender As Object, e As EventArgs)
            OskSendKeys("{pgdn}")
        End Sub

        Private Sub sbPgUp_Click_1(sender As Object, e As EventArgs)
        End Sub

        Private Sub sbRForwardSlash_Click(sender As Object, e As EventArgs)
        End Sub

        Private Sub sbPgUp_Click_2(sender As Object, e As EventArgs) Handles sbPgUp.Click
            OskSendKeys("{pgup}")
        End Sub

        Private Sub sbPgDown_Click_1(sender As Object, e As EventArgs) Handles sbPgDown.Click
            OskSendKeys("{pgdn}")
    End Sub

        Private Sub SplitContainer1_Panel1_Paint(sender As Object, e As PaintEventArgs) Handles SplitContainer1.Panel1.Paint
        End Sub

        Private Function IsRunningElevated() As Boolean
            'Discarded unreachable code: IL_001d, IL_002e, IL_0035
            Try
                Dim current As WindowsIdentity = WindowsIdentity.GetCurrent()
                Dim windowsPrincipal As WindowsPrincipal = New WindowsPrincipal(current)
                Return windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator)
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Dim result = False
                Call ProjectData.ClearProjectError()
                Return result
            End Try
        End Function

        Private Sub UpdateElevateButtonState()
            If IsRunningElevated() Then
                sbElevate.ButtonText = "ELEVATED"
                sbElevate.Text = "ELEVATED"
                sbElevate.Data = "ELEVATED"
                sbElevate.Data2 = "ELEVATED"
                sbElevate.Color = LCARScolorStyles.PrimaryFunction
            Else
                sbElevate.ButtonText = "ELEVATE"
                sbElevate.Text = "ELEVATE"
                sbElevate.Data = "ELEVATE"
                sbElevate.Data2 = "ELEVATE"
                sbElevate.Color = LCARScolorStyles.SystemFunction
        End If
        End Sub

        Private Sub sbElevate_Click(sender As Object, e As EventArgs) Handles sbElevate.Click
            Dim flag As Boolean = IsRunningElevated()
            Dim prompt = If(Not flag, "Restart keyboard with admin rights?", "Restart keyboard without admin rights?")
            If MsgBox(prompt, MsgBoxStyle.YesNo Or MsgBoxStyle.Question) <> MsgBoxResult.Yes Then
                Return
        End If
            Try
                If flag Then
                    Dim processStartInfo As ProcessStartInfo = New ProcessStartInfo()
                    processStartInfo.FileName = "explorer.exe"
                    processStartInfo.Arguments = """" & Application.ExecutablePath & """"
                    processStartInfo.UseShellExecute = True
                    Process.Start(processStartInfo)
                Else
                    Dim processStartInfo2 As ProcessStartInfo = New ProcessStartInfo()
                    processStartInfo2.FileName = Application.ExecutablePath
                    processStartInfo2.WorkingDirectory = Application.StartupPath
                    processStartInfo2.UseShellExecute = True
                    processStartInfo2.Verb = "runas"
                    Process.Start(processStartInfo2)
                End If
                _allowProcessExit = True
                Call Application.Exit()
            Catch ex As Win32Exception
                ProjectData.SetProjectError(ex)
                Dim ex2 = ex
                If Not flag Then
                    MsgBox("Elevation was cancelled or failed.", MsgBoxStyle.Exclamation)
                Else
                    MsgBox("Unable to restart the keyboard without admin rights." & vbCrLf & ex2.Message, MsgBoxStyle.Exclamation)
                End If
                Call ProjectData.ClearProjectError()
            Catch ex3 As Exception
                ProjectData.SetProjectError(ex3)
                Dim ex4 = ex3
                MsgBox("Unable to restart the keyboard." & vbCrLf & ex4.Message, MsgBoxStyle.Exclamation)
                Call ProjectData.ClearProjectError()
            End Try
        End Sub

        Private Sub sbBrowse_Click(sender As Object, e As EventArgs)
            Dim openFileDialog As OpenFileDialog = New OpenFileDialog()
            Dim dialogResult As DialogResult = openFileDialog.ShowDialog()
            If dialogResult <> DialogResult.OK Then
            End If
    End Sub

        Public Sub sbLock_Click(sender As Object, e As EventArgs) Handles sbLock.Click
            If sbFn.Clickable Then
                sbFn.Clickable = False
                sbFn.Color = LCARScolorStyles.FunctionUnavailable
            Else
                sbFn.Clickable = True
                sbFn.Color = LCARScolorStyles.StaticTan
        End If
        End Sub

        Private Sub sbFn_Click(sender As Object, e As EventArgs) Handles sbFn.Click
            UserButtons.Show()
        End Sub

        Private Sub sbFn1_Click(sender As Object, e As EventArgs) Handles sbFn1.Click
            If sbFn.Clickable Then
                My.Settings.FN1Name = UserButtons.txtUBName.Text
                My.Settings.FN1Path = UserButtons.txtUBLoc.Text
                sbFn1.Text = UserButtons.txtUBName.Text
                My.Settings.Save()
            ElseIf Not UserButtons.Visible Then
                Try
                    Interaction.Shell(My.Settings.FN1Path, AppWinStyle.MaximizedFocus)
                Catch ex As Exception
                    ProjectData.SetProjectError(ex)
                    Dim ex2 = ex
                    Call ProjectData.ClearProjectError()
                End Try
        End If
        End Sub

        Private Sub sbFn2_Click(sender As Object, e As EventArgs) Handles sbFn2.Click
            If sbFn.Clickable Then
                My.Settings.FN2Name = UserButtons.txtUBName.Text
                My.Settings.FN2Path = UserButtons.txtUBLoc.Text
                sbFn2.Text = UserButtons.txtUBName.Text
                My.Settings.Save()
            ElseIf Not UserButtons.Visible Then
                Try
                    Interaction.Shell(My.Settings.FN2Path, AppWinStyle.MaximizedFocus)
                Catch ex As Exception
                    ProjectData.SetProjectError(ex)
                    Dim ex2 = ex
                    Call ProjectData.ClearProjectError()
                End Try
            End If
    End Sub

        Private Sub sbFN3_Click(sender As Object, e As EventArgs) Handles sbFN3.Click
            If sbFn.Clickable Then
                My.Settings.FN3Name = UserButtons.txtUBName.Text
                My.Settings.FN3Path = UserButtons.txtUBLoc.Text
                sbFN3.Text = UserButtons.txtUBName.Text
                My.Settings.Save()
            ElseIf Not UserButtons.Visible Then
                Try
                    Interaction.Shell(My.Settings.FN3Path, AppWinStyle.MaximizedFocus)
                Catch ex As Exception
                    ProjectData.SetProjectError(ex)
                    Dim ex2 = ex
                    Call ProjectData.ClearProjectError()
                End Try
            End If
        End Sub

        Private Sub sbFn4_Click(sender As Object, e As EventArgs) Handles sbFn4.Click
            If sbFn.Clickable Then
                My.Settings.FN4Name = UserButtons.txtUBName.Text
                My.Settings.FN4Path = UserButtons.txtUBLoc.Text
                sbFn4.Text = UserButtons.txtUBName.Text
                My.Settings.Save()
            ElseIf Not UserButtons.Visible Then
                Try
                    Interaction.Shell(My.Settings.FN4Path, AppWinStyle.MaximizedFocus)
                Catch ex As Exception
                    ProjectData.SetProjectError(ex)
                    Dim ex2 = ex
                    Call ProjectData.ClearProjectError()
                End Try
            End If
        End Sub

        Private Sub sbFn5_Click(sender As Object, e As EventArgs) Handles sbFn5.Click
            If sbFn.Clickable Then
                My.Settings.FN5Name = UserButtons.txtUBName.Text
                My.Settings.FN5Path = UserButtons.txtUBLoc.Text
                sbFn5.Text = UserButtons.txtUBName.Text
                My.Settings.Save()
            ElseIf Not UserButtons.Visible Then
                Try
                    Interaction.Shell(My.Settings.FN5Path, AppWinStyle.MaximizedFocus)
                Catch ex As Exception
                    ProjectData.SetProjectError(ex)
                    Dim ex2 = ex
                    Call ProjectData.ClearProjectError()
                End Try
            End If
        End Sub

        Private Sub sbFn6_Click(sender As Object, e As EventArgs) Handles sbFn6.Click
            If sbFn.Clickable Then
                My.Settings.FN6Name = UserButtons.txtUBName.Text
                My.Settings.FN6Path = UserButtons.txtUBLoc.Text
                sbFn6.Text = UserButtons.txtUBName.Text
                My.Settings.Save()
            ElseIf Not UserButtons.Visible Then
                Try
                    Interaction.Shell(My.Settings.FN6Path, AppWinStyle.MaximizedFocus)
                Catch ex As Exception
                    ProjectData.SetProjectError(ex)
                    Dim ex2 = ex
                    Call ProjectData.ClearProjectError()
                End Try
            End If
        End Sub

        Private Sub sbFn7_Click(sender As Object, e As EventArgs) Handles sbFn7.Click
            If sbFn.Clickable Then
                My.Settings.FN7Name = UserButtons.txtUBName.Text
                My.Settings.FN7Path = UserButtons.txtUBLoc.Text
                sbFn7.Text = UserButtons.txtUBName.Text
                My.Settings.Save()
            ElseIf Not UserButtons.Visible Then
                Try
                    Interaction.Shell(My.Settings.FN7Path, AppWinStyle.MaximizedFocus)
                Catch ex As Exception
                    ProjectData.SetProjectError(ex)
                    Dim ex2 = ex
                    Call ProjectData.ClearProjectError()
                End Try
            End If
        End Sub

        Private Sub sbFn8_Click(sender As Object, e As EventArgs) Handles sbFn8.Click
            If sbFn.Clickable Then
                My.Settings.FN8Name = UserButtons.txtUBName.Text
                My.Settings.FN8Path = UserButtons.txtUBLoc.Text
                sbFn8.Text = UserButtons.txtUBName.Text
                My.Settings.Save()
            ElseIf Not UserButtons.Visible Then
                Try
                    Interaction.Shell(My.Settings.FN8Path, AppWinStyle.MaximizedFocus)
                Catch ex As Exception
                    ProjectData.SetProjectError(ex)
                    Dim ex2 = ex
                    Call ProjectData.ClearProjectError()
                End Try
        End If
        End Sub

        Private Sub sbFn9_Click(sender As Object, e As EventArgs) Handles sbFn9.Click
            If sbFn.Clickable Then
                My.Settings.FN9Name = UserButtons.txtUBName.Text
                My.Settings.FN9Path = UserButtons.txtUBLoc.Text
                sbFn9.Text = UserButtons.txtUBName.Text
                My.Settings.Save()
            ElseIf Not UserButtons.Visible Then
                Try
                    Interaction.Shell(My.Settings.FN9Path, AppWinStyle.MaximizedFocus)
                Catch ex As Exception
                    ProjectData.SetProjectError(ex)
                    Dim ex2 = ex
                    Call ProjectData.ClearProjectError()
                End Try
            End If
        End Sub

        Private Sub sbFn10_Click(sender As Object, e As EventArgs) Handles sbFn10.Click
            If sbFn.Clickable Then
                My.Settings.FN10Name = UserButtons.txtUBName.Text
                My.Settings.FN10Path = UserButtons.txtUBLoc.Text
                sbFn10.Text = UserButtons.txtUBName.Text
                My.Settings.Save()
            ElseIf Not UserButtons.Visible Then
                Try
                    Interaction.Shell(My.Settings.FN10Path, AppWinStyle.MaximizedFocus)
                Catch ex As Exception
                    ProjectData.SetProjectError(ex)
                    Dim ex2 = ex
                    Call ProjectData.ClearProjectError()
                End Try
            End If
        End Sub

        Private Sub sbFn11_Click(sender As Object, e As EventArgs) Handles sbFn11.Click
            If sbFn.Clickable Then
                My.Settings.FN11Name = UserButtons.txtUBName.Text
                My.Settings.FN11Path = UserButtons.txtUBLoc.Text
                sbFn11.Text = UserButtons.txtUBName.Text
                My.Settings.Save()
            ElseIf Not UserButtons.Visible Then
                Try
                    Interaction.Shell(My.Settings.FN11Path, AppWinStyle.MaximizedFocus)
                Catch ex As Exception
                    ProjectData.SetProjectError(ex)
                    Dim ex2 = ex
                    Call ProjectData.ClearProjectError()
                End Try
            End If
        End Sub

        Private Sub sbFn12_Click(sender As Object, e As EventArgs) Handles sbFn12.Click
            If sbFn.Clickable Then
                My.Settings.FN12Name = UserButtons.txtUBName.Text
                My.Settings.FN12Path = UserButtons.txtUBLoc.Text
                sbFn12.Text = UserButtons.txtUBName.Text
                My.Settings.Save()
            ElseIf Not UserButtons.Visible Then
                Try
                    Interaction.Shell(My.Settings.FN12Path, AppWinStyle.MaximizedFocus)
                Catch ex As Exception
                    ProjectData.SetProjectError(ex)
                    Dim ex2 = ex
                    Call ProjectData.ClearProjectError()
                End Try
            End If
    End Sub

        Private Sub Arrow_Click(sender As Object, e As EventArgs) Handles abUp.Click, abDown.Click, abLeft.Click, abRight.Click
            Dim standardButton As StandardButton = New StandardButton()
            Select Case CType(sender, ArrowButton).ArrowDirection
                Case LCARSarrowDirection.Up
                    standardButton.ButtonText = "UP"
                Case LCARSarrowDirection.Down
                    standardButton.ButtonText = "DOWN"
                Case LCARSarrowDirection.Left
                    standardButton.ButtonText = "LEFT"
                Case LCARSarrowDirection.Right
                    standardButton.ButtonText = "RIGHT"
            End Select
            StandardKey_Click(standardButton, e)
        End Sub

        ''' <summary>Hide dedicated PAGE UP/DN; those live on the pad when Num Lock is off.</summary>
        Private Sub HideDedicatedPageKeys()
            If sbPgUp IsNot Nothing AndAlso Not sbPgUp.IsDisposed Then
                sbPgUp.Visible = False
                sbPgUp.Enabled = False
        End If
            If sbPgDown IsNot Nothing AndAlso Not sbPgDown.IsDisposed Then
                sbPgDown.Visible = False
                sbPgDown.Enabled = False
            End If
        End Sub

        Private Function IsNumLockOff() As Boolean
            Return btnnumlock IsNot Nothing AndAlso btnnumlock.Color = LCARScolorStyles.SystemFunction
        End Function

        ''' <summary>Show digit labels when Num Lock is on; nav labels (Data2) when off.</summary>
        Private Sub ApplyNumLockPadLabels()
            If SplitContainer1 Is Nothing OrElse SplitContainer1.IsDisposed Then Return
            HideDedicatedPageKeys()
            Dim useNav As Boolean = IsNumLockOff()
            SetPadLabel(sbR7, useNav)
            SetPadLabel(sbR8, useNav)
            SetPadLabel(sbR9, useNav)
            SetPadLabel(sbR4, useNav)
            SetPadLabel(sbR5, useNav)
            SetPadLabel(sbR6, useNav)
            SetPadLabel(sbR1, useNav)
            SetPadLabel(sbR2, useNav)
            SetPadLabel(sbR3, useNav)
            SetPadLabel(sbR0, useNav)
            SetPadLabel(sbRPeriod, useNav)
            UpdateSnapButtonChrome()
        End Sub

        Private Sub SetPadLabel(btn As StandardButton, useNav As Boolean)
            If btn Is Nothing OrElse btn.IsDisposed Then Return
            Dim label As String
            If useNav Then
                label = Conversions.ToString(btn.Data2)
            Else
                label = Conversions.ToString(btn.Data)
            End If
            If label Is Nothing Then label = ""
            btn.ButtonText = label
            btn.Text = label
    End Sub

        Private Sub sbR9_Click(sender As Object, e As EventArgs) Handles sbR9.Click
            If IsNumLockOff() Then
                OskSendKeys("{PGUP}")
            Else
                OskSendKeys("{9}")
        End If
            If SHIFT Then
                Shift_Click(RuntimeHelpers.GetObjectValue(sender), e)
            End If
        End Sub

        Private Sub sbR8_Click(sender As Object, e As EventArgs) Handles sbR8.Click
            If IsNumLockOff() Then
                OskSendKeys("{UP}")
            Else
                OskSendKeys("{8}")
        End If
            If SHIFT Then
                Shift_Click(RuntimeHelpers.GetObjectValue(sender), e)
            End If
        End Sub

        Private Sub sbR7_Click(sender As Object, e As EventArgs) Handles sbR7.Click
            If IsNumLockOff() Then
                OskSendKeys("{HOME}")
            Else
                OskSendKeys("{7}")
                End If
            If SHIFT Then
                Shift_Click(RuntimeHelpers.GetObjectValue(sender), e)
            End If
        End Sub

        Private Sub sbR6_Click(sender As Object, e As EventArgs) Handles sbR6.Click
            If IsNumLockOff() Then
                OskSendKeys("{RIGHT}")
            Else
                OskSendKeys("{6}")
            End If
            If SHIFT Then
                Shift_Click(RuntimeHelpers.GetObjectValue(sender), e)
        End If
        End Sub

        Private Sub sbR5_Click(sender As Object, e As EventArgs) Handles sbR5.Click
            If Not IsNumLockOff() Then
                OskSendKeys("{5}")
                End If
            If SHIFT Then
                Shift_Click(RuntimeHelpers.GetObjectValue(sender), e)
                End If
    End Sub

        Private Sub sbR4_Click(sender As Object, e As EventArgs) Handles sbR4.Click
            If IsNumLockOff() Then
                OskSendKeys("{LEFT}")
            Else
                OskSendKeys("{4}")
            End If
            If SHIFT Then
                Shift_Click(RuntimeHelpers.GetObjectValue(sender), e)
        End If
    End Sub

        Private Sub sbR3_Click(sender As Object, e As EventArgs) Handles sbR3.Click
            If IsNumLockOff() Then
                OskSendKeys("{PGDN}")
            Else
                OskSendKeys("{3}")
            End If
            If SHIFT Then
                Shift_Click(RuntimeHelpers.GetObjectValue(sender), e)
        End If
    End Sub

        Private Sub sbR2_Click(sender As Object, e As EventArgs) Handles sbR2.Click
            If IsNumLockOff() Then
                OskSendKeys("{DOWN}")
            Else
                OskSendKeys("{2}")
            End If
            If SHIFT Then
                Shift_Click(RuntimeHelpers.GetObjectValue(sender), e)
        End If
    End Sub

        Private Sub sbR1_Click(sender As Object, e As EventArgs) Handles sbR1.Click
            If IsNumLockOff() Then
                OskSendKeys("{END}")
            Else
                OskSendKeys("{1}")
            End If
            If SHIFT Then
                Shift_Click(RuntimeHelpers.GetObjectValue(sender), e)
        End If
    End Sub

        Private Sub sbR0_Click(sender As Object, e As EventArgs) Handles sbR0.Click
            If IsNumLockOff() Then
                OskSendKeys("{INSERT}")
            Else
                OskSendKeys("{0}")
            End If
            If SHIFT Then
                Shift_Click(RuntimeHelpers.GetObjectValue(sender), e)
        End If
        End Sub

        Private Sub sbRForwardSlash_Click_1(sender As Object, e As EventArgs) Handles sbRForwardSlash.Click
            OskSendKeys("{/}")
            If SHIFT Then
                Shift_Click(RuntimeHelpers.GetObjectValue(sender), e)
        End If
    End Sub

        Private Sub sbRMultiply_Click(sender As Object, e As EventArgs) Handles sbRMultiply.Click
            OskSendKeys("{*}")
            If SHIFT Then
                Shift_Click(RuntimeHelpers.GetObjectValue(sender), e)
            End If
        End Sub

        Private Sub sbRMinus_Click(sender As Object, e As EventArgs) Handles sbRMinus.Click
            OskSendKeys("{-}")
            If SHIFT Then
                Shift_Click(RuntimeHelpers.GetObjectValue(sender), e)
        End If
    End Sub

        Private Sub sbRPlus_Click(sender As Object, e As EventArgs) Handles sbRPlus.Click
            OskSendKeys("{+}")
            If SHIFT Then
                Shift_Click(RuntimeHelpers.GetObjectValue(sender), e)
            End If
        End Sub

        Private Sub sbREquals_Click(sender As Object, e As EventArgs) Handles sbREquals.Click
            OskSendKeys("{=}")
            If SHIFT Then
                Shift_Click(RuntimeHelpers.GetObjectValue(sender), e)
        End If
    End Sub

        Private Sub sbRPeriod_Click(sender As Object, e As EventArgs) Handles sbRPeriod.Click
            If IsNumLockOff() Then
                OskSendKeys("{DEL}")
            Else
                OskSendKeys("{.}")
            End If
            If SHIFT Then
                Shift_Click(RuntimeHelpers.GetObjectValue(sender), e)
        End If
        End Sub

        Private Sub btnscrllk_Click(sender As Object, e As EventArgs) Handles btnScrollLock.Click
            If btnScrollLock.Color = LCARScolorStyles.SystemFunction Then
                keybd_event(145, 0, 1L, 0L)
                keybd_event(145, 0, 3L, 0L)
                btnScrollLock.Color = LCARScolorStyles.PrimaryFunction
            ElseIf btnScrollLock.Color = LCARScolorStyles.PrimaryFunction Then
                keybd_event(145, 0, 1L, 0L)
                keybd_event(145, 0, 3L, 0L)
                btnScrollLock.Color = LCARScolorStyles.SystemFunction
        End If
    End Sub

        Private Sub btnnumlok_Click(sender As Object, e As EventArgs) Handles btnnumlock.Click
            If btnnumlock.Color = LCARScolorStyles.SystemFunction Then
                keybd_event(144, 0, 1L, 0L)
                keybd_event(144, 0, 3L, 0L)
                btnnumlock.Color = LCARScolorStyles.PrimaryFunction
            Else
                keybd_event(144, 0, 1L, 0L)
                keybd_event(144, 0, 3L, 0L)
                btnnumlock.Color = LCARScolorStyles.SystemFunction
            End If
            ApplyNumLockPadLabels()
    End Sub

        Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
            TrackInputTarget()
            If GetKeyState(145L) = 1 Then
                If btnScrollLock.Color = LCARScolorStyles.SystemFunction Then
                    btnScrollLock.Color = LCARScolorStyles.PrimaryFunction
            End If
            ElseIf btnScrollLock.Color = LCARScolorStyles.PrimaryFunction Then
                btnScrollLock.Color = LCARScolorStyles.SystemFunction
        End If
            Dim numLockChanged = IsKeyLocked(Keys.NumLock) Xor btnnumlock.Color = LCARScolorStyles.PrimaryFunction
            If IsKeyLocked(Keys.NumLock) Then
                If btnnumlock.Color = LCARScolorStyles.SystemFunction Then
                    btnnumlock.Color = LCARScolorStyles.PrimaryFunction
        End If
            ElseIf btnnumlock.Color = LCARScolorStyles.PrimaryFunction Then
                btnnumlock.Color = LCARScolorStyles.SystemFunction
            End If
            If numLockChanged Then
                ApplyNumLockPadLabels()
        End If
            If IsKeyLocked(Keys.Capital) Then
                If sbCaps.Color = LCARScolorStyles.SystemFunction Then
                    sbCaps.Color = LCARScolorStyles.PrimaryFunction
        End If
            ElseIf Not CAPS AndAlso sbCaps.Color = LCARScolorStyles.PrimaryFunction Then
                sbCaps.Color = LCARScolorStyles.SystemFunction
            End If
            Dim flag2 = sbCaps.Color = LCARScolorStyles.PrimaryFunction Xor sbLShift.Color = LCARScolorStyles.PrimaryFunction
            If flag2 And Not uppercase Then
                Dim enumerator3 As IEnumerator = Nothing
                Try
                    enumerator3 = SplitContainer1.Panel1.Controls.GetEnumerator()
                    While enumerator3.MoveNext()
                        Dim lCARSbuttonClass3 = CType(enumerator3.Current, LCARSbuttonClass)
                        If lCARSbuttonClass3.ButtonText.Length = 1 And Char.IsLetter(Conversions.ToChar(lCARSbuttonClass3.ButtonText)) Then
                            lCARSbuttonClass3.ButtonText = Conversions.ToString(lCARSbuttonClass3.Data2)
        End If
                    End While

                Finally
                    If TypeOf enumerator3 Is IDisposable Then
                        TryCast(enumerator3, IDisposable).Dispose()
        End If
                End Try
            ElseIf uppercase AndAlso Not flag2 Then
                Dim enumerator4 As IEnumerator = Nothing
                Try
                    enumerator4 = SplitContainer1.Panel1.Controls.GetEnumerator()
                    While enumerator4.MoveNext()
                        Dim lCARSbuttonClass4 = CType(enumerator4.Current, LCARSbuttonClass)
                        If lCARSbuttonClass4.ButtonText.Length = 1 And Char.IsLetter(Conversions.ToChar(lCARSbuttonClass4.ButtonText)) Then
                            lCARSbuttonClass4.ButtonText = Conversions.ToString(lCARSbuttonClass4.Data)
                        End If
                    End While

                Finally
                    If TypeOf enumerator4 Is IDisposable Then
                        TryCast(enumerator4, IDisposable).Dispose()
                    End If
                End Try
            End If
            uppercase = flag2
    End Sub

        Private Sub sbA_Click(sender As Object, e As EventArgs) Handles sbA.Click
            Dim text = ""
            If CAPS And Not SHIFT Then
                OskSendKeys("{A}")
            ElseIf Not CAPS And SHIFT Then
                OskSendKeys("{A}")
            Else
                OskSendKeys("{a}")
            End If
            If SHIFT Then
                Shift_Click(RuntimeHelpers.GetObjectValue(sender), e)
        End If
    End Sub

        Private Sub sbB_Click(sender As Object, e As EventArgs) Handles sbB.Click
            Dim text = ""
            If CAPS And Not SHIFT Then
                OskSendKeys("{B}")
            ElseIf Not CAPS And SHIFT Then
                OskSendKeys("{B}")
            Else
                OskSendKeys("{b}")
            End If
            If SHIFT Then
                Shift_Click(RuntimeHelpers.GetObjectValue(sender), e)
        End If
    End Sub

        Private Sub sbC_Click(sender As Object, e As EventArgs) Handles sbC.Click
            Dim text = ""
            If CAPS And Not SHIFT Then
                OskSendKeys("{C}")
            ElseIf Not CAPS And SHIFT Then
                OskSendKeys("{C}")
            Else
                OskSendKeys("{c}")
            End If
            If SHIFT Then
                Shift_Click(RuntimeHelpers.GetObjectValue(sender), e)
        End If
        End Sub

        Private Sub sbD_Click(sender As Object, e As EventArgs) Handles sbD.Click
            Dim text = ""
            If CAPS And Not SHIFT Then
                OskSendKeys("{D}")
            ElseIf Not CAPS And SHIFT Then
                OskSendKeys("{D}")
            Else
                OskSendKeys("{d}")
        End If
            If SHIFT Then
                Shift_Click(RuntimeHelpers.GetObjectValue(sender), e)
            End If
    End Sub

        Private Sub sbE_Click(sender As Object, e As EventArgs) Handles sbE.Click
            Dim text = ""
            If CAPS And Not SHIFT Then
                OskSendKeys("{E}")
            ElseIf Not CAPS And SHIFT Then
                OskSendKeys("{E}")
            Else
                OskSendKeys("{e}")
            End If
            If SHIFT Then
                Shift_Click(RuntimeHelpers.GetObjectValue(sender), e)
        End If
        End Sub

        Private Sub sbF_Click(sender As Object, e As EventArgs) Handles sbF.Click
            Dim text = ""
            If CAPS And Not SHIFT Then
                OskSendKeys("{F}")
            ElseIf Not CAPS And SHIFT Then
                OskSendKeys("{F}")
            Else
                OskSendKeys("{f}")
        End If
            If SHIFT Then
                Shift_Click(RuntimeHelpers.GetObjectValue(sender), e)
            End If
    End Sub

        Private Sub sbG_Click(sender As Object, e As EventArgs) Handles sbG.Click
            Dim text = ""
            If CAPS And Not SHIFT Then
                OskSendKeys("{G}")
            ElseIf Not CAPS And SHIFT Then
                OskSendKeys("{G}")
            Else
                OskSendKeys("{g}")
            End If
            If SHIFT Then
                Shift_Click(RuntimeHelpers.GetObjectValue(sender), e)
        End If
        End Sub

        Private Sub sbH_Click(sender As Object, e As EventArgs) Handles sbH.Click
            Dim text = ""
            If CAPS And Not SHIFT Then
                OskSendKeys("{H}")
            ElseIf Not CAPS And SHIFT Then
                OskSendKeys("{H}")
            Else
                OskSendKeys("{h}")
        End If
            If SHIFT Then
                Shift_Click(RuntimeHelpers.GetObjectValue(sender), e)
            End If
    End Sub

        Private Sub sbI_Click(sender As Object, e As EventArgs) Handles sbI.Click
            Dim text = ""
            If CAPS And Not SHIFT Then
                OskSendKeys("{I}")
            ElseIf Not CAPS And SHIFT Then
                OskSendKeys("{I}")
            Else
                OskSendKeys("{i}")
            End If
            If SHIFT Then
                Shift_Click(RuntimeHelpers.GetObjectValue(sender), e)
        End If
        End Sub

        Private Sub sbJ_Click(sender As Object, e As EventArgs) Handles sbJ.Click
            Dim text = ""
            If CAPS And Not SHIFT Then
                OskSendKeys("{J}")
            ElseIf Not CAPS And SHIFT Then
                OskSendKeys("{J}")
            Else
                OskSendKeys("{j}")
        End If
            If SHIFT Then
                Shift_Click(RuntimeHelpers.GetObjectValue(sender), e)
            End If
    End Sub

        Private Sub sbK_Click(sender As Object, e As EventArgs) Handles sbK.Click
            Dim text = ""
            If CAPS And Not SHIFT Then
                OskSendKeys("{K}")
            ElseIf Not CAPS And SHIFT Then
                OskSendKeys("{K}")
            Else
                OskSendKeys("{k}")
            End If
            If SHIFT Then
                Shift_Click(RuntimeHelpers.GetObjectValue(sender), e)
        End If
        End Sub

        Private Sub sbL_Click(sender As Object, e As EventArgs) Handles sbL.Click
            Dim text = ""
            If CAPS And Not SHIFT Then
                OskSendKeys("{L}")
            ElseIf Not CAPS And SHIFT Then
                OskSendKeys("{L}")
            Else
                OskSendKeys("{l}")
        End If
            If SHIFT Then
                Shift_Click(RuntimeHelpers.GetObjectValue(sender), e)
            End If
    End Sub

        Private Sub sbM_Click(sender As Object, e As EventArgs) Handles sbM.Click
            Dim text = ""
            If CAPS And Not SHIFT Then
                OskSendKeys("{M}")
            ElseIf Not CAPS And SHIFT Then
                OskSendKeys("{M}")
            Else
                OskSendKeys("{m}")
            End If
            If SHIFT Then
                Shift_Click(RuntimeHelpers.GetObjectValue(sender), e)
        End If
        End Sub

        Private Sub sbN_Click(sender As Object, e As EventArgs) Handles sbN.Click
            Dim text = ""
            If CAPS And Not SHIFT Then
                OskSendKeys("{N}")
            ElseIf Not CAPS And SHIFT Then
                OskSendKeys("{N}")
            Else
                OskSendKeys("{n}")
        End If
            If SHIFT Then
                Shift_Click(RuntimeHelpers.GetObjectValue(sender), e)
            End If
    End Sub

        Private Sub sbO_Click(sender As Object, e As EventArgs) Handles sbO.Click
            Dim text = ""
            If CAPS And Not SHIFT Then
                OskSendKeys("{O}")
            ElseIf Not CAPS And SHIFT Then
                OskSendKeys("{O}")
            Else
                OskSendKeys("{o}")
            End If
            If SHIFT Then
                Shift_Click(RuntimeHelpers.GetObjectValue(sender), e)
        End If
        End Sub

        Private Sub sbP_Click(sender As Object, e As EventArgs) Handles sbP.Click
            Dim text = ""
            If CAPS And Not SHIFT Then
                OskSendKeys("{P}")
            ElseIf Not CAPS And SHIFT Then
                OskSendKeys("{P}")
            Else
                OskSendKeys("{p}")
        End If
            If SHIFT Then
                Shift_Click(RuntimeHelpers.GetObjectValue(sender), e)
            End If
    End Sub

        Private Sub sbQ_Click(sender As Object, e As EventArgs) Handles sbQ.Click
            Dim text = ""
            If CAPS And Not SHIFT Then
                OskSendKeys("{Q}")
            ElseIf Not CAPS And SHIFT Then
                OskSendKeys("{Q}")
            Else
                OskSendKeys("{q}")
            End If
            If SHIFT Then
                Shift_Click(RuntimeHelpers.GetObjectValue(sender), e)
        End If
        End Sub

        Private Sub sbR_Click(sender As Object, e As EventArgs) Handles sbR.Click
            Dim text = ""
            If CAPS And Not SHIFT Then
                OskSendKeys("{R}")
            ElseIf Not CAPS And SHIFT Then
                OskSendKeys("{R}")
            Else
                OskSendKeys("{r}")
        End If
            If SHIFT Then
                Shift_Click(RuntimeHelpers.GetObjectValue(sender), e)
            End If
    End Sub

        Private Sub sbS_Click(sender As Object, e As EventArgs) Handles sbS.Click
            Dim text = ""
            If CAPS And Not SHIFT Then
                OskSendKeys("{S}")
            ElseIf Not CAPS And SHIFT Then
                OskSendKeys("{S}")
            Else
                OskSendKeys("{s}")
            End If
            If SHIFT Then
                Shift_Click(RuntimeHelpers.GetObjectValue(sender), e)
        End If
        End Sub

        Private Sub sbT_Click(sender As Object, e As EventArgs) Handles sbT.Click
            Dim text = ""
            If CAPS And Not SHIFT Then
                OskSendKeys("{T}")
            ElseIf Not CAPS And SHIFT Then
                OskSendKeys("{T}")
            Else
                OskSendKeys("{t}")
        End If
            If SHIFT Then
                Shift_Click(RuntimeHelpers.GetObjectValue(sender), e)
            End If
    End Sub

        Private Sub sbU_Click(sender As Object, e As EventArgs) Handles sbU.Click
            Dim text = ""
            If CAPS And Not SHIFT Then
                OskSendKeys("{U}")
            ElseIf Not CAPS And SHIFT Then
                OskSendKeys("{U}")
            Else
                OskSendKeys("{u}")
            End If
            If SHIFT Then
                Shift_Click(RuntimeHelpers.GetObjectValue(sender), e)
        End If
        End Sub

        Private Sub sbV_Click(sender As Object, e As EventArgs) Handles sbV.Click
            Dim text = ""
            If CAPS And Not SHIFT Then
                OskSendKeys("{V}")
            ElseIf Not CAPS And SHIFT Then
                OskSendKeys("{V}")
            Else
                OskSendKeys("{v}")
        End If
            If SHIFT Then
                Shift_Click(RuntimeHelpers.GetObjectValue(sender), e)
            End If
    End Sub

        Private Sub sbW_Click(sender As Object, e As EventArgs) Handles sbW.Click
            Dim text = ""
            If CAPS And Not SHIFT Then
                OskSendKeys("{W}")
            ElseIf Not CAPS And SHIFT Then
                OskSendKeys("{W}")
            Else
                OskSendKeys("{w}")
            End If
            If SHIFT Then
                Shift_Click(RuntimeHelpers.GetObjectValue(sender), e)
        End If
        End Sub

        Private Sub sbX_Click(sender As Object, e As EventArgs) Handles sbX.Click
            Dim text = ""
            If CAPS And Not SHIFT Then
                OskSendKeys("{X}")
            ElseIf Not CAPS And SHIFT Then
                OskSendKeys("{X}")
            Else
                OskSendKeys("{x}")
        End If
            If SHIFT Then
                Shift_Click(RuntimeHelpers.GetObjectValue(sender), e)
            End If
    End Sub

        Private Sub sbY_Click(sender As Object, e As EventArgs) Handles sbY.Click
            Dim text = ""
            If CAPS And Not SHIFT Then
                OskSendKeys("{Y}")
            ElseIf Not CAPS And SHIFT Then
                OskSendKeys("{Y}")
            Else
                OskSendKeys("{y}")
            End If
            If SHIFT Then
                Shift_Click(RuntimeHelpers.GetObjectValue(sender), e)
        End If
        End Sub

        Private Sub sbZ_Click(sender As Object, e As EventArgs) Handles sbZ.Click
            Dim text = ""
            If CAPS And Not SHIFT Then
                OskSendKeys("{Z}")
            ElseIf Not CAPS And SHIFT Then
                OskSendKeys("{Z}")
            Else
                OskSendKeys("{z}")
        End If
            If SHIFT Then
                Shift_Click(RuntimeHelpers.GetObjectValue(sender), e)
            End If
    End Sub

        Private Sub Timer2_Tick(sender As Object, e As EventArgs) Handles Timer2.Tick
            If sbRAlt.Color = LCARScolorStyles.PrimaryFunction Then
                Alt_Click(RuntimeHelpers.GetObjectValue(sender), e)
        End If
        Timer2.Enabled = False
    End Sub

End Class

' TODO: Error SkippedTokensTrivia '}'

