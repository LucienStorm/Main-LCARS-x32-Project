using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

public sealed class MailFilterThunderbirdExporter
{
	private MailFilterThunderbirdExporter()
	{
	}

	public static bool ExportToFile(List<MailFilterRule> rules, string path)
	{
		bool result;
		if (rules == null || string.IsNullOrWhiteSpace(path))
		{
			result = false;
		}
		else
		{
			try
			{
				List<string> list = new List<string>();
				list.Add("version=\"9\"");
				list.Add("logging=\"no\"");
				foreach (MailFilterRule rule in rules)
				{
					if (rule != null)
					{
						AppendRuleLines(rule, list);
					}
				}
				File.WriteAllLines(path, list.ToArray(), Encoding.UTF8);
				result = true;
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = false;
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}

	private static void AppendRuleLines(MailFilterRule rule, List<string> lines)
	{
		lines.Add("name=\"" + Escape(rule.Name) + "\"");
		lines.Add("enabled=\"" + (rule.Enabled ? "yes" : "no") + "\"");
		lines.Add("type=\"" + (rule.RunOnIncoming ? "1" : "0") + "\"");
		lines.Add("condition=\"" + Escape(BuildConditionExpression(rule)) + "\"");
		foreach (MailFilterAction action in rule.Actions)
		{
			if (action != null)
			{
				lines.Add("action=\"" + Escape(action.ActionName) + "\"");
				if (!string.IsNullOrEmpty(action.ActionValue))
				{
					lines.Add("actionValue=\"" + Escape(action.ActionValue) + "\"");
				}
			}
		}
	}

	private static string BuildConditionExpression(MailFilterRule rule)
	{
		string text = ((rule.ConditionLogic != null) ? rule.ConditionLogic : "AND").Trim().ToUpperInvariant();
		if (Operators.CompareString(text, "OR", TextCompare: false) != 0)
		{
			text = "AND";
		}
		List<string> list = new List<string>();
		foreach (MailFilterCondition condition in rule.Conditions)
		{
			if (condition != null)
			{
				string[] array = new string[7] { "(", null, null, null, null, null, null };
				MailFilterCondition mailFilterCondition = condition;
				array[1] = ((mailFilterCondition.Field != null) ? mailFilterCondition.Field : "");
				array[2] = ",";
				MailFilterCondition mailFilterCondition2 = condition;
				array[3] = ((mailFilterCondition2.Operator != null) ? mailFilterCondition2.Operator : "");
				array[4] = ",";
				MailFilterCondition mailFilterCondition3 = condition;
				array[5] = ((mailFilterCondition3.Value != null) ? mailFilterCondition3.Value : "");
				array[6] = ")";
				list.Add(string.Concat(array));
			}
		}
		if (list.Count == 0)
		{
			return text + " (subject,contains,)";
		}
		return text + " " + string.Join(" ", list.ToArray());
	}

	private static string Escape(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return "";
		}
		return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
	}
}
