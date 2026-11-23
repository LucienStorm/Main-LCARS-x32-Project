' LCARSbuttons/LCARSbuttons/Diagnostics/MemoryCompositionPanel.vb
Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Management
Imports System.Runtime.InteropServices
Imports System.Windows.Forms

Namespace Diagnostics

''' <summary>
''' Resource Monitor–style physical memory composition + ~60s used-% sparkline.
''' Module lines show static capacity only (no per-DIMM usage).
''' </summary>
Public Class MemoryCompositionPanel
    Inherits Panel

    Private Structure MemSeg
        Public Name As String
        Public Bytes As Long
        Public Color As Color
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

    <DllImport("kernel32.dll", CharSet:=CharSet.Auto, SetLastError:=True)>
    Private Shared Function GlobalMemoryStatusEx(ByRef lpBuffer As MEMORYSTATUSEX) As Boolean
    End Function

    Private ReadOnly refreshTimer As New Timer()
    Private ReadOnly history As New List(Of Integer)()
    Private Const HistorySeconds As Integer = 60

    Private freeBytes As Long
    Private modifiedBytes As Long
    Private standbyBytes As Long
    Private inUseBytes As Long
    Private hardwareReservedBytes As Long
    Private availableBytes As Long
    Private cachedBytes As Long
    Private totalBytes As Long
    Private installedBytes As Long
    Private usedPercent As Integer
    Private moduleLines As String() = New String() {}

    Private ctrFree As PerformanceCounter
    Private ctrModified As PerformanceCounter
    Private ctrStandbyR As PerformanceCounter
    Private ctrStandbyN As PerformanceCounter
    Private ctrStandbyC As PerformanceCounter
    Private ctrCache As PerformanceCounter
    Private countersReady As Boolean = False

    Public Sub New()
        BackColor = Color.Black
        DoubleBuffered = True
        SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
        TryInitCounters()
        LoadModules()
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
        Sample()
    End Sub

    Private Sub TryInitCounters()
        Try
            ctrFree = New PerformanceCounter("Memory", "Free & Zero Page List Bytes", True)
            ctrModified = New PerformanceCounter("Memory", "Modified Page List Bytes", True)
            ctrStandbyR = New PerformanceCounter("Memory", "Standby Cache Reserve Bytes", True)
            ctrStandbyN = New PerformanceCounter("Memory", "Standby Cache Normal Priority Bytes", True)
            ctrStandbyC = New PerformanceCounter("Memory", "Standby Cache Core Bytes", True)
            ctrCache = New PerformanceCounter("Memory", "Cache Bytes", True)
            ctrFree.NextValue() : ctrModified.NextValue()
            ctrStandbyR.NextValue() : ctrStandbyN.NextValue() : ctrStandbyC.NextValue()
            ctrCache.NextValue()
            countersReady = True
        Catch
            countersReady = False
        End Try
    End Sub

    Private Sub LoadModules()
        Dim lines As New List(Of String)()
        Try
            Using q As New ManagementObjectSearcher("SELECT Capacity,Speed,BankLabel,Manufacturer FROM Win32_PhysicalMemory")
                Dim i As Integer = 0
                For Each o As ManagementObject In q.Get()
                    i += 1
                    Dim cap As Long = 0
                    Long.TryParse(If(o("Capacity"), "0").ToString(), cap)
                    Dim speed As String = If(o("Speed"), "").ToString()
                    Dim bank As String = If(o("BankLabel"), "").ToString()
                    Dim mfr As String = If(o("Manufacturer"), "").ToString().Trim()
                    Dim line As String = "MODULE " & i.ToString() & ": " & FormatBytes(cap)
                    If speed.Length > 0 Then line &= " @ " & speed & " MHz"
                    If bank.Length > 0 Then line &= "  [" & bank & "]"
                    If mfr.Length > 0 Then line &= "  " & mfr
                    lines.Add(line.ToUpperInvariant())
                    Try
                        installedBytes += cap
                    Catch
                    End Try
                Next
            End Using
        Catch
        End Try
        moduleLines = lines.ToArray()
    End Sub

    Public Sub Sample()
        Try
            Dim st As New MEMORYSTATUSEX()
            st.dwLength = CUInt(Marshal.SizeOf(GetType(MEMORYSTATUSEX)))
            If GlobalMemoryStatusEx(st) Then
                totalBytes = CLng(st.ullTotalPhys)
                availableBytes = CLng(st.ullAvailPhys)
                usedPercent = CInt(st.dwMemoryLoad)
            End If

            If installedBytes <= 0 Then installedBytes = totalBytes
            hardwareReservedBytes = Math.Max(0, installedBytes - totalBytes)

            If countersReady Then
                freeBytes = CLng(ctrFree.NextValue())
                modifiedBytes = CLng(ctrModified.NextValue())
                standbyBytes = CLng(ctrStandbyR.NextValue()) + CLng(ctrStandbyN.NextValue()) + CLng(ctrStandbyC.NextValue())
                cachedBytes = CLng(ctrCache.NextValue())
                Dim accounted As Long = freeBytes + modifiedBytes + standbyBytes
                inUseBytes = Math.Max(0, totalBytes - accounted)
                If availableBytes <= 0 Then availableBytes = freeBytes + standbyBytes
            Else
                freeBytes = availableBytes
                standbyBytes = 0
                modifiedBytes = 0
                inUseBytes = Math.Max(0, totalBytes - availableBytes)
                cachedBytes = 0
            End If

            history.Add(usedPercent)
            While history.Count > HistorySeconds
                history.RemoveAt(0)
            End While
            Invalidate()
        Catch
        End Try
    End Sub

    Protected Overrides Sub OnPaint(ByVal e As PaintEventArgs)
        MyBase.OnPaint(e)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.Clear(BackColor)

        Dim pad As Integer = 4
        Dim y As Integer = pad
        Using titleFont As New Font("LCARS", 12.0!, FontStyle.Regular, GraphicsUnit.Point)
            Using br As New SolidBrush(Color.Orange)
                g.DrawString("PHYSICAL MEMORY", titleFont, br, pad, y)
            End Using
        End Using
        y += 20

        Dim barH As Integer = 18
        Dim barW As Integer = Math.Max(40, Width - pad * 2)
        Dim segs As MemSeg() = {
            New MemSeg With {.Name = "HW RES", .Bytes = hardwareReservedBytes, .Color = Color.Gray},
            New MemSeg With {.Name = "IN USE", .Bytes = inUseBytes, .Color = Color.FromArgb(0, 180, 80)},
            New MemSeg With {.Name = "MODIFIED", .Bytes = modifiedBytes, .Color = Color.Orange},
            New MemSeg With {.Name = "STANDBY", .Bytes = standbyBytes, .Color = Color.FromArgb(40, 80, 180)},
            New MemSeg With {.Name = "FREE", .Bytes = freeBytes, .Color = Color.FromArgb(120, 180, 255)}
        }
        Dim denom As Long = installedBytes
        If denom <= 0 Then denom = totalBytes
        If denom <= 0 Then denom = 1
        Dim x As Integer = pad
        For Each s As MemSeg In segs
            Dim w As Integer = CInt((s.Bytes * barW) \ denom)
            If w < 1 AndAlso s.Bytes > 0 Then w = 1
            If w > 0 Then
                Using br As New SolidBrush(s.Color)
                    g.FillRectangle(br, x, y, w, barH)
                End Using
                x += w
            End If
        Next
        Using pen As New Pen(Color.Orange, 1)
            g.DrawRectangle(pen, pad, y, barW, barH)
        End Using
        y += barH + 6

        Using f As New Font("Arial Narrow", 9.0!, FontStyle.Bold)
            For Each s As MemSeg In segs
                Using br As New SolidBrush(s.Color)
                    g.FillRectangle(br, pad, y + 2, 10, 10)
                End Using
                Using br As New SolidBrush(Color.Orange)
                    g.DrawString(s.Name & "  " & FormatBytes(s.Bytes), f, br, pad + 14, y)
                End Using
                y += 14
            Next
            Using br As New SolidBrush(Color.Orange)
                g.DrawString("AVAILABLE  " & FormatBytes(availableBytes) & "    CACHED  " & FormatBytes(cachedBytes), f, br, pad, y) : y += 14
                g.DrawString("TOTAL  " & FormatBytes(totalBytes) & "    INSTALLED  " & FormatBytes(installedBytes), f, br, pad, y) : y += 16
            End Using
        End Using

        Dim sparkH As Integer = 36
        Dim sparkW As Integer = barW
        Using br As New SolidBrush(Color.FromArgb(20, 40, 20))
            g.FillRectangle(br, pad, y, sparkW, sparkH)
        End Using
        Using pen As New Pen(Color.FromArgb(60, 100, 60), 1)
            For i As Integer = 1 To 3
                Dim gy As Integer = y + (sparkH * i) \ 4
                g.DrawLine(pen, pad, gy, pad + sparkW, gy)
            Next
        End Using
        If history.Count > 1 Then
            Dim pts As New List(Of PointF)()
            For i As Integer = 0 To history.Count - 1
                Dim px As Single = pad + (CSng(i) / CSng(HistorySeconds - 1)) * sparkW
                Dim py As Single = y + sparkH - (history(i) / 100.0F) * sparkH
                pts.Add(New PointF(px, py))
            Next
            Using pen As New Pen(Color.Lime, 1.5F)
                If pts.Count >= 2 Then g.DrawLines(pen, pts.ToArray())
            End Using
            Dim fill As New List(Of PointF)(pts)
            fill.Add(New PointF(pts(pts.Count - 1).X, y + sparkH))
            fill.Add(New PointF(pts(0).X, y + sparkH))
            Using br As New SolidBrush(Color.FromArgb(80, 0, 255, 0))
                g.FillPolygon(br, fill.ToArray())
            End Using
        End If
        Using f As New Font("Arial Narrow", 8.0!, FontStyle.Bold)
            Using br As New SolidBrush(Color.Orange)
                g.DrawString("USED PHYSICAL  " & usedPercent.ToString() & "%  (60s)", f, br, pad, y + sparkH + 2)
            End Using
        End Using
        y += sparkH + 18

        Using f As New Font("Arial Narrow", 8.0!, FontStyle.Bold)
            Using br As New SolidBrush(Color.FromArgb(200, 180, 100))
                For Each line As String In moduleLines
                    If y > Height - 12 Then Exit For
                    g.DrawString(line, f, br, pad, y)
                    y += 12
                Next
            End Using
        End Using
    End Sub

    Private Shared Function FormatBytes(ByVal bytes As Long) As String
        If bytes < 0 Then bytes = 0
        Dim mb As Double = bytes / (1024.0R * 1024.0R)
        If mb >= 1024.0R Then Return (mb / 1024.0R).ToString("0.00") & " GB"
        Return mb.ToString("0") & " MB"
    End Function

    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        If disposing Then
            refreshTimer.Stop()
            refreshTimer.Dispose()
            TryDispose(ctrFree) : TryDispose(ctrModified)
            TryDispose(ctrStandbyR) : TryDispose(ctrStandbyN) : TryDispose(ctrStandbyC)
            TryDispose(ctrCache)
        End If
        MyBase.Dispose(disposing)
    End Sub

    Private Shared Sub TryDispose(ByVal c As PerformanceCounter)
        If c Is Nothing Then Return
        Try
            c.Dispose()
        Catch
        End Try
    End Sub
End Class

End Namespace
