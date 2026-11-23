Imports System
Imports System.Diagnostics
Imports System.IO
Imports System.Text
Imports System.Web.Script.Serialization
Imports Microsoft.VisualBasic.CompilerServices
    Public NotInheritable Class MorkProfileAcceptanceStore
        Public Class MorkProfileAcceptanceDto
            Public ProfileMabPath As String

            Public RecordedUtc As Date

            Public ContactCount As Integer

            Public RoundTripValidated As Boolean

            Public ByteIdenticalPreserve As Boolean

            Public LastError As String

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Public Shared LastRecord As MorkProfileAcceptanceDto

        Private Const StoreFileName As String = "mork-profile-acceptance.json"

        Private Sub New()
        End Sub

        Public Shared Sub RecordProfileImport(mabPath As String, contactCount As Integer, roundTripValidated As Boolean, byteIdenticalPreserve As Boolean, lastError As String)
            Dim morkProfileAcceptanceDto As MorkProfileAcceptanceDto = New MorkProfileAcceptanceDto()
            morkProfileAcceptanceDto.ProfileMabPath = If(Not Equals(mabPath, Nothing), mabPath, "").Trim()
            morkProfileAcceptanceDto.RecordedUtc = Date.UtcNow
            morkProfileAcceptanceDto.ContactCount = contactCount
            morkProfileAcceptanceDto.RoundTripValidated = roundTripValidated
            morkProfileAcceptanceDto.ByteIdenticalPreserve = byteIdenticalPreserve
            morkProfileAcceptanceDto.LastError = If(Not Equals(lastError, Nothing), lastError, "").Trim()
            LastRecord = morkProfileAcceptanceDto
            Try
                Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                Call File.WriteAllText(StorePath(), javaScriptSerializer.Serialize(LastRecord), Encoding.UTF8)
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
        End Sub

        Public Shared Function TryLoadSummaryLine() As String
            Dim result As String
            Try
                Dim path As String = StorePath()
                If Not File.Exists(path) Then
                    result = ""
                Else
                    Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                    Dim morkProfileAcceptanceDto = javaScriptSerializer.Deserialize(Of MorkProfileAcceptanceDto)(File.ReadAllText(path, Encoding.UTF8))
                    If morkProfileAcceptanceDto Is Nothing Then
                        result = ""
                    Else
                        LastRecord = morkProfileAcceptanceDto
                        result = "MORK PROFILE — cnt " & morkProfileAcceptanceDto.ContactCount.ToString() & If(morkProfileAcceptanceDto.ByteIdenticalPreserve, " · BYTE-OK", If(morkProfileAcceptanceDto.RoundTripValidated, " · RT-OK", " · RT-FAIL"))
                    End If
                End If
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                result = ""
                Call ProjectData.ClearProjectError()
            End Try
            Return result
        End Function

        Private Shared Function StorePath() As String
            Dim text As String = Path.Combine(AccountStore.DataRoot(), "abook")
            If Not Directory.Exists(text) Then
                Directory.CreateDirectory(text)
            End If
            Return Path.Combine(text, "mork-profile-acceptance.json")
        End Function
    End Class

