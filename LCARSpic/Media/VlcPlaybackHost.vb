' LCARSpic/Media/VlcPlaybackHost.vb
Option Strict On
Option Explicit On

Imports System.IO
Imports System.Windows.Forms
Imports LibVLCSharp.Shared

''' <summary>
''' LibVLC host for local audio/video. Natives live in lib\vlc next to the exe.
''' </summary>
Public Class VlcPlaybackHost
    Implements IDisposable

    Private _lib As LibVLC
    Private _player As MediaPlayer
    Private _media As LibVLCSharp.Shared.Media
    Private _disposed As Boolean

    Public ReadOnly Property IsPlaying As Boolean
        Get
            If _player Is Nothing Then Return False
            Return _player.IsPlaying
        End Get
    End Property

    Public Sub EnsureInitialized()
        If _lib IsNot Nothing Then Return
        Dim vlcDir As String = FindVlcDirectory()
        If String.IsNullOrEmpty(vlcDir) Then
            Throw New DirectoryNotFoundException("LibVLC natives not found (expected lib\vlc\libvlc.dll).")
        End If
        Core.Initialize(vlcDir)
        _lib = New LibVLC("--no-video-title-show", "--quiet")
        _player = New MediaPlayer(_lib)
    End Sub

    Public Sub AttachVideoSurface(ByVal hwnd As IntPtr)
        EnsureInitialized()
        _player.Hwnd = hwnd
    End Sub

    Public Sub PlayFile(ByVal path As String)
        EnsureInitialized()
        If String.IsNullOrEmpty(path) OrElse Not File.Exists(path) Then
            Throw New FileNotFoundException("Media file not found.", path)
        End If
        StopPlayback()
        _media = New LibVLCSharp.Shared.Media(_lib, path, FromType.FromPath)
        _player.Play(_media)
    End Sub

    Public Sub PausePlayback()
        If _player Is Nothing Then Return
        _player.SetPause(True)
    End Sub

    Public Sub ResumePlayback()
        If _player Is Nothing Then Return
        _player.SetPause(False)
    End Sub

    Public Sub TogglePause()
        If _player Is Nothing Then Return
        If _player.IsPlaying Then
            _player.SetPause(True)
        Else
            _player.SetPause(False)
        End If
    End Sub

    Public Sub StopPlayback()
        If _player IsNot Nothing Then
            _player.Stop()
        End If
        If _media IsNot Nothing Then
            _media.Dispose()
            _media = Nothing
        End If
    End Sub

    Public Function PositionMs() As Integer
        If _player Is Nothing Then Return 0
        Try
            Return CInt(_player.Time)
        Catch
            Return 0
        End Try
    End Function

    Private Shared Function FindVlcDirectory() As String
        Dim baseDir As String = AppDomain.CurrentDomain.BaseDirectory
        Dim candidates As String() = {
            Path.Combine(Path.Combine(baseDir, "lib"), "vlc"),
            Path.Combine(baseDir, "vlc"),
            Path.Combine(Path.Combine(Path.Combine(baseDir, ".."), ".."), Path.Combine("lib", "vlc"))
        }
        For Each c As String In candidates
            Dim full As String = Path.GetFullPath(c)
            If File.Exists(Path.Combine(full, "libvlc.dll")) Then Return full
        Next
        Return Nothing
    End Function

    Public Sub Dispose() Implements IDisposable.Dispose
        If _disposed Then Return
        _disposed = True
        Try
            StopPlayback()
        Catch
        End Try
        If _player IsNot Nothing Then
            _player.Dispose()
            _player = Nothing
        End If
        If _lib IsNot Nothing Then
            _lib.Dispose()
            _lib = Nothing
        End If
    End Sub
End Class
