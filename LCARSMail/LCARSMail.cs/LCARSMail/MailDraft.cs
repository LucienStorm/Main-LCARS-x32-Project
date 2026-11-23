using System;
using System.Collections.Generic;

namespace LCARSMail;

public class MailDraft
{
	public string Id;

	public string AccountId;

	public string ToAddresses;

	public string CcAddresses;

	public string Subject;

	public string BodyPlain;

	public string BodyHtml;

	public List<string> AttachmentPaths;

	public DateTime UpdatedUtc;

	public string ImapDraftFolder;

	public ulong ImapDraftUid;

	public MailDraft()
	{
		AttachmentPaths = new List<string>();
	}
}
