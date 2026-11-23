using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

public sealed class EwsItemIdStore
{
	private class MapEntry
	{
		public ulong Uid;

		public string ItemId;

		[DebuggerNonUserCode]
		public MapEntry()
		{
		}
	}

	private const string MapSubdir = "ews-item-map";

	private EwsItemIdStore()
	{
	}

	public static ulong ItemIdToUid(string itemId)
	{
		string text = ((itemId != null) ? itemId : "");
		if (text.Length == 0)
		{
			return 0uL;
		}
		ulong num = 14695981039346656037uL;
		byte[] bytes = Encoding.UTF8.GetBytes(text);
		foreach (byte b in bytes)
		{
			num ^= b;
			num = checked(num * 1099511628211L);
		}
		if (num == 0)
		{
			num = 1uL;
		}
		return num;
	}

	public static void Remember(string accountId, string folder, ulong uid, string itemId)
	{
		if (!string.IsNullOrWhiteSpace(accountId) && uid != 0)
		{
			string text = ((itemId != null) ? itemId : "").Trim();
			if (text.Length != 0)
			{
				Dictionary<string, MapEntry> dictionary = LoadMap(accountId, folder);
				dictionary[uid.ToString()] = new MapEntry
				{
					Uid = uid,
					ItemId = text
				};
				SaveMap(accountId, folder, dictionary);
			}
		}
	}

	public static bool TryGetItemId(string accountId, string folder, ulong uid, ref string itemId)
	{
		itemId = null;
		if (string.IsNullOrWhiteSpace(accountId) || uid == 0)
		{
			return false;
		}
		Dictionary<string, MapEntry> dictionary = LoadMap(accountId, folder);
		MapEntry value = null;
		if (!dictionary.TryGetValue(uid.ToString(), out value) || value == null)
		{
			return false;
		}
		itemId = value.ItemId;
		return (itemId != null && itemId.Length > 0) ? true : false;
	}

	private static Dictionary<string, MapEntry> LoadMap(string accountId, string folder)
	{
		string path = MapPath(accountId, folder);
		Dictionary<string, MapEntry> result;
		if (!File.Exists(path))
		{
			result = new Dictionary<string, MapEntry>(StringComparer.Ordinal);
		}
		else
		{
			try
			{
				JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
				List<MapEntry> list = javaScriptSerializer.Deserialize<List<MapEntry>>(File.ReadAllText(path, Encoding.UTF8));
				Dictionary<string, MapEntry> dictionary = new Dictionary<string, MapEntry>(StringComparer.Ordinal);
				if (list == null)
				{
					result = dictionary;
				}
				else
				{
					foreach (MapEntry item in list)
					{
						if (item != null && item.Uid != 0)
						{
							dictionary[item.Uid.ToString()] = item;
						}
					}
					result = dictionary;
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = new Dictionary<string, MapEntry>(StringComparer.Ordinal);
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}

	private static void SaveMap(string accountId, string folder, Dictionary<string, MapEntry> map)
	{
		string path = Path.Combine(AccountStore.DataRoot(), "ews-item-map");
		if (!Directory.Exists(path))
		{
			Directory.CreateDirectory(path);
		}
		List<MapEntry> list = new List<MapEntry>();
		if (map != null)
		{
			foreach (KeyValuePair<string, MapEntry> item in map)
			{
				if (item.Value != null)
				{
					list.Add(item.Value);
				}
			}
		}
		JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
		File.WriteAllText(MapPath(accountId, folder), javaScriptSerializer.Serialize(list), Encoding.UTF8);
	}

	private static string MapPath(string accountId, string folder)
	{
		string text = SanitizeToken(accountId);
		string text2 = SanitizeToken((folder != null) ? folder : "INBOX");
		return Path.Combine(AccountStore.DataRoot(), "ews-item-map", text + "__" + text2 + ".json");
	}

	private static string SanitizeToken(string raw)
	{
		string text = ((raw != null) ? raw : "").Trim();
		if (text.Length == 0)
		{
			return "default";
		}
		char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
		StringBuilder stringBuilder = new StringBuilder(text.Length);
		string text2 = text;
		int i = 0;
		for (int length = text2.Length; i < length; i = checked(i + 1))
		{
			char value = text2[i];
			if (Array.IndexOf(invalidFileNameChars, value) >= 0)
			{
				stringBuilder.Append('_');
			}
			else
			{
				stringBuilder.Append(value);
			}
		}
		return stringBuilder.ToString();
	}
}
