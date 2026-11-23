' LCARSmain/LCARSmain/Modules/modStartSearch.vb
Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.IO
Imports System.Windows.Forms

''' <summary>Builds Start Menu SEARCH results: programs, local files, web links.</summary>
Public Module modStartSearch

    Public Class SearchHit
        Public Kind As String = "" ' program | file | web
        Public Label As String = ""
        Public Target As String = ""
    End Class

    Public Function BuildResults(ByVal query As String, ByVal programsRoot As programList.DirectoryStartItem) As List(Of SearchHit)
        Dim hits As New List(Of SearchHit)()
        Dim q As String = If(query, "").Trim()
        If q = "" Then Return hits
        CollectPrograms(programsRoot, q, hits, 20)
        CollectFiles(q, hits, 20)
        hits.Add(MakeWeb("DuckDuckGo: " & q, "https://duckduckgo.com/?q=" & Uri.EscapeDataString(q)))
        hits.Add(MakeWeb("Wikipedia: " & q, "https://en.wikipedia.org/wiki/Special:Search?search=" & Uri.EscapeDataString(q)))
        hits.Add(MakeWeb("OpenStreetMap: " & q, "https://www.openstreetmap.org/search?query=" & Uri.EscapeDataString(q)))
        Return hits
    End Function

    Private Function MakeWeb(ByVal label As String, ByVal url As String) As SearchHit
        Dim h As New SearchHit()
        h.Kind = "web"
        h.Label = label
        h.Target = url
        Return h
    End Function

    Private Sub CollectPrograms(ByVal dir As programList.DirectoryStartItem, ByVal q As String, ByVal hits As List(Of SearchHit), ByVal limit As Integer)
        If dir Is Nothing OrElse hits.Count >= limit Then Return
        For Each item As Object In dir.subItems
            If hits.Count >= limit Then Return
            Dim fileItem As programList.FileStartItem = TryCast(item, programList.FileStartItem)
            If fileItem IsNot Nothing Then
                If fileItem.Name IsNot Nothing AndAlso fileItem.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 Then
                    Dim h As New SearchHit()
                    h.Kind = "program"
                    h.Label = fileItem.Name
                    h.Target = If(fileItem.Link.Executable, "")
                    hits.Add(h)
                End If
            Else
                Dim subDir As programList.DirectoryStartItem = TryCast(item, programList.DirectoryStartItem)
                If subDir IsNot Nothing Then
                    CollectPrograms(subDir, q, hits, limit)
                End If
            End If
        Next
    End Sub

    Private Sub CollectFiles(ByVal q As String, ByVal hits As List(Of SearchHit), ByVal limit As Integer)
        Dim roots As String() = {
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
        }
        For Each root As String In roots
            If hits.Count >= limit Then Return
            If String.IsNullOrEmpty(root) OrElse Not Directory.Exists(root) Then Continue For
            Try
                For Each path As String In Directory.EnumerateFiles(root, "*" & q & "*", SearchOption.TopDirectoryOnly)
                    If hits.Count >= limit Then Return
                    Dim h As New SearchHit()
                    h.Kind = "file"
                    h.Label = Path.GetFileName(path)
                    h.Target = path
                    hits.Add(h)
                Next
            Catch
            End Try
        Next
    End Sub

End Module
