' Lcars Web Browser/Documents/TypingAids.vb
Option Strict On
Option Explicit On

Imports System.Windows.Forms

''' <summary>
''' Tablet-oriented typing helpers for the document editor.
''' </summary>
Public Module TypingAids

    ''' <summary>
    ''' Auto-capitalizes the next character after sentence boundaries when enabled.
    ''' Returns True if the key was handled (replaced with uppercase).
    ''' </summary>
    Public Function TryAutoCapitalize(ByVal box As RichTextBox, ByVal e As KeyPressEventArgs, ByVal enabled As Boolean) As Boolean
        If Not enabled OrElse box Is Nothing OrElse e Is Nothing Then Return False
        If Char.IsControl(e.KeyChar) Then Return False
        If Not Char.IsLetter(e.KeyChar) Then Return False
        If Char.IsUpper(e.KeyChar) Then Return False

        Dim shouldCap As Boolean = False
        Dim pos As Integer = box.SelectionStart
        If pos <= 0 Then
            shouldCap = True
        Else
            Dim before As String = box.Text.Substring(0, Math.Min(pos, box.Text.Length))
            Dim i As Integer = before.Length - 1
            While i >= 0 AndAlso (before(i) = " "c OrElse before(i) = ControlChars.Tab OrElse before(i) = ControlChars.Lf OrElse before(i) = ControlChars.Cr)
                i -= 1
            End While
            If i < 0 Then
                shouldCap = True
            Else
                Dim c As Char = before(i)
                If c = "."c OrElse c = "!"c OrElse c = "?"c OrElse c = ControlChars.Lf OrElse c = ControlChars.Cr Then
                    shouldCap = True
                End If
            End If
        End If

        If Not shouldCap Then Return False
        e.KeyChar = Char.ToUpperInvariant(e.KeyChar)
        Return False ' still insert the modified char via default handling
    End Function
End Module
