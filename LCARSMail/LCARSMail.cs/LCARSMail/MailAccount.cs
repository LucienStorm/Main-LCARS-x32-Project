using System.Diagnostics;

namespace LCARSMail;

public class MailAccount
{
	public string Protocol;

	public string EwsUrl;

	public string Id;

	public string DisplayName;

	public string EmailAddress;

	public string ImapHost;

	public int ImapPort;

	public bool ImapSsl;

	public bool ImapStartTls;

	public string Username;

	public string SmtpHost;

	public int SmtpPort;

	public bool SmtpSsl;

	public bool SmtpStartTls;

	public string SmtpUsername;

	public bool StrictTls;

	public string AuthMethod;

	[DebuggerNonUserCode]
	public MailAccount()
	{
	}
}
