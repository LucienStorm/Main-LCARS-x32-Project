using System;
using System.Collections.Generic;

namespace LCARSMail;

public sealed class EwsBackgroundSync
{
	public static int LastBackgroundCalAutoReconcile = 0;

	public static int LastBackgroundCalChangeKeyRefreshes = 0;

	public static int LastBackgroundGalPushRetry = 0;

	public static int LastBackgroundCalPushAfterCk = 0;

	public static int LastBackgroundGalPushSecondRetry = 0;

	public static int LastBackgroundGalPushThirdRetry = 0;

	public static int LastBackgroundCalBidirectionalThirdPass = 0;

	public static int LastBackgroundCalPushAfterThirdPass = 0;

	public static int LastBackgroundGalPushFourthRetry = 0;

	public static int LastBackgroundCalBidirectionalFourthPass = 0;

	public static int LastBackgroundGalPushFifthRetry = 0;

	public static int LastBackgroundCalAutoReconcileSecondPass = 0;

	public static int LastBackgroundCalChangeKeyRefreshesSecondPass = 0;

	public static int LastBackgroundCalPushAfterFourthPass = 0;

	public static int LastBackgroundCalBidirectionalFifthPass = 0;

	public static int LastBackgroundGalPushSixthRetry = 0;

	public static int LastBackgroundCalAutoReconcileThirdPass = 0;

	public static int LastBackgroundCalPushAfterFifthPass = 0;

	public static int LastBackgroundCalBidirectionalSixthPass = 0;

	public static int LastBackgroundGalPushSeventhRetry = 0;

	public static int LastBackgroundCalChangeKeyRefreshesThirdPass = 0;

	public static int LastBackgroundCalAutoReconcileFourthPass = 0;

	public static int LastBackgroundCalPushAfterSixthPass = 0;

	public static int LastBackgroundCalBidirectionalSeventhPass = 0;

	public static int LastBackgroundCalBidirectionalEighthPass = 0;

	public static int LastBackgroundGalPushEighthRetry = 0;

	public static int LastBackgroundGalPushNinthRetry = 0;

	private EwsBackgroundSync()
	{
	}

	public static int TryMergeCalendarsForAccounts(IList<MailAccount> accounts)
	{
		if (accounts == null || accounts.Count == 0)
		{
			return 0;
		}
		int num = 0;
		LastBackgroundCalAutoReconcile = 0;
		LastBackgroundCalChangeKeyRefreshes = 0;
		LastBackgroundGalPushRetry = 0;
		LastBackgroundCalPushAfterCk = 0;
		LastBackgroundGalPushSecondRetry = 0;
		LastBackgroundGalPushThirdRetry = 0;
		LastBackgroundCalBidirectionalThirdPass = 0;
		LastBackgroundCalPushAfterThirdPass = 0;
		LastBackgroundGalPushFourthRetry = 0;
		LastBackgroundCalBidirectionalFourthPass = 0;
		LastBackgroundGalPushFifthRetry = 0;
		LastBackgroundCalAutoReconcileSecondPass = 0;
		LastBackgroundCalChangeKeyRefreshesSecondPass = 0;
		LastBackgroundCalPushAfterFourthPass = 0;
		LastBackgroundCalBidirectionalFifthPass = 0;
		LastBackgroundGalPushSixthRetry = 0;
		LastBackgroundCalAutoReconcileThirdPass = 0;
		LastBackgroundCalPushAfterFifthPass = 0;
		LastBackgroundCalBidirectionalSixthPass = 0;
		LastBackgroundGalPushSeventhRetry = 0;
		LastBackgroundCalChangeKeyRefreshesThirdPass = 0;
		LastBackgroundCalAutoReconcileFourthPass = 0;
		LastBackgroundCalPushAfterSixthPass = 0;
		LastBackgroundCalBidirectionalSeventhPass = 0;
		LastBackgroundCalBidirectionalEighthPass = 0;
		LastBackgroundGalPushEighthRetry = 0;
		LastBackgroundGalPushNinthRetry = 0;
		string oauthToken = EwsMailBridge.ResolveOAuthToken();
		checked
		{
			foreach (MailAccount account in accounts)
			{
				if (account == null || !EwsMailBridge.IsEwsAccount(account) || string.IsNullOrWhiteSpace(account.EwsUrl))
				{
					continue;
				}
				string password = "";
				AccountStore.TryGetPassword(account.Id, ref password);
				int num2 = EwsLcarsBridge.TrySyncEwsCalendarBidirectional(account.EwsUrl, account.Username, password, oauthToken);
				if (EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents() > 0)
				{
					num2 = Math.Max(num2, EwsLcarsBridge.TrySyncEwsCalendarBidirectional(account.EwsUrl, account.Username, password, oauthToken));
				}
				if (EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents() > 0)
				{
					int num3 = EwsCalendarConflictHelper.TryAutoReconcileUnchangedDirty(account.EwsUrl, account.Username, password, oauthToken);
					if (num3 > 0)
					{
						num2 = Math.Max(num2, num3);
						LastBackgroundCalAutoReconcile += num3;
					}
					else if (EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents() > 0)
					{
						num2 = Math.Max(num2, EwsLcarsBridge.TrySyncEwsCalendarBidirectional(account.EwsUrl, account.Username, password, oauthToken));
					}
				}
				if (EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents() > 0)
				{
					int num4 = EwsCalendarConflictHelper.TryRefreshChangeKeysForDirtyEvents(account.EwsUrl, account.Username, password, oauthToken);
					if (num4 > 0)
					{
						LastBackgroundCalChangeKeyRefreshes += num4;
						num2 = Math.Max(num2, num4);
					}
					if (EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents() > 0)
					{
						int num5 = EwsLcarsBridge.TryPushDirtyCalendarEventsToEws(account.EwsUrl, account.Username, password, oauthToken);
						if (num5 > 0)
						{
							LastBackgroundCalPushAfterCk += num5;
							num2 = Math.Max(num2, num5);
						}
					}
					if (EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents() > 0)
					{
						int num6 = EwsLcarsBridge.TrySyncEwsCalendarBidirectional(account.EwsUrl, account.Username, password, oauthToken);
						if (num6 > 0)
						{
							LastBackgroundCalBidirectionalThirdPass += num6;
							num2 = Math.Max(num2, num6);
						}
					}
					if (EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents() > 0)
					{
						int num7 = EwsLcarsBridge.TryPushDirtyCalendarEventsToEws(account.EwsUrl, account.Username, password, oauthToken);
						if (num7 > 0)
						{
							LastBackgroundCalPushAfterThirdPass += num7;
							num2 = Math.Max(num2, num7);
						}
					}
					if (EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents() > 0)
					{
						int num8 = EwsLcarsBridge.TrySyncEwsCalendarBidirectional(account.EwsUrl, account.Username, password, oauthToken);
						if (num8 > 0)
						{
							LastBackgroundCalBidirectionalFourthPass += num8;
							num2 = Math.Max(num2, num8);
						}
					}
					if (EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents() > 0)
					{
						int num9 = EwsCalendarConflictHelper.TryAutoReconcileUnchangedDirty(account.EwsUrl, account.Username, password, oauthToken);
						if (num9 > 0)
						{
							LastBackgroundCalAutoReconcileSecondPass += num9;
							num2 = Math.Max(num2, num9);
						}
					}
					if (EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents() > 0)
					{
						int num10 = EwsCalendarConflictHelper.TryRefreshChangeKeysForDirtyEvents(account.EwsUrl, account.Username, password, oauthToken);
						if (num10 > 0)
						{
							LastBackgroundCalChangeKeyRefreshesSecondPass += num10;
							num2 = Math.Max(num2, num10);
						}
					}
					if (EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents() > 0)
					{
						int num11 = EwsLcarsBridge.TryPushDirtyCalendarEventsToEws(account.EwsUrl, account.Username, password, oauthToken);
						if (num11 > 0)
						{
							LastBackgroundCalPushAfterFourthPass += num11;
							num2 = Math.Max(num2, num11);
						}
					}
					if (EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents() > 0)
					{
						int num12 = EwsLcarsBridge.TrySyncEwsCalendarBidirectional(account.EwsUrl, account.Username, password, oauthToken);
						if (num12 > 0)
						{
							LastBackgroundCalBidirectionalFifthPass += num12;
							num2 = Math.Max(num2, num12);
						}
					}
					if (EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents() > 0)
					{
						int num13 = EwsCalendarConflictHelper.TryAutoReconcileUnchangedDirty(account.EwsUrl, account.Username, password, oauthToken);
						if (num13 > 0)
						{
							LastBackgroundCalAutoReconcileThirdPass += num13;
							num2 = Math.Max(num2, num13);
						}
					}
					if (EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents() > 0)
					{
						int num14 = EwsLcarsBridge.TryPushDirtyCalendarEventsToEws(account.EwsUrl, account.Username, password, oauthToken);
						if (num14 > 0)
						{
							LastBackgroundCalPushAfterFifthPass += num14;
							num2 = Math.Max(num2, num14);
						}
					}
					if (EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents() > 0)
					{
						int num15 = EwsLcarsBridge.TrySyncEwsCalendarBidirectional(account.EwsUrl, account.Username, password, oauthToken);
						if (num15 > 0)
						{
							LastBackgroundCalBidirectionalSixthPass += num15;
							num2 = Math.Max(num2, num15);
						}
					}
					if (EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents() > 0)
					{
						int num16 = EwsCalendarConflictHelper.TryAutoReconcileUnchangedDirty(account.EwsUrl, account.Username, password, oauthToken);
						if (num16 > 0)
						{
							LastBackgroundCalAutoReconcileFourthPass += num16;
							num2 = Math.Max(num2, num16);
						}
					}
					if (EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents() > 0)
					{
						int num17 = EwsCalendarConflictHelper.TryRefreshChangeKeysForDirtyEvents(account.EwsUrl, account.Username, password, oauthToken);
						if (num17 > 0)
						{
							LastBackgroundCalChangeKeyRefreshesThirdPass += num17;
							num2 = Math.Max(num2, num17);
						}
					}
					if (EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents() > 0)
					{
						int num18 = EwsLcarsBridge.TryPushDirtyCalendarEventsToEws(account.EwsUrl, account.Username, password, oauthToken);
						if (num18 > 0)
						{
							LastBackgroundCalPushAfterSixthPass += num18;
							num2 = Math.Max(num2, num18);
						}
					}
					if (EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents() > 0)
					{
						int num19 = EwsLcarsBridge.TrySyncEwsCalendarBidirectional(account.EwsUrl, account.Username, password, oauthToken);
						if (num19 > 0)
						{
							LastBackgroundCalBidirectionalSeventhPass += num19;
							num2 = Math.Max(num2, num19);
						}
					}
					if (EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents() > 0)
					{
						int num20 = EwsCalendarConflictHelper.TryAutoReconcileUnchangedDirty(account.EwsUrl, account.Username, password, oauthToken);
						if (num20 > 0)
						{
							LastBackgroundCalAutoReconcileFourthPass += num20;
							num2 = Math.Max(num2, num20);
						}
					}
					if (EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents() > 0)
					{
						int num21 = EwsLcarsBridge.TrySyncEwsCalendarBidirectional(account.EwsUrl, account.Username, password, oauthToken);
						if (num21 > 0)
						{
							LastBackgroundCalBidirectionalEighthPass += num21;
							num2 = Math.Max(num2, num21);
						}
					}
				}
				if (num2 > 0)
				{
					num += num2;
				}
				EwsLcarsBridge.TryMergePersonalContactsFromEws(account.EwsUrl, account.Username, password, oauthToken);
				if (EwsGalSyncStore.ShouldRunGalSync())
				{
					bool flag = EwsGalSyncStore.ShouldRunDeepGalSync();
					int num22 = EwsLcarsBridge.TrySyncGalDirectoryBidirectional(account.EwsUrl, account.Username, password, oauthToken, flag);
					if (num22 >= 0)
					{
						EwsGalSyncStore.MarkGalSyncCompleted(num22);
						if (flag)
						{
							EwsGalSyncStore.MarkDeepGalSyncCompleted();
						}
					}
					int num23 = 1;
					while (AddressBookStore.CountGalPushDirtyContacts() > 0)
					{
						int num24 = EwsLcarsBridge.TryPushGalDirtyPersonalShadows(account.EwsUrl, account.Username, password, oauthToken);
						if (num24 <= 0)
						{
							break;
						}
						num22 = Math.Max(num22, num24);
						RecordBackgroundGalPushPass(num23, num24);
						num23++;
						if (num23 > 9)
						{
							break;
						}
					}
				}
				else
				{
					if (AddressBookStore.CountGalPushDirtyContacts() <= 0)
					{
						continue;
					}
					int num25 = 1;
					while (AddressBookStore.CountGalPushDirtyContacts() > 0)
					{
						int num26 = EwsLcarsBridge.TryPushGalDirtyPersonalShadows(account.EwsUrl, account.Username, password, oauthToken);
						if (num26 <= 0)
						{
							break;
						}
						RecordBackgroundGalPushPass(num25, num26);
						num25++;
						if (num25 > 3)
						{
							break;
						}
					}
				}
			}
			TryFlushMeetingRepliesForAccounts(accounts);
			return num;
		}
	}

	private static void RecordBackgroundGalPushPass(int pass, int pushed)
	{
		checked
		{
			if (pushed > 0)
			{
				switch (pass)
				{
				case 1:
					LastBackgroundGalPushRetry = Math.Max(LastBackgroundGalPushRetry, pushed);
					break;
				case 2:
					LastBackgroundGalPushSecondRetry += pushed;
					break;
				case 3:
					LastBackgroundGalPushThirdRetry += pushed;
					break;
				case 4:
					LastBackgroundGalPushFourthRetry += pushed;
					break;
				case 5:
					LastBackgroundGalPushFifthRetry += pushed;
					break;
				case 6:
					LastBackgroundGalPushSixthRetry += pushed;
					break;
				case 7:
					LastBackgroundGalPushSeventhRetry += pushed;
					break;
				case 8:
					LastBackgroundGalPushEighthRetry += pushed;
					break;
				case 9:
					LastBackgroundGalPushNinthRetry += pushed;
					break;
				}
			}
		}
	}

	private static void TryFlushMeetingRepliesForAccounts(IList<MailAccount> accounts)
	{
		if (accounts == null)
		{
			return;
		}
		foreach (MailAccount account in accounts)
		{
			if (account != null)
			{
				MailAccount mailAccount = account;
				string text = ((mailAccount.SmtpHost != null) ? mailAccount.SmtpHost : "").Trim();
				if (text.Length != 0)
				{
					string password = "";
					AccountStore.TryGetPassword(account.Id, ref password);
					mailAccount = account;
					string fromAddress = ((mailAccount.EmailAddress != null) ? mailAccount.EmailAddress : account.Username).Trim();
					MailCalendarMeetingReplyBridge.TryFlushQueuedMeetingReplies(text, account.SmtpPort, fromAddress, account.Username, password);
					break;
				}
			}
		}
	}
}
