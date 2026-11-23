using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSutilities;

[StandardModule]
public sealed class RssFeedStore
{
	private const string FeedsFile = "feeds.txt";

	public static string DataRoot()
	{
		string text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LCARS", "Rss");
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		return text;
	}

	public static List<string> LoadFeedUrls()
	{
		string path = Path.Combine(DataRoot(), "feeds.txt");
		if (!File.Exists(path))
		{
			return new List<string>();
		}
		string[] array = File.ReadAllLines(path, Encoding.UTF8);
		List<string> list = new List<string>();
		string[] array2 = array;
		foreach (string text in array2)
		{
			string text2 = ((text != null) ? text : "").Trim();
			if (text2.Length > 0)
			{
				list.Add(text2);
			}
		}
		return list;
	}

	public static void SaveFeedUrls(List<string> urls)
	{
		string path = Path.Combine(DataRoot(), "feeds.txt");
		File.WriteAllLines(path, urls.ToArray(), Encoding.UTF8);
	}

	public static string FetchFeedXml(string url)
	{
		if (string.IsNullOrWhiteSpace(url))
		{
			return "";
		}
		using WebClient webClient = new WebClient();
		webClient.Headers[HttpRequestHeader.UserAgent] = "LCARSRss/1.0";
		return webClient.DownloadString(url.Trim());
	}
}
