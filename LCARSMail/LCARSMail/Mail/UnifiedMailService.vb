Imports System.Collections.Generic
Imports MimeKit
    Public NotInheritable Class UnifiedMailService
        Private ReadOnly imap As ImapMailService

        Public ReadOnly Property ImapService As ImapMailService
            Get
                Return imap
            End Get
        End Property

        Public Sub New()
            imap = New ImapMailService()
        End Sub

        Public Function ListFolders(account As MailAccount, password As String) As List(Of MailFolderInfo)
            If EwsMailBridge.IsEwsAccount(account) Then
                Return EwsMailBridge.ListFolders(account)
            End If
            Return imap.ListFolders(account, password)
        End Function

        Public Function SyncHeaders(account As MailAccount, password As String, folder As String, Optional maxCount As Integer = 500) As List(Of MailMessageHeader)
            If EwsMailBridge.IsEwsAccount(account) Then
                Return EwsMailBridge.SyncHeaders(account, password, folder, maxCount)
            End If
            Return imap.SyncHeaders(account, password, folder, maxCount)
        End Function

        Public Function FetchBody(account As MailAccount, password As String, folder As String, uid As ULong) As MailMessageBody
            If EwsMailBridge.IsEwsAccount(account) Then
                Return EwsMailBridge.FetchBody(account, password, folder, uid)
            End If
            Return imap.FetchBody(account, password, folder, uid)
        End Function

        Public Sub MarkSeen(account As MailAccount, password As String, folder As String, uid As ULong)
            If EwsMailBridge.IsEwsAccount(account) Then
                EwsMailBridge.MarkSeen(account, password, folder, uid)
            Else
                imap.MarkSeen(account, password, folder, uid)
            End If
        End Sub

        Public Sub MarkUnseen(account As MailAccount, password As String, folder As String, uid As ULong)
            If EwsMailBridge.IsEwsAccount(account) Then
                EwsMailBridge.MarkUnseen(account, password, folder, uid)
            Else
                imap.MarkUnseen(account, password, folder, uid)
            End If
        End Sub

        Public Function SearchFolderHeaders(account As MailAccount, password As String, folder As String, queryText As String, Optional maxCount As Integer = 200) As List(Of MailMessageHeader)
            If EwsMailBridge.IsEwsAccount(account) Then
                Return EwsMailBridge.SearchFolderHeaders(account, password, folder, queryText, maxCount)
            End If
            Return imap.SearchFolderHeaders(account, password, folder, queryText, maxCount)
        End Function

        Public Function FetchPlainBodyForFilter(account As MailAccount, password As String, folder As String, uid As ULong) As String
            If EwsMailBridge.IsEwsAccount(account) Then
                Dim mailMessageBody = EwsMailBridge.FetchBody(account, password, folder, uid)
                If mailMessageBody Is Nothing Then
                    Return ""
                End If
                Return mailMessageBody.GetReadingPaneText()
            End If
            Return imap.FetchPlainBodyForFilter(account, password, folder, uid)
        End Function

        Public Sub DeleteMessages(account As MailAccount, password As String, folder As String, uids As List(Of ULong))
            If EwsMailBridge.IsEwsAccount(account) Then
                EwsMailBridge.DeleteMessages(account, password, folder, uids)
            Else
                imap.DeleteMessages(account, password, folder, uids)
            End If
        End Sub

        Public Sub MoveMessages(account As MailAccount, password As String, folder As String, destFolder As String, uids As List(Of ULong))
            If EwsMailBridge.IsEwsAccount(account) Then
                EwsMailBridge.MoveMessages(account, password, folder, destFolder, uids)
            Else
                imap.MoveMessages(account, password, folder, destFolder, uids)
            End If
        End Sub

        Public Function SaveAttachmentToFile(account As MailAccount, password As String, folder As String, uid As ULong, partIndex As Integer, destPath As String) As String
            If EwsMailBridge.IsEwsAccount(account) Then
                Return EwsMailBridge.SaveAttachmentToFile(account, password, folder, uid, partIndex, destPath)
            End If
            Return imap.SaveAttachmentToFile(account, password, folder, uid, partIndex, destPath)
        End Function

        Public Function ResolveDraftsFolderFullName(account As MailAccount, password As String) As String
            If EwsMailBridge.IsEwsAccount(account) Then
                Return "drafts"
            End If
            Return imap.ResolveDraftsFolderFullName(account, password)
        End Function

        Public Function AppendDraft(account As MailAccount, password As String, mime As MimeMessage, ByRef newUid As ULong) As String
            If EwsMailBridge.IsEwsAccount(account) Then
                Return "EWS drafts use compose send."
            End If
            Return imap.AppendDraft(account, password, mime, newUid)
        End Function

        Public Function ReplaceDraft(account As MailAccount, password As String, folder As String, uid As ULong, mime As MimeMessage, ByRef newUid As ULong) As String
            If EwsMailBridge.IsEwsAccount(account) Then
                Return "EWS drafts use compose send."
            End If
            Return imap.ReplaceDraft(account, password, folder, uid, mime, newUid)
        End Function

        Public Sub CopyMessages(account As MailAccount, password As String, folder As String, destFolder As String, uids As List(Of ULong))
            If EwsMailBridge.IsEwsAccount(account) Then
                EwsMailBridge.CopyMessages(account, password, folder, destFolder, uids)
            Else
                imap.CopyMessages(account, password, folder, destFolder, uids)
            End If
        End Sub

        Public Sub MoveToJunk(account As MailAccount, password As String, folder As String, uids As List(Of ULong))
            If EwsMailBridge.IsEwsAccount(account) Then
                EwsMailBridge.MoveToJunk(account, password, folder, uids)
            Else
                imap.MoveToJunk(account, password, folder, uids)
            End If
        End Sub

        Public Sub AddKeyword(account As MailAccount, password As String, folder As String, uid As ULong, keyword As String)
            If EwsMailBridge.IsEwsAccount(account) Then
                EwsMailBridge.AddKeyword(account, password, folder, uid, keyword)
            Else
                imap.AddKeyword(account, password, folder, uid, keyword)
            End If
        End Sub
    End Class

