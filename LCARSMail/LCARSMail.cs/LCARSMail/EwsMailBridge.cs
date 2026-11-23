using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

public sealed class EwsMailBridge
{
	public const string ProtocolName = "ews";

	private EwsMailBridge()
	{
	}

	public static bool IsEwsAccount(MailAccount account)
	{
		if (account == null)
		{
			return false;
		}
		string text = ((account.Protocol != null) ? account.Protocol : "").Trim();
		if (text.Length == 0)
		{
			return (!string.IsNullOrWhiteSpace(account.EwsUrl) && string.IsNullOrWhiteSpace(account.ImapHost)) ? true : false;
		}
		return string.Equals(text, "ews", StringComparison.OrdinalIgnoreCase);
	}

	public static MailAccount TryRegisterAccount(string ewsUrl, string username, string password, string displayName)
	{
		string text = ((username != null) ? username : "").Trim();
		if (text.Length == 0)
		{
			return null;
		}
		string text2 = ((ewsUrl != null) ? ewsUrl : "").Trim();
		if (text2.Length == 0)
		{
			text2 = "https://outlook.office365.com/EWS/Exchange.asmx";
		}
		MailAccount mailAccount = new MailAccount();
		mailAccount.Protocol = "ews";
		mailAccount.EwsUrl = text2;
		mailAccount.Id = "ews:" + text.ToLowerInvariant();
		mailAccount.Username = text;
		mailAccount.EmailAddress = text;
		mailAccount.DisplayName = ((displayName != null) ? displayName : "").Trim();
		MailAccount mailAccount2 = mailAccount;
		if (mailAccount2.DisplayName.Length == 0)
		{
			mailAccount2.DisplayName = "EWS " + text;
		}
		List<MailAccount> list = AccountStore.LoadAccounts();
		if (list == null)
		{
			list = new List<MailAccount>();
		}
		list.RemoveAll([SpecialName] (MailAccount a) => (a != null && string.Equals(a.Id, mailAccount2.Id, StringComparison.OrdinalIgnoreCase)) ? true : false);
		list.Add(mailAccount2);
		if (!AccountStore.SaveAccounts(list))
		{
			return null;
		}
		if (password != null)
		{
			AccountStore.SavePassword(mailAccount2.Id, password);
		}
		return mailAccount2;
	}

	public static string ResolveOAuthToken()
	{
		string accessToken = null;
		if (EwsOAuthStore.TryEnsureAccessToken(ref accessToken, "organizations", "94933e60-ee10-4250-a050-3efd588b0718"))
		{
			return accessToken;
		}
		EwsOAuthStore.TryLoadToken(ref accessToken);
		return (accessToken != null) ? accessToken : "";
	}

	public static List<MailFolderInfo> ListFolders(MailAccount account)
	{
		List<MailFolderInfo> list = new List<MailFolderInfo>();
		list.Add(new MailFolderInfo
		{
			FullName = "inbox",
			DisplayName = "INBOX"
		});
		list.Add(new MailFolderInfo
		{
			FullName = "sentitems",
			DisplayName = "SENT"
		});
		list.Add(new MailFolderInfo
		{
			FullName = "drafts",
			DisplayName = "DRAFTS"
		});
		list.Add(new MailFolderInfo
		{
			FullName = "deleteditems",
			DisplayName = "TRASH"
		});
		list.Add(new MailFolderInfo
		{
			FullName = "junkemail",
			DisplayName = "JUNK"
		});
		list.Add(new MailFolderInfo
		{
			FullName = "archive",
			DisplayName = "ARCHIVE"
		});
		List<MailFolderInfo> list2 = list;
		if (account == null || string.IsNullOrWhiteSpace(account.EwsUrl))
		{
			return list2;
		}
		string oauthToken = ResolveOAuthToken();
		string password = "";
		AccountStore.TryGetPassword(account.Id, ref password);
		List<EwsMailService.EwsFolderInfo> list3 = EwsMailService.TryFindMailSubfolders(account.EwsUrl, account.Username, password, oauthToken);
		if (list3 == null)
		{
			return list2;
		}
		foreach (EwsMailService.EwsFolderInfo item in list3)
		{
			if (item != null && !string.IsNullOrWhiteSpace(item.FolderId))
			{
				EwsMailService.EwsFolderInfo ewsFolderInfo = item;
				string text = ((ewsFolderInfo.DisplayName != null) ? ewsFolderInfo.DisplayName : item.FolderId).Trim();
				if (text.Length == 0)
				{
					text = item.FolderId;
				}
				list2.Add(new MailFolderInfo
				{
					FullName = "fid:" + item.FolderId,
					DisplayName = text.ToUpperInvariant()
				});
			}
		}
		return list2;
	}

	public static List<MailMessageHeader> SyncHeaders(MailAccount account, string password, string folder, int maxCount = 500)
	{
		string text = ((folder != null) ? folder : "").Trim();
		if (text.Length == 0)
		{
			text = "inbox";
		}
		string distinguishedFolderId = MapFolderToDistinguished(text);
		string oauthToken = ResolveOAuthToken();
		List<EwsMailService.EwsMailSummary> list = EwsMailService.TryFindFolderItems(account.EwsUrl, account.Username, password, distinguishedFolderId, oauthToken);
		if (list == null)
		{
			return new List<MailMessageHeader>();
		}
		string accountId = ((account.Id != null) ? account.Id : "");
		string folder2 = text;
		List<MailMessageHeader> list2 = new List<MailMessageHeader>();
		foreach (EwsMailService.EwsMailSummary item2 in list)
		{
			if (item2 != null && !string.IsNullOrWhiteSpace(item2.ItemId))
			{
				ulong uid = EwsItemIdStore.ItemIdToUid(item2.ItemId);
				EwsItemIdStore.Remember(accountId, folder2, uid, item2.ItemId);
				MailMessageHeader mailMessageHeader = new MailMessageHeader();
				mailMessageHeader.Uid = uid;
				mailMessageHeader.Folder = folder2;
				EwsMailService.EwsMailSummary ewsMailSummary = item2;
				mailMessageHeader.Subject = ((ewsMailSummary.Subject != null) ? ewsMailSummary.Subject : "");
				EwsMailService.EwsMailSummary ewsMailSummary2 = item2;
				mailMessageHeader.From = ((ewsMailSummary2.FromLine != null) ? ewsMailSummary2.FromLine : "");
				mailMessageHeader.DateUtc = ((DateTime.Compare(item2.DateUtc, DateTime.MinValue) == 0) ? DateTime.UtcNow : item2.DateUtc);
				mailMessageHeader.IsSeen = false;
				MailMessageHeader item = mailMessageHeader;
				list2.Add(item);
			}
		}
		list2.Sort([SpecialName] (MailMessageHeader a, MailMessageHeader b) => DateTime.Compare(b.DateUtc, a.DateUtc));
		if (maxCount > 0 && list2.Count > maxCount)
		{
			return list2.GetRange(0, maxCount);
		}
		return list2;
	}

	public static List<MailMessageHeader> SearchFolderHeaders(MailAccount account, string password, string folder, string queryText, int maxCount = 200)
	{
		string text = ((queryText != null) ? queryText : "").Trim().ToLowerInvariant();
		List<MailMessageHeader> list = SyncHeaders(account, password, folder);
		if (text.Length == 0)
		{
			if (maxCount > 0 && list.Count > maxCount)
			{
				return list.GetRange(0, maxCount);
			}
			return list;
		}
		List<MailMessageHeader> list2 = new List<MailMessageHeader>();
		string text2 = ((account.Id != null) ? account.Id : "");
		string text3 = ((folder != null) ? folder : "").Trim();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (text2.Length > 0)
		{
			foreach (MailMessageHeader item2 in MailSearchIndex.Search(text2, text, maxCount))
			{
				if (item2 != null && string.Equals(item2.Folder, text3, StringComparison.OrdinalIgnoreCase))
				{
					hashSet.Add(item2.Folder + "|" + item2.Uid);
				}
			}
		}
		foreach (MailMessageHeader item3 in list)
		{
			if (item3 != null)
			{
				MailMessageHeader mailMessageHeader = item3;
				string obj = ((mailMessageHeader.Subject != null) ? mailMessageHeader.Subject : "");
				MailMessageHeader mailMessageHeader2 = item3;
				string text4 = (obj + " " + ((mailMessageHeader2.From != null) ? mailMessageHeader2.From : "")).ToLowerInvariant();
				string item = text3 + "|" + item3.Uid;
				if (text4.IndexOf(text, StringComparison.Ordinal) >= 0 || hashSet.Contains(item))
				{
					list2.Add(item3);
				}
				if (maxCount > 0 && list2.Count >= maxCount)
				{
					break;
				}
			}
		}
		return list2;
	}

	public static MailMessageBody FetchBody(MailAccount account, string password, string folder, ulong uid)
	{
		string folder2 = ((folder != null) ? folder : "").Trim();
		string itemId = null;
		MailAccount mailAccount = account;
		if (!EwsItemIdStore.TryGetItemId((mailAccount.Id != null) ? mailAccount.Id : "", folder2, uid, ref itemId))
		{
			return new MailMessageBody();
		}
		string oauthToken = ResolveOAuthToken();
		EwsMailService.EwsMessageBody ewsMessageBody = EwsMailService.TryGetItemBody(account.EwsUrl, account.Username, password, itemId, oauthToken);
		MailMessageBody mailMessageBody = new MailMessageBody();
		if (ewsMessageBody == null)
		{
			return mailMessageBody;
		}
		EwsMailService.EwsMessageBody ewsMessageBody2 = ewsMessageBody;
		mailMessageBody.TextBody = ((ewsMessageBody2.BodyText != null) ? ewsMessageBody2.BodyText : "");
		if (mailMessageBody.TextBody.IndexOf("<html", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			mailMessageBody.HtmlBody = mailMessageBody.TextBody;
			mailMessageBody.TextBody = "";
		}
		if (ewsMessageBody.Attachments != null && ewsMessageBody.Attachments.Count > 0)
		{
			mailAccount = account;
			EwsAttachmentStore.RememberAttachments((mailAccount.Id != null) ? mailAccount.Id : "", folder2, uid, ewsMessageBody.Attachments);
			int num = 0;
			foreach (EwsMailService.EwsAttachmentInfo attachment in ewsMessageBody.Attachments)
			{
				if (attachment != null)
				{
					MailAttachmentInfo mailAttachmentInfo = new MailAttachmentInfo();
					mailAttachmentInfo.PartIndex = num;
					EwsMailService.EwsAttachmentInfo ewsAttachmentInfo = attachment;
					mailAttachmentInfo.FileName = ((ewsAttachmentInfo.FileName != null) ? ewsAttachmentInfo.FileName : "attachment");
					mailAttachmentInfo.Size = 0L;
					MailAttachmentInfo item = mailAttachmentInfo;
					mailMessageBody.Attachments.Add(item);
					num = checked(num + 1);
				}
			}
		}
		return mailMessageBody;
	}

	public static string SaveAttachmentToFile(MailAccount account, string password, string folder, ulong uid, int partIndex, string destPath)
	{
		string result;
		if (string.IsNullOrWhiteSpace(destPath))
		{
			result = "Save path is required.";
		}
		else
		{
			string folder2 = ((folder != null) ? folder : "").Trim();
			string attachmentId = null;
			string fileName = "";
			if (!EwsAttachmentStore.TryGetAttachmentId((account.Id != null) ? account.Id : "", folder2, uid, partIndex, ref attachmentId, ref fileName))
			{
				result = "EWS attachment not cached — open message again.";
			}
			else
			{
				string oauthToken = ResolveOAuthToken();
				byte[] array = EwsMailService.TryGetAttachmentBytes(account.EwsUrl, account.Username, password, attachmentId, oauthToken);
				if (array == null || array.Length == 0)
				{
					result = ((EwsMailService.LastError != null) ? EwsMailService.LastError : "Attachment download failed.");
				}
				else
				{
					try
					{
						File.WriteAllBytes(destPath, array);
						result = "";
					}
					catch (Exception ex)
					{
						ProjectData.SetProjectError(ex);
						Exception ex2 = ex;
						result = ex2.Message;
						ProjectData.ClearProjectError();
					}
				}
			}
		}
		return result;
	}

	public static void MarkSeen(MailAccount account, string password, string folder, ulong uid)
	{
		SetReadFlag(account, password, folder, uid, isRead: true);
	}

	public static void MarkUnseen(MailAccount account, string password, string folder, ulong uid)
	{
		SetReadFlag(account, password, folder, uid, isRead: false);
	}

	private static void SetReadFlag(MailAccount account, string password, string folder, ulong uid, bool isRead)
	{
		string folder2 = ((folder != null) ? folder : "").Trim();
		string itemId = null;
		if (EwsItemIdStore.TryGetItemId((account.Id != null) ? account.Id : "", folder2, uid, ref itemId))
		{
			string oauthToken = ResolveOAuthToken();
			EwsMailService.TryMarkItemsRead(account.EwsUrl, account.Username, password, new List<string> { itemId }, isRead, oauthToken);
		}
	}

	public static void MoveMessages(MailAccount account, string password, string folder, string destFolder, List<ulong> uids)
	{
		MoveOrDelete(account, password, folder, destFolder, uids, deleteOnly: false);
	}

	public static void MoveToJunk(MailAccount account, string password, string folder, List<ulong> uids)
	{
		MoveOrDelete(account, password, folder, "junkemail", uids, deleteOnly: false);
	}

	private static void MoveOrDelete(MailAccount account, string password, string folder, string destFolder, List<ulong> uids, bool deleteOnly)
	{
		if (uids == null || uids.Count == 0)
		{
			return;
		}
		string folder2 = ((folder != null) ? folder : "").Trim();
		string accountId = ((account.Id != null) ? account.Id : "");
		List<string> list = new List<string>();
		foreach (ulong uid in uids)
		{
			string itemId = null;
			if (EwsItemIdStore.TryGetItemId(accountId, folder2, uid, ref itemId))
			{
				list.Add(itemId);
			}
		}
		if (list.Count != 0)
		{
			string oauthToken = ResolveOAuthToken();
			if (deleteOnly)
			{
				EwsMailService.TryDeleteItems(account.EwsUrl, account.Username, password, list, oauthToken);
			}
			else if (destFolder.Length != 0)
			{
				string toDistinguishedFolderId = MapFolderToDistinguished(destFolder);
				EwsMailService.TryMoveItems(account.EwsUrl, account.Username, password, list, toDistinguishedFolderId, oauthToken);
			}
		}
	}

	public static void DeleteMessages(MailAccount account, string password, string folder, List<ulong> uids)
	{
		MoveOrDelete(account, password, folder, "deleteditems", uids, deleteOnly: true);
	}

	public static void CopyMessages(MailAccount account, string password, string folder, string destFolder, List<ulong> uids)
	{
		if (uids == null || uids.Count == 0)
		{
			return;
		}
		string folder2 = ((folder != null) ? folder : "").Trim();
		string accountId = ((account.Id != null) ? account.Id : "");
		List<string> list = new List<string>();
		foreach (ulong uid in uids)
		{
			string itemId = null;
			if (EwsItemIdStore.TryGetItemId(accountId, folder2, uid, ref itemId))
			{
				list.Add(itemId);
			}
		}
		if (list.Count != 0)
		{
			string oauthToken = ResolveOAuthToken();
			string toDistinguishedFolderId = MapFolderToDistinguished(destFolder);
			EwsMailService.TryCopyItems(account.EwsUrl, account.Username, password, list, toDistinguishedFolderId, oauthToken);
		}
	}

	public static void AddKeyword(MailAccount account, string password, string folder, ulong uid, string keyword)
	{
		string folder2 = ((folder != null) ? folder : "").Trim();
		string itemId = null;
		if (EwsItemIdStore.TryGetItemId((account.Id != null) ? account.Id : "", folder2, uid, ref itemId))
		{
			string text = ((keyword != null) ? keyword : "").Trim();
			if (text.Length != 0)
			{
				string oauthToken = ResolveOAuthToken();
				EwsMailService.TryAddItemCategory(account.EwsUrl, account.Username, password, itemId, text, oauthToken);
			}
		}
	}

	public static string TrySendFilterMail(MailAccount account, string password, string toAddress, string subject, string bodyText)
	{
		string oauthToken = ResolveOAuthToken();
		if (EwsMailService.TrySendMessage(account.EwsUrl, account.Username, password, toAddress, subject, bodyText, oauthToken))
		{
			return "";
		}
		return (EwsMailService.LastError != null) ? EwsMailService.LastError : "EWS send failed.";
	}

	public static string ResolveFilterFolder(string actionValue)
	{
		string text = MailFilterEngine.ResolveImapFolderPath(actionValue);
		if (text.Length == 0)
		{
			return "";
		}
		return MapFolderToDistinguished(text);
	}

	public static string MapFolderToDistinguished(string folder)
	{
		string text = ((folder != null) ? folder : "").Trim().ToLowerInvariant();
		if (text.Length != 0)
		{
			switch (text)
			{
			case "inbox":
				break;
			case "sent":
			case "sent items":
			case "sentitems":
				return "sentitems";
			case "drafts":
				return "drafts";
			case "trash":
			case "deleted items":
			case "deleteditems":
				return "deleteditems";
			case "junk":
			case "junkemail":
			case "spam":
				return "junkemail";
			case "archive":
				return "archive";
			default:
				if (text.StartsWith("fid:", StringComparison.OrdinalIgnoreCase))
				{
					return text.Substring(4);
				}
				return text;
			}
		}
		return "inbox";
	}
}
