Public Class ScreenChooserDialog
    Inherits LCARS.LCARSForm
    Private screenType As Type = Nothing
    Private selectedScreen As LCScreenImage = Nothing
    Private _screenIndex As Integer

    Public Sub New(ByVal screenIndex As Integer)
        InitializeComponent()
        _screenIndex = screenIndex
        ' A taskbar button and working-area binding leave this dialog on top of the new mode.
        Me.ShowInTaskbar = False
        Me.BindToWorkingArea = False
    End Sub

    Private Sub btnOK_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnOK.Click
        If screenType Is Nothing Then
            Me.Close()
            Return
        End If
        If Not IsMainScreenSelectable(screenType) Then
            LCARS.UI.MsgBox("That interface is offline.", MsgBoxStyle.OkOnly, "INTERFACE SELECTOR")
            Return
        End If
        Me.ShowInTaskbar = False
        Me.Hide()
        If Me.IsHandleCreated Then
            LinkedWindows.Remove(Me.Handle)
        End If
        Dim switched As Boolean = False
        modDiagnostics.BeginModeSwitchTrace("screen=" & _screenIndex & " targetType=" & screenType.FullName)
        Try
            modDiagnostics.LogStep("ScreenChooser.btnOK", "calling ShutdownScreen screen=" & _screenIndex)
            curBusiness(_screenIndex).ShutdownScreen()
            modDiagnostics.LogStep("ScreenChooser.btnOK", "creating instance " & screenType.Name)
            Dim myForm As Form = CType(Activator.CreateInstance(screenType, curBusiness(_screenIndex)), Form)
            modDiagnostics.LogStep("ScreenChooser.btnOK", "calling init " & screenType.Name)
            curBusiness(_screenIndex).init(myForm)
            switched = True
            modDiagnostics.LogStep("ScreenChooser.btnOK", "init completed " & screenType.Name & " bounds=" & myForm.Bounds.ToString())
        Catch ex As Exception
            modDiagnostics.LogException("ScreenChooserDialog.btnOK_Click", ex, "screen=" & _screenIndex)
            If Not switched Then
                Try
                    Dim business As modBusiness = curBusiness(_screenIndex)
                    If business.myForm Is Nothing OrElse business.myForm.IsDisposed Then
                        business.init(New frmMainscreen1(business))
                    End If
                Catch restoreEx As Exception
                    modDiagnostics.LogException("ScreenChooserDialog.btnOK_Click", restoreEx, "restore screen=" & _screenIndex)
                End Try
            End If
        Finally
            Try
                If Not Me.IsDisposed Then Me.Close()
            Catch
                Try
                    If Not Me.IsDisposed Then Me.Dispose()
                Catch
                End Try
            End Try
        End Try
    End Sub

    Private Sub sbCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles sbCancel.Click
        Me.Close()
    End Sub

    Private Sub ScreenChooserDialog_FormClosing(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing
        For i As Integer = 0 To gridScreens.Count - 1
            Dim myScreenImage As LCScreenImage = CType(gridScreens.Items(i), LCScreenImage)
            If Not myScreenImage.Image Is Nothing Then
                myScreenImage.Image.Dispose()
            End If
        Next
    End Sub

    Private Sub ScreenChooserDialog_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        For Each myScreenType As Type In getMainScreenTypes()
            Dim myScreen As New LCScreenImage()
            myScreen.Data = myScreenType
            AddHandler myScreen.Click, AddressOf myScreen_Click

            Dim unavailable As Boolean = Not IsMainScreenSelectable(myScreenType)
            If unavailable Then
                myScreen.Text = "SIGNAL LOST"
                myScreen.Corrupted = True
                myScreen.Image = CreateCorruptedPreviewImage()
            Else
                If GetType(frmMainscreen5).Equals(myScreenType) Then
                    myScreen.Text = "KLINGON // MODE 05"
                    myScreen.Color = LCARS.LCARScolorStyles.CriticalFunction
                    myScreen.Image = CreateCorruptedPreviewImage()
                Else
                    myScreen.Text = myScreenType.Name
                    Try
                        'Get cached files (Will match screen aspect ratio)
                        myScreen.Image = CType(System.Drawing.Image.FromFile(System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "\LCARS x32\Images\" & myScreenType.Name & "_" & _screenIndex & ".jpg"), Bitmap)
                    Catch ex As Exception
                        'File didn't exist or was in use
                        Dim pInfo As Reflection.PropertyInfo = myScreenType.GetProperty("ScreenImage")
                        If Not pInfo Is Nothing Then
                            myScreen.Image = CType(pInfo.GetValue(Nothing, Nothing), Bitmap)
                        End If
                    End Try
                End If
            End If
            gridScreens.Add(myScreen)
        Next
        LCARS.SetBeeping(Me)
    End Sub

    Private Sub myScreen_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim tile As LCScreenImage = CType(sender, LCScreenImage)
        Dim t As Type = TryCast(tile.Data, Type)
        If t Is Nothing OrElse Not IsMainScreenSelectable(t) OrElse tile.Corrupted Then
            Return
        End If
        If selectedScreen IsNot Nothing Then
            selectedScreen.Selected = False
        End If
        selectedScreen = tile
        selectedScreen.Selected = True
        screenType = t
    End Sub
End Class
