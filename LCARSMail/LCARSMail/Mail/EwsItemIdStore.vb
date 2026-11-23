Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Text
Imports System.Web.Script.Serialization
Imports Microsoft.VisualBasic.CompilerServices
    Public NotInheritable Class EwsItemIdStore
        Private Class MapEntry
            Public Uid As ULong

            Public ItemId As String

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Private Const MapSubdir As String = "ews-item-map"

        Private Sub New()
        End Sub

        Public Shared Function ItemIdToUid(itemId As String) As ULong
            Dim text = If(Not Equals(itemId, Nothing), itemId, "")
            If text.Length = 0 Then
                Return 0UL
            End If
            Dim num = 14695981039346656037UL
            Dim bytes = Encoding.UTF8.GetBytes(text)
            For Each b In bytes
                num = num Xor b
                num = num * 1099511628211L
            Next
            If num = 0 Then
                num = 1UL
            End If
            Return num
        End Function

        Public Shared Sub Remember(accountId As String, folder As String, uid As ULong, itemId As String)
            If Not String.IsNullOrWhiteSpace(accountId) AndAlso uid <> 0 Then
                Dim text As String = If(Not Equals(itemId, Nothing), itemId, "").Trim()
                If text.Length <> 0 Then
                    Dim dictionary = LoadMap(accountId, folder)
                    dictionary(uid.ToString()) = New MapEntry With {
        .Uid = uid,
        .ItemId = text
    }
                    SaveMap(accountId, folder, dictionary)
                End If
            End If
        End Sub

        Public Shared Function TryGetItemId(accountId As String, folder As String, uid As ULong, ByRef itemId As String) As Boolean
            itemId = Nothing
            If String.IsNullOrWhiteSpace(accountId) OrElse uid = 0 Then
                Return False
            End If
            Dim dictionary = LoadMap(accountId, folder)
            Dim value As MapEntry = Nothing
            If Not dictionary.TryGetValue(uid.ToString(), value) OrElse value Is Nothing Then
                Return False
            End If
            itemId = value.ItemId
            Return If(Not Equals(itemId, Nothing) AndAlso itemId.Length > 0, True, False)
        End Function

        Private Shared Function LoadMap(accountId As String, folder As String) As Dictionary(Of String, MapEntry)
            Dim path = MapPath(accountId, folder)
            Dim result As Dictionary(Of String, MapEntry)
            If Not File.Exists(path) Then
                result = New Dictionary(Of String, MapEntry)(StringComparer.Ordinal)
            Else
                Try
                    Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                    Dim list = javaScriptSerializer.Deserialize(Of List(Of MapEntry))(File.ReadAllText(path, Encoding.UTF8))
                    Dim dictionary As Dictionary(Of String, MapEntry) = New Dictionary(Of String, MapEntry)(StringComparer.Ordinal)
                    If list Is Nothing Then
                        result = dictionary
                    Else
                        For Each item In list
                            If item IsNot Nothing AndAlso item.Uid <> 0 Then
                                dictionary(item.Uid.ToString()) = item
                            End If
                        Next
                        result = dictionary
                    End If
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    result = New Dictionary(Of String, MapEntry)(StringComparer.Ordinal)
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Private Shared Sub SaveMap(accountId As String, folder As String, map As Dictionary(Of String, MapEntry))
            Dim path As String = IO.Path.Combine(AccountStore.DataRoot(), "ews-item-map")
            If Not Directory.Exists(path) Then
                Directory.CreateDirectory(path)
            End If
            Dim list As List(Of MapEntry) = New List(Of MapEntry)()
            If map IsNot Nothing Then
                For Each item In map
                    If item.Value IsNot Nothing Then
                        list.Add(item.Value)
                    End If
                Next
            End If
            Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
            File.WriteAllText(MapPath(accountId, folder), javaScriptSerializer.Serialize(list), Encoding.UTF8)
        End Sub

        Private Shared Function MapPath(accountId As String, folder As String) As String
            Dim text = SanitizeToken(accountId)
            Dim text2 = SanitizeToken(If(Not Equals(folder, Nothing), folder, "INBOX"))
            Return Path.Combine(AccountStore.DataRoot(), "ews-item-map", text & "__" & text2 & ".json")
        End Function

        Private Shared Function SanitizeToken(raw As String) As String
            Dim text As String = If(Not Equals(raw, Nothing), raw, "").Trim()
            If text.Length = 0 Then
                Return "default"
            End If
            Dim invalidFileNameChars As Char() = Path.GetInvalidFileNameChars()
            Dim stringBuilder As StringBuilder = New StringBuilder(text.Length)
            Dim text2 = text
            Dim i = 0
            Dim length = text2.Length

            While i < length
                Dim value = text2(i)
                If Array.IndexOf(invalidFileNameChars, value) >= 0 Then
                    stringBuilder.Append("_"c)
                Else
                    stringBuilder.Append(value)
                End If

                i = i + 1
            End While
            Return stringBuilder.ToString()
        End Function
    End Class

