' LCARSbuttons/LCARSbuttons/Diagnostics/ResourceMonitorPanel.vb
Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Windows.Forms

Namespace Diagnostics

''' <summary>Live CPU / RAM / disk meters for Task Manager and Engineering.</summary>
Public Class ResourceMonitorPanel
    Inherits Panel

    Private ReadOnly lblCpu As New Label()
    Private ReadOnly lblRam As New Label()
    Private ReadOnly lblDisk As New Label()
    Private ReadOnly barCpu As New ProgressBar()
    Private ReadOnly barRam As New ProgressBar()
    Private ReadOnly lstDisk As New ListView()
    Private ReadOnly refreshTimer As New Timer()

    Private cpuReady As Boolean = False
    Private lastIdle As Long = 0
    Private lastKernel As Long = 0
    Private lastUser As Long = 0

    <StructLayout(LayoutKind.Sequential)>
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

    <DllImport("kernel32.dll", SetLastError:=True)>
    Private Shared Function GetSystemTimes(ByRef idleTime As FILETIME, ByRef kernelTime As FILETIME, ByRef userTime As FILETIME) As Boolean
    End Function

    <DllImport("kernel32.dll", CharSet:=CharSet.Auto, SetLastError:=True)>
    Private Shared Function GlobalMemoryStatusEx(ByRef lpBuffer As MEMORYSTATUSEX) As Boolean
    End Function

    Public Sub New()
        BackColor = Color.Black
        Dock = DockStyle.Fill

        StyleLabel(lblCpu, "CPU")
        StyleLabel(lblRam, "RAM")
        StyleLabel(lblDisk, "DISKS")
        StyleBar(barCpu)
        StyleBar(barRam)

        lstDisk.View = View.Details
        lstDisk.FullRowSelect = True
        lstDisk.HeaderStyle = ColumnHeaderStyle.Nonclickable
        lstDisk.BackColor = Color.Black
        lstDisk.ForeColor = Color.Orange
        lstDisk.BorderStyle = BorderStyle.None
        lstDisk.Font = New Font("Arial Narrow", 11.0!, FontStyle.Bold)
        lstDisk.Columns.Add("DRIVE", 70)
        lstDisk.Columns.Add("USED %", 70)
        lstDisk.Columns.Add("FREE", 100)
        lstDisk.Columns.Add("TOTAL", 100)

        Controls.Add(lstDisk)
        Controls.Add(lblDisk)
        Controls.Add(barRam)
        Controls.Add(lblRam)
        Controls.Add(barCpu)
        Controls.Add(lblCpu)

        AddHandler Me.Resize, AddressOf OnPanelResize
        refreshTimer.Interval = 1000
        AddHandler refreshTimer.Tick, Sub() Sample()
        AddHandler Me.VisibleChanged, Sub()
                                          If Visible Then
                                              Sample()
                                              refreshTimer.Start()
                                          Else
                                              refreshTimer.Stop()
                                          End If
                                      End Sub
        LayoutChildren()
        Sample()
    End Sub

    Private Sub StyleLabel(ByVal lbl As Label, ByVal text As String)
        lbl.Text = text
        lbl.ForeColor = Color.Orange
        lbl.BackColor = Color.Black
        lbl.Font = New Font("LCARS", 14.0!, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        lbl.AutoSize = False
        lbl.TextAlign = ContentAlignment.MiddleLeft
    End Sub

    Private Sub StyleBar(ByVal bar As ProgressBar)
        bar.Minimum = 0
        bar.Maximum = 100
        bar.Height = 18
    End Sub

    Private Sub OnPanelResize(ByVal sender As Object, ByVal e As EventArgs)
        LayoutChildren()
    End Sub

    Private Sub LayoutChildren()
        Dim pad As Integer = 8
        Dim w As Integer = Math.Max(100, ClientSize.Width - pad * 2)
        Dim y As Integer = pad
        lblCpu.SetBounds(pad, y, w, 22) : y += 24
        barCpu.SetBounds(pad, y, w, 18) : y += 26
        lblRam.SetBounds(pad, y, w, 22) : y += 24
        barRam.SetBounds(pad, y, w, 18) : y += 28
        lblDisk.SetBounds(pad, y, w, 22) : y += 24
        lstDisk.SetBounds(pad, y, w, Math.Max(60, ClientSize.Height - y - pad))
        If lstDisk.Columns.Count >= 4 Then
            Dim cw As Integer = Math.Max(200, lstDisk.ClientSize.Width)
            lstDisk.Columns(0).Width = 70
            lstDisk.Columns(1).Width = 70
            lstDisk.Columns(2).Width = Math.Max(80, (cw - 140) \ 2)
            lstDisk.Columns(3).Width = Math.Max(80, (cw - 140) \ 2)
        End If
    End Sub

    Public Sub Sample()
        Dim cpu As Integer = SampleCpu()
        Dim ram As Integer = SampleRam()
        barCpu.Value = Math.Max(0, Math.Min(100, cpu))
        barRam.Value = Math.Max(0, Math.Min(100, ram))
        lblCpu.Text = "CPU  " & cpu.ToString() & "%"
        lblRam.Text = "RAM  " & ram.ToString() & "%"
        RefreshDisks()
    End Sub

    Private Function SampleCpu() As Integer
        Try
            Dim idle As FILETIME, kernel As FILETIME, user As FILETIME
            If Not GetSystemTimes(idle, kernel, user) Then Return barCpu.Value
            Dim idleT As Long = FileTimeToLong(idle)
            Dim kernelT As Long = FileTimeToLong(kernel)
            Dim userT As Long = FileTimeToLong(user)
            If Not cpuReady Then
                lastIdle = idleT : lastKernel = kernelT : lastUser = userT
                cpuReady = True
                Return barCpu.Value
            End If
            Dim idleD As Long = idleT - lastIdle
            Dim total As Long = (kernelT - lastKernel) + (userT - lastUser)
            lastIdle = idleT : lastKernel = kernelT : lastUser = userT
            If total <= 0 Then Return barCpu.Value
            Dim busy As Long = total - idleD
            If busy < 0 Then busy = 0
            Return CInt((busy * 100L) \ total)
        Catch
            Return barCpu.Value
        End Try
    End Function

    Private Function SampleRam() As Integer
        Try
            Dim st As New MEMORYSTATUSEX()
            st.dwLength = CUInt(Marshal.SizeOf(GetType(MEMORYSTATUSEX)))
            If GlobalMemoryStatusEx(st) Then Return CInt(st.dwMemoryLoad)
        Catch
        End Try
        Return barRam.Value
    End Function

    Private Sub RefreshDisks()
        Try
            lstDisk.BeginUpdate()
            lstDisk.Items.Clear()
            For Each d As DriveInfo In DriveInfo.GetDrives()
                Try
                    If Not d.IsReady Then Continue For
                    If d.DriveType <> DriveType.Fixed AndAlso d.DriveType <> DriveType.Removable Then Continue For
                    Dim total As Long = d.TotalSize
                    If total <= 0 Then Continue For
                    Dim free As Long = d.TotalFreeSpace
                    Dim usedPct As Integer = CInt(((total - free) * 100L) \ total)
                    Dim item As New ListViewItem(d.Name)
                    item.SubItems.Add(usedPct.ToString() & "%")
                    item.SubItems.Add(FormatBytes(free))
                    item.SubItems.Add(FormatBytes(total))
                    lstDisk.Items.Add(item)
                Catch
                End Try
            Next
            lstDisk.EndUpdate()
        Catch
        End Try
    End Sub

    Private Shared Function FormatBytes(ByVal bytes As Long) As String
        Dim gb As Double = bytes / (1024.0R * 1024.0R * 1024.0R)
        If gb >= 1.0R Then Return gb.ToString("0.0") & " GB"
        Dim mb As Double = bytes / (1024.0R * 1024.0R)
        Return mb.ToString("0") & " MB"
    End Function

    Private Shared Function FileTimeToLong(ByVal ft As FILETIME) As Long
        Return (CLng(ft.dwHighDateTime) << 32) Or CLng(ft.dwLowDateTime)
    End Function

    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        If disposing Then
            refreshTimer.Stop()
            refreshTimer.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub
End Class

End Namespace
