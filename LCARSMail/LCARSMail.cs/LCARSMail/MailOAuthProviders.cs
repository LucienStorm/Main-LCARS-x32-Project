using System;
using System.Diagnostics;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

public sealed class MailOAuthProviders
{
	public class ProviderInfo
	{
		public string Issuer;

		public string ClientId;

		public string ClientSecret;

		public string AuthorizationEndpoint;

		public string TokenEndpoint;

		public string Scope;

		public bool UsePkce;

		[DebuggerNonUserCode]
		public ProviderInfo()
		{
		}
	}

	public const string AuthMethodOAuth2 = "oauth2";

	public const string AuthMethodPassword = "password";

	private MailOAuthProviders()
	{
	}

	public static bool UsesOAuth(MailAccount account)
	{
		if (account == null)
		{
			return false;
		}
		if (EwsMailBridge.IsEwsAccount(account))
		{
			return false;
		}
		string a = ((account.AuthMethod != null) ? account.AuthMethod : "").Trim();
		if (string.Equals(a, "password", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (string.Equals(a, "oauth2", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		return TryResolveProvider(account) != null;
	}

	public static ProviderInfo TryResolveProvider(MailAccount account)
	{
		if (account == null)
		{
			return null;
		}
		string text = ((account.ImapHost != null) ? account.ImapHost : "").Trim();
		if (text.Length == 0)
		{
			return null;
		}
		return TryResolveByHostname(text);
	}

	public static ProviderInfo TryResolveByHostname(string hostname)
	{
		string text = ((hostname != null) ? hostname : "").Trim().ToLowerInvariant();
		if (text.Length == 0)
		{
			return null;
		}
		if (Operators.CompareString(text, "imap.gmail.com", TextCompare: false) != 0 && Operators.CompareString(text, "imap.googlemail.com", TextCompare: false) != 0 && !text.EndsWith(".gmail.com", StringComparison.Ordinal) && !text.EndsWith(".googlemail.com", StringComparison.Ordinal))
		{
			switch (text)
			{
			case "gmail.com":
			case "googlemail.com":
				break;
			default:
				if (!text.EndsWith(".office365.com", StringComparison.Ordinal) && !text.EndsWith(".outlook.com", StringComparison.Ordinal))
				{
					switch (text)
					{
					case "outlook.office.com":
					case "imap-mail.outlook.com":
					case "smtp-mail.outlook.com":
					case "hotmail.com":
						break;
					default:
						return null;
					}
				}
				goto case "outlook.office365.com";
			case "outlook.office365.com":
			case "smtp.office365.com":
				return MicrosoftProvider();
			}
		}
		return GoogleProvider();
	}

	public static bool IsThunderbirdOAuthAuthMethod(string authMethodRaw)
	{
		string text = ((authMethodRaw != null) ? authMethodRaw : "").Trim();
		if (text.Length == 0)
		{
			return false;
		}
		if (int.TryParse(text, out var result) && result == 10)
		{
			return true;
		}
		return (string.Equals(text, "oauth2", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "xoauth2", StringComparison.OrdinalIgnoreCase)) ? true : false;
	}

	public static ProviderInfo GoogleProvider()
	{
		ProviderInfo providerInfo = new ProviderInfo();
		providerInfo.Issuer = "accounts.google.com";
		providerInfo.ClientId = "406964657835-aq8lmia8j95dhl1a2bvharmfk3t1hgqj.apps.googleusercontent.com";
		providerInfo.ClientSecret = "kSmqreRr0qwBWJgbf5Y-PjSU";
		providerInfo.AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
		providerInfo.TokenEndpoint = "https://oauth2.googleapis.com/token";
		providerInfo.Scope = "https://mail.google.com/";
		providerInfo.UsePkce = true;
		return providerInfo;
	}

	public static ProviderInfo MicrosoftProvider()
	{
		ProviderInfo providerInfo = new ProviderInfo();
		providerInfo.Issuer = "login.microsoftonline.com";
		providerInfo.ClientId = "9e5f94bc-e8a4-4e73-b8be-63364c29d753";
		providerInfo.ClientSecret = "";
		providerInfo.AuthorizationEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize";
		providerInfo.TokenEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/token";
		providerInfo.Scope = "https://outlook.office.com/IMAP.AccessAsUser.All https://outlook.office.com/SMTP.Send offline_access";
		providerInfo.UsePkce = true;
		return providerInfo;
	}
}
