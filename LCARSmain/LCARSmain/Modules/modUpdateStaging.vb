Option Strict On

Imports System.Diagnostics
Imports System.IO

''' <summary>
''' Safe cleanup for LCARS update staging files. Never deletes the installer's own
''' EXE/DLL while runInstallScript is still running — that crashes the loaded process.
''' </summary>
Public Module modUpdateStaging

    Private ReadOnly ProtectedStagingFiles As String() = {"runInstallScript.exe", "Ionic.Zip.Reduced.dll"}

    Public Function GetUpdateStagingDirectory() As String
        Dim dir As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LCARS x32")
        dir = Path.Combine(dir, "UpdateStaging")
        Return dir
    End Function

    ''' <summary>
    ''' Cleans payload files after an update restart. Installer binaries are removed only
    ''' after runInstallScript has exited, using a delayed delete when needed.
    ''' </summary>
    Public Sub CleanupAfterUpdateRestart()
        modShellFallback.ClearPendingShellRestart()
        modShellFallback.ClearUpdateInProgress()
        Dim stagingDir As String = GetUpdateStagingDirectory()
        CleanupLegacyTempFolder()

        If Not Directory.Exists(stagingDir) Then
            Return
        End If

        DeleteFileIfExists(Path.Combine(stagingDir, "script.txt"))

        Dim cleanupList As String = Path.Combine(stagingDir, "cleanup.txt")
        If File.Exists(cleanupList) Then
            For Each line As String In File.ReadAllLines(cleanupList)
                Dim fileName As String = line.Trim()
                If fileName <> "" AndAlso Not IsProtectedStagingFile(fileName) Then
                    DeleteFileIfExists(Path.Combine(stagingDir, fileName))
                End If
            Next
            DeleteFileIfExists(cleanupList)
        End If

        ScheduleInstallerBinaryCleanup(stagingDir)
    End Sub

    Private Sub CleanupLegacyTempFolder()
        Dim tempDir As String = Path.GetTempPath()
        DeleteFileIfExists(Path.Combine(tempDir, "script.txt"))
        DeleteFileIfExists(Path.Combine(tempDir, "cleanup.txt"))
    End Sub

    Private Sub ScheduleInstallerBinaryCleanup(ByVal stagingDir As String)
        ' Never delete installer binaries synchronously — the process may still be
        ' unloading its EXE/DLL during exit, which causes "stopped working" crashes.
        Dim cleanupScript As String = Path.Combine(stagingDir, "lcars-staging-cleanup.cmd")
        Dim script As New System.Text.StringBuilder()
        script.AppendLine("@echo off")
        script.AppendLine(":wait")
        script.AppendLine("tasklist /fi ""imagename eq runInstallScript.exe"" | find /i ""runInstallScript.exe"" >nul")
        script.AppendLine("if %errorlevel%==0 (")
        script.AppendLine("  timeout /t 1 /nobreak >nul")
        script.AppendLine("  goto wait")
        script.AppendLine(")")
        For Each fileName As String In ProtectedStagingFiles
            script.AppendLine("del /f /q """ & Path.Combine(stagingDir, fileName) & """")
        Next
        script.AppendLine("del /f /q """ & cleanupScript & """")
        File.WriteAllText(cleanupScript, script.ToString())

        Dim psi As New ProcessStartInfo()
        psi.FileName = "cmd.exe"
        psi.Arguments = "/c start """" /min """ & cleanupScript & """"
        psi.CreateNoWindow = True
        psi.UseShellExecute = False
        Process.Start(psi)
    End Sub

    Private Function IsProtectedStagingFile(ByVal fileName As String) As Boolean
        For Each protectedName As String In ProtectedStagingFiles
            If String.Equals(fileName, protectedName, StringComparison.OrdinalIgnoreCase) Then
                Return True
            End If
        Next
        Return False
    End Function

    Private Sub DeleteFileIfExists(ByVal filePath As String)
        Try
            If File.Exists(filePath) Then
                File.Delete(filePath)
            End If
        Catch
        End Try
    End Sub

End Module
