Imports System
Imports System.Collections.Generic
Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.VisualBasic.CompilerServices
    Public NotInheritable Class MailSearchBackgroundIndexer
        Private Const FoldersPerRun As Integer = 384

        Private Const MaxFoldersPerAccount As Integer = 4096

        Private Const HeadersPerFolder As Integer = 0

        Private Const BodyPreviewPerFolder As Integer = 0

        Private Const BodyBackfillPerAccount As Integer = 176000

        Private Const BodyBackfillBurstPerAccount As Integer = 1024

        Private Shared ReadOnly gate As Object = RuntimeHelpers.GetObjectValue(New Object())

        Private Shared indexing As Boolean

        Private Shared backfilling As Boolean

        Public Shared LastRunSummary As String = ""

        Public Shared LastIndexedFolderCount As Integer = 0

        Public Shared LastBackfillRemaining As Integer = 0

        Public Shared LastBodiesIndexedThisRun As Integer = 0

        Private Sub New()
        End Sub

        Public Shared Sub QueueIndexAccounts(accounts As IList(Of MailAccount), passwordResolver As Func(Of MailAccount, String), mailService As UnifiedMailService)
            If accounts Is Nothing OrElse accounts.Count = 0 OrElse mailService Is Nothing Then
                Return
            End If
            Dim obj = gate
            ObjectFlowControl.CheckForSyncLockOnValueType(obj)
            Dim lockTaken = False
            Try
                Monitor.Enter(obj, lockTaken)
                If indexing Then
                    Return
                End If
                indexing = True
            Finally
                If lockTaken Then
                    Monitor.Exit(obj)
                End If
            End Try
            Dim accounts2 As List(Of MailAccount) = New List(Of MailAccount)(accounts)
            ThreadPool.QueueUserWorkItem(Sub(state)
                                             Try
                                                 RunIndex(accounts2, passwordResolver, mailService)
                                             Finally
                                                 Dim obj2 = gate
                                                 ObjectFlowControl.CheckForSyncLockOnValueType(obj2)
                                                 Dim lockTaken2 = False
                                                 Try
                                                     Monitor.Enter(obj2, lockTaken2)
                                                     indexing = False
                                                 Finally
                                                     If lockTaken2 Then
                                                         Monitor.Exit(obj2)
                                                     End If
                                                 End Try
                                             End Try
                                         End Sub)
        End Sub

        Private Shared Sub RunIndex(accounts As List(Of MailAccount), passwordResolver As Func(Of MailAccount, String), mailService As UnifiedMailService)
            Dim num = 0
            Dim num2 = 0
            Dim bodiesIndexed = 0
            For Each account In accounts
                If account Is Nothing Then
                    Continue For
                End If
                Dim mailAccount = account
                Dim text = If(Not Equals(mailAccount.Id, Nothing), mailAccount.Id, "")
                If text.Length = 0 Then
                    Continue For
                End If
                Dim text2 As String = Nothing
                If passwordResolver IsNot Nothing Then
                    text2 = passwordResolver(account)
                End If
                If EwsMailBridge.IsEwsAccount(account) AndAlso String.IsNullOrEmpty(text2) Then
                    text2 = " "
                End If
                If String.IsNullOrEmpty(text2) Then
                    Continue For
                End If
                Dim list As List(Of MailFolderInfo) = Nothing
                Try
                    list = mailService.ListFolders(account, text2)
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    list = Nothing
                    Call ProjectData.ClearProjectError()
                End Try
                If list Is Nothing OrElse list.Count = 0 Then
                    Continue For
                End If
                Dim list2 = MailSearchIndexCursor.TakeNextFolders(text, list, 384)
                For Each item In list2
                    If item Is Nothing Then
                        Continue For
                    End If
                    num += 1
                    Try
                        Dim list3 = mailService.SyncHeaders(account, text2, item.FullName, 0)
                        If list3 Is Nothing OrElse list3.Count = 0 Then
                            Continue For
                        End If
                        If False AndAlso list3.Count > 0 Then
                            list3 = list3.GetRange(0, 0)
                        End If
                        MailSearchIndex.UpsertHeaders(text, item.FullName, list3)
                        num2 += list3.Count
                        Dim num3 = 0
                        For Each item2 In list3
                            If item2 IsNot Nothing Then
                                If False AndAlso num3 >= 0 Then
                                    Exit For
                                End If
                                num3 += 1
                                Try
                                    Dim body = mailService.FetchBody(account, text2, item.FullName, item2.Uid)
                                    UpsertBodyPreviewFromFetch(text, item.FullName, item2.Uid, body, bodiesIndexed)
                                Catch projectError2 As Exception
                                    ProjectData.SetProjectError(projectError2)
                                    Call ProjectData.ClearProjectError()
                                End Try
                            End If
                        Next
                    Catch projectError3 As Exception
                        ProjectData.SetProjectError(projectError3)
                        Call ProjectData.ClearProjectError()
                    End Try
                Next
                MailSearchIndex.BackfillGlodaFtsMetaDates(text, 22000)
                Dim num4 = MailSearchIndex.RebuildFtsHeaderCorpusForAccount(text, 4400)
                If num4 > 0 AndAlso String.IsNullOrWhiteSpace(LastRunSummary) Then
                    LastRunSummary = "FTS-CORPUS " & num4.ToString()
                End If
                Dim list4 = MailSearchIndex.ListEntriesNeedingBodyPreview(text, 176000)
                For Each item3 In list4
                    If item3 IsNot Nothing Then
                        Try
                            Dim body2 = mailService.FetchBody(account, text2, item3.Folder, item3.Uid)
                            UpsertBodyPreviewFromFetch(text, item3.Folder, item3.Uid, body2, bodiesIndexed)
                        Catch projectError4 As Exception
                            ProjectData.SetProjectError(projectError4)
                            Call ProjectData.ClearProjectError()
                        End Try
                    End If
                Next
            Next
            LastIndexedFolderCount = num
            LastBodiesIndexedThisRun = bodiesIndexed
            LastBackfillRemaining = 0
            For Each account2 In accounts
                If account2 IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(account2.Id) Then
                    Dim num5 = MailSearchIndex.CountEntriesNeedingBodyPreview(account2.Id)
                    If num5 > LastBackfillRemaining Then
                        LastBackfillRemaining = num5
                    End If
                End If
            Next
            Dim text3 As String = (If(LastBackfillRemaining > 0, (" · BACKFILL " & LastBackfillRemaining.ToString()), ""))
            LastRunSummary = "INDEX " & num.ToString() & " fld / " & num2.ToString() & " hdr / " & bodiesIndexed.ToString() & " body" & text3
        End Sub

        Public Shared Sub QueueBodyBackfillBurst(accounts As IList(Of MailAccount), passwordResolver As Func(Of MailAccount, String), mailService As UnifiedMailService)
            If accounts Is Nothing OrElse accounts.Count = 0 OrElse mailService Is Nothing Then
                Return
            End If
            Dim obj = gate
            ObjectFlowControl.CheckForSyncLockOnValueType(obj)
            Dim lockTaken = False
            Try
                Monitor.Enter(obj, lockTaken)
                If indexing OrElse backfilling Then
                    Return
                End If
                backfilling = True
            Finally
                If lockTaken Then
                    Monitor.Exit(obj)
                End If
            End Try
            Dim accounts2 As List(Of MailAccount) = New List(Of MailAccount)(accounts)
            ThreadPool.QueueUserWorkItem(Sub(state)
                                             Try
                                                 RunBodyBackfillBurst(accounts2, passwordResolver, mailService)
                                             Finally
                                                 Dim obj2 = gate
                                                 ObjectFlowControl.CheckForSyncLockOnValueType(obj2)
                                                 Dim lockTaken2 = False
                                                 Try
                                                     Monitor.Enter(obj2, lockTaken2)
                                                     backfilling = False
                                                 Finally
                                                     If lockTaken2 Then
                                                         Monitor.Exit(obj2)
                                                     End If
                                                 End Try
                                             End Try
                                         End Sub)
        End Sub

        Private Shared Sub RunBodyBackfillBurst(accounts As List(Of MailAccount), passwordResolver As Func(Of MailAccount, String), mailService As UnifiedMailService)
            Dim bodiesIndexed = 0
            For Each account In accounts
                If account Is Nothing Then
                    Continue For
                End If
                Dim mailAccount = account
                Dim text = If(Not Equals(mailAccount.Id, Nothing), mailAccount.Id, "")
                If text.Length = 0 Then
                    Continue For
                End If
                Dim num = MailSearchIndex.CountEntriesNeedingBodyPreview(text)
                If num < 500 Then
                    Continue For
                End If
                Dim text2 As String = Nothing
                If passwordResolver IsNot Nothing Then
                    text2 = passwordResolver(account)
                End If
                If EwsMailBridge.IsEwsAccount(account) AndAlso String.IsNullOrEmpty(text2) Then
                    text2 = " "
                End If
                If String.IsNullOrEmpty(text2) Then
                    Continue For
                End If
                Dim maxCount = 1024
                If num >= 340000 Then
                    maxCount = 21504
                ElseIf num >= 320000 Then
                    maxCount = 20480
                ElseIf num >= 300000 Then
                    maxCount = 19456
                ElseIf num >= 280000 Then
                    maxCount = 18432
                ElseIf num >= 260000 Then
                    maxCount = 17408
                ElseIf num >= 240000 Then
                    maxCount = 16384
                ElseIf num >= 220000 Then
                    maxCount = 15360
                ElseIf num >= 200000 Then
                    maxCount = 14336
                ElseIf num >= 180000 Then
                    maxCount = 13312
                ElseIf num >= 160000 Then
                    maxCount = 12288
                ElseIf num >= 140000 Then
                    maxCount = 11264
                ElseIf num >= 120000 Then
                    maxCount = 10240
                ElseIf num >= 100000 Then
                    maxCount = 9216
                ElseIf num >= 80000 Then
                    maxCount = 8192
                ElseIf num >= 60000 Then
                    maxCount = 7168
                ElseIf num >= 40000 Then
                    maxCount = 6144
                ElseIf num >= 20000 Then
                    maxCount = 5120
                ElseIf num >= 10000 Then
                    maxCount = 4096
                ElseIf num >= 5000 Then
                    maxCount = 3072
                End If
                Dim list = MailSearchIndex.ListEntriesNeedingBodyPreview(text, maxCount)
                For Each item In list
                    If item IsNot Nothing Then
                        Try
                            Dim body = mailService.FetchBody(account, text2, item.Folder, item.Uid)
                            UpsertBodyPreviewFromFetch(text, item.Folder, item.Uid, body, bodiesIndexed)
                        Catch projectError As Exception
                            ProjectData.SetProjectError(projectError)
                            Call ProjectData.ClearProjectError()
                        End Try
                    End If
                Next
            Next
            If bodiesIndexed > 0 Then
                LastBodiesIndexedThisRun = bodiesIndexed
                LastRunSummary = "BACKFILL BURST " & bodiesIndexed.ToString() & " body → FTS"
            End If
            LastBackfillRemaining = 0
            For Each account2 In accounts
                If account2 IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(account2.Id) Then
                    Dim num2 = MailSearchIndex.CountEntriesNeedingBodyPreview(account2.Id)
                    If num2 > LastBackfillRemaining Then
                        LastBackfillRemaining = num2
                    End If
                End If
            Next
        End Sub

        Private Shared Sub UpsertBodyPreviewFromFetch(accountId As String, folder As String, uid As ULong, body As MailMessageBody, ByRef bodiesIndexed As Integer)
            If body IsNot Nothing Then
                Dim attachmentCount = CountNonInlineAttachments(body)
                MailSearchIndex.UpsertBodyPreview(accountId, folder, uid, body.GetReadingPaneText(), attachmentCount)
                bodiesIndexed += 1
            End If
        End Sub

        Private Shared Function CountNonInlineAttachments(body As MailMessageBody) As Integer
            Dim num = 0
            If body Is Nothing OrElse body.Attachments Is Nothing Then
                Return 0
            End If
            For Each attachment In body.Attachments
                If attachment IsNot Nothing AndAlso Not attachment.IsInline Then
                    num = num + 1
                End If
            Next
            Return num
        End Function
    End Class

