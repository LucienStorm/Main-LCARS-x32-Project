Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Globalization
Imports System.IO
Imports System.Net
Imports System.Runtime.CompilerServices
Imports System.Security.Cryptography
Imports System.Text
Imports System.Threading
Imports System.Web.Script.Serialization
Imports System.Windows.Forms
Imports LCARS
Imports LCARS.Controls
Imports Microsoft.VisualBasic.CompilerServices
    Public NotInheritable Class MailOAuthBrowserFlow
        Private Class frmOAuthWait
            Inherits LCARSForm
            Private cancelled As Boolean

            Public Sub New(message As String)
                CType(CObj(Me), Form).Text = "LCARS COMMS"
                CType(CObj(Me), Form).BackColor = Color.Black
                CType(CObj(Me), Control).ForeColor = Color.Orange
                CType(CObj(Me), Form).FormBorderStyle = FormBorderStyle.None
                CType(CObj(Me), Form).ControlBox = False
                Me.DoubleBuffered = True
                CType(Me, LCARSForm).BindToWorkingArea = False
                CType(CObj(Me), Form).StartPosition = FormStartPosition.CenterParent
                Dim clientSize As Size = New Size(280, 96)
                CType(CObj(Me), Form).ClientSize = clientSize
                CType(CObj(Me), Form).ShowInTaskbar = False
                Dim val As FlatButton = New FlatButton()
                CType(val, LCARSbuttonClass).ButtonText = "SIGNING IN"
                CType(val, LCARSbuttonClass).Text = "SIGNING IN"
                CType(val, LCARSbuttonClass).Clickable = False
                CType(val, LCARSbuttonClass).Color = CType(8, LCARScolorStyles)
                CType(val, LCARSbuttonClass).ButtonTextAlign = ContentAlignment.BottomRight
                clientSize = New Size(256, 28)
                CType(CObj(val), Control).Size = clientSize
                Dim location As Point = New Point(12, 8)
                CType(CObj(val), Control).Location = location
                CType(CObj(Me), Control).Controls.Add(CType(CObj(val), Control))
                Dim val2 As FlatButton = New FlatButton()
                CType(val2, LCARSbuttonClass).ButtonText = "CANCEL"
                CType(val2, LCARSbuttonClass).Text = "CANCEL"
                CType(val2, LCARSbuttonClass).Color = CType(3, LCARScolorStyles)
                CType(val2, LCARSbuttonClass).ButtonTextAlign = ContentAlignment.MiddleCenter
                clientSize = New Size(100, 32)
                CType(CObj(val2), Control).Size = clientSize
                location = New Point(168, 52)
                CType(CObj(val2), Control).Location = location
                AddHandler CType(CObj(val2), Control).Click, Sub(a0, a1)
                                                                 Call CType((Sub()
                                                                                 cancelled = True
                                                                                 CType(CObj(Me), Form).DialogResult = DialogResult.Cancel
                                                                                 Call CType(CObj(Me), Form).Close()
                                                                             End Sub), VB_0024AnonymousDelegate_1)()
                                                             End Sub
                CType(CObj(Me), Control).Controls.Add(CType(CObj(val2), Control))
            End Sub

            Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
                If Not cancelled AndAlso CType(CObj(Me), Form).DialogResult <> DialogResult.Cancel Then
                    CType(CObj(Me), Form).DialogResult = DialogResult.OK
                End If
                CType(Me, Form).OnFormClosing(e)
            End Sub
        End Class

        Public Shared LastError As String

        Private Sub New()
        End Sub

        Public Shared Function TryAuthorize(owner As IWin32Window, account As MailAccount, ByRef accessToken As String) As Boolean
            LastError = Nothing
            accessToken = Nothing
            Dim result As Boolean
            If account Is Nothing Then
                LastError = "Account is required."
                result = False
            Else
                Dim providerInfo = MailOAuthProviders.TryResolveProvider(account)
                If providerInfo Is Nothing Then
                    LastError = "No OAuth provider for this mail host."
                    result = False
                Else
                    Dim port = 0
                    Dim httpListener As HttpListener = Nothing
                    Try
                        httpListener = StartLoopbackListener(port)
                    Catch ex As Exception
                        ProjectData.SetProjectError(ex)
                        Dim ex2 = ex
                        LastError = "Could not start local login listener: " & ex2.Message
                        result = False
                        Call ProjectData.ClearProjectError()
                        GoTo IL_029d
                    End Try
                    Dim redirectUri As String = "http://127.0.0.1:" & port.ToString() & "/"
                    Dim text = NewRandomToken(32)
                    Dim text2 As String = NewCodeVerifier()
                    Dim codeChallenge = ToCodeChallenge(text2)
                    Dim loginHint = If(Not Equals(account.EmailAddress, Nothing), account.EmailAddress, account.Username)
                    Dim url = BuildAuthorizeUrl(providerInfo, redirectUri, text, codeChallenge, loginHint)
                    Dim frmOAuthWait As frmOAuthWait = New frmOAuthWait("")
                    Dim text3 As String = Nothing
                    Dim text4 As String = Nothing
                    Dim flag = False
                    Dim thread As Thread = New Thread(Sub()
                                                          Try
                                                              Dim context As HttpListenerContext = httpListener.GetContext()
                                                              Dim queryString = context.Request.QueryString
                                                              Dim text5 = queryString("error_description")
                                                              Dim text6 = If(Not Equals(text5, Nothing), text5, queryString("error"))
                                                              Dim a = queryString("state")
                                                              Dim text7 = queryString("code")
                                                              If Not String.IsNullOrEmpty(text6) Then
                                                                  text4 = text6
                                                              ElseIf Not String.Equals(a, text, StringComparison.Ordinal) Then
                                                                  text4 = "OAuth state mismatch."
                                                              ElseIf String.IsNullOrEmpty(text7) Then
                                                                  text4 = "Authorization code missing."
                                                              Else
                                                                  text3 = text7
                                                              End If
                                                              WriteCallbackPage(context.Response, text4)
                                                          Catch ex3 As HttpListenerException
                                                              ProjectData.SetProjectError(ex3)
                                                              Dim ex4 = ex3
                                                              If Equals(text4, Nothing) AndAlso Equals(text3, Nothing) Then
                                                                  text4 = "Login listener stopped."
                                                              End If
                                                              Call ProjectData.ClearProjectError()
                                                          Catch ex5 As Exception
                                                              ProjectData.SetProjectError(ex5)
                                                              Dim ex6 = ex5
                                                              text4 = ex6.Message
                                                              Call ProjectData.ClearProjectError()
                                                          Finally
                                                              flag = True
                                                              Try
                                                                  Call CType(CObj(frmOAuthWait), Control).BeginInvoke(CType(CType((Sub() Call CType(CObj(frmOAuthWait), Form).Close()), MethodInvoker), [Delegate]))
                                                              Catch projectError5 As Exception
                                                                  ProjectData.SetProjectError(projectError5)
                                                                  Call ProjectData.ClearProjectError()
                                                              End Try
                                                          End Try
                                                      End Sub)
                    thread.IsBackground = True
                    thread.Start()
                    If Not LcarsCommsUi.OpenInLcarsBrowser(url) Then
                        Try
                            httpListener.Stop()
                        Catch projectError As Exception
                            ProjectData.SetProjectError(projectError)
                            Call ProjectData.ClearProjectError()
                        End Try
                        LastError = "Could not open LCARS Web Browser for sign-in."
                        result = False
                    Else
                        Dim dialogResult As DialogResult = (If(owner Is Nothing, CType(CObj(frmOAuthWait), Form).ShowDialog(), CType(CObj(frmOAuthWait), Form).ShowDialog(owner)))
                        If dialogResult = DialogResult.Cancel AndAlso Not flag Then
                            Try
                                httpListener.Stop()
                            Catch projectError2 As Exception
                                ProjectData.SetProjectError(projectError2)
                                Call ProjectData.ClearProjectError()
                            End Try
                            LastError = "Sign-in cancelled."
                            result = False
                        Else
                            Try
                                httpListener.Stop()
                            Catch projectError3 As Exception
                                ProjectData.SetProjectError(projectError3)
                                Call ProjectData.ClearProjectError()
                            End Try
                            Try
                                httpListener.Close()
                            Catch projectError4 As Exception
                                ProjectData.SetProjectError(projectError4)
                                Call ProjectData.ClearProjectError()
                            End Try
                            If Not String.IsNullOrEmpty(text4) Then
                                LastError = text4
                                result = False
                            ElseIf String.IsNullOrEmpty(text3) Then
                                LastError = "No authorization code received."
                                result = False
                            Else
                                Dim refreshToken As String = Nothing
                                Dim expiresInSeconds = 3600
                                If Not ExchangeCode(providerInfo, text3, redirectUri, text2, accessToken, refreshToken, expiresInSeconds) Then
                                    result = False
                                Else
                                    MailOAuthStore.SaveTokens(account.Id, accessToken, refreshToken, expiresInSeconds)
                                    account.AuthMethod = "oauth2"
                                    result = Not String.IsNullOrWhiteSpace(accessToken)
                                End If
                            End If
                        End If
                    End If
                End If
            End If
            GoTo IL_029d
IL_029d:
            Return result
        End Function

        Public Shared Function TryRefresh(provider As MailOAuthProviders.ProviderInfo, refreshToken As String, ByRef accessToken As String, ByRef newRefreshToken As String, ByRef expiresInSeconds As Integer) As Boolean
            LastError = Nothing
            accessToken = Nothing
            newRefreshToken = Nothing
            expiresInSeconds = 3600
            If provider Is Nothing OrElse String.IsNullOrWhiteSpace(refreshToken) Then
                LastError = "Refresh token required."
                Return False
            End If
            Dim stringBuilder As StringBuilder = New StringBuilder()
            stringBuilder.Append("grant_type=refresh_token")
            stringBuilder.Append("&client_id=").Append(Uri.EscapeDataString(provider.ClientId))
            stringBuilder.Append("&refresh_token=").Append(Uri.EscapeDataString(refreshToken))
            If Not String.IsNullOrEmpty(provider.ClientSecret) Then
                stringBuilder.Append("&client_secret=").Append(Uri.EscapeDataString(provider.ClientSecret))
            End If
            If Not String.IsNullOrEmpty(provider.Scope) Then
                stringBuilder.Append("&scope=").Append(Uri.EscapeDataString(provider.Scope))
            End If
            Dim text As String = PostForm(provider.TokenEndpoint, stringBuilder.ToString())
            If text.Length = 0 Then
                Return False
            End If
            Return ParseTokenResponse(text, accessToken, newRefreshToken, expiresInSeconds)
        End Function

        Private Shared Function StartLoopbackListener(ByRef port As Integer) As HttpListener
            Dim ex As Exception = Nothing
            Dim num = 0
            Do
                Dim num2 = 17890 + num
                Dim httpListener As HttpListener = New HttpListener()
                Try
                    httpListener.Prefixes.Add("http://127.0.0.1:" & num2.ToString() & "/")
                    httpListener.Start()
                    port = num2
                    Return httpListener
                Catch ex2 As Exception
                    ProjectData.SetProjectError(ex2)
                    Dim ex3 = ex2
                    ex = ex3
                    Try
                        httpListener.Close()
                    Catch projectError As Exception
                        ProjectData.SetProjectError(projectError)
                        Call ProjectData.ClearProjectError()
                    End Try
                    Call ProjectData.ClearProjectError()
                End Try
                num += 1
            Loop While num <= 19
            If ex IsNot Nothing Then
                Throw ex
            End If
            Throw New InvalidOperationException("No free loopback port for OAuth.")
        End Function

        Private Shared Function BuildAuthorizeUrl(provider As MailOAuthProviders.ProviderInfo, redirectUri As String, state As String, codeChallenge As String, loginHint As String) As String
            Dim stringBuilder As StringBuilder = New StringBuilder()
            stringBuilder.Append(provider.AuthorizationEndpoint)
            stringBuilder.Append(If(provider.AuthorizationEndpoint.Contains("?"), "&", "?"))
            stringBuilder.Append("client_id=").Append(Uri.EscapeDataString(provider.ClientId))
            stringBuilder.Append("&response_type=code")
            stringBuilder.Append("&redirect_uri=").Append(Uri.EscapeDataString(redirectUri))
            stringBuilder.Append("&scope=").Append(Uri.EscapeDataString(provider.Scope))
            stringBuilder.Append("&state=").Append(Uri.EscapeDataString(state))
            stringBuilder.Append("&access_type=offline")
            stringBuilder.Append("&prompt=consent")
            If provider.UsePkce Then
                stringBuilder.Append("&code_challenge=").Append(Uri.EscapeDataString(codeChallenge))
                stringBuilder.Append("&code_challenge_method=S256")
            End If
            If Not String.IsNullOrWhiteSpace(loginHint) Then
                stringBuilder.Append("&login_hint=").Append(Uri.EscapeDataString(loginHint.Trim()))
            End If
            Return stringBuilder.ToString()
        End Function

        Private Shared Function ExchangeCode(provider As MailOAuthProviders.ProviderInfo, code As String, redirectUri As String, codeVerifier As String, ByRef accessToken As String, ByRef refreshToken As String, ByRef expiresInSeconds As Integer) As Boolean
            accessToken = Nothing
            refreshToken = Nothing
            expiresInSeconds = 3600
            Dim stringBuilder As StringBuilder = New StringBuilder()
            stringBuilder.Append("grant_type=authorization_code")
            stringBuilder.Append("&code=").Append(Uri.EscapeDataString(code))
            stringBuilder.Append("&redirect_uri=").Append(Uri.EscapeDataString(redirectUri))
            stringBuilder.Append("&client_id=").Append(Uri.EscapeDataString(provider.ClientId))
            If Not String.IsNullOrEmpty(provider.ClientSecret) Then
                stringBuilder.Append("&client_secret=").Append(Uri.EscapeDataString(provider.ClientSecret))
            End If
            If provider.UsePkce Then
                stringBuilder.Append("&code_verifier=").Append(Uri.EscapeDataString(codeVerifier))
            End If
            Dim text As String = PostForm(provider.TokenEndpoint, stringBuilder.ToString())
            If text.Length = 0 Then
                Return False
            End If
            Return ParseTokenResponse(text, accessToken, refreshToken, expiresInSeconds)
        End Function

        Private Shared Function ParseTokenResponse(json As String, ByRef accessToken As String, ByRef refreshToken As String, ByRef expiresInSeconds As Integer) As Boolean
            Dim result As Boolean
            Dim result2 As Integer = Nothing
            Try
                Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                Dim dictionary = javaScriptSerializer.Deserialize(Of Dictionary(Of String, Object))(json)
                If dictionary Is Nothing Then
                    LastError = "Invalid token response."
                    result = False
                ElseIf dictionary.ContainsKey("error") Then
                    LastError = Stringify(RuntimeHelpers.GetObjectValue(dictionary("error_description")))
                    If LastError.Length = 0 Then
                        LastError = Stringify(RuntimeHelpers.GetObjectValue(dictionary("error")))
                    End If
                    result = False
                Else
                    accessToken = Stringify(RuntimeHelpers.GetObjectValue(dictionary("access_token")))
                    refreshToken = Stringify(RuntimeHelpers.GetObjectValue(dictionary("refresh_token")))
                    expiresInSeconds = 3600

                    If dictionary.ContainsKey("expires_in") AndAlso Integer.TryParse(Stringify(RuntimeHelpers.GetObjectValue(dictionary("expires_in"))), result2) AndAlso result2 > 0 Then
                        expiresInSeconds = result2
                    End If
                    If accessToken.Length = 0 Then
                        LastError = "Access token missing."
                        result = False
                    Else
                        result = True
                    End If
                End If
            Catch ex As Exception
                ProjectData.SetProjectError(ex)
                Dim ex2 = ex
                LastError = ex2.Message
                result = False
                Call ProjectData.ClearProjectError()
            End Try
            Return result
        End Function

        Private Shared Function PostForm(url As String, formBody As String) As String
            Dim result As String
            Dim httpWebResponse2 As HttpWebResponse = Nothing
            Try
                Dim httpWebRequest = CType(WebRequest.Create(url), HttpWebRequest)
                httpWebRequest.Method = "POST"
                httpWebRequest.ContentType = "application/x-www-form-urlencoded"
                httpWebRequest.Timeout = 60000
                Dim bytes = Encoding.UTF8.GetBytes(If(Not Equals(formBody, Nothing), formBody, ""))
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
                Try

                    If CSharpImpl.__Assign(httpWebResponse2, TryCast(ex2.Response, HttpWebResponse)) IsNot Nothing Then
                        Dim streamReader2 As StreamReader = New StreamReader(httpWebResponse2.GetResponseStream(), Encoding.UTF8)
                        Dim text As String = streamReader2.ReadToEnd()
                        If text.Length <= 0 Then
                            GoTo end_IL_0103
                        End If
                        LastError = text
                        result = text
                        Call ProjectData.ClearProjectError()
                        GoTo end_IL_00d2
end_IL_0103:
                    End If
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    Call ProjectData.ClearProjectError()
                End Try
                LastError = ex2.Message
                result = ""
                Call ProjectData.ClearProjectError()
end_IL_00d2:
            Catch ex3 As Exception
                ProjectData.SetProjectError(ex3)
                Dim ex4 = ex3
                LastError = ex4.Message
                result = ""
                Call ProjectData.ClearProjectError()
            End Try
            Return result
        End Function

        Private Shared Sub WriteCallbackPage(response As HttpListenerResponse, errorText As String)
            Dim s = If(Not String.IsNullOrEmpty(errorText), "<!DOCTYPE html><html><body style='background:#000;color:#ff9900;font-family:Arial'><h2>LCARS COMMS</h2><p>Sign-in failed.</p><pre>" & HtmlEncode(errorText) & "</pre></body></html>", "<!DOCTYPE html><html><body style='background:#000;color:#ff9900;font-family:Arial'><h2>LCARS COMMS</h2><p>Sign-in complete. You may close this tab.</p></body></html>")
            Dim bytes = Encoding.UTF8.GetBytes(s)
            response.StatusCode = If(String.IsNullOrEmpty(errorText), 200, 400)
            response.ContentType = "text/html; charset=utf-8"
            response.ContentLength64 = bytes.Length
            response.OutputStream.Write(bytes, 0, bytes.Length)
            response.OutputStream.Close()
        End Sub

        Private Shared Function NewCodeVerifier() As String
            Return NewRandomToken(64)
        End Function

        Private Shared Function NewRandomToken(byteLength As Integer) As String
            Dim data = New Byte(byteLength - 1 + 1 - 1) {}
            Using rNGCryptoServiceProvider As RNGCryptoServiceProvider = New RNGCryptoServiceProvider()
                rNGCryptoServiceProvider.GetBytes(data)
            End Using
            Return Base64UrlEncode(data)
        End Function

        Private Shared Function ToCodeChallenge(verifier As String) As String
            Dim bytes = Encoding.ASCII.GetBytes(verifier)
            Dim sHA As SHA256 = SHA256.Create()
            Return Base64UrlEncode(sHA.ComputeHash(bytes))
        End Function

        Private Shared Function Base64UrlEncode(data As Byte()) As String
            Dim text = Convert.ToBase64String(data)
            Return text.TrimEnd("="c).Replace("+"c, "-"c).Replace("/"c, "_"c)
        End Function

        Private Shared Function Stringify(value As Object) As String
            If value Is Nothing Then
                Return ""
            End If
            Return Convert.ToString(RuntimeHelpers.GetObjectValue(value), CultureInfo.InvariantCulture)
        End Function

        Private Shared Function HtmlEncode(text As String) As String
            If Equals(text, Nothing) Then
                Return ""
            End If
            Return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("""", "&quot;")
        End Function

        Private Class CSharpImpl
            Shared Function __Assign(Of T)(ByRef target As T, value As T) As T
                target = value
                Return value
            End Function
        End Class
    End Class

