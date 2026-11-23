using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using Microsoft.VisualBasic.CompilerServices;
using MimeKit;

namespace LCARSMail;

public class ImapMailService
{
	[DebuggerNonUserCode]
	public ImapMailService()
	{
	}

	public List<MailFolderInfo> ListFolders(MailAccount account, string password)
	{
		using ImapClient imapClient = Connect(account, password);
		IMailFolder root = imapClient.Inbox;
		if (imapClient.PersonalNamespaces != null && imapClient.PersonalNamespaces.Count > 0)
		{
			root = imapClient.GetFolder(imapClient.PersonalNamespaces[0]);
		}
		return CollectFolders(imapClient, root);
	}

	public List<MailMessageHeader> SyncHeaders(MailAccount account, string password, string folder, int maxCount = 500)
	{
		using ImapClient client = Connect(account, password);
		IMailFolder mailFolder = OpenFolder(client, folder);
		CancellationToken cancellationToken = default(CancellationToken);
		mailFolder.Open(FolderAccess.ReadOnly, cancellationToken);
		IList<UniqueId> list = mailFolder.Search(SearchQuery.All, cancellationToken);
		if (list == null || list.Count == 0)
		{
			List<MailMessageHeader> list2 = new List<MailMessageHeader>();
			MailCache.SaveHeaders(account.Id, mailFolder.FullName, list2);
			return list2;
		}
		List<UniqueId> list3 = list.OrderBy([SpecialName] (UniqueId u) => u.Id).ToList();
		int num = list3.Count;
		if (maxCount > 0)
		{
			num = Math.Min(maxCount, list3.Count);
		}
		List<UniqueId> uids = list3.Skip(checked(list3.Count - num)).ToList();
		MessageSummaryItems items = MessageSummaryItems.BodyStructure | MessageSummaryItems.Envelope | MessageSummaryItems.Flags | MessageSummaryItems.Size | MessageSummaryItems.UniqueId | MessageSummaryItems.Headers;
		IList<IMessageSummary> source = mailFolder.Fetch(uids, items, cancellationToken);
		List<MailMessageHeader> list4 = new List<MailMessageHeader>();
		foreach (IMessageSummary item in source.OrderByDescending([SpecialName] (IMessageSummary s) => s.UniqueId.Id))
		{
			list4.Add(HeaderFromSummary(mailFolder.FullName, item));
		}
		MailCache.SaveHeaders(account.Id, mailFolder.FullName, list4);
		return list4;
	}

	public MailMessageBody FetchBody(MailAccount account, string password, string folder, ulong uid)
	{
		using ImapClient client = Connect(account, password);
		IMailFolder mailFolder = OpenFolder(client, folder);
		CancellationToken cancellationToken = default(CancellationToken);
		mailFolder.Open(FolderAccess.ReadOnly, cancellationToken);
		UniqueId uid2 = new UniqueId(checked((uint)uid));
		MimeMessage message = mailFolder.GetMessage(uid2, cancellationToken);
		MailAutocryptService.ApplyAutocryptFromMessage(message);
		string plainText = "";
		string htmlText = "";
		bool flag = false;
		if (MailPgpService.MessageLooksEncrypted(message))
		{
			flag = MailPgpService.TryDecryptMessage(message, "", ref plainText, ref htmlText);
			if (!flag)
			{
				string text = ExtractFirstFromAddress(message);
				if (text.Length > 0 && MailAutocryptService.TryImportWkdForEmail(text))
				{
					flag = MailPgpService.TryDecryptMessage(message, "", ref plainText, ref htmlText);
				}
			}
		}
		MailMessageBody mailMessageBody = new MailMessageBody();
		object textBody2;
		if (plainText.Length <= 0)
		{
			string textBody = message.TextBody;
			textBody2 = ((textBody != null) ? textBody : "");
		}
		else
		{
			textBody2 = plainText;
		}
		mailMessageBody.TextBody = (string)textBody2;
		object htmlBody2;
		if (htmlText.Length <= 0)
		{
			string htmlBody = message.HtmlBody;
			htmlBody2 = ((htmlBody != null) ? htmlBody : "");
		}
		else
		{
			htmlBody2 = htmlText;
		}
		mailMessageBody.HtmlBody = (string)htmlBody2;
		mailMessageBody.RequiresPgpDecrypt = ((MailPgpService.MessageLooksEncrypted(message) && !flag) ? true : false);
		mailMessageBody.Attachments = CollectAttachmentInfo(message);
		mailMessageBody.IcsCalendarParts = CollectCalendarIcsParts(message);
		MailMessageBody mailMessageBody2 = mailMessageBody;
		MailCache.SaveBody(account.Id, mailFolder.FullName, uid, mailMessageBody2);
		return mailMessageBody2;
	}

	public string SaveAttachmentToFile(MailAccount account, string password, string folder, ulong uid, int partIndex, string destPath)
	{
		string result;
		if (partIndex < 0)
		{
			result = "Invalid attachment.";
		}
		else if (string.IsNullOrWhiteSpace(destPath))
		{
			result = "Save path is required.";
		}
		else
		{
			try
			{
				using (ImapClient client = Connect(account, password))
				{
					IMailFolder mailFolder = OpenFolder(client, folder);
					CancellationToken cancellationToken = default(CancellationToken);
					mailFolder.Open(FolderAccess.ReadOnly, cancellationToken);
					UniqueId uid2 = new UniqueId(checked((uint)uid));
					MimeMessage message = mailFolder.GetMessage(uid2, cancellationToken);
					MimeEntity attachmentEntity = GetAttachmentEntity(message, partIndex);
					if (attachmentEntity != null)
					{
						using (FileStream stream = File.Create(destPath))
						{
							WriteAttachmentEntity(attachmentEntity, stream);
						}
						goto end_IL_0030;
					}
					result = "Attachment not found.";
					goto end_IL_0026;
					end_IL_0030:;
				}
				result = "";
				end_IL_0026:;
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Exception ex2 = ex;
				string message2 = ex2.Message;
				string text = ((message2 != null) ? message2 : "Save failed.").Trim();
				if (text.Length == 0)
				{
					text = "Save failed.";
				}
				result = text;
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}

	public void MarkSeen(MailAccount account, string password, string folder, ulong uid)
	{
		string folder2 = "";
		using (ImapClient client = Connect(account, password))
		{
			IMailFolder mailFolder = OpenFolder(client, folder);
			folder2 = mailFolder.FullName;
			CancellationToken cancellationToken = default(CancellationToken);
			mailFolder.Open(FolderAccess.ReadWrite, cancellationToken);
			UniqueId uid2 = new UniqueId(checked((uint)uid));
			mailFolder.AddFlags(uid2, MessageFlags.Seen, silent: true, cancellationToken);
		}
		MailCache.UpdateHeaderSeen(account.Id, folder2, uid, isSeen: true);
	}

	public void MarkUnseen(MailAccount account, string password, string folder, ulong uid)
	{
		string folder2 = "";
		using (ImapClient client = Connect(account, password))
		{
			IMailFolder mailFolder = OpenFolder(client, folder);
			folder2 = mailFolder.FullName;
			CancellationToken cancellationToken = default(CancellationToken);
			mailFolder.Open(FolderAccess.ReadWrite, cancellationToken);
			UniqueId uid2 = new UniqueId(checked((uint)uid));
			mailFolder.RemoveFlags(uid2, MessageFlags.Seen, silent: true, cancellationToken);
		}
		MailCache.UpdateHeaderSeen(account.Id, folder2, uid, isSeen: false);
	}

	public string ResolveDraftsFolderFullName(MailAccount account, string password)
	{
		string result;
		try
		{
			using ImapClient client = Connect(account, password);
			IMailFolder mailFolder = TryFindDraftsFolder(client);
			result = ((mailFolder != null) ? mailFolder.FullName : "");
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			result = "";
			ProjectData.ClearProjectError();
		}
		return result;
	}

	public string AppendDraft(MailAccount account, string password, MimeMessage message)
	{
		ulong newUid = 0uL;
		return AppendDraft(account, password, message, ref newUid);
	}

	public string AppendDraft(MailAccount account, string password, MimeMessage message, ref ulong newUid)
	{
		newUid = 0uL;
		string result;
		if (message == null)
		{
			result = "Nothing to save.";
		}
		else
		{
			try
			{
				using (ImapClient client = Connect(account, password))
				{
					IMailFolder mailFolder = TryFindDraftsFolder(client);
					if (mailFolder != null)
					{
						CancellationToken cancellationToken = default(CancellationToken);
						mailFolder.Open(FolderAccess.ReadWrite, cancellationToken);
						UniqueId? uniqueId = mailFolder.Append(message, MessageFlags.Draft, cancellationToken);
						if (uniqueId.HasValue)
						{
							newUid = uniqueId.Value.Id;
						}
						goto end_IL_001f;
					}
					result = "Drafts folder not found.";
					goto end_IL_0015;
					end_IL_001f:;
				}
				result = "";
				end_IL_0015:;
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Exception ex2 = ex;
				result = ex2.Message;
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}

	public string ReplaceDraft(MailAccount account, string password, string folderFullName, ulong oldUid, MimeMessage message, ref ulong newUid)
	{
		newUid = 0uL;
		string result;
		if (message == null)
		{
			result = "Nothing to save.";
		}
		else if (oldUid == 0)
		{
			result = AppendDraft(account, password, message, ref newUid);
		}
		else
		{
			try
			{
				using (ImapClient client = Connect(account, password))
				{
					IMailFolder mailFolder = OpenFolder(client, folderFullName);
					CancellationToken cancellationToken = default(CancellationToken);
					mailFolder.Open(FolderAccess.ReadWrite, cancellationToken);
					UniqueId uid = new UniqueId(checked((uint)oldUid));
					mailFolder.AddFlags(uid, MessageFlags.Deleted, silent: true, cancellationToken);
					mailFolder.Expunge(cancellationToken);
					UniqueId? uniqueId = mailFolder.Append(message, MessageFlags.Draft, cancellationToken);
					if (uniqueId.HasValue)
					{
						newUid = uniqueId.Value.Id;
					}
				}
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
		return result;
	}

	public List<MailMessageHeader> SearchFolderHeaders(MailAccount account, string password, string folder, string queryText, int maxCount = 200)
	{
		List<MailMessageHeader> list = new List<MailMessageHeader>();
		string text = ((queryText != null) ? queryText : "").Trim();
		if (text.Length == 0)
		{
			return list;
		}
		using (ImapClient client = Connect(account, password))
		{
			IMailFolder mailFolder = OpenFolder(client, folder);
			CancellationToken cancellationToken = default(CancellationToken);
			mailFolder.Open(FolderAccess.ReadOnly, cancellationToken);
			SearchQuery left = SearchQuery.Or(SearchQuery.SubjectContains(text), SearchQuery.FromContains(text));
			left = SearchQuery.Or(left, SearchQuery.BodyContains(text));
			IList<UniqueId> list2 = mailFolder.Search(left, cancellationToken);
			if (list2 == null || list2.Count == 0)
			{
				return list;
			}
			List<UniqueId> uids = list2.OrderByDescending([SpecialName] (UniqueId u) => u.Id).Take(maxCount).ToList();
			MessageSummaryItems items = MessageSummaryItems.Envelope | MessageSummaryItems.Flags | MessageSummaryItems.Size | MessageSummaryItems.UniqueId | MessageSummaryItems.Headers;
			IList<IMessageSummary> list3 = mailFolder.Fetch(uids, items, cancellationToken);
			foreach (IMessageSummary item in list3)
			{
				list.Add(HeaderFromSummary(mailFolder.FullName, item));
			}
		}
		return list;
	}

	private static IMailFolder TryFindDraftsFolder(ImapClient client)
	{
		if (client == null)
		{
			return null;
		}
		try
		{
			IMailFolder folder = client.GetFolder(SpecialFolder.Drafts);
			if (folder != null && folder.Exists)
			{
				return folder;
			}
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
		IMailFolder root = client.Inbox;
		if (client.PersonalNamespaces != null && client.PersonalNamespaces.Count > 0)
		{
			root = client.GetFolder(client.PersonalNamespaces[0]);
		}
		CancellationToken cancellationToken = default(CancellationToken);
		foreach (MailFolderInfo item in CollectFolders(client, root))
		{
			if (item == null)
			{
				continue;
			}
			MailFolderInfo mailFolderInfo = item;
			string text = ((mailFolderInfo.FullName != null) ? mailFolderInfo.FullName : "");
			if (text.IndexOf("draft", StringComparison.OrdinalIgnoreCase) < 0)
			{
				continue;
			}
			try
			{
				IMailFolder folder2 = client.GetFolder(item.FullName, cancellationToken);
				if (folder2 != null && folder2.Exists)
				{
					return folder2;
				}
			}
			catch (Exception projectError2)
			{
				ProjectData.SetProjectError(projectError2);
				ProjectData.ClearProjectError();
			}
		}
		return null;
	}

	public string FetchPlainBodyForFilter(MailAccount account, string password, string folder, ulong uid)
	{
		MailMessageBody body = null;
		if (MailCache.TryLoadBody(account.Id, folder, uid, ref body) && body != null)
		{
			return body.GetReadingPaneText();
		}
		MailMessageBody mailMessageBody = FetchBody(account, password, folder, uid);
		if (mailMessageBody == null)
		{
			return "";
		}
		return mailMessageBody.GetReadingPaneText();
	}

	public void CopyMessages(MailAccount account, string password, string sourceFolder, string destFolder, IEnumerable<ulong> uids)
	{
		if (uids == null)
		{
			return;
		}
		List<ulong> list = uids.Where([SpecialName] (ulong u) => u != 0).Distinct().ToList();
		if (list.Count == 0)
		{
			return;
		}
		using ImapClient client = Connect(account, password);
		IMailFolder mailFolder = OpenFolder(client, sourceFolder);
		CancellationToken cancellationToken = default(CancellationToken);
		mailFolder.Open(FolderAccess.ReadWrite, cancellationToken);
		IMailFolder destination = OpenFolder(client, destFolder);
		IList<UniqueId> uids2 = list.Select([SpecialName] (ulong u) => new UniqueId(checked((uint)u))).ToList();
		mailFolder.CopyTo(uids2, destination, cancellationToken);
	}

	public void MoveToJunk(MailAccount account, string password, string folder, IEnumerable<ulong> uids)
	{
		if (uids == null)
		{
			return;
		}
		List<ulong> list = uids.Where([SpecialName] (ulong u) => u != 0).Distinct().ToList();
		if (list.Count == 0)
		{
			return;
		}
		using ImapClient client = Connect(account, password);
		IMailFolder mailFolder = OpenFolder(client, folder);
		CancellationToken cancellationToken = default(CancellationToken);
		mailFolder.Open(FolderAccess.ReadWrite, cancellationToken);
		IMailFolder mailFolder2 = TryFindJunkFolder(client, mailFolder);
		IList<UniqueId> uids2 = list.Select([SpecialName] (ulong u) => new UniqueId(checked((uint)u))).ToList();
		if (mailFolder2 != null && !FolderNamesEqual(mailFolder2.FullName, mailFolder.FullName))
		{
			mailFolder.MoveTo(uids2, mailFolder2, cancellationToken);
			return;
		}
		mailFolder.AddFlags(uids2, MessageFlags.Deleted, silent: true, cancellationToken);
		mailFolder.Expunge(cancellationToken);
	}

	public void AddKeyword(MailAccount account, string password, string folder, ulong uid, string keyword)
	{
		string text = ((keyword != null) ? keyword : "").Trim();
		if (text.Length == 0)
		{
			return;
		}
		using ImapClient client = Connect(account, password);
		IMailFolder mailFolder = OpenFolder(client, folder);
		CancellationToken cancellationToken = default(CancellationToken);
		mailFolder.Open(FolderAccess.ReadWrite, cancellationToken);
		List<UniqueId> list = new List<UniqueId>();
		UniqueId item = new UniqueId(checked((uint)uid));
		list.Add(item);
		mailFolder.AddLabels(list, new List<string> { text }, silent: true, cancellationToken);
	}

	public void DeleteMessages(MailAccount account, string password, string folder, IEnumerable<ulong> uids)
	{
		if (uids == null)
		{
			return;
		}
		List<ulong> list = uids.Where([SpecialName] (ulong u) => u != 0).Distinct().ToList();
		if (list.Count == 0)
		{
			return;
		}
		using ImapClient client = Connect(account, password);
		IMailFolder mailFolder = OpenFolder(client, folder);
		CancellationToken cancellationToken = default(CancellationToken);
		mailFolder.Open(FolderAccess.ReadWrite, cancellationToken);
		IList<UniqueId> uids2 = list.Select([SpecialName] (ulong u) => new UniqueId(checked((uint)u))).ToList();
		IMailFolder mailFolder2 = TryFindTrashFolder(client, mailFolder);
		if (mailFolder2 != null && !FolderNamesEqual(mailFolder2.FullName, mailFolder.FullName))
		{
			mailFolder.MoveTo(uids2, mailFolder2, cancellationToken);
			return;
		}
		mailFolder.AddFlags(uids2, MessageFlags.Deleted, silent: true, cancellationToken);
		mailFolder.Expunge(cancellationToken);
	}

	public void MoveMessages(MailAccount account, string password, string sourceFolder, string destFolder, IEnumerable<ulong> uids)
	{
		if (uids == null)
		{
			return;
		}
		List<ulong> list = uids.Where([SpecialName] (ulong u) => u != 0).Distinct().ToList();
		if (list.Count == 0)
		{
			return;
		}
		string text = ((destFolder != null) ? destFolder : "").Trim();
		if (text.Length == 0)
		{
			throw new ArgumentException("Destination folder is required.", "destFolder");
		}
		using ImapClient client = Connect(account, password);
		IMailFolder mailFolder = OpenFolder(client, sourceFolder);
		if (!FolderNamesEqual(mailFolder.FullName, text))
		{
			CancellationToken cancellationToken = default(CancellationToken);
			mailFolder.Open(FolderAccess.ReadWrite, cancellationToken);
			IMailFolder destination = OpenFolder(client, text);
			IList<UniqueId> uids2 = list.Select([SpecialName] (ulong u) => new UniqueId(checked((uint)u))).ToList();
			mailFolder.MoveTo(uids2, destination, cancellationToken);
		}
	}

	private static IMailFolder TryFindTrashFolder(ImapClient client, IMailFolder currentFolder)
	{
		if (client == null)
		{
			return null;
		}
		try
		{
			IMailFolder folder = client.GetFolder(SpecialFolder.Trash);
			if (folder != null && folder.Exists)
			{
				return folder;
			}
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
		IMailFolder root = client.Inbox;
		if (client.PersonalNamespaces != null && client.PersonalNamespaces.Count > 0)
		{
			root = client.GetFolder(client.PersonalNamespaces[0]);
		}
		List<MailFolderInfo> list = CollectFolders(client, root);
		CancellationToken cancellationToken = default(CancellationToken);
		foreach (MailFolderInfo item in list)
		{
			if (item == null || (!IsTrashFolderName(item.FullName) && !IsTrashFolderName(item.DisplayName)))
			{
				continue;
			}
			try
			{
				IMailFolder folder2 = client.GetFolder(item.FullName, cancellationToken);
				if (folder2 != null && folder2.Exists)
				{
					return folder2;
				}
			}
			catch (Exception projectError2)
			{
				ProjectData.SetProjectError(projectError2);
				ProjectData.ClearProjectError();
			}
		}
		return null;
	}

	private static bool IsTrashFolderName(string name)
	{
		string text = ((name != null) ? name : "").Trim();
		if (text.Length == 0)
		{
			return false;
		}
		if (string.Equals(text, "Trash", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (string.Equals(text, "Deleted", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (string.Equals(text, "Deleted Items", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (string.Equals(text, "Bin", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (text.EndsWith(".Trash", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (text.EndsWith("/Trash", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (text.EndsWith("\\Trash", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (string.Equals(text, "[Gmail]/Trash", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (string.Equals(text, "INBOX.Trash", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		return false;
	}

	private static bool FolderNamesEqual(string a, string b)
	{
		return string.Equals(((a != null) ? a : "").Trim(), ((b != null) ? b : "").Trim(), StringComparison.OrdinalIgnoreCase);
	}

	private static ImapClient Connect(MailAccount account, string password)
	{
		if (account == null)
		{
			throw new ArgumentNullException("account");
		}
		if (password == null)
		{
			password = "";
		}
		MailAccount mailAccount = account;
		string text = ((mailAccount.ImapHost != null) ? mailAccount.ImapHost : "").Trim();
		if (text.Length == 0)
		{
			throw new ArgumentException("ImapHost is required.", "account");
		}
		int num = account.ImapPort;
		if (num <= 0)
		{
			if (account.ImapSsl)
			{
				num = 993;
			}
			else
			{
				num = 143;
			}
		}
		mailAccount = account;
		string text2 = ((mailAccount.Username != null) ? mailAccount.Username : "").Trim();
		if (text2.Length == 0)
		{
			mailAccount = account;
			text2 = ((mailAccount.EmailAddress != null) ? mailAccount.EmailAddress : "").Trim();
		}
		ImapClient imapClient = new ImapClient();
		bool strictMode = ((account != null && account.StrictTls) ? true : false);
		imapClient.ServerCertificateValidationCallback = [SpecialName] (object s, X509Certificate cert, X509Chain chain, SslPolicyErrors errors) => TlsPolicyStore.ValidateCertificate(text, num, cert as X509Certificate2, errors, strictMode);
		CancellationToken cancellationToken = default(CancellationToken);
		imapClient.Connect(text, num, ResolveSecureSocketOptions(account, num), cancellationToken);
		if (MailOAuthProviders.UsesOAuth(account))
		{
			imapClient.Authenticate(new SaslMechanismOAuth2(text2, password), cancellationToken);
		}
		else
		{
			imapClient.Authenticate(text2, password, cancellationToken);
		}
		return imapClient;
	}

	private static SecureSocketOptions ResolveSecureSocketOptions(MailAccount account, int port)
	{
		if (account.ImapStartTls)
		{
			return SecureSocketOptions.StartTlsWhenAvailable;
		}
		if (account.ImapSsl)
		{
			return SecureSocketOptions.SslOnConnect;
		}
		if (port == 993)
		{
			return SecureSocketOptions.SslOnConnect;
		}
		return SecureSocketOptions.Auto;
	}

	private static IMailFolder OpenFolder(ImapClient client, string folder)
	{
		string text = ((folder != null) ? folder : "").Trim();
		if (text.Length == 0)
		{
			text = client.Inbox.FullName;
		}
		CancellationToken cancellationToken = default(CancellationToken);
		return client.GetFolder(text, cancellationToken);
	}

	private static List<MailFolderInfo> CollectFolders(ImapClient client, IMailFolder root)
	{
		List<MailFolderInfo> list = new List<MailFolderInfo>();
		AppendFolderTree(root, list);
		return list.OrderBy([SpecialName] (MailFolderInfo f) => f.FullName, StringComparer.OrdinalIgnoreCase).ToList();
	}

	private static void AppendFolderTree(IMailFolder folder, List<MailFolderInfo> result)
	{
		result.Add(new MailFolderInfo
		{
			FullName = folder.FullName,
			DisplayName = folder.Name
		});
		CancellationToken cancellationToken = default(CancellationToken);
		foreach (IMailFolder subfolder in folder.GetSubfolders(subscribedOnly: false, cancellationToken))
		{
			AppendFolderTree(subfolder, result);
		}
	}

	private static int CountNonInlineAttachmentsFromBodyStructure(BodyPart body)
	{
		if (body == null)
		{
			return -1;
		}
		return CountNonInlineAttachmentsFromBodyPart(body);
	}

	private static int CountNonInlineAttachmentsFromBodyPart(BodyPart part)
	{
		if (part == null)
		{
			return 0;
		}
		if (part is BodyPartMultipart bodyPartMultipart)
		{
			int num = 0;
			if (bodyPartMultipart.BodyParts != null)
			{
				foreach (BodyPart bodyPart in bodyPartMultipart.BodyParts)
				{
					num = checked(num + CountNonInlineAttachmentsFromBodyPart(bodyPart));
				}
			}
			return num;
		}
		if (!(part is BodyPartBasic bodyPartBasic))
		{
			return 0;
		}
		if (bodyPartBasic.ContentDisposition != null && string.Equals(bodyPartBasic.ContentDisposition.Disposition, "attachment", StringComparison.OrdinalIgnoreCase))
		{
			return 1;
		}
		string fileName = bodyPartBasic.FileName;
		string text = ((fileName != null) ? fileName : "").Trim();
		if (text.Length == 0)
		{
			return 0;
		}
		string left = "";
		if (bodyPartBasic.ContentType != null)
		{
			fileName = bodyPartBasic.ContentType.MimeType;
			left = ((fileName != null) ? fileName : "").Trim().ToLowerInvariant();
		}
		if (Operators.CompareString(left, "text/plain", TextCompare: false) == 0 || Operators.CompareString(left, "text/html", TextCompare: false) == 0)
		{
			return 0;
		}
		return 1;
	}

	private static MailMessageHeader HeaderFromSummary(string folderFullName, IMessageSummary summary)
	{
		string subject = "";
		string text = "";
		string toAddresses = "";
		string ccAddresses = "";
		DateTime dateUtc = DateTime.MinValue;
		if (summary.Envelope != null)
		{
			string subject2 = summary.Envelope.Subject;
			subject = ((subject2 != null) ? subject2 : "");
			if (summary.Envelope.From != null && summary.Envelope.From.Count > 0)
			{
				text = summary.Envelope.From.ToString();
			}
			if (summary.Envelope.To != null && summary.Envelope.To.Count > 0)
			{
				toAddresses = summary.Envelope.To.ToString();
			}
			if (summary.Envelope.Cc != null && summary.Envelope.Cc.Count > 0)
			{
				ccAddresses = summary.Envelope.Cc.ToString();
			}
			if (summary.Envelope.Date.HasValue)
			{
				dateUtc = summary.Envelope.Date.Value.UtcDateTime;
			}
		}
		bool isSeen = false;
		bool isJunk = false;
		if (summary.Flags.HasValue)
		{
			isSeen = summary.Flags.Value.HasFlag(MessageFlags.Seen);
		}
		string messageId = HeaderValue(summary, "Message-Id");
		string inReplyTo = HeaderValue(summary, "In-Reply-To");
		string references = HeaderValue(summary, "References");
		string replyTo = HeaderValue(summary, "Reply-To");
		string listId = HeaderValue(summary, "List-Id");
		string returnPath = HeaderValue(summary, "Return-Path");
		long sizeBytes = 0L;
		if (summary.Size.HasValue)
		{
			sizeBytes = summary.Size.Value;
		}
		int headerAttachmentCount = CountNonInlineAttachmentsFromBodyStructure(summary.Body);
		MailMessageHeader mailMessageHeader = new MailMessageHeader();
		mailMessageHeader.Uid = summary.UniqueId.Id;
		mailMessageHeader.Folder = folderFullName;
		mailMessageHeader.Subject = subject;
		mailMessageHeader.From = text;
		mailMessageHeader.ToAddresses = toAddresses;
		mailMessageHeader.CcAddresses = ccAddresses;
		mailMessageHeader.DateUtc = dateUtc;
		mailMessageHeader.IsSeen = isSeen;
		mailMessageHeader.MessageId = messageId;
		mailMessageHeader.InReplyTo = inReplyTo;
		mailMessageHeader.References = references;
		mailMessageHeader.ReplyTo = replyTo;
		mailMessageHeader.ListId = listId;
		mailMessageHeader.ReturnPath = returnPath;
		mailMessageHeader.SizeBytes = sizeBytes;
		mailMessageHeader.IsJunk = isJunk;
		mailMessageHeader.HeaderAttachmentCount = headerAttachmentCount;
		return mailMessageHeader;
	}

	private static string HeaderValue(IMessageSummary summary, string name)
	{
		if (summary == null || summary.Headers == null)
		{
			return "";
		}
		string text = summary.Headers[name];
		return ((text != null) ? text : "").Trim();
	}

	private static List<string> CollectCalendarIcsParts(MimeMessage message)
	{
		List<string> result = new List<string>();
		if (message == null)
		{
			return result;
		}
		CollectCalendarIcsFromEntity(message.Body, result);
		return result;
	}

	private static void CollectCalendarIcsFromEntity(MimeEntity entity, List<string> result)
	{
		if (entity == null || result == null)
		{
			return;
		}
		if (entity is Multipart multipart)
		{
			{
				foreach (MimeEntity item in multipart)
				{
					CollectCalendarIcsFromEntity(item, result);
				}
				return;
			}
		}
		if (entity is TextPart textPart)
		{
			string text = "";
			if (textPart.ContentType != null && textPart.ContentType.MimeType != null)
			{
				text = textPart.ContentType.MimeType.Trim().ToLowerInvariant();
			}
			if (Operators.CompareString(text, "text/calendar", TextCompare: false) == 0 || text.Contains("calendar"))
			{
				string text2 = textPart.Text;
				string text3 = ((text2 != null) ? text2 : "");
				if (!string.IsNullOrWhiteSpace(text3))
				{
					result.Add(text3);
				}
			}
		}
		else
		{
			if (!(entity is MimePart { FileName: var fileName } mimePart))
			{
				return;
			}
			string text4 = ((fileName != null) ? fileName : "").Trim().ToLowerInvariant();
			string left = "";
			if (mimePart.ContentType != null && mimePart.ContentType.MimeType != null)
			{
				left = mimePart.ContentType.MimeType.Trim().ToLowerInvariant();
			}
			if (!text4.EndsWith(".ics", StringComparison.OrdinalIgnoreCase) && Operators.CompareString(left, "text/calendar", TextCompare: false) != 0)
			{
				return;
			}
			using MemoryStream memoryStream = new MemoryStream();
			CancellationToken cancellationToken = default(CancellationToken);
			mimePart.Content.DecodeTo(memoryStream, cancellationToken);
			string text5 = Encoding.UTF8.GetString(memoryStream.ToArray());
			if (!string.IsNullOrWhiteSpace(text5))
			{
				result.Add(text5);
			}
		}
	}

	private static List<MailAttachmentInfo> CollectAttachmentInfo(MimeMessage message)
	{
		List<MailAttachmentInfo> list = new List<MailAttachmentInfo>();
		if (message == null)
		{
			return list;
		}
		int num = 0;
		foreach (MimeEntity item in EnumerateDownloadableParts(message.Body))
		{
			if (item != null)
			{
				bool isInline = IsInlinePart(item);
				list.Add(new MailAttachmentInfo
				{
					PartIndex = num,
					FileName = ResolveAttachmentFileName(item),
					Size = ResolveAttachmentSize(item),
					IsInline = isInline,
					ContentId = ResolveContentId(item)
				});
				num = checked(num + 1);
			}
		}
		return list;
	}

	private static MimeEntity GetAttachmentEntity(MimeMessage message, int partIndex)
	{
		if (message == null || partIndex < 0)
		{
			return null;
		}
		int num = 0;
		foreach (MimeEntity item in EnumerateDownloadableParts(message.Body))
		{
			if (num == partIndex)
			{
				return item;
			}
			num = checked(num + 1);
		}
		return null;
	}

	private static IEnumerable<MimeEntity> EnumerateDownloadableParts(MimeEntity entity)
	{
		if (entity == null)
		{
			yield break;
		}
		if (entity is Multipart multipart)
		{
			{
				foreach (MimeEntity item in multipart)
				{
					foreach (MimeEntity item2 in EnumerateDownloadableParts(item))
					{
						yield return item2;
					}
				}
				yield break;
			}
		}
		if (!(entity is TextPart) && IsAttachmentLike(entity))
		{
			yield return entity;
		}
	}

	private static bool IsAttachmentLike(MimeEntity entity)
	{
		if (entity == null)
		{
			return false;
		}
		if (entity is MimePart || entity is MessagePart)
		{
			return true;
		}
		return false;
	}

	private static bool IsInlinePart(MimeEntity entity)
	{
		if (entity == null)
		{
			return false;
		}
		if (entity.ContentDisposition != null && string.Equals(entity.ContentDisposition.Disposition, "inline", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		return ResolveContentId(entity).Length > 0;
	}

	private static string ResolveContentId(MimeEntity entity)
	{
		if (entity == null || entity.ContentId == null)
		{
			return "";
		}
		return entity.ContentId.Trim('<', '>');
	}

	private static IMailFolder TryFindJunkFolder(ImapClient client, IMailFolder currentFolder)
	{
		if (client == null)
		{
			return null;
		}
		try
		{
			IMailFolder folder = client.GetFolder(SpecialFolder.Junk);
			if (folder != null && folder.Exists)
			{
				return folder;
			}
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
		IMailFolder root = client.Inbox;
		if (client.PersonalNamespaces != null && client.PersonalNamespaces.Count > 0)
		{
			root = client.GetFolder(client.PersonalNamespaces[0]);
		}
		List<MailFolderInfo> list = CollectFolders(client, root);
		CancellationToken cancellationToken = default(CancellationToken);
		foreach (MailFolderInfo item in list)
		{
			if (item == null)
			{
				continue;
			}
			MailFolderInfo mailFolderInfo = item;
			string text = ((mailFolderInfo.FullName != null) ? mailFolderInfo.FullName : "");
			if (text.IndexOf("junk", StringComparison.OrdinalIgnoreCase) < 0 && text.IndexOf("spam", StringComparison.OrdinalIgnoreCase) < 0)
			{
				continue;
			}
			try
			{
				IMailFolder folder2 = client.GetFolder(item.FullName, cancellationToken);
				if (folder2 != null && folder2.Exists)
				{
					return folder2;
				}
			}
			catch (Exception projectError2)
			{
				ProjectData.SetProjectError(projectError2);
				ProjectData.ClearProjectError();
			}
		}
		return null;
	}

	private static void WriteAttachmentEntity(MimeEntity entity, Stream stream)
	{
		if (entity == null)
		{
			throw new ArgumentNullException("entity");
		}
		if (stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		CancellationToken cancellationToken = default(CancellationToken);
		if (entity is MessagePart messagePart)
		{
			messagePart.WriteTo(stream, cancellationToken);
		}
		else if (entity is MimePart mimePart)
		{
			mimePart.Content.DecodeTo(stream, cancellationToken);
		}
		else
		{
			entity.WriteTo(stream, cancellationToken);
		}
	}

	private static string ResolveAttachmentFileName(MimeEntity entity)
	{
		if (entity == null)
		{
			return "attachment";
		}
		if (entity is MimePart { FileName: var fileName } && !string.IsNullOrWhiteSpace(fileName))
		{
			return fileName.Trim();
		}
		if (entity.ContentDisposition != null && !string.IsNullOrWhiteSpace(entity.ContentDisposition.FileName))
		{
			return entity.ContentDisposition.FileName.Trim();
		}
		if (entity.ContentType != null && !string.IsNullOrWhiteSpace(entity.ContentType.Name))
		{
			return entity.ContentType.Name.Trim();
		}
		return "attachment";
	}

	private static long ResolveAttachmentSize(MimeEntity entity)
	{
		if (entity == null)
		{
			return 0L;
		}
		if (entity.ContentDisposition != null && entity.ContentDisposition.Size.HasValue)
		{
			return entity.ContentDisposition.Size.Value;
		}
		return 0L;
	}

	private static string ExtractFirstFromAddress(MimeMessage message)
	{
		if (message == null || message.From == null)
		{
			return "";
		}
		foreach (MailboxAddress mailbox in message.From.Mailboxes)
		{
			if (mailbox is MailboxAddress { Address: var address })
			{
				string text = ((address != null) ? address : "").Trim();
				if (text.Length > 0)
				{
					return text;
				}
			}
		}
		return "";
	}
}
