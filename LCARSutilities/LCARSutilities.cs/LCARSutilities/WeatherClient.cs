using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSutilities;

[StandardModule]
public sealed class WeatherClient
{
	public class HourForecast
	{
		public string TimeLabel;

		public double? Temp;

		public int WeatherCode;

		public string Condition;

		public double? PrecipProbability;

		public HourForecast()
		{
			TimeLabel = "";
			WeatherCode = -1;
			Condition = "";
		}
	}

	public class DayForecast
	{
		public string DateLabel;

		public int WeatherCode;

		public double? High;

		public double? Low;

		public string Condition;

		public double? PrecipSum;

		public double? WindMax;

		public DayForecast()
		{
			DateLabel = "";
			WeatherCode = -1;
			Condition = "";
		}
	}

	public class ForecastSnapshot
	{
		public bool Ok;

		public string ErrorMessage;

		public string PlaceLabel;

		public double Latitude;

		public double Longitude;

		public string UnitSymbol;

		public double? CurrentTemp;

		public double? FeelsLike;

		public double? Humidity;

		public double? WindSpeed;

		public double? PrecipCurrent;

		public int CurrentCode;

		public string CurrentCondition;

		public List<HourForecast> Hours;

		public List<DayForecast> Days;

		public ForecastSnapshot()
		{
			ErrorMessage = "";
			PlaceLabel = "";
			UnitSymbol = "F";
			CurrentCode = -1;
			CurrentCondition = "";
			Hours = new List<HourForecast>();
			Days = new List<DayForecast>();
		}
	}

	public class LocationFix
	{
		public bool Ok;

		public double Latitude;

		public double Longitude;

		public string PlaceLabel;

		public string ErrorMessage;

		public LocationFix()
		{
			PlaceLabel = "";
			ErrorMessage = "";
		}
	}

	private const int Tls12 = 3072;

	private static bool tlsEnabled = false;

	public static LocationFix ResolveLocation()
	{
		LocationFix locationFix = new LocationFix();
		double latitude = 0.0;
		double longitude = 0.0;
		string placeLabel = "";
		if (TryResolveCoordinates(ref latitude, ref longitude, ref placeLabel))
		{
			locationFix.Ok = true;
			locationFix.Latitude = latitude;
			locationFix.Longitude = longitude;
			locationFix.PlaceLabel = placeLabel;
		}
		else
		{
			locationFix.ErrorMessage = "Set Weather city/lat-lon in LCARS Settings (same as HUD).";
		}
		return locationFix;
	}

	public static ForecastSnapshot FetchForecast(int forecastDays = 7)
	{
		EnsureTls12();
		ForecastSnapshot forecastSnapshot = new ForecastSnapshot();
		LocationFix locationFix = ResolveLocation();
		ForecastSnapshot result;
		if (!locationFix.Ok)
		{
			forecastSnapshot.ErrorMessage = locationFix.ErrorMessage;
			result = forecastSnapshot;
		}
		else
		{
			forecastSnapshot.Latitude = locationFix.Latitude;
			forecastSnapshot.Longitude = locationFix.Longitude;
			forecastSnapshot.PlaceLabel = locationFix.PlaceLabel;
			forecastSnapshot.UnitSymbol = (UseFahrenheit() ? "F" : "C");
			string text = (UseFahrenheit() ? "fahrenheit" : "celsius");
			if (forecastDays < 1)
			{
				forecastDays = 1;
			}
			if (forecastDays > 16)
			{
				forecastDays = 16;
			}
			string url = string.Format(CultureInfo.InvariantCulture, "https://api.open-meteo.com/v1/forecast?latitude={0:0.####}&longitude={1:0.####}&current=temperature_2m,apparent_temperature,weather_code,relative_humidity_2m,wind_speed_10m,precipitation&hourly=temperature_2m,weather_code,precipitation_probability&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_sum,wind_speed_10m_max&timezone=auto&temperature_unit={2}&forecast_days={3}", locationFix.Latitude, locationFix.Longitude, text, forecastDays);
			string text2 = "";
			try
			{
				text2 = DownloadUtf8(url);
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Exception ex2 = ex;
				forecastSnapshot.ErrorMessage = "Offline: " + ex2.Message;
				result = forecastSnapshot;
				ProjectData.ClearProjectError();
				goto IL_0264;
			}
			if (string.IsNullOrEmpty(text2))
			{
				forecastSnapshot.ErrorMessage = "Offline — no forecast data.";
				result = forecastSnapshot;
			}
			else
			{
				forecastSnapshot.CurrentTemp = ExtractJsonNumberNear(text2, "current", "temperature_2m");
				forecastSnapshot.FeelsLike = ExtractJsonNumberNear(text2, "current", "apparent_temperature");
				forecastSnapshot.Humidity = ExtractJsonNumberNear(text2, "current", "relative_humidity_2m");
				forecastSnapshot.WindSpeed = ExtractJsonNumberNear(text2, "current", "wind_speed_10m");
				forecastSnapshot.PrecipCurrent = ExtractJsonNumberNear(text2, "current", "precipitation");
				forecastSnapshot.CurrentCode = checked((int)Math.Round(ExtractJsonNumberNear(text2, "current", "weather_code") ?? (-1.0)));
				forecastSnapshot.CurrentCondition = WeatherCodeLabel(forecastSnapshot.CurrentCode);
				ParseHourly(text2, forecastSnapshot);
				ParseDaily(text2, forecastSnapshot, forecastDays);
				forecastSnapshot.Ok = ((forecastSnapshot.CurrentTemp.HasValue || forecastSnapshot.Days.Count > 0 || forecastSnapshot.Hours.Count > 0) ? true : false);
				if (!forecastSnapshot.Ok)
				{
					forecastSnapshot.ErrorMessage = "No weather data.";
				}
				result = forecastSnapshot;
			}
		}
		goto IL_0264;
		IL_0264:
		return result;
	}

	public static string OpenStreetMapUrl(double latitude, double longitude, int zoom = 12)
	{
		if (zoom < 1)
		{
			zoom = 1;
		}
		if (zoom > 19)
		{
			zoom = 19;
		}
		return string.Format(CultureInfo.InvariantCulture, "https://www.openstreetmap.org/?mlat={0:0.#####}&mlon={1:0.#####}#map={2}/{0:0.#####}/{1:0.#####}", latitude, longitude, zoom);
	}

	public static string WeatherCodeLabel(int code)
	{
		switch (code)
		{
		case 0:
			return "CLEAR";
		case 1:
			return "MOSTLY CLEAR";
		case 2:
			return "PARTLY CLOUDY";
		case 3:
			return "OVERCAST";
		case 45:
		case 48:
			return "FOG";
		case 51:
		case 53:
		case 55:
		case 56:
		case 57:
			return "DRIZZLE";
		case 61:
		case 63:
		case 65:
		case 66:
		case 67:
			return "RAIN";
		case 71:
		case 73:
		case 75:
		case 77:
			return "SNOW";
		case 80:
		case 81:
		case 82:
			return "SHOWERS";
		case 85:
		case 86:
			return "SNOW SHOWERS";
		case 95:
		case 96:
		case 99:
			return "THUNDERSTORM";
		default:
			return "—";
		}
	}

	private static void ParseHourly(string json, ForecastSnapshot snap)
	{
		int num = json.IndexOf("\"hourly\"", StringComparison.OrdinalIgnoreCase);
		if (num < 0)
		{
			return;
		}
		checked
		{
			string text = json.Substring(num, Math.Min(json.Length - num, 120000));
			int num2 = text.IndexOf("\"daily\"", StringComparison.OrdinalIgnoreCase);
			if (num2 > 0)
			{
				text = text.Substring(0, num2);
			}
			List<string> list = ExtractStringArray(text, "time");
			List<double> list2 = ExtractNumberArray(text, "temperature_2m");
			List<double> list3 = ExtractNumberArray(text, "weather_code");
			List<double> list4 = ExtractNumberArray(text, "precipitation_probability");
			DateTime now = DateTime.Now;
			int num3 = 0;
			int num4 = list.Count - 1;
			for (int i = 0; i <= num4; i++)
			{
				if (DateTime.TryParse(list[i], CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var result) && DateTime.Compare(result, now.AddMinutes(-30.0)) >= 0)
				{
					HourForecast hourForecast = new HourForecast();
					hourForecast.TimeLabel = result.ToString("HH:mm", CultureInfo.InvariantCulture);
					if (i < list2.Count)
					{
						hourForecast.Temp = list2[i];
					}
					if (i < list3.Count)
					{
						hourForecast.WeatherCode = (int)Math.Round(list3[i]);
						hourForecast.Condition = WeatherCodeLabel(hourForecast.WeatherCode);
					}
					if (i < list4.Count)
					{
						hourForecast.PrecipProbability = list4[i];
					}
					snap.Hours.Add(hourForecast);
					num3++;
					if (num3 >= 24)
					{
						break;
					}
				}
			}
		}
	}

	private static void ParseDaily(string json, ForecastSnapshot snap, int forecastDays)
	{
		int num = json.IndexOf("\"daily\"", StringComparison.OrdinalIgnoreCase);
		if (num < 0)
		{
			return;
		}
		checked
		{
			string json2 = json.Substring(num, Math.Min(json.Length - num, 60000));
			List<string> list = ExtractStringArray(json2, "time");
			List<double> list2 = ExtractNumberArray(json2, "weather_code");
			List<double> list3 = ExtractNumberArray(json2, "temperature_2m_max");
			List<double> list4 = ExtractNumberArray(json2, "temperature_2m_min");
			List<double> list5 = ExtractNumberArray(json2, "precipitation_sum");
			List<double> list6 = ExtractNumberArray(json2, "wind_speed_10m_max");
			int num2 = Math.Min(forecastDays, Math.Min(list3.Count, list4.Count));
			if (list.Count > 0)
			{
				num2 = Math.Min(num2, list.Count);
			}
			int num3 = num2 - 1;
			for (int i = 0; i <= num3; i++)
			{
				DayForecast dayForecast = new DayForecast();
				if (i < list.Count)
				{
					dayForecast.DateLabel = FormatDayLabel(list[i]);
				}
				else
				{
					dayForecast.DateLabel = "DAY " + (i + 1).ToString(CultureInfo.InvariantCulture);
				}
				if (i < list3.Count)
				{
					dayForecast.High = list3[i];
				}
				if (i < list4.Count)
				{
					dayForecast.Low = list4[i];
				}
				if (i < list5.Count)
				{
					dayForecast.PrecipSum = list5[i];
				}
				if (i < list6.Count)
				{
					dayForecast.WindMax = list6[i];
				}
				if (i < list2.Count)
				{
					dayForecast.WeatherCode = (int)Math.Round(list2[i]);
					dayForecast.Condition = WeatherCodeLabel(dayForecast.WeatherCode);
				}
				snap.Days.Add(dayForecast);
			}
		}
	}

	private static bool UseFahrenheit()
	{
		return string.Equals(Interaction.GetSetting("LCARS x32", "Weather", "UseFahrenheit", "True"), "True", StringComparison.OrdinalIgnoreCase);
	}

	private static bool TryResolveCoordinates(ref double latitude, ref double longitude, ref string placeLabel)
	{
		string text = Interaction.GetSetting("LCARS x32", "Weather", "Latitude").Trim();
		string text2 = Interaction.GetSetting("LCARS x32", "Weather", "Longitude").Trim();
		if (Operators.CompareString(text, "", TextCompare: false) != 0 && Operators.CompareString(text2, "", TextCompare: false) != 0 && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out latitude) && double.TryParse(text2, NumberStyles.Float, CultureInfo.InvariantCulture, out longitude))
		{
			placeLabel = Interaction.GetSetting("LCARS x32", "Weather", "City").Trim();
			return true;
		}
		string text3 = Interaction.GetSetting("LCARS x32", "Weather", "City").Trim();
		if (Operators.CompareString(text3, "", TextCompare: false) != 0)
		{
			if (TryParseLatLon(text3, ref latitude, ref longitude))
			{
				placeLabel = "";
				return true;
			}
			if (TryGeocodeCity(text3, ref latitude, ref longitude))
			{
				placeLabel = text3;
				return true;
			}
		}
		return TryGeolocateByIp(ref latitude, ref longitude, ref placeLabel);
	}

	private static bool TryParseLatLon(string text, ref double latitude, ref double longitude)
	{
		string[] array = text.Split(',');
		if (array.Length < 2)
		{
			return false;
		}
		return (double.TryParse(array[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out latitude) && double.TryParse(array[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out longitude) && latitude >= -90.0 && latitude <= 90.0 && longitude >= -180.0 && longitude <= 180.0) ? true : false;
	}

	private static bool TryGeocodeCity(string city, ref double latitude, ref double longitude)
	{
		string url = "https://geocoding-api.open-meteo.com/v1/search?name=" + Uri.EscapeDataString(city) + "&count=1&language=en&format=json";
		string text = DownloadUtf8(url);
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		if (text.IndexOf("\"results\"", StringComparison.OrdinalIgnoreCase) < 0)
		{
			return false;
		}
		Match match = Regex.Match(text, "\"latitude\"\\s*:\\s*(-?\\d+(?:\\.\\d+)?)", RegexOptions.CultureInvariant);
		Match match2 = Regex.Match(text, "\"longitude\"\\s*:\\s*(-?\\d+(?:\\.\\d+)?)", RegexOptions.CultureInvariant);
		if (!match.Success || !match2.Success)
		{
			return false;
		}
		return (double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out latitude) && double.TryParse(match2.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out longitude)) ? true : false;
	}

	private static bool TryGeolocateByIp(ref double latitude, ref double longitude, ref string placeLabel)
	{
		string text = DownloadUtf8("http://ip-api.com/json/?fields=status,lat,lon,city");
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		if (text.IndexOf("\"status\":\"fail\"", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return false;
		}
		Match match = Regex.Match(text, "\"lat\"\\s*:\\s*(-?\\d+(?:\\.\\d+)?)", RegexOptions.CultureInvariant);
		Match match2 = Regex.Match(text, "\"lon\"\\s*:\\s*(-?\\d+(?:\\.\\d+)?)", RegexOptions.CultureInvariant);
		if (!match.Success || !match2.Success)
		{
			return false;
		}
		Match match3 = Regex.Match(text, "\"city\"\\s*:\\s*\"([^\"]*)\"", RegexOptions.CultureInvariant);
		if (match3.Success)
		{
			placeLabel = match3.Groups[1].Value.Trim();
		}
		return (double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out latitude) && double.TryParse(match2.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out longitude)) ? true : false;
	}

	private static void EnsureTls12()
	{
		if (tlsEnabled)
		{
			return;
		}
		try
		{
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
			tlsEnabled = true;
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
	}

	private static string DownloadUtf8(string url)
	{
		EnsureTls12();
		using WebClient webClient = new WebClient();
		webClient.Encoding = Encoding.UTF8;
		webClient.Headers.Add("User-Agent", "LCARS-x32-Utilities/1.0");
		return webClient.DownloadString(url);
	}

	private static double? ExtractJsonNumber(string json, string key)
	{
		string pattern = "\"" + Regex.Escape(key) + "\"\\s*:\\s*(-?\\d+(?:\\.\\d+)?)";
		Match match = Regex.Match(json, pattern, RegexOptions.CultureInvariant);
		double? result = default(double?);
		if (!match.Success)
		{
			return result;
		}
		if (double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result2))
		{
			return result2;
		}
		return result;
	}

	private static double? ExtractJsonNumberNear(string json, string section, string key)
	{
		int num = json.IndexOf("\"" + section + "\"", StringComparison.OrdinalIgnoreCase);
		double? result = default(double?);
		if (num < 0)
		{
			return result;
		}
		string json2 = json.Substring(num, Math.Min(1200, checked(json.Length - num)));
		return ExtractJsonNumber(json2, key);
	}

	private static List<double> ExtractNumberArray(string json, string key)
	{
		List<double> list = new List<double>();
		string pattern = "\"" + Regex.Escape(key) + "\"\\s*:\\s*\\[([^\\]]*)\\]";
		Match match = Regex.Match(json, pattern, RegexOptions.CultureInvariant);
		if (!match.Success)
		{
			return list;
		}
		string[] array = match.Groups[1].Value.Split(',');
		foreach (string text in array)
		{
			string text2 = text.Trim();
			switch (text2)
			{
			case null:
			case "":
			case "null":
				continue;
			}
			if (double.TryParse(text2, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
			{
				list.Add(result);
			}
		}
		return list;
	}

	private static List<string> ExtractStringArray(string json, string key)
	{
		List<string> list = new List<string>();
		string pattern = "\"" + Regex.Escape(key) + "\"\\s*:\\s*\\[([^\\]]*)\\]";
		Match match = Regex.Match(json, pattern, RegexOptions.CultureInvariant);
		if (!match.Success)
		{
			return list;
		}
		foreach (Match item in Regex.Matches(match.Groups[1].Value, "\"([^\"]*)\""))
		{
			list.Add(item.Groups[1].Value);
		}
		return list;
	}

	private static string FormatDayLabel(string isoDate)
	{
		if (DateTime.TryParse(isoDate, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var result))
		{
			return result.ToString("ddd M/d", CultureInfo.InvariantCulture).ToUpperInvariant();
		}
		return isoDate;
	}
}
