using System.Collections.Generic;

namespace LCARSMail;

public class MailFilterRule
{
	public string Name;

	public bool Enabled;

	public bool RunOnIncoming;

	public List<MailFilterCondition> Conditions;

	public string ConditionLogic;

	public List<MailFilterAction> Actions;

	public MailFilterRule()
	{
		Name = string.Empty;
		Enabled = true;
		RunOnIncoming = true;
		Conditions = new List<MailFilterCondition>();
		ConditionLogic = "AND";
		Actions = new List<MailFilterAction>();
	}
}
