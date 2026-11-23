Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Linq
Imports System.Net.Security
Imports System.Security.Cryptography.X509Certificates
Imports System.Text
Imports System.Threading
Imports MailKit
Imports MailKit.Net.Imap
Imports MailKit.Search
Imports MailKit.Security
Imports Microsoft.VisualBasic.CompilerServices
Imports MimeKit
    Public Class ImapMailService
        <DebuggerNonUserCode>
        Public Sub New()
        End Sub

        Public Function ListFolders(account As MailAccount, password As String) As List(Of MailFolderInfo)
            Dim val As ImapClient = Connect(account, password)
            Try
                Dim root As IMailFolder = val.Inbox
                If val.PersonalNamespaces IsNot Nothing AndAlso val.PersonalNamespaces.Count > 0 Then
                    root = val.GetFolder(val.PersonalNamespaces(0))
                End If
                Return ImapMailService.CollectFolders(val, root)
            Finally
                If val IsNot Nothing Then CType(val, IDisposable).Dispose()
            End Try
        End Function

        Public Function SyncHeaders(account As MailAccount, password As String, folder As String, Optional maxCount As Integer = 500) As List(Of MailMessageHeader)
            Dim val As ImapClient = Connect(account, password)
            Try
                Dim val2 As IMailFolder = ImapMailService.OpenFolder(val, folder)
                Dim cancellationToken As CancellationToken = Nothing
                val2.Open(CType(1, FolderAccess), cancellationToken)
                Dim list As IList(Of UniqueId) = val2.Search(SearchQuery.All, cancellationToken)
                If list Is Nothing OrElse list.Count = 0 Then
                    Dim list2 As List(Of MailMessageHeader) = New List(Of MailMessageHeader)()
                    MailCache.SaveHeaders(account.Id, val2.FullName, list2)
                    Return list2
                End If
                Dim list3 As List(Of UniqueId) = list.OrderBy(Function(u As UniqueId) .Id).ToList()
                Dim num = list3.Count
                If maxCount > 0 Then
                    num = Math.Min(maxCount, list3.Count)
                End If
                Dim list4 As List(Of UniqueId) = list3.Skip(list3.Count - num).ToList()
                Dim val3 As MessageSummaryItems = CType(33372, MessageSummaryItems)
                Dim source As IList(Of IMessageSummary) = IMailFolderExtensions.Fetch(val2, list4, val3, cancellationToken)
                Dim list5 As List(Of MailMessageHeader) = New List(Of MailMessageHeader)()
                For Each item As IMessageSummary In source.OrderByDescending(Function(s As IMessageSummary)
                                                                                 Dim uniqueId As UniqueId = s.UniqueId
                                                                                 Return .Id
                                                                             End Function)
                    list5.Add(HeaderFromSummary(val2.FullName, item))
                Next
                MailCache.SaveHeaders(account.Id, val2.FullName, list5)
                Return list5
            Finally
                If val IsNot Nothing Then CType(val, IDisposable).Dispose()
            End Try
        End Function

        Public Function FetchBody(account As MailAccount, password As String, folder As String, uid As ULong) As MailMessageBody
            Dim val As ImapClient = Connect(account, password)
            Try
                Dim val2 As IMailFolder = ImapMailService.OpenFolder(val, folder)
                Dim cancellationToken As CancellationToken = Nothing
                val2.Open(CType(1, FolderAccess), cancellationToken)
                Dim val3 As UniqueId = DirectCast(Nothing, UniqueId)
                Call ._002Ector(uid)
                Dim message As MimeMessage = val2.GetMessage(val3, cancellationToken, CType(Nothing, ITransferProgress))
                MailAutocryptService.ApplyAutocryptFromMessage(message)
                Dim plainText = ""
                Dim htmlText = ""
                Dim flag = False
                If MailPgpService.MessageLooksEncrypted(message) Then
                    flag = MailPgpService.TryDecryptMessage(message, "", plainText, htmlText)
                    If Not flag Then
                        Dim text = ImapMailService.ExtractFirstFromAddress(message)
                        If text.Length > 0 AndAlso MailAutocryptService.TryImportWkdForEmail(text) Then
                            flag = MailPgpService.TryDecryptMessage(message, "", plainText, htmlText)
                        End If
                    End If
                End If
                Dim mailMessageBody As MailMessageBody = New MailMessageBody()
                Dim textBody2 As Object
                If plainText.Length <= 0 Then
                    Dim textBody As String = message.TextBody
                    textBody2 = If(Not Equals(textBody, Nothing), textBody, "")
                Else
                    textBody2 = plainText
                End If
                mailMessageBody.TextBody = CStr(textBody2)
                Dim htmlBody2 As Object
                If htmlText.Length <= 0 Then
                    Dim htmlBody As String = message.HtmlBody
                    htmlBody2 = If(Not Equals(htmlBody, Nothing), htmlBody, "")
                Else
                    htmlBody2 = htmlText
                End If
                mailMessageBody.HtmlBody = CStr(htmlBody2)
                mailMessageBody.RequiresPgpDecrypt = If(MailPgpService.MessageLooksEncrypted(message) AndAlso Not flag, True, False)
                mailMessageBody.Attachments = ImapMailService.CollectAttachmentInfo(message)
                mailMessageBody.IcsCalendarParts = ImapMailService.CollectCalendarIcsParts(message)
                Dim mailMessageBody2 = mailMessageBody
                MailCache.SaveBody(account.Id, val2.FullName, uid, mailMessageBody2)
                Return mailMessageBody2
            Finally
                If val IsNot Nothing Then CType(val, IDisposable).Dispose()
            End Try
        End Function

        Public Function SaveAttachmentToFile(account As MailAccount, password As String, folder As String, uid As ULong, partIndex As Integer, destPath As String) As String
            Dim result As String
            If partIndex < 0 Then
                result = "Invalid attachment."
            ElseIf String.IsNullOrWhiteSpace(destPath) Then
                result = "Save path is required."
            Else
                Try
                    Dim val As ImapClient = Connect(account, password)
                    Try
                        Dim val2 As IMailFolder = ImapMailService.OpenFolder(val, folder)
                        Dim cancellationToken As CancellationToken = Nothing
                        val2.Open(CType(1, FolderAccess), cancellationToken)
                        Dim val3 As UniqueId = DirectCast(Nothing, UniqueId)
                        Call ._002Ector(uid)
                        Dim message As MimeMessage = val2.GetMessage(val3, cancellationToken, CType(Nothing, ITransferProgress))
                        Dim attachmentEntity As MimeEntity = ImapMailService.GetAttachmentEntity(message, partIndex)
                        If attachmentEntity IsNot Nothing Then
                            Using stream = File.Create(destPath)
                                ImapMailService.WriteAttachmentEntity(attachmentEntity, stream)
                            End Using
                            GoTo end_IL_0030
                        End If
                        result = "Attachment not found."
                        GoTo end_IL_0026
end_IL_0030:
                    Finally
                        If val IsNot Nothing Then CType(val, IDisposable).Dispose()
                    End Try
                    result = ""
end_IL_0026:
                Catch ex As Exception
                    ProjectData.SetProjectError(ex)
                    Dim ex2 = ex
                    Dim message2 = ex2.Message
                    Dim text As String = If(Not Equals(message2, Nothing), message2, "Save failed.").Trim()
                    If text.Length = 0 Then
                        text = "Save failed."
                    End If
                    result = text
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Public Sub MarkSeen(account As MailAccount, password As String, folder As String, uid As ULong)
            Dim folder2 = ""
            Dim val As ImapClient = Connect(account, password)
            Try
                Dim val2 As IMailFolder = ImapMailService.OpenFolder(val, folder)
                folder2 = val2.FullName
                Dim cancellationToken As CancellationToken = Nothing
                val2.Open(CType(2, FolderAccess), cancellationToken)
                Dim val3 As UniqueId = DirectCast(Nothing, UniqueId)
                Call ._002Ector(uid)
                IMailFolderExtensions.AddFlags(val2, val3, CType(1, MessageFlags), True, cancellationToken)
            Finally
                If val IsNot Nothing Then CType(val, IDisposable).Dispose()
            End Try
            MailCache.UpdateHeaderSeen(account.Id, folder2, uid, isSeen:=True)
        End Sub

        Public Sub MarkUnseen(account As MailAccount, password As String, folder As String, uid As ULong)
            Dim folder2 = ""
            Dim val As ImapClient = Connect(account, password)
            Try
                Dim val2 As IMailFolder = ImapMailService.OpenFolder(val, folder)
                folder2 = val2.FullName
                Dim cancellationToken As CancellationToken = Nothing
                val2.Open(CType(2, FolderAccess), cancellationToken)
                Dim val3 As UniqueId = DirectCast(Nothing, UniqueId)
                Call ._002Ector(uid)
                IMailFolderExtensions.RemoveFlags(val2, val3, CType(1, MessageFlags), True, cancellationToken)
            Finally
                If val IsNot Nothing Then CType(val, IDisposable).Dispose()
            End Try
            MailCache.UpdateHeaderSeen(account.Id, folder2, uid, isSeen:=False)
        End Sub

        Public Function ResolveDraftsFolderFullName(account As MailAccount, password As String) As String
            Dim result As String
            Try
                Dim val As ImapClient = Connect(account, password)
                Try
                    Dim val2 As IMailFolder = ImapMailService.TryFindDraftsFolder(val)
                    result = If(val2 IsNot Nothing, val2.FullName, "")
                Finally
                    If val IsNot Nothing Then CType(val, IDisposable).Dispose()
                End Try
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                result = ""
                Call ProjectData.ClearProjectError()
            End Try
            Return result
        End Function

        Public Function AppendDraft(account As MailAccount, password As String, message As MimeMessage) As String
            Dim newUid = 0UL
            Return Me.AppendDraft(account, password, message, newUid)
        End Function

        Public Function AppendDraft(account As MailAccount, password As String, message As MimeMessage, ByRef newUid As ULong) As String
            newUid = 0UL
            Dim result As String
            If message Is Nothing Then
                result = "Nothing to save."
            Else
                Try
                    Dim val As ImapClient = Connect(account, password)
                    Try
                        Dim val2 As IMailFolder = ImapMailService.TryFindDraftsFolder(val)
                        If val2 IsNot Nothing Then
                            Dim cancellationToken As CancellationToken = Nothing
                            val2.Open(CType(2, FolderAccess), cancellationToken)
                            Dim val3 As UniqueId? = IMailFolderExtensions.Append(val2, message, CType(16, MessageFlags), cancellationToken, CType(Nothing, ITransferProgress))
                            If val3.HasValue Then
                                Dim value As UniqueId = val3.Value
                                newUid = .Id
                            End If
                            GoTo end_IL_001f
                        End If
                        result = "Drafts folder not found."
                        GoTo end_IL_0015
end_IL_001f:
                    Finally
                        If val IsNot Nothing Then CType(val, IDisposable).Dispose()
                    End Try
                    result = ""
end_IL_0015:
                Catch ex As Exception
                    ProjectData.SetProjectError(ex)
                    Dim ex2 = ex
                    result = ex2.Message
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Public Function ReplaceDraft(account As MailAccount, password As String, folderFullName As String, oldUid As ULong, message As MimeMessage, ByRef newUid As ULong) As String
            newUid = 0UL
            Dim result As String
            If message Is Nothing Then
                result = "Nothing to save."
            ElseIf oldUid = 0 Then
                result = Me.AppendDraft(account, password, message, newUid)
            Else
                Try
                    Dim val As ImapClient = Connect(account, password)
                    Try
                        Dim val2 As IMailFolder = ImapMailService.OpenFolder(val, folderFullName)
                        Dim cancellationToken As CancellationToken = Nothing
                        val2.Open(CType(2, FolderAccess), cancellationToken)
                        Dim val3 As UniqueId = DirectCast(Nothing, UniqueId)
                        Call ._002Ector(oldUid)
                        IMailFolderExtensions.AddFlags(val2, val3, CType(8, MessageFlags), True, cancellationToken)
                        val2.Expunge(cancellationToken)
                        Dim val4 As UniqueId? = IMailFolderExtensions.Append(val2, message, CType(16, MessageFlags), cancellationToken, CType(Nothing, ITransferProgress))
                        If val4.HasValue Then
                            Dim value As UniqueId = val4.Value
                            newUid = .Id
                        End If

                    Finally
                        If val IsNot Nothing Then CType(val, IDisposable).Dispose()
                    End Try
                    result = ""
                Catch ex As Exception
                    ProjectData.SetProjectError(ex)
                    Dim ex2 = ex
                    result = ex2.Message
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Public Function SearchFolderHeaders(account As MailAccount, password As String, folder As String, queryText As String, Optional maxCount As Integer = 200) As List(Of MailMessageHeader)
            Dim list As List(Of MailMessageHeader) = New List(Of MailMessageHeader)()
            Dim text As String = If(Not Equals(queryText, Nothing), queryText, "").Trim()
            If text.Length = 0 Then
                Return list
            End If
            Dim val As ImapClient = Connect(account, password)
            Try
                Dim val2 As IMailFolder = ImapMailService.OpenFolder(val, folder)
                Dim cancellationToken As CancellationToken = Nothing
                val2.Open(CType(1, FolderAccess), cancellationToken)
                Dim val3 As SearchQuery = CType(CObj(SearchQuery.[Or](CType(CObj(SearchQuery.SubjectContains(text)), SearchQuery), CType(CObj(SearchQuery.FromContains(text)), SearchQuery))), SearchQuery)
                val3 = CType(CObj(SearchQuery.[Or](val3, CType(CObj(SearchQuery.BodyContains(text)), SearchQuery))), SearchQuery)
                Dim list2 As IList(Of UniqueId) = val2.Search(val3, cancellationToken)
                If list2 Is Nothing OrElse list2.Count = 0 Then
                    Return list
                End If
                Dim list3 As List(Of UniqueId) = list2.OrderByDescending(Function(u As UniqueId) .Id).Take(maxCount).ToList()
                Dim val4 As MessageSummaryItems = CType(33368, MessageSummaryItems)
                Dim list4 As IList(Of IMessageSummary) = IMailFolderExtensions.Fetch(val2, list3, val4, cancellationToken)
                For Each item As IMessageSummary In list4
                    list.Add(HeaderFromSummary(val2.FullName, item))
                Next

            Finally
                If val IsNot Nothing Then CType(val, IDisposable).Dispose()
            End Try
            Return list
        End Function

        Private Shared Function TryFindDraftsFolder(client As ImapClient) As IMailFolder
            If client Is Nothing Then
                Return Nothing
            End If
            Try
                Dim folder As IMailFolder = client.GetFolder(CType(2, SpecialFolder))
                If folder IsNot Nothing AndAlso folder.Exists Then
                    Return folder
                End If
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
            Dim root As IMailFolder = client.Inbox
            If client.PersonalNamespaces IsNot Nothing AndAlso client.PersonalNamespaces.Count > 0 Then
                root = client.GetFolder(client.PersonalNamespaces(0))
            End If
            Dim cancellationToken As CancellationToken = Nothing
            For Each item As MailFolderInfo In ImapMailService.CollectFolders(client, root)
                If item Is Nothing Then
                    Continue For
                End If
                Dim mailFolderInfo = item
                Dim text = If(Not Equals(mailFolderInfo.FullName, Nothing), mailFolderInfo.FullName, "")
                If text.IndexOf("draft", StringComparison.OrdinalIgnoreCase) < 0 Then
                    Continue For
                End If
                Try
                    Dim folder2 As IMailFolder = client.GetFolder(item.FullName, cancellationToken)
                    If folder2 IsNot Nothing AndAlso folder2.Exists Then
                        Return folder2
                    End If
                Catch projectError2 As Exception
                    ProjectData.SetProjectError(projectError2)
                    Call ProjectData.ClearProjectError()
                End Try
            Next
            Return Nothing
        End Function

        Public Function FetchPlainBodyForFilter(account As MailAccount, password As String, folder As String, uid As ULong) As String
            Dim body As MailMessageBody = Nothing
            If MailCache.TryLoadBody(account.Id, folder, uid, body) AndAlso body IsNot Nothing Then
                Return body.GetReadingPaneText()
            End If
            Dim mailMessageBody = FetchBody(account, password, folder, uid)
            If mailMessageBody Is Nothing Then
                Return ""
            End If
            Return mailMessageBody.GetReadingPaneText()
        End Function

        Public Sub CopyMessages(account As MailAccount, password As String, sourceFolder As String, destFolder As String, uids As IEnumerable(Of ULong))
            If uids Is Nothing Then
                Return
            End If
            Dim list As List(Of ULong) = uids.Where(Function(u) u <> 0).Distinct().ToList()
            If list.Count = 0 Then
                Return
            End If
            Dim val As ImapClient = Connect(account, password)
            Try
                Dim val2 As IMailFolder = ImapMailService.OpenFolder(val, sourceFolder)
                Dim cancellationToken As CancellationToken = Nothing
                val2.Open(CType(2, FolderAccess), cancellationToken)
                Dim val3 As IMailFolder = ImapMailService.OpenFolder(val, destFolder)
                Dim list2 As IList(Of UniqueId) = list.[Select](Function(u)
                                                                    Dim result As UniqueId = DirectCast(Nothing, UniqueId)
                                                                    Call ._002Ector(CUInt(CUInt(u)))
                                                                    Return result
                                                                End Function).ToList()
                val2.CopyTo(list2, val3, cancellationToken)
            Finally
                If val IsNot Nothing Then CType(val, IDisposable).Dispose()
            End Try
        End Sub

        Public Sub MoveToJunk(account As MailAccount, password As String, folder As String, uids As IEnumerable(Of ULong))
            If uids Is Nothing Then
                Return
            End If
            Dim list As List(Of ULong) = uids.Where(Function(u) u <> 0).Distinct().ToList()
            If list.Count = 0 Then
                Return
            End If
            Dim val As ImapClient = Connect(account, password)
            Try
                Dim val2 As IMailFolder = ImapMailService.OpenFolder(val, folder)
                Dim cancellationToken As CancellationToken = Nothing
                val2.Open(CType(2, FolderAccess), cancellationToken)
                Dim val3 As IMailFolder = ImapMailService.TryFindJunkFolder(val, val2)
                Dim list2 As IList(Of UniqueId) = list.[Select](Function(u)
                                                                    Dim result As UniqueId = DirectCast(Nothing, UniqueId)
                                                                    Call ._002Ector(CUInt(CUInt(u)))
                                                                    Return result
                                                                End Function).ToList()
                If val3 IsNot Nothing AndAlso Not FolderNamesEqual(val3.FullName, val2.FullName) Then
                    val2.MoveTo(list2, val3, cancellationToken)
                    Return
                End If
                IMailFolderExtensions.AddFlags(val2, list2, CType(8, MessageFlags), True, cancellationToken)
                val2.Expunge(cancellationToken)
            Finally
                If val IsNot Nothing Then CType(val, IDisposable).Dispose()
            End Try
        End Sub

        Public Sub AddKeyword(account As MailAccount, password As String, folder As String, uid As ULong, keyword As String)
            Dim text As String = If(Not Equals(keyword, Nothing), keyword, "").Trim()
            If text.Length = 0 Then
                Return
            End If
            Dim val As ImapClient = Connect(account, password)
            Try
                Dim val2 As IMailFolder = ImapMailService.OpenFolder(val, folder)
                Dim cancellationToken As CancellationToken = Nothing
                val2.Open(CType(2, FolderAccess), cancellationToken)
                Dim list As List(Of UniqueId) = New List(Of UniqueId)()
                Dim item As UniqueId = DirectCast(Nothing, UniqueId)
                Call ._002Ector(uid)
                list.Add(item)
                IMailFolderExtensions.AddLabels(val2, list, CType(New List(Of String) From {
                        text
                    }, IList(Of String)), True, cancellationToken)
            Finally
                If val IsNot Nothing Then CType(val, IDisposable).Dispose()
            End Try
        End Sub

        Public Sub DeleteMessages(account As MailAccount, password As String, folder As String, uids As IEnumerable(Of ULong))
            If uids Is Nothing Then
                Return
            End If
            Dim list As List(Of ULong) = uids.Where(Function(u) u <> 0).Distinct().ToList()
            If list.Count = 0 Then
                Return
            End If
            Dim val As ImapClient = Connect(account, password)
            Try
                Dim val2 As IMailFolder = ImapMailService.OpenFolder(val, folder)
                Dim cancellationToken As CancellationToken = Nothing
                val2.Open(CType(2, FolderAccess), cancellationToken)
                Dim list2 As IList(Of UniqueId) = list.[Select](Function(u)
                                                                    Dim result As UniqueId = DirectCast(Nothing, UniqueId)
                                                                    Call ._002Ector(CUInt(CUInt(u)))
                                                                    Return result
                                                                End Function).ToList()
                Dim val3 As IMailFolder = ImapMailService.TryFindTrashFolder(val, val2)
                If val3 IsNot Nothing AndAlso Not FolderNamesEqual(val3.FullName, val2.FullName) Then
                    val2.MoveTo(list2, val3, cancellationToken)
                    Return
                End If
                IMailFolderExtensions.AddFlags(val2, list2, CType(8, MessageFlags), True, cancellationToken)
                val2.Expunge(cancellationToken)
            Finally
                If val IsNot Nothing Then CType(val, IDisposable).Dispose()
            End Try
        End Sub

        Public Sub MoveMessages(account As MailAccount, password As String, sourceFolder As String, destFolder As String, uids As IEnumerable(Of ULong))
            If uids Is Nothing Then
                Return
            End If
            Dim list As List(Of ULong) = uids.Where(Function(u) u <> 0).Distinct().ToList()
            If list.Count = 0 Then
                Return
            End If
            Dim text As String = If(Not Equals(destFolder, Nothing), destFolder, "").Trim()
            If text.Length = 0 Then
                Throw New ArgumentException("Destination folder is required.", "destFolder")
            End If
            Dim val As ImapClient = Connect(account, password)
            Try
                Dim val2 As IMailFolder = ImapMailService.OpenFolder(val, sourceFolder)
                If Not FolderNamesEqual(val2.FullName, text) Then
                    Dim cancellationToken As CancellationToken = Nothing
                    val2.Open(CType(2, FolderAccess), cancellationToken)
                    Dim val3 As IMailFolder = ImapMailService.OpenFolder(val, text)
                    Dim list2 As IList(Of UniqueId) = list.[Select](Function(u)
                                                                        Dim result As UniqueId = DirectCast(Nothing, UniqueId)
                                                                        Call ._002Ector(CUInt(CUInt(u)))
                                                                        Return result
                                                                    End Function).ToList()
                    val2.MoveTo(list2, val3, cancellationToken)
                End If

            Finally
                If val IsNot Nothing Then CType(val, IDisposable).Dispose()
            End Try
        End Sub

        Private Shared Function TryFindTrashFolder(client As ImapClient, currentFolder As IMailFolder) As IMailFolder
            If client Is Nothing Then
                Return Nothing
            End If
            Try
                Dim folder As IMailFolder = client.GetFolder(CType(7, SpecialFolder))
                If folder IsNot Nothing AndAlso folder.Exists Then
                    Return folder
                End If
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
            Dim root As IMailFolder = client.Inbox
            If client.PersonalNamespaces IsNot Nothing AndAlso client.PersonalNamespaces.Count > 0 Then
                root = client.GetFolder(client.PersonalNamespaces(0))
            End If
            Dim list = ImapMailService.CollectFolders(client, root)
            Dim cancellationToken As CancellationToken = Nothing
            For Each item In list
                If item Is Nothing OrElse Not IsTrashFolderName(item.FullName) AndAlso Not IsTrashFolderName(item.DisplayName) Then
                    Continue For
                End If
                Try
                    Dim folder2 As IMailFolder = client.GetFolder(item.FullName, cancellationToken)
                    If folder2 IsNot Nothing AndAlso folder2.Exists Then
                        Return folder2
                    End If
                Catch projectError2 As Exception
                    ProjectData.SetProjectError(projectError2)
                    Call ProjectData.ClearProjectError()
                End Try
            Next
            Return Nothing
        End Function

        Private Shared Function IsTrashFolderName(name As String) As Boolean
            Dim text As String = If(Not Equals(name, Nothing), name, "").Trim()
            If text.Length = 0 Then
                Return False
            End If
            If String.Equals(text, "Trash", StringComparison.OrdinalIgnoreCase) Then
                Return True
            End If
            If String.Equals(text, "Deleted", StringComparison.OrdinalIgnoreCase) Then
                Return True
            End If
            If String.Equals(text, "Deleted Items", StringComparison.OrdinalIgnoreCase) Then
                Return True
            End If
            If String.Equals(text, "Bin", StringComparison.OrdinalIgnoreCase) Then
                Return True
            End If
            If text.EndsWith(".Trash", StringComparison.OrdinalIgnoreCase) Then
                Return True
            End If
            If text.EndsWith("/Trash", StringComparison.OrdinalIgnoreCase) Then
                Return True
            End If
            If text.EndsWith("\Trash", StringComparison.OrdinalIgnoreCase) Then
                Return True
            End If
            If String.Equals(text, "[Gmail]/Trash", StringComparison.OrdinalIgnoreCase) Then
                Return True
            End If
            If String.Equals(text, "INBOX.Trash", StringComparison.OrdinalIgnoreCase) Then
                Return True
            End If
            Return False
        End Function

        Private Shared Function FolderNamesEqual(a As String, b As String) As Boolean
            Return String.Equals(If(Not Equals(a, Nothing), a, "").Trim(), If(Not Equals(b, Nothing), b, "").Trim(), StringComparison.OrdinalIgnoreCase)
        End Function

        Private Shared Function Connect(account As MailAccount, password As String) As ImapClient
            If account Is Nothing Then
                Throw New ArgumentNullException("account")
            End If
            If Equals(password, Nothing) Then
                password = ""
            End If
            Dim mailAccount = account
            Dim text As String = If(Not Equals(mailAccount.ImapHost, Nothing), mailAccount.ImapHost, "").Trim()
            If text.Length = 0 Then
                Throw New ArgumentException("ImapHost is required.", "account")
            End If
            Dim num = account.ImapPort
            If num <= 0 Then
                If account.ImapSsl Then
                    num = 993
                Else
                    num = 143
                End If
            End If
            mailAccount = account
            Dim text2 As String = If(Not Equals(mailAccount.Username, Nothing), mailAccount.Username, "").Trim()
            If text2.Length = 0 Then
                mailAccount = account
                text2 = If(Not Equals(mailAccount.EmailAddress, Nothing), mailAccount.EmailAddress, "").Trim()
            End If
            Dim val As ImapClient = New ImapClient()
            Dim strictMode = If(account IsNot Nothing AndAlso account.StrictTls, True, False)
            CType(val, MailService).ServerCertificateValidationCallback = Function(s, cert As X509Certificate, chain As X509Chain, errors As SslPolicyErrors) TlsPolicyStore.ValidateCertificate(text, num, TryCast(cert, X509Certificate2), errors, strictMode)
            Dim cancellationToken As CancellationToken = Nothing
            val.Connect(text, num, ResolveSecureSocketOptions(account, num), cancellationToken)
            If MailOAuthProviders.UsesOAuth(account) Then
                val.Authenticate(CType(New SaslMechanismOAuth2(text2, password), SaslMechanism), cancellationToken)
            Else
                CType(val, MailService).Authenticate(text2, password, cancellationToken)
            End If
            Return val
        End Function

        Private Shared Function ResolveSecureSocketOptions(account As MailAccount, port As Integer) As SecureSocketOptions
            If account.ImapStartTls Then
                Return CType(4, SecureSocketOptions)
            End If
            If account.ImapSsl Then
                Return CType(2, SecureSocketOptions)
            End If
            If port = 993 Then
                Return CType(2, SecureSocketOptions)
            End If
            Return CType(1, SecureSocketOptions)
        End Function

        Private Shared Function OpenFolder(client As ImapClient, folder As String) As IMailFolder
            Dim text As String = If(Not Equals(folder, Nothing), folder, "").Trim()
            If text.Length = 0 Then
                text = client.Inbox.FullName
            End If
            Dim cancellationToken As CancellationToken = Nothing
            Return client.GetFolder(text, cancellationToken)
        End Function

        Private Shared Function CollectFolders(client As ImapClient, root As IMailFolder) As List(Of MailFolderInfo)
            Dim list As List(Of MailFolderInfo) = New List(Of MailFolderInfo)()
            ImapMailService.AppendFolderTree(root, list)
            Return list.OrderBy(Function(f) f.FullName, StringComparer.OrdinalIgnoreCase).ToList()
        End Function

        Private Shared Sub AppendFolderTree(folder As IMailFolder, result As List(Of MailFolderInfo))
            result.Add(New MailFolderInfo With {
        .FullName = folder.FullName,
        .DisplayName = folder.Name
    })
            Dim cancellationToken As CancellationToken = Nothing
            For Each subfolder As IMailFolder In folder.GetSubfolders(False, cancellationToken)
                ImapMailService.AppendFolderTree(subfolder, result)
            Next
        End Sub

        Private Shared Function CountNonInlineAttachmentsFromBodyStructure(body As BodyPart) As Integer
            If body Is Nothing Then
                Return -1
            End If
            Return ImapMailService.CountNonInlineAttachmentsFromBodyPart(body)
        End Function

        Private Shared Function CountNonInlineAttachmentsFromBodyPart(part As BodyPart) As Integer
            If part Is Nothing Then
                Return 0
            End If
            Dim val As BodyPartMultipart = CType(If((TypeOf part Is BodyPartMultipart), part, Nothing), BodyPartMultipart)
            If val IsNot Nothing Then
                Dim num = 0
                If val.BodyParts IsNot Nothing Then
                    For Each bodyPart As BodyPart In val.BodyParts
                        num = num + ImapMailService.CountNonInlineAttachmentsFromBodyPart(bodyPart)
                    Next
                End If
                Return num
            End If
            Dim val2 As BodyPartBasic = CType(If((TypeOf part Is BodyPartBasic), part, Nothing), BodyPartBasic)
            If val2 Is Nothing Then
                Return 0
            End If
            If val2.ContentDisposition IsNot Nothing AndAlso String.Equals(val2.ContentDisposition.Disposition, "attachment", StringComparison.OrdinalIgnoreCase) Then
                Return 1
            End If
            Dim fileName As String = val2.FileName
            Dim text As String = If(Not Equals(fileName, Nothing), fileName, "").Trim()
            If text.Length = 0 Then
                Return 0
            End If
            Dim left = ""
            If CType(val2, BodyPart).ContentType IsNot Nothing Then
                fileName = CType(val2, BodyPart).ContentType.MimeType
                left = If(Not Equals(fileName, Nothing), fileName, "").Trim().ToLowerInvariant()
            End If
            If Operators.CompareString(left, "text/plain", TextCompare:=False) = 0 OrElse Operators.CompareString(left, "text/html", TextCompare:=False) = 0 Then
                Return 0
            End If
            Return 1
        End Function

        Private Shared Function HeaderFromSummary(folderFullName As String, summary As IMessageSummary) As MailMessageHeader
            Dim subject = ""
            Dim text = ""
            Dim toAddresses = ""
            Dim ccAddresses = ""
            Dim dateUtc = Date.MinValue
            If summary.Envelope IsNot Nothing Then
                Dim subject2 As String = summary.Envelope.Subject
                subject = If(Not Equals(subject2, Nothing), subject2, "")
                If summary.Envelope.From IsNot Nothing AndAlso summary.Envelope.From.Count > 0 Then
                    text = summary.Envelope.From.ToString()
                End If
                If summary.Envelope.[To] IsNot Nothing AndAlso summary.Envelope.[To].Count > 0 Then
                    toAddresses = summary.Envelope.[To].ToString()
                End If
                If summary.Envelope.Cc IsNot Nothing AndAlso summary.Envelope.Cc.Count > 0 Then
                    ccAddresses = summary.Envelope.Cc.ToString()
                End If
                If summary.Envelope.[Date].HasValue Then
                    dateUtc = summary.Envelope.[Date].Value.UtcDateTime
                End If
            End If
            Dim isSeen = False
            Dim isJunk = False
            If summary.Flags.HasValue Then
                isSeen = CType(summary.Flags.Value, [Enum]).HasFlag(CType(CObj(CType(1, MessageFlags)), [Enum]))
            End If
            Dim messageId = ImapMailService.HeaderValue(summary, "Message-Id")
            Dim inReplyTo = ImapMailService.HeaderValue(summary, "In-Reply-To")
            Dim references = ImapMailService.HeaderValue(summary, "References")
            Dim replyTo = ImapMailService.HeaderValue(summary, "Reply-To")
            Dim listId = ImapMailService.HeaderValue(summary, "List-Id")
            Dim returnPath = ImapMailService.HeaderValue(summary, "Return-Path")
            Dim sizeBytes = 0L
            If summary.Size.HasValue Then
                sizeBytes = summary.Size.Value
            End If
            Dim headerAttachmentCount = CountNonInlineAttachmentsFromBodyStructure(summary.Body)
            Dim mailMessageHeader As MailMessageHeader = New MailMessageHeader()
            Dim uniqueId As UniqueId = summary.UniqueId
            mailMessageHeader.Uid = .Id
            mailMessageHeader.Folder = folderFullName
            mailMessageHeader.Subject = subject
            mailMessageHeader.From = text
            mailMessageHeader.ToAddresses = toAddresses
            mailMessageHeader.CcAddresses = ccAddresses
            mailMessageHeader.DateUtc = dateUtc
            mailMessageHeader.IsSeen = isSeen
            mailMessageHeader.MessageId = messageId
            mailMessageHeader.InReplyTo = inReplyTo
            mailMessageHeader.References = references
            mailMessageHeader.ReplyTo = replyTo
            mailMessageHeader.ListId = listId
            mailMessageHeader.ReturnPath = returnPath
            mailMessageHeader.SizeBytes = sizeBytes
            mailMessageHeader.IsJunk = isJunk
            mailMessageHeader.HeaderAttachmentCount = headerAttachmentCount
            Return mailMessageHeader
        End Function

        Private Shared Function HeaderValue(summary As IMessageSummary, name As String) As String
            If summary Is Nothing OrElse summary.Headers Is Nothing Then
                Return ""
            End If
            Dim text As String = summary.Headers(name)
            Return If(Not Equals(text, Nothing), text, "").Trim()
        End Function

        Private Shared Function CollectCalendarIcsParts(message As MimeMessage) As List(Of String)
            Dim result As List(Of String) = New List(Of String)()
            If message Is Nothing Then
                Return result
            End If
            CollectCalendarIcsFromEntity(message.Body, result)
            Return result
        End Function

        Private Shared Sub CollectCalendarIcsFromEntity(entity As MimeEntity, result As List(Of String))
            If entity Is Nothing OrElse result Is Nothing Then
                Return
            End If
            Dim val As Multipart = CType(If((TypeOf entity Is Multipart), entity, Nothing), Multipart)
            If val IsNot Nothing Then
                For Each item As MimeEntity In val
                    ImapMailService.CollectCalendarIcsFromEntity(item, result)
                Next
                Return
            End If
            Dim val2 As TextPart = CType(If((TypeOf entity Is TextPart), entity, Nothing), TextPart)
            Dim text2 As String
            If val2 IsNot Nothing Then
                Dim text = ""
                If CType(val2, MimeEntity).ContentType IsNot Nothing AndAlso CType(val2, MimeEntity).ContentType.MimeType IsNot Nothing Then
                    text = CType(val2, MimeEntity).ContentType.MimeType.Trim().ToLowerInvariant()
                End If
                If Operators.CompareString(text, "text/calendar", TextCompare:=False) = 0 OrElse text.Contains("calendar") Then
                    text2 = val2.Text
                    Dim text3 = If(Not Equals(text2, Nothing), text2, "")
                    If Not String.IsNullOrWhiteSpace(text3) Then
                        result.Add(text3)
                    End If
                End If
                Return
            End If
            Dim val3 As MimePart = CType(If((TypeOf entity Is MimePart), entity, Nothing), MimePart)
            If val3 Is Nothing Then
                Return
            End If
            text2 = val3.FileName
            Dim text4 As String = If(Not Equals(text2, Nothing), text2, "").Trim().ToLowerInvariant()
            Dim left = ""
            If CType(val3, MimeEntity).ContentType IsNot Nothing AndAlso CType(val3, MimeEntity).ContentType.MimeType IsNot Nothing Then
                left = CType(val3, MimeEntity).ContentType.MimeType.Trim().ToLowerInvariant()
            End If
            If Not text4.EndsWith(".ics", StringComparison.OrdinalIgnoreCase) AndAlso Operators.CompareString(left, "text/calendar", TextCompare:=False) <> 0 Then
                Return
            End If
            Dim memoryStream As MemoryStream = New MemoryStream()
            Dim cancellationToken As CancellationToken = Nothing
            val3.Content.DecodeTo(memoryStream, cancellationToken)
            Dim text5 As String = Encoding.UTF8.GetString(memoryStream.ToArray())
            If Not String.IsNullOrWhiteSpace(text5) Then
                result.Add(text5)
            End If
        End Sub

        Private Shared Function CollectAttachmentInfo(message As MimeMessage) As List(Of MailAttachmentInfo)
            Dim list As List(Of MailAttachmentInfo) = New List(Of MailAttachmentInfo)()
            If message Is Nothing Then
                Return list
            End If
            Dim num = 0
            For Each item As MimeEntity In EnumerateDownloadableParts(message.Body)
                If item IsNot Nothing Then
                    Dim isInline = ImapMailService.IsInlinePart(item)
                    list.Add(New MailAttachmentInfo With {
        .PartIndex = num,
        .FileName = ImapMailService.ResolveAttachmentFileName(item),
        .Size = ImapMailService.ResolveAttachmentSize(item),
        .IsInline = isInline,
        .ContentId = ImapMailService.ResolveContentId(item)
    })
                    num = num + 1
                End If
            Next
            Return list
        End Function

        Private Shared Function GetAttachmentEntity(message As MimeMessage, partIndex As Integer) As MimeEntity
            If message Is Nothing OrElse partIndex < 0 Then
                Return Nothing
            End If
            Dim num = 0
            For Each item As MimeEntity In EnumerateDownloadableParts(message.Body)
                If num = partIndex Then
                    Return item
                End If
                num = num + 1
            Next
            Return Nothing
        End Function

        Private Shared Iterator Function EnumerateDownloadableParts(entity As MimeEntity) As IEnumerable(Of MimeEntity)
            If entity Is Nothing Then
                Return
            End If
            Dim val As Multipart = CType(If((TypeOf entity Is Multipart), entity, Nothing), Multipart)
            If val IsNot Nothing Then
                For Each item As MimeEntity In val
                    For Each item2 As MimeEntity In ImapMailService.EnumerateDownloadableParts(item)
                        Yield item2
                    Next
                Next
                Return
            End If
            If Not (TypeOf entity Is TextPart) AndAlso ImapMailService.IsAttachmentLike(entity) Then
                Yield entity
            End If
        End Function

        Private Shared Function IsAttachmentLike(entity As MimeEntity) As Boolean
            If entity Is Nothing Then
                Return False
            End If
            If TypeOf entity Is MimePart OrElse TypeOf entity Is MessagePart Then
                Return True
            End If
            Return False
        End Function

        Private Shared Function IsInlinePart(entity As MimeEntity) As Boolean
            If entity Is Nothing Then
                Return False
            End If
            If entity.ContentDisposition IsNot Nothing AndAlso String.Equals(entity.ContentDisposition.Disposition, "inline", StringComparison.OrdinalIgnoreCase) Then
                Return True
            End If
            Return ImapMailService.ResolveContentId(entity).Length > 0
        End Function

        Private Shared Function ResolveContentId(entity As MimeEntity) As String
            If entity Is Nothing OrElse entity.ContentId Is Nothing Then
                Return ""
            End If
            Return entity.ContentId.Trim("<"c, ">"c)
        End Function

        Private Shared Function TryFindJunkFolder(client As ImapClient, currentFolder As IMailFolder) As IMailFolder
            If client Is Nothing Then
                Return Nothing
            End If
            Try
                Dim folder As IMailFolder = client.GetFolder(CType(5, SpecialFolder))
                If folder IsNot Nothing AndAlso folder.Exists Then
                    Return folder
                End If
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
            Dim root As IMailFolder = client.Inbox
            If client.PersonalNamespaces IsNot Nothing AndAlso client.PersonalNamespaces.Count > 0 Then
                root = client.GetFolder(client.PersonalNamespaces(0))
            End If
            Dim list = ImapMailService.CollectFolders(client, root)
            Dim cancellationToken As CancellationToken = Nothing
            For Each item In list
                If item Is Nothing Then
                    Continue For
                End If
                Dim mailFolderInfo = item
                Dim text = If(Not Equals(mailFolderInfo.FullName, Nothing), mailFolderInfo.FullName, "")
                If text.IndexOf("junk", StringComparison.OrdinalIgnoreCase) < 0 AndAlso text.IndexOf("spam", StringComparison.OrdinalIgnoreCase) < 0 Then
                    Continue For
                End If
                Try
                    Dim folder2 As IMailFolder = client.GetFolder(item.FullName, cancellationToken)
                    If folder2 IsNot Nothing AndAlso folder2.Exists Then
                        Return folder2
                    End If
                Catch projectError2 As Exception
                    ProjectData.SetProjectError(projectError2)
                    Call ProjectData.ClearProjectError()
                End Try
            Next
            Return Nothing
        End Function

        Private Shared Sub WriteAttachmentEntity(entity As MimeEntity, stream As Stream)
            If entity Is Nothing Then
                Throw New ArgumentNullException("entity")
            End If
            If stream Is Nothing Then
                Throw New ArgumentNullException("stream")
            End If
            Dim val As MessagePart = CType(If((TypeOf entity Is MessagePart), entity, Nothing), MessagePart)
            Dim cancellationToken As CancellationToken = Nothing
            If val IsNot Nothing Then
                CType(val, MimeEntity).WriteTo(stream, cancellationToken)
                Return
            End If
            Dim val2 As MimePart = CType(If((TypeOf entity Is MimePart), entity, Nothing), MimePart)
            If val2 IsNot Nothing Then
                val2.Content.DecodeTo(stream, cancellationToken)
            Else
                entity.WriteTo(stream, cancellationToken)
            End If
        End Sub

        Private Shared Function ResolveAttachmentFileName(entity As MimeEntity) As String
            If entity Is Nothing Then
                Return "attachment"
            End If
            Dim val As MimePart = CType(If((TypeOf entity Is MimePart), entity, Nothing), MimePart)
            If val IsNot Nothing Then
                Dim fileName As String = val.FileName
                If Not String.IsNullOrWhiteSpace(fileName) Then
                    Return fileName.Trim()
                End If
            End If
            If entity.ContentDisposition IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(entity.ContentDisposition.FileName) Then
                Return entity.ContentDisposition.FileName.Trim()
            End If
            If entity.ContentType IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(entity.ContentType.Name) Then
                Return entity.ContentType.Name.Trim()
            End If
            Return "attachment"
        End Function

        Private Shared Function ResolveAttachmentSize(entity As MimeEntity) As Long
            If entity Is Nothing Then
                Return 0L
            End If
            If entity.ContentDisposition IsNot Nothing AndAlso entity.ContentDisposition.Size.HasValue Then
                Return entity.ContentDisposition.Size.Value
            End If
            Return 0L
        End Function

        Private Shared Function ExtractFirstFromAddress(message As MimeMessage) As String
            If message Is Nothing OrElse message.From Is Nothing Then
                Return ""
            End If
            For Each mailbox As MailboxAddress In message.From.Mailboxes
                Dim val As MailboxAddress = If((TypeOf mailbox Is MailboxAddress), mailbox, Nothing)
                If val IsNot Nothing Then
                    Dim address As String = val.Address
                    Dim text As String = If(Not Equals(address, Nothing), address, "").Trim()
                    If text.Length > 0 Then
                        Return text
                    End If
                End If
            Next
            Return ""
        End Function
    End Class

