Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms
Imports LCARS
Imports LCARS.Controls
    Public Class frmGlodaSavedSearchPicker
        Inherits LCARSForm
        Private Class SavedRow
            Public ReadOnly Query As String

            Private ReadOnly caption As String

            Public Sub New(query As String, display As String)
                Me.Query = query
                caption = display
            End Sub

            Public Overrides Function ToString() As String
                Return caption
            End Function
        End Class

        Private lstSaved As ListBox

        Private lblHint As Label

        Public Property SelectedQuery As String

        Public Sub New()
            Dim selectedQuery = ""
            Me.SelectedQuery = selectedQuery
            CType(CObj(Me), Form).BackColor = Color.Black
            Dim clientSize As Size = New Size(520, 360)
            CType(CObj(Me), Form).ClientSize = clientSize
            CType(CObj(Me), Form).StartPosition = FormStartPosition.CenterParent
            BuildUi()
            LoadSaved()
        End Sub

        Private Sub BuildUi()
            lblHint = New Label()
            lblHint.ForeColor = Color.Orange
            Dim label = lblHint
            Dim location As Point = New Point(12, 12)
            label.Location = location
            Dim label2 = lblHint
            Dim size As Size = New Size(496, 24)
            label2.Size = size
            lblHint.Text = "SAVED GLODA SEARCHES"
            CType(CObj(Me), Control).Controls.Add(lblHint)
            lstSaved = New ListBox()
            Dim listBox = lstSaved
            location = New Point(12, 40)
            listBox.Location = location
            Dim listBox2 = lstSaved
            size = New Size(496, 260)
            listBox2.Size = size
            lstSaved.BackColor = Color.FromArgb(30, 30, 30)
            lstSaved.ForeColor = Color.Orange
            AddHandler lstSaved.DoubleClick, AddressOf OnAccept
            CType(CObj(Me), Control).Controls.Add(lstSaved)
            Dim val As FlatButton = New FlatButton()
            CType(val, LCARSbuttonClass).ButtonText = "RUN"
            CType(val, LCARSbuttonClass).Text = "RUN"
            CType(val, LCARSbuttonClass).Color = CType(6, LCARScolorStyles)
            size = New Size(80, 28)
            CType(CObj(val), Control).Size = size
            location = New Point(12, 312)
            CType(CObj(val), Control).Location = location
            AddHandler CType(CObj(val), Control).Click, AddressOf OnAccept
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val), Control))
            Dim val2 As FlatButton = New FlatButton()
            CType(val2, LCARSbuttonClass).ButtonText = "CANCEL"
            CType(val2, LCARSbuttonClass).Text = "CANCEL"
            CType(val2, LCARSbuttonClass).Color = CType(9, LCARScolorStyles)
            size = New Size(100, 28)
            CType(CObj(val2), Control).Size = size
            location = New Point(100, 312)
            CType(CObj(val2), Control).Location = location
            AddHandler CType(CObj(val2), Control).Click, Sub(s, e) CType(CObj(Me), Form).DialogResult = DialogResult.Cancel
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val2), Control))
        End Sub

        Private Sub LoadSaved()
            lstSaved.Items.Clear()
            Dim list As List(Of KeyValuePair(Of String, String)) = MailGlodaSavedSearchStore.TryListSaved()
            If list Is Nothing OrElse list.Count = 0 Then
                lstSaved.Items.Add("(NO SAVED SEARCHES)")
                Return
            End If
            For Each item In list
                Dim text = item.Key & " — " & item.Value
                If text.Length > 120 Then
                    text = text.Substring(0, 117) & "…"
                End If
                lstSaved.Items.Add(New SavedRow(item.Value, text))
            Next
            lstSaved.SelectedIndex = 0
        End Sub

        Private Sub OnAccept(sender As Object, e As EventArgs)
            Dim savedRow As SavedRow = Nothing

            If lstSaved.SelectedItem IsNot Nothing AndAlso CSharpImpl.__Assign(savedRow, TryCast(lstSaved.SelectedItem, SavedRow)) IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(savedRow.Query) Then
                SelectedQuery = savedRow.Query
                CType(CObj(Me), Form).DialogResult = DialogResult.OK
                Call CType(CObj(Me), Form).Close()
            End If
        End Sub

        Private Class CSharpImpl
            Shared Function __Assign(Of T)(ByRef target As T, value As T) As T
                target = value
                Return value
            End Function
        End Class
    End Class

