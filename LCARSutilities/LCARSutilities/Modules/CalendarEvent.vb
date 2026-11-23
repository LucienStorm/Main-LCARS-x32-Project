Imports System

    Public Class CalendarEvent
        Public Uid As String

        Public Summary As String

        Public StartUtc As Date

        Public EndUtc As Date

        Public Location As String

        Public AlarmMinutesBefore As Integer

        Public TaskCompleted As Boolean

        Public TaskPriority As Integer

        Public TaskPercentComplete As Integer

        Public EwsItemId As String

        Public EwsChangeKey As String

        Public EwsDirty As Boolean

        Public EwsLastModifiedUtc As Date

        Public Category As String

        Public RecurrenceRule As String

        Public RecurrenceIdUtc As Date

        Public RecurrenceParentUid As String

        Public ExDateUtcCsv As String

        Public CalendarName As String

        Public CalDavDirty As Boolean

        Public CalDavEtag As String

        Public Organizer As String

        Public AttendeeMailto As String

        Public MyPartStat As String

        Public IsMeetingInvitation As Boolean

        Public Sub New()
            Uid = Guid.NewGuid().ToString()
            Summary = String.Empty
            StartUtc = Date.UtcNow
            EndUtc = Date.UtcNow.AddHours(1.0)
            Location = String.Empty
            TaskPriority = 5
            EwsItemId = String.Empty
            EwsChangeKey = String.Empty
            Category = String.Empty
            RecurrenceRule = String.Empty
            RecurrenceParentUid = String.Empty
            ExDateUtcCsv = String.Empty
            CalendarName = String.Empty
            CalDavEtag = String.Empty
            Organizer = String.Empty
            AttendeeMailto = String.Empty
            MyPartStat = String.Empty
        End Sub
    End Class
