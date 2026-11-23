Option Strict On

Imports System.Diagnostics
Imports System.IO
Imports System.Reflection
Imports System.Runtime.ExceptionServices
Imports System.Threading
Imports System.Windows.Forms

''' <summary>
''' Verbose file logging for shell freeze/crash investigation.
''' Log files:
'''   %ProgramData%\LCARS x32\shell-diagnostics.log
'''   Desktop\LCARS-ModeSwitch.log
''' </summary>
Public Module modDiagnostics
    Private ReadOnly WriteLock As New Object()
    Friend ReadOnly ScopeDepth As New ThreadLocal(Of Integer)()
    Private ReadOnly InFirstChance As New ThreadLocal(Of Boolean)()
    Private handlersInstalled As Boolean = False
    Private modeTraceUntilUtc As DateTime = DateTime.MinValue

    Private Const LogDirName As String = "LCARS x32"
    Private Const LogFileName As String = "shell-diagnostics.log"
    Private Const DesktopTraceFileName As String = "LCARS-ModeSwitch.log"
    Private Const MaxLogBytes As Long = 5L * 1024L * 1024L

    Public ReadOnly Property LogFilePath As String
        Get
            Return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), LogDirName, LogFileName)
        End Get
    End Property

    Public ReadOnly Property DesktopTraceFilePath As String
        Get
            Return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), DesktopTraceFileName)
        End Get
    End Property

    Public Sub EnsureCrashHooksInstalled()
        If handlersInstalled Then Return
        handlersInstalled = True
        Try
            AddHandler AppDomain.CurrentDomain.FirstChanceException, AddressOf OnFirstChanceException
            AddHandler AppDomain.CurrentDomain.UnhandledException, AddressOf OnDomainUnhandledException
        Catch
        End Try
    End Sub

    Public Sub BeginModeSwitchTrace(ByVal details As String)
        EnsureCrashHooksInstalled()
        modeTraceUntilUtc = DateTime.UtcNow.AddSeconds(20)
        WriteLine("STEP", "ModeSwitch.Begin", details, True)
    End Sub

    Public Sub LogStep(ByVal category As String, ByVal message As String)
        EnsureCrashHooksInstalled()
        WriteLine("STEP", category, message, True)
    End Sub

    Private Sub OnFirstChanceException(ByVal sender As Object, ByVal e As FirstChanceExceptionEventArgs)
        If e Is Nothing OrElse e.Exception Is Nothing Then Return
        If InFirstChance.IsValueCreated AndAlso InFirstChance.Value Then Return
        Try
            InFirstChance.Value = True
            Dim trace As String = Environment.StackTrace
            WriteLine("FCHC", e.Exception.GetType().FullName, e.Exception.Message & " | " & CompactStack(trace), True)
        Catch
        Finally
            InFirstChance.Value = False
        End Try
    End Sub

    Private Sub OnDomainUnhandledException(ByVal sender As Object, ByVal e As UnhandledExceptionEventArgs)
        Try
            Dim exObj As Object = If(e Is Nothing, Nothing, e.ExceptionObject)
            Dim msg As String = If(exObj Is Nothing, "null", exObj.ToString())
            WriteLine("FATL", "AppDomain.UnhandledException", "isTerminating=" & If(e Is Nothing, "?", e.IsTerminating.ToString()) & " :: " & msg, True)
        Catch
        End Try
    End Sub

    Private Function CompactStack(ByVal raw As String) As String
        If String.IsNullOrEmpty(raw) Then Return ""
        Dim lines() As String = raw.Split(New Char() {ChrW(13), ChrW(10)}, StringSplitOptions.RemoveEmptyEntries)
        Dim sb As New System.Text.StringBuilder()
        Dim count As Integer = 0
        For Each line As String In lines
            Dim trimmed As String = line.Trim()
            If trimmed.IndexOf("System.Environment.GetStackTrace", StringComparison.OrdinalIgnoreCase) >= 0 OrElse
               trimmed.IndexOf("System.Environment.get_StackTrace", StringComparison.OrdinalIgnoreCase) >= 0 OrElse
               trimmed.IndexOf("modDiagnostics.OnFirstChanceException", StringComparison.OrdinalIgnoreCase) >= 0 Then
                Continue For
            End If
            If sb.Length > 0 Then sb.Append(" <- ")
            sb.Append(trimmed)
            count += 1
            If count >= 8 Then Exit For
        Next
        Return sb.ToString()
    End Function

    Public Sub LogStartupBanner()
        EnsureCrashHooksInstalled()
        Dim version As String = Assembly.GetExecutingAssembly().GetName().Version.ToString()
        Dim packageVersion As String = TryReadPackageVersion()
        If Not String.IsNullOrEmpty(packageVersion) Then
            version &= " package=" & packageVersion
        End If
        modeTraceUntilUtc = DateTime.UtcNow.AddSeconds(20)
        LogStep("Startup", "LCARSmain " & version & " pid=" & Process.GetCurrentProcess().Id & " ptrSize=" & IntPtr.Size.ToString())
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
            Dim inWindow As Boolean = DateTime.UtcNow <= modeTraceUntilUtc
            Dim forceFlush As Boolean = flushImmediately OrElse inWindow OrElse
                level = "STEP" OrElse level = "EXCP" OrElse level = "FCHC" OrElse level = "FATL" OrElse level = "ERR " OrElse level = "WARN"
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
                AppendLineToFile(LogFilePath, line, forceFlush)
                If forceFlush Then
                    AppendLineToFile(DesktopTraceFilePath, line, True)
                End If
            End SyncLock
        Catch
        End Try
    End Sub

    Private Sub AppendLineToFile(ByVal path As String, ByVal line As String, ByVal flushImmediately As Boolean)
        Try
            If flushImmediately Then
                Using stream As New FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)
                    Using writer As New StreamWriter(stream)
                        writer.WriteLine(line)
                        writer.Flush()
                        stream.Flush(True)
                    End Using
                End Using
            Else
                File.AppendAllText(path, line & Environment.NewLine)
            End If
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
