Imports Microsoft.Web.WebView2.Core
Imports Microsoft.Web.WebView2.WinForms
Imports System.Drawing
Imports System.IO

Public Class form1
    Inherits LCARS.LCARSForm

    Private Const DefaultHomeUrl As String = "https://www.bing.com"

    Private tabCount As Integer = 0
    Private fbOpenFile As LCARS.Controls.FlatButton
    Private fbNewWeb As LCARS.Controls.FlatButton
    Private fbNewText As LCARS.Controls.FlatButton
    Private fbNewRichText As LCARS.Controls.FlatButton
    Private fbNewMarkdown As LCARS.Controls.FlatButton
    Private fbNewSheet As LCARS.Controls.FlatButton
    Private fbNewCanvas As LCARS.Controls.FlatButton
    Private fbSave As LCARS.Controls.FlatButton
    Private fbSaveAs As LCARS.Controls.FlatButton
    Private fbInkPen As LCARS.Controls.FlatButton
    Private fbInkEraser As LCARS.Controls.FlatButton
    Private fbInkColor As LCARS.Controls.FlatButton
    Private fbInkWidth As LCARS.Controls.FlatButton
    Private fbInkAnnotate As LCARS.Controls.FlatButton
    Private fbInkClear As LCARS.Controls.FlatButton

    ' Compact chrome: thin left border; app controls + tabs on the right (Settings lead).
    Private Const EdgePad As Integer = 4
    Private Const MainRailX As Integer = 8
    Private Const LeftBorder As Integer = 8
    Private Const ButtonW As Integer = 72
    Private Const ButtonH As Integer = 26
    Private Const RailGap As Integer = 4
    Private Const ContentGap As Integer = 4
    Private Const WorkspaceRailX As Integer = MainRailX + LeftBorder + ContentGap
    Private Const InkRailX As Integer = WorkspaceRailX
    Private Const InkButtonWidth As Integer = 34
    Private Const InkButtonHeight As Integer = 22
    Private Const TopBarY As Integer = 6
    Private Const TopBarH As Integer = 34
    Private Const ElbowArm As Integer = 22
    Private Const ElbowDrop As Integer = 10
    Private Const UrlEndPillW As Integer = 40
    Private Const RailStartY As Integer = TopBarY + TopBarH + ElbowDrop + RailGap
    Private Const TabHeaderWidth As Integer = 80
    ''' <summary>Gap between top-bar controls and the right elbow so Refresh/tabs do not collide.</summary>
    Private Const ElbowClearance As Integer = 12
    Private Const AddressBarH As Integer = 28
    Private Const LockIconW As Integer = 22
    Private Const LockIconH As Integer = 22

    Private _newTabMenuOpen As Boolean = False
    Private _applyingChrome As Boolean = False
    Private _bookmarksPanelOpen As Boolean = False
    Private urlEndCap As LCARS.Controls.HalfPillButton
    Private rightElbow As LCARS.Controls.Elbow
    ''' <summary>Fills empty top-bar space left of the (right-justified) web nav — like fbStartFill.</summary>
    Private topBarFill As LCARS.Controls.FlatButton
    ''' <summary>Second-row LCARS border when the address bar is hidden (non-web tabs).</summary>
    Private addressRowFill As LCARS.Controls.FlatButton
    Private addressRowEndCap As LCARS.Controls.HalfPillButton

    Private ReadOnly _inkPenColors As Color() = {
        Color.FromArgb(255, 153, 0),
        Color.FromArgb(255, 204, 102),
        Color.Red,
        Color.DeepSkyBlue,
        Color.LimeGreen,
        Color.White,
        Color.Magenta
    }
    Private _inkColorIndex As Integer = 0
    Private ReadOnly _inkPenWidths As Single() = {2.0F, 4.0F, 8.0F, 16.0F}
    Private _inkWidthIndex As Integer = 1
    Private ReadOnly _inkColorNames As String() = {"GOLD", "AMBER", "RED", "BLUE", "GREEN", "WHITE", "PINK"}

    ''' <summary>
    ''' Returns the IBrowserTab host stored on the selected tab page, or Nothing.
    ''' </summary>
    Private Function GetActiveTab() As IBrowserTab
        If TabControl1 Is Nothing OrElse TabControl1.TabPages.Count = 0 Then Return Nothing
        Dim page As TabPage = TabControl1.SelectedTab
        If page Is Nothing Then Return Nothing
        Return TryCast(page.Tag, IBrowserTab)
    End Function

    ''' <summary>
    ''' Returns the WebView2 in the active web tab, or Nothing.
    ''' </summary>
    Private Function TryGetActiveWebView() As WebView2
        Dim webTab As WebBrowserTab = TryCast(GetActiveTab(), WebBrowserTab)
        If webTab Is Nothing Then Return Nothing
        Return webTab.WebView
    End Function

    ''' <summary>
    ''' Reads the LCARS browser home page. Falls back to Bing if none is set.
    ''' </summary>
    Private Function GetHomeUrl() As String
        Dim stored As String = GetSetting("LCARS x32", "Browser", "HomeUrl", "")
        If Not String.IsNullOrEmpty(stored) Then
            Return stored
        End If
        Return DefaultHomeUrl
    End Function

    ''' <summary>
    ''' True for empty or about:blank URLs that should not become the home page.
    ''' </summary>
    Private Function IsUnsetUrl(ByVal url As String) As Boolean
        If String.IsNullOrEmpty(url) Then Return True
        Return url.StartsWith("about:", StringComparison.OrdinalIgnoreCase)
    End Function

    ''' <summary>
    ''' Saves the current tab URL as the LCARS browser home page.
    ''' </summary>
    Private Sub fbSetHome_Click(ByVal sender As Object, ByVal e As EventArgs) Handles fbSetHome.Click
        Dim tab As IBrowserTab = GetActiveTab()
        Dim url As String = ""
        If tab IsNot Nothing Then
            url = tab.GetPathOrUrl()
        End If
        If IsUnsetUrl(url) AndAlso Not String.IsNullOrEmpty(TextBox1.Text) Then
            url = NormalizeUrl(TextBox1.Text)
        End If
        If IsUnsetUrl(url) Then
            MessageBox.Show("Navigate to a page first, then press SET HOME.", "LCARS Web Browser", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If
        SaveSetting("LCARS x32", "Browser", "HomeUrl", url)
        MessageBox.Show("Home page set to:" & vbCrLf & url, "LCARS Web Browser", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    ''' <summary>
    ''' Ensures a typed address has a scheme so Navigate succeeds.
    ''' </summary>
    Private Function NormalizeUrl(ByVal raw As String) As String
        Dim text As String = If(raw, "").Trim()
        If text = "" Then Return GetHomeUrl()
        If text.Contains("://") Then Return text
        If text.StartsWith("\\") OrElse (text.Length >= 2 AndAlso Char.IsLetter(text(0)) AndAlso text(1) = ":"c) Then
            Return text
        End If
        Return "https://" & text
    End Function

    Private Sub WebView_NavigationStarting(ByVal sender As Object, ByVal e As CoreWebView2NavigationStartingEventArgs)
        Try
            ProgressBar1.Visible = True
            ProgressBar1.Style = ProgressBarStyle.Marquee
            ProgressBar1.MarqueeAnimationSpeed = 30
        Catch
        End Try
    End Sub

    Private Sub WebView_NavigationCompleted(ByVal sender As Object, ByVal e As CoreWebView2NavigationCompletedEventArgs)
        Try
            ProgressBar1.Style = ProgressBarStyle.Blocks
            ProgressBar1.MarqueeAnimationSpeed = 0
            ProgressBar1.Value = ProgressBar1.Maximum
        Catch
        End Try
        SyncActiveTabUi()
    End Sub

    Private Sub WebView_DocumentTitleChanged(ByVal sender As Object, ByVal e As Object)
        SyncActiveTabUi()
    End Sub

    Private Sub WebView_SourceChanged(ByVal sender As Object, ByVal e As CoreWebView2SourceChangedEventArgs)
        SyncActiveTabUi()
    End Sub

    ''' <summary>
    ''' Opens popup / target=_blank navigations in a new LCARS tab.
    ''' </summary>
    Private Async Sub WebView_NewWindowRequested(ByVal sender As Object, ByVal e As CoreWebView2NewWindowRequestedEventArgs)
        e.Handled = True
        Dim uri As String = e.Uri
        Await AddTabAsync(uri)
    End Sub

    ''' <summary>
    ''' Updates address bar, tab title, chrome visibility, and https padlock from the active view.
    ''' </summary>
    Private Sub SyncActiveTabUi()
        Dim tab As IBrowserTab = GetActiveTab()
        If tab Is Nothing Then
            ApplyChromeLayout()
            Return
        End If
        Dim webTab As WebBrowserTab = TryCast(tab, WebBrowserTab)
        If webTab IsNot Nothing AndAlso webTab.WebView IsNot Nothing AndAlso webTab.WebView.CoreWebView2 Is Nothing Then
            ApplyChromeLayout()
            Return
        End If

        Try
            If TabControl1.SelectedTab IsNot Nothing Then
                TabControl1.SelectedTab.Text = tab.Title
            End If
            TextBox1.Text = tab.GetPathOrUrl()
            If webTab IsNot Nothing Then
                sitesecurity()
            Else
                If PictureBox1 IsNot Nothing Then PictureBox1.Visible = False
            End If
            UpdateSaveButtons()
            UpdateInkToolbar()
        Catch
        End Try
        ApplyChromeLayout()
    End Sub

    Private Sub sitesecurity()
        Dim webaddress As String = TextBox1.Text
        Dim showLock As Boolean = webaddress.StartsWith("https:", StringComparison.OrdinalIgnoreCase)
        If PictureBox1 IsNot Nothing Then
            PictureBox1.Visible = showLock AndAlso TextBox1.Visible
        End If
        If FlatButton5 IsNot Nothing Then FlatButton5.Visible = False
    End Sub

    Private Function IsActiveWebTab() As Boolean
        Return TypeOf GetActiveTab() Is WebBrowserTab
    End Function

    Private Sub UpdateBookmarksButtonHighlight()
        If FlatButton12 Is Nothing Then Return
        FlatButton12.Lit = _bookmarksPanelOpen
        FlatButton12.Color = If(_bookmarksPanelOpen,
            LCARS.LCARScolorStyles.PrimaryFunction,
            LCARS.LCARScolorStyles.NavigationFunction)
    End Sub

    Private Sub EnsureSetHomeInBookmarksPanel()
        If fbSetHome Is Nothing OrElse GroupBox1 Is Nothing Then Return
        If fbSetHome.Parent IsNot GroupBox1 Then
            If fbSetHome.Parent IsNot Nothing Then fbSetHome.Parent.Controls.Remove(fbSetHome)
            GroupBox1.Controls.Add(fbSetHome)
        End If
        Dim sz As Drawing.Size = If(FlatButton19 IsNot Nothing, FlatButton19.Size, New Drawing.Size(100, 29))
        fbSetHome.Size = sz
        fbSetHome.ButtonText = "SET HOME"
        fbSetHome.Text = "SET HOME"
        If FlatButton19 IsNot Nothing Then
            Dim leftOfBookmark As Integer = FlatButton19.Left - sz.Width - 8
            If leftOfBookmark >= 8 Then
                fbSetHome.Location = New Drawing.Point(leftOfBookmark, FlatButton19.Top)
            Else
                fbSetHome.Location = New Drawing.Point(FlatButton19.Right + 8, FlatButton19.Top)
            End If
        Else
            fbSetHome.Location = New Drawing.Point(12, Math.Max(40, GroupBox1.ClientSize.Height - 40))
        End If
        fbSetHome.Visible = True
        fbSetHome.BringToFront()
    End Sub

    ''' <summary>
    ''' Adds a web tab, initializes WebView2, and optionally navigates.
    ''' </summary>
    Private Async Function AddWebTabAsync(Optional ByVal navigateUrl As String = Nothing) As Task
        TabControl1.TabPages.Add("NEW PAGE")
        Dim page As TabPage = TabControl1.TabPages(TabControl1.TabPages.Count - 1)
        TabControl1.SelectedTab = page

        Dim webTab As New WebBrowserTab()
        page.Tag = webTab
        AddHandler webTab.NavigationStarting, AddressOf WebView_NavigationStarting
        AddHandler webTab.NavigationCompleted, AddressOf WebView_NavigationCompleted
        AddHandler webTab.DocumentTitleChanged, AddressOf WebView_DocumentTitleChanged
        AddHandler webTab.SourceChanged, AddressOf WebView_SourceChanged
        AddHandler webTab.NewWindowRequested, AddressOf WebView_NewWindowRequested

        page.Controls.Add(webTab.ContentControl)
        Await webTab.InitializeAsync()
        tabCount += 1

        If webTab.WebView.CoreWebView2 IsNot Nothing Then
            Dim target As String = If(String.IsNullOrEmpty(navigateUrl), GetHomeUrl(), navigateUrl)
            webTab.NavigateOrOpen(target)
        End If
        SyncActiveTabUi()
    End Function

    ''' <summary>
    ''' Adds a document view tab for local PDF and image files.
    ''' </summary>
    Private Async Function AddViewTabAsync(ByVal filePath As String) As Task
        TabControl1.TabPages.Add("DOCUMENT")
        Dim page As TabPage = TabControl1.TabPages(TabControl1.TabPages.Count - 1)
        TabControl1.SelectedTab = page

        Dim viewTab As New DocumentViewTab()
        page.Tag = viewTab
        AddHandler viewTab.NavigationStarting, AddressOf WebView_NavigationStarting
        AddHandler viewTab.NavigationCompleted, AddressOf WebView_NavigationCompleted
        AddHandler viewTab.ContentChanged, AddressOf ViewTab_ContentChanged

        page.Controls.Add(viewTab.ContentControl)
        Await viewTab.InitializeAsync()
        tabCount += 1

        viewTab.NavigateOrOpen(filePath)
        SyncActiveTabUi()
    End Function

    Private Sub ViewTab_ContentChanged(ByVal sender As Object, ByVal e As EventArgs)
        SyncActiveTabUi()
    End Sub

    Private Sub EditorTab_ContentChanged(ByVal sender As Object, ByVal e As EventArgs)
        SyncActiveTabUi()
    End Sub

    ''' <summary>
    ''' Adds a unified document editor tab (txt/md/rtf/docx).
    ''' </summary>
    Private Function AddDocumentTabAsync(Optional ByVal filePath As String = Nothing) As Task
        TabControl1.TabPages.Add("NEW DOCUMENT")
        Dim page As TabPage = TabControl1.TabPages(TabControl1.TabPages.Count - 1)
        TabControl1.SelectedTab = page

        Dim docTab As New DocumentEditorTab()
        page.Tag = docTab
        AddHandler docTab.ContentChanged, AddressOf EditorTab_ContentChanged

        page.Controls.Add(docTab.ContentControl)
        tabCount += 1

        If String.IsNullOrEmpty(filePath) Then
            docTab.NewDocument(DocumentFormatKind.RichText)
        Else
            docTab.NavigateOrOpen(filePath)
        End If

        docTab.FocusContent()
        SyncActiveTabUi()
        Return Task.CompletedTask
    End Function

    ''' <summary>
    ''' Adds a plain-text editor tab, optionally loading a file.
    ''' </summary>
    Private Function AddTextTabAsync(Optional ByVal filePath As String = Nothing) As Task
        Return AddDocumentTabAsync(filePath)
    End Function

    ''' <summary>
    ''' Adds a rich-text editor tab, optionally loading a file.
    ''' </summary>
    Private Function AddRichTextTabAsync(Optional ByVal filePath As String = Nothing) As Task
        Return AddDocumentTabAsync(filePath)
    End Function

    ''' <summary>
    ''' Adds a spreadsheet tab, optionally loading a file.
    ''' </summary>
    Private Function AddSheetTabAsync(Optional ByVal filePath As String = Nothing) As Task
        TabControl1.TabPages.Add("NEW SHEET")
        Dim page As TabPage = TabControl1.TabPages(TabControl1.TabPages.Count - 1)
        TabControl1.SelectedTab = page

        Dim sheetTab As New SpreadsheetTab()
        page.Tag = sheetTab
        AddHandler sheetTab.ContentChanged, AddressOf EditorTab_ContentChanged

        page.Controls.Add(sheetTab.ContentControl)
        tabCount += 1

        If String.IsNullOrEmpty(filePath) Then
            sheetTab.NewDocument()
        Else
            sheetTab.NavigateOrOpen(filePath)
        End If

        sheetTab.FocusContent()
        SyncActiveTabUi()
        Return Task.CompletedTask
    End Function

    ''' <summary>
    ''' Adds an Obsidian-style canvas board tab.
    ''' </summary>
    Private Function AddCanvasTabAsync(Optional ByVal filePath As String = Nothing) As Task
        TabControl1.TabPages.Add("NEW CANVAS")
        Dim page As TabPage = TabControl1.TabPages(TabControl1.TabPages.Count - 1)
        TabControl1.SelectedTab = page

        Dim canvasTab As New BoardCanvasTab()
        page.Tag = canvasTab
        AddHandler canvasTab.ContentChanged, AddressOf EditorTab_ContentChanged
        AddHandler canvasTab.OpenUrlRequested, AddressOf CanvasTab_OpenUrlRequested

        page.Controls.Add(canvasTab.ContentControl)
        tabCount += 1

        If String.IsNullOrEmpty(filePath) Then
            canvasTab.NewDocument()
        Else
            canvasTab.NavigateOrOpen(filePath)
        End If

        canvasTab.FocusContent()
        SyncActiveTabUi()
        Return Task.CompletedTask
    End Function

    Private Async Sub CanvasTab_OpenUrlRequested(ByVal sender As Object, ByVal url As String)
        If String.IsNullOrWhiteSpace(url) Then Return
        Try
            Await OpenPathOrUrlAsync(url.Trim())
        Catch
        End Try
    End Sub

    Private Async Function AddMarkdownTabAsync(Optional ByVal filePath As String = Nothing) As Task
        Await AddDocumentTabAsync(filePath)
    End Function

    ''' <summary>
    ''' True when the tab kind supports Save / Save As.
    ''' </summary>
    Private Function IsEditableFileTab(ByVal tab As IBrowserTab) As Boolean
        If tab Is Nothing Then Return False
        If tab.TabKind = "Document" OrElse tab.TabKind = "Text" OrElse tab.TabKind = "RichText" OrElse tab.TabKind = "Markdown" OrElse tab.TabKind = "Sheet" OrElse tab.TabKind = "Canvas" Then
            Return True
        End If
        Return IsAnnotatedViewTab(tab)
    End Function

    ''' <summary>
    ''' True when a view tab has annotation ink that can be saved.
    ''' </summary>
    Private Function IsAnnotatedViewTab(ByVal tab As IBrowserTab) As Boolean
        Dim viewTab As DocumentViewTab = TryCast(tab, DocumentViewTab)
        If viewTab Is Nothing Then Return False
        Return viewTab.AnnotateMode OrElse viewTab.IsDirty
    End Function

    ''' <summary>
    ''' Returns the ink overlay on the active canvas or annotated view tab.
    ''' </summary>
    Private Function TryGetActiveInkOverlay() As InkOverlayControl
        Dim canvasTab As CanvasTab = TryCast(GetActiveTab(), CanvasTab)
        If canvasTab IsNot Nothing Then Return canvasTab.InkOverlay

        Dim viewTab As DocumentViewTab = TryCast(GetActiveTab(), DocumentViewTab)
        If viewTab IsNot Nothing AndAlso viewTab.AnnotateMode Then Return viewTab.InkOverlay

        Return Nothing
    End Function

    ''' <summary>
    ''' Enables Save when the active editable tab is dirty; Save As when any editable tab is active.
    ''' </summary>
    Private Sub UpdateSaveButtons()
        Dim tab As IBrowserTab = GetActiveTab()
        Dim editable As Boolean = IsEditableFileTab(tab)
        Dim dirty As Boolean = editable AndAlso tab.IsDirty

        If fbSave IsNot Nothing Then
            fbSave.Clickable = dirty
            fbSave.Color = If(dirty, LCARS.LCARScolorStyles.NavigationFunction, LCARS.LCARScolorStyles.FunctionUnavailable)
        End If
        If fbSaveAs IsNot Nothing Then
            fbSaveAs.Clickable = editable
            fbSaveAs.Color = If(editable, LCARS.LCARScolorStyles.NavigationFunction, LCARS.LCARScolorStyles.FunctionUnavailable)
        End If
    End Sub

    ''' <summary>
    ''' Asks to save a dirty tab before close; returns False when the user cancels.
    ''' </summary>
    Private Function PromptSaveDirtyTab(ByVal tab As IBrowserTab) As Boolean
        If tab Is Nothing OrElse Not tab.IsDirty Then Return True

        Dim title As String = tab.Title.TrimEnd("*"c)
        Dim result As MsgBoxResult = LCARS.UI.MsgBox(
            "Save changes to """ & title & """ before closing?" & vbCrLf & vbCrLf &
            "YES = Save   NO = Close without saving   CANCEL = Keep open",
            MsgBoxStyle.YesNoCancel Or MsgBoxStyle.Question,
            "LCARS Web Browser")

        If result = MsgBoxResult.Cancel Then Return False
        If result = MsgBoxResult.No Then Return True
        Return SaveTabToDisk(tab)
    End Function

    ''' <summary>
    ''' Prompts to save every dirty tab; returns False when the user cancels.
    ''' </summary>
    Private Function PromptSaveAllDirtyTabs() As Boolean
        For Each page As TabPage In TabControl1.TabPages
            Dim tab As IBrowserTab = TryCast(page.Tag, IBrowserTab)
            If tab Is Nothing OrElse Not tab.IsDirty Then Continue For
            TabControl1.SelectedTab = page
            SyncActiveTabUi()
            If Not PromptSaveDirtyTab(tab) Then Return False
        Next
        Return True
    End Function

    ''' <summary>
    ''' Saves a tab to its current path, or prompts when untitled.
    ''' </summary>
    Private Function SaveTabToDisk(ByVal tab As IBrowserTab) As Boolean
        If tab Is Nothing Then Return True

        Dim viewTab As DocumentViewTab = TryCast(tab, DocumentViewTab)
        If viewTab IsNot Nothing Then
            If viewTab.IsDirty Then
                viewTab.SaveInkSidecar()
                SyncActiveTabUi()
            End If
            Return True
        End If

        Dim path As String = tab.GetPathOrUrl()
        If String.IsNullOrEmpty(path) Then
            Return SaveTabAsDialog(tab)
        End If

        If tab.Save(path) Then
            SyncActiveTabUi()
            Return True
        End If

        MessageBox.Show("Unable to save file:" & vbCrLf & path, "LCARS Web Browser", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Return False
    End Function

    ''' <summary>
    ''' Prompts for a path and saves the given editable tab.
    ''' </summary>
    Private Function SaveTabAsDialog(ByVal tab As IBrowserTab) As Boolean
        If tab Is Nothing OrElse Not IsEditableFileTab(tab) Then Return True

        Using dlg As New SaveFileDialog()
            If tab.TabKind = "Document" Then
                dlg.Title = "Save Document"
                dlg.Filter = "Rich text|*.rtf|Markdown|*.md|Text|*.txt|Word document|*.docx|All files|*.*"
                dlg.DefaultExt = "rtf"
            ElseIf tab.TabKind = "Text" Then
                dlg.Title = "Save Text File"
                dlg.Filter = "Text files|*.txt|All files|*.*"
                dlg.DefaultExt = "txt"
            ElseIf tab.TabKind = "Markdown" Then
                dlg.Title = "Save Markdown File"
                dlg.Filter = "Markdown files|*.md|All files|*.*"
                dlg.DefaultExt = "md"
            ElseIf tab.TabKind = "Sheet" Then
                dlg.Title = "Save Spreadsheet"
                dlg.Filter = "CSV files|*.csv|Excel files|*.xlsx|All files|*.*"
                dlg.DefaultExt = "csv"
            ElseIf tab.TabKind = "Canvas" Then
                dlg.Title = "Save Canvas"
                dlg.Filter = "Obsidian canvas|*.canvas|LCARS canvas|*.lcarscanvas|PNG images|*.png|All files|*.*"
                dlg.DefaultExt = "canvas"
            ElseIf tab.TabKind = "View" Then
                dlg.Title = "Export Annotated Image"
                dlg.Filter = "PNG images|*.png|All files|*.*"
                dlg.DefaultExt = "png"
            Else
                dlg.Title = "Save Rich Text File"
                dlg.Filter = "Rich text files|*.rtf|All files|*.*"
                dlg.DefaultExt = "rtf"
            End If

            Dim currentPath As String = tab.GetPathOrUrl()
            If Not String.IsNullOrEmpty(currentPath) Then
                dlg.FileName = System.IO.Path.GetFileName(currentPath)
                dlg.InitialDirectory = System.IO.Path.GetDirectoryName(currentPath)
            End If

            If dlg.ShowDialog(Me) <> DialogResult.OK Then Return False

            Dim viewTab As DocumentViewTab = TryCast(tab, DocumentViewTab)
            If viewTab IsNot Nothing Then
                If System.IO.Path.GetExtension(dlg.FileName).Equals(".lcarsink", StringComparison.OrdinalIgnoreCase) Then
                    viewTab.SaveInkSidecar()
                    SyncActiveTabUi()
                    Return True
                End If
                If viewTab.Save(dlg.FileName) Then
                    SyncActiveTabUi()
                    Return True
                End If
                MessageBox.Show("Unable to export annotated image:" & vbCrLf & dlg.FileName, "LCARS Web Browser", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return False
            End If

            If tab.Save(dlg.FileName) Then
                SyncActiveTabUi()
                Return True
            End If
            MessageBox.Show("Unable to save file:" & vbCrLf & dlg.FileName, "LCARS Web Browser", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return False
        End Using
    End Function

    ''' <summary>
    ''' Disposes the tab host control tree for a closed tab page.
    ''' </summary>
    Private Sub DisposeTabHost(ByVal tabHost As IBrowserTab)
        If TypeOf tabHost Is WebBrowserTab Then
            DirectCast(tabHost, WebBrowserTab).Dispose()
        ElseIf TypeOf tabHost Is DocumentViewTab Then
            DirectCast(tabHost, DocumentViewTab).Dispose()
        ElseIf TypeOf tabHost Is DocumentEditorTab Then
            DirectCast(tabHost, DocumentEditorTab).Dispose()
        ElseIf TypeOf tabHost Is TextEditTab Then
            DirectCast(tabHost, TextEditTab).Dispose()
        ElseIf TypeOf tabHost Is RichTextEditTab Then
            DirectCast(tabHost, RichTextEditTab).Dispose()
        ElseIf TypeOf tabHost Is MarkdownEditTab Then
            DirectCast(tabHost, MarkdownEditTab).Dispose()
        ElseIf TypeOf tabHost Is SpreadsheetTab Then
            DirectCast(tabHost, SpreadsheetTab).Dispose()
        ElseIf TypeOf tabHost Is BoardCanvasTab Then
            DirectCast(tabHost, BoardCanvasTab).Dispose()
        ElseIf TypeOf tabHost Is CanvasTab Then
            DirectCast(tabHost, CanvasTab).Dispose()
        End If
    End Sub

    ''' <summary>
    ''' Saves the active editable tab to its current path, or prompts when untitled.
    ''' </summary>
    Private Sub SaveActiveTab()
        Dim tab As IBrowserTab = GetActiveTab()
        If Not IsEditableFileTab(tab) Then Return
        SaveTabToDisk(tab)
    End Sub

    ''' <summary>
    ''' Prompts for a path and saves the active editable tab.
    ''' </summary>
    Private Sub SaveActiveTabAs()
        Dim tab As IBrowserTab = GetActiveTab()
        If Not IsEditableFileTab(tab) Then Return
        SaveTabAsDialog(tab)
    End Sub

    ''' <summary>
    ''' Opens a path or URL in the tab kind chosen by FileOpenRouter.
    ''' </summary>
    Private Async Function OpenPathOrUrlAsync(ByVal pathOrUrl As String) As Task
        Dim text As String = If(pathOrUrl, "").Trim()
        If text = "" Then
            Await AddWebTabAsync()
            Return
        End If

        Dim localPath As String = text
        If File.Exists(localPath) Then
            If String.Equals(System.IO.Path.GetExtension(localPath), ".url", StringComparison.OrdinalIgnoreCase) Then
                Dim shortcutUrl As String = ReadUrlShortcut(localPath)
                If IsValidUrlShortcutTarget(shortcutUrl) Then
                    Await AddWebTabAsync(NormalizeUrl(shortcutUrl))
                    Return
                End If
                MessageBox.Show(
                    "Could not read a valid URL from shortcut:" & vbCrLf & localPath,
                    "LCARS Web Browser",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information)
                Return
            End If

            Dim kind As String = FileOpenRouter.Resolve(localPath)
            Select Case kind
                Case "Web"
                    Await AddWebTabAsync(ToFileUri(localPath))
                Case "View"
                    Await AddViewTabAsync(localPath)
                Case "Document", "Text", "RichText", "Markdown"
                    Await AddDocumentTabAsync(localPath)
                Case "Sheet"
                    Await AddSheetTabAsync(localPath)
                Case "Canvas"
                    Await AddCanvasTabAsync(localPath)
                Case Else
                    If FileOpenRouter.IsLikelyTextFile(localPath) Then
                        Await AddDocumentTabAsync(localPath)
                    Else
                        MessageBox.Show("Unsupported file type:" & vbCrLf & localPath, "LCARS Web Browser", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    End If
            End Select
            Return
        End If

        Await AddWebTabAsync(NormalizeUrl(text))
    End Function

    ''' <summary>
    ''' Adds a tab using the file router or home page when no target is given.
    ''' </summary>
    Private Async Function AddTabAsync(Optional ByVal navigateUrl As String = Nothing) As Task
        If String.IsNullOrEmpty(navigateUrl) Then
            Await AddWebTabAsync()
        Else
            Await OpenPathOrUrlAsync(navigateUrl)
        End If
    End Function

    Private Sub ShowUnsupportedTabKind(ByVal kind As String)
        MessageBox.Show(
            kind & " tabs are not available yet.",
            "LCARS Web Browser",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information)
    End Sub

    Private Function ReadUrlShortcut(ByVal path As String) As String
        Dim urlValue As String = Nothing
        Dim baseUrlValue As String = Nothing
        Try
            For Each line As String In File.ReadAllLines(path)
                If line.StartsWith("URL=", StringComparison.OrdinalIgnoreCase) Then
                    urlValue = line.Substring("URL=".Length).Trim()
                ElseIf line.StartsWith("BASEURL=", StringComparison.OrdinalIgnoreCase) Then
                    baseUrlValue = line.Substring("BASEURL=".Length).Trim()
                End If
            Next
        Catch
        End Try
        If Not String.IsNullOrEmpty(urlValue) Then Return urlValue
        If Not String.IsNullOrEmpty(baseUrlValue) Then Return baseUrlValue
        Return ""
    End Function

    Private Function IsValidUrlShortcutTarget(ByVal value As String) As Boolean
        Dim text As String = If(value, "").Trim()
        If text = "" Then Return False
        Return text.StartsWith("http://", StringComparison.OrdinalIgnoreCase) OrElse
               text.StartsWith("https://", StringComparison.OrdinalIgnoreCase) OrElse
               text.StartsWith("file://", StringComparison.OrdinalIgnoreCase)
    End Function

    Private Function ToFileUri(ByVal path As String) As String
        Return New Uri(path).AbsoluteUri
    End Function

    ''' <summary>
    ''' Positions chrome: blank upper-left elbow, nav + URL ending in a half-pill, narrow rail.
    ''' </summary>
    Private Sub ApplyChromeLayout()
        If _applyingChrome Then Return
        _applyingChrome = True
        SuspendLayout()
        Try
            EnsureUrlEndCap()

            Dim isWeb As Boolean = IsActiveWebTab()
            Dim elbowW As Integer = LeftBorder + ElbowArm
            Dim elbowH As Integer = TopBarH + ElbowDrop
            Dim clientW As Integer = Math.Max(320, ClientSize.Width)
            Dim clientH As Integer = Math.Max(240, ClientSize.Height)
            Dim appRailX As Integer = clientW - EdgePad - ButtonW
            ' Extra clearance so nav/tabs clear the right elbow (Refresh was overlapping).
            Dim rightClear As Integer = ElbowClearance + ContentGap
            Dim contentLeft As Integer = MainRailX + LeftBorder + ElbowArm + ContentGap
            Dim contentRightPad As Integer = ButtonW + rightClear + EdgePad
            Dim topBarLeft As Integer = MainRailX + elbowW
            ' Always reserve the address row so chrome does not jump; non-web shows a border bar there.
            Dim addressRowH As Integer = AddressBarH + ContentGap
            Dim contentTop As Integer = TopBarY + TopBarH + addressRowH + ContentGap
            Dim contentW As Integer = Math.Max(80, clientW - contentLeft - contentRightPad)
            Dim contentH As Integer = Math.Max(80, clientH - contentTop - EdgePad)

            EnsureChromeFillers()

            If Elbow1 IsNot Nothing Then
                Elbow1.Visible = True
                Elbow1.Clickable = True
                Elbow1.ElbowStyle = LCARS.Controls.Elbow.LCARSelbowStyles.UpperLeft
                Elbow1.Color = LCARS.LCARScolorStyles.LCARSDisplayOnly
                Elbow1.ButtonWidth = LeftBorder
                Elbow1.ButtonHeight = TopBarH
                Elbow1.ButtonText = ""
                Elbow1.Text = ""
                Elbow1.Location = New Drawing.Point(MainRailX, TopBarY)
                Elbow1.Size = New Drawing.Size(elbowW, elbowH)
                Elbow1.BringToFront()
            End If

            If rightElbow Is Nothing Then
                rightElbow = New LCARS.Controls.Elbow()
                rightElbow.Name = "elbRightTop"
                rightElbow.Clickable = False
                rightElbow.ElbowStyle = LCARS.Controls.Elbow.LCARSelbowStyles.UpperRight
                rightElbow.Color = LCARS.LCARScolorStyles.LCARSDisplayOnly
                rightElbow.ButtonText = ""
                rightElbow.Text = ""
                rightElbow.ButtonTextAlign = ContentAlignment.BottomRight
                rightElbow.ButtonTextHeight = 12
                Controls.Add(rightElbow)
            End If
            rightElbow.ButtonWidth = ButtonW
            rightElbow.ButtonHeight = 18
            rightElbow.ButtonText = ""
            rightElbow.Text = ""
            rightElbow.Size = New Drawing.Size(ButtonW + 20, elbowH)
            rightElbow.Location = New Drawing.Point(appRailX - 20, TopBarY)
            rightElbow.Visible = True
            rightElbow.BringToFront()

            Dim navY As Integer = TopBarY + 3
            Dim navH As Integer = TopBarH - 6
            Dim navRefreshW As Integer = 78
            Dim navStopW As Integer = 52
            Dim navArrowW As Integer = 48
            Dim homeW As Integer = 88
            Dim bookmarksW As Integer = 96
            Dim navGap As Integer = 4
            ' Right-justified web nav (+ Home / Bookmarks), same edge as the tab strip.
            Dim navClusterW As Integer =
                navArrowW + navGap + navArrowW + navGap + navStopW + navGap + navRefreshW +
                navGap + homeW + navGap + bookmarksW
            Dim navMaxRight As Integer = appRailX - rightClear
            If topBarLeft + 4 + navClusterW > navMaxRight Then
                Dim overflow As Integer = (topBarLeft + 4 + navClusterW) - navMaxRight
                homeW = Math.Max(64, homeW - overflow \ 2)
                bookmarksW = Math.Max(72, bookmarksW - overflow \ 2)
                navClusterW = navArrowW + navGap + navArrowW + navGap + navStopW + navGap + navRefreshW +
                    navGap + homeW + navGap + bookmarksW
            End If
            Dim navLeft As Integer = navMaxRight - navClusterW
            If navLeft < topBarLeft + 4 Then navLeft = topBarLeft + 4

            ' Shell-style filler left of the nav cluster (or full top bar when nav is hidden).
            Dim topFillRight As Integer = If(isWeb, navLeft - ContentGap, navMaxRight)
            Dim topFillW As Integer = Math.Max(8, topFillRight - topBarLeft)
            If topBarFill IsNot Nothing Then
                topBarFill.Visible = True
                topBarFill.Location = New Drawing.Point(topBarLeft, TopBarY)
                topBarFill.Size = New Drawing.Size(topFillW, TopBarH)
                topBarFill.SendToBack()
            End If

            Dim x As Integer = navLeft
            If Arrowbutton1 IsNot Nothing Then
                Arrowbutton1.Visible = isWeb
                Arrowbutton1.Location = New Drawing.Point(x, navY)
                Arrowbutton1.Size = New Drawing.Size(navArrowW, navH)
                Arrowbutton1.ButtonText = "<"
                Arrowbutton1.Text = "<"
                x += navArrowW + navGap
            End If
            If Arrowbutton2 IsNot Nothing Then
                Arrowbutton2.Visible = isWeb
                Arrowbutton2.Location = New Drawing.Point(x, navY)
                Arrowbutton2.Size = New Drawing.Size(navArrowW, navH)
                Arrowbutton2.ButtonText = ">"
                Arrowbutton2.Text = ">"
                x += navArrowW + navGap
            End If
            If FlatButton2 IsNot Nothing Then
                FlatButton2.Visible = isWeb
                FlatButton2.Location = New Drawing.Point(x, navY)
                FlatButton2.Size = New Drawing.Size(navStopW, navH)
                FlatButton2.ButtonText = "STOP"
                FlatButton2.Text = "STOP"
                x += navStopW + navGap
            End If
            If FlatButton1 IsNot Nothing Then
                FlatButton1.Visible = isWeb
                FlatButton1.Location = New Drawing.Point(x, navY)
                FlatButton1.Size = New Drawing.Size(navRefreshW, navH)
                FlatButton1.ButtonText = "REFRESH"
                FlatButton1.Text = "REFRESH"
                x += navRefreshW + navGap
            End If
            If FlatButton6 IsNot Nothing Then
                FlatButton6.Visible = isWeb
                FlatButton6.Location = New Drawing.Point(x, navY)
                FlatButton6.Size = New Drawing.Size(homeW, navH)
                FlatButton6.ButtonText = "HOME"
                FlatButton6.Text = "HOME"
                x += homeW + navGap
            End If
            If FlatButton12 IsNot Nothing Then
                FlatButton12.Visible = isWeb
                FlatButton12.Location = New Drawing.Point(x, navY)
                FlatButton12.Size = New Drawing.Size(bookmarksW, navH)
                FlatButton12.ButtonText = "BOOKMARKS"
                FlatButton12.Text = "BOOKMARKS"
                UpdateBookmarksButtonHighlight()
            End If

            If ComplexButton1 IsNot Nothing Then
                ComplexButton1.Visible = False
                ComplexButton1.SideText = ""
            End If

            ' Address row: URL when web; LCARS border bar when not (same reserved height).
            Dim addrY As Integer = TopBarY + TopBarH + 2
            Dim addrLeft As Integer = topBarLeft + 4
            Dim addrRight As Integer = appRailX - rightClear
            Dim lockGap As Integer = 4
            Dim lockReserve As Integer = LockIconW + lockGap
            Dim urlLeft As Integer = addrLeft + lockReserve
            Dim urlRight As Integer = addrRight - UrlEndPillW - 8
            Dim urlW As Integer = Math.Max(80, urlRight - urlLeft)

            If FlatButton4a IsNot Nothing Then
                ' Keep as thin underlay only for web URL chrome; non-web uses addressRowFill.
                FlatButton4a.Visible = isWeb
                If isWeb Then
                    FlatButton4a.Location = New Drawing.Point(topBarLeft, TopBarY + TopBarH)
                    FlatButton4a.Size = New Drawing.Size(Math.Max(40, addrRight - topBarLeft), AddressBarH)
                    FlatButton4a.Clickable = False
                    FlatButton4a.ButtonText = ""
                    FlatButton4a.Text = ""
                End If
            End If

            If addressRowFill IsNot Nothing Then
                If isWeb Then
                    addressRowFill.Visible = False
                Else
                    addressRowFill.Visible = True
                    addressRowFill.Location = New Drawing.Point(topBarLeft, TopBarY + TopBarH)
                    addressRowFill.Size = New Drawing.Size(Math.Max(40, addrRight - topBarLeft - UrlEndPillW - 4), AddressBarH)
                    addressRowFill.SendToBack()
                End If
            End If
            If addressRowEndCap IsNot Nothing Then
                If isWeb Then
                    addressRowEndCap.Visible = False
                Else
                    addressRowEndCap.Visible = True
                    addressRowEndCap.Location = New Drawing.Point(topBarLeft + addressRowFill.Width + 2, addrY)
                    addressRowEndCap.Size = New Drawing.Size(UrlEndPillW, AddressBarH - 4)
                    addressRowEndCap.BringToFront()
                End If
            End If

            If TextBox1 IsNot Nothing Then
                TextBox1.Visible = isWeb
                TextBox1.Location = New Drawing.Point(urlLeft, addrY)
                TextBox1.Size = New Drawing.Size(urlW, AddressBarH - 4)
                If isWeb Then TextBox1.BringToFront()
            End If
            If FlatButton13 IsNot Nothing Then
                FlatButton13.Visible = isWeb
                FlatButton13.Location = New Drawing.Point(urlLeft, addrY + AddressBarH - 6)
                FlatButton13.Size = New Drawing.Size(urlW, 4)
            End If
            If urlEndCap IsNot Nothing Then
                urlEndCap.Visible = isWeb
                If isWeb Then
                    urlEndCap.Location = New Drawing.Point(urlLeft + urlW + 4, addrY)
                    urlEndCap.Size = New Drawing.Size(UrlEndPillW, AddressBarH - 4)
                    urlEndCap.BringToFront()
                End If
            End If
            If PictureBox1 IsNot Nothing Then
                PictureBox1.Size = New Drawing.Size(LockIconW, LockIconH)
                PictureBox1.Location = New Drawing.Point(addrLeft, addrY + Math.Max(0, (AddressBarH - 4 - LockIconH) \ 2))
                PictureBox1.SizeMode = PictureBoxSizeMode.Zoom
                If isWeb Then
                    sitesecurity()
                    PictureBox1.BringToFront()
                Else
                    PictureBox1.Visible = False
                End If
            End If
            If Label4 IsNot Nothing Then Label4.Visible = False

            If FlatButton5 IsNot Nothing Then FlatButton5.Visible = False
            If FlatButton9 IsNot Nothing Then FlatButton9.Visible = False
            If FlatButton10 IsNot Nothing Then FlatButton10.Visible = False

            ' SET HOME lives in the bookmarks panel, not the right rail.
            EnsureSetHomeInBookmarksPanel()
            If fbSetHome IsNot Nothing AndAlso Not _bookmarksPanelOpen Then
                ' Still parented to GroupBox1; visibility follows panel.
                fbSetHome.Visible = True
            End If

            Dim railY As Integer = TopBarY + elbowH + RailGap
            Dim railStep As Integer = ButtonH + RailGap
            PlaceAppRailButton(FlatButton4, appRailX, railY, "OPEN FILE") : railY += railStep
            PlaceAppRailButton(FlatButton7, appRailX, railY, "NEW TAB") : railY += railStep
            PlaceAppRailButton(FlatButton8, appRailX, railY, "CLOSE TAB") : railY += railStep
            ' ZOOM cycles FlatButton14 → 15 → 16; keep them stacked on one rail slot.
            If FlatButton14 IsNot Nothing AndAlso FlatButton15 IsNot Nothing AndAlso FlatButton16 IsNot Nothing Then
                If Not FlatButton14.Visible AndAlso Not FlatButton15.Visible AndAlso Not FlatButton16.Visible Then
                    FlatButton14.Visible = True
                End If
                PlaceZoomRailButton(FlatButton14, appRailX, railY, "ZOOM")
                PlaceZoomRailButton(FlatButton15, appRailX, railY, "ZOOM")
                PlaceZoomRailButton(FlatButton16, appRailX, railY, "ZOOM")
                railY += railStep
            Else
                PlaceAppRailButton(FlatButton14, appRailX, railY, "ZOOM") : railY += railStep
            End If

            If ProgressBar1 IsNot Nothing Then
                ProgressBar1.Visible = isWeb
                If isWeb Then
                    ProgressBar1.Location = New Drawing.Point(appRailX, railY)
                    ProgressBar1.Size = New Drawing.Size(ButtonW, ButtonH)
                    ProgressBar1.BringToFront()
                    railY += railStep
                End If
            End If

            If FlatButton11 IsNot Nothing Then
                FlatButton11.Visible = True
                FlatButton11.Location = New Drawing.Point(MainRailX, TopBarY + elbowH + RailGap)
                FlatButton11.Size = New Drawing.Size(LeftBorder, Math.Max(24, clientH - EdgePad - (TopBarY + elbowH + RailGap)))
                FlatButton11.ButtonText = ""
                FlatButton11.Text = ""
            End If

            PlaceShellAlignedCloseButton(FlatButton3, EdgePad)
            If FlatButton3 IsNot Nothing Then
                FlatButton3.Width = ButtonW
                FlatButton3.Left = Math.Max(0, ClientSize.Width - EdgePad - ButtonW)
            End If

            Dim popY As Integer = TopBarY + elbowH + RailGap + (railStep * 1)
            Dim workspaceX As Integer = appRailX - ButtonW - ContentGap
            PlaceWorkspaceButtonAt(fbNewWeb, workspaceX, popY, "NEW WEB") : popY += railStep
            PlaceWorkspaceButtonAt(fbNewText, workspaceX, popY, "NEW DOCUMENT") : popY += railStep
            PlaceWorkspaceButtonAt(fbNewSheet, workspaceX, popY, "NEW SHEET") : popY += railStep
            PlaceWorkspaceButtonAt(fbNewCanvas, workspaceX, popY, "NEW CANVAS") : popY += railStep
            PlaceWorkspaceButtonAt(fbSave, workspaceX, popY, "SAVE") : popY += railStep
            PlaceWorkspaceButtonAt(fbSaveAs, workspaceX, popY, "SAVE AS")

            If fbNewRichText IsNot Nothing Then fbNewRichText.Visible = False
            If fbNewMarkdown IsNot Nothing Then fbNewMarkdown.Visible = False
            If fbOpenFile IsNot Nothing Then fbOpenFile.Visible = False

            Dim inkY As Integer = TopBarY + elbowH + RailGap
            Dim inkStep As Integer = InkButtonHeight + 2
            PlaceInkButton(fbInkPen, inkY, "PEN") : inkY += inkStep
            PlaceInkButton(fbInkEraser, inkY, "ERASE") : inkY += inkStep
            PlaceInkButton(fbInkColor, inkY, "COLOR") : inkY += inkStep
            PlaceInkButton(fbInkWidth, inkY, "WIDTH") : inkY += inkStep
            PlaceInkButton(fbInkAnnotate, inkY, "NOTE") : inkY += inkStep
            PlaceInkButton(fbInkClear, inkY, "CLEAR")

            If TabControl1 IsNot Nothing Then
                TabControl1.Alignment = TabAlignment.Top
                TabControl1.SizeMode = TabSizeMode.Normal
                TabControl1.Multiline = False
                TabControl1.RightToLeft = RightToLeft.Yes
                TabControl1.RightToLeftLayout = True
                TabControl1.Visible = Not _bookmarksPanelOpen
                TabControl1.SetBounds(contentLeft, contentTop, contentW, contentH)
            End If
            If GroupBox1 IsNot Nothing Then
                GroupBox1.Visible = _bookmarksPanelOpen
                GroupBox1.SetBounds(contentLeft, contentTop, contentW, contentH)
                If _bookmarksPanelOpen Then EnsureSetHomeInBookmarksPanel()
            End If

            If topBarFill IsNot Nothing Then topBarFill.SendToBack()
            If addressRowFill IsNot Nothing AndAlso addressRowFill.Visible Then addressRowFill.SendToBack()
            If rightElbow IsNot Nothing Then rightElbow.BringToFront()
            ApplyNewTabMenuVisibility()
            UpdateInkToolbar()
            BringWorkspaceControlsToFront()
            If FlatButton3 IsNot Nothing Then FlatButton3.BringToFront()
            If isWeb AndAlso PictureBox1 IsNot Nothing AndAlso PictureBox1.Visible Then PictureBox1.BringToFront()
            If isWeb AndAlso TextBox1 IsNot Nothing Then TextBox1.BringToFront()
            If isWeb Then
                If Arrowbutton1 IsNot Nothing Then Arrowbutton1.BringToFront()
                If Arrowbutton2 IsNot Nothing Then Arrowbutton2.BringToFront()
                If FlatButton1 IsNot Nothing Then FlatButton1.BringToFront()
                If FlatButton2 IsNot Nothing Then FlatButton2.BringToFront()
                If FlatButton6 IsNot Nothing Then FlatButton6.BringToFront()
                If FlatButton12 IsNot Nothing Then FlatButton12.BringToFront()
            End If
        Finally
            ResumeLayout(True)
            _applyingChrome = False
        End Try
    End Sub

    Private Sub EnsureChromeFillers()
        EnsureUrlEndCap()
        If topBarFill Is Nothing OrElse topBarFill.IsDisposed Then
            topBarFill = New LCARS.Controls.FlatButton() With {
                .Name = "topBarFill",
                .Clickable = False,
                .ButtonText = "",
                .Text = "",
                .Color = LCARS.LCARScolorStyles.StaticTan
            }
            Controls.Add(topBarFill)
        End If
        If addressRowFill Is Nothing OrElse addressRowFill.IsDisposed Then
            addressRowFill = New LCARS.Controls.FlatButton() With {
                .Name = "addressRowFill",
                .Clickable = False,
                .ButtonText = "",
                .Text = "",
                .Color = LCARS.LCARScolorStyles.LCARSDisplayOnly
            }
            Controls.Add(addressRowFill)
        End If
        If addressRowEndCap Is Nothing OrElse addressRowEndCap.IsDisposed Then
            addressRowEndCap = New LCARS.Controls.HalfPillButton() With {
                .Name = "addressRowEndCap",
                .Clickable = False,
                .ButtonText = "",
                .Text = "",
                .Color = LCARS.LCARScolorStyles.StaticTan,
                .ButtonStyle = LCARS.Controls.HalfPillButton.LCARSbuttonStyles.PillRight
            }
            Controls.Add(addressRowEndCap)
        End If
    End Sub

    Private Sub EnsureUrlEndCap()
        If urlEndCap IsNot Nothing Then Return
        urlEndCap = New LCARS.Controls.HalfPillButton()
        urlEndCap.Name = "urlEndCap"
        urlEndCap.ButtonText = ""
        urlEndCap.Text = ""
        urlEndCap.Clickable = False
        urlEndCap.Color = LCARS.LCARScolorStyles.StaticTan
        urlEndCap.ButtonStyle = LCARS.Controls.HalfPillButton.LCARSbuttonStyles.PillRight
        urlEndCap.Size = New Drawing.Size(UrlEndPillW, TopBarH - 6)
        Controls.Add(urlEndCap)
    End Sub

    Private Sub Form1_ClientSizeChanged(ByVal sender As Object, ByVal e As EventArgs) Handles MyBase.ClientSizeChanged
        If _applyingChrome OrElse Not IsHandleCreated OrElse Not Visible Then Return
        ApplyChromeLayout()
    End Sub

    Private Sub PlaceMainButton(ByVal btn As LCARS.Controls.FlatButton, ByVal y As Integer, ByVal caption As String)
        If btn Is Nothing Then Return
        btn.Location = New Drawing.Point(MainRailX, y)
        btn.Size = New Drawing.Size(ButtonW, ButtonH)
        btn.ButtonText = caption
        btn.Text = caption
        btn.ButtonTextAlign = ContentAlignment.MiddleCenter
    End Sub

    Private Sub PlaceAppRailButton(ByVal btn As LCARS.Controls.FlatButton, ByVal x As Integer, ByVal y As Integer, ByVal caption As String)
        If btn Is Nothing Then Return
        btn.Visible = True
        btn.Location = New Drawing.Point(x, y)
        btn.Size = New Drawing.Size(ButtonW, ButtonH)
        btn.ButtonText = caption
        btn.Text = caption
        btn.ButtonTextAlign = ContentAlignment.MiddleCenter
    End Sub

    Private Sub PlaceZoomRailButton(ByVal btn As LCARS.Controls.FlatButton, ByVal x As Integer, ByVal y As Integer, ByVal caption As String)
        If btn Is Nothing Then Return
        btn.Location = New Drawing.Point(x, y)
        btn.Size = New Drawing.Size(ButtonW, ButtonH)
        btn.ButtonText = caption
        btn.Text = caption
        btn.ButtonTextAlign = ContentAlignment.MiddleCenter
    End Sub

    Private Sub PlaceWorkspaceButton(ByVal btn As LCARS.Controls.FlatButton, ByVal y As Integer, ByVal caption As String)
        PlaceWorkspaceButtonAt(btn, WorkspaceRailX, y, caption)
    End Sub

    Private Sub PlaceWorkspaceButtonAt(ByVal btn As LCARS.Controls.FlatButton, ByVal x As Integer, ByVal y As Integer, ByVal caption As String)
        If btn Is Nothing Then Return
        btn.Location = New Drawing.Point(x, y)
        btn.Size = New Drawing.Size(ButtonW, ButtonH)
        btn.ButtonText = caption
        btn.Text = caption
        btn.ButtonTextAlign = ContentAlignment.MiddleCenter
    End Sub

    Private Sub PlaceInkButton(ByVal btn As LCARS.Controls.FlatButton, ByVal y As Integer, ByVal caption As String)
        If btn Is Nothing Then Return
        btn.Location = New Drawing.Point(InkRailX, y)
        btn.Size = New Drawing.Size(InkButtonWidth, InkButtonHeight)
        btn.ButtonText = caption
        btn.Text = caption
        btn.ButtonTextAlign = ContentAlignment.MiddleCenter
        btn.ButtonTextHeight = 12
    End Sub

    Private Sub ApplyNewTabMenuVisibility()
        Dim show As Boolean = _newTabMenuOpen
        If fbNewWeb IsNot Nothing Then fbNewWeb.Visible = show
        If fbNewText IsNot Nothing Then fbNewText.Visible = show
        If fbNewSheet IsNot Nothing Then fbNewSheet.Visible = show
        If fbNewCanvas IsNot Nothing Then fbNewCanvas.Visible = show
        If fbSave IsNot Nothing Then fbSave.Visible = show
        If fbSaveAs IsNot Nothing Then fbSaveAs.Visible = show
        If FlatButton7 IsNot Nothing Then
            FlatButton7.Color = If(show, LCARS.LCARScolorStyles.PrimaryFunction, LCARS.LCARScolorStyles.NavigationFunction)
        End If
        UpdateSaveButtons()
    End Sub

    Private Sub ToggleNewTabMenu()
        _newTabMenuOpen = Not _newTabMenuOpen
        ApplyNewTabMenuVisibility()
        BringWorkspaceControlsToFront()
    End Sub

    Private Sub EnsureOpenFileButton()
        If FlatButton4 IsNot Nothing Then
            RemoveHandler FlatButton4.Click, AddressOf FlatButton4_Click
            AddHandler FlatButton4.Click, AddressOf fbOpenFile_Click
        End If
    End Sub

    Private Sub BringWorkspaceControlsToFront()
        If Arrowbutton1 IsNot Nothing Then Arrowbutton1.BringToFront()
        If Arrowbutton2 IsNot Nothing Then Arrowbutton2.BringToFront()
        If FlatButton2 IsNot Nothing Then FlatButton2.BringToFront()
        If FlatButton1 IsNot Nothing Then FlatButton1.BringToFront()
        If fbNewWeb IsNot Nothing Then fbNewWeb.BringToFront()
        If fbNewText IsNot Nothing Then fbNewText.BringToFront()
        If fbNewSheet IsNot Nothing Then fbNewSheet.BringToFront()
        If fbNewCanvas IsNot Nothing Then fbNewCanvas.BringToFront()
        If fbSave IsNot Nothing Then fbSave.BringToFront()
        If fbSaveAs IsNot Nothing Then fbSaveAs.BringToFront()
        If fbInkPen IsNot Nothing Then fbInkPen.BringToFront()
        If fbInkEraser IsNot Nothing Then fbInkEraser.BringToFront()
        If fbInkColor IsNot Nothing Then fbInkColor.BringToFront()
        If fbInkWidth IsNot Nothing Then fbInkWidth.BringToFront()
        If fbInkAnnotate IsNot Nothing Then fbInkAnnotate.BringToFront()
        If fbInkClear IsNot Nothing Then fbInkClear.BringToFront()
    End Sub

    Private Function CreateRailButton(ByVal name As String, ByVal caption As String, ByVal handler As EventHandler) As LCARS.Controls.FlatButton
        Dim btn As New LCARS.Controls.FlatButton()
        btn.Beeping = True
        btn.Name = name
        btn.ButtonText = caption
        btn.Text = caption
        btn.ButtonTextAlign = ContentAlignment.MiddleCenter
        btn.Color = LCARS.LCARScolorStyles.NavigationFunction
        btn.Size = New Drawing.Size(ButtonW, ButtonH)
        btn.Visible = False
        AddHandler btn.Click, handler
        Controls.Add(btn)
        Return btn
    End Function

    Private Sub EnsureEditorButtons()
        If fbNewWeb Is Nothing Then fbNewWeb = CreateRailButton("fbNewWeb", "NEW WEB", AddressOf fbNewWeb_Click)
        If fbNewText Is Nothing Then fbNewText = CreateRailButton("fbNewText", "NEW DOCUMENT", AddressOf fbNewText_Click)
        If fbNewRichText Is Nothing Then
            fbNewRichText = CreateRailButton("fbNewRichText", "NEW RTF", AddressOf fbNewRichText_Click)
            fbNewRichText.Visible = False
        End If
        If fbNewMarkdown Is Nothing Then
            fbNewMarkdown = CreateRailButton("fbNewMarkdown", "NEW MD", AddressOf fbNewMarkdown_Click)
            fbNewMarkdown.Visible = False
        End If
        If fbNewSheet Is Nothing Then fbNewSheet = CreateRailButton("fbNewSheet", "NEW SHEET", AddressOf fbNewSheet_Click)
        If fbNewCanvas Is Nothing Then fbNewCanvas = CreateRailButton("fbNewCanvas", "NEW CANVAS", AddressOf fbNewCanvas_Click)

        EnsureInkButtons()

        If fbSave Is Nothing Then
            fbSave = CreateRailButton("fbSave", "SAVE", AddressOf fbSave_Click)
            fbSave.Clickable = False
            fbSave.Color = LCARS.LCARScolorStyles.FunctionUnavailable
        End If
        If fbSaveAs Is Nothing Then
            fbSaveAs = CreateRailButton("fbSaveAs", "SAVE AS", AddressOf fbSaveAs_Click)
            fbSaveAs.Clickable = False
            fbSaveAs.Color = LCARS.LCARScolorStyles.FunctionUnavailable
        End If

        ApplyChromeLayout()
    End Sub

    Private Sub EnsureInkButtons()
        If fbInkPen Is Nothing Then fbInkPen = CreateRailButton("fbInkPen", "PEN", AddressOf fbInkPen_Click)
        If fbInkEraser Is Nothing Then fbInkEraser = CreateRailButton("fbInkEraser", "ERASE", AddressOf fbInkEraser_Click)
        If fbInkColor Is Nothing Then fbInkColor = CreateRailButton("fbInkColor", "COLOR", AddressOf fbInkColor_Click)
        If fbInkWidth Is Nothing Then fbInkWidth = CreateRailButton("fbInkWidth", "WIDTH", AddressOf fbInkWidth_Click)
        If fbInkAnnotate Is Nothing Then fbInkAnnotate = CreateRailButton("fbInkAnnotate", "NOTE", AddressOf fbInkAnnotate_Click)
        If fbInkClear Is Nothing Then fbInkClear = CreateRailButton("fbInkClear", "CLEAR", AddressOf fbInkClear_Click)

        If TabControl1 IsNot Nothing AndAlso TabControl1.TabPages.Count > 0 Then
            UpdateInkToolButtons()
        End If
    End Sub

    Private Sub UpdateInkToolbar()
        Dim tab As IBrowserTab = GetActiveTab()
        Dim canvasActive As Boolean = TypeOf tab Is CanvasTab
        Dim viewTab As DocumentViewTab = TryCast(tab, DocumentViewTab)
        Dim annotateActive As Boolean = viewTab IsNot Nothing AndAlso viewTab.AnnotateMode
        Dim inkActive As Boolean = canvasActive OrElse annotateActive

        If fbInkPen IsNot Nothing Then fbInkPen.Visible = inkActive
        If fbInkEraser IsNot Nothing Then fbInkEraser.Visible = inkActive
        If fbInkColor IsNot Nothing Then fbInkColor.Visible = inkActive
        If fbInkWidth IsNot Nothing Then fbInkWidth.Visible = inkActive
        If fbInkAnnotate IsNot Nothing Then
            fbInkAnnotate.Visible = viewTab IsNot Nothing
            fbInkAnnotate.Lit = viewTab IsNot Nothing AndAlso viewTab.AnnotateMode
            fbInkAnnotate.Color = If(viewTab IsNot Nothing AndAlso viewTab.AnnotateMode, LCARS.LCARScolorStyles.NavigationFunction, LCARS.LCARScolorStyles.MiscFunction)
        End If
        If fbInkClear IsNot Nothing Then fbInkClear.Visible = inkActive

        If inkActive Then
            ApplyInkSettings(TryGetActiveInkOverlay())
            UpdateInkToolButtons()
            BringWorkspaceControlsToFront()
        End If
    End Sub

    Private Sub ApplyInkSettings(ByVal overlay As InkOverlayControl)
        If overlay Is Nothing Then Return
        overlay.PenColor = _inkPenColors(_inkColorIndex)
        overlay.PenWidth = _inkPenWidths(_inkWidthIndex)
    End Sub

    Private Sub UpdateInkToolButtons()
        Dim overlay As InkOverlayControl = TryGetActiveInkOverlay()
        Dim eraserOn As Boolean = overlay IsNot Nothing AndAlso overlay.EraserMode

        If fbInkPen IsNot Nothing Then
            fbInkPen.Lit = Not eraserOn
            fbInkPen.Color = If(Not eraserOn, LCARS.LCARScolorStyles.PrimaryFunction, LCARS.LCARScolorStyles.MiscFunction)
        End If
        If fbInkEraser IsNot Nothing Then
            fbInkEraser.Lit = eraserOn
            fbInkEraser.Color = If(eraserOn, LCARS.LCARScolorStyles.PrimaryFunction, LCARS.LCARScolorStyles.MiscFunction)
        End If
        If fbInkColor IsNot Nothing Then
            fbInkColor.ButtonText = _inkColorNames(_inkColorIndex)
            fbInkColor.Text = _inkColorNames(_inkColorIndex)
        End If
        If fbInkWidth IsNot Nothing Then
            Dim wText As String = "W" & CInt(_inkPenWidths(_inkWidthIndex))
            fbInkWidth.ButtonText = wText
            fbInkWidth.Text = wText
        End If
    End Sub

    Private Async Sub fbNewWeb_Click(ByVal sender As Object, ByVal e As EventArgs)
        Await AddTabAsync()
        _newTabMenuOpen = False
        ApplyNewTabMenuVisibility()
    End Sub

    Private Async Sub fbNewCanvas_Click(ByVal sender As Object, ByVal e As EventArgs)
        Await AddCanvasTabAsync()
        _newTabMenuOpen = False
        ApplyNewTabMenuVisibility()
    End Sub

    Private Sub fbInkPen_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim overlay As InkOverlayControl = TryGetActiveInkOverlay()
        If overlay Is Nothing Then Return
        overlay.EraserMode = False
        UpdateInkToolButtons()
    End Sub

    Private Sub fbInkEraser_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim overlay As InkOverlayControl = TryGetActiveInkOverlay()
        If overlay Is Nothing Then Return
        overlay.EraserMode = True
        UpdateInkToolButtons()
    End Sub

    Private Sub fbInkColor_Click(ByVal sender As Object, ByVal e As EventArgs)
        _inkColorIndex = (_inkColorIndex + 1) Mod _inkPenColors.Length
        Dim overlay As InkOverlayControl = TryGetActiveInkOverlay()
        If overlay IsNot Nothing Then
            overlay.PenColor = _inkPenColors(_inkColorIndex)
            overlay.EraserMode = False
        End If
        UpdateInkToolButtons()
    End Sub

    Private Sub fbInkWidth_Click(ByVal sender As Object, ByVal e As EventArgs)
        _inkWidthIndex = (_inkWidthIndex + 1) Mod _inkPenWidths.Length
        Dim overlay As InkOverlayControl = TryGetActiveInkOverlay()
        If overlay IsNot Nothing Then
            overlay.PenWidth = _inkPenWidths(_inkWidthIndex)
        End If
        UpdateInkToolButtons()
    End Sub

    Private Sub fbInkAnnotate_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim viewTab As DocumentViewTab = TryCast(GetActiveTab(), DocumentViewTab)
        If viewTab Is Nothing Then Return
        viewTab.AnnotateMode = Not viewTab.AnnotateMode
        SyncActiveTabUi()
    End Sub

    Private Sub fbInkClear_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim overlay As InkOverlayControl = TryGetActiveInkOverlay()
        If overlay Is Nothing Then Return

        Dim hadInk As Boolean = overlay.Document.Strokes.Count > 0
        overlay.Document.Clear()
        overlay.RefreshInkSurface()

        If hadInk Then
            Dim canvasTab As CanvasTab = TryCast(GetActiveTab(), CanvasTab)
            If canvasTab IsNot Nothing Then canvasTab.MarkInkModified()

            Dim viewTab As DocumentViewTab = TryCast(GetActiveTab(), DocumentViewTab)
            If viewTab IsNot Nothing Then viewTab.MarkInkModified()
        End If

        SyncActiveTabUi()
    End Sub

    Private Async Sub fbNewText_Click(ByVal sender As Object, ByVal e As EventArgs)
        Await AddTextTabAsync()
        _newTabMenuOpen = False
        ApplyNewTabMenuVisibility()
    End Sub

    Private Async Sub fbNewRichText_Click(ByVal sender As Object, ByVal e As EventArgs)
        Await AddRichTextTabAsync()
    End Sub

    Private Async Sub fbNewMarkdown_Click(ByVal sender As Object, ByVal e As EventArgs)
        Await AddMarkdownTabAsync()
    End Sub

    Private Async Sub fbNewSheet_Click(ByVal sender As Object, ByVal e As EventArgs)
        Await AddSheetTabAsync()
        _newTabMenuOpen = False
        ApplyNewTabMenuVisibility()
    End Sub

    Private Sub fbSave_Click(ByVal sender As Object, ByVal e As EventArgs)
        SaveActiveTab()
    End Sub

    Private Sub fbSaveAs_Click(ByVal sender As Object, ByVal e As EventArgs)
        SaveActiveTabAs()
    End Sub

    Private Async Sub fbOpenFile_Click(ByVal sender As Object, ByVal e As EventArgs)
        Using dlg As New OpenFileDialog()
            dlg.Title = "Open File"
            dlg.Filter = "All supported files|*.html;*.htm;*.url;*.pdf;*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp;*.txt;*.md;*.rtf;*.docx;*.csv;*.xlsx;*.lcarsink;*.lcarscanvas;*.canvas|" &
                         "Web pages|*.html;*.htm;*.url|" &
                         "Documents and images|*.pdf;*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp|" &
                         "Documents|*.txt;*.md;*.rtf;*.docx|" &
                         "Spreadsheets|*.csv;*.xlsx|" &
                         "Canvas|*.canvas;*.lcarscanvas;*.lcarsink|" &
                         "All files|*.*"
            dlg.FilterIndex = 1
            If dlg.ShowDialog(Me) = DialogResult.OK Then
                Await OpenPathOrUrlAsync(dlg.FileName)
            End If
        End Using
    End Sub

    Private Async Sub FrmMain_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles MyBase.Load
        FlatButton15.Visible = False
        FlatButton16.Visible = False
        PictureBox1.Visible = False
        FlatButton5.Width = 95

        EnsureOpenFileButton()
        EnsureEditorButtons()
        ApplyChromeLayout()

        Me.Visible = True
        AnimatePanel()
        ApplyChromeLayout()
        BringWorkspaceControlsToFront()

        For Each item As String In My.Settings.favList
            ListBox1.Items.Add(item)
        Next

        FlatButton17.Clickable = False
        FlatButton17.Color = LCARS.LCARScolorStyles.FunctionUnavailable
        FlatButton18.Clickable = False
        FlatButton18.Color = LCARS.LCARScolorStyles.FunctionUnavailable
        GroupBox1.Visible = False

        Dim args As String() = Environment.GetCommandLineArgs()
        Dim initialUrl As String = Nothing
        If args.Length > 1 AndAlso Not args(1).StartsWith("--", StringComparison.Ordinal) Then
            Label4.Text = args(1)
            initialUrl = args(1)
        End If

        Await AddTabAsync(initialUrl)

        If String.IsNullOrEmpty(Label4.Text) Then
            TextBox1.AutoCompleteMode = AutoCompleteMode.Suggest
            TextBox1.AutoCompleteSource = AutoCompleteSource.HistoryList
        End If

        LCARS.SetBeeping(Me, GetSetting("LCARS x32", "Application", "ButtonBeep", "TRUE"))

        Me.AllowDrop = True
        AddHandler Me.DragEnter, AddressOf Form1_DragEnter
        AddHandler Me.DragDrop, AddressOf Form1_DragDrop
    End Sub

    Private Sub Form1_DragEnter(ByVal sender As Object, ByVal e As DragEventArgs)
        If e.Data IsNot Nothing AndAlso e.Data.GetDataPresent(DataFormats.FileDrop) Then
            e.Effect = DragDropEffects.Copy
        Else
            e.Effect = DragDropEffects.None
        End If
    End Sub

    Private Async Sub Form1_DragDrop(ByVal sender As Object, ByVal e As DragEventArgs)
        If e.Data Is Nothing OrElse Not e.Data.GetDataPresent(DataFormats.FileDrop) Then Return

        Dim files As Object = e.Data.GetData(DataFormats.FileDrop)
        If Not TypeOf files Is String() Then Return

        For Each filePath As String In CType(files, String())
            If Not String.IsNullOrWhiteSpace(filePath) Then
                Await OpenPathOrUrlAsync(filePath)
            End If
        Next
    End Sub

    Private Sub TabControl1_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles TabControl1.SelectedIndexChanged
        SyncActiveTabUi()
    End Sub

    Private Sub TabControl1_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles TabControl1.Click
        SyncActiveTabUi()
    End Sub

    Public Class IEGetFavorites
        Public Shared Function GetFavoritesfromIE() As List(Of String)
            Dim list As New List(Of String)
            Try
                For Each favorites As String In _
                    My.Computer.FileSystem.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.Favorites), _
                                                    FileIO.SearchOption.SearchAllSubDirectories, "*.url")
                    Using sr As New StreamReader(favorites)
                        While Not sr.EndOfStream
                            Dim line As String = sr.ReadLine()
                            If line.Contains("BASEURL=") Then
                                list.Add(line.Replace("BASEURL=", ""))
                                Exit While
                            End If
                        End While
                    End Using
                Next
            Catch
                Return Nothing
            End Try
            Return list
        End Function
    End Class

    Private Sub FlatButton17_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles FlatButton17.Click
        ListBox1.Items.Clear()
        Try
            Dim list As New List(Of String)(IEGetFavorites.GetFavoritesfromIE())
            If list IsNot Nothing Then
                Me.ListBox1.Items.AddRange(list.ToArray())
            End If
        Catch
        End Try
        updatefavlist()
    End Sub

    Private Sub FlatButton18_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles FlatButton18.Click
        My.Settings.favList.Clear()
        ListBox1.Items.Clear()
        My.Settings.Save()
    End Sub

    Private Sub FlatButton12_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles FlatButton12.Click
        _bookmarksPanelOpen = Not _bookmarksPanelOpen
        If TabControl1 IsNot Nothing Then TabControl1.Visible = Not _bookmarksPanelOpen
        If GroupBox1 IsNot Nothing Then GroupBox1.Visible = _bookmarksPanelOpen
        UpdateBookmarksButtonHighlight()
        If _bookmarksPanelOpen Then EnsureSetHomeInBookmarksPanel()
        ApplyChromeLayout()
    End Sub

    Private Sub FlatButton20_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles FlatButton20.Click
        My.Settings.favList.Remove(ListBox1.SelectedItem)
        ListBox1.Items.Remove(ListBox1.SelectedItem)
        My.Settings.Save()
    End Sub

    Private Sub FlatButton19_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles FlatButton19.Click
        My.Settings.favList.Add(TextBox1.Text)
        ListBox1.Items.Add(TextBox1.Text)
        My.Settings.Save()
    End Sub

    Private Sub updatefavlist()
        For Each item As String In ListBox1.Items
            My.Settings.favList.Add(item)
        Next
        My.Settings.Save()
    End Sub

    Private Sub ListBox1_DoubleClick(ByVal sender As Object, ByVal e As System.EventArgs) Handles ListBox1.DoubleClick
        Dim browser As WebView2 = TryGetActiveWebView()
        If browser IsNot Nothing AndAlso browser.CoreWebView2 IsNot Nothing AndAlso ListBox1.SelectedItem IsNot Nothing Then
            browser.CoreWebView2.Navigate(NormalizeUrl(ListBox1.SelectedItem.ToString()))
        End If
        GroupBox1.Visible = False
        TabControl1.Visible = True
        _bookmarksPanelOpen = False
        UpdateBookmarksButtonHighlight()
        ApplyChromeLayout()
    End Sub

    Private Sub TextBox1_KeyPress(ByVal sender As Object, ByVal e As KeyPressEventArgs) Handles TextBox1.KeyPress
        If e.KeyChar = Chr(13) Then
            FlatButton4_Click(sender, EventArgs.Empty)
        End If
    End Sub

    Private Sub TextBox1_TextChanged(ByVal sender As Object, ByVal e As EventArgs) Handles TextBox1.TextChanged
        Dim selStart As Integer = TextBox1.SelectionStart
        TextBox1.Text = TextBox1.Text.ToLower()
        TextBox1.SelectionStart = selStart
    End Sub

    Private Sub Form1_FormClosing(ByVal sender As Object, ByVal e As FormClosingEventArgs) Handles MyBase.FormClosing
        If Not PromptSaveAllDirtyTabs() Then
            e.Cancel = True
        End If
    End Sub

    Private Sub Form1_FormClosed(ByVal sender As Object, ByVal e As FormClosedEventArgs) Handles MyBase.FormClosed
        My.Settings.Save()
    End Sub

    Private Sub FlatButton1_Click(ByVal sender As Object, ByVal e As EventArgs) Handles FlatButton1.Click
        Dim browser As WebView2 = TryGetActiveWebView()
        If browser IsNot Nothing AndAlso browser.CoreWebView2 IsNot Nothing Then
            browser.CoreWebView2.Reload()
        End If
    End Sub

    Private Sub FlatButton2_Click(ByVal sender As Object, ByVal e As EventArgs) Handles FlatButton2.Click
        Dim browser As WebView2 = TryGetActiveWebView()
        If browser IsNot Nothing AndAlso browser.CoreWebView2 IsNot Nothing Then
            browser.CoreWebView2.Stop()
        End If
    End Sub

    Private Sub FlatButton6_Click(ByVal sender As Object, ByVal e As EventArgs) Handles FlatButton6.Click
        Dim browser As WebView2 = TryGetActiveWebView()
        If browser IsNot Nothing AndAlso browser.CoreWebView2 IsNot Nothing Then
            browser.CoreWebView2.Navigate(GetHomeUrl())
        End If
    End Sub

    Private Sub FlatButton7_Click(ByVal sender As Object, ByVal e As EventArgs) Handles FlatButton7.Click
        ToggleNewTabMenu()
    End Sub

    Private Sub FlatButton8_Click(ByVal sender As Object, ByVal e As EventArgs) Handles FlatButton8.Click
        If TabControl1.TabPages.Count <= 1 Then Return

        Dim closing As TabPage = TabControl1.SelectedTab
        Dim tabHost As IBrowserTab = TryCast(closing.Tag, IBrowserTab)
        If Not PromptSaveDirtyTab(tabHost) Then Return

        DisposeTabHost(tabHost)
        TabControl1.TabPages.Remove(closing)
        TabControl1.SelectTab(TabControl1.TabPages.Count - 1)
        If tabCount > 0 Then tabCount -= 1
        SyncActiveTabUi()
    End Sub

    Private Sub Arrowbutton1_Click_1(ByVal sender As Object, ByVal e As EventArgs) Handles Arrowbutton1.Click
        Dim browser As WebView2 = TryGetActiveWebView()
        If browser IsNot Nothing AndAlso browser.CoreWebView2 IsNot Nothing AndAlso browser.CoreWebView2.CanGoBack Then
            browser.CoreWebView2.GoBack()
        End If
    End Sub

    Private Sub Arrowbutton2_Click_1(ByVal sender As Object, ByVal e As EventArgs) Handles Arrowbutton2.Click
        Dim browser As WebView2 = TryGetActiveWebView()
        If browser IsNot Nothing AndAlso browser.CoreWebView2 IsNot Nothing AndAlso browser.CoreWebView2.CanGoForward Then
            browser.CoreWebView2.GoForward()
        End If
    End Sub

    Private Sub FlatButton4_Click(ByVal sender As Object, ByVal e As EventArgs)
        NavigateAddressBar()
    End Sub

    Private Sub FlatButton5_Click(ByVal sender As Object, ByVal e As EventArgs) Handles FlatButton5.Click
        NavigateAddressBar()
    End Sub

    Private Sub NavigateAddressBar()
        Dim browser As WebView2 = TryGetActiveWebView()
        If browser IsNot Nothing AndAlso browser.CoreWebView2 IsNot Nothing Then
            browser.CoreWebView2.Navigate(NormalizeUrl(TextBox1.Text))
        End If
    End Sub

    Private Sub FlatButton3_Click(ByVal sender As Object, ByVal e As EventArgs) Handles FlatButton3.Click
        My.Settings.Save()
        Me.Close()
    End Sub

    Private Sub AnimatePanel()
        Dim interval As Integer = 25
        Dim index As Integer
        Dim makevisible As Boolean = True

        If makevisible = True Then
            Me.Visible = True
            Do Until index = Int(Replace(Me.Tag, ".", ""))
                For Each myControl As Control In Me.Controls
                    Try
                        If myControl.Tag = index Then
                            myControl.Visible = True
                            Application.DoEvents()
                        End If
                    Catch
                    End Try
                Next
                index += 1
                Threading.Thread.Sleep(interval)
                Application.DoEvents()
            Loop
        Else
            If Not Me.Tag Is Nothing Then
                If InStr(Me.Tag.ToString, ".") > 0 Then
                    index = Int(Replace(Me.Tag, ".", "")) - 1
                End If
            End If
            Do Until index = -1
                For Each myControl As Control In Me.Controls
                    Try
                        If myControl.Tag = index Then
                            myControl.Visible = False
                            Application.DoEvents()
                        End If
                    Catch
                    End Try
                Next
                index -= 1
                Application.DoEvents()
                Threading.Thread.Sleep(interval)
            Loop
            Me.Visible = False
            GC.Collect()
        End If
    End Sub

    Private Sub SetZoom(ByVal factor As Double)
        Dim browser As WebView2 = TryGetActiveWebView()
        If browser IsNot Nothing Then
            Try
                browser.ZoomFactor = factor
            Catch
            End Try
        End If
    End Sub

    Private Sub FlatButton14_Click(ByVal sender As Object, ByVal e As EventArgs) Handles FlatButton14.Click
        SetZoom(1.2R)
        FlatButton15.Visible = True
        FlatButton14.Visible = False
    End Sub

    Private Sub FlatButton15_Click(ByVal sender As Object, ByVal e As EventArgs) Handles FlatButton15.Click
        SetZoom(1.5R)
        FlatButton16.Visible = True
        FlatButton15.Visible = False
    End Sub

    Private Sub FlatButton16_Click(ByVal sender As Object, ByVal e As EventArgs) Handles FlatButton16.Click
        SetZoom(1.0R)
        FlatButton14.Visible = True
        FlatButton16.Visible = False
    End Sub

    Private Sub PictureBox1_Click(ByVal sender As Object, ByVal e As EventArgs) Handles PictureBox1.Click
        MessageBox.Show("This symbol shows that the site uses hypertext transfer with SSL/TLS protocol for encryption and security. However this does NOT guarantee that the site is secure or safe to use. Website security should be verified independently.")
    End Sub

    Private Sub Elbow1_Click(ByVal sender As Object, ByVal e As EventArgs) Handles Elbow1.Click
        ToggleBookmarkLocks()
    End Sub

    Private Sub FlatButton33_Click(ByVal sender As Object, ByVal e As EventArgs) Handles FlatButton33.Click
        ToggleBookmarkLocks()
    End Sub

    Private Sub ToggleBookmarkLocks()
        If FlatButton17.Clickable = False Then
            FlatButton17.Clickable = True
            FlatButton17.Color = LCARS.LCARScolorStyles.MiscFunction
        Else
            FlatButton17.Clickable = False
            FlatButton17.Color = LCARS.LCARScolorStyles.FunctionUnavailable
        End If

        If FlatButton18.Clickable = False Then
            FlatButton18.Clickable = True
            FlatButton18.Color = LCARS.LCARScolorStyles.MiscFunction
        Else
            FlatButton18.Clickable = False
            FlatButton18.Color = LCARS.LCARScolorStyles.FunctionUnavailable
        End If
    End Sub
End Class
