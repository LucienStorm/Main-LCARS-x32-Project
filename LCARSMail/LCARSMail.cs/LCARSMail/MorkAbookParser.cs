using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

public sealed class MorkAbookParser
{
	public static string LastError;

	public static bool LastSourceMabRoundTripValidated = false;

	public static bool LastSourceMabRoundTripByteIdentical = false;

	public static bool LastMabExportByteIdentical = false;

	public static bool LastMabExportVerifiedAgainstSource = false;

	public static bool LastMabExportEmailSetMatches = false;

	public static bool LastMabExportFieldsMatch = false;

	public static long LastMabExportSourceByteLength = 0L;

	public static long LastMabExportDestByteLength = 0L;

	public static int LastMabExportHeaderPrefixMatchBytes = 0;

	private const int ExportHeaderPrefixCompareBytes = 512;

	public static string LastValidatedSourceMabPath = "";

	public static int LastMorkColumnKeyCount = 0;

	public static int LastMorkRowPropertyFieldCount = 0;

	public static int LastMorkTableCount = 0;

	public static int LastMorkParsedContactCount = 0;

	public static int LastMorkRowMapCount = 0;

	public static bool LastMorkPreserveExportFieldValidated = false;

	private static readonly Regex EmailPattern = new Regex("[A-Za-z0-9._%+\\-]+@[A-Za-z0-9.\\-]+\\.[A-Za-z]{2,}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

	public static int TryMeasureExportHeaderPrefixMatch(string sourceMabPath, string exportedMabPath)
	{
		LastMabExportHeaderPrefixMatchBytes = 0;
		checked
		{
			int result;
			try
			{
				if (string.IsNullOrWhiteSpace(sourceMabPath) || !File.Exists(sourceMabPath))
				{
					result = 0;
				}
				else if (string.IsNullOrWhiteSpace(exportedMabPath) || !File.Exists(exportedMabPath))
				{
					result = 0;
				}
				else
				{
					byte[] array = File.ReadAllBytes(sourceMabPath);
					byte[] array2 = File.ReadAllBytes(exportedMabPath);
					int num = Math.Min(512, Math.Min(array.Length, array2.Length));
					int num2 = 0;
					int num3 = num - 1;
					for (int i = 0; i <= num3 && array[i] == array2[i]; i++)
					{
						num2++;
					}
					LastMabExportHeaderPrefixMatchBytes = num2;
					result = num2;
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
	}

	public static void RecordExportByteSizes(string sourceMabPath, string exportedMabPath)
	{
		LastMabExportSourceByteLength = 0L;
		LastMabExportDestByteLength = 0L;
		try
		{
			if (!string.IsNullOrWhiteSpace(sourceMabPath) && File.Exists(sourceMabPath))
			{
				LastMabExportSourceByteLength = new FileInfo(sourceMabPath).Length;
			}
			if (!string.IsNullOrWhiteSpace(exportedMabPath) && File.Exists(exportedMabPath))
			{
				LastMabExportDestByteLength = new FileInfo(exportedMabPath).Length;
			}
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
	}

	private MorkAbookParser()
	{
	}

	public static List<MailContact> TryParseContacts(string mabPath)
	{
		LastError = null;
		List<MailContact> result;
		if (string.IsNullOrWhiteSpace(mabPath) || !File.Exists(mabPath))
		{
			LastError = "abook.mab not found.";
			result = new List<MailContact>();
		}
		else
		{
			try
			{
				byte[] array = File.ReadAllBytes(mabPath);
				Encoding encoding = DetectMorkFileEncoding(array);
				result = ExtractContacts(encoding.GetString(array));
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Exception ex2 = ex;
				LastError = ex2.Message;
				result = new List<MailContact>();
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}

	internal static List<MailContact> ExtractContacts(string morkText)
	{
		Dictionary<string, MailContact> dictionary = new Dictionary<string, MailContact>(StringComparer.OrdinalIgnoreCase);
		if (string.IsNullOrEmpty(morkText))
		{
			return new List<MailContact>();
		}
		Dictionary<string, string> dictionary2 = ExtractPropertyMap(morkText, "DisplayName");
		Dictionary<string, string> dictionary3 = ExtractPropertyMap(morkText, "FirstName");
		Dictionary<string, string> dictionary4 = ExtractPropertyMap(morkText, "LastName");
		Dictionary<string, string> dictionary5 = ExtractPropertyMap(morkText, "NickName");
		Dictionary<string, string> dictionary6 = ExtractPropertyMap(morkText, "JobTitle");
		Dictionary<string, string> dictionary7 = ExtractPropertyMap(morkText, "Department");
		Dictionary<string, string> dictionary8 = ExtractPropertyMap(morkText, "WebPage1");
		Dictionary<string, string> dictionary9 = ExtractPropertyMap(morkText, "PrimaryEmail");
		Dictionary<string, string> dictionary10 = ExtractPropertyMap(morkText, "SecondEmail");
		Dictionary<string, string> dictionary11 = ExtractPropertyMap(morkText, "Company");
		Dictionary<string, string> dictionary12 = ExtractPropertyMap(morkText, "WorkPhone");
		Dictionary<string, string> dictionary13 = ExtractPropertyMap(morkText, "CellularNumber");
		Dictionary<string, string> dictionary14 = ExtractPropertyMap(morkText, "HomePhone");
		Dictionary<string, string> dictionary15 = ExtractPropertyMap(morkText, "FaxNumber");
		Dictionary<string, string> dictionary16 = ExtractPropertyMap(morkText, "PagerNumber");
		Dictionary<string, string> dictionary17 = ExtractPropertyMap(morkText, "Custom1");
		Dictionary<string, string> dictionary18 = ExtractPropertyMap(morkText, "Custom2");
		Dictionary<string, string> dictionary19 = ExtractPropertyMap(morkText, "Custom3");
		Dictionary<string, string> dictionary20 = ExtractPropertyMap(morkText, "Custom4");
		Dictionary<string, string> dictionary21 = ExtractPropertyMap(morkText, "SpouseName");
		Dictionary<string, string> dictionary22 = ExtractPropertyMap(morkText, "Notes");
		Dictionary<string, string> dictionary23 = ExtractPropertyMap(morkText, "ScreenName");
		Dictionary<string, string> dictionary24 = ExtractPropertyMap(morkText, "AimScreenName");
		Dictionary<string, string> dictionary25 = ExtractPropertyMap(morkText, "PopularityIndex");
		Dictionary<string, string> dictionary26 = ExtractPropertyMap(morkText, "RecordName");
		Dictionary<string, string> dictionary27 = ExtractPropertyMap(morkText, "PreferEmail");
		Dictionary<string, string> dictionary28 = ExtractPropertyMap(morkText, "LowercasePrimaryEmail");
		Dictionary<string, string> dictionary29 = ExtractPropertyMap(morkText, "WorkAddress");
		Dictionary<string, string> dictionary30 = ExtractPropertyMap(morkText, "HomeAddress");
		Dictionary<string, string> dictionary31 = ExtractPropertyMap(morkText, "BirthDay");
		MergePropertyMaps(dictionary31, ExtractPropertyMap(morkText, "Birthday"));
		Dictionary<string, string> dictionary32 = ExtractPropertyMap(morkText, "Anniversary");
		Dictionary<string, string> dictionary33 = ExtractPropertyMap(morkText, "MiddleName");
		Dictionary<string, string> dictionary34 = ExtractPropertyMap(morkText, "OfficeLocation");
		Dictionary<string, string> dictionary35 = ExtractPropertyMap(morkText, "OtherPhone");
		Dictionary<string, string> dictionary36 = ExtractPropertyMap(morkText, "AssistantName");
		Dictionary<string, string> dictionary37 = ExtractColumnKeyMap(morkText);
		LastMorkColumnKeyCount = dictionary37?.Count ?? 0;
		Dictionary<string, Dictionary<string, string>> dictionary38 = BuildRowPropertyMaps(morkText, dictionary37);
		LastMorkRowPropertyFieldCount = 0;
		if (dictionary38 != null && dictionary38.Count > 0)
		{
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (KeyValuePair<string, Dictionary<string, string>> item in dictionary38)
			{
				if (item.Value == null)
				{
					continue;
				}
				foreach (string key3 in item.Value.Keys)
				{
					hashSet.Add(key3);
				}
			}
			LastMorkRowPropertyFieldCount = hashSet.Count;
		}
		LastMorkRowMapCount = dictionary38?.Count ?? 0;
		MergeMapsFromRowProps(dictionary38, "PrimaryEmail", dictionary9);
		MergeMapsFromRowProps(dictionary38, "SecondEmail", dictionary10);
		MergeMapsFromRowProps(dictionary38, "DisplayName", dictionary2);
		MergeMapsFromRowProps(dictionary38, "FirstName", dictionary3);
		MergeMapsFromRowProps(dictionary38, "LastName", dictionary4);
		MergeMapsFromRowProps(dictionary38, "NickName", dictionary5);
		MergeMapsFromRowProps(dictionary38, "JobTitle", dictionary6);
		MergeMapsFromRowProps(dictionary38, "Department", dictionary7);
		MergeMapsFromRowProps(dictionary38, "WebPage1", dictionary8);
		MergeMapsFromRowProps(dictionary38, "Company", dictionary11);
		MergeMapsFromRowProps(dictionary38, "WorkPhone", dictionary12);
		MergeMapsFromRowProps(dictionary38, "CellularNumber", dictionary13);
		MergeMapsFromRowProps(dictionary38, "HomePhone", dictionary14);
		MergeMapsFromRowProps(dictionary38, "FaxNumber", dictionary15);
		MergeMapsFromRowProps(dictionary38, "PagerNumber", dictionary16);
		MergeMapsFromRowProps(dictionary38, "Custom1", dictionary17);
		MergeMapsFromRowProps(dictionary38, "Custom2", dictionary18);
		MergeMapsFromRowProps(dictionary38, "Custom3", dictionary19);
		MergeMapsFromRowProps(dictionary38, "Custom4", dictionary20);
		MergeMapsFromRowProps(dictionary38, "SpouseName", dictionary21);
		MergeMapsFromRowProps(dictionary38, "Notes", dictionary22);
		MergeMapsFromRowProps(dictionary38, "ScreenName", dictionary23);
		MergeMapsFromRowProps(dictionary38, "AimScreenName", dictionary24);
		MergeMapsFromRowProps(dictionary38, "PopularityIndex", dictionary25);
		MergeMapsFromRowProps(dictionary38, "RecordName", dictionary26);
		MergeMapsFromRowProps(dictionary38, "PreferEmail", dictionary27);
		MergeMapsFromRowProps(dictionary38, "LowercasePrimaryEmail", dictionary28);
		MergeMapsFromRowProps(dictionary38, "WorkAddress", dictionary29);
		MergeMapsFromRowProps(dictionary38, "HomeAddress", dictionary30);
		MergeMapsFromRowProps(dictionary38, "BirthDay", dictionary31);
		MergeMapsFromRowProps(dictionary38, "Anniversary", dictionary32);
		MergeMapsFromRowProps(dictionary38, "MiddleName", dictionary33);
		MergeMapsFromRowProps(dictionary38, "OfficeLocation", dictionary34);
		MergeMapsFromRowProps(dictionary38, "OtherPhone", dictionary35);
		MergeMapsFromRowProps(dictionary38, "AssistantName", dictionary36);
		if (dictionary37.Count > 0)
		{
			MergePropertyMaps(dictionary9, ExtractPropertyMapViaColumns(morkText, dictionary37, "PrimaryEmail"));
			MergePropertyMaps(dictionary10, ExtractPropertyMapViaColumns(morkText, dictionary37, "SecondEmail"));
			MergePropertyMaps(dictionary2, ExtractPropertyMapViaColumns(morkText, dictionary37, "DisplayName"));
			MergePropertyMaps(dictionary3, ExtractPropertyMapViaColumns(morkText, dictionary37, "FirstName"));
			MergePropertyMaps(dictionary4, ExtractPropertyMapViaColumns(morkText, dictionary37, "LastName"));
			MergePropertyMaps(dictionary5, ExtractPropertyMapViaColumns(morkText, dictionary37, "NickName"));
			MergePropertyMaps(dictionary6, ExtractPropertyMapViaColumns(morkText, dictionary37, "JobTitle"));
			MergePropertyMaps(dictionary7, ExtractPropertyMapViaColumns(morkText, dictionary37, "Department"));
			MergePropertyMaps(dictionary8, ExtractPropertyMapViaColumns(morkText, dictionary37, "WebPage1"));
			MergePropertyMaps(dictionary11, ExtractPropertyMapViaColumns(morkText, dictionary37, "Company"));
			MergePropertyMaps(dictionary12, ExtractPropertyMapViaColumns(morkText, dictionary37, "WorkPhone"));
			MergePropertyMaps(dictionary13, ExtractPropertyMapViaColumns(morkText, dictionary37, "CellularNumber"));
			MergePropertyMaps(dictionary14, ExtractPropertyMapViaColumns(morkText, dictionary37, "HomePhone"));
			MergePropertyMaps(dictionary15, ExtractPropertyMapViaColumns(morkText, dictionary37, "FaxNumber"));
			MergePropertyMaps(dictionary16, ExtractPropertyMapViaColumns(morkText, dictionary37, "PagerNumber"));
			MergePropertyMaps(dictionary17, ExtractPropertyMapViaColumns(morkText, dictionary37, "Custom1"));
			MergePropertyMaps(dictionary18, ExtractPropertyMapViaColumns(morkText, dictionary37, "Custom2"));
			MergePropertyMaps(dictionary19, ExtractPropertyMapViaColumns(morkText, dictionary37, "Custom3"));
			MergePropertyMaps(dictionary20, ExtractPropertyMapViaColumns(morkText, dictionary37, "Custom4"));
			MergePropertyMaps(dictionary21, ExtractPropertyMapViaColumns(morkText, dictionary37, "SpouseName"));
			MergePropertyMaps(dictionary22, ExtractPropertyMapViaColumns(morkText, dictionary37, "Notes"));
			MergePropertyMaps(dictionary23, ExtractPropertyMapViaColumns(morkText, dictionary37, "ScreenName"));
			MergePropertyMaps(dictionary24, ExtractPropertyMapViaColumns(morkText, dictionary37, "AimScreenName"));
			MergePropertyMaps(dictionary25, ExtractPropertyMapViaColumns(morkText, dictionary37, "PopularityIndex"));
			MergePropertyMaps(dictionary26, ExtractPropertyMapViaColumns(morkText, dictionary37, "RecordName"));
			MergePropertyMaps(dictionary27, ExtractPropertyMapViaColumns(morkText, dictionary37, "PreferEmail"));
			MergePropertyMaps(dictionary28, ExtractPropertyMapViaColumns(morkText, dictionary37, "LowercasePrimaryEmail"));
			MergePropertyMaps(dictionary29, ExtractPropertyMapViaColumns(morkText, dictionary37, "WorkAddress"));
			MergePropertyMaps(dictionary30, ExtractPropertyMapViaColumns(morkText, dictionary37, "HomeAddress"));
			MergePropertyMaps(dictionary31, ExtractPropertyMapViaColumns(morkText, dictionary37, "BirthDay"));
			MergePropertyMaps(dictionary31, ExtractPropertyMapViaColumns(morkText, dictionary37, "Birthday"));
			MergePropertyMaps(dictionary32, ExtractPropertyMapViaColumns(morkText, dictionary37, "Anniversary"));
			MergePropertyMaps(dictionary33, ExtractPropertyMapViaColumns(morkText, dictionary37, "MiddleName"));
			MergePropertyMaps(dictionary34, ExtractPropertyMapViaColumns(morkText, dictionary37, "OfficeLocation"));
			MergePropertyMaps(dictionary35, ExtractPropertyMapViaColumns(morkText, dictionary37, "OtherPhone"));
			MergePropertyMaps(dictionary36, ExtractPropertyMapViaColumns(morkText, dictionary37, "AssistantName"));
		}
		Dictionary<string, string> dictionary39 = new Dictionary<string, string>(StringComparer.Ordinal);
		foreach (KeyValuePair<string, string> item2 in dictionary9)
		{
			string text = item2.Value.Trim();
			if (text.Length != 0 && EmailPattern.IsMatch(text))
			{
				string displayName = ResolveDisplayName(item2.Key, dictionary2, dictionary3, dictionary4, dictionary5);
				AddContact(dictionary, displayName, text, dictionary11, dictionary12, dictionary14, dictionary22, item2.Key);
				string text2 = text.ToLowerInvariant();
				dictionary39[item2.Key] = text2;
				if (dictionary.ContainsKey(text2))
				{
					ApplyMorkNameFields(dictionary[text2], item2.Key, dictionary3, dictionary4, dictionary5);
					ApplyMorkJobTitle(dictionary[text2], item2.Key, dictionary6);
					ApplyMorkDepartment(dictionary[text2], item2.Key, dictionary7);
					ApplyMorkWebPage(dictionary[text2], item2.Key, dictionary8);
					ApplyMorkCellularNumber(dictionary[text2], item2.Key, dictionary13);
					ApplyMorkFaxNumber(dictionary[text2], item2.Key, dictionary15);
					ApplyMorkPagerNumber(dictionary[text2], item2.Key, dictionary16);
					ApplyMorkCustom1(dictionary[text2], item2.Key, dictionary17);
					ApplyMorkCustom2(dictionary[text2], item2.Key, dictionary18);
					ApplyMorkCustom3(dictionary[text2], item2.Key, dictionary19);
					ApplyMorkCustom4(dictionary[text2], item2.Key, dictionary20);
					ApplyMorkSpouseName(dictionary[text2], item2.Key, dictionary21);
					ApplyMorkScreenName(dictionary[text2], item2.Key, dictionary23);
					ApplyMorkAimScreenName(dictionary[text2], item2.Key, dictionary24);
					ApplyMorkPopularityIndex(dictionary[text2], item2.Key, dictionary25);
					ApplyMorkRecordName(dictionary[text2], item2.Key, dictionary26);
					ApplyMorkPreferEmail(dictionary[text2], item2.Key, dictionary27);
					ApplyMorkWorkAddress(dictionary[text2], item2.Key, dictionary29);
					ApplyMorkHomeAddress(dictionary[text2], item2.Key, dictionary30);
					ApplyMorkBirthDay(dictionary[text2], item2.Key, dictionary31);
					ApplyMorkAnniversary(dictionary[text2], item2.Key, dictionary32);
					ApplyMorkMiddleName(dictionary[text2], item2.Key, dictionary33);
					ApplyMorkOfficeLocation(dictionary[text2], item2.Key, dictionary34);
					ApplyMorkOtherPhone(dictionary[text2], item2.Key, dictionary35);
					ApplyMorkAssistantName(dictionary[text2], item2.Key, dictionary36);
				}
			}
		}
		foreach (KeyValuePair<string, string> item3 in dictionary10)
		{
			string text3 = item3.Value.Trim();
			if (text3.Length == 0 || !EmailPattern.IsMatch(text3))
			{
				continue;
			}
			if (dictionary39.ContainsKey(item3.Key))
			{
				string key = dictionary39[item3.Key];
				if (dictionary.ContainsKey(key))
				{
					dictionary[key].SecondaryEmail = text3.Trim();
				}
			}
			else
			{
				string displayName2 = ResolveDisplayName(item3.Key, dictionary2, dictionary3, dictionary4, dictionary5);
				AddContact(dictionary, displayName2, text3, dictionary11, dictionary12, dictionary14, dictionary22, item3.Key);
			}
		}
		foreach (KeyValuePair<string, string> item4 in dictionary28)
		{
			if (dictionary39.ContainsKey(item4.Key))
			{
				continue;
			}
			string text4 = item4.Value.Trim();
			if (text4.Length != 0 && EmailPattern.IsMatch(text4))
			{
				string displayName3 = ResolveDisplayName(item4.Key, dictionary2, dictionary3, dictionary4, dictionary5);
				AddContact(dictionary, displayName3, text4, dictionary11, dictionary12, dictionary14, dictionary22, item4.Key);
				dictionary39[item4.Key] = text4.ToLowerInvariant();
				string key2 = text4.ToLowerInvariant();
				if (dictionary.ContainsKey(key2))
				{
					ApplyMorkPreferEmail(dictionary[key2], item4.Key, dictionary27);
				}
			}
		}
		if (dictionary.Count == 0)
		{
			foreach (Match item5 in EmailPattern.Matches(morkText))
			{
				AddContact(dictionary, "", item5.Value, null, null, null, null, "");
			}
		}
		LastMorkTableCount = Regex.Matches(morkText, "\\(\\d+:").Count;
		List<MailContact> list = new List<MailContact>(dictionary.Values);
		list.Sort([SpecialName] (MailContact a, MailContact b) => string.Compare(a.ListCaption(), b.ListCaption(), StringComparison.OrdinalIgnoreCase));
		LastMorkParsedContactCount = list.Count;
		return list;
	}

	private static string ResolveDisplayName(string rowId, Dictionary<string, string> displayNames, Dictionary<string, string> firstNames, Dictionary<string, string> lastNames, Dictionary<string, string> nickNames)
	{
		if (displayNames.ContainsKey(rowId))
		{
			string text = displayNames[rowId].Trim();
			if (text.Length > 0)
			{
				return text;
			}
		}
		string text2 = "";
		string text3 = "";
		if (firstNames.ContainsKey(rowId))
		{
			text2 = firstNames[rowId].Trim();
		}
		if (lastNames.ContainsKey(rowId))
		{
			text3 = lastNames[rowId].Trim();
		}
		string text4 = (text2 + " " + text3).Trim();
		if (text4.Length > 0)
		{
			return text4;
		}
		if (nickNames.ContainsKey(rowId))
		{
			return nickNames[rowId].Trim();
		}
		return "";
	}

	private static void ApplyMorkNameFields(MailContact contact, string rowId, Dictionary<string, string> firstNames, Dictionary<string, string> lastNames, Dictionary<string, string> nickNames)
	{
		if (contact != null && !string.IsNullOrWhiteSpace(rowId))
		{
			if (firstNames != null && firstNames.ContainsKey(rowId))
			{
				contact.FirstName = firstNames[rowId].Trim();
			}
			if (lastNames != null && lastNames.ContainsKey(rowId))
			{
				contact.LastName = lastNames[rowId].Trim();
			}
			if (nickNames != null && nickNames.ContainsKey(rowId))
			{
				contact.NickName = nickNames[rowId].Trim();
			}
		}
	}

	private static void ApplyMorkJobTitle(MailContact contact, string rowId, Dictionary<string, string> jobTitles)
	{
		if (contact != null && !string.IsNullOrWhiteSpace(rowId) && jobTitles != null && jobTitles.ContainsKey(rowId))
		{
			contact.JobTitle = jobTitles[rowId].Trim();
		}
	}

	private static void ApplyMorkDepartment(MailContact contact, string rowId, Dictionary<string, string> departments)
	{
		if (contact != null && !string.IsNullOrWhiteSpace(rowId) && departments != null && departments.ContainsKey(rowId))
		{
			contact.Department = departments[rowId].Trim();
		}
	}

	private static void ApplyMorkWebPage(MailContact contact, string rowId, Dictionary<string, string> webPages)
	{
		if (contact != null && !string.IsNullOrWhiteSpace(rowId) && webPages != null && webPages.ContainsKey(rowId))
		{
			contact.WebPage = webPages[rowId].Trim();
		}
	}

	private static void ApplyMorkCellularNumber(MailContact contact, string rowId, Dictionary<string, string> cellularNumbers)
	{
		if (contact != null && !string.IsNullOrWhiteSpace(rowId) && cellularNumbers != null && cellularNumbers.ContainsKey(rowId))
		{
			string text = cellularNumbers[rowId].Trim();
			if (text.Length > 0)
			{
				contact.MobilePhone = text;
			}
		}
	}

	private static void ApplyMorkFaxNumber(MailContact contact, string rowId, Dictionary<string, string> faxNumbers)
	{
		if (contact != null && !string.IsNullOrWhiteSpace(rowId) && faxNumbers != null && faxNumbers.ContainsKey(rowId))
		{
			contact.FaxNumber = faxNumbers[rowId].Trim();
		}
	}

	private static void ApplyMorkPagerNumber(MailContact contact, string rowId, Dictionary<string, string> pagerNumbers)
	{
		if (contact != null && !string.IsNullOrWhiteSpace(rowId) && pagerNumbers != null && pagerNumbers.ContainsKey(rowId))
		{
			contact.PagerNumber = pagerNumbers[rowId].Trim();
		}
	}

	private static void ApplyMorkCustom1(MailContact contact, string rowId, Dictionary<string, string> custom1Values)
	{
		if (contact != null && !string.IsNullOrWhiteSpace(rowId) && custom1Values != null && custom1Values.ContainsKey(rowId))
		{
			contact.Custom1 = custom1Values[rowId].Trim();
		}
	}

	private static void ApplyMorkCustom2(MailContact contact, string rowId, Dictionary<string, string> custom2Values)
	{
		if (contact != null && !string.IsNullOrWhiteSpace(rowId) && custom2Values != null && custom2Values.ContainsKey(rowId))
		{
			contact.Custom2 = custom2Values[rowId].Trim();
		}
	}

	private static void ApplyMorkCustom3(MailContact contact, string rowId, Dictionary<string, string> custom3Values)
	{
		if (contact != null && !string.IsNullOrWhiteSpace(rowId) && custom3Values != null && custom3Values.ContainsKey(rowId))
		{
			contact.Custom3 = custom3Values[rowId].Trim();
		}
	}

	private static void ApplyMorkCustom4(MailContact contact, string rowId, Dictionary<string, string> custom4Values)
	{
		if (contact != null && !string.IsNullOrWhiteSpace(rowId) && custom4Values != null && custom4Values.ContainsKey(rowId))
		{
			contact.Custom4 = custom4Values[rowId].Trim();
		}
	}

	private static void ApplyMorkSpouseName(MailContact contact, string rowId, Dictionary<string, string> spouseNames)
	{
		if (contact != null && !string.IsNullOrWhiteSpace(rowId) && spouseNames != null && spouseNames.ContainsKey(rowId))
		{
			contact.SpouseName = spouseNames[rowId].Trim();
		}
	}

	private static void ApplyMorkScreenName(MailContact contact, string rowId, Dictionary<string, string> screenNames)
	{
		if (contact != null && !string.IsNullOrWhiteSpace(rowId) && screenNames != null && screenNames.ContainsKey(rowId))
		{
			contact.ScreenName = screenNames[rowId].Trim();
		}
	}

	private static void ApplyMorkAimScreenName(MailContact contact, string rowId, Dictionary<string, string> aimNames)
	{
		if (contact != null && !string.IsNullOrWhiteSpace(rowId) && aimNames != null && aimNames.ContainsKey(rowId))
		{
			contact.AimScreenName = aimNames[rowId].Trim();
		}
	}

	private static void ApplyMorkPopularityIndex(MailContact contact, string rowId, Dictionary<string, string> values)
	{
		if (contact != null && !string.IsNullOrWhiteSpace(rowId) && values != null && values.ContainsKey(rowId))
		{
			contact.PopularityIndex = values[rowId].Trim();
		}
	}

	private static void ApplyMorkRecordName(MailContact contact, string rowId, Dictionary<string, string> values)
	{
		if (contact != null && !string.IsNullOrWhiteSpace(rowId) && values != null && values.ContainsKey(rowId))
		{
			contact.RecordName = values[rowId].Trim();
		}
	}

	private static void ApplyMorkPreferEmail(MailContact contact, string rowId, Dictionary<string, string> values)
	{
		if (contact != null && !string.IsNullOrWhiteSpace(rowId) && values != null && values.ContainsKey(rowId))
		{
			contact.PreferEmail = values[rowId].Trim();
		}
	}

	private static void ApplyMorkWorkAddress(MailContact contact, string rowId, Dictionary<string, string> values)
	{
		if (contact != null && !string.IsNullOrWhiteSpace(rowId) && values != null && values.ContainsKey(rowId))
		{
			contact.WorkAddress = values[rowId].Trim();
		}
	}

	private static void ApplyMorkHomeAddress(MailContact contact, string rowId, Dictionary<string, string> values)
	{
		if (contact != null && !string.IsNullOrWhiteSpace(rowId) && values != null && values.ContainsKey(rowId))
		{
			contact.HomeAddress = values[rowId].Trim();
		}
	}

	private static void ApplyMorkBirthDay(MailContact contact, string rowId, Dictionary<string, string> values)
	{
		if (contact != null && !string.IsNullOrWhiteSpace(rowId) && values != null && values.ContainsKey(rowId))
		{
			contact.Birthday = values[rowId].Trim();
		}
	}

	private static void ApplyMorkAnniversary(MailContact contact, string rowId, Dictionary<string, string> values)
	{
		if (contact != null && !string.IsNullOrWhiteSpace(rowId) && values != null && values.ContainsKey(rowId))
		{
			contact.Anniversary = values[rowId].Trim();
		}
	}

	private static void ApplyMorkMiddleName(MailContact contact, string rowId, Dictionary<string, string> values)
	{
		if (contact != null && !string.IsNullOrWhiteSpace(rowId) && values != null && values.ContainsKey(rowId))
		{
			contact.MiddleName = values[rowId].Trim();
		}
	}

	private static void ApplyMorkOfficeLocation(MailContact contact, string rowId, Dictionary<string, string> values)
	{
		if (contact != null && !string.IsNullOrWhiteSpace(rowId) && values != null && values.ContainsKey(rowId))
		{
			contact.OfficeLocation = values[rowId].Trim();
		}
	}

	private static void ApplyMorkOtherPhone(MailContact contact, string rowId, Dictionary<string, string> values)
	{
		if (contact != null && !string.IsNullOrWhiteSpace(rowId) && values != null && values.ContainsKey(rowId))
		{
			contact.OtherPhone = values[rowId].Trim();
		}
	}

	private static void ApplyMorkAssistantName(MailContact contact, string rowId, Dictionary<string, string> values)
	{
		if (contact != null && !string.IsNullOrWhiteSpace(rowId) && values != null && values.ContainsKey(rowId))
		{
			contact.AssistantName = values[rowId].Trim();
		}
	}

	private static Dictionary<string, string> PickPrimaryEmailTableMap(List<Dictionary<string, string>> tableMaps)
	{
		if (tableMaps == null || tableMaps.Count == 0)
		{
			return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
		Dictionary<string, string> result = tableMaps[0];
		int num = 0;
		foreach (Dictionary<string, string> tableMap in tableMaps)
		{
			if (tableMap != null && FindColumnId(tableMap, "PrimaryEmail").Length != 0 && tableMap.Count > num)
			{
				num = tableMap.Count;
				result = tableMap;
			}
		}
		return result;
	}

	private static void AddContact(Dictionary<string, MailContact> byEmail, string displayName, string email, Dictionary<string, string> companies, Dictionary<string, string> workPhones, Dictionary<string, string> homePhones, Dictionary<string, string> notesMap, string rowId)
	{
		string key = email.ToLowerInvariant();
		if (!byEmail.ContainsKey(key))
		{
			string company = "";
			string workPhone = "";
			string homePhone = "";
			string notes = "";
			if (companies != null && companies.ContainsKey(rowId))
			{
				company = companies[rowId].Trim();
			}
			if (workPhones != null && workPhones.ContainsKey(rowId))
			{
				workPhone = workPhones[rowId].Trim();
			}
			if (homePhones != null && homePhones.ContainsKey(rowId))
			{
				homePhone = homePhones[rowId].Trim();
			}
			if (notesMap != null && notesMap.ContainsKey(rowId))
			{
				notes = notesMap[rowId].Trim();
			}
			byEmail[key] = new MailContact
			{
				DisplayName = ((displayName != null) ? displayName : "").Trim(),
				Email = email.Trim(),
				Company = company,
				WorkPhone = workPhone,
				HomePhone = homePhone,
				Notes = notes,
				MorkRowId = ((rowId != null) ? rowId : "").Trim()
			};
		}
	}

	private static void MergeMapsFromRowProps(Dictionary<string, Dictionary<string, string>> rowProps, string propertyName, Dictionary<string, string> target)
	{
		if (rowProps == null || target == null)
		{
			return;
		}
		foreach (KeyValuePair<string, Dictionary<string, string>> rowProp in rowProps)
		{
			if (rowProp.Value != null)
			{
				string value = null;
				if (rowProp.Value.TryGetValue(propertyName, out value) && !string.IsNullOrWhiteSpace(value) && !target.ContainsKey(rowProp.Key))
				{
					target[rowProp.Key] = value.Trim();
				}
			}
		}
	}

	internal static Dictionary<string, Dictionary<string, string>> BuildRowPropertyMaps(string text, Dictionary<string, string> colIdToProp)
	{
		Dictionary<string, Dictionary<string, string>> dictionary = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
		if (string.IsNullOrEmpty(text))
		{
			return dictionary;
		}
		Dictionary<string, string> dictionary2 = colIdToProp;
		if (dictionary2 == null || dictionary2.Count == 0)
		{
			dictionary2 = ExtractColumnKeyMap(text);
		}
		Dictionary<string, string> dictionary3 = ParseRawRowBodies(text);
		Dictionary<string, Dictionary<string, string>> dictionary4 = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
		foreach (KeyValuePair<string, string> item in dictionary3)
		{
			dictionary4[item.Key] = ParseRowCells(item.Value, dictionary2);
		}
		foreach (string key in dictionary4.Keys)
		{
			Dictionary<string, string> dictionary5 = ResolveRowProperties(key, dictionary2, dictionary4, new HashSet<string>(StringComparer.Ordinal));
			if (dictionary5 != null && dictionary5.Count > 0)
			{
				dictionary[key] = dictionary5;
			}
		}
		return dictionary;
	}

	private static Dictionary<string, string> ParseRawRowBodies(string text)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
		Regex regex = new Regex("(?:^|[\\s\\r\\n])@([^=\\s<>]+)=", RegexOptions.Compiled);
		MatchCollection matchCollection = regex.Matches(text);
		checked
		{
			int num = matchCollection.Count - 1;
			for (int i = 0; i <= num; i++)
			{
				Match match = matchCollection[i];
				string text2 = match.Groups[1].Value.Trim();
				if (text2.Length != 0)
				{
					int num2 = match.Index + match.Length;
					int num3 = ((i < matchCollection.Count - 1) ? matchCollection[i + 1].Index : text.Length);
					string value = text.Substring(num2, Math.Max(0, num3 - num2));
					if (!dictionary.ContainsKey(text2))
					{
						dictionary[text2] = value;
					}
				}
			}
			return dictionary;
		}
	}

	private static Dictionary<string, string> ParseRowCells(string body, Dictionary<string, string> colIdToProp)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
		if (string.IsNullOrEmpty(body))
		{
			return dictionary;
		}
		if (colIdToProp != null)
		{
			foreach (KeyValuePair<string, string> item in colIdToProp)
			{
				string key = item.Key;
				Regex regex = new Regex("\\^" + Regex.Escape(key) + "\\^([^\\^<()]+)", RegexOptions.Compiled);
				Match match = regex.Match(body);
				if (match.Success)
				{
					dictionary[key] = match.Groups[1].Value.Trim();
				}
			}
		}
		Regex regex2 = new Regex("\\(([^=\\)]+)=([^\\)]+)\\)", RegexOptions.Compiled);
		foreach (Match item2 in regex2.Matches(body))
		{
			string text = item2.Groups[1].Value.Trim();
			string value = item2.Groups[2].Value.Trim();
			if (text.Length == 0)
			{
				continue;
			}
			if (text.StartsWith("d=", StringComparison.OrdinalIgnoreCase))
			{
				string text2 = text.Substring(2).Trim();
				if (text2.StartsWith("c", StringComparison.OrdinalIgnoreCase) && text2.Length > 1)
				{
					text = text2.Substring(1);
				}
				else
				{
					if (!text2.StartsWith("%", StringComparison.OrdinalIgnoreCase))
					{
						dictionary["d:" + text2] = value;
						continue;
					}
					int num = text2.IndexOf(':');
					if (num > 1)
					{
						text = text2.Substring(1, checked(num - 1));
					}
				}
			}
			if (!dictionary.ContainsKey(text))
			{
				dictionary[text] = value;
			}
		}
		Regex regex3 = new Regex("\\(d=([^>\\)]+)>([^<(\\s]+)", RegexOptions.Compiled);
		foreach (Match item3 in regex3.Matches(body))
		{
			string text3 = item3.Groups[1].Value.Trim();
			string text4 = item3.Groups[2].Value.Trim();
			if (text3.Length != 0 && text4.Length != 0)
			{
				dictionary["d:" + text3] = text4;
			}
		}
		return dictionary;
	}

	private static Dictionary<string, string> ResolveRowProperties(string rowId, Dictionary<string, string> colIdToProp, Dictionary<string, Dictionary<string, string>> rawCells, HashSet<string> visiting)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (string.IsNullOrEmpty(rowId) || rawCells == null)
		{
			return dictionary;
		}
		if (!rawCells.ContainsKey(rowId))
		{
			return dictionary;
		}
		if (visiting.Contains(rowId))
		{
			return dictionary;
		}
		visiting.Add(rowId);
		Dictionary<string, string> dictionary2 = rawCells[rowId];
		foreach (KeyValuePair<string, string> item in dictionary2)
		{
			string key = item.Key;
			string text = ResolveCellValue(rowId, key, item.Value, colIdToProp, rawCells, visiting, 0);
			if (text.Length != 0)
			{
				if (key.StartsWith("d:", StringComparison.Ordinal))
				{
					dictionary[key.Substring(2)] = text;
				}
				else if (colIdToProp != null && colIdToProp.ContainsKey(key))
				{
					dictionary[colIdToProp[key]] = text;
				}
			}
		}
		visiting.Remove(rowId);
		return dictionary;
	}

	private static string ResolveCellValue(string rowId, string colKey, string raw, Dictionary<string, string> colIdToProp, Dictionary<string, Dictionary<string, string>> rawCells, HashSet<string> visiting, int depth)
	{
		if (depth > 24)
		{
			return "";
		}
		string text = ((raw != null) ? raw : "").Trim();
		if (text.Length == 0)
		{
			return "";
		}
		text = UnescapeMorkValue(text);
		if (IsRowReference(text) && rawCells.ContainsKey(text))
		{
			if (rawCells[text].ContainsKey(colKey))
			{
				return ResolveCellValue(text, colKey, rawCells[text][colKey], colIdToProp, rawCells, visiting, checked(depth + 1));
			}
			Dictionary<string, string> dictionary = ResolveRowProperties(text, colIdToProp, rawCells, visiting);
			if (colIdToProp != null && colIdToProp.ContainsKey(colKey))
			{
				string key = colIdToProp[colKey];
				if (dictionary.ContainsKey(key))
				{
					return dictionary[key];
				}
			}
			if (dictionary.ContainsKey("PrimaryEmail"))
			{
				return dictionary["PrimaryEmail"];
			}
		}
		return text;
	}

	private static bool IsRowReference(string value)
	{
		string text = ((value != null) ? value : "").Trim();
		if (text.Length == 0 || text.Length > 16)
		{
			return false;
		}
		if (text.IndexOf('@') >= 0 || text.IndexOf('.') >= 0)
		{
			return false;
		}
		return Regex.IsMatch(text, "^-?[0-9A-Fa-f]+$");
	}

	private static Dictionary<string, string> ExtractColumnKeyMap(string text)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (string.IsNullOrEmpty(text))
		{
			return dictionary;
		}
		Regex regex = new Regex("\\(k=([^\\)]*)\\)", RegexOptions.Compiled);
		checked
		{
			foreach (Match item in regex.Matches(text))
			{
				string value = item.Groups[1].Value;
				Regex regex2 = new Regex("%([0-9A-Fa-f]+):([A-Za-z0-9_]+)", RegexOptions.Compiled);
				foreach (Match item2 in regex2.Matches(value))
				{
					string text2 = item2.Groups[1].Value.Trim();
					string text3 = item2.Groups[2].Value.Trim();
					if (text2.Length > 0 && text3.Length > 0 && !dictionary.ContainsKey(text2))
					{
						dictionary[text2] = text3;
					}
				}
				string[] array = value.Split('^');
				for (int i = 0; i < array.Length - 1; i += 2)
				{
					string text4 = array[i].Trim();
					string text5 = array[i + 1].Trim();
					if (text4.StartsWith("%"))
					{
						int num = text4.IndexOf(':');
						if (num > 1)
						{
							text5 = text4.Substring(num + 1).Trim();
							text4 = text4.Substring(1, num - 1).Trim();
						}
					}
					if (text4.Length > 0 && text5.Length > 0 && Regex.IsMatch(text4, "^[0-9A-Fa-f]+$") && !dictionary.ContainsKey(text4))
					{
						dictionary[text4] = text5;
					}
				}
			}
			return dictionary;
		}
	}

	private static Dictionary<string, string> ParseColumnKeyChunk(string chunk)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (string.IsNullOrEmpty(chunk))
		{
			return dictionary;
		}
		Regex regex = new Regex("%([0-9A-Fa-f]+):([A-Za-z0-9_]+)", RegexOptions.Compiled);
		foreach (Match item in regex.Matches(chunk))
		{
			string text = item.Groups[1].Value.Trim();
			string text2 = item.Groups[2].Value.Trim();
			if (text.Length > 0 && text2.Length > 0 && !dictionary.ContainsKey(text))
			{
				dictionary[text] = text2;
			}
		}
		string[] array = chunk.Split('^');
		checked
		{
			for (int i = 0; i < array.Length - 1; i += 2)
			{
				string text3 = array[i].Trim();
				string text4 = array[i + 1].Trim();
				if (text3.StartsWith("%"))
				{
					int num = text3.IndexOf(':');
					if (num > 1)
					{
						text4 = text3.Substring(num + 1).Trim();
						text3 = text3.Substring(1, num - 1).Trim();
					}
				}
				if (text3.Length > 0 && text4.Length > 0 && Regex.IsMatch(text3, "^[0-9A-Fa-f]+$") && !dictionary.ContainsKey(text3))
				{
					dictionary[text3] = text4;
				}
			}
			return dictionary;
		}
	}

	private static List<Dictionary<string, string>> ExtractPerTableColumnMaps(string text)
	{
		List<Dictionary<string, string>> list = new List<Dictionary<string, string>>();
		if (string.IsNullOrEmpty(text))
		{
			return list;
		}
		Regex regex = new Regex("\\(k=([^\\)]*)\\)", RegexOptions.Compiled);
		foreach (Match item in regex.Matches(text))
		{
			Dictionary<string, string> dictionary = ParseColumnKeyChunk(item.Groups[1].Value);
			if (dictionary.Count > 0)
			{
				list.Add(dictionary);
			}
		}
		return list;
	}

	private static Dictionary<string, string> ResolveColumnMapForRowBody(string rowBody, List<Dictionary<string, string>> tableMaps)
	{
		if (tableMaps == null || tableMaps.Count == 0)
		{
			return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
		string text = ((rowBody != null) ? rowBody : "");
		Dictionary<string, string> result = tableMaps[0];
		int num = -1;
		foreach (Dictionary<string, string> tableMap in tableMaps)
		{
			if (tableMap == null)
			{
				continue;
			}
			int num2 = 0;
			foreach (string key in tableMap.Keys)
			{
				if (text.IndexOf("^" + key + "^", StringComparison.Ordinal) >= 0)
				{
					num2 = checked(num2 + 1);
				}
			}
			if (num2 > num)
			{
				num = num2;
				result = tableMap;
			}
		}
		return result;
	}

	private static void MergePropertyMaps(Dictionary<string, string> target, Dictionary<string, string> source)
	{
		if (target == null || source == null)
		{
			return;
		}
		foreach (KeyValuePair<string, string> item in source)
		{
			if (!target.ContainsKey(item.Key))
			{
				target[item.Key] = item.Value;
			}
		}
	}

	private static Dictionary<string, string> ExtractPropertyMapViaColumns(string text, Dictionary<string, string> colIdToProp, string propertyName)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
		if (string.IsNullOrEmpty(text) || colIdToProp == null || colIdToProp.Count == 0)
		{
			return dictionary;
		}
		string text2 = null;
		foreach (KeyValuePair<string, string> item in colIdToProp)
		{
			if (string.Equals(item.Value, propertyName, StringComparison.OrdinalIgnoreCase))
			{
				text2 = item.Key;
				break;
			}
		}
		if (string.IsNullOrEmpty(text2))
		{
			return dictionary;
		}
		string pattern = "\\^" + Regex.Escape(text2) + "\\^([^\\^<()]+)";
		Regex regex = new Regex(pattern, RegexOptions.Compiled);
		Regex regex2 = new Regex("@([^=]+)=([^@]*)", RegexOptions.Compiled);
		foreach (Match item2 in regex2.Matches(text))
		{
			string text3 = item2.Groups[1].Value.Trim();
			if (text3.Length == 0)
			{
				continue;
			}
			Match match2 = regex.Match(item2.Groups[2].Value);
			if (match2.Success)
			{
				string text4 = UnescapeMorkValue(match2.Groups[1].Value.Trim());
				if (text4.Length > 0 && !dictionary.ContainsKey(text3))
				{
					dictionary[text3] = text4;
				}
			}
		}
		return dictionary;
	}

	private static Dictionary<string, string> ExtractPropertyMap(string text, string propertyName)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
		if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(propertyName))
		{
			return dictionary;
		}
		string text2 = "(d=" + propertyName + ")";
		int num = 0;
		checked
		{
			while (num < text.Length)
			{
				int num2 = text.IndexOf(text2, num, StringComparison.Ordinal);
				if (num2 < 0)
				{
					break;
				}
				int num3 = text.LastIndexOf('@', num2);
				if (num3 < 0)
				{
					num = num2 + text2.Length;
					continue;
				}
				int num4 = text.IndexOf('>', num2);
				if (num4 < 0)
				{
					num = num2 + text2.Length;
					continue;
				}
				string text3 = ExtractRowId(text, num3, num2);
				string text4 = ExtractCellValue(text, num4 + 1);
				if (text3.Length > 0 && text4.Length > 0 && !dictionary.ContainsKey(text3))
				{
					dictionary[text3] = text4;
				}
				num = num4 + 1;
			}
			return dictionary;
		}
	}

	private static string ExtractRowId(string text, int rowStart, int markerPos)
	{
		checked
		{
			string text2 = text.Substring(rowStart, Math.Max(0, markerPos - rowStart));
			if (text2.StartsWith("@", StringComparison.Ordinal))
			{
				int num = text2.IndexOf('=');
				if (num > 1)
				{
					return text2.Substring(1, num - 1).Trim();
				}
			}
			Match match = Regex.Match(text2, "\\(([^=\\)]+)=", RegexOptions.RightToLeft);
			if (match.Success)
			{
				return match.Groups[1].Value.Trim();
			}
			return "";
		}
	}

	private static string ExtractCellValue(string text, int start)
	{
		if (start >= text.Length)
		{
			return "";
		}
		int num = text.IndexOfAny(new char[5] { '<', '(', '@', '\n', '\r' }, start);
		checked
		{
			if (num < 0)
			{
				num = Math.Min(text.Length, start + 200);
			}
			string text2 = text.Substring(start, num - start).Trim();
			text2 = text2.Trim(')', '(', '^', ' ', '\t');
			if (text2.StartsWith("="))
			{
				text2 = text2.Substring(1);
			}
			return UnescapeMorkValue(text2);
		}
	}

	private static string UnescapeMorkValue(string raw)
	{
		if (raw == null)
		{
			return "";
		}
		return raw.Replace("$20", " ").Replace("$40", "@").Replace("$2C", ",")
			.Replace("$28", "(")
			.Replace("$29", ")");
	}

	public static List<MailContact> TryParseContactsFromLdif(string ldifPath)
	{
		LastError = null;
		List<MailContact> list = new List<MailContact>();
		List<MailContact> result;
		if (string.IsNullOrWhiteSpace(ldifPath) || !File.Exists(ldifPath))
		{
			LastError = "LDIF file not found.";
			result = list;
		}
		else
		{
			try
			{
				string text = null;
				string text2 = null;
				string[] array = File.ReadAllLines(ldifPath, Encoding.UTF8);
				foreach (string text3 in array)
				{
					string text4 = ((text3 != null) ? text3 : "").Trim();
					if (text4.Length == 0)
					{
						if (text != null && text.Length > 0)
						{
							list.Add(new MailContact
							{
								Email = text,
								DisplayName = ((text2 != null) ? text2 : text).Trim()
							});
						}
						text = null;
						text2 = null;
					}
					else if (text4.StartsWith("mail:", StringComparison.OrdinalIgnoreCase))
					{
						text = text4.Substring(5).Trim();
					}
					else if (text4.StartsWith("cn:", StringComparison.OrdinalIgnoreCase))
					{
						text2 = text4.Substring(3).Trim();
					}
				}
				if (text != null && text.Length > 0)
				{
					list.Add(new MailContact
					{
						Email = text,
						DisplayName = ((text2 != null) ? text2 : text).Trim()
					});
				}
				result = list;
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Exception ex2 = ex;
				LastError = ex2.Message;
				result = new List<MailContact>();
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}

	public static bool ExportContactsToLdif(List<MailContact> contacts, string destPath)
	{
		LastError = null;
		bool result;
		if (contacts == null || string.IsNullOrWhiteSpace(destPath))
		{
			LastError = "Contacts and path required.";
			result = false;
		}
		else
		{
			try
			{
				StringBuilder stringBuilder = new StringBuilder();
				int num = 1;
				foreach (MailContact contact in contacts)
				{
					if (contact != null && !string.IsNullOrWhiteSpace(contact.Email))
					{
						stringBuilder.AppendLine("dn: cn=" + num + ",ou=contacts");
						stringBuilder.AppendLine("objectClass: inetOrgPerson");
						MailContact mailContact = contact;
						stringBuilder.AppendLine("cn: " + ((mailContact.DisplayName != null) ? mailContact.DisplayName : contact.Email));
						stringBuilder.AppendLine("mail: " + contact.Email);
						stringBuilder.AppendLine("");
						num = checked(num + 1);
					}
				}
				File.WriteAllText(destPath, stringBuilder.ToString(), Encoding.UTF8);
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

	public static bool TryWriteMorkPreservingSource(string sourceMabPath, List<MailContact> contacts, string destPath, ref bool byteIdentical)
	{
		LastError = null;
		byteIdentical = false;
		LastMorkPreserveExportFieldValidated = false;
		checked
		{
			bool result;
			if (string.IsNullOrWhiteSpace(sourceMabPath) || !File.Exists(sourceMabPath))
			{
				LastError = "Source abook.mab not found.";
				result = false;
			}
			else if (contacts == null || string.IsNullOrWhiteSpace(destPath))
			{
				LastError = "Contacts and path required.";
				result = false;
			}
			else if (TryContactsUnchangedVersusSource(sourceMabPath, contacts))
			{
				try
				{
					File.Copy(sourceMabPath, destPath, overwrite: true);
					byteIdentical = true;
					LastMabExportByteIdentical = true;
					LastMorkPreserveExportFieldValidated = true;
					LastMabExportVerifiedAgainstSource = TryVerifyExportBytesMatchSource(sourceMabPath, destPath);
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
			else
			{
				try
				{
					byte[] array = File.ReadAllBytes(sourceMabPath);
					Encoding encoding = DetectMorkFileEncoding(array);
					string text = encoding.GetString(array);
					string lineBreak = ((text.IndexOf("\r\n", StringComparison.Ordinal) >= 0) ? "\r\n" : "\n");
					string morkText = text;
					List<Dictionary<string, string>> list = ExtractPerTableColumnMaps(text);
					Dictionary<string, string> dictionary = ExtractColumnKeyMap(text);
					Dictionary<string, string> dictionary2 = ParseRawRowBodies(text);
					int num = FindMaxMorkRowId(text) + 1;
					foreach (MailContact contact in contacts)
					{
						if (contact == null || string.IsNullOrWhiteSpace(contact.Email))
						{
							continue;
						}
						string text2 = contact.Email.Trim();
						string emailEsc = EscapeMorkValue(text2);
						MailContact mailContact = contact;
						string nameEsc = EscapeMorkValue(((mailContact.DisplayName != null) ? mailContact.DisplayName : text2).Trim());
						string text3 = FindMorkRowIdForEmail(text, text2);
						if (text3.Length == 0 && !string.IsNullOrWhiteSpace(contact.MorkRowId))
						{
							text3 = contact.MorkRowId.Trim();
						}
						Dictionary<string, string> colMap = dictionary;
						if (text3.Length > 0 && dictionary2.ContainsKey(text3) && list.Count > 0)
						{
							colMap = ResolveColumnMapForRowBody(dictionary2[text3], list);
						}
						else if (list.Count > 0)
						{
							colMap = PickPrimaryEmailTableMap(list);
						}
						bool useColumnCells = PreferColumnCellRows(text, colMap);
						if (text3.Length > 0)
						{
							string text4 = TryReplaceMorkRowLine(morkText, text3, colMap, useColumnCells, contact, emailEsc, nameEsc, lineBreak);
							if (text4.Length > 0)
							{
								morkText = text4;
								continue;
							}
						}
						if (FindMorkRowIdForEmail(morkText, text2).Length <= 0)
						{
							string text5 = num.ToString();
							num++;
							string rowLine = BuildMorkAppendRow(text5, colMap, useColumnCells, contact, emailEsc, nameEsc);
							contact.MorkRowId = text5;
							morkText = TryInsertMorkRowBeforeTableEnd(morkText, rowLine, lineBreak, colMap);
						}
					}
					HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
					foreach (MailContact contact2 in contacts)
					{
						if (contact2 != null && !string.IsNullOrWhiteSpace(contact2.Email))
						{
							hashSet.Add(contact2.Email.Trim());
						}
					}
					morkText = TryRemoveOrphanMorkRows(morkText, hashSet, lineBreak);
					morkText = TryRemoveOrphanMorkRowsFromRawBodies(morkText, dictionary2, hashSet, lineBreak);
					byte[] bytes = encoding.GetBytes(morkText);
					File.WriteAllBytes(destPath, bytes);
					List<MailContact> list2 = TryParseContacts(destPath);
					if (list2 == null || list2.Count == 0)
					{
						LastError = "Preserving export failed parse.";
						result = false;
					}
					else
					{
						byteIdentical = ((array.Length == bytes.Length && StructuralCompareBytes(array, bytes)) ? true : false);
						LastMabExportByteIdentical = byteIdentical;
						if (byteIdentical)
						{
							LastMabExportVerifiedAgainstSource = TryVerifyExportBytesMatchSource(sourceMabPath, destPath);
						}
						if (!TryValidateMorkRoundTrip(contacts, destPath))
						{
							result = false;
						}
						else
						{
							LastMorkPreserveExportFieldValidated = true;
							result = true;
						}
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
			return result;
		}
	}

	public static bool TryWriteMorkFromContacts(List<MailContact> contacts, string destPath)
	{
		LastError = null;
		bool result;
		if (contacts == null || string.IsNullOrWhiteSpace(destPath))
		{
			LastError = "Contacts and path required.";
			result = false;
		}
		else
		{
			try
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.AppendLine("// <! <(meta 1)> // (f=iso-8859-1)");
				stringBuilder.AppendLine("(k=^80^PrimaryEmail^81^DisplayName^82^Company^83^WorkPhone^84^HomePhone^85^Notes^86^FirstName^87^LastName)");
				int num = 1;
				foreach (MailContact contact in contacts)
				{
					if (contact == null || string.IsNullOrWhiteSpace(contact.Email))
					{
						continue;
					}
					string text = num.ToString();
					MailContact mailContact = contact;
					string nameEsc = EscapeMorkValue((mailContact.DisplayName != null) ? mailContact.DisplayName : contact.Email);
					string emailEsc = EscapeMorkValue(contact.Email.Trim());
					Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
					dictionary.Add("80", "PrimaryEmail");
					dictionary.Add("81", "DisplayName");
					dictionary.Add("82", "Company");
					dictionary.Add("83", "WorkPhone");
					dictionary.Add("84", "HomePhone");
					dictionary.Add("85", "Notes");
					Dictionary<string, string> colMap = dictionary;
					bool flag = num % 2 == 0;
					string text2 = BuildMorkAppendRow(text, colMap, flag, contact, emailEsc, nameEsc);
					if (!flag)
					{
						if (!string.IsNullOrWhiteSpace(contact.WorkPhone))
						{
							text2 = text2 + "<" + text + ">(d=WorkPhone)>" + EscapeMorkValue(contact.WorkPhone);
						}
						if (!string.IsNullOrWhiteSpace(contact.HomePhone))
						{
							text2 = text2 + "<" + text + ">(d=HomePhone)>" + EscapeMorkValue(contact.HomePhone);
						}
						if (!string.IsNullOrWhiteSpace(contact.Notes))
						{
							text2 = text2 + "<" + text + ">(d=Notes)>" + EscapeMorkValue(contact.Notes);
						}
					}
					stringBuilder.AppendLine(text2);
					num = checked(num + 1);
				}
				Encoding encoding = Encoding.GetEncoding("iso-8859-1");
				File.WriteAllBytes(destPath, encoding.GetBytes(stringBuilder.ToString()));
				result = (TryValidateMorkRoundTrip(contacts, destPath) ? true : false);
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

	internal static Encoding DetectMorkFileEncoding(byte[] fileBytes)
	{
		if (fileBytes == null || fileBytes.Length == 0)
		{
			return Encoding.UTF8;
		}
		string text = Encoding.ASCII.GetString(fileBytes, 0, Math.Min(fileBytes.Length, 4096));
		if (text.IndexOf("iso-8859-1", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return Encoding.GetEncoding("iso-8859-1");
		}
		return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
	}

	public static bool TryWriteMorkFromSourceLayout(string sourceMabPath, List<MailContact> contacts, string destPath)
	{
		LastError = null;
		checked
		{
			bool result;
			if (string.IsNullOrWhiteSpace(sourceMabPath) || !File.Exists(sourceMabPath))
			{
				LastError = "Source abook.mab not found.";
				result = false;
			}
			else if (contacts == null || string.IsNullOrWhiteSpace(destPath))
			{
				LastError = "Contacts and path required.";
				result = false;
			}
			else
			{
				try
				{
					byte[] array = File.ReadAllBytes(sourceMabPath);
					Encoding encoding = DetectMorkFileEncoding(array);
					string text = encoding.GetString(array);
					string text2 = ((text.IndexOf("\r\n", StringComparison.Ordinal) >= 0) ? "\r\n" : "\n");
					List<Dictionary<string, string>> list = ExtractPerTableColumnMaps(text);
					Dictionary<string, string> dictionary = ExtractColumnKeyMap(text);
					Dictionary<string, string> dictionary2 = ParseRawRowBodies(text);
					StringBuilder stringBuilder = new StringBuilder();
					string[] array2 = text.Split(new string[1] { text2 }, StringSplitOptions.None);
					foreach (string text3 in array2)
					{
						if (text3.StartsWith("@", StringComparison.Ordinal))
						{
							break;
						}
						stringBuilder.Append(text3);
						stringBuilder.Append(text2);
					}
					if (list.Count == 0 && dictionary.Count == 0)
					{
						stringBuilder.Append("(k=^80^PrimaryEmail^81^DisplayName^82^Company^83^WorkPhone^84^HomePhone^85^Notes^86^FirstName^87^LastName)");
						stringBuilder.Append(text2);
					}
					int num = 1;
					foreach (MailContact contact in contacts)
					{
						if (contact != null && !string.IsNullOrWhiteSpace(contact.Email))
						{
							MailContact mailContact = contact;
							string text4 = ((mailContact.MorkRowId != null) ? mailContact.MorkRowId : "").Trim();
							if (text4.Length == 0)
							{
								text4 = num.ToString();
								num++;
							}
							Dictionary<string, string> colMap = dictionary;
							if (dictionary2.ContainsKey(text4) && list.Count > 0)
							{
								colMap = ResolveColumnMapForRowBody(dictionary2[text4], list);
							}
							else if (list.Count > 0)
							{
								colMap = list[Math.Min(num - 1, list.Count - 1)];
							}
							bool useColumnCells = PreferColumnCellRows(text, colMap);
							string emailEsc = EscapeMorkValue(contact.Email.Trim());
							mailContact = contact;
							string nameEsc = EscapeMorkValue(((mailContact.DisplayName != null) ? mailContact.DisplayName : contact.Email).Trim());
							stringBuilder.Append(BuildMorkAppendRow(text4, colMap, useColumnCells, contact, emailEsc, nameEsc));
							stringBuilder.Append(text2);
						}
					}
					File.WriteAllBytes(destPath, encoding.GetBytes(stringBuilder.ToString()));
					result = TryValidateMorkRoundTrip(contacts, destPath);
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
	}

	public static bool TryWriteMorkFullEncoderFromSource(string sourceMabPath, List<MailContact> contacts, string destPath)
	{
		LastError = null;
		checked
		{
			bool result;
			if (string.IsNullOrWhiteSpace(sourceMabPath) || !File.Exists(sourceMabPath))
			{
				LastError = "Source abook.mab not found.";
				result = false;
			}
			else if (contacts == null || string.IsNullOrWhiteSpace(destPath))
			{
				LastError = "Contacts and path required.";
				result = false;
			}
			else
			{
				try
				{
					byte[] array = File.ReadAllBytes(sourceMabPath);
					Encoding encoding = DetectMorkFileEncoding(array);
					string text = encoding.GetString(array);
					string text2 = ((text.IndexOf("\r\n", StringComparison.Ordinal) >= 0) ? "\r\n" : "\n");
					List<Dictionary<string, string>> list = ExtractPerTableColumnMaps(text);
					Dictionary<string, string> dictionary = ExtractColumnKeyMap(text);
					Dictionary<string, string> dictionary2 = ParseRawRowBodies(text);
					Dictionary<string, MailContact> dictionary3 = new Dictionary<string, MailContact>(StringComparer.OrdinalIgnoreCase);
					foreach (MailContact contact in contacts)
					{
						if (contact != null && !string.IsNullOrWhiteSpace(contact.Email))
						{
							dictionary3[contact.Email.Trim()] = contact;
						}
					}
					List<string> list2 = new List<string>();
					string[] array2 = text.Split(new string[1] { text2 }, StringSplitOptions.None);
					foreach (string text3 in array2)
					{
						string text4 = text3;
						if (text4.StartsWith("@", StringComparison.Ordinal))
						{
							int num = text4.IndexOf('=');
							if (num > 1)
							{
								string text5 = text4.Substring(1, num - 1).Trim();
								string text6 = text4.Substring(num + 1);
								MailContact mailContact = null;
								foreach (KeyValuePair<string, MailContact> item in dictionary3)
								{
									if (text6.IndexOf(item.Key, StringComparison.OrdinalIgnoreCase) >= 0)
									{
										mailContact = item.Value;
										break;
									}
								}
								if (mailContact == null && dictionary2.ContainsKey(text5))
								{
									foreach (KeyValuePair<string, MailContact> item2 in dictionary3)
									{
										if (!string.IsNullOrWhiteSpace(item2.Value.MorkRowId) && string.Equals(item2.Value.MorkRowId, text5, StringComparison.OrdinalIgnoreCase))
										{
											mailContact = item2.Value;
											break;
										}
									}
								}
								if (mailContact != null)
								{
									Dictionary<string, string> colMap = dictionary;
									if (dictionary2.ContainsKey(text5) && list.Count > 0)
									{
										colMap = ResolveColumnMapForRowBody(dictionary2[text5], list);
									}
									else if (list.Count > 0)
									{
										colMap = list[0];
									}
									bool useColumnCells = PreferColumnCellRows(text, colMap);
									string emailEsc = EscapeMorkValue(mailContact.Email.Trim());
									MailContact mailContact2 = mailContact;
									string nameEsc = EscapeMorkValue(((mailContact2.DisplayName != null) ? mailContact2.DisplayName : mailContact.Email).Trim());
									text4 = BuildMorkAppendRow(text5, colMap, useColumnCells, mailContact, emailEsc, nameEsc);
								}
							}
						}
						list2.Add(text4);
					}
					string text7 = string.Join(text2, list2.ToArray());
					if (Operators.CompareString(text2, "\r\n", TextCompare: false) == 0 && !text7.EndsWith("\r\n", StringComparison.Ordinal) && text.EndsWith("\r\n", StringComparison.Ordinal))
					{
						text7 += "\r\n";
					}
					File.WriteAllBytes(destPath, encoding.GetBytes(text7));
					if (CountMorkDataRows(text) != CountMorkDataRows(text7))
					{
						LastError = "Full encoder changed Mork row count.";
						result = false;
					}
					else
					{
						result = TryValidateMorkRoundTrip(contacts, destPath);
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
			}
			return result;
		}
	}

	private static int CountMorkDataRows(string morkText)
	{
		if (string.IsNullOrEmpty(morkText))
		{
			return 0;
		}
		int num = 0;
		Regex regex = new Regex("(?:^|[\\r\\n])@\\d+=", RegexOptions.Compiled);
		return regex.Matches(morkText).Count;
	}

	private static bool PreferColumnCellRows(string morkText, Dictionary<string, string> colMap)
	{
		if (colMap == null || colMap.Count == 0)
		{
			return false;
		}
		int num = 0;
		int num2 = 0;
		Regex regex = new Regex("@([^=]+)=([^@\\r\\n]*)", RegexOptions.Compiled);
		checked
		{
			foreach (Match item in regex.Matches((morkText != null) ? morkText : ""))
			{
				string value = item.Groups[2].Value;
				if (value.IndexOf('^') >= 0 && value.IndexOf("(d=", StringComparison.Ordinal) < 0)
				{
					num++;
				}
				else if (value.IndexOf("(d=", StringComparison.Ordinal) >= 0)
				{
					num2++;
				}
			}
			if (num == 0 && num2 == 0)
			{
				return true;
			}
			return num >= num2;
		}
	}

	private static string FindColumnId(Dictionary<string, string> colMap, string propertyName)
	{
		if (colMap == null)
		{
			return null;
		}
		foreach (KeyValuePair<string, string> item in colMap)
		{
			if (string.Equals(item.Value, propertyName, StringComparison.OrdinalIgnoreCase))
			{
				return item.Key;
			}
		}
		return null;
	}

	private static string BuildMorkAppendRow(string rowId, Dictionary<string, string> colMap, bool useColumnCells, MailContact contact, string emailEsc, string nameEsc)
	{
		if (useColumnCells && colMap != null && colMap.Count > 0)
		{
			string value = FindColumnId(colMap, "PrimaryEmail");
			string value2 = FindColumnId(colMap, "DisplayName");
			if (!string.IsNullOrEmpty(value))
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.Append('@').Append(rowId).Append("=^")
					.Append(value)
					.Append("^")
					.Append(emailEsc);
				if (!string.IsNullOrEmpty(value2))
				{
					stringBuilder.Append("^").Append(value2).Append("^")
						.Append(nameEsc);
				}
				string value3 = FindColumnId(colMap, "Company");
				if (!string.IsNullOrEmpty(value3) && contact != null && !string.IsNullOrWhiteSpace(contact.Company))
				{
					stringBuilder.Append("^").Append(value3).Append("^")
						.Append(EscapeMorkValue(contact.Company));
				}
				AppendColumnField(stringBuilder, colMap, "SecondEmail", (contact == null) ? "" : contact.SecondaryEmail);
				AppendColumnField(stringBuilder, colMap, "FirstName", (contact == null) ? "" : contact.FirstName);
				AppendColumnField(stringBuilder, colMap, "LastName", (contact == null) ? "" : contact.LastName);
				AppendColumnField(stringBuilder, colMap, "NickName", (contact == null) ? "" : contact.NickName);
				AppendColumnField(stringBuilder, colMap, "JobTitle", (contact == null) ? "" : contact.JobTitle);
				AppendColumnField(stringBuilder, colMap, "Department", (contact == null) ? "" : contact.Department);
				AppendColumnField(stringBuilder, colMap, "WebPage1", (contact == null) ? "" : contact.WebPage);
				AppendColumnField(stringBuilder, colMap, "WorkPhone", (contact == null) ? "" : contact.WorkPhone);
				AppendColumnField(stringBuilder, colMap, "HomePhone", (contact == null) ? "" : contact.HomePhone);
				AppendColumnField(stringBuilder, colMap, "FaxNumber", (contact == null) ? "" : contact.FaxNumber);
				AppendColumnField(stringBuilder, colMap, "PagerNumber", (contact == null) ? "" : contact.PagerNumber);
				AppendColumnField(stringBuilder, colMap, "Custom1", (contact == null) ? "" : contact.Custom1);
				AppendColumnField(stringBuilder, colMap, "Custom2", (contact == null) ? "" : contact.Custom2);
				AppendColumnField(stringBuilder, colMap, "Custom3", (contact == null) ? "" : contact.Custom3);
				AppendColumnField(stringBuilder, colMap, "Custom4", (contact == null) ? "" : contact.Custom4);
				AppendColumnField(stringBuilder, colMap, "SpouseName", (contact == null) ? "" : contact.SpouseName);
				AppendColumnField(stringBuilder, colMap, "Notes", (contact == null) ? "" : contact.Notes);
				AppendColumnField(stringBuilder, colMap, "CellularNumber", (contact == null) ? "" : contact.MobilePhone);
				AppendColumnField(stringBuilder, colMap, "WorkAddress", (contact == null) ? "" : contact.WorkAddress);
				AppendColumnField(stringBuilder, colMap, "HomeAddress", (contact == null) ? "" : contact.HomeAddress);
				AppendColumnField(stringBuilder, colMap, "BirthDay", (contact == null) ? "" : contact.Birthday);
				AppendColumnField(stringBuilder, colMap, "Anniversary", (contact == null) ? "" : contact.Anniversary);
				AppendColumnField(stringBuilder, colMap, "MiddleName", (contact == null) ? "" : contact.MiddleName);
				AppendColumnField(stringBuilder, colMap, "OfficeLocation", (contact == null) ? "" : contact.OfficeLocation);
				AppendColumnField(stringBuilder, colMap, "OtherPhone", (contact == null) ? "" : contact.OtherPhone);
				AppendColumnField(stringBuilder, colMap, "AssistantName", (contact == null) ? "" : contact.AssistantName);
				if (contact != null && !string.IsNullOrWhiteSpace(contact.Email))
				{
					AppendColumnField(stringBuilder, colMap, "LowercasePrimaryEmail", contact.Email.Trim().ToLowerInvariant());
				}
				AppendColumnField(stringBuilder, colMap, "ScreenName", (contact == null) ? "" : contact.ScreenName);
				AppendColumnField(stringBuilder, colMap, "AimScreenName", (contact == null) ? "" : contact.AimScreenName);
				AppendColumnField(stringBuilder, colMap, "PopularityIndex", (contact == null) ? "" : contact.PopularityIndex);
				AppendColumnField(stringBuilder, colMap, "RecordName", (contact == null) ? "" : contact.RecordName);
				AppendColumnField(stringBuilder, colMap, "PreferEmail", (contact == null) ? "" : contact.PreferEmail);
				return stringBuilder.ToString();
			}
		}
		string text = "@" + rowId + "=<" + rowId + ">(d=PrimaryEmail)>" + emailEsc + "<" + rowId + ">(d=DisplayName)>" + nameEsc;
		if (contact != null && !string.IsNullOrWhiteSpace(contact.Company))
		{
			text = text + "<" + rowId + ">(d=Company)>" + EscapeMorkValue(contact.Company);
		}
		text += AppendRowRefField(rowId, "SecondEmail", (contact == null) ? "" : contact.SecondaryEmail);
		text += AppendRowRefField(rowId, "FirstName", (contact == null) ? "" : contact.FirstName);
		text += AppendRowRefField(rowId, "LastName", (contact == null) ? "" : contact.LastName);
		text += AppendRowRefField(rowId, "NickName", (contact == null) ? "" : contact.NickName);
		text += AppendRowRefField(rowId, "JobTitle", (contact == null) ? "" : contact.JobTitle);
		text += AppendRowRefField(rowId, "Department", (contact == null) ? "" : contact.Department);
		text += AppendRowRefField(rowId, "WebPage1", (contact == null) ? "" : contact.WebPage);
		text += AppendRowRefField(rowId, "WorkPhone", (contact == null) ? "" : contact.WorkPhone);
		text += AppendRowRefField(rowId, "HomePhone", (contact == null) ? "" : contact.HomePhone);
		text += AppendRowRefField(rowId, "FaxNumber", (contact == null) ? "" : contact.FaxNumber);
		text += AppendRowRefField(rowId, "PagerNumber", (contact == null) ? "" : contact.PagerNumber);
		text += AppendRowRefField(rowId, "Custom1", (contact == null) ? "" : contact.Custom1);
		text += AppendRowRefField(rowId, "Custom2", (contact == null) ? "" : contact.Custom2);
		text += AppendRowRefField(rowId, "Custom3", (contact == null) ? "" : contact.Custom3);
		text += AppendRowRefField(rowId, "Custom4", (contact == null) ? "" : contact.Custom4);
		text += AppendRowRefField(rowId, "SpouseName", (contact == null) ? "" : contact.SpouseName);
		text += AppendRowRefField(rowId, "Notes", (contact == null) ? "" : contact.Notes);
		text += AppendRowRefField(rowId, "CellularNumber", (contact == null) ? "" : contact.MobilePhone);
		text += AppendRowRefField(rowId, "WorkAddress", (contact == null) ? "" : contact.WorkAddress);
		text += AppendRowRefField(rowId, "HomeAddress", (contact == null) ? "" : contact.HomeAddress);
		text += AppendRowRefField(rowId, "BirthDay", (contact == null) ? "" : contact.Birthday);
		text += AppendRowRefField(rowId, "Anniversary", (contact == null) ? "" : contact.Anniversary);
		text += AppendRowRefField(rowId, "MiddleName", (contact == null) ? "" : contact.MiddleName);
		text += AppendRowRefField(rowId, "OfficeLocation", (contact == null) ? "" : contact.OfficeLocation);
		text += AppendRowRefField(rowId, "OtherPhone", (contact == null) ? "" : contact.OtherPhone);
		text += AppendRowRefField(rowId, "AssistantName", (contact == null) ? "" : contact.AssistantName);
		if (contact != null && !string.IsNullOrWhiteSpace(contact.Email))
		{
			text += AppendRowRefField(rowId, "LowercasePrimaryEmail", contact.Email.Trim().ToLowerInvariant());
		}
		text += AppendRowRefField(rowId, "ScreenName", (contact == null) ? "" : contact.ScreenName);
		text += AppendRowRefField(rowId, "AimScreenName", (contact == null) ? "" : contact.AimScreenName);
		text += AppendRowRefField(rowId, "PopularityIndex", (contact == null) ? "" : contact.PopularityIndex);
		text += AppendRowRefField(rowId, "RecordName", (contact == null) ? "" : contact.RecordName);
		return text + AppendRowRefField(rowId, "PreferEmail", (contact == null) ? "" : contact.PreferEmail);
	}

	private static string TryInsertMorkRowBeforeTableEnd(string morkText, string rowLine, string lineBreak, Dictionary<string, string> colMap = null)
	{
		if (string.IsNullOrEmpty(morkText) || string.IsNullOrEmpty(rowLine))
		{
			return morkText;
		}
		int num = FindPrimaryEmailTableEndIndex(morkText, colMap);
		if (num <= 0)
		{
			num = morkText.LastIndexOf("(end", StringComparison.OrdinalIgnoreCase);
		}
		if (num <= 0)
		{
			return morkText + lineBreak + rowLine;
		}
		return morkText.Substring(0, num) + rowLine + lineBreak + morkText.Substring(num);
	}

	private static int FindPrimaryEmailTableEndIndex(string morkText, Dictionary<string, string> colMap)
	{
		if (string.IsNullOrEmpty(morkText))
		{
			return -1;
		}
		List<Dictionary<string, string>> tableMaps = ExtractPerTableColumnMaps(morkText);
		Dictionary<string, string> dictionary = colMap;
		if (dictionary == null || FindColumnId(dictionary, "PrimaryEmail").Length == 0)
		{
			dictionary = PickPrimaryEmailTableMap(tableMaps);
		}
		if (dictionary == null || FindColumnId(dictionary, "PrimaryEmail").Length == 0)
		{
			return -1;
		}
		Regex regex = new Regex("\\(k=([^\\)]*)\\)", RegexOptions.Compiled);
		checked
		{
			foreach (Match item in regex.Matches(morkText))
			{
				Dictionary<string, string> dictionary2 = ParseColumnKeyChunk(item.Groups[1].Value);
				if (dictionary2 == null || FindColumnId(dictionary2, "PrimaryEmail").Length == 0 || dictionary2.Count != dictionary.Count)
				{
					continue;
				}
				bool flag = true;
				foreach (KeyValuePair<string, string> item2 in dictionary)
				{
					string value = null;
					if (!dictionary2.TryGetValue(item2.Key, out value) || !string.Equals(value, item2.Value, StringComparison.OrdinalIgnoreCase))
					{
						flag = false;
						break;
					}
				}
				if (flag)
				{
					int num = morkText.IndexOf("(end", item.Index + item.Length, StringComparison.OrdinalIgnoreCase);
					if (num > 0)
					{
						return num;
					}
				}
			}
			foreach (Match item3 in regex.Matches(morkText))
			{
				Dictionary<string, string> dictionary3 = ParseColumnKeyChunk(item3.Groups[1].Value);
				if (dictionary3 != null && FindColumnId(dictionary3, "PrimaryEmail").Length != 0)
				{
					int num2 = morkText.IndexOf("(end", item3.Index + item3.Length, StringComparison.OrdinalIgnoreCase);
					if (num2 > 0)
					{
						return num2;
					}
				}
			}
			return -1;
		}
	}

	private static string TryRemoveOrphanMorkRowsFromRawBodies(string morkText, Dictionary<string, string> rawRows, HashSet<string> wantedEmails, string lineBreak)
	{
		if (string.IsNullOrEmpty(morkText) || rawRows == null || rawRows.Count == 0 || wantedEmails == null)
		{
			return morkText;
		}
		string text = morkText;
		checked
		{
			foreach (KeyValuePair<string, string> rawRow in rawRows)
			{
				string key = rawRow.Key;
				string value = rawRow.Value;
				string text2 = ((value != null) ? value : "");
				if (text2.Length == 0)
				{
					continue;
				}
				string text3 = ExtractEmailFromMorkRowBody(text2);
				if (text3.Length != 0 && !wantedEmails.Contains(text3))
				{
					string value2 = "@" + key + "=";
					int num = text.IndexOf(value2, StringComparison.Ordinal);
					if (num >= 0)
					{
						int num2 = text.IndexOf(lineBreak, num);
						num2 = ((num2 >= 0) ? (num2 + lineBreak.Length) : text.Length);
						text = text.Remove(num, num2 - num);
					}
				}
			}
			return text;
		}
	}

	private static string ExtractEmailFromMorkRowBody(string rowBody)
	{
		string input = ((rowBody != null) ? rowBody : "");
		foreach (Match item in EmailPattern.Matches(input))
		{
			if (item.Success)
			{
				return item.Value.Trim();
			}
		}
		return "";
	}

	private static void AppendColumnField(StringBuilder sb, Dictionary<string, string> colMap, string propertyName, string rawValue)
	{
		if (sb == null || colMap == null)
		{
			return;
		}
		string text = ((rawValue != null) ? rawValue : "").Trim();
		if (text.Length != 0)
		{
			string value = FindColumnId(colMap, propertyName);
			if (!string.IsNullOrEmpty(value))
			{
				sb.Append("^").Append(value).Append("^")
					.Append(EscapeMorkValue(text));
			}
		}
	}

	private static string AppendRowRefField(string rowId, string prop, string rawValue)
	{
		string text = ((rawValue != null) ? rawValue : "").Trim();
		if (text.Length == 0)
		{
			return "";
		}
		return "<" + rowId + ">(d=" + prop + ")>" + EscapeMorkValue(text);
	}

	private static string FindMorkRowIdForEmail(string morkText, string email)
	{
		string text = ((email != null) ? email : "").Trim();
		if (text.Length == 0 || string.IsNullOrEmpty(morkText))
		{
			return "";
		}
		Dictionary<string, string> dictionary = ExtractPropertyMap(morkText, "PrimaryEmail");
		foreach (KeyValuePair<string, string> item in dictionary)
		{
			if (string.Equals(item.Value, text, StringComparison.OrdinalIgnoreCase))
			{
				return item.Key;
			}
		}
		Dictionary<string, string> dictionary2 = ExtractColumnKeyMap(morkText);
		if (dictionary2.Count > 0)
		{
			Dictionary<string, string> dictionary3 = ExtractPropertyMapViaColumns(morkText, dictionary2, "PrimaryEmail");
			foreach (KeyValuePair<string, string> item2 in dictionary3)
			{
				if (string.Equals(item2.Value, text, StringComparison.OrdinalIgnoreCase))
				{
					return item2.Key;
				}
			}
		}
		return "";
	}

	private static string TryRemoveOrphanMorkRows(string morkText, HashSet<string> wantedEmails, string lineBreak)
	{
		if (string.IsNullOrEmpty(morkText) || wantedEmails == null)
		{
			return morkText;
		}
		Dictionary<string, string> dictionary = ExtractPropertyMap(morkText, "PrimaryEmail");
		Dictionary<string, string> dictionary2 = ExtractColumnKeyMap(morkText);
		if (dictionary.Count == 0 && dictionary2.Count > 0)
		{
			dictionary = ExtractPropertyMapViaColumns(morkText, dictionary2, "PrimaryEmail");
		}
		string text = morkText;
		checked
		{
			foreach (KeyValuePair<string, string> item in dictionary)
			{
				string value = item.Value;
				string text2 = ((value != null) ? value : "").Trim();
				if (text2.Length != 0 && !wantedEmails.Contains(text2))
				{
					string key = item.Key;
					string value2 = "@" + key + "=";
					int num = text.IndexOf(value2, StringComparison.Ordinal);
					if (num >= 0)
					{
						int num2 = text.IndexOf(lineBreak, num);
						num2 = ((num2 >= 0) ? (num2 + lineBreak.Length) : text.Length);
						text = text.Remove(num, num2 - num);
					}
				}
			}
			return text;
		}
	}

	private static string TryReplaceMorkRowLine(string morkText, string rowId, Dictionary<string, string> colMap, bool useColumnCells, MailContact contact, string emailEsc, string nameEsc, string lineBreak)
	{
		if (string.IsNullOrEmpty(morkText) || string.IsNullOrWhiteSpace(rowId))
		{
			return "";
		}
		string value = "@" + rowId + "=";
		int num = morkText.IndexOf(value, StringComparison.Ordinal);
		if (num < 0)
		{
			return "";
		}
		int num2 = morkText.IndexOf(lineBreak, num);
		if (num2 < 0)
		{
			num2 = morkText.Length;
		}
		string oldLine = morkText.Substring(num, checked(num2 - num));
		string newLine = BuildMorkAppendRow(rowId, colMap, useColumnCells, contact, emailEsc, nameEsc);
		newLine = AppendPreservedUnknownColumnCells(oldLine, newLine, colMap);
		return morkText.Substring(0, num) + newLine + morkText.Substring(num2);
	}

	private static bool IsKnownMorkExportProperty(string propertyName)
	{
		string text = ((propertyName != null) ? propertyName : "").Trim();
		if (text.Length == 0)
		{
			return false;
		}
		string text2 = text.ToLowerInvariant();
		if (Operators.CompareString(text2, "primaryemail", TextCompare: false) != 0 && Operators.CompareString(text2, "secondemail", TextCompare: false) != 0 && Operators.CompareString(text2, "displayname", TextCompare: false) != 0 && Operators.CompareString(text2, "firstname", TextCompare: false) != 0 && Operators.CompareString(text2, "lastname", TextCompare: false) != 0 && Operators.CompareString(text2, "nickname", TextCompare: false) != 0 && Operators.CompareString(text2, "jobtitle", TextCompare: false) != 0 && Operators.CompareString(text2, "department", TextCompare: false) != 0 && Operators.CompareString(text2, "webpage1", TextCompare: false) != 0 && Operators.CompareString(text2, "company", TextCompare: false) != 0 && Operators.CompareString(text2, "workphone", TextCompare: false) != 0 && Operators.CompareString(text2, "homephone", TextCompare: false) != 0 && Operators.CompareString(text2, "faxnumber", TextCompare: false) != 0 && Operators.CompareString(text2, "pagernumber", TextCompare: false) != 0 && Operators.CompareString(text2, "custom1", TextCompare: false) != 0 && Operators.CompareString(text2, "custom2", TextCompare: false) != 0 && Operators.CompareString(text2, "custom3", TextCompare: false) != 0 && Operators.CompareString(text2, "custom4", TextCompare: false) != 0 && Operators.CompareString(text2, "spousename", TextCompare: false) != 0 && Operators.CompareString(text2, "notes", TextCompare: false) != 0 && Operators.CompareString(text2, "cellularnumber", TextCompare: false) != 0 && Operators.CompareString(text2, "workaddress", TextCompare: false) != 0 && Operators.CompareString(text2, "homeaddress", TextCompare: false) != 0 && Operators.CompareString(text2, "birthday", TextCompare: false) != 0 && Operators.CompareString(text2, "anniversary", TextCompare: false) != 0 && Operators.CompareString(text2, "middlename", TextCompare: false) != 0 && Operators.CompareString(text2, "officelocation", TextCompare: false) != 0 && Operators.CompareString(text2, "otherphone", TextCompare: false) != 0 && Operators.CompareString(text2, "assistantname", TextCompare: false) != 0)
		{
			switch (text2)
			{
			case "lowercaseprimaryemail":
			case "screenname":
			case "aimscreenname":
			case "popularityindex":
			case "recordname":
			case "preferemail":
			case "birthday":
			case "anniversary":
			case "workaddress":
			case "homeaddress":
			case "middlename":
			case "officelocation":
			case "otherphone":
			case "assistantname":
				break;
			default:
				return false;
			}
		}
		return true;
	}

	private static string AppendPreservedUnknownColumnCells(string oldLine, string newLine, Dictionary<string, string> colMap)
	{
		if (string.IsNullOrEmpty(oldLine) || string.IsNullOrEmpty(newLine))
		{
			return newLine;
		}
		if (!oldLine.StartsWith("@", StringComparison.Ordinal) || !newLine.StartsWith("@", StringComparison.Ordinal))
		{
			return newLine;
		}
		int num = oldLine.IndexOf('=');
		if (num < 0)
		{
			return newLine;
		}
		checked
		{
			string text = oldLine.Substring(num + 1).Trim();
			if (text.Length == 0 || text.StartsWith("<"))
			{
				return newLine;
			}
			string[] array = text.Split('^');
			if (array.Length < 2)
			{
				return newLine;
			}
			StringBuilder stringBuilder = new StringBuilder(newLine);
			int num2 = 0;
			while (num2 < array.Length - 1)
			{
				string text2 = array[num2].Trim();
				string value = array[num2 + 1];
				num2 += 2;
				if (text2.Length != 0 && Regex.IsMatch(text2, "^[0-9A-Fa-f]+$") && newLine.IndexOf("^" + text2 + "^", StringComparison.Ordinal) < 0)
				{
					stringBuilder.Append("^").Append(text2).Append("^")
						.Append(value);
				}
			}
			return stringBuilder.ToString();
		}
	}

	private static int FindMaxMorkRowId(string morkText)
	{
		int num = 0;
		if (string.IsNullOrEmpty(morkText))
		{
			return num;
		}
		Regex regex = new Regex("@(\\d+)=", RegexOptions.Compiled);
		foreach (Match item in regex.Matches(morkText))
		{
			int result = 0;
			if (int.TryParse(item.Groups[1].Value, out result) && result > num)
			{
				num = result;
			}
		}
		return num;
	}

	private static bool StructuralCompareBytes(byte[] a, byte[] b)
	{
		if (a == null || b == null)
		{
			return false;
		}
		if (a.Length != b.Length)
		{
			return false;
		}
		checked
		{
			int num = a.Length - 1;
			for (int i = 0; i <= num; i++)
			{
				if (a[i] != b[i])
				{
					return false;
				}
			}
			return true;
		}
	}

	private static string EscapeMorkValue(string raw)
	{
		string text = ((raw != null) ? raw : "");
		return text.Replace(" ", "$20").Replace("@", "$40").Replace(",", "$2C")
			.Replace("(", "$28")
			.Replace(")", "$29");
	}

	private static bool TryValidateMorkRoundTrip(List<MailContact> source, string morkPath)
	{
		LastError = null;
		List<MailContact> list = TryParseContacts(morkPath);
		if (list == null || list.Count == 0)
		{
			LastError = "Mork export failed round-trip parse.";
			return false;
		}
		int num = 0;
		foreach (MailContact item in source)
		{
			if (item != null && !string.IsNullOrWhiteSpace(item.Email))
			{
				num = checked(num + 1);
			}
		}
		if (list.Count < num)
		{
			LastError = "Mork round-trip lost contacts (" + list.Count + " vs " + num + ").";
			return false;
		}
		foreach (MailContact item2 in source)
		{
			if (item2 == null || string.IsNullOrWhiteSpace(item2.Email))
			{
				continue;
			}
			MailContact mailContact = null;
			foreach (MailContact item3 in list)
			{
				if (item3 != null && string.Equals(item3.Email, item2.Email, StringComparison.OrdinalIgnoreCase))
				{
					mailContact = item3;
					break;
				}
			}
			if (mailContact == null)
			{
				LastError = "Mork round-trip missing " + item2.Email;
				return false;
			}
			MailContact mailContact2 = item2;
			string text = ((mailContact2.PreferEmail != null) ? mailContact2.PreferEmail : "").Trim();
			if (text.Length > 0)
			{
				mailContact2 = mailContact;
				string b = ((mailContact2.PreferEmail != null) ? mailContact2.PreferEmail : "").Trim();
				if (!string.Equals(text, b, StringComparison.OrdinalIgnoreCase))
				{
					LastError = "Mork round-trip PreferEmail mismatch for " + item2.Email;
					return false;
				}
			}
			mailContact2 = item2;
			string text2 = ((mailContact2.SecondaryEmail != null) ? mailContact2.SecondaryEmail : "").Trim();
			if (text2.Length > 0)
			{
				mailContact2 = mailContact;
				string b2 = ((mailContact2.SecondaryEmail != null) ? mailContact2.SecondaryEmail : "").Trim();
				if (!string.Equals(text2, b2, StringComparison.OrdinalIgnoreCase))
				{
					LastError = "Mork round-trip SecondEmail mismatch for " + item2.Email;
					return false;
				}
			}
			mailContact2 = item2;
			string text3 = ((mailContact2.MorkRowId != null) ? mailContact2.MorkRowId : "").Trim();
			if (text3.Length > 0)
			{
				mailContact2 = mailContact;
				string b3 = ((mailContact2.MorkRowId != null) ? mailContact2.MorkRowId : "").Trim();
				if (!string.Equals(text3, b3, StringComparison.OrdinalIgnoreCase))
				{
					LastError = "Mork round-trip MorkRowId mismatch for " + item2.Email;
					return false;
				}
			}
			mailContact2 = item2;
			string text4 = ((mailContact2.MobilePhone != null) ? mailContact2.MobilePhone : "").Trim();
			if (text4.Length > 0)
			{
				mailContact2 = mailContact;
				string b4 = ((mailContact2.MobilePhone != null) ? mailContact2.MobilePhone : "").Trim();
				if (!string.Equals(text4, b4, StringComparison.OrdinalIgnoreCase))
				{
					LastError = "Mork round-trip CellularNumber mismatch for " + item2.Email;
					return false;
				}
			}
			mailContact2 = item2;
			string text5 = ((mailContact2.ScreenName != null) ? mailContact2.ScreenName : "").Trim();
			if (text5.Length > 0)
			{
				mailContact2 = mailContact;
				string b5 = ((mailContact2.ScreenName != null) ? mailContact2.ScreenName : "").Trim();
				if (!string.Equals(text5, b5, StringComparison.OrdinalIgnoreCase))
				{
					LastError = "Mork round-trip ScreenName mismatch for " + item2.Email;
					return false;
				}
			}
			mailContact2 = item2;
			string text6 = ((mailContact2.JobTitle != null) ? mailContact2.JobTitle : "").Trim();
			if (text6.Length > 0)
			{
				mailContact2 = mailContact;
				string b6 = ((mailContact2.JobTitle != null) ? mailContact2.JobTitle : "").Trim();
				if (!string.Equals(text6, b6, StringComparison.OrdinalIgnoreCase))
				{
					LastError = "Mork round-trip JobTitle mismatch for " + item2.Email;
					return false;
				}
			}
			mailContact2 = item2;
			string text7 = ((mailContact2.NickName != null) ? mailContact2.NickName : "").Trim();
			if (text7.Length > 0)
			{
				mailContact2 = mailContact;
				string b7 = ((mailContact2.NickName != null) ? mailContact2.NickName : "").Trim();
				if (!string.Equals(text7, b7, StringComparison.OrdinalIgnoreCase))
				{
					LastError = "Mork round-trip NickName mismatch for " + item2.Email;
					return false;
				}
			}
			mailContact2 = item2;
			string text8 = ((mailContact2.AimScreenName != null) ? mailContact2.AimScreenName : "").Trim();
			if (text8.Length > 0)
			{
				mailContact2 = mailContact;
				string b8 = ((mailContact2.AimScreenName != null) ? mailContact2.AimScreenName : "").Trim();
				if (!string.Equals(text8, b8, StringComparison.OrdinalIgnoreCase))
				{
					LastError = "Mork round-trip AimScreenName mismatch for " + item2.Email;
					return false;
				}
			}
			if (!ValidateOptionalMorkFieldMatch(item2.Company, mailContact.Company, "Company", item2.Email))
			{
				return false;
			}
			if (!ValidateOptionalMorkFieldMatch(item2.Department, mailContact.Department, "Department", item2.Email))
			{
				return false;
			}
			if (!ValidateOptionalMorkFieldMatch(item2.WorkPhone, mailContact.WorkPhone, "WorkPhone", item2.Email))
			{
				return false;
			}
			if (!ValidateOptionalMorkFieldMatch(item2.PopularityIndex, mailContact.PopularityIndex, "PopularityIndex", item2.Email))
			{
				return false;
			}
			if (!ValidateOptionalMorkFieldMatch(item2.RecordName, mailContact.RecordName, "RecordName", item2.Email))
			{
				return false;
			}
			if (!ValidateOptionalMorkFieldMatch(item2.HomePhone, mailContact.HomePhone, "HomePhone", item2.Email))
			{
				return false;
			}
			if (!ValidateOptionalMorkFieldMatch(item2.FaxNumber, mailContact.FaxNumber, "FaxNumber", item2.Email))
			{
				return false;
			}
			if (!ValidateOptionalMorkFieldMatch(item2.PagerNumber, mailContact.PagerNumber, "PagerNumber", item2.Email))
			{
				return false;
			}
			if (!ValidateOptionalMorkFieldMatch(item2.WebPage, mailContact.WebPage, "WebPage", item2.Email))
			{
				return false;
			}
			if (!ValidateOptionalMorkFieldMatch(item2.SpouseName, mailContact.SpouseName, "SpouseName", item2.Email))
			{
				return false;
			}
			if (!ValidateOptionalMorkFieldMatch(item2.Anniversary, mailContact.Anniversary, "Anniversary", item2.Email))
			{
				return false;
			}
			if (!ValidateOptionalMorkFieldMatch(item2.Birthday, mailContact.Birthday, "Birthday", item2.Email))
			{
				return false;
			}
			if (!ValidateOptionalMorkFieldMatch(item2.Custom1, mailContact.Custom1, "Custom1", item2.Email))
			{
				return false;
			}
			if (!ValidateOptionalMorkFieldMatch(item2.Custom2, mailContact.Custom2, "Custom2", item2.Email))
			{
				return false;
			}
			if (!ValidateOptionalMorkFieldMatch(item2.Custom3, mailContact.Custom3, "Custom3", item2.Email))
			{
				return false;
			}
			if (!ValidateOptionalMorkFieldMatch(item2.Custom4, mailContact.Custom4, "Custom4", item2.Email))
			{
				return false;
			}
			if (!ValidateOptionalMorkFieldMatch(item2.Notes, mailContact.Notes, "Notes", item2.Email))
			{
				return false;
			}
			if (!ValidateOptionalMorkFieldMatch(item2.OtherPhone, mailContact.OtherPhone, "OtherPhone", item2.Email))
			{
				return false;
			}
			if (!ValidateOptionalMorkFieldMatch(item2.WorkAddress, mailContact.WorkAddress, "WorkAddress", item2.Email))
			{
				return false;
			}
			if (!ValidateOptionalMorkFieldMatch(item2.HomeAddress, mailContact.HomeAddress, "HomeAddress", item2.Email))
			{
				return false;
			}
			if (!ValidateOptionalMorkFieldMatch(item2.DisplayName, mailContact.DisplayName, "DisplayName", item2.Email))
			{
				return false;
			}
			if (!ValidateOptionalMorkFieldMatch(item2.FirstName, mailContact.FirstName, "FirstName", item2.Email))
			{
				return false;
			}
			if (!ValidateOptionalMorkFieldMatch(item2.LastName, mailContact.LastName, "LastName", item2.Email))
			{
				return false;
			}
			if (!ValidateOptionalMorkFieldMatch(item2.MiddleName, mailContact.MiddleName, "MiddleName", item2.Email))
			{
				return false;
			}
			if (!ValidateOptionalMorkFieldMatch(item2.OfficeLocation, mailContact.OfficeLocation, "OfficeLocation", item2.Email))
			{
				return false;
			}
			if (!ValidateOptionalMorkFieldMatch(item2.AssistantName, mailContact.AssistantName, "AssistantName", item2.Email))
			{
				return false;
			}
			string text9 = item2.Email.Trim().ToLowerInvariant();
			if (text9.Length > 0)
			{
				mailContact2 = mailContact;
				string b9 = ((mailContact2.Email != null) ? mailContact2.Email : "").Trim().ToLowerInvariant();
				if (!string.Equals(text9, b9, StringComparison.Ordinal))
				{
					LastError = "Mork round-trip LowercasePrimaryEmail mismatch for " + item2.Email;
					return false;
				}
			}
		}
		return true;
	}

	private static bool ValidateOptionalMorkFieldMatch(string sourceValue, string roundTripValue, string fieldLabel, string email)
	{
		string text = ((sourceValue != null) ? sourceValue : "").Trim();
		if (text.Length == 0)
		{
			return true;
		}
		string b = ((roundTripValue != null) ? roundTripValue : "").Trim();
		if (string.Equals(text, b, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		LastError = "Mork round-trip " + fieldLabel + " mismatch for " + email;
		return false;
	}

	public static bool TryVerifyExportBytesMatchSource(string sourceMabPath, string exportedMabPath)
	{
		LastMabExportVerifiedAgainstSource = false;
		bool result;
		if (string.IsNullOrWhiteSpace(sourceMabPath) || !File.Exists(sourceMabPath))
		{
			result = false;
		}
		else if (string.IsNullOrWhiteSpace(exportedMabPath) || !File.Exists(exportedMabPath))
		{
			result = false;
		}
		else
		{
			try
			{
				byte[] a = File.ReadAllBytes(sourceMabPath);
				byte[] b = File.ReadAllBytes(exportedMabPath);
				LastMabExportVerifiedAgainstSource = StructuralCompareBytes(a, b);
				result = LastMabExportVerifiedAgainstSource;
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

	public static bool TryExportByteIdenticalSourceCopy(string sourceMabPath, List<MailContact> contacts, string destPath)
	{
		LastError = null;
		LastMabExportByteIdentical = false;
		bool result;
		if (string.IsNullOrWhiteSpace(sourceMabPath) || !File.Exists(sourceMabPath))
		{
			result = false;
		}
		else if (!TryContactsUnchangedVersusSource(sourceMabPath, contacts))
		{
			result = false;
		}
		else
		{
			try
			{
				File.Copy(sourceMabPath, destPath, overwrite: true);
				LastMabExportByteIdentical = true;
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

	public static bool TryContactsUnchangedVersusSource(string sourceMabPath, List<MailContact> contacts)
	{
		if (string.IsNullOrWhiteSpace(sourceMabPath) || !File.Exists(sourceMabPath))
		{
			return false;
		}
		List<MailContact> list = TryParseContacts(sourceMabPath);
		if (list == null)
		{
			return false;
		}
		if (contacts == null)
		{
			return list.Count == 0;
		}
		Dictionary<string, MailContact> dictionary = new Dictionary<string, MailContact>(StringComparer.OrdinalIgnoreCase);
		foreach (MailContact item in list)
		{
			if (item != null && !string.IsNullOrWhiteSpace(item.Email))
			{
				dictionary[item.Email.Trim()] = item;
			}
		}
		int num = 0;
		foreach (MailContact contact in contacts)
		{
			if (contact != null && !string.IsNullOrWhiteSpace(contact.Email))
			{
				num = checked(num + 1);
				string key = contact.Email.Trim();
				if (!dictionary.ContainsKey(key))
				{
					return false;
				}
				if (!ContactExportFieldsEqual(contact, dictionary[key]))
				{
					return false;
				}
			}
		}
		return num == dictionary.Count;
	}

	private static bool ContactExportFieldsEqual(MailContact a, MailContact b)
	{
		if (a == null || b == null)
		{
			return false;
		}
		return (string.Equals(Norm(a.DisplayName), Norm(b.DisplayName), StringComparison.Ordinal) && string.Equals(Norm(a.FirstName), Norm(b.FirstName), StringComparison.Ordinal) && string.Equals(Norm(a.LastName), Norm(b.LastName), StringComparison.Ordinal) && string.Equals(Norm(a.Company), Norm(b.Company), StringComparison.Ordinal) && string.Equals(Norm(a.WorkPhone), Norm(b.WorkPhone), StringComparison.Ordinal) && string.Equals(Norm(a.MobilePhone), Norm(b.MobilePhone), StringComparison.Ordinal) && string.Equals(Norm(a.JobTitle), Norm(b.JobTitle), StringComparison.Ordinal) && string.Equals(Norm(a.Department), Norm(b.Department), StringComparison.Ordinal) && string.Equals(Norm(a.SecondaryEmail), Norm(b.SecondaryEmail), StringComparison.Ordinal) && string.Equals(Norm(a.NickName), Norm(b.NickName), StringComparison.Ordinal) && string.Equals(Norm(a.HomePhone), Norm(b.HomePhone), StringComparison.Ordinal) && string.Equals(Norm(a.Birthday), Norm(b.Birthday), StringComparison.Ordinal) && string.Equals(Norm(a.Notes), Norm(b.Notes), StringComparison.Ordinal) && string.Equals(Norm(a.WebPage), Norm(b.WebPage), StringComparison.Ordinal) && string.Equals(Norm(a.FaxNumber), Norm(b.FaxNumber), StringComparison.Ordinal) && string.Equals(Norm(a.AssistantName), Norm(b.AssistantName), StringComparison.Ordinal) && string.Equals(Norm(a.OfficeLocation), Norm(b.OfficeLocation), StringComparison.Ordinal) && string.Equals(Norm(a.MiddleName), Norm(b.MiddleName), StringComparison.Ordinal) && string.Equals(Norm(a.HomeAddress), Norm(b.HomeAddress), StringComparison.Ordinal) && string.Equals(Norm(a.WorkAddress), Norm(b.WorkAddress), StringComparison.Ordinal) && string.Equals(Norm(a.SpouseName), Norm(b.SpouseName), StringComparison.Ordinal) && string.Equals(Norm(a.Anniversary), Norm(b.Anniversary), StringComparison.Ordinal) && string.Equals(Norm(a.Custom1), Norm(b.Custom1), StringComparison.Ordinal) && string.Equals(Norm(a.PagerNumber), Norm(b.PagerNumber), StringComparison.Ordinal) && string.Equals(Norm(a.Custom2), Norm(b.Custom2), StringComparison.Ordinal) && string.Equals(Norm(a.Custom3), Norm(b.Custom3), StringComparison.Ordinal) && string.Equals(Norm(a.Custom4), Norm(b.Custom4), StringComparison.Ordinal) && string.Equals(Norm(a.ScreenName), Norm(b.ScreenName), StringComparison.Ordinal)) ? true : false;
	}

	private static string Norm(string s)
	{
		return ((s != null) ? s : "").Trim();
	}

	public static bool TryAssertExportEmailSetMatchesSource(string sourceMabPath, string exportedMabPath)
	{
		LastMabExportEmailSetMatches = false;
		if (string.IsNullOrWhiteSpace(sourceMabPath) || !File.Exists(sourceMabPath))
		{
			return false;
		}
		if (string.IsNullOrWhiteSpace(exportedMabPath) || !File.Exists(exportedMabPath))
		{
			return false;
		}
		List<MailContact> list = TryParseContacts(sourceMabPath);
		List<MailContact> list2 = TryParseContacts(exportedMabPath);
		if (list == null || list2 == null)
		{
			return false;
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (MailContact item in list)
		{
			if (item != null && !string.IsNullOrWhiteSpace(item.Email))
			{
				hashSet.Add(item.Email.Trim());
			}
		}
		HashSet<string> hashSet2 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (MailContact item2 in list2)
		{
			if (item2 != null && !string.IsNullOrWhiteSpace(item2.Email))
			{
				hashSet2.Add(item2.Email.Trim());
			}
		}
		LastMabExportEmailSetMatches = ((hashSet.Count == hashSet2.Count && hashSet.SetEquals(hashSet2)) ? true : false);
		return LastMabExportEmailSetMatches;
	}

	public static bool TryAssertExportContactFieldsMatchSource(string sourceMabPath, string exportedMabPath)
	{
		LastMabExportFieldsMatch = false;
		if (string.IsNullOrWhiteSpace(sourceMabPath) || !File.Exists(sourceMabPath))
		{
			return false;
		}
		if (string.IsNullOrWhiteSpace(exportedMabPath) || !File.Exists(exportedMabPath))
		{
			return false;
		}
		List<MailContact> list = TryParseContacts(sourceMabPath);
		List<MailContact> list2 = TryParseContacts(exportedMabPath);
		if (list == null || list2 == null)
		{
			return false;
		}
		Dictionary<string, MailContact> dictionary = new Dictionary<string, MailContact>(StringComparer.OrdinalIgnoreCase);
		foreach (MailContact item in list)
		{
			if (item != null && !string.IsNullOrWhiteSpace(item.Email))
			{
				dictionary[item.Email.Trim()] = item;
			}
		}
		int num = 0;
		foreach (MailContact item2 in list2)
		{
			if (item2 != null && !string.IsNullOrWhiteSpace(item2.Email))
			{
				string key = item2.Email.Trim();
				if (!dictionary.ContainsKey(key))
				{
					return false;
				}
				if (!ContactExportFieldsEqual(item2, dictionary[key]))
				{
					return false;
				}
				num = checked(num + 1);
			}
		}
		LastMabExportFieldsMatch = num == dictionary.Count;
		return LastMabExportFieldsMatch;
	}

	public static bool TryValidateSourceMabByteRoundTrip(string sourceMabPath)
	{
		LastError = null;
		LastSourceMabRoundTripValidated = false;
		LastSourceMabRoundTripByteIdentical = false;
		LastValidatedSourceMabPath = ((sourceMabPath != null) ? sourceMabPath : "").Trim();
		if (string.IsNullOrWhiteSpace(sourceMabPath) || !File.Exists(sourceMabPath))
		{
			LastError = "User abook.mab source path missing.";
			return false;
		}
		List<MailContact> list = TryParseContacts(sourceMabPath);
		if (list == null)
		{
			return false;
		}
		string text = Path.Combine(Path.GetTempPath(), "lcars-mork-preserve-" + Guid.NewGuid().ToString("N") + ".mab");
		string text2 = Path.Combine(Path.GetTempPath(), "lcars-mork-full-" + Guid.NewGuid().ToString("N") + ".mab");
		try
		{
			bool byteIdentical = false;
			if (!TryWriteMorkPreservingSource(sourceMabPath, list, text, ref byteIdentical))
			{
				return false;
			}
			LastSourceMabRoundTripByteIdentical = byteIdentical;
			if (!TryWriteMorkFullEncoderFromSource(sourceMabPath, list, text2))
			{
				return false;
			}
			if (!TryValidateMorkRoundTrip(list, text2))
			{
				return false;
			}
			LastSourceMabRoundTripValidated = true;
			return true;
		}
		finally
		{
			try
			{
				if (File.Exists(text))
				{
					File.Delete(text);
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
			try
			{
				if (File.Exists(text2))
				{
					File.Delete(text2);
				}
			}
			catch (Exception projectError2)
			{
				ProjectData.SetProjectError(projectError2);
				ProjectData.ClearProjectError();
			}
		}
	}

	public static bool TryValidateThunderbirdFixture(ref int contactCount)
	{
		LastError = null;
		contactCount = 0;
		string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestFixtures");
		string text = Path.Combine(path, "thunderbird-abook-sample.mork");
		string text2 = Path.Combine(path, "thunderbird-abook-golden.mork");
		string text3 = Path.Combine(path, "thunderbird-abook-profile.mork");
		string text4 = Path.Combine(path, "thunderbird-abook-mixed.mork");
		string text5 = Path.Combine(path, "thunderbird-abook-phones.mork");
		string text6 = Path.Combine(path, "thunderbird-abook-rowref.mork");
		string text7 = Path.Combine(path, "thunderbird-abook-company.mork");
		string text8 = Path.Combine(path, "thunderbird-abook-nick.mork");
		string text9 = Path.Combine(path, "thunderbird-abook-jobtitle.mork");
		string text10 = Path.Combine(path, "thunderbird-abook-department.mork");
		string text11 = Path.Combine(path, "thunderbird-abook-webpage.mork");
		string text12 = Path.Combine(path, "thunderbird-abook-fax.mork");
		string text13 = Path.Combine(path, "thunderbird-abook-pager.mork");
		string text14 = Path.Combine(path, "thunderbird-abook-custom1.mork");
		string text15 = Path.Combine(path, "thunderbird-abook-custom2.mork");
		string text16 = Path.Combine(path, "thunderbird-abook-custom3.mork");
		string text17 = Path.Combine(path, "thunderbird-abook-custom4.mork");
		string text18 = Path.Combine(path, "thunderbird-abook-spouse.mork");
		string text19 = Path.Combine(path, "thunderbird-abook-multitable.mork");
		if (!File.Exists(text))
		{
			LastError = "Missing thunderbird-abook-sample.mork";
			return false;
		}
		if (!File.Exists(text2))
		{
			LastError = "Missing thunderbird-abook-golden.mork";
			return false;
		}
		if (!File.Exists(text3))
		{
			LastError = "Missing thunderbird-abook-profile.mork";
			return false;
		}
		if (!File.Exists(text4))
		{
			LastError = "Missing thunderbird-abook-mixed.mork";
			return false;
		}
		if (!File.Exists(text5))
		{
			LastError = "Missing thunderbird-abook-phones.mork";
			return false;
		}
		if (!File.Exists(text6))
		{
			LastError = "Missing thunderbird-abook-rowref.mork";
			return false;
		}
		if (!File.Exists(text7))
		{
			LastError = "Missing thunderbird-abook-company.mork";
			return false;
		}
		if (!File.Exists(text8))
		{
			LastError = "Missing thunderbird-abook-nick.mork";
			return false;
		}
		if (!File.Exists(text9))
		{
			LastError = "Missing thunderbird-abook-jobtitle.mork";
			return false;
		}
		if (!File.Exists(text10))
		{
			LastError = "Missing thunderbird-abook-department.mork";
			return false;
		}
		if (!File.Exists(text11))
		{
			LastError = "Missing thunderbird-abook-webpage.mork";
			return false;
		}
		if (!File.Exists(text12))
		{
			LastError = "Missing thunderbird-abook-fax.mork";
			return false;
		}
		if (!File.Exists(text13))
		{
			LastError = "Missing thunderbird-abook-pager.mork";
			return false;
		}
		if (!File.Exists(text14))
		{
			LastError = "Missing thunderbird-abook-custom1.mork";
			return false;
		}
		if (!File.Exists(text15))
		{
			LastError = "Missing thunderbird-abook-custom2.mork";
			return false;
		}
		if (!File.Exists(text16))
		{
			LastError = "Missing thunderbird-abook-custom3.mork";
			return false;
		}
		if (!File.Exists(text17))
		{
			LastError = "Missing thunderbird-abook-custom4.mork";
			return false;
		}
		if (!File.Exists(text18))
		{
			LastError = "Missing thunderbird-abook-spouse.mork";
			return false;
		}
		if (!File.Exists(text19))
		{
			LastError = "Missing thunderbird-abook-multitable.mork";
			return false;
		}
		List<MailContact> list = TryParseContacts(text);
		if (list == null || list.Count < 2)
		{
			LastError = "Sample fixture parse failed.";
			return false;
		}
		if (!FixtureContainsEmail(list, "alice@example.com"))
		{
			LastError = "Sample fixture missing alice@example.com";
			return false;
		}
		bool byteIdentical = false;
		string destPath = Path.Combine(Path.GetTempPath(), "lcars-mork-sample-preserve.mab");
		if (!TryWriteMorkPreservingSource(text, list, destPath, ref byteIdentical))
		{
			LastError = ((LastError != null) ? LastError : "Sample preserve export failed.");
			return false;
		}
		if (!byteIdentical)
		{
			LastError = "Sample preserve export not byte-identical to source .mab.";
			return false;
		}
		List<MailContact> list2 = TryParseContacts(text2);
		if (list2 == null || list2.Count < 2)
		{
			LastError = "Golden fixture parse failed.";
			return false;
		}
		if (!FixtureContainsEmail(list2, "carol@example.net") || !FixtureContainsEmail(list2, "dave@example.com"))
		{
			LastError = "Golden fixture missing expected contacts.";
			return false;
		}
		List<MailContact> list3 = TryParseContacts(text3);
		if (list3 == null || list3.Count < 2)
		{
			LastError = "Profile fixture parse failed.";
			return false;
		}
		if (!FixtureContainsEmail(list3, "eve@example.com") || !FixtureContainsEmail(list3, "frank@example.net"))
		{
			LastError = "Profile fixture missing expected contacts.";
			return false;
		}
		if (!FixtureHasEmailOrSecondary(list3, "eve.alias@example.com"))
		{
			LastError = "Profile fixture missing SecondEmail.";
			return false;
		}
		string text20 = Path.Combine(Path.GetTempPath(), "lcars-mork-golden-roundtrip.mab");
		bool byteIdentical2 = false;
		if (!TryWriteMorkPreservingSource(text2, list2, text20, ref byteIdentical2))
		{
			LastError = ((LastError != null) ? LastError : "Golden preserve export failed.");
			return false;
		}
		if (!byteIdentical2)
		{
			LastError = "Golden preserve export not byte-identical to source .mab.";
			return false;
		}
		List<MailContact> list4 = TryParseContacts(text20);
		if (list4 == null || list4.Count < list2.Count)
		{
			LastError = "Golden export round-trip failed.";
			return false;
		}
		List<MailContact> list5 = new List<MailContact>(list2);
		foreach (MailContact item in list5)
		{
			if (item != null && string.Equals(item.Email, "carol@example.net", StringComparison.OrdinalIgnoreCase))
			{
				item.DisplayName = "Carol Updated";
				break;
			}
		}
		string text21 = Path.Combine(Path.GetTempPath(), "lcars-mork-golden-inplace.mab");
		bool byteIdentical3 = false;
		if (!TryWriteMorkPreservingSource(text2, list5, text21, ref byteIdentical3))
		{
			LastError = ((LastError != null) ? LastError : "Golden in-place patch export failed.");
			return false;
		}
		if (byteIdentical3)
		{
			LastError = "Golden in-place patch should change bytes when display name updates.";
			return false;
		}
		List<MailContact> list6 = TryParseContacts(text21);
		if (list6 == null || !FixtureContactHasDisplayName(list6, "carol@example.net", "Carol Updated"))
		{
			LastError = "Golden in-place patch round-trip failed.";
			return false;
		}
		string text22 = Path.Combine(Path.GetTempPath(), "lcars-mork-profile-layout.mab");
		if (!TryWriteMorkFromSourceLayout(text3, list3, text22))
		{
			LastError = ((LastError != null) ? LastError : "Profile layout re-encode failed.");
			return false;
		}
		List<MailContact> list7 = TryParseContacts(text22);
		if (list7 == null || list7.Count < list3.Count)
		{
			LastError = "Profile layout round-trip failed.";
			return false;
		}
		if (!FixtureHasEmailOrSecondary(list7, "eve.alias@example.com"))
		{
			LastError = "Profile layout export lost SecondEmail.";
			return false;
		}
		string text23 = Path.Combine(Path.GetTempPath(), "lcars-mork-golden-layout.mab");
		if (!TryWriteMorkFromSourceLayout(text2, list2, text23))
		{
			LastError = ((LastError != null) ? LastError : "Golden layout re-encode failed.");
			return false;
		}
		List<MailContact> list8 = TryParseContacts(text23);
		if (list8 == null || !FixtureContactHasFirstName(list8, "carol@example.net", "Carol"))
		{
			LastError = "Golden layout export lost FirstName column.";
			return false;
		}
		string text24 = Path.Combine(Path.GetTempPath(), "lcars-mork-ldif-roundtrip.ldif");
		if (!ExportContactsToLdif(list, text24))
		{
			LastError = ((LastError != null) ? LastError : "LDIF export failed.");
			return false;
		}
		List<MailContact> list9 = TryParseContactsFromLdif(text24);
		if (list9 == null || list9.Count < list.Count)
		{
			LastError = "LDIF round-trip parse failed.";
			return false;
		}
		if (!FixtureContainsEmail(list9, "alice@example.com"))
		{
			LastError = "LDIF round-trip missing alice@example.com";
			return false;
		}
		List<MailContact> list10 = TryParseContacts(text4);
		if (list10 == null || list10.Count < 2)
		{
			LastError = "Mixed-table fixture parse failed.";
			return false;
		}
		if (!FixtureContainsEmail(list10, "mixed@example.com") || !FixtureContainsEmail(list10, "alt@example.net"))
		{
			LastError = "Mixed-table fixture missing expected contacts.";
			return false;
		}
		string destPath2 = Path.Combine(Path.GetTempPath(), "lcars-mork-mixed-preserve.mab");
		bool byteIdentical4 = false;
		if (!TryWriteMorkPreservingSource(text4, list10, destPath2, ref byteIdentical4))
		{
			LastError = ((LastError != null) ? LastError : "Mixed-table preserve export failed.");
			return false;
		}
		if (!byteIdentical4)
		{
			LastError = "Mixed-table preserve export not byte-identical to source .mab.";
			return false;
		}
		string text25 = Path.Combine(Path.GetTempPath(), "lcars-mork-mixed-layout.mab");
		if (!TryWriteMorkFromSourceLayout(text4, list10, text25))
		{
			LastError = ((LastError != null) ? LastError : "Mixed-table layout re-encode failed.");
			return false;
		}
		List<MailContact> list11 = TryParseContacts(text25);
		if (list11 == null || !FixtureContainsEmail(list11, "alt@example.net"))
		{
			LastError = "Mixed-table layout round-trip failed.";
			return false;
		}
		List<MailContact> list12 = TryParseContacts(text5);
		if (list12 == null || list12.Count < 1)
		{
			LastError = "Phones fixture parse failed.";
			return false;
		}
		if (!FixtureContactHasPhones(list12, "phones@example.com", "555-0100", "555-0199"))
		{
			LastError = "Phones fixture missing WorkPhone/HomePhone.";
			return false;
		}
		string destPath3 = Path.Combine(Path.GetTempPath(), "lcars-mork-phones-preserve.mab");
		bool byteIdentical5 = false;
		if (!TryWriteMorkPreservingSource(text5, list12, destPath3, ref byteIdentical5))
		{
			LastError = ((LastError != null) ? LastError : "Phones preserve export failed.");
			return false;
		}
		if (!byteIdentical5)
		{
			LastError = "Phones preserve export not byte-identical to source .mab.";
			return false;
		}
		List<MailContact> list13 = TryParseContacts(text6);
		if (list13 == null || !FixtureContainsEmail(list13, "rowref@example.com"))
		{
			LastError = "Row-ref fixture parse failed.";
			return false;
		}
		if (!FixtureContactHasNotes(list13, "rowref@example.com", "Call before noon"))
		{
			LastError = "Row-ref fixture missing Notes.";
			return false;
		}
		string destPath4 = Path.Combine(Path.GetTempPath(), "lcars-mork-rowref-preserve.mab");
		bool byteIdentical6 = false;
		if (!TryWriteMorkPreservingSource(text6, list13, destPath4, ref byteIdentical6))
		{
			LastError = ((LastError != null) ? LastError : "Row-ref preserve export failed.");
			return false;
		}
		if (!byteIdentical6)
		{
			LastError = "Row-ref preserve export not byte-identical to source .mab.";
			return false;
		}
		List<MailContact> list14 = TryParseContacts(text7);
		if (list14 == null || !FixtureContainsEmail(list14, "company@example.com"))
		{
			LastError = "Company fixture parse failed.";
			return false;
		}
		if (!FixtureContactHasCompany(list14, "company@example.com", "Acme Corp"))
		{
			LastError = "Company fixture missing Company column.";
			return false;
		}
		string destPath5 = Path.Combine(Path.GetTempPath(), "lcars-mork-company-preserve.mab");
		bool byteIdentical7 = false;
		if (!TryWriteMorkPreservingSource(text7, list14, destPath5, ref byteIdentical7))
		{
			LastError = ((LastError != null) ? LastError : "Company preserve export failed.");
			return false;
		}
		if (!byteIdentical7)
		{
			LastError = "Company preserve export not byte-identical to source .mab.";
			return false;
		}
		List<MailContact> list15 = TryParseContacts(text8);
		if (list15 == null || !FixtureContainsEmail(list15, "nick@example.com"))
		{
			LastError = "Nick fixture parse failed.";
			return false;
		}
		if (!FixtureContactHasNickName(list15, "nick@example.com", "Nicky"))
		{
			LastError = "Nick fixture missing NickName column.";
			return false;
		}
		string destPath6 = Path.Combine(Path.GetTempPath(), "lcars-mork-nick-preserve.mab");
		bool byteIdentical8 = false;
		if (!TryWriteMorkPreservingSource(text8, list15, destPath6, ref byteIdentical8))
		{
			LastError = ((LastError != null) ? LastError : "Nick preserve export failed.");
			return false;
		}
		if (!byteIdentical8)
		{
			LastError = "Nick preserve export not byte-identical to source .mab.";
			return false;
		}
		List<MailContact> list16 = TryParseContacts(text9);
		if (list16 == null || !FixtureContainsEmail(list16, "job@example.com"))
		{
			LastError = "JobTitle fixture parse failed.";
			return false;
		}
		if (!FixtureContactHasJobTitle(list16, "job@example.com", "Chief Engineer"))
		{
			LastError = "JobTitle fixture missing JobTitle column.";
			return false;
		}
		string destPath7 = Path.Combine(Path.GetTempPath(), "lcars-mork-jobtitle-preserve.mab");
		bool byteIdentical9 = false;
		if (!TryWriteMorkPreservingSource(text9, list16, destPath7, ref byteIdentical9))
		{
			LastError = ((LastError != null) ? LastError : "JobTitle preserve export failed.");
			return false;
		}
		if (!byteIdentical9)
		{
			LastError = "JobTitle preserve export not byte-identical to source .mab.";
			return false;
		}
		List<MailContact> list17 = TryParseContacts(text10);
		if (list17 == null || !FixtureContainsEmail(list17, "dept@example.com"))
		{
			LastError = "Department fixture parse failed.";
			return false;
		}
		if (!FixtureContactHasDepartment(list17, "dept@example.com", "Engineering"))
		{
			LastError = "Department fixture missing Department column.";
			return false;
		}
		string destPath8 = Path.Combine(Path.GetTempPath(), "lcars-mork-department-preserve.mab");
		bool byteIdentical10 = false;
		if (!TryWriteMorkPreservingSource(text10, list17, destPath8, ref byteIdentical10))
		{
			LastError = ((LastError != null) ? LastError : "Department preserve export failed.");
			return false;
		}
		if (!byteIdentical10)
		{
			LastError = "Department preserve export not byte-identical to source .mab.";
			return false;
		}
		List<MailContact> list18 = TryParseContacts(text11);
		if (list18 == null || !FixtureContainsEmail(list18, "web@example.com"))
		{
			LastError = "WebPage fixture parse failed.";
			return false;
		}
		if (!FixtureContactHasWebPage(list18, "web@example.com", "https://example.com/~web"))
		{
			LastError = "WebPage fixture missing WebPage1 column.";
			return false;
		}
		string destPath9 = Path.Combine(Path.GetTempPath(), "lcars-mork-webpage-preserve.mab");
		bool byteIdentical11 = false;
		if (!TryWriteMorkPreservingSource(text11, list18, destPath9, ref byteIdentical11))
		{
			LastError = ((LastError != null) ? LastError : "WebPage preserve export failed.");
			return false;
		}
		if (!byteIdentical11)
		{
			LastError = "WebPage preserve export not byte-identical to source .mab.";
			return false;
		}
		List<MailContact> list19 = TryParseContacts(text12);
		if (list19 == null || !FixtureContainsEmail(list19, "fax@example.com"))
		{
			LastError = "Fax fixture parse failed.";
			return false;
		}
		if (!FixtureContactHasFaxNumber(list19, "fax@example.com", "555-0142"))
		{
			LastError = "Fax fixture missing FaxNumber column.";
			return false;
		}
		string destPath10 = Path.Combine(Path.GetTempPath(), "lcars-mork-fax-preserve.mab");
		bool byteIdentical12 = false;
		if (!TryWriteMorkPreservingSource(text12, list19, destPath10, ref byteIdentical12))
		{
			LastError = ((LastError != null) ? LastError : "Fax preserve export failed.");
			return false;
		}
		if (!byteIdentical12)
		{
			LastError = "Fax preserve export not byte-identical to source .mab.";
			return false;
		}
		List<MailContact> list20 = TryParseContacts(text13);
		if (list20 == null || !FixtureContainsEmail(list20, "pager@example.com"))
		{
			LastError = "Pager fixture parse failed.";
			return false;
		}
		if (!FixtureContactHasPagerNumber(list20, "pager@example.com", "555-0199"))
		{
			LastError = "Pager fixture missing PagerNumber column.";
			return false;
		}
		string destPath11 = Path.Combine(Path.GetTempPath(), "lcars-mork-pager-preserve.mab");
		bool byteIdentical13 = false;
		if (!TryWriteMorkPreservingSource(text13, list20, destPath11, ref byteIdentical13))
		{
			LastError = ((LastError != null) ? LastError : "Pager preserve export failed.");
			return false;
		}
		if (!byteIdentical13)
		{
			LastError = "Pager preserve export not byte-identical to source .mab.";
			return false;
		}
		List<MailContact> list21 = TryParseContacts(text14);
		if (list21 == null || !FixtureContainsEmail(list21, "custom@example.com"))
		{
			LastError = "Custom1 fixture parse failed.";
			return false;
		}
		if (!FixtureContactHasCustom1(list21, "custom@example.com", "VIP-42"))
		{
			LastError = "Custom1 fixture missing Custom1 column.";
			return false;
		}
		string destPath12 = Path.Combine(Path.GetTempPath(), "lcars-mork-custom1-preserve.mab");
		bool byteIdentical14 = false;
		if (!TryWriteMorkPreservingSource(text14, list21, destPath12, ref byteIdentical14))
		{
			LastError = ((LastError != null) ? LastError : "Custom1 preserve export failed.");
			return false;
		}
		if (!byteIdentical14)
		{
			LastError = "Custom1 preserve export not byte-identical to source .mab.";
			return false;
		}
		List<MailContact> list22 = TryParseContacts(text15);
		if (list22 == null || !FixtureContainsEmail(list22, "custom2@example.com"))
		{
			LastError = "Custom2 fixture parse failed.";
			return false;
		}
		if (!FixtureContactHasCustom2(list22, "custom2@example.com", "TAG-99"))
		{
			LastError = "Custom2 fixture missing Custom2 column.";
			return false;
		}
		string destPath13 = Path.Combine(Path.GetTempPath(), "lcars-mork-custom2-preserve.mab");
		bool byteIdentical15 = false;
		if (!TryWriteMorkPreservingSource(text15, list22, destPath13, ref byteIdentical15))
		{
			LastError = ((LastError != null) ? LastError : "Custom2 preserve export failed.");
			return false;
		}
		if (!byteIdentical15)
		{
			LastError = "Custom2 preserve export not byte-identical to source .mab.";
			return false;
		}
		List<MailContact> list23 = TryParseContacts(text16);
		if (list23 == null || !FixtureContainsEmail(list23, "custom3@example.com"))
		{
			LastError = "Custom3 fixture parse failed.";
			return false;
		}
		if (!FixtureContactHasCustom3(list23, "custom3@example.com", "RANK-A"))
		{
			LastError = "Custom3 fixture missing Custom3 column.";
			return false;
		}
		string destPath14 = Path.Combine(Path.GetTempPath(), "lcars-mork-custom3-preserve.mab");
		bool byteIdentical16 = false;
		if (!TryWriteMorkPreservingSource(text16, list23, destPath14, ref byteIdentical16))
		{
			LastError = ((LastError != null) ? LastError : "Custom3 preserve export failed.");
			return false;
		}
		if (!byteIdentical16)
		{
			LastError = "Custom3 preserve export not byte-identical to source .mab.";
			return false;
		}
		List<MailContact> list24 = TryParseContacts(text17);
		if (list24 == null || !FixtureContainsEmail(list24, "custom4@example.com"))
		{
			LastError = "Custom4 fixture parse failed.";
			return false;
		}
		if (!FixtureContactHasCustom4(list24, "custom4@example.com", "POOL-7"))
		{
			LastError = "Custom4 fixture missing Custom4 column.";
			return false;
		}
		string destPath15 = Path.Combine(Path.GetTempPath(), "lcars-mork-custom4-preserve.mab");
		bool byteIdentical17 = false;
		if (!TryWriteMorkPreservingSource(text17, list24, destPath15, ref byteIdentical17))
		{
			LastError = ((LastError != null) ? LastError : "Custom4 preserve export failed.");
			return false;
		}
		if (!byteIdentical17)
		{
			LastError = "Custom4 preserve export not byte-identical to source .mab.";
			return false;
		}
		List<MailContact> list25 = TryParseContacts(text18);
		if (list25 == null || !FixtureContainsEmail(list25, "spouse@example.com"))
		{
			LastError = "SpouseName fixture parse failed.";
			return false;
		}
		if (!FixtureContactHasSpouseName(list25, "spouse@example.com", "Alex Partner"))
		{
			LastError = "SpouseName fixture missing SpouseName column.";
			return false;
		}
		string destPath16 = Path.Combine(Path.GetTempPath(), "lcars-mork-spouse-preserve.mab");
		bool byteIdentical18 = false;
		if (!TryWriteMorkPreservingSource(text18, list25, destPath16, ref byteIdentical18))
		{
			LastError = ((LastError != null) ? LastError : "SpouseName preserve export failed.");
			return false;
		}
		if (!byteIdentical18)
		{
			LastError = "SpouseName preserve export not byte-identical to source .mab.";
			return false;
		}
		List<MailContact> list26 = TryParseContacts(text19);
		if (list26 == null || list26.Count < 2)
		{
			LastError = "Multi-table fixture parse failed.";
			return false;
		}
		if (!FixtureContainsEmail(list26, "alpha@example.com") || !FixtureContainsEmail(list26, "beta@example.com"))
		{
			LastError = "Multi-table fixture missing table rows.";
			return false;
		}
		string text26 = Path.Combine(Path.GetTempPath(), "lcars-mork-multitable-layout.mab");
		if (!TryWriteMorkFromSourceLayout(text19, list26, text26))
		{
			LastError = ((LastError != null) ? LastError : "Multi-table layout export failed.");
			return false;
		}
		string text27 = File.ReadAllText(text26);
		int num = 0;
		int startIndex = 0;
		checked
		{
			while (true)
			{
				startIndex = text27.IndexOf("(k=", startIndex, StringComparison.Ordinal);
				if (startIndex < 0)
				{
					break;
				}
				num++;
				startIndex += 3;
			}
			if (num < 2)
			{
				LastError = "Multi-table layout export dropped (k=) tables.";
				return false;
			}
			if (text27.IndexOf("^70^", StringComparison.Ordinal) < 0)
			{
				LastError = "Multi-table layout export lost second-table column ids.";
				return false;
			}
			string destPath17 = Path.Combine(Path.GetTempPath(), "lcars-mork-multitable-preserve.mab");
			bool byteIdentical19 = false;
			if (!TryWriteMorkPreservingSource(text19, list26, destPath17, ref byteIdentical19))
			{
				LastError = ((LastError != null) ? LastError : "Multi-table preserve export failed.");
				return false;
			}
			if (!byteIdentical19)
			{
				LastError = "Multi-table preserve export not byte-identical to source .mab.";
				return false;
			}
			string text28 = Path.Combine(Path.GetTempPath(), "lcars-mork-mixed-fullenc.mab");
			if (!TryWriteMorkFullEncoderFromSource(text4, list10, text28))
			{
				LastError = ((LastError != null) ? LastError : "Mixed-table full encoder export failed.");
				return false;
			}
			if (CountMorkDataRows(File.ReadAllText(text28)) != CountMorkDataRows(File.ReadAllText(text4)))
			{
				LastError = "Mixed-table full encoder row count mismatch.";
				return false;
			}
			contactCount = list.Count + list2.Count + list3.Count + list10.Count + list12.Count + list13.Count + list14.Count + list15.Count + list16.Count + list17.Count + list18.Count + list19.Count + list20.Count + list21.Count + list22.Count + list23.Count + list24.Count + list25.Count + list26.Count;
			return true;
		}
	}

	private static bool FixtureContainsEmail(List<MailContact> contacts, string email)
	{
		foreach (MailContact contact in contacts)
		{
			if (contact != null && string.Equals(contact.Email, email, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	private static bool FixtureContactHasDisplayName(List<MailContact> contacts, string email, string displayName)
	{
		foreach (MailContact contact in contacts)
		{
			if (contact == null || !string.Equals(contact.Email, email, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			MailContact mailContact = contact;
			return string.Equals(((mailContact.DisplayName != null) ? mailContact.DisplayName : "").Trim(), displayName, StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private static bool FixtureContactHasNickName(List<MailContact> contacts, string email, string nickName)
	{
		foreach (MailContact contact in contacts)
		{
			if (contact == null || !string.Equals(contact.Email, email, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			MailContact mailContact = contact;
			return string.Equals(((mailContact.NickName != null) ? mailContact.NickName : "").Trim(), nickName, StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private static bool FixtureContactHasJobTitle(List<MailContact> contacts, string email, string jobTitle)
	{
		foreach (MailContact contact in contacts)
		{
			if (contact == null || !string.Equals(contact.Email, email, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			MailContact mailContact = contact;
			return string.Equals(((mailContact.JobTitle != null) ? mailContact.JobTitle : "").Trim(), jobTitle, StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private static bool FixtureContactHasDepartment(List<MailContact> contacts, string email, string department)
	{
		foreach (MailContact contact in contacts)
		{
			if (contact == null || !string.Equals(contact.Email, email, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			MailContact mailContact = contact;
			return string.Equals(((mailContact.Department != null) ? mailContact.Department : "").Trim(), department, StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private static bool FixtureContactHasWebPage(List<MailContact> contacts, string email, string webPage)
	{
		foreach (MailContact contact in contacts)
		{
			if (contact == null || !string.Equals(contact.Email, email, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			MailContact mailContact = contact;
			return string.Equals(((mailContact.WebPage != null) ? mailContact.WebPage : "").Trim(), webPage, StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private static bool FixtureContactHasFaxNumber(List<MailContact> contacts, string email, string faxNumber)
	{
		foreach (MailContact contact in contacts)
		{
			if (contact == null || !string.Equals(contact.Email, email, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			MailContact mailContact = contact;
			return string.Equals(((mailContact.FaxNumber != null) ? mailContact.FaxNumber : "").Trim(), faxNumber, StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private static bool FixtureContactHasPagerNumber(List<MailContact> contacts, string email, string pagerNumber)
	{
		foreach (MailContact contact in contacts)
		{
			if (contact == null || !string.Equals(contact.Email, email, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			MailContact mailContact = contact;
			return string.Equals(((mailContact.PagerNumber != null) ? mailContact.PagerNumber : "").Trim(), pagerNumber, StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private static bool FixtureContactHasCustom1(List<MailContact> contacts, string email, string custom1)
	{
		foreach (MailContact contact in contacts)
		{
			if (contact == null || !string.Equals(contact.Email, email, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			MailContact mailContact = contact;
			return string.Equals(((mailContact.Custom1 != null) ? mailContact.Custom1 : "").Trim(), custom1, StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private static bool FixtureContactHasCustom2(List<MailContact> contacts, string email, string custom2)
	{
		foreach (MailContact contact in contacts)
		{
			if (contact == null || !string.Equals(contact.Email, email, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			MailContact mailContact = contact;
			return string.Equals(((mailContact.Custom2 != null) ? mailContact.Custom2 : "").Trim(), custom2, StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private static bool FixtureContactHasCustom3(List<MailContact> contacts, string email, string custom3)
	{
		foreach (MailContact contact in contacts)
		{
			if (contact == null || !string.Equals(contact.Email, email, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			MailContact mailContact = contact;
			return string.Equals(((mailContact.Custom3 != null) ? mailContact.Custom3 : "").Trim(), custom3, StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private static bool FixtureContactHasCustom4(List<MailContact> contacts, string email, string custom4)
	{
		foreach (MailContact contact in contacts)
		{
			if (contact == null || !string.Equals(contact.Email, email, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			MailContact mailContact = contact;
			return string.Equals(((mailContact.Custom4 != null) ? mailContact.Custom4 : "").Trim(), custom4, StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private static bool FixtureContactHasSpouseName(List<MailContact> contacts, string email, string spouseName)
	{
		foreach (MailContact contact in contacts)
		{
			if (contact == null || !string.Equals(contact.Email, email, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			MailContact mailContact = contact;
			return string.Equals(((mailContact.SpouseName != null) ? mailContact.SpouseName : "").Trim(), spouseName, StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private static bool FixtureContactHasCompany(List<MailContact> contacts, string email, string company)
	{
		foreach (MailContact contact in contacts)
		{
			if (contact == null || !string.Equals(contact.Email, email, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			MailContact mailContact = contact;
			return string.Equals(((mailContact.Company != null) ? mailContact.Company : "").Trim(), company, StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private static bool FixtureContactHasNotes(List<MailContact> contacts, string email, string notes)
	{
		foreach (MailContact contact in contacts)
		{
			if (contact == null || !string.Equals(contact.Email, email, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			MailContact mailContact = contact;
			return string.Equals(((mailContact.Notes != null) ? mailContact.Notes : "").Trim(), notes, StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private static bool FixtureContactHasPhones(List<MailContact> contacts, string email, string workPhone, string homePhone)
	{
		foreach (MailContact contact in contacts)
		{
			if (contact == null || !string.Equals(contact.Email, email, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			MailContact mailContact = contact;
			int result;
			if (string.Equals(((mailContact.WorkPhone != null) ? mailContact.WorkPhone : "").Trim(), workPhone, StringComparison.OrdinalIgnoreCase))
			{
				MailContact mailContact2 = contact;
				if (string.Equals(((mailContact2.HomePhone != null) ? mailContact2.HomePhone : "").Trim(), homePhone, StringComparison.OrdinalIgnoreCase))
				{
					result = 1;
					goto IL_007a;
				}
			}
			result = 0;
			goto IL_007a;
			IL_007a:
			return (byte)result != 0;
		}
		return false;
	}

	private static bool FixtureContactHasFirstName(List<MailContact> contacts, string email, string firstName)
	{
		foreach (MailContact contact in contacts)
		{
			if (contact == null || !string.Equals(contact.Email, email, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			MailContact mailContact = contact;
			return string.Equals(((mailContact.FirstName != null) ? mailContact.FirstName : "").Trim(), firstName, StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private static bool FixtureHasEmailOrSecondary(List<MailContact> contacts, string email)
	{
		foreach (MailContact contact in contacts)
		{
			if (contact != null)
			{
				if (string.Equals(contact.Email, email, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
				if (string.Equals(contact.SecondaryEmail, email, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
		}
		return false;
	}

	public static bool ExportContactsToVCardFile(List<MailContact> contacts, string destPath)
	{
		LastError = null;
		bool result;
		if (contacts == null || string.IsNullOrWhiteSpace(destPath))
		{
			result = false;
		}
		else
		{
			try
			{
				StringBuilder stringBuilder = new StringBuilder();
				foreach (MailContact contact in contacts)
				{
					if (contact != null && !string.IsNullOrWhiteSpace(contact.Email))
					{
						stringBuilder.AppendLine("BEGIN:VCARD");
						stringBuilder.AppendLine("VERSION:3.0");
						MailContact mailContact = contact;
						stringBuilder.AppendLine("FN:" + ((mailContact.DisplayName != null) ? mailContact.DisplayName : contact.Email));
						stringBuilder.AppendLine("EMAIL:" + contact.Email);
						if (!string.IsNullOrWhiteSpace(contact.Company))
						{
							stringBuilder.AppendLine("ORG:" + contact.Company);
						}
						if (!string.IsNullOrWhiteSpace(contact.JobTitle))
						{
							stringBuilder.AppendLine("TITLE:" + contact.JobTitle);
						}
						if (!string.IsNullOrWhiteSpace(contact.WebPage))
						{
							stringBuilder.AppendLine("URL:" + contact.WebPage);
						}
						if (!string.IsNullOrWhiteSpace(contact.WorkPhone))
						{
							stringBuilder.AppendLine("TEL;TYPE=WORK:" + contact.WorkPhone);
						}
						if (!string.IsNullOrWhiteSpace(contact.HomePhone))
						{
							stringBuilder.AppendLine("TEL;TYPE=HOME:" + contact.HomePhone);
						}
						if (!string.IsNullOrWhiteSpace(contact.Notes))
						{
							stringBuilder.AppendLine("NOTE:" + contact.Notes.Replace("\n", "\\n"));
						}
						stringBuilder.AppendLine("END:VCARD");
					}
				}
				File.WriteAllText(destPath, stringBuilder.ToString(), Encoding.UTF8);
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
}
