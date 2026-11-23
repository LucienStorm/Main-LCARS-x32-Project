namespace LCARSMail;

public class MailFilterCondition
{
	public string Field;

	public string Operator;

	public string Value;

	public MailFilterCondition()
	{
		Field = string.Empty;
		Operator = string.Empty;
		Value = string.Empty;
	}
}
