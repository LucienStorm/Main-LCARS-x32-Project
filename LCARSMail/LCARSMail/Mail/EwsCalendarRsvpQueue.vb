Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Text
Imports System.Web.Script.Serialization
Imports Microsoft.VisualBasic.CompilerServices
    Public NotInheritable Class EwsCalendarRsvpQueue
        Public Class RsvpRequest
            Public Uid As String

            Public PartStat As String

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Private Const QueueFileName As String = "ews-rsvp-queue.json"

        Public Shared LastProcessedCount As Integer = 0

        Private Sub New()
        End Sub

        Private Shared Function QueuePath() As String
            Return Path.Combine(LcarsCalendarBridge.CalendarDataRoot(), "ews-rsvp-queue.json")
        End Function

        Public Shared Function TakePendingRsvps() As List(Of RsvpRequest)
            LastProcessedCount = 0
            Dim path As String = QueuePath()
            Dim result As List(Of RsvpRequest)
            If Not File.Exists(path) Then
                result = New List(Of RsvpRequest)()
            Else
                Try
                    Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                    Dim list = javaScriptSerializer.Deserialize(Of List(Of RsvpRequest))(File.ReadAllText(path, Encoding.UTF8))
                    File.Delete(path)
                    result = (If(list IsNot Nothing, list, New List(Of RsvpRequest)()))
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    result = New List(Of RsvpRequest)()
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function
    End Class

