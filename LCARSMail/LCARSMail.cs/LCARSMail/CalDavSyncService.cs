using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

public sealed class CalDavSyncService
{
	public static string LastError;

	private CalDavSyncService()
	{
	}

	public static string TryDiscoverVtodoCollectionUrl(string calendarUrl, string username, string password)
	{
		LastError = null;
		string text = ((calendarUrl != null) ? calendarUrl : "").Trim();
		if (text.Length == 0)
		{
			return "";
		}
		string body = "<?xml version=\"1.0\" encoding=\"utf-8\"?><D:propfind xmlns:D=\"DAV:\" xmlns:C=\"urn:ietf:params:xml:ns:caldav\"><D:prop><D:resourcetype/><C:supported-calendar-component-set/></D:prop></D:propfind>";
		List<string> list = new List<string>();
		list.Add(text);
		string text2 = text.TrimEnd('/');
		int num = text2.LastIndexOf('/');
		if (num > 0)
		{
			list.Add(text2.Substring(0, checked(num + 1)));
		}
		foreach (string item in list)
		{
			string text3 = DavPropfind(item, username, password, body);
			if (text3.Length == 0)
			{
				continue;
			}
			foreach (Match item2 in new Regex("<D:response[\\s\\S]*?</D:response>", RegexOptions.IgnoreCase).Matches(text3))
			{
				string value = item2.Value;
				if (value.IndexOf("VTODO", StringComparison.OrdinalIgnoreCase) < 0)
				{
					continue;
				}
				Match match2 = Regex.Match(value, "<D:href[^>]*>([^<]+)</D:href>", RegexOptions.IgnoreCase);
				if (!match2.Success)
				{
					continue;
				}
				string text4 = match2.Groups[1].Value.Trim();
				if (text4.Length == 0)
				{
					continue;
				}
				if (!text4.EndsWith("/"))
				{
					text4 += "/";
				}
				return CombineCalDavUrl(text, text4);
			}
		}
		return "";
	}

	public static List<string> EnumerateVeventCollectionUrls(string calendarUrl, string username, string password)
	{
		List<CalDavCollectionDescriptor> list = EnumerateVeventCalendarDescriptors(calendarUrl, username, password);
		List<string> list2 = new List<string>();
		if (list == null)
		{
			return list2;
		}
		foreach (CalDavCollectionDescriptor item in list)
		{
			if (item != null && !string.IsNullOrWhiteSpace(item.Url))
			{
				list2.Add(item.Url.Trim());
			}
		}
		return list2;
	}

	public static List<CalDavCollectionDescriptor> EnumerateVeventCalendarDescriptors(string calendarUrl, string username, string password)
	{
		List<CalDavCollectionDescriptor> list = new List<CalDavCollectionDescriptor>();
		LastError = null;
		string text = ((calendarUrl != null) ? calendarUrl : "").Trim();
		if (text.Length == 0)
		{
			return list;
		}
		string body = "<?xml version=\"1.0\" encoding=\"utf-8\"?><D:propfind xmlns:D=\"DAV:\" xmlns:C=\"urn:ietf:params:xml:ns:caldav\"><D:prop><D:resourcetype/><D:displayname/><C:calendar-description/><C:supported-calendar-component-set/></D:prop></D:propfind>";
		List<string> list2 = new List<string>();
		list2.Add(text);
		string text2 = text.TrimEnd('/');
		int num = text2.LastIndexOf('/');
		if (num > 0)
		{
			list2.Add(text2.Substring(0, checked(num + 1)));
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string item in list2)
		{
			string text3 = DavPropfind(item, username, password, body);
			if (text3.Length == 0)
			{
				continue;
			}
			foreach (Match item2 in new Regex("<D:response[\\s\\S]*?</D:response>", RegexOptions.IgnoreCase).Matches(text3))
			{
				string value = item2.Value;
				if (value.IndexOf("VEVENT", StringComparison.OrdinalIgnoreCase) < 0)
				{
					continue;
				}
				Match match2 = Regex.Match(value, "<D:href[^>]*>([^<]+)</D:href>", RegexOptions.IgnoreCase);
				if (!match2.Success)
				{
					continue;
				}
				string text4 = match2.Groups[1].Value.Trim();
				if (text4.Length == 0)
				{
					continue;
				}
				if (!text4.EndsWith("/"))
				{
					text4 += "/";
				}
				string text5 = CombineCalDavUrl(text, text4);
				if (hashSet.Add(text5))
				{
					string text6 = ExtractDavTagText(value, "displayname");
					if (text6.Length == 0)
					{
						text6 = ExtractDavTagText(value, "calendar-description");
					}
					if (text6.Length == 0)
					{
						text6 = Path.GetFileName(text5.Trim().TrimEnd('/'));
					}
					list.Add(new CalDavCollectionDescriptor
					{
						Url = text5,
						DisplayName = text6
					});
				}
			}
		}
		return list;
	}

	public static string TryGetCollectionDisplayName(string calendarUrl, string username, string password)
	{
		LastError = null;
		string text = ((calendarUrl != null) ? calendarUrl : "").Trim();
		if (text.Length == 0)
		{
			return "";
		}
		string body = "<?xml version=\"1.0\" encoding=\"utf-8\"?><D:propfind xmlns:D=\"DAV:\" xmlns:C=\"urn:ietf:params:xml:ns:caldav\"><D:prop><D:displayname/><C:calendar-description/></D:prop></D:propfind>";
		string text2 = DavPropfind(text, username, password, body);
		if (text2.Length == 0)
		{
			return "";
		}
		string text3 = ExtractDavTagText(text2, "displayname");
		if (text3.Length == 0)
		{
			text3 = ExtractDavTagText(text2, "calendar-description");
		}
		return text3;
	}

	private static string ExtractDavTagText(string chunk, string localName)
	{
		if (string.IsNullOrWhiteSpace(chunk) || string.IsNullOrWhiteSpace(localName))
		{
			return "";
		}
		string pattern = "<(?:D:|d:)?" + Regex.Escape(localName) + "[^>]*>([^<]*)</(?:D:|d:)?" + Regex.Escape(localName) + ">";
		Match match = Regex.Match(chunk, pattern, RegexOptions.IgnoreCase);
		if (!match.Success)
		{
			return "";
		}
		return match.Groups[1].Value.Trim();
	}

	private static string DavPropfind(string url, string username, string password, string body)
	{
		string result;
		try
		{
			HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
			httpWebRequest.Method = "PROPFIND";
			httpWebRequest.Credentials = new NetworkCredential(username, password);
			httpWebRequest.PreAuthenticate = true;
			httpWebRequest.Timeout = 45000;
			httpWebRequest.ContentType = "application/xml; charset=utf-8";
			httpWebRequest.Headers.Add("Depth", "1");
			byte[] bytes = Encoding.UTF8.GetBytes(body);
			httpWebRequest.ContentLength = bytes.Length;
			using (Stream stream = httpWebRequest.GetRequestStream())
			{
				stream.Write(bytes, 0, bytes.Length);
			}
			using HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
			using StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8);
			result = streamReader.ReadToEnd();
		}
		catch (Exception ex)
		{
			ProjectData.SetProjectError(ex);
			Exception ex2 = ex;
			LastError = ex2.Message;
			result = "";
			ProjectData.ClearProjectError();
		}
		return result;
	}

	private static string CombineCalDavUrl(string baseUrl, string href)
	{
		if (href.StartsWith("http", StringComparison.OrdinalIgnoreCase))
		{
			return href;
		}
		string text = ((baseUrl != null) ? baseUrl : "").Trim();
		if (!text.EndsWith("/"))
		{
			text += "/";
		}
		if (href.StartsWith("/"))
		{
			Uri result = null;
			if (Uri.TryCreate(text, UriKind.Absolute, out result))
			{
				return result.Scheme + "://" + result.Host + (result.IsDefaultPort ? "" : (":" + result.Port)) + href;
			}
		}
		return text + href.TrimStart('/');
	}

	public static List<CalDavEvent> FetchEvents(string calendarUrl, string username, string password)
	{
		LastError = null;
		string text = ((calendarUrl != null) ? calendarUrl : "").Trim();
		if (text.Length == 0)
		{
			LastError = "CalDAV calendar URL is required.";
			return null;
		}
		string body = "<?xml version=\"1.0\" encoding=\"utf-8\"?><C:calendar-query xmlns:D=\"DAV:\" xmlns:C=\"urn:ietf:params:xml:ns:caldav\"><D:prop><D:getetag/><C:calendar-data/></D:prop><C:filter><C:comp-filter name=\"VCALENDAR\"><C:comp-filter name=\"VEVENT\"/><C:comp-filter name=\"VTODO\"/></C:comp-filter></C:filter></C:calendar-query>";
		string text2 = DavReport(text, username, password, body);
		if (text2.Length == 0)
		{
			return null;
		}
		return ParseCalendarDataBlocks(text2);
	}

	public static bool DeleteEvent(string calendarUrl, string username, string password, string uid)
	{
		LastError = null;
		string text = ((uid != null) ? uid : "").Trim();
		if (text.Length == 0)
		{
			LastError = "Event UID required.";
			return false;
		}
		string text2 = ((calendarUrl != null) ? calendarUrl : "").Trim();
		if (!text2.EndsWith("/"))
		{
			text2 += "/";
		}
		return DavDelete(text2 + text + ".ics", username, password);
	}

	public static bool PutEvent(string calendarUrl, string username, string password, CalDavEvent ev)
	{
		LastError = null;
		if (ev == null || string.IsNullOrWhiteSpace(ev.Uid))
		{
			LastError = "Event UID required.";
			return false;
		}
		string text = ((calendarUrl != null) ? calendarUrl : "").Trim();
		if (!text.EndsWith("/"))
		{
			text += "/";
		}
		string url = text + ev.Uid + ".ics";
		string ics = BuildIcs(ev);
		return DavPut(url, username, password, ics);
	}

	public static bool PutTask(string calendarUrl, string username, string password, CalDavEvent ev)
	{
		LastError = null;
		if (ev == null || string.IsNullOrWhiteSpace(ev.Uid))
		{
			LastError = "Task UID required.";
			return false;
		}
		string text = ((calendarUrl != null) ? calendarUrl : "").Trim();
		if (!text.EndsWith("/"))
		{
			text += "/";
		}
		string url = text + ev.Uid + ".ics";
		string ics = BuildTaskIcs(ev);
		return DavPut(url, username, password, ics);
	}

	private static List<CalDavEvent> ParseCalendarDataBlocks(string xml)
	{
		List<CalDavEvent> list = new List<CalDavEvent>();
		Regex regex = new Regex("BEGIN:VEVENT[\\s\\S]*?END:VEVENT", RegexOptions.IgnoreCase);
		foreach (Match item in regex.Matches(xml))
		{
			list.Add(ParseVEvent(item.Value));
		}
		Regex regex2 = new Regex("BEGIN:VTODO[\\s\\S]*?END:VTODO", RegexOptions.IgnoreCase);
		foreach (Match item2 in regex2.Matches(xml))
		{
			CalDavEvent calDavEvent = ParseVEvent(item2.Value);
			if (calDavEvent != null)
			{
				CalDavEvent calDavEvent2 = calDavEvent;
				string text = ((calDavEvent2.Summary != null) ? calDavEvent2.Summary : "");
				if (!text.StartsWith("[TASK]", StringComparison.OrdinalIgnoreCase))
				{
					calDavEvent.Summary = "[TASK] " + text;
				}
				if (calDavEvent.TaskCompleted && !text.StartsWith("[DONE]", StringComparison.OrdinalIgnoreCase))
				{
					calDavEvent.Summary = "[DONE] " + calDavEvent.Summary;
				}
				list.Add(calDavEvent);
			}
		}
		return list;
	}

	private static CalDavEvent ParseVEvent(string ics)
	{
		CalDavEvent calDavEvent = new CalDavEvent();
		calDavEvent.Summary = ExtractIcsLine(ics, "SUMMARY");
		calDavEvent.Uid = ExtractIcsLine(ics, "UID");
		string text = ExtractIcsLine(ics, "STATUS");
		calDavEvent.TaskCompleted = text.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase);
		string text2 = ExtractIcsLine(ics, "DTSTART");
		if (text2.Length == 0)
		{
			text2 = ExtractIcsLine(ics, "DUE");
		}
		string raw = ExtractIcsLine(ics, "DTEND");
		calDavEvent.StartUtc = ParseIcsDate(text2);
		calDavEvent.EndUtc = ParseIcsDate(raw);
		if (DateTime.Compare(calDavEvent.EndUtc, DateTime.MinValue) == 0 && DateTime.Compare(calDavEvent.StartUtc, DateTime.MinValue) != 0)
		{
			calDavEvent.EndUtc = calDavEvent.StartUtc.AddHours(1.0);
		}
		calDavEvent.Location = ExtractIcsLine(ics, "LOCATION");
		calDavEvent.RecurrenceRule = ExtractIcsLine(ics, "RRULE");
		calDavEvent.ExDateUtcCsv = ExtractExDateCsvFromIcs(ics);
		calDavEvent.Category = ExtractIcsLine(ics, "CATEGORIES");
		if (!string.IsNullOrWhiteSpace(calDavEvent.Category))
		{
			calDavEvent.CalendarName = calDavEvent.Category.Trim();
		}
		string text3 = ExtractIcsLine(ics, "TRIGGER");
		if (text3.StartsWith("-PT", StringComparison.OrdinalIgnoreCase) && text3.EndsWith("M", StringComparison.OrdinalIgnoreCase))
		{
			string s = text3.Substring(3, checked(text3.Length - 4));
			int result = 0;
			if (int.TryParse(s, out result))
			{
				calDavEvent.AlarmMinutesBefore = result;
			}
		}
		ParseMeetingLines(ics, calDavEvent);
		return calDavEvent;
	}

	private static void ParseMeetingLines(string ics, CalDavEvent ev)
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
				ev.Organizer = ParseIcsValueAfterColon(text2);
				ev.IsMeetingInvitation = true;
			}
			else if (text2.StartsWith("ATTENDEE", StringComparison.OrdinalIgnoreCase))
			{
				ev.AttendeeMailto = ParseIcsValueAfterColon(text2);
				string text3 = ParseIcsParam(text2, "PARTSTAT");
				if (text3.Length > 0)
				{
					ev.MyPartStat = text3;
				}
				ev.IsMeetingInvitation = true;
			}
		}
	}

	private static string ParseIcsValueAfterColon(string line)
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

	private static void AppendMeetingIcsLines(StringBuilder sb, CalDavEvent ev)
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
			string text = ev.AttendeeMailto.Trim();
			string text2 = ((ev.MyPartStat != null) ? ev.MyPartStat : "NEEDS-ACTION").Trim().ToUpperInvariant();
			if (text2.Length == 0)
			{
				text2 = "NEEDS-ACTION";
			}
			sb.AppendLine("ATTENDEE;CUTYPE=INDIVIDUAL;ROLE=REQ-PARTICIPANT;PARTSTAT=" + text2 + ";RSVP=TRUE:" + text);
		}
		else if (ev.IsMeetingInvitation && !string.IsNullOrWhiteSpace(ev.MyPartStat))
		{
			string text3 = ev.MyPartStat.Trim().ToUpperInvariant();
			sb.AppendLine("ATTENDEE;PARTSTAT=" + text3 + ":mailto:unknown@invalid");
		}
	}

	private static string BuildIcs(CalDavEvent ev)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("BEGIN:VCALENDAR");
		stringBuilder.AppendLine("VERSION:2.0");
		stringBuilder.AppendLine("PRODID:-//LCARS Mail//CalDAV//EN");
		stringBuilder.AppendLine("BEGIN:VEVENT");
		stringBuilder.AppendLine("UID:" + ev.Uid);
		if (DateTime.Compare(ev.RecurrenceIdUtc, DateTime.MinValue) != 0)
		{
			stringBuilder.AppendLine("RECURRENCE-ID:" + ev.RecurrenceIdUtc.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'"));
		}
		stringBuilder.AppendLine("SUMMARY:" + ((ev.Summary != null) ? ev.Summary : ""));
		if (!string.IsNullOrWhiteSpace(ev.Location))
		{
			stringBuilder.AppendLine("LOCATION:" + ev.Location.Trim());
		}
		if (!string.IsNullOrWhiteSpace(ev.RecurrenceRule))
		{
			stringBuilder.AppendLine("RRULE:" + ev.RecurrenceRule.Trim());
		}
		if (!string.IsNullOrWhiteSpace(ev.ExDateUtcCsv))
		{
			string[] array = ev.ExDateUtcCsv.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string text in array)
			{
				string text2 = text.Trim();
				if (text2.Length > 0)
				{
					stringBuilder.AppendLine("EXDATE:" + text2);
				}
			}
		}
		if (!string.IsNullOrWhiteSpace(ev.Category))
		{
			stringBuilder.AppendLine("CATEGORIES:" + ev.Category.Trim());
		}
		stringBuilder.AppendLine("DTSTART:" + ev.StartUtc.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'"));
		stringBuilder.AppendLine("DTEND:" + ev.EndUtc.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'"));
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

	private static string BuildTaskIcs(CalDavEvent ev)
	{
		string text = StripTaskDecorators((ev.Summary != null) ? ev.Summary : "");
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("BEGIN:VCALENDAR");
		stringBuilder.AppendLine("VERSION:2.0");
		stringBuilder.AppendLine("PRODID:-//LCARS Mail//CalDAV//EN");
		stringBuilder.AppendLine("BEGIN:VTODO");
		stringBuilder.AppendLine("UID:" + ev.Uid);
		stringBuilder.AppendLine("SUMMARY:" + text);
		if (ev.TaskCompleted)
		{
			stringBuilder.AppendLine("STATUS:COMPLETED");
			stringBuilder.AppendLine("COMPLETED:" + DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'"));
		}
		else
		{
			stringBuilder.AppendLine("STATUS:NEEDS-ACTION");
		}
		if (DateTime.Compare(ev.StartUtc, DateTime.MinValue) != 0)
		{
			stringBuilder.AppendLine("DUE:" + ev.StartUtc.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'"));
		}
		stringBuilder.AppendLine("END:VTODO");
		stringBuilder.AppendLine("END:VCALENDAR");
		return stringBuilder.ToString();
	}

	internal static string StripTaskDecorators(string summary)
	{
		string text = ((summary != null) ? summary : "").Trim();
		if (text.StartsWith("[DONE]", StringComparison.OrdinalIgnoreCase))
		{
			text = text.Substring(6).TrimStart();
		}
		if (text.StartsWith("[TASK]", StringComparison.OrdinalIgnoreCase))
		{
			text = text.Substring(6).TrimStart();
		}
		return text;
	}

	private static string ExtractExDateCsvFromIcs(string ics)
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

	private static string ExtractIcsLine(string ics, string key)
	{
		string[] array = ics.Split(new char[2] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
		foreach (string text in array)
		{
			string text2 = text.Trim();
			if (text2.StartsWith(key + ":", StringComparison.OrdinalIgnoreCase) || text2.StartsWith(key + ";", StringComparison.OrdinalIgnoreCase))
			{
				int num = text2.IndexOf(':');
				if (num >= 0)
				{
					return text2.Substring(checked(num + 1)).Trim();
				}
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
				result = ((!text.EndsWith("Z", StringComparison.OrdinalIgnoreCase)) ? DateTime.ParseExact(text, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal) : DateTime.ParseExact(text, "yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToUniversalTime());
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

	private static string DavReport(string url, string username, string password, string body)
	{
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
			byte[] bytes = Encoding.UTF8.GetBytes(body);
			httpWebRequest.ContentLength = bytes.Length;
			using (Stream stream = httpWebRequest.GetRequestStream())
			{
				stream.Write(bytes, 0, bytes.Length);
			}
			using HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
			using StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8);
			result = streamReader.ReadToEnd();
		}
		catch (Exception ex)
		{
			ProjectData.SetProjectError(ex);
			Exception ex2 = ex;
			LastError = ex2.Message;
			result = "";
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
		catch (Exception ex)
		{
			ProjectData.SetProjectError(ex);
			Exception ex2 = ex;
			LastError = ex2.Message;
			result = false;
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
		catch (Exception ex)
		{
			ProjectData.SetProjectError(ex);
			Exception ex2 = ex;
			LastError = ex2.Message;
			result = false;
			ProjectData.ClearProjectError();
		}
		return result;
	}
}
