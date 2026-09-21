' LCARSmain/Modules/modMediaStrip.vb
Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms
Imports LCARS.Controls

''' <summary>
''' Compact media transport strip on the View 1 weather/clock row.
''' Follows the same dual-parent layout path as lblWeather (header vs speech-row).
''' </summary>
Friend Module modMediaStrip
    Private Const StripName As String = "pnlMediaStrip"
    Private Const StripHeight As Integer = 22
    Private ReadOnly attached As New HashSet(Of Integer)()

    Public Sub SyncWithWeather(ByVal b As modBusiness, ByVal weather As Control, ByVal headerMode As Boolean)
        If b Is Nothing OrElse b.myForm Is Nothing Then Return
        If Not modMediaSession.HasActiveSession Then
            HideStrip(b)
            Return
        End If

        Dim strip As Panel = EnsureStrip(b)
        If strip Is Nothing OrElse weather Is Nothing OrElse weather.Parent Is Nothing Then
            HideStrip(b)
            Return
        End If

        Dim host As Control = weather.Parent
        If strip.Parent IsNot host Then
            host.Controls.Add(strip)
        End If

        UpdateStripLabels(strip)
        Dim h As Integer = Math.Max(18, Math.Min(StripHeight, weather.Height))
        Dim titleBtn As FlatButton = TryCast(strip.Controls("fbMediaTitle"), FlatButton)
        Dim titleW As Integer = 120
        If titleBtn IsNot Nothing Then
            Dim t As String = If(titleBtn.ButtonText, "")
            titleW = Math.Min(180, Math.Max(80, TextRenderer.MeasureText(t, titleBtn.Font).Width + 16))
        End If
        Dim stripW As Integer = titleW + 28 + 28 + 8

        Dim left As Integer
        Dim top As Integer = weather.Top + Math.Max(0, (weather.Height - h) \ 2)
        If headerMode Then
            ' Sit immediately left of weather
            left = Math.Max(132, weather.Left - stripW - 8)
            If left + stripW > weather.Left - 4 Then
                stripW = Math.Max(80, weather.Left - left - 4)
            End If
        Else
            ' Speech row: to the right of weather on fbClock
            left = weather.Right + 6
            Dim maxRight As Integer = host.Width - 4
            If left + stripW > maxRight Then
                stripW = Math.Max(80, maxRight - left)
            End If
            If stripW < 80 Then
                strip.Visible = False
                Return
            End If
        End If

        strip.Visible = True
        strip.Location = New Point(left, top)
        strip.Size = New Size(stripW, h)
        LayoutStripChildren(strip)
        strip.BringToFront()
    End Sub

    Public Sub Tick()
        ' Refresh labels while playing
        For Each b As modBusiness In CommonScreen.curBusiness
            If b Is Nothing OrElse Not b.isInit Then Continue For
            Dim found() As Control = b.myForm.Controls.Find(StripName, True)
            If found Is Nothing OrElse found.Length = 0 Then Continue For
            Dim strip As Panel = TryCast(found(0), Panel)
            If strip Is Nothing OrElse Not strip.Visible Then Continue For
            If Not modMediaSession.HasActiveSession Then
                strip.Visible = False
                Continue For
            End If
            UpdateStripLabels(strip)
        Next
    End Sub

    Private Sub HideStrip(ByVal b As modBusiness)
        If b Is Nothing OrElse b.myForm Is Nothing Then Return
        Dim found() As Control = b.myForm.Controls.Find(StripName, True)
        If found Is Nothing OrElse found.Length = 0 Then Return
        found(0).Visible = False
    End Sub

    Private Function EnsureStrip(ByVal b As modBusiness) As Panel
        Dim found() As Control = b.myForm.Controls.Find(StripName, True)
        If found IsNot Nothing AndAlso found.Length > 0 Then
            Return TryCast(found(0), Panel)
        End If

        Dim strip As New Panel()
        strip.Name = StripName
        strip.BackColor = Color.Black
        strip.Visible = False

        Dim fbTitle As New FlatButton()
        fbTitle.Name = "fbMediaTitle"
        fbTitle.ButtonText = "MEDIA"
        fbTitle.Text = "MEDIA"
        fbTitle.Color = LCARS.LCARScolorStyles.StaticTan
        fbTitle.ButtonTextAlign = ContentAlignment.MiddleLeft
        fbTitle.Clickable = True
        AddHandler fbTitle.Click, AddressOf OnShowPlayer
        strip.Controls.Add(fbTitle)

        Dim fbPlay As New FlatButton()
        fbPlay.Name = "fbMediaPlay"
        fbPlay.ButtonText = "||"
        fbPlay.Text = "||"
        fbPlay.Color = LCARS.LCARScolorStyles.PrimaryFunction
        fbPlay.ButtonTextAlign = ContentAlignment.MiddleCenter
        AddHandler fbPlay.Click, AddressOf OnPlayPause
        strip.Controls.Add(fbPlay)

        Dim fbStop As New FlatButton()
        fbStop.Name = "fbMediaStop"
        fbStop.ButtonText = "■"
        fbStop.Text = "■"
        fbStop.Color = LCARS.LCARScolorStyles.FunctionOffline
        fbStop.ButtonTextAlign = ContentAlignment.MiddleCenter
        AddHandler fbStop.Click, AddressOf OnStop
        strip.Controls.Add(fbStop)

        ' Temporary parent; Sync will reparent next to weather.
        b.myForm.Controls.Add(strip)
        attached.Add(b.ScreenIndex)
        Return strip
    End Function

    Private Sub LayoutStripChildren(ByVal strip As Panel)
        If strip Is Nothing Then Return
        Dim h As Integer = strip.Height
        Dim fbStop As Control = strip.Controls("fbMediaStop")
        Dim fbPlay As Control = strip.Controls("fbMediaPlay")
        Dim fbTitle As Control = strip.Controls("fbMediaTitle")
        Dim btnW As Integer = Math.Max(22, h)
        If fbStop IsNot Nothing Then
            fbStop.Size = New Size(btnW, h)
            fbStop.Location = New Point(strip.Width - btnW, 0)
        End If
        If fbPlay IsNot Nothing Then
            fbPlay.Size = New Size(btnW, h)
            fbPlay.Location = New Point(strip.Width - btnW * 2 - 2, 0)
        End If
        If fbTitle IsNot Nothing Then
            fbTitle.Location = New Point(0, 0)
            fbTitle.Size = New Size(Math.Max(40, strip.Width - btnW * 2 - 4), h)
        End If
    End Sub

    Private Sub UpdateStripLabels(ByVal strip As Panel)
        Dim fbTitle As FlatButton = TryCast(strip.Controls("fbMediaTitle"), FlatButton)
        Dim fbPlay As FlatButton = TryCast(strip.Controls("fbMediaPlay"), FlatButton)
        If fbTitle IsNot Nothing Then
            Dim t As String = If(String.IsNullOrEmpty(modMediaSession.LastTitle), "MEDIA", modMediaSession.LastTitle)
            If t.Length > 22 Then t = t.Substring(0, 20) & "…"
            fbTitle.ButtonText = t
            fbTitle.Text = t
        End If
        If fbPlay IsNot Nothing Then
            Dim p As String = If(modMediaSession.LastPlaying, "||", "▶")
            fbPlay.ButtonText = p
            fbPlay.Text = p
        End If
    End Sub

    Private Sub OnPlayPause(ByVal sender As Object, ByVal e As EventArgs)
        modMediaSession.SendCommandToPlayer(1)
    End Sub

    Private Sub OnStop(ByVal sender As Object, ByVal e As EventArgs)
        modMediaSession.SendCommandToPlayer(2)
    End Sub

    Private Sub OnShowPlayer(ByVal sender As Object, ByVal e As EventArgs)
        modMediaSession.SendCommandToPlayer(3)
    End Sub
End Module
