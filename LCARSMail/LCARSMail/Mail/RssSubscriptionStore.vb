Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Text
Imports System.Web.Script.Serialization
Imports Microsoft.VisualBasic.CompilerServices
    Public NotInheritable Class RssSubscriptionStore
        Private Const FileName As String = "rss-feeds.json"

        Private Sub New()
        End Sub

        Public Shared Function LoadFeedUrls() As List(Of String)
            Dim path As String = StorePath()
            Dim result As List(Of String)
            If Not File.Exists(path) Then
                result = New List(Of String)()
            Else
                Try
                    Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                    Dim list = javaScriptSerializer.Deserialize(Of List(Of String))(File.ReadAllText(path, Encoding.UTF8))
                    result = (If(list IsNot Nothing, list, New List(Of String)()))
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    result = New List(Of String)()
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Public Shared Sub SaveFeedUrls(urls As IList(Of String))
            If urls Is Nothing Then
                Return
            End If
            Dim list As List(Of String) = New List(Of String)()
            Dim hashSet As HashSet(Of String) = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            For Each url In urls
                Dim text As String = If(Not Equals(url, Nothing), url, "").Trim()
                If text.Length <> 0 AndAlso hashSet.Add(text) Then
                    list.Add(text)
                End If
            Next
            Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
            Call File.WriteAllText(StorePath(), javaScriptSerializer.Serialize(list), Encoding.UTF8)
        End Sub

        Public Shared Sub Subscribe(feedUrl As String)
            Dim text As String = If(Not Equals(feedUrl, Nothing), feedUrl, "").Trim()
            If text.Length = 0 Then
                Return
            End If
            Dim list As List(Of String) = LoadFeedUrls()
            For Each item In list
                If String.Equals(item, text, StringComparison.OrdinalIgnoreCase) Then
                    Return
                End If
            Next
            list.Add(text)
            SaveFeedUrls(list)
        End Sub

        Private Shared Function StorePath() As String
            Return Path.Combine(AccountStore.DataRoot(), "rss-feeds.json")
        End Function
    End Class

