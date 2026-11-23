using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

public sealed class MailSearchBackgroundIndexer
{
	private const int FoldersPerRun = 384;

	private const int MaxFoldersPerAccount = 4096;

	private const int HeadersPerFolder = 0;

	private const int BodyPreviewPerFolder = 0;

	private const int BodyBackfillPerAccount = 176000;

	private const int BodyBackfillBurstPerAccount = 1024;

	private static readonly object gate = RuntimeHelpers.GetObjectValue(new object());

	private static bool indexing;

	private static bool backfilling;

	public static string LastRunSummary = "";

	public static int LastIndexedFolderCount = 0;

	public static int LastBackfillRemaining = 0;

	public static int LastBodiesIndexedThisRun = 0;

	private MailSearchBackgroundIndexer()
	{
	}

	public static void QueueIndexAccounts(IList<MailAccount> accounts, Func<MailAccount, string> passwordResolver, UnifiedMailService mailService)
	{
		if (accounts == null || accounts.Count == 0 || mailService == null)
		{
			return;
		}
		object obj = gate;
		ObjectFlowControl.CheckForSyncLockOnValueType(obj);
		bool lockTaken = false;
		try
		{
			Monitor.Enter(obj, ref lockTaken);
			if (indexing)
			{
				return;
			}
			indexing = true;
		}
		finally
		{
			if (lockTaken)
			{
				Monitor.Exit(obj);
			}
		}
		List<MailAccount> accounts2 = new List<MailAccount>(accounts);
		ThreadPool.QueueUserWorkItem([SpecialName] (object state) =>
		{
			try
			{
				RunIndex(accounts2, passwordResolver, mailService);
			}
			finally
			{
				object obj2 = gate;
				ObjectFlowControl.CheckForSyncLockOnValueType(obj2);
				bool lockTaken2 = false;
				try
				{
					Monitor.Enter(obj2, ref lockTaken2);
					indexing = false;
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

	private static void RunIndex(List<MailAccount> accounts, Func<MailAccount, string> passwordResolver, UnifiedMailService mailService)
	{
		int num = 0;
		int num2 = 0;
		int bodiesIndexed = 0;
		checked
		{
			foreach (MailAccount account in accounts)
			{
				if (account == null)
				{
					continue;
				}
				MailAccount mailAccount = account;
				string text = ((mailAccount.Id != null) ? mailAccount.Id : "");
				if (text.Length == 0)
				{
					continue;
				}
				string text2 = null;
				if (passwordResolver != null)
				{
					text2 = passwordResolver(account);
				}
				if (EwsMailBridge.IsEwsAccount(account) && string.IsNullOrEmpty(text2))
				{
					text2 = " ";
				}
				if (string.IsNullOrEmpty(text2))
				{
					continue;
				}
				List<MailFolderInfo> list = null;
				try
				{
					list = mailService.ListFolders(account, text2);
				}
				catch (Exception projectError)
				{
					ProjectData.SetProjectError(projectError);
					list = null;
					ProjectData.ClearProjectError();
				}
				if (list == null || list.Count == 0)
				{
					continue;
				}
				List<MailFolderInfo> list2 = MailSearchIndexCursor.TakeNextFolders(text, list, 384);
				foreach (MailFolderInfo item in list2)
				{
					if (item == null)
					{
						continue;
					}
					num++;
					try
					{
						List<MailMessageHeader> list3 = mailService.SyncHeaders(account, text2, item.FullName, 0);
						if (list3 == null || list3.Count == 0)
						{
							continue;
						}
						if (false && list3.Count > 0)
						{
							list3 = list3.GetRange(0, 0);
						}
						MailSearchIndex.UpsertHeaders(text, item.FullName, list3);
						num2 += list3.Count;
						int num3 = 0;
						foreach (MailMessageHeader item2 in list3)
						{
							if (item2 != null)
							{
								if (false && num3 >= 0)
								{
									break;
								}
								num3++;
								try
								{
									MailMessageBody body = mailService.FetchBody(account, text2, item.FullName, item2.Uid);
									UpsertBodyPreviewFromFetch(text, item.FullName, item2.Uid, body, ref bodiesIndexed);
								}
								catch (Exception projectError2)
								{
									ProjectData.SetProjectError(projectError2);
									ProjectData.ClearProjectError();
								}
							}
						}
					}
					catch (Exception projectError3)
					{
						ProjectData.SetProjectError(projectError3);
						ProjectData.ClearProjectError();
					}
				}
				MailSearchIndex.BackfillGlodaFtsMetaDates(text, 22000);
				int num4 = MailSearchIndex.RebuildFtsHeaderCorpusForAccount(text, 4400);
				if (num4 > 0 && string.IsNullOrWhiteSpace(LastRunSummary))
				{
					LastRunSummary = "FTS-CORPUS " + num4;
				}
				List<MailSearchIndex.IndexBodyTarget> list4 = MailSearchIndex.ListEntriesNeedingBodyPreview(text, 176000);
				foreach (MailSearchIndex.IndexBodyTarget item3 in list4)
				{
					if (item3 != null)
					{
						try
						{
							MailMessageBody body2 = mailService.FetchBody(account, text2, item3.Folder, item3.Uid);
							UpsertBodyPreviewFromFetch(text, item3.Folder, item3.Uid, body2, ref bodiesIndexed);
						}
						catch (Exception projectError4)
						{
							ProjectData.SetProjectError(projectError4);
							ProjectData.ClearProjectError();
						}
					}
				}
			}
			LastIndexedFolderCount = num;
			LastBodiesIndexedThisRun = bodiesIndexed;
			LastBackfillRemaining = 0;
			foreach (MailAccount account2 in accounts)
			{
				if (account2 != null && !string.IsNullOrWhiteSpace(account2.Id))
				{
					int num5 = MailSearchIndex.CountEntriesNeedingBodyPreview(account2.Id);
					if (num5 > LastBackfillRemaining)
					{
						LastBackfillRemaining = num5;
					}
				}
			}
			string text3 = ((LastBackfillRemaining > 0) ? (" · BACKFILL " + LastBackfillRemaining) : "");
			LastRunSummary = "INDEX " + num + " fld / " + num2 + " hdr / " + bodiesIndexed + " body" + text3;
		}
	}

	public static void QueueBodyBackfillBurst(IList<MailAccount> accounts, Func<MailAccount, string> passwordResolver, UnifiedMailService mailService)
	{
		if (accounts == null || accounts.Count == 0 || mailService == null)
		{
			return;
		}
		object obj = gate;
		ObjectFlowControl.CheckForSyncLockOnValueType(obj);
		bool lockTaken = false;
		try
		{
			Monitor.Enter(obj, ref lockTaken);
			if (indexing || backfilling)
			{
				return;
			}
			backfilling = true;
		}
		finally
		{
			if (lockTaken)
			{
				Monitor.Exit(obj);
			}
		}
		List<MailAccount> accounts2 = new List<MailAccount>(accounts);
		ThreadPool.QueueUserWorkItem([SpecialName] (object state) =>
		{
			try
			{
				RunBodyBackfillBurst(accounts2, passwordResolver, mailService);
			}
			finally
			{
				object obj2 = gate;
				ObjectFlowControl.CheckForSyncLockOnValueType(obj2);
				bool lockTaken2 = false;
				try
				{
					Monitor.Enter(obj2, ref lockTaken2);
					backfilling = false;
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

	private static void RunBodyBackfillBurst(List<MailAccount> accounts, Func<MailAccount, string> passwordResolver, UnifiedMailService mailService)
	{
		int bodiesIndexed = 0;
		foreach (MailAccount account in accounts)
		{
			if (account == null)
			{
				continue;
			}
			MailAccount mailAccount = account;
			string text = ((mailAccount.Id != null) ? mailAccount.Id : "");
			if (text.Length == 0)
			{
				continue;
			}
			int num = MailSearchIndex.CountEntriesNeedingBodyPreview(text);
			if (num < 500)
			{
				continue;
			}
			string text2 = null;
			if (passwordResolver != null)
			{
				text2 = passwordResolver(account);
			}
			if (EwsMailBridge.IsEwsAccount(account) && string.IsNullOrEmpty(text2))
			{
				text2 = " ";
			}
			if (string.IsNullOrEmpty(text2))
			{
				continue;
			}
			int maxCount = 1024;
			if (num >= 340000)
			{
				maxCount = 21504;
			}
			else if (num >= 320000)
			{
				maxCount = 20480;
			}
			else if (num >= 300000)
			{
				maxCount = 19456;
			}
			else if (num >= 280000)
			{
				maxCount = 18432;
			}
			else if (num >= 260000)
			{
				maxCount = 17408;
			}
			else if (num >= 240000)
			{
				maxCount = 16384;
			}
			else if (num >= 220000)
			{
				maxCount = 15360;
			}
			else if (num >= 200000)
			{
				maxCount = 14336;
			}
			else if (num >= 180000)
			{
				maxCount = 13312;
			}
			else if (num >= 160000)
			{
				maxCount = 12288;
			}
			else if (num >= 140000)
			{
				maxCount = 11264;
			}
			else if (num >= 120000)
			{
				maxCount = 10240;
			}
			else if (num >= 100000)
			{
				maxCount = 9216;
			}
			else if (num >= 80000)
			{
				maxCount = 8192;
			}
			else if (num >= 60000)
			{
				maxCount = 7168;
			}
			else if (num >= 40000)
			{
				maxCount = 6144;
			}
			else if (num >= 20000)
			{
				maxCount = 5120;
			}
			else if (num >= 10000)
			{
				maxCount = 4096;
			}
			else if (num >= 5000)
			{
				maxCount = 3072;
			}
			List<MailSearchIndex.IndexBodyTarget> list = MailSearchIndex.ListEntriesNeedingBodyPreview(text, maxCount);
			foreach (MailSearchIndex.IndexBodyTarget item in list)
			{
				if (item != null)
				{
					try
					{
						MailMessageBody body = mailService.FetchBody(account, text2, item.Folder, item.Uid);
						UpsertBodyPreviewFromFetch(text, item.Folder, item.Uid, body, ref bodiesIndexed);
					}
					catch (Exception projectError)
					{
						ProjectData.SetProjectError(projectError);
						ProjectData.ClearProjectError();
					}
				}
			}
		}
		if (bodiesIndexed > 0)
		{
			LastBodiesIndexedThisRun = bodiesIndexed;
			LastRunSummary = "BACKFILL BURST " + bodiesIndexed + " body → FTS";
		}
		LastBackfillRemaining = 0;
		foreach (MailAccount account2 in accounts)
		{
			if (account2 != null && !string.IsNullOrWhiteSpace(account2.Id))
			{
				int num2 = MailSearchIndex.CountEntriesNeedingBodyPreview(account2.Id);
				if (num2 > LastBackfillRemaining)
				{
					LastBackfillRemaining = num2;
				}
			}
		}
	}

	private static void UpsertBodyPreviewFromFetch(string accountId, string folder, ulong uid, MailMessageBody body, ref int bodiesIndexed)
	{
		checked
		{
			if (body != null)
			{
				int attachmentCount = CountNonInlineAttachments(body);
				MailSearchIndex.UpsertBodyPreview(accountId, folder, uid, body.GetReadingPaneText(), attachmentCount);
				bodiesIndexed++;
			}
		}
	}

	private static int CountNonInlineAttachments(MailMessageBody body)
	{
		int num = 0;
		if (body == null || body.Attachments == null)
		{
			return 0;
		}
		foreach (MailAttachmentInfo attachment in body.Attachments)
		{
			if (attachment != null && !attachment.IsInline)
			{
				num = checked(num + 1);
			}
		}
		return num;
	}
}
