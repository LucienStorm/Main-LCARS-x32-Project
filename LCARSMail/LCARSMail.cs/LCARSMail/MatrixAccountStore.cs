using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

public sealed class MatrixAccountStore
{
	public class MatrixSettings
	{
		public string Homeserver;

		public string UserId;

		[DebuggerNonUserCode]
		public MatrixSettings()
		{
		}
	}

	private const string SettingsFileName = "matrix-settings.json";

	private const string TokenSecretKey = "matrix";

	private MatrixAccountStore()
	{
	}

	public static void SaveSession(string homeserver, string userId, string accessToken)
	{
		string text = ((homeserver != null) ? homeserver : "").Trim();
		if (text.Length == 0)
		{
			return;
		}
		try
		{
			MatrixSettings matrixSettings = new MatrixSettings();
			matrixSettings.Homeserver = text;
			matrixSettings.UserId = ((userId != null) ? userId : "").Trim();
			MatrixSettings obj = matrixSettings;
			JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
			File.WriteAllText(SettingsPath(), javaScriptSerializer.Serialize(obj), Encoding.UTF8);
			if (accessToken != null)
			{
				AccountStore.SavePassword("matrix", accessToken);
			}
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
	}

	public static bool TryLoadSession(ref string homeserver, ref string userId, ref string accessToken)
	{
		homeserver = null;
		userId = null;
		accessToken = null;
		bool result;
		if (!File.Exists(SettingsPath()))
		{
			result = false;
		}
		else
		{
			try
			{
				JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
				MatrixSettings matrixSettings = javaScriptSerializer.Deserialize<MatrixSettings>(File.ReadAllText(SettingsPath(), Encoding.UTF8));
				if (matrixSettings == null || string.IsNullOrWhiteSpace(matrixSettings.Homeserver))
				{
					result = false;
				}
				else
				{
					homeserver = matrixSettings.Homeserver.Trim();
					MatrixSettings matrixSettings2 = matrixSettings;
					userId = ((matrixSettings2.UserId != null) ? matrixSettings2.UserId : "").Trim();
					result = AccountStore.TryGetPassword("matrix", ref accessToken);
				}
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

	private static string SettingsPath()
	{
		return Path.Combine(AccountStore.DataRoot(), "matrix-settings.json");
	}
}
