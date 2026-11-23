' LCARSmain/LCARSmain/Modules/modUtilities.vb
Option Strict On
Option Explicit On

Imports System.Diagnostics
Imports System.IO
Imports System.Windows.Forms

''' <summary>Resolves and launches built-in utility executables next to LCARSmain.exe.</summary>
Public Module modUtilities

    Public Const UtilityPrefix As String = "lcars-util:"

    Public Function CalculatorExePath() As String
        Return Path.Combine(Application.StartupPath, "LCARSCalculator.exe")
    End Function

    Public Function TimerExePath() As String
        Return Path.Combine(Application.StartupPath, "LCARSTimer.exe")
    End Function

    Public Function ScreenshotExePath() As String
        Return Path.Combine(Application.StartupPath, "LCARSScreenshot.exe")
    End Function

    Public Function WeatherExePath() As String
        Return Path.Combine(Application.StartupPath, "LCARSWeather.exe")
    End Function

    Public Function MailExePath() As String
        Dim comms As String = Path.Combine(Application.StartupPath, "LCARScomms.exe")
        If File.Exists(comms) Then Return comms
        Return Path.Combine(Application.StartupPath, "LCARSMail.exe")
    End Function

    Public Function CalendarExePath() As String
        Return MailExePath()
    End Function

    Public Function RssExePath() As String
        Return MailExePath()
    End Function

    Public Function ChatExePath() As String
        Return MailExePath()
    End Function

    Public Function IsUtilityTarget(ByVal target As String) As Boolean
        Return ResolveUtilityExe(target) IsNot Nothing
    End Function

    Public Function ResolveUtilityExe(ByVal target As String) As String
        If String.IsNullOrEmpty(target) Then Return Nothing
        Dim p As String = target.Trim()
        If p.StartsWith(UtilityPrefix, StringComparison.OrdinalIgnoreCase) Then
            Select Case p.Substring(UtilityPrefix.Length).Trim().ToLowerInvariant()
                Case "calculator" : Return CalculatorExePath()
                Case "timer" : Return TimerExePath()
                Case "screenshot" : Return ScreenshotExePath()
                Case "weather" : Return WeatherExePath()
                Case "calendar", "cal" : Return CalendarExePath()
                Case "mail", "comms" : Return MailExePath()
                Case "rss", "feed", "chat", "irc" : Return MailExePath()
                Case Else : Return Nothing
            End Select
        End If
        Dim fileName As String = Path.GetFileName(p)
        If String.Equals(fileName, "LCARSCalculator.exe", StringComparison.OrdinalIgnoreCase) Then Return CalculatorExePath()
        If String.Equals(fileName, "LCARSTimer.exe", StringComparison.OrdinalIgnoreCase) Then Return TimerExePath()
        If String.Equals(fileName, "LCARSScreenshot.exe", StringComparison.OrdinalIgnoreCase) Then Return ScreenshotExePath()
        If String.Equals(fileName, "LCARSWeather.exe", StringComparison.OrdinalIgnoreCase) Then Return WeatherExePath()
        If String.Equals(fileName, "LCARSCalendar.exe", StringComparison.OrdinalIgnoreCase) Then Return CalendarExePath()
        If String.Equals(fileName, "LCARScomms.exe", StringComparison.OrdinalIgnoreCase) Then Return MailExePath()
        If String.Equals(fileName, "LCARSMail.exe", StringComparison.OrdinalIgnoreCase) Then Return MailExePath()
        If String.Equals(fileName, "LCARSRss.exe", StringComparison.OrdinalIgnoreCase) Then Return MailExePath()
        If String.Equals(fileName, "LCARSChat.exe", StringComparison.OrdinalIgnoreCase) Then Return MailExePath()
        If String.Equals(fileName, "LCARSutilities.exe", StringComparison.OrdinalIgnoreCase) Then Return CalculatorExePath()
        Return Nothing
    End Function

    ''' <summary>Launch a built-in utility when the target is a util path/exe; otherwise return False.</summary>
    Public Function TryLaunch(ByVal path As String, Optional ByVal owner As Form = Nothing) As Boolean
        If String.IsNullOrEmpty(path) Then Return False
        Dim exe As String = ResolveUtilityExe(path)
        If exe Is Nothing Then Return False
        LaunchExe(exe)
        Return True
    End Function

    Private Sub LaunchExe(ByVal exePath As String)
        If Not File.Exists(exePath) Then
            MessageBox.Show("Utility not found:" & vbCrLf & exePath, "LCARS", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            Return
        End If
        Dim psi As New ProcessStartInfo()
        psi.FileName = exePath
        psi.WorkingDirectory = Path.GetDirectoryName(exePath)
        psi.UseShellExecute = True
        Process.Start(psi)
    End Sub

End Module
