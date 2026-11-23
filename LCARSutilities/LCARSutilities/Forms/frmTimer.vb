Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Drawing
Imports System.Media
Imports System.Runtime.CompilerServices
Imports System.Windows.Forms
Imports LCARS
Imports LCARS.Controls

    Public Class frmTimer
        Inherits LCARSForm
        Private Const FormWidth As Integer = 420

        Private Const FormHeight As Integer = 380

        Private Const TickIntervalMs As Integer = 100

        Private Const MaxCustomMinutes As Integer = 999

        Private ReadOnly clock As Stopwatch

        <AccessedThroughProperty("tickTimer")>
        Private _tickTimer As Timer

        Private lblDisplay As Label

        Private lblMinutes As Label

        Private numMinutes As NumericUpDown

        Private ReadOnly countdownControls As List(Of Control)

        Private fbCountdown As FlatButton

        Private fbStopwatch As FlatButton

        Private isCountdownMode As Boolean

        Private countdownTotal As TimeSpan

        Private Overridable Property tickTimer As Timer
            <DebuggerNonUserCode>
            Get
                Return _tickTimer
            End Get
            <MethodImpl(MethodImplOptions.Synchronized)>
            <DebuggerNonUserCode>
            Set(value As Timer)
                Dim value2 = AddressOf tickTimer_Tick
                If _tickTimer IsNot Nothing Then
                    RemoveHandler _tickTimer.Tick, value2
                End If
                _tickTimer = value
                If _tickTimer IsNot Nothing Then
                    AddHandler _tickTimer.Tick, value2
                End If
            End Set
        End Property

        Public Sub New()
            clock = New Stopwatch()
            tickTimer = New Timer()
            countdownControls = New List(Of Control)()
            isCountdownMode = True
            countdownTotal = TimeSpan.FromMinutes(1.0)
            modUtilities.InitUtilityForm(Me, 420, 380)
            BuildUi()
            tickTimer.Interval = 100
            ShowDisplay()
        End Sub

        Private Sub BuildUi()
            Dim num = 12
            Dim num2 = 420 - num * 2
            Dim num3 = 28
            modUtilities.MakeUtilityTitle(Me, "TIMER", num2)
            lblDisplay = modUtilities.MakeUtilityLabel(Me, num, 48, num2, 70, 40F)
            lblDisplay.TextAlign = ContentAlignment.MiddleCenter
            lblDisplay.BorderStyle = BorderStyle.FixedSingle
            Dim num4 As Integer = num2 - 6 / 2
            fbCountdown = modUtilities.MakeUtilityButton(Me, "COUNTDOWN", num, 126, num4, num3, LCARScolorStyles.PrimaryFunction, Sub(s, e) SetMode(countdown:=True))
            fbStopwatch = modUtilities.MakeUtilityButton(Me, "STOPWATCH", num + num4 + 6, 126, num4, num3, LCARScolorStyles.NavigationFunction, Sub(s, e) SetMode(countdown:=False))
            Dim num5 As Integer = num2 - 18 / 4
            AddCountdownPreset("1 MIN", 1, 0, num5)
            AddCountdownPreset("5 MIN", 5, 1, num5)
            AddCountdownPreset("10 MIN", 10, 2, num5)
            numMinutes = New NumericUpDown()
            numMinutes.Minimum = 1D
            numMinutes.Maximum = 999D
            numMinutes.Value = 1D
            numMinutes.BackColor = Color.Black
            numMinutes.ForeColor = Color.Orange
            numMinutes.Font = New Font("Arial Narrow", 14F, FontStyle.Regular, GraphicsUnit.Point)
            numMinutes.TextAlign = HorizontalAlignment.Center
            Dim numericUpDown = numMinutes
            Dim location As Point = New Point(num, 206)
            numericUpDown.Location = location
            Dim numericUpDown2 = numMinutes
            Dim size As Size = New Size(num5, num3)
            numericUpDown2.Size = size
            Controls.Add(numMinutes)
            countdownControls.Add(numMinutes)
            lblMinutes = modUtilities.MakeUtilityLabel(Me, num + num5 + 6, 208, num5, num3, 14F)
            lblMinutes.Text = "MINUTES"
            countdownControls.Add(lblMinutes)
            Dim item As Control = modUtilities.MakeUtilityButton(Me, "SET", num + (num5 + 6) * 3, 206, num5, num3, LCARScolorStyles.Orange, Sub(s, e) SetCountdown(Convert.ToInt32(numMinutes.Value)))
            countdownControls.Add(item)
            Dim num6 As Integer = num2 - 12 / 3
            modUtilities.MakeUtilityButton(Me, "START", num, 256, num6, 36, LCARScolorStyles.PrimaryFunction, Sub(s, e) StartClock())
            modUtilities.MakeUtilityButton(Me, "PAUSE", num + num6 + 6, 256, num6, 36, LCARScolorStyles.NavigationFunction, Sub(s, e) PauseClock())
            modUtilities.MakeUtilityButton(Me, "RESET", num + (num6 + 6) * 2, 256, num6, 36, LCARScolorStyles.Orange, Sub(s, e) ResetClock())
            modUtilities.MakeUtilityButton(Me, "CLOSE", num, 312, num2, 40, LCARScolorStyles.FunctionOffline, Sub(s, e) Close())
        End Sub

        Private Sub AddCountdownPreset(caption As String, minutes As Integer, slot As Integer, width As Integer)
            Dim item As Control = modUtilities.MakeUtilityButton(Me, caption, 12 + slot * (width + 6), 166, width, 28, LCARScolorStyles.Orange, Sub(s, e) SetCountdown(minutes))
            countdownControls.Add(item)
        End Sub

        Private Sub SetMode(countdown As Boolean)
            isCountdownMode = countdown
            For Each countdownControl In countdownControls
                countdownControl.Visible = countdown
            Next
            ResetClock()
        End Sub

        Private Sub SetCountdown(minutes As Integer)
            countdownTotal = TimeSpan.FromMinutes(Math.Max(1, Math.Min(999, minutes)))
            ResetClock()
        End Sub

        Private Sub StartClock()
            If isCountdownMode AndAlso clock.Elapsed >= countdownTotal Then
                ResetClock()
            End If
            clock.Start()
            tickTimer.Start()
        End Sub

        Private Sub PauseClock()
            clock.Stop()
            tickTimer.Stop()
            ShowDisplay()
        End Sub

        Private Sub ResetClock()
            clock.Reset()
            tickTimer.Stop()
            ShowDisplay()
        End Sub

        Private Sub tickTimer_Tick(sender As Object, e As EventArgs)
            If isCountdownMode AndAlso clock.Elapsed >= countdownTotal Then
                clock.Stop()
                tickTimer.Stop()
                ShowDisplay()
                Call SystemSounds.Beep.Play()
            Else
                ShowDisplay()
            End If
        End Sub

        Private Sub ShowDisplay()
            If isCountdownMode Then
                Dim timeSpan = countdownTotal - clock.Elapsed
                If timeSpan < TimeSpan.Zero Then
                    timeSpan = TimeSpan.Zero
                End If
                Dim num As Long = Math.Round(Math.Ceiling(timeSpan.TotalSeconds))
                lblDisplay.Text = TimeSpan.FromSeconds(num).ToString("hh\:mm\:ss")
            Else
                Dim elapsed = clock.Elapsed
                lblDisplay.Text = elapsed.ToString("hh\:mm\:ss") & "." & elapsed.Milliseconds / 100.ToString()
            End If
        End Sub

        Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
            tickTimer.Stop()
            tickTimer.Dispose()
            MyBase.OnFormClosed(e)
        End Sub
    End Class
