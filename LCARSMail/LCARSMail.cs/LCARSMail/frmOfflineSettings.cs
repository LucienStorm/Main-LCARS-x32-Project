using System;
using System.Drawing;
using System.Windows.Forms;
using LCARS;
using LCARS.Controls;

namespace LCARSMail;

public class frmOfflineSettings : LCARSForm
{
	private TextBox txtBodyDays;

	private TextBox txtHeaderDays;

	private Label lblStatus;

	private readonly string purgeAccountId;

	public frmOfflineSettings()
		: this(null)
	{
	}

	public frmOfflineSettings(string accountId)
	{
		purgeAccountId = ((accountId != null) ? accountId : "").Trim();
		BackColor = Color.Black;
		Size clientSize = new Size(420, 240);
		ClientSize = clientSize;
		StartPosition = FormStartPosition.CenterParent;
		BuildUi();
		txtBodyDays.Text = MailOfflinePolicy.BodyRetentionDays().ToString();
		txtHeaderDays.Text = MailOfflinePolicy.HeaderRetentionDays().ToString();
	}

	private void BuildUi()
	{
		AddLabel("BODY RETENTION (DAYS)", 12, 16);
		txtBodyDays = AddField(12, 36, 380);
		AddLabel("HEADER RETENTION (DAYS)", 12, 68);
		txtHeaderDays = AddField(12, 88, 380);
		FlatButton flatButton = new FlatButton();
		flatButton.ButtonText = "SAVE";
		flatButton.Text = "SAVE";
		flatButton.Color = LCARScolorStyles.PrimaryFunction;
		Size size = new Size(90, 28);
		flatButton.Size = size;
		Point location = new Point(12, 128);
		flatButton.Location = location;
		flatButton.Click += OnSave;
		Controls.Add(flatButton);
		FlatButton flatButton2 = new FlatButton();
		flatButton2.ButtonText = "PURGE NOW";
		flatButton2.Text = "PURGE NOW";
		flatButton2.Color = LCARScolorStyles.SystemFunction;
		size = new Size(110, 28);
		flatButton2.Size = size;
		location = new Point(112, 128);
		flatButton2.Location = location;
		flatButton2.Click += OnPurgeNow;
		Controls.Add(flatButton2);
		lblStatus = new Label();
		lblStatus.ForeColor = Color.Orange;
		Label label = lblStatus;
		location = new Point(12, 200);
		label.Location = location;
		Label label2 = lblStatus;
		size = new Size(380, 24);
		label2.Size = size;
		Controls.Add(lblStatus);
	}

	private void OnSave(object sender, EventArgs e)
	{
		int result = 0;
		int result2 = 0;
		if (!int.TryParse(txtBodyDays.Text.Trim(), out result))
		{
			result = 90;
		}
		if (!int.TryParse(txtHeaderDays.Text.Trim(), out result2))
		{
			result2 = 365;
		}
		MailOfflinePolicy.SavePolicy(result, result2);
		lblStatus.Text = "POLICY SAVED — BODY " + result + "D / HDR " + result2 + "D";
	}

	private void OnPurgeNow(object sender, EventArgs e)
	{
		OnSave(null, EventArgs.Empty);
		if (purgeAccountId.Length == 0)
		{
			lblStatus.Text = "OPEN CACHE FROM MAIL WITH ACCOUNT";
			return;
		}
		MailOfflinePolicy.ApplyBodyRetention(purgeAccountId);
		MailOfflinePolicy.ApplyHeaderRetention(purgeAccountId);
		lblStatus.Text = "RETENTION PURGE APPLIED FOR ACCOUNT";
	}

	private void AddLabel(string text, int x, int y)
	{
		Label label = new Label();
		label.Text = text;
		label.ForeColor = Color.Orange;
		label.AutoSize = true;
		Point location = new Point(x, y);
		label.Location = location;
		Controls.Add(label);
	}

	private TextBox AddField(int x, int y, int w)
	{
		TextBox textBox = new TextBox();
		Point location = new Point(x, y);
		textBox.Location = location;
		Size size = new Size(w, 24);
		textBox.Size = size;
		textBox.BackColor = Color.FromArgb(40, 40, 40);
		textBox.ForeColor = Color.Orange;
		Controls.Add(textBox);
		return textBox;
	}
}
