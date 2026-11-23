Imports System
Imports System.Collections.Generic
Imports System.Text.RegularExpressions
    Public NotInheritable Class MailConversationGrouper
        Private Shared ReadOnly SubjectPrefixPattern As Regex = New Regex("^\s*(re|fwd|fw|aw|sv|antw|vs|enc|回复|答复)\s*(\[\d+\])?\s*:\s*", RegexOptions.IgnoreCase Or RegexOptions.Compiled Or RegexOptions.CultureInvariant)

        Private Shared ReadOnly MessageIdTokenPattern As Regex = New Regex("<[^>]+>", RegexOptions.Compiled)

        Private Sub New()
        End Sub

        Public Shared Function BuildThreadList(headers As List(Of MailMessageHeader)) As List(Of MailThreadListItem)
            Dim list As List(Of MailThreadListItem) = New List(Of MailThreadListItem)()
            If headers Is Nothing OrElse headers.Count = 0 Then
                Return list
            End If
            Dim parent As Dictionary(Of String, String) = New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            For Each header In headers
                If header Is Nothing Then
                    Continue For
                End If
                Dim list2 = CollectMessageIds(header)
                If list2.Count <> 0 Then
                    Dim a = list2(0)
                    Dim num = list2.Count - 1
                    For i = 1 To num
                        UnionIds(parent, a, list2(i))
                    Next
                End If
            Next
            Dim dictionary As Dictionary(Of String, List(Of MailMessageHeader)) = New Dictionary(Of String, List(Of MailMessageHeader))(StringComparer.OrdinalIgnoreCase)
            For Each header2 In headers
                If header2 IsNot Nothing Then
                    Dim key = ThreadKey(header2, parent)
                    If Not dictionary.ContainsKey(key) Then
                        dictionary(key) = New List(Of MailMessageHeader)()
                    End If
                    dictionary(key).Add(header2)
                End If
            Next
            For Each item In dictionary
                Dim value = item.Value
                value.Sort(Function(mailMessageHeader, b) Date.Compare(b.DateUtc, mailMessageHeader.DateUtc))
                Dim representative = value(0)
                list.Add(New MailThreadListItem With {
    .Representative = representative,
    .Messages = value,
    .ThreadKey = item.Key
})
            Next
            list.Sort(Function(mailThreadListItem, b) Date.Compare(b.Representative.DateUtc, mailThreadListItem.Representative.DateUtc))
            Return list
        End Function

        Public Shared Function ThreadKey(hdr As MailMessageHeader) As String
            Return ThreadKey(hdr, Nothing)
        End Function

        Private Shared Function ThreadKey(hdr As MailMessageHeader, parent As Dictionary(Of String, String)) As String
            If hdr Is Nothing Then
                Return ""
            End If
            Dim list = CollectMessageIds(hdr)
            If list.Count > 0 AndAlso parent IsNot Nothing Then
                Return "id:" & FindRoot(parent, list(0)).ToLowerInvariant()
            End If
            If list.Count > 0 Then
                Return "id:" & list(0).ToLowerInvariant()
            End If
            Dim mailMessageHeader = hdr
            Dim text = NormalizeSubject(If(Not Equals(mailMessageHeader.Subject, Nothing), mailMessageHeader.Subject, ""))
            mailMessageHeader = hdr
            Dim text2 As String = If(Not Equals(mailMessageHeader.From, Nothing), mailMessageHeader.From, "").Trim().ToLowerInvariant()
            Return "sub:" & text & "|" & text2
        End Function

        Private Shared Function CollectMessageIds(hdr As MailMessageHeader) As List(Of String)
            Dim list As List(Of String) = New List(Of String)()
            If hdr Is Nothing Then
                Return list
            End If
            AppendId(list, hdr.MessageId)
            AppendId(list, hdr.InReplyTo)
            Dim enumerator As List(Of String).Enumerator = Nothing
            Try
                enumerator = ParseReferences(If(Not Equals(hdr.References, Nothing), hdr.References, "")).GetEnumerator()
                While enumerator.MoveNext()
                    Dim current = enumerator.Current
                    AppendId(list, current)
                End While

            Finally
                CType(enumerator, IDisposable).Dispose()
            End Try
            Return list
        End Function

        Private Shared Sub AppendId(ids As List(Of String), raw As String)
            Dim text = NormalizeMessageId(raw)
            If text.Length = 0 Then
                Return
            End If
            For Each id In ids
                If String.Equals(id, text, StringComparison.OrdinalIgnoreCase) Then
                    Return
                End If
            Next
            ids.Add(text)
        End Sub

        Private Shared Function ParseReferences(referencesHeader As String) As List(Of String)
            Dim list As List(Of String) = New List(Of String)()
            Dim text As String = If(Not Equals(referencesHeader, Nothing), referencesHeader, "").Trim()
            If text.Length = 0 Then
                Return list
            End If
            For Each item As Match In MessageIdTokenPattern.Matches(text)
                Dim text2 = NormalizeMessageId(item.Value)
                If text2.Length > 0 Then
                    list.Add(text2)
                End If
            Next
            Return list
        End Function

        Private Shared Function NormalizeMessageId(raw As String) As String
            Dim text As String = If(Not Equals(raw, Nothing), raw, "").Trim()
            If text.Length = 0 Then
                Return ""
            End If
            If text.StartsWith("<") AndAlso text.EndsWith(">") AndAlso text.Length > 2 Then
                text = text.Substring(1, text.Length - 2).Trim()
            End If
            Return text
        End Function

        Private Shared Sub UnionIds(parent As Dictionary(Of String, String), a As String, b As String)
            Dim text = NormalizeMessageId(a)
            Dim text2 = NormalizeMessageId(b)
            If text.Length <> 0 AndAlso text2.Length <> 0 Then
                Dim text3 = FindRoot(parent, text)
                Dim text4 = FindRoot(parent, text2)
                If Not String.Equals(text3, text4, StringComparison.OrdinalIgnoreCase) Then
                    parent(text4) = text3
                End If
            End If
        End Sub

        Private Shared Function FindRoot(parent As Dictionary(Of String, String), id As String) As String
            Dim text = NormalizeMessageId(id)
            If text.Length = 0 Then
                Return ""
            End If
            If parent Is Nothing Then
                Return text
            End If
            Dim hashSet As HashSet(Of String) = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            While parent.ContainsKey(text) AndAlso hashSet.Add(text)
                text = parent(text)
            End While
            Dim text2 = NormalizeMessageId(id)
            While parent.ContainsKey(text2) AndAlso Not String.Equals(text2, text, StringComparison.OrdinalIgnoreCase)
                Dim text3 = parent(text2)
                parent(text2) = text
                text2 = text3
            End While
            Return text
        End Function

        Public Shared Function NormalizeSubject(subject As String) As String
            Dim text As String = If(Not Equals(subject, Nothing), subject, "").Trim()
            Dim a As String
            Do
                a = text
                text = SubjectPrefixPattern.Replace(text, "")
                text = text.Trim()
            Loop While Not String.Equals(a, text, StringComparison.Ordinal)
            Return text.ToLowerInvariant()
        End Function
    End Class

