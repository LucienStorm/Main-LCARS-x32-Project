Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Net
Imports System.Runtime.CompilerServices
Imports System.Text
Imports System.Web.Script.Serialization
Imports Microsoft.VisualBasic.CompilerServices
    Public NotInheritable Class MatrixChatService
        Public Class MatrixRoomSummary
            Public RoomId As String

            Public Name As String

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub

            Public Overrides Function ToString() As String
                If Not String.IsNullOrWhiteSpace(Name) Then
                    Return Name
                End If
                Return If(Not Equals(RoomId, Nothing), RoomId, "")
            End Function
        End Class

        Public Shared LastError As String

        Private Sub New()
        End Sub

        Public Shared Function TrySendText(homeserver As String, accessToken As String, roomId As String, message As String) As Boolean
            LastError = Nothing
            Dim text = NormalizeHomeserver(homeserver)
            Dim text2 As String = If(Not Equals(roomId, Nothing), roomId, "").Trim()
            Dim text3 As String = If(Not Equals(accessToken, Nothing), accessToken, "").Trim()
            Dim result As Boolean
            If text.Length = 0 OrElse text2.Length = 0 OrElse text3.Length = 0 Then
                LastError = "Homeserver, room id, and access token are required."
                result = False
            Else
                Dim text4 As String = Guid.NewGuid().ToString("N")
                Dim requestUriString = text & "/_matrix/client/v3/rooms/" & Uri.EscapeDataString(text2) & "/send/m.room.message/" & text4
                Dim s As String = New JavaScriptSerializer().Serialize(New Dictionary(Of String, Object) From {
        {"msgtype", "m.text"},
                                                                {"body", If(Not Equals(message, Nothing), message, "")}
    })
                Try
                    Dim httpWebRequest = CType(WebRequest.Create(requestUriString), HttpWebRequest)
                    httpWebRequest.Method = "PUT"
                    httpWebRequest.ContentType = "application/json"
                    httpWebRequest.Headers.Add("Authorization", "Bearer " & text3)
                    Dim bytes = Encoding.UTF8.GetBytes(s)
                    httpWebRequest.ContentLength = bytes.Length
                    Using stream As Stream = httpWebRequest.GetRequestStream()
                        stream.Write(bytes, 0, bytes.Length)
                    End Using
                    Dim httpWebResponse As HttpWebResponse = CType(httpWebRequest.GetResponse(), HttpWebResponse)
                    result = If(httpWebResponse.StatusCode >= HttpStatusCode.OK AndAlso httpWebResponse.StatusCode < HttpStatusCode.MultipleChoices, True, False)
                Catch ex As Exception
                    ProjectData.SetProjectError(ex)
                    Dim ex2 = ex
                    LastError = ex2.Message
                    result = False
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Public Shared Function TryPasswordLogin(homeserver As String, username As String, password As String, ByRef accessToken As String, ByRef userId As String) As Boolean
            LastError = Nothing
            accessToken = Nothing
            userId = Nothing
            Dim text = NormalizeHomeserver(homeserver)
            Dim result As Boolean
            Dim dictionary As Dictionary(Of String, Object) = Nothing
            If text.Length = 0 OrElse String.IsNullOrWhiteSpace(username) Then
                LastError = "Homeserver and username are required."
                result = False
            Else
                Dim url = text & "/_matrix/client/v3/login"
                Dim jsonBody As String = New JavaScriptSerializer().Serialize(New Dictionary(Of String, Object) From {
        {"type", "m.login.password"},
                                                                    {"identifier", New Dictionary(Of String, Object) From {
                {"type", "m.id.user"},
                                                                                                {"user", username.Trim()}
                            }},
                                                                {"password", If(Not Equals(password, Nothing), password, "")}
    })
                Try
                    Dim text2 = PostJson(url, jsonBody, Nothing)
                    If text2.Length = 0 Then
                        result = False
                    Else
                        Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()

                        If Not (CSharpImpl.__Assign(dictionary, TryCast(javaScriptSerializer.DeserializeObject(text2), Dictionary(Of String, Object))) IsNot Nothing) Then
                            LastError = "Login response invalid."
                            result = False
                        Else
                            If dictionary.ContainsKey("access_token") Then
                                accessToken = Conversions.ToString(dictionary("access_token"))
                            End If
                            If dictionary.ContainsKey("user_id") Then
                                userId = Conversions.ToString(dictionary("user_id"))
                            End If
                            If String.IsNullOrWhiteSpace(accessToken) Then
                                LastError = "No access token in login response."
                                result = False
                            Else
                                result = True
                            End If
                        End If
                    End If
                Catch ex As Exception
                    ProjectData.SetProjectError(ex)
                    Dim ex2 = ex
                    LastError = ex2.Message
                    result = False
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Public Shared Function TryFetchRoomDisplayName(homeserver As String, accessToken As String, roomId As String) As String
            LastError = Nothing
            Dim text = NormalizeHomeserver(homeserver)
            Dim text2 As String = If(Not Equals(roomId, Nothing), roomId, "").Trim()
            If text.Length = 0 OrElse text2.Length = 0 Then
                Return ""
            End If
            Dim url = text & "/_matrix/client/v3/rooms/" & Uri.EscapeDataString(text2) & "/state/m.room.name"
            Dim dictionary As Dictionary(Of String, Object) = Nothing
            Try
                Dim json = GetJson(url, accessToken)
                If json.Length = 0 Then
                    Return text2
                End If
                Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()

                If CSharpImpl.__Assign(dictionary, TryCast(javaScriptSerializer.DeserializeObject(json), Dictionary(Of String, Object))) IsNot Nothing AndAlso dictionary.ContainsKey("name") Then
                    Dim text3 = Conversions.ToString(dictionary("name"))
                    If Not String.IsNullOrWhiteSpace(text3) Then
                        Return text3
                    End If
                End If
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
            Return text2
        End Function

        Public Shared Function TryListJoinedRooms(homeserver As String, accessToken As String) As List(Of MatrixRoomSummary)
            LastError = Nothing
            Dim text = NormalizeHomeserver(homeserver)
            Dim text2 As String = If(Not Equals(accessToken, Nothing), accessToken, "").Trim()
            Dim result As List(Of MatrixRoomSummary)
            Dim array As Object() = Nothing
            If text.Length = 0 OrElse text2.Length = 0 Then
                LastError = "Homeserver and token required."
                result = Nothing
            Else
                Dim url = text & "/_matrix/client/v3/joined_rooms"
                Try
                    Dim json = GetJson(url, text2)
                    If json.Length = 0 Then
                        result = New List(Of MatrixRoomSummary)()
                    Else
                        Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                        Dim dictionary As Dictionary(Of String, Object) = TryCast(javaScriptSerializer.DeserializeObject(json), Dictionary(Of String, Object))
                        Dim list As List(Of MatrixRoomSummary) = New List(Of MatrixRoomSummary)()
                        If dictionary Is Nothing OrElse Not dictionary.ContainsKey("joined_rooms") Then
                            result = list
                        ElseIf Not (CSharpImpl.__Assign(array, TryCast(dictionary("joined_rooms"), Object())) IsNot Nothing) Then
                            result = list
                        Else
                            Dim array2 = array
                            Dim i = 0

                            While i < array2.Length
                                Dim objectValue = RuntimeHelpers.GetObjectValue(array2(i))
                                Dim text3 = Conversions.ToString(objectValue)
                                If Not String.IsNullOrWhiteSpace(text3) Then
                                    Dim name = TryFetchRoomDisplayName(text, text2, text3)
                                    list.Add(New MatrixRoomSummary With {
        .RoomId = text3,
        .Name = name
    })
                                End If

                                i = i + 1
                            End While
                            result = list
                        End If
                    End If
                Catch ex As Exception
                    ProjectData.SetProjectError(ex)
                    Dim ex2 = ex
                    LastError = ex2.Message
                    result = Nothing
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Public Shared Function TryFetchRecentMessages(homeserver As String, accessToken As String, roomId As String) As String
            LastError = Nothing
            Dim text = NormalizeHomeserver(homeserver)
            Dim requestUriString = text & "/_matrix/client/v3/rooms/" & Uri.EscapeDataString(roomId) & "/messages?dir=b&limit=20"
            Dim result As String
            Try
                Dim httpWebRequest = CType(WebRequest.Create(requestUriString), HttpWebRequest)
                httpWebRequest.Method = "GET"
                httpWebRequest.Headers.Add("Authorization", "Bearer " & accessToken)
                Dim httpWebResponse As HttpWebResponse = CType(httpWebRequest.GetResponse(), HttpWebResponse)
                Dim streamReader As StreamReader = New StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8)
                Dim json As String = streamReader.ReadToEnd()
                result = FormatRecentMessages(json)
            Catch ex As Exception
                ProjectData.SetProjectError(ex)
                Dim ex2 = ex
                LastError = ex2.Message
                result = ""
                Call ProjectData.ClearProjectError()
            End Try
            Return result
        End Function

        Public Shared Function FormatRecentMessages(json As String) As String
            If String.IsNullOrWhiteSpace(json) Then
                Return ""
            End If
            Dim dictionary As Dictionary(Of String, Object) = Nothing, array As Object() = Nothing, dictionary2 As Dictionary(Of String, Object) = Nothing
            Try
                Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()

                If Not (CSharpImpl.__Assign(dictionary, TryCast(javaScriptSerializer.DeserializeObject(json), Dictionary(Of String, Object))) IsNot Nothing) OrElse Not dictionary.ContainsKey("chunk") Then
                    Return json
                End If

                If Not (CSharpImpl.__Assign(array, TryCast(dictionary("chunk"), Object())) IsNot Nothing) Then
                    Return json
                End If
                Dim stringBuilder As StringBuilder = New StringBuilder()
                Dim array2 = array
                Dim i = 0

                While i < array2.Length
                    Dim objectValue = RuntimeHelpers.GetObjectValue(array2(i))

                    If CSharpImpl.__Assign(dictionary2, TryCast(objectValue, Dictionary(Of String, Object))) IsNot Nothing Then
                        Dim text = ""
                        If dictionary2.ContainsKey("sender") Then
                            text = Conversions.ToString(dictionary2("sender"))
                        End If
                        Dim dictionary3 As Dictionary(Of String, Object) = TryCast(dictionary2("content"), Dictionary(Of String, Object))
                        Dim text2 = ""
                        If dictionary3 IsNot Nothing AndAlso dictionary3.ContainsKey("body") Then
                            text2 = Conversions.ToString(dictionary3("body"))
                        End If
                        If text2.Length <> 0 Then
                            stringBuilder.AppendLine(text & ": " & text2)
                        End If
                    End If

                    i = i + 1
                End While
                Dim text3 As String = stringBuilder.ToString().Trim()
                If text3.Length > 0 Then
                    Return text3
                End If
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
            Return json
        End Function

        Private Shared Function PostJson(url As String, jsonBody As String, bearerToken As String) As String
            Dim httpWebRequest = CType(WebRequest.Create(url), HttpWebRequest)
            httpWebRequest.Method = "POST"
            httpWebRequest.ContentType = "application/json"
            If Not String.IsNullOrWhiteSpace(bearerToken) Then
                httpWebRequest.Headers.Add("Authorization", "Bearer " & bearerToken)
            End If
            Dim bytes = Encoding.UTF8.GetBytes(jsonBody)
            httpWebRequest.ContentLength = bytes.Length
            Using stream As Stream = httpWebRequest.GetRequestStream()
                stream.Write(bytes, 0, bytes.Length)
            End Using
            Dim httpWebResponse As HttpWebResponse = CType(httpWebRequest.GetResponse(), HttpWebResponse)
            Dim streamReader As StreamReader = New StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8)
            Return streamReader.ReadToEnd()
        End Function

        Private Shared Function GetJson(url As String, bearerToken As String) As String
            Dim httpWebRequest = CType(WebRequest.Create(url), HttpWebRequest)
            httpWebRequest.Method = "GET"
            If Not String.IsNullOrWhiteSpace(bearerToken) Then
                httpWebRequest.Headers.Add("Authorization", "Bearer " & bearerToken)
            End If
            Dim httpWebResponse As HttpWebResponse = CType(httpWebRequest.GetResponse(), HttpWebResponse)
            Dim streamReader As StreamReader = New StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8)
            Return streamReader.ReadToEnd()
        End Function

        Private Shared Function NormalizeHomeserver(homeserver As String) As String
            Dim text As String = If(Not Equals(homeserver, Nothing), homeserver, "").Trim().TrimEnd("/"c)
            If text.Length = 0 Then
                Return ""
            End If
            If Not text.StartsWith("http", StringComparison.OrdinalIgnoreCase) Then
                text = "https://" & text
            End If
            Return text
        End Function

        Private Class CSharpImpl
            Shared Function __Assign(Of T)(ByRef target As T, value As T) As T
                target = value
                Return value
            End Function
        End Class
    End Class

