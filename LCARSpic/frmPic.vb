Option Strict On
Option Explicit On

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

    Private Sub frmPic_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        'sets initial picture box status to empty & prevents icon from displaying in pic box
        picturebox1.InitialImage = Nothing
        Me.Text = "LCARS Media"
        EnsureMediaStages()
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

        fbPlayPause = New LCARS.Controls.StandardButton()
        fbPlayPause.ButtonText = "PLAY/PAUSE"
        fbPlayPause.Text = "PLAY/PAUSE"
        fbPlayPause.Color = LCARS.LCARScolorStyles.PrimaryFunction
        fbPlayPause.Size = New Size(130, 35)
        fbPlayPause.Visible = False
        AddHandler fbPlayPause.Click, AddressOf PlayPause_Click
        Controls.Add(fbPlayPause)
    End Sub

    Public Sub LoadMedia(ByVal path As String)
        If String.IsNullOrEmpty(path) Then Return
        Dim kind As MediaKind = MediaKindUtil.DetectMediaKind(path)
        If kind = MediaKind.None Then
            If Directory.Exists(path) Then
                ' Folder browse path handled by sbBrowse for photos; try first media file.
                Return
            End If
            MsgBox("Unsupported media type:" & vbCrLf & path, MsgBoxStyle.OkOnly Or MsgBoxStyle.Exclamation, "LCARS MEDIA")
            Return
        End If

        StopCurrentPlayback()
        currentKind = kind
        currentPath = path
        ApplyContentVisibility(kind)

        Select Case kind
            Case MediaKind.Photo
                myFiles.Clear()
                myFiles.Add(path)
                index = 1
                loadImages(index)
                origpicboxwidth = picturebox1.Width
                origpicboxheight = picturebox1.Height
            Case MediaKind.Music, MediaKind.Video
                Try
                    If vlcHost Is Nothing Then vlcHost = New VlcPlaybackHost()
                    If kind = MediaKind.Video Then
                        vlcHost.AttachVideoSurface(pnlVideo.Handle)
                    Else
                        vlcHost.AttachVideoSurface(IntPtr.Zero)
                        lblNowPlaying.Text = System.IO.Path.GetFileName(path)
                    End If
                    vlcHost.PlayFile(path)
                    MediaSessionIpc.BroadcastState(kind, System.IO.Path.GetFileName(path), True, 0)
                Catch ex As Exception
                    MsgBox("Playback failed:" & vbCrLf & ex.Message, MsgBoxStyle.OkOnly Or MsgBoxStyle.Exclamation, "LCARS MEDIA")
                End Try
        End Select
        ApplyRightRailLayout()
    End Sub

    Private Sub ApplyContentVisibility(ByVal kind As MediaKind)
        picturebox1.Visible = (kind = MediaKind.Photo OrElse kind = MediaKind.None)
        If pnlVideo IsNot Nothing Then pnlVideo.Visible = (kind = MediaKind.Video)
        If pnlMusic IsNot Nothing Then pnlMusic.Visible = (kind = MediaKind.Music)
        If fbPlayPause IsNot Nothing Then fbPlayPause.Visible = (kind = MediaKind.Music OrElse kind = MediaKind.Video)
        sbShow.Visible = (kind = MediaKind.Photo OrElse kind = MediaKind.None)
        fbZoomIn.Visible = (kind = MediaKind.Photo OrElse kind = MediaKind.None)
        fbZoomOut.Visible = (kind = MediaKind.Photo OrElse kind = MediaKind.None)
        fbActual.Visible = (kind = MediaKind.Photo OrElse kind = MediaKind.None)
        pbZoom.Visible = (kind = MediaKind.Photo OrElse kind = MediaKind.None)
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
        MediaSessionIpc.BroadcastState(currentKind, System.IO.Path.GetFileName(currentPath), vlcHost.IsPlaying, vlcHost.PositionMs())
    End Sub

    Protected Overrides Sub OnShellChromeLayout()
        ' Media app uses its own right-rail stack (CLOSE under NAV), not shell Start-Menu CLOSE alignment.
        ApplyRightRailLayout()
    End Sub

    ''' <summary>
    ''' Right-justified vertical stack: BROWSE, slideshow, zoom, NAV (diameter = BROWSE width), CLOSE.
    ''' </summary>
    Private Sub ApplyRightRailLayout()
        If sbBrowse Is Nothing OrElse sbExit Is Nothing Then Return
        If ClientSize.Width < 100 OrElse ClientSize.Height < 100 Then Return

        Const margin As Integer = 8
        Const gap As Integer = 6
        Dim railW As Integer = Math.Max(100, sbBrowse.Width)
        Dim right As Integer = ClientSize.Width - margin
        Dim railLeft As Integer = right - railW

        ' --- CLOSE at bottom of stack ---
        sbExit.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        sbExit.Size = New Size(railW, 28)
        sbExit.Location = New Point(railLeft, ClientSize.Height - margin - sbExit.Height)
        sbExit.ButtonText = "CLOSE"
        sbExit.Text = "CLOSE"
        sbExit.BringToFront()

        Dim y As Integer = sbExit.Top - gap

        ' --- NAV cross: outer diameter = railW ---
        Dim navSize As Integer = railW
        Dim arm As Integer = Math.Max(22, CInt(Math.Round(navSize * 0.24)))
        Dim navTop As Integer = y - navSize
        Dim navLeft As Integer = railLeft

        ' Horizontal arm (Panel2)
        Panel2.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        Panel2.Size = New Size(navSize, arm)
        Panel2.Location = New Point(navLeft, navTop + (navSize - arm) \ 2)
        LayoutNavHorizontalArm(Panel2, arm)

        ' Vertical arm (panel1)
        panel1.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        panel1.Size = New Size(arm, navSize)
        panel1.Location = New Point(navLeft + (navSize - arm) \ 2, navTop)
        LayoutNavVerticalArm(panel1, arm)

        panel1.BringToFront()
        Panel2.BringToFront()
        y = navTop - gap

        ' --- Zoom pie + −/FULL/+ row ---
        Dim zoomH As Integer = Math.Max(28, CInt(Math.Round(railW * 0.28)))
        Dim zoomBtnW As Integer = (railW - 4) \ 3
        fbZoomOut.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        fbActual.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        fbZoomIn.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        pbZoom.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right

        fbZoomOut.Size = New Size(zoomBtnW, zoomH)
        fbActual.Size = New Size(zoomBtnW, zoomH)
        fbZoomIn.Size = New Size(railW - zoomBtnW * 2, zoomH)
        fbZoomOut.Location = New Point(railLeft, y - zoomH)
        fbActual.Location = New Point(fbZoomOut.Right + 2, fbZoomOut.Top)
        fbZoomIn.Location = New Point(fbActual.Right + 2, fbZoomOut.Top)

        Dim pieH As Integer = Math.Max(36, CInt(Math.Round(railW * 0.42)))
        pbZoom.Size = New Size(railW, pieH)
        pbZoom.Location = New Point(railLeft, fbZoomOut.Top - 2 - pieH)
        ' Cutout circle diameter ≈ rail width (was CircleRadius 110 → ~220px ring).
        pbZoom.CircleRadius = railW \ 2
        pbZoom.CircleLocation = New Point(railW \ 2, pieH + railW \ 4)
        y = pbZoom.Top - gap

        ' --- BROWSE / SLIDESHOW ---
        sbShow.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        sbBrowse.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        sbShow.Size = New Size(railW, sbShow.Height)
        sbBrowse.Size = New Size(railW, sbBrowse.Height)
        sbShow.Location = New Point(railLeft, y - sbShow.Height)
        sbBrowse.Location = New Point(railLeft, sbShow.Top - gap - sbBrowse.Height)

        sbBrowse.BringToFront()
        sbShow.BringToFront()
        If fbPlayPause IsNot Nothing AndAlso fbPlayPause.Visible Then
            fbPlayPause.Size = New Size(railW, 35)
            fbPlayPause.Location = New Point(railLeft, sbBrowse.Top - gap - fbPlayPause.Height)
            fbPlayPause.BringToFront()
        End If
        fbZoomOut.BringToFront()
        fbActual.BringToFront()
        fbZoomIn.BringToFront()
        pbZoom.BringToFront()
        sbExit.BringToFront()
    End Sub

    Private Sub LayoutNavHorizontalArm(ByVal host As Panel, ByVal arm As Integer)
        If host Is Nothing Then Return
        Dim w As Integer = host.Width
        Dim btnH As Integer = Math.Max(18, arm - 4)
        Dim top As Integer = Math.Max(0, (host.Height - btnH) \ 2)
        Dim side As Integer = Math.Max(18, CInt(w * 0.16))
        Dim midW As Integer = Math.Max(24, w - side * 2 - 8)

        abPrev.Size = New Size(side, btnH)
        abPrev.Location = New Point(2, top)
        lft.Size = New Size(Math.Max(14, side - 4), btnH)
        lft.Location = New Point(abPrev.Right + 1, top)
        FlatButton1.Size = New Size(midW, btnH)
        FlatButton1.Location = New Point((w - midW) \ 2, top)
        FlatButton13.Size = New Size(Math.Max(12, side \ 2), btnH)
        FlatButton13.Location = New Point(FlatButton1.Right + 1, top)
        rht.Size = New Size(Math.Max(14, side - 4), btnH)
        rht.Location = New Point(w - side - 2 - rht.Width, top)
        abNext.Size = New Size(side, btnH)
        abNext.Location = New Point(w - side - 1, top)
    End Sub

    Private Sub LayoutNavVerticalArm(ByVal host As Panel, ByVal arm As Integer)
        If host Is Nothing Then Return
        Dim h As Integer = host.Height
        Dim btnW As Integer = Math.Max(18, arm - 4)
        Dim left As Integer = Math.Max(0, (host.Width - btnW) \ 2)
        Dim side As Integer = Math.Max(18, CInt(h * 0.16))
        Dim midH As Integer = Math.Max(24, h - side * 2 - 8)

        ArrowButton1.Size = New Size(btnW, side)
        ArrowButton1.Location = New Point(left, 1)
        up.Size = New Size(btnW, Math.Max(14, side - 2))
        up.Location = New Point(left, ArrowButton1.Bottom + 1)
        FlatButton2.Size = New Size(host.Width, Math.Min(midH, 28))
        FlatButton2.Location = New Point(0, (h - FlatButton2.Height) \ 2)
        FlatButton8.Size = New Size(btnW, midH)
        FlatButton8.Location = New Point(left, FlatButton2.Top - 4)
        FlatButton8.SendToBack()
        dwn.Size = New Size(btnW, Math.Max(14, side - 2))
        dwn.Location = New Point(left, h - side - dwn.Height - 1)
        ArrowButton2.Size = New Size(btnW, side)
        ArrowButton2.Location = New Point(left, h - side - 1)
        FlatButton2.BringToFront()
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
        abNext_Click(sender, e)



    End Sub

    Private Sub sbShow_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles sbShow.Click



        tmrShow.Enabled = Not tmrShow.Enabled

        If tmrShow.Enabled = True Then
            sbShow.ButtonText = "Stop Slideshow"

        Else
            sbShow.ButtonText = "Start Slideshow"



        End If
    End Sub

    Private Sub sbBrowse_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles sbBrowse.Click
        Using dlg As New OpenFileDialog()
            dlg.Title = "Open media"
            dlg.Filter = "Media files|*.jpg;*.jpeg;*.gif;*.bmp;*.png;*.tif;*.tiff;*.webp;*.mp3;*.flac;*.wav;*.m4a;*.aac;*.ogg;*.wma;*.opus;*.mp4;*.mkv;*.avi;*.wmv;*.mov;*.m4v;*.webm;*.mpg;*.mpeg;*.ts|Images|*.jpg;*.jpeg;*.gif;*.bmp;*.png;*.tif;*.tiff;*.webp|Audio|*.mp3;*.flac;*.wav;*.m4a;*.aac;*.ogg;*.wma;*.opus|Video|*.mp4;*.mkv;*.avi;*.wmv;*.mov;*.m4v;*.webm;*.mpg;*.mpeg;*.ts|All files|*.*"
            dlg.Multiselect = False
            If dlg.ShowDialog(Me) <> DialogResult.OK Then Return
            LoadMedia(dlg.FileName)
            Return
        End Using
    End Sub

    Private Sub abNext_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles abNext.Click

        If index + 1 > myFiles.Count Then
            ''we're past the end of the array, so start over
            index = 1
        Else
            index += 1
        End If

        loadImages(index)
    End Sub

    Private Sub abPrev_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles abPrev.Click

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



    Private Sub ArrowButton1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ArrowButton1.Click


        If picturebox1.Image IsNot Nothing Then
            picturebox1.Image.RotateFlip(RotateFlipType.Rotate180FlipNone)
            picturebox1.Refresh()
        End If

    End Sub

    Private Sub ArrowButton2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ArrowButton2.Click

        If picturebox1.Image IsNot Nothing Then
            picturebox1.Image.RotateFlip(RotateFlipType.Rotate180FlipNone)
            picturebox1.Refresh()
        End If

    End Sub


    Private Sub sbExit_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles sbExit.Click
        StopCurrentPlayback()
        If vlcHost IsNot Nothing Then
            vlcHost.Dispose()
            vlcHost = Nothing
        End If
        Me.Close()
    End Sub

    Private shiftButton As LCARS.Controls.FlatButton

    Private Sub shiftButtonDown(ByVal sender As Object, ByVal e As EventArgs) Handles up.MouseDown, dwn.MouseDown, lft.MouseDown, rht.MouseDown
        If picturebox1.Image Is Nothing Then Return
        shiftButton = DirectCast(sender, LCARS.Controls.FlatButton)
        tmrShift.Start()
        shiftTimer_Tick(sender, e)
    End Sub

    Private Sub shiftButtonUp(ByVal sender As Object, ByVal e As EventArgs) Handles up.MouseUp, dwn.MouseUp, lft.MouseUp, rht.MouseUp
        tmrShift.Stop()
        shiftButton = Nothing
    End Sub

    Private Sub shiftTimer_Tick(ByVal sender As Object, ByVal e As EventArgs) Handles tmrShift.Tick
        If shiftButton Is up Then
            picturebox1.Top += shiftDelta
        ElseIf shiftButton Is dwn Then
            picturebox1.Top -= shiftDelta
        ElseIf shiftButton Is lft Then
            picturebox1.Left += shiftDelta
        ElseIf shiftButton Is rht Then
            picturebox1.Left -= shiftDelta
        End If
    End Sub
End Class
