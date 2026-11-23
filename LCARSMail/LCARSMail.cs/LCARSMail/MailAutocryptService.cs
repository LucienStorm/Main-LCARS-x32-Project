using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.CompilerServices;
using MimeKit;

namespace LCARSMail;

public sealed class MailAutocryptService
{
	public static string LastError;

	private static readonly Regex AutocryptHeaderPattern = new Regex("Autocrypt:\\s*([^;\\r\\n]+(?:;\\s*[^;\\r\\n]+)*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private MailAutocryptService()
	{
	}

	public static string TryFetchWkdKeyArmored(string email)
	{
		LastError = null;
		string text = ((email != null) ? email : "").Trim();
		if (text.Length == 0 || !text.Contains("@"))
		{
			LastError = "Email required for WKD.";
			return "";
		}
		int num = text.LastIndexOf('@');
		string text2 = text.Substring(0, num);
		string text3 = text.Substring(checked(num + 1));
		string text4 = WkdHashLocalPart(text2);
		string requestUriString = "https://openpgpkey." + text3 + "/.well-known/openpgpkey/" + text3 + "/hu/" + text4 + "?l=" + Uri.EscapeDataString(text2);
		try
		{
			HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(requestUriString);
			httpWebRequest.Method = "GET";
			httpWebRequest.Timeout = 20000;
			using HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
			using StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8);
			string text5 = streamReader.ReadToEnd();
			if (text5.IndexOf("BEGIN PGP PUBLIC KEY BLOCK", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return text5;
			}
		}
		catch (Exception ex)
		{
			ProjectData.SetProjectError(ex);
			Exception ex2 = ex;
			LastError = ex2.Message;
			ProjectData.ClearProjectError();
		}
		return "";
	}

	public static bool TryImportWkdForEmail(string email)
	{
		string text = TryFetchWkdKeyArmored(email);
		if (text.Length == 0)
		{
			return false;
		}
		string path = Path.Combine(Path.GetTempPath(), "wkd-" + Guid.NewGuid().ToString("N") + ".asc");
		try
		{
			File.WriteAllText(path, text, Encoding.UTF8);
			return MailPgpService.ImportArmoredKeyFile(path) > 0;
		}
		finally
		{
			try
			{
				if (File.Exists(path))
				{
					File.Delete(path);
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}
	}

	public static string BuildOutgoingAutocryptHeader(MailAccount account)
	{
		if (account == null)
		{
			return "";
		}
		string text = ((account.EmailAddress != null) ? account.EmailAddress : "").Trim();
		if (text.Length == 0)
		{
			return "";
		}
		string text2 = FindFirstPublicKeyFile();
		if (text2 == null)
		{
			return "";
		}
		string armored = File.ReadAllText(text2, Encoding.UTF8);
		string text3 = ExtractBase64Key(armored);
		if (text3.Length == 0)
		{
			return "";
		}
		return "addr=" + text + "; keydata=" + text3;
	}

	public static void ApplyAutocryptFromMessage(MimeMessage message)
	{
		if (message == null || message.Headers == null)
		{
			return;
		}
		string text = message.Headers["Autocrypt"];
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		string text2 = ExtractAutocryptParam(text, "keydata");
		if (text2.Length == 0)
		{
			return;
		}
		try
		{
			byte[] bytes = Convert.FromBase64String(text2);
			string text3 = Encoding.UTF8.GetString(bytes);
			if (text3.IndexOf("BEGIN PGP", StringComparison.OrdinalIgnoreCase) < 0)
			{
				text3 = "-----BEGIN PGP PUBLIC KEY BLOCK-----\r\n" + text2 + "\r\n-----END PGP PUBLIC KEY BLOCK-----";
			}
			string path = Path.Combine(Path.GetTempPath(), "autocrypt-" + Guid.NewGuid().ToString("N") + ".asc");
			File.WriteAllText(path, text3, Encoding.UTF8);
			MailPgpService.ImportArmoredKeyFile(path);
			try
			{
				File.Delete(path);
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}
		catch (Exception projectError2)
		{
			ProjectData.SetProjectError(projectError2);
			ProjectData.ClearProjectError();
		}
	}

	private static string ExtractAutocryptParam(string header, string name)
	{
		string[] array = header.Split(';');
		string[] array2 = array;
		foreach (string text in array2)
		{
			string text2 = text.Trim();
			if (text2.StartsWith(name + "=", StringComparison.OrdinalIgnoreCase))
			{
				return text2.Substring(checked(name.Length + 1)).Trim();
			}
		}
		return "";
	}

	private static string ExtractBase64Key(string armored)
	{
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = false;
		string[] array = armored.Split(new string[2] { "\r\n", "\n" }, StringSplitOptions.None);
		foreach (string text in array)
		{
			string text2 = text.Trim();
			if (text2.StartsWith("-----BEGIN", StringComparison.Ordinal))
			{
				flag = true;
				continue;
			}
			if (text2.StartsWith("-----END", StringComparison.Ordinal))
			{
				break;
			}
			if (flag && text2.Length > 0)
			{
				stringBuilder.Append(text2);
			}
		}
		return stringBuilder.ToString();
	}

	private static string FindFirstPublicKeyFile()
	{
		try
		{
			string[] files = Directory.GetFiles(MailPgpService.GnupgHomeDirectory(), "*.asc");
			int num = 0;
			if (num < files.Length)
			{
				return files[num];
			}
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
		return null;
	}

	private static string WkdHashLocalPart(string localPart)
	{
		using SHA1 sHA = SHA1.Create();
		byte[] array = sHA.ComputeHash(Encoding.UTF8.GetBytes(localPart));
		StringBuilder stringBuilder = new StringBuilder(checked(array.Length * 2));
		byte[] array2 = array;
		foreach (byte b in array2)
		{
			stringBuilder.Append(b.ToString("x2"));
		}
		return stringBuilder.ToString();
	}
}
