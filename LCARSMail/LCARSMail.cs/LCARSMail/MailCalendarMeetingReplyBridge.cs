using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Web.Script.Serialization;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

public sealed class MailCalendarMeetingReplyBridge
{
	private class ReplyDto
	{
		public string EventUid;

		public string PartStat;

		public string Organizer;

		public string Summary;

		public string AttendeeMailto;

		[DebuggerNonUserCode]
		public ReplyDto()
		{
		}
	}

	public static int LastSentCount = 0;

	private const string QueueFileName = "calendar-meeting-reply-queue.json";

	private MailCalendarMeetingReplyBridge()
	{
	}

	public static int TryFlushQueuedMeetingReplies(string smtpHost, int smtpPort, string fromAddress, string smtpUser, string smtpPassword)
	{
		LastSentCount = 0;
		string text = ((smtpHost != null) ? smtpHost : "").Trim();
		string text2 = ((fromAddress != null) ? fromAddress : "").Trim();
		if (text.Length == 0 || text2.Length == 0)
		{
			return 0;
		}
		List<ReplyDto> list = TakePendingReplies();
		if (list == null || list.Count == 0)
		{
			return 0;
		}
		int num = 0;
		foreach (ReplyDto item in list)
		{
			if (item != null && TrySendOneReply(item, text, smtpPort, text2, smtpUser, smtpPassword))
			{
				num = checked(num + 1);
			}
		}
		LastSentCount = num;
		return num;
	}

	private static List<ReplyDto> TakePendingReplies()
	{
		string path = Path.Combine(LcarsCalendarBridge.CalendarDataRoot(), "calendar-meeting-reply-queue.json");
		List<ReplyDto> result;
		if (!File.Exists(path))
		{
			result = new List<ReplyDto>();
		}
		else
		{
			try
			{
				JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
				List<ReplyDto> list = javaScriptSerializer.Deserialize<List<ReplyDto>>(File.ReadAllText(path, Encoding.UTF8));
				File.Delete(path);
				result = ((list != null) ? list : new List<ReplyDto>());
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = new List<ReplyDto>();
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}

	private static bool TrySendOneReply(ReplyDto req, string smtpHost, int smtpPort, string fromAddress, string smtpUser, string smtpPassword)
	{
		string text = NormalizeSmtp(MailAttendeeHintBridge.NormalizeOrganizerSmtp(req.Organizer));
		bool result;
		if (text.Length == 0)
		{
			result = false;
		}
		else
		{
			ReplyDto replyDto = req;
			string text2 = ((replyDto.PartStat != null) ? replyDto.PartStat : "ACCEPTED").Trim().ToUpperInvariant();
			replyDto = req;
			string subject = "RE: " + ((replyDto.Summary != null) ? replyDto.Summary : "Meeting").Trim() + " — " + text2;
			string s = BuildReplyIcs(req, text2);
			try
			{
				using (MailMessage mailMessage = new MailMessage(fromAddress, text))
				{
					mailMessage.Subject = subject;
					mailMessage.Body = "PARTSTAT=" + text2;
					AlternateView item = new AlternateView(new MemoryStream(Encoding.UTF8.GetBytes(s)), "text/calendar; method=REPLY");
					mailMessage.AlternateViews.Add(item);
					using SmtpClient smtpClient = new SmtpClient(smtpHost, Math.Max(1, smtpPort));
					smtpClient.EnableSsl = ((smtpPort == 587 || smtpPort == 465) ? true : false);
					if (!string.IsNullOrEmpty(smtpUser))
					{
						smtpClient.Credentials = new NetworkCredential(smtpUser, smtpPassword);
					}
					smtpClient.Send(mailMessage);
				}
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

	private static string BuildReplyIcs(ReplyDto req, string partStat)
	{
		ReplyDto replyDto = req;
		string text = ((replyDto.EventUid != null) ? replyDto.EventUid : Guid.NewGuid().ToString()).Trim();
		replyDto = req;
		string text2 = ((replyDto.AttendeeMailto != null) ? replyDto.AttendeeMailto : "").Trim();
		if (text2.Length == 0)
		{
			text2 = "mailto:unknown@invalid";
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("BEGIN:VCALENDAR");
		stringBuilder.AppendLine("VERSION:2.0");
		stringBuilder.AppendLine("METHOD:REPLY");
		stringBuilder.AppendLine("BEGIN:VEVENT");
		stringBuilder.AppendLine("UID:" + text);
		replyDto = req;
		stringBuilder.AppendLine("SUMMARY:" + ((replyDto.Summary != null) ? replyDto.Summary : ""));
		stringBuilder.AppendLine("ATTENDEE;PARTSTAT=" + partStat + ":" + text2);
		stringBuilder.AppendLine("END:VEVENT");
		stringBuilder.AppendLine("END:VCALENDAR");
		return stringBuilder.ToString();
	}

	private static string NormalizeSmtp(string addr)
	{
		string text = ((addr != null) ? addr : "").Trim();
		if (text.Length == 0)
		{
			return "";
		}
		if (text.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
		{
			text = text.Substring(7);
		}
		return text.Trim();
	}
}
