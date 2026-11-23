using System.Collections.Generic;

namespace LCARSMail;

public class MailFilterRunSummary
{
	public int RulesEvaluated;

	public int MessagesMatched;

	public int ActionsApplied;

	public List<string> Warnings;

	public MailFilterRunSummary()
	{
		Warnings = new List<string>();
	}
}
