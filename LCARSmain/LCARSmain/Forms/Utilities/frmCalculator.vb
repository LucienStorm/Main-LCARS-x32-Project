' LCARSmain/LCARSmain/Forms/Utilities/frmCalculator.vb
Option Strict On
Option Explicit On
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Globalization
Imports System.Windows.Forms
''' <summary>
''' Scientific + graphing calculator (TI handheld–class feature set on a color LCARS panel).
''' </summary>
Public Class frmCalculator
Inherits LCARS.LCARSForm
Private Enum CalcUiMode Basic = 0 Sci = 1 Graph = 2
End Enum
Private Const FormWidth As Integer = 780
Private Const FormHeight As Integer = 640
Private currentMode As CalcUiMode = CalcUiMode.Sci
Private angleMode As CalcEngine.AngleMode = CalcEngine.AngleMode.Degrees
Private expr As String = ""
Private ans As Double = 0
Private memory As Double = 0
Private lblDisplay As Label
Private lblStatus As Label
Private pnlKeys As Panel
Private pnlGraph As Panel
Private picGraph As PictureBox
Private txtY1 As TextBox
Private txtXmin As TextBox
Private txtXmax As TextBox
Private txtYmin As TextBox
Private txtYmax As TextBox
Private lblTrace As Label
Private fbModeBasic As LCARS.Controls.FlatButton
Private fbModeSci As LCARS.Controls.FlatButton
Private fbModeGraph As LCARS.Controls.FlatButton
Private fbDegRad As LCARS.Controls.FlatButton
Private xmin As Double = -10
Private xmax As Double = 10
Private ymin As Double = -10
Private ymax As Double = 10
Private y1Expr As String = "sin(x)"
Private traceX As Double = 0
Private graphPoints As New List(Of PointF)()
Private graphOk As Boolean = False
Public Sub New()
MyBase.New()
modUtilities.InitUtilityForm(Me, FormWidth, FormHeight) BuildChrome() ShowMode(CalcUiMode.Sci)
End Sub
Private Sub BuildChrome()
Dim m As Integer = modUtilities.UtilityMargin
Dim w As Integer = FormWidth - m * 2 modUtilities.MakeUtilityTitle(Me, "CALCULATOR", w) fbModeBasic = modUtilities.MakeUtilityButton(Me, "BASIC", m, 42, 90, 28, LCARS.LCARScolorStyles.PrimaryFunction, Sub(s As Object, e As EventArgs) ShowMode(CalcUiMode.Basic)) fbModeSci = modUtilities.MakeUtilityButton(Me, "SCI", m + 96, 42, 90, 28, LCARS.LCARScolorStyles.Orange, Sub(s As Object, e As EventArgs) ShowMode(CalcUiMode.Sci)) fbModeGraph = modUtilities.MakeUtilityButton(Me, "GRAPH", m + 192, 42, 100, 28, LCARS.LCARScolorStyles.PrimaryFunction, Sub(s As Object, e As EventArgs) ShowMode(CalcUiMode.Graph)) fbDegRad = modUtilities.MakeUtilityButton(Me, "DEG", m + 300, 42, 70, 28, LCARS.LCARScolorStyles.MiscFunction, AddressOf ToggleAngleMode) lblDisplay = modUtilities.MakeUtilityLabel(Me, m, 76, w, 52, 28.0F)
lblDisplay.TextAlign = ContentAlignment.MiddleRight lblDisplay.BorderStyle = BorderStyle.FixedSingle lblDisplay.Text = "0" lblStatus = modUtilities.MakeUtilityLabel(Me, m, 130, w, 20, 11.0F)
lblStatus.TextAlign = ContentAlignment.MiddleLeft lblStatus.Text = "DEG · ANS=0 · M=0" pnlKeys = New Panel()
pnlKeys.BackColor = Color.Black pnlKeys.Location = New Point(m, 154)
pnlKeys.Size = New Size(w, 430)
Controls.Add(pnlKeys) pnlGraph = New Panel()
pnlGraph.BackColor = Color.Black pnlGraph.Location = New Point(m, 154)
pnlGraph.Size = New Size(w, 430)
pnlGraph.Visible = False Controls.Add(pnlGraph) BuildGraphPanel()
modUtilities.MakeUtilityButton(Me, "CLOSE", m, FormHeight - m - 36, w, 36, LCARS.LCARScolorStyles.FunctionOffline, Sub(s As Object, e As EventArgs) Close()) KeyPreview = True
AddHandler Me.KeyDown, AddressOf OnFormKeyDown
AddHandler Me.KeyPress, AddressOf OnFormKeyPress
End Sub
Private Sub BuildGraphPanel()
Dim y As Integer = 0
Dim lblY As Label = modUtilities.MakeUtilityLabel(pnlGraph, 0, y, 40, 24, 12.0F)
lblY.Text = "Y1=" txtY1 = New TextBox()
txtY1.BackColor = Color.FromArgb(20, 20, 20)
txtY1.ForeColor = Color.Orange txtY1.BorderStyle = BorderStyle.FixedSingle txtY1.Font = New Font(modUtilities.UtilityFontName, 12.0!, FontStyle.Bold)
txtY1.Text = y1Expr txtY1.SetBounds(44, y, 420, 24)
pnlGraph.Controls.Add(txtY1)
modUtilities.MakeUtilityButton(pnlGraph, "GRAPH", 470, y, 90, 26, LCARS.LCARScolorStyles.PrimaryFunction, Sub(s As Object, e As EventArgs) PlotGraph())
modUtilities.MakeUtilityButton(pnlGraph, "ZSTD", 566, y, 70, 26, LCARS.LCARScolorStyles.MiscFunction, Sub(s As Object, e As EventArgs) ZoomStandard())
modUtilities.MakeUtilityButton(pnlGraph, "ZFIT", 642, y, 70, 26, LCARS.LCARScolorStyles.MiscFunction, Sub(s As Object, e As EventArgs) ZoomFit()) y += 32 AddWinField("Xmin", xmin, 0, y, txtXmin) AddWinField("Xmax", xmax, 180, y, txtXmax) AddWinField("Ymin", ymin, 360, y, txtYmin) AddWinField("Ymax", ymax, 540, y, txtYmax) y += 30 picGraph = New PictureBox()
picGraph.BackColor = Color.FromArgb(10, 10, 16)
picGraph.BorderStyle = BorderStyle.FixedSingle picGraph.SetBounds(0, y, pnlGraph.Width, 300)
AddHandler picGraph.Paint, AddressOf PicGraph_Paint
AddHandler picGraph.MouseMove, AddressOf PicGraph_MouseMove
AddHandler picGraph.MouseClick, AddressOf PicGraph_MouseClick pnlGraph.Controls.Add(picGraph) y += 306 lblTrace = modUtilities.MakeUtilityLabel(pnlGraph, 0, y, pnlGraph.Width, 22, 11.0F)
lblTrace.Text = "TRACE — move mouse / click on graph" modUtilities.MakeUtilityButton(pnlGraph, "←", pnlGraph.Width - 160, y - 2, 40, 24, LCARS.LCARScolorStyles.PrimaryFunction, Sub(s As Object, e As EventArgs) NudgeTrace(-1))
modUtilities.MakeUtilityButton(pnlGraph, "→", pnlGraph.Width - 114, y - 2, 40, 24, LCARS.LCARScolorStyles.PrimaryFunction, Sub(s As Object, e As EventArgs) NudgeTrace(1))
modUtilities.MakeUtilityButton(pnlGraph, "TABLE", pnlGraph.Width - 68, y - 2, 68, 24, LCARS.LCARScolorStyles.MiscFunction, Sub(s As Object, e As EventArgs) ShowTable())
End Sub
Private Sub AddWinField(ByVal caption As String, ByVal value As Double, ByVal x As Integer, ByVal y As Integer, ByRef box As TextBox)
Dim lbl As Label = modUtilities.MakeUtilityLabel(pnlGraph, x, y, 44, 22, 10.0F)
lbl.Text = caption box = New TextBox()
box.BackColor = Color.FromArgb(20, 20, 20)
box.ForeColor = Color.Orange box.BorderStyle = BorderStyle.FixedSingle box.Font = New Font(modUtilities.UtilityFontName, 11.0!, FontStyle.Bold)
box.Text = value.ToString(CultureInfo.InvariantCulture)
box.SetBounds(x + 46, y, 80, 22)
pnlGraph.Controls.Add(box)
End Sub
Private Sub ShowMode(ByVal mode As CalcUiMode) currentMode = mode fbModeBasic.Color = If(mode = CalcUiMode.Basic, LCARS.LCARScolorStyles.Orange, LCARS.LCARScolorStyles.PrimaryFunction)
fbModeSci.Color = If(mode = CalcUiMode.Sci, LCARS.LCARScolorStyles.Orange, LCARS.LCARScolorStyles.PrimaryFunction)
fbModeGraph.Color = If(mode = CalcUiMode.Graph, LCARS.LCARScolorStyles.Orange, LCARS.LCARScolorStyles.PrimaryFunction)
fbModeBasic.Invalidate()
fbModeSci.Invalidate()
fbModeGraph.Invalidate()
Dim graph As Boolean = (mode = CalcUiMode.Graph)
pnlGraph.Visible = graph pnlKeys.Visible = Not graph lblDisplay.Visible = Not graph lblStatus.Visible = Not graph
If Not graph Then RebuildKeys() RefreshDisplay()
Else PlotGraph()
End If
End Sub
Private Sub ToggleAngleMode(ByVal sender As Object, ByVal e As EventArgs)
If angleMode = CalcEngine.AngleMode.Degrees Then angleMode = CalcEngine.AngleMode.Radians fbDegRad.ButtonText = "RAD"
Else angleMode = CalcEngine.AngleMode.Degrees fbDegRad.ButtonText = "DEG"
End If fbDegRad.Text = fbDegRad.ButtonText UpdateStatus()
End Sub
Private Sub RebuildKeys()
pnlKeys.Controls.Clear()
Dim rows As List(Of String()) = If(currentMode = CalcUiMode.Basic, BasicRows(), SciRows())
Dim cols As Integer = 0
For Each r As String() In rows
If r.Length > cols Then cols = r.Length
Next
Dim gap As Integer = 4
Dim kw As Integer = (pnlKeys.Width - gap * (cols - 1)) \ cols
Dim kh As Integer = (pnlKeys.Height - gap * (rows.Count - 1)) \ rows.Count
For row As Integer = 0 To rows.Count - 1
Dim keys() As String = rows(row)
For col As Integer = 0 To keys.Length - 1
Dim caption As String = keys(col)
If caption = "" Then Continue
For
Dim x As Integer = col * (kw + gap)
Dim y As Integer = row * (kh + gap)
Dim press As String = caption
Dim color As LCARS.LCARScolorStyles = KeyColor(caption)
modUtilities.MakeUtilityButton(pnlKeys, caption, x, y, kw, kh, color, Sub(s As Object, e As EventArgs) HandlePad(press))
Next
Next
End Sub
Private Shared Function BasicRows() As List(Of String())
Return New List(Of String()) From { New String() {"C", "BKSP", "(", ")", "/"}, New String() {"7", "8", "9", "*", "^"}, New String() {"4", "5", "6", "-", "ANS"}, New String() {"1", "2", "3", "+", "π"}, New String() {"0", ".", "+/-", "=", "e"} }
End Function
Private Shared Function SciRows() As List(Of String())
Return New List(Of String()) From { New String() {"C", "BKSP", "(", ")", "/", "^"}, New String() {"sin", "cos", "tan", "asin", "acos", "atan"}, New String() {"ln", "log10", "exp", "√", "x²", "1/x"}, New String() {"sinh", "cosh", "tanh", "nPr", "nCr", "x!"}, New String() {"7", "8", "9", "*", "π", "e"}, New String() {"4", "5", "6", "-", "ANS", "abs"}, New String() {"1", "2", "3", "+", "M+", "MR"}, New String() {"0", ".", "+/-", "=", "MC", "x"} }
End Function
Private Shared Function KeyColor(ByVal key As String) As LCARS.LCARScolorStyles
Select Case key
Case "C", "BKSP", "MC" :
Return LCARS.LCARScolorStyles.FunctionOffline
Case "+", "-", "*", "/", "^", "=" :
Return LCARS.LCARScolorStyles.PrimaryFunction
Case "sin", "cos", "tan", "asin", "acos", "atan", "ln", "log10", "exp", "√", "x²", "1/x", "sinh", "cosh", "tanh", "nPr", "nCr", "x!", "abs"
Return LCARS.LCARScolorStyles.MiscFunction
Case
Else :
Return LCARS.LCARScolorStyles.Orange
End Select
End Function
Private Sub HandlePad(ByVal key As String)
Select Case key
Case "C" expr = "" RefreshDisplay()
Case "BKSP"
If expr.Length > 0 Then expr = expr.Substring(0, expr.Length - 1) RefreshDisplay()
Case "=" EvaluateExpr()
Case "+/-"
If expr.StartsWith("-", StringComparison.Ordinal) Then expr = expr.Substring(1)
ElseIf expr.Length > 0 Then expr = "-" & expr
End If RefreshDisplay()
Case "ANS" InsertText(CalcEngine.FormatNumber(ans))
Case "π" InsertText("pi")
Case "e" InsertText("e")
Case "x" InsertText("x")
Case "√" InsertFn("sqrt")
Case "x²" InsertText("^2")
Case "1/x" expr = "1/(" & If(expr = "", "0", expr) & ")" RefreshDisplay()
Case "x!" InsertFn("fact")
Case "nPr" InsertFn("nPr") ' user fills nPr(n,r)
Case "nCr" InsertFn("nCr")
Case "abs" InsertFn("abs")
Case "sin", "cos", "tan", "asin", "acos", "atan", "sinh", "cosh", "tanh", "ln", "log10", "exp" InsertFn(key)
Case "M+" EvaluateExpr(False) memory += ans UpdateStatus()
Case "MR" InsertText(CalcEngine.FormatNumber(memory))
Case "MC" memory = 0 UpdateStatus()
Case
Else InsertText(key)
End Select
End Sub
Private Sub InsertFn(ByVal name As String) InsertText(name & "(")
End Sub
Private Sub InsertText(ByVal text As String) expr &= text RefreshDisplay()
End Sub
Private Sub EvaluateExpr(Optional ByVal updateDisplay As Boolean = True)
Dim source As String = If(expr = "", CalcEngine.FormatNumber(ans), expr)
Dim r As CalcEngine.EvalResult = CalcEngine.Evaluate(source, angleMode)
If Not r.Ok Then If updateDisplay Then lblDisplay.Text = "ERROR" lblStatus.Text = "ERR: " & r.ErrorMessage.ToUpperInvariant()
Return
End If ans = r.Value expr = ""
If updateDisplay Then lblDisplay.Text = CalcEngine.FormatNumber(ans) UpdateStatus()
End If
End Sub
Private Sub RefreshDisplay()
If expr = "" Then lblDisplay.Text = CalcEngine.FormatNumber(ans)
Else lblDisplay.Text = expr
End If UpdateStatus()
End Sub
Private Sub UpdateStatus()
Dim ang As String = If(angleMode = CalcEngine.AngleMode.Degrees, "DEG", "RAD")
lblStatus.Text = ang & " · ANS=" & CalcEngine.FormatNumber(ans) & " · M=" & CalcEngine.FormatNumber(memory)
End Sub
Private Sub OnFormKeyDown(ByVal sender As Object, ByVal e As KeyEventArgs)
If currentMode = CalcUiMode.Graph Then Return
If e.KeyCode = Keys.Enter OrElse e.KeyCode = Keys.Return Then EvaluateExpr()
e.Handled = True
Return
End If
If e.KeyCode = Keys.Escape Then expr = "" RefreshDisplay()
e.Handled = True
Return
End If
If e.KeyCode = Keys.Back Then HandlePad("BKSP")
e.Handled = True
End If
End Sub
Private Sub OnFormKeyPress(ByVal sender As Object, ByVal e As KeyPressEventArgs)
If currentMode = CalcUiMode.Graph Then Return
Dim ch As Char = e.KeyChar
If Char.IsDigit(ch) OrElse ch = "."c OrElse "+-*/^()".IndexOf(ch) >= 0 Then InsertText(ch.ToString())
e.Handled = True
ElseIf ch = "="c OrElse ch = ChrW(13) Then EvaluateExpr()
e.Handled = True
End If
End Sub ' ---- Graphing ----
Private Sub ReadWindow()
Double.TryParse(txtXmin.Text, NumberStyles.Float, CultureInfo.InvariantCulture, xmin)
Double.TryParse(txtXmax.Text, NumberStyles.Float, CultureInfo.InvariantCulture, xmax)
Double.TryParse(txtYmin.Text, NumberStyles.Float, CultureInfo.InvariantCulture, ymin)
Double.TryParse(txtYmax.Text, NumberStyles.Float, CultureInfo.InvariantCulture, ymax)
If xmax <= xmin Then xmax = xmin + 1
If ymax <= ymin Then ymax = ymin + 1 y1Expr = If(txtY1.Text, "").Trim()
If y1Expr = "" Then y1Expr = "x"
End Sub
Private Sub ZoomStandard() xmin = -10 : xmax = 10 : ymin = -10 : ymax = 10 SyncWindowBoxes() PlotGraph()
End Sub
Private Sub ZoomFit() ReadWindow()
Dim ys As New List(Of Double)()
Dim steps As Integer = 200
For i As Integer = 0 To steps
Dim xv As Double = xmin + (xmax - xmin) * i / steps
Dim r As CalcEngine.EvalResult = CalcEngine.Evaluate(y1Expr, angleMode, xv)
If r.Ok AndAlso Not Double.IsNaN(r.Value) AndAlso Not Double.IsInfinity(r.Value) Then ys.Add(r.Value)
End If
Next
If ys.Count = 0 Then Return ymin = ys(0) : ymax = ys(0)
For Each v As Double In ys
If v < ymin Then ymin = v
If v > ymax Then ymax = v
Next
Dim pad As Double = (ymax - ymin) * 0.1
If pad < 0.0001 Then pad = 1 ymin -= pad ymax += pad SyncWindowBoxes() PlotGraph()
End Sub
Private Sub SyncWindowBoxes()
txtXmin.Text = xmin.ToString(CultureInfo.InvariantCulture)
txtXmax.Text = xmax.ToString(CultureInfo.InvariantCulture)
txtYmin.Text = ymin.ToString(CultureInfo.InvariantCulture)
txtYmax.Text = ymax.ToString(CultureInfo.InvariantCulture)
txtY1.Text = y1Expr
End Sub
Private Sub PlotGraph() ReadWindow() SyncWindowBoxes()
graphPoints.Clear() graphOk = False
Dim w As Integer = Math.Max(10, picGraph.ClientSize.Width)
Dim samples As Integer = Math.Max(80, w)
Dim lastOk As Boolean = False
For i As Integer = 0 To samples
Dim xv As Double = xmin + (xmax - xmin) * i / samples
Dim r As CalcEngine.EvalResult = CalcEngine.Evaluate(y1Expr, angleMode, xv)
If r.Ok AndAlso Not Double.IsNaN(r.Value) AndAlso Not Double.IsInfinity(r.Value) Then graphPoints.Add(New PointF(CSng(xv), CSng(r.Value))) lastOk = True graphOk = True
ElseIf lastOk Then graphPoints.Add(New PointF(Single.NaN, Single.NaN)) ' break lastOk = False
End If
Next traceX = (xmin + xmax) / 2 UpdateTraceLabel()
picGraph.Invalidate()
End Sub
Private Sub PicGraph_Paint(ByVal sender As Object, ByVal e As PaintEventArgs)
Dim g As Graphics = e.Graphics g.SmoothingMode = SmoothingMode.AntiAlias g.Clear(Color.FromArgb(10, 10, 16))
Dim w As Integer = picGraph.ClientSize.Width
Dim h As Integer = picGraph.ClientSize.Height
If w < 2 OrElse h < 2 Then Return ' axes
Using penGrid As New Pen(Color.FromArgb(40, 60, 80), 1)
Dim x0 As Integer = MapX(0)
Dim y0 As Integer = MapY(0)
If x0 >= 0 AndAlso x0 < w Then g.DrawLine(penGrid, x0, 0, x0, h)
If y0 >= 0 AndAlso y0 < h Then g.DrawLine(penGrid, 0, y0, w, y0)
End Using
If graphOk AndAlso graphPoints.Count > 1 Then Using pen As New Pen(Color.Orange, 2)
Dim prev As PointF = New PointF(Single.NaN, Single.NaN)
For Each p As PointF In graphPoints
If Single.IsNaN(p.X) OrElse Single.IsNaN(p.Y) Then prev = p
Continue For
End If
Dim sx As Integer = MapX(p.X)
Dim sy As Integer = MapY(p.Y)
If Not Single.IsNaN(prev.X) Then Dim px As Integer = MapX(prev.X)
Dim py As Integer = MapY(prev.Y) ' skip huge jumps (asymptotes)
If Math.Abs(sy - py) < h * 2 Then g.DrawLine(pen, px, py, sx, sy)
End If
End If prev = p
Next
End Using
End If ' trace crosshair
Dim tx As Integer = MapX(traceX)
Dim tyVal As Double = TraceY()
Dim ty As Integer = MapY(tyVal)
Using penT As New Pen(Color.FromArgb(153, 204, 255), 1)
g.DrawLine(penT, tx, 0, tx, h)
If Not Double.IsNaN(tyVal) Then g.DrawEllipse(penT, tx - 4, ty - 4, 8, 8)
End If
End Using
End Sub
Private Function MapX(ByVal x As Double) As Integer
Dim w As Integer = picGraph.ClientSize.Width
Return CInt((x - xmin) / (xmax - xmin) * (w - 1))
End Function
Private Function MapY(ByVal y As Double) As Integer
Dim h As Integer = picGraph.ClientSize.Height
Return CInt((ymax - y) / (ymax - ymin) * (h - 1))
End Function
Private Function UnmapX(ByVal px As Integer) As Double
Dim w As Integer = Math.Max(1, picGraph.ClientSize.Width - 1)
Return xmin + (xmax - xmin) * px / w
End Function
Private Function TraceY() As Double
Dim r As CalcEngine.EvalResult = CalcEngine.Evaluate(y1Expr, angleMode, traceX)
If r.Ok Then Return r.Value
Return Double.NaN
End Function
Private Sub UpdateTraceLabel()
Dim yv As Double = TraceY()
If Double.IsNaN(yv) Then lblTrace.Text = "TRACE X=" & CalcEngine.FormatNumber(traceX) & " Y=—"
Else lblTrace.Text = "TRACE X=" & CalcEngine.FormatNumber(traceX) & " Y=" & CalcEngine.FormatNumber(yv)
End If
End Sub
Private Sub PicGraph_MouseMove(ByVal sender As Object, ByVal e As MouseEventArgs)
If (Control.MouseButtons And MouseButtons.Left) = 0 Then Return traceX = UnmapX(e.X) UpdateTraceLabel()
picGraph.Invalidate()
End Sub
Private Sub PicGraph_MouseClick(ByVal sender As Object, ByVal e As MouseEventArgs) traceX = UnmapX(e.X) UpdateTraceLabel()
picGraph.Invalidate()
End Sub
Private Sub NudgeTrace(ByVal dir As Integer)
Dim stepX As Double = (xmax - xmin) / Math.Max(40, picGraph.ClientSize.Width) traceX += dir * stepX
If traceX < xmin Then traceX = xmin
If traceX > xmax Then traceX = xmax UpdateTraceLabel()
picGraph.Invalidate()
End Sub
Private Sub ShowTable() ReadWindow()
Dim sb As New System.Text.StringBuilder()
sb.AppendLine("X" & vbTab & "Y1=" & y1Expr)
Dim n As Integer = 13
For i As Integer = 0 To n - 1
Dim xv As Double = xmin + (xmax - xmin) * i / (n - 1)
Dim r As CalcEngine.EvalResult = CalcEngine.Evaluate(y1Expr, angleMode, xv)
Dim yv As String = If(r.Ok, CalcEngine.FormatNumber(r.Value), "ERR")
sb.AppendLine(CalcEngine.FormatNumber(xv) & vbTab & yv)
Next MessageBox.Show(sb.ToString(), "TABLE", MessageBoxButtons.OK, MessageBoxIcon.Information)
End Sub
End Class
