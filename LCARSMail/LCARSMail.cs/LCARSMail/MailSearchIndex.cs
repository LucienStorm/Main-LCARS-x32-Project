using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Web.Script.Serialization;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

public sealed class MailSearchIndex
{
	private class IndexEntry
	{
		public string Folder;

		public ulong Uid;

		public string Subject;

		public string FromLine;

		public string ToLine;

		public string CcLine;

		public string BodyPreview;

		public DateTime DateUtc;

		public bool IsSeen;

		public long SizeBytes;

		public string MessageId;

		public string InReplyTo;

		public string ReferencesHeader;

		public string ReplyToLine;

		public string ListIdLine;

		public string ReturnPathLine;

		public int AttachmentCount;

		[DebuggerNonUserCode]
		public IndexEntry()
		{
		}
	}

	public class IndexBodyTarget
	{
		public string Folder;

		public ulong Uid;

		[DebuggerNonUserCode]
		public IndexBodyTarget()
		{
		}
	}

	private const string IndexSubdir = "search-index";

	private MailSearchIndex()
	{
	}

	public static void UpsertHeaders(string accountId, string folder, List<MailMessageHeader> headers)
	{
		if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(folder) || headers == null || headers.Count == 0)
		{
			return;
		}
		Dictionary<string, IndexEntry> dictionary = LoadMap(accountId);
		foreach (MailMessageHeader header in headers)
		{
			if (header != null)
			{
				string key = folder + "|" + header.Uid;
				string bodyPreview = "";
				if (dictionary.ContainsKey(key))
				{
					IndexEntry indexEntry = dictionary[key];
					bodyPreview = ((indexEntry.BodyPreview != null) ? indexEntry.BodyPreview : "");
				}
				int attachmentCount = 0;
				if (dictionary.ContainsKey(key))
				{
					attachmentCount = dictionary[key].AttachmentCount;
				}
				if (header.HeaderAttachmentCount >= 0)
				{
					attachmentCount = header.HeaderAttachmentCount;
				}
				IndexEntry indexEntry2 = new IndexEntry
				{
					Folder = folder,
					Uid = header.Uid
				};
				MailMessageHeader mailMessageHeader = header;
				indexEntry2.Subject = ((mailMessageHeader.Subject != null) ? mailMessageHeader.Subject : "");
				MailMessageHeader mailMessageHeader2 = header;
				indexEntry2.FromLine = ((mailMessageHeader2.From != null) ? mailMessageHeader2.From : "");
				MailMessageHeader mailMessageHeader3 = header;
				indexEntry2.ToLine = ((mailMessageHeader3.ToAddresses != null) ? mailMessageHeader3.ToAddresses : "");
				MailMessageHeader mailMessageHeader4 = header;
				indexEntry2.CcLine = ((mailMessageHeader4.CcAddresses != null) ? mailMessageHeader4.CcAddresses : "");
				indexEntry2.DateUtc = header.DateUtc;
				indexEntry2.BodyPreview = bodyPreview;
				indexEntry2.IsSeen = header.IsSeen;
				indexEntry2.SizeBytes = header.SizeBytes;
				indexEntry2.AttachmentCount = attachmentCount;
				MailMessageHeader mailMessageHeader5 = header;
				indexEntry2.MessageId = ((mailMessageHeader5.MessageId != null) ? mailMessageHeader5.MessageId : "");
				MailMessageHeader mailMessageHeader6 = header;
				indexEntry2.InReplyTo = ((mailMessageHeader6.InReplyTo != null) ? mailMessageHeader6.InReplyTo : "");
				MailMessageHeader mailMessageHeader7 = header;
				indexEntry2.ReferencesHeader = ((mailMessageHeader7.References != null) ? mailMessageHeader7.References : "");
				MailMessageHeader mailMessageHeader8 = header;
				indexEntry2.ReplyToLine = ((mailMessageHeader8.ReplyTo != null) ? mailMessageHeader8.ReplyTo : "");
				MailMessageHeader mailMessageHeader9 = header;
				indexEntry2.ListIdLine = ((mailMessageHeader9.ListId != null) ? mailMessageHeader9.ListId : "");
				MailMessageHeader mailMessageHeader10 = header;
				indexEntry2.ReturnPathLine = ((mailMessageHeader10.ReturnPath != null) ? mailMessageHeader10.ReturnPath : "");
				dictionary[key] = indexEntry2;
			}
		}
		SaveMap(accountId, dictionary);
		foreach (MailMessageHeader header2 in headers)
		{
			if (header2 != null)
			{
				string key2 = folder + "|" + header2.Uid;
				IndexEntry entry = dictionary[key2];
				ulong uid = header2.Uid;
				string subject = header2.Subject;
				string fromLine = header2.From;
				string bodyText = ComposeFtsBodyText("", entry, accountId);
				MailMessageHeader mailMessageHeader10 = header2;
				string toLine = ((mailMessageHeader10.ToAddresses != null) ? mailMessageHeader10.ToAddresses : "");
				MailMessageHeader mailMessageHeader9 = header2;
				MailSearchGlodaFts.UpsertMessage(accountId, folder, uid, subject, fromLine, bodyText, toLine, (mailMessageHeader9.CcAddresses != null) ? mailMessageHeader9.CcAddresses : "", header2.DateUtc);
			}
		}
	}

	public static void UpsertBodyPreview(string accountId, string folder, ulong uid, string bodyPreview, int attachmentCount = -1)
	{
		if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(folder))
		{
			return;
		}
		Dictionary<string, IndexEntry> dictionary = LoadMap(accountId);
		string key = folder + "|" + uid;
		if (dictionary.ContainsKey(key))
		{
			string text = ((bodyPreview != null) ? bodyPreview : "").Trim();
			if (text.Length > 64000)
			{
				text = text.Substring(0, 64000);
			}
			dictionary[key].BodyPreview = text;
			if (attachmentCount >= 0)
			{
				dictionary[key].AttachmentCount = attachmentCount;
			}
			SaveMap(accountId, dictionary);
			IndexEntry indexEntry = dictionary[key];
			MailSearchGlodaFts.UpsertMessage(accountId, folder, uid, indexEntry.Subject, indexEntry.FromLine, ComposeFtsBodyText(text, indexEntry, accountId), indexEntry.ToLine, indexEntry.CcLine, indexEntry.DateUtc);
		}
	}

	public static int RebuildFtsHeaderCorpusForAccount(string accountId, int maxRows = 500)
	{
		if (string.IsNullOrWhiteSpace(accountId))
		{
			return 0;
		}
		Dictionary<string, IndexEntry> dictionary = LoadMap(accountId);
		if (dictionary == null || dictionary.Count == 0)
		{
			return 0;
		}
		List<string> list = new List<string>(dictionary.Keys);
		list.Sort(StringComparer.Ordinal);
		int corpusRebuildStart = MailSearchIndexCursor.GetCorpusRebuildStart(accountId, list.Count);
		int num = 0;
		for (int i = 0; i < list.Count; i = checked(i + 1))
		{
			if (maxRows > 0 && num >= maxRows)
			{
				break;
			}
			int index = checked(corpusRebuildStart + i) % list.Count;
			string key = list[index];
			IndexEntry indexEntry = dictionary[key];
			if (indexEntry != null)
			{
				IndexEntry indexEntry2 = indexEntry;
				string preview = ((indexEntry2.BodyPreview != null) ? indexEntry2.BodyPreview : "");
				MailSearchGlodaFts.UpsertMessage(accountId, indexEntry.Folder, indexEntry.Uid, indexEntry.Subject, indexEntry.FromLine, ComposeFtsBodyText(preview, indexEntry, accountId), indexEntry.ToLine, indexEntry.CcLine, indexEntry.DateUtc);
				num = checked(num + 1);
			}
		}
		if (num > 0)
		{
			MailSearchIndexCursor.AdvanceCorpusRebuildCursor(accountId, list.Count, num);
		}
		return num;
	}

	public static int BackfillGlodaFtsMetaDates(string accountId, int maxRows = 4000)
	{
		if (string.IsNullOrWhiteSpace(accountId))
		{
			return 0;
		}
		Dictionary<string, IndexEntry> dictionary = LoadMap(accountId);
		if (dictionary == null || dictionary.Count == 0)
		{
			return 0;
		}
		List<string> list = new List<string>();
		foreach (KeyValuePair<string, IndexEntry> item in dictionary)
		{
			if (item.Value != null && DateTime.Compare(item.Value.DateUtc, DateTime.MinValue) != 0)
			{
				list.Add(item.Key);
			}
		}
		if (list.Count == 0)
		{
			return 0;
		}
		list.Sort(StringComparer.Ordinal);
		int metaBackfillStart = MailSearchIndexCursor.GetMetaBackfillStart(accountId, list.Count);
		int num = 0;
		for (int i = 0; i < list.Count; i = checked(i + 1))
		{
			if (maxRows > 0 && num >= maxRows)
			{
				break;
			}
			int index = checked(metaBackfillStart + i) % list.Count;
			IndexEntry indexEntry = dictionary[list[index]];
			if (indexEntry != null && DateTime.Compare(indexEntry.DateUtc, DateTime.MinValue) != 0)
			{
				MailSearchGlodaFts.TrySetMetaDateUtc(accountId, indexEntry.Folder, indexEntry.Uid, indexEntry.DateUtc);
				num = checked(num + 1);
			}
		}
		if (num > 0)
		{
			MailSearchIndexCursor.AdvanceMetaBackfillCursor(accountId, list.Count, num);
		}
		return num;
	}

	private static string ComposeFtsBodyText(string preview, IndexEntry entry, string accountId = null)
	{
		StringBuilder stringBuilder = new StringBuilder();
		string text = ((preview != null) ? preview : "").Trim();
		if (text.Length > 0)
		{
			stringBuilder.Append(text);
			AppendHeaderLine(stringBuilder, "X-LCARS-HasBody", "yes");
		}
		else
		{
			AppendHeaderLine(stringBuilder, "X-LCARS-HasBody", "no");
		}
		if (entry == null)
		{
			return stringBuilder.ToString();
		}
		string text2 = ((accountId != null) ? accountId : "").Trim();
		if (text2.Length > 0)
		{
			AppendHeaderLine(stringBuilder, "X-LCARS-Account", text2);
		}
		AppendHeaderLine(stringBuilder, "Subject", entry.Subject);
		AppendHeaderLine(stringBuilder, "From", entry.FromLine);
		AppendHeaderLine(stringBuilder, "To", entry.ToLine);
		AppendHeaderLine(stringBuilder, "Cc", entry.CcLine);
		checked
		{
			if (DateTime.Compare(entry.DateUtc, DateTime.MinValue) != 0)
			{
				AppendHeaderLine(stringBuilder, "Date", entry.DateUtc.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss'Z'"));
				DateTime dateTime = entry.DateUtc.ToUniversalTime();
				DateTime dateTime2 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
				AppendHeaderLine(stringBuilder, "X-LCARS-DateUnix", ((long)Math.Round((dateTime - dateTime2).TotalSeconds)).ToString());
			}
			if (entry.Uid != 0)
			{
				AppendHeaderLine(stringBuilder, "X-LCARS-Uid", entry.Uid.ToString());
			}
			AppendHeaderLine(stringBuilder, "Message-Id", entry.MessageId);
			AppendHeaderLine(stringBuilder, "In-Reply-To", entry.InReplyTo);
			AppendHeaderLine(stringBuilder, "References", entry.ReferencesHeader);
			AppendHeaderLine(stringBuilder, "Reply-To", entry.ReplyToLine);
			AppendHeaderLine(stringBuilder, "List-Id", entry.ListIdLine);
			AppendHeaderLine(stringBuilder, "Return-Path", entry.ReturnPathLine);
			AppendHeaderLine(stringBuilder, "X-LCARS-Seen", entry.IsSeen ? "read" : "unread");
			if (entry.SizeBytes > 0)
			{
				AppendHeaderLine(stringBuilder, "Size", entry.SizeBytes.ToString());
				AppendHeaderLine(stringBuilder, "X-LCARS-MessageSize", entry.SizeBytes.ToString());
			}
			string text3 = ((entry.Folder != null) ? entry.Folder : "").Trim();
			if (text3.Length > 0)
			{
				string value = text3;
				int num = text3.LastIndexOf('/');
				if (num >= 0 && num < text3.Length - 1)
				{
					value = text3.Substring(num + 1).Trim();
				}
				AppendHeaderLine(stringBuilder, "Folder", value);
				AppendHeaderLine(stringBuilder, "X-LCARS-FolderPath", text3);
			}
			string text4 = ResolveThreadRootToken(entry);
			if (text4.Length > 0)
			{
				AppendHeaderLine(stringBuilder, "X-LCARS-ThreadRoot", text4);
			}
			if (entry.AttachmentCount > 0)
			{
				AppendHeaderLine(stringBuilder, "X-LCARS-HasAttachment", "yes");
				AppendHeaderLine(stringBuilder, "X-LCARS-AttachmentCount", entry.AttachmentCount.ToString());
			}
			else
			{
				AppendHeaderLine(stringBuilder, "X-LCARS-HasAttachment", "no");
			}
			return stringBuilder.ToString();
		}
	}

	private static string ResolveThreadRootToken(IndexEntry entry)
	{
		if (entry == null)
		{
			return "";
		}
		string text = NormalizeMessageIdToken(entry.InReplyTo);
		if (text.Length > 0)
		{
			return text;
		}
		string text2 = ((entry.ReferencesHeader != null) ? entry.ReferencesHeader : "").Trim();
		if (text2.Length > 0)
		{
			int num = text2.IndexOf('<');
			if (num >= 0)
			{
				int num2 = text2.IndexOf('>', num);
				if (num2 > num)
				{
					return text2.Substring(num, checked(num2 - num + 1)).Trim();
				}
			}
			int num3 = text2.IndexOf(' ');
			if (num3 > 0)
			{
				return text2.Substring(0, num3).Trim();
			}
			return text2;
		}
		return NormalizeMessageIdToken(entry.MessageId);
	}

	private static string NormalizeMessageIdToken(string raw)
	{
		string text = ((raw != null) ? raw : "").Trim();
		if (text.Length == 0)
		{
			return "";
		}
		if (text.StartsWith("<") && text.EndsWith(">"))
		{
			return text;
		}
		if (text.Contains("@"))
		{
			return "<" + text.Trim('<', '>') + ">";
		}
		return text;
	}

	private static void AppendHeaderLine(StringBuilder sb, string label, string value)
	{
		string text = ((value != null) ? value : "").Trim();
		if (text.Length != 0)
		{
			if (sb.Length > 0)
			{
				sb.AppendLine();
			}
			sb.Append(label).Append(": ").Append(text);
		}
	}

	public static DateTime TryGetIndexedDateUtc(string accountId, string folder, ulong uid)
	{
		if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(folder))
		{
			return DateTime.MinValue;
		}
		Dictionary<string, IndexEntry> dictionary = LoadMap(accountId);
		string key = folder + "|" + uid;
		if (!dictionary.ContainsKey(key))
		{
			return DateTime.MinValue;
		}
		return dictionary[key].DateUtc;
	}

	public static int CountEntriesNeedingBodyPreview(string accountId)
	{
		if (string.IsNullOrWhiteSpace(accountId))
		{
			return 0;
		}
		int num = 0;
		foreach (KeyValuePair<string, IndexEntry> item in LoadMap(accountId))
		{
			IndexEntry value = item.Value;
			if (value != null && string.IsNullOrWhiteSpace(value.BodyPreview))
			{
				num = checked(num + 1);
			}
		}
		return num;
	}

	public static List<IndexBodyTarget> ListEntriesNeedingBodyPreview(string accountId, int maxCount = 500)
	{
		List<IndexBodyTarget> list = new List<IndexBodyTarget>();
		if (string.IsNullOrWhiteSpace(accountId))
		{
			return list;
		}
		Dictionary<string, IndexEntry> dictionary = LoadMap(accountId);
		foreach (KeyValuePair<string, IndexEntry> item in dictionary)
		{
			IndexEntry value = item.Value;
			if (value != null && string.IsNullOrWhiteSpace(value.BodyPreview))
			{
				list.Add(new IndexBodyTarget
				{
					Folder = value.Folder,
					Uid = value.Uid
				});
				if (maxCount > 0 && list.Count >= maxCount)
				{
					break;
				}
			}
		}
		return list;
	}

	public static List<IndexBodyTarget> ListEntriesWithBodyPreview(string accountId, int maxCount = 64)
	{
		List<IndexBodyTarget> list = new List<IndexBodyTarget>();
		if (string.IsNullOrWhiteSpace(accountId))
		{
			return list;
		}
		foreach (KeyValuePair<string, IndexEntry> item in LoadMap(accountId))
		{
			IndexEntry value = item.Value;
			if (value != null && !string.IsNullOrWhiteSpace(value.BodyPreview))
			{
				list.Add(new IndexBodyTarget
				{
					Folder = value.Folder,
					Uid = value.Uid
				});
				if (maxCount > 0 && list.Count >= maxCount)
				{
					break;
				}
			}
		}
		return list;
	}

	public static void RemoveMessage(string accountId, string folder, ulong uid)
	{
		if (!string.IsNullOrWhiteSpace(accountId))
		{
			Dictionary<string, IndexEntry> dictionary = LoadMap(accountId);
			string key = ((folder != null) ? folder : "") + "|" + uid;
			if (dictionary.ContainsKey(key))
			{
				dictionary.Remove(key);
				SaveMap(accountId, dictionary);
			}
			MailSearchGlodaFts.RemoveMessage(accountId, folder, uid);
		}
	}

	public static int CountEntries(string accountId)
	{
		if (string.IsNullOrWhiteSpace(accountId))
		{
			return 0;
		}
		return LoadMap(accountId).Count;
	}

	public static int CountDistinctIndexedFolders(string accountId)
	{
		if (string.IsNullOrWhiteSpace(accountId))
		{
			return 0;
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (KeyValuePair<string, IndexEntry> item in LoadMap(accountId))
		{
			IndexEntry value = item.Value;
			if (value != null)
			{
				IndexEntry indexEntry = value;
				string text = ((indexEntry.Folder != null) ? indexEntry.Folder : "").Trim();
				if (text.Length > 0)
				{
					hashSet.Add(text);
				}
			}
		}
		return hashSet.Count;
	}

	public static string TryGetBodyPreviewSnippet(string accountId, string folder, ulong uid, int maxLen = 80)
	{
		if (string.IsNullOrWhiteSpace(accountId))
		{
			return "";
		}
		Dictionary<string, IndexEntry> dictionary = LoadMap(accountId);
		string key = ((folder != null) ? folder : "") + "|" + uid;
		if (!dictionary.ContainsKey(key))
		{
			return "";
		}
		IndexEntry indexEntry = dictionary[key];
		string text = ((indexEntry.BodyPreview != null) ? indexEntry.BodyPreview : "").Trim();
		if (text.Length == 0)
		{
			return "";
		}
		text = text.Replace("\r", " ").Replace("\n", " ");
		if (text.Length > maxLen)
		{
			text = text.Substring(0, checked(maxLen - 1)) + "…";
		}
		return text;
	}

	public static List<MailMessageHeader> Search(string accountId, string queryText, int maxHits = 5000, int skip = 0)
	{
		List<MailMessageHeader> list = new List<MailMessageHeader>();
		if (string.IsNullOrWhiteSpace(accountId))
		{
			return list;
		}
		string text = ((queryText != null) ? queryText : "").Trim();
		if (text.Length == 0)
		{
			return list;
		}
		string queryText2 = text;
		string text2 = ExtractFolderFilter(ref queryText2);
		int num = ExtractNewerDaysFilter(ref queryText2);
		int num2 = ExtractOlderDaysFilter(ref queryText2);
		bool flag = ExtractUnreadOnlyFilter(ref queryText2);
		bool flag2 = ExtractSeenOnlyFilter(ref queryText2);
		bool flag3 = ExtractHasBodyFilter(ref queryText2);
		bool flag4 = ExtractHasAttachmentFilter(ref queryText2);
		long num3 = ExtractLargerMinBytesFilter(ref queryText2);
		long num4 = ExtractSmallerMaxBytesFilter(ref queryText2);
		string text3 = ExtractToAddressFilter(ref queryText2);
		string text4 = ExtractCcAddressFilter(ref queryText2);
		string text5 = ExtractMessageIdFilter(ref queryText2);
		string text6 = ExtractThreadFilter(ref queryText2);
		string text7 = ExtractInReplyToFilter(ref queryText2);
		string text8 = ExtractReferencesFilter(ref queryText2);
		string text9 = ExtractReplyToFilter(ref queryText2);
		string text10 = ExtractListIdFilter(ref queryText2);
		string text11 = ExtractReturnPathFilter(ref queryText2);
		List<MailMessageHeader> list2 = MailSearchGlodaFts.Search(accountId, queryText2, maxHits, skip);
		checked
		{
			if (list2 != null && list2.Count > 0)
			{
				Dictionary<string, IndexEntry> dictionary = LoadMap(accountId);
				foreach (MailMessageHeader item in list2)
				{
					if (item == null)
					{
						continue;
					}
					MailMessageHeader mailMessageHeader = item;
					string key = ((mailMessageHeader.Folder != null) ? mailMessageHeader.Folder : "") + "|" + item.Uid;
					if (dictionary.ContainsKey(key))
					{
						IndexEntry indexEntry = dictionary[key];
						item.DateUtc = indexEntry.DateUtc;
						if (string.IsNullOrWhiteSpace(item.Subject))
						{
							item.Subject = indexEntry.Subject;
						}
						if (string.IsNullOrWhiteSpace(item.From))
						{
							item.From = indexEntry.FromLine;
						}
						if (string.IsNullOrWhiteSpace(item.ToAddresses))
						{
							item.ToAddresses = indexEntry.ToLine;
						}
						if (string.IsNullOrWhiteSpace(item.CcAddresses))
						{
							item.CcAddresses = indexEntry.CcLine;
						}
						if (string.IsNullOrWhiteSpace(item.MessageId))
						{
							item.MessageId = indexEntry.MessageId;
						}
						if (string.IsNullOrWhiteSpace(item.InReplyTo))
						{
							item.InReplyTo = indexEntry.InReplyTo;
						}
						if (string.IsNullOrWhiteSpace(item.References))
						{
							item.References = indexEntry.ReferencesHeader;
						}
						if (string.IsNullOrWhiteSpace(item.ReplyTo))
						{
							item.ReplyTo = indexEntry.ReplyToLine;
						}
						if (string.IsNullOrWhiteSpace(item.ListId))
						{
							item.ListId = indexEntry.ListIdLine;
						}
						if (string.IsNullOrWhiteSpace(item.ReturnPath))
						{
							item.ReturnPath = indexEntry.ReturnPathLine;
						}
						item.IsSeen = indexEntry.IsSeen;
						if (indexEntry.SizeBytes > 0)
						{
							item.SizeBytes = indexEntry.SizeBytes;
						}
					}
					if (num3 > 0 || num4 > 0)
					{
						long sizeBytes = item.SizeBytes;
						if (sizeBytes <= 0 && dictionary.ContainsKey(key))
						{
							sizeBytes = dictionary[key].SizeBytes;
						}
						if ((num3 > 0 && sizeBytes < num3) || (num4 > 0 && sizeBytes > num4))
						{
							continue;
						}
					}
					if ((flag && item.IsSeen) || (flag2 && !item.IsSeen))
					{
						continue;
					}
					if (text2.Length > 0)
					{
						mailMessageHeader = item;
						if (!FolderMatches((mailMessageHeader.Folder != null) ? mailMessageHeader.Folder : "", text2))
						{
							continue;
						}
					}
					if (text3.Length > 0)
					{
						mailMessageHeader = item;
						if (((mailMessageHeader.ToAddresses != null) ? mailMessageHeader.ToAddresses : "").IndexOf(text3, StringComparison.OrdinalIgnoreCase) < 0)
						{
							continue;
						}
					}
					if (text4.Length > 0)
					{
						mailMessageHeader = item;
						if (((mailMessageHeader.CcAddresses != null) ? mailMessageHeader.CcAddresses : "").IndexOf(text4, StringComparison.OrdinalIgnoreCase) < 0)
						{
							continue;
						}
					}
					if (text5.Length > 0)
					{
						mailMessageHeader = item;
						if (((mailMessageHeader.MessageId != null) ? mailMessageHeader.MessageId : "").IndexOf(text5, StringComparison.OrdinalIgnoreCase) < 0)
						{
							continue;
						}
					}
					if (text6.Length > 0)
					{
						mailMessageHeader = item;
						string key2 = ((mailMessageHeader.Folder != null) ? mailMessageHeader.Folder : "") + "|" + item.Uid;
						IndexEntry entry = null;
						if (dictionary.ContainsKey(key2))
						{
							entry = dictionary[key2];
						}
						if (!HeaderMatchesThreadFilter(item, entry, text6))
						{
							continue;
						}
					}
					if (text7.Length > 0)
					{
						mailMessageHeader = item;
						if (((mailMessageHeader.InReplyTo != null) ? mailMessageHeader.InReplyTo : "").IndexOf(text7, StringComparison.OrdinalIgnoreCase) < 0)
						{
							continue;
						}
					}
					if (text8.Length > 0)
					{
						mailMessageHeader = item;
						if (((mailMessageHeader.References != null) ? mailMessageHeader.References : "").IndexOf(text8, StringComparison.OrdinalIgnoreCase) < 0)
						{
							continue;
						}
					}
					if (text9.Length > 0)
					{
						mailMessageHeader = item;
						if (((mailMessageHeader.ReplyTo != null) ? mailMessageHeader.ReplyTo : "").IndexOf(text9, StringComparison.OrdinalIgnoreCase) < 0)
						{
							continue;
						}
					}
					if (text10.Length > 0)
					{
						mailMessageHeader = item;
						if (((mailMessageHeader.ListId != null) ? mailMessageHeader.ListId : "").IndexOf(text10, StringComparison.OrdinalIgnoreCase) < 0)
						{
							continue;
						}
					}
					if (text11.Length > 0)
					{
						mailMessageHeader = item;
						if (((mailMessageHeader.ReturnPath != null) ? mailMessageHeader.ReturnPath : "").IndexOf(text11, StringComparison.OrdinalIgnoreCase) < 0)
						{
							continue;
						}
					}
					if ((!flag3 || (dictionary.ContainsKey(key) && !string.IsNullOrWhiteSpace(dictionary[key].BodyPreview))) && (!flag4 || (dictionary.ContainsKey(key) && dictionary[key].AttachmentCount > 0)) && (num <= 0 || DateTime.Compare(item.DateUtc, DateTime.MinValue) == 0 || DateTime.Compare(item.DateUtc, DateTime.UtcNow.AddDays(-num)) >= 0) && (num2 <= 0 || DateTime.Compare(item.DateUtc, DateTime.MinValue) == 0 || DateTime.Compare(item.DateUtc, DateTime.UtcNow.AddDays(-num2)) <= 0))
					{
						list.Add(item);
						if (maxHits > 0 && list.Count >= maxHits)
						{
							break;
						}
					}
				}
				if (list.Count > 0)
				{
					list.Sort([SpecialName] (MailMessageHeader a, MailMessageHeader b) => DateTime.Compare(b.DateUtc, a.DateUtc));
					return list;
				}
			}
			List<string[]> orGroups = ParseGlodaOrGroups(queryText2);
			Dictionary<string, IndexEntry> dictionary2 = LoadMap(accountId);
			foreach (KeyValuePair<string, IndexEntry> item2 in dictionary2)
			{
				IndexEntry value = item2.Value;
				if (value == null || !GlodaEntryMatches(value, orGroups) || (flag && value.IsSeen) || (flag2 && !value.IsSeen))
				{
					continue;
				}
				if (text2.Length > 0)
				{
					IndexEntry indexEntry2 = value;
					if (!FolderMatches((indexEntry2.Folder != null) ? indexEntry2.Folder : "", text2))
					{
						continue;
					}
				}
				if (text3.Length > 0)
				{
					IndexEntry indexEntry2 = value;
					if (((indexEntry2.ToLine != null) ? indexEntry2.ToLine : "").IndexOf(text3, StringComparison.OrdinalIgnoreCase) < 0)
					{
						continue;
					}
				}
				if (text4.Length > 0)
				{
					IndexEntry indexEntry2 = value;
					if (((indexEntry2.CcLine != null) ? indexEntry2.CcLine : "").IndexOf(text4, StringComparison.OrdinalIgnoreCase) < 0)
					{
						continue;
					}
				}
				if (text5.Length > 0)
				{
					IndexEntry indexEntry2 = value;
					if (((indexEntry2.MessageId != null) ? indexEntry2.MessageId : "").IndexOf(text5, StringComparison.OrdinalIgnoreCase) < 0)
					{
						continue;
					}
				}
				if (text6.Length > 0 && !EntryMatchesThreadFilter(value, text6))
				{
					continue;
				}
				if (text7.Length > 0)
				{
					IndexEntry indexEntry2 = value;
					if (((indexEntry2.InReplyTo != null) ? indexEntry2.InReplyTo : "").IndexOf(text7, StringComparison.OrdinalIgnoreCase) < 0)
					{
						continue;
					}
				}
				if (text8.Length > 0)
				{
					IndexEntry indexEntry2 = value;
					if (((indexEntry2.ReferencesHeader != null) ? indexEntry2.ReferencesHeader : "").IndexOf(text8, StringComparison.OrdinalIgnoreCase) < 0)
					{
						continue;
					}
				}
				if (text9.Length > 0)
				{
					IndexEntry indexEntry2 = value;
					if (((indexEntry2.ReplyToLine != null) ? indexEntry2.ReplyToLine : "").IndexOf(text9, StringComparison.OrdinalIgnoreCase) < 0)
					{
						continue;
					}
				}
				if (text10.Length > 0)
				{
					IndexEntry indexEntry2 = value;
					if (((indexEntry2.ListIdLine != null) ? indexEntry2.ListIdLine : "").IndexOf(text10, StringComparison.OrdinalIgnoreCase) < 0)
					{
						continue;
					}
				}
				if (text11.Length > 0)
				{
					IndexEntry indexEntry2 = value;
					if (((indexEntry2.ReturnPathLine != null) ? indexEntry2.ReturnPathLine : "").IndexOf(text11, StringComparison.OrdinalIgnoreCase) < 0)
					{
						continue;
					}
				}
				if ((!flag3 || !string.IsNullOrWhiteSpace(value.BodyPreview)) && (!flag4 || value.AttachmentCount > 0) && (num3 <= 0 || value.SizeBytes >= num3) && (num4 <= 0 || value.SizeBytes <= num4) && (num <= 0 || DateTime.Compare(value.DateUtc, DateTime.MinValue) == 0 || DateTime.Compare(value.DateUtc, DateTime.UtcNow.AddDays(-num)) >= 0) && (num2 <= 0 || DateTime.Compare(value.DateUtc, DateTime.MinValue) == 0 || DateTime.Compare(value.DateUtc, DateTime.UtcNow.AddDays(-num2)) <= 0))
				{
					list.Add(new MailMessageHeader
					{
						Folder = value.Folder,
						Uid = value.Uid,
						Subject = value.Subject,
						From = value.FromLine,
						ToAddresses = value.ToLine,
						CcAddresses = value.CcLine,
						MessageId = value.MessageId,
						InReplyTo = value.InReplyTo,
						References = value.ReferencesHeader,
						ReplyTo = value.ReplyToLine,
						ListId = value.ListIdLine,
						ReturnPath = value.ReturnPathLine,
						DateUtc = value.DateUtc,
						IsSeen = value.IsSeen,
						SizeBytes = value.SizeBytes
					});
					if (maxHits > 0 && list.Count >= maxHits)
					{
						break;
					}
				}
			}
			list.Sort([SpecialName] (MailMessageHeader a, MailMessageHeader b) => DateTime.Compare(b.DateUtc, a.DateUtc));
			return list;
		}
	}

	private static Dictionary<string, IndexEntry> LoadMap(string accountId)
	{
		string path = IndexPath(accountId);
		Dictionary<string, IndexEntry> result;
		if (!File.Exists(path))
		{
			result = new Dictionary<string, IndexEntry>(StringComparer.OrdinalIgnoreCase);
		}
		else
		{
			try
			{
				JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
				List<IndexEntry> list = javaScriptSerializer.Deserialize<List<IndexEntry>>(File.ReadAllText(path, Encoding.UTF8));
				Dictionary<string, IndexEntry> dictionary = new Dictionary<string, IndexEntry>(StringComparer.OrdinalIgnoreCase);
				if (list == null)
				{
					result = dictionary;
				}
				else
				{
					foreach (IndexEntry item in list)
					{
						if (item != null)
						{
							IndexEntry indexEntry = item;
							string key = ((indexEntry.Folder != null) ? indexEntry.Folder : "") + "|" + item.Uid;
							dictionary[key] = item;
						}
					}
					result = dictionary;
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = new Dictionary<string, IndexEntry>(StringComparer.OrdinalIgnoreCase);
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}

	private static void SaveMap(string accountId, Dictionary<string, IndexEntry> map)
	{
		List<IndexEntry> obj = new List<IndexEntry>(map.Values);
		JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
		File.WriteAllText(IndexPath(accountId), javaScriptSerializer.Serialize(obj), Encoding.UTF8);
	}

	internal static List<string[]> ParseGlodaOrGroups(string queryText)
	{
		List<string[]> list = new List<string[]>();
		if (string.IsNullOrWhiteSpace(queryText))
		{
			list.Add(new string[0]);
			return list;
		}
		string[] array = queryText.Split(new string[3] { " OR ", " or ", " Or " }, StringSplitOptions.None);
		string[] array2 = array;
		foreach (string segment in array2)
		{
			List<string> list2 = TokenizeGlodaTerms(segment);
			list.Add(list2.ToArray());
		}
		if (list.Count == 0)
		{
			list.Add(new string[0]);
		}
		return list;
	}

	private static List<string> TokenizeGlodaTerms(string segment)
	{
		List<string> list = new List<string>();
		string text = ((segment != null) ? segment : "").Trim();
		int i = 0;
		checked
		{
			while (i < text.Length)
			{
				if (text[i] == '"')
				{
					int num = text.IndexOf('"', i + 1);
					if (num > i)
					{
						list.Add(text.Substring(i + 1, num - i - 1).ToLowerInvariant());
						i = num + 1;
						continue;
					}
				}
				for (; i < text.Length && (text[i] == ' ' || text[i] == '\t'); i++)
				{
				}
				if (i >= text.Length)
				{
					break;
				}
				int num2 = i;
				for (; i < text.Length && text[i] != ' ' && text[i] != '\t'; i++)
				{
				}
				list.Add(text.Substring(num2, i - num2).ToLowerInvariant());
			}
			return list;
		}
	}

	private static bool GlodaEntryMatches(IndexEntry entry, List<string[]> orGroups)
	{
		if (orGroups == null || orGroups.Count == 0)
		{
			return true;
		}
		foreach (string[] orGroup in orGroups)
		{
			if (EntryMatchesTerms(entry, orGroup))
			{
				return true;
			}
		}
		return false;
	}

	private static bool EntryMatchesTerms(IndexEntry entry, string[] terms)
	{
		if (terms == null || terms.Length == 0)
		{
			return true;
		}
		string[] array = new string[9];
		array[0] = ((entry.Subject != null) ? entry.Subject : "");
		array[1] = " ";
		array[2] = ((entry.FromLine != null) ? entry.FromLine : "");
		array[3] = " ";
		array[4] = ((entry.ToLine != null) ? entry.ToLine : "");
		array[5] = " ";
		array[6] = ((entry.CcLine != null) ? entry.CcLine : "");
		array[7] = " ";
		IndexEntry indexEntry = entry;
		array[8] = ((indexEntry.BodyPreview != null) ? indexEntry.BodyPreview : "");
		string text = string.Concat(array).ToLowerInvariant();
		foreach (string text2 in terms)
		{
			string text3 = ((text2 != null) ? text2 : "").Trim().ToLowerInvariant();
			if (text3.Length == 0)
			{
				continue;
			}
			if (text3.StartsWith("from:", StringComparison.Ordinal))
			{
				string text4 = text3.Substring(5).Trim();
				if (text4.Length != 0)
				{
					indexEntry = entry;
					if (((indexEntry.FromLine != null) ? indexEntry.FromLine : "").ToLowerInvariant().IndexOf(text4, StringComparison.Ordinal) < 0)
					{
						return false;
					}
				}
			}
			else if (text3.StartsWith("to:", StringComparison.Ordinal))
			{
				string text5 = text3.Substring(3).Trim();
				if (text5.Length != 0)
				{
					indexEntry = entry;
					if (((indexEntry.ToLine != null) ? indexEntry.ToLine : "").ToLowerInvariant().IndexOf(text5, StringComparison.Ordinal) < 0)
					{
						return false;
					}
				}
			}
			else if (text3.StartsWith("cc:", StringComparison.Ordinal))
			{
				string text6 = text3.Substring(3).Trim();
				if (text6.Length != 0)
				{
					indexEntry = entry;
					if (((indexEntry.CcLine != null) ? indexEntry.CcLine : "").ToLowerInvariant().IndexOf(text6, StringComparison.Ordinal) < 0)
					{
						return false;
					}
				}
			}
			else if (text3.StartsWith("messageid:", StringComparison.Ordinal))
			{
				string text7 = text3.Substring(10).Trim();
				if (text7.Length != 0)
				{
					indexEntry = entry;
					if (((indexEntry.MessageId != null) ? indexEntry.MessageId : "").ToLowerInvariant().IndexOf(text7, StringComparison.Ordinal) < 0)
					{
						return false;
					}
				}
			}
			else if (text3.StartsWith("thread:", StringComparison.Ordinal) || text3.StartsWith("threadroot:", StringComparison.Ordinal))
			{
				string text8 = text3;
				text8 = ((!text8.StartsWith("threadroot:", StringComparison.Ordinal)) ? text8.Substring(7).Trim() : text8.Substring(11).Trim());
				if (text8.Length != 0 && !EntryMatchesThreadFilter(entry, text8))
				{
					return false;
				}
			}
			else if (text3.StartsWith("inreplyto:", StringComparison.Ordinal))
			{
				string text9 = text3.Substring(10).Trim();
				if (text9.Length != 0)
				{
					indexEntry = entry;
					if (((indexEntry.InReplyTo != null) ? indexEntry.InReplyTo : "").ToLowerInvariant().IndexOf(text9, StringComparison.Ordinal) < 0)
					{
						return false;
					}
				}
			}
			else if (text3.StartsWith("references:", StringComparison.Ordinal))
			{
				string text10 = text3.Substring(11).Trim();
				if (text10.Length != 0)
				{
					indexEntry = entry;
					if (((indexEntry.ReferencesHeader != null) ? indexEntry.ReferencesHeader : "").ToLowerInvariant().IndexOf(text10, StringComparison.Ordinal) < 0)
					{
						return false;
					}
				}
			}
			else if (text3.StartsWith("replyto:", StringComparison.Ordinal))
			{
				string text11 = text3.Substring(8).Trim();
				if (text11.Length != 0)
				{
					indexEntry = entry;
					if (((indexEntry.ReplyToLine != null) ? indexEntry.ReplyToLine : "").ToLowerInvariant().IndexOf(text11, StringComparison.Ordinal) < 0)
					{
						return false;
					}
				}
			}
			else if (text3.StartsWith("list-id:", StringComparison.Ordinal))
			{
				string text12 = text3.Substring(8).Trim();
				if (text12.Length != 0)
				{
					indexEntry = entry;
					if (((indexEntry.ListIdLine != null) ? indexEntry.ListIdLine : "").ToLowerInvariant().IndexOf(text12, StringComparison.Ordinal) < 0)
					{
						return false;
					}
				}
			}
			else if (text3.StartsWith("return-path:", StringComparison.Ordinal))
			{
				string text13 = text3.Substring(12).Trim();
				if (text13.Length != 0)
				{
					indexEntry = entry;
					if (((indexEntry.ReturnPathLine != null) ? indexEntry.ReturnPathLine : "").ToLowerInvariant().IndexOf(text13, StringComparison.Ordinal) < 0)
					{
						return false;
					}
				}
			}
			else if (text3.StartsWith("subject:", StringComparison.Ordinal))
			{
				string text14 = text3.Substring(8).Trim();
				if (text14.Length != 0)
				{
					indexEntry = entry;
					if (((indexEntry.Subject != null) ? indexEntry.Subject : "").ToLowerInvariant().IndexOf(text14, StringComparison.Ordinal) < 0)
					{
						return false;
					}
				}
			}
			else if (text3.StartsWith("body:", StringComparison.Ordinal))
			{
				string text15 = text3.Substring(5).Trim();
				if (text15.Length != 0)
				{
					indexEntry = entry;
					if (((indexEntry.BodyPreview != null) ? indexEntry.BodyPreview : "").ToLowerInvariant().IndexOf(text15, StringComparison.Ordinal) < 0)
					{
						return false;
					}
				}
			}
			else if (text3.Equals("has:attachment", StringComparison.Ordinal) || text3.Equals("has:attachments", StringComparison.Ordinal) || text3.Equals("attachment:yes", StringComparison.Ordinal))
			{
				if (entry.AttachmentCount <= 0)
				{
					return false;
				}
			}
			else if (text.IndexOf(text3, StringComparison.Ordinal) < 0)
			{
				return false;
			}
		}
		return true;
	}

	private static bool FolderMatches(string folderPath, string filterPattern)
	{
		string text = ((folderPath != null) ? folderPath : "");
		string text2 = ((filterPattern != null) ? filterPattern : "");
		if (text2.Length == 0)
		{
			return true;
		}
		return text.IndexOf(text2, StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static string ExtractToAddressFilter(ref string queryText)
	{
		string text = ((queryText != null) ? queryText : "").Trim();
		if (text.Length == 0)
		{
			return "";
		}
		string[] array = text.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		List<string> list = new List<string>();
		string result = "";
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			if (text2.StartsWith("to:", StringComparison.OrdinalIgnoreCase))
			{
				result = text2.Substring(3).Trim();
			}
			else
			{
				list.Add(text2);
			}
		}
		queryText = string.Join(" ", list);
		return result;
	}

	private static string ExtractCcAddressFilter(ref string queryText)
	{
		string text = ((queryText != null) ? queryText : "").Trim();
		if (text.Length == 0)
		{
			return "";
		}
		string[] array = text.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		List<string> list = new List<string>();
		string result = "";
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			if (text2.StartsWith("cc:", StringComparison.OrdinalIgnoreCase))
			{
				result = text2.Substring(3).Trim();
			}
			else
			{
				list.Add(text2);
			}
		}
		queryText = string.Join(" ", list);
		return result;
	}

	private static string ExtractMessageIdFilter(ref string queryText)
	{
		string text = ((queryText != null) ? queryText : "").Trim();
		if (text.Length == 0)
		{
			return "";
		}
		string[] array = text.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		List<string> list = new List<string>();
		string result = "";
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			if (text2.StartsWith("messageid:", StringComparison.OrdinalIgnoreCase))
			{
				result = text2.Substring(10).Trim();
			}
			else
			{
				list.Add(text2);
			}
		}
		queryText = string.Join(" ", list);
		return result;
	}

	private static string ExtractThreadFilter(ref string queryText)
	{
		string text = ((queryText != null) ? queryText : "").Trim();
		if (text.Length == 0)
		{
			return "";
		}
		string[] array = text.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		List<string> list = new List<string>();
		string result = "";
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			if (text2.StartsWith("thread:", StringComparison.OrdinalIgnoreCase))
			{
				result = text2.Substring(7).Trim();
			}
			else if (text2.StartsWith("threadroot:", StringComparison.OrdinalIgnoreCase))
			{
				result = text2.Substring(11).Trim();
			}
			else
			{
				list.Add(text2);
			}
		}
		queryText = string.Join(" ", list);
		return result;
	}

	private static bool EntryMatchesThreadFilter(IndexEntry entry, string threadFilter)
	{
		if (entry == null)
		{
			return false;
		}
		string text = NormalizeMessageIdToken(threadFilter);
		if (text.Length == 0)
		{
			text = ((threadFilter != null) ? threadFilter : "").Trim();
		}
		if (text.Length == 0)
		{
			return true;
		}
		string text2 = ResolveThreadRootToken(entry);
		if (text2.Length > 0 && TokenMatchesThreadNeedle(text2, text))
		{
			return true;
		}
		IndexEntry indexEntry = entry;
		if (TokenMatchesThreadNeedle((indexEntry.MessageId != null) ? indexEntry.MessageId : "", text))
		{
			return true;
		}
		indexEntry = entry;
		if (TokenMatchesThreadNeedle((indexEntry.InReplyTo != null) ? indexEntry.InReplyTo : "", text))
		{
			return true;
		}
		indexEntry = entry;
		string text3 = ((indexEntry.ReferencesHeader != null) ? indexEntry.ReferencesHeader : "");
		if (text3.Length > 0 && text3.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return true;
		}
		return false;
	}

	private static bool HeaderMatchesThreadFilter(MailMessageHeader hdr, IndexEntry entry, string threadFilter)
	{
		if (entry != null)
		{
			return EntryMatchesThreadFilter(entry, threadFilter);
		}
		if (hdr == null)
		{
			return false;
		}
		string text = NormalizeMessageIdToken(threadFilter);
		if (text.Length == 0)
		{
			text = ((threadFilter != null) ? threadFilter : "").Trim();
		}
		if (text.Length == 0)
		{
			return true;
		}
		MailMessageHeader mailMessageHeader = hdr;
		if (TokenMatchesThreadNeedle((mailMessageHeader.MessageId != null) ? mailMessageHeader.MessageId : "", text))
		{
			return true;
		}
		mailMessageHeader = hdr;
		if (TokenMatchesThreadNeedle((mailMessageHeader.InReplyTo != null) ? mailMessageHeader.InReplyTo : "", text))
		{
			return true;
		}
		mailMessageHeader = hdr;
		string text2 = ((mailMessageHeader.References != null) ? mailMessageHeader.References : "");
		if (text2.Length > 0 && text2.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return true;
		}
		return false;
	}

	private static bool TokenMatchesThreadNeedle(string rawToken, string needle)
	{
		string text = NormalizeMessageIdToken(rawToken);
		if (text.Length == 0)
		{
			text = ((rawToken != null) ? rawToken : "").Trim();
		}
		if (text.Length == 0)
		{
			return false;
		}
		if (string.Equals(text, needle, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		return text.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static string ExtractInReplyToFilter(ref string queryText)
	{
		string text = ((queryText != null) ? queryText : "").Trim();
		if (text.Length == 0)
		{
			return "";
		}
		string[] array = text.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		List<string> list = new List<string>();
		string result = "";
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			if (text2.StartsWith("inreplyto:", StringComparison.OrdinalIgnoreCase))
			{
				result = text2.Substring(10).Trim();
			}
			else
			{
				list.Add(text2);
			}
		}
		queryText = string.Join(" ", list);
		return result;
	}

	private static string ExtractReferencesFilter(ref string queryText)
	{
		string text = ((queryText != null) ? queryText : "").Trim();
		if (text.Length == 0)
		{
			return "";
		}
		string[] array = text.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		List<string> list = new List<string>();
		string result = "";
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			if (text2.StartsWith("references:", StringComparison.OrdinalIgnoreCase))
			{
				result = text2.Substring(11).Trim();
			}
			else
			{
				list.Add(text2);
			}
		}
		queryText = string.Join(" ", list);
		return result;
	}

	private static string ExtractReplyToFilter(ref string queryText)
	{
		string text = ((queryText != null) ? queryText : "").Trim();
		if (text.Length == 0)
		{
			return "";
		}
		string[] array = text.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		List<string> list = new List<string>();
		string result = "";
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			if (text2.StartsWith("replyto:", StringComparison.OrdinalIgnoreCase))
			{
				result = text2.Substring(8).Trim();
			}
			else
			{
				list.Add(text2);
			}
		}
		queryText = string.Join(" ", list);
		return result;
	}

	private static string ExtractListIdFilter(ref string queryText)
	{
		string text = ((queryText != null) ? queryText : "").Trim();
		if (text.Length == 0)
		{
			return "";
		}
		string[] array = text.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		List<string> list = new List<string>();
		string result = "";
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			if (text2.StartsWith("list-id:", StringComparison.OrdinalIgnoreCase))
			{
				result = text2.Substring(8).Trim();
			}
			else
			{
				list.Add(text2);
			}
		}
		queryText = string.Join(" ", list);
		return result;
	}

	private static string ExtractReturnPathFilter(ref string queryText)
	{
		string text = ((queryText != null) ? queryText : "").Trim();
		if (text.Length == 0)
		{
			return "";
		}
		string[] array = text.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		List<string> list = new List<string>();
		string result = "";
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			if (text2.StartsWith("return-path:", StringComparison.OrdinalIgnoreCase))
			{
				result = text2.Substring(12).Trim();
			}
			else
			{
				list.Add(text2);
			}
		}
		queryText = string.Join(" ", list);
		return result;
	}

	private static string ExtractFolderFilter(ref string queryText)
	{
		string text = ((queryText != null) ? queryText : "").Trim();
		if (text.Length == 0)
		{
			return "";
		}
		string[] array = text.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		List<string> list = new List<string>();
		string result = "";
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			if (text2.StartsWith("folder:", StringComparison.OrdinalIgnoreCase))
			{
				result = text2.Substring(7).Trim();
			}
			else
			{
				list.Add(text2);
			}
		}
		queryText = string.Join(" ", list);
		return result;
	}

	private static long ExtractSmallerMaxBytesFilter(ref string queryText)
	{
		string text = ((queryText != null) ? queryText : "").Trim();
		if (text.Length == 0)
		{
			return 0L;
		}
		string[] array = text.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		List<string> list = new List<string>();
		long result = 0L;
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			string text3 = text2.ToLowerInvariant();
			if (text3.StartsWith("smaller:", StringComparison.Ordinal))
			{
				result = ParseSizeTokenToBytes(text2.Substring(8).Trim());
			}
			else
			{
				list.Add(text2);
			}
		}
		queryText = string.Join(" ", list);
		return result;
	}

	private static long ExtractLargerMinBytesFilter(ref string queryText)
	{
		string text = ((queryText != null) ? queryText : "").Trim();
		if (text.Length == 0)
		{
			return 0L;
		}
		string[] array = text.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		List<string> list = new List<string>();
		long result = 0L;
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			string text3 = text2.ToLowerInvariant();
			if (text3.StartsWith("larger:", StringComparison.Ordinal))
			{
				string token = text2.Substring(7).Trim();
				result = ParseSizeTokenToBytes(token);
			}
			else
			{
				list.Add(text2);
			}
		}
		queryText = string.Join(" ", list);
		return result;
	}

	private static long ParseSizeTokenToBytes(string token)
	{
		string text = ((token != null) ? token : "").Trim().ToLowerInvariant();
		if (text.Length == 0)
		{
			return 0L;
		}
		long num = 1L;
		checked
		{
			if (text.EndsWith("k", StringComparison.Ordinal))
			{
				num = 1024L;
				text = text.Substring(0, text.Length - 1);
			}
			else if (text.EndsWith("m", StringComparison.Ordinal))
			{
				num = 1048576L;
				text = text.Substring(0, text.Length - 1);
			}
			else if (text.EndsWith("d", StringComparison.OrdinalIgnoreCase))
			{
				text = text.Substring(0, text.Length - 1);
			}
			long result = 0L;
			if (long.TryParse(text, out result) && result > 0)
			{
				return result * num;
			}
			return 0L;
		}
	}

	private static bool ExtractSeenOnlyFilter(ref string queryText)
	{
		string text = ((queryText != null) ? queryText : "").Trim();
		if (text.Length == 0)
		{
			return false;
		}
		string[] array = text.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		List<string> list = new List<string>();
		bool result = false;
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			if (text2.Equals("read:1", StringComparison.OrdinalIgnoreCase) || text2.Equals("read:true", StringComparison.OrdinalIgnoreCase) || text2.Equals("is:read", StringComparison.OrdinalIgnoreCase))
			{
				result = true;
			}
			else
			{
				list.Add(text2);
			}
		}
		queryText = string.Join(" ", list);
		return result;
	}

	private static bool ExtractHasBodyFilter(ref string queryText)
	{
		string text = ((queryText != null) ? queryText : "").Trim();
		if (text.Length == 0)
		{
			return false;
		}
		string[] array = text.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		List<string> list = new List<string>();
		bool result = false;
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			if (text2.Equals("has:body", StringComparison.OrdinalIgnoreCase) || text2.Equals("body:yes", StringComparison.OrdinalIgnoreCase))
			{
				result = true;
			}
			else
			{
				list.Add(text2);
			}
		}
		queryText = string.Join(" ", list);
		return result;
	}

	private static bool ExtractHasAttachmentFilter(ref string queryText)
	{
		string text = ((queryText != null) ? queryText : "").Trim();
		if (text.Length == 0)
		{
			return false;
		}
		string[] array = text.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		List<string> list = new List<string>();
		bool result = false;
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			if (text2.Equals("has:attachment", StringComparison.OrdinalIgnoreCase) || text2.Equals("has:attachments", StringComparison.OrdinalIgnoreCase) || text2.Equals("attachment:yes", StringComparison.OrdinalIgnoreCase))
			{
				result = true;
			}
			else
			{
				list.Add(text2);
			}
		}
		queryText = string.Join(" ", list);
		return result;
	}

	private static bool ExtractUnreadOnlyFilter(ref string queryText)
	{
		string text = ((queryText != null) ? queryText : "").Trim();
		if (text.Length == 0)
		{
			return false;
		}
		string[] array = text.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		List<string> list = new List<string>();
		bool result = false;
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			if (text2.Equals("unread:1", StringComparison.OrdinalIgnoreCase) || text2.Equals("unread:true", StringComparison.OrdinalIgnoreCase) || text2.Equals("is:unread", StringComparison.OrdinalIgnoreCase))
			{
				result = true;
			}
			else
			{
				list.Add(text2);
			}
		}
		queryText = string.Join(" ", list);
		return result;
	}

	private static int ExtractOlderDaysFilter(ref string queryText)
	{
		string text = ((queryText != null) ? queryText : "").Trim();
		if (text.Length == 0)
		{
			return 0;
		}
		string[] array = text.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		List<string> list = new List<string>();
		int result = 0;
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			string text3 = text2.ToLowerInvariant();
			if (text3.StartsWith("older:", StringComparison.Ordinal))
			{
				string text4 = text2.Substring(6).Trim();
				if (text4.EndsWith("d", StringComparison.OrdinalIgnoreCase))
				{
					text4 = text4.Substring(0, checked(text4.Length - 1));
				}
				int result2 = 0;
				if (int.TryParse(text4, out result2) && result2 > 0)
				{
					result = result2;
				}
			}
			else
			{
				list.Add(text2);
			}
		}
		queryText = string.Join(" ", list);
		return result;
	}

	private static int ExtractNewerDaysFilter(ref string queryText)
	{
		string text = ((queryText != null) ? queryText : "").Trim();
		if (text.Length == 0)
		{
			return 0;
		}
		string[] array = text.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		List<string> list = new List<string>();
		int result = 0;
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			string text3 = text2.ToLowerInvariant();
			if (text3.StartsWith("newer:", StringComparison.Ordinal))
			{
				string text4 = text2.Substring(6).Trim();
				if (text4.EndsWith("d", StringComparison.OrdinalIgnoreCase))
				{
					text4 = text4.Substring(0, checked(text4.Length - 1));
				}
				int result2 = 0;
				if (int.TryParse(text4, out result2) && result2 > 0)
				{
					result = result2;
				}
			}
			else
			{
				list.Add(text2);
			}
		}
		queryText = string.Join(" ", list);
		return result;
	}

	private static string IndexPath(string accountId)
	{
		string text = Path.Combine(AccountStore.DataRoot(), "search-index");
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		string text2 = accountId.Replace("\\", "_").Replace("/", "_").Replace(":", "_");
		return Path.Combine(text, text2 + ".json");
	}
}
