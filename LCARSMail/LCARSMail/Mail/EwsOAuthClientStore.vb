Imports System
Imports System.Diagnostics
Imports System.IO
Imports System.Text
Imports System.Web.Script.Serialization
Imports Microsoft.VisualBasic.CompilerServices
    Public NotInheritable Class EwsOAuthClientStore
        Public Class OAuthAppSettings
            Public Tenant As String

            Public ClientId As String

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Private Const SettingsFileName As String = "ews-oauth-app.json"

        Private Sub New()
        End Sub

        Public Shared Sub SaveSettings(tenant As String, clientId As String)
            Try
                Dim oAuthAppSettings As OAuthAppSettings = New OAuthAppSettings()
                oAuthAppSettings.Tenant = If(Not Equals(tenant, Nothing), tenant, "").Trim()
                oAuthAppSettings.ClientId = If(Not Equals(clientId, Nothing), clientId, "").Trim()
                Dim obj = oAuthAppSettings
                Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                Call File.WriteAllText(SettingsPath(), javaScriptSerializer.Serialize(obj), Encoding.UTF8)
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
        End Sub

        Public Shared Function TryLoadSettings(ByRef tenant As String, ByRef clientId As String) As Boolean
            tenant = "organizations"
            clientId = "94933e60-ee10-4250-a050-3efd588b0718"
            Dim result As Boolean
            If Not File.Exists(SettingsPath()) Then
                result = False
            Else
                Try
                    Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                    Dim oAuthAppSettings As OAuthAppSettings = javaScriptSerializer.Deserialize(Of OAuthAppSettings)(File.ReadAllText(SettingsPath(), Encoding.UTF8))
                    If oAuthAppSettings Is Nothing Then
                        result = False
                    Else
                        If Not String.IsNullOrWhiteSpace(oAuthAppSettings.Tenant) Then
                            tenant = oAuthAppSettings.Tenant.Trim()
                        End If
                        If Not String.IsNullOrWhiteSpace(oAuthAppSettings.ClientId) Then
                            clientId = oAuthAppSettings.ClientId.Trim()
                        End If
                        result = True
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
            Return Path.Combine(AccountStore.DataRoot(), "ews-oauth-app.json")
        End Function
    End Class

