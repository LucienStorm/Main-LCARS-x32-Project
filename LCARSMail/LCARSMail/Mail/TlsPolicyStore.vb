Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Net.Security
Imports System.Security.Cryptography.X509Certificates
Imports System.Text
Imports System.Web.Script.Serialization
Imports Microsoft.VisualBasic.CompilerServices
    Public NotInheritable Class TlsPolicyStore
        Private Const PolicyFileName As String = "tls-policy.json"

        Private Sub New()
        End Sub

        Public Shared Function ValidateCertificate(host As String, port As Integer, certificate As X509Certificate2, sslPolicyErrors As SslPolicyErrors, strictMode As Boolean) As Boolean
            If certificate Is Nothing Then
                Return Not strictMode
            End If
            If sslPolicyErrors = SslPolicyErrors.None Then
                RememberThumbprint(host, port, certificate)
                Return True
            End If
            Dim key = EndpointKey(host, port)
            Dim value As String = Nothing
            Dim dictionary As Dictionary(Of String, String) = LoadMap()
            dictionary.TryGetValue(key, value)
            Dim thumbprint = certificate.Thumbprint
            If Not String.IsNullOrEmpty(value) AndAlso String.Equals(value, thumbprint, StringComparison.OrdinalIgnoreCase) Then
                Return True
            End If
            If strictMode Then
                Return False
            End If
            RememberThumbprint(host, port, certificate)
            Return True
        End Function

        Public Shared Sub RememberThumbprint(host As String, port As Integer, certificate As X509Certificate2)
            If certificate IsNot Nothing Then
                Dim key = EndpointKey(host, port)
                Dim dictionary As Dictionary(Of String, String) = LoadMap()
                dictionary(key) = certificate.Thumbprint
                SaveMap(dictionary)
            End If
        End Sub

        Private Shared Function EndpointKey(host As String, port As Integer) As String
            Return If(Not Equals(host, Nothing), host, "").Trim().ToLowerInvariant() & ":" & port.ToString()
        End Function

        Private Shared Function PolicyPath() As String
            Return Path.Combine(AccountStore.DataRoot(), "tls-policy.json")
        End Function

        Private Shared Function LoadMap() As Dictionary(Of String, String)
            Dim path As String = PolicyPath()
            Dim result As Dictionary(Of String, String)
            If Not File.Exists(path) Then
                result = New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            Else
                Try
                    Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                    Dim dictionary = javaScriptSerializer.Deserialize(Of Dictionary(Of String, String))(File.ReadAllText(path, Encoding.UTF8))
                    result = (If(dictionary IsNot Nothing, New Dictionary(Of String, String)(dictionary, StringComparer.OrdinalIgnoreCase), New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)))
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    result = New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Private Shared Sub SaveMap(map As Dictionary(Of String, String))
            If map IsNot Nothing Then
                Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                Call File.WriteAllText(PolicyPath(), javaScriptSerializer.Serialize(map), Encoding.UTF8)
            End If
        End Sub
    End Class

