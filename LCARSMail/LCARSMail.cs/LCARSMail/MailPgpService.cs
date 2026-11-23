using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.VisualBasic.CompilerServices;
using MimeKit;
using MimeKit.Cryptography;

namespace LCARSMail;

public sealed class MailPgpService
{
	public static string LastError;

	private MailPgpService()
	{
	}

	public static string GnupgHomeDirectory()
	{
		string text = Path.Combine(AccountStore.DataRoot(), "pgp");
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		return text;
	}

	public static LcarsOpenPgpContext CreateContext()
	{
		return new LcarsOpenPgpContext();
	}

	public static int ImportArmoredKeyFile(string path)
	{
		LastError = null;
		if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
		{
			LastError = "Key file not found.";
			return -1;
		}
		string errorText = null;
		if (TryGpgImport(path, ref errorText))
		{
			return 1;
		}
		if (!string.IsNullOrEmpty(errorText))
		{
			LastError = errorText;
		}
		string errorText2 = null;
		if (TryGpgImportFromStaging(path, ref errorText2))
		{
			return 1;
		}
		LastError = ((errorText2 != null) ? errorText2 : ((LastError != null) ? LastError : "Key import failed."));
		return -1;
	}

	private static bool TryGpgImportFromStaging(string keyPath, ref string errorText)
	{
		errorText = null;
		bool result;
		if (FindGpgExecutable() == null)
		{
			result = false;
		}
		else
		{
			try
			{
				string path = GnupgHomeDirectory();
				string text = Path.Combine(path, "import-staging");
				if (!Directory.Exists(text))
				{
					Directory.CreateDirectory(text);
				}
				string text2 = Path.Combine(text, Path.GetFileName(keyPath));
				File.Copy(keyPath, text2, overwrite: true);
				result = TryGpgImport(text2, ref errorText);
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Exception ex2 = ex;
				errorText = ex2.Message;
				result = false;
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}

	private static bool TryGpgImport(string keyPath, ref string errorText)
	{
		errorText = null;
		string text = FindGpgExecutable();
		if (text == null)
		{
			return false;
		}
		try
		{
			string text2 = GnupgHomeDirectory();
			ProcessStartInfo processStartInfo = new ProcessStartInfo();
			processStartInfo.FileName = text;
			processStartInfo.Arguments = "--homedir \"" + text2 + "\" --batch --yes --import \"" + keyPath + "\"";
			processStartInfo.UseShellExecute = false;
			processStartInfo.CreateNoWindow = true;
			processStartInfo.RedirectStandardOutput = true;
			processStartInfo.RedirectStandardError = true;
			ProcessStartInfo startInfo = processStartInfo;
			using Process process = Process.Start(startInfo);
			process.WaitForExit(15000);
			if (process.ExitCode == 0)
			{
				return true;
			}
			errorText = process.StandardError.ReadToEnd().Trim();
			if (errorText.Length == 0)
			{
				errorText = "gpg import failed (" + process.ExitCode + ")";
			}
		}
		catch (Exception ex)
		{
			ProjectData.SetProjectError(ex);
			Exception ex2 = ex;
			errorText = ex2.Message;
			ProjectData.ClearProjectError();
		}
		return false;
	}

	private static string FindGpgExecutable()
	{
		string[] array = new string[3]
		{
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "GnuPG", "bin", "gpg.exe"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "GnuPG", "bin", "gpg.exe"),
			"gpg.exe"
		};
		string[] array2 = array;
		foreach (string text in array2)
		{
			if (!string.IsNullOrWhiteSpace(text))
			{
				if (Operators.CompareString(text, "gpg.exe", TextCompare: false) == 0)
				{
					return text;
				}
				if (File.Exists(text))
				{
					return text;
				}
			}
		}
		return null;
	}

	public static List<string> ListPublicKeyLabels()
	{
		List<string> list = new List<string>();
		try
		{
			string[] files = Directory.GetFiles(GnupgHomeDirectory(), "*.asc");
			foreach (string path in files)
			{
				list.Add(Path.GetFileName(path));
			}
			string path2 = Path.Combine(GnupgHomeDirectory(), "pubring.gpg");
			if (File.Exists(path2))
			{
				list.Add("pubring.gpg");
			}
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
		return list;
	}

	public static bool TryDecryptMessage(MimeMessage message, string passphrase, ref string plainText)
	{
		string htmlText = null;
		return TryDecryptMessage(message, passphrase, ref plainText, ref htmlText);
	}

	public static bool TryDecryptMessage(MimeMessage message, string passphrase, ref string plainText, ref string htmlText)
	{
		LastError = null;
		plainText = "";
		htmlText = "";
		if (message == null)
		{
			return false;
		}
		try
		{
			using LcarsOpenPgpContext ctx = CreateContext();
			if (message.Body is MultipartEncrypted multipartEncrypted)
			{
				CancellationToken cancellationToken = default(CancellationToken);
				MimeEntity entity = multipartEncrypted.Decrypt(ctx, cancellationToken);
				ExtractBodies(entity, ref plainText, ref htmlText);
				return (plainText.Length > 0 || htmlText.Length > 0) ? true : false;
			}
		}
		catch (Exception ex)
		{
			ProjectData.SetProjectError(ex);
			Exception ex2 = ex;
			LastError = ex2.Message;
			ProjectData.ClearProjectError();
		}
		return false;
	}

	public static bool TryEncryptBody(MailAccount account, string toAddrs, string bodyText, bool sign, bool encrypt, ref MimeEntity bodyEntity)
	{
		LastError = null;
		bodyEntity = new TextPart("plain")
		{
			Text = ((bodyText != null) ? bodyText : "")
		};
		bool result;
		if (!sign && !encrypt)
		{
			result = true;
		}
		else
		{
			try
			{
				using LcarsOpenPgpContext ctx = CreateContext();
				List<MailboxAddress> list = ParseMailboxAddresses(toAddrs);
				if (encrypt && list.Count == 0)
				{
					LastError = "No recipient addresses.";
					result = false;
					goto IL_0100;
				}
				TextPart textPart = new TextPart("plain");
				textPart.Text = ((bodyText != null) ? bodyText : "");
				MimeEntity entity = textPart;
				CancellationToken cancellationToken = default(CancellationToken);
				if (sign && encrypt)
				{
					MailboxAddress signer = ResolveSigner(account);
					bodyEntity = MultipartEncrypted.SignAndEncrypt(ctx, signer, DigestAlgorithm.Sha256, list, entity, cancellationToken);
					result = true;
					goto IL_0100;
				}
				if (encrypt)
				{
					bodyEntity = MultipartEncrypted.Encrypt(ctx, list, entity, cancellationToken);
					result = true;
					goto IL_0100;
				}
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Exception ex2 = ex;
				LastError = ex2.Message;
				result = false;
				ProjectData.ClearProjectError();
				goto IL_0100;
			}
			result = true;
		}
		goto IL_0100;
		IL_0100:
		return result;
	}

	public static bool MessageLooksEncrypted(MimeMessage message)
	{
		if (message == null)
		{
			return false;
		}
		return message.Body is MultipartEncrypted;
	}

	private static MailboxAddress ResolveSigner(MailAccount account)
	{
		string text = ((account != null) ? account.EmailAddress : "");
		string text2 = ((account != null) ? account.DisplayName : "");
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "user@local";
		}
		return new MailboxAddress((text2 != null) ? text2 : "", text.Trim());
	}

	private static List<MailboxAddress> ParseMailboxAddresses(string toAddrs)
	{
		List<MailboxAddress> list = new List<MailboxAddress>();
		string[] array = ((toAddrs != null) ? toAddrs : "").Split(',', ';');
		foreach (string text in array)
		{
			string text2 = text.Trim();
			if (text2.Length == 0)
			{
				continue;
			}
			try
			{
				MailboxAddress mailboxAddress = MailboxAddress.Parse(text2);
				if (mailboxAddress != null)
				{
					list.Add(mailboxAddress);
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}
		return list;
	}

	private static void ExtractBodies(MimeEntity entity, ref string plainText, ref string htmlText)
	{
		if (entity == null)
		{
			return;
		}
		if (entity is TextPart { Text: var text } textPart)
		{
			string text2 = ((text != null) ? text : "");
			if (textPart.IsHtml)
			{
				if (htmlText.Length == 0)
				{
					htmlText = text2;
				}
			}
			else if (plainText.Length == 0)
			{
				plainText = text2;
			}
		}
		else
		{
			if (!(entity is Multipart multipart))
			{
				return;
			}
			foreach (MimeEntity item in multipart)
			{
				ExtractBodies(item, ref plainText, ref htmlText);
			}
		}
	}

	private static string ExtractText(MimeEntity entity)
	{
		string plainText = "";
		string htmlText = "";
		ExtractBodies(entity, ref plainText, ref htmlText);
		if (plainText.Length > 0)
		{
			return plainText;
		}
		return htmlText;
	}
}
