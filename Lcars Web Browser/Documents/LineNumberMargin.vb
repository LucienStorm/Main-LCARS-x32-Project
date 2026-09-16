' Lcars Web Browser/Documents/LineNumberMargin.vb
Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Paints a line-number gutter beside a RichTextBox.
''' </summary>
Public Class LineNumberMargin
    Inherits Control

    Private _editor As RichTextBox

    Public Sub New()
        SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or ControlStyles.OptimizedDoubleBuffer, True)
        BackColor = Color.FromArgb(20, 20, 20)
        ForeColor = Color.FromArgb(255, 153, 0)
        Width = 44
        Dock = DockStyle.Left
    End Sub

    Public Property Editor As RichTextBox
        Get
            Return _editor
        End Get
        Set(ByVal value As RichTextBox)
            If _editor IsNot Nothing Then
                RemoveHandler _editor.TextChanged, AddressOf OnEditorChanged
                RemoveHandler _editor.VScroll, AddressOf OnEditorChanged
                RemoveHandler _editor.FontChanged, AddressOf OnEditorChanged
                RemoveHandler _editor.Resize, AddressOf OnEditorChanged
            End If
            _editor = value
            If _editor IsNot Nothing Then
                AddHandler _editor.TextChanged, AddressOf OnEditorChanged
                AddHandler _editor.VScroll, AddressOf OnEditorChanged
                AddHandler _editor.FontChanged, AddressOf OnEditorChanged
                AddHandler _editor.Resize, AddressOf OnEditorChanged
            End If
            Invalidate()
        End Set
    End Property

    Private Sub OnEditorChanged(ByVal sender As Object, ByVal e As EventArgs)
        Invalidate()
    End Sub

    Protected Overrides Sub OnPaint(ByVal e As PaintEventArgs)
        MyBase.OnPaint(e)
        If _editor Is Nothing Then Return

        e.Graphics.Clear(BackColor)
        Using font As New Font(_editor.Font.FontFamily, _editor.Font.Size, FontStyle.Regular)
            Dim lineHeight As Integer = TextRenderer.MeasureText("Ag", font).Height
            If lineHeight < 1 Then lineHeight = CInt(Math.Ceiling(font.GetHeight()))

            Dim firstIndex As Integer = _editor.GetCharIndexFromPosition(New Point(0, 0))
            Dim firstLine As Integer = _editor.GetLineFromCharIndex(firstIndex)
            Dim y As Integer = 0
            Dim line As Integer = firstLine
            Dim maxY As Integer = ClientSize.Height

            While y < maxY AndAlso line < Math.Max(1, _editor.Lines.Length)
                Dim label As String = (line + 1).ToString()
                Dim size As Size = TextRenderer.MeasureText(label, font)
                TextRenderer.DrawText(e.Graphics, label, font, New Point(Width - size.Width - 4, y), ForeColor)
                y += lineHeight
                line += 1
                If line >= _editor.Lines.Length AndAlso y < maxY Then
                    ' trailing empty visual line
                    Exit While
                End If
            End While
        End Using
    End Sub
End Class
