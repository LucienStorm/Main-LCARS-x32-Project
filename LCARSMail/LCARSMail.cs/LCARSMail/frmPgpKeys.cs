using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using LCARS;
using LCARS.Controls;

namespace LCARSMail;

public class frmPgpKeys : LCARSForm
{
	private ListBox lstKeys;

	private Label lblStatus;

	public frmPgpKeys()
	{
		Size clientSize = new Size(640, 400);
		ClientSize = clientSize;
		StartPosition = FormStartPosition.CenterParent;
		BackColor = Color.Black;
		BuildUi();
		RefreshKeys();
	}

	private void BuildUi()
	{
		FlatButton flatButton = new FlatButton();
		flatButton.ButtonText = "PGP KEYS";
		flatButton.Text = "PGP KEYS";
		flatButton.Clickable = false;
		flatButton.Color = LCARScolorStyles.StaticTan;
		Size size = new Size(120, 32);
		flatButton.Size = size;
		Point location = new Point(12, 8);
		flatButton.Location = location;
		Controls.Add(flatButton);
		FlatButton flatButton2 = new FlatButton();
		flatButton2.ButtonText = "IMPORT";
		flatButton2.Text = "IMPORT";
		flatButton2.Color = LCARScolorStyles.PrimaryFunction;
		size = new Size(100, 28);
		flatButton2.Size = size;
		location = new Point(140, 10);
		flatButton2.Location = location;
		flatButton2.Click += OnImport;
		Controls.Add(flatButton2);
		FlatButton flatButton3 = new FlatButton();
		flatButton3.ButtonText = "WKD";
		flatButton3.Text = "WKD";
		flatButton3.Color = LCARScolorStyles.SystemFunction;
		size = new Size(72, 28);
		flatButton3.Size = size;
		location = new Point(248, 10);
		flatButton3.Location = location;
		flatButton3.Click += OnWkdLookup;
		Controls.Add(flatButton3);
		FlatButton flatButton4 = new FlatButton();
		flatButton4.ButtonText = "CLOSE";
		flatButton4.Text = "CLOSE";
		flatButton4.Color = LCARScolorStyles.Orange;
		size = new Size(100, 28);
		flatButton4.Size = size;
		location = new Point(520, 10);
		flatButton4.Location = location;
		flatButton4.Click += [SpecialName] (object s, EventArgs e) =>
		{
			Close();
		};
		Controls.Add(flatButton4);
		lstKeys = new ListBox();
		ListBox listBox = lstKeys;
		location = new Point(12, 48);
		listBox.Location = location;
		ListBox listBox2 = lstKeys;
		size = new Size(616, 300);
		listBox2.Size = size;
		lstKeys.BackColor = Color.FromArgb(30, 30, 30);
		lstKeys.ForeColor = Color.Orange;
		Controls.Add(lstKeys);
		lblStatus = new Label();
		lblStatus.ForeColor = Color.Orange;
		Label label = lblStatus;
		location = new Point(12, 360);
		label.Location = location;
		Label label2 = lblStatus;
		size = new Size(616, 24);
		label2.Size = size;
		Controls.Add(lblStatus);
	}

	private void RefreshKeys()
	{
		lstKeys.Items.Clear();
		foreach (string item in MailPgpService.ListPublicKeyLabels())
		{
			lstKeys.Items.Add(item);
		}
		lblStatus.Text = lstKeys.Items.Count + " PUBLIC KEY(S) — RING: " + MailPgpService.GnupgHomeDirectory();
	}

	private void OnWkdLookup(object sender, EventArgs e)
	{
		string text = LcarsCommsUi.Prompt("Email for WKD lookup:", "LCARS COMMS — WKD");
		if (!string.IsNullOrWhiteSpace(text))
		{
			if (MailAutocryptService.TryImportWkdForEmail(text.Trim()))
			{
				RefreshKeys();
				lblStatus.Text = "WKD KEY IMPORTED";
			}
			else
			{
				string prompt = ((MailAutocryptService.LastError != null) ? MailAutocryptService.LastError : "WKD FAILED").ToUpperInvariant();
				lblStatus.Text = prompt;
				LcarsCommsUi.Alert(prompt, "LCARS COMMS — PGP ERROR");
			}
		}
	}

	private void OnImport(object sender, EventArgs e)
	{
		using OpenFileDialog openFileDialog = new OpenFileDialog();
		openFileDialog.Filter = "PGP keys (*.asc;*.gpg)|*.asc;*.gpg|All files|*.*";
		if (openFileDialog.ShowDialog(this) == DialogResult.OK)
		{
			int num = MailPgpService.ImportArmoredKeyFile(openFileDialog.FileName);
			if (num < 0)
			{
				lblStatus.Text = ((MailPgpService.LastError != null) ? MailPgpService.LastError : "IMPORT FAILED").ToUpperInvariant();
				return;
			}
			RefreshKeys();
			lblStatus.Text = "KEY IMPORTED";
		}
	}
}
