Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Text
Imports System.Web.Script.Serialization
Imports Microsoft.VisualBasic.CompilerServices
    Public NotInheritable Class EwsAttachmentStore
        Private Class AttachmentEntry
            Public PartIndex As Integer

            Public AttachmentId As String

            Public FileName As String

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Private Const CacheSubdir As String = "ews-attachments"

        Private Sub New()
        End Sub

        Public Shared Sub RememberAttachments(accountId As String, folder As String, uid As ULong, attachments As IList(Of EwsMailService.EwsAttachmentInfo))
            If attachments Is Nothing OrElse attachments.Count = 0 Then
                Return
            End If
            Dim list As List(Of AttachmentEntry) = New List(Of AttachmentEntry)()
            Dim num = 0
            For Each attachment In attachments
                If attachment IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(attachment.AttachmentId) Then
                    Dim attachmentEntry As AttachmentEntry = New AttachmentEntry With {
        .PartIndex = num,
        .AttachmentId = attachment.AttachmentId
    }
                    Dim ewsAttachmentInfo = attachment
                    attachmentEntry.FileName = If(Not Equals(ewsAttachmentInfo.FileName, Nothing), ewsAttachmentInfo.FileName, "")
                    list.Add(attachmentEntry)
                    num = num + 1
                End If
            Next
            If list.Count <> 0 Then
                SaveList(accountId, folder, uid, list)
            End If
        End Sub

        Public Shared Function TryGetAttachmentId(accountId As String, folder As String, uid As ULong, partIndex As Integer, ByRef attachmentId As String, ByRef fileName As String) As Boolean
            attachmentId = Nothing
            fileName = ""
            Dim list = LoadList(accountId, folder, uid)
            For Each item In list
                If item Is Nothing OrElse item.PartIndex <> partIndex Then
                    Continue For
                End If
                attachmentId = item.AttachmentId
                Dim attachmentEntry = item
                fileName = If(Not Equals(attachmentEntry.FileName, Nothing), attachmentEntry.FileName, "")
                Return If(Not Equals(attachmentId, Nothing) AndAlso attachmentId.Length > 0, True, False)
            Next
            Return False
        End Function

        Private Shared Sub SaveList(accountId As String, folder As String, uid As ULong, list As List(Of AttachmentEntry))
            Dim path As String = IO.Path.Combine(AccountStore.DataRoot(), "ews-attachments")
            If Not Directory.Exists(path) Then
                Directory.CreateDirectory(path)
            End If
            Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
            File.WriteAllText(CachePath(accountId, folder, uid), javaScriptSerializer.Serialize(list), Encoding.UTF8)
        End Sub

        Private Shared Function LoadList(accountId As String, folder As String, uid As ULong) As List(Of AttachmentEntry)
            Dim path = CachePath(accountId, folder, uid)
            Dim result As List(Of AttachmentEntry)
            If Not File.Exists(path) Then
                result = New List(Of AttachmentEntry)()
            Else
                Try
                    Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                    Dim list = javaScriptSerializer.Deserialize(Of List(Of AttachmentEntry))(File.ReadAllText(path, Encoding.UTF8))
                    result = (If(list IsNot Nothing, list, New List(Of AttachmentEntry)()))
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    result = New List(Of AttachmentEntry)()
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Private Shared Function CachePath(accountId As String, folder As String, uid As ULong) As String
            Dim text = SanitizeToken(accountId)
            Dim text2 = SanitizeToken(folder)
            Return Path.Combine(AccountStore.DataRoot(), "ews-attachments", text & "__" & text2 & "__" & uid.ToString() & ".json")
        End Function

        Private Shared Function SanitizeToken(raw As String) As String
            Dim text As String = If(Not Equals(raw, Nothing), raw, "").Trim()
            If text.Length = 0 Then
                Return "default"
            End If
            Dim invalidFileNameChars As Char() = Path.GetInvalidFileNameChars()
            Dim stringBuilder As StringBuilder = New StringBuilder(text.Length)
            Dim text2 = text
            Dim i = 0
            Dim length = text2.Length

            While i < length
                Dim value = text2(i)
                If Array.IndexOf(invalidFileNameChars, value) >= 0 Then
                    stringBuilder.Append("_"c)
                Else
                    stringBuilder.Append(value)
                End If

                i = i + 1
            End While
            Return stringBuilder.ToString()
        End Function
    End Class

