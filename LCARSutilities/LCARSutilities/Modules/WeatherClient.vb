Imports System
Imports System.Collections.Generic
Imports System.Globalization
Imports System.Net
Imports System.Text
Imports System.Text.RegularExpressions
Imports Microsoft.VisualBasic
Imports Microsoft.VisualBasic.CompilerServices

    <StandardModule>
    Public NotInheritable Class WeatherClient
        Public Class HourForecast
            Public TimeLabel As String

            Public Temp As Double?

            Public WeatherCode As Integer

            Public Condition As String

            Public PrecipProbability As Double?

            Public Sub New()
                TimeLabel = ""
                WeatherCode = -1
                Condition = ""
            End Sub
        End Class

        Public Class DayForecast
            Public DateLabel As String

            Public WeatherCode As Integer

            Public High As Double?

            Public Low As Double?

            Public Condition As String

            Public PrecipSum As Double?

            Public WindMax As Double?

            Public Sub New()
                DateLabel = ""
                WeatherCode = -1
                Condition = ""
            End Sub
        End Class

        Public Class ForecastSnapshot
            Public Ok As Boolean

            Public ErrorMessage As String

            Public PlaceLabel As String

            Public Latitude As Double

            Public Longitude As Double

            Public UnitSymbol As String

            Public CurrentTemp As Double?

            Public FeelsLike As Double?

            Public Humidity As Double?

            Public WindSpeed As Double?

            Public PrecipCurrent As Double?

            Public CurrentCode As Integer

            Public CurrentCondition As String

            Public Hours As List(Of HourForecast)

            Public Days As List(Of DayForecast)

            Public Sub New()
                ErrorMessage = ""
                PlaceLabel = ""
                UnitSymbol = "F"
                CurrentCode = -1
                CurrentCondition = ""
                Hours = New List(Of HourForecast)()
                Days = New List(Of DayForecast)()
            End Sub
        End Class

        Public Class LocationFix
            Public Ok As Boolean

            Public Latitude As Double

            Public Longitude As Double

            Public PlaceLabel As String

            Public ErrorMessage As String

            Public Sub New()
                PlaceLabel = ""
                ErrorMessage = ""
            End Sub
        End Class

        Private Const Tls12 As Integer = 3072

        Private Shared tlsEnabled As Boolean = False

        Public Shared Function ResolveLocation() As LocationFix
            Dim locationFix As LocationFix = New LocationFix()
            Dim latitude = 0.0
            Dim longitude = 0.0
            Dim placeLabel = ""
            If TryResolveCoordinates(latitude, longitude, placeLabel) Then
                locationFix.Ok = True
                locationFix.Latitude = latitude
                locationFix.Longitude = longitude
                locationFix.PlaceLabel = placeLabel
            Else
                locationFix.ErrorMessage = "Set Weather city/lat-lon in LCARS Settings (same as HUD)."
            End If
            Return locationFix
        End Function

        Public Shared Function FetchForecast(Optional forecastDays As Integer = 7) As ForecastSnapshot
            Call EnsureTls12()
            Dim forecastSnapshot As ForecastSnapshot = New ForecastSnapshot()
            Dim locationFix As LocationFix = ResolveLocation()
            Dim result As ForecastSnapshot
            If Not locationFix.Ok Then
                forecastSnapshot.ErrorMessage = locationFix.ErrorMessage
                result = forecastSnapshot
            Else
                forecastSnapshot.Latitude = locationFix.Latitude
                forecastSnapshot.Longitude = locationFix.Longitude
                forecastSnapshot.PlaceLabel = locationFix.PlaceLabel
                forecastSnapshot.UnitSymbol = (If(UseFahrenheit(), "F", "C"))
                Dim text As String = (If(UseFahrenheit(), "fahrenheit", "celsius"))
                If forecastDays < 1 Then
                    forecastDays = 1
                End If
                If forecastDays > 16 Then
                    forecastDays = 16
                End If
                Dim url = String.Format(CultureInfo.InvariantCulture, "https://api.open-meteo.com/v1/forecast?latitude={0:0.####}&longitude={1:0.####}&current=temperature_2m,apparent_temperature,weather_code,relative_humidity_2m,wind_speed_10m,precipitation&hourly=temperature_2m,weather_code,precipitation_probability&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_sum,wind_speed_10m_max&timezone=auto&temperature_unit={2}&forecast_days={3}", locationFix.Latitude, locationFix.Longitude, text, forecastDays)
                Dim text2 = ""
                Try
                    text2 = DownloadUtf8(url)
                Catch ex As Exception
                    ProjectData.SetProjectError(ex)
                    Dim ex2 = ex
                    forecastSnapshot.ErrorMessage = "Offline: " & ex2.Message
                    result = forecastSnapshot
                    Call ProjectData.ClearProjectError()
                    GoTo IL_0264
                End Try
                If String.IsNullOrEmpty(text2) Then
                    forecastSnapshot.ErrorMessage = "Offline — no forecast data."
                    result = forecastSnapshot
                Else
                    forecastSnapshot.CurrentTemp = ExtractJsonNumberNear(text2, "current", "temperature_2m")
                    forecastSnapshot.FeelsLike = ExtractJsonNumberNear(text2, "current", "apparent_temperature")
                    forecastSnapshot.Humidity = ExtractJsonNumberNear(text2, "current", "relative_humidity_2m")
                    forecastSnapshot.WindSpeed = ExtractJsonNumberNear(text2, "current", "wind_speed_10m")
                    forecastSnapshot.PrecipCurrent = ExtractJsonNumberNear(text2, "current", "precipitation")
                    forecastSnapshot.CurrentCode = CInt(Math.Round(If(ExtractJsonNumberNear(text2, "current", "weather_code"), -1.0)))
                    forecastSnapshot.CurrentCondition = WeatherCodeLabel(forecastSnapshot.CurrentCode)
                    ParseHourly(text2, forecastSnapshot)
                    ParseDaily(text2, forecastSnapshot, forecastDays)
                    forecastSnapshot.Ok = If(forecastSnapshot.CurrentTemp.HasValue OrElse forecastSnapshot.Days.Count > 0 OrElse forecastSnapshot.Hours.Count > 0, True, False)
                    If Not forecastSnapshot.Ok Then
                        forecastSnapshot.ErrorMessage = "No weather data."
                    End If
                    result = forecastSnapshot
                End If
            End If
            GoTo IL_0264
IL_0264:
            Return result
        End Function

        Public Shared Function OpenStreetMapUrl(latitude As Double, longitude As Double, Optional zoom As Integer = 12) As String
            If zoom < 1 Then
                zoom = 1
            End If
            If zoom > 19 Then
                zoom = 19
            End If
            Return String.Format(CultureInfo.InvariantCulture, "https://www.openstreetmap.org/?mlat={0:0.#####}&mlon={1:0.#####}#map={2}/{0:0.#####}/{1:0.#####}", latitude, longitude, zoom)
        End Function

        Public Shared Function WeatherCodeLabel(code As Integer) As String
            Select Case code
                Case 0
                    Return "CLEAR"
                Case 1
                    Return "MOSTLY CLEAR"
                Case 2
                    Return "PARTLY CLOUDY"
                Case 3
                    Return "OVERCAST"
                Case 45, 48
                    Return "FOG"
                Case 51, 53, 55, 56, 57
                    Return "DRIZZLE"
                Case 61, 63, 65, 66, 67
                    Return "RAIN"
                Case 71, 73, 75, 77
                    Return "SNOW"
                Case 80, 81, 82
                    Return "SHOWERS"
                Case 85, 86
                    Return "SNOW SHOWERS"
                Case 95, 96, 99
                    Return "THUNDERSTORM"
                Case Else
                    Return "—"
            End Select
        End Function

        Private Shared Sub ParseHourly(json As String, snap As ForecastSnapshot)
            Dim num = json.IndexOf("""hourly""", StringComparison.OrdinalIgnoreCase)
            If num < 0 Then
                Return
            End If
            Dim result As Date = Nothing
            Dim text = json.Substring(num, Math.Min(json.Length - num, 120000))
            Dim num2 = text.IndexOf("""daily""", StringComparison.OrdinalIgnoreCase)
            If num2 > 0 Then
                text = text.Substring(0, num2)
            End If
            Dim list = ExtractStringArray(text, "time")
            Dim list2 = ExtractNumberArray(text, "temperature_2m")
            Dim list3 = ExtractNumberArray(text, "weather_code")
            Dim list4 = ExtractNumberArray(text, "precipitation_probability")
            Dim now = Date.Now
            Dim num3 = 0
            Dim num4 = list.Count - 1
            For i = 0 To num4

                If Date.TryParse(list(i), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, result) AndAlso Date.Compare(result, now.AddMinutes(-30.0)) >= 0 Then
                    Dim hourForecast As HourForecast = New HourForecast()
                    hourForecast.TimeLabel = result.ToString("HH:mm", CultureInfo.InvariantCulture)
                    If i < list2.Count Then
                        hourForecast.Temp = list2(i)
                    End If
                    If i < list3.Count Then
                        hourForecast.WeatherCode = CInt(Math.Round(list3(i)))
                        hourForecast.Condition = WeatherCodeLabel(hourForecast.WeatherCode)
                    End If
                    If i < list4.Count Then
                        hourForecast.PrecipProbability = list4(i)
                    End If
                    snap.Hours.Add(hourForecast)
                    num3 += 1
                    If num3 >= 24 Then
                        Exit For
                    End If
                End If
            Next
        End Sub

        Private Shared Sub ParseDaily(json As String, snap As ForecastSnapshot, forecastDays As Integer)
            Dim num = json.IndexOf("""daily""", StringComparison.OrdinalIgnoreCase)
            If num < 0 Then
                Return
            End If
            Dim json2 = json.Substring(num, Math.Min(json.Length - num, 60000))
            Dim list = ExtractStringArray(json2, "time")
            Dim list2 = ExtractNumberArray(json2, "weather_code")
            Dim list3 = ExtractNumberArray(json2, "temperature_2m_max")
            Dim list4 = ExtractNumberArray(json2, "temperature_2m_min")
            Dim list5 = ExtractNumberArray(json2, "precipitation_sum")
            Dim list6 = ExtractNumberArray(json2, "wind_speed_10m_max")
            Dim num2 = Math.Min(forecastDays, Math.Min(list3.Count, list4.Count))
            If list.Count > 0 Then
                num2 = Math.Min(num2, list.Count)
            End If
            Dim num3 = num2 - 1
            For i = 0 To num3
                Dim dayForecast As DayForecast = New DayForecast()
                If i < list.Count Then
                    dayForecast.DateLabel = FormatDayLabel(list(i))
                Else
                    dayForecast.DateLabel = "DAY " & (i + 1).ToString(CultureInfo.InvariantCulture)
                End If
                If i < list3.Count Then
                    dayForecast.High = list3(i)
                End If
                If i < list4.Count Then
                    dayForecast.Low = list4(i)
                End If
                If i < list5.Count Then
                    dayForecast.PrecipSum = list5(i)
                End If
                If i < list6.Count Then
                    dayForecast.WindMax = list6(i)
                End If
                If i < list2.Count Then
                    dayForecast.WeatherCode = CInt(Math.Round(list2(i)))
                    dayForecast.Condition = WeatherCodeLabel(dayForecast.WeatherCode)
                End If
                snap.Days.Add(dayForecast)
            Next
        End Sub

        Private Shared Function UseFahrenheit() As Boolean
            Return String.Equals(GetSetting("LCARS x32", "Weather", "UseFahrenheit", "True"), "True", StringComparison.OrdinalIgnoreCase)
        End Function

        Private Shared Function TryResolveCoordinates(ByRef latitude As Double, ByRef longitude As Double, ByRef placeLabel As String) As Boolean
            Dim text As String = GetSetting("LCARS x32", "Weather", "Latitude").Trim()
            Dim text2 As String = GetSetting("LCARS x32", "Weather", "Longitude").Trim()
            If Operators.CompareString(text, "", TextCompare:=False) <> 0 AndAlso Operators.CompareString(text2, "", TextCompare:=False) <> 0 AndAlso Double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, latitude) AndAlso Double.TryParse(text2, NumberStyles.Float, CultureInfo.InvariantCulture, longitude) Then
                placeLabel = GetSetting("LCARS x32", "Weather", "City").Trim()
                Return True
            End If
            Dim text3 As String = GetSetting("LCARS x32", "Weather", "City").Trim()
            If Operators.CompareString(text3, "", TextCompare:=False) <> 0 Then
                If TryParseLatLon(text3, latitude, longitude) Then
                    placeLabel = ""
                    Return True
                End If
                If TryGeocodeCity(text3, latitude, longitude) Then
                    placeLabel = text3
                    Return True
                End If
            End If
            Return TryGeolocateByIp(latitude, longitude, placeLabel)
        End Function

        Private Shared Function TryParseLatLon(text As String, ByRef latitude As Double, ByRef longitude As Double) As Boolean
            Dim array = text.Split(","c)
            If array.Length < 2 Then
                Return False
            End If
            Return If((Double.TryParse(array(0).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, latitude) AndAlso Double.TryParse(array(1).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, longitude) AndAlso latitude >= -90.0 AndAlso latitude <= 90.0 AndAlso longitude >= -180.0 AndAlso longitude <= 180.0), True, False)
        End Function

        Private Shared Function TryGeocodeCity(city As String, ByRef latitude As Double, ByRef longitude As Double) As Boolean
            Dim url = "https://geocoding-api.open-meteo.com/v1/search?name=" & Uri.EscapeDataString(city) & "&count=1&language=en&format=json"
            Dim text = DownloadUtf8(url)
            If String.IsNullOrEmpty(text) Then
                Return False
            End If
            If text.IndexOf("""results""", StringComparison.OrdinalIgnoreCase) < 0 Then
                Return False
            End If
            Dim match = Regex.Match(text, """latitude""\s*:\s*(-?\d+(?:\.\d+)?)", RegexOptions.CultureInvariant)
            Dim match2 = Regex.Match(text, """longitude""\s*:\s*(-?\d+(?:\.\d+)?)", RegexOptions.CultureInvariant)
            If Not match.Success OrElse Not match2.Success Then
                Return False
            End If
            Return If(Double.TryParse(match.Groups(1).Value, NumberStyles.Float, CultureInfo.InvariantCulture, latitude) AndAlso Double.TryParse(match2.Groups(1).Value, NumberStyles.Float, CultureInfo.InvariantCulture, longitude), True, False)
        End Function

        Private Shared Function TryGeolocateByIp(ByRef latitude As Double, ByRef longitude As Double, ByRef placeLabel As String) As Boolean
            Dim text = DownloadUtf8("http://ip-api.com/json/?fields=status,lat,lon,city")
            If String.IsNullOrEmpty(text) Then
                Return False
            End If
            If text.IndexOf("""status"":""fail""", StringComparison.OrdinalIgnoreCase) >= 0 Then
                Return False
            End If
            Dim match = Regex.Match(text, """lat""\s*:\s*(-?\d+(?:\.\d+)?)", RegexOptions.CultureInvariant)
            Dim match2 = Regex.Match(text, """lon""\s*:\s*(-?\d+(?:\.\d+)?)", RegexOptions.CultureInvariant)
            If Not match.Success OrElse Not match2.Success Then
                Return False
            End If
            Dim match3 = Regex.Match(text, """city""\s*:\s*""([^""]*)""", RegexOptions.CultureInvariant)
            If match3.Success Then
                placeLabel = match3.Groups(1).Value.Trim()
            End If
            Return If(Double.TryParse(match.Groups(1).Value, NumberStyles.Float, CultureInfo.InvariantCulture, latitude) AndAlso Double.TryParse(match2.Groups(1).Value, NumberStyles.Float, CultureInfo.InvariantCulture, longitude), True, False)
        End Function

        Private Shared Sub EnsureTls12()
            If tlsEnabled Then
                Return
            End If
            Try
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
                tlsEnabled = True
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
        End Sub

        Private Shared Function DownloadUtf8(url As String) As String
            Call EnsureTls12()
            Dim webClient As WebClient = New WebClient()
            webClient.Encoding = Encoding.UTF8
            webClient.Headers.Add("User-Agent", "LCARS-x32-Utilities/1.0")
            Return webClient.DownloadString(url)
        End Function

        Private Shared Function ExtractJsonNumber(json As String, key As String) As Double?
            Dim pattern = """" & Regex.Escape(key) & """\s*:\s*(-?\d+(?:\.\d+)?)"
            Dim match = Regex.Match(json, pattern, RegexOptions.CultureInvariant)
            Dim result As Double? = Nothing
            If Not match.Success Then
                Return result
            End If
            Dim result2 As Double = Nothing

            If Double.TryParse(match.Groups(1).Value, NumberStyles.Float, CultureInfo.InvariantCulture, result2) Then
                Return result2
            End If
            Return result
        End Function

        Private Shared Function ExtractJsonNumberNear(json As String, section As String, key As String) As Double?
            Dim num = json.IndexOf("""" & section & """", StringComparison.OrdinalIgnoreCase)
            Dim result As Double? = Nothing
            If num < 0 Then
                Return result
            End If
            Dim json2 = json.Substring(num, Math.Min(1200, json.Length - num))
            Return ExtractJsonNumber(json2, key)
        End Function

        Private Shared Function ExtractNumberArray(json As String, key As String) As List(Of Double)
            Dim list As List(Of Double) = New List(Of Double)()
            Dim pattern = """" & Regex.Escape(key) & """\s*:\s*\[([^\]]*)\]"
            Dim match = Regex.Match(json, pattern, RegexOptions.CultureInvariant)
            If Not match.Success Then
                Return list
            End If
            Dim array = match.Groups(1).Value.Split(","c)
            Dim result As Double = Nothing
            For Each text In array
                Dim text2 As String = text.Trim()
                Select Case text2
                    Case Nothing, "", "null"
                        Continue For
                End Select

                If Double.TryParse(text2, NumberStyles.Float, CultureInfo.InvariantCulture, result) Then
                    list.Add(result)
                End If
            Next
            Return list
        End Function

        Private Shared Function ExtractStringArray(json As String, key As String) As List(Of String)
            Dim list As List(Of String) = New List(Of String)()
            Dim pattern = """" & Regex.Escape(key) & """\s*:\s*\[([^\]]*)\]"
            Dim match = Regex.Match(json, pattern, RegexOptions.CultureInvariant)
            If Not match.Success Then
                Return list
            End If
            For Each item As Match In Regex.Matches(match.Groups(1).Value, """([^""]*)""")
                list.Add(item.Groups(1).Value)
            Next
            Return list
        End Function

        Private Shared Function FormatDayLabel(isoDate As String) As String
            Dim result As Date = Nothing

            If Date.TryParse(isoDate, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, result) Then
                Return result.ToString("ddd M/d", CultureInfo.InvariantCulture).ToUpperInvariant()
            End If
            Return isoDate
        End Function
    End Class
