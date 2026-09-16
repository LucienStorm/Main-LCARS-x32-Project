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
        Me.pnlInstalling.SuspendLayout()
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
        'Installing
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackColor = System.Drawing.Color.Black
        Me.ClientSize = New System.Drawing.Size(548, 400)
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

End Class
