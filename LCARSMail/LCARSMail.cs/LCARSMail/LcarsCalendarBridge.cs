using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Web.Script.Serialization;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

[StandardModule]
public sealed class LcarsCalendarBridge
{
	public class SharedCalendarEvent
	{
		public string Uid;

		public string Summary;

		public string Location;

		public DateTime StartUtc;

		public DateTime EndUtc;

		public int AlarmMinutesBefore;

		public bool TaskCompleted;

		public string EwsItemId;

		public string EwsChangeKey;

		public bool EwsDirty;

		public DateTime EwsLastModifiedUtc;

		public string RecurrenceRule;

		public DateTime RecurrenceIdUtc;

		public string RecurrenceParentUid;

		public string ExDateUtcCsv;

		public string Category;

		public string CalendarName;

		public bool CalDavDirty;

		public string Organizer;

		public string AttendeeMailto;

		public string MyPartStat;

		public bool IsMeetingInvitation;

		[DebuggerNonUserCode]
		public SharedCalendarEvent()
		{
		}
	}

	private class CalDavSettingsDto
	{
		public string CalendarUrl
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string Username
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string TaskCollectionUrl
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public List<string> SecondaryCalendarUrls
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public Dictionary<string, string> CalendarUrlLabels
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string PrimaryCalendarDisplayName
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		[DebuggerNonUserCode]
		public CalDavSettingsDto()
		{
		}
	}

	public static int LastCalDavPrePushCount = 0;

	public static int LastCalDavPushCount = 0;

	public static int LastCalDavTaskCollectionPullCount = 0;

	public static int LastCalDavSecondaryCalendarPullCount = 0;

	public static int LastCalDavDiscoveredCalendars = 0;

	public static DateTime LastCalDavBidirectionalUtc = DateTime.MinValue;

	public static int LastCalDavPrimaryEventsTagged = 0;

	public static int LastCalDavSecondaryEventsTagged = 0;

	public static int LastCalDavTaskEventsTagged = 0;

	public static int LastCalDavPullMergeAdded = 0;

	public static int LastCalDavSecondaryPullMergeAdded = 0;

	public static int LastCalDavUrlLabelsStored = 0;

	public static int LastCalDavPrimaryPullMergeAdded = 0;

	public static int LastCalDavTaskPullMergeAdded = 0;

	private const string EventsFileName = "events.json";

	private const string CalDavSettingsFileName = "caldav-settings.json";

	private const string CalDavSecretKey = "caldav";

	public static int CountCalDavDirtyEvents()
	{
		int num = 0;
		foreach (SharedCalendarEvent item in LoadEvents())
		{
			if (item != null && item.CalDavDirty)
			{
				num = checked(num + 1);
			}
		}
		return num;
	}

	public static int CountDistinctCalendarNames()
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (SharedCalendarEvent item in LoadEvents())
		{
			if (item != null)
			{
				SharedCalendarEvent sharedCalendarEvent = item;
				string text = ((sharedCalendarEvent.CalendarName != null) ? sharedCalendarEvent.CalendarName : "").Trim();
				if (text.Length == 0)
				{
					text = "(default)";
				}
				hashSet.Add(text);
			}
		}
		return hashSet.Count;
	}

	public static int CountEventsWithRecurrenceRule()
	{
		int num = 0;
		foreach (SharedCalendarEvent item in LoadEvents())
		{
			if (item != null && !string.IsNullOrWhiteSpace(item.RecurrenceRule))
			{
				num = checked(num + 1);
			}
		}
		return num;
	}

	public static int CountEventsWithCategory()
	{
		int num = 0;
		foreach (SharedCalendarEvent item in LoadEvents())
		{
			if (item != null && !string.IsNullOrWhiteSpace(item.Category))
			{
				num = checked(num + 1);
			}
		}
		return num;
	}

	public static int CountEventsLinkedToEws()
	{
		int num = 0;
		foreach (SharedCalendarEvent item in LoadEvents())
		{
			if (item != null && !string.IsNullOrWhiteSpace(item.EwsItemId))
			{
				num = checked(num + 1);
			}
		}
		return num;
	}

	public static string CalendarDataRoot()
	{
		string text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LCARS", "Calendar");
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		return text;
	}

	public static List<SharedCalendarEvent> LoadEvents()
	{
		string path = Path.Combine(CalendarDataRoot(), "events.json");
		List<SharedCalendarEvent> result;
		if (!File.Exists(path))
		{
			result = new List<SharedCalendarEvent>();
		}
		else
		{
			try
			{
				JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
				List<SharedCalendarEvent> list = javaScriptSerializer.Deserialize<List<SharedCalendarEvent>>(File.ReadAllText(path, Encoding.UTF8));
				result = ((list != null) ? list : new List<SharedCalendarEvent>());
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = new List<SharedCalendarEvent>();
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}

	public static void SaveEvents(List<SharedCalendarEvent> events)
	{
		if (events != null)
		{
			JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
			File.WriteAllText(Path.Combine(CalendarDataRoot(), "events.json"), javaScriptSerializer.Serialize(events), Encoding.UTF8);
		}
	}

	public static int MergeCalDavPull(List<CalDavEvent> remote)
	{
		if (remote == null || remote.Count == 0)
		{
			return 0;
		}
		List<SharedCalendarEvent> list = LoadEvents();
		Dictionary<string, SharedCalendarEvent> dictionary = new Dictionary<string, SharedCalendarEvent>(StringComparer.OrdinalIgnoreCase);
		foreach (SharedCalendarEvent item in list)
		{
			if (item != null && !string.IsNullOrWhiteSpace(item.Uid))
			{
				dictionary[item.Uid] = item;
			}
		}
		int num = 0;
		foreach (CalDavEvent item2 in remote)
		{
			if (item2 == null)
			{
				continue;
			}
			CalDavEvent calDavEvent = item2;
			string text = ((calDavEvent.Uid != null) ? calDavEvent.Uid : "").Trim();
			if (text.Length == 0)
			{
				text = Guid.NewGuid().ToString();
			}
			SharedCalendarEvent sharedCalendarEvent = null;
			if (dictionary.ContainsKey(text))
			{
				sharedCalendarEvent = dictionary[text];
			}
			if (sharedCalendarEvent == null || !sharedCalendarEvent.CalDavDirty)
			{
				SharedCalendarEvent sharedCalendarEvent2 = new SharedCalendarEvent();
				sharedCalendarEvent2.Uid = text;
				calDavEvent = item2;
				sharedCalendarEvent2.Summary = ((calDavEvent.Summary != null) ? calDavEvent.Summary : "");
				sharedCalendarEvent2.StartUtc = item2.StartUtc;
				sharedCalendarEvent2.EndUtc = item2.EndUtc;
				sharedCalendarEvent2.AlarmMinutesBefore = item2.AlarmMinutesBefore;
				sharedCalendarEvent2.TaskCompleted = item2.TaskCompleted;
				CalDavEvent calDavEvent2 = item2;
				sharedCalendarEvent2.Location = ((calDavEvent2.Location != null) ? calDavEvent2.Location : "");
				CalDavEvent calDavEvent3 = item2;
				sharedCalendarEvent2.RecurrenceRule = ((calDavEvent3.RecurrenceRule != null) ? calDavEvent3.RecurrenceRule : "");
				CalDavEvent calDavEvent4 = item2;
				sharedCalendarEvent2.Category = ((calDavEvent4.Category != null) ? calDavEvent4.Category : "");
				CalDavEvent calDavEvent5 = item2;
				sharedCalendarEvent2.CalendarName = ((calDavEvent5.CalendarName != null) ? calDavEvent5.CalendarName : "");
				sharedCalendarEvent2.CalDavDirty = false;
				CalDavEvent calDavEvent6 = item2;
				sharedCalendarEvent2.Organizer = ((calDavEvent6.Organizer != null) ? calDavEvent6.Organizer : "");
				CalDavEvent calDavEvent7 = item2;
				sharedCalendarEvent2.AttendeeMailto = ((calDavEvent7.AttendeeMailto != null) ? calDavEvent7.AttendeeMailto : "");
				CalDavEvent calDavEvent8 = item2;
				sharedCalendarEvent2.MyPartStat = ((calDavEvent8.MyPartStat != null) ? calDavEvent8.MyPartStat : "");
				sharedCalendarEvent2.IsMeetingInvitation = item2.IsMeetingInvitation;
				CalDavEvent calDavEvent9 = item2;
				sharedCalendarEvent2.ExDateUtcCsv = ((calDavEvent9.ExDateUtcCsv != null) ? calDavEvent9.ExDateUtcCsv : "");
				SharedCalendarEvent sharedCalendarEvent3 = sharedCalendarEvent2;
				if (sharedCalendarEvent != null)
				{
					sharedCalendarEvent3.EwsItemId = sharedCalendarEvent.EwsItemId;
					sharedCalendarEvent3.EwsChangeKey = sharedCalendarEvent.EwsChangeKey;
					sharedCalendarEvent3.EwsDirty = sharedCalendarEvent.EwsDirty;
					sharedCalendarEvent3.EwsLastModifiedUtc = sharedCalendarEvent.EwsLastModifiedUtc;
				}
				if (!dictionary.ContainsKey(text))
				{
					num = checked(num + 1);
				}
				dictionary[text] = sharedCalendarEvent3;
			}
		}
		SaveEvents(new List<SharedCalendarEvent>(dictionary.Values));
		return num;
	}

	public static void SaveCalDavSettings(string calendarUrl, string username, string password)
	{
		string text = ((calendarUrl != null) ? calendarUrl : "").Trim();
		if (text.Length == 0)
		{
			return;
		}
		try
		{
			CalDavSettingsDto calDavSettingsDto = new CalDavSettingsDto();
			calDavSettingsDto.CalendarUrl = text;
			calDavSettingsDto.Username = ((username != null) ? username : "").Trim();
			CalDavSettingsDto obj = calDavSettingsDto;
			JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
			File.WriteAllText(CalDavSettingsPath(), javaScriptSerializer.Serialize(obj), Encoding.UTF8);
			if (password != null)
			{
				AccountStore.SavePassword("caldav", password);
			}
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
	}

	public static bool TryLoadCalDavSettings(ref string calendarUrl, ref string username, ref string password)
	{
		calendarUrl = null;
		username = null;
		password = null;
		bool result;
		if (!File.Exists(CalDavSettingsPath()))
		{
			result = false;
		}
		else
		{
			try
			{
				JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
				CalDavSettingsDto calDavSettingsDto = javaScriptSerializer.Deserialize<CalDavSettingsDto>(File.ReadAllText(CalDavSettingsPath(), Encoding.UTF8));
				if (calDavSettingsDto == null || string.IsNullOrWhiteSpace(calDavSettingsDto.CalendarUrl))
				{
					result = false;
				}
				else
				{
					calendarUrl = calDavSettingsDto.CalendarUrl.Trim();
					string username2 = calDavSettingsDto.Username;
					username = ((username2 != null) ? username2 : "").Trim();
					result = AccountStore.TryGetPassword("caldav", ref password);
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

	public static bool TryAutoDiscoverAndSaveTaskCollectionUrl()
	{
		string calendarUrl = null;
		string username = null;
		string password = null;
		if (!TryLoadCalDavSettings(ref calendarUrl, ref username, ref password))
		{
			return false;
		}
		if (TryLoadCalDavTaskCollectionUrl().Length > 0)
		{
			return false;
		}
		string text = CalDavSyncService.TryDiscoverVtodoCollectionUrl(calendarUrl, username, password);
		if (text.Length == 0)
		{
			return false;
		}
		TrySaveCalDavTaskCollectionUrl(text);
		return true;
	}

	public static bool TryEnsureTaskCollectionUrlDiscovered()
	{
		if (TryLoadCalDavTaskCollectionUrl().Length > 0)
		{
			return true;
		}
		return TryAutoDiscoverAndSaveTaskCollectionUrl();
	}

	private static void TrySaveCalDavTaskCollectionUrl(string taskCollectionUrl)
	{
		string text = ((taskCollectionUrl != null) ? taskCollectionUrl : "").Trim();
		if (text.Length == 0)
		{
			return;
		}
		try
		{
			string path = CalDavSettingsPath();
			if (File.Exists(path))
			{
				JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
				CalDavSettingsDto calDavSettingsDto = javaScriptSerializer.Deserialize<CalDavSettingsDto>(File.ReadAllText(path, Encoding.UTF8));
				if (calDavSettingsDto != null)
				{
					calDavSettingsDto.TaskCollectionUrl = text;
					File.WriteAllText(path, javaScriptSerializer.Serialize(calDavSettingsDto), Encoding.UTF8);
				}
			}
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
	}

	public static int TryRefreshDiscoveredVeventCalendarsFromServer(string calendarUrl, string username, string password)
	{
		LastCalDavDiscoveredCalendars = 0;
		List<CalDavCollectionDescriptor> list = CalDavSyncService.EnumerateVeventCalendarDescriptors(calendarUrl, username, password);
		if (list == null || list.Count == 0)
		{
			return 0;
		}
		List<string> list2 = new List<string>();
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (CalDavCollectionDescriptor item in list)
		{
			if (item != null && !string.IsNullOrWhiteSpace(item.Url))
			{
				string text = item.Url.Trim();
				list2.Add(text);
				CalDavCollectionDescriptor calDavCollectionDescriptor = item;
				string text2 = ((calDavCollectionDescriptor.DisplayName != null) ? calDavCollectionDescriptor.DisplayName : "").Trim();
				if (text2.Length > 0)
				{
					dictionary[text] = text2;
				}
			}
		}
		string text3 = calendarUrl.Trim();
		if (text3.Length > 0 && !dictionary.ContainsKey(text3))
		{
			string text4 = CalDavSyncService.TryGetCollectionDisplayName(text3, username, password);
			if (text4.Length > 0)
			{
				dictionary[text3] = text4;
			}
		}
		TrySaveSecondaryCalendarUrls(list2, dictionary);
		LastCalDavDiscoveredCalendars = list2.Count;
		return list2.Count;
	}

	private static void TrySaveSecondaryCalendarUrls(List<string> calendarUrls, Dictionary<string, string> urlLabels = null)
	{
		if (calendarUrls == null || calendarUrls.Count == 0)
		{
			return;
		}
		try
		{
			string path = CalDavSettingsPath();
			if (!File.Exists(path))
			{
				return;
			}
			JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
			CalDavSettingsDto calDavSettingsDto = javaScriptSerializer.Deserialize<CalDavSettingsDto>(File.ReadAllText(path, Encoding.UTF8));
			if (calDavSettingsDto == null)
			{
				return;
			}
			calDavSettingsDto.SecondaryCalendarUrls = calendarUrls;
			if (urlLabels != null && urlLabels.Count > 0)
			{
				calDavSettingsDto.CalendarUrlLabels = urlLabels;
				if (calDavSettingsDto.CalendarUrl != null && calDavSettingsDto.CalendarUrl.Trim().Length > 0)
				{
					string key = calDavSettingsDto.CalendarUrl.Trim();
					string value = null;
					if (urlLabels.TryGetValue(key, out value) && value != null && value.Trim().Length > 0)
					{
						calDavSettingsDto.PrimaryCalendarDisplayName = value.Trim();
					}
				}
			}
			File.WriteAllText(path, javaScriptSerializer.Serialize(calDavSettingsDto), Encoding.UTF8);
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
	}

	private static string TryLoadPrimaryCalendarDisplayName()
	{
		string result;
		try
		{
			if (!File.Exists(CalDavSettingsPath()))
			{
				result = "";
			}
			else
			{
				JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
				CalDavSettingsDto calDavSettingsDto = javaScriptSerializer.Deserialize<CalDavSettingsDto>(File.ReadAllText(CalDavSettingsPath(), Encoding.UTF8));
				if (calDavSettingsDto == null)
				{
					result = "";
				}
				else
				{
					string primaryCalendarDisplayName = calDavSettingsDto.PrimaryCalendarDisplayName;
					result = ((primaryCalendarDisplayName != null) ? primaryCalendarDisplayName : "").Trim();
				}
			}
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			result = "";
			ProjectData.ClearProjectError();
		}
		return result;
	}

	public static int CountStoredCalendarUrlLabels()
	{
		int result;
		try
		{
			if (!File.Exists(CalDavSettingsPath()))
			{
				result = 0;
			}
			else
			{
				JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
				CalDavSettingsDto calDavSettingsDto = javaScriptSerializer.Deserialize<CalDavSettingsDto>(File.ReadAllText(CalDavSettingsPath(), Encoding.UTF8));
				result = ((calDavSettingsDto != null && calDavSettingsDto.CalendarUrlLabels != null) ? calDavSettingsDto.CalendarUrlLabels.Count : 0);
			}
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			result = 0;
			ProjectData.ClearProjectError();
		}
		return result;
	}

	public static string TryResolveCalendarDisplayLabel(string calendarNameOrUrl)
	{
		string text = ((calendarNameOrUrl != null) ? calendarNameOrUrl : "").Trim();
		if (text.Length == 0)
		{
			return "";
		}
		try
		{
			if (!File.Exists(CalDavSettingsPath()))
			{
				return text;
			}
			JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
			CalDavSettingsDto calDavSettingsDto = javaScriptSerializer.Deserialize<CalDavSettingsDto>(File.ReadAllText(CalDavSettingsPath(), Encoding.UTF8));
			if (calDavSettingsDto == null || calDavSettingsDto.CalendarUrlLabels == null)
			{
				return text;
			}
			foreach (KeyValuePair<string, string> calendarUrlLabel in calDavSettingsDto.CalendarUrlLabels)
			{
				string key = calendarUrlLabel.Key;
				string text2 = ((key != null) ? key : "").Trim();
				if (text2.Length == 0)
				{
					continue;
				}
				string fileName = Path.GetFileName(text2.Trim().TrimEnd('/'));
				if (text.Equals(text2, StringComparison.OrdinalIgnoreCase) || text.Equals(fileName, StringComparison.OrdinalIgnoreCase))
				{
					key = calendarUrlLabel.Value;
					string text3 = ((key != null) ? key : "").Trim();
					if (text3.Length > 0)
					{
						return text3;
					}
				}
			}
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
		return text;
	}

	private static List<string> TryLoadSecondaryCalendarUrls()
	{
		List<string> result;
		try
		{
			if (!File.Exists(CalDavSettingsPath()))
			{
				result = new List<string>();
			}
			else
			{
				JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
				CalDavSettingsDto calDavSettingsDto = javaScriptSerializer.Deserialize<CalDavSettingsDto>(File.ReadAllText(CalDavSettingsPath(), Encoding.UTF8));
				result = ((calDavSettingsDto != null && calDavSettingsDto.SecondaryCalendarUrls != null) ? calDavSettingsDto.SecondaryCalendarUrls : new List<string>());
			}
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			result = new List<string>();
			ProjectData.ClearProjectError();
		}
		return result;
	}

	public static int TrySyncStoredCalDav()
	{
		string calendarUrl = null;
		string username = null;
		string password = null;
		if (!TryLoadCalDavSettings(ref calendarUrl, ref username, ref password))
		{
			return 0;
		}
		TryRefreshDiscoveredVeventCalendarsFromServer(calendarUrl, username, password);
		LastCalDavUrlLabelsStored = CountStoredCalendarUrlLabels();
		TryAutoDiscoverAndSaveTaskCollectionUrl();
		List<CalDavEvent> list = CalDavSyncService.FetchEvents(calendarUrl, username, password);
		if (list == null)
		{
			return -1;
		}
		string text = TryResolveCalendarDisplayLabel(calendarUrl.Trim());
		if (text.Length == 0)
		{
			text = TryLoadPrimaryCalendarDisplayName();
		}
		LastCalDavPrimaryEventsTagged = 0;
		checked
		{
			foreach (CalDavEvent item in list)
			{
				if (item != null && string.IsNullOrWhiteSpace(item.CalendarName) && text.Length > 0)
				{
					item.CalendarName = text;
					LastCalDavPrimaryEventsTagged++;
				}
			}
			int num = MergeCalDavPull(list);
			LastCalDavPrimaryPullMergeAdded = Math.Max(0, num);
			LastCalDavSecondaryCalendarPullCount = 0;
			LastCalDavSecondaryEventsTagged = 0;
			LastCalDavSecondaryPullMergeAdded = 0;
			foreach (string item2 in TryLoadSecondaryCalendarUrls())
			{
				if (string.IsNullOrWhiteSpace(item2) || string.Equals(item2.Trim(), calendarUrl.Trim(), StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				List<CalDavEvent> list2 = CalDavSyncService.FetchEvents(item2, username, password);
				if (list2 == null || list2.Count == 0)
				{
					continue;
				}
				string text2 = TryResolveCalendarDisplayLabel(item2.Trim());
				if (text2.Length == 0)
				{
					text2 = Path.GetFileName(item2.Trim().TrimEnd('/'));
				}
				foreach (CalDavEvent item3 in list2)
				{
					if (item3 != null && string.IsNullOrWhiteSpace(item3.CalendarName) && text2.Length > 0)
					{
						item3.CalendarName = text2;
						LastCalDavSecondaryEventsTagged++;
					}
					else if (item3 != null && string.IsNullOrWhiteSpace(item3.CalendarName))
					{
						item3.CalendarName = text2;
					}
				}
				LastCalDavSecondaryCalendarPullCount += list2.Count;
				int num2 = num;
				num += MergeCalDavPull(list2);
				LastCalDavSecondaryPullMergeAdded += Math.Max(0, num - num2);
			}
			string text3 = TryLoadCalDavTaskCollectionUrl();
			if (text3.Length > 0 && !string.Equals(text3, calendarUrl.Trim(), StringComparison.OrdinalIgnoreCase))
			{
				List<CalDavEvent> list3 = CalDavSyncService.FetchEvents(text3, username, password);
				if (list3 == null || list3.Count == 0)
				{
					string text4 = CalDavSyncService.TryDiscoverVtodoCollectionUrl(calendarUrl, username, password);
					if (text4.Length > 0 && !string.Equals(text4, text3, StringComparison.OrdinalIgnoreCase))
					{
						TrySaveCalDavTaskCollectionUrl(text4);
						text3 = text4;
						list3 = CalDavSyncService.FetchEvents(text3, username, password);
					}
				}
				if (list3 != null && list3.Count > 0)
				{
					LastCalDavTaskCollectionPullCount = list3.Count;
					LastCalDavTaskEventsTagged = 0;
					string text5 = TryResolveCalendarDisplayLabel(text3.Trim());
					if (text5.Length == 0)
					{
						text5 = "VTODO";
					}
					foreach (CalDavEvent item4 in list3)
					{
						if (item4 != null && string.IsNullOrWhiteSpace(item4.CalendarName) && text5.Length > 0)
						{
							item4.CalendarName = text5;
							LastCalDavTaskEventsTagged++;
						}
						else if (item4 != null && string.IsNullOrWhiteSpace(item4.CalendarName))
						{
							item4.CalendarName = text5;
						}
					}
					int num3 = num;
					num += MergeCalDavPull(list3);
					LastCalDavTaskPullMergeAdded = Math.Max(0, num - num3);
				}
			}
			LastCalDavPullMergeAdded = Math.Max(0, num);
			return num;
		}
	}

	private static string TryLoadCalDavTaskCollectionUrl()
	{
		string result;
		try
		{
			if (!File.Exists(CalDavSettingsPath()))
			{
				result = "";
			}
			else
			{
				JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
				CalDavSettingsDto calDavSettingsDto = javaScriptSerializer.Deserialize<CalDavSettingsDto>(File.ReadAllText(CalDavSettingsPath(), Encoding.UTF8));
				if (calDavSettingsDto == null)
				{
					result = "";
				}
				else
				{
					string taskCollectionUrl = calDavSettingsDto.TaskCollectionUrl;
					result = ((taskCollectionUrl != null) ? taskCollectionUrl : "").Trim();
				}
			}
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			result = "";
			ProjectData.ClearProjectError();
		}
		return result;
	}

	public static int TrySyncStoredCalDavBidirectional()
	{
		LastCalDavPrePushCount = 0;
		LastCalDavPushCount = 0;
		TryEnsureTaskCollectionUrlDiscovered();
		string calendarUrl = null;
		string username = null;
		string password = null;
		if (TryLoadCalDavSettings(ref calendarUrl, ref username, ref password))
		{
			LastCalDavPrePushCount = Math.Max(0, PushLocalEventsToCalDav(calendarUrl, username, password));
		}
		int num = TrySyncStoredCalDav();
		if (num < 0)
		{
			return num;
		}
		if (!TryLoadCalDavSettings(ref calendarUrl, ref username, ref password))
		{
			return Math.Max(num, LastCalDavPrePushCount);
		}
		int val = PushLocalEventsToCalDav(calendarUrl, username, password);
		LastCalDavPushCount = Math.Max(LastCalDavPrePushCount, Math.Max(0, val));
		LastCalDavBidirectionalUtc = DateTime.UtcNow;
		return Math.Max(num, LastCalDavPushCount);
	}

	public static bool TryPushTaskCompletion(SharedCalendarEvent ev)
	{
		if (ev == null || string.IsNullOrWhiteSpace(ev.Uid))
		{
			return false;
		}
		string calendarUrl = null;
		string username = null;
		string password = null;
		if (!TryLoadCalDavSettings(ref calendarUrl, ref username, ref password))
		{
			return false;
		}
		CalDavEvent calDavEvent = new CalDavEvent();
		calDavEvent.Uid = ev.Uid;
		calDavEvent.Summary = ev.Summary;
		calDavEvent.StartUtc = ev.StartUtc;
		calDavEvent.EndUtc = ev.EndUtc;
		calDavEvent.TaskCompleted = ev.TaskCompleted;
		CalDavEvent ev2 = calDavEvent;
		return CalDavSyncService.PutTask(calendarUrl, username, password, ev2);
	}

	private static string ResolveCalDavPushUrl(string primaryUrl, SharedCalendarEvent ev, string taskCollectionUrl)
	{
		string result = ((primaryUrl != null) ? primaryUrl : "").Trim();
		if (ev == null)
		{
			return result;
		}
		SharedCalendarEvent sharedCalendarEvent = ev;
		string text = ((sharedCalendarEvent.Summary != null) ? sharedCalendarEvent.Summary : "");
		if (text.IndexOf("[TASK]", StringComparison.OrdinalIgnoreCase) >= 0 && taskCollectionUrl.Length > 0)
		{
			return taskCollectionUrl;
		}
		sharedCalendarEvent = ev;
		string text2 = ((sharedCalendarEvent.CalendarName != null) ? sharedCalendarEvent.CalendarName : "").Trim();
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

	public static int PushLocalEventsToCalDav(string calendarUrl, string username, string password)
	{
		int num = 0;
		string taskCollectionUrl = TryLoadCalDavTaskCollectionUrl();
		List<SharedCalendarEvent> list = LoadEvents();
		list.Sort([SpecialName] (SharedCalendarEvent a, SharedCalendarEvent b) =>
		{
			if (a == null && b == null)
			{
				return 0;
			}
			if (a == null)
			{
				return 1;
			}
			if (b == null)
			{
				return -1;
			}
			if (a.CalDavDirty == b.CalDavDirty)
			{
				SharedCalendarEvent sharedCalendarEvent10 = a;
				string strA = ((sharedCalendarEvent10.Uid != null) ? sharedCalendarEvent10.Uid : "");
				sharedCalendarEvent10 = b;
				string strB = ((sharedCalendarEvent10.Uid != null) ? sharedCalendarEvent10.Uid : "");
				return string.Compare(strA, strB, StringComparison.OrdinalIgnoreCase);
			}
			return (!a.CalDavDirty) ? 1 : (-1);
		});
		foreach (SharedCalendarEvent item in list)
		{
			if (item != null && !string.IsNullOrWhiteSpace(item.Uid) && item.CalDavDirty)
			{
				CalDavEvent calDavEvent = new CalDavEvent();
				calDavEvent.Uid = item.Uid;
				calDavEvent.Summary = item.Summary;
				calDavEvent.StartUtc = item.StartUtc;
				calDavEvent.EndUtc = item.EndUtc;
				calDavEvent.AlarmMinutesBefore = item.AlarmMinutesBefore;
				calDavEvent.TaskCompleted = item.TaskCompleted;
				SharedCalendarEvent sharedCalendarEvent = item;
				calDavEvent.Location = ((sharedCalendarEvent.Location != null) ? sharedCalendarEvent.Location : "");
				SharedCalendarEvent sharedCalendarEvent2 = item;
				calDavEvent.RecurrenceRule = ((sharedCalendarEvent2.RecurrenceRule != null) ? sharedCalendarEvent2.RecurrenceRule : "");
				SharedCalendarEvent sharedCalendarEvent3 = item;
				calDavEvent.Category = ((sharedCalendarEvent3.Category != null) ? sharedCalendarEvent3.Category : "");
				SharedCalendarEvent sharedCalendarEvent4 = item;
				calDavEvent.CalendarName = ((sharedCalendarEvent4.CalendarName != null) ? sharedCalendarEvent4.CalendarName : "");
				SharedCalendarEvent sharedCalendarEvent5 = item;
				calDavEvent.Organizer = ((sharedCalendarEvent5.Organizer != null) ? sharedCalendarEvent5.Organizer : "");
				SharedCalendarEvent sharedCalendarEvent6 = item;
				calDavEvent.AttendeeMailto = ((sharedCalendarEvent6.AttendeeMailto != null) ? sharedCalendarEvent6.AttendeeMailto : "");
				SharedCalendarEvent sharedCalendarEvent7 = item;
				calDavEvent.MyPartStat = ((sharedCalendarEvent7.MyPartStat != null) ? sharedCalendarEvent7.MyPartStat : "");
				calDavEvent.IsMeetingInvitation = item.IsMeetingInvitation;
				calDavEvent.RecurrenceIdUtc = item.RecurrenceIdUtc;
				SharedCalendarEvent sharedCalendarEvent8 = item;
				calDavEvent.RecurrenceParentUid = ((sharedCalendarEvent8.RecurrenceParentUid != null) ? sharedCalendarEvent8.RecurrenceParentUid : "");
				SharedCalendarEvent sharedCalendarEvent9 = item;
				calDavEvent.ExDateUtcCsv = ((sharedCalendarEvent9.ExDateUtcCsv != null) ? sharedCalendarEvent9.ExDateUtcCsv : "");
				CalDavEvent ev = calDavEvent;
				sharedCalendarEvent9 = item;
				string text = ((sharedCalendarEvent9.Summary != null) ? sharedCalendarEvent9.Summary : "");
				string calendarUrl2 = ResolveCalDavPushUrl(calendarUrl, item, taskCollectionUrl);
				bool flag = false;
				if ((text.IndexOf("[TASK]", StringComparison.OrdinalIgnoreCase) < 0) ? CalDavSyncService.PutEvent(calendarUrl2, username, password, ev) : CalDavSyncService.PutTask(calendarUrl2, username, password, ev))
				{
					item.CalDavDirty = false;
					num = checked(num + 1);
				}
			}
		}
		if (num > 0)
		{
			SaveEvents(list);
		}
		return num;
	}

	public static int TryApplyThunderbirdCalDavFromProfile(string profileDir)
	{
		List<ThunderbirdCardDavAccount> list = ThunderbirdCardDavImporter.TryLoadCalDavFromProfile(profileDir);
		if (list == null || list.Count == 0)
		{
			return 0;
		}
		ThunderbirdCardDavAccount thunderbirdCardDavAccount = list[0];
		if (thunderbirdCardDavAccount == null || string.IsNullOrWhiteSpace(thunderbirdCardDavAccount.ServerUrl))
		{
			return 0;
		}
		string serverUrl = thunderbirdCardDavAccount.ServerUrl;
		ThunderbirdCardDavAccount thunderbirdCardDavAccount2 = thunderbirdCardDavAccount;
		SaveCalDavSettings(serverUrl, (thunderbirdCardDavAccount2.Username != null) ? thunderbirdCardDavAccount2.Username : "", null);
		return 1;
	}

	private static string CalDavSettingsPath()
	{
		return Path.Combine(AccountStore.DataRoot(), "caldav-settings.json");
	}
}
