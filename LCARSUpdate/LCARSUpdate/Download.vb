Imports LCARS.UI
<System.ComponentModel.ToolboxItem(False)> _
Friend Class Download
    Inherits LCARS.Controls.ProgressBar

    Public Event SizeAcquired(ByVal size As Long)
    Public Event StatusChanged(ByVal currentDownloaded As Long)
    Public Event DownloadComplete()
    Public Event DownloadFailed()
    Public Event UpdateFailed(ByVal sender As Object)

    Private downloadPath As String
    Private savePath As String
    Private md5 As String
    Private downloadSize As Long = 1
    Private _retryCount As Integer = 0
    Private downloadThread As System.Threading.Thread

    Private Delegate Sub SingleLongArg(ByVal status As Long)

    Public Property RetryCount() As Integer
        Get
            Return _retryCount
        End Get
        Set(ByVal value As Integer)
            _retryCount = value
        End Set
    End Property


    Public Sub New(ByVal path As String, ByVal file As String, ByVal md5Hash As String)
        downloadPath = path
        savePath = file
        md5 = md5Hash
    End Sub

    Public Sub StartDownload()
        downloadThread = New System.Threading.Thread(AddressOf DownloadSub)
        downloadThread.Start()
    End Sub
    Public Sub DownloadSub()
        Try
            If System.IO.File.Exists(savePath) Then
                System.IO.File.Delete(savePath)
            End If

            SetBottomTextSafe("Downloading")
            Dim client As New System.Net.WebClient()
            WebRequestHelper.ConfigureWebClient(client, downloadPath)
            client.DownloadFile(downloadPath, savePath)

            Dim fileInfo As New System.IO.FileInfo(savePath)
            RaiseEvent SizeAcquired(fileInfo.Length)
            RaiseEvent StatusChanged(fileInfo.Length)

            SetBottomTextSafe("Verifying")
            Dim hashArray As Byte()
            Dim myBuilder As New System.Text.StringBuilder
            Dim hashByte As Byte
            Dim myMD5 As New System.Security.Cryptography.MD5CryptoServiceProvider()
            Using myStream As New System.IO.FileStream(savePath, IO.FileMode.Open, IO.FileAccess.Read)
                hashArray = myMD5.ComputeHash(myStream)
            End Using
            For Each hashByte In hashArray
                myBuilder.Append(String.Format("{0:x2}", hashByte))
            Next
            If String.Equals(md5, myBuilder.ToString(), StringComparison.OrdinalIgnoreCase) Then
                RaiseEvent DownloadComplete()
            Else
                SetBottomTextSafe("Hash mismatch")
                RaiseEvent DownloadFailed()
                ShowMessageSafe("MD5 mismatch for " & System.IO.Path.GetFileName(savePath) & vbNewLine & _
                       "Expected: " & md5 & vbNewLine & _
                       "Got:      " & myBuilder.ToString())
            End If

        Catch ex As Exception
            SetBottomTextSafe("Failed")
            RaiseEvent DownloadFailed()
            Dim detail As String = "Download failed for:" & vbNewLine & downloadPath & vbNewLine & vbNewLine & ex.ToString()
            WriteCrashLog(detail)
            ShowMessageSafe(detail)
        End Try
    End Sub

    Private Sub SetBottomTextSafe(ByVal text As String)
        If Me.InvokeRequired Then
            Me.BeginInvoke(New StringHandler(AddressOf SetBottomTextSafe), text)
            Return
        End If
        Me.BottomText = text
    End Sub

    Private Delegate Sub StringHandler(ByVal text As String)

    Private Sub ShowMessageSafe(ByVal text As String)
        If Me.InvokeRequired Then
            Me.BeginInvoke(New StringHandler(AddressOf ShowMessageSafe), text)
            Return
        End If
        MsgBox(text)
    End Sub

    Private Sub WriteCrashLog(ByVal text As String)
        Try
            Dim logPath As String = My.Computer.FileSystem.SpecialDirectories.Temp & "\lcars-update-error.txt"
            System.IO.File.AppendAllText(logPath, DateTime.Now.ToString("u") & vbNewLine & text & vbNewLine & vbNewLine)
        Catch
        End Try
    End Sub

    Public Sub Me_DownloadComplete() Handles Me.DownloadComplete
        If Me.InvokeRequired Then
            Me.BeginInvoke(New MethodInvoker(AddressOf MarkDownloadCompleteUi))
            Return
        End If
        MarkDownloadCompleteUi()
    End Sub

    Private Sub MarkDownloadCompleteUi()
        Me.BottomText = "Completed"
        Me.Value = 1
    End Sub

    Public Sub Me_ProgressChanged(ByVal currentProgress As Long) Handles Me.StatusChanged
        If Me.InvokeRequired() Then
            Me.BeginInvoke(New SingleLongArg(AddressOf Me_ProgressChanged), currentProgress)
            Return
        End If
        Dim percent As Decimal = currentProgress / downloadSize
        Me.Value = percent
        Me.BottomText = (percent * 100).ToString("F") & "% of " & downloadSize & " bytes."
    End Sub

    Public Sub Me_SizeAcquired(ByVal size As Long) Handles Me.SizeAcquired
        Me.downloadSize = size
    End Sub

    Public Sub Me_Failed() Handles Me.DownloadFailed
        If Me.InvokeRequired Then
            Me.BeginInvoke(New MethodInvoker(AddressOf Me_FailedUi))
            Return
        End If
        Me_FailedUi()
    End Sub

    Private Sub Me_FailedUi()
        If Me.RetryCount < CType(GetSetting("LCARSUpdate", "Config", "MaxRetry", "3"), Integer) Then
            RetryCount += 1
            Me.downloadSize = 0
            Me.Value = 0
            Me.BottomText = "Retrying"
            StartDownload()
        Else
            Dim result As MsgBoxResult = MsgBox("Maximum retry limit reached for component: " & System.IO.Path.GetFileName(savePath) & " !" & vbNewLine & "Do you wish to attempt to download again?", MsgBoxStyle.YesNo)
            If result = MsgBoxResult.Yes Then
                Me.downloadSize = 0
                Me.Value = 0
                Me.BottomText = "Retrying"
                StartDownload()
            Else
                RaiseEvent UpdateFailed(Me)
            End If
        End If
    End Sub

End Class