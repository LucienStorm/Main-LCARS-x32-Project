Imports System
Imports System.IO
Imports System.Net
Imports System.Security.Cryptography
Imports System.Text
Imports System.Text.RegularExpressions
Imports Microsoft.VisualBasic.CompilerServices
Imports MimeKit
    Public NotInheritable Class MailAutocryptService
        Public Shared LastError As String

        Private Shared ReadOnly AutocryptHeaderPattern As Regex = New Regex("Autocrypt:\s*([^;\r\n]+(?:;\s*[^;\r\n]+)*)", RegexOptions.IgnoreCase Or RegexOptions.Compiled)

        Private Sub New()
        End Sub

        Public Shared Function TryFetchWkdKeyArmored(email As String) As String
            LastError = Nothing
            Dim text As String = If(Not Equals(email, Nothing), email, "").Trim()
            If text.Length = 0 OrElse Not text.Contains("@") Then
                LastError = "Email required for WKD."
                Return ""
            End If
            Dim num = text.LastIndexOf("@"c)
            Dim text2 = text.Substring(0, num)
            Dim text3 = text.Substring(num + 1)
            Dim text4 = WkdHashLocalPart(text2)
            Dim requestUriString = "https://openpgpkey." & text3 & "/.well-known/openpgpkey/" & text3 & "/hu/" & text4 & "?l=" & Uri.EscapeDataString(text2)
            Try
                Dim httpWebRequest = CType(WebRequest.Create(requestUriString), HttpWebRequest)
                httpWebRequest.Method = "GET"
                httpWebRequest.Timeout = 20000
                Dim httpWebResponse As HttpWebResponse = CType(httpWebRequest.GetResponse(), HttpWebResponse)
                Dim streamReader As StreamReader = New StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8)
                Dim text5 As String = streamReader.ReadToEnd()
                If text5.IndexOf("BEGIN PGP PUBLIC KEY BLOCK", StringComparison.OrdinalIgnoreCase) >= 0 Then
                    Return text5
                End If
            Catch ex As Exception
                ProjectData.SetProjectError(ex)
                Dim ex2 = ex
                LastError = ex2.Message
                Call ProjectData.ClearProjectError()
            End Try
            Return ""
        End Function

        Public Shared Function TryImportWkdForEmail(email As String) As Boolean
            Dim text = TryFetchWkdKeyArmored(email)
            If text.Length = 0 Then
                Return False
            End If
            Dim path As String = IO.Path.Combine(IO.Path.GetTempPath(), "wkd-" & Guid.NewGuid().ToString("N") & ".asc")
            Try
                File.WriteAllText(path, text, Encoding.UTF8)
                Return MailPgpService.ImportArmoredKeyFile(path) > 0
            Finally
                Try
                    If File.Exists(path) Then
                        File.Delete(path)
                    End If
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    Call ProjectData.ClearProjectError()
                End Try
            End Try
        End Function

        Public Shared Function BuildOutgoingAutocryptHeader(account As MailAccount) As String
            If account Is Nothing Then
                Return ""
            End If
            Dim text As String = If(Not Equals(account.EmailAddress, Nothing), account.EmailAddress, "").Trim()
            If text.Length = 0 Then
                Return ""
            End If
            Dim text2 As String = FindFirstPublicKeyFile()
            If Equals(text2, Nothing) Then
                Return ""
            End If
            Dim armored = File.ReadAllText(text2, Encoding.UTF8)
            Dim text3 = ExtractBase64Key(armored)
            If text3.Length = 0 Then
                Return ""
            End If
            Return "addr=" & text & "; keydata=" & text3
        End Function

        Public Shared Sub ApplyAutocryptFromMessage(message As MimeMessage)
            If message Is Nothing OrElse message.Headers Is Nothing Then
                Return
            End If
            Dim text As String = message.Headers("Autocrypt")
            If String.IsNullOrWhiteSpace(text) Then
                Return
            End If
            Dim text2 = ExtractAutocryptParam(text, "keydata")
            If text2.Length = 0 Then
                Return
            End If
            Try
                Dim bytes = Convert.FromBase64String(text2)
                Dim text3 = Encoding.UTF8.GetString(bytes)
                If text3.IndexOf("BEGIN PGP", StringComparison.OrdinalIgnoreCase) < 0 Then
                    text3 = "-----BEGIN PGP PUBLIC KEY BLOCK-----" & vbCrLf & text2 & vbCrLf & "-----END PGP PUBLIC KEY BLOCK-----"
                End If
                Dim path As String = IO.Path.Combine(IO.Path.GetTempPath(), "autocrypt-" & Guid.NewGuid().ToString("N") & ".asc")
                File.WriteAllText(path, text3, Encoding.UTF8)
                MailPgpService.ImportArmoredKeyFile(path)
                Try
                    File.Delete(path)
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    Call ProjectData.ClearProjectError()
                End Try
            Catch projectError2 As Exception
                ProjectData.SetProjectError(projectError2)
                Call ProjectData.ClearProjectError()
            End Try
        End Sub

        Private Shared Function ExtractAutocryptParam(header As String, name As String) As String
            Dim array = header.Split(";"c)
            Dim array2 = array
            For Each text In array2
                Dim text2 As String = text.Trim()
                If text2.StartsWith(name & "=", StringComparison.OrdinalIgnoreCase) Then
                    Return text2.Substring(name.Length + 1).Trim()
                End If
            Next
            Return ""
        End Function

        Private Shared Function ExtractBase64Key(armored As String) As String
            Dim stringBuilder As StringBuilder = New StringBuilder()
            Dim flag = False
            Dim array = armored.Split(New String(1) {vbCrLf, vbLf}, StringSplitOptions.None)
            For Each text In array
                Dim text2 As String = text.Trim()
                If text2.StartsWith("-----BEGIN", StringComparison.Ordinal) Then
                    flag = True
                    Continue For
                End If
                If text2.StartsWith("-----END", StringComparison.Ordinal) Then
                    Exit For
                End If
                If flag AndAlso text2.Length > 0 Then
                    stringBuilder.Append(text2)
                End If
            Next
            Return stringBuilder.ToString()
        End Function

        Private Shared Function FindFirstPublicKeyFile() As String
            Try
                Dim files As String() = Directory.GetFiles(MailPgpService.GnupgHomeDirectory(), "*.asc")
                Dim num = 0
                If num < files.Length Then
                    Return files(num)
                End If
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
            Return Nothing
        End Function

        Private Shared Function WkdHashLocalPart(localPart As String) As String
            Dim sHA As SHA1 = SHA1.Create()
            Dim array = sHA.ComputeHash(Encoding.UTF8.GetBytes(localPart))
            Dim stringBuilder As StringBuilder = New StringBuilder(array.Length * 2)
            Dim array2 = array
            For Each b In array2
                stringBuilder.Append(b.ToString("x2"))
            Next
            Return stringBuilder.ToString()
        End Function
    End Class

