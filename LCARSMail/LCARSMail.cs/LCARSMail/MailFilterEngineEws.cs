using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace LCARSMail;

public sealed class MailFilterEngineEws
{
	private MailFilterEngineEws()
	{
	}

	public static MailFilterRunSummary ApplyRulesToFolder(MailAccount account, string password, string folderFullName, List<MailMessageHeader> headers, UnifiedMailService mailService, List<MailFilterRule> rules)
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
					if (header == null)
					{
						continue;
					}
					Func<MailMessageHeader, string> bodyFetcher = [SpecialName] (MailMessageHeader h) => mailService.FetchPlainBodyForFilter(account, password, folderFullName, h.Uid);
					if (MailFilterEngine.RuleMatchesForHeader(rule, header, account, password, folderFullName, bodyFetcher, mailFilterRunSummary))
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

	private static bool ExecuteActions(MailFilterRule rule, MailAccount account, string password, string folderFullName, MailMessageHeader hdr, UnifiedMailService mailService, MailFilterRunSummary summary)
	{
		bool flag = false;
		if (rule.Actions == null)
		{
			return flag;
		}
		List<ulong> list = new List<ulong>();
		list.Add(hdr.Uid);
		List<ulong> uids = list;
		checked
		{
			foreach (MailFilterAction action in rule.Actions)
			{
				if (action == null)
				{
					continue;
				}
				MailFilterAction mailFilterAction = action;
				string text = ((mailFilterAction.ActionName != null) ? mailFilterAction.ActionName : "").Trim().ToLowerInvariant();
				mailFilterAction = action;
				string text2 = ((mailFilterAction.ActionValue != null) ? mailFilterAction.ActionValue : "").Trim();
				switch (text)
				{
				case "move to folder":
				{
					string text4 = EwsMailBridge.ResolveFilterFolder(text2);
					if (text4.Length > 0)
					{
						mailService.MoveMessages(account, password, folderFullName, text4, uids);
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
					mailService.DeleteMessages(account, password, folderFullName, uids);
					MailFilterRunSummary mailFilterRunSummary = summary;
					mailFilterRunSummary.ActionsApplied++;
					break;
				}
				case "mark as junk":
				case "junk":
				{
					mailService.MoveToJunk(account, password, folderFullName, uids);
					hdr.IsJunk = true;
					MailFilterRunSummary mailFilterRunSummary = summary;
					mailFilterRunSummary.ActionsApplied++;
					break;
				}
				case "copy to folder":
				{
					string text3 = EwsMailBridge.ResolveFilterFolder(text2);
					if (text3.Length > 0)
					{
						mailService.CopyMessages(account, password, folderFullName, text3, uids);
						MailFilterRunSummary mailFilterRunSummary = summary;
						mailFilterRunSummary.ActionsApplied++;
					}
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
						ForwardMessageEws(account, password, folderFullName, hdr, mailService, text2, summary);
					}
					break;
				case "reply to sender":
				case "reply":
					ReplyToSenderEws(account, password, folderFullName, hdr, mailService, text2, summary);
					break;
				case "stop execution":
					flag = true;
					break;
				default:
					if (!summary.Warnings.Contains("EWS filter action not supported: " + text))
					{
						summary.Warnings.Add("EWS filter action not supported: " + text);
					}
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

	private static void ForwardMessageEws(MailAccount account, string password, string folderFullName, MailMessageHeader hdr, UnifiedMailService mailService, string forwardTo, MailFilterRunSummary summary)
	{
		string bodyText = mailService.FetchPlainBodyForFilter(account, password, folderFullName, hdr.Uid);
		string subject = "Fwd: " + ((hdr.Subject != null) ? hdr.Subject : "");
		string text = EwsMailBridge.TrySendFilterMail(account, password, forwardTo, subject, bodyText);
		checked
		{
			if (string.IsNullOrEmpty(text))
			{
				summary.ActionsApplied++;
			}
			else if (!summary.Warnings.Contains(text))
			{
				summary.Warnings.Add(text);
			}
		}
	}

	private static void ReplyToSenderEws(MailAccount account, string password, string folderFullName, MailMessageHeader hdr, UnifiedMailService mailService, string template, MailFilterRunSummary summary)
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
				string text3 = EwsMailBridge.TrySendFilterMail(account, password, text, subject, text2);
				if (string.IsNullOrEmpty(text3))
				{
					summary.ActionsApplied++;
				}
				else if (!summary.Warnings.Contains(text3))
				{
					summary.Warnings.Add(text3);
				}
			}
		}
	}
}
