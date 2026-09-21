Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms

Public Class frmPic
    Inherits LCARS.LCARSForm

    'a collection to hold the paths to our pictures

    Dim myFiles As New Collection

    'index keeps track of where we are in the array.  It represents the location of the current image.
    Dim index As Integer
    Dim origpicboxwidth As Integer
    Dim origpicboxheight As Integer
    Const shiftDelta As Integer = 30
    Private currentKind As MediaKind = MediaKind.None
    Private currentPath As String = ""
    Private vlcHost As VlcPlaybackHost
    Private pnlVideo As Panel
    Private pnlMusic As Panel
    Private lblNowPlaying As Label
    Private fbPlayPause As LCARS.Controls.StandardButton
    Private fbSlideSettings As LCARS.Controls.StandardButton
    Private fbRadio As LCARS.Controls.StandardButton
    Private chrome As ChromeController
    Private transport As MediaTransportControls
    Private mediaLoop As Boolean = False
    Private seekDragging As Boolean = False
    Private Shared ReadOnly rndSlide As New Random()

    Private Sub frmPic_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        'sets initial picture box status to empty & prevents icon from displaying in pic box
        picturebox1.InitialImage = Nothing
        Me.Text = "LCARS Media"
        EnsureMediaStages()
        EnsureChromeController()
        ApplySlideshowTimerFromSettings()
        ApplyContentVisibility(MediaKind.None)
        ApplyRightRailLayout()

        Dim file As String() = Environment.GetCommandLineArgs()
        If file.Length > 1 AndAlso Not String.IsNullOrEmpty(file(1)) Then
            LoadMedia(file(1))
        End If
    End Sub

    Private Sub EnsureMediaStages()
        If pnlVideo IsNot Nothing Then Return
        pnlVideo = New Panel()
        pnlVideo.Name = "pnlVideo"
        pnlVideo.BackColor = Color.Black
        pnlVideo.Visible = False
        pnlVideo.Dock = DockStyle.Fill
        Panel3.Controls.Add(pnlVideo)

        pnlMusic = New Panel()
        pnlMusic.Name = "pnlMusic"
        pnlMusic.BackColor = Color.Black
        pnlMusic.Visible = False
        pnlMusic.Dock = DockStyle.Fill
        lblNowPlaying = New Label()
        lblNowPlaying.ForeColor = Color.Orange
        lblNowPlaying.Font = New Font("LCARS", 18.0F, FontStyle.Regular)
        lblNowPlaying.Dock = DockStyle.Fill
        lblNowPlaying.TextAlign = ContentAlignment.MiddleCenter
        lblNowPlaying.Text = "NO MEDIA"
        pnlMusic.Controls.Add(lblNowPlaying)
        Panel3.Controls.Add(pnlMusic)

        transport = New MediaTransportControls(Me)
        fbPlayPause = transport.PlayPause
        WireTransportHandlers()

        fbSlideSettings = New LCARS.Controls.StandardButton()
        fbSlideSettings.ButtonText = "SLIDE SET"
        fbSlideSettings.Text = "SLIDE SET"
        fbSlideSettings.Color = LCARS.LCARScolorStyles.SystemFunction
        fbSlideSettings.Size = New Size(130, 28)
        fbSlideSettings.Visible = False
        AddHandler fbSlideSettings.Click, AddressOf SlideSettings_Click
        Controls.Add(fbSlideSettings)

        fbRadio = New LCARS.Controls.StandardButton()
        fbRadio.ButtonText = "RADIO"
        fbRadio.Text = "RADIO"
        fbRadio.Color = LCARS.LCARScolorStyles.PrimaryFunction
        fbRadio.Size = New Size(130, 28)
        fbRadio.Visible = True
        AddHandler fbRadio.Click, AddressOf Radio_Click
        Controls.Add(fbRadio)

        If StandardButton1 IsNot Nothing Then
            StandardButton1.ButtonStyle = LCARS.Controls.StandardButton.LCARSbuttonStyles.Pill
            StandardButton1.Clickable = False
        End If
    End Sub

    Private Sub Radio_Click(ByVal sender As Object, ByVal e As EventArgs)
        Using dlg As New frmRadioPicker()
            If dlg.ShowDialog(Me) <> DialogResult.OK Then Return
            If String.IsNullOrEmpty(dlg.SelectedUrl) Then Return
            StartRadio(dlg.SelectedTitle, dlg.SelectedUrl)
        End Using
    End Sub

    Private Sub StartRadio(ByVal title As String, ByVal url As String)
        Try
            EnsureVlcHost()
            StopCurrentPlayback()
            currentKind = MediaKind.Radio
            currentPath = url
            ApplyContentVisibility(MediaKind.Radio)
            vlcHost.AttachVideoSurface(IntPtr.Zero)
            If lblNowPlaying IsNot Nothing Then lblNowPlaying.Text = title.ToUpperInvariant()
            vlcHost.PlayUrl(url)
            RefreshTransportLabels()
            MediaSessionIpc.BroadcastState(MediaKind.Radio, title, True, 0)
            EnsureChromeController()
            chrome.TransitionTo(MediaKind.Radio)
            ApplyRightRailLayout()
        Catch ex As Exception
            MsgBox("Radio failed:" & vbCrLf & ex.Message, MsgBoxStyle.OkOnly Or MsgBoxStyle.Exclamation, "LCARS RADIO")
        End Try
    End Sub

    ''' <summary>Bring the media window forward when a second launch hands off a file.</summary>
    Public Sub ActivateFromShell()
        If WindowState = FormWindowState.Minimized Then WindowState = FormWindowState.Normal
        Show()
        Activate()
        BringToFront()
        TopMost = True
        TopMost = False
        Focus()
    End Sub

    Protected Overrides Sub WndProc(ByRef m As Message)
        Const WM_COPYDATA As Integer = &H4A
        If m.Msg = WM_COPYDATA Then
            Try
                Dim cds As COPYDATASTRUCT = CType(Runtime.InteropServices.Marshal.PtrToStructure(m.LParam, GetType(COPYDATASTRUCT)), COPYDATASTRUCT)
                If cds.dwData.ToInt32() = MediaSessionIpc.MediaMagic AndAlso cds.lpData <> IntPtr.Zero AndAlso cds.cbData > 0 Then
                    Dim bytes(cds.cbData - 1) As Byte
                    Runtime.InteropServices.Marshal.Copy(cds.lpData, bytes, 0, cds.cbData)
                    Dim payload As String = System.Text.Encoding.Unicode.GetString(bytes).TrimEnd(ChrW(0))
                    If payload.StartsWith("CMD|", StringComparison.OrdinalIgnoreCase) Then
                        Dim id As Integer = 0
                        Integer.TryParse(payload.Substring(4), id)
                        HandleShellMediaCommand(CType(id, MediaSessionIpc.MediaCommand))
                        m.Result = New IntPtr(1)
                        Return
                    End If
                End If
            Catch
            End Try
        End If
        MyBase.WndProc(m)
    End Sub

    <Runtime.InteropServices.StructLayout(Runtime.InteropServices.LayoutKind.Sequential)>
    Private Structure COPYDATASTRUCT
        Public dwData As IntPtr
        Public cbData As Integer
        Public lpData As IntPtr
    End Structure

    Private Sub HandleShellMediaCommand(ByVal cmd As MediaSessionIpc.MediaCommand)
        Select Case cmd
            Case MediaSessionIpc.MediaCommand.PlayPause
                PlayPause_Click(Nothing, EventArgs.Empty)
            Case MediaSessionIpc.MediaCommand.StopPlayback
                TransportStop_Click(Nothing, EventArgs.Empty)
            Case MediaSessionIpc.MediaCommand.ShowWindow
                If Me.WindowState = FormWindowState.Minimized Then Me.WindowState = FormWindowState.Normal
                Me.Show()
                Me.Activate()
                Me.BringToFront()
        End Select
    End Sub

    Private Sub WireTransportHandlers()
        AddHandler transport.PlayPause.Click, AddressOf PlayPause_Click
        AddHandler transport.StopBtn.Click, AddressOf TransportStop_Click
        AddHandler transport.Rewind.Click, Sub() If vlcHost IsNot Nothing Then vlcHost.SeekRelativeMs(-10000)
        AddHandler transport.Forward.Click, Sub() If vlcHost IsNot Nothing Then vlcHost.SeekRelativeMs(10000)
        AddHandler transport.Mute.Click, AddressOf TransportMute_Click
        AddHandler transport.VolDown.Click, Sub() If vlcHost IsNot Nothing Then vlcHost.AdjustVolume(-5)
        AddHandler transport.VolUp.Click, Sub() If vlcHost IsNot Nothing Then vlcHost.AdjustVolume(5)
        AddHandler transport.LoopBtn.Click, AddressOf TransportLoop_Click
        AddHandler transport.Speed.Click, AddressOf TransportSpeed_Click
        AddHandler transport.AudioTrack.Click, AddressOf TransportAudio_Click
        AddHandler transport.Subtitles.Click, AddressOf TransportSubs_Click
        AddHandler transport.Fullscreen.Click, AddressOf TransportFullscreen_Click
        AddHandler transport.SeekBar.MouseDown, Sub() seekDragging = True
        AddHandler transport.SeekBar.MouseUp, AddressOf SeekBar_MouseUp
        AddHandler transport.SeekBar.Scroll, AddressOf SeekBar_Scroll
    End Sub

    Private Sub EnsureChromeController()
        If chrome IsNot Nothing Then Return
        ' Keep zoom/NAV out of slide animation — they live in the touch-scroll rail.
        Dim avButtons As Control() = New Control() {}
        Dim photoSet As New List(Of Control)()
        photoSet.Add(sbShow)
        photoSet.Add(fbSlideSettings)
        chrome = New ChromeController(
            Me,
            photoSet.ToArray(),
            avButtons,
            avButtons,
            New Control() {Elbow1, Elbow2, Elbow3, Elbow4},
            AddressOf ApplyRightRailLayout,
            AddressOf ApplyContentVisibility)
    End Sub

    Public Sub LoadMedia(ByVal path As String)
        If String.IsNullOrEmpty(path) Then Return
        path = path.Trim().Trim(""""c)
        Dim kind As MediaKind = MediaKindUtil.DetectMediaKind(path)
        If kind = MediaKind.None Then
            If Directory.Exists(path) Then
                LoadPhotoFolder(path, Nothing)
                Return
            End If
            MsgBox("Unsupported media type:" & vbCrLf & path, MsgBoxStyle.OkOnly Or MsgBoxStyle.Exclamation, "LCARS MEDIA")
            Return
        End If

        StopCurrentPlayback()
        currentKind = kind
        currentPath = path

        Select Case kind
            Case MediaKind.Photo
                LoadPhotoFolder(System.IO.Path.GetDirectoryName(path), path)
            Case MediaKind.Music, MediaKind.Video, MediaKind.Radio
                Try
                    EnsureVlcHost()
                    If kind = MediaKind.Video Then
                        ApplyContentVisibility(MediaKind.Video)
                        pnlVideo.BringToFront()
                        If Not pnlVideo.IsHandleCreated Then pnlVideo.CreateControl()
                        vlcHost.AttachVideoSurface(pnlVideo.Handle)
                        If Not File.Exists(path) Then
                            MsgBox("Media file not found (path missing or inaccessible):" & vbCrLf & path, MsgBoxStyle.OkOnly Or MsgBoxStyle.Exclamation, "LCARS MEDIA")
                            Return
                        End If
                        vlcHost.PlayFile(path)
                        MediaSessionIpc.BroadcastState(kind, System.IO.Path.GetFileName(path), True, 0)
                    ElseIf kind = MediaKind.Radio Then
                        ApplyContentVisibility(MediaKind.Radio)
                        vlcHost.AttachVideoSurface(IntPtr.Zero)
                        lblNowPlaying.Text = path
                        vlcHost.PlayUrl(path)
                        MediaSessionIpc.BroadcastState(kind, path, True, 0)
                    Else
                        If Not File.Exists(path) Then
                            MsgBox("Media file not found (path missing or inaccessible):" & vbCrLf & path, MsgBoxStyle.OkOnly Or MsgBoxStyle.Exclamation, "LCARS MEDIA")
                            Return
                        End If
                        vlcHost.AttachVideoSurface(IntPtr.Zero)
                        lblNowPlaying.Text = System.IO.Path.GetFileName(path)
                        vlcHost.PlayFile(path)
                        MediaSessionIpc.BroadcastState(kind, System.IO.Path.GetFileName(path), True, 0)
                    End If
                    RefreshTransportLabels()
                Catch ex As DirectoryNotFoundException
                    MsgBox(ex.Message, MsgBoxStyle.OkOnly Or MsgBoxStyle.Exclamation, "LCARS MEDIA")
                Catch ex As FileNotFoundException
                    MsgBox(ex.Message & If(String.IsNullOrEmpty(ex.FileName), "", vbCrLf & ex.FileName), MsgBoxStyle.OkOnly Or MsgBoxStyle.Exclamation, "LCARS MEDIA")
                Catch ex As Exception
                    MsgBox("Playback failed:" & vbCrLf & ex.Message & vbCrLf & path, MsgBoxStyle.OkOnly Or MsgBoxStyle.Exclamation, "LCARS MEDIA")
                End Try
        End Select
        EnsureChromeController()
        chrome.TransitionTo(kind)
    End Sub

    Private Sub EnsureVlcHost()
        If vlcHost IsNot Nothing Then Return
        vlcHost = New VlcPlaybackHost()
        AddHandler vlcHost.PlaybackEnded, AddressOf Vlc_PlaybackEnded
        AddHandler vlcHost.TimeChanged, AddressOf Vlc_TimeChanged
        AddHandler vlcHost.PlaybackFailed, AddressOf Vlc_PlaybackFailed
    End Sub

    Private Sub Vlc_PlaybackFailed(ByVal sender As Object, ByVal e As EventArgs)
        If Not Me.IsHandleCreated Then Return
        Dim host As VlcPlaybackHost = TryCast(sender, VlcPlaybackHost)
        _pendingVlcError = If(host IsNot Nothing, host.LastError, "")
        Me.BeginInvoke(New MethodInvoker(AddressOf ShowVlcPlaybackFailed))
    End Sub

    Private _pendingVlcError As String = ""

    Private Sub ShowVlcPlaybackFailed()
        MsgBox("LibVLC reported a playback error." & vbCrLf & _pendingVlcError & vbCrLf & currentPath,
               MsgBoxStyle.OkOnly Or MsgBoxStyle.Exclamation, "LCARS MEDIA")
    End Sub

    ''' <summary>Load all images in a folder; optionally select a starting file.</summary>
    Private Sub LoadPhotoFolder(ByVal folder As String, ByVal preferredFile As String)
        myFiles.Clear()
        If String.IsNullOrEmpty(folder) OrElse Not Directory.Exists(folder) Then
            If Not String.IsNullOrEmpty(preferredFile) AndAlso File.Exists(preferredFile) Then
                myFiles.Add(preferredFile)
            End If
        Else
            Dim exts As String() = {".jpg", ".jpeg", ".gif", ".bmp", ".png", ".tif", ".tiff", ".webp"}
            For Each f As String In Directory.GetFiles(folder)
                Dim ext As String = System.IO.Path.GetExtension(f).ToLowerInvariant()
                If Array.IndexOf(exts, ext) >= 0 Then myFiles.Add(f)
            Next
        End If
        If myFiles.Count = 0 Then Return
        currentKind = MediaKind.Photo
        index = 1
        If Not String.IsNullOrEmpty(preferredFile) Then
            For i As Integer = 1 To myFiles.Count
                If String.Equals(CStr(myFiles(i)), preferredFile, StringComparison.OrdinalIgnoreCase) Then
                    index = i
                    Exit For
                End If
            Next
        End If
        currentPath = CStr(myFiles(index))
        loadImages(index)
        origpicboxwidth = picturebox1.Width
        origpicboxheight = picturebox1.Height
        EnsureChromeController()
        chrome.TransitionTo(MediaKind.Photo)
    End Sub

    Private Sub ApplyContentVisibility(ByVal kind As MediaKind)
        Dim isPhoto As Boolean = (kind = MediaKind.Photo)
        Dim isAv As Boolean = (kind = MediaKind.Music OrElse kind = MediaKind.Video OrElse kind = MediaKind.Radio)
        picturebox1.Visible = (kind = MediaKind.Photo OrElse kind = MediaKind.None)
        If pnlVideo IsNot Nothing Then pnlVideo.Visible = (kind = MediaKind.Video)
        If pnlMusic IsNot Nothing Then pnlMusic.Visible = (kind = MediaKind.Music OrElse kind = MediaKind.Radio)
        If transport IsNot Nothing Then
            If kind = MediaKind.Radio Then
                ' Radio: compact A/V only (no seek chrome / video extras)
                transport.SetVisible(False, False)
                transport.PlayPause.Visible = True
                transport.StopBtn.Visible = True
                transport.Mute.Visible = True
                transport.VolDown.Visible = True
                transport.VolUp.Visible = True
                transport.SeekBar.Visible = False
                transport.TimeLabel.Visible = False
            Else
                transport.SetVisible(isAv, kind = MediaKind.Video)
            End If
        End If
        sbShow.Visible = isPhoto
        If fbSlideSettings IsNot Nothing Then fbSlideSettings.Visible = isPhoto
        fbZoomIn.Visible = isPhoto
        fbZoomOut.Visible = isPhoto
        fbActual.Visible = isPhoto
        pbZoom.Visible = isPhoto
        panel1.Visible = True
        Panel2.Visible = True
        If StandardButton1 IsNot Nothing Then StandardButton1.Visible = True
        If lblInfo IsNot Nothing Then lblInfo.Visible = isPhoto
        If fbRadio IsNot Nothing Then fbRadio.Visible = True
    End Sub

    Private Sub ApplySlideshowTimerFromSettings()
        tmrShow.Interval = SlideshowSettingsStore.IntervalSeconds * 1000
    End Sub

    Private Sub StopCurrentPlayback()
        Try
            If vlcHost IsNot Nothing Then vlcHost.StopPlayback()
        Catch
        End Try
        tmrShow.Enabled = False
        sbShow.ButtonText = "START SLIDE SHOW"
        sbShow.Text = "START SLIDE SHOW"
    End Sub

    Private Sub PlayPause_Click(ByVal sender As Object, ByVal e As EventArgs)
        If vlcHost Is Nothing Then Return
        vlcHost.TogglePause()
        RefreshTransportLabels()
        MediaSessionIpc.BroadcastState(currentKind, System.IO.Path.GetFileName(currentPath), vlcHost.IsPlaying, CInt(Math.Min(Integer.MaxValue, vlcHost.PositionMs())))
    End Sub

    Private Sub TransportStop_Click(ByVal sender As Object, ByVal e As EventArgs)
        If vlcHost Is Nothing Then Return
        vlcHost.StopPlayback()
        RefreshTransportLabels()
        MediaSessionIpc.BroadcastState(currentKind, System.IO.Path.GetFileName(currentPath), False, 0)
    End Sub

    Private Sub TransportMute_Click(ByVal sender As Object, ByVal e As EventArgs)
        If vlcHost Is Nothing Then Return
        vlcHost.ToggleMute()
        transport.Mute.ButtonText = If(vlcHost.Mute, "UNMUTE", "MUTE")
        transport.Mute.Text = transport.Mute.ButtonText
    End Sub

    Private Sub TransportLoop_Click(ByVal sender As Object, ByVal e As EventArgs)
        mediaLoop = Not mediaLoop
        transport.LoopBtn.ButtonText = If(mediaLoop, "LOOP ON", "LOOP OFF")
        transport.LoopBtn.Text = transport.LoopBtn.ButtonText
    End Sub

    Private Sub TransportSpeed_Click(ByVal sender As Object, ByVal e As EventArgs)
        If vlcHost Is Nothing Then Return
        Dim rate As Single = vlcHost.CycleRate()
        transport.Speed.ButtonText = "SPEED " & rate.ToString("0.##") & "x"
        transport.Speed.Text = transport.Speed.ButtonText
    End Sub

    Private Sub TransportAudio_Click(ByVal sender As Object, ByVal e As EventArgs)
        If vlcHost Is Nothing Then Return
        Dim name As String = vlcHost.CycleAudioTrack()
        transport.AudioTrack.ButtonText = name
        transport.AudioTrack.Text = name
    End Sub

    Private Sub TransportSubs_Click(ByVal sender As Object, ByVal e As EventArgs)
        If vlcHost Is Nothing Then Return
        Dim name As String = vlcHost.CycleSubtitle()
        transport.Subtitles.ButtonText = name
        transport.Subtitles.Text = name
    End Sub

    Private Sub TransportFullscreen_Click(ByVal sender As Object, ByVal e As EventArgs)
        If Me.FormBorderStyle = FormBorderStyle.None AndAlso Me.WindowState = FormWindowState.Maximized Then
            Me.FormBorderStyle = FormBorderStyle.Sizable
            Me.WindowState = FormWindowState.Normal
            transport.Fullscreen.ButtonText = "FULLSCREEN"
        Else
            Me.FormBorderStyle = FormBorderStyle.None
            Me.WindowState = FormWindowState.Maximized
            transport.Fullscreen.ButtonText = "WINDOWED"
        End If
        transport.Fullscreen.Text = transport.Fullscreen.ButtonText
    End Sub

    Private Sub SeekBar_MouseUp(ByVal sender As Object, ByVal e As MouseEventArgs)
        seekDragging = False
        SeekBar_Scroll(sender, e)
    End Sub

    Private Sub SeekBar_Scroll(ByVal sender As Object, ByVal e As EventArgs)
        If vlcHost Is Nothing OrElse transport Is Nothing Then Return
        Dim len As Long = vlcHost.LengthMs()
        If len <= 0 Then Return
        Dim ms As Long = CLng(Math.Round(len * (transport.SeekBar.Value / CDbl(transport.SeekBar.Maximum))))
        vlcHost.SeekToMs(ms)
        RefreshTransportLabels()
    End Sub

    Private Sub Vlc_PlaybackEnded(ByVal sender As Object, ByVal e As EventArgs)
        If Me.IsDisposed Then Return
        Me.BeginInvoke(New MethodInvoker(Sub()
                                             If mediaLoop AndAlso Not String.IsNullOrEmpty(currentPath) Then
                                                 Try
                                                     vlcHost.PlayFile(currentPath)
                                                 Catch
                                                 End Try
                                             Else
                                                 RefreshTransportLabels()
                                             End If
                                         End Sub))
    End Sub

    Private Sub Vlc_TimeChanged(ByVal sender As Object, ByVal e As EventArgs)
        If Me.IsDisposed OrElse seekDragging Then Return
        Try
            If Me.InvokeRequired Then
                Me.BeginInvoke(New MethodInvoker(AddressOf RefreshTransportLabels))
            Else
                RefreshTransportLabels()
            End If
        Catch
        End Try
    End Sub

    Private Sub RefreshTransportLabels()
        If transport Is Nothing OrElse vlcHost Is Nothing Then Return
        Dim pos As Long = vlcHost.PositionMs()
        Dim len As Long = vlcHost.LengthMs()
        transport.TimeLabel.Text = FormatMs(pos) & " / " & FormatMs(len)
        If Not seekDragging AndAlso len > 0 Then
            Dim v As Integer = CInt(Math.Max(0, Math.Min(transport.SeekBar.Maximum, Math.Round(transport.SeekBar.Maximum * (pos / CDbl(len))))))
            transport.SeekBar.Value = v
        End If
        transport.PlayPause.ButtonText = If(vlcHost.IsPlaying, "PAUSE", "PLAY")
        transport.PlayPause.Text = transport.PlayPause.ButtonText
        transport.Mute.ButtonText = If(vlcHost.Mute, "UNMUTE", "MUTE")
        transport.Mute.Text = transport.Mute.ButtonText
    End Sub

    Private Shared Function FormatMs(ByVal ms As Long) As String
        If ms < 0 Then ms = 0
        Dim totalSec As Integer = CInt(ms \ 1000L)
        Dim m As Integer = totalSec \ 60
        Dim s As Integer = totalSec Mod 60
        Dim h As Integer = m \ 60
        m = m Mod 60
        If h > 0 Then Return h.ToString("00") & ":" & m.ToString("00") & ":" & s.ToString("00")
        Return m.ToString("00") & ":" & s.ToString("00")
    End Function

    Protected Overrides Sub OnShellChromeLayout()
        ' Media app uses its own right-rail stack (CLOSE under NAV), not shell Start-Menu CLOSE alignment.
        ApplyRightRailLayout()
    End Sub

    ''' <summary>
    ''' Right rail: CLOSE/RADIO/BROWSE fixed at bottom; NAV disc fixed above them;
    ''' zoom/transport scroll in the viewport above NAV.
    ''' </summary>
    Private Sub ApplyRightRailLayout()
        If sbBrowse Is Nothing OrElse sbExit Is Nothing Then Return
        If ClientSize.Width < 100 OrElse ClientSize.Height < 100 Then Return
        EnsureRailScroll()
        EnsureAlbumButtons()

        Const margin As Integer = 8
        Const gap As Integer = 6
        Const frameBarW As Integer = 50
        Dim railW As Integer = Math.Max(100, sbBrowse.Width)
        Dim right As Integer = ClientSize.Width - margin
        Dim railLeft As Integer = right - railW
        Dim titleBottom As Integer = If(tbTitle IsNot Nothing, tbTitle.Bottom + 4, 48)

        ' --- Fixed: CLOSE at bottom ---
        sbExit.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        sbExit.Size = New Size(railW, 28)
        sbExit.Location = New Point(railLeft, ClientSize.Height - margin - sbExit.Height)
        sbExit.ButtonText = "CLOSE"
        sbExit.Text = "CLOSE"

        Dim yFixed As Integer = sbExit.Top - gap

        ' --- Fixed: BROWSE / slideshow / slide set / RADIO ---
        sbShow.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        sbBrowse.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        sbShow.Size = New Size(railW, Math.Max(28, sbShow.Height))
        sbBrowse.Size = New Size(railW, Math.Max(28, sbBrowse.Height))
        If fbSlideSettings IsNot Nothing AndAlso fbSlideSettings.Visible Then
            fbSlideSettings.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
            fbSlideSettings.Size = New Size(railW, 28)
            fbSlideSettings.Location = New Point(railLeft, yFixed - fbSlideSettings.Height)
            yFixed = fbSlideSettings.Top - gap
        End If
        If sbShow.Visible Then
            sbShow.Location = New Point(railLeft, yFixed - sbShow.Height)
            yFixed = sbShow.Top - gap
        End If
        sbBrowse.Location = New Point(railLeft, yFixed - sbBrowse.Height)
        yFixed = sbBrowse.Top - gap

        If fbRadio IsNot Nothing Then
            fbRadio.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
            fbRadio.Size = New Size(railW, 28)
            fbRadio.Location = New Point(railLeft, yFixed - fbRadio.Height)
            fbRadio.Visible = True
            fbRadio.BringToFront()
            yFixed = fbRadio.Top - gap
        End If

        ' --- Fixed NAV disc on the right rail (form coords — never x=0 on the form) ---
        Dim edge As Integer = Math.Max(28, CInt(Math.Round(railW * 0.3)))
        Dim center As Integer = Math.Max(32, railW - edge * 2)
        Dim navSize As Integer = railW
        EnsureNavOnForm()
        If fbNavCaption IsNot Nothing Then fbNavCaption.Visible = False

        Dim navTop As Integer = yFixed - navSize
        If StandardButton1 IsNot Nothing Then
            StandardButton1.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
            StandardButton1.ButtonStyle = LCARS.Controls.StandardButton.LCARSbuttonStyles.Pill
            StandardButton1.Size = New Size(navSize, navSize)
            StandardButton1.Location = New Point(railLeft, navTop)
            StandardButton1.Visible = True
            StandardButton1.SendToBack()
        End If
        panel1.Visible = True
        Panel2.Visible = True
        panel1.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        Panel2.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        panel1.Size = New Size(edge, navSize)
        panel1.Location = New Point(railLeft + (navSize - edge) \ 2, navTop)
        LayoutNavVerticalArm(panel1, edge, center)
        Panel2.Size = New Size(navSize, edge)
        Panel2.Location = New Point(railLeft, navTop + (navSize - edge) \ 2)
        LayoutNavHorizontalArm(Panel2, edge, center)
        panel1.BringToFront()
        Panel2.BringToFront()
        yFixed = navTop - gap

        ' --- Radio transport stays on the right form rail (never x=0 on the form) ---
        Dim isRadio As Boolean = (currentKind = MediaKind.Radio)
        If isRadio AndAlso transport IsNot Nothing Then
            EnsureTransportOnForm()
            Dim radioBtns As LCARS.Controls.StandardButton() = {
                transport.PlayPause, transport.StopBtn, transport.Mute, transport.VolDown, transport.VolUp
            }
            For Each b As LCARS.Controls.StandardButton In radioBtns
                If b Is Nothing OrElse Not b.Visible Then Continue For
                b.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
                b.Size = New Size(railW, 28)
                b.Location = New Point(railLeft, yFixed - b.Height)
                b.BringToFront()
                yFixed = b.Top - gap
            Next
        End If

        ' --- Scroll viewport above fixed NAV (zoom / album / non-radio transport) ---
        Dim showPn As Boolean = (currentKind = MediaKind.Photo)
        Dim showZoom As Boolean = fbZoomOut.Visible OrElse pbZoom.Visible
        Dim showTransport As Boolean = (Not isRadio) AndAlso (transport IsNot Nothing AndAlso transport.PlayPause.Visible)

        Dim viewportTop As Integer = titleBottom + 4
        Dim viewportBottom As Integer = yFixed
        Dim viewportH As Integer = Math.Max(40, viewportBottom - viewportTop)
        railScroll.PlaceViewport(railLeft, viewportTop, railW, viewportH)
        EnsureRailChildren()

        Dim contentH As Integer = gap
        If showPn Then contentH += 28 + gap
        If showZoom Then
            Dim zoomH As Integer = Math.Max(28, CInt(Math.Round(railW * 0.28)))
            Dim pieH As Integer = Math.Max(36, CInt(Math.Round(railW * 0.42)))
            contentH += zoomH + 2 + pieH + gap
        End If
        If showTransport Then
            Dim btnCount As Integer = 0
            For Each b As LCARS.Controls.StandardButton In transport.Buttons
                If b.Visible Then btnCount += 1
            Next
            contentH += btnCount * (28 + gap)
        End If
        contentH = Math.Max(contentH, viewportH)
        railScroll.SetContentHeight(contentH)

        Dim y As Integer = contentH - gap

        fbAlbumPrev.Visible = showPn
        fbAlbumNext.Visible = showPn
        If showPn Then
            fbAlbumNext.Size = New Size((railW - 4) \ 2, 28)
            fbAlbumPrev.Size = New Size(railW - fbAlbumNext.Width - 4, 28)
            fbAlbumNext.Location = New Point(fbAlbumPrev.Width + 4, y - fbAlbumNext.Height)
            fbAlbumPrev.Location = New Point(0, fbAlbumNext.Top)
            fbAlbumPrev.BringToFront()
            fbAlbumNext.BringToFront()
            y = fbAlbumPrev.Top - gap
        End If

        If showZoom Then
            Dim zoomH As Integer = Math.Max(28, CInt(Math.Round(railW * 0.28)))
            Dim zoomBtnW As Integer = (railW - 4) \ 3
            fbZoomOut.Size = New Size(zoomBtnW, zoomH)
            fbActual.Size = New Size(zoomBtnW, zoomH)
            fbZoomIn.Size = New Size(railW - zoomBtnW * 2, zoomH)
            fbZoomOut.Location = New Point(0, y - zoomH)
            fbActual.Location = New Point(fbZoomOut.Width + 2, fbZoomOut.Top)
            fbZoomIn.Location = New Point(fbActual.Left + fbActual.Width + 2, fbZoomOut.Top)

            Dim pieH As Integer = Math.Max(36, CInt(Math.Round(railW * 0.42)))
            pbZoom.Size = New Size(railW, pieH)
            pbZoom.Location = New Point(0, fbZoomOut.Top - 2 - pieH)
            pbZoom.CircleRadius = railW \ 2
            pbZoom.CircleLocation = New Point(railW \ 2, pieH + railW \ 4)
            fbZoomOut.BringToFront()
            fbActual.BringToFront()
            fbZoomIn.BringToFront()
            pbZoom.BringToFront()
            y = pbZoom.Top - gap
        End If

        If showTransport Then
            y = transport.LayoutAbove(0, railW, y, gap)
        End If

        railScroll.ResetScrollToBottom()

        ' --- Right LCARS frame ---
        Dim elbowW As Integer = 72
        Dim frameLeft As Integer = railLeft - gap - frameBarW
        Dim elbowLeft As Integer = frameLeft - (elbowW - frameBarW)
        Dim bottomChrome As Integer = ClientSize.Height - margin

        If Elbow4 IsNot Nothing Then
            Elbow4.Anchor = AnchorStyles.Top Or AnchorStyles.Right
            Elbow4.Size = New Size(elbowW, 68)
            Elbow4.Location = New Point(elbowLeft, titleBottom)
            Elbow4.ButtonWidth = frameBarW
        End If
        If Elbow3 IsNot Nothing Then
            Elbow3.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
            Elbow3.Size = New Size(elbowW, 68)
            Elbow3.Location = New Point(elbowLeft, bottomChrome - Elbow3.Height)
            Elbow3.ButtonWidth = frameBarW
        End If
        If fbInfo1 IsNot Nothing Then
            fbInfo1.Anchor = AnchorStyles.Top Or AnchorStyles.Right
            fbInfo1.Size = New Size(frameBarW, 57)
            fbInfo1.Location = New Point(frameLeft, Elbow4.Bottom + 4)
        End If
        If FlatButton6 IsNot Nothing Then
            FlatButton6.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Right
            Dim barTop As Integer = fbInfo1.Bottom + 4
            Dim barBottom As Integer = Elbow3.Top - 4
            FlatButton6.Location = New Point(frameLeft, barTop)
            FlatButton6.Size = New Size(frameBarW, Math.Max(40, barBottom - barTop))
        End If

        Dim stageLeft As Integer = 90
        If FlatButton4 IsNot Nothing Then stageLeft = FlatButton4.Right + 8
        Dim stageRight As Integer = elbowLeft - gap
        Dim stageTop As Integer = titleBottom + 8
        Dim stageBottom As Integer = bottomChrome - 8
        If Panel3 IsNot Nothing AndAlso stageRight > stageLeft + 100 Then
            Panel3.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left
            Panel3.Location = New Point(stageLeft, stageTop)
            Panel3.Size = New Size(stageRight - stageLeft, Math.Max(100, stageBottom - stageTop))
        End If

        If lblInfo IsNot Nothing AndAlso lblInfo.Visible Then
            lblInfo.Visible = False
        End If

        If transport IsNot Nothing AndAlso transport.SeekBar.Visible AndAlso Panel3 IsNot Nothing Then
            transport.LayoutSeek(Panel3.Left, Panel3.Width, ClientSize.Height - margin)
        End If

        sbBrowse.BringToFront()
        sbShow.BringToFront()
        If fbRadio IsNot Nothing Then fbRadio.BringToFront()
        If fbSlideSettings IsNot Nothing AndAlso fbSlideSettings.Visible Then fbSlideSettings.BringToFront()
        panel1.BringToFront()
        Panel2.BringToFront()
        railScroll.ViewportControl.BringToFront()
        sbExit.BringToFront()
    End Sub

    ''' <summary>NAV disc stays on the form at right-rail coords; pull it back if it was reparented into the scroll panel.</summary>
    Private Sub EnsureNavOnForm()
        If StandardButton1 IsNot Nothing AndAlso StandardButton1.Parent IsNot Me Then
            StandardButton1.Parent = Me
        End If
        If panel1 IsNot Nothing AndAlso panel1.Parent IsNot Me Then
            panel1.Parent = Me
        End If
        If Panel2 IsNot Nothing AndAlso Panel2.Parent IsNot Me Then
            Panel2.Parent = Me
        End If
    End Sub

    Private Sub EnsureTransportOnForm()
        If transport Is Nothing Then Return
        For Each b As LCARS.Controls.StandardButton In transport.Buttons
            If b IsNot Nothing AndAlso b.Parent IsNot Me Then b.Parent = Me
        Next
    End Sub

    Private railScroll As RailScrollPanel
    Private fbNavCaption As LCARS.Controls.FlatButton
    Private fbAlbumPrev As LCARS.Controls.StandardButton
    Private fbAlbumNext As LCARS.Controls.StandardButton

    Private Sub EnsureRailScroll()
        If railScroll IsNot Nothing Then Return
        railScroll = New RailScrollPanel(Me)
    End Sub

    Private Sub EnsureRailChildren()
        EnsureRailScroll()
        ' Zoom + non-radio transport scroll inside the right viewport.
        railScroll.Adopt(fbZoomOut)
        railScroll.Adopt(fbActual)
        railScroll.Adopt(fbZoomIn)
        railScroll.Adopt(pbZoom)
        If transport Is Nothing Then Return
        If currentKind = MediaKind.Radio Then Return
        For Each b As LCARS.Controls.StandardButton In transport.Buttons
            railScroll.Adopt(b)
        Next
    End Sub

    Private Sub EnsureAlbumButtons()
        If fbAlbumPrev IsNot Nothing Then Return
        EnsureRailScroll()
        fbAlbumPrev = New LCARS.Controls.StandardButton()
        fbAlbumPrev.ButtonText = "PREV"
        fbAlbumPrev.Text = "PREV"
        fbAlbumPrev.Color = LCARS.LCARScolorStyles.SystemFunction
        AddHandler fbAlbumPrev.Click, AddressOf AlbumPrev_Click
        railScroll.Adopt(fbAlbumPrev)
        fbAlbumNext = New LCARS.Controls.StandardButton()
        fbAlbumNext.ButtonText = "NEXT"
        fbAlbumNext.Text = "NEXT"
        fbAlbumNext.Color = LCARS.LCARScolorStyles.SystemFunction
        AddHandler fbAlbumNext.Click, AddressOf AlbumNext_Click
        railScroll.Adopt(fbAlbumNext)
    End Sub

    Private Sub LayoutNavHorizontalArm(ByVal host As Panel, ByVal edge As Integer, ByVal center As Integer)
        If host Is Nothing Then Return
        ' Equal left/right ArrowButtons for pan; hide leftover decorative segments that skewed size/color.
        If FlatButton13 IsNot Nothing Then FlatButton13.Visible = False
        If FlatButton1 IsNot Nothing Then FlatButton1.Visible = False
        If lft IsNot Nothing Then lft.Visible = False
        If rht IsNot Nothing Then rht.Visible = False

        Dim top As Integer = Math.Max(0, (host.Height - edge) \ 2)
        abPrev.Visible = True
        abNext.Visible = True
        abPrev.ArrowDirection = LCARS.LCARSarrowDirection.Left
        abNext.ArrowDirection = LCARS.LCARSarrowDirection.Right
        abPrev.Color = LCARS.LCARScolorStyles.SystemFunction
        abNext.Color = LCARS.LCARScolorStyles.SystemFunction
        abPrev.Size = New Size(edge, edge)
        abNext.Size = New Size(edge, edge)
        abPrev.Location = New Point(0, top)
        abNext.Location = New Point(host.Width - edge, top)
        abPrev.BringToFront()
        abNext.BringToFront()
    End Sub

    Private Sub LayoutNavVerticalArm(ByVal host As Panel, ByVal edge As Integer, ByVal center As Integer)
        If host Is Nothing Then Return
        If FlatButton8 IsNot Nothing Then FlatButton8.Visible = False
        If up IsNot Nothing Then up.Visible = False
        If dwn IsNot Nothing Then dwn.Visible = False

        Dim left As Integer = Math.Max(0, (host.Width - edge) \ 2)
        ArrowButton1.Visible = True
        ArrowButton2.Visible = True
        ArrowButton1.ArrowDirection = LCARS.LCARSarrowDirection.Up
        ArrowButton2.ArrowDirection = LCARS.LCARSarrowDirection.Down
        ArrowButton1.Color = LCARS.LCARScolorStyles.SystemFunction
        ArrowButton2.Color = LCARS.LCARScolorStyles.SystemFunction
        ArrowButton1.Size = New Size(edge, edge)
        ArrowButton2.Size = New Size(edge, edge)
        ArrowButton1.Location = New Point(left, 0)
        ArrowButton2.Location = New Point(left, host.Height - edge)

        FlatButton2.Visible = True
        FlatButton2.ButtonText = "NAV"
        FlatButton2.Text = "NAV"
        FlatButton2.ButtonTextAlign = ContentAlignment.MiddleCenter
        FlatButton2.Clickable = False
        FlatButton2.Color = LCARS.LCARScolorStyles.NavigationFunction
        FlatButton2.Size = New Size(center, center)
        FlatButton2.Location = New Point(Math.Max(0, (host.Width - center) \ 2), Math.Max(0, (host.Height - center) \ 2))
        FlatButton2.BringToFront()
        ArrowButton1.BringToFront()
        ArrowButton2.BringToFront()
    End Sub

    Private Sub frmPic_Resize(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Resize
        ApplyRightRailLayout()
    End Sub

    Private Sub loadImages(ByVal curIndex As Integer)
        If myFiles.Count > 0 Then
            Dim lastIndex As Integer
            Dim nextIndex As Integer

            If curIndex > 1 Then
                lastIndex = curIndex - 1
            Else
                lastIndex = myFiles.Count
            End If

            If curIndex + 1 <= myFiles.Count Then
                nextIndex = curIndex + 1
            Else
                nextIndex = 1
            End If

            If Not picturebox1.Image Is Nothing Then
                picturebox1.Image.Dispose()
            End If

            Dim myinfo As New System.IO.FileInfo(CStr(myFiles(curIndex)))

            picturebox1.Image = Image.FromFile(CStr(myFiles(curIndex)))
            lblInfo.Text = myinfo.Name & vbNewLine & vbNewLine & _
                          "RESOLUTION: " & picturebox1.Image.Width & "x" & picturebox1.Image.Height & vbNewLine & _
                         "FILE SIZE: " & myinfo.Length & " bytes" & vbNewLine & _
           "CREATED: " & myinfo.CreationTime.ToShortDateString & vbNewLine & _
            "MODIFIED: " & myinfo.LastWriteTime.ToShortDateString & vbNewLine
            '"NAME: " & myinfo.FullName.ToString & vbNewLine


            pbZoom.ButtonText = "ZOOM: " & Strings.FormatPercent(picturebox1.Width / picturebox1.Image.Width, 0)



            lastIndex = Nothing
            nextIndex = Nothing
        End If

    End Sub



    Private Sub tmrShow_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles tmrShow.Tick
        AdvanceSlideshow()
    End Sub

    Private Sub AdvanceSlideshow()
        If myFiles.Count = 0 Then
            tmrShow.Enabled = False
            Return
        End If
        If SlideshowSettingsStore.ShuffleEnabled AndAlso myFiles.Count > 1 Then
            Dim nextIdx As Integer = index
            Do
                nextIdx = rndSlide.Next(1, myFiles.Count + 1)
            Loop While nextIdx = index AndAlso myFiles.Count > 1
            index = nextIdx
            loadImages(index)
            Return
        End If
        If index + 1 > myFiles.Count Then
            If SlideshowSettingsStore.LoopEnabled Then
                index = 1
                loadImages(index)
            Else
                tmrShow.Enabled = False
                sbShow.ButtonText = "Start Slideshow"
                sbShow.Text = "Start Slideshow"
            End If
        Else
            index += 1
            loadImages(index)
        End If
    End Sub

    Private Sub sbShow_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles sbShow.Click
        ApplySlideshowTimerFromSettings()
        tmrShow.Enabled = Not tmrShow.Enabled
        If tmrShow.Enabled Then
            sbShow.ButtonText = "Stop Slideshow"
            sbShow.Text = "Stop Slideshow"
        Else
            sbShow.ButtonText = "Start Slideshow"
            sbShow.Text = "Start Slideshow"
        End If
    End Sub

    Private Sub SlideSettings_Click(ByVal sender As Object, ByVal e As EventArgs)
        Using dlg As New frmSlideshowSettings()
            If dlg.ShowDialog(Me) = DialogResult.OK Then
                ApplySlideshowTimerFromSettings()
            End If
        End Using
    End Sub

    Private Sub sbBrowse_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles sbBrowse.Click
        Using dlg As New LCARS.LCARSfileBrowseDialog(LCARS.LCARSfileBrowseDialog.LCARSDialogType.Open)
            dlg.Fullscreen = False
            dlg.SetFilterPatterns(
                "*.*",
                "*.JPG;*.JPEG;*.PNG;*.GIF;*.BMP;*.TIF;*.TIFF;*.WEBP",
                "*.MP3;*.WAV;*.FLAC;*.M4A;*.AAC;*.OGG;*.WMA;*.OPUS",
                "*.MP4;*.MKV;*.AVI;*.WMV;*.MOV;*.M4V;*.WEBM;*.MPG;*.MPEG;*.TS")
            Dim startDir As String = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)
            If currentKind = MediaKind.Photo Then
                startDir = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            ElseIf currentKind = MediaKind.Video Then
                startDir = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)
            End If
            If Not String.IsNullOrEmpty(currentPath) Then
                Dim parent As String = System.IO.Path.GetDirectoryName(currentPath)
                If Directory.Exists(parent) Then startDir = parent
            End If
            dlg.InitialDirectory = startDir
            If dlg.ShowDialog(Me) <> DialogResult.OK Then Return
            If String.IsNullOrEmpty(dlg.FileName) OrElse Not File.Exists(dlg.FileName) Then Return
            LoadMedia(dlg.FileName)
        End Using
    End Sub

    Private Sub AlbumNext_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
        If myFiles.Count = 0 Then Return
        If index + 1 > myFiles.Count Then
            index = 1
        Else
            index += 1
        End If
        loadImages(index)
    End Sub

    Private Sub AlbumPrev_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
        If myFiles.Count = 0 Then Return
        picturebox1.SizeMode = PictureBoxSizeMode.Zoom
        If index > 1 Then
            index -= 1
        Else
            index = myFiles.Count
        End If
        loadImages(index)
    End Sub


    Private Sub fbZoomOut_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles fbZoomOut.Click

        If picturebox1.Image IsNot Nothing Then

            picturebox1.Width = CInt(picturebox1.Width * 0.8)
            picturebox1.Height = CInt(picturebox1.Height * 0.8)

            pbZoom.ButtonText = "ZOOM: " & Strings.FormatPercent(picturebox1.Width / picturebox1.Image.Width, 0)

        End If

    End Sub

    Private Sub fbZoomIn_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles fbZoomIn.Click

        If picturebox1.Image IsNot Nothing Then


            picturebox1.Width = CInt(picturebox1.Width * 1.2)
            picturebox1.Height = CInt(picturebox1.Height * 1.2)
            pbZoom.ButtonText = "ZOOM: " & Strings.FormatPercent(picturebox1.Width / picturebox1.Image.Width, 0)

        End If

    End Sub

    Private Sub fbActual_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles fbActual.Click
        If picturebox1.Image IsNot Nothing Then
            picturebox1.SizeMode = PictureBoxSizeMode.Zoom
            picturebox1.Size = New Size(origpicboxwidth, origpicboxheight)

            Dim picboxlocx As Integer = 3
            Dim picboxlocy As Integer = 3
            picturebox1.Location = New Point(picboxlocx, picboxlocy)

            pbZoom.ButtonText = "ZOOM: " & Strings.FormatPercent(picturebox1.Width / picturebox1.Image.Width, 0)
        End If
    End Sub



    Private Sub sbExit_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles sbExit.Click
        StopCurrentPlayback()
        If chrome IsNot Nothing Then
            chrome.DisposeTimer()
            chrome = Nothing
        End If
        If vlcHost IsNot Nothing Then
            vlcHost.Dispose()
            vlcHost = Nothing
        End If
        Me.Close()
    End Sub

    Private shiftControl As Control

    Private Sub shiftButtonDown(ByVal sender As Object, ByVal e As EventArgs) Handles _
        up.MouseDown, dwn.MouseDown, lft.MouseDown, rht.MouseDown, _
        ArrowButton1.MouseDown, ArrowButton2.MouseDown, abPrev.MouseDown, abNext.MouseDown
        If picturebox1.Image Is Nothing Then Return
        shiftControl = DirectCast(sender, Control)
        tmrShift.Start()
        shiftTimer_Tick(sender, e)
    End Sub

    Private Sub shiftButtonUp(ByVal sender As Object, ByVal e As EventArgs) Handles _
        up.MouseUp, dwn.MouseUp, lft.MouseUp, rht.MouseUp, _
        ArrowButton1.MouseUp, ArrowButton2.MouseUp, abPrev.MouseUp, abNext.MouseUp
        tmrShift.Stop()
        shiftControl = Nothing
    End Sub

    Private Sub shiftTimer_Tick(ByVal sender As Object, ByVal e As EventArgs) Handles tmrShift.Tick
        If shiftControl Is Nothing Then Return
        If shiftControl Is up OrElse shiftControl Is ArrowButton1 Then
            picturebox1.Top += shiftDelta
        ElseIf shiftControl Is dwn OrElse shiftControl Is ArrowButton2 Then
            picturebox1.Top -= shiftDelta
        ElseIf shiftControl Is lft OrElse shiftControl Is abPrev Then
            picturebox1.Left += shiftDelta
        ElseIf shiftControl Is rht OrElse shiftControl Is abNext Then
            picturebox1.Left -= shiftDelta
        End If
    End Sub
End Class
