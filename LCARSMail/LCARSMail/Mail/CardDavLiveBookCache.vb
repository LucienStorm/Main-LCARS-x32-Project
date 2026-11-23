Imports System
Imports System.Collections.Generic
Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.VisualBasic.CompilerServices
    Public NotInheritable Class CardDavLiveBookCache
        Private Const FreshSeconds As Integer = 8

        Private Const FreshSecondsDirty As Integer = 4

        Public Shared LastFetchUtc As Date = Date.MinValue

        Private Const AlwaysOnRefreshMs As Integer = 12000

        Private Const AlwaysOnRefreshDirtyMs As Integer = 4000

        Public Shared LastAlwaysOnIntervalMs As Integer = 12000

        Private Shared ReadOnly gate As Object = RuntimeHelpers.GetObjectValue(New Object())

        Private Shared cached As List(Of MailContact)

        Private Shared refreshing As Boolean

        Private Shared alwaysOnTimer As Timer

        Public Shared LastRefreshSummary As String = ""

        Public Shared LastContactCount As Integer = 0

        Public Shared AlwaysOnActive As Boolean = False

        Public Shared LastAlwaysOnTickUtc As Date = Date.MinValue

        Public Shared LastAlwaysOnPushCount As Integer = 0

        Public Shared LastAlwaysOnDirtyCount As Integer = 0

        Public Shared LastAlwaysOnSyncUtc As Date = Date.MinValue

        Public Shared LastBidirectionalPushCount As Integer = 0

        Private Const BookDiscoveryIntervalSeconds As Integer = 3600

        Private Shared lastBookDiscoveryUtc As Date = Date.MinValue

        Public Shared LastDiscoveredBooksOnRefresh As Integer = 0

        Public Shared AlwaysOnCycleCount As Integer = 0

        Private Sub New()
        End Sub

        Public Shared Function GetLiveContacts() As List(Of MailContact)
            Dim obj = gate
            ObjectFlowControl.CheckForSyncLockOnValueType(obj)
            Dim lockTaken = False
            Try
                Monitor.Enter(obj, lockTaken)
                Dim num As Integer = (If((AddressBookStore.CountCardDavDirtyContacts() > 0), 4, 8))
                If cached IsNot Nothing AndAlso (Date.UtcNow - LastFetchUtc).TotalSeconds < num Then
                    Return CloneList(cached)
                End If

            Finally
                If lockTaken Then
                    Monitor.Exit(obj)
                End If
            End Try
            Return RefreshNow()
        End Function

        Public Shared Function RefreshNow() As List(Of MailContact)
            If AlwaysOnActive Then
                AlwaysOnCycleCount += 1
            End If
            Dim serverUrl As String = Nothing
            Dim username As String = Nothing
            Dim password As String = Nothing
            If AddressBookStore.TryLoadCardDavSettings(serverUrl, username, password) Then
                If (Date.UtcNow - lastBookDiscoveryUtc).TotalSeconds >= 3600.0 Then
                    LastDiscoveredBooksOnRefresh = AddressBookStore.TryRefreshDiscoveredCardDavBooksFromServer(serverUrl, username, password)
                    lastBookDiscoveryUtc = Date.UtcNow
                End If
                Dim num As Integer = AddressBookStore.TrySyncStoredCardDavBidirectional()
                If num >= 0 Then
                    AddressBookStore.TryMergeSecondaryThunderbirdCardDavBooks()
                    Dim list As List(Of MailContact) = AddressBookStore.LoadContacts()
                    Dim list2 As List(Of MailContact) = AddressBookStore.TryFetchLiveCardDavContacts()
                    If list2 IsNot Nothing AndAlso list2.Count > 0 Then
                        list = list2
                    End If
                    ReplaceCache(list)
                    Dim text As String = list.Count.ToString() & " contact(s) bidirectional"
                    If AddressBookStore.LastCardDavRemotePullCount > 0 Then
                        text = text & " · remote " & AddressBookStore.LastCardDavRemotePullCount.ToString()
                    End If
                    If AddressBookStore.LastCardDavPushCount > 0 Then
                        text = text & " · pushed " & AddressBookStore.LastCardDavPushCount.ToString()
                    End If
                    If CardDavSyncService.LastMergedAfter412 Then
                        text += " · 412 merged"
                    End If
                    If AddressBookStore.CountCardDavDirtyContacts() > 0 Then
                        Dim num2 As Integer = AddressBookStore.TryResolveAllCardDav412DirtyContacts()
                        If num2 > 0 Then
                            text = text & " · 412-FIX " & num2.ToString()
                        End If
                    End If
                    If LastDiscoveredBooksOnRefresh > 0 Then
                        text = text & " · BOOKS " & LastDiscoveredBooksOnRefresh.ToString()
                    End If
                    If AddressBookStore.LastCardDavSecondaryBooksMerged > 0 Then
                        text = text & " · 2ND+" & AddressBookStore.LastCardDavSecondaryBooksMerged.ToString()
                    End If
                    LastAlwaysOnPushCount = AddressBookStore.LastCardDavPushCount
                    LastBidirectionalPushCount = AddressBookStore.LastCardDavPushCount
                    LastAlwaysOnDirtyCount = AddressBookStore.CountCardDavDirtyContacts()
                    If String.Equals(CardDavSyncService.ConflictOn412Mode, "ServerWins", StringComparison.OrdinalIgnoreCase) Then
                        text += " · 412-SERVER"
                    ElseIf String.Equals(CardDavSyncService.ConflictOn412Mode, "ClientMerge", StringComparison.OrdinalIgnoreCase) Then
                        text += " · 412-CLIENT"
                    End If
                    If AlwaysOnActive Then
                        text += " · always-on"
                        If LastAlwaysOnPushCount > 0 Then
                            text = text & " · pushed " & LastAlwaysOnPushCount.ToString()
                        End If
                        If LastAlwaysOnDirtyCount > 0 Then
                            text = text & " · dirty " & LastAlwaysOnDirtyCount.ToString()
                        End If
                    End If
                    If LastAlwaysOnDirtyCount = 0 AndAlso LastAlwaysOnIntervalMs <> 12000 Then
                        text = text & " · idle " & 12.ToString() & "s"
                    ElseIf LastAlwaysOnDirtyCount > 0 Then
                        text = text & " · fast " & 4.ToString() & "s"
                    End If
                    LastRefreshSummary = text
                    Call RescheduleAlwaysOnTimer()
                    Return CloneList(list)
                End If
            End If
            Dim list3 As List(Of MailContact) = AddressBookStore.TryFetchLiveCardDavContacts()
            If list3 Is Nothing Then
                LastRefreshSummary = If(Not Equals(AddressBookStore.LastError, Nothing), AddressBookStore.LastError, "CardDAV live fetch failed")
                Return AddressBookStore.LoadContacts()
            End If
            ReplaceCache(list3)
            LastRefreshSummary = list3.Count.ToString() & " live contact(s)"
            Call RescheduleAlwaysOnTimer()
            Return CloneList(list3)
        End Function

        Public Shared Sub EnsureAlwaysOnStarted()
            Dim obj = gate
            ObjectFlowControl.CheckForSyncLockOnValueType(obj)
            Dim lockTaken = False
            Try
                Monitor.Enter(obj, lockTaken)
                If alwaysOnTimer IsNot Nothing Then
                    Return
                End If
                alwaysOnTimer = New Timer(period:=CSharpImpl.__Assign(LastAlwaysOnIntervalMs, GetRecommendedAlwaysOnIntervalMs()), callback:=AddressOf CardDavLiveBookCache.AlwaysOnTimerTick, state:=Nothing, dueTime:=2000)
                AlwaysOnActive = True
            Finally
                If lockTaken Then
                    Monitor.Exit(obj)
                End If
            End Try
            Call QueueBackgroundRefresh()
        End Sub

        Public Shared Function GetRecommendedAlwaysOnIntervalMs() As Integer
            If AddressBookStore.CountCardDavDirtyContacts() > 0 Then
                Return 4000
            End If
            Return 12000
        End Function

        Private Shared Sub RescheduleAlwaysOnTimer()
            Dim obj = gate
            ObjectFlowControl.CheckForSyncLockOnValueType(obj)
            Dim lockTaken = False
            Try
                Monitor.Enter(obj, lockTaken)
                If alwaysOnTimer IsNot Nothing Then
                    Dim period As Integer = (CSharpImpl.__Assign(LastAlwaysOnIntervalMs, GetRecommendedAlwaysOnIntervalMs()))
                    alwaysOnTimer.Change(1000, period)
                End If

            Finally
                If lockTaken Then
                    Monitor.Exit(obj)
                End If
            End Try
        End Sub

        Private Shared Sub AlwaysOnTimerTick(state As Object)
            LastAlwaysOnTickUtc = Date.UtcNow
            Dim serverUrl As String = Nothing
            Dim username As String = Nothing
            Dim password As String = Nothing
            If AddressBookStore.TryLoadCardDavSettings(serverUrl, username, password) AndAlso Not String.IsNullOrWhiteSpace(serverUrl) Then
                LastAlwaysOnDirtyCount = AddressBookStore.CountCardDavDirtyContacts()
                Call QueueBackgroundRefresh()
            End If
        End Sub

        Public Shared Sub QueueBackgroundRefresh()
            Dim obj = gate
            ObjectFlowControl.CheckForSyncLockOnValueType(obj)
            Dim lockTaken = False
            Try
                Monitor.Enter(obj, lockTaken)
                If refreshing Then
                    Return
                End If
                refreshing = True
            Finally
                If lockTaken Then
                    Monitor.Exit(obj)
                End If
            End Try
            Call ThreadPool.QueueUserWorkItem(Sub(state)
                                                  Try
                                                      Call RefreshNow()
                                                  Finally
                                                      Dim obj2 = gate
                                                      ObjectFlowControl.CheckForSyncLockOnValueType(obj2)
                                                      Dim lockTaken2 = False
                                                      Try
                                                          Monitor.Enter(obj2, lockTaken2)
                                                          refreshing = False
                                                      Finally
                                                          If lockTaken2 Then
                                                              Monitor.Exit(obj2)
                                                          End If
                                                      End Try
                                                  End Try
                                              End Sub)
        End Sub

        Public Shared Sub NotifyLocalBookUpdated(merged As List(Of MailContact))
            If merged IsNot Nothing AndAlso merged.Count <> 0 Then
                ReplaceCache(merged)
                If AddressBookStore.CountCardDavDirtyContacts() > 0 Then
                    Call QueueBackgroundRefresh()
                End If
            End If
        End Sub

        Private Shared Sub ReplaceCache(contacts As List(Of MailContact))
            Dim obj = gate
            ObjectFlowControl.CheckForSyncLockOnValueType(obj)
            Dim lockTaken = False
            Try
                Monitor.Enter(obj, lockTaken)
                cached = CloneList(contacts)
                LastFetchUtc = Date.UtcNow
                LastAlwaysOnSyncUtc = Date.UtcNow
                LastContactCount = If(contacts IsNot Nothing, contacts.Count, 0)
            Finally
                If lockTaken Then
                    Monitor.Exit(obj)
                End If
            End Try
        End Sub

        Private Shared Function CloneList(source As List(Of MailContact)) As List(Of MailContact)
            If source Is Nothing Then
                Return New List(Of MailContact)()
            End If
            Return New List(Of MailContact)(source)
        End Function

        Private Class CSharpImpl
            Shared Function __Assign(Of T)(ByRef target As T, value As T) As T
                target = value
                Return value
            End Function
        End Class
    End Class

