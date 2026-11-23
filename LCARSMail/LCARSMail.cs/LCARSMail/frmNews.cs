using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using LCARS;
using LCARS.Controls;

namespace LCARSMail;

public class frmNews : LCARSForm
{
	private TextBox txtHost;

	private ListBox lstGroups;

	private ListBox lstArticles;

	private TextBox txtArticle;

	private TextBox txtPostSubject;

	private TextBox txtPostBody;

	private Label lblStatus;

	private string currentGroup;

	private List<NntpArticleHeader> articleHeaders;

	public frmNews()
	{
		BackColor = Color.Black;
		Size clientSize = new Size(800, 560);
		ClientSize = clientSize;
		StartPosition = FormStartPosition.CenterParent;
		articleHeaders = new List<NntpArticleHeader>();
		BuildUi();
		string text = NntpSubscriptionStore.LoadLastHost();
		if (text.Length > 0)
		{
			txtHost.Text = text;
		}
		OnListSubscriptions(null, EventArgs.Empty);
	}

	private void BuildUi()
	{
		FlatButton flatButton = new FlatButton();
		flatButton.ButtonText = "NEWS";
		flatButton.Text = "NEWS";
		flatButton.Clickable = false;
		flatButton.Color = LCARScolorStyles.StaticTan;
		Point location = new Point(12, 8);
		flatButton.Location = location;
		Size size = new Size(80, 32);
		flatButton.Size = size;
		Controls.Add(flatButton);
		txtHost = new TextBox();
		TextBox textBox = txtHost;
		location = new Point(100, 12);
		textBox.Location = location;
		TextBox textBox2 = txtHost;
		size = new Size(280, 24);
		textBox2.Size = size;
		txtHost.Text = "news.example.com";
		txtHost.BackColor = Color.FromArgb(40, 40, 40);
		txtHost.ForeColor = Color.Orange;
		Controls.Add(txtHost);
		FlatButton flatButton2 = new FlatButton();
		flatButton2.ButtonText = "GROUPS";
		flatButton2.Text = "GROUPS";
		flatButton2.Color = LCARScolorStyles.PrimaryFunction;
		size = new Size(90, 28);
		flatButton2.Size = size;
		location = new Point(390, 10);
		flatButton2.Location = location;
		flatButton2.Click += OnListGroups;
		Controls.Add(flatButton2);
		FlatButton flatButton3 = new FlatButton();
		flatButton3.ButtonText = "SUBS";
		flatButton3.Text = "SUBS";
		flatButton3.Color = LCARScolorStyles.SystemFunction;
		size = new Size(72, 28);
		flatButton3.Size = size;
		location = new Point(488, 10);
		flatButton3.Location = location;
		flatButton3.Click += OnListSubscriptions;
		Controls.Add(flatButton3);
		FlatButton flatButton4 = new FlatButton();
		flatButton4.ButtonText = "SUB";
		flatButton4.Text = "SUB";
		flatButton4.Color = LCARScolorStyles.MiscFunction;
		size = new Size(64, 28);
		flatButton4.Size = size;
		location = new Point(566, 10);
		flatButton4.Location = location;
		flatButton4.Click += OnSubscribeCurrent;
		Controls.Add(flatButton4);
		FlatButton flatButton5 = new FlatButton();
		flatButton5.ButtonText = "UNSUB";
		flatButton5.Text = "UNSUB";
		flatButton5.Color = LCARScolorStyles.Orange;
		size = new Size(72, 28);
		flatButton5.Size = size;
		location = new Point(636, 10);
		flatButton5.Location = location;
		flatButton5.Click += OnUnsubscribeCurrent;
		Controls.Add(flatButton5);
		lstGroups = new ListBox();
		ListBox listBox = lstGroups;
		location = new Point(12, 48);
		listBox.Location = location;
		ListBox listBox2 = lstGroups;
		size = new Size(240, 200);
		listBox2.Size = size;
		lstGroups.BackColor = Color.FromArgb(30, 30, 30);
		lstGroups.ForeColor = Color.Orange;
		lstGroups.SelectedIndexChanged += OnGroupSelected;
		Controls.Add(lstGroups);
		lstArticles = new ListBox();
		ListBox listBox3 = lstArticles;
		location = new Point(260, 48);
		listBox3.Location = location;
		ListBox listBox4 = lstArticles;
		size = new Size(528, 200);
		listBox4.Size = size;
		lstArticles.BackColor = Color.FromArgb(30, 30, 30);
		lstArticles.ForeColor = Color.Orange;
		lstArticles.SelectedIndexChanged += OnArticleSelected;
		Controls.Add(lstArticles);
		txtArticle = new TextBox();
		txtArticle.Multiline = true;
		txtArticle.ScrollBars = ScrollBars.Vertical;
		TextBox textBox3 = txtArticle;
		location = new Point(12, 256);
		textBox3.Location = location;
		TextBox textBox4 = txtArticle;
		size = new Size(776, 200);
		textBox4.Size = size;
		txtArticle.BackColor = Color.FromArgb(30, 30, 30);
		txtArticle.ForeColor = Color.Orange;
		Controls.Add(txtArticle);
		txtPostSubject = new TextBox();
		TextBox textBox5 = txtPostSubject;
		location = new Point(12, 464);
		textBox5.Location = location;
		TextBox textBox6 = txtPostSubject;
		size = new Size(776, 24);
		textBox6.Size = size;
		txtPostSubject.BackColor = Color.FromArgb(40, 40, 40);
		txtPostSubject.ForeColor = Color.Orange;
		Controls.Add(txtPostSubject);
		txtPostBody = new TextBox();
		TextBox textBox7 = txtPostBody;
		location = new Point(12, 492);
		textBox7.Location = location;
		TextBox textBox8 = txtPostBody;
		size = new Size(580, 24);
		textBox8.Size = size;
		txtPostBody.BackColor = Color.FromArgb(40, 40, 40);
		txtPostBody.ForeColor = Color.Orange;
		Controls.Add(txtPostBody);
		FlatButton flatButton6 = new FlatButton();
		flatButton6.ButtonText = "POST";
		flatButton6.Text = "POST";
		flatButton6.Color = LCARScolorStyles.SystemFunction;
		size = new Size(90, 28);
		flatButton6.Size = size;
		location = new Point(600, 490);
		flatButton6.Location = location;
		flatButton6.Click += OnPost;
		Controls.Add(flatButton6);
		lblStatus = new Label();
		lblStatus.ForeColor = Color.Orange;
		Label label = lblStatus;
		location = new Point(12, 528);
		label.Location = location;
		Label label2 = lblStatus;
		size = new Size(776, 24);
		label2.Size = size;
		Controls.Add(lblStatus);
	}

	private void OnListGroups(object sender, EventArgs e)
	{
		lstGroups.Items.Clear();
		lstArticles.Items.Clear();
		txtArticle.Clear();
		NntpSubscriptionStore.SaveLastHost(txtHost.Text.Trim());
		List<string> list = NntpNewsService.TryListGroups(txtHost.Text.Trim(), 119);
		if (!string.IsNullOrEmpty(NntpNewsService.LastError))
		{
			lblStatus.Text = NntpNewsService.LastError.ToUpperInvariant();
			return;
		}
		foreach (string item in list)
		{
			lstGroups.Items.Add(item);
		}
		lblStatus.Text = list.Count + " GROUP(S)";
	}

	private void OnGroupSelected(object sender, EventArgs e)
	{
		if (lstGroups.SelectedItem == null)
		{
			return;
		}
		currentGroup = lstGroups.SelectedItem.ToString();
		lstArticles.Items.Clear();
		articleHeaders = NntpNewsService.TryListArticles(txtHost.Text.Trim(), 119, currentGroup);
		if (articleHeaders == null)
		{
			lblStatus.Text = ((NntpNewsService.LastError != null) ? NntpNewsService.LastError : "ARTICLES FAILED").ToUpperInvariant();
			return;
		}
		int lastArticleNumber = NntpSubscriptionStore.GetLastArticleNumber(currentGroup);
		int num = lastArticleNumber;
		foreach (NntpArticleHeader articleHeader in articleHeaders)
		{
			if (articleHeader != null)
			{
				string text = "";
				if (articleHeader.ArticleNumber > lastArticleNumber)
				{
					text = "* ";
				}
				ListBox.ObjectCollection items = lstArticles.Items;
				string[] array = new string[5]
				{
					text,
					"#",
					articleHeader.ArticleNumber.ToString(),
					" ",
					null
				};
				NntpArticleHeader nntpArticleHeader = articleHeader;
				array[4] = ((nntpArticleHeader.Subject != null) ? nntpArticleHeader.Subject : "");
				items.Add(string.Concat(array));
				if (articleHeader.ArticleNumber > num)
				{
					num = articleHeader.ArticleNumber;
				}
			}
		}
		if (num > lastArticleNumber)
		{
			lblStatus.Text = articleHeaders.Count + " ARTICLE(S) — " + checked(num - lastArticleNumber) + " NEW";
		}
	}

	private void OnArticleSelected(object sender, EventArgs e)
	{
		int selectedIndex = lstArticles.SelectedIndex;
		if (selectedIndex >= 0 && selectedIndex < articleHeaders.Count)
		{
			int articleNumber = articleHeaders[selectedIndex].ArticleNumber;
			txtArticle.Text = NntpNewsService.TryFetchArticle(txtHost.Text.Trim(), 119, currentGroup, articleNumber);
			NntpSubscriptionStore.SetLastArticleNumber(currentGroup, articleNumber);
		}
	}

	private void OnListSubscriptions(object sender, EventArgs e)
	{
		lstGroups.Items.Clear();
		lstArticles.Items.Clear();
		txtArticle.Clear();
		foreach (string item in NntpSubscriptionStore.LoadGroups())
		{
			lstGroups.Items.Add(item);
		}
		lblStatus.Text = lstGroups.Items.Count + " SUBSCRIPTION(S)";
	}

	private void OnSubscribeCurrent(object sender, EventArgs e)
	{
		if (lstGroups.SelectedItem == null)
		{
			lblStatus.Text = "SELECT A GROUP";
			return;
		}
		NntpSubscriptionStore.Subscribe(lstGroups.SelectedItem.ToString());
		lblStatus.Text = "SUBSCRIBED";
	}

	private void OnUnsubscribeCurrent(object sender, EventArgs e)
	{
		if (lstGroups.SelectedItem == null)
		{
			lblStatus.Text = "SELECT A GROUP";
			return;
		}
		NntpSubscriptionStore.Unsubscribe(lstGroups.SelectedItem.ToString());
		OnListSubscriptions(null, EventArgs.Empty);
	}

	private void OnPost(object sender, EventArgs e)
	{
		if (string.IsNullOrWhiteSpace(currentGroup))
		{
			lblStatus.Text = "SELECT A GROUP";
		}
		else if (NntpNewsService.TryPostArticle(txtHost.Text.Trim(), 119, "lcars@local", txtPostSubject.Text, currentGroup, txtPostBody.Text))
		{
			lblStatus.Text = "POSTED";
			OnGroupSelected(null, EventArgs.Empty);
		}
		else
		{
			lblStatus.Text = ((NntpNewsService.LastError != null) ? NntpNewsService.LastError : "POST FAILED").ToUpperInvariant();
		}
	}
}
