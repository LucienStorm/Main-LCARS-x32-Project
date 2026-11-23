using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Web.Script.Serialization;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

public sealed class DraftStore
{
	private class DraftDto
	{
		public string Id
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string AccountId
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

		public string Subject
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string BodyPlain
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string BodyHtml
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public List<string> AttachmentPaths
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public DateTime UpdatedUtc
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string ImapDraftFolder
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public ulong ImapDraftUid
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		[DebuggerNonUserCode]
		public DraftDto()
		{
		}

		public static DraftDto FromModel(MailDraft d)
		{
			DraftDto draftDto = new DraftDto();
			draftDto.Id = d.Id;
			draftDto.AccountId = ((d.AccountId != null) ? d.AccountId : "");
			draftDto.ToAddresses = ((d.ToAddresses != null) ? d.ToAddresses : "");
			draftDto.CcAddresses = ((d.CcAddresses != null) ? d.CcAddresses : "");
			draftDto.Subject = ((d.Subject != null) ? d.Subject : "");
			draftDto.BodyPlain = ((d.BodyPlain != null) ? d.BodyPlain : "");
			draftDto.BodyHtml = ((d.BodyHtml != null) ? d.BodyHtml : "");
			draftDto.AttachmentPaths = ((d.AttachmentPaths != null) ? d.AttachmentPaths : new List<string>());
			draftDto.UpdatedUtc = d.UpdatedUtc;
			draftDto.ImapDraftFolder = ((d.ImapDraftFolder != null) ? d.ImapDraftFolder : "");
			draftDto.ImapDraftUid = d.ImapDraftUid;
			return draftDto;
		}

		public MailDraft ToModel()
		{
			MailDraft mailDraft = new MailDraft();
			mailDraft.Id = Id;
			mailDraft.AccountId = AccountId;
			mailDraft.ToAddresses = ToAddresses;
			mailDraft.CcAddresses = CcAddresses;
			mailDraft.Subject = Subject;
			mailDraft.BodyPlain = BodyPlain;
			mailDraft.BodyHtml = BodyHtml;
			List<string> attachmentPaths = AttachmentPaths;
			mailDraft.AttachmentPaths = ((attachmentPaths != null) ? attachmentPaths : new List<string>());
			mailDraft.UpdatedUtc = UpdatedUtc;
			string imapDraftFolder = ImapDraftFolder;
			mailDraft.ImapDraftFolder = ((imapDraftFolder != null) ? imapDraftFolder : "");
			mailDraft.ImapDraftUid = ImapDraftUid;
			return mailDraft;
		}
	}

	private const string DraftsSubdir = "drafts";

	private DraftStore()
	{
	}

	public static string DraftsDirectory()
	{
		string text = Path.Combine(AccountStore.DataRoot(), "drafts");
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		return text;
	}

	public static string SaveDraft(MailDraft draft)
	{
		if (draft == null)
		{
			return "";
		}
		if (string.IsNullOrWhiteSpace(draft.Id))
		{
			draft.Id = Guid.NewGuid().ToString("N");
		}
		draft.UpdatedUtc = DateTime.UtcNow;
		string path = Path.Combine(DraftsDirectory(), draft.Id + ".json");
		JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
		File.WriteAllText(path, javaScriptSerializer.Serialize(DraftDto.FromModel(draft)), Encoding.UTF8);
		return draft.Id;
	}

	public static MailDraft LoadDraft(string draftId)
	{
		MailDraft result;
		if (string.IsNullOrWhiteSpace(draftId))
		{
			result = null;
		}
		else
		{
			string path = Path.Combine(DraftsDirectory(), draftId.Trim() + ".json");
			if (!File.Exists(path))
			{
				result = null;
			}
			else
			{
				try
				{
					JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
					result = javaScriptSerializer.Deserialize<DraftDto>(File.ReadAllText(path, Encoding.UTF8))?.ToModel();
				}
				catch (Exception projectError)
				{
					ProjectData.SetProjectError(projectError);
					result = null;
					ProjectData.ClearProjectError();
				}
			}
		}
		return result;
	}

	public static List<MailDraft> ListDraftsForAccount(string accountId)
	{
		List<MailDraft> list = new List<MailDraft>();
		if (string.IsNullOrWhiteSpace(accountId))
		{
			return list;
		}
		string[] files = Directory.GetFiles(DraftsDirectory(), "*.json");
		foreach (string path in files)
		{
			try
			{
				JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
				DraftDto draftDto = javaScriptSerializer.Deserialize<DraftDto>(File.ReadAllText(path, Encoding.UTF8));
				if (draftDto != null && string.Equals(draftDto.AccountId, accountId, StringComparison.OrdinalIgnoreCase))
				{
					list.Add(draftDto.ToModel());
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}
		list.Sort([SpecialName] (MailDraft a, MailDraft b) => DateTime.Compare(b.UpdatedUtc, a.UpdatedUtc));
		return list;
	}

	public static void DeleteDraft(string draftId)
	{
		if (!string.IsNullOrWhiteSpace(draftId))
		{
			string path = Path.Combine(DraftsDirectory(), draftId.Trim() + ".json");
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
	}
}
