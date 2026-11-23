using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

public sealed class ThunderbirdPrefsParser
{
	private static readonly Regex UserPrefPattern = new Regex("^\\s*user_pref\\s*\\(\\s*\"([^\"]+)\"\\s*,\\s*(.+?)\\s*\\)\\s*;\\s*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

	public static string LastError;

	private ThunderbirdPrefsParser()
	{
	}

	public static List<MailAccount> ParsePrefsJs(string prefsJsPath)
	{
		LastError = null;
		List<MailAccount> result;
		try
		{
			if (string.IsNullOrWhiteSpace(prefsJsPath))
			{
				LastError = "prefsJsPath is required.";
				result = new List<MailAccount>();
			}
			else if (!File.Exists(prefsJsPath))
			{
				LastError = "prefs.js not found: " + prefsJsPath;
				result = new List<MailAccount>();
			}
			else
			{
				Dictionary<string, string> dictionary = LoadPrefs(prefsJsPath);
				List<MailAccount> list = new List<MailAccount>();
				string value = null;
				if (!dictionary.TryGetValue("mail.accountmanager.accounts", out value))
				{
					result = list;
				}
				else
				{
					string[] array = SplitCsv(value);
					foreach (string text in array)
					{
						if (string.IsNullOrWhiteSpace(text))
						{
							continue;
						}
						string pref = GetPref(dictionary, "mail.account." + text + ".server");
						if (string.IsNullOrWhiteSpace(pref))
						{
							continue;
						}
						string pref2 = GetPref(dictionary, "mail.server." + pref + ".type");
						if (!string.Equals(pref2, "imap", StringComparison.OrdinalIgnoreCase))
						{
							continue;
						}
						string text2 = FirstCsvToken(GetPref(dictionary, "mail.account." + text + ".identities"));
						string text3 = "";
						string text4 = "";
						if (!string.IsNullOrWhiteSpace(text2))
						{
							text3 = GetPref(dictionary, "mail.identity." + text2 + ".useremail");
							text4 = GetPref(dictionary, "mail.identity." + text2 + ".fullname");
						}
						string text5 = GetPref(dictionary, "mail.server." + pref + ".userName");
						if (string.IsNullOrWhiteSpace(text5))
						{
							text5 = text3;
						}
						string pref3 = GetPref(dictionary, "mail.server." + pref + ".hostname");
						if (string.IsNullOrWhiteSpace(pref3) || string.IsNullOrWhiteSpace(text5))
						{
							continue;
						}
						string pref4 = GetPref(dictionary, "mail.server." + pref + ".socketType");
						bool imapSsl = false;
						bool imapStartTls = false;
						ParseSocketType(pref4, ref imapSsl, ref imapStartTls);
						int imapPort = ParsePort(GetPref(dictionary, "mail.server." + pref + ".port"), imapSsl, imapStartTls);
						string smtpHost = "";
						int smtpPort = 587;
						bool imapSsl2 = false;
						bool imapStartTls2 = false;
						string text6 = "";
						if (!string.IsNullOrWhiteSpace(text2))
						{
							string pref5 = GetPref(dictionary, "mail.identity." + text2 + ".smtpServer");
							if (!string.IsNullOrWhiteSpace(pref5))
							{
								string pref6 = GetPref(dictionary, "mail.smtpserver." + pref5 + ".hostname");
								smtpHost = ((pref6 != null) ? pref6 : "");
								text6 = GetPref(dictionary, "mail.smtpserver." + pref5 + ".username");
								if (string.IsNullOrWhiteSpace(text6))
								{
									text6 = ((text3 != null) ? text3 : "");
								}
								string pref7 = GetPref(dictionary, "mail.smtpserver." + pref5 + ".socketType");
								ParseSocketType(pref7, ref imapSsl2, ref imapStartTls2);
								smtpPort = ParseSmtpPort(GetPref(dictionary, "mail.smtpserver." + pref5 + ".port"), imapSsl2, imapStartTls2);
							}
						}
						bool strictTls = !string.Equals(GetPref(dictionary, "mail.server." + pref + ".allowUntrustedCerts"), "true", StringComparison.OrdinalIgnoreCase);
						string pref8 = GetPref(dictionary, "mail.server." + pref + ".authMethod");
						string authMethod = "password";
						if (MailOAuthProviders.IsThunderbirdOAuthAuthMethod(pref8) || MailOAuthProviders.TryResolveByHostname(pref3) != null)
						{
							authMethod = "oauth2";
						}
						MailAccount mailAccount = new MailAccount();
						mailAccount.Id = text;
						mailAccount.DisplayName = ((text4 != null) ? text4 : "");
						mailAccount.EmailAddress = ((text3 != null) ? text3 : "");
						mailAccount.ImapHost = pref3;
						mailAccount.ImapPort = imapPort;
						mailAccount.ImapSsl = imapSsl;
						mailAccount.ImapStartTls = imapStartTls;
						mailAccount.Username = text5;
						mailAccount.SmtpHost = smtpHost;
						mailAccount.SmtpPort = smtpPort;
						mailAccount.SmtpSsl = imapSsl2;
						mailAccount.SmtpStartTls = imapStartTls2;
						mailAccount.SmtpUsername = text6;
						mailAccount.StrictTls = strictTls;
						mailAccount.AuthMethod = authMethod;
						MailAccount item = mailAccount;
						list.Add(item);
					}
					result = list;
				}
			}
		}
		catch (Exception ex)
		{
			ProjectData.SetProjectError(ex);
			Exception ex2 = ex;
			LastError = ex2.Message;
			result = new List<MailAccount>();
			ProjectData.ClearProjectError();
		}
		return result;
	}

	private static Dictionary<string, string> LoadPrefs(string prefsJsPath)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
		try
		{
			string[] array = File.ReadAllLines(prefsJsPath, Encoding.UTF8);
			foreach (string text in array)
			{
				if (string.IsNullOrWhiteSpace(text))
				{
					continue;
				}
				string text2 = text.Trim();
				if (!text2.StartsWith("//", StringComparison.Ordinal))
				{
					Match match = UserPrefPattern.Match(text2);
					if (match.Success)
					{
						string value = match.Groups[1].Value;
						string valueRaw = match.Groups[2].Value.Trim();
						dictionary[value] = ParsePrefValue(valueRaw);
					}
				}
			}
		}
		catch (Exception ex)
		{
			ProjectData.SetProjectError(ex);
			Exception ex2 = ex;
			throw new IOException("Failed to read prefs.js: " + ex2.Message, ex2);
		}
		return dictionary;
	}

	private static string ParsePrefValue(string valueRaw)
	{
		if (valueRaw == null || valueRaw.Length == 0)
		{
			return "";
		}
		checked
		{
			if (valueRaw[0] == '"')
			{
				StringBuilder stringBuilder = new StringBuilder();
				int num = 1;
				while (num < valueRaw.Length)
				{
					char c = valueRaw[num];
					if (c == '\\' && num + 1 < valueRaw.Length)
					{
						stringBuilder.Append(valueRaw[num + 1]);
						num += 2;
						continue;
					}
					if (c == '"')
					{
						break;
					}
					stringBuilder.Append(c);
					num++;
				}
				return stringBuilder.ToString();
			}
			if (string.Equals(valueRaw, "true", StringComparison.OrdinalIgnoreCase))
			{
				return "true";
			}
			if (string.Equals(valueRaw, "false", StringComparison.OrdinalIgnoreCase))
			{
				return "false";
			}
			return valueRaw;
		}
	}

	private static string GetPref(Dictionary<string, string> prefs, string key)
	{
		string value = null;
		if (prefs.TryGetValue(key, out value))
		{
			return value;
		}
		return null;
	}

	private static string[] SplitCsv(string raw)
	{
		if (string.IsNullOrWhiteSpace(raw))
		{
			return new string[0];
		}
		return raw.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
	}

	private static string FirstCsvToken(string raw)
	{
		string[] array = SplitCsv((raw != null) ? raw : "");
		if (array.Length == 0)
		{
			return null;
		}
		return array[0].Trim();
	}

	private static void ParseSocketType(string socketTypeRaw, ref bool imapSsl, ref bool imapStartTls)
	{
		imapSsl = false;
		imapStartTls = false;
		if (int.TryParse((socketTypeRaw != null) ? socketTypeRaw : "", out var result))
		{
			switch (result)
			{
			case 1:
			case 3:
				imapSsl = true;
				break;
			case 2:
				imapSsl = true;
				imapStartTls = true;
				break;
			}
		}
	}

	private static int ParsePort(string portRaw, bool imapSsl, bool imapStartTls)
	{
		if (int.TryParse((portRaw != null) ? portRaw : "", out var result) && result > 0)
		{
			return result;
		}
		if (imapStartTls)
		{
			return 143;
		}
		return imapSsl ? 993 : 143;
	}

	private static int ParseSmtpPort(string portRaw, bool smtpSsl, bool smtpStartTls)
	{
		if (int.TryParse((portRaw != null) ? portRaw : "", out var result) && result > 0)
		{
			return result;
		}
		if (smtpStartTls)
		{
			return 587;
		}
		return smtpSsl ? 465 : 587;
	}
}
