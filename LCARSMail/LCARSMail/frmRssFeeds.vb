Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms
Imports LCARS
Imports LCARS.Controls
    Public Class frmRssFeeds
        Inherits LCARSForm
        Private txtFeedUrl As TextBox

        Private lstItems As ListBox

        Private txtBody As TextBox

        Private lblStatus As Label

        Private currentItems As List(Of RssFeedItem)

        Private lstFeeds As ListBox

        Public Sub New()
            CType(CObj(Me), Form).BackColor = Color.Black
            Dim clientSize As Size = New Size(720, 560)
            CType(CObj(Me), Form).ClientSize = clientSize
            CType(CObj(Me), Form).StartPosition = FormStartPosition.CenterParent
            currentItems = New List(Of RssFeedItem)()
            BuildUi()
            For Each item In RssSubscriptionStore.LoadFeedUrls()
                lstFeeds.Items.Add(item)
            Next
        End Sub

        Private Sub BuildUi()
            txtFeedUrl = New TextBox()
            Dim textBox = txtFeedUrl
            Dim location As Point = New Point(12, 12)
            textBox.Location = location
            Dim textBox2 = txtFeedUrl
            Dim size As Size = New Size(520, 24)
            textBox2.Size = size
            txtFeedUrl.BackColor = Color.FromArgb(40, 40, 40)
            txtFeedUrl.ForeColor = Color.Orange
            CType(CObj(Me), Control).Controls.Add(txtFeedUrl)
            Dim val As FlatButton = New FlatButton()
            CType(val, LCARSbuttonClass).ButtonText = "LOAD"
            CType(val, LCARSbuttonClass).Text = "LOAD"
            CType(val, LCARSbuttonClass).Color = CType(6, LCARScolorStyles)
            size = New Size(80, 28)
            CType(CObj(val), Control).Size = size
            location = New Point(540, 10)
            CType(CObj(val), Control).Location = location
            AddHandler CType(CObj(val), Control).Click, AddressOf OnLoadFeed
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val), Control))
            Dim val2 As FlatButton = New FlatButton()
            CType(val2, LCARSbuttonClass).ButtonText = "SUB"
            CType(val2, LCARSbuttonClass).Text = "SUB"
            CType(val2, LCARSbuttonClass).Color = CType(2, LCARScolorStyles)
            size = New Size(56, 28)
            CType(CObj(val2), Control).Size = size
            location = New Point(628, 10)
            CType(CObj(val2), Control).Location = location
            AddHandler CType(CObj(val2), Control).Click, AddressOf OnSubscribeFeed
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val2), Control))
            lstFeeds = New ListBox()
            Dim listBox = lstFeeds
            location = New Point(12, 44)
            listBox.Location = location
            Dim listBox2 = lstFeeds
            size = New Size(696, 72)
            listBox2.Size = size
            lstFeeds.BackColor = Color.FromArgb(25, 25, 25)
            lstFeeds.ForeColor = Color.Orange
            AddHandler lstFeeds.SelectedIndexChanged, AddressOf OnSavedFeedSelected
            CType(CObj(Me), Control).Controls.Add(lstFeeds)
            Dim val3 As FlatButton = New FlatButton()
            CType(val3, LCARSbuttonClass).ButtonText = "PLAY"
            CType(val3, LCARSbuttonClass).Text = "PLAY"
            CType(val3, LCARSbuttonClass).Color = CType(1, LCARScolorStyles)
            size = New Size(80, 28)
            CType(CObj(val3), Control).Size = size
            location = New Point(692, 10)
            CType(CObj(val3), Control).Location = location
            AddHandler CType(CObj(val3), Control).Click, AddressOf OnPlayEnclosure
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val3), Control))
            lstItems = New ListBox()
            Dim listBox3 = lstItems
            location = New Point(12, 124)
            listBox3.Location = location
            Dim listBox4 = lstItems
            size = New Size(696, 160)
            listBox4.Size = size
            lstItems.BackColor = Color.FromArgb(30, 30, 30)
            lstItems.ForeColor = Color.Orange
            AddHandler lstItems.SelectedIndexChanged, AddressOf OnItemSelected
            CType(CObj(Me), Control).Controls.Add(lstItems)
            txtBody = New TextBox()
            txtBody.Multiline = True
            txtBody.ScrollBars = ScrollBars.Vertical
            Dim textBox3 = txtBody
            location = New Point(12, 292)
            textBox3.Location = location
            Dim textBox4 = txtBody
            size = New Size(696, 220)
            textBox4.Size = size
            txtBody.BackColor = Color.FromArgb(30, 30, 30)
            txtBody.ForeColor = Color.Orange
            CType(CObj(Me), Control).Controls.Add(txtBody)
            lblStatus = New Label()
            lblStatus.ForeColor = Color.Orange
            Dim label = lblStatus
            location = New Point(12, 528)
            label.Location = location
            Dim label2 = lblStatus
            size = New Size(696, 24)
            label2.Size = size
            CType(CObj(Me), Control).Controls.Add(lblStatus)
        End Sub

        Private Sub OnSubscribeFeed(sender As Object, e As EventArgs)
            Dim text As String = txtFeedUrl.Text.Trim()
            If text.Length <> 0 Then
                RssSubscriptionStore.Subscribe(text)
                If Not lstFeeds.Items.Contains(text) Then
                    lstFeeds.Items.Add(text)
                End If
                lblStatus.Text = "FEED SUBSCRIBED"
            End If
        End Sub

        Private Sub OnSavedFeedSelected(sender As Object, e As EventArgs)
            If lstFeeds.SelectedItem IsNot Nothing Then
                txtFeedUrl.Text = lstFeeds.SelectedItem.ToString()
                OnLoadFeed(Nothing, EventArgs.Empty)
            End If
        End Sub

        Private Sub OnLoadFeed(sender As Object, e As EventArgs)
            lstItems.Items.Clear()
            Dim feedUrl As String = txtFeedUrl.Text.Trim()
            currentItems = RssFeedService.FetchItems(feedUrl)
            If currentItems Is Nothing Then
                lblStatus.Text = If(Not Equals(RssFeedService.LastError, Nothing), RssFeedService.LastError, "LOAD FAILED").ToUpperInvariant()
                Return
            End If
            RssSubscriptionStore.Subscribe(feedUrl)
            For Each currentItem In currentItems
                Dim text = ""
                If Not String.IsNullOrWhiteSpace(currentItem.EnclosureUrl) Then
                    text = "[MEDIA] "
                End If
                Dim items = lstItems.Items
                Dim text2 = text
                Dim rssFeedItem = currentItem
                items.Add(text2 & If(Not Equals(rssFeedItem.Title, Nothing), rssFeedItem.Title, currentItem.Link))
            Next
            lblStatus.Text = currentItems.Count.ToString() & " ITEM(S)"
        End Sub

        Private Sub OnItemSelected(sender As Object, e As EventArgs)
            Dim selectedIndex = lstItems.SelectedIndex
            If selectedIndex >= 0 AndAlso selectedIndex < currentItems.Count Then
                Dim rssFeedItem = currentItems(selectedIndex)
                Dim textBox = txtBody
                Dim rssFeedItem2 = rssFeedItem
                textBox.Text = If(Not Equals(rssFeedItem2.Description, Nothing), rssFeedItem2.Description, "")
                If Not String.IsNullOrWhiteSpace(rssFeedItem.EnclosureUrl) Then
                    lblStatus.Text = "ENCLOSURE READY — TAP PLAY"
                End If
            End If
        End Sub

        Private Sub OnPlayEnclosure(sender As Object, e As EventArgs)
            Dim selectedIndex = lstItems.SelectedIndex
            If selectedIndex >= 0 AndAlso selectedIndex < currentItems.Count Then
                RssFeedService.PlayEnclosure(currentItems(selectedIndex))
                If Not String.IsNullOrEmpty(RssFeedService.LastError) Then
                    lblStatus.Text = RssFeedService.LastError.ToUpperInvariant()
                End If
            End If
        End Sub
    End Class

