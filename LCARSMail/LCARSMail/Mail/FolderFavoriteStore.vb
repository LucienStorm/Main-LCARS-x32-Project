Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Text
Imports Microsoft.VisualBasic.CompilerServices
    Public NotInheritable Class FolderFavoriteStore
        Public Shared LastError As String

        Private Sub New()
        End Sub

        Private Shared Function StorePath() As String
            Dim text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LCARS x32", "Comms")
            If Not Directory.Exists(text) Then
                Directory.CreateDirectory(text)
            End If
            Return Path.Combine(text, "folder-favorites.txt")
        End Function

        Public Shared Function LoadFavorites() As List(Of String)
            LastError = Nothing
            Dim list As List(Of String) = New List(Of String)()
            Dim path As String = StorePath()
            If Not File.Exists(path) Then
                Return list
            End If
            Try
                Dim array = File.ReadAllLines(path, Encoding.UTF8)
                For Each text In array
                    Dim text2 As String = If(Not Equals(text, Nothing), text, "").Trim()
                    If text2.Length = 0 OrElse Not Directory.Exists(text2) Then
                        Continue For
                    End If
                    Dim flag = False
                    For Each item In list
                        If String.Equals(item, text2, StringComparison.OrdinalIgnoreCase) Then
                            flag = True
                            Exit For
                        End If
                    Next
                    If Not flag Then
                        list.Add(text2)
                    End If
                Next
            Catch ex As Exception
                ProjectData.SetProjectError(ex)
                Dim ex2 = ex
                LastError = ex2.Message
                Call ProjectData.ClearProjectError()
            End Try
            Return list
        End Function

        Public Shared Function IsFavorite(folderPath As String) As Boolean
            Dim text = Normalize(folderPath)
            If text.Length = 0 Then
                Return False
            End If
            For Each item In LoadFavorites()
                If String.Equals(item, text, StringComparison.OrdinalIgnoreCase) Then
                    Return True
                End If
            Next
            Return False
        End Function

        Public Shared Function AddFavorite(folderPath As String) As Boolean
            LastError = Nothing
            Dim text = Normalize(folderPath)
            If text.Length = 0 OrElse Not Directory.Exists(text) Then
                LastError = "Folder does not exist."
                Return False
            End If
            Dim list As List(Of String) = LoadFavorites()
            For Each item In list
                If String.Equals(item, text, StringComparison.OrdinalIgnoreCase) Then
                    Return True
                End If
            Next
            list.Add(text)
            Return SaveFavorites(list)
        End Function

        Public Shared Function RemoveFavorite(folderPath As String) As Boolean
            LastError = Nothing
            Dim text = Normalize(folderPath)
            If text.Length = 0 Then
                Return False
            End If
            Dim list As List(Of String) = LoadFavorites()
            Dim list2 As List(Of String) = New List(Of String)()
            Dim flag = False
            For Each item In list
                If String.Equals(item, text, StringComparison.OrdinalIgnoreCase) Then
                    flag = True
                Else
                    list2.Add(item)
                End If
            Next
            If Not flag Then
                Return True
            End If
            Return SaveFavorites(list2)
        End Function

        Private Shared Function SaveFavorites(paths As List(Of String)) As Boolean
            Dim result As Boolean
            Try
                Call File.WriteAllLines(StorePath(), paths.ToArray(), Encoding.UTF8)
                result = True
            Catch ex As Exception
                ProjectData.SetProjectError(ex)
                Dim ex2 = ex
                LastError = ex2.Message
                result = False
                Call ProjectData.ClearProjectError()
            End Try
            Return result
        End Function

        Private Shared Function Normalize(folderPath As String) As String
            Dim text As String = If(Not Equals(folderPath, Nothing), folderPath, "").Trim()
            Dim result As String
            If text.Length = 0 Then
                result = ""
            Else
                Try
                    result = Path.GetFullPath(text.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    result = text
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function
    End Class

