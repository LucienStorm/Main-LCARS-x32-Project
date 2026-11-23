<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmMainscreen5
    Inherits System.Windows.Forms.Form

    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    Private components As System.ComponentModel.IContainer

    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.pnlMainBar = New System.Windows.Forms.Panel
        Me.myClock = New LCARS.Controls.FlatButton
        Me.myStartMenu = New LCARS.Controls.FlatButton
        Me.myHelp = New LCARS.Controls.FlatButton
        Me.myOSK = New LCARS.Controls.FlatButton
        Me.mySpeech = New LCARS.Controls.FlatButton
        Me.myUserButtons = New LCARS.Controls.FlatButton
        Me.pnlApps = New System.Windows.Forms.Panel
        Me.pnlTray = New System.Windows.Forms.Panel
        Me.HideTrayButton = New LCARS.Controls.ArrowButton
        Me.ShowTrayButton = New LCARS.Controls.FlatButton
        Me.myButtonManager = New LCARS.Controls.FlatButton
        Me.pnlStart = New System.Windows.Forms.Panel
        Me.myAlertListButton = New LCARS.Controls.ArrowButton
        Me.pnlBatt = New System.Windows.Forms.Panel
        Me.fbBatt10 = New LCARS.Controls.FlatButton
        Me.fbBatt9 = New LCARS.Controls.FlatButton
        Me.fbBatt8 = New LCARS.Controls.FlatButton
        Me.fbBatt7 = New LCARS.Controls.FlatButton
        Me.fbBatt6 = New LCARS.Controls.FlatButton
        Me.fbBatt5 = New LCARS.Controls.FlatButton
        Me.fbBatt4 = New LCARS.Controls.FlatButton
        Me.fbBatt3 = New LCARS.Controls.FlatButton
        Me.fbBatt2 = New LCARS.Controls.FlatButton
        Me.fbBatt1 = New LCARS.Controls.FlatButton
        Me.lblBatt = New System.Windows.Forms.Label
        Me.lblPowerSource = New System.Windows.Forms.Label
        Me.myMusic = New LCARS.Controls.FlatButton
        Me.myVideos = New LCARS.Controls.FlatButton
        Me.myPictures = New LCARS.Controls.FlatButton
        Me.myDocuments = New LCARS.Controls.FlatButton
        Me.myRun = New LCARS.Controls.FlatButton
        Me.fbWebBrowser = New LCARS.Controls.FlatButton
        Me.fbTerminal = New LCARS.Controls.FlatButton
        Me.fbDesktop = New LCARS.Controls.FlatButton
        Me.fbMyNetwork = New LCARS.Controls.FlatButton
        Me.myPhoto = New LCARS.Controls.FlatButton
        Me.pnlPrograms = New LCARS.Controls.WindowlessContainer
        Me.myAlert = New LCARS.Controls.FlatButton
        Me.myEngineering = New LCARS.Controls.FlatButton
        Me.mySettings = New LCARS.Controls.FlatButton
        Me.MyComp = New LCARS.Controls.FlatButton
        Me.myModeSelect = New LCARS.Controls.FlatButton
        Me.myDeactivate = New LCARS.Controls.FlatButton
        Me.myDestruct = New LCARS.Controls.FlatButton
        Me.myProgsUp = New LCARS.Controls.FlatButton
        Me.fbProgramPages = New LCARS.Controls.FlatButton
        Me.myProgsNext = New LCARS.Controls.ArrowButton
        Me.myProgsBack = New LCARS.Controls.ArrowButton
        Me.gridUserButtons = New LCARS.Controls.ButtonGrid
        Me.pnlMain = New System.Windows.Forms.Panel
        Me.pnlMainBar.SuspendLayout()
        Me.pnlTray.SuspendLayout()
        Me.pnlStart.SuspendLayout()
        Me.pnlBatt.SuspendLayout()
        Me.SuspendLayout()
        '
        'pnlMainBar
        '
        Me.pnlMainBar.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.pnlMainBar.BackColor = System.Drawing.Color.Black
        Me.pnlMainBar.Controls.Add(Me.myClock)
        Me.pnlMainBar.Controls.Add(Me.myStartMenu)
        Me.pnlMainBar.Controls.Add(Me.myHelp)
        Me.pnlMainBar.Controls.Add(Me.myOSK)
        Me.pnlMainBar.Controls.Add(Me.mySpeech)
        Me.pnlMainBar.Controls.Add(Me.myUserButtons)
        Me.pnlMainBar.Controls.Add(Me.pnlApps)
        Me.pnlMainBar.Controls.Add(Me.pnlTray)
        Me.pnlMainBar.Controls.Add(Me.myButtonManager)
        Me.pnlMainBar.Controls.Add(Me.pnlStart)
        Me.pnlMainBar.Controls.Add(Me.gridUserButtons)
        Me.pnlMainBar.Controls.Add(Me.pnlMain)
        Me.pnlMainBar.Controls.Add(Me.pnlBatt)
        Me.pnlMainBar.Controls.Add(Me.myAlertListButton)
        Me.pnlMainBar.Controls.Add(Me.myAlert)
        Me.pnlMainBar.Controls.Add(Me.myDestruct)
        Me.pnlMainBar.Controls.Add(Me.myDeactivate)
        Me.pnlMainBar.Controls.Add(Me.myModeSelect)
        Me.pnlMainBar.Controls.Add(Me.MyComp)
        Me.pnlMainBar.Controls.Add(Me.mySettings)
        Me.pnlMainBar.Controls.Add(Me.myEngineering)
        Me.pnlMainBar.Controls.Add(Me.myPhoto)
        Me.pnlMainBar.Controls.Add(Me.fbWebBrowser)
        Me.pnlMainBar.Controls.Add(Me.fbTerminal)
        Me.pnlMainBar.Location = New System.Drawing.Point(0, 0)
        Me.pnlMainBar.Name = "pnlMainBar"
        Me.pnlMainBar.Size = New System.Drawing.Size(1024, 768)
        Me.pnlMainBar.TabIndex = 0
        '
        'myClock
        '
        Me.myClock.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.myClock.ButtonText = "STARDATE"
        Me.myClock.ButtonTextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.myClock.Clickable = False
        Me.myClock.Color = LCARS.LCARScolorStyles.CriticalFunction
        Me.myClock.Location = New System.Drawing.Point(820, 6)
        Me.myClock.Name = "myClock"
        Me.myClock.Size = New System.Drawing.Size(196, 32)
        Me.myClock.TabIndex = 1
        Me.myClock.Text = "STARDATE"
        '
        'myStartMenu
        '
        Me.myStartMenu.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.myStartMenu.ButtonText = "ghunmeH"
        Me.myStartMenu.ButtonTextAlign = System.Drawing.ContentAlignment.MiddleCenter
        Me.myStartMenu.Color = LCARS.LCARScolorStyles.CriticalFunction
        Me.myStartMenu.Location = New System.Drawing.Point(12, 726)
        Me.myStartMenu.Name = "myStartMenu"
        Me.myStartMenu.Size = New System.Drawing.Size(148, 34)
        Me.myStartMenu.TabIndex = 2
        Me.myStartMenu.Text = "ghunmeH"
        '
        'mySpeech
        '
        Me.mySpeech.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.mySpeech.ButtonText = "ghogh"
        Me.mySpeech.ButtonTextAlign = System.Drawing.ContentAlignment.MiddleCenter
        Me.mySpeech.Color = LCARS.LCARScolorStyles.PrimaryFunction
        Me.mySpeech.Location = New System.Drawing.Point(210, 8)
        Me.mySpeech.Name = "mySpeech"
        Me.mySpeech.Size = New System.Drawing.Size(86, 30)
        Me.mySpeech.TabIndex = 3
        Me.mySpeech.Text = "ghogh"
        '
        'myOSK
        '
        Me.myOSK.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.myOSK.ButtonText = "pIqaD"
        Me.myOSK.ButtonTextAlign = System.Drawing.ContentAlignment.MiddleCenter
        Me.myOSK.Color = LCARS.LCARScolorStyles.PrimaryFunction
        Me.myOSK.Location = New System.Drawing.Point(302, 8)
        Me.myOSK.Name = "myOSK"
        Me.myOSK.Size = New System.Drawing.Size(86, 30)
        Me.myOSK.TabIndex = 4
        Me.myOSK.Text = "pIqaD"
        '
        'myHelp
        '
        Me.myHelp.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.myHelp.ButtonText = "QaH"
        Me.myHelp.ButtonTextAlign = System.Drawing.ContentAlignment.MiddleCenter
        Me.myHelp.Color = LCARS.LCARScolorStyles.PrimaryFunction
        Me.myHelp.Location = New System.Drawing.Point(394, 8)
        Me.myHelp.Name = "myHelp"
        Me.myHelp.Size = New System.Drawing.Size(86, 30)
        Me.myHelp.TabIndex = 5
        Me.myHelp.Text = "QaH"
        '
        'myUserButtons
        '
        Me.myUserButtons.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.myUserButtons.ButtonText = "nuv"
        Me.myUserButtons.ButtonTextAlign = System.Drawing.ContentAlignment.MiddleCenter
        Me.myUserButtons.Color = LCARS.LCARScolorStyles.PrimaryFunction
        Me.myUserButtons.Location = New System.Drawing.Point(870, 726)
        Me.myUserButtons.Name = "myUserButtons"
        Me.myUserButtons.Size = New System.Drawing.Size(142, 34)
        Me.myUserButtons.TabIndex = 6
        Me.myUserButtons.Text = "nuv"
        '
        'pnlApps
        '
        Me.pnlApps.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.pnlApps.BackColor = System.Drawing.Color.Black
        Me.pnlApps.Location = New System.Drawing.Point(168, 728)
        Me.pnlApps.Name = "pnlApps"
        Me.pnlApps.Size = New System.Drawing.Size(486, 30)
        Me.pnlApps.TabIndex = 7
        '
        'pnlTray
        '
        Me.pnlTray.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.pnlTray.BackColor = System.Drawing.Color.Black
        Me.pnlTray.Controls.Add(Me.HideTrayButton)
        Me.pnlTray.Controls.Add(Me.ShowTrayButton)
        Me.pnlTray.Location = New System.Drawing.Point(736, 726)
        Me.pnlTray.Name = "pnlTray"
        Me.pnlTray.Size = New System.Drawing.Size(128, 34)
        Me.pnlTray.TabIndex = 8
        '
        'HideTrayButton
        '
        Me.HideTrayButton.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.HideTrayButton.ArrowDirection = LCARS.LCARSarrowDirection.Right
        Me.HideTrayButton.Location = New System.Drawing.Point(96, 2)
        Me.HideTrayButton.Name = "HideTrayButton"
        Me.HideTrayButton.Size = New System.Drawing.Size(30, 30)
        Me.HideTrayButton.TabIndex = 1
        Me.HideTrayButton.Visible = False
        '
        'ShowTrayButton
        '
        Me.ShowTrayButton.ButtonText = "De'Hom"
        Me.ShowTrayButton.ButtonTextAlign = System.Drawing.ContentAlignment.MiddleCenter
        Me.ShowTrayButton.Color = LCARS.LCARScolorStyles.CriticalFunction
        Me.ShowTrayButton.Location = New System.Drawing.Point(0, 0)
        Me.ShowTrayButton.Name = "ShowTrayButton"
        Me.ShowTrayButton.Size = New System.Drawing.Size(128, 34)
        Me.ShowTrayButton.TabIndex = 2
        Me.ShowTrayButton.Text = "De'Hom"
        '
        'myButtonManager
        '
        Me.myButtonManager.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.myButtonManager.ButtonText = "nuv vu'"
        Me.myButtonManager.Color = LCARS.LCARScolorStyles.CriticalFunction
        Me.myButtonManager.Location = New System.Drawing.Point(792, 686)
        Me.myButtonManager.Name = "myButtonManager"
        Me.myButtonManager.Size = New System.Drawing.Size(220, 28)
        Me.myButtonManager.TabIndex = 9
        Me.myButtonManager.Text = "nuv vu'"
        Me.myButtonManager.Visible = False
        '
        'pnlBatt
        '
        Me.pnlBatt.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.pnlBatt.BackColor = System.Drawing.Color.Black
        Me.pnlBatt.Controls.Add(Me.fbBatt10)
        Me.pnlBatt.Controls.Add(Me.fbBatt9)
        Me.pnlBatt.Controls.Add(Me.fbBatt8)
        Me.pnlBatt.Controls.Add(Me.fbBatt7)
        Me.pnlBatt.Controls.Add(Me.fbBatt6)
        Me.pnlBatt.Controls.Add(Me.fbBatt5)
        Me.pnlBatt.Controls.Add(Me.fbBatt4)
        Me.pnlBatt.Controls.Add(Me.fbBatt3)
        Me.pnlBatt.Controls.Add(Me.fbBatt2)
        Me.pnlBatt.Controls.Add(Me.fbBatt1)
        Me.pnlBatt.Controls.Add(Me.lblBatt)
        Me.pnlBatt.Controls.Add(Me.lblPowerSource)
        Me.pnlBatt.Location = New System.Drawing.Point(10, 52)
        Me.pnlBatt.Name = "pnlBatt"
        Me.pnlBatt.Size = New System.Drawing.Size(134, 72)
        Me.pnlBatt.TabIndex = 10
        '
        'fbBatt1..10
        '
        Me.fbBatt1.Clickable = False
        Me.fbBatt1.Color = LCARS.LCARScolorStyles.CriticalFunction
        Me.fbBatt1.Location = New System.Drawing.Point(6, 4)
        Me.fbBatt1.Name = "fbBatt1"
        Me.fbBatt1.Size = New System.Drawing.Size(10, 22)
        Me.fbBatt2.Clickable = False
        Me.fbBatt2.Color = LCARS.LCARScolorStyles.CriticalFunction
        Me.fbBatt2.Location = New System.Drawing.Point(18, 4)
        Me.fbBatt2.Name = "fbBatt2"
        Me.fbBatt2.Size = New System.Drawing.Size(10, 22)
        Me.fbBatt3.Clickable = False
        Me.fbBatt3.Color = LCARS.LCARScolorStyles.CriticalFunction
        Me.fbBatt3.Location = New System.Drawing.Point(30, 4)
        Me.fbBatt3.Name = "fbBatt3"
        Me.fbBatt3.Size = New System.Drawing.Size(10, 22)
        Me.fbBatt4.Clickable = False
        Me.fbBatt4.Color = LCARS.LCARScolorStyles.CriticalFunction
        Me.fbBatt4.Location = New System.Drawing.Point(42, 4)
        Me.fbBatt4.Name = "fbBatt4"
        Me.fbBatt4.Size = New System.Drawing.Size(10, 22)
        Me.fbBatt5.Clickable = False
        Me.fbBatt5.Color = LCARS.LCARScolorStyles.CriticalFunction
        Me.fbBatt5.Location = New System.Drawing.Point(54, 4)
        Me.fbBatt5.Name = "fbBatt5"
        Me.fbBatt5.Size = New System.Drawing.Size(10, 22)
        Me.fbBatt6.Clickable = False
        Me.fbBatt6.Color = LCARS.LCARScolorStyles.PrimaryFunction
        Me.fbBatt6.Location = New System.Drawing.Point(66, 4)
        Me.fbBatt6.Name = "fbBatt6"
        Me.fbBatt6.Size = New System.Drawing.Size(10, 22)
        Me.fbBatt7.Clickable = False
        Me.fbBatt7.Color = LCARS.LCARScolorStyles.PrimaryFunction
        Me.fbBatt7.Location = New System.Drawing.Point(78, 4)
        Me.fbBatt7.Name = "fbBatt7"
        Me.fbBatt7.Size = New System.Drawing.Size(10, 22)
        Me.fbBatt8.Clickable = False
        Me.fbBatt8.Color = LCARS.LCARScolorStyles.PrimaryFunction
        Me.fbBatt8.Location = New System.Drawing.Point(90, 4)
        Me.fbBatt8.Name = "fbBatt8"
        Me.fbBatt8.Size = New System.Drawing.Size(10, 22)
        Me.fbBatt9.Clickable = False
        Me.fbBatt9.Color = LCARS.LCARScolorStyles.PrimaryFunction
        Me.fbBatt9.Location = New System.Drawing.Point(102, 4)
        Me.fbBatt9.Name = "fbBatt9"
        Me.fbBatt9.Size = New System.Drawing.Size(10, 22)
        Me.fbBatt10.Clickable = False
        Me.fbBatt10.Color = LCARS.LCARScolorStyles.PrimaryFunction
        Me.fbBatt10.Location = New System.Drawing.Point(114, 4)
        Me.fbBatt10.Name = "fbBatt10"
        Me.fbBatt10.Size = New System.Drawing.Size(10, 22)
        '
        'lblBatt
        '
        Me.lblBatt.AutoSize = True
        Me.lblBatt.Font = New System.Drawing.Font("LCARS", 14.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblBatt.ForeColor = System.Drawing.Color.FromArgb(255, 255, 170, 0)
        Me.lblBatt.Location = New System.Drawing.Point(6, 30)
        Me.lblBatt.Name = "lblBatt"
        Me.lblBatt.Size = New System.Drawing.Size(42, 20)
        Me.lblBatt.Text = "100%"
        '
        'lblPowerSource
        '
        Me.lblPowerSource.AutoSize = True
        Me.lblPowerSource.Font = New System.Drawing.Font("LCARS", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblPowerSource.ForeColor = System.Drawing.Color.FromArgb(255, 220, 40, 20)
        Me.lblPowerSource.Location = New System.Drawing.Point(6, 50)
        Me.lblPowerSource.Name = "lblPowerSource"
        Me.lblPowerSource.Size = New System.Drawing.Size(60, 18)
        Me.lblPowerSource.Text = "HoS"
        '
        'Left tactical launcher column
        '
        Me.MyComp.Location = New System.Drawing.Point(10, 132)
        Me.MyComp.Name = "MyComp"
        Me.MyComp.Size = New System.Drawing.Size(134, 34)
        Me.MyComp.Text = "De'wI'"
        Me.MyComp.ButtonText = "De'wI'"
        '
        Me.mySettings.Location = New System.Drawing.Point(10, 172)
        Me.mySettings.Name = "mySettings"
        Me.mySettings.Size = New System.Drawing.Size(134, 34)
        Me.mySettings.Text = "DuH"
        Me.mySettings.ButtonText = "DuH"
        '
        Me.myEngineering.Location = New System.Drawing.Point(10, 212)
        Me.myEngineering.Name = "myEngineering"
        Me.myEngineering.Size = New System.Drawing.Size(134, 34)
        Me.myEngineering.Text = "jonwI'"
        Me.myEngineering.ButtonText = "jonwI'"
        '
        Me.fbWebBrowser.Location = New System.Drawing.Point(10, 252)
        Me.fbWebBrowser.Name = "fbWebBrowser"
        Me.fbWebBrowser.Size = New System.Drawing.Size(134, 34)
        Me.fbWebBrowser.Text = "QumwI'"
        Me.fbWebBrowser.ButtonText = "QumwI'"
        '
        Me.fbTerminal.Location = New System.Drawing.Point(10, 292)
        Me.fbTerminal.Name = "fbTerminal"
        Me.fbTerminal.Size = New System.Drawing.Size(134, 34)
        Me.fbTerminal.Text = "ra'wI'"
        Me.fbTerminal.ButtonText = "ra'wI'"
        '
        Me.myPhoto.Location = New System.Drawing.Point(10, 332)
        Me.myPhoto.Name = "myPhoto"
        Me.myPhoto.Size = New System.Drawing.Size(134, 34)
        Me.myPhoto.Text = "HaSta"
        Me.myPhoto.ButtonText = "HaSta"
        '
        Me.myModeSelect.Location = New System.Drawing.Point(10, 382)
        Me.myModeSelect.Name = "myModeSelect"
        Me.myModeSelect.Size = New System.Drawing.Size(134, 36)
        Me.myModeSelect.Text = "Segh wIv"
        Me.myModeSelect.ButtonText = "Segh wIv"
        '
        Me.myAlert.Location = New System.Drawing.Point(10, 424)
        Me.myAlert.Name = "myAlert"
        Me.myAlert.Size = New System.Drawing.Size(102, 36)
        Me.myAlert.Text = "ghuH Doq"
        Me.myAlert.ButtonText = "ghuH Doq"
        '
        Me.myAlertListButton.ArrowDirection = LCARS.LCARSarrowDirection.Right
        Me.myAlertListButton.Location = New System.Drawing.Point(116, 424)
        Me.myAlertListButton.Name = "myAlertListButton"
        Me.myAlertListButton.Size = New System.Drawing.Size(28, 36)
        '
        Me.myDestruct.Location = New System.Drawing.Point(10, 466)
        Me.myDestruct.Name = "myDestruct"
        Me.myDestruct.Size = New System.Drawing.Size(134, 36)
        Me.myDestruct.Text = "Qaw'"
        Me.myDestruct.ButtonText = "Qaw'"
        '
        Me.myDeactivate.Location = New System.Drawing.Point(10, 508)
        Me.myDeactivate.Name = "myDeactivate"
        Me.myDeactivate.Size = New System.Drawing.Size(134, 40)
        Me.myDeactivate.Text = "mev"
        Me.myDeactivate.ButtonText = "mev"
        '
        'pnlStart
        '
        Me.pnlStart.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.pnlStart.BackColor = System.Drawing.Color.Black
        Me.pnlStart.Controls.Add(Me.pnlPrograms)
        Me.pnlStart.Controls.Add(Me.myProgsUp)
        Me.pnlStart.Controls.Add(Me.fbProgramPages)
        Me.pnlStart.Controls.Add(Me.myProgsNext)
        Me.pnlStart.Controls.Add(Me.myProgsBack)
        Me.pnlStart.Controls.Add(Me.myDocuments)
        Me.pnlStart.Controls.Add(Me.myPictures)
        Me.pnlStart.Controls.Add(Me.myMusic)
        Me.pnlStart.Controls.Add(Me.myVideos)
        Me.pnlStart.Controls.Add(Me.fbDesktop)
        Me.pnlStart.Controls.Add(Me.fbMyNetwork)
        Me.pnlStart.Controls.Add(Me.myRun)
        Me.pnlStart.Location = New System.Drawing.Point(154, 52)
        Me.pnlStart.Name = "pnlStart"
        Me.pnlStart.Size = New System.Drawing.Size(340, 662)
        Me.pnlStart.TabIndex = 12
        Me.pnlStart.Visible = False
        '
        'pnlPrograms
        '
        Me.pnlPrograms.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.pnlPrograms.Location = New System.Drawing.Point(6, 32)
        Me.pnlPrograms.Name = "pnlPrograms"
        Me.pnlPrograms.Size = New System.Drawing.Size(200, 550)
        Me.pnlPrograms.TabIndex = 1
        '
        'myProgsUp
        '
        Me.myProgsUp.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.myProgsUp.ButtonText = "chap"
        Me.myProgsUp.Location = New System.Drawing.Point(6, 588)
        Me.myProgsUp.Name = "myProgsUp"
        Me.myProgsUp.Size = New System.Drawing.Size(200, 30)
        Me.myProgsUp.Text = "chap"
        '
        'myProgsBack
        '
        Me.myProgsBack.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.myProgsBack.ArrowDirection = LCARS.LCARSarrowDirection.Left
        Me.myProgsBack.Location = New System.Drawing.Point(6, 624)
        Me.myProgsBack.Name = "myProgsBack"
        Me.myProgsBack.Size = New System.Drawing.Size(32, 30)
        '
        'fbProgramPages
        '
        Me.fbProgramPages.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.fbProgramPages.ButtonText = "1/1"
        Me.fbProgramPages.Clickable = False
        Me.fbProgramPages.Location = New System.Drawing.Point(42, 624)
        Me.fbProgramPages.Name = "fbProgramPages"
        Me.fbProgramPages.Size = New System.Drawing.Size(128, 30)
        Me.fbProgramPages.Text = "1/1"
        '
        'myProgsNext
        '
        Me.myProgsNext.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.myProgsNext.ArrowDirection = LCARS.LCARSarrowDirection.Right
        Me.myProgsNext.Location = New System.Drawing.Point(174, 624)
        Me.myProgsNext.Name = "myProgsNext"
        Me.myProgsNext.Size = New System.Drawing.Size(32, 30)
        '
        'Start Menu Right Column Buttons
        '
        Me.myDocuments.Location = New System.Drawing.Point(212, 32)
        Me.myDocuments.Name = "myDocuments"
        Me.myDocuments.Size = New System.Drawing.Size(122, 34)
        Me.myDocuments.Text = "ghItlh"
        Me.myDocuments.ButtonText = "ghItlh"
        '
        Me.myPictures.Location = New System.Drawing.Point(212, 72)
        Me.myPictures.Name = "myPictures"
        Me.myPictures.Size = New System.Drawing.Size(122, 34)
        Me.myPictures.Text = "nagh beQ"
        Me.myPictures.ButtonText = "nagh beQ"
        '
        Me.myMusic.Location = New System.Drawing.Point(212, 112)
        Me.myMusic.Name = "myMusic"
        Me.myMusic.Size = New System.Drawing.Size(122, 34)
        Me.myMusic.Text = "QoQ"
        Me.myMusic.ButtonText = "QoQ"
        '
        Me.myVideos.Location = New System.Drawing.Point(212, 152)
        Me.myVideos.Name = "myVideos"
        Me.myVideos.Size = New System.Drawing.Size(122, 34)
        Me.myVideos.Text = "jIH"
        Me.myVideos.ButtonText = "jIH"
        '
        Me.fbDesktop.Location = New System.Drawing.Point(212, 192)
        Me.fbDesktop.Name = "fbDesktop"
        Me.fbDesktop.Size = New System.Drawing.Size(122, 34)
        Me.fbDesktop.Text = "raS De'"
        Me.fbDesktop.ButtonText = "raS De'"
        '
        Me.fbMyNetwork.Location = New System.Drawing.Point(212, 232)
        Me.fbMyNetwork.Name = "fbMyNetwork"
        Me.fbMyNetwork.Size = New System.Drawing.Size(122, 34)
        Me.fbMyNetwork.Text = "Qum De'"
        Me.fbMyNetwork.ButtonText = "Qum De'"
        '
        Me.myRun.Location = New System.Drawing.Point(212, 272)
        Me.myRun.Name = "myRun"
        Me.myRun.Size = New System.Drawing.Size(122, 34)
        Me.myRun.Text = "chu'"
        Me.myRun.ButtonText = "chu'"
        '
        'gridUserButtons
        '
        Me.gridUserButtons.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.gridUserButtons.ControlAddingDirection = LCARS.Controls.ButtonGrid.ControlDirection.Horizontal
        Me.gridUserButtons.ControlSize = New System.Drawing.Size(100, 25)
        Me.gridUserButtons.Location = New System.Drawing.Point(792, 52)
        Me.gridUserButtons.MinimumSize = New System.Drawing.Size(105, 30)
        Me.gridUserButtons.Name = "gridUserButtons"
        Me.gridUserButtons.Size = New System.Drawing.Size(220, 628)
        Me.gridUserButtons.TabIndex = 13
        Me.gridUserButtons.Visible = False
        '
        'pnlMain
        '
        Me.pnlMain.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.pnlMain.BackColor = System.Drawing.Color.Black
        Me.pnlMain.Location = New System.Drawing.Point(154, 52)
        Me.pnlMain.Name = "pnlMain"
        Me.pnlMain.Size = New System.Drawing.Size(858, 662)
        Me.pnlMain.TabIndex = 14
        '
        'frmMainscreen5
        '
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None
        Me.BackColor = System.Drawing.Color.Black
        Me.ClientSize = New System.Drawing.Size(1024, 768)
        Me.Controls.Add(Me.pnlMainBar)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None
        Me.Name = "frmMainscreen5"
        Me.ShowInTaskbar = False
        Me.Text = "frmMainscreen5"
        Me.pnlMainBar.ResumeLayout(False)
        Me.pnlTray.ResumeLayout(False)
        Me.pnlStart.ResumeLayout(False)
        Me.pnlBatt.ResumeLayout(False)
        Me.pnlBatt.PerformLayout()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents pnlMainBar As System.Windows.Forms.Panel
    Friend WithEvents gridUserButtons As LCARS.Controls.ButtonGrid
    Friend WithEvents myUserButtons As LCARS.Controls.FlatButton
    Friend WithEvents myButtonManager As LCARS.Controls.FlatButton
    Friend WithEvents myStartMenu As LCARS.Controls.FlatButton
    Friend WithEvents pnlMain As System.Windows.Forms.Panel
    Friend WithEvents pnlApps As System.Windows.Forms.Panel
    Friend WithEvents pnlStart As System.Windows.Forms.Panel
    Friend WithEvents myAlert As LCARS.Controls.FlatButton
    Friend WithEvents myEngineering As LCARS.Controls.FlatButton
    Friend WithEvents myModeSelect As LCARS.Controls.FlatButton
    Friend WithEvents myDeactivate As LCARS.Controls.FlatButton
    Friend WithEvents pnlPrograms As LCARS.Controls.WindowlessContainer
    Friend WithEvents myDestruct As LCARS.Controls.FlatButton
    Friend WithEvents myProgsUp As LCARS.Controls.FlatButton
    Friend WithEvents fbProgramPages As LCARS.Controls.FlatButton
    Friend WithEvents myProgsNext As LCARS.Controls.ArrowButton
    Friend WithEvents myProgsBack As LCARS.Controls.ArrowButton
    Friend WithEvents MyComp As LCARS.Controls.FlatButton
    Friend WithEvents mySettings As LCARS.Controls.FlatButton
    Friend WithEvents myPhoto As LCARS.Controls.FlatButton
    Friend WithEvents myDocuments As LCARS.Controls.FlatButton
    Friend WithEvents myPictures As LCARS.Controls.FlatButton
    Friend WithEvents pnlBatt As System.Windows.Forms.Panel
    Friend WithEvents lblBatt As System.Windows.Forms.Label
    Friend WithEvents lblPowerSource As System.Windows.Forms.Label
    Friend WithEvents pnlTray As System.Windows.Forms.Panel
    Friend WithEvents HideTrayButton As LCARS.Controls.ArrowButton
    Friend WithEvents myOSK As LCARS.Controls.FlatButton
    Friend WithEvents mySpeech As LCARS.Controls.FlatButton
    Friend WithEvents myClock As LCARS.Controls.FlatButton
    Friend WithEvents ShowTrayButton As LCARS.Controls.FlatButton
    Friend WithEvents fbBatt10 As LCARS.Controls.FlatButton
    Friend WithEvents fbBatt9 As LCARS.Controls.FlatButton
    Friend WithEvents fbBatt8 As LCARS.Controls.FlatButton
    Friend WithEvents fbBatt7 As LCARS.Controls.FlatButton
    Friend WithEvents fbBatt6 As LCARS.Controls.FlatButton
    Friend WithEvents fbBatt5 As LCARS.Controls.FlatButton
    Friend WithEvents fbBatt4 As LCARS.Controls.FlatButton
    Friend WithEvents fbBatt3 As LCARS.Controls.FlatButton
    Friend WithEvents fbBatt2 As LCARS.Controls.FlatButton
    Friend WithEvents fbBatt1 As LCARS.Controls.FlatButton
    Friend WithEvents myVideos As LCARS.Controls.FlatButton
    Friend WithEvents fbWebBrowser As LCARS.Controls.FlatButton
    Friend WithEvents fbTerminal As LCARS.Controls.FlatButton
    Friend WithEvents fbDesktop As LCARS.Controls.FlatButton
    Friend WithEvents fbMyNetwork As LCARS.Controls.FlatButton
    Friend WithEvents myHelp As LCARS.Controls.FlatButton
    Friend WithEvents myRun As LCARS.Controls.FlatButton
    Friend WithEvents myMusic As LCARS.Controls.FlatButton
    Friend WithEvents myAlertListButton As LCARS.Controls.ArrowButton
End Class
