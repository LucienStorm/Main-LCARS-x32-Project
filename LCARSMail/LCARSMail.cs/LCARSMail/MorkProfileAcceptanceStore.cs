using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

public sealed class MorkProfileAcceptanceStore
{
	public class MorkProfileAcceptanceDto
	{
		public string ProfileMabPath;

		public DateTime RecordedUtc;

		public int ContactCount;

		public bool RoundTripValidated;

		public bool ByteIdenticalPreserve;

		public string LastError;

		[DebuggerNonUserCode]
		public MorkProfileAcceptanceDto()
		{
		}
	}

	public static MorkProfileAcceptanceDto LastRecord;

	private const string StoreFileName = "mork-profile-acceptance.json";

	private MorkProfileAcceptanceStore()
	{
	}

	public static void RecordProfileImport(string mabPath, int contactCount, bool roundTripValidated, bool byteIdenticalPreserve, string lastError)
	{
		MorkProfileAcceptanceDto morkProfileAcceptanceDto = new MorkProfileAcceptanceDto();
		morkProfileAcceptanceDto.ProfileMabPath = ((mabPath != null) ? mabPath : "").Trim();
		morkProfileAcceptanceDto.RecordedUtc = DateTime.UtcNow;
		morkProfileAcceptanceDto.ContactCount = contactCount;
		morkProfileAcceptanceDto.RoundTripValidated = roundTripValidated;
		morkProfileAcceptanceDto.ByteIdenticalPreserve = byteIdenticalPreserve;
		morkProfileAcceptanceDto.LastError = ((lastError != null) ? lastError : "").Trim();
		LastRecord = morkProfileAcceptanceDto;
		try
		{
			JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
			File.WriteAllText(StorePath(), javaScriptSerializer.Serialize(LastRecord), Encoding.UTF8);
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
	}

	public static string TryLoadSummaryLine()
	{
		string result;
		try
		{
			string path = StorePath();
			if (!File.Exists(path))
			{
				result = "";
			}
			else
			{
				JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
				MorkProfileAcceptanceDto morkProfileAcceptanceDto = javaScriptSerializer.Deserialize<MorkProfileAcceptanceDto>(File.ReadAllText(path, Encoding.UTF8));
				if (morkProfileAcceptanceDto == null)
				{
					result = "";
				}
				else
				{
					LastRecord = morkProfileAcceptanceDto;
					result = "MORK PROFILE — cnt " + morkProfileAcceptanceDto.ContactCount + (morkProfileAcceptanceDto.ByteIdenticalPreserve ? " · BYTE-OK" : (morkProfileAcceptanceDto.RoundTripValidated ? " · RT-OK" : " · RT-FAIL"));
				}
			}
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			result = "";
			ProjectData.ClearProjectError();
		}
		return result;
	}

	private static string StorePath()
	{
		string text = Path.Combine(AccountStore.DataRoot(), "abook");
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		return Path.Combine(text, "mork-profile-acceptance.json");
	}
}
