' LCARSpic/Media/MediaTransportControls.vb
Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Full A/V transport cluster for the right rail (VLC-replacement coverage).
''' </summary>
Public Class MediaTransportControls
    Public ReadOnly Buttons As New List(Of LCARS.Controls.StandardButton)()
    Public ReadOnly SeekBar As TrackBar
    Public ReadOnly TimeLabel As Label

    Public ReadOnly PlayPause As LCARS.Controls.StandardButton
    Public ReadOnly StopBtn As LCARS.Controls.StandardButton
    Public ReadOnly Rewind As LCARS.Controls.StandardButton
    Public ReadOnly Forward As LCARS.Controls.StandardButton
    Public ReadOnly Mute As LCARS.Controls.StandardButton
    Public ReadOnly VolDown As LCARS.Controls.StandardButton
    Public ReadOnly VolUp As LCARS.Controls.StandardButton
    Public ReadOnly LoopBtn As LCARS.Controls.StandardButton
    Public ReadOnly Speed As LCARS.Controls.StandardButton
    Public ReadOnly AudioTrack As LCARS.Controls.StandardButton
    Public ReadOnly Subtitles As LCARS.Controls.StandardButton
    Public ReadOnly Fullscreen As LCARS.Controls.StandardButton

    Public Sub New(ByVal host As Control)
        PlayPause = MakeBtn("PLAY/PAUSE", LCARS.LCARScolorStyles.PrimaryFunction)
        StopBtn = MakeBtn("STOP", LCARS.LCARScolorStyles.FunctionOffline)
        Rewind = MakeBtn("REW 10s", LCARS.LCARScolorStyles.SystemFunction)
        Forward = MakeBtn("FFWD 10s", LCARS.LCARScolorStyles.SystemFunction)
        Mute = MakeBtn("MUTE", LCARS.LCARScolorStyles.SystemFunction)
        VolDown = MakeBtn("VOL −", LCARS.LCARScolorStyles.SystemFunction)
        VolUp = MakeBtn("VOL +", LCARS.LCARScolorStyles.SystemFunction)
        LoopBtn = MakeBtn("LOOP OFF", LCARS.LCARScolorStyles.SystemFunction)
        Speed = MakeBtn("SPEED 1x", LCARS.LCARScolorStyles.SystemFunction)
        AudioTrack = MakeBtn("AUDIO TRK", LCARS.LCARScolorStyles.SystemFunction)
        Subtitles = MakeBtn("SUBTITLES", LCARS.LCARScolorStyles.SystemFunction)
        Fullscreen = MakeBtn("FULLSCREEN", LCARS.LCARScolorStyles.PrimaryFunction)

        For Each b As LCARS.Controls.StandardButton In Buttons
            b.Visible = False
            host.Controls.Add(b)
        Next

        SeekBar = New TrackBar()
        SeekBar.Minimum = 0
        SeekBar.Maximum = 1000
        SeekBar.TickStyle = TickStyle.None
        SeekBar.Height = 28
        SeekBar.Visible = False
        SeekBar.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        host.Controls.Add(SeekBar)

        TimeLabel = New Label()
        TimeLabel.ForeColor = Color.Orange
        TimeLabel.Font = New Font("LCARS", 12.0F, FontStyle.Regular)
        TimeLabel.AutoSize = False
        TimeLabel.Height = 22
        TimeLabel.TextAlign = ContentAlignment.MiddleLeft
        TimeLabel.Text = "00:00 / 00:00"
        TimeLabel.Visible = False
        TimeLabel.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        host.Controls.Add(TimeLabel)
    End Sub

    Public Sub SetVisible(ByVal show As Boolean, ByVal isVideo As Boolean)
        For Each b As LCARS.Controls.StandardButton In Buttons
            b.Visible = show
        Next
        AudioTrack.Visible = show AndAlso isVideo
        Subtitles.Visible = show AndAlso isVideo
        Fullscreen.Visible = show AndAlso isVideo
        SeekBar.Visible = show
        TimeLabel.Visible = show
    End Sub

    ''' <summary>Lay out from bottom Y upward; returns new Y above the cluster.</summary>
    Public Function LayoutAbove(ByVal railLeft As Integer, ByVal railW As Integer, ByVal y As Integer, ByVal gap As Integer) As Integer
        Dim btnH As Integer = 28
        Dim ordered As New List(Of LCARS.Controls.StandardButton)()
        If Fullscreen.Visible Then ordered.Add(Fullscreen)
        If Subtitles.Visible Then ordered.Add(Subtitles)
        If AudioTrack.Visible Then ordered.Add(AudioTrack)
        ordered.Add(Speed)
        ordered.Add(LoopBtn)
        ordered.Add(VolUp)
        ordered.Add(VolDown)
        ordered.Add(Mute)
        ordered.Add(Forward)
        ordered.Add(Rewind)
        ordered.Add(StopBtn)
        ordered.Add(PlayPause)

        ' Stack bottom-up: first in list sits lowest (just above y).
        For i As Integer = 0 To ordered.Count - 1
            Dim b As LCARS.Controls.StandardButton = ordered(i)
            b.Size = New Size(railW, btnH)
            b.Location = New Point(railLeft, y - btnH)
            b.BringToFront()
            y = b.Top - gap
        Next
        Return y
    End Function

    Public Sub LayoutSeek(ByVal left As Integer, ByVal width As Integer, ByVal bottom As Integer)
        TimeLabel.Location = New Point(left, bottom - TimeLabel.Height)
        TimeLabel.Width = Math.Max(120, width \ 3)
        SeekBar.Location = New Point(TimeLabel.Right + 8, bottom - SeekBar.Height + 4)
        SeekBar.Width = Math.Max(80, width - TimeLabel.Width - 16)
        TimeLabel.BringToFront()
        SeekBar.BringToFront()
    End Sub

    Private Function MakeBtn(ByVal text As String, ByVal color As LCARS.LCARScolorStyles) As LCARS.Controls.StandardButton
        Dim b As New LCARS.Controls.StandardButton()
        b.ButtonText = text
        b.Text = text
        b.Color = color
        b.Size = New Size(130, 28)
        Buttons.Add(b)
        Return b
    End Function
End Class
