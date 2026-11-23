' LCARSmain/LCARSmain/Modules/CalcEngine.vb
Option Strict On
Option Explicit On
Imports System.Collections.Generic
Imports System.Globalization
Imports System.Text
''' <summary>
''' Expression evaluator
for scientific / graphing calculator (TI-class function set).
''' Supports + - * / ^, parentheses, unary minus, functions, and x
for graphing.
''' </summary>
Public Module CalcEngine
Public Enum AngleMode Degrees = 0 Radians = 1
End Enum
Public Class EvalResult
Public Ok As Boolean
Public Value As Double
Public ErrorMessage As String = ""
End Class
Private Class Tok
Public Kind As String ' num, id, op, lp, rp, comma
Public Text As String
Public Num As Double
End Class
''' <summary>Evaluate expression; optional x
for graphing Y=.</summary>
Public Function Evaluate(ByVal expression As String, ByVal mode As AngleMode, Optional ByVal x As Double = Double.NaN) As EvalResult
Dim r As New EvalResult()
Try
Dim tokens As List(Of Tok) = Tokenize(If(expression, ""))
If tokens.Count = 0 Then r.ErrorMessage = "empty"
Return r
End If
Dim pos As Integer = 0
Dim value As Double = ParseExpr(tokens, pos, mode, x)
If pos < tokens.Count Then r.ErrorMessage = "syntax"
Return r
End If
If Double.IsNaN(value) OrElse Double.IsInfinity(value) Then r.ErrorMessage = "domain"
Return r
End If r.Ok = True r.Value = value
Return r
Catch ex As Exception r.ErrorMessage = If(String.IsNullOrEmpty(ex.Message), "error", ex.Message)
Return r
End Try
End Function
Public Function FormatNumber(ByVal value As Double) As String
If Double.IsNaN(value) OrElse Double.IsInfinity(value) Then Return "ERROR"
Dim abs As Double = Math.Abs(value)
If abs <> 0 AndAlso (abs >= 1.0E+10 OrElse abs < 0.0000000001) Then Return value.ToString("0.########E+0", CultureInfo.InvariantCulture)
End If
Return value.ToString("0.############", CultureInfo.InvariantCulture)
End Function
Private Function Tokenize(ByVal src As String) As List(Of Tok)
Dim list As New List(Of Tok)()
Dim i As Integer = 0
Dim s As String = src.Trim().ToLowerInvariant().Replace(" ", "") s = s.Replace("×", "*").Replace("÷", "/").Replace("π", "pi")
While i < s.Length
Dim ch As Char = s(i)
If Char.IsDigit(ch) OrElse ch = "."c Then Dim start As Integer = i i += 1
While i < s.Length AndAlso (Char.IsDigit(s(i)) OrElse s(i) = "."c) i += 1
End While
If i < s.Length AndAlso (s(i) = "e"c OrElse s(i) = "E"c) Then Dim ePos As Integer = i i += 1
If i < s.Length AndAlso (s(i) = "+"c OrElse s(i) = "-"c) Then i += 1
Dim dig As Boolean = False
While i < s.Length AndAlso Char.IsDigit(s(i)) dig = True i += 1
End While
If Not dig Then i = ePos
End If
Dim numText As String = s.Substring(start, i - start)
Dim n As Double
If Not Double.TryParse(numText, NumberStyles.Float, CultureInfo.InvariantCulture, n) Then Throw New Exception("bad number")
End If
Dim t As New Tok()
t.Kind = "num" t.Text = numText t.Num = n list.Add(t)
ElseIf Char.IsLetter(ch) OrElse ch = "_"c Then Dim start As Integer = i i += 1
While i < s.Length AndAlso (Char.IsLetterOrDigit(s(i)) OrElse s(i) = "_"c) i += 1
End While
Dim id As String = s.Substring(start, i - start)
Dim t As New Tok()
t.Kind = "id" t.Text = id list.Add(t)
ElseIf ch = "("c Then list.Add(MakeTok("lp", "(")) i += 1
ElseIf ch = ")"c Then list.Add(MakeTok("rp", ")")) i += 1
ElseIf ch = ","c Then list.Add(MakeTok("comma", ",")) i += 1
ElseIf "^*/+-".IndexOf(ch) >= 0 Then list.Add(MakeTok("op", ch.ToString())) i += 1
Else Throw New Exception("bad char")
End If
End While
Return list
End Function
Private Function MakeTok(ByVal kind As String, ByVal text As String) As Tok
Dim t As New Tok()
t.Kind = kind t.Text = text
Return t
End Function ' expr = term ((+|-) term)*
Private Function ParseExpr(ByVal tokens As List(Of Tok), ByRef pos As Integer, ByVal mode As AngleMode, ByVal x As Double) As Double
Dim v As Double = ParseTerm(tokens, pos, mode, x)
While pos < tokens.Count AndAlso tokens(pos).Kind = "op" AndAlso (tokens(pos).Text = "+" OrElse tokens(pos).Text = "-")
Dim op As String = tokens(pos).Text pos += 1
Dim r As Double = ParseTerm(tokens, pos, mode, x)
If op = "+" Then v += r
Else v -= r
End While
Return v
End Function ' term = power ((*|/) power)*
Private Function ParseTerm(ByVal tokens As List(Of Tok), ByRef pos As Integer, ByVal mode As AngleMode, ByVal x As Double) As Double
Dim v As Double = ParsePower(tokens, pos, mode, x)
While pos < tokens.Count AndAlso tokens(pos).Kind = "op" AndAlso (tokens(pos).Text = "*" OrElse tokens(pos).Text = "/")
Dim op As String = tokens(pos).Text pos += 1
Dim r As Double = ParsePower(tokens, pos, mode, x)
If op = "*" Then v *= r
Else If r = 0 Then Throw New Exception("div0") v /= r
End If
End While
Return v
End Function ' power = unary (^ unary)* (right-assoc)
Private Function ParsePower(ByVal tokens As List(Of Tok), ByRef pos As Integer, ByVal mode As AngleMode, ByVal x As Double) As Double
Dim v As Double = ParseUnary(tokens, pos, mode, x)
If pos < tokens.Count AndAlso tokens(pos).Kind = "op" AndAlso tokens(pos).Text = "^" Then pos += 1
Dim exp As Double = ParsePower(tokens, pos, mode, x) v = Math.Pow(v, exp)
End If
Return v
End Function
Private Function ParseUnary(ByVal tokens As List(Of Tok), ByRef pos As Integer, ByVal mode As AngleMode, ByVal x As Double) As Double
If pos >= tokens.Count Then Throw New Exception("syntax")
If tokens(pos).Kind = "op" AndAlso tokens(pos).Text = "+" Then pos += 1
Return ParseUnary(tokens, pos, mode, x)
End If
If tokens(pos).Kind = "op" AndAlso tokens(pos).Text = "-" Then pos += 1
Return -ParseUnary(tokens, pos, mode, x)
End If
Return ParsePrimary(tokens, pos, mode, x)
End Function
Private Function ParsePrimary(ByVal tokens As List(Of Tok), ByRef pos As Integer, ByVal mode As AngleMode, ByVal x As Double) As Double
If pos >= tokens.Count Then Throw New Exception("syntax")
Dim t As Tok = tokens(pos)
If t.Kind = "num" Then pos += 1
Return t.Num
End If
If t.Kind = "lp" Then pos += 1
Dim v As Double = ParseExpr(tokens, pos, mode, x)
If pos >= tokens.Count OrElse tokens(pos).Kind <> "rp" Then Throw New Exception("paren") pos += 1
Return v
End If
If t.Kind = "id" Then pos += 1
Dim name As String = t.Text
If name = "x" Then If Double.IsNaN(x) Then Throw New Exception("no x")
Return x
End If
If name = "pi" Then Return Math.PI
If name = "e" Then Return Math.E ' function call
If pos >= tokens.Count OrElse tokens(pos).Kind <> "lp" Then Throw New Exception("fn") pos += 1
Dim args As New List(Of Double)()
If pos < tokens.Count AndAlso tokens(pos).Kind <> "rp" Then args.Add(ParseExpr(tokens, pos, mode, x))
While pos < tokens.Count AndAlso tokens(pos).Kind = "comma" pos += 1 args.Add(ParseExpr(tokens, pos, mode, x))
End While
End If
If pos >= tokens.Count OrElse tokens(pos).Kind <> "rp" Then Throw New Exception("paren") pos += 1
Return CallFn(name, args, mode)
End If
Throw New Exception("syntax")
End Function
Private Function CallFn(ByVal name As String, ByVal args As List(Of Double), ByVal mode As AngleMode) As Double
Select Case name
Case "sin" RequireArgs(args, 1)
Return Math.Sin(ToRad(args(0), mode))
Case "cos" RequireArgs(args, 1)
Return Math.Cos(ToRad(args(0), mode))
Case "tan" RequireArgs(args, 1)
Return Math.Tan(ToRad(args(0), mode))
Case "asin", "arcsin" RequireArgs(args, 1)
Return FromRad(Math.Asin(args(0)), mode)
Case "acos", "arccos" RequireArgs(args, 1)
Return FromRad(Math.Acos(args(0)), mode)
Case "atan", "arctan" RequireArgs(args, 1)
Return FromRad(Math.Atan(args(0)), mode)
Case "sinh" RequireArgs(args, 1)
Return Math.Sinh(args(0))
Case "cosh" RequireArgs(args, 1)
Return Math.Cosh(args(0))
Case "tanh" RequireArgs(args, 1)
Return Math.Tanh(args(0))
Case "ln", "log" RequireArgs(args, 1)
If args(0) <= 0 Then Throw New Exception("domain")
Return Math.Log(args(0))
Case "log10" RequireArgs(args, 1)
If args(0) <= 0 Then Throw New Exception("domain")
Return Math.Log10(args(0))
Case "exp" RequireArgs(args, 1)
Return Math.Exp(args(0))
Case "sqrt" RequireArgs(args, 1)
If args(0) < 0 Then Throw New Exception("domain")
Return Math.Sqrt(args(0))
Case "cbrt" RequireArgs(args, 1)
Return Math.Pow(args(0), 1.0 / 3.0)
Case "abs" RequireArgs(args, 1)
Return Math.Abs(args(0))
Case "floor" RequireArgs(args, 1)
Return Math.Floor(args(0))
Case "ceil", "ceiling" RequireArgs(args, 1)
Return Math.Ceiling(args(0))
Case "round" RequireArgs(args, 1)
Return Math.Round(args(0), MidpointRounding.AwayFromZero)
Case "fact", "factorial" RequireArgs(args, 1)
Return Factorial(args(0))
Case "pow" RequireArgs(args, 2)
Return Math.Pow(args(0), args(1))
Case "npr", "permut" RequireArgs(args, 2)
Return Permutation(args(0), args(1))
Case "ncr", "combin" RequireArgs(args, 2)
Return Combination(args(0), args(1))
Case "min"
If args.Count < 1 Then Throw New Exception("args")
Dim m As Double = args(0)
For Each a As Double In args
If a < m Then m = a
Next
Return m
Case "max"
If args.Count < 1 Then Throw New Exception("args")
Dim m As Double = args(0)
For Each a As Double In args
If a > m Then m = a
Next
Return m
Case
Else Throw New Exception("unknown fn")
End Select
End Function
Private Sub RequireArgs(ByVal args As List(Of Double), ByVal n As Integer)
If args Is Nothing OrElse args.Count <> n Then Throw New Exception("args")
End Sub
Private Function ToRad(ByVal v As Double, ByVal mode As AngleMode) As Double
If mode = AngleMode.Radians Then Return v
Return v * Math.PI / 180.0
End Function
Private Function FromRad(ByVal v As Double, ByVal mode As AngleMode) As Double
If mode = AngleMode.Radians Then Return v
Return v * 180.0 / Math.PI
End Function
Private Function Factorial(ByVal n As Double) As Double
If n < 0 OrElse n <> Math.Floor(n) OrElse n > 170 Then Throw New Exception("domain")
Dim r As Double = 1
For i As Integer = 2 To CInt(n) r *= i
Next
Return r
End Function
Private Function Permutation(ByVal n As Double, ByVal r As Double) As Double
If n < 0 OrElse r < 0 OrElse n <> Math.Floor(n) OrElse r <> Math.Floor(r) OrElse r > n Then Throw New Exception("domain")
Dim result As Double = 1
For i As Integer = 0 To CInt(r) - 1 result *= (n - i)
Next
Return result
End Function
Private Function Combination(ByVal n As Double, ByVal r As Double) As Double
If n < 0 OrElse r < 0 OrElse n <> Math.Floor(n) OrElse r <> Math.Floor(r) OrElse r > n Then Throw New Exception("domain") r = Math.Min(r, n - r)
Dim result As Double = 1
For i As Integer = 1 To CInt(r) result = result * (n - r + i) / i
Next
Return Math.Round(result)
End Function
End Module
