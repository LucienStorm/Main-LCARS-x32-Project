using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Web.Script.Serialization;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

[StandardModule]
public sealed class AddressBookStore
{
	private class CardDavSettingsDto
	{
		public string ServerUrl
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

		public string ConflictOn412Mode
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		[DebuggerNonUserCode]
		public CardDavSettingsDto()
		{
		}
	}

	private class CardDavSecondaryAccountDto
	{
		public string ServerUrl
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

		public string BookKey
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		[DebuggerNonUserCode]
		public CardDavSecondaryAccountDto()
		{
		}
	}

	private class ContactDto
	{
		public string DisplayName
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string Email
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string Company
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string WorkPhone
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string HomePhone
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string Notes
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string CardDavEtag
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string SecondaryEmail
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string FirstName
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string LastName
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string NickName
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string JobTitle
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string Department
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string MobilePhone
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string WebPage
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string FaxNumber
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string PagerNumber
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string WorkAddress
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string OfficeLocation
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string HomeAddress
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string Custom1
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string Custom2
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string Custom3
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string Custom4
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string MiddleName
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string Birthday
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string OtherPhone
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string SpouseName
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string Anniversary
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string AssistantName
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string ScreenName
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string AimScreenName
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string PopularityIndex
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string RecordName
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string PreferEmail
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string MorkRowId
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public bool CardDavDirty
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public bool IsGalEntry
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public bool GalPushDirty
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string GalPersonaId
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string GalPersonaChangeKey
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string GalDirectoryItemId
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string GalDirectoryChangeKey
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string EwsPersonalItemId
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		[DebuggerNonUserCode]
		public ContactDto()
		{
		}

		public static ContactDto FromModel(MailContact contact)
		{
			ContactDto contactDto = new ContactDto();
			contactDto.DisplayName = ((contact.DisplayName != null) ? contact.DisplayName : "");
			contactDto.Email = ((contact.Email != null) ? contact.Email : "");
			contactDto.Company = ((contact.Company != null) ? contact.Company : "");
			contactDto.WorkPhone = ((contact.WorkPhone != null) ? contact.WorkPhone : "");
			contactDto.HomePhone = ((contact.HomePhone != null) ? contact.HomePhone : "");
			contactDto.Notes = ((contact.Notes != null) ? contact.Notes : "");
			contactDto.CardDavEtag = ((contact.CardDavEtag != null) ? contact.CardDavEtag : "");
			contactDto.SecondaryEmail = ((contact.SecondaryEmail != null) ? contact.SecondaryEmail : "");
			contactDto.FirstName = ((contact.FirstName != null) ? contact.FirstName : "");
			contactDto.LastName = ((contact.LastName != null) ? contact.LastName : "");
			contactDto.NickName = ((contact.NickName != null) ? contact.NickName : "");
			contactDto.JobTitle = ((contact.JobTitle != null) ? contact.JobTitle : "");
			contactDto.Department = ((contact.Department != null) ? contact.Department : "");
			contactDto.MobilePhone = ((contact.MobilePhone != null) ? contact.MobilePhone : "");
			contactDto.WebPage = ((contact.WebPage != null) ? contact.WebPage : "");
			contactDto.FaxNumber = ((contact.FaxNumber != null) ? contact.FaxNumber : "");
			contactDto.PagerNumber = ((contact.PagerNumber != null) ? contact.PagerNumber : "");
			contactDto.WorkAddress = ((contact.WorkAddress != null) ? contact.WorkAddress : "");
			contactDto.OfficeLocation = ((contact.OfficeLocation != null) ? contact.OfficeLocation : "");
			contactDto.HomeAddress = ((contact.HomeAddress != null) ? contact.HomeAddress : "");
			contactDto.Custom1 = ((contact.Custom1 != null) ? contact.Custom1 : "");
			contactDto.Custom2 = ((contact.Custom2 != null) ? contact.Custom2 : "");
			contactDto.Custom3 = ((contact.Custom3 != null) ? contact.Custom3 : "");
			contactDto.Custom4 = ((contact.Custom4 != null) ? contact.Custom4 : "");
			contactDto.MiddleName = ((contact.MiddleName != null) ? contact.MiddleName : "");
			contactDto.Birthday = ((contact.Birthday != null) ? contact.Birthday : "");
			contactDto.OtherPhone = ((contact.OtherPhone != null) ? contact.OtherPhone : "");
			contactDto.SpouseName = ((contact.SpouseName != null) ? contact.SpouseName : "");
			contactDto.Anniversary = ((contact.Anniversary != null) ? contact.Anniversary : "");
			contactDto.AssistantName = ((contact.AssistantName != null) ? contact.AssistantName : "");
			contactDto.ScreenName = ((contact.ScreenName != null) ? contact.ScreenName : "");
			contactDto.AimScreenName = ((contact.AimScreenName != null) ? contact.AimScreenName : "");
			contactDto.PopularityIndex = ((contact.PopularityIndex != null) ? contact.PopularityIndex : "");
			contactDto.RecordName = ((contact.RecordName != null) ? contact.RecordName : "");
			contactDto.PreferEmail = ((contact.PreferEmail != null) ? contact.PreferEmail : "");
			contactDto.MorkRowId = ((contact.MorkRowId != null) ? contact.MorkRowId : "");
			contactDto.CardDavDirty = contact.CardDavDirty;
			contactDto.IsGalEntry = contact.IsGalEntry;
			contactDto.GalPushDirty = contact.GalPushDirty;
			contactDto.GalPersonaId = ((contact.GalPersonaId != null) ? contact.GalPersonaId : "");
			contactDto.GalPersonaChangeKey = ((contact.GalPersonaChangeKey != null) ? contact.GalPersonaChangeKey : "");
			contactDto.GalDirectoryItemId = ((contact.GalDirectoryItemId != null) ? contact.GalDirectoryItemId : "");
			contactDto.GalDirectoryChangeKey = ((contact.GalDirectoryChangeKey != null) ? contact.GalDirectoryChangeKey : "");
			contactDto.EwsPersonalItemId = ((contact.EwsPersonalItemId != null) ? contact.EwsPersonalItemId : "");
			return contactDto;
		}

		public MailContact ToModel()
		{
			MailContact mailContact = new MailContact();
			string displayName = DisplayName;
			mailContact.DisplayName = ((displayName != null) ? displayName : "");
			string email = Email;
			mailContact.Email = ((email != null) ? email : "");
			string company = Company;
			mailContact.Company = ((company != null) ? company : "");
			string workPhone = WorkPhone;
			mailContact.WorkPhone = ((workPhone != null) ? workPhone : "");
			string homePhone = HomePhone;
			mailContact.HomePhone = ((homePhone != null) ? homePhone : "");
			string notes = Notes;
			mailContact.Notes = ((notes != null) ? notes : "");
			string cardDavEtag = CardDavEtag;
			mailContact.CardDavEtag = ((cardDavEtag != null) ? cardDavEtag : "");
			string secondaryEmail = SecondaryEmail;
			mailContact.SecondaryEmail = ((secondaryEmail != null) ? secondaryEmail : "");
			string firstName = FirstName;
			mailContact.FirstName = ((firstName != null) ? firstName : "");
			string lastName = LastName;
			mailContact.LastName = ((lastName != null) ? lastName : "");
			string nickName = NickName;
			mailContact.NickName = ((nickName != null) ? nickName : "");
			string jobTitle = JobTitle;
			mailContact.JobTitle = ((jobTitle != null) ? jobTitle : "");
			string department = Department;
			mailContact.Department = ((department != null) ? department : "");
			string mobilePhone = MobilePhone;
			mailContact.MobilePhone = ((mobilePhone != null) ? mobilePhone : "");
			string webPage = WebPage;
			mailContact.WebPage = ((webPage != null) ? webPage : "");
			string faxNumber = FaxNumber;
			mailContact.FaxNumber = ((faxNumber != null) ? faxNumber : "");
			string pagerNumber = PagerNumber;
			mailContact.PagerNumber = ((pagerNumber != null) ? pagerNumber : "");
			string workAddress = WorkAddress;
			mailContact.WorkAddress = ((workAddress != null) ? workAddress : "");
			string officeLocation = OfficeLocation;
			mailContact.OfficeLocation = ((officeLocation != null) ? officeLocation : "");
			string homeAddress = HomeAddress;
			mailContact.HomeAddress = ((homeAddress != null) ? homeAddress : "");
			string custom = Custom1;
			mailContact.Custom1 = ((custom != null) ? custom : "");
			string custom2 = Custom2;
			mailContact.Custom2 = ((custom2 != null) ? custom2 : "");
			string custom3 = Custom3;
			mailContact.Custom3 = ((custom3 != null) ? custom3 : "");
			string custom4 = Custom4;
			mailContact.Custom4 = ((custom4 != null) ? custom4 : "");
			string middleName = MiddleName;
			mailContact.MiddleName = ((middleName != null) ? middleName : "");
			string birthday = Birthday;
			mailContact.Birthday = ((birthday != null) ? birthday : "");
			string otherPhone = OtherPhone;
			mailContact.OtherPhone = ((otherPhone != null) ? otherPhone : "");
			string spouseName = SpouseName;
			mailContact.SpouseName = ((spouseName != null) ? spouseName : "");
			string anniversary = Anniversary;
			mailContact.Anniversary = ((anniversary != null) ? anniversary : "");
			string assistantName = AssistantName;
			mailContact.AssistantName = ((assistantName != null) ? assistantName : "");
			string screenName = ScreenName;
			mailContact.ScreenName = ((screenName != null) ? screenName : "");
			string aimScreenName = AimScreenName;
			mailContact.AimScreenName = ((aimScreenName != null) ? aimScreenName : "");
			string popularityIndex = PopularityIndex;
			mailContact.PopularityIndex = ((popularityIndex != null) ? popularityIndex : "");
			string recordName = RecordName;
			mailContact.RecordName = ((recordName != null) ? recordName : "");
			string preferEmail = PreferEmail;
			mailContact.PreferEmail = ((preferEmail != null) ? preferEmail : "");
			string morkRowId = MorkRowId;
			mailContact.MorkRowId = ((morkRowId != null) ? morkRowId : "");
			mailContact.CardDavDirty = CardDavDirty;
			mailContact.IsGalEntry = IsGalEntry;
			mailContact.GalPushDirty = GalPushDirty;
			string galPersonaId = GalPersonaId;
			mailContact.GalPersonaId = ((galPersonaId != null) ? galPersonaId : "");
			string galPersonaChangeKey = GalPersonaChangeKey;
			mailContact.GalPersonaChangeKey = ((galPersonaChangeKey != null) ? galPersonaChangeKey : "");
			string galDirectoryItemId = GalDirectoryItemId;
			mailContact.GalDirectoryItemId = ((galDirectoryItemId != null) ? galDirectoryItemId : "");
			string galDirectoryChangeKey = GalDirectoryChangeKey;
			mailContact.GalDirectoryChangeKey = ((galDirectoryChangeKey != null) ? galDirectoryChangeKey : "");
			string ewsPersonalItemId = EwsPersonalItemId;
			mailContact.EwsPersonalItemId = ((ewsPersonalItemId != null) ? ewsPersonalItemId : "");
			return mailContact;
		}
	}

	private const string ContactsFileName = "contacts.json";

	private const string AbookFileName = "abook.sqlite";

	private const string AbookMabFileName = "abook.mab";

	private const string CardDavSettingsFileName = "carddav-settings.json";

	private const string CardDavSecondaryAccountsFileName = "carddav-secondary-accounts.json";

	private const string CardDavSecretKey = "carddav";

	private const string SourceMabPathFileName = "abook-source-mab.txt";

	private const string ImportSql = "SELECT c.id, TRIM(COALESCE(c.name, '')) AS card_name, TRIM(p.value) AS email FROM cards c INNER JOIN properties p ON p.card_id = c.id WHERE p.name = 'PrimaryEmail' AND TRIM(COALESCE(p.value, '')) <> '' ORDER BY card_name, email;";

	public static string LastError;

	public static int LastCardDavPushCount;

	public static int LastCardDavRemotePullCount = 0;

	public static int LastCardDavSecondaryBooksMerged = 0;

	public static int LastCardDavDiscoveredBooks = 0;

	public static DateTime LastCardDavBidirectionalUtc = DateTime.MinValue;

	public static int LastGalPersonalPushCount;

	public static bool LastImportSkipped;

	public static int LastCardDavLiveOverlayCount = 0;

	public static int LastCardDav412BatchResolved = 0;

	public static string ContactsJsonPath()
	{
		return Path.Combine(AccountStore.DataRoot(), "contacts.json");
	}

	public static List<MailContact> LoadContacts()
	{
		string path = ContactsJsonPath();
		List<MailContact> result;
		if (!File.Exists(path))
		{
			result = new List<MailContact>();
		}
		else
		{
			try
			{
				string input = File.ReadAllText(path, Encoding.UTF8);
				JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
				List<ContactDto> list = javaScriptSerializer.Deserialize<List<ContactDto>>(input);
				if (list == null)
				{
					result = new List<MailContact>();
				}
				else
				{
					List<MailContact> list2 = new List<MailContact>();
					foreach (ContactDto item in list)
					{
						if (item != null)
						{
							MailContact mailContact = item.ToModel();
							if (mailContact != null && !string.IsNullOrWhiteSpace(mailContact.Email))
							{
								list2.Add(mailContact);
							}
						}
					}
					result = list2;
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = new List<MailContact>();
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}

	public static int TryImportFromProfile(string profileDir)
	{
		LastError = null;
		LastImportSkipped = false;
		int result;
		if (string.IsNullOrWhiteSpace(profileDir))
		{
			LastError = "Profile folder is required.";
			result = -1;
		}
		else
		{
			string path = profileDir.Trim();
			List<MailContact> list = null;
			string text = Path.Combine(path, "abook.sqlite");
			if (File.Exists(text))
			{
				try
				{
					list = ReadContactsFromAbookSqlite(text);
				}
				catch (Exception ex)
				{
					ProjectData.SetProjectError(ex);
					Exception ex2 = ex;
					LastError = ex2.Message;
					result = -1;
					ProjectData.ClearProjectError();
					goto IL_017e;
				}
			}
			if (list == null)
			{
				list = new List<MailContact>();
			}
			string text2 = Path.Combine(path, "abook.mab");
			if (File.Exists(text2))
			{
				TrySaveSourceMabPath(text2);
				MorkAbookParser.TryValidateSourceMabByteRoundTrip(text2);
				List<MailContact> list2 = MorkAbookParser.TryParseContacts(text2);
				if (list2 != null && list2.Count > 0)
				{
					list = MergeContacts(list, list2);
				}
				else if (list.Count == 0 && !string.IsNullOrEmpty(MorkAbookParser.LastError))
				{
					LastError = MorkAbookParser.LastError;
					result = -1;
					goto IL_017e;
				}
			}
			else if (list.Count == 0)
			{
				string text3 = Path.Combine(path, "abook.ldif");
				if (File.Exists(text3))
				{
					List<MailContact> list3 = MorkAbookParser.TryParseContactsFromLdif(text3);
					if (list3 != null && list3.Count > 0)
					{
						list = list3;
					}
				}
				if (list.Count == 0)
				{
					LastImportSkipped = true;
					result = 0;
					goto IL_017e;
				}
			}
			if (!SaveContacts(list))
			{
				result = -1;
			}
			else
			{
				if (File.Exists(text2))
				{
					MorkProfileAcceptanceStore.RecordProfileImport(text2, list.Count, MorkAbookParser.LastSourceMabRoundTripValidated, MorkAbookParser.LastSourceMabRoundTripByteIdentical, MorkAbookParser.LastError);
				}
				result = list.Count;
			}
		}
		goto IL_017e;
		IL_017e:
		return result;
	}

	public static bool TryExportContactsLdif(string destPath)
	{
		LastError = null;
		return MorkAbookParser.ExportContactsToLdif(LoadContacts(), destPath);
	}

	public static bool TryExportContactsVCard(string destPath)
	{
		LastError = null;
		return MorkAbookParser.ExportContactsToVCardFile(LoadContacts(), destPath);
	}

	public static bool TryExportContactsMork(string destPath)
	{
		LastError = null;
		int contactCount = 0;
		if (!MorkAbookParser.TryValidateThunderbirdFixture(ref contactCount))
		{
			LastError = ((MorkAbookParser.LastError != null) ? MorkAbookParser.LastError : "Mork golden self-test failed.");
			return false;
		}
		string text = TryLoadSourceMabPath();
		List<MailContact> contacts = LoadContacts();
		if (text.Length > 0)
		{
			MorkAbookParser.TryValidateSourceMabByteRoundTrip(text);
			if (MorkAbookParser.TryContactsUnchangedVersusSource(text, contacts))
			{
				if (MorkAbookParser.TryExportByteIdenticalSourceCopy(text, contacts, destPath) && MorkAbookParser.TryVerifyExportBytesMatchSource(text, destPath))
				{
					return true;
				}
				bool byteIdentical = false;
				if (MorkAbookParser.TryWriteMorkPreservingSource(text, contacts, destPath, ref byteIdentical) && byteIdentical)
				{
					MorkAbookParser.LastMabExportByteIdentical = true;
					if (MorkAbookParser.TryVerifyExportBytesMatchSource(text, destPath))
					{
						return true;
					}
				}
				LastError = "Unchanged abook requires byte-identical .mab export (slice 13 REAL gate).";
				return false;
			}
			bool byteIdentical2 = false;
			if (MorkAbookParser.TryWriteMorkPreservingSource(text, contacts, destPath, ref byteIdentical2))
			{
				MorkAbookParser.LastMabExportByteIdentical = byteIdentical2;
				if (MorkAbookParser.TryVerifyExportBytesMatchSource(text, destPath))
				{
					return true;
				}
			}
			if (MorkAbookParser.TryWriteMorkFullEncoderFromSource(text, contacts, destPath))
			{
				LastError = "Edited contacts exported with full Mork encoder — not byte-identical TB .mab (slice 13 Open).";
				return true;
			}
			LastError = ((MorkAbookParser.LastError != null) ? MorkAbookParser.LastError : "Mork export failed for edited contacts.");
			return false;
		}
		return MorkAbookParser.TryWriteMorkFromContacts(contacts, destPath);
	}

	public static List<MailContact> LoadLiveContacts()
	{
		return CardDavLiveBookCache.GetLiveContacts();
	}

	public static List<MailContact> TryFetchLiveCardDavContacts()
	{
		LastError = null;
		string serverUrl = null;
		string username = null;
		string password = null;
		if (!TryLoadCardDavSettings(ref serverUrl, ref username, ref password))
		{
			return null;
		}
		List<CardDavSyncService.CardDavRemoteContact> list = CardDavSyncService.FetchRemoteContacts(serverUrl, username, password);
		if (list == null)
		{
			LastError = CardDavSyncService.LastError;
			return null;
		}
		List<MailContact> list2 = new List<MailContact>();
		foreach (CardDavSyncService.CardDavRemoteContact item in list)
		{
			if (item != null && item.Contact != null)
			{
				if (!string.IsNullOrWhiteSpace(item.Etag))
				{
					item.Contact.CardDavEtag = item.Etag;
				}
				list2.Add(item.Contact);
			}
		}
		return TryOverlayLocalCardDavEditsOnRemote(list2);
	}

	public static List<MailContact> TryOverlayLocalCardDavEditsOnRemote(List<MailContact> remote)
	{
		Dictionary<string, MailContact> dictionary = new Dictionary<string, MailContact>(StringComparer.OrdinalIgnoreCase);
		if (remote != null)
		{
			foreach (MailContact item in remote)
			{
				if (item != null && !string.IsNullOrWhiteSpace(item.Email))
				{
					dictionary[item.Email.Trim()] = item;
				}
			}
		}
		foreach (MailContact item2 in LoadContacts())
		{
			if (item2 != null && !string.IsNullOrWhiteSpace(item2.Email))
			{
				string key = item2.Email.Trim();
				if (item2.CardDavDirty || !dictionary.ContainsKey(key))
				{
					dictionary[key] = item2;
				}
			}
		}
		List<MailContact> list = new List<MailContact>(dictionary.Values);
		list.Sort([SpecialName] (MailContact a, MailContact b) => string.Compare(a.ListCaption(), b.ListCaption(), StringComparison.OrdinalIgnoreCase));
		LastCardDavLiveOverlayCount = list.Count;
		return list;
	}

	public static bool TryRemoveContact(string email, bool deleteRemoteCardDav = true)
	{
		LastError = null;
		string text = ((email != null) ? email : "").Trim();
		if (text.Length == 0)
		{
			LastError = "Email is required.";
			return false;
		}
		List<MailContact> list = LoadContacts();
		if (list.RemoveAll([SpecialName] (MailContact c) => (c != null && string.Equals(c.Email, text, StringComparison.OrdinalIgnoreCase)) ? true : false) <= 0)
		{
			LastError = "Contact not found.";
			return false;
		}
		if (!SaveContacts(list))
		{
			return false;
		}
		if (deleteRemoteCardDav)
		{
			TryDeleteContactCardDav(text);
		}
		return true;
	}

	public static bool TryDeleteContactCardDav(string email)
	{
		LastError = null;
		string serverUrl = null;
		string username = null;
		string password = null;
		if (!TryLoadCardDavSettings(ref serverUrl, ref username, ref password))
		{
			return false;
		}
		if (!CardDavSyncService.DeleteContact(serverUrl, username, password, email))
		{
			LastError = CardDavSyncService.LastError;
			return false;
		}
		return true;
	}

	public static bool TryPushContactCardDav(string serverUrl, string username, string password, MailContact contact)
	{
		LastError = null;
		if (!CardDavSyncService.PutContactAutoMerge(serverUrl, username, password, contact))
		{
			LastError = CardDavSyncService.LastError;
			return false;
		}
		List<MailContact> list = new List<MailContact>();
		list.Add(contact);
		if (TryMergeContacts(list) < 0)
		{
			LastError = ((LastError != null) ? LastError : "Contact pushed but local save failed.");
			return false;
		}
		SaveCardDavSettings(serverUrl, username, password);
		return true;
	}

	public static int TryPushAllLocalContactsCardDav(string serverUrl, string username, string password)
	{
		LastError = null;
		int num = 0;
		List<MailContact> list = LoadContacts();
		list.Sort([SpecialName] (MailContact a, MailContact b) =>
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
			if (a.CardDavDirty == b.CardDavDirty)
			{
				return string.Compare(a.Email, b.Email, StringComparison.OrdinalIgnoreCase);
			}
			return (!a.CardDavDirty) ? 1 : (-1);
		});
		bool flag = false;
		foreach (MailContact item in list)
		{
			if (item != null && !string.IsNullOrWhiteSpace(item.Email) && (item.CardDavDirty || string.IsNullOrWhiteSpace(item.CardDavEtag)) && TryPushContactCardDav(serverUrl, username, password, item))
			{
				item.CardDavDirty = false;
				num = checked(num + 1);
				flag = true;
			}
		}
		if (flag)
		{
			SaveContacts(list);
		}
		return num;
	}

	public static int TryMergeContacts(List<MailContact> incoming)
	{
		LastError = null;
		if (incoming == null || incoming.Count == 0)
		{
			return 0;
		}
		List<MailContact> contacts = MergeContacts(LoadContacts(), incoming);
		if (!SaveContacts(contacts))
		{
			return -1;
		}
		return incoming.Count;
	}

	public static int TrySyncCardDav(string serverUrl, string username, string password)
	{
		LastError = null;
		string text = ((serverUrl != null) ? serverUrl : "").Trim();
		if (text.Length == 0)
		{
			LastError = "CardDAV URL is required.";
			return -1;
		}
		List<CardDavSyncService.CardDavRemoteContact> list = CardDavSyncService.FetchRemoteContacts(text, username, password);
		if (list == null)
		{
			LastError = ((CardDavSyncService.LastError != null) ? CardDavSyncService.LastError : "CardDAV sync failed.");
			return -1;
		}
		List<MailContact> list2 = new List<MailContact>();
		foreach (CardDavSyncService.CardDavRemoteContact item in list)
		{
			if (item != null && item.Contact != null)
			{
				if (!string.IsNullOrWhiteSpace(item.Etag))
				{
					item.Contact.CardDavEtag = item.Etag;
				}
				list2.Add(item.Contact);
			}
		}
		List<MailContact> list3 = LoadContacts();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (MailContact item2 in list3)
		{
			if (item2 != null && item2.CardDavDirty && !string.IsNullOrWhiteSpace(item2.Email))
			{
				hashSet.Add(item2.Email.Trim());
			}
		}
		List<MailContact> list4 = MergeContacts(list3, list2);
		foreach (MailContact item3 in list4)
		{
			if (item3 != null && !string.IsNullOrWhiteSpace(item3.Email) && !string.IsNullOrWhiteSpace(item3.CardDavEtag) && !hashSet.Contains(item3.Email.Trim()))
			{
				item3.CardDavDirty = false;
			}
		}
		if (!SaveContacts(list4))
		{
			return -1;
		}
		LastCardDavRemotePullCount = list2.Count;
		CardDavLiveBookCache.NotifyLocalBookUpdated(list4);
		SaveCardDavSettings(text, username, password);
		return list4.Count;
	}

	private static int TryMergeCardDavRemoteOnly(string serverUrl, string username, string password)
	{
		LastError = null;
		string text = ((serverUrl != null) ? serverUrl : "").Trim();
		if (text.Length == 0)
		{
			return 0;
		}
		List<CardDavSyncService.CardDavRemoteContact> list = CardDavSyncService.FetchRemoteContacts(text, username, password);
		if (list == null)
		{
			return 0;
		}
		List<MailContact> list2 = new List<MailContact>();
		foreach (CardDavSyncService.CardDavRemoteContact item in list)
		{
			if (item != null && item.Contact != null)
			{
				if (!string.IsNullOrWhiteSpace(item.Etag))
				{
					item.Contact.CardDavEtag = item.Etag;
				}
				list2.Add(item.Contact);
			}
		}
		if (list2.Count == 0)
		{
			return 0;
		}
		List<MailContact> list3 = LoadContacts();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (MailContact item2 in list3)
		{
			if (item2 != null && item2.CardDavDirty && !string.IsNullOrWhiteSpace(item2.Email))
			{
				hashSet.Add(item2.Email.Trim());
			}
		}
		List<MailContact> list4 = MergeContacts(list3, list2);
		foreach (MailContact item3 in list4)
		{
			if (item3 != null && !string.IsNullOrWhiteSpace(item3.Email) && !string.IsNullOrWhiteSpace(item3.CardDavEtag) && !hashSet.Contains(item3.Email.Trim()))
			{
				item3.CardDavDirty = false;
			}
		}
		if (!SaveContacts(list4))
		{
			return -1;
		}
		CardDavLiveBookCache.NotifyLocalBookUpdated(list4);
		return list2.Count;
	}

	public static void SaveCardDavSettings(string serverUrl, string username, string password, string conflictOn412Mode = null)
	{
		string text = ((serverUrl != null) ? serverUrl : "").Trim();
		string username2 = ((username != null) ? username : "").Trim();
		if (text.Length == 0)
		{
			return;
		}
		try
		{
			string text2 = ((conflictOn412Mode != null) ? conflictOn412Mode : CardDavSyncService.ConflictOn412Mode).Trim();
			if (text2.Length == 0)
			{
				text2 = "ClientMerge";
			}
			CardDavSyncService.ConflictOn412Mode = text2;
			CardDavSettingsDto cardDavSettingsDto = new CardDavSettingsDto();
			cardDavSettingsDto.ServerUrl = text;
			cardDavSettingsDto.Username = username2;
			cardDavSettingsDto.ConflictOn412Mode = text2;
			CardDavSettingsDto obj = cardDavSettingsDto;
			JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
			File.WriteAllText(CardDavSettingsPath(), javaScriptSerializer.Serialize(obj), Encoding.UTF8);
			if (password != null)
			{
				AccountStore.SavePassword("carddav", password);
			}
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
	}

	public static bool TryLoadCardDavSettings(ref string serverUrl, ref string username, ref string password)
	{
		serverUrl = null;
		username = null;
		password = null;
		string path = CardDavSettingsPath();
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
				CardDavSettingsDto cardDavSettingsDto = javaScriptSerializer.Deserialize<CardDavSettingsDto>(File.ReadAllText(path, Encoding.UTF8));
				if (cardDavSettingsDto == null || string.IsNullOrWhiteSpace(cardDavSettingsDto.ServerUrl))
				{
					result = false;
				}
				else
				{
					serverUrl = cardDavSettingsDto.ServerUrl.Trim();
					string username2 = cardDavSettingsDto.Username;
					username = ((username2 != null) ? username2 : "").Trim();
					username2 = cardDavSettingsDto.ConflictOn412Mode;
					string text = ((username2 != null) ? username2 : "").Trim();
					if (text.Length > 0)
					{
						CardDavSyncService.ConflictOn412Mode = text;
					}
					result = AccountStore.TryGetPassword("carddav", ref password);
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

	public static int TrySyncStoredCardDav()
	{
		string serverUrl = null;
		string username = null;
		string password = null;
		if (!TryLoadCardDavSettings(ref serverUrl, ref username, ref password))
		{
			return 0;
		}
		return TrySyncCardDav(serverUrl, username, password);
	}

	public static int TryApplyThunderbirdCardDavFromProfile(string profileDir)
	{
		LastError = null;
		List<ThunderbirdCardDavAccount> list = ThunderbirdCardDavImporter.TryLoadFromProfile(profileDir);
		if (list == null || list.Count == 0)
		{
			return 0;
		}
		SaveThunderbirdCardDavSecondaryAccounts(list);
		ThunderbirdCardDavAccount thunderbirdCardDavAccount = list[0];
		if (thunderbirdCardDavAccount == null || string.IsNullOrWhiteSpace(thunderbirdCardDavAccount.ServerUrl))
		{
			return 0;
		}
		string serverUrl = thunderbirdCardDavAccount.ServerUrl;
		ThunderbirdCardDavAccount thunderbirdCardDavAccount2 = thunderbirdCardDavAccount;
		SaveCardDavSettings(serverUrl, (thunderbirdCardDavAccount2.Username != null) ? thunderbirdCardDavAccount2.Username : "", null);
		return list.Count;
	}

	public static void SaveThunderbirdCardDavSecondaryAccounts(List<ThunderbirdCardDavAccount> accounts)
	{
		if (accounts == null || accounts.Count == 0)
		{
			return;
		}
		try
		{
			List<CardDavSecondaryAccountDto> list = new List<CardDavSecondaryAccountDto>();
			int num = 0;
			foreach (ThunderbirdCardDavAccount account in accounts)
			{
				if (account != null && !string.IsNullOrWhiteSpace(account.ServerUrl))
				{
					num = checked(num + 1);
					ThunderbirdCardDavAccount thunderbirdCardDavAccount = account;
					string text = ((thunderbirdCardDavAccount.BookKey != null) ? thunderbirdCardDavAccount.BookKey : "").Trim();
					if (text.Length == 0)
					{
						text = "carddav-tb-" + num;
					}
					CardDavSecondaryAccountDto cardDavSecondaryAccountDto = new CardDavSecondaryAccountDto
					{
						ServerUrl = account.ServerUrl.Trim()
					};
					thunderbirdCardDavAccount = account;
					cardDavSecondaryAccountDto.Username = ((thunderbirdCardDavAccount.Username != null) ? thunderbirdCardDavAccount.Username : "").Trim();
					cardDavSecondaryAccountDto.BookKey = text;
					list.Add(cardDavSecondaryAccountDto);
				}
			}
			JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
			File.WriteAllText(Path.Combine(AccountStore.DataRoot(), "carddav-secondary-accounts.json"), javaScriptSerializer.Serialize(list), Encoding.UTF8);
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
	}

	public static List<ThunderbirdCardDavAccount> TryLoadThunderbirdCardDavSecondaryAccounts()
	{
		List<ThunderbirdCardDavAccount> list = new List<ThunderbirdCardDavAccount>();
		try
		{
			string path = Path.Combine(AccountStore.DataRoot(), "carddav-secondary-accounts.json");
			if (!File.Exists(path))
			{
				return list;
			}
			JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
			List<CardDavSecondaryAccountDto> list2 = javaScriptSerializer.Deserialize<List<CardDavSecondaryAccountDto>>(File.ReadAllText(path, Encoding.UTF8));
			if (list2 == null)
			{
				return list;
			}
			foreach (CardDavSecondaryAccountDto item in list2)
			{
				if (item != null && !string.IsNullOrWhiteSpace(item.ServerUrl))
				{
					ThunderbirdCardDavAccount thunderbirdCardDavAccount = new ThunderbirdCardDavAccount
					{
						ServerUrl = item.ServerUrl.Trim()
					};
					string username = item.Username;
					thunderbirdCardDavAccount.Username = ((username != null) ? username : "").Trim();
					string bookKey = item.BookKey;
					thunderbirdCardDavAccount.BookKey = ((bookKey != null) ? bookKey : "").Trim();
					list.Add(thunderbirdCardDavAccount);
				}
			}
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
		return list;
	}

	public static int TryRefreshDiscoveredCardDavBooksFromServer(string serverUrl, string username, string password)
	{
		LastCardDavDiscoveredBooks = 0;
		List<string> list = CardDavSyncService.EnumerateAddressBookUrls(serverUrl, username, password);
		if (list == null || list.Count == 0)
		{
			return 0;
		}
		List<ThunderbirdCardDavAccount> list2 = new List<ThunderbirdCardDavAccount>();
		int num = 0;
		foreach (string item in list)
		{
			if (!string.IsNullOrWhiteSpace(item))
			{
				num = checked(num + 1);
				list2.Add(new ThunderbirdCardDavAccount
				{
					ServerUrl = item.Trim(),
					Username = ((username != null) ? username : "").Trim(),
					BookKey = "carddav-disc-" + num
				});
			}
		}
		SaveThunderbirdCardDavSecondaryAccounts(list2);
		LastCardDavDiscoveredBooks = list2.Count;
		return list2.Count;
	}

	public static int TryMergeSecondaryThunderbirdCardDavBooks()
	{
		LastCardDavSecondaryBooksMerged = 0;
		string serverUrl = null;
		string username = null;
		string password = null;
		if (!TryLoadCardDavSettings(ref serverUrl, ref username, ref password))
		{
			return 0;
		}
		if (string.IsNullOrEmpty(password))
		{
			return 0;
		}
		List<ThunderbirdCardDavAccount> list = TryLoadThunderbirdCardDavSecondaryAccounts();
		if (list == null || list.Count == 0)
		{
			return 0;
		}
		int num = 0;
		int num2 = 0;
		checked
		{
			foreach (ThunderbirdCardDavAccount item in list)
			{
				if (item == null || string.IsNullOrWhiteSpace(item.ServerUrl))
				{
					continue;
				}
				num2++;
				if (!string.Equals(item.ServerUrl.Trim(), serverUrl.Trim(), StringComparison.OrdinalIgnoreCase))
				{
					ThunderbirdCardDavAccount thunderbirdCardDavAccount = item;
					string text = ((thunderbirdCardDavAccount.Username != null) ? thunderbirdCardDavAccount.Username : "").Trim();
					if (text.Length == 0)
					{
						text = username;
					}
					string password2 = password;
					thunderbirdCardDavAccount = item;
					string text2 = ((thunderbirdCardDavAccount.BookKey != null) ? thunderbirdCardDavAccount.BookKey : "").Trim();
					if (text2.Length == 0)
					{
						text2 = "carddav-tb-" + num2;
					}
					string password3 = null;
					if (AccountStore.TryGetPassword(text2, ref password3) && !string.IsNullOrEmpty(password3))
					{
						password2 = password3;
					}
					int num3 = TryMergeCardDavRemoteOnly(item.ServerUrl, text, password2);
					if (num3 > 0)
					{
						num += num3;
					}
				}
			}
			LastCardDavSecondaryBooksMerged = num;
			return num;
		}
	}

	public static int CountGalPushDirtyContacts()
	{
		int num = 0;
		foreach (MailContact item in LoadContacts())
		{
			if (item != null && item.IsGalEntry && item.GalPushDirty)
			{
				num = checked(num + 1);
			}
		}
		return num;
	}

	public static int CountCardDavDirtyContacts()
	{
		int num = 0;
		foreach (MailContact item in LoadContacts())
		{
			if (item != null && !string.IsNullOrWhiteSpace(item.Email) && item.CardDavDirty)
			{
				num = checked(num + 1);
			}
		}
		return num;
	}

	public static int TrySyncStoredCardDavBidirectional()
	{
		LastCardDavPushCount = 0;
		string serverUrl = null;
		string username = null;
		string password = null;
		bool flag = TryLoadCardDavSettings(ref serverUrl, ref username, ref password);
		if (flag && CountCardDavDirtyContacts() > 0)
		{
			int val = TryPushAllLocalContactsCardDav(serverUrl, username, password);
			LastCardDavPushCount = Math.Max(0, val);
		}
		int num = TrySyncStoredCardDav();
		if (num < 0)
		{
			return num;
		}
		if (!flag)
		{
			return num;
		}
		int num2 = TryPushAllLocalContactsCardDav(serverUrl, username, password);
		LastCardDavPushCount = Math.Max(LastCardDavPushCount, Math.Max(0, num2));
		if (num2 < 0)
		{
			return num;
		}
		LastCardDavBidirectionalUtc = DateTime.UtcNow;
		int val2 = TryMergeSecondaryThunderbirdCardDavBooks();
		return Math.Max(Math.Max(num, num2), val2);
	}

	private static string CardDavSettingsPath()
	{
		return Path.Combine(AccountStore.DataRoot(), "carddav-settings.json");
	}

	private static void TrySaveSourceMabPath(string mabPath)
	{
		try
		{
			File.WriteAllText(Path.Combine(AccountStore.DataRoot(), "abook-source-mab.txt"), mabPath.Trim(), Encoding.UTF8);
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
	}

	public static bool TryValidateStoredSourceMab()
	{
		string text = TryLoadSourceMabPath();
		if (text.Length == 0)
		{
			return false;
		}
		return MorkAbookParser.TryValidateSourceMabByteRoundTrip(text);
	}

	public static string TryGetStoredSourceMabPath()
	{
		return TryLoadSourceMabPath();
	}

	private static string TryLoadSourceMabPath()
	{
		string result;
		try
		{
			string path = Path.Combine(AccountStore.DataRoot(), "abook-source-mab.txt");
			if (!File.Exists(path))
			{
				result = "";
			}
			else
			{
				string text = File.ReadAllText(path, Encoding.UTF8).Trim();
				result = ((text.Length != 0 && File.Exists(text)) ? text : "");
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

	public static int TryResolveAllCardDav412DirtyContacts()
	{
		LastCardDav412BatchResolved = 0;
		string serverUrl = null;
		string username = null;
		string password = null;
		if (!TryLoadCardDavSettings(ref serverUrl, ref username, ref password))
		{
			return 0;
		}
		if (string.IsNullOrEmpty(password))
		{
			return 0;
		}
		string conflictOn412Mode = CardDavSyncService.ConflictOn412Mode;
		int num = 0;
		foreach (MailContact item in LoadContacts())
		{
			if (item != null && !string.IsNullOrWhiteSpace(item.Email) && item.CardDavDirty && TryResolveCardDav412ConflictForEmail(item.Email, serverUrl, username, password, conflictOn412Mode))
			{
				num = checked(num + 1);
			}
		}
		LastCardDav412BatchResolved = num;
		return num;
	}

	public static bool TryResolveCardDav412ConflictForEmail(string email, string serverUrl, string username, string password, string perContactMode)
	{
		LastError = null;
		string text = ((email != null) ? email : "").Trim();
		if (text.Length == 0)
		{
			LastError = "Email required.";
			return false;
		}
		string text2 = ((perContactMode != null) ? perContactMode : CardDavSyncService.ConflictOn412Mode).Trim();
		if (text2.Length == 0)
		{
			text2 = "ClientMerge";
		}
		string conflictOn412Mode = CardDavSyncService.ConflictOn412Mode;
		CardDavSyncService.ConflictOn412Mode = text2;
		try
		{
			MailContact mailContact = null;
			foreach (MailContact item in LoadContacts())
			{
				if (item != null && string.Equals(item.Email, text, StringComparison.OrdinalIgnoreCase))
				{
					mailContact = item;
					break;
				}
			}
			if (mailContact == null)
			{
				LastError = "Contact not found locally.";
				return false;
			}
			mailContact.CardDavDirty = true;
			return TryPushContactCardDav(serverUrl, username, password, mailContact);
		}
		finally
		{
			CardDavSyncService.ConflictOn412Mode = conflictOn412Mode;
		}
	}

	public static bool SaveContacts(List<MailContact> contacts)
	{
		LastError = null;
		bool result;
		if (contacts == null)
		{
			LastError = "contacts is required.";
			result = false;
		}
		else
		{
			try
			{
				List<ContactDto> list = new List<ContactDto>();
				foreach (MailContact contact in contacts)
				{
					if (contact != null && !string.IsNullOrWhiteSpace(contact.Email))
					{
						list.Add(ContactDto.FromModel(contact));
					}
				}
				JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
				string contents = javaScriptSerializer.Serialize(list);
				File.WriteAllText(ContactsJsonPath(), contents, Encoding.UTF8);
				CardDavLiveBookCache.NotifyLocalBookUpdated(contacts);
				result = true;
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Exception ex2 = ex;
				LastError = ex2.Message;
				result = false;
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}

	private static List<MailContact> ReadContactsFromAbookSqlite(string abookPath)
	{
		Dictionary<string, MailContact> dictionary = new Dictionary<string, MailContact>(StringComparer.OrdinalIgnoreCase);
		Dictionary<int, string> dictionary2 = new Dictionary<int, string>();
		using (SQLiteConnection sQLiteConnection = new SQLiteConnection("Data Source=" + abookPath + ";Mode=ReadOnly;"))
		{
			sQLiteConnection.Open();
			LoadDisplayNameProperties(sQLiteConnection, dictionary2);
			SQLiteCommand sQLiteCommand = new SQLiteCommand("SELECT c.id, TRIM(COALESCE(c.name, '')) AS card_name, TRIM(p.value) AS email FROM cards c INNER JOIN properties p ON p.card_id = c.id WHERE p.name = 'PrimaryEmail' AND TRIM(COALESCE(p.value, '')) <> '' ORDER BY card_name, email;", sQLiteConnection);
			try
			{
				SQLiteDataReader sQLiteDataReader = sQLiteCommand.ExecuteReader();
				try
				{
					while (sQLiteDataReader.Read())
					{
						int @int = sQLiteDataReader.GetInt32(0);
						string text = (sQLiteDataReader.IsDBNull(1) ? "" : sQLiteDataReader.GetString(1)).Trim();
						string text2 = (sQLiteDataReader.IsDBNull(2) ? "" : sQLiteDataReader.GetString(2)).Trim();
						if (text2.Length != 0)
						{
							string text3 = text;
							if (text3.Length == 0 && dictionary2.ContainsKey(@int))
							{
								text3 = dictionary2[@int];
							}
							MailContact mailContact = new MailContact();
							mailContact.DisplayName = text3;
							mailContact.Email = text2;
							MailContact value = mailContact;
							string key = text2.ToLowerInvariant();
							if (!dictionary.ContainsKey(key))
							{
								dictionary[key] = value;
							}
						}
					}
				}
				finally
				{
					IDisposable disposable = sQLiteDataReader as IDisposable;
					if (disposable != null)
					{
						disposable.Dispose();
					}
				}
			}
			finally
			{
				((IDisposable)(object)sQLiteCommand)?.Dispose();
			}
		}
		List<MailContact> list = new List<MailContact>(dictionary.Values);
		list.Sort([SpecialName] (MailContact a, MailContact b) => string.Compare(a.ListCaption(), b.ListCaption(), StringComparison.OrdinalIgnoreCase));
		return list;
	}

	private static List<MailContact> MergeContacts(List<MailContact> primary, List<MailContact> extra)
	{
		Dictionary<string, MailContact> dictionary = new Dictionary<string, MailContact>(StringComparer.OrdinalIgnoreCase);
		if (primary != null)
		{
			foreach (MailContact item in primary)
			{
				if (item != null && !string.IsNullOrWhiteSpace(item.Email))
				{
					dictionary[item.Email.Trim().ToLowerInvariant()] = item;
				}
			}
		}
		if (extra != null)
		{
			foreach (MailContact item2 in extra)
			{
				if (item2 == null || string.IsNullOrWhiteSpace(item2.Email))
				{
					continue;
				}
				string key = item2.Email.Trim().ToLowerInvariant();
				if (dictionary.ContainsKey(key))
				{
					MailContact mailContact = dictionary[key];
					if (!string.IsNullOrWhiteSpace(item2.DisplayName))
					{
						mailContact.DisplayName = item2.DisplayName;
					}
					if (!string.IsNullOrWhiteSpace(item2.Company))
					{
						mailContact.Company = item2.Company;
					}
					if (!string.IsNullOrWhiteSpace(item2.WorkPhone))
					{
						mailContact.WorkPhone = item2.WorkPhone;
					}
					if (!string.IsNullOrWhiteSpace(item2.HomePhone))
					{
						mailContact.HomePhone = item2.HomePhone;
					}
					if (!string.IsNullOrWhiteSpace(item2.Notes))
					{
						mailContact.Notes = item2.Notes;
					}
					if (!string.IsNullOrWhiteSpace(item2.CardDavEtag))
					{
						mailContact.CardDavEtag = item2.CardDavEtag;
					}
					if (item2.CardDavDirty)
					{
						mailContact.CardDavDirty = true;
					}
					if (!string.IsNullOrWhiteSpace(item2.SecondaryEmail))
					{
						mailContact.SecondaryEmail = item2.SecondaryEmail;
					}
					if (!string.IsNullOrWhiteSpace(item2.FirstName))
					{
						mailContact.FirstName = item2.FirstName;
					}
					if (!string.IsNullOrWhiteSpace(item2.LastName))
					{
						mailContact.LastName = item2.LastName;
					}
					if (!string.IsNullOrWhiteSpace(item2.NickName))
					{
						mailContact.NickName = item2.NickName;
					}
					if (!string.IsNullOrWhiteSpace(item2.JobTitle))
					{
						mailContact.JobTitle = item2.JobTitle;
					}
					if (!string.IsNullOrWhiteSpace(item2.Department))
					{
						mailContact.Department = item2.Department;
					}
					if (!string.IsNullOrWhiteSpace(item2.MobilePhone))
					{
						mailContact.MobilePhone = item2.MobilePhone;
					}
					if (!string.IsNullOrWhiteSpace(item2.WebPage))
					{
						mailContact.WebPage = item2.WebPage;
					}
					if (!string.IsNullOrWhiteSpace(item2.FaxNumber))
					{
						mailContact.FaxNumber = item2.FaxNumber;
					}
					if (!string.IsNullOrWhiteSpace(item2.PagerNumber))
					{
						mailContact.PagerNumber = item2.PagerNumber;
					}
					if (!string.IsNullOrWhiteSpace(item2.WorkAddress))
					{
						mailContact.WorkAddress = item2.WorkAddress;
					}
					if (!string.IsNullOrWhiteSpace(item2.OfficeLocation))
					{
						mailContact.OfficeLocation = item2.OfficeLocation;
					}
					if (!string.IsNullOrWhiteSpace(item2.HomeAddress))
					{
						mailContact.HomeAddress = item2.HomeAddress;
					}
					if (!string.IsNullOrWhiteSpace(item2.Custom1))
					{
						mailContact.Custom1 = item2.Custom1;
					}
					if (!string.IsNullOrWhiteSpace(item2.Custom2))
					{
						mailContact.Custom2 = item2.Custom2;
					}
					if (!string.IsNullOrWhiteSpace(item2.Custom3))
					{
						mailContact.Custom3 = item2.Custom3;
					}
					if (!string.IsNullOrWhiteSpace(item2.Custom4))
					{
						mailContact.Custom4 = item2.Custom4;
					}
					if (!string.IsNullOrWhiteSpace(item2.MiddleName))
					{
						mailContact.MiddleName = item2.MiddleName;
					}
					if (!string.IsNullOrWhiteSpace(item2.Birthday))
					{
						mailContact.Birthday = item2.Birthday;
					}
					if (!string.IsNullOrWhiteSpace(item2.OtherPhone))
					{
						mailContact.OtherPhone = item2.OtherPhone;
					}
					if (!string.IsNullOrWhiteSpace(item2.SpouseName))
					{
						mailContact.SpouseName = item2.SpouseName;
					}
					if (!string.IsNullOrWhiteSpace(item2.Anniversary))
					{
						mailContact.Anniversary = item2.Anniversary;
					}
					if (!string.IsNullOrWhiteSpace(item2.AssistantName))
					{
						mailContact.AssistantName = item2.AssistantName;
					}
					if (item2.IsGalEntry)
					{
						mailContact.IsGalEntry = true;
					}
					if (item2.GalPushDirty)
					{
						mailContact.GalPushDirty = true;
					}
					if (!string.IsNullOrWhiteSpace(item2.GalPersonaId))
					{
						mailContact.GalPersonaId = item2.GalPersonaId;
					}
					if (!string.IsNullOrWhiteSpace(item2.GalPersonaChangeKey))
					{
						mailContact.GalPersonaChangeKey = item2.GalPersonaChangeKey;
					}
					if (!string.IsNullOrWhiteSpace(item2.GalDirectoryItemId))
					{
						mailContact.GalDirectoryItemId = item2.GalDirectoryItemId;
					}
					if (!string.IsNullOrWhiteSpace(item2.GalDirectoryChangeKey))
					{
						mailContact.GalDirectoryChangeKey = item2.GalDirectoryChangeKey;
					}
					if (!string.IsNullOrWhiteSpace(item2.EwsPersonalItemId))
					{
						mailContact.EwsPersonalItemId = item2.EwsPersonalItemId;
					}
				}
				else
				{
					dictionary[key] = item2;
				}
			}
		}
		List<MailContact> list = new List<MailContact>(dictionary.Values);
		list.Sort([SpecialName] (MailContact a, MailContact b) => string.Compare(a.ListCaption(), b.ListCaption(), StringComparison.OrdinalIgnoreCase));
		return list;
	}

	private static void LoadDisplayNameProperties(SQLiteConnection conn, Dictionary<int, string> displayNameByCardId)
	{
		SQLiteCommand sQLiteCommand = new SQLiteCommand("SELECT card_id, TRIM(value) FROM properties WHERE name = 'DisplayName' AND TRIM(COALESCE(value, '')) <> '';", conn);
		try
		{
			SQLiteDataReader sQLiteDataReader = sQLiteCommand.ExecuteReader();
			try
			{
				while (sQLiteDataReader.Read())
				{
					int @int = sQLiteDataReader.GetInt32(0);
					string text = (sQLiteDataReader.IsDBNull(1) ? "" : sQLiteDataReader.GetString(1)).Trim();
					if (text.Length != 0 && !displayNameByCardId.ContainsKey(@int))
					{
						displayNameByCardId[@int] = text;
					}
				}
			}
			finally
			{
				IDisposable disposable = sQLiteDataReader as IDisposable;
				if (disposable != null)
				{
					disposable.Dispose();
				}
			}
		}
		finally
		{
			((IDisposable)(object)sQLiteCommand)?.Dispose();
		}
	}
}
