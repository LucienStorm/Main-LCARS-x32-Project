Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Net
Imports System.Reflection
Imports System.Text
Imports System.Xml
Imports Microsoft.VisualBasic.CompilerServices
    Public NotInheritable Class RssFeedService
        Public Shared LastError As String

        Private Sub New()
        End Sub

        Public Shared Function FetchItems(feedUrl As String) As List(Of RssFeedItem)
            LastError = Nothing
            Dim text As String = If(Not Equals(feedUrl, Nothing), feedUrl, "").Trim()
            Dim result As List(Of RssFeedItem)
            If text.Length = 0 Then
                LastError = "Feed URL is required."
                result = Nothing
            Else
                Try
                    Dim httpWebRequest = CType(WebRequest.Create(text), HttpWebRequest)
                    httpWebRequest.Timeout = 30000
                    httpWebRequest.UserAgent = "LCARSMail/1.0"
                    Dim httpWebResponse As HttpWebResponse = CType(httpWebRequest.GetResponse(), HttpWebResponse)
                    Dim streamReader As StreamReader = New StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8)
                    result = ParseFeedXml(streamReader.ReadToEnd())
                Catch ex As Exception
                    ProjectData.SetProjectError(ex)
                    Dim ex2 = ex
                    LastError = ex2.Message
                    result = Nothing
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Public Shared Sub PlayEnclosure(item As RssFeedItem)
            If item Is Nothing OrElse String.IsNullOrWhiteSpace(item.EnclosureUrl) Then
                Return
            End If
            LastError = Nothing
            Try
                Dim text = DownloadEnclosureToTemp(item.EnclosureUrl)
                If Not Equals(text, Nothing) Then
                    Dim text2 As String = Path.Combine(ResolveStartupPath(), "LCARSmedia.exe")
                    If File.Exists(text2) Then
                        Dim processStartInfo As ProcessStartInfo = New ProcessStartInfo()
                        processStartInfo.FileName = text2
                        processStartInfo.Arguments = """" & text & """"
                        processStartInfo.WorkingDirectory = Path.GetDirectoryName(text2)
                        processStartInfo.UseShellExecute = False
                        Dim startInfo = processStartInfo
                        Process.Start(startInfo)
                    Else
                        Dim processStartInfo As ProcessStartInfo = New ProcessStartInfo()
                        processStartInfo.FileName = text
                        processStartInfo.UseShellExecute = True
                        Process.Start(processStartInfo)
                    End If
                End If
            Catch ex As Exception
                ProjectData.SetProjectError(ex)
                Dim ex2 = ex
                LastError = ex2.Message
                Call ProjectData.ClearProjectError()
            End Try
        End Sub

        Private Shared Function ResolveStartupPath() As String
            Dim directoryName As String = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            If Not String.IsNullOrWhiteSpace(directoryName) Then
                Return directoryName
            End If
            Return AppDomain.CurrentDomain.BaseDirectory
        End Function

        Private Shared Function DownloadEnclosureToTemp(url As String) As String
            Dim text As String = url.Trim()
            If text.Length = 0 Then
                Return Nothing
            End If
            Dim text2 = ".media"
            Try
                Dim uri As Uri = New Uri(text)
                Dim extension = Path.GetExtension(uri.AbsolutePath)
                If extension.Length > 1 Then
                    text2 = extension
                End If
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
            Dim text3 As String = Path.Combine(Path.GetTempPath(), "lcars-rss-" & Guid.NewGuid().ToString("N") & text2)
            Dim httpWebRequest = CType(WebRequest.Create(text), HttpWebRequest)
            httpWebRequest.Timeout = 120000
            httpWebRequest.UserAgent = "LCARSMail/1.0"
            Using httpWebResponse As HttpWebResponse = CType(httpWebRequest.GetResponse(), HttpWebResponse)
                Dim stream As Stream = httpWebResponse.GetResponseStream()
                Dim destination As FileStream = New FileStream(text3, FileMode.Create, FileAccess.Write)
                stream.CopyTo(destination)
            End Using
            Return text3
        End Function

        Private Shared Function ParseFeedXml(xml As String) As List(Of RssFeedItem)
            Dim list As List(Of RssFeedItem) = New List(Of RssFeedItem)()
            If String.IsNullOrWhiteSpace(xml) Then
                Return list
            End If
            Dim xmlDocument As XmlDocument = New XmlDocument()
            xmlDocument.LoadXml(xml)
            Dim elementsByTagName = xmlDocument.GetElementsByTagName("item")
            If elementsByTagName Is Nothing OrElse elementsByTagName.Count = 0 Then
                elementsByTagName = xmlDocument.GetElementsByTagName("entry")
            End If
            If elementsByTagName Is Nothing Then
                Return list
            End If
            For Each item As XmlNode In elementsByTagName
                If item Is Nothing Then
                    Continue For
                End If
                Dim rssFeedItem As RssFeedItem = New RssFeedItem()
                rssFeedItem.Title = ChildText(item, "title")
                rssFeedItem.Link = ChildLink(item)
                rssFeedItem.Description = ChildText(item, "description")
                If rssFeedItem.Description.Length = 0 Then
                    rssFeedItem.Description = ChildText(item, "summary")
                End If
                Dim xmlNode2 = item.SelectSingleNode("enclosure")
                If xmlNode2 IsNot Nothing AndAlso xmlNode2.Attributes IsNot Nothing Then
                    Dim xmlAttribute = xmlNode2.Attributes("url")
                    Dim xmlAttribute2 = xmlNode2.Attributes("type")
                    If xmlAttribute IsNot Nothing Then
                        rssFeedItem.EnclosureUrl = xmlAttribute.Value
                    End If
                    If xmlAttribute2 IsNot Nothing Then
                        rssFeedItem.EnclosureType = xmlAttribute2.Value
                    End If
                End If
                list.Add(rssFeedItem)
            Next
            Return list
        End Function

        Private Shared Function ChildText(parent As XmlNode, name As String) As String
            Dim xmlNode = parent.SelectSingleNode(name)
            If xmlNode Is Nothing Then
                Return ""
            End If
            Dim innerText = xmlNode.InnerText
            Return If(Not Equals(innerText, Nothing), innerText, "").Trim()
        End Function

        Private Shared Function ChildLink(parent As XmlNode) As String
            Dim xmlNode = parent.SelectSingleNode("link")
            If xmlNode Is Nothing Then
                Return ""
            End If
            If xmlNode.Attributes IsNot Nothing AndAlso xmlNode.Attributes("href") IsNot Nothing Then
                Return xmlNode.Attributes("href").Value
            End If
            Dim innerText = xmlNode.InnerText
            Return If(Not Equals(innerText, Nothing), innerText, "").Trim()
        End Function
    End Class

