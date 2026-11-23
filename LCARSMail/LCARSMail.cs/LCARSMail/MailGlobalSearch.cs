using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace LCARSMail;

public sealed class MailGlobalSearch
{
	private const int MaxFolders = 0;

	private const int MaxPerFolder = 2000;

	private MailGlobalSearch()
	{
	}

	public static List<MailMessageHeader> SearchAccount(MailAccount account, string password, UnifiedMailService mailService, string queryText)
	{
		Dictionary<string, MailMessageHeader> dictionary = new Dictionary<string, MailMessageHeader>(StringComparer.OrdinalIgnoreCase);
		if (account == null || mailService == null)
		{
			return new List<MailMessageHeader>();
		}
		string text = ((queryText != null) ? queryText : "").Trim();
		if (text.Length == 0)
		{
			return new List<MailMessageHeader>();
		}
		if (!string.IsNullOrWhiteSpace(account.Id))
		{
			foreach (MailMessageHeader item in MailSearchIndex.Search(account.Id, text))
			{
				if (item != null)
				{
					MailMessageHeader mailMessageHeader = item;
					string key = ((mailMessageHeader.Folder != null) ? mailMessageHeader.Folder : "") + "|" + item.Uid;
					if (!dictionary.ContainsKey(key))
					{
						dictionary[key] = item;
					}
				}
			}
		}
		List<MailFolderInfo> list = mailService.ListFolders(account, password);
		int num = 0;
		foreach (MailFolderInfo item2 in list)
		{
			if (item2 == null)
			{
				continue;
			}
			if (false)
			{
				if (num >= 0)
				{
					break;
				}
				num = checked(num + 1);
			}
			List<MailMessageHeader> list2 = mailService.SearchFolderHeaders(account, password, item2.FullName, text, 2000);
			foreach (MailMessageHeader item3 in list2)
			{
				if (item3 != null)
				{
					string key2 = item2.FullName + "|" + item3.Uid;
					if (!dictionary.ContainsKey(key2))
					{
						dictionary[key2] = item3;
					}
				}
			}
		}
		List<MailMessageHeader> list3 = new List<MailMessageHeader>(dictionary.Values);
		list3.Sort([SpecialName] (MailMessageHeader a, MailMessageHeader b) => DateTime.Compare(b.DateUtc, a.DateUtc));
		return list3;
	}
}
