using System;
using System.Collections.Generic;

namespace LCARSMail;

public sealed class EwsCalendarConflictHelper
{
	private EwsCalendarConflictHelper()
	{
	}

	public static int CountDirtyEwsCalendarEvents()
	{
		int num = 0;
		foreach (LcarsCalendarBridge.SharedCalendarEvent item in LcarsCalendarBridge.LoadEvents())
		{
			if (item != null && item.EwsDirty && !string.IsNullOrWhiteSpace(item.EwsItemId))
			{
				num = checked(num + 1);
			}
		}
		return num;
	}

	public static bool TryRefreshLocalFromServer(string ewsUrl, string username, string password, string uid, string oauthToken = null)
	{
		string text = ((uid != null) ? uid : "").Trim();
		if (text.Length == 0)
		{
			return false;
		}
		List<LcarsCalendarBridge.SharedCalendarEvent> list = LcarsCalendarBridge.LoadEvents();
		LcarsCalendarBridge.SharedCalendarEvent sharedCalendarEvent = null;
		foreach (LcarsCalendarBridge.SharedCalendarEvent item in list)
		{
			if (item != null && string.Equals(item.Uid, text, StringComparison.OrdinalIgnoreCase))
			{
				sharedCalendarEvent = item;
				break;
			}
		}
		if (sharedCalendarEvent == null || string.IsNullOrWhiteSpace(sharedCalendarEvent.EwsItemId))
		{
			return false;
		}
		EwsMailService.EwsCalendarEventDetails ewsCalendarEventDetails = EwsMailService.TryGetCalendarEventDetails(ewsUrl, username, password, sharedCalendarEvent.EwsItemId, oauthToken);
		if (ewsCalendarEventDetails == null)
		{
			return false;
		}
		LcarsCalendarBridge.SharedCalendarEvent sharedCalendarEvent2 = sharedCalendarEvent;
		EwsMailService.EwsCalendarEventDetails ewsCalendarEventDetails2 = ewsCalendarEventDetails;
		sharedCalendarEvent2.Summary = ((ewsCalendarEventDetails2.Subject != null) ? ewsCalendarEventDetails2.Subject : sharedCalendarEvent.Summary);
		LcarsCalendarBridge.SharedCalendarEvent sharedCalendarEvent3 = sharedCalendarEvent;
		ewsCalendarEventDetails2 = ewsCalendarEventDetails;
		sharedCalendarEvent3.Location = ((ewsCalendarEventDetails2.Location != null) ? ewsCalendarEventDetails2.Location : sharedCalendarEvent.Location);
		sharedCalendarEvent.StartUtc = ewsCalendarEventDetails.StartUtc;
		sharedCalendarEvent.EndUtc = ewsCalendarEventDetails.EndUtc;
		LcarsCalendarBridge.SharedCalendarEvent sharedCalendarEvent4 = sharedCalendarEvent;
		ewsCalendarEventDetails2 = ewsCalendarEventDetails;
		sharedCalendarEvent4.Category = ((ewsCalendarEventDetails2.Category != null) ? ewsCalendarEventDetails2.Category : sharedCalendarEvent.Category);
		sharedCalendarEvent.AlarmMinutesBefore = ewsCalendarEventDetails.AlarmMinutesBefore;
		LcarsCalendarBridge.SharedCalendarEvent sharedCalendarEvent5 = sharedCalendarEvent;
		ewsCalendarEventDetails2 = ewsCalendarEventDetails;
		sharedCalendarEvent5.EwsChangeKey = ((ewsCalendarEventDetails2.ChangeKey != null) ? ewsCalendarEventDetails2.ChangeKey : sharedCalendarEvent.EwsChangeKey);
		if (DateTime.Compare(ewsCalendarEventDetails.LastModifiedUtc, DateTime.MinValue) != 0)
		{
			sharedCalendarEvent.EwsLastModifiedUtc = ewsCalendarEventDetails.LastModifiedUtc;
		}
		sharedCalendarEvent.EwsDirty = false;
		sharedCalendarEvent.CalDavDirty = true;
		LcarsCalendarBridge.SaveEvents(list);
		return true;
	}

	public static int TryRefreshAllDirtyFromServer(string ewsUrl, string username, string password, string oauthToken = null)
	{
		int num = 0;
		foreach (LcarsCalendarBridge.SharedCalendarEvent item in LcarsCalendarBridge.LoadEvents())
		{
			if (item != null && item.EwsDirty && !string.IsNullOrWhiteSpace(item.Uid) && TryRefreshLocalFromServer(ewsUrl, username, password, item.Uid, oauthToken))
			{
				num = checked(num + 1);
			}
		}
		return num;
	}

	public static int TryAutoReconcileUnchangedDirty(string ewsUrl, string username, string password, string oauthToken = null)
	{
		int num = 0;
		List<LcarsCalendarBridge.SharedCalendarEvent> list = LcarsCalendarBridge.LoadEvents();
		if (list == null)
		{
			return 0;
		}
		foreach (LcarsCalendarBridge.SharedCalendarEvent item in list)
		{
			if (item == null || !item.EwsDirty || string.IsNullOrWhiteSpace(item.EwsItemId))
			{
				continue;
			}
			EwsMailService.EwsCalendarEventDetails ewsCalendarEventDetails = EwsMailService.TryGetCalendarEventDetails(ewsUrl, username, password, item.EwsItemId, oauthToken);
			if (ewsCalendarEventDetails != null && FieldsMatchServer(item, ewsCalendarEventDetails))
			{
				if (ewsCalendarEventDetails.ChangeKey.Length > 0)
				{
					item.EwsChangeKey = ewsCalendarEventDetails.ChangeKey;
				}
				if (DateTime.Compare(ewsCalendarEventDetails.LastModifiedUtc, DateTime.MinValue) != 0)
				{
					item.EwsLastModifiedUtc = ewsCalendarEventDetails.LastModifiedUtc;
				}
				item.EwsDirty = false;
				num = checked(num + 1);
			}
		}
		if (num > 0)
		{
			LcarsCalendarBridge.SaveEvents(list);
		}
		return num;
	}

	public static int TryRefreshChangeKeysForDirtyEvents(string ewsUrl, string username, string password, string oauthToken = null)
	{
		int num = 0;
		List<LcarsCalendarBridge.SharedCalendarEvent> list = LcarsCalendarBridge.LoadEvents();
		if (list == null)
		{
			return 0;
		}
		foreach (LcarsCalendarBridge.SharedCalendarEvent item in list)
		{
			if (item == null || !item.EwsDirty || string.IsNullOrWhiteSpace(item.EwsItemId))
			{
				continue;
			}
			EwsMailService.EwsCalendarEventDetails ewsCalendarEventDetails = EwsMailService.TryGetCalendarEventDetails(ewsUrl, username, password, item.EwsItemId, oauthToken);
			if (ewsCalendarEventDetails != null)
			{
				if (ewsCalendarEventDetails.ChangeKey.Length > 0)
				{
					item.EwsChangeKey = ewsCalendarEventDetails.ChangeKey;
				}
				if (DateTime.Compare(ewsCalendarEventDetails.LastModifiedUtc, DateTime.MinValue) != 0)
				{
					item.EwsLastModifiedUtc = ewsCalendarEventDetails.LastModifiedUtc;
				}
				num = checked(num + 1);
			}
		}
		if (num > 0)
		{
			LcarsCalendarBridge.SaveEvents(list);
		}
		return num;
	}

	public static bool TryReconcileSingleIfServerMatchesLocal(LcarsCalendarBridge.SharedCalendarEvent ev, EwsMailService.EwsCalendarEventDetails detail)
	{
		if (ev == null || detail == null)
		{
			return false;
		}
		if (!FieldsMatchServer(ev, detail))
		{
			return false;
		}
		if (detail.ChangeKey.Length > 0)
		{
			ev.EwsChangeKey = detail.ChangeKey;
		}
		if (DateTime.Compare(detail.LastModifiedUtc, DateTime.MinValue) != 0)
		{
			ev.EwsLastModifiedUtc = detail.LastModifiedUtc;
		}
		ev.EwsDirty = false;
		return true;
	}

	private static bool FieldsMatchServer(LcarsCalendarBridge.SharedCalendarEvent ev, EwsMailService.EwsCalendarEventDetails detail)
	{
		if (ev == null || detail == null)
		{
			return false;
		}
		LcarsCalendarBridge.SharedCalendarEvent sharedCalendarEvent = ev;
		string a = ((sharedCalendarEvent.Summary != null) ? sharedCalendarEvent.Summary : "").Trim();
		EwsMailService.EwsCalendarEventDetails ewsCalendarEventDetails = detail;
		string b = ((ewsCalendarEventDetails.Subject != null) ? ewsCalendarEventDetails.Subject : "").Trim();
		if (!string.Equals(a, b, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		sharedCalendarEvent = ev;
		string a2 = ((sharedCalendarEvent.Location != null) ? sharedCalendarEvent.Location : "").Trim();
		ewsCalendarEventDetails = detail;
		string b2 = ((ewsCalendarEventDetails.Location != null) ? ewsCalendarEventDetails.Location : "").Trim();
		if (!string.Equals(a2, b2, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (DateTime.Compare(ev.StartUtc, DateTime.MinValue) != 0 && DateTime.Compare(detail.StartUtc, DateTime.MinValue) != 0 && Math.Abs((ev.StartUtc - detail.StartUtc).TotalMinutes) > 1.0)
		{
			return false;
		}
		if (DateTime.Compare(ev.EndUtc, DateTime.MinValue) != 0 && DateTime.Compare(detail.EndUtc, DateTime.MinValue) != 0 && Math.Abs((ev.EndUtc - detail.EndUtc).TotalMinutes) > 1.0)
		{
			return false;
		}
		sharedCalendarEvent = ev;
		string a3 = ((sharedCalendarEvent.Category != null) ? sharedCalendarEvent.Category : "").Trim();
		ewsCalendarEventDetails = detail;
		string b3 = ((ewsCalendarEventDetails.Category != null) ? ewsCalendarEventDetails.Category : "").Trim();
		if (!string.Equals(a3, b3, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (ev.AlarmMinutesBefore != detail.AlarmMinutesBefore)
		{
			return false;
		}
		sharedCalendarEvent = ev;
		bool flag = !string.IsNullOrWhiteSpace((sharedCalendarEvent.RecurrenceRule != null) ? sharedCalendarEvent.RecurrenceRule : "");
		if (flag != detail.HasRecurrence)
		{
			return false;
		}
		if (flag && detail.HasRecurrence)
		{
			sharedCalendarEvent = ev;
			string text = NormalizeRecurrenceToken((sharedCalendarEvent.RecurrenceRule != null) ? sharedCalendarEvent.RecurrenceRule : "");
			ewsCalendarEventDetails = detail;
			string text2 = NormalizeRecurrenceToken((ewsCalendarEventDetails.RecurrenceSignature != null) ? ewsCalendarEventDetails.RecurrenceSignature : "");
			if (text.Length > 0 && text2.Length > 0 && text2.IndexOf(text.Substring(0, Math.Min(24, text.Length)), StringComparison.OrdinalIgnoreCase) < 0 && text.IndexOf("FREQ", StringComparison.OrdinalIgnoreCase) >= 0 && text2.IndexOf("FREQ", StringComparison.OrdinalIgnoreCase) < 0)
			{
				return false;
			}
		}
		return true;
	}

	private static string NormalizeRecurrenceToken(string raw)
	{
		string text = ((raw != null) ? raw : "").Trim().ToUpperInvariant();
		if (text.StartsWith("RRULE:", StringComparison.Ordinal))
		{
			text = text.Substring(6).Trim();
		}
		return text;
	}
}
