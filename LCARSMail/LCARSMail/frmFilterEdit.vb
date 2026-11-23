Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports LCARS
Imports LCARS.Controls
    Public Class frmFilterEdit
        Inherits LCARSForm
        Private Const MarginX As Integer = 12

        Private Const FieldH As Integer = 24

        Private Const LabelW As Integer = 88

        Private ReadOnly workingRule As MailFilterRule

        Private ReadOnly isNewRule As Boolean

        Private txtName As TextBox

        Private chkEnabled As CheckBox

        Private chkRunIncoming As CheckBox

        Private cboLogic As ComboBox

        Private lstConditions As ListBox

        Private lstActions As ListBox

        Private fbOk As FlatButton

        Private fbCancel As FlatButton

        Public ReadOnly Property EditedRule As MailFilterRule
            Get
                Return workingRule
            End Get
        End Property

        Public Sub New(rule As MailFilterRule, createNew As Boolean)
            workingRule = CloneRule(rule)
            isNewRule = createNew
            CType(CObj(Me), Form).Text = If(createNew, "NEW FILTER", "EDIT FILTER")
            CType(CObj(Me), Form).BackColor = Color.Black
            Dim clientSize As Size = New Size(640, 520)
            CType(CObj(Me), Form).ClientSize = clientSize
            CType(CObj(Me), Form).StartPosition = FormStartPosition.CenterParent
            BuildUi()
            LoadFromRule()
        End Sub

        Private Shared Function CloneRule(rule As MailFilterRule) As MailFilterRule
            If rule Is Nothing Then
                rule = New MailFilterRule()
            End If
            Dim mailFilterRule As MailFilterRule = New MailFilterRule()
            mailFilterRule.Name = rule.Name
            mailFilterRule.Enabled = rule.Enabled
            mailFilterRule.RunOnIncoming = rule.RunOnIncoming
            mailFilterRule.ConditionLogic = rule.ConditionLogic
            Dim mailFilterRule2 = mailFilterRule
            For Each condition In rule.Conditions
                mailFilterRule2.Conditions.Add(New MailFilterCondition With {
        .Field = condition.Field,
        .[Operator] = condition.Operator,
        .Value = condition.Value
    })
            Next
            For Each action In rule.Actions
                mailFilterRule2.Actions.Add(New MailFilterAction With {
        .ActionName = action.ActionName,
        .ActionValue = action.ActionValue
    })
            Next
            Return mailFilterRule2
        End Function

        Private Sub BuildUi()
            Dim num = 8
            AddLabel("NAME", 12, num + 4)
            txtName = AddText(108, num, 520)
            num += 30
            chkEnabled = New CheckBox()
            chkEnabled.Text = "ENABLED"
            chkEnabled.ForeColor = Color.Orange
            chkEnabled.BackColor = Color.Black
            Dim checkBox = chkEnabled
            Dim location As Point = New Point(108, num)
            checkBox.Location = location
            chkEnabled.AutoSize = True
            CType(CObj(Me), Control).Controls.Add(chkEnabled)
            chkRunIncoming = New CheckBox()
            chkRunIncoming.Text = "RUN ON INCOMING"
            chkRunIncoming.ForeColor = Color.Orange
            chkRunIncoming.BackColor = Color.Black
            Dim checkBox2 = chkRunIncoming
            location = New Point(280, num)
            checkBox2.Location = location
            chkRunIncoming.AutoSize = True
            CType(CObj(Me), Control).Controls.Add(chkRunIncoming)
            Dim label As Label = New Label()
            label.Text = "CONDITION FIELD: body · subject · from · to (body uses IMAP fetch)"
            label.ForeColor = Color.Orange
            label.BackColor = Color.Black
            label.AutoSize = True
            location = New Point(108, num + 28)
            label.Location = location
            CType(CObj(Me), Control).Controls.Add(label)
            num += 32
            AddLabel("LOGIC", 12, num + 4)
            cboLogic = New ComboBox()
            cboLogic.DropDownStyle = ComboBoxStyle.DropDownList
            cboLogic.Items.Add("AND")
            cboLogic.Items.Add("OR")
            Dim comboBox = cboLogic
            location = New Point(108, num)
            comboBox.Location = location
            Dim comboBox2 = cboLogic
            Dim size As Size = New Size(80, 24)
            comboBox2.Size = size
            cboLogic.BackColor = Color.FromArgb(40, 40, 40)
            cboLogic.ForeColor = Color.Orange
            CType(CObj(Me), Control).Controls.Add(cboLogic)
            num += 34
            AddLabel("CONDITIONS", 12, num)
            lstConditions = New ListBox()
            Dim listBox = lstConditions
            location = New Point(108, num)
            listBox.Location = location
            Dim listBox2 = lstConditions
            size = New Size(520, 100)
            listBox2.Size = size
            lstConditions.BackColor = Color.FromArgb(30, 30, 30)
            lstConditions.ForeColor = Color.Orange
            CType(CObj(Me), Control).Controls.Add(lstConditions)
            Dim value As FlatButton = MakeBtn("ADD", 12, num + 104, New EventHandler(AddressOf OnAddCondition))
            Dim value2 As FlatButton = MakeBtn("EDIT", 102, num + 104, New EventHandler(AddressOf OnEditCondition))
            Dim value3 As FlatButton = MakeBtn("DEL", 192, num + 104, New EventHandler(AddressOf OnDeleteCondition))
            CType(CObj(Me), Control).Controls.Add(CType(CObj(value), Control))
            CType(CObj(Me), Control).Controls.Add(CType(CObj(value2), Control))
            CType(CObj(Me), Control).Controls.Add(CType(CObj(value3), Control))
            num += 140
            AddLabel("ACTIONS", 12, num)
            lstActions = New ListBox()
            Dim listBox3 = lstActions
            location = New Point(108, num)
            listBox3.Location = location
            Dim listBox4 = lstActions
            size = New Size(520, 100)
            listBox4.Size = size
            lstActions.BackColor = Color.FromArgb(30, 30, 30)
            lstActions.ForeColor = Color.Orange
            CType(CObj(Me), Control).Controls.Add(lstActions)
            Dim value4 As FlatButton = MakeBtn("ADD", 12, num + 104, New EventHandler(AddressOf OnAddAction))
            Dim value5 As FlatButton = MakeBtn("EDIT", 102, num + 104, New EventHandler(AddressOf OnEditAction))
            Dim value6 As FlatButton = MakeBtn("DEL", 192, num + 104, New EventHandler(AddressOf OnDeleteAction))
            CType(CObj(Me), Control).Controls.Add(CType(CObj(value4), Control))
            CType(CObj(Me), Control).Controls.Add(CType(CObj(value5), Control))
            CType(CObj(Me), Control).Controls.Add(CType(CObj(value6), Control))
            num += 150
            fbOk = MakeBtn("SAVE", CType(CObj(Me), Form).ClientSize.Width - 220, num, New EventHandler(AddressOf OnOkClick))
            CType(fbOk, LCARSbuttonClass).Color = CType(6, LCARScolorStyles)
            fbCancel = MakeBtn("CANCEL", CType(CObj(Me), Form).ClientSize.Width - 110, num, New EventHandler(AddressOf OnCancelClick))
            CType(fbCancel, LCARSbuttonClass).Color = CType(9, LCARScolorStyles)
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbOk), Control))
            CType(CObj(Me), Control).Controls.Add(CType(CObj(fbCancel), Control))
        End Sub

        Private Sub LoadFromRule()
            txtName.Text = If(Not Equals(workingRule.Name, Nothing), workingRule.Name, "")
            chkEnabled.Checked = workingRule.Enabled
            chkRunIncoming.Checked = workingRule.RunOnIncoming
            cboLogic.SelectedItem = If(String.Equals(workingRule.ConditionLogic, "OR", StringComparison.OrdinalIgnoreCase), "OR", "AND")
            RefreshConditionList()
            RefreshActionList()
        End Sub

        Private Sub RefreshConditionList()
            lstConditions.Items.Clear()
            For Each condition In workingRule.Conditions
                Dim items = lstConditions.Items
                Dim array = New String(4) {}
                Dim mailFilterCondition = condition
                array(0) = If(Not Equals(mailFilterCondition.Field, Nothing), mailFilterCondition.Field, "")
                array(1) = " "
                Dim mailFilterCondition2 = condition
                array(2) = If(Not Equals(mailFilterCondition2.Operator, Nothing), mailFilterCondition2.Operator, "")
                array(3) = " "
                Dim mailFilterCondition3 = condition
                array(4) = If(Not Equals(mailFilterCondition3.Value, Nothing), mailFilterCondition3.Value, "")
                items.Add(String.Concat(array))
            Next
        End Sub

        Private Sub RefreshActionList()
            lstActions.Items.Clear()
            For Each action In workingRule.Actions
                Dim mailFilterAction = action
                Dim text = If(Not Equals(mailFilterAction.ActionName, Nothing), mailFilterAction.ActionName, "")
                If Not String.IsNullOrEmpty(action.ActionValue) Then
                    text = text & " → " & action.ActionValue
                End If
                lstActions.Items.Add(text)
            Next
        End Sub

        Private Sub OnCancelClick(sender As Object, e As EventArgs)
            CType(CObj(Me), Form).DialogResult = DialogResult.Cancel
            Call CType(CObj(Me), Form).Close()
        End Sub

        Private Sub OnOkClick(sender As Object, e As EventArgs)
            workingRule.Name = txtName.Text.Trim()
            workingRule.Enabled = chkEnabled.Checked
            workingRule.RunOnIncoming = chkRunIncoming.Checked
            workingRule.ConditionLogic = (If(cboLogic.SelectedItem Is Nothing, "AND", cboLogic.SelectedItem.ToString()))
            CType(CObj(Me), Form).DialogResult = DialogResult.OK
            Call CType(CObj(Me), Form).Close()
        End Sub

        Private Sub OnAddCondition(sender As Object, e As EventArgs)
            Dim frmFilterPartEdit2 As frmFilterPartEdit = New frmFilterPartEdit(forCondition:=True, Nothing, Nothing)
            Try
                If CType(CObj(frmFilterPartEdit2), Form).ShowDialog(CType(Me, IWin32Window)) = DialogResult.OK Then
                    workingRule.Conditions.Add(frmFilterPartEdit2.AsCondition())
                    RefreshConditionList()
                End If

            Finally
                If frmFilterPartEdit2 IsNot Nothing Then CType(frmFilterPartEdit2, IDisposable).Dispose()
            End Try
        End Sub

        Private Sub OnEditCondition(sender As Object, e As EventArgs)
            Dim selectedIndex = lstConditions.SelectedIndex
            If selectedIndex < 0 OrElse selectedIndex >= workingRule.Conditions.Count Then
                Return
            End If
            Dim frmFilterPartEdit2 As frmFilterPartEdit = New frmFilterPartEdit(forCondition:=True, workingRule.Conditions(selectedIndex), Nothing)
            Try
                If CType(CObj(frmFilterPartEdit2), Form).ShowDialog(CType(Me, IWin32Window)) = DialogResult.OK Then
                    workingRule.Conditions(selectedIndex) = frmFilterPartEdit2.AsCondition()
                    RefreshConditionList()
                End If

            Finally
                If frmFilterPartEdit2 IsNot Nothing Then CType(frmFilterPartEdit2, IDisposable).Dispose()
            End Try
        End Sub

        Private Sub OnDeleteCondition(sender As Object, e As EventArgs)
            Dim selectedIndex = lstConditions.SelectedIndex
            If selectedIndex >= 0 AndAlso selectedIndex < workingRule.Conditions.Count Then
                workingRule.Conditions.RemoveAt(selectedIndex)
                RefreshConditionList()
            End If
        End Sub

        Private Sub OnAddAction(sender As Object, e As EventArgs)
            Dim frmFilterPartEdit2 As frmFilterPartEdit = New frmFilterPartEdit(forCondition:=False, Nothing, Nothing)
            Try
                If CType(CObj(frmFilterPartEdit2), Form).ShowDialog(CType(Me, IWin32Window)) = DialogResult.OK Then
                    workingRule.Actions.Add(frmFilterPartEdit2.AsAction())
                    RefreshActionList()
                End If

            Finally
                If frmFilterPartEdit2 IsNot Nothing Then CType(frmFilterPartEdit2, IDisposable).Dispose()
            End Try
        End Sub

        Private Sub OnEditAction(sender As Object, e As EventArgs)
            Dim selectedIndex = lstActions.SelectedIndex
            If selectedIndex < 0 OrElse selectedIndex >= workingRule.Actions.Count Then
                Return
            End If
            Dim frmFilterPartEdit2 As frmFilterPartEdit = New frmFilterPartEdit(forCondition:=False, Nothing, workingRule.Actions(selectedIndex))
            Try
                If CType(CObj(frmFilterPartEdit2), Form).ShowDialog(CType(Me, IWin32Window)) = DialogResult.OK Then
                    workingRule.Actions(selectedIndex) = frmFilterPartEdit2.AsAction()
                    RefreshActionList()
                End If

            Finally
                If frmFilterPartEdit2 IsNot Nothing Then CType(frmFilterPartEdit2, IDisposable).Dispose()
            End Try
        End Sub

        Private Sub OnDeleteAction(sender As Object, e As EventArgs)
            Dim selectedIndex = lstActions.SelectedIndex
            If selectedIndex >= 0 AndAlso selectedIndex < workingRule.Actions.Count Then
                workingRule.Actions.RemoveAt(selectedIndex)
                RefreshActionList()
            End If
        End Sub

        Private Sub AddLabel(text As String, x As Integer, y As Integer)
            Dim label As Label = New Label()
            label.Text = text
            label.ForeColor = Color.Orange
            label.BackColor = Color.Black
            label.AutoSize = True
            Dim location As Point = New Point(x, y)
            label.Location = location
            CType(CObj(Me), Control).Controls.Add(label)
        End Sub

        Private Function AddText(x As Integer, y As Integer, w As Integer) As TextBox
            Dim textBox As TextBox = New TextBox()
            Dim location As Point = New Point(x, y)
            textBox.Location = location
            Dim size As Size = New Size(w, 24)
            textBox.Size = size
            textBox.BackColor = Color.FromArgb(40, 40, 40)
            textBox.ForeColor = Color.Orange
            textBox.BorderStyle = BorderStyle.FixedSingle
            CType(CObj(Me), Control).Controls.Add(textBox)
            Return textBox
        End Function

        Private Function MakeBtn(caption As String, x As Integer, y As Integer, handler As EventHandler) As FlatButton
            Dim val As FlatButton = New FlatButton()
            CType(val, LCARSbuttonClass).ButtonText = caption
            CType(val, LCARSbuttonClass).Text = caption
            Dim size As Size = New Size(80, 28)
            CType(CObj(val), Control).Size = size
            Dim location As Point = New Point(x, y)
            CType(CObj(val), Control).Location = location
            CType(val, LCARSbuttonClass).Color = CType(2, LCARScolorStyles)
            AddHandler CType(CObj(val), Control).Click, handler
            Return val
        End Function
    End Class

