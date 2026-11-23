using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.VisualBasic.CompilerServices;
using MimeKit;

namespace LCARSMail;

public sealed class MailAddressHelper
{
	private MailAddressHelper()
	{
	}

	public static string FormatDisplayLine(string raw)
	{
		string text = ((raw != null) ? raw : "").Trim();
		if (text.Length == 0)
		{
			return "";
		}
		try
		{
			InternetAddressList internetAddressList = InternetAddressList.Parse(text);
			List<string> list = new List<string>();
			foreach (InternetAddress item in internetAddressList)
			{
				list.Add(FormatOne(item));
			}
			if (list.Count > 0)
			{
				return string.Join("; ", list.ToArray());
			}
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
		return text;
	}

	public static string FormatOne(InternetAddress addr)
	{
		if (addr == null)
		{
			return "";
		}
		string name;
		if (addr is GroupAddress groupAddress)
		{
			List<string> list = new List<string>();
			foreach (InternetAddress member in groupAddress.Members)
			{
				list.Add(FormatOne(member));
			}
			name = groupAddress.Name;
			string text = ((name != null) ? name : "").Trim();
			if (text.Length == 0)
			{
				text = "group";
			}
			return text + ": " + string.Join(", ", list.ToArray()) + ";";
		}
		if (!(addr is MailboxAddress mailboxAddress))
		{
			return addr.ToString();
		}
		string text2 = DecodeEmailForDisplay(mailboxAddress.Address);
		name = mailboxAddress.Name;
		string text3 = ((name != null) ? name : "").Trim();
		if (text3.Length == 0)
		{
			return text2;
		}
		return text3 + " <" + text2 + ">";
	}

	public static List<string> SplitAddressTokens(string raw)
	{
		List<string> list = new List<string>();
		string text = ((raw != null) ? raw : "").Trim();
		if (text.Length == 0)
		{
			return list;
		}
		try
		{
			InternetAddressList internetAddressList = InternetAddressList.Parse(text);
			foreach (InternetAddress item in internetAddressList)
			{
				if (item is MailboxAddress mailboxAddress)
				{
					list.Add(mailboxAddress.ToString());
				}
				else
				{
					if (!(item is GroupAddress groupAddress))
					{
						continue;
					}
					foreach (InternetAddress member in groupAddress.Members)
					{
						if (member is MailboxAddress mailboxAddress2)
						{
							list.Add(mailboxAddress2.ToString());
						}
					}
				}
			}
			return list;
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
		string[] array = text.Split(',');
		foreach (string text2 in array)
		{
			string text3 = text2.Trim();
			if (text3.Length > 0)
			{
				list.Add(text3);
			}
		}
		return list;
	}

	public static string NormalizeForTransport(string raw)
	{
		string text = ((raw != null) ? raw : "").Trim();
		string result;
		if (text.Length == 0)
		{
			result = "";
		}
		else
		{
			try
			{
				InternetAddressList internetAddressList = InternetAddressList.Parse(text);
				InternetAddressList internetAddressList2 = new InternetAddressList();
				foreach (InternetAddress item in internetAddressList)
				{
					internetAddressList2.Add(EncodeAddress(item));
				}
				result = internetAddressList2.ToString();
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = EncodeEmailIdn(text);
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}

	private static InternetAddress EncodeAddress(InternetAddress addr)
	{
		if (addr is MailboxAddress { Name: var name } mailboxAddress)
		{
			return new MailboxAddress((name != null) ? name : "", EncodeEmailIdn(mailboxAddress.Address));
		}
		if (addr is GroupAddress groupAddress)
		{
			InternetAddressList internetAddressList = new InternetAddressList();
			foreach (InternetAddress member in groupAddress.Members)
			{
				internetAddressList.Add(EncodeAddress(member));
			}
			string name2 = groupAddress.Name;
			return new GroupAddress((name2 != null) ? name2 : "", internetAddressList);
		}
		return addr;
	}

	public static string EncodeEmailIdn(string email)
	{
		string text = ((email != null) ? email : "").Trim();
		if (text.Length == 0 || !text.Contains("@"))
		{
			return text;
		}
		int num = text.LastIndexOf('@');
		string text2 = text.Substring(0, num);
		string text3 = text.Substring(checked(num + 1));
		try
		{
			IdnMapping idnMapping = new IdnMapping();
			text3 = idnMapping.GetAscii(text3);
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
		return text2 + "@" + text3;
	}

	public static string DecodeEmailForDisplay(string email)
	{
		string text = ((email != null) ? email : "").Trim();
		if (text.Length == 0 || !text.Contains("@"))
		{
			return text;
		}
		int num = text.LastIndexOf('@');
		string text2 = text.Substring(0, num);
		string text3 = text.Substring(checked(num + 1));
		try
		{
			IdnMapping idnMapping = new IdnMapping();
			text3 = idnMapping.GetUnicode(text3);
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
		return text2 + "@" + text3;
	}
}
