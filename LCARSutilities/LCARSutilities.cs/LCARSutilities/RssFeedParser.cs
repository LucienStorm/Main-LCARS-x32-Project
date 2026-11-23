using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSutilities;

public sealed class RssFeedParser
{
	private RssFeedParser()
	{
	}

	public static List<RssItem> Parse(string xml)
	{
		List<RssItem> list = new List<RssItem>();
		if (string.IsNullOrWhiteSpace(xml))
		{
			return list;
		}
		try
		{
			XDocument xDocument = XDocument.Parse(xml);
			XElement root = xDocument.Root;
			if (root == null)
			{
				return list;
			}
			if (root.Name.LocalName.Equals("rss", StringComparison.OrdinalIgnoreCase))
			{
				foreach (XElement item in from x in root.Descendants()
					where Operators.CompareString(x.Name.LocalName, "item", TextCompare: false) == 0
					select x)
				{
					list.Add(ParseRssItem(item));
				}
			}
			else if (root.Name.LocalName.Equals("feed", StringComparison.OrdinalIgnoreCase))
			{
				foreach (XElement item2 in from x in root.Elements()
					where Operators.CompareString(x.Name.LocalName, "entry", TextCompare: false) == 0
					select x)
				{
					list.Add(ParseAtomEntry(item2));
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

	private static RssItem ParseRssItem(XElement item)
	{
		RssItem rssItem = new RssItem();
		rssItem.Title = ElementText(item, "title");
		rssItem.Link = ElementText(item, "link");
		rssItem.Description = ElementText(item, "description");
		string s = ElementText(item, "pubDate");
		if (DateTime.TryParse(s, out var result))
		{
			rssItem.PublishedUtc = result.ToUniversalTime();
		}
		return rssItem;
	}

	private static RssItem ParseAtomEntry(XElement entry)
	{
		RssItem rssItem = new RssItem();
		rssItem.Title = ElementText(entry, "title");
		XElement xElement = entry.Elements().FirstOrDefault([SpecialName] (XElement x) => Operators.CompareString(x.Name.LocalName, "link", TextCompare: false) == 0);
		if (xElement != null)
		{
			XAttribute xAttribute = xElement.Attribute("href");
			rssItem.Link = ((xAttribute != null) ? xAttribute.Value : "");
		}
		rssItem.Description = ElementText(entry, "summary");
		if (rssItem.Description.Length == 0)
		{
			rssItem.Description = ElementText(entry, "content");
		}
		string text = ElementText(entry, "updated");
		if (text.Length == 0)
		{
			text = ElementText(entry, "published");
		}
		if (DateTime.TryParse(text, out var result))
		{
			rssItem.PublishedUtc = result.ToUniversalTime();
		}
		return rssItem;
	}

	private static string ElementText(XElement parent, string localName)
	{
		XElement xElement = parent.Elements().FirstOrDefault([SpecialName] (XElement x) => Operators.CompareString(x.Name.LocalName, localName, TextCompare: false) == 0);
		if (xElement == null)
		{
			return "";
		}
		string value = xElement.Value;
		return ((value != null) ? value : "").Trim();
	}
}
