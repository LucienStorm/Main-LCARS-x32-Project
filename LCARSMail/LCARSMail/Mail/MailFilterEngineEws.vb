Imports System
Imports System.Collections.Generic
    Public NotInheritable Class MailFilterEngineEws
        Private Sub New()
        End Sub

        Public Shared Function ApplyRulesToFolder(account As MailAccount, password As String, folderFullName As String, headers As List(Of MailMessageHeader), mailService As UnifiedMailService, rules As List(Of MailFilterRule)) As MailFilterRunSummary
            Dim mailFilterRunSummary As MailFilterRunSummary = New MailFilterRunSummary()
            If account Is Nothing OrElse mailService Is Nothing OrElse headers Is Nothing OrElse rules Is Nothing Then
                Return mailFilterRunSummary
            End If
            If headers.Count = 0 OrElse rules.Count = 0 Then
                Return mailFilterRunSummary
            End If
            For Each rule In rules
                If rule Is Nothing OrElse Not rule.Enabled OrElse Not rule.RunOnIncoming Then
                    Continue For
                End If
                Dim mailFilterRunSummary2 = mailFilterRunSummary
                mailFilterRunSummary2.RulesEvaluated += 1
                For Each header In headers
                    If header Is Nothing Then
                        Continue For
                    End If
                    Dim bodyFetcher As Func(Of MailMessageHeader, String) = Function(h) mailService.FetchPlainBodyForFilter(account, password, folderFullName, h.Uid)
                    If MailFilterEngine.RuleMatchesForHeader(rule, header, account, password, folderFullName, bodyFetcher, mailFilterRunSummary) Then
                        mailFilterRunSummary2 = mailFilterRunSummary
                        mailFilterRunSummary2.MessagesMatched += 1
                        If ExecuteActions(rule, account, password, folderFullName, header, mailService, mailFilterRunSummary) Then
                            Exit For
                        End If
                    End If
                Next
            Next
            Return mailFilterRunSummary
        End Function

        Private Shared Function ExecuteActions(rule As MailFilterRule, account As MailAccount, password As String, folderFullName As String, hdr As MailMessageHeader, mailService As UnifiedMailService, summary As MailFilterRunSummary) As Boolean
            Dim flag = False
            If rule.Actions Is Nothing Then
                Return flag
            End If
            Dim list As List(Of ULong) = New List(Of ULong)()
            list.Add(hdr.Uid)
            Dim uids = list
            For Each action In rule.Actions
                If action Is Nothing Then
                    Continue For
                End If
                Dim mailFilterAction = action
                Dim text As String = If(Not Equals(mailFilterAction.ActionName, Nothing), mailFilterAction.ActionName, "").Trim().ToLowerInvariant()
                mailFilterAction = action
                Dim text2 As String = If(Not Equals(mailFilterAction.ActionValue, Nothing), mailFilterAction.ActionValue, "").Trim()
                Select Case text
                    Case "move to folder"
                        Dim text4 = EwsMailBridge.ResolveFilterFolder(text2)
                        If text4.Length > 0 Then
                            mailService.MoveMessages(account, password, folderFullName, text4, uids)
                            Dim mailFilterRunSummary = summary
                            mailFilterRunSummary.ActionsApplied += 1
                        End If
                        Exit Select
                    Case "mark read", "mark as read"
                        mailService.MarkSeen(account, password, folderFullName, hdr.Uid)
                        hdr.IsSeen = True
                        Dim mailFilterRunSummary = summary
                        mailFilterRunSummary.ActionsApplied += 1
                        Exit Select
                    Case "mark unread", "mark as unread"
                        mailService.MarkUnseen(account, password, folderFullName, hdr.Uid)
                        hdr.IsSeen = False
                        Dim mailFilterRunSummary = summary
                        mailFilterRunSummary.ActionsApplied += 1
                        Exit Select
                    Case "delete", "delete from pop server", "delete from server"
                        mailService.DeleteMessages(account, password, folderFullName, uids)
                        Dim mailFilterRunSummary = summary
                        mailFilterRunSummary.ActionsApplied += 1
                        Exit Select
                    Case "mark as junk", "junk"
                        mailService.MoveToJunk(account, password, folderFullName, uids)
                        hdr.IsJunk = True
                        Dim mailFilterRunSummary = summary
                        mailFilterRunSummary.ActionsApplied += 1
                        Exit Select
                    Case "copy to folder"
                        Dim text3 = EwsMailBridge.ResolveFilterFolder(text2)
                        If text3.Length > 0 Then
                            mailService.CopyMessages(account, password, folderFullName, text3, uids)
                            Dim mailFilterRunSummary = summary
                            mailFilterRunSummary.ActionsApplied += 1
                        End If
                        Exit Select
                    Case "add tag", "tag"
                        If text2.Length > 0 Then
                            mailService.AddKeyword(account, password, folderFullName, hdr.Uid, text2)
                            Dim mailFilterRunSummary = summary
                            mailFilterRunSummary.ActionsApplied += 1
                        End If
                    Case "forward message to", "forward"
                        If text2.Length > 0 Then
                            ForwardMessageEws(account, password, folderFullName, hdr, mailService, text2, summary)
                        End If
                    Case "reply to sender", "reply"
                        ReplyToSenderEws(account, password, folderFullName, hdr, mailService, text2, summary)
                    Case "stop execution"
                        flag = True
                    Case Else
                        If Not summary.Warnings.Contains("EWS filter action not supported: " & text) Then
                            summary.Warnings.Add("EWS filter action not supported: " & text)
                        End If
                End Select
                If Not flag Then
                    Continue For
                End If
                Exit For
            Next
            Return flag
        End Function

        Private Shared Sub ForwardMessageEws(account As MailAccount, password As String, folderFullName As String, hdr As MailMessageHeader, mailService As UnifiedMailService, forwardTo As String, summary As MailFilterRunSummary)
            Dim bodyText = mailService.FetchPlainBodyForFilter(account, password, folderFullName, hdr.Uid)
            Dim subject = "Fwd: " & If(Not Equals(hdr.Subject, Nothing), hdr.Subject, "")
            Dim text = EwsMailBridge.TrySendFilterMail(account, password, forwardTo, subject, bodyText)
            If String.IsNullOrEmpty(text) Then
                summary.ActionsApplied += 1
            ElseIf Not summary.Warnings.Contains(text) Then
                summary.Warnings.Add(text)
            End If
        End Sub

        Private Shared Sub ReplyToSenderEws(account As MailAccount, password As String, folderFullName As String, hdr As MailMessageHeader, mailService As UnifiedMailService, template As String, summary As MailFilterRunSummary)
            Dim mailMessageHeader = hdr
            Dim text As String = If(Not Equals(mailMessageHeader.From, Nothing), mailMessageHeader.From, "").Trim()
            If text.Length <> 0 Then
                Dim text2 = If(Not Equals(template, Nothing), template, "")
                If text2.Length = 0 Then
                    text2 = "This is an automated reply."
                End If
                mailMessageHeader = hdr
                Dim subject = "Re: " & If(Not Equals(mailMessageHeader.Subject, Nothing), mailMessageHeader.Subject, "")
                Dim text3 = EwsMailBridge.TrySendFilterMail(account, password, text, subject, text2)
                If String.IsNullOrEmpty(text3) Then
                    summary.ActionsApplied += 1
                ElseIf Not summary.Warnings.Contains(text3) Then
                    summary.Warnings.Add(text3)
                End If
            End If
        End Sub
    End Class

