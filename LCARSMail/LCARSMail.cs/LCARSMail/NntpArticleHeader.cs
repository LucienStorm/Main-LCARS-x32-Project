using System.Diagnostics;

namespace LCARSMail;

public class NntpArticleHeader
{
	public int ArticleNumber;

	public string Subject;

	public string FromLine;

	[DebuggerNonUserCode]
	public NntpArticleHeader()
	{
	}
}
