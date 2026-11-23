using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

[StandardModule]
public sealed class MailFilterStore
{
	private class RuleDto
	{
		public string Name
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public bool Enabled
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public bool RunOnIncoming
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string ConditionLogic
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public List<ConditionDto> Conditions
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public List<ActionDto> Actions
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		[DebuggerNonUserCode]
		public RuleDto()
		{
		}

		public static RuleDto FromModel(MailFilterRule rule)
		{
			RuleDto ruleDto = new RuleDto();
			ruleDto.Name = ((rule.Name != null) ? rule.Name : "");
			ruleDto.Enabled = rule.Enabled;
			ruleDto.RunOnIncoming = rule.RunOnIncoming;
			ruleDto.ConditionLogic = ((rule.ConditionLogic != null) ? rule.ConditionLogic : "AND");
			ruleDto.Conditions = new List<ConditionDto>();
			ruleDto.Actions = new List<ActionDto>();
			RuleDto ruleDto2 = ruleDto;
			foreach (MailFilterCondition condition in rule.Conditions)
			{
				List<ConditionDto> conditions = ruleDto2.Conditions;
				ConditionDto conditionDto = new ConditionDto();
				MailFilterCondition mailFilterCondition = condition;
				conditionDto.Field = ((mailFilterCondition.Field != null) ? mailFilterCondition.Field : "");
				MailFilterCondition mailFilterCondition2 = condition;
				conditionDto.Op = ((mailFilterCondition2.Operator != null) ? mailFilterCondition2.Operator : "");
				MailFilterCondition mailFilterCondition3 = condition;
				conditionDto.Value = ((mailFilterCondition3.Value != null) ? mailFilterCondition3.Value : "");
				conditions.Add(conditionDto);
			}
			foreach (MailFilterAction action in rule.Actions)
			{
				List<ActionDto> actions = ruleDto2.Actions;
				ActionDto actionDto = new ActionDto();
				MailFilterAction mailFilterAction = action;
				actionDto.ActionName = ((mailFilterAction.ActionName != null) ? mailFilterAction.ActionName : "");
				MailFilterAction mailFilterAction2 = action;
				actionDto.ActionValue = ((mailFilterAction2.ActionValue != null) ? mailFilterAction2.ActionValue : "");
				actions.Add(actionDto);
			}
			return ruleDto2;
		}

		public MailFilterRule ToModel()
		{
			MailFilterRule mailFilterRule = new MailFilterRule();
			string name = Name;
			mailFilterRule.Name = ((name != null) ? name : "");
			mailFilterRule.Enabled = Enabled;
			mailFilterRule.RunOnIncoming = RunOnIncoming;
			string conditionLogic = ConditionLogic;
			mailFilterRule.ConditionLogic = ((conditionLogic != null) ? conditionLogic : "AND");
			MailFilterRule mailFilterRule2 = mailFilterRule;
			if (Conditions != null)
			{
				foreach (ConditionDto condition in Conditions)
				{
					List<MailFilterCondition> conditions = mailFilterRule2.Conditions;
					MailFilterCondition mailFilterCondition = new MailFilterCondition();
					conditionLogic = condition.Field;
					mailFilterCondition.Field = ((conditionLogic != null) ? conditionLogic : "");
					name = condition.Op;
					mailFilterCondition.Operator = ((name != null) ? name : "");
					string value = condition.Value;
					mailFilterCondition.Value = ((value != null) ? value : "");
					conditions.Add(mailFilterCondition);
				}
			}
			if (Actions != null)
			{
				foreach (ActionDto action in Actions)
				{
					List<MailFilterAction> actions = mailFilterRule2.Actions;
					MailFilterAction mailFilterAction = new MailFilterAction();
					string value = action.ActionName;
					mailFilterAction.ActionName = ((value != null) ? value : "");
					conditionLogic = action.ActionValue;
					mailFilterAction.ActionValue = ((conditionLogic != null) ? conditionLogic : "");
					actions.Add(mailFilterAction);
				}
			}
			return mailFilterRule2;
		}
	}

	private class ConditionDto
	{
		public string Field
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string Op
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string Value
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		[DebuggerNonUserCode]
		public ConditionDto()
		{
		}
	}

	private class ActionDto
	{
		public string ActionName
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string ActionValue
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		[DebuggerNonUserCode]
		public ActionDto()
		{
		}
	}

	private const string FiltersFileName = "filters.json";

	private const string FilterRulesFileName = "msgFilterRules.dat";

	public static string LastError;

	public static bool LastImportSkipped;

	public static string FiltersJsonPath()
	{
		return Path.Combine(AccountStore.DataRoot(), "filters.json");
	}

	public static List<MailFilterRule> LoadRules()
	{
		string path = FiltersJsonPath();
		List<MailFilterRule> result;
		if (!File.Exists(path))
		{
			result = new List<MailFilterRule>();
		}
		else
		{
			try
			{
				string input = File.ReadAllText(path, Encoding.UTF8);
				JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
				List<RuleDto> list = javaScriptSerializer.Deserialize<List<RuleDto>>(input);
				if (list == null)
				{
					result = new List<MailFilterRule>();
				}
				else
				{
					List<MailFilterRule> list2 = new List<MailFilterRule>();
					foreach (RuleDto item in list)
					{
						MailFilterRule mailFilterRule = item.ToModel();
						if (mailFilterRule != null)
						{
							list2.Add(mailFilterRule);
						}
					}
					result = list2;
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = new List<MailFilterRule>();
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}

	public static bool SaveRules(List<MailFilterRule> rules)
	{
		LastError = null;
		bool result;
		if (rules == null)
		{
			LastError = "rules is required.";
			result = false;
		}
		else
		{
			try
			{
				List<RuleDto> list = new List<RuleDto>();
				foreach (MailFilterRule rule in rules)
				{
					if (rule != null)
					{
						list.Add(RuleDto.FromModel(rule));
					}
				}
				JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
				string contents = javaScriptSerializer.Serialize(list);
				File.WriteAllText(FiltersJsonPath(), contents, Encoding.UTF8);
				result = true;
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Exception ex2 = ex;
				LastError = ex2.Message;
				result = false;
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}

	public static int TryImportFromProfile(string profileDir)
	{
		LastError = null;
		LastImportSkipped = false;
		if (string.IsNullOrWhiteSpace(profileDir))
		{
			LastError = "Profile folder is required.";
			return -1;
		}
		string path = Path.Combine(profileDir.Trim(), "msgFilterRules.dat");
		if (!File.Exists(path))
		{
			LastImportSkipped = true;
			return 0;
		}
		List<MailFilterRule> list = ThunderbirdMsgFilterParser.ParseFile(path);
		if (!string.IsNullOrEmpty(ThunderbirdMsgFilterParser.LastError))
		{
			LastError = ThunderbirdMsgFilterParser.LastError;
			return -1;
		}
		if (!SaveRules(list))
		{
			return -1;
		}
		return list.Count;
	}
}
