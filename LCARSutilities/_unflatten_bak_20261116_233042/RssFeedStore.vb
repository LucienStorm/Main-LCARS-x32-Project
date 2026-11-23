' LCARSutilities/LCARSutilities/Modules/RssFeedStore.vb
Option Strict On
Option Explicit On
Imports System.Collections.Generic
Imports System.IO
Imports System.Net
Imports System.Text
Public Module RssFeedStore
Private Const FeedsFile As String = "feeds.txt"
Public Function DataRoot() As String
Dim dir As String = IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LCARS", "Rss")
If Not Directory.Exists(dir) Then Directory.CreateDirectory(dir)
Return dir
End Function
Public Function LoadFeedUrls() As List(Of String)
Dim path As String = IO.Path.Combine(DataRoot(), FeedsFile)
If Not File.Exists(path) Then Return New List(Of String)()
Dim lines As String() = File.ReadAllLines(path, Encoding.UTF8)
Dim result As New List(Of String)()
For Each line As String In lines
Dim url As String = If(line, "").Trim()
If url.Length > 0 Then result.Add(url)
Next
Return result
End Function
Public Sub SaveFeedUrls(ByVal urls As List(Of String))
Dim path As String = IO.Path.Combine(DataRoot(), FeedsFile)
File.WriteAllLines(path, urls.ToArray(), Encoding.UTF8)
End Sub
Public Function FetchFeedXml(ByVal url As String) As String
If String.IsNullOrWhiteSpace(url) Then Return ""
Using client As New WebClient()
client.Headers(HttpRequestHeader.UserAgent) = "LCARSRss/1.0"
Return client.DownloadString(url.Trim())
End Using
End Function
End Module
