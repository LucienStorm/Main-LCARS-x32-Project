Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Drawing
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Windows.Forms
Imports Microsoft.VisualBasic.CompilerServices
    Public Class DesktopHiddenPanel
        Inherits Panel
        Private Delegate Function EnumWindowsProc(hWnd As IntPtr, lParam As IntPtr) As Boolean

        Private Class HiddenRow
            Public Hwnd As IntPtr

            Public Pid As Integer

            Public ProcessName As String

            Public Title As String

            Public Reason As String

            Public MemMb As Long

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Private Const GWL_EXSTYLE As Integer = -20

        Private Const GW_OWNER As Integer = 4

        Private Const WS_EX_TOOLWINDOW As Integer = 128

        Private Const WS_EX_APPWINDOW As Integer = 262144

        Private Const WS_EX_NOREDIRECTIONBITMAP As Integer = 33554432

        Private Const DWMWA_CLOAKED As Integer = 14

        Private Const WM_CLOSE As Integer = 16

        Private ReadOnly lst As ListView

        Private ReadOnly status As Label

        Private ReadOnly hint As Label

        Private _busy As Boolean

        <DllImport("user32.dll")>
        Private Shared Function EnumWindows(lpEnumFunc As EnumWindowsProc, lParam As IntPtr) As Boolean
        End Function

        <DllImport("user32.dll")>
        Private Shared Function IsWindowVisible(hWnd As IntPtr) As Boolean
        End Function

        <DllImport("user32.dll", SetLastError:=True)>
        Private Shared Function GetWindow(hWnd As IntPtr, uCmd As Integer) As IntPtr
        End Function

        <DllImport("user32.dll", SetLastError:=True)>
        Private Shared Function GetParent(hWnd As IntPtr) As IntPtr
        End Function

        <DllImport("user32.dll", EntryPoint:="GetWindowLong")>
        Private Shared Function GetWindowLong32(hWnd As IntPtr, nIndex As Integer) As Integer
        End Function

        <DllImport("user32.dll", EntryPoint:="GetWindowLongPtr")>
        Private Shared Function GetWindowLongPtr64(hWnd As IntPtr, nIndex As Integer) As IntPtr
        End Function

        <DllImport("user32.dll", CharSet:=CharSet.Auto, SetLastError:=True)>
        Private Shared Function GetWindowText(hWnd As IntPtr, lpString As StringBuilder, nMaxCount As Integer) As Integer
        End Function

        <DllImport("user32.dll", CharSet:=CharSet.Auto, SetLastError:=True)>
        Private Shared Function GetWindowTextLength(hWnd As IntPtr) As Integer
        End Function

        <DllImport("user32.dll", SetLastError:=True)>
        Private Shared Function GetWindowThreadProcessId(hWnd As IntPtr, ByRef lpdwProcessId As Integer) As Integer
        End Function

        <DllImport("user32.dll")>
        Private Shared Function IsHungAppWindow(hWnd As IntPtr) As Boolean
        End Function

        <DllImport("user32.dll", CharSet:=CharSet.Auto)>
        Private Shared Function SendMessage(hWnd As IntPtr, msg As Integer, wParam As IntPtr, lParam As IntPtr) As IntPtr
        End Function

        <DllImport("dwmapi.dll")>
        Private Shared Function DwmGetWindowAttribute(hwnd As IntPtr, dwAttribute As Integer, ByRef pvAttribute As Integer, cbAttribute As Integer) As Integer
        End Function

        Public Sub New()
            lst = New ListView()
            status = New Label()
            hint = New Label()
            MyBase.BackColor = Color.Black
            MyBase.Dock = DockStyle.Fill
            hint.ForeColor = Color.FromArgb(255, 153, 0)
            hint.BackColor = Color.Black
            hint.Font = New Font("Arial Narrow", 9F, FontStyle.Bold)
            hint.Text = "WINDOWS DESKTOP / TASKBAR ONLY — HIDDEN FROM LCARS TASKBAR"
            hint.AutoSize = False
            status.ForeColor = Color.Orange
            status.BackColor = Color.Black
            status.Font = New Font("Arial Narrow", 10F, FontStyle.Bold)
            status.Text = "READY"
            status.AutoSize = False
            lst.View = View.Details
            lst.FullRowSelect = True
            lst.MultiSelect = False
            lst.HideSelection = False
            lst.HeaderStyle = ColumnHeaderStyle.Nonclickable
            lst.BackColor = Color.Black
            lst.ForeColor = Color.Orange
            lst.BorderStyle = BorderStyle.None
            lst.Font = New Font("Arial Narrow", 11F, FontStyle.Bold)
            lst.Columns.Add("NAME", 140)
            lst.Columns.Add("PID", 60)
            lst.Columns.Add("MEM MB", 70)
            lst.Columns.Add("WHY HIDDEN", 160)
            lst.Columns.Add("TITLE", 220)
            Controls.Add(hint)
            Controls.Add(lst)
            Controls.Add(status)
            AddHandler Resize, AddressOf OnPanelResize
        End Sub

        Public Function TryGetSelection(ByRef pid As Integer, ByRef hwnd As IntPtr, ByRef processName As String) As Boolean
            pid = -1
            hwnd = IntPtr.Zero
            processName = ""
            If lst.SelectedItems.Count = 0 Then
                Return False
            End If
            Dim hiddenRow As HiddenRow = Nothing

            If Not (CSharpImpl.__Assign(hiddenRow, TryCast(lst.SelectedItems(0).Tag, HiddenRow)) IsNot Nothing) Then
                Return False
            End If
            pid = hiddenRow.Pid
            hwnd = hiddenRow.Hwnd
            processName = hiddenRow.ProcessName
            Return True
        End Function

        Public Sub RefreshList()
            If _busy Then
                Return
            End If
            _busy = True
            Dim hiddenRow As HiddenRow = Nothing
            Try
                Dim intPtr = IntPtr.Zero

                If lst.SelectedItems.Count > 0 AndAlso CSharpImpl.__Assign(hiddenRow, TryCast(lst.SelectedItems(0).Tag, HiddenRow)) IsNot Nothing Then
                    intPtr = hiddenRow.Hwnd
                End If
                Dim list As List(Of HiddenRow) = EnumerateHiddenFromLcars()
                list.Sort(Function(a, b) String.Compare(a.ProcessName, b.ProcessName, StringComparison.OrdinalIgnoreCase))
                lst.BeginUpdate()
                lst.Items.Clear()
                For Each item In list
                    Dim listViewItem As ListViewItem = New ListViewItem(item.ProcessName)
                    listViewItem.SubItems.Add(item.Pid.ToString())
                    listViewItem.SubItems.Add(item.MemMb.ToString())
                    listViewItem.SubItems.Add(item.Reason)
                    Dim subItems = listViewItem.SubItems
                    Dim hiddenRow2 = item
                    subItems.Add(If(Not Equals(hiddenRow2.Title, Nothing), hiddenRow2.Title, ""))
                    listViewItem.Tag = item
                    lst.Items.Add(listViewItem)
                    If intPtr <> IntPtr.Zero AndAlso item.Hwnd = intPtr Then
                        listViewItem.Selected = True
                        listViewItem.EnsureVisible()
                    End If
                Next
                lst.EndUpdate()
                status.Text = list.Count.ToString() & " DESKTOP-ONLY WINDOW(S)"
            Catch ex As Exception
                ProjectData.SetProjectError(ex)
                Dim ex2 = ex
                status.Text = "REFRESH ERROR: " & ex2.Message
                Call ProjectData.ClearProjectError()
            Finally
                _busy = False
            End Try
        End Sub

        Public Function TryEndSelected() As String
            Dim pid = -1
            Dim hwnd = IntPtr.Zero
            Dim processName = ""
            Dim result As String
            If Not TryGetSelection(pid, hwnd, processName) Then
                status.Text = "SELECT A WINDOW"
                result = status.Text
            Else
                Try
                    If hwnd <> IntPtr.Zero Then
                        SendMessage(hwnd, 16, IntPtr.Zero, IntPtr.Zero)
                    End If
                    Using process = Process.GetProcessById(pid)
                        If Not process.HasExited AndAlso Not process.WaitForExit(800) Then
                            process.Kill()
                        End If
                    End Using
                    Dim text As String = "ENDED " & processName & " (" & pid.ToString() & ")"
                    RefreshList()
                    status.Text = text
                    result = text
                Catch ex As Exception
                    ProjectData.SetProjectError(ex)
                    Dim ex2 = ex
                    status.Text = "END FAILED: " & ex2.Message
                    result = status.Text
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Private Sub OnPanelResize(sender As Object, e As EventArgs)
            Dim label = hint
            Dim location As Point = New Point(0, 0)
            label.Location = location
            Dim label2 = hint
            Dim size As Size = New Size(Math.Max(40, ClientSize.Width), 18)
            label2.Size = size
            location = (CSharpImpl.__Assign(status.Location, New Point(0, Math.Max(20, ClientSize.Height - 18))))
            Dim label3 = status
            Dim size2 As Size = New Size(Math.Max(40, ClientSize.Width), 16)
            label3.Size = size2
            Dim listView = lst
            location = New Point(0, 22)
            listView.Location = location
            Dim listView2 = lst
            Dim size3 As Size = New Size(Math.Max(40, ClientSize.Width), Math.Max(40, ClientSize.Height - 42))
            listView2.Size = size3
            If lst.Columns.Count >= 5 Then
                Dim num = lst.ClientSize.Width
                lst.Columns(0).Width = Math.Max(100, CInt(Math.Round(num * 0.22)))
                lst.Columns(1).Width = 60
                lst.Columns(2).Width = 70
                lst.Columns(3).Width = Math.Max(100, CInt(Math.Round(num * 0.28)))
                lst.Columns(4).Width = Math.Max(80, num - lst.Columns(0).Width - lst.Columns(3).Width - 140)
            End If
        End Sub

        Private Function EnumerateHiddenFromLcars() As List(Of HiddenRow)
            Dim list As List(Of HiddenRow) = New List(Of HiddenRow)()
            Call EnumWindows(Function(hWnd, lParam)
                                 Dim reason As String = Nothing
                                 If Not IsDesktopTaskbarCandidate(hWnd) Then
                                     Return True
                                 End If
                                 If LcarsWouldShow(hWnd, reason) Then
                                     Return True
                                 End If
                                 Dim lpdwProcessId = 0
                                 GetWindowThreadProcessId(hWnd, lpdwProcessId)
                                 Dim processName = "?"
                                 Dim memMb = 0L
                                 If lpdwProcessId > 0 Then
                                     Try
                                         Dim process = Process.GetProcessById(lpdwProcessId)
                                         processName = process.ProcessName
                                         Try
                                             memMb = process.WorkingSet64 / 1048576
                                         Catch projectError As Exception
                                             ProjectData.SetProjectError(projectError)
                                             Call ProjectData.ClearProjectError()
                                         End Try
                                     Catch projectError2 As Exception
                                         ProjectData.SetProjectError(projectError2)
                                         Call ProjectData.ClearProjectError()
                                     End Try
                                 End If
                                 Dim title = ReadTitle(hWnd)
                                 list.Add(New HiddenRow With {
                         .Hwnd = hWnd,
                         .Pid = lpdwProcessId,
                         .ProcessName = processName,
                         .Title = title,
                         .Reason = If(Not Equals(reason, Nothing), reason, "FILTERED"),
                         .MemMb = memMb
                     })
                                 Return True
                             End Function, IntPtr.Zero)
            Return list
        End Function

        Private Function IsDesktopTaskbarCandidate(hWnd As IntPtr) As Boolean
            If hWnd = IntPtr.Zero Then
                Return False
            End If
            If Not IsWindowVisible(hWnd) Then
                Return False
            End If
            If GetParent(hWnd) <> IntPtr.Zero Then
                Return False
            End If
            Dim flag = GetWindow(hWnd, 4) = IntPtr.Zero
            Dim exStyle = GetExStyle(hWnd)
            Dim flag2 = (exStyle And &H80) = 128
            Dim flag3 = (exStyle And &H40000) = 262144
            If Not flag2 AndAlso flag OrElse flag3 AndAlso Not flag Then
                Return True
            End If
            Return False
        End Function

        ''' <summary>
        '''  Mirrors CommonScreen.fEnumWindowsCallBack filters. Returns True if LCARS taskbar
        '''  would list this window; otherwise sets reason and returns False.
        '''  </summary>
        Private Function LcarsWouldShow(hWnd As IntPtr, ByRef reason As String) As Boolean
            reason = Nothing
            Dim reason2 As String = Nothing
            If IsBlockedShellWindow(hWnd, reason2) Then
                reason = reason2
                Return False
            End If
            If IsWindowCloaked(hWnd) Then
                reason = "CLOAKED UWP"
                Return False
            End If
            Dim exStyle = GetExStyle(hWnd)
            If (exStyle And &H2000000) = 33554432 AndAlso IsHungAppWindow(hWnd) Then
                reason = "HUNG MODERN APP"
                Return False
            End If
            If GetWindowTextLength(hWnd) = 0 Then
                reason = "NO CAPTION"
                Return False
            End If
            Return True
        End Function

        Private Function IsBlockedShellWindow(hWnd As IntPtr, ByRef reason As String) As Boolean
            reason = Nothing
            Dim lpdwProcessId = 0
            GetWindowThreadProcessId(hWnd, lpdwProcessId)
            If lpdwProcessId > 0 Then
                Try
                    Dim process = Process.GetProcessById(lpdwProcessId)
                    Select Case process.ProcessName.ToLowerInvariant()
                        Case "shellexperiencehost", "startmenuexperiencehost", "applicationframehost"
                            reason = "SHELL HOST"
                            Return True
                        Case "systemsettings", "searchui", "searchapp"
                            reason = "SETTINGS/SEARCH"
                            Return True
                        Case "explorer"
                            reason = "EXPLORER"
                            Return True
                    End Select
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Dim text = ReadTitle(hWnd)
            If text.IndexOf("Shell Experience Host", StringComparison.OrdinalIgnoreCase) >= 0 Then
                reason = "SHELL HOST"
                Return True
            End If
            Return False
        End Function

        Private Function IsWindowCloaked(hWnd As IntPtr) As Boolean
            Dim result As Boolean
            Try
                Dim pvAttribute = 0
                Dim num = DwmGetWindowAttribute(hWnd, 14, pvAttribute, 4)
                result = If(num = 0 AndAlso pvAttribute <> 0, True, False)
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                result = False
                Call ProjectData.ClearProjectError()
            End Try
            Return result
        End Function

        Private Function GetExStyle(hWnd As IntPtr) As Integer
            If IntPtr.Size = 8 Then
                Return CInt((GetWindowLongPtr64(hWnd, -20).ToInt64() And &HFFFFFFFFUI))
            End If
            Return GetWindowLong32(hWnd, -20)
        End Function

        Private Function ReadTitle(hWnd As IntPtr) As String
            Dim windowTextLength = GetWindowTextLength(hWnd)
            If windowTextLength <= 0 Then
                Return ""
            End If
            Dim stringBuilder As StringBuilder = New StringBuilder(windowTextLength + 1)
            GetWindowText(hWnd, stringBuilder, stringBuilder.Capacity)
            Return stringBuilder.ToString()
        End Function

        Private Class CSharpImpl
            Shared Function __Assign(Of T)(ByRef target As T, value As T) As T
                target = value
                Return value
            End Function
        End Class
    End Class

