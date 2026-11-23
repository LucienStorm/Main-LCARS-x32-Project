using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSutilities;

[StandardModule]
public sealed class EwsCalendarDeleteQueue
{
	private const string QueueFileName = "ews-delete-queue.json";

	public static void Enqueue(string ewsItemId)
	{
		string text = ((ewsItemId != null) ? ewsItemId : "").Trim();
		if (text.Length != 0)
		{
			List<string> list = LoadQueue();
			if (!list.Contains(text))
			{
				list.Add(text);
				SaveQueue(list);
			}
		}
	}

	private static string QueuePath()
	{
		return Path.Combine(IcsCalendarStore.DataRoot(), "ews-delete-queue.json");
	}

	private static List<string> LoadQueue()
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
				result = ((list != null) ? list : new List<string>());
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

	private static void SaveQueue(List<string> ids)
	{
		if (ids == null)
		{
			return;
		}
		try
		{
			JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
			File.WriteAllText(QueuePath(), javaScriptSerializer.Serialize(ids), Encoding.UTF8);
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
	}
}
