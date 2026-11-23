' LCARSmain/LCARSmain/Modules/Hearth/modHearthVoiceClient.vb
Option Strict On
Option Explicit On
Imports System.IO
Imports System.Media
Imports System.Net
Imports System.Text
Imports System.Threading
Imports System.Windows.Forms
''' <summary>Hub text-turn + network TTS WAV playback (no Windows SAPI
for hub replies).</summary> Module modHearthVoiceClient
Private _busy As Integer = 0
Private _reauthPrompted As Boolean = False
''' <summary>Call after a successful re-enroll so a later 401 can prompt again.</summary>
Public Sub ResetReauthPrompt() _reauthPrompted = False
End Sub
Public Sub HandleUtteranceAsync(ByVal text As String)
If String.IsNullOrEmpty(text) Then Return
If Interlocked.CompareExchange(_busy, 1, 0) <> 0 Then modDiagnostics.LogWarn("HearthVoiceClient", "busy — dropping utterance")
Return
End If ThreadPool.QueueUserWorkItem(Sub()
Try HandleUtterance(text)
Finally Interlocked.Exchange(_busy, 0)
End Try
End Sub)
End Sub
Public Sub HandleUtterance(ByVal text As String)
Dim creds As modHearthEnrollStore.HearthCredentials = modHearthEnrollStore.Load()
If creds Is Nothing Then ShowUiMessage("Not enrolled with Hearth. Open Hearth settings on this PC and Enroll.")
Return
End If
Dim reply As String = Nothing
Dim actionJson As String = Nothing
Dim err As String = Nothing
Dim status As Integer = PostTextTurn(creds, text, reply, actionJson, err)
If status = 401 Then modHearthEnrollStore.Clear()
If Not _reauthPrompted Then _reauthPrompted = True ShowUiMessage("This PC was unenrolled — open Hearth settings and Enroll again.")
End If
Return
End If
If status < 200 OrElse status >= 300 Then modDiagnostics.LogWarn("HearthVoiceClient.TextTurn", "HTTP " & status.ToString() & " " & If(err, ""))
If Not String.IsNullOrEmpty(reply) Then ' still
try to speak
partial reply via TTS
if present
Else ShowUiMessage("Hearth voice turn failed (" & status.ToString() & ").")
Return
End If
End If
If Not String.IsNullOrEmpty(reply) Then PlayHubSpeech(creds, reply)
End If
If Not String.IsNullOrEmpty(actionJson) Then Dim name As String = modHearthJson.GetString(actionJson, "name")
Dim confirm As Boolean = modHearthJson.GetBool(actionJson, "confirm", False)
Dim argsObj As String = modHearthJson.ExtractObject(actionJson, "args")
If String.IsNullOrEmpty(argsObj) Then argsObj = "{}" modHearthActions.HandleAction(name, argsObj, confirm)
End If
End Sub
Private Function PostTextTurn(ByVal creds As modHearthEnrollStore.HearthCredentials, _ ByVal text As String, _ ByRef reply As String, _ ByRef actionJson As String, _ ByRef err As String) As Integer reply = Nothing actionJson = Nothing err = Nothing
Dim url As String = creds.HubBaseUrl.TrimEnd("/"c) & "/v1/voice/lcars/text-turn"
Dim body As String = "{" & """text"":" & modHearthJson.Quote(text) & "," & """client_id"":" & modHearthJson.Quote(creds.ClientId) & "," & """session_id"":" & modHearthJson.Quote(creds.SessionId) & "," & """personality"":" & modHearthJson.Quote(creds.Personality) & "}"
Try
Dim req As HttpWebRequest = CType(WebRequest.Create(url), HttpWebRequest)
req.Method = "POST" req.ContentType = "application/json" req.Timeout = 120000 req.Headers("Authorization") = "Bearer " & creds.BearerToken req.Headers("X-Device-Id") = creds.ClientId
If Not String.IsNullOrEmpty(creds.SessionId) Then req.Headers("X-Session-ID") = creds.SessionId
End If
Dim bytes() As Byte = Encoding.UTF8.GetBytes(body)
req.ContentLength = bytes.Length
Using s As Stream = req.GetRequestStream()
s.Write(bytes, 0, bytes.Length)
End Using
Using resp As HttpWebResponse = CType(req.GetResponse(), HttpWebResponse)
Dim json As String
Using sr As New StreamReader(resp.GetResponseStream(), Encoding.UTF8) json = sr.ReadToEnd()
End Using reply = modHearthJson.GetString(json, "reply") actionJson = modHearthJson.ExtractObject(json, "action")
Return CInt(resp.StatusCode)
End Using
Catch ex As WebException
Dim http As HttpWebResponse = TryCast(ex.Response, HttpWebResponse)
If http IsNot Nothing Then Try
Using sr As New StreamReader(http.GetResponseStream(), Encoding.UTF8) err = sr.ReadToEnd()
End Using reply = modHearthJson.GetString(err, "reply")
Catch
End Try
Return CInt(http.StatusCode)
End If err = ex.Message
Return 0
Catch ex As Exception err = ex.Message
Return 0
End Try
End Function
Private Sub PlayHubSpeech(ByVal creds As modHearthEnrollStore.HearthCredentials, ByVal reply As String)
Dim tmp As String = Nothing
Try
Dim url As String = creds.HubBaseUrl.TrimEnd("/"c) & "/v1/audio/speech"
Dim body As String = "{" & """input"":" & modHearthJson.Quote(reply) & "," & """format"":""wav""," & """personality"":" & modHearthJson.Quote(creds.Personality) & "}"
Dim req As HttpWebRequest = CType(WebRequest.Create(url), HttpWebRequest)
req.Method = "POST" req.ContentType = "application/json" req.Timeout = 60000 req.Headers("Authorization") = "Bearer " & creds.BearerToken req.Headers("X-Personality") = creds.Personality
Dim bytes() As Byte = Encoding.UTF8.GetBytes(body)
req.ContentLength = bytes.Length
Using s As Stream = req.GetRequestStream()
s.Write(bytes, 0, bytes.Length)
End Using
Using resp As HttpWebResponse = CType(req.GetResponse(), HttpWebResponse) tmp = Path.Combine(Path.GetTempPath(), "lcars-hearth-" & Guid.NewGuid().ToString("N") & ".wav")
Using fs As New FileStream(tmp, FileMode.Create, FileAccess.Write)
Dim buf(8191) As Byte
Dim rs As Stream = resp.GetResponseStream()
Dim n As Integer
Do n = rs.Read(buf, 0, buf.Length)
If n > 0 Then fs.Write(buf, 0, n)
Loop
While n > 0
End Using
End Using ' Play on UI thread-ish; SoundPlayer is fine from background
for sync play
Using player As New SoundPlayer(tmp)
player.PlaySync()
End Using
Catch ex As Exception modDiagnostics.LogException("HearthVoiceClient.PlayHubSpeech", ex, "TTS failed — showing text only") ShowUiMessage(reply)
Finally
Try
If tmp IsNot Nothing AndAlso File.Exists(tmp) Then File.Delete(tmp)
Catch
End Try
End Try
End Sub
Private Sub ShowUiMessage(ByVal msg As String)
Try
If myDesktop IsNot Nothing AndAlso myDesktop.InvokeRequired Then myDesktop.BeginInvoke(New MethodInvoker(Sub() ShowUiMessage(msg)))
Return
End If LCARS.UI.MsgBox(msg, MsgBoxStyle.OkOnly, "HEARTH")
Catch
End Try
End Sub
End Module
