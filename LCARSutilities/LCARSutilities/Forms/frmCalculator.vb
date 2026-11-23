Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Globalization
Imports System.Runtime.CompilerServices
Imports System.Text
Imports System.Windows.Forms
Imports LCARS
Imports LCARS.Controls
Imports Microsoft.VisualBasic.CompilerServices

    Public Class frmCalculator
        Inherits LCARSForm
        Private Enum CalcUiMode
            Basic
            Sci
            Graph
        End Enum

        <CompilerGenerated>
        Friend Class _Closure_0024__1
            Public _0024VB_0024Local_handler As Action(Of String)

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Private Const FormWidth As Integer = 780

        Private Const FormHeight As Integer = 560

        Private Const KeyFontNormal As Single = 16F

        Private Const KeyFontOperator As Single = 22F

        Private Const KeyFontName As String = "Arial Narrow"

        Private Const GraphKeypadWidth As Integer = 300

        Private Const CloseButtonHeight As Integer = 32

        Private currentMode As CalcUiMode

        Private angleMode As CalcEngine.AngleMode

        Private expr As String

        Private ans As Double

        Private memory As Double

        Private layoutBusy As Boolean

        Private titleBar As FlatButton

        Private lblDisplay As Label

        Private lblStatus As Label

        Private pnlKeys As Panel

        Private pnlGraph As Panel

        Private pnlGraphKeys As Panel

        Private picGraph As GraphCanvas

        Private txtY1 As TextBox

        Private txtXmin As TextBox

        Private txtXmax As TextBox

        Private txtYmin As TextBox

        Private txtYmax As TextBox

        Private lblY1 As Label

        Private lblTrace As Label

        Private fbModeBasic As FlatButton

        Private fbModeSci As FlatButton

        Private fbModeGraph As FlatButton

        Private fbDegRad As FlatButton

        Private fbClose As FlatButton

        Private fbPlot As FlatButton

        Private fbZstd As FlatButton

        Private fbZfit As FlatButton

        Private fbTraceLeft As FlatButton

        Private fbTraceRight As FlatButton

        Private fbTable As FlatButton

        Private lblXmin As Label

        Private lblXmax As Label

        Private lblYmin As Label

        Private lblYmax As Label

        Private xmin As Double

        Private xmax As Double

        Private ymin As Double

        Private ymax As Double

        Private y1Expr As String

        Private traceX As Double

        Private graphPoints As List(Of PointF)

        Private graphOk As Boolean

        Public Sub New()
            currentMode = CalcUiMode.Sci
            angleMode = CalcEngine.AngleMode.Degrees
            expr = ""
            ans = 0.0
            memory = 0.0
            layoutBusy = False
            xmin = -10.0
            xmax = 10.0
            ymin = -10.0
            ymax = 10.0
            y1Expr = ""
            traceX = 0.0
            graphPoints = New List(Of PointF)()
            graphOk = False
            modUtilities.InitUtilityFormFloating(Me, 780, 560)
            BuildChrome()
            AddHandler Resize, AddressOf OnFormResize
            AddHandler Shown, AddressOf OnFormShown
            ShowMode(CalcUiMode.Sci)
        End Sub

        Private Sub OnFormShown(sender As Object, e As EventArgs)
            LayoutChrome()
        End Sub

        Private Sub OnFormResize(sender As Object, e As EventArgs)
            LayoutChrome()
        End Sub

        Private Sub BuildChrome()
            Dim num = 12
            titleBar = modUtilities.MakeUtilityTitle(Me, "CALCULATOR", 200)
            fbModeBasic = StyleModeButton(modUtilities.MakeUtilityButton(Me, "BASIC", num, 40, 90, 26, LCARScolorStyles.PrimaryFunction, Sub(s, e) ShowMode(CalcUiMode.Basic)))
            fbModeSci = StyleModeButton(modUtilities.MakeUtilityButton(Me, "SCI", num + 96, 40, 90, 26, LCARScolorStyles.Orange, Sub(s, e) ShowMode(CalcUiMode.Sci)))
            fbModeGraph = StyleModeButton(modUtilities.MakeUtilityButton(Me, "GRAPH", num + 192, 40, 100, 26, LCARScolorStyles.PrimaryFunction, Sub(s, e) ShowMode(CalcUiMode.Graph)))
            fbDegRad = StyleModeButton(modUtilities.MakeUtilityButton(Me, "DEG", num + 300, 40, 70, 26, LCARScolorStyles.MiscFunction, New EventHandler(AddressOf ToggleAngleMode)))
            lblDisplay = modUtilities.MakeUtilityLabel(Me, num, 70, 200, 48, 30F)
            lblDisplay.TextAlign = ContentAlignment.MiddleRight
            lblDisplay.BorderStyle = BorderStyle.FixedSingle
            lblDisplay.Text = "0"
            lblStatus = modUtilities.MakeUtilityLabel(Me, num, 120, 200, 18, 12F)
            lblStatus.TextAlign = ContentAlignment.MiddleLeft
            lblStatus.Text = "DEG  ·  ANS=0  ·  M=0"
            pnlKeys = New Panel()
            pnlKeys.BackColor = Color.Black
            Controls.Add(pnlKeys)
            pnlGraph = New Panel()
            pnlGraph.BackColor = Color.Black
            pnlGraph.Visible = False
            Controls.Add(pnlGraph)
            BuildGraphPanel()
            fbClose = modUtilities.MakeUtilityButton(Me, "CLOSE", num, 100, 200, 32, LCARScolorStyles.FunctionOffline, Sub(s, e) Close())
            ApplyKeyFont(fbClose, "CLOSE")
            KeyPreview = True
            AddHandler KeyDown, AddressOf OnFormKeyDown
            AddHandler KeyPress, AddressOf OnFormKeyPress
        End Sub

        Private Function StyleModeButton(btn As FlatButton) As FlatButton
            ApplyKeyFont(btn, btn.ButtonText)
            Return btn
        End Function

        Private Sub BuildGraphPanel()
            lblY1 = modUtilities.MakeUtilityLabel(pnlGraph, 0, 0, 40, 24, 13F)
            lblY1.Text = "Y1="
            txtY1 = New TextBox()
            txtY1.BackColor = Color.FromArgb(20, 20, 20)
            txtY1.ForeColor = Color.Orange
            txtY1.BorderStyle = BorderStyle.FixedSingle
            txtY1.Font = New Font("Arial Narrow", 14F, FontStyle.Bold)
            txtY1.Text = y1Expr
            pnlGraph.Controls.Add(txtY1)
            fbPlot = ApplyNamed(modUtilities.MakeUtilityButton(pnlGraph, "PLOT", 0, 0, 80, 26, LCARScolorStyles.PrimaryFunction, Sub(s, e) PlotGraph()), "PLOT")
            fbZstd = ApplyNamed(modUtilities.MakeUtilityButton(pnlGraph, "ZSTD", 0, 0, 70, 26, LCARScolorStyles.MiscFunction, Sub(s, e) ZoomStandard()), "ZSTD")
            fbZfit = ApplyNamed(modUtilities.MakeUtilityButton(pnlGraph, "ZFIT", 0, 0, 70, 26, LCARScolorStyles.MiscFunction, Sub(s, e) ZoomFit()), "ZFIT")
            AddWinField("Xmin", xmin, lblXmin, txtXmin)
            AddWinField("Xmax", xmax, lblXmax, txtXmax)
            AddWinField("Ymin", ymin, lblYmin, txtYmin)
            AddWinField("Ymax", ymax, lblYmax, txtYmax)
            picGraph = New GraphCanvas()
            picGraph.BackColor = Color.FromArgb(10, 10, 16)
            picGraph.BorderStyle = BorderStyle.FixedSingle
            picGraph.TabStop = True
            AddHandler picGraph.Paint, AddressOf PicGraph_Paint
            AddHandler picGraph.WindowChanged, AddressOf Me.PicGraph_WindowChanged
            AddHandler picGraph.TraceRequested, AddressOf Me.PicGraph_TraceRequested
            pnlGraph.Controls.Add(picGraph)
            pnlGraphKeys = New Panel()
            pnlGraphKeys.BackColor = Color.Black
            pnlGraph.Controls.Add(pnlGraphKeys)
            lblTrace = modUtilities.MakeUtilityLabel(pnlGraph, 0, 0, 200, 22, 12F)
            lblTrace.Text = "TRACE — tap  ·  drag pan  ·  pinch/wheel zoom"
            fbTraceLeft = ApplyNamed(modUtilities.MakeUtilityButton(pnlGraph, "←", 0, 0, 40, 24, LCARScolorStyles.PrimaryFunction, Sub(s, e) NudgeTrace(-1)), "←")
            fbTraceRight = ApplyNamed(modUtilities.MakeUtilityButton(pnlGraph, "→", 0, 0, 40, 24, LCARScolorStyles.PrimaryFunction, Sub(s, e) NudgeTrace(1)), "→")
            fbTable = ApplyNamed(modUtilities.MakeUtilityButton(pnlGraph, "TABLE", 0, 0, 70, 24, LCARScolorStyles.MiscFunction, Sub(s, e) ShowTable()), "TABLE")
        End Sub

        Private Function ApplyNamed(btn As FlatButton, caption As String) As FlatButton
            ApplyKeyFont(btn, caption)
            Return btn
        End Function

        Private Sub AddWinField(caption As String, value As Double, ByRef lbl As Label, ByRef box As TextBox)
            lbl = modUtilities.MakeUtilityLabel(pnlGraph, 0, 0, 44, 22, 11F)
            lbl.Text = caption
            box = New TextBox()
            box.BackColor = Color.FromArgb(20, 20, 20)
            box.ForeColor = Color.Orange
            box.BorderStyle = BorderStyle.FixedSingle
            box.Font = New Font("Arial Narrow", 12F, FontStyle.Bold)
            box.Text = value.ToString(CultureInfo.InvariantCulture)
            pnlGraph.Controls.Add(box)
        End Sub

        Private Sub LayoutChrome()
            If layoutBusy OrElse Not IsHandleCreated Then
                Return
            End If
            layoutBusy = True
            Try
                Dim num = 12
                Dim num2 = Math.Max(320, ClientSize.Width - num * 2)
                Dim num3 = Math.Max(240, ClientSize.Height)
                Dim location As Point
                If titleBar IsNot Nothing Then
                    Dim flatButton = titleBar
                    location = New Point(num, num)
                    flatButton.Location = location
                    Dim flatButton2 = titleBar
                    Dim size As Size = New Size(num2, 28)
                    flatButton2.Size = size
                End If
                Dim flatButton3 = fbModeBasic
                location = New Point(num, 40)
                flatButton3.Location = location
                Dim flatButton4 = fbModeSci
                location = New Point(num + 96, 40)
                flatButton4.Location = location
                Dim flatButton5 = fbModeGraph
                location = New Point(num + 192, 40)
                flatButton5.Location = location
                Dim flatButton6 = fbDegRad
                location = New Point(num + 300, 40)
                flatButton6.Location = location
                Dim num4 As Integer
                If currentMode = CalcUiMode.Graph Then
                    lblDisplay.Visible = False
                    lblStatus.Visible = False
                    num4 = 72
                Else
                    lblDisplay.Visible = True
                    lblStatus.Visible = True
                    lblDisplay.SetBounds(num, 70, num2, 48)
                    lblStatus.SetBounds(num, 120, num2, 18)
                    num4 = 142
                End If
                fbClose.SetBounds(num, num3 - num - 32, num2, 32)
                Dim num5 = fbClose.Top - 6
                Dim num6 = Math.Max(100, num5 - num4)
                pnlKeys.SetBounds(num, num4, num2, num6)
                pnlGraph.SetBounds(num, num4, num2, num6)
                If currentMode = CalcUiMode.Graph Then
                    LayoutGraphBody()
                ElseIf pnlKeys.Visible Then
                    RebuildKeys()
                End If

            Finally
                layoutBusy = False
            End Try
        End Sub

        Private Sub LayoutGraphBody()
            Dim num = 0
            Dim num2 = pnlGraph.ClientSize.Width
            Dim num3 = pnlGraph.ClientSize.Height
            lblY1.SetBounds(0, num + 2, 40, 24)
            Dim num4 = 236
            txtY1.SetBounds(44, num, Math.Max(120, num2 - 44 - num4 - 12), 26)
            Dim num5 = txtY1.Right + 8
            fbPlot.SetBounds(num5, num, 80, 26)
            fbZstd.SetBounds(num5 + 88, num, 70, 26)
            fbZfit.SetBounds(num5 + 166, num, 70, 26)
            num += 30
            PlaceWinField(lblXmin, txtXmin, 0, num)
            PlaceWinField(lblXmax, txtXmax, 180, num)
            PlaceWinField(lblYmin, txtYmin, 360, num)
            PlaceWinField(lblYmax, txtYmax, 540, num)
            num += 28
            Dim num6 = 26
            Dim num7 = num3 - num - num6 - 2
            If num7 < 80 Then
                num7 = 80
            End If
            Dim num8 = 300
            If num2 < num8 + 200 Then
                num8 = Math.Max(180, num2 / 3)
            End If
            Dim num9 = num2 - num8 - 8
            If num9 < 120 Then
                num8 = Math.Max(140, num2 / 3)
                num9 = Math.Max(100, num2 - num8 - 8)
            End If
            picGraph.SetBounds(0, num, num9, num7)
            pnlGraphKeys.SetBounds(num9 + 8, num, num8, num7)
            num += num7 + 2
            lblTrace.SetBounds(0, num, Math.Max(80, num9 - 180), 22)
            fbTraceLeft.SetBounds(num9 - 170, num - 2, 40, 24)
            fbTraceRight.SetBounds(num9 - 124, num - 2, 40, 24)
            fbTable.SetBounds(num9 - 78, num - 2, 70, 24)
            RebuildGraphKeys()
            picGraph.SetWindow(xmin, xmax, ymin, ymax)
            If Operators.CompareString(y1Expr, "", TextCompare:=False) <> 0 Then
                PlotGraph()
            Else
                picGraph.Invalidate()
            End If
        End Sub

        Private Shared Sub PlaceWinField(lbl As Label, box As TextBox, x As Integer, y As Integer)
            lbl.SetBounds(x, y, 44, 22)
            box.SetBounds(x + 46, y, 80, 22)
        End Sub

        Private Sub ShowMode(mode As CalcUiMode)
            currentMode = mode
            fbModeBasic.Color = If(mode = CalcUiMode.Basic, LCARScolorStyles.Orange, LCARScolorStyles.PrimaryFunction)
            fbModeSci.Color = If(mode = CalcUiMode.Sci, LCARScolorStyles.Orange, LCARScolorStyles.PrimaryFunction)
            fbModeGraph.Color = If(mode = CalcUiMode.Graph, LCARScolorStyles.Orange, LCARScolorStyles.PrimaryFunction)
            fbModeBasic.Invalidate()
            fbModeSci.Invalidate()
            fbModeGraph.Invalidate()
            Dim flag = mode = CalcUiMode.Graph
            pnlGraph.Visible = flag
            pnlKeys.Visible = Not flag
            LayoutChrome()
            If flag Then
                SyncWindowBoxes()
                picGraph.SetWindow(xmin, xmax, ymin, ymax)
                picGraph.Invalidate()
            Else
                RebuildKeys()
                RefreshDisplay()
            End If
        End Sub

        Private Sub ToggleAngleMode(sender As Object, e As EventArgs)
            If angleMode = CalcEngine.AngleMode.Degrees Then
                angleMode = CalcEngine.AngleMode.Radians
                fbDegRad.ButtonText = "RAD"
            Else
                angleMode = CalcEngine.AngleMode.Degrees
                fbDegRad.ButtonText = "DEG"
            End If
            fbDegRad.Text = fbDegRad.ButtonText
            UpdateStatus()
        End Sub

        Private Sub RebuildKeys()
            FillKeyPanel(pnlKeys, If(currentMode = CalcUiMode.Basic, BasicRows(), SciRows()), New Action(Of String)(AddressOf HandlePad))
        End Sub

        Private Sub RebuildGraphKeys()
            FillKeyPanel(pnlGraphKeys, GraphRows(), New Action(Of String)(AddressOf HandleGraphPad))
        End Sub

        Private Sub FillKeyPanel(host As Panel, rows As List(Of String()), handler As Action(Of String))
            Dim closure_0024__ As _Closure_0024__1 = New _Closure_0024__1()
            closure_0024__._0024VB_0024Local_handler = handler
            host.Controls.Clear()
            If host.Width < 10 OrElse host.Height < 10 Then
                Return
            End If
            Dim num = 0
            For Each row In rows
                If row.Length > num Then
                    num = row.Length
                End If
            Next
            Dim num2 = 3
            Dim num3 As Integer = Math.Max(20, host.Width - num2 * (num - 1) / num)
            Dim num4 As Integer = Math.Max(18, host.Height - num2 * (rows.Count - 1) / rows.Count)
            Dim num5 = rows.Count - 1
            Dim closure_0024__2 As _Closure_0024__1._Closure_0024__2 = DirectCast(Nothing, _Closure_0024__1._Closure_0024__2)
            For i = 0 To num5
                Dim array = rows(i)
                Dim num6 = array.Length - 1
                For j = 0 To num6
                    closure_0024__2 = New frmCalculator._Closure_0024__1._Closure_0024__2(closure_0024__2)
                    closure_0024__2._0024VB_0024NonLocal__0024VB_0024Closure_ClosureVariable_1 = closure_0024__
                    Dim text = array(j)
                    If Operators.CompareString(text, "", TextCompare:=False) <> 0 Then
                        Dim num7 = j * (num3 + num2)
                        Dim num8 = i * (num4 + num2)
                        closure_0024__2._0024VB_0024Local_press = text
                        Dim buttonColor = KeyColor(text)
                        Dim btn = modUtilities.MakeUtilityButton(host, text, num7, num8, num3, num4, buttonColor, closure_0024__2._Lambda_0024__11)
                        ApplyKeyFont(btn, text)
                    End If
                Next
            Next
        End Sub

        Private Shared Sub ApplyKeyFont(btn As FlatButton, caption As String)
            If btn IsNot Nothing Then
                Dim num = If(IsOperatorKey(caption), 22F, 16F)
                Dim style = If(IsOperatorKey(caption), FontStyle.Bold, FontStyle.Regular)
                Try
                    btn.ButtonTextHeight = CInt(Math.Round(num))
                    btn.Font = New Font("Arial Narrow", num, style, GraphicsUnit.Point)
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    Call ProjectData.ClearProjectError()
                End Try
                btn.Invalidate()
            End If
        End Sub

        Private Shared Function IsOperatorKey(key As String) As Boolean
            Select Case key
                Case "+", "-", "*", "/", "^", "=", "(", ")", "."
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        Private Shared Function BasicRows() As List(Of String())
            Dim list As List(Of String()) = New List(Of String())()
            list.Add(New String(4) {"C", "BKSP", "(", ")", "/"})
            list.Add(New String(4) {"7", "8", "9", "*", "^"})
            list.Add(New String(4) {"4", "5", "6", "-", "ANS"})
            list.Add(New String(4) {"1", "2", "3", "+", "π"})
            list.Add(New String(4) {"0", ".", "+/-", "=", "e"})
            Return list
        End Function

        Private Shared Function SciRows() As List(Of String())
            Dim list As List(Of String()) = New List(Of String())()
            list.Add(New String(5) {"C", "BKSP", "(", ")", "/", "^"})
            list.Add(New String(5) {"sin", "cos", "tan", "asin", "acos", "atan"})
            list.Add(New String(5) {"ln", "log10", "exp", "√", "x²", "1/x"})
            list.Add(New String(5) {"sinh", "cosh", "tanh", "nPr", "nCr", "x!"})
            list.Add(New String(5) {"7", "8", "9", "*", "π", "e"})
            list.Add(New String(5) {"4", "5", "6", "-", "ANS", "abs"})
            list.Add(New String(5) {"1", "2", "3", "+", "M+", "MR"})
            list.Add(New String(5) {"0", ".", "+/-", "=", "MC", "x"})
            Return list
        End Function

        Private Shared Function GraphRows() As List(Of String())
            Dim list As List(Of String()) = New List(Of String())()
            list.Add(New String(4) {"C", "BKSP", "(", ")", "/"})
            list.Add(New String(4) {"7", "8", "9", "*", "^"})
            list.Add(New String(4) {"4", "5", "6", "-", "x"})
            list.Add(New String(4) {"1", "2", "3", "+", "x²"})
            list.Add(New String(4) {"0", ".", "π", "e", "PLOT"})
            Return list
        End Function

        Private Shared Function KeyColor(key As String) As LCARScolorStyles
            Select Case key
                Case "C", "BKSP", "MC"
                    Return LCARScolorStyles.FunctionOffline
                Case "+", "-", "*", "/", "^", "=", "PLOT"
                    Return LCARScolorStyles.PrimaryFunction
                Case "sin", "cos", "tan", "asin", "acos", "atan", "ln", "log10", "exp", "√", "x²", "1/x", "sinh", "cosh", "tanh", "nPr", "nCr", "x!", "abs", "x"
                    Return LCARScolorStyles.MiscFunction
                Case Else
                    Return LCARScolorStyles.Orange
            End Select
        End Function

        Private Sub HandlePad(key As String)
            Select Case key
                Case "C"
                    expr = ""
                    RefreshDisplay()
                Case "BKSP"
                    If expr.Length > 0 Then
                        expr = expr.Substring(0, expr.Length - 1)
                    End If
                    RefreshDisplay()
                Case "="
                    EvaluateExpr()
                Case "+/-"
                    If expr.StartsWith("-", StringComparison.Ordinal) Then
                        expr = expr.Substring(1)
                    ElseIf expr.Length > 0 Then
                        expr = "-" & expr
                    End If
                    RefreshDisplay()
                Case "ANS"
                    InsertText(CalcEngine.FormatNumber(ans))
                Case "π"
                    InsertText("pi")
                Case "e"
                    InsertText("e")
                Case "x"
                    InsertText("x")
                Case "√"
                    InsertFn("sqrt")
                Case "x²"
                    InsertText("^2")
                Case "1/x"
                    expr = "1/(" & If(Operators.CompareString(expr, "", TextCompare:=False) = 0, "0", expr) & ")"
                    RefreshDisplay()
                Case "x!"
                    InsertFn("fact")
                Case "nPr"
                    InsertFn("nPr")
                Case "nCr"
                    InsertFn("nCr")
                Case "abs"
                    InsertFn("abs")
                Case "sin", "cos", "tan", "asin", "acos", "atan", "sinh", "cosh", "tanh", "ln", "log10", "exp"
                    InsertFn(key)
                Case "M+"
                    EvaluateExpr(updateDisplay:=False)
                    memory += ans
                    UpdateStatus()
                Case "MR"
                    InsertText(CalcEngine.FormatNumber(memory))
                Case "MC"
                    memory = 0.0
                    UpdateStatus()
                Case Else
                    InsertText(key)
            End Select
        End Sub

        Private Sub HandleGraphPad(key As String)
            Select Case key
                Case "C"
                    txtY1.Text = ""
                    txtY1.Focus()
                    txtY1.SelectionStart = 0
                Case "BKSP"
                    Dim text = txtY1.Text
                    Dim selectionStart = txtY1.SelectionStart
                    If txtY1.SelectionLength > 0 Then
                        txtY1.Text = text.Remove(txtY1.SelectionStart, txtY1.SelectionLength)
                        txtY1.SelectionStart = selectionStart
                    ElseIf selectionStart > 0 AndAlso text.Length > 0 Then
                        txtY1.Text = text.Remove(selectionStart - 1, 1)
                        txtY1.SelectionStart = selectionStart - 1
                    End If
                    txtY1.Focus()
                    Exit Select
                Case "PLOT", "="
                    PlotGraph()
                Case "π"
                    InsertIntoY1("pi")
                Case "e"
                    InsertIntoY1("e")
                Case "x"
                    InsertIntoY1("x")
                Case "x²"
                    InsertIntoY1("^2")
                Case Else
                    InsertIntoY1(key)
            End Select
        End Sub

        Private Sub InsertIntoY1(text As String)
            Dim text2 = txtY1.Text
            Dim text3 = If(Not Equals(text2, Nothing), text2, "")
            Dim num = txtY1.SelectionStart
            Dim num2 = txtY1.SelectionLength
            If num < 0 Then
                num = text3.Length
            End If
            If num2 < 0 Then
                num2 = 0
            End If
            If num + num2 > text3.Length Then
                num2 = Math.Max(0, text3.Length - num)
            End If
            txtY1.Text = text3.Substring(0, num) & text & text3.Substring(num + num2)
            txtY1.SelectionStart = num + text.Length
            txtY1.SelectionLength = 0
            txtY1.Focus()
        End Sub

        Private Sub InsertFn(name As String)
            InsertText(name & "(")
        End Sub

        Private Sub InsertText(text As String)
            expr += text
            RefreshDisplay()
        End Sub

        Private Sub EvaluateExpr(Optional updateDisplay As Boolean = True)
            Dim expression = If(Operators.CompareString(expr, "", TextCompare:=False) = 0, CalcEngine.FormatNumber(ans), expr)
            Dim evalResult = CalcEngine.Evaluate(expression, angleMode)
            If Not evalResult.Ok Then
                If updateDisplay Then
                    lblDisplay.Text = "ERROR"
                End If
                lblStatus.Text = "ERR: " & evalResult.ErrorMessage.ToUpperInvariant()
                Return
            End If
            ans = evalResult.Value
            expr = ""
            If updateDisplay Then
                lblDisplay.Text = CalcEngine.FormatNumber(ans)
                UpdateStatus()
            End If
        End Sub

        Private Sub RefreshDisplay()
            If Operators.CompareString(expr, "", TextCompare:=False) = 0 Then
                lblDisplay.Text = CalcEngine.FormatNumber(ans)
            Else
                lblDisplay.Text = expr
            End If
            UpdateStatus()
        End Sub

        Private Sub UpdateStatus()
            Dim text = If(angleMode = CalcEngine.AngleMode.Degrees, "DEG", "RAD")
            lblStatus.Text = text & "  ·  ANS=" & CalcEngine.FormatNumber(ans) & "  ·  M=" & CalcEngine.FormatNumber(memory)
        End Sub

        Private Sub OnFormKeyDown(sender As Object, e As KeyEventArgs)
            If currentMode = CalcUiMode.Graph Then
                If e.KeyCode = Keys.Return OrElse e.KeyCode = Keys.Return Then
                    PlotGraph()
                    e.Handled = True
                End If
            ElseIf e.KeyCode = Keys.Return OrElse e.KeyCode = Keys.Return Then
                EvaluateExpr()
                e.Handled = True
            ElseIf e.KeyCode = Keys.Escape Then
                expr = ""
                RefreshDisplay()
                e.Handled = True
            ElseIf e.KeyCode = Keys.Back Then
                HandlePad("BKSP")
                e.Handled = True
            End If
        End Sub

        Private Sub OnFormKeyPress(sender As Object, e As KeyPressEventArgs)
            If currentMode <> CalcUiMode.Graph Then
                Dim keyChar = e.KeyChar
                If Char.IsDigit(keyChar) OrElse keyChar = "."c OrElse "+-*/^()".IndexOf(keyChar) >= 0 Then
                    InsertText(keyChar.ToString())
                    e.Handled = True
                ElseIf keyChar = "="c OrElse keyChar = ChrW(13) Then
                    EvaluateExpr()
                    e.Handled = True
                End If
            End If
        End Sub

        Private Sub ReadWindow()
            Double.TryParse(txtXmin.Text, NumberStyles.Float, CultureInfo.InvariantCulture, xmin)
            Double.TryParse(txtXmax.Text, NumberStyles.Float, CultureInfo.InvariantCulture, xmax)
            Double.TryParse(txtYmin.Text, NumberStyles.Float, CultureInfo.InvariantCulture, ymin)
            Double.TryParse(txtYmax.Text, NumberStyles.Float, CultureInfo.InvariantCulture, ymax)
            If xmax <= xmin Then
                xmax = xmin + 1.0
            End If
            If ymax <= ymin Then
                ymax = ymin + 1.0
            End If
            Dim text = txtY1.Text
            y1Expr = If(Not Equals(text, Nothing), text, "").Trim()
            picGraph.SetWindow(xmin, xmax, ymin, ymax)
        End Sub

        Private Sub ZoomStandard()
            xmin = -10.0
            xmax = 10.0
            ymin = -10.0
            ymax = 10.0
            SyncWindowBoxes()
            picGraph.SetWindow(xmin, xmax, ymin, ymax)
            PlotGraph()
        End Sub

        Private Sub ZoomFit()
            ReadWindow()
            If Operators.CompareString(y1Expr, "", TextCompare:=False) = 0 Then
                Return
            End If
            Dim list As List(Of Double) = New List(Of Double)()
            Dim num = 200
            Dim num2 = num
            Dim i = 0

            While i <= num2
                Dim num3 = xmin + (xmax - xmin) * i / num
                Dim evalResult = CalcEngine.Evaluate(y1Expr, angleMode, num3)
                If evalResult.Ok AndAlso Not Double.IsNaN(evalResult.Value) AndAlso Not Double.IsInfinity(evalResult.Value) Then
                    list.Add(evalResult.Value)
                End If

                i = i + 1
            End While
            If list.Count = 0 Then
                Return
            End If
            ymin = list(0)
            ymax = list(0)
            For Each item In list
                If item < ymin Then
                    ymin = item
                End If
                If item > ymax Then
                    ymax = item
                End If
            Next
            Dim num4 = (ymax - ymin) * 0.1
            If num4 < 0.0001 Then
                num4 = 1.0
            End If
            ymin -= num4
            ymax += num4
            SyncWindowBoxes()
            picGraph.SetWindow(xmin, xmax, ymin, ymax)
            PlotGraph()
        End Sub

        Private Sub SyncWindowBoxes()
            txtXmin.Text = xmin.ToString(CultureInfo.InvariantCulture)
            txtXmax.Text = xmax.ToString(CultureInfo.InvariantCulture)
            txtYmin.Text = ymin.ToString(CultureInfo.InvariantCulture)
            txtYmax.Text = ymax.ToString(CultureInfo.InvariantCulture)
            txtY1.Text = y1Expr
        End Sub

        Private Sub PicGraph_WindowChanged(sender As Object, e As EventArgs)
            picGraph.GetWindow(xmin, xmax, ymin, ymax)
            SyncWindowBoxes()
            RecomputeCurve()
            UpdateTraceLabel()
            picGraph.Invalidate()
        End Sub

        Private Sub PicGraph_TraceRequested(sender As Object, location As Point)
            traceX = picGraph.UnmapX(location.X)
            UpdateTraceLabel()
            picGraph.Invalidate()
        End Sub

        Private Sub PlotGraph()
            ReadWindow()
            SyncWindowBoxes()
            RecomputeCurve()
            If graphOk OrElse Operators.CompareString(y1Expr, "", TextCompare:=False) = 0 Then
                traceX = (xmin + xmax) / 2.0
            End If
            UpdateTraceLabel()
            picGraph.Invalidate()
        End Sub

        Private Sub RecomputeCurve()
            graphPoints.Clear()
            graphOk = False
            If Operators.CompareString(y1Expr, "", TextCompare:=False) = 0 Then
                Return
            End If
            Dim val = Math.Max(10, picGraph.ClientSize.Width)
            Dim num = Math.Max(80, val)
            Dim flag = False
            Dim num2 = num
            Dim i = 0

            While i <= num2
                Dim num3 = xmin + (xmax - xmin) * i / num
                Dim evalResult = CalcEngine.Evaluate(y1Expr, angleMode, num3)
                If evalResult.Ok AndAlso Not Double.IsNaN(evalResult.Value) AndAlso Not Double.IsInfinity(evalResult.Value) Then
                    Dim list = graphPoints
                    Dim item As PointF = New PointF(num3, evalResult.Value)
                    list.Add(item)
                    flag = True
                    graphOk = True
                ElseIf flag Then
                    Dim list2 = graphPoints
                    Dim item As PointF = New PointF(Single.NaN, Single.NaN)
                    list2.Add(item)
                    flag = False
                End If

                i = i + 1
            End While
        End Sub

        Private Sub PicGraph_Paint(sender As Object, e As PaintEventArgs)
            Dim graphics = e.Graphics
            graphics.SmoothingMode = SmoothingMode.AntiAlias
            graphics.Clear(Color.FromArgb(10, 10, 16))
            Dim num = picGraph.ClientSize.Width
            Dim num2 = picGraph.ClientSize.Height
            If num < 2 OrElse num2 < 2 Then
                Return
            End If
            Using pen As Pen = New Pen(Color.FromArgb(40, 60, 80), 1F)
                Dim num3 = MapX(0.0)
                Dim num4 = MapY(0.0)
                If num3 >= 0 AndAlso num3 < num Then
                    graphics.DrawLine(pen, num3, 0, num3, num2)
                End If
                If num4 >= 0 AndAlso num4 < num2 Then
                    graphics.DrawLine(pen, 0, num4, num, num4)
                End If
            End Using
            If graphOk AndAlso graphPoints.Count > 1 Then
                Dim pen2 As Pen = New Pen(Color.Orange, 2F)
                Dim pointF As PointF = New PointF(Single.NaN, Single.NaN)
                For Each graphPoint In graphPoints
                    If Single.IsNaN(graphPoint.X) OrElse Single.IsNaN(graphPoint.Y) OrElse Single.IsInfinity(graphPoint.X) OrElse Single.IsInfinity(graphPoint.Y) Then
                        pointF = New PointF(Single.NaN, Single.NaN)
                        Continue For
                    End If
                    Dim x = MapX(graphPoint.X)
                    Dim num5 = MapY(graphPoint.Y)
                    If Not Single.IsNaN(pointF.X) AndAlso Not Single.IsInfinity(pointF.X) Then
                        Dim x2 = MapX(pointF.X)
                        Dim num6 = MapY(pointF.Y)
                        If Math.Abs(num5 - num6) < num2 * 2 Then
                            graphics.DrawLine(pen2, x2, num6, x, num5)
                        End If
                    End If
                    pointF = graphPoint
                Next
            End If
            Dim num7 = MapX(traceX)
            Dim v As Double = TraceY()
            Dim pen3 As Pen = New Pen(Color.FromArgb(153, 204, 255), 1F)
            graphics.DrawLine(pen3, num7, 0, num7, num2)
            If IsFinite(v) Then
                Dim num8 = MapY(v)
                graphics.DrawEllipse(pen3, num7 - 4, num8 - 4, 8, 8)
            End If
        End Sub

        Private Function MapX(x As Double) As Integer
            Dim num = picGraph.ClientSize.Width
            If num < 2 OrElse Not IsFinite(x) OrElse Not IsFinite(xmin) OrElse Not IsFinite(xmax) OrElse xmax = xmin Then
                Return 0
            End If
            Return ClampToInt((x - xmin) / (xmax - xmin) * num - 1)
        End Function

        Private Function MapY(y As Double) As Integer
            Dim num = picGraph.ClientSize.Height
            If num < 2 OrElse Not IsFinite(y) OrElse Not IsFinite(ymin) OrElse Not IsFinite(ymax) OrElse ymax = ymin Then
                Return 0
            End If
            Return ClampToInt((ymax - y) / (ymax - ymin) * num - 1)
        End Function

        Private Shared Function IsFinite(v As Double) As Boolean
            Return If(Not Double.IsNaN(v) AndAlso Not Double.IsInfinity(v), True, False)
        End Function

        Private Shared Function ClampToInt(v As Double) As Integer
            If Double.IsNaN(v) OrElse Double.IsInfinity(v) Then
                Return 0
            End If
            If v >= 2147483647.0 Then
                Return Integer.MaxValue
            End If
            If v <= -2147483648.0 Then
                Return Integer.MinValue
            End If
            Return Math.Round(Math.Round(v))
        End Function

        Private Function TraceY() As Double
            Dim evalResult = CalcEngine.Evaluate(y1Expr, angleMode, traceX)
            If evalResult.Ok Then
                Return evalResult.Value
            End If
            Return Double.NaN
        End Function

        Private Sub UpdateTraceLabel()
            Dim num As Double = TraceY()
            If Double.IsNaN(num) Then
                lblTrace.Text = "TRACE  X=" & CalcEngine.FormatNumber(traceX) & "  Y=—  ·  pan/pinch/wheel"
            Else
                lblTrace.Text = "TRACE  X=" & CalcEngine.FormatNumber(traceX) & "  Y=" & CalcEngine.FormatNumber(num)
            End If
        End Sub

        Private Sub NudgeTrace(dir As Integer)
            Dim num = (xmax - xmin) / Math.Max(40, picGraph.ClientSize.Width)
            traceX += dir * num
            If traceX < xmin Then
                traceX = xmin
            End If
            If traceX > xmax Then
                traceX = xmax
            End If
            UpdateTraceLabel()
            picGraph.Invalidate()
        End Sub

        Private Sub ShowTable()
            ReadWindow()
            Dim stringBuilder As StringBuilder = New StringBuilder()
            stringBuilder.AppendLine("X" & vbTab & "Y1=" & y1Expr)
            Dim num = 13
            Dim num2 = num - 1
            For i = 0 To num2
                Dim value = xmin + (xmax - xmin) * i / (num - 1)
                Dim evalResult = CalcEngine.Evaluate(y1Expr, angleMode, value)
                Dim text = If(evalResult.Ok, CalcEngine.FormatNumber(evalResult.Value), "ERR")
                stringBuilder.AppendLine(CalcEngine.FormatNumber(value) & vbTab & text)
            Next
            Call MessageBox.Show(stringBuilder.ToString(), "TABLE", MessageBoxButtons.OK, MessageBoxIcon.Asterisk)
        End Sub
    End Class
