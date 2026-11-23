using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

public sealed class CardDavSyncService
{
	public class CardDavRemoteContact
	{
		public MailContact Contact;

		public string Etag;

		[DebuggerNonUserCode]
		public CardDavRemoteContact()
		{
		}
	}

	public static string LastError;

	public static bool LastMergedAfter412;

	public static string Last412ConflictEmail = "";

	public static string ConflictOn412Mode = "ClientMerge";

	private CardDavSyncService()
	{
	}

	public static List<MailContact> FetchContacts(string serverUrl, string username, string password)
	{
		LastError = null;
		string text = ResolveAddressBookUrl(serverUrl, username, password);
		if (text.Length == 0)
		{
			return null;
		}
		string body = "<?xml version=\"1.0\" encoding=\"utf-8\"?><C:addressbook-query xmlns:D=\"DAV:\" xmlns:C=\"urn:ietf:params:xml:ns:carddav\"><D:prop><D:getetag/><C:address-data/></D:prop></C:addressbook-query>";
		string text2 = DavRequest(text, "REPORT", username, password, body, "1");
		if (text2.Length == 0)
		{
			return null;
		}
		return ParseAddressDataResponses(text2);
	}

	public static List<CardDavRemoteContact> FetchRemoteContacts(string serverUrl, string username, string password)
	{
		LastError = null;
		string text = ResolveAddressBookUrl(serverUrl, username, password);
		if (text.Length == 0)
		{
			return null;
		}
		string body = "<?xml version=\"1.0\" encoding=\"utf-8\"?><C:addressbook-query xmlns:D=\"DAV:\" xmlns:C=\"urn:ietf:params:xml:ns:carddav\"><D:prop><D:getetag/><C:address-data/></D:prop></C:addressbook-query>";
		string text2 = DavRequest(text, "REPORT", username, password, body, "1");
		if (text2.Length == 0)
		{
			return null;
		}
		return ParseRemoteContacts(text2);
	}

	public static bool PutContact(string serverUrl, string username, string password, MailContact contact)
	{
		LastError = null;
		if (contact == null || string.IsNullOrWhiteSpace(contact.Email))
		{
			LastError = "Contact email is required.";
			return false;
		}
		string text = ResolveAddressBookUrl(serverUrl, username, password);
		if (text.Length == 0)
		{
			return false;
		}
		return PutContactAutoMerge(serverUrl, username, password, contact);
	}

	public static bool PutContactAutoMerge(string serverUrl, string username, string password, MailContact contact)
	{
		LastError = null;
		LastMergedAfter412 = false;
		if (contact == null || string.IsNullOrWhiteSpace(contact.Email))
		{
			LastError = "Contact email is required.";
			return false;
		}
		string text = ResolveAddressBookUrl(serverUrl, username, password);
		if (text.Length == 0)
		{
			return false;
		}
		string text2 = BuildContactResourceUrl(text, contact.Email);
		string vcard = BuildVCard(contact);
		MailContact mailContact = contact;
		string ifMatchEtag = ((mailContact.CardDavEtag != null) ? mailContact.CardDavEtag : "").Trim();
		int httpStatus = 0;
		string responseEtag = null;
		if (DavPutStatus(text2, username, password, vcard, ifMatchEtag, ref httpStatus, ref responseEtag))
		{
			if (!string.IsNullOrWhiteSpace(responseEtag))
			{
				contact.CardDavEtag = responseEtag;
			}
			return true;
		}
		if (httpStatus != 412)
		{
			return false;
		}
		Last412ConflictEmail = contact.Email.Trim();
		MailContact contact2 = null;
		string etag = null;
		if (!TryGetContactResource(text2, username, password, ref contact2, ref etag))
		{
			LastError = ((LastError != null) ? LastError : "CardDAV 412 — could not refetch remote contact.");
			return false;
		}
		if (string.Equals(ConflictOn412Mode, "ServerWins", StringComparison.OrdinalIgnoreCase))
		{
			ApplyRemoteContactWins(contact, contact2);
		}
		else
		{
			MergeContactForConflict(contact, contact2);
		}
		if (!string.IsNullOrWhiteSpace(etag))
		{
			contact.CardDavEtag = etag;
		}
		vcard = BuildVCard(contact);
		mailContact = contact;
		ifMatchEtag = ((mailContact.CardDavEtag != null) ? mailContact.CardDavEtag : "").Trim();
		LastMergedAfter412 = true;
		if (DavPutStatus(text2, username, password, vcard, ifMatchEtag, ref httpStatus, ref responseEtag))
		{
			if (!string.IsNullOrWhiteSpace(responseEtag))
			{
				contact.CardDavEtag = responseEtag;
			}
			return true;
		}
		LastError = ((LastError != null) ? LastError : "CardDAV merge retry failed.");
		return false;
	}

	public static bool DeleteContact(string serverUrl, string username, string password, string email)
	{
		LastError = null;
		string text = ((email != null) ? email : "").Trim();
		bool result;
		if (text.Length == 0)
		{
			LastError = "Email is required.";
			result = false;
		}
		else
		{
			string text2 = ResolveAddressBookUrl(serverUrl, username, password);
			if (text2.Length == 0)
			{
				result = false;
			}
			else
			{
				string requestUriString = BuildContactResourceUrl(text2, text);
				try
				{
					HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(requestUriString);
					httpWebRequest.Method = "DELETE";
					httpWebRequest.Credentials = new NetworkCredential(username, password);
					httpWebRequest.PreAuthenticate = true;
					httpWebRequest.Timeout = 45000;
					using HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
					int statusCode = (int)httpWebResponse.StatusCode;
					result = ((statusCode == 200 || statusCode == 204 || statusCode == 404) ? true : false);
				}
				catch (WebException ex)
				{
					ProjectData.SetProjectError(ex);
					WebException ex2 = ex;
					if (ex2.Response is HttpWebResponse { StatusCode: HttpStatusCode.NotFound })
					{
						result = true;
						ProjectData.ClearProjectError();
					}
					else
					{
						LastError = ex2.Message;
						result = false;
						ProjectData.ClearProjectError();
					}
				}
				catch (Exception ex3)
				{
					ProjectData.SetProjectError(ex3);
					Exception ex4 = ex3;
					LastError = ex4.Message;
					result = false;
					ProjectData.ClearProjectError();
				}
			}
		}
		return result;
	}

	private static string BuildContactResourceUrl(string bookUrl, string email)
	{
		string text = bookUrl;
		if (!text.EndsWith("/"))
		{
			text += "/";
		}
		return text + SanitizeToken(email) + ".vcf";
	}

	private static void ApplyRemoteContactWins(MailContact local, MailContact remote)
	{
		if (local != null && remote != null)
		{
			local.DisplayName = remote.DisplayName;
			local.Company = remote.Company;
			local.WorkPhone = remote.WorkPhone;
			local.HomePhone = remote.HomePhone;
			local.Notes = remote.Notes;
			local.SecondaryEmail = remote.SecondaryEmail;
			local.FirstName = remote.FirstName;
			local.LastName = remote.LastName;
			local.NickName = remote.NickName;
			local.MobilePhone = remote.MobilePhone;
			local.JobTitle = remote.JobTitle;
			local.Department = remote.Department;
		}
	}

	private static void MergeBlankContactField(ref string localVal, string remoteVal)
	{
		if (string.IsNullOrWhiteSpace(localVal) && !string.IsNullOrWhiteSpace(remoteVal))
		{
			localVal = remoteVal;
		}
	}

	private static void MergeContactForConflict(MailContact local, MailContact remote)
	{
		if (local != null && remote != null)
		{
			MergeBlankContactField(ref local.DisplayName, remote.DisplayName);
			MergeBlankContactField(ref local.Company, remote.Company);
			MergeBlankContactField(ref local.WorkPhone, remote.WorkPhone);
			MergeBlankContactField(ref local.HomePhone, remote.HomePhone);
			MergeBlankContactField(ref local.MobilePhone, remote.MobilePhone);
			MergeBlankContactField(ref local.FaxNumber, remote.FaxNumber);
			MergeBlankContactField(ref local.PagerNumber, remote.PagerNumber);
			MergeBlankContactField(ref local.OtherPhone, remote.OtherPhone);
			MergeBlankContactField(ref local.Notes, remote.Notes);
			MergeBlankContactField(ref local.SecondaryEmail, remote.SecondaryEmail);
			MergeBlankContactField(ref local.FirstName, remote.FirstName);
			MergeBlankContactField(ref local.LastName, remote.LastName);
			MergeBlankContactField(ref local.MiddleName, remote.MiddleName);
			MergeBlankContactField(ref local.NickName, remote.NickName);
			MergeBlankContactField(ref local.JobTitle, remote.JobTitle);
			MergeBlankContactField(ref local.Department, remote.Department);
			MergeBlankContactField(ref local.OfficeLocation, remote.OfficeLocation);
			MergeBlankContactField(ref local.WebPage, remote.WebPage);
			MergeBlankContactField(ref local.WorkAddress, remote.WorkAddress);
			MergeBlankContactField(ref local.HomeAddress, remote.HomeAddress);
			MergeBlankContactField(ref local.Custom1, remote.Custom1);
			MergeBlankContactField(ref local.Custom2, remote.Custom2);
			MergeBlankContactField(ref local.Custom3, remote.Custom3);
			MergeBlankContactField(ref local.Custom4, remote.Custom4);
			MergeBlankContactField(ref local.Birthday, remote.Birthday);
			MergeBlankContactField(ref local.Anniversary, remote.Anniversary);
			MergeBlankContactField(ref local.SpouseName, remote.SpouseName);
			MergeBlankContactField(ref local.AssistantName, remote.AssistantName);
			MergeBlankContactField(ref local.ScreenName, remote.ScreenName);
			MergeBlankContactField(ref local.AimScreenName, remote.AimScreenName);
			MergeBlankContactField(ref local.PreferEmail, remote.PreferEmail);
			MergeBlankContactField(ref local.RecordName, remote.RecordName);
			MergeBlankContactField(ref local.PopularityIndex, remote.PopularityIndex);
		}
	}

	private static bool TryGetContactResource(string resourceUrl, string username, string password, ref MailContact contact, ref string etag)
	{
		contact = null;
		etag = null;
		bool result;
		try
		{
			HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(resourceUrl);
			httpWebRequest.Method = "GET";
			httpWebRequest.Credentials = new NetworkCredential(username, password);
			httpWebRequest.PreAuthenticate = true;
			httpWebRequest.Timeout = 45000;
			using HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
			etag = httpWebResponse.Headers["ETag"];
			using StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8);
			string text = streamReader.ReadToEnd();
			List<MailContact> list = ParseVCardBlocks(text);
			if (list == null || list.Count == 0)
			{
				LastError = "Remote vCard empty.";
				result = false;
			}
			else
			{
				contact = list[0];
				result = true;
			}
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

	public static string ResolveAddressBookUrl(string serverUrl, string username, string password)
	{
		LastError = null;
		string text = ((serverUrl != null) ? serverUrl : "").Trim();
		if (text.Length == 0)
		{
			LastError = "CardDAV server URL is required.";
			return "";
		}
		if (text.IndexOf(".vcf", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return text;
		}
		string body = "<?xml version=\"1.0\" encoding=\"utf-8\"?><D:propfind xmlns:D=\"DAV:\" xmlns:C=\"urn:ietf:params:xml:ns:carddav\"><D:prop><D:resourcetype/><D:displayname/></D:prop></D:propfind>";
		string text2 = DavRequest(text, "PROPFIND", username, password, body, "1");
		if (text2.Length == 0)
		{
			return "";
		}
		List<string> list = ExtractHrefBlocks(text2);
		foreach (string item in list)
		{
			if (item.IndexOf("addressbook", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				string text3 = ExtractTagValue(item, "href");
				if (text3.Length > 0)
				{
					return CombineUrl(text, text3);
				}
			}
		}
		return text;
	}

	public static List<string> EnumerateAddressBookUrls(string serverUrl, string username, string password)
	{
		LastError = null;
		List<string> list = new List<string>();
		string text = ((serverUrl != null) ? serverUrl : "").Trim();
		if (text.Length == 0)
		{
			return list;
		}
		string body = "<?xml version=\"1.0\" encoding=\"utf-8\"?><D:propfind xmlns:D=\"DAV:\" xmlns:C=\"urn:ietf:params:xml:ns:carddav\"><D:prop><D:resourcetype/><D:displayname/></D:prop></D:propfind>";
		string text2 = DavRequest(text, "PROPFIND", username, password, body, "1");
		if (text2.Length == 0)
		{
			return list;
		}
		foreach (string item in ExtractHrefBlocks(text2))
		{
			if (item.IndexOf("addressbook", StringComparison.OrdinalIgnoreCase) < 0)
			{
				continue;
			}
			string text3 = ExtractTagValue(item, "href");
			if (text3.Length == 0)
			{
				continue;
			}
			string text4 = CombineUrl(text, text3);
			bool flag = false;
			foreach (string item2 in list)
			{
				if (string.Equals(item2, text4, StringComparison.OrdinalIgnoreCase))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				list.Add(text4);
			}
		}
		return list;
	}

	public static List<MailContact> ParseVCardContacts(string vcardPayload)
	{
		return ParseVCardBlocks(vcardPayload);
	}

	private static List<MailContact> ParseAddressDataResponses(string xml)
	{
		List<MailContact> list = new List<MailContact>();
		foreach (CardDavRemoteContact item in ParseRemoteContacts(xml))
		{
			if (item != null && item.Contact != null)
			{
				list.Add(item.Contact);
			}
		}
		return list;
	}

	private static List<CardDavRemoteContact> ParseRemoteContacts(string xml)
	{
		List<CardDavRemoteContact> list = new List<CardDavRemoteContact>();
		if (string.IsNullOrEmpty(xml))
		{
			return list;
		}
		foreach (string item in ExtractHrefBlocks(xml))
		{
			string text = ExtractTagValue(item, "D:getetag");
			if (text.Length == 0)
			{
				text = ExtractTagValue(item, "getetag");
			}
			List<string> list2 = ExtractAddressDataBlocks(item);
			foreach (string item2 in list2)
			{
				foreach (MailContact item3 in ParseVCardBlocks(item2))
				{
					if (item3 != null)
					{
						item3.CardDavEtag = text;
						list.Add(new CardDavRemoteContact
						{
							Contact = item3,
							Etag = text
						});
					}
				}
			}
		}
		return list;
	}

	private static List<MailContact> ParseVCardBlocks(string text)
	{
		List<MailContact> list = new List<MailContact>();
		if (string.IsNullOrWhiteSpace(text))
		{
			return list;
		}
		Regex regex = new Regex("BEGIN:VCARD[\\s\\S]*?END:VCARD", RegexOptions.IgnoreCase);
		foreach (Match item in regex.Matches(text))
		{
			string value = item.Value;
			List<string> list2 = ExtractVCardLines(value, "EMAIL");
			if (list2 == null || list2.Count == 0)
			{
				continue;
			}
			string text2 = PickPreferredVCardEmail(value, list2);
			if (text2.Length > 0)
			{
				list2.Remove(text2);
				list2.Insert(0, text2);
			}
			string text3 = ExtractVCardLine(value, "FN");
			if (text3.Length == 0)
			{
				text3 = ExtractVCardLine(value, "N").Replace(';', ' ').Trim();
			}
			string nickName = ExtractVCardLine(value, "NICKNAME");
			string text4 = ExtractVCardLine(value, "X-SCREENNAME");
			if (text4.Length == 0)
			{
				text4 = ExtractVCardLine(value, "X-TB-SCREENNAME");
			}
			if (text4.Length == 0)
			{
				text4 = ExtractVCardLine(value, "X-MOZILLA-SCREENNAME");
			}
			string text5 = ExtractVCardLine(value, "X-AIM");
			if (text5.Length == 0)
			{
				text5 = ExtractImppAimScreenName(value);
			}
			string text6 = ExtractVCardLine(value, "ORG");
			string company = text6;
			string department = "";
			if (text6.Length > 0)
			{
				string[] array = text6.Split(';');
				company = array[0].Trim();
				if (array.Length > 1)
				{
					department = array[1].Trim();
				}
			}
			string text7 = ExtractVCardLine(value, "TITLE");
			if (text7.Length == 0)
			{
				text7 = ExtractVCardLine(value, "ROLE");
			}
			string webPage = ExtractVCardLine(value, "URL");
			string text8 = ExtractVCardLine(value, "NOTE");
			string text9 = ExtractVCardLine(value, "CATEGORIES");
			string text10 = ExtractVCardLine(value, "X-CUSTOM1");
			if (text10.Length == 0 && text9.Length > 0)
			{
				text10 = text9;
			}
			string custom = ExtractVCardLine(value, "X-CUSTOM2");
			string text11 = ExtractVCardLine(value, "X-CUSTOM3");
			string custom2 = ExtractVCardLine(value, "X-CUSTOM4");
			string text12 = ExtractVCardLine(value, "LANG");
			if (text12.Length > 0 && text11.Length == 0)
			{
				text11 = text12;
			}
			string birthday = ExtractVCardLine(value, "BDAY");
			string anniversary = ExtractVCardLine(value, "ANNIVERSARY");
			string text13 = ExtractVCardLine(value, "REV");
			if (text13.Length > 0 && string.IsNullOrWhiteSpace(text8))
			{
				text8 = "REV " + text13;
			}
			string text14 = ExtractVCardLine(value, "X-SPOUSE");
			if (text14.Length == 0)
			{
				text14 = ExtractVCardLine(value, "RELATED;TYPE=SPOUSE");
			}
			string text15 = ExtractVCardLine(value, "X-ASSISTANT");
			if (text15.Length == 0)
			{
				text15 = ExtractVCardLine(value, "RELATED;TYPE=ASSISTANT");
			}
			string text16 = ExtractVCardLine(value, "X-OFFICE");
			if (text16.Length == 0)
			{
				text16 = ExtractVCardLine(value, "X-MOZILLA-OFFICE");
			}
			string text17 = ExtractVCardLine(value, "TEL;TYPE=OTHER");
			if (text17.Length == 0)
			{
				text17 = ExtractVCardLine(value, "TEL;TYPE=VOICE");
			}
			string text18 = PickPreferredVCardTel(value, "WORK");
			if (text18.Length == 0)
			{
				text18 = ExtractVCardLine(value, "TEL;TYPE=WORK");
			}
			if (text18.Length == 0)
			{
				text18 = ExtractVCardLine(value, "TEL");
			}
			string text19 = PickPreferredVCardTel(value, "HOME");
			if (text19.Length == 0)
			{
				text19 = ExtractVCardLine(value, "TEL;TYPE=HOME");
			}
			string text20 = PickPreferredVCardTel(value, "CELL");
			if (text20.Length == 0)
			{
				text20 = PickPreferredVCardTel(value, "MOBILE");
			}
			if (text20.Length == 0)
			{
				text20 = PickPreferredVCardTel(value, "IPHONE");
			}
			if (text20.Length == 0)
			{
				text20 = ExtractVCardLine(value, "TEL;TYPE=CELL");
			}
			if (text20.Length == 0)
			{
				text20 = ExtractVCardLine(value, "TEL;TYPE=MOBILE");
			}
			if (text20.Length == 0)
			{
				text20 = ExtractVCardLine(value, "TEL;TYPE=IPHONE");
			}
			string text21 = PickPreferredVCardTel(value, "FAX");
			if (text21.Length == 0)
			{
				text21 = ExtractVCardLine(value, "TEL;TYPE=FAX");
			}
			string text22 = PickPreferredVCardTel(value, "PAGER");
			if (text22.Length == 0)
			{
				text22 = ExtractVCardLine(value, "TEL;TYPE=PAGER");
			}
			string text23 = ExtractVCardLine(value, "ADR;TYPE=WORK");
			string rawAdr = ExtractVCardLine(value, "ADR;TYPE=HOME");
			if (text23.Length == 0)
			{
				text23 = ExtractVCardLine(value, "ADR");
			}
			string text24 = ExtractVCardLine(value, "N");
			string firstName = "";
			string lastName = "";
			string middleName = "";
			if (text24.Length > 0)
			{
				string[] array2 = text24.Split(';');
				if (array2.Length > 1)
				{
					lastName = array2[0].Trim();
				}
				if (array2.Length > 2)
				{
					firstName = array2[1].Trim();
				}
				if (array2.Length > 3)
				{
					middleName = array2[2].Trim();
				}
			}
			MailContact mailContact = new MailContact();
			mailContact.Email = list2[0];
			mailContact.DisplayName = text3;
			mailContact.NickName = nickName;
			mailContact.Company = company;
			mailContact.Department = department;
			mailContact.JobTitle = text7;
			mailContact.Notes = text8;
			mailContact.WorkPhone = text18;
			mailContact.HomePhone = text19;
			mailContact.MobilePhone = text20;
			mailContact.FaxNumber = text21;
			mailContact.PagerNumber = text22;
			mailContact.WorkAddress = FormatVCardAdr(text23);
			mailContact.HomeAddress = FormatVCardAdr(rawAdr);
			mailContact.WebPage = webPage;
			mailContact.FirstName = firstName;
			mailContact.LastName = lastName;
			mailContact.Custom1 = text10;
			mailContact.Custom2 = custom;
			mailContact.Custom3 = text11;
			mailContact.Custom4 = custom2;
			mailContact.Birthday = birthday;
			mailContact.OtherPhone = text17;
			mailContact.Anniversary = anniversary;
			mailContact.SpouseName = text14;
			mailContact.MiddleName = middleName;
			mailContact.ScreenName = text4;
			mailContact.AimScreenName = text5;
			mailContact.AssistantName = text15;
			mailContact.OfficeLocation = text16;
			MailContact mailContact2 = mailContact;
			if (list2.Count > 1)
			{
				mailContact2.SecondaryEmail = list2[1];
			}
			list.Add(mailContact2);
		}
		if (list.Count == 0)
		{
			Regex regex2 = new Regex("[A-Za-z0-9._%+\\-]+@[A-Za-z0-9.\\-]+\\.[A-Za-z]{2,}", RegexOptions.Compiled);
			foreach (Match item2 in regex2.Matches(text))
			{
				list.Add(new MailContact
				{
					Email = item2.Value,
					DisplayName = ""
				});
			}
		}
		return list;
	}

	private static string PickPreferredVCardTel(string card, string typeToken)
	{
		if (string.IsNullOrWhiteSpace(card) || string.IsNullOrWhiteSpace(typeToken))
		{
			return "";
		}
		string[] array = card.Split(new char[2] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
		foreach (string text in array)
		{
			string text2 = text.Trim();
			if (!text2.StartsWith("TEL", StringComparison.OrdinalIgnoreCase) || text2.IndexOf("PREF", StringComparison.OrdinalIgnoreCase) < 0 || text2.IndexOf(typeToken, StringComparison.OrdinalIgnoreCase) < 0)
			{
				continue;
			}
			int num = text2.LastIndexOf(':');
			if (num >= 0)
			{
				string text3 = text2.Substring(checked(num + 1)).Trim();
				if (text3.Length > 0)
				{
					return text3;
				}
			}
		}
		return "";
	}

	private static string PickPreferredVCardEmail(string card, List<string> emails)
	{
		if (emails == null || emails.Count == 0)
		{
			return "";
		}
		string[] array = card.Split(new char[2] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
		foreach (string text in array)
		{
			string text2 = text.Trim();
			if (!text2.StartsWith("EMAIL", StringComparison.OrdinalIgnoreCase) || text2.IndexOf("PREF", StringComparison.OrdinalIgnoreCase) < 0)
			{
				continue;
			}
			int num = text2.LastIndexOf(':');
			if (num >= 0)
			{
				string text3 = text2.Substring(checked(num + 1)).Trim();
				if (text3.Length > 0)
				{
					return text3;
				}
			}
		}
		return "";
	}

	private static string ExtractImppAimScreenName(string card)
	{
		if (string.IsNullOrWhiteSpace(card))
		{
			return "";
		}
		string[] array = card.Split(new char[2] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
		foreach (string text in array)
		{
			string text2 = text.Trim();
			if (text2.IndexOf("IMPP", StringComparison.OrdinalIgnoreCase) < 0 || (text2.IndexOf("AIM", StringComparison.OrdinalIgnoreCase) < 0 && text2.IndexOf("aim:", StringComparison.OrdinalIgnoreCase) < 0))
			{
				continue;
			}
			int num = text2.LastIndexOf(':');
			if (num >= 0)
			{
				string text3 = text2.Substring(checked(num + 1)).Trim();
				if (text3.StartsWith("aim:", StringComparison.OrdinalIgnoreCase))
				{
					text3 = text3.Substring(4).Trim();
				}
				if (text3.Length > 0)
				{
					return text3;
				}
			}
		}
		return "";
	}

	private static string ExtractVCardLine(string card, string key)
	{
		string[] array = card.Split(new char[2] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
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

	private static List<string> ExtractVCardLines(string card, string key)
	{
		List<string> list = new List<string>();
		string[] array = card.Split(new char[2] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
		foreach (string text in array)
		{
			string text2 = text.Trim();
			if (!text2.StartsWith(key, StringComparison.OrdinalIgnoreCase) || (!text2.StartsWith(key + ":", StringComparison.OrdinalIgnoreCase) && !text2.StartsWith(key + ";", StringComparison.OrdinalIgnoreCase)))
			{
				continue;
			}
			int num = text2.IndexOf(':');
			if (num >= 0)
			{
				string text3 = text2.Substring(checked(num + 1)).Trim();
				if (text3.Length > 0)
				{
					list.Add(text3);
				}
			}
		}
		return list;
	}

	private static string BuildVCard(MailContact contact)
	{
		string text = contact.Email.Trim();
		MailContact mailContact = contact;
		string text2 = ((mailContact.DisplayName != null) ? mailContact.DisplayName : "").Trim();
		if (text2.Length == 0)
		{
			text2 = text;
		}
		mailContact = contact;
		string text3 = ((mailContact.FirstName != null) ? mailContact.FirstName : "").Trim();
		mailContact = contact;
		string text4 = ((mailContact.LastName != null) ? mailContact.LastName : "").Trim();
		if (text3.Length == 0 && text4.Length == 0 && text2.Length > 0)
		{
			int num = text2.IndexOf(' ');
			if (num > 0)
			{
				text3 = text2.Substring(0, num).Trim();
				text4 = text2.Substring(checked(num + 1)).Trim();
			}
			else
			{
				text4 = text2;
			}
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("BEGIN:VCARD");
		stringBuilder.AppendLine("PRODID:-//LCARS Mail//CardDAV//EN");
		stringBuilder.AppendLine("VERSION:3.0");
		stringBuilder.AppendLine("FN:" + EscapeVCard(text2));
		mailContact = contact;
		string value = ((mailContact.MiddleName != null) ? mailContact.MiddleName : "").Trim();
		string[] array = new string[8]
		{
			"N:",
			EscapeVCard(text4),
			";",
			EscapeVCard(text3),
			";",
			EscapeVCard(value),
			";;",
			null
		};
		mailContact = contact;
		array[7] = EscapeVCard((mailContact.NickName != null) ? mailContact.NickName : "");
		stringBuilder.AppendLine(string.Concat(array));
		stringBuilder.AppendLine("EMAIL;TYPE=INTERNET,PREF:" + text);
		if (!string.IsNullOrWhiteSpace(contact.SecondaryEmail))
		{
			stringBuilder.AppendLine("EMAIL;TYPE=INTERNET:" + contact.SecondaryEmail.Trim());
		}
		if (!string.IsNullOrWhiteSpace(contact.NickName))
		{
			stringBuilder.AppendLine("NICKNAME:" + EscapeVCard(contact.NickName));
		}
		if (!string.IsNullOrWhiteSpace(contact.ScreenName))
		{
			stringBuilder.AppendLine("X-SCREENNAME:" + EscapeVCard(contact.ScreenName));
			stringBuilder.AppendLine("X-MOZILLA-SCREENNAME:" + EscapeVCard(contact.ScreenName));
		}
		if (!string.IsNullOrWhiteSpace(contact.AimScreenName))
		{
			stringBuilder.AppendLine("X-AIM:" + EscapeVCard(contact.AimScreenName));
			stringBuilder.AppendLine("IMPP;TYPE=AIM:" + EscapeVCard("aim:" + contact.AimScreenName.Trim()));
		}
		if (!string.IsNullOrWhiteSpace(contact.Company))
		{
			string text5 = EscapeVCard(contact.Company);
			if (!string.IsNullOrWhiteSpace(contact.Department))
			{
				text5 = text5 + ";" + EscapeVCard(contact.Department);
			}
			stringBuilder.AppendLine("ORG:" + text5);
		}
		else if (!string.IsNullOrWhiteSpace(contact.Department))
		{
			stringBuilder.AppendLine("ORG:" + EscapeVCard(contact.Department));
		}
		if (!string.IsNullOrWhiteSpace(contact.JobTitle))
		{
			stringBuilder.AppendLine("TITLE:" + EscapeVCard(contact.JobTitle));
			stringBuilder.AppendLine("ROLE:" + EscapeVCard(contact.JobTitle));
		}
		bool flag = !string.IsNullOrWhiteSpace(contact.MobilePhone);
		bool flag2 = !string.IsNullOrWhiteSpace(contact.WorkPhone);
		bool flag3 = !string.IsNullOrWhiteSpace(contact.HomePhone);
		bool flag4 = ((flag || flag2 || flag3) ? true : false);
		if (flag2)
		{
			string text6 = (flag ? "WORK" : "WORK,PREF");
			stringBuilder.AppendLine("TEL;TYPE=" + text6 + ":" + EscapeVCard(contact.WorkPhone));
		}
		if (flag3)
		{
			string text7 = ((flag || flag2) ? "HOME" : "HOME,PREF");
			stringBuilder.AppendLine("TEL;TYPE=" + text7 + ":" + EscapeVCard(contact.HomePhone));
		}
		if (flag)
		{
			stringBuilder.AppendLine("TEL;TYPE=CELL,PREF:" + EscapeVCard(contact.MobilePhone));
			stringBuilder.AppendLine("TEL;TYPE=IPHONE:" + EscapeVCard(contact.MobilePhone));
		}
		if (!string.IsNullOrWhiteSpace(contact.FaxNumber))
		{
			string text8 = (flag4 ? "FAX" : "FAX,PREF");
			stringBuilder.AppendLine("TEL;TYPE=" + text8 + ":" + EscapeVCard(contact.FaxNumber));
		}
		if (!string.IsNullOrWhiteSpace(contact.PagerNumber))
		{
			string text9 = ((flag4 || !string.IsNullOrWhiteSpace(contact.FaxNumber)) ? "PAGER" : "PAGER,PREF");
			stringBuilder.AppendLine("TEL;TYPE=" + text9 + ":" + EscapeVCard(contact.PagerNumber));
		}
		bool flag5 = !string.IsNullOrWhiteSpace(contact.WorkAddress);
		bool flag6 = !string.IsNullOrWhiteSpace(contact.HomeAddress);
		if (flag5)
		{
			string text10 = (flag6 ? "WORK" : "WORK,PREF");
			stringBuilder.AppendLine("ADR;TYPE=" + text10 + ":" + BuildVCardAdrValue(contact.WorkAddress));
		}
		if (flag6)
		{
			string text11 = (flag5 ? "HOME" : "HOME,PREF");
			stringBuilder.AppendLine("ADR;TYPE=" + text11 + ":" + BuildVCardAdrValue(contact.HomeAddress));
		}
		if (!string.IsNullOrWhiteSpace(contact.WebPage))
		{
			stringBuilder.AppendLine("URL:" + EscapeVCard(contact.WebPage));
		}
		if (!string.IsNullOrWhiteSpace(contact.Notes))
		{
			stringBuilder.AppendLine("NOTE:" + EscapeVCard(contact.Notes));
		}
		if (!string.IsNullOrWhiteSpace(contact.Custom1))
		{
			stringBuilder.AppendLine("X-CUSTOM1:" + EscapeVCard(contact.Custom1));
			stringBuilder.AppendLine("CATEGORIES:" + EscapeVCard(contact.Custom1));
		}
		if (!string.IsNullOrWhiteSpace(contact.Custom2))
		{
			stringBuilder.AppendLine("X-CUSTOM2:" + EscapeVCard(contact.Custom2));
		}
		if (!string.IsNullOrWhiteSpace(contact.Custom3))
		{
			stringBuilder.AppendLine("X-CUSTOM3:" + EscapeVCard(contact.Custom3));
		}
		if (!string.IsNullOrWhiteSpace(contact.Custom4))
		{
			stringBuilder.AppendLine("X-CUSTOM4:" + EscapeVCard(contact.Custom4));
		}
		if (!string.IsNullOrWhiteSpace(contact.Birthday))
		{
			stringBuilder.AppendLine("BDAY:" + EscapeVCard(contact.Birthday));
		}
		if (!string.IsNullOrWhiteSpace(contact.Anniversary))
		{
			stringBuilder.AppendLine("ANNIVERSARY:" + EscapeVCard(contact.Anniversary));
		}
		if (!string.IsNullOrWhiteSpace(contact.SpouseName))
		{
			stringBuilder.AppendLine("X-SPOUSE:" + EscapeVCard(contact.SpouseName));
		}
		if (!string.IsNullOrWhiteSpace(contact.AssistantName))
		{
			stringBuilder.AppendLine("X-ASSISTANT:" + EscapeVCard(contact.AssistantName));
			stringBuilder.AppendLine("RELATED;TYPE=ASSISTANT:" + EscapeVCard(contact.AssistantName));
		}
		if (!string.IsNullOrWhiteSpace(contact.OfficeLocation))
		{
			stringBuilder.AppendLine("X-OFFICE:" + EscapeVCard(contact.OfficeLocation));
			stringBuilder.AppendLine("X-MOZILLA-OFFICE:" + EscapeVCard(contact.OfficeLocation));
		}
		if (!string.IsNullOrWhiteSpace(contact.OtherPhone))
		{
			string text12 = ((flag4 || !string.IsNullOrWhiteSpace(contact.FaxNumber) || !string.IsNullOrWhiteSpace(contact.PagerNumber)) ? "OTHER" : "OTHER,PREF");
			stringBuilder.AppendLine("TEL;TYPE=" + text12 + ":" + EscapeVCard(contact.OtherPhone));
		}
		mailContact = contact;
		string text13 = ((mailContact.Custom3 != null) ? mailContact.Custom3 : "").Trim();
		if (text13.Length > 0 && text13.Length <= 16 && !text13.Contains("@"))
		{
			stringBuilder.AppendLine("LANG:" + EscapeVCard(text13));
		}
		else
		{
			stringBuilder.AppendLine("LANG:en");
		}
		stringBuilder.AppendLine("UID:" + text);
		stringBuilder.AppendLine("REV:" + DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture));
		stringBuilder.AppendLine("END:VCARD");
		return stringBuilder.ToString();
	}

	private static string FormatVCardAdr(string rawAdr)
	{
		string text = ((rawAdr != null) ? rawAdr : "").Trim();
		if (text.Length == 0)
		{
			return "";
		}
		string[] array = text.Split(';');
		string text2 = "";
		if (array.Length > 2)
		{
			text2 = array[2].Trim();
		}
		if (text2.Length == 0 && array.Length > 0)
		{
			text2 = array[checked(array.Length - 1)].Trim();
		}
		string text3 = "";
		if (array.Length > 3)
		{
			text3 = array[3].Trim();
		}
		string text4 = "";
		if (array.Length > 4)
		{
			text4 = array[4].Trim();
		}
		string text5 = "";
		if (array.Length > 5)
		{
			text5 = array[5].Trim();
		}
		string text6 = "";
		if (array.Length > 6)
		{
			text6 = array[6].Trim();
		}
		StringBuilder stringBuilder = new StringBuilder();
		if (text2.Length > 0)
		{
			stringBuilder.Append(text2);
		}
		if (text3.Length > 0)
		{
			if (stringBuilder.Length > 0)
			{
				stringBuilder.Append(", ");
			}
			stringBuilder.Append(text3);
		}
		if (text4.Length > 0)
		{
			if (stringBuilder.Length > 0)
			{
				stringBuilder.Append(", ");
			}
			stringBuilder.Append(text4);
		}
		if (text5.Length > 0)
		{
			if (stringBuilder.Length > 0)
			{
				stringBuilder.Append(" ");
			}
			stringBuilder.Append(text5);
		}
		if (text6.Length > 0)
		{
			if (stringBuilder.Length > 0)
			{
				stringBuilder.Append(", ");
			}
			stringBuilder.Append(text6);
		}
		if (stringBuilder.Length > 0)
		{
			return stringBuilder.ToString();
		}
		return text2;
	}

	private static string BuildVCardAdrValue(string singleLineAddress)
	{
		string text = ((singleLineAddress != null) ? singleLineAddress : "").Trim();
		if (text.Length == 0)
		{
			return ";;;;;;";
		}
		return ";;" + EscapeVCard(text) + ";;;;";
	}

	private static string EscapeVCard(string value)
	{
		return ((value != null) ? value : "").Replace(";", "\\;").Replace(",", "\\,").Replace("\r\n", "\\n");
	}

	private static string DavRequest(string url, string method, string username, string password, string body, string depth)
	{
		string result;
		try
		{
			HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
			httpWebRequest.Method = method;
			httpWebRequest.Credentials = new NetworkCredential(username, password);
			httpWebRequest.PreAuthenticate = true;
			httpWebRequest.Timeout = 45000;
			httpWebRequest.ContentType = "application/xml; charset=utf-8";
			if (depth != null)
			{
				httpWebRequest.Headers.Add("Depth", depth);
			}
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

	private static bool DavPutStatus(string url, string username, string password, string vcard, string ifMatchEtag, ref int httpStatus, ref string responseEtag)
	{
		httpStatus = 0;
		responseEtag = null;
		bool result;
		try
		{
			HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
			httpWebRequest.Method = "PUT";
			httpWebRequest.Credentials = new NetworkCredential(username, password);
			httpWebRequest.PreAuthenticate = true;
			httpWebRequest.Timeout = 45000;
			httpWebRequest.ContentType = "text/vcard; charset=utf-8";
			string text = ((ifMatchEtag != null) ? ifMatchEtag : "").Trim();
			if (text.Length > 0)
			{
				httpWebRequest.Headers.Add("If-Match", text);
			}
			else
			{
				httpWebRequest.Headers.Add("If-None-Match", "*");
			}
			byte[] bytes = Encoding.UTF8.GetBytes(vcard);
			httpWebRequest.ContentLength = bytes.Length;
			using (Stream stream = httpWebRequest.GetRequestStream())
			{
				stream.Write(bytes, 0, bytes.Length);
			}
			using HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
			httpStatus = (int)httpWebResponse.StatusCode;
			responseEtag = httpWebResponse.Headers["ETag"];
			result = ((httpStatus >= 200 && httpStatus < 300) ? true : false);
		}
		catch (WebException ex)
		{
			ProjectData.SetProjectError(ex);
			WebException ex2 = ex;
			if (!(ex2.Response is HttpWebResponse httpWebResponse2))
			{
				goto IL_01af;
			}
			httpStatus = (int)httpWebResponse2.StatusCode;
			responseEtag = httpWebResponse2.Headers["ETag"];
			if (httpStatus == 201 || httpStatus == 204)
			{
				result = true;
				ProjectData.ClearProjectError();
			}
			else
			{
				if (httpStatus != 412)
				{
					goto IL_01af;
				}
				LastError = "CardDAV etag conflict (412).";
				result = false;
				ProjectData.ClearProjectError();
			}
			goto end_IL_013b;
			IL_01af:
			LastError = ex2.Message;
			result = false;
			ProjectData.ClearProjectError();
			end_IL_013b:;
		}
		catch (Exception ex3)
		{
			ProjectData.SetProjectError(ex3);
			Exception ex4 = ex3;
			LastError = ex4.Message;
			result = false;
			ProjectData.ClearProjectError();
		}
		return result;
	}

	private static bool DavPut(string url, string username, string password, string vcard, string ifMatchEtag = null)
	{
		int httpStatus = 0;
		string responseEtag = null;
		return DavPutStatus(url, username, password, vcard, (ifMatchEtag != null) ? ifMatchEtag : "", ref httpStatus, ref responseEtag);
	}

	private static List<string> ExtractAddressDataBlocks(string xml)
	{
		List<string> list = new List<string>();
		if (string.IsNullOrEmpty(xml))
		{
			return list;
		}
		string text = "address-data";
		int num = 0;
		checked
		{
			while (num < xml.Length)
			{
				int num2 = xml.IndexOf("<", num, StringComparison.Ordinal);
				if (num2 < 0)
				{
					break;
				}
				int num3 = xml.IndexOf('>', num2);
				if (num3 < 0)
				{
					break;
				}
				string text2 = xml.Substring(num2, num3 - num2 + 1);
				if (text2.IndexOf(text, StringComparison.OrdinalIgnoreCase) < 0)
				{
					num = num3 + 1;
					continue;
				}
				string text3 = "</" + text + ">";
				int num4 = xml.IndexOf(text3, num3, StringComparison.OrdinalIgnoreCase);
				if (num4 < 0)
				{
					break;
				}
				string text4 = xml.Substring(num3 + 1, num4 - num3 - 1);
				list.Add(DecodeXmlText(text4.Trim()));
				num = num4 + text3.Length;
			}
			return list;
		}
	}

	private static List<string> ExtractHrefBlocks(string xml)
	{
		List<string> list = new List<string>();
		string[] array = xml.Split(new string[2] { "<D:response", "<d:response" }, StringSplitOptions.None);
		checked
		{
			int num = array.Length - 1;
			for (int i = 1; i <= num; i++)
			{
				list.Add(array[i]);
			}
			return list;
		}
	}

	private static string ExtractTagValue(string block, string tag)
	{
		string text = "<" + tag + ">";
		string value = "</" + tag + ">";
		int num = block.IndexOf(text, StringComparison.OrdinalIgnoreCase);
		if (num < 0)
		{
			return "";
		}
		checked
		{
			num += text.Length;
			int num2 = block.IndexOf(value, num, StringComparison.OrdinalIgnoreCase);
			if (num2 < 0)
			{
				return "";
			}
			return block.Substring(num, num2 - num).Trim();
		}
	}

	private static string CombineUrl(string baseUrl, string href)
	{
		if (href.StartsWith("http", StringComparison.OrdinalIgnoreCase))
		{
			return href;
		}
		Uri baseUri = new Uri(baseUrl);
		return new Uri(baseUri, href).ToString();
	}

	private static string DecodeXmlText(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return "";
		}
		return value.Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&")
			.Replace("&quot;", "\"");
	}

	private static string SanitizeToken(string email)
	{
		string input = email.Replace("@", "_at_").Replace(".", "_");
		return Regex.Replace(input, "[^A-Za-z0-9_\\-]", "_");
	}
}
