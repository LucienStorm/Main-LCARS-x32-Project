using System;

namespace LCARSMail;

public class MailMessageHeader
{
	public ulong Uid;

	public string Folder;

	public string AccountId;

	public string Subject;

	public string From;

	public string ToAddresses;

	public string CcAddresses;

	public DateTime DateUtc;

	public bool IsSeen;

	public string MessageId;

	public string InReplyTo;

	public string References;

	public string ReplyTo;

	public string ListId;

	public string ReturnPath;

	public long SizeBytes;

	public bool IsJunk;

	public int HeaderAttachmentCount;

	public double FtsRank;

	public MailMessageHeader()
	{
		HeaderAttachmentCount = -1;
	}
}
