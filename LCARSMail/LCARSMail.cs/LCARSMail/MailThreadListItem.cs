using System.Collections.Generic;
using System.Diagnostics;

namespace LCARSMail;

public class MailThreadListItem
{
	public string ThreadKey;

	public MailMessageHeader Representative;

	public List<MailMessageHeader> Messages;

	public int MessageCount => (Messages != null) ? Messages.Count : 0;

	[DebuggerNonUserCode]
	public MailThreadListItem()
	{
	}
}
