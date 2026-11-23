using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

public sealed class EwsCalendarRsvpQueue
{
	public class RsvpRequest
	{
		public string Uid;

		public string PartStat;

		[DebuggerNonUserCode]
		public RsvpRequest()
		{
		}
	}

	private const string QueueFileName = "ews-rsvp-queue.json";

	public static int LastProcessedCount = 0;

	private EwsCalendarRsvpQueue()
	{
	}

	private static string QueuePath()
	{
		return Path.Combine(LcarsCalendarBridge.CalendarDataRoot(), "ews-rsvp-queue.json");
	}

	public static List<RsvpRequest> TakePendingRsvps()
	{
		LastProcessedCount = 0;
		string path = QueuePath();
		List<RsvpRequest> result;
		if (!File.Exists(path))
		{
			result = new List<RsvpRequest>();
		}
		else
		{
			try
			{
				JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
				List<RsvpRequest> list = javaScriptSerializer.Deserialize<List<RsvpRequest>>(File.ReadAllText(path, Encoding.UTF8));
				File.Delete(path);
				result = ((list != null) ? list : new List<RsvpRequest>());
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = new List<RsvpRequest>();
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}
}
