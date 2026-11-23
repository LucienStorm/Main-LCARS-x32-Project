using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using LCARS;
using LCARS.Controls;

namespace LCARSMail;

public class frmEwsSync : LCARSForm
{
	private enum EwsListMode
	{
		Mail,
		Calendar,
		Contacts
	}

	private class DevicePollArgs
	{
		public string Tenant;

		public string ClientId;

		public string DeviceCode;

		public int IntervalSeconds;

		[DebuggerNonUserCode]
		public DevicePollArgs()
		{
		}
	}

	private TextBox txtUrl;

	private TextBox txtUser;

	private TextBox txtPassword;

	private TextBox txtOAuth;

	private TextBox txtTenant;

	private TextBox txtClientId;

	private Label lblDeviceHint;

	private readonly BackgroundWorker oauthPollWorker;

	private ListBox lstItems;

	private TextBox txtBody;

	private TextBox txtSendTo;

	private TextBox txtSendSubject;

	private TextBox txtSendBody;

	private Label lblStatus;

	private List<EwsMailService.EwsMailSummary> summaries;

	private List<EwsMailService.EwsCalendarSummary> calendarSummaries;

	private List<EwsMailService.EwsResolvedContact> resolvedContacts;

	private EwsListMode listMode;

	public frmEwsSync()
	{
		listMode = EwsListMode.Mail;
		BackColor = Color.Black;
		Size clientSize = new Size(640, 688);
		ClientSize = clientSize;
		StartPosition = FormStartPosition.CenterParent;
		summaries = new List<EwsMailService.EwsMailSummary>();
		calendarSummaries = new List<EwsMailService.EwsCalendarSummary>();
		resolvedContacts = new List<EwsMailService.EwsResolvedContact>();
		oauthPollWorker = new BackgroundWorker();
		oauthPollWorker.WorkerSupportsCancellation = true;
		oauthPollWorker.DoWork += OAuthPoll_DoWork;
		oauthPollWorker.RunWorkerCompleted += OAuthPoll_Completed;
		BuildUi();
		TryLoadStoredOAuth();
		TryLoadOAuthAppSettings();
		int num = EwsGalSyncStore.TryRefreshLastGalMergedCountFromDisk();
		if (num > 0)
		{
			DateTime t = EwsGalSyncStore.TryGetLastDeepSyncUtc();
			string text = "";
			if (DateTime.Compare(t, DateTime.MinValue) != 0)
			{
				text = " · DEEP " + t.ToLocalTime().ToString("yyyy-MM-dd");
			}
			lblStatus.Text = "LAST GAL MERGE: " + num + " CONTACT(S) (PARTIAL GAL)" + text;
		}
	}

	private void BuildUi()
	{
		AddLabel("EWS URL", 12, 12);
		txtUrl = AddField(12, 32, 610);
		txtUrl.Text = "https://outlook.office365.com/EWS/Exchange.asmx";
		AddLabel("USERNAME", 12, 64);
		txtUser = AddField(12, 84, 610);
		AddLabel("PASSWORD", 12, 116);
		txtPassword = AddField(12, 136, 610);
		txtPassword.UseSystemPasswordChar = true;
		AddLabel("OAUTH TENANT", 12, 168);
		txtTenant = AddField(12, 188, 280);
		txtTenant.Text = "organizations";
		AddLabel("CLIENT ID", 300, 168);
		txtClientId = AddField(300, 188, 322);
		txtClientId.Text = "94933e60-ee10-4250-a050-3efd588b0718";
		AddLabel("OAUTH ACCESS TOKEN", 12, 216);
		txtOAuth = AddField(12, 236, 500);
		txtOAuth.UseSystemPasswordChar = true;
		FlatButton flatButton = new FlatButton();
		flatButton.ButtonText = "DEVICE";
		flatButton.Text = "DEVICE";
		flatButton.Color = LCARScolorStyles.NavigationFunction;
		Size size = new Size(90, 28);
		flatButton.Size = size;
		Point location = new Point(520, 234);
		flatButton.Location = location;
		flatButton.Click += OnDeviceOAuth;
		Controls.Add(flatButton);
		lblDeviceHint = new Label();
		lblDeviceHint.ForeColor = Color.Orange;
		Label label = lblDeviceHint;
		location = new Point(12, 264);
		label.Location = location;
		Label label2 = lblDeviceHint;
		size = new Size(610, 32);
		label2.Size = size;
		lblDeviceHint.Text = "DEVICE LOGIN USES MICROSOFT OAUTH (EWS SCOPE).";
		Controls.Add(lblDeviceHint);
		FlatButton flatButton2 = new FlatButton();
		flatButton2.ButtonText = "INBOX";
		flatButton2.Text = "INBOX";
		flatButton2.Color = LCARScolorStyles.PrimaryFunction;
		size = new Size(90, 28);
		flatButton2.Size = size;
		location = new Point(12, 300);
		flatButton2.Location = location;
		flatButton2.Click += OnSync;
		Controls.Add(flatButton2);
		FlatButton flatButton3 = new FlatButton();
		flatButton3.ButtonText = "CAL";
		flatButton3.Text = "CAL";
		flatButton3.Color = LCARScolorStyles.NavigationFunction;
		size = new Size(72, 28);
		flatButton3.Size = size;
		location = new Point(108, 300);
		flatButton3.Location = location;
		flatButton3.Click += OnCalendar;
		Controls.Add(flatButton3);
		FlatButton flatButton4 = new FlatButton();
		flatButton4.ButtonText = "CONTACTS";
		flatButton4.Text = "CONTACTS";
		flatButton4.Color = LCARScolorStyles.NavigationFunction;
		size = new Size(90, 28);
		flatButton4.Size = size;
		location = new Point(186, 300);
		flatButton4.Location = location;
		flatButton4.Click += OnContacts;
		Controls.Add(flatButton4);
		FlatButton flatButton5 = new FlatButton();
		flatButton5.ButtonText = "MERGE CAL";
		flatButton5.Text = "MERGE CAL";
		flatButton5.Color = LCARScolorStyles.SystemFunction;
		size = new Size(100, 28);
		flatButton5.Size = size;
		location = new Point(284, 300);
		flatButton5.Location = location;
		flatButton5.Click += OnMergeCalendar;
		Controls.Add(flatButton5);
		FlatButton flatButton6 = new FlatButton();
		flatButton6.ButtonText = "MERGE ABOOK";
		flatButton6.Text = "MERGE ABOOK";
		flatButton6.Color = LCARScolorStyles.SystemFunction;
		size = new Size(110, 28);
		flatButton6.Size = size;
		location = new Point(392, 300);
		flatButton6.Location = location;
		flatButton6.Click += OnMergeAddressBook;
		Controls.Add(flatButton6);
		FlatButton flatButton7 = new FlatButton();
		flatButton7.ButtonText = "SYNC GAL";
		flatButton7.Text = "SYNC GAL";
		flatButton7.Color = LCARScolorStyles.SystemFunction;
		size = new Size(90, 28);
		flatButton7.Size = size;
		location = new Point(106, 332);
		flatButton7.Location = location;
		flatButton7.Click += OnSyncGal;
		Controls.Add(flatButton7);
		FlatButton flatButton8 = new FlatButton();
		flatButton8.ButtonText = "CAL FIX";
		flatButton8.Text = "CAL FIX";
		flatButton8.Color = LCARScolorStyles.Orange;
		size = new Size(80, 28);
		flatButton8.Size = size;
		location = new Point(202, 332);
		flatButton8.Location = location;
		flatButton8.Click += OnCalendarConflictFix;
		Controls.Add(flatButton8);
		FlatButton flatButton9 = new FlatButton();
		flatButton9.ButtonText = "ADD MAIL";
		flatButton9.Text = "ADD MAIL";
		flatButton9.Color = LCARScolorStyles.PrimaryFunction;
		size = new Size(90, 28);
		flatButton9.Size = size;
		location = new Point(508, 300);
		flatButton9.Location = location;
		flatButton9.Click += OnAddToMail;
		Controls.Add(flatButton9);
		lstItems = new ListBox();
		ListBox listBox = lstItems;
		location = new Point(12, 368);
		listBox.Location = location;
		ListBox listBox2 = lstItems;
		size = new Size(300, 160);
		listBox2.Size = size;
		lstItems.BackColor = Color.FromArgb(30, 30, 30);
		lstItems.ForeColor = Color.Orange;
		lstItems.SelectedIndexChanged += OnItemSelected;
		Controls.Add(lstItems);
		txtBody = new TextBox();
		txtBody.Multiline = true;
		txtBody.ScrollBars = ScrollBars.Vertical;
		TextBox textBox = txtBody;
		location = new Point(322, 368);
		textBox.Location = location;
		TextBox textBox2 = txtBody;
		size = new Size(300, 160);
		textBox2.Size = size;
		txtBody.BackColor = Color.FromArgb(30, 30, 30);
		txtBody.ForeColor = Color.Orange;
		Controls.Add(txtBody);
		AddLabel("SEND TO", 12, 504);
		txtSendTo = AddField(12, 524, 610);
		AddLabel("SUBJECT", 12, 552);
		txtSendSubject = AddField(12, 572, 610);
		AddLabel("BODY", 12, 600);
		txtSendBody = AddField(12, 620, 450);
		FlatButton flatButton10 = new FlatButton();
		flatButton10.ButtonText = "SEND";
		flatButton10.Text = "SEND";
		flatButton10.Color = LCARScolorStyles.SystemFunction;
		size = new Size(72, 28);
		flatButton10.Size = size;
		location = new Point(470, 618);
		flatButton10.Location = location;
		flatButton10.Click += OnSend;
		Controls.Add(flatButton10);
		lblStatus = new Label();
		lblStatus.ForeColor = Color.Orange;
		Label label3 = lblStatus;
		location = new Point(12, 656);
		label3.Location = location;
		Label label4 = lblStatus;
		size = new Size(610, 24);
		label4.Size = size;
		Controls.Add(lblStatus);
	}

	private void OnAddToMail(object sender, EventArgs e)
	{
		string text = OAuthToken();
		if (!string.IsNullOrWhiteSpace(text))
		{
			EwsOAuthStore.SaveToken(text);
		}
		MailAccount mailAccount = EwsMailBridge.TryRegisterAccount(txtUrl.Text, txtUser.Text, txtPassword.Text, txtUser.Text);
		if (mailAccount == null)
		{
			lblStatus.Text = ((AccountStore.LastError != null) ? AccountStore.LastError : "ADD MAIL FAILED").ToUpperInvariant();
		}
		else
		{
			lblStatus.Text = "EWS ACCOUNT ADDED — OPEN MAIL REFRESH";
		}
	}

	private void OnSync(object sender, EventArgs e)
	{
		listMode = EwsListMode.Mail;
		lstItems.Items.Clear();
		txtBody.Clear();
		summaries = EwsMailService.TryFindInboxSummaries(txtUrl.Text, txtUser.Text, txtPassword.Text, OAuthToken());
		if (summaries == null)
		{
			lblStatus.Text = ((EwsMailService.LastError != null) ? EwsMailService.LastError : "EWS FAILED").ToUpperInvariant();
			return;
		}
		foreach (EwsMailService.EwsMailSummary summary in summaries)
		{
			ListBox.ObjectCollection items = lstItems.Items;
			EwsMailService.EwsMailSummary ewsMailSummary = summary;
			items.Add((ewsMailSummary.Subject != null) ? ewsMailSummary.Subject : summary.ItemId);
		}
		lblStatus.Text = summaries.Count + " MESSAGE(S)";
	}

	private void OnCalendar(object sender, EventArgs e)
	{
		listMode = EwsListMode.Calendar;
		lstItems.Items.Clear();
		txtBody.Clear();
		calendarSummaries = EwsMailService.TryFindCalendarSummaries(txtUrl.Text, txtUser.Text, txtPassword.Text, OAuthToken());
		if (calendarSummaries == null)
		{
			lblStatus.Text = ((EwsMailService.LastError != null) ? EwsMailService.LastError : "EWS CALENDAR FAILED").ToUpperInvariant();
			return;
		}
		foreach (EwsMailService.EwsCalendarSummary calendarSummary in calendarSummaries)
		{
			EwsMailService.EwsCalendarSummary ewsCalendarSummary = calendarSummary;
			string text = ((ewsCalendarSummary.Subject != null) ? ewsCalendarSummary.Subject : calendarSummary.ItemId);
			if (!string.IsNullOrWhiteSpace(calendarSummary.StartText))
			{
				text = text + " @ " + calendarSummary.StartText;
			}
			lstItems.Items.Add(text);
		}
		lblStatus.Text = calendarSummaries.Count + " CALENDAR ITEM(S)";
	}

	private void OnMergeCalendar(object sender, EventArgs e)
	{
		int num = EwsLcarsBridge.TrySyncEwsCalendarBidirectional(txtUrl.Text, txtUser.Text, txtPassword.Text, OAuthToken());
		if (num < 0)
		{
			lblStatus.Text = ((EwsLcarsBridge.LastError != null) ? EwsLcarsBridge.LastError : "CAL 2-WAY FAILED").ToUpperInvariant();
			return;
		}
		string text = "";
		if (EwsLcarsBridge.LastCalendarPushCount > 0)
		{
			text = " · PUSHED " + EwsLcarsBridge.LastCalendarPushCount;
		}
		string text2 = "";
		if (EwsLcarsBridge.LastCalendarConflictOverwrites > 0)
		{
			text2 = " · OVERWRITES " + EwsLcarsBridge.LastCalendarConflictOverwrites;
		}
		int num2 = EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents();
		if (EwsLcarsBridge.LastCalendarAutoServerWins > 0)
		{
			text2 = text2 + " · SRV-WIN " + EwsLcarsBridge.LastCalendarAutoServerWins;
		}
		if (EwsLcarsBridge.LastCalendarAutoClientWins > 0)
		{
			text2 = text2 + " · CL-WIN " + EwsLcarsBridge.LastCalendarAutoClientWins;
		}
		if (EwsLcarsBridge.LastCalendarAutoReconcileCleared > 0)
		{
			text2 = text2 + " · RECON-CLEARED " + EwsLcarsBridge.LastCalendarAutoReconcileCleared;
		}
		if (EwsBackgroundSync.LastBackgroundCalAutoReconcile > 0)
		{
			text2 = text2 + " · BG-RECON " + EwsBackgroundSync.LastBackgroundCalAutoReconcile;
		}
		if (EwsBackgroundSync.LastBackgroundCalChangeKeyRefreshes > 0)
		{
			text2 = text2 + " · BG-CK " + EwsBackgroundSync.LastBackgroundCalChangeKeyRefreshes;
		}
		if (EwsBackgroundSync.LastBackgroundCalPushAfterCk > 0)
		{
			text2 = text2 + " · BG-PUSH-CAL " + EwsBackgroundSync.LastBackgroundCalPushAfterCk;
		}
		if (EwsBackgroundSync.LastBackgroundCalBidirectionalThirdPass > 0)
		{
			text2 = text2 + " · BG-CAL-3 " + EwsBackgroundSync.LastBackgroundCalBidirectionalThirdPass;
		}
		if (EwsBackgroundSync.LastBackgroundCalPushAfterThirdPass > 0)
		{
			text2 = text2 + " · BG-PUSH-CAL-2 " + EwsBackgroundSync.LastBackgroundCalPushAfterThirdPass;
		}
		if (EwsBackgroundSync.LastBackgroundCalBidirectionalFourthPass > 0)
		{
			text2 = text2 + " · BG-CAL-4 " + EwsBackgroundSync.LastBackgroundCalBidirectionalFourthPass;
		}
		if (EwsBackgroundSync.LastBackgroundCalAutoReconcileSecondPass > 0)
		{
			text2 = text2 + " · BG-RECON-2 " + EwsBackgroundSync.LastBackgroundCalAutoReconcileSecondPass;
		}
		if (EwsBackgroundSync.LastBackgroundCalChangeKeyRefreshesSecondPass > 0)
		{
			text2 = text2 + " · BG-CK-2 " + EwsBackgroundSync.LastBackgroundCalChangeKeyRefreshesSecondPass;
		}
		if (EwsBackgroundSync.LastBackgroundCalPushAfterFourthPass > 0)
		{
			text2 = text2 + " · BG-PUSH-CAL-3 " + EwsBackgroundSync.LastBackgroundCalPushAfterFourthPass;
		}
		if (EwsBackgroundSync.LastBackgroundCalBidirectionalFifthPass > 0)
		{
			text2 = text2 + " · BG-CAL-5 " + EwsBackgroundSync.LastBackgroundCalBidirectionalFifthPass;
		}
		if (EwsBackgroundSync.LastBackgroundCalAutoReconcileThirdPass > 0)
		{
			text2 = text2 + " · BG-RECON-3 " + EwsBackgroundSync.LastBackgroundCalAutoReconcileThirdPass;
		}
		if (EwsBackgroundSync.LastBackgroundCalPushAfterFifthPass > 0)
		{
			text2 = text2 + " · BG-PUSH-CAL-4 " + EwsBackgroundSync.LastBackgroundCalPushAfterFifthPass;
		}
		if (EwsBackgroundSync.LastBackgroundCalBidirectionalSixthPass > 0)
		{
			text2 = text2 + " · BG-CAL-6 " + EwsBackgroundSync.LastBackgroundCalBidirectionalSixthPass;
		}
		if (EwsBackgroundSync.LastBackgroundCalAutoReconcileFourthPass > 0)
		{
			text2 = text2 + " · BG-RECON-4 " + EwsBackgroundSync.LastBackgroundCalAutoReconcileFourthPass;
		}
		if (EwsBackgroundSync.LastBackgroundCalChangeKeyRefreshesThirdPass > 0)
		{
			text2 = text2 + " · BG-CK-3 " + EwsBackgroundSync.LastBackgroundCalChangeKeyRefreshesThirdPass;
		}
		if (EwsBackgroundSync.LastBackgroundCalPushAfterSixthPass > 0)
		{
			text2 = text2 + " · BG-PUSH-CAL-5 " + EwsBackgroundSync.LastBackgroundCalPushAfterSixthPass;
		}
		if (EwsBackgroundSync.LastBackgroundCalBidirectionalSeventhPass > 0)
		{
			text2 = text2 + " · BG-CAL-7 " + EwsBackgroundSync.LastBackgroundCalBidirectionalSeventhPass;
		}
		if (EwsBackgroundSync.LastBackgroundCalBidirectionalEighthPass > 0)
		{
			text2 = text2 + " · BG-CAL-8 " + EwsBackgroundSync.LastBackgroundCalBidirectionalEighthPass;
		}
		if (num2 > 0)
		{
			text2 = text2 + " · EWS-DIRTY " + num2 + " (NOT CONFLICT-SAFE REAL)";
		}
		if (EwsLcarsBridge.LastCalendarRsvpPushCount > 0)
		{
			text = text + " · RSVP " + EwsLcarsBridge.LastCalendarRsvpPushCount;
		}
		if (EwsLcarsBridge.LastCalendarChangeKeyRefreshes > 0)
		{
			text = text + " · CK-REF " + EwsLcarsBridge.LastCalendarChangeKeyRefreshes;
		}
		int num3 = AddressBookStore.CountGalPushDirtyContacts();
		if (num3 > 0)
		{
			text = text + " · GAL-DIRTY " + num3;
		}
		lblStatus.Text = num + " CAL 2-WAY (PARTIAL — SEE open-slice-acceptance-gaps-340.md)" + text + text2;
	}

	private void OnCalendarConflictFix(object sender, EventArgs e)
	{
		int num = EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents();
		string text = LcarsCommsUi.Prompt("Calendar conflict merge (partial). Type SERVER to pull EWS over local dirty events, or LOCAL to force push.", "LCARS COMMS — EWS CAL FIX", (num > 0) ? "SERVER" : "LOCAL").Trim();
		if (text.Length != 0)
		{
			if (text.Equals("SERVER", StringComparison.OrdinalIgnoreCase))
			{
				int num2 = EwsCalendarConflictHelper.TryRefreshAllDirtyFromServer(txtUrl.Text, txtUser.Text, txtPassword.Text, OAuthToken());
				lblStatus.Text = num2 + " EVENT(S) REFRESHED FROM SERVER (PARTIAL MERGE UX)";
			}
			else if (text.Equals("LOCAL", StringComparison.OrdinalIgnoreCase))
			{
				int num3 = EwsLcarsBridge.TryPushDirtyCalendarEventsToEws(txtUrl.Text, txtUser.Text, txtPassword.Text, OAuthToken());
				lblStatus.Text = num3 + " LOCAL PUSH(ES) · OVERWRITES " + EwsLcarsBridge.LastCalendarConflictOverwrites;
			}
			else
			{
				lblStatus.Text = "CAL FIX: TYPE SERVER OR LOCAL";
				LcarsCommsUi.Alert("Type SERVER or LOCAL.", "LCARS COMMS — EWS CAL FIX");
			}
		}
	}

	private void OnMergeAddressBook(object sender, EventArgs e)
	{
		string text = LcarsCommsUi.Prompt("Search name or email to merge:", "LCARS COMMS — EWS ABOOK", (txtUser != null) ? txtUser.Text : "");
		if (!string.IsNullOrWhiteSpace(text))
		{
			int num = EwsLcarsBridge.TryMergeContactsFromEws(txtUrl.Text, txtUser.Text, txtPassword.Text, text, OAuthToken());
			if (num < 0)
			{
				string prompt = ((EwsLcarsBridge.LastError != null) ? EwsLcarsBridge.LastError : "MERGE ABOOK FAILED").ToUpperInvariant();
				lblStatus.Text = prompt;
				LcarsCommsUi.Alert(prompt, "LCARS COMMS — EWS ERROR");
			}
			else
			{
				lblStatus.Text = num + " CONTACT(S) MERGED TO ABOOK";
			}
		}
	}

	private void OnSyncGal(object sender, EventArgs e)
	{
		lblStatus.Text = "GAL HARVEST RUNNING…";
		Application.DoEvents();
		int num = EwsLcarsBridge.TrySyncGalDirectoryBidirectional(txtUrl.Text, txtUser.Text, txtPassword.Text, OAuthToken(), deepGalHarvest: true);
		if (num < 0)
		{
			lblStatus.Text = ((EwsLcarsBridge.LastError != null) ? EwsLcarsBridge.LastError : "SYNC GAL FAILED").ToUpperInvariant();
			return;
		}
		string text = "";
		if (EwsLcarsBridge.LastGalPersonalPushCount > 0)
		{
			text = " · PUSH " + EwsLcarsBridge.LastGalPersonalPushCount;
		}
		if (EwsLcarsBridge.LastGalPersonaUpdateAttempts > 0)
		{
			text = text + " · PERSONA API " + EwsLcarsBridge.LastGalPersonaUpdateAttempts;
			if (EwsMailService.LastGalPersonaUpdateDenied)
			{
				text += " (DENIED)";
			}
		}
		if (EwsLcarsBridge.LastGalDirectoryUpdateAttempts > 0)
		{
			text = text + " · DIR CONTACT " + EwsLcarsBridge.LastGalDirectoryUpdateAttempts;
			if (EwsMailService.LastGalDirectoryUpdateDenied)
			{
				text += " (DENIED)";
			}
		}
		if (EwsLcarsBridge.LastGalChangeKeyRefreshes > 0)
		{
			text = text + " · GAL-CK " + EwsLcarsBridge.LastGalChangeKeyRefreshes;
		}
		if (EwsLcarsBridge.LastGalPersonaChangeKeyRefreshes > 0)
		{
			text = text + " · PERSONA-CK " + EwsLcarsBridge.LastGalPersonaChangeKeyRefreshes;
		}
		if (EwsLcarsBridge.LastGalDirectoryChangeKeyRefreshes > 0)
		{
			text = text + " · DIR-CK " + EwsLcarsBridge.LastGalDirectoryChangeKeyRefreshes;
		}
		if (EwsLcarsBridge.LastGalDirectoryResolveSuccess > 0)
		{
			text = text + " · RESOLVE " + EwsLcarsBridge.LastGalDirectoryResolveSuccess;
		}
		if (EwsBackgroundSync.LastBackgroundGalPushRetry > 0)
		{
			text = text + " · BG-PUSH " + EwsBackgroundSync.LastBackgroundGalPushRetry;
		}
		if (EwsBackgroundSync.LastBackgroundGalPushSecondRetry > 0)
		{
			text = text + " · BG-GAL-2 " + EwsBackgroundSync.LastBackgroundGalPushSecondRetry;
		}
		if (EwsBackgroundSync.LastBackgroundGalPushThirdRetry > 0)
		{
			text = text + " · BG-GAL-3 " + EwsBackgroundSync.LastBackgroundGalPushThirdRetry;
		}
		if (EwsBackgroundSync.LastBackgroundGalPushFourthRetry > 0)
		{
			text = text + " · BG-GAL-4 " + EwsBackgroundSync.LastBackgroundGalPushFourthRetry;
		}
		if (EwsBackgroundSync.LastBackgroundGalPushFifthRetry > 0)
		{
			text = text + " · BG-GAL-5 " + EwsBackgroundSync.LastBackgroundGalPushFifthRetry;
		}
		if (EwsBackgroundSync.LastBackgroundGalPushSixthRetry > 0)
		{
			text = text + " · BG-GAL-6 " + EwsBackgroundSync.LastBackgroundGalPushSixthRetry;
		}
		if (EwsBackgroundSync.LastBackgroundGalPushSeventhRetry > 0)
		{
			text = text + " · BG-GAL-7 " + EwsBackgroundSync.LastBackgroundGalPushSeventhRetry;
		}
		if (EwsBackgroundSync.LastBackgroundGalPushEighthRetry > 0)
		{
			text = text + " · BG-GAL-8 " + EwsBackgroundSync.LastBackgroundGalPushEighthRetry;
		}
		if (EwsBackgroundSync.LastBackgroundGalPushNinthRetry > 0)
		{
			text = text + " · BG-GAL-9 " + EwsBackgroundSync.LastBackgroundGalPushNinthRetry;
		}
		string text2 = EwsGalWriteProofStore.TryLoadLastProofSummary();
		if (text2.Length > 0)
		{
			text2 = " · " + text2;
		}
		string text3 = EwsGalWriteProofStore.TryGetDominantWritePath();
		if (text3.Length > 0)
		{
			text2 = text2 + " · DOM " + text3.ToUpperInvariant();
		}
		if (EwsGalWriteProofStore.LastProof != null)
		{
			int num2 = checked(EwsGalWriteProofStore.LastProof.ShadowUpdateSuccess + EwsGalWriteProofStore.LastProof.ShadowCreateSuccess);
			if (num2 > 0)
			{
				text2 = text2 + " · SHADOW " + num2;
			}
		}
		lblStatus.Text = num + " GAL 2-WAY (PERSONA UPDATE + SHADOW — NOT FULL GAL WRITE)" + text + text2;
	}

	private void OnContacts(object sender, EventArgs e)
	{
		listMode = EwsListMode.Contacts;
		string text = LcarsCommsUi.Prompt("Search name or email:", "LCARS COMMS — EWS CONTACTS", (txtUser != null) ? txtUser.Text : "");
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		lstItems.Items.Clear();
		txtBody.Clear();
		resolvedContacts = EwsMailService.TryResolveContacts(txtUrl.Text, txtUser.Text, txtPassword.Text, text, OAuthToken());
		if (resolvedContacts == null)
		{
			string prompt = ((EwsMailService.LastError != null) ? EwsMailService.LastError : "EWS CONTACTS FAILED").ToUpperInvariant();
			lblStatus.Text = prompt;
			LcarsCommsUi.Alert(prompt, "LCARS COMMS — EWS ERROR");
			return;
		}
		foreach (EwsMailService.EwsResolvedContact resolvedContact in resolvedContacts)
		{
			ListBox.ObjectCollection items = lstItems.Items;
			EwsMailService.EwsResolvedContact ewsResolvedContact = resolvedContact;
			items.Add(((ewsResolvedContact.DisplayName != null) ? ewsResolvedContact.DisplayName : resolvedContact.EmailAddress) + " <" + resolvedContact.EmailAddress + ">");
		}
		lblStatus.Text = resolvedContacts.Count + " CONTACT(S)";
	}

	private void OnItemSelected(object sender, EventArgs e)
	{
		txtBody.Clear();
		int selectedIndex = lstItems.SelectedIndex;
		if (selectedIndex < 0)
		{
			return;
		}
		if (listMode == EwsListMode.Calendar)
		{
			if (calendarSummaries != null && selectedIndex < calendarSummaries.Count)
			{
				EwsMailService.EwsCalendarSummary ewsCalendarSummary = calendarSummaries[selectedIndex];
				TextBox textBox = txtBody;
				EwsMailService.EwsCalendarSummary ewsCalendarSummary2 = ewsCalendarSummary;
				textBox.Text = ((ewsCalendarSummary2.StartText != null) ? ewsCalendarSummary2.StartText : "") + "\r\n" + ewsCalendarSummary.ItemId;
				if (txtSendSubject != null)
				{
					TextBox textBox2 = txtSendSubject;
					ewsCalendarSummary2 = ewsCalendarSummary;
					textBox2.Text = ((ewsCalendarSummary2.Subject != null) ? ewsCalendarSummary2.Subject : "");
				}
				lblStatus.Text = "CALENDAR ITEM";
			}
		}
		else if (listMode == EwsListMode.Contacts)
		{
			if (resolvedContacts != null && selectedIndex < resolvedContacts.Count)
			{
				EwsMailService.EwsResolvedContact ewsResolvedContact = resolvedContacts[selectedIndex];
				txtBody.Text = ewsResolvedContact.EmailAddress;
				if (txtSendTo != null)
				{
					txtSendTo.Text = ewsResolvedContact.EmailAddress;
				}
				lblStatus.Text = "CONTACT SELECTED";
			}
		}
		else
		{
			if (summaries == null || selectedIndex >= summaries.Count)
			{
				return;
			}
			EwsMailService.EwsMailSummary ewsMailSummary = summaries[selectedIndex];
			if (ewsMailSummary == null || string.IsNullOrWhiteSpace(ewsMailSummary.ItemId))
			{
				return;
			}
			EwsMailService.EwsMessageBody ewsMessageBody = EwsMailService.TryGetItemBody(txtUrl.Text, txtUser.Text, txtPassword.Text, ewsMailSummary.ItemId, OAuthToken());
			if (ewsMessageBody == null)
			{
				lblStatus.Text = ((EwsMailService.LastError != null) ? EwsMailService.LastError : "GET ITEM FAILED").ToUpperInvariant();
				return;
			}
			TextBox textBox3 = txtBody;
			EwsMailService.EwsMessageBody ewsMessageBody2 = ewsMessageBody;
			textBox3.Text = ((ewsMessageBody2.BodyText != null) ? ewsMessageBody2.BodyText : "");
			if (txtSendSubject != null && txtSendSubject.Text.Length == 0)
			{
				TextBox textBox4 = txtSendSubject;
				ewsMessageBody2 = ewsMessageBody;
				textBox4.Text = ((ewsMessageBody2.Subject != null) ? ewsMessageBody2.Subject : ewsMailSummary.Subject);
			}
			lblStatus.Text = "MESSAGE LOADED";
		}
	}

	private void OnSend(object sender, EventArgs e)
	{
		if (EwsMailService.TrySendMessage(txtUrl.Text, txtUser.Text, txtPassword.Text, txtSendTo.Text, txtSendSubject.Text, txtSendBody.Text, OAuthToken()))
		{
			lblStatus.Text = "SENT";
			OnSync(null, EventArgs.Empty);
		}
		else
		{
			lblStatus.Text = ((EwsMailService.LastError != null) ? EwsMailService.LastError : "SEND FAILED").ToUpperInvariant();
		}
	}

	private void TryLoadStoredOAuth()
	{
		string token = null;
		if (EwsOAuthStore.TryLoadToken(ref token) && txtOAuth != null)
		{
			txtOAuth.Text = token;
		}
	}

	private void TryLoadOAuthAppSettings()
	{
		string tenant = null;
		string clientId = null;
		EwsOAuthClientStore.TryLoadSettings(ref tenant, ref clientId);
		if (txtTenant != null && !string.IsNullOrWhiteSpace(tenant))
		{
			txtTenant.Text = tenant;
		}
		if (txtClientId != null && !string.IsNullOrWhiteSpace(clientId))
		{
			txtClientId.Text = clientId;
		}
	}

	private void OnDeviceOAuth(object sender, EventArgs e)
	{
		if (oauthPollWorker.IsBusy)
		{
			lblStatus.Text = "DEVICE LOGIN IN PROGRESS";
			return;
		}
		EwsOAuthClientStore.SaveSettings(txtTenant.Text, txtClientId.Text);
		EwsOAuthDeviceFlow.DeviceStartResult deviceStartResult = EwsOAuthDeviceFlow.TryStartDeviceFlow(txtTenant.Text, txtClientId.Text);
		if (deviceStartResult == null)
		{
			string prompt = ((EwsOAuthDeviceFlow.LastError != null) ? EwsOAuthDeviceFlow.LastError : "DEVICE START FAILED").ToUpperInvariant();
			lblStatus.Text = prompt;
			LcarsCommsUi.Alert(prompt, "LCARS COMMS — EWS DEVICE LOGIN");
			return;
		}
		EwsOAuthDeviceFlow.DeviceStartResult deviceStartResult2 = deviceStartResult;
		string text = ((deviceStartResult2.Message != null) ? deviceStartResult2.Message : "");
		if (text.Length == 0)
		{
			text = "Go to " + deviceStartResult.VerificationUri + " and enter code: " + deviceStartResult.UserCode;
		}
		lblDeviceHint.Text = text.ToUpperInvariant();
		if (!string.IsNullOrWhiteSpace(deviceStartResult.VerificationUri) && !LcarsCommsUi.OpenInLcarsBrowser(deviceStartResult.VerificationUri))
		{
			lblStatus.Text = "BROWSER OPEN FAILED — USE URL BELOW";
		}
		deviceStartResult2 = deviceStartResult;
		string text2 = "DEVICE CODE: " + ((deviceStartResult2.UserCode != null) ? deviceStartResult2.UserCode : "");
		if (!string.IsNullOrWhiteSpace(deviceStartResult.UserCode))
		{
			LcarsCommsUi.Alert(text2 + "\r\n\r\nLCARS Web Browser should open the sign-in page.\r\nEnter the code above if it is not filled in automatically.\r\n\r\n" + text, "LCARS COMMS — EWS DEVICE LOGIN");
		}
		else
		{
			LcarsCommsUi.Alert(text, "LCARS COMMS — EWS DEVICE LOGIN");
		}
		lblStatus.Text = "WAITING FOR DEVICE APPROVAL…";
		oauthPollWorker.RunWorkerAsync(new DevicePollArgs
		{
			Tenant = txtTenant.Text,
			ClientId = txtClientId.Text,
			DeviceCode = deviceStartResult.DeviceCode,
			IntervalSeconds = deviceStartResult.IntervalSeconds
		});
	}

	private void OAuthPoll_DoWork(object sender, DoWorkEventArgs e)
	{
		if (!(e.Argument is DevicePollArgs devicePollArgs))
		{
			return;
		}
		BackgroundWorker backgroundWorker = sender as BackgroundWorker;
		int num = Math.Max(5, devicePollArgs.IntervalSeconds);
		int num2 = 0;
		checked
		{
			do
			{
				if (backgroundWorker != null && backgroundWorker.CancellationPending)
				{
					e.Cancel = true;
					return;
				}
				string accessToken = null;
				string refreshToken = null;
				switch (EwsOAuthDeviceFlow.TryPollAccessToken(devicePollArgs.Tenant, devicePollArgs.ClientId, devicePollArgs.DeviceCode, ref accessToken, ref refreshToken))
				{
				case EwsOAuthDeviceFlow.TokenPollResult.Success:
					if (!string.IsNullOrWhiteSpace(refreshToken))
					{
						EwsOAuthStore.SaveRefreshToken(refreshToken);
					}
					e.Result = accessToken;
					return;
				case EwsOAuthDeviceFlow.TokenPollResult.Failed:
					e.Result = null;
					return;
				}
				Thread.Sleep(num * 1000);
				num2++;
			}
			while (num2 <= 180);
			EwsOAuthDeviceFlow.LastError = "Device login timed out.";
			e.Result = null;
		}
	}

	private void OAuthPoll_Completed(object sender, RunWorkerCompletedEventArgs e)
	{
		if (e.Cancelled)
		{
			lblStatus.Text = "DEVICE LOGIN CANCELLED";
			return;
		}
		string text = e.Result as string;
		if (string.IsNullOrWhiteSpace(text))
		{
			string prompt = ((EwsOAuthDeviceFlow.LastError != null) ? EwsOAuthDeviceFlow.LastError : "DEVICE LOGIN FAILED").ToUpperInvariant();
			lblStatus.Text = prompt;
			LcarsCommsUi.Alert(prompt, "LCARS COMMS — EWS DEVICE LOGIN");
		}
		else
		{
			txtOAuth.Text = text;
			EwsOAuthStore.SaveToken(text);
			lblStatus.Text = "OAUTH TOKEN RECEIVED";
			LcarsCommsUi.Alert("OAuth token received. Device login complete.", "LCARS COMMS — EWS DEVICE LOGIN");
		}
	}

	private string OAuthToken()
	{
		if (txtOAuth == null)
		{
			return null;
		}
		string text = txtOAuth.Text.Trim();
		if (text.Length > 0)
		{
			EwsOAuthStore.SaveToken(text);
			return text;
		}
		string accessToken = null;
		if (EwsOAuthStore.TryEnsureAccessToken(ref accessToken, txtTenant.Text, txtClientId.Text))
		{
			txtOAuth.Text = accessToken;
			return accessToken;
		}
		return null;
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
