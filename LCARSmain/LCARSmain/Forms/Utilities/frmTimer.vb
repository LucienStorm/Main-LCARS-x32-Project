' LCARSmain/LCARSmain/Forms/Utilities/frmTimer.vb
Option Strict On
Option Explicit On
Imports System.Diagnostics
Imports System.Drawing
Imports System.Windows.Forms
''' <summary>LCARS countdown timer (1/5/10 min presets + custom) and stopwatch.</summary>
Public Class frmTimer
Inherits LCARS.LCARSForm
Private Const FormWidth As Integer = 420
Private Const FormHeight As Integer = 380
Private Const TickIntervalMs As Integer = 100
Private Const MaxCustomMinutes As Integer = 999
Private ReadOnly clock As New Stopwatch()
Private WithEvents tickTimer As New Timer()
Private lblDisplay As Label
Private lblMinutes As Label
Private numMinutes As NumericUpDown
Private ReadOnly countdownControls As New System.Collections.Generic.List(Of Control)
Private fbCountdown As LCARS.Controls.FlatButton
Private fbStopwatch As LCARS.Controls.FlatButton
Private isCountdownMode As Boolean = True
Private countdownTotal As TimeSpan = TimeSpan.FromMinutes(1)
Public Sub New()
MyBase.New()
modUtilities.InitUtilityForm(Me, FormWidth, FormHeight) BuildUi()
tickTimer.Interval = TickIntervalMs ShowDisplay()
End Sub
Private Sub BuildUi()
Dim margin As Integer = modUtilities.UtilityMargin
Dim contentWidth As Integer = FormWidth - margin * 2
Dim h As Integer = modUtilities.UtilityButtonHeight modUtilities.MakeUtilityTitle(Me, "TIMER", contentWidth) lblDisplay = modUtilities.MakeUtilityLabel(Me, margin, 48, contentWidth, 70, 40.0F)
lblDisplay.TextAlign = ContentAlignment.MiddleCenter lblDisplay.BorderStyle = BorderStyle.FixedSingle
Dim halfWidth As Integer = (contentWidth - 6) \ 2 fbCountdown = modUtilities.MakeUtilityButton(Me, "COUNTDOWN", margin, 126, halfWidth, h, LCARS.LCARScolorStyles.PrimaryFunction, Sub(s As Object, e As EventArgs) SetMode(True)) fbStopwatch = modUtilities.MakeUtilityButton(Me, "STOPWATCH", margin + halfWidth + 6, 126, halfWidth, h, LCARS.LCARScolorStyles.NavigationFunction, Sub(s As Object, e As EventArgs) SetMode(False))
Dim quarterWidth As Integer = (contentWidth - 6 * 3) \ 4 AddCountdownPreset("1 MIN", 1, 0, quarterWidth) AddCountdownPreset("5 MIN", 5, 1, quarterWidth) AddCountdownPreset("10 MIN", 10, 2, quarterWidth) numMinutes = New NumericUpDown()
numMinutes.Minimum = 1 numMinutes.Maximum = MaxCustomMinutes numMinutes.Value = 1 numMinutes.BackColor = Color.Black numMinutes.ForeColor = Color.Orange numMinutes.Font = New Font(modUtilities.UtilityFontName, 14.0F, FontStyle.Regular, GraphicsUnit.Point)
numMinutes.TextAlign = HorizontalAlignment.Center numMinutes.Location = New Point(margin, 206)
numMinutes.Size = New Size(quarterWidth, h)
Me.Controls.Add(numMinutes)
countdownControls.Add(numMinutes) lblMinutes = modUtilities.MakeUtilityLabel(Me, margin + quarterWidth + 6, 208, quarterWidth, h, 14.0F)
lblMinutes.Text = "MINUTES" countdownControls.Add(lblMinutes)
Dim setButton As Control = modUtilities.MakeUtilityButton(Me, "SET", margin + (quarterWidth + 6) * 3, 206, quarterWidth, h, LCARS.LCARScolorStyles.Orange, Sub(s As Object, e As EventArgs) SetCountdown(CInt(numMinutes.Value)))
countdownControls.Add(setButton)
Dim thirdWidth As Integer = (contentWidth - 6 * 2) \ 3 modUtilities.MakeUtilityButton(Me, "START", margin, 256, thirdWidth, 36, LCARS.LCARScolorStyles.PrimaryFunction, Sub(s As Object, e As EventArgs) StartClock())
modUtilities.MakeUtilityButton(Me, "PAUSE", margin + thirdWidth + 6, 256, thirdWidth, 36, LCARS.LCARScolorStyles.NavigationFunction, Sub(s As Object, e As EventArgs) PauseClock())
modUtilities.MakeUtilityButton(Me, "RESET", margin + (thirdWidth + 6) * 2, 256, thirdWidth, 36, LCARS.LCARScolorStyles.Orange, Sub(s As Object, e As EventArgs) ResetClock())
modUtilities.MakeUtilityButton(Me, "CLOSE", margin, 312, contentWidth, 40, LCARS.LCARScolorStyles.FunctionOffline, Sub(s As Object, e As EventArgs) Close())
End Sub
Private Sub AddCountdownPreset(ByVal caption As String, ByVal minutes As Integer, ByVal slot As Integer, ByVal width As Integer)
Dim btn As Control = modUtilities.MakeUtilityButton(Me, caption, modUtilities.UtilityMargin + slot * (width + 6), 166, width, modUtilities.UtilityButtonHeight, LCARS.LCARScolorStyles.Orange, Sub(s As Object, e As EventArgs) SetCountdown(minutes))
countdownControls.Add(btn)
End Sub
Private Sub SetMode(ByVal countdown As Boolean) isCountdownMode = countdown
For Each ctl As Control In countdownControls ctl.Visible = countdown
Next ResetClock()
End Sub
Private Sub SetCountdown(ByVal minutes As Integer) countdownTotal = TimeSpan.FromMinutes(Math.Max(1, Math.Min(MaxCustomMinutes, minutes))) ResetClock()
End Sub
Private Sub StartClock()
If isCountdownMode AndAlso clock.Elapsed >= countdownTotal Then ResetClock()
clock.Start()
tickTimer.Start()
End Sub
Private Sub PauseClock()
clock.Stop()
tickTimer.Stop() ShowDisplay()
End Sub
Private Sub ResetClock()
clock.Reset()
tickTimer.Stop() ShowDisplay()
End Sub
Private Sub tickTimer_Tick(ByVal sender As Object, ByVal e As EventArgs) Handles tickTimer.Tick
If isCountdownMode AndAlso clock.Elapsed >= countdownTotal Then clock.Stop()
tickTimer.Stop() ShowDisplay()
System.Media.SystemSounds.Beep.Play()
Return
End If ShowDisplay()
End Sub
Private Sub ShowDisplay()
If isCountdownMode Then Dim remaining As TimeSpan = countdownTotal - clock.Elapsed
If remaining < TimeSpan.Zero Then remaining = TimeSpan.Zero ' Round up so the display reads 00:00:01 until the final instant.
Dim wholeSeconds As Long = CLng(Math.Ceiling(remaining.TotalSeconds))
lblDisplay.Text = TimeSpan.FromSeconds(wholeSeconds).ToString("hh\:mm\:ss")
Return
End If
Dim elapsed As TimeSpan = clock.Elapsed lblDisplay.Text = elapsed.ToString("hh\:mm\:ss") & "." & (elapsed.Milliseconds \ 100).ToString()
End Sub
Protected Overrides Sub OnFormClosed(ByVal e As FormClosedEventArgs)
tickTimer.Stop()
tickTimer.Dispose()
MyBase.OnFormClosed(e)
End Sub
End Class
