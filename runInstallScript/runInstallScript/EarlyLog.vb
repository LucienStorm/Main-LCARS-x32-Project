Option Strict On

Imports System.IO

''' <summary>
''' Writes a startup breadcrumb before the main form loads so crashes during
''' InitializeComponent still leave evidence on disk.
''' </summary>
Friend Module EarlyLog

    Public Sub WriteStartupBreadcrumb()
        Try
            Dim line As String = DateTime.Now.ToString("u") & "  EarlyLog: process start. StartupPath=" & _
                                 Application.StartupPath & "  Args=" & Environment.CommandLine & vbCrLf
            Dim staging As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LCARS x32")
            staging = Path.Combine(staging, "UpdateStaging")
            Dim common As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "LCARS x32")
            TryAppend(Path.Combine(Application.StartupPath, "lcars-install-log.txt"), line)
            TryAppend(Path.Combine(staging, "lcars-install-log.txt"), line)
            If Not Directory.Exists(common) Then
                Directory.CreateDirectory(common)
            End If
            TryAppend(Path.Combine(common, "lcars-install-log.txt"), line)
        Catch
        End Try
    End Sub

    Public Sub WriteException(ByVal where As String, ByVal ex As Exception)
        Dim line As String = DateTime.Now.ToString("u") & "  EXCEPTION at " & where & ": " & ex.ToString() & vbCrLf
        Dim staging As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LCARS x32")
        staging = Path.Combine(staging, "UpdateStaging")
        Dim common As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "LCARS x32")
        TryAppend(Path.Combine(Application.StartupPath, "lcars-install-log.txt"), line)
        TryAppend(Path.Combine(staging, "lcars-install-log.txt"), line)
        If Not Directory.Exists(common) Then
            Directory.CreateDirectory(common)
        End If
        TryAppend(Path.Combine(common, "lcars-install-log.txt"), line)
    End Sub

    Private Sub TryAppend(ByVal filePath As String, ByVal line As String)
        Try
            Dim parent As String = Path.GetDirectoryName(filePath)
            If Not String.IsNullOrEmpty(parent) AndAlso Not Directory.Exists(parent) Then
                Directory.CreateDirectory(parent)
            End If
            File.AppendAllText(filePath, line)
        Catch
        End Try
    End Sub

End Module
