using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic.CompilerServices;
using Microsoft.Web.WebView2.WinForms;

namespace LCARSMail;

public class MailHtmlReadPane : UserControl
{
	private WebView2 webView;

	private Label fallbackLabel;

	private bool initStarted;

	private string pendingHtml;

	private string pendingPlain;

	public MailHtmlReadPane()
	{
		BackColor = Color.Black;
		DoubleBuffered = true;
		fallbackLabel = new Label();
		fallbackLabel.Dock = DockStyle.Fill;
		fallbackLabel.ForeColor = Color.Orange;
		fallbackLabel.BackColor = Color.Black;
		fallbackLabel.Font = new Font("Consolas", 9.5f, FontStyle.Regular);
		fallbackLabel.AutoSize = false;
		fallbackLabel.TextAlign = ContentAlignment.TopLeft;
		Controls.Add(fallbackLabel);
		HandleCreated += OnHandleCreatedAsync;
	}

	public void Clear()
	{
		pendingHtml = null;
		pendingPlain = null;
		fallbackLabel.Text = "";
		fallbackLabel.Visible = true;
		if (webView != null)
		{
			webView.Visible = false;
		}
	}

	public void ShowBody(MailMessageBody body)
	{
		ShowBody(body, null);
	}

	public void ShowBody(MailMessageBody body, List<MailAttachmentInfo> attachments)
	{
		if (body == null)
		{
			Clear();
			return;
		}
		string text = ((body.HtmlBody != null) ? body.HtmlBody : "").Trim();
		if (text.Length > 0)
		{
			ShowHtml(ApplyInlineContentIds(text, attachments));
		}
		else
		{
			ShowPlain(body.GetReadingPaneText());
		}
	}

	private static string ApplyInlineContentIds(string html, List<MailAttachmentInfo> attachments)
	{
		if (string.IsNullOrEmpty(html) || attachments == null)
		{
			return html;
		}
		string text = html;
		foreach (MailAttachmentInfo attachment in attachments)
		{
			if (attachment == null || !attachment.IsInline)
			{
				continue;
			}
			MailAttachmentInfo mailAttachmentInfo = attachment;
			string text2 = ((mailAttachmentInfo.ContentId != null) ? mailAttachmentInfo.ContentId : "").Trim();
			if (text2.Length != 0)
			{
				mailAttachmentInfo = attachment;
				string text3 = ((mailAttachmentInfo.CachedPath != null) ? mailAttachmentInfo.CachedPath : "").Trim();
				if (text3.Length != 0 && File.Exists(text3))
				{
					string absoluteUri = new Uri(text3).AbsoluteUri;
					string pattern = "cid:" + Regex.Escape(text2);
					text = Regex.Replace(text, pattern, absoluteUri, RegexOptions.IgnoreCase);
				}
			}
		}
		return text;
	}

	public void ShowPlain(string text)
	{
		pendingHtml = null;
		pendingPlain = ((text != null) ? text : "");
		if (webView != null)
		{
			webView.Visible = false;
		}
		fallbackLabel.Visible = true;
		fallbackLabel.Text = pendingPlain;
	}

	public void ShowHtml(string html)
	{
		pendingPlain = null;
		pendingHtml = WrapHtmlDocument((html != null) ? html : "");
		if (webView != null && webView.CoreWebView2 != null)
		{
			webView.Visible = true;
			fallbackLabel.Visible = false;
			webView.CoreWebView2.NavigateToString(pendingHtml);
		}
		else
		{
			fallbackLabel.Visible = true;
			fallbackLabel.Text = StripTagsLight(pendingHtml);
			EnsureWebViewAsync();
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
			webView.Visible = false;
			Controls.Add(webView);
			webView.BringToFront();
			await webView.EnsureCoreWebView2Async(null);
			if (!string.IsNullOrEmpty(pendingHtml))
			{
				webView.Visible = true;
				fallbackLabel.Visible = false;
				webView.CoreWebView2.NavigateToString(pendingHtml);
			}
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			initStarted = false;
			ProjectData.ClearProjectError();
		}
	}

	private static string WrapHtmlDocument(string innerHtml)
	{
		return "<!DOCTYPE html><html><head><meta charset=\"utf-8\"/><style>body{background:#000;color:#f90;font-family:Segoe UI,Arial,sans-serif;font-size:14px;margin:8px;}a{color:#fc6;}img{max-width:100%;}</style></head><body>" + innerHtml + "</body></html>";
	}

	private static string StripTagsLight(string html)
	{
		if (string.IsNullOrEmpty(html))
		{
			return "";
		}
		return Regex.Replace(html, "<[^>]+>", " ").Trim();
	}
}
