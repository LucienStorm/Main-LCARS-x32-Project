Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Text
Imports System.Web.Script.Serialization
Imports Microsoft.VisualBasic.CompilerServices

    <StandardModule>
    Public NotInheritable Class EwsCalendarRsvpQueue
        Public Class RsvpRequest
            Public Uid As String

            Public PartStat As String

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Private Const QueueFileName As String = "ews-rsvp-queue.json"

        Public Shared Sub Enqueue(eventUid As String, partStat As String)
            Dim text As String = If(Not Equals(eventUid, Nothing), eventUid, "").Trim()
            Dim text2 As String = If(Not Equals(partStat, Nothing), partStat, "").Trim().ToUpperInvariant()
            If text.Length <> 0 AndAlso text2.Length <> 0 Then
                Dim list As List(Of RsvpRequest) = LoadQueue()
                list.Add(New RsvpRequest With {
        .Uid = text,
        .PartStat = text2
    })
                SaveQueue(list)
            End If
        End Sub

        Private Shared Function QueuePath() As String
            Return Path.Combine(IcsCalendarStore.DataRoot(), "ews-rsvp-queue.json")
        End Function

        Private Shared Function LoadQueue() As List(Of RsvpRequest)
            Dim path As String = QueuePath()
            Dim result As List(Of RsvpRequest)
            If Not File.Exists(path) Then
                result = New List(Of RsvpRequest)()
            Else
                Try
                    Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                    Dim list = javaScriptSerializer.Deserialize(Of List(Of RsvpRequest))(File.ReadAllText(path, Encoding.UTF8))
                    result = (If(list IsNot Nothing, list, New List(Of RsvpRequest)()))
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    result = New List(Of RsvpRequest)()
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Private Shared Sub SaveQueue(items As List(Of RsvpRequest))
            If items Is Nothing Then
                Return
            End If
            Try
                Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                Call File.WriteAllText(QueuePath(), javaScriptSerializer.Serialize(items), Encoding.UTF8)
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
        End Sub
    End Class
