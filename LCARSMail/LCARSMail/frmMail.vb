Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Drawing
Imports System.Globalization
Imports System.IO
Imports System.Text
Imports System.Threading
Imports System.Windows.Forms
Imports LCARS
Imports LCARS.Controls
Imports Microsoft.VisualBasic.CompilerServices
    Public Class frmMail
        Inherits LCARSForm
        Private Enum CommsMode
            Mail
            Chat
            Rss
            Calendar
        End Enum

        Private Class MessageRowTag
            Public Header As MailMessageHeader

            Public Account As MailAccount

            Public ThreadKey As String

            Public ThreadCount As Integer

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Private Class FolderRefreshEntry
            Public Account As MailAccount

            Public Password As String

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Private Class FolderWorkerArgs
            Public Entries As List(Of FolderRefreshEntry)

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Private Class AccountFolderSection
            Public Account As MailAccount

            Public Folders As List(Of MailFolderInfo)

            Public ErrorText As String

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Private Class FolderRowTag
            Public Account As MailAccount

            Public Folder As MailFolderInfo

            Public Flip As Boolean

            Public IsUnifiedInbox As Boolean

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Private Class HeaderWorkerArgs
            Public Account As MailAccount

            Public Password As String

            Public Folder As String

            Public Unified As Boolean

            Public Entries As List(Of FolderRefreshEntry)

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Private Class HeaderWorkerResult
            Public Headers As List(Of MailMessageHeader)

            Public Folder As String

            Public ErrorText As String

            Public FilterNote As String

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Private Class BodyWorkerArgs
            Public Account As MailAccount

            Public Password As String

            Public Folder As String

            Public Uid As ULong

            Public Header As MailMessageHeader

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Private Class BodyWorkerResult
            Public Body As MailMessageBody

            Public Header As MailMessageHeader

            Public ErrorText As String

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Private Class MutationWorkerArgs
            Public Kind As String

            Public Account As MailAccount

            Public Password As String

            Public Folder As String

            Public DestFolder As String

            Public Uids As List(Of ULong)

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Private Class MutationWorkerResult
            Public Kind As String

            Public ErrorText As String

            Public CachePurgedCount As Integer

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Private Class AttachmentWorkerArgs
            Public Account As MailAccount

            Public Password As String

            Public Folder As String

            Public Uid As ULong

            Public PartIndex As Integer

            Public FileName As String

            Public DestPath As String

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Private Class AttachmentWorkerResult
            Public PartIndex As Integer

            Public FileName As String

            Public Folder As String

            Public Uid As ULong

            Public AccountId As String

            Public DestPath As String

            Public ErrorText As String

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Private Class GlobalSearchArgs
            Public Account As MailAccount

            Public Password As String

            Public Query As String

            Public ImapFolderOnly As Boolean

            Public Folder As String

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Private Class GlobalSearchResult
            Public Headers As List(Of MailMessageHeader)

            Public ErrorText As String

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Private Class ThreadContextListItem
            Public ReadOnly Header As MailMessageHeader

            Private ReadOnly caption As String

            Public Sub New(hdr As MailMessageHeader, text As String)
                Header = hdr
                caption = text
            End Sub

            Public Overrides Function ToString() As String
                Return caption
            End Function
        End Class

        Private NotInheritable Class MailFolderPickerDialog
            Inherits LCARSForm
            Private Const RowHeight As Integer = 36

            Private Const RowGap As Integer = 4

            Private Const TopChrome As Integer = 44

            Private Const BottomBar As Integer = 44

            Private ReadOnly folderHost As LcarsScrollPanel

            Private ReadOnly fbCancel As FlatButton

            Private selectedFullName As String

            Public ReadOnly Property SelectedFolderFullName As String
                Get
                    Return selectedFullName
                End Get
            End Property

            Public Sub New(folders As IList(Of MailFolderInfo), excludeFullName As String)
                CType(CObj(Me), Form).Text = "LCARS MOVE"
                CType(CObj(Me), Form).BackColor = Color.Black
                CType(CObj(Me), Control).ForeColor = Color.Orange
                CType(CObj(Me), Form).FormBorderStyle = FormBorderStyle.None
                CType(CObj(Me), Form).ControlBox = False
                Me.DoubleBuffered = True
                CType(Me, LCARSForm).BindToWorkingArea = False
                CType(CObj(Me), Form).StartPosition = FormStartPosition.CenterParent
                Dim clientSize As Size = New Size(420, 480)
                CType(CObj(Me), Form).ClientSize = clientSize
                CType(CObj(Me), Form).ShowInTaskbar = False
                Dim val As FlatButton = New FlatButton()
                CType(val, LCARSbuttonClass).ButtonText = "MOVE TO FOLDER"
                CType(val, LCARSbuttonClass).Text = CType(val, LCARSbuttonClass).ButtonText
                CType(val, LCARSbuttonClass).Clickable = False
                CType(val, LCARSbuttonClass).Color = CType(8, LCARScolorStyles)
                CType(val, LCARSbuttonClass).ButtonTextAlign = ContentAlignment.BottomRight
                clientSize = New Size(180, 32)
                CType(CObj(val), Control).Size = clientSize
                Dim location As Point = New Point(12, 6)
                CType(CObj(val), Control).Location = location
                CType(CObj(Me), Control).Controls.Add(CType(CObj(val), Control))
                folderHost = New LcarsScrollPanel()
                CType(CObj(folderHost), Control).Name = "pnlMoveFolders"
                CType(CObj(folderHost), Control).BackColor = Color.Black
                Dim obj As LcarsScrollPanel = folderHost
                location = New Point(8, 44)
                CType(CObj(obj), Control).Location = location
                Dim obj2 As LcarsScrollPanel = folderHost
                Dim size As Size = New Size(CType(CObj(Me), Form).ClientSize.Width - 16, CType(CObj(Me), Form).ClientSize.Height - 44 - 44)
                CType(CObj(obj2), Control).Size = size
                CType(CObj(Me), Control).Controls.Add(CType(CObj(folderHost), Control))
                fbCancel = New FlatButton()
                CType(fbCancel, LCARSbuttonClass).ButtonText = "CANCEL"
                CType(fbCancel, LCARSbuttonClass).Text = "CANCEL"
                CType(fbCancel, LCARSbuttonClass).Color = CType(9, LCARScolorStyles)
                Dim obj3 As FlatButton = fbCancel
                size = New Size(100, 28)
                CType(CObj(obj3), Control).Size = size
                location = (CSharpImpl.__Assign(CType(CObj(fbCancel), Control).Location, New Point(CType(CObj(Me), Form).ClientSize.Width - CType(CObj(fbCancel), Control).Width - 12, CType(CObj(Me), Form).ClientSize.Height - 36)))
                AddHandler CType(CObj(fbCancel), Control).Click, Sub(a0, a1)
                                                                     Call (Sub() CType(CObj(Me), Form).DialogResult = DialogResult.Cancel)()
                                                                 End Sub
                CType(CObj(Me), Control).Controls.Add(CType(CObj(fbCancel), Control))
                Dim b As String = If(Not Equals(excludeFullName, Nothing), excludeFullName, "").Trim()
                Dim num = 4
                Dim flag = False
                Dim contentPanel As Panel = folderHost.ContentPanel
                Dim width = Math.Max(120, contentPanel.Width - 8)
                If folders IsNot Nothing Then
                    For Each folder In folders
                        If folder Is Nothing Then
                            Continue For
                        End If
                        Dim mailFolderInfo = folder
                        Dim text As String = If(Not Equals(mailFolderInfo.FullName, Nothing), mailFolderInfo.FullName, "").Trim()
                        If text.Length <> 0 AndAlso Not String.Equals(text, b, StringComparison.OrdinalIgnoreCase) Then
                            mailFolderInfo = folder
                            Dim text2 = If(Not Equals(mailFolderInfo.DisplayName, Nothing), mailFolderInfo.DisplayName, text)
                            If String.IsNullOrWhiteSpace(text2) Then
                                text2 = text
                            End If
                            Dim val2 As FlatButton = New FlatButton()
                            CType(val2, LCARSbuttonClass).ButtonText = text2.ToUpperInvariant()
                            CType(val2, LCARSbuttonClass).Text = CType(val2, LCARSbuttonClass).ButtonText
                            CType(val2, LCARSbuttonClass).Clickable = True
                            CType(val2, LCARSbuttonClass).Beeping = True
                            CType(val2, LCARSbuttonClass).ButtonTextAlign = ContentAlignment.MiddleLeft
                            CType(val2, LCARSbuttonClass).ButtonTextHeight = 14
                            CType(val2, LCARSbuttonClass).Color = CType(If(flag, 2, 8), LCARScolorStyles)
                            flag = Not flag
                            CType(CObj(val2), Control).Tag = text
                            location = New Point(2, num)
                            CType(CObj(val2), Control).Location = location
                            size = New Size(width, 36)
                            CType(CObj(val2), Control).Size = size
                            AddHandler CType(CObj(val2), Control).Click, AddressOf OnFolderPickClick
                            contentPanel.Controls.Add(CType(CObj(val2), Control))
                            num += 40
                        End If
                    Next
                End If
                folderHost.Relayout()
            End Sub

            Private Sub OnFolderPickClick(sender As Object, e As EventArgs)
                Dim val As FlatButton = CType(If((TypeOf sender Is FlatButton), sender, Nothing), FlatButton)
                If val IsNot Nothing Then
                    Dim value As String = TryCast(CType(CObj(val), Control).Tag, String)
                    If Not String.IsNullOrWhiteSpace(value) Then
                        selectedFullName = value
                        CType(CObj(Me), Form).DialogResult = DialogResult.OK
                    End If
                End If
            End Sub

            Private Class CSharpImpl
                Shared Function __Assign(Of T)(ByRef target As T, value As T) As T
                    target = value
                    Return value
                End Function
            End Class
        End Class

        Private Const RailLeft As Integer = 8

        Private Const LeftBorder As Integer = 8

        Private Const LeftElbowArm As Integer = 22

        Private Const TopChrome As Integer = 40

        Private Const ModeStripHeight As Integer = 36

        Private Const ToolbarRowHeight As Integer = 32

        Private Const ComposeToolbarGap As Integer = 4

        Private Const BottomPad As Integer = 40

        Private Const PaneSepWidth As Integer = 2

        Private Const PaneGap As Integer = 10

        Private Const FolderPaneWidth As Integer = 200

        Private Const ReadPaneMinWidth As Integer = 220

        Private Const ListPaneMinWidth As Integer = 180

        Private Const RightRailWidth As Integer = 108

        Private Const RightRailBtnH As Integer = 28

        Private Const RightRailGap As Integer = 4

        Private Const RowBtnHeight As Integer = 36

        Private Const RowGap As Integer = 4

        Private Const AccountHeaderHeight As Integer = 30

        Private Const FolderIndent As Integer = 10

        Private Shared ReadOnly PaneSepGold As Color = Color.FromArgb(255, 204, 102)

        Private ReadOnly mailService As UnifiedMailService

        Private ReadOnly sessionPasswords As Dictionary(Of String, String)

        Private ReadOnly accountCollapsed As Dictionary(Of String, Boolean)

        Private ReadOnly folderWorker As BackgroundWorker

        Private ReadOnly headerWorker As BackgroundWorker

        Private ReadOnly bodyWorker As BackgroundWorker

        Private ReadOnly mutationWorker As BackgroundWorker

        Private ReadOnly attachmentWorker As BackgroundWorker

        Private accounts As List(Of MailAccount)

        Private activeAccount As MailAccount

        Private selectedFolderFullName As String

        Private selectedHeader As MailMessageHeader

        Private selectedBody As MailMessageBody

        Private lastFolderSections As List(Of AccountFolderSection)

        Private currentFolderHeaders As List(Of MailMessageHeader)

        Private fbModeMail As FlatButton

        Private fbModeChat As FlatButton

        Private fbModeRss As FlatButton

        Private fbModeCal As FlatButton

        Private fbImport As FlatButton

        Private fbFilters As FlatButton

        Private fbPgp As FlatButton

        Private fbCardDav As FlatButton

        Private fbAbook As FlatButton

        Private fbNews As FlatButton

        Private fbEws As FlatButton

        Private fbCache As FlatButton

        Private fbIndexNow As FlatButton

        Private fbRefresh As FlatButton

        Private currentMode As CommsMode

        Private modeHost As Panel

        Private rssEmbed As frmRssFeeds

        Private chatEmbed As frmChat

        Private calEmbed As frmCalDavSync

        Private unifiedInboxActive As Boolean

        Private Const UnifiedInboxToken As String = "__UNIFIED_INBOX__"

        Private lastImportedProfileDir As String

        Private fbNew As FlatButton

        Private fbReply As FlatButton

        Private fbReplyAll As FlatButton

        Private fbForward As FlatButton

        Private fbDelete As FlatButton

        Private fbMove As FlatButton

        Private fbSearch As FlatButton

        Private fbFacetFrom As FlatButton

        Private fbFacetSubj As FlatButton

        Private fbFacetBody As FlatButton

        Private fbFacetTo As FlatButton

        Private fbFacetUnread As FlatButton

        Private fbFacetCc As FlatButton

        Private fbFacetNewer As FlatButton

        Private fbFacetHasBody As FlatButton

        Private fbFacet30d As FlatButton

        Private fbFacetRead As FlatButton

        Private fbFacetFolder As FlatButton

        Private fbFacetLarger As FlatButton

        Private fbFacet1Y As FlatButton

        Private fbSaveSearch As FlatButton

        Private fbRecallSearch As FlatButton

        Private txtSearch As TextBox

        Private fbClose As FlatButton

        Private lblStatus As Label

        Private elbTop As Elbow

        Private elbBottom As Elbow

        Private fbRailFill As FlatButton

        Private paneSep1 As Panel

        Private paneSep2 As Panel

        Private folderHost As LcarsScrollPanel

        Private messageHost As LcarsScrollPanel

        Private readHost As LcarsScrollPanel

        Private lblFolderEmpty As Label

        Private lblMessageEmpty As Label

        Private lblReadEmpty As Label

        Private lblReadMeta As Label

        Private lblReadSubject As Label

        Private lblReadThread As Label

        Private lstThreadContext As ListBox

        Private bodyLabel As Label

        Private lblReadAttachments As Label

        Private lstReadAttachments As ListBox

        Private fbSaveAttachment As FlatButton

        Private selectedFolderButton As FlatButton

        Private selectedMessageButton As FlatButton

        Private ReadOnly selectedMessages As Dictionary(Of ULong, MailMessageHeader)

        Private ReadOnly messageButtonsByUid As Dictionary(Of ULong, FlatButton)

        Private messageListOrder As List(Of MailMessageHeader)

        Private lastMessageListIndex As Integer

        Private conversationViewEnabled As Boolean

        Private htmlReadPane As MailHtmlReadPane

        Private fbThreads As FlatButton

        Private fbDrafts As FlatButton

        Private ReadOnly expandedThreadKeys As HashSet(Of String)

        Private ReadOnly globalSearchWorker As BackgroundWorker

        Private ReadOnly commsSyncTimer As Windows.Forms.Timer

        Private ReadOnly cardDavLiveTimer As Windows.Forms.Timer

        Private ReadOnly calDavLiveTimer As Windows.Forms.Timer

        Private useImapSearch As Boolean

        Private crossFolderSearchActive As Boolean

        Private lastFtsQuery As String

        Private ftsResultSkip As Integer

        Private ftsLastPageFull As Boolean

        Private Const FtsPageSize As Integer = 5000

        Public Sub New(Optional startupMode As String = Nothing)
            mailService = New UnifiedMailService()
            sessionPasswords = New Dictionary(Of String, String)(StringComparer.Ordinal)
            accountCollapsed = New Dictionary(Of String, Boolean)(StringComparer.Ordinal)
            folderWorker = New BackgroundWorker()
            headerWorker = New BackgroundWorker()
            bodyWorker = New BackgroundWorker()
            mutationWorker = New BackgroundWorker()
            attachmentWorker = New BackgroundWorker()
            currentMode = CommsMode.Mail
            unifiedInboxActive = False
            selectedMessages = New Dictionary(Of ULong, MailMessageHeader)()
            messageButtonsByUid = New Dictionary(Of ULong, FlatButton)()
            messageListOrder = New List(Of MailMessageHeader)()
            lastMessageListIndex = -1
            expandedThreadKeys = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            globalSearchWorker = New BackgroundWorker()
            commsSyncTimer = New Windows.Forms.Timer()
            cardDavLiveTimer = New Windows.Forms.Timer()
            calDavLiveTimer = New Windows.Forms.Timer()
            lastFtsQuery = ""
            ftsResultSkip = 0
            ftsLastPageFull = False
            CType(CObj(Me), Form).Text = "LCARS COMMS"
            CType(CObj(Me), Form).BackColor = Color.Black
            CType(CObj(Me), Control).ForeColor = Color.Orange
            CType(CObj(Me), Form).FormBorderStyle = FormBorderStyle.None
            CType(CObj(Me), Form).ControlBox = False
            Me.DoubleBuffered = True
            CType(Me, LCARSForm).BindToWorkingArea = True
            InitWorkers()
            BuildChrome()
            BuildPanes()
            EnsureModeEmbeds()
            AddHandler CType(CObj(Me), Control).Resize, AddressOf OnMailResize
            AddHandler CType(CObj(Me), Form).Shown, AddressOf OnMailShown
            AddHandler CType(CObj(Me), Form).Activated, AddressOf OnMailActivated
            currentMode = ParseStartupMode(startupMode)
        End Sub

        Private Shared Function ParseStartupMode(raw As String) As CommsMode
            Select Case If(Not Equals(raw, Nothing), raw, "").Trim().ToLowerInvariant()
                Case "chat", "irc"
                    Return CommsMode.Chat
                Case "rss", "feed"
                    Return CommsMode.Rss
                Case "cal", "calendar"
                    Return CommsMode.Calendar
                Case Else
                    Return CommsMode.Mail
            End Select
        End Function

        Private Sub InitWorkers()
            folderWorker.WorkerSupportsCancellation = False
            AddHandler folderWorker.DoWork, AddressOf FolderWorker_DoWork
            AddHandler folderWorker.RunWorkerCompleted, AddressOf FolderWorker_Completed
            headerWorker.WorkerSupportsCancellation = False
            AddHandler headerWorker.DoWork, AddressOf HeaderWorker_DoWork
            AddHandler headerWorker.RunWorkerCompleted, AddressOf HeaderWorker_Completed
            bodyWorker.WorkerSupportsCancellation = False
            AddHandler bodyWorker.DoWork, AddressOf BodyWorker_DoWork
            AddHandler bodyWorker.RunWorkerCompleted, AddressOf Me.BodyWorker_Completed
            mutationWorker.WorkerSupportsCancellation = False
            AddHandler mutationWorker.DoWork, AddressOf MutationWorker_DoWork
            AddHandler mutationWorker.RunWorkerCompleted, AddressOf MutationWorker_Completed
            attachmentWorker.WorkerSupportsCancellation = False
            AddHandler attachmentWorker.DoWork, AddressOf AttachmentWorker_DoWork
            AddHandler attachmentWorker.RunWorkerCompleted, AddressOf AttachmentWorker_Completed
            globalSearchWorker.WorkerSupportsCancellation = False
            AddHandler globalSearchWorker.DoWork, AddressOf GlobalSearchWorker_DoWork
            AddHandler globalSearchWorker.RunWorkerCompleted, AddressOf GlobalSearchWorker_Completed
            commsSyncTimer.Interval = 900000
            AddHandler commsSyncTimer.Tick, AddressOf OnCommsSyncTimerTick
            cardDavLiveTimer.Interval = 15000
            AddHandler cardDavLiveTimer.Tick, AddressOf OnCardDavLiveTimerTick
            CardDavLiveBookCache.EnsureAlwaysOnStarted()
            calDavLiveTimer.Interval = 60000
            AddHandler calDavLiveTimer.Tick, AddressOf OnCalDavLiveTimerTick
        End Sub

        Private Sub OnCommsSyncTimerTick(sender As Object, e As EventArgs)
            QueueBackgroundCommsSync()
            QueueBackgroundSearchIndex()
            If accounts IsNot Nothing AndAlso accounts.Count > 0 AndAlso activeAccount IsNot Nothing AndAlso MailSearchIndex.CountEntriesNeedingBodyPreview(activeAccount.Id) >= 350 Then
                MailSearchBackgroundIndexer.QueueBodyBackfillBurst(accounts, Function(acct) ResolvePassword(acct, promptIfMissing:=False), mailService)
            End If
            If Not String.IsNullOrWhiteSpace(MailSearchBackgroundIndexer.LastRunSummary) Then
                SetStatus(MailSearchBackgroundIndexer.LastRunSummary.ToUpperInvariant())
            End If
        End Sub

        Private Sub OnCardDavLiveTimerTick(sender As Object, e As EventArgs)
            Dim num As Integer = AddressBookStore.CountCardDavDirtyContacts()
            cardDavLiveTimer.Interval = If(num > 0, 4000, 12000)
            Call ThreadPool.QueueUserWorkItem(Sub(state)
                                                  Try
                                                      AddressBookStore.TrySyncStoredCardDavBidirectional()
                                                      CardDavLiveBookCache.QueueBackgroundRefresh()
                                                  Catch projectError As Exception
                                                      ProjectData.SetProjectError(projectError)
                                                      Call ProjectData.ClearProjectError()
                                                  End Try
                                              End Sub)
            If Date.Compare(CardDavLiveBookCache.LastFetchUtc, Date.MinValue) <> 0 Then
                Dim text As String = CardDavLiveBookCache.LastContactCount.ToString()
                Dim text2 As String = " · " & CardDavLiveBookCache.LastAlwaysOnIntervalMs / 1000.ToString() & "s"
                Dim text3 As String = (If((AddressBookStore.CountCardDavDirtyContacts() > 0), " · DIRTY", ""))
                Dim text4 As String = (If(CardDavLiveBookCache.LastBidirectionalPushCount > 0, (" · PUSH " & CardDavLiveBookCache.LastBidirectionalPushCount.ToString()), ""))
                If CardDavSyncService.LastMergedAfter412 AndAlso CardDavSyncService.Last412ConflictEmail.Length > 0 Then
                    text4 = text4 & " · 412 " & CardDavSyncService.Last412ConflictEmail.Trim()
                End If
                If AddressBookStore.LastCardDav412BatchResolved > 0 Then
                    text4 = text4 & " · 412-FIX " & AddressBookStore.LastCardDav412BatchResolved.ToString()
                End If
                If AddressBookStore.LastCardDavSecondaryBooksMerged > 0 Then
                    text4 = text4 & " · 2ND+" & AddressBookStore.LastCardDavSecondaryBooksMerged.ToString()
                End If
                If AddressBookStore.LastCardDavDiscoveredBooks > 0 Then
                    text4 = text4 & " · BOOKS " & AddressBookStore.LastCardDavDiscoveredBooks.ToString()
                End If
                If AddressBookStore.LastCardDavRemotePullCount > 0 Then
                    text4 = text4 & " · REMOTE " & AddressBookStore.LastCardDavRemotePullCount.ToString()
                End If
                If AddressBookStore.LastCardDavPushCount > 0 Then
                    text4 = text4 & " · CARD-PUSH " & AddressBookStore.LastCardDavPushCount.ToString()
                End If
                If String.Equals(CardDavSyncService.ConflictOn412Mode, "ServerWins", StringComparison.OrdinalIgnoreCase) Then
                    text4 += " · 412-SERVER"
                ElseIf String.Equals(CardDavSyncService.ConflictOn412Mode, "ClientMerge", StringComparison.OrdinalIgnoreCase) Then
                    text4 += " · 412-CLIENT"
                End If
                If Not String.IsNullOrWhiteSpace(CardDavLiveBookCache.LastRefreshSummary) Then
                    SetStatus("ABOOK LIVE " & text & " — " & CardDavLiveBookCache.LastRefreshSummary.ToUpperInvariant() & text2 & text3 & text4)
                Else
                    SetStatus("ABOOK LIVE " & text & " @ " & CardDavLiveBookCache.LastFetchUtc.ToLocalTime().ToString("HH:mm:ss") & text2 & text3 & text4)
                End If
            End If
        End Sub

        Private Sub OnCalDavLiveTimerTick(sender As Object, e As EventArgs)
            Dim num As Integer = LcarsCalendarBridge.CountCalDavDirtyEvents()
            calDavLiveTimer.Interval = If(num > 0, 20000, 60000)
            Dim calendarUrl As String = Nothing
            Dim username As String = Nothing
            Dim password As String = Nothing
            If Not LcarsCalendarBridge.TryLoadCalDavSettings(calendarUrl, username, password) Then
                Return
            End If
            Call ThreadPool.QueueUserWorkItem(Sub(state)
                                                  Try
                                                      LcarsCalendarBridge.TrySyncStoredCalDavBidirectional()
                                                  Catch projectError As Exception
                                                      ProjectData.SetProjectError(projectError)
                                                      Call ProjectData.ClearProjectError()
                                                  End Try
                                              End Sub)
            Dim num2 As Integer = EwsCalendarConflictHelper.CountDirtyEwsCalendarEvents()
            If Date.Compare(LcarsCalendarBridge.LastCalDavBidirectionalUtc, Date.MinValue) <> 0 OrElse num > 0 OrElse num2 > 0 Then
                Dim text As String = (If(LcarsCalendarBridge.LastCalDavPrimaryEventsTagged > 0, (" · TAG " & LcarsCalendarBridge.LastCalDavPrimaryEventsTagged.ToString()), ""))
                If LcarsCalendarBridge.LastCalDavSecondaryEventsTagged > 0 Then
                    text = text & " · 2ND-TAG " & LcarsCalendarBridge.LastCalDavSecondaryEventsTagged.ToString()
                End If
                If LcarsCalendarBridge.LastCalDavTaskEventsTagged > 0 Then
                    text = text & " · TASK-TAG " & LcarsCalendarBridge.LastCalDavTaskEventsTagged.ToString()
                End If
                If LcarsCalendarBridge.LastCalDavPullMergeAdded > 0 Then
                    text = text & " · PULL " & LcarsCalendarBridge.LastCalDavPullMergeAdded.ToString()
                End If
                If LcarsCalendarBridge.LastCalDavDiscoveredCalendars > 0 Then
                    text = text & " · CALS " & LcarsCalendarBridge.LastCalDavDiscoveredCalendars.ToString()
                End If
                If LcarsCalendarBridge.LastCalDavSecondaryPullMergeAdded > 0 Then
                    text = text & " · 2ND-PULL " & LcarsCalendarBridge.LastCalDavSecondaryPullMergeAdded.ToString()
                End If
                If LcarsCalendarBridge.LastCalDavUrlLabelsStored > 0 Then
                    text = text & " · PROP " & LcarsCalendarBridge.LastCalDavUrlLabelsStored.ToString()
                End If
                If LcarsCalendarBridge.LastCalDavPrimaryPullMergeAdded > 0 Then
                    text = text & " · 1ST-PULL " & LcarsCalendarBridge.LastCalDavPrimaryPullMergeAdded.ToString()
                End If
                If LcarsCalendarBridge.LastCalDavTaskPullMergeAdded > 0 Then
                    text = text & " · TASK-PULL " & LcarsCalendarBridge.LastCalDavTaskPullMergeAdded.ToString()
                End If
                SetStatus("CAL LIVE · DIRTY " & num.ToString() & " · EWS " & num2.ToString() & " · CAL " & LcarsCalendarBridge.CountDistinctCalendarNames().ToString() & " · EWS-LINK " & LcarsCalendarBridge.CountEventsLinkedToEws().ToString() & " · PUSH " & LcarsCalendarBridge.LastCalDavPushCount.ToString() & text)
            End If
        End Sub

        Private Sub OnMailActivated(sender As Object, e As EventArgs)
            CardDavLiveBookCache.QueueBackgroundRefresh()
            Call ThreadPool.QueueUserWorkItem(Sub(state)
                                                  Try
                                                      LcarsCalendarBridge.TrySyncStoredCalDavBidirectional()
                                                  Catch projectError As Exception
                                                      ProjectData.SetProjectError(projectError)
                                                      Call ProjectData.ClearProjectError()
                                                  End Try
                                              End Sub)
        End Sub

        Private Sub OnIndexNowClick(sender As Object, e As EventArgs)
            QueueBackgroundSearchIndex()
            Dim text = ""
            If activeAccount IsNot Nothing Then
                Dim list As List(Of MailFolderInfo) = FoldersForActiveAccount()
                If list IsNot Nothing AndAlso list.Count > 0 Then
                    text = MailSearchIndexCursor.FormatRotationHint(activeAccount.Id, list.Count)
                End If
                Dim num = MailSearchIndex.CountEntriesNeedingBodyPreview(activeAccount.Id)
                If num > 0 Then
                    text = text & " · BACKFILL " & num.ToString()
                End If
            End If
            SetStatus("GLODA INDEX QUEUED" & If(text.Length > 0, " — " & text, ""))
        End Sub

        Private Sub OnFacetFolderClick(sender As Object, e As EventArgs)
            Dim defaultResponse As String = If(Not Equals(selectedFolderFullName, Nothing), selectedFolderFullName, "INBOX").Trim()
            Dim text = LcarsCommsUi.Prompt("Folder name (Gloda folder: facet):", "LCARS COMMS — GLODA FOLDER", defaultResponse)
            If Not String.IsNullOrWhiteSpace(text) Then
                ApplySearchFacet("folder:" & text.Trim())
            End If
        End Sub

        Private Sub BuildChrome()
            elbTop = New Elbow()
            CType(CObj(elbTop), Control).Name = "elbTop"
            elbTop.ElbowStyle = CType(2, LCARSelbowStyles)
            CType(elbTop, LCARSbuttonClass).Color = CType(5, LCARScolorStyles)
            CType(elbTop, LCARSbuttonClass).ButtonText = ""
            CType(elbTop, LCARSbuttonClass).Text = ""
            elbTop.ButtonWidth = 8
            elbTop.ButtonHeight = 18
            Dim obj As Elbow = elbTop
            Dim size As Size = New Size(30, 36)
            CType(CObj(obj), Control).Size = size
            Dim obj2 As Elbow = elbTop
            Dim location As Point = New Point(8, 4)
            CType(CObj(obj2), Control).Location = location
            CType(elbTop, LCARSbuttonClass).Clickable = False
            CType(CObj(Me), Control).Controls.Add(CType(CObj(elbTop), Control))
            fbRailFill = New FlatButton()
            CType(CObj(fbRailFill), Control).Name = "fbRailFill"
            CType(fbRailFill, LCARSbuttonClass).ButtonText = ""
            CType(fbRailFill, LCARSbuttonClass).Text = ""
            CType(fbRailFill, LCARSbuttonClass).Clickable = False
            CType(fbRailFill, LCARSbuttonClass).Color = CType(5, LCARScolorStyles)
            CType(CObj(fbRailFill), Control).Width = 8
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbRailFill), Control))
            elbBottom = New Elbow()
            CType(CObj(elbBottom), Control).Name = "elbBottom"
            elbBottom.ElbowStyle = CType(3, LCARSelbowStyles)
            CType(elbBottom, LCARSbuttonClass).Color = CType(5, LCARScolorStyles)
            elbBottom.ButtonWidth = 8
            Dim obj3 As Elbow = elbBottom
            size = New Size(30, 36)
            CType(CObj(obj3), Control).Size = size
            CType(elbBottom, LCARSbuttonClass).Clickable = False
            CType(CObj(Me), Control).Controls.Add(CType(CObj(elbBottom), Control))
            fbModeMail = frmMail.MakeToolbarButton("fbModeMail", "MAIL", CType(6, LCARScolorStyles))
            Dim obj4 As FlatButton = fbModeMail
            size = New Size(100, 32)
            CType(CObj(obj4), Control).Size = size
            AddHandler CType(CObj(fbModeMail), Control).Click, Sub(a0, a1)
                                                                   Call (Sub() SetCommsMode(CommsMode.Mail))()
                                                               End Sub
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbModeMail), Control))
            fbModeChat = frmMail.MakeToolbarButton("fbModeChat", "CHAT", CType(2, LCARScolorStyles))
            Dim obj5 As FlatButton = fbModeChat
            size = New Size(100, 32)
            CType(CObj(obj5), Control).Size = size
            AddHandler CType(CObj(fbModeChat), Control).Click, Sub(a0, a1)
                                                                   Call (Sub() SetCommsMode(CommsMode.Chat))()
                                                               End Sub
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbModeChat), Control))
            fbModeRss = frmMail.MakeToolbarButton("fbModeRss", "RSS", CType(2, LCARScolorStyles))
            Dim obj6 As FlatButton = fbModeRss
            size = New Size(100, 32)
            CType(CObj(obj6), Control).Size = size
            AddHandler CType(CObj(fbModeRss), Control).Click, Sub(a0, a1)
                                                                  Call (Sub() SetCommsMode(CommsMode.Rss))()
                                                              End Sub
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbModeRss), Control))
            fbModeCal = frmMail.MakeToolbarButton("fbModeCal", "CALENDAR", CType(2, LCARScolorStyles))
            Dim obj7 As FlatButton = fbModeCal
            size = New Size(120, 32)
            CType(CObj(obj7), Control).Size = size
            AddHandler CType(CObj(fbModeCal), Control).Click, Sub(a0, a1)
                                                                  Call (Sub() SetCommsMode(CommsMode.Calendar))()
                                                              End Sub
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbModeCal), Control))
            modeHost = New Panel()
            modeHost.Name = "pnlModeHost"
            modeHost.BackColor = Color.Black
            modeHost.Visible = False
            CType(CObj(Me), Control).Controls.Add(modeHost)
            fbFilters = frmMail.MakeToolbarButton("fbFilters", "FILTERS", CType(2, LCARScolorStyles))
            Dim obj8 As FlatButton = fbFilters
            size = New Size(96, 28)
            CType(CObj(obj8), Control).Size = size
            AddHandler CType(CObj(fbFilters), Control).Click, AddressOf OnFiltersClick
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbFilters), Control))
            fbPgp = frmMail.MakeToolbarButton("fbPgp", "PGP", CType(1, LCARScolorStyles))
            Dim obj9 As FlatButton = fbPgp
            size = New Size(72, 28)
            CType(CObj(obj9), Control).Size = size
            AddHandler CType(CObj(fbPgp), Control).Click, AddressOf OnPgpClick
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbPgp), Control))
            fbCardDav = frmMail.MakeToolbarButton("fbCardDav", "CARDDAV", CType(8, LCARScolorStyles))
            Dim obj10 As FlatButton = fbCardDav
            size = New Size(96, 28)
            CType(CObj(obj10), Control).Size = size
            AddHandler CType(CObj(fbCardDav), Control).Click, AddressOf OnCardDavClick
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbCardDav), Control))
            fbAbook = frmMail.MakeToolbarButton("fbAbook", "ABOOK", CType(8, LCARScolorStyles))
            Dim obj11 As FlatButton = fbAbook
            size = New Size(72, 28)
            CType(CObj(obj11), Control).Size = size
            AddHandler CType(CObj(fbAbook), Control).Click, AddressOf OnAddressBookClick
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbAbook), Control))
            fbNews = frmMail.MakeToolbarButton("fbNews", "NEWS", CType(8, LCARScolorStyles))
            Dim obj12 As FlatButton = fbNews
            size = New Size(72, 28)
            CType(CObj(obj12), Control).Size = size
            AddHandler CType(CObj(fbNews), Control).Click, AddressOf OnNewsClick
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbNews), Control))
            fbEws = frmMail.MakeToolbarButton("fbEws", "EWS", CType(8, LCARScolorStyles))
            Dim obj13 As FlatButton = fbEws
            size = New Size(56, 28)
            CType(CObj(obj13), Control).Size = size
            AddHandler CType(CObj(fbEws), Control).Click, AddressOf OnEwsClick
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbEws), Control))
            fbCache = frmMail.MakeToolbarButton("fbCache", "CACHE", CType(2, LCARScolorStyles))
            fbIndexNow = frmMail.MakeToolbarButton("fbIndexNow", "INDEX", CType(2, LCARScolorStyles))
            Dim obj14 As FlatButton = fbIndexNow
            size = New Size(72, 28)
            CType(CObj(obj14), Control).Size = size
            AddHandler CType(CObj(fbIndexNow), Control).Click, AddressOf OnIndexNowClick
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbIndexNow), Control))
            Dim obj15 As FlatButton = fbCache
            size = New Size(72, 28)
            CType(CObj(obj15), Control).Size = size
            AddHandler CType(CObj(fbCache), Control).Click, AddressOf OnCacheClick
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbCache), Control))
            fbImport = frmMail.MakeToolbarButton("fbImport", "IMPORT", CType(5, LCARScolorStyles))
            Dim obj16 As FlatButton = fbImport
            size = New Size(80, 28)
            CType(CObj(obj16), Control).Size = size
            AddHandler CType(CObj(fbImport), Control).Click, AddressOf OnImportClick
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbImport), Control))
            fbRefresh = frmMail.MakeToolbarButton("fbRefresh", "REFRESH", CType(1, LCARScolorStyles))
            AddHandler CType(CObj(fbRefresh), Control).Click, AddressOf OnRefreshClick
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbRefresh), Control))
            fbNew = frmMail.MakeToolbarButton("fbNew", "NEW", CType(6, LCARScolorStyles))
            Dim obj17 As FlatButton = fbNew
            size = New Size(88, 28)
            CType(CObj(obj17), Control).Size = size
            AddHandler CType(CObj(fbNew), Control).Click, AddressOf OnNewClick
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbNew), Control))
            fbDrafts = frmMail.MakeToolbarButton("fbDrafts", "DRAFTS", CType(1, LCARScolorStyles))
            Dim obj18 As FlatButton = fbDrafts
            size = New Size(96, 28)
            CType(CObj(obj18), Control).Size = size
            AddHandler CType(CObj(fbDrafts), Control).Click, AddressOf OnDraftsClick
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbDrafts), Control))
            fbReply = frmMail.MakeToolbarButton("fbReply", "REPLY", CType(2, LCARScolorStyles))
            Dim obj19 As FlatButton = fbReply
            size = New Size(88, 28)
            CType(CObj(obj19), Control).Size = size
            AddHandler CType(CObj(fbReply), Control).Click, AddressOf OnReplyClick
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbReply), Control))
            fbReplyAll = frmMail.MakeToolbarButton("fbReplyAll", "REPLY ALL", CType(2, LCARScolorStyles))
            Dim obj20 As FlatButton = fbReplyAll
            size = New Size(108, 28)
            CType(CObj(obj20), Control).Size = size
            AddHandler CType(CObj(fbReplyAll), Control).Click, AddressOf OnReplyAllClick
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbReplyAll), Control))
            fbForward = frmMail.MakeToolbarButton("fbForward", "FORWARD", CType(1, LCARScolorStyles))
            Dim obj21 As FlatButton = fbForward
            size = New Size(100, 28)
            CType(CObj(obj21), Control).Size = size
            AddHandler CType(CObj(fbForward), Control).Click, AddressOf OnForwardClick
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbForward), Control))
            fbDelete = frmMail.MakeToolbarButton("fbDelete", "DELETE", CType(9, LCARScolorStyles))
            Dim obj22 As FlatButton = fbDelete
            size = New Size(96, 28)
            CType(CObj(obj22), Control).Size = size
            AddHandler CType(CObj(fbDelete), Control).Click, AddressOf OnDeleteClick
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbDelete), Control))
            fbMove = frmMail.MakeToolbarButton("fbMove", "MOVE", CType(6, LCARScolorStyles))
            Dim obj23 As FlatButton = fbMove
            size = New Size(88, 28)
            CType(CObj(obj23), Control).Size = size
            AddHandler CType(CObj(fbMove), Control).Click, AddressOf OnMoveClick
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbMove), Control))
            fbThreads = frmMail.MakeToolbarButton("fbThreads", "THREADS", CType(8, LCARScolorStyles))
            Dim obj24 As FlatButton = fbThreads
            size = New Size(100, 28)
            CType(CObj(obj24), Control).Size = size
            AddHandler CType(CObj(fbThreads), Control).Click, AddressOf OnThreadsToggleClick
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbThreads), Control))
            fbSearch = frmMail.MakeToolbarButton("fbSearch", "SEARCH", CType(8, LCARScolorStyles))
            Dim obj25 As FlatButton = fbSearch
            size = New Size(88, 28)
            CType(CObj(obj25), Control).Size = size
            CType(fbSearch, LCARSbuttonClass).Clickable = True
            CType(fbSearch, LCARSbuttonClass).Beeping = True
            AddHandler CType(CObj(fbSearch), Control).Click, AddressOf OnSearchButtonClick
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbSearch), Control))
            txtSearch = New TextBox()
            txtSearch.Name = "txtSearch"
            txtSearch.BorderStyle = BorderStyle.FixedSingle
            txtSearch.BackColor = Color.FromArgb(40, 40, 40)
            txtSearch.ForeColor = Color.Orange
            txtSearch.Font = LcarsUiFont(10F, FontStyle.Regular)
            Dim textBox = txtSearch
            size = New Size(160, 24)
            textBox.Size = size
            AddHandler txtSearch.TextChanged, AddressOf OnSearchTextChanged
            AddHandler txtSearch.Enter, AddressOf OnSearchEnter
            AddHandler txtSearch.KeyDown, AddressOf OnSearchKeyDown
            CType(CObj(Me), Control).Controls.Add(txtSearch)
            fbFacetFrom = frmMail.MakeToolbarButton("fbFacetFrom", "FROM", CType(4, LCARScolorStyles))
            Dim obj26 As FlatButton = fbFacetFrom
            size = New Size(56, 28)
            CType(CObj(obj26), Control).Size = size
            AddHandler CType(CObj(fbFacetFrom), Control).Click, Sub(a0, a1)
                                                                    Call (Sub() ApplySearchFacet("from:"))()
                                                                End Sub
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbFacetFrom), Control))
            fbFacetSubj = frmMail.MakeToolbarButton("fbFacetSubj", "SUBJ", CType(4, LCARScolorStyles))
            Dim obj27 As FlatButton = fbFacetSubj
            size = New Size(56, 28)
            CType(CObj(obj27), Control).Size = size
            AddHandler CType(CObj(fbFacetSubj), Control).Click, Sub(a0, a1)
                                                                    Call (Sub() ApplySearchFacet("subject:"))()
                                                                End Sub
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbFacetSubj), Control))
            fbFacetBody = frmMail.MakeToolbarButton("fbFacetBody", "BODY", CType(4, LCARScolorStyles))
            Dim obj28 As FlatButton = fbFacetBody
            size = New Size(56, 28)
            CType(CObj(obj28), Control).Size = size
            AddHandler CType(CObj(fbFacetBody), Control).Click, Sub(a0, a1)
                                                                    Call (Sub() ApplySearchFacet("body:"))()
                                                                End Sub
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbFacetBody), Control))
            fbFacetTo = frmMail.MakeToolbarButton("fbFacetTo", "TO", CType(4, LCARScolorStyles))
            Dim obj29 As FlatButton = fbFacetTo
            size = New Size(44, 28)
            CType(CObj(obj29), Control).Size = size
            AddHandler CType(CObj(fbFacetTo), Control).Click, Sub(a0, a1)
                                                                  Call (Sub() ApplySearchFacet("to:"))()
                                                              End Sub
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbFacetTo), Control))
            fbFacetUnread = frmMail.MakeToolbarButton("fbFacetUnread", "UNREAD", CType(3, LCARScolorStyles))
            Dim obj30 As FlatButton = fbFacetUnread
            size = New Size(72, 28)
            CType(CObj(obj30), Control).Size = size
            AddHandler CType(CObj(fbFacetUnread), Control).Click, Sub(a0, a1)
                                                                      Call (Sub() ApplySearchFacet("is:unread"))()
                                                                  End Sub
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbFacetUnread), Control))
            fbFacetCc = frmMail.MakeToolbarButton("fbFacetCc", "CC", CType(4, LCARScolorStyles))
            Dim obj31 As FlatButton = fbFacetCc
            size = New Size(44, 28)
            CType(CObj(obj31), Control).Size = size
            AddHandler CType(CObj(fbFacetCc), Control).Click, Sub(a0, a1)
                                                                  Call (Sub() ApplySearchFacet("cc:"))()
                                                              End Sub
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbFacetCc), Control))
            fbFacetNewer = frmMail.MakeToolbarButton("fbFacetNewer", "7D", CType(1, LCARScolorStyles))
            Dim obj32 As FlatButton = fbFacetNewer
            size = New Size(44, 28)
            CType(CObj(obj32), Control).Size = size
            AddHandler CType(CObj(fbFacetNewer), Control).Click, Sub(a0, a1)
                                                                     Call (Sub() ApplySearchFacet("newer:7d"))()
                                                                 End Sub
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbFacetNewer), Control))
            fbFacetHasBody = frmMail.MakeToolbarButton("fbFacetHasBody", "BODY+", CType(1, LCARScolorStyles))
            Dim obj33 As FlatButton = fbFacetHasBody
            size = New Size(56, 28)
            CType(CObj(obj33), Control).Size = size
            AddHandler CType(CObj(fbFacetHasBody), Control).Click, Sub(a0, a1)
                                                                       Call (Sub() ApplySearchFacet("has:body"))()
                                                                   End Sub
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbFacetHasBody), Control))
            fbFacet30d = frmMail.MakeToolbarButton("fbFacet30d", "30D", CType(1, LCARScolorStyles))
            Dim obj34 As FlatButton = fbFacet30d
            size = New Size(44, 28)
            CType(CObj(obj34), Control).Size = size
            AddHandler CType(CObj(fbFacet30d), Control).Click, Sub(a0, a1)
                                                                   Call (Sub() ApplySearchFacet("newer:30d"))()
                                                               End Sub
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbFacet30d), Control))
            fbFacetRead = frmMail.MakeToolbarButton("fbFacetRead", "READ", CType(4, LCARScolorStyles))
            Dim obj35 As FlatButton = fbFacetRead
            size = New Size(52, 28)
            CType(CObj(obj35), Control).Size = size
            AddHandler CType(CObj(fbFacetRead), Control).Click, Sub(a0, a1)
                                                                    Call (Sub() ApplySearchFacet("is:read"))()
                                                                End Sub
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbFacetRead), Control))
            fbFacetFolder = frmMail.MakeToolbarButton("fbFacetFolder", "FLD", CType(1, LCARScolorStyles))
            Dim obj36 As FlatButton = fbFacetFolder
            size = New Size(44, 28)
            CType(CObj(obj36), Control).Size = size
            AddHandler CType(CObj(fbFacetFolder), Control).Click, AddressOf OnFacetFolderClick
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbFacetFolder), Control))
            fbFacetLarger = frmMail.MakeToolbarButton("fbFacetLarger", "BIG", CType(1, LCARScolorStyles))
            Dim obj37 As FlatButton = fbFacetLarger
            size = New Size(44, 28)
            CType(CObj(obj37), Control).Size = size
            AddHandler CType(CObj(fbFacetLarger), Control).Click, Sub(a0, a1)
                                                                      Call (Sub() ApplySearchFacet("larger:100k"))()
                                                                  End Sub
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbFacetLarger), Control))
            fbFacet1Y = frmMail.MakeToolbarButton("fbFacet1Y", "1Y", CType(1, LCARScolorStyles))
            Dim obj38 As FlatButton = fbFacet1Y
            size = New Size(40, 28)
            CType(CObj(obj38), Control).Size = size
            AddHandler CType(CObj(fbFacet1Y), Control).Click, Sub(a0, a1)
                                                                  Call (Sub() ApplySearchFacet("newer:365d"))()
                                                              End Sub
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbFacet1Y), Control))
            Dim val As FlatButton = frmMail.MakeToolbarButton("fbFacet90d", "90D", CType(1, LCARScolorStyles))
            size = New Size(44, 28)
            CType(CObj(val), Control).Size = size
            AddHandler CType(CObj(val), Control).Click, Sub(a0, a1)
                                                            Call (Sub() ApplySearchFacet("newer:90d"))()
                                                        End Sub
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val), Control))
            Dim val2 As FlatButton = frmMail.MakeToolbarButton("fbFacet6M", "6M", CType(1, LCARScolorStyles))
            size = New Size(40, 28)
            CType(CObj(val2), Control).Size = size
            AddHandler CType(CObj(val2), Control).Click, Sub(a0, a1)
                                                             Call (Sub() ApplySearchFacet("newer:180d"))()
                                                         End Sub
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val2), Control))
            Dim val3 As FlatButton = frmMail.MakeToolbarButton("fbFacetSmall", "SML", CType(1, LCARScolorStyles))
            size = New Size(44, 28)
            CType(CObj(val3), Control).Size = size
            AddHandler CType(CObj(val3), Control).Click, Sub(a0, a1)
                                                             Call (Sub() ApplySearchFacet("smaller:50k"))()
                                                         End Sub
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val3), Control))
            Dim val4 As FlatButton = frmMail.MakeToolbarButton("fbFacet2W", "2W", CType(1, LCARScolorStyles))
            size = New Size(40, 28)
            CType(CObj(val4), Control).Size = size
            AddHandler CType(CObj(val4), Control).Click, Sub(a0, a1)
                                                             Call (Sub() ApplySearchFacet("newer:14d"))()
                                                         End Sub
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val4), Control))
            Dim val5 As FlatButton = frmMail.MakeToolbarButton("fbFacetReply", "RPLY", CType(4, LCARScolorStyles))
            size = New Size(48, 28)
            CType(CObj(val5), Control).Size = size
            AddHandler CType(CObj(val5), Control).Click, Sub(a0, a1)
                                                             Call (Sub() ApplySearchFacet("replyto:"))()
                                                         End Sub
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val5), Control))
            fbSaveSearch = frmMail.MakeToolbarButton("fbSaveSearch", "SAVE", CType(2, LCARScolorStyles))
            Dim obj39 As FlatButton = fbSaveSearch
            size = New Size(52, 28)
            CType(CObj(obj39), Control).Size = size
            AddHandler CType(CObj(fbSaveSearch), Control).Click, AddressOf OnSaveGlodaSearchClick
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbSaveSearch), Control))
            fbRecallSearch = frmMail.MakeToolbarButton("fbRecallSearch", "RECALL", CType(1, LCARScolorStyles))
            Dim obj40 As FlatButton = fbRecallSearch
            size = New Size(64, 28)
            CType(CObj(obj40), Control).Size = size
            AddHandler CType(CObj(fbRecallSearch), Control).Click, AddressOf OnRecallGlodaSearchClick
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbRecallSearch), Control))
            lblStatus = New Label()
            lblStatus.Name = "lblStatus"
            lblStatus.AutoSize = False
            lblStatus.TextAlign = ContentAlignment.MiddleLeft
            lblStatus.ForeColor = Color.Orange
            lblStatus.BackColor = Color.Black
            lblStatus.Font = LcarsUiFont(10F)
            lblStatus.Text = "READY"
            CType(CObj(Me), Control).Controls.Add(lblStatus)
            fbClose = New FlatButton()
            CType(CObj(fbClose), Control).Name = "fbClose"
            CType(fbClose, LCARSbuttonClass).ButtonText = "CLOSE"
            CType(fbClose, LCARSbuttonClass).Text = "CLOSE"
            CType(fbClose, LCARSbuttonClass).Color = CType(9, LCARScolorStyles)
            Dim obj41 As FlatButton = fbClose
            size = New Size(100, 28)
            CType(CObj(obj41), Control).Size = size
            AddHandler CType(CObj(fbClose), Control).Click, AddressOf OnCloseClick
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbClose), Control))
        End Sub

        Private Sub BuildPanes()
            folderHost = New LcarsScrollPanel()
            CType(CObj(folderHost), Control).Name = "pnlFolders"
            CType(CObj(folderHost), Control).BackColor = Color.Black
            folderHost.CollapseTrackWhenIdle = True
            CType(CObj(Me), Control).Controls.Add(CType(CObj(folderHost), Control))
            paneSep1 = New Panel()
            paneSep1.Name = "paneSep1"
            paneSep1.BackColor = PaneSepGold
            paneSep1.Width = 2
            CType(CObj(Me), Control).Controls.Add(paneSep1)
            messageHost = New LcarsScrollPanel()
            CType(CObj(messageHost), Control).Name = "pnlMessages"
            CType(CObj(messageHost), Control).BackColor = Color.Black
            messageHost.CollapseTrackWhenIdle = True
            CType(CObj(Me), Control).Controls.Add(CType(CObj(messageHost), Control))
            paneSep2 = New Panel()
            paneSep2.Name = "paneSep2"
            paneSep2.BackColor = PaneSepGold
            paneSep2.Width = 2
            CType(CObj(Me), Control).Controls.Add(paneSep2)
            readHost = New LcarsScrollPanel()
            CType(CObj(readHost), Control).Name = "pnlReading"
            CType(CObj(readHost), Control).BackColor = Color.Black
            readHost.CollapseTrackWhenIdle = True
            CType(CObj(Me), Control).Controls.Add(CType(CObj(readHost), Control))
            lblFolderEmpty = MakeEmptyLabel("SELECT REFRESH OR IMPORT")
            folderHost.ContentPanel.Controls.Add(lblFolderEmpty)
            lblMessageEmpty = MakeEmptyLabel("SELECT A FOLDER")
            messageHost.ContentPanel.Controls.Add(lblMessageEmpty)
            lblReadEmpty = MakeEmptyLabel("SELECT A MESSAGE")
            readHost.ContentPanel.Controls.Add(lblReadEmpty)
            lblReadMeta = New Label()
            lblReadMeta.AutoSize = True
            Dim label = lblReadMeta
            Dim maximumSize As Size = New Size(400, 0)
            label.MaximumSize = maximumSize
            lblReadMeta.ForeColor = Color.FromArgb(200, 140, 60)
            lblReadMeta.BackColor = Color.Black
            lblReadMeta.Font = LcarsUiFont(9F)
            lblReadMeta.Visible = False
            readHost.ContentPanel.Controls.Add(lblReadMeta)
            lblReadSubject = New Label()
            lblReadSubject.AutoSize = True
            Dim label2 = lblReadSubject
            maximumSize = New Size(400, 0)
            label2.MaximumSize = maximumSize
            lblReadSubject.ForeColor = Color.Orange
            lblReadSubject.BackColor = Color.Black
            lblReadSubject.Font = LcarsUiFont(11F)
            lblReadSubject.Visible = False
            readHost.ContentPanel.Controls.Add(lblReadSubject)
            lblReadThread = New Label()
            lblReadThread.AutoSize = True
            lblReadThread.ForeColor = Color.FromArgb(200, 140, 60)
            lblReadThread.BackColor = Color.Black
            lblReadThread.Font = LcarsUiFont(9F)
            lblReadThread.Text = "THREAD"
            lblReadThread.Visible = False
            readHost.ContentPanel.Controls.Add(lblReadThread)
            lstThreadContext = New ListBox()
            lstThreadContext.BorderStyle = BorderStyle.FixedSingle
            lstThreadContext.BackColor = Color.FromArgb(40, 40, 40)
            lstThreadContext.ForeColor = Color.White
            lstThreadContext.Font = LcarsUiFont(8.5F, FontStyle.Regular)
            lstThreadContext.Visible = False
            AddHandler lstThreadContext.SelectedIndexChanged, AddressOf Me.OnThreadContextSelected
            readHost.ContentPanel.Controls.Add(lstThreadContext)
            bodyLabel = New Label()
            bodyLabel.AutoSize = True
            Dim label3 = bodyLabel
            maximumSize = New Size(400, 0)
            label3.MaximumSize = maximumSize
            bodyLabel.ForeColor = Color.Orange
            bodyLabel.BackColor = Color.Black
            bodyLabel.Font = New Font("Consolas", 9.5F, FontStyle.Regular)
            bodyLabel.Visible = False
            readHost.ContentPanel.Controls.Add(bodyLabel)
            htmlReadPane = New MailHtmlReadPane()
            htmlReadPane.Visible = False
            readHost.ContentPanel.Controls.Add(htmlReadPane)
            lblReadAttachments = New Label()
            lblReadAttachments.AutoSize = True
            lblReadAttachments.ForeColor = Color.FromArgb(200, 140, 60)
            lblReadAttachments.BackColor = Color.Black
            lblReadAttachments.Font = LcarsUiFont(9F)
            lblReadAttachments.Text = "ATTACHMENTS"
            lblReadAttachments.Visible = False
            readHost.ContentPanel.Controls.Add(lblReadAttachments)
            lstReadAttachments = New ListBox()
            lstReadAttachments.BorderStyle = BorderStyle.FixedSingle
            lstReadAttachments.BackColor = Color.FromArgb(40, 40, 40)
            lstReadAttachments.ForeColor = Color.White
            lstReadAttachments.Font = LcarsUiFont(9F, FontStyle.Regular)
            lstReadAttachments.Visible = False
            readHost.ContentPanel.Controls.Add(lstReadAttachments)
            fbSaveAttachment = frmMail.MakeToolbarButton("fbSaveAttachment", "SAVE AS", CType(6, LCARScolorStyles))
            Dim obj As FlatButton = fbSaveAttachment
            maximumSize = New Size(100, 28)
            CType(CObj(obj), Control).Size = maximumSize
            CType(CObj(fbSaveAttachment), Control).Visible = False
            AddHandler CType(CObj(fbSaveAttachment), Control).Click, AddressOf OnSaveAttachmentClick
            readHost.ContentPanel.Controls.Add(CType(CObj(fbSaveAttachment), Control))
            Call CType(CObj(folderHost), Control).SendToBack()
            Call CType(CObj(messageHost), Control).SendToBack()
            Call CType(CObj(readHost), Control).SendToBack()
            If paneSep1 IsNot Nothing Then
                paneSep1.BringToFront()
            End If
            If paneSep2 IsNot Nothing Then
                paneSep2.BringToFront()
            End If
            Call CType(CObj(fbClose), Control).BringToFront()
            lblStatus.BringToFront()
            Call CType(CObj(fbImport), Control).BringToFront()
            If fbFilters IsNot Nothing Then
                Call CType(CObj(fbFilters), Control).BringToFront()
            End If
            If fbPgp IsNot Nothing Then
                Call CType(CObj(fbPgp), Control).BringToFront()
            End If
            Call CType(CObj(fbRefresh), Control).BringToFront()
            Call CType(CObj(fbNew), Control).BringToFront()
            Call CType(CObj(fbReply), Control).BringToFront()
            Call CType(CObj(fbReplyAll), Control).BringToFront()
            Call CType(CObj(fbForward), Control).BringToFront()
            Call CType(CObj(fbDelete), Control).BringToFront()
            Call CType(CObj(fbMove), Control).BringToFront()
            Call CType(CObj(fbSearch), Control).BringToFront()
            If fbFacetFrom IsNot Nothing Then
                Call CType(CObj(fbFacetFrom), Control).BringToFront()
            End If
            If fbFacetSubj IsNot Nothing Then
                Call CType(CObj(fbFacetSubj), Control).BringToFront()
            End If
            If fbFacetBody IsNot Nothing Then
                Call CType(CObj(fbFacetBody), Control).BringToFront()
            End If
            If fbFacetTo IsNot Nothing Then
                Call CType(CObj(fbFacetTo), Control).BringToFront()
            End If
            If fbFacetUnread IsNot Nothing Then
                Call CType(CObj(fbFacetUnread), Control).BringToFront()
            End If
            If fbFacetCc IsNot Nothing Then
                Call CType(CObj(fbFacetCc), Control).BringToFront()
            End If
            If fbFacetNewer IsNot Nothing Then
                Call CType(CObj(fbFacetNewer), Control).BringToFront()
            End If
            If fbFacetHasBody IsNot Nothing Then
                Call CType(CObj(fbFacetHasBody), Control).BringToFront()
            End If
            If fbFacet30d IsNot Nothing Then
                Call CType(CObj(fbFacet30d), Control).BringToFront()
            End If
            If fbFacetRead IsNot Nothing Then
                Call CType(CObj(fbFacetRead), Control).BringToFront()
            End If
            If fbFacetFolder IsNot Nothing Then
                Call CType(CObj(fbFacetFolder), Control).BringToFront()
            End If
            If fbFacetLarger IsNot Nothing Then
                Call CType(CObj(fbFacetLarger), Control).BringToFront()
            End If
            If fbFacet1Y IsNot Nothing Then
                Call CType(CObj(fbFacet1Y), Control).BringToFront()
            End If
            If fbSaveSearch IsNot Nothing Then
                Call CType(CObj(fbSaveSearch), Control).BringToFront()
            End If
            If fbRecallSearch IsNot Nothing Then
                Call CType(CObj(fbRecallSearch), Control).BringToFront()
            End If
            txtSearch.BringToFront()
            If elbTop IsNot Nothing Then
                Call CType(CObj(elbTop), Control).BringToFront()
            End If
            If fbRailFill IsNot Nothing Then
                Call CType(CObj(fbRailFill), Control).BringToFront()
            End If
            If elbBottom IsNot Nothing Then
                Call CType(CObj(elbBottom), Control).BringToFront()
            End If
            UpdateComposeButtonsEnabled()
        End Sub

        Private Shared Function MakeToolbarButton(name As String, caption As String, color As LCARScolorStyles) As FlatButton
            Dim val As FlatButton = New FlatButton()
            CType(CObj(val), Control).Name = name
            CType(val, LCARSbuttonClass).ButtonText = caption
            CType(val, LCARSbuttonClass).Text = caption
            CType(val, LCARSbuttonClass).Clickable = True
            CType(val, LCARSbuttonClass).Beeping = True
            CType(val, LCARSbuttonClass).Color = color
            CType(val, LCARSbuttonClass).ButtonTextAlign = ContentAlignment.BottomRight
            CType(val, LCARSbuttonClass).ButtonTextHeight = 12
            Dim size As Size = New Size(140, 28)
            CType(CObj(val), Control).Size = size
            Return val
        End Function

        Private Shared Function MakeEmptyLabel(text As String) As Label
            Dim label As Label = New Label()
            label.AutoSize = False
            label.TextAlign = ContentAlignment.MiddleCenter
            label.ForeColor = Color.FromArgb(160, 100, 40)
            label.BackColor = Color.Black
            label.Font = LcarsUiFont(10F)
            label.Text = text.ToUpperInvariant()
            Return label
        End Function

        Private Shared Function LcarsUiFont(size As Single, Optional style As FontStyle = FontStyle.Bold) As Font
            Dim result As Font
            Try
                result = New Font("Arial Narrow", size, style, GraphicsUnit.Point)
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                result = New Font("Segoe UI", size, style, GraphicsUnit.Point)
                Call ProjectData.ClearProjectError()
            End Try
            Return result
        End Function

        Protected Overrides Sub OnShellChromeLayout()
            PlaceShellAlignedCloseButton(CType(CObj(fbClose), Control), 8)
            LayoutContent()
        End Sub

        Private Sub OnMailResize(sender As Object, e As EventArgs)
            LayoutContent()
        End Sub

        Private Sub OnMailShown(sender As Object, e As EventArgs)
            LayoutContent()
            If Not commsSyncTimer.Enabled Then
                commsSyncTimer.Start()
            End If
            If Not cardDavLiveTimer.Enabled Then
                cardDavLiveTimer.Start()
            End If
            If Not calDavLiveTimer.Enabled Then
                calDavLiveTimer.Start()
            End If
            CardDavLiveBookCache.QueueBackgroundRefresh()
            Call ThreadPool.QueueUserWorkItem(Sub(state)
                                                  Try
                                                      AddressBookStore.TrySyncStoredCardDavBidirectional()
                                                      CardDavLiveBookCache.QueueBackgroundRefresh()
                                                  Catch projectError As Exception
                                                      ProjectData.SetProjectError(projectError)
                                                      Call ProjectData.ClearProjectError()
                                                  End Try
                                              End Sub)
            LoadAccountsFromStore()
            QueueBackgroundSearchIndex()
            If activeAccount IsNot Nothing AndAlso MailSearchIndex.CountEntriesNeedingBodyPreview(activeAccount.Id) > 8000 Then
                QueueBackgroundSearchIndex()
            End If
            SetCommsMode(currentMode)
            If currentMode = CommsMode.Mail Then
                If accounts Is Nothing OrElse accounts.Count = 0 Then
                    SetStatus("IMPORT A THUNDERBIRD PROFILE")
                    ShowEmptyStates()
                Else
                    SetStatus("LOADING FOLDERS…")
                    StartRefreshFolders()
                End If
            End If
        End Sub

        Private Sub LayoutContent()
            Dim num = 44
            Dim flag As Boolean
            Dim num8 As Integer
            Dim num9 As Integer
            Dim num10 As Integer
            Dim num11 As Integer
            Dim num12 As Integer
            Dim num13 As Integer
            Dim num2 = Math.Max(num + 200, CType(CObj(Me), Form).ClientSize.Width - 8 - 108)
            Dim location As Point
            If elbTop IsNot Nothing Then
                Dim obj As Elbow = elbTop
                location = New Point(8, 4)
                CType(CObj(obj), Control).Location = location
                Dim obj2 As Elbow = elbTop
                Dim size As Size = New Size(30, 36)
                CType(CObj(obj2), Control).Size = size
                CType(elbTop, LCARSbuttonClass).ButtonText = ""
                CType(elbTop, LCARSbuttonClass).Text = ""
            End If
            flag = currentMode = CommsMode.Mail
            Dim num3 = 42
            Dim x = num
            x = Me.PlaceTopTool(fbModeMail, x, num3, 8)
            x = Me.PlaceTopTool(fbModeChat, x, num3, 8)
            x = Me.PlaceTopTool(fbModeRss, x, num3, 8)
            x = Me.PlaceTopTool(fbModeCal, x, num3, 8)
            Dim num4 = num3 + 36 + 4
            Dim x2 = num
            If flag Then
                x2 = Me.PlaceTopTool(fbFilters, x2, num4, 6)
                x2 = Me.PlaceTopTool(fbPgp, x2, num4, 6)
                x2 = Me.PlaceTopTool(fbAbook, x2, num4, 6)
                x2 = Me.PlaceTopTool(fbCardDav, x2, num4, 6)
                x2 = Me.PlaceTopTool(fbNews, x2, num4, 6)
                x2 = Me.PlaceTopTool(fbEws, x2, num4, 6)
                x2 = Me.PlaceTopTool(fbIndexNow, x2, num4, 6)
                x2 = Me.PlaceTopTool(fbCache, x2, num4, 6)
                x2 = Me.PlaceTopTool(fbImport, x2, num4, 6)
            End If
            SetMailToolVisible(flag)
            Dim num5 = num4 + 32 + 4
            Dim x3 = num
            If flag Then
                x3 = Me.PlaceTopTool(fbSearch, x3, num5, 6)
                x3 = Me.PlaceTopTool(fbFacetFrom, x3, num5, 4)
                x3 = Me.PlaceTopTool(fbFacetSubj, x3, num5, 4)
                x3 = Me.PlaceTopTool(fbFacetBody, x3, num5, 4)
                x3 = Me.PlaceTopTool(fbFacetTo, x3, num5, 4)
                x3 = Me.PlaceTopTool(fbFacetUnread, x3, num5, 4)
                x3 = Me.PlaceTopTool(fbFacetCc, x3, num5, 4)
                x3 = Me.PlaceTopTool(fbFacetNewer, x3, num5, 4)
                x3 = Me.PlaceTopTool(fbFacetHasBody, x3, num5, 4)
                x3 = Me.PlaceTopTool(fbFacet30d, x3, num5, 4)
                x3 = Me.PlaceTopTool(fbFacetRead, x3, num5, 4)
                x3 = Me.PlaceTopTool(fbFacetFolder, x3, num5, 4)
                x3 = Me.PlaceTopTool(fbFacetLarger, x3, num5, 4)
                x3 = Me.PlaceTopTool(fbFacet1Y, x3, num5, 4)
                x3 = Me.PlaceTopTool(fbSaveSearch, x3, num5, 4)
                x3 = Me.PlaceTopTool(fbRecallSearch, x3, num5, 6)
                If txtSearch IsNot Nothing Then
                    txtSearch.Visible = True
                    location = (CSharpImpl.__Assign(txtSearch.Location, New Point(x3, num5 + 2)))
                    txtSearch.Width = Math.Max(80, num2 - 12 - x3)
                End If
            ElseIf txtSearch IsNot Nothing Then
                txtSearch.Visible = False
            End If
            SetMailFacetVisible(flag)
            PlaceShellAlignedCloseButton(CType(CObj(fbClose), Control), 8)
            If fbClose IsNot Nothing Then
                CType(CObj(fbClose), Control).Width = 108
                CType(CObj(fbClose), Control).Left = num2
            End If
            Dim y = 42
            Dim stepY = 32
            If flag Then
                y = Me.PlaceRightRail(fbNew, num2, y, stepY)
                y = Me.PlaceRightRail(fbDrafts, num2, y, stepY)
                y = Me.PlaceRightRail(fbReply, num2, y, stepY)
                y = Me.PlaceRightRail(fbReplyAll, num2, y, stepY)
                y = Me.PlaceRightRail(fbForward, num2, y, stepY)
                y = Me.PlaceRightRail(fbDelete, num2, y, stepY)
                y = Me.PlaceRightRail(fbMove, num2, y, stepY)
                y = Me.PlaceRightRail(fbThreads, num2, y, stepY)
                y = Me.PlaceRightRail(fbRefresh, num2, y, stepY)
            End If
            SetMailRailVisible(flag)
            If lblStatus IsNot Nothing Then
                Dim y2 = If(flag, num5 + 32 + 2, num4 + 4)
                location = (CSharpImpl.__Assign(lblStatus.Location, New Point(num, y2)))
                Dim label = lblStatus
                Dim size As Size = New Size(Math.Max(40, num2 - num - 8), 20)
                label.Size = size
            End If
            If elbBottom IsNot Nothing Then
                location = (CSharpImpl.__Assign(CType(CObj(elbBottom), Control).Location, New Point(8, CType(CObj(Me), Form).ClientSize.Height - CType(CObj(elbBottom), Control).Height - 4)))
                Dim obj3 As Elbow = elbBottom
                Dim size As Size = New Size(30, 36)
                CType(CObj(obj3), Control).Size = size
            End If
            If fbRailFill IsNot Nothing Then
                Dim num6 = 40
                Dim num7 = If(elbBottom Is Nothing, CType(CObj(Me), Form).ClientSize.Height - 40, CType(CObj(elbBottom), Control).Top)
                Dim obj4 As FlatButton = fbRailFill
                location = New Point(8, num6)
                CType(CObj(obj4), Control).Location = location
                Dim obj5 As FlatButton = fbRailFill
                Dim size As Size = New Size(8, Math.Max(20, num7 - num6))
                CType(CObj(obj5), Control).Size = size
            End If
            num8 = If(flag, num5 + 32 + 4 + 22, num3 + 36 + 8)
            num9 = Math.Max(80, CType(CObj(Me), Form).ClientSize.Height - num8 - 40)
            num10 = num
            num11 = num2 - 8
            If modeHost IsNot Nothing Then
                Dim panel = modeHost
                location = New Point(num10, num8)
                panel.Location = location
                Dim panel2 = modeHost
                Dim size As Size = New Size(Math.Max(100, num11 - num10), num9)
                panel2.Size = size
                modeHost.Visible = Not flag
                modeHost.BringToFront()
            End If
            If folderHost Is Nothing OrElse messageHost Is Nothing OrElse readHost Is Nothing Then
                Return
            End If
            num12 = Math.Max(240, num11 - num10)
            num13 = 24
            Dim num14 As Integer = Math.Min(200, Math.Max(140, num12 / 5))
            Dim num15 As Integer = Math.Max(220, num12 / 3)
            Dim num16 = num12 - num14 - num15 - num13
            If num16 < 180 Then
                Dim num17 = 180 - num16
                num15 = Math.Max(160, num15 - num17)
                num16 = num12 - num14 - num15 - num13
            End If
            If num16 < 120 Then
                num16 = 120
            End If
            CType(CObj(folderHost), Control).Visible = flag
            CType(CObj(messageHost), Control).Visible = flag
            CType(CObj(readHost), Control).Visible = flag
            If paneSep1 IsNot Nothing Then
                paneSep1.Visible = flag
            End If
            If paneSep2 IsNot Nothing Then
                paneSep2.Visible = flag
            End If
            If flag Then
                Dim obj6 As LcarsScrollPanel = folderHost
                Dim location As Point = New Point(num10, num8)
                CType(CObj(obj6), Control).Location = location
                Dim obj7 As LcarsScrollPanel = folderHost
                Dim size As Size = New Size(num14, num9)
                CType(CObj(obj7), Control).Size = size
                If paneSep1 IsNot Nothing Then
                    Dim panel3 = paneSep1
                    location = New Point(CType(CObj(folderHost), Control).Right + 5, num8 + 4)
                    panel3.Location = location
                    Dim panel4 = paneSep1
                    size = New Size(2, Math.Max(40, num9 - 8))
                    panel4.Size = size
                End If
                Dim obj8 As LcarsScrollPanel = messageHost
                location = New Point(CType(CObj(folderHost), Control).Right + 10 + 2, num8)
                CType(CObj(obj8), Control).Location = location
                Dim obj9 As LcarsScrollPanel = messageHost
                size = New Size(num16, num9)
                CType(CObj(obj9), Control).Size = size
                If paneSep2 IsNot Nothing Then
                    Dim panel5 = paneSep2
                    location = New Point(CType(CObj(messageHost), Control).Right + 5, num8 + 4)
                    panel5.Location = location
                    Dim panel6 = paneSep2
                    size = New Size(2, Math.Max(40, num9 - 8))
                    panel6.Size = size
                End If
                Dim obj10 As LcarsScrollPanel = readHost
                location = New Point(CType(CObj(messageHost), Control).Right + 10 + 2, num8)
                CType(CObj(obj10), Control).Location = location
                Dim obj11 As LcarsScrollPanel = readHost
                size = New Size(Math.Max(120, num11 - CType(CObj(readHost), Control).Left), num9)
                CType(CObj(obj11), Control).Size = size
                frmMail.LayoutEmptyLabel(lblFolderEmpty, folderHost)
                frmMail.LayoutEmptyLabel(lblMessageEmpty, messageHost)
                frmMail.LayoutEmptyLabel(lblReadEmpty, readHost)
                Dim width As Integer = Math.Max(100, readHost.ContentPanel.Width - 12)
                Dim label2 = lblReadMeta
                size = New Size(width, 0)
                label2.MaximumSize = size
                Dim label3 = lblReadSubject
                size = New Size(width, 0)
                label3.MaximumSize = size
                Dim label4 = bodyLabel
                size = New Size(width, 0)
                label4.MaximumSize = size
                If htmlReadPane IsNot Nothing Then
                    htmlReadPane.Width = width
                End If
                If lblReadAttachments IsNot Nothing Then
                    Dim label5 = lblReadAttachments
                    size = New Size(width, 0)
                    label5.MaximumSize = size
                End If
                If lstReadAttachments IsNot Nothing Then
                    lstReadAttachments.Width = width
                End If
                folderHost.Relayout()
                messageHost.Relayout()
                readHost.Relayout()
            End If
        End Sub

        Private Sub SetMailToolVisible(visible As Boolean)
            Dim array = New FlatButton(8) {fbFilters, fbPgp, fbAbook, fbCardDav, fbNews, fbEws, fbIndexNow, fbCache, fbImport}
            For Each val As FlatButton In array
                If val IsNot Nothing Then
                    CType(CObj(val), Control).Visible = visible
                End If
            Next
        End Sub

        Private Sub SetMailFacetVisible(visible As Boolean)
            Dim array = New FlatButton(15) {fbSearch, fbFacetFrom, fbFacetSubj, fbFacetBody, fbFacetTo, fbFacetUnread, fbFacetCc, fbFacetNewer, fbFacetHasBody, fbFacet30d, fbFacetRead, fbFacetFolder, fbFacetLarger, fbFacet1Y, fbSaveSearch, fbRecallSearch}
            For Each val As FlatButton In array
                If val IsNot Nothing Then
                    CType(CObj(val), Control).Visible = visible
                End If
            Next
        End Sub

        Private Sub SetMailRailVisible(visible As Boolean)
            Dim array = New FlatButton(8) {fbNew, fbDrafts, fbReply, fbReplyAll, fbForward, fbDelete, fbMove, fbThreads, fbRefresh}
            For Each val As FlatButton In array
                If val IsNot Nothing Then
                    CType(CObj(val), Control).Visible = visible
                End If
            Next
        End Sub

        Private Sub EnsureModeEmbeds()
            If modeHost IsNot Nothing Then
                If rssEmbed Is Nothing Then
                    rssEmbed = New frmRssFeeds()
                    EmbedChildForm(CType(CObj(rssEmbed), Form))
                End If
                If chatEmbed Is Nothing Then
                    chatEmbed = New frmChat()
                    EmbedChildForm(CType(CObj(chatEmbed), Form))
                End If
                If calEmbed Is Nothing Then
                    calEmbed = New frmCalDavSync()
                    EmbedChildForm(CType(CObj(calEmbed), Form))
                End If
            End If
        End Sub

        Private Sub EmbedChildForm(child As Form)
            If child IsNot Nothing AndAlso modeHost IsNot Nothing Then
                child.TopLevel = False
                child.FormBorderStyle = FormBorderStyle.None
                child.ControlBox = False
                Dim val As LCARSForm = CType(CObj(If((TypeOf child Is LCARSForm), child, Nothing)), LCARSForm)
                If val IsNot Nothing Then
                    val.BindToWorkingArea = False
                End If
                child.Dock = DockStyle.Fill
                child.Visible = False
                modeHost.Controls.Add(child)
                child.Show()
            End If
        End Sub

        Private Sub SetCommsMode(mode As CommsMode)
            currentMode = mode
            EnsureModeEmbeds()
            CType(fbModeMail, LCARSbuttonClass).Color = CType(If(mode = CommsMode.Mail, 6, 2), LCARScolorStyles)
            CType(fbModeChat, LCARSbuttonClass).Color = CType(If(mode = CommsMode.Chat, 6, 2), LCARScolorStyles)
            CType(fbModeRss, LCARSbuttonClass).Color = CType(If(mode = CommsMode.Rss, 6, 2), LCARScolorStyles)
            CType(fbModeCal, LCARSbuttonClass).Color = CType(If(mode = CommsMode.Calendar, 6, 2), LCARScolorStyles)
            If rssEmbed IsNot Nothing Then
                CType(CObj(rssEmbed), Control).Visible = mode = CommsMode.Rss
            End If
            If chatEmbed IsNot Nothing Then
                CType(CObj(chatEmbed), Control).Visible = mode = CommsMode.Chat
            End If
            If calEmbed IsNot Nothing Then
                CType(CObj(calEmbed), Control).Visible = mode = CommsMode.Calendar
            End If
            LayoutContent()
            Select Case mode
                Case CommsMode.Mail
                    SetStatus(If(accounts IsNot Nothing AndAlso accounts.Count > 0, "MAIL", "IMPORT A THUNDERBIRD PROFILE"))
                Case CommsMode.Chat
                    SetStatus("CHAT")
                Case CommsMode.Rss
                    SetStatus("RSS")
                Case Else
                    SetStatus("CALENDAR")
            End Select
        End Sub

        Private Function PlaceTopTool(btn As FlatButton, x As Integer, y As Integer, gapAfter As Integer) As Integer
            If btn Is Nothing Then
                Return x
            End If
            Dim location As Point = New Point(x, y)
            CType(CObj(btn), Control).Location = location
            Call CType(CObj(btn), Control).BringToFront()
            Return CType(CObj(btn), Control).Right + gapAfter
        End Function

        Private Function PlaceRightRail(btn As FlatButton, x As Integer, y As Integer, stepY As Integer) As Integer
            If btn Is Nothing Then
                Return y
            End If
            Dim size As Size = New Size(108, 28)
            CType(CObj(btn), Control).Size = size
            Dim location As Point = New Point(x, y)
            CType(CObj(btn), Control).Location = location
            CType(btn, LCARSbuttonClass).ButtonTextAlign = ContentAlignment.BottomRight
            Call CType(CObj(btn), Control).BringToFront()
            Return y + stepY
        End Function

        Private Shared Sub LayoutEmptyLabel(lbl As Label, host As LcarsScrollPanel)
            If lbl IsNot Nothing AndAlso host IsNot Nothing Then
                Dim location As Point = New Point(4, Math.Max(8, CType(CObj(host), Control).ClientSize.Height - 48 / 2))
                lbl.Location = location
                Dim size As Size = New Size(Math.Max(80, host.ContentPanel.Width - 8), 48)
                lbl.Size = size
            End If
        End Sub

        Private Sub OnCloseClick(sender As Object, e As EventArgs)
            Call CType(CObj(Me), Form).Close()
        End Sub

        Private Sub SetStatus(text As String)
            If lblStatus IsNot Nothing Then
                Dim text2 As String = If(Not Equals(text, Nothing), text, "").Trim()
                If text2.Length = 0 Then
                    text2 = "READY"
                End If
                lblStatus.Text = text2.ToUpperInvariant()
            End If
        End Sub

        Private Sub SetBusy(busy As Boolean)
            If fbRefresh IsNot Nothing Then
                CType(fbRefresh, LCARSbuttonClass).Clickable = Not busy
            End If
            If fbImport IsNot Nothing Then
                CType(fbImport, LCARSbuttonClass).Clickable = Not busy
            End If
            If fbFilters IsNot Nothing Then
                CType(fbFilters, LCARSbuttonClass).Clickable = Not busy
            End If
            If fbPgp IsNot Nothing Then
                CType(fbPgp, LCARSbuttonClass).Clickable = Not busy
            End If
            UpdateComposeButtonsEnabled()
        End Sub

        Private Sub UpdateComposeButtonsEnabled()
            Dim flag = activeAccount IsNot Nothing
            Dim flag2 = If(selectedHeader IsNot Nothing OrElse selectedMessages.Count > 0, True, False)
            Dim flag3 = If(folderWorker IsNot Nothing AndAlso folderWorker.IsBusy OrElse headerWorker IsNot Nothing AndAlso headerWorker.IsBusy OrElse bodyWorker IsNot Nothing AndAlso bodyWorker.IsBusy OrElse mutationWorker IsNot Nothing AndAlso mutationWorker.IsBusy OrElse attachmentWorker IsNot Nothing AndAlso attachmentWorker.IsBusy, True, False)
            Dim flag4 = Not String.IsNullOrWhiteSpace(selectedFolderFullName)
            If fbNew IsNot Nothing Then
                CType(fbNew, LCARSbuttonClass).Clickable = If(flag AndAlso Not flag3, True, False)
            End If
            If fbReply IsNot Nothing Then
                CType(fbReply, LCARSbuttonClass).Clickable = If(flag AndAlso flag2 AndAlso Not flag3, True, False)
            End If
            If fbReplyAll IsNot Nothing Then
                CType(fbReplyAll, LCARSbuttonClass).Clickable = If(flag AndAlso flag2 AndAlso Not flag3, True, False)
            End If
            If fbForward IsNot Nothing Then
                CType(fbForward, LCARSbuttonClass).Clickable = If(flag AndAlso flag2 AndAlso Not flag3, True, False)
            End If
            If fbDelete IsNot Nothing Then
                CType(fbDelete, LCARSbuttonClass).Clickable = If(flag AndAlso flag2 AndAlso flag4 AndAlso Not flag3, True, False)
            End If
            If fbMove IsNot Nothing Then
                CType(fbMove, LCARSbuttonClass).Clickable = If(flag AndAlso flag2 AndAlso flag4 AndAlso Not flag3, True, False)
            End If
        End Sub

        Private Sub LoadAccountsFromStore()
            accounts = AccountStore.LoadAccounts()
            If accounts Is Nothing OrElse accounts.Count = 0 Then
                activeAccount = Nothing
                Return
            End If
            activeAccount = accounts(0)
            QueueBackgroundCommsSync()
        End Sub

        Private Sub QueueBackgroundSearchIndex()
            If accounts Is Nothing OrElse accounts.Count = 0 Then
                Return
            End If
            MailSearchBackgroundIndexer.QueueIndexAccounts(accounts, Function(acct) ResolvePassword(acct, promptIfMissing:=False), mailService)
            Call ThreadPool.QueueUserWorkItem(Sub(state)
                                                  Thread.Sleep(5000)
                                                  Try
                                                      If Not CType(CObj(Me), Control).IsDisposed AndAlso CType(CObj(Me), Control).IsHandleCreated Then
                                                          Dim lastRunSummary = MailSearchBackgroundIndexer.LastRunSummary
                                                          If Not String.IsNullOrWhiteSpace(lastRunSummary) Then
                                                              Call CType(CObj(Me), Control).BeginInvoke(CType(CType((Sub() SetStatus(lastRunSummary.ToUpperInvariant())), Action), [Delegate]))
                                                          End If
                                                      End If
                                                  Catch projectError As Exception
                                                      ProjectData.SetProjectError(projectError)
                                                      Call ProjectData.ClearProjectError()
                                                  End Try
                                              End Sub)
        End Sub

        Private Sub QueueBackgroundCommsSync()
            Dim text = If(activeAccount IsNot Nothing, activeAccount.Id, Nothing)
            Call ThreadPool.QueueUserWorkItem(Sub(state)
                                                  Dim num = 0
                                                  Dim num2 = 0
                                                  Dim num3 = 0
                                                  Try
                                                      num = AddressBookStore.TrySyncStoredCardDavBidirectional()
                                                      num2 = LcarsCalendarBridge.TrySyncStoredCalDavBidirectional()
                                                      If accounts IsNot Nothing Then
                                                          num3 = EwsBackgroundSync.TryMergeCalendarsForAccounts(accounts)
                                                      End If
                                                  Catch projectError As Exception
                                                      ProjectData.SetProjectError(projectError)
                                                      Call ProjectData.ClearProjectError()
                                                  End Try
                                                  Dim num4 = 0
                                                  Try
                                                      If Not String.IsNullOrWhiteSpace(text) Then
                                                          num4 = MailCalendarInvitationBridge.TryScanIndexedBodiesForInvitations(text, 96)
                                                      End If
                                                  Catch projectError2 As Exception
                                                      ProjectData.SetProjectError(projectError2)
                                                      Call ProjectData.ClearProjectError()
                                                  End Try
                                                  Try
                                                      If Not CType(CObj(Me), Control).IsDisposed AndAlso CType(CObj(Me), Control).IsHandleCreated Then
                                                          Call CType(CObj(Me), Control).BeginInvoke(CType(CType((Sub()
                                                                                                                     If num > 0 OrElse num2 > 0 OrElse num3 > 0 OrElse num4 > 0 Then
                                                                                                                         SetStatus("BACKGROUND SYNC — CARD +" & num.ToString() & " / CAL +" & num2.ToString() & " / EWS CAL +" & num3.ToString() & " / INVITE +" & num4.ToString())
                                                                                                                     ElseIf num2 < 0 Then
                                                                                                                         SetStatus("BACKGROUND CALDAV SYNC WARNING")
                                                                                                                     End If
                                                                                                                 End Sub), Action), [Delegate]))
                                                      End If
                                                  Catch projectError3 As Exception
                                                      ProjectData.SetProjectError(projectError3)
                                                      Call ProjectData.ClearProjectError()
                                                  End Try
                                              End Sub)
        End Sub

        Private Function ResolvePassword(account As MailAccount, promptIfMissing As Boolean) As String
            If account Is Nothing Then
                Return Nothing
            End If
            Dim mailAccount = account
            Dim text = If(Not Equals(mailAccount.Id, Nothing), mailAccount.Id, "")
            If text.Length = 0 Then
                Return Nothing
            End If
            If EwsMailBridge.IsEwsAccount(account) AndAlso EwsMailBridge.ResolveOAuthToken().Length > 0 Then
                sessionPasswords(text) = ""
                Return ""
            End If
            If MailOAuthProviders.UsesOAuth(account) Then
                Dim accessToken As String = Nothing
                If MailOAuthStore.TryEnsureAccessToken(account, accessToken) AndAlso Not String.IsNullOrWhiteSpace(accessToken) Then
                    Return accessToken
                End If
                If Not promptIfMissing Then
                    Return Nothing
                End If
                account.AuthMethod = "oauth2"
                PersistAccountAuthMethod(account)
                Dim accessToken2 As String = Nothing
                If Not MailOAuthBrowserFlow.TryAuthorize(CType(Me, IWin32Window), account, accessToken2) Then
                    Me.SetStatus(If(Not Equals(MailOAuthBrowserFlow.LastError, Nothing), MailOAuthBrowserFlow.LastError, "SIGN-IN FAILED"))
                    Return Nothing
                End If
                PersistAccountAuthMethod(account)
                Return accessToken2
            End If
            Dim value As String = Nothing
            If sessionPasswords.TryGetValue(text, value) Then
                Return value
            End If
            Dim password As String = Nothing
            If AccountStore.TryGetPassword(text, password) Then
                sessionPasswords(text) = password
                Return password
            End If
            If Not promptIfMissing Then
                Return Nothing
            End If
            If EwsMailBridge.IsEwsAccount(account) AndAlso EwsMailBridge.ResolveOAuthToken().Length > 0 Then
                sessionPasswords(text) = ""
                Return ""
            End If
            mailAccount = account
            Dim text2 = If(Not Equals(mailAccount.EmailAddress, Nothing), mailAccount.EmailAddress, account.Username)
            If String.IsNullOrWhiteSpace(text2) Then
                text2 = account.DisplayName
            End If
            If String.IsNullOrWhiteSpace(text2) Then
                text2 = text
            End If
            Return PromptAndStorePassword(account, text2)
        End Function

        Private Function PromptAndStorePassword(account As MailAccount, label As String) As String
            Dim text = If(Not Equals(account.Id, Nothing), account.Id, "")
            Dim text2 = LcarsCommsUi.PromptPassword(CType(Me, IWin32Window), "Enter the mail password for " & label & ".")
            If Equals(text2, Nothing) Then
                Return Nothing
            End If
            sessionPasswords(text) = text2
            AccountStore.SavePassword(text, text2)
            Return text2
        End Function

        Private Sub PersistAccountAuthMethod(account As MailAccount)
            If account Is Nothing OrElse String.IsNullOrWhiteSpace(account.Id) Then
                Return
            End If
            Dim list As List(Of MailAccount) = AccountStore.LoadAccounts()
            Dim flag = False
            Dim num = list.Count - 1
            For i = 0 To num
                If String.Equals(list(i).Id, account.Id, StringComparison.OrdinalIgnoreCase) Then
                    list(i).AuthMethod = account.AuthMethod
                    flag = True
                    Exit For
                End If
            Next
            If flag Then
                AccountStore.SaveAccounts(list)
            End If
        End Sub

        Private Sub OnImportClick(sender As Object, e As EventArgs)
            Dim text = LcarsCommsUi.PickFolder(CType(Me, IWin32Window), "THUNDERBIRD PROFILE (PREFS.JS)")
            If String.IsNullOrEmpty(text) Then
                Return
            End If
            Dim prefsJsPath = Path.Combine(text, "prefs.js")
            Dim list = ThunderbirdPrefsParser.ParsePrefsJs(prefsJsPath)
            If list Is Nothing OrElse list.Count = 0 Then
                Dim text2 = If(Not Equals(ThunderbirdPrefsParser.LastError, Nothing), ThunderbirdPrefsParser.LastError, "No IMAP accounts found in prefs.js.")
                SetStatus(text2)
                LcarsCommsUi.Alert(text2, "LCARS COMMS — IMPORT ERROR")
                Return
            End If
            Dim list2 As List(Of MailAccount) = AccountStore.LoadAccounts()
            If list2 IsNot Nothing AndAlso list2.Count > 0 Then
                Dim prompt As String = "This will replace " & list2.Count.ToString() & " saved account(s) with " & list.Count.ToString() & " from the selected profile." & vbCrLf & vbCrLf & "Continue?"
                If Not LcarsCommsUi.Confirm(prompt, "LCARS COMMS — RE-IMPORT") Then
                    SetStatus("IMPORT CANCELLED")
                    Return
                End If
            End If
            For Each item In list
                Dim text3 = ResolvePassword(item, promptIfMissing:=True)
                If Equals(text3, Nothing) Then
                    SetStatus("IMPORT CANCELLED — PASSWORD REQUIRED")
                    LcarsCommsUi.Alert("Import cancelled — password required for each account.", "LCARS COMMS — IMPORT")
                    Return
                End If
            Next
            If Not AccountStore.SaveAccounts(list) Then
                Dim text4 = If(Not Equals(AccountStore.LastError, Nothing), AccountStore.LastError, "Failed to save accounts.")
                SetStatus(text4)
                LcarsCommsUi.Alert(text4, "LCARS COMMS — IMPORT ERROR")
                Return
            End If
            LoadAccountsFromStore()
            selectedFolderFullName = Nothing
            selectedHeader = Nothing
            ClearReadingPane()
            ClearMessageList()
            Dim text5 As String = "IMPORTED " & list.Count.ToString() & " ACCOUNT(S)"
            Dim num = AddressBookStore.TryImportFromProfile(text)
            If num > 0 Then
                text5 = text5 & " + " & num.ToString() & " CONTACT(S)"
            ElseIf num < 0 Then
                Dim text6 = If(Not Equals(AddressBookStore.LastError, Nothing), AddressBookStore.LastError, "address book import failed")
                text5 = text5 & " — " & text6.ToUpperInvariant()
            End If
            Dim num2 = MailFilterStore.TryImportFromProfile(text)
            If num2 > 0 Then
                text5 = text5 & " + " & num2.ToString() & " FILTER(S)"
            ElseIf num2 < 0 Then
                Dim text7 = If(Not Equals(MailFilterStore.LastError, Nothing), MailFilterStore.LastError, "filter import failed")
                text5 = text5 & " — " & text7.ToUpperInvariant()
            End If
            lastImportedProfileDir = text
            If AddressBookStore.TryApplyThunderbirdCardDavFromProfile(text) > 0 Then
                text5 += " + CARDDAV"
            End If
            AddressBookStore.TrySyncStoredCardDavBidirectional()
            CardDavLiveBookCache.QueueBackgroundRefresh()
            If LcarsCalendarBridge.TryApplyThunderbirdCalDavFromProfile(text) > 0 Then
                text5 += " + CALDAV"
            End If
            LcarsCalendarBridge.TrySyncStoredCalDavBidirectional()
            SetStatus(text5)
            StartRefreshFolders()
        End Sub

        Private Sub OnFiltersClick(sender As Object, e As EventArgs)
            Dim password = If(activeAccount IsNot Nothing, ResolvePassword(activeAccount, promptIfMissing:=False), Nothing)
            Dim frmFilters2 As frmFilters = New frmFilters(activeAccount, password, selectedFolderFullName, mailService.ImapService, AddressOf RefreshCurrentFolderHeaders)
            Try
                CType(CObj(frmFilters2), Form).ShowDialog(CType(Me, IWin32Window))
            Finally
                If frmFilters2 IsNot Nothing Then CType(frmFilters2, IDisposable).Dispose()
            End Try
        End Sub

        Private Sub OnDraftsClick(sender As Object, e As EventArgs)
            If activeAccount Is Nothing Then
                SetStatus("IMPORT A THUNDERBIRD PROFILE")
                Return
            End If
            If (Control.ModifierKeys And Keys.Shift) = Keys.Shift Then
                Me.TrySelectDraftsFolder()
                Return
            End If
            Dim text = ResolvePassword(activeAccount, promptIfMissing:=False)
            Dim imapDraftHeaders As List(Of MailMessageHeader) = Nothing
            If Not Equals(text, Nothing) Then
                Dim text2 = mailService.ResolveDraftsFolderFullName(activeAccount, text)
                If text2.Length > 0 Then
                    Try
                        imapDraftHeaders = mailService.SyncHeaders(activeAccount, text, text2)
                    Catch projectError As Exception
                        ProjectData.SetProjectError(projectError)
                        imapDraftHeaders = Nothing
                        Call ProjectData.ClearProjectError()
                    End Try
                End If
            End If
            Dim mailAccount = activeAccount
            Dim frmDraftPicker2 As frmDraftPicker = New frmDraftPicker(If(Not Equals(mailAccount.Id, Nothing), mailAccount.Id, ""), imapDraftHeaders)
            Try
                If CType(CObj(frmDraftPicker2), Form).ShowDialog(CType(Me, IWin32Window)) = DialogResult.OK Then
                    If frmDraftPicker2.SelectedImapHeader IsNot Nothing Then
                        OpenImapDraft(frmDraftPicker2.SelectedImapHeader)
                    Else
                        OpenComposeFromDraft(frmDraftPicker2.SelectedDraft)
                    End If
                End If

            Finally
                If frmDraftPicker2 IsNot Nothing Then CType(frmDraftPicker2, IDisposable).Dispose()
            End Try
        End Sub

        Private Sub OnPgpClick(sender As Object, e As EventArgs)
            Dim frmPgpKeys2 As frmPgpKeys = New frmPgpKeys()
            Try
                CType(CObj(frmPgpKeys2), Form).ShowDialog(CType(Me, IWin32Window))
            Finally
                If frmPgpKeys2 IsNot Nothing Then CType(frmPgpKeys2, IDisposable).Dispose()
            End Try
        End Sub

        Private Sub OnCardDavClick(sender As Object, e As EventArgs)
            Dim frmCardDavSync2 As frmCardDavSync = New frmCardDavSync(If(Not Equals(lastImportedProfileDir, Nothing), lastImportedProfileDir, ""))
            Try
                CType(CObj(frmCardDavSync2), Form).ShowDialog(CType(Me, IWin32Window))
            Finally
                If frmCardDavSync2 IsNot Nothing Then CType(frmCardDavSync2, IDisposable).Dispose()
            End Try
        End Sub

        Private Sub OnAddressBookClick(sender As Object, e As EventArgs)
            Dim frmAddressBook2 As frmAddressBook = New frmAddressBook()
            Try
                CType(CObj(frmAddressBook2), Form).ShowDialog(CType(Me, IWin32Window))
            Finally
                If frmAddressBook2 IsNot Nothing Then CType(frmAddressBook2, IDisposable).Dispose()
            End Try
        End Sub

        Private Sub OnNewsClick(sender As Object, e As EventArgs)
            Dim frmNews2 As frmNews = New frmNews()
            Try
                CType(CObj(frmNews2), Form).ShowDialog(CType(Me, IWin32Window))
            Finally
                If frmNews2 IsNot Nothing Then CType(frmNews2, IDisposable).Dispose()
            End Try
        End Sub

        Private Sub OnEwsClick(sender As Object, e As EventArgs)
            Dim frmEwsSync2 As frmEwsSync = New frmEwsSync()
            Try
                CType(CObj(frmEwsSync2), Form).ShowDialog(CType(Me, IWin32Window))
            Finally
                If frmEwsSync2 IsNot Nothing Then CType(frmEwsSync2, IDisposable).Dispose()
            End Try
        End Sub

        Private Sub OnCacheClick(sender As Object, e As EventArgs)
            Dim frmOfflineSettings2 As frmOfflineSettings = New frmOfflineSettings(If(activeAccount IsNot Nothing, activeAccount.Id, Nothing))
            Try
                CType(CObj(frmOfflineSettings2), Form).ShowDialog(CType(Me, IWin32Window))
            Finally
                If frmOfflineSettings2 IsNot Nothing Then CType(frmOfflineSettings2, IDisposable).Dispose()
            End Try
            If activeAccount IsNot Nothing Then
                MailOfflinePolicy.ApplyBodyRetention(activeAccount.Id)
                MailOfflinePolicy.ApplyHeaderRetention(activeAccount.Id)
                SetStatus("CACHE RETENTION — BODY " & MailOfflinePolicy.BodyRetentionDays().ToString() & "D / HDR " & MailOfflinePolicy.HeaderRetentionDays().ToString() & "D APPLIED")
            End If
        End Sub

        Private Sub OnRefreshClick(sender As Object, e As EventArgs)
            If accounts Is Nothing OrElse accounts.Count = 0 Then
                SetStatus("IMPORT A THUNDERBIRD PROFILE")
            Else
                StartRefreshFolders()
            End If
        End Sub

        Private Sub StartRefreshFolders()
            If folderWorker.IsBusy Then
                Return
            End If
            If accounts Is Nothing OrElse accounts.Count = 0 Then
                SetStatus("IMPORT A THUNDERBIRD PROFILE")
                Return
            End If
            Dim list As List(Of FolderRefreshEntry) = New List(Of FolderRefreshEntry)()
            For Each account In accounts
                If account IsNot Nothing Then
                    Dim text = ResolvePassword(account, promptIfMissing:=True)
                    If Equals(text, Nothing) Then
                        SetStatus("PASSWORD REQUIRED")
                        Return
                    End If
                    list.Add(New FolderRefreshEntry With {
        .Account = account,
        .Password = text
    })
                End If
            Next
            If list.Count = 0 Then
                SetStatus("IMPORT A THUNDERBIRD PROFILE")
                Return
            End If
            SetBusy(busy:=True)
            SetStatus("LISTING FOLDERS…")
            folderWorker.RunWorkerAsync(New FolderWorkerArgs With {
        .Entries = list
    })
        End Sub

        Private Sub FolderWorker_DoWork(sender As Object, e As DoWorkEventArgs)
            Dim folderWorkerArgs = CType(e.Argument, FolderWorkerArgs)
            Dim list As List(Of AccountFolderSection) = New List(Of AccountFolderSection)()
            If folderWorkerArgs Is Nothing OrElse folderWorkerArgs.Entries Is Nothing Then
                e.Result = list
                Return
            End If
            For Each entry In folderWorkerArgs.Entries
                If entry Is Nothing OrElse entry.Account Is Nothing Then
                    Continue For
                End If
                Dim accountFolderSection As AccountFolderSection = New AccountFolderSection()
                accountFolderSection.Account = entry.Account
                Dim accountFolderSection2 = accountFolderSection
                Try
                    accountFolderSection2.Folders = mailService.ListFolders(entry.Account, entry.Password)
                    If accountFolderSection2.Folders Is Nothing OrElse accountFolderSection2.Folders.Count = 0 Then
                        accountFolderSection2.Folders = FallbackInboxOnly()
                        accountFolderSection2.ErrorText = "NO FOLDERS — USING INBOX"
                    End If
                Catch ex As Exception
                    ProjectData.SetProjectError(ex)
                    Dim ex2 = ex
                    accountFolderSection2.Folders = FallbackInboxOnly()
                    accountFolderSection2.ErrorText = ex2.Message
                    Call ProjectData.ClearProjectError()
                End Try
                list.Add(accountFolderSection2)
            Next
            e.Result = list
        End Sub

        Private Sub FolderWorker_Completed(sender As Object, e As RunWorkerCompletedEventArgs)
            SetBusy(busy:=False)
            If e.Error IsNot Nothing Then
                SetStatus("FOLDER ERROR: " & e.Error.Message)
                Return
            End If
            Dim list As List(Of AccountFolderSection) = Nothing

            If Not (CSharpImpl.__Assign(list, TryCast(e.Result, List(Of AccountFolderSection))) IsNot Nothing) Then
                SetStatus("FOLDER ERROR")
                Return
            End If
            PopulateFolderRailFromSections(list)
            Dim num = 0
            Dim num2 = 0
            For Each item In list
                If item IsNot Nothing Then
                    If item.Folders IsNot Nothing Then
                        num += item.Folders.Count
                    End If
                    If Not String.IsNullOrEmpty(item.ErrorText) Then
                        num2 += 1
                    End If
                End If
            Next
            If num2 > 0 Then
                SetStatus(num.ToString() & " FOLDER(S) — " & num2.ToString() & " ACCOUNT WARNING(S)")
            Else
                SetStatus(num.ToString() & " FOLDER(S) — " & list.Count.ToString() & " ACCOUNT(S)")
            End If
        End Sub

        Private Shared Function FallbackInboxOnly() As List(Of MailFolderInfo)
            Dim list As List(Of MailFolderInfo) = New List(Of MailFolderInfo)()
            list.Add(New MailFolderInfo With {
        .FullName = "INBOX",
        .DisplayName = "INBOX"
    })
            Return list
        End Function

        Private Function ResolveInboxFolderName(account As MailAccount, password As String) As String
            If account Is Nothing Then
                Return "INBOX"
            End If
            If lastFolderSections IsNot Nothing Then
                For Each lastFolderSection In lastFolderSections
                    If lastFolderSection Is Nothing OrElse lastFolderSection.Account Is Nothing OrElse Not String.Equals(lastFolderSection.Account.Id, account.Id, StringComparison.OrdinalIgnoreCase) Then
                        Continue For
                    End If
                    If lastFolderSection.Folders Is Nothing Then
                        Exit For
                    End If
                    For Each folder In lastFolderSection.Folders
                        If folder IsNot Nothing Then
                            Dim mailFolderInfo = folder
                            Dim text = If(Not Equals(mailFolderInfo.FullName, Nothing), mailFolderInfo.FullName, "")
                            mailFolderInfo = folder
                            Dim a = If(Not Equals(mailFolderInfo.DisplayName, Nothing), mailFolderInfo.DisplayName, "")
                            If String.Equals(text, "INBOX", StringComparison.OrdinalIgnoreCase) OrElse String.Equals(a, "INBOX", StringComparison.OrdinalIgnoreCase) OrElse String.Equals(a, "Inbox", StringComparison.OrdinalIgnoreCase) Then
                                Return text
                            End If
                        End If
                    Next
                    Exit For
                Next
            End If
            Return "INBOX"
        End Function

        Private Function FindAccountById(accountId As String) As MailAccount
            If accounts Is Nothing OrElse String.IsNullOrWhiteSpace(accountId) Then
                Return Nothing
            End If
            For Each account In accounts
                If account IsNot Nothing AndAlso String.Equals(account.Id, accountId, StringComparison.OrdinalIgnoreCase) Then
                    Return account
                End If
            Next
            Return Nothing
        End Function

        Private Sub PopulateFolderRailFromSections(sections As List(Of AccountFolderSection))
            If folderHost Is Nothing Then
                Return
            End If
            lastFolderSections = sections
            ClearFolderSelection()
            ClearFolderRailControls()
            If sections Is Nothing OrElse sections.Count = 0 Then
                lblFolderEmpty.Visible = True
                folderHost.Relayout()
                Return
            End If
            lblFolderEmpty.Visible = False
            Dim contentPanel As Panel = folderHost.ContentPanel
            Dim num = 4
            Dim num2 = Math.Max(80, contentPanel.Width - 8)
            If sections.Count > 0 Then
                Dim folderRowTag As FolderRowTag = New FolderRowTag()
                folderRowTag.IsUnifiedInbox = True
                folderRowTag.Folder = New MailFolderInfo With {
    .FullName = "__UNIFIED_INBOX__",
    .DisplayName = "INBOX"
}
                folderRowTag.Flip = False
                Dim tag = folderRowTag
                Dim val As FlatButton = MakeFolderButton("INBOX", flip:=False)
                CType(CObj(val), Control).Tag = tag
                Dim location As Point = New Point(2, num)
                CType(CObj(val), Control).Location = location
                Dim size As Size = New Size(num2, 36)
                CType(CObj(val), Control).Size = size
                AddHandler CType(CObj(val), Control).Click, AddressOf Me.OnFolderButtonClick
                contentPanel.Controls.Add(CType(CObj(val), Control))
                num += 44
            End If
            For Each section In sections
                If section Is Nothing OrElse section.Account Is Nothing Then
                    Continue For
                End If
                Dim account = section.Account
                Dim mailAccount = account
                Dim accountId = If(Not Equals(mailAccount.Id, Nothing), mailAccount.Id, "")
                Dim flag = IsAccountCollapsed(accountId)
                Dim val2 As FlatButton = MakeAccountHeaderButton(AccountDisplayLabel(account), flag)
                CType(CObj(val2), Control).Tag = account
                Dim location As Point = New Point(2, num)
                CType(CObj(val2), Control).Location = location
                Dim size As Size = New Size(num2, 30)
                CType(CObj(val2), Control).Size = size
                AddHandler CType(CObj(val2), Control).Click, AddressOf OnAccountHeaderClick
                contentPanel.Controls.Add(CType(CObj(val2), Control))
                num += 34
                If flag Then
                    Continue For
                End If
                Dim flag2 = False
                Dim width = Math.Max(70, num2 - 10)
                Dim list = section.Folders
                If list Is Nothing OrElse list.Count = 0 Then
                    list = FallbackInboxOnly()
                End If
                For Each item In list
                    If item IsNot Nothing Then
                        Dim mailFolderInfo = item
                        Dim text = If(Not Equals(mailFolderInfo.DisplayName, Nothing), mailFolderInfo.DisplayName, item.FullName)
                        If String.IsNullOrWhiteSpace(text) Then
                            text = item.FullName
                        End If
                        Dim folderRowTag As FolderRowTag = New FolderRowTag()
                        folderRowTag.Account = account
                        folderRowTag.Folder = item
                        folderRowTag.Flip = flag2
                        Dim tag2 = folderRowTag
                        Dim val3 As FlatButton = MakeFolderButton(text.ToUpperInvariant(), flag2)
                        flag2 = Not flag2
                        CType(CObj(val3), Control).Tag = tag2
                        location = New Point(12, num)
                        CType(CObj(val3), Control).Location = location
                        size = New Size(width, 36)
                        CType(CObj(val3), Control).Size = size
                        AddHandler CType(CObj(val3), Control).Click, AddressOf Me.OnFolderButtonClick
                        contentPanel.Controls.Add(CType(CObj(val3), Control))
                        num += 40
                    End If
                Next
            Next
            folderHost.Relayout()
        End Sub

        Private Sub ClearFolderRailControls()
            Dim contentPanel As Panel = folderHost.ContentPanel
            For Each control As Control In contentPanel.Controls
                If control IsNot lblFolderEmpty Then
                    contentPanel.Controls.Remove(control)
                    control.Dispose()
                End If
            Next
        End Sub

        Private Sub RebuildFolderRailFromCache()
            If lastFolderSections IsNot Nothing Then
                PopulateFolderRailFromSections(lastFolderSections)
            End If
        End Sub

        Private Function IsAccountCollapsed(accountId As String) As Boolean
            If String.IsNullOrEmpty(accountId) Then
                Return False
            End If
            Dim value = False
            If accountCollapsed.TryGetValue(accountId, value) Then
                Return value
            End If
            Return False
        End Function

        Private Shared Function AccountDisplayLabel(acct As MailAccount) As String
            Dim mailAccount As MailAccount
            If EwsMailBridge.IsEwsAccount(acct) Then
                mailAccount = acct
                Dim text = If(Not Equals(mailAccount.DisplayName, Nothing), mailAccount.DisplayName, acct.EmailAddress)
                If String.IsNullOrWhiteSpace(text) Then
                    text = "EWS"
                End If
                Return "EWS · " & text
            End If
            mailAccount = acct
            Dim text2 = If(Not Equals(mailAccount.EmailAddress, Nothing), mailAccount.EmailAddress, acct.Username)
            If String.IsNullOrWhiteSpace(text2) Then
                text2 = acct.DisplayName
            End If
            If String.IsNullOrWhiteSpace(text2) Then
                text2 = acct.Id
            End If
            Return If(Not Equals(text2, Nothing), text2, "").ToUpperInvariant()
        End Function

        Private Shared Function MakeAccountHeaderButton(caption As String, collapsed As Boolean) As FlatButton
            Dim text = If(collapsed, "▸ ", "▾ ")
            Dim val As FlatButton = New FlatButton()
            CType(val, LCARSbuttonClass).ButtonText = text & caption
            CType(val, LCARSbuttonClass).Text = CType(val, LCARSbuttonClass).ButtonText
            CType(val, LCARSbuttonClass).Clickable = True
            CType(val, LCARSbuttonClass).Beeping = True
            CType(val, LCARSbuttonClass).Color = CType(9, LCARScolorStyles)
            CType(val, LCARSbuttonClass).ButtonTextAlign = ContentAlignment.MiddleLeft
            CType(val, LCARSbuttonClass).ButtonTextHeight = 14
            Return val
        End Function

        Private Shared Function MakeFolderButton(caption As String, flip As Boolean) As FlatButton
            Dim val As FlatButton = New FlatButton()
            CType(val, LCARSbuttonClass).ButtonText = caption
            CType(val, LCARSbuttonClass).Text = caption
            CType(val, LCARSbuttonClass).Clickable = True
            CType(val, LCARSbuttonClass).Beeping = True
            CType(val, LCARSbuttonClass).ButtonTextAlign = ContentAlignment.MiddleLeft
            CType(val, LCARSbuttonClass).ButtonTextHeight = 14
            CType(val, LCARSbuttonClass).Color = FolderButtonColor(flip)
            Return val
        End Function

        Private Shared Function FolderButtonColor(flip As Boolean) As LCARScolorStyles
            If flip Then
                Return CType(2, LCARScolorStyles)
            End If
            Return CType(8, LCARScolorStyles)
        End Function

        Private Sub OnAccountHeaderClick(sender As Object, e As EventArgs)
            Dim val As FlatButton = CType(If((TypeOf sender Is FlatButton), sender, Nothing), FlatButton)
            Dim mailAccount As MailAccount = Nothing

            If val IsNot Nothing AndAlso CSharpImpl.__Assign(mailAccount, TryCast(CType(CObj(val), Control).Tag, MailAccount)) IsNot Nothing Then
                Dim mailAccount2 = mailAccount
                Dim text = If(Not Equals(mailAccount2.Id, Nothing), mailAccount2.Id, "")
                If text.Length <> 0 Then
                    Dim flag = IsAccountCollapsed(text)
                    accountCollapsed(text) = Not flag
                    RebuildFolderRailFromCache()
                End If
            End If
        End Sub

                ''' Cannot convert MethodDeclarationSyntax, System.ArgumentOutOfRangeException: Exception of type 'System.ArgumentOutOfRangeException' was thrown. (Parameter 'node')
''' Actual value was LCARSMail.frmMail.FolderRowTag { Folder: not null } folderRowTag.
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.ConvertToVariableDeclaratorOrNull(IsPatternExpressionSyntax node) in /_/CodeConverter/VB/CommonConversions.cs:line 175
'''    at System.Linq.Enumerable.SelectListIterator`2.MoveNext()
'''    at System.Linq.Enumerable.WhereEnumerableIterator`1.MoveNext()
'''    at System.Collections.Generic.LargeArrayBuilder`1.AddRange(IEnumerable`1 items)
'''    at System.Collections.Generic.SparseArrayBuilder`1.ReserveOrAdd(IEnumerable`1 items)
'''    at System.Linq.Enumerable.Concat2Iterator`1.ToArray()
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.ConvertToDeclarationStatement(List`1 des, List`1 isPatternExpressions) in /_/CodeConverter/VB/CommonConversions.cs:line 105
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.InsertRequiredDeclarations(SyntaxList`1 convertedStatements, CSharpSyntaxNode originaNode) in /_/CodeConverter/VB/CommonConversions.cs:line 89
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.ConvertStatement(StatementSyntax statement, CSharpSyntaxVisitor`1 methodBodyVisitor) in /_/CodeConverter/VB/CommonConversions.cs:line 75
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.<>c__DisplayClass10_0.<ConvertStatements>b__0(StatementSyntax s) in /_/CodeConverter/VB/CommonConversions.cs:line 69
'''    at System.Linq.Enumerable.SelectManySingleSelectorIterator`2.MoveNext()
'''    at Microsoft.CodeAnalysis.SyntaxList`1.CreateNode(IEnumerable`1 nodes)
'''    at Microsoft.CodeAnalysis.SyntaxList`1..ctor(IEnumerable`1 nodes)
'''    at Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory.List[TNode](IEnumerable`1 nodes)
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.ConvertStatements(SyntaxList`1 statements, MethodBodyExecutableStatementVisitor iteratorState) in /_/CodeConverter/VB/CommonConversions.cs:line 69
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.ConvertBody(BlockSyntax body, ArrowExpressionClauseSyntax expressionBody, Boolean hasReturnType, MethodBodyExecutableStatementVisitor iteratorState) in /_/CodeConverter/VB/CommonConversions.cs:line 49
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitMethodDeclaration(MethodDeclarationSyntax node) in /_/CodeConverter/VB/NodesVisitor.cs:line 428
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingVisitorWrapper`1.Accept(SyntaxNode csNode, Boolean addSourceMapping) in /_/CodeConverter/VB/CommentConvertingVisitorWrapper.cs:line 20
''' 
''' Input:
''' 
''' 	private void OnFolderButtonClick(object sender, System.EventArgs e)
''' 	{
''' 		FlatButton val = (FlatButton)((sender is FlatButton) ? sender : null);
''' 		if (val != null && ((System.Windows.Forms.Control)(object)val).Tag is LCARSMail.frmMail.FolderRowTag { Folder: not null } folderRowTag && (folderRowTag.IsUnifiedInbox || folderRowTag.Account != null))
''' 		{
''' 			if (this.selectedFolderButton != null && ((System.Windows.Forms.Control)(object)this.selectedFolderButton).Tag is LCARSMail.frmMail.FolderRowTag folderRowTag2)
''' 			{
''' 				((LCARSbuttonClass)this.selectedFolderButton).Color = LCARSMail.frmMail.FolderButtonColor(folderRowTag2.Flip);
''' 			}
''' 			this.selectedFolderButton = val;
''' 			((LCARSbuttonClass)val).Color = (LCARScolorStyles)6;
''' 			this.unifiedInboxActive = folderRowTag.IsUnifiedInbox;
''' 			if (folderRowTag.IsUnifiedInbox)
''' 			{
''' 				this.activeAccount = ((this.accounts != null && this.accounts.Count > 0) ? this.accounts[0] : null);
''' 				this.selectedFolderFullName = "__UNIFIED_INBOX__";
''' 			}
''' 			else
''' 			{
''' 				this.activeAccount = folderRowTag.Account;
''' 				this.selectedFolderFullName = folderRowTag.Folder.FullName;
''' 			}
''' 			this.crossFolderSearchActive = false;
''' 			this.selectedHeader = null;
''' 			this.selectedBody = null;
''' 			this.ClearReadingPane();
''' 			this.UpdateComposeButtonsEnabled();
''' 			this.StartSyncHeaders(this.selectedFolderFullName);
''' 		}
''' 	}
''' 
''' 

        Private Sub StartSyncHeaders(folderFullName As String)
            If headerWorker.IsBusy Then
                Return
            End If
            currentFolderHeaders = Nothing
            SetBusy(busy:=True)
            SetStatus("SYNCING MESSAGES…")
            If String.Equals(folderFullName, "__UNIFIED_INBOX__", StringComparison.Ordinal) OrElse unifiedInboxActive Then
                Dim list As List(Of FolderRefreshEntry) = New List(Of FolderRefreshEntry)()
                If accounts IsNot Nothing Then
                    For Each account In accounts
                        If account IsNot Nothing Then
                            Dim text = ResolvePassword(account, promptIfMissing:=False)
                            If Not Equals(text, Nothing) Then
                                list.Add(New FolderRefreshEntry With {
        .Account = account,
        .Password = text
    })
                            End If
                        End If
                    Next
                End If
                If list.Count = 0 Then
                    SetBusy(busy:=False)
                    SetStatus("PASSWORD REQUIRED — USE REFRESH")
                    Return
                End If
                headerWorker.RunWorkerAsync(New HeaderWorkerArgs With {
        .Unified = True,
        .Folder = "__UNIFIED_INBOX__",
        .Entries = list
    })
            ElseIf activeAccount Is Nothing Then
                SetBusy(busy:=False)
            Else
                Dim text2 = ResolvePassword(activeAccount, promptIfMissing:=False)
                If Equals(text2, Nothing) Then
                    SetBusy(busy:=False)
                    SetStatus("PASSWORD REQUIRED — USE REFRESH")
                    Return
                End If
                headerWorker.RunWorkerAsync(New HeaderWorkerArgs With {
        .Account = activeAccount,
        .Password = text2,
        .Folder = folderFullName,
        .Unified = False
    })
            End If
        End Sub

        Private Sub HeaderWorker_DoWork(sender As Object, e As DoWorkEventArgs)
            Dim headerWorkerArgs = CType(e.Argument, HeaderWorkerArgs)
            Dim headerWorkerResult As HeaderWorkerResult = New HeaderWorkerResult()
            headerWorkerResult.Folder = headerWorkerArgs.Folder
            Dim headerWorkerResult2 = headerWorkerResult
            If headerWorkerArgs IsNot Nothing AndAlso headerWorkerArgs.Unified AndAlso headerWorkerArgs.Entries IsNot Nothing Then
                Dim list As List(Of MailMessageHeader) = New List(Of MailMessageHeader)()
                For Each entry In headerWorkerArgs.Entries
                    If entry Is Nothing OrElse entry.Account Is Nothing Then
                        Continue For
                    End If
                    Try
                        Dim folder = ResolveInboxFolderName(entry.Account, entry.Password)
                        Dim list2 = mailService.SyncHeaders(entry.Account, entry.Password, folder, 300)
                        If list2 Is Nothing Then
                            Continue For
                        End If
                        For Each item In list2
                            If item IsNot Nothing Then
                                item.AccountId = entry.Account.Id
                                If String.IsNullOrWhiteSpace(item.Folder) Then
                                    item.Folder = folder
                                End If
                                list.Add(item)
                            End If
                        Next
                    Catch projectError As Exception
                        ProjectData.SetProjectError(projectError)
                        Call ProjectData.ClearProjectError()
                    End Try
                Next
                list.Sort(Function(a, b) b.DateUtc.CompareTo(a.DateUtc))
                headerWorkerResult2.Headers = list
                e.Result = headerWorkerResult2
                Return
            End If
            Try
                headerWorkerResult2.Headers = mailService.SyncHeaders(headerWorkerArgs.Account, headerWorkerArgs.Password, headerWorkerArgs.Folder)
                Dim list3 As List(Of MailFilterRule) = MailFilterStore.LoadRules()
                If list3 IsNot Nothing AndAlso list3.Count > 0 AndAlso headerWorkerResult2.Headers IsNot Nothing AndAlso headerWorkerResult2.Headers.Count > 0 Then
                    Dim mailFilterRunSummary = If(Not EwsMailBridge.IsEwsAccount(headerWorkerArgs.Account), MailFilterEngine.ApplyRulesToFolder(headerWorkerArgs.Account, headerWorkerArgs.Password, headerWorkerArgs.Folder, headerWorkerResult2.Headers, mailService.ImapService, list3), MailFilterEngineEws.ApplyRulesToFolder(headerWorkerArgs.Account, headerWorkerArgs.Password, headerWorkerArgs.Folder, headerWorkerResult2.Headers, mailService, list3))
                    If mailFilterRunSummary IsNot Nothing AndAlso mailFilterRunSummary.ActionsApplied > 0 Then
                        headerWorkerResult2.Headers = mailService.SyncHeaders(headerWorkerArgs.Account, headerWorkerArgs.Password, headerWorkerArgs.Folder)
                        headerWorkerResult2.FilterNote = mailFilterRunSummary.ActionsApplied.ToString() & " FILTER ACTION(S)"
                    ElseIf mailFilterRunSummary IsNot Nothing AndAlso mailFilterRunSummary.Warnings.Count > 0 Then
                        headerWorkerResult2.FilterNote = mailFilterRunSummary.Warnings(0)
                    End If
                End If
            Catch ex As Exception
                ProjectData.SetProjectError(ex)
                Dim ex2 = ex
                headerWorkerResult2.ErrorText = ex2.Message
                headerWorkerResult2.Headers = MailCache.LoadHeaders(headerWorkerArgs.Account.Id, headerWorkerArgs.Folder)
                Call ProjectData.ClearProjectError()
            End Try
            e.Result = headerWorkerResult2
        End Sub

        Private Sub HeaderWorker_Completed(sender As Object, e As RunWorkerCompletedEventArgs)
            SetBusy(busy:=False)
            Dim headerWorkerResult As HeaderWorkerResult = Nothing
            If e.Error IsNot Nothing Then
                SetStatus("SYNC ERROR: " & e.Error.Message)
            ElseIf CSharpImpl.__Assign(headerWorkerResult, TryCast(e.Result, HeaderWorkerResult)) IsNot Nothing Then
                If activeAccount IsNot Nothing Then
                    MailOfflinePolicy.ApplyBodyRetention(activeAccount.Id)
                    MailOfflinePolicy.ApplyHeaderRetention(activeAccount.Id)
                End If
                currentFolderHeaders = headerWorkerResult.Headers
                If activeAccount IsNot Nothing AndAlso headerWorkerResult.Headers IsNot Nothing AndAlso headerWorkerResult.Headers.Count > 0 Then
                    MailSearchIndex.UpsertHeaders(activeAccount.Id, headerWorkerResult.Folder, headerWorkerResult.Headers)
                End If
                Dim shownCount As Integer = ApplyMessageListFilter()
                If Not String.IsNullOrEmpty(headerWorkerResult.ErrorText) Then
                    SetStatus("SYNC WARNING: " & headerWorkerResult.ErrorText)
                ElseIf Not String.IsNullOrEmpty(headerWorkerResult.FilterNote) Then
                    UpdateMessageCountStatus(shownCount)
                    SetStatus(headerWorkerResult.FilterNote.ToUpperInvariant())
                Else
                    UpdateMessageCountStatus(shownCount)
                End If
            End If
        End Sub

        Private Sub OnSearchEnter(sender As Object, e As EventArgs)
            SetStatus("SEARCH: fts · thread: · has:attachment · to: · cc: · messageid: · list-id: · return-path: · replyto: · TAP SEARCH")
        End Sub

        Private Sub OnSearchKeyDown(sender As Object, e As KeyEventArgs)
            If e IsNot Nothing AndAlso e.KeyCode = Keys.Return Then
                OnSearchButtonClick(fbSearch, EventArgs.Empty)
                e.Handled = True
                e.SuppressKeyPress = True
            End If
        End Sub

        Private Sub ApplySearchFacet(prefix As String)
            If txtSearch Is Nothing Then
                Return
            End If
            Dim text As String = If(Not Equals(prefix, Nothing), prefix, "").Trim()
            If text.Length <> 0 Then
                Dim text2 As String = txtSearch.Text.Trim()
                If text2.StartsWith(text, StringComparison.OrdinalIgnoreCase) Then
                    ApplyFtsSearch(text2.Substring(text.Length).Trim())
                    Return
                End If
                txtSearch.Text = text & " "
                txtSearch.SelectionStart = txtSearch.Text.Length
                txtSearch.Focus()
            End If
        End Sub

        Private Sub OnSearchButtonClick(sender As Object, e As EventArgs)
            Dim text As String = (If(txtSearch IsNot Nothing, txtSearch.Text.Trim(), ""))
            If text.Length = 0 Then
                SetStatus("SEARCH: gloda: · from: · to: · cc: · newer:7d · is:unread · global: · index:")
            ElseIf text.StartsWith("fts:", StringComparison.OrdinalIgnoreCase) OrElse text.StartsWith("gloda:", StringComparison.OrdinalIgnoreCase) Then
                Dim startIndex = If(text.StartsWith("gloda:", StringComparison.OrdinalIgnoreCase), 6, 4)
                ApplyFtsSearch(text.Substring(startIndex).Trim())
            ElseIf text.StartsWith("global:", StringComparison.OrdinalIgnoreCase) Then
                StartGlobalSearch(text.Substring(7).Trim())
            ElseIf text.StartsWith("index:", StringComparison.OrdinalIgnoreCase) Then
                ApplyLocalIndexSearch(text.Substring(6).Trim())
            ElseIf text.StartsWith("imap:", StringComparison.OrdinalIgnoreCase) Then
                OnSearchTextChanged(txtSearch, EventArgs.Empty)
            ElseIf activeAccount IsNot Nothing AndAlso text.Length >= 2 Then
                If crossFolderSearchActive AndAlso ftsLastPageFull AndAlso String.Equals(text, lastFtsQuery, StringComparison.OrdinalIgnoreCase) Then
                    ApplyFtsSearchPage(append:=True)
                Else
                    ApplyFtsSearch(text)
                End If
            End If
        End Sub

        Private Sub OnSearchTextChanged(sender As Object, e As EventArgs)
            Dim text As String = (If(txtSearch IsNot Nothing, txtSearch.Text.Trim(), ""))
            If text.StartsWith("fts:", StringComparison.OrdinalIgnoreCase) OrElse text.StartsWith("gloda:", StringComparison.OrdinalIgnoreCase) Then
                Dim startIndex = If(text.StartsWith("gloda:", StringComparison.OrdinalIgnoreCase), 6, 4)
                ApplyFtsSearch(text.Substring(startIndex).Trim())
                Return
            End If
            If text.StartsWith("global:", StringComparison.OrdinalIgnoreCase) Then
                StartGlobalSearch(text.Substring(7).Trim())
                Return
            End If
            If text.StartsWith("index:", StringComparison.OrdinalIgnoreCase) Then
                ApplyLocalIndexSearch(text.Substring(6).Trim())
                Return
            End If
            useImapSearch = text.StartsWith("imap:", StringComparison.OrdinalIgnoreCase)
            If useImapSearch AndAlso text.Length > 5 AndAlso activeAccount IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(selectedFolderFullName) Then
                StartImapFolderSearch(text.Substring(5).Trim())
            ElseIf currentFolderHeaders IsNot Nothing Then
                Dim shownCount As Integer = ApplyMessageListFilter()
                UpdateMessageCountStatus(shownCount)
            End If
        End Sub

        Private Sub ApplyFtsSearch(query As String)
            If activeAccount IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(query) Then
                lastFtsQuery = query.Trim()
                ftsResultSkip = 0
                ApplyFtsSearchPage(append:=False)
            End If
        End Sub

        Private Sub ApplyFtsSearchPage(append As Boolean)
            If activeAccount IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(lastFtsQuery) Then
                If Not append Then
                    ftsResultSkip = 0
                End If
                Dim list = MailSearchIndex.Search(activeAccount.Id, lastFtsQuery, 5000, ftsResultSkip)
                If list IsNot Nothing AndAlso list.Count > 1 AndAlso conversationViewEnabled Then
                    list = MailSearchGlodaFts.ApplyThreadAwareRanking(list)
                End If
                ftsLastPageFull = If(list IsNot Nothing AndAlso list.Count >= 5000, True, False)
                crossFolderSearchActive = True
                If append AndAlso currentFolderHeaders IsNot Nothing AndAlso list IsNot Nothing Then
                    Dim list2 As List(Of MailMessageHeader) = New List(Of MailMessageHeader)(currentFolderHeaders)
                    list2.AddRange(list)
                    currentFolderHeaders = list2
                Else
                    currentFolderHeaders = list
                End If
                ClearMessageSelectionKeepButtons()
                selectedHeader = Nothing
                selectedBody = Nothing
                ClearReadingPane()
                Dim shownCount As Integer = ApplyMessageListFilter()
                UpdateMessageCountStatus(shownCount)
                Dim num = MailSearchIndex.CountEntries(activeAccount.Id)
                Dim num2 = MailSearchIndex.CountDistinctIndexedFolders(activeAccount.Id)
                Dim num3 = MailSearchGlodaFts.CountIndexedMessages(activeAccount.Id)
                Dim num4 = MailSearchGlodaFts.CountMessagesWithBody(activeAccount.Id)
                Dim num5 = If(currentFolderHeaders IsNot Nothing, currentFolderHeaders.Count, 0)
                Dim text = If(ftsLastPageFull, " — TAP SEARCH FOR MORE", "")
                Dim text2 = If(lastFtsQuery.Length > 48, lastFtsQuery.Substring(0, 45) & "…", lastFtsQuery)
                Dim text3 = ""
                If list IsNot Nothing AndAlso list.Count > 0 AndAlso list(0).FtsRank <> 0.0 Then
                    text3 = " · TOP RANK " & list(0).FtsRank.ToString("0.0", CultureInfo.InvariantCulture)
                End If
                Dim text4 = ""
                If Not String.IsNullOrWhiteSpace(MailSearchBackgroundIndexer.LastRunSummary) Then
                    text4 = " · " & MailSearchBackgroundIndexer.LastRunSummary.ToUpperInvariant()
                End If
                Dim text5 = ""
                Dim num6 = MailSearchIndex.CountEntriesNeedingBodyPreview(activeAccount.Id)
                If num6 > 0 Then
                    text5 = " · BACKFILL " & num6.ToString()
                End If
                If MailSearchBackgroundIndexer.LastBackfillRemaining > 0 Then
                    text5 = text5 & " · IDXQ " & MailSearchBackgroundIndexer.LastBackfillRemaining.ToString()
                End If
                If MailSearchBackgroundIndexer.LastBodiesIndexedThisRun > 0 Then
                    text5 = text5 & " · RUN+" & MailSearchBackgroundIndexer.LastBodiesIndexedThisRun.ToString()
                End If
                Dim text6 = ""
                If num > 0 AndAlso num3 > 0 Then
                    text6 = " · FTS-COV " & CInt(Math.Round(Math.Min(100.0, Math.Round(100.0 * num3 / num)))).ToString() & "%"
                End If
                Dim text7 = ""
                If num > 0 AndAlso num4 > 0 Then
                    text7 = " · BODY-COV " & CInt(Math.Round(Math.Min(100.0, Math.Round(100.0 * num4 / num)))).ToString() & "%"
                End If
                Dim text8 = ""
                Dim glodaCorpusStats = MailSearchGlodaFts.TryGetCorpusStats(activeAccount.Id)
                If glodaCorpusStats IsNot Nothing AndAlso glodaCorpusStats.TotalFtsRows > 0 Then
                    text8 = " · CORPUS " & glodaCorpusStats.TotalFtsRows.ToString() & "/" & glodaCorpusStats.WithBodyRows.ToString()
                End If
                SetStatus("GLODA: """ & text2 & """ — " & num5.ToString() & " SHOWN — " & num2.ToString() & " FLD · IDX " & num.ToString() & " · FTS " & num3.ToString() & " · BODY " & num4.ToString() & text6 & text7 & text8 & text5 & text3 & " · THREAD-RANK" & text4 & text)
                If list IsNot Nothing Then
                    ftsResultSkip += list.Count
                End If
            End If
        End Sub

        Private Sub OnSaveGlodaSearchClick(sender As Object, e As EventArgs)
            Dim text As String = (If(txtSearch IsNot Nothing, txtSearch.Text.Trim(), ""))
            If text.Length = 0 Then
                SetStatus("SAVE SEARCH: ENTER QUERY FIRST")
                Return
            End If
            Dim text2 As String = If(Not Equals(selectedFolderFullName, Nothing), selectedFolderFullName, "").Trim()
            If MailGlodaSavedSearchStore.TrySaveSearch("", text, text2) Then
                SetStatus("GLODA SEARCH SAVED" & If(text2.Length > 0, " — FLD " & text2, ""))
            Else
                SetStatus("GLODA SEARCH SAVE FAILED")
            End If
        End Sub

        Private Sub OnRecallGlodaSearchClick(sender As Object, e As EventArgs)
            Dim list As List(Of KeyValuePair(Of String, String)) = MailGlodaSavedSearchStore.TryListSaved()
            If list Is Nothing OrElse list.Count = 0 Then
                Dim text As String = MailGlodaSavedSearchStore.TryGetMostRecentQuery()
                If text.Length = 0 Then
                    SetStatus("RECALL: NO SAVED SEARCHES")
                    Return
                End If
                txtSearch.Text = text
                OnSearchButtonClick(fbSearch, EventArgs.Empty)
                Return
            End If
            Dim frmGlodaSavedSearchPicker2 As frmGlodaSavedSearchPicker = New frmGlodaSavedSearchPicker()
            Try
                If CType(CObj(frmGlodaSavedSearchPicker2), Form).ShowDialog(CType(Me, IWin32Window)) = DialogResult.OK AndAlso Not String.IsNullOrWhiteSpace(frmGlodaSavedSearchPicker2.SelectedQuery) Then
                    txtSearch.Text = frmGlodaSavedSearchPicker2.SelectedQuery
                    OnSearchButtonClick(fbSearch, EventArgs.Empty)
                    SetStatus("RECALL: PICKED SEARCH")
                End If

            Finally
                If frmGlodaSavedSearchPicker2 IsNot Nothing Then CType(frmGlodaSavedSearchPicker2, IDisposable).Dispose()
            End Try
        End Sub

        Private Sub ApplyLocalIndexSearch(query As String)
            If activeAccount IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(query) Then
                Dim list As List(Of MailMessageHeader) = CSharpImpl.__Assign(currentFolderHeaders, MailSearchIndex.Search(activeAccount.Id, query, 10000))
                ClearMessageSelectionKeepButtons()
                selectedHeader = Nothing
                selectedBody = Nothing
                ClearReadingPane()
                Dim shownCount As Integer = ApplyMessageListFilter()
                UpdateMessageCountStatus(shownCount)
                SetStatus("INDEX: " & list.Count.ToString() & " HIT(S)")
            End If
        End Sub

        Private Sub StartGlobalSearch(query As String)
            If Not globalSearchWorker.IsBusy AndAlso activeAccount IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(query) Then
                Dim text = ResolvePassword(activeAccount, promptIfMissing:=False)
                If Equals(text, Nothing) AndAlso Not EwsMailBridge.IsEwsAccount(activeAccount) Then
                    SetStatus("PASSWORD REQUIRED")
                    Return
                End If
                SetStatus("GLOBAL SEARCH…")
                globalSearchWorker.RunWorkerAsync(New GlobalSearchArgs With {
        .Account = activeAccount,
        .Password = text,
        .Query = query,
        .ImapFolderOnly = False
    })
            End If
        End Sub

        Private Sub StartImapFolderSearch(query As String)
            If Not globalSearchWorker.IsBusy AndAlso activeAccount IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(selectedFolderFullName) Then
                Dim text = ResolvePassword(activeAccount, promptIfMissing:=False)
                If Not Equals(text, Nothing) Then
                    SetStatus("IMAP SEARCH…")
                    globalSearchWorker.RunWorkerAsync(New GlobalSearchArgs With {
        .Account = activeAccount,
        .Password = text,
        .Query = query,
        .ImapFolderOnly = True,
        .Folder = selectedFolderFullName
    })
                End If
            End If
        End Sub

        Private Sub GlobalSearchWorker_DoWork(sender As Object, e As DoWorkEventArgs)
            Dim globalSearchArgs = CType(e.Argument, GlobalSearchArgs)
            Dim globalSearchResult As GlobalSearchResult = New GlobalSearchResult()
            Try
                If globalSearchArgs.ImapFolderOnly AndAlso Not String.IsNullOrWhiteSpace(globalSearchArgs.Folder) Then
                    globalSearchResult.Headers = mailService.SearchFolderHeaders(globalSearchArgs.Account, globalSearchArgs.Password, globalSearchArgs.Folder, globalSearchArgs.Query)
                Else
                    globalSearchResult.Headers = MailGlobalSearch.SearchAccount(globalSearchArgs.Account, globalSearchArgs.Password, mailService, globalSearchArgs.Query)
                End If
            Catch ex As Exception
                ProjectData.SetProjectError(ex)
                Dim ex2 = ex
                globalSearchResult.ErrorText = ex2.Message
                Call ProjectData.ClearProjectError()
            End Try
            e.Result = globalSearchResult
        End Sub

        Private Sub GlobalSearchWorker_Completed(sender As Object, e As RunWorkerCompletedEventArgs)
            Dim globalSearchResult As GlobalSearchResult = Nothing
            If e.Error IsNot Nothing Then
                SetStatus("SEARCH ERROR: " & e.Error.Message)
            ElseIf CSharpImpl.__Assign(globalSearchResult, TryCast(e.Result, GlobalSearchResult)) IsNot Nothing Then
                If Not String.IsNullOrEmpty(globalSearchResult.ErrorText) Then
                    SetStatus("SEARCH ERROR: " & globalSearchResult.ErrorText)
                    Return
                End If
                currentFolderHeaders = globalSearchResult.Headers
                Dim shownCount As Integer = ApplyMessageListFilter()
                UpdateMessageCountStatus(shownCount)
                SetStatus("SEARCH: " & shownCount.ToString() & " RESULT(S)")
            End If
        End Sub

        Private Function ApplyMessageListFilter() As Integer
            If currentFolderHeaders Is Nothing Then
                PopulateMessageList(Nothing)
                Return 0
            End If
            Dim text = ""
            If txtSearch IsNot Nothing Then
                text = txtSearch.Text.Trim()
            End If
            If text.Length = 0 Then
                crossFolderSearchActive = False
                PopulateMessageList(currentFolderHeaders)
                Return currentFolderHeaders.Count
            End If
            Dim text2 = text
            If text2.StartsWith("imap:", StringComparison.OrdinalIgnoreCase) AndAlso text2.Length > 5 Then
                text2 = text2.Substring(5).Trim()
            End If
            Dim list As List(Of MailMessageHeader) = New List(Of MailMessageHeader)()
            For Each currentFolderHeader In currentFolderHeaders
                If HeaderMatchesSearch(currentFolderHeader, text2) Then
                    list.Add(currentFolderHeader)
                End If
            Next
            If list.Count = 0 AndAlso activeAccount IsNot Nothing AndAlso text2.Length > 0 Then
                For Each item In MailSearchIndex.Search(activeAccount.Id, text2, 200)
                    If item IsNot Nothing AndAlso (String.IsNullOrWhiteSpace(selectedFolderFullName) OrElse String.Equals(item.Folder, selectedFolderFullName, StringComparison.OrdinalIgnoreCase)) Then
                        list.Add(item)
                    End If
                Next
            End If
            PopulateMessageList(list)
            Return list.Count
        End Function

        Private Shared Function HeaderMatchesSearch(hdr As MailMessageHeader, query As String) As Boolean
            If hdr Is Nothing OrElse String.IsNullOrEmpty(query) Then
                Return False
            End If
            Dim mailMessageHeader = hdr
            Dim text = If(Not Equals(mailMessageHeader.From, Nothing), mailMessageHeader.From, "")
            mailMessageHeader = hdr
            Dim text2 = If(Not Equals(mailMessageHeader.Subject, Nothing), mailMessageHeader.Subject, "")
            Return If(text.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 OrElse text2.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0, True, False)
        End Function

        Private Sub UpdateMessageCountStatus(shownCount As Integer)
            Dim num = If(currentFolderHeaders IsNot Nothing, currentFolderHeaders.Count, 0)
            Dim text As String = (If(txtSearch IsNot Nothing, txtSearch.Text.Trim(), ""))
            If text.Length > 0 AndAlso num > 0 Then
                SetStatus(shownCount.ToString() & " MATCH(ES) — " & num.ToString() & " IN FOLDER")
            Else
                SetStatus(shownCount.ToString() & " MESSAGE(S)")
            End If
        End Sub

        Private Sub PopulateMessageList(headers As List(Of MailMessageHeader))
            ClearMessageSelection()
            messageListOrder = New List(Of MailMessageHeader)()
            messageButtonsByUid.Clear()
            Dim contentPanel As Panel = messageHost.ContentPanel
            For Each control As Control In contentPanel.Controls
                If control IsNot lblMessageEmpty Then
                    contentPanel.Controls.Remove(control)
                    control.Dispose()
                End If
            Next
            If headers Is Nothing OrElse headers.Count = 0 Then
                lblMessageEmpty.Visible = True
                lblMessageEmpty.Text = "NO MESSAGES"
                messageHost.Relayout()
                Return
            End If
            lblMessageEmpty.Visible = False
            Dim num = 4
            Dim btnW = Math.Max(100, contentPanel.Width - 8)
            Dim flag = False
            If Not conversationViewEnabled Then
                For Each header In headers
                    If header IsNot Nothing Then
                        messageListOrder.Add(header)
                        AddMessageRowButton(contentPanel, header, "", 1, btnW, num, flag)
                        flag = Not flag
                        num += 48
                    End If
                Next
            Else
                Dim list = MailConversationGrouper.BuildThreadList(headers)
                For Each item In list
                    If item Is Nothing OrElse item.Representative Is Nothing Then
                        Continue For
                    End If
                    Dim mailThreadListItem = item
                    Dim text = If(Not Equals(mailThreadListItem.ThreadKey, Nothing), mailThreadListItem.ThreadKey, "")
                    If expandedThreadKeys.Contains(text) AndAlso item.Messages IsNot Nothing AndAlso item.Messages.Count > 1 Then
                        For Each message In item.Messages
                            If message IsNot Nothing Then
                                messageListOrder.Add(message)
                                AddMessageRowButton(contentPanel, message, text, 1, btnW, num, flag)
                                flag = Not flag
                                num += 48
                            End If
                        Next
                    Else
                        Dim representative = item.Representative
                        messageListOrder.Add(representative)
                        AddMessageRowButton(contentPanel, representative, text, item.MessageCount, btnW, num, flag)
                        flag = Not flag
                        num += 48
                    End If
                Next
            End If
            messageHost.Relayout()
        End Sub

        Private Sub AddMessageRowButton(content As Panel, hdr As MailMessageHeader, threadKey As String, threadCount As Integer, btnW As Integer, y As Integer, flip As Boolean)
            Dim mailMessageHeader = hdr
            Dim text = If(Not Equals(mailMessageHeader.Subject, Nothing), mailMessageHeader.Subject, "(NO SUBJECT)")
            mailMessageHeader = hdr
            Dim text2 = If(Not Equals(mailMessageHeader.From, Nothing), mailMessageHeader.From, "")
            Dim text3 = text
            Dim mailAccount = FindAccountById(hdr.AccountId)
            If mailAccount Is Nothing Then
                mailAccount = activeAccount
            End If
            If unifiedInboxActive AndAlso mailAccount IsNot Nothing Then
                Dim mailAccount2 = mailAccount
                Dim text4 = If(Not Equals(mailAccount2.EmailAddress, Nothing), mailAccount2.EmailAddress, mailAccount.Username)
                If String.IsNullOrWhiteSpace(text4) Then
                    text4 = mailAccount.Id
                End If
                If Not String.IsNullOrWhiteSpace(text4) Then
                    text3 = "[" & text4 & "] " & text3
                End If
            ElseIf crossFolderSearchActive AndAlso Not String.IsNullOrWhiteSpace(hdr.Folder) Then
                text3 = "[" & hdr.Folder & "] " & text3
            End If
            If text2.Length > 0 Then
                text3 = text3 & " — " & text2
            End If
            If crossFolderSearchActive AndAlso mailAccount IsNot Nothing Then
                If hdr.FtsRank <> 0.0 Then
                    text3 = "R" & hdr.FtsRank.ToString("0.0", CultureInfo.InvariantCulture) & " " & text3
                End If
                Dim text5 = MailSearchGlodaFts.TryGetBodySnippetForMessage(mailAccount.Id, hdr.Folder, hdr.Uid)
                If text5.Length = 0 Then
                    text5 = MailSearchIndex.TryGetBodyPreviewSnippet(mailAccount.Id, hdr.Folder, hdr.Uid, 72)
                End If
                If text5.Length > 0 Then
                    text3 = text3 & " · " & text5
                End If
            End If
            If threadCount > 1 Then
                Dim text6 = If(expandedThreadKeys.Contains(threadKey), "▾ ", "▸ ")
                text3 = text6 & text3 & " (" & threadCount.ToString() & ")"
            End If
            If text3.Length > 120 Then
                text3 = text3.Substring(0, 117) & "…"
            End If
            Dim val As FlatButton = MakeRowButton(text3.ToUpperInvariant(), flip, hdr.IsSeen)
            CType(CObj(val), Control).Tag = New MessageRowTag With {
        .Header = hdr,
        .Account = mailAccount,
        .ThreadKey = threadKey,
        .ThreadCount = threadCount
    }
            Dim location As Point = New Point(2, y)
            CType(CObj(val), Control).Location = location
            Dim size As Size = New Size(btnW, 44)
            CType(CObj(val), Control).Size = size
            CType(val, LCARSbuttonClass).ButtonTextHeight = 22
            AddHandler CType(CObj(val), Control).Click, AddressOf OnMessageButtonClick
            AddHandler CType(CObj(val), Button).DoubleClick, AddressOf OnMessageButtonDoubleClick
            content.Controls.Add(CType(CObj(val), Control))
            messageButtonsByUid(hdr.Uid) = val
        End Sub

        Private Function MakeRowButton(caption As String, flip As Boolean, seen As Boolean) As FlatButton
            Dim val As FlatButton = New FlatButton()
            CType(val, LCARSbuttonClass).ButtonText = caption
            CType(val, LCARSbuttonClass).Text = caption
            CType(val, LCARSbuttonClass).Clickable = True
            CType(val, LCARSbuttonClass).Beeping = True
            CType(val, LCARSbuttonClass).ButtonTextAlign = ContentAlignment.MiddleLeft
            CType(val, LCARSbuttonClass).ButtonTextHeight = 14
            If Not seen Then
                CType(val, LCARSbuttonClass).Color = CType(6, LCARScolorStyles)
            ElseIf flip Then
                CType(val, LCARSbuttonClass).Color = CType(1, LCARScolorStyles)
            Else
                CType(val, LCARSbuttonClass).Color = CType(5, LCARScolorStyles)
            End If
            Return val
        End Function

        Private Sub OnMessageButtonClick(sender As Object, e As EventArgs)
            Dim val As FlatButton = CType(If((TypeOf sender Is FlatButton), sender, Nothing), FlatButton)
            If val Is Nothing Then
                Return
            End If
            Dim messageRowTag As MessageRowTag = TryCast(CType(CObj(val), Control).Tag, MessageRowTag)
            Dim mailMessageHeader As MailMessageHeader = (If(messageRowTag IsNot Nothing, messageRowTag.Header, TryCast(CType(CObj(val), Control).Tag, MailMessageHeader)))
            If mailMessageHeader Is Nothing Then
                Return
            End If
            If messageRowTag IsNot Nothing AndAlso messageRowTag.Account IsNot Nothing Then
                activeAccount = messageRowTag.Account
            ElseIf Not String.IsNullOrWhiteSpace(mailMessageHeader.AccountId) Then
                Dim mailAccount = FindAccountById(mailMessageHeader.AccountId)
                If mailAccount IsNot Nothing Then
                    activeAccount = mailAccount
                End If
            End If
            If conversationViewEnabled AndAlso messageRowTag IsNot Nothing AndAlso messageRowTag.ThreadCount > 1 AndAlso (Control.ModifierKeys And Keys.Alt) = Keys.Alt AndAlso Not String.IsNullOrEmpty(messageRowTag.ThreadKey) Then
                If expandedThreadKeys.Contains(messageRowTag.ThreadKey) Then
                    expandedThreadKeys.Remove(messageRowTag.ThreadKey)
                Else
                    expandedThreadKeys.Add(messageRowTag.ThreadKey)
                End If
                ApplyMessageListFilter()
                Return
            End If
            Dim num = messageListOrder.FindIndex(Function(h) If(h IsNot Nothing AndAlso h.Uid = mailMessageHeader.Uid, True, False))
            Dim flag = (Control.ModifierKeys And Keys.Control) = Keys.Control
            If (Control.ModifierKeys And Keys.Shift) = Keys.Shift AndAlso lastMessageListIndex >= 0 AndAlso num >= 0 Then
                Dim num2 = Math.Min(lastMessageListIndex, num)
                Dim num3 = Math.Max(lastMessageListIndex, num)
                ClearMessageSelectionKeepButtons()
                Dim num4 = num3
                Dim num5 = num2

                While num5 <= num4
                    Dim mailMessageHeader2 = messageListOrder(num5)
                    If mailMessageHeader2 IsNot Nothing Then
                        selectedMessages(mailMessageHeader2.Uid) = mailMessageHeader2
                    End If

                    num5 = num5 + 1
                End While
            ElseIf flag Then
                If selectedMessages.ContainsKey(mailMessageHeader.Uid) Then
                    selectedMessages.Remove(mailMessageHeader.Uid)
                Else
                    selectedMessages(mailMessageHeader.Uid) = mailMessageHeader
                End If
            Else
                ClearMessageSelectionKeepButtons()
                selectedMessages(mailMessageHeader.Uid) = mailMessageHeader
            End If
            If num >= 0 Then
                lastMessageListIndex = num
            End If
            ApplyMessageSelectionColors()
            selectedMessageButton = val
            selectedHeader = mailMessageHeader
            UpdateComposeButtonsEnabled()
            StartFetchBody(mailMessageHeader)
        End Sub

        Private Sub OnMessageButtonDoubleClick(sender As Object, e As EventArgs)
            Dim val As FlatButton = CType(If((TypeOf sender Is FlatButton), sender, Nothing), FlatButton)
            If val Is Nothing Then
                Return
            End If
            Dim messageRowTag As MessageRowTag = TryCast(CType(CObj(val), Control).Tag, MessageRowTag)
            Dim mailMessageHeader As MailMessageHeader = (If(messageRowTag IsNot Nothing, messageRowTag.Header, TryCast(CType(CObj(val), Control).Tag, MailMessageHeader)))
            If mailMessageHeader IsNot Nothing AndAlso IsDraftsFolder(selectedFolderFullName) Then
                OpenImapDraft(mailMessageHeader)
            ElseIf conversationViewEnabled AndAlso messageRowTag IsNot Nothing AndAlso messageRowTag.ThreadCount > 1 AndAlso Not String.IsNullOrEmpty(messageRowTag.ThreadKey) Then
                If expandedThreadKeys.Contains(messageRowTag.ThreadKey) Then
                    expandedThreadKeys.Remove(messageRowTag.ThreadKey)
                Else
                    expandedThreadKeys.Add(messageRowTag.ThreadKey)
                End If
                ApplyMessageListFilter()
            End If
        End Sub

        Private Sub ApplyMessageSelectionColors()
            Dim messageRowTag As MessageRowTag = Nothing
            For Each item In messageButtonsByUid
                Dim value As FlatButton = item.Value
                Dim mailMessageHeader As MailMessageHeader = (If((CSharpImpl.__Assign(messageRowTag, TryCast(CType(CObj(value), Control).Tag, MessageRowTag)) IsNot Nothing), messageRowTag.Header, TryCast(CType(CObj(value), Control).Tag, MailMessageHeader)))
                If mailMessageHeader IsNot Nothing Then
                    If selectedMessages.ContainsKey(mailMessageHeader.Uid) Then
                        CType(value, LCARSbuttonClass).Color = CType(9, LCARScolorStyles)
                        Continue For
                    End If
                    Dim flip As Boolean = Decimal.Compare(Decimal.Remainder(New Decimal(mailMessageHeader.Uid), 2D), 0D) = 0
                    CType(value, LCARSbuttonClass).Color = MessageButtonColor(mailMessageHeader, flip)
                End If
            Next
        End Sub

        Private Sub ClearMessageSelectionKeepButtons()
            selectedMessages.Clear()
            selectedMessageButton = Nothing
            selectedHeader = Nothing
        End Sub

        Private Sub OnThreadsToggleClick(sender As Object, e As EventArgs)
            conversationViewEnabled = Not conversationViewEnabled
            If fbThreads IsNot Nothing Then
                CType(fbThreads, LCARSbuttonClass).Color = CType(If(conversationViewEnabled, 6, 8), LCARScolorStyles)
            End If
            If currentFolderHeaders IsNot Nothing Then
                ApplyMessageListFilter()
            End If
            If conversationViewEnabled Then
                SetStatus("THREADS ON — ALT+CLICK ROW TO EXPAND")
            Else
                SetStatus("THREADS OFF")
            End If
        End Sub

        Private Function SelectedUids() As List(Of ULong)
            Dim list As List(Of ULong) = New List(Of ULong)()
            If selectedMessages.Count <= 0 Then
                If selectedHeader IsNot Nothing Then
                    list.Add(selectedHeader.Uid)
                End If
                Return list
            End If
            For Each selectedMessage In selectedMessages
                list.Add(selectedMessage.Key)
            Next
            Return list
        End Function

        Private Shared Function MessageButtonColor(hdr As MailMessageHeader, flip As Boolean) As LCARScolorStyles
            If hdr IsNot Nothing AndAlso Not hdr.IsSeen Then
                Return CType(6, LCARScolorStyles)
            End If
            If flip Then
                Return CType(1, LCARScolorStyles)
            End If
            Return CType(5, LCARScolorStyles)
        End Function

        Private Sub StartFetchBody(hdr As MailMessageHeader)
            If bodyWorker.IsBusy OrElse activeAccount Is Nothing OrElse hdr Is Nothing Then
                Return
            End If
            Dim text = If(Not Equals(hdr.Folder, Nothing), hdr.Folder, "")
            If String.IsNullOrWhiteSpace(text) OrElse String.Equals(text, "__UNIFIED_INBOX__", StringComparison.Ordinal) Then
                text = If(Not Equals(selectedFolderFullName, Nothing), selectedFolderFullName, "")
            End If
            If String.IsNullOrWhiteSpace(text) OrElse String.Equals(text, "__UNIFIED_INBOX__", StringComparison.Ordinal) Then
                text = "INBOX"
            End If
            Dim body As MailMessageBody = Nothing
            If MailCache.TryLoadBody(activeAccount.Id, text, hdr.Uid, body) Then
                ShowMessageBody(hdr, body)
            Else
                lblReadEmpty.Visible = True
                lblReadEmpty.Text = "LOADING…"
                lblReadMeta.Visible = False
                lblReadSubject.Visible = False
                bodyLabel.Visible = False
                If htmlReadPane IsNot Nothing Then
                    htmlReadPane.Clear()
                End If
                SetAttachmentPaneVisible(visible:=False)
                readHost.Relayout()
            End If
            Dim text2 = ResolvePassword(activeAccount, promptIfMissing:=False)
            If Equals(text2, Nothing) Then
                SetStatus("PASSWORD REQUIRED")
                Return
            End If
            SetBusy(busy:=True)
            SetStatus("LOADING MESSAGE…")
            bodyWorker.RunWorkerAsync(New BodyWorkerArgs With {
        .Account = activeAccount,
        .Password = text2,
        .Folder = text,
        .Uid = hdr.Uid,
        .Header = hdr
    })
        End Sub

        Private Sub BodyWorker_DoWork(sender As Object, e As DoWorkEventArgs)
            Dim bodyWorkerArgs = CType(e.Argument, BodyWorkerArgs)
            Dim bodyWorkerResult As BodyWorkerResult = New BodyWorkerResult()
            bodyWorkerResult.Header = bodyWorkerArgs.Header
            Dim bodyWorkerResult2 = bodyWorkerResult
            Try
                bodyWorkerResult2.Body = mailService.FetchBody(bodyWorkerArgs.Account, bodyWorkerArgs.Password, bodyWorkerArgs.Folder, bodyWorkerArgs.Uid)
                mailService.MarkSeen(bodyWorkerArgs.Account, bodyWorkerArgs.Password, bodyWorkerArgs.Folder, bodyWorkerArgs.Uid)
                If bodyWorkerResult2.Header IsNot Nothing Then
                    bodyWorkerResult2.Header.IsSeen = True
                End If
            Catch ex As Exception
                ProjectData.SetProjectError(ex)
                Dim ex2 = ex
                bodyWorkerResult2.ErrorText = ex2.Message
                Dim body As MailMessageBody = Nothing
                If MailCache.TryLoadBody(bodyWorkerArgs.Account.Id, bodyWorkerArgs.Folder, bodyWorkerArgs.Uid, body) Then
                    bodyWorkerResult2.Body = body
                End If
                Call ProjectData.ClearProjectError()
            End Try
            e.Result = bodyWorkerResult2
        End Sub

                ''' Cannot convert MethodDeclarationSyntax, System.ArgumentOutOfRangeException: Exception of type 'System.ArgumentOutOfRangeException' was thrown. (Parameter 'node')
''' Actual value was LCARSMail.frmMail.BodyWorkerResult { Header: not null } bodyWorkerResult.
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.ConvertToVariableDeclaratorOrNull(IsPatternExpressionSyntax node) in /_/CodeConverter/VB/CommonConversions.cs:line 175
'''    at System.Linq.Enumerable.SelectListIterator`2.MoveNext()
'''    at System.Linq.Enumerable.WhereEnumerableIterator`1.MoveNext()
'''    at System.Collections.Generic.LargeArrayBuilder`1.AddRange(IEnumerable`1 items)
'''    at System.Collections.Generic.SparseArrayBuilder`1.ReserveOrAdd(IEnumerable`1 items)
'''    at System.Linq.Enumerable.Concat2Iterator`1.ToArray()
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.ConvertToDeclarationStatement(List`1 des, List`1 isPatternExpressions) in /_/CodeConverter/VB/CommonConversions.cs:line 105
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.InsertRequiredDeclarations(SyntaxList`1 convertedStatements, CSharpSyntaxNode originaNode) in /_/CodeConverter/VB/CommonConversions.cs:line 89
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.ConvertStatement(StatementSyntax statement, CSharpSyntaxVisitor`1 methodBodyVisitor) in /_/CodeConverter/VB/CommonConversions.cs:line 75
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.<>c__DisplayClass10_0.<ConvertStatements>b__0(StatementSyntax s) in /_/CodeConverter/VB/CommonConversions.cs:line 69
'''    at System.Linq.Enumerable.SelectManySingleSelectorIterator`2.MoveNext()
'''    at Microsoft.CodeAnalysis.SyntaxList`1.CreateNode(IEnumerable`1 nodes)
'''    at Microsoft.CodeAnalysis.SyntaxList`1..ctor(IEnumerable`1 nodes)
'''    at Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory.List[TNode](IEnumerable`1 nodes)
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.ConvertStatements(SyntaxList`1 statements, MethodBodyExecutableStatementVisitor iteratorState) in /_/CodeConverter/VB/CommonConversions.cs:line 69
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.ConvertBody(BlockSyntax body, ArrowExpressionClauseSyntax expressionBody, Boolean hasReturnType, MethodBodyExecutableStatementVisitor iteratorState) in /_/CodeConverter/VB/CommonConversions.cs:line 49
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitMethodDeclaration(MethodDeclarationSyntax node) in /_/CodeConverter/VB/NodesVisitor.cs:line 428
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingVisitorWrapper`1.Accept(SyntaxNode csNode, Boolean addSourceMapping) in /_/CodeConverter/VB/CommentConvertingVisitorWrapper.cs:line 20
''' 
''' Input:
''' 
''' 	private void BodyWorker_Completed(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
''' 	{
''' 		this.SetBusy(busy: false);
''' 		if (e.Error != null)
''' 		{
''' 			this.SetStatus("LOAD ERROR: " + e.Error.Message);
''' 		}
''' 		else
''' 		{
''' 			if (!(e.Result is LCARSMail.frmMail.BodyWorkerResult { Header: not null } bodyWorkerResult))
''' 			{
''' 				return;
''' 			}
''' 			if (bodyWorkerResult.Body != null)
''' 			{
''' 				this.ShowMessageBody(bodyWorkerResult.Header, bodyWorkerResult.Body);
''' 				if (this.activeAccount != null && bodyWorkerResult.Header != null)
''' 				{
''' 					string folder = ((this.selectedFolderFullName != null) ? this.selectedFolderFullName : bodyWorkerResult.Header.Folder);
''' 					LCARSMail.MailSearchIndex.UpsertBodyPreview(this.activeAccount.Id, folder, bodyWorkerResult.Header.Uid, bodyWorkerResult.Body.GetReadingPaneText());
''' 				}
''' 			}
''' 			if (this.selectedMessageButton != null && object.ReferenceEquals(System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(((System.Windows.Forms.Control)(object)this.selectedMessageButton).Tag), bodyWorkerResult.Header))
''' 			{
''' 				((LCARSbuttonClass)this.selectedMessageButton).Color = (LCARScolorStyles)9;
''' 			}
''' 			else if (this.selectedMessageButton != null && ((System.Windows.Forms.Control)(object)this.selectedMessageButton).Tag is LCARSMail.MailMessageHeader mailMessageHeader && mailMessageHeader.Uid == bodyWorkerResult.Header.Uid)
''' 			{
''' 				mailMessageHeader.IsSeen = true;
''' 				((LCARSbuttonClass)this.selectedMessageButton).Color = (LCARScolorStyles)9;
''' 			}
''' 			if (!string.IsNullOrEmpty(bodyWorkerResult.ErrorText))
''' 			{
''' 				this.SetStatus("LOAD WARNING: " + bodyWorkerResult.ErrorText);
''' 			}
''' 			else
''' 			{
''' 				this.SetStatus("MESSAGE LOADED");
''' 			}
''' 		}
''' 	}
''' 
''' 

        Private Sub ShowMessageBody(hdr As MailMessageHeader, body As MailMessageBody)
            selectedBody = body
            lblReadEmpty.Visible = False
            UpdateThreadContext(hdr)
            Dim mailMessageHeader = hdr
            Dim raw = If(Not Equals(mailMessageHeader.From, Nothing), mailMessageHeader.From, "")
            Dim text = ""
            If Date.Compare(hdr.DateUtc, Date.MinValue) <> 0 Then
                text = hdr.DateUtc.ToLocalTime().ToString("g")
            End If
            Dim text2 As String = (MailAddressHelper.FormatDisplayLine(raw) & "  ·  " & text).Trim()
            If lstThreadContext IsNot Nothing AndAlso lstThreadContext.Visible AndAlso lstThreadContext.Items.Count > 1 Then
                text2 = text2 & "  ·  " & lstThreadContext.Items.Count.ToString() & " IN THREAD"
            End If
            lblReadMeta.Text = text2.ToUpperInvariant()
            lblReadMeta.Visible = True
            Dim label = lblReadSubject
            mailMessageHeader = hdr
            label.Text = If(Not Equals(mailMessageHeader.Subject, Nothing), mailMessageHeader.Subject, "(NO SUBJECT)").ToUpperInvariant()
            lblReadSubject.Visible = True
            PopulateAttachmentList(hdr, body)
            TryPrefetchInlineAttachments(hdr, body)
            bodyLabel.Visible = False
            If htmlReadPane IsNot Nothing Then
                htmlReadPane.Visible = True
                htmlReadPane.ShowBody(body, If(body IsNot Nothing, body.Attachments, Nothing))
            Else
                Dim text3 = ""
                If body IsNot Nothing Then
                    text3 = body.GetReadingPaneText()
                End If
                If text3.Length = 0 Then
                    text3 = "(NO TEXT BODY)"
                End If
                bodyLabel.Text = text3
                bodyLabel.Visible = True
            End If
            LayoutReadingPaneControls()
            readHost.Relayout()
            UpdateAttachmentSaveEnabled()
            Dim num = MailCalendarInvitationBridge.TryImportFromMessageBody(body)
            If num > 0 Then
                Dim text4 = ""
                If MailCalendarInvitationBridge.LastImportedWithRecurrence > 0 Then
                    text4 = text4 & " · RRULE " & MailCalendarInvitationBridge.LastImportedWithRecurrence.ToString()
                End If
                If MailCalendarInvitationBridge.LastImportedWithCategory > 0 Then
                    text4 = text4 & " · CAT " & MailCalendarInvitationBridge.LastImportedWithCategory.ToString()
                End If
                If MailCalendarInvitationBridge.LastImportedWithAlarm > 0 Then
                    text4 = text4 & " · ALARM " & MailCalendarInvitationBridge.LastImportedWithAlarm.ToString()
                End If
                SetStatus("INVITE IMPORT +" & num.ToString() & " (MIME CALENDAR)" & text4 & " — CALDAV SYNC QUEUED")
            End If
        End Sub

        Private Sub PopulateAttachmentList(hdr As MailMessageHeader, body As MailMessageBody)
            If lstReadAttachments Is Nothing Then
                Return
            End If
            lstReadAttachments.Items.Clear()
            If (body Is Nothing OrElse body.Attachments Is Nothing OrElse body.Attachments.Count <= 0) AndAlso 0 = 0 Then
                SetAttachmentPaneVisible(visible:=False)
                Return
            End If
            Dim text = If(Not Equals(selectedFolderFullName, Nothing), selectedFolderFullName, If(hdr IsNot Nothing, hdr.Folder, ""))
            If String.IsNullOrWhiteSpace(text) Then
                text = "INBOX"
            End If
            Dim text2 = If(activeAccount IsNot Nothing, activeAccount.Id, "")
            For Each attachment In body.Attachments
                If attachment IsNot Nothing Then
                    If text2.Length > 0 AndAlso hdr IsNot Nothing Then
                        attachment.CachedPath = MailCache.TryGetCachedAttachmentPath(text2, text, hdr.Uid, attachment.PartIndex, attachment.FileName)
                    End If
                    lstReadAttachments.Items.Add(attachment)
                End If
            Next
            SetAttachmentPaneVisible(lstReadAttachments.Items.Count > 0)
            If lstReadAttachments.Items.Count > 0 Then
                lstReadAttachments.SelectedIndex = 0
            End If
        End Sub

        Private Sub SetAttachmentPaneVisible(visible As Boolean)
            If lblReadAttachments IsNot Nothing Then
                lblReadAttachments.Visible = visible
            End If
            If lstReadAttachments IsNot Nothing Then
                lstReadAttachments.Visible = visible
            End If
            If fbSaveAttachment IsNot Nothing Then
                CType(CObj(fbSaveAttachment), Control).Visible = visible
            End If
        End Sub

        Private Sub LayoutReadingPaneControls()
            Dim y = 4
            Dim label = lblReadMeta
            Dim location As Point = New Point(4, y)
            label.Location = location
            y = lblReadMeta.Bottom + 6
            Dim label2 = lblReadSubject
            location = New Point(4, y)
            label2.Location = location
            y = lblReadSubject.Bottom + 8
            Dim width As Integer = Math.Max(100, readHost.ContentPanel.Width - 12)
            If lstThreadContext IsNot Nothing AndAlso lstThreadContext.Visible Then
                Dim label3 = lblReadThread
                location = New Point(4, y)
                label3.Location = location
                y = lblReadThread.Bottom + 4
                Dim num = Math.Min(5, Math.Max(1, lstThreadContext.Items.Count))
                Dim height = num * lstThreadContext.ItemHeight + 4
                Dim listBox = lstThreadContext
                location = New Point(4, y)
                listBox.Location = location
                Dim listBox2 = lstThreadContext
                Dim size As Size = New Size(width, height)
                listBox2.Size = size
                y = lstThreadContext.Bottom + 8
            End If
            If lstReadAttachments IsNot Nothing AndAlso lstReadAttachments.Visible Then
                Dim label4 = lblReadAttachments
                location = New Point(4, y)
                label4.Location = location
                y = lblReadAttachments.Bottom + 4
                Dim num2 = Math.Min(4, Math.Max(1, lstReadAttachments.Items.Count))
                Dim height2 = num2 * lstReadAttachments.ItemHeight + 4
                Dim listBox3 = lstReadAttachments
                location = New Point(4, y)
                listBox3.Location = location
                Dim listBox4 = lstReadAttachments
                Dim size As Size = New Size(width, height2)
                listBox4.Size = size
                y = lstReadAttachments.Bottom + 6
                If fbSaveAttachment IsNot Nothing Then
                    Dim obj As FlatButton = fbSaveAttachment
                    location = New Point(4, y)
                    CType(CObj(obj), Control).Location = location
                    y = CType(CObj(fbSaveAttachment), Control).Bottom + 10
                End If
            End If
            Dim label5 = bodyLabel
            location = New Point(4, y)
            label5.Location = location
            If htmlReadPane IsNot Nothing AndAlso htmlReadPane.Visible Then
                Dim height3 As Integer = Math.Max(120, readHost.ContentPanel.Height - y - 8)
                Dim mailHtmlReadPane = htmlReadPane
                location = New Point(4, y)
                mailHtmlReadPane.Location = location
                Dim mailHtmlReadPane2 = htmlReadPane
                Dim size As Size = New Size(width, height3)
                mailHtmlReadPane2.Size = size
            End If
        End Sub

        Private Sub UpdateAttachmentSaveEnabled()
            If fbSaveAttachment IsNot Nothing AndAlso lstReadAttachments IsNot Nothing Then
                Dim flag = If(attachmentWorker IsNot Nothing AndAlso attachmentWorker.IsBusy, True, False)
                CType(fbSaveAttachment, LCARSbuttonClass).Clickable = If(lstReadAttachments.Visible AndAlso lstReadAttachments.SelectedIndex >= 0 AndAlso Not flag, True, False)
            End If
        End Sub

        Private Sub OnSaveAttachmentClick(sender As Object, e As EventArgs)
            Dim mailAttachmentInfo As MailAttachmentInfo = Nothing

            If selectedHeader Is Nothing OrElse activeAccount Is Nothing OrElse lstReadAttachments Is Nothing OrElse lstReadAttachments.SelectedItem Is Nothing OrElse attachmentWorker.IsBusy OrElse Not (CSharpImpl.__Assign(mailAttachmentInfo, TryCast(lstReadAttachments.SelectedItem, MailAttachmentInfo)) IsNot Nothing) Then
                Return
            End If
            Dim mailAttachmentInfo2 = mailAttachmentInfo
            Dim text As String = If(Not Equals(mailAttachmentInfo2.FileName, Nothing), mailAttachmentInfo2.FileName, "attachment").Trim()
            If text.Length = 0 Then
                text = "attachment"
            End If
            Dim text2 As String = Nothing
            Using saveFileDialog As SaveFileDialog = New SaveFileDialog()
                saveFileDialog.Title = "SAVE ATTACHMENT"
                saveFileDialog.FileName = text
                saveFileDialog.OverwritePrompt = True
                If saveFileDialog.ShowDialog(CType(Me, IWin32Window)) <> DialogResult.OK Then
                    Return
                End If
                text2 = saveFileDialog.FileName
            End Using
            Dim text3 = If(Not Equals(selectedFolderFullName, Nothing), selectedFolderFullName, selectedHeader.Folder)
            If String.IsNullOrWhiteSpace(text3) Then
                text3 = "INBOX"
            End If
            mailAttachmentInfo2 = mailAttachmentInfo
            Dim text4 As String = If(Not Equals(mailAttachmentInfo2.CachedPath, Nothing), mailAttachmentInfo2.CachedPath, "").Trim()
            If text4.Length = 0 Then
                text4 = MailCache.TryGetCachedAttachmentPath(activeAccount.Id, text3, selectedHeader.Uid, mailAttachmentInfo.PartIndex, mailAttachmentInfo.FileName)
            End If
            If Not String.IsNullOrWhiteSpace(text4) AndAlso File.Exists(text4) Then
                Try
                    File.Copy(text4, text2, overwrite:=True)
                    SetStatus("ATTACHMENT SAVED")
                    Return
                Catch ex As Exception
                    ProjectData.SetProjectError(ex)
                    Dim ex2 = ex
                    SetStatus("SAVE ERROR: " & ex2.Message)
                    Call ProjectData.ClearProjectError()
                    Return
                End Try
            End If
            Dim text5 = ResolvePassword(activeAccount, promptIfMissing:=False)
            If Equals(text5, Nothing) Then
                SetStatus("PASSWORD REQUIRED")
                Return
            End If
            SetBusy(busy:=True)
            SetStatus("DOWNLOADING ATTACHMENT…")
            attachmentWorker.RunWorkerAsync(New AttachmentWorkerArgs With {
        .Account = activeAccount,
        .Password = text5,
        .Folder = text3,
        .Uid = selectedHeader.Uid,
        .PartIndex = mailAttachmentInfo.PartIndex,
        .FileName = mailAttachmentInfo.FileName,
        .DestPath = text2
    })
        End Sub

        Private Sub AttachmentWorker_DoWork(sender As Object, e As DoWorkEventArgs)
            Dim attachmentWorkerArgs = CType(e.Argument, AttachmentWorkerArgs)
            Dim attachmentWorkerResult As AttachmentWorkerResult = New AttachmentWorkerResult()
            If attachmentWorkerArgs Is Nothing Then
                attachmentWorkerResult.ErrorText = "Nothing to save."
                e.Result = attachmentWorkerResult
                Return
            End If
            attachmentWorkerResult.DestPath = attachmentWorkerArgs.DestPath
            attachmentWorkerResult.PartIndex = attachmentWorkerArgs.PartIndex
            attachmentWorkerResult.FileName = attachmentWorkerArgs.FileName
            attachmentWorkerResult.Folder = attachmentWorkerArgs.Folder
            attachmentWorkerResult.Uid = attachmentWorkerArgs.Uid
            If attachmentWorkerArgs.Account IsNot Nothing Then
                Dim account = attachmentWorkerArgs.Account
                attachmentWorkerResult.AccountId = If(Not Equals(account.Id, Nothing), account.Id, "")
            End If
            Try
                Dim text = mailService.SaveAttachmentToFile(attachmentWorkerArgs.Account, attachmentWorkerArgs.Password, attachmentWorkerArgs.Folder, attachmentWorkerArgs.Uid, attachmentWorkerArgs.PartIndex, attachmentWorkerArgs.DestPath)
                If Not Equals(text, Nothing) AndAlso text.Length > 0 Then
                    attachmentWorkerResult.ErrorText = text
                ElseIf attachmentWorkerArgs.Account IsNot Nothing Then
                    MailCache.SaveAttachmentCache(attachmentWorkerArgs.Account.Id, attachmentWorkerArgs.Folder, attachmentWorkerArgs.Uid, attachmentWorkerArgs.PartIndex, attachmentWorkerArgs.FileName, attachmentWorkerArgs.DestPath)
                End If
            Catch ex As Exception
                ProjectData.SetProjectError(ex)
                Dim ex2 = ex
                attachmentWorkerResult.ErrorText = ex2.Message
                Call ProjectData.ClearProjectError()
            End Try
            e.Result = attachmentWorkerResult
        End Sub

        Private Sub AttachmentWorker_Completed(sender As Object, e As RunWorkerCompletedEventArgs)
            SetBusy(busy:=False)
            UpdateAttachmentSaveEnabled()
            If e.Error IsNot Nothing Then
                SetStatus("SAVE ERROR: " & e.Error.Message)
                Return
            End If
            Dim attachmentWorkerResult As AttachmentWorkerResult = TryCast(e.Result, AttachmentWorkerResult)
            If attachmentWorkerResult IsNot Nothing AndAlso Not String.IsNullOrEmpty(attachmentWorkerResult.ErrorText) Then
                SetStatus("SAVE ERROR: " & attachmentWorkerResult.ErrorText)
                Return
            End If
            If attachmentWorkerResult IsNot Nothing AndAlso selectedBody IsNot Nothing AndAlso selectedBody.Attachments IsNot Nothing Then
                Dim cachedPath = MailCache.TryGetCachedAttachmentPath(attachmentWorkerResult.AccountId, attachmentWorkerResult.Folder, attachmentWorkerResult.Uid, attachmentWorkerResult.PartIndex, attachmentWorkerResult.FileName)
                For Each attachment In selectedBody.Attachments
                    If attachment IsNot Nothing AndAlso attachment.PartIndex = attachmentWorkerResult.PartIndex Then
                        attachment.CachedPath = cachedPath
                        Exit For
                    End If
                Next
            End If
            SetStatus("ATTACHMENT SAVED")
        End Sub

        Private Sub ClearReadingPane()
            selectedBody = Nothing
            lblReadMeta.Visible = False
            lblReadSubject.Visible = False
            bodyLabel.Visible = False
            If htmlReadPane IsNot Nothing Then
                htmlReadPane.Clear()
                htmlReadPane.Visible = False
            End If
            SetAttachmentPaneVisible(visible:=False)
            If lstReadAttachments IsNot Nothing Then
                lstReadAttachments.Items.Clear()
            End If
            lblReadEmpty.Visible = True
            lblReadEmpty.Text = "SELECT A MESSAGE"
            readHost.Relayout()
            UpdateComposeButtonsEnabled()
        End Sub

        Private Sub ClearMessageList()
            currentFolderHeaders = Nothing
            ClearMessageSelection()
            Dim contentPanel As Panel = messageHost.ContentPanel
            For Each control As Control In contentPanel.Controls
                If control IsNot lblMessageEmpty Then
                    contentPanel.Controls.Remove(control)
                    control.Dispose()
                End If
            Next
            lblMessageEmpty.Visible = True
            lblMessageEmpty.Text = "SELECT A FOLDER"
            messageHost.Relayout()
        End Sub

        Private Sub ClearFolderSelection()
            selectedFolderButton = Nothing
        End Sub

        Private Sub ClearMessageSelection()
            selectedMessageButton = Nothing
            selectedHeader = Nothing
            selectedMessages.Clear()
            UpdateComposeButtonsEnabled()
        End Sub

        Private Sub OnNewClick(sender As Object, e As EventArgs)
            OpenCompose("", "", "", "")
        End Sub

        Private Sub OnReplyClick(sender As Object, e As EventArgs)
            If selectedHeader IsNot Nothing Then
                Dim mailMessageHeader = selectedHeader
                Dim toAddrs As String = If(Not Equals(mailMessageHeader.From, Nothing), mailMessageHeader.From, "").Trim()
                Dim subject = SubjectWithPrefix("Re:", selectedHeader.Subject)
                Dim body = BuildQuotedReplyBody(selectedHeader)
                OpenCompose(toAddrs, "", subject, body)
            End If
        End Sub

        Private Sub OnReplyAllClick(sender As Object, e As EventArgs)
            If selectedHeader IsNot Nothing Then
                Dim toOut = ""
                Dim ccOut = ""
                BuildReplyAllAddresses(selectedHeader, activeAccount, toOut, ccOut)
                Dim subject = SubjectWithPrefix("Re:", selectedHeader.Subject)
                Dim body = BuildQuotedReplyBody(selectedHeader)
                OpenCompose(toOut, ccOut, subject, body)
            End If
        End Sub

        Private Sub OnForwardClick(sender As Object, e As EventArgs)
            If selectedHeader IsNot Nothing Then
                Dim subject = SubjectWithPrefix("Fwd:", selectedHeader.Subject)
                Dim body = BuildQuotedForwardBody(selectedHeader)
                OpenCompose("", "", subject, body)
            End If
        End Sub

        Private Function MutationSourceFolder() As String
            If selectedHeader IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(selectedHeader.Folder) AndAlso Not String.Equals(selectedHeader.Folder, "__UNIFIED_INBOX__", StringComparison.Ordinal) Then
                Return selectedHeader.Folder
            End If
            If Not String.IsNullOrWhiteSpace(selectedFolderFullName) AndAlso Not String.Equals(selectedFolderFullName, "__UNIFIED_INBOX__", StringComparison.Ordinal) Then
                Return selectedFolderFullName
            End If
            Return "INBOX"
        End Function

        Private Sub OnDeleteClick(sender As Object, e As EventArgs)
            If selectedHeader Is Nothing AndAlso selectedMessages.Count = 0 OrElse activeAccount Is Nothing Then
                Return
            End If
            Dim text As String = MutationSourceFolder()
            If String.IsNullOrWhiteSpace(text) OrElse mutationWorker.IsBusy Then
                Return
            End If
            Dim text2 = ResolvePassword(activeAccount, promptIfMissing:=False)
            If Equals(text2, Nothing) Then
                SetStatus("PASSWORD REQUIRED")
                Return
            End If
            Dim list As List(Of ULong) = SelectedUids()
            If list.Count <> 0 Then
                StartMutation("delete", text, Nothing, list, text2)
            End If
        End Sub

        Private Sub OnMoveClick(sender As Object, e As EventArgs)
            If selectedHeader Is Nothing AndAlso selectedMessages.Count = 0 OrElse activeAccount Is Nothing Then
                Return
            End If
            Dim text As String = MutationSourceFolder()
            If String.IsNullOrWhiteSpace(text) OrElse mutationWorker.IsBusy Then
                Return
            End If
            Dim text2 = ShowMoveFolderPicker(text)
            If String.IsNullOrWhiteSpace(text2) Then
                Return
            End If
            Dim text3 = ResolvePassword(activeAccount, promptIfMissing:=False)
            If Equals(text3, Nothing) Then
                SetStatus("PASSWORD REQUIRED")
                Return
            End If
            Dim list As List(Of ULong) = SelectedUids()
            If list.Count <> 0 Then
                StartMutation("move", text, text2, list, text3)
            End If
        End Sub

        Private Function ShowMoveFolderPicker(excludeFullName As String) As String
            Dim list As List(Of MailFolderInfo) = FoldersForActiveAccount()
            If list Is Nothing OrElse list.Count = 0 Then
                SetStatus("NO FOLDERS — REFRESH")
                Return Nothing
            End If
            Dim mailFolderPickerDialog As MailFolderPickerDialog = New MailFolderPickerDialog(list, excludeFullName)
            Try
                If CType(CObj(mailFolderPickerDialog), Form).ShowDialog(CType(Me, IWin32Window)) = DialogResult.OK Then
                    Return mailFolderPickerDialog.SelectedFolderFullName
                End If

            Finally
                If CObj(mailFolderPickerDialog) IsNot Nothing Then CType(CObj(mailFolderPickerDialog), IDisposable).Dispose()
            End Try
            Return Nothing
        End Function

        Private Function FoldersForActiveAccount() As List(Of MailFolderInfo)
            If activeAccount Is Nothing OrElse lastFolderSections Is Nothing Then
                Return Nothing
            End If
            Dim mailAccount = activeAccount
            Dim b = If(Not Equals(mailAccount.Id, Nothing), mailAccount.Id, "")
            For Each lastFolderSection In lastFolderSections
                If lastFolderSection IsNot Nothing AndAlso lastFolderSection.Account IsNot Nothing Then
                    mailAccount = lastFolderSection.Account
                    If String.Equals(If(Not Equals(mailAccount.Id, Nothing), mailAccount.Id, ""), b, StringComparison.OrdinalIgnoreCase) Then
                        Return lastFolderSection.Folders
                    End If
                End If
            Next
            Return Nothing
        End Function

        Private Sub StartMutation(kind As String, folder As String, destFolder As String, uids As List(Of ULong), password As String)
            If Not mutationWorker.IsBusy AndAlso activeAccount IsNot Nothing AndAlso uids IsNot Nothing AndAlso uids.Count <> 0 Then
                ClearReadingPane()
                ClearMessageSelection()
                SetBusy(busy:=True)
                Dim text As String = uids.Count.ToString()
                If String.Equals(kind, "move", StringComparison.OrdinalIgnoreCase) Then
                    SetStatus("MOVING " & text & " MESSAGE(S)…")
                Else
                    SetStatus("DELETING " & text & " MESSAGE(S)…")
                End If
                mutationWorker.RunWorkerAsync(New MutationWorkerArgs With {
        .Kind = kind,
        .Account = activeAccount,
        .Password = password,
        .Folder = folder,
        .DestFolder = destFolder,
        .Uids = uids
    })
            End If
        End Sub

        Private Sub MutationWorker_DoWork(sender As Object, e As DoWorkEventArgs)
            Dim mutationWorkerArgs = CType(e.Argument, MutationWorkerArgs)
            Dim mutationWorkerResult As MutationWorkerResult = New MutationWorkerResult()
            Dim mutationWorkerResult2 = mutationWorkerResult
            Dim mutationWorkerArgs2 = mutationWorkerArgs
            mutationWorkerResult2.Kind = If(Not Equals(mutationWorkerArgs2.Kind, Nothing), mutationWorkerArgs2.Kind, "")
            Dim mutationWorkerResult3 = mutationWorkerResult
            If mutationWorkerArgs Is Nothing OrElse mutationWorkerArgs.Account Is Nothing OrElse mutationWorkerArgs.Uids Is Nothing OrElse mutationWorkerArgs.Uids.Count = 0 Then
                mutationWorkerResult3.ErrorText = "Nothing to change."
                e.Result = mutationWorkerResult3
                Return
            End If
            Try
                If String.Equals(mutationWorkerArgs.Kind, "move", StringComparison.OrdinalIgnoreCase) Then
                    mailService.MoveMessages(mutationWorkerArgs.Account, mutationWorkerArgs.Password, mutationWorkerArgs.Folder, mutationWorkerArgs.DestFolder, mutationWorkerArgs.Uids)
                Else
                    mailService.DeleteMessages(mutationWorkerArgs.Account, mutationWorkerArgs.Password, mutationWorkerArgs.Folder, mutationWorkerArgs.Uids)
                    For Each uid In mutationWorkerArgs.Uids
                        MailCache.RemoveMessageCache(mutationWorkerArgs.Account.Id, mutationWorkerArgs.Folder, uid)
                        MailSearchIndex.RemoveMessage(mutationWorkerArgs.Account.Id, mutationWorkerArgs.Folder, uid)
                        mutationWorkerResult = mutationWorkerResult3
                        mutationWorkerResult.CachePurgedCount += 1
                    Next
                End If
            Catch ex As Exception
                ProjectData.SetProjectError(ex)
                Dim ex2 = ex
                mutationWorkerResult3.ErrorText = ex2.Message
                Call ProjectData.ClearProjectError()
            End Try
            e.Result = mutationWorkerResult3
        End Sub

        Private Sub MutationWorker_Completed(sender As Object, e As RunWorkerCompletedEventArgs)
            SetBusy(busy:=False)
            If e.Error IsNot Nothing Then
                SetStatus("MAIL ERROR: " & e.Error.Message)
                RefreshCurrentFolderHeaders()
                Return
            End If
            Dim mutationWorkerResult As MutationWorkerResult = TryCast(e.Result, MutationWorkerResult)
            If mutationWorkerResult IsNot Nothing AndAlso Not String.IsNullOrEmpty(mutationWorkerResult.ErrorText) Then
                SetStatus("MAIL ERROR: " & mutationWorkerResult.ErrorText)
            ElseIf mutationWorkerResult IsNot Nothing AndAlso String.Equals(mutationWorkerResult.Kind, "move", StringComparison.OrdinalIgnoreCase) Then
                SetStatus("MESSAGE MOVED")
            ElseIf mutationWorkerResult IsNot Nothing AndAlso mutationWorkerResult.CachePurgedCount > 0 Then
                SetStatus("DELETED — CACHE EXPUNGED " & mutationWorkerResult.CachePurgedCount.ToString())
            Else
                SetStatus("MESSAGE DELETED")
            End If
            RefreshCurrentFolderHeaders()
        End Sub

        Private Sub RefreshCurrentFolderHeaders()
            If Not String.IsNullOrWhiteSpace(selectedFolderFullName) Then
                StartSyncHeaders(selectedFolderFullName)
            End If
        End Sub

                ''' Cannot convert MethodDeclarationSyntax, System.ArgumentOutOfRangeException: Exception of type 'System.ArgumentOutOfRangeException' was thrown. (Parameter 'node')
''' Actual value was LCARSMail.frmMail.FolderRowTag { Folder: not null } folderRowTag.
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.ConvertToVariableDeclaratorOrNull(IsPatternExpressionSyntax node) in /_/CodeConverter/VB/CommonConversions.cs:line 175
'''    at System.Linq.Enumerable.SelectListIterator`2.MoveNext()
'''    at System.Linq.Enumerable.WhereEnumerableIterator`1.MoveNext()
'''    at System.Collections.Generic.LargeArrayBuilder`1.AddRange(IEnumerable`1 items)
'''    at System.Collections.Generic.SparseArrayBuilder`1.ReserveOrAdd(IEnumerable`1 items)
'''    at System.Linq.Enumerable.Concat2Iterator`1.ToArray()
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.ConvertToDeclarationStatement(List`1 des, List`1 isPatternExpressions) in /_/CodeConverter/VB/CommonConversions.cs:line 105
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.InsertRequiredDeclarations(SyntaxList`1 convertedStatements, CSharpSyntaxNode originaNode) in /_/CodeConverter/VB/CommonConversions.cs:line 89
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.ConvertStatement(StatementSyntax statement, CSharpSyntaxVisitor`1 methodBodyVisitor) in /_/CodeConverter/VB/CommonConversions.cs:line 75
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.<>c__DisplayClass10_0.<ConvertStatements>b__0(StatementSyntax s) in /_/CodeConverter/VB/CommonConversions.cs:line 69
'''    at System.Linq.Enumerable.SelectManySingleSelectorIterator`2.MoveNext()
'''    at Microsoft.CodeAnalysis.SyntaxList`1.CreateNode(IEnumerable`1 nodes)
'''    at Microsoft.CodeAnalysis.SyntaxList`1..ctor(IEnumerable`1 nodes)
'''    at Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory.List[TNode](IEnumerable`1 nodes)
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.ConvertStatements(SyntaxList`1 statements, MethodBodyExecutableStatementVisitor iteratorState) in /_/CodeConverter/VB/CommonConversions.cs:line 69
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.ConvertBody(BlockSyntax body, ArrowExpressionClauseSyntax expressionBody, Boolean hasReturnType, MethodBodyExecutableStatementVisitor iteratorState) in /_/CodeConverter/VB/CommonConversions.cs:line 49
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitMethodDeclaration(MethodDeclarationSyntax node) in /_/CodeConverter/VB/NodesVisitor.cs:line 428
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingVisitorWrapper`1.Accept(SyntaxNode csNode, Boolean addSourceMapping) in /_/CodeConverter/VB/CommentConvertingVisitorWrapper.cs:line 20
''' 
''' Input:
''' 
''' 	private void TrySelectDraftsFolder()
''' 	{
''' 		if (this.folderHost == null || this.activeAccount == null)
''' 		{
''' 			return;
''' 		}
''' 		LCARSMail.MailAccount mailAccount = this.activeAccount;
''' 		string b = ((mailAccount.Id != null) ? mailAccount.Id : "");
''' 		foreach (System.Windows.Forms.Control control in this.folderHost.ContentPanel.Controls)
''' 		{
''' 			FlatButton val = (FlatButton)(object)((control is FlatButton) ? control : null);
''' 			if (val == null || !(((System.Windows.Forms.Control)(object)val).Tag is LCARSMail.frmMail.FolderRowTag { Folder: not null } folderRowTag) || !string.Equals((folderRowTag.Account != null) ? folderRowTag.Account.Id : "", b, System.StringComparison.OrdinalIgnoreCase) || !LCARSMail.frmMail.IsDraftsFolder(folderRowTag.Folder.FullName))
''' 			{
''' 				continue;
''' 			}
''' 			OnFolderButtonClick(val, System.EventArgs.Empty);
''' 			this.SetStatus("DRAFTS FOLDER SELECTED");
''' 			return;
''' 		}
''' 		this.SetStatus("DRAFTS FOLDER NOT FOUND — REFRESH FOLDERS");
''' 	}
''' 
''' 

        Private Shared Function IsDraftsFolder(folderFullName As String) As Boolean
            Dim text As String = If(Not Equals(folderFullName, Nothing), folderFullName, "").Trim().ToLowerInvariant()
            If text.Length = 0 Then
                Return False
            End If
            Return If(Operators.CompareString(text, "drafts", TextCompare:=False) = 0 OrElse text.EndsWith("/drafts") OrElse text.EndsWith(".drafts"), True, False)
        End Function

        Private Sub OpenImapDraft(hdr As MailMessageHeader)
            If activeAccount Is Nothing OrElse hdr Is Nothing Then
                Return
            End If
            Dim text = ResolvePassword(activeAccount, promptIfMissing:=True)
            If Equals(text, Nothing) Then
                SetStatus("PASSWORD REQUIRED")
                Return
            End If
            Dim mailMessageHeader = hdr
            Dim text2 = If(Not Equals(mailMessageHeader.Folder, Nothing), mailMessageHeader.Folder, selectedFolderFullName)
            If String.IsNullOrWhiteSpace(text2) Then
                text2 = "Drafts"
            End If
            SetBusy(busy:=True)
            SetStatus("LOADING DRAFT…")
            Try
                Dim mailMessageBody = mailService.FetchBody(activeAccount, text, text2, hdr.Uid)
                Dim body = If(mailMessageBody IsNot Nothing, mailMessageBody.TextBody, "")
                Dim bodyHtml = If(mailMessageBody IsNot Nothing, mailMessageBody.HtmlBody, "")
                mailMessageHeader = hdr
                Dim toAddrs = If(Not Equals(mailMessageHeader.ToAddresses, Nothing), mailMessageHeader.ToAddresses, "")
                mailMessageHeader = hdr
                Dim ccAddrs = If(Not Equals(mailMessageHeader.CcAddresses, Nothing), mailMessageHeader.CcAddresses, "")
                Dim folder = text2
                Dim uid = hdr.Uid
                mailMessageHeader = hdr
                Me.OpenComposeFromImap(folder, uid, toAddrs, ccAddrs, If(Not Equals(mailMessageHeader.Subject, Nothing), mailMessageHeader.Subject, ""), body, bodyHtml)
            Catch ex As Exception
                ProjectData.SetProjectError(ex)
                Dim ex2 = ex
                SetStatus("DRAFT LOAD ERROR: " & ex2.Message)
                Call ProjectData.ClearProjectError()
            Finally
                SetBusy(busy:=False)
            End Try
        End Sub

        Private Sub OpenComposeFromDraft(draft As MailDraft)
            If draft IsNot Nothing Then
                OpenComposeInternal(draft.ToAddresses, draft.CcAddresses, draft.Subject, draft.BodyPlain, draft.BodyHtml, draft, Nothing, 0UL)
            End If
        End Sub

        Private Sub OpenComposeFromImap(folder As String, uid As ULong, toAddrs As String, ccAddrs As String, subject As String, body As String, bodyHtml As String)
            OpenComposeInternal(toAddrs, ccAddrs, subject, body, bodyHtml, Nothing, folder, uid)
        End Sub

        Private Sub UpdateThreadContext(hdr As MailMessageHeader)
            If lstThreadContext Is Nothing OrElse lblReadThread Is Nothing Then
                Return
            End If
            lstThreadContext.Items.Clear()
            lstThreadContext.Visible = False
            lblReadThread.Visible = False
            If hdr Is Nothing OrElse currentFolderHeaders Is Nothing OrElse currentFolderHeaders.Count < 2 Then
                Return
            End If
            Dim list = MailConversationGrouper.BuildThreadList(currentFolderHeaders)
            Dim mailThreadListItem As MailThreadListItem = Nothing
            For Each item In list
                If item Is Nothing OrElse item.Messages Is Nothing Then
                    Continue For
                End If
                For Each message In item.Messages
                    If message IsNot Nothing AndAlso message.Uid = hdr.Uid Then
                        mailThreadListItem = item
                        Exit For
                    End If
                Next
                If mailThreadListItem Is Nothing Then
                    Continue For
                End If
                Exit For
            Next
            If mailThreadListItem Is Nothing OrElse mailThreadListItem.MessageCount <= 1 Then
                Return
            End If
            lblReadThread.Text = "THREAD (" & mailThreadListItem.MessageCount.ToString() & ")"
            lblReadThread.Visible = True
            lstThreadContext.Visible = True
            For Each message2 In mailThreadListItem.Messages
                If message2 IsNot Nothing Then
                    Dim text = If(message2.Uid = hdr.Uid, "▸ ", "  ")
                    Dim mailMessageHeader = message2
                    Dim text2 = If(Not Equals(mailMessageHeader.Subject, Nothing), mailMessageHeader.Subject, "")
                    If text2.Length > 40 Then
                        text2 = text2.Substring(0, 37) & "…"
                    End If
                    Dim array As String() = New String(5) {text, message2.DateUtc.ToLocalTime().ToString("g"), " — ", text2, " — ", Nothing}
                    mailMessageHeader = message2
                    array(5) = If(Not Equals(mailMessageHeader.From, Nothing), mailMessageHeader.From, "")
                    Dim text3 = String.Concat(array)
                    If text3.Length > 100 Then
                        text3 = text3.Substring(0, 97) & "…"
                    End If
                    lstThreadContext.Items.Add(New ThreadContextListItem(message2, text3.ToUpperInvariant()))
                End If
            Next
        End Sub

                ''' Cannot convert MethodDeclarationSyntax, System.ArgumentOutOfRangeException: Exception of type 'System.ArgumentOutOfRangeException' was thrown. (Parameter 'node')
''' Actual value was LCARSMail.frmMail.ThreadContextListItem { Header: not null } threadContextListItem.
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.ConvertToVariableDeclaratorOrNull(IsPatternExpressionSyntax node) in /_/CodeConverter/VB/CommonConversions.cs:line 175
'''    at System.Linq.Enumerable.SelectListIterator`2.MoveNext()
'''    at System.Linq.Enumerable.WhereEnumerableIterator`1.MoveNext()
'''    at System.Collections.Generic.LargeArrayBuilder`1.AddRange(IEnumerable`1 items)
'''    at System.Collections.Generic.SparseArrayBuilder`1.ReserveOrAdd(IEnumerable`1 items)
'''    at System.Linq.Enumerable.Concat2Iterator`1.ToArray()
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.ConvertToDeclarationStatement(List`1 des, List`1 isPatternExpressions) in /_/CodeConverter/VB/CommonConversions.cs:line 105
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.InsertRequiredDeclarations(SyntaxList`1 convertedStatements, CSharpSyntaxNode originaNode) in /_/CodeConverter/VB/CommonConversions.cs:line 89
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.ConvertStatement(StatementSyntax statement, CSharpSyntaxVisitor`1 methodBodyVisitor) in /_/CodeConverter/VB/CommonConversions.cs:line 75
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.<>c__DisplayClass10_0.<ConvertStatements>b__0(StatementSyntax s) in /_/CodeConverter/VB/CommonConversions.cs:line 69
'''    at System.Linq.Enumerable.SelectManySingleSelectorIterator`2.MoveNext()
'''    at Microsoft.CodeAnalysis.SyntaxList`1.CreateNode(IEnumerable`1 nodes)
'''    at Microsoft.CodeAnalysis.SyntaxList`1..ctor(IEnumerable`1 nodes)
'''    at Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory.List[TNode](IEnumerable`1 nodes)
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.ConvertStatements(SyntaxList`1 statements, MethodBodyExecutableStatementVisitor iteratorState) in /_/CodeConverter/VB/CommonConversions.cs:line 69
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.ConvertBody(BlockSyntax body, ArrowExpressionClauseSyntax expressionBody, Boolean hasReturnType, MethodBodyExecutableStatementVisitor iteratorState) in /_/CodeConverter/VB/CommonConversions.cs:line 49
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitMethodDeclaration(MethodDeclarationSyntax node) in /_/CodeConverter/VB/NodesVisitor.cs:line 428
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingVisitorWrapper`1.Accept(SyntaxNode csNode, Boolean addSourceMapping) in /_/CodeConverter/VB/CommentConvertingVisitorWrapper.cs:line 20
''' 
''' Input:
''' 
''' 	private void OnThreadContextSelected(object sender, System.EventArgs e)
''' 	{
''' 		if (this.lstThreadContext != null && this.lstThreadContext.SelectedItem != null && this.lstThreadContext.SelectedItem is LCARSMail.frmMail.ThreadContextListItem { Header: not null } threadContextListItem && (this.selectedHeader == null || this.selectedHeader.Uid != threadContextListItem.Header.Uid))
''' 		{
''' 			this.selectedHeader = threadContextListItem.Header;
''' 			if (this.messageButtonsByUid.ContainsKey(threadContextListItem.Header.Uid))
''' 			{
''' 				this.selectedMessageButton = this.messageButtonsByUid[threadContextListItem.Header.Uid];
''' 			}
''' 			this.ClearMessageSelectionKeepButtons();
''' 			this.selectedMessages[threadContextListItem.Header.Uid] = threadContextListItem.Header;
''' 			this.ApplyMessageSelectionColors();
''' 			this.StartFetchBody(threadContextListItem.Header);
''' 		}
''' 	}
''' 
''' 

        Private Sub OpenCompose(toAddrs As String, ccAddrs As String, subject As String, body As String, Optional bodyHtml As String = Nothing)
            OpenComposeInternal(toAddrs, ccAddrs, subject, body, bodyHtml, Nothing, Nothing, 0UL)
        End Sub

        Private Sub OpenComposeInternal(toAddrs As String, ccAddrs As String, subject As String, body As String, bodyHtml As String, linkedDraft As MailDraft, imapFolder As String, imapUid As ULong)
            If activeAccount Is Nothing Then
                SetStatus("IMPORT A THUNDERBIRD PROFILE")
                Return
            End If
            Dim text = ResolvePassword(activeAccount, promptIfMissing:=True)
            If Equals(text, Nothing) Then
                SetStatus("PASSWORD REQUIRED")
                Return
            End If
            Dim frmCompose2 As frmCompose = New frmCompose(activeAccount, text)
            Try
                If linkedDraft IsNot Nothing Then
                    frmCompose2.ApplyDraftLink(linkedDraft)
                Else
                    frmCompose2.ComposeTo = toAddrs
                    frmCompose2.ComposeCc = ccAddrs
                    frmCompose2.ComposeSubject = subject
                    If Not String.IsNullOrWhiteSpace(bodyHtml) Then
                        frmCompose2.ComposeBodyHtml = bodyHtml
                    Else
                        frmCompose2.ComposeBody = body
                    End If
                End If
                If imapUid <> 0 AndAlso Not String.IsNullOrWhiteSpace(imapFolder) Then
                    frmCompose2.ApplyImapDraftLink(imapFolder, imapUid)
                End If
                frmCompose2.SendStatusReporter = Sub(msg) SetStatus(msg)
                Dim dialogResult = CType(CObj(frmCompose2), Form).ShowDialog(CType(Me, IWin32Window))
                If dialogResult = DialogResult.OK Then
                    SetStatus("MESSAGE SENT")
                End If

            Finally
                If frmCompose2 IsNot Nothing Then CType(frmCompose2, IDisposable).Dispose()
            End Try
        End Sub

        Private Function BodyTextForQuote(hdr As MailMessageHeader) As String
            If selectedBody IsNot Nothing Then
                Dim readingPaneText As String = selectedBody.GetReadingPaneText()
                If Not String.IsNullOrWhiteSpace(readingPaneText) Then
                    Return readingPaneText
                End If
            End If
            If hdr Is Nothing OrElse activeAccount Is Nothing Then
                Return ""
            End If
            Dim text = If(Not Equals(selectedFolderFullName, Nothing), selectedFolderFullName, hdr.Folder)
            If String.IsNullOrWhiteSpace(text) Then
                text = "INBOX"
            End If
            Dim body As MailMessageBody = Nothing
            If MailCache.TryLoadBody(activeAccount.Id, text, hdr.Uid, body) AndAlso body IsNot Nothing Then
                Return body.GetReadingPaneText()
            End If
            If bodyLabel IsNot Nothing AndAlso bodyLabel.Visible Then
                Dim text2 = bodyLabel.Text
                Dim text3 = If(Not Equals(text2, Nothing), text2, "")
                If text3.Length > 0 AndAlso Not String.Equals(text3, "(NO TEXT BODY)", StringComparison.OrdinalIgnoreCase) Then
                    Return text3
                End If
            End If
            Return ""
        End Function

        Private Function BuildQuotedReplyBody(hdr As MailMessageHeader) As String
            If hdr Is Nothing Then
                Return ""
            End If
            Dim text = BodyTextForQuote(hdr)
            Dim text2 = If(Not Equals(hdr.From, Nothing), hdr.From, "")
            Dim text3 = ""
            If Date.Compare(hdr.DateUtc, Date.MinValue) <> 0 Then
                text3 = hdr.DateUtc.ToLocalTime().ToString("f")
            End If
            Dim stringBuilder As StringBuilder = New StringBuilder()
            stringBuilder.AppendLine()
            stringBuilder.AppendLine()
            If text3.Length > 0 OrElse text2.Length > 0 Then
                stringBuilder.Append("On ")
                If text3.Length > 0 Then
                    stringBuilder.Append(text3)
                End If
                If text2.Length > 0 Then
                    If text3.Length > 0 Then
                        stringBuilder.Append(", ")
                    End If
                    stringBuilder.Append(text2)
                End If
                stringBuilder.AppendLine(" wrote:")
            End If
            Dim array = text.Split(New String(1) {vbCrLf, vbLf}, StringSplitOptions.None)
            For Each text4 In array
                stringBuilder.AppendLine("> " & text4)
            Next
            Return stringBuilder.ToString()
        End Function

        Private Function BuildQuotedForwardBody(hdr As MailMessageHeader) As String
            If hdr Is Nothing Then
                Return ""
            End If
            Dim value = BodyTextForQuote(hdr)
            Dim stringBuilder As StringBuilder = New StringBuilder()
            stringBuilder.AppendLine()
            stringBuilder.AppendLine("---------- Forwarded message ----------")
            If Not String.IsNullOrWhiteSpace(hdr.From) Then
                stringBuilder.AppendLine("From: " & hdr.From)
            End If
            If Date.Compare(hdr.DateUtc, Date.MinValue) <> 0 Then
                stringBuilder.AppendLine("Date: " & hdr.DateUtc.ToLocalTime().ToString("f"))
            End If
            stringBuilder.AppendLine("Subject: " & If(Not Equals(hdr.Subject, Nothing), hdr.Subject, ""))
            If Not String.IsNullOrWhiteSpace(hdr.ToAddresses) Then
                stringBuilder.AppendLine("To: " & hdr.ToAddresses)
            End If
            If Not String.IsNullOrWhiteSpace(hdr.CcAddresses) Then
                stringBuilder.AppendLine("Cc: " & hdr.CcAddresses)
            End If
            stringBuilder.AppendLine()
            stringBuilder.Append(value)
            Return stringBuilder.ToString()
        End Function

        Private Shared Function SubjectWithPrefix(prefix As String, subject As String) As String
            Dim text As String = If(Not Equals(subject, Nothing), subject, "").Trim()
            Dim text2 As String = If(Not Equals(prefix, Nothing), prefix, "").Trim()
            If text2.Length = 0 Then
                Return text
            End If
            If Not text2.EndsWith(":", StringComparison.Ordinal) Then
                text2 += ":"
            End If
            If text.Length = 0 Then
                Return text2 & " "
            End If
            If text.StartsWith(text2, StringComparison.OrdinalIgnoreCase) Then
                Return text
            End If
            Return text2 & " " & text
        End Function

        Private Shared Sub BuildReplyAllAddresses(hdr As MailMessageHeader, account As MailAccount, ByRef toOut As String, ByRef ccOut As String)
            toOut = If(Not Equals(hdr.From, Nothing), hdr.From, "").Trim()
            ccOut = ""
            Dim hashSet As HashSet(Of String) = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            If account IsNot Nothing Then
                AddSelfKey(hashSet, account.EmailAddress)
                AddSelfKey(hashSet, account.Username)
                AddSelfKey(hashSet, account.SmtpUsername)
                AddSelfKey(hashSet, account.DisplayName)
            End If
            Dim text = NormalizeAddressKey(toOut)
            Dim list As List(Of String) = New List(Of String)()
            Dim hashSet2 As HashSet(Of String) = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            For Each item In SplitAddressList(hdr.ToAddresses)
                Dim text2 = NormalizeAddressKey(item)
                If text2.Length <> 0 AndAlso Not hashSet.Contains(text2) AndAlso (text.Length <= 0 OrElse Not String.Equals(text2, text, StringComparison.OrdinalIgnoreCase)) AndAlso hashSet2.Add(text2) Then
                    list.Add(item.Trim())
                End If
            Next
            For Each item2 In SplitAddressList(hdr.CcAddresses)
                Dim text3 = NormalizeAddressKey(item2)
                If text3.Length <> 0 AndAlso Not hashSet.Contains(text3) AndAlso (text.Length <= 0 OrElse Not String.Equals(text3, text, StringComparison.OrdinalIgnoreCase)) AndAlso hashSet2.Add(text3) Then
                    list.Add(item2.Trim())
                End If
            Next
            ccOut = String.Join(", ", list.ToArray())
        End Sub

        Private Shared Sub AddSelfKey(keys As HashSet(Of String), value As String)
            Dim text = NormalizeAddressKey(value)
            If text.Length > 0 Then
                keys.Add(text)
            End If
        End Sub

        Private Shared Function SplitAddressList(list As String) As IEnumerable(Of String)
            Return MailAddressHelper.SplitAddressTokens(list).ToArray()
        End Function

        Private Sub TryPrefetchInlineAttachments(hdr As MailMessageHeader, body As MailMessageBody)
            If body Is Nothing OrElse body.Attachments Is Nothing OrElse activeAccount Is Nothing OrElse hdr Is Nothing Then
                Return
            End If
            Dim text = If(Not Equals(body.HtmlBody, Nothing), body.HtmlBody, "")
            If text.IndexOf("cid:", StringComparison.OrdinalIgnoreCase) < 0 Then
                Return
            End If
            Dim text2 = ResolvePassword(activeAccount, promptIfMissing:=False)
            If Equals(text2, Nothing) Then
                Return
            End If
            Dim text3 = If(Not Equals(selectedFolderFullName, Nothing), selectedFolderFullName, hdr.Folder)
            If String.IsNullOrWhiteSpace(text3) Then
                text3 = "INBOX"
            End If
            Dim flag = False
            For Each attachment In body.Attachments
                If attachment Is Nothing OrElse Not attachment.IsInline OrElse Not String.IsNullOrWhiteSpace(attachment.CachedPath) AndAlso File.Exists(attachment.CachedPath) Then
                    Continue For
                End If
                Dim text4 As String = Path.Combine(Path.GetTempPath(), "lcars-inline-" & Guid.NewGuid().ToString("N"))
                Try
                    Dim text5 = mailService.SaveAttachmentToFile(activeAccount, text2, text3, hdr.Uid, attachment.PartIndex, text4)
                    If Equals(text5, Nothing) OrElse text5.Length <= 0 Then
                        attachment.CachedPath = MailCache.SaveAttachmentCache(activeAccount.Id, text3, hdr.Uid, attachment.PartIndex, attachment.FileName, text4)
                        flag = True
                    End If

                Finally
                    Try
                        If File.Exists(text4) Then
                            File.Delete(text4)
                        End If
                    Catch projectError As Exception
                        ProjectData.SetProjectError(projectError)
                        Call ProjectData.ClearProjectError()
                    End Try
                End Try
            Next
            If flag AndAlso htmlReadPane IsNot Nothing Then
                htmlReadPane.ShowBody(body, body.Attachments)
            End If
        End Sub

        Private Shared Function NormalizeAddressKey(addr As String) As String
            Dim text As String = If(Not Equals(addr, Nothing), addr, "").Trim()
            If text.Length = 0 Then
                Return ""
            End If
            Dim num = text.LastIndexOf("<"c)
            If num >= 0 Then
                Dim num2 = text.IndexOf(">"c, num)
                If num2 > num Then
                    text = text.Substring(num + 1, num2 - num - 1).Trim()
                End If
            End If
            If text.Contains("@") Then
                Return text.ToLowerInvariant()
            End If
            Return text.ToLowerInvariant()
        End Function

        Private Sub ShowEmptyStates()
            If folderHost IsNot Nothing Then
                lblFolderEmpty.Visible = True
            End If
            ClearMessageList()
            ClearReadingPane()
        End Sub

        Private Class CSharpImpl
            Shared Function __Assign(Of T)(ByRef target As T, value As T) As T
                target = value
                Return value
            End Function
        End Class
    End Class

