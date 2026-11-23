using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

public sealed class EwsOAuthClientStore
{
	public class OAuthAppSettings
	{
		public string Tenant;

		public string ClientId;

		[DebuggerNonUserCode]
		public OAuthAppSettings()
		{
		}
	}

	private const string SettingsFileName = "ews-oauth-app.json";

	private EwsOAuthClientStore()
	{
	}

	public static void SaveSettings(string tenant, string clientId)
	{
		try
		{
			OAuthAppSettings oAuthAppSettings = new OAuthAppSettings();
			oAuthAppSettings.Tenant = ((tenant != null) ? tenant : "").Trim();
			oAuthAppSettings.ClientId = ((clientId != null) ? clientId : "").Trim();
			OAuthAppSettings obj = oAuthAppSettings;
			JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
			File.WriteAllText(SettingsPath(), javaScriptSerializer.Serialize(obj), Encoding.UTF8);
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
	}

	public static bool TryLoadSettings(ref string tenant, ref string clientId)
	{
		tenant = "organizations";
		clientId = "94933e60-ee10-4250-a050-3efd588b0718";
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
				OAuthAppSettings oAuthAppSettings = javaScriptSerializer.Deserialize<OAuthAppSettings>(File.ReadAllText(SettingsPath(), Encoding.UTF8));
				if (oAuthAppSettings == null)
				{
					result = false;
				}
				else
				{
					if (!string.IsNullOrWhiteSpace(oAuthAppSettings.Tenant))
					{
						tenant = oAuthAppSettings.Tenant.Trim();
					}
					if (!string.IsNullOrWhiteSpace(oAuthAppSettings.ClientId))
					{
						clientId = oAuthAppSettings.ClientId.Trim();
					}
					result = true;
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
		return Path.Combine(AccountStore.DataRoot(), "ews-oauth-app.json");
	}
}
