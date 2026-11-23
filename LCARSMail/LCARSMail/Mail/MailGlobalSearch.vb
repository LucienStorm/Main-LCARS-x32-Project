Imports System
Imports System.Collections.Generic
    Public NotInheritable Class MailGlobalSearch
        Private Const MaxFolders As Integer = 0

        Private Const MaxPerFolder As Integer = 2000

        Private Sub New()
        End Sub

        Public Shared Function SearchAccount(account As MailAccount, password As String, mailService As UnifiedMailService, queryText As String) As List(Of MailMessageHeader)
            Dim dictionary As Dictionary(Of String, MailMessageHeader) = New Dictionary(Of String, MailMessageHeader)(StringComparer.OrdinalIgnoreCase)
            If account Is Nothing OrElse mailService Is Nothing Then
                Return New List(Of MailMessageHeader)()
            End If
            Dim text As String = If(Not Equals(queryText, Nothing), queryText, "").Trim()
            If text.Length = 0 Then
                Return New List(Of MailMessageHeader)()
            End If
            If Not String.IsNullOrWhiteSpace(account.Id) Then
                For Each item In MailSearchIndex.Search(account.Id, text)
                    If item IsNot Nothing Then
                        Dim mailMessageHeader = item
                        Dim key As String = If(Not Equals(mailMessageHeader.Folder, Nothing), mailMessageHeader.Folder, "") & "|" & item.Uid.ToString()
                        If Not dictionary.ContainsKey(key) Then
                            dictionary(key) = item
                        End If
                    End If
                Next
            End If
            Dim list = mailService.ListFolders(account, password)
            Dim num = 0
            For Each item2 In list
                If item2 Is Nothing Then
                    Continue For
                End If
                If False Then
                    If num >= 0 Then
                        Exit For
                    End If
                    num = num + 1
                End If
                Dim list2 = mailService.SearchFolderHeaders(account, password, item2.FullName, text, 2000)
                For Each item3 In list2
                    If item3 IsNot Nothing Then
                        Dim key2 As String = item2.FullName & "|" & item3.Uid.ToString()
                        If Not dictionary.ContainsKey(key2) Then
                            dictionary(key2) = item3
                        End If
                    End If
                Next
            Next
            Dim list3 As List(Of MailMessageHeader) = New List(Of MailMessageHeader)(dictionary.Values)
            list3.Sort(Function(a, b) Date.Compare(b.DateUtc, a.DateUtc))
            Return list3
        End Function
    End Class

