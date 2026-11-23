Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Text
Imports System.Windows.Forms
Imports LCARS
Imports LCARS.Controls
    Public Class frmFilters
        Inherits LCARSForm
        Private Const MarginX As Integer = 12

        Private Const TopChrome As Integer = 44

        Private Const BottomBar As Integer = 40

        Private fbTitle As FlatButton

        Private fbImport As FlatButton

        Private fbExport As FlatButton

        Private fbNew As FlatButton

        Private fbEdit As FlatButton

        Private fbDelete As FlatButton

        Private fbToggle As FlatButton

        Private fbRunNow As FlatButton

        Private fbClose As FlatButton

        Private lstRules As ListBox

        Private lblDetail As Label

        Private lblStatus As Label

        Private rules As List(Of MailFilterRule)

        Private ReadOnly runAccount As MailAccount

        Private ReadOnly runPassword As String

        Private ReadOnly runFolder As String

        Private ReadOnly runMailService As ImapMailService

        Private ReadOnly afterRunCallback As Action

        Public Sub New()
            Me.New(Nothing, Nothing, Nothing, Nothing, Nothing)
        End Sub

        Public Sub New(account As MailAccount, password As String, folderFullName As String, mailService As ImapMailService, refreshCallback As Action)
            runAccount = account
            runPassword = If(Not Equals(password, Nothing), password, "")
            runFolder = If(Not Equals(folderFullName, Nothing), folderFullName, "")
            runMailService = mailService
            afterRunCallback = refreshCallback
            CType(CObj(Me), Form).Text = "LCARS MAIL FILTERS"
            CType(CObj(Me), Form).BackColor = Color.Black
            Dim clientSize As Size = New Size(720, 480)
            CType(CObj(Me), Form).ClientSize = clientSize
            CType(CObj(Me), Form).StartPosition = FormStartPosition.CenterParent
            rules = MailFilterStore.LoadRules()
            BuildChrome()
            RefreshList()
        End Sub

        Private Sub BuildChrome()
            fbTitle = New FlatButton()
            CType(fbTitle, LCARSbuttonClass).ButtonText = "FILTERS"
            CType(fbTitle, LCARSbuttonClass).Text = "FILTERS"
            CType(fbTitle, LCARSbuttonClass).Clickable = False
            CType(fbTitle, LCARSbuttonClass).Color = CType(8, LCARScolorStyles)
            Dim obj As FlatButton = fbTitle
            Dim size As Size = New Size(120, 32)
            CType(CObj(obj), Control).Size = size
            Dim obj2 As FlatButton = fbTitle
            Dim location As Point = New Point(12, 8)
            CType(CObj(obj2), Control).Location = location
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbTitle), Control))
            Dim x As Integer = CType(CObj(fbTitle), Control).Right + 8
            fbNew = Me.MakeButton("NEW", CType(6, LCARScolorStyles), 72)
            Dim obj3 As FlatButton = fbNew
            location = New Point(x, 10)
            CType(CObj(obj3), Control).Location = location
            AddHandler CType(CObj(fbNew), Control).Click, AddressOf OnNewClick
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbNew), Control))
            x = CType(CObj(fbNew), Control).Right + 6
            fbEdit = Me.MakeButton("EDIT", CType(2, LCARScolorStyles), 72)
            Dim obj4 As FlatButton = fbEdit
            location = New Point(x, 10)
            CType(CObj(obj4), Control).Location = location
            AddHandler CType(CObj(fbEdit), Control).Click, AddressOf OnEditClick
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbEdit), Control))
            x = CType(CObj(fbEdit), Control).Right + 6
            fbDelete = Me.MakeButton("DELETE", CType(9, LCARScolorStyles), 80)
            Dim obj5 As FlatButton = fbDelete
            location = New Point(x, 10)
            CType(CObj(obj5), Control).Location = location
            AddHandler CType(CObj(fbDelete), Control).Click, AddressOf OnDeleteClick
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbDelete), Control))
            x = CType(CObj(fbDelete), Control).Right + 6
            fbToggle = Me.MakeButton("TOGGLE", CType(1, LCARScolorStyles), 88)
            Dim obj6 As FlatButton = fbToggle
            location = New Point(x, 10)
            CType(CObj(obj6), Control).Location = location
            AddHandler CType(CObj(fbToggle), Control).Click, AddressOf OnToggleClick
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbToggle), Control))
            x = CType(CObj(fbToggle), Control).Right + 6
            fbImport = Me.MakeButton("IMPORT", CType(6, LCARScolorStyles), 88)
            Dim obj7 As FlatButton = fbImport
            location = New Point(x, 10)
            CType(CObj(obj7), Control).Location = location
            AddHandler CType(CObj(fbImport), Control).Click, AddressOf OnImportClick
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbImport), Control))
            x = CType(CObj(fbImport), Control).Right + 6
            fbExport = Me.MakeButton("EXPORT", CType(8, LCARScolorStyles), 88)
            Dim obj8 As FlatButton = fbExport
            location = New Point(x, 10)
            CType(CObj(obj8), Control).Location = location
            AddHandler CType(CObj(fbExport), Control).Click, AddressOf OnExportClick
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbExport), Control))
            fbRunNow = Me.MakeButton("RUN NOW", CType(6, LCARScolorStyles), 100)
            AddHandler CType(CObj(fbRunNow), Control).Click, AddressOf OnRunNowClick
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbRunNow), Control))
            fbClose = Me.MakeButton("CLOSE", CType(9, LCARScolorStyles), 100)
            AddHandler CType(CObj(fbClose), Control).Click, Sub(s, e) Call CType(CObj(Me), Form).Close()
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbClose), Control))
            lstRules = New ListBox()
            lstRules.BackColor = Color.FromArgb(30, 30, 30)
            lstRules.ForeColor = Color.Orange
            lstRules.BorderStyle = BorderStyle.FixedSingle
            lstRules.Font = New Font("Arial Narrow", 11F)
            Dim listBox = lstRules
            location = New Point(12, 44)
            listBox.Location = location
            Dim listBox2 = lstRules
            size = New Size(696, 280)
            listBox2.Size = size
            AddHandler lstRules.SelectedIndexChanged, AddressOf OnRuleSelected
            CType(CObj(Me), Control).Controls.Add(lstRules)
            lblDetail = New Label()
            lblDetail.ForeColor = Color.Orange
            lblDetail.BackColor = Color.Black
            lblDetail.Font = New Font("Arial Narrow", 10F)
            Dim label = lblDetail
            location = New Point(12, 332)
            label.Location = location
            Dim label2 = lblDetail
            size = New Size(696, 120)
            label2.Size = size
            CType(CObj(Me), Control).Controls.Add(lblDetail)
            lblStatus = New Label()
            lblStatus.ForeColor = Color.Orange
            lblStatus.BackColor = Color.Black
            lblStatus.Font = New Font("Arial Narrow", 10F)
            lblStatus.Text = rules.Count.ToString() & " RULE(S) LOADED"
            lblStatus.AutoSize = False
            lblStatus.TextAlign = ContentAlignment.MiddleLeft
            CType(CObj(Me), Control).Controls.Add(lblStatus)
            AddHandler CType(CObj(Me), Control).Resize, AddressOf OnFormResize
            LayoutChrome()
        End Sub

        Private Sub OnFormResize(sender As Object, e As EventArgs)
            LayoutChrome()
        End Sub

        Private Sub LayoutChrome()
            Dim num = Math.Max(400, CType(CObj(Me), Form).ClientSize.Width)
            Dim num2 = Math.Max(320, CType(CObj(Me), Form).ClientSize.Height)
            Dim obj As FlatButton = fbClose
            Dim location As Point = New Point(num - 12 - CType(CObj(fbClose), Control).Width, 10)
            CType(CObj(obj), Control).Location = location
            Dim obj2 As FlatButton = fbRunNow
            location = New Point(CType(CObj(fbClose), Control).Left - 8 - CType(CObj(fbRunNow), Control).Width, 10)
            CType(CObj(obj2), Control).Location = location
            lstRules.Width = num - 24
            lstRules.Height = Math.Max(120, num2 - 44 - 40 - 140)
            Dim label = lblDetail
            location = New Point(12, lstRules.Bottom + 8)
            label.Location = location
            lblDetail.Width = lstRules.Width
            lblDetail.Height = Math.Max(60, num2 - lblDetail.Top - 40 - 8)
            lblStatus.SetBounds(12, num2 - 40, lstRules.Width, 32)
        End Sub

        Private Function MakeButton(caption As String, color As LCARScolorStyles, width As Integer) As FlatButton
            Dim val As FlatButton = New FlatButton()
            CType(val, LCARSbuttonClass).ButtonText = caption
            CType(val, LCARSbuttonClass).Text = caption
            CType(val, LCARSbuttonClass).Color = color
            Dim size As Size = New Size(width, 28)
            CType(CObj(val), Control).Size = size
            Return val
        End Function

        Private Sub PersistRules()
            If Not MailFilterStore.SaveRules(rules) Then
                lblStatus.Text = If(Not Equals(MailFilterStore.LastError, Nothing), MailFilterStore.LastError, "SAVE FAILED").ToUpperInvariant()
            End If
        End Sub

        Private Sub RefreshList()
            lstRules.Items.Clear()
            If rules Is Nothing Then
                rules = New List(Of MailFilterRule)()
            End If
            For Each rule In rules
                If rule IsNot Nothing Then
                    Dim text = If(rule.Enabled, "", "[OFF] ")
                    Dim items = lstRules.Items
                    Dim mailFilterRule = rule
                    items.Add(text & If(Not Equals(mailFilterRule.Name, Nothing), mailFilterRule.Name, "(unnamed)"))
                End If
            Next
            lblStatus.Text = rules.Count.ToString() & " RULE(S) LOADED"
            lblDetail.Text = "SELECT A RULE"
        End Sub

        Private Function SelectedRuleIndex() As Integer
            Return lstRules.SelectedIndex
        End Function

        Private Sub OnRuleSelected(sender As Object, e As EventArgs)
            Dim num As Integer = SelectedRuleIndex()
            If num < 0 OrElse rules Is Nothing OrElse num >= rules.Count Then
                Return
            End If
            Dim mailFilterRule = rules(num)
            If mailFilterRule Is Nothing Then
                Return
            End If
            Dim stringBuilder As StringBuilder = New StringBuilder()
            stringBuilder.AppendLine("ENABLED: " & mailFilterRule.Enabled.ToString().ToUpperInvariant())
            Dim mailFilterRule2 = mailFilterRule
            stringBuilder.AppendLine("LOGIC: " & If(Not Equals(mailFilterRule2.ConditionLogic, Nothing), mailFilterRule2.ConditionLogic, "AND"))
            stringBuilder.AppendLine("CONDITIONS:")
            For Each condition In mailFilterRule.Conditions
                Dim array = New String(5) {"  • ", Nothing, Nothing, Nothing, Nothing, Nothing}
                Dim mailFilterCondition = condition
                array(1) = If(Not Equals(mailFilterCondition.Field, Nothing), mailFilterCondition.Field, "")
                array(2) = " "
                Dim mailFilterCondition2 = condition
                array(3) = If(Not Equals(mailFilterCondition2.Operator, Nothing), mailFilterCondition2.Operator, "")
                array(4) = " "
                Dim mailFilterCondition3 = condition
                array(5) = If(Not Equals(mailFilterCondition3.Value, Nothing), mailFilterCondition3.Value, "")
                stringBuilder.AppendLine(String.Concat(array))
            Next
            stringBuilder.AppendLine("ACTIONS:")
            For Each action In mailFilterRule.Actions
                Dim mailFilterAction = action
                Dim obj = If(Not Equals(mailFilterAction.ActionName, Nothing), mailFilterAction.ActionName, "")
                Dim mailFilterAction2 = action
                stringBuilder.AppendLine("  • " & obj & " " & If(Not Equals(mailFilterAction2.ActionValue, Nothing), mailFilterAction2.ActionValue, ""))
            Next
            lblDetail.Text = stringBuilder.ToString()
        End Sub

        Private Sub OnNewClick(sender As Object, e As EventArgs)
            Dim frmFilterEdit2 As frmFilterEdit = New frmFilterEdit(New MailFilterRule(), createNew:=True)
            Try
                If CType(CObj(frmFilterEdit2), Form).ShowDialog(CType(Me, IWin32Window)) = DialogResult.OK Then
                    rules.Add(frmFilterEdit2.EditedRule)
                    PersistRules()
                    RefreshList()
                End If

            Finally
                If frmFilterEdit2 IsNot Nothing Then CType(frmFilterEdit2, IDisposable).Dispose()
            End Try
        End Sub

        Private Sub OnEditClick(sender As Object, e As EventArgs)
            Dim num As Integer = SelectedRuleIndex()
            If num < 0 Then
                Return
            End If
            Dim frmFilterEdit2 As frmFilterEdit = New frmFilterEdit(rules(num), createNew:=False)
            Try
                If CType(CObj(frmFilterEdit2), Form).ShowDialog(CType(Me, IWin32Window)) = DialogResult.OK Then
                    rules(num) = frmFilterEdit2.EditedRule
                    PersistRules()
                    RefreshList()
                End If

            Finally
                If frmFilterEdit2 IsNot Nothing Then CType(frmFilterEdit2, IDisposable).Dispose()
            End Try
        End Sub

        Private Sub OnDeleteClick(sender As Object, e As EventArgs)
            Dim num As Integer = SelectedRuleIndex()
            If num >= 0 Then
                rules.RemoveAt(num)
                PersistRules()
                RefreshList()
            End If
        End Sub

        Private Sub OnToggleClick(sender As Object, e As EventArgs)
            Dim num As Integer = SelectedRuleIndex()
            If num >= 0 Then
                rules(num).Enabled = Not rules(num).Enabled
                PersistRules()
                RefreshList()
                lstRules.SelectedIndex = num
            End If
        End Sub

        Private Sub OnImportClick(sender As Object, e As EventArgs)
            Dim text = LcarsCommsUi.PickFolder(CType(Me, IWin32Window), "THUNDERBIRD PROFILE (MSGFILTERRULES.DAT)")
            If Not String.IsNullOrEmpty(text) Then
                Dim num = MailFilterStore.TryImportFromProfile(text)
                If num < 0 Then
                    Dim text2 As String = If(Not Equals(MailFilterStore.LastError, Nothing), MailFilterStore.LastError, "IMPORT FAILED").ToUpperInvariant()
                    lblStatus.Text = text2
                    LcarsCommsUi.Alert(text2, "LCARS COMMS — FILTER IMPORT")
                Else
                    rules = MailFilterStore.LoadRules()
                    RefreshList()
                    lblStatus.Text = "IMPORTED " & num.ToString() & " RULE(S)"
                End If
            End If
        End Sub

        Private Sub OnExportClick(sender As Object, e As EventArgs)
            Dim saveFileDialog As SaveFileDialog = New SaveFileDialog()
            saveFileDialog.Filter = "Thunderbird filters|msgFilterRules.dat|All|*.*"
            saveFileDialog.FileName = "msgFilterRules.dat"
            If saveFileDialog.ShowDialog(CType(Me, IWin32Window)) = DialogResult.OK Then
                If MailFilterThunderbirdExporter.ExportToFile(rules, saveFileDialog.FileName) Then
                    lblStatus.Text = "EXPORTED"
                Else
                    lblStatus.Text = "EXPORT FAILED"
                End If
            End If
        End Sub

        Private Sub OnRunNowClick(sender As Object, e As EventArgs)
            If runAccount Is Nothing OrElse runMailService Is Nothing OrElse String.IsNullOrWhiteSpace(runFolder) Then
                lblStatus.Text = "SELECT A FOLDER IN MAIL FIRST"
                Return
            End If
            Dim headers = runMailService.SyncHeaders(runAccount, runPassword, runFolder)
            Dim mailFilterRunSummary = MailFilterEngine.ApplyRulesToFolder(runAccount, runPassword, runFolder, headers, runMailService, rules)
            lblStatus.Text = mailFilterRunSummary.ActionsApplied.ToString() & " ACTION(S) — " & mailFilterRunSummary.MessagesMatched.ToString() & " MATCH(ES)"
            If afterRunCallback IsNot Nothing Then
                afterRunCallback()
            End If
        End Sub
    End Class

