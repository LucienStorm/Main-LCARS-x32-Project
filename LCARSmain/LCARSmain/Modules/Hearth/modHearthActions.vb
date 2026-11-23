' LCARSmain/LCARSmain/Modules/Hearth/modHearthActions.vb
Option Strict On
Option Explicit On
Imports System.Diagnostics
Imports System.Windows.Forms
''' <summary>Phase 5 action catalog — shell executes; hub only proposes advertised names.</summary> Module modHearthActions
Private pendingName As String = Nothing
Private pendingArgsJson As String = Nothing
Public Function KnownAction(ByVal name As String) As Boolean
If String.IsNullOrEmpty(name) Then Return False
Select Case name.Trim().ToLowerInvariant()
Case "open_task_manager", "open_engineering", "open_quick", "open_app", "deactivate_shell"
Return True
Case
Else Return False
End Select
End Function
Public Sub HandleAction(ByVal name As String, ByVal argsJson As String, ByVal confirm As Boolean)
If String.IsNullOrEmpty(name) Then Return
If Not KnownAction(name) Then modDiagnostics.LogWarn("HearthActions", "unknown action: " & name) ShowNotice("I don't have that control yet.")
Return
End If
If confirm Then pendingName = name pendingArgsJson = argsJson ShowConfirmUi(name)
Return
End If ExecuteNow(name, argsJson)
End Sub
Public Sub ConfirmPending()
If String.IsNullOrEmpty(pendingName) Then CloseConfirmForms(DialogResult.Yes)
Return
End If
Dim n As String = pendingName
Dim a As String = pendingArgsJson pendingName = Nothing pendingArgsJson = Nothing CloseConfirmForms(DialogResult.Yes) ExecuteNow(n, a)
End Sub
Public Sub CancelPending() pendingName = Nothing pendingArgsJson = Nothing CloseConfirmForms(DialogResult.No)
End Sub
Private Sub CloseConfirmForms(ByVal result As DialogResult)
Try
For Each f As Form In Application.OpenForms
Dim hc As frmHearthConfirm = TryCast(f, frmHearthConfirm)
If hc IsNot Nothing AndAlso Not hc.IsDisposed Then hc.DialogResult = result hc.Close()
End If
Next
Catch
End Try
End Sub
Public ReadOnly Property HasPending As Boolean Get
Return Not String.IsNullOrEmpty(pendingName)
End Get
End Property
Private Sub ShowConfirmUi(ByVal name As String)
Try
If myDesktop IsNot Nothing AndAlso myDesktop.InvokeRequired Then myDesktop.BeginInvoke(New MethodInvoker(Sub() ShowConfirmUi(name)))
Return
End If LCARSSound.ConfirmSound.Play()
Dim f As New frmHearthConfirm(name)
AddHandler f.FormClosed, Sub(s As Object, e As FormClosedEventArgs)
Dim frm As frmHearthConfirm = TryCast(s, frmHearthConfirm)
If frm Is Nothing Then Return ' Voice path may have already cleared pending via ConfirmPending/CancelPending.
If String.IsNullOrEmpty(pendingName) Then Return
If frm.DialogResult = DialogResult.Yes Then ConfirmPending()
Else CancelPending()
End If
End Sub f.Show(myDesktop)
Catch ex As Exception modDiagnostics.LogException("HearthActions.ShowConfirmUi", ex, name) CancelPending()
End Try
End Sub
Private Sub ExecuteNow(ByVal name As String, ByVal argsJson As String)
Try
If myDesktop IsNot Nothing AndAlso myDesktop.InvokeRequired Then myDesktop.BeginInvoke(New MethodInvoker(Sub() ExecuteNow(name, argsJson)))
Return
End If
Select Case name.Trim().ToLowerInvariant()
Case "open_task_manager" modResourceHud.LaunchTaskManager()
Case "open_engineering"
If curBusiness IsNot Nothing AndAlso curBusiness.Count > 0 AndAlso curBusiness(0) IsNot Nothing Then curBusiness(0).myEngineeringButton_Click(Nothing, Nothing)
End If
Case "open_quick"
If curBusiness IsNot Nothing AndAlso curBusiness.Count > 0 AndAlso curBusiness(0) IsNot Nothing Then Dim qc As New frmQuickControls()
qc.Show(curBusiness(0).myForm)
End If
Case "open_app"
Dim target As String = modHearthJson.GetString(If(argsJson, "{}"), "target") LaunchAllowlistedApp(target)
Case "deactivate_shell" doDeactivate(Nothing)
End Select
Catch ex As Exception modDiagnostics.LogException("HearthActions.ExecuteNow", ex, name)
End Try
End Sub
Private Sub LaunchAllowlistedApp(ByVal target As String)
If String.IsNullOrEmpty(target) Then ShowNotice("No app target.")
Return
End If
Dim t As String = target.Trim().ToLowerInvariant()
Dim path As String = Nothing
Select Case t
Case "browser", "web", "webview"
If curBusiness IsNot Nothing AndAlso curBusiness.Count > 0 Then curBusiness(0).myWebBrowser_Click(Nothing, Nothing)
Return
Case "explorer", "files" path = System.IO.Path.Combine(Application.StartupPath, "LCARSexplorer.exe")
Case "terminal", "remote" path = System.IO.Path.Combine(Application.StartupPath, "LCARSTerminal.exe")
Case "media" path = System.IO.Path.Combine(Application.StartupPath, "LCARSmedia.exe")
Case "keyboard", "osk"
If curBusiness IsNot Nothing AndAlso curBusiness.Count > 0 Then curBusiness(0).myOSK_Click(Nothing, Nothing)
Return
Case
Else ShowNotice("App not allowlisted: " & target)
Return
End Select
If path IsNot Nothing AndAlso System.IO.File.Exists(path) Then Process.Start(path)
Else ShowNotice("App missing: " & target)
End If
End Sub
Private Sub ShowNotice(ByVal msg As String)
Try LCARS.UI.MsgBox(msg, MsgBoxStyle.OkOnly, "HEARTH")
Catch
End Try
End Sub
End Module
