' LCARSpic/My Project/ApplicationEvents.vb
Option Strict On
Option Explicit On

Imports Microsoft.VisualBasic.ApplicationServices

Namespace My
    Partial Friend Class MyApplication
        ''' <summary>Second launch hands the file path to the running media window.</summary>
        Private Sub MyApplication_StartupNextInstance(ByVal sender As Object, ByVal e As StartupNextInstanceEventArgs) Handles Me.StartupNextInstance
            e.BringToForeground = True
            Dim f As frmPic = TryCast(MainForm, frmPic)
            If f Is Nothing Then Return
            Try
                If e.CommandLine IsNot Nothing AndAlso e.CommandLine.Count > 0 Then
                    Dim arg As String = e.CommandLine(0)
                    If Not String.IsNullOrEmpty(arg) Then
                        f.BeginInvoke(New Action(Of String)(AddressOf f.LoadMedia), arg)
                    End If
                End If
                f.BeginInvoke(New MethodInvoker(AddressOf f.ActivateFromShell))
            Catch
            End Try
        End Sub
    End Class
End Namespace
