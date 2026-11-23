Imports System
Imports System.Collections.Generic
    Public NotInheritable Class EwsBackgroundSync
        Public Shared LastBackgroundCalAutoReconcile As Integer = 0

        Public Shared LastBackgroundCalChangeKeyRefreshes As Integer = 0

        Public Shared LastBackgroundGalPushRetry As Integer = 0

        Public Shared LastBackgroundCalPushAfterCk As Integer = 0

        Public Shared LastBackgroundGalPushSecondRetry As Integer = 0

        Public Shared LastBackgroundGalPushThirdRetry As Integer = 0

        Public Shared LastBackgroundCalBidirectionalThirdPass As Integer = 0

        Public Shared LastBackgroundCalPushAfterThirdPass As Integer = 0

        Public Shared LastBackgroundGalPushFourthRetry As Integer = 0

        Public Shared LastBackgroundCalBidirectionalFourthPass As Integer = 0

        Public Shared LastBackgroundGalPushFifthRetry As Integer = 0

        Public Shared LastBackgroundCalAutoReconcileSecondPass As Integer = 0

        Public Shared LastBackgroundCalChangeKeyRefreshesSecondPass As Integer = 0

        Public Shared LastBackgroundCalPushAfterFourthPass As Integer = 0

        Public Shared LastBackgroundCalBidirectionalFifthPass As Integer = 0

        Public Shared LastBackgroundGalPushSixthRetry As Integer = 0

        Public Shared LastBackgroundCalAutoReconcileThirdPass As Integer = 0

        Public Shared LastBackgroundCalPushAfterFifthPass As Integer = 0

        Public Shared LastBackgroundCalBidirectionalSixthPass As Integer = 0

        Public Shared LastBackgroundGalPushSeventhRetry As Integer = 0

        Public Shared LastBackgroundCalChangeKeyRefreshesThirdPass As Integer = 0

        Public Shared LastBackgroundCalAutoReconcileFourthPass As Integer = 0

        Public Shared LastBackgroundCalPushAfterSixthPass As Integer = 0

        Public Shared LastBackgroundCalBidirectionalSeventhPass As Integer = 0

        Public Shared LastBackgroundCalBidirectionalEighthPass As Integer = 0

        Public Shared LastBackgroundGalPushEighthRetry As Integer = 0

        Public Shared LastBackgroundGalPushNinthRetry As Integer = 0

        Private Sub New()
        End Sub

        Public Shared Function TryMergeCalendarsForAccounts(accounts As IList(Of MailAccount)) As Integer
            If accounts Is Nothing OrElse accounts.Count = 0 Then
                Return 0
            End If
            Dim num = 0
            LastBackgroundCalAutoReconcile = 0
            LastBackgroundCalChangeKeyRefreshes = 0
            LastBackgroundGalPushRetry = 0
            LastBackgroundCalPushAfterCk = 0
            LastBackgroundGalPushSecondRetry = 0
            LastBackgroundGalPushThirdRetry = 0
            LastBackgroundCalBidirectionalThirdPass = 0
            LastBackgroundCalPushAfterThirdPass = 0
            LastBackgroundGalPushFourthRetry = 0
            LastBackgroundCalBidirectionalFourthPass = 0
            LastBackgroundGalPushFifthRetry = 0
            LastBackgroundCalAutoReconcileSecondPass = 0
            LastBackgroundCalChangeKeyRefreshesSecondPass = 0
            LastBackgroundCalPushAfterFourthPass = 0
            LastBackgroundCalBidirectionalFifthPass = 0
            LastBackgroundGalPushSixthRetry = 0
            LastBackgroundCalAutoReconcileThirdPass = 0
            LastBackgroundCalPushAfterFifthPass = 0
            LastBackgroundCalBidirectionalSixthPass = 0
            LastBackgroundGalPushSeventhRetry = 0
            LastBackgroundCalChangeKeyRefreshesThirdPass = 0
            LastBackgroundCalAutoReconcileFourthPass = 0
            LastBackgroundCalPushAfterSixthPass = 0
            LastBackgroundCalBidirectionalSeventhPass = 0
            LastBackgroundCalBidirectionalEighthPass = 0
            LastBackgroundGalPushEighthRetry = 0
            LastBackgroundGalPushNinthRetry = 0
            Dim oauthToken As String = EwsMailBridge.ResolveOAuthToken()
            For Each account In accounts
                If account Is Nothing OrElse Not EwsMailBridge.IsEwsAccount(account) OrElse String.IsNullOrWhiteSpace(account.EwsUrl) Then
                    Continue For
                End If
                Dim password = ""
                AccountStore.TryGetPassword(account.Id, password)
                Dim num2 = EwsLcarsBridge.TrySyncEwsCalendarBidirectional(account.EwsUrl, account.Username, password, oauthToken)
                If EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents() > 0 Then
                    num2 = Math.Max(num2, EwsLcarsBridge.TrySyncEwsCalendarBidirectional(account.EwsUrl, account.Username, password, oauthToken))
                End If
                If EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents() > 0 Then
                    Dim num3 = EwsCalendarConflictHelper.TryAutoReconcileUnchangedDirty(account.EwsUrl, account.Username, password, oauthToken)
                    If num3 > 0 Then
                        num2 = Math.Max(num2, num3)
                        LastBackgroundCalAutoReconcile += num3
                    ElseIf EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents() > 0 Then
                        num2 = Math.Max(num2, EwsLcarsBridge.TrySyncEwsCalendarBidirectional(account.EwsUrl, account.Username, password, oauthToken))
                    End If
                End If
                If EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents() > 0 Then
                    Dim num4 = EwsCalendarConflictHelper.TryRefreshChangeKeysForDirtyEvents(account.EwsUrl, account.Username, password, oauthToken)
                    If num4 > 0 Then
                        LastBackgroundCalChangeKeyRefreshes += num4
                        num2 = Math.Max(num2, num4)
                    End If
                    If EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents() > 0 Then
                        Dim num5 = EwsLcarsBridge.TryPushDirtyCalendarEventsToEws(account.EwsUrl, account.Username, password, oauthToken)
                        If num5 > 0 Then
                            LastBackgroundCalPushAfterCk += num5
                            num2 = Math.Max(num2, num5)
                        End If
                    End If
                    If EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents() > 0 Then
                        Dim num6 = EwsLcarsBridge.TrySyncEwsCalendarBidirectional(account.EwsUrl, account.Username, password, oauthToken)
                        If num6 > 0 Then
                            LastBackgroundCalBidirectionalThirdPass += num6
                            num2 = Math.Max(num2, num6)
                        End If
                    End If
                    If EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents() > 0 Then
                        Dim num7 = EwsLcarsBridge.TryPushDirtyCalendarEventsToEws(account.EwsUrl, account.Username, password, oauthToken)
                        If num7 > 0 Then
                            LastBackgroundCalPushAfterThirdPass += num7
                            num2 = Math.Max(num2, num7)
                        End If
                    End If
                    If EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents() > 0 Then
                        Dim num8 = EwsLcarsBridge.TrySyncEwsCalendarBidirectional(account.EwsUrl, account.Username, password, oauthToken)
                        If num8 > 0 Then
                            LastBackgroundCalBidirectionalFourthPass += num8
                            num2 = Math.Max(num2, num8)
                        End If
                    End If
                    If EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents() > 0 Then
                        Dim num9 = EwsCalendarConflictHelper.TryAutoReconcileUnchangedDirty(account.EwsUrl, account.Username, password, oauthToken)
                        If num9 > 0 Then
                            LastBackgroundCalAutoReconcileSecondPass += num9
                            num2 = Math.Max(num2, num9)
                        End If
                    End If
                    If EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents() > 0 Then
                        Dim num10 = EwsCalendarConflictHelper.TryRefreshChangeKeysForDirtyEvents(account.EwsUrl, account.Username, password, oauthToken)
                        If num10 > 0 Then
                            LastBackgroundCalChangeKeyRefreshesSecondPass += num10
                            num2 = Math.Max(num2, num10)
                        End If
                    End If
                    If EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents() > 0 Then
                        Dim num11 = EwsLcarsBridge.TryPushDirtyCalendarEventsToEws(account.EwsUrl, account.Username, password, oauthToken)
                        If num11 > 0 Then
                            LastBackgroundCalPushAfterFourthPass += num11
                            num2 = Math.Max(num2, num11)
                        End If
                    End If
                    If EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents() > 0 Then
                        Dim num12 = EwsLcarsBridge.TrySyncEwsCalendarBidirectional(account.EwsUrl, account.Username, password, oauthToken)
                        If num12 > 0 Then
                            LastBackgroundCalBidirectionalFifthPass += num12
                            num2 = Math.Max(num2, num12)
                        End If
                    End If
                    If EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents() > 0 Then
                        Dim num13 = EwsCalendarConflictHelper.TryAutoReconcileUnchangedDirty(account.EwsUrl, account.Username, password, oauthToken)
                        If num13 > 0 Then
                            LastBackgroundCalAutoReconcileThirdPass += num13
                            num2 = Math.Max(num2, num13)
                        End If
                    End If
                    If EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents() > 0 Then
                        Dim num14 = EwsLcarsBridge.TryPushDirtyCalendarEventsToEws(account.EwsUrl, account.Username, password, oauthToken)
                        If num14 > 0 Then
                            LastBackgroundCalPushAfterFifthPass += num14
                            num2 = Math.Max(num2, num14)
                        End If
                    End If
                    If EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents() > 0 Then
                        Dim num15 = EwsLcarsBridge.TrySyncEwsCalendarBidirectional(account.EwsUrl, account.Username, password, oauthToken)
                        If num15 > 0 Then
                            LastBackgroundCalBidirectionalSixthPass += num15
                            num2 = Math.Max(num2, num15)
                        End If
                    End If
                    If EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents() > 0 Then
                        Dim num16 = EwsCalendarConflictHelper.TryAutoReconcileUnchangedDirty(account.EwsUrl, account.Username, password, oauthToken)
                        If num16 > 0 Then
                            LastBackgroundCalAutoReconcileFourthPass += num16
                            num2 = Math.Max(num2, num16)
                        End If
                    End If
                    If EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents() > 0 Then
                        Dim num17 = EwsCalendarConflictHelper.TryRefreshChangeKeysForDirtyEvents(account.EwsUrl, account.Username, password, oauthToken)
                        If num17 > 0 Then
                            LastBackgroundCalChangeKeyRefreshesThirdPass += num17
                            num2 = Math.Max(num2, num17)
                        End If
                    End If
                    If EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents() > 0 Then
                        Dim num18 = EwsLcarsBridge.TryPushDirtyCalendarEventsToEws(account.EwsUrl, account.Username, password, oauthToken)
                        If num18 > 0 Then
                            LastBackgroundCalPushAfterSixthPass += num18
                            num2 = Math.Max(num2, num18)
                        End If
                    End If
                    If EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents() > 0 Then
                        Dim num19 = EwsLcarsBridge.TrySyncEwsCalendarBidirectional(account.EwsUrl, account.Username, password, oauthToken)
                        If num19 > 0 Then
                            LastBackgroundCalBidirectionalSeventhPass += num19
                            num2 = Math.Max(num2, num19)
                        End If
                    End If
                    If EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents() > 0 Then
                        Dim num20 = EwsCalendarConflictHelper.TryAutoReconcileUnchangedDirty(account.EwsUrl, account.Username, password, oauthToken)
                        If num20 > 0 Then
                            LastBackgroundCalAutoReconcileFourthPass += num20
                            num2 = Math.Max(num2, num20)
                        End If
                    End If
                    If EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents() > 0 Then
                        Dim num21 = EwsLcarsBridge.TrySyncEwsCalendarBidirectional(account.EwsUrl, account.Username, password, oauthToken)
                        If num21 > 0 Then
                            LastBackgroundCalBidirectionalEighthPass += num21
                            num2 = Math.Max(num2, num21)
                        End If
                    End If
                End If
                If num2 > 0 Then
                    num += num2
                End If
                EwsLcarsBridge.TryMergePersonalContactsFromEws(account.EwsUrl, account.Username, password, oauthToken)
                If EwsGalSyncStore.ShouldRunGalSync() Then
                    Dim flag As Boolean = EwsGalSyncStore.ShouldRunDeepGalSync()
                    Dim num22 = EwsLcarsBridge.TrySyncGalDirectoryBidirectional(account.EwsUrl, account.Username, password, oauthToken, flag)
                    If num22 >= 0 Then
                        EwsGalSyncStore.MarkGalSyncCompleted(num22)
                        If flag Then
                            EwsGalSyncStore.MarkDeepGalSyncCompleted()
                        End If
                    End If
                    Dim num23 = 1
                    While AddressBookStore.CountGalPushDirtyContacts() > 0
                        Dim num24 = EwsLcarsBridge.TryPushGalDirtyPersonalShadows(account.EwsUrl, account.Username, password, oauthToken)
                        If num24 <= 0 Then
                            Exit While
                        End If
                        num22 = Math.Max(num22, num24)
                        RecordBackgroundGalPushPass(num23, num24)
                        num23 += 1
                        If num23 > 9 Then
                            Exit While
                        End If
                    End While
                Else
                    If AddressBookStore.CountGalPushDirtyContacts() <= 0 Then
                        Continue For
                    End If
                    Dim num25 = 1
                    While AddressBookStore.CountGalPushDirtyContacts() > 0
                        Dim num26 = EwsLcarsBridge.TryPushGalDirtyPersonalShadows(account.EwsUrl, account.Username, password, oauthToken)
                        If num26 <= 0 Then
                            Exit While
                        End If
                        RecordBackgroundGalPushPass(num25, num26)
                        num25 += 1
                        If num25 > 3 Then
                            Exit While
                        End If
                    End While
                End If
            Next
            TryFlushMeetingRepliesForAccounts(accounts)
            Return num
        End Function

        Private Shared Sub RecordBackgroundGalPushPass(pass As Integer, pushed As Integer)
            If pushed > 0 Then
                Select Case pass
                    Case 1
                        LastBackgroundGalPushRetry = Math.Max(LastBackgroundGalPushRetry, pushed)
                    Case 2
                        LastBackgroundGalPushSecondRetry += pushed
                    Case 3
                        LastBackgroundGalPushThirdRetry += pushed
                    Case 4
                        LastBackgroundGalPushFourthRetry += pushed
                    Case 5
                        LastBackgroundGalPushFifthRetry += pushed
                    Case 6
                        LastBackgroundGalPushSixthRetry += pushed
                    Case 7
                        LastBackgroundGalPushSeventhRetry += pushed
                    Case 8
                        LastBackgroundGalPushEighthRetry += pushed
                    Case 9
                        LastBackgroundGalPushNinthRetry += pushed
                End Select
            End If
        End Sub

        Private Shared Sub TryFlushMeetingRepliesForAccounts(accounts As IList(Of MailAccount))
            If accounts Is Nothing Then
                Return
            End If
            For Each account In accounts
                If account IsNot Nothing Then
                    Dim mailAccount = account
                    Dim text As String = If(Not Equals(mailAccount.SmtpHost, Nothing), mailAccount.SmtpHost, "").Trim()
                    If text.Length <> 0 Then
                        Dim password = ""
                        AccountStore.TryGetPassword(account.Id, password)
                        mailAccount = account
                        Dim fromAddress As String = If(Not Equals(mailAccount.EmailAddress, Nothing), mailAccount.EmailAddress, account.Username).Trim()
                        MailCalendarMeetingReplyBridge.TryFlushQueuedMeetingReplies(text, account.SmtpPort, fromAddress, account.Username, password)
                        Exit For
                    End If
                End If
            Next
        End Sub
    End Class

