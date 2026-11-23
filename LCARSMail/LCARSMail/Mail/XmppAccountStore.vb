Imports System
Imports System.Diagnostics
Imports System.IO
Imports System.Text
Imports System.Web.Script.Serialization
Imports Microsoft.VisualBasic.CompilerServices
    Public NotInheritable Class XmppAccountStore
        Public Class XmppSettings
            Public Host As String

            Public Port As Integer

            Public Jid As String

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Private Const SettingsFileName As String = "xmpp-settings.json"

        Private Const PasswordSecretKey As String = "xmpp"

        Private Sub New()
        End Sub

        Public Shared Sub SaveAccount(host As String, port As Integer, jid As String, password As String)
            Try
                Dim xmppSettings As XmppSettings = New XmppSettings()
                xmppSettings.Host = If(Not Equals(host, Nothing), host, "").Trim()
                xmppSettings.Port = port
                xmppSettings.Jid = If(Not Equals(jid, Nothing), jid, "").Trim()
                Dim obj = xmppSettings
                Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                Call File.WriteAllText(SettingsPath(), javaScriptSerializer.Serialize(obj), Encoding.UTF8)
                If Not Equals(password, Nothing) Then
                    AccountStore.SavePassword("xmpp", password)
                End If
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
        End Sub

        Public Shared Function TryLoadAccount(ByRef host As String, ByRef port As Integer, ByRef jid As String, ByRef password As String) As Boolean
            host = Nothing
            jid = Nothing
            password = Nothing
            port = 5222
            Dim result As Boolean
            If Not File.Exists(SettingsPath()) Then
                result = False
            Else
                Try
                    Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                    Dim xmppSettings As XmppSettings = javaScriptSerializer.Deserialize(Of XmppSettings)(File.ReadAllText(SettingsPath(), Encoding.UTF8))
                    If xmppSettings Is Nothing OrElse String.IsNullOrWhiteSpace(xmppSettings.Jid) Then
                        result = False
                    Else
                        Dim xmppSettings2 = xmppSettings
                        host = If(Not Equals(xmppSettings2.Host, Nothing), xmppSettings2.Host, "").Trim()
                        jid = xmppSettings.Jid.Trim()
                        If xmppSettings.Port > 0 Then
                            port = xmppSettings.Port
                        End If
                        result = AccountStore.TryGetPassword("xmpp", password)
                    End If
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    result = False
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Private Shared Function SettingsPath() As String
            Return Path.Combine(AccountStore.DataRoot(), "xmpp-settings.json")
        End Function
    End Class

