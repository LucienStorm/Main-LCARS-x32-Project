Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports LCARS
Imports LCARS.Controls
    Public Class frmFilterPartEdit
        Inherits LCARSForm
        Private ReadOnly editCondition As Boolean

        Private cboField As ComboBox

        Private cboOp As ComboBox

        Private txtValue As TextBox

        Private cboAction As ComboBox

        Private txtActionValue As TextBox

        Public Sub New(forCondition As Boolean, cond As MailFilterCondition, act As MailFilterAction)
            editCondition = forCondition
            CType(CObj(Me), Form).Text = If(forCondition, "CONDITION", "ACTION")
            CType(CObj(Me), Form).BackColor = Color.Black
            Dim clientSize As Size = New Size(480, If(forCondition, 220, 200))
            CType(CObj(Me), Form).ClientSize = clientSize
            CType(CObj(Me), Form).StartPosition = FormStartPosition.CenterParent
            Dim num = 12
            If forCondition Then
                AddPrompt("FIELD", 12, num)
                cboField = AddCombo(12, num + 18, 450)
                cboField.Items.AddRange(New Object(7) {"subject", "from", "to", "cc", "toorcc", "body", "age", "size"})
                num += 48
                AddPrompt("OPERATOR", 12, num)
                cboOp = AddCombo(12, num + 18, 450)
                cboOp.Items.AddRange(New Object(6) {"contains", "doesn't contain", "is", "isn't", "begins with", "ends with", "matches"})
                num += 48
                AddPrompt("VALUE", 12, num)
                txtValue = AddField(12, num + 18, 450)
                If cond IsNot Nothing Then
                    Dim cbo = cboField
                    Dim mailFilterCondition = cond
                    frmFilterPartEdit.SelectCombo(cbo, If(Not Equals(mailFilterCondition.Field, Nothing), mailFilterCondition.Field, ""))
                    Dim cbo2 = cboOp
                    mailFilterCondition = cond
                    frmFilterPartEdit.SelectCombo(cbo2, If(Not Equals(mailFilterCondition.Operator, Nothing), mailFilterCondition.Operator, ""))
                    Dim textBox = txtValue
                    mailFilterCondition = cond
                    textBox.Text = If(Not Equals(mailFilterCondition.Value, Nothing), mailFilterCondition.Value, "")
                End If
            Else
                AddPrompt("ACTION", 12, num)
                cboAction = AddCombo(12, num + 18, 450)
                cboAction.Items.AddRange(New Object(9) {"Move to folder", "Copy to folder", "Mark read", "Mark unread", "Delete", "Mark as junk", "Add tag", "Forward", "Reply to sender", "Stop execution"})
                num += 48
                AddPrompt("VALUE (FOLDER / TAG / ADDRESS)", 12, num)
                txtActionValue = AddField(12, num + 18, 450)
                If act IsNot Nothing Then
                    Dim cbo3 = cboAction
                    Dim mailFilterAction = act
                    frmFilterPartEdit.SelectCombo(cbo3, If(Not Equals(mailFilterAction.ActionName, Nothing), mailFilterAction.ActionName, ""))
                    Dim textBox2 = txtActionValue
                    mailFilterAction = act
                    textBox2.Text = If(Not Equals(mailFilterAction.ActionValue, Nothing), mailFilterAction.ActionValue, "")
                End If
            End If
            Dim val As FlatButton = New FlatButton()
            CType(val, LCARSbuttonClass).ButtonText = "OK"
            CType(val, LCARSbuttonClass).Text = "OK"
            CType(val, LCARSbuttonClass).Color = CType(6, LCARScolorStyles)
            clientSize = New Size(80, 28)
            CType(CObj(val), Control).Size = clientSize
            Dim location As Point = New Point(280, CType(CObj(Me), Form).ClientSize.Height - 44)
            CType(CObj(val), Control).Location = location
            AddHandler CType(CObj(val), Control).Click, AddressOf OnOkClick
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val), Control))
            Dim val2 As FlatButton = New FlatButton()
            CType(val2, LCARSbuttonClass).ButtonText = "CANCEL"
            CType(val2, LCARSbuttonClass).Text = "CANCEL"
            CType(val2, LCARSbuttonClass).Color = CType(9, LCARScolorStyles)
            clientSize = New Size(80, 28)
            CType(CObj(val2), Control).Size = clientSize
            location = (CSharpImpl.__Assign(CType(CObj(val2), Control).Location, New Point(370, CType(CObj(Me), Form).ClientSize.Height - 44)))
            AddHandler CType(CObj(val2), Control).Click, AddressOf OnCancelClick
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val2), Control))
        End Sub

        Private Shared Sub SelectCombo(cbo As ComboBox, value As String)
            If cbo Is Nothing Then
                Return
            End If
            Dim text As String = If(Not Equals(value, Nothing), value, "").Trim()
            If text.Length = 0 Then
                Return
            End If
            Dim num = cbo.Items.Count - 1
            For i = 0 To num
                If String.Equals(cbo.Items(i).ToString(), text, StringComparison.OrdinalIgnoreCase) Then
                    cbo.SelectedIndex = i
                    Return
                End If
            Next
            cbo.Items.Add(text)
            cbo.SelectedIndex = cbo.Items.Count - 1
        End Sub

        Private Sub OnOkClick(sender As Object, e As EventArgs)
            CType(CObj(Me), Form).DialogResult = DialogResult.OK
            Call CType(CObj(Me), Form).Close()
        End Sub

        Private Sub OnCancelClick(sender As Object, e As EventArgs)
            CType(CObj(Me), Form).DialogResult = DialogResult.Cancel
            Call CType(CObj(Me), Form).Close()
        End Sub

        Public Function AsCondition() As MailFilterCondition
            Dim mailFilterCondition As MailFilterCondition = New MailFilterCondition()
            mailFilterCondition.Field = (If(cboField IsNot Nothing AndAlso cboField.SelectedItem IsNot Nothing, cboField.SelectedItem.ToString(), cboField.Text)).Trim()
            mailFilterCondition.Operator = (If(cboOp IsNot Nothing AndAlso cboOp.SelectedItem IsNot Nothing, cboOp.SelectedItem.ToString(), cboOp.Text)).Trim()
            mailFilterCondition.Value = (If(txtValue IsNot Nothing, txtValue.Text.Trim(), ""))
            Return mailFilterCondition
        End Function

        Public Function AsAction() As MailFilterAction
            Dim mailFilterAction As MailFilterAction = New MailFilterAction()
            mailFilterAction.ActionName = (If(cboAction IsNot Nothing AndAlso cboAction.SelectedItem IsNot Nothing, cboAction.SelectedItem.ToString(), cboAction.Text)).Trim()
            mailFilterAction.ActionValue = (If(txtActionValue IsNot Nothing, txtActionValue.Text.Trim(), ""))
            Return mailFilterAction
        End Function

        Private Sub AddPrompt(text As String, x As Integer, y As Integer)
            Dim label As Label = New Label()
            label.Text = text
            label.ForeColor = Color.Orange
            label.BackColor = Color.Black
            label.AutoSize = True
            Dim location As Point = New Point(x, y)
            label.Location = location
            CType(CObj(Me), Control).Controls.Add(label)
        End Sub

        Private Function AddField(x As Integer, y As Integer, w As Integer) As TextBox
            Dim textBox As TextBox = New TextBox()
            Dim location As Point = New Point(x, y)
            textBox.Location = location
            Dim size As Size = New Size(w, 24)
            textBox.Size = size
            textBox.BackColor = Color.FromArgb(40, 40, 40)
            textBox.ForeColor = Color.Orange
            CType(CObj(Me), Control).Controls.Add(textBox)
            Return textBox
        End Function

        Private Function AddCombo(x As Integer, y As Integer, w As Integer) As ComboBox
            Dim comboBox As ComboBox = New ComboBox()
            Dim location As Point = New Point(x, y)
            comboBox.Location = location
            Dim size As Size = New Size(w, 24)
            comboBox.Size = size
            comboBox.DropDownStyle = ComboBoxStyle.DropDown
            comboBox.BackColor = Color.FromArgb(40, 40, 40)
            comboBox.ForeColor = Color.Orange
            CType(CObj(Me), Control).Controls.Add(comboBox)
            Return comboBox
        End Function

        Private Class CSharpImpl
            Shared Function __Assign(Of T)(ByRef target As T, value As T) As T
                target = value
                Return value
            End Function
        End Class
    End Class

