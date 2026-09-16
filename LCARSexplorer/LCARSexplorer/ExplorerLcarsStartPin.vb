Option Strict On

Imports System.Collections.Generic
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Xml
Imports Microsoft.Win32

''' <summary>
''' Writes pins into the shared LCARS Start layout store and asks the shell to refresh.
''' </summary>
Friend Module ExplorerLcarsStartPin

    Private Declare Auto Function RegisterWindowMessage Lib "user32.dll" (ByVal lpString As String) As UInteger
    Private Declare Auto Function PostMessage Lib "user32.dll" (ByVal hWnd As IntPtr, ByVal Msg As UInteger, ByVal wParam As IntPtr, ByVal lParam As IntPtr) As Boolean
    Private Const HWND_BROADCAST As Integer = &HFFFF
    Private Const RefreshStartLParam As Integer = 21

    Public Function LayoutFilePath() As String
        Dim dir As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LCARS x32")
        Return Path.Combine(dir, "start-layout.xml")
    End Function

    ''' <summary>
    ''' Pins a file/shortcut target to LCARS Start and notifies the shell.
    ''' </summary>
    Public Function PinSelected(ByVal filePath As String) As String
        If String.IsNullOrEmpty(filePath) OrElse Not File.Exists(filePath) Then
            Return "Select a file or shortcut first."
        End If
        Dim target As String = filePath
        Dim displayName As String = Path.GetFileNameWithoutExtension(filePath)
        Dim ext As String = Path.GetExtension(filePath).ToLowerInvariant()
        If ext = ".lnk" Then
            Try
                Dim resolved As String = ResolveShortcutTarget(filePath)
                If Not String.IsNullOrEmpty(resolved) Then target = resolved
            Catch
            End Try
        End If
        AppendPin(displayName, target)
        NotifyLcarsRefreshStart()
        Return "Pinned to LCARS Start: " & displayName
    End Function

    Private Sub AppendPin(ByVal displayName As String, ByVal targetPath As String)
        Dim pins As New List(Of KeyValuePair(Of String, String))
        Dim mode As String = "traditional"
        Dim hidden As New List(Of String)
        Dim newly As New List(Of KeyValuePair(Of String, String))
        Dim known As New List(Of String)
        Dim lastScan As String = ""
        Dim layoutPath As String = LayoutFilePath()
        If File.Exists(layoutPath) Then
            Try
                Dim doc As New XmlDocument()
                doc.Load(layoutPath)
                Dim root As XmlElement = doc.DocumentElement
                If root IsNot Nothing Then
                    Dim modeNode As XmlNode = root.SelectSingleNode("mode")
                    If modeNode IsNot Nothing Then mode = modeNode.InnerText.Trim()
                    Dim lastNode As XmlNode = root.SelectSingleNode("lastProgramsScanUtc")
                    If lastNode IsNot Nothing Then lastScan = lastNode.InnerText.Trim()
                    ReadPairs(root.SelectSingleNode("pins"), pins)
                    ReadStrings(root.SelectSingleNode("hidden"), hidden)
                    ReadPairs(root.SelectSingleNode("newlyInstalled"), newly)
                    ReadStrings(root.SelectSingleNode("knownInstalls"), known)
                End If
            Catch
            End Try
        End If
        For Each p As KeyValuePair(Of String, String) In pins
            If String.Equals(p.Value, targetPath, StringComparison.OrdinalIgnoreCase) Then
                Return
            End If
        Next
        pins.Add(New KeyValuePair(Of String, String)(displayName, targetPath))
        hidden.RemoveAll(Function(h) String.Equals(h, targetPath, StringComparison.OrdinalIgnoreCase))

        Dim dir As String = IO.Path.GetDirectoryName(layoutPath)
        If Not Directory.Exists(dir) Then Directory.CreateDirectory(dir)
        Dim settings As New XmlWriterSettings()
        settings.Indent = True
        settings.Encoding = New UTF8Encoding(False)
        Using writer As XmlWriter = XmlWriter.Create(layoutPath, settings)
            writer.WriteStartDocument()
            writer.WriteStartElement("startLayout")
            writer.WriteElementString("mode", mode)
            writer.WriteElementString("lastProgramsScanUtc", lastScan)
            WritePairs(writer, "pins", pins)
            WriteStrings(writer, "hidden", hidden)
            WritePairs(writer, "newlyInstalled", newly)
            WriteStrings(writer, "knownInstalls", known)
            writer.WriteEndElement()
            writer.WriteEndDocument()
        End Using
    End Sub

    Private Sub NotifyLcarsRefreshStart()
        Try
            Dim msg As UInteger = RegisterWindowMessage("LCARS_X32_MSG")
            Dim handleText As String = GetSetting("LCARS x32", "Application", "MainWindowHandle", "0")
            Dim handleValue As Integer = 0
            Integer.TryParse(handleText, handleValue)
            If handleValue <> 0 Then
                PostMessage(New IntPtr(handleValue), msg, IntPtr.Zero, New IntPtr(RefreshStartLParam))
            Else
                PostMessage(New IntPtr(HWND_BROADCAST), msg, IntPtr.Zero, New IntPtr(RefreshStartLParam))
            End If
        Catch
        End Try
    End Sub

    Private Function ResolveShortcutTarget(ByVal lnkPath As String) As String
        ' Lightweight .lnk parse via WScript if available; otherwise keep the .lnk path.
        Try
            Dim shellType As Type = Type.GetTypeFromProgID("WScript.Shell")
            If shellType Is Nothing Then Return lnkPath
            Dim shell As Object = Activator.CreateInstance(shellType)
            Dim shortcut As Object = shellType.InvokeMember("CreateShortcut", Reflection.BindingFlags.InvokeMethod, Nothing, shell, New Object() {lnkPath})
            Dim target As Object = shortcut.GetType().InvokeMember("TargetPath", Reflection.BindingFlags.GetProperty, Nothing, shortcut, Nothing)
            If target IsNot Nothing Then
                Dim s As String = CStr(target)
                If Not String.IsNullOrEmpty(s) Then Return s
            End If
        Catch
        End Try
        Return lnkPath
    End Function

    Private Sub ReadPairs(ByVal node As XmlNode, ByVal list As List(Of KeyValuePair(Of String, String)))
        If node Is Nothing Then Return
        For Each child As XmlNode In node.ChildNodes
            If child.NodeType <> XmlNodeType.Element Then Continue For
            Dim n As XmlNode = child.SelectSingleNode("name")
            Dim p As XmlNode = child.SelectSingleNode("path")
            Dim name As String = If(n Is Nothing, "", n.InnerText)
            Dim pathVal As String = If(p Is Nothing, "", p.InnerText)
            If Not String.IsNullOrEmpty(pathVal) Then
                list.Add(New KeyValuePair(Of String, String)(name, pathVal))
            End If
        Next
    End Sub

    Private Sub ReadStrings(ByVal node As XmlNode, ByVal list As List(Of String))
        If node Is Nothing Then Return
        For Each child As XmlNode In node.ChildNodes
            If child.NodeType <> XmlNodeType.Element Then Continue For
            If Not String.IsNullOrEmpty(child.InnerText) Then list.Add(child.InnerText)
        Next
    End Sub

    Private Sub WritePairs(ByVal writer As XmlWriter, ByVal elementName As String, ByVal list As List(Of KeyValuePair(Of String, String)))
        writer.WriteStartElement(elementName)
        For Each p As KeyValuePair(Of String, String) In list
            writer.WriteStartElement("item")
            writer.WriteElementString("name", p.Key)
            writer.WriteElementString("path", p.Value)
            writer.WriteEndElement()
        Next
        writer.WriteEndElement()
    End Sub

    Private Sub WriteStrings(ByVal writer As XmlWriter, ByVal elementName As String, ByVal list As List(Of String))
        writer.WriteStartElement(elementName)
        For Each s As String In list
            writer.WriteElementString("item", s)
        Next
        writer.WriteEndElement()
    End Sub

End Module
