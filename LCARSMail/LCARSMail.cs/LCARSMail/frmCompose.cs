using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using LCARS;
using LCARS.Controls;
using Microsoft.VisualBasic.CompilerServices;
using MimeKit;

namespace LCARSMail;

public class frmCompose : LCARSForm
{
	private class SendWorkerArgs
	{
		public string PlainBody;

		public string HtmlBody;

		public bool SignPgp;

		public bool EncryptPgp;

		[DebuggerNonUserCode]
		public SendWorkerArgs()
		{
		}
	}

	private class SendWorkerResult
	{
		public string ErrorText;

		[DebuggerNonUserCode]
		public SendWorkerResult()
		{
		}
	}

	private sealed class ComposeContactPickerDialog : LCARSForm
	{
		private const int TopChrome = 44;

		private const int BottomBar = 44;

		private readonly ListBox lstContacts;

		private readonly List<MailContact> allContacts;

		private Timer liveRefreshTimer;

		public IList<MailContact> SelectedContacts
		{
			get
			{
				List<MailContact> list = new List<MailContact>();
				if (lstContacts == null || allContacts == null)
				{
					return list;
				}
				foreach (object selectedIndex in lstContacts.SelectedIndices)
				{
					int num = Conversions.ToInteger(selectedIndex);
					if (num >= 0 && num < allContacts.Count)
					{
						MailContact mailContact = allContacts[num];
						if (mailContact != null)
						{
							list.Add(mailContact);
						}
					}
				}
				return list;
			}
		}

		public ComposeContactPickerDialog(IList<MailContact> contacts)
		{
			allContacts = new List<MailContact>();
			if (contacts != null)
			{
				foreach (MailContact contact in contacts)
				{
					if (contact != null && !string.IsNullOrWhiteSpace(contact.Email))
					{
						allContacts.Add(contact);
					}
				}
			}
			Text = "LCARS CONTACTS";
			BackColor = Color.Black;
			ForeColor = Color.Orange;
			FormBorderStyle = FormBorderStyle.None;
			ControlBox = false;
			DoubleBuffered = true;
			BindToWorkingArea = false;
			StartPosition = FormStartPosition.CenterParent;
			Size clientSize = new Size(480, 520);
			ClientSize = clientSize;
			ShowInTaskbar = false;
			FlatButton flatButton = new FlatButton();
			flatButton.ButtonText = "PICK CONTACTS";
			flatButton.Text = flatButton.ButtonText;
			flatButton.Clickable = false;
			flatButton.Color = LCARScolorStyles.StaticTan;
			flatButton.ButtonTextAlign = ContentAlignment.BottomRight;
			clientSize = new Size(180, 32);
			flatButton.Size = clientSize;
			Point location = new Point(12, 6);
			flatButton.Location = location;
			Controls.Add(flatButton);
			lstContacts = new ListBox();
			lstContacts.BorderStyle = BorderStyle.FixedSingle;
			lstContacts.BackColor = Color.FromArgb(40, 40, 40);
			lstContacts.ForeColor = Color.White;
			lstContacts.Font = LcarsUiFont(9.5f, FontStyle.Regular);
			lstContacts.SelectionMode = SelectionMode.MultiExtended;
			ListBox listBox = lstContacts;
			location = new Point(12, 44);
			listBox.Location = location;
			ListBox listBox2 = lstContacts;
			checked
			{
				Size size = new Size(ClientSize.Width - 24, ClientSize.Height - 44 - 44);
				listBox2.Size = size;
				foreach (MailContact allContact in allContacts)
				{
					lstContacts.Items.Add(allContact.ListCaption().ToUpperInvariant());
				}
				Controls.Add(lstContacts);
				FlatButton flatButton2 = new FlatButton
				{
					ButtonText = "CANCEL",
					Text = "CANCEL",
					Color = LCARScolorStyles.Orange
				};
				size = new Size(100, 28);
				flatButton2.Size = size;
				location = (flatButton2.Location = new Point(ClientSize.Width - flatButton2.Width - 12, ClientSize.Height - 36));
				flatButton2.Click += [SpecialName] [DebuggerStepThrough] (object a0, EventArgs a1) =>
				{
					((VB_0024AnonymousDelegate_1)([SpecialName] () =>
					{
						DialogResult = DialogResult.Cancel;
					}))();
				};
				Controls.Add(flatButton2);
				FlatButton flatButton3 = new FlatButton();
				flatButton3.ButtonText = "ADD NEW";
				flatButton3.Text = flatButton3.ButtonText;
				flatButton3.Color = LCARScolorStyles.MiscFunction;
				size = new Size(90, 28);
				flatButton3.Size = size;
				location = (flatButton3.Location = new Point(12, ClientSize.Height - 36));
				flatButton3.Click += OnAddContact;
				Controls.Add(flatButton3);
				FlatButton flatButton4 = new FlatButton();
				flatButton4.ButtonText = "EDIT";
				flatButton4.Text = flatButton4.ButtonText;
				flatButton4.Color = LCARScolorStyles.SystemFunction;
				size = new Size(72, 28);
				flatButton4.Size = size;
				location = (flatButton4.Location = new Point(flatButton3.Right + 8, ClientSize.Height - 36));
				flatButton4.Click += OnEditContact;
				Controls.Add(flatButton4);
				FlatButton flatButton5 = new FlatButton();
				flatButton5.ButtonText = "ADD TO TO";
				flatButton5.Text = flatButton5.ButtonText;
				flatButton5.Color = LCARScolorStyles.PrimaryFunction;
				size = new Size(120, 28);
				flatButton5.Size = size;
				location = (flatButton5.Location = new Point(flatButton2.Left - 8 - flatButton5.Width, ClientSize.Height - 36));
				flatButton5.Click += [SpecialName] [DebuggerStepThrough] (object a0, EventArgs a1) =>
				{
					((VB_0024AnonymousDelegate_1)([SpecialName] () =>
					{
						if (lstContacts.SelectedIndices.Count != 0)
						{
							DialogResult = DialogResult.OK;
						}
					}))();
				};
				Controls.Add(flatButton5);
				FlatButton flatButton6 = new FlatButton
				{
					ButtonText = "LIVE",
					Text = "LIVE",
					Color = LCARScolorStyles.SystemFunction
				};
				size = new Size(64, 28);
				flatButton6.Size = size;
				location = (flatButton6.Location = new Point(flatButton4.Right + 8, ClientSize.Height - 36));
				flatButton6.Click += [SpecialName] [DebuggerStepThrough] (object a0, EventArgs a1) =>
				{
					((VB_0024AnonymousDelegate_1)([SpecialName] () =>
					{
						CardDavLiveBookCache.RefreshNow();
						ReloadContactList();
					}))();
				};
				Controls.Add(flatButton6);
				liveRefreshTimer = new Timer();
				liveRefreshTimer.Interval = 20000;
				liveRefreshTimer.Tick += [SpecialName] (object s, EventArgs ev) =>
				{
					CardDavLiveBookCache.QueueBackgroundRefresh();
					ReloadContactList();
				};
				Load += [SpecialName] [DebuggerStepThrough] (object a0, EventArgs a1) =>
				{
					((VB_0024AnonymousDelegate_1)([SpecialName] () =>
					{
						CardDavLiveBookCache.QueueBackgroundRefresh();
						liveRefreshTimer.Start();
					}))();
				};
				FormClosed += [SpecialName] (object s, FormClosedEventArgs ev) =>
				{
					if (liveRefreshTimer != null)
					{
						liveRefreshTimer.Stop();
					}
				};
			}
		}

		private void OnAddContact(object sender, EventArgs e)
		{
			using (frmContactEdit frmContactEdit2 = new frmContactEdit())
			{
				if (frmContactEdit2.ShowDialog(this) != DialogResult.OK)
				{
					return;
				}
			}
			ReloadContactList();
		}

		private void OnEditContact(object sender, EventArgs e)
		{
			if (lstContacts.SelectedIndex < 0)
			{
				return;
			}
			int selectedIndex = lstContacts.SelectedIndex;
			if (selectedIndex < 0 || selectedIndex >= allContacts.Count)
			{
				return;
			}
			using (frmContactEdit frmContactEdit2 = new frmContactEdit(allContacts[selectedIndex]))
			{
				if (frmContactEdit2.ShowDialog(this) != DialogResult.OK)
				{
					return;
				}
			}
			ReloadContactList();
		}

		private void ReloadContactList()
		{
			allContacts.Clear();
			foreach (MailContact item in AddressBookStore.LoadLiveContacts())
			{
				if (item != null && !string.IsNullOrWhiteSpace(item.Email))
				{
					allContacts.Add(item);
				}
			}
			lstContacts.Items.Clear();
			foreach (MailContact allContact in allContacts)
			{
				lstContacts.Items.Add(allContact.ListCaption().ToUpperInvariant());
			}
		}
	}

	private const int MarginX = 12;

	private const int LabelW = 72;

	private const int FieldH = 24;

	private const int RowGap = 6;

	private const int TopChrome = 40;

	private const int BottomBar = 44;

	private const int AttachmentRowH = 72;

	private const int ContactsBtnW = 96;

	private const int ContactsBtnGap = 6;

	private readonly MailAccount mailAccount;

	private readonly string mailPassword;

	private readonly SmtpMailService smtpService;

	private readonly ImapMailService imapService;

	private readonly List<string> composeAttachmentPaths;

	private readonly BackgroundWorker sendWorker;

	private MailHtmlComposePane htmlComposePane;

	private string draftId;

	private string imapDraftFolder;

	private ulong imapDraftUid;

	private FlatButton fbTitle;

	private Label lblTo;

	private Label lblCc;

	private Label lblSubject;

	private TextBox txtTo;

	private FlatButton fbContacts;

	private TextBox txtCc;

	private TextBox txtSubject;

	private TextBox txtBody;

	private Label lblAttachments;

	private ListBox lstAttachments;

	private FlatButton fbAddAttachment;

	private FlatButton fbRemoveAttachment;

	private Label lblStatus;

	private FlatButton fbSend;

	private FlatButton fbSaveDraft;

	private FlatButton fbCancel;

	private CheckBox chkSignPgp;

	private CheckBox chkEncryptPgp;

	public IList<string> AttachmentPaths => composeAttachmentPaths;

	public string ComposeTo
	{
		get
		{
			return (txtTo == null) ? "" : txtTo.Text;
		}
		set
		{
			if (txtTo != null)
			{
				txtTo.Text = ((value != null) ? value : "");
			}
		}
	}

	public string ComposeCc
	{
		get
		{
			return (txtCc == null) ? "" : txtCc.Text;
		}
		set
		{
			if (txtCc != null)
			{
				txtCc.Text = ((value != null) ? value : "");
			}
		}
	}

	public string ComposeSubject
	{
		get
		{
			return (txtSubject == null) ? "" : txtSubject.Text;
		}
		set
		{
			if (txtSubject != null)
			{
				txtSubject.Text = ((value != null) ? value : "");
			}
		}
	}

	public string ComposeBodyHtml
	{
		get
		{
			if (htmlComposePane != null)
			{
				return htmlComposePane.HtmlBody;
			}
			return "";
		}
		set
		{
			if (htmlComposePane != null)
			{
				htmlComposePane.SetHtmlContent((value != null) ? value : "");
			}
		}
	}

	public string ComposeBody
	{
		get
		{
			if (htmlComposePane != null)
			{
				return htmlComposePane.PlainText;
			}
			return (txtBody == null) ? "" : txtBody.Text;
		}
		set
		{
			if (htmlComposePane != null)
			{
				htmlComposePane.PlainText = ((value != null) ? value : "");
			}
			else if (txtBody != null)
			{
				txtBody.Text = ((value != null) ? value : "");
			}
		}
	}

	public Action<string> SendStatusReporter
	{
		[DebuggerNonUserCode]
		get;
		[DebuggerNonUserCode]
		set;
	}

	public void ApplyDraftLink(MailDraft draft)
	{
		if (draft == null)
		{
			return;
		}
		MailDraft mailDraft = draft;
		draftId = ((mailDraft.Id != null) ? mailDraft.Id : "");
		mailDraft = draft;
		imapDraftFolder = ((mailDraft.ImapDraftFolder != null) ? mailDraft.ImapDraftFolder : "");
		imapDraftUid = draft.ImapDraftUid;
		mailDraft = draft;
		ComposeTo = ((mailDraft.ToAddresses != null) ? mailDraft.ToAddresses : "");
		mailDraft = draft;
		ComposeCc = ((mailDraft.CcAddresses != null) ? mailDraft.CcAddresses : "");
		mailDraft = draft;
		ComposeSubject = ((mailDraft.Subject != null) ? mailDraft.Subject : "");
		if (!string.IsNullOrWhiteSpace(draft.BodyHtml))
		{
			ComposeBodyHtml = draft.BodyHtml;
		}
		else
		{
			mailDraft = draft;
			ComposeBody = ((mailDraft.BodyPlain != null) ? mailDraft.BodyPlain : "");
		}
		composeAttachmentPaths.Clear();
		if (draft.AttachmentPaths != null)
		{
			composeAttachmentPaths.AddRange(draft.AttachmentPaths);
		}
		if (lstAttachments == null)
		{
			return;
		}
		lstAttachments.Items.Clear();
		foreach (string composeAttachmentPath in composeAttachmentPaths)
		{
			lstAttachments.Items.Add(Path.GetFileName(composeAttachmentPath));
		}
	}

	public void ApplyImapDraftLink(string folderFullName, ulong uid)
	{
		imapDraftFolder = ((folderFullName != null) ? folderFullName : "").Trim();
		imapDraftUid = uid;
	}

	public frmCompose(MailAccount account, string password)
	{
		smtpService = new SmtpMailService();
		imapService = new ImapMailService();
		composeAttachmentPaths = new List<string>();
		sendWorker = new BackgroundWorker();
		mailAccount = account;
		mailPassword = ((password != null) ? password : "");
		Text = "LCARS COMPOSE";
		BackColor = Color.Black;
		ForeColor = Color.Orange;
		FormBorderStyle = FormBorderStyle.None;
		ControlBox = false;
		DoubleBuffered = true;
		BindToWorkingArea = false;
		StartPosition = FormStartPosition.CenterParent;
		Size clientSize = new Size(640, 520);
		ClientSize = clientSize;
		ShowInTaskbar = false;
		sendWorker.WorkerSupportsCancellation = false;
		sendWorker.DoWork += SendWorker_DoWork;
		sendWorker.RunWorkerCompleted += SendWorker_Completed;
		BuildChrome();
		CardDavLiveBookCache.EnsureAlwaysOnStarted();
		Resize += OnComposeResize;
		Shown += OnComposeShown;
	}

	private void BuildChrome()
	{
		fbTitle = new FlatButton();
		fbTitle.Name = "fbTitle";
		fbTitle.ButtonText = "COMPOSE";
		fbTitle.Text = "COMPOSE";
		fbTitle.Clickable = false;
		fbTitle.Color = LCARScolorStyles.StaticTan;
		fbTitle.ButtonTextAlign = ContentAlignment.BottomRight;
		fbTitle.ButtonTextHeight = 14;
		FlatButton flatButton = fbTitle;
		Size size = new Size(120, 32);
		flatButton.Size = size;
		FlatButton flatButton2 = fbTitle;
		Point location = new Point(12, 4);
		flatButton2.Location = location;
		Controls.Add(fbTitle);
		lblTo = MakeFieldLabel("TO");
		lblCc = MakeFieldLabel("CC");
		lblSubject = MakeFieldLabel("SUBJECT");
		txtTo = MakeFieldTextBox();
		fbContacts = MakeActionButton("fbContacts", "CONTACTS", LCARScolorStyles.MiscFunction);
		FlatButton flatButton3 = fbContacts;
		size = new Size(96, 24);
		flatButton3.Size = size;
		fbContacts.Click += OnContactsClick;
		txtCc = MakeFieldTextBox();
		txtSubject = MakeFieldTextBox();
		txtBody = MakeFieldTextBox();
		txtBody.Visible = false;
		htmlComposePane = new MailHtmlComposePane();
		htmlComposePane.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
		lblAttachments = MakeFieldLabel("FILES");
		lblAttachments.TextAlign = ContentAlignment.TopRight;
		lstAttachments = new ListBox();
		lstAttachments.BorderStyle = BorderStyle.FixedSingle;
		lstAttachments.BackColor = Color.FromArgb(40, 40, 40);
		lstAttachments.ForeColor = Color.White;
		lstAttachments.Font = LcarsUiFont(9f, FontStyle.Regular);
		fbAddAttachment = MakeActionButton("fbAddAttachment", "ADD FILE", LCARScolorStyles.SystemFunction);
		FlatButton flatButton4 = fbAddAttachment;
		size = new Size(100, 28);
		flatButton4.Size = size;
		fbAddAttachment.Click += OnAddAttachmentClick;
		fbRemoveAttachment = MakeActionButton("fbRemoveAttachment", "REMOVE", LCARScolorStyles.Orange);
		FlatButton flatButton5 = fbRemoveAttachment;
		size = new Size(100, 28);
		flatButton5.Size = size;
		fbRemoveAttachment.Click += OnRemoveAttachmentClick;
		lblStatus = new Label();
		lblStatus.Name = "lblStatus";
		lblStatus.AutoSize = false;
		lblStatus.TextAlign = ContentAlignment.MiddleLeft;
		lblStatus.ForeColor = Color.FromArgb(200, 140, 60);
		lblStatus.BackColor = Color.Black;
		lblStatus.Font = LcarsUiFont(9f);
		lblStatus.Text = "";
		fbSend = MakeActionButton("fbSend", "SEND", LCARScolorStyles.PrimaryFunction);
		fbSend.Click += OnSendClick;
		fbSaveDraft = MakeActionButton("fbSaveDraft", "SAVE DRAFT", LCARScolorStyles.SystemFunction);
		fbSaveDraft.Click += OnSaveDraftClick;
		fbCancel = MakeActionButton("fbCancel", "CANCEL", LCARScolorStyles.NavigationFunction);
		fbCancel.Click += OnCancelClick;
		chkSignPgp = new CheckBox();
		chkSignPgp.Text = "SIGN PGP";
		chkSignPgp.ForeColor = Color.Orange;
		chkSignPgp.BackColor = Color.Black;
		chkSignPgp.Font = LcarsUiFont(9f);
		chkSignPgp.AutoSize = true;
		chkEncryptPgp = new CheckBox();
		chkEncryptPgp.Text = "ENCRYPT PGP";
		chkEncryptPgp.ForeColor = Color.Orange;
		chkEncryptPgp.BackColor = Color.Black;
		chkEncryptPgp.Font = LcarsUiFont(9f);
		chkEncryptPgp.AutoSize = true;
		Controls.Add(lblTo);
		Controls.Add(lblCc);
		Controls.Add(lblSubject);
		Controls.Add(txtTo);
		Controls.Add(fbContacts);
		Controls.Add(txtCc);
		Controls.Add(txtSubject);
		Controls.Add(lblAttachments);
		Controls.Add(lstAttachments);
		Controls.Add(fbAddAttachment);
		Controls.Add(fbRemoveAttachment);
		Controls.Add(txtBody);
		Controls.Add(htmlComposePane);
		Controls.Add(lblStatus);
		Controls.Add(fbSend);
		Controls.Add(fbSaveDraft);
		Controls.Add(fbCancel);
		Controls.Add(chkSignPgp);
		Controls.Add(chkEncryptPgp);
		fbSend.BringToFront();
		fbCancel.BringToFront();
		fbTitle.BringToFront();
	}

	private void OnComposeShown(object sender, EventArgs e)
	{
		LayoutFields();
		if (txtTo != null)
		{
			txtTo.Focus();
		}
	}

	private void OnComposeResize(object sender, EventArgs e)
	{
		LayoutFields();
	}

	private void LayoutFields()
	{
		checked
		{
			if (txtTo != null)
			{
				int num = 92;
				int num2 = Math.Max(120, ClientSize.Width - num - 12);
				int fieldW = Math.Max(80, num2 - 96 - 6);
				int num3 = 40;
				PlaceLabelRow(lblTo, txtTo, num3, num, fieldW);
				Point location;
				if (fbContacts != null && txtTo != null)
				{
					FlatButton flatButton = fbContacts;
					location = new Point(txtTo.Right + 6, num3);
					flatButton.Location = location;
					FlatButton flatButton2 = fbContacts;
					Size size = new Size(96, 24);
					flatButton2.Size = size;
				}
				num3 += 30;
				PlaceLabelRow(lblCc, txtCc, num3, num, num2);
				num3 += 30;
				PlaceLabelRow(lblSubject, txtSubject, num3, num, num2);
				num3 += 34;
				if (lblAttachments != null)
				{
					location = (lblAttachments.Location = new Point(12, num3 + 2));
					Label label = lblAttachments;
					Size size = new Size(72, 72);
					label.Size = size;
				}
				if (lstAttachments != null)
				{
					location = (lstAttachments.Location = new Point(num, num3));
					ListBox listBox = lstAttachments;
					Size size = new Size(Math.Max(120, num2 - 216), 72);
					listBox.Size = size;
				}
				if (fbAddAttachment != null && lstAttachments != null)
				{
					location = (fbAddAttachment.Location = new Point(lstAttachments.Right + 8, num3));
				}
				if (fbRemoveAttachment != null && fbAddAttachment != null)
				{
					location = (fbRemoveAttachment.Location = new Point(fbAddAttachment.Left, fbAddAttachment.Bottom + 6));
				}
				num3 += 78;
				int num4 = ClientSize.Height - 44;
				int num5 = Math.Max(80, num4 - num3 - 8);
				if (htmlComposePane != null)
				{
					location = (htmlComposePane.Location = new Point(num, num3));
					MailHtmlComposePane mailHtmlComposePane = htmlComposePane;
					Size size = new Size(num2, num5);
					mailHtmlComposePane.Size = size;
				}
				if (txtBody != null)
				{
					location = (txtBody.Location = new Point(num, num3));
					TextBox textBox = txtBody;
					Size size = new Size(num2, num5);
					textBox.Size = size;
				}
				if (lblStatus != null)
				{
					location = (lblStatus.Location = new Point(12, num4 + 4));
					Label label2 = lblStatus;
					Size size = new Size(Math.Max(80, num2 + 72 - fbSend.Width - fbCancel.Width - fbSaveDraft.Width - 16), 28);
					label2.Size = size;
				}
				if (fbCancel != null)
				{
					location = (fbCancel.Location = new Point(ClientSize.Width - 12 - fbCancel.Width, num4 + 4));
				}
				if (fbSend != null && fbCancel != null)
				{
					FlatButton flatButton3 = fbSend;
					location = new Point(fbCancel.Left - 8 - fbSend.Width, num4 + 4);
					flatButton3.Location = location;
				}
				if (fbSaveDraft != null && fbSend != null)
				{
					FlatButton flatButton4 = fbSaveDraft;
					location = new Point(fbSend.Left - 8 - fbSaveDraft.Width, num4 + 4);
					flatButton4.Location = location;
				}
				if (chkEncryptPgp != null && fbSend != null)
				{
					CheckBox checkBox = chkEncryptPgp;
					location = new Point(fbSend.Left - 130, num4 + 6);
					checkBox.Location = location;
				}
				if (chkSignPgp != null && chkEncryptPgp != null)
				{
					CheckBox checkBox2 = chkSignPgp;
					location = new Point(chkEncryptPgp.Left - 110, num4 + 6);
					checkBox2.Location = location;
				}
			}
		}
	}

	private static void PlaceLabelRow(Label lbl, TextBox field, int y, int fieldLeft, int fieldW)
	{
		if (lbl != null)
		{
			Point location = new Point(12, checked(y + 4));
			lbl.Location = location;
		}
		if (field != null)
		{
			Point location = new Point(fieldLeft, y);
			field.Location = location;
			Size size = new Size(fieldW, 24);
			field.Size = size;
		}
	}

	private void OnSendClick(object sender, EventArgs e)
	{
		if (!sendWorker.IsBusy)
		{
			if (fbSend != null)
			{
				fbSend.Clickable = false;
			}
			SetStatus("SENDING…");
			ReportSendStatus("SENDING…");
			sendWorker.RunWorkerAsync();
		}
	}

	private void SendWorker_DoWork(object sender, DoWorkEventArgs e)
	{
		SendWorkerArgs sendWorkerArgs = new SendWorkerArgs();
		sendWorkerArgs.PlainBody = ComposeBody;
		sendWorkerArgs.HtmlBody = ((htmlComposePane != null) ? htmlComposePane.HtmlBody : "");
		sendWorkerArgs.SignPgp = ((chkSignPgp != null && chkSignPgp.Checked) ? true : false);
		sendWorkerArgs.EncryptPgp = ((chkEncryptPgp != null && chkEncryptPgp.Checked) ? true : false);
		SendWorkerArgs sendWorkerArgs2 = sendWorkerArgs;
		SendWorkerResult sendWorkerResult = new SendWorkerResult();
		if (EwsMailBridge.IsEwsAccount(mailAccount))
		{
			string oauthToken = EwsMailBridge.ResolveOAuthToken();
			if (!EwsMailService.TrySendMessageHtml(mailAccount.EwsUrl, mailAccount.Username, mailPassword, ComposeTo, ComposeSubject, sendWorkerArgs2.HtmlBody, sendWorkerArgs2.PlainBody, oauthToken))
			{
				sendWorkerResult.ErrorText = ((EwsMailService.LastError != null) ? EwsMailService.LastError : "EWS send failed.");
			}
		}
		else
		{
			sendWorkerResult.ErrorText = smtpService.Send(mailAccount, mailPassword, ComposeTo, ComposeCc, ComposeSubject, sendWorkerArgs2.PlainBody, composeAttachmentPaths, sendWorkerArgs2.SignPgp, sendWorkerArgs2.EncryptPgp, sendWorkerArgs2.HtmlBody);
		}
		e.Result = sendWorkerResult;
	}

	private void SendWorker_Completed(object sender, RunWorkerCompletedEventArgs e)
	{
		if (fbSend != null)
		{
			fbSend.Clickable = true;
		}
		if (e.Error != null)
		{
			SetStatus(e.Error.Message);
			ReportSendStatus("SEND ERROR: " + e.Error.Message);
			return;
		}
		if (e.Result is SendWorkerResult sendWorkerResult && !string.IsNullOrEmpty(sendWorkerResult.ErrorText))
		{
			SetStatus(sendWorkerResult.ErrorText);
			ReportSendStatus("SEND ERROR: " + sendWorkerResult.ErrorText);
			return;
		}
		if (!string.IsNullOrEmpty(draftId))
		{
			DraftStore.DeleteDraft(draftId);
		}
		ReportSendStatus("MESSAGE SENT");
		DialogResult = DialogResult.OK;
		Close();
	}

	private void OnSaveDraftClick(object sender, EventArgs e)
	{
		if (mailAccount == null)
		{
			return;
		}
		MailDraft mailDraft = new MailDraft();
		mailDraft.Id = draftId;
		mailDraft.AccountId = ((mailAccount.Id != null) ? mailAccount.Id : "");
		mailDraft.ToAddresses = ComposeTo;
		mailDraft.CcAddresses = ComposeCc;
		mailDraft.Subject = ComposeSubject;
		mailDraft.BodyPlain = ComposeBody;
		mailDraft.BodyHtml = ((htmlComposePane != null) ? htmlComposePane.HtmlBody : "");
		mailDraft.ImapDraftFolder = imapDraftFolder;
		mailDraft.ImapDraftUid = imapDraftUid;
		MailDraft mailDraft2 = mailDraft;
		mailDraft2.AttachmentPaths.AddRange(composeAttachmentPaths);
		draftId = DraftStore.SaveDraft(mailDraft2);
		try
		{
			MimeMessage message = smtpService.CreateMimeMessage(mailAccount, ComposeTo, ComposeCc, ComposeSubject, ComposeBody, composeAttachmentPaths, (htmlComposePane != null) ? htmlComposePane.HtmlBody : null);
			ulong newUid = 0uL;
			string text = "";
			if (imapDraftUid != 0 && !string.IsNullOrWhiteSpace(imapDraftFolder))
			{
				text = imapService.ReplaceDraft(mailAccount, mailPassword, imapDraftFolder, imapDraftUid, message, ref newUid);
			}
			else
			{
				text = imapService.AppendDraft(mailAccount, mailPassword, message, ref newUid);
				if (newUid != 0 && string.IsNullOrWhiteSpace(imapDraftFolder))
				{
					imapDraftFolder = imapService.ResolveDraftsFolderFullName(mailAccount, mailPassword);
				}
			}
			if (text != null && text.Length > 0)
			{
				SetStatus("DRAFT SAVED LOCALLY — IMAP: " + text);
				return;
			}
			if (newUid != 0)
			{
				imapDraftUid = newUid;
				mailDraft2.ImapDraftFolder = imapDraftFolder;
				mailDraft2.ImapDraftUid = newUid;
				DraftStore.SaveDraft(mailDraft2);
			}
		}
		catch (Exception ex)
		{
			ProjectData.SetProjectError(ex);
			Exception ex2 = ex;
			SetStatus("DRAFT SAVED LOCALLY — IMAP: " + ex2.Message);
			ProjectData.ClearProjectError();
			return;
		}
		SetStatus("DRAFT SAVED — LOCAL + IMAP UID " + imapDraftUid);
	}

	private void ReportSendStatus(string text)
	{
		Action<string> sendStatusReporter = SendStatusReporter;
		if (sendStatusReporter == null)
		{
			return;
		}
		try
		{
			sendStatusReporter(text);
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
	}

	private void OnCancelClick(object sender, EventArgs e)
	{
		DialogResult = DialogResult.Cancel;
		Close();
	}

	private void OnAddAttachmentClick(object sender, EventArgs e)
	{
		using OpenFileDialog openFileDialog = new OpenFileDialog();
		openFileDialog.Title = "ADD ATTACHMENT";
		openFileDialog.Multiselect = true;
		openFileDialog.CheckFileExists = true;
		if (openFileDialog.ShowDialog(this) != DialogResult.OK)
		{
			return;
		}
		string[] fileNames = openFileDialog.FileNames;
		foreach (string text in fileNames)
		{
			if (!string.IsNullOrWhiteSpace(text) && File.Exists(text) && !composeAttachmentPaths.Contains(text))
			{
				composeAttachmentPaths.Add(text);
				if (lstAttachments != null)
				{
					lstAttachments.Items.Add(Path.GetFileName(text));
				}
			}
		}
	}

	private void OnContactsClick(object sender, EventArgs e)
	{
		CardDavLiveBookCache.EnsureAlwaysOnStarted();
		CardDavLiveBookCache.RefreshNow();
		List<MailContact> list = AddressBookStore.LoadLiveContacts();
		if (list == null || list.Count == 0)
		{
			SetStatus("NO CONTACTS — IMPORT PROFILE WITH ABOOK");
			return;
		}
		using ComposeContactPickerDialog composeContactPickerDialog = new ComposeContactPickerDialog(list);
		if (composeContactPickerDialog.ShowDialog(this) != DialogResult.OK)
		{
			return;
		}
		IList<MailContact> selectedContacts = composeContactPickerDialog.SelectedContacts;
		if (selectedContacts == null || selectedContacts.Count == 0)
		{
			return;
		}
		List<string> list2 = new List<string>();
		foreach (MailContact item in selectedContacts)
		{
			if (item != null)
			{
				string text = item.FormatAddress();
				if (text.Length != 0)
				{
					list2.Add(text);
				}
			}
		}
		if (list2.Count != 0)
		{
			string text2 = string.Join(", ", list2.ToArray());
			string text3 = ((txtTo == null) ? "" : txtTo.Text).Trim();
			if (text3.Length == 0)
			{
				ComposeTo = text2;
			}
			else
			{
				ComposeTo = text3 + ", " + text2;
			}
			SetStatus("");
			if (txtTo != null)
			{
				txtTo.Focus();
			}
		}
	}

	private void OnRemoveAttachmentClick(object sender, EventArgs e)
	{
		if (lstAttachments != null && lstAttachments.SelectedIndex >= 0)
		{
			int selectedIndex = lstAttachments.SelectedIndex;
			if (selectedIndex >= 0 && selectedIndex < composeAttachmentPaths.Count)
			{
				composeAttachmentPaths.RemoveAt(selectedIndex);
				lstAttachments.Items.RemoveAt(selectedIndex);
			}
		}
	}

	private void SetStatus(string text)
	{
		if (lblStatus != null)
		{
			lblStatus.Text = ((text != null) ? text : "").Trim().ToUpperInvariant();
		}
	}

	private static Label MakeFieldLabel(string caption)
	{
		Label label = new Label();
		label.AutoSize = false;
		Size size = new Size(72, 24);
		label.Size = size;
		label.TextAlign = ContentAlignment.MiddleRight;
		label.ForeColor = Color.Orange;
		label.BackColor = Color.Black;
		label.Font = LcarsUiFont(10f);
		label.Text = caption.ToUpperInvariant();
		return label;
	}

	private static TextBox MakeFieldTextBox()
	{
		TextBox textBox = new TextBox();
		textBox.BorderStyle = BorderStyle.FixedSingle;
		textBox.BackColor = Color.FromArgb(40, 40, 40);
		textBox.ForeColor = Color.White;
		return textBox;
	}

	private static FlatButton MakeActionButton(string name, string caption, LCARScolorStyles color)
	{
		FlatButton flatButton = new FlatButton();
		flatButton.Name = name;
		flatButton.ButtonText = caption;
		flatButton.Text = caption;
		flatButton.Clickable = true;
		flatButton.Beeping = true;
		flatButton.Color = color;
		flatButton.ButtonTextAlign = ContentAlignment.BottomRight;
		flatButton.ButtonTextHeight = 12;
		Size size = new Size(100, 28);
		flatButton.Size = size;
		return flatButton;
	}

	private static Font LcarsUiFont(float size, FontStyle style = FontStyle.Bold)
	{
		Font result;
		try
		{
			result = new Font("Arial Narrow", size, style, GraphicsUnit.Point);
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			result = new Font("Segoe UI", size, style, GraphicsUnit.Point);
			ProjectData.ClearProjectError();
		}
		return result;
	}
}
