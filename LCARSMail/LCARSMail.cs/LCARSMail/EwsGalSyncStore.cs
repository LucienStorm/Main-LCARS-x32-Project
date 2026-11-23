using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

public sealed class EwsGalSyncStore
{
	private class GalSyncDto
	{
		public DateTime LastSyncUtc;

		public DateTime LastDeepSyncUtc;

		public int LastGalMergedCount;

		[DebuggerNonUserCode]
		public GalSyncDto()
		{
		}
	}

	private const string StoreFileName = "ews-gal-sync.json";

	private const double MinHoursBetweenSync = 3.0;

	private const double MinDaysBetweenDeepSync = 4.0;

	public static int LastGalMergedCount = 0;

	private EwsGalSyncStore()
	{
	}

	public static DateTime TryGetLastDeepSyncUtc()
	{
		return LoadDto()?.LastDeepSyncUtc ?? DateTime.MinValue;
	}

	public static int TryRefreshLastGalMergedCountFromDisk()
	{
		GalSyncDto galSyncDto = LoadDto();
		if (galSyncDto == null)
		{
			return LastGalMergedCount;
		}
		LastGalMergedCount = Math.Max(0, galSyncDto.LastGalMergedCount);
		return LastGalMergedCount;
	}

	public static bool ShouldRunGalSync()
	{
		GalSyncDto galSyncDto = LoadDto();
		if (galSyncDto == null)
		{
			return true;
		}
		return (DateTime.UtcNow - galSyncDto.LastSyncUtc).TotalHours >= 3.0;
	}

	public static void MarkGalSyncCompleted(int mergedCount = 0)
	{
		LastGalMergedCount = Math.Max(0, mergedCount);
		try
		{
			GalSyncDto galSyncDto = LoadDto();
			GalSyncDto galSyncDto2 = new GalSyncDto();
			galSyncDto2.LastSyncUtc = DateTime.UtcNow;
			galSyncDto2.LastDeepSyncUtc = galSyncDto?.LastDeepSyncUtc ?? DateTime.MinValue;
			galSyncDto2.LastGalMergedCount = LastGalMergedCount;
			GalSyncDto obj = galSyncDto2;
			JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
			File.WriteAllText(StorePath(), javaScriptSerializer.Serialize(obj), Encoding.UTF8);
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
	}

	public static bool ShouldRunDeepGalSync()
	{
		GalSyncDto galSyncDto = LoadDto();
		if (galSyncDto == null)
		{
			return true;
		}
		if (DateTime.Compare(galSyncDto.LastDeepSyncUtc, DateTime.MinValue) == 0)
		{
			return true;
		}
		return (DateTime.UtcNow - galSyncDto.LastDeepSyncUtc).TotalDays >= 4.0;
	}

	public static void MarkDeepGalSyncCompleted()
	{
		try
		{
			GalSyncDto galSyncDto = LoadDto();
			GalSyncDto galSyncDto2 = new GalSyncDto();
			galSyncDto2.LastSyncUtc = galSyncDto?.LastSyncUtc ?? DateTime.UtcNow;
			galSyncDto2.LastDeepSyncUtc = DateTime.UtcNow;
			galSyncDto2.LastGalMergedCount = galSyncDto?.LastGalMergedCount ?? LastGalMergedCount;
			GalSyncDto obj = galSyncDto2;
			JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
			File.WriteAllText(StorePath(), javaScriptSerializer.Serialize(obj), Encoding.UTF8);
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
	}

	private static GalSyncDto LoadDto()
	{
		GalSyncDto result;
		if (!File.Exists(StorePath()))
		{
			result = null;
		}
		else
		{
			try
			{
				JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
				result = javaScriptSerializer.Deserialize<GalSyncDto>(File.ReadAllText(StorePath(), Encoding.UTF8));
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = null;
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}

	private static string StorePath()
	{
		return Path.Combine(AccountStore.DataRoot(), "ews-gal-sync.json");
	}
}
