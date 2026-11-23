Imports System
Imports System.Diagnostics
Imports System.IO
Imports System.Text
Imports System.Web.Script.Serialization
Imports Microsoft.VisualBasic.CompilerServices
    Public NotInheritable Class MatrixAccountStore
        Public Class MatrixSettings
            Public Homeserver As String

            Public UserId As String

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Private Const SettingsFileName As String = "matrix-settings.json"

        Private Const TokenSecretKey As String = "matrix"

        Private Sub New()
        End Sub

        Public Shared Sub SaveSession(homeserver As String, userId As String, accessToken As String)
            Dim text As String = If(Not Equals(homeserver, Nothing), homeserver, "").Trim()
            If text.Length = 0 Then
                Return
            End If
            Try
                Dim matrixSettings As MatrixSettings = New MatrixSettings()
                matrixSettings.Homeserver = text
                matrixSettings.UserId = If(Not Equals(userId, Nothing), userId, "").Trim()
                Dim obj = matrixSettings
                Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                Call File.WriteAllText(SettingsPath(), javaScriptSerializer.Serialize(obj), Encoding.UTF8)
                If Not Equals(accessToken, Nothing) Then
                    AccountStore.SavePassword("matrix", accessToken)
                End If
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
        End Sub

        Public Shared Function TryLoadSession(ByRef homeserver As String, ByRef userId As String, ByRef accessToken As String) As Boolean
            homeserver = Nothing
            userId = Nothing
            accessToken = Nothing
            Dim result As Boolean
            If Not File.Exists(SettingsPath()) Then
                result = False
            Else
                Try
                    Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                    Dim matrixSettings As MatrixSettings = javaScriptSerializer.Deserialize(Of MatrixSettings)(File.ReadAllText(SettingsPath(), Encoding.UTF8))
                    If matrixSettings Is Nothing OrElse String.IsNullOrWhiteSpace(matrixSettings.Homeserver) Then
                        result = False
                    Else
                        homeserver = matrixSettings.Homeserver.Trim()
                        Dim matrixSettings2 = matrixSettings
                        userId = If(Not Equals(matrixSettings2.UserId, Nothing), matrixSettings2.UserId, "").Trim()
                        result = AccountStore.TryGetPassword("matrix", accessToken)
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
            Return Path.Combine(AccountStore.DataRoot(), "matrix-settings.json")
        End Function
    End Class

