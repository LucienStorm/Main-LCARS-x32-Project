Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Text
Imports System.Web.Script.Serialization
Imports Microsoft.VisualBasic.CompilerServices
    <StandardModule>
    Public NotInheritable Class MailCache
        Private Class HeaderDto
            Public Property Uid As ULong

            Public Property Subject As String

            Public Property FromAddress As String

            Public Property ToAddresses As String

            Public Property CcAddresses As String

            Public Property DateUtc As Date

            Public Property IsSeen As Boolean

            Public Property MessageId As String

            Public Property InReplyTo As String

            Public Property ReferencesHeader As String

            Public Property ReplyToLine As String

            Public Property ListIdLine As String

            Public Property ReturnPathLine As String

            Public Property SizeBytes As Long

            Public Property IsJunk As Boolean

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub

            Public Shared Function FromModel(h As MailMessageHeader) As HeaderDto
                Dim headerDto As HeaderDto = New HeaderDto()
                headerDto.Uid = h.Uid
                headerDto.Subject = If(Not Equals(h.Subject, Nothing), h.Subject, "")
                headerDto.FromAddress = If(Not Equals(h.From, Nothing), h.From, "")
                headerDto.ToAddresses = If(Not Equals(h.ToAddresses, Nothing), h.ToAddresses, "")
                headerDto.CcAddresses = If(Not Equals(h.CcAddresses, Nothing), h.CcAddresses, "")
                headerDto.DateUtc = h.DateUtc
                headerDto.IsSeen = h.IsSeen
                headerDto.MessageId = If(Not Equals(h.MessageId, Nothing), h.MessageId, "")
                headerDto.InReplyTo = If(Not Equals(h.InReplyTo, Nothing), h.InReplyTo, "")
                headerDto.ReferencesHeader = If(Not Equals(h.References, Nothing), h.References, "")
                headerDto.ReplyToLine = If(Not Equals(h.ReplyTo, Nothing), h.ReplyTo, "")
                headerDto.ListIdLine = If(Not Equals(h.ListId, Nothing), h.ListId, "")
                headerDto.ReturnPathLine = If(Not Equals(h.ReturnPath, Nothing), h.ReturnPath, "")
                headerDto.SizeBytes = h.SizeBytes
                headerDto.IsJunk = h.IsJunk
                Return headerDto
            End Function

            Public Function ToModel(folder As String) As MailMessageHeader
                Dim mailMessageHeader As MailMessageHeader = New MailMessageHeader()
                mailMessageHeader.Uid = Uid
                mailMessageHeader.Folder = If(Not Equals(folder, Nothing), folder, "")
                Dim subject = Me.Subject
                mailMessageHeader.Subject = If(Not Equals(subject, Nothing), subject, "")
                Dim fromAddress = Me.FromAddress
                mailMessageHeader.From = If(Not Equals(fromAddress, Nothing), fromAddress, "")
                Dim toAddresses = Me.ToAddresses
                mailMessageHeader.ToAddresses = If(Not Equals(toAddresses, Nothing), toAddresses, "")
                Dim ccAddresses = Me.CcAddresses
                mailMessageHeader.CcAddresses = If(Not Equals(ccAddresses, Nothing), ccAddresses, "")
                mailMessageHeader.DateUtc = DateUtc
                mailMessageHeader.IsSeen = IsSeen
                Dim messageId = Me.MessageId
                mailMessageHeader.MessageId = If(Not Equals(messageId, Nothing), messageId, "")
                Dim inReplyTo = Me.InReplyTo
                mailMessageHeader.InReplyTo = If(Not Equals(inReplyTo, Nothing), inReplyTo, "")
                Dim referencesHeader = Me.ReferencesHeader
                mailMessageHeader.References = If(Not Equals(referencesHeader, Nothing), referencesHeader, "")
                Dim replyToLine = Me.ReplyToLine
                mailMessageHeader.ReplyTo = If(Not Equals(replyToLine, Nothing), replyToLine, "")
                Dim listIdLine = Me.ListIdLine
                mailMessageHeader.ListId = If(Not Equals(listIdLine, Nothing), listIdLine, "")
                Dim returnPathLine = Me.ReturnPathLine
                mailMessageHeader.ReturnPath = If(Not Equals(returnPathLine, Nothing), returnPathLine, "")
                mailMessageHeader.SizeBytes = SizeBytes
                mailMessageHeader.IsJunk = IsJunk
                Return mailMessageHeader
            End Function
        End Class

        Private Class AttachmentDto
            Public Property PartIndex As Integer

            Public Property FileName As String

            Public Property Size As Long

            Public Property IsInline As Boolean

            Public Property ContentId As String

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Private Class BodyDto
            Public Property TextBody As String

            Public Property HtmlBody As String

            Public Property Attachments As List(Of AttachmentDto)

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub

            Public Shared Function FromModel(body As MailMessageBody) As BodyDto
                Dim bodyDto As BodyDto = New BodyDto()
                bodyDto.TextBody = If(Not Equals(body.TextBody, Nothing), body.TextBody, "")
                bodyDto.HtmlBody = If(Not Equals(body.HtmlBody, Nothing), body.HtmlBody, "")
                Dim bodyDto2 = bodyDto
                bodyDto2.Attachments = New List(Of AttachmentDto)()
                If body.Attachments IsNot Nothing Then
                    For Each attachment In body.Attachments
                        If attachment IsNot Nothing Then
                            Dim attachments = bodyDto2.Attachments
                            Dim attachmentDto As AttachmentDto = New AttachmentDto With {
        .PartIndex = attachment.PartIndex
    }
                            Dim mailAttachmentInfo = attachment
                            attachmentDto.FileName = If(Not Equals(mailAttachmentInfo.FileName, Nothing), mailAttachmentInfo.FileName, "")
                            attachmentDto.Size = attachment.Size
                            attachmentDto.IsInline = attachment.IsInline
                            Dim mailAttachmentInfo2 = attachment
                            attachmentDto.ContentId = If(Not Equals(mailAttachmentInfo2.ContentId, Nothing), mailAttachmentInfo2.ContentId, "")
                            attachments.Add(attachmentDto)
                        End If
                    Next
                End If
                Return bodyDto2
            End Function

            Public Function ToModel() As MailMessageBody
                Dim mailMessageBody As MailMessageBody = New MailMessageBody()
                Dim textBody = Me.TextBody
                mailMessageBody.TextBody = If(Not Equals(textBody, Nothing), textBody, "")
                Dim htmlBody = Me.HtmlBody
                mailMessageBody.HtmlBody = If(Not Equals(htmlBody, Nothing), htmlBody, "")
                Dim mailMessageBody2 = mailMessageBody
                If Attachments IsNot Nothing Then
                    For Each attachment In Attachments
                        If attachment IsNot Nothing Then
                            Dim attachments = mailMessageBody2.Attachments
                            Dim mailAttachmentInfo As MailAttachmentInfo = New MailAttachmentInfo With {
        .PartIndex = attachment.PartIndex
    }
                            htmlBody = attachment.FileName
                            mailAttachmentInfo.FileName = If(Not Equals(htmlBody, Nothing), htmlBody, "")
                            mailAttachmentInfo.Size = attachment.Size
                            mailAttachmentInfo.IsInline = attachment.IsInline
                            textBody = attachment.ContentId
                            mailAttachmentInfo.ContentId = If(Not Equals(textBody, Nothing), textBody, "")
                            attachments.Add(mailAttachmentInfo)
                        End If
                    Next
                End If
                Return mailMessageBody2
            End Function
        End Class

        Private Const CacheSubdir As String = "cache"

        Private Const HeadersFileName As String = "headers.json"

        Private Const BodiesSubdir As String = "bodies"

        Private Const BodyJsonSuffix As String = ".body.json"

        Private Const LegacyBodySuffix As String = ".txt"

        Private Const AttachmentCachePrefix As String = "att_"

        Public Shared Function LoadHeaders(accountId As String, folder As String) As List(Of MailMessageHeader)
            Dim path = HeadersPath(accountId, folder)
            Dim result As List(Of MailMessageHeader)
            If Not File.Exists(path) Then
                result = New List(Of MailMessageHeader)()
            Else
                Try
                    Dim input = File.ReadAllText(path, Encoding.UTF8)
                    Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                    Dim list = javaScriptSerializer.Deserialize(Of List(Of HeaderDto))(input)
                    If list Is Nothing Then
                        result = New List(Of MailMessageHeader)()
                    Else
                        Dim list2 As List(Of MailMessageHeader) = New List(Of MailMessageHeader)()
                        For Each item In list
                            If item IsNot Nothing Then
                                list2.Add(item.ToModel(folder))
                            End If
                        Next
                        result = list2
                    End If
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    result = New List(Of MailMessageHeader)()
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Public Shared Sub SaveHeaders(accountId As String, folder As String, headers As List(Of MailMessageHeader))
            If String.IsNullOrWhiteSpace(accountId) OrElse headers Is Nothing Then
                Return
            End If
            Dim path = FolderCacheDir(accountId, folder)
            If Not Directory.Exists(path) Then
                Directory.CreateDirectory(path)
            End If
            Dim list As List(Of HeaderDto) = New List(Of HeaderDto)()
            For Each header In headers
                If header IsNot Nothing Then
                    list.Add(HeaderDto.FromModel(header))
                End If
            Next
            Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
            Dim contents = javaScriptSerializer.Serialize(list)
            File.WriteAllText(HeadersPath(accountId, folder), contents, Encoding.UTF8)
        End Sub

        Public Shared Function TryLoadBody(accountId As String, folder As String, uid As ULong, ByRef body As MailMessageBody) As Boolean
            body = Nothing
            Dim result As Boolean
            If String.IsNullOrWhiteSpace(accountId) Then
                result = False
            Else
                Dim path = BodyJsonPath(accountId, folder, uid)
                If File.Exists(path) Then
                    Try
                        Dim input = File.ReadAllText(path, Encoding.UTF8)
                        Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                        Dim bodyDto = javaScriptSerializer.Deserialize(Of BodyDto)(input)
                        If bodyDto IsNot Nothing Then
                            body = bodyDto.ToModel()
                            HydrateAttachmentCachePaths(accountId, folder, uid, body)
                            result = True
                            GoTo IL_00c2
                        End If
                    Catch projectError As Exception
                        ProjectData.SetProjectError(projectError)
                        Call ProjectData.ClearProjectError()
                    End Try
                End If
                Dim path2 = LegacyBodyPath(accountId, folder, uid)
                If Not File.Exists(path2) Then
                    result = False
                Else
                    Try
                        body = New MailMessageBody With {
        .TextBody = File.ReadAllText(path2, Encoding.UTF8)
    }
                        result = True
                    Catch projectError2 As Exception
                        ProjectData.SetProjectError(projectError2)
                        body = Nothing
                        result = False
                        Call ProjectData.ClearProjectError()
                    End Try
                End If
            End If
            GoTo IL_00c2
IL_00c2:
            Return result
        End Function

        Public Shared Sub SaveBody(accountId As String, folder As String, uid As ULong, body As MailMessageBody)
            If Not String.IsNullOrWhiteSpace(accountId) Then
                If body Is Nothing Then
                    body = New MailMessageBody()
                End If
                Dim path = IO.Path.Combine(FolderCacheDir(accountId, folder), "bodies")
                If Not Directory.Exists(path) Then
                    Directory.CreateDirectory(path)
                End If
                Dim obj = BodyDto.FromModel(body)
                Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                Dim contents = javaScriptSerializer.Serialize(obj)
                File.WriteAllText(BodyJsonPath(accountId, folder, uid), contents, Encoding.UTF8)
            End If
        End Sub

        Public Shared Function TryGetCachedAttachmentPath(accountId As String, folder As String, uid As ULong, partIndex As Integer, fileName As String) As String
            Dim text = AttachmentCachePath(accountId, folder, uid, partIndex, fileName)
            If File.Exists(text) Then
                Return text
            End If
            Return Nothing
        End Function

        Public Shared Function SaveAttachmentCache(accountId As String, folder As String, uid As ULong, partIndex As Integer, fileName As String, sourcePath As String) As String
            If String.IsNullOrWhiteSpace(sourcePath) OrElse Not File.Exists(sourcePath) Then
                Return Nothing
            End If
            Dim text = AttachmentCachePath(accountId, folder, uid, partIndex, fileName)
            Dim directoryName = Path.GetDirectoryName(text)
            If Not Directory.Exists(directoryName) Then
                Directory.CreateDirectory(directoryName)
            End If
            File.Copy(sourcePath, text, overwrite:=True)
            Return text
        End Function

        Public Shared Sub UpdateHeaderSeen(accountId As String, folder As String, uid As ULong, isSeen As Boolean)
            Dim list = LoadHeaders(accountId, folder)
            Dim flag = False
            For Each item In list
                If item IsNot Nothing AndAlso item.Uid = uid Then
                    item.IsSeen = isSeen
                    flag = True
                    Exit For
                End If
            Next
            If flag Then
                SaveHeaders(accountId, folder, list)
            End If
        End Sub

        Public Shared Sub RemoveMessageCache(accountId As String, folder As String, uid As ULong)
            If String.IsNullOrWhiteSpace(accountId) OrElse uid = 0 Then
                Return
            End If
            Dim list = LoadHeaders(accountId, folder)
            Dim flag = False
            Dim i = list.Count - 1

            While i >= 0
                Dim mailMessageHeader = list(i)
                If mailMessageHeader IsNot Nothing AndAlso mailMessageHeader.Uid = uid Then
                    list.RemoveAt(i)
                    flag = True
                End If

                i += -1
            End While
            If flag Then
                SaveHeaders(accountId, folder, list)
            End If
            Dim path = BodyJsonPath(accountId, folder, uid)
            If File.Exists(path) Then
                Try
                    File.Delete(path)
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Dim path2 = LegacyBodyPath(accountId, folder, uid)
            If File.Exists(path2) Then
                Try
                    File.Delete(path2)
                Catch projectError2 As Exception
                    ProjectData.SetProjectError(projectError2)
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Dim path3 As String = IO.Path.Combine(FolderCacheDir(accountId, folder), "bodies", uid.ToString() & "_files")
            If Not Directory.Exists(path3) Then
                Return
            End If
            Try
                Directory.Delete(path3, recursive:=True)
            Catch projectError3 As Exception
                ProjectData.SetProjectError(projectError3)
                Call ProjectData.ClearProjectError()
            End Try
        End Sub

        Private Shared Sub HydrateAttachmentCachePaths(accountId As String, folder As String, uid As ULong, body As MailMessageBody)
            If body Is Nothing OrElse body.Attachments Is Nothing Then
                Return
            End If
            For Each attachment In body.Attachments
                If attachment IsNot Nothing Then
                    attachment.CachedPath = TryGetCachedAttachmentPath(accountId, folder, uid, attachment.PartIndex, attachment.FileName)
                End If
            Next
        End Sub

        Private Shared Function CacheRoot() As String
            Return Path.Combine(AccountStore.DataRoot(), "cache")
        End Function

        Private Shared Function AccountCacheDir(accountId As String) As String
            Return Path.Combine(CacheRoot(), SanitizeFileToken(accountId))
        End Function

        Private Shared Function FolderCacheDir(accountId As String, folder As String) As String
            Return Path.Combine(AccountCacheDir(accountId), SanitizeFolderToken(folder))
        End Function

        Private Shared Function HeadersPath(accountId As String, folder As String) As String
            Return Path.Combine(FolderCacheDir(accountId, folder), "headers.json")
        End Function

        Private Shared Function BodyJsonPath(accountId As String, folder As String, uid As ULong) As String
            Return Path.Combine(FolderCacheDir(accountId, folder), "bodies", uid.ToString() & ".body.json")
        End Function

        Private Shared Function LegacyBodyPath(accountId As String, folder As String, uid As ULong) As String
            Return Path.Combine(FolderCacheDir(accountId, folder), "bodies", uid.ToString() & ".txt")
        End Function

        Private Shared Function AttachmentCachePath(accountId As String, folder As String, uid As ULong, partIndex As Integer, fileName As String) As String
            Dim path As String = IO.Path.Combine(FolderCacheDir(accountId, folder), "bodies", uid.ToString() & "_files")
            Dim text = SanitizeFileToken(If(Not Equals(fileName, Nothing), fileName, "attachment"))
            Return IO.Path.Combine(path, "att_" & partIndex.ToString() & "_" & text)
        End Function

        Private Shared Function SanitizeFolderToken(folder As String) As String
            If String.IsNullOrWhiteSpace(folder) Then
                Return "INBOX"
            End If
            Dim token = folder.Replace("/"c, "_"c).Replace("\"c, "_"c)
            Return SanitizeFileToken(token)
        End Function

        Private Shared Function SanitizeFileToken(token As String) As String
            Dim invalidFileNameChars As Char() = Path.GetInvalidFileNameChars()
            Dim stringBuilder As StringBuilder = New StringBuilder(token.Length)
            Dim i = 0
            Dim length = token.Length

            While i < length
                Dim value = token(i)
                If Array.IndexOf(invalidFileNameChars, value) >= 0 Then
                    stringBuilder.Append("_"c)
                Else
                    stringBuilder.Append(value)
                End If

                i = i + 1
            End While
            Dim text As String = stringBuilder.ToString()
            If String.IsNullOrWhiteSpace(text) Then
                text = "folder"
            End If
            Return text
        End Function
    End Class

