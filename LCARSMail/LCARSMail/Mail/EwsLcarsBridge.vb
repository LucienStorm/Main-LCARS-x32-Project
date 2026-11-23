Imports System
Imports System.Collections.Generic
Public NotInheritable Class EwsLcarsBridge
    Public Shared LastError As String

        Public Shared LastCalendarPushCount As Integer

        Public Shared LastCalendarConflictOverwrites As Integer

        Public Shared LastCalendarAutoServerWins As Integer = 0

        Public Shared LastCalendarAutoClientWins As Integer = 0

        Public Shared LastCalendarAutoReconcileCleared As Integer = 0

        Public Shared LastCalendarChangeKeyRefreshes As Integer = 0

        Public Shared LastGalChangeKeyRefreshes As Integer = 0

        Public Shared LastGalPersonaChangeKeyRefreshes As Integer = 0

        Public Shared LastGalDirectoryChangeKeyRefreshes As Integer = 0

        Public Shared LastGalPersonalPushCount As Integer

        Public Shared LastGalPersonaUpdateAttempts As Integer

        Public Shared LastGalDirectoryUpdateAttempts As Integer

        Public Shared LastGalDirectoryResolveSuccess As Integer = 0

        Public Shared LastCalendarRsvpPushCount As Integer = 0

    Private Sub New()
    End Sub

        Public Shared Function TryMergeCalendarFromEws(ewsUrl As String, username As String, password As String, Optional oauthToken As String = Nothing) As Integer
        LastError = Nothing
            Dim list = EwsMailService.TryFindCalendarSummaries(ewsUrl, username, password, oauthToken)
            If list Is Nothing Then
                LastError = If(Not Equals(EwsMailService.LastError, Nothing), EwsMailService.LastError, "EWS calendar list failed.")
            Return -1
        End If
            Dim list2 As List(Of LcarsCalendarBridge.SharedCalendarEvent) = LcarsCalendarBridge.LoadEvents()
            Dim dictionary As Dictionary(Of String, LcarsCalendarBridge.SharedCalendarEvent) = New Dictionary(Of String, LcarsCalendarBridge.SharedCalendarEvent)(StringComparer.OrdinalIgnoreCase)
            Dim dictionary2 As Dictionary(Of String, LcarsCalendarBridge.SharedCalendarEvent) = New Dictionary(Of String, LcarsCalendarBridge.SharedCalendarEvent)(StringComparer.OrdinalIgnoreCase)
            For Each item In list2
                If item IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(item.Uid) Then
                    dictionary(item.Uid) = item
                    Dim sharedCalendarEvent = item
                    Dim text As String = If(Not Equals(sharedCalendarEvent.EwsItemId, Nothing), sharedCalendarEvent.EwsItemId, "").Trim()
                    If text.Length > 0 Then
                        dictionary2(text) = item
                    End If
                End If
            Next
            Dim hashSet As HashSet(Of String) = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            Dim num = 0
            For Each item2 In list
                If item2 Is Nothing OrElse String.IsNullOrWhiteSpace(item2.ItemId) Then
                    Continue For
                End If
                hashSet.Add(item2.ItemId.Trim())
                Dim ewsCalendarEventDetails = EwsMailService.TryGetCalendarEventDetails(ewsUrl, username, password, item2.ItemId, oauthToken)
                If ewsCalendarEventDetails Is Nothing Then
                    Continue For
                End If
                Dim ewsCalendarEventDetails2 = ewsCalendarEventDetails
                Dim text2 As String = If(Not Equals(ewsCalendarEventDetails2.Uid, Nothing), ewsCalendarEventDetails2.Uid, "").Trim()
                If text2.Length = 0 Then
                    text2 = item2.ItemId.Trim()
                End If
                Dim sharedCalendarEvent2 As LcarsCalendarBridge.SharedCalendarEvent = Nothing
                If dictionary.ContainsKey(text2) Then
                    sharedCalendarEvent2 = dictionary(text2)
                ElseIf dictionary2.ContainsKey(item2.ItemId.Trim()) Then
                    sharedCalendarEvent2 = dictionary2(item2.ItemId.Trim())
                    text2 = sharedCalendarEvent2.Uid
                End If
                If sharedCalendarEvent2 IsNot Nothing AndAlso sharedCalendarEvent2.EwsDirty Then
                    If Date.Compare(ewsCalendarEventDetails.LastModifiedUtc, Date.MinValue) <> 0 Then
                        If ewsCalendarEventDetails.ChangeKey.Length > 0 Then
                            sharedCalendarEvent2.EwsChangeKey = ewsCalendarEventDetails.ChangeKey
                        End If
                        If Date.Compare(ewsCalendarEventDetails.LastModifiedUtc, sharedCalendarEvent2.EwsLastModifiedUtc) > 0 Then
                            sharedCalendarEvent2.EwsLastModifiedUtc = ewsCalendarEventDetails.LastModifiedUtc
                        End If
                        dictionary(text2) = sharedCalendarEvent2
                        Dim sharedCalendarEvent = sharedCalendarEvent2
                        Dim text3 As String = If(Not Equals(sharedCalendarEvent.EwsItemId, Nothing), sharedCalendarEvent.EwsItemId, "").Trim()
                        If text3.Length > 0 Then
                            dictionary2(text3) = sharedCalendarEvent2
                        End If
                    End If
                Else
                    If sharedCalendarEvent2 IsNot Nothing AndAlso Date.Compare(sharedCalendarEvent2.EwsLastModifiedUtc, Date.MinValue) <> 0 AndAlso Date.Compare(ewsCalendarEventDetails.LastModifiedUtc, Date.MinValue) <> 0 AndAlso Date.Compare(sharedCalendarEvent2.EwsLastModifiedUtc, ewsCalendarEventDetails.LastModifiedUtc) > 0 Then
                        Continue For
                    End If
                    Dim sharedCalendarEvent3 As LcarsCalendarBridge.SharedCalendarEvent = New LcarsCalendarBridge.SharedCalendarEvent()
                    sharedCalendarEvent3.Uid = text2
                    ewsCalendarEventDetails2 = ewsCalendarEventDetails
                    sharedCalendarEvent3.Summary = If(Not Equals(ewsCalendarEventDetails2.Subject, Nothing), ewsCalendarEventDetails2.Subject, item2.Subject)
                    Dim ewsCalendarEventDetails3 = ewsCalendarEventDetails
                    sharedCalendarEvent3.Location = If(Not Equals(ewsCalendarEventDetails3.Location, Nothing), ewsCalendarEventDetails3.Location, "")
                    sharedCalendarEvent3.StartUtc = ewsCalendarEventDetails.StartUtc
                    sharedCalendarEvent3.EndUtc = ewsCalendarEventDetails.EndUtc
                    sharedCalendarEvent3.AlarmMinutesBefore = ewsCalendarEventDetails.AlarmMinutesBefore
                    sharedCalendarEvent3.TaskCompleted = False
                    Dim ewsCalendarEventDetails4 = ewsCalendarEventDetails
                    sharedCalendarEvent3.Category = If(Not Equals(ewsCalendarEventDetails4.Category, Nothing), ewsCalendarEventDetails4.Category, "")
                    Dim ewsCalendarEventDetails5 = ewsCalendarEventDetails
                    sharedCalendarEvent3.EwsItemId = If(Not Equals(ewsCalendarEventDetails5.ItemId, Nothing), ewsCalendarEventDetails5.ItemId, item2.ItemId)
                    Dim ewsCalendarEventDetails6 = ewsCalendarEventDetails
                    sharedCalendarEvent3.EwsChangeKey = If(Not Equals(ewsCalendarEventDetails6.ChangeKey, Nothing), ewsCalendarEventDetails6.ChangeKey, item2.ChangeKey)
                    sharedCalendarEvent3.EwsDirty = False
                    sharedCalendarEvent3.EwsLastModifiedUtc = ewsCalendarEventDetails.LastModifiedUtc
                    Dim sharedCalendarEvent4 = sharedCalendarEvent3
                    Dim sharedCalendarEvent As LcarsCalendarBridge.SharedCalendarEvent
                    If sharedCalendarEvent2 IsNot Nothing Then
                        If String.IsNullOrWhiteSpace(sharedCalendarEvent4.Location) Then
                            sharedCalendarEvent = sharedCalendarEvent2
                            sharedCalendarEvent4.Location = If(Not Equals(sharedCalendarEvent.Location, Nothing), sharedCalendarEvent.Location, "")
                        End If
                        sharedCalendarEvent = sharedCalendarEvent2
                        sharedCalendarEvent4.RecurrenceRule = If(Not Equals(sharedCalendarEvent.RecurrenceRule, Nothing), sharedCalendarEvent.RecurrenceRule, "")
                        If String.IsNullOrWhiteSpace(sharedCalendarEvent4.Category) Then
                            sharedCalendarEvent = sharedCalendarEvent2
                            sharedCalendarEvent4.Category = If(Not Equals(sharedCalendarEvent.Category, Nothing), sharedCalendarEvent.Category, "")
                        End If
                        If ewsCalendarEventDetails.AlarmMinutesBefore <= 0 AndAlso sharedCalendarEvent2.AlarmMinutesBefore > 0 Then
                            sharedCalendarEvent4.AlarmMinutesBefore = sharedCalendarEvent2.AlarmMinutesBefore
                        End If
                        sharedCalendarEvent = sharedCalendarEvent2
                        sharedCalendarEvent4.CalendarName = If(Not Equals(sharedCalendarEvent.CalendarName, Nothing), sharedCalendarEvent.CalendarName, "")
                        sharedCalendarEvent4.CalDavDirty = sharedCalendarEvent2.CalDavDirty
                    End If
                    If Not dictionary.ContainsKey(text2) Then
                        num = num + 1
                    End If
                    dictionary(text2) = sharedCalendarEvent4
                    sharedCalendarEvent = sharedCalendarEvent4
                    Dim text4 As String = If(Not Equals(sharedCalendarEvent.EwsItemId, Nothing), sharedCalendarEvent.EwsItemId, "").Trim()
                    If text4.Length > 0 Then
                        dictionary2(text4) = sharedCalendarEvent4
                    End If
                End If
            Next
            Dim list3 As List(Of String) = New List(Of String)()
            For Each item3 In dictionary
                Dim value = item3.Value
                If value IsNot Nothing AndAlso Not value.EwsDirty Then
                    Dim sharedCalendarEvent = value
                    Dim text5 As String = If(Not Equals(sharedCalendarEvent.EwsItemId, Nothing), sharedCalendarEvent.EwsItemId, "").Trim()
                    If text5.Length <> 0 AndAlso Not hashSet.Contains(text5) Then
                        list3.Add(item3.Key)
                    End If
                End If
            Next
            For Each item4 In list3
                dictionary.Remove(item4)
            Next
            LcarsCalendarBridge.SaveEvents(New List(Of LcarsCalendarBridge.SharedCalendarEvent)(dictionary.Values))
            Return num
    End Function

        Public Shared Function TryMergeContactsFromEws(ewsUrl As String, username As String, password As String, searchQuery As String, Optional oauthToken As String = Nothing) As Integer
        LastError = Nothing
            Dim text As String = If(Not Equals(searchQuery, Nothing), searchQuery, "").Trim()
            If text.Length = 0 Then
            LastError = "Contact search query is required."
            Return -1
        End If
            Dim list = EwsMailService.TryResolveContacts(ewsUrl, username, password, text, oauthToken)
            If list Is Nothing Then
                LastError = If(Not Equals(EwsMailService.LastError, Nothing), EwsMailService.LastError, "EWS ResolveNames failed.")
                Return -1
            End If
            Dim list2 As List(Of MailContact) = New List(Of MailContact)()
            For Each item In list
                If item IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(item.EmailAddress) Then
                    Dim mailContact As MailContact = New MailContact()
                    Dim ewsResolvedContact = item
                    mailContact.DisplayName = If(Not Equals(ewsResolvedContact.DisplayName, Nothing), ewsResolvedContact.DisplayName, item.EmailAddress).Trim()
                    mailContact.Email = item.EmailAddress.Trim()
                    Dim ewsResolvedContact2 = item
                    mailContact.Company = If(Not Equals(ewsResolvedContact2.Company, Nothing), ewsResolvedContact2.Company, "").Trim()
                    Dim ewsResolvedContact3 = item
                    mailContact.WorkPhone = If(Not Equals(ewsResolvedContact3.BusinessPhone, Nothing), ewsResolvedContact3.BusinessPhone, "").Trim()
                    list2.Add(mailContact)
                End If
            Next
            Return AddressBookStore.TryMergeContacts(list2)
        End Function

        Public Shared Function TrySyncGalDirectory(ewsUrl As String, username As String, password As String, Optional oauthToken As String = Nothing, Optional deepGalHarvest As Boolean = False) As Integer
            LastError = Nothing
            Dim list = EwsMailService.TryHarvestGalDirectory(ewsUrl, username, password, oauthToken, deepGalHarvest)
            If list Is Nothing Then
                LastError = If(Not Equals(EwsMailService.LastError, Nothing), EwsMailService.LastError, "GAL harvest failed.")
                Return -1
            End If
            If list.Count = 0 Then
                LastError = If(Not Equals(EwsMailService.LastError, Nothing), EwsMailService.LastError, "GAL harvest returned no contacts.")
                Return 0
            End If
            Dim list2 As List(Of MailContact) = New List(Of MailContact)()
            For Each item In list
                If item IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(item.EmailAddress) Then
                    Dim mailContact As MailContact = New MailContact()
                    Dim ewsResolvedContact = item
                    mailContact.DisplayName = If(Not Equals(ewsResolvedContact.DisplayName, Nothing), ewsResolvedContact.DisplayName, item.EmailAddress).Trim()
                    mailContact.Email = item.EmailAddress.Trim()
                    Dim ewsResolvedContact2 = item
                    mailContact.Company = If(Not Equals(ewsResolvedContact2.Company, Nothing), ewsResolvedContact2.Company, "").Trim()
                    Dim ewsResolvedContact3 = item
                    mailContact.WorkPhone = If(Not Equals(ewsResolvedContact3.BusinessPhone, Nothing), ewsResolvedContact3.BusinessPhone, "").Trim()
                    Dim ewsResolvedContact4 = item
                    mailContact.MobilePhone = If(Not Equals(ewsResolvedContact4.MobilePhone, Nothing), ewsResolvedContact4.MobilePhone, "").Trim()
                    Dim ewsResolvedContact5 = item
                    mailContact.FirstName = If(Not Equals(ewsResolvedContact5.FirstName, Nothing), ewsResolvedContact5.FirstName, "").Trim()
                    Dim ewsResolvedContact6 = item
                    mailContact.LastName = If(Not Equals(ewsResolvedContact6.LastName, Nothing), ewsResolvedContact6.LastName, "").Trim()
                    Dim ewsResolvedContact7 = item
                    mailContact.JobTitle = If(Not Equals(ewsResolvedContact7.JobTitle, Nothing), ewsResolvedContact7.JobTitle, "").Trim()
                    Dim ewsResolvedContact8 = item
                    mailContact.Department = If(Not Equals(ewsResolvedContact8.Department, Nothing), ewsResolvedContact8.Department, "").Trim()
                    Dim ewsResolvedContact9 = item
                    mailContact.WebPage = If(Not Equals(ewsResolvedContact9.WebPage, Nothing), ewsResolvedContact9.WebPage, "").Trim()
                    Dim ewsResolvedContact10 = item
                    mailContact.FaxNumber = If(Not Equals(ewsResolvedContact10.FaxNumber, Nothing), ewsResolvedContact10.FaxNumber, "").Trim()
                    Dim ewsResolvedContact11 = item
                    mailContact.PagerNumber = If(Not Equals(ewsResolvedContact11.PagerNumber, Nothing), ewsResolvedContact11.PagerNumber, "").Trim()
                    Dim ewsResolvedContact12 = item
                    mailContact.OfficeLocation = If(Not Equals(ewsResolvedContact12.OfficeLocation, Nothing), ewsResolvedContact12.OfficeLocation, "").Trim()
                    Dim ewsResolvedContact13 = item
                    mailContact.HomePhone = If(Not Equals(ewsResolvedContact13.HomePhone, Nothing), ewsResolvedContact13.HomePhone, "").Trim()
                    Dim ewsResolvedContact14 = item
                    mailContact.MiddleName = If(Not Equals(ewsResolvedContact14.MiddleName, Nothing), ewsResolvedContact14.MiddleName, "").Trim()
                    Dim ewsResolvedContact15 = item
                    mailContact.OtherPhone = If(Not Equals(ewsResolvedContact15.OtherPhone, Nothing), ewsResolvedContact15.OtherPhone, "").Trim()
                    Dim ewsResolvedContact16 = item
                    mailContact.AssistantName = If(Not Equals(ewsResolvedContact16.AssistantName, Nothing), ewsResolvedContact16.AssistantName, "").Trim()
                    mailContact.IsGalEntry = True
                    Dim ewsResolvedContact17 = item
                    mailContact.GalPersonaId = If(Not Equals(ewsResolvedContact17.GalPersonaId, Nothing), ewsResolvedContact17.GalPersonaId, "").Trim()
                    Dim ewsResolvedContact18 = item
                    mailContact.GalPersonaChangeKey = If(Not Equals(ewsResolvedContact18.GalPersonaChangeKey, Nothing), ewsResolvedContact18.GalPersonaChangeKey, "").Trim()
                    Dim ewsResolvedContact19 = item
                    mailContact.GalDirectoryItemId = If(Not Equals(ewsResolvedContact19.DirectoryContactItemId, Nothing), ewsResolvedContact19.DirectoryContactItemId, "").Trim()
                    Dim ewsResolvedContact20 = item
                    mailContact.GalDirectoryChangeKey = If(Not Equals(ewsResolvedContact20.DirectoryContactChangeKey, Nothing), ewsResolvedContact20.DirectoryContactChangeKey, "").Trim()
                    list2.Add(mailContact)
                End If
            Next
            Return AddressBookStore.TryMergeContacts(list2)
        End Function

        Public Shared Function TrySyncGalDirectoryBidirectional(ewsUrl As String, username As String, password As String, Optional oauthToken As String = Nothing, Optional deepGalHarvest As Boolean = False) As Integer
            LastGalPersonalPushCount = 0
            Dim num = TrySyncGalDirectory(ewsUrl, username, password, oauthToken, deepGalHarvest)
            If num < 0 Then
                Return num
            End If
            Dim val = TryPushGalDirtyPersonalShadows(ewsUrl, username, password, oauthToken)
            LastGalPersonalPushCount = Math.Max(0, val)
            AddressBookStore.LastGalPersonalPushCount = LastGalPersonalPushCount
            Return Math.Max(num, val)
        End Function

        Public Shared Function TryPushGalDirtyPersonalShadows(ewsUrl As String, username As String, password As String, Optional oauthToken As String = Nothing) As Integer
            LastError = Nothing
            LastGalPersonaUpdateAttempts = 0
            LastGalDirectoryUpdateAttempts = 0
            LastGalDirectoryResolveSuccess = 0
            LastGalChangeKeyRefreshes = 0
            LastGalPersonaChangeKeyRefreshes = 0
            LastGalDirectoryChangeKeyRefreshes = 0
            Dim list As List(Of MailContact) = AddressBookStore.LoadContacts()
            If list Is Nothing OrElse list.Count = 0 Then
                Return 0
            End If
            Dim num = 0
            For Each item In list
                If item IsNot Nothing AndAlso item.IsGalEntry AndAlso item.GalPushDirty Then
                    num += 1
                End If
            Next
            For Each item2 In list
                If item2 IsNot Nothing AndAlso item2.IsGalEntry AndAlso item2.GalPushDirty AndAlso Not String.IsNullOrWhiteSpace(item2.Email) Then
                    If EwsMailService.TryResolveDirectoryContactItemIds(ewsUrl, username, password, item2, oauthToken) Then
                        LastGalChangeKeyRefreshes += 1
                    End If
                    If EwsMailService.TryRefreshGalPersonaChangeKey(ewsUrl, username, password, item2, oauthToken) Then
                        LastGalPersonaChangeKeyRefreshes += 1
                    End If
                    If EwsMailService.TryRefreshGalDirectoryChangeKey(ewsUrl, username, password, item2, oauthToken) Then
                        LastGalDirectoryChangeKeyRefreshes += 1
                    End If
                End If
            Next
            If LastGalChangeKeyRefreshes > 0 OrElse LastGalPersonaChangeKeyRefreshes > 0 OrElse LastGalDirectoryChangeKeyRefreshes > 0 Then
                AddressBookStore.SaveContacts(list)
            End If
            Dim num2 = 0
            Dim num3 = 0
            Dim num4 = 0
            Dim num5 = 0
            Dim num6 = 0
            Dim flag = False
            Dim flag2 = False
            For Each item3 In list
                If item3 IsNot Nothing AndAlso item3.IsGalEntry AndAlso item3.GalPushDirty AndAlso Not String.IsNullOrWhiteSpace(item3.Email) AndAlso String.IsNullOrWhiteSpace(item3.GalDirectoryItemId) AndAlso EwsMailService.TryResolveDirectoryContactItemIds(ewsUrl, username, password, item3, oauthToken) Then
                    flag2 = True
                    LastGalDirectoryResolveSuccess += 1
                End If
            Next
            If flag2 Then
                AddressBookStore.SaveContacts(list)
            End If
            For Each item4 In list
                If item4 Is Nothing OrElse Not item4.IsGalEntry OrElse Not item4.GalPushDirty OrElse String.IsNullOrWhiteSpace(item4.Email) Then
                    Continue For
                End If
                Dim flag3 = False
                If Not String.IsNullOrWhiteSpace(item4.GalPersonaId) Then
                    LastGalPersonaUpdateAttempts += 1
                    If EwsMailService.TryUpdateGalPersonaContact(ewsUrl, username, password, item4, oauthToken) Then
                        item4.GalPushDirty = False
                        num2 += 1
                        num3 += 1
                        flag = True
                        flag3 = True
                    End If
                End If
                If Not flag3 AndAlso Not String.IsNullOrWhiteSpace(item4.GalDirectoryItemId) Then
                    LastGalDirectoryUpdateAttempts += 1
                    If EwsMailService.TryUpdateGalDirectoryContact(ewsUrl, username, password, item4, oauthToken) Then
                        item4.GalPushDirty = False
                        num2 += 1
                        num4 += 1
                        flag = True
                        flag3 = True
                    End If
                End If
                If flag3 Then
                    Continue For
                End If
                If Not String.IsNullOrWhiteSpace(item4.EwsPersonalItemId) Then
                    If EwsMailService.TryUpdatePersonalContactShadow(ewsUrl, username, password, item4, oauthToken) Then
                        item4.GalPushDirty = False
                        num2 += 1
                        num5 += 1
                        flag = True
                    End If
                    Continue For
                End If
                Dim text = EwsMailService.TryCreatePersonalContactShadow(ewsUrl, username, password, item4, oauthToken)
                If Not String.IsNullOrWhiteSpace(text) Then
                    item4.EwsPersonalItemId = text
                    item4.GalPushDirty = False
                    num2 += 1
                    num6 += 1
                    flag = True
                End If
            Next
            EwsGalWriteProofStore.RecordPushSession(num, LastGalPersonaUpdateAttempts, num3, LastGalDirectoryUpdateAttempts, num4, num5, num6, EwsMailService.LastGalPersonaUpdateDenied, EwsMailService.LastGalDirectoryUpdateDenied)
            If flag Then
                AddressBookStore.SaveContacts(list)
            End If
            LastGalPersonalPushCount = num2
            AddressBookStore.LastGalPersonalPushCount = num2
            Return num2
        End Function

        Public Shared Function TryMergePersonalContactsFromEws(ewsUrl As String, username As String, password As String, Optional oauthToken As String = Nothing) As Integer
            LastError = Nothing
            Dim list = EwsMailService.TryHarvestPersonalContactsFolder(ewsUrl, username, password, oauthToken)
            If list Is Nothing Then
                LastError = If(Not Equals(EwsMailService.LastError, Nothing), EwsMailService.LastError, "EWS contacts folder harvest failed.")
            Return -1
        End If
            If list.Count = 0 Then
                Return 0
            End If
            Dim list2 As List(Of MailContact) = New List(Of MailContact)()
            For Each item In list
                If item IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(item.EmailAddress) Then
                    Dim mailContact As MailContact = New MailContact()
                    Dim ewsResolvedContact = item
                    mailContact.DisplayName = If(Not Equals(ewsResolvedContact.DisplayName, Nothing), ewsResolvedContact.DisplayName, item.EmailAddress).Trim()
                    mailContact.Email = item.EmailAddress.Trim()
                    Dim ewsResolvedContact2 = item
                    mailContact.Company = If(Not Equals(ewsResolvedContact2.Company, Nothing), ewsResolvedContact2.Company, "").Trim()
                    Dim ewsResolvedContact3 = item
                    mailContact.WorkPhone = If(Not Equals(ewsResolvedContact3.BusinessPhone, Nothing), ewsResolvedContact3.BusinessPhone, "").Trim()
                    Dim ewsResolvedContact4 = item
                    mailContact.MobilePhone = If(Not Equals(ewsResolvedContact4.MobilePhone, Nothing), ewsResolvedContact4.MobilePhone, "").Trim()
                    Dim ewsResolvedContact5 = item
                    mailContact.FirstName = If(Not Equals(ewsResolvedContact5.FirstName, Nothing), ewsResolvedContact5.FirstName, "").Trim()
                    Dim ewsResolvedContact6 = item
                    mailContact.LastName = If(Not Equals(ewsResolvedContact6.LastName, Nothing), ewsResolvedContact6.LastName, "").Trim()
                    Dim ewsResolvedContact7 = item
                    mailContact.JobTitle = If(Not Equals(ewsResolvedContact7.JobTitle, Nothing), ewsResolvedContact7.JobTitle, "").Trim()
                    Dim ewsResolvedContact8 = item
                    mailContact.Department = If(Not Equals(ewsResolvedContact8.Department, Nothing), ewsResolvedContact8.Department, "").Trim()
                    Dim ewsResolvedContact9 = item
                    mailContact.WebPage = If(Not Equals(ewsResolvedContact9.WebPage, Nothing), ewsResolvedContact9.WebPage, "").Trim()
                    Dim ewsResolvedContact10 = item
                    mailContact.FaxNumber = If(Not Equals(ewsResolvedContact10.FaxNumber, Nothing), ewsResolvedContact10.FaxNumber, "").Trim()
                    Dim ewsResolvedContact11 = item
                    mailContact.PagerNumber = If(Not Equals(ewsResolvedContact11.PagerNumber, Nothing), ewsResolvedContact11.PagerNumber, "").Trim()
                    Dim ewsResolvedContact12 = item
                    mailContact.OfficeLocation = If(Not Equals(ewsResolvedContact12.OfficeLocation, Nothing), ewsResolvedContact12.OfficeLocation, "").Trim()
                    Dim ewsResolvedContact13 = item
                    mailContact.HomePhone = If(Not Equals(ewsResolvedContact13.HomePhone, Nothing), ewsResolvedContact13.HomePhone, "").Trim()
                    Dim ewsResolvedContact14 = item
                    mailContact.MiddleName = If(Not Equals(ewsResolvedContact14.MiddleName, Nothing), ewsResolvedContact14.MiddleName, "").Trim()
                    Dim ewsResolvedContact15 = item
                    mailContact.OtherPhone = If(Not Equals(ewsResolvedContact15.OtherPhone, Nothing), ewsResolvedContact15.OtherPhone, "").Trim()
                    Dim ewsResolvedContact16 = item
                    mailContact.AssistantName = If(Not Equals(ewsResolvedContact16.AssistantName, Nothing), ewsResolvedContact16.AssistantName, "").Trim()
                    list2.Add(mailContact)
                End If
            Next
            Return AddressBookStore.TryMergeContacts(list2)
        End Function

        Public Shared Function TryPushDirtyCalendarEventsToEws(ewsUrl As String, username As String, password As String, Optional oauthToken As String = Nothing) As Integer
            LastError = Nothing
            Dim val = TryProcessPendingCalendarRsvps(ewsUrl, username, password, oauthToken)
            Dim list As List(Of String) = EwsCalendarDeleteQueue.TakePendingDeletes()
            Dim num = 0
            If list IsNot Nothing AndAlso list.Count > 0 AndAlso EwsMailService.TryDeleteItems(ewsUrl, username, password, list, oauthToken) Then
                num = list.Count
            End If
            Dim list2 As List(Of LcarsCalendarBridge.SharedCalendarEvent) = LcarsCalendarBridge.LoadEvents()
            If list2 Is Nothing OrElse list2.Count = 0 Then
                Return 0
            End If
            LastCalendarChangeKeyRefreshes = EwsCalendarConflictHelper.TryRefreshChangeKeysForDirtyEvents(ewsUrl, username, password, oauthToken)
            Dim num2 = 0
            LastCalendarConflictOverwrites = 0
            LastCalendarAutoServerWins = 0
            LastCalendarAutoClientWins = 0
            For Each item In list2
                If item Is Nothing OrElse Not item.EwsDirty AndAlso (Not String.IsNullOrWhiteSpace(item.EwsItemId) OrElse 1 = 0) Then
                    Continue For
                End If
                If String.IsNullOrWhiteSpace(item.EwsItemId) Then
                    Dim changeKey As String = Nothing
                    Dim text = EwsMailService.TryCreateCalendarEvent(ewsUrl, username, password, item, oauthToken, changeKey)
                    If Not String.IsNullOrWhiteSpace(text) Then
                        item.EwsItemId = text
                        item.EwsChangeKey = If(Not Equals(changeKey, Nothing), changeKey, "")
                        item.EwsDirty = False
                        item.EwsLastModifiedUtc = Date.UtcNow
                        num2 += 1
                    End If
                Else
                    If Not item.EwsDirty Then
                        Continue For
                    End If
                    If String.IsNullOrWhiteSpace(item.EwsChangeKey) Then
                        Dim ewsCalendarEventDetails = EwsMailService.TryGetCalendarEventDetails(ewsUrl, username, password, item.EwsItemId, oauthToken)
                        If ewsCalendarEventDetails IsNot Nothing AndAlso ewsCalendarEventDetails.ChangeKey.Length > 0 Then
                            item.EwsChangeKey = ewsCalendarEventDetails.ChangeKey
                            If Date.Compare(ewsCalendarEventDetails.LastModifiedUtc, Date.MinValue) <> 0 Then
                                item.EwsLastModifiedUtc = ewsCalendarEventDetails.LastModifiedUtc
                            End If
                        End If
                    End If
                    Dim sharedCalendarEvent = item
                    Dim text2 = If(Not Equals(sharedCalendarEvent.EwsChangeKey, Nothing), sharedCalendarEvent.EwsChangeKey, "")
                    If EwsMailService.TryUpdateCalendarEvent(ewsUrl, username, password, item.EwsItemId, item, oauthToken) Then
                        item.EwsDirty = False
                        item.EwsLastModifiedUtc = Date.UtcNow
                        num2 += 1
                        If text2.Length > 0 Then
                            sharedCalendarEvent = item
                            If Not String.Equals(text2, If(Not Equals(sharedCalendarEvent.EwsChangeKey, Nothing), sharedCalendarEvent.EwsChangeKey, ""), StringComparison.Ordinal) Then
                                LastCalendarConflictOverwrites += 1
                            End If
                        End If
                        Continue For
                    End If
                    Dim ewsCalendarEventDetails2 = EwsMailService.TryGetCalendarEventDetails(ewsUrl, username, password, item.EwsItemId, oauthToken)
                    If ewsCalendarEventDetails2 IsNot Nothing AndAlso Date.Compare(ewsCalendarEventDetails2.LastModifiedUtc, Date.MinValue) <> 0 AndAlso Date.Compare(item.EwsLastModifiedUtc, Date.MinValue) <> 0 AndAlso Date.Compare(ewsCalendarEventDetails2.LastModifiedUtc, item.EwsLastModifiedUtc) > 0 Then
                        Dim ewsCalendarEventDetails3 = ewsCalendarEventDetails2
                        item.Summary = If(Not Equals(ewsCalendarEventDetails3.Subject, Nothing), ewsCalendarEventDetails3.Subject, item.Summary)
                        ewsCalendarEventDetails3 = ewsCalendarEventDetails2
                        item.Location = If(Not Equals(ewsCalendarEventDetails3.Location, Nothing), ewsCalendarEventDetails3.Location, item.Location)
                        item.StartUtc = ewsCalendarEventDetails2.StartUtc
                        item.EndUtc = ewsCalendarEventDetails2.EndUtc
                        ewsCalendarEventDetails3 = ewsCalendarEventDetails2
                        item.Category = If(Not Equals(ewsCalendarEventDetails3.Category, Nothing), ewsCalendarEventDetails3.Category, item.Category)
                        item.AlarmMinutesBefore = ewsCalendarEventDetails2.AlarmMinutesBefore
                        ewsCalendarEventDetails3 = ewsCalendarEventDetails2
                        item.EwsChangeKey = If(Not Equals(ewsCalendarEventDetails3.ChangeKey, Nothing), ewsCalendarEventDetails3.ChangeKey, item.EwsChangeKey)
                        item.EwsLastModifiedUtc = ewsCalendarEventDetails2.LastModifiedUtc
                        item.EwsDirty = False
                        item.CalDavDirty = True
                        LastCalendarAutoServerWins += 1
                        Continue For
                    End If
                    If ewsCalendarEventDetails2 IsNot Nothing AndAlso Date.Compare(item.EwsLastModifiedUtc, Date.MinValue) <> 0 AndAlso Date.Compare(ewsCalendarEventDetails2.LastModifiedUtc, Date.MinValue) <> 0 AndAlso Date.Compare(item.EwsLastModifiedUtc, ewsCalendarEventDetails2.LastModifiedUtc) > 0 Then
                        If ewsCalendarEventDetails2.ChangeKey.Length > 0 Then
                            item.EwsChangeKey = ewsCalendarEventDetails2.ChangeKey
                        End If
                        If EwsMailService.TryUpdateCalendarEvent(ewsUrl, username, password, item.EwsItemId, item, oauthToken) Then
                            item.EwsDirty = False
                            item.EwsLastModifiedUtc = Date.UtcNow
                            num2 += 1
                            LastCalendarAutoClientWins += 1
                        Else
                            item.EwsDirty = True
                        End If
                        Continue For
                    End If
                    If ewsCalendarEventDetails2 IsNot Nothing AndAlso EwsCalendarConflictHelper.TryReconcileSingleIfServerMatchesLocal(item, ewsCalendarEventDetails2) Then
                        num2 += 1
                        Continue For
                    End If
                    item.EwsDirty = True
                    If ewsCalendarEventDetails2 IsNot Nothing AndAlso ewsCalendarEventDetails2.ChangeKey.Length > 0 Then
                        item.EwsChangeKey = ewsCalendarEventDetails2.ChangeKey
                        If Date.Compare(ewsCalendarEventDetails2.LastModifiedUtc, Date.MinValue) <> 0 Then
                            item.EwsLastModifiedUtc = ewsCalendarEventDetails2.LastModifiedUtc
                        End If
                    End If
                End If
            Next
            LcarsCalendarBridge.SaveEvents(list2)
            LastCalendarPushCount = num2
            Return Math.Max(num2, val)
        End Function

        Private Shared Function TryProcessPendingCalendarRsvps(ewsUrl As String, username As String, password As String, Optional oauthToken As String = Nothing) As Integer
            LastCalendarRsvpPushCount = 0
            Dim list As List(Of EwsCalendarRsvpQueue.RsvpRequest) = EwsCalendarRsvpQueue.TakePendingRsvps()
            If list Is Nothing OrElse list.Count = 0 Then
                Return 0
            End If
            Dim list2 As List(Of LcarsCalendarBridge.SharedCalendarEvent) = LcarsCalendarBridge.LoadEvents()
            If list2 Is Nothing Then
                Return 0
            End If
            Dim num = 0
            For Each item In list
                If item Is Nothing OrElse String.IsNullOrWhiteSpace(item.Uid) Then
                    Continue For
                End If
                Dim rsvpRequest = item
                Dim text As String = If(Not Equals(rsvpRequest.PartStat, Nothing), rsvpRequest.PartStat, "").Trim().ToUpperInvariant()
                If text.Length = 0 Then
                    Continue For
                End If
                For Each item2 In list2
                    If item2 Is Nothing OrElse Not String.Equals(item2.Uid, item.Uid, StringComparison.OrdinalIgnoreCase) Then
                        Continue For
                    End If
                    item2.MyPartStat = text
                    item2.IsMeetingInvitation = True
                    If String.IsNullOrWhiteSpace(item2.EwsItemId) Then
                        item2.EwsDirty = True
                    ElseIf EwsMailService.TryUpdateCalendarEvent(ewsUrl, username, password, item2.EwsItemId, item2, oauthToken) Then
                        item2.EwsDirty = False
                        item2.CalDavDirty = True
                        num = num + 1
                    Else
                        item2.EwsDirty = True
                    End If
                    Exit For
                Next
            Next
            If num > 0 Then
                LcarsCalendarBridge.SaveEvents(list2)
            End If
            LastCalendarRsvpPushCount = num
            EwsCalendarRsvpQueue.LastProcessedCount = num
            Return num
    End Function

        Public Shared Function TrySyncEwsCalendarBidirectional(ewsUrl As String, username As String, password As String, Optional oauthToken As String = Nothing) As Integer
            LastError = Nothing
            LastCalendarAutoReconcileCleared = 0
            Dim num = TryMergeCalendarFromEws(ewsUrl, username, password, oauthToken)
            If num < 0 Then
                Return num
            End If
            Dim num2 = TryPushDirtyCalendarEventsToEws(ewsUrl, username, password, oauthToken)
            Dim num3 = 0
            While EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents() > 0
                Dim num4 = EwsCalendarConflictHelper.TryAutoReconcileUnchangedDirty(ewsUrl, username, password, oauthToken)
                If num4 <= 0 Then
                    num2 = Math.Max(num2, TryPushDirtyCalendarEventsToEws(ewsUrl, username, password, oauthToken))
                Else
                    num2 = Math.Max(num2, num4)
                    LastCalendarAutoReconcileCleared += num4
                End If
                num3 += 1
                If num3 > 4 Then
                    Exit While
                End If
            End While
            Return Math.Max(num, Math.Max(0, num2))
        End Function
End Class

