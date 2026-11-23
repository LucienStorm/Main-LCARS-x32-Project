using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSutilities;

[StandardModule]
internal sealed class Program
{
	[STAThread]
	public static void Main(string[] args)
	{
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(defaultValue: false);
		Application.ThreadException += OnThreadException;
		AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
		try
		{
			string text = ResolveMode(args);
			Form form = null;
			switch (text)
			{
			case "timer":
				form = new frmTimer();
				break;
			case "screenshot":
				form = new frmScreenshot();
				break;
			case "weather":
				form = new frmWeather();
				break;
			case "calendar":
			case "cal":
				form = new frmCalendar();
				break;
			case "rss":
			case "feed":
			case "feeds":
				form = new frmRss();
				break;
			case "chat":
			case "irc":
				form = new frmChat();
				break;
			default:
				form = new frmCalculator();
				break;
			}
			Application.Run(form);
		}
		catch (Exception ex)
		{
			ProjectData.SetProjectError(ex);
			Exception ex2 = ex;
			MessageBox.Show("LCARS utility failed to start:\r\n\r\n" + ex2.ToString(), "LCARS Utilities", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			ProjectData.ClearProjectError();
		}
	}

	private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
	{
		MessageBox.Show("LCARS utility error:\r\n\r\n" + e.Exception.ToString(), "LCARS Utilities", MessageBoxButtons.OK, MessageBoxIcon.Hand);
	}

	private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		string text = ((e.ExceptionObject is Exception ex) ? ex.ToString() : e.ExceptionObject.ToString());
		MessageBox.Show("LCARS utility fatal error:\r\n\r\n" + text, "LCARS Utilities", MessageBoxButtons.OK, MessageBoxIcon.Hand);
	}

	private static string ResolveMode(string[] args)
	{
		if (args != null && args.Length > 0 && !string.IsNullOrEmpty(args[0]))
		{
			return NormalizeMode(args[0]);
		}
		string text = Path.GetFileNameWithoutExtension(Application.ExecutablePath);
		if (text == null)
		{
			text = "";
		}
		text = text.Trim();
		if (text.StartsWith("LCARS", StringComparison.OrdinalIgnoreCase) && text.Length > 5)
		{
			return NormalizeMode(text.Substring(5));
		}
		return NormalizeMode(text);
	}

	private static string NormalizeMode(string raw)
	{
		string text = ((raw != null) ? raw : "").Trim().ToLowerInvariant();
		if (text.StartsWith("/") || text.StartsWith("-"))
		{
			text = text.Substring(1);
		}
		switch (text)
		{
		case "timer":
		case "stopwatch":
			return "timer";
		case "screenshot":
		case "screen":
		case "capture":
			return "screenshot";
		case "weather":
		case "wx":
			return "weather";
		case "calendar":
		case "cal":
			return "calendar";
		case "rss":
		case "feed":
		case "feeds":
			return "rss";
		case "chat":
		case "irc":
			return "chat";
		default:
			return "calculator";
		}
	}
}
