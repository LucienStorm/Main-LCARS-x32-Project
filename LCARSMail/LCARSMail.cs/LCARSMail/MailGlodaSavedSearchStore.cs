using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Web.Script.Serialization;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

public sealed class MailGlodaSavedSearchStore
{
	private class SavedSearchDto
	{
		public string Name;

		public string Query;

		public string FolderScope;

		public DateTime SavedUtc;

		[DebuggerNonUserCode]
		public SavedSearchDto()
		{
		}
	}

	private const string StoreFileName = "gloda-saved-searches.json";

	private const int MaxSaved = 24;

	private MailGlodaSavedSearchStore()
	{
	}

	public static bool TrySaveSearch(string name, string query, string folderScope = null)
	{
		string text = ((query != null) ? query : "").Trim();
		if (text.Length == 0)
		{
			return false;
		}
		string text2 = ((folderScope != null) ? folderScope : "").Trim();
		if (text2.Length > 0 && text.IndexOf("folder:", StringComparison.OrdinalIgnoreCase) < 0)
		{
			text = "folder:" + text2 + " " + text;
		}
		string text3 = ((name != null) ? name : "").Trim();
		if (text3.Length == 0)
		{
			text3 = text;
		}
		if (text3.Length > 64)
		{
			text3 = text3.Substring(0, 61) + "…";
		}
		List<SavedSearchDto> list = LoadAll();
		list.RemoveAll([SpecialName] (SavedSearchDto s) => (s != null && string.Equals(s.Query, text, StringComparison.OrdinalIgnoreCase)) ? true : false);
		list.Insert(0, new SavedSearchDto
		{
			Name = text3,
			Query = text,
			FolderScope = text2,
			SavedUtc = DateTime.UtcNow
		});
		while (list.Count > 24)
		{
			list.RemoveAt(checked(list.Count - 1));
		}
		return WriteAll(list);
	}

	public static List<KeyValuePair<string, string>> TryListSaved()
	{
		List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();
		foreach (SavedSearchDto item2 in LoadAll())
		{
			if (item2 != null && !string.IsNullOrWhiteSpace(item2.Query))
			{
				SavedSearchDto savedSearchDto = item2;
				KeyValuePair<string, string> item = new KeyValuePair<string, string>((savedSearchDto.Name != null) ? savedSearchDto.Name : item2.Query, item2.Query);
				list.Add(item);
			}
		}
		return list;
	}

	public static string TryGetMostRecentQuery()
	{
		List<SavedSearchDto> list = LoadAll();
		if (list == null || list.Count == 0)
		{
			return "";
		}
		SavedSearchDto savedSearchDto = list[0];
		if (savedSearchDto == null)
		{
			return "";
		}
		SavedSearchDto savedSearchDto2 = savedSearchDto;
		return ((savedSearchDto2.Query != null) ? savedSearchDto2.Query : "").Trim();
	}

	private static List<SavedSearchDto> LoadAll()
	{
		string path = StorePath();
		List<SavedSearchDto> result;
		if (!File.Exists(path))
		{
			result = new List<SavedSearchDto>();
		}
		else
		{
			try
			{
				JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
				List<SavedSearchDto> list = javaScriptSerializer.Deserialize<List<SavedSearchDto>>(File.ReadAllText(path, Encoding.UTF8));
				result = ((list != null) ? list : new List<SavedSearchDto>());
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = new List<SavedSearchDto>();
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}

	private static bool WriteAll(List<SavedSearchDto> list)
	{
		bool result;
		try
		{
			JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
			File.WriteAllText(StorePath(), javaScriptSerializer.Serialize(list), Encoding.UTF8);
			result = true;
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			result = false;
			ProjectData.ClearProjectError();
		}
		return result;
	}

	private static string StorePath()
	{
		string text = Path.Combine(AccountStore.DataRoot(), "search-index");
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		return Path.Combine(text, "gloda-saved-searches.json");
	}
}
