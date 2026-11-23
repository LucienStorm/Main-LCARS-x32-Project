Imports System
Imports System.Collections.Generic
Imports System.Globalization
Imports Microsoft.VisualBasic.CompilerServices
    Public NotInheritable Class MailFilterEngine
        Private Sub New()
        End Sub

        Public Shared Function ApplyRulesToFolder(account As MailAccount, password As String, folderFullName As String, headers As List(Of MailMessageHeader), mailService As ImapMailService, rules As List(Of MailFilterRule)) As MailFilterRunSummary
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
                    If header IsNot Nothing AndAlso RuleMatches(rule, header, account, password, folderFullName, mailService, mailFilterRunSummary) Then
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

        Public Shared Function RuleMatchesForHeader(rule As MailFilterRule, hdr As MailMessageHeader, account As MailAccount, password As String, folderFullName As String, bodyFetcher As Func(Of MailMessageHeader, String), summary As MailFilterRunSummary) As Boolean
            If rule.Conditions Is Nothing OrElse rule.Conditions.Count = 0 Then
                Return True
            End If
            Dim flag = String.Equals(rule.ConditionLogic, "OR", StringComparison.OrdinalIgnoreCase)
            Dim result = False
            Dim result2 = True
            For Each condition In rule.Conditions
                Dim flag2 = ConditionMatchesUnified(condition, hdr, account, password, folderFullName, bodyFetcher, summary)
                If flag2 Then
                    result = True
                End If
                If Not flag2 Then
                    result2 = False
                End If
            Next
            If flag Then
                Return result
            End If
            Return result2
        End Function

        Private Shared Function ConditionMatchesUnified(cond As MailFilterCondition, hdr As MailMessageHeader, account As MailAccount, password As String, folderFullName As String, bodyFetcher As Func(Of MailMessageHeader, String), summary As MailFilterRunSummary) As Boolean
            If cond Is Nothing Then
                Return True
            End If
            Dim mailFilterCondition = cond
            Dim text As String = If(Not Equals(mailFilterCondition.Field, Nothing), mailFilterCondition.Field, "").Trim().ToLowerInvariant()
            mailFilterCondition = cond
            Dim op As String = If(Not Equals(mailFilterCondition.Operator, Nothing), mailFilterCondition.Operator, "").Trim().ToLowerInvariant()
            mailFilterCondition = cond
            Dim needle As String = If(Not Equals(mailFilterCondition.Value, Nothing), mailFilterCondition.Value, "").Trim()
            Dim haystack = ""
            Select Case text
                Case "from", "sender"
                    Dim mailMessageHeader = hdr
                    haystack = If(Not Equals(mailMessageHeader.From, Nothing), mailMessageHeader.From, "")
                    Exit Select
                Case "to"
                    Dim mailMessageHeader = hdr
                    haystack = If(Not Equals(mailMessageHeader.ToAddresses, Nothing), mailMessageHeader.ToAddresses, "")
                    Exit Select
                Case "subject"
                    Dim mailMessageHeader = hdr
                    haystack = If(Not Equals(mailMessageHeader.Subject, Nothing), mailMessageHeader.Subject, "")
                    Exit Select
                Case "body"
                    If bodyFetcher IsNot Nothing Then
                        haystack = bodyFetcher(hdr)
                    End If
                Case "age"
                    Return MatchAge(op, needle, hdr.DateUtc)
                Case "size"
                    Return MatchSize(op, needle, hdr.SizeBytes)
                Case Else
                    Dim mailMessageHeader = hdr
                    haystack = If(Not Equals(mailMessageHeader.Subject, Nothing), mailMessageHeader.Subject, "") & " " & If(Not Equals(hdr.From, Nothing), hdr.From, "")
                    Exit Select
            End Select
            Return MatchText(op, haystack, needle)
        End Function

        Private Shared Function RuleMatches(rule As MailFilterRule, hdr As MailMessageHeader, account As MailAccount, password As String, folderFullName As String, mailService As ImapMailService, summary As MailFilterRunSummary) As Boolean
            If rule.Conditions Is Nothing OrElse rule.Conditions.Count = 0 Then
                Return True
            End If
            Dim flag = String.Equals(rule.ConditionLogic, "OR", StringComparison.OrdinalIgnoreCase)
            Dim result = False
            Dim result2 = True
            For Each condition In rule.Conditions
                Dim flag2 = ConditionMatches(condition, hdr, account, password, folderFullName, mailService, summary)
                If flag2 Then
                    result = True
                End If
                If Not flag2 Then
                    result2 = False
                End If
            Next
            If flag Then
                Return result
            End If
            Return result2
        End Function

        Private Shared Function ConditionMatches(cond As MailFilterCondition, hdr As MailMessageHeader, account As MailAccount, password As String, folderFullName As String, mailService As ImapMailService, summary As MailFilterRunSummary) As Boolean
            If cond Is Nothing Then
                Return True
            End If
            Dim mailFilterCondition = cond
            Dim text As String = If(Not Equals(mailFilterCondition.Field, Nothing), mailFilterCondition.Field, "").Trim().ToLowerInvariant()
            mailFilterCondition = cond
            Dim op As String = If(Not Equals(mailFilterCondition.Operator, Nothing), mailFilterCondition.Operator, "").Trim().ToLowerInvariant()
            mailFilterCondition = cond
            Dim needle As String = If(Not Equals(mailFilterCondition.Value, Nothing), mailFilterCondition.Value, "").Trim()
            Dim text2 = ""
            Select Case text
                Case "from", "sender"
                    Dim mailMessageHeader2 = hdr
                    text2 = If(Not Equals(mailMessageHeader2.From, Nothing), mailMessageHeader2.From, "")
                    Exit Select
                Case "to"
                    Dim mailMessageHeader2 = hdr
                    text2 = If(Not Equals(mailMessageHeader2.ToAddresses, Nothing), mailMessageHeader2.ToAddresses, "")
                    Exit Select
                Case "cc"
                    Dim mailMessageHeader2 = hdr
                    text2 = If(Not Equals(mailMessageHeader2.CcAddresses, Nothing), mailMessageHeader2.CcAddresses, "")
                    Exit Select
                Case "toorcc"
                    Dim mailMessageHeader2 = hdr
                    Dim obj2 = If(Not Equals(mailMessageHeader2.ToAddresses, Nothing), mailMessageHeader2.ToAddresses, "")
                    Dim mailMessageHeader = hdr
                    text2 = obj2 & " " & If(Not Equals(mailMessageHeader.CcAddresses, Nothing), mailMessageHeader.CcAddresses, "")
                    Exit Select
                Case "subject"
                    Dim mailMessageHeader = hdr
                    text2 = If(Not Equals(mailMessageHeader.Subject, Nothing), mailMessageHeader.Subject, "")
                    Exit Select
                Case "body"
                    text2 = mailService.FetchPlainBodyForFilter(account, password, folderFullName, hdr.Uid)
                Case "age"
                    Return MatchAge(op, needle, hdr.DateUtc)
                Case "size"
                    Return MatchSize(op, needle, hdr.SizeBytes)
                Case Else
                    Dim mailMessageHeader = hdr
                    Dim obj = If(Not Equals(mailMessageHeader.Subject, Nothing), mailMessageHeader.Subject, "")
                    Dim mailMessageHeader2 = hdr
                    text2 = obj & " " & If(Not Equals(mailMessageHeader2.From, Nothing), mailMessageHeader2.From, "")
                    Exit Select
            End Select
            Return MatchText(op, text2, needle)
        End Function

        Private Shared Function MatchText(op As String, haystack As String, needle As String) As Boolean
            Dim text = If(Not Equals(haystack, Nothing), haystack, "")
            Dim text2 = If(Not Equals(needle, Nothing), needle, "")
            Dim comparisonType = StringComparison.OrdinalIgnoreCase
            Select Case op
                Case "contains", "doesn't contain"
                    Dim flag2 = text.IndexOf(text2, comparisonType) >= 0
                    Return If(Operators.CompareString(op, "doesn't contain", TextCompare:=False) = 0, Not flag2, flag2)
                Case "is", "isn't"
                    Dim flag As Boolean = String.Equals(text.Trim(), text2.Trim(), comparisonType)
                    Return If(Operators.CompareString(op, "isn't", TextCompare:=False) = 0, Not flag, flag)
                Case "begins with"
                    Return text.StartsWith(text2, comparisonType)
                Case "ends with"
                    Return text.EndsWith(text2, comparisonType)
                Case Else
                    Return text.IndexOf(text2, comparisonType) >= 0
            End Select
        End Function

        Private Shared Function MatchAge(op As String, needle As String, dateUtc As Date) As Boolean
            If Date.Compare(dateUtc, Date.MinValue) = 0 Then
                Return False
            End If
            Dim result = 0
            Integer.TryParse(needle, NumberStyles.Integer, CultureInfo.InvariantCulture, result)
            If result < 0 Then
                result = 0
            End If
            Dim totalDays As Double = (Date.UtcNow - dateUtc.ToUniversalTime()).TotalDays
            If Operators.CompareString(op, "is greater than", TextCompare:=False) = 0 Then
                Return totalDays > result
            End If
            If Operators.CompareString(op, "is less than", TextCompare:=False) = 0 Then
                Return totalDays < result
            End If
            Return totalDays >= result
        End Function

        Private Shared Function MatchSize(op As String, needle As String, sizeBytes As Long) As Boolean
            Dim result = 0L
            Long.TryParse(needle, NumberStyles.Integer, CultureInfo.InvariantCulture, result)
            If Operators.CompareString(op, "is greater than", TextCompare:=False) = 0 Then
                Return sizeBytes > result
            End If
            If Operators.CompareString(op, "is less than", TextCompare:=False) = 0 Then
                Return sizeBytes < result
            End If
            Return sizeBytes >= result
        End Function

        Private Shared Function ExecuteActions(rule As MailFilterRule, account As MailAccount, password As String, folderFullName As String, hdr As MailMessageHeader, mailService As ImapMailService, summary As MailFilterRunSummary) As Boolean
            Dim flag = False
            If rule.Actions Is Nothing Then
                Return flag
            End If
            For Each action In rule.Actions
                If action Is Nothing Then
                    Continue For
                End If
                Dim mailFilterAction = action
                Dim text As String = If(Not Equals(mailFilterAction.ActionName, Nothing), mailFilterAction.ActionName, "").Trim()
                mailFilterAction = action
                Dim text2 As String = If(Not Equals(mailFilterAction.ActionValue, Nothing), mailFilterAction.ActionValue, "").Trim()
                Select Case text.ToLowerInvariant()
                    Case "move to folder"
                        Dim text4 = ResolveImapFolderPath(text2)
                        If text4.Length > 0 Then
                            mailService.MoveMessages(account, password, folderFullName, text4, New ULong(0) {hdr.Uid})
                            Dim mailFilterRunSummary = summary
                            mailFilterRunSummary.ActionsApplied += 1
                        End If
                        Exit Select
                    Case "copy to folder"
                        Dim text3 = ResolveImapFolderPath(text2)
                        If text3.Length > 0 Then
                            mailService.CopyMessages(account, password, folderFullName, text3, New ULong(0) {hdr.Uid})
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
                        mailService.DeleteMessages(account, password, folderFullName, New ULong(0) {hdr.Uid})
                        Dim mailFilterRunSummary = summary
                        mailFilterRunSummary.ActionsApplied += 1
                        Exit Select
                    Case "mark as junk", "junk"
                        mailService.MoveToJunk(account, password, folderFullName, New ULong(0) {hdr.Uid})
                        hdr.IsJunk = True
                        Dim mailFilterRunSummary = summary
                        mailFilterRunSummary.ActionsApplied += 1
                        Exit Select
                    Case "add tag", "tag"
                        If text2.Length > 0 Then
                            mailService.AddKeyword(account, password, folderFullName, hdr.Uid, text2)
                            Dim mailFilterRunSummary = summary
                            mailFilterRunSummary.ActionsApplied += 1
                        End If
                    Case "forward message to", "forward"
                        If text2.Length > 0 Then
                            ForwardMessage(account, password, folderFullName, hdr, mailService, text2, summary)
                        End If
                    Case "reply to sender", "reply"
                        ReplyToSender(account, password, folderFullName, hdr, mailService, text2, summary)
                    Case "stop execution"
                        flag = True
                    Case Else
                        AppendWarningOnce(summary, "Unsupported filter action: " & text)
                End Select
                If Not flag Then
                    Continue For
                End If
                Exit For
            Next
            Return flag
        End Function

        Private Shared Sub ForwardMessage(account As MailAccount, password As String, folderFullName As String, hdr As MailMessageHeader, mailService As ImapMailService, forwardTo As String, summary As MailFilterRunSummary)
            Dim bodyText = mailService.FetchPlainBodyForFilter(account, password, folderFullName, hdr.Uid)
            Dim subject = "Fwd: " & If(Not Equals(hdr.Subject, Nothing), hdr.Subject, "")
            Dim smtpMailService As SmtpMailService = New SmtpMailService()
            Dim text = smtpMailService.Send(account, password, forwardTo, "", subject, bodyText, Nothing)
            If String.IsNullOrEmpty(text) Then
                summary.ActionsApplied += 1
            Else
                AppendWarningOnce(summary, "Forward failed: " & text)
            End If
        End Sub

        Private Shared Sub ReplyToSender(account As MailAccount, password As String, folderFullName As String, hdr As MailMessageHeader, mailService As ImapMailService, template As String, summary As MailFilterRunSummary)
            Dim mailMessageHeader = hdr
            Dim text As String = If(Not Equals(mailMessageHeader.From, Nothing), mailMessageHeader.From, "").Trim()
            If text.Length <> 0 Then
                Dim text2 = If(Not Equals(template, Nothing), template, "")
                If text2.Length = 0 Then
                    text2 = "This is an automated reply."
                End If
                mailMessageHeader = hdr
                Dim subject = "Re: " & If(Not Equals(mailMessageHeader.Subject, Nothing), mailMessageHeader.Subject, "")
                Dim smtpMailService As SmtpMailService = New SmtpMailService()
                Dim text3 = smtpMailService.Send(account, password, text, "", subject, text2, Nothing)
                If String.IsNullOrEmpty(text3) Then
                    summary.ActionsApplied += 1
                Else
                    AppendWarningOnce(summary, "Reply failed: " & text3)
                End If
            End If
        End Sub

        Public Shared Function ResolveImapFolderPath(actionValue As String) As String
            Dim text As String = If(Not Equals(actionValue, Nothing), actionValue, "").Trim()
            If text.Length = 0 Then
                Return ""
            End If
            If text.StartsWith("imap://", StringComparison.OrdinalIgnoreCase) OrElse text.StartsWith("imaps://", StringComparison.OrdinalIgnoreCase) Then
                Dim num = text.IndexOf("//", StringComparison.Ordinal)
                If num < 0 Then
                    Return ""
                End If
                Dim num2 = text.IndexOf("/"c, num + 2)
                If num2 >= 0 AndAlso num2 + 1 < text.Length Then
                    Return Uri.UnescapeDataString(text.Substring(num2 + 1))
                End If
                Return ""
            End If
            Return text
        End Function

        Private Shared Sub AppendWarningOnce(summary As MailFilterRunSummary, text As String)
            If summary IsNot Nothing AndAlso Not String.IsNullOrEmpty(text) AndAlso Not summary.Warnings.Contains(text) Then
                summary.Warnings.Add(text)
            End If
        End Sub
    End Class

