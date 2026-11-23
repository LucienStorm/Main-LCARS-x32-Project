Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Net
Imports System.Text
Imports Microsoft.VisualBasic.CompilerServices

    <StandardModule>
    Public NotInheritable Class RssFeedStore
        Private Const FeedsFile As String = "feeds.txt"

        Public Shared Function DataRoot() As String
            Dim text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LCARS", "Rss")
            If Not Directory.Exists(text) Then
                Directory.CreateDirectory(text)
            End If
            Return text
        End Function

        Public Shared Function LoadFeedUrls() As List(Of String)
            Dim path As String = IO.Path.Combine(DataRoot(), "feeds.txt")
            If Not File.Exists(path) Then
                Return New List(Of String)()
            End If
            Dim array = File.ReadAllLines(path, Encoding.UTF8)
            Dim list As List(Of String) = New List(Of String)()
            Dim array2 = array
            For Each text In array2
                Dim text2 As String = If(Not Equals(text, Nothing), text, "").Trim()
                If text2.Length > 0 Then
                    list.Add(text2)
                End If
            Next
            Return list
        End Function

        Public Shared Sub SaveFeedUrls(urls As List(Of String))
            Dim path As String = IO.Path.Combine(DataRoot(), "feeds.txt")
            Call File.WriteAllLines(path, urls.ToArray(), Encoding.UTF8)
        End Sub

        Public Shared Function FetchFeedXml(url As String) As String
            If String.IsNullOrWhiteSpace(url) Then
                Return ""
            End If
            Dim webClient As WebClient = New WebClient()
            webClient.Headers(HttpRequestHeader.UserAgent) = "LCARSRss/1.0"
            Return webClient.DownloadString(url.Trim())
        End Function
    End Class
