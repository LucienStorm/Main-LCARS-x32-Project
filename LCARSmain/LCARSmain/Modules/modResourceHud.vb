Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Drawing
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Windows.Forms
Imports LCARS
Imports LCARS.Controls
Imports Microsoft.VisualBasic.CompilerServices
    <StandardModule>
    Friend NotInheritable Class modResourceHud
        Private Structure FILETIME
            Public dwLowDateTime As UInteger

            Public dwHighDateTime As UInteger
        End Structure

        <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Auto)>
        Private Structure MEMORYSTATUSEX
            Public dwLength As UInteger

            Public dwMemoryLoad As UInteger

            Public ullTotalPhys As ULong

            Public ullAvailPhys As ULong

            Public ullTotalPageFile As ULong

            Public ullAvailPageFile As ULong

            Public ullTotalVirtual As ULong

            Public ullAvailVirtual As ULong

            Public ullAvailExtendedVirtual As ULong
        End Structure

        Private Const HudName As String = "pnlResourceHud"

        Private Const ArrowName As String = "abResourceHudShow"

        Private Const DoubleTapMs As Integer = 350

        Private Const SampleMs As Integer = 1000

        Private Const PrefFileName As String = "resource-hud.txt"

        Private Shared ReadOnly attachedScreens As HashSet(Of Integer) = New HashSet(Of Integer)()

        Private Shared ReadOnly hudByScreen As Dictionary(Of Integer, Panel) = New Dictionary(Of Integer, Panel)()

        Private Shared ReadOnly arrowByScreen As Dictionary(Of Integer, ArrowButton) = New Dictionary(Of Integer, ArrowButton)()

        Private Shared ReadOnly visibleByScreen As Dictionary(Of Integer, Boolean) = New Dictionary(Of Integer, Boolean)()

        Private Shared cpuCounterReady As Boolean = False

        Private Shared lastIdle As Long = 0L

        Private Shared lastKernel As Long = 0L

        Private Shared lastUser As Long = 0L

        Private Shared lastCpuPct As Integer = 0

        Private Shared lastRamPct As Integer = 0

        Private Shared sampleTimer As Timer

        <DllImport("kernel32.dll", SetLastError:=True)>
        Private Shared Function GetSystemTimes(ByRef idleTime As FILETIME, ByRef kernelTime As FILETIME, ByRef userTime As FILETIME) As Boolean
        End Function

        <DllImport("kernel32.dll", CharSet:=CharSet.Auto, SetLastError:=True)>
        Private Shared Function GlobalMemoryStatusEx(ByRef lpBuffer As MEMORYSTATUSEX) As Boolean
        End Function

        Public Shared Sub DetachScreen(screenIndex As Integer)
            attachedScreens.Remove(screenIndex)
            hudByScreen.Remove(screenIndex)
            arrowByScreen.Remove(screenIndex)
        End Sub

        Public Shared Sub AttachResourceHud(b As modBusiness)
            If b Is Nothing OrElse b.myForm Is Nothing OrElse Not String.Equals(b.myForm.Name, "frmMainscreen1", StringComparison.OrdinalIgnoreCase) Then
                Return
            End If
            If attachedScreens.Contains(b.ScreenIndex) AndAlso hudByScreen.ContainsKey(b.ScreenIndex) Then
                Dim existingHud As Panel = hudByScreen(b.ScreenIndex)
                If existingHud IsNot Nothing AndAlso Not existingHud.IsDisposed Then
                    SyncResourceHudLayout(b)
                    Return
                End If
                DetachScreen(b.ScreenIndex)
            End If
            Dim control = FindNamed(b.myForm, "pnlMainContainer")
            If control IsNot Nothing Then
                Dim panel As Panel = TryCast(FindNamed(b.myForm, "pnlResourceHud"), Panel)
                If panel Is Nothing Then
                    panel = CreateHudPanel()
                    control.Controls.Add(panel)
                End If
                Dim arrowButton As ArrowButton = TryCast(FindNamed(b.myForm, "abResourceHudShow"), ArrowButton)
                If arrowButton Is Nothing Then
                    arrowButton = CreateShowArrow()
                    control.Controls.Add(arrowButton)
                End If
                hudByScreen(b.ScreenIndex) = panel
                arrowByScreen(b.ScreenIndex) = arrowButton
                Dim flag As Boolean = LoadVisiblePreference()
                visibleByScreen(b.ScreenIndex) = flag
                ApplyVisibility(b.ScreenIndex, flag)
                WireHudGestures(b, panel)
                AddHandler arrowButton.Click, Sub(s, e)
                                                  SetVisible(b.ScreenIndex, visible:=True)
                                                  SyncResourceHudLayout(b)
                                              End Sub
                attachedScreens.Add(b.ScreenIndex)
                Call EnsureSampleTimer()
                SyncResourceHudLayout(b)
                UpdateHudText(panel)
            End If
        End Sub

        Public Shared Sub SyncResourceHudLayoutPublic(b As modBusiness)
            SyncResourceHudLayout(b)
        End Sub

        Public Shared Sub LaunchTaskManager()
            Dim text = Path.Combine(Application.StartupPath, "LCARSTaskManager.exe")
            If Not File.Exists(text) Then
                Try
                    Process.Start("taskmgr")
                    Return
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    Call ProjectData.ClearProjectError()
                    Return
                End Try
            End If
            Try
                Dim processesByName = Process.GetProcessesByName("LCARSTaskManager")
                For Each process In processesByName
                    Try
                        If process.MainWindowHandle <> IntPtr.Zero Then
                            NativeShowWindow(process.MainWindowHandle)
                            Return
                        End If
                    Catch projectError2 As Exception
                        ProjectData.SetProjectError(projectError2)
                        Call ProjectData.ClearProjectError()
                    End Try
                Next
                Process.Start(text)
            Catch projectError3 As Exception
                ProjectData.SetProjectError(projectError3)
                Try
                    Process.Start("taskmgr")
                Catch projectError4 As Exception
                    ProjectData.SetProjectError(projectError4)
                    Call ProjectData.ClearProjectError()
                End Try
                Call ProjectData.ClearProjectError()
            End Try
        End Sub

        Private Shared Sub NativeShowWindow(hwnd As IntPtr)
            Try
                SetForegroundWindow(hwnd)
                ShowWindow(hwnd, 9)
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
        End Sub

        <DllImport("user32.dll")>
        Private Shared Function SetForegroundWindow(hWnd As IntPtr) As Boolean
        End Function

        <DllImport("user32.dll")>
        Private Shared Function ShowWindow(hWnd As IntPtr, nCmdShow As Integer) As Boolean
        End Function

        Private Shared Function CreateHudPanel() As Panel
            Dim panel As Panel = New Panel()
            panel.Name = "pnlResourceHud"
            panel.BackColor = Color.Black
            Dim size As Size = New Size(160, 48)
            panel.Size = size
            panel.Visible = True
            Dim label As Label = New Label()
            label.Name = "lblHudCpu"
            label.AutoSize = False
            label.Font = New Font("LCARS", 14F, FontStyle.Regular, GraphicsUnit.Point, 0)
            label.ForeColor = Color.Orange
            label.BackColor = Color.Black
            label.TextAlign = ContentAlignment.MiddleLeft
            label.Dock = DockStyle.Top
            label.Height = 22
            label.Text = "CPU --%"
            Dim label2 As Label = New Label()
            label2.Name = "lblHudRam"
            label2.AutoSize = False
            label2.Font = New Font("LCARS", 14F, FontStyle.Regular, GraphicsUnit.Point, 0)
            label2.ForeColor = Color.FromArgb(255, 153, 102)
            label2.BackColor = Color.Black
            label2.TextAlign = ContentAlignment.MiddleLeft
            label2.Dock = DockStyle.Fill
            label2.Text = "RAM --%"
            panel.Controls.Add(label2)
            panel.Controls.Add(label)
            Return panel
        End Function

        Private Shared Function CreateShowArrow() As ArrowButton
            Dim arrowButton As ArrowButton = New ArrowButton()
            arrowButton.Name = "abResourceHudShow"
            arrowButton.ArrowDirection = LCARSarrowDirection.Right
            arrowButton.Color = LCARScolorStyles.SystemFunction
            Dim size As Size = New Size(22, 22)
            arrowButton.Size = size
            arrowButton.Visible = False
            arrowButton.Beeping = True
            Return arrowButton
        End Function

        Private Shared Sub WireHudGestures(b As modBusiness, hud As Panel)
            Dim timer As Timer = New Timer()
            timer.Interval = 350
            Dim flag = False
            AddHandler timer.Tick, Sub(sender, e)
                                       timer.Stop()
                                       flag = False
                                       Dim flag2 = True
                                       If visibleByScreen.ContainsKey(b.ScreenIndex) Then
                                           flag2 = visibleByScreen(b.ScreenIndex)
                                       End If
                                       SetVisible(b.ScreenIndex, Not flag2)
                                       SyncResourceHudLayout(b)
                                   End Sub
            Dim value As MouseEventHandler = Sub(sender, e)
                                                 If e.Button = MouseButtons.Left Then
                                                     If flag Then
                                                         timer.Stop()
                                                         flag = False
                                                         Call LaunchTaskManager()
                                                     Else
                                                         flag = True
                                                         timer.Stop()
                                                         timer.Start()
                                                     End If
                                                 End If
                                             End Sub
            AddHandler hud.MouseUp, value
            For Each control As Control In hud.Controls
                AddHandler control.MouseUp, value
            Next
        End Sub

        Private Shared Sub SyncResourceHudLayout(b As modBusiness)
            If b Is Nothing OrElse b.myForm Is Nothing Then
                Return
            End If
            Dim value As Panel = Nothing
            Dim value2 As ArrowButton = Nothing
            If Not hudByScreen.TryGetValue(b.ScreenIndex, value) Then
                Return
            End If
            arrowByScreen.TryGetValue(b.ScreenIndex, value2)
            Dim control = FindNamed(b.myForm, "pnlMainContainer")
            Dim control2 = FindNamed(b.myForm, "pnlBatt")
            Dim control3 = FindNamed(b.myForm, "myClock")
            Dim myWeather = b.myWeather
            If control Is Nothing OrElse value Is Nothing Then
                Return
            End If
            If value.Parent IsNot control Then
                If value.Parent IsNot Nothing Then
                    value.Parent.Controls.Remove(value)
                End If
                control.Controls.Add(value)
            End If
            If value2 IsNot Nothing AndAlso value2.Parent IsNot control Then
                If value2.Parent IsNot Nothing Then
                    value2.Parent.Controls.Remove(value2)
                End If
                control.Controls.Add(value2)
            End If
            Dim control4 = FindNamed(b.myForm, "pnlMainBar")
            If If(control4 IsNot Nothing AndAlso control4.Top <= 0, True, False) Then
                value.Visible = False
                If value2 IsNot Nothing Then
                    value2.Visible = False
                End If
                Return
            End If
            Dim num = 260
            If control2 IsNot Nothing Then
                num = control2.Right + 8
            End If
            Dim num2 = control.ClientSize.Width - 200
            If myWeather IsNot Nothing AndAlso myWeather.Visible AndAlso myWeather.Parent Is control Then
                num2 = Math.Min(num2, myWeather.Left - 8)
            ElseIf control3 IsNot Nothing Then
                Dim text = control3.Text
                Dim text2 = If(Not Equals(text, Nothing), text, "")
                Dim width = TextRenderer.MeasureText(If(String.IsNullOrEmpty(text2), "000000.00", text2), control3.Font).Width
                num2 = Math.Min(num2, control3.Right - width - 12)
            End If
            Dim num3 = Math.Max(0, num2 - num)
            Dim num4 = 12
            If control3 IsNot Nothing Then
                num4 = Math.Max(4, control3.Top + Math.Max(0, Math.Min(66, control3.Height) - 48 / 2))
            End If
            Dim flag = True
            If visibleByScreen.ContainsKey(b.ScreenIndex) Then
                flag = visibleByScreen(b.ScreenIndex)
            End If
            If flag AndAlso num3 >= 90 Then
                value.Visible = True
                Dim panel = value
                Dim location As Point = New Point(num, num4)
                panel.Location = location
                Dim panel2 = value
                Dim size As Size = New Size(Math.Min(200, Math.Max(100, num3)), 48)
                panel2.Size = size
                value.BringToFront()
                If value2 IsNot Nothing Then
                    value2.Visible = False
                End If
            Else
                value.Visible = False
                If value2 IsNot Nothing Then
                    value2.Visible = True
                    Dim arrowButton = value2
                    Dim location As Point = New Point(num, num4 + 12)
                    arrowButton.Location = location
                    value2.BringToFront()
                End If
            End If
        End Sub

        Private Shared Sub SetVisible(screenIndex As Integer, visible As Boolean)
            visibleByScreen(screenIndex) = visible
            SaveVisiblePreference(visible)
            ApplyVisibility(screenIndex, visible)
        End Sub

        Private Shared Sub ApplyVisibility(screenIndex As Integer, visible As Boolean)
            Dim value As Panel = Nothing
            Dim value2 As ArrowButton = Nothing
            hudByScreen.TryGetValue(screenIndex, value)
            arrowByScreen.TryGetValue(screenIndex, value2)
            If value IsNot Nothing Then
                value.Visible = visible
            End If
            If value2 IsNot Nothing Then
                value2.Visible = Not visible
            End If
        End Sub

        Private Shared Sub EnsureSampleTimer()
            If sampleTimer Is Nothing Then
                sampleTimer = New Timer()
                sampleTimer.Interval = 1000
                AddHandler sampleTimer.Tick, AddressOf OnSampleTick
                Call sampleTimer.Start()
                Call SampleOnce()
            End If
        End Sub

        Private Shared Sub OnSampleTick(sender As Object, e As EventArgs)
            Call SampleOnce()
            For Each item In hudByScreen
                If item.Value IsNot Nothing AndAlso Not item.Value.IsDisposed AndAlso item.Value.Visible Then
                    UpdateHudText(item.Value)
                End If
            Next
        End Sub

        Private Shared Sub SampleOnce()
            lastRamPct = SampleRamPercent()
            lastCpuPct = SampleCpuPercent()
        End Sub

        Private Shared Function SampleRamPercent() As Integer
            Try
                Dim lpBuffer As MEMORYSTATUSEX = New MEMORYSTATUSEX With {
    .dwLength = Marshal.SizeOf(GetType(MEMORYSTATUSEX))
}
                If GlobalMemoryStatusEx(lpBuffer) Then
                    Return Math.Max(0, Math.Min(100, CInt(lpBuffer.dwMemoryLoad)))
                End If
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
            Return lastRamPct
        End Function

        Private Shared Function SampleCpuPercent() As Integer
            Dim result As Integer
            Try
                Dim idleTime As FILETIME = Nothing
                Dim kernelTime As FILETIME = Nothing
                Dim userTime As FILETIME = Nothing
                If Not GetSystemTimes(idleTime, kernelTime, userTime) Then
                    result = lastCpuPct
                Else
                    Dim num = FileTimeToLong(idleTime)
                    Dim num2 = FileTimeToLong(kernelTime)
                    Dim num3 = FileTimeToLong(userTime)
                    If Not cpuCounterReady Then
                        lastIdle = num
                        lastKernel = num2
                        lastUser = num3
                        cpuCounterReady = True
                        result = lastCpuPct
                    Else
                        Dim num4 = num - lastIdle
                        Dim num5 = num2 - lastKernel
                        Dim num6 = num3 - lastUser
                        lastIdle = num
                        lastKernel = num2
                        lastUser = num3
                        Dim num7 = num5 + num6
                        If num7 <= 0 Then
                            result = lastCpuPct
                        Else
                            Dim num8 = num7 - num4
                            If num8 < 0 Then
                                num8 = 0L
                            End If
                            result = Math.Max(0, Math.Min(100, CInt(num8 * 100 / num7)))
                        End If
                    End If
                End If
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                result = lastCpuPct
                Call ProjectData.ClearProjectError()
            End Try
            Return result
        End Function

        Private Shared Function FileTimeToLong(ft As FILETIME) As Long
            Return CULng(ft.dwHighDateTime) << 32 Or ft.dwLowDateTime
        End Function

        Private Shared Sub UpdateHudText(hud As Panel)
            If hud IsNot Nothing Then
                Dim control = FindChild(hud, "lblHudCpu")
                Dim control2 = FindChild(hud, "lblHudRam")
                If control IsNot Nothing Then
                    control.Text = "CPU  " & lastCpuPct.ToString() & "%"
                End If
                If control2 IsNot Nothing Then
                    control2.Text = "RAM  " & lastRamPct.ToString() & "%"
                End If
            End If
        End Sub

        Private Shared Function PrefPath() As String
            Dim text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "LCARS x32")
            If Not Directory.Exists(text) Then
                Try
                    Directory.CreateDirectory(text)
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return Path.Combine(text, "resource-hud.txt")
        End Function

        Private Shared Function LoadVisiblePreference() As Boolean
            Dim result As Boolean
            Try
                Dim path As String = PrefPath()
                If Not File.Exists(path) Then
                    result = True
                Else
                    Dim text As String = File.ReadAllText(path).Trim()
                    result = If(Operators.CompareString(text, "0", TextCompare:=False) <> 0 AndAlso Not String.Equals(text, "false", StringComparison.OrdinalIgnoreCase), True, False)
                End If
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                result = True
                Call ProjectData.ClearProjectError()
            End Try
            Return result
        End Function

        Private Shared Sub SaveVisiblePreference(visible As Boolean)
            Try
                Call File.WriteAllText(PrefPath(), If(visible, "1", "0"))
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
        End Sub

        Private Shared Function FindNamed(root As Control, name As String) As Control
            Dim array = root.Controls.Find(name, searchAllChildren:=True)
            If array.Length > 0 Then
                Return array(0)
            End If
            Return Nothing
        End Function

        Private Shared Function FindChild(parent As Control, name As String) As Control
            For Each control As Control In parent.Controls
                If String.Equals(control.Name, name, StringComparison.OrdinalIgnoreCase) Then
                    Return control
                End If
            Next
            Return Nothing
        End Function
    End Class

