Imports System
Imports System.Diagnostics
Imports System.IO
Imports System.Text
Imports System.Web.Script.Serialization
Imports Microsoft.VisualBasic.CompilerServices
    Public NotInheritable Class EwsGalSyncStore
        Private Class GalSyncDto
            Public LastSyncUtc As Date

            Public LastDeepSyncUtc As Date

            Public LastGalMergedCount As Integer

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Private Const StoreFileName As String = "ews-gal-sync.json"

        Private Const MinHoursBetweenSync As Double = 3.0

        Private Const MinDaysBetweenDeepSync As Double = 4.0

        Public Shared LastGalMergedCount As Integer = 0

        Private Sub New()
        End Sub

        Public Shared Function TryGetLastDeepSyncUtc() As Date
            Return If(LoadDto() IsNot Nothing, LoadDto().LastDeepSyncUtc, Date.MinValue)
        End Function

        Public Shared Function TryRefreshLastGalMergedCountFromDisk() As Integer
            Dim galSyncDto As GalSyncDto = LoadDto()
            If galSyncDto Is Nothing Then
                Return LastGalMergedCount
            End If
            LastGalMergedCount = Math.Max(0, galSyncDto.LastGalMergedCount)
            Return LastGalMergedCount
        End Function

        Public Shared Function ShouldRunGalSync() As Boolean
            Dim galSyncDto As GalSyncDto = LoadDto()
            If galSyncDto Is Nothing Then
                Return True
            End If
            Return (Date.UtcNow - galSyncDto.LastSyncUtc).TotalHours >= 3.0
        End Function

        Public Shared Sub MarkGalSyncCompleted(Optional mergedCount As Integer = 0)
            LastGalMergedCount = Math.Max(0, mergedCount)
            Try
                Dim galSyncDto As GalSyncDto = LoadDto()
                Dim galSyncDto2 As GalSyncDto = New GalSyncDto()
                galSyncDto2.LastSyncUtc = Date.UtcNow
                galSyncDto2.LastDeepSyncUtc = If(galSyncDto IsNot Nothing, galSyncDto.LastDeepSyncUtc, Date.MinValue)
                galSyncDto2.LastGalMergedCount = LastGalMergedCount
                Dim obj = galSyncDto2
                Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                Call File.WriteAllText(StorePath(), javaScriptSerializer.Serialize(obj), Encoding.UTF8)
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
        End Sub

        Public Shared Function ShouldRunDeepGalSync() As Boolean
            Dim galSyncDto As GalSyncDto = LoadDto()
            If galSyncDto Is Nothing Then
                Return True
            End If
            If Date.Compare(galSyncDto.LastDeepSyncUtc, Date.MinValue) = 0 Then
                Return True
            End If
            Return (Date.UtcNow - galSyncDto.LastDeepSyncUtc).TotalDays >= 4.0
        End Function

        Public Shared Sub MarkDeepGalSyncCompleted()
            Try
                Dim galSyncDto As GalSyncDto = LoadDto()
                Dim galSyncDto2 As GalSyncDto = New GalSyncDto()
                galSyncDto2.LastSyncUtc = If(galSyncDto IsNot Nothing, galSyncDto.LastSyncUtc, Date.UtcNow)
                galSyncDto2.LastDeepSyncUtc = Date.UtcNow
                galSyncDto2.LastGalMergedCount = If(galSyncDto IsNot Nothing, galSyncDto.LastGalMergedCount, LastGalMergedCount)
                Dim obj = galSyncDto2
                Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                Call File.WriteAllText(StorePath(), javaScriptSerializer.Serialize(obj), Encoding.UTF8)
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
        End Sub

        Private Shared Function LoadDto() As GalSyncDto
            Dim result As GalSyncDto
            If Not File.Exists(StorePath()) Then
                result = Nothing
            Else
                Try
                    Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                    result = javaScriptSerializer.Deserialize(Of GalSyncDto)(File.ReadAllText(StorePath(), Encoding.UTF8))
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    result = Nothing
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Private Shared Function StorePath() As String
            Return Path.Combine(AccountStore.DataRoot(), "ews-gal-sync.json")
        End Function
    End Class

