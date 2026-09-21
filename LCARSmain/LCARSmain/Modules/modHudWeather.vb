Option Strict On

Imports System.Globalization
Imports System.Net
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Threading
Imports System.Windows.Forms

''' <summary>
''' Compact read-only weather HUD on main screens (Open-Meteo; no API key).
''' Location: city name, optional lat/lon, or IP geolocation fallback.
''' </summary>
Friend Module modHudWeather
    Private Const RefreshMinutes As Integer = 30
    Private Const TickCheckInterval As Integer = 50
    Private Const Tls12 As Integer = 3072
    Private Const WeatherHeight As Integer = 22
    Private ReadOnly WeatherFont As New Font("LCARS", 16.0!, FontStyle.Regular, GraphicsUnit.Point, CByte(0))

    Private ReadOnly attachedScreens As New HashSet(Of Integer)()
    Private ReadOnly weatherLock As New Object()

    Private weatherText As String = "WX ..."
    Private lastFetchUtc As DateTime = DateTime.MinValue
    Private fetchInProgress As Boolean = False
    Private tickCounter As Integer = 0
    Private tlsEnabled As Boolean = False
    Private ReadOnly clockHandlersAttached As New HashSet(Of Integer)()
    Private ReadOnly lastWeatherTickLayoutKey As New Dictionary(Of Integer, String)()
    Private ReadOnly view1WeatherLayoutInProgress As New HashSet(Of Integer)()

    Private Const View1BarLeft As Integer = 132
    Private Const View1HeaderStripHeight As Integer = 66
    Private Const View1WeatherMinWidth As Integer = 80

    ''' <summary>
    ''' Recomputes weather position after LCARS main-bar layout changes.
    ''' </summary>
    Public Sub SyncWeatherLayoutPublic(ByVal b As modBusiness)
        SyncWeatherLayout(b)
    End Sub

    Public Sub RemoveView1LegacySpeechRowBar(ByVal form As Form)
        If form Is Nothing Then Return
        Dim leftoverNames As String() = {"fbView1SpeechRowBar", "fbView1TopBar"}
        For Each leftoverName As String In leftoverNames
            Dim legacy() As Control = form.Controls.Find(leftoverName, True)
            For Each match As Control In legacy
                If match.Parent IsNot Nothing Then
                    match.Parent.Controls.Remove(match)
                End If
                match.Dispose()
            Next
        Next
    End Sub

    Public Function IsView1CollapsedPublic(ByVal b As modBusiness) As Boolean
        Return IsView1Collapsed(b)
    End Function

    ''' <summary>
    ''' Repositions weather after mainscreen expand/collapse swaps the clock control.
    ''' </summary>
    Public Sub RebindClockLayout(ByVal b As modBusiness)
        If Not b.hasClock Then Return
        SyncWeatherLayout(b)
    End Sub

    Public Sub AttachWeatherHud(ByVal b As modBusiness)
        If Not b.hasClock OrElse b.myClock Is Nothing Then Return
        If attachedScreens.Contains(b.ScreenIndex) AndAlso b.myWeather IsNot Nothing AndAlso Not b.myWeather.IsDisposed Then
            SyncWeatherLayout(b)
            Return
        End If

        Dim existing() As Control = b.myForm.Controls.Find("lblWeather", True)
        If existing.Length > 0 AndAlso Not existing(0).IsDisposed Then
            b.myWeather = existing(0)
        ElseIf String.Equals(b.myForm.Name, "frmMainscreen1", StringComparison.OrdinalIgnoreCase) Then
            Dim fbClock As LCARS.Controls.FlatButton = GetView1BarClock(b.myForm)
            Dim mainBar As Control = GetView1MainBar(b.myForm)
            Dim parent As Control = If(fbClock, If(mainBar, b.myClock.Parent))
            b.myWeather = CreateWeatherControlOnParent(parent)
        Else
            b.myWeather = CreateWeatherControl(b.myClock)
        End If

        If b.myWeather Is Nothing Then
            modDiagnostics.LogWarn("modHudWeather.AttachWeatherHud",
                "screen=" & b.ScreenIndex & " failed to create weather control clockParent=" &
                If(b.myClock.Parent Is Nothing, "null", b.myClock.Parent.Name))
            Return
        End If

        attachedScreens.Add(b.ScreenIndex)
        AttachClockLayoutHandlers(b)
        If String.Equals(b.myForm.Name, "frmMainscreen1", StringComparison.OrdinalIgnoreCase) Then
            RemoveView1LegacySpeechRowBar(b.myForm)
        End If
        SyncWeatherLayout(b)
        InitDisplay(b)
        RequestRefreshIfNeeded(True)
        modDiagnostics.LogInfo("modHudWeather.AttachWeatherHud",
            "screen=" & b.ScreenIndex & " weather=" & b.myWeather.Bounds.ToString() &
            " parent=" & If(b.myWeather.Parent Is Nothing, "null", b.myWeather.Parent.Name))
    End Sub

    ''' <summary>
    ''' Applies the cached weather text to one screen.
    ''' </summary>
    Public Sub InitDisplay(ByVal b As modBusiness)
        If b.myWeather Is Nothing Then Return
        SetWeatherText(b.myWeather, GetWeatherText())
    End Sub

    ''' <summary>
    ''' Called from the common mainscreen timer; throttles refresh and keeps layout aligned.
    ''' </summary>
    Public Sub TickWeather()
        tickCounter += 1
        If tickCounter Mod 10 = 0 Then
            For Each b As modBusiness In CommonScreen.curBusiness
                If Not b.isInit OrElse Not b.hasWeather Then Continue For
                Dim layoutKey As String = GetWeatherLayoutKey(b)
                Dim cachedKey As String = Nothing
                If lastWeatherTickLayoutKey.TryGetValue(b.ScreenIndex, cachedKey) AndAlso cachedKey = layoutKey Then
                    Continue For
                End If
                SyncWeatherLayout(b)
                lastWeatherTickLayoutKey(b.ScreenIndex) = GetWeatherLayoutKey(b)
            Next
            modMediaStrip.Tick()
        End If

        If Not IsWeatherEnabled() Then Return
        If tickCounter Mod TickCheckInterval <> 0 Then Return
        RequestRefreshIfNeeded(False)
    End Sub

    ''' <summary>
    ''' Forces an immediate background fetch (e.g. after settings save).
    ''' </summary>
    Public Sub ForceRefresh()
        SyncLock weatherLock
            lastFetchUtc = DateTime.MinValue
            weatherText = "WX ..."
        End SyncLock
        PushWeatherToScreens()
        RequestRefreshIfNeeded(True)
    End Sub

    Private Sub AttachClockLayoutHandlers(ByVal b As modBusiness)
        If clockHandlersAttached.Contains(b.ScreenIndex) Then Return
        If b.myClock Is Nothing Then Return

        clockHandlersAttached.Add(b.ScreenIndex)
        AddHandler b.myClock.LocationChanged, Sub(sender As Object, e As EventArgs) SyncWeatherLayout(b)
        AddHandler b.myClock.SizeChanged, Sub(sender As Object, e As EventArgs) SyncWeatherLayout(b)
        If b.myClock.Parent IsNot Nothing Then
            AddHandler b.myClock.Parent.Resize, Sub(sender As Object, e As EventArgs) SyncWeatherLayout(b)
        End If
        If b.myMainPanel IsNot Nothing Then
            AddHandler b.myMainPanel.Resize, Sub(sender As Object, e As EventArgs) SyncWeatherLayout(b)
        End If
        If String.Equals(b.myForm.Name, "frmMainscreen1", StringComparison.OrdinalIgnoreCase) Then
            Dim headerClock As Label = GetView1HeaderClock(b.myForm)
            If headerClock IsNot Nothing Then
                AddHandler headerClock.LocationChanged, Sub(sender As Object, e As EventArgs) SyncWeatherLayout(b)
                AddHandler headerClock.SizeChanged, Sub(sender As Object, e As EventArgs) SyncWeatherLayout(b)
                If headerClock.Parent IsNot Nothing Then
                    AddHandler headerClock.Parent.Resize, Sub(sender As Object, e As EventArgs) SyncWeatherLayout(b)
                End If
            End If
            Dim fbClock As LCARS.Controls.FlatButton = GetView1BarClock(b.myForm)
            If fbClock IsNot Nothing Then
                AddHandler fbClock.LocationChanged, Sub(sender As Object, e As EventArgs) SyncWeatherLayout(b)
                AddHandler fbClock.SizeChanged, Sub(sender As Object, e As EventArgs) SyncWeatherLayout(b)
                AddHandler fbClock.ColorsAvailable.ColorsUpdated, Sub(sender As Object, e As EventArgs) SyncWeatherLayout(b)
            End If
            Dim mainBar As Control = GetView1MainBar(b.myForm)
            If mainBar IsNot Nothing Then
                AddHandler mainBar.LocationChanged, Sub(sender As Object, e As EventArgs) SyncWeatherLayout(b)
                AddHandler mainBar.Resize, Sub(sender As Object, e As EventArgs) SyncWeatherLayout(b)
            End If
            Dim container As Control = GetView1Container(b.myForm)
            If container IsNot Nothing Then
                AddHandler container.Resize, Sub(sender As Object, e As EventArgs) SyncWeatherLayout(b)
            End If
        Else
            Dim headerClock As Label = GetView1HeaderClock(b.myForm)
            If headerClock IsNot Nothing Then
                AddHandler headerClock.LocationChanged, Sub(sender As Object, e As EventArgs) SyncWeatherLayout(b)
                AddHandler headerClock.SizeChanged, Sub(sender As Object, e As EventArgs) SyncWeatherLayout(b)
                If headerClock.Parent IsNot Nothing Then
                    AddHandler headerClock.Parent.Resize, Sub(sender As Object, e As EventArgs) SyncWeatherLayout(b)
                End If
            End If
            Dim fbClock As LCARS.Controls.FlatButton = GetView1BarClock(b.myForm)
            If fbClock IsNot Nothing Then
                AddHandler fbClock.LocationChanged, Sub(sender As Object, e As EventArgs) SyncWeatherLayout(b)
                AddHandler fbClock.SizeChanged, Sub(sender As Object, e As EventArgs) SyncWeatherLayout(b)
                AddHandler fbClock.ColorsAvailable.ColorsUpdated, Sub(sender As Object, e As EventArgs) SyncWeatherLayout(b)
            End If
            Dim mainBar As Control = GetView1MainBar(b.myForm)
            If mainBar IsNot Nothing Then
                AddHandler mainBar.Resize, Sub(sender As Object, e As EventArgs) SyncWeatherLayout(b)
                AddHandler mainBar.LocationChanged, Sub(sender As Object, e As EventArgs) SyncWeatherLayout(b)
            End If
            Dim container As Control = GetView1Container(b.myForm)
            If container IsNot Nothing Then
                AddHandler container.Resize, Sub(sender As Object, e As EventArgs) SyncWeatherLayout(b)
            End If
            Dim secondaryMatches() As Control = b.myForm.Controls.Find("FlatButton7", True)
            If secondaryMatches.Length > 0 Then
                Dim secondaryBar As Control = secondaryMatches(0)
                AddHandler secondaryBar.LocationChanged, Sub(sender As Object, e As EventArgs) SyncWeatherLayout(b)
                AddHandler secondaryBar.SizeChanged, Sub(sender As Object, e As EventArgs) SyncWeatherLayout(b)
            End If
        End If
    End Sub

    Private Function GetWeatherLayoutKey(ByVal b As modBusiness) As String
        If b.myClock Is Nothing OrElse b.myMainPanel Is Nothing Then Return String.Empty
        Dim weatherText As String = If(b.myWeather Is Nothing, String.Empty, b.myWeather.Text)
        Dim clockText As String = If(b.myClock Is Nothing, String.Empty, b.myClock.Text)
        Dim view1State As String = String.Empty
        If String.Equals(b.myForm.Name, "frmMainscreen1", StringComparison.OrdinalIgnoreCase) Then
            Dim mainBar As Control = GetView1MainBar(b.myForm)
            If mainBar IsNot Nothing Then
                view1State = "|barTop=" & mainBar.Top.ToString()
            End If
        End If
        Return b.myClock.Bounds.ToString() & "|" & b.myMainPanel.Size.ToString() & "|" & weatherText & "|" & clockText & view1State
    End Function

    Private Function IsView1Collapsed(ByVal b As modBusiness) As Boolean
        If b.myForm Is Nothing Then Return False
        If Not String.Equals(b.myForm.Name, "frmMainscreen1", StringComparison.OrdinalIgnoreCase) Then Return False
        Dim mainBars() As Control = b.myForm.Controls.Find("pnlMainBar", True)
        If mainBars.Length = 0 Then Return False
        Return mainBars(0).Top <> 0
    End Function

    Private Sub EnsureControlOnParent(ByVal child As Control, ByVal parent As Control)
        If child Is Nothing OrElse parent Is Nothing Then Return
        If child.Parent Is parent Then Return
        If child.Parent IsNot Nothing Then child.Parent.Controls.Remove(child)
        parent.Controls.Add(child)
    End Sub

    Private Function GetView1SpeechButton(ByVal form As Form) As Control
        If form Is Nothing Then Return Nothing
        Dim matches() As Control = form.Controls.Find("mySpeech", True)
        If matches.Length = 0 Then Return Nothing
        Return matches(0)
    End Function

    Private Function GetView1Container(ByVal form As Form) As Control
        If form Is Nothing Then Return Nothing
        Dim matches() As Control = form.Controls.Find("pnlMainContainer", True)
        If matches.Length = 0 Then Return Nothing
        Return matches(0)
    End Function

    Private Function GetView1MainBar(ByVal form As Form) As Control
        If form Is Nothing Then Return Nothing
        Dim matches() As Control = form.Controls.Find("pnlMainBar", True)
        If matches.Length = 0 Then Return Nothing
        Return matches(0)
    End Function

    Private Function GetView1BarClock(ByVal form As Form) As LCARS.Controls.FlatButton
        If form Is Nothing Then Return Nothing
        Dim matches() As Control = form.Controls.Find("fbClock", True)
        If matches.Length = 0 Then Return Nothing
        Return TryCast(matches(0), LCARS.Controls.FlatButton)
    End Function

    Private Function GetView1HeaderClock(ByVal form As Form) As Label
        If form Is Nothing Then Return Nothing
        Dim clockControls() As Control = form.Controls.Find("myClock", True)
        If clockControls.Length = 0 Then Return Nothing
        Return TryCast(clockControls(0), Label)
    End Function

    Private Function GetView1ClockLeftInContainer(ByVal container As Control, ByVal mainBar As Control, ByVal fbClock As LCARS.Controls.FlatButton, ByVal headerClock As Label) As Integer
        Dim clockLeft As Integer = container.ClientSize.Width
        If headerClock IsNot Nothing AndAlso headerClock.Visible Then
            clockLeft = Math.Min(clockLeft, headerClock.Left)
        End If
        If fbClock IsNot Nothing Then
            Dim fbLeft As Integer = fbClock.Left
            If fbClock.Parent Is mainBar Then fbLeft = mainBar.Left + fbClock.Left
            clockLeft = Math.Min(clockLeft, fbLeft)
        End If
        Return clockLeft
    End Function

    Private Sub SyncWeatherLayout(ByVal b As modBusiness)
        If b.myClock Is Nothing OrElse b.myClock.IsDisposed Then Return

        If String.Equals(b.myForm.Name, "frmMainscreen1", StringComparison.OrdinalIgnoreCase) Then
            SyncView1WeatherLayout(b)
            Return
        End If

        If b.myWeather Is Nothing OrElse b.myWeather.IsDisposed Then Return

        Using diag As DiagnosticScope = modDiagnostics.BeginScope("modHudWeather.SyncWeatherLayout", "screen=" & b.ScreenIndex)
            SyncGenericWeatherLayout(b)
        End Using
    End Sub

    Private Sub SyncView1WeatherLayout(ByVal b As modBusiness)
        If view1WeatherLayoutInProgress.Contains(b.ScreenIndex) Then Return
        view1WeatherLayoutInProgress.Add(b.ScreenIndex)
        Try
            SyncView1WeatherLayoutCore(b)
        Finally
            view1WeatherLayoutInProgress.Remove(b.ScreenIndex)
        End Try
    End Sub

    Private Sub SyncView1CollapsedHeaderColors(ByVal b As modBusiness, ByVal headerClock As Label)
        If headerClock Is Nothing Then Return
        headerClock.ForeColor = Color.Orange
        headerClock.BackColor = Color.Transparent
    End Sub

    Private Sub SyncView1WeatherLayoutCore(ByVal b As modBusiness)
        Using diag As DiagnosticScope = modDiagnostics.BeginScope("modHudWeather.SyncView1WeatherLayout", "screen=" & b.ScreenIndex)
            Dim mainBar As Control = GetView1MainBar(b.myForm)
            Dim container As Control = GetView1Container(b.myForm)
            Dim fbClock As LCARS.Controls.FlatButton = GetView1BarClock(b.myForm)
            Dim headerClock As Label = GetView1HeaderClock(b.myForm)
            Dim weather As Control = b.myWeather
            ' Code "collapsed" = panel slid down, large myClock visible (user's annotated "move weather up" state).
            Dim headerClockMode As Boolean = IsView1Collapsed(b)

            If headerClockMode AndAlso headerClock IsNot Nothing Then
                SyncView1CollapsedHeaderColors(b, headerClock)
            End If

            If mainBar Is Nothing OrElse container Is Nothing OrElse fbClock Is Nothing Then
                modDiagnostics.LogWarn("modHudWeather.SyncView1WeatherLayout",
                    "screen=" & b.ScreenIndex & " missing chrome mainBar=" & (mainBar IsNot Nothing).ToString() &
                    " container=" & (container IsNot Nothing).ToString() &
                    " fbClock=" & (fbClock IsNot Nothing).ToString())
                Return
            End If

            If weather Is Nothing OrElse weather.IsDisposed Then
                Dim createParent As Control = If(headerClockMode AndAlso headerClock IsNot Nothing, headerClock.Parent, fbClock)
                weather = CreateWeatherControlOnParent(If(createParent, container))
                b.myWeather = weather
                If weather IsNot Nothing Then
                    SetWeatherText(weather, GetWeatherText())
                    RequestRefreshIfNeeded(True)
                End If
            End If
            If weather Is Nothing OrElse weather.IsDisposed Then
                modDiagnostics.LogWarn("modHudWeather.SyncView1WeatherLayout", "screen=" & b.ScreenIndex & " weather still null")
                Return
            End If

            Dim weatherLabel As Label = TryCast(weather, Label)
            weather.Font = WeatherFont
            weather.Anchor = AnchorStyles.None
            If String.IsNullOrEmpty(weather.Text) Then weather.Text = GetWeatherText()

            Dim textWidth As Integer = TextRenderer.MeasureText(If(String.IsNullOrEmpty(weather.Text), "WX ...", weather.Text), weather.Font).Width + 8
            Dim desiredWidth As Integer = Math.Max(View1WeatherMinWidth, Math.Min(textWidth, 420))

            If headerClockMode AndAlso headerClock IsNot Nothing AndAlso headerClock.Visible Then
                ' Beside the large header clock (empty space to its left), same ink as the clock.
                LayoutView1HeaderWeather(b, weather, weatherLabel, headerClock, container, desiredWidth)
                modMediaStrip.SyncWithWeather(b, weather, True)
            Else
                ' Speech-row mode: on fbClock fill, black text like the bar clock glyphs.
                LayoutView1SpeechRowWeather(weather, weatherLabel, fbClock, desiredWidth)
                modMediaStrip.SyncWithWeather(b, weather, False)
            End If

            modDiagnostics.LogInfo("modHudWeather.SyncView1WeatherLayout",
                "headerClockMode=" & headerClockMode.ToString() &
                " weatherText=" & If(weather.Text, "") &
                " fore=" & weather.ForeColor.ToString() &
                " back=" & weather.BackColor.ToString() &
                " visible=" & weather.Visible.ToString() &
                " bounds=" & weather.Bounds.ToString() &
                " parent=" & If(weather.Parent Is Nothing, "null", weather.Parent.Name) &
                " fbClock=" & fbClock.Bounds.ToString())
        End Using
    End Sub

    ''' <summary>
    ''' Places weather in the header empty space left of myClock; orange like the clock.
    ''' </summary>
    Private Sub LayoutView1HeaderWeather(ByVal b As modBusiness, ByVal weather As Control, ByVal weatherLabel As Label,
                                         ByVal headerClock As Label, ByVal container As Control, ByVal desiredWidth As Integer)
        Dim host As Control = If(headerClock.Parent, container)
        If host Is Nothing Then Return

        EnsureControlOnParent(weather, host)
        Dim clockFore As Color = headerClock.ForeColor
        If clockFore.A = 0 OrElse clockFore.ToArgb() = Color.Black.ToArgb() Then
            clockFore = If(b.myForm IsNot Nothing, b.myForm.ForeColor, Color.Orange)
        End If
        ' Black plate behind orange text would look wrong; use host black (no opaque tan plate).
        ApplyView1WeatherLabelStyle(weather, weatherLabel, clockFore, Color.Black, ContentAlignment.MiddleRight)

        weather.Height = Math.Max(WeatherHeight, Math.Min(36, headerClock.Height \ 2))
        Dim width As Integer = desiredWidth

        ' myClock is TopRight-aligned — sit in the open area immediately left of the digits.
        Dim clockText As String = If(String.IsNullOrEmpty(headerClock.Text), "000000.00", headerClock.Text)
        Dim clockTextWidth As Integer = TextRenderer.MeasureText(clockText, headerClock.Font).Width
        Dim clockTextLeft As Integer = headerClock.Right - clockTextWidth
        Dim gap As Integer = 12
        Dim left As Integer = Math.Max(View1BarLeft, clockTextLeft - gap - width)
        Dim top As Integer = headerClock.Top + Math.Max(0, (headerClock.Height - weather.Height) \ 2)

        ' Keep clear of the apps/tray strip if it overlaps this band.
        If b.myAppsPanel IsNot Nothing AndAlso b.myAppsPanel.Visible AndAlso b.myAppsPanel.Parent Is host Then
            Dim appsBottom As Integer = b.myAppsPanel.Bottom
            If top < appsBottom AndAlso top + weather.Height > b.myAppsPanel.Top Then
                top = Math.Max(top, appsBottom + 2)
            End If
        End If

        weather.Visible = True
        weather.Location = New Drawing.Point(left, top)
        weather.Size = New Drawing.Size(width, weather.Height)
        weather.BringToFront()
    End Sub

    ''' <summary>
    ''' Places weather on the speech-row fbClock fill with black text (matches bar clock ink).
    ''' </summary>
    Private Sub LayoutView1SpeechRowWeather(ByVal weather As Control, ByVal weatherLabel As Label,
                                            ByVal fbClock As LCARS.Controls.FlatButton, ByVal desiredWidth As Integer)
        EnsureControlOnParent(weather, fbClock)
        Dim tanFill As Color = GetStaticTanColor(fbClock)
        ApplyView1WeatherLabelStyle(weather, weatherLabel, Color.Black, tanFill, ContentAlignment.MiddleLeft)

        weather.Height = Math.Min(WeatherHeight, Math.Max(16, fbClock.Height))

        Dim clockText As String = If(String.IsNullOrEmpty(fbClock.ButtonText), fbClock.Text, fbClock.ButtonText)
        If String.IsNullOrEmpty(clockText) Then clockText = "X32"
        Dim clockTextWidth As Integer = TextRenderer.MeasureText(clockText, WeatherFont).Width + 16
        If clockTextWidth > fbClock.Width \ 2 Then
            clockTextWidth = Math.Max(48, Math.Min(clockTextWidth, fbClock.Width \ 2))
        End If

        Dim gap As Integer = 8
        Dim left As Integer = clockTextWidth + gap
        Dim rightLimit As Integer = Math.Max(left + View1WeatherMinWidth, fbClock.Width - 4)
        Dim width As Integer = Math.Min(desiredWidth, Math.Max(View1WeatherMinWidth, rightLimit - left))
        Dim top As Integer = Math.Max(0, (fbClock.Height - weather.Height) \ 2)

        Dim showWeather As Boolean = fbClock.Visible AndAlso fbClock.Width > 80 AndAlso width >= 40
        weather.Visible = showWeather
        If showWeather Then
            weather.Location = New Drawing.Point(left, top)
            weather.Size = New Drawing.Size(width, weather.Height)
            weather.BringToFront()
        End If
    End Sub

    Private Function GetStaticTanColor(ByVal fbClock As LCARS.Controls.FlatButton) As Color
        Dim tanFill As Color = ColorTranslator.FromHtml("#FFCC66")
        If fbClock Is Nothing Then Return tanFill
        Try
            tanFill = fbClock.ColorsAvailable.getColor(LCARS.LCARScolorStyles.StaticTan)
        Catch
        End Try
        Return tanFill
    End Function

    Private Sub ApplyView1WeatherLabelStyle(ByVal weather As Control, ByVal weatherLabel As Label, ByVal foreColor As Color, ByVal backColor As Color, ByVal align As ContentAlignment)
        weather.ForeColor = foreColor
        weather.BackColor = backColor
        If weatherLabel IsNot Nothing Then
            weatherLabel.ForeColor = foreColor
            weatherLabel.BackColor = backColor
            weatherLabel.TextAlign = align
        End If
    End Sub

    Private Sub SyncGenericWeatherLayout(ByVal b As modBusiness)
        Dim clock As Control = b.myClock
        Dim weather As Control = b.myWeather

        If weather.Parent IsNot clock.Parent Then
            If weather.Parent IsNot Nothing Then
                weather.Parent.Controls.Remove(weather)
            End If
            clock.Parent.Controls.Add(weather)
        End If

        Dim weatherLabel As Label = TryCast(weather, Label)
        weather.Font = WeatherFont
        weather.ForeColor = clock.ForeColor
        weather.Height = WeatherHeight
        If weatherLabel IsNot Nothing Then
            weatherLabel.BackColor = Color.Transparent
            weatherLabel.TextAlign = ContentAlignment.BottomRight
        End If
        weather.Anchor = AnchorStyles.None

        Dim clockTextBottom As Integer = GetClockTextBottom(clock)
        Dim weatherTextHeight As Integer = TextRenderer.MeasureText("Ag", weather.Font).Height
        Dim top As Integer = clockTextBottom - weatherTextHeight
        If top < 0 Then top = 0

        Dim textWidth As Integer = TextRenderer.MeasureText(weather.Text, weather.Font).Width + 8
        Dim width As Integer = Math.Max(80, Math.Min(textWidth, 420))
        Dim maxRight As Integer = clock.Left - 6
        width = Math.Min(width, Math.Max(80, maxRight))
        Dim left As Integer = Math.Max(0, maxRight - width)

        If weather.Location.X <> left OrElse weather.Location.Y <> top OrElse weather.Width <> width OrElse weather.Height <> WeatherHeight Then
            weather.Location = New Drawing.Point(left, top)
            weather.Size = New Drawing.Size(width, WeatherHeight)
        End If

        weather.BringToFront()
    End Sub

    Private Function GetClockTextBottom(ByVal clock As Control) As Integer
        Dim align As ContentAlignment = ContentAlignment.TopLeft
        Dim clockLabel As Label = TryCast(clock, Label)
        If clockLabel IsNot Nothing Then
            align = clockLabel.TextAlign
        Else
            Dim lcarsBtn As LCARS.LCARSbuttonClass = TryCast(clock, LCARS.LCARSbuttonClass)
            If lcarsBtn IsNot Nothing Then
                align = lcarsBtn.ButtonTextAlign
            End If
        End If

        If IsBottomAligned(align) Then
            Return clock.Bottom
        End If

        Dim sample As String = If(String.IsNullOrEmpty(clock.Text), "00:00:00", clock.Text)
        Return clock.Top + TextRenderer.MeasureText(sample, clock.Font).Height
    End Function

    Private Function IsBottomAligned(ByVal align As ContentAlignment) As Boolean
        Return align = ContentAlignment.BottomLeft OrElse _
               align = ContentAlignment.BottomCenter OrElse _
               align = ContentAlignment.BottomRight
    End Function

    Private Sub RequestRefreshIfNeeded(ByVal ignoreInterval As Boolean)
        If Not IsWeatherEnabled() Then Return
        If fetchInProgress Then Return

        Dim shouldFetch As Boolean = ignoreInterval
        SyncLock weatherLock
            If Not shouldFetch Then
                If lastFetchUtc = DateTime.MinValue Then
                    shouldFetch = True
                ElseIf DateTime.UtcNow - lastFetchUtc >= TimeSpan.FromMinutes(RefreshMinutes) Then
                    shouldFetch = True
                End If
            End If
            If shouldFetch Then fetchInProgress = True
        End SyncLock

        If shouldFetch Then
            ThreadPool.QueueUserWorkItem(AddressOf FetchWorker)
        End If
    End Sub

    Private Sub FetchWorker(ByVal state As Object)
        Dim displayText As String = "WX OFFLINE"
        Try
            EnsureTls12()
            displayText = FetchWeatherText()
        Catch
            displayText = "WX OFFLINE"
        Finally
            SyncLock weatherLock
                weatherText = displayText
                lastFetchUtc = DateTime.UtcNow
                fetchInProgress = False
            End SyncLock
            PushWeatherToScreens()
        End Try
    End Sub

    Private Sub PushWeatherToScreens()
        Dim text As String = GetWeatherText()
        For Each b As modBusiness In CommonScreen.curBusiness
            If Not b.isInit OrElse Not b.hasWeather OrElse b.myWeather Is Nothing Then Continue For
            Dim target As Control = b.myWeather
            If target.IsDisposed Then Continue For
            If target.InvokeRequired Then
                Try
                    target.BeginInvoke(New Action(Of Control, String)(AddressOf SetWeatherText), target, text)
                Catch
                End Try
            Else
                SetWeatherText(target, text)
            End If
        Next
    End Sub

    Private Function GetWeatherText() As String
        SyncLock weatherLock
            Return weatherText
        End SyncLock
    End Function

    Private Function CreateWeatherControl(ByVal clock As Control) As Control
        If clock Is Nothing Then Return Nothing
        Return CreateWeatherControlOnParent(clock.Parent, clock)
    End Function

    Private Function CreateWeatherControlOnParent(ByVal parent As Control, Optional ByVal clock As Control = Nothing) As Control
        If parent Is Nothing Then Return Nothing

        Dim lbl As New Label()
        lbl.Name = "lblWeather"
        lbl.AutoSize = False
        ' Opaque tan — Transparent over a black panel punches a black hole through chrome.
        Dim tanFill As Color = ColorTranslator.FromHtml("#FFCC66")
        Dim fbParent As LCARS.Controls.FlatButton = TryCast(parent, LCARS.Controls.FlatButton)
        If fbParent IsNot Nothing Then
            Try
                tanFill = fbParent.ColorsAvailable.getColor(LCARS.LCARScolorStyles.StaticTan)
            Catch
            End Try
        End If
        lbl.BackColor = tanFill
        lbl.ForeColor = Color.Black
        lbl.Font = WeatherFont
        lbl.TextAlign = ContentAlignment.MiddleLeft
        lbl.Anchor = AnchorStyles.None
        lbl.Size = New Drawing.Size(160, WeatherHeight)
        If clock IsNot Nothing AndAlso clock.Parent Is parent Then
            Dim clockTextBottom As Integer = GetClockTextBottom(clock)
            Dim weatherTextHeight As Integer = TextRenderer.MeasureText("Ag", WeatherFont).Height
            lbl.Location = New Drawing.Point(Math.Max(0, clock.Left - 126), clockTextBottom - weatherTextHeight)
        Else
            lbl.Location = New Drawing.Point(8, 0)
        End If
        lbl.Text = GetWeatherText()
        If String.IsNullOrEmpty(lbl.Text) Then lbl.Text = "WX ..."

        parent.Controls.Add(lbl)
        lbl.BringToFront()
        Return lbl
    End Function

    Private Sub SetWeatherText(ByVal target As Control, ByVal text As String)
        If target Is Nothing OrElse target.IsDisposed Then Return
        target.Text = text
        Dim business As modBusiness = Nothing
        For Each b As modBusiness In CommonScreen.curBusiness
            If b.myWeather Is target Then
                business = b
                Exit For
            End If
        Next
        If business IsNot Nothing Then SyncWeatherLayout(business)
    End Sub

    Private Function IsWeatherEnabled() As Boolean
        Return String.Equals(GetSetting("LCARS x32", "Weather", "Enabled", "True"), "True", StringComparison.OrdinalIgnoreCase)
    End Function

    Private Function UseFahrenheit() As Boolean
        Return String.Equals(GetSetting("LCARS x32", "Weather", "UseFahrenheit", "True"), "True", StringComparison.OrdinalIgnoreCase)
    End Function

    Private Sub EnsureTls12()
        If tlsEnabled Then Return
        Try
            ServicePointManager.SecurityProtocol = DirectCast(Tls12, SecurityProtocolType)
            tlsEnabled = True
        Catch
        End Try
    End Sub

    Private Function FetchWeatherText() As String
        Dim latitude As Double = 0
        Dim longitude As Double = 0
        Dim placeLabel As String = ""

        If Not TryResolveCoordinates(latitude, longitude, placeLabel) Then
            Return "WX SET CITY"
        End If

        Dim unit As String = If(UseFahrenheit(), "fahrenheit", "celsius")
        Dim unitSymbol As String = If(UseFahrenheit(), "F", "C")
        Dim forecastUrl As String = String.Format(CultureInfo.InvariantCulture,
            "https://api.open-meteo.com/v1/forecast?latitude={0:0.####}&longitude={1:0.####}&current=temperature_2m,weather_code&daily=temperature_2m_max,temperature_2m_min&timezone=auto&temperature_unit={2}&forecast_days=1",
            latitude, longitude, unit)

        Dim json As String = DownloadUtf8(forecastUrl)
        If String.IsNullOrEmpty(json) Then Return "WX OFFLINE"

        Dim currentTemp As Double? = ExtractJsonNumber(json, "temperature_2m")
        Dim currentCode As Integer = CInt(ExtractJsonNumber(json, "weather_code").GetValueOrDefault(-1))
        Dim hiTemp As Double? = ExtractFirstArrayNumber(json, "temperature_2m_max")
        Dim loTemp As Double? = ExtractFirstArrayNumber(json, "temperature_2m_min")

        If Not currentTemp.HasValue Then Return "WX N/A"

        Dim tempText As String = CInt(Math.Round(currentTemp.Value)).ToString(CultureInfo.InvariantCulture) & unitSymbol
        Dim conditionText As String = WeatherCodeLabel(currentCode)
        Dim rangeText As String = ""
        If hiTemp.HasValue AndAlso loTemp.HasValue Then
            rangeText = " " & CInt(Math.Round(hiTemp.Value)).ToString(CultureInfo.InvariantCulture) &
                        "/" & CInt(Math.Round(loTemp.Value)).ToString(CultureInfo.InvariantCulture)
        End If

        If placeLabel <> "" Then
            Dim placeName As String = placeLabel.Split(","c)(0).Trim().ToUpperInvariant()
            Return placeName & " " & tempText & " " & conditionText & rangeText
        End If
        Return tempText & " " & conditionText & rangeText
    End Function

    Private Function ShortPlaceName(ByVal place As String) As String
        Return place.Split(","c)(0).Trim().ToUpperInvariant()
    End Function

    Private Function TryResolveCoordinates(ByRef latitude As Double, ByRef longitude As Double, ByRef placeLabel As String) As Boolean
        Dim savedLat As String = GetSetting("LCARS x32", "Weather", "Latitude", "").Trim()
        Dim savedLon As String = GetSetting("LCARS x32", "Weather", "Longitude", "").Trim()
        If savedLat <> "" AndAlso savedLon <> "" Then
            If Double.TryParse(savedLat, NumberStyles.Float, CultureInfo.InvariantCulture, latitude) AndAlso
               Double.TryParse(savedLon, NumberStyles.Float, CultureInfo.InvariantCulture, longitude) Then
                placeLabel = GetSetting("LCARS x32", "Weather", "City", "").Trim()
                Return True
            End If
        End If

        Dim city As String = GetSetting("LCARS x32", "Weather", "City", "").Trim()
        If city <> "" Then
            Dim latLon As Double() = Nothing
            If TryParseLatLon(city, latitude, longitude) Then
                placeLabel = ""
                Return True
            End If
            If TryGeocodeCity(city, latitude, longitude) Then
                placeLabel = city
                Return True
            End If
        End If

        Return TryGeolocateByIp(latitude, longitude, placeLabel)
    End Function

    Private Function TryParseLatLon(ByVal text As String, ByRef latitude As Double, ByRef longitude As Double) As Boolean
        Dim parts() As String = text.Split(","c)
        If parts.Length < 2 Then Return False
        Return Double.TryParse(parts(0).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, latitude) AndAlso
               Double.TryParse(parts(1).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, longitude) AndAlso
               latitude >= -90 AndAlso latitude <= 90 AndAlso longitude >= -180 AndAlso longitude <= 180
    End Function

    Private Function TryGeocodeCity(ByVal city As String, ByRef latitude As Double, ByRef longitude As Double) As Boolean
        Dim url As String = "https://geocoding-api.open-meteo.com/v1/search?name=" &
                            Uri.EscapeDataString(city) & "&count=1&language=en&format=json"
        Dim json As String = DownloadUtf8(url)
        If String.IsNullOrEmpty(json) Then Return False
        If json.IndexOf("""results""", StringComparison.OrdinalIgnoreCase) < 0 Then Return False

        Dim latMatch As Match = Regex.Match(json, """latitude""\s*:\s*(-?\d+(?:\.\d+)?)", RegexOptions.CultureInvariant)
        Dim lonMatch As Match = Regex.Match(json, """longitude""\s*:\s*(-?\d+(?:\.\d+)?)", RegexOptions.CultureInvariant)
        If Not latMatch.Success OrElse Not lonMatch.Success Then Return False

        Return Double.TryParse(latMatch.Groups(1).Value, NumberStyles.Float, CultureInfo.InvariantCulture, latitude) AndAlso
               Double.TryParse(lonMatch.Groups(1).Value, NumberStyles.Float, CultureInfo.InvariantCulture, longitude)
    End Function

    Private Function TryGeolocateByIp(ByRef latitude As Double, ByRef longitude As Double, ByRef placeLabel As String) As Boolean
        Dim json As String = DownloadUtf8("http://ip-api.com/json/?fields=status,lat,lon,city")
        If String.IsNullOrEmpty(json) Then Return False
        If json.IndexOf("""status"":""fail""", StringComparison.OrdinalIgnoreCase) >= 0 Then Return False

        Dim latMatch As Match = Regex.Match(json, """lat""\s*:\s*(-?\d+(?:\.\d+)?)", RegexOptions.CultureInvariant)
        Dim lonMatch As Match = Regex.Match(json, """lon""\s*:\s*(-?\d+(?:\.\d+)?)", RegexOptions.CultureInvariant)
        If Not latMatch.Success OrElse Not lonMatch.Success Then Return False

        Dim cityMatch As Match = Regex.Match(json, """city""\s*:\s*""([^""]*)""", RegexOptions.CultureInvariant)
        If cityMatch.Success Then placeLabel = cityMatch.Groups(1).Value.Trim()

        Return Double.TryParse(latMatch.Groups(1).Value, NumberStyles.Float, CultureInfo.InvariantCulture, latitude) AndAlso
               Double.TryParse(lonMatch.Groups(1).Value, NumberStyles.Float, CultureInfo.InvariantCulture, longitude)
    End Function

    Private Function DownloadUtf8(ByVal url As String) As String
        Using client As New WebClient()
            client.Encoding = Encoding.UTF8
            client.Headers.Add("User-Agent", "LCARS-x32-Weather/1.0")
            Return client.DownloadString(url)
        End Using
    End Function

    Private Function ExtractJsonNumber(ByVal json As String, ByVal key As String) As Double?
        Dim pattern As String = """" & Regex.Escape(key) & """\s*:\s*(-?\d+(?:\.\d+)?)"
        Dim match As Match = Regex.Match(json, pattern, RegexOptions.CultureInvariant)
        If Not match.Success Then Return Nothing

        Dim value As Double
        If Double.TryParse(match.Groups(1).Value, NumberStyles.Float, CultureInfo.InvariantCulture, value) Then
            Return value
        End If
        Return Nothing
    End Function

    Private Function ExtractFirstArrayNumber(ByVal json As String, ByVal key As String) As Double?
        Dim pattern As String = """" & Regex.Escape(key) & """\s*:\s*\[\s*(-?\d+(?:\.\d+)?)"
        Dim match As Match = Regex.Match(json, pattern, RegexOptions.CultureInvariant)
        If Not match.Success Then Return Nothing

        Dim value As Double
        If Double.TryParse(match.Groups(1).Value, NumberStyles.Float, CultureInfo.InvariantCulture, value) Then
            Return value
        End If
        Return Nothing
    End Function

    Private Function WeatherCodeLabel(ByVal code As Integer) As String
        Select Case code
            Case 0
                Return "CLR"
            Case 1
                Return "MCLR"
            Case 2
                Return "PTCLD"
            Case 3
                Return "OVCST"
            Case 45, 48
                Return "FOG"
            Case 51, 53, 55, 56, 57
                Return "DRIZ"
            Case 61, 63, 65, 66, 67
                Return "RAIN"
            Case 71, 73, 75, 77
                Return "SNOW"
            Case 80, 81, 82
                Return "SHWR"
            Case 85, 86
                Return "SSNOW"
            Case 95, 96, 99
                Return "TSTM"
            Case Else
                Return "WX"
        End Select
    End Function
End Module
