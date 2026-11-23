using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSutilities;

[StandardModule]
public sealed class IcsCalendarStore
{
	private const string EventsFile = "events.json";

	public static string DataRoot()
	{
		string text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LCARS", "Calendar");
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		return text;
	}

	public static List<CalendarEvent> LoadEvents()
	{
		string path = Path.Combine(DataRoot(), "events.json");
		List<CalendarEvent> result;
		if (!File.Exists(path))
		{
			result = new List<CalendarEvent>();
		}
		else
		{
			try
			{
				JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
				List<CalendarEvent> list = javaScriptSerializer.Deserialize<List<CalendarEvent>>(File.ReadAllText(path, Encoding.UTF8));
				result = ((list != null) ? list : new List<CalendarEvent>());
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = new List<CalendarEvent>();
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}

	public static void SaveEvents(List<CalendarEvent> events)
	{
		JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
		File.WriteAllText(Path.Combine(DataRoot(), "events.json"), javaScriptSerializer.Serialize(events), Encoding.UTF8);
	}

	public static int ImportIcsFile(string path)
	{
		if (!File.Exists(path))
		{
			return 0;
		}
		string ics = File.ReadAllText(path, Encoding.UTF8);
		List<CalendarEvent> list = ParseIcs(ics);
		List<CalendarEvent> list2 = LoadEvents();
		list2.AddRange(list);
		SaveEvents(list2);
		return list.Count;
	}

	public static List<CalendarEvent> ParseIcs(string ics)
	{
		List<CalendarEvent> list = new List<CalendarEvent>();
		CalendarEvent calendarEvent = null;
		string[] array = ics.Split(new string[2] { "\r\n", "\n" }, StringSplitOptions.None);
		foreach (string line in array)
		{
			string text = UnfoldIcsLine(line).Trim();
			if (text.Length == 0)
			{
				continue;
			}
			if (Operators.CompareString(text, "BEGIN:VEVENT", TextCompare: false) == 0)
			{
				calendarEvent = new CalendarEvent();
			}
			else if (Operators.CompareString(text, "END:VEVENT", TextCompare: false) == 0)
			{
				if (calendarEvent != null)
				{
					list.Add(calendarEvent);
				}
				calendarEvent = null;
			}
			else
			{
				if (calendarEvent == null)
				{
					continue;
				}
				if (text.StartsWith("UID:", StringComparison.OrdinalIgnoreCase))
				{
					calendarEvent.Uid = text.Substring(4).Trim();
				}
				if (text.StartsWith("SUMMARY:", StringComparison.OrdinalIgnoreCase))
				{
					calendarEvent.Summary = text.Substring(8).Trim();
				}
				if (text.StartsWith("LOCATION:", StringComparison.OrdinalIgnoreCase))
				{
					calendarEvent.Location = text.Substring(9).Trim();
				}
				if (text.StartsWith("DTSTART", StringComparison.OrdinalIgnoreCase))
				{
					calendarEvent.StartUtc = ParseIcsDate(text);
				}
				if (text.StartsWith("DTEND", StringComparison.OrdinalIgnoreCase))
				{
					calendarEvent.EndUtc = ParseIcsDate(text);
				}
				if (text.StartsWith("ORGANIZER", StringComparison.OrdinalIgnoreCase))
				{
					calendarEvent.Organizer = ParseIcsValue(text);
					calendarEvent.IsMeetingInvitation = true;
				}
				if (text.StartsWith("ATTENDEE", StringComparison.OrdinalIgnoreCase))
				{
					calendarEvent.AttendeeMailto = ParseIcsValue(text);
					string text2 = ParseIcsParam(text, "PARTSTAT");
					if (text2.Length > 0)
					{
						calendarEvent.MyPartStat = text2;
					}
					calendarEvent.IsMeetingInvitation = true;
				}
			}
		}
		return list;
	}

	private static string ParseIcsValue(string line)
	{
		int num = line.IndexOf(':');
		if (num < 0)
		{
			return line.Trim();
		}
		return line.Substring(checked(num + 1)).Trim();
	}

	private static string ParseIcsParam(string line, string paramName)
	{
		string text = line.ToUpperInvariant();
		string text2 = paramName.ToUpperInvariant() + "=";
		int num = text.IndexOf(text2, StringComparison.Ordinal);
		if (num < 0)
		{
			return "";
		}
		string text3 = line.Substring(checked(num + text2.Length));
		int num2 = text3.IndexOf(';');
		if (num2 >= 0)
		{
			text3 = text3.Substring(0, num2);
		}
		return text3.Trim();
	}

	private static string UnfoldIcsLine(string line)
	{
		return ((line != null) ? line : "").Replace("\\n", "").Replace("\\r", "");
	}

	private static DateTime ParseIcsDate(string line)
	{
		string text = line;
		int num = text.IndexOf(':');
		checked
		{
			if (num >= 0)
			{
				text = text.Substring(num + 1).Trim();
			}
			if (text.EndsWith("Z", StringComparison.OrdinalIgnoreCase))
			{
				text = text.Substring(0, text.Length - 1);
				if (DateTime.TryParseExact(text, "yyyyMMdd'T'HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var result))
				{
					return result.ToUniversalTime();
				}
			}
			if (DateTime.TryParseExact(text, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result2))
			{
				return result2.ToUniversalTime();
			}
			return DateTime.UtcNow;
		}
	}
}
