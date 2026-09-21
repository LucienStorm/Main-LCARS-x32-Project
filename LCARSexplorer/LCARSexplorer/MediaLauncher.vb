' LCARSexplorer/MediaLauncher.vb
Option Strict On
Option Explicit On

Imports System.Diagnostics
Imports System.IO
Imports System.Windows.Forms

''' <summary>
''' Routes photo/audio/video opens to LCARSmedia.exe instead of Windows associations.
''' </summary>
Public Module MediaLauncher
    Private ReadOnly ImageExts As String() = {".jpg", ".jpeg", ".gif", ".bmp", ".png", ".tif", ".tiff", ".webp"}
    Private ReadOnly AudioExts As String() = {".mp3", ".flac", ".wav", ".m4a", ".aac", ".ogg", ".wma", ".opus"}
    Private ReadOnly VideoExts As String() = {".mp4", ".mkv", ".avi", ".wmv", ".mov", ".m4v", ".webm", ".mpg", ".mpeg", ".ts"}

    Public Function IsMediaFile(ByVal filePath As String) As Boolean
        If String.IsNullOrEmpty(filePath) Then Return False
        Dim ext As String = System.IO.Path.GetExtension(filePath).ToLowerInvariant()
        Return Array.IndexOf(ImageExts, ext) >= 0 OrElse
               Array.IndexOf(AudioExts, ext) >= 0 OrElse
               Array.IndexOf(VideoExts, ext) >= 0
    End Function

    Public Function TryOpenInLcarsMedia(ByVal filePath As String) As Boolean
        If Not IsMediaFile(filePath) Then Return False
        Dim exe As String = System.IO.Path.Combine(Application.StartupPath, "LCARSmedia.exe")
        If Not File.Exists(exe) Then
            MsgBox("LCARSmedia.exe not found next to LCARSexplorer:" & vbCrLf & exe, MsgBoxStyle.OkOnly Or MsgBoxStyle.Exclamation, "LCARS MEDIA")
            Return True ' handled (failed)
        End If
        If Not File.Exists(filePath) Then
            MsgBox("File not found or not accessible:" & vbCrLf & filePath, MsgBoxStyle.OkOnly Or MsgBoxStyle.Exclamation, "LCARS MEDIA")
            Return True
        End If
        Try
            Dim psi As New ProcessStartInfo()
            psi.FileName = exe
            psi.Arguments = """" & filePath & """"
            psi.WorkingDirectory = Application.StartupPath
            Process.Start(psi)
        Catch ex As Exception
            MsgBox("Could not start LCARSmedia:" & vbCrLf & ex.Message & vbCrLf & filePath, MsgBoxStyle.OkOnly Or MsgBoxStyle.Exclamation, "LCARS MEDIA")
        End Try
        Return True
    End Function
End Module
