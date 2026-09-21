' LCARSpic/Media/frmRadioPicker.vb
Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Windows.Forms

''' <summary>LCARS-styled scrollable internet-radio station picker.</summary>
Public Class frmRadioPicker
    Inherits Form

    Public SelectedTitle As String = ""
    Public SelectedUrl As String = ""

    Private ReadOnly scrollHost As Panel
    Private ReadOnly listInner As Panel
    Private ReadOnly txtCustom As TextBox
    Private dragging As Boolean
    Private dragStartY As Integer
    Private dragStartTop As Integer
    Private movedPx As Integer

    Public Sub New()
        Text = "LCARS RADIO"
        FormBorderStyle = FormBorderStyle.None
        MaximizeBox = False
        MinimizeBox = False
        ShowInTaskbar = False
        StartPosition = FormStartPosition.CenterParent
        BackColor = Color.Black
        ForeColor = Color.Orange
        ClientSize = New Size(440, 520)
        Font = New Font("LCARS", 14.0F, FontStyle.Regular)

        Dim tbTitle As New LCARS.Controls.TextButton()
        tbTitle.ButtonText = "INTERNET RADIO"
        tbTitle.Text = "INTERNET RADIO"
        tbTitle.Clickable = False
        tbTitle.Location = New Point(8, 8)
        tbTitle.Size = New Size(424, 32)
        Controls.Add(tbTitle)

        scrollHost = New Panel()
        scrollHost.Location = New Point(8, 48)
        scrollHost.Size = New Size(424, 360)
        scrollHost.BackColor = Color.Black
        scrollHost.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        Controls.Add(scrollHost)

        listInner = New Panel()
        listInner.Location = New Point(0, 0)
        listInner.Width = scrollHost.Width
        listInner.BackColor = Color.Black
        scrollHost.Controls.Add(listInner)

        Dim y As Integer = 0
        Const btnH As Integer = 30
        Const gap As Integer = 4
        Dim colorFlip As Boolean = False
        For Each st As RadioStations.Station In RadioStations.All
            Dim btn As New LCARS.Controls.StandardButton()
            btn.ButtonText = st.Title.ToUpperInvariant()
            btn.Text = btn.ButtonText
            btn.Size = New Size(listInner.Width - 4, btnH)
            btn.Location = New Point(2, y)
            btn.Color = If(colorFlip, LCARS.LCARScolorStyles.SystemFunction, LCARS.LCARScolorStyles.PrimaryFunction)
            btn.Tag = st
            AddHandler btn.Click, AddressOf Station_Click
            listInner.Controls.Add(btn)
            WireDrag(btn)
            y += btnH + gap
            colorFlip = Not colorFlip
        Next
        listInner.Height = Math.Max(scrollHost.Height, y + 4)
        WireDrag(scrollHost)
        WireDrag(listInner)

        Dim lblCustom As New Label()
        lblCustom.Text = "CUSTOM URL"
        lblCustom.ForeColor = Color.Orange
        lblCustom.Font = New Font("LCARS", 12.0F, FontStyle.Regular)
        lblCustom.Location = New Point(8, 418)
        lblCustom.AutoSize = True
        lblCustom.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        Controls.Add(lblCustom)

        txtCustom = New TextBox()
        txtCustom.Location = New Point(8, 440)
        txtCustom.Size = New Size(300, 28)
        txtCustom.BackColor = Color.FromArgb(20, 20, 20)
        txtCustom.ForeColor = Color.Orange
        txtCustom.BorderStyle = BorderStyle.FixedSingle
        txtCustom.Font = New Font("LCARS", 12.0F, FontStyle.Regular)
        txtCustom.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        Controls.Add(txtCustom)

        Dim sbPlayUrl As New LCARS.Controls.StandardButton()
        sbPlayUrl.ButtonText = "PLAY URL"
        sbPlayUrl.Text = "PLAY URL"
        sbPlayUrl.Color = LCARS.LCARScolorStyles.PrimaryFunction
        sbPlayUrl.Size = New Size(110, 28)
        sbPlayUrl.Location = New Point(318, 440)
        sbPlayUrl.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        AddHandler sbPlayUrl.Click, AddressOf PlayUrl_Click
        Controls.Add(sbPlayUrl)

        Dim sbCancel As New LCARS.Controls.StandardButton()
        sbCancel.ButtonText = "CANCEL"
        sbCancel.Text = "CANCEL"
        sbCancel.Color = LCARS.LCARScolorStyles.FunctionOffline
        sbCancel.Size = New Size(110, 28)
        sbCancel.Location = New Point(318, 480)
        sbCancel.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        AddHandler sbCancel.Click, Sub()
                                       DialogResult = DialogResult.Cancel
                                       Close()
                                   End Sub
        Controls.Add(sbCancel)

        Dim tip As New Label()
        tip.Text = "DRAG LIST TO SCROLL"
        tip.ForeColor = Color.FromArgb(180, 120, 40)
        tip.Font = New Font("LCARS", 10.0F, FontStyle.Regular)
        tip.Location = New Point(8, 484)
        tip.AutoSize = True
        tip.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        Controls.Add(tip)
    End Sub

    Private Sub Station_Click(ByVal sender As Object, ByVal e As EventArgs)
        If movedPx >= 10 Then Return
        Dim btn As LCARS.Controls.StandardButton = TryCast(sender, LCARS.Controls.StandardButton)
        If btn Is Nothing Then Return
        Dim st As RadioStations.Station = CType(btn.Tag, RadioStations.Station)
        SelectedTitle = st.Title
        SelectedUrl = st.Url
        DialogResult = DialogResult.OK
        Close()
    End Sub

    Private Sub PlayUrl_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim url As String = If(txtCustom.Text, "").Trim()
        If Not url.StartsWith("http", StringComparison.OrdinalIgnoreCase) Then
            MsgBox("Paste an http(s) stream URL.", MsgBoxStyle.OkOnly, "LCARS RADIO")
            Return
        End If
        SelectedTitle = url
        SelectedUrl = url
        DialogResult = DialogResult.OK
        Close()
    End Sub

    Private Sub WireDrag(ByVal c As Control)
        AddHandler c.MouseDown, AddressOf OnListDown
        AddHandler c.MouseMove, AddressOf OnListMove
        AddHandler c.MouseUp, AddressOf OnListUp
    End Sub

    Private Sub OnListDown(ByVal sender As Object, ByVal e As MouseEventArgs)
        If e.Button <> MouseButtons.Left Then Return
        dragging = True
        movedPx = 0
        dragStartY = Cursor.Position.Y
        dragStartTop = listInner.Top
        scrollHost.Capture = True
    End Sub

    Private Sub OnListMove(ByVal sender As Object, ByVal e As MouseEventArgs)
        If Not dragging Then Return
        Dim dy As Integer = Cursor.Position.Y - dragStartY
        movedPx = Math.Max(movedPx, Math.Abs(dy))
        If movedPx < 8 Then Return
        Dim minTop As Integer = Math.Min(0, scrollHost.Height - listInner.Height)
        Dim nextTop As Integer = dragStartTop + dy
        If nextTop > 0 Then nextTop = 0
        If nextTop < minTop Then nextTop = minTop
        listInner.Top = nextTop
    End Sub

    Private Sub OnListUp(ByVal sender As Object, ByVal e As MouseEventArgs)
        dragging = False
        scrollHost.Capture = False
    End Sub
End Class
