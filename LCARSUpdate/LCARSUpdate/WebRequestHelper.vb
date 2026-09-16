Friend Module WebRequestHelper

    Private Const DefaultTimeoutMs As Integer = 300000

    ''' <summary>
    ''' Applies timeout and proxy settings suited to LCARS update servers (including LAN hosts).
    ''' </summary>
    Public Function UsesLocalNetwork(ByVal url As String) As Boolean
        Return ShouldBypassProxy(url)
    End Function

    Public Sub ConfigureWebClient(ByVal client As System.Net.WebClient, ByVal url As String)
        client.Headers.Add("User-Agent", "LCARSUpdate/1.0")
        If ShouldBypassProxy(url) Then
            client.Proxy = Nothing
        Else
            Dim proxy As System.Net.IWebProxy = System.Net.WebRequest.GetSystemWebProxy()
            proxy.Credentials = System.Net.CredentialCache.DefaultCredentials
            client.Proxy = proxy
        End If
    End Sub

    Public Sub Configure(ByVal request As System.Net.WebRequest, ByVal url As String)
        request.Timeout = DefaultTimeoutMs
        Dim httpRequest As System.Net.HttpWebRequest = TryCast(request, System.Net.HttpWebRequest)
        If httpRequest IsNot Nothing Then
            httpRequest.ReadWriteTimeout = DefaultTimeoutMs
        End If

        If ShouldBypassProxy(url) Then
            request.Proxy = Nothing
        Else
            Dim proxy As System.Net.IWebProxy = System.Net.WebRequest.GetSystemWebProxy()
            proxy.Credentials = System.Net.CredentialCache.DefaultCredentials
            request.Proxy = proxy
        End If
    End Sub

    Private Function ShouldBypassProxy(ByVal url As String) As Boolean
        Try
            Dim uri As New Uri(url)
            Dim host As String = uri.Host.ToLower()
            If host = "localhost" Then
                Return True
            End If

            Dim parts() As String = host.Split("."c)
            If parts.Length <> 4 Then
                Return False
            End If

            Dim firstOctet As Integer = Integer.Parse(parts(0))
            Dim secondOctet As Integer = Integer.Parse(parts(1))

            If firstOctet = 10 Then
                Return True
            End If
            If firstOctet = 192 AndAlso secondOctet = 168 Then
                Return True
            End If
            If firstOctet = 172 AndAlso secondOctet >= 16 AndAlso secondOctet <= 31 Then
                Return True
            End If
            If firstOctet = 127 Then
                Return True
            End If
        Catch
        End Try

        Return False
    End Function

End Module
