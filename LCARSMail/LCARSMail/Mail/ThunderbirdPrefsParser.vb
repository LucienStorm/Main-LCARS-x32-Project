Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Text
Imports System.Text.RegularExpressions
Imports Microsoft.VisualBasic.CompilerServices
    Public NotInheritable Class ThunderbirdPrefsParser
        Private Shared ReadOnly UserPrefPattern As Regex = New Regex("^\s*user_pref\s*\(\s*""([^""]+)""\s*,\s*(.+?)\s*\)\s*;\s*$", RegexOptions.Compiled Or RegexOptions.CultureInvariant)

        Public Shared LastError As String

        Private Sub New()
        End Sub

        Public Shared Function ParsePrefsJs(prefsJsPath As String) As List(Of MailAccount)
            LastError = Nothing
            Dim result As List(Of MailAccount)
            Try
                If String.IsNullOrWhiteSpace(prefsJsPath) Then
                    LastError = "prefsJsPath is required."
                    result = New List(Of MailAccount)()
                ElseIf Not File.Exists(prefsJsPath) Then
                    LastError = "prefs.js not found: " & prefsJsPath
                    result = New List(Of MailAccount)()
                Else
                    Dim dictionary = LoadPrefs(prefsJsPath)
                    Dim list As List(Of MailAccount) = New List(Of MailAccount)()
                    Dim value As String = Nothing
                    If Not dictionary.TryGetValue("mail.accountmanager.accounts", value) Then
                        result = list
                    Else
                        Dim array = SplitCsv(value)
                        For Each text In array
                            If String.IsNullOrWhiteSpace(text) Then
                                Continue For
                            End If
                            Dim pref = GetPref(dictionary, "mail.account." & text & ".server")
                            If String.IsNullOrWhiteSpace(pref) Then
                                Continue For
                            End If
                            Dim pref2 = GetPref(dictionary, "mail.server." & pref & ".type")
                            If Not String.Equals(pref2, "imap", StringComparison.OrdinalIgnoreCase) Then
                                Continue For
                            End If
                            Dim text2 = FirstCsvToken(GetPref(dictionary, "mail.account." & text & ".identities"))
                            Dim text3 = ""
                            Dim text4 = ""
                            If Not String.IsNullOrWhiteSpace(text2) Then
                                text3 = GetPref(dictionary, "mail.identity." & text2 & ".useremail")
                                text4 = GetPref(dictionary, "mail.identity." & text2 & ".fullname")
                            End If
                            Dim text5 = GetPref(dictionary, "mail.server." & pref & ".userName")
                            If String.IsNullOrWhiteSpace(text5) Then
                                text5 = text3
                            End If
                            Dim pref3 = GetPref(dictionary, "mail.server." & pref & ".hostname")
                            If String.IsNullOrWhiteSpace(pref3) OrElse String.IsNullOrWhiteSpace(text5) Then
                                Continue For
                            End If
                            Dim pref4 = GetPref(dictionary, "mail.server." & pref & ".socketType")
                            Dim imapSsl = False
                            Dim imapStartTls = False
                            ParseSocketType(pref4, imapSsl, imapStartTls)
                            Dim imapPort = ParsePort(GetPref(dictionary, "mail.server." & pref & ".port"), imapSsl, imapStartTls)
                            Dim smtpHost = ""
                            Dim smtpPort = 587
                            Dim imapSsl2 = False
                            Dim imapStartTls2 = False
                            Dim text6 = ""
                            If Not String.IsNullOrWhiteSpace(text2) Then
                                Dim pref5 = GetPref(dictionary, "mail.identity." & text2 & ".smtpServer")
                                If Not String.IsNullOrWhiteSpace(pref5) Then
                                    Dim pref6 = GetPref(dictionary, "mail.smtpserver." & pref5 & ".hostname")
                                    smtpHost = If(Not Equals(pref6, Nothing), pref6, "")
                                    text6 = GetPref(dictionary, "mail.smtpserver." & pref5 & ".username")
                                    If String.IsNullOrWhiteSpace(text6) Then
                                        text6 = If(Not Equals(text3, Nothing), text3, "")
                                    End If
                                    Dim pref7 = GetPref(dictionary, "mail.smtpserver." & pref5 & ".socketType")
                                    ParseSocketType(pref7, imapSsl2, imapStartTls2)
                                    smtpPort = ParseSmtpPort(GetPref(dictionary, "mail.smtpserver." & pref5 & ".port"), imapSsl2, imapStartTls2)
                                End If
                            End If
                            Dim strictTls = Not String.Equals(GetPref(dictionary, "mail.server." & pref & ".allowUntrustedCerts"), "true", StringComparison.OrdinalIgnoreCase)
                            Dim pref8 = GetPref(dictionary, "mail.server." & pref & ".authMethod")
                            Dim authMethod = "password"
                            If MailOAuthProviders.IsThunderbirdOAuthAuthMethod(pref8) OrElse MailOAuthProviders.TryResolveByHostname(pref3) IsNot Nothing Then
                                authMethod = "oauth2"
                            End If
                            Dim mailAccount As MailAccount = New MailAccount()
                            mailAccount.Id = text
                            mailAccount.DisplayName = If(Not Equals(text4, Nothing), text4, "")
                            mailAccount.EmailAddress = If(Not Equals(text3, Nothing), text3, "")
                            mailAccount.ImapHost = pref3
                            mailAccount.ImapPort = imapPort
                            mailAccount.ImapSsl = imapSsl
                            mailAccount.ImapStartTls = imapStartTls
                            mailAccount.Username = text5
                            mailAccount.SmtpHost = smtpHost
                            mailAccount.SmtpPort = smtpPort
                            mailAccount.SmtpSsl = imapSsl2
                            mailAccount.SmtpStartTls = imapStartTls2
                            mailAccount.SmtpUsername = text6
                            mailAccount.StrictTls = strictTls
                            mailAccount.AuthMethod = authMethod
                            Dim item = mailAccount
                            list.Add(item)
                        Next
                        result = list
                    End If
                End If
            Catch ex As Exception
                ProjectData.SetProjectError(ex)
                Dim ex2 = ex
                LastError = ex2.Message
                result = New List(Of MailAccount)()
                Call ProjectData.ClearProjectError()
            End Try
            Return result
        End Function

        Private Shared Function LoadPrefs(prefsJsPath As String) As Dictionary(Of String, String)
            Dim dictionary As Dictionary(Of String, String) = New Dictionary(Of String, String)(StringComparer.Ordinal)
            Try
                Dim array = File.ReadAllLines(prefsJsPath, Encoding.UTF8)
                For Each text In array
                    If String.IsNullOrWhiteSpace(text) Then
                        Continue For
                    End If
                    Dim text2 As String = text.Trim()
                    If Not text2.StartsWith("//", StringComparison.Ordinal) Then
                        Dim match = UserPrefPattern.Match(text2)
                        If match.Success Then
                            Dim value = match.Groups(1).Value
                            Dim valueRaw As String = match.Groups(2).Value.Trim()
                            dictionary(value) = ParsePrefValue(valueRaw)
                        End If
                    End If
                Next
            Catch ex As Exception
                ProjectData.SetProjectError(ex)
                Dim ex2 = ex
                Throw New IOException("Failed to read prefs.js: " & ex2.Message, ex2)
            End Try
            Return dictionary
        End Function

        Private Shared Function ParsePrefValue(valueRaw As String) As String
            If Equals(valueRaw, Nothing) OrElse valueRaw.Length = 0 Then
                Return ""
            End If
            If valueRaw(0) = """"c Then
                Dim stringBuilder As StringBuilder = New StringBuilder()
                Dim num = 1
                While num < valueRaw.Length
                    Dim c = valueRaw(num)
                    If c = "\"c AndAlso num + 1 < valueRaw.Length Then
                        stringBuilder.Append(valueRaw(num + 1))
                        num += 2
                        Continue While
                    End If
                    If c = """"c Then
                        Exit While
                    End If
                    stringBuilder.Append(c)
                    num += 1
                End While
                Return stringBuilder.ToString()
            End If
            If String.Equals(valueRaw, "true", StringComparison.OrdinalIgnoreCase) Then
                Return "true"
            End If
            If String.Equals(valueRaw, "false", StringComparison.OrdinalIgnoreCase) Then
                Return "false"
            End If
            Return valueRaw
        End Function

        Private Shared Function GetPref(prefs As Dictionary(Of String, String), key As String) As String
            Dim value As String = Nothing
            If prefs.TryGetValue(key, value) Then
                Return value
            End If
            Return Nothing
        End Function

        Private Shared Function SplitCsv(raw As String) As String()
            If String.IsNullOrWhiteSpace(raw) Then
                Return New String(-1) {}
            End If
            Return raw.Split(New Char(0) {","c}, StringSplitOptions.RemoveEmptyEntries)
        End Function

        Private Shared Function FirstCsvToken(raw As String) As String
            Dim array = SplitCsv(If(Not Equals(raw, Nothing), raw, ""))
            If array.Length = 0 Then
                Return Nothing
            End If
            Return array(0).Trim()
        End Function

        Private Shared Sub ParseSocketType(socketTypeRaw As String, ByRef imapSsl As Boolean, ByRef imapStartTls As Boolean)
            imapSsl = False
            imapStartTls = False
            Dim result As Integer = Nothing

            If Integer.TryParse(If(Not Equals(socketTypeRaw, Nothing), socketTypeRaw, ""), result) Then
                Select Case result
                    Case 1, 3
                        imapSsl = True
                    Case 2
                        imapSsl = True
                        imapStartTls = True
                End Select
            End If
        End Sub

        Private Shared Function ParsePort(portRaw As String, imapSsl As Boolean, imapStartTls As Boolean) As Integer
            Dim result As Integer = Nothing

            If Integer.TryParse(If(Not Equals(portRaw, Nothing), portRaw, ""), result) AndAlso result > 0 Then
                Return result
            End If
            If imapStartTls Then
                Return 143
            End If
            Return If(imapSsl, 993, 143)
        End Function

        Private Shared Function ParseSmtpPort(portRaw As String, smtpSsl As Boolean, smtpStartTls As Boolean) As Integer
            Dim result As Integer = Nothing

            If Integer.TryParse(If(Not Equals(portRaw, Nothing), portRaw, ""), result) AndAlso result > 0 Then
                Return result
            End If
            If smtpStartTls Then
                Return 587
            End If
            Return If(smtpSsl, 465, 587)
        End Function
    End Class

