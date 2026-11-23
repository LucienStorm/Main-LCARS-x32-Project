' LCARSbuttons/LCARSbuttons/Diagnostics/AudioEndpointService.vb
Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Runtime.InteropServices

Namespace Diagnostics

''' <summary>Playback / recording endpoint list + default / communications role (Core Audio).</summary>
Public Class AudioEndpointInfo
    Public Id As String = ""
    Public Name As String = ""
    Public IsRender As Boolean = True
    Public IsDefaultConsole As Boolean = False
    Public IsDefaultMultimedia As Boolean = False
    Public IsDefaultCommunications As Boolean = False

    Public ReadOnly Property IsDefaultDevice As Boolean
        Get
            Return IsDefaultConsole OrElse IsDefaultMultimedia
        End Get
    End Property

    Public Overrides Function ToString() As String
        Dim marks As String = ""
        If IsDefaultDevice Then marks &= " [DEFAULT]"
        If IsDefaultCommunications Then marks &= " [COMMS]"
        Return Name & marks
    End Function
End Class

Public Module AudioEndpointService

    Private Const CLSID_MMDeviceEnumerator As String = "BCDE0395-E52F-467C-8E3D-C4579291692E"
    Private Const CLSID_PolicyConfigClient As String = "870AF99C-171D-4F9E-AF0D-E63DF40C2BC9"
    Private Const IID_IMMDeviceEnumerator As String = "A95664D2-9614-4F35-A746-DE8DB63617E6"
    Private Const IID_IMMDevice As String = "D666063F-1587-4E43-81F1-B948E807363F"
    Private Const IID_IMMDeviceCollection As String = "0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"
    Private Const IID_IPropertyStore As String = "886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"
    Private Const IID_IPolicyConfig As String = "F8679F50-850A-41CF-9C72-430F290290C8"
    Private Const DEVICE_STATE_ACTIVE As Integer = &H1
    Private Const STGM_READ As Integer = 0
    Private Const eRender As Integer = 0
    Private Const eCapture As Integer = 1
    Private Const eConsole As Integer = 0
    Private Const eMultimedia As Integer = 1
    Private Const eCommunications As Integer = 2

    Private ReadOnly PKEY_Device_FriendlyName As New PROPERTYKEY(New Guid("A45C254E-DF1C-4EFD-8020-67D146A850E0"), 14)

    <StructLayout(LayoutKind.Sequential)>
    Private Structure PROPERTYKEY
        Public fmtid As Guid
        Public pid As Integer
        Public Sub New(ByVal f As Guid, ByVal p As Integer)
            fmtid = f
            pid = p
        End Sub
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Private Structure PROPVARIANT
        Public vt As Short
        Public wReserved1 As Short
        Public wReserved2 As Short
        Public wReserved3 As Short
        Public ptr As IntPtr
        Public pad As IntPtr
    End Structure

    <ComImport(), Guid(IID_IMMDeviceEnumerator), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Private Interface IMMDeviceEnumerator
        <PreserveSig()> Function EnumAudioEndpoints(ByVal dataFlow As Integer, ByVal dwStateMask As Integer, <Out()> ByRef ppDevices As IMMDeviceCollection) As Integer
        <PreserveSig()> Function GetDefaultAudioEndpoint(ByVal dataFlow As Integer, ByVal role As Integer, <Out()> ByRef ppDevice As IMMDevice) As Integer
        <PreserveSig()> Function GetDevice(<MarshalAs(UnmanagedType.LPWStr)> ByVal pwstrId As String, <Out()> ByRef ppDevice As IMMDevice) As Integer
    End Interface

    <ComImport(), Guid(IID_IMMDeviceCollection), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Private Interface IMMDeviceCollection
        <PreserveSig()> Function GetCount(<Out()> ByRef pcDevices As UInteger) As Integer
        <PreserveSig()> Function Item(ByVal nDevice As UInteger, <Out()> ByRef ppDevice As IMMDevice) As Integer
    End Interface

    <ComImport(), Guid(IID_IMMDevice), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Private Interface IMMDevice
        <PreserveSig()> Function Activate(ByRef iid As Guid, ByVal dwClsCtx As Integer, ByVal pActivationParams As IntPtr, <Out(), MarshalAs(UnmanagedType.IUnknown)> ByRef ppInterface As Object) As Integer
        <PreserveSig()> Function OpenPropertyStore(ByVal stgmAccess As Integer, <Out()> ByRef ppProperties As IPropertyStore) As Integer
        <PreserveSig()> Function GetId(<Out(), MarshalAs(UnmanagedType.LPWStr)> ByRef ppstrId As String) As Integer
        <PreserveSig()> Function GetState(<Out()> ByRef pdwState As Integer) As Integer
    End Interface

    <ComImport(), Guid(IID_IPropertyStore), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Private Interface IPropertyStore
        <PreserveSig()> Function GetCount(<Out()> ByRef cProps As UInteger) As Integer
        <PreserveSig()> Function GetAt(ByVal iProp As UInteger, ByRef pkey As PROPERTYKEY) As Integer
        <PreserveSig()> Function GetValue(ByRef key As PROPERTYKEY, ByRef pv As PROPVARIANT) As Integer
        <PreserveSig()> Function SetValue(ByRef key As PROPERTYKEY, ByRef pv As PROPVARIANT) As Integer
        <PreserveSig()> Function Commit() As Integer
    End Interface

    <ComImport(), Guid(IID_IPolicyConfig), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Private Interface IPolicyConfig
        Sub Stub01()
        Sub Stub02()
        Sub Stub03()
        Sub Stub04()
        Sub Stub05()
        Sub Stub06()
        Sub Stub07()
        Sub Stub08()
        Sub Stub09()
        Sub Stub10()
        <PreserveSig()> Function SetDefaultEndpoint(<MarshalAs(UnmanagedType.LPWStr)> ByVal deviceId As String, ByVal role As Integer) As Integer
    End Interface

    <DllImport("ole32.dll")>
    Private Function PropVariantClear(ByRef pvar As PROPVARIANT) As Integer
    End Function

    Public Function ListPlaybackDevices() As List(Of AudioEndpointInfo)
        Return ListDevices(eRender)
    End Function

    Public Function ListRecordingDevices() As List(Of AudioEndpointInfo)
        Return ListDevices(eCapture)
    End Function

    ''' <summary>Communications candidates: render + capture, labeled by flow.</summary>
    Public Function ListCommunicationsDevices() As List(Of AudioEndpointInfo)
        Dim list As New List(Of AudioEndpointInfo)()
        For Each d As AudioEndpointInfo In ListDevices(eRender)
            d.Name = "OUT · " & d.Name
            list.Add(d)
        Next
        For Each d As AudioEndpointInfo In ListDevices(eCapture)
            d.Name = "IN · " & d.Name
            list.Add(d)
        Next
        Return list
    End Function

    Public Function GetDefaultName(ByVal render As Boolean, ByVal role As Integer) As String
        Try
            Dim enumerator As IMMDeviceEnumerator = CreateEnumerator()
            If enumerator Is Nothing Then Return ""
            Dim device As IMMDevice = Nothing
            Dim flow As Integer = If(render, eRender, eCapture)
            If enumerator.GetDefaultAudioEndpoint(flow, role, device) <> 0 OrElse device Is Nothing Then Return ""
            Return GetFriendlyName(device)
        Catch
            Return ""
        End Try
    End Function

    ''' <summary>Set as default Playback/Recording device (Console + Multimedia roles).</summary>
    Public Function SetDefaultDevice(ByVal deviceId As String) As String
        Return SetRoles(deviceId, True, True, False)
    End Function

    ''' <summary>Set as default Communications device for this endpoint.</summary>
    Public Function SetCommunicationsDevice(ByVal deviceId As String) As String
        Return SetRoles(deviceId, False, False, True)
    End Function

    ''' <summary>Set Console + Multimedia + Communications (full default).</summary>
    Public Function SetAllRoles(ByVal deviceId As String) As String
        Return SetRoles(deviceId, True, True, True)
    End Function

    Private Function SetRoles(ByVal deviceId As String, ByVal consoleRole As Boolean, ByVal multimedia As Boolean, ByVal communications As Boolean) As String
        If String.IsNullOrEmpty(deviceId) Then Return "no device"
        Try
            Dim type As Type = Type.GetTypeFromCLSID(New Guid(CLSID_PolicyConfigClient))
            If type Is Nothing Then Return "policy config unavailable"
            Dim obj As Object = Activator.CreateInstance(type)
            Dim policy As IPolicyConfig = TryCast(obj, IPolicyConfig)
            If policy Is Nothing Then
                Try
                    Marshal.ReleaseComObject(obj)
                Catch
                End Try
                Return "policy config cast failed"
            End If
            Dim fails As New List(Of String)()
            If consoleRole AndAlso policy.SetDefaultEndpoint(deviceId, eConsole) <> 0 Then fails.Add("console")
            If multimedia AndAlso policy.SetDefaultEndpoint(deviceId, eMultimedia) <> 0 Then fails.Add("multimedia")
            If communications AndAlso policy.SetDefaultEndpoint(deviceId, eCommunications) <> 0 Then fails.Add("communications")
            Try
                Marshal.ReleaseComObject(obj)
            Catch
            End Try
            If fails.Count = 0 Then Return "OK"
            Return "failed: " & String.Join(",", fails.ToArray())
        Catch ex As Exception
            Return "set default failed: " & ex.Message
        End Try
    End Function

    Private Function ListDevices(ByVal dataFlow As Integer) As List(Of AudioEndpointInfo)
        Dim list As New List(Of AudioEndpointInfo)()
        Try
            Dim enumerator As IMMDeviceEnumerator = CreateEnumerator()
            If enumerator Is Nothing Then Return list

            Dim defaultConsole As String = GetDefaultId(enumerator, dataFlow, eConsole)
            Dim defaultMulti As String = GetDefaultId(enumerator, dataFlow, eMultimedia)
            Dim defaultComms As String = GetDefaultId(enumerator, dataFlow, eCommunications)

            Dim collection As IMMDeviceCollection = Nothing
            If enumerator.EnumAudioEndpoints(dataFlow, DEVICE_STATE_ACTIVE, collection) <> 0 OrElse collection Is Nothing Then Return list

            Dim count As UInteger = 0
            collection.GetCount(count)
            If count = 0UI Then Return list
            For i As UInteger = 0UI To count - 1UI
                Dim device As IMMDevice = Nothing
                If collection.Item(i, device) <> 0 OrElse device Is Nothing Then Continue For
                Dim id As String = ""
                device.GetId(id)
                Dim info As New AudioEndpointInfo()
                info.Id = If(id, "")
                info.Name = GetFriendlyName(device)
                If String.IsNullOrEmpty(info.Name) Then info.Name = info.Id
                info.IsRender = (dataFlow = eRender)
                info.IsDefaultConsole = String.Equals(info.Id, defaultConsole, StringComparison.OrdinalIgnoreCase)
                info.IsDefaultMultimedia = String.Equals(info.Id, defaultMulti, StringComparison.OrdinalIgnoreCase)
                info.IsDefaultCommunications = String.Equals(info.Id, defaultComms, StringComparison.OrdinalIgnoreCase)
                list.Add(info)
            Next
        Catch
        End Try
        list.Sort(Function(a, b) String.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase))
        Return list
    End Function

    Private Function CreateEnumerator() As IMMDeviceEnumerator
        Dim t As Type = Type.GetTypeFromCLSID(New Guid(CLSID_MMDeviceEnumerator))
        If t Is Nothing Then Return Nothing
        Return TryCast(Activator.CreateInstance(t), IMMDeviceEnumerator)
    End Function

    Private Function GetDefaultId(ByVal enumerator As IMMDeviceEnumerator, ByVal dataFlow As Integer, ByVal role As Integer) As String
        Try
            Dim device As IMMDevice = Nothing
            If enumerator.GetDefaultAudioEndpoint(dataFlow, role, device) <> 0 OrElse device Is Nothing Then Return ""
            Dim id As String = ""
            device.GetId(id)
            Return If(id, "")
        Catch
            Return ""
        End Try
    End Function

    Private Function GetFriendlyName(ByVal device As IMMDevice) As String
        Try
            Dim store As IPropertyStore = Nothing
            If device.OpenPropertyStore(STGM_READ, store) <> 0 OrElse store Is Nothing Then Return ""
            Dim key As PROPERTYKEY = PKEY_Device_FriendlyName
            Dim pv As New PROPVARIANT()
            If store.GetValue(key, pv) <> 0 Then Return ""
            Try
                ' VT_LPWSTR = 31
                If pv.vt = 31 AndAlso pv.ptr <> IntPtr.Zero Then
                    Return Marshal.PtrToStringUni(pv.ptr)
                End If
            Finally
                PropVariantClear(pv)
            End Try
        Catch
        End Try
        Return ""
    End Function

End Module

End Namespace
