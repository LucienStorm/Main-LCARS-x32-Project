' LCARSbuttons/LCARSbuttons/Diagnostics/DisplayService.vb
Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Management
Imports System.Runtime.InteropServices
Imports Microsoft.Win32

Namespace Diagnostics

Public Class DisplayModeInfo
    Public Width As Integer
    Public Height As Integer
    Public Frequency As Integer
    Public Overrides Function ToString() As String
        Return Width.ToString() & " × " & Height.ToString() & " @ " & Frequency.ToString() & "Hz"
    End Function
End Class

''' <summary>Brightness, rotation lock, resolution, and orientation for Engineering DISPLAY.</summary>
Public Module DisplayService

    Private Const KeyPath As String = "SOFTWARE\Microsoft\Windows\CurrentVersion\AutoRotation"
    Private Const ValueName As String = "Enable"
    Private Const AR_DISABLED As Integer = &H1
    Private Const ENUM_CURRENT_SETTINGS As Integer = -1
    Private Const CDS_UPDATEREGISTRY As Integer = &H1
    Private Const CDS_TEST As Integer = &H2
    Private Const DISP_CHANGE_SUCCESSFUL As Integer = 0
    Private Const DM_PELSWIDTH As Integer = &H80000
    Private Const DM_PELSHEIGHT As Integer = &H100000
    Private Const DM_DISPLAYFREQUENCY As Integer = &H400000
    Private Const DM_DISPLAYORIENTATION As Integer = &H80
    Private Const DMDO_DEFAULT As Integer = 0
    Private Const DMDO_90 As Integer = 1
    Private Const DMDO_180 As Integer = 2
    Private Const DMDO_270 As Integer = 3

    <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Ansi)>
    Private Structure DEVMODE
        <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=32)> Public dmDeviceName As String
        Public dmSpecVersion As Short
        Public dmDriverVersion As Short
        Public dmSize As Short
        Public dmDriverExtra As Short
        Public dmFields As Integer
        Public dmPositionX As Integer
        Public dmPositionY As Integer
        Public dmDisplayOrientation As Integer
        Public dmDisplayFixedOutput As Integer
        Public dmColor As Short
        Public dmDuplex As Short
        Public dmYResolution As Short
        Public dmTTOption As Short
        Public dmCollate As Short
        <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=32)> Public dmFormName As String
        Public dmLogPixels As Short
        Public dmBitsPerPel As Integer
        Public dmPelsWidth As Integer
        Public dmPelsHeight As Integer
        Public dmDisplayFlags As Integer
        Public dmDisplayFrequency As Integer
        Public dmICMMethod As Integer
        Public dmICMIntent As Integer
        Public dmMediaType As Integer
        Public dmDitherType As Integer
        Public dmReserved1 As Integer
        Public dmReserved2 As Integer
        Public dmPanningWidth As Integer
        Public dmPanningHeight As Integer
    End Structure

    <DllImport("user32.dll", EntryPoint:="SetAutoRotation", SetLastError:=True)>
    Private Function NativeSetAutoRotation(ByVal enable As Boolean) As Boolean
    End Function

    <DllImport("user32.dll", EntryPoint:="GetAutoRotationState", SetLastError:=True)>
    Private Function NativeGetAutoRotationState(ByRef pState As Integer) As Boolean
    End Function

    <DllImport("user32.dll", CharSet:=CharSet.Ansi)>
    Private Function EnumDisplaySettings(ByVal lpszDeviceName As String, ByVal iModeNum As Integer, ByRef lpDevMode As DEVMODE) As Boolean
    End Function

    <DllImport("user32.dll", CharSet:=CharSet.Ansi)>
    Private Function ChangeDisplaySettings(ByRef lpDevMode As DEVMODE, ByVal dwFlags As Integer) As Integer
    End Function

    Public Function GetBrightnessPercent() As Integer
        Try
            Using s As New ManagementObjectSearcher("root\WMI", "SELECT CurrentBrightness FROM WmiMonitorBrightness")
                For Each o As ManagementObject In s.Get()
                    Return CInt(o("CurrentBrightness"))
                Next
            End Using
        Catch
        End Try
        Return -1
    End Function

    Public Function SetBrightnessPercent(ByVal percent As Integer) As Boolean
        Try
            Dim level As UInteger = CUInt(Math.Max(0, Math.Min(100, percent)))
            Using s As New ManagementObjectSearcher("root\WMI", "SELECT * FROM WmiMonitorBrightnessMethods")
                For Each o As ManagementObject In s.Get()
                    o.InvokeMethod("WmiSetBrightness", New Object() {1UI, level})
                    Return True
                Next
            End Using
        Catch
        End Try
        Return False
    End Function

    Public Function IsBrightnessSupported() As Boolean
        Return GetBrightnessPercent() >= 0
    End Function

    Public Function IsRotationLocked() As Boolean
        Try
            Dim state As Integer = 0
            If NativeGetAutoRotationState(state) Then
                Return (state And AR_DISABLED) <> 0
            End If
        Catch
        End Try
        Try
            Using key As RegistryKey = Registry.CurrentUser.OpenSubKey(KeyPath, False)
                If key IsNot Nothing Then
                    Dim v As Object = key.GetValue(ValueName, 1)
                    Return Convert.ToInt32(v) = 0
                End If
            End Using
        Catch
        End Try
        Return False
    End Function

    Public Function SetRotationLocked(ByVal locked As Boolean) As Boolean
        Try
            NativeSetAutoRotation(Not locked)
        Catch
        End Try
        Try
            Using key As RegistryKey = Registry.CurrentUser.CreateSubKey(KeyPath)
                If key IsNot Nothing Then
                    key.SetValue(ValueName, If(locked, 0, 1), RegistryValueKind.DWord)
                End If
            End Using
            Return True
        Catch
            Return False
        End Try
    End Function

    Public Function GetCurrentMode() As DisplayModeInfo
        Dim dm As DEVMODE = NewDevMode()
        If Not EnumDisplaySettings(Nothing, ENUM_CURRENT_SETTINGS, dm) Then Return Nothing
        Dim info As New DisplayModeInfo()
        info.Width = dm.dmPelsWidth
        info.Height = dm.dmPelsHeight
        info.Frequency = dm.dmDisplayFrequency
        Return info
    End Function

    Public Function ListModes() As List(Of DisplayModeInfo)
        Dim map As New Dictionary(Of String, DisplayModeInfo)(StringComparer.OrdinalIgnoreCase)
        Dim i As Integer = 0
        While True
            Dim dm As DEVMODE = NewDevMode()
            If Not EnumDisplaySettings(Nothing, i, dm) Then Exit While
            i += 1
            If dm.dmBitsPerPel < 32 Then Continue While
            If dm.dmPelsWidth < 800 OrElse dm.dmPelsHeight < 600 Then Continue While
            Dim key As String = dm.dmPelsWidth.ToString() & "x" & dm.dmPelsHeight.ToString() & "@" & dm.dmDisplayFrequency.ToString()
            If map.ContainsKey(key) Then Continue While
            Dim info As New DisplayModeInfo()
            info.Width = dm.dmPelsWidth
            info.Height = dm.dmPelsHeight
            info.Frequency = dm.dmDisplayFrequency
            map.Add(key, info)
        End While
        Dim list As New List(Of DisplayModeInfo)(map.Values)
        list.Sort(Function(a, b)
                      If a.Width <> b.Width Then Return b.Width.CompareTo(a.Width)
                      If a.Height <> b.Height Then Return b.Height.CompareTo(a.Height)
                      Return b.Frequency.CompareTo(a.Frequency)
                  End Function)
        Return list
    End Function

    Public Function ApplyMode(ByVal mode As DisplayModeInfo) As String
        If mode Is Nothing Then Return "no mode"
        Dim dm As DEVMODE = NewDevMode()
        If Not EnumDisplaySettings(Nothing, ENUM_CURRENT_SETTINGS, dm) Then Return "cannot read current mode"
        dm.dmFields = DM_PELSWIDTH Or DM_PELSHEIGHT Or DM_DISPLAYFREQUENCY
        dm.dmPelsWidth = mode.Width
        dm.dmPelsHeight = mode.Height
        dm.dmDisplayFrequency = mode.Frequency
        Dim test As Integer = ChangeDisplaySettings(dm, CDS_TEST)
        If test <> DISP_CHANGE_SUCCESSFUL Then Return "mode not supported"
        Dim apply As Integer = ChangeDisplaySettings(dm, CDS_UPDATEREGISTRY)
        If apply = DISP_CHANGE_SUCCESSFUL Then Return "OK"
        Return "change failed (" & apply.ToString() & ")"
    End Function

    Public Function GetOrientationDegrees() As Integer
        Dim dm As DEVMODE = NewDevMode()
        If Not EnumDisplaySettings(Nothing, ENUM_CURRENT_SETTINGS, dm) Then Return 0
        Select Case dm.dmDisplayOrientation
            Case DMDO_90 : Return 90
            Case DMDO_180 : Return 180
            Case DMDO_270 : Return 270
            Case Else : Return 0
        End Select
    End Function

    Public Function SetOrientationDegrees(ByVal degrees As Integer) As String
        Dim orient As Integer = DMDO_DEFAULT
        Select Case degrees
            Case 90 : orient = DMDO_90
            Case 180 : orient = DMDO_180
            Case 270 : orient = DMDO_270
            Case Else : orient = DMDO_DEFAULT
        End Select
        Dim dm As DEVMODE = NewDevMode()
        If Not EnumDisplaySettings(Nothing, ENUM_CURRENT_SETTINGS, dm) Then Return "cannot read current mode"
        Dim swap As Boolean = ((dm.dmDisplayOrientation = DMDO_DEFAULT OrElse dm.dmDisplayOrientation = DMDO_180) Xor
                               (orient = DMDO_DEFAULT OrElse orient = DMDO_180))
        If swap Then
            Dim w As Integer = dm.dmPelsWidth
            dm.dmPelsWidth = dm.dmPelsHeight
            dm.dmPelsHeight = w
            dm.dmFields = dm.dmFields Or DM_PELSWIDTH Or DM_PELSHEIGHT
        End If
        dm.dmDisplayOrientation = orient
        dm.dmFields = dm.dmFields Or DM_DISPLAYORIENTATION
        Dim apply As Integer = ChangeDisplaySettings(dm, CDS_UPDATEREGISTRY)
        If apply = DISP_CHANGE_SUCCESSFUL Then Return "OK"
        Return "orientation failed (" & apply.ToString() & ")"
    End Function

    Private Function NewDevMode() As DEVMODE
        Dim dm As New DEVMODE()
        dm.dmSize = CShort(Marshal.SizeOf(GetType(DEVMODE)))
        Return dm
    End Function

End Module

End Namespace
