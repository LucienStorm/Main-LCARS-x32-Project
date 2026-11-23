using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

public sealed class MailFilterEngine
{
	private MailFilterEngine()
	{
	}

	public static MailFilterRunSummary ApplyRulesToFolder(MailAccount account, string password, string folderFullName, List<MailMessageHeader> headers, ImapMailService mailService, List<MailFilterRule> rules)
	{
		MailFilterRunSummary mailFilterRunSummary = new MailFilterRunSummary();
		if (account == null || mailService == null || headers == null || rules == null)
		{
			return mailFilterRunSummary;
		}
		if (headers.Count == 0 || rules.Count == 0)
		{
			return mailFilterRunSummary;
		}
		checked
		{
			foreach (MailFilterRule rule in rules)
			{
				if (rule == null || !rule.Enabled || !rule.RunOnIncoming)
				{
					continue;
				}
				MailFilterRunSummary mailFilterRunSummary2 = mailFilterRunSummary;
				mailFilterRunSummary2.RulesEvaluated++;
				foreach (MailMessageHeader header in headers)
				{
					if (header != null && RuleMatches(rule, header, account, password, folderFullName, mailService, mailFilterRunSummary))
					{
						mailFilterRunSummary2 = mailFilterRunSummary;
						mailFilterRunSummary2.MessagesMatched++;
						if (ExecuteActions(rule, account, password, folderFullName, header, mailService, mailFilterRunSummary))
						{
							break;
						}
					}
				}
			}
			return mailFilterRunSummary;
		}
	}

	public static bool RuleMatchesForHeader(MailFilterRule rule, MailMessageHeader hdr, MailAccount account, string password, string folderFullName, Func<MailMessageHeader, string> bodyFetcher, MailFilterRunSummary summary)
	{
		if (rule.Conditions == null || rule.Conditions.Count == 0)
		{
			return true;
		}
		bool flag = string.Equals(rule.ConditionLogic, "OR", StringComparison.OrdinalIgnoreCase);
		bool result = false;
		bool result2 = true;
		foreach (MailFilterCondition condition in rule.Conditions)
		{
			bool flag2 = ConditionMatchesUnified(condition, hdr, account, password, folderFullName, bodyFetcher, summary);
			if (flag2)
			{
				result = true;
			}
			if (!flag2)
			{
				result2 = false;
			}
		}
		if (flag)
		{
			return result;
		}
		return result2;
	}

	private static bool ConditionMatchesUnified(MailFilterCondition cond, MailMessageHeader hdr, MailAccount account, string password, string folderFullName, Func<MailMessageHeader, string> bodyFetcher, MailFilterRunSummary summary)
	{
		if (cond == null)
		{
			return true;
		}
		MailFilterCondition mailFilterCondition = cond;
		string text = ((mailFilterCondition.Field != null) ? mailFilterCondition.Field : "").Trim().ToLowerInvariant();
		mailFilterCondition = cond;
		string op = ((mailFilterCondition.Operator != null) ? mailFilterCondition.Operator : "").Trim().ToLowerInvariant();
		mailFilterCondition = cond;
		string needle = ((mailFilterCondition.Value != null) ? mailFilterCondition.Value : "").Trim();
		string haystack = "";
		switch (text)
		{
		case "from":
		case "sender":
		{
			MailMessageHeader mailMessageHeader = hdr;
			haystack = ((mailMessageHeader.From != null) ? mailMessageHeader.From : "");
			break;
		}
		case "to":
		{
			MailMessageHeader mailMessageHeader = hdr;
			haystack = ((mailMessageHeader.ToAddresses != null) ? mailMessageHeader.ToAddresses : "");
			break;
		}
		case "subject":
		{
			MailMessageHeader mailMessageHeader = hdr;
			haystack = ((mailMessageHeader.Subject != null) ? mailMessageHeader.Subject : "");
			break;
		}
		case "body":
			if (bodyFetcher != null)
			{
				haystack = bodyFetcher(hdr);
			}
			break;
		case "age":
			return MatchAge(op, needle, hdr.DateUtc);
		case "size":
			return MatchSize(op, needle, hdr.SizeBytes);
		default:
		{
			MailMessageHeader mailMessageHeader = hdr;
			haystack = ((mailMessageHeader.Subject != null) ? mailMessageHeader.Subject : "") + " " + ((hdr.From != null) ? hdr.From : "");
			break;
		}
		}
		return MatchText(op, haystack, needle);
	}

	private static bool RuleMatches(MailFilterRule rule, MailMessageHeader hdr, MailAccount account, string password, string folderFullName, ImapMailService mailService, MailFilterRunSummary summary)
	{
		if (rule.Conditions == null || rule.Conditions.Count == 0)
		{
			return true;
		}
		bool flag = string.Equals(rule.ConditionLogic, "OR", StringComparison.OrdinalIgnoreCase);
		bool result = false;
		bool result2 = true;
		foreach (MailFilterCondition condition in rule.Conditions)
		{
			bool flag2 = ConditionMatches(condition, hdr, account, password, folderFullName, mailService, summary);
			if (flag2)
			{
				result = true;
			}
			if (!flag2)
			{
				result2 = false;
			}
		}
		if (flag)
		{
			return result;
		}
		return result2;
	}

	private static bool ConditionMatches(MailFilterCondition cond, MailMessageHeader hdr, MailAccount account, string password, string folderFullName, ImapMailService mailService, MailFilterRunSummary summary)
	{
		if (cond == null)
		{
			return true;
		}
		MailFilterCondition mailFilterCondition = cond;
		string text = ((mailFilterCondition.Field != null) ? mailFilterCondition.Field : "").Trim().ToLowerInvariant();
		mailFilterCondition = cond;
		string op = ((mailFilterCondition.Operator != null) ? mailFilterCondition.Operator : "").Trim().ToLowerInvariant();
		mailFilterCondition = cond;
		string needle = ((mailFilterCondition.Value != null) ? mailFilterCondition.Value : "").Trim();
		string text2 = "";
		switch (text)
		{
		case "from":
		case "sender":
		{
			MailMessageHeader mailMessageHeader2 = hdr;
			text2 = ((mailMessageHeader2.From != null) ? mailMessageHeader2.From : "");
			break;
		}
		case "to":
		{
			MailMessageHeader mailMessageHeader2 = hdr;
			text2 = ((mailMessageHeader2.ToAddresses != null) ? mailMessageHeader2.ToAddresses : "");
			break;
		}
		case "cc":
		{
			MailMessageHeader mailMessageHeader2 = hdr;
			text2 = ((mailMessageHeader2.CcAddresses != null) ? mailMessageHeader2.CcAddresses : "");
			break;
		}
		case "toorcc":
		{
			MailMessageHeader mailMessageHeader2 = hdr;
			string obj2 = ((mailMessageHeader2.ToAddresses != null) ? mailMessageHeader2.ToAddresses : "");
			MailMessageHeader mailMessageHeader = hdr;
			text2 = obj2 + " " + ((mailMessageHeader.CcAddresses != null) ? mailMessageHeader.CcAddresses : "");
			break;
		}
		case "subject":
		{
			MailMessageHeader mailMessageHeader = hdr;
			text2 = ((mailMessageHeader.Subject != null) ? mailMessageHeader.Subject : "");
			break;
		}
		case "body":
			text2 = mailService.FetchPlainBodyForFilter(account, password, folderFullName, hdr.Uid);
			break;
		case "age":
			return MatchAge(op, needle, hdr.DateUtc);
		case "size":
			return MatchSize(op, needle, hdr.SizeBytes);
		default:
		{
			MailMessageHeader mailMessageHeader = hdr;
			string obj = ((mailMessageHeader.Subject != null) ? mailMessageHeader.Subject : "");
			MailMessageHeader mailMessageHeader2 = hdr;
			text2 = obj + " " + ((mailMessageHeader2.From != null) ? mailMessageHeader2.From : "");
			break;
		}
		}
		return MatchText(op, text2, needle);
	}

	private static bool MatchText(string op, string haystack, string needle)
	{
		string text = ((haystack != null) ? haystack : "");
		string text2 = ((needle != null) ? needle : "");
		StringComparison comparisonType = StringComparison.OrdinalIgnoreCase;
		switch (op)
		{
		case "contains":
		case "doesn't contain":
		{
			bool flag2 = text.IndexOf(text2, comparisonType) >= 0;
			return (Operators.CompareString(op, "doesn't contain", TextCompare: false) == 0) ? (!flag2) : flag2;
		}
		case "is":
		case "isn't":
		{
			bool flag = string.Equals(text.Trim(), text2.Trim(), comparisonType);
			return (Operators.CompareString(op, "isn't", TextCompare: false) == 0) ? (!flag) : flag;
		}
		case "begins with":
			return text.StartsWith(text2, comparisonType);
		case "ends with":
			return text.EndsWith(text2, comparisonType);
		default:
			return text.IndexOf(text2, comparisonType) >= 0;
		}
	}

	private static bool MatchAge(string op, string needle, DateTime dateUtc)
	{
		if (DateTime.Compare(dateUtc, DateTime.MinValue) == 0)
		{
			return false;
		}
		int result = 0;
		int.TryParse(needle, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
		if (result < 0)
		{
			result = 0;
		}
		double totalDays = (DateTime.UtcNow - dateUtc.ToUniversalTime()).TotalDays;
		if (Operators.CompareString(op, "is greater than", TextCompare: false) == 0)
		{
			return totalDays > (double)result;
		}
		if (Operators.CompareString(op, "is less than", TextCompare: false) == 0)
		{
			return totalDays < (double)result;
		}
		return totalDays >= (double)result;
	}

	private static bool MatchSize(string op, string needle, long sizeBytes)
	{
		long result = 0L;
		long.TryParse(needle, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
		if (Operators.CompareString(op, "is greater than", TextCompare: false) == 0)
		{
			return sizeBytes > result;
		}
		if (Operators.CompareString(op, "is less than", TextCompare: false) == 0)
		{
			return sizeBytes < result;
		}
		return sizeBytes >= result;
	}

	private static bool ExecuteActions(MailFilterRule rule, MailAccount account, string password, string folderFullName, MailMessageHeader hdr, ImapMailService mailService, MailFilterRunSummary summary)
	{
		bool flag = false;
		if (rule.Actions == null)
		{
			return flag;
		}
		checked
		{
			foreach (MailFilterAction action in rule.Actions)
			{
				if (action == null)
				{
					continue;
				}
				MailFilterAction mailFilterAction = action;
				string text = ((mailFilterAction.ActionName != null) ? mailFilterAction.ActionName : "").Trim();
				mailFilterAction = action;
				string text2 = ((mailFilterAction.ActionValue != null) ? mailFilterAction.ActionValue : "").Trim();
				switch (text.ToLowerInvariant())
				{
				case "move to folder":
				{
					string text4 = ResolveImapFolderPath(text2);
					if (text4.Length > 0)
					{
						mailService.MoveMessages(account, password, folderFullName, text4, new ulong[1] { hdr.Uid });
						MailFilterRunSummary mailFilterRunSummary = summary;
						mailFilterRunSummary.ActionsApplied++;
					}
					break;
				}
				case "copy to folder":
				{
					string text3 = ResolveImapFolderPath(text2);
					if (text3.Length > 0)
					{
						mailService.CopyMessages(account, password, folderFullName, text3, new ulong[1] { hdr.Uid });
						MailFilterRunSummary mailFilterRunSummary = summary;
						mailFilterRunSummary.ActionsApplied++;
					}
					break;
				}
				case "mark read":
				case "mark as read":
				{
					mailService.MarkSeen(account, password, folderFullName, hdr.Uid);
					hdr.IsSeen = true;
					MailFilterRunSummary mailFilterRunSummary = summary;
					mailFilterRunSummary.ActionsApplied++;
					break;
				}
				case "mark unread":
				case "mark as unread":
				{
					mailService.MarkUnseen(account, password, folderFullName, hdr.Uid);
					hdr.IsSeen = false;
					MailFilterRunSummary mailFilterRunSummary = summary;
					mailFilterRunSummary.ActionsApplied++;
					break;
				}
				case "delete":
				case "delete from pop server":
				case "delete from server":
				{
					mailService.DeleteMessages(account, password, folderFullName, new ulong[1] { hdr.Uid });
					MailFilterRunSummary mailFilterRunSummary = summary;
					mailFilterRunSummary.ActionsApplied++;
					break;
				}
				case "mark as junk":
				case "junk":
				{
					mailService.MoveToJunk(account, password, folderFullName, new ulong[1] { hdr.Uid });
					hdr.IsJunk = true;
					MailFilterRunSummary mailFilterRunSummary = summary;
					mailFilterRunSummary.ActionsApplied++;
					break;
				}
				case "add tag":
				case "tag":
					if (text2.Length > 0)
					{
						mailService.AddKeyword(account, password, folderFullName, hdr.Uid, text2);
						MailFilterRunSummary mailFilterRunSummary = summary;
						mailFilterRunSummary.ActionsApplied++;
					}
					break;
				case "forward message to":
				case "forward":
					if (text2.Length > 0)
					{
						ForwardMessage(account, password, folderFullName, hdr, mailService, text2, summary);
					}
					break;
				case "reply to sender":
				case "reply":
					ReplyToSender(account, password, folderFullName, hdr, mailService, text2, summary);
					break;
				case "stop execution":
					flag = true;
					break;
				default:
					AppendWarningOnce(summary, "Unsupported filter action: " + text);
					break;
				}
				if (!flag)
				{
					continue;
				}
				break;
			}
			return flag;
		}
	}

	private static void ForwardMessage(MailAccount account, string password, string folderFullName, MailMessageHeader hdr, ImapMailService mailService, string forwardTo, MailFilterRunSummary summary)
	{
		string bodyText = mailService.FetchPlainBodyForFilter(account, password, folderFullName, hdr.Uid);
		string subject = "Fwd: " + ((hdr.Subject != null) ? hdr.Subject : "");
		SmtpMailService smtpMailService = new SmtpMailService();
		string text = smtpMailService.Send(account, password, forwardTo, "", subject, bodyText, null);
		checked
		{
			if (string.IsNullOrEmpty(text))
			{
				summary.ActionsApplied++;
			}
			else
			{
				AppendWarningOnce(summary, "Forward failed: " + text);
			}
		}
	}

	private static void ReplyToSender(MailAccount account, string password, string folderFullName, MailMessageHeader hdr, ImapMailService mailService, string template, MailFilterRunSummary summary)
	{
		MailMessageHeader mailMessageHeader = hdr;
		string text = ((mailMessageHeader.From != null) ? mailMessageHeader.From : "").Trim();
		checked
		{
			if (text.Length != 0)
			{
				string text2 = ((template != null) ? template : "");
				if (text2.Length == 0)
				{
					text2 = "This is an automated reply.";
				}
				mailMessageHeader = hdr;
				string subject = "Re: " + ((mailMessageHeader.Subject != null) ? mailMessageHeader.Subject : "");
				SmtpMailService smtpMailService = new SmtpMailService();
				string text3 = smtpMailService.Send(account, password, text, "", subject, text2, null);
				if (string.IsNullOrEmpty(text3))
				{
					summary.ActionsApplied++;
				}
				else
				{
					AppendWarningOnce(summary, "Reply failed: " + text3);
				}
			}
		}
	}

	public static string ResolveImapFolderPath(string actionValue)
	{
		string text = ((actionValue != null) ? actionValue : "").Trim();
		if (text.Length == 0)
		{
			return "";
		}
		checked
		{
			if (text.StartsWith("imap://", StringComparison.OrdinalIgnoreCase) || text.StartsWith("imaps://", StringComparison.OrdinalIgnoreCase))
			{
				int num = text.IndexOf("//", StringComparison.Ordinal);
				if (num < 0)
				{
					return "";
				}
				int num2 = text.IndexOf('/', num + 2);
				if (num2 >= 0 && num2 + 1 < text.Length)
				{
					return Uri.UnescapeDataString(text.Substring(num2 + 1));
				}
				return "";
			}
			return text;
		}
	}

	private static void AppendWarningOnce(MailFilterRunSummary summary, string text)
	{
		if (summary != null && !string.IsNullOrEmpty(text) && !summary.Warnings.Contains(text))
		{
			summary.Warnings.Add(text);
		}
	}
}
