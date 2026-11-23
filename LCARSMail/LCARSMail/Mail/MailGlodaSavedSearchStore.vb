Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Text
Imports System.Web.Script.Serialization
Imports Microsoft.VisualBasic.CompilerServices
    Public NotInheritable Class MailGlodaSavedSearchStore
        Private Class SavedSearchDto
            Public Name As String

            Public Query As String

            Public FolderScope As String

            Public SavedUtc As Date

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Private Const StoreFileName As String = "gloda-saved-searches.json"

        Private Const MaxSaved As Integer = 24

        Private Sub New()
        End Sub

        Public Shared Function TrySaveSearch(name As String, query As String, Optional folderScope As String = Nothing) As Boolean
            Dim text As String = If(Not Equals(query, Nothing), query, "").Trim()
            If text.Length = 0 Then
                Return False
            End If
            Dim text2 As String = If(Not Equals(folderScope, Nothing), folderScope, "").Trim()
            If text2.Length > 0 AndAlso text.IndexOf("folder:", StringComparison.OrdinalIgnoreCase) < 0 Then
                text = "folder:" & text2 & " " & text
            End If
            Dim text3 As String = If(Not Equals(name, Nothing), name, "").Trim()
            If text3.Length = 0 Then
                text3 = text
            End If
            If text3.Length > 64 Then
                text3 = text3.Substring(0, 61) & "…"
            End If
            Dim list As List(Of SavedSearchDto) = LoadAll()
            list.RemoveAll(Function(s) If(s IsNot Nothing AndAlso String.Equals(s.Query, text, StringComparison.OrdinalIgnoreCase), True, False))
            list.Insert(0, New SavedSearchDto With {
        .Name = text3,
        .Query = text,
        .FolderScope = text2,
        .SavedUtc = Date.UtcNow
    })
            While list.Count > 24
                list.RemoveAt(list.Count - 1)
            End While
            Return WriteAll(list)
        End Function

        Public Shared Function TryListSaved() As List(Of KeyValuePair(Of String, String))
            Dim list As List(Of KeyValuePair(Of String, String)) = New List(Of KeyValuePair(Of String, String))()
            For Each item2 In LoadAll()
                If item2 IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(item2.Query) Then
                    Dim savedSearchDto = item2
                    Dim item As KeyValuePair(Of String, String) = New KeyValuePair(Of String, String)(If(Not Equals(savedSearchDto.Name, Nothing), savedSearchDto.Name, item2.Query), item2.Query)
                    list.Add(item)
                End If
            Next
            Return list
        End Function

        Public Shared Function TryGetMostRecentQuery() As String
            Dim list As List(Of SavedSearchDto) = LoadAll()
            If list Is Nothing OrElse list.Count = 0 Then
                Return ""
            End If
            Dim savedSearchDto = list(0)
            If savedSearchDto Is Nothing Then
                Return ""
            End If
            Dim savedSearchDto2 = savedSearchDto
            Return If(Not Equals(savedSearchDto2.Query, Nothing), savedSearchDto2.Query, "").Trim()
        End Function

        Private Shared Function LoadAll() As List(Of SavedSearchDto)
            Dim path As String = StorePath()
            Dim result As List(Of SavedSearchDto)
            If Not File.Exists(path) Then
                result = New List(Of SavedSearchDto)()
            Else
                Try
                    Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                    Dim list = javaScriptSerializer.Deserialize(Of List(Of SavedSearchDto))(File.ReadAllText(path, Encoding.UTF8))
                    result = (If(list IsNot Nothing, list, New List(Of SavedSearchDto)()))
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    result = New List(Of SavedSearchDto)()
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Private Shared Function WriteAll(list As List(Of SavedSearchDto)) As Boolean
            Dim result As Boolean
            Try
                Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                Call File.WriteAllText(StorePath(), javaScriptSerializer.Serialize(list), Encoding.UTF8)
                result = True
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                result = False
                Call ProjectData.ClearProjectError()
            End Try
            Return result
        End Function

        Private Shared Function StorePath() As String
            Dim text As String = Path.Combine(AccountStore.DataRoot(), "search-index")
            If Not Directory.Exists(text) Then
                Directory.CreateDirectory(text)
            End If
            Return Path.Combine(text, "gloda-saved-searches.json")
        End Function
    End Class

