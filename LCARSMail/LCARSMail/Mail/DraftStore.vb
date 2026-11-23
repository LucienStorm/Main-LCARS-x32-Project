Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Text
Imports System.Web.Script.Serialization
Imports Microsoft.VisualBasic.CompilerServices
    Public NotInheritable Class DraftStore
        Private Class DraftDto
            Public Property Id As String

            Public Property AccountId As String

            Public Property ToAddresses As String

            Public Property CcAddresses As String

            Public Property Subject As String

            Public Property BodyPlain As String

            Public Property BodyHtml As String

            Public Property AttachmentPaths As List(Of String)

            Public Property UpdatedUtc As Date

            Public Property ImapDraftFolder As String

            Public Property ImapDraftUid As ULong

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub

            Public Shared Function FromModel(d As MailDraft) As DraftDto
                Dim draftDto As DraftDto = New DraftDto()
                draftDto.Id = d.Id
                draftDto.AccountId = If(Not Equals(d.AccountId, Nothing), d.AccountId, "")
                draftDto.ToAddresses = If(Not Equals(d.ToAddresses, Nothing), d.ToAddresses, "")
                draftDto.CcAddresses = If(Not Equals(d.CcAddresses, Nothing), d.CcAddresses, "")
                draftDto.Subject = If(Not Equals(d.Subject, Nothing), d.Subject, "")
                draftDto.BodyPlain = If(Not Equals(d.BodyPlain, Nothing), d.BodyPlain, "")
                draftDto.BodyHtml = If(Not Equals(d.BodyHtml, Nothing), d.BodyHtml, "")
                draftDto.AttachmentPaths = (If(d.AttachmentPaths IsNot Nothing, d.AttachmentPaths, New List(Of String)()))
                draftDto.UpdatedUtc = d.UpdatedUtc
                draftDto.ImapDraftFolder = If(Not Equals(d.ImapDraftFolder, Nothing), d.ImapDraftFolder, "")
                draftDto.ImapDraftUid = d.ImapDraftUid
                Return draftDto
            End Function

            Public Function ToModel() As MailDraft
                Dim mailDraft As MailDraft = New MailDraft()
                mailDraft.Id = Id
                mailDraft.AccountId = AccountId
                mailDraft.ToAddresses = ToAddresses
                mailDraft.CcAddresses = CcAddresses
                mailDraft.Subject = Subject
                mailDraft.BodyPlain = BodyPlain
                mailDraft.BodyHtml = BodyHtml
                Dim attachmentPaths = Me.AttachmentPaths
                mailDraft.AttachmentPaths = (If(attachmentPaths IsNot Nothing, attachmentPaths, New List(Of String)()))
                mailDraft.UpdatedUtc = UpdatedUtc
                Dim imapDraftFolder = Me.ImapDraftFolder
                mailDraft.ImapDraftFolder = If(Not Equals(imapDraftFolder, Nothing), imapDraftFolder, "")
                mailDraft.ImapDraftUid = ImapDraftUid
                Return mailDraft
            End Function
        End Class

        Private Const DraftsSubdir As String = "drafts"

        Private Sub New()
        End Sub

        Public Shared Function DraftsDirectory() As String
            Dim text As String = Path.Combine(AccountStore.DataRoot(), "drafts")
            If Not Directory.Exists(text) Then
                Directory.CreateDirectory(text)
            End If
            Return text
        End Function

        Public Shared Function SaveDraft(draft As MailDraft) As String
            If draft Is Nothing Then
                Return ""
            End If
            If String.IsNullOrWhiteSpace(draft.Id) Then
                draft.Id = Guid.NewGuid().ToString("N")
            End If
            draft.UpdatedUtc = Date.UtcNow
            Dim path As String = IO.Path.Combine(DraftsDirectory(), draft.Id & ".json")
            Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
            File.WriteAllText(path, javaScriptSerializer.Serialize(DraftDto.FromModel(draft)), Encoding.UTF8)
            Return draft.Id
        End Function

        Public Shared Function LoadDraft(draftId As String) As MailDraft
            Dim result As MailDraft
            If String.IsNullOrWhiteSpace(draftId) Then
                result = Nothing
            Else
                Dim path As String = IO.Path.Combine(DraftsDirectory(), draftId.Trim() & ".json")
                If Not File.Exists(path) Then
                    result = Nothing
                Else
                    Try
                        Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                        result = If(javaScriptSerializer.Deserialize(Of DraftDto)(File.ReadAllText(path, Encoding.UTF8)) IsNot Nothing, javaScriptSerializer.Deserialize(Of DraftDto)(File.ReadAllText(path, Encoding.UTF8)).ToModel, Nothing)()
                    Catch projectError As Exception
                        ProjectData.SetProjectError(projectError)
                        result = Nothing
                        Call ProjectData.ClearProjectError()
                    End Try
                End If
            End If
            Return result
        End Function

        Public Shared Function ListDraftsForAccount(accountId As String) As List(Of MailDraft)
            Dim list As List(Of MailDraft) = New List(Of MailDraft)()
            If String.IsNullOrWhiteSpace(accountId) Then
                Return list
            End If
            Dim files As String() = Directory.GetFiles(DraftsDirectory(), "*.json")
            For Each path In files
                Try
                    Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                    Dim draftDto = javaScriptSerializer.Deserialize(Of DraftDto)(File.ReadAllText(path, Encoding.UTF8))
                    If draftDto IsNot Nothing AndAlso String.Equals(draftDto.AccountId, accountId, StringComparison.OrdinalIgnoreCase) Then
                        list.Add(draftDto.ToModel())
                    End If
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    Call ProjectData.ClearProjectError()
                End Try
            Next
            list.Sort(Function(a, b) Date.Compare(b.UpdatedUtc, a.UpdatedUtc))
            Return list
        End Function

        Public Shared Sub DeleteDraft(draftId As String)
            If Not String.IsNullOrWhiteSpace(draftId) Then
                Dim path As String = IO.Path.Combine(DraftsDirectory(), draftId.Trim() & ".json")
                If File.Exists(path) Then
                    File.Delete(path)
                End If
            End If
        End Sub
    End Class

