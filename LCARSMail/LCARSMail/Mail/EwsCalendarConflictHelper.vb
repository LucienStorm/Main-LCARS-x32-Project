Imports System
Imports System.Collections.Generic
    Public NotInheritable Class EwsCalendarConflictHelper
        Private Sub New()
        End Sub

        Public Shared Function CountDirtyEwsCalendarEvents() As Integer
            Dim num = 0
            For Each item In LcarsCalendarBridge.LoadEvents()
                If item IsNot Nothing AndAlso item.EwsDirty AndAlso Not String.IsNullOrWhiteSpace(item.EwsItemId) Then
                    num = num + 1
                End If
            Next
            Return num
        End Function

        Public Shared Function TryRefreshLocalFromServer(ewsUrl As String, username As String, password As String, uid As String, Optional oauthToken As String = Nothing) As Boolean
            Dim text As String = If(Not Equals(uid, Nothing), uid, "").Trim()
            If text.Length = 0 Then
                Return False
            End If
            Dim list As List(Of LcarsCalendarBridge.SharedCalendarEvent) = LcarsCalendarBridge.LoadEvents()
            Dim sharedCalendarEvent As LcarsCalendarBridge.SharedCalendarEvent = Nothing
            For Each item In list
                If item IsNot Nothing AndAlso String.Equals(item.Uid, text, StringComparison.OrdinalIgnoreCase) Then
                    sharedCalendarEvent = item
                    Exit For
                End If
            Next
            If sharedCalendarEvent Is Nothing OrElse String.IsNullOrWhiteSpace(sharedCalendarEvent.EwsItemId) Then
                Return False
            End If
            Dim ewsCalendarEventDetails = EwsMailService.TryGetCalendarEventDetails(ewsUrl, username, password, sharedCalendarEvent.EwsItemId, oauthToken)
            If ewsCalendarEventDetails Is Nothing Then
                Return False
            End If
            Dim sharedCalendarEvent2 = sharedCalendarEvent
            Dim ewsCalendarEventDetails2 = ewsCalendarEventDetails
            sharedCalendarEvent2.Summary = If(Not Equals(ewsCalendarEventDetails2.Subject, Nothing), ewsCalendarEventDetails2.Subject, sharedCalendarEvent.Summary)
            Dim sharedCalendarEvent3 = sharedCalendarEvent
            ewsCalendarEventDetails2 = ewsCalendarEventDetails
            sharedCalendarEvent3.Location = If(Not Equals(ewsCalendarEventDetails2.Location, Nothing), ewsCalendarEventDetails2.Location, sharedCalendarEvent.Location)
            sharedCalendarEvent.StartUtc = ewsCalendarEventDetails.StartUtc
            sharedCalendarEvent.EndUtc = ewsCalendarEventDetails.EndUtc
            Dim sharedCalendarEvent4 = sharedCalendarEvent
            ewsCalendarEventDetails2 = ewsCalendarEventDetails
            sharedCalendarEvent4.Category = If(Not Equals(ewsCalendarEventDetails2.Category, Nothing), ewsCalendarEventDetails2.Category, sharedCalendarEvent.Category)
            sharedCalendarEvent.AlarmMinutesBefore = ewsCalendarEventDetails.AlarmMinutesBefore
            Dim sharedCalendarEvent5 = sharedCalendarEvent
            ewsCalendarEventDetails2 = ewsCalendarEventDetails
            sharedCalendarEvent5.EwsChangeKey = If(Not Equals(ewsCalendarEventDetails2.ChangeKey, Nothing), ewsCalendarEventDetails2.ChangeKey, sharedCalendarEvent.EwsChangeKey)
            If Date.Compare(ewsCalendarEventDetails.LastModifiedUtc, Date.MinValue) <> 0 Then
                sharedCalendarEvent.EwsLastModifiedUtc = ewsCalendarEventDetails.LastModifiedUtc
            End If
            sharedCalendarEvent.EwsDirty = False
            sharedCalendarEvent.CalDavDirty = True
            LcarsCalendarBridge.SaveEvents(list)
            Return True
        End Function

        Public Shared Function TryRefreshAllDirtyFromServer(ewsUrl As String, username As String, password As String, Optional oauthToken As String = Nothing) As Integer
            Dim num = 0
            For Each item In LcarsCalendarBridge.LoadEvents()
                If item IsNot Nothing AndAlso item.EwsDirty AndAlso Not String.IsNullOrWhiteSpace(item.Uid) AndAlso TryRefreshLocalFromServer(ewsUrl, username, password, item.Uid, oauthToken) Then
                    num = num + 1
                End If
            Next
            Return num
        End Function

        Public Shared Function TryAutoReconcileUnchangedDirty(ewsUrl As String, username As String, password As String, Optional oauthToken As String = Nothing) As Integer
            Dim num = 0
            Dim list As List(Of LcarsCalendarBridge.SharedCalendarEvent) = LcarsCalendarBridge.LoadEvents()
            If list Is Nothing Then
                Return 0
            End If
            For Each item In list
                If item Is Nothing OrElse Not item.EwsDirty OrElse String.IsNullOrWhiteSpace(item.EwsItemId) Then
                    Continue For
                End If
                Dim ewsCalendarEventDetails = EwsMailService.TryGetCalendarEventDetails(ewsUrl, username, password, item.EwsItemId, oauthToken)
                If ewsCalendarEventDetails IsNot Nothing AndAlso FieldsMatchServer(item, ewsCalendarEventDetails) Then
                    If ewsCalendarEventDetails.ChangeKey.Length > 0 Then
                        item.EwsChangeKey = ewsCalendarEventDetails.ChangeKey
                    End If
                    If Date.Compare(ewsCalendarEventDetails.LastModifiedUtc, Date.MinValue) <> 0 Then
                        item.EwsLastModifiedUtc = ewsCalendarEventDetails.LastModifiedUtc
                    End If
                    item.EwsDirty = False
                    num = num + 1
                End If
            Next
            If num > 0 Then
                LcarsCalendarBridge.SaveEvents(list)
            End If
            Return num
        End Function

        Public Shared Function TryRefreshChangeKeysForDirtyEvents(ewsUrl As String, username As String, password As String, Optional oauthToken As String = Nothing) As Integer
            Dim num = 0
            Dim list As List(Of LcarsCalendarBridge.SharedCalendarEvent) = LcarsCalendarBridge.LoadEvents()
            If list Is Nothing Then
                Return 0
            End If
            For Each item In list
                If item Is Nothing OrElse Not item.EwsDirty OrElse String.IsNullOrWhiteSpace(item.EwsItemId) Then
                    Continue For
                End If
                Dim ewsCalendarEventDetails = EwsMailService.TryGetCalendarEventDetails(ewsUrl, username, password, item.EwsItemId, oauthToken)
                If ewsCalendarEventDetails IsNot Nothing Then
                    If ewsCalendarEventDetails.ChangeKey.Length > 0 Then
                        item.EwsChangeKey = ewsCalendarEventDetails.ChangeKey
                    End If
                    If Date.Compare(ewsCalendarEventDetails.LastModifiedUtc, Date.MinValue) <> 0 Then
                        item.EwsLastModifiedUtc = ewsCalendarEventDetails.LastModifiedUtc
                    End If
                    num = num + 1
                End If
            Next
            If num > 0 Then
                LcarsCalendarBridge.SaveEvents(list)
            End If
            Return num
        End Function

        Public Shared Function TryReconcileSingleIfServerMatchesLocal(ev As LcarsCalendarBridge.SharedCalendarEvent, detail As EwsMailService.EwsCalendarEventDetails) As Boolean
            If ev Is Nothing OrElse detail Is Nothing Then
                Return False
            End If
            If Not FieldsMatchServer(ev, detail) Then
                Return False
            End If
            If detail.ChangeKey.Length > 0 Then
                ev.EwsChangeKey = detail.ChangeKey
            End If
            If Date.Compare(detail.LastModifiedUtc, Date.MinValue) <> 0 Then
                ev.EwsLastModifiedUtc = detail.LastModifiedUtc
            End If
            ev.EwsDirty = False
            Return True
        End Function

        Private Shared Function FieldsMatchServer(ev As LcarsCalendarBridge.SharedCalendarEvent, detail As EwsMailService.EwsCalendarEventDetails) As Boolean
            If ev Is Nothing OrElse detail Is Nothing Then
                Return False
            End If
            Dim sharedCalendarEvent = ev
            Dim a As String = If(Not Equals(sharedCalendarEvent.Summary, Nothing), sharedCalendarEvent.Summary, "").Trim()
            Dim ewsCalendarEventDetails = detail
            Dim b As String = If(Not Equals(ewsCalendarEventDetails.Subject, Nothing), ewsCalendarEventDetails.Subject, "").Trim()
            If Not String.Equals(a, b, StringComparison.OrdinalIgnoreCase) Then
                Return False
            End If
            sharedCalendarEvent = ev
            Dim a2 As String = If(Not Equals(sharedCalendarEvent.Location, Nothing), sharedCalendarEvent.Location, "").Trim()
            ewsCalendarEventDetails = detail
            Dim b2 As String = If(Not Equals(ewsCalendarEventDetails.Location, Nothing), ewsCalendarEventDetails.Location, "").Trim()
            If Not String.Equals(a2, b2, StringComparison.OrdinalIgnoreCase) Then
                Return False
            End If
            If Date.Compare(ev.StartUtc, Date.MinValue) <> 0 AndAlso Date.Compare(detail.StartUtc, Date.MinValue) <> 0 AndAlso Math.Abs((ev.StartUtc - detail.StartUtc).TotalMinutes) > 1.0 Then
                Return False
            End If
            If Date.Compare(ev.EndUtc, Date.MinValue) <> 0 AndAlso Date.Compare(detail.EndUtc, Date.MinValue) <> 0 AndAlso Math.Abs((ev.EndUtc - detail.EndUtc).TotalMinutes) > 1.0 Then
                Return False
            End If
            sharedCalendarEvent = ev
            Dim a3 As String = If(Not Equals(sharedCalendarEvent.Category, Nothing), sharedCalendarEvent.Category, "").Trim()
            ewsCalendarEventDetails = detail
            Dim b3 As String = If(Not Equals(ewsCalendarEventDetails.Category, Nothing), ewsCalendarEventDetails.Category, "").Trim()
            If Not String.Equals(a3, b3, StringComparison.OrdinalIgnoreCase) Then
                Return False
            End If
            If ev.AlarmMinutesBefore <> detail.AlarmMinutesBefore Then
                Return False
            End If
            sharedCalendarEvent = ev
            Dim flag = Not String.IsNullOrWhiteSpace(If(Not Equals(sharedCalendarEvent.RecurrenceRule, Nothing), sharedCalendarEvent.RecurrenceRule, ""))
            If flag <> detail.HasRecurrence Then
                Return False
            End If
            If flag AndAlso detail.HasRecurrence Then
                sharedCalendarEvent = ev
                Dim text = NormalizeRecurrenceToken(If(Not Equals(sharedCalendarEvent.RecurrenceRule, Nothing), sharedCalendarEvent.RecurrenceRule, ""))
                ewsCalendarEventDetails = detail
                Dim text2 = NormalizeRecurrenceToken(If(Not Equals(ewsCalendarEventDetails.RecurrenceSignature, Nothing), ewsCalendarEventDetails.RecurrenceSignature, ""))
                If text.Length > 0 AndAlso text2.Length > 0 AndAlso text2.IndexOf(text.Substring(0, Math.Min(24, text.Length)), StringComparison.OrdinalIgnoreCase) < 0 AndAlso text.IndexOf("FREQ", StringComparison.OrdinalIgnoreCase) >= 0 AndAlso text2.IndexOf("FREQ", StringComparison.OrdinalIgnoreCase) < 0 Then
                    Return False
                End If
            End If
            Return True
        End Function

        Private Shared Function NormalizeRecurrenceToken(raw As String) As String
            Dim text As String = If(Not Equals(raw, Nothing), raw, "").Trim().ToUpperInvariant()
            If text.StartsWith("RRULE:", StringComparison.Ordinal) Then
                text = text.Substring(6).Trim()
            End If
            Return text
        End Function
    End Class

