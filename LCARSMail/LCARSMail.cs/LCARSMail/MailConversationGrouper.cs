using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace LCARSMail;

public sealed class MailConversationGrouper
{
	private static readonly Regex SubjectPrefixPattern = new Regex("^\\s*(re|fwd|fw|aw|sv|antw|vs|enc|回复|答复)\\s*(\\[\\d+\\])?\\s*:\\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

	private static readonly Regex MessageIdTokenPattern = new Regex("<[^>]+>", RegexOptions.Compiled);

	private MailConversationGrouper()
	{
	}

	public static List<MailThreadListItem> BuildThreadList(List<MailMessageHeader> headers)
	{
		List<MailThreadListItem> list = new List<MailThreadListItem>();
		if (headers == null || headers.Count == 0)
		{
			return list;
		}
		Dictionary<string, string> parent = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		checked
		{
			foreach (MailMessageHeader header in headers)
			{
				if (header == null)
				{
					continue;
				}
				List<string> list2 = CollectMessageIds(header);
				if (list2.Count != 0)
				{
					string a = list2[0];
					int num = list2.Count - 1;
					for (int i = 1; i <= num; i++)
					{
						UnionIds(parent, a, list2[i]);
					}
				}
			}
			Dictionary<string, List<MailMessageHeader>> dictionary = new Dictionary<string, List<MailMessageHeader>>(StringComparer.OrdinalIgnoreCase);
			foreach (MailMessageHeader header2 in headers)
			{
				if (header2 != null)
				{
					string key = ThreadKey(header2, parent);
					if (!dictionary.ContainsKey(key))
					{
						dictionary[key] = new List<MailMessageHeader>();
					}
					dictionary[key].Add(header2);
				}
			}
			foreach (KeyValuePair<string, List<MailMessageHeader>> item in dictionary)
			{
				List<MailMessageHeader> value = item.Value;
				value.Sort([SpecialName] (MailMessageHeader mailMessageHeader, MailMessageHeader b) => DateTime.Compare(b.DateUtc, mailMessageHeader.DateUtc));
				MailMessageHeader representative = value[0];
				list.Add(new MailThreadListItem
				{
					Representative = representative,
					Messages = value,
					ThreadKey = item.Key
				});
			}
			list.Sort([SpecialName] (MailThreadListItem mailThreadListItem, MailThreadListItem b) => DateTime.Compare(b.Representative.DateUtc, mailThreadListItem.Representative.DateUtc));
			return list;
		}
	}

	public static string ThreadKey(MailMessageHeader hdr)
	{
		return ThreadKey(hdr, null);
	}

	private static string ThreadKey(MailMessageHeader hdr, Dictionary<string, string> parent)
	{
		if (hdr == null)
		{
			return "";
		}
		List<string> list = CollectMessageIds(hdr);
		if (list.Count > 0 && parent != null)
		{
			return "id:" + FindRoot(parent, list[0]).ToLowerInvariant();
		}
		if (list.Count > 0)
		{
			return "id:" + list[0].ToLowerInvariant();
		}
		MailMessageHeader mailMessageHeader = hdr;
		string text = NormalizeSubject((mailMessageHeader.Subject != null) ? mailMessageHeader.Subject : "");
		mailMessageHeader = hdr;
		string text2 = ((mailMessageHeader.From != null) ? mailMessageHeader.From : "").Trim().ToLowerInvariant();
		return "sub:" + text + "|" + text2;
	}

	private static List<string> CollectMessageIds(MailMessageHeader hdr)
	{
		List<string> list = new List<string>();
		if (hdr == null)
		{
			return list;
		}
		AppendId(list, hdr.MessageId);
		AppendId(list, hdr.InReplyTo);
		List<string>.Enumerator enumerator = default(List<string>.Enumerator);
		try
		{
			enumerator = ParseReferences((hdr.References != null) ? hdr.References : "").GetEnumerator();
			while (enumerator.MoveNext())
			{
				string current = enumerator.Current;
				AppendId(list, current);
			}
		}
		finally
		{
			((IDisposable)enumerator/*cast due to .constrained prefix*/).Dispose();
		}
		return list;
	}

	private static void AppendId(List<string> ids, string raw)
	{
		string text = NormalizeMessageId(raw);
		if (text.Length == 0)
		{
			return;
		}
		foreach (string id in ids)
		{
			if (string.Equals(id, text, StringComparison.OrdinalIgnoreCase))
			{
				return;
			}
		}
		ids.Add(text);
	}

	private static List<string> ParseReferences(string referencesHeader)
	{
		List<string> list = new List<string>();
		string text = ((referencesHeader != null) ? referencesHeader : "").Trim();
		if (text.Length == 0)
		{
			return list;
		}
		foreach (Match item in MessageIdTokenPattern.Matches(text))
		{
			string text2 = NormalizeMessageId(item.Value);
			if (text2.Length > 0)
			{
				list.Add(text2);
			}
		}
		return list;
	}

	private static string NormalizeMessageId(string raw)
	{
		string text = ((raw != null) ? raw : "").Trim();
		if (text.Length == 0)
		{
			return "";
		}
		if (text.StartsWith("<") && text.EndsWith(">") && text.Length > 2)
		{
			text = text.Substring(1, checked(text.Length - 2)).Trim();
		}
		return text;
	}

	private static void UnionIds(Dictionary<string, string> parent, string a, string b)
	{
		string text = NormalizeMessageId(a);
		string text2 = NormalizeMessageId(b);
		if (text.Length != 0 && text2.Length != 0)
		{
			string text3 = FindRoot(parent, text);
			string text4 = FindRoot(parent, text2);
			if (!string.Equals(text3, text4, StringComparison.OrdinalIgnoreCase))
			{
				parent[text4] = text3;
			}
		}
	}

	private static string FindRoot(Dictionary<string, string> parent, string id)
	{
		string text = NormalizeMessageId(id);
		if (text.Length == 0)
		{
			return "";
		}
		if (parent == null)
		{
			return text;
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		while (parent.ContainsKey(text) && hashSet.Add(text))
		{
			text = parent[text];
		}
		string text2 = NormalizeMessageId(id);
		while (parent.ContainsKey(text2) && !string.Equals(text2, text, StringComparison.OrdinalIgnoreCase))
		{
			string text3 = parent[text2];
			parent[text2] = text;
			text2 = text3;
		}
		return text;
	}

	public static string NormalizeSubject(string subject)
	{
		string text = ((subject != null) ? subject : "").Trim();
		string a;
		do
		{
			a = text;
			text = SubjectPrefixPattern.Replace(text, "");
			text = text.Trim();
		}
		while (!string.Equals(a, text, StringComparison.Ordinal));
		return text.ToLowerInvariant();
	}
}
