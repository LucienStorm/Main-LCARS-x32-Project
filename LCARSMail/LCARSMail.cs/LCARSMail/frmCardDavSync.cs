using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using LCARS;
using LCARS.Controls;

namespace LCARSMail;

public class frmCardDavSync : LCARSForm
{
	private TextBox txtUrl;

	private TextBox txtUser;

	private TextBox txtPassword;

	private Label lblStatus;

	private ComboBox cboTbAccounts;

	private ComboBox cbo412Conflict;

	private string lastProfileDir;

	private List<ThunderbirdCardDavAccount> tbAccounts;

	public frmCardDavSync()
	{
		BackColor = Color.Black;
		Size clientSize = new Size(520, 320);
		ClientSize = clientSize;
		StartPosition = FormStartPosition.CenterParent;
		BuildUi();
	}

	public frmCardDavSync(string profileDir)
		: this()
	{
		lastProfileDir = ((profileDir != null) ? profileDir : "").Trim();
		TryLoadThunderbirdCardDav();
		TryLoadStoredCardDav();
	}

	private void BuildUi()
	{
		AddLabel("SERVER URL", 12, 16);
		txtUrl = AddField(12, 36, 490);
		AddLabel("USERNAME", 12, 68);
		txtUser = AddField(12, 88, 490);
		AddLabel("PASSWORD", 12, 120);
		txtPassword = AddField(12, 140, 490);
		txtPassword.UseSystemPasswordChar = true;
		AddLabel("TB CARDDAV BOOK", 12, 168);
		cboTbAccounts = new ComboBox();
		cboTbAccounts.DropDownStyle = ComboBoxStyle.DropDownList;
		ComboBox comboBox = cboTbAccounts;
		Point location = new Point(12, 188);
		comboBox.Location = location;
		ComboBox comboBox2 = cboTbAccounts;
		Size size = new Size(360, 24);
		comboBox2.Size = size;
		cboTbAccounts.BackColor = Color.FromArgb(40, 40, 40);
		cboTbAccounts.ForeColor = Color.Orange;
		cboTbAccounts.SelectedIndexChanged += OnTbAccountChanged;
		Controls.Add(cboTbAccounts);
		AddLabel("412 CONFLICT", 380, 168);
		cbo412Conflict = new ComboBox();
		cbo412Conflict.DropDownStyle = ComboBoxStyle.DropDownList;
		cbo412Conflict.Items.AddRange(new object[2] { "ClientMerge", "ServerWins" });
		ComboBox comboBox3 = cbo412Conflict;
		location = new Point(380, 188);
		comboBox3.Location = location;
		ComboBox comboBox4 = cbo412Conflict;
		size = new Size(122, 24);
		comboBox4.Size = size;
		cbo412Conflict.BackColor = Color.FromArgb(40, 40, 40);
		cbo412Conflict.ForeColor = Color.Orange;
		Controls.Add(cbo412Conflict);
		lblStatus = new Label();
		lblStatus.ForeColor = Color.Orange;
		Label label = lblStatus;
		location = new Point(12, 220);
		label.Location = location;
		Label label2 = lblStatus;
		size = new Size(490, 24);
		label2.Size = size;
		Controls.Add(lblStatus);
		FlatButton flatButton = new FlatButton();
		flatButton.ButtonText = "SYNC";
		flatButton.Text = "SYNC";
		flatButton.Color = LCARScolorStyles.PrimaryFunction;
		size = new Size(72, 28);
		flatButton.Size = size;
		location = new Point(200, 252);
		flatButton.Location = location;
		flatButton.Click += OnSyncClick;
		Controls.Add(flatButton);
		FlatButton flatButton2 = new FlatButton();
		flatButton2.ButtonText = "PUSH";
		flatButton2.Text = "PUSH";
		flatButton2.Color = LCARScolorStyles.SystemFunction;
		size = new Size(72, 28);
		flatButton2.Size = size;
		location = new Point(278, 252);
		flatButton2.Location = location;
		flatButton2.Click += OnPushClick;
		Controls.Add(flatButton2);
		FlatButton flatButton3 = new FlatButton();
		flatButton3.ButtonText = "PUSH ALL";
		flatButton3.Text = "PUSH ALL";
		flatButton3.Color = LCARScolorStyles.NavigationFunction;
		size = new Size(88, 28);
		flatButton3.Size = size;
		location = new Point(356, 252);
		flatButton3.Location = location;
		flatButton3.Click += OnPushAllClick;
		Controls.Add(flatButton3);
		FlatButton flatButton4 = new FlatButton();
		flatButton4.ButtonText = "EXPORT";
		flatButton4.Text = "EXPORT";
		flatButton4.Color = LCARScolorStyles.MiscFunction;
		size = new Size(80, 28);
		flatButton4.Size = size;
		location = new Point(450, 252);
		flatButton4.Location = location;
		flatButton4.Click += OnExportClick;
		Controls.Add(flatButton4);
		FlatButton flatButton5 = new FlatButton();
		flatButton5.ButtonText = "CLOSE";
		flatButton5.Text = "CLOSE";
		flatButton5.Color = LCARScolorStyles.Orange;
		size = new Size(72, 28);
		flatButton5.Size = size;
		location = new Point(12, 288);
		flatButton5.Location = location;
		flatButton5.Click += OnCloseClick;
		Controls.Add(flatButton5);
		FlatButton flatButton6 = new FlatButton();
		flatButton6.ButtonText = "412 FIX";
		flatButton6.Text = "412 FIX";
		flatButton6.Color = LCARScolorStyles.Orange;
		size = new Size(72, 28);
		flatButton6.Size = size;
		location = new Point(92, 288);
		flatButton6.Location = location;
		flatButton6.Click += OnResolve412Click;
		Controls.Add(flatButton6);
	}

	private void TryLoadThunderbirdCardDav()
	{
		tbAccounts = null;
		cboTbAccounts.Items.Clear();
		if (lastProfileDir == null || lastProfileDir.Length == 0)
		{
			tbAccounts = AddressBookStore.TryLoadThunderbirdCardDavSecondaryAccounts();
		}
		else
		{
			tbAccounts = ThunderbirdCardDavImporter.TryLoadFromProfile(lastProfileDir);
			if (tbAccounts != null && tbAccounts.Count > 0)
			{
				AddressBookStore.SaveThunderbirdCardDavSecondaryAccounts(tbAccounts);
			}
		}
		if (tbAccounts == null || tbAccounts.Count == 0)
		{
			return;
		}
		int num = 0;
		foreach (ThunderbirdCardDavAccount tbAccount in tbAccounts)
		{
			if (tbAccount != null)
			{
				num = checked(num + 1);
				if (string.IsNullOrWhiteSpace(tbAccount.BookKey))
				{
					tbAccount.BookKey = "carddav-tb-" + num;
				}
				string item = num + " · " + tbAccount.ServerUrl;
				cboTbAccounts.Items.Add(item);
			}
		}
		cboTbAccounts.SelectedIndex = 0;
		ApplyTbAccountFields(tbAccounts[0]);
	}

	private void OnTbAccountChanged(object sender, EventArgs e)
	{
		if (tbAccounts != null && cboTbAccounts.SelectedIndex >= 0 && cboTbAccounts.SelectedIndex < tbAccounts.Count)
		{
			ApplyTbAccountFields(tbAccounts[cboTbAccounts.SelectedIndex]);
		}
	}

	private void ApplyTbAccountFields(ThunderbirdCardDavAccount acct)
	{
		if (acct != null)
		{
			txtUrl.Text = acct.ServerUrl;
			txtUser.Text = acct.Username;
		}
	}

	private void TryLoadStoredCardDav()
	{
		string serverUrl = null;
		string username = null;
		string password = null;
		if (AddressBookStore.TryLoadCardDavSettings(ref serverUrl, ref username, ref password))
		{
			if (string.IsNullOrWhiteSpace(txtUrl.Text))
			{
				txtUrl.Text = serverUrl;
			}
			if (string.IsNullOrWhiteSpace(txtUser.Text))
			{
				txtUser.Text = username;
			}
			if (password != null)
			{
				txtPassword.Text = password;
			}
			string text = CardDavSyncService.ConflictOn412Mode;
			if (string.IsNullOrWhiteSpace(text))
			{
				text = "ClientMerge";
			}
			cbo412Conflict.SelectedItem = text;
			if (cbo412Conflict.SelectedIndex < 0)
			{
				cbo412Conflict.SelectedIndex = 0;
			}
		}
	}

	private void PersistCardDavSettingsFromForm()
	{
		string conflictOn412Mode = "ClientMerge";
		if (cbo412Conflict.SelectedItem != null)
		{
			conflictOn412Mode = cbo412Conflict.SelectedItem.ToString().Trim();
		}
		AddressBookStore.SaveCardDavSettings(txtUrl.Text, txtUser.Text, txtPassword.Text, conflictOn412Mode);
		if (tbAccounts == null || cboTbAccounts.SelectedIndex < 0 || cboTbAccounts.SelectedIndex >= tbAccounts.Count)
		{
			return;
		}
		ThunderbirdCardDavAccount thunderbirdCardDavAccount = tbAccounts[cboTbAccounts.SelectedIndex];
		if (thunderbirdCardDavAccount != null)
		{
			ThunderbirdCardDavAccount thunderbirdCardDavAccount2 = thunderbirdCardDavAccount;
			string text = ((thunderbirdCardDavAccount2.BookKey != null) ? thunderbirdCardDavAccount2.BookKey : "").Trim();
			if (text.Length == 0)
			{
				text = "carddav-tb-" + checked(cboTbAccounts.SelectedIndex + 1);
			}
			thunderbirdCardDavAccount.BookKey = text;
			if (txtPassword.Text.Length > 0)
			{
				AccountStore.SavePassword(text, txtPassword.Text);
			}
		}
		AddressBookStore.SaveThunderbirdCardDavSecondaryAccounts(tbAccounts);
	}

	private void EnsureMissingBookPasswords()
	{
		if (tbAccounts == null || tbAccounts.Count == 0)
		{
			return;
		}
		foreach (ThunderbirdCardDavAccount tbAccount in tbAccounts)
		{
			if (tbAccount == null)
			{
				continue;
			}
			ThunderbirdCardDavAccount thunderbirdCardDavAccount = tbAccount;
			string text = ((thunderbirdCardDavAccount.BookKey != null) ? thunderbirdCardDavAccount.BookKey : "").Trim();
			if (text.Length == 0)
			{
				continue;
			}
			string password = null;
			if (!AccountStore.TryGetPassword(text, ref password) || string.IsNullOrEmpty(password))
			{
				string text2 = LcarsCommsUi.Prompt("Password for CardDAV book:\r\n" + tbAccount.ServerUrl, "LCARS COMMS — BOOK PASSWORD", txtPassword.Text);
				if (!string.IsNullOrWhiteSpace(text2))
				{
					AccountStore.SavePassword(text, text2);
				}
			}
		}
		AddressBookStore.SaveThunderbirdCardDavSecondaryAccounts(tbAccounts);
	}

	private void OnResolve412Click(object sender, EventArgs e)
	{
		string text = LcarsCommsUi.Prompt("Contact email with CardDAV 412 conflict:", "LCARS COMMS — 412 RESOLVE", CardDavSyncService.Last412ConflictEmail);
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		string text2 = LcarsCommsUi.Prompt("Mode: ClientMerge or ServerWins", "LCARS COMMS — 412 RESOLVE", CardDavSyncService.ConflictOn412Mode);
		if (!string.IsNullOrWhiteSpace(text2))
		{
			PersistCardDavSettingsFromForm();
			if (AddressBookStore.TryResolveCardDav412ConflictForEmail(text, txtUrl.Text, txtUser.Text, txtPassword.Text, text2))
			{
				lblStatus.Text = "412 RESOLVED FOR " + text.Trim().ToUpperInvariant();
				return;
			}
			string prompt = ((AddressBookStore.LastError != null) ? AddressBookStore.LastError : "412 RESOLVE FAILED").ToUpperInvariant();
			lblStatus.Text = prompt;
			LcarsCommsUi.Alert(prompt, "LCARS COMMS — CARDDAV ERROR");
		}
	}

	private void OnPushAllClick(object sender, EventArgs e)
	{
		int num = AddressBookStore.TryPushAllLocalContactsCardDav(txtUrl.Text, txtUser.Text, txtPassword.Text);
		if (num <= 0 && !string.IsNullOrEmpty(AddressBookStore.LastError))
		{
			lblStatus.Text = ((AddressBookStore.LastError != null) ? AddressBookStore.LastError : "PUSH ALL FAILED").ToUpperInvariant();
		}
		else
		{
			lblStatus.Text = num + " CONTACT(S) PUSHED";
		}
	}

	private void OnPushClick(object sender, EventArgs e)
	{
		string text = LcarsCommsUi.Prompt("Contact email to upload:", "LCARS COMMS — PUSH VCARD");
		if (!string.IsNullOrWhiteSpace(text))
		{
			string text2 = LcarsCommsUi.Prompt("Display name:", "LCARS COMMS — PUSH VCARD", text);
			MailContact mailContact = new MailContact();
			mailContact.Email = text.Trim();
			mailContact.DisplayName = ((text2 != null) ? text2 : "").Trim();
			MailContact contact = mailContact;
			if (AddressBookStore.TryPushContactCardDav(txtUrl.Text, txtUser.Text, txtPassword.Text, contact))
			{
				lblStatus.Text = "CONTACT PUSHED";
			}
			else
			{
				lblStatus.Text = ((AddressBookStore.LastError != null) ? AddressBookStore.LastError : "PUSH FAILED").ToUpperInvariant();
			}
		}
	}

	private void OnExportClick(object sender, EventArgs e)
	{
		using SaveFileDialog saveFileDialog = new SaveFileDialog();
		saveFileDialog.Filter = "vCard (*.vcf)|*.vcf|LDIF (*.ldif)|*.ldif|Mork (*.mab)|*.mab";
		if (saveFileDialog.ShowDialog(this) != DialogResult.OK)
		{
			return;
		}
		bool flag = false;
		flag = (saveFileDialog.FileName.EndsWith(".ldif", StringComparison.OrdinalIgnoreCase) ? AddressBookStore.TryExportContactsLdif(saveFileDialog.FileName) : ((!saveFileDialog.FileName.EndsWith(".mab", StringComparison.OrdinalIgnoreCase)) ? AddressBookStore.TryExportContactsVCard(saveFileDialog.FileName) : AddressBookStore.TryExportContactsMork(saveFileDialog.FileName)));
		if (flag && saveFileDialog.FileName.EndsWith(".mab", StringComparison.OrdinalIgnoreCase))
		{
			if (MorkAbookParser.LastMabExportByteIdentical || MorkAbookParser.LastMabExportVerifiedAgainstSource)
			{
				lblStatus.Text = "EXPORT OK — BYTE-IDENTICAL .MAB (UNCHANGED REAL GATE)";
			}
			else if (!string.IsNullOrEmpty(AddressBookStore.LastError))
			{
				lblStatus.Text = AddressBookStore.LastError.ToUpperInvariant();
			}
			else
			{
				lblStatus.Text = "EXPORT OK — EDITED ENCODE (SLICE 13 OPEN)";
			}
		}
		else
		{
			lblStatus.Text = (flag ? "EXPORT OK" : ((AddressBookStore.LastError != null) ? AddressBookStore.LastError : "EXPORT FAILED")).ToUpperInvariant();
		}
	}

	private void OnCloseClick(object sender, EventArgs e)
	{
		Close();
	}

	private void OnSyncClick(object sender, EventArgs e)
	{
		PersistCardDavSettingsFromForm();
		EnsureMissingBookPasswords();
		AddressBookStore.TryRefreshDiscoveredCardDavBooksFromServer(txtUrl.Text, txtUser.Text, txtPassword.Text);
		int num = AddressBookStore.TrySyncStoredCardDavBidirectional();
		if (num < 0)
		{
			lblStatus.Text = ((AddressBookStore.LastError != null) ? AddressBookStore.LastError : "SYNC FAILED").ToUpperInvariant();
			return;
		}
		string text = "";
		if (AddressBookStore.LastCardDavDiscoveredBooks > 0)
		{
			text = " · DISC " + AddressBookStore.LastCardDavDiscoveredBooks;
		}
		if (AddressBookStore.LastCardDavSecondaryBooksMerged > 0)
		{
			text = text + " · TB BOOKS " + AddressBookStore.LastCardDavSecondaryBooksMerged;
		}
		lblStatus.Text = num + " CONTACT(S) 2-WAY" + text + " · 412=" + CardDavSyncService.ConflictOn412Mode.ToUpperInvariant();
	}

	private void AddLabel(string text, int x, int y)
	{
		Label label = new Label();
		label.Text = text;
		label.ForeColor = Color.Orange;
		label.BackColor = Color.Black;
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
