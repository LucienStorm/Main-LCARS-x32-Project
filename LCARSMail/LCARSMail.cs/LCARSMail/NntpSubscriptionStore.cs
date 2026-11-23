using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

public sealed class NntpSubscriptionStore
{
	private const string FileName = "nntp-subscriptions.json";

	private const string HostFileName = "nntp-last-host.txt";

	private const string LastArticleFileName = "nntp-last-article.json";

	private NntpSubscriptionStore()
	{
	}

	public static List<string> LoadGroups()
	{
		string path = StorePath();
		List<string> result;
		if (!File.Exists(path))
		{
			result = new List<string>();
		}
		else
		{
			try
			{
				string input = File.ReadAllText(path, Encoding.UTF8);
				JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
				List<string> list = javaScriptSerializer.Deserialize<List<string>>(input);
				result = ((list != null) ? list : new List<string>());
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = new List<string>();
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}

	public static void SaveGroups(IList<string> groups)
	{
		if (groups == null)
		{
			return;
		}
		List<string> list = new List<string>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string group in groups)
		{
			string text = ((group != null) ? group : "").Trim();
			if (text.Length != 0 && hashSet.Add(text))
			{
				list.Add(text);
			}
		}
		list.Sort(StringComparer.OrdinalIgnoreCase);
		JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
		File.WriteAllText(StorePath(), javaScriptSerializer.Serialize(list), Encoding.UTF8);
	}

	public static void Subscribe(string groupName)
	{
		string text = ((groupName != null) ? groupName : "").Trim();
		if (text.Length == 0)
		{
			return;
		}
		List<string> list = LoadGroups();
		foreach (string item in list)
		{
			if (string.Equals(item, text, StringComparison.OrdinalIgnoreCase))
			{
				return;
			}
		}
		list.Add(text);
		SaveGroups(list);
	}

	public static void Unsubscribe(string groupName)
	{
		string text = ((groupName != null) ? groupName : "").Trim();
		if (text.Length == 0)
		{
			return;
		}
		List<string> list = LoadGroups();
		List<string> list2 = new List<string>();
		foreach (string item in list)
		{
			if (!string.Equals(item, text, StringComparison.OrdinalIgnoreCase))
			{
				list2.Add(item);
			}
		}
		SaveGroups(list2);
	}

	public static string LoadLastHost()
	{
		string path = Path.Combine(AccountStore.DataRoot(), "nntp-last-host.txt");
		string result;
		if (!File.Exists(path))
		{
			result = "";
		}
		else
		{
			try
			{
				result = File.ReadAllText(path, Encoding.UTF8).Trim();
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = "";
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}

	public static void SaveLastHost(string host)
	{
		string text = ((host != null) ? host : "").Trim();
		if (text.Length != 0)
		{
			File.WriteAllText(Path.Combine(AccountStore.DataRoot(), "nntp-last-host.txt"), text, Encoding.UTF8);
		}
	}

	public static int GetLastArticleNumber(string groupName)
	{
		string text = ((groupName != null) ? groupName : "").Trim();
		if (text.Length == 0)
		{
			return 0;
		}
		Dictionary<string, int> dictionary = LoadLastArticleMap();
		if (dictionary.ContainsKey(text))
		{
			return dictionary[text];
		}
		return 0;
	}

	public static void SetLastArticleNumber(string groupName, int articleNumber)
	{
		string text = ((groupName != null) ? groupName : "").Trim();
		if (text.Length != 0)
		{
			Dictionary<string, int> dictionary = LoadLastArticleMap();
			dictionary[text] = articleNumber;
			SaveLastArticleMap(dictionary);
		}
	}

	private static Dictionary<string, int> LoadLastArticleMap()
	{
		string path = Path.Combine(AccountStore.DataRoot(), "nntp-last-article.json");
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
				Dictionary<string, int> dictionary = javaScriptSerializer.Deserialize<Dictionary<string, int>>(File.ReadAllText(path, Encoding.UTF8));
				result = ((dictionary != null) ? dictionary : new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase));
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

	private static void SaveLastArticleMap(Dictionary<string, int> map)
	{
		JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
		File.WriteAllText(Path.Combine(AccountStore.DataRoot(), "nntp-last-article.json"), javaScriptSerializer.Serialize(map), Encoding.UTF8);
	}

	private static string StorePath()
	{
		return Path.Combine(AccountStore.DataRoot(), "nntp-subscriptions.json");
	}
}
