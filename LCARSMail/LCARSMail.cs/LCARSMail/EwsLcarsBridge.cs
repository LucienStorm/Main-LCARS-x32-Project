using System;
using System.Collections.Generic;

namespace LCARSMail;

public sealed class EwsLcarsBridge
{
	public static string LastError;

	public static int LastCalendarPushCount;

	public static int LastCalendarConflictOverwrites;

	public static int LastCalendarAutoServerWins = 0;

	public static int LastCalendarAutoClientWins = 0;

	public static int LastCalendarAutoReconcileCleared = 0;

	public static int LastCalendarChangeKeyRefreshes = 0;

	public static int LastGalChangeKeyRefreshes = 0;

	public static int LastGalPersonaChangeKeyRefreshes = 0;

	public static int LastGalDirectoryChangeKeyRefreshes = 0;

	public static int LastGalPersonalPushCount;

	public static int LastGalPersonaUpdateAttempts;

	public static int LastGalDirectoryUpdateAttempts;

	public static int LastGalDirectoryResolveSuccess = 0;

	public static int LastCalendarRsvpPushCount = 0;

	private EwsLcarsBridge()
	{
	}

	public static int TryMergeCalendarFromEws(string ewsUrl, string username, string password, string oauthToken = null)
	{
		LastError = null;
		List<EwsMailService.EwsCalendarSummary> list = EwsMailService.TryFindCalendarSummaries(ewsUrl, username, password, oauthToken);
		if (list == null)
		{
			LastError = ((EwsMailService.LastError != null) ? EwsMailService.LastError : "EWS calendar list failed.");
			return -1;
		}
		List<LcarsCalendarBridge.SharedCalendarEvent> list2 = LcarsCalendarBridge.LoadEvents();
		Dictionary<string, LcarsCalendarBridge.SharedCalendarEvent> dictionary = new Dictionary<string, LcarsCalendarBridge.SharedCalendarEvent>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, LcarsCalendarBridge.SharedCalendarEvent> dictionary2 = new Dictionary<string, LcarsCalendarBridge.SharedCalendarEvent>(StringComparer.OrdinalIgnoreCase);
		foreach (LcarsCalendarBridge.SharedCalendarEvent item in list2)
		{
			if (item != null && !string.IsNullOrWhiteSpace(item.Uid))
			{
				dictionary[item.Uid] = item;
				LcarsCalendarBridge.SharedCalendarEvent sharedCalendarEvent = item;
				string text = ((sharedCalendarEvent.EwsItemId != null) ? sharedCalendarEvent.EwsItemId : "").Trim();
				if (text.Length > 0)
				{
					dictionary2[text] = item;
				}
			}
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		int num = 0;
		foreach (EwsMailService.EwsCalendarSummary item2 in list)
		{
			if (item2 == null || string.IsNullOrWhiteSpace(item2.ItemId))
			{
				continue;
			}
			hashSet.Add(item2.ItemId.Trim());
			EwsMailService.EwsCalendarEventDetails ewsCalendarEventDetails = EwsMailService.TryGetCalendarEventDetails(ewsUrl, username, password, item2.ItemId, oauthToken);
			if (ewsCalendarEventDetails == null)
			{
				continue;
			}
			EwsMailService.EwsCalendarEventDetails ewsCalendarEventDetails2 = ewsCalendarEventDetails;
			string text2 = ((ewsCalendarEventDetails2.Uid != null) ? ewsCalendarEventDetails2.Uid : "").Trim();
			if (text2.Length == 0)
			{
				text2 = item2.ItemId.Trim();
			}
			LcarsCalendarBridge.SharedCalendarEvent sharedCalendarEvent2 = null;
			if (dictionary.ContainsKey(text2))
			{
				sharedCalendarEvent2 = dictionary[text2];
			}
			else if (dictionary2.ContainsKey(item2.ItemId.Trim()))
			{
				sharedCalendarEvent2 = dictionary2[item2.ItemId.Trim()];
				text2 = sharedCalendarEvent2.Uid;
			}
			if (sharedCalendarEvent2 != null && sharedCalendarEvent2.EwsDirty)
			{
				if (DateTime.Compare(ewsCalendarEventDetails.LastModifiedUtc, DateTime.MinValue) != 0)
				{
					if (ewsCalendarEventDetails.ChangeKey.Length > 0)
					{
						sharedCalendarEvent2.EwsChangeKey = ewsCalendarEventDetails.ChangeKey;
					}
					if (DateTime.Compare(ewsCalendarEventDetails.LastModifiedUtc, sharedCalendarEvent2.EwsLastModifiedUtc) > 0)
					{
						sharedCalendarEvent2.EwsLastModifiedUtc = ewsCalendarEventDetails.LastModifiedUtc;
					}
					dictionary[text2] = sharedCalendarEvent2;
					LcarsCalendarBridge.SharedCalendarEvent sharedCalendarEvent = sharedCalendarEvent2;
					string text3 = ((sharedCalendarEvent.EwsItemId != null) ? sharedCalendarEvent.EwsItemId : "").Trim();
					if (text3.Length > 0)
					{
						dictionary2[text3] = sharedCalendarEvent2;
					}
				}
			}
			else
			{
				if (sharedCalendarEvent2 != null && DateTime.Compare(sharedCalendarEvent2.EwsLastModifiedUtc, DateTime.MinValue) != 0 && DateTime.Compare(ewsCalendarEventDetails.LastModifiedUtc, DateTime.MinValue) != 0 && DateTime.Compare(sharedCalendarEvent2.EwsLastModifiedUtc, ewsCalendarEventDetails.LastModifiedUtc) > 0)
				{
					continue;
				}
				LcarsCalendarBridge.SharedCalendarEvent sharedCalendarEvent3 = new LcarsCalendarBridge.SharedCalendarEvent();
				sharedCalendarEvent3.Uid = text2;
				ewsCalendarEventDetails2 = ewsCalendarEventDetails;
				sharedCalendarEvent3.Summary = ((ewsCalendarEventDetails2.Subject != null) ? ewsCalendarEventDetails2.Subject : item2.Subject);
				EwsMailService.EwsCalendarEventDetails ewsCalendarEventDetails3 = ewsCalendarEventDetails;
				sharedCalendarEvent3.Location = ((ewsCalendarEventDetails3.Location != null) ? ewsCalendarEventDetails3.Location : "");
				sharedCalendarEvent3.StartUtc = ewsCalendarEventDetails.StartUtc;
				sharedCalendarEvent3.EndUtc = ewsCalendarEventDetails.EndUtc;
				sharedCalendarEvent3.AlarmMinutesBefore = ewsCalendarEventDetails.AlarmMinutesBefore;
				sharedCalendarEvent3.TaskCompleted = false;
				EwsMailService.EwsCalendarEventDetails ewsCalendarEventDetails4 = ewsCalendarEventDetails;
				sharedCalendarEvent3.Category = ((ewsCalendarEventDetails4.Category != null) ? ewsCalendarEventDetails4.Category : "");
				EwsMailService.EwsCalendarEventDetails ewsCalendarEventDetails5 = ewsCalendarEventDetails;
				sharedCalendarEvent3.EwsItemId = ((ewsCalendarEventDetails5.ItemId != null) ? ewsCalendarEventDetails5.ItemId : item2.ItemId);
				EwsMailService.EwsCalendarEventDetails ewsCalendarEventDetails6 = ewsCalendarEventDetails;
				sharedCalendarEvent3.EwsChangeKey = ((ewsCalendarEventDetails6.ChangeKey != null) ? ewsCalendarEventDetails6.ChangeKey : item2.ChangeKey);
				sharedCalendarEvent3.EwsDirty = false;
				sharedCalendarEvent3.EwsLastModifiedUtc = ewsCalendarEventDetails.LastModifiedUtc;
				LcarsCalendarBridge.SharedCalendarEvent sharedCalendarEvent4 = sharedCalendarEvent3;
				LcarsCalendarBridge.SharedCalendarEvent sharedCalendarEvent;
				if (sharedCalendarEvent2 != null)
				{
					if (string.IsNullOrWhiteSpace(sharedCalendarEvent4.Location))
					{
						sharedCalendarEvent = sharedCalendarEvent2;
						sharedCalendarEvent4.Location = ((sharedCalendarEvent.Location != null) ? sharedCalendarEvent.Location : "");
					}
					sharedCalendarEvent = sharedCalendarEvent2;
					sharedCalendarEvent4.RecurrenceRule = ((sharedCalendarEvent.RecurrenceRule != null) ? sharedCalendarEvent.RecurrenceRule : "");
					if (string.IsNullOrWhiteSpace(sharedCalendarEvent4.Category))
					{
						sharedCalendarEvent = sharedCalendarEvent2;
						sharedCalendarEvent4.Category = ((sharedCalendarEvent.Category != null) ? sharedCalendarEvent.Category : "");
					}
					if (ewsCalendarEventDetails.AlarmMinutesBefore <= 0 && sharedCalendarEvent2.AlarmMinutesBefore > 0)
					{
						sharedCalendarEvent4.AlarmMinutesBefore = sharedCalendarEvent2.AlarmMinutesBefore;
					}
					sharedCalendarEvent = sharedCalendarEvent2;
					sharedCalendarEvent4.CalendarName = ((sharedCalendarEvent.CalendarName != null) ? sharedCalendarEvent.CalendarName : "");
					sharedCalendarEvent4.CalDavDirty = sharedCalendarEvent2.CalDavDirty;
				}
				if (!dictionary.ContainsKey(text2))
				{
					num = checked(num + 1);
				}
				dictionary[text2] = sharedCalendarEvent4;
				sharedCalendarEvent = sharedCalendarEvent4;
				string text4 = ((sharedCalendarEvent.EwsItemId != null) ? sharedCalendarEvent.EwsItemId : "").Trim();
				if (text4.Length > 0)
				{
					dictionary2[text4] = sharedCalendarEvent4;
				}
			}
		}
		List<string> list3 = new List<string>();
		foreach (KeyValuePair<string, LcarsCalendarBridge.SharedCalendarEvent> item3 in dictionary)
		{
			LcarsCalendarBridge.SharedCalendarEvent value = item3.Value;
			if (value != null && !value.EwsDirty)
			{
				LcarsCalendarBridge.SharedCalendarEvent sharedCalendarEvent = value;
				string text5 = ((sharedCalendarEvent.EwsItemId != null) ? sharedCalendarEvent.EwsItemId : "").Trim();
				if (text5.Length != 0 && !hashSet.Contains(text5))
				{
					list3.Add(item3.Key);
				}
			}
		}
		foreach (string item4 in list3)
		{
			dictionary.Remove(item4);
		}
		LcarsCalendarBridge.SaveEvents(new List<LcarsCalendarBridge.SharedCalendarEvent>(dictionary.Values));
		return num;
	}

	public static int TryMergeContactsFromEws(string ewsUrl, string username, string password, string searchQuery, string oauthToken = null)
	{
		LastError = null;
		string text = ((searchQuery != null) ? searchQuery : "").Trim();
		if (text.Length == 0)
		{
			LastError = "Contact search query is required.";
			return -1;
		}
		List<EwsMailService.EwsResolvedContact> list = EwsMailService.TryResolveContacts(ewsUrl, username, password, text, oauthToken);
		if (list == null)
		{
			LastError = ((EwsMailService.LastError != null) ? EwsMailService.LastError : "EWS ResolveNames failed.");
			return -1;
		}
		List<MailContact> list2 = new List<MailContact>();
		foreach (EwsMailService.EwsResolvedContact item in list)
		{
			if (item != null && !string.IsNullOrWhiteSpace(item.EmailAddress))
			{
				MailContact mailContact = new MailContact();
				EwsMailService.EwsResolvedContact ewsResolvedContact = item;
				mailContact.DisplayName = ((ewsResolvedContact.DisplayName != null) ? ewsResolvedContact.DisplayName : item.EmailAddress).Trim();
				mailContact.Email = item.EmailAddress.Trim();
				EwsMailService.EwsResolvedContact ewsResolvedContact2 = item;
				mailContact.Company = ((ewsResolvedContact2.Company != null) ? ewsResolvedContact2.Company : "").Trim();
				EwsMailService.EwsResolvedContact ewsResolvedContact3 = item;
				mailContact.WorkPhone = ((ewsResolvedContact3.BusinessPhone != null) ? ewsResolvedContact3.BusinessPhone : "").Trim();
				list2.Add(mailContact);
			}
		}
		return AddressBookStore.TryMergeContacts(list2);
	}

	public static int TrySyncGalDirectory(string ewsUrl, string username, string password, string oauthToken = null, bool deepGalHarvest = false)
	{
		LastError = null;
		List<EwsMailService.EwsResolvedContact> list = EwsMailService.TryHarvestGalDirectory(ewsUrl, username, password, oauthToken, deepGalHarvest);
		if (list == null)
		{
			LastError = ((EwsMailService.LastError != null) ? EwsMailService.LastError : "GAL harvest failed.");
			return -1;
		}
		if (list.Count == 0)
		{
			LastError = ((EwsMailService.LastError != null) ? EwsMailService.LastError : "GAL harvest returned no contacts.");
			return 0;
		}
		List<MailContact> list2 = new List<MailContact>();
		foreach (EwsMailService.EwsResolvedContact item in list)
		{
			if (item != null && !string.IsNullOrWhiteSpace(item.EmailAddress))
			{
				MailContact mailContact = new MailContact();
				EwsMailService.EwsResolvedContact ewsResolvedContact = item;
				mailContact.DisplayName = ((ewsResolvedContact.DisplayName != null) ? ewsResolvedContact.DisplayName : item.EmailAddress).Trim();
				mailContact.Email = item.EmailAddress.Trim();
				EwsMailService.EwsResolvedContact ewsResolvedContact2 = item;
				mailContact.Company = ((ewsResolvedContact2.Company != null) ? ewsResolvedContact2.Company : "").Trim();
				EwsMailService.EwsResolvedContact ewsResolvedContact3 = item;
				mailContact.WorkPhone = ((ewsResolvedContact3.BusinessPhone != null) ? ewsResolvedContact3.BusinessPhone : "").Trim();
				EwsMailService.EwsResolvedContact ewsResolvedContact4 = item;
				mailContact.MobilePhone = ((ewsResolvedContact4.MobilePhone != null) ? ewsResolvedContact4.MobilePhone : "").Trim();
				EwsMailService.EwsResolvedContact ewsResolvedContact5 = item;
				mailContact.FirstName = ((ewsResolvedContact5.FirstName != null) ? ewsResolvedContact5.FirstName : "").Trim();
				EwsMailService.EwsResolvedContact ewsResolvedContact6 = item;
				mailContact.LastName = ((ewsResolvedContact6.LastName != null) ? ewsResolvedContact6.LastName : "").Trim();
				EwsMailService.EwsResolvedContact ewsResolvedContact7 = item;
				mailContact.JobTitle = ((ewsResolvedContact7.JobTitle != null) ? ewsResolvedContact7.JobTitle : "").Trim();
				EwsMailService.EwsResolvedContact ewsResolvedContact8 = item;
				mailContact.Department = ((ewsResolvedContact8.Department != null) ? ewsResolvedContact8.Department : "").Trim();
				EwsMailService.EwsResolvedContact ewsResolvedContact9 = item;
				mailContact.WebPage = ((ewsResolvedContact9.WebPage != null) ? ewsResolvedContact9.WebPage : "").Trim();
				EwsMailService.EwsResolvedContact ewsResolvedContact10 = item;
				mailContact.FaxNumber = ((ewsResolvedContact10.FaxNumber != null) ? ewsResolvedContact10.FaxNumber : "").Trim();
				EwsMailService.EwsResolvedContact ewsResolvedContact11 = item;
				mailContact.PagerNumber = ((ewsResolvedContact11.PagerNumber != null) ? ewsResolvedContact11.PagerNumber : "").Trim();
				EwsMailService.EwsResolvedContact ewsResolvedContact12 = item;
				mailContact.OfficeLocation = ((ewsResolvedContact12.OfficeLocation != null) ? ewsResolvedContact12.OfficeLocation : "").Trim();
				EwsMailService.EwsResolvedContact ewsResolvedContact13 = item;
				mailContact.HomePhone = ((ewsResolvedContact13.HomePhone != null) ? ewsResolvedContact13.HomePhone : "").Trim();
				EwsMailService.EwsResolvedContact ewsResolvedContact14 = item;
				mailContact.MiddleName = ((ewsResolvedContact14.MiddleName != null) ? ewsResolvedContact14.MiddleName : "").Trim();
				EwsMailService.EwsResolvedContact ewsResolvedContact15 = item;
				mailContact.OtherPhone = ((ewsResolvedContact15.OtherPhone != null) ? ewsResolvedContact15.OtherPhone : "").Trim();
				EwsMailService.EwsResolvedContact ewsResolvedContact16 = item;
				mailContact.AssistantName = ((ewsResolvedContact16.AssistantName != null) ? ewsResolvedContact16.AssistantName : "").Trim();
				mailContact.IsGalEntry = true;
				EwsMailService.EwsResolvedContact ewsResolvedContact17 = item;
				mailContact.GalPersonaId = ((ewsResolvedContact17.GalPersonaId != null) ? ewsResolvedContact17.GalPersonaId : "").Trim();
				EwsMailService.EwsResolvedContact ewsResolvedContact18 = item;
				mailContact.GalPersonaChangeKey = ((ewsResolvedContact18.GalPersonaChangeKey != null) ? ewsResolvedContact18.GalPersonaChangeKey : "").Trim();
				EwsMailService.EwsResolvedContact ewsResolvedContact19 = item;
				mailContact.GalDirectoryItemId = ((ewsResolvedContact19.DirectoryContactItemId != null) ? ewsResolvedContact19.DirectoryContactItemId : "").Trim();
				EwsMailService.EwsResolvedContact ewsResolvedContact20 = item;
				mailContact.GalDirectoryChangeKey = ((ewsResolvedContact20.DirectoryContactChangeKey != null) ? ewsResolvedContact20.DirectoryContactChangeKey : "").Trim();
				list2.Add(mailContact);
			}
		}
		return AddressBookStore.TryMergeContacts(list2);
	}

	public static int TrySyncGalDirectoryBidirectional(string ewsUrl, string username, string password, string oauthToken = null, bool deepGalHarvest = false)
	{
		LastGalPersonalPushCount = 0;
		int num = TrySyncGalDirectory(ewsUrl, username, password, oauthToken, deepGalHarvest);
		if (num < 0)
		{
			return num;
		}
		int val = TryPushGalDirtyPersonalShadows(ewsUrl, username, password, oauthToken);
		LastGalPersonalPushCount = Math.Max(0, val);
		AddressBookStore.LastGalPersonalPushCount = LastGalPersonalPushCount;
		return Math.Max(num, val);
	}

	public static int TryPushGalDirtyPersonalShadows(string ewsUrl, string username, string password, string oauthToken = null)
	{
		LastError = null;
		LastGalPersonaUpdateAttempts = 0;
		LastGalDirectoryUpdateAttempts = 0;
		LastGalDirectoryResolveSuccess = 0;
		LastGalChangeKeyRefreshes = 0;
		LastGalPersonaChangeKeyRefreshes = 0;
		LastGalDirectoryChangeKeyRefreshes = 0;
		List<MailContact> list = AddressBookStore.LoadContacts();
		if (list == null || list.Count == 0)
		{
			return 0;
		}
		int num = 0;
		checked
		{
			foreach (MailContact item in list)
			{
				if (item != null && item.IsGalEntry && item.GalPushDirty)
				{
					num++;
				}
			}
			foreach (MailContact item2 in list)
			{
				if (item2 != null && item2.IsGalEntry && item2.GalPushDirty && !string.IsNullOrWhiteSpace(item2.Email))
				{
					if (EwsMailService.TryResolveDirectoryContactItemIds(ewsUrl, username, password, item2, oauthToken))
					{
						LastGalChangeKeyRefreshes++;
					}
					if (EwsMailService.TryRefreshGalPersonaChangeKey(ewsUrl, username, password, item2, oauthToken))
					{
						LastGalPersonaChangeKeyRefreshes++;
					}
					if (EwsMailService.TryRefreshGalDirectoryChangeKey(ewsUrl, username, password, item2, oauthToken))
					{
						LastGalDirectoryChangeKeyRefreshes++;
					}
				}
			}
			if (LastGalChangeKeyRefreshes > 0 || LastGalPersonaChangeKeyRefreshes > 0 || LastGalDirectoryChangeKeyRefreshes > 0)
			{
				AddressBookStore.SaveContacts(list);
			}
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			int num5 = 0;
			int num6 = 0;
			bool flag = false;
			bool flag2 = false;
			foreach (MailContact item3 in list)
			{
				if (item3 != null && item3.IsGalEntry && item3.GalPushDirty && !string.IsNullOrWhiteSpace(item3.Email) && string.IsNullOrWhiteSpace(item3.GalDirectoryItemId) && EwsMailService.TryResolveDirectoryContactItemIds(ewsUrl, username, password, item3, oauthToken))
				{
					flag2 = true;
					LastGalDirectoryResolveSuccess++;
				}
			}
			if (flag2)
			{
				AddressBookStore.SaveContacts(list);
			}
			foreach (MailContact item4 in list)
			{
				if (item4 == null || !item4.IsGalEntry || !item4.GalPushDirty || string.IsNullOrWhiteSpace(item4.Email))
				{
					continue;
				}
				bool flag3 = false;
				if (!string.IsNullOrWhiteSpace(item4.GalPersonaId))
				{
					LastGalPersonaUpdateAttempts++;
					if (EwsMailService.TryUpdateGalPersonaContact(ewsUrl, username, password, item4, oauthToken))
					{
						item4.GalPushDirty = false;
						num2++;
						num3++;
						flag = true;
						flag3 = true;
					}
				}
				if (!flag3 && !string.IsNullOrWhiteSpace(item4.GalDirectoryItemId))
				{
					LastGalDirectoryUpdateAttempts++;
					if (EwsMailService.TryUpdateGalDirectoryContact(ewsUrl, username, password, item4, oauthToken))
					{
						item4.GalPushDirty = false;
						num2++;
						num4++;
						flag = true;
						flag3 = true;
					}
				}
				if (flag3)
				{
					continue;
				}
				if (!string.IsNullOrWhiteSpace(item4.EwsPersonalItemId))
				{
					if (EwsMailService.TryUpdatePersonalContactShadow(ewsUrl, username, password, item4, oauthToken))
					{
						item4.GalPushDirty = false;
						num2++;
						num5++;
						flag = true;
					}
					continue;
				}
				string text = EwsMailService.TryCreatePersonalContactShadow(ewsUrl, username, password, item4, oauthToken);
				if (!string.IsNullOrWhiteSpace(text))
				{
					item4.EwsPersonalItemId = text;
					item4.GalPushDirty = false;
					num2++;
					num6++;
					flag = true;
				}
			}
			EwsGalWriteProofStore.RecordPushSession(num, LastGalPersonaUpdateAttempts, num3, LastGalDirectoryUpdateAttempts, num4, num5, num6, EwsMailService.LastGalPersonaUpdateDenied, EwsMailService.LastGalDirectoryUpdateDenied);
			if (flag)
			{
				AddressBookStore.SaveContacts(list);
			}
			LastGalPersonalPushCount = num2;
			AddressBookStore.LastGalPersonalPushCount = num2;
			return num2;
		}
	}

	public static int TryMergePersonalContactsFromEws(string ewsUrl, string username, string password, string oauthToken = null)
	{
		LastError = null;
		List<EwsMailService.EwsResolvedContact> list = EwsMailService.TryHarvestPersonalContactsFolder(ewsUrl, username, password, oauthToken);
		if (list == null)
		{
			LastError = ((EwsMailService.LastError != null) ? EwsMailService.LastError : "EWS contacts folder harvest failed.");
			return -1;
		}
		if (list.Count == 0)
		{
			return 0;
		}
		List<MailContact> list2 = new List<MailContact>();
		foreach (EwsMailService.EwsResolvedContact item in list)
		{
			if (item != null && !string.IsNullOrWhiteSpace(item.EmailAddress))
			{
				MailContact mailContact = new MailContact();
				EwsMailService.EwsResolvedContact ewsResolvedContact = item;
				mailContact.DisplayName = ((ewsResolvedContact.DisplayName != null) ? ewsResolvedContact.DisplayName : item.EmailAddress).Trim();
				mailContact.Email = item.EmailAddress.Trim();
				EwsMailService.EwsResolvedContact ewsResolvedContact2 = item;
				mailContact.Company = ((ewsResolvedContact2.Company != null) ? ewsResolvedContact2.Company : "").Trim();
				EwsMailService.EwsResolvedContact ewsResolvedContact3 = item;
				mailContact.WorkPhone = ((ewsResolvedContact3.BusinessPhone != null) ? ewsResolvedContact3.BusinessPhone : "").Trim();
				EwsMailService.EwsResolvedContact ewsResolvedContact4 = item;
				mailContact.MobilePhone = ((ewsResolvedContact4.MobilePhone != null) ? ewsResolvedContact4.MobilePhone : "").Trim();
				EwsMailService.EwsResolvedContact ewsResolvedContact5 = item;
				mailContact.FirstName = ((ewsResolvedContact5.FirstName != null) ? ewsResolvedContact5.FirstName : "").Trim();
				EwsMailService.EwsResolvedContact ewsResolvedContact6 = item;
				mailContact.LastName = ((ewsResolvedContact6.LastName != null) ? ewsResolvedContact6.LastName : "").Trim();
				EwsMailService.EwsResolvedContact ewsResolvedContact7 = item;
				mailContact.JobTitle = ((ewsResolvedContact7.JobTitle != null) ? ewsResolvedContact7.JobTitle : "").Trim();
				EwsMailService.EwsResolvedContact ewsResolvedContact8 = item;
				mailContact.Department = ((ewsResolvedContact8.Department != null) ? ewsResolvedContact8.Department : "").Trim();
				EwsMailService.EwsResolvedContact ewsResolvedContact9 = item;
				mailContact.WebPage = ((ewsResolvedContact9.WebPage != null) ? ewsResolvedContact9.WebPage : "").Trim();
				EwsMailService.EwsResolvedContact ewsResolvedContact10 = item;
				mailContact.FaxNumber = ((ewsResolvedContact10.FaxNumber != null) ? ewsResolvedContact10.FaxNumber : "").Trim();
				EwsMailService.EwsResolvedContact ewsResolvedContact11 = item;
				mailContact.PagerNumber = ((ewsResolvedContact11.PagerNumber != null) ? ewsResolvedContact11.PagerNumber : "").Trim();
				EwsMailService.EwsResolvedContact ewsResolvedContact12 = item;
				mailContact.OfficeLocation = ((ewsResolvedContact12.OfficeLocation != null) ? ewsResolvedContact12.OfficeLocation : "").Trim();
				EwsMailService.EwsResolvedContact ewsResolvedContact13 = item;
				mailContact.HomePhone = ((ewsResolvedContact13.HomePhone != null) ? ewsResolvedContact13.HomePhone : "").Trim();
				EwsMailService.EwsResolvedContact ewsResolvedContact14 = item;
				mailContact.MiddleName = ((ewsResolvedContact14.MiddleName != null) ? ewsResolvedContact14.MiddleName : "").Trim();
				EwsMailService.EwsResolvedContact ewsResolvedContact15 = item;
				mailContact.OtherPhone = ((ewsResolvedContact15.OtherPhone != null) ? ewsResolvedContact15.OtherPhone : "").Trim();
				EwsMailService.EwsResolvedContact ewsResolvedContact16 = item;
				mailContact.AssistantName = ((ewsResolvedContact16.AssistantName != null) ? ewsResolvedContact16.AssistantName : "").Trim();
				list2.Add(mailContact);
			}
		}
		return AddressBookStore.TryMergeContacts(list2);
	}

	public static int TryPushDirtyCalendarEventsToEws(string ewsUrl, string username, string password, string oauthToken = null)
	{
		LastError = null;
		int val = TryProcessPendingCalendarRsvps(ewsUrl, username, password, oauthToken);
		List<string> list = EwsCalendarDeleteQueue.TakePendingDeletes();
		int num = 0;
		if (list != null && list.Count > 0 && EwsMailService.TryDeleteItems(ewsUrl, username, password, list, oauthToken))
		{
			num = list.Count;
		}
		List<LcarsCalendarBridge.SharedCalendarEvent> list2 = LcarsCalendarBridge.LoadEvents();
		if (list2 == null || list2.Count == 0)
		{
			return 0;
		}
		LastCalendarChangeKeyRefreshes = EwsCalendarConflictHelper.TryRefreshChangeKeysForDirtyEvents(ewsUrl, username, password, oauthToken);
		int num2 = 0;
		LastCalendarConflictOverwrites = 0;
		LastCalendarAutoServerWins = 0;
		LastCalendarAutoClientWins = 0;
		checked
		{
			foreach (LcarsCalendarBridge.SharedCalendarEvent item in list2)
			{
				if (item == null || (!item.EwsDirty && (!string.IsNullOrWhiteSpace(item.EwsItemId) || 1 == 0)))
				{
					continue;
				}
				if (string.IsNullOrWhiteSpace(item.EwsItemId))
				{
					string changeKey = null;
					string text = EwsMailService.TryCreateCalendarEvent(ewsUrl, username, password, item, oauthToken, ref changeKey);
					if (!string.IsNullOrWhiteSpace(text))
					{
						item.EwsItemId = text;
						item.EwsChangeKey = ((changeKey != null) ? changeKey : "");
						item.EwsDirty = false;
						item.EwsLastModifiedUtc = DateTime.UtcNow;
						num2++;
					}
				}
				else
				{
					if (!item.EwsDirty)
					{
						continue;
					}
					if (string.IsNullOrWhiteSpace(item.EwsChangeKey))
					{
						EwsMailService.EwsCalendarEventDetails ewsCalendarEventDetails = EwsMailService.TryGetCalendarEventDetails(ewsUrl, username, password, item.EwsItemId, oauthToken);
						if (ewsCalendarEventDetails != null && ewsCalendarEventDetails.ChangeKey.Length > 0)
						{
							item.EwsChangeKey = ewsCalendarEventDetails.ChangeKey;
							if (DateTime.Compare(ewsCalendarEventDetails.LastModifiedUtc, DateTime.MinValue) != 0)
							{
								item.EwsLastModifiedUtc = ewsCalendarEventDetails.LastModifiedUtc;
							}
						}
					}
					LcarsCalendarBridge.SharedCalendarEvent sharedCalendarEvent = item;
					string text2 = ((sharedCalendarEvent.EwsChangeKey != null) ? sharedCalendarEvent.EwsChangeKey : "");
					if (EwsMailService.TryUpdateCalendarEvent(ewsUrl, username, password, item.EwsItemId, item, oauthToken))
					{
						item.EwsDirty = false;
						item.EwsLastModifiedUtc = DateTime.UtcNow;
						num2++;
						if (text2.Length > 0)
						{
							sharedCalendarEvent = item;
							if (!string.Equals(text2, (sharedCalendarEvent.EwsChangeKey != null) ? sharedCalendarEvent.EwsChangeKey : "", StringComparison.Ordinal))
							{
								LastCalendarConflictOverwrites++;
							}
						}
						continue;
					}
					EwsMailService.EwsCalendarEventDetails ewsCalendarEventDetails2 = EwsMailService.TryGetCalendarEventDetails(ewsUrl, username, password, item.EwsItemId, oauthToken);
					if (ewsCalendarEventDetails2 != null && DateTime.Compare(ewsCalendarEventDetails2.LastModifiedUtc, DateTime.MinValue) != 0 && DateTime.Compare(item.EwsLastModifiedUtc, DateTime.MinValue) != 0 && DateTime.Compare(ewsCalendarEventDetails2.LastModifiedUtc, item.EwsLastModifiedUtc) > 0)
					{
						EwsMailService.EwsCalendarEventDetails ewsCalendarEventDetails3 = ewsCalendarEventDetails2;
						item.Summary = ((ewsCalendarEventDetails3.Subject != null) ? ewsCalendarEventDetails3.Subject : item.Summary);
						ewsCalendarEventDetails3 = ewsCalendarEventDetails2;
						item.Location = ((ewsCalendarEventDetails3.Location != null) ? ewsCalendarEventDetails3.Location : item.Location);
						item.StartUtc = ewsCalendarEventDetails2.StartUtc;
						item.EndUtc = ewsCalendarEventDetails2.EndUtc;
						ewsCalendarEventDetails3 = ewsCalendarEventDetails2;
						item.Category = ((ewsCalendarEventDetails3.Category != null) ? ewsCalendarEventDetails3.Category : item.Category);
						item.AlarmMinutesBefore = ewsCalendarEventDetails2.AlarmMinutesBefore;
						ewsCalendarEventDetails3 = ewsCalendarEventDetails2;
						item.EwsChangeKey = ((ewsCalendarEventDetails3.ChangeKey != null) ? ewsCalendarEventDetails3.ChangeKey : item.EwsChangeKey);
						item.EwsLastModifiedUtc = ewsCalendarEventDetails2.LastModifiedUtc;
						item.EwsDirty = false;
						item.CalDavDirty = true;
						LastCalendarAutoServerWins++;
						continue;
					}
					if (ewsCalendarEventDetails2 != null && DateTime.Compare(item.EwsLastModifiedUtc, DateTime.MinValue) != 0 && DateTime.Compare(ewsCalendarEventDetails2.LastModifiedUtc, DateTime.MinValue) != 0 && DateTime.Compare(item.EwsLastModifiedUtc, ewsCalendarEventDetails2.LastModifiedUtc) > 0)
					{
						if (ewsCalendarEventDetails2.ChangeKey.Length > 0)
						{
							item.EwsChangeKey = ewsCalendarEventDetails2.ChangeKey;
						}
						if (EwsMailService.TryUpdateCalendarEvent(ewsUrl, username, password, item.EwsItemId, item, oauthToken))
						{
							item.EwsDirty = false;
							item.EwsLastModifiedUtc = DateTime.UtcNow;
							num2++;
							LastCalendarAutoClientWins++;
						}
						else
						{
							item.EwsDirty = true;
						}
						continue;
					}
					if (ewsCalendarEventDetails2 != null && EwsCalendarConflictHelper.TryReconcileSingleIfServerMatchesLocal(item, ewsCalendarEventDetails2))
					{
						num2++;
						continue;
					}
					item.EwsDirty = true;
					if (ewsCalendarEventDetails2 != null && ewsCalendarEventDetails2.ChangeKey.Length > 0)
					{
						item.EwsChangeKey = ewsCalendarEventDetails2.ChangeKey;
						if (DateTime.Compare(ewsCalendarEventDetails2.LastModifiedUtc, DateTime.MinValue) != 0)
						{
							item.EwsLastModifiedUtc = ewsCalendarEventDetails2.LastModifiedUtc;
						}
					}
				}
			}
			LcarsCalendarBridge.SaveEvents(list2);
			LastCalendarPushCount = num2;
			return Math.Max(num2, val);
		}
	}

	private static int TryProcessPendingCalendarRsvps(string ewsUrl, string username, string password, string oauthToken = null)
	{
		LastCalendarRsvpPushCount = 0;
		List<EwsCalendarRsvpQueue.RsvpRequest> list = EwsCalendarRsvpQueue.TakePendingRsvps();
		if (list == null || list.Count == 0)
		{
			return 0;
		}
		List<LcarsCalendarBridge.SharedCalendarEvent> list2 = LcarsCalendarBridge.LoadEvents();
		if (list2 == null)
		{
			return 0;
		}
		int num = 0;
		foreach (EwsCalendarRsvpQueue.RsvpRequest item in list)
		{
			if (item == null || string.IsNullOrWhiteSpace(item.Uid))
			{
				continue;
			}
			EwsCalendarRsvpQueue.RsvpRequest rsvpRequest = item;
			string text = ((rsvpRequest.PartStat != null) ? rsvpRequest.PartStat : "").Trim().ToUpperInvariant();
			if (text.Length == 0)
			{
				continue;
			}
			foreach (LcarsCalendarBridge.SharedCalendarEvent item2 in list2)
			{
				if (item2 == null || !string.Equals(item2.Uid, item.Uid, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				item2.MyPartStat = text;
				item2.IsMeetingInvitation = true;
				if (string.IsNullOrWhiteSpace(item2.EwsItemId))
				{
					item2.EwsDirty = true;
				}
				else if (EwsMailService.TryUpdateCalendarEvent(ewsUrl, username, password, item2.EwsItemId, item2, oauthToken))
				{
					item2.EwsDirty = false;
					item2.CalDavDirty = true;
					num = checked(num + 1);
				}
				else
				{
					item2.EwsDirty = true;
				}
				break;
			}
		}
		if (num > 0)
		{
			LcarsCalendarBridge.SaveEvents(list2);
		}
		LastCalendarRsvpPushCount = num;
		EwsCalendarRsvpQueue.LastProcessedCount = num;
		return num;
	}

	public static int TrySyncEwsCalendarBidirectional(string ewsUrl, string username, string password, string oauthToken = null)
	{
		LastError = null;
		LastCalendarAutoReconcileCleared = 0;
		int num = TryMergeCalendarFromEws(ewsUrl, username, password, oauthToken);
		if (num < 0)
		{
			return num;
		}
		int num2 = TryPushDirtyCalendarEventsToEws(ewsUrl, username, password, oauthToken);
		int num3 = 0;
		checked
		{
			while (EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents() > 0)
			{
				int num4 = EwsCalendarConflictHelper.TryAutoReconcileUnchangedDirty(ewsUrl, username, password, oauthToken);
				if (num4 <= 0)
				{
					num2 = Math.Max(num2, TryPushDirtyCalendarEventsToEws(ewsUrl, username, password, oauthToken));
				}
				else
				{
					num2 = Math.Max(num2, num4);
					LastCalendarAutoReconcileCleared += num4;
				}
				num3++;
				if (num3 > 4)
				{
					break;
				}
			}
			return Math.Max(num, Math.Max(0, num2));
		}
	}
}
