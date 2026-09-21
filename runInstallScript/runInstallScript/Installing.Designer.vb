<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Installing
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
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

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    ' Plain WinForms only — LCARS custom controls crash during elevated startup
    ' when the LCARS font / SoundPlayer path is unavailable to the admin token.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.lblTitle = New System.Windows.Forms.Label()
        Me.lblMessage = New System.Windows.Forms.Label()
        Me.pnlInstalling = New System.Windows.Forms.Panel()
        Me.Progress = New System.Windows.Forms.ProgressBar()
        Me.lblProgress = New System.Windows.Forms.Label()
        Me.lstStatus = New System.Windows.Forms.ListBox()
        Me.sbCancel = New System.Windows.Forms.Button()
        Me.sbContinue = New System.Windows.Forms.Button()
        Me.pnlFinishMenu = New System.Windows.Forms.Panel()
        Me.lblFinishMenuTitle = New System.Windows.Forms.Label()
        Me.btnFinished = New System.Windows.Forms.Button()
        Me.btnHibernate = New System.Windows.Forms.Button()
        Me.btnSuspend = New System.Windows.Forms.Button()
        Me.btnLock = New System.Windows.Forms.Button()
        Me.btnLogOff = New System.Windows.Forms.Button()
        Me.btnRestart = New System.Windows.Forms.Button()
        Me.btnShutDown = New System.Windows.Forms.Button()
        Me.btnCloseLcars = New System.Windows.Forms.Button()
        Me.pnlInstalling.SuspendLayout()
        Me.pnlFinishMenu.SuspendLayout()
        Me.SuspendLayout()
        '
        'lblTitle
        '
        Me.lblTitle.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblTitle.Font = New System.Drawing.Font("Segoe UI", 16.0!, System.Drawing.FontStyle.Bold)
        Me.lblTitle.ForeColor = System.Drawing.Color.Orange
        Me.lblTitle.Location = New System.Drawing.Point(12, 12)
        Me.lblTitle.Name = "lblTitle"
        Me.lblTitle.Size = New System.Drawing.Size(524, 32)
        Me.lblTitle.TabIndex = 0
        Me.lblTitle.Text = "INSTALLING UPDATES"
        '
        'lblMessage
        '
        Me.lblMessage.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblMessage.Font = New System.Drawing.Font("Segoe UI", 11.0!)
        Me.lblMessage.ForeColor = System.Drawing.Color.Orange
        Me.lblMessage.Location = New System.Drawing.Point(12, 56)
        Me.lblMessage.Name = "lblMessage"
        Me.lblMessage.Size = New System.Drawing.Size(524, 200)
        Me.lblMessage.TabIndex = 1
        Me.lblMessage.Text = "Updates are ready to install." & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "Please exit all LCARS programs and press CONTINUE."
        '
        'pnlInstalling
        '
        Me.pnlInstalling.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.pnlInstalling.Controls.Add(Me.Progress)
        Me.pnlInstalling.Controls.Add(Me.lblProgress)
        Me.pnlInstalling.Controls.Add(Me.lstStatus)
        Me.pnlInstalling.Location = New System.Drawing.Point(12, 56)
        Me.pnlInstalling.Name = "pnlInstalling"
        Me.pnlInstalling.Size = New System.Drawing.Size(524, 280)
        Me.pnlInstalling.TabIndex = 3
        Me.pnlInstalling.Visible = False
        '
        'Progress
        '
        Me.Progress.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Progress.Location = New System.Drawing.Point(3, 8)
        Me.Progress.Maximum = 100
        Me.Progress.Name = "Progress"
        Me.Progress.Size = New System.Drawing.Size(518, 28)
        Me.Progress.TabIndex = 1
        '
        'lblProgress
        '
        Me.lblProgress.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblProgress.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblProgress.ForeColor = System.Drawing.Color.Orange
        Me.lblProgress.Location = New System.Drawing.Point(3, 42)
        Me.lblProgress.Name = "lblProgress"
        Me.lblProgress.Size = New System.Drawing.Size(518, 22)
        Me.lblProgress.TabIndex = 2
        Me.lblProgress.Text = "0% complete"
        '
        'lstStatus
        '
        Me.lstStatus.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lstStatus.BackColor = System.Drawing.Color.Black
        Me.lstStatus.ForeColor = System.Drawing.Color.Orange
        Me.lstStatus.FormattingEnabled = True
        Me.lstStatus.ItemHeight = 18
        Me.lstStatus.Location = New System.Drawing.Point(3, 70)
        Me.lstStatus.Name = "lstStatus"
        Me.lstStatus.Size = New System.Drawing.Size(518, 202)
        Me.lstStatus.TabIndex = 0
        '
        'sbCancel
        '
        Me.sbCancel.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.sbCancel.BackColor = System.Drawing.Color.FromArgb(CType(CType(120, Byte), Integer), CType(CType(0, Byte), Integer), CType(CType(0, Byte), Integer))
        Me.sbCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.sbCancel.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Bold)
        Me.sbCancel.ForeColor = System.Drawing.Color.Orange
        Me.sbCancel.Location = New System.Drawing.Point(266, 350)
        Me.sbCancel.Name = "sbCancel"
        Me.sbCancel.Size = New System.Drawing.Size(132, 36)
        Me.sbCancel.TabIndex = 2
        Me.sbCancel.Text = "CANCEL"
        Me.sbCancel.UseVisualStyleBackColor = False
        '
        'sbContinue
        '
        Me.sbContinue.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.sbContinue.BackColor = System.Drawing.Color.FromArgb(CType(CType(51, Byte), Integer), CType(CType(102, Byte), Integer), CType(CType(204, Byte), Integer))
        Me.sbContinue.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.sbContinue.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Bold)
        Me.sbContinue.ForeColor = System.Drawing.Color.Black
        Me.sbContinue.Location = New System.Drawing.Point(404, 350)
        Me.sbContinue.Name = "sbContinue"
        Me.sbContinue.Size = New System.Drawing.Size(132, 36)
        Me.sbContinue.TabIndex = 2
        Me.sbContinue.Text = "CONTINUE"
        Me.sbContinue.UseVisualStyleBackColor = False
        '
        'pnlFinishMenu
        '
        Me.pnlFinishMenu.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.pnlFinishMenu.BackColor = System.Drawing.Color.Black
        Me.pnlFinishMenu.BorderStyle = System.Windows.Forms.BorderStyle.None
        Me.pnlFinishMenu.Controls.Add(Me.lblFinishMenuTitle)
        Me.pnlFinishMenu.Controls.Add(Me.btnHibernate)
        Me.pnlFinishMenu.Controls.Add(Me.btnSuspend)
        Me.pnlFinishMenu.Controls.Add(Me.btnLock)
        Me.pnlFinishMenu.Controls.Add(Me.btnLogOff)
        Me.pnlFinishMenu.Controls.Add(Me.btnRestart)
        Me.pnlFinishMenu.Controls.Add(Me.btnShutDown)
        Me.pnlFinishMenu.Controls.Add(Me.btnCloseLcars)
        Me.pnlFinishMenu.Controls.Add(Me.btnFinished)
        Me.pnlFinishMenu.Location = New System.Drawing.Point(280, 340)
        Me.pnlFinishMenu.Name = "pnlFinishMenu"
        Me.pnlFinishMenu.Size = New System.Drawing.Size(268, 420)
        Me.pnlFinishMenu.TabIndex = 10
        Me.pnlFinishMenu.Visible = False
        '
        'lblFinishMenuTitle
        '
        Me.lblFinishMenuTitle.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.lblFinishMenuTitle.ForeColor = System.Drawing.Color.FromArgb(CType(CType(255, Byte), Integer), CType(CType(153, Byte), Integer), CType(CType(0, Byte), Integer))
        Me.lblFinishMenuTitle.Location = New System.Drawing.Point(28, 28)
        Me.lblFinishMenuTitle.Name = "lblFinishMenuTitle"
        Me.lblFinishMenuTitle.Size = New System.Drawing.Size(220, 22)
        Me.lblFinishMenuTitle.TabIndex = 0
        Me.lblFinishMenuTitle.Text = "SYSTEM OPTIONS"
        '
        'btnFinished — primary / default (taller LCARS choice)
        '
        Me.btnFinished.BackColor = System.Drawing.Color.FromArgb(CType(CType(51, Byte), Integer), CType(CType(102, Byte), Integer), CType(CType(204, Byte), Integer))
        Me.btnFinished.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnFinished.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Bold)
        Me.btnFinished.ForeColor = System.Drawing.Color.Black
        Me.btnFinished.Location = New System.Drawing.Point(28, 340)
        Me.btnFinished.Name = "btnFinished"
        Me.btnFinished.Size = New System.Drawing.Size(220, 64)
        Me.btnFinished.TabIndex = 8
        Me.btnFinished.Text = ""
        Me.btnFinished.UseVisualStyleBackColor = False
        '
        'btnHibernate
        '
        Me.btnHibernate.BackColor = System.Drawing.Color.FromArgb(CType(CType(153, Byte), Integer), CType(CType(102, Byte), Integer), CType(CType(204, Byte), Integer))
        Me.btnHibernate.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnHibernate.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.btnHibernate.ForeColor = System.Drawing.Color.Black
        Me.btnHibernate.Location = New System.Drawing.Point(10, 32)
        Me.btnHibernate.Name = "btnHibernate"
        Me.btnHibernate.Size = New System.Drawing.Size(196, 32)
        Me.btnHibernate.TabIndex = 1
        Me.btnHibernate.Text = "HIBERNATE"
        Me.btnHibernate.UseVisualStyleBackColor = False
        '
        'btnSuspend
        '
        Me.btnSuspend.BackColor = System.Drawing.Color.FromArgb(CType(CType(153, Byte), Integer), CType(CType(102, Byte), Integer), CType(CType(204, Byte), Integer))
        Me.btnSuspend.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnSuspend.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.btnSuspend.ForeColor = System.Drawing.Color.Black
        Me.btnSuspend.Location = New System.Drawing.Point(10, 70)
        Me.btnSuspend.Name = "btnSuspend"
        Me.btnSuspend.Size = New System.Drawing.Size(196, 32)
        Me.btnSuspend.TabIndex = 2
        Me.btnSuspend.Text = "SUSPEND"
        Me.btnSuspend.UseVisualStyleBackColor = False
        '
        'btnLock
        '
        Me.btnLock.BackColor = System.Drawing.Color.FromArgb(CType(CType(255, Byte), Integer), CType(CType(153, Byte), Integer), CType(CType(0, Byte), Integer))
        Me.btnLock.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnLock.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.btnLock.ForeColor = System.Drawing.Color.Black
        Me.btnLock.Location = New System.Drawing.Point(10, 108)
        Me.btnLock.Name = "btnLock"
        Me.btnLock.Size = New System.Drawing.Size(196, 32)
        Me.btnLock.TabIndex = 3
        Me.btnLock.Text = "LOCK"
        Me.btnLock.UseVisualStyleBackColor = False
        '
        'btnLogOff
        '
        Me.btnLogOff.BackColor = System.Drawing.Color.FromArgb(CType(CType(255, Byte), Integer), CType(CType(153, Byte), Integer), CType(CType(0, Byte), Integer))
        Me.btnLogOff.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnLogOff.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.btnLogOff.ForeColor = System.Drawing.Color.Black
        Me.btnLogOff.Location = New System.Drawing.Point(10, 146)
        Me.btnLogOff.Name = "btnLogOff"
        Me.btnLogOff.Size = New System.Drawing.Size(196, 32)
        Me.btnLogOff.TabIndex = 4
        Me.btnLogOff.Text = "LOG OFF"
        Me.btnLogOff.UseVisualStyleBackColor = False
        '
        'btnRestart
        '
        Me.btnRestart.BackColor = System.Drawing.Color.FromArgb(CType(CType(204, Byte), Integer), CType(CType(51, Byte), Integer), CType(CType(51, Byte), Integer))
        Me.btnRestart.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnRestart.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.btnRestart.ForeColor = System.Drawing.Color.Black
        Me.btnRestart.Location = New System.Drawing.Point(10, 184)
        Me.btnRestart.Name = "btnRestart"
        Me.btnRestart.Size = New System.Drawing.Size(196, 32)
        Me.btnRestart.TabIndex = 5
        Me.btnRestart.Text = "RESTART"
        Me.btnRestart.UseVisualStyleBackColor = False
        '
        'btnShutDown
        '
        Me.btnShutDown.BackColor = System.Drawing.Color.FromArgb(CType(CType(204, Byte), Integer), CType(CType(51, Byte), Integer), CType(CType(51, Byte), Integer))
        Me.btnShutDown.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnShutDown.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.btnShutDown.ForeColor = System.Drawing.Color.Black
        Me.btnShutDown.Location = New System.Drawing.Point(10, 222)
        Me.btnShutDown.Name = "btnShutDown"
        Me.btnShutDown.Size = New System.Drawing.Size(196, 32)
        Me.btnShutDown.TabIndex = 6
        Me.btnShutDown.Text = "SHUT DOWN"
        Me.btnShutDown.UseVisualStyleBackColor = False
        '
        'btnCloseLcars
        '
        Me.btnCloseLcars.BackColor = System.Drawing.Color.FromArgb(CType(CType(120, Byte), Integer), CType(CType(0, Byte), Integer), CType(CType(0, Byte), Integer))
        Me.btnCloseLcars.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnCloseLcars.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.btnCloseLcars.ForeColor = System.Drawing.Color.Orange
        Me.btnCloseLcars.Location = New System.Drawing.Point(10, 260)
        Me.btnCloseLcars.Name = "btnCloseLcars"
        Me.btnCloseLcars.Size = New System.Drawing.Size(196, 32)
        Me.btnCloseLcars.TabIndex = 7
        Me.btnCloseLcars.Text = "CLOSE LCARS"
        Me.btnCloseLcars.UseVisualStyleBackColor = False
        '
        'Installing
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackColor = System.Drawing.Color.Black
        Me.ClientSize = New System.Drawing.Size(548, 400)
        Me.Controls.Add(Me.pnlFinishMenu)
        Me.Controls.Add(Me.pnlInstalling)
        Me.Controls.Add(Me.sbCancel)
        Me.Controls.Add(Me.sbContinue)
        Me.Controls.Add(Me.lblMessage)
        Me.Controls.Add(Me.lblTitle)
        Me.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.ForeColor = System.Drawing.Color.Orange
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None
        Me.Name = "Installing"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Installing Updates"
        Me.TopMost = True
        Me.pnlInstalling.ResumeLayout(False)
        Me.pnlFinishMenu.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents lblTitle As System.Windows.Forms.Label
    Friend WithEvents lblMessage As System.Windows.Forms.Label
    Friend WithEvents sbContinue As System.Windows.Forms.Button
    Friend WithEvents sbCancel As System.Windows.Forms.Button
    Friend WithEvents pnlInstalling As System.Windows.Forms.Panel
    Friend WithEvents Progress As System.Windows.Forms.ProgressBar
    Friend WithEvents lblProgress As System.Windows.Forms.Label
    Friend WithEvents lstStatus As System.Windows.Forms.ListBox
    Friend WithEvents pnlFinishMenu As System.Windows.Forms.Panel
    Friend WithEvents lblFinishMenuTitle As System.Windows.Forms.Label
    Friend WithEvents btnFinished As System.Windows.Forms.Button
    Friend WithEvents btnHibernate As System.Windows.Forms.Button
    Friend WithEvents btnSuspend As System.Windows.Forms.Button
    Friend WithEvents btnLock As System.Windows.Forms.Button
    Friend WithEvents btnLogOff As System.Windows.Forms.Button
    Friend WithEvents btnRestart As System.Windows.Forms.Button
    Friend WithEvents btnShutDown As System.Windows.Forms.Button
    Friend WithEvents btnCloseLcars As System.Windows.Forms.Button

End Class
