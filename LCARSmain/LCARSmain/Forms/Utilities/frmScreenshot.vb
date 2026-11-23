' LCARSmain/LCARSmain/Forms/Utilities/frmScreenshot.vb
Option Strict On
Option Explicit On
Imports System.Diagnostics
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.IO
Imports System.Windows.Forms
''' <summary>Captures the current screen to Pictures\LCARS as a PNG and previews it.</summary>
Public Class frmScreenshot
Inherits LCARS.LCARSForm
Private Const FormWidth As Integer = 520
Private Const FormHeight As Integer = 420
Private Const ShotFolderName As String = "LCARS"
Private Const ShotFileFormat As String = "'lcars-shot-'yyyyMMdd-HHmmss'.png'"
Private Const HideBeforeCaptureMs As Integer = 250
Private picPreview As PictureBox
Private lblStatus As Label
Private lastSavedPath As String = ""
Public Sub New()
MyBase.New()
modUtilities.InitUtilityForm(Me, FormWidth, FormHeight) BuildUi()
End Sub
Private Sub BuildUi()
Dim margin As Integer = modUtilities.UtilityMargin
Dim contentWidth As Integer = FormWidth - margin * 2 modUtilities.MakeUtilityTitle(Me, "SCREENSHOT", contentWidth) picPreview = New PictureBox()
picPreview.BackColor = Color.Black picPreview.BorderStyle = BorderStyle.FixedSingle picPreview.SizeMode = PictureBoxSizeMode.Zoom picPreview.Location = New Point(margin, 48)
picPreview.Size = New Size(contentWidth, 240)
Me.Controls.Add(picPreview) lblStatus = modUtilities.MakeUtilityLabel(Me, margin, 296, contentWidth, 28, 14.0F)
lblStatus.Text = "READY"
Dim thirdWidth As Integer = (contentWidth - 6 * 2) \ 3 modUtilities.MakeUtilityButton(Me, "CAPTURE SCREEN", margin, 336, thirdWidth, 40, LCARS.LCARScolorStyles.PrimaryFunction, Sub(s As Object, e As EventArgs) CaptureScreen())
modUtilities.MakeUtilityButton(Me, "OPEN FOLDER", margin + thirdWidth + 6, 336, thirdWidth, 40, LCARS.LCARScolorStyles.NavigationFunction, Sub(s As Object, e As EventArgs) OpenFolder())
modUtilities.MakeUtilityButton(Me, "CLOSE", margin + (thirdWidth + 6) * 2, 336, thirdWidth, 40, LCARS.LCARScolorStyles.FunctionOffline, Sub(s As Object, e As EventArgs) Close())
End Sub
Private Shared Function ShotFolder() As String
Return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), ShotFolderName)
End Function
''' <summary>Hides this panel, copies the screen it was on, saves a PNG, and shows the preview.</summary>
Private Sub CaptureScreen()
Dim bounds As Rectangle = Screen.FromControl(Me).Bounds
Dim shot As New Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb)
Try Me.Opacity = 0 Application.DoEvents()
System.Threading.Thread.Sleep(HideBeforeCaptureMs)
Using g As Graphics = Graphics.FromImage(shot)
g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size)
End Using
Catch ex As Exception shot.Dispose()
lblStatus.Text = "CAPTURE FAILED"
Return
Finally Me.Opacity = 1
End Try
Try Directory.CreateDirectory(ShotFolder())
Dim savePath As String = Path.Combine(ShotFolder(), DateTime.Now.ToString(ShotFileFormat))
shot.Save(savePath, ImageFormat.Png) lastSavedPath = savePath lblStatus.Text = "SAVED " & Path.GetFileName(savePath)
Catch ex As Exception lblStatus.Text = "SAVE FAILED"
End Try
Dim previous As Image = picPreview.Image picPreview.Image = shot
If previous IsNot Nothing Then previous.Dispose()
End Sub
Private Sub OpenFolder()
Try Directory.CreateDirectory(ShotFolder())
If lastSavedPath <> "" AndAlso File.Exists(lastSavedPath) Then Process.Start("explorer.exe", "/select,""" & lastSavedPath & """")
Else Process.Start("explorer.exe", """" & ShotFolder() & """")
End If
Catch ex As Exception lblStatus.Text = "CANNOT OPEN FOLDER"
End Try
End Sub
Protected Overrides Sub OnFormClosed(ByVal e As FormClosedEventArgs)
If picPreview.Image IsNot Nothing Then picPreview.Image.Dispose()
MyBase.OnFormClosed(e)
End Sub
End Class
