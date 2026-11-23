using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

public sealed class XmppAccountStore
{
	public class XmppSettings
	{
		public string Host;

		public int Port;

		public string Jid;

		[DebuggerNonUserCode]
		public XmppSettings()
		{
		}
	}

	private const string SettingsFileName = "xmpp-settings.json";

	private const string PasswordSecretKey = "xmpp";

	private XmppAccountStore()
	{
	}

	public static void SaveAccount(string host, int port, string jid, string password)
	{
		try
		{
			XmppSettings xmppSettings = new XmppSettings();
			xmppSettings.Host = ((host != null) ? host : "").Trim();
			xmppSettings.Port = port;
			xmppSettings.Jid = ((jid != null) ? jid : "").Trim();
			XmppSettings obj = xmppSettings;
			JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
			File.WriteAllText(SettingsPath(), javaScriptSerializer.Serialize(obj), Encoding.UTF8);
			if (password != null)
			{
				AccountStore.SavePassword("xmpp", password);
			}
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
	}

	public static bool TryLoadAccount(ref string host, ref int port, ref string jid, ref string password)
	{
		host = null;
		jid = null;
		password = null;
		port = 5222;
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
				XmppSettings xmppSettings = javaScriptSerializer.Deserialize<XmppSettings>(File.ReadAllText(SettingsPath(), Encoding.UTF8));
				if (xmppSettings == null || string.IsNullOrWhiteSpace(xmppSettings.Jid))
				{
					result = false;
				}
				else
				{
					XmppSettings xmppSettings2 = xmppSettings;
					host = ((xmppSettings2.Host != null) ? xmppSettings2.Host : "").Trim();
					jid = xmppSettings.Jid.Trim();
					if (xmppSettings.Port > 0)
					{
						port = xmppSettings.Port;
					}
					result = AccountStore.TryGetPassword("xmpp", ref password);
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
		return Path.Combine(AccountStore.DataRoot(), "xmpp-settings.json");
	}
}
