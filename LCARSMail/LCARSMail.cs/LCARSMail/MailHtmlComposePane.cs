using System;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic.CompilerServices;
using Microsoft.Web.WebView2.WinForms;

namespace LCARSMail;

public class MailHtmlComposePane : UserControl
{
	private Panel toolPanel;

	private WebView2 webView;

	private TextBox fallbackBox;

	private bool initStarted;

	private bool useFallback;

	public string PlainText
	{
		get
		{
			if (useFallback || webView == null || webView.CoreWebView2 == null)
			{
				string text = fallbackBox.Text;
				return (text != null) ? text : "";
			}
			return GetPlainTextSync();
		}
		set
		{
			string text = ((value != null) ? value : "");
			if (useFallback || webView == null || webView.CoreWebView2 == null)
			{
				fallbackBox.Text = text;
			}
			else
			{
				SetDesignModeHtml("<p>" + EscapeHtml(text).Replace("\r\n", "<br/>") + "</p>");
			}
		}
	}

	public string HtmlBody
	{
		get
		{
			if (useFallback || webView == null || webView.CoreWebView2 == null)
			{
				return "<p>" + EscapeHtml(fallbackBox.Text).Replace("\r\n", "<br/>") + "</p>";
			}
			return GetInnerHtmlSync();
		}
	}

	public bool UsesRichEditor => (!useFallback && webView != null && webView.CoreWebView2 != null) ? true : false;

	public MailHtmlComposePane()
	{
		BackColor = Color.Black;
		DoubleBuffered = true;
		toolPanel = new Panel();
		toolPanel.Dock = DockStyle.Top;
		toolPanel.Height = 32;
		toolPanel.BackColor = Color.FromArgb(30, 30, 30);
		BuildToolbar();
		Controls.Add(toolPanel);
		fallbackBox = new TextBox();
		fallbackBox.Multiline = true;
		fallbackBox.Dock = DockStyle.Fill;
		fallbackBox.BackColor = Color.FromArgb(40, 40, 40);
		fallbackBox.ForeColor = Color.Orange;
		fallbackBox.BorderStyle = BorderStyle.FixedSingle;
		fallbackBox.Font = new Font("Consolas", 9.5f, FontStyle.Regular);
		fallbackBox.AcceptsReturn = true;
		fallbackBox.AcceptsTab = true;
		fallbackBox.Visible = true;
		Controls.Add(fallbackBox);
		HandleCreated += OnHandleCreatedAsync;
	}

	private void BuildToolbar()
	{
		int num = 4;
		AddToolButton("B", num, OnBoldClick);
		checked
		{
			num += 36;
			AddToolButton("I", num, OnItalicClick);
			num += 36;
			AddToolButton("U", num, OnUnderlineClick);
			num += 36;
			AddToolButton("LINK", num, OnLinkClick);
			num += 56;
			AddToolButton("IMG", num, OnImageClick);
		}
	}

	private void AddToolButton(string text, int x, EventHandler handler)
	{
		Button button = new Button();
		button.Text = text;
		button.FlatStyle = FlatStyle.Flat;
		button.ForeColor = Color.Orange;
		button.BackColor = Color.FromArgb(50, 50, 50);
		Size size = new Size((text.Length > 2) ? 52 : 32, 24);
		button.Size = size;
		Point location = new Point(x, 4);
		button.Location = location;
		button.Click += handler;
		toolPanel.Controls.Add(button);
	}

	public void SetHtmlContent(string html)
	{
		string text = ((html != null) ? html : "");
		if (useFallback || webView == null || webView.CoreWebView2 == null)
		{
			fallbackBox.Text = StripTagsLight(text);
		}
		else
		{
			SetDesignModeHtml(text);
		}
	}

	private void OnBoldClick(object sender, EventArgs e)
	{
		RunEditorCommand("bold");
	}

	private void OnItalicClick(object sender, EventArgs e)
	{
		RunEditorCommand("italic");
	}

	private void OnUnderlineClick(object sender, EventArgs e)
	{
		RunEditorCommand("underline");
	}

	private void OnLinkClick(object sender, EventArgs e)
	{
		string text = LcarsCommsUi.Prompt("Link URL:", "LCARS COMMS — INSERT LINK", "https://");
		if (!string.IsNullOrWhiteSpace(text))
		{
			RunEditorCommand("createLink", text.Trim());
		}
	}

	private void OnImageClick(object sender, EventArgs e)
	{
		using OpenFileDialog openFileDialog = new OpenFileDialog();
		openFileDialog.Filter = "Images|*.png;*.jpg;*.jpeg;*.gif;*.bmp|All|*.*";
		if (openFileDialog.ShowDialog() != DialogResult.OK)
		{
			return;
		}
		try
		{
			byte[] inArray = File.ReadAllBytes(openFileDialog.FileName);
			string text = Path.GetExtension(openFileDialog.FileName).Trim('.').ToLowerInvariant();
			if (text.Length == 0)
			{
				text = "png";
			}
			string text2 = Convert.ToBase64String(inArray);
			string text3 = "<img src=\"data:image/" + text + ";base64," + text2 + "\" alt=\"inline\"/>";
			RunEditorCommand("insertHTML", text3.Replace("'", "\\'"));
		}
		catch (Exception ex)
		{
			ProjectData.SetProjectError(ex);
			Exception ex2 = ex;
			LcarsCommsUi.Alert(ex2.Message, "LCARS COMMS — IMAGE ERROR");
			ProjectData.ClearProjectError();
		}
	}

	private void RunEditorCommand(string command, string value = null)
	{
		if (webView == null || webView.CoreWebView2 == null)
		{
			return;
		}
		try
		{
			if (value == null)
			{
				webView.CoreWebView2.ExecuteScriptAsync("document.execCommand('" + command + "', false, null);");
				return;
			}
			string text = value.Replace("\\", "\\\\").Replace("'", "\\'");
			webView.CoreWebView2.ExecuteScriptAsync("document.execCommand('" + command + "', false, '" + text + "');");
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
	}

	private async void OnHandleCreatedAsync(object sender, EventArgs e)
	{
		await EnsureWebViewAsync();
	}

	private async Task EnsureWebViewAsync()
	{
		if (initStarted || !IsHandleCreated)
		{
			return;
		}
		initStarted = true;
		try
		{
			webView = new WebView2();
			webView.Dock = DockStyle.Fill;
			webView.DefaultBackgroundColor = Color.Black;
			Controls.Add(webView);
			webView.BringToFront();
			toolPanel.BringToFront();
			await webView.EnsureCoreWebView2Async(null);
			webView.CoreWebView2.NavigateToString(BuildEditorShell(""));
			fallbackBox.Visible = false;
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			useFallback = true;
			initStarted = false;
			ProjectData.ClearProjectError();
		}
	}

	private void SetDesignModeHtml(string innerHtml)
	{
		if (webView != null && webView.CoreWebView2 != null)
		{
			webView.CoreWebView2.NavigateToString(BuildEditorShell(innerHtml));
		}
	}

	private static string BuildEditorShell(string innerHtml)
	{
		return "<!DOCTYPE html><html><head><meta charset=\"utf-8\"/><style>body{background:#1a1a1a;color:#f90;font-family:Segoe UI,Arial,sans-serif;font-size:14px;margin:8px;}</style></head><body contenteditable=\"true\" spellcheck=\"true\">" + innerHtml + "</body></html>";
	}

	private string GetInnerHtmlSync()
	{
		string result;
		try
		{
			Task<string> task = webView.CoreWebView2.ExecuteScriptAsync("document.body.innerHTML");
			task.Wait(3000);
			result = UnquoteJsonString(task.Result);
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			result = fallbackBox.Text;
			ProjectData.ClearProjectError();
		}
		return result;
	}

	private string GetPlainTextSync()
	{
		string result;
		try
		{
			Task<string> task = webView.CoreWebView2.ExecuteScriptAsync("document.body.innerText");
			task.Wait(3000);
			result = UnquoteJsonString(task.Result);
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			result = fallbackBox.Text;
			ProjectData.ClearProjectError();
		}
		return result;
	}

	private static string UnquoteJsonString(string jsonQuoted)
	{
		if (string.IsNullOrEmpty(jsonQuoted))
		{
			return "";
		}
		string text = jsonQuoted.Trim();
		if (text.StartsWith("\"", StringComparison.Ordinal) && text.EndsWith("\"", StringComparison.Ordinal) && text.Length >= 2)
		{
			text = text.Substring(1, checked(text.Length - 2));
			text = text.Replace("\\n", "\n").Replace("\\r", "").Replace("\\\"", "\"")
				.Replace("\\\\", "\\");
		}
		return text;
	}

	private static string StripTagsLight(string html)
	{
		if (string.IsNullOrEmpty(html))
		{
			return "";
		}
		return Regex.Replace(html, "<[^>]+>", " ").Trim();
	}

	private static string EscapeHtml(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return "";
		}
		return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
	}
}
