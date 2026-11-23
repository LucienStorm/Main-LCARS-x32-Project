Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Globalization
Imports System.Threading
Imports System.Windows.Forms
Imports LCARS
Imports LCARS.Controls
Imports Microsoft.VisualBasic.CompilerServices

    Public Class frmWeather
        Inherits LCARSForm
        Private Const FormWidth As Integer = 800

        Private Const FormHeight As Integer = 640

        Private Const HourStripHeight As Integer = 118

        Private Const DayStripHeight As Integer = 168

        Private Const HourCardWidth As Integer = 78

        Private Const DayCardWidth As Integer = 150

        Private Const CardGap As Integer = 6

        Private lblPlace As Label

        Private lblCurrent As Label

        Private lblCondition As Label

        Private lblDetails As Label

        Private lblStatus As Label

        Private lblHourlyHeader As Label

        Private lblDailyHeader As Label

        Private scrollHours As LcarsHScrollPanel

        Private scrollDays As LcarsHScrollPanel

        Private loadBusy As Boolean

        Private lastSnap As WeatherClient.ForecastSnapshot

        Public Sub New()
            loadBusy = False
            lastSnap = Nothing
            modUtilities.InitUtilityFormFloating(Me, 800, 640)
            BuildUi()
            AddHandler Shown, Sub(s, e) BeginRefresh()
            AddHandler Resize, Sub(s, e) LayoutUi()
        End Sub

        Private Sub BuildUi()
            Dim num = 12
            Dim num2 = 800 - num * 2
            modUtilities.MakeUtilityTitle(Me, "WEATHER", num2 - 360)
            modUtilities.MakeUtilityButton(Me, "REFRESH", 800 - num - 350, num, 110, 28, LCARScolorStyles.PrimaryFunction, Sub(s, e) BeginRefresh())
            modUtilities.MakeUtilityButton(Me, "MAP", 800 - num - 230, num, 100, 28, LCARScolorStyles.NavigationFunction, Sub(s, e) OpenMap())
            modUtilities.MakeUtilityButton(Me, "CLOSE", 800 - num - 120, num, 110, 28, LCARScolorStyles.FunctionOffline, Sub(s, e) Close())
            lblPlace = modUtilities.MakeUtilityLabel(Me, num, 48, num2, 24, 16F)
            lblPlace.Text = "LOCATING…"
            lblCurrent = modUtilities.MakeUtilityLabel(Me, num, 74, 220, 48, 36F)
            lblCurrent.Text = "—"
            lblCondition = modUtilities.MakeUtilityLabel(Me, num + 230, 78, num2 - 230, 40, 14F)
            lblCondition.Text = ""
            lblDetails = modUtilities.MakeUtilityLabel(Me, num, 128, num2, 22, 12F)
            lblDetails.Text = ""
            lblStatus = modUtilities.MakeUtilityLabel(Me, num, 152, num2, 18, 11F)
            lblStatus.Text = ""
            lblHourlyHeader = modUtilities.MakeUtilityLabel(Me, num, 178, num2, 18, 11F)
            lblHourlyHeader.Text = "HOURLY — SWIPE"
            scrollHours = New LcarsHScrollPanel()
            Controls.Add(scrollHours)
            lblDailyHeader = modUtilities.MakeUtilityLabel(Me, num, 326, num2, 18, 11F)
            lblDailyHeader.Text = "7-DAY — SWIPE"
            scrollDays = New LcarsHScrollPanel()
            Controls.Add(scrollDays)
            LayoutUi()
        End Sub

        Private Sub LayoutUi()
            Dim num = 12
            Dim num2 = Math.Max(320, ClientSize.Width - num * 2)
            lblPlace.Width = num2
            lblCondition.Width = Math.Max(120, num2 - 230)
            lblDetails.Width = num2
            lblStatus.Width = num2
            lblHourlyHeader.Width = num2
            lblDailyHeader.Width = num2
            Dim num3 = 198
            scrollHours.SetBounds(num, num3, num2, 118)
            Dim num4 = num3 + 118 + 28
            lblDailyHeader.Top = num4 - 20
            Dim num5 = Math.Max(168, ClientSize.Height - num4 - num)
            If num5 > 208 Then
                num5 = 208
            End If
            scrollDays.SetBounds(num, num4, num2, num5)
            RelayoutHourCards()
            RelayoutDayCards()
        End Sub

        Private Sub BeginRefresh()
            If Not loadBusy Then
                loadBusy = True
                lblStatus.Text = "LOADING…"
                Call ThreadPool.QueueUserWorkItem(New WaitCallback(AddressOf RefreshWorker))
            End If
        End Sub

        Private Sub RefreshWorker(state As Object)
            Dim forecastSnapshot As WeatherClient.ForecastSnapshot = Nothing
            Try
                forecastSnapshot = WeatherClient.FetchForecast()
            Catch ex As Exception
                ProjectData.SetProjectError(ex)
                Dim ex2 = ex
                forecastSnapshot = New WeatherClient.ForecastSnapshot()
                forecastSnapshot.ErrorMessage = ex2.Message
                Call ProjectData.ClearProjectError()
            End Try
            If Not IsDisposed Then
                Dim snap = forecastSnapshot
                BeginInvoke(CType(Sub() ApplySnapshot(snap), MethodInvoker))
            End If
        End Sub

        Private Sub ApplySnapshot(snap As WeatherClient.ForecastSnapshot)
            loadBusy = False
            lastSnap = snap
            If snap Is Nothing Then
                lblStatus.Text = "ERROR"
                Return
            End If
            If Not snap.Ok Then
                lblPlace.Text = "WEATHER"
                lblCurrent.Text = "—"
                lblCondition.Text = ""
                lblDetails.Text = ""
                lblStatus.Text = snap.ErrorMessage.ToUpperInvariant()
                scrollHours.ContentPanel.Controls.Clear()
                scrollDays.ContentPanel.Controls.Clear()
                scrollHours.Relayout()
                scrollDays.Relayout()
                Return
            End If
            Dim text = snap.PlaceLabel
            If Operators.CompareString(text, "", TextCompare:=False) = 0 Then
                text = String.Format(CultureInfo.InvariantCulture, "{0:0.####}, {1:0.####}", snap.Latitude, snap.Longitude)
            End If
            lblPlace.Text = text.ToUpperInvariant()
            If snap.CurrentTemp.HasValue Then
                lblCurrent.Text = CInt(Math.Round(Math.Round(snap.CurrentTemp.Value))).ToString(CultureInfo.InvariantCulture) & "°" & snap.UnitSymbol
            Else
                lblCurrent.Text = "—"
            End If
            Dim text2 = snap.CurrentCondition
            If snap.FeelsLike.HasValue Then
                text2 = text2 & "  ·  FEELS " & CInt(Math.Round(Math.Round(snap.FeelsLike.Value))).ToString(CultureInfo.InvariantCulture) & "°" & snap.UnitSymbol
            End If
            lblCondition.Text = text2
            Dim list As List(Of String) = New List(Of String)()
            If snap.Humidity.HasValue Then
                list.Add("HUM " & CInt(Math.Round(Math.Round(snap.Humidity.Value))).ToString(CultureInfo.InvariantCulture) & "%")
            End If
            If snap.WindSpeed.HasValue Then
                list.Add("WIND " & CInt(Math.Round(Math.Round(snap.WindSpeed.Value))).ToString(CultureInfo.InvariantCulture))
            End If
            If snap.PrecipCurrent.HasValue Then
                list.Add("PRECIP " & snap.PrecipCurrent.Value.ToString("0.#", CultureInfo.InvariantCulture))
            End If
            lblDetails.Text = String.Join("  ·  ", list.ToArray())
            lblStatus.Text = "OPEN-METEO  ·  " & Date.Now.ToString("HH:mm", CultureInfo.InvariantCulture)
            BuildHourCards(snap)
            BuildDayCards(snap)
        End Sub

        Private Sub OpenMap()
            Dim num = 0.0
            Dim num2 = 0.0
            If lastSnap IsNot Nothing AndAlso lastSnap.Ok Then
                num = lastSnap.Latitude
                num2 = lastSnap.Longitude
            Else
                Dim locationFix As WeatherClient.LocationFix = WeatherClient.ResolveLocation()
                If Not locationFix.Ok Then
                    lblStatus.Text = locationFix.ErrorMessage.ToUpperInvariant()
                    Return
                End If
                num = locationFix.Latitude
                num2 = locationFix.Longitude
            End If
            modUtilities.OpenUrlInLcarsBrowser(WeatherClient.OpenStreetMapUrl(num, num2))
            lblStatus.Text = "MAP OPENED IN BROWSER."
        End Sub

        Private Sub BuildHourCards(snap As WeatherClient.ForecastSnapshot)
            scrollHours.ContentPanel.Controls.Clear()
            Dim unitSymbol = snap.UnitSymbol
            Dim num = Math.Max(70, 86)
            For Each hour In snap.Hours
                Dim panel As Panel = New Panel()
                panel.BackColor = Color.FromArgb(16, 16, 16)
                panel.BorderStyle = BorderStyle.FixedSingle
                Dim size As Size = New Size(78, num)
                panel.Size = size
                Dim label As Label = New Label()
                label.ForeColor = Color.Orange
                label.BackColor = Color.Transparent
                label.Font = New Font("Arial Narrow", 10F, FontStyle.Bold)
                label.Text = hour.TimeLabel
                label.SetBounds(4, 4, 70, 16)
                panel.Controls.Add(label)
                Dim label2 As Label = New Label()
                label2.ForeColor = Color.FromArgb(255, 204, 102)
                label2.BackColor = Color.Transparent
                label2.Font = New Font("Arial Narrow", 14F, FontStyle.Bold)
                label2.Text = If(hour.Temp.HasValue, CInt(Math.Round(Math.Round(hour.Temp.Value))).ToString(CultureInfo.InvariantCulture) & "°" & unitSymbol, "—")
                label2.SetBounds(4, 24, 70, 22)
                panel.Controls.Add(label2)
                Dim label3 As Label = New Label()
                label3.ForeColor = Color.Orange
                label3.BackColor = Color.Transparent
                label3.Font = New Font("Arial Narrow", 8F, FontStyle.Regular)
                Dim text = hour.Condition
                If hour.PrecipProbability.HasValue Then
                    text = text & " " & CInt(Math.Round(Math.Round(hour.PrecipProbability.Value))).ToString(CultureInfo.InvariantCulture) & "%"
                End If
                label3.Text = text
                label3.SetBounds(4, 50, 70, Math.Max(20, num - 54))
                panel.Controls.Add(label3)
                scrollHours.ContentPanel.Controls.Add(panel)
            Next
            RelayoutHourCards()
            scrollHours.HookScrollGesture(scrollHours.ContentPanel)
        End Sub

        Private Sub BuildDayCards(snap As WeatherClient.ForecastSnapshot)
            scrollDays.ContentPanel.Controls.Clear()
            Dim unitSymbol = snap.UnitSymbol
            Dim num = Math.Max(110, 136)
            For Each day In snap.Days
                Dim panel As Panel = New Panel()
                panel.BackColor = Color.FromArgb(16, 16, 16)
                panel.BorderStyle = BorderStyle.FixedSingle
                Dim size As Size = New Size(150, num)
                panel.Size = size
                Dim label As Label = New Label()
                label.ForeColor = Color.Orange
                label.BackColor = Color.Transparent
                label.Font = New Font("Arial Narrow", 11F, FontStyle.Bold)
                label.Text = day.DateLabel
                label.SetBounds(8, 6, 134, 20)
                panel.Controls.Add(label)
                Dim label2 As Label = New Label()
                label2.ForeColor = Color.FromArgb(255, 204, 102)
                label2.BackColor = Color.Transparent
                label2.Font = New Font("Arial Narrow", 10F, FontStyle.Regular)
                label2.Text = day.Condition
                label2.SetBounds(8, 28, 134, 28)
                panel.Controls.Add(label2)
                Dim label3 As Label = New Label()
                label3.ForeColor = Color.Orange
                label3.BackColor = Color.Transparent
                label3.Font = New Font("Arial Narrow", 14F, FontStyle.Bold)
                Dim text = If(day.High.HasValue, CInt(Math.Round(Math.Round(day.High.Value))).ToString(CultureInfo.InvariantCulture), "—")
                Dim text2 = If(day.Low.HasValue, CInt(Math.Round(Math.Round(day.Low.Value))).ToString(CultureInfo.InvariantCulture), "—")
                label3.Text = text & "° / " & text2 & "°" & unitSymbol
                label3.SetBounds(8, 60, 134, 24)
                panel.Controls.Add(label3)
                Dim label4 As Label = New Label()
                label4.ForeColor = Color.FromArgb(200, 160, 80)
                label4.BackColor = Color.Transparent
                label4.Font = New Font("Arial Narrow", 9F, FontStyle.Regular)
                Dim list As List(Of String) = New List(Of String)()
                If day.PrecipSum.HasValue Then
                    list.Add("P " & day.PrecipSum.Value.ToString("0.#", CultureInfo.InvariantCulture))
                End If
                If day.WindMax.HasValue Then
                    list.Add("W " & CInt(Math.Round(Math.Round(day.WindMax.Value))).ToString(CultureInfo.InvariantCulture))
                End If
                label4.Text = String.Join(" · ", list.ToArray())
                label4.SetBounds(8, 88, 134, 18)
                panel.Controls.Add(label4)
                scrollDays.ContentPanel.Controls.Add(panel)
            Next
            RelayoutDayCards()
            scrollDays.HookScrollGesture(scrollDays.ContentPanel)
        End Sub

        Private Sub RelayoutHourCards()
            If scrollHours Is Nothing Then
                Return
            End If
            Dim num = 0
            Dim num2 = Math.Max(70, scrollHours.Height - 28 - 4)
            For Each control As Control In scrollHours.ContentPanel.Controls
                If TypeOf control Is Panel Then
                    control.SetBounds(num, 0, 78, num2)
                    num += 84
                End If
            Next
            scrollHours.Relayout()
        End Sub

        Private Sub RelayoutDayCards()
            If scrollDays Is Nothing Then
                Return
            End If
            Dim num = 0
            Dim num2 = Math.Max(110, scrollDays.Height - 28 - 4)
            For Each control As Control In scrollDays.ContentPanel.Controls
                If TypeOf control Is Panel Then
                    control.SetBounds(num, 0, 150, num2)
                    num += 156
                End If
            Next
            scrollDays.Relayout()
        End Sub
    End Class
