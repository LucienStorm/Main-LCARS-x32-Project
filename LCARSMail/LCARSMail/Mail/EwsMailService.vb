Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Globalization
Imports System.IO
Imports System.Net
Imports System.Runtime.InteropServices
Imports System.Security
Imports System.Text
Imports System.Xml
Imports Microsoft.VisualBasic.CompilerServices
    Public NotInheritable Class EwsMailService
        Public Class EwsMailSummary
            Public ItemId As String

            Public Subject As String

            Public FromLine As String

            Public DateUtc As Date

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Public Class EwsAttachmentInfo
            Public AttachmentId As String

            Public FileName As String

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Public Class EwsMessageBody
            Public Subject As String

            Public BodyText As String

            Public Attachments As List(Of EwsAttachmentInfo)

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Public Class EwsFolderInfo
            Public FolderId As String

            Public DisplayName As String

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Public Class EwsCalendarSummary
            Public ItemId As String

            Public ChangeKey As String

            Public Subject As String

            Public StartText As String

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Public Class EwsCalendarEventDetails
            Public Uid As String

            Public ItemId As String

            Public ChangeKey As String

            Public Subject As String

            Public StartUtc As Date

            Public EndUtc As Date

            Public AlarmMinutesBefore As Integer

            Public LastModifiedUtc As Date

            Public Location As String

            Public Category As String

            Public HasRecurrence As Boolean

            Public RecurrenceSignature As String

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Public Class EwsResolvedContact
            Public DisplayName As String

            Public EmailAddress As String

            Public Company As String

            Public BusinessPhone As String

            Public MobilePhone As String

            Public JobTitle As String

            Public Department As String

            Public WebPage As String

            Public FaxNumber As String

            Public PagerNumber As String

            Public OfficeLocation As String

            Public HomePhone As String

            Public MiddleName As String

            Public OtherPhone As String

            Public AssistantName As String

            Public FirstName As String

            Public LastName As String

            Public GalPersonaId As String

            Public GalPersonaChangeKey As String

            Public DirectoryContactItemId As String

            Public DirectoryContactChangeKey As String

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Public Shared LastError As String

        Private Const SoapEnvelopeNs As String = "http://schemas.xmlsoap.org/soap/envelope/"

        Private Const TypesNs As String = "http://schemas.microsoft.com/exchange/services/2006/types"

        Private Const MessagesNs As String = "http://schemas.microsoft.com/exchange/services/2006/messages"

        Public Shared LastGalPersonaUpdateDenied As Boolean

        Public Shared LastGalDirectoryUpdateDenied As Boolean

        Private Shared ReadOnly DistinguishedFolderIds As HashSet(Of String) = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase) From {
                "inbox",
                "sentitems",
                "drafts",
                "deleteditems",
                "junkemail",
                "archive",
                "outbox",
                "clutter"
            }

        Private Sub New()
        End Sub

        Public Shared Function TryFindInboxItems(ewsUrl As String, username As String, password As String, Optional oauthToken As String = Nothing) As String
            Dim envelope = "<?xml version=""1.0"" encoding=""utf-8""?><soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:t=""http://schemas.microsoft.com/exchange/services/2006/types"" xmlns:m=""http://schemas.microsoft.com/exchange/services/2006/messages""><soap:Body><m:FindItem Traversal=""Shallow""><m:ItemShape><t:BaseShape>Default</t:BaseShape></m:ItemShape><m:ParentFolderIds><t:DistinguishedFolderId Id=""inbox""/></m:ParentFolderIds></m:FindItem></soap:Body></soap:Envelope>"
            Return PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/FindItem", envelope, oauthToken)
        End Function

        Public Shared Function ParseInboxSummaries(soapXml As String) As List(Of EwsMailSummary)
            Dim list As List(Of EwsMailSummary) = New List(Of EwsMailSummary)()
            If String.IsNullOrWhiteSpace(soapXml) Then
                Return list
            End If
            Dim array = soapXml.Split(New String(0) {"<t:Subject>"}, StringSplitOptions.None)
            Dim num = array.Length - 1
            For i = 1 To num
                Dim text = array(i)
                Dim num2 = text.IndexOf("</t:Subject>", StringComparison.OrdinalIgnoreCase)
                Dim subject = ""
                If num2 > 0 Then
                    subject = text.Substring(0, num2)
                End If
                Dim text2 = "ItemId Id="""
                Dim num3 = text.IndexOf(text2, StringComparison.OrdinalIgnoreCase)
                Dim text3 = ""
                If num3 >= 0 Then
                    num3 += text2.Length
                    Dim num4 = text.IndexOf(""""c, num3)
                    If num4 > num3 Then
                        text3 = text.Substring(num3, num4 - num3)
                    End If
                End If
                Dim text4 = ExtractFirstTagText(text, "Name")
                If text4.Length = 0 Then
                    text4 = ExtractFirstTagText(text, "EmailAddress")
                End If
                Dim text5 = ExtractFirstTagText(text, "DateTimeReceived")
                If text5.Length = 0 Then
                    text5 = ExtractFirstTagText(text, "DateTimeSent")
                End If
                Dim dateUtc = ParseEwsDateTime(text5)
                If text3.Length > 0 Then
                    list.Add(New EwsMailSummary With {
    .ItemId = text3,
    .Subject = subject,
    .FromLine = text4,
    .DateUtc = dateUtc
})
                End If
            Next
            Return list
        End Function

        Public Shared Function TryFindFolderItems(ewsUrl As String, username As String, password As String, distinguishedFolderId As String, Optional oauthToken As String = Nothing) As List(Of EwsMailSummary)
            Dim text As String = If(Not Equals(distinguishedFolderId, Nothing), distinguishedFolderId, "inbox").Trim()
            If text.Length = 0 Then
                text = "inbox"
            End If
            Dim text2 = BuildParentFolderIdXml(text)
            Dim list As List(Of EwsMailSummary) = New List(Of EwsMailSummary)()
            Dim num = 250
            Dim num2 = 0
            Dim num3 = 0
            Do
                Dim envelope As String = "<?xml version=""1.0"" encoding=""utf-8""?><soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:t=""http://schemas.microsoft.com/exchange/services/2006/types"" xmlns:m=""http://schemas.microsoft.com/exchange/services/2006/messages""><soap:Body><m:FindItem Traversal=""Shallow""><m:ItemShape><t:BaseShape>Default</t:BaseShape></m:ItemShape><m:IndexedPageItemView MaxEntriesReturned=""" & num.ToString() & """ Offset=""" & num2.ToString() & """ BasePoint=""Beginning""/><m:ParentFolderIds>" & text2 & "</m:ParentFolderIds></m:FindItem></soap:Body></soap:Envelope>"
                Dim text3 = PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/FindItem", envelope, oauthToken)
                If String.IsNullOrEmpty(text3) Then
                    Exit Do
                End If
                Dim list2 = ParseInboxSummaries(text3)
                If list2 Is Nothing OrElse list2.Count = 0 Then
                    Exit Do
                End If
                list.AddRange(list2)
                If list2.Count < num Then
                    Exit Do
                End If
                num2 += list2.Count
                num3 += 1
            Loop While num3 <= 199
            If list.Count = 0 Then
                If String.IsNullOrWhiteSpace(LastError) Then
                    LastError = "FindItem returned no messages."
                End If
                Return New List(Of EwsMailSummary)()
            End If
            Return list
        End Function

        Public Shared Function TryFindInboxSummaries(ewsUrl As String, username As String, password As String, Optional oauthToken As String = Nothing) As List(Of EwsMailSummary)
            Dim text = TryFindInboxItems(ewsUrl, username, password, oauthToken)
            If String.IsNullOrEmpty(text) Then
                Return Nothing
            End If
            Return ParseInboxSummaries(text)
        End Function

        Public Shared Function TryGetItemBody(ewsUrl As String, username As String, password As String, itemId As String, Optional oauthToken As String = Nothing) As EwsMessageBody
            LastError = Nothing
            Dim text As String = If(Not Equals(itemId, Nothing), itemId, "").Trim()
            If text.Length = 0 Then
                LastError = "Item id is required."
                Return Nothing
            End If
            Dim text2 = SecurityElement.Escape(text)
            Dim envelope = "<?xml version=""1.0"" encoding=""utf-8""?><soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:t=""http://schemas.microsoft.com/exchange/services/2006/types"" xmlns:m=""http://schemas.microsoft.com/exchange/services/2006/messages""><soap:Body><m:GetItem><m:ItemShape><t:BaseShape>Default</t:BaseShape><t:IncludeMimeContent>false</t:IncludeMimeContent></m:ItemShape><m:ItemIds><t:ItemId Id=""" & text2 & """/></m:ItemIds></m:GetItem></soap:Body></soap:Envelope>"
            Dim text3 = PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/GetItem", envelope, oauthToken)
            If String.IsNullOrEmpty(text3) Then
                Return Nothing
            End If
            Dim ewsMessageBody As EwsMessageBody = New EwsMessageBody()
            ewsMessageBody.Subject = ExtractXmlElementText(text3, "Subject")
            ewsMessageBody.BodyText = ExtractXmlElementText(text3, "Body")
            If ewsMessageBody.BodyText.Length = 0 Then
                ewsMessageBody.BodyText = ExtractXmlElementText(text3, "t:Body")
            End If
            ewsMessageBody.Attachments = ParseFileAttachments(text3)
            If ewsMessageBody.Attachments Is Nothing Then
                ewsMessageBody.Attachments = New List(Of EwsAttachmentInfo)()
            End If
            Return ewsMessageBody
        End Function

        Public Shared Function TryGetAttachmentBytes(ewsUrl As String, username As String, password As String, attachmentId As String, Optional oauthToken As String = Nothing) As Byte()
            LastError = Nothing
            Dim text As String = If(Not Equals(attachmentId, Nothing), attachmentId, "").Trim()
            Dim result As Byte()
            If text.Length = 0 Then
                result = Nothing
            Else
                Dim text2 = SecurityElement.Escape(text)
                Dim envelope = "<?xml version=""1.0"" encoding=""utf-8""?><soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:t=""http://schemas.microsoft.com/exchange/services/2006/types"" xmlns:m=""http://schemas.microsoft.com/exchange/services/2006/messages""><soap:Body><m:GetAttachment><m:AttachmentIds><t:AttachmentId Id=""" & text2 & """/></m:AttachmentIds></m:GetAttachment></soap:Body></soap:Envelope>"
                Dim text3 = PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/GetAttachment", envelope, oauthToken)
                If String.IsNullOrEmpty(text3) Then
                    result = Nothing
                Else
                    Dim text4 = ExtractXmlElementText(text3, "Content")
                    If text4.Length = 0 Then
                        text4 = ExtractFirstTagText(text3, "Content")
                    End If
                    If text4.Length = 0 Then
                        result = Nothing
                    Else
                        Try
                            result = Convert.FromBase64String(text4)
                        Catch ex As Exception
                            ProjectData.SetProjectError(ex)
                            Dim ex2 = ex
                            LastError = ex2.Message
                            result = Nothing
                            Call ProjectData.ClearProjectError()
                        End Try
                    End If
                End If
            End If
            Return result
        End Function

        Public Shared Function TryMoveItems(ewsUrl As String, username As String, password As String, itemIds As IList(Of String), toDistinguishedFolderId As String, Optional oauthToken As String = Nothing) As Boolean
            LastError = Nothing
            If itemIds Is Nothing OrElse itemIds.Count = 0 Then
                Return True
            End If
            Dim text As String = BuildDestinationFolderIdXml(If(Not Equals(toDistinguishedFolderId, Nothing), toDistinguishedFolderId, "inbox").Trim())
            Dim stringBuilder As StringBuilder = New StringBuilder()
            For Each itemId In itemIds
                Dim text2 As String = SecurityElement.Escape(If(Not Equals(itemId, Nothing), itemId, "").Trim())
                If text2.Length <> 0 Then
                    stringBuilder.Append("<t:ItemId Id=""").Append(text2).Append("""/>")
                End If
            Next
            If stringBuilder.Length = 0 Then
                Return True
            End If
            Dim envelope As String = "<?xml version=""1.0"" encoding=""utf-8""?><soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:t=""http://schemas.microsoft.com/exchange/services/2006/types"" xmlns:m=""http://schemas.microsoft.com/exchange/services/2006/messages""><soap:Body><m:MoveItem><m:ToFolderId>" & text & "</m:ToFolderId><m:ItemIds>" & stringBuilder.ToString() & "</m:ItemIds></m:MoveItem></soap:Body></soap:Envelope>"
            Dim text3 = PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/MoveItem", envelope, oauthToken)
            If String.IsNullOrEmpty(text3) Then
                Return False
            End If
            Return text3.IndexOf("ResponseClass=""Error""", StringComparison.OrdinalIgnoreCase) < 0
        End Function

        Public Shared Function TryCopyItems(ewsUrl As String, username As String, password As String, itemIds As IList(Of String), toDistinguishedFolderId As String, Optional oauthToken As String = Nothing) As Boolean
            LastError = Nothing
            If itemIds Is Nothing OrElse itemIds.Count = 0 Then
                Return True
            End If
            Dim text As String = BuildDestinationFolderIdXml(If(Not Equals(toDistinguishedFolderId, Nothing), toDistinguishedFolderId, "inbox").Trim())
            Dim stringBuilder As StringBuilder = New StringBuilder()
            For Each itemId In itemIds
                Dim text2 As String = SecurityElement.Escape(If(Not Equals(itemId, Nothing), itemId, "").Trim())
                If text2.Length <> 0 Then
                    stringBuilder.Append("<t:ItemId Id=""").Append(text2).Append("""/>")
                End If
            Next
            If stringBuilder.Length = 0 Then
                Return True
            End If
            Dim envelope As String = "<?xml version=""1.0"" encoding=""utf-8""?><soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:t=""http://schemas.microsoft.com/exchange/services/2006/types"" xmlns:m=""http://schemas.microsoft.com/exchange/services/2006/messages""><soap:Body><m:CopyItem><m:ToFolderId>" & text & "</m:ToFolderId><m:ItemIds>" & stringBuilder.ToString() & "</m:ItemIds></m:CopyItem></soap:Body></soap:Envelope>"
            Dim text3 = PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/CopyItem", envelope, oauthToken)
            If String.IsNullOrEmpty(text3) Then
                Return False
            End If
            Return text3.IndexOf("ResponseClass=""Error""", StringComparison.OrdinalIgnoreCase) < 0
        End Function

        Public Shared Function TryAddItemCategory(ewsUrl As String, username As String, password As String, itemId As String, category As String, Optional oauthToken As String = Nothing) As Boolean
            LastError = Nothing
            Dim text As String = If(Not Equals(itemId, Nothing), itemId, "").Trim()
            Dim text2 As String = If(Not Equals(category, Nothing), category, "").Trim()
            If text.Length = 0 OrElse text2.Length = 0 Then
                Return False
            End If
            Dim text3 = SecurityElement.Escape(text)
            Dim text4 = SecurityElement.Escape(text2)
            Dim envelope = "<?xml version=""1.0"" encoding=""utf-8""?><soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:t=""http://schemas.microsoft.com/exchange/services/2006/types"" xmlns:m=""http://schemas.microsoft.com/exchange/services/2006/messages""><soap:Body><m:UpdateItem ConflictResolution=""AlwaysOverwrite"" MessageDisposition=""SaveOnly""><m:ItemChanges><t:ItemChange><t:ItemId Id=""" & text3 & """/><t:Updates><t:SetItemField FieldURI=""item:Categories""><t:Message><t:Categories><t:String>" & text4 & "</t:String></t:Categories></t:Message></t:SetItemField></t:Updates></t:ItemChange></m:ItemChanges></m:UpdateItem></soap:Body></soap:Envelope>"
            Dim text5 = PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/UpdateItem", envelope, oauthToken)
            If String.IsNullOrEmpty(text5) Then
                Return False
            End If
            Return text5.IndexOf("ResponseClass=""Error""", StringComparison.OrdinalIgnoreCase) < 0
        End Function

        Public Shared Function TryFindMailSubfolders(ewsUrl As String, username As String, password As String, Optional oauthToken As String = Nothing) As List(Of EwsFolderInfo)
            LastError = Nothing
            Dim envelope = "<?xml version=""1.0"" encoding=""utf-8""?><soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:t=""http://schemas.microsoft.com/exchange/services/2006/types"" xmlns:m=""http://schemas.microsoft.com/exchange/services/2006/messages""><soap:Body><m:FindFolder Traversal=""Shallow""><m:FolderShape><t:BaseShape>Default</t:BaseShape></m:FolderShape><m:ParentFolderIds><t:DistinguishedFolderId Id=""msgfolderroot""/></m:ParentFolderIds></m:FindFolder></soap:Body></soap:Envelope>"
            Dim text = PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/FindFolder", envelope, oauthToken)
            If String.IsNullOrEmpty(text) Then
                Return New List(Of EwsFolderInfo)()
            End If
            Return ParseFolderList(text)
        End Function

        Private Shared Function ParseFolderList(soapXml As String) As List(Of EwsFolderInfo)
            Dim list As List(Of EwsFolderInfo) = New List(Of EwsFolderInfo)()
            If String.IsNullOrWhiteSpace(soapXml) Then
                Return list
            End If
            Dim array = soapXml.Split(New String(0) {"<t:Folder>"}, StringSplitOptions.None)
            Dim num = array.Length - 1
            For i = 1 To num
                Dim text = array(i)
                Dim text2 = "FolderId Id="""
                Dim num2 = text.IndexOf(text2, StringComparison.OrdinalIgnoreCase)
                If num2 < 0 Then
                    Continue For
                End If
                num2 += text2.Length
                Dim num3 = text.IndexOf(""""c, num2)
                If num3 > num2 Then
                    Dim text3 = text.Substring(num2, num3 - num2)
                    Dim displayName = ExtractFirstTagText(text, "DisplayName")
                    If text3.Length <> 0 Then
                        list.Add(New EwsFolderInfo With {
    .FolderId = text3,
    .DisplayName = displayName
})
                    End If
                End If
            Next
            Return list
        End Function

        Public Shared Function TryMarkItemsRead(ewsUrl As String, username As String, password As String, itemIds As IList(Of String), isRead As Boolean, Optional oauthToken As String = Nothing) As Boolean
            If itemIds Is Nothing OrElse itemIds.Count = 0 Then
                Return True
            End If
            Dim stringBuilder As StringBuilder = New StringBuilder()
            For Each itemId In itemIds
                Dim text As String = SecurityElement.Escape(If(Not Equals(itemId, Nothing), itemId, "").Trim())
                If text.Length <> 0 Then
                    stringBuilder.Append("<t:ItemId Id=""").Append(text).Append("""/>")
                End If
            Next
            If stringBuilder.Length = 0 Then
                Return True
            End If
            Dim value = If(isRead, "true", "false")
            Dim stringBuilder2 As StringBuilder = New StringBuilder()
            For Each itemId2 In itemIds
                Dim text2 As String = SecurityElement.Escape(If(Not Equals(itemId2, Nothing), itemId2, "").Trim())
                If text2.Length <> 0 Then
                    stringBuilder2.Append("<t:ItemChange><t:ItemId Id=""").Append(text2).Append("""/>")
                    stringBuilder2.Append("<t:Updates><t:SetItemField FieldURI=""message:IsRead"">")
                    stringBuilder2.Append("<t:Message><t:IsRead>").Append(value).Append("</t:IsRead></t:Message>")
                    stringBuilder2.Append("</t:SetItemField></t:Updates></t:ItemChange>")
                End If
            Next
            If stringBuilder2.Length = 0 Then
                Return True
            End If
            Dim envelope As String = "<?xml version=""1.0"" encoding=""utf-8""?><soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:t=""http://schemas.microsoft.com/exchange/services/2006/types"" xmlns:m=""http://schemas.microsoft.com/exchange/services/2006/messages""><soap:Body><m:UpdateItem ConflictResolution=""AlwaysOverwrite"" MessageDisposition=""SaveOnly""><m:ItemChanges>" & stringBuilder2.ToString() & "</m:ItemChanges></m:UpdateItem></soap:Body></soap:Envelope>"
            Dim text3 = PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/UpdateItem", envelope, oauthToken)
            If String.IsNullOrEmpty(text3) Then
                Return False
            End If
            Return text3.IndexOf("ResponseClass=""Error""", StringComparison.OrdinalIgnoreCase) < 0
        End Function

        Private Shared Function ParseFileAttachments(soapXml As String) As List(Of EwsAttachmentInfo)
            Dim list As List(Of EwsAttachmentInfo) = New List(Of EwsAttachmentInfo)()
            If String.IsNullOrWhiteSpace(soapXml) Then
                Return list
            End If
            Dim text = "FileAttachment"
            Dim num = 0
            While num < soapXml.Length
                Dim num2 = soapXml.IndexOf(text, num, StringComparison.OrdinalIgnoreCase)
                If num2 < 0 Then
                    Exit While
                End If
                Dim num3 = soapXml.IndexOf(text, num2 + text.Length, StringComparison.OrdinalIgnoreCase)
                If num3 < 0 Then
                    num3 = Math.Min(soapXml.Length, num2 + 4000)
                End If
                Dim chunk = soapXml.Substring(num2, num3 - num2)
                Dim text2 = ExtractFirstAttachmentId(chunk)
                Dim fileName = ExtractFirstTagText(chunk, "Name")
                If text2.Length > 0 Then
                    list.Add(New EwsAttachmentInfo With {
    .AttachmentId = text2,
    .FileName = fileName
})
                End If
                num = num2 + text.Length
            End While
            Return list
        End Function

        Private Shared Function ExtractFirstAttachmentId(chunk As String) As String
            Dim text = "AttachmentId Id="""
            Dim num = chunk.IndexOf(text, StringComparison.OrdinalIgnoreCase)
            If num < 0 Then
                Return ""
            End If
            num += text.Length
            Dim num2 = chunk.IndexOf(""""c, num)
            If num2 <= num Then
                Return ""
            End If
            Return chunk.Substring(num, num2 - num)
        End Function

        Public Shared Function TrySendMessage(ewsUrl As String, username As String, password As String, toAddress As String, subject As String, bodyText As String, Optional oauthToken As String = Nothing) As Boolean
            LastError = Nothing
            Dim text As String = If(Not Equals(toAddress, Nothing), toAddress, "").Trim()
            If text.Length = 0 Then
                LastError = "To address is required."
                Return False
            End If
            Dim text2 = SecurityElement.Escape(If(Not Equals(subject, Nothing), subject, ""))
            Dim text3 = SecurityElement.Escape(If(Not Equals(bodyText, Nothing), bodyText, ""))
            Dim text4 = SecurityElement.Escape(text)
            Dim envelope = "<?xml version=""1.0"" encoding=""utf-8""?><soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:t=""http://schemas.microsoft.com/exchange/services/2006/types"" xmlns:m=""http://schemas.microsoft.com/exchange/services/2006/messages""><soap:Body><m:CreateItem MessageDisposition=""SendAndSaveCopy""><m:Items><t:Message><t:Subject>" & text2 & "</t:Subject><t:Body BodyType=""Text"">" & text3 & "</t:Body><t:ToRecipients><t:Mailbox><t:EmailAddress>" & text4 & "</t:EmailAddress></t:Mailbox></t:ToRecipients></t:Message></m:Items></m:CreateItem></soap:Body></soap:Envelope>"
            Dim text5 = PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/CreateItem", envelope, oauthToken)
            If String.IsNullOrEmpty(text5) Then
                Return False
            End If
            If text5.IndexOf("ResponseClass=""Error""", StringComparison.OrdinalIgnoreCase) >= 0 Then
                LastError = ExtractXmlElementText(text5, "Message")
                If LastError.Length = 0 Then
                    LastError = "EWS CreateItem failed."
                End If
                Return False
            End If
            Return True
        End Function

        Public Shared Function TrySendMessageHtml(ewsUrl As String, username As String, password As String, toAddress As String, subject As String, bodyHtml As String, bodyText As String, Optional oauthToken As String = Nothing) As Boolean
            LastError = Nothing
            Dim text As String = If(Not Equals(toAddress, Nothing), toAddress, "").Trim()
            If text.Length = 0 Then
                LastError = "To address is required."
                Return False
            End If
            Dim text2 As String = If(Not Equals(bodyHtml, Nothing), bodyHtml, "").Trim()
            Dim str As String = If(Not Equals(bodyText, Nothing), bodyText, "").Trim()
            Dim text3 = "Text"
            Dim text4 = SecurityElement.Escape(str)
            If text2.Length > 0 Then
                text3 = "HTML"
                text4 = SecurityElement.Escape(text2)
            End If
            Dim text5 = SecurityElement.Escape(If(Not Equals(subject, Nothing), subject, ""))
            Dim text6 = SecurityElement.Escape(text)
            Dim envelope = "<?xml version=""1.0"" encoding=""utf-8""?><soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:t=""http://schemas.microsoft.com/exchange/services/2006/types"" xmlns:m=""http://schemas.microsoft.com/exchange/services/2006/messages""><soap:Body><m:CreateItem MessageDisposition=""SendAndSaveCopy""><m:Items><t:Message><t:Subject>" & text5 & "</t:Subject><t:Body BodyType=""" & text3 & """>" & text4 & "</t:Body><t:ToRecipients><t:Mailbox><t:EmailAddress>" & text6 & "</t:EmailAddress></t:Mailbox></t:ToRecipients></t:Message></m:Items></m:CreateItem></soap:Body></soap:Envelope>"
            Dim text7 = PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/CreateItem", envelope, oauthToken)
            If String.IsNullOrEmpty(text7) Then
                Return False
            End If
            If text7.IndexOf("ResponseClass=""Error""", StringComparison.OrdinalIgnoreCase) >= 0 Then
                LastError = ExtractXmlElementText(text7, "Message")
                If LastError.Length = 0 Then
                    LastError = "EWS CreateItem failed."
                End If
                Return False
            End If
            Return True
        End Function

        Public Shared Function TryDeleteItems(ewsUrl As String, username As String, password As String, itemIds As IList(Of String), Optional oauthToken As String = Nothing) As Boolean
            LastError = Nothing
            If itemIds Is Nothing OrElse itemIds.Count = 0 Then
                Return True
            End If
            Dim stringBuilder As StringBuilder = New StringBuilder()
            For Each itemId In itemIds
                Dim text As String = SecurityElement.Escape(If(Not Equals(itemId, Nothing), itemId, "").Trim())
                If text.Length <> 0 Then
                    stringBuilder.Append("<t:ItemId Id=""").Append(text).Append("""/>")
                End If
            Next
            If stringBuilder.Length = 0 Then
                Return True
            End If
            Dim envelope As String = "<?xml version=""1.0"" encoding=""utf-8""?><soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:t=""http://schemas.microsoft.com/exchange/services/2006/types"" xmlns:m=""http://schemas.microsoft.com/exchange/services/2006/messages""><soap:Body><m:DeleteItem DeleteType=""MoveToDeletedItems""><m:ItemIds>" & stringBuilder.ToString() & "</m:ItemIds></m:DeleteItem></soap:Body></soap:Envelope>"
            Dim text2 = PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/DeleteItem", envelope, oauthToken)
            If String.IsNullOrEmpty(text2) Then
                Return False
            End If
            Return text2.IndexOf("ResponseClass=""Error""", StringComparison.OrdinalIgnoreCase) < 0
        End Function

        Public Shared Function TryGetCalendarEventDetails(ewsUrl As String, username As String, password As String, itemId As String, Optional oauthToken As String = Nothing) As EwsCalendarEventDetails
            LastError = Nothing
            Dim text As String = If(Not Equals(itemId, Nothing), itemId, "").Trim()
            If text.Length = 0 Then
                LastError = "Item id is required."
                Return Nothing
            End If
            Dim text2 = SecurityElement.Escape(text)
            Dim envelope = "<?xml version=""1.0"" encoding=""utf-8""?><soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:t=""http://schemas.microsoft.com/exchange/services/2006/types"" xmlns:m=""http://schemas.microsoft.com/exchange/services/2006/messages""><soap:Body><m:GetItem><m:ItemShape><t:BaseShape>Default</t:BaseShape></m:ItemShape><m:ItemIds><t:ItemId Id=""" & text2 & """/></m:ItemIds></m:GetItem></soap:Body></soap:Envelope>"
            Dim text3 = PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/GetItem", envelope, oauthToken)
            If String.IsNullOrEmpty(text3) Then
                Return Nothing
            End If
            Dim ewsCalendarEventDetails As EwsCalendarEventDetails = New EwsCalendarEventDetails()
            ewsCalendarEventDetails.ItemId = text
            ExtractItemIdAndChangeKey(text3, ewsCalendarEventDetails.ItemId, ewsCalendarEventDetails.ChangeKey)
            ewsCalendarEventDetails.Subject = ExtractXmlElementText(text3, "Subject")
            ewsCalendarEventDetails.Location = ExtractFirstTagText(text3, "Location")
            ewsCalendarEventDetails.Uid = ExtractFirstTagText(text3, "UID")
            If ewsCalendarEventDetails.Uid.Length = 0 Then
                ewsCalendarEventDetails.Uid = text
            End If
            Dim raw = ExtractFirstTagText(text3, "LastModifiedTime")
            ewsCalendarEventDetails.LastModifiedUtc = ParseEwsDateTime(raw)
            Dim raw2 = ExtractFirstTagText(text3, "Start")
            Dim raw3 = ExtractFirstTagText(text3, "End")
            ewsCalendarEventDetails.StartUtc = ParseEwsDateTime(raw2)
            ewsCalendarEventDetails.EndUtc = ParseEwsDateTime(raw3)
            If Date.Compare(ewsCalendarEventDetails.EndUtc, Date.MinValue) = 0 AndAlso Date.Compare(ewsCalendarEventDetails.StartUtc, Date.MinValue) <> 0 Then
                ewsCalendarEventDetails.EndUtc = ewsCalendarEventDetails.StartUtc.AddHours(1.0)
            End If
            Dim text4 = ExtractFirstTagText(text3, "ReminderMinutesBefore")
            If text4.Length = 0 Then
                text4 = ExtractFirstTagText(text3, "ReminderDueBy")
            End If
            Dim result = 0
            If Integer.TryParse(text4, result) AndAlso result > 0 Then
                ewsCalendarEventDetails.AlarmMinutesBefore = result
            Else
                Dim dateTime = ParseEwsDateTime(text4)
                If Date.Compare(dateTime, Date.MinValue) <> 0 AndAlso Date.Compare(ewsCalendarEventDetails.StartUtc, Date.MinValue) <> 0 AndAlso Date.Compare(dateTime, ewsCalendarEventDetails.StartUtc) < 0 Then
                    ewsCalendarEventDetails.AlarmMinutesBefore = CInt(Math.Round(Math.Max(1.0, Math.Round((ewsCalendarEventDetails.StartUtc - dateTime).TotalMinutes))))
                End If
            End If
            ewsCalendarEventDetails.Category = ExtractFirstCalendarCategory(text3)
            Dim num = If(String.IsNullOrEmpty(text3), -1, text3.IndexOf("<t:Recurrence", StringComparison.OrdinalIgnoreCase))
            If num < 0 Then
                num = If(String.IsNullOrEmpty(text3), -1, text3.IndexOf("<Recurrence", StringComparison.OrdinalIgnoreCase))
            End If
            ewsCalendarEventDetails.HasRecurrence = num >= 0
            If ewsCalendarEventDetails.HasRecurrence Then
                Dim num2 = text3.IndexOf("</t:Recurrence>", num, StringComparison.OrdinalIgnoreCase)
                If num2 < 0 Then
                    num2 = text3.IndexOf("</Recurrence>", num, StringComparison.OrdinalIgnoreCase)
                End If
                If num2 > num Then
                    Dim text5 = text3.Substring(num, Math.Min(256, num2 - num + 16))
                    ewsCalendarEventDetails.RecurrenceSignature = text5.Replace(vbCr, " ").Replace(vbLf, " ").Trim()
                End If
            End If
            Return ewsCalendarEventDetails
        End Function

        Private Shared Function ExtractFirstCalendarCategory(soapXml As String) As String
            If String.IsNullOrWhiteSpace(soapXml) Then
                Return ""
            End If
            Dim num = soapXml.IndexOf("<t:Categories>", StringComparison.OrdinalIgnoreCase)
            If num < 0 Then
                Return ""
            End If
            Dim chunk = soapXml.Substring(num, Math.Min(512, soapXml.Length - num))
            Return ExtractFirstTagText(chunk, "String")
        End Function

        Private Shared Function ParseEwsDateTime(raw As String) As Date
            Dim text As String = If(Not Equals(raw, Nothing), raw, "").Trim()
            If text.Length = 0 Then
                Return Date.MinValue
            End If
            Dim result As Date = Nothing
            Try

                If Date.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, result) Then
                    Return result.ToUniversalTime()
                End If
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
            Return Date.MinValue
        End Function

        Public Shared Function TryFindCalendarSummaries(ewsUrl As String, username As String, password As String, Optional oauthToken As String = Nothing) As List(Of EwsCalendarSummary)
            Dim envelope = "<?xml version=""1.0"" encoding=""utf-8""?><soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:t=""http://schemas.microsoft.com/exchange/services/2006/types"" xmlns:m=""http://schemas.microsoft.com/exchange/services/2006/messages""><soap:Body><m:FindItem Traversal=""Shallow""><m:ItemShape><t:BaseShape>Default</t:BaseShape></m:ItemShape><m:ParentFolderIds><t:DistinguishedFolderId Id=""calendar""/></m:ParentFolderIds></m:FindItem></soap:Body></soap:Envelope>"
            Dim text = PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/FindItem", envelope, oauthToken)
            If String.IsNullOrEmpty(text) Then
                Return Nothing
            End If
            Return ParseCalendarSummaries(text)
        End Function

        Public Shared Function ParseCalendarSummaries(soapXml As String) As List(Of EwsCalendarSummary)
            Dim list As List(Of EwsCalendarSummary) = New List(Of EwsCalendarSummary)()
            If String.IsNullOrWhiteSpace(soapXml) Then
                Return list
            End If
            Dim array = soapXml.Split(New String(0) {"<t:Subject>"}, StringSplitOptions.None)
            Dim num = array.Length - 1
            For i = 1 To num
                Dim text = array(i)
                Dim num2 = text.IndexOf("</t:Subject>", StringComparison.OrdinalIgnoreCase)
                Dim subject = ""
                If num2 > 0 Then
                    subject = text.Substring(0, num2)
                End If
                Dim startText = ExtractFirstTagText(text, "Start")
                Dim itemId = ""
                Dim changeKey = ""
                ExtractItemIdAndChangeKey(text, itemId, changeKey)
                If itemId.Length > 0 Then
                    list.Add(New EwsCalendarSummary With {
    .ItemId = itemId,
    .ChangeKey = changeKey,
    .Subject = subject,
    .StartText = startText
})
                End If
            Next
            Return list
        End Function

        Public Shared Function TryResolveContacts(ewsUrl As String, username As String, password As String, query As String, Optional oauthToken As String = Nothing) As List(Of EwsResolvedContact)
            LastError = Nothing
            Dim text As String = If(Not Equals(query, Nothing), query, "").Trim()
            If text.Length = 0 Then
                LastError = "Contact search query is required."
                Return Nothing
            End If
            Dim text2 = SecurityElement.Escape(text)
            Dim envelope = "<?xml version=""1.0"" encoding=""utf-8""?><soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:t=""http://schemas.microsoft.com/exchange/services/2006/types"" xmlns:m=""http://schemas.microsoft.com/exchange/services/2006/messages""><soap:Body><m:ResolveNames ReturnFullContactInformation=""true""><m:UnresolvedEntry>" & text2 & "</m:UnresolvedEntry></m:ResolveNames></soap:Body></soap:Envelope>"
            Dim text3 = PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/ResolveNames", envelope, oauthToken)
            If String.IsNullOrEmpty(text3) Then
                Return Nothing
            End If
            Return ParseResolvedContacts(text3)
        End Function

        Public Shared Function TryCreateCalendarEvent(ewsUrl As String, username As String, password As String, ev As LcarsCalendarBridge.SharedCalendarEvent, Optional oauthToken As String = Nothing, Optional ByRef changeKey As String = Nothing) As String
            LastError = Nothing
            changeKey = Nothing
            If ev Is Nothing Then
                Return Nothing
            End If
            Dim sharedCalendarEvent = ev
            Dim text = SecurityElement.Escape(If(Not Equals(sharedCalendarEvent.Summary, Nothing), sharedCalendarEvent.Summary, "LCARS EVENT"))
            Dim text2 As String = ev.StartUtc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.000Z")
            Dim text3 As String = ev.EndUtc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.000Z")
            Dim text4 = ""
            sharedCalendarEvent = ev
            Dim text5 As String = If(Not Equals(sharedCalendarEvent.Location, Nothing), sharedCalendarEvent.Location, "").Trim()
            If text5.Length > 0 Then
                text4 = "<t:Location>" & SecurityElement.Escape(text5) & "</t:Location>"
            End If
            Dim text6 = BuildCalendarReminderCreateXml(ev)
            Dim text7 = BuildCalendarCategoryCreateXml(ev)
            Dim envelope = "<?xml version=""1.0"" encoding=""utf-8""?><soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:t=""http://schemas.microsoft.com/exchange/services/2006/types"" xmlns:m=""http://schemas.microsoft.com/exchange/services/2006/messages""><soap:Body><m:CreateItem SendMeetingInvitations=""SendToNone""><m:SavedItemFolderId><t:DistinguishedFolderId Id=""calendar""/></m:SavedItemFolderId><m:Items><t:CalendarItem><t:Subject>" & text & "</t:Subject>" & text4 & text7 & text6 & "<t:Start>" & text2 & "</t:Start><t:End>" & text3 & "</t:End></t:CalendarItem></m:Items></m:CreateItem></soap:Body></soap:Envelope>"
            Dim text8 = PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/CreateItem", envelope, oauthToken)
            If String.IsNullOrEmpty(text8) Then
                Return Nothing
            End If
            Dim itemId = ""
            ExtractItemIdAndChangeKey(text8, itemId, changeKey)
            If itemId.Length = 0 Then
                itemId = ExtractFirstItemId(text8)
            End If
            Return itemId
        End Function

        Public Shared Function TryUpdateCalendarEvent(ewsUrl As String, username As String, password As String, itemId As String, ev As LcarsCalendarBridge.SharedCalendarEvent, Optional oauthToken As String = Nothing) As Boolean
            LastError = Nothing
            If TryUpdateCalendarEventInternal(ewsUrl, username, password, itemId, ev, oauthToken, "AutoResolve") Then
                Return True
            End If
            Dim ewsCalendarEventDetails = TryGetCalendarEventDetails(ewsUrl, username, password, itemId, oauthToken)
            If ewsCalendarEventDetails IsNot Nothing AndAlso ewsCalendarEventDetails.ChangeKey.Length > 0 Then
                ev.EwsChangeKey = ewsCalendarEventDetails.ChangeKey
                If Date.Compare(ewsCalendarEventDetails.LastModifiedUtc, Date.MinValue) <> 0 Then
                    ev.EwsLastModifiedUtc = ewsCalendarEventDetails.LastModifiedUtc
                End If
            End If
            Return TryUpdateCalendarEventInternal(ewsUrl, username, password, itemId, ev, oauthToken, "AlwaysOverwrite")
        End Function

        Private Shared Function BuildCalendarLocationUpdateField(ev As LcarsCalendarBridge.SharedCalendarEvent) As String
            Dim obj As Object
            If ev IsNot Nothing Then
                obj = If(Not Equals(ev.Location, Nothing), ev.Location, "")
            Else
                obj = ""
            End If
            Dim text As String = CStr(obj).Trim()
            If text.Length = 0 Then
                Return ""
            End If
            Dim text2 = SecurityElement.Escape(text)
            Return "<t:SetItemField><t:FieldURI FieldURI=""calendar:Location""/><t:CalendarItem><t:Location>" & text2 & "</t:Location></t:CalendarItem></t:SetItemField>"
        End Function

        Private Shared Function BuildCalendarReminderCreateXml(ev As LcarsCalendarBridge.SharedCalendarEvent) As String
            If ev Is Nothing OrElse ev.AlarmMinutesBefore <= 0 Then
                Return ""
            End If
            Return "<t:ReminderIsSet>true</t:ReminderIsSet><t:ReminderMinutesBefore>" & ev.AlarmMinutesBefore.ToString() & "</t:ReminderMinutesBefore>"
        End Function

        Private Shared Function BuildCalendarCategoryCreateXml(ev As LcarsCalendarBridge.SharedCalendarEvent) As String
            Dim obj As Object
            If ev IsNot Nothing Then
                obj = If(Not Equals(ev.Category, Nothing), ev.Category, "")
            Else
                obj = ""
            End If
            Dim text As String = CStr(obj).Trim()
            If text.Length = 0 Then
                Return ""
            End If
            Dim text2 = SecurityElement.Escape(text)
            Return "<t:Categories><t:String>" & text2 & "</t:String></t:Categories>"
        End Function

        Private Shared Function BuildCalendarReminderUpdateField(ev As LcarsCalendarBridge.SharedCalendarEvent) As String
            If ev Is Nothing OrElse ev.AlarmMinutesBefore <= 0 Then
                Return ""
            End If
            Return "<t:SetItemField><t:FieldURI FieldURI=""item:ReminderMinutesBefore""/><t:CalendarItem><t:ReminderIsSet>true</t:ReminderIsSet><t:ReminderMinutesBefore>" & ev.AlarmMinutesBefore.ToString() & "</t:ReminderMinutesBefore></t:CalendarItem></t:SetItemField>"
        End Function

        Private Shared Function BuildCalendarCategoryUpdateField(ev As LcarsCalendarBridge.SharedCalendarEvent) As String
            Dim obj As Object
            If ev IsNot Nothing Then
                obj = If(Not Equals(ev.Category, Nothing), ev.Category, "")
            Else
                obj = ""
            End If
            Dim text As String = CStr(obj).Trim()
            If text.Length = 0 Then
                Return ""
            End If
            Dim text2 = SecurityElement.Escape(text)
            Return "<t:SetItemField><t:FieldURI FieldURI=""item:Categories""/><t:CalendarItem><t:Categories><t:String>" & text2 & "</t:String></t:Categories></t:CalendarItem></t:SetItemField>"
        End Function

        Private Shared Function TryUpdateCalendarEventInternal(ewsUrl As String, username As String, password As String, itemId As String, ev As LcarsCalendarBridge.SharedCalendarEvent, oauthToken As String, conflictResolution As String) As Boolean
            LastError = Nothing
            Dim text As String = If(Not Equals(itemId, Nothing), itemId, "").Trim()
            If text.Length = 0 OrElse ev Is Nothing Then
                Return False
            End If
            Dim sharedCalendarEvent = ev
            Dim text2 = SecurityElement.Escape(If(Not Equals(sharedCalendarEvent.Summary, Nothing), sharedCalendarEvent.Summary, "LCARS EVENT"))
            Dim text3 As String = ev.StartUtc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.000Z")
            Dim text4 As String = ev.EndUtc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.000Z")
            Dim text5 = SecurityElement.Escape(text)
            sharedCalendarEvent = ev
            Dim text6 = SecurityElement.Escape(If(Not Equals(sharedCalendarEvent.EwsChangeKey, Nothing), sharedCalendarEvent.EwsChangeKey, ""))
            Dim text7 = "<t:ItemId Id=""" & text5 & """"
            If text6.Length > 0 Then
                text7 = text7 & " ChangeKey=""" & text6 & """"
            End If
            text7 += "/>"
            Dim str As String = (If(String.IsNullOrWhiteSpace(conflictResolution), "AutoResolve", conflictResolution.Trim()))
            Dim envelope = "<?xml version=""1.0"" encoding=""utf-8""?><soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:t=""http://schemas.microsoft.com/exchange/services/2006/types"" xmlns:m=""http://schemas.microsoft.com/exchange/services/2006/messages""><soap:Body><m:UpdateItem ConflictResolution=""" & SecurityElement.Escape(str) & """ MessageDisposition=""SaveOnly""><m:ItemChanges><t:ItemChange>" & text7 & "<t:Updates><t:SetItemField><t:FieldURI FieldURI=""item:Subject""/><t:CalendarItem><t:Subject>" & text2 & "</t:Subject></t:CalendarItem></t:SetItemField>" & BuildCalendarLocationUpdateField(ev) & BuildCalendarCategoryUpdateField(ev) & BuildCalendarReminderUpdateField(ev) & "<t:SetItemField><t:FieldURI FieldURI=""calendar:Start""/><t:CalendarItem><t:Start>" & text3 & "</t:Start></t:CalendarItem></t:SetItemField><t:SetItemField><t:FieldURI FieldURI=""calendar:End""/><t:CalendarItem><t:End>" & text4 & "</t:End></t:CalendarItem></t:SetItemField></t:Updates></t:ItemChange></m:ItemChanges></m:UpdateItem></soap:Body></soap:Envelope>"
            Dim text8 = PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/UpdateItem", envelope, oauthToken)
            If String.IsNullOrEmpty(text8) Then
                Return False
            End If
            If text8.IndexOf("ResponseClass=""Error""", StringComparison.OrdinalIgnoreCase) >= 0 Then
                Return False
            End If
            Dim itemId2 = ev.EwsItemId
            Dim changeKey = ""
            ExtractItemIdAndChangeKey(text8, itemId2, changeKey)
            If changeKey.Length > 0 Then
                ev.EwsChangeKey = changeKey
            End If
            If itemId2.Length > 0 Then
                ev.EwsItemId = itemId2
            End If
            Return True
        End Function

        Private Shared Function ExtractPersonaItemId(chunk As String) As String
            Dim text = "PersonaId Id="""
            Dim num = If(Not Equals(chunk, Nothing), chunk, "").IndexOf(text, StringComparison.OrdinalIgnoreCase)
            If num < 0 Then
                Return ""
            End If
            num += text.Length
            Dim num2 = chunk.IndexOf(""""c, num)
            If num2 <= num Then
                Return ""
            End If
            Return chunk.Substring(num, num2 - num).Trim()
        End Function

        Private Shared Function ExtractPersonaChangeKey(chunk As String) As String
            Dim value = "PersonaId Id="""
            Dim num = If(Not Equals(chunk, Nothing), chunk, "").IndexOf(value, StringComparison.OrdinalIgnoreCase)
            If num < 0 Then
                Return ""
            End If
            Dim text = "ChangeKey="""
            Dim num2 = chunk.IndexOf(text, num, StringComparison.OrdinalIgnoreCase)
            If num2 < 0 Then
                Return ""
            End If
            num2 += text.Length
            Dim num3 = chunk.IndexOf(""""c, num2)
            If num3 <= num2 Then
                Return ""
            End If
            Return chunk.Substring(num2, num3 - num2).Trim()
        End Function

        Public Shared Function TryUpdateGalPersonaContact(ewsUrl As String, username As String, password As String, contact As MailContact, Optional oauthToken As String = Nothing) As Boolean
            LastError = Nothing
            LastGalPersonaUpdateDenied = False
            If contact Is Nothing Then
                Return False
            End If
            Dim mailContact = contact
            Dim itemId As String = If(Not Equals(mailContact.GalPersonaId, Nothing), mailContact.GalPersonaId, "").Trim()
            If itemId.Length = 0 Then
                LastError = "GAL PersonaId missing."
                Return False
            End If
            mailContact = contact
            Dim text As String = If(Not Equals(mailContact.GalPersonaChangeKey, Nothing), mailContact.GalPersonaChangeKey, "").Trim()
            mailContact = contact
            Dim text2 As String = SecurityElement.Escape(If(Not Equals(mailContact.DisplayName, Nothing), mailContact.DisplayName, contact.Email).Trim())
            mailContact = contact
            Dim text3 As String = SecurityElement.Escape(If(Not Equals(mailContact.FirstName, Nothing), mailContact.FirstName, "").Trim())
            mailContact = contact
            Dim text4 As String = SecurityElement.Escape(If(Not Equals(mailContact.LastName, Nothing), mailContact.LastName, "").Trim())
            mailContact = contact
            Dim text5 As String = SecurityElement.Escape(If(Not Equals(mailContact.Company, Nothing), mailContact.Company, "").Trim())
            mailContact = contact
            Dim text6 As String = SecurityElement.Escape(If(Not Equals(mailContact.JobTitle, Nothing), mailContact.JobTitle, "").Trim())
            mailContact = contact
            Dim text7 As String = SecurityElement.Escape(If(Not Equals(mailContact.Department, Nothing), mailContact.Department, "").Trim())
            mailContact = contact
            Dim text8 As String = SecurityElement.Escape(If(Not Equals(mailContact.WorkPhone, Nothing), mailContact.WorkPhone, "").Trim())
            mailContact = contact
            Dim text9 As String = SecurityElement.Escape(If(Not Equals(mailContact.MobilePhone, Nothing), mailContact.MobilePhone, "").Trim())
            Dim text10 = If(text.Length > 0, " ChangeKey=""" & SecurityElement.Escape(text) & """", "")
            Dim stringBuilder As StringBuilder = New StringBuilder()
            stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=""persona:DisplayName""/><t:Persona><t:DisplayName>" & text2 & "</t:DisplayName></t:Persona></t:SetItemField>")
            If text3.Length > 0 Then
                stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=""persona:GivenName""/><t:Persona><t:GivenName>" & text3 & "</t:GivenName></t:Persona></t:SetItemField>")
            End If
            If text4.Length > 0 Then
                stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=""persona:Surname""/><t:Persona><t:Surname>" & text4 & "</t:Surname></t:Persona></t:SetItemField>")
            End If
            If text5.Length > 0 Then
                stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=""persona:CompanyName""/><t:Persona><t:CompanyName>" & text5 & "</t:CompanyName></t:Persona></t:SetItemField>")
            End If
            If text6.Length > 0 Then
                stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=""persona:Title""/><t:Persona><t:Title>" & text6 & "</t:Title></t:Persona></t:SetItemField>")
            End If
            If text7.Length > 0 Then
                stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=""persona:Department""/><t:Persona><t:Department>" & text7 & "</t:Department></t:Persona></t:SetItemField>")
            End If
            If text8.Length > 0 Then
                stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=""persona:BusinessPhoneNumber""/><t:Persona><t:BusinessPhoneNumber>" & text8 & "</t:BusinessPhoneNumber></t:Persona></t:SetItemField>")
            End If
            If text9.Length > 0 Then
                stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=""persona:MobilePhone""/><t:Persona><t:MobilePhone>" & text9 & "</t:MobilePhone></t:Persona></t:SetItemField>")
            End If
            Dim envelope As String = "<?xml version=""1.0"" encoding=""utf-8""?><soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:t=""http://schemas.microsoft.com/exchange/services/2006/types"" xmlns:m=""http://schemas.microsoft.com/exchange/services/2006/messages""><soap:Body><m:UpdateItem ConflictResolution=""AlwaysOverwrite"" MessageDisposition=""SaveOnly""><m:ItemChanges><t:ItemChange><t:ItemId Id=""" & SecurityElement.Escape(itemId) & """" & text10 & "/><t:Updates>" & stringBuilder.ToString() & "</t:Updates></t:ItemChange></m:ItemChanges></m:UpdateItem></soap:Body></soap:Envelope>"
            Dim text11 = PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/UpdateItem", envelope, oauthToken)
            If String.IsNullOrEmpty(text11) Then
                Return False
            End If
            If text11.IndexOf("ResponseClass=""Error""", StringComparison.OrdinalIgnoreCase) >= 0 Then
                LastGalPersonaUpdateDenied = True
                LastError = "GAL Persona UpdateItem denied (directory read-only for this mailbox)."
                Return False
            End If
            Dim changeKey = ""
            ExtractItemIdAndChangeKey(text11, itemId, changeKey)
            If changeKey.Length > 0 Then
                contact.GalPersonaChangeKey = changeKey
            End If
            Return True
        End Function

        Public Shared Function TryRefreshGalPersonaChangeKey(ewsUrl As String, username As String, password As String, contact As MailContact, Optional oauthToken As String = Nothing) As Boolean
            LastError = Nothing
            If contact Is Nothing OrElse String.IsNullOrWhiteSpace(contact.Email) Then
                Return False
            End If
            Dim list As List(Of EwsResolvedContact) = TryResolveContacts(ewsUrl, username, password, contact.Email.Trim(), oauthToken)
            If list Is Nothing OrElse list.Count = 0 Then
                Return False
            End If
            For Each item In list
                If item Is Nothing OrElse Not String.Equals(item.EmailAddress, contact.Email, StringComparison.OrdinalIgnoreCase) Then
                    Continue For
                End If
                Dim result = False
                If Not String.IsNullOrWhiteSpace(item.GalPersonaId) Then
                    contact.GalPersonaId = item.GalPersonaId.Trim()
                    result = True
                End If
                If Not String.IsNullOrWhiteSpace(item.GalPersonaChangeKey) Then
                    contact.GalPersonaChangeKey = item.GalPersonaChangeKey.Trim()
                    result = True
                End If
                Return result
            Next
            Return False
        End Function

        Public Shared Function TryRefreshGalDirectoryChangeKey(ewsUrl As String, username As String, password As String, contact As MailContact, Optional oauthToken As String = Nothing) As Boolean
            LastError = Nothing
            If contact Is Nothing OrElse String.IsNullOrWhiteSpace(contact.Email) Then
                Return False
            End If
            Dim list As List(Of EwsResolvedContact) = TryResolveContacts(ewsUrl, username, password, contact.Email.Trim(), oauthToken)
            If list Is Nothing OrElse list.Count = 0 Then
                Return False
            End If
            For Each item In list
                If item Is Nothing OrElse Not String.Equals(item.EmailAddress, contact.Email, StringComparison.OrdinalIgnoreCase) Then
                    Continue For
                End If
                Dim result = False
                If Not String.IsNullOrWhiteSpace(item.DirectoryContactItemId) Then
                    contact.GalDirectoryItemId = item.DirectoryContactItemId.Trim()
                    result = True
                End If
                If Not String.IsNullOrWhiteSpace(item.DirectoryContactChangeKey) Then
                    contact.GalDirectoryChangeKey = item.DirectoryContactChangeKey.Trim()
                    result = True
                End If
                Return result
            Next
            Return False
        End Function

        Public Shared Function TryResolveDirectoryContactItemIds(ewsUrl As String, username As String, password As String, contact As MailContact, Optional oauthToken As String = Nothing) As Boolean
            LastError = Nothing
            If contact Is Nothing OrElse String.IsNullOrWhiteSpace(contact.Email) Then
                Return False
            End If
            If Not String.IsNullOrWhiteSpace(contact.GalDirectoryItemId) Then
                Return True
            End If
            Dim list As List(Of EwsResolvedContact) = TryResolveContacts(ewsUrl, username, password, contact.Email.Trim(), oauthToken)
            If list Is Nothing OrElse list.Count = 0 Then
                Return False
            End If
            For Each item In list
                If item Is Nothing OrElse Not String.Equals(item.EmailAddress, contact.Email, StringComparison.OrdinalIgnoreCase) OrElse String.IsNullOrWhiteSpace(item.DirectoryContactItemId) Then
                    Continue For
                End If
                contact.GalDirectoryItemId = item.DirectoryContactItemId.Trim()
                Dim ewsResolvedContact = item
                contact.GalDirectoryChangeKey = If(Not Equals(ewsResolvedContact.DirectoryContactChangeKey, Nothing), ewsResolvedContact.DirectoryContactChangeKey, "").Trim()
                Return True
            Next
            Return False
        End Function

        Public Shared Function TryUpdateGalDirectoryContact(ewsUrl As String, username As String, password As String, contact As MailContact, Optional oauthToken As String = Nothing) As Boolean
            LastError = Nothing
            LastGalDirectoryUpdateDenied = False
            If contact Is Nothing Then
                Return False
            End If
            Dim mailContact = contact
            Dim itemId As String = If(Not Equals(mailContact.GalDirectoryItemId, Nothing), mailContact.GalDirectoryItemId, "").Trim()
            If itemId.Length = 0 Then
                LastError = "Directory Contact ItemId missing."
                Return False
            End If
            mailContact = contact
            Dim text As String = If(Not Equals(mailContact.GalDirectoryChangeKey, Nothing), mailContact.GalDirectoryChangeKey, "").Trim()
            Dim text2 = If(text.Length > 0, " ChangeKey=""" & SecurityElement.Escape(text) & """", "")
            mailContact = contact
            Dim text3 As String = SecurityElement.Escape(If(Not Equals(mailContact.DisplayName, Nothing), mailContact.DisplayName, contact.Email).Trim())
            mailContact = contact
            Dim text4 As String = SecurityElement.Escape(If(Not Equals(mailContact.FirstName, Nothing), mailContact.FirstName, "").Trim())
            mailContact = contact
            Dim text5 As String = SecurityElement.Escape(If(Not Equals(mailContact.LastName, Nothing), mailContact.LastName, "").Trim())
            mailContact = contact
            Dim text6 As String = SecurityElement.Escape(If(Not Equals(mailContact.Company, Nothing), mailContact.Company, "").Trim())
            mailContact = contact
            Dim text7 As String = SecurityElement.Escape(If(Not Equals(mailContact.JobTitle, Nothing), mailContact.JobTitle, "").Trim())
            mailContact = contact
            Dim text8 As String = SecurityElement.Escape(If(Not Equals(mailContact.WorkPhone, Nothing), mailContact.WorkPhone, "").Trim())
            mailContact = contact
            Dim text9 As String = SecurityElement.Escape(If(Not Equals(mailContact.MobilePhone, Nothing), mailContact.MobilePhone, "").Trim())
            mailContact = contact
            Dim text10 As String = SecurityElement.Escape(If(Not Equals(mailContact.Department, Nothing), mailContact.Department, "").Trim())
            Dim stringBuilder As StringBuilder = New StringBuilder()
            stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=""contacts:DisplayName""/><t:Contact><t:DisplayName>" & text3 & "</t:DisplayName></t:Contact></t:SetItemField>")
            If text4.Length > 0 Then
                stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=""contacts:GivenName""/><t:Contact><t:GivenName>" & text4 & "</t:GivenName></t:Contact></t:SetItemField>")
            End If
            If text5.Length > 0 Then
                stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=""contacts:Surname""/><t:Contact><t:Surname>" & text5 & "</t:Surname></t:Contact></t:SetItemField>")
            End If
            If text6.Length > 0 Then
                stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=""contacts:CompanyName""/><t:Contact><t:CompanyName>" & text6 & "</t:CompanyName></t:Contact></t:SetItemField>")
            End If
            If text7.Length > 0 Then
                stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=""contacts:JobTitle""/><t:Contact><t:JobTitle>" & text7 & "</t:JobTitle></t:Contact></t:SetItemField>")
            End If
            If text10.Length > 0 Then
                stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=""contacts:Department""/><t:Contact><t:Department>" & text10 & "</t:Department></t:Contact></t:SetItemField>")
            End If
            If text8.Length > 0 Then
                stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=""contacts:BusinessPhone""/><t:Contact><t:BusinessPhone>" & text8 & "</t:BusinessPhone></t:Contact></t:SetItemField>")
            End If
            If text9.Length > 0 Then
                stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=""contacts:MobilePhone""/><t:Contact><t:MobilePhone>" & text9 & "</t:MobilePhone></t:Contact></t:SetItemField>")
            End If
            mailContact = contact
            Dim text11 As String = SecurityElement.Escape(If(Not Equals(mailContact.SecondaryEmail, Nothing), mailContact.SecondaryEmail, "").Trim())
            If text11.Length > 0 Then
                stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=""contacts:EmailAddress"" Key=""EmailAddress2""/><t:Contact><t:EmailAddresses><t:Entry Key=""EmailAddress2"">" & text11 & "</t:Entry></t:EmailAddresses></t:Contact></t:SetItemField>")
            End If
            Dim envelope As String = "<?xml version=""1.0"" encoding=""utf-8""?><soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:t=""http://schemas.microsoft.com/exchange/services/2006/types"" xmlns:m=""http://schemas.microsoft.com/exchange/services/2006/messages""><soap:Body><m:UpdateItem ConflictResolution=""AlwaysOverwrite"" MessageDisposition=""SaveOnly""><m:ItemChanges><t:ItemChange><t:ItemId Id=""" & SecurityElement.Escape(itemId) & """" & text2 & "/><t:Updates>" & stringBuilder.ToString() & "</t:Updates></t:ItemChange></m:ItemChanges></m:UpdateItem></soap:Body></soap:Envelope>"
            Dim text12 = PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/UpdateItem", envelope, oauthToken)
            If String.IsNullOrEmpty(text12) Then
                Return False
            End If
            If text12.IndexOf("ResponseClass=""Error""", StringComparison.OrdinalIgnoreCase) >= 0 Then
                LastGalDirectoryUpdateDenied = True
                LastError = "Directory Contact UpdateItem denied (GAL read-only or insufficient rights)."
                Return False
            End If
            Dim changeKey = ""
            ExtractItemIdAndChangeKey(text12, itemId, changeKey)
            If changeKey.Length > 0 Then
                contact.GalDirectoryChangeKey = changeKey
            End If
            Return True
        End Function

        Public Shared Function TryUpdatePersonalContactShadow(ewsUrl As String, username As String, password As String, contact As MailContact, Optional oauthToken As String = Nothing) As Boolean
            LastError = Nothing
            If contact Is Nothing OrElse String.IsNullOrWhiteSpace(contact.EwsPersonalItemId) Then
                LastError = "Personal contact ItemId missing."
                Return False
            End If
            Dim str As String = contact.EwsPersonalItemId.Trim()
            Dim mailContact = contact
            Dim text As String = SecurityElement.Escape(If(Not Equals(mailContact.DisplayName, Nothing), mailContact.DisplayName, contact.Email).Trim())
            mailContact = contact
            Dim text2 As String = SecurityElement.Escape(If(Not Equals(mailContact.FirstName, Nothing), mailContact.FirstName, "").Trim())
            mailContact = contact
            Dim text3 As String = SecurityElement.Escape(If(Not Equals(mailContact.LastName, Nothing), mailContact.LastName, "").Trim())
            mailContact = contact
            Dim text4 As String = SecurityElement.Escape(If(Not Equals(mailContact.Company, Nothing), mailContact.Company, "").Trim())
            mailContact = contact
            Dim text5 As String = SecurityElement.Escape(If(Not Equals(mailContact.JobTitle, Nothing), mailContact.JobTitle, "").Trim())
            mailContact = contact
            Dim text6 As String = SecurityElement.Escape(If(Not Equals(mailContact.WorkPhone, Nothing), mailContact.WorkPhone, "").Trim())
            mailContact = contact
            Dim text7 As String = SecurityElement.Escape(If(Not Equals(mailContact.MobilePhone, Nothing), mailContact.MobilePhone, "").Trim())
            mailContact = contact
            Dim text8 As String = SecurityElement.Escape(If(Not Equals(mailContact.Department, Nothing), mailContact.Department, "").Trim())
            mailContact = contact
            Dim text9 As String = SecurityElement.Escape(If(Not Equals(mailContact.SecondaryEmail, Nothing), mailContact.SecondaryEmail, "").Trim())
            Dim stringBuilder As StringBuilder = New StringBuilder()
            stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=""contacts:DisplayName""/><t:Contact><t:DisplayName>" & text & "</t:DisplayName></t:Contact></t:SetItemField>")
            If text2.Length > 0 Then
                stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=""contacts:GivenName""/><t:Contact><t:GivenName>" & text2 & "</t:GivenName></t:Contact></t:SetItemField>")
            End If
            If text3.Length > 0 Then
                stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=""contacts:Surname""/><t:Contact><t:Surname>" & text3 & "</t:Surname></t:Contact></t:SetItemField>")
            End If
            If text4.Length > 0 Then
                stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=""contacts:CompanyName""/><t:Contact><t:CompanyName>" & text4 & "</t:CompanyName></t:Contact></t:SetItemField>")
            End If
            If text5.Length > 0 Then
                stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=""contacts:JobTitle""/><t:Contact><t:JobTitle>" & text5 & "</t:JobTitle></t:Contact></t:SetItemField>")
            End If
            If text8.Length > 0 Then
                stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=""contacts:Department""/><t:Contact><t:Department>" & text8 & "</t:Department></t:Contact></t:SetItemField>")
            End If
            If text6.Length > 0 Then
                stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=""contacts:BusinessPhone""/><t:Contact><t:BusinessPhone>" & text6 & "</t:BusinessPhone></t:Contact></t:SetItemField>")
            End If
            If text7.Length > 0 Then
                stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=""contacts:MobilePhone""/><t:Contact><t:MobilePhone>" & text7 & "</t:MobilePhone></t:Contact></t:SetItemField>")
            End If
            If text9.Length > 0 Then
                stringBuilder.Append("<t:SetItemField><t:FieldURI FieldURI=""contacts:EmailAddress"" Key=""EmailAddress2""/><t:Contact><t:EmailAddresses><t:Entry Key=""EmailAddress2"">" & text9 & "</t:Entry></t:EmailAddresses></t:Contact></t:SetItemField>")
            End If
            Dim envelope As String = "<?xml version=""1.0"" encoding=""utf-8""?><soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:t=""http://schemas.microsoft.com/exchange/services/2006/types"" xmlns:m=""http://schemas.microsoft.com/exchange/services/2006/messages""><soap:Body><m:UpdateItem ConflictResolution=""AlwaysOverwrite"" MessageDisposition=""SaveOnly""><m:ItemChanges><t:ItemChange><t:ItemId Id=""" & SecurityElement.Escape(str) & """/><t:Updates>" & stringBuilder.ToString() & "</t:Updates></t:ItemChange></m:ItemChanges></m:UpdateItem></soap:Body></soap:Envelope>"
            Dim text10 = PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/UpdateItem", envelope, oauthToken)
            If String.IsNullOrEmpty(text10) Then
                Return False
            End If
            If text10.IndexOf("ResponseClass=""Error""", StringComparison.OrdinalIgnoreCase) >= 0 Then
                LastError = "Personal contact UpdateItem failed."
                Return False
            End If
            Return True
        End Function

        Public Shared Function TryCreatePersonalContactShadow(ewsUrl As String, username As String, password As String, contact As MailContact, Optional oauthToken As String = Nothing) As String
            LastError = Nothing
            If contact Is Nothing OrElse String.IsNullOrWhiteSpace(contact.Email) Then
                LastError = "Contact email required."
                Return Nothing
            End If
            Dim text As String = contact.Email.Trim()
            Dim mailContact = contact
            Dim text2 As String = SecurityElement.Escape(If(Not Equals(mailContact.FirstName, Nothing), mailContact.FirstName, "").Trim())
            mailContact = contact
            Dim text3 As String = SecurityElement.Escape(If(Not Equals(mailContact.LastName, Nothing), mailContact.LastName, "").Trim())
            mailContact = contact
            Dim text4 As String = SecurityElement.Escape(If(Not Equals(mailContact.DisplayName, Nothing), mailContact.DisplayName, text).Trim())
            If text2.Length = 0 AndAlso text3.Length = 0 AndAlso text4.Length > 0 Then
                text2 = text4
            End If
            mailContact = contact
            Dim text5 As String = SecurityElement.Escape(If(Not Equals(mailContact.Company, Nothing), mailContact.Company, "").Trim())
            mailContact = contact
            Dim text6 As String = SecurityElement.Escape(If(Not Equals(mailContact.JobTitle, Nothing), mailContact.JobTitle, "").Trim())
            mailContact = contact
            Dim text7 As String = SecurityElement.Escape(If(Not Equals(mailContact.WorkPhone, Nothing), mailContact.WorkPhone, "").Trim())
            mailContact = contact
            Dim text8 As String = SecurityElement.Escape(If(Not Equals(mailContact.MobilePhone, Nothing), mailContact.MobilePhone, "").Trim())
            Dim text9 = SecurityElement.Escape("SMTP:" & text)
            Dim text10 = If(text5.Length > 0, "<t:CompanyName>" & text5 & "</t:CompanyName>", "")
            Dim text11 = If(text6.Length > 0, "<t:JobTitle>" & text6 & "</t:JobTitle>", "")
            Dim text12 = If(text7.Length > 0, "<t:BusinessPhone>" & text7 & "</t:BusinessPhone>", "")
            Dim text13 = If(text8.Length > 0, "<t:MobilePhone>" & text8 & "</t:MobilePhone>", "")
            Dim envelope = "<?xml version=""1.0"" encoding=""utf-8""?><soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:t=""http://schemas.microsoft.com/exchange/services/2006/types"" xmlns:m=""http://schemas.microsoft.com/exchange/services/2006/messages""><soap:Body><m:CreateItem><m:SavedItemFolderId><t:DistinguishedFolderId Id=""contacts""/></m:SavedItemFolderId><m:Items><t:Contact><t:DisplayName>" & text4 & "</t:DisplayName>" & If(text2.Length > 0, "<t:GivenName>" & text2 & "</t:GivenName>", "") & If(text3.Length > 0, "<t:Surname>" & text3 & "</t:Surname>", "") & text10 & text11 & text12 & text13 & "<t:EmailAddresses><t:Entry Key=""EmailAddress1"">" & text9 & "</t:Entry></t:EmailAddresses><t:Body BodyType=""Text"">LCARS GAL shadow — not directory write</t:Body></t:Contact></m:Items></m:CreateItem></soap:Body></soap:Envelope>"
            Dim text14 = PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/CreateItem", envelope, oauthToken)
            If String.IsNullOrEmpty(text14) Then
                Return Nothing
            End If
            If text14.IndexOf("ResponseClass=""Error""", StringComparison.OrdinalIgnoreCase) >= 0 Then
                LastError = "EWS CreateItem contact failed."
                Return Nothing
            End If
            Dim itemId = ""
            Dim changeKey = ""
            ExtractItemIdAndChangeKey(text14, itemId, changeKey)
            If itemId.Length = 0 Then
                itemId = ExtractFirstItemId(text14)
            End If
            Return itemId
        End Function

        Public Shared Function TryHarvestPersonalContactsFolder(ewsUrl As String, username As String, password As String, Optional oauthToken As String = Nothing) As List(Of EwsResolvedContact)
            LastError = Nothing
            Dim list As List(Of EwsResolvedContact) = New List(Of EwsResolvedContact)()
            Dim num = 100
            Dim num2 = 0
            Dim num3 = 0
            Do
                Dim envelope As String = "<?xml version=""1.0"" encoding=""utf-8""?><soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:t=""http://schemas.microsoft.com/exchange/services/2006/types"" xmlns:m=""http://schemas.microsoft.com/exchange/services/2006/messages""><soap:Body><m:FindItem Traversal=""Shallow""><m:ItemShape><t:BaseShape>Default</t:BaseShape></m:ItemShape><m:IndexedPageItemView MaxEntriesReturned=""" & num.ToString() & """ Offset=""" & num2.ToString() & """ BasePoint=""Beginning""/><m:ParentFolderIds><t:DistinguishedFolderId Id=""contacts""/></m:ParentFolderIds></m:FindItem></soap:Body></soap:Envelope>"
                Dim text = PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/FindItem", envelope, oauthToken)
                If String.IsNullOrEmpty(text) Then
                    Exit Do
                End If
                Dim list2 = ParseContactFolderItems(text)
                If list2 Is Nothing OrElse list2.Count = 0 Then
                    Exit Do
                End If
                list.AddRange(list2)
                If list2.Count < num Then
                    Exit Do
                End If
                num2 += list2.Count
                num3 += 1
            Loop While num3 <= 99
            Return list
        End Function

        Public Shared Function ParseContactFolderItems(soapXml As String) As List(Of EwsResolvedContact)
            Dim list As List(Of EwsResolvedContact) = New List(Of EwsResolvedContact)()
            If String.IsNullOrWhiteSpace(soapXml) Then
                Return list
            End If
            Dim array = soapXml.Split(New String(0) {"<t:Contact>"}, StringSplitOptions.None)
            Dim num = array.Length - 1
            For i = 1 To num
                Dim chunk = array(i)
                Dim text = ExtractFirstTagText(chunk, "EmailAddress")
                If text.Length = 0 Then
                    Dim text2 = ExtractFirstTagText(chunk, "SmtpAddress")
                    If text2.Length > 0 Then
                        text = text2
                    End If
                End If
                If text.Length <> 0 Then
                    Dim text3 = ExtractFirstTagText(chunk, "GivenName")
                    Dim text4 = ExtractFirstTagText(chunk, "Surname")
                    Dim middleName = ExtractFirstTagText(chunk, "MiddleName")
                    Dim text5 = ExtractFirstTagText(chunk, "DisplayName")
                    If text5.Length = 0 Then
                        text5 = (text3 & " " & text4).Trim()
                    End If
                    Dim company = ExtractFirstTagText(chunk, "CompanyName")
                    Dim businessPhone = ExtractFirstTagText(chunk, "BusinessPhone")
                    Dim mobilePhone = ExtractFirstTagText(chunk, "MobilePhone")
                    Dim text6 = ExtractFirstTagText(chunk, "JobTitle")
                    If text6.Length = 0 Then
                        text6 = ExtractFirstTagText(chunk, "Title")
                    End If
                    Dim department = ExtractFirstTagText(chunk, "Department")
                    Dim webPage = ExtractFirstTagText(chunk, "BusinessHomePage")
                    Dim faxNumber = ExtractFirstTagText(chunk, "BusinessFax")
                    Dim pagerNumber = ExtractFirstTagText(chunk, "PagerNumber")
                    Dim homePhone = ExtractFirstTagText(chunk, "HomePhone")
                    Dim text7 = ExtractFirstTagText(chunk, "OtherTelephone")
                    If text7.Length = 0 Then
                        text7 = ExtractFirstTagText(chunk, "OtherPhone")
                    End If
                    Dim assistantName = ExtractFirstTagText(chunk, "AssistantName")
                    Dim text8 = ExtractFirstTagText(chunk, "OfficeLocation")
                    If text8.Length = 0 Then
                        text8 = ExtractFirstTagText(chunk, "PhysicalDeliveryOfficeName")
                    End If
                    Dim itemId = ""
                    Dim changeKey = ""
                    ExtractItemIdAndChangeKey(chunk, itemId, changeKey)
                    list.Add(New EwsResolvedContact With {
    .DisplayName = text5,
    .EmailAddress = text,
    .Company = company,
    .BusinessPhone = businessPhone,
    .MobilePhone = mobilePhone,
    .FirstName = text3,
    .LastName = text4,
    .MiddleName = middleName,
    .JobTitle = text6,
    .Department = department,
    .WebPage = webPage,
    .FaxNumber = faxNumber,
    .PagerNumber = pagerNumber,
    .OfficeLocation = text8,
    .HomePhone = homePhone,
    .OtherPhone = text7,
    .AssistantName = assistantName,
    .DirectoryContactItemId = itemId,
    .DirectoryContactChangeKey = changeKey
})
                End If
            Next
            Return list
        End Function

        Public Shared Function TryFindPeopleGal(ewsUrl As String, username As String, password As String, queryString As String, Optional oauthToken As String = Nothing, Optional maxEntries As Integer = 100) As List(Of EwsResolvedContact)
            LastError = Nothing
            Dim text As String = If(Not Equals(queryString, Nothing), queryString, "").Trim()
            If text.Length = 0 Then
                LastError = "FindPeople query is required."
                Return Nothing
            End If
            Dim text2 = SecurityElement.Escape(text)
            Dim num = Math.Max(1, Math.Min(maxEntries, 1000))
            Dim list As List(Of EwsResolvedContact) = New List(Of EwsResolvedContact)()
            Dim num2 = 0
            Dim num3 = 0
            Do
                Dim envelope As String = "<?xml version=""1.0"" encoding=""utf-8""?><soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:t=""http://schemas.microsoft.com/exchange/services/2006/types"" xmlns:m=""http://schemas.microsoft.com/exchange/services/2006/messages""><soap:Body><m:FindPeople><m:PersonaShape><t:BaseShape>Default</t:BaseShape></m:PersonaShape><m:IndexedPageItemView MaxEntriesReturned=""" & num.ToString() & """ Offset=""" & num2.ToString() & """ BasePoint=""Beginning""/><m:ParentFolderId><t:DistinguishedFolderId Id=""directory""/></m:ParentFolderId><m:QueryString>" & text2 & "</m:QueryString></m:FindPeople></soap:Body></soap:Envelope>"
                Dim text3 = PostSoap(ewsUrl, username, password, "http://schemas.microsoft.com/exchange/services/2006/messages/FindPeople", envelope, oauthToken)
                If String.IsNullOrEmpty(text3) Then
                    Exit Do
                End If
                Dim list2 = ParseFindPeopleContacts(text3)
                If list2 Is Nothing OrElse list2.Count = 0 Then
                    Exit Do
                End If
                list.AddRange(list2)
                If list2.Count < num Then
                    Exit Do
                End If
                num2 += list2.Count
                num3 += 1
            Loop While num3 <= 49
            If list.Count = 0 Then
                Return New List(Of EwsResolvedContact)()
            End If
            Return list
        End Function

        Public Shared Function TryHarvestGalDirectory(ewsUrl As String, username As String, password As String, Optional oauthToken As String = Nothing, Optional deepHarvest As Boolean = False) As List(Of EwsResolvedContact)
            LastError = Nothing
            Dim dictionary As Dictionary(Of String, EwsResolvedContact) = New Dictionary(Of String, EwsResolvedContact)(StringComparer.OrdinalIgnoreCase)
            Dim text = "abcdefghijklmnopqrstuvwxyz0123456789"
            Dim text2 = text
            Dim i = 0
            For length = text2.Length To length - 1
                Call MergeFindPeopleBatch(dictionary, TryFindPeopleGal(ewsUrl, username, password, text2(i).ToString(), oauthToken, 250))
            Next
            Dim list = TryHarvestPersonalContactsFolder(ewsUrl, username, password, oauthToken)
            If list IsNot Nothing Then
                MergeFindPeopleBatch(dictionary, list)
            End If
            If deepHarvest Then
                MergeFindPeopleBatch(dictionary, TryFindPeopleGal(ewsUrl, username, password, "@", oauthToken, 250))
                Dim text3 = "abcdefghijklmnopqrstuvwxyz"
                Dim text4 = text3
                Dim j = 0
                For length2 = text4.Length To length2 - 1
                    Dim c = text4(j)
                    Dim text5 = text3
                    Dim k = 0
                    For length3 = text5.Length To length3 - 1
                        Dim c2 = text5(k)
                        Call MergeFindPeopleBatch(dictionary, TryFindPeopleGal(ewsUrl, username, password, c.ToString() & c2.ToString(), oauthToken, 200))
                    Next
                Next
            End If
            If dictionary.Count = 0 Then
                If String.IsNullOrWhiteSpace(LastError) Then
                    LastError = "GAL harvest returned no contacts."
                End If
                Return New List(Of EwsResolvedContact)()
            End If
            Return New List(Of EwsResolvedContact)(dictionary.Values)
        End Function

        Private Shared Sub MergeFindPeopleBatch(byEmail As Dictionary(Of String, EwsResolvedContact), batch As List(Of EwsResolvedContact))
            If byEmail Is Nothing OrElse batch Is Nothing Then
                Return
            End If
            For Each item In batch
                If item IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(item.EmailAddress) Then
                    byEmail(item.EmailAddress.Trim()) = item
                End If
            Next
        End Sub

        Public Shared Function ParseFindPeopleContacts(soapXml As String) As List(Of EwsResolvedContact)
            Dim list As List(Of EwsResolvedContact) = New List(Of EwsResolvedContact)()
            If String.IsNullOrWhiteSpace(soapXml) Then
                Return list
            End If
            Dim array = soapXml.Split(New String(0) {"<t:Persona>"}, StringSplitOptions.None)
            Dim num = array.Length - 1
            For i = 1 To num
                Dim chunk = array(i)
                Dim text = ExtractFirstTagText(chunk, "EmailAddress")
                If text.Length = 0 Then
                    text = ExtractFirstTagText(chunk, "EmailAddress")
                End If
                If text.Length = 0 Then
                    Dim text2 = ExtractFirstTagText(chunk, "SmtpAddress")
                    If text2.Length > 0 Then
                        text = text2
                    End If
                End If
                If text.Length <> 0 Then
                    Dim text3 = ExtractFirstTagText(chunk, "GivenName")
                    Dim text4 = ExtractFirstTagText(chunk, "Surname")
                    Dim middleName = ExtractFirstTagText(chunk, "MiddleName")
                    Dim text5 = ExtractFirstTagText(chunk, "DisplayName")
                    If text5.Length = 0 Then
                        text5 = (text3 & " " & text4).Trim()
                    End If
                    Dim text6 = ExtractFirstTagText(chunk, "CompanyName")
                    If text6.Length = 0 Then
                        text6 = ExtractFirstTagText(chunk, "OfficeLocation")
                    End If
                    Dim businessPhone = ExtractFirstTagText(chunk, "BusinessPhone")
                    Dim mobilePhone = ExtractFirstTagText(chunk, "MobilePhone")
                    Dim text7 = ExtractFirstTagText(chunk, "JobTitle")
                    If text7.Length = 0 Then
                        text7 = ExtractFirstTagText(chunk, "Title")
                    End If
                    Dim department = ExtractFirstTagText(chunk, "Department")
                    Dim webPage = ExtractFirstTagText(chunk, "BusinessHomePage")
                    Dim faxNumber = ExtractFirstTagText(chunk, "BusinessFax")
                    Dim pagerNumber = ExtractFirstTagText(chunk, "PagerNumber")
                    Dim homePhone = ExtractFirstTagText(chunk, "HomePhone")
                    Dim text8 = ExtractFirstTagText(chunk, "OtherTelephone")
                    If text8.Length = 0 Then
                        text8 = ExtractFirstTagText(chunk, "OtherPhone")
                    End If
                    Dim assistantName = ExtractFirstTagText(chunk, "AssistantName")
                    Dim text9 = ExtractFirstTagText(chunk, "OfficeLocation")
                    If text9.Length = 0 Then
                        text9 = ExtractFirstTagText(chunk, "PhysicalDeliveryOfficeName")
                    End If
                    Dim galPersonaId = ExtractPersonaItemId(chunk)
                    Dim galPersonaChangeKey = ExtractPersonaChangeKey(chunk)
                    list.Add(New EwsResolvedContact With {
    .DisplayName = text5,
    .EmailAddress = text,
    .Company = text6,
    .BusinessPhone = businessPhone,
    .MobilePhone = mobilePhone,
    .JobTitle = text7,
    .Department = department,
    .WebPage = webPage,
    .FaxNumber = faxNumber,
    .PagerNumber = pagerNumber,
    .OfficeLocation = text9,
    .HomePhone = homePhone,
    .MiddleName = middleName,
    .OtherPhone = text8,
    .AssistantName = assistantName,
    .FirstName = text3,
    .LastName = text4,
    .GalPersonaId = galPersonaId,
    .GalPersonaChangeKey = galPersonaChangeKey
})
                End If
            Next
            If list.Count = 0 Then
                Dim list2 = ParseResolvedContacts(soapXml)
                If list2 IsNot Nothing AndAlso list2.Count > 0 Then
                    Return list2
                End If
            End If
            Return list
        End Function

        Public Shared Function ParseResolvedContacts(soapXml As String) As List(Of EwsResolvedContact)
            Dim dictionary As Dictionary(Of String, EwsResolvedContact) = New Dictionary(Of String, EwsResolvedContact)(StringComparer.OrdinalIgnoreCase)
            If String.IsNullOrWhiteSpace(soapXml) Then
                Return New List(Of EwsResolvedContact)()
            End If
            Dim array = soapXml.Split(New String(0) {"<t:Mailbox>"}, StringSplitOptions.None)
            Dim num = array.Length - 1
            For i = 1 To num
                Dim chunk = array(i)
                Dim text = NormalizeSmtpAddress(ExtractFirstTagText(chunk, "EmailAddress"))
                If text.Length <> 0 Then
                    Dim displayName = ExtractFirstTagText(chunk, "Name")
                    dictionary(text) = New EwsResolvedContact With {
    .DisplayName = displayName,
    .EmailAddress = text
}
                End If
            Next
            Dim array2 = soapXml.Split(New String(0) {"<t:Contact>"}, StringSplitOptions.None)
            Dim num2 = array2.Length - 1
            For j = 1 To num2
                Dim text2 = array2(j)
                Dim text3 = ExtractContactPrimaryEmail(text2)
                If text3.Length = 0 Then
                    Continue For
                End If
                Dim text4 = ExtractFirstTagText(text2, "GivenName")
                Dim text5 = ExtractFirstTagText(text2, "Surname")
                Dim text6 = ExtractFirstTagText(text2, "DisplayName")
                If text6.Length = 0 Then
                    text6 = (text4 & " " & text5).Trim()
                End If
                Dim itemId = ""
                Dim changeKey = ""
                ExtractItemIdAndChangeKey(text2, itemId, changeKey)
                Dim ewsResolvedContact As EwsResolvedContact = Nothing
                If dictionary.ContainsKey(text3) Then
                    ewsResolvedContact = dictionary(text3)
                    If text6.Length > 0 Then
                        ewsResolvedContact.DisplayName = text6
                    End If
                    If itemId.Length > 0 Then
                        ewsResolvedContact.DirectoryContactItemId = itemId
                    End If
                    If changeKey.Length > 0 Then
                        ewsResolvedContact.DirectoryContactChangeKey = changeKey
                    End If
                Else
                    dictionary(text3) = New EwsResolvedContact With {
    .DisplayName = text6,
    .EmailAddress = text3,
    .FirstName = text4,
    .LastName = text5,
    .DirectoryContactItemId = itemId,
    .DirectoryContactChangeKey = changeKey
}
                End If
            Next
            Return New List(Of EwsResolvedContact)(dictionary.Values)
        End Function

        Private Shared Function ExtractContactPrimaryEmail(contactChunk As String) As String
            Dim text = NormalizeSmtpAddress(ExtractFirstTagText(contactChunk, "EmailAddress"))
            If text.Length > 0 Then
                Return text
            End If
            Dim text2 = NormalizeSmtpAddress(ExtractFirstTagText(contactChunk, "SmtpAddress"))
            If text2.Length > 0 Then
                Return text2
            End If
            Dim text3 = If(Not Equals(contactChunk, Nothing), contactChunk, "")
            Dim value = "<t:Entry"
            Dim num = 0
            While num < text3.Length
                num = text3.IndexOf(value, num, StringComparison.OrdinalIgnoreCase)
                If num < 0 Then
                    Exit While
                End If
                Dim num2 = text3.IndexOf("</t:Entry>", num, StringComparison.OrdinalIgnoreCase)
                If num2 < 0 Then
                    Exit While
                End If
                Dim text4 = text3.Substring(num, num2 - num)
                If text4.IndexOf("EmailAddress", StringComparison.OrdinalIgnoreCase) >= 0 Then
                    Dim text5 = ExtractFirstTagText(text4, "Entry")
                    If text5.Length = 0 Then
                        Dim num3 = text4.LastIndexOf(">"c)
                        If num3 >= 0 AndAlso num3 + 1 < text4.Length Then
                            text5 = text4.Substring(num3 + 1).Trim()
                        End If
                    End If
                    text5 = NormalizeSmtpAddress(text5)
                    If text5.Length > 0 Then
                        Return text5
                    End If
                End If
                num = num2 + 1
            End While
            Return ""
        End Function

        Private Shared Function NormalizeSmtpAddress(raw As String) As String
            Dim text As String = If(Not Equals(raw, Nothing), raw, "").Trim()
            If text.Length = 0 Then
                Return ""
            End If
            If text.StartsWith("SMTP:", StringComparison.OrdinalIgnoreCase) Then
                text = text.Substring(5).Trim()
            End If
            If text.StartsWith("smtp:", StringComparison.OrdinalIgnoreCase) Then
                text = text.Substring(5).Trim()
            End If
            Return text
        End Function

        Private Shared Sub ExtractItemIdAndChangeKey(chunk As String, ByRef itemId As String, ByRef changeKey As String)
            itemId = ""
            changeKey = ""
            Dim text = "ItemId Id="""
            Dim num = chunk.IndexOf(text, StringComparison.OrdinalIgnoreCase)
            If num < 0 Then
                Return
            End If
            num += text.Length
            Dim num2 = chunk.IndexOf(""""c, num)
            If num2 <= num Then
                Return
            End If
            itemId = chunk.Substring(num, num2 - num)
            Dim text2 = "ChangeKey="""
            Dim num3 = chunk.IndexOf(text2, num2, StringComparison.OrdinalIgnoreCase)
            If num3 >= 0 Then
                num3 += text2.Length
                Dim num4 = chunk.IndexOf(""""c, num3)
                If num4 > num3 Then
                    changeKey = chunk.Substring(num3, num4 - num3)
                End If
            End If
        End Sub

        Private Shared Function ExtractFirstItemId(chunk As String) As String
            Dim text = "ItemId Id="""
            Dim num = chunk.IndexOf(text, StringComparison.OrdinalIgnoreCase)
            If num < 0 Then
                Return ""
            End If
            num += text.Length
            Dim num2 = chunk.IndexOf(""""c, num)
            If num2 <= num Then
                Return ""
            End If
            Return chunk.Substring(num, num2 - num)
        End Function

        Private Shared Function ExtractFirstTagText(chunk As String, localName As String) As String
            Dim text = "<t:" & localName & ">"
            Dim num = chunk.IndexOf(text, StringComparison.OrdinalIgnoreCase)
            If num < 0 Then
                Return ""
            End If
            num += text.Length
            Dim value = "</t:" & localName & ">"
            Dim num2 = chunk.IndexOf(value, num, StringComparison.OrdinalIgnoreCase)
            If num2 < 0 Then
                Return ""
            End If
            Return chunk.Substring(num, num2 - num).Trim()
        End Function

        Public Shared Function ParseItemIdsFromFindResponse(soapXml As String) As List(Of String)
            Dim list As List(Of String) = New List(Of String)()
            If String.IsNullOrWhiteSpace(soapXml) Then
                Return list
            End If
            Dim text = "ItemId Id="""
            Dim startIndex = 0
            While True
                Dim num = soapXml.IndexOf(text, startIndex, StringComparison.OrdinalIgnoreCase)
                If num < 0 Then
                    Exit While
                End If
                num += text.Length
                Dim num2 = soapXml.IndexOf(""""c, num)
                If num2 < 0 Then
                    Exit While
                End If
                list.Add(soapXml.Substring(num, num2 - num))
                startIndex = num2 + 1
            End While
            Return list
        End Function

        Private Shared Function PostSoap(ewsUrl As String, username As String, password As String, soapAction As String, envelope As String, Optional oauthToken As String = Nothing) As String
            LastError = Nothing
            Dim text As String = If(Not Equals(ewsUrl, Nothing), ewsUrl, "").Trim()
            Dim result As String
            If text.Length = 0 Then
                LastError = "EWS URL is required."
                result = ""
            Else
                Dim text2 As String = If(Not Equals(oauthToken, Nothing), oauthToken, "").Trim()
                Dim text3 As String = If(Not Equals(username, Nothing), username, "").Trim()
                Dim password2 = If(Not Equals(password, Nothing), password, "")
                If text2.Length = 0 AndAlso text3.Length = 0 Then
                    LastError = "EWS username/password or OAuth token required."
                    result = ""
                Else
                    Try
                        Dim httpWebRequest = CType(WebRequest.Create(text), HttpWebRequest)
                        httpWebRequest.Method = "POST"
                        httpWebRequest.ContentType = "text/xml; charset=utf-8"
                        httpWebRequest.Headers.Add("SOAPAction", soapAction)
                        If text2.Length > 0 Then
                            httpWebRequest.Headers.Add("Authorization", "Bearer " & text2)
                            If text3.Length > 0 Then
                                httpWebRequest.Headers.Add("X-AnchorMailbox", text3)
                            End If
                        Else
                            httpWebRequest.Credentials = New NetworkCredential(text3, password2)
                        End If
                        httpWebRequest.Timeout = 45000
                        Dim bytes = Encoding.UTF8.GetBytes(envelope)
                        httpWebRequest.ContentLength = bytes.Length
                        Using stream As Stream = httpWebRequest.GetRequestStream()
                            stream.Write(bytes, 0, bytes.Length)
                        End Using
                        Dim httpWebResponse As HttpWebResponse = CType(httpWebRequest.GetResponse(), HttpWebResponse)
                        Dim streamReader As StreamReader = New StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8)
                        result = streamReader.ReadToEnd()
                    Catch ex As WebException
                        ProjectData.SetProjectError(ex)
                        Dim ex2 = ex
                        LastError = ex2.Message
                        Try
                            If ex2.Response IsNot Nothing Then
                                Dim streamReader2 As StreamReader = New StreamReader(ex2.Response.GetResponseStream(), Encoding.UTF8)
                                LastError = streamReader2.ReadToEnd()
                            End If
                        Catch projectError As Exception
                            ProjectData.SetProjectError(projectError)
                            Call ProjectData.ClearProjectError()
                        End Try
                        result = ""
                        Call ProjectData.ClearProjectError()
                    Catch ex3 As Exception
                        ProjectData.SetProjectError(ex3)
                        Dim ex4 = ex3
                        LastError = ex4.Message
                        result = ""
                        Call ProjectData.ClearProjectError()
                    End Try
                End If
            End If
            Return result
        End Function

        Private Shared Function IsDistinguishedFolderId(folderId As String) As Boolean
            Return DistinguishedFolderIds.Contains(If(Not Equals(folderId, Nothing), folderId, "").Trim())
        End Function

        Private Shared Function BuildParentFolderIdXml(folderId As String) As String
            Dim text As String = If(Not Equals(folderId, Nothing), folderId, "").Trim()
            If text.Length = 0 Then
                text = "inbox"
            End If
            If IsDistinguishedFolderId(text) Then
                Return "<t:DistinguishedFolderId Id=""" & SecurityElement.Escape(text.ToLowerInvariant()) & """/>"
            End If
            Return "<t:FolderId Id=""" & SecurityElement.Escape(text) & """/>"
        End Function

        Private Shared Function BuildDestinationFolderIdXml(folderId As String) As String
            Return BuildParentFolderIdXml(folderId)
        End Function

        Private Shared Function ExtractXmlElementText(xml As String, localName As String) As String
            Dim result As String
            If String.IsNullOrWhiteSpace(xml) Then
                result = ""
            Else
                Try
                    Dim xmlDocument As XmlDocument = New XmlDocument()
                    xmlDocument.LoadXml(xml)
                    Dim elementsByTagName = xmlDocument.GetElementsByTagName(localName)
                    If elementsByTagName Is Nothing OrElse elementsByTagName.Count = 0 Then
                        elementsByTagName = xmlDocument.GetElementsByTagName("t:" & localName)
                    End If
                    If elementsByTagName Is Nothing OrElse elementsByTagName.Count = 0 Then
                        result = ""
                    Else
                        Dim innerText = elementsByTagName(0).InnerText
                        result = If(Not Equals(innerText, Nothing), innerText, "").Trim()
                    End If
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    result = ""
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function
    End Class

