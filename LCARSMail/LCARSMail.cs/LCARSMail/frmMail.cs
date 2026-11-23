using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using LCARS;
using LCARS.Controls;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

public class frmMail : LCARSForm
{
	private enum CommsMode
	{
		Mail,
		Chat,
		Rss,
		Calendar
	}

	private class MessageRowTag
	{
		public MailMessageHeader Header;

		public MailAccount Account;

		public string ThreadKey;

		public int ThreadCount;

		[DebuggerNonUserCode]
		public MessageRowTag()
		{
		}
	}

	private class FolderRefreshEntry
	{
		public MailAccount Account;

		public string Password;

		[DebuggerNonUserCode]
		public FolderRefreshEntry()
		{
		}
	}

	private class FolderWorkerArgs
	{
		public List<FolderRefreshEntry> Entries;

		[DebuggerNonUserCode]
		public FolderWorkerArgs()
		{
		}
	}

	private class AccountFolderSection
	{
		public MailAccount Account;

		public List<MailFolderInfo> Folders;

		public string ErrorText;

		[DebuggerNonUserCode]
		public AccountFolderSection()
		{
		}
	}

	private class FolderRowTag
	{
		public MailAccount Account;

		public MailFolderInfo Folder;

		public bool Flip;

		public bool IsUnifiedInbox;

		[DebuggerNonUserCode]
		public FolderRowTag()
		{
		}
	}

	private class HeaderWorkerArgs
	{
		public MailAccount Account;

		public string Password;

		public string Folder;

		public bool Unified;

		public List<FolderRefreshEntry> Entries;

		[DebuggerNonUserCode]
		public HeaderWorkerArgs()
		{
		}
	}

	private class HeaderWorkerResult
	{
		public List<MailMessageHeader> Headers;

		public string Folder;

		public string ErrorText;

		public string FilterNote;

		[DebuggerNonUserCode]
		public HeaderWorkerResult()
		{
		}
	}

	private class BodyWorkerArgs
	{
		public MailAccount Account;

		public string Password;

		public string Folder;

		public ulong Uid;

		public MailMessageHeader Header;

		[DebuggerNonUserCode]
		public BodyWorkerArgs()
		{
		}
	}

	private class BodyWorkerResult
	{
		public MailMessageBody Body;

		public MailMessageHeader Header;

		public string ErrorText;

		[DebuggerNonUserCode]
		public BodyWorkerResult()
		{
		}
	}

	private class MutationWorkerArgs
	{
		public string Kind;

		public MailAccount Account;

		public string Password;

		public string Folder;

		public string DestFolder;

		public List<ulong> Uids;

		[DebuggerNonUserCode]
		public MutationWorkerArgs()
		{
		}
	}

	private class MutationWorkerResult
	{
		public string Kind;

		public string ErrorText;

		public int CachePurgedCount;

		[DebuggerNonUserCode]
		public MutationWorkerResult()
		{
		}
	}

	private class AttachmentWorkerArgs
	{
		public MailAccount Account;

		public string Password;

		public string Folder;

		public ulong Uid;

		public int PartIndex;

		public string FileName;

		public string DestPath;

		[DebuggerNonUserCode]
		public AttachmentWorkerArgs()
		{
		}
	}

	private class AttachmentWorkerResult
	{
		public int PartIndex;

		public string FileName;

		public string Folder;

		public ulong Uid;

		public string AccountId;

		public string DestPath;

		public string ErrorText;

		[DebuggerNonUserCode]
		public AttachmentWorkerResult()
		{
		}
	}

	private class GlobalSearchArgs
	{
		public MailAccount Account;

		public string Password;

		public string Query;

		public bool ImapFolderOnly;

		public string Folder;

		[DebuggerNonUserCode]
		public GlobalSearchArgs()
		{
		}
	}

	private class GlobalSearchResult
	{
		public List<MailMessageHeader> Headers;

		public string ErrorText;

		[DebuggerNonUserCode]
		public GlobalSearchResult()
		{
		}
	}

	private class ThreadContextListItem
	{
		public readonly MailMessageHeader Header;

		private readonly string caption;

		public ThreadContextListItem(MailMessageHeader hdr, string text)
		{
			Header = hdr;
			caption = text;
		}

		public override string ToString()
		{
			return caption;
		}
	}

	private sealed class MailFolderPickerDialog : LCARSForm
	{
		private const int RowHeight = 36;

		private const int RowGap = 4;

		private const int TopChrome = 44;

		private const int BottomBar = 44;

		private readonly LcarsScrollPanel folderHost;

		private readonly FlatButton fbCancel;

		private string selectedFullName;

		public string SelectedFolderFullName => selectedFullName;

		public MailFolderPickerDialog(IList<MailFolderInfo> folders, string excludeFullName)
		{
			Text = "LCARS MOVE";
			BackColor = Color.Black;
			ForeColor = Color.Orange;
			FormBorderStyle = FormBorderStyle.None;
			ControlBox = false;
			DoubleBuffered = true;
			BindToWorkingArea = false;
			StartPosition = FormStartPosition.CenterParent;
			Size clientSize = new Size(420, 480);
			ClientSize = clientSize;
			ShowInTaskbar = false;
			FlatButton flatButton = new FlatButton();
			flatButton.ButtonText = "MOVE TO FOLDER";
			flatButton.Text = flatButton.ButtonText;
			flatButton.Clickable = false;
			flatButton.Color = LCARScolorStyles.StaticTan;
			flatButton.ButtonTextAlign = ContentAlignment.BottomRight;
			clientSize = new Size(180, 32);
			flatButton.Size = clientSize;
			Point location = new Point(12, 6);
			flatButton.Location = location;
			Controls.Add(flatButton);
			folderHost = new LcarsScrollPanel();
			folderHost.Name = "pnlMoveFolders";
			folderHost.BackColor = Color.Black;
			LcarsScrollPanel lcarsScrollPanel = folderHost;
			location = new Point(8, 44);
			lcarsScrollPanel.Location = location;
			LcarsScrollPanel lcarsScrollPanel2 = folderHost;
			checked
			{
				Size size = new Size(ClientSize.Width - 16, ClientSize.Height - 44 - 44);
				lcarsScrollPanel2.Size = size;
				Controls.Add(folderHost);
				fbCancel = new FlatButton();
				fbCancel.ButtonText = "CANCEL";
				fbCancel.Text = "CANCEL";
				fbCancel.Color = LCARScolorStyles.Orange;
				FlatButton flatButton2 = fbCancel;
				size = new Size(100, 28);
				flatButton2.Size = size;
				location = (fbCancel.Location = new Point(ClientSize.Width - fbCancel.Width - 12, ClientSize.Height - 36));
				fbCancel.Click += [SpecialName] [DebuggerStepThrough] (object a0, EventArgs a1) =>
				{
					((VB_0024AnonymousDelegate_1)([SpecialName] () =>
					{
						DialogResult = DialogResult.Cancel;
					}))();
				};
				Controls.Add(fbCancel);
				string b = ((excludeFullName != null) ? excludeFullName : "").Trim();
				int num = 4;
				bool flag = false;
				Panel contentPanel = folderHost.ContentPanel;
				int num2 = Math.Max(120, contentPanel.Width - 8);
				if (folders != null)
				{
					foreach (MailFolderInfo folder in folders)
					{
						if (folder == null)
						{
							continue;
						}
						MailFolderInfo mailFolderInfo = folder;
						string text = ((mailFolderInfo.FullName != null) ? mailFolderInfo.FullName : "").Trim();
						if (text.Length != 0 && !string.Equals(text, b, StringComparison.OrdinalIgnoreCase))
						{
							mailFolderInfo = folder;
							string text2 = ((mailFolderInfo.DisplayName != null) ? mailFolderInfo.DisplayName : text);
							if (string.IsNullOrWhiteSpace(text2))
							{
								text2 = text;
							}
							FlatButton flatButton3 = new FlatButton();
							flatButton3.ButtonText = text2.ToUpperInvariant();
							flatButton3.Text = flatButton3.ButtonText;
							flatButton3.Clickable = true;
							flatButton3.Beeping = true;
							flatButton3.ButtonTextAlign = ContentAlignment.MiddleLeft;
							flatButton3.ButtonTextHeight = 14;
							flatButton3.Color = (flag ? LCARScolorStyles.MiscFunction : LCARScolorStyles.StaticTan);
							flag = !flag;
							flatButton3.Tag = text;
							location = new Point(2, num);
							flatButton3.Location = location;
							size = new Size(num2, 36);
							flatButton3.Size = size;
							flatButton3.Click += OnFolderPickClick;
							contentPanel.Controls.Add(flatButton3);
							num += 40;
						}
					}
				}
				folderHost.Relayout();
			}
		}

		private void OnFolderPickClick(object sender, EventArgs e)
		{
			if (sender is FlatButton flatButton)
			{
				string value = flatButton.Tag as string;
				if (!string.IsNullOrWhiteSpace(value))
				{
					selectedFullName = value;
					DialogResult = DialogResult.OK;
				}
			}
		}
	}

	private const int RailLeft = 8;

	private const int LeftBorder = 8;

	private const int LeftElbowArm = 22;

	private const int TopChrome = 40;

	private const int ModeStripHeight = 36;

	private const int ToolbarRowHeight = 32;

	private const int ComposeToolbarGap = 4;

	private const int BottomPad = 40;

	private const int PaneSepWidth = 2;

	private const int PaneGap = 10;

	private const int FolderPaneWidth = 200;

	private const int ReadPaneMinWidth = 220;

	private const int ListPaneMinWidth = 180;

	private const int RightRailWidth = 108;

	private const int RightRailBtnH = 28;

	private const int RightRailGap = 4;

	private const int RowBtnHeight = 36;

	private const int RowGap = 4;

	private const int AccountHeaderHeight = 30;

	private const int FolderIndent = 10;

	private static readonly Color PaneSepGold = Color.FromArgb(255, 204, 102);

	private readonly UnifiedMailService mailService;

	private readonly Dictionary<string, string> sessionPasswords;

	private readonly Dictionary<string, bool> accountCollapsed;

	private readonly BackgroundWorker folderWorker;

	private readonly BackgroundWorker headerWorker;

	private readonly BackgroundWorker bodyWorker;

	private readonly BackgroundWorker mutationWorker;

	private readonly BackgroundWorker attachmentWorker;

	private List<MailAccount> accounts;

	private MailAccount activeAccount;

	private string selectedFolderFullName;

	private MailMessageHeader selectedHeader;

	private MailMessageBody selectedBody;

	private List<AccountFolderSection> lastFolderSections;

	private List<MailMessageHeader> currentFolderHeaders;

	private FlatButton fbModeMail;

	private FlatButton fbModeChat;

	private FlatButton fbModeRss;

	private FlatButton fbModeCal;

	private FlatButton fbImport;

	private FlatButton fbFilters;

	private FlatButton fbPgp;

	private FlatButton fbCardDav;

	private FlatButton fbAbook;

	private FlatButton fbNews;

	private FlatButton fbEws;

	private FlatButton fbCache;

	private FlatButton fbIndexNow;

	private FlatButton fbRefresh;

	private CommsMode currentMode;

	private Panel modeHost;

	private frmRssFeeds rssEmbed;

	private frmChat chatEmbed;

	private frmCalDavSync calEmbed;

	private bool unifiedInboxActive;

	private const string UnifiedInboxToken = "__UNIFIED_INBOX__";

	private string lastImportedProfileDir;

	private FlatButton fbNew;

	private FlatButton fbReply;

	private FlatButton fbReplyAll;

	private FlatButton fbForward;

	private FlatButton fbDelete;

	private FlatButton fbMove;

	private FlatButton fbSearch;

	private FlatButton fbFacetFrom;

	private FlatButton fbFacetSubj;

	private FlatButton fbFacetBody;

	private FlatButton fbFacetTo;

	private FlatButton fbFacetUnread;

	private FlatButton fbFacetCc;

	private FlatButton fbFacetNewer;

	private FlatButton fbFacetHasBody;

	private FlatButton fbFacet30d;

	private FlatButton fbFacetRead;

	private FlatButton fbFacetFolder;

	private FlatButton fbFacetLarger;

	private FlatButton fbFacet1Y;

	private FlatButton fbSaveSearch;

	private FlatButton fbRecallSearch;

	private TextBox txtSearch;

	private FlatButton fbClose;

	private Label lblStatus;

	private Elbow elbTop;

	private Elbow elbBottom;

	private FlatButton fbRailFill;

	private Panel paneSep1;

	private Panel paneSep2;

	private LcarsScrollPanel folderHost;

	private LcarsScrollPanel messageHost;

	private LcarsScrollPanel readHost;

	private Label lblFolderEmpty;

	private Label lblMessageEmpty;

	private Label lblReadEmpty;

	private Label lblReadMeta;

	private Label lblReadSubject;

	private Label lblReadThread;

	private ListBox lstThreadContext;

	private Label bodyLabel;

	private Label lblReadAttachments;

	private ListBox lstReadAttachments;

	private FlatButton fbSaveAttachment;

	private FlatButton selectedFolderButton;

	private FlatButton selectedMessageButton;

	private readonly Dictionary<ulong, MailMessageHeader> selectedMessages;

	private readonly Dictionary<ulong, FlatButton> messageButtonsByUid;

	private List<MailMessageHeader> messageListOrder;

	private int lastMessageListIndex;

	private bool conversationViewEnabled;

	private MailHtmlReadPane htmlReadPane;

	private FlatButton fbThreads;

	private FlatButton fbDrafts;

	private readonly HashSet<string> expandedThreadKeys;

	private readonly BackgroundWorker globalSearchWorker;

	private readonly System.Windows.Forms.Timer commsSyncTimer;

	private readonly System.Windows.Forms.Timer cardDavLiveTimer;

	private readonly System.Windows.Forms.Timer calDavLiveTimer;

	private bool useImapSearch;

	private bool crossFolderSearchActive;

	private string lastFtsQuery;

	private int ftsResultSkip;

	private bool ftsLastPageFull;

	private const int FtsPageSize = 5000;

	public frmMail(string startupMode = null)
	{
		mailService = new UnifiedMailService();
		sessionPasswords = new Dictionary<string, string>(StringComparer.Ordinal);
		accountCollapsed = new Dictionary<string, bool>(StringComparer.Ordinal);
		folderWorker = new BackgroundWorker();
		headerWorker = new BackgroundWorker();
		bodyWorker = new BackgroundWorker();
		mutationWorker = new BackgroundWorker();
		attachmentWorker = new BackgroundWorker();
		currentMode = CommsMode.Mail;
		unifiedInboxActive = false;
		selectedMessages = new Dictionary<ulong, MailMessageHeader>();
		messageButtonsByUid = new Dictionary<ulong, FlatButton>();
		messageListOrder = new List<MailMessageHeader>();
		lastMessageListIndex = -1;
		expandedThreadKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		globalSearchWorker = new BackgroundWorker();
		commsSyncTimer = new System.Windows.Forms.Timer();
		cardDavLiveTimer = new System.Windows.Forms.Timer();
		calDavLiveTimer = new System.Windows.Forms.Timer();
		lastFtsQuery = "";
		ftsResultSkip = 0;
		ftsLastPageFull = false;
		Text = "LCARS COMMS";
		BackColor = Color.Black;
		ForeColor = Color.Orange;
		FormBorderStyle = FormBorderStyle.None;
		ControlBox = false;
		DoubleBuffered = true;
		BindToWorkingArea = true;
		InitWorkers();
		BuildChrome();
		BuildPanes();
		EnsureModeEmbeds();
		Resize += OnMailResize;
		Shown += OnMailShown;
		Activated += OnMailActivated;
		currentMode = ParseStartupMode(startupMode);
	}

	private static CommsMode ParseStartupMode(string raw)
	{
		switch (((raw != null) ? raw : "").Trim().ToLowerInvariant())
		{
		case "chat":
		case "irc":
			return CommsMode.Chat;
		case "rss":
		case "feed":
			return CommsMode.Rss;
		case "cal":
		case "calendar":
			return CommsMode.Calendar;
		default:
			return CommsMode.Mail;
		}
	}

	private void InitWorkers()
	{
		folderWorker.WorkerSupportsCancellation = false;
		folderWorker.DoWork += FolderWorker_DoWork;
		folderWorker.RunWorkerCompleted += FolderWorker_Completed;
		headerWorker.WorkerSupportsCancellation = false;
		headerWorker.DoWork += HeaderWorker_DoWork;
		headerWorker.RunWorkerCompleted += HeaderWorker_Completed;
		bodyWorker.WorkerSupportsCancellation = false;
		bodyWorker.DoWork += BodyWorker_DoWork;
		bodyWorker.RunWorkerCompleted += BodyWorker_Completed;
		mutationWorker.WorkerSupportsCancellation = false;
		mutationWorker.DoWork += MutationWorker_DoWork;
		mutationWorker.RunWorkerCompleted += MutationWorker_Completed;
		attachmentWorker.WorkerSupportsCancellation = false;
		attachmentWorker.DoWork += AttachmentWorker_DoWork;
		attachmentWorker.RunWorkerCompleted += AttachmentWorker_Completed;
		globalSearchWorker.WorkerSupportsCancellation = false;
		globalSearchWorker.DoWork += GlobalSearchWorker_DoWork;
		globalSearchWorker.RunWorkerCompleted += GlobalSearchWorker_Completed;
		commsSyncTimer.Interval = 900000;
		commsSyncTimer.Tick += OnCommsSyncTimerTick;
		cardDavLiveTimer.Interval = 15000;
		cardDavLiveTimer.Tick += OnCardDavLiveTimerTick;
		CardDavLiveBookCache.EnsureAlwaysOnStarted();
		calDavLiveTimer.Interval = 60000;
		calDavLiveTimer.Tick += OnCalDavLiveTimerTick;
	}

	private void OnCommsSyncTimerTick(object sender, EventArgs e)
	{
		QueueBackgroundCommsSync();
		QueueBackgroundSearchIndex();
		if (accounts != null && accounts.Count > 0 && activeAccount != null && MailSearchIndex.CountEntriesNeedingBodyPreview(activeAccount.Id) >= 350)
		{
			MailSearchBackgroundIndexer.QueueBodyBackfillBurst(accounts, [SpecialName] (MailAccount acct) => ResolvePassword(acct, promptIfMissing: false), mailService);
		}
		if (!string.IsNullOrWhiteSpace(MailSearchBackgroundIndexer.LastRunSummary))
		{
			SetStatus(MailSearchBackgroundIndexer.LastRunSummary.ToUpperInvariant());
		}
	}

	private void OnCardDavLiveTimerTick(object sender, EventArgs e)
	{
		int num = AddressBookStore.CountCardDavDirtyContacts();
		cardDavLiveTimer.Interval = ((num > 0) ? 4000 : 12000);
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
		});
		if (DateTime.Compare(CardDavLiveBookCache.LastFetchUtc, DateTime.MinValue) != 0)
		{
			string text = CardDavLiveBookCache.LastContactCount.ToString();
			string text2 = " · " + CardDavLiveBookCache.LastAlwaysOnIntervalMs / 1000 + "s";
			string text3 = ((AddressBookStore.CountCardDavDirtyContacts() > 0) ? " · DIRTY" : "");
			string text4 = ((CardDavLiveBookCache.LastBidirectionalPushCount > 0) ? (" · PUSH " + CardDavLiveBookCache.LastBidirectionalPushCount) : "");
			if (CardDavSyncService.LastMergedAfter412 && CardDavSyncService.Last412ConflictEmail.Length > 0)
			{
				text4 = text4 + " · 412 " + CardDavSyncService.Last412ConflictEmail.Trim();
			}
			if (AddressBookStore.LastCardDav412BatchResolved > 0)
			{
				text4 = text4 + " · 412-FIX " + AddressBookStore.LastCardDav412BatchResolved;
			}
			if (AddressBookStore.LastCardDavSecondaryBooksMerged > 0)
			{
				text4 = text4 + " · 2ND+" + AddressBookStore.LastCardDavSecondaryBooksMerged;
			}
			if (AddressBookStore.LastCardDavDiscoveredBooks > 0)
			{
				text4 = text4 + " · BOOKS " + AddressBookStore.LastCardDavDiscoveredBooks;
			}
			if (AddressBookStore.LastCardDavRemotePullCount > 0)
			{
				text4 = text4 + " · REMOTE " + AddressBookStore.LastCardDavRemotePullCount;
			}
			if (AddressBookStore.LastCardDavPushCount > 0)
			{
				text4 = text4 + " · CARD-PUSH " + AddressBookStore.LastCardDavPushCount;
			}
			if (string.Equals(CardDavSyncService.ConflictOn412Mode, "ServerWins", StringComparison.OrdinalIgnoreCase))
			{
				text4 += " · 412-SERVER";
			}
			else if (string.Equals(CardDavSyncService.ConflictOn412Mode, "ClientMerge", StringComparison.OrdinalIgnoreCase))
			{
				text4 += " · 412-CLIENT";
			}
			if (!string.IsNullOrWhiteSpace(CardDavLiveBookCache.LastRefreshSummary))
			{
				SetStatus("ABOOK LIVE " + text + " — " + CardDavLiveBookCache.LastRefreshSummary.ToUpperInvariant() + text2 + text3 + text4);
			}
			else
			{
				SetStatus("ABOOK LIVE " + text + " @ " + CardDavLiveBookCache.LastFetchUtc.ToLocalTime().ToString("HH:mm:ss") + text2 + text3 + text4);
			}
		}
	}

	private void OnCalDavLiveTimerTick(object sender, EventArgs e)
	{
		int num = LcarsCalendarBridge.CountCalDavDirtyEvents();
		calDavLiveTimer.Interval = ((num > 0) ? 20000 : 60000);
		string calendarUrl = null;
		string username = null;
		string password = null;
		if (!LcarsCalendarBridge.TryLoadCalDavSettings(ref calendarUrl, ref username, ref password))
		{
			return;
		}
		ThreadPool.QueueUserWorkItem([SpecialName] (object state) =>
		{
			try
			{
				LcarsCalendarBridge.TrySyncStoredCalDavBidirectional();
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		});
		int num2 = EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents();
		if (DateTime.Compare(LcarsCalendarBridge.LastCalDavBidirectionalUtc, DateTime.MinValue) != 0 || num > 0 || num2 > 0)
		{
			string text = ((LcarsCalendarBridge.LastCalDavPrimaryEventsTagged > 0) ? (" · TAG " + LcarsCalendarBridge.LastCalDavPrimaryEventsTagged) : "");
			if (LcarsCalendarBridge.LastCalDavSecondaryEventsTagged > 0)
			{
				text = text + " · 2ND-TAG " + LcarsCalendarBridge.LastCalDavSecondaryEventsTagged;
			}
			if (LcarsCalendarBridge.LastCalDavTaskEventsTagged > 0)
			{
				text = text + " · TASK-TAG " + LcarsCalendarBridge.LastCalDavTaskEventsTagged;
			}
			if (LcarsCalendarBridge.LastCalDavPullMergeAdded > 0)
			{
				text = text + " · PULL " + LcarsCalendarBridge.LastCalDavPullMergeAdded;
			}
			if (LcarsCalendarBridge.LastCalDavDiscoveredCalendars > 0)
			{
				text = text + " · CALS " + LcarsCalendarBridge.LastCalDavDiscoveredCalendars;
			}
			if (LcarsCalendarBridge.LastCalDavSecondaryPullMergeAdded > 0)
			{
				text = text + " · 2ND-PULL " + LcarsCalendarBridge.LastCalDavSecondaryPullMergeAdded;
			}
			if (LcarsCalendarBridge.LastCalDavUrlLabelsStored > 0)
			{
				text = text + " · PROP " + LcarsCalendarBridge.LastCalDavUrlLabelsStored;
			}
			if (LcarsCalendarBridge.LastCalDavPrimaryPullMergeAdded > 0)
			{
				text = text + " · 1ST-PULL " + LcarsCalendarBridge.LastCalDavPrimaryPullMergeAdded;
			}
			if (LcarsCalendarBridge.LastCalDavTaskPullMergeAdded > 0)
			{
				text = text + " · TASK-PULL " + LcarsCalendarBridge.LastCalDavTaskPullMergeAdded;
			}
			SetStatus("CAL LIVE · DIRTY " + num + " · EWS " + num2 + " · CAL " + LcarsCalendarBridge.CountDistinctCalendarNames() + " · EWS-LINK " + LcarsCalendarBridge.CountEventsLinkedToEws() + " · PUSH " + LcarsCalendarBridge.LastCalDavPushCount + text);
		}
	}

	private void OnMailActivated(object sender, EventArgs e)
	{
		CardDavLiveBookCache.QueueBackgroundRefresh();
		ThreadPool.QueueUserWorkItem([SpecialName] (object state) =>
		{
			try
			{
				LcarsCalendarBridge.TrySyncStoredCalDavBidirectional();
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		});
	}

	private void OnIndexNowClick(object sender, EventArgs e)
	{
		QueueBackgroundSearchIndex();
		string text = "";
		if (activeAccount != null)
		{
			List<MailFolderInfo> list = FoldersForActiveAccount();
			if (list != null && list.Count > 0)
			{
				text = MailSearchIndexCursor.FormatRotationHint(activeAccount.Id, list.Count);
			}
			int num = MailSearchIndex.CountEntriesNeedingBodyPreview(activeAccount.Id);
			if (num > 0)
			{
				text = text + " · BACKFILL " + num;
			}
		}
		SetStatus("GLODA INDEX QUEUED" + ((text.Length > 0) ? (" — " + text) : ""));
	}

	private void OnFacetFolderClick(object sender, EventArgs e)
	{
		string defaultResponse = ((selectedFolderFullName != null) ? selectedFolderFullName : "INBOX").Trim();
		string text = LcarsCommsUi.Prompt("Folder name (Gloda folder: facet):", "LCARS COMMS — GLODA FOLDER", defaultResponse);
		if (!string.IsNullOrWhiteSpace(text))
		{
			ApplySearchFacet("folder:" + text.Trim());
		}
	}

	private void BuildChrome()
	{
		elbTop = new Elbow();
		elbTop.Name = "elbTop";
		elbTop.ElbowStyle = Elbow.LCARSelbowStyles.UpperLeft;
		elbTop.Color = LCARScolorStyles.LCARSDisplayOnly;
		elbTop.ButtonText = "";
		elbTop.Text = "";
		elbTop.ButtonWidth = 8;
		elbTop.ButtonHeight = 18;
		Elbow elbow = elbTop;
		Size size = new Size(30, 36);
		elbow.Size = size;
		Elbow elbow2 = elbTop;
		Point location = new Point(8, 4);
		elbow2.Location = location;
		elbTop.Clickable = false;
		Controls.Add(elbTop);
		fbRailFill = new FlatButton();
		fbRailFill.Name = "fbRailFill";
		fbRailFill.ButtonText = "";
		fbRailFill.Text = "";
		fbRailFill.Clickable = false;
		fbRailFill.Color = LCARScolorStyles.LCARSDisplayOnly;
		fbRailFill.Width = 8;
		Controls.Add(fbRailFill);
		elbBottom = new Elbow();
		elbBottom.Name = "elbBottom";
		elbBottom.ElbowStyle = Elbow.LCARSelbowStyles.LowerLeft;
		elbBottom.Color = LCARScolorStyles.LCARSDisplayOnly;
		elbBottom.ButtonWidth = 8;
		Elbow elbow3 = elbBottom;
		size = new Size(30, 36);
		elbow3.Size = size;
		elbBottom.Clickable = false;
		Controls.Add(elbBottom);
		fbModeMail = MakeToolbarButton("fbModeMail", "MAIL", LCARScolorStyles.PrimaryFunction);
		FlatButton flatButton = fbModeMail;
		size = new Size(100, 32);
		flatButton.Size = size;
		fbModeMail.Click += [SpecialName] [DebuggerStepThrough] (object a0, EventArgs a1) =>
		{
			((VB_0024AnonymousDelegate_1)([SpecialName] () =>
			{
				SetCommsMode(CommsMode.Mail);
			}))();
		};
		Controls.Add(fbModeMail);
		fbModeChat = MakeToolbarButton("fbModeChat", "CHAT", LCARScolorStyles.MiscFunction);
		FlatButton flatButton2 = fbModeChat;
		size = new Size(100, 32);
		flatButton2.Size = size;
		fbModeChat.Click += [SpecialName] [DebuggerStepThrough] (object a0, EventArgs a1) =>
		{
			((VB_0024AnonymousDelegate_1)([SpecialName] () =>
			{
				SetCommsMode(CommsMode.Chat);
			}))();
		};
		Controls.Add(fbModeChat);
		fbModeRss = MakeToolbarButton("fbModeRss", "RSS", LCARScolorStyles.MiscFunction);
		FlatButton flatButton3 = fbModeRss;
		size = new Size(100, 32);
		flatButton3.Size = size;
		fbModeRss.Click += [SpecialName] [DebuggerStepThrough] (object a0, EventArgs a1) =>
		{
			((VB_0024AnonymousDelegate_1)([SpecialName] () =>
			{
				SetCommsMode(CommsMode.Rss);
			}))();
		};
		Controls.Add(fbModeRss);
		fbModeCal = MakeToolbarButton("fbModeCal", "CALENDAR", LCARScolorStyles.MiscFunction);
		FlatButton flatButton4 = fbModeCal;
		size = new Size(120, 32);
		flatButton4.Size = size;
		fbModeCal.Click += [SpecialName] [DebuggerStepThrough] (object a0, EventArgs a1) =>
		{
			((VB_0024AnonymousDelegate_1)([SpecialName] () =>
			{
				SetCommsMode(CommsMode.Calendar);
			}))();
		};
		Controls.Add(fbModeCal);
		modeHost = new Panel();
		modeHost.Name = "pnlModeHost";
		modeHost.BackColor = Color.Black;
		modeHost.Visible = false;
		Controls.Add(modeHost);
		fbFilters = MakeToolbarButton("fbFilters", "FILTERS", LCARScolorStyles.MiscFunction);
		FlatButton flatButton5 = fbFilters;
		size = new Size(96, 28);
		flatButton5.Size = size;
		fbFilters.Click += OnFiltersClick;
		Controls.Add(fbFilters);
		fbPgp = MakeToolbarButton("fbPgp", "PGP", LCARScolorStyles.SystemFunction);
		FlatButton flatButton6 = fbPgp;
		size = new Size(72, 28);
		flatButton6.Size = size;
		fbPgp.Click += OnPgpClick;
		Controls.Add(fbPgp);
		fbCardDav = MakeToolbarButton("fbCardDav", "CARDDAV", LCARScolorStyles.StaticTan);
		FlatButton flatButton7 = fbCardDav;
		size = new Size(96, 28);
		flatButton7.Size = size;
		fbCardDav.Click += OnCardDavClick;
		Controls.Add(fbCardDav);
		fbAbook = MakeToolbarButton("fbAbook", "ABOOK", LCARScolorStyles.StaticTan);
		FlatButton flatButton8 = fbAbook;
		size = new Size(72, 28);
		flatButton8.Size = size;
		fbAbook.Click += OnAddressBookClick;
		Controls.Add(fbAbook);
		fbNews = MakeToolbarButton("fbNews", "NEWS", LCARScolorStyles.StaticTan);
		FlatButton flatButton9 = fbNews;
		size = new Size(72, 28);
		flatButton9.Size = size;
		fbNews.Click += OnNewsClick;
		Controls.Add(fbNews);
		fbEws = MakeToolbarButton("fbEws", "EWS", LCARScolorStyles.StaticTan);
		FlatButton flatButton10 = fbEws;
		size = new Size(56, 28);
		flatButton10.Size = size;
		fbEws.Click += OnEwsClick;
		Controls.Add(fbEws);
		fbCache = MakeToolbarButton("fbCache", "CACHE", LCARScolorStyles.MiscFunction);
		fbIndexNow = MakeToolbarButton("fbIndexNow", "INDEX", LCARScolorStyles.MiscFunction);
		FlatButton flatButton11 = fbIndexNow;
		size = new Size(72, 28);
		flatButton11.Size = size;
		fbIndexNow.Click += OnIndexNowClick;
		Controls.Add(fbIndexNow);
		FlatButton flatButton12 = fbCache;
		size = new Size(72, 28);
		flatButton12.Size = size;
		fbCache.Click += OnCacheClick;
		Controls.Add(fbCache);
		fbImport = MakeToolbarButton("fbImport", "IMPORT", LCARScolorStyles.LCARSDisplayOnly);
		FlatButton flatButton13 = fbImport;
		size = new Size(80, 28);
		flatButton13.Size = size;
		fbImport.Click += OnImportClick;
		Controls.Add(fbImport);
		fbRefresh = MakeToolbarButton("fbRefresh", "REFRESH", LCARScolorStyles.SystemFunction);
		fbRefresh.Click += OnRefreshClick;
		Controls.Add(fbRefresh);
		fbNew = MakeToolbarButton("fbNew", "NEW", LCARScolorStyles.PrimaryFunction);
		FlatButton flatButton14 = fbNew;
		size = new Size(88, 28);
		flatButton14.Size = size;
		fbNew.Click += OnNewClick;
		Controls.Add(fbNew);
		fbDrafts = MakeToolbarButton("fbDrafts", "DRAFTS", LCARScolorStyles.SystemFunction);
		FlatButton flatButton15 = fbDrafts;
		size = new Size(96, 28);
		flatButton15.Size = size;
		fbDrafts.Click += OnDraftsClick;
		Controls.Add(fbDrafts);
		fbReply = MakeToolbarButton("fbReply", "REPLY", LCARScolorStyles.MiscFunction);
		FlatButton flatButton16 = fbReply;
		size = new Size(88, 28);
		flatButton16.Size = size;
		fbReply.Click += OnReplyClick;
		Controls.Add(fbReply);
		fbReplyAll = MakeToolbarButton("fbReplyAll", "REPLY ALL", LCARScolorStyles.MiscFunction);
		FlatButton flatButton17 = fbReplyAll;
		size = new Size(108, 28);
		flatButton17.Size = size;
		fbReplyAll.Click += OnReplyAllClick;
		Controls.Add(fbReplyAll);
		fbForward = MakeToolbarButton("fbForward", "FORWARD", LCARScolorStyles.SystemFunction);
		FlatButton flatButton18 = fbForward;
		size = new Size(100, 28);
		flatButton18.Size = size;
		fbForward.Click += OnForwardClick;
		Controls.Add(fbForward);
		fbDelete = MakeToolbarButton("fbDelete", "DELETE", LCARScolorStyles.Orange);
		FlatButton flatButton19 = fbDelete;
		size = new Size(96, 28);
		flatButton19.Size = size;
		fbDelete.Click += OnDeleteClick;
		Controls.Add(fbDelete);
		fbMove = MakeToolbarButton("fbMove", "MOVE", LCARScolorStyles.PrimaryFunction);
		FlatButton flatButton20 = fbMove;
		size = new Size(88, 28);
		flatButton20.Size = size;
		fbMove.Click += OnMoveClick;
		Controls.Add(fbMove);
		fbThreads = MakeToolbarButton("fbThreads", "THREADS", LCARScolorStyles.StaticTan);
		FlatButton flatButton21 = fbThreads;
		size = new Size(100, 28);
		flatButton21.Size = size;
		fbThreads.Click += OnThreadsToggleClick;
		Controls.Add(fbThreads);
		fbSearch = MakeToolbarButton("fbSearch", "SEARCH", LCARScolorStyles.StaticTan);
		FlatButton flatButton22 = fbSearch;
		size = new Size(88, 28);
		flatButton22.Size = size;
		fbSearch.Clickable = true;
		fbSearch.Beeping = true;
		fbSearch.Click += OnSearchButtonClick;
		Controls.Add(fbSearch);
		txtSearch = new TextBox();
		txtSearch.Name = "txtSearch";
		txtSearch.BorderStyle = BorderStyle.FixedSingle;
		txtSearch.BackColor = Color.FromArgb(40, 40, 40);
		txtSearch.ForeColor = Color.Orange;
		txtSearch.Font = LcarsUiFont(10f, FontStyle.Regular);
		TextBox textBox = txtSearch;
		size = new Size(160, 24);
		textBox.Size = size;
		txtSearch.TextChanged += OnSearchTextChanged;
		txtSearch.Enter += OnSearchEnter;
		txtSearch.KeyDown += OnSearchKeyDown;
		Controls.Add(txtSearch);
		fbFacetFrom = MakeToolbarButton("fbFacetFrom", "FROM", LCARScolorStyles.NavigationFunction);
		FlatButton flatButton23 = fbFacetFrom;
		size = new Size(56, 28);
		flatButton23.Size = size;
		fbFacetFrom.Click += [SpecialName] [DebuggerStepThrough] (object a0, EventArgs a1) =>
		{
			((VB_0024AnonymousDelegate_1)([SpecialName] () =>
			{
				ApplySearchFacet("from:");
			}))();
		};
		Controls.Add(fbFacetFrom);
		fbFacetSubj = MakeToolbarButton("fbFacetSubj", "SUBJ", LCARScolorStyles.NavigationFunction);
		FlatButton flatButton24 = fbFacetSubj;
		size = new Size(56, 28);
		flatButton24.Size = size;
		fbFacetSubj.Click += [SpecialName] [DebuggerStepThrough] (object a0, EventArgs a1) =>
		{
			((VB_0024AnonymousDelegate_1)([SpecialName] () =>
			{
				ApplySearchFacet("subject:");
			}))();
		};
		Controls.Add(fbFacetSubj);
		fbFacetBody = MakeToolbarButton("fbFacetBody", "BODY", LCARScolorStyles.NavigationFunction);
		FlatButton flatButton25 = fbFacetBody;
		size = new Size(56, 28);
		flatButton25.Size = size;
		fbFacetBody.Click += [SpecialName] [DebuggerStepThrough] (object a0, EventArgs a1) =>
		{
			((VB_0024AnonymousDelegate_1)([SpecialName] () =>
			{
				ApplySearchFacet("body:");
			}))();
		};
		Controls.Add(fbFacetBody);
		fbFacetTo = MakeToolbarButton("fbFacetTo", "TO", LCARScolorStyles.NavigationFunction);
		FlatButton flatButton26 = fbFacetTo;
		size = new Size(44, 28);
		flatButton26.Size = size;
		fbFacetTo.Click += [SpecialName] [DebuggerStepThrough] (object a0, EventArgs a1) =>
		{
			((VB_0024AnonymousDelegate_1)([SpecialName] () =>
			{
				ApplySearchFacet("to:");
			}))();
		};
		Controls.Add(fbFacetTo);
		fbFacetUnread = MakeToolbarButton("fbFacetUnread", "UNREAD", LCARScolorStyles.CriticalFunction);
		FlatButton flatButton27 = fbFacetUnread;
		size = new Size(72, 28);
		flatButton27.Size = size;
		fbFacetUnread.Click += [SpecialName] [DebuggerStepThrough] (object a0, EventArgs a1) =>
		{
			((VB_0024AnonymousDelegate_1)([SpecialName] () =>
			{
				ApplySearchFacet("is:unread");
			}))();
		};
		Controls.Add(fbFacetUnread);
		fbFacetCc = MakeToolbarButton("fbFacetCc", "CC", LCARScolorStyles.NavigationFunction);
		FlatButton flatButton28 = fbFacetCc;
		size = new Size(44, 28);
		flatButton28.Size = size;
		fbFacetCc.Click += [SpecialName] [DebuggerStepThrough] (object a0, EventArgs a1) =>
		{
			((VB_0024AnonymousDelegate_1)([SpecialName] () =>
			{
				ApplySearchFacet("cc:");
			}))();
		};
		Controls.Add(fbFacetCc);
		fbFacetNewer = MakeToolbarButton("fbFacetNewer", "7D", LCARScolorStyles.SystemFunction);
		FlatButton flatButton29 = fbFacetNewer;
		size = new Size(44, 28);
		flatButton29.Size = size;
		fbFacetNewer.Click += [SpecialName] [DebuggerStepThrough] (object a0, EventArgs a1) =>
		{
			((VB_0024AnonymousDelegate_1)([SpecialName] () =>
			{
				ApplySearchFacet("newer:7d");
			}))();
		};
		Controls.Add(fbFacetNewer);
		fbFacetHasBody = MakeToolbarButton("fbFacetHasBody", "BODY+", LCARScolorStyles.SystemFunction);
		FlatButton flatButton30 = fbFacetHasBody;
		size = new Size(56, 28);
		flatButton30.Size = size;
		fbFacetHasBody.Click += [SpecialName] [DebuggerStepThrough] (object a0, EventArgs a1) =>
		{
			((VB_0024AnonymousDelegate_1)([SpecialName] () =>
			{
				ApplySearchFacet("has:body");
			}))();
		};
		Controls.Add(fbFacetHasBody);
		fbFacet30d = MakeToolbarButton("fbFacet30d", "30D", LCARScolorStyles.SystemFunction);
		FlatButton flatButton31 = fbFacet30d;
		size = new Size(44, 28);
		flatButton31.Size = size;
		fbFacet30d.Click += [SpecialName] [DebuggerStepThrough] (object a0, EventArgs a1) =>
		{
			((VB_0024AnonymousDelegate_1)([SpecialName] () =>
			{
				ApplySearchFacet("newer:30d");
			}))();
		};
		Controls.Add(fbFacet30d);
		fbFacetRead = MakeToolbarButton("fbFacetRead", "READ", LCARScolorStyles.NavigationFunction);
		FlatButton flatButton32 = fbFacetRead;
		size = new Size(52, 28);
		flatButton32.Size = size;
		fbFacetRead.Click += [SpecialName] [DebuggerStepThrough] (object a0, EventArgs a1) =>
		{
			((VB_0024AnonymousDelegate_1)([SpecialName] () =>
			{
				ApplySearchFacet("is:read");
			}))();
		};
		Controls.Add(fbFacetRead);
		fbFacetFolder = MakeToolbarButton("fbFacetFolder", "FLD", LCARScolorStyles.SystemFunction);
		FlatButton flatButton33 = fbFacetFolder;
		size = new Size(44, 28);
		flatButton33.Size = size;
		fbFacetFolder.Click += OnFacetFolderClick;
		Controls.Add(fbFacetFolder);
		fbFacetLarger = MakeToolbarButton("fbFacetLarger", "BIG", LCARScolorStyles.SystemFunction);
		FlatButton flatButton34 = fbFacetLarger;
		size = new Size(44, 28);
		flatButton34.Size = size;
		fbFacetLarger.Click += [SpecialName] [DebuggerStepThrough] (object a0, EventArgs a1) =>
		{
			((VB_0024AnonymousDelegate_1)([SpecialName] () =>
			{
				ApplySearchFacet("larger:100k");
			}))();
		};
		Controls.Add(fbFacetLarger);
		fbFacet1Y = MakeToolbarButton("fbFacet1Y", "1Y", LCARScolorStyles.SystemFunction);
		FlatButton flatButton35 = fbFacet1Y;
		size = new Size(40, 28);
		flatButton35.Size = size;
		fbFacet1Y.Click += [SpecialName] [DebuggerStepThrough] (object a0, EventArgs a1) =>
		{
			((VB_0024AnonymousDelegate_1)([SpecialName] () =>
			{
				ApplySearchFacet("newer:365d");
			}))();
		};
		Controls.Add(fbFacet1Y);
		FlatButton flatButton36 = MakeToolbarButton("fbFacet90d", "90D", LCARScolorStyles.SystemFunction);
		size = new Size(44, 28);
		flatButton36.Size = size;
		flatButton36.Click += [SpecialName] [DebuggerStepThrough] (object a0, EventArgs a1) =>
		{
			((VB_0024AnonymousDelegate_1)([SpecialName] () =>
			{
				ApplySearchFacet("newer:90d");
			}))();
		};
		Controls.Add(flatButton36);
		FlatButton flatButton37 = MakeToolbarButton("fbFacet6M", "6M", LCARScolorStyles.SystemFunction);
		size = new Size(40, 28);
		flatButton37.Size = size;
		flatButton37.Click += [SpecialName] [DebuggerStepThrough] (object a0, EventArgs a1) =>
		{
			((VB_0024AnonymousDelegate_1)([SpecialName] () =>
			{
				ApplySearchFacet("newer:180d");
			}))();
		};
		Controls.Add(flatButton37);
		FlatButton flatButton38 = MakeToolbarButton("fbFacetSmall", "SML", LCARScolorStyles.SystemFunction);
		size = new Size(44, 28);
		flatButton38.Size = size;
		flatButton38.Click += [SpecialName] [DebuggerStepThrough] (object a0, EventArgs a1) =>
		{
			((VB_0024AnonymousDelegate_1)([SpecialName] () =>
			{
				ApplySearchFacet("smaller:50k");
			}))();
		};
		Controls.Add(flatButton38);
		FlatButton flatButton39 = MakeToolbarButton("fbFacet2W", "2W", LCARScolorStyles.SystemFunction);
		size = new Size(40, 28);
		flatButton39.Size = size;
		flatButton39.Click += [SpecialName] [DebuggerStepThrough] (object a0, EventArgs a1) =>
		{
			((VB_0024AnonymousDelegate_1)([SpecialName] () =>
			{
				ApplySearchFacet("newer:14d");
			}))();
		};
		Controls.Add(flatButton39);
		FlatButton flatButton40 = MakeToolbarButton("fbFacetReply", "RPLY", LCARScolorStyles.NavigationFunction);
		size = new Size(48, 28);
		flatButton40.Size = size;
		flatButton40.Click += [SpecialName] [DebuggerStepThrough] (object a0, EventArgs a1) =>
		{
			((VB_0024AnonymousDelegate_1)([SpecialName] () =>
			{
				ApplySearchFacet("replyto:");
			}))();
		};
		Controls.Add(flatButton40);
		fbSaveSearch = MakeToolbarButton("fbSaveSearch", "SAVE", LCARScolorStyles.MiscFunction);
		FlatButton flatButton41 = fbSaveSearch;
		size = new Size(52, 28);
		flatButton41.Size = size;
		fbSaveSearch.Click += OnSaveGlodaSearchClick;
		Controls.Add(fbSaveSearch);
		fbRecallSearch = MakeToolbarButton("fbRecallSearch", "RECALL", LCARScolorStyles.SystemFunction);
		FlatButton flatButton42 = fbRecallSearch;
		size = new Size(64, 28);
		flatButton42.Size = size;
		fbRecallSearch.Click += OnRecallGlodaSearchClick;
		Controls.Add(fbRecallSearch);
		lblStatus = new Label();
		lblStatus.Name = "lblStatus";
		lblStatus.AutoSize = false;
		lblStatus.TextAlign = ContentAlignment.MiddleLeft;
		lblStatus.ForeColor = Color.Orange;
		lblStatus.BackColor = Color.Black;
		lblStatus.Font = LcarsUiFont(10f);
		lblStatus.Text = "READY";
		Controls.Add(lblStatus);
		fbClose = new FlatButton();
		fbClose.Name = "fbClose";
		fbClose.ButtonText = "CLOSE";
		fbClose.Text = "CLOSE";
		fbClose.Color = LCARScolorStyles.Orange;
		FlatButton flatButton43 = fbClose;
		size = new Size(100, 28);
		flatButton43.Size = size;
		fbClose.Click += OnCloseClick;
		Controls.Add(fbClose);
	}

	private void BuildPanes()
	{
		folderHost = new LcarsScrollPanel();
		folderHost.Name = "pnlFolders";
		folderHost.BackColor = Color.Black;
		folderHost.CollapseTrackWhenIdle = true;
		Controls.Add(folderHost);
		paneSep1 = new Panel();
		paneSep1.Name = "paneSep1";
		paneSep1.BackColor = PaneSepGold;
		paneSep1.Width = 2;
		Controls.Add(paneSep1);
		messageHost = new LcarsScrollPanel();
		messageHost.Name = "pnlMessages";
		messageHost.BackColor = Color.Black;
		messageHost.CollapseTrackWhenIdle = true;
		Controls.Add(messageHost);
		paneSep2 = new Panel();
		paneSep2.Name = "paneSep2";
		paneSep2.BackColor = PaneSepGold;
		paneSep2.Width = 2;
		Controls.Add(paneSep2);
		readHost = new LcarsScrollPanel();
		readHost.Name = "pnlReading";
		readHost.BackColor = Color.Black;
		readHost.CollapseTrackWhenIdle = true;
		Controls.Add(readHost);
		lblFolderEmpty = MakeEmptyLabel("SELECT REFRESH OR IMPORT");
		folderHost.ContentPanel.Controls.Add(lblFolderEmpty);
		lblMessageEmpty = MakeEmptyLabel("SELECT A FOLDER");
		messageHost.ContentPanel.Controls.Add(lblMessageEmpty);
		lblReadEmpty = MakeEmptyLabel("SELECT A MESSAGE");
		readHost.ContentPanel.Controls.Add(lblReadEmpty);
		lblReadMeta = new Label();
		lblReadMeta.AutoSize = true;
		Label label = lblReadMeta;
		Size maximumSize = new Size(400, 0);
		label.MaximumSize = maximumSize;
		lblReadMeta.ForeColor = Color.FromArgb(200, 140, 60);
		lblReadMeta.BackColor = Color.Black;
		lblReadMeta.Font = LcarsUiFont(9f);
		lblReadMeta.Visible = false;
		readHost.ContentPanel.Controls.Add(lblReadMeta);
		lblReadSubject = new Label();
		lblReadSubject.AutoSize = true;
		Label label2 = lblReadSubject;
		maximumSize = new Size(400, 0);
		label2.MaximumSize = maximumSize;
		lblReadSubject.ForeColor = Color.Orange;
		lblReadSubject.BackColor = Color.Black;
		lblReadSubject.Font = LcarsUiFont(11f);
		lblReadSubject.Visible = false;
		readHost.ContentPanel.Controls.Add(lblReadSubject);
		lblReadThread = new Label();
		lblReadThread.AutoSize = true;
		lblReadThread.ForeColor = Color.FromArgb(200, 140, 60);
		lblReadThread.BackColor = Color.Black;
		lblReadThread.Font = LcarsUiFont(9f);
		lblReadThread.Text = "THREAD";
		lblReadThread.Visible = false;
		readHost.ContentPanel.Controls.Add(lblReadThread);
		lstThreadContext = new ListBox();
		lstThreadContext.BorderStyle = BorderStyle.FixedSingle;
		lstThreadContext.BackColor = Color.FromArgb(40, 40, 40);
		lstThreadContext.ForeColor = Color.White;
		lstThreadContext.Font = LcarsUiFont(8.5f, FontStyle.Regular);
		lstThreadContext.Visible = false;
		lstThreadContext.SelectedIndexChanged += OnThreadContextSelected;
		readHost.ContentPanel.Controls.Add(lstThreadContext);
		bodyLabel = new Label();
		bodyLabel.AutoSize = true;
		Label label3 = bodyLabel;
		maximumSize = new Size(400, 0);
		label3.MaximumSize = maximumSize;
		bodyLabel.ForeColor = Color.Orange;
		bodyLabel.BackColor = Color.Black;
		bodyLabel.Font = new Font("Consolas", 9.5f, FontStyle.Regular);
		bodyLabel.Visible = false;
		readHost.ContentPanel.Controls.Add(bodyLabel);
		htmlReadPane = new MailHtmlReadPane();
		htmlReadPane.Visible = false;
		readHost.ContentPanel.Controls.Add(htmlReadPane);
		lblReadAttachments = new Label();
		lblReadAttachments.AutoSize = true;
		lblReadAttachments.ForeColor = Color.FromArgb(200, 140, 60);
		lblReadAttachments.BackColor = Color.Black;
		lblReadAttachments.Font = LcarsUiFont(9f);
		lblReadAttachments.Text = "ATTACHMENTS";
		lblReadAttachments.Visible = false;
		readHost.ContentPanel.Controls.Add(lblReadAttachments);
		lstReadAttachments = new ListBox();
		lstReadAttachments.BorderStyle = BorderStyle.FixedSingle;
		lstReadAttachments.BackColor = Color.FromArgb(40, 40, 40);
		lstReadAttachments.ForeColor = Color.White;
		lstReadAttachments.Font = LcarsUiFont(9f, FontStyle.Regular);
		lstReadAttachments.Visible = false;
		readHost.ContentPanel.Controls.Add(lstReadAttachments);
		fbSaveAttachment = MakeToolbarButton("fbSaveAttachment", "SAVE AS", LCARScolorStyles.PrimaryFunction);
		FlatButton flatButton = fbSaveAttachment;
		maximumSize = new Size(100, 28);
		flatButton.Size = maximumSize;
		fbSaveAttachment.Visible = false;
		fbSaveAttachment.Click += OnSaveAttachmentClick;
		readHost.ContentPanel.Controls.Add(fbSaveAttachment);
		folderHost.SendToBack();
		messageHost.SendToBack();
		readHost.SendToBack();
		if (paneSep1 != null)
		{
			paneSep1.BringToFront();
		}
		if (paneSep2 != null)
		{
			paneSep2.BringToFront();
		}
		fbClose.BringToFront();
		lblStatus.BringToFront();
		fbImport.BringToFront();
		if (fbFilters != null)
		{
			fbFilters.BringToFront();
		}
		if (fbPgp != null)
		{
			fbPgp.BringToFront();
		}
		fbRefresh.BringToFront();
		fbNew.BringToFront();
		fbReply.BringToFront();
		fbReplyAll.BringToFront();
		fbForward.BringToFront();
		fbDelete.BringToFront();
		fbMove.BringToFront();
		fbSearch.BringToFront();
		if (fbFacetFrom != null)
		{
			fbFacetFrom.BringToFront();
		}
		if (fbFacetSubj != null)
		{
			fbFacetSubj.BringToFront();
		}
		if (fbFacetBody != null)
		{
			fbFacetBody.BringToFront();
		}
		if (fbFacetTo != null)
		{
			fbFacetTo.BringToFront();
		}
		if (fbFacetUnread != null)
		{
			fbFacetUnread.BringToFront();
		}
		if (fbFacetCc != null)
		{
			fbFacetCc.BringToFront();
		}
		if (fbFacetNewer != null)
		{
			fbFacetNewer.BringToFront();
		}
		if (fbFacetHasBody != null)
		{
			fbFacetHasBody.BringToFront();
		}
		if (fbFacet30d != null)
		{
			fbFacet30d.BringToFront();
		}
		if (fbFacetRead != null)
		{
			fbFacetRead.BringToFront();
		}
		if (fbFacetFolder != null)
		{
			fbFacetFolder.BringToFront();
		}
		if (fbFacetLarger != null)
		{
			fbFacetLarger.BringToFront();
		}
		if (fbFacet1Y != null)
		{
			fbFacet1Y.BringToFront();
		}
		if (fbSaveSearch != null)
		{
			fbSaveSearch.BringToFront();
		}
		if (fbRecallSearch != null)
		{
			fbRecallSearch.BringToFront();
		}
		txtSearch.BringToFront();
		if (elbTop != null)
		{
			elbTop.BringToFront();
		}
		if (fbRailFill != null)
		{
			fbRailFill.BringToFront();
		}
		if (elbBottom != null)
		{
			elbBottom.BringToFront();
		}
		UpdateComposeButtonsEnabled();
	}

	private static FlatButton MakeToolbarButton(string name, string caption, LCARScolorStyles color)
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
		Size size = new Size(140, 28);
		flatButton.Size = size;
		return flatButton;
	}

	private static Label MakeEmptyLabel(string text)
	{
		Label label = new Label();
		label.AutoSize = false;
		label.TextAlign = ContentAlignment.MiddleCenter;
		label.ForeColor = Color.FromArgb(160, 100, 40);
		label.BackColor = Color.Black;
		label.Font = LcarsUiFont(10f);
		label.Text = text.ToUpperInvariant();
		return label;
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

	protected override void OnShellChromeLayout()
	{
		PlaceShellAlignedCloseButton(fbClose, 8);
		LayoutContent();
	}

	private void OnMailResize(object sender, EventArgs e)
	{
		LayoutContent();
	}

	private void OnMailShown(object sender, EventArgs e)
	{
		LayoutContent();
		if (!commsSyncTimer.Enabled)
		{
			commsSyncTimer.Start();
		}
		if (!cardDavLiveTimer.Enabled)
		{
			cardDavLiveTimer.Start();
		}
		if (!calDavLiveTimer.Enabled)
		{
			calDavLiveTimer.Start();
		}
		CardDavLiveBookCache.QueueBackgroundRefresh();
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
		});
		LoadAccountsFromStore();
		QueueBackgroundSearchIndex();
		if (activeAccount != null && MailSearchIndex.CountEntriesNeedingBodyPreview(activeAccount.Id) > 8000)
		{
			QueueBackgroundSearchIndex();
		}
		SetCommsMode(currentMode);
		if (currentMode == CommsMode.Mail)
		{
			if (accounts == null || accounts.Count == 0)
			{
				SetStatus("IMPORT A THUNDERBIRD PROFILE");
				ShowEmptyStates();
			}
			else
			{
				SetStatus("LOADING FOLDERS…");
				StartRefreshFolders();
			}
		}
	}

	private void LayoutContent()
	{
		int num = 44;
		bool flag;
		int num13;
		int num14;
		int num15;
		int num16;
		int num17;
		int num18;
		checked
		{
			int num2 = Math.Max(num + 200, ClientSize.Width - 8 - 108);
			Point location;
			if (elbTop != null)
			{
				Elbow elbow = elbTop;
				location = new Point(8, 4);
				elbow.Location = location;
				Elbow elbow2 = elbTop;
				Size size = new Size(30, 36);
				elbow2.Size = size;
				elbTop.ButtonText = "";
				elbTop.Text = "";
			}
			flag = currentMode == CommsMode.Mail;
			int num3 = 42;
			int num4 = num;
			num4 = PlaceTopTool(fbModeMail, num4, num3, 8);
			num4 = PlaceTopTool(fbModeChat, num4, num3, 8);
			num4 = PlaceTopTool(fbModeRss, num4, num3, 8);
			num4 = PlaceTopTool(fbModeCal, num4, num3, 8);
			int num5 = num3 + 36 + 4;
			int num6 = num;
			if (flag)
			{
				num6 = PlaceTopTool(fbFilters, num6, num5, 6);
				num6 = PlaceTopTool(fbPgp, num6, num5, 6);
				num6 = PlaceTopTool(fbAbook, num6, num5, 6);
				num6 = PlaceTopTool(fbCardDav, num6, num5, 6);
				num6 = PlaceTopTool(fbNews, num6, num5, 6);
				num6 = PlaceTopTool(fbEws, num6, num5, 6);
				num6 = PlaceTopTool(fbIndexNow, num6, num5, 6);
				num6 = PlaceTopTool(fbCache, num6, num5, 6);
				num6 = PlaceTopTool(fbImport, num6, num5, 6);
			}
			SetMailToolVisible(flag);
			int num7 = num5 + 32 + 4;
			int num8 = num;
			if (flag)
			{
				num8 = PlaceTopTool(fbSearch, num8, num7, 6);
				num8 = PlaceTopTool(fbFacetFrom, num8, num7, 4);
				num8 = PlaceTopTool(fbFacetSubj, num8, num7, 4);
				num8 = PlaceTopTool(fbFacetBody, num8, num7, 4);
				num8 = PlaceTopTool(fbFacetTo, num8, num7, 4);
				num8 = PlaceTopTool(fbFacetUnread, num8, num7, 4);
				num8 = PlaceTopTool(fbFacetCc, num8, num7, 4);
				num8 = PlaceTopTool(fbFacetNewer, num8, num7, 4);
				num8 = PlaceTopTool(fbFacetHasBody, num8, num7, 4);
				num8 = PlaceTopTool(fbFacet30d, num8, num7, 4);
				num8 = PlaceTopTool(fbFacetRead, num8, num7, 4);
				num8 = PlaceTopTool(fbFacetFolder, num8, num7, 4);
				num8 = PlaceTopTool(fbFacetLarger, num8, num7, 4);
				num8 = PlaceTopTool(fbFacet1Y, num8, num7, 4);
				num8 = PlaceTopTool(fbSaveSearch, num8, num7, 4);
				num8 = PlaceTopTool(fbRecallSearch, num8, num7, 6);
				if (txtSearch != null)
				{
					txtSearch.Visible = true;
					location = (txtSearch.Location = new Point(num8, num7 + 2));
					txtSearch.Width = Math.Max(80, num2 - 12 - num8);
				}
			}
			else if (txtSearch != null)
			{
				txtSearch.Visible = false;
			}
			SetMailFacetVisible(flag);
			PlaceShellAlignedCloseButton(fbClose, 8);
			if (fbClose != null)
			{
				fbClose.Width = 108;
				fbClose.Left = num2;
			}
			int num9 = 42;
			int stepY = 32;
			if (flag)
			{
				num9 = PlaceRightRail(fbNew, num2, num9, stepY);
				num9 = PlaceRightRail(fbDrafts, num2, num9, stepY);
				num9 = PlaceRightRail(fbReply, num2, num9, stepY);
				num9 = PlaceRightRail(fbReplyAll, num2, num9, stepY);
				num9 = PlaceRightRail(fbForward, num2, num9, stepY);
				num9 = PlaceRightRail(fbDelete, num2, num9, stepY);
				num9 = PlaceRightRail(fbMove, num2, num9, stepY);
				num9 = PlaceRightRail(fbThreads, num2, num9, stepY);
				num9 = PlaceRightRail(fbRefresh, num2, num9, stepY);
			}
			SetMailRailVisible(flag);
			if (lblStatus != null)
			{
				int num10 = (flag ? (num7 + 32 + 2) : (num5 + 4));
				location = (lblStatus.Location = new Point(num, num10));
				Label label = lblStatus;
				Size size = new Size(Math.Max(40, num2 - num - 8), 20);
				label.Size = size;
			}
			if (elbBottom != null)
			{
				location = (elbBottom.Location = new Point(8, ClientSize.Height - elbBottom.Height - 4));
				Elbow elbow3 = elbBottom;
				Size size = new Size(30, 36);
				elbow3.Size = size;
			}
			if (fbRailFill != null)
			{
				int num11 = 40;
				int num12 = ((elbBottom == null) ? (ClientSize.Height - 40) : elbBottom.Top);
				FlatButton flatButton = fbRailFill;
				location = new Point(8, num11);
				flatButton.Location = location;
				FlatButton flatButton2 = fbRailFill;
				Size size = new Size(8, Math.Max(20, num12 - num11));
				flatButton2.Size = size;
			}
			num13 = (flag ? (num7 + 32 + 4 + 22) : (num3 + 36 + 8));
			num14 = Math.Max(80, ClientSize.Height - num13 - 40);
			num15 = num;
			num16 = num2 - 8;
			if (modeHost != null)
			{
				Panel panel = modeHost;
				location = new Point(num15, num13);
				panel.Location = location;
				Panel panel2 = modeHost;
				Size size = new Size(Math.Max(100, num16 - num15), num14);
				panel2.Size = size;
				modeHost.Visible = !flag;
				modeHost.BringToFront();
			}
			if (folderHost == null || messageHost == null || readHost == null)
			{
				return;
			}
			num17 = Math.Max(240, num16 - num15);
			num18 = 24;
		}
		int num19 = Math.Min(200, Math.Max(140, num17 / 5));
		int num20 = Math.Max(220, num17 / 3);
		checked
		{
			int num21 = num17 - num19 - num20 - num18;
			if (num21 < 180)
			{
				int num22 = 180 - num21;
				num20 = Math.Max(160, num20 - num22);
				num21 = num17 - num19 - num20 - num18;
			}
			if (num21 < 120)
			{
				num21 = 120;
			}
			folderHost.Visible = flag;
			messageHost.Visible = flag;
			readHost.Visible = flag;
			if (paneSep1 != null)
			{
				paneSep1.Visible = flag;
			}
			if (paneSep2 != null)
			{
				paneSep2.Visible = flag;
			}
			if (flag)
			{
				LcarsScrollPanel lcarsScrollPanel = folderHost;
				Point location = new Point(num15, num13);
				lcarsScrollPanel.Location = location;
				LcarsScrollPanel lcarsScrollPanel2 = folderHost;
				Size size = new Size(num19, num14);
				lcarsScrollPanel2.Size = size;
				if (paneSep1 != null)
				{
					Panel panel3 = paneSep1;
					location = new Point(folderHost.Right + 5, num13 + 4);
					panel3.Location = location;
					Panel panel4 = paneSep1;
					size = new Size(2, Math.Max(40, num14 - 8));
					panel4.Size = size;
				}
				LcarsScrollPanel lcarsScrollPanel3 = messageHost;
				location = new Point(folderHost.Right + 10 + 2, num13);
				lcarsScrollPanel3.Location = location;
				LcarsScrollPanel lcarsScrollPanel4 = messageHost;
				size = new Size(num21, num14);
				lcarsScrollPanel4.Size = size;
				if (paneSep2 != null)
				{
					Panel panel5 = paneSep2;
					location = new Point(messageHost.Right + 5, num13 + 4);
					panel5.Location = location;
					Panel panel6 = paneSep2;
					size = new Size(2, Math.Max(40, num14 - 8));
					panel6.Size = size;
				}
				LcarsScrollPanel lcarsScrollPanel5 = readHost;
				location = new Point(messageHost.Right + 10 + 2, num13);
				lcarsScrollPanel5.Location = location;
				LcarsScrollPanel lcarsScrollPanel6 = readHost;
				size = new Size(Math.Max(120, num16 - readHost.Left), num14);
				lcarsScrollPanel6.Size = size;
				LayoutEmptyLabel(lblFolderEmpty, folderHost);
				LayoutEmptyLabel(lblMessageEmpty, messageHost);
				LayoutEmptyLabel(lblReadEmpty, readHost);
				int num23 = Math.Max(100, readHost.ContentPanel.Width - 12);
				Label label2 = lblReadMeta;
				size = new Size(num23, 0);
				label2.MaximumSize = size;
				Label label3 = lblReadSubject;
				size = new Size(num23, 0);
				label3.MaximumSize = size;
				Label label4 = bodyLabel;
				size = new Size(num23, 0);
				label4.MaximumSize = size;
				if (htmlReadPane != null)
				{
					htmlReadPane.Width = num23;
				}
				if (lblReadAttachments != null)
				{
					Label label5 = lblReadAttachments;
					size = new Size(num23, 0);
					label5.MaximumSize = size;
				}
				if (lstReadAttachments != null)
				{
					lstReadAttachments.Width = num23;
				}
				folderHost.Relayout();
				messageHost.Relayout();
				readHost.Relayout();
			}
		}
	}

	private void SetMailToolVisible(bool visible)
	{
		FlatButton[] array = new FlatButton[9] { fbFilters, fbPgp, fbAbook, fbCardDav, fbNews, fbEws, fbIndexNow, fbCache, fbImport };
		foreach (FlatButton flatButton in array)
		{
			if (flatButton != null)
			{
				flatButton.Visible = visible;
			}
		}
	}

	private void SetMailFacetVisible(bool visible)
	{
		FlatButton[] array = new FlatButton[16]
		{
			fbSearch, fbFacetFrom, fbFacetSubj, fbFacetBody, fbFacetTo, fbFacetUnread, fbFacetCc, fbFacetNewer, fbFacetHasBody, fbFacet30d,
			fbFacetRead, fbFacetFolder, fbFacetLarger, fbFacet1Y, fbSaveSearch, fbRecallSearch
		};
		foreach (FlatButton flatButton in array)
		{
			if (flatButton != null)
			{
				flatButton.Visible = visible;
			}
		}
	}

	private void SetMailRailVisible(bool visible)
	{
		FlatButton[] array = new FlatButton[9] { fbNew, fbDrafts, fbReply, fbReplyAll, fbForward, fbDelete, fbMove, fbThreads, fbRefresh };
		foreach (FlatButton flatButton in array)
		{
			if (flatButton != null)
			{
				flatButton.Visible = visible;
			}
		}
	}

	private void EnsureModeEmbeds()
	{
		if (modeHost != null)
		{
			if (rssEmbed == null)
			{
				rssEmbed = new frmRssFeeds();
				EmbedChildForm(rssEmbed);
			}
			if (chatEmbed == null)
			{
				chatEmbed = new frmChat();
				EmbedChildForm(chatEmbed);
			}
			if (calEmbed == null)
			{
				calEmbed = new frmCalDavSync();
				EmbedChildForm(calEmbed);
			}
		}
	}

	private void EmbedChildForm(Form child)
	{
		if (child != null && modeHost != null)
		{
			child.TopLevel = false;
			child.FormBorderStyle = FormBorderStyle.None;
			child.ControlBox = false;
			if (child is LCARSForm lCARSForm)
			{
				lCARSForm.BindToWorkingArea = false;
			}
			child.Dock = DockStyle.Fill;
			child.Visible = false;
			modeHost.Controls.Add(child);
			child.Show();
		}
	}

	private void SetCommsMode(CommsMode mode)
	{
		currentMode = mode;
		EnsureModeEmbeds();
		fbModeMail.Color = ((mode == CommsMode.Mail) ? LCARScolorStyles.PrimaryFunction : LCARScolorStyles.MiscFunction);
		fbModeChat.Color = ((mode == CommsMode.Chat) ? LCARScolorStyles.PrimaryFunction : LCARScolorStyles.MiscFunction);
		fbModeRss.Color = ((mode == CommsMode.Rss) ? LCARScolorStyles.PrimaryFunction : LCARScolorStyles.MiscFunction);
		fbModeCal.Color = ((mode == CommsMode.Calendar) ? LCARScolorStyles.PrimaryFunction : LCARScolorStyles.MiscFunction);
		if (rssEmbed != null)
		{
			rssEmbed.Visible = mode == CommsMode.Rss;
		}
		if (chatEmbed != null)
		{
			chatEmbed.Visible = mode == CommsMode.Chat;
		}
		if (calEmbed != null)
		{
			calEmbed.Visible = mode == CommsMode.Calendar;
		}
		LayoutContent();
		switch (mode)
		{
		case CommsMode.Mail:
			SetStatus((accounts != null && accounts.Count > 0) ? "MAIL" : "IMPORT A THUNDERBIRD PROFILE");
			break;
		case CommsMode.Chat:
			SetStatus("CHAT");
			break;
		case CommsMode.Rss:
			SetStatus("RSS");
			break;
		default:
			SetStatus("CALENDAR");
			break;
		}
	}

	private int PlaceTopTool(FlatButton btn, int x, int y, int gapAfter)
	{
		if (btn == null)
		{
			return x;
		}
		Point location = new Point(x, y);
		btn.Location = location;
		btn.BringToFront();
		return checked(btn.Right + gapAfter);
	}

	private int PlaceRightRail(FlatButton btn, int x, int y, int stepY)
	{
		if (btn == null)
		{
			return y;
		}
		Size size = new Size(108, 28);
		btn.Size = size;
		Point location = new Point(x, y);
		btn.Location = location;
		btn.ButtonTextAlign = ContentAlignment.BottomRight;
		btn.BringToFront();
		return checked(y + stepY);
	}

	private static void LayoutEmptyLabel(Label lbl, LcarsScrollPanel host)
	{
		if (lbl != null && host != null)
		{
			Point location = new Point(4, Math.Max(8, checked(host.ClientSize.Height - 48) / 2));
			lbl.Location = location;
			Size size = new Size(Math.Max(80, checked(host.ContentPanel.Width - 8)), 48);
			lbl.Size = size;
		}
	}

	private void OnCloseClick(object sender, EventArgs e)
	{
		Close();
	}

	private void SetStatus(string text)
	{
		if (lblStatus != null)
		{
			string text2 = ((text != null) ? text : "").Trim();
			if (text2.Length == 0)
			{
				text2 = "READY";
			}
			lblStatus.Text = text2.ToUpperInvariant();
		}
	}

	private void SetBusy(bool busy)
	{
		if (fbRefresh != null)
		{
			fbRefresh.Clickable = !busy;
		}
		if (fbImport != null)
		{
			fbImport.Clickable = !busy;
		}
		if (fbFilters != null)
		{
			fbFilters.Clickable = !busy;
		}
		if (fbPgp != null)
		{
			fbPgp.Clickable = !busy;
		}
		UpdateComposeButtonsEnabled();
	}

	private void UpdateComposeButtonsEnabled()
	{
		bool flag = activeAccount != null;
		bool flag2 = ((selectedHeader != null || selectedMessages.Count > 0) ? true : false);
		bool flag3 = (((folderWorker != null && folderWorker.IsBusy) || (headerWorker != null && headerWorker.IsBusy) || (bodyWorker != null && bodyWorker.IsBusy) || (mutationWorker != null && mutationWorker.IsBusy) || (attachmentWorker != null && attachmentWorker.IsBusy)) ? true : false);
		bool flag4 = !string.IsNullOrWhiteSpace(selectedFolderFullName);
		if (fbNew != null)
		{
			fbNew.Clickable = ((flag && !flag3) ? true : false);
		}
		if (fbReply != null)
		{
			fbReply.Clickable = ((flag && flag2 && !flag3) ? true : false);
		}
		if (fbReplyAll != null)
		{
			fbReplyAll.Clickable = ((flag && flag2 && !flag3) ? true : false);
		}
		if (fbForward != null)
		{
			fbForward.Clickable = ((flag && flag2 && !flag3) ? true : false);
		}
		if (fbDelete != null)
		{
			fbDelete.Clickable = ((flag && flag2 && flag4 && !flag3) ? true : false);
		}
		if (fbMove != null)
		{
			fbMove.Clickable = ((flag && flag2 && flag4 && !flag3) ? true : false);
		}
	}

	private void LoadAccountsFromStore()
	{
		accounts = AccountStore.LoadAccounts();
		if (accounts == null || accounts.Count == 0)
		{
			activeAccount = null;
			return;
		}
		activeAccount = accounts[0];
		QueueBackgroundCommsSync();
	}

	private void QueueBackgroundSearchIndex()
	{
		if (accounts == null || accounts.Count == 0)
		{
			return;
		}
		MailSearchBackgroundIndexer.QueueIndexAccounts(accounts, [SpecialName] (MailAccount acct) => ResolvePassword(acct, promptIfMissing: false), mailService);
		ThreadPool.QueueUserWorkItem([SpecialName] (object state) =>
		{
			Thread.Sleep(5000);
			try
			{
				if (!IsDisposed && IsHandleCreated)
				{
					string lastRunSummary = MailSearchBackgroundIndexer.LastRunSummary;
					if (!string.IsNullOrWhiteSpace(lastRunSummary))
					{
						BeginInvoke((Action)([SpecialName] () =>
						{
							SetStatus(lastRunSummary.ToUpperInvariant());
						}));
					}
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		});
	}

	private void QueueBackgroundCommsSync()
	{
		string text = ((activeAccount != null) ? activeAccount.Id : null);
		ThreadPool.QueueUserWorkItem([SpecialName] (object state) =>
		{
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			try
			{
				num = AddressBookStore.TrySyncStoredCardDavBidirectional();
				num2 = LcarsCalendarBridge.TrySyncStoredCalDavBidirectional();
				if (accounts != null)
				{
					num3 = EwsBackgroundSync.TryMergeCalendarsForAccounts(accounts);
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
			int num4 = 0;
			try
			{
				if (!string.IsNullOrWhiteSpace(text))
				{
					num4 = MailCalendarInvitationBridge.TryScanIndexedBodiesForInvitations(text, 96);
				}
			}
			catch (Exception projectError2)
			{
				ProjectData.SetProjectError(projectError2);
				ProjectData.ClearProjectError();
			}
			try
			{
				if (!IsDisposed && IsHandleCreated)
				{
					BeginInvoke((Action)([SpecialName] () =>
					{
						if (num > 0 || num2 > 0 || num3 > 0 || num4 > 0)
						{
							SetStatus("BACKGROUND SYNC — CARD +" + num + " / CAL +" + num2 + " / EWS CAL +" + num3 + " / INVITE +" + num4);
						}
						else if (num2 < 0)
						{
							SetStatus("BACKGROUND CALDAV SYNC WARNING");
						}
					}));
				}
			}
			catch (Exception projectError3)
			{
				ProjectData.SetProjectError(projectError3);
				ProjectData.ClearProjectError();
			}
		});
	}

	private string ResolvePassword(MailAccount account, bool promptIfMissing)
	{
		if (account == null)
		{
			return null;
		}
		MailAccount mailAccount = account;
		string text = ((mailAccount.Id != null) ? mailAccount.Id : "");
		if (text.Length == 0)
		{
			return null;
		}
		if (EwsMailBridge.IsEwsAccount(account) && EwsMailBridge.ResolveOAuthToken().Length > 0)
		{
			sessionPasswords[text] = "";
			return "";
		}
		if (MailOAuthProviders.UsesOAuth(account))
		{
			string accessToken = null;
			if (MailOAuthStore.TryEnsureAccessToken(account, ref accessToken) && !string.IsNullOrWhiteSpace(accessToken))
			{
				return accessToken;
			}
			if (!promptIfMissing)
			{
				return null;
			}
			account.AuthMethod = "oauth2";
			PersistAccountAuthMethod(account);
			string accessToken2 = null;
			if (!MailOAuthBrowserFlow.TryAuthorize(this, account, ref accessToken2))
			{
				SetStatus((MailOAuthBrowserFlow.LastError != null) ? MailOAuthBrowserFlow.LastError : "SIGN-IN FAILED");
				return null;
			}
			PersistAccountAuthMethod(account);
			return accessToken2;
		}
		string value = null;
		if (sessionPasswords.TryGetValue(text, out value))
		{
			return value;
		}
		string password = null;
		if (AccountStore.TryGetPassword(text, ref password))
		{
			sessionPasswords[text] = password;
			return password;
		}
		if (!promptIfMissing)
		{
			return null;
		}
		if (EwsMailBridge.IsEwsAccount(account) && EwsMailBridge.ResolveOAuthToken().Length > 0)
		{
			sessionPasswords[text] = "";
			return "";
		}
		mailAccount = account;
		string text2 = ((mailAccount.EmailAddress != null) ? mailAccount.EmailAddress : account.Username);
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = account.DisplayName;
		}
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = text;
		}
		return PromptAndStorePassword(account, text2);
	}

	private string PromptAndStorePassword(MailAccount account, string label)
	{
		string text = ((account.Id != null) ? account.Id : "");
		string text2 = LcarsCommsUi.PromptPassword(this, "Enter the mail password for " + label + ".");
		if (text2 == null)
		{
			return null;
		}
		sessionPasswords[text] = text2;
		AccountStore.SavePassword(text, text2);
		return text2;
	}

	private void PersistAccountAuthMethod(MailAccount account)
	{
		if (account == null || string.IsNullOrWhiteSpace(account.Id))
		{
			return;
		}
		List<MailAccount> list = AccountStore.LoadAccounts();
		bool flag = false;
		checked
		{
			int num = list.Count - 1;
			for (int i = 0; i <= num; i++)
			{
				if (string.Equals(list[i].Id, account.Id, StringComparison.OrdinalIgnoreCase))
				{
					list[i].AuthMethod = account.AuthMethod;
					flag = true;
					break;
				}
			}
			if (flag)
			{
				AccountStore.SaveAccounts(list);
			}
		}
	}

	private void OnImportClick(object sender, EventArgs e)
	{
		string text = LcarsCommsUi.PickFolder(this, "THUNDERBIRD PROFILE (PREFS.JS)");
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		string prefsJsPath = Path.Combine(text, "prefs.js");
		List<MailAccount> list = ThunderbirdPrefsParser.ParsePrefsJs(prefsJsPath);
		if (list == null || list.Count == 0)
		{
			string text2 = ((ThunderbirdPrefsParser.LastError != null) ? ThunderbirdPrefsParser.LastError : "No IMAP accounts found in prefs.js.");
			SetStatus(text2);
			LcarsCommsUi.Alert(text2, "LCARS COMMS — IMPORT ERROR");
			return;
		}
		List<MailAccount> list2 = AccountStore.LoadAccounts();
		if (list2 != null && list2.Count > 0)
		{
			string prompt = "This will replace " + list2.Count + " saved account(s) with " + list.Count + " from the selected profile.\r\n\r\nContinue?";
			if (!LcarsCommsUi.Confirm(prompt, "LCARS COMMS — RE-IMPORT"))
			{
				SetStatus("IMPORT CANCELLED");
				return;
			}
		}
		foreach (MailAccount item in list)
		{
			string text3 = ResolvePassword(item, promptIfMissing: true);
			if (text3 == null)
			{
				SetStatus("IMPORT CANCELLED — PASSWORD REQUIRED");
				LcarsCommsUi.Alert("Import cancelled — password required for each account.", "LCARS COMMS — IMPORT");
				return;
			}
		}
		if (!AccountStore.SaveAccounts(list))
		{
			string text4 = ((AccountStore.LastError != null) ? AccountStore.LastError : "Failed to save accounts.");
			SetStatus(text4);
			LcarsCommsUi.Alert(text4, "LCARS COMMS — IMPORT ERROR");
			return;
		}
		LoadAccountsFromStore();
		selectedFolderFullName = null;
		selectedHeader = null;
		ClearReadingPane();
		ClearMessageList();
		string text5 = "IMPORTED " + list.Count + " ACCOUNT(S)";
		int num = AddressBookStore.TryImportFromProfile(text);
		if (num > 0)
		{
			text5 = text5 + " + " + num + " CONTACT(S)";
		}
		else if (num < 0)
		{
			string text6 = ((AddressBookStore.LastError != null) ? AddressBookStore.LastError : "address book import failed");
			text5 = text5 + " — " + text6.ToUpperInvariant();
		}
		int num2 = MailFilterStore.TryImportFromProfile(text);
		if (num2 > 0)
		{
			text5 = text5 + " + " + num2 + " FILTER(S)";
		}
		else if (num2 < 0)
		{
			string text7 = ((MailFilterStore.LastError != null) ? MailFilterStore.LastError : "filter import failed");
			text5 = text5 + " — " + text7.ToUpperInvariant();
		}
		lastImportedProfileDir = text;
		if (AddressBookStore.TryApplyThunderbirdCardDavFromProfile(text) > 0)
		{
			text5 += " + CARDDAV";
		}
		AddressBookStore.TrySyncStoredCardDavBidirectional();
		CardDavLiveBookCache.QueueBackgroundRefresh();
		if (LcarsCalendarBridge.TryApplyThunderbirdCalDavFromProfile(text) > 0)
		{
			text5 += " + CALDAV";
		}
		LcarsCalendarBridge.TrySyncStoredCalDavBidirectional();
		SetStatus(text5);
		StartRefreshFolders();
	}

	private void OnFiltersClick(object sender, EventArgs e)
	{
		string password = ((activeAccount != null) ? ResolvePassword(activeAccount, promptIfMissing: false) : null);
		using frmFilters frmFilters2 = new frmFilters(activeAccount, password, selectedFolderFullName, mailService.ImapService, RefreshCurrentFolderHeaders);
		frmFilters2.ShowDialog(this);
	}

	private void OnDraftsClick(object sender, EventArgs e)
	{
		if (activeAccount == null)
		{
			SetStatus("IMPORT A THUNDERBIRD PROFILE");
			return;
		}
		if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift)
		{
			TrySelectDraftsFolder();
			return;
		}
		string text = ResolvePassword(activeAccount, promptIfMissing: false);
		List<MailMessageHeader> imapDraftHeaders = null;
		if (text != null)
		{
			string text2 = mailService.ResolveDraftsFolderFullName(activeAccount, text);
			if (text2.Length > 0)
			{
				try
				{
					imapDraftHeaders = mailService.SyncHeaders(activeAccount, text, text2);
				}
				catch (Exception projectError)
				{
					ProjectData.SetProjectError(projectError);
					imapDraftHeaders = null;
					ProjectData.ClearProjectError();
				}
			}
		}
		MailAccount mailAccount = activeAccount;
		using frmDraftPicker frmDraftPicker2 = new frmDraftPicker((mailAccount.Id != null) ? mailAccount.Id : "", imapDraftHeaders);
		if (frmDraftPicker2.ShowDialog(this) == DialogResult.OK)
		{
			if (frmDraftPicker2.SelectedImapHeader != null)
			{
				OpenImapDraft(frmDraftPicker2.SelectedImapHeader);
			}
			else
			{
				OpenComposeFromDraft(frmDraftPicker2.SelectedDraft);
			}
		}
	}

	private void OnPgpClick(object sender, EventArgs e)
	{
		using frmPgpKeys frmPgpKeys2 = new frmPgpKeys();
		frmPgpKeys2.ShowDialog(this);
	}

	private void OnCardDavClick(object sender, EventArgs e)
	{
		using frmCardDavSync frmCardDavSync2 = new frmCardDavSync((lastImportedProfileDir != null) ? lastImportedProfileDir : "");
		frmCardDavSync2.ShowDialog(this);
	}

	private void OnAddressBookClick(object sender, EventArgs e)
	{
		using frmAddressBook frmAddressBook2 = new frmAddressBook();
		frmAddressBook2.ShowDialog(this);
	}

	private void OnNewsClick(object sender, EventArgs e)
	{
		using frmNews frmNews2 = new frmNews();
		frmNews2.ShowDialog(this);
	}

	private void OnEwsClick(object sender, EventArgs e)
	{
		using frmEwsSync frmEwsSync2 = new frmEwsSync();
		frmEwsSync2.ShowDialog(this);
	}

	private void OnCacheClick(object sender, EventArgs e)
	{
		using (frmOfflineSettings frmOfflineSettings2 = new frmOfflineSettings((activeAccount != null) ? activeAccount.Id : null))
		{
			frmOfflineSettings2.ShowDialog(this);
		}
		if (activeAccount != null)
		{
			MailOfflinePolicy.ApplyBodyRetention(activeAccount.Id);
			MailOfflinePolicy.ApplyHeaderRetention(activeAccount.Id);
			SetStatus("CACHE RETENTION — BODY " + MailOfflinePolicy.BodyRetentionDays() + "D / HDR " + MailOfflinePolicy.HeaderRetentionDays() + "D APPLIED");
		}
	}

	private void OnRefreshClick(object sender, EventArgs e)
	{
		if (accounts == null || accounts.Count == 0)
		{
			SetStatus("IMPORT A THUNDERBIRD PROFILE");
		}
		else
		{
			StartRefreshFolders();
		}
	}

	private void StartRefreshFolders()
	{
		if (folderWorker.IsBusy)
		{
			return;
		}
		if (accounts == null || accounts.Count == 0)
		{
			SetStatus("IMPORT A THUNDERBIRD PROFILE");
			return;
		}
		List<FolderRefreshEntry> list = new List<FolderRefreshEntry>();
		foreach (MailAccount account in accounts)
		{
			if (account != null)
			{
				string text = ResolvePassword(account, promptIfMissing: true);
				if (text == null)
				{
					SetStatus("PASSWORD REQUIRED");
					return;
				}
				list.Add(new FolderRefreshEntry
				{
					Account = account,
					Password = text
				});
			}
		}
		if (list.Count == 0)
		{
			SetStatus("IMPORT A THUNDERBIRD PROFILE");
			return;
		}
		SetBusy(busy: true);
		SetStatus("LISTING FOLDERS…");
		folderWorker.RunWorkerAsync(new FolderWorkerArgs
		{
			Entries = list
		});
	}

	private void FolderWorker_DoWork(object sender, DoWorkEventArgs e)
	{
		FolderWorkerArgs folderWorkerArgs = (FolderWorkerArgs)e.Argument;
		List<AccountFolderSection> list = new List<AccountFolderSection>();
		if (folderWorkerArgs == null || folderWorkerArgs.Entries == null)
		{
			e.Result = list;
			return;
		}
		foreach (FolderRefreshEntry entry in folderWorkerArgs.Entries)
		{
			if (entry == null || entry.Account == null)
			{
				continue;
			}
			AccountFolderSection accountFolderSection = new AccountFolderSection();
			accountFolderSection.Account = entry.Account;
			AccountFolderSection accountFolderSection2 = accountFolderSection;
			try
			{
				accountFolderSection2.Folders = mailService.ListFolders(entry.Account, entry.Password);
				if (accountFolderSection2.Folders == null || accountFolderSection2.Folders.Count == 0)
				{
					accountFolderSection2.Folders = FallbackInboxOnly();
					accountFolderSection2.ErrorText = "NO FOLDERS — USING INBOX";
				}
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Exception ex2 = ex;
				accountFolderSection2.Folders = FallbackInboxOnly();
				accountFolderSection2.ErrorText = ex2.Message;
				ProjectData.ClearProjectError();
			}
			list.Add(accountFolderSection2);
		}
		e.Result = list;
	}

	private void FolderWorker_Completed(object sender, RunWorkerCompletedEventArgs e)
	{
		SetBusy(busy: false);
		if (e.Error != null)
		{
			SetStatus("FOLDER ERROR: " + e.Error.Message);
			return;
		}
		if (!(e.Result is List<AccountFolderSection> list))
		{
			SetStatus("FOLDER ERROR");
			return;
		}
		PopulateFolderRailFromSections(list);
		int num = 0;
		int num2 = 0;
		checked
		{
			foreach (AccountFolderSection item in list)
			{
				if (item != null)
				{
					if (item.Folders != null)
					{
						num += item.Folders.Count;
					}
					if (!string.IsNullOrEmpty(item.ErrorText))
					{
						num2++;
					}
				}
			}
			if (num2 > 0)
			{
				SetStatus(num + " FOLDER(S) — " + num2 + " ACCOUNT WARNING(S)");
			}
			else
			{
				SetStatus(num + " FOLDER(S) — " + list.Count + " ACCOUNT(S)");
			}
		}
	}

	private static List<MailFolderInfo> FallbackInboxOnly()
	{
		List<MailFolderInfo> list = new List<MailFolderInfo>();
		list.Add(new MailFolderInfo
		{
			FullName = "INBOX",
			DisplayName = "INBOX"
		});
		return list;
	}

	private string ResolveInboxFolderName(MailAccount account, string password)
	{
		if (account == null)
		{
			return "INBOX";
		}
		if (lastFolderSections != null)
		{
			foreach (AccountFolderSection lastFolderSection in lastFolderSections)
			{
				if (lastFolderSection == null || lastFolderSection.Account == null || !string.Equals(lastFolderSection.Account.Id, account.Id, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				if (lastFolderSection.Folders == null)
				{
					break;
				}
				foreach (MailFolderInfo folder in lastFolderSection.Folders)
				{
					if (folder != null)
					{
						MailFolderInfo mailFolderInfo = folder;
						string text = ((mailFolderInfo.FullName != null) ? mailFolderInfo.FullName : "");
						mailFolderInfo = folder;
						string a = ((mailFolderInfo.DisplayName != null) ? mailFolderInfo.DisplayName : "");
						if (string.Equals(text, "INBOX", StringComparison.OrdinalIgnoreCase) || string.Equals(a, "INBOX", StringComparison.OrdinalIgnoreCase) || string.Equals(a, "Inbox", StringComparison.OrdinalIgnoreCase))
						{
							return text;
						}
					}
				}
				break;
			}
		}
		return "INBOX";
	}

	private MailAccount FindAccountById(string accountId)
	{
		if (accounts == null || string.IsNullOrWhiteSpace(accountId))
		{
			return null;
		}
		foreach (MailAccount account in accounts)
		{
			if (account != null && string.Equals(account.Id, accountId, StringComparison.OrdinalIgnoreCase))
			{
				return account;
			}
		}
		return null;
	}

	private void PopulateFolderRailFromSections(List<AccountFolderSection> sections)
	{
		if (folderHost == null)
		{
			return;
		}
		lastFolderSections = sections;
		ClearFolderSelection();
		ClearFolderRailControls();
		if (sections == null || sections.Count == 0)
		{
			lblFolderEmpty.Visible = true;
			folderHost.Relayout();
			return;
		}
		lblFolderEmpty.Visible = false;
		Panel contentPanel = folderHost.ContentPanel;
		int num = 4;
		checked
		{
			int num2 = Math.Max(80, contentPanel.Width - 8);
			if (sections.Count > 0)
			{
				FolderRowTag folderRowTag = new FolderRowTag();
				folderRowTag.IsUnifiedInbox = true;
				folderRowTag.Folder = new MailFolderInfo
				{
					FullName = "__UNIFIED_INBOX__",
					DisplayName = "INBOX"
				};
				folderRowTag.Flip = false;
				FolderRowTag tag = folderRowTag;
				FlatButton flatButton = MakeFolderButton("INBOX", flip: false);
				flatButton.Tag = tag;
				Point location = new Point(2, num);
				flatButton.Location = location;
				Size size = new Size(num2, 36);
				flatButton.Size = size;
				flatButton.Click += OnFolderButtonClick;
				contentPanel.Controls.Add(flatButton);
				num += 44;
			}
			foreach (AccountFolderSection section in sections)
			{
				if (section == null || section.Account == null)
				{
					continue;
				}
				MailAccount account = section.Account;
				MailAccount mailAccount = account;
				string accountId = ((mailAccount.Id != null) ? mailAccount.Id : "");
				bool flag = IsAccountCollapsed(accountId);
				FlatButton flatButton2 = MakeAccountHeaderButton(AccountDisplayLabel(account), flag);
				flatButton2.Tag = account;
				Point location = new Point(2, num);
				flatButton2.Location = location;
				Size size = new Size(num2, 30);
				flatButton2.Size = size;
				flatButton2.Click += OnAccountHeaderClick;
				contentPanel.Controls.Add(flatButton2);
				num += 34;
				if (flag)
				{
					continue;
				}
				bool flag2 = false;
				int num3 = Math.Max(70, num2 - 10);
				List<MailFolderInfo> list = section.Folders;
				if (list == null || list.Count == 0)
				{
					list = FallbackInboxOnly();
				}
				foreach (MailFolderInfo item in list)
				{
					if (item != null)
					{
						MailFolderInfo mailFolderInfo = item;
						string text = ((mailFolderInfo.DisplayName != null) ? mailFolderInfo.DisplayName : item.FullName);
						if (string.IsNullOrWhiteSpace(text))
						{
							text = item.FullName;
						}
						FolderRowTag folderRowTag = new FolderRowTag();
						folderRowTag.Account = account;
						folderRowTag.Folder = item;
						folderRowTag.Flip = flag2;
						FolderRowTag tag2 = folderRowTag;
						FlatButton flatButton3 = MakeFolderButton(text.ToUpperInvariant(), flag2);
						flag2 = !flag2;
						flatButton3.Tag = tag2;
						location = new Point(12, num);
						flatButton3.Location = location;
						size = new Size(num3, 36);
						flatButton3.Size = size;
						flatButton3.Click += OnFolderButtonClick;
						contentPanel.Controls.Add(flatButton3);
						num += 40;
					}
				}
			}
			folderHost.Relayout();
		}
	}

	private void ClearFolderRailControls()
	{
		Panel contentPanel = folderHost.ContentPanel;
		foreach (Control control in contentPanel.Controls)
		{
			if (control != lblFolderEmpty)
			{
				contentPanel.Controls.Remove(control);
				control.Dispose();
			}
		}
	}

	private void RebuildFolderRailFromCache()
	{
		if (lastFolderSections != null)
		{
			PopulateFolderRailFromSections(lastFolderSections);
		}
	}

	private bool IsAccountCollapsed(string accountId)
	{
		if (string.IsNullOrEmpty(accountId))
		{
			return false;
		}
		bool value = false;
		if (accountCollapsed.TryGetValue(accountId, out value))
		{
			return value;
		}
		return false;
	}

	private static string AccountDisplayLabel(MailAccount acct)
	{
		MailAccount mailAccount;
		if (EwsMailBridge.IsEwsAccount(acct))
		{
			mailAccount = acct;
			string text = ((mailAccount.DisplayName != null) ? mailAccount.DisplayName : acct.EmailAddress);
			if (string.IsNullOrWhiteSpace(text))
			{
				text = "EWS";
			}
			return "EWS · " + text;
		}
		mailAccount = acct;
		string text2 = ((mailAccount.EmailAddress != null) ? mailAccount.EmailAddress : acct.Username);
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = acct.DisplayName;
		}
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = acct.Id;
		}
		return ((text2 != null) ? text2 : "").ToUpperInvariant();
	}

	private static FlatButton MakeAccountHeaderButton(string caption, bool collapsed)
	{
		string text = (collapsed ? "▸ " : "▾ ");
		FlatButton flatButton = new FlatButton();
		flatButton.ButtonText = text + caption;
		flatButton.Text = flatButton.ButtonText;
		flatButton.Clickable = true;
		flatButton.Beeping = true;
		flatButton.Color = LCARScolorStyles.Orange;
		flatButton.ButtonTextAlign = ContentAlignment.MiddleLeft;
		flatButton.ButtonTextHeight = 14;
		return flatButton;
	}

	private static FlatButton MakeFolderButton(string caption, bool flip)
	{
		FlatButton flatButton = new FlatButton();
		flatButton.ButtonText = caption;
		flatButton.Text = caption;
		flatButton.Clickable = true;
		flatButton.Beeping = true;
		flatButton.ButtonTextAlign = ContentAlignment.MiddleLeft;
		flatButton.ButtonTextHeight = 14;
		flatButton.Color = FolderButtonColor(flip);
		return flatButton;
	}

	private static LCARScolorStyles FolderButtonColor(bool flip)
	{
		if (flip)
		{
			return LCARScolorStyles.MiscFunction;
		}
		return LCARScolorStyles.StaticTan;
	}

	private void OnAccountHeaderClick(object sender, EventArgs e)
	{
		if (sender is FlatButton { Tag: MailAccount tag })
		{
			MailAccount mailAccount = tag;
			string text = ((mailAccount.Id != null) ? mailAccount.Id : "");
			if (text.Length != 0)
			{
				bool flag = IsAccountCollapsed(text);
				accountCollapsed[text] = !flag;
				RebuildFolderRailFromCache();
			}
		}
	}

	private void OnFolderButtonClick(object sender, EventArgs e)
	{
		if (sender is FlatButton { Tag: FolderRowTag { Folder: not null } tag } flatButton && (tag.IsUnifiedInbox || tag.Account != null))
		{
			if (selectedFolderButton != null && selectedFolderButton.Tag is FolderRowTag folderRowTag)
			{
				selectedFolderButton.Color = FolderButtonColor(folderRowTag.Flip);
			}
			selectedFolderButton = flatButton;
			flatButton.Color = LCARScolorStyles.PrimaryFunction;
			unifiedInboxActive = tag.IsUnifiedInbox;
			if (tag.IsUnifiedInbox)
			{
				activeAccount = ((accounts != null && accounts.Count > 0) ? accounts[0] : null);
				selectedFolderFullName = "__UNIFIED_INBOX__";
			}
			else
			{
				activeAccount = tag.Account;
				selectedFolderFullName = tag.Folder.FullName;
			}
			crossFolderSearchActive = false;
			selectedHeader = null;
			selectedBody = null;
			ClearReadingPane();
			UpdateComposeButtonsEnabled();
			StartSyncHeaders(selectedFolderFullName);
		}
	}

	private void StartSyncHeaders(string folderFullName)
	{
		if (headerWorker.IsBusy)
		{
			return;
		}
		currentFolderHeaders = null;
		SetBusy(busy: true);
		SetStatus("SYNCING MESSAGES…");
		if (string.Equals(folderFullName, "__UNIFIED_INBOX__", StringComparison.Ordinal) || unifiedInboxActive)
		{
			List<FolderRefreshEntry> list = new List<FolderRefreshEntry>();
			if (accounts != null)
			{
				foreach (MailAccount account in accounts)
				{
					if (account != null)
					{
						string text = ResolvePassword(account, promptIfMissing: false);
						if (text != null)
						{
							list.Add(new FolderRefreshEntry
							{
								Account = account,
								Password = text
							});
						}
					}
				}
			}
			if (list.Count == 0)
			{
				SetBusy(busy: false);
				SetStatus("PASSWORD REQUIRED — USE REFRESH");
				return;
			}
			headerWorker.RunWorkerAsync(new HeaderWorkerArgs
			{
				Unified = true,
				Folder = "__UNIFIED_INBOX__",
				Entries = list
			});
		}
		else if (activeAccount == null)
		{
			SetBusy(busy: false);
		}
		else
		{
			string text2 = ResolvePassword(activeAccount, promptIfMissing: false);
			if (text2 == null)
			{
				SetBusy(busy: false);
				SetStatus("PASSWORD REQUIRED — USE REFRESH");
				return;
			}
			headerWorker.RunWorkerAsync(new HeaderWorkerArgs
			{
				Account = activeAccount,
				Password = text2,
				Folder = folderFullName,
				Unified = false
			});
		}
	}

	private void HeaderWorker_DoWork(object sender, DoWorkEventArgs e)
	{
		HeaderWorkerArgs headerWorkerArgs = (HeaderWorkerArgs)e.Argument;
		HeaderWorkerResult headerWorkerResult = new HeaderWorkerResult();
		headerWorkerResult.Folder = headerWorkerArgs.Folder;
		HeaderWorkerResult headerWorkerResult2 = headerWorkerResult;
		if (headerWorkerArgs != null && headerWorkerArgs.Unified && headerWorkerArgs.Entries != null)
		{
			List<MailMessageHeader> list = new List<MailMessageHeader>();
			foreach (FolderRefreshEntry entry in headerWorkerArgs.Entries)
			{
				if (entry == null || entry.Account == null)
				{
					continue;
				}
				try
				{
					string folder = ResolveInboxFolderName(entry.Account, entry.Password);
					List<MailMessageHeader> list2 = mailService.SyncHeaders(entry.Account, entry.Password, folder, 300);
					if (list2 == null)
					{
						continue;
					}
					foreach (MailMessageHeader item in list2)
					{
						if (item != null)
						{
							item.AccountId = entry.Account.Id;
							if (string.IsNullOrWhiteSpace(item.Folder))
							{
								item.Folder = folder;
							}
							list.Add(item);
						}
					}
				}
				catch (Exception projectError)
				{
					ProjectData.SetProjectError(projectError);
					ProjectData.ClearProjectError();
				}
			}
			list.Sort([SpecialName] (MailMessageHeader a, MailMessageHeader b) => b.DateUtc.CompareTo(a.DateUtc));
			headerWorkerResult2.Headers = list;
			e.Result = headerWorkerResult2;
			return;
		}
		try
		{
			headerWorkerResult2.Headers = mailService.SyncHeaders(headerWorkerArgs.Account, headerWorkerArgs.Password, headerWorkerArgs.Folder);
			List<MailFilterRule> list3 = MailFilterStore.LoadRules();
			if (list3 != null && list3.Count > 0 && headerWorkerResult2.Headers != null && headerWorkerResult2.Headers.Count > 0)
			{
				MailFilterRunSummary mailFilterRunSummary = ((!EwsMailBridge.IsEwsAccount(headerWorkerArgs.Account)) ? MailFilterEngine.ApplyRulesToFolder(headerWorkerArgs.Account, headerWorkerArgs.Password, headerWorkerArgs.Folder, headerWorkerResult2.Headers, mailService.ImapService, list3) : MailFilterEngineEws.ApplyRulesToFolder(headerWorkerArgs.Account, headerWorkerArgs.Password, headerWorkerArgs.Folder, headerWorkerResult2.Headers, mailService, list3));
				if (mailFilterRunSummary != null && mailFilterRunSummary.ActionsApplied > 0)
				{
					headerWorkerResult2.Headers = mailService.SyncHeaders(headerWorkerArgs.Account, headerWorkerArgs.Password, headerWorkerArgs.Folder);
					headerWorkerResult2.FilterNote = mailFilterRunSummary.ActionsApplied + " FILTER ACTION(S)";
				}
				else if (mailFilterRunSummary != null && mailFilterRunSummary.Warnings.Count > 0)
				{
					headerWorkerResult2.FilterNote = mailFilterRunSummary.Warnings[0];
				}
			}
		}
		catch (Exception ex)
		{
			ProjectData.SetProjectError(ex);
			Exception ex2 = ex;
			headerWorkerResult2.ErrorText = ex2.Message;
			headerWorkerResult2.Headers = MailCache.LoadHeaders(headerWorkerArgs.Account.Id, headerWorkerArgs.Folder);
			ProjectData.ClearProjectError();
		}
		e.Result = headerWorkerResult2;
	}

	private void HeaderWorker_Completed(object sender, RunWorkerCompletedEventArgs e)
	{
		SetBusy(busy: false);
		if (e.Error != null)
		{
			SetStatus("SYNC ERROR: " + e.Error.Message);
		}
		else if (e.Result is HeaderWorkerResult headerWorkerResult)
		{
			if (activeAccount != null)
			{
				MailOfflinePolicy.ApplyBodyRetention(activeAccount.Id);
				MailOfflinePolicy.ApplyHeaderRetention(activeAccount.Id);
			}
			currentFolderHeaders = headerWorkerResult.Headers;
			if (activeAccount != null && headerWorkerResult.Headers != null && headerWorkerResult.Headers.Count > 0)
			{
				MailSearchIndex.UpsertHeaders(activeAccount.Id, headerWorkerResult.Folder, headerWorkerResult.Headers);
			}
			int shownCount = ApplyMessageListFilter();
			if (!string.IsNullOrEmpty(headerWorkerResult.ErrorText))
			{
				SetStatus("SYNC WARNING: " + headerWorkerResult.ErrorText);
			}
			else if (!string.IsNullOrEmpty(headerWorkerResult.FilterNote))
			{
				UpdateMessageCountStatus(shownCount);
				SetStatus(headerWorkerResult.FilterNote.ToUpperInvariant());
			}
			else
			{
				UpdateMessageCountStatus(shownCount);
			}
		}
	}

	private void OnSearchEnter(object sender, EventArgs e)
	{
		SetStatus("SEARCH: fts · thread: · has:attachment · to: · cc: · messageid: · list-id: · return-path: · replyto: · TAP SEARCH");
	}

	private void OnSearchKeyDown(object sender, KeyEventArgs e)
	{
		if (e != null && e.KeyCode == Keys.Return)
		{
			OnSearchButtonClick(fbSearch, EventArgs.Empty);
			e.Handled = true;
			e.SuppressKeyPress = true;
		}
	}

	private void ApplySearchFacet(string prefix)
	{
		if (txtSearch == null)
		{
			return;
		}
		string text = ((prefix != null) ? prefix : "").Trim();
		if (text.Length != 0)
		{
			string text2 = txtSearch.Text.Trim();
			if (text2.StartsWith(text, StringComparison.OrdinalIgnoreCase))
			{
				ApplyFtsSearch(text2.Substring(text.Length).Trim());
				return;
			}
			txtSearch.Text = text + " ";
			txtSearch.SelectionStart = txtSearch.Text.Length;
			txtSearch.Focus();
		}
	}

	private void OnSearchButtonClick(object sender, EventArgs e)
	{
		string text = ((txtSearch != null) ? txtSearch.Text.Trim() : "");
		if (text.Length == 0)
		{
			SetStatus("SEARCH: gloda: · from: · to: · cc: · newer:7d · is:unread · global: · index:");
		}
		else if (text.StartsWith("fts:", StringComparison.OrdinalIgnoreCase) || text.StartsWith("gloda:", StringComparison.OrdinalIgnoreCase))
		{
			int startIndex = (text.StartsWith("gloda:", StringComparison.OrdinalIgnoreCase) ? 6 : 4);
			ApplyFtsSearch(text.Substring(startIndex).Trim());
		}
		else if (text.StartsWith("global:", StringComparison.OrdinalIgnoreCase))
		{
			StartGlobalSearch(text.Substring(7).Trim());
		}
		else if (text.StartsWith("index:", StringComparison.OrdinalIgnoreCase))
		{
			ApplyLocalIndexSearch(text.Substring(6).Trim());
		}
		else if (text.StartsWith("imap:", StringComparison.OrdinalIgnoreCase))
		{
			OnSearchTextChanged(txtSearch, EventArgs.Empty);
		}
		else if (activeAccount != null && text.Length >= 2)
		{
			if (crossFolderSearchActive && ftsLastPageFull && string.Equals(text, lastFtsQuery, StringComparison.OrdinalIgnoreCase))
			{
				ApplyFtsSearchPage(append: true);
			}
			else
			{
				ApplyFtsSearch(text);
			}
		}
	}

	private void OnSearchTextChanged(object sender, EventArgs e)
	{
		string text = ((txtSearch != null) ? txtSearch.Text.Trim() : "");
		if (text.StartsWith("fts:", StringComparison.OrdinalIgnoreCase) || text.StartsWith("gloda:", StringComparison.OrdinalIgnoreCase))
		{
			int startIndex = (text.StartsWith("gloda:", StringComparison.OrdinalIgnoreCase) ? 6 : 4);
			ApplyFtsSearch(text.Substring(startIndex).Trim());
			return;
		}
		if (text.StartsWith("global:", StringComparison.OrdinalIgnoreCase))
		{
			StartGlobalSearch(text.Substring(7).Trim());
			return;
		}
		if (text.StartsWith("index:", StringComparison.OrdinalIgnoreCase))
		{
			ApplyLocalIndexSearch(text.Substring(6).Trim());
			return;
		}
		useImapSearch = text.StartsWith("imap:", StringComparison.OrdinalIgnoreCase);
		if (useImapSearch && text.Length > 5 && activeAccount != null && !string.IsNullOrWhiteSpace(selectedFolderFullName))
		{
			StartImapFolderSearch(text.Substring(5).Trim());
		}
		else if (currentFolderHeaders != null)
		{
			int shownCount = ApplyMessageListFilter();
			UpdateMessageCountStatus(shownCount);
		}
	}

	private void ApplyFtsSearch(string query)
	{
		if (activeAccount != null && !string.IsNullOrWhiteSpace(query))
		{
			lastFtsQuery = query.Trim();
			ftsResultSkip = 0;
			ApplyFtsSearchPage(append: false);
		}
	}

	private void ApplyFtsSearchPage(bool append)
	{
		checked
		{
			if (activeAccount != null && !string.IsNullOrWhiteSpace(lastFtsQuery))
			{
				if (!append)
				{
					ftsResultSkip = 0;
				}
				List<MailMessageHeader> list = MailSearchIndex.Search(activeAccount.Id, lastFtsQuery, 5000, ftsResultSkip);
				if (list != null && list.Count > 1 && conversationViewEnabled)
				{
					list = MailSearchGlodaFts.ApplyThreadAwareRanking(list);
				}
				ftsLastPageFull = ((list != null && list.Count >= 5000) ? true : false);
				crossFolderSearchActive = true;
				if (append && currentFolderHeaders != null && list != null)
				{
					List<MailMessageHeader> list2 = new List<MailMessageHeader>(currentFolderHeaders);
					list2.AddRange(list);
					currentFolderHeaders = list2;
				}
				else
				{
					currentFolderHeaders = list;
				}
				ClearMessageSelectionKeepButtons();
				selectedHeader = null;
				selectedBody = null;
				ClearReadingPane();
				int shownCount = ApplyMessageListFilter();
				UpdateMessageCountStatus(shownCount);
				int num = MailSearchIndex.CountEntries(activeAccount.Id);
				int num2 = MailSearchIndex.CountDistinctIndexedFolders(activeAccount.Id);
				int num3 = MailSearchGlodaFts.CountIndexedMessages(activeAccount.Id);
				int num4 = MailSearchGlodaFts.CountMessagesWithBody(activeAccount.Id);
				int num5 = ((currentFolderHeaders != null) ? currentFolderHeaders.Count : 0);
				string text = (ftsLastPageFull ? " — TAP SEARCH FOR MORE" : "");
				string text2 = ((lastFtsQuery.Length > 48) ? (lastFtsQuery.Substring(0, 45) + "…") : lastFtsQuery);
				string text3 = "";
				if (list != null && list.Count > 0 && list[0].FtsRank != 0.0)
				{
					text3 = " · TOP RANK " + list[0].FtsRank.ToString("0.0", CultureInfo.InvariantCulture);
				}
				string text4 = "";
				if (!string.IsNullOrWhiteSpace(MailSearchBackgroundIndexer.LastRunSummary))
				{
					text4 = " · " + MailSearchBackgroundIndexer.LastRunSummary.ToUpperInvariant();
				}
				string text5 = "";
				int num6 = MailSearchIndex.CountEntriesNeedingBodyPreview(activeAccount.Id);
				if (num6 > 0)
				{
					text5 = " · BACKFILL " + num6;
				}
				if (MailSearchBackgroundIndexer.LastBackfillRemaining > 0)
				{
					text5 = text5 + " · IDXQ " + MailSearchBackgroundIndexer.LastBackfillRemaining;
				}
				if (MailSearchBackgroundIndexer.LastBodiesIndexedThisRun > 0)
				{
					text5 = text5 + " · RUN+" + MailSearchBackgroundIndexer.LastBodiesIndexedThisRun;
				}
				string text6 = "";
				if (num > 0 && num3 > 0)
				{
					text6 = " · FTS-COV " + (int)Math.Round(Math.Min(100.0, Math.Round(100.0 * (double)num3 / (double)num))) + "%";
				}
				string text7 = "";
				if (num > 0 && num4 > 0)
				{
					text7 = " · BODY-COV " + (int)Math.Round(Math.Min(100.0, Math.Round(100.0 * (double)num4 / (double)num))) + "%";
				}
				string text8 = "";
				MailSearchGlodaFts.GlodaCorpusStats glodaCorpusStats = MailSearchGlodaFts.TryGetCorpusStats(activeAccount.Id);
				if (glodaCorpusStats != null && glodaCorpusStats.TotalFtsRows > 0)
				{
					text8 = " · CORPUS " + glodaCorpusStats.TotalFtsRows + "/" + glodaCorpusStats.WithBodyRows;
				}
				SetStatus("GLODA: \"" + text2 + "\" — " + num5 + " SHOWN — " + num2 + " FLD · IDX " + num + " · FTS " + num3 + " · BODY " + num4 + text6 + text7 + text8 + text5 + text3 + " · THREAD-RANK" + text4 + text);
				if (list != null)
				{
					ftsResultSkip += list.Count;
				}
			}
		}
	}

	private void OnSaveGlodaSearchClick(object sender, EventArgs e)
	{
		string text = ((txtSearch != null) ? txtSearch.Text.Trim() : "");
		if (text.Length == 0)
		{
			SetStatus("SAVE SEARCH: ENTER QUERY FIRST");
			return;
		}
		string text2 = ((selectedFolderFullName != null) ? selectedFolderFullName : "").Trim();
		if (MailGlodaSavedSearchStore.TrySaveSearch("", text, text2))
		{
			SetStatus("GLODA SEARCH SAVED" + ((text2.Length > 0) ? (" — FLD " + text2) : ""));
		}
		else
		{
			SetStatus("GLODA SEARCH SAVE FAILED");
		}
	}

	private void OnRecallGlodaSearchClick(object sender, EventArgs e)
	{
		List<KeyValuePair<string, string>> list = MailGlodaSavedSearchStore.TryListSaved();
		if (list == null || list.Count == 0)
		{
			string text = MailGlodaSavedSearchStore.TryGetMostRecentQuery();
			if (text.Length == 0)
			{
				SetStatus("RECALL: NO SAVED SEARCHES");
				return;
			}
			txtSearch.Text = text;
			OnSearchButtonClick(fbSearch, EventArgs.Empty);
			return;
		}
		using frmGlodaSavedSearchPicker frmGlodaSavedSearchPicker2 = new frmGlodaSavedSearchPicker();
		if (frmGlodaSavedSearchPicker2.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(frmGlodaSavedSearchPicker2.SelectedQuery))
		{
			txtSearch.Text = frmGlodaSavedSearchPicker2.SelectedQuery;
			OnSearchButtonClick(fbSearch, EventArgs.Empty);
			SetStatus("RECALL: PICKED SEARCH");
		}
	}

	private void ApplyLocalIndexSearch(string query)
	{
		if (activeAccount != null && !string.IsNullOrWhiteSpace(query))
		{
			List<MailMessageHeader> list = (currentFolderHeaders = MailSearchIndex.Search(activeAccount.Id, query, 10000));
			ClearMessageSelectionKeepButtons();
			selectedHeader = null;
			selectedBody = null;
			ClearReadingPane();
			int shownCount = ApplyMessageListFilter();
			UpdateMessageCountStatus(shownCount);
			SetStatus("INDEX: " + list.Count + " HIT(S)");
		}
	}

	private void StartGlobalSearch(string query)
	{
		if (!globalSearchWorker.IsBusy && activeAccount != null && !string.IsNullOrWhiteSpace(query))
		{
			string text = ResolvePassword(activeAccount, promptIfMissing: false);
			if (text == null && !EwsMailBridge.IsEwsAccount(activeAccount))
			{
				SetStatus("PASSWORD REQUIRED");
				return;
			}
			SetStatus("GLOBAL SEARCH…");
			globalSearchWorker.RunWorkerAsync(new GlobalSearchArgs
			{
				Account = activeAccount,
				Password = text,
				Query = query,
				ImapFolderOnly = false
			});
		}
	}

	private void StartImapFolderSearch(string query)
	{
		if (!globalSearchWorker.IsBusy && activeAccount != null && !string.IsNullOrWhiteSpace(selectedFolderFullName))
		{
			string text = ResolvePassword(activeAccount, promptIfMissing: false);
			if (text != null)
			{
				SetStatus("IMAP SEARCH…");
				globalSearchWorker.RunWorkerAsync(new GlobalSearchArgs
				{
					Account = activeAccount,
					Password = text,
					Query = query,
					ImapFolderOnly = true,
					Folder = selectedFolderFullName
				});
			}
		}
	}

	private void GlobalSearchWorker_DoWork(object sender, DoWorkEventArgs e)
	{
		GlobalSearchArgs globalSearchArgs = (GlobalSearchArgs)e.Argument;
		GlobalSearchResult globalSearchResult = new GlobalSearchResult();
		try
		{
			if (globalSearchArgs.ImapFolderOnly && !string.IsNullOrWhiteSpace(globalSearchArgs.Folder))
			{
				globalSearchResult.Headers = mailService.SearchFolderHeaders(globalSearchArgs.Account, globalSearchArgs.Password, globalSearchArgs.Folder, globalSearchArgs.Query);
			}
			else
			{
				globalSearchResult.Headers = MailGlobalSearch.SearchAccount(globalSearchArgs.Account, globalSearchArgs.Password, mailService, globalSearchArgs.Query);
			}
		}
		catch (Exception ex)
		{
			ProjectData.SetProjectError(ex);
			Exception ex2 = ex;
			globalSearchResult.ErrorText = ex2.Message;
			ProjectData.ClearProjectError();
		}
		e.Result = globalSearchResult;
	}

	private void GlobalSearchWorker_Completed(object sender, RunWorkerCompletedEventArgs e)
	{
		if (e.Error != null)
		{
			SetStatus("SEARCH ERROR: " + e.Error.Message);
		}
		else if (e.Result is GlobalSearchResult globalSearchResult)
		{
			if (!string.IsNullOrEmpty(globalSearchResult.ErrorText))
			{
				SetStatus("SEARCH ERROR: " + globalSearchResult.ErrorText);
				return;
			}
			currentFolderHeaders = globalSearchResult.Headers;
			int shownCount = ApplyMessageListFilter();
			UpdateMessageCountStatus(shownCount);
			SetStatus("SEARCH: " + shownCount + " RESULT(S)");
		}
	}

	private int ApplyMessageListFilter()
	{
		if (currentFolderHeaders == null)
		{
			PopulateMessageList(null);
			return 0;
		}
		string text = "";
		if (txtSearch != null)
		{
			text = txtSearch.Text.Trim();
		}
		if (text.Length == 0)
		{
			crossFolderSearchActive = false;
			PopulateMessageList(currentFolderHeaders);
			return currentFolderHeaders.Count;
		}
		string text2 = text;
		if (text2.StartsWith("imap:", StringComparison.OrdinalIgnoreCase) && text2.Length > 5)
		{
			text2 = text2.Substring(5).Trim();
		}
		List<MailMessageHeader> list = new List<MailMessageHeader>();
		foreach (MailMessageHeader currentFolderHeader in currentFolderHeaders)
		{
			if (HeaderMatchesSearch(currentFolderHeader, text2))
			{
				list.Add(currentFolderHeader);
			}
		}
		if (list.Count == 0 && activeAccount != null && text2.Length > 0)
		{
			foreach (MailMessageHeader item in MailSearchIndex.Search(activeAccount.Id, text2, 200))
			{
				if (item != null && (string.IsNullOrWhiteSpace(selectedFolderFullName) || string.Equals(item.Folder, selectedFolderFullName, StringComparison.OrdinalIgnoreCase)))
				{
					list.Add(item);
				}
			}
		}
		PopulateMessageList(list);
		return list.Count;
	}

	private static bool HeaderMatchesSearch(MailMessageHeader hdr, string query)
	{
		if (hdr == null || string.IsNullOrEmpty(query))
		{
			return false;
		}
		MailMessageHeader mailMessageHeader = hdr;
		string text = ((mailMessageHeader.From != null) ? mailMessageHeader.From : "");
		mailMessageHeader = hdr;
		string text2 = ((mailMessageHeader.Subject != null) ? mailMessageHeader.Subject : "");
		return (text.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 || text2.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ? true : false;
	}

	private void UpdateMessageCountStatus(int shownCount)
	{
		int num = ((currentFolderHeaders != null) ? currentFolderHeaders.Count : 0);
		string text = ((txtSearch != null) ? txtSearch.Text.Trim() : "");
		if (text.Length > 0 && num > 0)
		{
			SetStatus(shownCount + " MATCH(ES) — " + num + " IN FOLDER");
		}
		else
		{
			SetStatus(shownCount + " MESSAGE(S)");
		}
	}

	private void PopulateMessageList(List<MailMessageHeader> headers)
	{
		ClearMessageSelection();
		messageListOrder = new List<MailMessageHeader>();
		messageButtonsByUid.Clear();
		Panel contentPanel = messageHost.ContentPanel;
		foreach (Control control in contentPanel.Controls)
		{
			if (control != lblMessageEmpty)
			{
				contentPanel.Controls.Remove(control);
				control.Dispose();
			}
		}
		if (headers == null || headers.Count == 0)
		{
			lblMessageEmpty.Visible = true;
			lblMessageEmpty.Text = "NO MESSAGES";
			messageHost.Relayout();
			return;
		}
		lblMessageEmpty.Visible = false;
		int num = 4;
		checked
		{
			int btnW = Math.Max(100, contentPanel.Width - 8);
			bool flag = false;
			if (!conversationViewEnabled)
			{
				foreach (MailMessageHeader header in headers)
				{
					if (header != null)
					{
						messageListOrder.Add(header);
						AddMessageRowButton(contentPanel, header, "", 1, btnW, num, flag);
						flag = !flag;
						num += 48;
					}
				}
			}
			else
			{
				List<MailThreadListItem> list = MailConversationGrouper.BuildThreadList(headers);
				foreach (MailThreadListItem item in list)
				{
					if (item == null || item.Representative == null)
					{
						continue;
					}
					MailThreadListItem mailThreadListItem = item;
					string text = ((mailThreadListItem.ThreadKey != null) ? mailThreadListItem.ThreadKey : "");
					if (expandedThreadKeys.Contains(text) && item.Messages != null && item.Messages.Count > 1)
					{
						foreach (MailMessageHeader message in item.Messages)
						{
							if (message != null)
							{
								messageListOrder.Add(message);
								AddMessageRowButton(contentPanel, message, text, 1, btnW, num, flag);
								flag = !flag;
								num += 48;
							}
						}
					}
					else
					{
						MailMessageHeader representative = item.Representative;
						messageListOrder.Add(representative);
						AddMessageRowButton(contentPanel, representative, text, item.MessageCount, btnW, num, flag);
						flag = !flag;
						num += 48;
					}
				}
			}
			messageHost.Relayout();
		}
	}

	private void AddMessageRowButton(Panel content, MailMessageHeader hdr, string threadKey, int threadCount, int btnW, int y, bool flip)
	{
		MailMessageHeader mailMessageHeader = hdr;
		string text = ((mailMessageHeader.Subject != null) ? mailMessageHeader.Subject : "(NO SUBJECT)");
		mailMessageHeader = hdr;
		string text2 = ((mailMessageHeader.From != null) ? mailMessageHeader.From : "");
		string text3 = text;
		MailAccount mailAccount = FindAccountById(hdr.AccountId);
		if (mailAccount == null)
		{
			mailAccount = activeAccount;
		}
		if (unifiedInboxActive && mailAccount != null)
		{
			MailAccount mailAccount2 = mailAccount;
			string text4 = ((mailAccount2.EmailAddress != null) ? mailAccount2.EmailAddress : mailAccount.Username);
			if (string.IsNullOrWhiteSpace(text4))
			{
				text4 = mailAccount.Id;
			}
			if (!string.IsNullOrWhiteSpace(text4))
			{
				text3 = "[" + text4 + "] " + text3;
			}
		}
		else if (crossFolderSearchActive && !string.IsNullOrWhiteSpace(hdr.Folder))
		{
			text3 = "[" + hdr.Folder + "] " + text3;
		}
		if (text2.Length > 0)
		{
			text3 = text3 + " — " + text2;
		}
		if (crossFolderSearchActive && mailAccount != null)
		{
			if (hdr.FtsRank != 0.0)
			{
				text3 = "R" + hdr.FtsRank.ToString("0.0", CultureInfo.InvariantCulture) + " " + text3;
			}
			string text5 = MailSearchGlodaFts.TryGetBodySnippetForMessage(mailAccount.Id, hdr.Folder, hdr.Uid);
			if (text5.Length == 0)
			{
				text5 = MailSearchIndex.TryGetBodyPreviewSnippet(mailAccount.Id, hdr.Folder, hdr.Uid, 72);
			}
			if (text5.Length > 0)
			{
				text3 = text3 + " · " + text5;
			}
		}
		if (threadCount > 1)
		{
			string text6 = (expandedThreadKeys.Contains(threadKey) ? "▾ " : "▸ ");
			text3 = text6 + text3 + " (" + threadCount + ")";
		}
		if (text3.Length > 120)
		{
			text3 = text3.Substring(0, 117) + "…";
		}
		FlatButton flatButton = MakeRowButton(text3.ToUpperInvariant(), flip, hdr.IsSeen);
		flatButton.Tag = new MessageRowTag
		{
			Header = hdr,
			Account = mailAccount,
			ThreadKey = threadKey,
			ThreadCount = threadCount
		};
		Point location = new Point(2, y);
		flatButton.Location = location;
		Size size = new Size(btnW, 44);
		flatButton.Size = size;
		flatButton.ButtonTextHeight = 22;
		flatButton.Click += OnMessageButtonClick;
		flatButton.DoubleClick += OnMessageButtonDoubleClick;
		content.Controls.Add(flatButton);
		messageButtonsByUid[hdr.Uid] = flatButton;
	}

	private FlatButton MakeRowButton(string caption, bool flip, bool seen)
	{
		FlatButton flatButton = new FlatButton();
		flatButton.ButtonText = caption;
		flatButton.Text = caption;
		flatButton.Clickable = true;
		flatButton.Beeping = true;
		flatButton.ButtonTextAlign = ContentAlignment.MiddleLeft;
		flatButton.ButtonTextHeight = 14;
		if (!seen)
		{
			flatButton.Color = LCARScolorStyles.PrimaryFunction;
		}
		else if (flip)
		{
			flatButton.Color = LCARScolorStyles.SystemFunction;
		}
		else
		{
			flatButton.Color = LCARScolorStyles.LCARSDisplayOnly;
		}
		return flatButton;
	}

	private void OnMessageButtonClick(object sender, EventArgs e)
	{
		if (!(sender is FlatButton flatButton))
		{
			return;
		}
		MessageRowTag messageRowTag = flatButton.Tag as MessageRowTag;
		MailMessageHeader mailMessageHeader = ((messageRowTag != null) ? messageRowTag.Header : (flatButton.Tag as MailMessageHeader));
		if (mailMessageHeader == null)
		{
			return;
		}
		if (messageRowTag != null && messageRowTag.Account != null)
		{
			activeAccount = messageRowTag.Account;
		}
		else if (!string.IsNullOrWhiteSpace(mailMessageHeader.AccountId))
		{
			MailAccount mailAccount = FindAccountById(mailMessageHeader.AccountId);
			if (mailAccount != null)
			{
				activeAccount = mailAccount;
			}
		}
		if (conversationViewEnabled && messageRowTag != null && messageRowTag.ThreadCount > 1 && (Control.ModifierKeys & Keys.Alt) == Keys.Alt && !string.IsNullOrEmpty(messageRowTag.ThreadKey))
		{
			if (expandedThreadKeys.Contains(messageRowTag.ThreadKey))
			{
				expandedThreadKeys.Remove(messageRowTag.ThreadKey);
			}
			else
			{
				expandedThreadKeys.Add(messageRowTag.ThreadKey);
			}
			ApplyMessageListFilter();
			return;
		}
		int num = messageListOrder.FindIndex([SpecialName] (MailMessageHeader h) => (h != null && h.Uid == mailMessageHeader.Uid) ? true : false);
		bool flag = (Control.ModifierKeys & Keys.Control) == Keys.Control;
		if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift && lastMessageListIndex >= 0 && num >= 0)
		{
			int num2 = Math.Min(lastMessageListIndex, num);
			int num3 = Math.Max(lastMessageListIndex, num);
			ClearMessageSelectionKeepButtons();
			int num4 = num3;
			for (int num5 = num2; num5 <= num4; num5 = checked(num5 + 1))
			{
				MailMessageHeader mailMessageHeader2 = messageListOrder[num5];
				if (mailMessageHeader2 != null)
				{
					selectedMessages[mailMessageHeader2.Uid] = mailMessageHeader2;
				}
			}
		}
		else if (flag)
		{
			if (selectedMessages.ContainsKey(mailMessageHeader.Uid))
			{
				selectedMessages.Remove(mailMessageHeader.Uid);
			}
			else
			{
				selectedMessages[mailMessageHeader.Uid] = mailMessageHeader;
			}
		}
		else
		{
			ClearMessageSelectionKeepButtons();
			selectedMessages[mailMessageHeader.Uid] = mailMessageHeader;
		}
		if (num >= 0)
		{
			lastMessageListIndex = num;
		}
		ApplyMessageSelectionColors();
		selectedMessageButton = flatButton;
		selectedHeader = mailMessageHeader;
		UpdateComposeButtonsEnabled();
		StartFetchBody(mailMessageHeader);
	}

	private void OnMessageButtonDoubleClick(object sender, EventArgs e)
	{
		if (!(sender is FlatButton flatButton))
		{
			return;
		}
		MessageRowTag messageRowTag = flatButton.Tag as MessageRowTag;
		MailMessageHeader mailMessageHeader = ((messageRowTag != null) ? messageRowTag.Header : (flatButton.Tag as MailMessageHeader));
		if (mailMessageHeader != null && IsDraftsFolder(selectedFolderFullName))
		{
			OpenImapDraft(mailMessageHeader);
		}
		else if (conversationViewEnabled && messageRowTag != null && messageRowTag.ThreadCount > 1 && !string.IsNullOrEmpty(messageRowTag.ThreadKey))
		{
			if (expandedThreadKeys.Contains(messageRowTag.ThreadKey))
			{
				expandedThreadKeys.Remove(messageRowTag.ThreadKey);
			}
			else
			{
				expandedThreadKeys.Add(messageRowTag.ThreadKey);
			}
			ApplyMessageListFilter();
		}
	}

	private void ApplyMessageSelectionColors()
	{
		foreach (KeyValuePair<ulong, FlatButton> item in messageButtonsByUid)
		{
			FlatButton value = item.Value;
			MailMessageHeader mailMessageHeader = ((value.Tag is MessageRowTag messageRowTag) ? messageRowTag.Header : (value.Tag as MailMessageHeader));
			if (mailMessageHeader != null)
			{
				if (selectedMessages.ContainsKey(mailMessageHeader.Uid))
				{
					value.Color = LCARScolorStyles.Orange;
					continue;
				}
				bool flip = decimal.Compare(decimal.Remainder(new decimal(mailMessageHeader.Uid), 2m), 0m) == 0;
				value.Color = MessageButtonColor(mailMessageHeader, flip);
			}
		}
	}

	private void ClearMessageSelectionKeepButtons()
	{
		selectedMessages.Clear();
		selectedMessageButton = null;
		selectedHeader = null;
	}

	private void OnThreadsToggleClick(object sender, EventArgs e)
	{
		conversationViewEnabled = !conversationViewEnabled;
		if (fbThreads != null)
		{
			fbThreads.Color = (conversationViewEnabled ? LCARScolorStyles.PrimaryFunction : LCARScolorStyles.StaticTan);
		}
		if (currentFolderHeaders != null)
		{
			ApplyMessageListFilter();
		}
		if (conversationViewEnabled)
		{
			SetStatus("THREADS ON — ALT+CLICK ROW TO EXPAND");
		}
		else
		{
			SetStatus("THREADS OFF");
		}
	}

	private List<ulong> SelectedUids()
	{
		List<ulong> list = new List<ulong>();
		if (selectedMessages.Count <= 0)
		{
			if (selectedHeader != null)
			{
				list.Add(selectedHeader.Uid);
			}
			return list;
		}
		foreach (KeyValuePair<ulong, MailMessageHeader> selectedMessage in selectedMessages)
		{
			list.Add(selectedMessage.Key);
		}
		return list;
	}

	private static LCARScolorStyles MessageButtonColor(MailMessageHeader hdr, bool flip)
	{
		if (hdr != null && !hdr.IsSeen)
		{
			return LCARScolorStyles.PrimaryFunction;
		}
		if (flip)
		{
			return LCARScolorStyles.SystemFunction;
		}
		return LCARScolorStyles.LCARSDisplayOnly;
	}

	private void StartFetchBody(MailMessageHeader hdr)
	{
		if (bodyWorker.IsBusy || activeAccount == null || hdr == null)
		{
			return;
		}
		string text = ((hdr.Folder != null) ? hdr.Folder : "");
		if (string.IsNullOrWhiteSpace(text) || string.Equals(text, "__UNIFIED_INBOX__", StringComparison.Ordinal))
		{
			text = ((selectedFolderFullName != null) ? selectedFolderFullName : "");
		}
		if (string.IsNullOrWhiteSpace(text) || string.Equals(text, "__UNIFIED_INBOX__", StringComparison.Ordinal))
		{
			text = "INBOX";
		}
		MailMessageBody body = null;
		if (MailCache.TryLoadBody(activeAccount.Id, text, hdr.Uid, ref body))
		{
			ShowMessageBody(hdr, body);
		}
		else
		{
			lblReadEmpty.Visible = true;
			lblReadEmpty.Text = "LOADING…";
			lblReadMeta.Visible = false;
			lblReadSubject.Visible = false;
			bodyLabel.Visible = false;
			if (htmlReadPane != null)
			{
				htmlReadPane.Clear();
			}
			SetAttachmentPaneVisible(visible: false);
			readHost.Relayout();
		}
		string text2 = ResolvePassword(activeAccount, promptIfMissing: false);
		if (text2 == null)
		{
			SetStatus("PASSWORD REQUIRED");
			return;
		}
		SetBusy(busy: true);
		SetStatus("LOADING MESSAGE…");
		bodyWorker.RunWorkerAsync(new BodyWorkerArgs
		{
			Account = activeAccount,
			Password = text2,
			Folder = text,
			Uid = hdr.Uid,
			Header = hdr
		});
	}

	private void BodyWorker_DoWork(object sender, DoWorkEventArgs e)
	{
		BodyWorkerArgs bodyWorkerArgs = (BodyWorkerArgs)e.Argument;
		BodyWorkerResult bodyWorkerResult = new BodyWorkerResult();
		bodyWorkerResult.Header = bodyWorkerArgs.Header;
		BodyWorkerResult bodyWorkerResult2 = bodyWorkerResult;
		try
		{
			bodyWorkerResult2.Body = mailService.FetchBody(bodyWorkerArgs.Account, bodyWorkerArgs.Password, bodyWorkerArgs.Folder, bodyWorkerArgs.Uid);
			mailService.MarkSeen(bodyWorkerArgs.Account, bodyWorkerArgs.Password, bodyWorkerArgs.Folder, bodyWorkerArgs.Uid);
			if (bodyWorkerResult2.Header != null)
			{
				bodyWorkerResult2.Header.IsSeen = true;
			}
		}
		catch (Exception ex)
		{
			ProjectData.SetProjectError(ex);
			Exception ex2 = ex;
			bodyWorkerResult2.ErrorText = ex2.Message;
			MailMessageBody body = null;
			if (MailCache.TryLoadBody(bodyWorkerArgs.Account.Id, bodyWorkerArgs.Folder, bodyWorkerArgs.Uid, ref body))
			{
				bodyWorkerResult2.Body = body;
			}
			ProjectData.ClearProjectError();
		}
		e.Result = bodyWorkerResult2;
	}

	private void BodyWorker_Completed(object sender, RunWorkerCompletedEventArgs e)
	{
		SetBusy(busy: false);
		if (e.Error != null)
		{
			SetStatus("LOAD ERROR: " + e.Error.Message);
		}
		else
		{
			if (!(e.Result is BodyWorkerResult { Header: not null } bodyWorkerResult))
			{
				return;
			}
			if (bodyWorkerResult.Body != null)
			{
				ShowMessageBody(bodyWorkerResult.Header, bodyWorkerResult.Body);
				if (activeAccount != null && bodyWorkerResult.Header != null)
				{
					string folder = ((selectedFolderFullName != null) ? selectedFolderFullName : bodyWorkerResult.Header.Folder);
					MailSearchIndex.UpsertBodyPreview(activeAccount.Id, folder, bodyWorkerResult.Header.Uid, bodyWorkerResult.Body.GetReadingPaneText());
				}
			}
			if (selectedMessageButton != null && object.ReferenceEquals(RuntimeHelpers.GetObjectValue(selectedMessageButton.Tag), bodyWorkerResult.Header))
			{
				selectedMessageButton.Color = LCARScolorStyles.Orange;
			}
			else if (selectedMessageButton != null && selectedMessageButton.Tag is MailMessageHeader mailMessageHeader && mailMessageHeader.Uid == bodyWorkerResult.Header.Uid)
			{
				mailMessageHeader.IsSeen = true;
				selectedMessageButton.Color = LCARScolorStyles.Orange;
			}
			if (!string.IsNullOrEmpty(bodyWorkerResult.ErrorText))
			{
				SetStatus("LOAD WARNING: " + bodyWorkerResult.ErrorText);
			}
			else
			{
				SetStatus("MESSAGE LOADED");
			}
		}
	}

	private void ShowMessageBody(MailMessageHeader hdr, MailMessageBody body)
	{
		selectedBody = body;
		lblReadEmpty.Visible = false;
		UpdateThreadContext(hdr);
		MailMessageHeader mailMessageHeader = hdr;
		string raw = ((mailMessageHeader.From != null) ? mailMessageHeader.From : "");
		string text = "";
		if (DateTime.Compare(hdr.DateUtc, DateTime.MinValue) != 0)
		{
			text = hdr.DateUtc.ToLocalTime().ToString("g");
		}
		string text2 = (MailAddressHelper.FormatDisplayLine(raw) + "  ·  " + text).Trim();
		if (lstThreadContext != null && lstThreadContext.Visible && lstThreadContext.Items.Count > 1)
		{
			text2 = text2 + "  ·  " + lstThreadContext.Items.Count + " IN THREAD";
		}
		lblReadMeta.Text = text2.ToUpperInvariant();
		lblReadMeta.Visible = true;
		Label label = lblReadSubject;
		mailMessageHeader = hdr;
		label.Text = ((mailMessageHeader.Subject != null) ? mailMessageHeader.Subject : "(NO SUBJECT)").ToUpperInvariant();
		lblReadSubject.Visible = true;
		PopulateAttachmentList(hdr, body);
		TryPrefetchInlineAttachments(hdr, body);
		bodyLabel.Visible = false;
		if (htmlReadPane != null)
		{
			htmlReadPane.Visible = true;
			htmlReadPane.ShowBody(body, body?.Attachments);
		}
		else
		{
			string text3 = "";
			if (body != null)
			{
				text3 = body.GetReadingPaneText();
			}
			if (text3.Length == 0)
			{
				text3 = "(NO TEXT BODY)";
			}
			bodyLabel.Text = text3;
			bodyLabel.Visible = true;
		}
		LayoutReadingPaneControls();
		readHost.Relayout();
		UpdateAttachmentSaveEnabled();
		int num = MailCalendarInvitationBridge.TryImportFromMessageBody(body);
		if (num > 0)
		{
			string text4 = "";
			if (MailCalendarInvitationBridge.LastImportedWithRecurrence > 0)
			{
				text4 = text4 + " · RRULE " + MailCalendarInvitationBridge.LastImportedWithRecurrence;
			}
			if (MailCalendarInvitationBridge.LastImportedWithCategory > 0)
			{
				text4 = text4 + " · CAT " + MailCalendarInvitationBridge.LastImportedWithCategory;
			}
			if (MailCalendarInvitationBridge.LastImportedWithAlarm > 0)
			{
				text4 = text4 + " · ALARM " + MailCalendarInvitationBridge.LastImportedWithAlarm;
			}
			SetStatus("INVITE IMPORT +" + num + " (MIME CALENDAR)" + text4 + " — CALDAV SYNC QUEUED");
		}
	}

	private void PopulateAttachmentList(MailMessageHeader hdr, MailMessageBody body)
	{
		if (lstReadAttachments == null)
		{
			return;
		}
		lstReadAttachments.Items.Clear();
		if ((body == null || body.Attachments == null || body.Attachments.Count <= 0) && 0 == 0)
		{
			SetAttachmentPaneVisible(visible: false);
			return;
		}
		string text = ((selectedFolderFullName != null) ? selectedFolderFullName : ((hdr != null) ? hdr.Folder : ""));
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "INBOX";
		}
		string text2 = ((activeAccount != null) ? activeAccount.Id : "");
		foreach (MailAttachmentInfo attachment in body.Attachments)
		{
			if (attachment != null)
			{
				if (text2.Length > 0 && hdr != null)
				{
					attachment.CachedPath = MailCache.TryGetCachedAttachmentPath(text2, text, hdr.Uid, attachment.PartIndex, attachment.FileName);
				}
				lstReadAttachments.Items.Add(attachment);
			}
		}
		SetAttachmentPaneVisible(lstReadAttachments.Items.Count > 0);
		if (lstReadAttachments.Items.Count > 0)
		{
			lstReadAttachments.SelectedIndex = 0;
		}
	}

	private void SetAttachmentPaneVisible(bool visible)
	{
		if (lblReadAttachments != null)
		{
			lblReadAttachments.Visible = visible;
		}
		if (lstReadAttachments != null)
		{
			lstReadAttachments.Visible = visible;
		}
		if (fbSaveAttachment != null)
		{
			fbSaveAttachment.Visible = visible;
		}
	}

	private void LayoutReadingPaneControls()
	{
		int num = 4;
		Label label = lblReadMeta;
		Point location = new Point(4, num);
		label.Location = location;
		checked
		{
			num = lblReadMeta.Bottom + 6;
			Label label2 = lblReadSubject;
			location = new Point(4, num);
			label2.Location = location;
			num = lblReadSubject.Bottom + 8;
			int num2 = Math.Max(100, readHost.ContentPanel.Width - 12);
			if (lstThreadContext != null && lstThreadContext.Visible)
			{
				Label label3 = lblReadThread;
				location = new Point(4, num);
				label3.Location = location;
				num = lblReadThread.Bottom + 4;
				int num3 = Math.Min(5, Math.Max(1, lstThreadContext.Items.Count));
				int num4 = num3 * lstThreadContext.ItemHeight + 4;
				ListBox listBox = lstThreadContext;
				location = new Point(4, num);
				listBox.Location = location;
				ListBox listBox2 = lstThreadContext;
				Size size = new Size(num2, num4);
				listBox2.Size = size;
				num = lstThreadContext.Bottom + 8;
			}
			if (lstReadAttachments != null && lstReadAttachments.Visible)
			{
				Label label4 = lblReadAttachments;
				location = new Point(4, num);
				label4.Location = location;
				num = lblReadAttachments.Bottom + 4;
				int num5 = Math.Min(4, Math.Max(1, lstReadAttachments.Items.Count));
				int num6 = num5 * lstReadAttachments.ItemHeight + 4;
				ListBox listBox3 = lstReadAttachments;
				location = new Point(4, num);
				listBox3.Location = location;
				ListBox listBox4 = lstReadAttachments;
				Size size = new Size(num2, num6);
				listBox4.Size = size;
				num = lstReadAttachments.Bottom + 6;
				if (fbSaveAttachment != null)
				{
					FlatButton flatButton = fbSaveAttachment;
					location = new Point(4, num);
					flatButton.Location = location;
					num = fbSaveAttachment.Bottom + 10;
				}
			}
			Label label5 = bodyLabel;
			location = new Point(4, num);
			label5.Location = location;
			if (htmlReadPane != null && htmlReadPane.Visible)
			{
				int num7 = Math.Max(120, readHost.ContentPanel.Height - num - 8);
				MailHtmlReadPane mailHtmlReadPane = htmlReadPane;
				location = new Point(4, num);
				mailHtmlReadPane.Location = location;
				MailHtmlReadPane mailHtmlReadPane2 = htmlReadPane;
				Size size = new Size(num2, num7);
				mailHtmlReadPane2.Size = size;
			}
		}
	}

	private void UpdateAttachmentSaveEnabled()
	{
		if (fbSaveAttachment != null && lstReadAttachments != null)
		{
			bool flag = ((attachmentWorker != null && attachmentWorker.IsBusy) ? true : false);
			fbSaveAttachment.Clickable = ((lstReadAttachments.Visible && lstReadAttachments.SelectedIndex >= 0 && !flag) ? true : false);
		}
	}

	private void OnSaveAttachmentClick(object sender, EventArgs e)
	{
		if (selectedHeader == null || activeAccount == null || lstReadAttachments == null || lstReadAttachments.SelectedItem == null || attachmentWorker.IsBusy || !(lstReadAttachments.SelectedItem is MailAttachmentInfo mailAttachmentInfo))
		{
			return;
		}
		MailAttachmentInfo mailAttachmentInfo2 = mailAttachmentInfo;
		string text = ((mailAttachmentInfo2.FileName != null) ? mailAttachmentInfo2.FileName : "attachment").Trim();
		if (text.Length == 0)
		{
			text = "attachment";
		}
		string text2 = null;
		using (SaveFileDialog saveFileDialog = new SaveFileDialog())
		{
			saveFileDialog.Title = "SAVE ATTACHMENT";
			saveFileDialog.FileName = text;
			saveFileDialog.OverwritePrompt = true;
			if (saveFileDialog.ShowDialog(this) != DialogResult.OK)
			{
				return;
			}
			text2 = saveFileDialog.FileName;
		}
		string text3 = ((selectedFolderFullName != null) ? selectedFolderFullName : selectedHeader.Folder);
		if (string.IsNullOrWhiteSpace(text3))
		{
			text3 = "INBOX";
		}
		mailAttachmentInfo2 = mailAttachmentInfo;
		string text4 = ((mailAttachmentInfo2.CachedPath != null) ? mailAttachmentInfo2.CachedPath : "").Trim();
		if (text4.Length == 0)
		{
			text4 = MailCache.TryGetCachedAttachmentPath(activeAccount.Id, text3, selectedHeader.Uid, mailAttachmentInfo.PartIndex, mailAttachmentInfo.FileName);
		}
		if (!string.IsNullOrWhiteSpace(text4) && File.Exists(text4))
		{
			try
			{
				File.Copy(text4, text2, overwrite: true);
				SetStatus("ATTACHMENT SAVED");
				return;
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Exception ex2 = ex;
				SetStatus("SAVE ERROR: " + ex2.Message);
				ProjectData.ClearProjectError();
				return;
			}
		}
		string text5 = ResolvePassword(activeAccount, promptIfMissing: false);
		if (text5 == null)
		{
			SetStatus("PASSWORD REQUIRED");
			return;
		}
		SetBusy(busy: true);
		SetStatus("DOWNLOADING ATTACHMENT…");
		attachmentWorker.RunWorkerAsync(new AttachmentWorkerArgs
		{
			Account = activeAccount,
			Password = text5,
			Folder = text3,
			Uid = selectedHeader.Uid,
			PartIndex = mailAttachmentInfo.PartIndex,
			FileName = mailAttachmentInfo.FileName,
			DestPath = text2
		});
	}

	private void AttachmentWorker_DoWork(object sender, DoWorkEventArgs e)
	{
		AttachmentWorkerArgs attachmentWorkerArgs = (AttachmentWorkerArgs)e.Argument;
		AttachmentWorkerResult attachmentWorkerResult = new AttachmentWorkerResult();
		if (attachmentWorkerArgs == null)
		{
			attachmentWorkerResult.ErrorText = "Nothing to save.";
			e.Result = attachmentWorkerResult;
			return;
		}
		attachmentWorkerResult.DestPath = attachmentWorkerArgs.DestPath;
		attachmentWorkerResult.PartIndex = attachmentWorkerArgs.PartIndex;
		attachmentWorkerResult.FileName = attachmentWorkerArgs.FileName;
		attachmentWorkerResult.Folder = attachmentWorkerArgs.Folder;
		attachmentWorkerResult.Uid = attachmentWorkerArgs.Uid;
		if (attachmentWorkerArgs.Account != null)
		{
			MailAccount account = attachmentWorkerArgs.Account;
			attachmentWorkerResult.AccountId = ((account.Id != null) ? account.Id : "");
		}
		try
		{
			string text = mailService.SaveAttachmentToFile(attachmentWorkerArgs.Account, attachmentWorkerArgs.Password, attachmentWorkerArgs.Folder, attachmentWorkerArgs.Uid, attachmentWorkerArgs.PartIndex, attachmentWorkerArgs.DestPath);
			if (text != null && text.Length > 0)
			{
				attachmentWorkerResult.ErrorText = text;
			}
			else if (attachmentWorkerArgs.Account != null)
			{
				MailCache.SaveAttachmentCache(attachmentWorkerArgs.Account.Id, attachmentWorkerArgs.Folder, attachmentWorkerArgs.Uid, attachmentWorkerArgs.PartIndex, attachmentWorkerArgs.FileName, attachmentWorkerArgs.DestPath);
			}
		}
		catch (Exception ex)
		{
			ProjectData.SetProjectError(ex);
			Exception ex2 = ex;
			attachmentWorkerResult.ErrorText = ex2.Message;
			ProjectData.ClearProjectError();
		}
		e.Result = attachmentWorkerResult;
	}

	private void AttachmentWorker_Completed(object sender, RunWorkerCompletedEventArgs e)
	{
		SetBusy(busy: false);
		UpdateAttachmentSaveEnabled();
		if (e.Error != null)
		{
			SetStatus("SAVE ERROR: " + e.Error.Message);
			return;
		}
		AttachmentWorkerResult attachmentWorkerResult = e.Result as AttachmentWorkerResult;
		if (attachmentWorkerResult != null && !string.IsNullOrEmpty(attachmentWorkerResult.ErrorText))
		{
			SetStatus("SAVE ERROR: " + attachmentWorkerResult.ErrorText);
			return;
		}
		if (attachmentWorkerResult != null && selectedBody != null && selectedBody.Attachments != null)
		{
			string cachedPath = MailCache.TryGetCachedAttachmentPath(attachmentWorkerResult.AccountId, attachmentWorkerResult.Folder, attachmentWorkerResult.Uid, attachmentWorkerResult.PartIndex, attachmentWorkerResult.FileName);
			foreach (MailAttachmentInfo attachment in selectedBody.Attachments)
			{
				if (attachment != null && attachment.PartIndex == attachmentWorkerResult.PartIndex)
				{
					attachment.CachedPath = cachedPath;
					break;
				}
			}
		}
		SetStatus("ATTACHMENT SAVED");
	}

	private void ClearReadingPane()
	{
		selectedBody = null;
		lblReadMeta.Visible = false;
		lblReadSubject.Visible = false;
		bodyLabel.Visible = false;
		if (htmlReadPane != null)
		{
			htmlReadPane.Clear();
			htmlReadPane.Visible = false;
		}
		SetAttachmentPaneVisible(visible: false);
		if (lstReadAttachments != null)
		{
			lstReadAttachments.Items.Clear();
		}
		lblReadEmpty.Visible = true;
		lblReadEmpty.Text = "SELECT A MESSAGE";
		readHost.Relayout();
		UpdateComposeButtonsEnabled();
	}

	private void ClearMessageList()
	{
		currentFolderHeaders = null;
		ClearMessageSelection();
		Panel contentPanel = messageHost.ContentPanel;
		foreach (Control control in contentPanel.Controls)
		{
			if (control != lblMessageEmpty)
			{
				contentPanel.Controls.Remove(control);
				control.Dispose();
			}
		}
		lblMessageEmpty.Visible = true;
		lblMessageEmpty.Text = "SELECT A FOLDER";
		messageHost.Relayout();
	}

	private void ClearFolderSelection()
	{
		selectedFolderButton = null;
	}

	private void ClearMessageSelection()
	{
		selectedMessageButton = null;
		selectedHeader = null;
		selectedMessages.Clear();
		UpdateComposeButtonsEnabled();
	}

	private void OnNewClick(object sender, EventArgs e)
	{
		OpenCompose("", "", "", "");
	}

	private void OnReplyClick(object sender, EventArgs e)
	{
		if (selectedHeader != null)
		{
			MailMessageHeader mailMessageHeader = selectedHeader;
			string toAddrs = ((mailMessageHeader.From != null) ? mailMessageHeader.From : "").Trim();
			string subject = SubjectWithPrefix("Re:", selectedHeader.Subject);
			string body = BuildQuotedReplyBody(selectedHeader);
			OpenCompose(toAddrs, "", subject, body);
		}
	}

	private void OnReplyAllClick(object sender, EventArgs e)
	{
		if (selectedHeader != null)
		{
			string toOut = "";
			string ccOut = "";
			BuildReplyAllAddresses(selectedHeader, activeAccount, ref toOut, ref ccOut);
			string subject = SubjectWithPrefix("Re:", selectedHeader.Subject);
			string body = BuildQuotedReplyBody(selectedHeader);
			OpenCompose(toOut, ccOut, subject, body);
		}
	}

	private void OnForwardClick(object sender, EventArgs e)
	{
		if (selectedHeader != null)
		{
			string subject = SubjectWithPrefix("Fwd:", selectedHeader.Subject);
			string body = BuildQuotedForwardBody(selectedHeader);
			OpenCompose("", "", subject, body);
		}
	}

	private string MutationSourceFolder()
	{
		if (selectedHeader != null && !string.IsNullOrWhiteSpace(selectedHeader.Folder) && !string.Equals(selectedHeader.Folder, "__UNIFIED_INBOX__", StringComparison.Ordinal))
		{
			return selectedHeader.Folder;
		}
		if (!string.IsNullOrWhiteSpace(selectedFolderFullName) && !string.Equals(selectedFolderFullName, "__UNIFIED_INBOX__", StringComparison.Ordinal))
		{
			return selectedFolderFullName;
		}
		return "INBOX";
	}

	private void OnDeleteClick(object sender, EventArgs e)
	{
		if ((selectedHeader == null && selectedMessages.Count == 0) || activeAccount == null)
		{
			return;
		}
		string text = MutationSourceFolder();
		if (string.IsNullOrWhiteSpace(text) || mutationWorker.IsBusy)
		{
			return;
		}
		string text2 = ResolvePassword(activeAccount, promptIfMissing: false);
		if (text2 == null)
		{
			SetStatus("PASSWORD REQUIRED");
			return;
		}
		List<ulong> list = SelectedUids();
		if (list.Count != 0)
		{
			StartMutation("delete", text, null, list, text2);
		}
	}

	private void OnMoveClick(object sender, EventArgs e)
	{
		if ((selectedHeader == null && selectedMessages.Count == 0) || activeAccount == null)
		{
			return;
		}
		string text = MutationSourceFolder();
		if (string.IsNullOrWhiteSpace(text) || mutationWorker.IsBusy)
		{
			return;
		}
		string text2 = ShowMoveFolderPicker(text);
		if (string.IsNullOrWhiteSpace(text2))
		{
			return;
		}
		string text3 = ResolvePassword(activeAccount, promptIfMissing: false);
		if (text3 == null)
		{
			SetStatus("PASSWORD REQUIRED");
			return;
		}
		List<ulong> list = SelectedUids();
		if (list.Count != 0)
		{
			StartMutation("move", text, text2, list, text3);
		}
	}

	private string ShowMoveFolderPicker(string excludeFullName)
	{
		List<MailFolderInfo> list = FoldersForActiveAccount();
		if (list == null || list.Count == 0)
		{
			SetStatus("NO FOLDERS — REFRESH");
			return null;
		}
		using (MailFolderPickerDialog mailFolderPickerDialog = new MailFolderPickerDialog(list, excludeFullName))
		{
			if (mailFolderPickerDialog.ShowDialog(this) == DialogResult.OK)
			{
				return mailFolderPickerDialog.SelectedFolderFullName;
			}
		}
		return null;
	}

	private List<MailFolderInfo> FoldersForActiveAccount()
	{
		if (activeAccount == null || lastFolderSections == null)
		{
			return null;
		}
		MailAccount mailAccount = activeAccount;
		string b = ((mailAccount.Id != null) ? mailAccount.Id : "");
		foreach (AccountFolderSection lastFolderSection in lastFolderSections)
		{
			if (lastFolderSection != null && lastFolderSection.Account != null)
			{
				mailAccount = lastFolderSection.Account;
				if (string.Equals((mailAccount.Id != null) ? mailAccount.Id : "", b, StringComparison.OrdinalIgnoreCase))
				{
					return lastFolderSection.Folders;
				}
			}
		}
		return null;
	}

	private void StartMutation(string kind, string folder, string destFolder, List<ulong> uids, string password)
	{
		if (!mutationWorker.IsBusy && activeAccount != null && uids != null && uids.Count != 0)
		{
			ClearReadingPane();
			ClearMessageSelection();
			SetBusy(busy: true);
			string text = uids.Count.ToString();
			if (string.Equals(kind, "move", StringComparison.OrdinalIgnoreCase))
			{
				SetStatus("MOVING " + text + " MESSAGE(S)…");
			}
			else
			{
				SetStatus("DELETING " + text + " MESSAGE(S)…");
			}
			mutationWorker.RunWorkerAsync(new MutationWorkerArgs
			{
				Kind = kind,
				Account = activeAccount,
				Password = password,
				Folder = folder,
				DestFolder = destFolder,
				Uids = uids
			});
		}
	}

	private void MutationWorker_DoWork(object sender, DoWorkEventArgs e)
	{
		MutationWorkerArgs mutationWorkerArgs = (MutationWorkerArgs)e.Argument;
		MutationWorkerResult mutationWorkerResult = new MutationWorkerResult();
		MutationWorkerResult mutationWorkerResult2 = mutationWorkerResult;
		MutationWorkerArgs mutationWorkerArgs2 = mutationWorkerArgs;
		mutationWorkerResult2.Kind = ((mutationWorkerArgs2.Kind != null) ? mutationWorkerArgs2.Kind : "");
		MutationWorkerResult mutationWorkerResult3 = mutationWorkerResult;
		if (mutationWorkerArgs == null || mutationWorkerArgs.Account == null || mutationWorkerArgs.Uids == null || mutationWorkerArgs.Uids.Count == 0)
		{
			mutationWorkerResult3.ErrorText = "Nothing to change.";
			e.Result = mutationWorkerResult3;
			return;
		}
		checked
		{
			try
			{
				if (string.Equals(mutationWorkerArgs.Kind, "move", StringComparison.OrdinalIgnoreCase))
				{
					mailService.MoveMessages(mutationWorkerArgs.Account, mutationWorkerArgs.Password, mutationWorkerArgs.Folder, mutationWorkerArgs.DestFolder, mutationWorkerArgs.Uids);
				}
				else
				{
					mailService.DeleteMessages(mutationWorkerArgs.Account, mutationWorkerArgs.Password, mutationWorkerArgs.Folder, mutationWorkerArgs.Uids);
					foreach (ulong uid in mutationWorkerArgs.Uids)
					{
						MailCache.RemoveMessageCache(mutationWorkerArgs.Account.Id, mutationWorkerArgs.Folder, uid);
						MailSearchIndex.RemoveMessage(mutationWorkerArgs.Account.Id, mutationWorkerArgs.Folder, uid);
						mutationWorkerResult = mutationWorkerResult3;
						mutationWorkerResult.CachePurgedCount++;
					}
				}
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Exception ex2 = ex;
				mutationWorkerResult3.ErrorText = ex2.Message;
				ProjectData.ClearProjectError();
			}
			e.Result = mutationWorkerResult3;
		}
	}

	private void MutationWorker_Completed(object sender, RunWorkerCompletedEventArgs e)
	{
		SetBusy(busy: false);
		if (e.Error != null)
		{
			SetStatus("MAIL ERROR: " + e.Error.Message);
			RefreshCurrentFolderHeaders();
			return;
		}
		MutationWorkerResult mutationWorkerResult = e.Result as MutationWorkerResult;
		if (mutationWorkerResult != null && !string.IsNullOrEmpty(mutationWorkerResult.ErrorText))
		{
			SetStatus("MAIL ERROR: " + mutationWorkerResult.ErrorText);
		}
		else if (mutationWorkerResult != null && string.Equals(mutationWorkerResult.Kind, "move", StringComparison.OrdinalIgnoreCase))
		{
			SetStatus("MESSAGE MOVED");
		}
		else if (mutationWorkerResult != null && mutationWorkerResult.CachePurgedCount > 0)
		{
			SetStatus("DELETED — CACHE EXPUNGED " + mutationWorkerResult.CachePurgedCount);
		}
		else
		{
			SetStatus("MESSAGE DELETED");
		}
		RefreshCurrentFolderHeaders();
	}

	private void RefreshCurrentFolderHeaders()
	{
		if (!string.IsNullOrWhiteSpace(selectedFolderFullName))
		{
			StartSyncHeaders(selectedFolderFullName);
		}
	}

	private void TrySelectDraftsFolder()
	{
		if (folderHost == null || activeAccount == null)
		{
			return;
		}
		MailAccount mailAccount = activeAccount;
		string b = ((mailAccount.Id != null) ? mailAccount.Id : "");
		foreach (Control control in folderHost.ContentPanel.Controls)
		{
			if (!(control is FlatButton { Tag: FolderRowTag { Folder: not null } tag } flatButton) || !string.Equals((tag.Account != null) ? tag.Account.Id : "", b, StringComparison.OrdinalIgnoreCase) || !IsDraftsFolder(tag.Folder.FullName))
			{
				continue;
			}
			OnFolderButtonClick(flatButton, EventArgs.Empty);
			SetStatus("DRAFTS FOLDER SELECTED");
			return;
		}
		SetStatus("DRAFTS FOLDER NOT FOUND — REFRESH FOLDERS");
	}

	private static bool IsDraftsFolder(string folderFullName)
	{
		string text = ((folderFullName != null) ? folderFullName : "").Trim().ToLowerInvariant();
		if (text.Length == 0)
		{
			return false;
		}
		return (Operators.CompareString(text, "drafts", TextCompare: false) == 0 || text.EndsWith("/drafts") || text.EndsWith(".drafts")) ? true : false;
	}

	private void OpenImapDraft(MailMessageHeader hdr)
	{
		if (activeAccount == null || hdr == null)
		{
			return;
		}
		string text = ResolvePassword(activeAccount, promptIfMissing: true);
		if (text == null)
		{
			SetStatus("PASSWORD REQUIRED");
			return;
		}
		MailMessageHeader mailMessageHeader = hdr;
		string text2 = ((mailMessageHeader.Folder != null) ? mailMessageHeader.Folder : selectedFolderFullName);
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = "Drafts";
		}
		SetBusy(busy: true);
		SetStatus("LOADING DRAFT…");
		try
		{
			MailMessageBody mailMessageBody = mailService.FetchBody(activeAccount, text, text2, hdr.Uid);
			string body = ((mailMessageBody != null) ? mailMessageBody.TextBody : "");
			string bodyHtml = ((mailMessageBody != null) ? mailMessageBody.HtmlBody : "");
			mailMessageHeader = hdr;
			string toAddrs = ((mailMessageHeader.ToAddresses != null) ? mailMessageHeader.ToAddresses : "");
			mailMessageHeader = hdr;
			string ccAddrs = ((mailMessageHeader.CcAddresses != null) ? mailMessageHeader.CcAddresses : "");
			string folder = text2;
			ulong uid = hdr.Uid;
			mailMessageHeader = hdr;
			OpenComposeFromImap(folder, uid, toAddrs, ccAddrs, (mailMessageHeader.Subject != null) ? mailMessageHeader.Subject : "", body, bodyHtml);
		}
		catch (Exception ex)
		{
			ProjectData.SetProjectError(ex);
			Exception ex2 = ex;
			SetStatus("DRAFT LOAD ERROR: " + ex2.Message);
			ProjectData.ClearProjectError();
		}
		finally
		{
			SetBusy(busy: false);
		}
	}

	private void OpenComposeFromDraft(MailDraft draft)
	{
		if (draft != null)
		{
			OpenComposeInternal(draft.ToAddresses, draft.CcAddresses, draft.Subject, draft.BodyPlain, draft.BodyHtml, draft, null, 0uL);
		}
	}

	private void OpenComposeFromImap(string folder, ulong uid, string toAddrs, string ccAddrs, string subject, string body, string bodyHtml)
	{
		OpenComposeInternal(toAddrs, ccAddrs, subject, body, bodyHtml, null, folder, uid);
	}

	private void UpdateThreadContext(MailMessageHeader hdr)
	{
		if (lstThreadContext == null || lblReadThread == null)
		{
			return;
		}
		lstThreadContext.Items.Clear();
		lstThreadContext.Visible = false;
		lblReadThread.Visible = false;
		if (hdr == null || currentFolderHeaders == null || currentFolderHeaders.Count < 2)
		{
			return;
		}
		List<MailThreadListItem> list = MailConversationGrouper.BuildThreadList(currentFolderHeaders);
		MailThreadListItem mailThreadListItem = null;
		foreach (MailThreadListItem item in list)
		{
			if (item == null || item.Messages == null)
			{
				continue;
			}
			foreach (MailMessageHeader message in item.Messages)
			{
				if (message != null && message.Uid == hdr.Uid)
				{
					mailThreadListItem = item;
					break;
				}
			}
			if (mailThreadListItem == null)
			{
				continue;
			}
			break;
		}
		if (mailThreadListItem == null || mailThreadListItem.MessageCount <= 1)
		{
			return;
		}
		lblReadThread.Text = "THREAD (" + mailThreadListItem.MessageCount + ")";
		lblReadThread.Visible = true;
		lstThreadContext.Visible = true;
		foreach (MailMessageHeader message2 in mailThreadListItem.Messages)
		{
			if (message2 != null)
			{
				string text = ((message2.Uid == hdr.Uid) ? "▸ " : "  ");
				MailMessageHeader mailMessageHeader = message2;
				string text2 = ((mailMessageHeader.Subject != null) ? mailMessageHeader.Subject : "");
				if (text2.Length > 40)
				{
					text2 = text2.Substring(0, 37) + "…";
				}
				string[] array = new string[6]
				{
					text,
					message2.DateUtc.ToLocalTime().ToString("g"),
					" — ",
					text2,
					" — ",
					null
				};
				mailMessageHeader = message2;
				array[5] = ((mailMessageHeader.From != null) ? mailMessageHeader.From : "");
				string text3 = string.Concat(array);
				if (text3.Length > 100)
				{
					text3 = text3.Substring(0, 97) + "…";
				}
				lstThreadContext.Items.Add(new ThreadContextListItem(message2, text3.ToUpperInvariant()));
			}
		}
	}

	private void OnThreadContextSelected(object sender, EventArgs e)
	{
		if (lstThreadContext != null && lstThreadContext.SelectedItem != null && lstThreadContext.SelectedItem is ThreadContextListItem { Header: not null } threadContextListItem && (selectedHeader == null || selectedHeader.Uid != threadContextListItem.Header.Uid))
		{
			selectedHeader = threadContextListItem.Header;
			if (messageButtonsByUid.ContainsKey(threadContextListItem.Header.Uid))
			{
				selectedMessageButton = messageButtonsByUid[threadContextListItem.Header.Uid];
			}
			ClearMessageSelectionKeepButtons();
			selectedMessages[threadContextListItem.Header.Uid] = threadContextListItem.Header;
			ApplyMessageSelectionColors();
			StartFetchBody(threadContextListItem.Header);
		}
	}

	private void OpenCompose(string toAddrs, string ccAddrs, string subject, string body, string bodyHtml = null)
	{
		OpenComposeInternal(toAddrs, ccAddrs, subject, body, bodyHtml, null, null, 0uL);
	}

	private void OpenComposeInternal(string toAddrs, string ccAddrs, string subject, string body, string bodyHtml, MailDraft linkedDraft, string imapFolder, ulong imapUid)
	{
		if (activeAccount == null)
		{
			SetStatus("IMPORT A THUNDERBIRD PROFILE");
			return;
		}
		string text = ResolvePassword(activeAccount, promptIfMissing: true);
		if (text == null)
		{
			SetStatus("PASSWORD REQUIRED");
			return;
		}
		using frmCompose frmCompose2 = new frmCompose(activeAccount, text);
		if (linkedDraft != null)
		{
			frmCompose2.ApplyDraftLink(linkedDraft);
		}
		else
		{
			frmCompose2.ComposeTo = toAddrs;
			frmCompose2.ComposeCc = ccAddrs;
			frmCompose2.ComposeSubject = subject;
			if (!string.IsNullOrWhiteSpace(bodyHtml))
			{
				frmCompose2.ComposeBodyHtml = bodyHtml;
			}
			else
			{
				frmCompose2.ComposeBody = body;
			}
		}
		if (imapUid != 0 && !string.IsNullOrWhiteSpace(imapFolder))
		{
			frmCompose2.ApplyImapDraftLink(imapFolder, imapUid);
		}
		frmCompose2.SendStatusReporter = [SpecialName] (string msg) =>
		{
			SetStatus(msg);
		};
		DialogResult dialogResult = frmCompose2.ShowDialog(this);
		if (dialogResult == DialogResult.OK)
		{
			SetStatus("MESSAGE SENT");
		}
	}

	private string BodyTextForQuote(MailMessageHeader hdr)
	{
		if (selectedBody != null)
		{
			string readingPaneText = selectedBody.GetReadingPaneText();
			if (!string.IsNullOrWhiteSpace(readingPaneText))
			{
				return readingPaneText;
			}
		}
		if (hdr == null || activeAccount == null)
		{
			return "";
		}
		string text = ((selectedFolderFullName != null) ? selectedFolderFullName : hdr.Folder);
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "INBOX";
		}
		MailMessageBody body = null;
		if (MailCache.TryLoadBody(activeAccount.Id, text, hdr.Uid, ref body) && body != null)
		{
			return body.GetReadingPaneText();
		}
		if (bodyLabel != null && bodyLabel.Visible)
		{
			string text2 = bodyLabel.Text;
			string text3 = ((text2 != null) ? text2 : "");
			if (text3.Length > 0 && !string.Equals(text3, "(NO TEXT BODY)", StringComparison.OrdinalIgnoreCase))
			{
				return text3;
			}
		}
		return "";
	}

	private string BuildQuotedReplyBody(MailMessageHeader hdr)
	{
		if (hdr == null)
		{
			return "";
		}
		string text = BodyTextForQuote(hdr);
		string text2 = ((hdr.From != null) ? hdr.From : "");
		string text3 = "";
		if (DateTime.Compare(hdr.DateUtc, DateTime.MinValue) != 0)
		{
			text3 = hdr.DateUtc.ToLocalTime().ToString("f");
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine();
		stringBuilder.AppendLine();
		if (text3.Length > 0 || text2.Length > 0)
		{
			stringBuilder.Append("On ");
			if (text3.Length > 0)
			{
				stringBuilder.Append(text3);
			}
			if (text2.Length > 0)
			{
				if (text3.Length > 0)
				{
					stringBuilder.Append(", ");
				}
				stringBuilder.Append(text2);
			}
			stringBuilder.AppendLine(" wrote:");
		}
		string[] array = text.Split(new string[2] { "\r\n", "\n" }, StringSplitOptions.None);
		foreach (string text4 in array)
		{
			stringBuilder.AppendLine("> " + text4);
		}
		return stringBuilder.ToString();
	}

	private string BuildQuotedForwardBody(MailMessageHeader hdr)
	{
		if (hdr == null)
		{
			return "";
		}
		string value = BodyTextForQuote(hdr);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("---------- Forwarded message ----------");
		if (!string.IsNullOrWhiteSpace(hdr.From))
		{
			stringBuilder.AppendLine("From: " + hdr.From);
		}
		if (DateTime.Compare(hdr.DateUtc, DateTime.MinValue) != 0)
		{
			stringBuilder.AppendLine("Date: " + hdr.DateUtc.ToLocalTime().ToString("f"));
		}
		stringBuilder.AppendLine("Subject: " + ((hdr.Subject != null) ? hdr.Subject : ""));
		if (!string.IsNullOrWhiteSpace(hdr.ToAddresses))
		{
			stringBuilder.AppendLine("To: " + hdr.ToAddresses);
		}
		if (!string.IsNullOrWhiteSpace(hdr.CcAddresses))
		{
			stringBuilder.AppendLine("Cc: " + hdr.CcAddresses);
		}
		stringBuilder.AppendLine();
		stringBuilder.Append(value);
		return stringBuilder.ToString();
	}

	private static string SubjectWithPrefix(string prefix, string subject)
	{
		string text = ((subject != null) ? subject : "").Trim();
		string text2 = ((prefix != null) ? prefix : "").Trim();
		if (text2.Length == 0)
		{
			return text;
		}
		if (!text2.EndsWith(":", StringComparison.Ordinal))
		{
			text2 += ":";
		}
		if (text.Length == 0)
		{
			return text2 + " ";
		}
		if (text.StartsWith(text2, StringComparison.OrdinalIgnoreCase))
		{
			return text;
		}
		return text2 + " " + text;
	}

	private static void BuildReplyAllAddresses(MailMessageHeader hdr, MailAccount account, ref string toOut, ref string ccOut)
	{
		toOut = ((hdr.From != null) ? hdr.From : "").Trim();
		ccOut = "";
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (account != null)
		{
			AddSelfKey(hashSet, account.EmailAddress);
			AddSelfKey(hashSet, account.Username);
			AddSelfKey(hashSet, account.SmtpUsername);
			AddSelfKey(hashSet, account.DisplayName);
		}
		string text = NormalizeAddressKey(toOut);
		List<string> list = new List<string>();
		HashSet<string> hashSet2 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string item in SplitAddressList(hdr.ToAddresses))
		{
			string text2 = NormalizeAddressKey(item);
			if (text2.Length != 0 && !hashSet.Contains(text2) && (text.Length <= 0 || !string.Equals(text2, text, StringComparison.OrdinalIgnoreCase)) && hashSet2.Add(text2))
			{
				list.Add(item.Trim());
			}
		}
		foreach (string item2 in SplitAddressList(hdr.CcAddresses))
		{
			string text3 = NormalizeAddressKey(item2);
			if (text3.Length != 0 && !hashSet.Contains(text3) && (text.Length <= 0 || !string.Equals(text3, text, StringComparison.OrdinalIgnoreCase)) && hashSet2.Add(text3))
			{
				list.Add(item2.Trim());
			}
		}
		ccOut = string.Join(", ", list.ToArray());
	}

	private static void AddSelfKey(HashSet<string> keys, string value)
	{
		string text = NormalizeAddressKey(value);
		if (text.Length > 0)
		{
			keys.Add(text);
		}
	}

	private static IEnumerable<string> SplitAddressList(string list)
	{
		return MailAddressHelper.SplitAddressTokens(list).ToArray();
	}

	private void TryPrefetchInlineAttachments(MailMessageHeader hdr, MailMessageBody body)
	{
		if (body == null || body.Attachments == null || activeAccount == null || hdr == null)
		{
			return;
		}
		string text = ((body.HtmlBody != null) ? body.HtmlBody : "");
		if (text.IndexOf("cid:", StringComparison.OrdinalIgnoreCase) < 0)
		{
			return;
		}
		string text2 = ResolvePassword(activeAccount, promptIfMissing: false);
		if (text2 == null)
		{
			return;
		}
		string text3 = ((selectedFolderFullName != null) ? selectedFolderFullName : hdr.Folder);
		if (string.IsNullOrWhiteSpace(text3))
		{
			text3 = "INBOX";
		}
		bool flag = false;
		foreach (MailAttachmentInfo attachment in body.Attachments)
		{
			if (attachment == null || !attachment.IsInline || (!string.IsNullOrWhiteSpace(attachment.CachedPath) && File.Exists(attachment.CachedPath)))
			{
				continue;
			}
			string text4 = Path.Combine(Path.GetTempPath(), "lcars-inline-" + Guid.NewGuid().ToString("N"));
			try
			{
				string text5 = mailService.SaveAttachmentToFile(activeAccount, text2, text3, hdr.Uid, attachment.PartIndex, text4);
				if (text5 == null || text5.Length <= 0)
				{
					attachment.CachedPath = MailCache.SaveAttachmentCache(activeAccount.Id, text3, hdr.Uid, attachment.PartIndex, attachment.FileName, text4);
					flag = true;
				}
			}
			finally
			{
				try
				{
					if (File.Exists(text4))
					{
						File.Delete(text4);
					}
				}
				catch (Exception projectError)
				{
					ProjectData.SetProjectError(projectError);
					ProjectData.ClearProjectError();
				}
			}
		}
		if (flag && htmlReadPane != null)
		{
			htmlReadPane.ShowBody(body, body.Attachments);
		}
	}

	private static string NormalizeAddressKey(string addr)
	{
		string text = ((addr != null) ? addr : "").Trim();
		if (text.Length == 0)
		{
			return "";
		}
		int num = text.LastIndexOf('<');
		if (num >= 0)
		{
			int num2 = text.IndexOf('>', num);
			if (num2 > num)
			{
				text = checked(text.Substring(num + 1, num2 - num - 1)).Trim();
			}
		}
		if (text.Contains("@"))
		{
			return text.ToLowerInvariant();
		}
		return text.ToLowerInvariant();
	}

	private void ShowEmptyStates()
	{
		if (folderHost != null)
		{
			lblFolderEmpty.Visible = true;
		}
		ClearMessageList();
		ClearReadingPane();
	}
}
