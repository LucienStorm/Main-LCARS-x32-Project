using System;

namespace LCARSutilities;

public class RssItem
{
	public string Title;

	public string Link;

	public string Description;

	public DateTime PublishedUtc;

	public RssItem()
	{
		Title = string.Empty;
		Link = string.Empty;
		Description = string.Empty;
		PublishedUtc = DateTime.MinValue;
	}
}
