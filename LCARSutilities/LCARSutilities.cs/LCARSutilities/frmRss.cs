using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using LCARS;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSutilities;

public class frmRss : LCARSForm
{
	private const int FormW = 820;

	private const int FormH = 580;

	private ListBox lstFeeds;

	private ListBox lstItems;

	private TextBox txtArticle;

	private Label lblStatus;

	private List<string> feedUrls;

	private List<RssItem> currentItems;

	public frmRss()
	{
		modUtilities.InitUtilityFormFloating(this, 820, 580);
		feedUrls = RssFeedStore.LoadFeedUrls();
		currentItems = new List<RssItem>();
		BuildUi();
		RefreshFeedList();
	}

	private void BuildUi()
	{
		modUtilities.MakeUtilityTitle(this, "RSS", 400);
		modUtilities.MakeUtilityButton(this, "ADD FEED", 398, 12, 110, 28, LCARScolorStyles.PrimaryFunction, OnAddFeed);
		modUtilities.MakeUtilityButton(this, "REFRESH", 518, 12, 100, 28, LCARScolorStyles.SystemFunction, OnRefreshFeed);
		modUtilities.MakeUtilityButton(this, "CLOSE", 688, 12, 110, 28, LCARScolorStyles.Orange, [SpecialName] (object s, EventArgs e) =>
		{
			Close();
		});
		lstFeeds = new ListBox();
		ListBox listBox = lstFeeds;
		Point location = new Point(12, 48);
		listBox.Location = location;
		ListBox listBox2 = lstFeeds;
		Size size = new Size(240, 480);
		listBox2.Size = size;
		lstFeeds.BackColor = Color.FromArgb(30, 30, 30);
		lstFeeds.ForeColor = Color.Orange;
		lstFeeds.SelectedIndexChanged += OnFeedSelected;
		Controls.Add(lstFeeds);
		lstItems = new ListBox();
		ListBox listBox3 = lstItems;
		location = new Point(260, 48);
		listBox3.Location = location;
		ListBox listBox4 = lstItems;
		size = new Size(260, 480);
		listBox4.Size = size;
		lstItems.BackColor = Color.FromArgb(30, 30, 30);
		lstItems.ForeColor = Color.Orange;
		lstItems.SelectedIndexChanged += OnItemSelected;
		Controls.Add(lstItems);
		txtArticle = new TextBox();
		txtArticle.Multiline = true;
		txtArticle.ReadOnly = true;
		txtArticle.ScrollBars = ScrollBars.Vertical;
		TextBox textBox = txtArticle;
		location = new Point(530, 48);
		textBox.Location = location;
		TextBox textBox2 = txtArticle;
		size = new Size(278, 480);
		textBox2.Size = size;
		txtArticle.BackColor = Color.FromArgb(20, 20, 20);
		txtArticle.ForeColor = Color.Orange;
		Controls.Add(txtArticle);
		lblStatus = modUtilities.MakeUtilityLabel(this, 12, 540, 796, 20, 10f);
	}

	private void RefreshFeedList()
	{
		lstFeeds.Items.Clear();
		foreach (string feedUrl in feedUrls)
		{
			lstFeeds.Items.Add(feedUrl);
		}
		lblStatus.Text = feedUrls.Count + " FEED(S)";
	}

	private void OnAddFeed(object sender, EventArgs e)
	{
		string text = Interaction.InputBox("Feed URL (RSS or Atom):", "LCARS RSS", "https://");
		if (!string.IsNullOrWhiteSpace(text))
		{
			feedUrls.Add(text.Trim());
			RssFeedStore.SaveFeedUrls(feedUrls);
			RefreshFeedList();
		}
	}

	private void OnFeedSelected(object sender, EventArgs e)
	{
		if (lstFeeds.SelectedIndex >= 0)
		{
			OnRefreshFeed(RuntimeHelpers.GetObjectValue(sender), e);
		}
	}

	private void OnRefreshFeed(object sender, EventArgs e)
	{
		if (lstFeeds.SelectedIndex < 0 && feedUrls.Count > 0)
		{
			lstFeeds.SelectedIndex = 0;
		}
		if (lstFeeds.SelectedIndex < 0)
		{
			return;
		}
		try
		{
			lblStatus.Text = "FETCHING…";
			Application.DoEvents();
			string xml = RssFeedStore.FetchFeedXml(feedUrls[lstFeeds.SelectedIndex]);
			currentItems = RssFeedParser.Parse(xml);
			lstItems.Items.Clear();
			foreach (RssItem currentItem in currentItems)
			{
				ListBox.ObjectCollection items = lstItems.Items;
				RssItem rssItem = currentItem;
				items.Add((rssItem.Title != null) ? rssItem.Title : "(untitled)");
			}
			lblStatus.Text = currentItems.Count + " ARTICLE(S)";
		}
		catch (Exception ex)
		{
			ProjectData.SetProjectError(ex);
			Exception ex2 = ex;
			lblStatus.Text = ex2.Message.ToUpperInvariant();
			ProjectData.ClearProjectError();
		}
	}

	private void OnItemSelected(object sender, EventArgs e)
	{
		if (lstItems.SelectedIndex >= 0 && lstItems.SelectedIndex < currentItems.Count)
		{
			RssItem rssItem = currentItems[lstItems.SelectedIndex];
			txtArticle.Text = rssItem.Title + "\r\n\r\n" + rssItem.Description + "\r\n\r\n" + rssItem.Link;
		}
	}
}
