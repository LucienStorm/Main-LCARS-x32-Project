Namespace My
    Partial Friend Class MyApplication
        ''' <summary>
        ''' When launched as --ensure-webview2, install the runtime and exit without UI.
        ''' </summary>
        Private Sub MyApplication_Startup(ByVal sender As Object, ByVal e As Microsoft.VisualBasic.ApplicationServices.StartupEventArgs) Handles Me.Startup
            For Each arg As String In e.CommandLine
                If String.Equals(arg, "--ensure-webview2", StringComparison.OrdinalIgnoreCase) Then
                    WebView2RuntimeInstaller.EnsureInstalled()
                    e.Cancel = True
                    Return
                End If
            Next
        End Sub
    End Class
End Namespace
