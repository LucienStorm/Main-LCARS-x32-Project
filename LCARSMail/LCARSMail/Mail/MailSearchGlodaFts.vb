Imports System
Imports System.Collections.Generic
Imports System.Data.SQLite
Imports System.Diagnostics
Imports System.IO
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports System.Text
Imports Microsoft.VisualBasic.CompilerServices
    Public NotInheritable Class MailSearchGlodaFts
        Public Class GlodaCorpusStats
            Public TotalFtsRows As Integer

            Public WithBodyRows As Integer

            Public DistinctAccounts As Integer

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Private Const DbFileName As String = "gloda-fts.sqlite"

        Private Const FtsCreateSql As String = "CREATE VIRTUAL TABLE IF NOT EXISTS mail_fts USING fts5(account_id UNINDEXED, folder UNINDEXED, uid UNINDEXED, subject, from_line, to_line, cc_line, body, tokenize='porter unicode61');"

        Private Sub New()
        End Sub

        Public Shared Function DbPath() As String
            Dim text As String = Path.Combine(AccountStore.DataRoot(), "search-index")
            If Not Directory.Exists(text) Then
                Directory.CreateDirectory(text)
            End If
            Return Path.Combine(text, "gloda-fts.sqlite")
        End Function

        Public Shared Sub EnsureSchema()
            Dim val As SQLiteConnection = New SQLiteConnection("Data Source=" & DbPath() & ";")
            Try
                val.Open()
                MailSearchGlodaFts.ApplySearchPragmas(val)
                MailSearchGlodaFts.TryMigrateFtsSchemaIfNeeded(val)
                Dim val2 As SQLiteCommand = New SQLiteCommand("CREATE VIRTUAL TABLE IF NOT EXISTS mail_fts USING fts5(account_id UNINDEXED, folder UNINDEXED, uid UNINDEXED, subject, from_line, to_line, cc_line, body, tokenize='porter unicode61');", val)
                Try
                    val2.ExecuteNonQuery()
                Finally
                    If val2 IsNot Nothing Then CType(val2, IDisposable).Dispose()
                End Try
                MailSearchGlodaFts.EnsureMetaSchema(val)
            Finally
                If val IsNot Nothing Then CType(val, IDisposable).Dispose()
            End Try
        End Sub

        Private Shared Sub EnsureMetaSchema(conn As SQLiteConnection)
            If conn Is Nothing Then
                Return
            End If
            Dim val As SQLiteCommand = New SQLiteCommand("CREATE TABLE IF NOT EXISTS mail_fts_meta (rowid INTEGER PRIMARY KEY, date_unix INTEGER NOT NULL DEFAULT 0);", conn)
            Try
                val.ExecuteNonQuery()
            Finally
                If val IsNot Nothing Then CType(val, IDisposable).Dispose()
            End Try
        End Sub

        Private Shared Function FtsDefinitionIncludesToLine(conn As SQLiteConnection) As Boolean
            Dim result As Boolean
            Try
                Dim val As SQLiteCommand = New SQLiteCommand("SELECT sql FROM sqlite_master WHERE type='table' AND name='mail_fts';", conn)
                Try
                    Dim objectValue As Object = RuntimeHelpers.GetObjectValue(val.ExecuteScalar())
                    result = objectValue IsNot Nothing AndAlso objectValue IsNot DBNull.Value AndAlso objectValue.ToString().IndexOf("to_line", StringComparison.OrdinalIgnoreCase) >= 0
                Finally
                    If val IsNot Nothing Then CType(val, IDisposable).Dispose()
                End Try
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                result = False
                Call ProjectData.ClearProjectError()
            End Try
            Return result
        End Function

        Private Shared Sub TryMigrateFtsSchemaIfNeeded(conn As SQLiteConnection)
            If conn Is Nothing OrElse MailSearchGlodaFts.FtsDefinitionIncludesToLine(conn) Then
                Return
            End If
            Dim flag = False
            Try
                Dim val As SQLiteCommand = New SQLiteCommand("SELECT 1 FROM mail_fts LIMIT 1;", conn)
                Try
                    Dim objectValue As Object = RuntimeHelpers.GetObjectValue(val.ExecuteScalar())
                    flag = If(objectValue IsNot Nothing AndAlso objectValue IsNot DBNull.Value, True, False)
                Finally
                    If val IsNot Nothing Then CType(val, IDisposable).Dispose()
                End Try
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                flag = False
                Call ProjectData.ClearProjectError()
            End Try
            If Not flag Then
                Return
            End If
            Dim val2 As SQLiteCommand = New SQLiteCommand("CREATE VIRTUAL TABLE mail_fts_v322 USING fts5(account_id UNINDEXED, folder UNINDEXED, uid UNINDEXED, subject, from_line, to_line, cc_line, body, tokenize='porter unicode61');", conn)
            Try
                val2.ExecuteNonQuery()
            Finally
                If val2 IsNot Nothing Then CType(val2, IDisposable).Dispose()
            End Try
            Dim val3 As SQLiteCommand = New SQLiteCommand("INSERT INTO mail_fts_v322(rowid, account_id, folder, uid, subject, from_line, to_line, cc_line, body) SELECT rowid, account_id, folder, uid, subject, from_line, '', '', body FROM mail_fts;", conn)
            Try
                val3.ExecuteNonQuery()
            Finally
                If val3 IsNot Nothing Then CType(val3, IDisposable).Dispose()
            End Try
            Dim val4 As SQLiteCommand = New SQLiteCommand("DROP TABLE mail_fts;", conn)
            Try
                val4.ExecuteNonQuery()
            Finally
                If val4 IsNot Nothing Then CType(val4, IDisposable).Dispose()
            End Try
            Dim val5 As SQLiteCommand = New SQLiteCommand("CREATE VIRTUAL TABLE IF NOT EXISTS mail_fts USING fts5(account_id UNINDEXED, folder UNINDEXED, uid UNINDEXED, subject, from_line, to_line, cc_line, body, tokenize='porter unicode61');", conn)
            Try
                val5.ExecuteNonQuery()
            Finally
                If val5 IsNot Nothing Then CType(val5, IDisposable).Dispose()
            End Try
            Dim val6 As SQLiteCommand = New SQLiteCommand("INSERT INTO mail_fts(rowid, account_id, folder, uid, subject, from_line, to_line, cc_line, body) SELECT rowid, account_id, folder, uid, subject, from_line, to_line, cc_line, body FROM mail_fts_v322;", conn)
            Try
                val6.ExecuteNonQuery()
            Finally
                If val6 IsNot Nothing Then CType(val6, IDisposable).Dispose()
            End Try
            Dim val7 As SQLiteCommand = New SQLiteCommand("DROP TABLE mail_fts_v322;", conn)
            Try
                val7.ExecuteNonQuery()
            Finally
                If val7 IsNot Nothing Then CType(val7, IDisposable).Dispose()
            End Try
        End Sub

        Private Shared Sub ApplySearchPragmas(conn As SQLiteConnection)
            If conn Is Nothing Then
                Return
            End If
            Try
                Dim val As SQLiteCommand = New SQLiteCommand("PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL; PRAGMA cache_size=-64000; PRAGMA mmap_size=268435456; PRAGMA temp_store=MEMORY;", conn)
                Try
                    val.ExecuteNonQuery()
                Finally
                    If val IsNot Nothing Then CType(val, IDisposable).Dispose()
                End Try
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
        End Sub

        Public Shared Sub UpsertMessage(accountId As String, folder As String, uid As ULong, subject As String, fromLine As String, bodyText As String, Optional toLine As String = Nothing, Optional ccLine As String = Nothing, Optional dateUtc As Date = Nothing)
            If String.IsNullOrWhiteSpace(accountId) OrElse String.IsNullOrWhiteSpace(folder) Then
                Return
            End If
            Call EnsureSchema()
            Dim num = StableRowId(accountId, folder, uid)
            Dim val As SQLiteConnection = New SQLiteConnection("Data Source=" & DbPath() & ";")
            Try
                val.Open()
                MailSearchGlodaFts.ApplySearchPragmas(val)
                Dim val2 As SQLiteCommand = New SQLiteCommand("DELETE FROM mail_fts WHERE rowid=@id;", val)
                Try
                    val2.Parameters.AddWithValue("@id", num)
                    val2.ExecuteNonQuery()
                Finally
                    If val2 IsNot Nothing Then CType(val2, IDisposable).Dispose()
                End Try
                Dim val3 As SQLiteCommand = New SQLiteCommand("INSERT INTO mail_fts(rowid, account_id, folder, uid, subject, from_line, to_line, cc_line, body) VALUES (@id, @acct, @folder, @uid, @subj, @froml, @tol, @ccl, @body);", val)
                Try
                    val3.Parameters.AddWithValue("@id", num)
                    val3.Parameters.AddWithValue("@acct", accountId)
                    val3.Parameters.AddWithValue("@folder", folder)
                    val3.Parameters.AddWithValue("@uid", CObj(uid.ToString()))
                    val3.Parameters.AddWithValue("@subj", If(Not Equals(subject, Nothing), subject, ""))
                    val3.Parameters.AddWithValue("@froml", If(Not Equals(fromLine, Nothing), fromLine, ""))
                    val3.Parameters.AddWithValue("@tol", If(Not Equals(toLine, Nothing), toLine, ""))
                    val3.Parameters.AddWithValue("@ccl", If(Not Equals(ccLine, Nothing), ccLine, ""))
                    Dim text = If(Not Equals(bodyText, Nothing), bodyText, "")
                    If text.Length > 256000 Then
                        text = text.Substring(0, 256000)
                    End If
                    val3.Parameters.AddWithValue("@body", text)
                    val3.ExecuteNonQuery()
                Finally
                    If val3 IsNot Nothing Then CType(val3, IDisposable).Dispose()
                End Try
                MailSearchGlodaFts.UpsertMetaDate(val, num, dateUtc)
            Finally
                If val IsNot Nothing Then CType(val, IDisposable).Dispose()
            End Try
        End Sub

        Private Shared Sub UpsertMetaDate(conn As SQLiteConnection, rowId As Long, dateUtc As Date)
            If conn Is Nothing Then
                Return
            End If
            MailSearchGlodaFts.EnsureMetaSchema(conn)
            Dim num = 0L
            If Date.Compare(dateUtc, Date.MinValue) <> 0 Then
                Dim dateTime As Date = dateUtc.ToUniversalTime()
                Dim dateTime2 As Date = New DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                num = CLng(Math.Round((dateTime - dateTime2).TotalSeconds))
            End If
            Dim val As SQLiteCommand = New SQLiteCommand("INSERT INTO mail_fts_meta(rowid, date_unix) VALUES (@id, @du) ON CONFLICT(rowid) DO UPDATE SET date_unix=@du;", conn)
            Try
                val.Parameters.AddWithValue("@id", rowId)
                val.Parameters.AddWithValue("@du", num)
                val.ExecuteNonQuery()
            Finally
                If val IsNot Nothing Then CType(val, IDisposable).Dispose()
            End Try
        End Sub

        Public Shared Sub RemoveMessage(accountId As String, folder As String, uid As ULong)
            If String.IsNullOrWhiteSpace(accountId) Then
                Return
            End If
            Call EnsureSchema()
            Dim num = StableRowId(accountId, folder, uid)
            Dim val As SQLiteConnection = New SQLiteConnection("Data Source=" & DbPath() & ";")
            Try
                val.Open()
                Dim val2 As SQLiteCommand = New SQLiteCommand("DELETE FROM mail_fts WHERE rowid=@id;", val)
                Try
                    val2.Parameters.AddWithValue("@id", num)
                    val2.ExecuteNonQuery()
                Finally
                    If val2 IsNot Nothing Then CType(val2, IDisposable).Dispose()
                End Try
                Dim val3 As SQLiteCommand = New SQLiteCommand("DELETE FROM mail_fts_meta WHERE rowid=@id;", val)
                Try
                    val3.Parameters.AddWithValue("@id", num)
                    val3.ExecuteNonQuery()
                Finally
                    If val3 IsNot Nothing Then CType(val3, IDisposable).Dispose()
                End Try

            Finally
                If val IsNot Nothing Then CType(val, IDisposable).Dispose()
            End Try
        End Sub

        Public Shared Function CountIndexedMessages(accountId As String) As Integer
            Dim result As Integer
            If String.IsNullOrWhiteSpace(accountId) Then
                result = 0
            ElseIf Not File.Exists(DbPath()) Then
                result = 0
            Else
                Call EnsureSchema()
                Try
                    Dim val As SQLiteConnection = New SQLiteConnection("Data Source=" & DbPath() & ";")
                    Try
                        val.Open()
                        Dim val2 As SQLiteCommand = New SQLiteCommand("SELECT COUNT(*) FROM mail_fts WHERE account_id=@acct;", val)
                        Try
                            val2.Parameters.AddWithValue("@acct", accountId)
                            Dim objectValue As Object = RuntimeHelpers.GetObjectValue(val2.ExecuteScalar())
                            result = If(objectValue IsNot Nothing AndAlso objectValue IsNot DBNull.Value, Convert.ToInt32(RuntimeHelpers.GetObjectValue(objectValue)), 0)
                        Finally
                            If val2 IsNot Nothing Then CType(val2, IDisposable).Dispose()
                        End Try

                    Finally
                        If val IsNot Nothing Then CType(val, IDisposable).Dispose()
                    End Try
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    result = 0
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Public Shared Function CountMessagesWithBody(accountId As String) As Integer
            Dim result As Integer
            If String.IsNullOrWhiteSpace(accountId) Then
                result = 0
            ElseIf Not File.Exists(DbPath()) Then
                result = 0
            Else
                Call EnsureSchema()
                Try
                    Dim val As SQLiteConnection = New SQLiteConnection("Data Source=" & DbPath() & ";")
                    Try
                        val.Open()
                        Dim val2 As SQLiteCommand = New SQLiteCommand("SELECT COUNT(*) FROM mail_fts WHERE account_id=@acct AND length(trim(body)) > 0;", val)
                        Try
                            val2.Parameters.AddWithValue("@acct", accountId)
                            Dim objectValue As Object = RuntimeHelpers.GetObjectValue(val2.ExecuteScalar())
                            result = If(objectValue IsNot Nothing AndAlso objectValue IsNot DBNull.Value, Convert.ToInt32(RuntimeHelpers.GetObjectValue(objectValue)), 0)
                        Finally
                            If val2 IsNot Nothing Then CType(val2, IDisposable).Dispose()
                        End Try

                    Finally
                        If val IsNot Nothing Then CType(val, IDisposable).Dispose()
                    End Try
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    result = 0
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Public Shared Function Search(accountId As String, queryText As String, Optional maxHits As Integer = 12000, Optional skip As Integer = 0) As List(Of MailMessageHeader)
            Dim list As List(Of MailMessageHeader) = New List(Of MailMessageHeader)()
            Dim result As List(Of MailMessageHeader)
            If String.IsNullOrWhiteSpace(accountId) Then
                result = list
            Else
                Dim queryText2 As String = If(Not Equals(queryText, Nothing), queryText, "").Trim()
                If queryText2.Length = 0 Then
                    result = list
                ElseIf Not File.Exists(DbPath()) Then
                    result = list
                Else
                    Dim text = ExtractFolderFilter(queryText2)
                    Dim num = ExtractNewerDaysFromQuery(queryText2)
                    Dim num2 = ExtractOlderDaysFromQuery(queryText2)
                    ExtractLargerFromQuery(queryText2)
                    ExtractSmallerFromQuery(queryText2)
                    ExtractUnreadFromQuery(queryText2)
                    ExtractReadFromQuery(queryText2)
                    Dim flag = ExtractHasBodyFromQuery(queryText2)
                    ExtractToFromQuery(queryText2)
                    ExtractCcFromQuery(queryText2)
                    ExtractMessageIdFromQuery(queryText2)
                    ExtractInReplyToFromQuery(queryText2)
                    ExtractReferencesFromQuery(queryText2)
                    ExtractReplyToFromQuery(queryText2)
                    ExtractListIdFromQuery(queryText2)
                    ExtractReturnPathFromQuery(queryText2)
                    Dim text2 = BuildFtsMatchQuery(queryText2)
                    If text2.Length = 0 Then
                        result = list
                    Else
                        Call EnsureSchema()
                        If num > 0 OrElse num2 > 0 Then
                            MailSearchIndex.BackfillGlodaFtsMetaDates(accountId, 8000)
                        End If
                        Dim num3 = 0L
                        Dim num4 = 0L
                        If num > 0 Then
                            Dim dateTime = Date.UtcNow.AddDays(-num)
                            Dim dateTime2 As Date = New DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                            num3 = CLng(Math.Round((dateTime - dateTime2).TotalSeconds))
                        End If
                        If num2 > 0 Then
                            Dim dateTime3 = Date.UtcNow.AddDays(-num2)
                            Dim dateTime4 As Date = New DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                            num4 = CLng(Math.Round((dateTime3 - dateTime4).TotalSeconds))
                        End If
                        Try
                            Dim val As SQLiteConnection = New SQLiteConnection("Data Source=" & DbPath() & ";")
                            Try
                                val.Open()
                                MailSearchGlodaFts.ApplySearchPragmas(val)
                                Dim flag2 = If(num3 > 0 OrElse num4 > 0, True, False)
                                Dim text3 = "SELECT f.folder, f.uid, f.subject, f.from_line, bm25(f) AS rank FROM mail_fts f "
                                If flag2 Then
                                    text3 += "LEFT JOIN mail_fts_meta m ON m.rowid = f.rowid "
                                End If
                                text3 += "WHERE f.account_id=@acct AND f MATCH @match "
                                If text.Length > 0 Then
                                    text3 += "AND f.folder LIKE @folder ESCAPE '\' "
                                End If
                                If num3 > 0 Then
                                    text3 += "AND (IFNULL(m.date_unix, 0) = 0 OR m.date_unix >= @newerCut) "
                                End If
                                If num4 > 0 Then
                                    text3 += "AND (IFNULL(m.date_unix, 0) = 0 OR m.date_unix <= @olderCut) "
                                End If
                                If flag Then
                                    text3 += "AND length(trim(f.body)) > 0 "
                                End If
                                text3 += "ORDER BY rank LIMIT @lim OFFSET @skip;"
                                Dim val2 As SQLiteCommand = New SQLiteCommand(text3, val)
                                Try
                                    val2.Parameters.AddWithValue("@acct", accountId)
                                    val2.Parameters.AddWithValue("@match", text2)
                                    If text.Length > 0 Then
                                        val2.Parameters.AddWithValue("@folder", "%" & text & "%")
                                    End If
                                    If num3 > 0 Then
                                        val2.Parameters.AddWithValue("@newerCut", num3)
                                    End If
                                    If num4 > 0 Then
                                        val2.Parameters.AddWithValue("@olderCut", num4)
                                    End If
                                    val2.Parameters.AddWithValue("@lim", Math.Max(1, maxHits))
                                    val2.Parameters.AddWithValue("@skip", Math.Max(0, skip))
                                    Dim val3 As SQLiteDataReader = val2.ExecuteReader()
                                    Try
                                        While val3.Read()
                                            Dim folder = If(val3.IsDBNull(0), "", val3.GetString(0))
                                            Dim s = If(val3.IsDBNull(1), "0", val3.GetString(1))
                                            Dim result2 = 0UL
                                            ULong.TryParse(s, result2)
                                            Dim subject = If(val3.IsDBNull(2), "", val3.GetString(2))
                                            Dim text4 = If(val3.IsDBNull(3), "", val3.GetString(3))
                                            Dim ftsRank = 0.0
                                            If Not val3.IsDBNull(4) Then
                                                ftsRank = Convert.ToDouble(RuntimeHelpers.GetObjectValue(val3.GetValue(4)))
                                            End If
                                            Dim dateUtc = MailSearchIndex.TryGetIndexedDateUtc(accountId, folder, result2)
                                            list.Add(New MailMessageHeader With {
    .Folder = folder,
    .Uid = result2,
    .Subject = subject,
    .From = text4,
    .DateUtc = dateUtc,
    .FtsRank = ftsRank
})
                                        End While

                                    Finally
                                        If val3 IsNot Nothing Then CType(val3, IDisposable).Dispose()
                                    End Try

                                Finally
                                    If val2 IsNot Nothing Then CType(val2, IDisposable).Dispose()
                                End Try

                            Finally
                                If val IsNot Nothing Then CType(val, IDisposable).Dispose()
                            End Try
                        Catch projectError As Exception
                            ProjectData.SetProjectError(projectError)
                            result = New List(Of MailMessageHeader)()
                            Call ProjectData.ClearProjectError()
                            GoTo IL_04a9
                        End Try
                        result = ApplyDateFacetFilters(list, num, num2)
                    End If
                End If
            End If
            GoTo IL_04a9
IL_04a9:
            Return result
        End Function

        Private Shared Function ApplyDateFacetFilters(hits As List(Of MailMessageHeader), newerDays As Integer, olderDays As Integer) As List(Of MailMessageHeader)
            If hits Is Nothing OrElse hits.Count = 0 Then
                Return hits
            End If
            If newerDays <= 0 AndAlso olderDays <= 0 Then
                Return hits
            End If
            Dim list As List(Of MailMessageHeader) = New List(Of MailMessageHeader)()
            For Each hit In hits
                If hit IsNot Nothing AndAlso (newerDays <= 0 OrElse Date.Compare(hit.DateUtc, Date.MinValue) = 0 OrElse Date.Compare(hit.DateUtc, Date.UtcNow.AddDays(-newerDays)) >= 0) AndAlso (olderDays <= 0 OrElse Date.Compare(hit.DateUtc, Date.MinValue) = 0 OrElse Date.Compare(hit.DateUtc, Date.UtcNow.AddDays(-olderDays)) <= 0) Then
                    list.Add(hit)
                End If
            Next
            Return list
        End Function

        Public Shared Function ApplyThreadAwareRanking(hits As List(Of MailMessageHeader)) As List(Of MailMessageHeader)
            If hits Is Nothing OrElse hits.Count <= 1 Then
                Return hits
            End If
            Dim list = MailConversationGrouper.BuildThreadList(hits)
            If list Is Nothing OrElse list.Count = 0 Then
                Return hits
            End If
            Dim list2 As List(Of MailMessageHeader) = New List(Of MailMessageHeader)()
            For Each item In list
                If item Is Nothing OrElse item.Messages Is Nothing OrElse item.Messages.Count = 0 Then
                    Continue For
                End If
                Dim mailMessageHeader = item.Representative
                Dim num = If(mailMessageHeader IsNot Nothing, mailMessageHeader.FtsRank, 0.0)
                For Each message In item.Messages
                    If message IsNot Nothing AndAlso (num = 0.0 OrElse message.FtsRank <> 0.0 AndAlso message.FtsRank < num) Then
                        num = message.FtsRank
                        mailMessageHeader = message
                    End If
                Next
                If mailMessageHeader IsNot Nothing Then
                    If item.MessageCount > 1 AndAlso num <> 0.0 Then
                        mailMessageHeader.FtsRank = num - Math.Log(Math.Max(2, item.MessageCount)) * 0.15
                    End If
                    list2.Add(mailMessageHeader)
                End If
            Next
            list2.Sort(Function(a, b)
                           Dim num2 = If(a IsNot Nothing, a.FtsRank, 0.0)
                           Dim num3 = If(b IsNot Nothing, b.FtsRank, 0.0)
                           If num2 = 0.0 AndAlso num3 = 0.0 Then
                               Return 0
                           End If
                           If num2 = 0.0 Then
                               Return 1
                           End If
                           Return If(num3 = 0.0, -1, num2.CompareTo(num3))
                       End Function)
            Return list2
        End Function

        Public Shared Function TryGetBodySnippetForMessage(accountId As String, folder As String, uid As ULong) As String
            Dim result As String
            If String.IsNullOrWhiteSpace(accountId) OrElse String.IsNullOrWhiteSpace(folder) Then
                result = ""
            ElseIf Not File.Exists(DbPath()) Then
                result = ""
            Else
                Call EnsureSchema()
                Dim num = StableRowId(accountId, folder, uid)
                Try
                    Dim val As SQLiteConnection = New SQLiteConnection("Data Source=" & DbPath() & ";")
                    Try
                        val.Open()
                        Dim val2 As SQLiteCommand = New SQLiteCommand("SELECT snippet(mail_fts, 4, '[', ']', '…', 12) FROM mail_fts WHERE rowid=@id AND account_id=@acct;", val)
                        Try
                            val2.Parameters.AddWithValue("@id", num)
                            val2.Parameters.AddWithValue("@acct", accountId)
                            Dim objectValue As Object = RuntimeHelpers.GetObjectValue(val2.ExecuteScalar())
                            result = (If(objectValue IsNot Nothing AndAlso objectValue IsNot DBNull.Value, objectValue.ToString().Trim(), ""))
                        Finally
                            If val2 IsNot Nothing Then CType(val2, IDisposable).Dispose()
                        End Try

                    Finally
                        If val IsNot Nothing Then CType(val, IDisposable).Dispose()
                    End Try
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    result = ""
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Friend Shared Function BuildFtsMatchQuery(userQuery As String) As String
            Dim list = MailSearchIndex.ParseGlodaOrGroups(userQuery)
            If list Is Nothing OrElse list.Count = 0 Then
                Return ""
            End If
            Dim list2 As List(Of String) = New List(Of String)()
            For Each item In list
                If item Is Nothing OrElse item.Length = 0 Then
                    Continue For
                End If
                Dim list3 As List(Of String) = New List(Of String)()
                Dim array = item
                For Each rawTerm In array
                    Dim text = MapFacetTerm(rawTerm)
                    If text.Length <> 0 Then
                        list3.Add(text)
                    End If
                Next
                If list3.Count <> 0 Then
                    list2.Add("(" & String.Join(" AND ", list3) & ")")
                End If
            Next
            If list2.Count = 0 Then
                Return ""
            End If
            Return String.Join(" OR ", list2)
        End Function

        Private Shared Function MapFacetTerm(rawTerm As String) As String
            Dim text As String = If(Not Equals(rawTerm, Nothing), rawTerm, "").Trim()
            If text.Length = 0 Then
                Return ""
            End If
            If text.StartsWith("-") AndAlso text.Length > 1 Then
                Dim text2 = MapFacetTerm(text.Substring(1))
                If text2.Length = 0 Then
                    Return ""
                End If
                Return "NOT (" & text2 & ")"
            End If
            Dim text3 As String = text.ToLowerInvariant()
            If text3.StartsWith("from:", StringComparison.Ordinal) Then
                Return "from_line:" & EscapeFtsTerm(text.Substring(5))
            End If
            If text3.StartsWith("to:", StringComparison.Ordinal) Then
                Return "to_line:" & EscapeFtsTerm(text.Substring(3))
            End If
            If text3.StartsWith("cc:", StringComparison.Ordinal) Then
                Return "cc_line:" & EscapeFtsTerm(text.Substring(3))
            End If
            If text3.StartsWith("messageid:", StringComparison.Ordinal) Then
                Dim text4 As String = text.Substring(10).Trim()
                If text4.Length = 0 Then
                    Return ""
                End If
                Return "body:" & EscapeFtsTerm("Message-Id: " & text4)
            End If
            If text3.StartsWith("inreplyto:", StringComparison.Ordinal) Then
                Dim text5 As String = text.Substring(10).Trim()
                If text5.Length = 0 Then
                    Return ""
                End If
                Return "body:" & EscapeFtsTerm("In-Reply-To: " & text5)
            End If
            If text3.StartsWith("references:", StringComparison.Ordinal) Then
                Dim text6 As String = text.Substring(11).Trim()
                If text6.Length = 0 Then
                    Return ""
                End If
                Return "body:" & EscapeFtsTerm("References: " & text6)
            End If
            If text3.StartsWith("replyto:", StringComparison.Ordinal) Then
                Dim text7 As String = text.Substring(8).Trim()
                If text7.Length = 0 Then
                    Return ""
                End If
                Return "body:" & EscapeFtsTerm("Reply-To: " & text7)
            End If
            If text3.StartsWith("list-id:", StringComparison.Ordinal) Then
                Dim text8 As String = text.Substring(8).Trim()
                If text8.Length = 0 Then
                    Return ""
                End If
                Return "body:" & EscapeFtsTerm("List-Id: " & text8)
            End If
            If text3.StartsWith("return-path:", StringComparison.Ordinal) Then
                Dim text9 As String = text.Substring(12).Trim()
                If text9.Length = 0 Then
                    Return ""
                End If
                Return "body:" & EscapeFtsTerm("Return-Path: " & text9)
            End If
            If text3.StartsWith("label:", StringComparison.Ordinal) OrElse text3.StartsWith("tag:", StringComparison.Ordinal) Then
                Dim num = text.IndexOf(":"c)
                Dim text10 As String = If(num >= 0, text.Substring(num + 1), "").Trim()
                If text10.Length = 0 Then
                    Return ""
                End If
                Dim text11 = EscapeFtsTerm(text10)
                Return "(subject:" & text11 & " OR body:" & text11 & ")"
            End If
            If text3.StartsWith("subject:", StringComparison.Ordinal) Then
                Return "subject:" & EscapeFtsTerm(text.Substring(8))
            End If
            If text3.StartsWith("body:", StringComparison.Ordinal) Then
                Return "body:" & EscapeFtsTerm(text.Substring(5))
            End If
            If text3.StartsWith("folder:", StringComparison.Ordinal) Then
                Return ""
            End If
            If text3.StartsWith("newer:", StringComparison.Ordinal) Then
                Return ""
            End If
            If text3.StartsWith("older:", StringComparison.Ordinal) Then
                Return ""
            End If
            If text3.StartsWith("larger:", StringComparison.Ordinal) Then
                Return ""
            End If
            If text3.StartsWith("smaller:", StringComparison.Ordinal) Then
                Return ""
            End If
            If text3.Equals("unread:1", StringComparison.Ordinal) OrElse text3.Equals("unread:true", StringComparison.Ordinal) OrElse text3.Equals("is:unread", StringComparison.Ordinal) Then
                Return ""
            End If
            If text3.Equals("read:1", StringComparison.Ordinal) OrElse text3.Equals("read:true", StringComparison.Ordinal) OrElse text3.Equals("is:read", StringComparison.Ordinal) Then
                Return ""
            End If
            If text3.Equals("has:body", StringComparison.Ordinal) OrElse text3.Equals("body:yes", StringComparison.Ordinal) Then
                Return ""
            End If
            Return EscapeFtsTerm(text)
        End Function

        Private Shared Function ExtractNewerDaysFromQuery(ByRef queryText As String) As Integer
            Dim text As String = If(Not Equals(queryText, Nothing), queryText, "").Trim()
            If text.Length = 0 Then
                Return 0
            End If
            Dim array = text.Split(New Char(1) {" "c, ChrW(9)}, StringSplitOptions.RemoveEmptyEntries)
            Dim list As List(Of String) = New List(Of String)()
            Dim result = 0
            Dim array2 = array
            For Each text2 In array2
                If text2.StartsWith("newer:", StringComparison.OrdinalIgnoreCase) Then
                    Dim text3 As String = text2.Substring(6).Trim()
                    If text3.EndsWith("d", StringComparison.OrdinalIgnoreCase) Then
                        text3 = text3.Substring(0, text3.Length - 1)
                    End If
                    Dim result2 = 0
                    If Integer.TryParse(text3, result2) AndAlso result2 > 0 Then
                        result = result2
                    End If
                Else
                    list.Add(text2)
                End If
            Next
            queryText = String.Join(" ", list)
            Return result
        End Function

        Private Shared Sub ExtractLargerFromQuery(ByRef queryText As String)
            Dim text As String = If(Not Equals(queryText, Nothing), queryText, "").Trim()
            If text.Length = 0 Then
                Return
            End If
            Dim array = text.Split(New Char(1) {" "c, ChrW(9)}, StringSplitOptions.RemoveEmptyEntries)
            Dim list As List(Of String) = New List(Of String)()
            Dim array2 = array
            For Each text2 In array2
                If Not text2.StartsWith("larger:", StringComparison.OrdinalIgnoreCase) Then
                    list.Add(text2)
                End If
            Next
            queryText = String.Join(" ", list)
        End Sub

        Private Shared Sub ExtractSmallerFromQuery(ByRef queryText As String)
            Dim text As String = If(Not Equals(queryText, Nothing), queryText, "").Trim()
            If text.Length = 0 Then
                Return
            End If
            Dim array = text.Split(New Char(1) {" "c, ChrW(9)}, StringSplitOptions.RemoveEmptyEntries)
            Dim list As List(Of String) = New List(Of String)()
            Dim array2 = array
            For Each text2 In array2
                If Not text2.StartsWith("smaller:", StringComparison.OrdinalIgnoreCase) Then
                    list.Add(text2)
                End If
            Next
            queryText = String.Join(" ", list)
        End Sub

        Private Shared Function ExtractOlderDaysFromQuery(ByRef queryText As String) As Integer
            Dim text As String = If(Not Equals(queryText, Nothing), queryText, "").Trim()
            If text.Length = 0 Then
                Return 0
            End If
            Dim array = text.Split(New Char(1) {" "c, ChrW(9)}, StringSplitOptions.RemoveEmptyEntries)
            Dim list As List(Of String) = New List(Of String)()
            Dim result = 0
            Dim array2 = array
            For Each text2 In array2
                If text2.StartsWith("older:", StringComparison.OrdinalIgnoreCase) Then
                    Dim text3 As String = text2.Substring(6).Trim()
                    If text3.EndsWith("d", StringComparison.OrdinalIgnoreCase) Then
                        text3 = text3.Substring(0, text3.Length - 1)
                    End If
                    Dim result2 = 0
                    If Integer.TryParse(text3, result2) AndAlso result2 > 0 Then
                        result = result2
                    End If
                Else
                    list.Add(text2)
                End If
            Next
            queryText = String.Join(" ", list)
            Return result
        End Function

        Private Shared Sub ExtractToFromQuery(ByRef queryText As String)
            Dim text As String = If(Not Equals(queryText, Nothing), queryText, "").Trim()
            If text.Length = 0 Then
                Return
            End If
            Dim array = text.Split(New Char(1) {" "c, ChrW(9)}, StringSplitOptions.RemoveEmptyEntries)
            Dim list As List(Of String) = New List(Of String)()
            Dim array2 = array
            For Each text2 In array2
                If Not text2.StartsWith("to:", StringComparison.OrdinalIgnoreCase) Then
                    list.Add(text2)
                End If
            Next
            queryText = String.Join(" ", list)
        End Sub

        Private Shared Sub ExtractCcFromQuery(ByRef queryText As String)
            Dim text As String = If(Not Equals(queryText, Nothing), queryText, "").Trim()
            If text.Length = 0 Then
                Return
            End If
            Dim array = text.Split(New Char(1) {" "c, ChrW(9)}, StringSplitOptions.RemoveEmptyEntries)
            Dim list As List(Of String) = New List(Of String)()
            Dim array2 = array
            For Each text2 In array2
                If Not text2.StartsWith("cc:", StringComparison.OrdinalIgnoreCase) Then
                    list.Add(text2)
                End If
            Next
            queryText = String.Join(" ", list)
        End Sub

        Private Shared Sub ExtractMessageIdFromQuery(ByRef queryText As String)
            Dim text As String = If(Not Equals(queryText, Nothing), queryText, "").Trim()
            If text.Length = 0 Then
                Return
            End If
            Dim array = text.Split(New Char(1) {" "c, ChrW(9)}, StringSplitOptions.RemoveEmptyEntries)
            Dim list As List(Of String) = New List(Of String)()
            Dim array2 = array
            For Each text2 In array2
                If Not text2.StartsWith("messageid:", StringComparison.OrdinalIgnoreCase) Then
                    list.Add(text2)
                End If
            Next
            queryText = String.Join(" ", list)
        End Sub

        Private Shared Sub ExtractInReplyToFromQuery(ByRef queryText As String)
            Dim text As String = If(Not Equals(queryText, Nothing), queryText, "").Trim()
            If text.Length = 0 Then
                Return
            End If
            Dim array = text.Split(New Char(1) {" "c, ChrW(9)}, StringSplitOptions.RemoveEmptyEntries)
            Dim list As List(Of String) = New List(Of String)()
            Dim array2 = array
            For Each text2 In array2
                If Not text2.StartsWith("inreplyto:", StringComparison.OrdinalIgnoreCase) Then
                    list.Add(text2)
                End If
            Next
            queryText = String.Join(" ", list)
        End Sub

        Private Shared Sub ExtractReferencesFromQuery(ByRef queryText As String)
            Dim text As String = If(Not Equals(queryText, Nothing), queryText, "").Trim()
            If text.Length = 0 Then
                Return
            End If
            Dim array = text.Split(New Char(1) {" "c, ChrW(9)}, StringSplitOptions.RemoveEmptyEntries)
            Dim list As List(Of String) = New List(Of String)()
            Dim array2 = array
            For Each text2 In array2
                If Not text2.StartsWith("references:", StringComparison.OrdinalIgnoreCase) Then
                    list.Add(text2)
                End If
            Next
            queryText = String.Join(" ", list)
        End Sub

        Private Shared Sub ExtractReplyToFromQuery(ByRef queryText As String)
            Dim text As String = If(Not Equals(queryText, Nothing), queryText, "").Trim()
            If text.Length = 0 Then
                Return
            End If
            Dim array = text.Split(New Char(1) {" "c, ChrW(9)}, StringSplitOptions.RemoveEmptyEntries)
            Dim list As List(Of String) = New List(Of String)()
            Dim array2 = array
            For Each text2 In array2
                If Not text2.StartsWith("replyto:", StringComparison.OrdinalIgnoreCase) Then
                    list.Add(text2)
                End If
            Next
            queryText = String.Join(" ", list)
        End Sub

        Private Shared Sub ExtractListIdFromQuery(ByRef queryText As String)
            Dim text As String = If(Not Equals(queryText, Nothing), queryText, "").Trim()
            If text.Length = 0 Then
                Return
            End If
            Dim array = text.Split(New Char(1) {" "c, ChrW(9)}, StringSplitOptions.RemoveEmptyEntries)
            Dim list As List(Of String) = New List(Of String)()
            Dim array2 = array
            For Each text2 In array2
                If Not text2.StartsWith("list-id:", StringComparison.OrdinalIgnoreCase) Then
                    list.Add(text2)
                End If
            Next
            queryText = String.Join(" ", list)
        End Sub

        Private Shared Sub ExtractReturnPathFromQuery(ByRef queryText As String)
            Dim text As String = If(Not Equals(queryText, Nothing), queryText, "").Trim()
            If text.Length = 0 Then
                Return
            End If
            Dim array = text.Split(New Char(1) {" "c, ChrW(9)}, StringSplitOptions.RemoveEmptyEntries)
            Dim list As List(Of String) = New List(Of String)()
            Dim array2 = array
            For Each text2 In array2
                If Not text2.StartsWith("return-path:", StringComparison.OrdinalIgnoreCase) Then
                    list.Add(text2)
                End If
            Next
            queryText = String.Join(" ", list)
        End Sub

        Private Shared Function ExtractHasBodyFromQuery(ByRef queryText As String) As Boolean
            Dim text As String = If(Not Equals(queryText, Nothing), queryText, "").Trim()
            If text.Length = 0 Then
                Return False
            End If
            Dim array = text.Split(New Char(1) {" "c, ChrW(9)}, StringSplitOptions.RemoveEmptyEntries)
            Dim list As List(Of String) = New List(Of String)()
            Dim result = False
            Dim array2 = array
            For Each text2 In array2
                If text2.Equals("has:body", StringComparison.OrdinalIgnoreCase) OrElse text2.Equals("body:yes", StringComparison.OrdinalIgnoreCase) Then
                    result = True
                Else
                    list.Add(text2)
                End If
            Next
            queryText = String.Join(" ", list)
            Return result
        End Function

        Public Shared Sub TrySetMetaDateUtc(accountId As String, folder As String, uid As ULong, dateUtc As Date)
            If String.IsNullOrWhiteSpace(accountId) OrElse String.IsNullOrWhiteSpace(folder) OrElse Date.Compare(dateUtc, Date.MinValue) = 0 Then
                Return
            End If
            Call EnsureSchema()
            Dim rowId = StableRowId(accountId, folder, uid)
            Dim val As SQLiteConnection = New SQLiteConnection("Data Source=" & DbPath() & ";")
            Try
                val.Open()
                MailSearchGlodaFts.UpsertMetaDate(val, rowId, dateUtc)
            Finally
                If val IsNot Nothing Then CType(val, IDisposable).Dispose()
            End Try
        End Sub

        Private Shared Sub ExtractUnreadFromQuery(ByRef queryText As String)
            Dim text As String = If(Not Equals(queryText, Nothing), queryText, "").Trim()
            If text.Length = 0 Then
                Return
            End If
            Dim array = text.Split(New Char(1) {" "c, ChrW(9)}, StringSplitOptions.RemoveEmptyEntries)
            Dim list As List(Of String) = New List(Of String)()
            Dim array2 = array
            For Each text2 In array2
                If Not text2.Equals("unread:1", StringComparison.OrdinalIgnoreCase) AndAlso Not text2.Equals("unread:true", StringComparison.OrdinalIgnoreCase) AndAlso Not text2.Equals("is:unread", StringComparison.OrdinalIgnoreCase) Then
                    list.Add(text2)
                End If
            Next
            queryText = String.Join(" ", list)
        End Sub

        Private Shared Sub ExtractReadFromQuery(ByRef queryText As String)
            Dim text As String = If(Not Equals(queryText, Nothing), queryText, "").Trim()
            If text.Length = 0 Then
                Return
            End If
            Dim array = text.Split(New Char(1) {" "c, ChrW(9)}, StringSplitOptions.RemoveEmptyEntries)
            Dim list As List(Of String) = New List(Of String)()
            Dim array2 = array
            For Each text2 In array2
                If Not text2.Equals("read:1", StringComparison.OrdinalIgnoreCase) AndAlso Not text2.Equals("read:true", StringComparison.OrdinalIgnoreCase) AndAlso Not text2.Equals("is:read", StringComparison.OrdinalIgnoreCase) Then
                    list.Add(text2)
                End If
            Next
            queryText = String.Join(" ", list)
        End Sub

        Private Shared Function ExtractFolderFilter(ByRef queryText As String) As String
            Dim text As String = If(Not Equals(queryText, Nothing), queryText, "").Trim()
            If text.Length = 0 Then
                Return ""
            End If
            Dim array = text.Split(New Char(1) {" "c, ChrW(9)}, StringSplitOptions.RemoveEmptyEntries)
            Dim list As List(Of String) = New List(Of String)()
            Dim result = ""
            Dim array2 = array
            For Each text2 In array2
                If text2.StartsWith("folder:", StringComparison.OrdinalIgnoreCase) Then
                    result = text2.Substring(7).Trim().Replace("\", "\\").Replace("%", "\%").Replace("_", "\_")
                Else
                    list.Add(text2)
                End If
            Next
            queryText = String.Join(" ", list)
            Return result
        End Function

        Private Shared Function EscapeFtsTerm(raw As String) As String
            Dim text As String = If(Not Equals(raw, Nothing), raw, "").Trim().ToLowerInvariant()
            If text.Length = 0 Then
                Return ""
            End If
            text = text.Replace("""", """""")
            Dim text2 = text
            Dim i = 0
            Dim length = text2.Length

            While i < length
                Dim c = text2(i)
                If Not Char.IsLetterOrDigit(c) AndAlso c <> "@"c AndAlso c <> "."c AndAlso c <> "_"c AndAlso c <> "-"c Then
                    Return """" & text & """"
                End If

                i = i + 1
            End While
            Return text
        End Function

        Public Shared Function TryGetCorpusStats(Optional accountId As String = Nothing) As GlodaCorpusStats
            Dim glodaCorpusStats As GlodaCorpusStats = New GlodaCorpusStats()
            If Not File.Exists(DbPath()) Then
                Return glodaCorpusStats
            End If
            Call EnsureSchema()
            Try
                Dim val As SQLiteConnection = New SQLiteConnection("Data Source=" & DbPath() & ";")
                Try
                    val.Open()
                    If String.IsNullOrWhiteSpace(accountId) Then
                        Dim val2 As SQLiteCommand = New SQLiteCommand("SELECT COUNT(DISTINCT account_id) FROM mail_fts;", val)
                        Try
                            Dim objectValue As Object = RuntimeHelpers.GetObjectValue(val2.ExecuteScalar())
                            If objectValue IsNot Nothing AndAlso objectValue IsNot DBNull.Value Then
                                glodaCorpusStats.DistinctAccounts = Convert.ToInt32(RuntimeHelpers.GetObjectValue(objectValue))
                            End If

                        Finally
                            If val2 IsNot Nothing Then CType(val2, IDisposable).Dispose()
                        End Try
                        Dim val3 As SQLiteCommand = New SQLiteCommand("SELECT COUNT(*) FROM mail_fts;", val)
                        Try
                            Dim objectValue2 As Object = RuntimeHelpers.GetObjectValue(val3.ExecuteScalar())
                            If objectValue2 IsNot Nothing AndAlso objectValue2 IsNot DBNull.Value Then
                                glodaCorpusStats.TotalFtsRows = Convert.ToInt32(RuntimeHelpers.GetObjectValue(objectValue2))
                            End If

                        Finally
                            If val3 IsNot Nothing Then CType(val3, IDisposable).Dispose()
                        End Try
                        Dim val4 As SQLiteCommand = New SQLiteCommand("SELECT COUNT(*) FROM mail_fts WHERE length(trim(body)) > 0;", val)
                        Try
                            Dim objectValue3 As Object = RuntimeHelpers.GetObjectValue(val4.ExecuteScalar())
                            If objectValue3 IsNot Nothing AndAlso objectValue3 IsNot DBNull.Value Then
                                glodaCorpusStats.WithBodyRows = Convert.ToInt32(RuntimeHelpers.GetObjectValue(objectValue3))
                            End If

                        Finally
                            If val4 IsNot Nothing Then CType(val4, IDisposable).Dispose()
                        End Try
                    Else
                        glodaCorpusStats.DistinctAccounts = 1
                        glodaCorpusStats.TotalFtsRows = CountIndexedMessages(accountId)
                        glodaCorpusStats.WithBodyRows = CountMessagesWithBody(accountId)
                    End If

                Finally
                    If val IsNot Nothing Then CType(val, IDisposable).Dispose()
                End Try
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
            Return glodaCorpusStats
        End Function

        Private Shared Function StableRowId(accountId As String, folder As String, uid As ULong) As Long
            Dim s As String = accountId & "|" & If(Not Equals(folder, Nothing), folder, "") & "|" & uid.ToString()
            Dim num = 14695981039346656037UL
            Dim bytes = Encoding.UTF8.GetBytes(s)
            For Each b In bytes
                num = num Xor b
                num *= 1099511628211L
            Next
            If num = 0 Then
                num = 1UL
            End If
            If Decimal.Compare(New Decimal(num), 9223372036854775807D) > 0 Then
                num = num And &H7FFFFFFFFFFFFFFFL
            End If
            Return num
        End Function
    End Class

