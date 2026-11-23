Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Text
Imports System.Web.Script.Serialization
Imports Microsoft.VisualBasic.CompilerServices
    Public NotInheritable Class NntpSubscriptionStore
        Private Const FileName As String = "nntp-subscriptions.json"

        Private Const HostFileName As String = "nntp-last-host.txt"

        Private Const LastArticleFileName As String = "nntp-last-article.json"

        Private Sub New()
        End Sub

        Public Shared Function LoadGroups() As List(Of String)
            Dim path As String = StorePath()
            Dim result As List(Of String)
            If Not File.Exists(path) Then
                result = New List(Of String)()
            Else
                Try
                    Dim input = File.ReadAllText(path, Encoding.UTF8)
                    Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                    Dim list = javaScriptSerializer.Deserialize(Of List(Of String))(input)
                    result = (If(list IsNot Nothing, list, New List(Of String)()))
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    result = New List(Of String)()
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Public Shared Sub SaveGroups(groups As IList(Of String))
            If groups Is Nothing Then
                Return
            End If
            Dim list As List(Of String) = New List(Of String)()
            Dim hashSet As HashSet(Of String) = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            For Each group In groups
                Dim text As String = If(Not Equals(group, Nothing), group, "").Trim()
                If text.Length <> 0 AndAlso hashSet.Add(text) Then
                    list.Add(text)
                End If
            Next
            list.Sort(StringComparer.OrdinalIgnoreCase)
            Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
            Call File.WriteAllText(StorePath(), javaScriptSerializer.Serialize(list), Encoding.UTF8)
        End Sub

        Public Shared Sub Subscribe(groupName As String)
            Dim text As String = If(Not Equals(groupName, Nothing), groupName, "").Trim()
            If text.Length = 0 Then
                Return
            End If
            Dim list As List(Of String) = LoadGroups()
            For Each item In list
                If String.Equals(item, text, StringComparison.OrdinalIgnoreCase) Then
                    Return
                End If
            Next
            list.Add(text)
            SaveGroups(list)
        End Sub

        Public Shared Sub Unsubscribe(groupName As String)
            Dim text As String = If(Not Equals(groupName, Nothing), groupName, "").Trim()
            If text.Length = 0 Then
                Return
            End If
            Dim list As List(Of String) = LoadGroups()
            Dim list2 As List(Of String) = New List(Of String)()
            For Each item In list
                If Not String.Equals(item, text, StringComparison.OrdinalIgnoreCase) Then
                    list2.Add(item)
                End If
            Next
            SaveGroups(list2)
        End Sub

        Public Shared Function LoadLastHost() As String
            Dim path As String = IO.Path.Combine(AccountStore.DataRoot(), "nntp-last-host.txt")
            Dim result As String
            If Not File.Exists(path) Then
                result = ""
            Else
                Try
                    result = File.ReadAllText(path, Encoding.UTF8).Trim()
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    result = ""
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Public Shared Sub SaveLastHost(host As String)
            Dim text As String = If(Not Equals(host, Nothing), host, "").Trim()
            If text.Length <> 0 Then
                Call File.WriteAllText(Path.Combine(AccountStore.DataRoot(), "nntp-last-host.txt"), text, Encoding.UTF8)
            End If
        End Sub

        Public Shared Function GetLastArticleNumber(groupName As String) As Integer
            Dim text As String = If(Not Equals(groupName, Nothing), groupName, "").Trim()
            If text.Length = 0 Then
                Return 0
            End If
            Dim dictionary As Dictionary(Of String, Integer) = LoadLastArticleMap()
            If dictionary.ContainsKey(text) Then
                Return dictionary(text)
            End If
            Return 0
        End Function

        Public Shared Sub SetLastArticleNumber(groupName As String, articleNumber As Integer)
            Dim text As String = If(Not Equals(groupName, Nothing), groupName, "").Trim()
            If text.Length <> 0 Then
                Dim dictionary As Dictionary(Of String, Integer) = LoadLastArticleMap()
                dictionary(text) = articleNumber
                SaveLastArticleMap(dictionary)
            End If
        End Sub

        Private Shared Function LoadLastArticleMap() As Dictionary(Of String, Integer)
            Dim path As String = IO.Path.Combine(AccountStore.DataRoot(), "nntp-last-article.json")
            Dim result As Dictionary(Of String, Integer)
            If Not File.Exists(path) Then
                result = New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
            Else
                Try
                    Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                    Dim dictionary = javaScriptSerializer.Deserialize(Of Dictionary(Of String, Integer))(File.ReadAllText(path, Encoding.UTF8))
                    result = (If(dictionary IsNot Nothing, dictionary, New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)))
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    result = New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Private Shared Sub SaveLastArticleMap(map As Dictionary(Of String, Integer))
            Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
            Call File.WriteAllText(Path.Combine(AccountStore.DataRoot(), "nntp-last-article.json"), javaScriptSerializer.Serialize(map), Encoding.UTF8)
        End Sub

        Private Shared Function StorePath() As String
            Return Path.Combine(AccountStore.DataRoot(), "nntp-subscriptions.json")
        End Function
    End Class

