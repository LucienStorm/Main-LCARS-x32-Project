Imports System
Imports System.Collections.Generic
Imports System.IO
Imports Microsoft.VisualBasic.CompilerServices
    Public NotInheritable Class EwsMailBridge
        Public Const ProtocolName As String = "ews"

        Private Sub New()
        End Sub

        Public Shared Function IsEwsAccount(account As MailAccount) As Boolean
            If account Is Nothing Then
                Return False
            End If
            Dim text As String = If(Not Equals(account.Protocol, Nothing), account.Protocol, "").Trim()
            If text.Length = 0 Then
                Return If(Not String.IsNullOrWhiteSpace(account.EwsUrl) AndAlso String.IsNullOrWhiteSpace(account.ImapHost), True, False)
            End If
            Return String.Equals(text, "ews", StringComparison.OrdinalIgnoreCase)
        End Function

        Public Shared Function TryRegisterAccount(ewsUrl As String, username As String, password As String, displayName As String) As MailAccount
            Dim text As String = If(Not Equals(username, Nothing), username, "").Trim()
            If text.Length = 0 Then
                Return Nothing
            End If
            Dim text2 As String = If(Not Equals(ewsUrl, Nothing), ewsUrl, "").Trim()
            If text2.Length = 0 Then
                text2 = "https://outlook.office365.com/EWS/Exchange.asmx"
            End If
            Dim mailAccount As MailAccount = New MailAccount()
            mailAccount.Protocol = "ews"
            mailAccount.EwsUrl = text2
            mailAccount.Id = "ews:" & text.ToLowerInvariant()
            mailAccount.Username = text
            mailAccount.EmailAddress = text
            mailAccount.DisplayName = If(Not Equals(displayName, Nothing), displayName, "").Trim()
            Dim mailAccount2 = mailAccount
            If mailAccount2.DisplayName.Length = 0 Then
                mailAccount2.DisplayName = "EWS " & text
            End If
            Dim list As List(Of MailAccount) = AccountStore.LoadAccounts()
            If list Is Nothing Then
                list = New List(Of MailAccount)()
            End If
            list.RemoveAll(Function(a) If(a IsNot Nothing AndAlso String.Equals(a.Id, mailAccount2.Id, StringComparison.OrdinalIgnoreCase), True, False))
            list.Add(mailAccount2)
            If Not AccountStore.SaveAccounts(list) Then
                Return Nothing
            End If
            If Not Equals(password, Nothing) Then
                AccountStore.SavePassword(mailAccount2.Id, password)
            End If
            Return mailAccount2
        End Function

        Public Shared Function ResolveOAuthToken() As String
            Dim accessToken As String = Nothing
            If EwsOAuthStore.TryEnsureAccessToken(accessToken, "organizations", "94933e60-ee10-4250-a050-3efd588b0718") Then
                Return accessToken
            End If
            EwsOAuthStore.TryLoadToken(accessToken)
            Return If(Not Equals(accessToken, Nothing), accessToken, "")
        End Function

        Public Shared Function ListFolders(account As MailAccount) As List(Of MailFolderInfo)
            Dim list As List(Of MailFolderInfo) = New List(Of MailFolderInfo)()
            list.Add(New MailFolderInfo With {
        .FullName = "inbox",
        .DisplayName = "INBOX"
    })
            list.Add(New MailFolderInfo With {
        .FullName = "sentitems",
        .DisplayName = "SENT"
    })
            list.Add(New MailFolderInfo With {
        .FullName = "drafts",
        .DisplayName = "DRAFTS"
    })
            list.Add(New MailFolderInfo With {
        .FullName = "deleteditems",
        .DisplayName = "TRASH"
    })
            list.Add(New MailFolderInfo With {
        .FullName = "junkemail",
        .DisplayName = "JUNK"
    })
            list.Add(New MailFolderInfo With {
        .FullName = "archive",
        .DisplayName = "ARCHIVE"
    })
            Dim list2 = list
            If account Is Nothing OrElse String.IsNullOrWhiteSpace(account.EwsUrl) Then
                Return list2
            End If
            Dim oauthToken As String = ResolveOAuthToken()
            Dim password = ""
            AccountStore.TryGetPassword(account.Id, password)
            Dim list3 = EwsMailService.TryFindMailSubfolders(account.EwsUrl, account.Username, password, oauthToken)
            If list3 Is Nothing Then
                Return list2
            End If
            For Each item In list3
                If item IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(item.FolderId) Then
                    Dim ewsFolderInfo = item
                    Dim text As String = If(Not Equals(ewsFolderInfo.DisplayName, Nothing), ewsFolderInfo.DisplayName, item.FolderId).Trim()
                    If text.Length = 0 Then
                        text = item.FolderId
                    End If
                    list2.Add(New MailFolderInfo With {
        .FullName = "fid:" & item.FolderId,
        .DisplayName = text.ToUpperInvariant()
    })
                End If
            Next
            Return list2
        End Function

        Public Shared Function SyncHeaders(account As MailAccount, password As String, folder As String, Optional maxCount As Integer = 500) As List(Of MailMessageHeader)
            Dim text As String = If(Not Equals(folder, Nothing), folder, "").Trim()
            If text.Length = 0 Then
                text = "inbox"
            End If
            Dim distinguishedFolderId = MapFolderToDistinguished(text)
            Dim oauthToken As String = ResolveOAuthToken()
            Dim list = EwsMailService.TryFindFolderItems(account.EwsUrl, account.Username, password, distinguishedFolderId, oauthToken)
            If list Is Nothing Then
                Return New List(Of MailMessageHeader)()
            End If
            Dim accountId = If(Not Equals(account.Id, Nothing), account.Id, "")
            Dim folder2 = text
            Dim list2 As List(Of MailMessageHeader) = New List(Of MailMessageHeader)()
            For Each item2 In list
                If item2 IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(item2.ItemId) Then
                    Dim uid = EwsItemIdStore.ItemIdToUid(item2.ItemId)
                    EwsItemIdStore.Remember(accountId, folder2, uid, item2.ItemId)
                    Dim mailMessageHeader As MailMessageHeader = New MailMessageHeader()
                    mailMessageHeader.Uid = uid
                    mailMessageHeader.Folder = folder2
                    Dim ewsMailSummary = item2
                    mailMessageHeader.Subject = If(Not Equals(ewsMailSummary.Subject, Nothing), ewsMailSummary.Subject, "")
                    Dim ewsMailSummary2 = item2
                    mailMessageHeader.From = If(Not Equals(ewsMailSummary2.FromLine, Nothing), ewsMailSummary2.FromLine, "")
                    mailMessageHeader.DateUtc = If(Date.Compare(item2.DateUtc, Date.MinValue) = 0, Date.UtcNow, item2.DateUtc)
                    mailMessageHeader.IsSeen = False
                    Dim item = mailMessageHeader
                    list2.Add(item)
                End If
            Next
            list2.Sort(Function(a, b) Date.Compare(b.DateUtc, a.DateUtc))
            If maxCount > 0 AndAlso list2.Count > maxCount Then
                Return list2.GetRange(0, maxCount)
            End If
            Return list2
        End Function

        Public Shared Function SearchFolderHeaders(account As MailAccount, password As String, folder As String, queryText As String, Optional maxCount As Integer = 200) As List(Of MailMessageHeader)
            Dim text As String = If(Not Equals(queryText, Nothing), queryText, "").Trim().ToLowerInvariant()
            Dim list = SyncHeaders(account, password, folder)
            If text.Length = 0 Then
                If maxCount > 0 AndAlso list.Count > maxCount Then
                    Return list.GetRange(0, maxCount)
                End If
                Return list
            End If
            Dim list2 As List(Of MailMessageHeader) = New List(Of MailMessageHeader)()
            Dim text2 = If(Not Equals(account.Id, Nothing), account.Id, "")
            Dim text3 As String = If(Not Equals(folder, Nothing), folder, "").Trim()
            Dim hashSet As HashSet(Of String) = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            If text2.Length > 0 Then
                For Each item2 In MailSearchIndex.Search(text2, text, maxCount)
                    If item2 IsNot Nothing AndAlso String.Equals(item2.Folder, text3, StringComparison.OrdinalIgnoreCase) Then
                        hashSet.Add(item2.Folder & "|" & item2.Uid.ToString())
                    End If
                Next
            End If
            For Each item3 In list
                If item3 IsNot Nothing Then
                    Dim mailMessageHeader = item3
                    Dim obj = If(Not Equals(mailMessageHeader.Subject, Nothing), mailMessageHeader.Subject, "")
                    Dim mailMessageHeader2 = item3
                    Dim text4 As String = (obj & " " & If(Not Equals(mailMessageHeader2.From, Nothing), mailMessageHeader2.From, "")).ToLowerInvariant()
                    Dim item As String = text3 & "|" & item3.Uid.ToString()
                    If text4.IndexOf(text, StringComparison.Ordinal) >= 0 OrElse hashSet.Contains(item) Then
                        list2.Add(item3)
                    End If
                    If maxCount > 0 AndAlso list2.Count >= maxCount Then
                        Exit For
                    End If
                End If
            Next
            Return list2
        End Function

        Public Shared Function FetchBody(account As MailAccount, password As String, folder As String, uid As ULong) As MailMessageBody
            Dim folder2 As String = If(Not Equals(folder, Nothing), folder, "").Trim()
            Dim itemId As String = Nothing
            Dim mailAccount = account
            If Not EwsItemIdStore.TryGetItemId(If(Not Equals(mailAccount.Id, Nothing), mailAccount.Id, ""), folder2, uid, itemId) Then
                Return New MailMessageBody()
            End If
            Dim oauthToken As String = ResolveOAuthToken()
            Dim ewsMessageBody = EwsMailService.TryGetItemBody(account.EwsUrl, account.Username, password, itemId, oauthToken)
            Dim mailMessageBody As MailMessageBody = New MailMessageBody()
            If ewsMessageBody Is Nothing Then
                Return mailMessageBody
            End If
            Dim ewsMessageBody2 = ewsMessageBody
            mailMessageBody.TextBody = If(Not Equals(ewsMessageBody2.BodyText, Nothing), ewsMessageBody2.BodyText, "")
            If mailMessageBody.TextBody.IndexOf("<html", StringComparison.OrdinalIgnoreCase) >= 0 Then
                mailMessageBody.HtmlBody = mailMessageBody.TextBody
                mailMessageBody.TextBody = ""
            End If
            If ewsMessageBody.Attachments IsNot Nothing AndAlso ewsMessageBody.Attachments.Count > 0 Then
                mailAccount = account
                EwsAttachmentStore.RememberAttachments(If(Not Equals(mailAccount.Id, Nothing), mailAccount.Id, ""), folder2, uid, ewsMessageBody.Attachments)
                Dim num = 0
                For Each attachment In ewsMessageBody.Attachments
                    If attachment IsNot Nothing Then
                        Dim mailAttachmentInfo As MailAttachmentInfo = New MailAttachmentInfo()
                        mailAttachmentInfo.PartIndex = num
                        Dim ewsAttachmentInfo = attachment
                        mailAttachmentInfo.FileName = If(Not Equals(ewsAttachmentInfo.FileName, Nothing), ewsAttachmentInfo.FileName, "attachment")
                        mailAttachmentInfo.Size = 0L
                        Dim item = mailAttachmentInfo
                        mailMessageBody.Attachments.Add(item)
                        num = num + 1
                    End If
                Next
            End If
            Return mailMessageBody
        End Function

        Public Shared Function SaveAttachmentToFile(account As MailAccount, password As String, folder As String, uid As ULong, partIndex As Integer, destPath As String) As String
            Dim result As String
            If String.IsNullOrWhiteSpace(destPath) Then
                result = "Save path is required."
            Else
                Dim folder2 As String = If(Not Equals(folder, Nothing), folder, "").Trim()
                Dim attachmentId As String = Nothing
                Dim fileName = ""
                If Not EwsAttachmentStore.TryGetAttachmentId(If(Not Equals(account.Id, Nothing), account.Id, ""), folder2, uid, partIndex, attachmentId, fileName) Then
                    result = "EWS attachment not cached — open message again."
                Else
                    Dim oauthToken As String = ResolveOAuthToken()
                    Dim array = EwsMailService.TryGetAttachmentBytes(account.EwsUrl, account.Username, password, attachmentId, oauthToken)
                    If array Is Nothing OrElse array.Length = 0 Then
                        result = If(Not Equals(EwsMailService.LastError, Nothing), EwsMailService.LastError, "Attachment download failed.")
                    Else
                        Try
                            File.WriteAllBytes(destPath, array)
                            result = ""
                        Catch ex As Exception
                            ProjectData.SetProjectError(ex)
                            Dim ex2 = ex
                            result = ex2.Message
                            Call ProjectData.ClearProjectError()
                        End Try
                    End If
                End If
            End If
            Return result
        End Function

        Public Shared Sub MarkSeen(account As MailAccount, password As String, folder As String, uid As ULong)
            SetReadFlag(account, password, folder, uid, isRead:=True)
        End Sub

        Public Shared Sub MarkUnseen(account As MailAccount, password As String, folder As String, uid As ULong)
            SetReadFlag(account, password, folder, uid, isRead:=False)
        End Sub

        Private Shared Sub SetReadFlag(account As MailAccount, password As String, folder As String, uid As ULong, isRead As Boolean)
            Dim folder2 As String = If(Not Equals(folder, Nothing), folder, "").Trim()
            Dim itemId As String = Nothing
            If EwsItemIdStore.TryGetItemId(If(Not Equals(account.Id, Nothing), account.Id, ""), folder2, uid, itemId) Then
                Dim oauthToken As String = ResolveOAuthToken()
                EwsMailService.TryMarkItemsRead(account.EwsUrl, account.Username, password, New List(Of String) From {
                        itemId
                    }, isRead, oauthToken)
            End If
        End Sub

        Public Shared Sub MoveMessages(account As MailAccount, password As String, folder As String, destFolder As String, uids As List(Of ULong))
            MoveOrDelete(account, password, folder, destFolder, uids, deleteOnly:=False)
        End Sub

        Public Shared Sub MoveToJunk(account As MailAccount, password As String, folder As String, uids As List(Of ULong))
            MoveOrDelete(account, password, folder, "junkemail", uids, deleteOnly:=False)
        End Sub

        Private Shared Sub MoveOrDelete(account As MailAccount, password As String, folder As String, destFolder As String, uids As List(Of ULong), deleteOnly As Boolean)
            If uids Is Nothing OrElse uids.Count = 0 Then
                Return
            End If
            Dim folder2 As String = If(Not Equals(folder, Nothing), folder, "").Trim()
            Dim accountId = If(Not Equals(account.Id, Nothing), account.Id, "")
            Dim list As List(Of String) = New List(Of String)()
            For Each uid In uids
                Dim itemId As String = Nothing
                If EwsItemIdStore.TryGetItemId(accountId, folder2, uid, itemId) Then
                    list.Add(itemId)
                End If
            Next
            If list.Count <> 0 Then
                Dim oauthToken As String = ResolveOAuthToken()
                If deleteOnly Then
                    EwsMailService.TryDeleteItems(account.EwsUrl, account.Username, password, list, oauthToken)
                ElseIf destFolder.Length <> 0 Then
                    Dim toDistinguishedFolderId = MapFolderToDistinguished(destFolder)
                    EwsMailService.TryMoveItems(account.EwsUrl, account.Username, password, list, toDistinguishedFolderId, oauthToken)
                End If
            End If
        End Sub

        Public Shared Sub DeleteMessages(account As MailAccount, password As String, folder As String, uids As List(Of ULong))
            MoveOrDelete(account, password, folder, "deleteditems", uids, deleteOnly:=True)
        End Sub

        Public Shared Sub CopyMessages(account As MailAccount, password As String, folder As String, destFolder As String, uids As List(Of ULong))
            If uids Is Nothing OrElse uids.Count = 0 Then
                Return
            End If
            Dim folder2 As String = If(Not Equals(folder, Nothing), folder, "").Trim()
            Dim accountId = If(Not Equals(account.Id, Nothing), account.Id, "")
            Dim list As List(Of String) = New List(Of String)()
            For Each uid In uids
                Dim itemId As String = Nothing
                If EwsItemIdStore.TryGetItemId(accountId, folder2, uid, itemId) Then
                    list.Add(itemId)
                End If
            Next
            If list.Count <> 0 Then
                Dim oauthToken As String = ResolveOAuthToken()
                Dim toDistinguishedFolderId = MapFolderToDistinguished(destFolder)
                EwsMailService.TryCopyItems(account.EwsUrl, account.Username, password, list, toDistinguishedFolderId, oauthToken)
            End If
        End Sub

        Public Shared Sub AddKeyword(account As MailAccount, password As String, folder As String, uid As ULong, keyword As String)
            Dim folder2 As String = If(Not Equals(folder, Nothing), folder, "").Trim()
            Dim itemId As String = Nothing
            If EwsItemIdStore.TryGetItemId(If(Not Equals(account.Id, Nothing), account.Id, ""), folder2, uid, itemId) Then
                Dim text As String = If(Not Equals(keyword, Nothing), keyword, "").Trim()
                If text.Length <> 0 Then
                    Dim oauthToken As String = ResolveOAuthToken()
                    EwsMailService.TryAddItemCategory(account.EwsUrl, account.Username, password, itemId, text, oauthToken)
                End If
            End If
        End Sub

        Public Shared Function TrySendFilterMail(account As MailAccount, password As String, toAddress As String, subject As String, bodyText As String) As String
            Dim oauthToken As String = ResolveOAuthToken()
            If EwsMailService.TrySendMessage(account.EwsUrl, account.Username, password, toAddress, subject, bodyText, oauthToken) Then
                Return ""
            End If
            Return If(Not Equals(EwsMailService.LastError, Nothing), EwsMailService.LastError, "EWS send failed.")
        End Function

        Public Shared Function ResolveFilterFolder(actionValue As String) As String
            Dim text = MailFilterEngine.ResolveImapFolderPath(actionValue)
            If text.Length = 0 Then
                Return ""
            End If
            Return MapFolderToDistinguished(text)
        End Function

        Public Shared Function MapFolderToDistinguished(folder As String) As String
            Dim text As String = If(Not Equals(folder, Nothing), folder, "").Trim().ToLowerInvariant()
            If text.Length <> 0 Then
                Select Case text
                    Case "inbox"
                    Case "sent", "sent items", "sentitems"
                        Return "sentitems"
                    Case "drafts"
                        Return "drafts"
                    Case "trash", "deleted items", "deleteditems"
                        Return "deleteditems"
                    Case "junk", "junkemail", "spam"
                        Return "junkemail"
                    Case "archive"
                        Return "archive"
                    Case Else
                        If text.StartsWith("fid:", StringComparison.OrdinalIgnoreCase) Then
                            Return text.Substring(4)
                        End If
                        Return text
                End Select
            End If
            Return "inbox"
        End Function
    End Class

