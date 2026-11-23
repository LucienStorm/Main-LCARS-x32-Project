using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSutilities;

[StandardModule]
public sealed class CalendarMeetingReplyQueue
{
	public class ReplyRequest
	{
		public string EventUid;

		public string PartStat;

		public string Organizer;

		public string Summary;

		public string AttendeeMailto;

		[DebuggerNonUserCode]
		public ReplyRequest()
		{
		}
	}

	private const string QueueFileName = "calendar-meeting-reply-queue.json";

	public static void Enqueue(string eventUid, string partStat, string organizer, string summary, string attendeeMailto)
	{
		string text = ((eventUid != null) ? eventUid : "").Trim();
		string text2 = ((partStat != null) ? partStat : "").Trim().ToUpperInvariant();
		if (text.Length != 0 && text2.Length != 0)
		{
			List<ReplyRequest> list = LoadQueue();
			list.Add(new ReplyRequest
			{
				EventUid = text,
				PartStat = text2,
				Organizer = ((organizer != null) ? organizer : "").Trim(),
				Summary = ((summary != null) ? summary : "").Trim(),
				AttendeeMailto = ((attendeeMailto != null) ? attendeeMailto : "").Trim()
			});
			SaveQueue(list);
		}
	}

	private static string QueuePath()
	{
		return Path.Combine(IcsCalendarStore.DataRoot(), "calendar-meeting-reply-queue.json");
	}

	private static List<ReplyRequest> LoadQueue()
	{
		string path = QueuePath();
		List<ReplyRequest> result;
		if (!File.Exists(path))
		{
			result = new List<ReplyRequest>();
		}
		else
		{
			try
			{
				JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
				List<ReplyRequest> list = javaScriptSerializer.Deserialize<List<ReplyRequest>>(File.ReadAllText(path, Encoding.UTF8));
				result = ((list != null) ? list : new List<ReplyRequest>());
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = new List<ReplyRequest>();
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}

	private static void SaveQueue(List<ReplyRequest> items)
	{
		if (items == null)
		{
			return;
		}
		try
		{
			JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
			File.WriteAllText(QueuePath(), javaScriptSerializer.Serialize(items), Encoding.UTF8);
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
	}
}
