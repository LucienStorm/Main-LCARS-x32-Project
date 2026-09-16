Option Strict On

Imports System.Collections.Generic
Imports System.IO
Imports System.Text
Imports System.Xml

''' <summary>
''' LCARS-owned Start layout (pins, hidden items, app-focused mode, newly installed).
''' Stored under LocalAppData so USB/portable installs can back it up. Does not replace
''' Personal Programs (registry UserButtons) or Explorer SHORTCUTS (My.Settings).
''' </summary>
Public Module StartMenuLayout

    Public Const ModeTraditional As String = "traditional"
    Public Const ModeAppFocused As String = "appFocused"
    Public Const NewlyInstalledDays As Integer = 7

    Public Class PinEntry
        Public Name As String = ""
        Public Path As String = ""
    End Class

    Public Class LayoutState
        Public Mode As String = ModeTraditional
        Public Pins As New List(Of PinEntry)
        Public HiddenPaths As New List(Of String)
        Public NewlyInstalled As New List(Of PinEntry)
        Public KnownInstallPaths As New List(Of String)
        Public LastProgramsScanUtc As String = ""
    End Class

    Private cached As LayoutState = Nothing

    Public Function LayoutFilePath() As String
        Dim dir As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LCARS x32")
        Return Path.Combine(dir, "start-layout.xml")
    End Function

    Public Function LoadLayout() As LayoutState
        If cached IsNot Nothing Then Return cached
        Dim state As New LayoutState()
        Dim path As String = LayoutFilePath()
        Try
            If File.Exists(path) Then
                Dim doc As New XmlDocument()
                doc.Load(path)
                Dim root As XmlElement = doc.DocumentElement
                If root IsNot Nothing Then
                    Dim modeNode As XmlNode = root.SelectSingleNode("mode")
                    If modeNode IsNot Nothing AndAlso Not String.IsNullOrEmpty(modeNode.InnerText) Then
                        state.Mode = modeNode.InnerText.Trim()
                    End If
                    Dim lastScan As XmlNode = root.SelectSingleNode("lastProgramsScanUtc")
                    If lastScan IsNot Nothing Then state.LastProgramsScanUtc = lastScan.InnerText.Trim()
                    ReadPinList(root.SelectSingleNode("pins"), state.Pins)
                    ReadStringList(root.SelectSingleNode("hidden"), state.HiddenPaths)
                    ReadPinList(root.SelectSingleNode("newlyInstalled"), state.NewlyInstalled)
                    ReadStringList(root.SelectSingleNode("knownInstalls"), state.KnownInstallPaths)
                End If
            End If
        Catch
            state = New LayoutState()
        End Try
        PruneNewlyInstalled(state)
        cached = state
        Return state
    End Function

    Public Sub SaveLayout(ByVal state As LayoutState)
        If state Is Nothing Then Return
        PruneNewlyInstalled(state)
        Dim filePath As String = LayoutFilePath()
        Dim dir As String = IO.Path.GetDirectoryName(filePath)
        If Not Directory.Exists(dir) Then Directory.CreateDirectory(dir)
        Dim settings As New XmlWriterSettings()
        settings.Indent = True
        settings.Encoding = New UTF8Encoding(False)
        Using writer As XmlWriter = XmlWriter.Create(filePath, settings)
            writer.WriteStartDocument()
            writer.WriteStartElement("startLayout")
            writer.WriteElementString("mode", state.Mode)
            writer.WriteElementString("lastProgramsScanUtc", state.LastProgramsScanUtc)
            WritePinList(writer, "pins", state.Pins)
            WriteStringList(writer, "hidden", state.HiddenPaths)
            WritePinList(writer, "newlyInstalled", state.NewlyInstalled)
            WriteStringList(writer, "knownInstalls", state.KnownInstallPaths)
            writer.WriteEndElement()
            writer.WriteEndDocument()
        End Using
        cached = state
    End Sub

    Public Sub InvalidateCache()
        cached = Nothing
    End Sub

    Public Sub SetMode(ByVal mode As String)
        Dim state As LayoutState = LoadLayout()
        state.Mode = If(String.Equals(mode, ModeAppFocused, StringComparison.OrdinalIgnoreCase), ModeAppFocused, ModeTraditional)
        SaveLayout(state)
    End Sub

    Public Function IsAppFocused() As Boolean
        Return String.Equals(LoadLayout().Mode, ModeAppFocused, StringComparison.OrdinalIgnoreCase)
    End Function

    Public Sub ToggleMode()
        SetMode(If(IsAppFocused(), ModeTraditional, ModeAppFocused))
    End Sub

    Public Sub PinApplication(ByVal displayName As String, ByVal targetPath As String)
        If String.IsNullOrEmpty(targetPath) Then Return
        Dim state As LayoutState = LoadLayout()
        For Each p As PinEntry In state.Pins
            If String.Equals(p.Path, targetPath, StringComparison.OrdinalIgnoreCase) Then Return
        Next
        Dim entry As New PinEntry()
        entry.Name = If(String.IsNullOrEmpty(displayName), Path.GetFileNameWithoutExtension(targetPath), displayName)
        entry.Path = targetPath
        state.Pins.Add(entry)
        ' Un-hide if previously removed in edit mode.
        state.HiddenPaths.RemoveAll(Function(h) String.Equals(h, targetPath, StringComparison.OrdinalIgnoreCase))
        SaveLayout(state)
    End Sub

    Public Sub UnpinAt(ByVal index As Integer)
        Dim state As LayoutState = LoadLayout()
        If index < 0 OrElse index >= state.Pins.Count Then Return
        state.Pins.RemoveAt(index)
        SaveLayout(state)
    End Sub

    Public Sub MovePin(ByVal index As Integer, ByVal delta As Integer)
        Dim state As LayoutState = LoadLayout()
        Dim newIndex As Integer = index + delta
        If index < 0 OrElse index >= state.Pins.Count Then Return
        If newIndex < 0 OrElse newIndex >= state.Pins.Count Then Return
        Dim item As PinEntry = state.Pins(index)
        state.Pins.RemoveAt(index)
        state.Pins.Insert(newIndex, item)
        SaveLayout(state)
    End Sub

    Public Sub HideItem(ByVal targetPathOrName As String)
        If String.IsNullOrEmpty(targetPathOrName) Then Return
        Dim state As LayoutState = LoadLayout()
        For Each h As String In state.HiddenPaths
            If String.Equals(h, targetPathOrName, StringComparison.OrdinalIgnoreCase) Then Return
        Next
        state.HiddenPaths.Add(targetPathOrName)
        SaveLayout(state)
    End Sub

    ''' <summary>
    ''' Builds the virtual root directory shown in Start: optional pinned/newly-installed
    ''' sections first (app-focused), then classic All Programs (filtered by hidden).
    ''' </summary>
    Public Function BuildDisplayRoot(ByVal classicRoot As programList.DirectoryStartItem) As programList.DirectoryStartItem
        Dim state As LayoutState = LoadLayout()
        DetectNewlyInstalled(classicRoot, state)

        Dim root As New programList.DirectoryStartItem()
        root.Name = "Programs"

        ' Always surface system tools (Control Panel, etc.) at the top level.
        Dim systemDir As New programList.DirectoryStartItem()
        systemDir.Name = "System"
        systemDir.subItems.Add(MakeFileItem("Control Panel", "control.exe"))
        Try
            Dim sysRoot As String = Environment.GetFolderPath(Environment.SpecialFolder.System)
            Dim settingsCmd As String = Path.Combine(sysRoot, "control.exe")
            If File.Exists(settingsCmd) Then
                systemDir.subItems.Add(MakeFileItem("Programs and Features", "appwiz.cpl"))
                systemDir.subItems.Add(MakeFileItem("Device Manager", "devmgmt.msc"))
                systemDir.subItems.Add(MakeFileItem("Network Connections", "ncpa.cpl"))
            End If
        Catch
        End Try
        root.subItems.Add(systemDir)

        If IsAppFocused() Then
            If state.Pins.Count > 0 Then
                Dim pinsDir As New programList.DirectoryStartItem()
                pinsDir.Name = "Pinned"
                For Each p As PinEntry In state.Pins
                    pinsDir.subItems.Add(MakeFileItem(p.Name, p.Path))
                Next
                root.subItems.Add(pinsDir)
            End If
            PruneNewlyInstalled(state)
            If state.NewlyInstalled.Count > 0 Then
                Dim neu As New programList.DirectoryStartItem()
                neu.Name = "Newly Installed"
                For Each p As PinEntry In state.NewlyInstalled
                    Dim exePath As String = p.Path
                    Dim tab As Integer = exePath.IndexOf(vbTab)
                    If tab > 0 Then exePath = exePath.Substring(0, tab)
                    neu.subItems.Add(MakeFileItem(p.Name, exePath))
                Next
                root.subItems.Add(neu)
            End If
            Dim allDir As New programList.DirectoryStartItem()
            allDir.Name = "All Programs"
            CopyFiltered(classicRoot, allDir, state)
            root.subItems.Add(allDir)
        Else
            ' Traditional: classic tree, with pinned folder prepended when present.
            If state.Pins.Count > 0 Then
                Dim pinsDir As New programList.DirectoryStartItem()
                pinsDir.Name = "Pinned"
                For Each p As PinEntry In state.Pins
                    pinsDir.subItems.Add(MakeFileItem(p.Name, p.Path))
                Next
                root.subItems.Add(pinsDir)
            End If
            CopyFiltered(classicRoot, root, state)
        End If
        Return root
    End Function

    Private Sub CopyFiltered(ByVal source As programList.DirectoryStartItem, ByVal dest As programList.DirectoryStartItem, ByVal state As LayoutState)
        If source Is Nothing OrElse source.subItems Is Nothing Then Return
        For Each item As programList.StartItem In source.subItems
            If TypeOf item Is programList.DirectoryStartItem Then
                Dim srcDir As programList.DirectoryStartItem = CType(item, programList.DirectoryStartItem)
                If IsHidden(state, srcDir.Name) Then Continue For
                Dim child As New programList.DirectoryStartItem()
                child.Name = srcDir.Name
                CopyFiltered(srcDir, child, state)
                dest.subItems.Add(child)
            ElseIf TypeOf item Is programList.FileStartItem Then
                Dim srcFile As programList.FileStartItem = CType(item, programList.FileStartItem)
                Dim exe As String = ""
                Try
                    exe = srcFile.Link.Executable
                Catch
                End Try
                If IsHidden(state, exe) OrElse IsHidden(state, srcFile.Name) Then Continue For
                dest.subItems.Add(srcFile)
            End If
        Next
    End Sub

    Private Function IsHidden(ByVal state As LayoutState, ByVal key As String) As Boolean
        If String.IsNullOrEmpty(key) Then Return False
        For Each h As String In state.HiddenPaths
            If String.Equals(h, key, StringComparison.OrdinalIgnoreCase) Then Return True
        Next
        Return False
    End Function

    Private Function MakeFileItem(ByVal name As String, ByVal exePath As String) As programList.FileStartItem
        Dim f As New programList.FileStartItem()
        f.Name = name
        Dim link As programList.LNKinfo
        link.Executable = exePath
        link.Args = ""
        f.Link = link
        Return f
    End Function

    ''' <summary>
    ''' First scan records all known exes; later scans treat new leaf .exe/.lnk targets as newly installed.
    ''' Also merges Uninstall registry + Desktop shortcuts so installs outside Start Menu still appear.
    ''' </summary>
    Private Sub DetectNewlyInstalled(ByVal classicRoot As programList.DirectoryStartItem, ByVal state As LayoutState)
        Dim found As New List(Of PinEntry)
        CollectExecutables(classicRoot, found)
        CollectDesktopShortcuts(found)
        CollectUninstallRegistry(found)
        If state.KnownInstallPaths.Count = 0 Then
            For Each p As PinEntry In found
                state.KnownInstallPaths.Add(p.Path)
            Next
            state.LastProgramsScanUtc = DateTime.UtcNow.ToString("o")
            SaveLayout(state)
            Return
        End If
        Dim known As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        For Each k As String In state.KnownInstallPaths
            known.Add(k)
        Next
        Dim changed As Boolean = False
        For Each p As PinEntry In found
            If Not known.Contains(p.Path) Then
                known.Add(p.Path)
                state.KnownInstallPaths.Add(p.Path)
                Dim already As Boolean = False
                For Each existing As PinEntry In state.NewlyInstalled
                    Dim existingPath As String = existing.Path
                    Dim tab As Integer = existingPath.IndexOf(vbTab)
                    If tab > 0 Then existingPath = existingPath.Substring(0, tab)
                    If String.Equals(existingPath, p.Path, StringComparison.OrdinalIgnoreCase) Then
                        already = True
                        Exit For
                    End If
                Next
                If Not already Then
                    Dim aged As New PinEntry()
                    aged.Name = p.Name
                    aged.Path = p.Path & vbTab & DateTime.UtcNow.ToString("o")
                    state.NewlyInstalled.Add(aged)
                End If
                changed = True
            End If
        Next
        If changed Then
            state.LastProgramsScanUtc = DateTime.UtcNow.ToString("o")
            SaveLayout(state)
        End If
    End Sub

    Private Sub CollectDesktopShortcuts(ByVal into As List(Of PinEntry))
        Try
            CollectShortcutsFromFolder(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), into)
            CollectShortcutsFromFolder(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), into)
        Catch
        End Try
    End Sub

    Private Sub CollectShortcutsFromFolder(ByVal folder As String, ByVal into As List(Of PinEntry))
        If String.IsNullOrEmpty(folder) OrElse Not Directory.Exists(folder) Then Return
        For Each f As String In Directory.GetFiles(folder, "*.lnk")
            Try
                Dim name As String = Path.GetFileNameWithoutExtension(f)
                Dim p As New PinEntry()
                p.Name = name
                p.Path = f
                into.Add(p)
            Catch
            End Try
        Next
    End Sub

    Private Sub CollectUninstallRegistry(ByVal into As List(Of PinEntry))
        CollectUninstallHive(Microsoft.Win32.Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", into)
        CollectUninstallHive(Microsoft.Win32.Registry.LocalMachine, "SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall", into)
        CollectUninstallHive(Microsoft.Win32.Registry.CurrentUser, "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", into)
        CollectAppPaths(Microsoft.Win32.Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths", into)
        CollectAppPaths(Microsoft.Win32.Registry.LocalMachine, "SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\App Paths", into)
    End Sub

    Private Sub CollectAppPaths(ByVal root As Microsoft.Win32.RegistryKey, ByVal subPath As String, ByVal into As List(Of PinEntry))
        Try
            Using key As Microsoft.Win32.RegistryKey = root.OpenSubKey(subPath, False)
                If key Is Nothing Then Return
                For Each name As String In key.GetSubKeyNames()
                    Try
                        Using app As Microsoft.Win32.RegistryKey = key.OpenSubKey(name, False)
                            If app Is Nothing Then Continue For
                            Dim defVal As Object = app.GetValue(Nothing)
                            If defVal Is Nothing Then Continue For
                            Dim exePath As String = defVal.ToString().Trim(""""c).Trim()
                            If Not exePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) Then Continue For
                            If Not File.Exists(exePath) Then Continue For
                            Dim p As New PinEntry()
                            p.Name = Path.GetFileNameWithoutExtension(name)
                            p.Path = exePath
                            into.Add(p)
                        End Using
                    Catch
                    End Try
                Next
            End Using
        Catch
        End Try
    End Sub

    Private Sub CollectUninstallHive(ByVal root As Microsoft.Win32.RegistryKey, ByVal subPath As String, ByVal into As List(Of PinEntry))
        Try
            Using key As Microsoft.Win32.RegistryKey = root.OpenSubKey(subPath, False)
                If key Is Nothing Then Return
                For Each name As String In key.GetSubKeyNames()
                    Try
                        Using app As Microsoft.Win32.RegistryKey = key.OpenSubKey(name, False)
                            If app Is Nothing Then Continue For
                            Dim display As Object = app.GetValue("DisplayName")
                            If display Is Nothing Then Continue For
                            Dim displayName As String = display.ToString()
                            If String.IsNullOrEmpty(displayName) Then Continue For
                            Dim systemComp As Object = app.GetValue("SystemComponent")
                            If systemComp IsNot Nothing AndAlso CInt(systemComp) = 1 Then Continue For
                            Dim releaseType As Object = app.GetValue("ReleaseType")
                            If releaseType IsNot Nothing Then Continue For
                            Dim loc As Object = app.GetValue("DisplayIcon")
                            Dim pathHint As String = If(loc Is Nothing, "", loc.ToString())
                            If String.IsNullOrEmpty(pathHint) Then
                                Dim u As Object = app.GetValue("UninstallString")
                                If u IsNot Nothing Then pathHint = u.ToString()
                            End If
                            If String.IsNullOrEmpty(pathHint) Then Continue For
                            ' Normalize "C:\path\app.exe,0" style icons.
                            Dim comma As Integer = pathHint.IndexOf(","c)
                            If comma > 0 Then pathHint = pathHint.Substring(0, comma)
                            pathHint = pathHint.Trim(""""c).Trim()
                            If Not pathHint.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) Then Continue For
                            If Not File.Exists(pathHint) Then Continue For
                            Dim p As New PinEntry()
                            p.Name = displayName
                            p.Path = pathHint
                            into.Add(p)
                        End Using
                    Catch
                    End Try
                Next
            End Using
        Catch
        End Try
    End Sub

    Private Sub CollectExecutables(ByVal dir As programList.DirectoryStartItem, ByVal into As List(Of PinEntry))
        If dir Is Nothing OrElse dir.subItems Is Nothing Then Return
        For Each item As programList.StartItem In dir.subItems
            If TypeOf item Is programList.DirectoryStartItem Then
                CollectExecutables(CType(item, programList.DirectoryStartItem), into)
            ElseIf TypeOf item Is programList.FileStartItem Then
                Dim f As programList.FileStartItem = CType(item, programList.FileStartItem)
                Dim exe As String = ""
                Try
                    exe = f.Link.Executable
                Catch
                End Try
                If String.IsNullOrEmpty(exe) Then Continue For
                Dim p As New PinEntry()
                p.Name = Path.GetFileNameWithoutExtension(f.Name)
                p.Path = exe
                into.Add(p)
            End If
        Next
    End Sub

    Private Sub PruneNewlyInstalled(ByVal state As LayoutState)
        If state Is Nothing OrElse state.NewlyInstalled Is Nothing Then Return
        Dim keep As New List(Of PinEntry)
        Dim cutoff As DateTime = DateTime.UtcNow.AddDays(-NewlyInstalledDays)
        For Each p As PinEntry In state.NewlyInstalled
            Dim raw As String = p.Path
            Dim tab As Integer = raw.IndexOf(vbTab)
            Dim whenUtc As DateTime = DateTime.UtcNow
            If tab > 0 Then
                DateTime.TryParse(raw.Substring(tab + 1), Nothing, Globalization.DateTimeStyles.RoundtripKind, whenUtc)
                Dim cleaned As New PinEntry()
                cleaned.Name = p.Name
                cleaned.Path = raw
                If whenUtc >= cutoff Then keep.Add(cleaned)
            Else
                keep.Add(p)
            End If
        Next
        state.NewlyInstalled = keep
    End Sub

    Private Sub ReadPinList(ByVal node As XmlNode, ByVal list As List(Of PinEntry))
        If node Is Nothing Then Return
        For Each child As XmlNode In node.ChildNodes
            If child.NodeType <> XmlNodeType.Element Then Continue For
            Dim p As New PinEntry()
            Dim n As XmlNode = child.SelectSingleNode("name")
            Dim pathNode As XmlNode = child.SelectSingleNode("path")
            If n IsNot Nothing Then p.Name = n.InnerText
            If pathNode IsNot Nothing Then p.Path = pathNode.InnerText
            If Not String.IsNullOrEmpty(p.Path) Then list.Add(p)
        Next
    End Sub

    Private Sub ReadStringList(ByVal node As XmlNode, ByVal list As List(Of String))
        If node Is Nothing Then Return
        For Each child As XmlNode In node.ChildNodes
            If child.NodeType <> XmlNodeType.Element Then Continue For
            Dim text As String = child.InnerText
            If Not String.IsNullOrEmpty(text) Then list.Add(text)
        Next
    End Sub

    Private Sub WritePinList(ByVal writer As XmlWriter, ByVal elementName As String, ByVal list As List(Of PinEntry))
        writer.WriteStartElement(elementName)
        For Each p As PinEntry In list
            writer.WriteStartElement("item")
            writer.WriteElementString("name", p.Name)
            writer.WriteElementString("path", p.Path)
            writer.WriteEndElement()
        Next
        writer.WriteEndElement()
    End Sub

    Private Sub WriteStringList(ByVal writer As XmlWriter, ByVal elementName As String, ByVal list As List(Of String))
        writer.WriteStartElement(elementName)
        For Each s As String In list
            writer.WriteElementString("item", s)
        Next
        writer.WriteEndElement()
    End Sub

End Module
