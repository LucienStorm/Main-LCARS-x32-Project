using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using LCARS;
using LCARS.Controls;

namespace LCARSMail;

public class frmRssFeeds : LCARSForm
{
	private TextBox txtFeedUrl;

	private ListBox lstItems;

	private TextBox txtBody;

	private Label lblStatus;

	private List<RssFeedItem> currentItems;

	private ListBox lstFeeds;

	public frmRssFeeds()
	{
		BackColor = Color.Black;
		Size clientSize = new Size(720, 560);
		ClientSize = clientSize;
		StartPosition = FormStartPosition.CenterParent;
		currentItems = new List<RssFeedItem>();
		BuildUi();
		foreach (string item in RssSubscriptionStore.LoadFeedUrls())
		{
			lstFeeds.Items.Add(item);
		}
	}

	private void BuildUi()
	{
		txtFeedUrl = new TextBox();
		TextBox textBox = txtFeedUrl;
		Point location = new Point(12, 12);
		textBox.Location = location;
		TextBox textBox2 = txtFeedUrl;
		Size size = new Size(520, 24);
		textBox2.Size = size;
		txtFeedUrl.BackColor = Color.FromArgb(40, 40, 40);
		txtFeedUrl.ForeColor = Color.Orange;
		Controls.Add(txtFeedUrl);
		FlatButton flatButton = new FlatButton();
		flatButton.ButtonText = "LOAD";
		flatButton.Text = "LOAD";
		flatButton.Color = LCARScolorStyles.PrimaryFunction;
		size = new Size(80, 28);
		flatButton.Size = size;
		location = new Point(540, 10);
		flatButton.Location = location;
		flatButton.Click += OnLoadFeed;
		Controls.Add(flatButton);
		FlatButton flatButton2 = new FlatButton();
		flatButton2.ButtonText = "SUB";
		flatButton2.Text = "SUB";
		flatButton2.Color = LCARScolorStyles.MiscFunction;
		size = new Size(56, 28);
		flatButton2.Size = size;
		location = new Point(628, 10);
		flatButton2.Location = location;
		flatButton2.Click += OnSubscribeFeed;
		Controls.Add(flatButton2);
		lstFeeds = new ListBox();
		ListBox listBox = lstFeeds;
		location = new Point(12, 44);
		listBox.Location = location;
		ListBox listBox2 = lstFeeds;
		size = new Size(696, 72);
		listBox2.Size = size;
		lstFeeds.BackColor = Color.FromArgb(25, 25, 25);
		lstFeeds.ForeColor = Color.Orange;
		lstFeeds.SelectedIndexChanged += OnSavedFeedSelected;
		Controls.Add(lstFeeds);
		FlatButton flatButton3 = new FlatButton();
		flatButton3.ButtonText = "PLAY";
		flatButton3.Text = "PLAY";
		flatButton3.Color = LCARScolorStyles.SystemFunction;
		size = new Size(80, 28);
		flatButton3.Size = size;
		location = new Point(692, 10);
		flatButton3.Location = location;
		flatButton3.Click += OnPlayEnclosure;
		Controls.Add(flatButton3);
		lstItems = new ListBox();
		ListBox listBox3 = lstItems;
		location = new Point(12, 124);
		listBox3.Location = location;
		ListBox listBox4 = lstItems;
		size = new Size(696, 160);
		listBox4.Size = size;
		lstItems.BackColor = Color.FromArgb(30, 30, 30);
		lstItems.ForeColor = Color.Orange;
		lstItems.SelectedIndexChanged += OnItemSelected;
		Controls.Add(lstItems);
		txtBody = new TextBox();
		txtBody.Multiline = true;
		txtBody.ScrollBars = ScrollBars.Vertical;
		TextBox textBox3 = txtBody;
		location = new Point(12, 292);
		textBox3.Location = location;
		TextBox textBox4 = txtBody;
		size = new Size(696, 220);
		textBox4.Size = size;
		txtBody.BackColor = Color.FromArgb(30, 30, 30);
		txtBody.ForeColor = Color.Orange;
		Controls.Add(txtBody);
		lblStatus = new Label();
		lblStatus.ForeColor = Color.Orange;
		Label label = lblStatus;
		location = new Point(12, 528);
		label.Location = location;
		Label label2 = lblStatus;
		size = new Size(696, 24);
		label2.Size = size;
		Controls.Add(lblStatus);
	}

	private void OnSubscribeFeed(object sender, EventArgs e)
	{
		string text = txtFeedUrl.Text.Trim();
		if (text.Length != 0)
		{
			RssSubscriptionStore.Subscribe(text);
			if (!lstFeeds.Items.Contains(text))
			{
				lstFeeds.Items.Add(text);
			}
			lblStatus.Text = "FEED SUBSCRIBED";
		}
	}

	private void OnSavedFeedSelected(object sender, EventArgs e)
	{
		if (lstFeeds.SelectedItem != null)
		{
			txtFeedUrl.Text = lstFeeds.SelectedItem.ToString();
			OnLoadFeed(null, EventArgs.Empty);
		}
	}

	private void OnLoadFeed(object sender, EventArgs e)
	{
		lstItems.Items.Clear();
		string feedUrl = txtFeedUrl.Text.Trim();
		currentItems = RssFeedService.FetchItems(feedUrl);
		if (currentItems == null)
		{
			lblStatus.Text = ((RssFeedService.LastError != null) ? RssFeedService.LastError : "LOAD FAILED").ToUpperInvariant();
			return;
		}
		RssSubscriptionStore.Subscribe(feedUrl);
		foreach (RssFeedItem currentItem in currentItems)
		{
			string text = "";
			if (!string.IsNullOrWhiteSpace(currentItem.EnclosureUrl))
			{
				text = "[MEDIA] ";
			}
			ListBox.ObjectCollection items = lstItems.Items;
			string obj = text;
			RssFeedItem rssFeedItem = currentItem;
			items.Add(obj + ((rssFeedItem.Title != null) ? rssFeedItem.Title : currentItem.Link));
		}
		lblStatus.Text = currentItems.Count + " ITEM(S)";
	}

	private void OnItemSelected(object sender, EventArgs e)
	{
		int selectedIndex = lstItems.SelectedIndex;
		if (selectedIndex >= 0 && selectedIndex < currentItems.Count)
		{
			RssFeedItem rssFeedItem = currentItems[selectedIndex];
			TextBox textBox = txtBody;
			RssFeedItem rssFeedItem2 = rssFeedItem;
			textBox.Text = ((rssFeedItem2.Description != null) ? rssFeedItem2.Description : "");
			if (!string.IsNullOrWhiteSpace(rssFeedItem.EnclosureUrl))
			{
				lblStatus.Text = "ENCLOSURE READY — TAP PLAY";
			}
		}
	}

	private void OnPlayEnclosure(object sender, EventArgs e)
	{
		int selectedIndex = lstItems.SelectedIndex;
		if (selectedIndex >= 0 && selectedIndex < currentItems.Count)
		{
			RssFeedService.PlayEnclosure(currentItems[selectedIndex]);
			if (!string.IsNullOrEmpty(RssFeedService.LastError))
			{
				lblStatus.Text = RssFeedService.LastError.ToUpperInvariant();
			}
		}
	}
}
