using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

public sealed class EwsGalWriteProofStore
{
	public class GalWriteProofDto
	{
		public DateTime RecordedUtc;

		public int DirtyCount;

		public int PersonaAttempts;

		public int PersonaSuccess;

		public int DirectoryAttempts;

		public int DirectorySuccess;

		public int ShadowUpdateSuccess;

		public int ShadowCreateSuccess;

		public bool PersonaDenied;

		public bool DirectoryDenied;

		public string SummaryLine;

		public string DominantWritePath;

		[DebuggerNonUserCode]
		public GalWriteProofDto()
		{
		}
	}

	private const string StoreFileName = "ews-gal-write-proof.json";

	public static GalWriteProofDto LastProof;

	private EwsGalWriteProofStore()
	{
	}

	public static void RecordPushSession(int dirtyCount, int personaAttempts, int personaSuccess, int directoryAttempts, int directorySuccess, int shadowUpdateSuccess, int shadowCreateSuccess, bool personaDenied, bool directoryDenied)
	{
		string text = "GAL PUSH PROOF — dirty " + dirtyCount + " · persona " + personaSuccess + "/" + personaAttempts + " · dir " + directorySuccess + "/" + directoryAttempts + " · shadow upd " + shadowUpdateSuccess + " · shadow new " + shadowCreateSuccess;
		if (personaDenied)
		{
			text += " · PERSONA DENIED";
		}
		if (directoryDenied)
		{
			text += " · DIR DENIED";
		}
		if (personaSuccess == 0 && directorySuccess == 0 && shadowUpdateSuccess == 0 && shadowCreateSuccess == 0 && dirtyCount > 0)
		{
			text += " · NO GAL WRITE CONFIRMED";
		}
		string text2 = "none";
		if (personaSuccess > 0)
		{
			text2 = "persona";
		}
		if (directorySuccess > personaSuccess)
		{
			text2 = "directory";
		}
		if (checked(shadowUpdateSuccess + shadowCreateSuccess > directorySuccess && shadowUpdateSuccess + shadowCreateSuccess > personaSuccess))
		{
			text2 = "shadow";
		}
		if (Operators.CompareString(text2, "none", TextCompare: false) != 0)
		{
			text = text + " · PATH " + text2.ToUpperInvariant();
		}
		GalWriteProofDto galWriteProofDto = new GalWriteProofDto();
		galWriteProofDto.RecordedUtc = DateTime.UtcNow;
		galWriteProofDto.DirtyCount = dirtyCount;
		galWriteProofDto.PersonaAttempts = personaAttempts;
		galWriteProofDto.PersonaSuccess = personaSuccess;
		galWriteProofDto.DirectoryAttempts = directoryAttempts;
		galWriteProofDto.DirectorySuccess = directorySuccess;
		galWriteProofDto.ShadowUpdateSuccess = shadowUpdateSuccess;
		galWriteProofDto.ShadowCreateSuccess = shadowCreateSuccess;
		galWriteProofDto.PersonaDenied = personaDenied;
		galWriteProofDto.DirectoryDenied = directoryDenied;
		galWriteProofDto.SummaryLine = text;
		galWriteProofDto.DominantWritePath = text2;
		LastProof = galWriteProofDto;
		try
		{
			JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
			File.WriteAllText(StorePath(), javaScriptSerializer.Serialize(LastProof), Encoding.UTF8);
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
	}

	public static string TryGetDominantWritePath()
	{
		string text = TryLoadLastProofSummary();
		if (LastProof == null)
		{
			return "";
		}
		GalWriteProofDto lastProof = LastProof;
		return ((lastProof.DominantWritePath != null) ? lastProof.DominantWritePath : "").Trim();
	}

	public static string TryLoadLastProofSummary()
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
				GalWriteProofDto galWriteProofDto = javaScriptSerializer.Deserialize<GalWriteProofDto>(File.ReadAllText(path, Encoding.UTF8));
				if (galWriteProofDto == null || string.IsNullOrWhiteSpace(galWriteProofDto.SummaryLine))
				{
					result = "";
				}
				else
				{
					LastProof = galWriteProofDto;
					result = galWriteProofDto.SummaryLine.Trim();
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
		string text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LCARS", "Mail");
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		return Path.Combine(text, "ews-gal-write-proof.json");
	}
}
