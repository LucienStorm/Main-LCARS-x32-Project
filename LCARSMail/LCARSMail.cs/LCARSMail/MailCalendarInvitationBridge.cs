using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

public sealed class MailCalendarInvitationBridge
{
	private static readonly Regex IcsBlockRx = new Regex("BEGIN:VCALENDAR[\\s\\S]*?END:VCALENDAR", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	public static int LastImportedCount = 0;

	public static int LastCalDavPushFromInvite = 0;

	public static int LastImportedWithAlarm = 0;

	public static int LastImportedWithRecurrence = 0;

	public static int LastImportedWithCategory = 0;

	private MailCalendarInvitationBridge()
	{
	}

	public static int TryImportFromMessageBody(MailMessageBody body)
	{
		LastImportedCount = 0;
		if (body == null)
		{
			return 0;
		}
		int num = 0;
		checked
		{
			num += ImportInvitationBlocksFromText(body.TextBody);
			if (body.IcsCalendarParts != null)
			{
				foreach (string icsCalendarPart in body.IcsCalendarParts)
				{
					num += ImportInvitationBlocksFromText(icsCalendarPart);
				}
			}
			LastImportedCount = num;
			return num;
		}
	}

	public static int TryImportInvitationBlocksFromText(string mailBody)
	{
		return LastImportedCount = ImportInvitationBlocksFromText(mailBody);
	}

	private static int ImportInvitationBlocksFromText(string mailBody)
	{
		if (string.IsNullOrWhiteSpace(mailBody))
		{
			return 0;
		}
		int num = 0;
		foreach (Match item in IcsBlockRx.Matches(mailBody))
		{
			if (item != null && item.Success)
			{
				string value = item.Value;
				if (value.IndexOf("BEGIN:VEVENT", StringComparison.OrdinalIgnoreCase) >= 0 && value.IndexOf("METHOD:CANCEL", StringComparison.OrdinalIgnoreCase) < 0 && (value.IndexOf("METHOD:REQUEST", StringComparison.OrdinalIgnoreCase) >= 0 || value.IndexOf("ORGANIZER", StringComparison.OrdinalIgnoreCase) >= 0) && TryMergeIcsIntoSharedCalendar(value))
				{
					num = checked(num + 1);
				}
			}
		}
		return num;
	}

	public static int TryScanIndexedBodiesForInvitations(string accountId, int maxMessages = 48)
	{
		if (string.IsNullOrWhiteSpace(accountId))
		{
			return 0;
		}
		List<MailSearchIndex.IndexBodyTarget> list = MailSearchIndex.ListEntriesWithBodyPreview(accountId, maxMessages);
		int num = 0;
		foreach (MailSearchIndex.IndexBodyTarget item in list)
		{
			if (item != null)
			{
				string text = MailSearchIndex.TryGetBodyPreviewSnippet(accountId, item.Folder, item.Uid, 4096);
				if (text.Length != 0)
				{
					num = checked(num + TryImportInvitationBlocksFromText(text));
				}
			}
		}
		LastImportedCount = num;
		return num;
	}

	private static bool TryMergeIcsIntoSharedCalendar(string ics)
	{
		if (string.IsNullOrWhiteSpace(ics))
		{
			return false;
		}
		List<LcarsCalendarBridge.SharedCalendarEvent> list = ParseIcsToSharedEvents(ics);
		if (list == null || list.Count == 0)
		{
			return false;
		}
		List<LcarsCalendarBridge.SharedCalendarEvent> list2 = LcarsCalendarBridge.LoadEvents();
		Dictionary<string, LcarsCalendarBridge.SharedCalendarEvent> dictionary = new Dictionary<string, LcarsCalendarBridge.SharedCalendarEvent>(StringComparer.OrdinalIgnoreCase);
		foreach (LcarsCalendarBridge.SharedCalendarEvent item in list2)
		{
			if (item != null && !string.IsNullOrWhiteSpace(item.Uid))
			{
				dictionary[item.Uid] = item;
			}
		}
		bool flag = false;
		LastImportedWithRecurrence = 0;
		LastImportedWithCategory = 0;
		checked
		{
			foreach (LcarsCalendarBridge.SharedCalendarEvent item2 in list)
			{
				if (item2 == null || string.IsNullOrWhiteSpace(item2.Uid))
				{
					continue;
				}
				item2.IsMeetingInvitation = true;
				item2.CalDavDirty = true;
				if (!string.IsNullOrWhiteSpace(item2.RecurrenceRule))
				{
					LastImportedWithRecurrence++;
				}
				if (!string.IsNullOrWhiteSpace(item2.Category))
				{
					LastImportedWithCategory++;
				}
				if (item2.AlarmMinutesBefore > 0)
				{
					LastImportedWithAlarm++;
				}
				LcarsCalendarBridge.SharedCalendarEvent sharedCalendarEvent = null;
				if (dictionary.ContainsKey(item2.Uid))
				{
					sharedCalendarEvent = dictionary[item2.Uid];
					if (sharedCalendarEvent.CalDavDirty)
					{
						continue;
					}
					item2.EwsItemId = sharedCalendarEvent.EwsItemId;
					item2.EwsChangeKey = sharedCalendarEvent.EwsChangeKey;
					item2.EwsDirty = sharedCalendarEvent.EwsDirty;
					item2.EwsLastModifiedUtc = sharedCalendarEvent.EwsLastModifiedUtc;
				}
				dictionary[item2.Uid] = item2;
				flag = true;
			}
			if (!flag)
			{
				return false;
			}
			LcarsCalendarBridge.SaveEvents(new List<LcarsCalendarBridge.SharedCalendarEvent>(dictionary.Values));
			QueueCalDavSyncFromInvitations();
			return true;
		}
	}

	public static void QueueCalDavSyncFromInvitations()
	{
		ThreadPool.QueueUserWorkItem([SpecialName] (object state) =>
		{
			try
			{
				LastCalDavPushFromInvite = LcarsCalendarBridge.TrySyncStoredCalDavBidirectional();
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				LastCalDavPushFromInvite = -1;
				ProjectData.ClearProjectError();
			}
		});
	}

	private static List<LcarsCalendarBridge.SharedCalendarEvent> ParseIcsToSharedEvents(string ics)
	{
		List<LcarsCalendarBridge.SharedCalendarEvent> list = new List<LcarsCalendarBridge.SharedCalendarEvent>();
		string text = "";
		LcarsCalendarBridge.SharedCalendarEvent sharedCalendarEvent = null;
		bool flag = false;
		string[] array = ics.Split(new string[2] { "\r\n", "\n" }, StringSplitOptions.None);
		foreach (string text2 in array)
		{
			string text3 = text2.Trim();
			if (text3.Length == 0)
			{
				continue;
			}
			if (text3.StartsWith("X-WR-CALNAME:", StringComparison.OrdinalIgnoreCase))
			{
				text = text3.Substring(13).Trim();
			}
			else if (text3.Equals("BEGIN:VALARM", StringComparison.OrdinalIgnoreCase))
			{
				flag = true;
			}
			else if (text3.Equals("END:VALARM", StringComparison.OrdinalIgnoreCase))
			{
				flag = false;
			}
			else if (text3.Equals("BEGIN:VEVENT", StringComparison.OrdinalIgnoreCase))
			{
				sharedCalendarEvent = new LcarsCalendarBridge.SharedCalendarEvent();
				if (text.Length > 0)
				{
					sharedCalendarEvent.CalendarName = text;
				}
				flag = false;
			}
			else if (text3.Equals("END:VEVENT", StringComparison.OrdinalIgnoreCase))
			{
				if (sharedCalendarEvent != null && !string.IsNullOrWhiteSpace(sharedCalendarEvent.Uid))
				{
					list.Add(sharedCalendarEvent);
				}
				sharedCalendarEvent = null;
				flag = false;
			}
			else if (flag && sharedCalendarEvent != null && text3.StartsWith("TRIGGER:", StringComparison.OrdinalIgnoreCase))
			{
				int num = ParseIcsTriggerMinutes(text3.Substring(8).Trim());
				if (num > 0)
				{
					sharedCalendarEvent.AlarmMinutesBefore = num;
				}
			}
			else
			{
				if (sharedCalendarEvent == null)
				{
					continue;
				}
				if (text3.StartsWith("UID:", StringComparison.OrdinalIgnoreCase))
				{
					sharedCalendarEvent.Uid = text3.Substring(4).Trim();
				}
				if (text3.StartsWith("SUMMARY:", StringComparison.OrdinalIgnoreCase))
				{
					sharedCalendarEvent.Summary = text3.Substring(8).Trim();
				}
				if (text3.StartsWith("LOCATION:", StringComparison.OrdinalIgnoreCase))
				{
					sharedCalendarEvent.Location = text3.Substring(9).Trim();
				}
				if (text3.StartsWith("DTSTART", StringComparison.OrdinalIgnoreCase))
				{
					sharedCalendarEvent.StartUtc = ParseIcsUtc(text3);
				}
				if (text3.StartsWith("DTEND", StringComparison.OrdinalIgnoreCase))
				{
					sharedCalendarEvent.EndUtc = ParseIcsUtc(text3);
				}
				if (text3.StartsWith("RRULE:", StringComparison.OrdinalIgnoreCase))
				{
					sharedCalendarEvent.RecurrenceRule = text3.Substring(6).Trim();
				}
				if (text3.StartsWith("CATEGORIES:", StringComparison.OrdinalIgnoreCase))
				{
					string text4 = text3.Substring(11).Trim();
					if (text4.Length > 0)
					{
						sharedCalendarEvent.Category = text4.Split(',')[0].Trim();
					}
				}
				if (text3.StartsWith("ORGANIZER", StringComparison.OrdinalIgnoreCase))
				{
					sharedCalendarEvent.Organizer = ParseIcsValue(text3);
					sharedCalendarEvent.IsMeetingInvitation = true;
				}
				if (text3.StartsWith("ATTENDEE", StringComparison.OrdinalIgnoreCase))
				{
					sharedCalendarEvent.AttendeeMailto = ParseIcsValue(text3);
					string text5 = ParseIcsParam(text3, "PARTSTAT");
					if (text5.Length > 0)
					{
						sharedCalendarEvent.MyPartStat = text5;
					}
					sharedCalendarEvent.IsMeetingInvitation = true;
				}
			}
		}
		return list;
	}

	private static int ParseIcsTriggerMinutes(string trigger)
	{
		string text = ((trigger != null) ? trigger : "").Trim().ToUpperInvariant();
		if (text.Length == 0)
		{
			return 0;
		}
		checked
		{
			if (text.StartsWith("-PT", StringComparison.Ordinal) && text.EndsWith("M", StringComparison.Ordinal))
			{
				string s = text.Substring(3, text.Length - 4);
				int result = 0;
				if (int.TryParse(s, out result) && result > 0)
				{
					return result;
				}
			}
			if (text.StartsWith("-P", StringComparison.Ordinal) && text.IndexOf("T", StringComparison.Ordinal) > 0)
			{
				int result2 = 0;
				int result3 = 0;
				int result4 = 0;
				int num = text.IndexOf("D", StringComparison.Ordinal);
				if (num > 1)
				{
					int.TryParse(text.Substring(2, num - 2), out result2);
				}
				int num2 = text.IndexOf("T", StringComparison.Ordinal);
				if (num2 >= 0)
				{
					int num3 = text.IndexOf("H", num2, StringComparison.Ordinal);
					if (num3 > num2 + 1)
					{
						int.TryParse(text.Substring(num2 + 1, num3 - num2 - 1), out result3);
					}
					int num4 = text.IndexOf("M", StringComparison.Ordinal);
					if (num4 > num3 + 1)
					{
						int.TryParse(text.Substring(num3 + 1, num4 - num3 - 1), out result4);
					}
				}
				return result2 * 24 * 60 + result3 * 60 + result4;
			}
			return 0;
		}
	}

	private static DateTime ParseIcsUtc(string line)
	{
		int num = line.IndexOf(':');
		if (num < 0)
		{
			return DateTime.UtcNow;
		}
		string text = line.Substring(checked(num + 1)).Trim();
		try
		{
			if (text.EndsWith("Z", StringComparison.OrdinalIgnoreCase))
			{
				return DateTime.ParseExact(text, "yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToUniversalTime();
			}
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
		return DateTime.UtcNow;
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
}
