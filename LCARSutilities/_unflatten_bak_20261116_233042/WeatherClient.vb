' LCARSutilities/LCARSutilities/Modules/WeatherClient.vb
Option Strict On
Option Explicit On
Imports
System.Globalization
Imports
System.Net
Imports
System.Text
Imports
System.Text.RegularExpressions
Imports
Microsoft.VisualBasic
''' <summary>
''' Open-Meteo client
for the Weather utility. Uses the same registry settings as the shell HUD.
''' </summary>
Public Module WeatherClient
Private Const Tls12 As Integer = 3072
Private tlsEnabled As Boolean = False
Public Class HourForecast
Public TimeLabel As String = ""
Public Temp As Double?
Public WeatherCode As Integer = -1
Public Condition As String = ""
Public PrecipProbability As Double?
End Class
Public Class DayForecast
Public DateLabel As String = ""
Public WeatherCode As Integer = -1
Public High As Double?
Public Low As Double?
Public Condition As String = ""
Public PrecipSum As Double?
Public WindMax As Double?
End Class
Public Class ForecastSnapshot
Public Ok As Boolean
Public ErrorMessage As String = ""
Public PlaceLabel As String = ""
Public Latitude As Double
Public Longitude As Double
Public UnitSymbol As String = "F"
Public CurrentTemp As Double?
Public FeelsLike As Double?
Public Humidity As Double?
Public WindSpeed As Double?
Public PrecipCurrent As Double?
Public CurrentCode As Integer = -1
Public CurrentCondition As String = ""
Public Hours As New System.Collections.Generic.List(Of HourForecast)()
Public Days As New System.Collections.Generic.List(Of DayForecast)()
End Class
Public Class LocationFix
Public Ok As Boolean
Public Latitude As Double
Public Longitude As Double
Public PlaceLabel As String = ""
Public ErrorMessage As String = ""
End Class
Public Function ResolveLocation() As LocationFix
Dim fix As New LocationFix()
Dim lat As Double = 0
Dim lon As Double = 0
Dim place As String = ""
If TryResolveCoordinates(lat, lon, place) Then
fix.Ok = True
fix.Latitude = lat
fix.Longitude = lon
fix.PlaceLabel = place
Else fix.ErrorMessage = "Set Weather city/lat-lon in LCARS Settings (same as HUD)."
End If
Return fix
End Function
Public Function FetchForecast(Optional ByVal forecastDays As Integer = 7) As ForecastSnapshot EnsureTls12()
Dim snap As New ForecastSnapshot()
Dim fix As LocationFix = ResolveLocation()
If Not fix.Ok Then snap.ErrorMessage = fix.ErrorMessage
Return snap
End If
snap.Latitude = fix.Latitude
snap.Longitude = fix.Longitude
snap.PlaceLabel = fix.PlaceLabel
snap.UnitSymbol = If(UseFahrenheit(), "F", "C")
Dim unit As String = If(UseFahrenheit(), "fahrenheit", "celsius")
If forecastDays < 1 Then forecastDays = 1
If forecastDays > 16 Then forecastDays = 16 ' Match HUD-valid daily keys (never include "time" — Open-Meteo returns 400).
Dim url As String = String.Format(CultureInfo.InvariantCulture, "https://api.open-meteo.com/v1/forecast?latitude={0:0.####}&longitude={1:0.####}" & "&current=temperature_2m,apparent_temperature,weather_code,relative_humidity_2m,wind_speed_10m,precipitation" & "&hourly=temperature_2m,weather_code,precipitation_probability" & "&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_sum,wind_speed_10m_max" & "&timezone=auto&temperature_unit={2}&forecast_days={3}", fix.Latitude, fix.Longitude, unit, forecastDays)
Dim json As String = ""
Try json = DownloadUtf8(url)
Catch ex As Exception
snap.ErrorMessage = "Offline: " & ex.Message
Return snap
End Try
If
String.IsNullOrEmpty(json) Then snap.ErrorMessage = "Offline — no forecast data."
Return snap
End If
snap.CurrentTemp = ExtractJsonNumberNear(json, "current", "temperature_2m")
snap.FeelsLike = ExtractJsonNumberNear(json, "current", "apparent_temperature")
snap.Humidity = ExtractJsonNumberNear(json, "current", "relative_humidity_2m")
snap.WindSpeed = ExtractJsonNumberNear(json, "current", "wind_speed_10m")
snap.PrecipCurrent = ExtractJsonNumberNear(json, "current", "precipitation")
Dim codeVal As Double? = ExtractJsonNumberNear(json, "current", "weather_code")
snap.CurrentCode = CInt(codeVal.GetValueOrDefault(-1))
snap.CurrentCondition = WeatherCodeLabel(snap.CurrentCode) ParseHourly(json, snap) ParseDaily(json, snap, forecastDays)
snap.Ok = snap.CurrentTemp.HasValue OrElse snap.Days.Count > 0 OrElse snap.Hours.Count > 0
If Not snap.Ok Then snap.ErrorMessage = "No weather data."
Return snap
End Function
Public Function OpenStreetMapUrl(ByVal latitude As Double, ByVal longitude As Double, Optional ByVal zoom As Integer = 12) As String
If zoom < 1 Then zoom = 1
If zoom > 19 Then
zoom = 19
Return
String.Format(CultureInfo.InvariantCulture, "https://www.openstreetmap.org/?mlat={0:0.#####}&mlon={1:0.#####}#map={2}/{0:0.#####}/{1:0.#####}", latitude, longitude, zoom)
End Function
Public Function WeatherCodeLabel(ByVal code As Integer) As String
Select Case code
Case 0 :
Return "CLEAR"
Case 1 :
Return "MOSTLY CLEAR"
Case 2 :
Return "PARTLY CLOUDY"
Case 3 :
Return "OVERCAST"
Case 45, 48 :
Return "FOG"
Case 51, 53, 55, 56, 57 :
Return "DRIZZLE"
Case 61, 63, 65, 66, 67 :
Return "RAIN"
Case 71, 73, 75, 77 :
Return "SNOW"
Case 80, 81, 82 :
Return "SHOWERS"
Case 85, 86 :
Return "SNOW SHOWERS"
Case 95, 96, 99 :
Return "THUNDERSTORM"
Case
Else :
Return "—"
End Select
End Function
Private Sub ParseHourly(ByVal json As String, ByVal snap As ForecastSnapshot)
Dim hourlyIdx As Integer = json.IndexOf("""hourly""", StringComparison.OrdinalIgnoreCase)
If hourlyIdx < 0 Then Return
Dim slice As String = json.Substring(hourlyIdx, Math.Min(json.Length - hourlyIdx, 120000))
Dim dailyIdx As Integer = slice.IndexOf("""daily""", StringComparison.OrdinalIgnoreCase)
If dailyIdx > 0 Then slice = slice.Substring(0, dailyIdx)
Dim times As System.Collections.Generic.List(Of String) = ExtractStringArray(slice, "time")
Dim temps As System.Collections.Generic.List(Of Double) = ExtractNumberArray(slice, "temperature_2m")
Dim codes As System.Collections.Generic.List(Of Double) = ExtractNumberArray(slice, "weather_code")
Dim precip As System.Collections.Generic.List(Of Double) = ExtractNumberArray(slice, "precipitation_probability")
Dim nowLocal As DateTime = DateTime.Now
Dim added As Integer = 0
For i As Integer = 0 To times.Count - 1
Dim dt As DateTime
If Not DateTime.TryParse(times(i), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, dt) Then Continue
For
If dt <
nowLocal.AddMinutes(-30) Then Continue
For
Dim hour As New HourForecast()
hour.TimeLabel = dt.ToString("HH:mm", CultureInfo.InvariantCulture)
If i <
temps.Count Then hour.Temp = temps(i)
If i <
codes.Count Then hour.WeatherCode = CInt(codes(i))
hour.Condition = WeatherCodeLabel(hour.WeatherCode)
End If
If i <
precip.Count Then hour.PrecipProbability = precip(i)
snap.Hours.Add(hour) added += 1
If added >= 24 Then Exit
For
Next
End Sub
Private Sub ParseDaily(ByVal json As String, ByVal snap As ForecastSnapshot, ByVal forecastDays As Integer)
Dim dailyIdx As Integer = json.IndexOf("""daily""", StringComparison.OrdinalIgnoreCase)
If dailyIdx < 0 Then Return
Dim slice As String = json.Substring(dailyIdx, Math.Min(json.Length - dailyIdx, 60000))
Dim times As System.Collections.Generic.List(Of String) = ExtractStringArray(slice, "time")
Dim codes As System.Collections.Generic.List(Of Double) = ExtractNumberArray(slice, "weather_code")
Dim highs As System.Collections.Generic.List(Of Double) = ExtractNumberArray(slice, "temperature_2m_max")
Dim lows As System.Collections.Generic.List(Of Double) = ExtractNumberArray(slice, "temperature_2m_min")
Dim precip As System.Collections.Generic.List(Of Double) = ExtractNumberArray(slice, "precipitation_sum")
Dim wind As System.Collections.Generic.List(Of Double) = ExtractNumberArray(slice, "wind_speed_10m_max")
Dim n As Integer = Math.Min(forecastDays, Math.Min(highs.Count, lows.Count))
If
times.Count > 0 Then n = Math.Min(n, times.Count)
For i As Integer = 0 To n - 1
Dim day As New DayForecast()
If i <
times.Count Then day.DateLabel = FormatDayLabel(times(i))
Else day.DateLabel = "DAY " & (i + 1).ToString(CultureInfo.InvariantCulture)
End If
If i <
highs.Count Then day.High = highs(i)
If i <
lows.Count Then day.Low = lows(i)
If i <
precip.Count Then day.PrecipSum = precip(i)
If i <
wind.Count Then day.WindMax = wind(i)
If i <
codes.Count Then day.WeatherCode = CInt(codes(i))
day.Condition = WeatherCodeLabel(day.WeatherCode)
End If
snap.Days.Add(day)
Next
End Sub
Private Function UseFahrenheit() As Boolean
Return
String.Equals(GetSetting("LCARS x32", "Weather", "UseFahrenheit", "True"), "True", StringComparison.OrdinalIgnoreCase)
End Function
Private Function TryResolveCoordinates(ByRef latitude As Double, ByRef longitude As Double, ByRef placeLabel As String) As Boolean
Dim savedLat As String = GetSetting("LCARS x32", "Weather", "Latitude", "").Trim()
Dim savedLon As String = GetSetting("LCARS x32", "Weather", "Longitude", "").Trim()
If savedLat <> "" AndAlso savedLon <> "" Then
If
Double.TryParse(savedLat, NumberStyles.Float, CultureInfo.InvariantCulture, latitude) AndAlso Double.TryParse(savedLon, NumberStyles.Float, CultureInfo.InvariantCulture, longitude) Then placeLabel = GetSetting("LCARS x32", "Weather", "City", "").Trim()
Return True
End If
End If
Dim city As String = GetSetting("LCARS x32", "Weather", "City", "").Trim()
If city <> "" Then
If TryParseLatLon(city, latitude, longitude) Then placeLabel = ""
Return True
End If
If TryGeocodeCity(city, latitude, longitude) Then placeLabel = city
Return True
End If
End If
Return TryGeolocateByIp(latitude, longitude, placeLabel)
End Function
Private Function TryParseLatLon(ByVal text As String, ByRef latitude As Double, ByRef longitude As Double) As Boolean
Dim parts() As String = text.Split(","c)
If
parts.Length < 2 Then Return False
Return
Double.TryParse(parts(0).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, latitude) AndAlso Double.TryParse(parts(1).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, longitude) AndAlso latitude >= -90 AndAlso latitude <= 90 AndAlso longitude >= -180 AndAlso longitude <= 180
End Function
Private Function TryGeocodeCity(ByVal city As String, ByRef latitude As Double, ByRef longitude As Double) As Boolean
Dim url As String = "https://geocoding-api.open-meteo.com/v1/search?name=" & Uri.EscapeDataString(city) & "&count=1&language=en&format=json"
Dim json As String = DownloadUtf8(url)
If
String.IsNullOrEmpty(json) Then Return False
If
json.IndexOf("""results""", StringComparison.OrdinalIgnoreCase) < 0 Then Return False
Dim latMatch As Match = Regex.Match(json, """latitude""\s*:\s*(-?\d+(?:\.\d+)?)", RegexOptions.CultureInvariant)
Dim lonMatch As Match = Regex.Match(json, """longitude""\s*:\s*(-?\d+(?:\.\d+)?)", RegexOptions.CultureInvariant)
If Not latMatch.Success OrElse Not lonMatch.Success Then Return False
Return
Double.TryParse(latMatch.Groups(1).Value, NumberStyles.Float, CultureInfo.InvariantCulture, latitude) AndAlso Double.TryParse(lonMatch.Groups(1).Value, NumberStyles.Float, CultureInfo.InvariantCulture, longitude)
End Function
Private Function TryGeolocateByIp(ByRef latitude As Double, ByRef longitude As Double, ByRef placeLabel As String) As Boolean
Dim json As String = DownloadUtf8("http://ip-api.com/json/?fields=status,lat,lon,city")
If
String.IsNullOrEmpty(json) Then Return False
If
json.IndexOf("""status"":""fail""", StringComparison.OrdinalIgnoreCase) >= 0 Then Return False
Dim latMatch As Match = Regex.Match(json, """lat""\s*:\s*(-?\d+(?:\.\d+)?)", RegexOptions.CultureInvariant)
Dim lonMatch As Match = Regex.Match(json, """lon""\s*:\s*(-?\d+(?:\.\d+)?)", RegexOptions.CultureInvariant)
If Not latMatch.Success OrElse Not lonMatch.Success Then Return False
Dim cityMatch As Match = Regex.Match(json, """city""\s*:\s*""([^""]*)""", RegexOptions.CultureInvariant)
If
cityMatch.Success Then placeLabel = cityMatch.Groups(1).Value.Trim()
Return
Double.TryParse(latMatch.Groups(1).Value, NumberStyles.Float, CultureInfo.InvariantCulture, latitude) AndAlso Double.TryParse(lonMatch.Groups(1).Value, NumberStyles.Float, CultureInfo.InvariantCulture, longitude)
End Function
Private Sub EnsureTls12()
If tlsEnabled Then Return
Try
ServicePointManager.SecurityProtocol = DirectCast(Tls12, SecurityProtocolType) tlsEnabled = True
Catch
End Try
End Sub
Private Function DownloadUtf8(ByVal url As String) As String EnsureTls12()
Using client As New WebClient()
client.Encoding = Encoding.UTF8
client.Headers.Add("User-Agent", "LCARS-x32-Utilities/1.0")
Return
client.DownloadString(url)
End Using
End Function
Private Function ExtractJsonNumber(ByVal json As String, ByVal key As String) As Double?
Dim pattern As String = """" & Regex.Escape(key) & """\s*:\s*(-?\d+(?:\.\d+)?)"
Dim match As Match = Regex.Match(json, pattern, RegexOptions.CultureInvariant)
If Not match.Success Then Return Nothing
Dim value As Double
If
Double.TryParse(match.Groups(1).Value, NumberStyles.Float, CultureInfo.InvariantCulture, value) Then Return value
Return Nothing
End Function
Private Function ExtractJsonNumberNear(ByVal json As String, ByVal section As String, ByVal key As String) As Double?
Dim idx As Integer = json.IndexOf("""" & section & """", StringComparison.OrdinalIgnoreCase)
If idx < 0 Then Return Nothing
Dim slice As String = json.Substring(idx, Math.Min(1200, json.Length - idx))
Return ExtractJsonNumber(slice, key)
End Function
Private Function ExtractNumberArray(ByVal json As String, ByVal key As String) As System.Collections.Generic.List(Of Double)
Dim list As New System.Collections.Generic.List(Of Double)()
Dim pattern As String = """" & Regex.Escape(key) & """\s*:\s*\[([^\]]*)\]"
Dim match As Match = Regex.Match(json, pattern, RegexOptions.CultureInvariant)
If Not match.Success Then Return list
For Each part As String In match.Groups(1).Value.Split(","c)
Dim t As String = part.Trim()
If t = "" OrElse t = "null" Then Continue
For
Dim v As Double
If
Double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, v) Then list.Add(v)
Next
Return list
End Function
Private Function ExtractStringArray(ByVal json As String, ByVal key As String) As System.Collections.Generic.List(Of String)
Dim list As New System.Collections.Generic.List(Of String)()
Dim pattern As String = """" & Regex.Escape(key) & """\s*:\s*\[([^\]]*)\]"
Dim match As Match = Regex.Match(json, pattern, RegexOptions.CultureInvariant)
If Not match.Success Then Return list
For Each m As Match In Regex.Matches(match.Groups(1).Value, """([^""]*)""")
list.Add(m.Groups(1).Value)
Next
Return list
End Function
Private Function FormatDayLabel(ByVal isoDate As String) As String
Dim dt As DateTime
If
DateTime.TryParse(isoDate, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, dt) Then
Return
dt.ToString("ddd M/d", CultureInfo.InvariantCulture).ToUpperInvariant()
End If
Return isoDate
End Function
End Module
