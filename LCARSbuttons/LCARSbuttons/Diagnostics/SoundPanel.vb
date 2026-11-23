' LCARSbuttons/LCARSbuttons/Diagnostics/SoundPanel.vb
Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Drawing
Imports System.Diagnostics
Imports System.Windows.Forms
Imports LCARS.Controls

Namespace Diagnostics

''' <summary>
''' Live volume/mute + Playback / Recording / Communications device selection.
''' Windows sound-scheme effects stay in LCARS Settings — not here.
''' </summary>
Public Class SoundPanel
    Inherits Panel
    Implements IEngineeringPanelRefresh

    Private Enum DeviceCategory
        Playback = 0
        Recording = 1
        Communications = 2
    End Enum

    Private ReadOnly lblDefaults As New Label()
    Private ReadOnly lblVol As New Label()
    Private ReadOnly trkVol As New System.Windows.Forms.TrackBar()
    Private ReadOnly lstDevices As New ListBox()
    Private ReadOnly status As New Label()
    Private ReadOnly poll As New Timer()

    Private fbRefresh As FlatButton
    Private fbMute As FlatButton
    Private fbUp As FlatButton
    Private fbDown As FlatButton
    Private fbCatPlayback As FlatButton
    Private fbCatRecording As FlatButton
    Private fbCatComms As FlatButton
    Private fbSetDefault As FlatButton
    Private fbSetComms As FlatButton
    Private fbMixer As FlatButton

    Private _category As DeviceCategory = DeviceCategory.Playback
    Private _suppress As Boolean = False
    Private _loaded As Boolean = False
    Private devices As New List(Of AudioEndpointInfo)()

    Public Sub New()
        BackColor = Color.Black
        Dock = DockStyle.Fill

        lblDefaults.ForeColor = Color.Orange
        lblDefaults.BackColor = Color.Black
        lblDefaults.Font = New Font("Arial Narrow", 9.0!, FontStyle.Bold)
        lblDefaults.AutoSize = False
        lblDefaults.Text = "DEFAULTS"

        lblVol.ForeColor = Color.Orange
        lblVol.BackColor = Color.Black
        lblVol.Font = New Font("Arial Narrow", 11.0!, FontStyle.Bold)
        lblVol.Text = "VOLUME"
        lblVol.AutoSize = False

        trkVol.Minimum = 0
        trkVol.Maximum = 100
        trkVol.TickFrequency = 10
        trkVol.BackColor = Color.Black
        trkVol.AutoSize = False
        AddHandler trkVol.ValueChanged, AddressOf OnVolumeChanged

        lstDevices.BackColor = Color.Black
        lstDevices.ForeColor = Color.Orange
        lstDevices.BorderStyle = BorderStyle.FixedSingle
        lstDevices.Font = New Font("Arial Narrow", 10.0!, FontStyle.Bold)
        lstDevices.IntegralHeight = False

        status.ForeColor = Color.Orange
        status.BackColor = Color.Black
        status.Font = New Font("Arial Narrow", 10.0!, FontStyle.Bold)
        status.Text = "READY"
        status.AutoSize = False

        fbRefresh = MakeButton("REFRESH", LCARS.LCARScolorStyles.PrimaryFunction)
        AddHandler fbRefresh.Click, Sub() SyncFromSystem(True)

        fbMute = MakeButton("MUTE", LCARS.LCARScolorStyles.Orange)
        AddHandler fbMute.Click, AddressOf OnMuteToggle

        fbUp = MakeButton("VOL +", LCARS.LCARScolorStyles.PrimaryFunction)
        AddHandler fbUp.Click, Sub()
                                   AudioVolumeService.VolumeStep(True)
                                   SyncVolumeOnly()
                               End Sub
        fbDown = MakeButton("VOL −", LCARS.LCARScolorStyles.PrimaryFunction)
        AddHandler fbDown.Click, Sub()
                                     AudioVolumeService.VolumeStep(False)
                                     SyncVolumeOnly()
                                 End Sub

        fbCatPlayback = MakeButton("PLAYBACK", LCARS.LCARScolorStyles.PrimaryFunction)
        AddHandler fbCatPlayback.Click, Sub() SetCategory(DeviceCategory.Playback)
        fbCatRecording = MakeButton("RECORDING", LCARS.LCARScolorStyles.PrimaryFunction)
        AddHandler fbCatRecording.Click, Sub() SetCategory(DeviceCategory.Recording)
        fbCatComms = MakeButton("COMMS", LCARS.LCARScolorStyles.PrimaryFunction)
        AddHandler fbCatComms.Click, Sub() SetCategory(DeviceCategory.Communications)

        fbSetDefault = MakeButton("SET DEFAULT", LCARS.LCARScolorStyles.PrimaryFunction)
        AddHandler fbSetDefault.Click, AddressOf OnSetDefault
        fbSetComms = MakeButton("SET COMMS", LCARS.LCARScolorStyles.MiscFunction)
        AddHandler fbSetComms.Click, AddressOf OnSetComms
        fbMixer = MakeButton("MIXER", LCARS.LCARScolorStyles.MiscFunction)
        AddHandler fbMixer.Click, Sub()
                                      Try
                                          Process.Start("sndvol.exe")
                                      Catch
                                          ShellProcess.StartControlPanel("mmsys.cpl")
                                      End Try
                                  End Sub

        poll.Interval = 500
        AddHandler poll.Tick, Sub()
                                  If Not IsHandleCreated OrElse Not Visible OrElse Height < 40 Then Return
                                  If trkVol.Capture Then Return
                                  SyncVolumeOnly()
                              End Sub

        Controls.Add(lblDefaults)
        Controls.Add(lblVol)
        Controls.Add(trkVol)
        Controls.Add(lstDevices)
        Controls.Add(status)
        Controls.Add(fbRefresh)
        Controls.Add(fbMute)
        Controls.Add(fbUp)
        Controls.Add(fbDown)
        Controls.Add(fbCatPlayback)
        Controls.Add(fbCatRecording)
        Controls.Add(fbCatComms)
        Controls.Add(fbSetDefault)
        Controls.Add(fbSetComms)
        Controls.Add(fbMixer)

        AddHandler Me.Resize, Sub() LayoutChildren()
        AddHandler Me.VisibleChanged, Sub()
                                          If Visible AndAlso Height > 40 Then OnSectionShown()
                                      End Sub
        AddHandler Me.HandleDestroyed, Sub() poll.Stop()
        LayoutChildren()
        HighlightCategory()
    End Sub

    Public Sub OnSectionShown() Implements IEngineeringPanelRefresh.OnSectionShown
        SyncFromSystem(True)
        poll.Start()
    End Sub

    Private Function MakeButton(ByVal caption As String, ByVal color As LCARS.LCARScolorStyles) As FlatButton
        Dim b As New FlatButton()
        b.ButtonText = caption
        b.Text = caption
        b.Clickable = True
        b.Beeping = True
        b.Color = color
        b.ButtonTextAlign = ContentAlignment.BottomRight
        b.Size = New Size(100, 26)
        Return b
    End Function

    Private Sub LayoutChildren()
        Dim pad As Integer = 4
        Dim y As Integer = pad
        Dim x As Integer = pad
        For Each b As FlatButton In New FlatButton() {fbRefresh, fbMute, fbUp, fbDown, fbMixer}
            b.Location = New Point(x, y)
            b.Size = New Size(88, 24)
            x += 92
        Next
        y += 28
        lblVol.SetBounds(pad, y, Math.Max(60, ClientSize.Width - pad * 2), 18)
        y += 18
        trkVol.SetBounds(pad, y, Math.Max(60, ClientSize.Width - pad * 2), 36)
        y += 38
        lblDefaults.SetBounds(pad, y, Math.Max(60, ClientSize.Width - pad * 2), 32)
        y += 34
        x = pad
        For Each b As FlatButton In New FlatButton() {fbCatPlayback, fbCatRecording, fbCatComms}
            b.Location = New Point(x, y)
            b.Size = New Size(100, 24)
            x += 104
        Next
        y += 28
        x = pad
        For Each b As FlatButton In New FlatButton() {fbSetDefault, fbSetComms}
            b.Location = New Point(x, y)
            b.Size = New Size(110, 24)
            x += 114
        Next
        y += 28
        status.SetBounds(pad, Math.Max(y + 40, ClientSize.Height - 18), Math.Max(60, ClientSize.Width - pad * 2), 16)
        lstDevices.SetBounds(pad, y, Math.Max(60, ClientSize.Width - pad * 2), Math.Max(40, status.Top - y - 4))
    End Sub

    Private Sub SetCategory(ByVal cat As DeviceCategory)
        _category = cat
        HighlightCategory()
        ReloadDeviceList()
        UpdateActionHints()
    End Sub

    Private Sub HighlightCategory()
        fbCatPlayback.Color = If(_category = DeviceCategory.Playback, LCARS.LCARScolorStyles.Orange, LCARS.LCARScolorStyles.PrimaryFunction)
        fbCatRecording.Color = If(_category = DeviceCategory.Recording, LCARS.LCARScolorStyles.Orange, LCARS.LCARScolorStyles.PrimaryFunction)
        fbCatComms.Color = If(_category = DeviceCategory.Communications, LCARS.LCARScolorStyles.Orange, LCARS.LCARScolorStyles.PrimaryFunction)
        fbCatPlayback.Invalidate()
        fbCatRecording.Invalidate()
        fbCatComms.Invalidate()
    End Sub

    Private Sub UpdateActionHints()
        Select Case _category
            Case DeviceCategory.Playback
                fbSetDefault.ButtonText = "SET DEFAULT"
                fbSetComms.ButtonText = "SET COMMS OUT"
                fbSetComms.Visible = True
            Case DeviceCategory.Recording
                fbSetDefault.ButtonText = "SET DEFAULT"
                fbSetComms.ButtonText = "SET COMMS IN"
                fbSetComms.Visible = True
            Case Else
                fbSetDefault.ButtonText = "SET COMMS"
                fbSetComms.ButtonText = "SET ALL ROLES"
                fbSetComms.Visible = True
        End Select
        fbSetDefault.Text = fbSetDefault.ButtonText
        fbSetComms.Text = fbSetComms.ButtonText
    End Sub

    Private Sub SyncFromSystem(ByVal reloadDevices As Boolean)
        SyncVolumeOnly()
        UpdateDefaultsLabel()
        If reloadDevices OrElse Not _loaded Then
            ReloadDeviceList()
            UpdateActionHints()
            _loaded = True
            status.Text = "SYNCED"
        End If
        LayoutChildren()
    End Sub

    Private Sub SyncVolumeOnly()
        _suppress = True
        Try
            Dim pct As Integer = AudioVolumeService.GetVolumePercent()
            If pct < 0 Then
                lblVol.Text = "VOLUME  UNAVAILABLE"
            Else
                If trkVol.Value <> pct Then trkVol.Value = pct
                lblVol.Text = "VOLUME  " & pct.ToString() & "%"
            End If
            Dim muted As Boolean = AudioVolumeService.IsMuted()
            fbMute.ButtonText = If(muted, "UNMUTE", "MUTE")
            fbMute.Text = fbMute.ButtonText
        Finally
            _suppress = False
        End Try
    End Sub

    Private Sub UpdateDefaultsLabel()
        Dim play As String = AudioEndpointService.GetDefaultName(True, 0)
        Dim rec As String = AudioEndpointService.GetDefaultName(False, 0)
        Dim commOut As String = AudioEndpointService.GetDefaultName(True, 2)
        Dim commIn As String = AudioEndpointService.GetDefaultName(False, 2)
        If play = "" Then play = "—"
        If rec = "" Then rec = "—"
        If commOut = "" Then commOut = "—"
        If commIn = "" Then commIn = "—"
        lblDefaults.Text =
            "PLAYBACK  " & play.ToUpperInvariant() & ControlChars.CrLf &
            "RECORDING  " & rec.ToUpperInvariant() & "  ·  COMMS OUT " & commOut.ToUpperInvariant() & " / IN " & commIn.ToUpperInvariant()
    End Sub

    Private Sub ReloadDeviceList()
        Select Case _category
            Case DeviceCategory.Playback
                devices = AudioEndpointService.ListPlaybackDevices()
            Case DeviceCategory.Recording
                devices = AudioEndpointService.ListRecordingDevices()
            Case Else
                devices = AudioEndpointService.ListCommunicationsDevices()
        End Select
        Dim keepId As String = ""
        Dim cur As AudioEndpointInfo = TryCast(lstDevices.SelectedItem, AudioEndpointInfo)
        If cur IsNot Nothing Then keepId = cur.Id

        lstDevices.BeginUpdate()
        lstDevices.Items.Clear()
        Dim selectIndex As Integer = -1
        For i As Integer = 0 To devices.Count - 1
            lstDevices.Items.Add(devices(i))
            If keepId <> "" AndAlso String.Equals(devices(i).Id, keepId, StringComparison.OrdinalIgnoreCase) Then
                selectIndex = i
            ElseIf selectIndex < 0 AndAlso devices(i).IsDefaultDevice Then
                selectIndex = i
            End If
        Next
        lstDevices.EndUpdate()
        If selectIndex >= 0 Then lstDevices.SelectedIndex = selectIndex
        status.Text = devices.Count.ToString() & " DEVICES — " & _category.ToString().ToUpperInvariant()
    End Sub

    Private Function SelectedDevice() As AudioEndpointInfo
        Return TryCast(lstDevices.SelectedItem, AudioEndpointInfo)
    End Function

    Private Sub OnSetDefault(ByVal sender As Object, ByVal e As EventArgs)
        Dim d As AudioEndpointInfo = SelectedDevice()
        If d Is Nothing Then
            status.Text = "SELECT A DEVICE"
            Return
        End If
        Dim result As String
        If _category = DeviceCategory.Communications Then
            result = AudioEndpointService.SetCommunicationsDevice(d.Id)
        Else
            result = AudioEndpointService.SetDefaultDevice(d.Id)
        End If
        status.Text = result.ToUpperInvariant()
        AudioVolumeService.InvalidateCache()
        SyncFromSystem(True)
    End Sub

    Private Sub OnSetComms(ByVal sender As Object, ByVal e As EventArgs)
        Dim d As AudioEndpointInfo = SelectedDevice()
        If d Is Nothing Then
            status.Text = "SELECT A DEVICE"
            Return
        End If
        Dim result As String
        If _category = DeviceCategory.Communications Then
            result = AudioEndpointService.SetAllRoles(d.Id)
        Else
            result = AudioEndpointService.SetCommunicationsDevice(d.Id)
        End If
        status.Text = result.ToUpperInvariant()
        AudioVolumeService.InvalidateCache()
        SyncFromSystem(True)
    End Sub

    Private Sub OnMuteToggle(ByVal sender As Object, ByVal e As EventArgs)
        Dim muted As Boolean = AudioVolumeService.IsMuted()
        If AudioVolumeService.SetMuted(Not muted) Then
            status.Text = If(Not muted, "MUTED", "UNMUTED")
            SyncVolumeOnly()
        Else
            status.Text = "MUTE FAILED"
        End If
    End Sub

    Private Sub OnVolumeChanged(ByVal sender As Object, ByVal e As EventArgs)
        If _suppress Then Return
        If AudioVolumeService.SetVolumePercent(trkVol.Value) Then
            lblVol.Text = "VOLUME  " & trkVol.Value.ToString() & "%"
            status.Text = "VOLUME SET"
        Else
            status.Text = "VOLUME SET FAILED"
        End If
    End Sub
End Class

End Namespace
