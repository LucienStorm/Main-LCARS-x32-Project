using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

public sealed class CardDavLiveBookCache
{
	private const int FreshSeconds = 8;

	private const int FreshSecondsDirty = 4;

	public static DateTime LastFetchUtc = DateTime.MinValue;

	private const int AlwaysOnRefreshMs = 12000;

	private const int AlwaysOnRefreshDirtyMs = 4000;

	public static int LastAlwaysOnIntervalMs = 12000;

	private static readonly object gate = RuntimeHelpers.GetObjectValue(new object());

	private static List<MailContact> cached;

	private static bool refreshing;

	private static Timer alwaysOnTimer;

	public static string LastRefreshSummary = "";

	public static int LastContactCount = 0;

	public static bool AlwaysOnActive = false;

	public static DateTime LastAlwaysOnTickUtc = DateTime.MinValue;

	public static int LastAlwaysOnPushCount = 0;

	public static int LastAlwaysOnDirtyCount = 0;

	public static DateTime LastAlwaysOnSyncUtc = DateTime.MinValue;

	public static int LastBidirectionalPushCount = 0;

	private const int BookDiscoveryIntervalSeconds = 3600;

	private static DateTime lastBookDiscoveryUtc = DateTime.MinValue;

	public static int LastDiscoveredBooksOnRefresh = 0;

	public static int AlwaysOnCycleCount = 0;

	private CardDavLiveBookCache()
	{
	}

	public static List<MailContact> GetLiveContacts()
	{
		object obj = gate;
		ObjectFlowControl.CheckForSyncLockOnValueType(obj);
		bool lockTaken = false;
		try
		{
			Monitor.Enter(obj, ref lockTaken);
			int num = ((AddressBookStore.CountCardDavDirtyContacts() > 0) ? 4 : 8);
			if (cached != null && (DateTime.UtcNow - LastFetchUtc).TotalSeconds < (double)num)
			{
				return CloneList(cached);
			}
		}
		finally
		{
			if (lockTaken)
			{
				Monitor.Exit(obj);
			}
		}
		return RefreshNow();
	}

	public static List<MailContact> RefreshNow()
	{
		checked
		{
			if (AlwaysOnActive)
			{
				AlwaysOnCycleCount++;
			}
			string serverUrl = null;
			string username = null;
			string password = null;
			if (AddressBookStore.TryLoadCardDavSettings(ref serverUrl, ref username, ref password))
			{
				if ((DateTime.UtcNow - lastBookDiscoveryUtc).TotalSeconds >= 3600.0)
				{
					LastDiscoveredBooksOnRefresh = AddressBookStore.TryRefreshDiscoveredCardDavBooksFromServer(serverUrl, username, password);
					lastBookDiscoveryUtc = DateTime.UtcNow;
				}
				int num = AddressBookStore.TrySyncStoredCardDavBidirectional();
				if (num >= 0)
				{
					AddressBookStore.TryMergeSecondaryThunderbirdCardDavBooks();
					List<MailContact> list = AddressBookStore.LoadContacts();
					List<MailContact> list2 = AddressBookStore.TryFetchLiveCardDavContacts();
					if (list2 != null && list2.Count > 0)
					{
						list = list2;
					}
					ReplaceCache(list);
					string text = list.Count + " contact(s) bidirectional";
					if (AddressBookStore.LastCardDavRemotePullCount > 0)
					{
						text = text + " · remote " + AddressBookStore.LastCardDavRemotePullCount;
					}
					if (AddressBookStore.LastCardDavPushCount > 0)
					{
						text = text + " · pushed " + AddressBookStore.LastCardDavPushCount;
					}
					if (CardDavSyncService.LastMergedAfter412)
					{
						text += " · 412 merged";
					}
					if (AddressBookStore.CountCardDavDirtyContacts() > 0)
					{
						int num2 = AddressBookStore.TryResolveAllCardDav412DirtyContacts();
						if (num2 > 0)
						{
							text = text + " · 412-FIX " + num2;
						}
					}
					if (LastDiscoveredBooksOnRefresh > 0)
					{
						text = text + " · BOOKS " + LastDiscoveredBooksOnRefresh;
					}
					if (AddressBookStore.LastCardDavSecondaryBooksMerged > 0)
					{
						text = text + " · 2ND+" + AddressBookStore.LastCardDavSecondaryBooksMerged;
					}
					LastAlwaysOnPushCount = AddressBookStore.LastCardDavPushCount;
					LastBidirectionalPushCount = AddressBookStore.LastCardDavPushCount;
					LastAlwaysOnDirtyCount = AddressBookStore.CountCardDavDirtyContacts();
					if (string.Equals(CardDavSyncService.ConflictOn412Mode, "ServerWins", StringComparison.OrdinalIgnoreCase))
					{
						text += " · 412-SERVER";
					}
					else if (string.Equals(CardDavSyncService.ConflictOn412Mode, "ClientMerge", StringComparison.OrdinalIgnoreCase))
					{
						text += " · 412-CLIENT";
					}
					if (AlwaysOnActive)
					{
						text += " · always-on";
						if (LastAlwaysOnPushCount > 0)
						{
							text = text + " · pushed " + LastAlwaysOnPushCount;
						}
						if (LastAlwaysOnDirtyCount > 0)
						{
							text = text + " · dirty " + LastAlwaysOnDirtyCount;
						}
					}
					if (LastAlwaysOnDirtyCount == 0 && LastAlwaysOnIntervalMs != 12000)
					{
						text = text + " · idle " + 12 + "s";
					}
					else if (LastAlwaysOnDirtyCount > 0)
					{
						text = text + " · fast " + 4 + "s";
					}
					LastRefreshSummary = text;
					RescheduleAlwaysOnTimer();
					return CloneList(list);
				}
			}
			List<MailContact> list3 = AddressBookStore.TryFetchLiveCardDavContacts();
			if (list3 == null)
			{
				LastRefreshSummary = ((AddressBookStore.LastError != null) ? AddressBookStore.LastError : "CardDAV live fetch failed");
				return AddressBookStore.LoadContacts();
			}
			ReplaceCache(list3);
			LastRefreshSummary = list3.Count + " live contact(s)";
			RescheduleAlwaysOnTimer();
			return CloneList(list3);
		}
	}

	public static void EnsureAlwaysOnStarted()
	{
		object obj = gate;
		ObjectFlowControl.CheckForSyncLockOnValueType(obj);
		bool lockTaken = false;
		try
		{
			Monitor.Enter(obj, ref lockTaken);
			if (alwaysOnTimer != null)
			{
				return;
			}
			alwaysOnTimer = new Timer(period: LastAlwaysOnIntervalMs = GetRecommendedAlwaysOnIntervalMs(), callback: AlwaysOnTimerTick, state: null, dueTime: 2000);
			AlwaysOnActive = true;
		}
		finally
		{
			if (lockTaken)
			{
				Monitor.Exit(obj);
			}
		}
		QueueBackgroundRefresh();
	}

	public static int GetRecommendedAlwaysOnIntervalMs()
	{
		if (AddressBookStore.CountCardDavDirtyContacts() > 0)
		{
			return 4000;
		}
		return 12000;
	}

	private static void RescheduleAlwaysOnTimer()
	{
		object obj = gate;
		ObjectFlowControl.CheckForSyncLockOnValueType(obj);
		bool lockTaken = false;
		try
		{
			Monitor.Enter(obj, ref lockTaken);
			if (alwaysOnTimer != null)
			{
				int period = (LastAlwaysOnIntervalMs = GetRecommendedAlwaysOnIntervalMs());
				alwaysOnTimer.Change(1000, period);
			}
		}
		finally
		{
			if (lockTaken)
			{
				Monitor.Exit(obj);
			}
		}
	}

	private static void AlwaysOnTimerTick(object state)
	{
		LastAlwaysOnTickUtc = DateTime.UtcNow;
		string serverUrl = null;
		string username = null;
		string password = null;
		if (AddressBookStore.TryLoadCardDavSettings(ref serverUrl, ref username, ref password) && !string.IsNullOrWhiteSpace(serverUrl))
		{
			LastAlwaysOnDirtyCount = AddressBookStore.CountCardDavDirtyContacts();
			QueueBackgroundRefresh();
		}
	}

	public static void QueueBackgroundRefresh()
	{
		object obj = gate;
		ObjectFlowControl.CheckForSyncLockOnValueType(obj);
		bool lockTaken = false;
		try
		{
			Monitor.Enter(obj, ref lockTaken);
			if (refreshing)
			{
				return;
			}
			refreshing = true;
		}
		finally
		{
			if (lockTaken)
			{
				Monitor.Exit(obj);
			}
		}
		ThreadPool.QueueUserWorkItem([SpecialName] (object state) =>
		{
			try
			{
				RefreshNow();
			}
			finally
			{
				object obj2 = gate;
				ObjectFlowControl.CheckForSyncLockOnValueType(obj2);
				bool lockTaken2 = false;
				try
				{
					Monitor.Enter(obj2, ref lockTaken2);
					refreshing = false;
				}
				finally
				{
					if (lockTaken2)
					{
						Monitor.Exit(obj2);
					}
				}
			}
		});
	}

	public static void NotifyLocalBookUpdated(List<MailContact> merged)
	{
		if (merged != null && merged.Count != 0)
		{
			ReplaceCache(merged);
			if (AddressBookStore.CountCardDavDirtyContacts() > 0)
			{
				QueueBackgroundRefresh();
			}
		}
	}

	private static void ReplaceCache(List<MailContact> contacts)
	{
		object obj = gate;
		ObjectFlowControl.CheckForSyncLockOnValueType(obj);
		bool lockTaken = false;
		try
		{
			Monitor.Enter(obj, ref lockTaken);
			cached = CloneList(contacts);
			LastFetchUtc = DateTime.UtcNow;
			LastAlwaysOnSyncUtc = DateTime.UtcNow;
			LastContactCount = contacts?.Count ?? 0;
		}
		finally
		{
			if (lockTaken)
			{
				Monitor.Exit(obj);
			}
		}
	}

	private static List<MailContact> CloneList(List<MailContact> source)
	{
		if (source == null)
		{
			return new List<MailContact>();
		}
		return new List<MailContact>(source);
	}
}
