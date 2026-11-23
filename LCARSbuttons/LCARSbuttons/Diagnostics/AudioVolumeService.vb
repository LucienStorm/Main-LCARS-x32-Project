' LCARSbuttons/LCARSbuttons/Diagnostics/AudioVolumeService.vb
Option Strict On
Option Explicit On

Imports System.Runtime.InteropServices

Namespace Diagnostics

''' <summary>Master volume / mute via Core Audio (+ APPCOMMAND nudge fallback).</summary>
Public Module AudioVolumeService

    Private Const CLSID_MMDeviceEnumerator As String = "BCDE0395-E52F-467C-8E3D-C4579291692E"
    Private Const IID_IMMDevice As String = "D666063F-1587-4E43-81F1-B948E807363F"
    Private Const IID_IMMDeviceEnumerator As String = "A95664D2-9614-4F35-A746-DE8DB63617E6"
    Private Const IID_IAudioEndpointVolume As String = "5CDF2C82-841E-4546-9722-0CF74078229A"
    Private Const CLSCTX_INPROC_SERVER As Integer = 1
    Private Const eRender As Integer = 0
    Private Const eConsole As Integer = 0
    Private Const eMultimedia As Integer = 1
    Private Const WM_APPCOMMAND As Integer = &H319
    Private Const APPCOMMAND_VOLUME_MUTE As Integer = 8
    Private Const APPCOMMAND_VOLUME_DOWN As Integer = 9
    Private Const APPCOMMAND_VOLUME_UP As Integer = 10
    Private Const HWND_BROADCAST As Integer = &HFFFF

    Private ReadOnly volumeLock As New Object()
    Private cachedVolume As IAudioEndpointVolume

    <ComImport(), Guid(IID_IAudioEndpointVolume), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Private Interface IAudioEndpointVolume
        <PreserveSig()> Function RegisterControlChangeNotify(ByVal pNotify As IntPtr) As Integer
        <PreserveSig()> Function UnregisterControlChangeNotify(ByVal pNotify As IntPtr) As Integer
        <PreserveSig()> Function GetChannelCount(<Out()> ByRef pnChannelCount As UInteger) As Integer
        <PreserveSig()> Function SetMasterVolumeLevel(ByVal fLevelDB As Single, ByRef pguidEventContext As Guid) As Integer
        <PreserveSig()> Function SetMasterVolumeLevelScalar(ByVal fLevel As Single, ByRef pguidEventContext As Guid) As Integer
        <PreserveSig()> Function GetMasterVolumeLevel(<Out()> ByRef pfLevelDB As Single) As Integer
        <PreserveSig()> Function GetMasterVolumeLevelScalar(<Out()> ByRef pfLevel As Single) As Integer
        <PreserveSig()> Function SetChannelVolumeLevel(ByVal nChannel As UInteger, ByVal fLevelDB As Single, ByRef pguidEventContext As Guid) As Integer
        <PreserveSig()> Function SetChannelVolumeLevelScalar(ByVal nChannel As UInteger, ByVal fLevel As Single, ByRef pguidEventContext As Guid) As Integer
        <PreserveSig()> Function GetChannelVolumeLevel(ByVal nChannel As UInteger, <Out()> ByRef pfLevelDB As Single) As Integer
        <PreserveSig()> Function GetChannelVolumeLevelScalar(ByVal nChannel As UInteger, <Out()> ByRef pfLevel As Single) As Integer
        <PreserveSig()> Function SetMute(ByVal bMute As Integer, ByRef pguidEventContext As Guid) As Integer
        <PreserveSig()> Function GetMute(<Out()> ByRef pbMute As Integer) As Integer
    End Interface

    <ComImport(), Guid(IID_IMMDeviceEnumerator), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Private Interface IMMDeviceEnumerator
        <PreserveSig()> Function EnumAudioEndpoints(ByVal dataFlow As Integer, ByVal dwStateMask As Integer, <Out()> ByRef ppDevices As Object) As Integer
        <PreserveSig()> Function GetDefaultAudioEndpoint(ByVal dataFlow As Integer, ByVal role As Integer, <Out()> ByRef ppDevice As IMMDevice) As Integer
    End Interface

    <ComImport(), Guid(IID_IMMDevice), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Private Interface IMMDevice
        <PreserveSig()> Function Activate(ByRef iid As Guid, ByVal dwClsCtx As Integer, ByVal pActivationParams As IntPtr, <Out(), MarshalAs(UnmanagedType.IUnknown)> ByRef ppInterface As Object) As Integer
    End Interface

    <DllImport("user32.dll")>
    Private Function SendMessage(ByVal hWnd As IntPtr, ByVal msg As Integer, ByVal wParam As IntPtr, ByVal lParam As IntPtr) As IntPtr
    End Function

    Public Function GetVolumePercent() As Integer
        Try
            Dim vol As IAudioEndpointVolume = TryGetVolumeInterface()
            If vol IsNot Nothing Then
                Dim level As Single
                If vol.GetMasterVolumeLevelScalar(level) = 0 Then
                    Return Math.Max(0, Math.Min(100, CInt(Math.Round(level * 100.0F))))
                End If
                Invalidate()
            End If
        Catch
            Invalidate()
        End Try
        Return -1
    End Function

    Public Function SetVolumePercent(ByVal percent As Integer) As Boolean
        Dim target As Integer = Math.Max(0, Math.Min(100, percent))
        Try
            Dim vol As IAudioEndpointVolume = TryGetVolumeInterface()
            If vol IsNot Nothing Then
                Dim ctx As Guid = Guid.Empty
                If vol.SetMasterVolumeLevelScalar(target / 100.0F, ctx) = 0 Then
                    Dim verify As Integer = GetVolumePercent()
                    If verify >= 0 AndAlso Math.Abs(verify - target) <= 2 Then Return True
                End If
                Invalidate()
            End If
        Catch
            Invalidate()
        End Try
        NudgeVolumeToPercent(target / 100.0F)
        Dim after As Integer = GetVolumePercent()
        Return after >= 0 AndAlso Math.Abs(after - target) <= 5
    End Function

    Public Function IsMuted() As Boolean
        Try
            Dim vol As IAudioEndpointVolume = TryGetVolumeInterface()
            If vol IsNot Nothing Then
                Dim m As Integer = 0
                If vol.GetMute(m) = 0 Then Return m <> 0
            End If
        Catch
            Invalidate()
        End Try
        Return False
    End Function

    Public Function SetMuted(ByVal muted As Boolean) As Boolean
        Try
            Dim vol As IAudioEndpointVolume = TryGetVolumeInterface()
            If vol IsNot Nothing Then
                Dim ctx As Guid = Guid.Empty
                If vol.SetMute(If(muted, 1, 0), ctx) = 0 Then
                    Dim verify As Integer = 0
                    If vol.GetMute(verify) = 0 AndAlso ((verify <> 0) = muted) Then Return True
                End If
                Invalidate()
            End If
        Catch
            Invalidate()
        End Try
        If IsMuted() = muted Then Return True
        SendAppCommand(APPCOMMAND_VOLUME_MUTE)
        Return IsMuted() = muted
    End Function

    Public Sub VolumeStep(ByVal up As Boolean)
        SendAppCommand(If(up, APPCOMMAND_VOLUME_UP, APPCOMMAND_VOLUME_DOWN))
    End Sub

    Public Function GetDefaultEndpointName() As String
        Return "DEFAULT PLAYBACK"
    End Function

    Private Sub NudgeVolumeToPercent(ByVal target As Single)
        Dim currentPct As Integer = GetVolumePercent()
        If currentPct < 0 Then Return
        Dim current As Single = currentPct / 100.0F
        If Math.Abs(current - target) < 0.02F Then Return
        Dim maxSteps As Integer = 50
        Dim stepCount As Integer = 0
        While current < target - 0.01F AndAlso stepCount < maxSteps
            SendAppCommand(APPCOMMAND_VOLUME_UP)
            currentPct = GetVolumePercent()
            If currentPct < 0 Then Return
            current = currentPct / 100.0F
            stepCount += 1
        End While
        stepCount = 0
        While current > target + 0.01F AndAlso stepCount < maxSteps
            SendAppCommand(APPCOMMAND_VOLUME_DOWN)
            currentPct = GetVolumePercent()
            If currentPct < 0 Then Return
            current = currentPct / 100.0F
            stepCount += 1
        End While
    End Sub

    Private Sub SendAppCommand(ByVal command As Integer)
        SendMessage(New IntPtr(HWND_BROADCAST), WM_APPCOMMAND, IntPtr.Zero, New IntPtr(command << 16))
    End Sub

    ''' <summary>Drop cached endpoint volume after default device changes.</summary>
    Public Sub InvalidateCache()
        Invalidate()
    End Sub

    Private Function TryGetVolumeInterface() As IAudioEndpointVolume
        SyncLock volumeLock
            If cachedVolume IsNot Nothing Then Return cachedVolume
            cachedVolume = CreateVolumeInterface(eConsole)
            If cachedVolume Is Nothing Then cachedVolume = CreateVolumeInterface(eMultimedia)
            Return cachedVolume
        End SyncLock
    End Function

    Private Sub Invalidate()
        SyncLock volumeLock
            cachedVolume = Nothing
        End SyncLock
    End Sub

    Private Function CreateVolumeInterface(ByVal role As Integer) As IAudioEndpointVolume
        Try
            Dim enumType As Type = Type.GetTypeFromCLSID(New Guid(CLSID_MMDeviceEnumerator))
            If enumType Is Nothing Then Return Nothing
            Dim enumerator As IMMDeviceEnumerator = CType(Activator.CreateInstance(enumType), IMMDeviceEnumerator)
            Dim device As IMMDevice = Nothing
            Dim hr As Integer = enumerator.GetDefaultAudioEndpoint(eRender, role, device)
            If hr <> 0 OrElse device Is Nothing Then Return Nothing
            Dim iid As Guid = New Guid(IID_IAudioEndpointVolume)
            Dim obj As Object = Nothing
            hr = device.Activate(iid, CLSCTX_INPROC_SERVER, IntPtr.Zero, obj)
            If hr <> 0 OrElse obj Is Nothing Then Return Nothing
            Return CType(obj, IAudioEndpointVolume)
        Catch
            Return Nothing
        End Try
    End Function

End Module

End Namespace
