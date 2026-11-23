using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Xml;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

public sealed class EwsMailService
{
	public class EwsMailSummary
	{
		public string ItemId;

		public string Subject;

		public string FromLine;

		public DateTime DateUtc;

		[DebuggerNonUserCode]
		public EwsMailSummary()
		{
		}
	}

	public class EwsAttachmentInfo
	{
		public string AttachmentId;

		public string FileName;

		[DebuggerNonUserCode]
		public EwsAttachmentInfo()
		{
		}
	}

	public class EwsMessageBody
	{
		public string Subject;

		public string BodyText;

		public List<EwsAttachmentInfo> Attachments;

		[DebuggerNonUserCode]
		public EwsMessageBody()
		{
		}
	}

	public class EwsFolderInfo
	{
		public string FolderId;

		public string DisplayName;

		[DebuggerNonUserCode]
		public EwsFolderInfo()
		{
		}
	}

	public class EwsCalendarSummary
	{
		public string ItemId;

		public string ChangeKey;

		public string Subject;

		public string StartText;

		[DebuggerNonUserCode]
		public EwsCalendarSummary()
		{
		}
	}

	public class EwsCalendarEventDetails
	{
		public string Uid;

		public string ItemId;

		public string ChangeKey;

		public string Subject;

		public DateTime StartUtc;

		public DateTime EndUtc;

		public int AlarmMinutesBefore;

		public DateTime LastModifiedUtc;

		public string Location;

		public string Category;

		public bool HasRecurrence;

		public string RecurrenceSignature;

		[DebuggerNonUserCode]
		public EwsCalendarEventDetails()
		{
		}
	}

	public class EwsResolvedContact
	{
		public string DisplayName;

		public string EmailAddress;

		public string Company;

		public string BusinessPhone;

		public string MobilePhone;

		public string JobTitle;

		public string Department;

		public string WebPage;

		public string FaxNumber;

		public string PagerNumber;

		public string OfficeLocation;

		public string HomePhone;

		public string MiddleName;

		public string OtherPhone;

		public string AssistantName;

		public string FirstName;

		public string LastName;

		public string GalPersonaId;

		public string GalPersonaChangeKey;

		public string DirectoryContactItemId;

		public string DirectoryContactChangeKey;

		[DebuggerNonUserCode]
		public EwsResolvedContact()
		{
		}
	}

	public static string LastError;

	private const string SoapEnvelopeNs = "http://schemas.xmlsoap.org/soap/envelope/";

	private const string TypesNs = "http://schemas.microsoft.com/exchange/services/2006/types";

	private const string MessagesNs = "http://schemas.microsoft.com/exchange/services/2006/messages";

	public static bool LastGalPersonaUpdateDenied;

	public static bool LastGalDirectoryUpdateDenied;

	private static readonly HashSet<string> DistinguishedFolderIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "inbox", "sentitems", "drafts", "deleteditems", "junkemail", "archive", "outbox", "clutter" };

	private EwsMailService()
	{
	}

	public static string TryFindInboxItems(string ewsUrl, string username, string password, string oauthToken = null)
	{
		string envelope = "<?xml version=\"1.0\" encoding=\"utf-8\"?><soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:t=\"http://schemas.microsoft.com/exchange/services/2006/types\" xmlns:m=\"http://schemas.microsoft.com/exchange/services/2006/messages\"><soap:Body><m:FindItem Traversal=\"Shallow\"><m:ItemShape><t:BaseShape>Default</t:BaseShape></m:ItemShape><m:ParentFolderIds><t:DistinguishedFolderId Id=\"inbox\"/></m:ParentFolderIds></m:FindItem></soap:Body></soap:Envelope>";
		return PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/FindItem", envelope, oauthToken);
	}

	public static List<EwsMailSummary> ParseInboxSummaries(string soapXml)
	{
		List<EwsMailSummary> list = new List<EwsMailSummary>();
		if (string.IsNullOrWhiteSpace(soapXml))
		{
			return list;
		}
		string[] array = soapXml.Split(new string[1] { "<t:Subject>" }, StringSplitOptions.None);
		checked
		{
			int num = array.Length - 1;
			for (int i = 1; i <= num; i++)
			{
				string text = array[i];
				int num2 = text.IndexOf("</t:Subject>", StringComparison.OrdinalIgnoreCase);
				string subject = "";
				if (num2 > 0)
				{
					subject = text.Substring(0, num2);
				}
				string text2 = "ItemId Id=\"";
				int num3 = text.IndexOf(text2, StringComparison.OrdinalIgnoreCase);
				string text3 = "";
				if (num3 >= 0)
				{
					num3 += text2.Length;
					int num4 = text.IndexOf('"', num3);
					if (num4 > num3)
					{
						text3 = text.Substring(num3, num4 - num3);
					}
				}
				string text4 = ExtractFirstTagText(text, "Name");
				if (text4.Length == 0)
				{
					text4 = ExtractFirstTagText(text, "EmailAddress");
				}
				string text5 = ExtractFirstTagText(text, "DateTimeReceived");
				if (text5.Length == 0)
				{
					text5 = ExtractFirstTagText(text, "DateTimeSent");
				}
				DateTime dateUtc = ParseEwsDateTime(text5);
				if (text3.Length > 0)
				{
					list.Add(new EwsMailSummary
					{
						ItemId = text3,
						Subject = subject,
						FromLine = text4,
						DateUtc = dateUtc
					});
				}
			}
			return list;
		}
	}

	public static List<EwsMailSummary> TryFindFolderItems(string ewsUrl, string username, string password, string distinguishedFolderId, string oauthToken = null)
	{
		string text = ((distinguishedFolderId != null) ? distinguishedFolderId : "inbox").Trim();
		if (text.Length == 0)
		{
			text = "inbox";
		}
		string text2 = BuildParentFolderIdXml(text);
		List<EwsMailSummary> list = new List<EwsMailSummary>();
		int num = 250;
		int num2 = 0;
		int num3 = 0;
		checked
		{
			do
			{
				string envelope = "<?xml version=\"1.0\" encoding=\"utf-8\"?><soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:t=\"http://schemas.microsoft.com/exchange/services/2006/types\" xmlns:m=\"http://schemas.microsoft.com/exchange/services/2006/messages\"><soap:Body><m:FindItem Traversal=\"Shallow\"><m:ItemShape><t:BaseShape>Default</t:BaseShape></m:ItemShape><m:IndexedPageItemView MaxEntriesReturned=\"" + num + "\" Offset=\"" + num2 + "\" BasePoint=\"Beginning\"/><m:ParentFolderIds>" + text2 + "</m:ParentFolderIds></m:FindItem></soap:Body></soap:Envelope>";
				string text3 = PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/FindItem", envelope, oauthToken);
				if (string.IsNullOrEmpty(text3))
				{
					break;
				}
				List<EwsMailSummary> list2 = ParseInboxSummaries(text3);
				if (list2 == null || list2.Count == 0)
				{
					break;
				}
				list.AddRange(list2);
				if (list2.Count < num)
				{
					break;
				}
				num2 += list2.Count;
				num3++;
			}
			while (num3 <= 199);
			if (list.Count == 0)
			{
				if (string.IsNullOrWhiteSpace(LastError))
				{
					LastError = "FindItem returned no messages.";
				}
				return new List<EwsMailSummary>();
			}
			return list;
		}
	}

	public static List<EwsMailSummary> TryFindInboxSummaries(string ewsUrl, string username, string password, string oauthToken = null)
	{
		string text = TryFindInboxItems(ewsUrl, username, password, oauthToken);
		if (string.IsNullOrEmpty(text))
		{
			return null;
		}
		return ParseInboxSummaries(text);
	}

	public static EwsMessageBody TryGetItemBody(string ewsUrl, string username, string password, string itemId, string oauthToken = null)
	{
		LastError = null;
		string text = ((itemId != null) ? itemId : "").Trim();
		if (text.Length == 0)
		{
			LastError = "Item id is required.";
			return null;
		}
		string text2 = SecurityElement.Escape(text);
		string envelope = "<?xml version=\"1.0\" encoding=\"utf-8\"?><soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:t=\"http://schemas.microsoft.com/exchange/services/2006/types\" xmlns:m=\"http://schemas.microsoft.com/exchange/services/2006/messages\"><soap:Body><m:GetItem><m:ItemShape><t:BaseShape>Default</t:BaseShape><t:IncludeMimeContent>false</t:IncludeMimeContent></m:ItemShape><m:ItemIds><t:ItemId Id=\"" + text2 + "\"/></m:ItemIds></m:GetItem></soap:Body></soap:Envelope>";
		string text3 = PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/GetItem", envelope, oauthToken);
		if (string.IsNullOrEmpty(text3))
		{
			return null;
		}
		EwsMessageBody ewsMessageBody = new EwsMessageBody();
		ewsMessageBody.Subject = ExtractXmlElementText(text3, "Subject");
		ewsMessageBody.BodyText = ExtractXmlElementText(text3, "Body");
		if (ewsMessageBody.BodyText.Length == 0)
		{
			ewsMessageBody.BodyText = ExtractXmlElementText(text3, "t:Body");
		}
		ewsMessageBody.Attachments = ParseFileAttachments(text3);
		if (ewsMessageBody.Attachments == null)
		{
			ewsMessageBody.Attachments = new List<EwsAttachmentInfo>();
		}
		return ewsMessageBody;
	}

	public static byte[] TryGetAttachmentBytes(string ewsUrl, string username, string password, string attachmentId, string oauthToken = null)
	{
		LastError = null;
		string text = ((attachmentId != null) ? attachmentId : "").Trim();
		byte[] result;
		if (text.Length == 0)
		{
			result = null;
		}
		else
		{
			string text2 = SecurityElement.Escape(text);
			string envelope = "<?xml version=\"1.0\" encoding=\"utf-8\"?><soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:t=\"http://schemas.microsoft.com/exchange/services/2006/types\" xmlns:m=\"http://schemas.microsoft.com/exchange/services/2006/messages\"><soap:Body><m:GetAttachment><m:AttachmentIds><t:AttachmentId Id=\"" + text2 + "\"/></m:AttachmentIds></m:GetAttachment></soap:Body></soap:Envelope>";
			string text3 = PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/GetAttachment", envelope, oauthToken);
			if (string.IsNullOrEmpty(text3))
			{
				result = null;
			}
			else
			{
				string text4 = ExtractXmlElementText(text3, "Content");
				if (text4.Length == 0)
				{
					text4 = ExtractFirstTagText(text3, "Content");
				}
				if (text4.Length == 0)
				{
					result = null;
				}
				else
				{
					try
					{
						result = Convert.FromBase64String(text4);
					}
					catch (Exception ex)
					{
						ProjectData.SetProjectError(ex);
						Exception ex2 = ex;
						LastError = ex2.Message;
						result = null;
						ProjectData.ClearProjectError();
					}
				}
			}
		}
		return result;
	}

	public static bool TryMoveItems(string ewsUrl, string username, string password, IList<string> itemIds, string toDistinguishedFolderId, string oauthToken = null)
	{
		LastError = null;
		if (itemIds == null || itemIds.Count == 0)
		{
			return true;
		}
		string text = BuildDestinationFolderIdXml(((toDistinguishedFolderId != null) ? toDistinguishedFolderId : "inbox").Trim());
		StringBuilder stringBuilder = new StringBuilder();
		foreach (string itemId in itemIds)
		{
			string text2 = SecurityElement.Escape(((itemId != null) ? itemId : "").Trim());
			if (text2.Length != 0)
			{
				stringBuilder.Append("<t:ItemId Id=\"").Append(text2).Append("\"/>");
			}
		}
		if (stringBuilder.Length == 0)
		{
			return true;
		}
		string envelope = "<?xml version=\"1.0\" encoding=\"utf-8\"?><soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:t=\"http://schemas.microsoft.com/exchange/services/2006/types\" xmlns:m=\"http://schemas.microsoft.com/exchange/services/2006/messages\"><soap:Body><m:MoveItem><m:ToFolderId>" + text + "</m:ToFolderId><m:ItemIds>" + stringBuilder.ToString() + "</m:ItemIds></m:MoveItem></soap:Body></soap:Envelope>";
		string text3 = PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/MoveItem", envelope, oauthToken);
		if (string.IsNullOrEmpty(text3))
		{
			return false;
		}
		return text3.IndexOf("ResponseClass=\"Error\"", StringComparison.OrdinalIgnoreCase) < 0;
	}

	public static bool TryCopyItems(string ewsUrl, string username, string password, IList<string> itemIds, string toDistinguishedFolderId, string oauthToken = null)
	{
		LastError = null;
		if (itemIds == null || itemIds.Count == 0)
		{
			return true;
		}
		string text = BuildDestinationFolderIdXml(((toDistinguishedFolderId != null) ? toDistinguishedFolderId : "inbox").Trim());
		StringBuilder stringBuilder = new StringBuilder();
		foreach (string itemId in itemIds)
		{
			string text2 = SecurityElement.Escape(((itemId != null) ? itemId : "").Trim());
			if (text2.Length != 0)
			{
				stringBuilder.Append("<t:ItemId Id=\"").Append(text2).Append("\"/>");
			}
		}
		if (stringBuilder.Length == 0)
		{
			return true;
		}
		string envelope = "<?xml version=\"1.0\" encoding=\"utf-8\"?><soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:t=\"http://schemas.microsoft.com/exchange/services/2006/types\" xmlns:m=\"http://schemas.microsoft.com/exchange/services/2006/messages\"><soap:Body><m:CopyItem><m:ToFolderId>" + text + "</m:ToFolderId><m:ItemIds>" + stringBuilder.ToString() + "</m:ItemIds></m:CopyItem></soap:Body></soap:Envelope>";
		string text3 = PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/CopyItem", envelope, oauthToken);
		if (string.IsNullOrEmpty(text3))
		{
			return false;
		}
		return text3.IndexOf("ResponseClass=\"Error\"", StringComparison.OrdinalIgnoreCase) < 0;
	}

	public static bool TryAddItemCategory(string ewsUrl, string username, string password, string itemId, string category, string oauthToken = null)
	{
		LastError = null;
		string text = ((itemId != null) ? itemId : "").Trim();
		string text2 = ((category != null) ? category : "").Trim();
		if (text.Length == 0 || text2.Length == 0)
		{
			return false;
		}
		string text3 = SecurityElement.Escape(text);
		string text4 = SecurityElement.Escape(text2);
		string envelope = "<?xml version=\"1.0\" encoding=\"utf-8\"?><soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:t=\"http://schemas.microsoft.com/exchange/services/2006/types\" xmlns:m=\"http://schemas.microsoft.com/exchange/services/2006/messages\"><soap:Body><m:UpdateItem ConflictResolution=\"AlwaysOverwrite\" MessageDisposition=\"SaveOnly\"><m:ItemChanges><t:ItemChange><t:ItemId Id=\"" + text3 + "\"/><t:Updates><t:SetItemField FieldURI=\"item:Categories\"><t:Message><t:Categories><t:String>" + text4 + "</t:String></t:Categories></t:Message></t:SetItemField></t:Updates></t:ItemChange></m:ItemChanges></m:UpdateItem></soap:Body></soap:Envelope>";
		string text5 = PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/UpdateItem", envelope, oauthToken);
		if (string.IsNullOrEmpty(text5))
		{
			return false;
		}
		return text5.IndexOf("ResponseClass=\"Error\"", StringComparison.OrdinalIgnoreCase) < 0;
	}

	public static List<EwsFolderInfo> TryFindMailSubfolders(string ewsUrl, string username, string password, string oauthToken = null)
	{
		LastError = null;
		string envelope = "<?xml version=\"1.0\" encoding=\"utf-8\"?><soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:t=\"http://schemas.microsoft.com/exchange/services/2006/types\" xmlns:m=\"http://schemas.microsoft.com/exchange/services/2006/messages\"><soap:Body><m:FindFolder Traversal=\"Shallow\"><m:FolderShape><t:BaseShape>Default</t:BaseShape></m:FolderShape><m:ParentFolderIds><t:DistinguishedFolderId Id=\"msgfolderroot\"/></m:ParentFolderIds></m:FindFolder></soap:Body></soap:Envelope>";
		string text = PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/FindFolder", envelope, oauthToken);
		if (string.IsNullOrEmpty(text))
		{
			return new List<EwsFolderInfo>();
		}
		return ParseFolderList(text);
	}

	private static List<EwsFolderInfo> ParseFolderList(string soapXml)
	{
		List<EwsFolderInfo> list = new List<EwsFolderInfo>();
		if (string.IsNullOrWhiteSpace(soapXml))
		{
			return list;
		}
		string[] array = soapXml.Split(new string[1] { "<t:Folder>" }, StringSplitOptions.None);
		checked
		{
			int num = array.Length - 1;
			for (int i = 1; i <= num; i++)
			{
				string text = array[i];
				string text2 = "FolderId Id=\"";
				int num2 = text.IndexOf(text2, StringComparison.OrdinalIgnoreCase);
				if (num2 < 0)
				{
					continue;
				}
				num2 += text2.Length;
				int num3 = text.IndexOf('"', num2);
				if (num3 > num2)
				{
					string text3 = text.Substring(num2, num3 - num2);
					string displayName = ExtractFirstTagText(text, "DisplayName");
					if (text3.Length != 0)
					{
						list.Add(new EwsFolderInfo
						{
							FolderId = text3,
							DisplayName = displayName
						});
					}
				}
			}
			return list;
		}
	}

	public static bool TryMarkItemsRead(string ewsUrl, string username, string password, IList<string> itemIds, bool isRead, string oauthToken = null)
	{
		if (itemIds == null || itemIds.Count == 0)
		{
			return true;
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (string itemId in itemIds)
		{
			string text = SecurityElement.Escape(((itemId != null) ? itemId : "").Trim());
			if (text.Length != 0)
			{
				stringBuilder.Append("<t:ItemId Id=\"").Append(text).Append("\"/>");
			}
		}
		if (stringBuilder.Length == 0)
		{
			return true;
		}
		string value = (isRead ? "true" : "false");
		StringBuilder stringBuilder2 = new StringBuilder();
		foreach (string itemId2 in itemIds)
		{
			string text2 = SecurityElement.Escape(((itemId2 != null) ? itemId2 : "").Trim());
			if (text2.Length != 0)
			{
				stringBuilder2.Append("<t:ItemChange><t:ItemId Id=\"").Append(text2).Append("\"/>");
				stringBuilder2.Append("<t:Updates><t:SetItemField FieldURI=\"message:IsRead\">");
				stringBuilder2.Append("<t:Message><t:IsRead>").Append(value).Append("</t:IsRead></t:Message>");
				stringBuilder2.Append("</t:SetItemField></t:Updates></t:ItemChange>");
			}
		}
		if (stringBuilder2.Length == 0)
		{
			return true;
		}
		string envelope = "<?xml version=\"1.0\" encoding=\"utf-8\"?><soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:t=\"http://schemas.microsoft.com/exchange/services/2006/types\" xmlns:m=\"http://schemas.microsoft.com/exchange/services/2006/messages\"><soap:Body><m:UpdateItem ConflictResolution=\"AlwaysOverwrite\" MessageDisposition=\"SaveOnly\"><m:ItemChanges>" + stringBuilder2.ToString() + "</m:ItemChanges></m:UpdateItem></soap:Body></soap:Envelope>";
		string text3 = PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/UpdateItem", envelope, oauthToken);
		if (string.IsNullOrEmpty(text3))
		{
			return false;
		}
		return text3.IndexOf("ResponseClass=\"Error\"", StringComparison.OrdinalIgnoreCase) < 0;
	}

	private static List<EwsAttachmentInfo> ParseFileAttachments(string soapXml)
	{
		List<EwsAttachmentInfo> list = new List<EwsAttachmentInfo>();
		if (string.IsNullOrWhiteSpace(soapXml))
		{
			return list;
		}
		string text = "FileAttachment";
		int num = 0;
		checked
		{
			while (num < soapXml.Length)
			{
				int num2 = soapXml.IndexOf(text, num, StringComparison.OrdinalIgnoreCase);
				if (num2 < 0)
				{
					break;
				}
				int num3 = soapXml.IndexOf(text, num2 + text.Length, StringComparison.OrdinalIgnoreCase);
				if (num3 < 0)
				{
					num3 = Math.Min(soapXml.Length, num2 + 4000);
				}
				string chunk = soapXml.Substring(num2, num3 - num2);
				string text2 = ExtractFirstAttachmentId(chunk);
				string fileName = ExtractFirstTagText(chunk, "Name");
				if (text2.Length > 0)
				{
					list.Add(new EwsAttachmentInfo
					{
						AttachmentId = text2,
						FileName = fileName
					});
				}
				num = num2 + text.Length;
			}
			return list;
		}
	}

	private static string ExtractFirstAttachmentId(string chunk)
	{
		string text = "AttachmentId Id=\"";
		int num = chunk.IndexOf(text, StringComparison.OrdinalIgnoreCase);
		if (num < 0)
		{
			return "";
		}
		checked
		{
			num += text.Length;
			int num2 = chunk.IndexOf('"', num);
			if (num2 <= num)
			{
				return "";
			}
			return chunk.Substring(num, num2 - num);
		}
	}

	public static bool TrySendMessage(string ewsUrl, string username, string password, string toAddress, string subject, string bodyText, string oauthToken = null)
	{
		LastError = null;
		string text = ((toAddress != null) ? toAddress : "").Trim();
		if (text.Length == 0)
		{
			LastError = "To address is required.";
			return false;
		}
		string text2 = SecurityElement.Escape((subject != null) ? subject : "");
		string text3 = SecurityElement.Escape((bodyText != null) ? bodyText : "");
		string text4 = SecurityElement.Escape(text);
		string envelope = "<?xml version=\"1.0\" encoding=\"utf-8\"?><soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:t=\"http://schemas.microsoft.com/exchange/services/2006/types\" xmlns:m=\"http://schemas.microsoft.com/exchange/services/2006/messages\"><soap:Body><m:CreateItem MessageDisposition=\"SendAndSaveCopy\"><m:Items><t:Message><t:Subject>" + text2 + "</t:Subject><t:Body BodyType=\"Text\">" + text3 + "</t:Body><t:ToRecipients><t:Mailbox><t:EmailAddress>" + text4 + "</t:EmailAddress></t:Mailbox></t:ToRecipients></t:Message></m:Items></m:CreateItem></soap:Body></soap:Envelope>";
		string text5 = PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/CreateItem", envelope, oauthToken);
		if (string.IsNullOrEmpty(text5))
		{
			return false;
		}
		if (text5.IndexOf("ResponseClass=\"Error\"", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			LastError = ExtractXmlElementText(text5, "Message");
			if (LastError.Length == 0)
			{
				LastError = "EWS CreateItem failed.";
			}
			return false;
		}
		return true;
	}

	public static bool TrySendMessageHtml(string ewsUrl, string username, string password, string toAddress, string subject, string bodyHtml, string bodyText, string oauthToken = null)
	{
		LastError = null;
		string text = ((toAddress != null) ? toAddress : "").Trim();
		if (text.Length == 0)
		{
			LastError = "To address is required.";
			return false;
		}
		string text2 = ((bodyHtml != null) ? bodyHtml : "").Trim();
		string str = ((bodyText != null) ? bodyText : "").Trim();
		string text3 = "Text";
		string text4 = SecurityElement.Escape(str);
		if (text2.Length > 0)
		{
			text3 = "HTML";
			text4 = SecurityElement.Escape(text2);
		}
		string text5 = SecurityElement.Escape((subject != null) ? subject : "");
		string text6 = SecurityElement.Escape(text);
		string envelope = "<?xml version=\"1.0\" encoding=\"utf-8\"?><soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:t=\"http://schemas.microsoft.com/exchange/services/2006/types\" xmlns:m=\"http://schemas.microsoft.com/exchange/services/2006/messages\"><soap:Body><m:CreateItem MessageDisposition=\"SendAndSaveCopy\"><m:Items><t:Message><t:Subject>" + text5 + "</t:Subject><t:Body BodyType=\"" + text3 + "\">" + text4 + "</t:Body><t:ToRecipients><t:Mailbox><t:EmailAddress>" + text6 + "</t:EmailAddress></t:Mailbox></t:ToRecipients></t:Message></m:Items></m:CreateItem></soap:Body></soap:Envelope>";
		string text7 = PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/CreateItem", envelope, oauthToken);
		if (string.IsNullOrEmpty(text7))
		{
			return false;
		}
		if (text7.IndexOf("ResponseClass=\"Error\"", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			LastError = ExtractXmlElementText(text7, "Message");
			if (LastError.Length == 0)
			{
				LastError = "EWS CreateItem failed.";
			}
			return false;
		}
		return true;
	}

	public static bool TryDeleteItems(string ewsUrl, string username, string password, IList<string> itemIds, string oauthToken = null)
	{
		LastError = null;
		if (itemIds == null || itemIds.Count == 0)
		{
			return true;
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (string itemId in itemIds)
		{
			string text = SecurityElement.Escape(((itemId != null) ? itemId : "").Trim());
			if (text.Length != 0)
			{
				stringBuilder.Append("<t:ItemId Id=\"").Append(text).Append("\"/>");
			}
		}
		if (stringBuilder.Length == 0)
		{
			return true;
		}
		string envelope = "<?xml version=\"1.0\" encoding=\"utf-8\"?><soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:t=\"http://schemas.microsoft.com/exchange/services/2006/types\" xmlns:m=\"http://schemas.microsoft.com/exchange/services/2006/messages\"><soap:Body><m:DeleteItem DeleteType=\"MoveToDeletedItems\"><m:ItemIds>" + stringBuilder.ToString() + "</m:ItemIds></m:DeleteItem></soap:Body></soap:Envelope>";
		string text2 = PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/DeleteItem", envelope, oauthToken);
		if (string.IsNullOrEmpty(text2))
		{
			return false;
		}
		return text2.IndexOf("ResponseClass=\"Error\"", StringComparison.OrdinalIgnoreCase) < 0;
	}

	public static EwsCalendarEventDetails TryGetCalendarEventDetails(string ewsUrl, string username, string password, string itemId, string oauthToken = null)
	{
		LastError = null;
		string text = ((itemId != null) ? itemId : "").Trim();
		if (text.Length == 0)
		{
			LastError = "Item id is required.";
			return null;
		}
		string text2 = SecurityElement.Escape(text);
		string envelope = "<?xml version=\"1.0\" encoding=\"utf-8\"?><soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:t=\"http://schemas.microsoft.com/exchange/services/2006/types\" xmlns:m=\"http://schemas.microsoft.com/exchange/services/2006/messages\"><soap:Body><m:GetItem><m:ItemShape><t:BaseShape>Default</t:BaseShape></m:ItemShape><m:ItemIds><t:ItemId Id=\"" + text2 + "\"/></m:ItemIds></m:GetItem></soap:Body></soap:Envelope>";
		string text3 = PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/GetItem", envelope, oauthToken);
		if (string.IsNullOrEmpty(text3))
		{
			return null;
		}
		EwsCalendarEventDetails ewsCalendarEventDetails = new EwsCalendarEventDetails();
		ewsCalendarEventDetails.ItemId = text;
		ExtractItemIdAndChangeKey(text3, ref ewsCalendarEventDetails.ItemId, ref ewsCalendarEventDetails.ChangeKey);
		ewsCalendarEventDetails.Subject = ExtractXmlElementText(text3, "Subject");
		ewsCalendarEventDetails.Location = ExtractFirstTagText(text3, "Location");
		ewsCalendarEventDetails.Uid = ExtractFirstTagText(text3, "UID");
		if (ewsCalendarEventDetails.Uid.Length == 0)
		{
			ewsCalendarEventDetails.Uid = text;
		}
		string raw = ExtractFirstTagText(text3, "LastModifiedTime");
		ewsCalendarEventDetails.LastModifiedUtc = ParseEwsDateTime(raw);
		string raw2 = ExtractFirstTagText(text3, "Start");
		string raw3 = ExtractFirstTagText(text3, "End");
		ewsCalendarEventDetails.StartUtc = ParseEwsDateTime(raw2);
		ewsCalendarEventDetails.EndUtc = ParseEwsDateTime(raw3);
		if (DateTime.Compare(ewsCalendarEventDetails.EndUtc, DateTime.MinValue) == 0 && DateTime.Compare(ewsCalendarEventDetails.StartUtc, DateTime.MinValue) != 0)
		{
			ewsCalendarEventDetails.EndUtc = ewsCalendarEventDetails.StartUtc.AddHours(1.0);
		}
		string text4 = ExtractFirstTagText(text3, "ReminderMinutesBefore");
		if (text4.Length == 0)
		{
			text4 = ExtractFirstTagText(text3, "ReminderDueBy");
		}
		int result = 0;
		checked
		{
			if (int.TryParse(text4, out result) && result > 0)
			{
				ewsCalendarEventDetails.AlarmMinutesBefore = result;
			}
			else
			{
				DateTime dateTime = ParseEwsDateTime(text4);
				if (DateTime.Compare(dateTime, DateTime.MinValue) != 0 && DateTime.Compare(ewsCalendarEventDetails.StartUtc, DateTime.MinValue) != 0 && DateTime.Compare(dateTime, ewsCalendarEventDetails.StartUtc) < 0)
				{
					ewsCalendarEventDetails.AlarmMinutesBefore = (int)Math.Round(Math.Max(1.0, Math.Round((ewsCalendarEventDetails.StartUtc - dateTime).TotalMinutes)));
				}
			}
			ewsCalendarEventDetails.Category = ExtractFirstCalendarCategory(text3);
			int num = (string.IsNullOrEmpty(text3) ? (-1) : text3.IndexOf("<t:Recurrence", StringComparison.OrdinalIgnoreCase));
			if (num < 0)
			{
				num = (string.IsNullOrEmpty(text3) ? (-1) : text3.IndexOf("<Recurrence", StringComparison.OrdinalIgnoreCase));
			}
			ewsCalendarEventDetails.HasRecurrence = num >= 0;
			if (ewsCalendarEventDetails.HasRecurrence)
			{
				int num2 = text3.IndexOf("</t:Recurrence>", num, StringComparison.OrdinalIgnoreCase);
				if (num2 < 0)
				{
					num2 = text3.IndexOf("</Recurrence>", num, StringComparison.OrdinalIgnoreCase);
				}
				if (num2 > num)
				{
					string text5 = text3.Substring(num, Math.Min(256, num2 - num + 16));
					ewsCalendarEventDetails.RecurrenceSignature = text5.Replace("\r", " ").Replace("\n", " ").Trim();
				}
			}
			return ewsCalendarEventDetails;
		}
	}

	private static string ExtractFirstCalendarCategory(string soapXml)
	{
		if (string.IsNullOrWhiteSpace(soapXml))
		{
			return "";
		}
		int num = soapXml.IndexOf("<t:Categories>", StringComparison.OrdinalIgnoreCase);
		if (num < 0)
		{
			return "";
		}
		string chunk = soapXml.Substring(num, Math.Min(512, checked(soapXml.Length - num)));
		return ExtractFirstTagText(chunk, "String");
	}

	private static DateTime ParseEwsDateTime(string raw)
	{
		string text = ((raw != null) ? raw : "").Trim();
		if (text.Length == 0)
		{
			return DateTime.MinValue;
		}
		try
		{
			if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var result))
			{
				return result.ToUniversalTime();
			}
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
		return DateTime.MinValue;
	}

	public static List<EwsCalendarSummary> TryFindCalendarSummaries(string ewsUrl, string username, string password, string oauthToken = null)
	{
		string envelope = "<?xml version=\"1.0\" encoding=\"utf-8\"?><soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:t=\"http://schemas.microsoft.com/exchange/services/2006/types\" xmlns:m=\"http://schemas.microsoft.com/exchange/services/2006/messages\"><soap:Body><m:FindItem Traversal=\"Shallow\"><m:ItemShape><t:BaseShape>Default</t:BaseShape></m:ItemShape><m:ParentFolderIds><t:DistinguishedFolderId Id=\"calendar\"/></m:ParentFolderIds></m:FindItem></soap:Body></soap:Envelope>";
		string text = PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/FindItem", envelope, oauthToken);
		if (string.IsNullOrEmpty(text))
		{
			return null;
		}
		return ParseCalendarSummaries(text);
	}

	public static List<EwsCalendarSummary> ParseCalendarSummaries(string soapXml)
	{
		List<EwsCalendarSummary> list = new List<EwsCalendarSummary>();
		if (string.IsNullOrWhiteSpace(soapXml))
		{
			return list;
		}
		string[] array = soapXml.Split(new string[1] { "<t:Subject>" }, StringSplitOptions.None);
		checked
		{
			int num = array.Length - 1;
			for (int i = 1; i <= num; i++)
			{
				string text = array[i];
				int num2 = text.IndexOf("</t:Subject>", StringComparison.OrdinalIgnoreCase);
				string subject = "";
				if (num2 > 0)
				{
					subject = text.Substring(0, num2);
				}
				string startText = ExtractFirstTagText(text, "Start");
				string itemId = "";
				string changeKey = "";
				ExtractItemIdAndChangeKey(text, ref itemId, ref changeKey);
				if (itemId.Length > 0)
				{
					list.Add(new EwsCalendarSummary
					{
						ItemId = itemId,
						ChangeKey = changeKey,
						Subject = subject,
						StartText = startText
					});
				}
			}
			return list;
		}
	}

	public static List<EwsResolvedContact> TryResolveContacts(string ewsUrl, string username, string password, string query, string oauthToken = null)
	{
		LastError = null;
		string text = ((query != null) ? query : "").Trim();
		if (text.Length == 0)
		{
			LastError = "Contact search query is required.";
			return null;
		}
		string text2 = SecurityElement.Escape(text);
		string envelope = "<?xml version=\"1.0\" encoding=\"utf-8\"?><soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:t=\"http://schemas.microsoft.com/exchange/services/2006/types\" xmlns:m=\"http://schemas.microsoft.com/exchange/services/2006/messages\"><soap:Body><m:ResolveNames ReturnFullContactInformation=\"true\"><m:UnresolvedEntry>" + text2 + "</m:UnresolvedEntry></m:ResolveNames></soap:Body></soap:Envelope>";
		string text3 = PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/ResolveNames", envelope, oauthToken);
		if (string.IsNullOrEmpty(text3))
		{
			return null;
		}
		return ParseResolvedContacts(text3);
	}

	public static string TryCreateCalendarEvent(string ewsUrl, string username, string password, LcarsCalendarBridge.SharedCalendarEvent ev, string oauthToken, ref string changeKey)
	{
		LastError = null;
		changeKey = null;
		if (ev == null)
		{
			return null;
		}
		LcarsCalendarBridge.SharedCalendarEvent sharedCalendarEvent = ev;
		string text = SecurityElement.Escape((sharedCalendarEvent.Summary != null) ? sharedCalendarEvent.Summary : "LCARS EVENT");
		string text2 = ev.StartUtc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.000Z");
		string text3 = ev.EndUtc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.000Z");
		string text4 = "";
		sharedCalendarEvent = ev;
		string text5 = ((sharedCalendarEvent.Location != null) ? sharedCalendarEvent.Location : "").Trim();
		if (text5.Length > 0)
		{
			text4 = "<t:Location>" + SecurityElement.Escape(text5) + "</t:Location>";
		}
		string text6 = BuildCalendarReminderCreateXml(ev);
		string text7 = BuildCalendarCategoryCreateXml(ev);
		string envelope = "<?xml version=\"1.0\" encoding=\"utf-8\"?><soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:t=\"http://schemas.microsoft.com/exchange/services/2006/types\" xmlns:m=\"http://schemas.microsoft.com/exchange/services/2006/messages\"><soap:Body><m:CreateItem SendMeetingInvitations=\"SendToNone\"><m:SavedItemFolderId><t:DistinguishedFolderId Id=\"calendar\"/></m:SavedItemFolderId><m:Items><t:CalendarItem><t:Subject>" + text + "</t:Subject>" + text4 + text7 + text6 + "<t:Start>" + text2 + "</t:Start><t:End>" + text3 + "</t:End></t:CalendarItem></m:Items></m:CreateItem></soap:Body></soap:Envelope>";
		string text8 = PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/CreateItem", envelope, oauthToken);
		if (string.IsNullOrEmpty(text8))
		{
			return null;
		}
		string itemId = "";
		ExtractItemIdAndChangeKey(text8, ref itemId, ref changeKey);
		if (itemId.Length == 0)
		{
			itemId = ExtractFirstItemId(text8);
		}
		return itemId;
	}

	public static bool TryUpdateCalendarEvent(string ewsUrl, string username, string password, string itemId, LcarsCalendarBridge.SharedCalendarEvent ev, string oauthToken = null)
	{
		LastError = null;
		if (TryUpdateCalendarEventInternal(ewsUrl, username, password, itemId, ev, oauthToken, "AutoResolve"))
		{
			return true;
		}
		EwsCalendarEventDetails ewsCalendarEventDetails = TryGetCalendarEventDetails(ewsUrl, username, password, itemId, oauthToken);
		if (ewsCalendarEventDetails != null && ewsCalendarEventDetails.ChangeKey.Length > 0)
		{
			ev.EwsChangeKey = ewsCalendarEventDetails.ChangeKey;
			if (DateTime.Compare(ewsCalendarEventDetails.LastModifiedUtc, DateTime.MinValue) != 0)
			{
				ev.EwsLastModifiedUtc = ewsCalendarEventDetails.LastModifiedUtc;
			}
		}
		return TryUpdateCalendarEventInternal(ewsUrl, username, password, itemId, ev, oauthToken, "AlwaysOverwrite");
	}

	private static string BuildCalendarLocationUpdateField(LcarsCalendarBridge.SharedCalendarEvent ev)
	{
		object obj;
		if (ev != null)
		{
			obj = ((ev.Location != null) ? ev.Location : "");
		}
		else
		{
			obj = "";
		}
		string text = ((string)obj).Trim();
		if (text.Length == 0)
		{
			return "";
		}
		string text2 = SecurityElement.Escape(text);
		return "<t:SetItemField><t:FieldURI FieldURI=\"calendar:Location\"/><t:CalendarItem><t:Location>" + text2 + "</t:Location></t:CalendarItem></t:SetItemField>";
	}

	private static string BuildCalendarReminderCreateXml(LcarsCalendarBridge.SharedCalendarEvent ev)
	{
		if (ev == null || ev.AlarmMinutesBefore <= 0)
		{
			return "";
		}
		return "<t:ReminderIsSet>true</t:ReminderIsSet><t:ReminderMinutesBefore>" + ev.AlarmMinutesBefore + "</t:ReminderMinutesBefore>";
	}

	private static string BuildCalendarCategoryCreateXml(LcarsCalendarBridge.SharedCalendarEvent ev)
	{
		object obj;
		if (ev != null)
		{
			obj = ((ev.Category != null) ? ev.Category : "");
		}
		else
		{
			obj = "";
		}
		string text = ((string)obj).Trim();
		if (text.Length == 0)
		{
			return "";
		}
		string text2 = SecurityElement.Escape(text);
		return "<t:Categories><t:String>" + text2 + "</t:String></t:Categories>";
	}

	private static string BuildCalendarReminderUpdateField(LcarsCalendarBridge.SharedCalendarEvent ev)
	{
		if (ev == null || ev.AlarmMinutesBefore <= 0)
		{
			return "";
		}
		return "<t:SetItemField><t:FieldURI FieldURI=\"item:ReminderMinutesBefore\"/><t:CalendarItem><t:ReminderIsSet>true</t:ReminderIsSet><t:ReminderMinutesBefore>" + ev.AlarmMinutesBefore + "</t:ReminderMinutesBefore></t:CalendarItem></t:SetItemField>";
	}

	private static string BuildCalendarCategoryUpdateField(LcarsCalendarBridge.SharedCalendarEvent ev)
	{
		object obj;
		if (ev != null)
		{
			obj = ((ev.Category != null) ? ev.Category : "");
		}
		else
		{
			obj = "";
		}
		string text = ((string)obj).Trim();
		if (text.Length == 0)
		{
			return "";
		}
		string text2 = SecurityElement.Escape(text);
		return "<t:SetItemField><t:FieldURI FieldURI=\"item:Categories\"/><t:CalendarItem><t:Categories><t:String>" + text2 + "</t:String></t:Categories></t:CalendarItem></t:SetItemField>";
	}

	private static bool TryUpdateCalendarEventInternal(string ewsUrl, string username, string password, string itemId, LcarsCalendarBridge.SharedCalendarEvent ev, string oauthToken, string conflictResolution)
	{
		LastError = null;
		string text = ((itemId != null) ? itemId : "").Trim();
		if (text.Length == 0 || ev == null)
		{
			return false;
		}
		LcarsCalendarBridge.SharedCalendarEvent sharedCalendarEvent = ev;
		string text2 = SecurityElement.Escape((sharedCalendarEvent.Summary != null) ? sharedCalendarEvent.Summary : "LCARS EVENT");
		string text3 = ev.StartUtc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.000Z");
		string text4 = ev.EndUtc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.000Z");
		string text5 = SecurityElement.Escape(text);
		sharedCalendarEvent = ev;
		string text6 = SecurityElement.Escape((sharedCalendarEvent.EwsChangeKey != null) ? sharedCalendarEvent.EwsChangeKey : "");
		string text7 = "<t:ItemId Id=\"" + text5 + "\"";
		if (text6.Length > 0)
		{
			text7 = text7 + " ChangeKey=\"" + text6 + "\"";
		}
		text7 += "/>";
		string str = (string.IsNullOrWhiteSpace(conflictResolution) ? "AutoResolve" : conflictResolution.Trim());
		string envelope = "<?xml version=\"1.0\" encoding=\"utf-8\"?><soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:t=\"http://schemas.microsoft.com/exchange/services/2006/types\" xmlns:m=\"http://schemas.microsoft.com/exchange/services/2006/messages\"><soap:Body><m:UpdateItem ConflictResolution=\"" + SecurityElement.Escape(str) + "\" MessageDisposition=\"SaveOnly\"><m:ItemChanges><t:ItemChange>" + text7 + "<t:Updates><t:SetItemField><t:FieldURI FieldURI=\"item:Subject\"/><t:CalendarItem><t:Subject>" + text2 + "</t:Subject></t:CalendarItem></t:SetItemField>" + BuildCalendarLocationUpdateField(ev) + BuildCalendarCategoryUpdateField(ev) + BuildCalendarReminderUpdateField(ev) + "<t:SetItemField><t:FieldURI FieldURI=\"calendar:Start\"/><t:CalendarItem><t:Start>" + text3 + "</t:Start></t:CalendarItem></t:SetItemField><t:SetItemField><t:FieldURI FieldURI=\"calendar:End\"/><t:CalendarItem><t:End>" + text4 + "</t:End></t:CalendarItem></t:SetItemField></t:Updates></t:ItemChange></m:ItemChanges></m:UpdateItem></soap:Body></soap:Envelope>";
		string text8 = PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/UpdateItem", envelope, oauthToken);
		if (string.IsNullOrEmpty(text8))
		{
			return false;
		}
		if (text8.IndexOf("ResponseClass=\"Error\"", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return false;
		}
		string itemId2 = ev.EwsItemId;
		string changeKey = "";
		ExtractItemIdAndChangeKey(text8, ref itemId2, ref changeKey);
		if (changeKey.Length > 0)
		{
			ev.EwsChangeKey = changeKey;
		}
		if (itemId2.Length > 0)
		{
			ev.EwsItemId = itemId2;
		}
		return true;
	}

	private static string ExtractPersonaItemId(string chunk)
	{
		string text = "PersonaId Id=\"";
		int num = ((chunk != null) ? chunk : "").IndexOf(text, StringComparison.OrdinalIgnoreCase);
		if (num < 0)
		{
			return "";
		}
		checked
		{
			num += text.Length;
			int num2 = chunk.IndexOf('"', num);
			if (num2 <= num)
			{
				return "";
			}
			return chunk.Substring(num, num2 - num).Trim();
		}
	}

	private static string ExtractPersonaChangeKey(string chunk)
	{
		string value = "PersonaId Id=\"";
		int num = ((chunk != null) ? chunk : "").IndexOf(value, StringComparison.OrdinalIgnoreCase);
		if (num < 0)
		{
			return "";
		}
		string text = "ChangeKey=\"";
		int num2 = chunk.IndexOf(text, num, StringComparison.OrdinalIgnoreCase);
		if (num2 < 0)
		{
			return "";
		}
		checked
		{
			num2 += text.Length;
			int num3 = chunk.IndexOf('"', num2);
			if (num3 <= num2)
			{
				return "";
			}
			return chunk.Substring(num2, num3 - num2).Trim();
		}
	}

	public static bool TryUpdateGalPersonaContact(string ewsUrl, string username, string password, MailContact contact, string oauthToken = null)
	{
		LastError = null;
		LastGalPersonaUpdateDenied = false;
		if (contact == null)
		{
			return false;
		}
		MailContact mailContact = contact;
		string itemId = ((mailContact.GalPersonaId != null) ? mailContact.GalPersonaId : "").Trim();
		if (itemId.Length == 0)
		{
			LastError = "GAL PersonaId missing.";
			return false;
		}
		mailContact = contact;
		string text = ((mailContact.GalPersonaChangeKey != null) ? mailContact.GalPersonaChangeKey : "").Trim();
		mailContact = contact;
		string text2 = SecurityElement.Escape(((mailContact.DisplayName != null) ? mailContact.DisplayName : contact.Email).Trim());
		mailContact = contact;
		string text3 = SecurityElement.Escape(((mailContact.FirstName != null) ? mailContact.FirstName : "").Trim());
		mailContact = contact;
		string text4 = SecurityElement.Escape(((mailContact.LastName != null) ? mailContact.LastName : "").Trim());
		mailContact = contact;
		string text5 = SecurityElement.Escape(((mailContact.Company != null) ? mailContact.Company : "").Trim());
		mailContact = contact;
		string text6 = SecurityElement.Escape(((mailContact.JobTitle != null) ? mailContact.JobTitle : "").Trim());
		mailContact = contact;
		string text7 = SecurityElement.Escape(((mailContact.Department != null) ? mailContact.Department : "").Trim());
		mailContact = contact;
		string text8 = SecurityElement.Escape(((mailContact.WorkPhone != null) ? mailContact.WorkPhone : "").Trim());
		mailContact = contact;
		string text9 = SecurityElement.Escape(((mailContact.MobilePhone != null) ? mailContact.MobilePhone : "").Trim());
		string text10 = ((text.Length > 0) ? (" ChangeKey=\"" + SecurityElement.Escape(text) + "\"") : "");
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=\"persona:DisplayName\"/><t:Persona><t:DisplayName>" + text2 + "</t:DisplayName></t:Persona></t:SetItemField>");
		if (text3.Length > 0)
		{
			stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=\"persona:GivenName\"/><t:Persona><t:GivenName>" + text3 + "</t:GivenName></t:Persona></t:SetItemField>");
		}
		if (text4.Length > 0)
		{
			stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=\"persona:Surname\"/><t:Persona><t:Surname>" + text4 + "</t:Surname></t:Persona></t:SetItemField>");
		}
		if (text5.Length > 0)
		{
			stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=\"persona:CompanyName\"/><t:Persona><t:CompanyName>" + text5 + "</t:CompanyName></t:Persona></t:SetItemField>");
		}
		if (text6.Length > 0)
		{
			stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=\"persona:Title\"/><t:Persona><t:Title>" + text6 + "</t:Title></t:Persona></t:SetItemField>");
		}
		if (text7.Length > 0)
		{
			stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=\"persona:Department\"/><t:Persona><t:Department>" + text7 + "</t:Department></t:Persona></t:SetItemField>");
		}
		if (text8.Length > 0)
		{
			stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=\"persona:BusinessPhoneNumber\"/><t:Persona><t:BusinessPhoneNumber>" + text8 + "</t:BusinessPhoneNumber></t:Persona></t:SetItemField>");
		}
		if (text9.Length > 0)
		{
			stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=\"persona:MobilePhone\"/><t:Persona><t:MobilePhone>" + text9 + "</t:MobilePhone></t:Persona></t:SetItemField>");
		}
		string envelope = "<?xml version=\"1.0\" encoding=\"utf-8\"?><soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:t=\"http://schemas.microsoft.com/exchange/services/2006/types\" xmlns:m=\"http://schemas.microsoft.com/exchange/services/2006/messages\"><soap:Body><m:UpdateItem ConflictResolution=\"AlwaysOverwrite\" MessageDisposition=\"SaveOnly\"><m:ItemChanges><t:ItemChange><t:ItemId Id=\"" + SecurityElement.Escape(itemId) + "\"" + text10 + "/><t:Updates>" + stringBuilder.ToString() + "</t:Updates></t:ItemChange></m:ItemChanges></m:UpdateItem></soap:Body></soap:Envelope>";
		string text11 = PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/UpdateItem", envelope, oauthToken);
		if (string.IsNullOrEmpty(text11))
		{
			return false;
		}
		if (text11.IndexOf("ResponseClass=\"Error\"", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			LastGalPersonaUpdateDenied = true;
			LastError = "GAL Persona UpdateItem denied (directory read-only for this mailbox).";
			return false;
		}
		string changeKey = "";
		ExtractItemIdAndChangeKey(text11, ref itemId, ref changeKey);
		if (changeKey.Length > 0)
		{
			contact.GalPersonaChangeKey = changeKey;
		}
		return true;
	}

	public static bool TryRefreshGalPersonaChangeKey(string ewsUrl, string username, string password, MailContact contact, string oauthToken = null)
	{
		LastError = null;
		if (contact == null || string.IsNullOrWhiteSpace(contact.Email))
		{
			return false;
		}
		List<EwsResolvedContact> list = TryResolveContacts(ewsUrl, username, password, contact.Email.Trim(), oauthToken);
		if (list == null || list.Count == 0)
		{
			return false;
		}
		foreach (EwsResolvedContact item in list)
		{
			if (item == null || !string.Equals(item.EmailAddress, contact.Email, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			bool result = false;
			if (!string.IsNullOrWhiteSpace(item.GalPersonaId))
			{
				contact.GalPersonaId = item.GalPersonaId.Trim();
				result = true;
			}
			if (!string.IsNullOrWhiteSpace(item.GalPersonaChangeKey))
			{
				contact.GalPersonaChangeKey = item.GalPersonaChangeKey.Trim();
				result = true;
			}
			return result;
		}
		return false;
	}

	public static bool TryRefreshGalDirectoryChangeKey(string ewsUrl, string username, string password, MailContact contact, string oauthToken = null)
	{
		LastError = null;
		if (contact == null || string.IsNullOrWhiteSpace(contact.Email))
		{
			return false;
		}
		List<EwsResolvedContact> list = TryResolveContacts(ewsUrl, username, password, contact.Email.Trim(), oauthToken);
		if (list == null || list.Count == 0)
		{
			return false;
		}
		foreach (EwsResolvedContact item in list)
		{
			if (item == null || !string.Equals(item.EmailAddress, contact.Email, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			bool result = false;
			if (!string.IsNullOrWhiteSpace(item.DirectoryContactItemId))
			{
				contact.GalDirectoryItemId = item.DirectoryContactItemId.Trim();
				result = true;
			}
			if (!string.IsNullOrWhiteSpace(item.DirectoryContactChangeKey))
			{
				contact.GalDirectoryChangeKey = item.DirectoryContactChangeKey.Trim();
				result = true;
			}
			return result;
		}
		return false;
	}

	public static bool TryResolveDirectoryContactItemIds(string ewsUrl, string username, string password, MailContact contact, string oauthToken = null)
	{
		LastError = null;
		if (contact == null || string.IsNullOrWhiteSpace(contact.Email))
		{
			return false;
		}
		if (!string.IsNullOrWhiteSpace(contact.GalDirectoryItemId))
		{
			return true;
		}
		List<EwsResolvedContact> list = TryResolveContacts(ewsUrl, username, password, contact.Email.Trim(), oauthToken);
		if (list == null || list.Count == 0)
		{
			return false;
		}
		foreach (EwsResolvedContact item in list)
		{
			if (item == null || !string.Equals(item.EmailAddress, contact.Email, StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(item.DirectoryContactItemId))
			{
				continue;
			}
			contact.GalDirectoryItemId = item.DirectoryContactItemId.Trim();
			EwsResolvedContact ewsResolvedContact = item;
			contact.GalDirectoryChangeKey = ((ewsResolvedContact.DirectoryContactChangeKey != null) ? ewsResolvedContact.DirectoryContactChangeKey : "").Trim();
			return true;
		}
		return false;
	}

	public static bool TryUpdateGalDirectoryContact(string ewsUrl, string username, string password, MailContact contact, string oauthToken = null)
	{
		LastError = null;
		LastGalDirectoryUpdateDenied = false;
		if (contact == null)
		{
			return false;
		}
		MailContact mailContact = contact;
		string itemId = ((mailContact.GalDirectoryItemId != null) ? mailContact.GalDirectoryItemId : "").Trim();
		if (itemId.Length == 0)
		{
			LastError = "Directory Contact ItemId missing.";
			return false;
		}
		mailContact = contact;
		string text = ((mailContact.GalDirectoryChangeKey != null) ? mailContact.GalDirectoryChangeKey : "").Trim();
		string text2 = ((text.Length > 0) ? (" ChangeKey=\"" + SecurityElement.Escape(text) + "\"") : "");
		mailContact = contact;
		string text3 = SecurityElement.Escape(((mailContact.DisplayName != null) ? mailContact.DisplayName : contact.Email).Trim());
		mailContact = contact;
		string text4 = SecurityElement.Escape(((mailContact.FirstName != null) ? mailContact.FirstName : "").Trim());
		mailContact = contact;
		string text5 = SecurityElement.Escape(((mailContact.LastName != null) ? mailContact.LastName : "").Trim());
		mailContact = contact;
		string text6 = SecurityElement.Escape(((mailContact.Company != null) ? mailContact.Company : "").Trim());
		mailContact = contact;
		string text7 = SecurityElement.Escape(((mailContact.JobTitle != null) ? mailContact.JobTitle : "").Trim());
		mailContact = contact;
		string text8 = SecurityElement.Escape(((mailContact.WorkPhone != null) ? mailContact.WorkPhone : "").Trim());
		mailContact = contact;
		string text9 = SecurityElement.Escape(((mailContact.MobilePhone != null) ? mailContact.MobilePhone : "").Trim());
		mailContact = contact;
		string text10 = SecurityElement.Escape(((mailContact.Department != null) ? mailContact.Department : "").Trim());
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=\"contacts:DisplayName\"/><t:Contact><t:DisplayName>" + text3 + "</t:DisplayName></t:Contact></t:SetItemField>");
		if (text4.Length > 0)
		{
			stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=\"contacts:GivenName\"/><t:Contact><t:GivenName>" + text4 + "</t:GivenName></t:Contact></t:SetItemField>");
		}
		if (text5.Length > 0)
		{
			stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=\"contacts:Surname\"/><t:Contact><t:Surname>" + text5 + "</t:Surname></t:Contact></t:SetItemField>");
		}
		if (text6.Length > 0)
		{
			stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=\"contacts:CompanyName\"/><t:Contact><t:CompanyName>" + text6 + "</t:CompanyName></t:Contact></t:SetItemField>");
		}
		if (text7.Length > 0)
		{
			stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=\"contacts:JobTitle\"/><t:Contact><t:JobTitle>" + text7 + "</t:JobTitle></t:Contact></t:SetItemField>");
		}
		if (text10.Length > 0)
		{
			stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=\"contacts:Department\"/><t:Contact><t:Department>" + text10 + "</t:Department></t:Contact></t:SetItemField>");
		}
		if (text8.Length > 0)
		{
			stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=\"contacts:BusinessPhone\"/><t:Contact><t:BusinessPhone>" + text8 + "</t:BusinessPhone></t:Contact></t:SetItemField>");
		}
		if (text9.Length > 0)
		{
			stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=\"contacts:MobilePhone\"/><t:Contact><t:MobilePhone>" + text9 + "</t:MobilePhone></t:Contact></t:SetItemField>");
		}
		mailContact = contact;
		string text11 = SecurityElement.Escape(((mailContact.SecondaryEmail != null) ? mailContact.SecondaryEmail : "").Trim());
		if (text11.Length > 0)
		{
			stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=\"contacts:EmailAddress\" Key=\"EmailAddress2\"/><t:Contact><t:EmailAddresses><t:Entry Key=\"EmailAddress2\">" + text11 + "</t:Entry></t:EmailAddresses></t:Contact></t:SetItemField>");
		}
		string envelope = "<?xml version=\"1.0\" encoding=\"utf-8\"?><soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:t=\"http://schemas.microsoft.com/exchange/services/2006/types\" xmlns:m=\"http://schemas.microsoft.com/exchange/services/2006/messages\"><soap:Body><m:UpdateItem ConflictResolution=\"AlwaysOverwrite\" MessageDisposition=\"SaveOnly\"><m:ItemChanges><t:ItemChange><t:ItemId Id=\"" + SecurityElement.Escape(itemId) + "\"" + text2 + "/><t:Updates>" + stringBuilder.ToString() + "</t:Updates></t:ItemChange></m:ItemChanges></m:UpdateItem></soap:Body></soap:Envelope>";
		string text12 = PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/UpdateItem", envelope, oauthToken);
		if (string.IsNullOrEmpty(text12))
		{
			return false;
		}
		if (text12.IndexOf("ResponseClass=\"Error\"", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			LastGalDirectoryUpdateDenied = true;
			LastError = "Directory Contact UpdateItem denied (GAL read-only or insufficient rights).";
			return false;
		}
		string changeKey = "";
		ExtractItemIdAndChangeKey(text12, ref itemId, ref changeKey);
		if (changeKey.Length > 0)
		{
			contact.GalDirectoryChangeKey = changeKey;
		}
		return true;
	}

	public static bool TryUpdatePersonalContactShadow(string ewsUrl, string username, string password, MailContact contact, string oauthToken = null)
	{
		LastError = null;
		if (contact == null || string.IsNullOrWhiteSpace(contact.EwsPersonalItemId))
		{
			LastError = "Personal contact ItemId missing.";
			return false;
		}
		string str = contact.EwsPersonalItemId.Trim();
		MailContact mailContact = contact;
		string text = SecurityElement.Escape(((mailContact.DisplayName != null) ? mailContact.DisplayName : contact.Email).Trim());
		mailContact = contact;
		string text2 = SecurityElement.Escape(((mailContact.FirstName != null) ? mailContact.FirstName : "").Trim());
		mailContact = contact;
		string text3 = SecurityElement.Escape(((mailContact.LastName != null) ? mailContact.LastName : "").Trim());
		mailContact = contact;
		string text4 = SecurityElement.Escape(((mailContact.Company != null) ? mailContact.Company : "").Trim());
		mailContact = contact;
		string text5 = SecurityElement.Escape(((mailContact.JobTitle != null) ? mailContact.JobTitle : "").Trim());
		mailContact = contact;
		string text6 = SecurityElement.Escape(((mailContact.WorkPhone != null) ? mailContact.WorkPhone : "").Trim());
		mailContact = contact;
		string text7 = SecurityElement.Escape(((mailContact.MobilePhone != null) ? mailContact.MobilePhone : "").Trim());
		mailContact = contact;
		string text8 = SecurityElement.Escape(((mailContact.Department != null) ? mailContact.Department : "").Trim());
		mailContact = contact;
		string text9 = SecurityElement.Escape(((mailContact.SecondaryEmail != null) ? mailContact.SecondaryEmail : "").Trim());
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=\"contacts:DisplayName\"/><t:Contact><t:DisplayName>" + text + "</t:DisplayName></t:Contact></t:SetItemField>");
		if (text2.Length > 0)
		{
			stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=\"contacts:GivenName\"/><t:Contact><t:GivenName>" + text2 + "</t:GivenName></t:Contact></t:SetItemField>");
		}
		if (text3.Length > 0)
		{
			stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=\"contacts:Surname\"/><t:Contact><t:Surname>" + text3 + "</t:Surname></t:Contact></t:SetItemField>");
		}
		if (text4.Length > 0)
		{
			stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=\"contacts:CompanyName\"/><t:Contact><t:CompanyName>" + text4 + "</t:CompanyName></t:Contact></t:SetItemField>");
		}
		if (text5.Length > 0)
		{
			stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=\"contacts:JobTitle\"/><t:Contact><t:JobTitle>" + text5 + "</t:JobTitle></t:Contact></t:SetItemField>");
		}
		if (text8.Length > 0)
		{
			stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=\"contacts:Department\"/><t:Contact><t:Department>" + text8 + "</t:Department></t:Contact></t:SetItemField>");
		}
		if (text6.Length > 0)
		{
			stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=\"contacts:BusinessPhone\"/><t:Contact><t:BusinessPhone>" + text6 + "</t:BusinessPhone></t:Contact></t:SetItemField>");
		}
		if (text7.Length > 0)
		{
			stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=\"contacts:MobilePhone\"/><t:Contact><t:MobilePhone>" + text7 + "</t:MobilePhone></t:Contact></t:SetItemField>");
		}
		if (text9.Length > 0)
		{
			stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=\"contacts:EmailAddress\" Key=\"EmailAddress2\"/><t:Contact><t:EmailAddresses><t:Entry Key=\"EmailAddress2\">" + text9 + "</t:Entry></t:EmailAddresses></t:Contact></t:SetItemField>");
		}
		string envelope = "<?xml version=\"1.0\" encoding=\"utf-8\"?><soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:t=\"http://schemas.microsoft.com/exchange/services/2006/types\" xmlns:m=\"http://schemas.microsoft.com/exchange/services/2006/messages\"><soap:Body><m:UpdateItem ConflictResolution=\"AlwaysOverwrite\" MessageDisposition=\"SaveOnly\"><m:ItemChanges><t:ItemChange><t:ItemId Id=\"" + SecurityElement.Escape(str) + "\"/><t:Updates>" + stringBuilder.ToString() + "</t:Updates></t:ItemChange></m:ItemChanges></m:UpdateItem></soap:Body></soap:Envelope>";
		string text10 = PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/UpdateItem", envelope, oauthToken);
		if (string.IsNullOrEmpty(text10))
		{
			return false;
		}
		if (text10.IndexOf("ResponseClass=\"Error\"", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			LastError = "Personal contact UpdateItem failed.";
			return false;
		}
		return true;
	}

	public static string TryCreatePersonalContactShadow(string ewsUrl, string username, string password, MailContact contact, string oauthToken = null)
	{
		LastError = null;
		if (contact == null || string.IsNullOrWhiteSpace(contact.Email))
		{
			LastError = "Contact email required.";
			return null;
		}
		string text = contact.Email.Trim();
		MailContact mailContact = contact;
		string text2 = SecurityElement.Escape(((mailContact.FirstName != null) ? mailContact.FirstName : "").Trim());
		mailContact = contact;
		string text3 = SecurityElement.Escape(((mailContact.LastName != null) ? mailContact.LastName : "").Trim());
		mailContact = contact;
		string text4 = SecurityElement.Escape(((mailContact.DisplayName != null) ? mailContact.DisplayName : text).Trim());
		if (text2.Length == 0 && text3.Length == 0 && text4.Length > 0)
		{
			text2 = text4;
		}
		mailContact = contact;
		string text5 = SecurityElement.Escape(((mailContact.Company != null) ? mailContact.Company : "").Trim());
		mailContact = contact;
		string text6 = SecurityElement.Escape(((mailContact.JobTitle != null) ? mailContact.JobTitle : "").Trim());
		mailContact = contact;
		string text7 = SecurityElement.Escape(((mailContact.WorkPhone != null) ? mailContact.WorkPhone : "").Trim());
		mailContact = contact;
		string text8 = SecurityElement.Escape(((mailContact.MobilePhone != null) ? mailContact.MobilePhone : "").Trim());
		string text9 = SecurityElement.Escape("SMTP:" + text);
		string text10 = ((text5.Length > 0) ? ("<t:CompanyName>" + text5 + "</t:CompanyName>") : "");
		string text11 = ((text6.Length > 0) ? ("<t:JobTitle>" + text6 + "</t:JobTitle>") : "");
		string text12 = ((text7.Length > 0) ? ("<t:BusinessPhone>" + text7 + "</t:BusinessPhone>") : "");
		string text13 = ((text8.Length > 0) ? ("<t:MobilePhone>" + text8 + "</t:MobilePhone>") : "");
		string envelope = "<?xml version=\"1.0\" encoding=\"utf-8\"?><soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:t=\"http://schemas.microsoft.com/exchange/services/2006/types\" xmlns:m=\"http://schemas.microsoft.com/exchange/services/2006/messages\"><soap:Body><m:CreateItem><m:SavedItemFolderId><t:DistinguishedFolderId Id=\"contacts\"/></m:SavedItemFolderId><m:Items><t:Contact><t:DisplayName>" + text4 + "</t:DisplayName>" + ((text2.Length > 0) ? ("<t:GivenName>" + text2 + "</t:GivenName>") : "") + ((text3.Length > 0) ? ("<t:Surname>" + text3 + "</t:Surname>") : "") + text10 + text11 + text12 + text13 + "<t:EmailAddresses><t:Entry Key=\"EmailAddress1\">" + text9 + "</t:Entry></t:EmailAddresses><t:Body BodyType=\"Text\">LCARS GAL shadow — not directory write</t:Body></t:Contact></m:Items></m:CreateItem></soap:Body></soap:Envelope>";
		string text14 = PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/CreateItem", envelope, oauthToken);
		if (string.IsNullOrEmpty(text14))
		{
			return null;
		}
		if (text14.IndexOf("ResponseClass=\"Error\"", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			LastError = "EWS CreateItem contact failed.";
			return null;
		}
		string itemId = "";
		string changeKey = "";
		ExtractItemIdAndChangeKey(text14, ref itemId, ref changeKey);
		if (itemId.Length == 0)
		{
			itemId = ExtractFirstItemId(text14);
		}
		return itemId;
	}

	public static List<EwsResolvedContact> TryHarvestPersonalContactsFolder(string ewsUrl, string username, string password, string oauthToken = null)
	{
		LastError = null;
		List<EwsResolvedContact> list = new List<EwsResolvedContact>();
		int num = 100;
		int num2 = 0;
		int num3 = 0;
		checked
		{
			do
			{
				string envelope = "<?xml version=\"1.0\" encoding=\"utf-8\"?><soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:t=\"http://schemas.microsoft.com/exchange/services/2006/types\" xmlns:m=\"http://schemas.microsoft.com/exchange/services/2006/messages\"><soap:Body><m:FindItem Traversal=\"Shallow\"><m:ItemShape><t:BaseShape>Default</t:BaseShape></m:ItemShape><m:IndexedPageItemView MaxEntriesReturned=\"" + num + "\" Offset=\"" + num2 + "\" BasePoint=\"Beginning\"/><m:ParentFolderIds><t:DistinguishedFolderId Id=\"contacts\"/></m:ParentFolderIds></m:FindItem></soap:Body></soap:Envelope>";
				string text = PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/FindItem", envelope, oauthToken);
				if (string.IsNullOrEmpty(text))
				{
					break;
				}
				List<EwsResolvedContact> list2 = ParseContactFolderItems(text);
				if (list2 == null || list2.Count == 0)
				{
					break;
				}
				list.AddRange(list2);
				if (list2.Count < num)
				{
					break;
				}
				num2 += list2.Count;
				num3++;
			}
			while (num3 <= 99);
			return list;
		}
	}

	public static List<EwsResolvedContact> ParseContactFolderItems(string soapXml)
	{
		List<EwsResolvedContact> list = new List<EwsResolvedContact>();
		if (string.IsNullOrWhiteSpace(soapXml))
		{
			return list;
		}
		string[] array = soapXml.Split(new string[1] { "<t:Contact>" }, StringSplitOptions.None);
		checked
		{
			int num = array.Length - 1;
			for (int i = 1; i <= num; i++)
			{
				string chunk = array[i];
				string text = ExtractFirstTagText(chunk, "EmailAddress");
				if (text.Length == 0)
				{
					string text2 = ExtractFirstTagText(chunk, "SmtpAddress");
					if (text2.Length > 0)
					{
						text = text2;
					}
				}
				if (text.Length != 0)
				{
					string text3 = ExtractFirstTagText(chunk, "GivenName");
					string text4 = ExtractFirstTagText(chunk, "Surname");
					string middleName = ExtractFirstTagText(chunk, "MiddleName");
					string text5 = ExtractFirstTagText(chunk, "DisplayName");
					if (text5.Length == 0)
					{
						text5 = (text3 + " " + text4).Trim();
					}
					string company = ExtractFirstTagText(chunk, "CompanyName");
					string businessPhone = ExtractFirstTagText(chunk, "BusinessPhone");
					string mobilePhone = ExtractFirstTagText(chunk, "MobilePhone");
					string text6 = ExtractFirstTagText(chunk, "JobTitle");
					if (text6.Length == 0)
					{
						text6 = ExtractFirstTagText(chunk, "Title");
					}
					string department = ExtractFirstTagText(chunk, "Department");
					string webPage = ExtractFirstTagText(chunk, "BusinessHomePage");
					string faxNumber = ExtractFirstTagText(chunk, "BusinessFax");
					string pagerNumber = ExtractFirstTagText(chunk, "PagerNumber");
					string homePhone = ExtractFirstTagText(chunk, "HomePhone");
					string text7 = ExtractFirstTagText(chunk, "OtherTelephone");
					if (text7.Length == 0)
					{
						text7 = ExtractFirstTagText(chunk, "OtherPhone");
					}
					string assistantName = ExtractFirstTagText(chunk, "AssistantName");
					string text8 = ExtractFirstTagText(chunk, "OfficeLocation");
					if (text8.Length == 0)
					{
						text8 = ExtractFirstTagText(chunk, "PhysicalDeliveryOfficeName");
					}
					string itemId = "";
					string changeKey = "";
					ExtractItemIdAndChangeKey(chunk, ref itemId, ref changeKey);
					list.Add(new EwsResolvedContact
					{
						DisplayName = text5,
						EmailAddress = text,
						Company = company,
						BusinessPhone = businessPhone,
						MobilePhone = mobilePhone,
						FirstName = text3,
						LastName = text4,
						MiddleName = middleName,
						JobTitle = text6,
						Department = department,
						WebPage = webPage,
						FaxNumber = faxNumber,
						PagerNumber = pagerNumber,
						OfficeLocation = text8,
						HomePhone = homePhone,
						OtherPhone = text7,
						AssistantName = assistantName,
						DirectoryContactItemId = itemId,
						DirectoryContactChangeKey = changeKey
					});
				}
			}
			return list;
		}
	}

	public static List<EwsResolvedContact> TryFindPeopleGal(string ewsUrl, string username, string password, string queryString, string oauthToken = null, int maxEntries = 100)
	{
		LastError = null;
		string text = ((queryString != null) ? queryString : "").Trim();
		if (text.Length == 0)
		{
			LastError = "FindPeople query is required.";
			return null;
		}
		string text2 = SecurityElement.Escape(text);
		int num = Math.Max(1, Math.Min(maxEntries, 1000));
		List<EwsResolvedContact> list = new List<EwsResolvedContact>();
		int num2 = 0;
		int num3 = 0;
		checked
		{
			do
			{
				string envelope = "<?xml version=\"1.0\" encoding=\"utf-8\"?><soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:t=\"http://schemas.microsoft.com/exchange/services/2006/types\" xmlns:m=\"http://schemas.microsoft.com/exchange/services/2006/messages\"><soap:Body><m:FindPeople><m:PersonaShape><t:BaseShape>Default</t:BaseShape></m:PersonaShape><m:IndexedPageItemView MaxEntriesReturned=\"" + num + "\" Offset=\"" + num2 + "\" BasePoint=\"Beginning\"/><m:ParentFolderId><t:DistinguishedFolderId Id=\"directory\"/></m:ParentFolderId><m:QueryString>" + text2 + "</m:QueryString></m:FindPeople></soap:Body></soap:Envelope>";
				string text3 = PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/FindPeople", envelope, oauthToken);
				if (string.IsNullOrEmpty(text3))
				{
					break;
				}
				List<EwsResolvedContact> list2 = ParseFindPeopleContacts(text3);
				if (list2 == null || list2.Count == 0)
				{
					break;
				}
				list.AddRange(list2);
				if (list2.Count < num)
				{
					break;
				}
				num2 += list2.Count;
				num3++;
			}
			while (num3 <= 49);
			if (list.Count == 0)
			{
				return new List<EwsResolvedContact>();
			}
			return list;
		}
	}

	public static List<EwsResolvedContact> TryHarvestGalDirectory(string ewsUrl, string username, string password, string oauthToken = null, bool deepHarvest = false)
	{
		LastError = null;
		Dictionary<string, EwsResolvedContact> dictionary = new Dictionary<string, EwsResolvedContact>(StringComparer.OrdinalIgnoreCase);
		string text = "abcdefghijklmnopqrstuvwxyz0123456789";
		string text2 = text;
		int i = 0;
		checked
		{
			for (int length = text2.Length; i < length; i++)
			{
				MergeFindPeopleBatch(dictionary, TryFindPeopleGal(ewsUrl, username, password, text2[i].ToString(), oauthToken, 250));
			}
			List<EwsResolvedContact> list = TryHarvestPersonalContactsFolder(ewsUrl, username, password, oauthToken);
			if (list != null)
			{
				MergeFindPeopleBatch(dictionary, list);
			}
			if (deepHarvest)
			{
				MergeFindPeopleBatch(dictionary, TryFindPeopleGal(ewsUrl, username, password, "@", oauthToken, 250));
				string text3 = "abcdefghijklmnopqrstuvwxyz";
				string text4 = text3;
				int j = 0;
				for (int length2 = text4.Length; j < length2; j++)
				{
					char c = text4[j];
					string text5 = text3;
					int k = 0;
					for (int length3 = text5.Length; k < length3; k++)
					{
						char c2 = text5[k];
						MergeFindPeopleBatch(dictionary, TryFindPeopleGal(ewsUrl, username, password, c.ToString() + c2, oauthToken, 200));
					}
				}
			}
			if (dictionary.Count == 0)
			{
				if (string.IsNullOrWhiteSpace(LastError))
				{
					LastError = "GAL harvest returned no contacts.";
				}
				return new List<EwsResolvedContact>();
			}
			return new List<EwsResolvedContact>(dictionary.Values);
		}
	}

	private static void MergeFindPeopleBatch(Dictionary<string, EwsResolvedContact> byEmail, List<EwsResolvedContact> batch)
	{
		if (byEmail == null || batch == null)
		{
			return;
		}
		foreach (EwsResolvedContact item in batch)
		{
			if (item != null && !string.IsNullOrWhiteSpace(item.EmailAddress))
			{
				byEmail[item.EmailAddress.Trim()] = item;
			}
		}
	}

	public static List<EwsResolvedContact> ParseFindPeopleContacts(string soapXml)
	{
		List<EwsResolvedContact> list = new List<EwsResolvedContact>();
		if (string.IsNullOrWhiteSpace(soapXml))
		{
			return list;
		}
		string[] array = soapXml.Split(new string[1] { "<t:Persona>" }, StringSplitOptions.None);
		checked
		{
			int num = array.Length - 1;
			for (int i = 1; i <= num; i++)
			{
				string chunk = array[i];
				string text = ExtractFirstTagText(chunk, "EmailAddress");
				if (text.Length == 0)
				{
					text = ExtractFirstTagText(chunk, "EmailAddress");
				}
				if (text.Length == 0)
				{
					string text2 = ExtractFirstTagText(chunk, "SmtpAddress");
					if (text2.Length > 0)
					{
						text = text2;
					}
				}
				if (text.Length != 0)
				{
					string text3 = ExtractFirstTagText(chunk, "GivenName");
					string text4 = ExtractFirstTagText(chunk, "Surname");
					string middleName = ExtractFirstTagText(chunk, "MiddleName");
					string text5 = ExtractFirstTagText(chunk, "DisplayName");
					if (text5.Length == 0)
					{
						text5 = (text3 + " " + text4).Trim();
					}
					string text6 = ExtractFirstTagText(chunk, "CompanyName");
					if (text6.Length == 0)
					{
						text6 = ExtractFirstTagText(chunk, "OfficeLocation");
					}
					string businessPhone = ExtractFirstTagText(chunk, "BusinessPhone");
					string mobilePhone = ExtractFirstTagText(chunk, "MobilePhone");
					string text7 = ExtractFirstTagText(chunk, "JobTitle");
					if (text7.Length == 0)
					{
						text7 = ExtractFirstTagText(chunk, "Title");
					}
					string department = ExtractFirstTagText(chunk, "Department");
					string webPage = ExtractFirstTagText(chunk, "BusinessHomePage");
					string faxNumber = ExtractFirstTagText(chunk, "BusinessFax");
					string pagerNumber = ExtractFirstTagText(chunk, "PagerNumber");
					string homePhone = ExtractFirstTagText(chunk, "HomePhone");
					string text8 = ExtractFirstTagText(chunk, "OtherTelephone");
					if (text8.Length == 0)
					{
						text8 = ExtractFirstTagText(chunk, "OtherPhone");
					}
					string assistantName = ExtractFirstTagText(chunk, "AssistantName");
					string text9 = ExtractFirstTagText(chunk, "OfficeLocation");
					if (text9.Length == 0)
					{
						text9 = ExtractFirstTagText(chunk, "PhysicalDeliveryOfficeName");
					}
					string galPersonaId = ExtractPersonaItemId(chunk);
					string galPersonaChangeKey = ExtractPersonaChangeKey(chunk);
					list.Add(new EwsResolvedContact
					{
						DisplayName = text5,
						EmailAddress = text,
						Company = text6,
						BusinessPhone = businessPhone,
						MobilePhone = mobilePhone,
						JobTitle = text7,
						Department = department,
						WebPage = webPage,
						FaxNumber = faxNumber,
						PagerNumber = pagerNumber,
						OfficeLocation = text9,
						HomePhone = homePhone,
						MiddleName = middleName,
						OtherPhone = text8,
						AssistantName = assistantName,
						FirstName = text3,
						LastName = text4,
						GalPersonaId = galPersonaId,
						GalPersonaChangeKey = galPersonaChangeKey
					});
				}
			}
			if (list.Count == 0)
			{
				List<EwsResolvedContact> list2 = ParseResolvedContacts(soapXml);
				if (list2 != null && list2.Count > 0)
				{
					return list2;
				}
			}
			return list;
		}
	}

	public static List<EwsResolvedContact> ParseResolvedContacts(string soapXml)
	{
		Dictionary<string, EwsResolvedContact> dictionary = new Dictionary<string, EwsResolvedContact>(StringComparer.OrdinalIgnoreCase);
		if (string.IsNullOrWhiteSpace(soapXml))
		{
			return new List<EwsResolvedContact>();
		}
		string[] array = soapXml.Split(new string[1] { "<t:Mailbox>" }, StringSplitOptions.None);
		checked
		{
			int num = array.Length - 1;
			for (int i = 1; i <= num; i++)
			{
				string chunk = array[i];
				string text = NormalizeSmtpAddress(ExtractFirstTagText(chunk, "EmailAddress"));
				if (text.Length != 0)
				{
					string displayName = ExtractFirstTagText(chunk, "Name");
					dictionary[text] = new EwsResolvedContact
					{
						DisplayName = displayName,
						EmailAddress = text
					};
				}
			}
			string[] array2 = soapXml.Split(new string[1] { "<t:Contact>" }, StringSplitOptions.None);
			int num2 = array2.Length - 1;
			for (int j = 1; j <= num2; j++)
			{
				string text2 = array2[j];
				string text3 = ExtractContactPrimaryEmail(text2);
				if (text3.Length == 0)
				{
					continue;
				}
				string text4 = ExtractFirstTagText(text2, "GivenName");
				string text5 = ExtractFirstTagText(text2, "Surname");
				string text6 = ExtractFirstTagText(text2, "DisplayName");
				if (text6.Length == 0)
				{
					text6 = (text4 + " " + text5).Trim();
				}
				string itemId = "";
				string changeKey = "";
				ExtractItemIdAndChangeKey(text2, ref itemId, ref changeKey);
				EwsResolvedContact ewsResolvedContact = null;
				if (dictionary.ContainsKey(text3))
				{
					ewsResolvedContact = dictionary[text3];
					if (text6.Length > 0)
					{
						ewsResolvedContact.DisplayName = text6;
					}
					if (itemId.Length > 0)
					{
						ewsResolvedContact.DirectoryContactItemId = itemId;
					}
					if (changeKey.Length > 0)
					{
						ewsResolvedContact.DirectoryContactChangeKey = changeKey;
					}
				}
				else
				{
					dictionary[text3] = new EwsResolvedContact
					{
						DisplayName = text6,
						EmailAddress = text3,
						FirstName = text4,
						LastName = text5,
						DirectoryContactItemId = itemId,
						DirectoryContactChangeKey = changeKey
					};
				}
			}
			return new List<EwsResolvedContact>(dictionary.Values);
		}
	}

	private static string ExtractContactPrimaryEmail(string contactChunk)
	{
		string text = NormalizeSmtpAddress(ExtractFirstTagText(contactChunk, "EmailAddress"));
		if (text.Length > 0)
		{
			return text;
		}
		string text2 = NormalizeSmtpAddress(ExtractFirstTagText(contactChunk, "SmtpAddress"));
		if (text2.Length > 0)
		{
			return text2;
		}
		string text3 = ((contactChunk != null) ? contactChunk : "");
		string value = "<t:Entry";
		int num = 0;
		checked
		{
			while (num < text3.Length)
			{
				num = text3.IndexOf(value, num, StringComparison.OrdinalIgnoreCase);
				if (num < 0)
				{
					break;
				}
				int num2 = text3.IndexOf("</t:Entry>", num, StringComparison.OrdinalIgnoreCase);
				if (num2 < 0)
				{
					break;
				}
				string text4 = text3.Substring(num, num2 - num);
				if (text4.IndexOf("EmailAddress", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					string text5 = ExtractFirstTagText(text4, "Entry");
					if (text5.Length == 0)
					{
						int num3 = text4.LastIndexOf('>');
						if (num3 >= 0 && num3 + 1 < text4.Length)
						{
							text5 = text4.Substring(num3 + 1).Trim();
						}
					}
					text5 = NormalizeSmtpAddress(text5);
					if (text5.Length > 0)
					{
						return text5;
					}
				}
				num = num2 + 1;
			}
			return "";
		}
	}

	private static string NormalizeSmtpAddress(string raw)
	{
		string text = ((raw != null) ? raw : "").Trim();
		if (text.Length == 0)
		{
			return "";
		}
		if (text.StartsWith("SMTP:", StringComparison.OrdinalIgnoreCase))
		{
			text = text.Substring(5).Trim();
		}
		if (text.StartsWith("smtp:", StringComparison.OrdinalIgnoreCase))
		{
			text = text.Substring(5).Trim();
		}
		return text;
	}

	private static void ExtractItemIdAndChangeKey(string chunk, ref string itemId, ref string changeKey)
	{
		itemId = "";
		changeKey = "";
		string text = "ItemId Id=\"";
		int num = chunk.IndexOf(text, StringComparison.OrdinalIgnoreCase);
		if (num < 0)
		{
			return;
		}
		checked
		{
			num += text.Length;
			int num2 = chunk.IndexOf('"', num);
			if (num2 <= num)
			{
				return;
			}
			itemId = chunk.Substring(num, num2 - num);
			string text2 = "ChangeKey=\"";
			int num3 = chunk.IndexOf(text2, num2, StringComparison.OrdinalIgnoreCase);
			if (num3 >= 0)
			{
				num3 += text2.Length;
				int num4 = chunk.IndexOf('"', num3);
				if (num4 > num3)
				{
					changeKey = chunk.Substring(num3, num4 - num3);
				}
			}
		}
	}

	private static string ExtractFirstItemId(string chunk)
	{
		string text = "ItemId Id=\"";
		int num = chunk.IndexOf(text, StringComparison.OrdinalIgnoreCase);
		if (num < 0)
		{
			return "";
		}
		checked
		{
			num += text.Length;
			int num2 = chunk.IndexOf('"', num);
			if (num2 <= num)
			{
				return "";
			}
			return chunk.Substring(num, num2 - num);
		}
	}

	private static string ExtractFirstTagText(string chunk, string localName)
	{
		string text = "<t:" + localName + ">";
		int num = chunk.IndexOf(text, StringComparison.OrdinalIgnoreCase);
		if (num < 0)
		{
			return "";
		}
		checked
		{
			num += text.Length;
			string value = "</t:" + localName + ">";
			int num2 = chunk.IndexOf(value, num, StringComparison.OrdinalIgnoreCase);
			if (num2 < 0)
			{
				return "";
			}
			return chunk.Substring(num, num2 - num).Trim();
		}
	}

	public static List<string> ParseItemIdsFromFindResponse(string soapXml)
	{
		List<string> list = new List<string>();
		if (string.IsNullOrWhiteSpace(soapXml))
		{
			return list;
		}
		string text = "ItemId Id=\"";
		int startIndex = 0;
		checked
		{
			while (true)
			{
				int num = soapXml.IndexOf(text, startIndex, StringComparison.OrdinalIgnoreCase);
				if (num < 0)
				{
					break;
				}
				num += text.Length;
				int num2 = soapXml.IndexOf('"', num);
				if (num2 < 0)
				{
					break;
				}
				list.Add(soapXml.Substring(num, num2 - num));
				startIndex = num2 + 1;
			}
			return list;
		}
	}

	private static string PostSoap(string ewsUrl, string username, string password, string soapAction, string envelope, string oauthToken = null)
	{
		LastError = null;
		string text = ((ewsUrl != null) ? ewsUrl : "").Trim();
		string result;
		if (text.Length == 0)
		{
			LastError = "EWS URL is required.";
			result = "";
		}
		else
		{
			string text2 = ((oauthToken != null) ? oauthToken : "").Trim();
			string text3 = ((username != null) ? username : "").Trim();
			string password2 = ((password != null) ? password : "");
			if (text2.Length == 0 && text3.Length == 0)
			{
				LastError = "EWS username/password or OAuth token required.";
				result = "";
			}
			else
			{
				try
				{
					HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(text);
					httpWebRequest.Method = "POST";
					httpWebRequest.ContentType = "text/xml; charset=utf-8";
					httpWebRequest.Headers.Add("SOAPAction", soapAction);
					if (text2.Length > 0)
					{
						httpWebRequest.Headers.Add("Authorization", "Bearer " + text2);
						if (text3.Length > 0)
						{
							httpWebRequest.Headers.Add("X-AnchorMailbox", text3);
						}
					}
					else
					{
						httpWebRequest.Credentials = new NetworkCredential(text3, password2);
					}
					httpWebRequest.Timeout = 45000;
					byte[] bytes = Encoding.UTF8.GetBytes(envelope);
					httpWebRequest.ContentLength = bytes.Length;
					using (Stream stream = httpWebRequest.GetRequestStream())
					{
						stream.Write(bytes, 0, bytes.Length);
					}
					using HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
					using StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8);
					result = streamReader.ReadToEnd();
				}
				catch (WebException ex)
				{
					ProjectData.SetProjectError(ex);
					WebException ex2 = ex;
					LastError = ex2.Message;
					try
					{
						if (ex2.Response != null)
						{
							using StreamReader streamReader2 = new StreamReader(ex2.Response.GetResponseStream(), Encoding.UTF8);
							LastError = streamReader2.ReadToEnd();
						}
					}
					catch (Exception projectError)
					{
						ProjectData.SetProjectError(projectError);
						ProjectData.ClearProjectError();
					}
					result = "";
					ProjectData.ClearProjectError();
				}
				catch (Exception ex3)
				{
					ProjectData.SetProjectError(ex3);
					Exception ex4 = ex3;
					LastError = ex4.Message;
					result = "";
					ProjectData.ClearProjectError();
				}
			}
		}
		return result;
	}

	private static bool IsDistinguishedFolderId(string folderId)
	{
		return DistinguishedFolderIds.Contains(((folderId != null) ? folderId : "").Trim());
	}

	private static string BuildParentFolderIdXml(string folderId)
	{
		string text = ((folderId != null) ? folderId : "").Trim();
		if (text.Length == 0)
		{
			text = "inbox";
		}
		if (IsDistinguishedFolderId(text))
		{
			return "<t:DistinguishedFolderId Id=\"" + SecurityElement.Escape(text.ToLowerInvariant()) + "\"/>";
		}
		return "<t:FolderId Id=\"" + SecurityElement.Escape(text) + "\"/>";
	}

	private static string BuildDestinationFolderIdXml(string folderId)
	{
		return BuildParentFolderIdXml(folderId);
	}

	private static string ExtractXmlElementText(string xml, string localName)
	{
		string result;
		if (string.IsNullOrWhiteSpace(xml))
		{
			result = "";
		}
		else
		{
			try
			{
				XmlDocument xmlDocument = new XmlDocument();
				xmlDocument.LoadXml(xml);
				XmlNodeList elementsByTagName = xmlDocument.GetElementsByTagName(localName);
				if (elementsByTagName == null || elementsByTagName.Count == 0)
				{
					elementsByTagName = xmlDocument.GetElementsByTagName("t:" + localName);
				}
				if (elementsByTagName == null || elementsByTagName.Count == 0)
				{
					result = "";
				}
				else
				{
					string innerText = elementsByTagName[0].InnerText;
					result = ((innerText != null) ? innerText : "").Trim();
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = "";
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}
}
