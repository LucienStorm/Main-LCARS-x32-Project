Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Text.RegularExpressions
Imports MimeKit
    Public NotInheritable Class MailHtmlInlineRelatedBuilder
        Private Shared ReadOnly DataUriPattern As Regex = New Regex("src\s*=\s*[""']data:image/([^;]+);base64,([^""']+)[""']", RegexOptions.IgnoreCase Or RegexOptions.Compiled)

        Private Sub New()
        End Sub

        Public Shared Function BuildHtmlRoot(html As String, plain As String) As MimeEntity
            Dim text As String = If(Not Equals(html, Nothing), html, "").Trim()
            Dim text2 = If(Not Equals(plain, Nothing), plain, "")
            Dim val As TextPart
            If text.Length = 0 Then
                val = New TextPart("plain")
                val.Text = text2
                Return CType(CObj(val), MimeEntity)
            End If
            Dim val2 As Multipart = New Multipart("related")
            Dim list As List(Of String) = New List(Of String)()
            Dim text3 = text
            Dim num = 0
            For Each item As Match In DataUriPattern.Matches(text)
                Dim text4 As String = item.Groups(1).Value.Trim()
                Dim value = item.Groups(2).Value
                Dim text5 As String = "img" & num.ToString() & "@lcars.local"
                num = num + 1
                Dim val3 As MimePart = New MimePart("image", text4)
                val3.Content = CType(New MimeContent(CType(New MemoryStream(Convert.FromBase64String(value)), Stream), CType(0, ContentEncoding)), IMimeContent)
                val3.ContentTransferEncoding = CType(4, ContentEncoding)
                CType(val3, MimeEntity).ContentId = text5
                CType(val3, MimeEntity).ContentDisposition = New ContentDisposition("inline")
                Dim val4 As MimePart = val3
                val2.Add(CType(CObj(val4), MimeEntity))
                text3 = text3.Replace(item.Value, "src=""cid:" & text5 & """")
                list.Add(text5)
            Next
            val = New TextPart("html")
            val.Text = text3
            Dim val5 As TextPart = val
            val2.Add(CType(CObj(val5), MimeEntity))
            If text2.Length = 0 Then
                text2 = StripTagsLight(text3)
            End If
            Dim val6 As Multipart = New Multipart("alternative")
            val = New TextPart("plain")
            val.Text = text2
            val6.Add(CType(CObj(val), MimeEntity))
            If val2.Count > 1 Then
                val6.Add(CType(CObj(val2), MimeEntity))
            Else
                val6.Add(CType(CObj(val5), MimeEntity))
            End If
            Return CType(CObj(val6), MimeEntity)
        End Function

        Private Shared Function StripTagsLight(html As String) As String
            If String.IsNullOrEmpty(html) Then
                Return ""
            End If
            Return Regex.Replace(html, "<[^>]+>", " ").Trim()
        End Function
    End Class

