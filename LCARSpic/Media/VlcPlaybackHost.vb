' LCARSpic/Media/VlcPlaybackHost.vb
Option Strict On
Option Explicit On

Imports System.IO
Imports System.IO.Compression
Imports System.Windows.Forms
Imports LibVLCSharp.Shared

''' <summary>
''' LibVLC host for local audio/video and network streams.
''' Natives: lib\vlc next to LCARSmedia.exe (or auto-extract lib-vlc.zip).
''' </summary>
Public Class VlcPlaybackHost
    Implements IDisposable

    Private _lib As LibVLC
    Private _player As MediaPlayer
    Private _media As LibVLCSharp.Shared.Media
    Private _disposed As Boolean
    Private _rateIndex As Integer = 2
    Private Shared ReadOnly RateSteps As Single() = {0.5F, 0.75F, 1.0F, 1.25F, 1.5F, 2.0F}
    Private Shared _coreReady As Boolean

    Public Event PlaybackEnded As EventHandler
    Public Event TimeChanged As EventHandler
    Public Event PlaybackFailed As EventHandler

    Private _lastError As String = ""

    Public ReadOnly Property LastError As String
        Get
            Return If(_lastError, "")
        End Get
    End Property

    Public ReadOnly Property IsPlaying As Boolean
        Get
            If _player Is Nothing Then Return False
            Return _player.IsPlaying
        End Get
    End Property

    Public ReadOnly Property IsPaused As Boolean
        Get
            If _player Is Nothing Then Return False
            Try
                Return _player.State = VLCState.Paused
            Catch
                Return False
            End Try
        End Get
    End Property

    Public Property Volume As Integer
        Get
            If _player Is Nothing Then Return 100
            Return Math.Max(0, Math.Min(100, _player.Volume))
        End Get
        Set(ByVal value As Integer)
            EnsureInitialized()
            _player.Volume = Math.Max(0, Math.Min(100, value))
        End Set
    End Property

    Public Property Mute As Boolean
        Get
            If _player Is Nothing Then Return False
            Return _player.Mute
        End Get
        Set(ByVal value As Boolean)
            EnsureInitialized()
            _player.Mute = value
        End Set
    End Property

    Public ReadOnly Property Rate As Single
        Get
            Return RateSteps(_rateIndex)
        End Get
    End Property

    Public Sub EnsureInitialized()
        If _lib IsNot Nothing Then Return
        Dim vlcDir As String = FindVlcDirectory()
        If String.IsNullOrEmpty(vlcDir) Then
            TryExtractBundledZip()
            vlcDir = FindVlcDirectory()
        End If
        If String.IsNullOrEmpty(vlcDir) Then
            Throw New DirectoryNotFoundException(
                "LibVLC natives not found. Expected lib\vlc\libvlc.dll next to LCARSmedia.exe" &
                " (or lib-vlc.zip to extract). Re-run LCARS Update.")
        End If

        Dim pluginDir As String = Path.Combine(vlcDir, "plugins")
        If Not Directory.Exists(pluginDir) Then
            Throw New DirectoryNotFoundException("LibVLC plugins folder missing:" & vbCrLf & pluginDir)
        End If

        ' Explicit plugin path — Core.Initialize alone is not always enough on tablet installs.
        Environment.SetEnvironmentVariable("VLC_PLUGIN_PATH", pluginDir)
        If Not _coreReady Then
            Core.Initialize(vlcDir)
            _coreReady = True
        End If

        _lib = New LibVLC(
            "--no-video-title-show",
            "--quiet",
            "--avcodec-hw=none",
            "--aout=directsound",
            "--file-caching=1000",
            "--network-caching=3000",
            "--plugin-path=" & pluginDir)
        _player = New MediaPlayer(_lib)
        AddHandler _player.EndReached, AddressOf OnEndReached
        AddHandler _player.TimeChanged, AddressOf OnTimeChanged
        AddHandler _player.EncounteredError, AddressOf OnEncounteredError
        _player.Volume = 100
        WriteDiag("LibVLC ready. dir=" & vlcDir & " plugins=" & pluginDir)
    End Sub

    Public Sub AttachVideoSurface(ByVal hwnd As IntPtr)
        EnsureInitialized()
        _player.Hwnd = hwnd
    End Sub

    Public Sub PlayFile(ByVal path As String)
        EnsureInitialized()
        _lastError = ""
        If String.IsNullOrEmpty(path) Then
            Throw New FileNotFoundException("Media path was empty.")
        End If
        path = path.Trim().Trim(""""c)
        If Not File.Exists(path) Then
            Throw New FileNotFoundException(
                "Media file not found or not accessible:" & vbCrLf & path, path)
        End If
        StopPlaybackKeepSurface()
        Dim full As String = System.IO.Path.GetFullPath(path)
        _media = New LibVLCSharp.Shared.Media(_lib, full, FromType.FromPath)
        _media.AddOption(":avcodec-hw=none")
        _media.AddOption(":file-caching=1000")
        Dim started As Boolean = _player.Play(_media)
        WriteDiag("PlayFile started=" & started.ToString() & " path=" & full & " state=" & _player.State.ToString())
        If Not started Then
            Throw New InvalidOperationException("LibVLC Play() returned false for:" & vbCrLf & full)
        End If
    End Sub

    ''' <summary>Network / radio stream (http, https, m3u, pls, icy).</summary>
    Public Sub PlayUrl(ByVal url As String)
        EnsureInitialized()
        _lastError = ""
        If String.IsNullOrEmpty(url) Then
            Throw New ArgumentException("Stream URL was empty.")
        End If
        url = url.Trim()
        StopPlaybackKeepSurface()
        _media = New LibVLCSharp.Shared.Media(_lib, url, FromType.FromLocation)
        _media.AddOption(":network-caching=3000")
        Dim started As Boolean = _player.Play(_media)
        WriteDiag("PlayUrl started=" & started.ToString() & " url=" & url)
        If Not started Then
            Throw New InvalidOperationException("LibVLC Play() returned false for stream:" & vbCrLf & url)
        End If
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
        If _player.State = VLCState.Ended OrElse _player.State = VLCState.Stopped Then
            If _media IsNot Nothing Then _player.Play(_media)
            Return
        End If
        _player.Pause()
    End Sub

    Public Sub StopPlayback()
        StopPlaybackKeepSurface()
    End Sub

    Private Sub StopPlaybackKeepSurface()
        If _player IsNot Nothing Then
            Try
                _player.Stop()
            Catch
            End Try
        End If
        If _media IsNot Nothing Then
            Try
                _media.Dispose()
            Catch
            End Try
            _media = Nothing
        End If
    End Sub

    Public Function PositionMs() As Long
        If _player Is Nothing Then Return 0
        Try
            Return _player.Time
        Catch
            Return 0
        End Try
    End Function

    Public Function LengthMs() As Long
        If _player Is Nothing Then Return 0
        Try
            Return Math.Max(0L, _player.Length)
        Catch
            Return 0
        End Try
    End Function

    Public Sub SeekToMs(ByVal ms As Long)
        If _player Is Nothing Then Return
        Dim len As Long = LengthMs()
        If len <= 0 Then Return
        _player.Time = Math.Max(0L, Math.Min(len, ms))
    End Sub

    Public Sub SeekRelativeMs(ByVal deltaMs As Long)
        SeekToMs(PositionMs() + deltaMs)
    End Sub

    Public Sub AdjustVolume(ByVal delta As Integer)
        Volume = Volume + delta
    End Sub

    Public Sub ToggleMute()
        Mute = Not Mute
    End Sub

    Public Function CycleRate() As Single
        EnsureInitialized()
        _rateIndex = (_rateIndex + 1) Mod RateSteps.Length
        _player.SetRate(RateSteps(_rateIndex))
        Return RateSteps(_rateIndex)
    End Function

    Public Function CycleAudioTrack() As String
        EnsureInitialized()
        Try
            Dim tracks As Structures.TrackDescription() = _player.AudioTrackDescription
            If tracks Is Nothing OrElse tracks.Length = 0 Then Return "NO AUDIO TRACKS"
            Dim cur As Integer = _player.AudioTrack
            Dim idx As Integer = 0
            For i As Integer = 0 To tracks.Length - 1
                If tracks(i).Id = cur Then
                    idx = i
                    Exit For
                End If
            Next
            idx = (idx + 1) Mod tracks.Length
            _player.SetAudioTrack(tracks(idx).Id)
            Return tracks(idx).Name
        Catch
            Return "AUDIO N/A"
        End Try
    End Function

    Public Function CycleSubtitle() As String
        EnsureInitialized()
        Try
            Dim tracks As Structures.TrackDescription() = _player.SpuDescription
            If tracks Is Nothing OrElse tracks.Length = 0 Then
                _player.SetSpu(-1)
                Return "NO SUBS"
            End If
            Dim cur As Integer = _player.Spu
            Dim idx As Integer = -1
            For i As Integer = 0 To tracks.Length - 1
                If tracks(i).Id = cur Then
                    idx = i
                    Exit For
                End If
            Next
            idx += 1
            If idx >= tracks.Length Then
                _player.SetSpu(-1)
                Return "SUBS OFF"
            End If
            _player.SetSpu(tracks(idx).Id)
            Return tracks(idx).Name
        Catch
            Return "SUBS N/A"
        End Try
    End Function

    Private Sub OnEndReached(ByVal sender As Object, ByVal e As EventArgs)
        RaiseEvent PlaybackEnded(Me, EventArgs.Empty)
    End Sub

    Private Sub OnTimeChanged(ByVal sender As Object, ByVal e As MediaPlayerTimeChangedEventArgs)
        RaiseEvent TimeChanged(Me, EventArgs.Empty)
    End Sub

    Private Sub OnEncounteredError(ByVal sender As Object, ByVal e As EventArgs)
        Dim st As String = ""
        Try
            If _player IsNot Nothing Then st = _player.State.ToString()
        Catch
        End Try
        _lastError = "LibVLC EncounteredError (state=" & st & "). Check lib\vlc\plugins and codecs."
        WriteDiag(_lastError)
        RaiseEvent PlaybackFailed(Me, EventArgs.Empty)
    End Sub

    Private Shared Sub WriteDiag(ByVal line As String)
        Try
            Dim logPath As String = System.IO.Path.Combine(Application.StartupPath, "lcars-media-vlc.log")
            File.AppendAllText(logPath, DateTime.Now.ToString("u") & " " & line & Environment.NewLine)
        Catch
        End Try
    End Sub

    Private Shared Sub TryExtractBundledZip()
        Try
            Dim baseDir As String = AppDomain.CurrentDomain.BaseDirectory
            Dim zipPath As String = Path.Combine(baseDir, "lib-vlc.zip")
            If Not File.Exists(zipPath) Then
                zipPath = Path.Combine(Application.StartupPath, "lib-vlc.zip")
            End If
            If Not File.Exists(zipPath) Then Return
            Dim extractRoot As String = Application.StartupPath
            If Not File.Exists(Path.Combine(extractRoot, "LCARSmedia.exe")) Then
                extractRoot = baseDir
            End If
            Dim marker As String = Path.Combine(Path.Combine(extractRoot, "lib"), Path.Combine("vlc", "libvlc.dll"))
            If File.Exists(marker) Then Return
            ZipFile.ExtractToDirectory(zipPath, extractRoot)
            WriteDiag("Extracted lib-vlc.zip to " & extractRoot)
        Catch ex As Exception
            WriteDiag("Extract zip failed: " & ex.Message)
        End Try
    End Sub

    Private Shared Function FindVlcDirectory() As String
        Dim baseDir As String = AppDomain.CurrentDomain.BaseDirectory
        Dim candidates As String() = {
            Path.Combine(Path.Combine(Application.StartupPath, "lib"), "vlc"),
            Path.Combine(Path.Combine(baseDir, "lib"), "vlc"),
            Path.Combine(baseDir, "vlc"),
            Path.Combine(Application.StartupPath, "vlc")
        }
        For Each c As String In candidates
            Try
                Dim full As String = Path.GetFullPath(c)
                If File.Exists(Path.Combine(full, "libvlc.dll")) AndAlso
                   Directory.Exists(Path.Combine(full, "plugins")) Then
                    Return full
                End If
            Catch
            End Try
        Next
        Return Nothing
    End Function

    Public Sub Dispose() Implements IDisposable.Dispose
        If _disposed Then Return
        _disposed = True
        Try
            If _player IsNot Nothing Then
                RemoveHandler _player.EndReached, AddressOf OnEndReached
                RemoveHandler _player.TimeChanged, AddressOf OnTimeChanged
                RemoveHandler _player.EncounteredError, AddressOf OnEncounteredError
            End If
            StopPlaybackKeepSurface()
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
