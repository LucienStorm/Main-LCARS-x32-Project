Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Text
Imports System.Web.Script.Serialization
Imports Microsoft.VisualBasic.CompilerServices
    Public NotInheritable Class MailSearchIndexCursor
        Private Class CursorDto
            Public AccountId As String

            Public FolderIndex As Integer

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Private Class CorpusCursorDto
            Public AccountId As String

            Public RowIndex As Integer

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Private Const StoreFileName As String = "index-folder-cursor.json"

        Private Const CorpusStoreFileName As String = "index-corpus-cursor.json"

        Private Const MetaStoreFileName As String = "index-meta-cursor.json"

        Private Sub New()
        End Sub

        Public Shared Function TakeNextFolders(accountId As String, allFolders As IList(Of MailFolderInfo), batchSize As Integer) As List(Of MailFolderInfo)
            Dim list As List(Of MailFolderInfo) = New List(Of MailFolderInfo)()
            If String.IsNullOrWhiteSpace(accountId) OrElse allFolders Is Nothing OrElse allFolders.Count = 0 Then
                Return list
            End If
            Dim num = Math.Max(1, batchSize)
            Dim dictionary As Dictionary(Of String, Integer) = LoadMap()
            Dim num2 = 0
            If dictionary.ContainsKey(accountId) Then
                num2 = dictionary(accountId)
            End If
            If num2 < 0 OrElse num2 >= allFolders.Count Then
                num2 = 0
            End If
            Dim num3 = 0
            Dim num4 = num2
            While num3 < num AndAlso num3 < allFolders.Count
                Dim mailFolderInfo = allFolders(num4)
                If mailFolderInfo IsNot Nothing Then
                    list.Add(mailFolderInfo)
                End If
                num3 += 1
                num4 += 1
                If num4 >= allFolders.Count Then
                    num4 = 0
                End If
                If num4 = num2 AndAlso num3 > 0 Then
                    Exit While
                End If
            End While
            dictionary(accountId) = num4
            SaveMap(dictionary)
            Return list
        End Function

        Private Shared Function LoadMap() As Dictionary(Of String, Integer)
            Dim result As Dictionary(Of String, Integer)
            If Not File.Exists(StorePath()) Then
                result = New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
            Else
                Try
                    Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                    Dim list As List(Of CursorDto) = javaScriptSerializer.Deserialize(Of List(Of CursorDto))(File.ReadAllText(StorePath(), Encoding.UTF8))
                    Dim dictionary As Dictionary(Of String, Integer) = New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
                    If list Is Nothing Then
                        result = dictionary
                    Else
                        For Each item In list
                            If item IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(item.AccountId) Then
                                dictionary(item.AccountId) = item.FolderIndex
                            End If
                        Next
                        result = dictionary
                    End If
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    result = New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Private Shared Sub SaveMap(map As Dictionary(Of String, Integer))
            Try
                Dim list As List(Of CursorDto) = New List(Of CursorDto)()
                For Each item In map
                    list.Add(New CursorDto With {
        .AccountId = item.Key,
        .FolderIndex = item.Value
    })
                Next
                Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                Call File.WriteAllText(StorePath(), javaScriptSerializer.Serialize(list), Encoding.UTF8)
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
        End Sub

        Private Shared Function StorePath() As String
            Return Path.Combine(AccountStore.DataRoot(), "search-index", "index-folder-cursor.json")
        End Function

        Public Shared Function FormatRotationHint(accountId As String, folderCount As Integer) As String
            If String.IsNullOrWhiteSpace(accountId) OrElse folderCount <= 0 Then
                Return ""
            End If
            Dim dictionary As Dictionary(Of String, Integer) = LoadMap()
            Dim num = 0
            If dictionary.ContainsKey(accountId) Then
                num = dictionary(accountId)
            End If
            If num < 0 OrElse num >= folderCount Then
                num = 0
            End If
            Dim text = ""
            Dim dictionary2 As Dictionary(Of String, Integer) = LoadCorpusMap()
            If dictionary2.ContainsKey(accountId) Then
                text = " · FTS-CUR " & dictionary2(accountId).ToString()
            End If
            Return "CURSOR " & num.ToString() & "/" & folderCount.ToString() & text
        End Function

        Public Shared Function GetCorpusRebuildStart(accountId As String, entryCount As Integer) As Integer
            If String.IsNullOrWhiteSpace(accountId) OrElse entryCount <= 0 Then
                Return 0
            End If
            Dim dictionary As Dictionary(Of String, Integer) = LoadCorpusMap()
            Dim num = 0
            If dictionary.ContainsKey(accountId) Then
                num = dictionary(accountId)
            End If
            If num < 0 OrElse num >= entryCount Then
                num = 0
            End If
            Return num
        End Function

        Public Shared Sub AdvanceCorpusRebuildCursor(accountId As String, entryCount As Integer, processed As Integer)
            If Not String.IsNullOrWhiteSpace(accountId) AndAlso entryCount > 0 AndAlso processed > 0 Then
                Dim dictionary As Dictionary(Of String, Integer) = LoadCorpusMap()
                Dim num = 0
                If dictionary.ContainsKey(accountId) Then
                    num = dictionary(accountId)
                End If
                If num < 0 OrElse num >= entryCount Then
                    num = 0
                End If
                dictionary(accountId) = num + processed Mod entryCount
                SaveCorpusMap(dictionary)
            End If
        End Sub

        Private Shared Function LoadCorpusMap() As Dictionary(Of String, Integer)
            Dim path As String = CorpusStorePath()
            Dim result As Dictionary(Of String, Integer)
            If Not File.Exists(path) Then
                result = New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
            Else
                Try
                    Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                    Dim list = javaScriptSerializer.Deserialize(Of List(Of CorpusCursorDto))(File.ReadAllText(path, Encoding.UTF8))
                    Dim dictionary As Dictionary(Of String, Integer) = New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
                    If list Is Nothing Then
                        result = dictionary
                    Else
                        For Each item In list
                            If item IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(item.AccountId) Then
                                dictionary(item.AccountId) = item.RowIndex
                            End If
                        Next
                        result = dictionary
                    End If
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    result = New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Private Shared Sub SaveCorpusMap(map As Dictionary(Of String, Integer))
            Try
                Dim list As List(Of CorpusCursorDto) = New List(Of CorpusCursorDto)()
                For Each item In map
                    list.Add(New CorpusCursorDto With {
        .AccountId = item.Key,
        .RowIndex = item.Value
    })
                Next
                Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                Call File.WriteAllText(CorpusStorePath(), javaScriptSerializer.Serialize(list), Encoding.UTF8)
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
        End Sub

        Private Shared Function CorpusStorePath() As String
            Return Path.Combine(AccountStore.DataRoot(), "search-index", "index-corpus-cursor.json")
        End Function

        Public Shared Function GetMetaBackfillStart(accountId As String, entryCount As Integer) As Integer
            If String.IsNullOrWhiteSpace(accountId) OrElse entryCount <= 0 Then
                Return 0
            End If
            Dim dictionary As Dictionary(Of String, Integer) = LoadMetaMap()
            Dim num = 0
            If dictionary.ContainsKey(accountId) Then
                num = dictionary(accountId)
            End If
            If num < 0 OrElse num >= entryCount Then
                num = 0
            End If
            Return num
        End Function

        Public Shared Sub AdvanceMetaBackfillCursor(accountId As String, entryCount As Integer, processed As Integer)
            If Not String.IsNullOrWhiteSpace(accountId) AndAlso entryCount > 0 AndAlso processed > 0 Then
                Dim dictionary As Dictionary(Of String, Integer) = LoadMetaMap()
                Dim num = 0
                If dictionary.ContainsKey(accountId) Then
                    num = dictionary(accountId)
                End If
                If num < 0 OrElse num >= entryCount Then
                    num = 0
                End If
                dictionary(accountId) = num + processed Mod entryCount
                SaveMetaMap(dictionary)
            End If
        End Sub

        Private Shared Function LoadMetaMap() As Dictionary(Of String, Integer)
            Dim path As String = MetaStorePath()
            Dim result As Dictionary(Of String, Integer)
            If Not File.Exists(path) Then
                result = New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
            Else
                Try
                    Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                    Dim list = javaScriptSerializer.Deserialize(Of List(Of CorpusCursorDto))(File.ReadAllText(path, Encoding.UTF8))
                    Dim dictionary As Dictionary(Of String, Integer) = New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
                    If list Is Nothing Then
                        result = dictionary
                    Else
                        For Each item In list
                            If item IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(item.AccountId) Then
                                dictionary(item.AccountId) = item.RowIndex
                            End If
                        Next
                        result = dictionary
                    End If
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    result = New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Private Shared Sub SaveMetaMap(map As Dictionary(Of String, Integer))
            Try
                Dim list As List(Of CorpusCursorDto) = New List(Of CorpusCursorDto)()
                For Each item In map
                    list.Add(New CorpusCursorDto With {
        .AccountId = item.Key,
        .RowIndex = item.Value
    })
                Next
                Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                Call File.WriteAllText(MetaStorePath(), javaScriptSerializer.Serialize(list), Encoding.UTF8)
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
        End Sub

        Private Shared Function MetaStorePath() As String
            Return Path.Combine(AccountStore.DataRoot(), "search-index", "index-meta-cursor.json")
        End Function
    End Class

