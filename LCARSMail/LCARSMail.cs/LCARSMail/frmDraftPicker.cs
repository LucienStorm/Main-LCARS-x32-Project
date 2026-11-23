using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using LCARS;
using LCARS.Controls;

namespace LCARSMail;

public class frmDraftPicker : LCARSForm
{
	private class DraftListItem
	{
		public readonly MailDraft Draft;

		private readonly string caption;

		public DraftListItem(MailDraft d, string text)
		{
			Draft = d;
			caption = text;
		}

		public override string ToString()
		{
			return caption;
		}
	}

	private class ImapDraftListItem
	{
		public readonly MailMessageHeader Header;

		private readonly string caption;

		public ImapDraftListItem(MailMessageHeader hdr, string text)
		{
			Header = hdr;
			caption = text;
		}

		public override string ToString()
		{
			return caption;
		}
	}

	private ListBox lst;

	public MailDraft SelectedDraft
	{
		[DebuggerNonUserCode]
		get;
		[DebuggerNonUserCode]
		set;
	}

	public MailMessageHeader SelectedImapHeader
	{
		[DebuggerNonUserCode]
		get;
		[DebuggerNonUserCode]
		set;
	}

	public frmDraftPicker(string accountId)
		: this(accountId, null)
	{
	}

	public frmDraftPicker(string accountId, List<MailMessageHeader> imapDraftHeaders)
	{
		BackColor = Color.Black;
		Size clientSize = new Size(560, 400);
		ClientSize = clientSize;
		StartPosition = FormStartPosition.CenterParent;
		Text = "DRAFTS";
		FlatButton flatButton = new FlatButton
		{
			ButtonText = "DRAFTS",
			Text = "DRAFTS",
			Clickable = false,
			Color = LCARScolorStyles.StaticTan
		};
		Point location = new Point(12, 8);
		flatButton.Location = location;
		clientSize = new Size(100, 32);
		flatButton.Size = clientSize;
		Controls.Add(flatButton);
		Label label = new Label
		{
			Text = "LOCAL JSON + IMAP DRAFTS — DOUBLE-CLICK TO OPEN",
			ForeColor = Color.Orange,
			AutoSize = true
		};
		location = new Point(120, 16);
		label.Location = location;
		Controls.Add(label);
		lst = new ListBox();
		ListBox listBox = lst;
		location = new Point(12, 48);
		listBox.Location = location;
		ListBox listBox2 = lst;
		clientSize = new Size(536, 300);
		listBox2.Size = clientSize;
		lst.BackColor = Color.FromArgb(30, 30, 30);
		lst.ForeColor = Color.Orange;
		lst.DoubleClick += OnOpen;
		Controls.Add(lst);
		if (imapDraftHeaders != null)
		{
			foreach (MailMessageHeader imapDraftHeader in imapDraftHeaders)
			{
				if (imapDraftHeader != null)
				{
					MailMessageHeader mailMessageHeader = imapDraftHeader;
					string text = ((mailMessageHeader.Subject != null) ? mailMessageHeader.Subject : "(NO SUBJECT)");
					lst.Items.Add(new ImapDraftListItem(imapDraftHeader, "[IMAP] " + text + " — " + imapDraftHeader.DateUtc.ToLocalTime().ToString("g")));
				}
			}
		}
		List<MailDraft> list = DraftStore.ListDraftsForAccount(accountId);
		foreach (MailDraft item in list)
		{
			MailDraft mailDraft = item;
			string text2 = ((mailDraft.Subject != null) ? mailDraft.Subject : "(NO SUBJECT)");
			lst.Items.Add(new DraftListItem(item, "[LOCAL] " + text2 + " — " + item.UpdatedUtc.ToLocalTime().ToString("g")));
		}
		FlatButton flatButton2 = new FlatButton
		{
			ButtonText = "OPEN",
			Text = "OPEN",
			Color = LCARScolorStyles.PrimaryFunction
		};
		clientSize = new Size(100, 28);
		flatButton2.Size = clientSize;
		location = new Point(12, 360);
		flatButton2.Location = location;
		flatButton2.Click += OnOpen;
		Controls.Add(flatButton2);
		FlatButton flatButton3 = new FlatButton
		{
			ButtonText = "CANCEL",
			Text = "CANCEL",
			Color = LCARScolorStyles.Orange
		};
		clientSize = new Size(100, 28);
		flatButton3.Size = clientSize;
		location = new Point(448, 360);
		flatButton3.Location = location;
		flatButton3.Click += OnCancelClick;
		Controls.Add(flatButton3);
	}

	private void OnCancelClick(object sender, EventArgs e)
	{
		DialogResult = DialogResult.Cancel;
		Close();
	}

	private void OnOpen(object sender, EventArgs e)
	{
		if (lst.SelectedItem is ImapDraftListItem imapDraftListItem)
		{
			SelectedImapHeader = imapDraftListItem.Header;
			DialogResult = DialogResult.OK;
			Close();
		}
		else if (lst.SelectedItem is DraftListItem draftListItem)
		{
			SelectedDraft = draftListItem.Draft;
			DialogResult = DialogResult.OK;
			Close();
		}
	}
}
