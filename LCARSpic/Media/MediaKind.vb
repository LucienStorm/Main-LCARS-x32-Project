' LCARSpic/Media/MediaKind.vb
Option Strict On
Option Explicit On

Imports System.IO

Public Enum MediaKind
    None = 0
    Photo = 1
    Music = 2
    Video = 3
    Radio = 4
End Enum

Public Module MediaKindUtil
    Private ReadOnly ImageExts As String() = {".jpg", ".jpeg", ".gif", ".bmp", ".png", ".tif", ".tiff", ".webp"}
    Private ReadOnly AudioExts As String() = {".mp3", ".flac", ".wav", ".m4a", ".aac", ".ogg", ".wma", ".opus"}
    Private ReadOnly VideoExts As String() = {".mp4", ".mkv", ".avi", ".wmv", ".mov", ".m4v", ".webm", ".mpg", ".mpeg", ".ts"}

    Public Function DetectMediaKind(ByVal path As String) As MediaKind
        If String.IsNullOrEmpty(path) Then Return MediaKind.None
        Dim p As String = path.Trim()
        If p.StartsWith("http://", StringComparison.OrdinalIgnoreCase) OrElse
           p.StartsWith("https://", StringComparison.OrdinalIgnoreCase) OrElse
           p.StartsWith("mms://", StringComparison.OrdinalIgnoreCase) OrElse
           p.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase) Then
            Return MediaKind.Radio
        End If
        Dim ext As String = System.IO.Path.GetExtension(p).ToLowerInvariant()
        If Array.IndexOf(ImageExts, ext) >= 0 Then Return MediaKind.Photo
        If Array.IndexOf(AudioExts, ext) >= 0 Then Return MediaKind.Music
        If Array.IndexOf(VideoExts, ext) >= 0 Then Return MediaKind.Video
        Return MediaKind.None
    End Function

    Public Function IsSupportedMedia(ByVal path As String) As Boolean
        Return DetectMediaKind(path) <> MediaKind.None
    End Function
End Module

