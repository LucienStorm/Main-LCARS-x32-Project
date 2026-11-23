Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Text
Imports System.Web.Script.Serialization
Imports Microsoft.VisualBasic.CompilerServices

    <StandardModule>
    Public NotInheritable Class MailAttendeeHint
        Private Class AccountEmailDto
            Public EmailAddress As String

            Public Username As String

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Public Shared Function TryGetPrimaryAttendeeMailto() As String
            Dim path = IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LCARS x32", "Mail", "accounts.json")
            If Not File.Exists(path) Then
                Return ""
            End If
            Try
                Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                Dim list = javaScriptSerializer.Deserialize(Of List(Of AccountEmailDto))(File.ReadAllText(path, Encoding.UTF8))
                If list Is Nothing Then
                    Return ""
                End If
                For Each item In list
                    If item Is Nothing Then
                        Continue For
                    End If
                    Dim accountEmailDto = item
                    Dim text As String = If(Not Equals(accountEmailDto.EmailAddress, Nothing), accountEmailDto.EmailAddress, "").Trim()
                    If text.Length = 0 Then
                        accountEmailDto = item
                        text = If(Not Equals(accountEmailDto.Username, Nothing), accountEmailDto.Username, "").Trim()
                    End If
                    If text.Length = 0 Then
                        Continue For
                    End If
                    If text.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) Then
                        Return text
                    End If
                    Return "mailto:" & text
                Next
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
            Return ""
        End Function

        Public Shared Function TryLaunchOrganizerRsvpMail(organizerLine As String, eventSummary As String, partStat As String) As Boolean
            Dim text = NormalizeOrganizerSmtp(organizerLine)
            Dim result As Boolean
            If text.Length = 0 Then
                result = False
            Else
                Dim text2 As String = If(Not Equals(partStat, Nothing), partStat, "").Trim().ToUpperInvariant()
                If text2.Length = 0 Then
                    result = False
                Else
                    Dim text3 As String = Uri.EscapeDataString("RE: " & If(Not Equals(eventSummary, Nothing), eventSummary, "Meeting").Trim() & " — " & text2)
                    Dim text4 = Uri.EscapeDataString("PARTSTAT=" & text2)
                    Try
                        Process.Start("mailto:" & text & "?subject=" & text3 & "&body=" & text4)
                        result = True
                    Catch projectError As Exception
                        ProjectData.SetProjectError(projectError)
                        result = False
                        Call ProjectData.ClearProjectError()
                    End Try
                End If
            End If
            Return result
        End Function

        Private Shared Function NormalizeOrganizerSmtp(organizerLine As String) As String
            Dim text As String = If(Not Equals(organizerLine, Nothing), organizerLine, "").Trim()
            If text.Length = 0 Then
                Return ""
            End If
            Dim num = text.IndexOf("mailto:", StringComparison.OrdinalIgnoreCase)
            If num >= 0 Then
                text = text.Substring(num + 7)
            End If
            Dim num2 = text.IndexOf("@"c)
            If num2 <= 0 Then
                Return ""
            End If
            Dim num3 = text.IndexOf(";"c)
            If num3 > 0 Then
                text = text.Substring(0, num3)
            End If
            Return text.Trim()
        End Function
    End Class
