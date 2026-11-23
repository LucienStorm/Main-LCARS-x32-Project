using System.Collections.Generic;
using MimeKit;

namespace LCARSMail;

public sealed class UnifiedMailService
{
	private readonly ImapMailService imap;

	public ImapMailService ImapService => imap;

	public UnifiedMailService()
	{
		imap = new ImapMailService();
	}

	public List<MailFolderInfo> ListFolders(MailAccount account, string password)
	{
		if (EwsMailBridge.IsEwsAccount(account))
		{
			return EwsMailBridge.ListFolders(account);
		}
		return imap.ListFolders(account, password);
	}

	public List<MailMessageHeader> SyncHeaders(MailAccount account, string password, string folder, int maxCount = 500)
	{
		if (EwsMailBridge.IsEwsAccount(account))
		{
			return EwsMailBridge.SyncHeaders(account, password, folder, maxCount);
		}
		return imap.SyncHeaders(account, password, folder, maxCount);
	}

	public MailMessageBody FetchBody(MailAccount account, string password, string folder, ulong uid)
	{
		if (EwsMailBridge.IsEwsAccount(account))
		{
			return EwsMailBridge.FetchBody(account, password, folder, uid);
		}
		return imap.FetchBody(account, password, folder, uid);
	}

	public void MarkSeen(MailAccount account, string password, string folder, ulong uid)
	{
		if (EwsMailBridge.IsEwsAccount(account))
		{
			EwsMailBridge.MarkSeen(account, password, folder, uid);
		}
		else
		{
			imap.MarkSeen(account, password, folder, uid);
		}
	}

	public void MarkUnseen(MailAccount account, string password, string folder, ulong uid)
	{
		if (EwsMailBridge.IsEwsAccount(account))
		{
			EwsMailBridge.MarkUnseen(account, password, folder, uid);
		}
		else
		{
			imap.MarkUnseen(account, password, folder, uid);
		}
	}

	public List<MailMessageHeader> SearchFolderHeaders(MailAccount account, string password, string folder, string queryText, int maxCount = 200)
	{
		if (EwsMailBridge.IsEwsAccount(account))
		{
			return EwsMailBridge.SearchFolderHeaders(account, password, folder, queryText, maxCount);
		}
		return imap.SearchFolderHeaders(account, password, folder, queryText, maxCount);
	}

	public string FetchPlainBodyForFilter(MailAccount account, string password, string folder, ulong uid)
	{
		if (EwsMailBridge.IsEwsAccount(account))
		{
			MailMessageBody mailMessageBody = EwsMailBridge.FetchBody(account, password, folder, uid);
			if (mailMessageBody == null)
			{
				return "";
			}
			return mailMessageBody.GetReadingPaneText();
		}
		return imap.FetchPlainBodyForFilter(account, password, folder, uid);
	}

	public void DeleteMessages(MailAccount account, string password, string folder, List<ulong> uids)
	{
		if (EwsMailBridge.IsEwsAccount(account))
		{
			EwsMailBridge.DeleteMessages(account, password, folder, uids);
		}
		else
		{
			imap.DeleteMessages(account, password, folder, uids);
		}
	}

	public void MoveMessages(MailAccount account, string password, string folder, string destFolder, List<ulong> uids)
	{
		if (EwsMailBridge.IsEwsAccount(account))
		{
			EwsMailBridge.MoveMessages(account, password, folder, destFolder, uids);
		}
		else
		{
			imap.MoveMessages(account, password, folder, destFolder, uids);
		}
	}

	public string SaveAttachmentToFile(MailAccount account, string password, string folder, ulong uid, int partIndex, string destPath)
	{
		if (EwsMailBridge.IsEwsAccount(account))
		{
			return EwsMailBridge.SaveAttachmentToFile(account, password, folder, uid, partIndex, destPath);
		}
		return imap.SaveAttachmentToFile(account, password, folder, uid, partIndex, destPath);
	}

	public string ResolveDraftsFolderFullName(MailAccount account, string password)
	{
		if (EwsMailBridge.IsEwsAccount(account))
		{
			return "drafts";
		}
		return imap.ResolveDraftsFolderFullName(account, password);
	}

	public string AppendDraft(MailAccount account, string password, MimeMessage mime, ref ulong newUid)
	{
		if (EwsMailBridge.IsEwsAccount(account))
		{
			return "EWS drafts use compose send.";
		}
		return imap.AppendDraft(account, password, mime, ref newUid);
	}

	public string ReplaceDraft(MailAccount account, string password, string folder, ulong uid, MimeMessage mime, ref ulong newUid)
	{
		if (EwsMailBridge.IsEwsAccount(account))
		{
			return "EWS drafts use compose send.";
		}
		return imap.ReplaceDraft(account, password, folder, uid, mime, ref newUid);
	}

	public void CopyMessages(MailAccount account, string password, string folder, string destFolder, List<ulong> uids)
	{
		if (EwsMailBridge.IsEwsAccount(account))
		{
			EwsMailBridge.CopyMessages(account, password, folder, destFolder, uids);
		}
		else
		{
			imap.CopyMessages(account, password, folder, destFolder, uids);
		}
	}

	public void MoveToJunk(MailAccount account, string password, string folder, List<ulong> uids)
	{
		if (EwsMailBridge.IsEwsAccount(account))
		{
			EwsMailBridge.MoveToJunk(account, password, folder, uids);
		}
		else
		{
			imap.MoveToJunk(account, password, folder, uids);
		}
	}

	public void AddKeyword(MailAccount account, string password, string folder, ulong uid, string keyword)
	{
		if (EwsMailBridge.IsEwsAccount(account))
		{
			EwsMailBridge.AddKeyword(account, password, folder, uid, keyword);
		}
		else
		{
			imap.AddKeyword(account, password, folder, uid, keyword);
		}
	}
}
