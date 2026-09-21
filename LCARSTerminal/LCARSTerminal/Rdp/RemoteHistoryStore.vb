' LCARSTerminal/Rdp/RemoteHistoryStore.vb
Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.IO
Imports System.Web.Script.Serialization

''' <summary>Recent successful remote connections (separate from saved profiles).</summary>
Public Class RemoteHistoryEntry
    Public Property Hostname As String = ""
    Public Property Username As String = ""
    Public Property Protocol As Integer = CInt(RemoteProtocol.Rdp)
    Public Property Port As Integer = 3389
    Public Property ProfileId As String = ""
    Public Property LastUsedUtc As String = ""

    Public Function ProtocolKind() As RemoteProtocol
        If Protocol = CInt(RemoteProtocol.Vnc) Then Return RemoteProtocol.Vnc
        Return RemoteProtocol.Rdp
    End Function

    Public Function DisplayText() As String
        Dim tag As String = If(ProtocolKind() = RemoteProtocol.Vnc, "VNC", "RDP")
        Dim host As String = If(String.IsNullOrWhiteSpace(Hostname), "(unknown)", Hostname.Trim())
        If Not String.IsNullOrWhiteSpace(Username) Then
            Return tag & "  " & host & "  —  " & Username.Trim()
        End If
        Return tag & "  " & host
    End Function
End Class

''' <summary>Persists connection history under AppData\LCARSTerminal\Rdp\history.json.</summary>
Public Class RemoteHistoryStore
    Private Const MaxEntries As Integer = 40
    Private ReadOnly _path As String

    Public Sub New(Optional ByVal rootDir As String = Nothing)
        Dim root As String = rootDir
        If String.IsNullOrWhiteSpace(root) Then
            root = Path.Combine( _
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), _
                "LCARSTerminal", "Rdp")
        End If
        _path = Path.Combine(root, "history.json")
    End Sub

    Public Function LoadAll() As List(Of RemoteHistoryEntry)
        If Not File.Exists(_path) Then Return New List(Of RemoteHistoryEntry)()
        Try
            Dim text As String = File.ReadAllText(_path)
            If String.IsNullOrWhiteSpace(text) Then Return New List(Of RemoteHistoryEntry)()
            Dim ser As New JavaScriptSerializer()
            Dim list As List(Of RemoteHistoryEntry) = ser.Deserialize(Of List(Of RemoteHistoryEntry))(text)
            If list Is Nothing Then Return New List(Of RemoteHistoryEntry)()
            Return list
        Catch
            Return New List(Of RemoteHistoryEntry)()
        End Try
    End Function

    Public Sub Record(ByVal profile As RdpConnectionProfile)
        If profile Is Nothing OrElse String.IsNullOrWhiteSpace(profile.Hostname) Then Return
        Dim list As List(Of RemoteHistoryEntry) = LoadAll()
        Dim keyHost As String = profile.Hostname.Trim().ToLowerInvariant()
        Dim keyProto As Integer = CInt(profile.Protocol)
        list.RemoveAll(Function(e) _
            e IsNot Nothing AndAlso _
            String.Equals(If(e.Hostname, "").Trim().ToLowerInvariant(), keyHost, StringComparison.Ordinal) AndAlso _
            e.Protocol = keyProto)
        Dim entry As New RemoteHistoryEntry()
        entry.Hostname = profile.Hostname.Trim()
        entry.Username = If(profile.Username, "").Trim()
        entry.Protocol = keyProto
        entry.Port = profile.Port
        entry.ProfileId = profile.Id.ToString("N")
        entry.LastUsedUtc = DateTime.UtcNow.ToString("o")
        list.Insert(0, entry)
        While list.Count > MaxEntries
            list.RemoveAt(list.Count - 1)
        End While
        SaveAll(list)
    End Sub

    Private Sub SaveAll(ByVal entries As List(Of RemoteHistoryEntry))
        Dim dir As String = Path.GetDirectoryName(_path)
        If Not String.IsNullOrEmpty(dir) AndAlso Not Directory.Exists(dir) Then
            Directory.CreateDirectory(dir)
        End If
        Dim ser As New JavaScriptSerializer()
        File.WriteAllText(_path, ser.Serialize(entries))
    End Sub
End Class
