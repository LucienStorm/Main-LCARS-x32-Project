Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Text
Imports System.Web.Script.Serialization
Imports Microsoft.VisualBasic.CompilerServices
    Public NotInheritable Class MailSearchIndex
        Private Class IndexEntry
            Public Folder As String

            Public Uid As ULong

            Public Subject As String

            Public FromLine As String

            Public ToLine As String

            Public CcLine As String

            Public BodyPreview As String

            Public DateUtc As Date

            Public IsSeen As Boolean

            Public SizeBytes As Long

            Public MessageId As String

            Public InReplyTo As String

            Public ReferencesHeader As String

            Public ReplyToLine As String

            Public ListIdLine As String

            Public ReturnPathLine As String

            Public AttachmentCount As Integer

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Public Class IndexBodyTarget
            Public Folder As String

            Public Uid As ULong

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Private Const IndexSubdir As String = "search-index"

        Private Sub New()
        End Sub

        Public Shared Sub UpsertHeaders(accountId As String, folder As String, headers As List(Of MailMessageHeader))
            If String.IsNullOrWhiteSpace(accountId) OrElse String.IsNullOrWhiteSpace(folder) OrElse headers Is Nothing OrElse headers.Count = 0 Then
                Return
            End If
            Dim dictionary = LoadMap(accountId)
            For Each header In headers
                If header IsNot Nothing Then
                    Dim key As String = folder & "|" & header.Uid.ToString()
                    Dim bodyPreview = ""
                    If dictionary.ContainsKey(key) Then
                        Dim indexEntry = dictionary(key)
                        bodyPreview = If(Not Equals(indexEntry.BodyPreview, Nothing), indexEntry.BodyPreview, "")
                    End If
                    Dim attachmentCount = 0
                    If dictionary.ContainsKey(key) Then
                        attachmentCount = dictionary(key).AttachmentCount
                    End If
                    If header.HeaderAttachmentCount >= 0 Then
                        attachmentCount = header.HeaderAttachmentCount
                    End If
                    Dim indexEntry2 As IndexEntry = New IndexEntry With {
        .Folder = folder,
        .Uid = header.Uid
    }
                    Dim mailMessageHeader = header
                    indexEntry2.Subject = If(Not Equals(mailMessageHeader.Subject, Nothing), mailMessageHeader.Subject, "")
                    Dim mailMessageHeader2 = header
                    indexEntry2.FromLine = If(Not Equals(mailMessageHeader2.From, Nothing), mailMessageHeader2.From, "")
                    Dim mailMessageHeader3 = header
                    indexEntry2.ToLine = If(Not Equals(mailMessageHeader3.ToAddresses, Nothing), mailMessageHeader3.ToAddresses, "")
                    Dim mailMessageHeader4 = header
                    indexEntry2.CcLine = If(Not Equals(mailMessageHeader4.CcAddresses, Nothing), mailMessageHeader4.CcAddresses, "")
                    indexEntry2.DateUtc = header.DateUtc
                    indexEntry2.BodyPreview = bodyPreview
                    indexEntry2.IsSeen = header.IsSeen
                    indexEntry2.SizeBytes = header.SizeBytes
                    indexEntry2.AttachmentCount = attachmentCount
                    Dim mailMessageHeader5 = header
                    indexEntry2.MessageId = If(Not Equals(mailMessageHeader5.MessageId, Nothing), mailMessageHeader5.MessageId, "")
                    Dim mailMessageHeader6 = header
                    indexEntry2.InReplyTo = If(Not Equals(mailMessageHeader6.InReplyTo, Nothing), mailMessageHeader6.InReplyTo, "")
                    Dim mailMessageHeader7 = header
                    indexEntry2.ReferencesHeader = If(Not Equals(mailMessageHeader7.References, Nothing), mailMessageHeader7.References, "")
                    Dim mailMessageHeader8 = header
                    indexEntry2.ReplyToLine = If(Not Equals(mailMessageHeader8.ReplyTo, Nothing), mailMessageHeader8.ReplyTo, "")
                    Dim mailMessageHeader9 = header
                    indexEntry2.ListIdLine = If(Not Equals(mailMessageHeader9.ListId, Nothing), mailMessageHeader9.ListId, "")
                    Dim mailMessageHeader10 = header
                    indexEntry2.ReturnPathLine = If(Not Equals(mailMessageHeader10.ReturnPath, Nothing), mailMessageHeader10.ReturnPath, "")
                    dictionary(key) = indexEntry2
                End If
            Next
            SaveMap(accountId, dictionary)
            For Each header2 In headers
                If header2 IsNot Nothing Then
                    Dim key2 As String = folder & "|" & header2.Uid.ToString()
                    Dim entry = dictionary(key2)
                    Dim uid = header2.Uid
                    Dim subject = header2.Subject
                    Dim fromLine = header2.From
                    Dim bodyText = ComposeFtsBodyText("", entry, accountId)
                    Dim mailMessageHeader10 = header2
                    Dim toLine = If(Not Equals(mailMessageHeader10.ToAddresses, Nothing), mailMessageHeader10.ToAddresses, "")
                    Dim mailMessageHeader9 = header2
                    MailSearchGlodaFts.UpsertMessage(accountId, folder, uid, subject, fromLine, bodyText, toLine, If(Not Equals(mailMessageHeader9.CcAddresses, Nothing), mailMessageHeader9.CcAddresses, ""), header2.DateUtc)
                End If
            Next
        End Sub

        Public Shared Sub UpsertBodyPreview(accountId As String, folder As String, uid As ULong, bodyPreview As String, Optional attachmentCount As Integer = -1)
            If String.IsNullOrWhiteSpace(accountId) OrElse String.IsNullOrWhiteSpace(folder) Then
                Return
            End If
            Dim dictionary = LoadMap(accountId)
            Dim key As String = folder & "|" & uid.ToString()
            If dictionary.ContainsKey(key) Then
                Dim text As String = If(Not Equals(bodyPreview, Nothing), bodyPreview, "").Trim()
                If text.Length > 64000 Then
                    text = text.Substring(0, 64000)
                End If
                dictionary(key).BodyPreview = text
                If attachmentCount >= 0 Then
                    dictionary(key).AttachmentCount = attachmentCount
                End If
                SaveMap(accountId, dictionary)
                Dim indexEntry = dictionary(key)
                MailSearchGlodaFts.UpsertMessage(accountId, folder, uid, indexEntry.Subject, indexEntry.FromLine, ComposeFtsBodyText(text, indexEntry, accountId), indexEntry.ToLine, indexEntry.CcLine, indexEntry.DateUtc)
            End If
        End Sub

        Public Shared Function RebuildFtsHeaderCorpusForAccount(accountId As String, Optional maxRows As Integer = 500) As Integer
            If String.IsNullOrWhiteSpace(accountId) Then
                Return 0
            End If
            Dim dictionary = LoadMap(accountId)
            If dictionary Is Nothing OrElse dictionary.Count = 0 Then
                Return 0
            End If
            Dim list As List(Of String) = New List(Of String)(dictionary.Keys)
            list.Sort(StringComparer.Ordinal)
            Dim corpusRebuildStart = MailSearchIndexCursor.GetCorpusRebuildStart(accountId, list.Count)
            Dim num = 0
            Dim i = 0

            While i < list.Count
                If maxRows > 0 AndAlso num >= maxRows Then
                    Exit While
                End If
                Dim index = corpusRebuildStart + i Mod list.Count
                Dim key = list(index)
                Dim indexEntry = dictionary(key)
                If indexEntry IsNot Nothing Then
                    Dim indexEntry2 = indexEntry
                    Dim preview = If(Not Equals(indexEntry2.BodyPreview, Nothing), indexEntry2.BodyPreview, "")
                    MailSearchGlodaFts.UpsertMessage(accountId, indexEntry.Folder, indexEntry.Uid, indexEntry.Subject, indexEntry.FromLine, ComposeFtsBodyText(preview, indexEntry, accountId), indexEntry.ToLine, indexEntry.CcLine, indexEntry.DateUtc)
                    num = num + 1
                End If

                i = i + 1
            End While
            If num > 0 Then
                MailSearchIndexCursor.AdvanceCorpusRebuildCursor(accountId, list.Count, num)
            End If
            Return num
        End Function

        Public Shared Function BackfillGlodaFtsMetaDates(accountId As String, Optional maxRows As Integer = 4000) As Integer
            If String.IsNullOrWhiteSpace(accountId) Then
                Return 0
            End If
            Dim dictionary = LoadMap(accountId)
            If dictionary Is Nothing OrElse dictionary.Count = 0 Then
                Return 0
            End If
            Dim list As List(Of String) = New List(Of String)()
            For Each item In dictionary
                If item.Value IsNot Nothing AndAlso Date.Compare(item.Value.DateUtc, Date.MinValue) <> 0 Then
                    list.Add(item.Key)
                End If
            Next
            If list.Count = 0 Then
                Return 0
            End If
            list.Sort(StringComparer.Ordinal)
            Dim metaBackfillStart = MailSearchIndexCursor.GetMetaBackfillStart(accountId, list.Count)
            Dim num = 0
            Dim i = 0

            While i < list.Count
                If maxRows > 0 AndAlso num >= maxRows Then
                    Exit While
                End If
                Dim index = metaBackfillStart + i Mod list.Count
                Dim indexEntry = dictionary(list(index))
                If indexEntry IsNot Nothing AndAlso Date.Compare(indexEntry.DateUtc, Date.MinValue) <> 0 Then
                    MailSearchGlodaFts.TrySetMetaDateUtc(accountId, indexEntry.Folder, indexEntry.Uid, indexEntry.DateUtc)
                    num = num + 1
                End If

                i = i + 1
            End While
            If num > 0 Then
                MailSearchIndexCursor.AdvanceMetaBackfillCursor(accountId, list.Count, num)
            End If
            Return num
        End Function

        Private Shared Function ComposeFtsBodyText(preview As String, entry As IndexEntry, Optional accountId As String = Nothing) As String
            Dim stringBuilder As StringBuilder = New StringBuilder()
            Dim text As String = If(Not Equals(preview, Nothing), preview, "").Trim()
            If text.Length > 0 Then
                stringBuilder.Append(text)
                AppendHeaderLine(stringBuilder, "X-LCARS-HasBody", "yes")
            Else
                AppendHeaderLine(stringBuilder, "X-LCARS-HasBody", "no")
            End If
            If entry Is Nothing Then
                Return stringBuilder.ToString()
            End If
            Dim text2 As String = If(Not Equals(accountId, Nothing), accountId, "").Trim()
            If text2.Length > 0 Then
                AppendHeaderLine(stringBuilder, "X-LCARS-Account", text2)
            End If
            AppendHeaderLine(stringBuilder, "Subject", entry.Subject)
            AppendHeaderLine(stringBuilder, "From", entry.FromLine)
            AppendHeaderLine(stringBuilder, "To", entry.ToLine)
            AppendHeaderLine(stringBuilder, "Cc", entry.CcLine)
            If Date.Compare(entry.DateUtc, Date.MinValue) <> 0 Then
                Call AppendHeaderLine(stringBuilder, "Date", entry.DateUtc.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss'Z'"))
                Dim dateTime As Date = entry.DateUtc.ToUniversalTime()
                Dim dateTime2 As Date = New DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                Call AppendHeaderLine(stringBuilder, "X-LCARS-DateUnix", CLng(Math.Round((dateTime - dateTime2).TotalSeconds)).ToString())
            End If
            If entry.Uid <> 0 Then
                Call AppendHeaderLine(stringBuilder, "X-LCARS-Uid", entry.Uid.ToString())
            End If
            AppendHeaderLine(stringBuilder, "Message-Id", entry.MessageId)
            AppendHeaderLine(stringBuilder, "In-Reply-To", entry.InReplyTo)
            AppendHeaderLine(stringBuilder, "References", entry.ReferencesHeader)
            AppendHeaderLine(stringBuilder, "Reply-To", entry.ReplyToLine)
            AppendHeaderLine(stringBuilder, "List-Id", entry.ListIdLine)
            AppendHeaderLine(stringBuilder, "Return-Path", entry.ReturnPathLine)
            AppendHeaderLine(stringBuilder, "X-LCARS-Seen", If(entry.IsSeen, "read", "unread"))
            If entry.SizeBytes > 0 Then
                Call AppendHeaderLine(stringBuilder, "Size", entry.SizeBytes.ToString())
                Call AppendHeaderLine(stringBuilder, "X-LCARS-MessageSize", entry.SizeBytes.ToString())
            End If
            Dim text3 As String = If(Not Equals(entry.Folder, Nothing), entry.Folder, "").Trim()
            If text3.Length > 0 Then
                Dim value = text3
                Dim num = text3.LastIndexOf("/"c)
                If num >= 0 AndAlso num < text3.Length - 1 Then
                    value = text3.Substring(num + 1).Trim()
                End If
                AppendHeaderLine(stringBuilder, "Folder", value)
                AppendHeaderLine(stringBuilder, "X-LCARS-FolderPath", text3)
            End If
            Dim text4 = ResolveThreadRootToken(entry)
            If text4.Length > 0 Then
                AppendHeaderLine(stringBuilder, "X-LCARS-ThreadRoot", text4)
            End If
            If entry.AttachmentCount > 0 Then
                AppendHeaderLine(stringBuilder, "X-LCARS-HasAttachment", "yes")
                Call AppendHeaderLine(stringBuilder, "X-LCARS-AttachmentCount", entry.AttachmentCount.ToString())
            Else
                AppendHeaderLine(stringBuilder, "X-LCARS-HasAttachment", "no")
            End If
            Return stringBuilder.ToString()
        End Function

        Private Shared Function ResolveThreadRootToken(entry As IndexEntry) As String
            If entry Is Nothing Then
                Return ""
            End If
            Dim text = NormalizeMessageIdToken(entry.InReplyTo)
            If text.Length > 0 Then
                Return text
            End If
            Dim text2 As String = If(Not Equals(entry.ReferencesHeader, Nothing), entry.ReferencesHeader, "").Trim()
            If text2.Length > 0 Then
                Dim num = text2.IndexOf("<"c)
                If num >= 0 Then
                    Dim num2 = text2.IndexOf(">"c, num)
                    If num2 > num Then
                        Return text2.Substring(num, num2 - num + 1).Trim()
                    End If
                End If
                Dim num3 = text2.IndexOf(" "c)
                If num3 > 0 Then
                    Return text2.Substring(0, num3).Trim()
                End If
                Return text2
            End If
            Return NormalizeMessageIdToken(entry.MessageId)
        End Function

        Private Shared Function NormalizeMessageIdToken(raw As String) As String
            Dim text As String = If(Not Equals(raw, Nothing), raw, "").Trim()
            If text.Length = 0 Then
                Return ""
            End If
            If text.StartsWith("<") AndAlso text.EndsWith(">") Then
                Return text
            End If
            If text.Contains("@") Then
                Return "<" & text.Trim("<"c, ">"c) & ">"
            End If
            Return text
        End Function

        Private Shared Sub AppendHeaderLine(sb As StringBuilder, label As String, value As String)
            Dim text As String = If(Not Equals(value, Nothing), value, "").Trim()
            If text.Length <> 0 Then
                If sb.Length > 0 Then
                    sb.AppendLine()
                End If
                sb.Append(label).Append(": ").Append(text)
            End If
        End Sub

        Public Shared Function TryGetIndexedDateUtc(accountId As String, folder As String, uid As ULong) As Date
            If String.IsNullOrWhiteSpace(accountId) OrElse String.IsNullOrWhiteSpace(folder) Then
                Return Date.MinValue
            End If
            Dim dictionary = LoadMap(accountId)
            Dim key As String = folder & "|" & uid.ToString()
            If Not dictionary.ContainsKey(key) Then
                Return Date.MinValue
            End If
            Return dictionary(key).DateUtc
        End Function

        Public Shared Function CountEntriesNeedingBodyPreview(accountId As String) As Integer
            If String.IsNullOrWhiteSpace(accountId) Then
                Return 0
            End If
            Dim num = 0
            For Each item In LoadMap(accountId)
                Dim value = item.Value
                If value IsNot Nothing AndAlso String.IsNullOrWhiteSpace(value.BodyPreview) Then
                    num = num + 1
                End If
            Next
            Return num
        End Function

        Public Shared Function ListEntriesNeedingBodyPreview(accountId As String, Optional maxCount As Integer = 500) As List(Of IndexBodyTarget)
            Dim list As List(Of IndexBodyTarget) = New List(Of IndexBodyTarget)()
            If String.IsNullOrWhiteSpace(accountId) Then
                Return list
            End If
            Dim dictionary = LoadMap(accountId)
            For Each item In dictionary
                Dim value = item.Value
                If value IsNot Nothing AndAlso String.IsNullOrWhiteSpace(value.BodyPreview) Then
                    list.Add(New IndexBodyTarget With {
        .Folder = value.Folder,
        .Uid = value.Uid
    })
                    If maxCount > 0 AndAlso list.Count >= maxCount Then
                        Exit For
                    End If
                End If
            Next
            Return list
        End Function

        Public Shared Function ListEntriesWithBodyPreview(accountId As String, Optional maxCount As Integer = 64) As List(Of IndexBodyTarget)
            Dim list As List(Of IndexBodyTarget) = New List(Of IndexBodyTarget)()
            If String.IsNullOrWhiteSpace(accountId) Then
                Return list
            End If
            For Each item In LoadMap(accountId)
                Dim value = item.Value
                If value IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(value.BodyPreview) Then
                    list.Add(New IndexBodyTarget With {
        .Folder = value.Folder,
        .Uid = value.Uid
    })
                    If maxCount > 0 AndAlso list.Count >= maxCount Then
                        Exit For
                    End If
                End If
            Next
            Return list
        End Function

        Public Shared Sub RemoveMessage(accountId As String, folder As String, uid As ULong)
            If Not String.IsNullOrWhiteSpace(accountId) Then
                Dim dictionary = LoadMap(accountId)
                Dim key As String = If(Not Equals(folder, Nothing), folder, "") & "|" & uid.ToString()
                If dictionary.ContainsKey(key) Then
                    dictionary.Remove(key)
                    SaveMap(accountId, dictionary)
                End If
                MailSearchGlodaFts.RemoveMessage(accountId, folder, uid)
            End If
        End Sub

        Public Shared Function CountEntries(accountId As String) As Integer
            If String.IsNullOrWhiteSpace(accountId) Then
                Return 0
            End If
            Return LoadMap(accountId).Count
        End Function

        Public Shared Function CountDistinctIndexedFolders(accountId As String) As Integer
            If String.IsNullOrWhiteSpace(accountId) Then
                Return 0
            End If
            Dim hashSet As HashSet(Of String) = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            For Each item In LoadMap(accountId)
                Dim value = item.Value
                If value IsNot Nothing Then
                    Dim indexEntry = value
                    Dim text As String = If(Not Equals(indexEntry.Folder, Nothing), indexEntry.Folder, "").Trim()
                    If text.Length > 0 Then
                        hashSet.Add(text)
                    End If
                End If
            Next
            Return hashSet.Count
        End Function

        Public Shared Function TryGetBodyPreviewSnippet(accountId As String, folder As String, uid As ULong, Optional maxLen As Integer = 80) As String
            If String.IsNullOrWhiteSpace(accountId) Then
                Return ""
            End If
            Dim dictionary = LoadMap(accountId)
            Dim key As String = If(Not Equals(folder, Nothing), folder, "") & "|" & uid.ToString()
            If Not dictionary.ContainsKey(key) Then
                Return ""
            End If
            Dim indexEntry = dictionary(key)
            Dim text As String = If(Not Equals(indexEntry.BodyPreview, Nothing), indexEntry.BodyPreview, "").Trim()
            If text.Length = 0 Then
                Return ""
            End If
            text = text.Replace(vbCr, " ").Replace(vbLf, " ")
            If text.Length > maxLen Then
                text = text.Substring(0, maxLen - 1) & "…"
            End If
            Return text
        End Function

        Public Shared Function Search(accountId As String, queryText As String, Optional maxHits As Integer = 5000, Optional skip As Integer = 0) As List(Of MailMessageHeader)
            Dim list As List(Of MailMessageHeader) = New List(Of MailMessageHeader)()
            If String.IsNullOrWhiteSpace(accountId) Then
                Return list
            End If
            Dim text As String = If(Not Equals(queryText, Nothing), queryText, "").Trim()
            If text.Length = 0 Then
                Return list
            End If
            Dim queryText2 = text
            Dim text2 = ExtractFolderFilter(queryText2)
            Dim num = ExtractNewerDaysFilter(queryText2)
            Dim num2 = ExtractOlderDaysFilter(queryText2)
            Dim flag = ExtractUnreadOnlyFilter(queryText2)
            Dim flag2 = ExtractSeenOnlyFilter(queryText2)
            Dim flag3 = ExtractHasBodyFilter(queryText2)
            Dim flag4 = ExtractHasAttachmentFilter(queryText2)
            Dim num3 = ExtractLargerMinBytesFilter(queryText2)
            Dim num4 = ExtractSmallerMaxBytesFilter(queryText2)
            Dim text3 = ExtractToAddressFilter(queryText2)
            Dim text4 = ExtractCcAddressFilter(queryText2)
            Dim text5 = ExtractMessageIdFilter(queryText2)
            Dim text6 = ExtractThreadFilter(queryText2)
            Dim text7 = ExtractInReplyToFilter(queryText2)
            Dim text8 = ExtractReferencesFilter(queryText2)
            Dim text9 = ExtractReplyToFilter(queryText2)
            Dim text10 = ExtractListIdFilter(queryText2)
            Dim text11 = ExtractReturnPathFilter(queryText2)
            Dim list2 = MailSearchGlodaFts.Search(accountId, queryText2, maxHits, skip)
            If list2 IsNot Nothing AndAlso list2.Count > 0 Then
                Dim dictionary = LoadMap(accountId)
                For Each item In list2
                    If item Is Nothing Then
                        Continue For
                    End If
                    Dim mailMessageHeader = item
                    Dim key As String = If(Not Equals(mailMessageHeader.Folder, Nothing), mailMessageHeader.Folder, "") & "|" & item.Uid.ToString()
                    If dictionary.ContainsKey(key) Then
                        Dim indexEntry = dictionary(key)
                        item.DateUtc = indexEntry.DateUtc
                        If String.IsNullOrWhiteSpace(item.Subject) Then
                            item.Subject = indexEntry.Subject
                        End If
                        If String.IsNullOrWhiteSpace(item.From) Then
                            item.From = indexEntry.FromLine
                        End If
                        If String.IsNullOrWhiteSpace(item.ToAddresses) Then
                            item.ToAddresses = indexEntry.ToLine
                        End If
                        If String.IsNullOrWhiteSpace(item.CcAddresses) Then
                            item.CcAddresses = indexEntry.CcLine
                        End If
                        If String.IsNullOrWhiteSpace(item.MessageId) Then
                            item.MessageId = indexEntry.MessageId
                        End If
                        If String.IsNullOrWhiteSpace(item.InReplyTo) Then
                            item.InReplyTo = indexEntry.InReplyTo
                        End If
                        If String.IsNullOrWhiteSpace(item.References) Then
                            item.References = indexEntry.ReferencesHeader
                        End If
                        If String.IsNullOrWhiteSpace(item.ReplyTo) Then
                            item.ReplyTo = indexEntry.ReplyToLine
                        End If
                        If String.IsNullOrWhiteSpace(item.ListId) Then
                            item.ListId = indexEntry.ListIdLine
                        End If
                        If String.IsNullOrWhiteSpace(item.ReturnPath) Then
                            item.ReturnPath = indexEntry.ReturnPathLine
                        End If
                        item.IsSeen = indexEntry.IsSeen
                        If indexEntry.SizeBytes > 0 Then
                            item.SizeBytes = indexEntry.SizeBytes
                        End If
                    End If
                    If num3 > 0 OrElse num4 > 0 Then
                        Dim sizeBytes = item.SizeBytes
                        If sizeBytes <= 0 AndAlso dictionary.ContainsKey(key) Then
                            sizeBytes = dictionary(key).SizeBytes
                        End If
                        If num3 > 0 AndAlso sizeBytes < num3 OrElse num4 > 0 AndAlso sizeBytes > num4 Then
                            Continue For
                        End If
                    End If
                    If flag AndAlso item.IsSeen OrElse flag2 AndAlso Not item.IsSeen Then
                        Continue For
                    End If
                    If text2.Length > 0 Then
                        mailMessageHeader = item
                        If Not FolderMatches(If(Not Equals(mailMessageHeader.Folder, Nothing), mailMessageHeader.Folder, ""), text2) Then
                            Continue For
                        End If
                    End If
                    If text3.Length > 0 Then
                        mailMessageHeader = item
                        If If(Not Equals(mailMessageHeader.ToAddresses, Nothing), mailMessageHeader.ToAddresses, "").IndexOf(text3, StringComparison.OrdinalIgnoreCase) < 0 Then
                            Continue For
                        End If
                    End If
                    If text4.Length > 0 Then
                        mailMessageHeader = item
                        If If(Not Equals(mailMessageHeader.CcAddresses, Nothing), mailMessageHeader.CcAddresses, "").IndexOf(text4, StringComparison.OrdinalIgnoreCase) < 0 Then
                            Continue For
                        End If
                    End If
                    If text5.Length > 0 Then
                        mailMessageHeader = item
                        If If(Not Equals(mailMessageHeader.MessageId, Nothing), mailMessageHeader.MessageId, "").IndexOf(text5, StringComparison.OrdinalIgnoreCase) < 0 Then
                            Continue For
                        End If
                    End If
                    If text6.Length > 0 Then
                        mailMessageHeader = item
                        Dim key2 As String = If(Not Equals(mailMessageHeader.Folder, Nothing), mailMessageHeader.Folder, "") & "|" & item.Uid.ToString()
                        Dim entry As IndexEntry = Nothing
                        If dictionary.ContainsKey(key2) Then
                            entry = dictionary(key2)
                        End If
                        If Not HeaderMatchesThreadFilter(item, entry, text6) Then
                            Continue For
                        End If
                    End If
                    If text7.Length > 0 Then
                        mailMessageHeader = item
                        If If(Not Equals(mailMessageHeader.InReplyTo, Nothing), mailMessageHeader.InReplyTo, "").IndexOf(text7, StringComparison.OrdinalIgnoreCase) < 0 Then
                            Continue For
                        End If
                    End If
                    If text8.Length > 0 Then
                        mailMessageHeader = item
                        If If(Not Equals(mailMessageHeader.References, Nothing), mailMessageHeader.References, "").IndexOf(text8, StringComparison.OrdinalIgnoreCase) < 0 Then
                            Continue For
                        End If
                    End If
                    If text9.Length > 0 Then
                        mailMessageHeader = item
                        If If(Not Equals(mailMessageHeader.ReplyTo, Nothing), mailMessageHeader.ReplyTo, "").IndexOf(text9, StringComparison.OrdinalIgnoreCase) < 0 Then
                            Continue For
                        End If
                    End If
                    If text10.Length > 0 Then
                        mailMessageHeader = item
                        If If(Not Equals(mailMessageHeader.ListId, Nothing), mailMessageHeader.ListId, "").IndexOf(text10, StringComparison.OrdinalIgnoreCase) < 0 Then
                            Continue For
                        End If
                    End If
                    If text11.Length > 0 Then
                        mailMessageHeader = item
                        If If(Not Equals(mailMessageHeader.ReturnPath, Nothing), mailMessageHeader.ReturnPath, "").IndexOf(text11, StringComparison.OrdinalIgnoreCase) < 0 Then
                            Continue For
                        End If
                    End If
                    If (Not flag3 OrElse dictionary.ContainsKey(key) AndAlso Not String.IsNullOrWhiteSpace(dictionary(key).BodyPreview)) AndAlso (Not flag4 OrElse dictionary.ContainsKey(key) AndAlso dictionary(key).AttachmentCount > 0) AndAlso (num <= 0 OrElse Date.Compare(item.DateUtc, Date.MinValue) = 0 OrElse Date.Compare(item.DateUtc, Date.UtcNow.AddDays(-num)) >= 0) AndAlso (num2 <= 0 OrElse Date.Compare(item.DateUtc, Date.MinValue) = 0 OrElse Date.Compare(item.DateUtc, Date.UtcNow.AddDays(-num2)) <= 0) Then
                        list.Add(item)
                        If maxHits > 0 AndAlso list.Count >= maxHits Then
                            Exit For
                        End If
                    End If
                Next
                If list.Count > 0 Then
                    list.Sort(Function(a, b) Date.Compare(b.DateUtc, a.DateUtc))
                    Return list
                End If
            End If
            Dim orGroups = ParseGlodaOrGroups(queryText2)
            Dim dictionary2 = LoadMap(accountId)
            For Each item2 In dictionary2
                Dim value = item2.Value
                If value Is Nothing OrElse Not GlodaEntryMatches(value, orGroups) OrElse flag AndAlso value.IsSeen OrElse flag2 AndAlso Not value.IsSeen Then
                    Continue For
                End If
                If text2.Length > 0 Then
                    Dim indexEntry2 = value
                    If Not FolderMatches(If(Not Equals(indexEntry2.Folder, Nothing), indexEntry2.Folder, ""), text2) Then
                        Continue For
                    End If
                End If
                If text3.Length > 0 Then
                    Dim indexEntry2 = value
                    If If(Not Equals(indexEntry2.ToLine, Nothing), indexEntry2.ToLine, "").IndexOf(text3, StringComparison.OrdinalIgnoreCase) < 0 Then
                        Continue For
                    End If
                End If
                If text4.Length > 0 Then
                    Dim indexEntry2 = value
                    If If(Not Equals(indexEntry2.CcLine, Nothing), indexEntry2.CcLine, "").IndexOf(text4, StringComparison.OrdinalIgnoreCase) < 0 Then
                        Continue For
                    End If
                End If
                If text5.Length > 0 Then
                    Dim indexEntry2 = value
                    If If(Not Equals(indexEntry2.MessageId, Nothing), indexEntry2.MessageId, "").IndexOf(text5, StringComparison.OrdinalIgnoreCase) < 0 Then
                        Continue For
                    End If
                End If
                If text6.Length > 0 AndAlso Not EntryMatchesThreadFilter(value, text6) Then
                    Continue For
                End If
                If text7.Length > 0 Then
                    Dim indexEntry2 = value
                    If If(Not Equals(indexEntry2.InReplyTo, Nothing), indexEntry2.InReplyTo, "").IndexOf(text7, StringComparison.OrdinalIgnoreCase) < 0 Then
                        Continue For
                    End If
                End If
                If text8.Length > 0 Then
                    Dim indexEntry2 = value
                    If If(Not Equals(indexEntry2.ReferencesHeader, Nothing), indexEntry2.ReferencesHeader, "").IndexOf(text8, StringComparison.OrdinalIgnoreCase) < 0 Then
                        Continue For
                    End If
                End If
                If text9.Length > 0 Then
                    Dim indexEntry2 = value
                    If If(Not Equals(indexEntry2.ReplyToLine, Nothing), indexEntry2.ReplyToLine, "").IndexOf(text9, StringComparison.OrdinalIgnoreCase) < 0 Then
                        Continue For
                    End If
                End If
                If text10.Length > 0 Then
                    Dim indexEntry2 = value
                    If If(Not Equals(indexEntry2.ListIdLine, Nothing), indexEntry2.ListIdLine, "").IndexOf(text10, StringComparison.OrdinalIgnoreCase) < 0 Then
                        Continue For
                    End If
                End If
                If text11.Length > 0 Then
                    Dim indexEntry2 = value
                    If If(Not Equals(indexEntry2.ReturnPathLine, Nothing), indexEntry2.ReturnPathLine, "").IndexOf(text11, StringComparison.OrdinalIgnoreCase) < 0 Then
                        Continue For
                    End If
                End If
                If (Not flag3 OrElse Not String.IsNullOrWhiteSpace(value.BodyPreview)) AndAlso (Not flag4 OrElse value.AttachmentCount > 0) AndAlso (num3 <= 0 OrElse value.SizeBytes >= num3) AndAlso (num4 <= 0 OrElse value.SizeBytes <= num4) AndAlso (num <= 0 OrElse Date.Compare(value.DateUtc, Date.MinValue) = 0 OrElse Date.Compare(value.DateUtc, Date.UtcNow.AddDays(-num)) >= 0) AndAlso (num2 <= 0 OrElse Date.Compare(value.DateUtc, Date.MinValue) = 0 OrElse Date.Compare(value.DateUtc, Date.UtcNow.AddDays(-num2)) <= 0) Then
                    list.Add(New MailMessageHeader With {
    .Folder = value.Folder,
    .Uid = value.Uid,
    .Subject = value.Subject,
    .From = value.FromLine,
    .ToAddresses = value.ToLine,
    .CcAddresses = value.CcLine,
    .MessageId = value.MessageId,
    .InReplyTo = value.InReplyTo,
    .References = value.ReferencesHeader,
    .ReplyTo = value.ReplyToLine,
    .ListId = value.ListIdLine,
    .ReturnPath = value.ReturnPathLine,
    .DateUtc = value.DateUtc,
    .IsSeen = value.IsSeen,
    .SizeBytes = value.SizeBytes
})
                    If maxHits > 0 AndAlso list.Count >= maxHits Then
                        Exit For
                    End If
                End If
            Next
            list.Sort(Function(a, b) Date.Compare(b.DateUtc, a.DateUtc))
            Return list
        End Function

        Private Shared Function LoadMap(accountId As String) As Dictionary(Of String, IndexEntry)
            Dim path = IndexPath(accountId)
            Dim result As Dictionary(Of String, IndexEntry)
            If Not File.Exists(path) Then
                result = New Dictionary(Of String, IndexEntry)(StringComparer.OrdinalIgnoreCase)
            Else
                Try
                    Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                    Dim list = javaScriptSerializer.Deserialize(Of List(Of IndexEntry))(File.ReadAllText(path, Encoding.UTF8))
                    Dim dictionary As Dictionary(Of String, IndexEntry) = New Dictionary(Of String, IndexEntry)(StringComparer.OrdinalIgnoreCase)
                    If list Is Nothing Then
                        result = dictionary
                    Else
                        For Each item In list
                            If item IsNot Nothing Then
                                Dim indexEntry = item
                                Dim key As String = If(Not Equals(indexEntry.Folder, Nothing), indexEntry.Folder, "") & "|" & item.Uid.ToString()
                                dictionary(key) = item
                            End If
                        Next
                        result = dictionary
                    End If
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    result = New Dictionary(Of String, IndexEntry)(StringComparer.OrdinalIgnoreCase)
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Private Shared Sub SaveMap(accountId As String, map As Dictionary(Of String, IndexEntry))
            Dim obj As List(Of IndexEntry) = New List(Of IndexEntry)(map.Values)
            Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
            File.WriteAllText(IndexPath(accountId), javaScriptSerializer.Serialize(obj), Encoding.UTF8)
        End Sub

        Friend Shared Function ParseGlodaOrGroups(queryText As String) As List(Of String())
            Dim list As List(Of String()) = New List(Of String())()
            If String.IsNullOrWhiteSpace(queryText) Then
                list.Add(New String(-1) {})
                Return list
            End If
            Dim array = queryText.Split(New String(2) {" OR ", " or ", " Or "}, StringSplitOptions.None)
            Dim array2 = array
            For Each segment In array2
                Dim list2 = TokenizeGlodaTerms(segment)
                list.Add(list2.ToArray())
            Next
            If list.Count = 0 Then
                list.Add(New String(-1) {})
            End If
            Return list
        End Function

        Private Shared Function TokenizeGlodaTerms(segment As String) As List(Of String)
            Dim list As List(Of String) = New List(Of String)()
            Dim text As String = If(Not Equals(segment, Nothing), segment, "").Trim()
            Dim i = 0
            While i < text.Length
                If text(i) = """"c Then
                    Dim num = text.IndexOf(""""c, i + 1)
                    If num > i Then
                        list.Add(text.Substring(i + 1, num - i - 1).ToLowerInvariant())
                        i = num + 1
                        Continue While
                    End If
                End If
                While i < text.Length AndAlso (text(i) = " "c OrElse text(i) = ChrW(9))
                    i += 1
                End While
                If i >= text.Length Then
                    Exit While
                End If
                Dim num2 = i
                While i < text.Length AndAlso text(i) <> " "c AndAlso text(i) <> ChrW(9)
                    i += 1
                End While
                list.Add(text.Substring(num2, i - num2).ToLowerInvariant())
            End While
            Return list
        End Function

        Private Shared Function GlodaEntryMatches(entry As IndexEntry, orGroups As List(Of String())) As Boolean
            If orGroups Is Nothing OrElse orGroups.Count = 0 Then
                Return True
            End If
            For Each orGroup In orGroups
                If EntryMatchesTerms(entry, orGroup) Then
                    Return True
                End If
            Next
            Return False
        End Function

        Private Shared Function EntryMatchesTerms(entry As IndexEntry, terms As String()) As Boolean
            If terms Is Nothing OrElse terms.Length = 0 Then
                Return True
            End If
            Dim array = New String(8) {}
            array(0) = If(Not Equals(entry.Subject, Nothing), entry.Subject, "")
            array(1) = " "
            array(2) = If(Not Equals(entry.FromLine, Nothing), entry.FromLine, "")
            array(3) = " "
            array(4) = If(Not Equals(entry.ToLine, Nothing), entry.ToLine, "")
            array(5) = " "
            array(6) = If(Not Equals(entry.CcLine, Nothing), entry.CcLine, "")
            array(7) = " "
            Dim indexEntry = entry
            array(8) = If(Not Equals(indexEntry.BodyPreview, Nothing), indexEntry.BodyPreview, "")
            Dim text As String = String.Concat(array).ToLowerInvariant()
            For Each text2 In terms
                Dim text3 As String = If(Not Equals(text2, Nothing), text2, "").Trim().ToLowerInvariant()
                If text3.Length = 0 Then
                    Continue For
                End If
                If text3.StartsWith("from:", StringComparison.Ordinal) Then
                    Dim text4 As String = text3.Substring(5).Trim()
                    If text4.Length <> 0 Then
                        indexEntry = entry
                        If If(Not Equals(indexEntry.FromLine, Nothing), indexEntry.FromLine, "").ToLowerInvariant().IndexOf(text4, StringComparison.Ordinal) < 0 Then
                            Return False
                        End If
                    End If
                ElseIf text3.StartsWith("to:", StringComparison.Ordinal) Then
                    Dim text5 As String = text3.Substring(3).Trim()
                    If text5.Length <> 0 Then
                        indexEntry = entry
                        If If(Not Equals(indexEntry.ToLine, Nothing), indexEntry.ToLine, "").ToLowerInvariant().IndexOf(text5, StringComparison.Ordinal) < 0 Then
                            Return False
                        End If
                    End If
                ElseIf text3.StartsWith("cc:", StringComparison.Ordinal) Then
                    Dim text6 As String = text3.Substring(3).Trim()
                    If text6.Length <> 0 Then
                        indexEntry = entry
                        If If(Not Equals(indexEntry.CcLine, Nothing), indexEntry.CcLine, "").ToLowerInvariant().IndexOf(text6, StringComparison.Ordinal) < 0 Then
                            Return False
                        End If
                    End If
                ElseIf text3.StartsWith("messageid:", StringComparison.Ordinal) Then
                    Dim text7 As String = text3.Substring(10).Trim()
                    If text7.Length <> 0 Then
                        indexEntry = entry
                        If If(Not Equals(indexEntry.MessageId, Nothing), indexEntry.MessageId, "").ToLowerInvariant().IndexOf(text7, StringComparison.Ordinal) < 0 Then
                            Return False
                        End If
                    End If
                ElseIf text3.StartsWith("thread:", StringComparison.Ordinal) OrElse text3.StartsWith("threadroot:", StringComparison.Ordinal) Then
                    Dim text8 = text3
                    text8 = (If(Not text8.StartsWith("threadroot:", StringComparison.Ordinal), text8.Substring(7).Trim(), text8.Substring(11).Trim()))
                    If text8.Length <> 0 AndAlso Not EntryMatchesThreadFilter(entry, text8) Then
                        Return False
                    End If
                ElseIf text3.StartsWith("inreplyto:", StringComparison.Ordinal) Then
                    Dim text9 As String = text3.Substring(10).Trim()
                    If text9.Length <> 0 Then
                        indexEntry = entry
                        If If(Not Equals(indexEntry.InReplyTo, Nothing), indexEntry.InReplyTo, "").ToLowerInvariant().IndexOf(text9, StringComparison.Ordinal) < 0 Then
                            Return False
                        End If
                    End If
                ElseIf text3.StartsWith("references:", StringComparison.Ordinal) Then
                    Dim text10 As String = text3.Substring(11).Trim()
                    If text10.Length <> 0 Then
                        indexEntry = entry
                        If If(Not Equals(indexEntry.ReferencesHeader, Nothing), indexEntry.ReferencesHeader, "").ToLowerInvariant().IndexOf(text10, StringComparison.Ordinal) < 0 Then
                            Return False
                        End If
                    End If
                ElseIf text3.StartsWith("replyto:", StringComparison.Ordinal) Then
                    Dim text11 As String = text3.Substring(8).Trim()
                    If text11.Length <> 0 Then
                        indexEntry = entry
                        If If(Not Equals(indexEntry.ReplyToLine, Nothing), indexEntry.ReplyToLine, "").ToLowerInvariant().IndexOf(text11, StringComparison.Ordinal) < 0 Then
                            Return False
                        End If
                    End If
                ElseIf text3.StartsWith("list-id:", StringComparison.Ordinal) Then
                    Dim text12 As String = text3.Substring(8).Trim()
                    If text12.Length <> 0 Then
                        indexEntry = entry
                        If If(Not Equals(indexEntry.ListIdLine, Nothing), indexEntry.ListIdLine, "").ToLowerInvariant().IndexOf(text12, StringComparison.Ordinal) < 0 Then
                            Return False
                        End If
                    End If
                ElseIf text3.StartsWith("return-path:", StringComparison.Ordinal) Then
                    Dim text13 As String = text3.Substring(12).Trim()
                    If text13.Length <> 0 Then
                        indexEntry = entry
                        If If(Not Equals(indexEntry.ReturnPathLine, Nothing), indexEntry.ReturnPathLine, "").ToLowerInvariant().IndexOf(text13, StringComparison.Ordinal) < 0 Then
                            Return False
                        End If
                    End If
                ElseIf text3.StartsWith("subject:", StringComparison.Ordinal) Then
                    Dim text14 As String = text3.Substring(8).Trim()
                    If text14.Length <> 0 Then
                        indexEntry = entry
                        If If(Not Equals(indexEntry.Subject, Nothing), indexEntry.Subject, "").ToLowerInvariant().IndexOf(text14, StringComparison.Ordinal) < 0 Then
                            Return False
                        End If
                    End If
                ElseIf text3.StartsWith("body:", StringComparison.Ordinal) Then
                    Dim text15 As String = text3.Substring(5).Trim()
                    If text15.Length <> 0 Then
                        indexEntry = entry
                        If If(Not Equals(indexEntry.BodyPreview, Nothing), indexEntry.BodyPreview, "").ToLowerInvariant().IndexOf(text15, StringComparison.Ordinal) < 0 Then
                            Return False
                        End If
                    End If
                ElseIf text3.Equals("has:attachment", StringComparison.Ordinal) OrElse text3.Equals("has:attachments", StringComparison.Ordinal) OrElse text3.Equals("attachment:yes", StringComparison.Ordinal) Then
                    If entry.AttachmentCount <= 0 Then
                        Return False
                    End If
                ElseIf text.IndexOf(text3, StringComparison.Ordinal) < 0 Then
                    Return False
                End If
            Next
            Return True
        End Function

        Private Shared Function FolderMatches(folderPath As String, filterPattern As String) As Boolean
            Dim text = If(Not Equals(folderPath, Nothing), folderPath, "")
            Dim text2 = If(Not Equals(filterPattern, Nothing), filterPattern, "")
            If text2.Length = 0 Then
                Return True
            End If
            Return text.IndexOf(text2, StringComparison.OrdinalIgnoreCase) >= 0
        End Function

        Private Shared Function ExtractToAddressFilter(ByRef queryText As String) As String
            Dim text As String = If(Not Equals(queryText, Nothing), queryText, "").Trim()
            If text.Length = 0 Then
                Return ""
            End If
            Dim array = text.Split(New Char(1) {" "c, ChrW(9)}, StringSplitOptions.RemoveEmptyEntries)
            Dim list As List(Of String) = New List(Of String)()
            Dim result = ""
            Dim array2 = array
            For Each text2 In array2
                If text2.StartsWith("to:", StringComparison.OrdinalIgnoreCase) Then
                    result = text2.Substring(3).Trim()
                Else
                    list.Add(text2)
                End If
            Next
            queryText = String.Join(" ", list)
            Return result
        End Function

        Private Shared Function ExtractCcAddressFilter(ByRef queryText As String) As String
            Dim text As String = If(Not Equals(queryText, Nothing), queryText, "").Trim()
            If text.Length = 0 Then
                Return ""
            End If
            Dim array = text.Split(New Char(1) {" "c, ChrW(9)}, StringSplitOptions.RemoveEmptyEntries)
            Dim list As List(Of String) = New List(Of String)()
            Dim result = ""
            Dim array2 = array
            For Each text2 In array2
                If text2.StartsWith("cc:", StringComparison.OrdinalIgnoreCase) Then
                    result = text2.Substring(3).Trim()
                Else
                    list.Add(text2)
                End If
            Next
            queryText = String.Join(" ", list)
            Return result
        End Function

        Private Shared Function ExtractMessageIdFilter(ByRef queryText As String) As String
            Dim text As String = If(Not Equals(queryText, Nothing), queryText, "").Trim()
            If text.Length = 0 Then
                Return ""
            End If
            Dim array = text.Split(New Char(1) {" "c, ChrW(9)}, StringSplitOptions.RemoveEmptyEntries)
            Dim list As List(Of String) = New List(Of String)()
            Dim result = ""
            Dim array2 = array
            For Each text2 In array2
                If text2.StartsWith("messageid:", StringComparison.OrdinalIgnoreCase) Then
                    result = text2.Substring(10).Trim()
                Else
                    list.Add(text2)
                End If
            Next
            queryText = String.Join(" ", list)
            Return result
        End Function

        Private Shared Function ExtractThreadFilter(ByRef queryText As String) As String
            Dim text As String = If(Not Equals(queryText, Nothing), queryText, "").Trim()
            If text.Length = 0 Then
                Return ""
            End If
            Dim array = text.Split(New Char(1) {" "c, ChrW(9)}, StringSplitOptions.RemoveEmptyEntries)
            Dim list As List(Of String) = New List(Of String)()
            Dim result = ""
            Dim array2 = array
            For Each text2 In array2
                If text2.StartsWith("thread:", StringComparison.OrdinalIgnoreCase) Then
                    result = text2.Substring(7).Trim()
                ElseIf text2.StartsWith("threadroot:", StringComparison.OrdinalIgnoreCase) Then
                    result = text2.Substring(11).Trim()
                Else
                    list.Add(text2)
                End If
            Next
            queryText = String.Join(" ", list)
            Return result
        End Function

        Private Shared Function EntryMatchesThreadFilter(entry As IndexEntry, threadFilter As String) As Boolean
            If entry Is Nothing Then
                Return False
            End If
            Dim text = NormalizeMessageIdToken(threadFilter)
            If text.Length = 0 Then
                text = If(Not Equals(threadFilter, Nothing), threadFilter, "").Trim()
            End If
            If text.Length = 0 Then
                Return True
            End If
            Dim text2 = ResolveThreadRootToken(entry)
            If text2.Length > 0 AndAlso TokenMatchesThreadNeedle(text2, text) Then
                Return True
            End If
            Dim indexEntry = entry
            If TokenMatchesThreadNeedle(If(Not Equals(indexEntry.MessageId, Nothing), indexEntry.MessageId, ""), text) Then
                Return True
            End If
            indexEntry = entry
            If TokenMatchesThreadNeedle(If(Not Equals(indexEntry.InReplyTo, Nothing), indexEntry.InReplyTo, ""), text) Then
                Return True
            End If
            indexEntry = entry
            Dim text3 = If(Not Equals(indexEntry.ReferencesHeader, Nothing), indexEntry.ReferencesHeader, "")
            If text3.Length > 0 AndAlso text3.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0 Then
                Return True
            End If
            Return False
        End Function

        Private Shared Function HeaderMatchesThreadFilter(hdr As MailMessageHeader, entry As IndexEntry, threadFilter As String) As Boolean
            If entry IsNot Nothing Then
                Return EntryMatchesThreadFilter(entry, threadFilter)
            End If
            If hdr Is Nothing Then
                Return False
            End If
            Dim text = NormalizeMessageIdToken(threadFilter)
            If text.Length = 0 Then
                text = If(Not Equals(threadFilter, Nothing), threadFilter, "").Trim()
            End If
            If text.Length = 0 Then
                Return True
            End If
            Dim mailMessageHeader = hdr
            If TokenMatchesThreadNeedle(If(Not Equals(mailMessageHeader.MessageId, Nothing), mailMessageHeader.MessageId, ""), text) Then
                Return True
            End If
            mailMessageHeader = hdr
            If TokenMatchesThreadNeedle(If(Not Equals(mailMessageHeader.InReplyTo, Nothing), mailMessageHeader.InReplyTo, ""), text) Then
                Return True
            End If
            mailMessageHeader = hdr
            Dim text2 = If(Not Equals(mailMessageHeader.References, Nothing), mailMessageHeader.References, "")
            If text2.Length > 0 AndAlso text2.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0 Then
                Return True
            End If
            Return False
        End Function

        Private Shared Function TokenMatchesThreadNeedle(rawToken As String, needle As String) As Boolean
            Dim text = NormalizeMessageIdToken(rawToken)
            If text.Length = 0 Then
                text = If(Not Equals(rawToken, Nothing), rawToken, "").Trim()
            End If
            If text.Length = 0 Then
                Return False
            End If
            If String.Equals(text, needle, StringComparison.OrdinalIgnoreCase) Then
                Return True
            End If
            Return text.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0
        End Function

        Private Shared Function ExtractInReplyToFilter(ByRef queryText As String) As String
            Dim text As String = If(Not Equals(queryText, Nothing), queryText, "").Trim()
            If text.Length = 0 Then
                Return ""
            End If
            Dim array = text.Split(New Char(1) {" "c, ChrW(9)}, StringSplitOptions.RemoveEmptyEntries)
            Dim list As List(Of String) = New List(Of String)()
            Dim result = ""
            Dim array2 = array
            For Each text2 In array2
                If text2.StartsWith("inreplyto:", StringComparison.OrdinalIgnoreCase) Then
                    result = text2.Substring(10).Trim()
                Else
                    list.Add(text2)
                End If
            Next
            queryText = String.Join(" ", list)
            Return result
        End Function

        Private Shared Function ExtractReferencesFilter(ByRef queryText As String) As String
            Dim text As String = If(Not Equals(queryText, Nothing), queryText, "").Trim()
            If text.Length = 0 Then
                Return ""
            End If
            Dim array = text.Split(New Char(1) {" "c, ChrW(9)}, StringSplitOptions.RemoveEmptyEntries)
            Dim list As List(Of String) = New List(Of String)()
            Dim result = ""
            Dim array2 = array
            For Each text2 In array2
                If text2.StartsWith("references:", StringComparison.OrdinalIgnoreCase) Then
                    result = text2.Substring(11).Trim()
                Else
                    list.Add(text2)
                End If
            Next
            queryText = String.Join(" ", list)
            Return result
        End Function

        Private Shared Function ExtractReplyToFilter(ByRef queryText As String) As String
            Dim text As String = If(Not Equals(queryText, Nothing), queryText, "").Trim()
            If text.Length = 0 Then
                Return ""
            End If
            Dim array = text.Split(New Char(1) {" "c, ChrW(9)}, StringSplitOptions.RemoveEmptyEntries)
            Dim list As List(Of String) = New List(Of String)()
            Dim result = ""
            Dim array2 = array
            For Each text2 In array2
                If text2.StartsWith("replyto:", StringComparison.OrdinalIgnoreCase) Then
                    result = text2.Substring(8).Trim()
                Else
                    list.Add(text2)
                End If
            Next
            queryText = String.Join(" ", list)
            Return result
        End Function

        Private Shared Function ExtractListIdFilter(ByRef queryText As String) As String
            Dim text As String = If(Not Equals(queryText, Nothing), queryText, "").Trim()
            If text.Length = 0 Then
                Return ""
            End If
            Dim array = text.Split(New Char(1) {" "c, ChrW(9)}, StringSplitOptions.RemoveEmptyEntries)
            Dim list As List(Of String) = New List(Of String)()
            Dim result = ""
            Dim array2 = array
            For Each text2 In array2
                If text2.StartsWith("list-id:", StringComparison.OrdinalIgnoreCase) Then
                    result = text2.Substring(8).Trim()
                Else
                    list.Add(text2)
                End If
            Next
            queryText = String.Join(" ", list)
            Return result
        End Function

        Private Shared Function ExtractReturnPathFilter(ByRef queryText As String) As String
            Dim text As String = If(Not Equals(queryText, Nothing), queryText, "").Trim()
            If text.Length = 0 Then
                Return ""
            End If
            Dim array = text.Split(New Char(1) {" "c, ChrW(9)}, StringSplitOptions.RemoveEmptyEntries)
            Dim list As List(Of String) = New List(Of String)()
            Dim result = ""
            Dim array2 = array
            For Each text2 In array2
                If text2.StartsWith("return-path:", StringComparison.OrdinalIgnoreCase) Then
                    result = text2.Substring(12).Trim()
                Else
                    list.Add(text2)
                End If
            Next
            queryText = String.Join(" ", list)
            Return result
        End Function

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
                    result = text2.Substring(7).Trim()
                Else
                    list.Add(text2)
                End If
            Next
            queryText = String.Join(" ", list)
            Return result
        End Function

        Private Shared Function ExtractSmallerMaxBytesFilter(ByRef queryText As String) As Long
            Dim text As String = If(Not Equals(queryText, Nothing), queryText, "").Trim()
            If text.Length = 0 Then
                Return 0L
            End If
            Dim array = text.Split(New Char(1) {" "c, ChrW(9)}, StringSplitOptions.RemoveEmptyEntries)
            Dim list As List(Of String) = New List(Of String)()
            Dim result = 0L
            Dim array2 = array
            For Each text2 In array2
                Dim text3 As String = text2.ToLowerInvariant()
                If text3.StartsWith("smaller:", StringComparison.Ordinal) Then
                    result = ParseSizeTokenToBytes(text2.Substring(8).Trim())
                Else
                    list.Add(text2)
                End If
            Next
            queryText = String.Join(" ", list)
            Return result
        End Function

        Private Shared Function ExtractLargerMinBytesFilter(ByRef queryText As String) As Long
            Dim text As String = If(Not Equals(queryText, Nothing), queryText, "").Trim()
            If text.Length = 0 Then
                Return 0L
            End If
            Dim array = text.Split(New Char(1) {" "c, ChrW(9)}, StringSplitOptions.RemoveEmptyEntries)
            Dim list As List(Of String) = New List(Of String)()
            Dim result = 0L
            Dim array2 = array
            For Each text2 In array2
                Dim text3 As String = text2.ToLowerInvariant()
                If text3.StartsWith("larger:", StringComparison.Ordinal) Then
                    Dim token As String = text2.Substring(7).Trim()
                    result = ParseSizeTokenToBytes(token)
                Else
                    list.Add(text2)
                End If
            Next
            queryText = String.Join(" ", list)
            Return result
        End Function

        Private Shared Function ParseSizeTokenToBytes(token As String) As Long
            Dim text As String = If(Not Equals(token, Nothing), token, "").Trim().ToLowerInvariant()
            If text.Length = 0 Then
                Return 0L
            End If
            Dim num = 1L
            If text.EndsWith("k", StringComparison.Ordinal) Then
                num = 1024L
                text = text.Substring(0, text.Length - 1)
            ElseIf text.EndsWith("m", StringComparison.Ordinal) Then
                num = 1048576L
                text = text.Substring(0, text.Length - 1)
            ElseIf text.EndsWith("d", StringComparison.OrdinalIgnoreCase) Then
                text = text.Substring(0, text.Length - 1)
            End If
            Dim result = 0L
            If Long.TryParse(text, result) AndAlso result > 0 Then
                Return result * num
            End If
            Return 0L
        End Function

        Private Shared Function ExtractSeenOnlyFilter(ByRef queryText As String) As Boolean
            Dim text As String = If(Not Equals(queryText, Nothing), queryText, "").Trim()
            If text.Length = 0 Then
                Return False
            End If
            Dim array = text.Split(New Char(1) {" "c, ChrW(9)}, StringSplitOptions.RemoveEmptyEntries)
            Dim list As List(Of String) = New List(Of String)()
            Dim result = False
            Dim array2 = array
            For Each text2 In array2
                If text2.Equals("read:1", StringComparison.OrdinalIgnoreCase) OrElse text2.Equals("read:true", StringComparison.OrdinalIgnoreCase) OrElse text2.Equals("is:read", StringComparison.OrdinalIgnoreCase) Then
                    result = True
                Else
                    list.Add(text2)
                End If
            Next
            queryText = String.Join(" ", list)
            Return result
        End Function

        Private Shared Function ExtractHasBodyFilter(ByRef queryText As String) As Boolean
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

        Private Shared Function ExtractHasAttachmentFilter(ByRef queryText As String) As Boolean
            Dim text As String = If(Not Equals(queryText, Nothing), queryText, "").Trim()
            If text.Length = 0 Then
                Return False
            End If
            Dim array = text.Split(New Char(1) {" "c, ChrW(9)}, StringSplitOptions.RemoveEmptyEntries)
            Dim list As List(Of String) = New List(Of String)()
            Dim result = False
            Dim array2 = array
            For Each text2 In array2
                If text2.Equals("has:attachment", StringComparison.OrdinalIgnoreCase) OrElse text2.Equals("has:attachments", StringComparison.OrdinalIgnoreCase) OrElse text2.Equals("attachment:yes", StringComparison.OrdinalIgnoreCase) Then
                    result = True
                Else
                    list.Add(text2)
                End If
            Next
            queryText = String.Join(" ", list)
            Return result
        End Function

        Private Shared Function ExtractUnreadOnlyFilter(ByRef queryText As String) As Boolean
            Dim text As String = If(Not Equals(queryText, Nothing), queryText, "").Trim()
            If text.Length = 0 Then
                Return False
            End If
            Dim array = text.Split(New Char(1) {" "c, ChrW(9)}, StringSplitOptions.RemoveEmptyEntries)
            Dim list As List(Of String) = New List(Of String)()
            Dim result = False
            Dim array2 = array
            For Each text2 In array2
                If text2.Equals("unread:1", StringComparison.OrdinalIgnoreCase) OrElse text2.Equals("unread:true", StringComparison.OrdinalIgnoreCase) OrElse text2.Equals("is:unread", StringComparison.OrdinalIgnoreCase) Then
                    result = True
                Else
                    list.Add(text2)
                End If
            Next
            queryText = String.Join(" ", list)
            Return result
        End Function

        Private Shared Function ExtractOlderDaysFilter(ByRef queryText As String) As Integer
            Dim text As String = If(Not Equals(queryText, Nothing), queryText, "").Trim()
            If text.Length = 0 Then
                Return 0
            End If
            Dim array = text.Split(New Char(1) {" "c, ChrW(9)}, StringSplitOptions.RemoveEmptyEntries)
            Dim list As List(Of String) = New List(Of String)()
            Dim result = 0
            Dim array2 = array
            For Each text2 In array2
                Dim text3 As String = text2.ToLowerInvariant()
                If text3.StartsWith("older:", StringComparison.Ordinal) Then
                    Dim text4 As String = text2.Substring(6).Trim()
                    If text4.EndsWith("d", StringComparison.OrdinalIgnoreCase) Then
                        text4 = text4.Substring(0, text4.Length - 1)
                    End If
                    Dim result2 = 0
                    If Integer.TryParse(text4, result2) AndAlso result2 > 0 Then
                        result = result2
                    End If
                Else
                    list.Add(text2)
                End If
            Next
            queryText = String.Join(" ", list)
            Return result
        End Function

        Private Shared Function ExtractNewerDaysFilter(ByRef queryText As String) As Integer
            Dim text As String = If(Not Equals(queryText, Nothing), queryText, "").Trim()
            If text.Length = 0 Then
                Return 0
            End If
            Dim array = text.Split(New Char(1) {" "c, ChrW(9)}, StringSplitOptions.RemoveEmptyEntries)
            Dim list As List(Of String) = New List(Of String)()
            Dim result = 0
            Dim array2 = array
            For Each text2 In array2
                Dim text3 As String = text2.ToLowerInvariant()
                If text3.StartsWith("newer:", StringComparison.Ordinal) Then
                    Dim text4 As String = text2.Substring(6).Trim()
                    If text4.EndsWith("d", StringComparison.OrdinalIgnoreCase) Then
                        text4 = text4.Substring(0, text4.Length - 1)
                    End If
                    Dim result2 = 0
                    If Integer.TryParse(text4, result2) AndAlso result2 > 0 Then
                        result = result2
                    End If
                Else
                    list.Add(text2)
                End If
            Next
            queryText = String.Join(" ", list)
            Return result
        End Function

        Private Shared Function IndexPath(accountId As String) As String
            Dim text As String = Path.Combine(AccountStore.DataRoot(), "search-index")
            If Not Directory.Exists(text) Then
                Directory.CreateDirectory(text)
            End If
            Dim text2 = accountId.Replace("\", "_").Replace("/", "_").Replace(":", "_")
            Return Path.Combine(text, text2 & ".json")
        End Function
    End Class

