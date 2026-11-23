using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace LCARSMail;

public sealed class ThunderbirdCardDavImporter
{
	private static readonly Regex UserPrefPattern = new Regex("^\\s*user_pref\\s*\\(\\s*\"([^\"]+)\"\\s*,\\s*(.+?)\\s*\\)\\s*;\\s*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

	private ThunderbirdCardDavImporter()
	{
	}

	public static List<ThunderbirdCardDavAccount> TryLoadFromProfile(string profileDir)
	{
		return TryLoadDavServersFromProfile(profileDir, "carddav");
	}

	public static List<ThunderbirdCardDavAccount> TryLoadCalDavFromProfile(string profileDir)
	{
		return TryLoadDavServersFromProfile(profileDir, "caldav");
	}

	private static List<ThunderbirdCardDavAccount> TryLoadDavServersFromProfile(string profileDir, string serverType)
	{
		List<ThunderbirdCardDavAccount> list = new List<ThunderbirdCardDavAccount>();
		if (string.IsNullOrWhiteSpace(profileDir))
		{
			return list;
		}
		string text = Path.Combine(profileDir.Trim(), "prefs.js");
		if (!File.Exists(text))
		{
			return list;
		}
		string text2 = ((serverType != null) ? serverType : "").Trim();
		if (text2.Length == 0)
		{
			return list;
		}
		Dictionary<string, string> dictionary = LoadPrefs(text);
		foreach (string key in dictionary.Keys)
		{
			if (key.StartsWith("mail.server.", StringComparison.Ordinal) && key.EndsWith(".type", StringComparison.Ordinal) && string.Equals(dictionary[key], text2, StringComparison.OrdinalIgnoreCase))
			{
				string text3 = key.Substring("mail.server.".Length);
				text3 = text3.Substring(0, checked(text3.Length - ".type".Length));
				string pref = GetPref(dictionary, "mail.server." + text3 + ".serverURI");
				if (string.IsNullOrWhiteSpace(pref))
				{
					pref = GetPref(dictionary, "mail.server." + text3 + ".hostname");
				}
				string pref2 = GetPref(dictionary, "mail.server." + text3 + ".userName");
				if (!string.IsNullOrWhiteSpace(pref))
				{
					list.Add(new ThunderbirdCardDavAccount
					{
						ServerUrl = pref,
						Username = ((pref2 != null) ? pref2 : "")
					});
				}
			}
		}
		return list;
	}

	private static Dictionary<string, string> LoadPrefs(string prefsJsPath)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
		string[] array = File.ReadAllLines(prefsJsPath, Encoding.UTF8);
		foreach (string text in array)
		{
			Match match = UserPrefPattern.Match(text.Trim());
			if (match.Success)
			{
				dictionary[match.Groups[1].Value] = ParsePrefValue(match.Groups[2].Value.Trim());
			}
		}
		return dictionary;
	}

	private static string ParsePrefValue(string valueRaw)
	{
		if (valueRaw.Length > 0 && valueRaw[0] == '"')
		{
			return valueRaw.Trim('"').Replace("\\\\", "\\");
		}
		return valueRaw;
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
}
