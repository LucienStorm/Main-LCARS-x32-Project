' LCARSMail/LCARSMail/Mail/AccountStore.vb
Option Strict On
Option Infer On
Option Explicit On

Imports System.Collections.Generic
Imports System.IO
Imports System.Security.Cryptography
Imports System.Text
Imports System.Web.Script.Serialization

''' <summary>
''' Account metadata in AppData; passwords via DPAPI (CurrentUser).
''' Optional SMTP-only secret: pass accountId &amp; "|smtp" to SavePassword / TryGetPassword.
''' </summary>
Public Module AccountStore

    Private Const AccountsFileName As String = "accounts.json"
    Private Const SecretsSubdir As String = "secrets"

    ''' <summary>Set when SaveAccounts returns False.</summary>
    Public LastError As String

    Public Function DataRoot() As String
        Dim root As String = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LCARS x32", "Mail")
        If Not Directory.Exists(root) Then Directory.CreateDirectory(root)
        Return root
    End Function

    Public Function SaveAccounts(ByVal accounts As List(Of MailAccount)) As Boolean
        LastError = Nothing
        If accounts Is Nothing Then
            LastError = "accounts is required."
            Return False
        End If
        Try
            Dim root As String = DataRoot()
            If Not Directory.Exists(root) Then Directory.CreateDirectory(root)
            Dim dtoList As New List(Of AccountDto)()
            For Each acct As MailAccount In accounts
                dtoList.Add(AccountDto.FromModel(acct))
            Next
            Dim ser As New JavaScriptSerializer()
            File.WriteAllText(AccountsJsonPath(), ser.Serialize(dtoList), Encoding.UTF8)
            Return True
        Catch ex As Exception
            LastError = ex.Message
            Return False
        End Try
    End Function

    Public Function LoadAccounts() As List(Of MailAccount)
        Dim path As String = AccountsJsonPath()
        If Not File.Exists(path) Then Return New List(Of MailAccount)()
        Try
            Dim ser As New JavaScriptSerializer()
            Dim list As List(Of AccountDto) = ser.Deserialize(Of List(Of AccountDto))(File.ReadAllText(path, Encoding.UTF8))
            If list Is Nothing Then Return New List(Of MailAccount)()
            Dim result As New List(Of MailAccount)()
            For Each item As AccountDto In list
                If item IsNot Nothing Then result.Add(item.ToModel())
            Next
            Return result
        Catch
            Return New List(Of MailAccount)()
        End Try
    End Function

    Public Sub SavePassword(ByVal accountId As String, ByVal password As String)
        If String.IsNullOrWhiteSpace(accountId) Then
            Throw New ArgumentException("accountId is required.", "accountId")
        End If
        If password Is Nothing Then password = ""
        Dim plain As Byte() = Encoding.UTF8.GetBytes(password)
        Dim protectedBytes As Byte() = ProtectedData.Protect(plain, Nothing, DataProtectionScope.CurrentUser)
        Dim dir As String = Path.Combine(DataRoot(), SecretsSubdir)
        If Not Directory.Exists(dir) Then Directory.CreateDirectory(dir)
        File.WriteAllBytes(SecretPath(accountId), protectedBytes)
    End Sub

    Public Function TryGetPassword(ByVal accountId As String, ByRef password As String) As Boolean
        password = Nothing
        If String.IsNullOrWhiteSpace(accountId) Then Return False
        Dim path As String = SecretPath(accountId)
        If Not File.Exists(path) Then Return False
        Try
            Dim encrypted As Byte() = File.ReadAllBytes(path)
            Dim plain As Byte() = ProtectedData.Unprotect(encrypted, Nothing, DataProtectionScope.CurrentUser)
            password = Encoding.UTF8.GetString(plain)
            Return True
        Catch
            password = Nothing
            Return False
        End Try
    End Function

    Private Function AccountsJsonPath() As String
        Return Path.Combine(DataRoot(), AccountsFileName)
    End Function

    Private Function SecretPath(ByVal accountId As String) As String
        Return Path.Combine(DataRoot(), SecretsSubdir, SanitizeFileToken(accountId) & ".dpapi")
    End Function

    Private Function SanitizeFileToken(ByVal accountId As String) As String
        Dim invalid As Char() = Path.GetInvalidFileNameChars()
        Dim sb As New StringBuilder(accountId.Length)
        For Each ch As Char In accountId
            If Array.IndexOf(invalid, ch) >= 0 Then
                sb.Append("_"c)
            Else
                sb.Append(ch)
            End If
        Next
        Dim token As String = sb.ToString()
        If String.IsNullOrWhiteSpace(token) Then token = "account"
        Return token
    End Function

    Private Class AccountDto
        Public Property Id As String
        Public Property DisplayName As String
        Public Property EmailAddress As String
        Public Property ImapHost As String
        Public Property ImapPort As Integer
        Public Property ImapSsl As Boolean
        Public Property ImapStartTls As Boolean
        Public Property Username As String
        Public Property SmtpHost As String
        Public Property SmtpPort As Integer
        Public Property SmtpSsl As Boolean
        Public Property SmtpStartTls As Boolean
        Public Property SmtpUsername As String
        Public Property Protocol As String
        Public Property EwsUrl As String
        Public Property AuthMethod As String

        Public Shared Function FromModel(ByVal acct As MailAccount) As AccountDto
            Dim dto As New AccountDto()
            dto.Protocol = If(acct.Protocol, "")
            dto.EwsUrl = If(acct.EwsUrl, "")
            dto.Id = If(acct.Id, "")
            dto.DisplayName = If(acct.DisplayName, "")
            dto.EmailAddress = If(acct.EmailAddress, "")
            dto.ImapHost = If(acct.ImapHost, "")
            dto.ImapPort = acct.ImapPort
            dto.ImapSsl = acct.ImapSsl
            dto.ImapStartTls = acct.ImapStartTls
            dto.Username = If(acct.Username, "")
            dto.SmtpHost = If(acct.SmtpHost, "")
            dto.SmtpPort = If(acct.SmtpPort > 0, acct.SmtpPort, 587)
            dto.SmtpSsl = acct.SmtpSsl
            dto.SmtpStartTls = acct.SmtpStartTls
            dto.SmtpUsername = If(acct.SmtpUsername, "")
            dto.AuthMethod = If(acct.AuthMethod, "")
            Return dto
        End Function

        Public Function ToModel() As MailAccount
            Dim acct As New MailAccount()
            acct.Protocol = If(Protocol, "")
            acct.EwsUrl = If(EwsUrl, "")
            acct.Id = If(Id, "")
            acct.DisplayName = If(DisplayName, "")
            acct.EmailAddress = If(EmailAddress, "")
            acct.ImapHost = If(ImapHost, "")
            acct.ImapPort = ImapPort
            acct.ImapSsl = ImapSsl
            acct.ImapStartTls = ImapStartTls
            acct.Username = If(Username, "")
            acct.SmtpHost = If(SmtpHost, "")
            acct.SmtpPort = If(SmtpPort > 0, SmtpPort, 587)
            acct.SmtpSsl = SmtpSsl
            acct.SmtpStartTls = SmtpStartTls
            acct.SmtpUsername = If(SmtpUsername, "")
            acct.AuthMethod = If(AuthMethod, "")
            Return acct
        End Function
    End Class

End Module
