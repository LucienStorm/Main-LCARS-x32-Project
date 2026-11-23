' LCARSMail/LCARSMail/Mail/MailOAuthProviders.vb
Option Strict On
Option Infer On
Option Explicit On

''' <summary>
''' Built-in OAuth issuer details aligned with Thunderbird's public OAuth2 clients
''' (Google / Microsoft). These client IDs and the Google client secret are public
''' Thunderbird values, not private LCARS credentials.
''' </summary>
Public NotInheritable Class MailOAuthProviders

    Public Const AuthMethodOAuth2 As String = "oauth2"
    Public Const AuthMethodPassword As String = "password"

    Public Class ProviderInfo
        Public Issuer As String
        Public ClientId As String
        Public ClientSecret As String
        Public AuthorizationEndpoint As String
        Public TokenEndpoint As String
        Public Scope As String
        Public UsePkce As Boolean
    End Class

    Private Sub New()
    End Sub

    ''' <summary>True when this account should use browser OAuth (XOAUTH2).</summary>
    Public Shared Function UsesOAuth(ByVal account As MailAccount) As Boolean
        If account Is Nothing Then Return False
        If EwsMailBridge.IsEwsAccount(account) Then Return False
        Dim method As String = If(account.AuthMethod, "").Trim()
        If String.Equals(method, AuthMethodPassword, StringComparison.OrdinalIgnoreCase) Then Return False
        If String.Equals(method, AuthMethodOAuth2, StringComparison.OrdinalIgnoreCase) Then Return True
        Return TryResolveProvider(account) IsNot Nothing
    End Function

    Public Shared Function TryResolveProvider(ByVal account As MailAccount) As ProviderInfo
        If account Is Nothing Then Return Nothing
        Dim host As String = If(account.ImapHost, "").Trim()
        If host.Length = 0 Then Return Nothing
        Return TryResolveByHostname(host)
    End Function

    Public Shared Function TryResolveByHostname(ByVal hostname As String) As ProviderInfo
        Dim host As String = If(hostname, "").Trim().ToLowerInvariant()
        If host.Length = 0 Then Return Nothing

        If host = "imap.gmail.com" OrElse
           host = "imap.googlemail.com" OrElse
           host.EndsWith(".gmail.com", StringComparison.Ordinal) OrElse
           host.EndsWith(".googlemail.com", StringComparison.Ordinal) OrElse
           host = "gmail.com" OrElse
           host = "googlemail.com" Then
            Return GoogleProvider()
        End If

        If host = "outlook.office365.com" OrElse
           host = "smtp.office365.com" OrElse
           host.EndsWith(".office365.com", StringComparison.Ordinal) OrElse
           host.EndsWith(".outlook.com", StringComparison.Ordinal) OrElse
           host = "outlook.office.com" OrElse
           host = "imap-mail.outlook.com" OrElse
           host = "smtp-mail.outlook.com" OrElse
           host = "hotmail.com" Then
            Return MicrosoftProvider()
        End If

        Return Nothing
    End Function

    ''' <summary>Thunderbird authMethod 10 = OAuth2.</summary>
    Public Shared Function IsThunderbirdOAuthAuthMethod(ByVal authMethodRaw As String) As Boolean
        Dim raw As String = If(authMethodRaw, "").Trim()
        If raw.Length = 0 Then Return False
        Dim n As Integer
        If Integer.TryParse(raw, n) AndAlso n = 10 Then Return True
        Return String.Equals(raw, "oauth2", StringComparison.OrdinalIgnoreCase) OrElse
               String.Equals(raw, "xoauth2", StringComparison.OrdinalIgnoreCase)
    End Function

    Public Shared Function GoogleProvider() As ProviderInfo
        Return New ProviderInfo() With {
            .Issuer = "accounts.google.com",
            .ClientId = "406964657835-aq8lmia8j95dhl1a2bvharmfk3t1hgqj.apps.googleusercontent.com",
            .ClientSecret = "kSmqreRr0qwBWJgbf5Y-PjSU",
            .AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth",
            .TokenEndpoint = "https://oauth2.googleapis.com/token",
            .Scope = "https://mail.google.com/",
            .UsePkce = True
        }
    End Function

    Public Shared Function MicrosoftProvider() As ProviderInfo
        Return New ProviderInfo() With {
            .Issuer = "login.microsoftonline.com",
            .ClientId = "9e5f94bc-e8a4-4e73-b8be-63364c29d753",
            .ClientSecret = "",
            .AuthorizationEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize",
            .TokenEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/token",
            .Scope = "https://outlook.office.com/IMAP.AccessAsUser.All https://outlook.office.com/SMTP.Send offline_access",
            .UsePkce = True
        }
    End Function

End Class
