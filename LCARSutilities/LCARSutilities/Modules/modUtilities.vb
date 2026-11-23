Imports System
Imports System.Diagnostics
Imports System.Drawing
Imports System.IO
Imports System.Runtime.CompilerServices
Imports System.Windows.Forms
Imports LCARS
Imports LCARS.Controls
Imports Microsoft.VisualBasic.CompilerServices

    <StandardModule>
    Public NotInheritable Class modUtilities
        Friend Const UtilityFontName As String = "Arial Narrow"

        Friend Const UtilityMargin As Integer = 12

        Friend Const UtilityButtonHeight As Integer = 28

        Private Const CascadeStep As Integer = 40

        Private Shared cascadeIndex As Integer = 0

        Private Shared dragForm As Form = Nothing

        Private Shared dragGrab As Point = Point.Empty

        Private Shared dragActive As Boolean = False

        Friend Shared Sub InitUtilityFormFloating(frm As LCARSForm, clientWidth As Integer, clientHeight As Integer)
            frm.Text = "LCARS"
            frm.BackColor = Color.Black
            frm.FormBorderStyle = FormBorderStyle.None
            frm.ControlBox = False
            frm.MaximizeBox = False
            frm.MinimizeBox = False
            frm.ShowInTaskbar = True
            frm.TopMost = False
            frm.StartPosition = FormStartPosition.Manual
            frm.BindToWorkingArea = False
            frm.RegisterWithLcarsShellEnabled = True
            frm.WindowState = FormWindowState.Normal
            Dim clientSize As Size = New Size(Math.Max(200, clientWidth), Math.Max(160, clientHeight))
            frm.ClientSize = clientSize
            Dim workingArea = Screen.FromPoint(Cursor.Position).WorkingArea
            Dim num As Integer = workingArea.Left + Math.Max(0, workingArea.Width - frm.Width / 2)
            Dim num2 As Integer = workingArea.Top + Math.Max(0, workingArea.Height - frm.Height / 2)
            Dim num3 = cascadeIndex Mod 8
            cascadeIndex += 1
            Dim num4 = num + num3 * 40
            Dim num5 = num2 + num3 * 40
            If num4 + frm.Width > workingArea.Right Then
                num4 = Math.Max(workingArea.Left, workingArea.Right - frm.Width)
            End If
            If num5 + frm.Height > workingArea.Bottom Then
                num5 = Math.Max(workingArea.Top, workingArea.Bottom - frm.Height)
            End If
            Dim location As Point = New Point(num4, num5)
            frm.Location = location
        End Sub

        Friend Shared Sub InitUtilityForm(frm As LCARSForm, clientWidth As Integer, clientHeight As Integer)
            InitUtilityFormFloating(frm, clientWidth, clientHeight)
        End Sub

        Friend Shared Sub WireUtilityTitleDrag(title As Control, frm As Form)
            If title IsNot Nothing AndAlso frm IsNot Nothing Then
                AddHandler title.MouseDown, AddressOf TitleDrag_MouseDown
                AddHandler title.MouseMove, AddressOf TitleDrag_MouseMove
                AddHandler title.MouseUp, AddressOf TitleDrag_MouseUp
                title.Tag = frm
            End If
        End Sub

        Private Shared Sub TitleDrag_MouseDown(sender As Object, e As MouseEventArgs)
            Dim control As Control = Nothing

            If e.Button <> MouseButtons.Left OrElse Not (CSharpImpl.__Assign(control, TryCast(sender, Control)) IsNot Nothing) Then
                Return
            End If
            Dim form As Form = TryCast(control.Tag, Form)
            If form Is Nothing Then
                form = control.FindForm()
            End If
            If form Is Nothing Then
                Return
            End If
            dragForm = form
            dragGrab = New Point(Control.MousePosition.X - form.Left, Control.MousePosition.Y - form.Top)
            dragActive = True
            Try
                control.Capture = True
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
        End Sub

        Private Shared Sub TitleDrag_MouseMove(sender As Object, e As MouseEventArgs)
            If Not dragActive OrElse dragForm Is Nothing Then
                Return
            End If
            If (Control.MouseButtons And MouseButtons.Left) = 0 Then
                TitleDrag_MouseUp(RuntimeHelpers.GetObjectValue(sender), e)
                Return
            End If
            Dim num = Control.MousePosition.X - dragGrab.X
            Dim num2 = Control.MousePosition.Y - dragGrab.Y
            Dim workingArea = Screen.FromControl(dragForm).WorkingArea
            If num + dragForm.Width < workingArea.Left + 40 Then
                num = workingArea.Left + 40 - dragForm.Width
            End If
            If num2 < workingArea.Top Then
                num2 = workingArea.Top
            End If
            If num > workingArea.Right - 40 Then
                num = workingArea.Right - 40
            End If
            If num2 > workingArea.Bottom - 40 Then
                num2 = workingArea.Bottom - 40
            End If
            Dim form = dragForm
            Dim location As Point = New Point(num, num2)
            form.Location = location
        End Sub

        Private Shared Sub TitleDrag_MouseUp(sender As Object, e As MouseEventArgs)
            dragActive = False
            dragForm = Nothing
            Dim control As Control = Nothing

            If CSharpImpl.__Assign(control, TryCast(sender, Control)) IsNot Nothing Then
                Try
                    control.Capture = False
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    Call ProjectData.ClearProjectError()
                End Try
            End If
        End Sub

        Friend Shared Function MakeUtilityTitle(parent As Control, text As String, width As Integer) As FlatButton
            Dim flatButton As FlatButton = New FlatButton()
            flatButton.ButtonText = text
            flatButton.Text = text
            flatButton.Clickable = True
            flatButton.Beeping = False
            flatButton.Color = LCARScolorStyles.StaticTan
            flatButton.ButtonTextAlign = ContentAlignment.MiddleLeft
            Dim location As Point = New Point(12, 12)
            flatButton.Location = location
            Dim size As Size = New Size(width, 28)
            flatButton.Size = size
            parent.Controls.Add(flatButton)
            Dim form As Form = TryCast(parent, Form)
            If form Is Nothing AndAlso parent IsNot Nothing Then
                form = parent.FindForm()
            End If
            If form IsNot Nothing Then
                WireUtilityTitleDrag(flatButton, form)
            End If
            Return flatButton
        End Function

        Friend Shared Function MakeUtilityButton(parent As Control, text As String, x As Integer, y As Integer, width As Integer, height As Integer, buttonColor As LCARScolorStyles, handler As EventHandler) As FlatButton
            Dim flatButton As FlatButton = New FlatButton()
            flatButton.ButtonText = text
            flatButton.Text = text
            flatButton.Clickable = True
            flatButton.Beeping = True
            flatButton.Color = buttonColor
            Dim location As Point = New Point(x, y)
            flatButton.Location = location
            Dim size As Size = New Size(width, height)
            flatButton.Size = size
            AddHandler flatButton.Click, handler
            parent.Controls.Add(flatButton)
            Return flatButton
        End Function

        Friend Shared Function MakeUtilityLabel(parent As Control, x As Integer, y As Integer, width As Integer, height As Integer, fontSize As Single) As Label
            Dim label As Label = New Label()
            label.ForeColor = Color.Orange
            label.BackColor = Color.Black
            label.Font = New Font("Arial Narrow", fontSize, FontStyle.Regular, GraphicsUnit.Point)
            Dim location As Point = New Point(x, y)
            label.Location = location
            Dim size As Size = New Size(width, height)
            label.Size = size
            parent.Controls.Add(label)
            Return label
        End Function

        Friend Shared Sub OpenUrlInLcarsBrowser(url As String)
            If String.IsNullOrEmpty(url) Then
                Return
            End If
            Dim text = Path.Combine(Application.StartupPath, "LCARSWebBrowser.exe")
            Try
                If File.Exists(text) Then
                    Dim processStartInfo As ProcessStartInfo = New ProcessStartInfo()
                    processStartInfo.FileName = text
                    processStartInfo.Arguments = """" & url & """"
                    processStartInfo.WorkingDirectory = Application.StartupPath
                    processStartInfo.UseShellExecute = True
                    Process.Start(processStartInfo)
                Else
                    Dim processStartInfo2 As ProcessStartInfo = New ProcessStartInfo()
                    processStartInfo2.FileName = url
                    processStartInfo2.UseShellExecute = True
                    Process.Start(processStartInfo2)
                End If
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
        End Sub

        Private Class CSharpImpl
            <Obsolete("Please refactor calling code to use normal Visual Basic assignment")>
            Shared Function __Assign(Of T)(ByRef target As T, value As T) As T
                target = value
                Return value
            End Function
        End Class
    End Class
