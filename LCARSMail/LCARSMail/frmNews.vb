Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms
Imports LCARS
Imports LCARS.Controls
    Public Class frmNews
        Inherits LCARSForm
        Private txtHost As TextBox

        Private lstGroups As ListBox

        Private lstArticles As ListBox

        Private txtArticle As TextBox

        Private txtPostSubject As TextBox

        Private txtPostBody As TextBox

        Private lblStatus As Label

        Private currentGroup As String

        Private articleHeaders As List(Of NntpArticleHeader)

        Public Sub New()
            CType(CObj(Me), Form).BackColor = Color.Black
            Dim clientSize As Size = New Size(800, 560)
            CType(CObj(Me), Form).ClientSize = clientSize
            CType(CObj(Me), Form).StartPosition = FormStartPosition.CenterParent
            articleHeaders = New List(Of NntpArticleHeader)()
            BuildUi()
            Dim text As String = NntpSubscriptionStore.LoadLastHost()
            If text.Length > 0 Then
                txtHost.Text = text
            End If
            OnListSubscriptions(Nothing, EventArgs.Empty)
        End Sub

        Private Sub BuildUi()
            Dim val As FlatButton = New FlatButton()
            CType(val, LCARSbuttonClass).ButtonText = "NEWS"
            CType(val, LCARSbuttonClass).Text = "NEWS"
            CType(val, LCARSbuttonClass).Clickable = False
            CType(val, LCARSbuttonClass).Color = CType(8, LCARScolorStyles)
            Dim location As Point = New Point(12, 8)
            CType(CObj(val), Control).Location = location
            Dim size As Size = New Size(80, 32)
            CType(CObj(val), Control).Size = size
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val), Control))
            txtHost = New TextBox()
            Dim textBox = txtHost
            location = New Point(100, 12)
            textBox.Location = location
            Dim textBox2 = txtHost
            size = New Size(280, 24)
            textBox2.Size = size
            txtHost.Text = "news.example.com"
            txtHost.BackColor = Color.FromArgb(40, 40, 40)
            txtHost.ForeColor = Color.Orange
            CType(CObj(Me), Control).Controls.Add(txtHost)
            Dim val2 As FlatButton = New FlatButton()
            CType(val2, LCARSbuttonClass).ButtonText = "GROUPS"
            CType(val2, LCARSbuttonClass).Text = "GROUPS"
            CType(val2, LCARSbuttonClass).Color = CType(6, LCARScolorStyles)
            size = New Size(90, 28)
            CType(CObj(val2), Control).Size = size
            location = New Point(390, 10)
            CType(CObj(val2), Control).Location = location
            AddHandler CType(CObj(val2), Control).Click, AddressOf OnListGroups
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val2), Control))
            Dim val3 As FlatButton = New FlatButton()
            CType(val3, LCARSbuttonClass).ButtonText = "SUBS"
            CType(val3, LCARSbuttonClass).Text = "SUBS"
            CType(val3, LCARSbuttonClass).Color = CType(1, LCARScolorStyles)
            size = New Size(72, 28)
            CType(CObj(val3), Control).Size = size
            location = New Point(488, 10)
            CType(CObj(val3), Control).Location = location
            AddHandler CType(CObj(val3), Control).Click, AddressOf OnListSubscriptions
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val3), Control))
            Dim val4 As FlatButton = New FlatButton()
            CType(val4, LCARSbuttonClass).ButtonText = "SUB"
            CType(val4, LCARSbuttonClass).Text = "SUB"
            CType(val4, LCARSbuttonClass).Color = CType(2, LCARScolorStyles)
            size = New Size(64, 28)
            CType(CObj(val4), Control).Size = size
            location = New Point(566, 10)
            CType(CObj(val4), Control).Location = location
            AddHandler CType(CObj(val4), Control).Click, AddressOf OnSubscribeCurrent
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val4), Control))
            Dim val5 As FlatButton = New FlatButton()
            CType(val5, LCARSbuttonClass).ButtonText = "UNSUB"
            CType(val5, LCARSbuttonClass).Text = "UNSUB"
            CType(val5, LCARSbuttonClass).Color = CType(9, LCARScolorStyles)
            size = New Size(72, 28)
            CType(CObj(val5), Control).Size = size
            location = New Point(636, 10)
            CType(CObj(val5), Control).Location = location
            AddHandler CType(CObj(val5), Control).Click, AddressOf OnUnsubscribeCurrent
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val5), Control))
            lstGroups = New ListBox()
            Dim listBox = lstGroups
            location = New Point(12, 48)
            listBox.Location = location
            Dim listBox2 = lstGroups
            size = New Size(240, 200)
            listBox2.Size = size
            lstGroups.BackColor = Color.FromArgb(30, 30, 30)
            lstGroups.ForeColor = Color.Orange
            AddHandler lstGroups.SelectedIndexChanged, AddressOf OnGroupSelected
            CType(CObj(Me), Control).Controls.Add(lstGroups)
            lstArticles = New ListBox()
            Dim listBox3 = lstArticles
            location = New Point(260, 48)
            listBox3.Location = location
            Dim listBox4 = lstArticles
            size = New Size(528, 200)
            listBox4.Size = size
            lstArticles.BackColor = Color.FromArgb(30, 30, 30)
            lstArticles.ForeColor = Color.Orange
            AddHandler lstArticles.SelectedIndexChanged, AddressOf OnArticleSelected
            CType(CObj(Me), Control).Controls.Add(lstArticles)
            txtArticle = New TextBox()
            txtArticle.Multiline = True
            txtArticle.ScrollBars = ScrollBars.Vertical
            Dim textBox3 = txtArticle
            location = New Point(12, 256)
            textBox3.Location = location
            Dim textBox4 = txtArticle
            size = New Size(776, 200)
            textBox4.Size = size
            txtArticle.BackColor = Color.FromArgb(30, 30, 30)
            txtArticle.ForeColor = Color.Orange
            CType(CObj(Me), Control).Controls.Add(txtArticle)
            txtPostSubject = New TextBox()
            Dim textBox5 = txtPostSubject
            location = New Point(12, 464)
            textBox5.Location = location
            Dim textBox6 = txtPostSubject
            size = New Size(776, 24)
            textBox6.Size = size
            txtPostSubject.BackColor = Color.FromArgb(40, 40, 40)
            txtPostSubject.ForeColor = Color.Orange
            CType(CObj(Me), Control).Controls.Add(txtPostSubject)
            txtPostBody = New TextBox()
            Dim textBox7 = txtPostBody
            location = New Point(12, 492)
            textBox7.Location = location
            Dim textBox8 = txtPostBody
            size = New Size(580, 24)
            textBox8.Size = size
            txtPostBody.BackColor = Color.FromArgb(40, 40, 40)
            txtPostBody.ForeColor = Color.Orange
            CType(CObj(Me), Control).Controls.Add(txtPostBody)
            Dim val6 As FlatButton = New FlatButton()
            CType(val6, LCARSbuttonClass).ButtonText = "POST"
            CType(val6, LCARSbuttonClass).Text = "POST"
            CType(val6, LCARSbuttonClass).Color = CType(1, LCARScolorStyles)
            size = New Size(90, 28)
            CType(CObj(val6), Control).Size = size
            location = New Point(600, 490)
            CType(CObj(val6), Control).Location = location
            AddHandler CType(CObj(val6), Control).Click, AddressOf OnPost
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val6), Control))
            lblStatus = New Label()
            lblStatus.ForeColor = Color.Orange
            Dim label = lblStatus
            location = New Point(12, 528)
            label.Location = location
            Dim label2 = lblStatus
            size = New Size(776, 24)
            label2.Size = size
            CType(CObj(Me), Control).Controls.Add(lblStatus)
        End Sub

        Private Sub OnListGroups(sender As Object, e As EventArgs)
            lstGroups.Items.Clear()
            lstArticles.Items.Clear()
            txtArticle.Clear()
            NntpSubscriptionStore.SaveLastHost(txtHost.Text.Trim())
            Dim list As List(Of String) = NntpNewsService.TryListGroups(txtHost.Text.Trim(), 119)
            If Not String.IsNullOrEmpty(NntpNewsService.LastError) Then
                lblStatus.Text = NntpNewsService.LastError.ToUpperInvariant()
                Return
            End If
            For Each item In list
                lstGroups.Items.Add(item)
            Next
            lblStatus.Text = list.Count.ToString() & " GROUP(S)"
        End Sub

        Private Sub OnGroupSelected(sender As Object, e As EventArgs)
            If lstGroups.SelectedItem Is Nothing Then
                Return
            End If
            currentGroup = lstGroups.SelectedItem.ToString()
            lstArticles.Items.Clear()
            articleHeaders = NntpNewsService.TryListArticles(txtHost.Text.Trim(), 119, currentGroup)
            If articleHeaders Is Nothing Then
                lblStatus.Text = If(Not Equals(NntpNewsService.LastError, Nothing), NntpNewsService.LastError, "ARTICLES FAILED").ToUpperInvariant()
                Return
            End If
            Dim lastArticleNumber = NntpSubscriptionStore.GetLastArticleNumber(currentGroup)
            Dim num = lastArticleNumber
            For Each articleHeader In articleHeaders
                If articleHeader IsNot Nothing Then
                    Dim text = ""
                    If articleHeader.ArticleNumber > lastArticleNumber Then
                        text = "* "
                    End If
                    Dim items = lstArticles.Items
                    Dim array As String() = New String(4) {text, "#", articleHeader.ArticleNumber.ToString(), " ", Nothing}
                    Dim nntpArticleHeader = articleHeader
                    array(4) = If(Not Equals(nntpArticleHeader.Subject, Nothing), nntpArticleHeader.Subject, "")
                    items.Add(String.Concat(array))
                    If articleHeader.ArticleNumber > num Then
                        num = articleHeader.ArticleNumber
                    End If
                End If
            Next
            If num > lastArticleNumber Then
                lblStatus.Text = articleHeaders.Count.ToString() & " ARTICLE(S) — " & num - lastArticleNumber.ToString() & " NEW"
            End If
        End Sub

        Private Sub OnArticleSelected(sender As Object, e As EventArgs)
            Dim selectedIndex = lstArticles.SelectedIndex
            If selectedIndex >= 0 AndAlso selectedIndex < articleHeaders.Count Then
                Dim articleNumber = articleHeaders(selectedIndex).ArticleNumber
                txtArticle.Text = NntpNewsService.TryFetchArticle(txtHost.Text.Trim(), 119, currentGroup, articleNumber)
                NntpSubscriptionStore.SetLastArticleNumber(currentGroup, articleNumber)
            End If
        End Sub

        Private Sub OnListSubscriptions(sender As Object, e As EventArgs)
            lstGroups.Items.Clear()
            lstArticles.Items.Clear()
            txtArticle.Clear()
            For Each item In NntpSubscriptionStore.LoadGroups()
                lstGroups.Items.Add(item)
            Next
            lblStatus.Text = lstGroups.Items.Count.ToString() & " SUBSCRIPTION(S)"
        End Sub

        Private Sub OnSubscribeCurrent(sender As Object, e As EventArgs)
            If lstGroups.SelectedItem Is Nothing Then
                lblStatus.Text = "SELECT A GROUP"
                Return
            End If
            NntpSubscriptionStore.Subscribe(lstGroups.SelectedItem.ToString())
            lblStatus.Text = "SUBSCRIBED"
        End Sub

        Private Sub OnUnsubscribeCurrent(sender As Object, e As EventArgs)
            If lstGroups.SelectedItem Is Nothing Then
                lblStatus.Text = "SELECT A GROUP"
                Return
            End If
            NntpSubscriptionStore.Unsubscribe(lstGroups.SelectedItem.ToString())
            OnListSubscriptions(Nothing, EventArgs.Empty)
        End Sub

        Private Sub OnPost(sender As Object, e As EventArgs)
            If String.IsNullOrWhiteSpace(currentGroup) Then
                lblStatus.Text = "SELECT A GROUP"
            ElseIf NntpNewsService.TryPostArticle(txtHost.Text.Trim(), 119, "lcars@local", txtPostSubject.Text, currentGroup, txtPostBody.Text) Then
                lblStatus.Text = "POSTED"
                OnGroupSelected(Nothing, EventArgs.Empty)
            Else
                lblStatus.Text = If(Not Equals(NntpNewsService.LastError, Nothing), NntpNewsService.LastError, "POST FAILED").ToUpperInvariant()
            End If
        End Sub
    End Class

