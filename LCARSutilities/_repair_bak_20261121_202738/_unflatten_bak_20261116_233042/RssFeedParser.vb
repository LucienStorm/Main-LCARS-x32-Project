' LCARSutilities/LCARSutilities/Modules/RssFeedParser.vb
Option Strict On
Option Explicit On
Imports System.Collections.Generic
Imports System.Linq
Imports System.Xml.Linq
Public Class RssItem
Public Title As String = String.Empty
Public Link As String = String.Empty
Public Description As String = String.Empty
Public PublishedUtc As DateTime = DateTime.MinValue
End Class
Public NotInheritable Class RssFeedParser
Private Sub New()
End Sub
Public Shared Function Parse(ByVal xml As String) As List(Of RssItem)
Dim items As New List(Of RssItem)()
If String.IsNullOrWhiteSpace(xml) Then Return items
Try
Dim doc As XDocument = XDocument.Parse(xml)
Dim root As XElement = doc.Root
If root Is Nothing Then Return items
If root.Name.LocalName.Equals("rss", StringComparison.OrdinalIgnoreCase) Then For Each item As XElement In root.Descendants().Where(Function(x)
x.Name.LocalName = "item")
items.Add(ParseRssItem(item))
Next
ElseIf root.Name.LocalName.Equals("feed", StringComparison.OrdinalIgnoreCase) Then For Each entry As XElement In root.Elements().Where(Function(x)
x.Name.LocalName = "entry")
items.Add(ParseAtomEntry(entry))
Next
End If
Catch
End Try
Return items
End Function
Private Shared Function ParseRssItem(ByVal item As XElement) As RssItem
Dim rss As New RssItem()
rss.Title = ElementText(item, "title")
rss.Link = ElementText(item, "link")
rss.Description = ElementText(item, "description")
Dim dateText As String = ElementText(item, "pubDate")
Dim dt As DateTime
If DateTime.TryParse(dateText, dt) Then rss.PublishedUtc = dt.ToUniversalTime()
Return rss
End Function
Private Shared Function ParseAtomEntry(ByVal entry As XElement) As RssItem
Dim rss As New RssItem()
rss.Title = ElementText(entry, "title")
Dim linkEl As XElement = entry.Elements().FirstOrDefault(Function(x)
x.Name.LocalName = "link")
If linkEl IsNot Nothing Then Dim hrefAttr As XAttribute = linkEl.Attribute("href")
rss.Link = If(hrefAttr IsNot Nothing, hrefAttr.Value, "")
End If rss.Description = ElementText(entry, "summary")
If rss.Description.Length = 0 Then rss.Description = ElementText(entry, "content")
Dim dateText As String = ElementText(entry, "updated")
If dateText.Length = 0 Then dateText = ElementText(entry, "published")
Dim dt As DateTime
If DateTime.TryParse(dateText, dt) Then rss.PublishedUtc = dt.ToUniversalTime()
Return rss
End Function
Private Shared Function ElementText(ByVal parent As XElement, ByVal localName As String) As String
Dim el As XElement = parent.Elements().FirstOrDefault(Function(x)
x.Name.LocalName = localName)
If el Is Nothing Then Return ""
Return If(el.Value, "").Trim()
End Function
End Class
