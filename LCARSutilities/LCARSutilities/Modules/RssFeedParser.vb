Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Xml.Linq
Imports Microsoft.VisualBasic.CompilerServices

    Public NotInheritable Class RssFeedParser
        Private Sub New()
        End Sub

        Public Shared Function Parse(xml As String) As List(Of RssItem)
            Dim list As List(Of RssItem) = New List(Of RssItem)()
            If String.IsNullOrWhiteSpace(xml) Then
                Return list
            End If
            Try
                Dim xDocument = XDocument.Parse(xml)
                Dim root = xDocument.Root
                If root Is Nothing Then
                    Return list
                End If
                If root.Name.LocalName.Equals("rss", StringComparison.OrdinalIgnoreCase) Then
                    For Each item In From x In root.Descendants() Where Operators.CompareString(x.Name.LocalName, "item", TextCompare:=False) = 0 Select x
                        list.Add(ParseRssItem(item))
                    Next
                ElseIf root.Name.LocalName.Equals("feed", StringComparison.OrdinalIgnoreCase) Then
                    For Each item2 In From x In root.Elements() Where Operators.CompareString(x.Name.LocalName, "entry", TextCompare:=False) = 0 Select x
                        list.Add(ParseAtomEntry(item2))
                    Next
                End If
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
            Return list
        End Function

        Private Shared Function ParseRssItem(item As XElement) As RssItem
            Dim rssItem As RssItem = New RssItem()
            rssItem.Title = ElementText(item, "title")
            rssItem.Link = ElementText(item, "link")
            rssItem.Description = ElementText(item, "description")
            Dim s = ElementText(item, "pubDate")
            Dim result As Date = Nothing

            If Date.TryParse(s, result) Then
                rssItem.PublishedUtc = result.ToUniversalTime()
            End If
            Return rssItem
        End Function

        Private Shared Function ParseAtomEntry(entry As XElement) As RssItem
            Dim rssItem As RssItem = New RssItem()
            rssItem.Title = ElementText(entry, "title")
            Dim xElement As XElement = entry.Elements().FirstOrDefault(Function(x) Operators.CompareString(x.Name.LocalName, "link", TextCompare:=False) = 0)
            If xElement IsNot Nothing Then
                Dim xAttribute = xElement.Attribute("href")
                rssItem.Link = If(xAttribute IsNot Nothing, xAttribute.Value, "")
            End If
            rssItem.Description = ElementText(entry, "summary")
            If rssItem.Description.Length = 0 Then
                rssItem.Description = ElementText(entry, "content")
            End If
            Dim text = ElementText(entry, "updated")
            If text.Length = 0 Then
                text = ElementText(entry, "published")
            End If
            Dim result As Date = Nothing

            If Date.TryParse(text, result) Then
                rssItem.PublishedUtc = result.ToUniversalTime()
            End If
            Return rssItem
        End Function

        Private Shared Function ElementText(parent As XElement, localName As String) As String
            Dim xElement As XElement = parent.Elements().FirstOrDefault(Function(x) Operators.CompareString(x.Name.LocalName, localName, TextCompare:=False) = 0)
            If xElement Is Nothing Then
                Return ""
            End If
            Dim value = xElement.Value
            Return If(Not Equals(value, Nothing), value, "").Trim()
        End Function
    End Class
