' Lcars Web Browser/Canvas/CanvasMarkdownPainter.vb
' Lightweight GDI+ Markdown renderer for idle canvas note cards.
Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Drawing
Imports System.Text.RegularExpressions

''' <summary>
''' Draws a Markdown subset into a card rectangle (headers, lists, emphasis, code, links).
''' Edit mode still uses the plain-source RichTextBox; this is view-only.
''' </summary>
Public NotInheritable Class CanvasMarkdownPainter
    Private Sub New()
    End Sub

    Private Enum InlineKind
        Plain = 0
        Bold = 1
        Italic = 2
        Code = 3
        Link = 4
    End Enum

    Private Structure InlineRun
        Public Text As String
        Public Kind As InlineKind
    End Structure

    Public Shared Sub Draw(ByVal g As Graphics,
                           ByVal markdown As String,
                           ByVal bounds As RectangleF,
                           ByVal zoom As Single,
                           ByVal textColor As Color,
                           ByVal accentColor As Color)
        If g Is Nothing OrElse bounds.Width < 8 OrElse bounds.Height < 8 Then Return
        If String.IsNullOrWhiteSpace(markdown) Then Return

        Dim z As Single = Math.Max(0.55F, zoom)
        Dim y As Single = bounds.Y
        Dim maxY As Single = bounds.Bottom
        Dim bodySize As Single = Math.Max(8.0F, 10.0F * z)
        Dim codeSize As Single = Math.Max(7.5F, 9.0F * z)

        Using bodyFont As New Font("Segoe UI", bodySize, FontStyle.Regular)
            Using boldFont As New Font("Segoe UI", bodySize, FontStyle.Bold)
                Using italicFont As New Font("Segoe UI", bodySize, FontStyle.Italic)
                    Using codeFont As New Font("Consolas", codeSize, FontStyle.Regular)
                        Using textBrush As New SolidBrush(textColor)
                            Using accentBrush As New SolidBrush(accentColor)
                                Using mutedBrush As New SolidBrush(Color.FromArgb(200, accentColor.R, accentColor.G, accentColor.B))
                                    Dim normalized As String = markdown.Replace(vbCrLf, vbLf).Replace(vbCr, vbLf)
                                    Dim fenceParts() As String = Regex.Split(normalized, "```")
                                    For partIndex As Integer = 0 To fenceParts.Length - 1
                                        If y >= maxY Then Exit For
                                        Dim segment As String = fenceParts(partIndex)
                                        If partIndex Mod 2 = 1 Then
                                            y = DrawCodeBlock(g, segment, bounds, y, maxY, codeFont, mutedBrush, textBrush)
                                        Else
                                            y = DrawBlocks(g, segment, bounds, y, maxY, z,
                                                           bodyFont, boldFont, italicFont, codeFont,
                                                           textBrush, accentBrush)
                                        End If
                                    Next
                                End Using
                            End Using
                        End Using
                    End Using
                End Using
            End Using
        End Using
    End Sub

    Private Shared Function DrawBlocks(ByVal g As Graphics,
                                       ByVal text As String,
                                       ByVal bounds As RectangleF,
                                       ByVal y As Single,
                                       ByVal maxY As Single,
                                       ByVal z As Single,
                                       ByVal bodyFont As Font,
                                       ByVal boldFont As Font,
                                       ByVal italicFont As Font,
                                       ByVal codeFont As Font,
                                       ByVal textBrush As Brush,
                                       ByVal accentBrush As Brush) As Single
        If String.IsNullOrEmpty(text) Then Return y
        Dim lines() As String = text.Split(ControlChars.Lf)
        Dim para As New List(Of String)()

        Dim flushPara As Action = Sub()
                                      If para.Count = 0 OrElse y >= maxY Then
                                          para.Clear()
                                          Return
                                      End If
                                      Dim joined As String = String.Join(" ", para)
                                      y = DrawWrappedInlines(g, ParseInlines(joined), bounds.X, bounds.Width, y, maxY,
                                                             bodyFont, boldFont, italicFont, codeFont,
                                                             textBrush, accentBrush, 0.0F)
                                      y += 4.0F * z
                                      para.Clear()
                                  End Sub

        For Each raw As String In lines
            If y >= maxY Then Exit For
            Dim line As String = If(raw, "")

            If String.IsNullOrWhiteSpace(line) Then
                flushPara()
                y += 3.0F * z
                Continue For
            End If

            Dim headerMatch As Match = Regex.Match(line, "^(#{1,6})\s+(.+)$")
            If headerMatch.Success Then
                flushPara()
                Dim level As Integer = headerMatch.Groups(1).Value.Length
                Dim scale As Single = Math.Max(1.05F, 1.55F - (level - 1) * 0.12F)
                Dim hSize As Single = Math.Max(9.0F, bodyFont.Size * scale)
                Using hFont As New Font("Segoe UI", hSize, FontStyle.Bold)
                    y = DrawWrappedInlines(g, ParseInlines(headerMatch.Groups(2).Value), bounds.X, bounds.Width, y, maxY,
                                           hFont, hFont, hFont, codeFont,
                                           accentBrush, accentBrush, 0.0F)
                    y += 5.0F * z
                End Using
                Continue For
            End If

            Dim ulMatch As Match = Regex.Match(line, "^[\-\*]\s+(.+)$")
            If ulMatch.Success Then
                flushPara()
                Dim bulletX As Single = bounds.X
                Dim contentX As Single = bounds.X + 14.0F * z
                Dim contentW As Single = Math.Max(8.0F, bounds.Width - 14.0F * z)
                Dim lineH As Single = bodyFont.GetHeight(g)
                If y + lineH <= maxY Then
                    g.DrawString("•", bodyFont, accentBrush, bulletX, y)
                End If
                y = DrawWrappedInlines(g, ParseInlines(ulMatch.Groups(1).Value), contentX, contentW, y, maxY,
                                       bodyFont, boldFont, italicFont, codeFont,
                                       textBrush, accentBrush, 0.0F)
                y += 2.0F * z
                Continue For
            End If

            Dim olMatch As Match = Regex.Match(line, "^(\d+)\.\s+(.+)$")
            If olMatch.Success Then
                flushPara()
                Dim marker As String = olMatch.Groups(1).Value & "."
                Dim contentX As Single = bounds.X + 18.0F * z
                Dim contentW As Single = Math.Max(8.0F, bounds.Width - 18.0F * z)
                Dim lineH As Single = bodyFont.GetHeight(g)
                If y + lineH <= maxY Then
                    g.DrawString(marker, bodyFont, accentBrush, bounds.X, y)
                End If
                y = DrawWrappedInlines(g, ParseInlines(olMatch.Groups(2).Value), contentX, contentW, y, maxY,
                                       bodyFont, boldFont, italicFont, codeFont,
                                       textBrush, accentBrush, 0.0F)
                y += 2.0F * z
                Continue For
            End If

            Dim quoteMatch As Match = Regex.Match(line, "^>\s?(.*)$")
            If quoteMatch.Success Then
                flushPara()
                Dim barW As Single = 3.0F * z
                Using barBrush As New SolidBrush(Color.FromArgb(180, 255, 153, 0))
                    g.FillRectangle(barBrush, bounds.X, y, barW, bodyFont.GetHeight(g))
                End Using
                Dim qx As Single = bounds.X + barW + 6.0F * z
                Dim qw As Single = Math.Max(8.0F, bounds.Width - barW - 6.0F * z)
                y = DrawWrappedInlines(g, ParseInlines(quoteMatch.Groups(1).Value), qx, qw, y, maxY,
                                       italicFont, boldFont, italicFont, codeFont,
                                       textBrush, accentBrush, 0.0F)
                y += 3.0F * z
                Continue For
            End If

            para.Add(line.Trim())
        Next

        flushPara()
        Return y
    End Function

    Private Shared Function DrawCodeBlock(ByVal g As Graphics,
                                          ByVal segment As String,
                                          ByVal bounds As RectangleF,
                                          ByVal y As Single,
                                          ByVal maxY As Single,
                                          ByVal codeFont As Font,
                                          ByVal mutedBrush As Brush,
                                          ByVal textBrush As Brush) As Single
        Dim code As String = segment
        Dim nl As Integer = code.IndexOf(ControlChars.Lf)
        If nl >= 0 Then code = code.Substring(nl + 1)
        code = code.TrimEnd(ControlChars.Cr, ControlChars.Lf)
        If String.IsNullOrEmpty(code) Then Return y

        Dim lineH As Single = codeFont.GetHeight(g)
        Dim lines() As String = code.Replace(vbCrLf, vbLf).Replace(vbCr, vbLf).Split(ControlChars.Lf)
        Dim blockH As Single = Math.Min(maxY - y, lineH * lines.Length + 8.0F)
        If blockH < lineH Then Return maxY

        Using bg As New SolidBrush(Color.FromArgb(90, 0, 0, 0))
            g.FillRectangle(bg, bounds.X, y, bounds.Width, blockH)
        End Using
        y += 4.0F
        For Each ln As String In lines
            If y + lineH > maxY Then Exit For
            Dim clipped As String = ln
            ' Simple clip: measure and truncate with ellipsis if needed.
            While clipped.Length > 0 AndAlso g.MeasureString(clipped, codeFont).Width > bounds.Width - 8.0F
                If clipped.Length <= 1 Then Exit While
                clipped = clipped.Substring(0, clipped.Length - 1)
            End While
            g.DrawString(clipped, codeFont, mutedBrush, bounds.X + 4.0F, y)
            y += lineH
        Next
        y += 4.0F
        Return y
    End Function

    Private Shared Function DrawWrappedInlines(ByVal g As Graphics,
                                               ByVal runs As List(Of InlineRun),
                                               ByVal x As Single,
                                               ByVal width As Single,
                                               ByVal y As Single,
                                               ByVal maxY As Single,
                                               ByVal bodyFont As Font,
                                               ByVal boldFont As Font,
                                               ByVal italicFont As Font,
                                               ByVal codeFont As Font,
                                               ByVal textBrush As Brush,
                                               ByVal accentBrush As Brush,
                                               ByVal indent As Single) As Single
        If runs Is Nothing OrElse runs.Count = 0 Then Return y
        Dim left As Single = x + indent
        Dim maxW As Single = Math.Max(8.0F, width - indent)
        Dim cx As Single = left
        Dim lineH As Single = bodyFont.GetHeight(g)

        For Each run As InlineRun In runs
            Dim font As Font = FontFor(run.Kind, bodyFont, boldFont, italicFont, codeFont)
            Dim brush As Brush = If(run.Kind = InlineKind.Link OrElse run.Kind = InlineKind.Code, accentBrush, textBrush)
            Dim remaining As String = If(run.Text, "")
            While remaining.Length > 0 AndAlso y < maxY
                Dim fit As Integer = FitChars(g, remaining, font, maxW - (cx - left))
                If fit <= 0 Then
                    ' No room on this line — wrap.
                    If cx > left Then
                        y += lineH
                        cx = left
                        If y >= maxY Then Exit For
                        fit = FitChars(g, remaining, font, maxW)
                    End If
                    If fit <= 0 Then
                        ' Single glyph wider than line — force one char.
                        fit = 1
                    End If
                End If

                Dim chunk As String = remaining.Substring(0, fit)
                ' Prefer breaking at space when wrapping mid-run.
                If fit < remaining.Length Then
                    Dim spaceAt As Integer = chunk.LastIndexOf(" "c)
                    If spaceAt > 0 Then
                        fit = spaceAt + 1
                        chunk = remaining.Substring(0, fit)
                    End If
                End If

                If y + lineH > maxY Then Return maxY
                g.DrawString(chunk, font, brush, cx, y)
                Dim w As Single = g.MeasureString(chunk, font).Width
                cx += w
                remaining = remaining.Substring(fit)
                If remaining.Length > 0 Then
                    y += lineH
                    cx = left
                End If
            End While
        Next

        Return y + lineH
    End Function

    Private Shared Function FontFor(ByVal kind As InlineKind, ByVal body As Font, ByVal bold As Font, ByVal italic As Font, ByVal code As Font) As Font
        Select Case kind
            Case InlineKind.Bold
                Return bold
            Case InlineKind.Italic
                Return italic
            Case InlineKind.Code
                Return code
            Case Else
                Return body
        End Select
    End Function

    Private Shared Function FitChars(ByVal g As Graphics, ByVal text As String, ByVal font As Font, ByVal maxWidth As Single) As Integer
        If String.IsNullOrEmpty(text) OrElse maxWidth <= 2.0F Then Return 0
        If g.MeasureString(text, font).Width <= maxWidth Then Return text.Length

        Dim lo As Integer = 1
        Dim hi As Integer = text.Length
        Dim best As Integer = 0
        While lo <= hi
            Dim mid As Integer = (lo + hi) \ 2
            Dim w As Single = g.MeasureString(text.Substring(0, mid), font).Width
            If w <= maxWidth Then
                best = mid
                lo = mid + 1
            Else
                hi = mid - 1
            End If
        End While
        Return best
    End Function

    Private Shared Function ParseInlines(ByVal text As String) As List(Of InlineRun)
        Dim result As New List(Of InlineRun)()
        If String.IsNullOrEmpty(text) Then Return result

        ' Order: links, code, bold, italic.
        Dim pattern As String =
            "\[([^\]]+)\]\(([^)]+)\)|`([^`]+)`|\*\*([^*]+)\*\*|__([^_]+)__|\*([^*]+)\*|(?<!\w)_([^_]+)_(?!\w)"
        Dim last As Integer = 0
        For Each m As Match In Regex.Matches(text, pattern)
            If m.Index > last Then
                result.Add(New InlineRun With {.Text = text.Substring(last, m.Index - last), .Kind = InlineKind.Plain})
            End If
            If m.Groups(1).Success Then
                result.Add(New InlineRun With {.Text = m.Groups(1).Value, .Kind = InlineKind.Link})
            ElseIf m.Groups(3).Success Then
                result.Add(New InlineRun With {.Text = m.Groups(3).Value, .Kind = InlineKind.Code})
            ElseIf m.Groups(4).Success Then
                result.Add(New InlineRun With {.Text = m.Groups(4).Value, .Kind = InlineKind.Bold})
            ElseIf m.Groups(5).Success Then
                result.Add(New InlineRun With {.Text = m.Groups(5).Value, .Kind = InlineKind.Bold})
            ElseIf m.Groups(6).Success Then
                result.Add(New InlineRun With {.Text = m.Groups(6).Value, .Kind = InlineKind.Italic})
            ElseIf m.Groups(7).Success Then
                result.Add(New InlineRun With {.Text = m.Groups(7).Value, .Kind = InlineKind.Italic})
            End If
            last = m.Index + m.Length
        Next
        If last < text.Length Then
            result.Add(New InlineRun With {.Text = text.Substring(last), .Kind = InlineKind.Plain})
        End If
        If result.Count = 0 Then
            result.Add(New InlineRun With {.Text = text, .Kind = InlineKind.Plain})
        End If
        Return result
    End Function
End Class
