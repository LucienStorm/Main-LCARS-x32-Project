' LCARSmain/LCARSmain/Modules/Hearth/modHearthEnrollAgent.vb
Option Strict On
Option Explicit On
Imports System.IO
Imports System.Net
Imports System.Text
Imports System.Threading
''' <summary>
''' Loopback-only enroll API
for Hearth settings (browser on this PC).
''' Default port 18765 — matches Hearth hub.lcars.enroll_port.
''' </summary> Module modHearthEnrollAgent
Private listener As HttpListener
Private listenThread As Thread
Private _port As Integer = modHearthEnrollStore.DefaultEnrollPort
Private _running As Boolean = False
Public ReadOnly Property IsRunning As Boolean Get
Return _running
End Get
End Property
Public ReadOnly Property Port As Integer Get
Return _port
End Get
End Property
Public Sub StartAgent(Optional ByVal port As Integer = 0)
If _running Then Return
If port > 0 Then _port = port
Try listener = New HttpListener()
listener.Prefixes.Add("http://127.0.0.1:" & _port.ToString() & "/")
listener.Start() _running = True listenThread = New Thread(AddressOf ListenLoop)
listenThread.IsBackground = True listenThread.Name = "HearthEnrollAgent" listenThread.Start()
modDiagnostics.LogInfo("HearthEnrollAgent.Start", "listening on 127.0.0.1:" & _port.ToString())
Catch ex As Exception _running = False modDiagnostics.LogException("HearthEnrollAgent.Start", ex, "port=" & _port.ToString())
End Try
End Sub
Public Sub StopAgent() _running = False
Try
If listener IsNot Nothing Then listener.Stop()
listener.Close()
End If
Catch
End Try listener = Nothing modDiagnostics.LogInfo("HearthEnrollAgent.Stop", "stopped")
End Sub
Private Sub ListenLoop()
While _running AndAlso listener IsNot Nothing AndAlso listener.IsListening
Try
Dim ctx As HttpListenerContext = listener.GetContext()
ThreadPool.QueueUserWorkItem(Sub(state) HandleRequest(CType(state, HttpListenerContext)), ctx)
Catch ex As HttpListenerException
If Not _running Then Exit
While
Catch ex As Exception
If Not _running Then Exit
While modDiagnostics.LogException("HearthEnrollAgent.ListenLoop", ex, "")
End Try
End While
End Sub
Private Sub HandleRequest(ByVal ctx As HttpListenerContext)
Try
Dim req As HttpListenerRequest = ctx.Request
Dim res As HttpListenerResponse = ctx.Response ApplyCors(req, res)
If req.HttpMethod = "OPTIONS" Then res.StatusCode = 204 res.Close()
Return
End If
Dim path As String = req.Url.AbsolutePath.TrimEnd("/"c).ToLowerInvariant()
If path.EndsWith("/v1/enroll/status") AndAlso req.HttpMethod = "GET" Then WriteJson(res, 200, BuildStatusJson())
Return
End If
If path.EndsWith("/v1/enroll/accept") AndAlso req.HttpMethod = "POST" Then Dim body As String = ReadBody(req)
Dim err As String = Nothing
If Not ApplyAccept(body, err) Then WriteJson(res, 400, "{""ok"":false,""error"":" & modHearthJson.Quote(If(err, "accept failed")) & "}")
Return
End If WriteJson(res, 200, "{""ok"":true}")
Return
End If
If path.EndsWith("/v1/enroll/clear") AndAlso req.HttpMethod = "POST" Then modHearthEnrollStore.Clear() WriteJson(res, 200, "{""ok"":true}")
Return
End If WriteJson(res, 404, "{""ok"":false,""error"":""not found""}")
Catch ex As Exception modDiagnostics.LogException("HearthEnrollAgent.HandleRequest", ex, "")
Try WriteJson(ctx.Response, 500, "{""ok"":false,""error"":""internal""}")
Catch
End Try
End Try
End Sub
Private Sub ApplyCors(ByVal req As HttpListenerRequest, ByVal res As HttpListenerResponse)
Dim origin As String = req.Headers("Origin")
If String.IsNullOrEmpty(origin) Then origin = "*" res.Headers("Access-Control-Allow-Origin") = origin res.Headers("Access-Control-Allow-Methods") = "GET, POST, OPTIONS" res.Headers("Access-Control-Allow-Headers") = "Content-Type, Authorization" res.Headers("Access-Control-Max-Age") = "600"
If origin <> "*" Then res.Headers("Vary") = "Origin"
End If
End Sub
Private Function BuildStatusJson() As String
Dim creds As modHearthEnrollStore.HearthCredentials = modHearthEnrollStore.Load()
Dim enrolled As Boolean = creds IsNot Nothing
Dim clientId As String = "null"
Dim sessionId As String = "null"
If enrolled Then clientId = modHearthJson.Quote(creds.ClientId) sessionId = modHearthJson.Quote(creds.SessionId)
End If
Return "{" & """ok"":true," & """product"":""lcarsx32""," & """hostname"":" & modHearthJson.Quote(Environment.MachineName) & "," & """version"":" & modHearthJson.Quote(modHearthEnrollStore.ProductVersion()) & "," & """enrolled"":" & If(enrolled, "true", "false") & "," & """client_id"":" & clientId & "," & """session_id"":" & sessionId & "," & """action_catalog_version"":" & modHearthEnrollStore.ActionCatalogVersion.ToString() & "}"
End Function
Private Function ApplyAccept(ByVal body As String, ByRef err As String) As Boolean err = Nothing
If String.IsNullOrEmpty(body) Then err = "empty body"
Return False
End If
Dim hub As String = modHearthJson.GetString(body, "hub_base_url")
Dim token As String = modHearthJson.GetString(body, "bearer_token")
Dim clientId As String = modHearthJson.GetString(body, "client_id")
Dim sessionId As String = modHearthJson.GetString(body, "session_id")
If String.IsNullOrEmpty(hub) OrElse String.IsNullOrEmpty(token) OrElse String.IsNullOrEmpty(clientId) Then err = "hub_base_url, bearer_token, and client_id required"
Return False
End If
Dim existing As modHearthEnrollStore.HearthCredentials = modHearthEnrollStore.Load()
Dim replace As Boolean = modHearthJson.GetBool(body, "replace", False)
If existing IsNot Nothing AndAlso Not String.Equals(existing.ClientId, clientId, StringComparison.OrdinalIgnoreCase) AndAlso Not replace Then err = "already enrolled with different client_id; pass replace:true"
Return False
End If
Dim c As New modHearthEnrollStore.HearthCredentials()
c.HubBaseUrl = hub.TrimEnd("/"c)
c.BearerToken = token c.ClientId = clientId c.SessionId = If(sessionId, "")
c.Personality = If(modHearthJson.GetString(body, "personality"), "ASSISTANT")
c.Channel = If(modHearthJson.GetString(body, "channel"), "lcars")
modHearthEnrollStore.Save(c)
Return True
End Function
Private Function ReadBody(ByVal req As HttpListenerRequest) As String
Using sr As New StreamReader(req.InputStream, If(req.ContentEncoding, Encoding.UTF8))
Return sr.ReadToEnd()
End Using
End Function
Private Sub WriteJson(ByVal res As HttpListenerResponse, ByVal status As Integer, ByVal json As String)
Dim bytes() As Byte = Encoding.UTF8.GetBytes(If(json, "{}"))
res.StatusCode = status res.ContentType = "application/json; charset=utf-8" res.ContentLength64 = bytes.Length res.OutputStream.Write(bytes, 0, bytes.Length)
res.OutputStream.Close()
End Sub
End Module
