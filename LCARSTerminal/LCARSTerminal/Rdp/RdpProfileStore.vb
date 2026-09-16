' LCARSTerminal/Rdp/RdpProfileStore.vb
Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.IO
Imports System.Web.Script.Serialization

''' <summary>
''' Loads/saves RDP connection profiles as JSON under the current Windows user's AppData.
''' </summary>
Public Class RdpProfileStore
    Private ReadOnly _rootDir As String

    Public Sub New(Optional ByVal rootDir As String = Nothing)
        If String.IsNullOrWhiteSpace(rootDir) Then
            _rootDir = Path.Combine( _
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), _
                "LCARSTerminal", _
                "Rdp")
        Else
            _rootDir = rootDir
        End If
    End Sub

    Public Function ProfilesPath() As String
        Return Path.Combine(_rootDir, "profiles.json")
    End Function

    Public Function LoadAll() As List(Of RdpConnectionProfile)
        Dim path As String = ProfilesPath()
        If Not File.Exists(path) Then
            Return New List(Of RdpConnectionProfile)()
        End If
        Dim text As String = File.ReadAllText(path)
        If String.IsNullOrWhiteSpace(text) Then
            Return New List(Of RdpConnectionProfile)()
        End If
        Dim ser As New JavaScriptSerializer()
        Dim list As List(Of RdpConnectionProfile) = ser.Deserialize(Of List(Of RdpConnectionProfile))(text)
        If list Is Nothing Then
            Return New List(Of RdpConnectionProfile)()
        End If
        Return list
    End Function

    Public Sub SaveAll(ByVal profiles As IList(Of RdpConnectionProfile))
        If profiles Is Nothing Then Throw New ArgumentNullException("profiles")
        If Not Directory.Exists(_rootDir) Then
            Directory.CreateDirectory(_rootDir)
        End If
        Dim ser As New JavaScriptSerializer()
        Dim text As String = ser.Serialize(profiles)
        File.WriteAllText(ProfilesPath(), text)
    End Sub
End Class
