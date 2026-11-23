Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Text
Imports System.Web.Script.Serialization
Imports Microsoft.VisualBasic.CompilerServices
    Public NotInheritable Class EwsCalendarDeleteQueue
        Private Const QueueFileName As String = "ews-delete-queue.json"

        Private Sub New()
        End Sub

        Private Shared Function QueuePath() As String
            Return Path.Combine(LcarsCalendarBridge.CalendarDataRoot(), "ews-delete-queue.json")
        End Function

        Public Shared Function TakePendingDeletes() As List(Of String)
            Dim path As String = QueuePath()
            Dim result As List(Of String)
            If Not File.Exists(path) Then
                result = New List(Of String)()
            Else
                Try
                    Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                    Dim list = javaScriptSerializer.Deserialize(Of List(Of String))(File.ReadAllText(path, Encoding.UTF8))
                    File.Delete(path)
                    If list Is Nothing Then
                        result = New List(Of String)()
                    Else
                        Dim list2 As List(Of String) = New List(Of String)()
                        For Each item In list
                            Dim text As String = If(Not Equals(item, Nothing), item, "").Trim()
                            If text.Length > 0 Then
                                list2.Add(text)
                            End If
                        Next
                        result = list2
                    End If
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    result = New List(Of String)()
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function
    End Class

