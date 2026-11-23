Imports System
Imports System.Diagnostics
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.IO
Imports System.Threading
Imports System.Windows.Forms
Imports LCARS
Imports Microsoft.VisualBasic.CompilerServices

    Public Class frmScreenshot
        Inherits LCARSForm
        Private Const FormWidth As Integer = 520

        Private Const FormHeight As Integer = 420

        Private Const ShotFolderName As String = "LCARS"

        Private Const ShotFileFormat As String = "'lcars-shot-'yyyyMMdd-HHmmss'.png'"

        Private Const HideBeforeCaptureMs As Integer = 250

        Private picPreview As PictureBox

        Private lblStatus As Label

        Private lastSavedPath As String

        Public Sub New()
            lastSavedPath = ""
            modUtilities.InitUtilityForm(Me, 520, 420)
            BuildUi()
        End Sub

        Private Sub BuildUi()
            Dim num = 12
            Dim num2 = 520 - num * 2
            modUtilities.MakeUtilityTitle(Me, "SCREENSHOT", num2)
            picPreview = New PictureBox()
            picPreview.BackColor = Color.Black
            picPreview.BorderStyle = BorderStyle.FixedSingle
            picPreview.SizeMode = PictureBoxSizeMode.Zoom
            Dim pictureBox = picPreview
            Dim location As Point = New Point(num, 48)
            pictureBox.Location = location
            Dim pictureBox2 = picPreview
            Dim size As Size = New Size(num2, 240)
            pictureBox2.Size = size
            Controls.Add(picPreview)
            lblStatus = modUtilities.MakeUtilityLabel(Me, num, 296, num2, 28, 14F)
            lblStatus.Text = "READY"
            Dim num3 As Integer = num2 - 12 / 3
            modUtilities.MakeUtilityButton(Me, "CAPTURE SCREEN", num, 336, num3, 40, LCARScolorStyles.PrimaryFunction, Sub(s, e) CaptureScreen())
            modUtilities.MakeUtilityButton(Me, "OPEN FOLDER", num + num3 + 6, 336, num3, 40, LCARScolorStyles.NavigationFunction, Sub(s, e) OpenFolder())
            modUtilities.MakeUtilityButton(Me, "CLOSE", num + (num3 + 6) * 2, 336, num3, 40, LCARScolorStyles.FunctionOffline, Sub(s, e) Close())
        End Sub

        Private Shared Function ShotFolder() As String
            Return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "LCARS")
        End Function

        Private Sub CaptureScreen()
            Dim bounds = Screen.FromControl(Me).Bounds
            Dim bitmap As Bitmap = New Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb)
            Try
                Opacity = 0.0
                Call Application.DoEvents()
                Thread.Sleep(250)
                Dim graphics = Graphics.FromImage(bitmap)
                graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size)
            Catch ex As Exception
                ProjectData.SetProjectError(ex)
                Dim ex2 = ex
                bitmap.Dispose()
                lblStatus.Text = "CAPTURE FAILED"
                Call ProjectData.ClearProjectError()
                Return
            Finally
                Opacity = 1.0
            End Try
            Try
                Call Directory.CreateDirectory(ShotFolder())
                Dim text As String = Path.Combine(ShotFolder(), Date.Now.ToString("'lcars-shot-'yyyyMMdd-HHmmss'.png'"))
                bitmap.Save(text, ImageFormat.Png)
                lastSavedPath = text
                lblStatus.Text = "SAVED " & Path.GetFileName(text)
            Catch ex3 As Exception
                ProjectData.SetProjectError(ex3)
                Dim ex4 = ex3
                lblStatus.Text = "SAVE FAILED"
                Call ProjectData.ClearProjectError()
            End Try
            Dim image = picPreview.Image
            picPreview.Image = bitmap
            image?.Dispose()
        End Sub

        Private Sub OpenFolder()
            Try
                Call Directory.CreateDirectory(ShotFolder())
                If Operators.CompareString(lastSavedPath, "", TextCompare:=False) <> 0 AndAlso File.Exists(lastSavedPath) Then
                    Process.Start("explorer.exe", "/select,""" & lastSavedPath & """")
                Else
                    Call Process.Start("explorer.exe", """" & ShotFolder() & """")
                End If
            Catch ex As Exception
                ProjectData.SetProjectError(ex)
                Dim ex2 = ex
                lblStatus.Text = "CANNOT OPEN FOLDER"
                Call ProjectData.ClearProjectError()
            End Try
        End Sub

        Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
            If picPreview.Image IsNot Nothing Then
                picPreview.Image.Dispose()
            End If
            MyBase.OnFormClosed(e)
        End Sub
    End Class
