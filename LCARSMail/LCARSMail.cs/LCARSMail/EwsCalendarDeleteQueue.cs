using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

public sealed class EwsCalendarDeleteQueue
{
	private const string QueueFileName = "ews-delete-queue.json";

	private EwsCalendarDeleteQueue()
	{
	}

	private static string QueuePath()
	{
		return Path.Combine(LcarsCalendarBridge.CalendarDataRoot(), "ews-delete-queue.json");
	}

	public static List<string> TakePendingDeletes()
	{
		string path = QueuePath();
		List<string> result;
		if (!File.Exists(path))
		{
			result = new List<string>();
		}
		else
		{
			try
			{
				JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
				List<string> list = javaScriptSerializer.Deserialize<List<string>>(File.ReadAllText(path, Encoding.UTF8));
				File.Delete(path);
				if (list == null)
				{
					result = new List<string>();
				}
				else
				{
					List<string> list2 = new List<string>();
					foreach (string item in list)
					{
						string text = ((item != null) ? item : "").Trim();
						if (text.Length > 0)
						{
							list2.Add(text);
						}
					}
					result = list2;
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = new List<string>();
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}
}
