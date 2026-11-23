using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

public sealed class MailSearchIndexCursor
{
	private class CursorDto
	{
		public string AccountId;

		public int FolderIndex;

		[DebuggerNonUserCode]
		public CursorDto()
		{
		}
	}

	private class CorpusCursorDto
	{
		public string AccountId;

		public int RowIndex;

		[DebuggerNonUserCode]
		public CorpusCursorDto()
		{
		}
	}

	private const string StoreFileName = "index-folder-cursor.json";

	private const string CorpusStoreFileName = "index-corpus-cursor.json";

	private const string MetaStoreFileName = "index-meta-cursor.json";

	private MailSearchIndexCursor()
	{
	}

	public static List<MailFolderInfo> TakeNextFolders(string accountId, IList<MailFolderInfo> allFolders, int batchSize)
	{
		List<MailFolderInfo> list = new List<MailFolderInfo>();
		if (string.IsNullOrWhiteSpace(accountId) || allFolders == null || allFolders.Count == 0)
		{
			return list;
		}
		int num = Math.Max(1, batchSize);
		Dictionary<string, int> dictionary = LoadMap();
		int num2 = 0;
		if (dictionary.ContainsKey(accountId))
		{
			num2 = dictionary[accountId];
		}
		if (num2 < 0 || num2 >= allFolders.Count)
		{
			num2 = 0;
		}
		int num3 = 0;
		int num4 = num2;
		checked
		{
			while (num3 < num && num3 < allFolders.Count)
			{
				MailFolderInfo mailFolderInfo = allFolders[num4];
				if (mailFolderInfo != null)
				{
					list.Add(mailFolderInfo);
				}
				num3++;
				num4++;
				if (num4 >= allFolders.Count)
				{
					num4 = 0;
				}
				if (num4 == num2 && num3 > 0)
				{
					break;
				}
			}
			dictionary[accountId] = num4;
			SaveMap(dictionary);
			return list;
		}
	}

	private static Dictionary<string, int> LoadMap()
	{
		Dictionary<string, int> result;
		if (!File.Exists(StorePath()))
		{
			result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		else
		{
			try
			{
				JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
				List<CursorDto> list = javaScriptSerializer.Deserialize<List<CursorDto>>(File.ReadAllText(StorePath(), Encoding.UTF8));
				Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
				if (list == null)
				{
					result = dictionary;
				}
				else
				{
					foreach (CursorDto item in list)
					{
						if (item != null && !string.IsNullOrWhiteSpace(item.AccountId))
						{
							dictionary[item.AccountId] = item.FolderIndex;
						}
					}
					result = dictionary;
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}

	private static void SaveMap(Dictionary<string, int> map)
	{
		try
		{
			List<CursorDto> list = new List<CursorDto>();
			foreach (KeyValuePair<string, int> item in map)
			{
				list.Add(new CursorDto
				{
					AccountId = item.Key,
					FolderIndex = item.Value
				});
			}
			JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
			File.WriteAllText(StorePath(), javaScriptSerializer.Serialize(list), Encoding.UTF8);
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
	}

	private static string StorePath()
	{
		return Path.Combine(AccountStore.DataRoot(), "search-index", "index-folder-cursor.json");
	}

	public static string FormatRotationHint(string accountId, int folderCount)
	{
		if (string.IsNullOrWhiteSpace(accountId) || folderCount <= 0)
		{
			return "";
		}
		Dictionary<string, int> dictionary = LoadMap();
		int num = 0;
		if (dictionary.ContainsKey(accountId))
		{
			num = dictionary[accountId];
		}
		if (num < 0 || num >= folderCount)
		{
			num = 0;
		}
		string text = "";
		Dictionary<string, int> dictionary2 = LoadCorpusMap();
		if (dictionary2.ContainsKey(accountId))
		{
			text = " · FTS-CUR " + dictionary2[accountId];
		}
		return "CURSOR " + num + "/" + folderCount + text;
	}

	public static int GetCorpusRebuildStart(string accountId, int entryCount)
	{
		if (string.IsNullOrWhiteSpace(accountId) || entryCount <= 0)
		{
			return 0;
		}
		Dictionary<string, int> dictionary = LoadCorpusMap();
		int num = 0;
		if (dictionary.ContainsKey(accountId))
		{
			num = dictionary[accountId];
		}
		if (num < 0 || num >= entryCount)
		{
			num = 0;
		}
		return num;
	}

	public static void AdvanceCorpusRebuildCursor(string accountId, int entryCount, int processed)
	{
		if (!string.IsNullOrWhiteSpace(accountId) && entryCount > 0 && processed > 0)
		{
			Dictionary<string, int> dictionary = LoadCorpusMap();
			int num = 0;
			if (dictionary.ContainsKey(accountId))
			{
				num = dictionary[accountId];
			}
			if (num < 0 || num >= entryCount)
			{
				num = 0;
			}
			dictionary[accountId] = checked(num + processed) % entryCount;
			SaveCorpusMap(dictionary);
		}
	}

	private static Dictionary<string, int> LoadCorpusMap()
	{
		string path = CorpusStorePath();
		Dictionary<string, int> result;
		if (!File.Exists(path))
		{
			result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		else
		{
			try
			{
				JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
				List<CorpusCursorDto> list = javaScriptSerializer.Deserialize<List<CorpusCursorDto>>(File.ReadAllText(path, Encoding.UTF8));
				Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
				if (list == null)
				{
					result = dictionary;
				}
				else
				{
					foreach (CorpusCursorDto item in list)
					{
						if (item != null && !string.IsNullOrWhiteSpace(item.AccountId))
						{
							dictionary[item.AccountId] = item.RowIndex;
						}
					}
					result = dictionary;
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}

	private static void SaveCorpusMap(Dictionary<string, int> map)
	{
		try
		{
			List<CorpusCursorDto> list = new List<CorpusCursorDto>();
			foreach (KeyValuePair<string, int> item in map)
			{
				list.Add(new CorpusCursorDto
				{
					AccountId = item.Key,
					RowIndex = item.Value
				});
			}
			JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
			File.WriteAllText(CorpusStorePath(), javaScriptSerializer.Serialize(list), Encoding.UTF8);
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
	}

	private static string CorpusStorePath()
	{
		return Path.Combine(AccountStore.DataRoot(), "search-index", "index-corpus-cursor.json");
	}

	public static int GetMetaBackfillStart(string accountId, int entryCount)
	{
		if (string.IsNullOrWhiteSpace(accountId) || entryCount <= 0)
		{
			return 0;
		}
		Dictionary<string, int> dictionary = LoadMetaMap();
		int num = 0;
		if (dictionary.ContainsKey(accountId))
		{
			num = dictionary[accountId];
		}
		if (num < 0 || num >= entryCount)
		{
			num = 0;
		}
		return num;
	}

	public static void AdvanceMetaBackfillCursor(string accountId, int entryCount, int processed)
	{
		if (!string.IsNullOrWhiteSpace(accountId) && entryCount > 0 && processed > 0)
		{
			Dictionary<string, int> dictionary = LoadMetaMap();
			int num = 0;
			if (dictionary.ContainsKey(accountId))
			{
				num = dictionary[accountId];
			}
			if (num < 0 || num >= entryCount)
			{
				num = 0;
			}
			dictionary[accountId] = checked(num + processed) % entryCount;
			SaveMetaMap(dictionary);
		}
	}

	private static Dictionary<string, int> LoadMetaMap()
	{
		string path = MetaStorePath();
		Dictionary<string, int> result;
		if (!File.Exists(path))
		{
			result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		else
		{
			try
			{
				JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
				List<CorpusCursorDto> list = javaScriptSerializer.Deserialize<List<CorpusCursorDto>>(File.ReadAllText(path, Encoding.UTF8));
				Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
				if (list == null)
				{
					result = dictionary;
				}
				else
				{
					foreach (CorpusCursorDto item in list)
					{
						if (item != null && !string.IsNullOrWhiteSpace(item.AccountId))
						{
							dictionary[item.AccountId] = item.RowIndex;
						}
					}
					result = dictionary;
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}

	private static void SaveMetaMap(Dictionary<string, int> map)
	{
		try
		{
			List<CorpusCursorDto> list = new List<CorpusCursorDto>();
			foreach (KeyValuePair<string, int> item in map)
			{
				list.Add(new CorpusCursorDto
				{
					AccountId = item.Key,
					RowIndex = item.Value
				});
			}
			JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
			File.WriteAllText(MetaStorePath(), javaScriptSerializer.Serialize(list), Encoding.UTF8);
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
	}

	private static string MetaStorePath()
	{
		return Path.Combine(AccountStore.DataRoot(), "search-index", "index-meta-cursor.json");
	}
}
