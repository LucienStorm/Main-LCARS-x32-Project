using System.Diagnostics;

namespace LCARSMail;

public class RssFeedItem
{
	public string Title;

	public string Link;

	public string Description;

	public string EnclosureUrl;

	public string EnclosureType;

	[DebuggerNonUserCode]
	public RssFeedItem()
	{
	}
}
