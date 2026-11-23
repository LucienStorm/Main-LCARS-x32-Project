using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Forms;
using LCARS;
using LCARS.Controls;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

public class frmAddressBook : LCARSForm
{
	private ListBox lstContacts;

	private Label lblStatus;

	private bool liveMode;

	private readonly List<MailContact> shownContacts;

	private readonly System.Windows.Forms.Timer liveTimer;

	public frmAddressBook()
	{
		shownContacts = new List<MailContact>();
		liveTimer = new System.Windows.Forms.Timer();
		BackColor = Color.Black;
		Size clientSize = new Size(560, 480);
		ClientSize = clientSize;
		StartPosition = FormStartPosition.CenterParent;
		BuildUi();
		liveMode = true;
		RefreshList();
		liveTimer.Interval = 10000;
		liveTimer.Tick += OnLiveTimerTick;
		liveTimer.Start();
		CardDavLiveBookCache.EnsureAlwaysOnStarted();
		CardDavLiveBookCache.RefreshNow();
		AddressBookStore.TryValidateStoredSourceMab();
		ThreadPool.QueueUserWorkItem([SpecialName] (object state) =>
		{
			try
			{
				string serverUrl = null;
				string username = null;
				string password = null;
				if (AddressBookStore.TryLoadCardDavSettings(ref serverUrl, ref username, ref password))
				{
					AddressBookStore.TryRefreshDiscoveredCardDavBooksFromServer(serverUrl, username, password);
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		});
	}

	protected override void OnFormClosed(FormClosedEventArgs e)
	{
		liveTimer.Stop();
		base.OnFormClosed(e);
	}

	private void OnLiveTimerTick(object sender, EventArgs e)
	{
		if (!liveMode)
		{
			return;
		}
		int num = AddressBookStore.CountCardDavDirtyContacts();
		liveTimer.Interval = ((num > 0) ? 4000 : 8000);
		ThreadPool.QueueUserWorkItem([SpecialName] (object state) =>
		{
			try
			{
				AddressBookStore.TrySyncStoredCardDavBidirectional();
				CardDavLiveBookCache.QueueBackgroundRefresh();
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
			try
			{
				if (!IsDisposed && IsHandleCreated)
				{
					BeginInvoke(new Action(RefreshList));
				}
			}
			catch (Exception projectError2)
			{
				ProjectData.SetProjectError(projectError2);
				ProjectData.ClearProjectError();
			}
		});
	}

	private void BuildUi()
	{
		FlatButton flatButton = new FlatButton();
		flatButton.ButtonText = "ADDRESS BOOK";
		flatButton.Text = "ADDRESS BOOK";
		flatButton.Color = LCARScolorStyles.PrimaryFunction;
		Size size = new Size(200, 28);
		flatButton.Size = size;
		Point location = new Point(12, 12);
		flatButton.Location = location;
		Controls.Add(flatButton);
		FlatButton flatButton2 = new FlatButton();
		flatButton2.ButtonText = "LIVE REFRESH";
		flatButton2.Text = "LIVE REFRESH";
		flatButton2.Color = LCARScolorStyles.SystemFunction;
		size = new Size(120, 28);
		flatButton2.Size = size;
		location = new Point(220, 12);
		flatButton2.Location = location;
		flatButton2.Click += OnLiveRefresh;
		Controls.Add(flatButton2);
		FlatButton flatButton3 = new FlatButton();
		flatButton3.ButtonText = "SYNC SAVE";
		flatButton3.Text = "SYNC SAVE";
		flatButton3.Color = LCARScolorStyles.NavigationFunction;
		size = new Size(100, 28);
		flatButton3.Size = size;
		location = new Point(350, 12);
		flatButton3.Location = location;
		flatButton3.Click += OnSyncSave;
		Controls.Add(flatButton3);
		FlatButton flatButton4 = new FlatButton();
		flatButton4.ButtonText = "ADD";
		flatButton4.Text = "ADD";
		flatButton4.Color = LCARScolorStyles.MiscFunction;
		size = new Size(72, 28);
		flatButton4.Size = size;
		location = new Point(460, 12);
		flatButton4.Location = location;
		flatButton4.Click += OnAdd;
		Controls.Add(flatButton4);
		FlatButton flatButton5 = new FlatButton();
		flatButton5.ButtonText = "412 MODE";
		flatButton5.Text = "412 MODE";
		flatButton5.Color = LCARScolorStyles.Orange;
		size = new Size(88, 28);
		flatButton5.Size = size;
		location = new Point(12, 436);
		flatButton5.Location = location;
		flatButton5.Click += OnToggle412Mode;
		Controls.Add(flatButton5);
		lstContacts = new ListBox();
		ListBox listBox = lstContacts;
		location = new Point(12, 48);
		listBox.Location = location;
		ListBox listBox2 = lstContacts;
		size = new Size(536, 380);
		listBox2.Size = size;
		lstContacts.BackColor = Color.FromArgb(30, 30, 30);
		lstContacts.ForeColor = Color.Orange;
		lstContacts.DoubleClick += OnEditSelected;
		Controls.Add(lstContacts);
		lblStatus = new Label();
		lblStatus.ForeColor = Color.Orange;
		Label label = lblStatus;
		location = new Point(108, 436);
		label.Location = location;
		Label label2 = lblStatus;
		size = new Size(440, 32);
		label2.Size = size;
		Controls.Add(lblStatus);
	}

	private void RefreshList()
	{
		lstContacts.Items.Clear();
		shownContacts.Clear();
		List<MailContact> list = (liveMode ? CardDavLiveBookCache.GetLiveContacts() : AddressBookStore.LoadContacts());
		if (list == null)
		{
			list = AddressBookStore.LoadContacts();
		}
		foreach (MailContact item in list)
		{
			if (item != null && !string.IsNullOrWhiteSpace(item.Email))
			{
				shownContacts.Add(item);
				lstContacts.Items.Add(item.ListCaption().ToUpperInvariant());
			}
		}
		string text = (CardDavLiveBookCache.AlwaysOnActive ? " · TIMER ON" : "");
		string text2 = (liveMode ? ("ALWAYS-ON LIVE BOOK" + text) : "LOCAL JSON");
		string text3 = (string.IsNullOrWhiteSpace(CardDavLiveBookCache.LastRefreshSummary) ? "" : (" — " + CardDavLiveBookCache.LastRefreshSummary));
		string text4 = "";
		if (liveMode && DateTime.Compare(CardDavLiveBookCache.LastFetchUtc, DateTime.MinValue) != 0)
		{
			text4 = " @ " + CardDavLiveBookCache.LastFetchUtc.ToLocalTime().ToString("HH:mm:ss");
		}
		int num = 0;
		foreach (MailContact shownContact in shownContacts)
		{
			if (shownContact != null && shownContact.CardDavDirty)
			{
				num = checked(num + 1);
			}
		}
		string text5 = ((num > 0) ? (" — " + num + " DIRTY") : "");
		string text6 = "";
		string serverUrl = null;
		string username = null;
		string password = null;
		if (AddressBookStore.TryLoadCardDavSettings(ref serverUrl, ref username, ref password) && !string.IsNullOrWhiteSpace(serverUrl))
		{
			string text7 = serverUrl.Trim();
			if (text7.Length > 36)
			{
				text7 = text7.Substring(0, 33) + "…";
			}
			text6 = " — " + text7;
		}
		string text8 = "";
		if (MorkAbookParser.LastSourceMabRoundTripValidated)
		{
			text8 = " · MORK SRC OK";
			if (MorkAbookParser.LastSourceMabRoundTripByteIdentical)
			{
				text8 += " BYTE-IDENT";
			}
		}
		if (MorkAbookParser.LastSourceMabRoundTripByteIdentical)
		{
			text8 += " · BYTE-RT OK";
		}
		if (MorkAbookParser.LastMorkPreserveExportFieldValidated)
		{
			text8 += " · MORK-FIELD-RT";
		}
		if (MorkAbookParser.LastMabExportByteIdentical)
		{
			text8 += " · MORK-EXPORT-BYTE";
		}
		if (AddressBookStore.LastCardDavLiveOverlayCount > 0)
		{
			text8 = text8 + " · LIVE-OVERLAY " + AddressBookStore.LastCardDavLiveOverlayCount;
		}
		if (MorkAbookParser.LastMorkColumnKeyCount > 0)
		{
			text8 = text8 + " · COLS " + MorkAbookParser.LastMorkColumnKeyCount;
		}
		if (MorkAbookParser.LastMorkRowPropertyFieldCount > 0)
		{
			text8 = text8 + " · FIELDS " + MorkAbookParser.LastMorkRowPropertyFieldCount;
		}
		if (MorkAbookParser.LastMorkTableCount > 0)
		{
			text8 = text8 + " · TABLES " + MorkAbookParser.LastMorkTableCount;
		}
		if (MorkAbookParser.LastMorkParsedContactCount > 0)
		{
			text8 = text8 + " · ROWS " + MorkAbookParser.LastMorkParsedContactCount;
		}
		if (MorkAbookParser.LastMorkRowMapCount > 0)
		{
			text8 = text8 + " · RMAP " + MorkAbookParser.LastMorkRowMapCount;
		}
		if (CardDavLiveBookCache.AlwaysOnActive && DateTime.Compare(CardDavLiveBookCache.LastAlwaysOnSyncUtc, DateTime.MinValue) != 0)
		{
			text8 = text8 + " · LIVE @" + CardDavLiveBookCache.LastAlwaysOnSyncUtc.ToLocalTime().ToString("HH:mm");
		}
		if (CardDavLiveBookCache.AlwaysOnActive && CardDavLiveBookCache.AlwaysOnCycleCount > 0)
		{
			text8 = text8 + " · CYCLE " + CardDavLiveBookCache.AlwaysOnCycleCount;
		}
		if (CardDavSyncService.Last412ConflictEmail.Length > 0)
		{
			text8 = text8 + " · 412@" + CardDavSyncService.Last412ConflictEmail;
		}
		if (DateTime.Compare(AddressBookStore.LastCardDavBidirectionalUtc, DateTime.MinValue) != 0)
		{
			text8 = text8 + " · CARDDAV @" + AddressBookStore.LastCardDavBidirectionalUtc.ToLocalTime().ToString("HH:mm");
		}
		if (AddressBookStore.LastCardDavDiscoveredBooks > 0)
		{
			text8 = text8 + " · " + AddressBookStore.LastCardDavDiscoveredBooks + " BOOKS";
		}
		if (AddressBookStore.LastCardDav412BatchResolved > 0)
		{
			text8 = text8 + " · 412-FIX " + AddressBookStore.LastCardDav412BatchResolved;
		}
		string text9 = ((CardDavSyncService.ConflictOn412Mode != null) ? CardDavSyncService.ConflictOn412Mode : "").Trim();
		if (text9.Length > 0)
		{
			text8 = text8 + " · 412=" + text9.ToUpperInvariant();
		}
		if (AddressBookStore.LastCardDavSecondaryBooksMerged > 0)
		{
			text8 = text8 + " · 2ND+" + AddressBookStore.LastCardDavSecondaryBooksMerged;
		}
		string text10 = AddressBookStore.TryGetStoredSourceMabPath();
		if (text10.Length > 0)
		{
			string fileName = Path.GetFileName(text10);
			text8 = text8 + " · " + fileName;
		}
		lblStatus.Text = lstContacts.Items.Count + " CONTACT(S) — " + text2 + text4 + text3 + text5 + text8 + text6;
	}

	private void OnLiveRefresh(object sender, EventArgs e)
	{
		liveMode = true;
		RefreshList();
		if (lstContacts.Items.Count == 0)
		{
			lblStatus.Text = ((AddressBookStore.LastError != null) ? AddressBookStore.LastError : "LIVE REFRESH FAILED").ToUpperInvariant();
		}
	}

	private void OnSyncSave(object sender, EventArgs e)
	{
		int num = AddressBookStore.TrySyncStoredCardDavBidirectional();
		if (num < 0)
		{
			lblStatus.Text = ((AddressBookStore.LastError != null) ? AddressBookStore.LastError : "SYNC FAILED").ToUpperInvariant();
			return;
		}
		liveMode = true;
		RefreshList();
		string text = EwsGalWriteProofStore.TryLoadLastProofSummary();
		string text2 = ((text.Length > 0 && text.Length <= 64) ? (" · " + text) : "");
		lblStatus.Text = num + " CONTACT(S) MERGED — LIVE BOOK ACTIVE" + text2;
	}

	private void OnAdd(object sender, EventArgs e)
	{
		using (frmContactEdit frmContactEdit2 = new frmContactEdit())
		{
			if (frmContactEdit2.ShowDialog(this) != DialogResult.OK)
			{
				return;
			}
		}
		RefreshList();
	}

	private void OnToggle412Mode(object sender, EventArgs e)
	{
		if (string.Equals(CardDavSyncService.ConflictOn412Mode, "ServerWins", StringComparison.OrdinalIgnoreCase))
		{
			CardDavSyncService.ConflictOn412Mode = "ClientMerge";
		}
		else
		{
			CardDavSyncService.ConflictOn412Mode = "ServerWins";
		}
		RefreshList();
		lblStatus.Text = "412 POLICY — " + CardDavSyncService.ConflictOn412Mode.ToUpperInvariant();
	}

	private void OnEditSelected(object sender, EventArgs e)
	{
		int selectedIndex = lstContacts.SelectedIndex;
		if (selectedIndex < 0 || selectedIndex >= shownContacts.Count)
		{
			return;
		}
		MailContact mailContact = shownContacts[selectedIndex];
		if (mailContact != null)
		{
			using (frmContactEdit frmContactEdit2 = new frmContactEdit(mailContact))
			{
				frmContactEdit2.ShowDialog(this);
			}
			RefreshList();
		}
	}
}
