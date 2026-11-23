Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Globalization
Imports Microsoft.VisualBasic.CompilerServices

    <StandardModule>
    Public NotInheritable Class CalcEngine
        Public Enum AngleMode
            Degrees
            Radians
        End Enum

        Public Class EvalResult
            Public Ok As Boolean

            Public Value As Double

            Public ErrorMessage As String

            Public Sub New()
                ErrorMessage = ""
            End Sub
        End Class

        Private Class Tok
            Public Kind As String

            Public Text As String

            Public Num As Double

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Public Shared Function Evaluate(expression As String, mode As AngleMode, Optional x As Double = Double.NaN) As EvalResult
            Dim evalResult As EvalResult = New EvalResult()
            Dim result As EvalResult
            Try
                Dim list = Tokenize(If(Not Equals(expression, Nothing), expression, ""))
                If list.Count = 0 Then
                    evalResult.ErrorMessage = "empty"
                    result = evalResult
                Else
                    Dim pos = 0
                    Dim num = ParseExpr(list, pos, mode, x)
                    If pos < list.Count Then
                        evalResult.ErrorMessage = "syntax"
                        result = evalResult
                    ElseIf Double.IsNaN(num) OrElse Double.IsInfinity(num) Then
                        evalResult.ErrorMessage = "domain"
                        result = evalResult
                    Else
                        evalResult.Ok = True
                        evalResult.Value = num
                        result = evalResult
                    End If
                End If
            Catch ex As Exception
                ProjectData.SetProjectError(ex)
                Dim ex2 = ex
                evalResult.ErrorMessage = If(String.IsNullOrEmpty(ex2.Message), "error", ex2.Message)
                result = evalResult
                Call ProjectData.ClearProjectError()
            End Try
            Return result
        End Function

        Public Shared Function FormatNumber(value As Double) As String
            If Double.IsNaN(value) OrElse Double.IsInfinity(value) Then
                Return "ERROR"
            End If
            Dim num = Math.Abs(value)
            If num <> 0.0 AndAlso (num >= 10000000000.0 OrElse num < 1E-10) Then
                Return value.ToString("0.########E+0", CultureInfo.InvariantCulture)
            End If
            Return value.ToString("0.############", CultureInfo.InvariantCulture)
        End Function

        Private Shared Function Tokenize(src As String) As List(Of Tok)
            Dim list As List(Of Tok) = New List(Of Tok)()
            Dim i = 0
            Dim text As String = src.Trim().ToLowerInvariant().Replace(" ", "")
            text = text.Replace("×", "*").Replace("÷", "/").Replace("π", "pi")
            Dim result As Double = Nothing
            While i < text.Length
                Dim c = text(i)
                If Char.IsDigit(c) OrElse c = "."c Then
                    Dim num = i
                    i += 1

                    While i < text.Length AndAlso (Char.IsDigit(text(i)) OrElse text(i) = "."c)
                        i += 1
                    End While
                    If i < text.Length AndAlso (text(i) = "e"c OrElse text(i) = "E"c) Then
                        Dim num2 = i
                        i += 1
                        If i < text.Length AndAlso (text(i) = "+"c OrElse text(i) = "-"c) Then
                            i += 1
                        End If
                        Dim flag = False
                        While i < text.Length AndAlso Char.IsDigit(text(i))
                            flag = True
                            i += 1
                        End While
                        If Not flag Then
                            i = num2
                        End If
                    End If
                    Dim text2 = text.Substring(num, i - num)

                    If Not Double.TryParse(text2, NumberStyles.Float, CultureInfo.InvariantCulture, result) Then
                        Throw New Exception("bad number")
                    End If
                    Dim tok As Tok = New Tok()
                    tok.Kind = "num"
                    tok.Text = text2
                    tok.Num = result
                    list.Add(tok)
                    Continue While
                End If
                If Not Char.IsLetter(c) Then
                    Select Case c
                        Case "_"c
                        Case "("c
                            list.Add(MakeTok("lp", "("))
                            i += 1
                            Continue While
                        Case ")"c
                            list.Add(MakeTok("rp", ")"))
                            i += 1
                            Continue While
                        Case ","c
                            list.Add(MakeTok("comma", ","))
                            i += 1
                            Continue While
                        Case Else
                            If "^*/+-".IndexOf(c) >= 0 Then
                                list.Add(MakeTok("op", c.ToString()))
                                i += 1
                                Continue While
                            End If
                            Throw New Exception("bad char")
                    End Select
                End If
                Dim num3 = i
                i += 1

                While i < text.Length AndAlso (Char.IsLetterOrDigit(text(i)) OrElse text(i) = "_"c)
                    i += 1
                End While
                Dim text3 = text.Substring(num3, i - num3)
                Dim tok2 As Tok = New Tok()
                tok2.Kind = "id"
                tok2.Text = text3
                list.Add(tok2)
            End While
            Return list
        End Function

        Private Shared Function MakeTok(kind As String, text As String) As Tok
            Dim tok As Tok = New Tok()
            tok.Kind = kind
            tok.Text = text
            Return tok
        End Function

        Private Shared Function ParseExpr(tokens As List(Of Tok), ByRef pos As Integer, mode As AngleMode, x As Double) As Double
            Dim num = ParseTerm(tokens, pos, mode, x)
            While pos < tokens.Count AndAlso Operators.CompareString(tokens(pos).Kind, "op", TextCompare:=False) = 0 AndAlso (Operators.CompareString(tokens(pos).Text, "+", TextCompare:=False) = 0 OrElse Operators.CompareString(tokens(pos).Text, "-", TextCompare:=False) = 0)
                Dim text = tokens(pos).Text
                pos += 1
                Dim num2 = ParseTerm(tokens, pos, mode, x)
                num = If(Operators.CompareString(text, "+", TextCompare:=False) <> 0, num - num2, num + num2)
            End While
            Return num
        End Function

        Private Shared Function ParseTerm(tokens As List(Of Tok), ByRef pos As Integer, mode As AngleMode, x As Double) As Double
            Dim num = ParsePower(tokens, pos, mode, x)
            While pos < tokens.Count AndAlso Operators.CompareString(tokens(pos).Kind, "op", TextCompare:=False) = 0 AndAlso (Operators.CompareString(tokens(pos).Text, "*", TextCompare:=False) = 0 OrElse Operators.CompareString(tokens(pos).Text, "/", TextCompare:=False) = 0)
                Dim text = tokens(pos).Text
                pos += 1
                Dim num2 = ParsePower(tokens, pos, mode, x)
                If Operators.CompareString(text, "*", TextCompare:=False) = 0 Then
                    num *= num2
                    Continue While
                End If
                If num2 = 0.0 Then
                    Throw New Exception("div0")
                End If
                num /= num2
            End While
            Return num
        End Function

        Private Shared Function ParsePower(tokens As List(Of Tok), ByRef pos As Integer, mode As AngleMode, x As Double) As Double
            Dim num = ParseUnary(tokens, pos, mode, x)
            If pos < tokens.Count AndAlso Operators.CompareString(tokens(pos).Kind, "op", TextCompare:=False) = 0 AndAlso Operators.CompareString(tokens(pos).Text, "^", TextCompare:=False) = 0 Then
                pos += 1
                Dim y = ParsePower(tokens, pos, mode, x)
                num = Math.Pow(num, y)
            End If
            Return num
        End Function

        Private Shared Function ParseUnary(tokens As List(Of Tok), ByRef pos As Integer, mode As AngleMode, x As Double) As Double
            If pos >= tokens.Count Then
                Throw New Exception("syntax")
            End If
            If Operators.CompareString(tokens(pos).Kind, "op", TextCompare:=False) = 0 AndAlso Operators.CompareString(tokens(pos).Text, "+", TextCompare:=False) = 0 Then
                pos += 1
                Return ParseUnary(tokens, pos, mode, x)
            End If
            If Operators.CompareString(tokens(pos).Kind, "op", TextCompare:=False) = 0 AndAlso Operators.CompareString(tokens(pos).Text, "-", TextCompare:=False) = 0 Then
                pos += 1
                Return 0.0 - ParseUnary(tokens, pos, mode, x)
            End If
            Return ParsePrimary(tokens, pos, mode, x)
        End Function

        Private Shared Function ParsePrimary(tokens As List(Of Tok), ByRef pos As Integer, mode As AngleMode, x As Double) As Double
            If pos >= tokens.Count Then
                Throw New Exception("syntax")
            End If
            Dim tok = tokens(pos)
            If Operators.CompareString(tok.Kind, "num", TextCompare:=False) = 0 Then
                pos += 1
                Return tok.Num
            End If
            If Operators.CompareString(tok.Kind, "lp", TextCompare:=False) = 0 Then
                pos += 1
                Dim result = ParseExpr(tokens, pos, mode, x)
                If pos >= tokens.Count OrElse Operators.CompareString(tokens(pos).Kind, "rp", TextCompare:=False) <> 0 Then
                    Throw New Exception("paren")
                End If
                pos += 1
                Return result
            End If
            If Operators.CompareString(tok.Kind, "id", TextCompare:=False) = 0 Then
                pos += 1
                Dim text = tok.Text
                Select Case text
                    Case "x"
                        If Double.IsNaN(x) Then
                            Throw New Exception("no x")
                        End If
                        Return x
                    Case "pi"
                        Return Math.PI
                    Case "e"
                        Return Math.E
                    Case Else
                        If pos >= tokens.Count OrElse Operators.CompareString(tokens(pos).Kind, "lp", TextCompare:=False) <> 0 Then
                            Throw New Exception("fn")
                        End If
                        pos += 1
                        Dim list As List(Of Double) = New List(Of Double)()
                        If pos < tokens.Count AndAlso Operators.CompareString(tokens(pos).Kind, "rp", TextCompare:=False) <> 0 Then
                            list.Add(ParseExpr(tokens, pos, mode, x))
                            While pos < tokens.Count AndAlso Operators.CompareString(tokens(pos).Kind, "comma", TextCompare:=False) = 0
                                pos += 1
                                list.Add(ParseExpr(tokens, pos, mode, x))
                            End While
                        End If
                        If pos >= tokens.Count OrElse Operators.CompareString(tokens(pos).Kind, "rp", TextCompare:=False) <> 0 Then
                            Throw New Exception("paren")
                        End If
                        pos += 1
                        Return CallFn(text, list, mode)
                End Select
            End If
            Throw New Exception("syntax")
        End Function

        Private Shared Function CallFn(name As String, args As List(Of Double), mode As AngleMode) As Double
            Select Case name
                Case "sin"
                    RequireArgs(args, 1)
                    Return Math.Sin(ToRad(args(0), mode))
                Case "cos"
                    RequireArgs(args, 1)
                    Return Math.Cos(ToRad(args(0), mode))
                Case "tan"
                    RequireArgs(args, 1)
                    Return Math.Tan(ToRad(args(0), mode))
                Case "asin", "arcsin"
                    RequireArgs(args, 1)
                    Return FromRad(Math.Asin(args(0)), mode)
                Case "acos", "arccos"
                    RequireArgs(args, 1)
                    Return FromRad(Math.Acos(args(0)), mode)
                Case "atan", "arctan"
                    RequireArgs(args, 1)
                    Return FromRad(Math.Atan(args(0)), mode)
                Case "sinh"
                    RequireArgs(args, 1)
                    Return Math.Sinh(args(0))
                Case "cosh"
                    RequireArgs(args, 1)
                    Return Math.Cosh(args(0))
                Case "tanh"
                    RequireArgs(args, 1)
                    Return Math.Tanh(args(0))
                Case "ln", "log"
                    RequireArgs(args, 1)
                    If args(0) <= 0.0 Then
                        Throw New Exception("domain")
                    End If
                    Return Math.Log(args(0))
                Case "log10"
                    RequireArgs(args, 1)
                    If args(0) <= 0.0 Then
                        Throw New Exception("domain")
                    End If
                    Return Math.Log10(args(0))
                Case "exp"
                    RequireArgs(args, 1)
                    Return Math.Exp(args(0))
                Case "sqrt"
                    RequireArgs(args, 1)
                    If args(0) < 0.0 Then
                        Throw New Exception("domain")
                    End If
                    Return Math.Sqrt(args(0))
                Case "cbrt"
                    RequireArgs(args, 1)
                    Return Math.Pow(args(0), 1.0 / 3.0)
                Case "abs"
                    RequireArgs(args, 1)
                    Return Math.Abs(args(0))
                Case "floor"
                    RequireArgs(args, 1)
                    Return Math.Floor(args(0))
                Case "ceil", "ceiling"
                    RequireArgs(args, 1)
                    Return Math.Ceiling(args(0))
                Case "round"
                    RequireArgs(args, 1)
                    Return Math.Round(args(0), MidpointRounding.AwayFromZero)
                Case "fact", "factorial"
                    RequireArgs(args, 1)
                    Return Factorial(args(0))
                Case "pow"
                    RequireArgs(args, 2)
                    Return Math.Pow(args(0), args(1))
                Case "npr", "permut"
                    RequireArgs(args, 2)
                    Return Permutation(args(0), args(1))
                Case "ncr", "combin"
                    RequireArgs(args, 2)
                    Return Combination(args(0), args(1))
                Case "min"
                    If args.Count < 1 Then
                        Throw New Exception("args")
                    End If
                    Dim num2 = args(0)
                    For Each arg In args
                        If arg < num2 Then
                            num2 = arg
                        End If
                    Next
                    Return num2
                Case "max"
                    If args.Count < 1 Then
                        Throw New Exception("args")
                    End If
                    Dim num = args(0)
                    For Each arg2 In args
                        If arg2 > num Then
                            num = arg2
                        End If
                    Next
                    Return num
                Case Else
                    Throw New Exception("unknown fn")
            End Select
        End Function

        Private Shared Sub RequireArgs(args As List(Of Double), n As Integer)
            If args Is Nothing OrElse args.Count <> n Then
                Throw New Exception("args")
            End If
        End Sub

        Private Shared Function ToRad(v As Double, mode As AngleMode) As Double
            If mode = AngleMode.Radians Then
                Return v
            End If
            Return v * Math.PI / 180.0
        End Function

        Private Shared Function FromRad(v As Double, mode As AngleMode) As Double
            If mode = AngleMode.Radians Then
                Return v
            End If
            Return v * 180.0 / Math.PI
        End Function

        Private Shared Function Factorial(n As Double) As Double
            If n < 0.0 OrElse n <> Math.Floor(n) OrElse n > 170.0 Then
                Throw New Exception("domain")
            End If
            Dim num = 1.0
            Dim num2 As Integer = Math.Round(n)
            For i = 2 To num2
                num *= i
            Next
            Return num
        End Function

        Private Shared Function Permutation(n As Double, r As Double) As Double
            If n < 0.0 OrElse r < 0.0 OrElse n <> Math.Floor(n) OrElse r <> Math.Floor(r) OrElse r > n Then
                Throw New Exception("domain")
            End If
            Dim num = 1.0
            Dim num2 = CInt(Math.Round(r)) - 1
            For i = 0 To num2
                num *= n - i
            Next
            Return num
        End Function

        Private Shared Function Combination(n As Double, r As Double) As Double
            If n < 0.0 OrElse r < 0.0 OrElse n <> Math.Floor(n) OrElse r <> Math.Floor(r) OrElse r > n Then
                Throw New Exception("domain")
            End If
            r = Math.Min(r, n - r)
            Dim num = 1.0
            Dim num2 As Integer = Math.Round(r)
            For i = 1 To num2
                num = num * (n - r + i) / i
            Next
            Return Math.Round(num)
        End Function
    End Class
