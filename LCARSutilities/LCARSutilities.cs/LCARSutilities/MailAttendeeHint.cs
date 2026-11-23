using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSutilities;

[StandardModule]
public sealed class MailAttendeeHint
{
	private class AccountEmailDto
	{
		public string EmailAddress;

		public string Username;

		[DebuggerNonUserCode]
		public AccountEmailDto()
		{
		}
	}

	public static string TryGetPrimaryAttendeeMailto()
	{
		string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LCARS x32", "Mail", "accounts.json");
		if (!File.Exists(path))
		{
			return "";
		}
		try
		{
			JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
			List<AccountEmailDto> list = javaScriptSerializer.Deserialize<List<AccountEmailDto>>(File.ReadAllText(path, Encoding.UTF8));
			if (list == null)
			{
				return "";
			}
			foreach (AccountEmailDto item in list)
			{
				if (item == null)
				{
					continue;
				}
				AccountEmailDto accountEmailDto = item;
				string text = ((accountEmailDto.EmailAddress != null) ? accountEmailDto.EmailAddress : "").Trim();
				if (text.Length == 0)
				{
					accountEmailDto = item;
					text = ((accountEmailDto.Username != null) ? accountEmailDto.Username : "").Trim();
				}
				if (text.Length == 0)
				{
					continue;
				}
				if (text.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
				{
					return text;
				}
				return "mailto:" + text;
			}
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
		return "";
	}

	public static bool TryLaunchOrganizerRsvpMail(string organizerLine, string eventSummary, string partStat)
	{
		string text = NormalizeOrganizerSmtp(organizerLine);
		bool result;
		if (text.Length == 0)
		{
			result = false;
		}
		else
		{
			string text2 = ((partStat != null) ? partStat : "").Trim().ToUpperInvariant();
			if (text2.Length == 0)
			{
				result = false;
			}
			else
			{
				string text3 = Uri.EscapeDataString("RE: " + ((eventSummary != null) ? eventSummary : "Meeting").Trim() + " — " + text2);
				string text4 = Uri.EscapeDataString("PARTSTAT=" + text2);
				try
				{
					Process.Start("mailto:" + text + "?subject=" + text3 + "&body=" + text4);
					result = true;
				}
				catch (Exception projectError)
				{
					ProjectData.SetProjectError(projectError);
					result = false;
					ProjectData.ClearProjectError();
				}
			}
		}
		return result;
	}

	private static string NormalizeOrganizerSmtp(string organizerLine)
	{
		string text = ((organizerLine != null) ? organizerLine : "").Trim();
		if (text.Length == 0)
		{
			return "";
		}
		int num = text.IndexOf("mailto:", StringComparison.OrdinalIgnoreCase);
		if (num >= 0)
		{
			text = text.Substring(checked(num + 7));
		}
		int num2 = text.IndexOf('@');
		if (num2 <= 0)
		{
			return "";
		}
		int num3 = text.IndexOf(';');
		if (num3 > 0)
		{
			text = text.Substring(0, num3);
		}
		return text.Trim();
	}
}
