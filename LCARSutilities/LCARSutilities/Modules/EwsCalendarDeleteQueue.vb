Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Text
Imports System.Web.Script.Serialization
Imports Microsoft.VisualBasic.CompilerServices

    <StandardModule>
    Public NotInheritable Class EwsCalendarDeleteQueue
        Private Const QueueFileName As String = "ews-delete-queue.json"

        Public Shared Sub Enqueue(ewsItemId As String)
            Dim text As String = If(Not Equals(ewsItemId, Nothing), ewsItemId, "").Trim()
            If text.Length <> 0 Then
                Dim list As List(Of String) = LoadQueue()
                If Not list.Contains(text) Then
                    list.Add(text)
                    SaveQueue(list)
                End If
            End If
        End Sub

        Private Shared Function QueuePath() As String
            Return Path.Combine(IcsCalendarStore.DataRoot(), "ews-delete-queue.json")
        End Function

        Private Shared Function LoadQueue() As List(Of String)
            Dim path As String = QueuePath()
            Dim result As List(Of String)
            If Not File.Exists(path) Then
                result = New List(Of String)()
            Else
                Try
                    Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                    Dim list = javaScriptSerializer.Deserialize(Of List(Of String))(File.ReadAllText(path, Encoding.UTF8))
                    result = (If(list IsNot Nothing, list, New List(Of String)()))
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    result = New List(Of String)()
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Private Shared Sub SaveQueue(ids As List(Of String))
            If ids Is Nothing Then
                Return
            End If
            Try
                Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                Call File.WriteAllText(QueuePath(), javaScriptSerializer.Serialize(ids), Encoding.UTF8)
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
        End Sub
    End Class
