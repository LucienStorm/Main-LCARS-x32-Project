Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Text
Imports System.Web.Script.Serialization
Imports Microsoft.VisualBasic.CompilerServices
    <StandardModule>
    Public NotInheritable Class LcarsCalendarBridge
        Public Class SharedCalendarEvent
            Public Uid As String

            Public Summary As String

            Public Location As String

            Public StartUtc As Date

            Public EndUtc As Date

            Public AlarmMinutesBefore As Integer

            Public TaskCompleted As Boolean

            Public EwsItemId As String

            Public EwsChangeKey As String

            Public EwsDirty As Boolean

            Public EwsLastModifiedUtc As Date

            Public RecurrenceRule As String

            Public RecurrenceIdUtc As Date

            Public RecurrenceParentUid As String

            Public ExDateUtcCsv As String

            Public Category As String

            Public CalendarName As String

            Public CalDavDirty As Boolean

            Public Organizer As String

            Public AttendeeMailto As String

            Public MyPartStat As String

            Public IsMeetingInvitation As Boolean

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Private Class CalDavSettingsDto
            Public Property CalendarUrl As String

            Public Property Username As String

            Public Property TaskCollectionUrl As String

            Public Property SecondaryCalendarUrls As List(Of String)

            Public Property CalendarUrlLabels As Dictionary(Of String, String)

            Public Property PrimaryCalendarDisplayName As String

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Public Shared LastCalDavPrePushCount As Integer = 0

        Public Shared LastCalDavPushCount As Integer = 0

        Public Shared LastCalDavTaskCollectionPullCount As Integer = 0

        Public Shared LastCalDavSecondaryCalendarPullCount As Integer = 0

        Public Shared LastCalDavDiscoveredCalendars As Integer = 0

        Public Shared LastCalDavBidirectionalUtc As Date = Date.MinValue

        Public Shared LastCalDavPrimaryEventsTagged As Integer = 0

        Public Shared LastCalDavSecondaryEventsTagged As Integer = 0

        Public Shared LastCalDavTaskEventsTagged As Integer = 0

        Public Shared LastCalDavPullMergeAdded As Integer = 0

        Public Shared LastCalDavSecondaryPullMergeAdded As Integer = 0

        Public Shared LastCalDavUrlLabelsStored As Integer = 0

        Public Shared LastCalDavPrimaryPullMergeAdded As Integer = 0

        Public Shared LastCalDavTaskPullMergeAdded As Integer = 0

        Private Const EventsFileName As String = "events.json"

        Private Const CalDavSettingsFileName As String = "caldav-settings.json"

        Private Const CalDavSecretKey As String = "caldav"

        Public Shared Function CountCalDavDirtyEvents() As Integer
            Dim num = 0
            For Each item In LoadEvents()
                If item IsNot Nothing AndAlso item.CalDavDirty Then
                    num = num + 1
                End If
            Next
            Return num
        End Function

        Public Shared Function CountDistinctCalendarNames() As Integer
            Dim hashSet As HashSet(Of String) = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            For Each item In LoadEvents()
                If item IsNot Nothing Then
                    Dim sharedCalendarEvent = item
                    Dim text As String = If(Not Equals(sharedCalendarEvent.CalendarName, Nothing), sharedCalendarEvent.CalendarName, "").Trim()
                    If text.Length = 0 Then
                        text = "(default)"
                    End If
                    hashSet.Add(text)
                End If
            Next
            Return hashSet.Count
        End Function

        Public Shared Function CountEventsWithRecurrenceRule() As Integer
            Dim num = 0
            For Each item In LoadEvents()
                If item IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(item.RecurrenceRule) Then
                    num = num + 1
                End If
            Next
            Return num
        End Function

        Public Shared Function CountEventsWithCategory() As Integer
            Dim num = 0
            For Each item In LoadEvents()
                If item IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(item.Category) Then
                    num = num + 1
                End If
            Next
            Return num
        End Function

        Public Shared Function CountEventsLinkedToEws() As Integer
            Dim num = 0
            For Each item In LoadEvents()
                If item IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(item.EwsItemId) Then
                    num = num + 1
                End If
            Next
            Return num
        End Function

        Public Shared Function CalendarDataRoot() As String
            Dim text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LCARS", "Calendar")
            If Not Directory.Exists(text) Then
                Directory.CreateDirectory(text)
            End If
            Return text
        End Function

        Public Shared Function LoadEvents() As List(Of SharedCalendarEvent)
            Dim path As String = IO.Path.Combine(CalendarDataRoot(), "events.json")
            Dim result As List(Of SharedCalendarEvent)
            If Not File.Exists(path) Then
                result = New List(Of SharedCalendarEvent)()
            Else
                Try
                    Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                    Dim list = javaScriptSerializer.Deserialize(Of List(Of SharedCalendarEvent))(File.ReadAllText(path, Encoding.UTF8))
                    result = (If(list IsNot Nothing, list, New List(Of SharedCalendarEvent)()))
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    result = New List(Of SharedCalendarEvent)()
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Public Shared Sub SaveEvents(events As List(Of SharedCalendarEvent))
            If events IsNot Nothing Then
                Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                Call File.WriteAllText(Path.Combine(CalendarDataRoot(), "events.json"), javaScriptSerializer.Serialize(events), Encoding.UTF8)
            End If
        End Sub

        Public Shared Function MergeCalDavPull(remote As List(Of CalDavEvent)) As Integer
            If remote Is Nothing OrElse remote.Count = 0 Then
                Return 0
            End If
            Dim list As List(Of SharedCalendarEvent) = LoadEvents()
            Dim dictionary As Dictionary(Of String, SharedCalendarEvent) = New Dictionary(Of String, SharedCalendarEvent)(StringComparer.OrdinalIgnoreCase)
            For Each item In list
                If item IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(item.Uid) Then
                    dictionary(item.Uid) = item
                End If
            Next
            Dim num = 0
            For Each item2 In remote
                If item2 Is Nothing Then
                    Continue For
                End If
                Dim calDavEvent = item2
                Dim text As String = If(Not Equals(calDavEvent.Uid, Nothing), calDavEvent.Uid, "").Trim()
                If text.Length = 0 Then
                    text = Guid.NewGuid().ToString()
                End If
                Dim sharedCalendarEvent As SharedCalendarEvent = Nothing
                If dictionary.ContainsKey(text) Then
                    sharedCalendarEvent = dictionary(text)
                End If
                If sharedCalendarEvent Is Nothing OrElse Not sharedCalendarEvent.CalDavDirty Then
                    Dim sharedCalendarEvent2 As SharedCalendarEvent = New SharedCalendarEvent()
                    sharedCalendarEvent2.Uid = text
                    calDavEvent = item2
                    sharedCalendarEvent2.Summary = If(Not Equals(calDavEvent.Summary, Nothing), calDavEvent.Summary, "")
                    sharedCalendarEvent2.StartUtc = item2.StartUtc
                    sharedCalendarEvent2.EndUtc = item2.EndUtc
                    sharedCalendarEvent2.AlarmMinutesBefore = item2.AlarmMinutesBefore
                    sharedCalendarEvent2.TaskCompleted = item2.TaskCompleted
                    Dim calDavEvent2 = item2
                    sharedCalendarEvent2.Location = If(Not Equals(calDavEvent2.Location, Nothing), calDavEvent2.Location, "")
                    Dim calDavEvent3 = item2
                    sharedCalendarEvent2.RecurrenceRule = If(Not Equals(calDavEvent3.RecurrenceRule, Nothing), calDavEvent3.RecurrenceRule, "")
                    Dim calDavEvent4 = item2
                    sharedCalendarEvent2.Category = If(Not Equals(calDavEvent4.Category, Nothing), calDavEvent4.Category, "")
                    Dim calDavEvent5 = item2
                    sharedCalendarEvent2.CalendarName = If(Not Equals(calDavEvent5.CalendarName, Nothing), calDavEvent5.CalendarName, "")
                    sharedCalendarEvent2.CalDavDirty = False
                    Dim calDavEvent6 = item2
                    sharedCalendarEvent2.Organizer = If(Not Equals(calDavEvent6.Organizer, Nothing), calDavEvent6.Organizer, "")
                    Dim calDavEvent7 = item2
                    sharedCalendarEvent2.AttendeeMailto = If(Not Equals(calDavEvent7.AttendeeMailto, Nothing), calDavEvent7.AttendeeMailto, "")
                    Dim calDavEvent8 = item2
                    sharedCalendarEvent2.MyPartStat = If(Not Equals(calDavEvent8.MyPartStat, Nothing), calDavEvent8.MyPartStat, "")
                    sharedCalendarEvent2.IsMeetingInvitation = item2.IsMeetingInvitation
                    Dim calDavEvent9 = item2
                    sharedCalendarEvent2.ExDateUtcCsv = If(Not Equals(calDavEvent9.ExDateUtcCsv, Nothing), calDavEvent9.ExDateUtcCsv, "")
                    Dim sharedCalendarEvent3 = sharedCalendarEvent2
                    If sharedCalendarEvent IsNot Nothing Then
                        sharedCalendarEvent3.EwsItemId = sharedCalendarEvent.EwsItemId
                        sharedCalendarEvent3.EwsChangeKey = sharedCalendarEvent.EwsChangeKey
                        sharedCalendarEvent3.EwsDirty = sharedCalendarEvent.EwsDirty
                        sharedCalendarEvent3.EwsLastModifiedUtc = sharedCalendarEvent.EwsLastModifiedUtc
                    End If
                    If Not dictionary.ContainsKey(text) Then
                        num = num + 1
                    End If
                    dictionary(text) = sharedCalendarEvent3
                End If
            Next
            Call SaveEvents(New List(Of SharedCalendarEvent)(dictionary.Values))
            Return num
        End Function

        Public Shared Sub SaveCalDavSettings(calendarUrl As String, username As String, password As String)
            Dim text As String = If(Not Equals(calendarUrl, Nothing), calendarUrl, "").Trim()
            If text.Length = 0 Then
                Return
            End If
            Try
                Dim calDavSettingsDto As CalDavSettingsDto = New CalDavSettingsDto()
                calDavSettingsDto.CalendarUrl = text
                calDavSettingsDto.Username = If(Not Equals(username, Nothing), username, "").Trim()
                Dim obj = calDavSettingsDto
                Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                Call File.WriteAllText(CalDavSettingsPath(), javaScriptSerializer.Serialize(obj), Encoding.UTF8)
                If Not Equals(password, Nothing) Then
                    AccountStore.SavePassword("caldav", password)
                End If
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
        End Sub

        Public Shared Function TryLoadCalDavSettings(ByRef calendarUrl As String, ByRef username As String, ByRef password As String) As Boolean
            calendarUrl = Nothing
            username = Nothing
            password = Nothing
            Dim result As Boolean
            If Not File.Exists(CalDavSettingsPath()) Then
                result = False
            Else
                Try
                    Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                    Dim calDavSettingsDto As CalDavSettingsDto = javaScriptSerializer.Deserialize(Of CalDavSettingsDto)(File.ReadAllText(CalDavSettingsPath(), Encoding.UTF8))
                    If calDavSettingsDto Is Nothing OrElse String.IsNullOrWhiteSpace(calDavSettingsDto.CalendarUrl) Then
                        result = False
                    Else
                        calendarUrl = calDavSettingsDto.CalendarUrl.Trim()
                        Dim username2 = calDavSettingsDto.Username
                        username = If(Not Equals(username2, Nothing), username2, "").Trim()
                        result = AccountStore.TryGetPassword("caldav", password)
                    End If
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    result = False
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Public Shared Function TryAutoDiscoverAndSaveTaskCollectionUrl() As Boolean
            Dim calendarUrl As String = Nothing
            Dim username As String = Nothing
            Dim password As String = Nothing
            If Not TryLoadCalDavSettings(calendarUrl, username, password) Then
                Return False
            End If
            If TryLoadCalDavTaskCollectionUrl().Length > 0 Then
                Return False
            End If
            Dim text = CalDavSyncService.TryDiscoverVtodoCollectionUrl(calendarUrl, username, password)
            If text.Length = 0 Then
                Return False
            End If
            TrySaveCalDavTaskCollectionUrl(text)
            Return True
        End Function

        Public Shared Function TryEnsureTaskCollectionUrlDiscovered() As Boolean
            If TryLoadCalDavTaskCollectionUrl().Length > 0 Then
                Return True
            End If
            Return TryAutoDiscoverAndSaveTaskCollectionUrl()
        End Function

        Private Shared Sub TrySaveCalDavTaskCollectionUrl(taskCollectionUrl As String)
            Dim text As String = If(Not Equals(taskCollectionUrl, Nothing), taskCollectionUrl, "").Trim()
            If text.Length = 0 Then
                Return
            End If
            Try
                Dim path As String = CalDavSettingsPath()
                If File.Exists(path) Then
                    Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                    Dim calDavSettingsDto = javaScriptSerializer.Deserialize(Of CalDavSettingsDto)(File.ReadAllText(path, Encoding.UTF8))
                    If calDavSettingsDto IsNot Nothing Then
                        calDavSettingsDto.TaskCollectionUrl = text
                        File.WriteAllText(path, javaScriptSerializer.Serialize(calDavSettingsDto), Encoding.UTF8)
                    End If
                End If
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
        End Sub

        Public Shared Function TryRefreshDiscoveredVeventCalendarsFromServer(calendarUrl As String, username As String, password As String) As Integer
            LastCalDavDiscoveredCalendars = 0
            Dim list = CalDavSyncService.EnumerateVeventCalendarDescriptors(calendarUrl, username, password)
            If list Is Nothing OrElse list.Count = 0 Then
                Return 0
            End If
            Dim list2 As List(Of String) = New List(Of String)()
            Dim dictionary As Dictionary(Of String, String) = New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            For Each item In list
                If item IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(item.Url) Then
                    Dim text As String = item.Url.Trim()
                    list2.Add(text)
                    Dim calDavCollectionDescriptor = item
                    Dim text2 As String = If(Not Equals(calDavCollectionDescriptor.DisplayName, Nothing), calDavCollectionDescriptor.DisplayName, "").Trim()
                    If text2.Length > 0 Then
                        dictionary(text) = text2
                    End If
                End If
            Next
            Dim text3 As String = calendarUrl.Trim()
            If text3.Length > 0 AndAlso Not dictionary.ContainsKey(text3) Then
                Dim text4 = CalDavSyncService.TryGetCollectionDisplayName(text3, username, password)
                If text4.Length > 0 Then
                    dictionary(text3) = text4
                End If
            End If
            TrySaveSecondaryCalendarUrls(list2, dictionary)
            LastCalDavDiscoveredCalendars = list2.Count
            Return list2.Count
        End Function

        Private Shared Sub TrySaveSecondaryCalendarUrls(calendarUrls As List(Of String), Optional urlLabels As Dictionary(Of String, String) = Nothing)
            If calendarUrls Is Nothing OrElse calendarUrls.Count = 0 Then
                Return
            End If
            Try
                Dim path As String = CalDavSettingsPath()
                If Not File.Exists(path) Then
                    Return
                End If
                Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                Dim calDavSettingsDto = javaScriptSerializer.Deserialize(Of CalDavSettingsDto)(File.ReadAllText(path, Encoding.UTF8))
                If calDavSettingsDto Is Nothing Then
                    Return
                End If
                calDavSettingsDto.SecondaryCalendarUrls = calendarUrls
                If urlLabels IsNot Nothing AndAlso urlLabels.Count > 0 Then
                    calDavSettingsDto.CalendarUrlLabels = urlLabels
                    If Not Equals(calDavSettingsDto.CalendarUrl, Nothing) AndAlso calDavSettingsDto.CalendarUrl.Trim().Length > 0 Then
                        Dim key As String = calDavSettingsDto.CalendarUrl.Trim()
                        Dim value As String = Nothing
                        If urlLabels.TryGetValue(key, value) AndAlso Not Equals(value, Nothing) AndAlso value.Trim().Length > 0 Then
                            calDavSettingsDto.PrimaryCalendarDisplayName = value.Trim()
                        End If
                    End If
                End If
                File.WriteAllText(path, javaScriptSerializer.Serialize(calDavSettingsDto), Encoding.UTF8)
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
        End Sub

        Private Shared Function TryLoadPrimaryCalendarDisplayName() As String
            Dim result As String
            Try
                If Not File.Exists(CalDavSettingsPath()) Then
                    result = ""
                Else
                    Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                    Dim calDavSettingsDto As CalDavSettingsDto = javaScriptSerializer.Deserialize(Of CalDavSettingsDto)(File.ReadAllText(CalDavSettingsPath(), Encoding.UTF8))
                    If calDavSettingsDto Is Nothing Then
                        result = ""
                    Else
                        Dim primaryCalendarDisplayName = calDavSettingsDto.PrimaryCalendarDisplayName
                        result = If(Not Equals(primaryCalendarDisplayName, Nothing), primaryCalendarDisplayName, "").Trim()
                    End If
                End If
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                result = ""
                Call ProjectData.ClearProjectError()
            End Try
            Return result
        End Function

        Public Shared Function CountStoredCalendarUrlLabels() As Integer
            Dim result As Integer
            Try
                If Not File.Exists(CalDavSettingsPath()) Then
                    result = 0
                Else
                    Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                    Dim calDavSettingsDto As CalDavSettingsDto = javaScriptSerializer.Deserialize(Of CalDavSettingsDto)(File.ReadAllText(CalDavSettingsPath(), Encoding.UTF8))
                    result = If(calDavSettingsDto IsNot Nothing AndAlso calDavSettingsDto.CalendarUrlLabels IsNot Nothing, calDavSettingsDto.CalendarUrlLabels.Count, 0)
                End If
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                result = 0
                Call ProjectData.ClearProjectError()
            End Try
            Return result
        End Function

        Public Shared Function TryResolveCalendarDisplayLabel(calendarNameOrUrl As String) As String
            Dim text As String = If(Not Equals(calendarNameOrUrl, Nothing), calendarNameOrUrl, "").Trim()
            If text.Length = 0 Then
                Return ""
            End If
            Try
                If Not File.Exists(CalDavSettingsPath()) Then
                    Return text
                End If
                Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                Dim calDavSettingsDto As CalDavSettingsDto = javaScriptSerializer.Deserialize(Of CalDavSettingsDto)(File.ReadAllText(CalDavSettingsPath(), Encoding.UTF8))
                If calDavSettingsDto Is Nothing OrElse calDavSettingsDto.CalendarUrlLabels Is Nothing Then
                    Return text
                End If
                For Each calendarUrlLabel In calDavSettingsDto.CalendarUrlLabels
                    Dim key = calendarUrlLabel.Key
                    Dim text2 As String = If(Not Equals(key, Nothing), key, "").Trim()
                    If text2.Length = 0 Then
                        Continue For
                    End If
                    Dim fileName As String = Path.GetFileName(text2.Trim().TrimEnd("/"c))
                    If text.Equals(text2, StringComparison.OrdinalIgnoreCase) OrElse text.Equals(fileName, StringComparison.OrdinalIgnoreCase) Then
                        key = calendarUrlLabel.Value
                        Dim text3 As String = If(Not Equals(key, Nothing), key, "").Trim()
                        If text3.Length > 0 Then
                            Return text3
                        End If
                    End If
                Next
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
            Return text
        End Function

        Private Shared Function TryLoadSecondaryCalendarUrls() As List(Of String)
            Dim result As List(Of String)
            Try
                If Not File.Exists(CalDavSettingsPath()) Then
                    result = New List(Of String)()
                Else
                    Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                    Dim calDavSettingsDto As CalDavSettingsDto = javaScriptSerializer.Deserialize(Of CalDavSettingsDto)(File.ReadAllText(CalDavSettingsPath(), Encoding.UTF8))
                    result = (If(calDavSettingsDto IsNot Nothing AndAlso calDavSettingsDto.SecondaryCalendarUrls IsNot Nothing, calDavSettingsDto.SecondaryCalendarUrls, New List(Of String)()))
                End If
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                result = New List(Of String)()
                Call ProjectData.ClearProjectError()
            End Try
            Return result
        End Function

        Public Shared Function TrySyncStoredCalDav() As Integer
            Dim calendarUrl As String = Nothing
            Dim username As String = Nothing
            Dim password As String = Nothing
            If Not TryLoadCalDavSettings(calendarUrl, username, password) Then
                Return 0
            End If
            TryRefreshDiscoveredVeventCalendarsFromServer(calendarUrl, username, password)
            LastCalDavUrlLabelsStored = CountStoredCalendarUrlLabels()
            Call TryAutoDiscoverAndSaveTaskCollectionUrl()
            Dim list = CalDavSyncService.FetchEvents(calendarUrl, username, password)
            If list Is Nothing Then
                Return -1
            End If
            Dim text As String = TryResolveCalendarDisplayLabel(calendarUrl.Trim())
            If text.Length = 0 Then
                text = TryLoadPrimaryCalendarDisplayName()
            End If
            LastCalDavPrimaryEventsTagged = 0
            For Each item In list
                If item IsNot Nothing AndAlso String.IsNullOrWhiteSpace(item.CalendarName) AndAlso text.Length > 0 Then
                    item.CalendarName = text
                    LastCalDavPrimaryEventsTagged += 1
                End If
            Next
            Dim num = MergeCalDavPull(list)
            LastCalDavPrimaryPullMergeAdded = Math.Max(0, num)
            LastCalDavSecondaryCalendarPullCount = 0
            LastCalDavSecondaryEventsTagged = 0
            LastCalDavSecondaryPullMergeAdded = 0
            For Each item2 In TryLoadSecondaryCalendarUrls()
                If String.IsNullOrWhiteSpace(item2) OrElse String.Equals(item2.Trim(), calendarUrl.Trim(), StringComparison.OrdinalIgnoreCase) Then
                    Continue For
                End If
                Dim list2 = CalDavSyncService.FetchEvents(item2, username, password)
                If list2 Is Nothing OrElse list2.Count = 0 Then
                    Continue For
                End If
                Dim text2 As String = TryResolveCalendarDisplayLabel(item2.Trim())
                If text2.Length = 0 Then
                    text2 = Path.GetFileName(item2.Trim().TrimEnd("/"c))
                End If
                For Each item3 In list2
                    If item3 IsNot Nothing AndAlso String.IsNullOrWhiteSpace(item3.CalendarName) AndAlso text2.Length > 0 Then
                        item3.CalendarName = text2
                        LastCalDavSecondaryEventsTagged += 1
                    ElseIf item3 IsNot Nothing AndAlso String.IsNullOrWhiteSpace(item3.CalendarName) Then
                        item3.CalendarName = text2
                    End If
                Next
                LastCalDavSecondaryCalendarPullCount += list2.Count
                Dim num2 = num
                num += MergeCalDavPull(list2)
                LastCalDavSecondaryPullMergeAdded += Math.Max(0, num - num2)
            Next
            Dim text3 As String = TryLoadCalDavTaskCollectionUrl()
            If text3.Length > 0 AndAlso Not String.Equals(text3, calendarUrl.Trim(), StringComparison.OrdinalIgnoreCase) Then
                Dim list3 = CalDavSyncService.FetchEvents(text3, username, password)
                If list3 Is Nothing OrElse list3.Count = 0 Then
                    Dim text4 = CalDavSyncService.TryDiscoverVtodoCollectionUrl(calendarUrl, username, password)
                    If text4.Length > 0 AndAlso Not String.Equals(text4, text3, StringComparison.OrdinalIgnoreCase) Then
                        TrySaveCalDavTaskCollectionUrl(text4)
                        text3 = text4
                        list3 = CalDavSyncService.FetchEvents(text3, username, password)
                    End If
                End If
                If list3 IsNot Nothing AndAlso list3.Count > 0 Then
                    LastCalDavTaskCollectionPullCount = list3.Count
                    LastCalDavTaskEventsTagged = 0
                    Dim text5 As String = TryResolveCalendarDisplayLabel(text3.Trim())
                    If text5.Length = 0 Then
                        text5 = "VTODO"
                    End If
                    For Each item4 In list3
                        If item4 IsNot Nothing AndAlso String.IsNullOrWhiteSpace(item4.CalendarName) AndAlso text5.Length > 0 Then
                            item4.CalendarName = text5
                            LastCalDavTaskEventsTagged += 1
                        ElseIf item4 IsNot Nothing AndAlso String.IsNullOrWhiteSpace(item4.CalendarName) Then
                            item4.CalendarName = text5
                        End If
                    Next
                    Dim num3 = num
                    num += MergeCalDavPull(list3)
                    LastCalDavTaskPullMergeAdded = Math.Max(0, num - num3)
                End If
            End If
            LastCalDavPullMergeAdded = Math.Max(0, num)
            Return num
        End Function

        Private Shared Function TryLoadCalDavTaskCollectionUrl() As String
            Dim result As String
            Try
                If Not File.Exists(CalDavSettingsPath()) Then
                    result = ""
                Else
                    Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                    Dim calDavSettingsDto As CalDavSettingsDto = javaScriptSerializer.Deserialize(Of CalDavSettingsDto)(File.ReadAllText(CalDavSettingsPath(), Encoding.UTF8))
                    If calDavSettingsDto Is Nothing Then
                        result = ""
                    Else
                        Dim taskCollectionUrl = calDavSettingsDto.TaskCollectionUrl
                        result = If(Not Equals(taskCollectionUrl, Nothing), taskCollectionUrl, "").Trim()
                    End If
                End If
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                result = ""
                Call ProjectData.ClearProjectError()
            End Try
            Return result
        End Function

        Public Shared Function TrySyncStoredCalDavBidirectional() As Integer
            LastCalDavPrePushCount = 0
            LastCalDavPushCount = 0
            Call TryEnsureTaskCollectionUrlDiscovered()
            Dim calendarUrl As String = Nothing
            Dim username As String = Nothing
            Dim password As String = Nothing
            If TryLoadCalDavSettings(calendarUrl, username, password) Then
                LastCalDavPrePushCount = Math.Max(0, PushLocalEventsToCalDav(calendarUrl, username, password))
            End If
            Dim num As Integer = TrySyncStoredCalDav()
            If num < 0 Then
                Return num
            End If
            If Not TryLoadCalDavSettings(calendarUrl, username, password) Then
                Return Math.Max(num, LastCalDavPrePushCount)
            End If
            Dim val = PushLocalEventsToCalDav(calendarUrl, username, password)
            LastCalDavPushCount = Math.Max(LastCalDavPrePushCount, Math.Max(0, val))
            LastCalDavBidirectionalUtc = Date.UtcNow
            Return Math.Max(num, LastCalDavPushCount)
        End Function

        Public Shared Function TryPushTaskCompletion(ev As SharedCalendarEvent) As Boolean
            If ev Is Nothing OrElse String.IsNullOrWhiteSpace(ev.Uid) Then
                Return False
            End If
            Dim calendarUrl As String = Nothing
            Dim username As String = Nothing
            Dim password As String = Nothing
            If Not TryLoadCalDavSettings(calendarUrl, username, password) Then
                Return False
            End If
            Dim calDavEvent As CalDavEvent = New CalDavEvent()
            calDavEvent.Uid = ev.Uid
            calDavEvent.Summary = ev.Summary
            calDavEvent.StartUtc = ev.StartUtc
            calDavEvent.EndUtc = ev.EndUtc
            calDavEvent.TaskCompleted = ev.TaskCompleted
            Dim ev2 = calDavEvent
            Return CalDavSyncService.PutTask(calendarUrl, username, password, ev2)
        End Function

        Private Shared Function ResolveCalDavPushUrl(primaryUrl As String, ev As SharedCalendarEvent, taskCollectionUrl As String) As String
            Dim result As String = If(Not Equals(primaryUrl, Nothing), primaryUrl, "").Trim()
            If ev Is Nothing Then
                Return result
            End If
            Dim sharedCalendarEvent = ev
            Dim text = If(Not Equals(sharedCalendarEvent.Summary, Nothing), sharedCalendarEvent.Summary, "")
            If text.IndexOf("[TASK]", StringComparison.OrdinalIgnoreCase) >= 0 AndAlso taskCollectionUrl.Length > 0 Then
                Return taskCollectionUrl
            End If
            sharedCalendarEvent = ev
            Dim text2 As String = If(Not Equals(sharedCalendarEvent.CalendarName, Nothing), sharedCalendarEvent.CalendarName, "").Trim()
            If text2.Length = 0 Then
                Return result
            End If
            For Each item In TryLoadSecondaryCalendarUrls()
                If Not String.IsNullOrWhiteSpace(item) Then
                    Dim fileName As String = Path.GetFileName(item.Trim().TrimEnd("/"c))
                    If text2.Equals(fileName, StringComparison.OrdinalIgnoreCase) Then
                        Return item.Trim()
                    End If
                End If
            Next
            Return result
        End Function

        Public Shared Function PushLocalEventsToCalDav(calendarUrl As String, username As String, password As String) As Integer
            Dim num = 0
            Dim taskCollectionUrl As String = TryLoadCalDavTaskCollectionUrl()
            Dim list As List(Of SharedCalendarEvent) = LoadEvents()
            list.Sort(Function(a, b)
                          If a Is Nothing AndAlso b Is Nothing Then
                              Return 0
                          End If
                          If a Is Nothing Then
                              Return 1
                          End If
                          If b Is Nothing Then
                              Return -1
                          End If
                          If a.CalDavDirty = b.CalDavDirty Then
                              Dim sharedCalendarEvent10 = a
                              Dim strA = If(Not Equals(sharedCalendarEvent10.Uid, Nothing), sharedCalendarEvent10.Uid, "")
                              sharedCalendarEvent10 = b
                              Dim strB = If(Not Equals(sharedCalendarEvent10.Uid, Nothing), sharedCalendarEvent10.Uid, "")
                              Return String.Compare(strA, strB, StringComparison.OrdinalIgnoreCase)
                          End If
                          Return If(Not a.CalDavDirty, 1, -1)
                      End Function)
            For Each item In list
                If item IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(item.Uid) AndAlso item.CalDavDirty Then
                    Dim calDavEvent As CalDavEvent = New CalDavEvent()
                    calDavEvent.Uid = item.Uid
                    calDavEvent.Summary = item.Summary
                    calDavEvent.StartUtc = item.StartUtc
                    calDavEvent.EndUtc = item.EndUtc
                    calDavEvent.AlarmMinutesBefore = item.AlarmMinutesBefore
                    calDavEvent.TaskCompleted = item.TaskCompleted
                    Dim sharedCalendarEvent = item
                    calDavEvent.Location = If(Not Equals(sharedCalendarEvent.Location, Nothing), sharedCalendarEvent.Location, "")
                    Dim sharedCalendarEvent2 = item
                    calDavEvent.RecurrenceRule = If(Not Equals(sharedCalendarEvent2.RecurrenceRule, Nothing), sharedCalendarEvent2.RecurrenceRule, "")
                    Dim sharedCalendarEvent3 = item
                    calDavEvent.Category = If(Not Equals(sharedCalendarEvent3.Category, Nothing), sharedCalendarEvent3.Category, "")
                    Dim sharedCalendarEvent4 = item
                    calDavEvent.CalendarName = If(Not Equals(sharedCalendarEvent4.CalendarName, Nothing), sharedCalendarEvent4.CalendarName, "")
                    Dim sharedCalendarEvent5 = item
                    calDavEvent.Organizer = If(Not Equals(sharedCalendarEvent5.Organizer, Nothing), sharedCalendarEvent5.Organizer, "")
                    Dim sharedCalendarEvent6 = item
                    calDavEvent.AttendeeMailto = If(Not Equals(sharedCalendarEvent6.AttendeeMailto, Nothing), sharedCalendarEvent6.AttendeeMailto, "")
                    Dim sharedCalendarEvent7 = item
                    calDavEvent.MyPartStat = If(Not Equals(sharedCalendarEvent7.MyPartStat, Nothing), sharedCalendarEvent7.MyPartStat, "")
                    calDavEvent.IsMeetingInvitation = item.IsMeetingInvitation
                    calDavEvent.RecurrenceIdUtc = item.RecurrenceIdUtc
                    Dim sharedCalendarEvent8 = item
                    calDavEvent.RecurrenceParentUid = If(Not Equals(sharedCalendarEvent8.RecurrenceParentUid, Nothing), sharedCalendarEvent8.RecurrenceParentUid, "")
                    Dim sharedCalendarEvent9 = item
                    calDavEvent.ExDateUtcCsv = If(Not Equals(sharedCalendarEvent9.ExDateUtcCsv, Nothing), sharedCalendarEvent9.ExDateUtcCsv, "")
                    Dim ev = calDavEvent
                    sharedCalendarEvent9 = item
                    Dim text = If(Not Equals(sharedCalendarEvent9.Summary, Nothing), sharedCalendarEvent9.Summary, "")
                    Dim calendarUrl2 = ResolveCalDavPushUrl(calendarUrl, item, taskCollectionUrl)
                    Dim flag = False
                    If If(text.IndexOf("[TASK]", StringComparison.OrdinalIgnoreCase) < 0, CalDavSyncService.PutEvent(calendarUrl2, username, password, ev), CalDavSyncService.PutTask(calendarUrl2, username, password, ev)) Then
                        item.CalDavDirty = False
                        num = num + 1
                    End If
                End If
            Next
            If num > 0 Then
                SaveEvents(list)
            End If
            Return num
        End Function

        Public Shared Function TryApplyThunderbirdCalDavFromProfile(profileDir As String) As Integer
            Dim list = ThunderbirdCardDavImporter.TryLoadCalDavFromProfile(profileDir)
            If list Is Nothing OrElse list.Count = 0 Then
                Return 0
            End If
            Dim thunderbirdCardDavAccount = list(0)
            If thunderbirdCardDavAccount Is Nothing OrElse String.IsNullOrWhiteSpace(thunderbirdCardDavAccount.ServerUrl) Then
                Return 0
            End If
            Dim serverUrl = thunderbirdCardDavAccount.ServerUrl
            Dim thunderbirdCardDavAccount2 = thunderbirdCardDavAccount
            SaveCalDavSettings(serverUrl, If(Not Equals(thunderbirdCardDavAccount2.Username, Nothing), thunderbirdCardDavAccount2.Username, ""), Nothing)
            Return 1
        End Function

        Private Shared Function CalDavSettingsPath() As String
            Return Path.Combine(AccountStore.DataRoot(), "caldav-settings.json")
        End Function
    End Class

