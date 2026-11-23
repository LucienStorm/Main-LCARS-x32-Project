using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using LCARS;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

public sealed class LcarsCommsUi
{
	private LcarsCommsUi()
	{
	}

	public static MsgBoxResult Alert(string prompt, string title = "LCARS COMMS", MsgBoxStyle buttons = MsgBoxStyle.OkOnly)
	{
		return UI.MsgBox((prompt != null) ? prompt : "", buttons, (title != null) ? title : "LCARS COMMS");
	}

	public static bool Confirm(string prompt, string title = "LCARS COMMS")
	{
		return Alert(prompt, title, MsgBoxStyle.YesNo | MsgBoxStyle.Critical) == MsgBoxResult.Yes;
	}

	public static string Prompt(string promptText, string title = "LCARS COMMS", string defaultResponse = "")
	{
		return UI.inputbox((promptText != null) ? promptText : "", (title != null) ? title : "LCARS COMMS", (defaultResponse != null) ? defaultResponse : "");
	}

	public static string PromptPassword(IWin32Window owner, string promptText, string title = "LCARS COMMS — PASSWORD")
	{
		return frmPasswordPrompt.Prompt(owner, (promptText != null) ? promptText : "", (title != null) ? title : "LCARS COMMS — PASSWORD");
	}

	public static string PickFolder(IWin32Window owner, string title = "SELECT FOLDER", string startPath = null)
	{
		return frmFolderSelect.Pick(owner, title, startPath);
	}

	public static bool OpenInLcarsBrowser(string url)
	{
		string text = ((url != null) ? url : "").Trim();
		bool result;
		if (text.Length == 0)
		{
			Alert("No URL to open.", "LCARS COMMS — ERROR");
			result = false;
		}
		else
		{
			if (!text.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !text.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
			{
				text = "https://" + text;
			}
			string text2 = ResolveLcarsBrowserExe();
			if (string.IsNullOrEmpty(text2))
			{
				Alert("LCARS Web Browser was not found next to Comms.\r\nExpected LCARSWebBrowser.exe in the LCARS install folder.", "LCARS COMMS — ERROR");
				result = false;
			}
			else
			{
				try
				{
					ProcessStartInfo processStartInfo = new ProcessStartInfo();
					processStartInfo.FileName = text2;
					processStartInfo.Arguments = "\"" + text + "\"";
					processStartInfo.WorkingDirectory = Path.GetDirectoryName(text2);
					processStartInfo.UseShellExecute = true;
					Process.Start(processStartInfo);
					result = true;
				}
				catch (Exception ex)
				{
					ProjectData.SetProjectError(ex);
					Exception ex2 = ex;
					Alert("Could not start LCARS Web Browser.\r\n\r\n" + ex2.Message, "LCARS COMMS — ERROR");
					result = false;
					ProjectData.ClearProjectError();
				}
			}
		}
		return result;
	}

	private static string ResolveLcarsBrowserExe()
	{
		List<string> list = new List<string>();
		list.Add(Path.Combine(Application.StartupPath, "LCARSWebBrowser.exe"));
		try
		{
			string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			if (!string.IsNullOrEmpty(directoryName))
			{
				list.Add(Path.Combine(directoryName, "LCARSWebBrowser.exe"));
			}
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
		try
		{
			string setting = Interaction.GetSetting("LCARS x32", "Application", "InstallPath");
			if (!string.IsNullOrEmpty(setting))
			{
				list.Add(Path.Combine(setting, "LCARSWebBrowser.exe"));
			}
		}
		catch (Exception projectError2)
		{
			ProjectData.SetProjectError(projectError2);
			ProjectData.ClearProjectError();
		}
		foreach (string item in list)
		{
			if (!string.IsNullOrEmpty(item) && File.Exists(item))
			{
				return item;
			}
		}
		return null;
	}
}
