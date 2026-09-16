Imports LCARS.UI
Public Class frmUpdate
    Inherits LCARS.LCARSForm

    Protected Overrides Sub OnLCARSClosing()
        ' Do nothing. Overrides close on LCARS close.
    End Sub

    'Files to have on server:
    '  version.txt: Contains version information for every file in LCARS. 
    '  copy of every file that needs to be updated.
    'version.txt description:
    '  Starts with one-line version designation. This is a date right now, but it can be any GUID
    '  Remainder of file is composed of unlimited number of entries formatted:
    '     [filename]
    '     [version designation]
    '     [download path]
    '     [md5 hash]
    '     [Class]
    '  We do not need an entry for every file in LCARS, only the ones that need to be updated from
    '  the initial release with auto-updates.

    Dim debugSwitch As Boolean = False
    Dim path() As String
    Dim version As String = ""
    Public updateList As New Collection
    Dim componentsLeft As Integer = 0
    Dim silent As Boolean = False
    Private downloadControls As New List(Of Download)
    Private nextDownloadIndex As Integer = 0
    Private downloadPhaseStarted As Boolean = False
    Private nextButtonArmed As Boolean = False
    Private WithEvents nextArmTimer As New System.Windows.Forms.Timer()
    Private Const NextArmDelayMs As Integer = 1000


    Public Structure component
        Dim name As String
        Dim version As String
        Dim downloadPath As String
        Dim md5 As String
        Dim fileClass As String
        Dim data As String
    End Structure

    Private Sub frmUpdate_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        If GetSetting("LCARS x32", "Application", "DebugSwitch", "FALSE") Then
            rtbServer.Visible = True
        End If
        'Determine what update path we're on, and what the server is
        If GetSetting("LCARSUpdate", "Config", "UpdatePath", "release") = "release" Then
            path = GetSetting("LCARSUpdate", "Config", "ReleaseURL", "http://www.lcarsx32.com/lcars/x32/ReleaseVersion.txt").Split(",")
        ElseIf GetSetting("LCARSUpdate", "Config", "UpdatePath", "release") = "experimental" Then
            path = GetSetting("LCARSUpdate", "Config", "ExperimentalURL", "http://www.lcarsx32.com/lcars/x32/ExperimentalVersion.txt").Split(",")
        Else
            path = GetSetting("LCARSUpdate", "Config", "CustomURL").Split(",")
        End If

        'Determine if being run automatically in "silent" mode.
        If Command() = "-s" Then
            silent = True
        End If

        nextArmTimer.Interval = NextArmDelayMs
        AddHandler nextArmTimer.Tick, AddressOf nextArmTimer_Tick

        Dim asmVersion As String = Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()
        tbTitle.ButtonText = "LCARS UPDATE " & asmVersion
        tbTitle.Text = tbTitle.ButtonText

        'Randomize paths
        Dim i As Integer
        Dim rand As New Random()
        For i = 0 To path.Length - 1
            Dim r As Integer = rand.Next(0, path.Length)
            Dim temp As String = path(i)
            path(i) = path(r)
            path(r) = temp
        Next

        Dim checkThread As New Threading.Thread(AddressOf CheckServers)
        checkThread.Start(path)
    End Sub

    Private Sub CheckServers(ByVal path() As String)
        Dim found As Boolean = False
        Dim needsDownload As Boolean = False
        Dim activeServer As String = ""
        For Each myPath As String In path
            Dim checkResult As Integer = CheckVersion(myPath)
            If checkResult = 1 Then
                needsDownload = True
                found = True
                activeServer = myPath
                Exit For
            ElseIf checkResult = 2 Then
                found = True
                activeServer = myPath
                Exit For
            End If
        Next
        If Not found Then
            If Me.InvokeRequired Then
                Me.BeginInvoke(New MethodInvoker(AddressOf ShowNoServerResponse))
            Else
                ShowNoServerResponse()
            End If
            Return
        End If
        If needsDownload Then
            If Me.InvokeRequired Then
                Me.BeginInvoke(New StringHandler(AddressOf ShowUpdateListReady), activeServer)
            Else
                ShowUpdateListReady(activeServer)
            End If
        End If
    End Sub

    Private Delegate Sub StringHandler(ByVal value As String)

    Private Sub ShowNoServerResponse()
        If Not silent Then
            MsgBox("No servers have responded. Try again in a few minutes.")
        End If
        End
    End Sub

    Private Sub ShowUpdateListReady(ByVal serverPath As String)
        lstUpdates.Items.Clear()
        For Each myEntry As component In updateList
            lstUpdates.Items.Add(myEntry.name)
        Next
        rtbServer.Text = serverPath
        lblMessage.Text = "The following components must be updated." & vbCrLf & _
                          "Review the list, then press NEXT when you are ready." & vbCrLf & _
                          "Install folder: " & Application.StartupPath
        sbNext.Visible = True
        sbNext.Clickable = False
        sbNext.Lit = False
        nextButtonArmed = False
        nextArmTimer.Stop()
        nextArmTimer.Start()
    End Sub

    Private Sub nextArmTimer_Tick(ByVal sender As Object, ByVal e As EventArgs)
        nextArmTimer.Stop()
        nextButtonArmed = True
        sbNext.Clickable = True
        sbNext.Lit = True
        lblMessage.Text = "The following components must be updated." & vbCrLf & _
                          "Press NEXT to begin downloading." & vbCrLf & _
                          "Install folder: " & Application.StartupPath
    End Sub

    Private Function CheckVersion(ByVal path As String) As Integer
        ' 0 = server error / not ready, 1 = updates needed, 2 = already up to date
        Try
            updateList = New Collection()
            Dim versionsPath As String = Application.StartupPath & "\versions.txt"
            EnsureVersionsFile(versionsPath)
            Dim localVersions As New ProgramVersions(versionsPath)
            Dim reader As System.IO.StreamReader
            Dim response As System.Net.HttpWebResponse
            Dim request As System.Net.WebRequest = System.Net.WebRequest.Create(path)
            WebRequestHelper.Configure(request, path)
            response = CType(request.GetResponse(), System.Net.HttpWebResponse)
            reader = New System.IO.StreamReader(response.GetResponseStream())
            version = reader.ReadLine()
            If version = "Not ready" Then
                Return 0
            End If

            ' Decide by file content (MD5), not by version labels — survives manual copies and partial installs.
            Do While reader.Peek() >= 0
                Dim myEntry As New component
                myEntry.name = reader.ReadLine()
                myEntry.version = reader.ReadLine()
                myEntry.downloadPath = reader.ReadLine()
                myEntry.md5 = reader.ReadLine()
                myEntry.fileClass = reader.ReadLine()
                If String.IsNullOrEmpty(myEntry.name) Then
                    Continue Do
                End If
                If LocalFileNeedsUpdate(myEntry.name, myEntry.md5) Then
                    updateList.Add(myEntry)
                Else
                    ' File already matches server — keep versions.txt in sync with what is on disk.
                    localVersions.UpdateVersion(myEntry.name, myEntry.version)
                End If
            Loop
            reader.Close()
            If Not response Is Nothing Then
                response.Close()
            End If

            If updateList.Count = 0 Then
                If localVersions.getGlobalVersion() <> version Then
                    localVersions.UpdateGlobalVersion(version)
                End If
                localVersions.SaveFile()
                If Me.InvokeRequired Then
                    Me.BeginInvoke(New MethodInvoker(AddressOf ShowUpToDateAndClose))
                Else
                    ShowUpToDateAndClose()
                End If
                Return 2
            End If

            Return 1
        Catch ex As Exception
            If Not silent Then
                Dim seeError As DialogResult = MsgBox("An error occured while updating." & vbNewLine & _
                                                      "If your computer is protected by a firewall, be sure that LCARS x32 can access the internet." & vbNewLine & _
                                                      "Do you wish to see a detailed error message?", MsgBoxStyle.YesNo)
                If seeError = System.Windows.Forms.DialogResult.Yes Then
                    MsgBox("Server: " & path & vbNewLine & ex.ToString())
                End If
            End If
            Try
                System.IO.File.AppendAllText(My.Computer.FileSystem.SpecialDirectories.Temp & "\lcars-update-error.txt", _
                    DateTime.Now.ToString("u") & " CheckVersion" & vbNewLine & path & vbNewLine & ex.ToString() & vbNewLine & vbNewLine)
            Catch
            End Try
            Return 0
        End Try
    End Function

    Private Sub ShowUpToDateAndClose()
        If Not silent Then
            MsgBox("LCARS x32 is up-to-date.")
        End If
        Me.Close()
    End Sub

    Private Sub EnsureVersionsFile(ByVal versionsPath As String)
        If System.IO.File.Exists(versionsPath) Then
            Return
        End If
        System.IO.File.WriteAllText(versionsPath, "0.0.0.0" & Environment.NewLine)
    End Sub

    ''' <summary>
    ''' True when the install file is missing or its MD5 does not match the server manifest.
    ''' </summary>
    Private Function LocalFileNeedsUpdate(ByVal fileName As String, ByVal expectedMd5 As String) As Boolean
        Dim localPath As String = System.IO.Path.Combine(Application.StartupPath, fileName)
        If Not System.IO.File.Exists(localPath) Then
            Return True
        End If
        Dim actual As String = ComputeFileMd5(localPath)
        If String.IsNullOrEmpty(actual) Then
            Return True
        End If
        Return Not String.Equals(actual, expectedMd5, StringComparison.OrdinalIgnoreCase)
    End Function

    Private Function ComputeFileMd5(ByVal filePath As String) As String
        Try
            Dim hashBytes As Byte()
            Using stream As New System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read)
                Using md5 As New System.Security.Cryptography.MD5CryptoServiceProvider()
                    hashBytes = md5.ComputeHash(stream)
                End Using
            End Using
            Dim builder As New System.Text.StringBuilder()
            Dim b As Byte
            For Each b In hashBytes
                builder.Append(String.Format("{0:x2}", b))
            Next
            Return builder.ToString()
        Catch
            Return ""
        End Try
    End Function

    Private Sub sbCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles sbCancel.Click
        End 'This terminates the downloads too.
    End Sub

    Private Function GetUpdateStagingDirectory() As String
        Dim dir As String = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LCARS x32")
        dir = System.IO.Path.Combine(dir, "UpdateStaging")
        If Not System.IO.Directory.Exists(dir) Then
            System.IO.Directory.CreateDirectory(dir)
        End If
        Return dir
    End Function

    Private Sub sbNext_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles sbNext.Click
        If downloadPhaseStarted Then
            Return
        End If
        If Not nextButtonArmed Then
            Return
        End If
        downloadPhaseStarted = True
        nextArmTimer.Stop()
        sbNext.Clickable = False
        StartDownloadPhase()
    End Sub

    Private Sub StartDownloadPhase()
        ' Downloads begin only after the user presses NEXT.
        'This sub should switch to the download screen and start the download threads
        'At this point, the update can still be canceled.
        'The computer can be used while the download progresses

        'switch to download screen
        sbNext.Visible = False
        pnlDownloadList.Visible = True
        pnlDownloadList.BringToFront()
        lblMessage.Text = "Downloading updates."
        Dim count As Integer = 0
        downloadControls.Clear()
        nextDownloadIndex = 0
        'initialize the download components (one file at a time for slow Wi-Fi / LAN hosts)
        Dim stagingDir As String = GetUpdateStagingDirectory()
        For Each myComponent As component In updateList
            Dim progress As New Download(myComponent.downloadPath, stagingDir & "\" & myComponent.name, myComponent.md5)
            progress.Width = pnlDownloadList.Width - 23
            progress.Height = 100
            progress.Value = 0
            progress.Left = 0
            progress.Top = 105 * count
            progress.TopText = myComponent.name
            progress.BottomText = "Starting"
            pnlDownloadList.Controls.Add(progress)
            AddHandler progress.DownloadComplete, AddressOf downloadCompleted
            AddHandler progress.UpdateFailed, AddressOf updateFailed
            downloadControls.Add(progress)
            count += 1
        Next
        componentsLeft = downloadControls.Count
        If downloadControls.Count > 0 Then
            downloadControls(0).StartDownload()
        End If
    End Sub

    Public Sub downloadCompleted()
        If Me.InvokeRequired Then
            Me.BeginInvoke(New MethodInvoker(AddressOf downloadCompleted))
            Return
        End If
        componentsLeft -= 1
        If componentsLeft > 0 Then
            nextDownloadIndex += 1
            downloadControls(nextDownloadIndex).StartDownload()
            Return
        End If
        If componentsLeft = 0 Then
            FinishDownloadsAndLaunchInstaller()
        End If
    End Sub

    ''' <summary>
    ''' Writes the install script, launches the installer, then exits so file locks (LCARS.dll) are released.
    ''' Prefer a just-downloaded runInstallScript.exe in Temp; never overwrite it with the older install-folder copy.
    ''' </summary>
    Private Sub FinishDownloadsAndLaunchInstaller()
        Dim stagingDir As String = GetUpdateStagingDirectory()
        Try
            sbCancel.Visible = False
            Dim myWriter As New System.IO.StreamWriter(stagingDir & "\script.txt")
            Try
                SaveSetting("LCARS x32", "Application", "InstallPath", Application.StartupPath)
            Catch
            End Try
            myWriter.WriteLine("Program Version")
            myWriter.WriteLine(version)
            myWriter.WriteLine("End Program Version")
            myWriter.WriteLine("Install Path")
            myWriter.WriteLine(Application.StartupPath)
            myWriter.WriteLine("End Install Path")
            myWriter.WriteLine("File List")
            For Each myFile As component In updateList
                If myFile.fileClass = "File" Then
                    myWriter.WriteLine(myFile.name)
                    myWriter.WriteLine(myFile.version)
                End If
            Next
            myWriter.WriteLine("End File List")
            myWriter.WriteLine("Run List")
            For Each myFile As component In updateList
                If myFile.fileClass = "Run" Then
                    myWriter.WriteLine(myFile.name)
                    myWriter.WriteLine(myFile.version)
                End If
            Next
            myWriter.WriteLine("End Run List")
            myWriter.WriteLine("Extract List")
            For Each myFile As component In updateList
                If myFile.fileClass = "Extract" Then
                    myWriter.WriteLine(myFile.name)
                    myWriter.WriteLine(myFile.version)
                End If
            Next
            myWriter.WriteLine("End Extract List")
            myWriter.WriteLine("End Script")
            myWriter.Close()

            EnsureTempTool("runInstallScript.exe", stagingDir)
            EnsureTempTool("Ionic.Zip.Reduced.dll", stagingDir)

            Dim launchNote As String = stagingDir & "\lcars-update-launch.txt"
            Dim installerPath As String = stagingDir & "\runInstallScript.exe"
            Dim launchLines As New System.Text.StringBuilder()
            launchLines.AppendLine(DateTime.Now.ToString("u"))
            launchLines.AppendLine("UpdaterVersion=" & Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString())
            launchLines.AppendLine("InstallFolder=" & Application.StartupPath)
            launchLines.AppendLine("StagingFolder=" & stagingDir)
            launchLines.AppendLine("InstallerPath=" & installerPath)
            launchLines.AppendLine("InstallerExists=" & System.IO.File.Exists(installerPath).ToString())
            launchLines.AppendLine("IonicExists=" & System.IO.File.Exists(stagingDir & "\Ionic.Zip.Reduced.dll").ToString())
            launchLines.AppendLine("ScriptExists=" & System.IO.File.Exists(stagingDir & "\script.txt").ToString())
            System.IO.File.WriteAllText(launchNote, launchLines.ToString())

            ' Machine-wide handoff so the elevated installer finds staging even when
            ' Application.StartupPath is wrong (UAC / host process directories).
            Dim handoffDir As String = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "LCARS x32")
            If Not System.IO.Directory.Exists(handoffDir) Then
                System.IO.Directory.CreateDirectory(handoffDir)
            End If
            System.IO.File.WriteAllText(System.IO.Path.Combine(handoffDir, "current-staging.txt"), stagingDir)

            LaunchInstallerElevated(installerPath, stagingDir, Application.StartupPath)
            System.IO.File.AppendAllText(launchNote, "LaunchProcessStartReturned=True" & vbCrLf)
        Catch ex As Exception
            Dim detail As String = "Could not start the installer." & vbNewLine & vbNewLine & ex.ToString()
            Try
                System.IO.File.AppendAllText(stagingDir & "\lcars-update-error.txt", DateTime.Now.ToString("u") & vbNewLine & detail & vbNewLine & vbNewLine)
            Catch
            End Try
            MsgBox(detail & vbNewLine & vbNewLine & "Details also saved to:" & vbNewLine & stagingDir & "\lcars-update-error.txt")
            Return
        End Try
        ' Brief pause so the elevated process can fully start before this process dies.
        Threading.Thread.Sleep(1500)
        End
    End Sub

    ''' <summary>
    ''' Uses the Temp copy when this update already downloaded it; otherwise copies from the install folder.
    ''' Pass installFolder explicitly — under UAC, Application.StartupPath on the elevated installer can be wrong.
    ''' </summary>
    Private Sub LaunchInstallerElevated(ByVal installerPath As String, ByVal workingDirectory As String, ByVal installFolder As String)
        Dim psi As New ProcessStartInfo()
        psi.FileName = installerPath
        psi.WorkingDirectory = workingDirectory
        psi.UseShellExecute = True
        psi.Verb = "runas"
        ' Arg1 = install folder (USB/portable safe). Staging is resolved via handoff file + Assembly.Location.
        If Not String.IsNullOrEmpty(installFolder) Then
            psi.Arguments = """" & installFolder.TrimEnd("\"c) & """"
        End If
        Try
            Process.Start(psi)
        Catch ex As System.ComponentModel.Win32Exception
            If ex.NativeErrorCode = 1223 Then
                Throw New InvalidOperationException("Update cancelled: administrator approval was required to install files.", ex)
            End If
            Throw
        End Try
    End Sub

    Private Sub EnsureTempTool(ByVal fileName As String, ByVal tempDir As String)
        Dim tempFile As String = tempDir & "\" & fileName
        Dim downloadedThisRun As Boolean = False
        For Each myFile As component In updateList
            If String.Equals(myFile.name, fileName, StringComparison.OrdinalIgnoreCase) Then
                downloadedThisRun = True
                Exit For
            End If
        Next
        If downloadedThisRun AndAlso System.IO.File.Exists(tempFile) Then
            Return
        End If
        Dim sourceFile As String = Application.StartupPath & "\" & fileName
        If System.IO.File.Exists(sourceFile) Then
            My.Computer.FileSystem.CopyFile(sourceFile, tempFile, True)
            Return
        End If
        If System.IO.File.Exists(tempFile) Then
            Return
        End If
        Throw New System.IO.FileNotFoundException("Missing tool required for install: " & fileName, sourceFile)
    End Sub

    Public Sub updateFailed(ByVal sender As Object)
        If Me.InvokeRequired Then
            Me.BeginInvoke(New MethodInvoker(AddressOf ShowUpdateFailed))
            Return
        End If
        ShowUpdateFailed()
    End Sub

    Private Sub ShowUpdateFailed()
        MsgBox("Update failed. Please retry at a later date.")
        sbCancel.doClick(Me, New EventArgs)
    End Sub

    Private Sub pnlDownloadList_Scroll(ByVal sender As Object, ByVal e As System.Windows.Forms.ScrollEventArgs) Handles pnlDownloadList.Scroll
        For Each myDownload As Download In pnlDownloadList.Controls
            myDownload.Refresh()
        Next
    End Sub
End Class
