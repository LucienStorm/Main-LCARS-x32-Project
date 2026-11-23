' LCARSmain/LCARSmain/Modules/Hearth/modHearthJson.vb
Option Strict On
Option Explicit On
Imports System.Collections.Generic
Imports System.Text
''' <summary>Minimal JSON helpers
for Hearth enroll/voice (no System.Web.Extensions dependency).</summary> Module modHearthJson
Public Function Escape(ByVal s As String) As String
If s Is Nothing Then Return ""
Dim sb As New StringBuilder(s.Length + 8)
For Each ch As Char In s
Select Case ch
Case """"c : sb.Append("\""")
Case "\"c : sb.Append("\\")
Case ControlChars.Cr : sb.Append("\r")
Case ControlChars.Lf : sb.Append("\n")
Case ControlChars.Tab : sb.Append("\t")
Case
Else If AscW(ch) < 32 Then sb.Append("\u" & AscW(ch).ToString("x4"))
Else sb.Append(ch)
End If
End Select
Next
Return sb.ToString()
End Function
Public Function Quote(ByVal s As String) As String
Return """" & Escape(s) & """"
End Function
Public Function GetString(ByVal json As String, ByVal key As String) As String
If String.IsNullOrEmpty(json) OrElse String.IsNullOrEmpty(key) Then Return Nothing
Dim needle As String = """" & key & """"
Dim i As Integer = json.IndexOf(needle, StringComparison.OrdinalIgnoreCase)
If i < 0 Then Return Nothing i = json.IndexOf(":"c, i + needle.Length)
If i < 0 Then Return Nothing i += 1
While i < json.Length AndAlso Char.IsWhiteSpace(json(i)) i += 1
End While
If i >= json.Length Then Return Nothing
If json(i) = "n"c OrElse json(i) = "N"c Then If json.Substring(i).StartsWith("null", StringComparison.OrdinalIgnoreCase) Then Return Nothing
End If
If json(i) <> """"c Then ' bare token (true/false/number) — read until comma/brace
Dim j As Integer = i
While j < json.Length AndAlso json(j) <> ","c AndAlso json(j) <> "}"c AndAlso json(j) <> "]"c j += 1
End While
Return json.Substring(i, j - i).Trim()
End If i += 1
Dim sb As New StringBuilder()
While i < json.Length
Dim c As Char = json(i)
If c = """"c Then Exit
While
If c = "\"c AndAlso i + 1 < json.Length Then i += 1
Dim esc As Char = json(i)
Select Case esc
Case """"c, "\"c, "/"c : sb.Append(esc)
Case "n"c : sb.Append(ControlChars.Lf)
Case "r"c : sb.Append(ControlChars.Cr)
Case "t"c : sb.Append(ControlChars.Tab)
Case
Else : sb.Append(esc)
End Select
Else sb.Append(c)
End If i += 1
End While
Return sb.ToString()
End Function
Public Function GetBool(ByVal json As String, ByVal key As String, ByVal defaultValue As Boolean) As Boolean
Dim raw As String = GetString(json, key)
If raw Is Nothing Then Return defaultValue
If raw.Equals("true", StringComparison.OrdinalIgnoreCase) Then Return True
If raw.Equals("false", StringComparison.OrdinalIgnoreCase) Then Return False
Return defaultValue
End Function
Public Function ExtractObject(ByVal json As String, ByVal key As String) As String
If String.IsNullOrEmpty(json) Then Return Nothing
Dim needle As String = """" & key & """"
Dim i As Integer = json.IndexOf(needle, StringComparison.OrdinalIgnoreCase)
If i < 0 Then Return Nothing i = json.IndexOf(":"c, i + needle.Length)
If i < 0 Then Return Nothing i += 1
While i < json.Length AndAlso Char.IsWhiteSpace(json(i)) i += 1
End While
If i >= json.Length Then Return Nothing
If json(i) = "n"c Then Return Nothing
If json(i) <> "{"c Then Return Nothing
Dim depth As Integer = 0
Dim start As Integer = i
While i < json.Length
Dim c As Char = json(i)
If c = "{"c Then depth += 1
If c = "}"c Then depth -= 1
If depth = 0 Then Return json.Substring(start, i - start + 1)
End If ' skip strings
If c = """"c Then i += 1
While i < json.Length
If json(i) = "\"c Then i += 2
Continue While
End If
If json(i) = """"c Then Exit
While i += 1
End While
End If i += 1
End While
Return Nothing
End Function
End Module
