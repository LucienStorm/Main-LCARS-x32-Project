Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Threading
Imports Microsoft.VisualBasic.CompilerServices
Imports MimeKit
Imports MimeKit.Cryptography
    Public NotInheritable Class MailPgpService
        Public Shared LastError As String

        Private Sub New()
        End Sub

        Public Shared Function GnupgHomeDirectory() As String
            Dim text As String = Path.Combine(AccountStore.DataRoot(), "pgp")
            If Not Directory.Exists(text) Then
                Directory.CreateDirectory(text)
            End If
            Return text
        End Function

        Public Shared Function CreateContext() As LcarsOpenPgpContext
            Return New LcarsOpenPgpContext()
        End Function

        Public Shared Function ImportArmoredKeyFile(path As String) As Integer
            LastError = Nothing
            If String.IsNullOrWhiteSpace(path) OrElse Not File.Exists(path) Then
                LastError = "Key file not found."
                Return -1
            End If
            Dim errorText As String = Nothing
            If TryGpgImport(path, errorText) Then
                Return 1
            End If
            If Not String.IsNullOrEmpty(errorText) Then
                LastError = errorText
            End If
            Dim errorText2 As String = Nothing
            If TryGpgImportFromStaging(path, errorText2) Then
                Return 1
            End If
            LastError = If(Not Equals(errorText2, Nothing), errorText2, If(Not Equals(LastError, Nothing), LastError, "Key import failed."))
            Return -1
        End Function

        Private Shared Function TryGpgImportFromStaging(keyPath As String, ByRef errorText As String) As Boolean
            errorText = Nothing
            Dim result As Boolean
            If Equals(FindGpgExecutable(), Nothing) Then
                result = False
            Else
                Try
                    Dim path As String = GnupgHomeDirectory()
                    Dim text = IO.Path.Combine(path, "import-staging")
                    If Not Directory.Exists(text) Then
                        Directory.CreateDirectory(text)
                    End If
                    Dim text2 = IO.Path.Combine(text, IO.Path.GetFileName(keyPath))
                    File.Copy(keyPath, text2, overwrite:=True)
                    result = TryGpgImport(text2, errorText)
                Catch ex As Exception
                    ProjectData.SetProjectError(ex)
                    Dim ex2 = ex
                    errorText = ex2.Message
                    result = False
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Private Shared Function TryGpgImport(keyPath As String, ByRef errorText As String) As Boolean
            errorText = Nothing
            Dim text As String = FindGpgExecutable()
            If Equals(text, Nothing) Then
                Return False
            End If
            Try
                Dim text2 As String = GnupgHomeDirectory()
                Dim processStartInfo As ProcessStartInfo = New ProcessStartInfo()
                processStartInfo.FileName = text
                processStartInfo.Arguments = "--homedir """ & text2 & """ --batch --yes --import """ & keyPath & """"
                processStartInfo.UseShellExecute = False
                processStartInfo.CreateNoWindow = True
                processStartInfo.RedirectStandardOutput = True
                processStartInfo.RedirectStandardError = True
                Dim startInfo = processStartInfo
                Dim process = Process.Start(startInfo)
                process.WaitForExit(15000)
                If process.ExitCode = 0 Then
                    Return True
                End If
                errorText = process.StandardError.ReadToEnd().Trim()
                If errorText.Length = 0 Then
                    errorText = "gpg import failed (" & process.ExitCode.ToString() & ")"
                End If
            Catch ex As Exception
                ProjectData.SetProjectError(ex)
                Dim ex2 = ex
                errorText = ex2.Message
                Call ProjectData.ClearProjectError()
            End Try
            Return False
        End Function

        Private Shared Function FindGpgExecutable() As String
            Dim array = New String(2) {Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "GnuPG", "bin", "gpg.exe"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "GnuPG", "bin", "gpg.exe"), "gpg.exe"}
            Dim array2 = array
            For Each text In array2
                If Not String.IsNullOrWhiteSpace(text) Then
                    If Operators.CompareString(text, "gpg.exe", TextCompare:=False) = 0 Then
                        Return text
                    End If
                    If File.Exists(text) Then
                        Return text
                    End If
                End If
            Next
            Return Nothing
        End Function

        Public Shared Function ListPublicKeyLabels() As List(Of String)
            Dim list As List(Of String) = New List(Of String)()
            Try
                Dim files As String() = Directory.GetFiles(GnupgHomeDirectory(), "*.asc")
                For Each path In files
                    list.Add(IO.Path.GetFileName(path))
                Next
                Dim path2 As String = Path.Combine(GnupgHomeDirectory(), "pubring.gpg")
                If File.Exists(path2) Then
                    list.Add("pubring.gpg")
                End If
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
            Return list
        End Function

        Public Shared Function TryDecryptMessage(message As MimeMessage, passphrase As String, ByRef plainText As String) As Boolean
            Dim htmlText As String = Nothing
            Return MailPgpService.TryDecryptMessage(message, passphrase, plainText, htmlText)
        End Function

        Public Shared Function TryDecryptMessage(message As MimeMessage, passphrase As String, ByRef plainText As String, ByRef htmlText As String) As Boolean
            LastError = Nothing
            plainText = ""
            htmlText = ""
            If message Is Nothing Then
                Return False
            End If
            Try
                Dim lcarsOpenPgpContext As LcarsOpenPgpContext = CreateContext()
                Try
                    Dim body As MimeEntity = message.Body
                    Dim val As MultipartEncrypted = CType(If((TypeOf body Is MultipartEncrypted), body, Nothing), MultipartEncrypted)
                    If val IsNot Nothing Then
                        Dim cancellationToken As CancellationToken = Nothing
                        Dim entity As MimeEntity = val.Decrypt(CType(CObj(lcarsOpenPgpContext), OpenPgpContext), cancellationToken)
                        MailPgpService.ExtractBodies(entity, plainText, htmlText)
                        Return If(plainText.Length > 0 OrElse htmlText.Length > 0, True, False)
                    End If

                Finally
                    If CObj(lcarsOpenPgpContext) IsNot Nothing Then CType(CObj(lcarsOpenPgpContext), IDisposable).Dispose()
                End Try
            Catch ex As Exception
                ProjectData.SetProjectError(ex)
                Dim ex2 = ex
                LastError = ex2.Message
                Call ProjectData.ClearProjectError()
            End Try
            Return False
        End Function

        Public Shared Function TryEncryptBody(account As MailAccount, toAddrs As String, bodyText As String, sign As Boolean, encrypt As Boolean, ByRef bodyEntity As MimeEntity) As Boolean
            LastError = Nothing
            Dim val As TextPart = New TextPart("plain")
            val.Text = If(Not Equals(bodyText, Nothing), bodyText, "")
            bodyEntity = CType(CObj(val), MimeEntity)
            Dim result As Boolean
            If Not sign AndAlso Not encrypt Then
                result = True
            Else
                Try
                    Dim lcarsOpenPgpContext As LcarsOpenPgpContext = CreateContext()
                    Try
                        Dim list = ParseMailboxAddresses(toAddrs)
                        If encrypt AndAlso list.Count = 0 Then
                            LastError = "No recipient addresses."
                            result = False
                            GoTo IL_0100
                        End If
                        val = New TextPart("plain")
                        val.Text = If(Not Equals(bodyText, Nothing), bodyText, "")
                        Dim val2 As MimeEntity = CType(CObj(val), MimeEntity)
                        Dim cancellationToken As CancellationToken = Nothing
                        If sign AndAlso encrypt Then
                            Dim val3 As MailboxAddress = ResolveSigner(account)
                            bodyEntity = CType(CObj(MultipartEncrypted.SignAndEncrypt(CType(CObj(lcarsOpenPgpContext), OpenPgpContext), val3, CType(8, DigestAlgorithm), list, val2, cancellationToken)), MimeEntity)
                            result = True
                            GoTo IL_0100
                        End If
                        If encrypt Then
                            bodyEntity = CType(CObj(MultipartEncrypted.Encrypt(CType(CObj(lcarsOpenPgpContext), OpenPgpContext), list, val2, cancellationToken)), MimeEntity)
                            result = True
                            GoTo IL_0100
                        End If

                    Finally
                        If CObj(lcarsOpenPgpContext) IsNot Nothing Then CType(CObj(lcarsOpenPgpContext), IDisposable).Dispose()
                    End Try
                Catch ex As Exception
                    ProjectData.SetProjectError(ex)
                    Dim ex2 = ex
                    LastError = ex2.Message
                    result = False
                    Call ProjectData.ClearProjectError()
                    GoTo IL_0100
                End Try
                result = True
            End If
            GoTo IL_0100
IL_0100:
            Return result
        End Function

        Public Shared Function MessageLooksEncrypted(message As MimeMessage) As Boolean
            If message Is Nothing Then
                Return False
            End If
            Return TypeOf message.Body Is MultipartEncrypted
        End Function

        Private Shared Function ResolveSigner(account As MailAccount) As MailboxAddress
            Dim text = If(account IsNot Nothing, account.EmailAddress, "")
            Dim text2 = If(account IsNot Nothing, account.DisplayName, "")
            If String.IsNullOrWhiteSpace(text) Then
                text = "user@local"
            End If
            Return New MailboxAddress(If(Not Equals(text2, Nothing), text2, ""), text.Trim())
        End Function

        Private Shared Function ParseMailboxAddresses(toAddrs As String) As List(Of MailboxAddress)
            Dim list As List(Of MailboxAddress) = New List(Of MailboxAddress)()
            Dim array = If(Not Equals(toAddrs, Nothing), toAddrs, "").Split(","c, ";"c)
            For Each text In array
                Dim text2 As String = text.Trim()
                If text2.Length = 0 Then
                    Continue For
                End If
                Try
                    Dim val As MailboxAddress = MailboxAddress.Parse(text2)
                    If val IsNot Nothing Then
                        list.Add(val)
                    End If
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    Call ProjectData.ClearProjectError()
                End Try
            Next
            Return list
        End Function

        Private Shared Sub ExtractBodies(entity As MimeEntity, ByRef plainText As String, ByRef htmlText As String)
            If entity Is Nothing Then
                Return
            End If
            Dim val As TextPart = CType(If((TypeOf entity Is TextPart), entity, Nothing), TextPart)
            If val IsNot Nothing Then
                Dim text As String = val.Text
                Dim text2 = If(Not Equals(text, Nothing), text, "")
                If val.IsHtml Then
                    If htmlText.Length = 0 Then
                        htmlText = text2
                    End If
                ElseIf plainText.Length = 0 Then
                    plainText = text2
                End If
                Return
            End If
            Dim val2 As Multipart = CType(If((TypeOf entity Is Multipart), entity, Nothing), Multipart)
            If val2 Is Nothing Then
                Return
            End If
            For Each item As MimeEntity In val2
                MailPgpService.ExtractBodies(item, plainText, htmlText)
            Next
        End Sub

        Private Shared Function ExtractText(entity As MimeEntity) As String
            Dim plainText = ""
            Dim htmlText = ""
            MailPgpService.ExtractBodies(entity, plainText, htmlText)
            If plainText.Length > 0 Then
                Return plainText
            End If
            Return htmlText
        End Function
    End Class

