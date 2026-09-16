Imports Microsoft.Web.WebView2.Core
Imports System.IO
Imports System.Net
Imports Microsoft.Win32

''' <summary>
''' Ensures the Evergreen WebView2 Runtime exists by downloading Microsoft's
''' installer when needed. Call on browser start; also used as
''' LCARSWebBrowser.exe --ensure-webview2 from the shell.
''' </summary>
Public Module WebView2RuntimeInstaller

    Private Const ClientId As String = "{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}"
    Private Const BootstrapperUrl As String = "https://go.microsoft.com/fwlink/p/?LinkId=2124703"
    Private Const RuntimeFolderName As String = "WebView2Runtime"

    Private ensuredThisProcess As Boolean = False
    Private ensureSucceeded As Boolean = False

    ''' <summary>
    ''' Folder of an optional leftover Fixed Runtime next to the exe.
    ''' </summary>
    Public Function GetBundledRuntimeFolder() As String
        Return Path.Combine(Application.StartupPath, RuntimeFolderName)
    End Function

    ''' <summary>
    ''' True when a bundled Fixed Runtime is already on disk.
    ''' </summary>
    Public Function BundledRuntimeAvailable() As Boolean
        Return File.Exists(Path.Combine(GetBundledRuntimeFolder(), "msedgewebview2.exe"))
    End Function

    ''' <summary>
    ''' True when Evergreen WebView2 is installed for this user or machine.
    ''' </summary>
    Public Function EvergreenRuntimeAvailable() As Boolean
        Try
            Dim version As String = CoreWebView2Environment.GetAvailableBrowserVersionString()
            If Not String.IsNullOrEmpty(version) Then Return True
        Catch
        End Try
        Return RegistryRuntimePresent()
    End Function

    ''' <summary>
    ''' Registry fallback used before the WebView2 API can load.
    ''' </summary>
    Private Function RegistryRuntimePresent() As Boolean
        Dim subKeys() As String = { _
            "SOFTWARE\Microsoft\EdgeUpdate\Clients\" & ClientId, _
            "SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\" & ClientId}
        Dim hives() As RegistryKey = {Registry.LocalMachine, Registry.CurrentUser}
        For Each hive As RegistryKey In hives
            For Each subKey As String In subKeys
                Try
                    Dim key As RegistryKey = hive.OpenSubKey(subKey)
                    If key IsNot Nothing Then
                        Dim pv As Object = key.GetValue("pv")
                        If pv IsNot Nothing Then
                            Dim version As String = pv.ToString()
                            If version.Length > 0 AndAlso version <> "0.0.0.0" Then
                                Return True
                            End If
                        End If
                    End If
                Catch
                End Try
            Next
        Next
        Return False
    End Function

    ''' <summary>
    ''' Returns true when any usable runtime is available after an automatic install attempt.
    ''' </summary>
    Public Function EnsureInstalled() As Boolean
        If ensuredThisProcess Then Return ensureSucceeded
        If EvergreenRuntimeAvailable() OrElse BundledRuntimeAvailable() Then
            ensuredThisProcess = True
            ensureSucceeded = True
            Return True
        End If
        ensureSucceeded = DownloadAndInstall()
        ensuredThisProcess = True
        Return ensureSucceeded
    End Function

    ''' <summary>
    ''' Downloads the Evergreen bootstrapper and runs a silent install.
    ''' </summary>
    Private Function DownloadAndInstall() As Boolean
        Dim status As Form = CreateStatusForm("Downloading WebView2 Runtime from Microsoft." & vbCrLf & "This can take a few minutes.")
        status.Show()
        status.Refresh()
        Application.DoEvents()

        Dim setupPath As String = Path.Combine(Path.GetTempPath(), "MicrosoftEdgeWebview2Setup.exe")
        Try
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
            Using client As New WebClient()
                client.DownloadFile(BootstrapperUrl, setupPath)
            End Using

            SetStatus(status, "Installing WebView2 Runtime. Windows may ask for permission.")
            If Not RunInstaller(setupPath, False) Then
                RunInstaller(setupPath, True)
            End If
            Return EvergreenRuntimeAvailable()
        Catch ex As Exception
            MessageBox.Show( _
                "Could not download or install WebView2 Runtime from Microsoft." & vbCrLf & ex.Message, _
                "LCARS Web Browser", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            Return False
        Finally
            Try
                If File.Exists(setupPath) Then
                    File.Delete(setupPath)
                End If
            Catch
            End Try
            status.Close()
            status.Dispose()
        End Try
    End Function

    ''' <summary>
    ''' Runs the bootstrapper silently. elevate=True shows UAC.
    ''' </summary>
    Private Function RunInstaller(ByVal setupPath As String, ByVal elevate As Boolean) As Boolean
        Dim psi As New ProcessStartInfo()
        psi.FileName = setupPath
        psi.Arguments = "/silent /install"
        psi.UseShellExecute = True
        If elevate Then
            psi.Verb = "runas"
        End If
        Try
            Dim proc As Process = Process.Start(psi)
            If proc Is Nothing Then Return False
            proc.WaitForExit()
        Catch
            Return False
        End Try
        Return EvergreenRuntimeAvailable()
    End Function

    ''' <summary>
    ''' Builds the blocking status window shown during download/install.
    ''' </summary>
    Private Function CreateStatusForm(ByVal message As String) As Form
        Dim status As New Form()
        status.Text = "LCARS Web Browser"
        status.FormBorderStyle = FormBorderStyle.FixedDialog
        status.StartPosition = FormStartPosition.CenterScreen
        status.ControlBox = False
        status.TopMost = True
        status.ShowInTaskbar = False
        status.Width = 520
        status.Height = 140
        status.BackColor = Color.Black
        Dim lbl As New Label()
        lbl.Name = "lblStatus"
        lbl.ForeColor = Color.FromArgb(255, 153, 0)
        lbl.Dock = DockStyle.Fill
        lbl.TextAlign = ContentAlignment.MiddleCenter
        lbl.Font = New Font("Microsoft Sans Serif", 12.0F, FontStyle.Regular)
        lbl.Text = message
        status.Controls.Add(lbl)
        Return status
    End Function

    ''' <summary>
    ''' Updates the status form text.
    ''' </summary>
    Private Sub SetStatus(ByVal status As Form, ByVal message As String)
        Dim lbl As Label = TryCast(status.Controls("lblStatus"), Label)
        If lbl IsNot Nothing Then
            lbl.Text = message
        End If
        status.Refresh()
        Application.DoEvents()
    End Sub

End Module
