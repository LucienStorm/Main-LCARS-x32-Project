Imports System.IO
Imports System.Collections.Generic
Imports LCARS.UI
Imports LCARS.LightweightControls

'To do:
'Finish file associations
'Improve context menu

Public Class frmMyComp
    Inherits LCARS.LCARSForm

#Region " Enum "
    Private Enum SelectMode
        Clear = 0
        Add = 1
        Symmetric = 2
    End Enum
#End Region

#Region " Global Variables "

    Public curPath As String = My.Settings.startDir
    Dim selectedButtons As New HashSet(Of LCARS.IDataControl)
    Dim selMode As SelectMode
    Dim selStart As Point
    Dim clickStart As DateTime
    Dim moved As Boolean
    Dim mySelection As New frmSelect()
    Dim selectTime As TimeSpan = TimeSpan.FromMilliseconds(500)
    Dim WithEvents clipListener As New ClipboardListener(Me)
    Dim networkScanBusy As Boolean = False
    Dim cachedSmbHosts As New List(Of SmbDiscoveredHost)()
    Private networkScanStartedOnce As Boolean = False

#End Region

#Region " Selection Support "
    Private Const SelectMoveDeadZone As Integer = 12

    Private ReadOnly Property cancelClick() As Boolean
        Get
            Return moved OrElse (Now - clickStart) > selectTime
        End Get
    End Property

    ''' <summary>
    ''' True when Click Mode is Double (tap selects, double-tap opens).
    ''' </summary>
    Private Function IsDoubleClickMode() As Boolean
        Return String.Equals(My.Settings.ClickMode, "Double", StringComparison.OrdinalIgnoreCase)
    End Function

    ''' <summary>
    ''' Message shown when an action needs a selection but none exists.
    ''' </summary>
    Private Function NoSelectionMessage() As String
        Dim how As String
        If IsDoubleClickMode() Then
            how = "Select an item with a single tap (it will stay white)."
        Else
            how = "Select an item by holding your finger/mouse down for about half a second (it will stay white)."
        End If
        Return "No item selected.  " & how & vbNewLine & _
               "You can also select multiple items by dragging on the empty area around the buttons."
    End Function

    Private Sub OnSelectStart()
        If My.Computer.Keyboard.ShiftKeyDown Then
            selMode = SelectMode.Add
        ElseIf My.Computer.Keyboard.CtrlKeyDown Then
            selMode = SelectMode.Symmetric
        Else
            selMode = SelectMode.Clear
            For Each myButton As LCComplexButton In selectedButtons
                myButton.RedAlert = LCARS.LCARSalert.Normal
            Next
            selectedButtons.clear()
        End If
    End Sub

    Private Sub OnSelectionChanged()
        Dim oneSelected As Boolean = (selectedButtons.Count = 1)
        sbRename.Lit = oneSelected
        sbRename.Clickable = oneSelected
        sbOpenWith.Lit = oneSelected
        sbPinToStart.Lit = oneSelected
        Dim atLeastOne As Boolean = (selectedButtons.Count > 0)
        sbProperties.Lit = atLeastOne
    End Sub

    ''' <summary>
    ''' Applies selection highlight and updates the selection set for one item.
    ''' </summary>
    Private Sub ApplyItemSelection(ByVal ctrl As LCComplexButton)
        If selMode = SelectMode.Symmetric AndAlso selectedButtons.contains(ctrl) Then
            selectedButtons.remove(ctrl)
            ctrl.RedAlert = LCARS.LCARSalert.Normal
        Else
            selectedButtons.add(ctrl)
            ctrl.RedAlert = LCARS.LCARSalert.White
        End If
        OnSelectionChanged()
    End Sub

    Private Sub item_MouseDown(ByVal sender As Object, ByVal e As MouseEventArgs)
        If e.Button <> Windows.Forms.MouseButtons.Left Then Return
        clickStart = Now
        moved = False
        selStart = e.Location
        OnSelectStart()
    End Sub

    Private Sub item_MouseMove(ByVal sender As Object, ByVal e As MouseEventArgs)
        If e.Button <> Windows.Forms.MouseButtons.Left Then Return
        Dim dx As Integer = Math.Abs(e.X - selStart.X)
        Dim dy As Integer = Math.Abs(e.Y - selStart.Y)
        If dx > SelectMoveDeadZone OrElse dy > SelectMoveDeadZone Then
            moved = True
        End If
    End Sub

    Private Sub item_Click(ByVal sender As Object, ByVal e As EventArgs)
        ' Double mode: tap selects immediately. Single mode: hold-to-select (selectTime).
        ' Open is wired separately via associateClickHandler (Click or DoubleClick).
        If moved Then Return

        Dim shouldSelect As Boolean
        If IsDoubleClickMode() Then
            shouldSelect = True
        Else
            shouldSelect = (Now - clickStart) > selectTime
        End If
        If Not shouldSelect Then Return

        Dim ctrl As LCComplexButton = DirectCast(sender, LCComplexButton)
        ApplyItemSelection(ctrl)
    End Sub

    Private Sub pnlMyComp_MouseDown(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles gridMyComp.MouseDown
        If e.Button <> Windows.Forms.MouseButtons.Left Then Return
        OnSelectStart()
        moved = False
        selStart = e.Location
    End Sub

    Private Sub pnlMyComp_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles gridMyComp.MouseMove
        If e.Button = Windows.Forms.MouseButtons.Left Then
            Dim dx As Integer = Math.Abs(e.X - selStart.X)
            Dim dy As Integer = Math.Abs(e.Y - selStart.Y)
            If dx > SelectMoveDeadZone OrElse dy > SelectMoveDeadZone Then
                moved = True
            End If
            If Not moved Then Return

            Dim selectionRect As Rectangle = getSelectionRect(e.Location)
            mySelection.Bounds = gridMyComp.RectangleToScreen(selectionRect)
            If Not mySelection.Visible Then
                mySelection.Show()
            End If

            checkSelected(selectionRect)
        End If
    End Sub

    Private Sub pnlMyComp_MouseUp(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles gridMyComp.MouseUp
        If moved Then
            checkSelected(getSelectionRect(e.Location), True)
        End If
        mySelection.Hide()
        OnSelectionChanged()
    End Sub

    Private Function getSelectionRect(ByVal p As Point) As Rectangle
        Dim x1, x2, y1, y2 As Integer

        If p.X > selStart.X Then
            x1 = selStart.X
            x2 = p.X
        Else
            x1 = p.X
            x2 = selStart.X
        End If

        If p.Y > selStart.Y Then
            y1 = selStart.Y
            y2 = p.Y
        Else
            y1 = p.Y
            y2 = selStart.Y
        End If

        Return New Rectangle(x1, y1, x2 - x1, y2 - y1)
    End Function

    Private Sub checkSelected(ByVal selectionRect As Rectangle, Optional ByVal rememberSelection As Boolean = False)
        Dim myButton As LCComplexButton
        For i As Integer = gridMyComp.CurrentPage * gridMyComp.PageSize _
                To Math.Min(gridMyComp.Count - 1, _
                            (gridMyComp.CurrentPage + 1) * gridMyComp.PageSize - 1)
            myButton = DirectCast(gridMyComp.Items(i), LCComplexButton)

            If myButton.Bounds.IntersectsWith(selectionRect) Then
                If selMode = SelectMode.Symmetric Then
                    If selectedButtons.contains(myButton) Then
                        myButton.RedAlert = LCARS.LCARSalert.Normal
                        If rememberSelection Then selectedButtons.remove(myButton)
                    Else
                        myButton.RedAlert = LCARS.LCARSalert.White
                        If rememberSelection Then selectedButtons.add(myButton)
                    End If
                Else
                    myButton.RedAlert = LCARS.LCARSalert.White
                    If rememberSelection Then
                        selectedButtons.add(myButton)
                    End If
                End If
            Else
                If selectedButtons.contains(myButton) Then
                    myButton.RedAlert = LCARS.LCARSalert.White
                Else
                    myButton.RedAlert = LCARS.LCARSalert.Normal
                End If
            End If
        Next
    End Sub
#End Region

    Private Sub frmMyComp_KeyDown(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles Me.KeyDown
        If e.KeyData = Keys.PageDown Then
            If gridMyComp.CurrentPage + 1 < gridMyComp.PageCount Then
                gridMyComp.CurrentPage += 1
            End If
        ElseIf e.KeyData = Keys.PageUp Then
            If gridMyComp.CurrentPage > 0 Then
                gridMyComp.CurrentPage -= 1
            End If
        End If
    End Sub

    Private Sub frmMyComp_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        My.Settings.TryUpgrade()
        If Command() <> "" Then
            Dim arg As String = Command().Trim().Trim(""""c)
            If NetworkPlacesRoot.IsNetworkRoot(arg) Then
                curPath = NetworkPlacesRoot.NetworkRootToken
            ElseIf arg <> "" AndAlso Directory.Exists(arg) Then
                curPath = arg
            ElseIf arg.StartsWith("\\") Then
                curPath = arg
            End If
        End If
        Clipboard_Changed(Me, EventArgs.Empty)
        OnSelectionChanged()
        loadShortcuts()
        LCARS.SetBeeping(Me)
        loadDir(curPath)
    End Sub

    Protected Overrides Sub OnShellChromeLayout()
        ' Keep CLOSE on the lower-right Actions column: same shape/size as GO TO / REFRESH, not the shell pill.
        If sbClose Is Nothing Then Return
        Dim actionsX As Integer = If(sbGoTo IsNot Nothing, sbGoTo.Left, 533)
        Dim btnW As Integer = If(sbGoTo IsNot Nothing, sbGoTo.Width, 87)
        Dim btnH As Integer = If(sbGoTo IsNot Nothing, sbGoTo.Height, 26)
        sbClose.ButtonStyle = LCARS.Controls.StandardButton.LCARSbuttonStyles.RoundedSquare
        sbClose.Size = New Size(btnW, btnH)
        sbClose.ButtonTextAlign = ContentAlignment.BottomRight
        sbClose.ButtonTextHeight = 14
        sbClose.Color = LCARS.LCARScolorStyles.FunctionOffline
        sbClose.ButtonText = "CLOSE"
        sbClose.Text = "CLOSE"
        Dim y As Integer = ClientSize.Height - 78
        If y < 0 Then y = 0
        sbClose.Location = New Point(actionsX, y)
        sbClose.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        sbClose.BringToFront()
    End Sub

    Private Sub loadMyComp()
        gridMyComp.Clear()
        gridMyComp.ControlSize = New Size((gridMyComp.Width - 38) \ 2, 30)

        gridMyComp.Text = "MY COMPUTER"
        sbUpDir.Lit = False

        pnlVisible.Visible = False
        pnlEdit.Visible = False

        Dim beeping As Boolean = LCARS.x32.modSettings.ButtonBeep
        For Each myDrive As DriveInfo In DriveInfo.GetDrives()
            Dim myButton As New LCComplexButton
            myButton.HoldDraw = True
            myButton.Data = myDrive.RootDirectory.FullName()
            myButton.Beeping = beeping
            myButton.HoldDraw = False
            AddHandler myButton.MouseDown, AddressOf item_MouseDown
            AddHandler myButton.MouseMove, AddressOf item_MouseMove
            AddHandler myButton.Click, AddressOf item_Click

            If myDrive.IsReady Then
                myButton.Color = LCARS.LCARScolorStyles.NavigationFunction
                If myDrive.VolumeLabel = "" Then
                    myButton.Text = "Local Disk (" & myDrive.Name & ")"
                Else
                    myButton.Text = myDrive.VolumeLabel & " (" & myDrive.Name & ")"
                End If
                myButton.SideText = ToDriveSize(myDrive.TotalSize)
                associateClickHandler(myButton, AddressOf directory_click)
            Else
                myButton.Color = LCARS.LCARScolorStyles.FunctionUnavailable
                myButton.Text = "DRIVE OFFLINE (" & myDrive.Name & ")"
                myButton.SideText = "--"
                associateClickHandler(myButton, AddressOf offlineDrive_Click)
            End If

            gridMyComp.Add(myButton)
        Next
    End Sub

    Private Sub offlineDrive_Click(ByVal sender As Object, ByVal e As EventArgs)
        If cancelClick Then Return
        MsgBox("Drive not ready.", MsgBoxStyle.OkOnly, "Drive offline")
    End Sub

    Private Sub directory_click(ByVal sender As Object, ByVal e As EventArgs)
        If cancelClick Then Return
        loadDir(CStr(DirectCast(sender, LCComplexButton).Data))
    End Sub

    Private Sub reparsePoint_Click(ByVal sender As Object, ByVal e As EventArgs)
        If cancelClick Then Return
        MsgBox("Reparse points cannot (yet) be traversed", _
               MsgBoxStyle.Information Or MsgBoxStyle.OkOnly, _
               "Not available")
    End Sub

    Public Sub loadDir(ByVal newpath As String)
        If newpath = "" Then
            loadMyComp()
            Return
        End If
        If NetworkPlacesRoot.IsNetworkRoot(newpath) Then
            loadNetworkPlaces()
            Return
        End If
        If IsHostOnlyUnc(newpath) Then
            loadHostShares(newpath.TrimStart("\"c))
            Return
        End If

        Dim infos() As FileSystemInfo = Nothing
        If Not NetworkAccess.TryGetFileSystemInfos(newpath, Me, infos) Then
            Return
        End If

        curPath = newpath
        sbUpDir.Lit = True
        pnlVisible.Visible = True

        Dim title As String = Path.GetFileNameWithoutExtension(curPath)
        If title <> "" Then
            gridMyComp.Text = title
        Else
            gridMyComp.Text = newpath
        End If

        gridMyComp.Clear()
        gridMyComp.ControlSize = New Size(300, 30)
        Dim beeping As Boolean = LCARS.x32.modSettings.ButtonBeep
        For Each curItem As FileSystemInfo In infos
            Dim fileAttr As FileAttributes = curItem.Attributes
            Dim hidden As Boolean = FileHasFlag(fileAttr, FileAttributes.Hidden)
            Dim system As Boolean = FileHasFlag(fileAttr, FileAttributes.System)
            Dim directory As Boolean = FileHasFlag(fileAttr, FileAttributes.Directory)
            Dim reparsePoint As Boolean = FileHasFlag(fileAttr, FileAttributes.ReparsePoint)
            Dim canStat As Boolean = True
            Dim subDirs() As FileSystemInfo = Nothing
            If directory And Not reparsePoint Then
                Try
                    subDirs = DirectCast(curItem, DirectoryInfo).GetDirectories()
                Catch ex As Exception
                    canStat = False
                End Try
            End If
            If hidden And Not My.Settings.showHidden Or _
                system And Not My.Settings.showSystem Or _
                reparsePoint And Not My.Settings.showReparse Or _
                My.Settings.check And Not canStat Then
                Continue For
            End If

            Dim myButton As New LCComplexButton()

            myButton.HoldDraw = True
            myButton.Text = curItem.Name
            myButton.Data = curItem.FullName
            myButton.Beeping = beeping
            myButton.HoldDraw = False
            If My.Settings.dimHidden Then myButton.Lit = Not hidden

            AddHandler myButton.MouseDown, AddressOf item_MouseDown
            AddHandler myButton.MouseMove, AddressOf item_MouseMove
            AddHandler myButton.Click, AddressOf item_Click

            If reparsePoint Then
                myButton.Color = LCARS.LCARScolorStyles.FunctionUnavailable
                myButton.SideText = "--"
                associateClickHandler(myButton, AddressOf reparsePoint_Click)
            ElseIf directory Then
                If canStat Then
                    Dim curDir As DirectoryInfo = CType(curItem, DirectoryInfo)
                    myButton.Color = LCARS.LCARScolorStyles.NavigationFunction
                    myButton.SideText = subDirs.Length & "." & curDir.GetFiles().Length
                    associateClickHandler(myButton, AddressOf directory_click)
                Else
                    myButton.SideText = "--"
                    myButton.Color = LCARS.LCARScolorStyles.FunctionOffline
                    associateClickHandler(myButton, AddressOf myErrorAlert)
                End If
            Else
                If My.Settings.ColorFiles Then
                    Dim mycolors() As String = myButton.ColorsAvailable.getColors
                    mycolors(LCARS.LCARScolorStyles.MiscFunction) = getExtColor(Path.GetExtension(curItem.Name))
                    myButton.ColorsAvailable.setColors(mycolors)
                End If

                myButton.Color = LCARS.LCARScolorStyles.MiscFunction
                Dim ext As String = Path.GetExtension(curItem.FullName).Replace(".", "")
                If ext <> "" Then
                    If ext.Length > 6 Then
                        ext = ext.Substring(0, 6) & "."
                    End If
                    myButton.SideText = ext.ToUpper
                Else
                    myButton.SideText = "---"
                End If
                associateClickHandler(myButton, AddressOf myFile_Click)
            End If

            gridMyComp.Add(myButton)
        Next
    End Sub

    Private Function IsHostOnlyUnc(ByVal path As String) As Boolean
        If (String.IsNullOrEmpty(path) OrElse path.Trim().Length = 0) OrElse Not path.StartsWith("\\") Then Return False
        Dim rest As String = path.TrimStart("\"c)
        Return rest.Length > 0 AndAlso rest.IndexOf("\"c) < 0
    End Function

    Private Sub loadNetworkPlaces()
        curPath = NetworkPlacesRoot.NetworkRootToken
        gridMyComp.Clear()
        gridMyComp.ControlSize = New Size((gridMyComp.Width - 38) \ 2, 30)
        gridMyComp.Text = "NETWORK PLACES"
        sbUpDir.Lit = False
        pnlVisible.Visible = False
        pnlEdit.Visible = False

        Dim beeping As Boolean = LCARS.x32.modSettings.ButtonBeep

        Dim hdrMapped As New LCComplexButton()
        hdrMapped.HoldDraw = True
        hdrMapped.Text = "MAPPED NETWORK DRIVES"
        hdrMapped.SideText = "---"
        hdrMapped.Color = LCARS.LCARScolorStyles.StaticTan
        hdrMapped.Clickable = False
        hdrMapped.HoldDraw = False
        gridMyComp.Add(hdrMapped)

        Dim drives As List(Of DriveInfo) = NetworkPlacesRoot.GetMappedNetworkDrives()
        If drives.Count = 0 Then
            Dim emptyDrv As New LCComplexButton()
            emptyDrv.HoldDraw = True
            emptyDrv.Text = "(none found)"
            emptyDrv.SideText = "--"
            emptyDrv.Color = LCARS.LCARScolorStyles.FunctionUnavailable
            emptyDrv.Clickable = False
            emptyDrv.HoldDraw = False
            gridMyComp.Add(emptyDrv)
        Else
            For Each myDrive As DriveInfo In drives
                Dim myButton As New LCComplexButton()
                myButton.HoldDraw = True
                myButton.Data = myDrive.RootDirectory.FullName()
                myButton.Beeping = beeping
                myButton.HoldDraw = False
                AddHandler myButton.MouseDown, AddressOf item_MouseDown
                AddHandler myButton.MouseMove, AddressOf item_MouseMove
                AddHandler myButton.Click, AddressOf item_Click
                If myDrive.IsReady Then
                    myButton.Color = LCARS.LCARScolorStyles.NavigationFunction
                    If myDrive.VolumeLabel = "" Then
                        myButton.Text = "Network Drive (" & myDrive.Name & ")"
                    Else
                        myButton.Text = myDrive.VolumeLabel & " (" & myDrive.Name & ")"
                    End If
                    Try
                        myButton.SideText = ToDriveSize(myDrive.TotalSize)
                    Catch
                        myButton.SideText = "--"
                    End Try
                    associateClickHandler(myButton, AddressOf directory_click)
                Else
                    myButton.Color = LCARS.LCARScolorStyles.FunctionUnavailable
                    myButton.Text = "DRIVE OFFLINE (" & myDrive.Name & ")"
                    myButton.SideText = "--"
                    associateClickHandler(myButton, AddressOf offlineDrive_Click)
                End If
                gridMyComp.Add(myButton)
            Next
        End If

        Dim hdrLan As New LCComplexButton()
        hdrLan.HoldDraw = True
        hdrLan.Text = "ON THIS NETWORK"
        hdrLan.SideText = "SCAN"
        hdrLan.Color = LCARS.LCARScolorStyles.PrimaryFunction
        hdrLan.Beeping = beeping
        hdrLan.HoldDraw = False
        AddHandler hdrLan.Click, AddressOf networkScanHeader_Click
        gridMyComp.Add(hdrLan)

        If cachedSmbHosts.Count = 0 Then
            Dim emptyHost As New LCComplexButton()
            emptyHost.HoldDraw = True
            emptyHost.Text = If(networkScanBusy OrElse Not networkScanStartedOnce, "(scanning…)", "(none found — tap SCAN above)")
            emptyHost.SideText = "--"
            emptyHost.Color = LCARS.LCARScolorStyles.FunctionUnavailable
            emptyHost.Clickable = False
            emptyHost.HoldDraw = False
            gridMyComp.Add(emptyHost)
        Else
            For Each host As SmbDiscoveredHost In cachedSmbHosts
                Dim hostBtn As New LCComplexButton()
                hostBtn.HoldDraw = True
                hostBtn.Text = host.DisplayText()
                Dim hostTarget As String
                If host.IsLocal Then
                    hostTarget = Environment.MachineName
                ElseIf Not String.IsNullOrEmpty(host.Hostname) AndAlso host.Hostname.Trim().Length > 0 Then
                    hostTarget = host.Hostname.Trim()
                Else
                    hostTarget = host.IpAddress
                End If
                hostBtn.Data = "\\" & hostTarget
                hostBtn.SideText = If(host.IsLocal, "LOCAL", "SMB")
                hostBtn.Color = LCARS.LCARScolorStyles.NavigationFunction
                hostBtn.Beeping = beeping
                hostBtn.HoldDraw = False
                AddHandler hostBtn.MouseDown, AddressOf item_MouseDown
                AddHandler hostBtn.MouseMove, AddressOf item_MouseMove
                AddHandler hostBtn.Click, AddressOf item_Click
                associateClickHandler(hostBtn, AddressOf networkHost_Click)
                gridMyComp.Add(hostBtn)
            Next
        End If

        If Not networkScanStartedOnce Then
            networkScanStartedOnce = True
            BeginInvoke(New MethodInvoker(AddressOf BeginNetworkScan))
        End If
    End Sub

    Private Sub networkScanHeader_Click(ByVal sender As Object, ByVal e As EventArgs)
        BeginNetworkScan()
    End Sub

    Private Sub networkHost_Click(ByVal sender As Object, ByVal e As EventArgs)
        If cancelClick Then Return
        Dim data As String = CStr(DirectCast(sender, LCComplexButton).Data)
        loadDir(data)
    End Sub

    Private Sub BeginNetworkScan()
        If networkScanBusy Then Return
        networkScanBusy = True
        gridMyComp.Text = "NETWORK PLACES (SCANNING…)"
        Dim bw As New System.ComponentModel.BackgroundWorker()
        AddHandler bw.DoWork, Sub(s, args) args.Result = NetworkPlacesRoot.ScanSmbHosts()
        AddHandler bw.RunWorkerCompleted,
            Sub(s, args)
                networkScanBusy = False
                If args.Error IsNot Nothing Then
                    MsgBox("Network scan failed: " & args.Error.Message, MsgBoxStyle.OkOnly Or MsgBoxStyle.Exclamation, "SCAN")
                ElseIf args.Result IsNot Nothing Then
                    cachedSmbHosts = CType(args.Result, List(Of SmbDiscoveredHost))
                End If
                If NetworkPlacesRoot.IsNetworkRoot(curPath) Then
                    loadNetworkPlaces()
                End If
            End Sub
        bw.RunWorkerAsync()
    End Sub

    Private Sub loadHostShares(ByVal host As String)
        Dim hostClean As String = host.Trim().TrimStart("\"c)
        Dim slash As Integer = hostClean.IndexOf("\"c)
        If slash > 0 Then hostClean = hostClean.Substring(0, slash)

        curPath = "\\" & hostClean
        sbUpDir.Lit = True
        pnlVisible.Visible = False
        pnlEdit.Visible = False
        gridMyComp.Clear()
        gridMyComp.ControlSize = New Size((gridMyComp.Width - 38) \ 2, 30)
        gridMyComp.Text = "SHARES: " & hostClean

        Dim shares As List(Of String) = Nothing
        Try
            shares = SmbShareEnumerator.ListShares(hostClean)
        Catch ex As Exception
            If NetworkAccess.IsUnauthorizedOrNetworkError(ex) Then
                Dim infos() As FileSystemInfo = Nothing
                ' Force cred prompt path via a dummy share open attempt
                Dim dummy As String = "\\" & hostClean & "\IPC$"
                NetworkAccess.TryGetFileSystemInfos(dummy, Me, infos)
                Try
                    shares = SmbShareEnumerator.ListShares(hostClean)
                Catch
                    shares = New List(Of String)()
                End Try
            Else
                MsgBox("Unable to list shares on " & hostClean & vbCrLf & ex.Message, MsgBoxStyle.OkOnly Or MsgBoxStyle.Exclamation, "SHARES")
                shares = New List(Of String)()
            End If
        End Try

        Dim beeping As Boolean = LCARS.x32.modSettings.ButtonBeep
        If shares Is Nothing OrElse shares.Count = 0 Then
            Dim empty As New LCComplexButton()
            empty.HoldDraw = True
            empty.Text = "(no shares found)"
            empty.SideText = "--"
            empty.Color = LCARS.LCARScolorStyles.FunctionUnavailable
            empty.Clickable = False
            empty.HoldDraw = False
            gridMyComp.Add(empty)
            Return
        End If

        For Each shareName As String In shares
            Dim myButton As New LCComplexButton()
            myButton.HoldDraw = True
            myButton.Text = shareName
            myButton.Data = "\\" & hostClean & "\" & shareName
            myButton.SideText = "SHARE"
            myButton.Color = LCARS.LCARScolorStyles.NavigationFunction
            myButton.Beeping = beeping
            myButton.HoldDraw = False
            AddHandler myButton.MouseDown, AddressOf item_MouseDown
            AddHandler myButton.MouseMove, AddressOf item_MouseMove
            AddHandler myButton.Click, AddressOf item_Click
            associateClickHandler(myButton, AddressOf directory_click)
            gridMyComp.Add(myButton)
        Next
    End Sub


    Private Sub myFile_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        If cancelClick Then Return
        Dim btn As LCComplexButton = DirectCast(sender, LCComplexButton)
        Dim file As String = DirectCast(btn.Data, String)
        If MediaLauncher.TryOpenInLcarsMedia(file) Then Return
        Try
            Dim myNewProcess As New System.Diagnostics.ProcessStartInfo
            Dim myProcess As Process

            myNewProcess.FileName = file
            myNewProcess.WorkingDirectory = curPath
            myProcess = Process.Start(myNewProcess)
        Catch
            Try
                Shell(file, AppWinStyle.NormalFocus)
            Catch ex As Exception
                MsgBox("Error: " & vbNewLine & vbNewLine & ex.Message)
            End Try
        End Try
    End Sub

    Private Sub sbProperties_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles sbProperties.Click
        If selectedButtons.Count > 0 Then
            Dim props As New frmProperties(getSelectedFiles())
            dockDialog(props)
        Else
            MsgBox(NoSelectionMessage(), MsgBoxStyle.Exclamation, "ERROR: NO ITEM SELECTED")
        End If
    End Sub

    Private Sub dockDialog(ByVal dialog As Form)
        enableNavigation(False)
        AddHandler dialog.FormClosed, AddressOf dialog_closed
        dialog.Size = gridMyComp.Size
        dialog.Location = Point.Empty
        dialog.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Bottom Or AnchorStyles.Right
        gridMyComp.Controls.Add(dialog)
        dialog.Show()
        dialog.BringToFront()
        dialog.Focus()
    End Sub

    Private Sub dialog_closed(ByVal sender As Object, ByVal e As EventArgs)
        gridMyComp.Controls.Remove(CType(sender, Form))
        enableNavigation(True)
    End Sub

    Public Sub enableNavigation(ByVal en As Boolean)
        pnlVisible.Enabled = en
        sbUpDir.Enabled = en
        sbProperties.Enabled = en
        sbOptions.Enabled = en
        sbGoTo.Enabled = en
        sbRefresh.Enabled = en
    End Sub

    Private Sub sbUpDir_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles sbUpDir.Click
        If curPath = "" Then Return
        If NetworkPlacesRoot.IsNetworkRoot(curPath) Then Return
        If IsHostOnlyUnc(curPath) Then
            loadDir(NetworkPlacesRoot.NetworkRootToken)
            Return
        End If
        Dim parent As String = Path.GetDirectoryName(curPath)
        If String.IsNullOrEmpty(parent) OrElse parent = "\" Then
            If curPath.StartsWith("\\") Then
                loadDir(NetworkPlacesRoot.NetworkRootToken)
            Else
                loadDir("")
            End If
            Return
        End If
        ' UNC share root parent is \\host — show share list, not My Computer
        If IsHostOnlyUnc(parent) OrElse (parent.StartsWith("\\") AndAlso parent.TrimStart("\"c).IndexOf("\"c) < 0) Then
            loadDir(parent)
            Return
        End If
        loadDir(parent)
    End Sub

    Private Function getSelectedFiles() As String()
        Dim myFiles As New List(Of String)(selectedButtons.Count)
        For Each mybutton As LCComplexButton In selectedButtons
            myFiles.Add(DirectCast(mybutton.Data, String))
        Next
        Return myFiles.ToArray()
    End Function

    Private Sub sbDelete_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles sbDelete.Click
        Dim result As MsgBoxResult = MsgBox( _
                "Are you sure you want to delete the selected file(s)/folder(s)?!", _
                MsgBoxStyle.YesNo, "DELETE?")

        If result = MsgBoxResult.Yes Then
            Dim form As New frmCopying(getSelectedFiles(), "", FileActions.Delete)
            AddHandler form.TaskCompleted, AddressOf Task_Finished
            form.Show()
        End If
    End Sub

    Private Sub sbCopy_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles sbCopy.Click
        Dim myfiles() As String = getSelectedFiles()

        If myfiles.Length > 0 Then
            Dim myStream As New MemoryStream(4)
            Dim bytes() As Byte = {5, 0, 0, 0}

            myStream.Write(bytes, 0, bytes.Length)
            Dim data_object As New DataObject()
            data_object.SetData("FileDrop", True, myfiles)
            data_object.SetData("Preferred DropEffect", myStream)

            Clipboard.Clear()
            Clipboard.SetDataObject(data_object, True)

        End If
    End Sub

    Private Sub sbCut_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles sbCut.Click
        Dim myfiles() As String = getSelectedFiles()

        If myfiles.Length > 0 Then
            Dim myStream As New MemoryStream(4)
            Dim bytes() As Byte = {2, 0, 0, 0}

            myStream.Write(bytes, 0, bytes.Length)
            Dim data_object As New DataObject()
            data_object.SetData("FileDrop", True, myfiles)
            data_object.SetData("Preferred DropEffect", myStream)

            Clipboard.Clear()
            Clipboard.SetDataObject(data_object, True)

        End If
    End Sub

    Private Sub Clipboard_Changed(ByVal sender As Object, ByVal e As EventArgs) Handles clipListener.ClipboardChanged
        sbPaste.Lit = Clipboard.ContainsFileDropList()
        sbPaste.Clickable = sbPaste.Lit
    End Sub

    Private Sub sbPaste_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles sbPaste.Click
        If Clipboard.ContainsFileDropList Then
            Dim data As IDataObject = Clipboard.GetDataObject

            Dim files() As String = DirectCast(Clipboard.GetData(DataFormats.FileDrop), String())
            Dim MyStream As MemoryStream = DirectCast(data.GetData("Preferred DropEffect", True), MemoryStream)
            Dim flag As Integer = MyStream.ReadByte

            If flag = 2 Then
                'cut
                Dim form As New frmCopying(files, curPath, FileActions.Cut)
                AddHandler form.TaskCompleted, AddressOf Task_Finished
                form.Show()
                Clipboard.Clear()
            Else
                'copy
                Dim form As New frmCopying(files, curPath, FileActions.Copy)
                AddHandler form.TaskCompleted, AddressOf Task_Finished
                form.Show()
            End If
        End If
    End Sub

    Private Sub sbRename_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles sbRename.Click
        If selectedButtons.Count = 1 Then
            Dim path As String = DirectCast(selectedButtons.first().Data, String)
            Dim ren As New frmRename(path)
            dockDialog(ren)
            AddHandler ren.FormClosed, AddressOf dialog_closed_reload
            pnlEdit.Visible = False
        End If
    End Sub

    Private Sub dialog_closed_reload(ByVal sender As Object, ByVal e As EventArgs)
        Dim tmpPage As Integer = gridMyComp.CurrentPage
        loadDir(curPath)
        gridMyComp.CurrentPage = tmpPage
    End Sub

    Private Sub sbClose_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles sbClose.Click
        Me.Close()
    End Sub

    Private Sub sbNewFolder_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles sbNewFolder.Click
        Dim strName As String 'New Folder button added by Tim 5/26/11
        strName = inputbox("Enter the name of the new folder", "New Folder", "New Folder")
        strName = Path.Combine(curPath, strName)
        If Not Directory.Exists(strName) Then
            IO.Directory.CreateDirectory(strName)
            loadDir(curPath)
        End If
    End Sub

    Private Sub sbOpenWith_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles sbOpenWith.Click
        If (selectedButtons.Count = 1) Then
            Dim mySelect As frmFileSelect = New frmFileSelect("C:\Program Files\", ".exe,.bat,", "Select program executable")
            mySelect.ShowDialog()
            If (mySelect.DialogResult = Windows.Forms.DialogResult.OK) Then
                Dim newProg As String = mySelect.ReturnPath
                Shell("""" & newProg & """" & " """ & CStr(selectedButtons.first().Data) & """", AppWinStyle.NormalFocus)
            End If
        Else
            MsgBox("Please select one file", MsgBoxStyle.Information)
        End If
    End Sub

    Private Sub sbPinToStart_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles sbPinToStart.Click
        Dim files() As String = getSelectedFiles()
        If files.Length <> 1 Then
            MsgBox("Select one file or shortcut to pin.", MsgBoxStyle.Information, "PIN TO LCARS START")
            Return
        End If
        Dim result As String = ExplorerLcarsStartPin.PinSelected(files(0))
        MsgBox(result, MsgBoxStyle.Information, "PIN TO LCARS START")
    End Sub

    Private Sub sbOptions_Click(ByVal senter As System.Object, ByVal e As System.EventArgs) Handles sbOptions.Click
        frmOptions.ShowDialog()
        loadDir(curPath)
        loadShortcuts()
    End Sub

    Private Sub sbEdit_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles sbEdit.Click
        pnlEdit.Visible = Not pnlEdit.Visible
        If pnlEdit.Visible Then
            pnlEdit.BringToFront()
            pnlSystemDefined.Visible = False
        End If
    End Sub

    Private Sub sbGoTo_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles sbGoTo.Click
        pnlSystemDefined.Visible = Not pnlSystemDefined.Visible
        If pnlSystemDefined.Visible Then
            pnlEdit.Visible = False
            pnlSystemDefined.BringToFront()
        End If
    End Sub
    Private Sub loadShortcuts()
        pnlShortcuts.Controls.Clear()
        For i As Integer = 0 To My.Settings.shortcuts.Count - 1
            Dim myShortcut As New LCARS.Controls.StandardButton
            Dim shortcutPath As String = My.Settings.shortcuts.Item(i)
            With myShortcut
                .holdDraw = True
                .Text = My.Settings.shortcutNames.Item(i)
                If shortcutPath.StartsWith(SystemShortcut.systemPrefix) Then
                    Dim s As SystemShortcut = SystemShortcut.FromSettingsName(shortcutPath)
                    If s Is Nothing Then Continue For
                    .Color = LCARS.LCARScolorStyles.SystemFunction
                    .Data = s.Location
                Else
                    .Color = LCARS.LCARScolorStyles.NavigationFunction
                    .Data = shortcutPath
                End If
                .ButtonStyle = LCARS.Controls.StandardButton.LCARSbuttonStyles.RoundedSquare
                .Width = pnlShortcuts.Width
                .Height = 26
                .Top = i * 32
            End With
            AddHandler myShortcut.Click, AddressOf MyShortcut_Click
            myShortcut.holdDraw = False
            pnlShortcuts.Controls.Add(myShortcut)
        Next
    End Sub
    Private Sub MyShortcut_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        pnlSystemDefined.Visible = False
        loadDir(DirectCast(DirectCast(sender, LCARS.IDataControl).Data, String))
    End Sub
    Private Sub myErrorAlert(ByVal sender As Object, ByVal e As EventArgs)
        If cancelClick Then Return
        LCARS.Alerts.ActivateAlert("Red", Me.Handle)
        MsgBox("Error: Access Denied", MsgBoxStyle.Critical, "Access Denied")
        LCARS.Alerts.DeactivateAlert(Me.Handle)
    End Sub

    Private Sub sbRefresh_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles sbRefresh.Click
        If NetworkPlacesRoot.IsNetworkRoot(curPath) Then
            BeginNetworkScan()
            Return
        End If
        loadDir(curPath)
    End Sub

    Private Sub sbEnterPath_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles sbEnterPath.Click
        pnlSystemDefined.Visible = False
        Dim newPath As String = inputbox("Enter a path to go to.", "Enter Path", curPath)
        If Directory.Exists(newPath) And (Not newPath = "") Then
            loadDir(newPath)
        End If
    End Sub

    Private Sub sbSaveCurrent_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles sbSaveCurrent.Click
        My.Settings.shortcuts.Add(curPath)
        My.Settings.shortcutNames.Add(tbTitle.Text)
        My.Settings.Save()
        loadShortcuts()
    End Sub

    'Changes page on mouse scroll
    Private Sub Me_MouseScroll(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles Me.MouseWheel
        If e.Delta > 0 Then
            If gridMyComp.CurrentPage > 0 Then
                gridMyComp.CurrentPage -= 1
            End If
        Else
            If gridMyComp.CurrentPage - 1 < gridMyComp.PageCount Then
                gridMyComp.CurrentPage += 1
            End If
        End If
    End Sub

    Private Sub Task_Finished(ByVal sender As Object, ByVal e As System.EventArgs)
        loadDir(curPath)
    End Sub

    Private Sub gridMyComp_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles gridMyComp.TextChanged
        Me.Text = gridMyComp.Text
        tbTitle.Text = gridMyComp.Text
    End Sub

    Private Sub abShortcutsUp_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles abShortcutsUp.Click
        If pnlShortcuts.Controls(0).Top < 0 Then
            Dim offset As Integer = pnlShortcuts.Controls(0).Height + 6
            For Each ctrl As Control In pnlShortcuts.Controls
                ctrl.Top += offset
                ctrl.Visible = ctrl.Top >= 0 AndAlso ctrl.Bottom <= pnlShortcuts.Height
            Next
        End If
    End Sub

    Private Sub abShortcutsDown_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles abShortcutsDown.Click
        If pnlShortcuts.Controls(pnlShortcuts.Controls.Count - 1).Bottom > pnlShortcuts.Height Then
            Dim offset As Integer = pnlShortcuts.Controls(0).Height + 6
            For Each ctrl As Control In pnlShortcuts.Controls
                ctrl.Top -= offset
                ctrl.Visible = ctrl.Top >= 0 AndAlso ctrl.Bottom <= pnlShortcuts.Height
            Next
        End If
    End Sub
End Class
