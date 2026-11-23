Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Runtime.CompilerServices
Imports System.Windows.Forms
Imports LCARS
Imports Microsoft.VisualBasic
Imports Microsoft.VisualBasic.CompilerServices

    Public Class frmRss
        Inherits LCARSForm
        Private Const FormW As Integer = 820

        Private Const FormH As Integer = 580

        Private lstFeeds As ListBox

        Private lstItems As ListBox

        Private txtArticle As TextBox

        Private lblStatus As Label

        Private feedUrls As List(Of String)

        Private currentItems As List(Of RssItem)

        Public Sub New()
            modUtilities.InitUtilityFormFloating(Me, 820, 580)
            feedUrls = RssFeedStore.LoadFeedUrls()
            currentItems = New List(Of RssItem)()
            BuildUi()
            RefreshFeedList()
        End Sub

        Private Sub BuildUi()
            modUtilities.MakeUtilityTitle(Me, "RSS", 400)
            modUtilities.MakeUtilityButton(Me, "ADD FEED", 398, 12, 110, 28, LCARScolorStyles.PrimaryFunction, New EventHandler(AddressOf OnAddFeed))
            modUtilities.MakeUtilityButton(Me, "REFRESH", 518, 12, 100, 28, LCARScolorStyles.SystemFunction, New EventHandler(AddressOf OnRefreshFeed))
            modUtilities.MakeUtilityButton(Me, "CLOSE", 688, 12, 110, 28, LCARScolorStyles.Orange, Sub(s, e) Close())
            lstFeeds = New ListBox()
            Dim listBox = lstFeeds
            Dim location As Point = New Point(12, 48)
            listBox.Location = location
            Dim listBox2 = lstFeeds
            Dim size As Size = New Size(240, 480)
            listBox2.Size = size
            lstFeeds.BackColor = Color.FromArgb(30, 30, 30)
            lstFeeds.ForeColor = Color.Orange
            AddHandler lstFeeds.SelectedIndexChanged, AddressOf OnFeedSelected
            Controls.Add(lstFeeds)
            lstItems = New ListBox()
            Dim listBox3 = lstItems
            location = New Point(260, 48)
            listBox3.Location = location
            Dim listBox4 = lstItems
            size = New Size(260, 480)
            listBox4.Size = size
            lstItems.BackColor = Color.FromArgb(30, 30, 30)
            lstItems.ForeColor = Color.Orange
            AddHandler lstItems.SelectedIndexChanged, AddressOf OnItemSelected
            Controls.Add(lstItems)
            txtArticle = New TextBox()
            txtArticle.Multiline = True
            txtArticle.ReadOnly = True
            txtArticle.ScrollBars = ScrollBars.Vertical
            Dim textBox = txtArticle
            location = New Point(530, 48)
            textBox.Location = location
            Dim textBox2 = txtArticle
            size = New Size(278, 480)
            textBox2.Size = size
            txtArticle.BackColor = Color.FromArgb(20, 20, 20)
            txtArticle.ForeColor = Color.Orange
            Controls.Add(txtArticle)
            lblStatus = modUtilities.MakeUtilityLabel(Me, 12, 540, 796, 20, 10F)
        End Sub

        Private Sub RefreshFeedList()
            lstFeeds.Items.Clear()
            For Each feedUrl In feedUrls
                lstFeeds.Items.Add(feedUrl)
            Next
            lblStatus.Text = feedUrls.Count.ToString() & " FEED(S)"
        End Sub

        Private Sub OnAddFeed(sender As Object, e As EventArgs)
            Dim text = InputBox("Feed URL (RSS or Atom):", "LCARS RSS", "https://")
            If Not String.IsNullOrWhiteSpace(text) Then
                feedUrls.Add(text.Trim())
                RssFeedStore.SaveFeedUrls(feedUrls)
                RefreshFeedList()
            End If
        End Sub

        Private Sub OnFeedSelected(sender As Object, e As EventArgs)
            If lstFeeds.SelectedIndex >= 0 Then
                OnRefreshFeed(RuntimeHelpers.GetObjectValue(sender), e)
            End If
        End Sub

        Private Sub OnRefreshFeed(sender As Object, e As EventArgs)
            If lstFeeds.SelectedIndex < 0 AndAlso feedUrls.Count > 0 Then
                lstFeeds.SelectedIndex = 0
            End If
            If lstFeeds.SelectedIndex < 0 Then
                Return
            End If
            Try
                lblStatus.Text = "FETCHING…"
                Call Application.DoEvents()
                Dim xml = RssFeedStore.FetchFeedXml(feedUrls(lstFeeds.SelectedIndex))
                currentItems = RssFeedParser.Parse(xml)
                lstItems.Items.Clear()
                For Each currentItem In currentItems
                    Dim items = lstItems.Items
                    Dim rssItem = currentItem
                    items.Add(If(Not Equals(rssItem.Title, Nothing), rssItem.Title, "(untitled)"))
                Next
                lblStatus.Text = currentItems.Count.ToString() & " ARTICLE(S)"
            Catch ex As Exception
                ProjectData.SetProjectError(ex)
                Dim ex2 = ex
                lblStatus.Text = ex2.Message.ToUpperInvariant()
                Call ProjectData.ClearProjectError()
            End Try
        End Sub

        Private Sub OnItemSelected(sender As Object, e As EventArgs)
            If lstItems.SelectedIndex >= 0 AndAlso lstItems.SelectedIndex < currentItems.Count Then
                Dim rssItem = currentItems(lstItems.SelectedIndex)
                txtArticle.Text = rssItem.Title & vbCrLf & vbCrLf & rssItem.Description & vbCrLf & vbCrLf & rssItem.Link
            End If
        End Sub
    End Class
