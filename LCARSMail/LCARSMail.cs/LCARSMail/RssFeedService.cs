using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Xml;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

public sealed class RssFeedService
{
	public static string LastError;

	private RssFeedService()
	{
	}

	public static List<RssFeedItem> FetchItems(string feedUrl)
	{
		LastError = null;
		string text = ((feedUrl != null) ? feedUrl : "").Trim();
		List<RssFeedItem> result;
		if (text.Length == 0)
		{
			LastError = "Feed URL is required.";
			result = null;
		}
		else
		{
			try
			{
				HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(text);
				httpWebRequest.Timeout = 30000;
				httpWebRequest.UserAgent = "LCARSMail/1.0";
				using HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
				using StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8);
				result = ParseFeedXml(streamReader.ReadToEnd());
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Exception ex2 = ex;
				LastError = ex2.Message;
				result = null;
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}

	public static void PlayEnclosure(RssFeedItem item)
	{
		if (item == null || string.IsNullOrWhiteSpace(item.EnclosureUrl))
		{
			return;
		}
		LastError = null;
		try
		{
			string text = DownloadEnclosureToTemp(item.EnclosureUrl);
			if (text != null)
			{
				string text2 = Path.Combine(ResolveStartupPath(), "LCARSmedia.exe");
				if (File.Exists(text2))
				{
					ProcessStartInfo processStartInfo = new ProcessStartInfo();
					processStartInfo.FileName = text2;
					processStartInfo.Arguments = "\"" + text + "\"";
					processStartInfo.WorkingDirectory = Path.GetDirectoryName(text2);
					processStartInfo.UseShellExecute = false;
					ProcessStartInfo startInfo = processStartInfo;
					Process.Start(startInfo);
				}
				else
				{
					ProcessStartInfo processStartInfo = new ProcessStartInfo();
					processStartInfo.FileName = text;
					processStartInfo.UseShellExecute = true;
					Process.Start(processStartInfo);
				}
			}
		}
		catch (Exception ex)
		{
			ProjectData.SetProjectError(ex);
			Exception ex2 = ex;
			LastError = ex2.Message;
			ProjectData.ClearProjectError();
		}
	}

	private static string ResolveStartupPath()
	{
		string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		if (!string.IsNullOrWhiteSpace(directoryName))
		{
			return directoryName;
		}
		return AppDomain.CurrentDomain.BaseDirectory;
	}

	private static string DownloadEnclosureToTemp(string url)
	{
		string text = url.Trim();
		if (text.Length == 0)
		{
			return null;
		}
		string text2 = ".media";
		try
		{
			Uri uri = new Uri(text);
			string extension = Path.GetExtension(uri.AbsolutePath);
			if (extension.Length > 1)
			{
				text2 = extension;
			}
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
		string text3 = Path.Combine(Path.GetTempPath(), "lcars-rss-" + Guid.NewGuid().ToString("N") + text2);
		HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(text);
		httpWebRequest.Timeout = 120000;
		httpWebRequest.UserAgent = "LCARSMail/1.0";
		using (HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse())
		{
			using Stream stream = httpWebResponse.GetResponseStream();
			using FileStream destination = new FileStream(text3, FileMode.Create, FileAccess.Write);
			stream.CopyTo(destination);
		}
		return text3;
	}

	private static List<RssFeedItem> ParseFeedXml(string xml)
	{
		List<RssFeedItem> list = new List<RssFeedItem>();
		if (string.IsNullOrWhiteSpace(xml))
		{
			return list;
		}
		XmlDocument xmlDocument = new XmlDocument();
		xmlDocument.LoadXml(xml);
		XmlNodeList elementsByTagName = xmlDocument.GetElementsByTagName("item");
		if (elementsByTagName == null || elementsByTagName.Count == 0)
		{
			elementsByTagName = xmlDocument.GetElementsByTagName("entry");
		}
		if (elementsByTagName == null)
		{
			return list;
		}
		foreach (XmlNode item in elementsByTagName)
		{
			if (item == null)
			{
				continue;
			}
			RssFeedItem rssFeedItem = new RssFeedItem();
			rssFeedItem.Title = ChildText(item, "title");
			rssFeedItem.Link = ChildLink(item);
			rssFeedItem.Description = ChildText(item, "description");
			if (rssFeedItem.Description.Length == 0)
			{
				rssFeedItem.Description = ChildText(item, "summary");
			}
			XmlNode xmlNode2 = item.SelectSingleNode("enclosure");
			if (xmlNode2 != null && xmlNode2.Attributes != null)
			{
				XmlAttribute xmlAttribute = xmlNode2.Attributes["url"];
				XmlAttribute xmlAttribute2 = xmlNode2.Attributes["type"];
				if (xmlAttribute != null)
				{
					rssFeedItem.EnclosureUrl = xmlAttribute.Value;
				}
				if (xmlAttribute2 != null)
				{
					rssFeedItem.EnclosureType = xmlAttribute2.Value;
				}
			}
			list.Add(rssFeedItem);
		}
		return list;
	}

	private static string ChildText(XmlNode parent, string name)
	{
		XmlNode xmlNode = parent.SelectSingleNode(name);
		if (xmlNode == null)
		{
			return "";
		}
		string innerText = xmlNode.InnerText;
		return ((innerText != null) ? innerText : "").Trim();
	}

	private static string ChildLink(XmlNode parent)
	{
		XmlNode xmlNode = parent.SelectSingleNode("link");
		if (xmlNode == null)
		{
			return "";
		}
		if (xmlNode.Attributes != null && xmlNode.Attributes["href"] != null)
		{
			return xmlNode.Attributes["href"].Value;
		}
		string innerText = xmlNode.InnerText;
		return ((innerText != null) ? innerText : "").Trim();
	}
}
