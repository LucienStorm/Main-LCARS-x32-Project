Option Strict On

Imports System.IO
Imports System.Net
Imports Microsoft.Win32

''' <summary>
''' Ensures the Evergreen WebView2 Runtime is present. On first LCARS boot
''' (and any later boot while it is missing) this downloads Microsoft's
''' bootstrapper and installs it. No manual copy of runtime files.
''' </summary>
Public Module modWebView2Runtime

    Private Const ClientId As String = "{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}"
    Private Const BootstrapperUrl As String = "https://go.microsoft.com/fwlink/p/?LinkId=2124703"
    Private Const Tls12 As Integer = 3072

    Private ensuredThisProcess As Boolean = False

    ''' <summary>
    ''' True when a usable Evergreen (or per-user) WebView2 Runtime is registered.
    ''' </summary>
    Public Function IsWebView2RuntimeInstalled() As Boolean
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
    ''' Installs WebView2 from the internet if it is not already present.
    ''' Safe to call on every LCARS start; no-ops when already installed.
    ''' </summary>
    Public Sub EnsureWebView2Runtime()
        If ensuredThisProcess Then Return
        ensuredThisProcess = True
        If IsWebView2RuntimeInstalled() Then Return
        InstallEvergreenRuntime()
    End Sub

    ''' <summary>
    ''' Downloads the Evergreen bootstrapper and runs a silent install.
    ''' </summary>
    Private Sub InstallEvergreenRuntime()
        Dim status As Form = CreateStatusForm("Downloading WebView2 Runtime from Microsoft." & vbCrLf & "This can take a few minutes.")
        status.Show()
        status.Refresh()
        Application.DoEvents()

        Dim setupPath As String = Path.Combine(Path.GetTempPath(), "MicrosoftEdgeWebview2Setup.exe")
        Try
            EnableTls12()
            Using client As New WebClient()
                client.DownloadFile(BootstrapperUrl, setupPath)
            End Using

            SetStatus(status, "Installing WebView2 Runtime. Windows may ask for permission.")
            If Not RunInstaller(setupPath, False) Then
                RunInstaller(setupPath, True)
            End If

            If Not IsWebView2RuntimeInstalled() Then
                MessageBox.Show( _
                    "WebView2 Runtime could not be installed automatically." & vbCrLf & _
                    "LCARS needs an internet connection and permission to install Microsoft's WebView2 Runtime.", _
                    "LCARS x32", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            End If
        Catch ex As Exception
            MessageBox.Show( _
                "Could not download or install WebView2 Runtime from Microsoft." & vbCrLf & ex.Message, _
                "LCARS x32", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
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
    End Sub

    ''' <summary>
    ''' Runs the bootstrapper silently. elevate=True shows UAC for a machine-wide install.
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
        Return IsWebView2RuntimeInstalled()
    End Function

    ''' <summary>
    ''' Enables TLS 1.2 so nuget.org / Microsoft downloads work on .NET 3.5.
    ''' </summary>
    Private Sub EnableTls12()
        Try
            ServicePointManager.SecurityProtocol = DirectCast(Tls12, SecurityProtocolType)
        Catch
        End Try
    End Sub

    ''' <summary>
    ''' Builds the blocking status window shown during download/install.
    ''' </summary>
    Private Function CreateStatusForm(ByVal message As String) As Form
        Dim status As New Form()
        status.Text = "LCARS x32"
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
