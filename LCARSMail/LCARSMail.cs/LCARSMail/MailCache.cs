using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

[StandardModule]
public sealed class MailCache
{
	private class HeaderDto
	{
		public ulong Uid
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string Subject
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string FromAddress
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string ToAddresses
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string CcAddresses
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public DateTime DateUtc
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public bool IsSeen
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string MessageId
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string InReplyTo
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string ReferencesHeader
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string ReplyToLine
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string ListIdLine
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string ReturnPathLine
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public long SizeBytes
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public bool IsJunk
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		[DebuggerNonUserCode]
		public HeaderDto()
		{
		}

		public static HeaderDto FromModel(MailMessageHeader h)
		{
			HeaderDto headerDto = new HeaderDto();
			headerDto.Uid = h.Uid;
			headerDto.Subject = ((h.Subject != null) ? h.Subject : "");
			headerDto.FromAddress = ((h.From != null) ? h.From : "");
			headerDto.ToAddresses = ((h.ToAddresses != null) ? h.ToAddresses : "");
			headerDto.CcAddresses = ((h.CcAddresses != null) ? h.CcAddresses : "");
			headerDto.DateUtc = h.DateUtc;
			headerDto.IsSeen = h.IsSeen;
			headerDto.MessageId = ((h.MessageId != null) ? h.MessageId : "");
			headerDto.InReplyTo = ((h.InReplyTo != null) ? h.InReplyTo : "");
			headerDto.ReferencesHeader = ((h.References != null) ? h.References : "");
			headerDto.ReplyToLine = ((h.ReplyTo != null) ? h.ReplyTo : "");
			headerDto.ListIdLine = ((h.ListId != null) ? h.ListId : "");
			headerDto.ReturnPathLine = ((h.ReturnPath != null) ? h.ReturnPath : "");
			headerDto.SizeBytes = h.SizeBytes;
			headerDto.IsJunk = h.IsJunk;
			return headerDto;
		}

		public MailMessageHeader ToModel(string folder)
		{
			MailMessageHeader mailMessageHeader = new MailMessageHeader();
			mailMessageHeader.Uid = Uid;
			mailMessageHeader.Folder = ((folder != null) ? folder : "");
			string subject = Subject;
			mailMessageHeader.Subject = ((subject != null) ? subject : "");
			string fromAddress = FromAddress;
			mailMessageHeader.From = ((fromAddress != null) ? fromAddress : "");
			string toAddresses = ToAddresses;
			mailMessageHeader.ToAddresses = ((toAddresses != null) ? toAddresses : "");
			string ccAddresses = CcAddresses;
			mailMessageHeader.CcAddresses = ((ccAddresses != null) ? ccAddresses : "");
			mailMessageHeader.DateUtc = DateUtc;
			mailMessageHeader.IsSeen = IsSeen;
			string messageId = MessageId;
			mailMessageHeader.MessageId = ((messageId != null) ? messageId : "");
			string inReplyTo = InReplyTo;
			mailMessageHeader.InReplyTo = ((inReplyTo != null) ? inReplyTo : "");
			string referencesHeader = ReferencesHeader;
			mailMessageHeader.References = ((referencesHeader != null) ? referencesHeader : "");
			string replyToLine = ReplyToLine;
			mailMessageHeader.ReplyTo = ((replyToLine != null) ? replyToLine : "");
			string listIdLine = ListIdLine;
			mailMessageHeader.ListId = ((listIdLine != null) ? listIdLine : "");
			string returnPathLine = ReturnPathLine;
			mailMessageHeader.ReturnPath = ((returnPathLine != null) ? returnPathLine : "");
			mailMessageHeader.SizeBytes = SizeBytes;
			mailMessageHeader.IsJunk = IsJunk;
			return mailMessageHeader;
		}
	}

	private class AttachmentDto
	{
		public int PartIndex
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string FileName
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public long Size
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public bool IsInline
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string ContentId
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		[DebuggerNonUserCode]
		public AttachmentDto()
		{
		}
	}

	private class BodyDto
	{
		public string TextBody
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string HtmlBody
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public List<AttachmentDto> Attachments
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		[DebuggerNonUserCode]
		public BodyDto()
		{
		}

		public static BodyDto FromModel(MailMessageBody body)
		{
			BodyDto bodyDto = new BodyDto();
			bodyDto.TextBody = ((body.TextBody != null) ? body.TextBody : "");
			bodyDto.HtmlBody = ((body.HtmlBody != null) ? body.HtmlBody : "");
			BodyDto bodyDto2 = bodyDto;
			bodyDto2.Attachments = new List<AttachmentDto>();
			if (body.Attachments != null)
			{
				foreach (MailAttachmentInfo attachment in body.Attachments)
				{
					if (attachment != null)
					{
						List<AttachmentDto> attachments = bodyDto2.Attachments;
						AttachmentDto attachmentDto = new AttachmentDto
						{
							PartIndex = attachment.PartIndex
						};
						MailAttachmentInfo mailAttachmentInfo = attachment;
						attachmentDto.FileName = ((mailAttachmentInfo.FileName != null) ? mailAttachmentInfo.FileName : "");
						attachmentDto.Size = attachment.Size;
						attachmentDto.IsInline = attachment.IsInline;
						MailAttachmentInfo mailAttachmentInfo2 = attachment;
						attachmentDto.ContentId = ((mailAttachmentInfo2.ContentId != null) ? mailAttachmentInfo2.ContentId : "");
						attachments.Add(attachmentDto);
					}
				}
			}
			return bodyDto2;
		}

		public MailMessageBody ToModel()
		{
			MailMessageBody mailMessageBody = new MailMessageBody();
			string textBody = TextBody;
			mailMessageBody.TextBody = ((textBody != null) ? textBody : "");
			string htmlBody = HtmlBody;
			mailMessageBody.HtmlBody = ((htmlBody != null) ? htmlBody : "");
			MailMessageBody mailMessageBody2 = mailMessageBody;
			if (Attachments != null)
			{
				foreach (AttachmentDto attachment in Attachments)
				{
					if (attachment != null)
					{
						List<MailAttachmentInfo> attachments = mailMessageBody2.Attachments;
						MailAttachmentInfo mailAttachmentInfo = new MailAttachmentInfo
						{
							PartIndex = attachment.PartIndex
						};
						htmlBody = attachment.FileName;
						mailAttachmentInfo.FileName = ((htmlBody != null) ? htmlBody : "");
						mailAttachmentInfo.Size = attachment.Size;
						mailAttachmentInfo.IsInline = attachment.IsInline;
						textBody = attachment.ContentId;
						mailAttachmentInfo.ContentId = ((textBody != null) ? textBody : "");
						attachments.Add(mailAttachmentInfo);
					}
				}
			}
			return mailMessageBody2;
		}
	}

	private const string CacheSubdir = "cache";

	private const string HeadersFileName = "headers.json";

	private const string BodiesSubdir = "bodies";

	private const string BodyJsonSuffix = ".body.json";

	private const string LegacyBodySuffix = ".txt";

	private const string AttachmentCachePrefix = "att_";

	public static List<MailMessageHeader> LoadHeaders(string accountId, string folder)
	{
		string path = HeadersPath(accountId, folder);
		List<MailMessageHeader> result;
		if (!File.Exists(path))
		{
			result = new List<MailMessageHeader>();
		}
		else
		{
			try
			{
				string input = File.ReadAllText(path, Encoding.UTF8);
				JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
				List<HeaderDto> list = javaScriptSerializer.Deserialize<List<HeaderDto>>(input);
				if (list == null)
				{
					result = new List<MailMessageHeader>();
				}
				else
				{
					List<MailMessageHeader> list2 = new List<MailMessageHeader>();
					foreach (HeaderDto item in list)
					{
						if (item != null)
						{
							list2.Add(item.ToModel(folder));
						}
					}
					result = list2;
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = new List<MailMessageHeader>();
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}

	public static void SaveHeaders(string accountId, string folder, List<MailMessageHeader> headers)
	{
		if (string.IsNullOrWhiteSpace(accountId) || headers == null)
		{
			return;
		}
		string path = FolderCacheDir(accountId, folder);
		if (!Directory.Exists(path))
		{
			Directory.CreateDirectory(path);
		}
		List<HeaderDto> list = new List<HeaderDto>();
		foreach (MailMessageHeader header in headers)
		{
			if (header != null)
			{
				list.Add(HeaderDto.FromModel(header));
			}
		}
		JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
		string contents = javaScriptSerializer.Serialize(list);
		File.WriteAllText(HeadersPath(accountId, folder), contents, Encoding.UTF8);
	}

	public static bool TryLoadBody(string accountId, string folder, ulong uid, ref MailMessageBody body)
	{
		body = null;
		bool result;
		if (string.IsNullOrWhiteSpace(accountId))
		{
			result = false;
		}
		else
		{
			string path = BodyJsonPath(accountId, folder, uid);
			if (File.Exists(path))
			{
				try
				{
					string input = File.ReadAllText(path, Encoding.UTF8);
					JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
					BodyDto bodyDto = javaScriptSerializer.Deserialize<BodyDto>(input);
					if (bodyDto != null)
					{
						body = bodyDto.ToModel();
						HydrateAttachmentCachePaths(accountId, folder, uid, body);
						result = true;
						goto IL_00c2;
					}
				}
				catch (Exception projectError)
				{
					ProjectData.SetProjectError(projectError);
					ProjectData.ClearProjectError();
				}
			}
			string path2 = LegacyBodyPath(accountId, folder, uid);
			if (!File.Exists(path2))
			{
				result = false;
			}
			else
			{
				try
				{
					body = new MailMessageBody
					{
						TextBody = File.ReadAllText(path2, Encoding.UTF8)
					};
					result = true;
				}
				catch (Exception projectError2)
				{
					ProjectData.SetProjectError(projectError2);
					body = null;
					result = false;
					ProjectData.ClearProjectError();
				}
			}
		}
		goto IL_00c2;
		IL_00c2:
		return result;
	}

	public static void SaveBody(string accountId, string folder, ulong uid, MailMessageBody body)
	{
		if (!string.IsNullOrWhiteSpace(accountId))
		{
			if (body == null)
			{
				body = new MailMessageBody();
			}
			string path = Path.Combine(FolderCacheDir(accountId, folder), "bodies");
			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}
			BodyDto obj = BodyDto.FromModel(body);
			JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
			string contents = javaScriptSerializer.Serialize(obj);
			File.WriteAllText(BodyJsonPath(accountId, folder, uid), contents, Encoding.UTF8);
		}
	}

	public static string TryGetCachedAttachmentPath(string accountId, string folder, ulong uid, int partIndex, string fileName)
	{
		string text = AttachmentCachePath(accountId, folder, uid, partIndex, fileName);
		if (File.Exists(text))
		{
			return text;
		}
		return null;
	}

	public static string SaveAttachmentCache(string accountId, string folder, ulong uid, int partIndex, string fileName, string sourcePath)
	{
		if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
		{
			return null;
		}
		string text = AttachmentCachePath(accountId, folder, uid, partIndex, fileName);
		string directoryName = Path.GetDirectoryName(text);
		if (!Directory.Exists(directoryName))
		{
			Directory.CreateDirectory(directoryName);
		}
		File.Copy(sourcePath, text, overwrite: true);
		return text;
	}

	public static void UpdateHeaderSeen(string accountId, string folder, ulong uid, bool isSeen)
	{
		List<MailMessageHeader> list = LoadHeaders(accountId, folder);
		bool flag = false;
		foreach (MailMessageHeader item in list)
		{
			if (item != null && item.Uid == uid)
			{
				item.IsSeen = isSeen;
				flag = true;
				break;
			}
		}
		if (flag)
		{
			SaveHeaders(accountId, folder, list);
		}
	}

	public static void RemoveMessageCache(string accountId, string folder, ulong uid)
	{
		if (string.IsNullOrWhiteSpace(accountId) || uid == 0)
		{
			return;
		}
		List<MailMessageHeader> list = LoadHeaders(accountId, folder);
		bool flag = false;
		checked
		{
			for (int i = list.Count - 1; i >= 0; i += -1)
			{
				MailMessageHeader mailMessageHeader = list[i];
				if (mailMessageHeader != null && mailMessageHeader.Uid == uid)
				{
					list.RemoveAt(i);
					flag = true;
				}
			}
			if (flag)
			{
				SaveHeaders(accountId, folder, list);
			}
			string path = BodyJsonPath(accountId, folder, uid);
			if (File.Exists(path))
			{
				try
				{
					File.Delete(path);
				}
				catch (Exception projectError)
				{
					ProjectData.SetProjectError(projectError);
					ProjectData.ClearProjectError();
				}
			}
			string path2 = LegacyBodyPath(accountId, folder, uid);
			if (File.Exists(path2))
			{
				try
				{
					File.Delete(path2);
				}
				catch (Exception projectError2)
				{
					ProjectData.SetProjectError(projectError2);
					ProjectData.ClearProjectError();
				}
			}
			string path3 = Path.Combine(FolderCacheDir(accountId, folder), "bodies", uid + "_files");
			if (!Directory.Exists(path3))
			{
				return;
			}
			try
			{
				Directory.Delete(path3, recursive: true);
			}
			catch (Exception projectError3)
			{
				ProjectData.SetProjectError(projectError3);
				ProjectData.ClearProjectError();
			}
		}
	}

	private static void HydrateAttachmentCachePaths(string accountId, string folder, ulong uid, MailMessageBody body)
	{
		if (body == null || body.Attachments == null)
		{
			return;
		}
		foreach (MailAttachmentInfo attachment in body.Attachments)
		{
			if (attachment != null)
			{
				attachment.CachedPath = TryGetCachedAttachmentPath(accountId, folder, uid, attachment.PartIndex, attachment.FileName);
			}
		}
	}

	private static string CacheRoot()
	{
		return Path.Combine(AccountStore.DataRoot(), "cache");
	}

	private static string AccountCacheDir(string accountId)
	{
		return Path.Combine(CacheRoot(), SanitizeFileToken(accountId));
	}

	private static string FolderCacheDir(string accountId, string folder)
	{
		return Path.Combine(AccountCacheDir(accountId), SanitizeFolderToken(folder));
	}

	private static string HeadersPath(string accountId, string folder)
	{
		return Path.Combine(FolderCacheDir(accountId, folder), "headers.json");
	}

	private static string BodyJsonPath(string accountId, string folder, ulong uid)
	{
		return Path.Combine(FolderCacheDir(accountId, folder), "bodies", uid + ".body.json");
	}

	private static string LegacyBodyPath(string accountId, string folder, ulong uid)
	{
		return Path.Combine(FolderCacheDir(accountId, folder), "bodies", uid + ".txt");
	}

	private static string AttachmentCachePath(string accountId, string folder, ulong uid, int partIndex, string fileName)
	{
		string path = Path.Combine(FolderCacheDir(accountId, folder), "bodies", uid + "_files");
		string text = SanitizeFileToken((fileName != null) ? fileName : "attachment");
		return Path.Combine(path, "att_" + partIndex + "_" + text);
	}

	private static string SanitizeFolderToken(string folder)
	{
		if (string.IsNullOrWhiteSpace(folder))
		{
			return "INBOX";
		}
		string token = folder.Replace('/', '_').Replace('\\', '_');
		return SanitizeFileToken(token);
	}

	private static string SanitizeFileToken(string token)
	{
		char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
		StringBuilder stringBuilder = new StringBuilder(token.Length);
		int i = 0;
		for (int length = token.Length; i < length; i = checked(i + 1))
		{
			char value = token[i];
			if (Array.IndexOf(invalidFileNameChars, value) >= 0)
			{
				stringBuilder.Append('_');
			}
			else
			{
				stringBuilder.Append(value);
			}
		}
		string text = stringBuilder.ToString();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "folder";
		}
		return text;
	}
}
