Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Net.Security
Imports System.Security.Cryptography.X509Certificates
Imports System.Threading
Imports MailKit
Imports MailKit.Net.Smtp
Imports MailKit.Security
Imports Microsoft.VisualBasic.CompilerServices
Imports MimeKit
    Public Class SmtpMailService
        <DebuggerNonUserCode>
        Public Sub New()
        End Sub

        Public Function CreateMimeMessage(account As MailAccount, toAddrs As String, ccAddrs As String, subject As String, bodyText As String, attachmentPaths As List(Of String), Optional bodyHtml As String = Nothing) As MimeMessage
            Dim list As InternetAddressList = Nothing
            Dim text = SmtpMailService.TryParseAddressList(toAddrs, list)
            If Not Equals(text, Nothing) Then
                Throw New InvalidOperationException(text)
            End If
            If list Is Nothing Then
                list = New InternetAddressList()
            End If
            Dim list2 As InternetAddressList = Nothing
            text = SmtpMailService.TryParseAddressList(ccAddrs, list2)
            If Not Equals(text, Nothing) Then
                Throw New InvalidOperationException(text)
            End If
            Return SmtpMailService.BuildMessage(account, list, list2, subject, bodyText, bodyHtml, attachmentPaths, signPgp:=False, encryptPgp:=False)
        End Function

        Public Function Send(account As MailAccount, password As String, toAddrs As String, ccAddrs As String, subject As String, bodyText As String, attachmentPaths As List(Of String), Optional signPgp As Boolean = False, Optional encryptPgp As Boolean = False, Optional bodyHtml As String = Nothing) As String
            Dim result As String
            If account Is Nothing Then
                result = "No mail account selected."
            Else
                If Equals(password, Nothing) Then
                    password = ""
                End If
                Dim text As String = If(Not Equals(account.SmtpHost, Nothing), account.SmtpHost, "").Trim()
                If text.Length = 0 Then
                    result = "SMTP server is not configured for this account."
                Else
                    Dim list As InternetAddressList = Nothing
                    Dim text2 = SmtpMailService.TryParseAddressList(toAddrs, list)
                    If Not Equals(text2, Nothing) Then
                        result = text2
                    ElseIf list Is Nothing OrElse list.Count = 0 Then
                        result = "Enter at least one To address."
                    Else
                        Dim list2 As InternetAddressList = Nothing
                        text2 = SmtpMailService.TryParseAddressList(ccAddrs, list2)
                        If Not Equals(text2, Nothing) Then
                            result = text2
                        Else
                            Try
                                Dim val As MimeMessage = SmtpMailService.BuildMessage(account, list, list2, subject, bodyText, bodyHtml, attachmentPaths, signPgp, encryptPgp)
                                Dim val2 As SmtpClient = Connect(account, password)
                                Try
                                    Dim cancellationToken As CancellationToken = Nothing
                                    CType(val2, MailTransport).Send(val, cancellationToken, CType(Nothing, ITransferProgress))
                                Finally
                                    If val2 IsNot Nothing Then CType(val2, IDisposable).Dispose()
                                End Try
                                result = ""
                            Catch ex As Exception
                                ProjectData.SetProjectError(ex)
                                Dim ex2 = ex
                                Dim message = ex2.Message
                                Dim text3 As String = If(Not Equals(message, Nothing), message, "Send failed.").Trim()
                                If text3.Length = 0 Then
                                    text3 = "Send failed."
                                End If
                                result = text3
                                Call ProjectData.ClearProjectError()
                            End Try
                        End If
                    End If
                End If
            End If
            Return result
        End Function

        Private Shared Function BuildMessage(account As MailAccount, toList As InternetAddressList, ccList As InternetAddressList, subject As String, bodyText As String, bodyHtml As String, attachmentPaths As List(Of String), signPgp As Boolean, encryptPgp As Boolean) As MimeMessage
            Dim val As MimeMessage = New MimeMessage()
            val.From.Add(CType(CObj(ResolveFromAddress(account)), InternetAddress))
            val.[To].AddRange(CType(toList, IEnumerable(Of InternetAddress)))
            If ccList IsNot Nothing AndAlso ccList.Count > 0 Then
                val.Cc.AddRange(CType(ccList, IEnumerable(Of InternetAddress)))
            End If
            val.Subject = If(Not Equals(subject, Nothing), subject, "")
            Dim bodyText2 = If(Not Equals(bodyText, Nothing), bodyText, "")
            Dim bodyEntity As MimeEntity = Nothing
            If signPgp OrElse encryptPgp Then
                If Not MailPgpService.TryEncryptBody(account, toList.ToString(), bodyText2, signPgp, encryptPgp, bodyEntity) Then
                    Throw New InvalidOperationException(If(Not Equals(MailPgpService.LastError, Nothing), MailPgpService.LastError, "PGP failed."))
                End If
            Else
                bodyEntity = BuildBodyEntity(bodyText2, bodyHtml, attachmentPaths)
            End If
            val.Body = bodyEntity
            Dim text = MailAutocryptService.BuildOutgoingAutocryptHeader(account)
            If text.Length > 0 Then
                val.Headers("Autocrypt") = text
            End If
            Return val
        End Function

        Private Shared Function BuildBodyEntity(bodyText As String, bodyHtml As String, attachmentPaths As List(Of String)) As MimeEntity
            Dim list = CollectExistingAttachmentPaths(attachmentPaths)
            Dim val As MimeEntity = BuildAlternativePart(bodyText, bodyHtml)
            If list.Count = 0 Then
                Return val
            End If
            Dim val2 As Multipart = New Multipart("mixed")
            val2.Add(val)
            For Each item In list
                val2.Add(CType(CObj(CreateAttachmentPart(item)), MimeEntity))
            Next
            Return CType(CObj(val2), MimeEntity)
        End Function

        Private Shared Function BuildAlternativePart(bodyText As String, bodyHtml As String) As MimeEntity
            Dim text = If(Not Equals(bodyText, Nothing), bodyText, "")
            Dim text2 As String = If(Not Equals(bodyHtml, Nothing), bodyHtml, "").Trim()
            If text2.Length = 0 Then
                Dim val As TextPart = New TextPart("plain")
                val.Text = text
                Return CType(CObj(val), MimeEntity)
            End If
            If text.Length = 0 Then
                text = text2
            End If
            Return MailHtmlInlineRelatedBuilder.BuildHtmlRoot(text2, text)
        End Function

        Private Shared Function CollectExistingAttachmentPaths(attachmentPaths As List(Of String)) As List(Of String)
            Dim list As List(Of String) = New List(Of String)()
            If attachmentPaths Is Nothing Then
                Return list
            End If
            For Each attachmentPath In attachmentPaths
                If Not String.IsNullOrWhiteSpace(attachmentPath) Then
                    Dim text As String = attachmentPath.Trim()
                    If File.Exists(text) Then
                        list.Add(text)
                    End If
                End If
            Next
            Return list
        End Function

        Private Shared Function CreateAttachmentPart(filePath As String) As MimePart
            Dim val As MimePart = New MimePart()
            val.Content = CType(New MimeContent(File.OpenRead(filePath), CType(0, ContentEncoding)), IMimeContent)
            CType(val, MimeEntity).ContentDisposition = New ContentDisposition("attachment")
            val.ContentTransferEncoding = CType(4, ContentEncoding)
            val.FileName = Path.GetFileName(filePath)
            Return val
        End Function

        Private Shared Function ResolveFromAddress(account As MailAccount) As MailboxAddress
            Dim mailAccount = account
            Dim text As String = If(Not Equals(mailAccount.EmailAddress, Nothing), mailAccount.EmailAddress, "").Trim()
            If text.Length = 0 Then
                mailAccount = account
                text = If(Not Equals(mailAccount.Username, Nothing), mailAccount.Username, "").Trim()
            End If
            If text.Length = 0 Then
                mailAccount = account
                text = If(Not Equals(mailAccount.SmtpUsername, Nothing), mailAccount.SmtpUsername, "").Trim()
            End If
            If text.Length = 0 Then
                Throw New InvalidOperationException("Account has no From email address.")
            End If
            mailAccount = account
            Dim text2 As String = If(Not Equals(mailAccount.DisplayName, Nothing), mailAccount.DisplayName, "").Trim()
            If text2.Length = 0 Then
                Return MailboxAddress.Parse(text)
            End If
            Return New MailboxAddress(text2, text)
        End Function

        Private Shared Function TryParseAddressList(raw As String, ByRef list As InternetAddressList) As String
            list = New InternetAddressList()
            Dim text As String = MailAddressHelper.NormalizeForTransport(If(Not Equals(raw, Nothing), raw, "").Trim())
            Dim result As String
            If text.Length = 0 Then
                result = Nothing
            Else
                Try
                    list = InternetAddressList.Parse(text)
                    result = Nothing
                Catch ex As Exception
                    ProjectData.SetProjectError(ex)
                    Dim ex2 = ex
                    result = "Invalid address: " & text & " (" & ex2.Message & ")"
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Private Shared Function Connect(account As MailAccount, password As String) As SmtpClient
            Dim mailAccount = account
            Dim text As String = If(Not Equals(mailAccount.SmtpHost, Nothing), mailAccount.SmtpHost, "").Trim()
            Dim num = account.SmtpPort
            If num <= 0 Then
                If account.SmtpSsl AndAlso Not account.SmtpStartTls Then
                    num = 465
                Else
                    num = 587
                End If
            End If
            mailAccount = account
            Dim text2 As String = If(Not Equals(mailAccount.SmtpUsername, Nothing), mailAccount.SmtpUsername, "").Trim()
            If text2.Length = 0 Then
                mailAccount = account
                text2 = If(Not Equals(mailAccount.Username, Nothing), mailAccount.Username, "").Trim()
            End If
            If text2.Length = 0 Then
                mailAccount = account
                text2 = If(Not Equals(mailAccount.EmailAddress, Nothing), mailAccount.EmailAddress, "").Trim()
            End If
            Dim val As SmtpClient = New SmtpClient()
            Dim strictMode = If(account IsNot Nothing AndAlso account.StrictTls, True, False)
            CType(val, MailService).ServerCertificateValidationCallback = Function(s, cert As X509Certificate, chain As X509Chain, errors As SslPolicyErrors) TlsPolicyStore.ValidateCertificate(text, num, TryCast(cert, X509Certificate2), errors, strictMode)
            Dim cancellationToken As CancellationToken = Nothing
            val.Connect(text, num, ResolveSecureSocketOptions(account, num), cancellationToken)
            If MailOAuthProviders.UsesOAuth(account) Then
                val.Authenticate(CType(New SaslMechanismOAuth2(text2, password), SaslMechanism), cancellationToken)
            Else
                CType(val, MailService).Authenticate(text2, password, cancellationToken)
            End If
            Return val
        End Function

        Private Shared Function ResolveSecureSocketOptions(account As MailAccount, port As Integer) As SecureSocketOptions
            If account.SmtpStartTls Then
                Return CType(4, SecureSocketOptions)
            End If
            If account.SmtpSsl Then
                Return CType(2, SecureSocketOptions)
            End If
            If port = 465 Then
                Return CType(2, SecureSocketOptions)
            End If
            Return CType(1, SecureSocketOptions)
        End Function
    End Class

