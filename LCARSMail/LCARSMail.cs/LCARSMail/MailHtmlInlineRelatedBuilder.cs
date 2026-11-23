using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using MimeKit;

namespace LCARSMail;

public sealed class MailHtmlInlineRelatedBuilder
{
	private static readonly Regex DataUriPattern = new Regex("src\\s*=\\s*[\"']data:image/([^;]+);base64,([^\"']+)[\"']", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private MailHtmlInlineRelatedBuilder()
	{
	}

	public static MimeEntity BuildHtmlRoot(string html, string plain)
	{
		string text = ((html != null) ? html : "").Trim();
		string text2 = ((plain != null) ? plain : "");
		TextPart textPart;
		if (text.Length == 0)
		{
			textPart = new TextPart("plain");
			textPart.Text = text2;
			return textPart;
		}
		Multipart multipart = new Multipart("related");
		List<string> list = new List<string>();
		string text3 = text;
		int num = 0;
		foreach (Match item in DataUriPattern.Matches(text))
		{
			string mediaSubtype = item.Groups[1].Value.Trim();
			string value = item.Groups[2].Value;
			string text4 = "img" + num + "@lcars.local";
			num = checked(num + 1);
			MimePart mimePart = new MimePart("image", mediaSubtype);
			mimePart.Content = new MimeContent(new MemoryStream(Convert.FromBase64String(value)));
			mimePart.ContentTransferEncoding = ContentEncoding.Base64;
			mimePart.ContentId = text4;
			mimePart.ContentDisposition = new ContentDisposition("inline");
			MimePart entity = mimePart;
			multipart.Add(entity);
			text3 = text3.Replace(item.Value, "src=\"cid:" + text4 + "\"");
			list.Add(text4);
		}
		textPart = new TextPart("html");
		textPart.Text = text3;
		TextPart entity2 = textPart;
		multipart.Add(entity2);
		if (text2.Length == 0)
		{
			text2 = StripTagsLight(text3);
		}
		Multipart multipart2 = new Multipart("alternative");
		multipart2.Add(new TextPart("plain")
		{
			Text = text2
		});
		if (multipart.Count > 1)
		{
			multipart2.Add(multipart);
		}
		else
		{
			multipart2.Add(entity2);
		}
		return multipart2;
	}

	private static string StripTagsLight(string html)
	{
		if (string.IsNullOrEmpty(html))
		{
			return "";
		}
		return Regex.Replace(html, "<[^>]+>", " ").Trim();
	}
}
