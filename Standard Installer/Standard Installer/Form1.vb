Imports System.Runtime.InteropServices

Public Class Form1
    Dim installing As Integer = 0
    Dim installThread As System.Threading.Thread
    Dim installPath As String = ""
    Private Event StatusChanged(ByVal message As String, ByVal Progress As Decimal)
    Private Event InstallFinished()
    Private Delegate Sub SetProgress(ByVal CurrentProgress As Decimal)
    Private Delegate Sub AddItem(ByVal NewItem As Object)
    Private Delegate Sub NoArgs()
    Private Delegate Sub ShowErrorHandler(ByVal detail As String)

#Region " API calls "
    <DllImport("gdi32")> _
    Public Shared Function AddFontResource(ByVal lpFileName As String) As Integer
    End Function

    <DllImport("user32.dll")> _
    Public Shared Function SendMessage(ByVal hWnd As Integer, ByVal Msg As UInteger, ByVal wParam As Integer, ByVal lParam As Integer) As Integer
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)> _
    Shared Function WriteProfileString(ByVal lpszSection As String, ByVal lpszKeyName As String, ByVal lpszString As String) As Integer
    End Function
#End Region

    Private Sub Form1_FormClosing(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing
        If installing = 0 Then
            Dim result As DialogResult = MsgBox("Are you sure you wish to cancel the installation?", MsgBoxStyle.YesNoCancel)
            If Not result = System.Windows.Forms.DialogResult.Yes Then
                e.Cancel = True
            End If
        ElseIf installing = 2 Then
            ' Finished — allow close
        Else
            e.Cancel = True
        End If
    End Sub

    Private Sub Form1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        txtInstallPath.Text = My.Computer.FileSystem.SpecialDirectories.ProgramFiles & "\LCARS x32"
        progress.Minimum = 0
        progress.Maximum = 100
        progress.Value = 0
    End Sub

    Private Sub btnCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCancel.Click
        Me.Close()
    End Sub

    Private Sub btnBrowse_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnBrowse.Click
        Dim browse As New FolderBrowserDialog()
        If browse.ShowDialog() = System.Windows.Forms.DialogResult.OK Then
            txtInstallPath.Text = browse.SelectedPath
        End If
    End Sub

    Private Sub btnNext_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnNext.Click
        installPath = txtInstallPath.Text.Trim()
        If String.IsNullOrEmpty(installPath) Then
            MsgBox("Choose an install folder first.")
            Return
        End If

        Dim payloadPath As String = System.IO.Path.Combine(System.Windows.Forms.Application.StartupPath, "CurrentVersion.zip")
        If Not System.IO.File.Exists(payloadPath) Then
            MsgBox("CurrentVersion.zip was not found next to the installer." & vbNewLine & vbNewLine & _
                   "Copy the whole LCARS install folder (installer + CurrentVersion.zip + DLLs), then run again.")
            Return
        End If

        installing = 1
        btnNext.Enabled = False
        btnBrowse.Enabled = False
        btnCancel.Enabled = False
        txtInstallPath.Enabled = False
        pnlInstalling.Visible = True
        lstInstalling.Items.Clear()
        progress.Value = 0

        installThread = New Threading.Thread(AddressOf installThreadSub)
        installThread.IsBackground = True
        installThread.Start()
    End Sub

    Private Sub installThreadSub()
        Try
            RaiseEvent StatusChanged("Extracting files. This may take a few moments.", 0.1D)
            If Not System.IO.Directory.Exists(installPath) Then
                System.IO.Directory.CreateDirectory(installPath)
            End If

            Dim payloadPath As String = System.IO.Path.Combine(System.Windows.Forms.Application.StartupPath, "CurrentVersion.zip")
            Using zip As Ionic.Zip.ZipFile = Ionic.Zip.ZipFile.Read(payloadPath)
                zip.ExtractAll(installPath, Ionic.Zip.ExtractExistingFileAction.OverwriteSilently)
            End Using

            RaiseEvent StatusChanged("Installing LCARS font.", 0.7D)
            TryInstallFont()

            RaiseEvent StatusChanged("Creating shortcut.", 0.9D)
            TryCreateDesktopShortcut()

            RaiseEvent StatusChanged("Installation complete.", 1D)
            RaiseEvent InstallFinished()
        Catch ex As Exception
            WriteInstallLog(ex.ToString())
            ShowInstallError(ex.ToString())
        End Try
    End Sub

    Private Sub TryInstallFont()
        Try
            Dim fontSource As String = System.IO.Path.Combine(installPath, "lcars.ttf")
            If Not System.IO.File.Exists(fontSource) Then
                Return
            End If
            Dim fontsDir As String = System.IO.Path.Combine( _
                Environment.GetEnvironmentVariable("WINDIR"), "Fonts")
            Dim fontDest As String = System.IO.Path.Combine(fontsDir, "lcars.ttf")
            System.IO.File.Copy(fontSource, fontDest, True)
            Const WM_FONTCHANGE As Integer = &H1D
            Const HWND_BROADCAST As Integer = &HFFFF
            AddFontResource(fontDest)
            SendMessage(HWND_BROADCAST, WM_FONTCHANGE, 0, 0)
            WriteProfileString("fonts", "LCARS (TrueType)", "lcars.ttf")
        Catch
            ' Font install is best-effort; LCARS still runs with a fallback face.
        End Try
    End Sub

    ''' <summary>
    ''' Creates a desktop shortcut via WScript.Shell late binding (no Interop.IWshRuntimeLibrary).
    ''' </summary>
    Private Sub TryCreateDesktopShortcut()
        Try
            Dim shortcutPath As String = My.Computer.FileSystem.SpecialDirectories.Desktop & "\LCARS x32.lnk"
            Dim target As String = System.IO.Path.Combine(installPath, "LCARSmain.exe")
            Dim shell As Object = CreateObject("WScript.Shell")
            Dim sc As Object = shell.CreateShortcut(shortcutPath)
            sc.TargetPath = target
            sc.WorkingDirectory = installPath
            sc.WindowStyle = 1
            sc.Description = "LCARS x32"
            sc.IconLocation = target & ", 0"
            sc.Save()
        Catch ex As System.Exception
            WriteInstallLog("Shortcut failed (install continues): " & ex.ToString())
            RaiseEvent StatusChanged("Shortcut skipped (you can launch LCARSmain.exe directly).", 0.95D)
        End Try
    End Sub

    Private Sub Me_StatusChanged(ByVal message As String, ByVal CurrentProgress As Decimal) Handles Me.StatusChanged
        Try
            BeginInvoke(New SetProgress(AddressOf ChangeProgress), CurrentProgress * 100D)
            BeginInvoke(New AddItem(AddressOf AddItemToListbox), message)
        Catch
        End Try
    End Sub

    Private Sub FinishInstall() Handles Me.InstallFinished
        If Me.InvokeRequired Then
            Me.BeginInvoke(New NoArgs(AddressOf FinishInstall))
            Return
        End If
        installing = 2
        Dim installedVersion As String = "unknown"
        Try
            Using myreader As New System.IO.StreamReader(System.IO.Path.Combine(installPath, "versions.txt"))
                installedVersion = myreader.ReadLine()
            End Using
        Catch
        End Try
        MsgBox("LCARS x32 has finished installing." & vbNewLine & _
               "Version: " & installedVersion & vbNewLine & vbNewLine & _
               "Installed to:" & vbNewLine & installPath)
        End
    End Sub

    Private Sub ChangeProgress(ByVal CurrentProgress As Decimal)
        Dim value As Integer = CInt(Math.Max(0, Math.Min(100, CurrentProgress)))
        progress.Value = value
    End Sub

    Private Sub AddItemToListbox(ByVal item As String)
        lstInstalling.Items.Add(item)
    End Sub

    Private Sub ShowInstallError(ByVal detail As String)
        If Me.InvokeRequired Then
            Me.BeginInvoke(New ShowErrorHandler(AddressOf ShowInstallError), detail)
            Return
        End If
        installing = 0
        btnNext.Enabled = True
        btnBrowse.Enabled = True
        btnCancel.Enabled = True
        txtInstallPath.Enabled = True
        Dim logPath As String = GetInstallLogPath()
        MsgBox("Installation failed." & vbNewLine & vbNewLine & detail & vbNewLine & vbNewLine & _
               "Log file:" & vbNewLine & logPath)
    End Sub

    Private Function GetInstallLogPath() As String
        Return System.IO.Path.Combine(My.Computer.FileSystem.SpecialDirectories.Temp, "lcars-install-error.txt")
    End Function

    Private Sub WriteInstallLog(ByVal text As String)
        Try
            System.IO.File.AppendAllText(GetInstallLogPath(), DateTime.Now.ToString("u") & vbNewLine & text & vbNewLine & vbNewLine)
        Catch
        End Try
    End Sub
End Class
