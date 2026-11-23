using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSutilities;

[StandardModule]
public sealed class CalDavCalendarSync
{
	private class CalDavDto
	{
		public string CalendarUrl;

		public string Username;

		public string TaskCollectionUrl;

		public List<string> SecondaryCalendarUrls;

		public Dictionary<string, string> CalendarUrlLabels;

		public string PrimaryCalendarDisplayName;

		[DebuggerNonUserCode]
		public CalDavDto()
		{
		}
	}

	private const string MailRootSubdir = "LCARS x32\\Mail";

	private const string CalDavSettingsFile = "caldav-settings.json";

	private const string CalDavSecretKey = "caldav";

	public static int LastBidirectionalSyncCount = 0;

	public static int LastBidirectionalPushCount = 0;

	public static int LastPrimaryEventsTagged = 0;

	public static int LastSecondaryEventsTagged = 0;

	public static int LastTaskEventsTagged = 0;

	public static int LastPullMergeAdded = 0;

	public static int LastSecondaryPullMergeAdded = 0;

	public static int LastUrlLabelsAtPull = 0;

	public static int LastPrimaryPullMergeAdded = 0;

	public static int LastTaskPullMergeAdded = 0;

	public static bool IsTaskEvent(CalendarEvent ev)
	{
		if (ev == null)
		{
			return false;
		}
		return ((ev.Summary != null) ? ev.Summary : "").IndexOf("[TASK]", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	public static bool TryPushEvent(CalendarEvent ev)
	{
		if (ev == null || string.IsNullOrWhiteSpace(ev.Uid))
		{
			return false;
		}
		if (IsTaskEvent(ev))
		{
			return CalDavTaskPush.TryPushTask(ev);
		}
		string calendarUrl = null;
		string username = null;
		string password = null;
		if (!TryLoadCalDavCredentials(ref calendarUrl, ref username, ref password))
		{
			return false;
		}
		string text = ResolveCalendarPushUrl(calendarUrl.Trim(), ev).Trim();
		if (!text.EndsWith("/"))
		{
			text += "/";
		}
		bool flag = DavPut(text + ev.Uid.Trim() + ".ics", username, password, BuildEventIcs(ev));
		if (flag)
		{
			ev.CalDavDirty = false;
		}
		return flag;
	}

	public static bool TryDeleteEvent(CalendarEvent ev)
	{
		if (ev == null || string.IsNullOrWhiteSpace(ev.Uid))
		{
			return false;
		}
		string calendarUrl = null;
		string username = null;
		string password = null;
		if (!TryLoadCalDavCredentials(ref calendarUrl, ref username, ref password))
		{
			return false;
		}
		string text = calendarUrl.Trim();
		if (!text.EndsWith("/"))
		{
			text += "/";
		}
		return DavDelete(text + ev.Uid.Trim() + ".ics", username, password);
	}

	public static int TryPullAndMerge()
	{
		string calendarUrl = null;
		string username = null;
		string password = null;
		if (!TryLoadCalDavCredentials(ref calendarUrl, ref username, ref password))
		{
			return -1;
		}
		string text = DavReport(calendarUrl, username, password);
		if (text.Length == 0)
		{
			return -1;
		}
		List<CalendarEvent> list = ParseCalendarReport(text);
		string text2 = TryGetPrimaryCalendarFriendlyLabel();
		LastPrimaryEventsTagged = 0;
		checked
		{
			foreach (CalendarEvent item in list)
			{
				if (item != null && string.IsNullOrWhiteSpace(item.CalendarName) && text2.Length > 0)
				{
					item.CalendarName = text2;
					LastPrimaryEventsTagged++;
				}
			}
			List<CalendarEvent> list2 = IcsCalendarStore.LoadEvents();
			Dictionary<string, CalendarEvent> dictionary = new Dictionary<string, CalendarEvent>(StringComparer.OrdinalIgnoreCase);
			foreach (CalendarEvent item2 in list2)
			{
				if (item2 != null && !string.IsNullOrWhiteSpace(item2.Uid))
				{
					dictionary[item2.Uid] = item2;
				}
			}
			int num = 0;
			LastPrimaryPullMergeAdded = 0;
			foreach (CalendarEvent item3 in list)
			{
				if (item3 == null || string.IsNullOrWhiteSpace(item3.Uid))
				{
					continue;
				}
				CalendarEvent calendarEvent = null;
				if (dictionary.ContainsKey(item3.Uid))
				{
					calendarEvent = dictionary[item3.Uid];
				}
				if (calendarEvent != null && calendarEvent.CalDavDirty)
				{
					continue;
				}
				if (calendarEvent != null)
				{
					item3.EwsItemId = calendarEvent.EwsItemId;
					item3.EwsChangeKey = calendarEvent.EwsChangeKey;
					item3.EwsDirty = calendarEvent.EwsDirty;
					item3.EwsLastModifiedUtc = calendarEvent.EwsLastModifiedUtc;
					if (string.IsNullOrWhiteSpace(item3.RecurrenceRule))
					{
						item3.RecurrenceRule = calendarEvent.RecurrenceRule;
					}
					if (string.IsNullOrWhiteSpace(item3.Category))
					{
						item3.Category = calendarEvent.Category;
					}
					if (string.IsNullOrWhiteSpace(item3.CalendarName))
					{
						item3.CalendarName = calendarEvent.CalendarName;
					}
					if (string.IsNullOrWhiteSpace(item3.Location))
					{
						item3.Location = calendarEvent.Location;
					}
					if (string.IsNullOrWhiteSpace(item3.Organizer))
					{
						item3.Organizer = calendarEvent.Organizer;
					}
					if (string.IsNullOrWhiteSpace(item3.AttendeeMailto))
					{
						item3.AttendeeMailto = calendarEvent.AttendeeMailto;
					}
					if (string.IsNullOrWhiteSpace(item3.MyPartStat))
					{
						item3.MyPartStat = calendarEvent.MyPartStat;
					}
					if (!item3.IsMeetingInvitation)
					{
						item3.IsMeetingInvitation = calendarEvent.IsMeetingInvitation;
					}
				}
				if (!dictionary.ContainsKey(item3.Uid))
				{
					num++;
					LastPrimaryPullMergeAdded++;
				}
				dictionary[item3.Uid] = item3;
			}
			IcsCalendarStore.SaveEvents(new List<CalendarEvent>(dictionary.Values));
			string text3 = TryLoadTaskCollectionUrl();
			LastTaskEventsTagged = 0;
			LastTaskPullMergeAdded = 0;
			if (text3.Length > 0 && !string.Equals(text3, calendarUrl.Trim(), StringComparison.OrdinalIgnoreCase))
			{
				string text4 = DavReport(text3, username, password);
				if (text4.Length > 0)
				{
					string text5 = TryGetCalendarFriendlyLabel(text3.Trim());
					if (text5.Length == 0)
					{
						text5 = "VTODO";
					}
					foreach (CalendarEvent item4 in ParseCalendarReport(text4))
					{
						if (item4 != null && !string.IsNullOrWhiteSpace(item4.Uid))
						{
							if (string.IsNullOrWhiteSpace(item4.CalendarName) && text5.Length > 0)
							{
								item4.CalendarName = text5;
								LastTaskEventsTagged++;
							}
							else if (string.IsNullOrWhiteSpace(item4.CalendarName))
							{
								item4.CalendarName = text5;
							}
							if (!dictionary.ContainsKey(item4.Uid))
							{
								num++;
								LastTaskPullMergeAdded++;
							}
							dictionary[item4.Uid] = item4;
						}
					}
					IcsCalendarStore.SaveEvents(new List<CalendarEvent>(dictionary.Values));
				}
			}
			LastSecondaryEventsTagged = 0;
			LastSecondaryPullMergeAdded = 0;
			foreach (string item5 in TryLoadSecondaryCalendarUrls())
			{
				if (string.IsNullOrWhiteSpace(item5) || string.Equals(item5.Trim(), calendarUrl.Trim(), StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				string text6 = DavReport(item5, username, password);
				if (text6.Length == 0)
				{
					continue;
				}
				string text7 = TryGetCalendarFriendlyLabel(item5.Trim());
				if (text7.Length == 0)
				{
					text7 = Path.GetFileName(item5.Trim().TrimEnd('/'));
				}
				foreach (CalendarEvent item6 in ParseCalendarReport(text6))
				{
					if (item6 != null && !string.IsNullOrWhiteSpace(item6.Uid))
					{
						if (string.IsNullOrWhiteSpace(item6.CalendarName) && text7.Length > 0)
						{
							item6.CalendarName = text7;
							LastSecondaryEventsTagged++;
						}
						else if (string.IsNullOrWhiteSpace(item6.CalendarName))
						{
							item6.CalendarName = text7;
						}
						if (!dictionary.ContainsKey(item6.Uid))
						{
							num++;
							LastSecondaryPullMergeAdded++;
						}
						dictionary[item6.Uid] = item6;
					}
				}
				IcsCalendarStore.SaveEvents(new List<CalendarEvent>(dictionary.Values));
			}
			LastPullMergeAdded = Math.Max(0, num);
			LastUrlLabelsAtPull = CountKnownCalendarUrlLabels();
			return num;
		}
	}

	private static List<string> TryLoadSecondaryCalendarUrls()
	{
		string path = Path.Combine(MailDataRoot(), "caldav-settings.json");
		List<string> result;
		if (!File.Exists(path))
		{
			result = new List<string>();
		}
		else
		{
			try
			{
				JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
				CalDavDto calDavDto = javaScriptSerializer.Deserialize<CalDavDto>(File.ReadAllText(path, Encoding.UTF8));
				result = ((calDavDto != null && calDavDto.SecondaryCalendarUrls != null) ? calDavDto.SecondaryCalendarUrls : new List<string>());
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = new List<string>();
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}

	private static string TryLoadTaskCollectionUrl()
	{
		string path = Path.Combine(MailDataRoot(), "caldav-settings.json");
		string result;
		if (!File.Exists(path))
		{
			result = "";
		}
		else
		{
			try
			{
				JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
				CalDavDto calDavDto = javaScriptSerializer.Deserialize<CalDavDto>(File.ReadAllText(path, Encoding.UTF8));
				if (calDavDto == null)
				{
					result = "";
				}
				else
				{
					CalDavDto calDavDto2 = calDavDto;
					result = ((calDavDto2.TaskCollectionUrl != null) ? calDavDto2.TaskCollectionUrl : "").Trim();
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = "";
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}

	private static string ResolveCalendarPushUrl(string primaryUrl, CalendarEvent ev)
	{
		string result = ((primaryUrl != null) ? primaryUrl : "").Trim();
		if (ev == null)
		{
			return result;
		}
		if (IsTaskEvent(ev))
		{
			string text = TryLoadTaskCollectionUrl();
			if (text.Length > 0)
			{
				return text;
			}
		}
		string text2 = ((ev.CalendarName != null) ? ev.CalendarName : "").Trim();
		if (text2.Length == 0)
		{
			return result;
		}
		foreach (string item in TryLoadSecondaryCalendarUrls())
		{
			if (!string.IsNullOrWhiteSpace(item))
			{
				string fileName = Path.GetFileName(item.Trim().TrimEnd('/'));
				if (text2.Equals(fileName, StringComparison.OrdinalIgnoreCase))
				{
					return item.Trim();
				}
			}
		}
		return result;
	}

	public static int TrySyncBidirectional()
	{
		int num = TryPullAndMerge();
		if (num < 0)
		{
			return num;
		}
		int num2 = 0;
		foreach (CalendarEvent item in IcsCalendarStore.LoadEvents())
		{
			if (item != null && !string.IsNullOrWhiteSpace(item.Uid) && item.CalDavDirty && TryPushEvent(item))
			{
				num2 = checked(num2 + 1);
			}
		}
		LastBidirectionalPushCount = Math.Max(0, num2);
		LastBidirectionalSyncCount = Math.Max(num, LastBidirectionalPushCount);
		return LastBidirectionalSyncCount;
	}

	private static string BuildEventIcs(CalendarEvent ev)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("BEGIN:VCALENDAR");
		stringBuilder.AppendLine("VERSION:2.0");
		stringBuilder.AppendLine("PRODID:-//LCARS Calendar//CalDAV//EN");
		stringBuilder.AppendLine("BEGIN:VEVENT");
		stringBuilder.AppendLine("UID:" + ev.Uid);
		if (DateTime.Compare(ev.RecurrenceIdUtc, DateTime.MinValue) != 0)
		{
			stringBuilder.AppendLine("RECURRENCE-ID:" + ev.RecurrenceIdUtc.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture));
		}
		stringBuilder.AppendLine("SUMMARY:" + ((ev.Summary != null) ? ev.Summary : ""));
		if (!string.IsNullOrWhiteSpace(ev.Location))
		{
			stringBuilder.AppendLine("LOCATION:" + ev.Location);
		}
		stringBuilder.AppendLine("DTSTART:" + ev.StartUtc.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture));
		stringBuilder.AppendLine("DTEND:" + ev.EndUtc.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture));
		if (!string.IsNullOrWhiteSpace(ev.RecurrenceRule))
		{
			stringBuilder.AppendLine("RRULE:" + ev.RecurrenceRule.Trim());
		}
		AppendExDateLines(stringBuilder, ev);
		if (!string.IsNullOrWhiteSpace(ev.Category))
		{
			stringBuilder.AppendLine("CATEGORIES:" + ev.Category.Trim());
		}
		if (!string.IsNullOrWhiteSpace(ev.CalendarName))
		{
			stringBuilder.AppendLine("X-LCARS-CALNAME:" + ev.CalendarName.Trim());
		}
		if (ev.AlarmMinutesBefore > 0)
		{
			stringBuilder.AppendLine("BEGIN:VALARM");
			stringBuilder.AppendLine("TRIGGER:-PT" + ev.AlarmMinutesBefore + "M");
			stringBuilder.AppendLine("ACTION:DISPLAY");
			stringBuilder.AppendLine("DESCRIPTION:LCARS");
			stringBuilder.AppendLine("END:VALARM");
		}
		AppendMeetingIcsLines(stringBuilder, ev);
		stringBuilder.AppendLine("END:VEVENT");
		stringBuilder.AppendLine("END:VCALENDAR");
		return stringBuilder.ToString();
	}

	private static void AppendExDateLines(StringBuilder sb, CalendarEvent ev)
	{
		if (ev == null)
		{
			return;
		}
		string text = ((ev.ExDateUtcCsv != null) ? ev.ExDateUtcCsv : "").Trim();
		if (text.Length == 0)
		{
			return;
		}
		string[] array = text.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
		foreach (string text2 in array)
		{
			string text3 = text2.Trim();
			if (text3.Length != 0)
			{
				sb.AppendLine("EXDATE:" + text3);
			}
		}
	}

	private static void AppendMeetingIcsLines(StringBuilder sb, CalendarEvent ev)
	{
		if (ev == null)
		{
			return;
		}
		if (!string.IsNullOrWhiteSpace(ev.Organizer))
		{
			sb.AppendLine("ORGANIZER:" + ev.Organizer.Trim());
		}
		if (!string.IsNullOrWhiteSpace(ev.AttendeeMailto))
		{
			string text = ((ev.MyPartStat != null) ? ev.MyPartStat : "NEEDS-ACTION").Trim().ToUpperInvariant();
			if (text.Length == 0)
			{
				text = "NEEDS-ACTION";
			}
			sb.AppendLine("ATTENDEE;CUTYPE=INDIVIDUAL;ROLE=REQ-PARTICIPANT;PARTSTAT=" + text + ";RSVP=TRUE:" + ev.AttendeeMailto.Trim());
		}
		else if (ev.IsMeetingInvitation && !string.IsNullOrWhiteSpace(ev.MyPartStat))
		{
			sb.AppendLine("ATTENDEE;PARTSTAT=" + ev.MyPartStat.Trim().ToUpperInvariant() + ":mailto:unknown@invalid");
		}
	}

	private static List<CalendarEvent> ParseCalendarReport(string xml)
	{
		List<CalendarEvent> list = new List<CalendarEvent>();
		Regex regex = new Regex("BEGIN:VEVENT[\\s\\S]*?END:VEVENT", RegexOptions.IgnoreCase);
		foreach (Match item in regex.Matches(xml))
		{
			list.Add(ParseVEventBlock(item.Value));
		}
		Regex regex2 = new Regex("BEGIN:VTODO[\\s\\S]*?END:VTODO", RegexOptions.IgnoreCase);
		foreach (Match item2 in regex2.Matches(xml))
		{
			CalendarEvent calendarEvent = ParseVEventBlock(item2.Value);
			if (calendarEvent != null)
			{
				CalendarEvent calendarEvent2 = calendarEvent;
				string text = ((calendarEvent2.Summary != null) ? calendarEvent2.Summary : "");
				if (text.IndexOf("[TASK]", StringComparison.OrdinalIgnoreCase) < 0)
				{
					calendarEvent.Summary = "[TASK] " + text;
				}
				string text2 = ExtractIcs(item2.Value, "STATUS");
				if (text2.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase) && text.IndexOf("[DONE]", StringComparison.OrdinalIgnoreCase) < 0)
				{
					calendarEvent.Summary = "[DONE] " + calendarEvent.Summary;
				}
				list.Add(calendarEvent);
			}
		}
		return list;
	}

	private static CalendarEvent ParseVEventBlock(string ics)
	{
		CalendarEvent calendarEvent = new CalendarEvent();
		calendarEvent.Uid = ExtractIcs(ics, "UID");
		calendarEvent.Summary = ExtractIcs(ics, "SUMMARY");
		calendarEvent.Location = ExtractIcs(ics, "LOCATION");
		calendarEvent.StartUtc = ParseIcsDate(ExtractIcs(ics, "DTSTART"));
		calendarEvent.EndUtc = ParseIcsDate(ExtractIcs(ics, "DTEND"));
		if (DateTime.Compare(calendarEvent.StartUtc, DateTime.MinValue) == 0)
		{
			calendarEvent.StartUtc = ParseIcsDate(ExtractIcs(ics, "DUE"));
		}
		if (DateTime.Compare(calendarEvent.EndUtc, DateTime.MinValue) == 0 && DateTime.Compare(calendarEvent.StartUtc, DateTime.MinValue) != 0)
		{
			calendarEvent.EndUtc = calendarEvent.StartUtc.AddHours(1.0);
		}
		string text = ExtractIcs(ics, "TRIGGER");
		if (text.StartsWith("-PT", StringComparison.OrdinalIgnoreCase) && text.EndsWith("M", StringComparison.OrdinalIgnoreCase))
		{
			string s = text.Substring(3, checked(text.Length - 4));
			int result = 0;
			if (int.TryParse(s, out result))
			{
				calendarEvent.AlarmMinutesBefore = result;
			}
		}
		string s2 = ExtractIcs(ics, "PRIORITY");
		int result2 = 0;
		if (int.TryParse(s2, out result2))
		{
			calendarEvent.TaskPriority = Math.Max(0, Math.Min(9, result2));
		}
		string s3 = ExtractIcs(ics, "PERCENT-COMPLETE");
		int result3 = 0;
		if (int.TryParse(s3, out result3))
		{
			calendarEvent.TaskPercentComplete = Math.Max(0, Math.Min(100, result3));
		}
		calendarEvent.RecurrenceRule = ExtractIcs(ics, "RRULE");
		calendarEvent.ExDateUtcCsv = ExtractExDateCsv(ics);
		calendarEvent.Category = ExtractIcs(ics, "CATEGORIES");
		string text2 = ExtractIcs(ics, "X-LCARS-CALNAME");
		if (text2.Length == 0)
		{
			text2 = ExtractIcs(ics, "X-WR-CALNAME");
		}
		if (text2.Length > 0)
		{
			calendarEvent.CalendarName = text2.Trim();
		}
		else if (!string.IsNullOrWhiteSpace(calendarEvent.Category))
		{
			calendarEvent.CalendarName = calendarEvent.Category.Trim();
		}
		string text3 = ExtractIcs(ics, "STATUS");
		if (text3.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase))
		{
			calendarEvent.TaskCompleted = true;
		}
		ParseMeetingInvitationLines(ics, calendarEvent);
		return calendarEvent;
	}

	private static void ParseMeetingInvitationLines(string ics, CalendarEvent ev)
	{
		if (ev == null || string.IsNullOrEmpty(ics))
		{
			return;
		}
		string[] array = ics.Split(new char[2] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
		foreach (string text in array)
		{
			string text2 = text.Trim();
			if (text2.StartsWith("ORGANIZER", StringComparison.OrdinalIgnoreCase))
			{
				ev.Organizer = ParseIcsLineValue(text2);
				ev.IsMeetingInvitation = true;
			}
			else if (text2.StartsWith("ATTENDEE", StringComparison.OrdinalIgnoreCase))
			{
				ev.AttendeeMailto = ParseIcsLineValue(text2);
				string text3 = ParseIcsLineParam(text2, "PARTSTAT");
				if (text3.Length > 0)
				{
					ev.MyPartStat = text3;
				}
				ev.IsMeetingInvitation = true;
			}
		}
	}

	private static string ParseIcsLineValue(string line)
	{
		int num = line.IndexOf(':');
		if (num < 0)
		{
			return line.Trim();
		}
		return line.Substring(checked(num + 1)).Trim();
	}

	private static string ParseIcsLineParam(string line, string paramName)
	{
		string text = line.ToUpperInvariant();
		string text2 = paramName.ToUpperInvariant() + "=";
		int num = text.IndexOf(";" + text2, StringComparison.Ordinal);
		if (num < 0)
		{
			num = text.IndexOf(text2, StringComparison.Ordinal);
		}
		if (num < 0)
		{
			return "";
		}
		checked
		{
			num += text2.Length;
			int num2 = text.IndexOf(';', num);
			if (num2 < 0)
			{
				num2 = text.IndexOf(':', num);
			}
			if (num2 < 0)
			{
				num2 = text.Length;
			}
			return line.Substring(num, num2 - num).Trim();
		}
	}

	private static string ExtractExDateCsv(string ics)
	{
		if (string.IsNullOrEmpty(ics))
		{
			return "";
		}
		List<string> list = new List<string>();
		string[] array = ics.Split(new char[2] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
		foreach (string text in array)
		{
			string text2 = text.Trim();
			if (text2.StartsWith("EXDATE:", StringComparison.OrdinalIgnoreCase))
			{
				string text3 = text2.Substring(7).Trim();
				if (text3.Length > 0)
				{
					list.Add(text3);
				}
			}
		}
		if (list.Count == 0)
		{
			return "";
		}
		return string.Join(",", list.ToArray());
	}

	private static string ExtractIcs(string ics, string key)
	{
		string[] array = ics.Split(new char[2] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
		foreach (string text in array)
		{
			string text2 = text.Trim();
			if (text2.StartsWith(key + ":", StringComparison.OrdinalIgnoreCase))
			{
				return text2.Substring(checked(key.Length + 1)).Trim();
			}
		}
		return "";
	}

	private static DateTime ParseIcsDate(string raw)
	{
		string text = ((raw != null) ? raw : "").Trim();
		DateTime result;
		if (text.Length == 0)
		{
			result = DateTime.MinValue;
		}
		else
		{
			try
			{
				result = ((!text.EndsWith("Z", StringComparison.OrdinalIgnoreCase)) ? DateTime.ParseExact(text, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal).ToUniversalTime() : DateTime.ParseExact(text, "yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToUniversalTime());
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = DateTime.MinValue;
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}

	private static string DavReport(string url, string username, string password)
	{
		string s = "<?xml version=\"1.0\" encoding=\"utf-8\"?><C:calendar-query xmlns:D=\"DAV:\" xmlns:C=\"urn:ietf:params:xml:ns:caldav\"><D:prop><D:getetag/><C:calendar-data/></D:prop><C:filter><C:comp-filter name=\"VCALENDAR\"><C:comp-filter name=\"VEVENT\"/><C:comp-filter name=\"VTODO\"/></C:comp-filter></C:filter></C:calendar-query>";
		string result;
		try
		{
			HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
			httpWebRequest.Method = "REPORT";
			httpWebRequest.Credentials = new NetworkCredential(username, password);
			httpWebRequest.PreAuthenticate = true;
			httpWebRequest.Timeout = 45000;
			httpWebRequest.ContentType = "application/xml; charset=utf-8";
			httpWebRequest.Headers.Add("Depth", "1");
			byte[] bytes = Encoding.UTF8.GetBytes(s);
			httpWebRequest.ContentLength = bytes.Length;
			using (Stream stream = httpWebRequest.GetRequestStream())
			{
				stream.Write(bytes, 0, bytes.Length);
			}
			using HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
			using StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8);
			result = streamReader.ReadToEnd();
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			result = "";
			ProjectData.ClearProjectError();
		}
		return result;
	}

	private static bool DavPut(string url, string username, string password, string ics)
	{
		bool result;
		try
		{
			HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
			httpWebRequest.Method = "PUT";
			httpWebRequest.Credentials = new NetworkCredential(username, password);
			httpWebRequest.ContentType = "text/calendar; charset=utf-8";
			byte[] bytes = Encoding.UTF8.GetBytes(ics);
			httpWebRequest.ContentLength = bytes.Length;
			using (Stream stream = httpWebRequest.GetRequestStream())
			{
				stream.Write(bytes, 0, bytes.Length);
			}
			using HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
			result = ((httpWebResponse.StatusCode >= HttpStatusCode.OK && httpWebResponse.StatusCode < HttpStatusCode.MultipleChoices) ? true : false);
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			result = false;
			ProjectData.ClearProjectError();
		}
		return result;
	}

	private static bool DavDelete(string url, string username, string password)
	{
		bool result;
		try
		{
			HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
			httpWebRequest.Method = "DELETE";
			httpWebRequest.Credentials = new NetworkCredential(username, password);
			using HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
			int statusCode = (int)httpWebResponse.StatusCode;
			result = (((statusCode >= 200 && statusCode < 300) || statusCode == 404) ? true : false);
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			result = false;
			ProjectData.ClearProjectError();
		}
		return result;
	}

	private static bool TryLoadCalDavCredentials(ref string calendarUrl, ref string username, ref string password)
	{
		calendarUrl = null;
		username = null;
		password = null;
		string path = Path.Combine(MailDataRoot(), "caldav-settings.json");
		bool result;
		if (!File.Exists(path))
		{
			result = false;
		}
		else
		{
			try
			{
				JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
				CalDavDto calDavDto = javaScriptSerializer.Deserialize<CalDavDto>(File.ReadAllText(path, Encoding.UTF8));
				if (calDavDto == null || string.IsNullOrWhiteSpace(calDavDto.CalendarUrl))
				{
					result = false;
				}
				else
				{
					calendarUrl = calDavDto.CalendarUrl.Trim();
					CalDavDto calDavDto2 = calDavDto;
					username = ((calDavDto2.Username != null) ? calDavDto2.Username : "").Trim();
					result = TryReadMailSecret("caldav", ref password);
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = false;
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}

	private static string MailDataRoot()
	{
		return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LCARS x32\\Mail");
	}

	private static bool TryReadMailSecret(string secretKey, ref string password)
	{
		password = null;
		string path = Path.Combine(MailDataRoot(), "secrets", SanitizeToken(secretKey) + ".dpapi");
		bool result;
		if (!File.Exists(path))
		{
			result = false;
		}
		else
		{
			try
			{
				byte[] bytes = ProtectedData.Unprotect(File.ReadAllBytes(path), null, DataProtectionScope.CurrentUser);
				password = Encoding.UTF8.GetString(bytes);
				result = true;
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = false;
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}

	private static string SanitizeToken(string token)
	{
		char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
		StringBuilder stringBuilder = new StringBuilder(token.Length);
		int i = 0;
		for (int length = token.Length; i < length; i = checked(i + 1))
		{
			char value = token[i];
			if (Array.IndexOf(invalidFileNameChars, value) >= 0)
			{
				stringBuilder.Append('_');
			}
			else
			{
				stringBuilder.Append(value);
			}
		}
		string text = stringBuilder.ToString();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "account";
		}
		return text;
	}

	public static string TryGetPrimaryCalendarFriendlyLabel()
	{
		string path = Path.Combine(MailDataRoot(), "caldav-settings.json");
		if (!File.Exists(path))
		{
			return "";
		}
		try
		{
			JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
			CalDavDto calDavDto = javaScriptSerializer.Deserialize<CalDavDto>(File.ReadAllText(path, Encoding.UTF8));
			if (calDavDto == null)
			{
				return "";
			}
			CalDavDto calDavDto2 = calDavDto;
			string text = ((calDavDto2.PrimaryCalendarDisplayName != null) ? calDavDto2.PrimaryCalendarDisplayName : "").Trim();
			if (text.Length > 0)
			{
				return text.ToUpperInvariant();
			}
			calDavDto2 = calDavDto;
			string text2 = ((calDavDto2.CalendarUrl != null) ? calDavDto2.CalendarUrl : "").Trim();
			if (text2.Length > 0)
			{
				return TryGetCalendarFriendlyLabel(text2);
			}
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
		return "";
	}

	public static int CountKnownCalendarUrlLabels()
	{
		return TryLoadCalendarUrlLabels()?.Count ?? 0;
	}

	public static string TryGetCalendarFriendlyLabel(string calendarNameOrUrl)
	{
		string text = ((calendarNameOrUrl != null) ? calendarNameOrUrl : "").Trim();
		if (text.Length == 0)
		{
			return "";
		}
		Dictionary<string, string> dictionary = TryLoadCalendarUrlLabels();
		if (dictionary == null || dictionary.Count == 0)
		{
			return text;
		}
		foreach (KeyValuePair<string, string> item in dictionary)
		{
			string key = item.Key;
			string text2 = ((key != null) ? key : "").Trim();
			if (text2.Length == 0)
			{
				continue;
			}
			string fileName = Path.GetFileName(text2.Trim().TrimEnd('/'));
			if (text.Equals(text2, StringComparison.OrdinalIgnoreCase) || text.Equals(fileName, StringComparison.OrdinalIgnoreCase))
			{
				key = item.Value;
				string text3 = ((key != null) ? key : "").Trim();
				if (text3.Length > 0)
				{
					return text3.ToUpperInvariant();
				}
			}
		}
		return text.ToUpperInvariant();
	}

	private static Dictionary<string, string> TryLoadCalendarUrlLabels()
	{
		string path = Path.Combine(MailDataRoot(), "caldav-settings.json");
		Dictionary<string, string> result;
		if (!File.Exists(path))
		{
			result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
		else
		{
			try
			{
				JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
				CalDavDto calDavDto = javaScriptSerializer.Deserialize<CalDavDto>(File.ReadAllText(path, Encoding.UTF8));
				result = ((calDavDto != null && calDavDto.CalendarUrlLabels != null) ? calDavDto.CalendarUrlLabels : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}
}
