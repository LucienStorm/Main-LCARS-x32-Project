using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualBasic.CompilerServices;
using MimeKit.Cryptography;

namespace LCARSMail;

[StandardModule]
internal sealed class Program
{
	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool AllocConsole();

	[STAThread]
	public static void Main()
	{
		if (!TryRunParseFixture(Environment.GetCommandLineArgs()))
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(defaultValue: false);
			CryptographyContext.Register([SpecialName] () => new LcarsOpenPgpContext());
			string text = ParseModeArg(Environment.GetCommandLineArgs());
			if (string.IsNullOrWhiteSpace(text))
			{
				text = InferModeFromExeName();
			}
			Application.Run(new frmMail(text));
		}
	}

	private static string ParseModeArg(string[] args)
	{
		if (args == null)
		{
			return null;
		}
		checked
		{
			int num = args.Length - 1;
			for (int i = 1; i <= num; i++)
			{
				string[] array = args;
				string[] array2 = array;
				int num2 = i;
				string text = ((array2[num2] != null) ? array[num2] : "").Trim();
				if (text.StartsWith("--mode=", StringComparison.OrdinalIgnoreCase))
				{
					return text.Substring("--mode=".Length).Trim();
				}
				if (string.Equals(text, "--mode", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
				{
					array = args;
					string[] array3 = array;
					num2 = i + 1;
					return ((array3[num2] != null) ? array[num2] : "").Trim();
				}
				if (!text.StartsWith("-", StringComparison.Ordinal))
				{
					string text2 = text.ToLowerInvariant();
					switch (text2)
					{
					case "mail":
					case "chat":
					case "rss":
					case "calendar":
					case "cal":
						return text2;
					}
				}
			}
			return null;
		}
	}

	private static string InferModeFromExeName()
	{
		try
		{
			string text = Path.GetFileNameWithoutExtension(Application.ExecutablePath).ToLowerInvariant();
			if (text.IndexOf("chat", StringComparison.Ordinal) >= 0)
			{
				return "chat";
			}
			if (text.IndexOf("rss", StringComparison.Ordinal) >= 0)
			{
				return "rss";
			}
			if (text.IndexOf("calendar", StringComparison.Ordinal) >= 0)
			{
				return "calendar";
			}
			if (text.EndsWith("cal", StringComparison.Ordinal))
			{
				return "calendar";
			}
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
		return null;
	}

	private static bool TryRunParseFixture(string[] args)
	{
		if (args == null || args.Length < 2)
		{
			return false;
		}
		string text = null;
		checked
		{
			int num = args.Length - 1;
			for (int i = 1; i <= num; i++)
			{
				if (string.Equals(args[i], "--parse-fixture", StringComparison.OrdinalIgnoreCase))
				{
					text = ((i + 1 >= args.Length || args[i + 1].StartsWith("-", StringComparison.Ordinal)) ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Testdata", "sample-prefs.js") : args[i + 1]);
					break;
				}
			}
			if (string.IsNullOrWhiteSpace(text))
			{
				return false;
			}
			AllocConsole();
			try
			{
				List<MailAccount> list = ThunderbirdPrefsParser.ParsePrefsJs(text);
				if (!string.IsNullOrEmpty(ThunderbirdPrefsParser.LastError))
				{
					Console.WriteLine("Parse error: {0}", ThunderbirdPrefsParser.LastError);
					Environment.Exit(1);
				}
				Console.WriteLine("Parsed {0} IMAP account(s) from {1}", list.Count, text);
				foreach (MailAccount item in list)
				{
					Console.WriteLine("  {0}: {1} @ {2}:{3} ssl={4} startTls={5} user={6}", item.Id, item.EmailAddress, item.ImapHost, item.ImapPort, item.ImapSsl, item.ImapStartTls, item.Username);
				}
				Environment.Exit((list.Count < 1) ? 1 : 0);
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Exception ex2 = ex;
				Console.WriteLine("Parse error: {0}", ex2.Message);
				Environment.Exit(1);
				ProjectData.ClearProjectError();
			}
			return true;
		}
	}
}
