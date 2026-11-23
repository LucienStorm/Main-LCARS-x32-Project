' LCARSexplorer/Dialogs/frmOpenWith.vb
Option Strict On
Option Explicit On
Imports System.Diagnostics
Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms
''' <summary>LCARS Open With: LCARS apps first, then browse
for any program.</summary>
Public Class frmOpenWith
Inherits Form
Public SelectedExe As String = ""
Private ReadOnly filePath As String
Public Sub New(ByVal targetFile As String) filePath = targetFile Text = "OPEN WITH" FormBorderStyle = FormBorderStyle.None MaximizeBox = False MinimizeBox = False ShowInTaskbar = False StartPosition = FormStartPosition.CenterParent BackColor = Color.Black ForeColor = Color.Orange ClientSize = New Size(420, 460) Font = New Font("LCARS", 14.0F, FontStyle.Regular)
Dim tbTitle As New LCARS.Controls.TextButton()
tbTitle.ButtonText = "OPEN WITH" tbTitle.Text = "OPEN WITH" tbTitle.Clickable = False tbTitle.Location = New Point(8, 8)
tbTitle.Size = New Size(404, 32)
Controls.Add(tbTitle)
Dim lblFile As New Label()
lblFile.ForeColor = Color.FromArgb(200, 140, 40)
lblFile.Font = New Font("LCARS", 11.0F, FontStyle.Regular)
lblFile.Location = New Point(8, 48)
lblFile.Size = New Size(404, 40)
lblFile.Text = Path.GetFileName(filePath)
Controls.Add(lblFile)
Dim lblLcars As New Label()
lblLcars.Text = "LCARS APPLICATIONS" lblLcars.ForeColor = Color.Orange lblLcars.Font = New Font("LCARS", 12.0F, FontStyle.Regular)
lblLcars.Location = New Point(8, 92)
lblLcars.AutoSize = True Controls.Add(lblLcars)
Dim y As Integer = 118
Dim apps As String()() = { New String() {"MEDIA VIEWER", "LCARSmedia.exe"}, New String() {"WEB BROWSER", "LCARSWebBrowser.exe"}, New String() {"FILE BROWSER", "LCARSexplorer.exe"}, New String() {"TERMINAL", "LCARSTerminal.exe"} }
For Each app As String() In apps
Dim exePath As String = Path.Combine(Application.StartupPath, app(1))
Dim btn As New LCARS.Controls.StandardButton()
btn.ButtonText = app(0)
btn.Text = app(0)
btn.Size = New Size(404, 34)
btn.Location = New Point(8, y)
btn.Color = LCARS.LCARScolorStyles.PrimaryFunction btn.Enabled = File.Exists(exePath)
btn.Tag = exePath
If btn.Enabled Then AddHandler btn.Click, AddressOf LcarsApp_Click
Else btn.Color = LCARS.LCARScolorStyles.FunctionOffline
End If Controls.Add(btn) y += 40
Next
Dim lblOther As New Label()
lblOther.Text = "OTHER" lblOther.ForeColor = Color.Orange lblOther.Font = New Font("LCARS", 12.0F, FontStyle.Regular)
lblOther.Location = New Point(8, y + 8)
lblOther.AutoSize = True Controls.Add(lblOther) y += 36
Dim sbBrowse As New LCARS.Controls.StandardButton()
sbBrowse.ButtonText = "BROWSE FOR PROGRAM…" sbBrowse.Text = "BROWSE FOR PROGRAM…" sbBrowse.Color = LCARS.LCARScolorStyles.SystemFunction sbBrowse.Size = New Size(404, 34)
sbBrowse.Location = New Point(8, y)
AddHandler sbBrowse.Click, AddressOf Browse_Click Controls.Add(sbBrowse)
Dim sbCancel As New LCARS.Controls.StandardButton()
sbCancel.ButtonText = "CANCEL" sbCancel.Text = "CANCEL" sbCancel.Color = LCARS.LCARScolorStyles.FunctionOffline sbCancel.Size = New Size(120, 28)
sbCancel.Location = New Point(292, 420)
sbCancel.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
AddHandler sbCancel.Click, Sub() DialogResult = DialogResult.Cancel Close()
End Sub Controls.Add(sbCancel)
End Sub
Private Sub LcarsApp_Click(ByVal sender As Object, ByVal e As EventArgs)
Dim btn As LCARS.Controls.StandardButton = TryCast(sender, LCARS.Controls.StandardButton)
If btn Is Nothing Then Return SelectedExe = TryCast(btn.Tag, String)
If String.IsNullOrEmpty(SelectedExe) Then Return DialogResult = DialogResult.OK Close()
End Sub
Private Sub Browse_Click(ByVal sender As Object, ByVal e As EventArgs)
Dim startDir As String = "C:\Program Files\"
If Not Directory.Exists(startDir) Then startDir = Application.StartupPath
Dim mySelect As New frmFileSelect(startDir, ".exe,.bat,", "Select program executable")
If mySelect.ShowDialog(Me) = DialogResult.OK Then SelectedExe = mySelect.ReturnPath DialogResult = DialogResult.OK Close()
End If
End Sub
Public Shared Function TryLaunch(ByVal exePath As String, ByVal filePath As String) As Boolean
If String.IsNullOrEmpty(exePath) OrElse String.IsNullOrEmpty(filePath) Then Return False
If Not File.Exists(exePath) Then MsgBox("Program not found:" & vbCrLf & exePath, MsgBoxStyle.Exclamation, "OPEN WITH")
Return False
End If
Try
Dim psi As New ProcessStartInfo()
psi.FileName = exePath psi.Arguments = """" & filePath & """" psi.WorkingDirectory = Path.GetDirectoryName(exePath)
Process.Start(psi)
Return True
Catch ex As Exception MsgBox("Could not open:" & vbCrLf & ex.Message, MsgBoxStyle.Exclamation, "OPEN WITH")
Return False
End Try
End Function
End Class
