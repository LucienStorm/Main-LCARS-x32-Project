' LCARSmain/LCARSmain/Modules/Hearth/modHearthEnrollStore.vb
Option Strict On
Option Explicit On
Imports System.IO
Imports System.Text
''' <summary>Persists Hearth per-host credentials under AppData (never log bearer_token).</summary> Module modHearthEnrollStore
Public Const ActionCatalogVersion As Integer = 1
Public Const DefaultEnrollPort As Integer = 18765
Public Class HearthCredentials
Public HubBaseUrl As String = ""
Public BearerToken As String = ""
Public ClientId As String = ""
Public SessionId As String = ""
Public Personality As String = "ASSISTANT"
Public Channel As String = "lcars"
End Class
Private Function StoreDir() As String
Dim root As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LCARS x32", "Hearth")
If Not Directory.Exists(root) Then Directory.CreateDirectory(root)
Return root
End Function
Private Function CredPath() As String
Return Path.Combine(StoreDir(), "enroll.json")
End Function
Public Function IsEnrolled() As Boolean
Dim c As HearthCredentials = Load()
Return c IsNot Nothing AndAlso Not String.IsNullOrEmpty(c.BearerToken) AndAlso Not String.IsNullOrEmpty(c.ClientId)
End Function
Public Function Load() As HearthCredentials
Try
Dim path As String = CredPath()
If Not File.Exists(path) Then Return Nothing
Dim json As String = File.ReadAllText(path, Encoding.UTF8)
Dim c As New HearthCredentials()
c.HubBaseUrl = If(modHearthJson.GetString(json, "hub_base_url"), "")
c.BearerToken = If(modHearthJson.GetString(json, "bearer_token"), "")
c.ClientId = If(modHearthJson.GetString(json, "client_id"), "")
c.SessionId = If(modHearthJson.GetString(json, "session_id"), "")
c.Personality = If(modHearthJson.GetString(json, "personality"), "ASSISTANT")
c.Channel = If(modHearthJson.GetString(json, "channel"), "lcars")
If String.IsNullOrEmpty(c.BearerToken) OrElse String.IsNullOrEmpty(c.ClientId) Then Return Nothing
Return c
Catch
Return Nothing
End Try
End Function
Public Sub Save(ByVal c As HearthCredentials)
If c Is Nothing Then Throw New ArgumentNullException("c")
Dim json As String = "{" & """hub_base_url"":" & modHearthJson.Quote(If(c.HubBaseUrl, "")) & "," & """bearer_token"":" & modHearthJson.Quote(If(c.BearerToken, "")) & "," & """client_id"":" & modHearthJson.Quote(If(c.ClientId, "")) & "," & """session_id"":" & modHearthJson.Quote(If(c.SessionId, "")) & "," & """personality"":" & modHearthJson.Quote(If(c.Personality, "ASSISTANT")) & "," & """channel"":" & modHearthJson.Quote(If(c.Channel, "lcars")) & "}"
Dim path As String = CredPath()
File.WriteAllText(path, json, Encoding.UTF8)
Try
Dim fi As New FileInfo(path)
fi.Attributes = fi.Attributes Or FileAttributes.Hidden
Catch
End Try modDiagnostics.LogInfo("HearthEnrollStore.Save", "enrolled client_id=" & c.ClientId & " session=" & c.SessionId)
Try modHearthVoiceClient.ResetReauthPrompt()
Catch
End Try
End Sub
Public Sub Clear()
Try
Dim path As String = CredPath()
If File.Exists(path) Then File.Delete(path)
modDiagnostics.LogInfo("HearthEnrollStore.Clear", "credentials cleared")
Catch ex As Exception modDiagnostics.LogException("HearthEnrollStore.Clear", ex, "")
End Try
End Sub
Public Function ProductVersion() As String
Try
Return My.Application.Info.Version.ToString()
Catch
Return "0.7.2.0"
End Try
End Function
End Module
