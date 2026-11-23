Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.IO
Imports System.Threading
Imports System.Windows.Forms
Imports LCARS
Imports LCARS.Controls
Imports Microsoft.VisualBasic.CompilerServices
    Public Class frmAddressBook
        Inherits LCARSForm
        Private lstContacts As ListBox

        Private lblStatus As Label

        Private liveMode As Boolean

        Private ReadOnly shownContacts As List(Of MailContact)

        Private ReadOnly liveTimer As Windows.Forms.Timer

        Public Sub New()
            shownContacts = New List(Of MailContact)()
            liveTimer = New Windows.Forms.Timer()
            CType(CObj(Me), Form).BackColor = Color.Black
            Dim clientSize As Size = New Size(560, 480)
            CType(CObj(Me), Form).ClientSize = clientSize
            CType(CObj(Me), Form).StartPosition = FormStartPosition.CenterParent
            BuildUi()
            liveMode = True
            RefreshList()
            liveTimer.Interval = 10000
            AddHandler liveTimer.Tick, AddressOf OnLiveTimerTick
            liveTimer.Start()
            CardDavLiveBookCache.EnsureAlwaysOnStarted()
            CardDavLiveBookCache.RefreshNow()
            AddressBookStore.TryValidateStoredSourceMab()
            Call ThreadPool.QueueUserWorkItem(Sub(state)
                                                  Try
                                                      Dim serverUrl As String = Nothing
                                                      Dim username As String = Nothing
                                                      Dim password As String = Nothing
                                                      If AddressBookStore.TryLoadCardDavSettings(serverUrl, username, password) Then
                                                          AddressBookStore.TryRefreshDiscoveredCardDavBooksFromServer(serverUrl, username, password)
                                                      End If
                                                  Catch projectError As Exception
                                                      ProjectData.SetProjectError(projectError)
                                                      Call ProjectData.ClearProjectError()
                                                  End Try
                                              End Sub)
        End Sub

        Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
            liveTimer.Stop()
            CType(Me, Form).OnFormClosed(e)
        End Sub

        Private Sub OnLiveTimerTick(sender As Object, e As EventArgs)
            If Not liveMode Then
                Return
            End If
            Dim num As Integer = AddressBookStore.CountCardDavDirtyContacts()
            liveTimer.Interval = If(num > 0, 4000, 8000)
            Call ThreadPool.QueueUserWorkItem(Sub(state)
                                                  Try
                                                      AddressBookStore.TrySyncStoredCardDavBidirectional()
                                                      CardDavLiveBookCache.QueueBackgroundRefresh()
                                                  Catch projectError As Exception
                                                      ProjectData.SetProjectError(projectError)
                                                      Call ProjectData.ClearProjectError()
                                                  End Try
                                                  Try
                                                      If Not CType(CObj(Me), Control).IsDisposed AndAlso CType(CObj(Me), Control).IsHandleCreated Then
                                                          Call CType(CObj(Me), Control).BeginInvoke(CType(New Action(AddressOf RefreshList), [Delegate]))
                                                      End If
                                                  Catch projectError2 As Exception
                                                      ProjectData.SetProjectError(projectError2)
                                                      Call ProjectData.ClearProjectError()
                                                  End Try
                                              End Sub)
        End Sub

        Private Sub BuildUi()
            Dim val As FlatButton = New FlatButton()
            CType(val, LCARSbuttonClass).ButtonText = "ADDRESS BOOK"
            CType(val, LCARSbuttonClass).Text = "ADDRESS BOOK"
            CType(val, LCARSbuttonClass).Color = CType(6, LCARScolorStyles)
            Dim size As Size = New Size(200, 28)
            CType(CObj(val), Control).Size = size
            Dim location As Point = New Point(12, 12)
            CType(CObj(val), Control).Location = location
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val), Control))
            Dim val2 As FlatButton = New FlatButton()
            CType(val2, LCARSbuttonClass).ButtonText = "LIVE REFRESH"
            CType(val2, LCARSbuttonClass).Text = "LIVE REFRESH"
            CType(val2, LCARSbuttonClass).Color = CType(1, LCARScolorStyles)
            size = New Size(120, 28)
            CType(CObj(val2), Control).Size = size
            location = New Point(220, 12)
            CType(CObj(val2), Control).Location = location
            AddHandler CType(CObj(val2), Control).Click, AddressOf OnLiveRefresh
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val2), Control))
            Dim val3 As FlatButton = New FlatButton()
            CType(val3, LCARSbuttonClass).ButtonText = "SYNC SAVE"
            CType(val3, LCARSbuttonClass).Text = "SYNC SAVE"
            CType(val3, LCARSbuttonClass).Color = CType(4, LCARScolorStyles)
            size = New Size(100, 28)
            CType(CObj(val3), Control).Size = size
            location = New Point(350, 12)
            CType(CObj(val3), Control).Location = location
            AddHandler CType(CObj(val3), Control).Click, AddressOf OnSyncSave
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val3), Control))
            Dim val4 As FlatButton = New FlatButton()
            CType(val4, LCARSbuttonClass).ButtonText = "ADD"
            CType(val4, LCARSbuttonClass).Text = "ADD"
            CType(val4, LCARSbuttonClass).Color = CType(2, LCARScolorStyles)
            size = New Size(72, 28)
            CType(CObj(val4), Control).Size = size
            location = New Point(460, 12)
            CType(CObj(val4), Control).Location = location
            AddHandler CType(CObj(val4), Control).Click, AddressOf OnAdd
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val4), Control))
            Dim val5 As FlatButton = New FlatButton()
            CType(val5, LCARSbuttonClass).ButtonText = "412 MODE"
            CType(val5, LCARSbuttonClass).Text = "412 MODE"
            CType(val5, LCARSbuttonClass).Color = CType(9, LCARScolorStyles)
            size = New Size(88, 28)
            CType(CObj(val5), Control).Size = size
            location = New Point(12, 436)
            CType(CObj(val5), Control).Location = location
            AddHandler CType(CObj(val5), Control).Click, AddressOf OnToggle412Mode
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val5), Control))
            lstContacts = New ListBox()
            Dim listBox = lstContacts
            location = New Point(12, 48)
            listBox.Location = location
            Dim listBox2 = lstContacts
            size = New Size(536, 380)
            listBox2.Size = size
            lstContacts.BackColor = Color.FromArgb(30, 30, 30)
            lstContacts.ForeColor = Color.Orange
            AddHandler lstContacts.DoubleClick, AddressOf OnEditSelected
            CType(CObj(Me), Control).Controls.Add(lstContacts)
            lblStatus = New Label()
            lblStatus.ForeColor = Color.Orange
            Dim label = lblStatus
            location = New Point(108, 436)
            label.Location = location
            Dim label2 = lblStatus
            size = New Size(440, 32)
            label2.Size = size
            CType(CObj(Me), Control).Controls.Add(lblStatus)
        End Sub

        Private Sub RefreshList()
            lstContacts.Items.Clear()
            shownContacts.Clear()
            Dim list As List(Of MailContact) = (If(liveMode, CardDavLiveBookCache.GetLiveContacts(), AddressBookStore.LoadContacts()))
            If list Is Nothing Then
                list = AddressBookStore.LoadContacts()
            End If
            For Each item In list
                If item IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(item.Email) Then
                    shownContacts.Add(item)
                    lstContacts.Items.Add(item.ListCaption().ToUpperInvariant())
                End If
            Next
            Dim text = If(CardDavLiveBookCache.AlwaysOnActive, " · TIMER ON", "")
            Dim text2 = If(liveMode, "ALWAYS-ON LIVE BOOK" & text, "LOCAL JSON")
            Dim text3 = If(String.IsNullOrWhiteSpace(CardDavLiveBookCache.LastRefreshSummary), "", " — " & CardDavLiveBookCache.LastRefreshSummary)
            Dim text4 = ""
            If liveMode AndAlso Date.Compare(CardDavLiveBookCache.LastFetchUtc, Date.MinValue) <> 0 Then
                text4 = " @ " & CardDavLiveBookCache.LastFetchUtc.ToLocalTime().ToString("HH:mm:ss")
            End If
            Dim num = 0
            For Each shownContact In shownContacts
                If shownContact IsNot Nothing AndAlso shownContact.CardDavDirty Then
                    num = num + 1
                End If
            Next
            Dim text5 As String = (If(num > 0, (" — " & num.ToString() & " DIRTY"), ""))
            Dim text6 = ""
            Dim serverUrl As String = Nothing
            Dim username As String = Nothing
            Dim password As String = Nothing
            If AddressBookStore.TryLoadCardDavSettings(serverUrl, username, password) AndAlso Not String.IsNullOrWhiteSpace(serverUrl) Then
                Dim text7 As String = serverUrl.Trim()
                If text7.Length > 36 Then
                    text7 = text7.Substring(0, 33) & "…"
                End If
                text6 = " — " & text7
            End If
            Dim text8 = ""
            If MorkAbookParser.LastSourceMabRoundTripValidated Then
                text8 = " · MORK SRC OK"
                If MorkAbookParser.LastSourceMabRoundTripByteIdentical Then
                    text8 += " BYTE-IDENT"
                End If
            End If
            If MorkAbookParser.LastSourceMabRoundTripByteIdentical Then
                text8 += " · BYTE-RT OK"
            End If
            If MorkAbookParser.LastMorkPreserveExportFieldValidated Then
                text8 += " · MORK-FIELD-RT"
            End If
            If MorkAbookParser.LastMabExportByteIdentical Then
                text8 += " · MORK-EXPORT-BYTE"
            End If
            If AddressBookStore.LastCardDavLiveOverlayCount > 0 Then
                text8 = text8 & " · LIVE-OVERLAY " & AddressBookStore.LastCardDavLiveOverlayCount.ToString()
            End If
            If MorkAbookParser.LastMorkColumnKeyCount > 0 Then
                text8 = text8 & " · COLS " & MorkAbookParser.LastMorkColumnKeyCount.ToString()
            End If
            If MorkAbookParser.LastMorkRowPropertyFieldCount > 0 Then
                text8 = text8 & " · FIELDS " & MorkAbookParser.LastMorkRowPropertyFieldCount.ToString()
            End If
            If MorkAbookParser.LastMorkTableCount > 0 Then
                text8 = text8 & " · TABLES " & MorkAbookParser.LastMorkTableCount.ToString()
            End If
            If MorkAbookParser.LastMorkParsedContactCount > 0 Then
                text8 = text8 & " · ROWS " & MorkAbookParser.LastMorkParsedContactCount.ToString()
            End If
            If MorkAbookParser.LastMorkRowMapCount > 0 Then
                text8 = text8 & " · RMAP " & MorkAbookParser.LastMorkRowMapCount.ToString()
            End If
            If CardDavLiveBookCache.AlwaysOnActive AndAlso Date.Compare(CardDavLiveBookCache.LastAlwaysOnSyncUtc, Date.MinValue) <> 0 Then
                text8 = text8 & " · LIVE @" & CardDavLiveBookCache.LastAlwaysOnSyncUtc.ToLocalTime().ToString("HH:mm")
            End If
            If CardDavLiveBookCache.AlwaysOnActive AndAlso CardDavLiveBookCache.AlwaysOnCycleCount > 0 Then
                text8 = text8 & " · CYCLE " & CardDavLiveBookCache.AlwaysOnCycleCount.ToString()
            End If
            If CardDavSyncService.Last412ConflictEmail.Length > 0 Then
                text8 = text8 & " · 412@" & CardDavSyncService.Last412ConflictEmail
            End If
            If Date.Compare(AddressBookStore.LastCardDavBidirectionalUtc, Date.MinValue) <> 0 Then
                text8 = text8 & " · CARDDAV @" & AddressBookStore.LastCardDavBidirectionalUtc.ToLocalTime().ToString("HH:mm")
            End If
            If AddressBookStore.LastCardDavDiscoveredBooks > 0 Then
                text8 = text8 & " · " & AddressBookStore.LastCardDavDiscoveredBooks.ToString() & " BOOKS"
            End If
            If AddressBookStore.LastCardDav412BatchResolved > 0 Then
                text8 = text8 & " · 412-FIX " & AddressBookStore.LastCardDav412BatchResolved.ToString()
            End If
            Dim text9 As String = If(Not Equals(CardDavSyncService.ConflictOn412Mode, Nothing), CardDavSyncService.ConflictOn412Mode, "").Trim()
            If text9.Length > 0 Then
                text8 = text8 & " · 412=" & text9.ToUpperInvariant()
            End If
            If AddressBookStore.LastCardDavSecondaryBooksMerged > 0 Then
                text8 = text8 & " · 2ND+" & AddressBookStore.LastCardDavSecondaryBooksMerged.ToString()
            End If
            Dim text10 As String = AddressBookStore.TryGetStoredSourceMabPath()
            If text10.Length > 0 Then
                Dim fileName = Path.GetFileName(text10)
                text8 = text8 & " · " & fileName
            End If
            lblStatus.Text = lstContacts.Items.Count.ToString() & " CONTACT(S) — " & text2 & text4 & text3 & text5 & text8 & text6
        End Sub

        Private Sub OnLiveRefresh(sender As Object, e As EventArgs)
            liveMode = True
            RefreshList()
            If lstContacts.Items.Count = 0 Then
                lblStatus.Text = If(Not Equals(AddressBookStore.LastError, Nothing), AddressBookStore.LastError, "LIVE REFRESH FAILED").ToUpperInvariant()
            End If
        End Sub

        Private Sub OnSyncSave(sender As Object, e As EventArgs)
            Dim num As Integer = AddressBookStore.TrySyncStoredCardDavBidirectional()
            If num < 0 Then
                lblStatus.Text = If(Not Equals(AddressBookStore.LastError, Nothing), AddressBookStore.LastError, "SYNC FAILED").ToUpperInvariant()
                Return
            End If
            liveMode = True
            RefreshList()
            Dim text As String = EwsGalWriteProofStore.TryLoadLastProofSummary()
            Dim text2 = If(text.Length > 0 AndAlso text.Length <= 64, " · " & text, "")
            lblStatus.Text = num.ToString() & " CONTACT(S) MERGED — LIVE BOOK ACTIVE" & text2
        End Sub

        Private Sub OnAdd(sender As Object, e As EventArgs)
            Dim frmContactEdit2 As frmContactEdit = New frmContactEdit()
            Try
                If CType(CObj(frmContactEdit2), Form).ShowDialog(CType(Me, IWin32Window)) <> DialogResult.OK Then
                    Return
                End If

            Finally
                If frmContactEdit2 IsNot Nothing Then CType(frmContactEdit2, IDisposable).Dispose()
            End Try
            RefreshList()
        End Sub

        Private Sub OnToggle412Mode(sender As Object, e As EventArgs)
            If String.Equals(CardDavSyncService.ConflictOn412Mode, "ServerWins", StringComparison.OrdinalIgnoreCase) Then
                CardDavSyncService.ConflictOn412Mode = "ClientMerge"
            Else
                CardDavSyncService.ConflictOn412Mode = "ServerWins"
            End If
            RefreshList()
            lblStatus.Text = "412 POLICY — " & CardDavSyncService.ConflictOn412Mode.ToUpperInvariant()
        End Sub

        Private Sub OnEditSelected(sender As Object, e As EventArgs)
            Dim selectedIndex = lstContacts.SelectedIndex
            If selectedIndex < 0 OrElse selectedIndex >= shownContacts.Count Then
                Return
            End If
            Dim mailContact = shownContacts(selectedIndex)
            If mailContact IsNot Nothing Then
                Dim frmContactEdit2 As frmContactEdit = New frmContactEdit(mailContact)
                Try
                    CType(CObj(frmContactEdit2), Form).ShowDialog(CType(Me, IWin32Window))
                Finally
                    If frmContactEdit2 IsNot Nothing Then CType(frmContactEdit2, IDisposable).Dispose()
                End Try
                RefreshList()
            End If
        End Sub
    End Class

