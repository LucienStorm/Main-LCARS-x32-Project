using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

public sealed class ThunderbirdMsgFilterParser
{
	private static readonly Regex KeyValuePattern = new Regex("^\\s*([A-Za-z_]+)\\s*=\\s*\"((?:\\\\.|[^\"\\\\])*)\"\\s*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

	public static string LastError;

	private ThunderbirdMsgFilterParser()
	{
	}

	public static List<MailFilterRule> ParseFile(string path)
	{
		LastError = null;
		List<MailFilterRule> result;
		if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
		{
			LastError = "msgFilterRules.dat not found.";
			result = new List<MailFilterRule>();
		}
		else
		{
			try
			{
				string[] lines = File.ReadAllLines(path, Encoding.UTF8);
				result = ParseLines(lines);
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Exception ex2 = ex;
				LastError = ex2.Message;
				result = new List<MailFilterRule>();
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}

	public static List<MailFilterRule> ParseLines(string[] lines)
	{
		List<MailFilterRule> list = new List<MailFilterRule>();
		if (lines == null)
		{
			return list;
		}
		MailFilterRule mailFilterRule = null;
		foreach (string text in lines)
		{
			string text2 = ((text != null) ? text : "").Trim();
			if (text2.Length == 0)
			{
				continue;
			}
			Match match = KeyValuePattern.Match(text2);
			if (!match.Success)
			{
				continue;
			}
			string value = match.Groups[1].Value;
			string text3 = UnescapeMozillaString(match.Groups[2].Value);
			switch (value)
			{
			case "name":
			{
				if (mailFilterRule != null)
				{
					list.Add(mailFilterRule);
				}
				MailFilterRule mailFilterRule2 = new MailFilterRule();
				mailFilterRule2.Name = text3;
				mailFilterRule = mailFilterRule2;
				break;
			}
			case "enabled":
				if (mailFilterRule != null)
				{
					mailFilterRule.Enabled = IsTruthy(text3);
				}
				break;
			case "type":
				if (mailFilterRule != null)
				{
					int result = 0;
					int.TryParse(text3, out result);
					mailFilterRule.RunOnIncoming = (((result & 1) != 0 || result == 0) ? true : false);
				}
				break;
			case "condition":
				if (mailFilterRule != null)
				{
					ParseConditionExpression(text3, mailFilterRule);
				}
				break;
			case "action":
				mailFilterRule?.Actions.Add(new MailFilterAction
				{
					ActionName = text3
				});
				break;
			case "actionValue":
				if (mailFilterRule != null && mailFilterRule.Actions.Count > 0)
				{
					mailFilterRule.Actions[checked(mailFilterRule.Actions.Count - 1)].ActionValue = text3;
				}
				break;
			}
		}
		if (mailFilterRule != null)
		{
			list.Add(mailFilterRule);
		}
		return list;
	}

	private static void ParseConditionExpression(string expr, MailFilterRule rule)
	{
		if (rule == null)
		{
			return;
		}
		rule.Conditions.Clear();
		if (string.IsNullOrWhiteSpace(expr))
		{
			return;
		}
		string text = expr.Trim();
		if (text.StartsWith("AND ", StringComparison.OrdinalIgnoreCase))
		{
			rule.ConditionLogic = "AND";
			text = text.Substring(4).Trim();
		}
		else if (text.StartsWith("OR ", StringComparison.OrdinalIgnoreCase))
		{
			rule.ConditionLogic = "OR";
			text = text.Substring(3).Trim();
		}
		List<string> list = SplitTopLevelConditions(text);
		foreach (string item in list)
		{
			MailFilterCondition mailFilterCondition = ParseSingleCondition(item);
			if (mailFilterCondition != null)
			{
				rule.Conditions.Add(mailFilterCondition);
			}
		}
	}

	private static MailFilterCondition ParseSingleCondition(string part)
	{
		string text = ((part != null) ? part : "").Trim();
		checked
		{
			if (text.StartsWith("(") && text.EndsWith(")"))
			{
				text = text.Substring(1, text.Length - 2).Trim();
			}
			if (text.Length == 0)
			{
				return null;
			}
			string[] array = SplitConditionTokens(text);
			if (array.Length < 3)
			{
				return null;
			}
			MailFilterCondition mailFilterCondition = new MailFilterCondition();
			mailFilterCondition.Field = array[0];
			mailFilterCondition.Operator = array[1];
			mailFilterCondition.Value = string.Join(",", array, 2, array.Length - 2);
			return mailFilterCondition;
		}
	}

	private static string[] SplitConditionTokens(string p)
	{
		List<string> list = new List<string>();
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = false;
		checked
		{
			int num = p.Length - 1;
			for (int i = 0; i <= num; i++)
			{
				char c = p[i];
				switch (c)
				{
				case '"':
					flag = !flag;
					continue;
				case ',':
					if (!flag)
					{
						list.Add(stringBuilder.ToString().Trim());
						stringBuilder.Clear();
						continue;
					}
					break;
				}
				stringBuilder.Append(c);
			}
			if (stringBuilder.Length > 0)
			{
				list.Add(stringBuilder.ToString().Trim());
			}
			return list.ToArray();
		}
	}

	private static List<string> SplitTopLevelConditions(string expr)
	{
		List<string> list = new List<string>();
		int num = 0;
		int num2 = 0;
		checked
		{
			int num3 = expr.Length - 1;
			for (int i = 0; i <= num3; i++)
			{
				char c = expr[i];
				if (c == '(')
				{
					num++;
				}
				if (c == ')')
				{
					num--;
				}
				if (c == ',' && num == 0)
				{
					list.Add(expr.Substring(num2, i - num2).Trim());
					num2 = i + 1;
				}
			}
			if (num2 < expr.Length)
			{
				list.Add(expr.Substring(num2).Trim());
			}
			return list;
		}
	}

	private static string UnescapeMozillaString(string s)
	{
		if (s == null)
		{
			return "";
		}
		return s.Replace("\\\"", "\"").Replace("\\\\", "\\").Replace("\\n", "\n")
			.Replace("\\r", "");
	}

	private static bool IsTruthy(string value)
	{
		string text = ((value != null) ? value : "").Trim();
		return (string.Equals(text, "yes", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "true", StringComparison.OrdinalIgnoreCase) || Operators.CompareString(text, "1", TextCompare: false) == 0) ? true : false;
	}
}
