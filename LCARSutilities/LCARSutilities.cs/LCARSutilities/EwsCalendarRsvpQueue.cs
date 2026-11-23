using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSutilities;

[StandardModule]
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

	public static void Enqueue(string eventUid, string partStat)
	{
		string text = ((eventUid != null) ? eventUid : "").Trim();
		string text2 = ((partStat != null) ? partStat : "").Trim().ToUpperInvariant();
		if (text.Length != 0 && text2.Length != 0)
		{
			List<RsvpRequest> list = LoadQueue();
			list.Add(new RsvpRequest
			{
				Uid = text,
				PartStat = text2
			});
			SaveQueue(list);
		}
	}

	private static string QueuePath()
	{
		return Path.Combine(IcsCalendarStore.DataRoot(), "ews-rsvp-queue.json");
	}

	private static List<RsvpRequest> LoadQueue()
	{
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

	private static void SaveQueue(List<RsvpRequest> items)
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
