Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms
Imports LCARS
Imports LCARS.Controls
    Public Class frmDraftPicker
        Inherits LCARSForm
        Private Class DraftListItem
            Public ReadOnly Draft As MailDraft

            Private ReadOnly caption As String

            Public Sub New(d As MailDraft, text As String)
                Draft = d
                caption = text
            End Sub

            Public Overrides Function ToString() As String
                Return caption
            End Function
        End Class

        Private Class ImapDraftListItem
            Public ReadOnly Header As MailMessageHeader

            Private ReadOnly caption As String

            Public Sub New(hdr As MailMessageHeader, text As String)
                Header = hdr
                caption = text
            End Sub

            Public Overrides Function ToString() As String
                Return caption
            End Function
        End Class

        Private lst As ListBox

        Public Property SelectedDraft As MailDraft

        Public Property SelectedImapHeader As MailMessageHeader

        Public Sub New(accountId As String)
            Me.New(accountId, Nothing)
        End Sub

        Public Sub New(accountId As String, imapDraftHeaders As List(Of MailMessageHeader))
            CType(CObj(Me), Form).BackColor = Color.Black
            Dim clientSize As Size = New Size(560, 400)
            CType(CObj(Me), Form).ClientSize = clientSize
            CType(CObj(Me), Form).StartPosition = FormStartPosition.CenterParent
            CType(CObj(Me), Form).Text = "DRAFTS"
            Dim val As FlatButton = New FlatButton()
            CType(val, LCARSbuttonClass).ButtonText = "DRAFTS"
            CType(val, LCARSbuttonClass).Text = "DRAFTS"
            CType(val, LCARSbuttonClass).Clickable = False
            CType(val, LCARSbuttonClass).Color = CType(8, LCARScolorStyles)
            Dim location As Point = New Point(12, 8)
            CType(CObj(val), Control).Location = location
            clientSize = New Size(100, 32)
            CType(CObj(val), Control).Size = clientSize
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val), Control))
            Dim label As Label = New Label With {
        .Text = "LOCAL JSON + IMAP DRAFTS — DOUBLE-CLICK TO OPEN",
        .ForeColor = Color.Orange,
        .AutoSize = True
    }
            location = New Point(120, 16)
            label.Location = location
            CType(CObj(Me), Control).Controls.Add(label)
            lst = New ListBox()
            Dim listBox = lst
            location = New Point(12, 48)
            listBox.Location = location
            Dim listBox2 = lst
            clientSize = New Size(536, 300)
            listBox2.Size = clientSize
            lst.BackColor = Color.FromArgb(30, 30, 30)
            lst.ForeColor = Color.Orange
            AddHandler lst.DoubleClick, AddressOf OnOpen
            CType(CObj(Me), Control).Controls.Add(lst)
            If imapDraftHeaders IsNot Nothing Then
                For Each imapDraftHeader In imapDraftHeaders
                    If imapDraftHeader IsNot Nothing Then
                        Dim mailMessageHeader = imapDraftHeader
                        Dim text = If(Not Equals(mailMessageHeader.Subject, Nothing), mailMessageHeader.Subject, "(NO SUBJECT)")
                        lst.Items.Add(New ImapDraftListItem(imapDraftHeader, "[IMAP] " & text & " — " & imapDraftHeader.DateUtc.ToLocalTime().ToString("g")))
                    End If
                Next
            End If
            Dim list = DraftStore.ListDraftsForAccount(accountId)
            For Each item In list
                Dim mailDraft = item
                Dim text2 = If(Not Equals(mailDraft.Subject, Nothing), mailDraft.Subject, "(NO SUBJECT)")
                lst.Items.Add(New DraftListItem(item, "[LOCAL] " & text2 & " — " & item.UpdatedUtc.ToLocalTime().ToString("g")))
            Next
            Dim val2 As FlatButton = New FlatButton()
            CType(val2, LCARSbuttonClass).ButtonText = "OPEN"
            CType(val2, LCARSbuttonClass).Text = "OPEN"
            CType(val2, LCARSbuttonClass).Color = CType(6, LCARScolorStyles)
            clientSize = New Size(100, 28)
            CType(CObj(val2), Control).Size = clientSize
            location = New Point(12, 360)
            CType(CObj(val2), Control).Location = location
            AddHandler CType(CObj(val2), Control).Click, AddressOf OnOpen
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val2), Control))
            Dim val3 As FlatButton = New FlatButton()
            CType(val3, LCARSbuttonClass).ButtonText = "CANCEL"
            CType(val3, LCARSbuttonClass).Text = "CANCEL"
            CType(val3, LCARSbuttonClass).Color = CType(9, LCARScolorStyles)
            clientSize = New Size(100, 28)
            CType(CObj(val3), Control).Size = clientSize
            location = New Point(448, 360)
            CType(CObj(val3), Control).Location = location
            AddHandler CType(CObj(val3), Control).Click, AddressOf OnCancelClick
            CType(CObj(Me), Control).Controls.Add(CType(CObj(val3), Control))
        End Sub

        Private Sub OnCancelClick(sender As Object, e As EventArgs)
            CType(CObj(Me), Form).DialogResult = DialogResult.Cancel
            Call CType(CObj(Me), Form).Close()
        End Sub

        Private Sub OnOpen(sender As Object, e As EventArgs)
            Dim imapDraftListItem As ImapDraftListItem = Nothing, draftListItem As DraftListItem = Nothing

            If CSharpImpl.__Assign(imapDraftListItem, TryCast(lst.SelectedItem, ImapDraftListItem)) IsNot Nothing Then
                SelectedImapHeader = imapDraftListItem.Header
                CType(CObj(Me), Form).DialogResult = DialogResult.OK
                Call CType(CObj(Me), Form).Close()
            ElseIf CSharpImpl.__Assign(draftListItem, TryCast(lst.SelectedItem, DraftListItem)) IsNot Nothing Then
                SelectedDraft = draftListItem.Draft
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

