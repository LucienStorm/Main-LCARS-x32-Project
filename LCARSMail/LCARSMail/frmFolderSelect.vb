Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms
Imports LCARS
Imports LCARS.Controls
Imports Microsoft.VisualBasic.CompilerServices
    Public Class frmFolderSelect
        Inherits LCARSForm
        Private Const RowH As Integer = 36

        Private Const RowGap As Integer = 4

        Private Const PlacesHub As String = ""

        Private currentPath As String

        Private ReadOnly promptTitle As String

        Private lblPath As Label

        Private folderHost As LcarsScrollPanel

        Private fbUp As FlatButton

        Private fbPin As FlatButton

        Private fbSelect As FlatButton

        Private fbCancel As FlatButton

        Private fbTitle As FlatButton

        Private fbPlaces As FlatButton

        Public Property SelectedPath As String

        Public Sub New(Optional title As String = "SELECT FOLDER", Optional startPath As String = Nothing)
            currentPath = ""
            promptTitle = (If(String.IsNullOrWhiteSpace(title), "SELECT FOLDER", title.Trim().ToUpperInvariant()))
            CType(CObj(Me), Form).Text = "LCARS COMMS — FOLDER"
            CType(CObj(Me), Form).BackColor = Color.Black
            CType(CObj(Me), Control).ForeColor = Color.Orange
            CType(CObj(Me), Form).FormBorderStyle = FormBorderStyle.None
            CType(CObj(Me), Form).ControlBox = False
            Me.DoubleBuffered = True
            CType(Me, LCARSForm).BindToWorkingArea = False
            CType(CObj(Me), Form).StartPosition = FormStartPosition.CenterParent
            Dim clientSize As Size = New Size(680, 500)
            CType(CObj(Me), Form).ClientSize = clientSize
            CType(CObj(Me), Form).ShowInTaskbar = False
            fbTitle = New FlatButton()
            CType(fbTitle, LCARSbuttonClass).ButtonText = promptTitle
            CType(fbTitle, LCARSbuttonClass).Text = promptTitle
            CType(fbTitle, LCARSbuttonClass).Clickable = False
            CType(fbTitle, LCARSbuttonClass).Color = CType(8, LCARScolorStyles)
            CType(fbTitle, LCARSbuttonClass).ButtonTextAlign = ContentAlignment.BottomRight
            Dim obj As FlatButton = fbTitle
            clientSize = New Size(320, 32)
            CType(CObj(obj), Control).Size = clientSize
            Dim obj2 As FlatButton = fbTitle
            Dim location As Point = New Point(12, 8)
            CType(CObj(obj2), Control).Location = location
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbTitle), Control))
            lblPath = New Label()
            lblPath.AutoSize = False
            lblPath.ForeColor = Color.Orange
            lblPath.BackColor = Color.Black
            lblPath.Font = New Font("Arial Narrow", 11F, FontStyle.Bold)
            Dim label = lblPath
            location = New Point(12, 46)
            label.Location = location
            Dim label2 = lblPath
            clientSize = New Size(656, 24)
            label2.Size = clientSize
            CType(CObj(Me), Control).Controls.Add(lblPath)
            folderHost = New LcarsScrollPanel()
            Dim obj3 As LcarsScrollPanel = folderHost
            location = New Point(12, 74)
            CType(CObj(obj3), Control).Location = location
            Dim obj4 As LcarsScrollPanel = folderHost
            clientSize = New Size(656, 360)
            CType(CObj(obj4), Control).Size = clientSize
            CType(CObj(folderHost), Control).BackColor = Color.Black
            folderHost.CollapseTrackWhenIdle = True
            CType(CObj(Me), Control).Controls.Add(CType(CObj(folderHost), Control))
            location = New Point(12, 450)
            fbPlaces = frmFolderSelect.MakeBtn("PLACES", CType(1, LCARScolorStyles), location)
            AddHandler CType(CObj(fbPlaces), Control).Click, AddressOf OnPlacesClick
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbPlaces), Control))
            location = New Point(124, 450)
            fbUp = frmFolderSelect.MakeBtn("UP", CType(4, LCARScolorStyles), location)
            AddHandler CType(CObj(fbUp), Control).Click, AddressOf OnUpClick
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbUp), Control))
            location = New Point(236, 450)
            fbPin = frmFolderSelect.MakeBtn("PIN", CType(2, LCARScolorStyles), location)
            AddHandler CType(CObj(fbPin), Control).Click, AddressOf OnPinClick
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbPin), Control))
            location = New Point(348, 450)
            fbSelect = frmFolderSelect.MakeBtn("SELECT", CType(6, LCARScolorStyles), location)
            AddHandler CType(CObj(fbSelect), Control).Click, AddressOf OnSelectClick
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbSelect), Control))
            location = New Point(568, 450)
            fbCancel = frmFolderSelect.MakeBtn("CANCEL", CType(9, LCARScolorStyles), location)
            AddHandler CType(CObj(fbCancel), Control).Click, AddressOf OnCancelClick
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbCancel), Control))
            Dim text As String = If(Not Equals(startPath, Nothing), startPath, "").Trim()
            If text.Length > 0 AndAlso Directory.Exists(text) Then
                currentPath = text
            Else
                currentPath = ""
            End If
            LoadCurrent()
        End Sub

        Public Shared Function Pick(owner As IWin32Window, Optional title As String = "SELECT FOLDER", Optional startPath As String = Nothing) As String
            Dim frmFolderSelect2 As frmFolderSelect = New frmFolderSelect(title, startPath)
            Try
                If CType(CObj(frmFolderSelect2), Form).ShowDialog(owner) = DialogResult.OK Then
                    Return frmFolderSelect2.SelectedPath
                End If

            Finally
                If frmFolderSelect2 IsNot Nothing Then CType(frmFolderSelect2, IDisposable).Dispose()
            End Try
            Return Nothing
        End Function

        Private Shared Function MakeBtn(caption As String, color As LCARScolorStyles, loc As Point) As FlatButton
            Dim val As FlatButton = New FlatButton()
            CType(val, LCARSbuttonClass).ButtonText = caption
            CType(val, LCARSbuttonClass).Text = caption
            CType(val, LCARSbuttonClass).Color = color
            CType(val, LCARSbuttonClass).ButtonTextAlign = ContentAlignment.BottomRight
            Dim size As Size = New Size(100, 28)
            CType(CObj(val), Control).Size = size
            CType(CObj(val), Control).Location = loc
            Return val
        End Function

        Private Sub LoadCurrent()
            folderHost.ContentPanel.Controls.Clear()
            Dim flag = String.IsNullOrEmpty(currentPath)
            lblPath.Text = (If(flag, "PLACES — FAVORITES · USER FOLDERS · DRIVES", currentPath.ToUpperInvariant()))
            CType(fbUp, LCARSbuttonClass).Clickable = Not flag
            CType(fbPlaces, LCARSbuttonClass).Clickable = Not flag
            CType(fbSelect, LCARSbuttonClass).Clickable = If(Not flag AndAlso Directory.Exists(currentPath), True, False)
            UpdatePinButton()
            Dim y = 4
            Dim flip = False
            Dim width As Integer = Math.Max(200, folderHost.ContentPanel.Width - 8)
            If flag Then
                y = LoadPlacesHub(y, width, flip)
            Else
                y = LoadDirectoryListing(y, width, flip)
            End If
            folderHost.Relayout()
        End Sub

        Private Function LoadPlacesHub(y As Integer, width As Integer, ByRef flip As Boolean) As Integer
            Dim list As List(Of String) = FolderFavoriteStore.LoadFavorites()
            If list.Count > 0 Then
                y = AddSectionLabel("FAVORITES", y)
                For Each item In list
                    Dim caption = FavoriteCaption(item)
                    AddFolderRow(item, caption, enabled:=True, y, width, flip, CType(6, LCARScolorStyles))
                    y += 40
                    flip = Not flip
                Next
                y += 8
            End If
            y = AddSectionLabel("USER FOLDERS", y)
            For Each item2 In BuiltInUserPlaces()
                If Directory.Exists(item2.Value) Then
                    AddFolderRow(item2.Value, item2.Key, enabled:=True, y, width, flip, CType(4, LCARScolorStyles))
                    y += 40
                    flip = Not flip
                End If
            Next
            y += 8
            y = AddSectionLabel("DRIVES", y)
            Dim drives As DriveInfo() = DriveInfo.GetDrives()
            For Each driveInfo In drives
                Dim flag = False
                Try
                    flag = driveInfo.IsReady
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    Call ProjectData.ClearProjectError()
                End Try
                Dim caption2 As String
                If flag Then
                    Dim text = ""
                    Try
                        text = driveInfo.VolumeLabel
                    Catch projectError2 As Exception
                        ProjectData.SetProjectError(projectError2)
                        Call ProjectData.ClearProjectError()
                    End Try
                    caption2 = (If(Not String.IsNullOrEmpty(text), (text.ToUpperInvariant() & " (" & driveInfo.Name & ")"), "LOCAL DISK (" & driveInfo.Name & ")"))
                Else
                    caption2 = "DRIVE OFFLINE (" & driveInfo.Name & ")"
                End If
                AddFolderRow(driveInfo.RootDirectory.FullName, caption2, flag, y, width, flip, CType(2, LCARScolorStyles))
                y += 40
                flip = Not flip
            Next
            Return y
        End Function

        Private Function LoadDirectoryListing(y As Integer, width As Integer, ByRef flip As Boolean) As Integer
            Dim list As List(Of DirectoryInfo) = New List(Of DirectoryInfo)()
            Dim result As Integer
            Try
                Dim directoryInfo As DirectoryInfo = New DirectoryInfo(currentPath)
                Dim directories As DirectoryInfo() = directoryInfo.GetDirectories()
                For Each directoryInfo2 In directories
                    Try
                        If (directoryInfo2.Attributes And FileAttributes.Hidden) = 0 AndAlso (directoryInfo2.Attributes And FileAttributes.System) = 0 Then
                            list.Add(directoryInfo2)
                        End If
                    Catch projectError As Exception
                        ProjectData.SetProjectError(projectError)
                        Call ProjectData.ClearProjectError()
                    End Try
                Next
            Catch ex As Exception
                ProjectData.SetProjectError(ex)
                Dim ex2 = ex
                LcarsCommsUi.Alert("Cannot open folder:" & vbCrLf & currentPath & vbCrLf & vbCrLf & ex2.Message, "LCARS COMMS — FOLDER ERROR")
                currentPath = ""
                LoadCurrent()
                result = y
                Call ProjectData.ClearProjectError()
                GoTo IL_01b3
            End Try
            list.Sort(Function(a, b) String.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase))
            Dim forcedColor As LCARScolorStyles? = Nothing
            For Each item In list
                AddFolderRow(item.FullName, item.Name.ToUpperInvariant(), enabled:=True, y, width, flip, forcedColor)
                y = y + 40
                flip = Not flip
            Next
            If list.Count = 0 Then
                Dim label As Label = New Label()
                label.Text = "NO SUBFOLDERS — TAP SELECT TO USE THIS FOLDER"
                label.ForeColor = Color.FromArgb(200, 140, 60)
                label.BackColor = Color.Black
                label.AutoSize = True
                Dim location As Point = New Point(8, 12)
                label.Location = location
                folderHost.ContentPanel.Controls.Add(label)
            End If
            result = y
            GoTo IL_01b3
IL_01b3:
            Return result
        End Function

        Private Function AddSectionLabel(text As String, y As Integer) As Integer
            Dim label As Label = New Label()
            label.Text = text
            label.ForeColor = Color.FromArgb(255, 204, 102)
            label.BackColor = Color.Black
            label.Font = New Font("Arial Narrow", 10F, FontStyle.Bold)
            label.AutoSize = True
            Dim location As Point = New Point(8, y)
            label.Location = location
            folderHost.ContentPanel.Controls.Add(label)
            Return y + 22
        End Function

        Private Shared Function BuiltInUserPlaces() As List(Of KeyValuePair(Of String, String))
            Dim list As List(Of KeyValuePair(Of String, String)) = New List(Of KeyValuePair(Of String, String))()
            AddPlace(list, "DESKTOP", Environment.SpecialFolder.DesktopDirectory)
            AddPlace(list, "DOCUMENTS", Environment.SpecialFolder.Personal)
            Call AddPlace(list, "DOWNLOADS", GetDownloadsPath())
            AddPlace(list, "PICTURES", Environment.SpecialFolder.MyPictures)
            AddPlace(list, "MUSIC", Environment.SpecialFolder.MyMusic)
            AddPlace(list, "VIDEOS", Environment.SpecialFolder.MyVideos)
            AddPlace(list, "USER PROFILE", Environment.SpecialFolder.UserProfile)
            Return list
        End Function

        Private Shared Sub AddPlace(list As List(Of KeyValuePair(Of String, String)), caption As String, folder As Environment.SpecialFolder)
            Try
                Dim folderPath = Environment.GetFolderPath(folder)
                If Not String.IsNullOrEmpty(folderPath) Then
                    Dim item As KeyValuePair(Of String, String) = New KeyValuePair(Of String, String)(caption, folderPath)
                    list.Add(item)
                End If
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
        End Sub

        Private Shared Sub AddPlace(list As List(Of KeyValuePair(Of String, String)), caption As String, path As String)
            If Not String.IsNullOrEmpty(path) Then
                Dim item As KeyValuePair(Of String, String) = New KeyValuePair(Of String, String)(caption, path)
                list.Add(item)
            End If
        End Sub

        Private Shared Function GetDownloadsPath() As String
            Try
                Dim folderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                Dim text = Path.Combine(folderPath, "Downloads")
                If Directory.Exists(text) Then
                    Return text
                End If
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
            Return Nothing
        End Function

        Private Shared Function FavoriteCaption(folderPath As String) As String
            Dim result As String
            Try
                Dim path = folderPath.TrimEnd(IO.Path.DirectorySeparatorChar, IO.Path.AltDirectorySeparatorChar)
                Dim text = IO.Path.GetFileName(path)
                If String.IsNullOrEmpty(text) Then
                    text = folderPath
                End If
                result = "* " & text.ToUpperInvariant()
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                result = "* " & folderPath.ToUpperInvariant()
                Call ProjectData.ClearProjectError()
            End Try
            Return result
        End Function

        Private Sub AddFolderRow(fullPath As String, caption As String, enabled As Boolean, y As Integer, width As Integer, flip As Boolean, forcedColor As LCARScolorStyles?)
            Dim val As FlatButton = New FlatButton()
            CType(val, LCARSbuttonClass).ButtonText = caption
            CType(val, LCARSbuttonClass).Text = caption
            CType(val, LCARSbuttonClass).Clickable = enabled
            If Not enabled Then
                CType(val, LCARSbuttonClass).Color = CType(0, LCARScolorStyles)
            ElseIf forcedColor.HasValue Then
                CType(val, LCARSbuttonClass).Color = forcedColor.Value
            Else
                CType(val, LCARSbuttonClass).Color = CType(If(flip, 4, 2), LCARScolorStyles)
            End If
            CType(val, LCARSbuttonClass).ButtonTextAlign = ContentAlignment.MiddleLeft
            CType(val, LCARSbuttonClass).ButtonTextHeight = 14
            Dim size As Size = New Size(width, 36)
            CType(CObj(val), Control).Size = size
            Dim location As Point = New Point(4, y)
            CType(CObj(val), Control).Location = location
            CType(CObj(val), Control).Tag = fullPath
            If enabled Then
                AddHandler CType(CObj(val), Control).Click, AddressOf OnFolderClick
            End If
            folderHost.ContentPanel.Controls.Add(CType(CObj(val), Control))
        End Sub

        Private Sub UpdatePinButton()
            If String.IsNullOrEmpty(currentPath) OrElse Not Directory.Exists(currentPath) Then
                CType(fbPin, LCARSbuttonClass).Clickable = False
                CType(fbPin, LCARSbuttonClass).ButtonText = "PIN"
                CType(fbPin, LCARSbuttonClass).Text = "PIN"
                Return
            End If
            CType(fbPin, LCARSbuttonClass).Clickable = True
            If FolderFavoriteStore.IsFavorite(currentPath) Then
                CType(fbPin, LCARSbuttonClass).ButtonText = "UNPIN"
                CType(fbPin, LCARSbuttonClass).Text = "UNPIN"
                CType(fbPin, LCARSbuttonClass).Color = CType(3, LCARScolorStyles)
            Else
                CType(fbPin, LCARSbuttonClass).ButtonText = "PIN"
                CType(fbPin, LCARSbuttonClass).Text = "PIN"
                CType(fbPin, LCARSbuttonClass).Color = CType(2, LCARScolorStyles)
            End If
        End Sub

        Private Sub OnFolderClick(sender As Object, e As EventArgs)
            Dim val As FlatButton = CType(If((TypeOf sender Is FlatButton), sender, Nothing), FlatButton)
            If val IsNot Nothing Then
                Dim text As String = TryCast(CType(CObj(val), Control).Tag, String)
                If Not String.IsNullOrEmpty(text) AndAlso Directory.Exists(text) Then
                    currentPath = text
                    LoadCurrent()
                End If
            End If
        End Sub

        Private Sub OnPlacesClick(sender As Object, e As EventArgs)
            currentPath = ""
            LoadCurrent()
        End Sub

        Private Sub OnUpClick(sender As Object, e As EventArgs)
            If String.IsNullOrEmpty(currentPath) Then
                Return
            End If
            Try
                Dim text = currentPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                Dim directoryName = Path.GetDirectoryName(text)
                Dim pathRoot = Path.GetPathRoot(text)
                If String.IsNullOrEmpty(directoryName) OrElse String.Equals(directoryName, text, StringComparison.OrdinalIgnoreCase) OrElse String.Equals(text, pathRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase) Then
                    currentPath = ""
                Else
                    currentPath = directoryName
                End If
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                currentPath = ""
                Call ProjectData.ClearProjectError()
            End Try
            LoadCurrent()
        End Sub

        Private Sub OnPinClick(sender As Object, e As EventArgs)
            If String.IsNullOrEmpty(currentPath) OrElse Not Directory.Exists(currentPath) Then
                Return
            End If
            If FolderFavoriteStore.IsFavorite(currentPath) Then
                If Not FolderFavoriteStore.RemoveFavorite(currentPath) Then
                    LcarsCommsUi.Alert(If(Not Equals(FolderFavoriteStore.LastError, Nothing), FolderFavoriteStore.LastError, "UNPIN FAILED"), "LCARS COMMS — FOLDER")
                    Return
                End If
            ElseIf Not FolderFavoriteStore.AddFavorite(currentPath) Then
                LcarsCommsUi.Alert(If(Not Equals(FolderFavoriteStore.LastError, Nothing), FolderFavoriteStore.LastError, "PIN FAILED"), "LCARS COMMS — FOLDER")
                Return
            End If
            UpdatePinButton()
        End Sub

        Private Sub OnSelectClick(sender As Object, e As EventArgs)
            If String.IsNullOrEmpty(currentPath) OrElse Not Directory.Exists(currentPath) Then
                LcarsCommsUi.Alert("Choose a folder from Places (or open a drive), then tap SELECT.", "LCARS COMMS — FOLDER")
                Return
            End If
            SelectedPath = currentPath
            CType(CObj(Me), Form).DialogResult = DialogResult.OK
            Call CType(CObj(Me), Form).Close()
        End Sub

        Private Sub OnCancelClick(sender As Object, e As EventArgs)
            SelectedPath = Nothing
            CType(CObj(Me), Form).DialogResult = DialogResult.Cancel
            Call CType(CObj(Me), Form).Close()
        End Sub
    End Class

