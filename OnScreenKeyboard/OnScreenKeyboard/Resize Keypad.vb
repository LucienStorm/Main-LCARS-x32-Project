Public Class Resize_Keypad

    'required for drag option
    Dim drag As Boolean
    Dim mousex As Integer
    Dim mousey As Integer

    ' Always drive the OSK that owns this keypad — never a stray default instance.
    Private ReadOnly Property HostKeyboard As frmKeyboard
        Get
            Dim owned As frmKeyboard = TryCast(Me.Owner, frmKeyboard)
            If owned IsNot Nothing Then Return owned
            Return frmKeyboard
        End Get
    End Property

    Private Sub Resize_Keypad_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        Me.TopMost = True

        'Sets the resize panel to the last location chosen by user
        Dim saved As Point = My.Settings.Resize_KeypadPosition
        Dim host As frmKeyboard = HostKeyboard
        Dim scr As Screen = Screen.FromPoint(If(host IsNot Nothing, host.Location, Cursor.Position))
        Const menuClearance As Integer = 110
        If saved.IsEmpty OrElse saved.Y < scr.Bounds.Top + menuClearance OrElse
           saved.X < scr.Bounds.Left OrElse saved.X > scr.Bounds.Right - 50 Then
            Me.Left = scr.Bounds.Left + (scr.Bounds.Width - Me.Width) \ 2
            Me.Top = scr.Bounds.Top + menuClearance
        Else
            Me.Location = saved
        End If

        Me.BringToFront()
    End Sub


    Private Sub sbDone_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles sbDone.Click

        'Sets the resize panel to the last location chosen by user
        My.Settings.Resize_KeypadPosition = Location
        HostKeyboard.frmKeyboard_ResizeEnd(sender, e)
        My.Settings.Save()
        Me.Hide()



    End Sub

    Private Sub sbWidthMinus_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles sbWidthMinus.Click

        HostKeyboard.sbWidthMinus_Click(sender, e)

    End Sub

    Private Sub sbWidthPlus_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles sbWidthPlus.Click

        HostKeyboard.sbWidthPlus_Click(sender, e)

    End Sub

    Private Sub sbHeightMinus_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles sbHeightMinus.Click

        HostKeyboard.sbHeightMinus_Click(sender, e)

    End Sub

    Private Sub sbHeightPlus_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles sbHeightPlus.Click

        HostKeyboard.sbHeightPlus_Click(sender, e)

    End Sub

    Private Sub sbIncrementMinus_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles sbIncrementMinus.Click

        HostKeyboard.sbIncrementMinus_Click(sender, e)
        lblIncrement.Text = HostKeyboard.lblIncrement.Text

    End Sub


    Private Sub sbIncrementPlus_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles sbIncrementPlus.Click

        HostKeyboard.sbIncrementPlus_Click(sender, e)
        lblIncrement.Text = HostKeyboard.lblIncrement.Text

    End Sub



    Private Sub sbMove_MouseDown(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles sbMove.MouseDown

        drag = True 'Sets drag variable to true
        mousex = Windows.Forms.Cursor.Position.X - Me.Left
        mousey = Windows.Forms.Cursor.Position.Y - Me.Top

    End Sub



    Private Sub sbMove_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles sbMove.MouseMove

        'Drag Function
        If drag Then
            Me.Top = Windows.Forms.Cursor.Position.Y - mousey
            Me.Left = Windows.Forms.Cursor.Position.X - mousex
        End If

    End Sub

    Private Sub sbMove_MouseUp(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles sbMove.MouseUp

        drag = False
        My.Settings.Resize_KeypadPosition = Me.Location
        My.Settings.Save()
    End Sub


End Class
