Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Net
Imports System.Runtime.CompilerServices
Imports System.Text
Imports System.Web.Script.Serialization
Imports Microsoft.VisualBasic.CompilerServices
    Public NotInheritable Class EwsOAuthDeviceFlow
        Public Enum TokenPollResult
            Success
            Pending
            Failed
        End Enum

        Public Class DeviceStartResult
            Public UserCode As String

            Public VerificationUri As String

            Public DeviceCode As String

            Public IntervalSeconds As Integer

            Public Message As String

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Public Shared LastError As String

        Public Const DefaultClientId As String = "94933e60-ee10-4250-a050-3efd588b0718"

        Public Const DefaultScope As String = "https://outlook.office365.com/EWS.AccessAsUser.All offline_access openid profile"

        Private Sub New()
        End Sub

        Public Shared Function TryStartDeviceFlow(tenant As String, clientId As String, Optional scope As String = Nothing) As DeviceStartResult
            LastError = Nothing
            Dim text = NormalizeTenant(tenant)
            Dim text2 As String = If(Not Equals(clientId, Nothing), clientId, "").Trim()
            If text2.Length = 0 Then
                text2 = "94933e60-ee10-4250-a050-3efd588b0718"
            End If
            Dim stringToEscape As String = If(Not Equals(scope, Nothing), scope, "https://outlook.office365.com/EWS.AccessAsUser.All offline_access openid profile").Trim()
            Dim url = "https://login.microsoftonline.com/" & text & "/oauth2/v2.0/devicecode"
            Dim formBody = "client_id=" & Uri.EscapeDataString(text2) & "&scope=" & Uri.EscapeDataString(stringToEscape)
            Dim text3 = PostForm(url, formBody)
            Dim result As DeviceStartResult
            If text3.Length = 0 Then
                result = Nothing
            Else
                Try
                    Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                    Dim dictionary = javaScriptSerializer.Deserialize(Of Dictionary(Of String, Object))(text3)
                    If dictionary Is Nothing Then
                        LastError = "Invalid device code response."
                        result = Nothing
                    ElseIf dictionary.ContainsKey("error") Then
                        LastError = Stringify(RuntimeHelpers.GetObjectValue(dictionary("error_description")))
                        If LastError.Length = 0 Then
                            LastError = Stringify(RuntimeHelpers.GetObjectValue(dictionary("error")))
                        End If
                        result = Nothing
                    Else
                        Dim deviceStartResult As DeviceStartResult = New DeviceStartResult()
                        deviceStartResult.UserCode = Stringify(RuntimeHelpers.GetObjectValue(dictionary("user_code")))
                        Dim text4 = Stringify(RuntimeHelpers.GetObjectValue(dictionary("verification_uri_complete")))
                        If text4.Length > 0 Then
                            deviceStartResult.VerificationUri = text4
                        Else
                            deviceStartResult.VerificationUri = Stringify(RuntimeHelpers.GetObjectValue(dictionary("verification_uri")))
                        End If
                        deviceStartResult.DeviceCode = Stringify(RuntimeHelpers.GetObjectValue(dictionary("device_code")))
                        deviceStartResult.Message = Stringify(RuntimeHelpers.GetObjectValue(dictionary("message")))
                        deviceStartResult.IntervalSeconds = 5
                        If dictionary.ContainsKey("interval") Then
                            Dim result2 = 0
                            If Integer.TryParse(Stringify(RuntimeHelpers.GetObjectValue(dictionary("interval"))), result2) AndAlso result2 > 0 Then
                                deviceStartResult.IntervalSeconds = result2
                            End If
                        End If
                        If deviceStartResult.DeviceCode.Length = 0 Then
                            LastError = "Device code missing from response."
                            result = Nothing
                        Else
                            result = deviceStartResult
                        End If
                    End If
                Catch ex As Exception
                    ProjectData.SetProjectError(ex)
                    Dim ex2 = ex
                    LastError = ex2.Message
                    result = Nothing
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Public Shared Function TryRefreshAccessToken(tenant As String, clientId As String, refreshToken As String, ByRef accessToken As String, ByRef newRefreshToken As String) As TokenPollResult
            LastError = Nothing
            accessToken = Nothing
            newRefreshToken = Nothing
            Dim text = NormalizeTenant(tenant)
            Dim text2 As String = If(Not Equals(clientId, Nothing), clientId, "").Trim()
            If text2.Length = 0 Then
                text2 = "94933e60-ee10-4250-a050-3efd588b0718"
            End If
            Dim text3 As String = If(Not Equals(refreshToken, Nothing), refreshToken, "").Trim()
            Dim result As TokenPollResult
            If text3.Length = 0 Then
                LastError = "Refresh token required."
                result = TokenPollResult.Failed
            Else
                Dim url = "https://login.microsoftonline.com/" & text & "/oauth2/v2.0/token"
                Dim formBody = "grant_type=refresh_token&client_id=" & Uri.EscapeDataString(text2) & "&refresh_token=" & Uri.EscapeDataString(text3)
                Dim text4 = PostForm(url, formBody)
                If text4.Length = 0 Then
                    result = TokenPollResult.Failed
                Else
                    Try
                        Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                        Dim dictionary = javaScriptSerializer.Deserialize(Of Dictionary(Of String, Object))(text4)
                        If dictionary Is Nothing Then
                            LastError = "Invalid refresh response."
                            result = TokenPollResult.Failed
                        ElseIf dictionary.ContainsKey("access_token") Then
                            accessToken = Stringify(RuntimeHelpers.GetObjectValue(dictionary("access_token")))
                            newRefreshToken = Stringify(RuntimeHelpers.GetObjectValue(dictionary("refresh_token")))
                            result = TokenPollResult.Success
                        Else
                            LastError = Stringify(RuntimeHelpers.GetObjectValue(dictionary("error_description")))
                            If LastError.Length = 0 Then
                                LastError = Stringify(RuntimeHelpers.GetObjectValue(dictionary("error")))
                            End If
                            result = TokenPollResult.Failed
                        End If
                    Catch ex As Exception
                        ProjectData.SetProjectError(ex)
                        Dim ex2 = ex
                        LastError = ex2.Message
                        result = TokenPollResult.Failed
                        Call ProjectData.ClearProjectError()
                    End Try
                End If
            End If
            Return result
        End Function

        Public Shared Function TryPollAccessToken(tenant As String, clientId As String, deviceCode As String, ByRef accessToken As String, ByRef refreshToken As String) As TokenPollResult
            LastError = Nothing
            accessToken = Nothing
            refreshToken = Nothing
            Dim text = NormalizeTenant(tenant)
            Dim text2 As String = If(Not Equals(clientId, Nothing), clientId, "").Trim()
            If text2.Length = 0 Then
                text2 = "94933e60-ee10-4250-a050-3efd588b0718"
            End If
            Dim text3 As String = If(Not Equals(deviceCode, Nothing), deviceCode, "").Trim()
            Dim result As TokenPollResult
            If text3.Length = 0 Then
                LastError = "Device code required."
                result = TokenPollResult.Failed
            Else
                Dim url = "https://login.microsoftonline.com/" & text & "/oauth2/v2.0/token"
                Dim formBody = "grant_type=urn:ietf:params:oauth:grant-type:device_code&client_id=" & Uri.EscapeDataString(text2) & "&device_code=" & Uri.EscapeDataString(text3)
                Dim text4 = PostForm(url, formBody)
                If text4.Length = 0 Then
                    result = TokenPollResult.Failed
                Else
                    Try
                        Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                        Dim dictionary = javaScriptSerializer.Deserialize(Of Dictionary(Of String, Object))(text4)
                        If dictionary Is Nothing Then
                            LastError = "Invalid token response."
                            result = TokenPollResult.Failed
                        ElseIf dictionary.ContainsKey("access_token") Then
                            accessToken = Stringify(RuntimeHelpers.GetObjectValue(dictionary("access_token")))
                            refreshToken = Stringify(RuntimeHelpers.GetObjectValue(dictionary("refresh_token")))
                            result = TokenPollResult.Success
                        Else
                            Dim text5 = Stringify(RuntimeHelpers.GetObjectValue(dictionary("error")))
                            If text5.Equals("authorization_pending", StringComparison.OrdinalIgnoreCase) OrElse text5.Equals("slow_down", StringComparison.OrdinalIgnoreCase) Then
                                result = TokenPollResult.Pending
                            Else
                                LastError = Stringify(RuntimeHelpers.GetObjectValue(dictionary("error_description")))
                                If LastError.Length = 0 Then
                                    LastError = text5
                                End If
                                result = TokenPollResult.Failed
                            End If
                        End If
                    Catch ex As Exception
                        ProjectData.SetProjectError(ex)
                        Dim ex2 = ex
                        LastError = ex2.Message
                        result = TokenPollResult.Failed
                        Call ProjectData.ClearProjectError()
                    End Try
                End If
            End If
            Return result
        End Function

        Private Shared Function NormalizeTenant(tenant As String) As String
            Dim text As String = If(Not Equals(tenant, Nothing), tenant, "").Trim()
            If text.Length = 0 Then
                Return "organizations"
            End If
            Return text
        End Function

        Private Shared Function PostForm(url As String, formBody As String) As String
            Dim result As String
            Try
                Dim httpWebRequest = CType(WebRequest.Create(url), HttpWebRequest)
                httpWebRequest.Method = "POST"
                httpWebRequest.ContentType = "application/x-www-form-urlencoded"
                httpWebRequest.Timeout = 45000
                Dim bytes = Encoding.UTF8.GetBytes(formBody)
                httpWebRequest.ContentLength = bytes.Length
                Using stream As Stream = httpWebRequest.GetRequestStream()
                    stream.Write(bytes, 0, bytes.Length)
                End Using
                Dim httpWebResponse As HttpWebResponse = CType(httpWebRequest.GetResponse(), HttpWebResponse)
                Dim streamReader As StreamReader = New StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8)
                result = streamReader.ReadToEnd()
            Catch ex As WebException
                ProjectData.SetProjectError(ex)
                Dim ex2 = ex
                LastError = ReadWebExceptionBody(ex2)
                If LastError.Length = 0 Then
                    LastError = ex2.Message
                End If
                result = ""
                Call ProjectData.ClearProjectError()
            Catch ex3 As Exception
                ProjectData.SetProjectError(ex3)
                Dim ex4 = ex3
                LastError = ex4.Message
                result = ""
                Call ProjectData.ClearProjectError()
            End Try
            Return result
        End Function

        Private Shared Function ReadWebExceptionBody(ex As WebException) As String
            Dim result As String
            If ex Is Nothing OrElse ex.Response Is Nothing Then
                result = ""
            Else
                Try
                    Dim streamReader As StreamReader = New StreamReader(ex.Response.GetResponseStream(), Encoding.UTF8)
                    result = streamReader.ReadToEnd()
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    result = ""
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Private Shared Function Stringify(value As Object) As String
            If value Is Nothing Then
                Return ""
            End If
            Return value.ToString()
        End Function
    End Class

