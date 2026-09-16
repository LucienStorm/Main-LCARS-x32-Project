
Public Class frmMainscreen3
    Implements IAutohide

    Private myBusiness As modBusiness

    Public Function getAutohideEdges() As IAutohide.AutohideEdges Implements IAutohide.getAutohideEdges
        Return IAutohide.AutohideEdges.Top Or IAutohide.AutohideEdges.Bottom
    End Function

    Public Sub New(ByVal b As modBusiness)
        InitializeComponent()
        Me.myBusiness = b
    End Sub

    Private Sub myStartMenu_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles myStartMenu.Click
        pnlStart.Visible = Not pnlStart.Visible
        If pnlStart.Visible Then
            pnlMain.Left += pnlStart.Width + 6
            pnlMain.Width -= pnlStart.Width + 6
            myBusiness.RefreshStartMenuPrograms()
        Else
            pnlMain.Left -= pnlStart.Width + 6
            pnlMain.Width += pnlStart.Width + 6
        End If
    End Sub

    Private Sub startMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles myDeactivate.Click, myDestruct.Click, myAlert.Click, myModeSelect.Click, myEngineering.Click, MyComp.Click, mySettings.Click, myPhoto.Click, fbWebBrowser.Click, myRun.Click, myVideos.Click, myMusic.Click, myDocuments.Click, myPictures.Click
        If myStartMenu.Visible Then myStartMenu.doClick(sender, e)
    End Sub

    Private Sub myUserButtons_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles myUserButtons.Click
        If gridUserButtons Is Nothing Then Return
        myBusiness.TogglePersonalPrograms(Not gridUserButtons.Visible, gridUserButtons.Width)
    End Sub

    Public Shared ReadOnly Property ScreenImage() As Image
        Get
            Return My.Resources.frmmainscreen3
        End Get
    End Property

End Class