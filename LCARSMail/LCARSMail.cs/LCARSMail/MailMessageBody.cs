using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LCARSMail;

public class MailMessageBody
{
	public string TextBody;

	public string HtmlBody;

	public bool RequiresPgpDecrypt;

	public List<MailAttachmentInfo> Attachments;

	public List<string> IcsCalendarParts;

	public MailMessageBody()
	{
		Attachments = new List<MailAttachmentInfo>();
		IcsCalendarParts = new List<string>();
	}

	public string GetReadingPaneText()
	{
		if (!string.IsNullOrWhiteSpace(TextBody))
		{
			return TextBody;
		}
		if (!string.IsNullOrWhiteSpace(HtmlBody))
		{
			return StripHtmlLight(HtmlBody);
		}
		return "";
	}

	private static string StripHtmlLight(string html)
	{
		if (string.IsNullOrEmpty(html))
		{
			return "";
		}
		string input = Regex.Replace(html, "<[^>]+>", " ");
		input = Regex.Replace(input, "\\s+", " ");
		return input.Trim();
	}
}
