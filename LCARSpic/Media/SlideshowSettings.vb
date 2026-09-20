' LCARSpic/Media/SlideshowSettings.vb
Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Windows.Forms
Imports Microsoft.VisualBasic

''' <summary>
''' Persisted photo slideshow options under LCARS x32 / LCARSmedia.
''' </summary>
Public Module SlideshowSettingsStore
    Private Const AppName As String = "LCARS x32"
    Private Const Section As String = "LCARSmedia"

    Public Property IntervalSeconds As Integer
        Get
            Dim v As Integer
            If Integer.TryParse(GetSetting(AppName, Section, "SlideIntervalSec", "5"), v) AndAlso v >= 1 Then
                Return Math.Min(120, v)
            End If
            Return 5
        End Get
        Set(ByVal value As Integer)
            SaveSetting(AppName, Section, "SlideIntervalSec", Math.Max(1, Math.Min(120, value)).ToString())
        End Set
    End Property

    Public Property LoopEnabled As Boolean
        Get
            Return String.Equals(GetSetting(AppName, Section, "SlideLoop", "True"), "True", StringComparison.OrdinalIgnoreCase)
        End Get
        Set(ByVal value As Boolean)
            SaveSetting(AppName, Section, "SlideLoop", value.ToString())
        End Set
    End Property

    Public Property ShuffleEnabled As Boolean
        Get
            Return String.Equals(GetSetting(AppName, Section, "SlideShuffle", "False"), "True", StringComparison.OrdinalIgnoreCase)
        End Get
        Set(ByVal value As Boolean)
            SaveSetting(AppName, Section, "SlideShuffle", value.ToString())
        End Set
    End Property
End Module

''' <summary>Minimal LCARS-styled slideshow settings dialog.</summary>
Public Class frmSlideshowSettings
    Inherits Form

    Private ReadOnly numInterval As NumericUpDown
    Private ReadOnly chkLoop As CheckBox
    Private ReadOnly chkShuffle As CheckBox

    Public Sub New()
        Text = "SLIDESHOW SETTINGS"
        FormBorderStyle = FormBorderStyle.FixedDialog
        MaximizeBox = False
        MinimizeBox = False
        ShowInTaskbar = False
        StartPosition = FormStartPosition.CenterParent
        BackColor = Color.Black
        ForeColor = Color.Orange
        ClientSize = New Size(320, 200)
        Font = New Font("LCARS", 12.0F, FontStyle.Regular)

        Dim lbl As New Label()
        lbl.Text = "INTERVAL (SEC)"
        lbl.ForeColor = Color.Orange
        lbl.Location = New Point(16, 20)
        lbl.AutoSize = True
        Controls.Add(lbl)

        numInterval = New NumericUpDown()
        numInterval.Minimum = 1
        numInterval.Maximum = 120
        numInterval.Value = SlideshowSettingsStore.IntervalSeconds
        numInterval.Location = New Point(180, 16)
        numInterval.Width = 80
        numInterval.BackColor = Color.FromArgb(40, 40, 40)
        numInterval.ForeColor = Color.Orange
        Controls.Add(numInterval)

        chkLoop = New CheckBox()
        chkLoop.Text = "LOOP"
        chkLoop.ForeColor = Color.Orange
        chkLoop.Checked = SlideshowSettingsStore.LoopEnabled
        chkLoop.Location = New Point(16, 60)
        chkLoop.AutoSize = True
        Controls.Add(chkLoop)

        chkShuffle = New CheckBox()
        chkShuffle.Text = "SHUFFLE"
        chkShuffle.ForeColor = Color.Orange
        chkShuffle.Checked = SlideshowSettingsStore.ShuffleEnabled
        chkShuffle.Location = New Point(16, 95)
        chkShuffle.AutoSize = True
        Controls.Add(chkShuffle)

        Dim btnOk As New Button()
        btnOk.Text = "OK"
        btnOk.BackColor = Color.FromArgb(255, 153, 0)
        btnOk.ForeColor = Color.Black
        btnOk.FlatStyle = FlatStyle.Flat
        btnOk.Location = New Point(80, 145)
        btnOk.Size = New Size(70, 28)
        AddHandler btnOk.Click, AddressOf Ok_Click
        Controls.Add(btnOk)

        Dim btnCancel As New Button()
        btnCancel.Text = "CANCEL"
        btnCancel.BackColor = Color.FromArgb(120, 80, 40)
        btnCancel.ForeColor = Color.Black
        btnCancel.FlatStyle = FlatStyle.Flat
        btnCancel.Location = New Point(170, 145)
        btnCancel.Size = New Size(70, 28)
        AddHandler btnCancel.Click, Sub()
                                        DialogResult = DialogResult.Cancel
                                        Close()
                                    End Sub
        Controls.Add(btnCancel)
        AcceptButton = btnOk
        CancelButton = btnCancel
    End Sub

    Private Sub Ok_Click(ByVal sender As Object, ByVal e As EventArgs)
        SlideshowSettingsStore.IntervalSeconds = CInt(numInterval.Value)
        SlideshowSettingsStore.LoopEnabled = chkLoop.Checked
        SlideshowSettingsStore.ShuffleEnabled = chkShuffle.Checked
        DialogResult = DialogResult.OK
        Close()
    End Sub
End Class
