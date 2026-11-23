using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

public sealed class EwsAttachmentStore
{
	private class AttachmentEntry
	{
		public int PartIndex;

		public string AttachmentId;

		public string FileName;

		[DebuggerNonUserCode]
		public AttachmentEntry()
		{
		}
	}

	private const string CacheSubdir = "ews-attachments";

	private EwsAttachmentStore()
	{
	}

	public static void RememberAttachments(string accountId, string folder, ulong uid, IList<EwsMailService.EwsAttachmentInfo> attachments)
	{
		if (attachments == null || attachments.Count == 0)
		{
			return;
		}
		List<AttachmentEntry> list = new List<AttachmentEntry>();
		int num = 0;
		foreach (EwsMailService.EwsAttachmentInfo attachment in attachments)
		{
			if (attachment != null && !string.IsNullOrWhiteSpace(attachment.AttachmentId))
			{
				AttachmentEntry attachmentEntry = new AttachmentEntry
				{
					PartIndex = num,
					AttachmentId = attachment.AttachmentId
				};
				EwsMailService.EwsAttachmentInfo ewsAttachmentInfo = attachment;
				attachmentEntry.FileName = ((ewsAttachmentInfo.FileName != null) ? ewsAttachmentInfo.FileName : "");
				list.Add(attachmentEntry);
				num = checked(num + 1);
			}
		}
		if (list.Count != 0)
		{
			SaveList(accountId, folder, uid, list);
		}
	}

	public static bool TryGetAttachmentId(string accountId, string folder, ulong uid, int partIndex, ref string attachmentId, ref string fileName)
	{
		attachmentId = null;
		fileName = "";
		List<AttachmentEntry> list = LoadList(accountId, folder, uid);
		foreach (AttachmentEntry item in list)
		{
			if (item == null || item.PartIndex != partIndex)
			{
				continue;
			}
			attachmentId = item.AttachmentId;
			AttachmentEntry attachmentEntry = item;
			fileName = ((attachmentEntry.FileName != null) ? attachmentEntry.FileName : "");
			return (attachmentId != null && attachmentId.Length > 0) ? true : false;
		}
		return false;
	}

	private static void SaveList(string accountId, string folder, ulong uid, List<AttachmentEntry> list)
	{
		string path = Path.Combine(AccountStore.DataRoot(), "ews-attachments");
		if (!Directory.Exists(path))
		{
			Directory.CreateDirectory(path);
		}
		JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
		File.WriteAllText(CachePath(accountId, folder, uid), javaScriptSerializer.Serialize(list), Encoding.UTF8);
	}

	private static List<AttachmentEntry> LoadList(string accountId, string folder, ulong uid)
	{
		string path = CachePath(accountId, folder, uid);
		List<AttachmentEntry> result;
		if (!File.Exists(path))
		{
			result = new List<AttachmentEntry>();
		}
		else
		{
			try
			{
				JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
				List<AttachmentEntry> list = javaScriptSerializer.Deserialize<List<AttachmentEntry>>(File.ReadAllText(path, Encoding.UTF8));
				result = ((list != null) ? list : new List<AttachmentEntry>());
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = new List<AttachmentEntry>();
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}

	private static string CachePath(string accountId, string folder, ulong uid)
	{
		string text = SanitizeToken(accountId);
		string text2 = SanitizeToken(folder);
		return Path.Combine(AccountStore.DataRoot(), "ews-attachments", text + "__" + text2 + "__" + uid + ".json");
	}

	private static string SanitizeToken(string raw)
	{
		string text = ((raw != null) ? raw : "").Trim();
		if (text.Length == 0)
		{
			return "default";
		}
		char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
		StringBuilder stringBuilder = new StringBuilder(text.Length);
		string text2 = text;
		int i = 0;
		for (int length = text2.Length; i < length; i = checked(i + 1))
		{
			char value = text2[i];
			if (Array.IndexOf(invalidFileNameChars, value) >= 0)
			{
				stringBuilder.Append('_');
			}
			else
			{
				stringBuilder.Append(value);
			}
		}
		return stringBuilder.ToString();
	}
}
