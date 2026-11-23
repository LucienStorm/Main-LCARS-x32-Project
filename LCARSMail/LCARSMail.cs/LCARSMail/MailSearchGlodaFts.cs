using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

public sealed class MailSearchGlodaFts
{
	public class GlodaCorpusStats
	{
		public int TotalFtsRows;

		public int WithBodyRows;

		public int DistinctAccounts;

		[DebuggerNonUserCode]
		public GlodaCorpusStats()
		{
		}
	}

	private const string DbFileName = "gloda-fts.sqlite";

	private const string FtsCreateSql = "CREATE VIRTUAL TABLE IF NOT EXISTS mail_fts USING fts5(account_id UNINDEXED, folder UNINDEXED, uid UNINDEXED, subject, from_line, to_line, cc_line, body, tokenize='porter unicode61');";

	private MailSearchGlodaFts()
	{
	}

	public static string DbPath()
	{
		string text = Path.Combine(AccountStore.DataRoot(), "search-index");
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		return Path.Combine(text, "gloda-fts.sqlite");
	}

	public static void EnsureSchema()
	{
		using SQLiteConnection sQLiteConnection = new SQLiteConnection("Data Source=" + DbPath() + ";");
		sQLiteConnection.Open();
		ApplySearchPragmas(sQLiteConnection);
		TryMigrateFtsSchemaIfNeeded(sQLiteConnection);
		SQLiteCommand sQLiteCommand = new SQLiteCommand("CREATE VIRTUAL TABLE IF NOT EXISTS mail_fts USING fts5(account_id UNINDEXED, folder UNINDEXED, uid UNINDEXED, subject, from_line, to_line, cc_line, body, tokenize='porter unicode61');", sQLiteConnection);
		try
		{
			sQLiteCommand.ExecuteNonQuery();
		}
		finally
		{
			((IDisposable)(object)sQLiteCommand)?.Dispose();
		}
		EnsureMetaSchema(sQLiteConnection);
	}

	private static void EnsureMetaSchema(SQLiteConnection conn)
	{
		if (conn == null)
		{
			return;
		}
		SQLiteCommand sQLiteCommand = new SQLiteCommand("CREATE TABLE IF NOT EXISTS mail_fts_meta (rowid INTEGER PRIMARY KEY, date_unix INTEGER NOT NULL DEFAULT 0);", conn);
		try
		{
			sQLiteCommand.ExecuteNonQuery();
		}
		finally
		{
			((IDisposable)(object)sQLiteCommand)?.Dispose();
		}
	}

	private static bool FtsDefinitionIncludesToLine(SQLiteConnection conn)
	{
		bool result;
		try
		{
			SQLiteCommand sQLiteCommand = new SQLiteCommand("SELECT sql FROM sqlite_master WHERE type='table' AND name='mail_fts';", conn);
			try
			{
				object objectValue = RuntimeHelpers.GetObjectValue(sQLiteCommand.ExecuteScalar());
				result = objectValue != null && objectValue != DBNull.Value && objectValue.ToString().IndexOf("to_line", StringComparison.OrdinalIgnoreCase) >= 0;
			}
			finally
			{
				((IDisposable)(object)sQLiteCommand)?.Dispose();
			}
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			result = false;
			ProjectData.ClearProjectError();
		}
		return result;
	}

	private static void TryMigrateFtsSchemaIfNeeded(SQLiteConnection conn)
	{
		if (conn == null || FtsDefinitionIncludesToLine(conn))
		{
			return;
		}
		bool flag = false;
		try
		{
			SQLiteCommand sQLiteCommand = new SQLiteCommand("SELECT 1 FROM mail_fts LIMIT 1;", conn);
			try
			{
				object objectValue = RuntimeHelpers.GetObjectValue(sQLiteCommand.ExecuteScalar());
				flag = ((objectValue != null && objectValue != DBNull.Value) ? true : false);
			}
			finally
			{
				((IDisposable)(object)sQLiteCommand)?.Dispose();
			}
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			flag = false;
			ProjectData.ClearProjectError();
		}
		if (!flag)
		{
			return;
		}
		SQLiteCommand sQLiteCommand2 = new SQLiteCommand("CREATE VIRTUAL TABLE mail_fts_v322 USING fts5(account_id UNINDEXED, folder UNINDEXED, uid UNINDEXED, subject, from_line, to_line, cc_line, body, tokenize='porter unicode61');", conn);
		try
		{
			sQLiteCommand2.ExecuteNonQuery();
		}
		finally
		{
			((IDisposable)(object)sQLiteCommand2)?.Dispose();
		}
		SQLiteCommand sQLiteCommand3 = new SQLiteCommand("INSERT INTO mail_fts_v322(rowid, account_id, folder, uid, subject, from_line, to_line, cc_line, body) SELECT rowid, account_id, folder, uid, subject, from_line, '', '', body FROM mail_fts;", conn);
		try
		{
			sQLiteCommand3.ExecuteNonQuery();
		}
		finally
		{
			((IDisposable)(object)sQLiteCommand3)?.Dispose();
		}
		SQLiteCommand sQLiteCommand4 = new SQLiteCommand("DROP TABLE mail_fts;", conn);
		try
		{
			sQLiteCommand4.ExecuteNonQuery();
		}
		finally
		{
			((IDisposable)(object)sQLiteCommand4)?.Dispose();
		}
		SQLiteCommand sQLiteCommand5 = new SQLiteCommand("CREATE VIRTUAL TABLE IF NOT EXISTS mail_fts USING fts5(account_id UNINDEXED, folder UNINDEXED, uid UNINDEXED, subject, from_line, to_line, cc_line, body, tokenize='porter unicode61');", conn);
		try
		{
			sQLiteCommand5.ExecuteNonQuery();
		}
		finally
		{
			((IDisposable)(object)sQLiteCommand5)?.Dispose();
		}
		SQLiteCommand sQLiteCommand6 = new SQLiteCommand("INSERT INTO mail_fts(rowid, account_id, folder, uid, subject, from_line, to_line, cc_line, body) SELECT rowid, account_id, folder, uid, subject, from_line, to_line, cc_line, body FROM mail_fts_v322;", conn);
		try
		{
			sQLiteCommand6.ExecuteNonQuery();
		}
		finally
		{
			((IDisposable)(object)sQLiteCommand6)?.Dispose();
		}
		SQLiteCommand sQLiteCommand7 = new SQLiteCommand("DROP TABLE mail_fts_v322;", conn);
		try
		{
			sQLiteCommand7.ExecuteNonQuery();
		}
		finally
		{
			((IDisposable)(object)sQLiteCommand7)?.Dispose();
		}
	}

	private static void ApplySearchPragmas(SQLiteConnection conn)
	{
		if (conn == null)
		{
			return;
		}
		try
		{
			SQLiteCommand sQLiteCommand = new SQLiteCommand("PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL; PRAGMA cache_size=-64000; PRAGMA mmap_size=268435456; PRAGMA temp_store=MEMORY;", conn);
			try
			{
				sQLiteCommand.ExecuteNonQuery();
			}
			finally
			{
				((IDisposable)(object)sQLiteCommand)?.Dispose();
			}
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
	}

	public static void UpsertMessage(string accountId, string folder, ulong uid, string subject, string fromLine, string bodyText, string toLine = null, string ccLine = null, DateTime dateUtc = default(DateTime))
	{
		if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(folder))
		{
			return;
		}
		EnsureSchema();
		long num = StableRowId(accountId, folder, uid);
		using SQLiteConnection sQLiteConnection = new SQLiteConnection("Data Source=" + DbPath() + ";");
		sQLiteConnection.Open();
		ApplySearchPragmas(sQLiteConnection);
		SQLiteCommand sQLiteCommand = new SQLiteCommand("DELETE FROM mail_fts WHERE rowid=@id;", sQLiteConnection);
		try
		{
			sQLiteCommand.Parameters.AddWithValue("@id", num);
			sQLiteCommand.ExecuteNonQuery();
		}
		finally
		{
			((IDisposable)(object)sQLiteCommand)?.Dispose();
		}
		SQLiteCommand sQLiteCommand2 = new SQLiteCommand("INSERT INTO mail_fts(rowid, account_id, folder, uid, subject, from_line, to_line, cc_line, body) VALUES (@id, @acct, @folder, @uid, @subj, @froml, @tol, @ccl, @body);", sQLiteConnection);
		try
		{
			sQLiteCommand2.Parameters.AddWithValue("@id", num);
			sQLiteCommand2.Parameters.AddWithValue("@acct", accountId);
			sQLiteCommand2.Parameters.AddWithValue("@folder", folder);
			sQLiteCommand2.Parameters.AddWithValue("@uid", uid.ToString());
			sQLiteCommand2.Parameters.AddWithValue("@subj", (subject != null) ? subject : "");
			sQLiteCommand2.Parameters.AddWithValue("@froml", (fromLine != null) ? fromLine : "");
			sQLiteCommand2.Parameters.AddWithValue("@tol", (toLine != null) ? toLine : "");
			sQLiteCommand2.Parameters.AddWithValue("@ccl", (ccLine != null) ? ccLine : "");
			string text = ((bodyText != null) ? bodyText : "");
			if (text.Length > 256000)
			{
				text = text.Substring(0, 256000);
			}
			sQLiteCommand2.Parameters.AddWithValue("@body", text);
			sQLiteCommand2.ExecuteNonQuery();
		}
		finally
		{
			((IDisposable)(object)sQLiteCommand2)?.Dispose();
		}
		UpsertMetaDate(sQLiteConnection, num, dateUtc);
	}

	private static void UpsertMetaDate(SQLiteConnection conn, long rowId, DateTime dateUtc)
	{
		if (conn == null)
		{
			return;
		}
		EnsureMetaSchema(conn);
		long num = 0L;
		if (DateTime.Compare(dateUtc, DateTime.MinValue) != 0)
		{
			DateTime dateTime = dateUtc.ToUniversalTime();
			DateTime dateTime2 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			num = checked((long)Math.Round((dateTime - dateTime2).TotalSeconds));
		}
		SQLiteCommand sQLiteCommand = new SQLiteCommand("INSERT INTO mail_fts_meta(rowid, date_unix) VALUES (@id, @du) ON CONFLICT(rowid) DO UPDATE SET date_unix=@du;", conn);
		try
		{
			sQLiteCommand.Parameters.AddWithValue("@id", rowId);
			sQLiteCommand.Parameters.AddWithValue("@du", num);
			sQLiteCommand.ExecuteNonQuery();
		}
		finally
		{
			((IDisposable)(object)sQLiteCommand)?.Dispose();
		}
	}

	public static void RemoveMessage(string accountId, string folder, ulong uid)
	{
		if (string.IsNullOrWhiteSpace(accountId))
		{
			return;
		}
		EnsureSchema();
		long num = StableRowId(accountId, folder, uid);
		using SQLiteConnection sQLiteConnection = new SQLiteConnection("Data Source=" + DbPath() + ";");
		sQLiteConnection.Open();
		SQLiteCommand sQLiteCommand = new SQLiteCommand("DELETE FROM mail_fts WHERE rowid=@id;", sQLiteConnection);
		try
		{
			sQLiteCommand.Parameters.AddWithValue("@id", num);
			sQLiteCommand.ExecuteNonQuery();
		}
		finally
		{
			((IDisposable)(object)sQLiteCommand)?.Dispose();
		}
		SQLiteCommand sQLiteCommand2 = new SQLiteCommand("DELETE FROM mail_fts_meta WHERE rowid=@id;", sQLiteConnection);
		try
		{
			sQLiteCommand2.Parameters.AddWithValue("@id", num);
			sQLiteCommand2.ExecuteNonQuery();
		}
		finally
		{
			((IDisposable)(object)sQLiteCommand2)?.Dispose();
		}
	}

	public static int CountIndexedMessages(string accountId)
	{
		int result;
		if (string.IsNullOrWhiteSpace(accountId))
		{
			result = 0;
		}
		else if (!File.Exists(DbPath()))
		{
			result = 0;
		}
		else
		{
			EnsureSchema();
			try
			{
				using SQLiteConnection sQLiteConnection = new SQLiteConnection("Data Source=" + DbPath() + ";");
				sQLiteConnection.Open();
				SQLiteCommand sQLiteCommand = new SQLiteCommand("SELECT COUNT(*) FROM mail_fts WHERE account_id=@acct;", sQLiteConnection);
				try
				{
					sQLiteCommand.Parameters.AddWithValue("@acct", accountId);
					object objectValue = RuntimeHelpers.GetObjectValue(sQLiteCommand.ExecuteScalar());
					result = ((objectValue != null && objectValue != DBNull.Value) ? Convert.ToInt32(RuntimeHelpers.GetObjectValue(objectValue)) : 0);
				}
				finally
				{
					((IDisposable)(object)sQLiteCommand)?.Dispose();
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = 0;
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}

	public static int CountMessagesWithBody(string accountId)
	{
		int result;
		if (string.IsNullOrWhiteSpace(accountId))
		{
			result = 0;
		}
		else if (!File.Exists(DbPath()))
		{
			result = 0;
		}
		else
		{
			EnsureSchema();
			try
			{
				using SQLiteConnection sQLiteConnection = new SQLiteConnection("Data Source=" + DbPath() + ";");
				sQLiteConnection.Open();
				SQLiteCommand sQLiteCommand = new SQLiteCommand("SELECT COUNT(*) FROM mail_fts WHERE account_id=@acct AND length(trim(body)) > 0;", sQLiteConnection);
				try
				{
					sQLiteCommand.Parameters.AddWithValue("@acct", accountId);
					object objectValue = RuntimeHelpers.GetObjectValue(sQLiteCommand.ExecuteScalar());
					result = ((objectValue != null && objectValue != DBNull.Value) ? Convert.ToInt32(RuntimeHelpers.GetObjectValue(objectValue)) : 0);
				}
				finally
				{
					((IDisposable)(object)sQLiteCommand)?.Dispose();
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = 0;
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}

	public static List<MailMessageHeader> Search(string accountId, string queryText, int maxHits = 12000, int skip = 0)
	{
		List<MailMessageHeader> list = new List<MailMessageHeader>();
		List<MailMessageHeader> result;
		checked
		{
			if (string.IsNullOrWhiteSpace(accountId))
			{
				result = list;
			}
			else
			{
				string queryText2 = ((queryText != null) ? queryText : "").Trim();
				if (queryText2.Length == 0)
				{
					result = list;
				}
				else if (!File.Exists(DbPath()))
				{
					result = list;
				}
				else
				{
					string text = ExtractFolderFilter(ref queryText2);
					int num = ExtractNewerDaysFromQuery(ref queryText2);
					int num2 = ExtractOlderDaysFromQuery(ref queryText2);
					ExtractLargerFromQuery(ref queryText2);
					ExtractSmallerFromQuery(ref queryText2);
					ExtractUnreadFromQuery(ref queryText2);
					ExtractReadFromQuery(ref queryText2);
					bool flag = ExtractHasBodyFromQuery(ref queryText2);
					ExtractToFromQuery(ref queryText2);
					ExtractCcFromQuery(ref queryText2);
					ExtractMessageIdFromQuery(ref queryText2);
					ExtractInReplyToFromQuery(ref queryText2);
					ExtractReferencesFromQuery(ref queryText2);
					ExtractReplyToFromQuery(ref queryText2);
					ExtractListIdFromQuery(ref queryText2);
					ExtractReturnPathFromQuery(ref queryText2);
					string text2 = BuildFtsMatchQuery(queryText2);
					if (text2.Length == 0)
					{
						result = list;
					}
					else
					{
						EnsureSchema();
						if (num > 0 || num2 > 0)
						{
							MailSearchIndex.BackfillGlodaFtsMetaDates(accountId, 8000);
						}
						long num3 = 0L;
						long num4 = 0L;
						if (num > 0)
						{
							DateTime dateTime = DateTime.UtcNow.AddDays(-num);
							DateTime dateTime2 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
							num3 = (long)Math.Round((dateTime - dateTime2).TotalSeconds);
						}
						if (num2 > 0)
						{
							DateTime dateTime3 = DateTime.UtcNow.AddDays(-num2);
							DateTime dateTime4 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
							num4 = (long)Math.Round((dateTime3 - dateTime4).TotalSeconds);
						}
						try
						{
							using SQLiteConnection sQLiteConnection = new SQLiteConnection("Data Source=" + DbPath() + ";");
							sQLiteConnection.Open();
							ApplySearchPragmas(sQLiteConnection);
							bool flag2 = ((num3 > 0 || num4 > 0) ? true : false);
							string text3 = "SELECT f.folder, f.uid, f.subject, f.from_line, bm25(f) AS rank FROM mail_fts f ";
							if (flag2)
							{
								text3 += "LEFT JOIN mail_fts_meta m ON m.rowid = f.rowid ";
							}
							text3 += "WHERE f.account_id=@acct AND f MATCH @match ";
							if (text.Length > 0)
							{
								text3 += "AND f.folder LIKE @folder ESCAPE '\\' ";
							}
							if (num3 > 0)
							{
								text3 += "AND (IFNULL(m.date_unix, 0) = 0 OR m.date_unix >= @newerCut) ";
							}
							if (num4 > 0)
							{
								text3 += "AND (IFNULL(m.date_unix, 0) = 0 OR m.date_unix <= @olderCut) ";
							}
							if (flag)
							{
								text3 += "AND length(trim(f.body)) > 0 ";
							}
							text3 += "ORDER BY rank LIMIT @lim OFFSET @skip;";
							SQLiteCommand sQLiteCommand = new SQLiteCommand(text3, sQLiteConnection);
							try
							{
								sQLiteCommand.Parameters.AddWithValue("@acct", accountId);
								sQLiteCommand.Parameters.AddWithValue("@match", text2);
								if (text.Length > 0)
								{
									sQLiteCommand.Parameters.AddWithValue("@folder", "%" + text + "%");
								}
								if (num3 > 0)
								{
									sQLiteCommand.Parameters.AddWithValue("@newerCut", num3);
								}
								if (num4 > 0)
								{
									sQLiteCommand.Parameters.AddWithValue("@olderCut", num4);
								}
								sQLiteCommand.Parameters.AddWithValue("@lim", Math.Max(1, maxHits));
								sQLiteCommand.Parameters.AddWithValue("@skip", Math.Max(0, skip));
								SQLiteDataReader sQLiteDataReader = sQLiteCommand.ExecuteReader();
								try
								{
									while (sQLiteDataReader.Read())
									{
										string folder = (sQLiteDataReader.IsDBNull(0) ? "" : sQLiteDataReader.GetString(0));
										string s = (sQLiteDataReader.IsDBNull(1) ? "0" : sQLiteDataReader.GetString(1));
										ulong result2 = 0uL;
										ulong.TryParse(s, out result2);
										string subject = (sQLiteDataReader.IsDBNull(2) ? "" : sQLiteDataReader.GetString(2));
										string text4 = (sQLiteDataReader.IsDBNull(3) ? "" : sQLiteDataReader.GetString(3));
										double ftsRank = 0.0;
										if (!sQLiteDataReader.IsDBNull(4))
										{
											ftsRank = Convert.ToDouble(RuntimeHelpers.GetObjectValue(sQLiteDataReader.GetValue(4)));
										}
										DateTime dateUtc = MailSearchIndex.TryGetIndexedDateUtc(accountId, folder, result2);
										list.Add(new MailMessageHeader
										{
											Folder = folder,
											Uid = result2,
											Subject = subject,
											From = text4,
											DateUtc = dateUtc,
											FtsRank = ftsRank
										});
									}
								}
								finally
								{
									IDisposable disposable = sQLiteDataReader as IDisposable;
									if (disposable != null)
									{
										disposable.Dispose();
									}
								}
							}
							finally
							{
								((IDisposable)(object)sQLiteCommand)?.Dispose();
							}
						}
						catch (Exception projectError)
						{
							ProjectData.SetProjectError(projectError);
							result = new List<MailMessageHeader>();
							ProjectData.ClearProjectError();
							goto IL_04a9;
						}
						result = ApplyDateFacetFilters(list, num, num2);
					}
				}
			}
			goto IL_04a9;
		}
		IL_04a9:
		return result;
	}

	private static List<MailMessageHeader> ApplyDateFacetFilters(List<MailMessageHeader> hits, int newerDays, int olderDays)
	{
		if (hits == null || hits.Count == 0)
		{
			return hits;
		}
		if (newerDays <= 0 && olderDays <= 0)
		{
			return hits;
		}
		List<MailMessageHeader> list = new List<MailMessageHeader>();
		foreach (MailMessageHeader hit in hits)
		{
			if (checked(hit != null && (newerDays <= 0 || DateTime.Compare(hit.DateUtc, DateTime.MinValue) == 0 || DateTime.Compare(hit.DateUtc, DateTime.UtcNow.AddDays(-newerDays)) >= 0) && (olderDays <= 0 || DateTime.Compare(hit.DateUtc, DateTime.MinValue) == 0 || DateTime.Compare(hit.DateUtc, DateTime.UtcNow.AddDays(-olderDays)) <= 0)))
			{
				list.Add(hit);
			}
		}
		return list;
	}

	public static List<MailMessageHeader> ApplyThreadAwareRanking(List<MailMessageHeader> hits)
	{
		if (hits == null || hits.Count <= 1)
		{
			return hits;
		}
		List<MailThreadListItem> list = MailConversationGrouper.BuildThreadList(hits);
		if (list == null || list.Count == 0)
		{
			return hits;
		}
		List<MailMessageHeader> list2 = new List<MailMessageHeader>();
		foreach (MailThreadListItem item in list)
		{
			if (item == null || item.Messages == null || item.Messages.Count == 0)
			{
				continue;
			}
			MailMessageHeader mailMessageHeader = item.Representative;
			double num = mailMessageHeader?.FtsRank ?? 0.0;
			foreach (MailMessageHeader message in item.Messages)
			{
				if (message != null && (num == 0.0 || (message.FtsRank != 0.0 && message.FtsRank < num)))
				{
					num = message.FtsRank;
					mailMessageHeader = message;
				}
			}
			if (mailMessageHeader != null)
			{
				if (item.MessageCount > 1 && num != 0.0)
				{
					mailMessageHeader.FtsRank = num - Math.Log(Math.Max(2, item.MessageCount)) * 0.15;
				}
				list2.Add(mailMessageHeader);
			}
		}
		list2.Sort([SpecialName] (MailMessageHeader a, MailMessageHeader b) =>
		{
			double num2 = a?.FtsRank ?? 0.0;
			double num3 = b?.FtsRank ?? 0.0;
			if (num2 == 0.0 && num3 == 0.0)
			{
				return 0;
			}
			if (num2 == 0.0)
			{
				return 1;
			}
			return (num3 == 0.0) ? (-1) : num2.CompareTo(num3);
		});
		return list2;
	}

	public static string TryGetBodySnippetForMessage(string accountId, string folder, ulong uid)
	{
		string result;
		if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(folder))
		{
			result = "";
		}
		else if (!File.Exists(DbPath()))
		{
			result = "";
		}
		else
		{
			EnsureSchema();
			long num = StableRowId(accountId, folder, uid);
			try
			{
				using SQLiteConnection sQLiteConnection = new SQLiteConnection("Data Source=" + DbPath() + ";");
				sQLiteConnection.Open();
				SQLiteCommand sQLiteCommand = new SQLiteCommand("SELECT snippet(mail_fts, 4, '[', ']', '…', 12) FROM mail_fts WHERE rowid=@id AND account_id=@acct;", sQLiteConnection);
				try
				{
					sQLiteCommand.Parameters.AddWithValue("@id", num);
					sQLiteCommand.Parameters.AddWithValue("@acct", accountId);
					object objectValue = RuntimeHelpers.GetObjectValue(sQLiteCommand.ExecuteScalar());
					result = ((objectValue != null && objectValue != DBNull.Value) ? objectValue.ToString().Trim() : "");
				}
				finally
				{
					((IDisposable)(object)sQLiteCommand)?.Dispose();
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = "";
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}

	internal static string BuildFtsMatchQuery(string userQuery)
	{
		List<string[]> list = MailSearchIndex.ParseGlodaOrGroups(userQuery);
		if (list == null || list.Count == 0)
		{
			return "";
		}
		List<string> list2 = new List<string>();
		foreach (string[] item in list)
		{
			if (item == null || item.Length == 0)
			{
				continue;
			}
			List<string> list3 = new List<string>();
			string[] array = item;
			foreach (string rawTerm in array)
			{
				string text = MapFacetTerm(rawTerm);
				if (text.Length != 0)
				{
					list3.Add(text);
				}
			}
			if (list3.Count != 0)
			{
				list2.Add("(" + string.Join(" AND ", list3) + ")");
			}
		}
		if (list2.Count == 0)
		{
			return "";
		}
		return string.Join(" OR ", list2);
	}

	private static string MapFacetTerm(string rawTerm)
	{
		string text = ((rawTerm != null) ? rawTerm : "").Trim();
		if (text.Length == 0)
		{
			return "";
		}
		if (text.StartsWith("-") && text.Length > 1)
		{
			string text2 = MapFacetTerm(text.Substring(1));
			if (text2.Length == 0)
			{
				return "";
			}
			return "NOT (" + text2 + ")";
		}
		string text3 = text.ToLowerInvariant();
		if (text3.StartsWith("from:", StringComparison.Ordinal))
		{
			return "from_line:" + EscapeFtsTerm(text.Substring(5));
		}
		if (text3.StartsWith("to:", StringComparison.Ordinal))
		{
			return "to_line:" + EscapeFtsTerm(text.Substring(3));
		}
		if (text3.StartsWith("cc:", StringComparison.Ordinal))
		{
			return "cc_line:" + EscapeFtsTerm(text.Substring(3));
		}
		if (text3.StartsWith("messageid:", StringComparison.Ordinal))
		{
			string text4 = text.Substring(10).Trim();
			if (text4.Length == 0)
			{
				return "";
			}
			return "body:" + EscapeFtsTerm("Message-Id: " + text4);
		}
		if (text3.StartsWith("inreplyto:", StringComparison.Ordinal))
		{
			string text5 = text.Substring(10).Trim();
			if (text5.Length == 0)
			{
				return "";
			}
			return "body:" + EscapeFtsTerm("In-Reply-To: " + text5);
		}
		if (text3.StartsWith("references:", StringComparison.Ordinal))
		{
			string text6 = text.Substring(11).Trim();
			if (text6.Length == 0)
			{
				return "";
			}
			return "body:" + EscapeFtsTerm("References: " + text6);
		}
		if (text3.StartsWith("replyto:", StringComparison.Ordinal))
		{
			string text7 = text.Substring(8).Trim();
			if (text7.Length == 0)
			{
				return "";
			}
			return "body:" + EscapeFtsTerm("Reply-To: " + text7);
		}
		if (text3.StartsWith("list-id:", StringComparison.Ordinal))
		{
			string text8 = text.Substring(8).Trim();
			if (text8.Length == 0)
			{
				return "";
			}
			return "body:" + EscapeFtsTerm("List-Id: " + text8);
		}
		if (text3.StartsWith("return-path:", StringComparison.Ordinal))
		{
			string text9 = text.Substring(12).Trim();
			if (text9.Length == 0)
			{
				return "";
			}
			return "body:" + EscapeFtsTerm("Return-Path: " + text9);
		}
		if (text3.StartsWith("label:", StringComparison.Ordinal) || text3.StartsWith("tag:", StringComparison.Ordinal))
		{
			int num = text.IndexOf(':');
			string text10 = ((num >= 0) ? text.Substring(checked(num + 1)) : "").Trim();
			if (text10.Length == 0)
			{
				return "";
			}
			string text11 = EscapeFtsTerm(text10);
			return "(subject:" + text11 + " OR body:" + text11 + ")";
		}
		if (text3.StartsWith("subject:", StringComparison.Ordinal))
		{
			return "subject:" + EscapeFtsTerm(text.Substring(8));
		}
		if (text3.StartsWith("body:", StringComparison.Ordinal))
		{
			return "body:" + EscapeFtsTerm(text.Substring(5));
		}
		if (text3.StartsWith("folder:", StringComparison.Ordinal))
		{
			return "";
		}
		if (text3.StartsWith("newer:", StringComparison.Ordinal))
		{
			return "";
		}
		if (text3.StartsWith("older:", StringComparison.Ordinal))
		{
			return "";
		}
		if (text3.StartsWith("larger:", StringComparison.Ordinal))
		{
			return "";
		}
		if (text3.StartsWith("smaller:", StringComparison.Ordinal))
		{
			return "";
		}
		if (text3.Equals("unread:1", StringComparison.Ordinal) || text3.Equals("unread:true", StringComparison.Ordinal) || text3.Equals("is:unread", StringComparison.Ordinal))
		{
			return "";
		}
		if (text3.Equals("read:1", StringComparison.Ordinal) || text3.Equals("read:true", StringComparison.Ordinal) || text3.Equals("is:read", StringComparison.Ordinal))
		{
			return "";
		}
		if (text3.Equals("has:body", StringComparison.Ordinal) || text3.Equals("body:yes", StringComparison.Ordinal))
		{
			return "";
		}
		return EscapeFtsTerm(text);
	}

	private static int ExtractNewerDaysFromQuery(ref string queryText)
	{
		string text = ((queryText != null) ? queryText : "").Trim();
		if (text.Length == 0)
		{
			return 0;
		}
		string[] array = text.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		List<string> list = new List<string>();
		int result = 0;
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			if (text2.StartsWith("newer:", StringComparison.OrdinalIgnoreCase))
			{
				string text3 = text2.Substring(6).Trim();
				if (text3.EndsWith("d", StringComparison.OrdinalIgnoreCase))
				{
					text3 = text3.Substring(0, checked(text3.Length - 1));
				}
				int result2 = 0;
				if (int.TryParse(text3, out result2) && result2 > 0)
				{
					result = result2;
				}
			}
			else
			{
				list.Add(text2);
			}
		}
		queryText = string.Join(" ", list);
		return result;
	}

	private static void ExtractLargerFromQuery(ref string queryText)
	{
		string text = ((queryText != null) ? queryText : "").Trim();
		if (text.Length == 0)
		{
			return;
		}
		string[] array = text.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		List<string> list = new List<string>();
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			if (!text2.StartsWith("larger:", StringComparison.OrdinalIgnoreCase))
			{
				list.Add(text2);
			}
		}
		queryText = string.Join(" ", list);
	}

	private static void ExtractSmallerFromQuery(ref string queryText)
	{
		string text = ((queryText != null) ? queryText : "").Trim();
		if (text.Length == 0)
		{
			return;
		}
		string[] array = text.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		List<string> list = new List<string>();
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			if (!text2.StartsWith("smaller:", StringComparison.OrdinalIgnoreCase))
			{
				list.Add(text2);
			}
		}
		queryText = string.Join(" ", list);
	}

	private static int ExtractOlderDaysFromQuery(ref string queryText)
	{
		string text = ((queryText != null) ? queryText : "").Trim();
		if (text.Length == 0)
		{
			return 0;
		}
		string[] array = text.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		List<string> list = new List<string>();
		int result = 0;
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			if (text2.StartsWith("older:", StringComparison.OrdinalIgnoreCase))
			{
				string text3 = text2.Substring(6).Trim();
				if (text3.EndsWith("d", StringComparison.OrdinalIgnoreCase))
				{
					text3 = text3.Substring(0, checked(text3.Length - 1));
				}
				int result2 = 0;
				if (int.TryParse(text3, out result2) && result2 > 0)
				{
					result = result2;
				}
			}
			else
			{
				list.Add(text2);
			}
		}
		queryText = string.Join(" ", list);
		return result;
	}

	private static void ExtractToFromQuery(ref string queryText)
	{
		string text = ((queryText != null) ? queryText : "").Trim();
		if (text.Length == 0)
		{
			return;
		}
		string[] array = text.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		List<string> list = new List<string>();
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			if (!text2.StartsWith("to:", StringComparison.OrdinalIgnoreCase))
			{
				list.Add(text2);
			}
		}
		queryText = string.Join(" ", list);
	}

	private static void ExtractCcFromQuery(ref string queryText)
	{
		string text = ((queryText != null) ? queryText : "").Trim();
		if (text.Length == 0)
		{
			return;
		}
		string[] array = text.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		List<string> list = new List<string>();
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			if (!text2.StartsWith("cc:", StringComparison.OrdinalIgnoreCase))
			{
				list.Add(text2);
			}
		}
		queryText = string.Join(" ", list);
	}

	private static void ExtractMessageIdFromQuery(ref string queryText)
	{
		string text = ((queryText != null) ? queryText : "").Trim();
		if (text.Length == 0)
		{
			return;
		}
		string[] array = text.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		List<string> list = new List<string>();
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			if (!text2.StartsWith("messageid:", StringComparison.OrdinalIgnoreCase))
			{
				list.Add(text2);
			}
		}
		queryText = string.Join(" ", list);
	}

	private static void ExtractInReplyToFromQuery(ref string queryText)
	{
		string text = ((queryText != null) ? queryText : "").Trim();
		if (text.Length == 0)
		{
			return;
		}
		string[] array = text.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		List<string> list = new List<string>();
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			if (!text2.StartsWith("inreplyto:", StringComparison.OrdinalIgnoreCase))
			{
				list.Add(text2);
			}
		}
		queryText = string.Join(" ", list);
	}

	private static void ExtractReferencesFromQuery(ref string queryText)
	{
		string text = ((queryText != null) ? queryText : "").Trim();
		if (text.Length == 0)
		{
			return;
		}
		string[] array = text.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		List<string> list = new List<string>();
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			if (!text2.StartsWith("references:", StringComparison.OrdinalIgnoreCase))
			{
				list.Add(text2);
			}
		}
		queryText = string.Join(" ", list);
	}

	private static void ExtractReplyToFromQuery(ref string queryText)
	{
		string text = ((queryText != null) ? queryText : "").Trim();
		if (text.Length == 0)
		{
			return;
		}
		string[] array = text.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		List<string> list = new List<string>();
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			if (!text2.StartsWith("replyto:", StringComparison.OrdinalIgnoreCase))
			{
				list.Add(text2);
			}
		}
		queryText = string.Join(" ", list);
	}

	private static void ExtractListIdFromQuery(ref string queryText)
	{
		string text = ((queryText != null) ? queryText : "").Trim();
		if (text.Length == 0)
		{
			return;
		}
		string[] array = text.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		List<string> list = new List<string>();
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			if (!text2.StartsWith("list-id:", StringComparison.OrdinalIgnoreCase))
			{
				list.Add(text2);
			}
		}
		queryText = string.Join(" ", list);
	}

	private static void ExtractReturnPathFromQuery(ref string queryText)
	{
		string text = ((queryText != null) ? queryText : "").Trim();
		if (text.Length == 0)
		{
			return;
		}
		string[] array = text.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		List<string> list = new List<string>();
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			if (!text2.StartsWith("return-path:", StringComparison.OrdinalIgnoreCase))
			{
				list.Add(text2);
			}
		}
		queryText = string.Join(" ", list);
	}

	private static bool ExtractHasBodyFromQuery(ref string queryText)
	{
		string text = ((queryText != null) ? queryText : "").Trim();
		if (text.Length == 0)
		{
			return false;
		}
		string[] array = text.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		List<string> list = new List<string>();
		bool result = false;
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			if (text2.Equals("has:body", StringComparison.OrdinalIgnoreCase) || text2.Equals("body:yes", StringComparison.OrdinalIgnoreCase))
			{
				result = true;
			}
			else
			{
				list.Add(text2);
			}
		}
		queryText = string.Join(" ", list);
		return result;
	}

	public static void TrySetMetaDateUtc(string accountId, string folder, ulong uid, DateTime dateUtc)
	{
		if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(folder) || DateTime.Compare(dateUtc, DateTime.MinValue) == 0)
		{
			return;
		}
		EnsureSchema();
		long rowId = StableRowId(accountId, folder, uid);
		using SQLiteConnection sQLiteConnection = new SQLiteConnection("Data Source=" + DbPath() + ";");
		sQLiteConnection.Open();
		UpsertMetaDate(sQLiteConnection, rowId, dateUtc);
	}

	private static void ExtractUnreadFromQuery(ref string queryText)
	{
		string text = ((queryText != null) ? queryText : "").Trim();
		if (text.Length == 0)
		{
			return;
		}
		string[] array = text.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		List<string> list = new List<string>();
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			if (!text2.Equals("unread:1", StringComparison.OrdinalIgnoreCase) && !text2.Equals("unread:true", StringComparison.OrdinalIgnoreCase) && !text2.Equals("is:unread", StringComparison.OrdinalIgnoreCase))
			{
				list.Add(text2);
			}
		}
		queryText = string.Join(" ", list);
	}

	private static void ExtractReadFromQuery(ref string queryText)
	{
		string text = ((queryText != null) ? queryText : "").Trim();
		if (text.Length == 0)
		{
			return;
		}
		string[] array = text.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		List<string> list = new List<string>();
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			if (!text2.Equals("read:1", StringComparison.OrdinalIgnoreCase) && !text2.Equals("read:true", StringComparison.OrdinalIgnoreCase) && !text2.Equals("is:read", StringComparison.OrdinalIgnoreCase))
			{
				list.Add(text2);
			}
		}
		queryText = string.Join(" ", list);
	}

	private static string ExtractFolderFilter(ref string queryText)
	{
		string text = ((queryText != null) ? queryText : "").Trim();
		if (text.Length == 0)
		{
			return "";
		}
		string[] array = text.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		List<string> list = new List<string>();
		string result = "";
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			if (text2.StartsWith("folder:", StringComparison.OrdinalIgnoreCase))
			{
				result = text2.Substring(7).Trim().Replace("\\", "\\\\")
					.Replace("%", "\\%")
					.Replace("_", "\\_");
			}
			else
			{
				list.Add(text2);
			}
		}
		queryText = string.Join(" ", list);
		return result;
	}

	private static string EscapeFtsTerm(string raw)
	{
		string text = ((raw != null) ? raw : "").Trim().ToLowerInvariant();
		if (text.Length == 0)
		{
			return "";
		}
		text = text.Replace("\"", "\"\"");
		string text2 = text;
		int i = 0;
		for (int length = text2.Length; i < length; i = checked(i + 1))
		{
			char c = text2[i];
			if (!char.IsLetterOrDigit(c) && c != '@' && c != '.' && c != '_' && c != '-')
			{
				return "\"" + text + "\"";
			}
		}
		return text;
	}

	public static GlodaCorpusStats TryGetCorpusStats(string accountId = null)
	{
		GlodaCorpusStats glodaCorpusStats = new GlodaCorpusStats();
		if (!File.Exists(DbPath()))
		{
			return glodaCorpusStats;
		}
		EnsureSchema();
		try
		{
			using SQLiteConnection sQLiteConnection = new SQLiteConnection("Data Source=" + DbPath() + ";");
			sQLiteConnection.Open();
			if (string.IsNullOrWhiteSpace(accountId))
			{
				SQLiteCommand sQLiteCommand = new SQLiteCommand("SELECT COUNT(DISTINCT account_id) FROM mail_fts;", sQLiteConnection);
				try
				{
					object objectValue = RuntimeHelpers.GetObjectValue(sQLiteCommand.ExecuteScalar());
					if (objectValue != null && objectValue != DBNull.Value)
					{
						glodaCorpusStats.DistinctAccounts = Convert.ToInt32(RuntimeHelpers.GetObjectValue(objectValue));
					}
				}
				finally
				{
					((IDisposable)(object)sQLiteCommand)?.Dispose();
				}
				SQLiteCommand sQLiteCommand2 = new SQLiteCommand("SELECT COUNT(*) FROM mail_fts;", sQLiteConnection);
				try
				{
					object objectValue2 = RuntimeHelpers.GetObjectValue(sQLiteCommand2.ExecuteScalar());
					if (objectValue2 != null && objectValue2 != DBNull.Value)
					{
						glodaCorpusStats.TotalFtsRows = Convert.ToInt32(RuntimeHelpers.GetObjectValue(objectValue2));
					}
				}
				finally
				{
					((IDisposable)(object)sQLiteCommand2)?.Dispose();
				}
				SQLiteCommand sQLiteCommand3 = new SQLiteCommand("SELECT COUNT(*) FROM mail_fts WHERE length(trim(body)) > 0;", sQLiteConnection);
				try
				{
					object objectValue3 = RuntimeHelpers.GetObjectValue(sQLiteCommand3.ExecuteScalar());
					if (objectValue3 != null && objectValue3 != DBNull.Value)
					{
						glodaCorpusStats.WithBodyRows = Convert.ToInt32(RuntimeHelpers.GetObjectValue(objectValue3));
					}
				}
				finally
				{
					((IDisposable)(object)sQLiteCommand3)?.Dispose();
				}
			}
			else
			{
				glodaCorpusStats.DistinctAccounts = 1;
				glodaCorpusStats.TotalFtsRows = CountIndexedMessages(accountId);
				glodaCorpusStats.WithBodyRows = CountMessagesWithBody(accountId);
			}
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
		return glodaCorpusStats;
	}

	private static long StableRowId(string accountId, string folder, ulong uid)
	{
		string s = accountId + "|" + ((folder != null) ? folder : "") + "|" + uid;
		ulong num = 14695981039346656037uL;
		byte[] bytes = Encoding.UTF8.GetBytes(s);
		checked
		{
			foreach (byte b in bytes)
			{
				num ^= b;
				num *= 1099511628211L;
			}
			if (num == 0)
			{
				num = 1uL;
			}
			if (decimal.Compare(new decimal(num), 9223372036854775807m) > 0)
			{
				num &= 0x7FFFFFFFFFFFFFFFL;
			}
			return (long)num;
		}
	}
}
