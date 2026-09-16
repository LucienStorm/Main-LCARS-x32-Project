Option Strict On

Imports System.Diagnostics
Imports System.IO
Imports System.Reflection
Imports System.Threading
Imports System.Windows.Forms

''' <summary>
''' Verbose file logging for shell freeze/crash investigation.
''' Log file: %ProgramData%\LCARS x32\shell-diagnostics.log
''' </summary>
Public Module modDiagnostics
    Private ReadOnly WriteLock As New Object()
    Friend ReadOnly ScopeDepth As New ThreadLocal(Of Integer)()

    Private Const LogDirName As String = "LCARS x32"
    Private Const LogFileName As String = "shell-diagnostics.log"
    Private Const MaxLogBytes As Long = 5L * 1024L * 1024L

    Public ReadOnly Property LogFilePath As String
        Get
            Return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), LogDirName, LogFileName)
        End Get
    End Property

    Public Sub LogStartupBanner()
        Dim version As String = Assembly.GetExecutingAssembly().GetName().Version.ToString()
        Dim packageVersion As String = TryReadPackageVersion()
        If Not String.IsNullOrEmpty(packageVersion) Then
            version &= " package=" & packageVersion
        End If
        LogInfo("Startup", "LCARSmain " & version & " pid=" & Process.GetCurrentProcess().Id)
    End Sub

    Private Function TryReadPackageVersion() As String
        Try
            Dim versionFile As String = Path.Combine(Application.StartupPath, "versions.txt")
            If Not File.Exists(versionFile) Then Return Nothing
            For Each line As String In File.ReadAllLines(versionFile)
                Dim trimmed As String = line.Trim()
                If trimmed.Length > 0 Then Return trimmed
            Next
        Catch
        End Try
        Return Nothing
    End Function

    Public Sub LogTouch(ByVal category As String, ByVal message As String)
        WriteLine("TOUCH", category, message, True)
    End Sub

    Public Sub LogInfo(ByVal category As String, ByVal message As String)
        WriteLine("INFO", category, message, False)
    End Sub

    Public Sub LogWarn(ByVal category As String, ByVal message As String)
        WriteLine("WARN", category, message, False)
    End Sub

    Public Sub LogError(ByVal category As String, ByVal message As String)
        WriteLine("ERR ", category, message, False)
    End Sub

    Public Sub LogException(ByVal category As String, ByVal ex As Exception, Optional ByVal context As String = Nothing)
        Dim message As String = ex.ToString()
        If Not String.IsNullOrEmpty(context) Then
            message = context & " :: " & message
        End If
        WriteLine("EXCP", category, message, True)
    End Sub

    Public Function BeginScope(ByVal category As String, Optional ByVal details As String = Nothing) As DiagnosticScope
        Return New DiagnosticScope(category, details)
    End Function

    Friend Sub WriteLine(ByVal level As String, ByVal category As String, ByVal message As String, ByVal flushImmediately As Boolean)
        Try
            Dim depth As Integer = 0
            If ScopeDepth IsNot Nothing AndAlso ScopeDepth.IsValueCreated Then
                depth = ScopeDepth.Value
            End If
            Dim line As String = String.Format("{0:yyyy-MM-dd HH:mm:ss.fff} tid={1} depth={2} [{3}] {4}: {5}",
                                                 DateTime.Now,
                                                 Thread.CurrentThread.ManagedThreadId,
                                                 depth,
                                                 level,
                                                 category,
                                                 message)
            SyncLock WriteLock
                Dim dir As String = Path.GetDirectoryName(LogFilePath)
                If Not Directory.Exists(dir) Then
                    Directory.CreateDirectory(dir)
                End If
                RotateIfNeeded()
                If flushImmediately Then
                    Using stream As New FileStream(LogFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)
                        Using writer As New StreamWriter(stream)
                            writer.WriteLine(line)
                            writer.Flush()
                            stream.Flush()
                        End Using
                    End Using
                Else
                    File.AppendAllText(LogFilePath, line & Environment.NewLine)
                End If
            End SyncLock
        Catch
        End Try
    End Sub

    Private Sub RotateIfNeeded()
        Dim info As New FileInfo(LogFilePath)
        If Not info.Exists OrElse info.Length <= MaxLogBytes Then Return
        Dim backup As String = LogFilePath & ".old"
        If File.Exists(backup) Then
            File.Delete(backup)
        End If
        File.Move(LogFilePath, backup)
    End Sub
End Module

''' <summary>
''' Logs method entry/exit with elapsed time and nesting depth.
''' </summary>
Public Class DiagnosticScope
    Implements IDisposable

    Private ReadOnly category As String
    Private ReadOnly sw As Stopwatch
    Private disposed As Boolean = False

    Public Sub New(ByVal category As String, Optional ByVal details As String = Nothing)
        Me.category = category
        modDiagnostics.ScopeDepth.Value = modDiagnostics.ScopeDepth.Value + 1
        sw = Stopwatch.StartNew()
        Dim message As String = "ENTER"
        If Not String.IsNullOrEmpty(details) Then
            message &= " " & details
        End If
        modDiagnostics.WriteLine("CALL", category, message, False)
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        If disposed Then Return
        disposed = True
        sw.Stop()
        modDiagnostics.WriteLine("CALL", category, "EXIT " & sw.ElapsedMilliseconds & "ms", False)
        modDiagnostics.ScopeDepth.Value = Math.Max(0, modDiagnostics.ScopeDepth.Value - 1)
    End Sub
End Class
