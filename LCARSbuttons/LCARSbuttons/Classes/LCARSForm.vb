Imports System.Runtime.InteropServices
Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' A base Form class to handle common LCARS functions
''' </summary>
''' <remarks>
''' Any form that inherits from this class will have default handlers for LCARS events, and be bound
''' to the working area when maximized. This eliminates the previous requirement that LCARS apps 
''' register their main window with LCARS to get position and size updates.
''' 
''' Supported events:
'''  - Alerts initiated/ended
'''  - Colors changed
'''  - Beeping updated
'''  - LCARS closing
''' 
''' Default handlers are supplied for color changing, beep updating, and LCARS closing.
''' </remarks>
Public Class LCARSForm
    Inherits System.Windows.Forms.Form

#Region " Windows API "
    <StructLayout(LayoutKind.Sequential)> _
    Private Structure POINTAPI
        Dim X As Integer
        Dim Y As Integer
    End Structure

    <StructLayout(LayoutKind.Sequential)> _
    Private Structure RECT
        Dim Left As Integer
        Dim Top As Integer
        Dim Right As Integer
        Dim Bottom As Integer
    End Structure

    <StructLayout(LayoutKind.Sequential)> _
    Private Structure MINMAXINFO
        Dim ptReserved As POINTAPI
        Dim ptMaxSize As POINTAPI
        Dim ptMaxPosition As POINTAPI
        Dim ptMinTrackSize As POINTAPI
        Dim ptMaxTrackSize As POINTAPI
    End Structure

    <StructLayout(LayoutKind.Sequential)> _
    Private Structure MONITORINFO
        Dim cbSize As Int32
        Dim rcMonitor As RECT
        Dim rcWork As RECT
        Dim dwFlags As Int32
    End Structure

    <StructLayout(LayoutKind.Sequential)> _
    Private Structure COPYDATASTRUCT
        Public dwData As IntPtr
        Public cdData As Integer
        Public lpData As IntPtr
    End Structure

    Private Declare Function MonitorFromWindow Lib "user32" (ByVal hwnd As Int32, ByVal dwFlags As Int32) As Int32

    Private Declare Function MonitorFromPoint Lib "user32" (ByVal pt As POINTAPI, ByVal dwFlags As Int32) As Int32

    Private Declare Auto Function GetMonitorInfo Lib "user32" (ByVal hMonitor As Int32, ByRef lpmi As MONITORINFO) As Integer

    Private Declare Function RegisterWindowMessageA Lib "user32.dll" (ByVal lpString As String) As Integer

    Private Declare Auto Function SendMessage Lib "user32.dll" (ByVal hWnd As IntPtr, ByVal msg As Integer, ByVal wParam As IntPtr, ByVal lParam As IntPtr) As IntPtr

    Private Declare Function SystemParametersInfo Lib "user32.dll" Alias "SystemParametersInfoA" (ByVal uAction As UInteger, ByVal uParam As UInteger, ByVal lpvParam As IntPtr, ByVal fuWinIni As UInteger) As Boolean

    Private Const MONITOR_DEFAULTTONEAREST As Int32 = &H2
    Private Const MONITOR_DEFAULTTOPRIMARY As Int32 = &H1

    Private Const WM_MINMAXINFO As Integer = &H24
    Private Const WM_SETTINGCHANGE As Integer = &H1A
    Private Const WM_COPYDATA As Integer = &H4A
    Private Const SPI_GETWORKAREA As UInteger = 48
#End Region

    Private X32_MSG As Integer
    Private x32Handle As IntPtr = IntPtr.Zero
    Private registeredWithLcars As Boolean = False

    ''' <summary>
    ''' When False, this form keeps its manual bounds (e.g. Quick Controls dropdown).
    ''' </summary>
    Public Property BindToWorkingArea As Boolean = True

    ' View 1 Start Menu: 100×25, top is 78px above working-area (pnlMain) bottom.
    ' App CLOSE must share that horizontal line on the right edge.
    Public Shared ReadOnly ShellStartMenuSize As New Size(100, 25)
    Public Const ShellStartMenuTopFromWorkBottom As Integer = 78

#Region " Events "
    ''' <summary>
    ''' Raised when an alert is initiated.
    ''' </summary>
    ''' <param name="AlertID">ID of the alert initiated</param>
    Public Event AlertInitiated(ByVal AlertID As Integer)
    ''' <summary>
    ''' Raised when the current alert has ended
    ''' </summary>
    ''' <remarks>
    ''' If an alert ends because another alert has replaced it, this event will not be raised, only a
    ''' new <see cref="AlertInitiated">AlertInitiated</see> event
    ''' </remarks>
    Public Event AlertEnded()
#End Region

    ''' <summary>
    ''' Handles LCARS messages and maximized bounds
    ''' </summary>
    Protected Overrides Sub WndProc(ByRef m As System.Windows.Forms.Message)
        If m.Msg = X32_MSG Then
            m.Result = New IntPtr(1)
            Select Case m.LParam.ToInt32()
                Case 2
                    ' Registration ack from shell posts WParam=shellHwnd; color broadcast uses 0.
                    If m.WParam <> IntPtr.Zero Then
                        x32Handle = m.WParam
                    End If
                    OnColorsChange()
                Case 3
                    OnBeepingUpdate(String.Equals(GetSetting("LCARS x32", "Application", "ButtonBeep", "TRUE"), "True", StringComparison.OrdinalIgnoreCase))
                Case 11
                    OnAlertInitiated(m.WParam.ToInt32())
                Case 7
                    OnAlertEnded()
                Case 13
                    OnLCARSClosing()
            End Select

        ElseIf m.Msg = WM_COPYDATA Then
            If HandleWorkingAreaCopyData(m) Then
                Return
            End If
            MyBase.WndProc(m)

        ElseIf m.Msg = WM_MINMAXINFO Then
            Dim mmi As MINMAXINFO = CType(Marshal.PtrToStructure(m.LParam, GetType(MINMAXINFO)), MINMAXINFO)
            Dim monitor As Integer = MonitorFromWindow(Me.Handle.ToInt32(), MONITOR_DEFAULTTONEAREST)
            Dim pt0 As POINTAPI = New POINTAPI With {.X = 0, .Y = 0}
            Dim primary As Integer = MonitorFromPoint(pt0, MONITOR_DEFAULTTOPRIMARY)
            If monitor <> 0 AndAlso primary <> 0 Then
                Dim minfo As MONITORINFO = New MONITORINFO()
                Dim pminfo As MONITORINFO = New MONITORINFO()
                minfo.cbSize = Marshal.SizeOf(minfo)
                pminfo.cbSize = Marshal.SizeOf(pminfo)
                If GetMonitorInfo(monitor, minfo) <> 0 AndAlso GetMonitorInfo(primary, pminfo) Then
                    mmi.ptMaxPosition.X = minfo.rcWork.Left - minfo.rcMonitor.Left
                    mmi.ptMaxPosition.Y = minfo.rcWork.Top - minfo.rcMonitor.Top
                    mmi.ptMaxTrackSize.X = minfo.rcWork.Right - minfo.rcWork.Left
                    mmi.ptMaxTrackSize.Y = minfo.rcWork.Bottom - minfo.rcWork.Top

                    Marshal.StructureToPtr(mmi, m.LParam, True)
                    m.Result = New IntPtr(1)
                    Return
                End If
            End If
            m.Result = IntPtr.Zero

        ElseIf m.Msg = WM_SETTINGCHANGE Then
            MyBase.WndProc(m)
            If BindToWorkingArea Then
                ApplyCurrentWorkingAreaBounds()
            End If

        Else
            MyBase.WndProc(m)
        End If
    End Sub

    Private Function HandleWorkingAreaCopyData(ByRef m As Message) As Boolean
        If x32Handle = IntPtr.Zero Then
            x32Handle = New IntPtr(CInt(GetSetting("LCARS x32", "Application", "MainWindowHandle", "0")))
        End If

        Dim myData As COPYDATASTRUCT = CType(Marshal.PtrToStructure(m.LParam, GetType(COPYDATASTRUCT)), COPYDATASTRUCT)
        If myData.dwData.ToInt32() <> 100 Then
            Return False
        End If

        ' Accept shell working-area notifies even if MainWindowHandle drifted (desktop vs startup hwnd).
        If x32Handle <> IntPtr.Zero AndAlso m.WParam <> IntPtr.Zero AndAlso m.WParam <> x32Handle Then
            Dim registryHandle As IntPtr = New IntPtr(CInt(GetSetting("LCARS x32", "Application", "MainWindowHandle", "0")))
            If m.WParam <> registryHandle Then
                ' Still apply — dwData=100 is LCARS-private; rejecting it left Terminal stuck at old size.
            End If
            x32Handle = m.WParam
        End If

        Dim newBounds As Rectangle = CType(Marshal.PtrToStructure(myData.lpData, GetType(Rectangle)), Rectangle)
        Dim boundsCopy As Rectangle = newBounds
        BeginInvoke(New MethodInvoker(Sub() ApplyWorkingAreaBounds(boundsCopy)))
        m.Result = New IntPtr(1)
        Return True
    End Function

    Private Sub ApplyCurrentWorkingAreaBounds()
        If BindToWorkingArea = False Then Return
        ' Borderless LCARS apps track the hole via Normal bounds; Maximized is optional.
        If Me.FormBorderStyle <> FormBorderStyle.None AndAlso Me.WindowState <> FormWindowState.Maximized Then Return
        ApplyWorkingAreaBounds(GetSystemWorkingArea())
    End Sub

    ''' <summary>
    ''' Applies LCARS desktop-hole bounds. Borderless forms stay Normal and set Bounds
    ''' directly — re-Maximizing fights FormBorderStyle.None and drops resize updates.
    ''' </summary>
    Protected Overridable Sub ApplyWorkingAreaBounds(ByVal bounds As Rectangle)
        Try
            If bounds.Width <= 0 OrElse bounds.Height <= 0 Then Return

            If Me.FormBorderStyle = FormBorderStyle.None Then
                If Me.WindowState <> FormWindowState.Normal Then
                    Me.WindowState = FormWindowState.Normal
                End If
                If Me.Bounds <> bounds Then
                    Me.Bounds = bounds
                End If
                OnShellChromeLayout()
                Return
            End If

            If Me.Bounds = bounds AndAlso Me.WindowState = FormWindowState.Maximized Then
                OnShellChromeLayout()
                Return
            End If

            If Me.WindowState = FormWindowState.Maximized Then
                Me.WindowState = FormWindowState.Normal
            End If
            Me.Bounds = bounds
            Me.WindowState = FormWindowState.Maximized
            OnShellChromeLayout()
        Catch
            Try
                Me.Bounds = bounds
            Catch
            End Try
            OnShellChromeLayout()
        End Try
    End Sub

    Private Function GetSystemWorkingArea() As Rectangle
        Dim area As New RECT()
        Dim ptr As IntPtr = Marshal.AllocHGlobal(Marshal.SizeOf(area))
        Try
            If SystemParametersInfo(SPI_GETWORKAREA, 0, ptr, 0) Then
                area = CType(Marshal.PtrToStructure(ptr, GetType(RECT)), RECT)
                Return Rectangle.FromLTRB(area.Left, area.Top, area.Right, area.Bottom)
            End If
        Finally
            Marshal.FreeHGlobal(ptr)
        End Try
        Return Screen.FromHandle(Me.Handle).WorkingArea
    End Function

    ''' <summary>
    ''' Re-registers with the shell so resize notifies resume after a timed-out notify drop.
    ''' </summary>
    Public Sub EnsureRegisteredWithLcarsShell()
        registeredWithLcars = False
        RegisterWithLcarsShell()
    End Sub

    Private Sub RegisterWithLcarsShell()
        If registeredWithLcars OrElse Not Me.IsHandleCreated Then Return
        If X32_MSG = 0 Then
            X32_MSG = RegisterWindowMessageA("LCARS_X32_MSG")
        End If
        x32Handle = New IntPtr(CInt(GetSetting("LCARS x32", "Application", "MainWindowHandle", "0")))
        If x32Handle = IntPtr.Zero Then Return

        SendMessage(x32Handle, X32_MSG, Me.Handle, New IntPtr(1))
        registeredWithLcars = True
    End Sub

    Protected Overrides Sub OnHandleCreated(ByVal e As EventArgs)
        MyBase.OnHandleCreated(e)
        RegisterWithLcarsShell()
    End Sub

    Protected Overrides Sub OnLoad(ByVal e As System.EventArgs)
        X32_MSG = RegisterWindowMessageA("LCARS_X32_MSG")
        MyBase.OnLoad(e)
        RegisterWithLcarsShell()
        If BindToWorkingArea Then
            ApplyCurrentWorkingAreaBounds()
        End If
        OnShellChromeLayout()
    End Sub

    Protected Overrides Sub OnClientSizeChanged(ByVal e As EventArgs)
        MyBase.OnClientSizeChanged(e)
        If IsHandleCreated Then
            OnShellChromeLayout()
        End If
    End Sub

    ''' <summary>
    ''' Override to re-place shell-aligned CLOSE (and similar) after resize / working-area changes.
    ''' </summary>
    Protected Overridable Sub OnShellChromeLayout()
    End Sub

    ''' <summary>
    ''' Places CLOSE on the right at the same vertical row as View 1 Start Menu (left).
    ''' </summary>
    Protected Sub PlaceShellAlignedCloseButton(ByVal btn As Control, Optional ByVal rightInset As Integer = 0)
        If btn Is Nothing OrElse btn.IsDisposed Then Return
        Dim w As Integer = ShellStartMenuSize.Width
        Dim h As Integer = ShellStartMenuSize.Height
        Dim y As Integer = Math.Max(0, ClientSize.Height - ShellStartMenuTopFromWorkBottom)
        Dim x As Integer = Math.Max(0, ClientSize.Width - rightInset - w)
        btn.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        btn.Size = New Size(w, h)
        btn.Location = New Point(x, y)
        Try
            Dim std As LCARS.Controls.StandardButton = TryCast(btn, LCARS.Controls.StandardButton)
            If std IsNot Nothing Then
                std.ButtonText = "CLOSE"
                std.Text = "CLOSE"
                std.ButtonTextAlign = ContentAlignment.BottomRight
            End If
            Dim flat As LCARS.Controls.FlatButton = TryCast(btn, LCARS.Controls.FlatButton)
            If flat IsNot Nothing Then
                flat.ButtonText = "CLOSE"
                flat.Text = "CLOSE"
                flat.ButtonTextAlign = ContentAlignment.BottomRight
            End If
        Catch
        End Try
        btn.Visible = True
        btn.BringToFront()
    End Sub

    Protected Overridable Sub OnColorsChange()
        LCARS.UpdateColors(Me)
    End Sub
    Protected Overridable Sub OnBeepingUpdate(ByVal beep As Boolean)
        LCARS.SetBeeping(Me, beep)
    End Sub

    Protected Overridable Sub OnAlertInitiated(ByVal alertID As Integer)
        RaiseEvent AlertInitiated(alertID)
    End Sub

    Protected Overridable Sub OnAlertEnded()
        RaiseEvent AlertEnded()
    End Sub

    Protected Overridable Sub OnLCARSClosing()
        Me.Close()
    End Sub

    Public Sub New()
        Me.BackColor = Color.Black
        Me.ForeColor = Color.Orange
    End Sub
End Class
