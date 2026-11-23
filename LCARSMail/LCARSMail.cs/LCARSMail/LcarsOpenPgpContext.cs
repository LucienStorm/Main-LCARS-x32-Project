using MimeKit.Cryptography;
using Org.BouncyCastle.Bcpg.OpenPgp;

namespace LCARSMail;

public sealed class LcarsOpenPgpContext : GnuPGContext
{
	public LcarsOpenPgpContext()
		: base(MailPgpService.GnupgHomeDirectory())
	{
	}

	protected override string GetPasswordForKey(PgpSecretKey key)
	{
		return string.Empty;
	}
}
