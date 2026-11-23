Imports System
Imports System.Diagnostics
Imports System.IO
Imports System.Text
Imports System.Web.Script.Serialization
Imports Microsoft.VisualBasic.CompilerServices
    Public NotInheritable Class EwsGalWriteProofStore
        Public Class GalWriteProofDto
            Public RecordedUtc As Date

            Public DirtyCount As Integer

            Public PersonaAttempts As Integer

            Public PersonaSuccess As Integer

            Public DirectoryAttempts As Integer

            Public DirectorySuccess As Integer

            Public ShadowUpdateSuccess As Integer

            Public ShadowCreateSuccess As Integer

            Public PersonaDenied As Boolean

            Public DirectoryDenied As Boolean

            Public SummaryLine As String

            Public DominantWritePath As String

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Private Const StoreFileName As String = "ews-gal-write-proof.json"

        Public Shared LastProof As GalWriteProofDto

        Private Sub New()
        End Sub

        Public Shared Sub RecordPushSession(dirtyCount As Integer, personaAttempts As Integer, personaSuccess As Integer, directoryAttempts As Integer, directorySuccess As Integer, shadowUpdateSuccess As Integer, shadowCreateSuccess As Integer, personaDenied As Boolean, directoryDenied As Boolean)
            Dim text As String = "GAL PUSH PROOF — dirty " & dirtyCount.ToString() & " · persona " & personaSuccess.ToString() & "/" & personaAttempts.ToString() & " · dir " & directorySuccess.ToString() & "/" & directoryAttempts.ToString() & " · shadow upd " & shadowUpdateSuccess.ToString() & " · shadow new " & shadowCreateSuccess.ToString()
            If personaDenied Then
                text += " · PERSONA DENIED"
            End If
            If directoryDenied Then
                text += " · DIR DENIED"
            End If
            If personaSuccess = 0 AndAlso directorySuccess = 0 AndAlso shadowUpdateSuccess = 0 AndAlso shadowCreateSuccess = 0 AndAlso dirtyCount > 0 Then
                text += " · NO GAL WRITE CONFIRMED"
            End If
            Dim text2 = "none"
            If personaSuccess > 0 Then
                text2 = "persona"
            End If
            If directorySuccess > personaSuccess Then
                text2 = "directory"
            End If
            If shadowUpdateSuccess + shadowCreateSuccess > directorySuccess AndAlso shadowUpdateSuccess + shadowCreateSuccess > personaSuccess Then
                text2 = "shadow"
            End If
            If Operators.CompareString(text2, "none", TextCompare:=False) <> 0 Then
                text = text & " · PATH " & text2.ToUpperInvariant()
            End If
            Dim galWriteProofDto As GalWriteProofDto = New GalWriteProofDto()
            galWriteProofDto.RecordedUtc = Date.UtcNow
            galWriteProofDto.DirtyCount = dirtyCount
            galWriteProofDto.PersonaAttempts = personaAttempts
            galWriteProofDto.PersonaSuccess = personaSuccess
            galWriteProofDto.DirectoryAttempts = directoryAttempts
            galWriteProofDto.DirectorySuccess = directorySuccess
            galWriteProofDto.ShadowUpdateSuccess = shadowUpdateSuccess
            galWriteProofDto.ShadowCreateSuccess = shadowCreateSuccess
            galWriteProofDto.PersonaDenied = personaDenied
            galWriteProofDto.DirectoryDenied = directoryDenied
            galWriteProofDto.SummaryLine = text
            galWriteProofDto.DominantWritePath = text2
            LastProof = galWriteProofDto
            Try
                Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                Call File.WriteAllText(StorePath(), javaScriptSerializer.Serialize(LastProof), Encoding.UTF8)
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
        End Sub

        Public Shared Function TryGetDominantWritePath() As String
            Dim text As String = TryLoadLastProofSummary()
            If EwsGalWriteProofStore.LastProof Is Nothing Then
                Return ""
            End If
            Dim lastProof = EwsGalWriteProofStore.LastProof
            Return If(Not Equals(lastProof.DominantWritePath, Nothing), lastProof.DominantWritePath, "").Trim()
        End Function

        Public Shared Function TryLoadLastProofSummary() As String
            Dim result As String
            Try
                Dim path As String = StorePath()
                If Not File.Exists(path) Then
                    result = ""
                Else
                    Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                    Dim galWriteProofDto = javaScriptSerializer.Deserialize(Of GalWriteProofDto)(File.ReadAllText(path, Encoding.UTF8))
                    If galWriteProofDto Is Nothing OrElse String.IsNullOrWhiteSpace(galWriteProofDto.SummaryLine) Then
                        result = ""
                    Else
                        LastProof = galWriteProofDto
                        result = galWriteProofDto.SummaryLine.Trim()
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
            Dim text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LCARS", "Mail")
            If Not Directory.Exists(text) Then
                Directory.CreateDirectory(text)
            End If
            Return Path.Combine(text, "ews-gal-write-proof.json")
        End Function
    End Class

