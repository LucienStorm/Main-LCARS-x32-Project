' LCARSutilities/LCARSutilities/Program.vb
Option Strict On
Option Explicit On
Option Infer On

Imports System.IO
Imports System.Windows.Forms

''' <summary>
''' Shared entry for LCARS* utility exes (same binary, different file name)
''' or LCARSutilities.exe &lt;mode&gt;.
''' </summary>
Module Program
    <STAThread()>
    Public Sub Main(ByVal args() As String)
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)
        AddHandler Application.ThreadException, AddressOf OnThreadException
        AddHandler AppDomain.CurrentDomain.UnhandledException, AddressOf OnUnhandledException
        Try
            Dim mode As String = ResolveMode(args)
            Dim frm As Form = Nothing
            Select Case mode
                Case "timer"
                    frm = New frmTimer()
                Case "screenshot"
                    frm = New frmScreenshot()
                Case "weather"
                    frm = New frmWeather()
                Case "calendar", "cal"
                    frm = New frmCalendar()
                Case "rss", "feed", "feeds"
                    frm = New frmRss()
                Case "chat", "irc"
                    frm = New frmChat()
                Case Else
                    frm = New frmCalculator()
            End Select
            Application.Run(frm)
        Catch ex As Exception
            MessageBox.Show("LCARS utility failed to start:" & vbCrLf & vbCrLf & ex.ToString(), "LCARS Utilities", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub OnThreadException(ByVal sender As Object, ByVal e As Threading.ThreadExceptionEventArgs)
        MessageBox.Show("LCARS utility error:" & vbCrLf & vbCrLf & e.Exception.ToString(), "LCARS Utilities", MessageBoxButtons.OK, MessageBoxIcon.Error)
    End Sub

    Private Sub OnUnhandledException(ByVal sender As Object, ByVal e As UnhandledExceptionEventArgs)
        Dim ex As Exception = TryCast(e.ExceptionObject, Exception)
        Dim msg As String = If(ex IsNot Nothing, ex.ToString(), e.ExceptionObject.ToString())
        MessageBox.Show("LCARS utility fatal error:" & vbCrLf & vbCrLf & msg, "LCARS Utilities", MessageBoxButtons.OK, MessageBoxIcon.Error)
    End Sub

    Private Function ResolveMode(ByVal args() As String) As String
        If args IsNot Nothing AndAlso args.Length > 0 AndAlso Not String.IsNullOrEmpty(args(0)) Then
            Return NormalizeMode(args(0))
        End If
        Dim baseName As String = Path.GetFileNameWithoutExtension(Application.ExecutablePath)
        If baseName Is Nothing Then baseName = ""
        baseName = baseName.Trim()
        If baseName.StartsWith("LCARS", StringComparison.OrdinalIgnoreCase) AndAlso baseName.Length > 5 Then
            Return NormalizeMode(baseName.Substring(5))
        End If
        Return NormalizeMode(baseName)
    End Function

    Private Function NormalizeMode(ByVal raw As String) As String
        Dim m As String = If(raw, "").Trim().ToLowerInvariant()
        If m.StartsWith("/") OrElse m.StartsWith("-") Then m = m.Substring(1)
        Select Case m
            Case "timer", "stopwatch"
                Return "timer"
            Case "screenshot", "screen", "capture"
                Return "screenshot"
            Case "weather", "wx"
                Return "weather"
            Case "calendar", "cal"
                Return "calendar"
            Case "rss", "feed", "feeds"
                Return "rss"
            Case "chat", "irc"
                Return "chat"
            Case Else
                Return "calculator"
        End Select
    End Function
End Module
