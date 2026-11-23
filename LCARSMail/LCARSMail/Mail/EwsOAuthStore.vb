Public NotInheritable Class EwsOAuthStore
        Private Const AccessKey As String = "ews-oauth"

        Private Const RefreshKey As String = "ews-oauth-refresh"

        Private Sub New()
        End Sub

        Public Shared Sub SaveToken(token As String)
            If Not Equals(token, Nothing) Then
                AccountStore.SavePassword("ews-oauth", token)
            End If
        End Sub

        Public Shared Sub SaveRefreshToken(token As String)
            If Not Equals(token, Nothing) Then
                AccountStore.SavePassword("ews-oauth-refresh", token)
            End If
        End Sub

        Public Shared Function TryLoadToken(ByRef token As String) As Boolean
            token = Nothing
            Return AccountStore.TryGetPassword("ews-oauth", token)
        End Function

        Public Shared Function TryLoadRefreshToken(ByRef token As String) As Boolean
            token = Nothing
            Return AccountStore.TryGetPassword("ews-oauth-refresh", token)
        End Function

        Public Shared Function TryEnsureAccessToken(ByRef accessToken As String, tenant As String, clientId As String) As Boolean
            accessToken = Nothing
            Dim token As String = Nothing
            If TryLoadToken(token) AndAlso Not String.IsNullOrWhiteSpace(token) Then
                accessToken = token
                Return True
            End If
            Dim token2 As String = Nothing
            If Not TryLoadRefreshToken(token2) OrElse String.IsNullOrWhiteSpace(token2) Then
                Return False
            End If
            Dim accessToken2 As String = Nothing
            Dim newRefreshToken As String = Nothing
            If EwsOAuthDeviceFlow.TryRefreshAccessToken(tenant, clientId, token2, accessToken2, newRefreshToken) <> EwsOAuthDeviceFlow.TokenPollResult.Success Then
                Return False
            End If
            If Not String.IsNullOrWhiteSpace(accessToken2) Then
                SaveToken(accessToken2)
                accessToken = accessToken2
            End If
            If Not String.IsNullOrWhiteSpace(newRefreshToken) Then
                SaveRefreshToken(newRefreshToken)
            End If
            Return If(Not Equals(accessToken, Nothing) AndAlso accessToken.Length > 0, True, False)
        End Function
    End Class

