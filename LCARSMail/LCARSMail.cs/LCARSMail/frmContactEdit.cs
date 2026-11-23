using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using LCARS;
using LCARS.Controls;

namespace LCARSMail;

public class frmContactEdit : LCARSForm
{
	private TextBox txtEmail;

	private TextBox txtName;

	private TextBox txtCompany;

	private TextBox txtWorkPhone;

	private TextBox txtHomePhone;

	private TextBox txtNotes;

	private Label lblStatus;

	private readonly string originalEmail;

	private string cardDavEtag;

	private bool isGalEntry;

	private string galPersonaId;

	private string galPersonaChangeKey;

	private string galDirectoryItemId;

	private string galDirectoryChangeKey;

	private string ewsPersonalItemId;

	public frmContactEdit()
		: this(null)
	{
	}

	public frmContactEdit(MailContact existing)
	{
		cardDavEtag = "";
		galPersonaId = "";
		galPersonaChangeKey = "";
		galDirectoryItemId = "";
		galDirectoryChangeKey = "";
		ewsPersonalItemId = "";
		BackColor = Color.Black;
		Size clientSize = new Size(520, 360);
		ClientSize = clientSize;
		StartPosition = FormStartPosition.CenterParent;
		originalEmail = existing?.Email;
		if (existing != null)
		{
			MailContact mailContact = existing;
			cardDavEtag = ((mailContact.CardDavEtag != null) ? mailContact.CardDavEtag : "");
			isGalEntry = existing.IsGalEntry;
			mailContact = existing;
			galPersonaId = ((mailContact.GalPersonaId != null) ? mailContact.GalPersonaId : "");
			mailContact = existing;
			galPersonaChangeKey = ((mailContact.GalPersonaChangeKey != null) ? mailContact.GalPersonaChangeKey : "");
			mailContact = existing;
			galDirectoryItemId = ((mailContact.GalDirectoryItemId != null) ? mailContact.GalDirectoryItemId : "");
			mailContact = existing;
			galDirectoryChangeKey = ((mailContact.GalDirectoryChangeKey != null) ? mailContact.GalDirectoryChangeKey : "");
			mailContact = existing;
			ewsPersonalItemId = ((mailContact.EwsPersonalItemId != null) ? mailContact.EwsPersonalItemId : "");
		}
		BuildUi();
		if (existing != null)
		{
			TextBox textBox = txtEmail;
			MailContact mailContact = existing;
			textBox.Text = ((mailContact.Email != null) ? mailContact.Email : "");
			TextBox textBox2 = txtName;
			mailContact = existing;
			textBox2.Text = ((mailContact.DisplayName != null) ? mailContact.DisplayName : "");
			TextBox textBox3 = txtCompany;
			mailContact = existing;
			textBox3.Text = ((mailContact.Company != null) ? mailContact.Company : "");
			TextBox textBox4 = txtWorkPhone;
			mailContact = existing;
			textBox4.Text = ((mailContact.WorkPhone != null) ? mailContact.WorkPhone : "");
			TextBox textBox5 = txtHomePhone;
			mailContact = existing;
			textBox5.Text = ((mailContact.HomePhone != null) ? mailContact.HomePhone : "");
			TextBox textBox6 = txtNotes;
			mailContact = existing;
			textBox6.Text = ((mailContact.Notes != null) ? mailContact.Notes : "");
		}
	}

	private void BuildUi()
	{
		AddLabel("EMAIL", 12, 12);
		txtEmail = AddField(12, 32, 490);
		AddLabel("NAME", 12, 64);
		txtName = AddField(12, 84, 490);
		AddLabel("COMPANY", 12, 116);
		txtCompany = AddField(12, 136, 490);
		AddLabel("WORK PHONE", 12, 168);
		txtWorkPhone = AddField(12, 188, 240);
		AddLabel("HOME PHONE", 260, 168);
		txtHomePhone = AddField(260, 188, 242);
		AddLabel("NOTES", 12, 220);
		txtNotes = AddField(12, 240, 490);
		txtNotes.Multiline = true;
		txtNotes.Height = 56;
		lblStatus = new Label();
		lblStatus.ForeColor = Color.Orange;
		Label label = lblStatus;
		Point location = new Point(12, 304);
		label.Location = location;
		Label label2 = lblStatus;
		Size size = new Size(320, 24);
		label2.Size = size;
		Controls.Add(lblStatus);
		FlatButton flatButton = new FlatButton();
		flatButton.ButtonText = "SAVE";
		flatButton.Text = "SAVE";
		flatButton.Color = LCARScolorStyles.PrimaryFunction;
		size = new Size(72, 28);
		flatButton.Size = size;
		location = new Point(250, 300);
		flatButton.Location = location;
		flatButton.Click += OnSave;
		Controls.Add(flatButton);
		if (!string.IsNullOrWhiteSpace(originalEmail))
		{
			FlatButton flatButton2 = new FlatButton();
			flatButton2.ButtonText = "DELETE";
			flatButton2.Text = "DELETE";
			flatButton2.Color = LCARScolorStyles.CriticalFunction;
			size = new Size(72, 28);
			flatButton2.Size = size;
			location = new Point(330, 300);
			flatButton2.Location = location;
			flatButton2.Click += OnDelete;
			Controls.Add(flatButton2);
		}
		FlatButton flatButton3 = new FlatButton();
		flatButton3.ButtonText = "CANCEL";
		flatButton3.Text = "CANCEL";
		flatButton3.Color = LCARScolorStyles.Orange;
		size = new Size(72, 28);
		flatButton3.Size = size;
		location = new Point(410, 300);
		flatButton3.Location = location;
		flatButton3.Click += [SpecialName] [DebuggerStepThrough] (object a0, EventArgs a1) =>
		{
			((VB_0024AnonymousDelegate_1)([SpecialName] () =>
			{
				DialogResult = DialogResult.Cancel;
			}))();
		};
		Controls.Add(flatButton3);
	}

	private void OnDelete(object sender, EventArgs e)
	{
		string text = ((originalEmail != null) ? originalEmail : "").Trim();
		if (text.Length == 0)
		{
			string text2 = txtEmail.Text;
			text = ((text2 != null) ? text2 : "").Trim();
		}
		if (text.Length != 0 && LcarsCommsUi.Confirm("Delete contact " + text + "?", "LCARS COMMS — CONTACT"))
		{
			if (!AddressBookStore.TryRemoveContact(text))
			{
				string prompt = ((AddressBookStore.LastError != null) ? AddressBookStore.LastError : "DELETE FAILED").ToUpperInvariant();
				lblStatus.Text = prompt;
				LcarsCommsUi.Alert(prompt, "LCARS COMMS — CONTACT ERROR");
			}
			else
			{
				DialogResult = DialogResult.OK;
				Close();
			}
		}
	}

	private void OnSave(object sender, EventArgs e)
	{
		string text = txtEmail.Text;
		string text2 = ((text != null) ? text : "").Trim();
		if (text2.Length == 0)
		{
			lblStatus.Text = "EMAIL REQUIRED";
			return;
		}
		MailContact mailContact = new MailContact();
		mailContact.Email = text2;
		text = txtName.Text;
		mailContact.DisplayName = ((text != null) ? text : "").Trim();
		string text3 = txtCompany.Text;
		mailContact.Company = ((text3 != null) ? text3 : "").Trim();
		string text4 = txtWorkPhone.Text;
		mailContact.WorkPhone = ((text4 != null) ? text4 : "").Trim();
		string text5 = txtHomePhone.Text;
		mailContact.HomePhone = ((text5 != null) ? text5 : "").Trim();
		string text6 = txtNotes.Text;
		mailContact.Notes = ((text6 != null) ? text6 : "").Trim();
		mailContact.CardDavEtag = cardDavEtag;
		mailContact.CardDavDirty = true;
		mailContact.IsGalEntry = isGalEntry;
		mailContact.GalPersonaId = galPersonaId;
		mailContact.GalPersonaChangeKey = galPersonaChangeKey;
		mailContact.GalDirectoryItemId = galDirectoryItemId;
		mailContact.GalDirectoryChangeKey = galDirectoryChangeKey;
		mailContact.EwsPersonalItemId = ewsPersonalItemId;
		MailContact mailContact2 = mailContact;
		if (mailContact2.DisplayName.Length == 0)
		{
			mailContact2.DisplayName = text2;
		}
		if (!string.IsNullOrWhiteSpace(originalEmail) && !string.Equals(originalEmail, text2, StringComparison.OrdinalIgnoreCase))
		{
			AddressBookStore.TryRemoveContact(originalEmail, deleteRemoteCardDav: false);
		}
		if (mailContact2.IsGalEntry)
		{
			mailContact2.GalPushDirty = true;
		}
		List<MailContact> list = new List<MailContact>();
		list.Add(mailContact2);
		List<MailContact> incoming = list;
		if (AddressBookStore.TryMergeContacts(incoming) < 0)
		{
			lblStatus.Text = ((AddressBookStore.LastError != null) ? AddressBookStore.LastError : "SAVE FAILED").ToUpperInvariant();
			return;
		}
		string serverUrl = null;
		string username = null;
		string password = null;
		if (AddressBookStore.TryLoadCardDavSettings(ref serverUrl, ref username, ref password))
		{
			int num = AddressBookStore.TrySyncStoredCardDavBidirectional();
			if (num < 0)
			{
				lblStatus.Text = "LOCAL OK — CARDDAV 2-WAY: " + ((AddressBookStore.LastError != null) ? AddressBookStore.LastError : "SYNC FAILED").ToUpperInvariant();
				DialogResult = DialogResult.OK;
				Close();
				return;
			}
			if (mailContact2.CardDavDirty && AddressBookStore.TryResolveCardDav412ConflictForEmail(mailContact2.Email, serverUrl, username, password, CardDavSyncService.ConflictOn412Mode))
			{
				mailContact2.CardDavDirty = false;
			}
		}
		CardDavLiveBookCache.QueueBackgroundRefresh();
		CardDavLiveBookCache.RefreshNow();
		string text7 = "CONTACT SAVED";
		if (CardDavSyncService.LastMergedAfter412)
		{
			text7 += " — 412 MERGED";
		}
		if (mailContact2.IsGalEntry && mailContact2.GalPushDirty)
		{
			text7 += " — GAL PUSH QUEUED";
			string text8 = EwsGalWriteProofStore.TryLoadLastProofSummary();
			if (text8.Length > 0 && text8.Length <= 80)
			{
				text7 = text7 + " · " + text8;
			}
		}
		lblStatus.Text = text7.ToUpperInvariant();
		DialogResult = DialogResult.OK;
		Close();
	}

	private void AddLabel(string text, int x, int y)
	{
		Label label = new Label();
		label.ForeColor = Color.Orange;
		Point location = new Point(x, y);
		label.Location = location;
		Size size = new Size(200, 18);
		label.Size = size;
		label.Text = text;
		Controls.Add(label);
	}

	private TextBox AddField(int x, int y, int w)
	{
		TextBox textBox = new TextBox();
		Point location = new Point(x, y);
		textBox.Location = location;
		Size size = new Size(w, 22);
		textBox.Size = size;
		textBox.BackColor = Color.FromArgb(30, 30, 30);
		textBox.ForeColor = Color.Orange;
		Controls.Add(textBox);
		return textBox;
	}
}
