using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

public sealed class RssSubscriptionStore
{
	private const string FileName = "rss-feeds.json";

	private RssSubscriptionStore()
	{
	}

	public static List<string> LoadFeedUrls()
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
				JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
				List<string> list = javaScriptSerializer.Deserialize<List<string>>(File.ReadAllText(path, Encoding.UTF8));
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

	public static void SaveFeedUrls(IList<string> urls)
	{
		if (urls == null)
		{
			return;
		}
		List<string> list = new List<string>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string url in urls)
		{
			string text = ((url != null) ? url : "").Trim();
			if (text.Length != 0 && hashSet.Add(text))
			{
				list.Add(text);
			}
		}
		JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
		File.WriteAllText(StorePath(), javaScriptSerializer.Serialize(list), Encoding.UTF8);
	}

	public static void Subscribe(string feedUrl)
	{
		string text = ((feedUrl != null) ? feedUrl : "").Trim();
		if (text.Length == 0)
		{
			return;
		}
		List<string> list = LoadFeedUrls();
		foreach (string item in list)
		{
			if (string.Equals(item, text, StringComparison.OrdinalIgnoreCase))
			{
				return;
			}
		}
		list.Add(text);
		SaveFeedUrls(list);
	}

	private static string StorePath()
	{
		return Path.Combine(AccountStore.DataRoot(), "rss-feeds.json");
	}
}
