using System;
using System.IO;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

public sealed class MailOfflinePolicy
{
	private const string PolicyFileName = "cache-policy.txt";

	private const int DefaultBodyRetentionDays = 90;

	private const int DefaultHeaderRetentionDays = 365;

	private MailOfflinePolicy()
	{
	}

	public static int BodyRetentionDays()
	{
		return ReadPolicyInt("body", 90);
	}

	public static int HeaderRetentionDays()
	{
		return ReadPolicyInt("header", 365);
	}

	public static void SavePolicy(int bodyDays, int headerDays)
	{
		if (bodyDays <= 0)
		{
			bodyDays = 90;
		}
		if (headerDays <= 0)
		{
			headerDays = 365;
		}
		string path = Path.Combine(AccountStore.DataRoot(), "cache-policy.txt");
		File.WriteAllText(path, "body=" + bodyDays + "\r\nheader=" + headerDays);
	}

	private static int ReadPolicyInt(string key, int defaultValue)
	{
		string path = Path.Combine(AccountStore.DataRoot(), "cache-policy.txt");
		if (!File.Exists(path))
		{
			return defaultValue;
		}
		try
		{
			string[] array = File.ReadAllLines(path);
			foreach (string text in array)
			{
				string text2 = ((text != null) ? text : "").Trim();
				if (text2.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
				{
					int result = 0;
					if (int.TryParse(text2.Substring(checked(key.Length + 1)), out result) && result > 0)
					{
						return result;
					}
				}
				if (Operators.CompareString(key, "body", TextCompare: false) == 0 && !text2.Contains("="))
				{
					int result2 = 0;
					if (int.TryParse(text2, out result2) && result2 > 0)
					{
						return result2;
					}
				}
			}
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
		return defaultValue;
	}

	public static void ApplyHeaderRetention(string accountId)
	{
		if (string.IsNullOrWhiteSpace(accountId))
		{
			return;
		}
		string path = Path.Combine(AccountStore.DataRoot(), "cache", accountId);
		if (!Directory.Exists(path))
		{
			return;
		}
		DateTime t = DateTime.UtcNow.AddDays(checked(-HeaderRetentionDays()));
		string[] files = Directory.GetFiles(path, "headers.json", SearchOption.AllDirectories);
		foreach (string path2 in files)
		{
			try
			{
				if (DateTime.Compare(File.GetLastWriteTimeUtc(path2), t) < 0)
				{
					File.Delete(path2);
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}
	}

	public static void ApplyBodyRetention(string accountId)
	{
		if (string.IsNullOrWhiteSpace(accountId))
		{
			return;
		}
		string path = Path.Combine(AccountStore.DataRoot(), "cache", accountId);
		if (!Directory.Exists(path))
		{
			return;
		}
		DateTime t = DateTime.UtcNow.AddDays(checked(-BodyRetentionDays()));
		string[] files = Directory.GetFiles(path, "*.body.json", SearchOption.AllDirectories);
		foreach (string path2 in files)
		{
			try
			{
				if (DateTime.Compare(File.GetLastWriteTimeUtc(path2), t) < 0)
				{
					File.Delete(path2);
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}
	}
}
