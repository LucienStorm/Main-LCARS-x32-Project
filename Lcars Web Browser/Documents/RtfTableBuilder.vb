' Lcars Web Browser/Documents/RtfTableBuilder.vb
Option Strict On
Option Explicit On

Imports System.Text

''' <summary>
''' Builds RTF table fragments for RichTextBox insertion.
''' </summary>
Public Module RtfTableBuilder

    ''' <summary>
    ''' Returns an RTF snippet for a simple bordered table (twips).
    ''' </summary>
    Public Function BuildTable(ByVal rows As Integer, ByVal cols As Integer, Optional ByVal cellWidthTwips As Integer = 1800) As String
        If rows < 1 Then rows = 1
        If cols < 1 Then cols = 1
        If rows > 20 Then rows = 20
        If cols > 10 Then cols = 10

        Dim sb As New StringBuilder()
        sb.Append("{\rtf1\ansi\deff0")
        For r As Integer = 0 To rows - 1
            sb.Append("\trowd\trgaph108\trleft0")
            For c As Integer = 1 To cols
                sb.Append("\cellx").Append((c * cellWidthTwips).ToString())
            Next
            For c As Integer = 0 To cols - 1
                sb.Append("\intbl ")
                sb.Append("R").Append((r + 1).ToString()).Append("C").Append((c + 1).ToString())
                sb.Append("\cell")
            Next
            sb.Append("\row")
        Next
        sb.Append("\pard\par}")
        Return sb.ToString()
    End Function
End Module
