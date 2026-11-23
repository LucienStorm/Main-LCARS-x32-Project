using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using ProtectedData = System.Security.Cryptography.ProtectedData;
using DataProtectionScope = System.Security.Cryptography.DataProtectionScope;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSutilities;

[StandardModule]
public sealed class CalDavTaskPush
{
	private class CalDavDto
	{
		public string CalendarUrl;

		public string Username;

		[DebuggerNonUserCode]
		public CalDavDto()
		{
		}
	}

	private const string MailRootSubdir = "LCARS x32\\Mail";

	private const string CalDavSettingsFile = "caldav-settings.json";

	private const string CalDavSecretKey = "caldav";

	public static bool TryPushTask(CalendarEvent ev)
	{
		if (ev == null || string.IsNullOrWhiteSpace(ev.Uid))
		{
			return false;
		}
		string text = ((ev.Summary != null) ? ev.Summary : "");
		if (text.IndexOf("[TASK]", StringComparison.OrdinalIgnoreCase) < 0)
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
		string text2 = calendarUrl.Trim();
		if (!text2.EndsWith("/"))
		{
			text2 += "/";
		}
		string url = text2 + ev.Uid.Trim() + ".ics";
		string ics = BuildTaskIcs(ev);
		bool flag = DavPut(url, username, password, ics);
		if (flag)
		{
			ev.CalDavDirty = false;
		}
		return flag;
	}

	private static string BuildTaskIcs(CalendarEvent ev)
	{
		string text = StripDecorators((ev.Summary != null) ? ev.Summary : "");
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("BEGIN:VCALENDAR");
		stringBuilder.AppendLine("VERSION:2.0");
		stringBuilder.AppendLine("PRODID:-//LCARS Calendar//CalDAV//EN");
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
		int num = Math.Max(0, Math.Min(100, ev.TaskPercentComplete));
		if (ev.TaskCompleted && num < 100)
		{
			num = 100;
		}
		stringBuilder.AppendLine("PERCENT-COMPLETE:" + num);
		stringBuilder.AppendLine("PRIORITY:" + Math.Max(0, Math.Min(9, ev.TaskPriority)));
		if (DateTime.Compare(ev.StartUtc, DateTime.MinValue) != 0)
		{
			stringBuilder.AppendLine("DUE:" + ev.StartUtc.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'"));
		}
		if (ev.AlarmMinutesBefore > 0)
		{
			stringBuilder.AppendLine("BEGIN:VALARM");
			stringBuilder.AppendLine("TRIGGER:-PT" + ev.AlarmMinutesBefore + "M");
			stringBuilder.AppendLine("ACTION:DISPLAY");
			stringBuilder.AppendLine("DESCRIPTION:Task reminder");
			stringBuilder.AppendLine("END:VALARM");
		}
		if (!string.IsNullOrWhiteSpace(ev.Category))
		{
			stringBuilder.AppendLine("CATEGORIES:" + ev.Category.Trim());
		}
		if (!string.IsNullOrWhiteSpace(ev.RecurrenceRule))
		{
			stringBuilder.AppendLine("RRULE:" + ev.RecurrenceRule.Trim());
		}
		if (!string.IsNullOrWhiteSpace(ev.CalendarName))
		{
			stringBuilder.AppendLine("X-LCARS-CALNAME:" + ev.CalendarName.Trim());
		}
		stringBuilder.AppendLine("END:VTODO");
		stringBuilder.AppendLine("END:VCALENDAR");
		return stringBuilder.ToString();
	}

	private static string StripDecorators(string summary)
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
		bool result;
		if (string.IsNullOrWhiteSpace(secretKey))
		{
			result = false;
		}
		else
		{
			string path = Path.Combine(MailDataRoot(), "secrets", SanitizeToken(secretKey) + ".dpapi");
			if (!File.Exists(path))
			{
				result = false;
			}
			else
			{
				try
				{
					byte[] encryptedData = File.ReadAllBytes(path);
					byte[] bytes = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
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
}
