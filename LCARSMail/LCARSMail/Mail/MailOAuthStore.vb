Imports System
Imports System.Globalization
    Public NotInheritable Class MailOAuthStore
        Private Sub New()
        End Sub

        Public Shared Sub SaveTokens(accountId As String, accessToken As String, refreshToken As String, expiresInSeconds As Integer)
            If Not String.IsNullOrWhiteSpace(accountId) Then
                If Not String.IsNullOrWhiteSpace(accessToken) Then
                    AccountStore.SavePassword(AccessKey(accountId), accessToken)
                End If
                If Not String.IsNullOrWhiteSpace(refreshToken) Then
                    AccountStore.SavePassword(RefreshKey(accountId), refreshToken)
                End If
                Dim dateTime = Date.UtcNow.AddSeconds(Math.Max(60, expiresInSeconds) - 60)
                AccountStore.SavePassword(ExpiresKey(accountId), dateTime.ToString("o", CultureInfo.InvariantCulture))
            End If
        End Sub

        Public Shared Function TryLoadAccessToken(accountId As String, ByRef accessToken As String) As Boolean
            accessToken = Nothing
            Return If(AccountStore.TryGetPassword(AccessKey(accountId), accessToken) AndAlso Not String.IsNullOrWhiteSpace(accessToken), True, False)
        End Function

        Public Shared Function TryLoadRefreshToken(accountId As String, ByRef refreshToken As String) As Boolean
            refreshToken = Nothing
            Return If(AccountStore.TryGetPassword(RefreshKey(accountId), refreshToken) AndAlso Not String.IsNullOrWhiteSpace(refreshToken), True, False)
        End Function

        Public Shared Function IsAccessTokenFresh(accountId As String) As Boolean
            Dim password As String = Nothing
            If Not AccountStore.TryGetPassword(ExpiresKey(accountId), password) OrElse String.IsNullOrWhiteSpace(password) Then
                Return False
            End If
            Dim result As Date = Nothing

            If Not Date.TryParse(password, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, result) Then
                Return False
            End If
            Return Date.Compare(result, Date.UtcNow) > 0
        End Function

        Public Shared Function TryEnsureAccessToken(account As MailAccount, ByRef accessToken As String) As Boolean
            accessToken = Nothing
            If account Is Nothing OrElse String.IsNullOrWhiteSpace(account.Id) Then
                Return False
            End If
            Dim id = account.Id
            Dim accessToken2 As String = Nothing
            If IsAccessTokenFresh(id) AndAlso TryLoadAccessToken(id, accessToken2) Then
                accessToken = accessToken2
                Return True
            End If
            Dim refreshToken As String = Nothing
            If Not TryLoadRefreshToken(id, refreshToken) Then
                If TryLoadAccessToken(id, accessToken2) Then
                    accessToken = accessToken2
                    Return True
                End If
                Return False
            End If
            Dim providerInfo = MailOAuthProviders.TryResolveProvider(account)
            If providerInfo Is Nothing Then
                Return False
            End If
            Dim accessToken3 As String = Nothing
            Dim newRefreshToken As String = Nothing
            Dim expiresInSeconds = 3600
            If Not MailOAuthBrowserFlow.TryRefresh(providerInfo, refreshToken, accessToken3, newRefreshToken, expiresInSeconds) Then
                Return False
            End If
            If String.IsNullOrWhiteSpace(newRefreshToken) Then
                newRefreshToken = refreshToken
            End If
            SaveTokens(id, accessToken3, newRefreshToken, expiresInSeconds)
            accessToken = accessToken3
            Return Not String.IsNullOrWhiteSpace(accessToken)
        End Function

        Private Shared Function AccessKey(accountId As String) As String
            Return accountId & "|oauth-access"
        End Function

        Private Shared Function RefreshKey(accountId As String) As String
            Return accountId & "|oauth-refresh"
        End Function

        Private Shared Function ExpiresKey(accountId As String) As String
            Return accountId & "|oauth-expires"
        End Function
    End Class

