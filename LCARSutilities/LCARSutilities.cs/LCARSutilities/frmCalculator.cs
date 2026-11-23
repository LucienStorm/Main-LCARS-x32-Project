using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Forms;
using LCARS;
using LCARS.Controls;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSutilities;

public class frmCalculator : LCARSForm
{
	private enum CalcUiMode
	{
		Basic,
		Sci,
		Graph
	}

	private const int FormWidth = 780;

	private const int FormHeight = 560;

	private const float KeyFontNormal = 16f;

	private const float KeyFontOperator = 22f;

	private const string KeyFontName = "Arial Narrow";

	private const int GraphKeypadWidth = 300;

	private const int CloseButtonHeight = 32;

	private CalcUiMode currentMode;

	private CalcEngine.AngleMode angleMode;

	private string expr;

	private double ans;

	private double memory;

	private bool layoutBusy;

	private FlatButton titleBar;

	private Label lblDisplay;

	private Label lblStatus;

	private Panel pnlKeys;

	private Panel pnlGraph;

	private Panel pnlGraphKeys;

	private GraphCanvas picGraph;

	private TextBox txtY1;

	private TextBox txtXmin;

	private TextBox txtXmax;

	private TextBox txtYmin;

	private TextBox txtYmax;

	private Label lblY1;

	private Label lblTrace;

	private FlatButton fbModeBasic;

	private FlatButton fbModeSci;

	private FlatButton fbModeGraph;

	private FlatButton fbDegRad;

	private FlatButton fbClose;

	private FlatButton fbPlot;

	private FlatButton fbZstd;

	private FlatButton fbZfit;

	private FlatButton fbTraceLeft;

	private FlatButton fbTraceRight;

	private FlatButton fbTable;

	private Label lblXmin;

	private Label lblXmax;

	private Label lblYmin;

	private Label lblYmax;

	private double xmin;

	private double xmax;

	private double ymin;

	private double ymax;

	private string y1Expr;

	private double traceX;

	private List<PointF> graphPoints;

	private bool graphOk;

	public frmCalculator()
	{
		currentMode = CalcUiMode.Sci;
		angleMode = CalcEngine.AngleMode.Degrees;
		expr = "";
		ans = 0.0;
		memory = 0.0;
		layoutBusy = false;
		xmin = -10.0;
		xmax = 10.0;
		ymin = -10.0;
		ymax = 10.0;
		y1Expr = "";
		traceX = 0.0;
		graphPoints = new List<PointF>();
		graphOk = false;
		modUtilities.InitUtilityFormFloating(this, 780, 560);
		BuildChrome();
		Resize += OnFormResize;
		Shown += OnFormShown;
		ShowMode(CalcUiMode.Sci);
	}

	private void OnFormShown(object sender, EventArgs e)
	{
		LayoutChrome();
	}

	private void OnFormResize(object sender, EventArgs e)
	{
		LayoutChrome();
	}

	private void BuildChrome()
	{
		int num = 12;
		titleBar = modUtilities.MakeUtilityTitle(this, "CALCULATOR", 200);
		fbModeBasic = StyleModeButton(modUtilities.MakeUtilityButton(this, "BASIC", num, 40, 90, 26, LCARScolorStyles.PrimaryFunction, [SpecialName] (object s, EventArgs e) =>
		{
			ShowMode(CalcUiMode.Basic);
		}));
		checked
		{
			fbModeSci = StyleModeButton(modUtilities.MakeUtilityButton(this, "SCI", num + 96, 40, 90, 26, LCARScolorStyles.Orange, [SpecialName] (object s, EventArgs e) =>
			{
				ShowMode(CalcUiMode.Sci);
			}));
			fbModeGraph = StyleModeButton(modUtilities.MakeUtilityButton(this, "GRAPH", num + 192, 40, 100, 26, LCARScolorStyles.PrimaryFunction, [SpecialName] (object s, EventArgs e) =>
			{
				ShowMode(CalcUiMode.Graph);
			}));
			fbDegRad = StyleModeButton(modUtilities.MakeUtilityButton(this, "DEG", num + 300, 40, 70, 26, LCARScolorStyles.MiscFunction, ToggleAngleMode));
			lblDisplay = modUtilities.MakeUtilityLabel(this, num, 70, 200, 48, 30f);
			lblDisplay.TextAlign = ContentAlignment.MiddleRight;
			lblDisplay.BorderStyle = BorderStyle.FixedSingle;
			lblDisplay.Text = "0";
			lblStatus = modUtilities.MakeUtilityLabel(this, num, 120, 200, 18, 12f);
			lblStatus.TextAlign = ContentAlignment.MiddleLeft;
			lblStatus.Text = "DEG  ·  ANS=0  ·  M=0";
			pnlKeys = new Panel();
			pnlKeys.BackColor = Color.Black;
			Controls.Add(pnlKeys);
			pnlGraph = new Panel();
			pnlGraph.BackColor = Color.Black;
			pnlGraph.Visible = false;
			Controls.Add(pnlGraph);
			BuildGraphPanel();
			fbClose = modUtilities.MakeUtilityButton(this, "CLOSE", num, 100, 200, 32, LCARScolorStyles.FunctionOffline, [SpecialName] (object s, EventArgs e) =>
			{
				Close();
			});
			ApplyKeyFont(fbClose, "CLOSE");
			KeyPreview = true;
			KeyDown += OnFormKeyDown;
			KeyPress += OnFormKeyPress;
		}
	}

	private FlatButton StyleModeButton(FlatButton btn)
	{
		ApplyKeyFont(btn, btn.ButtonText);
		return btn;
	}

	private void BuildGraphPanel()
	{
		lblY1 = modUtilities.MakeUtilityLabel(pnlGraph, 0, 0, 40, 24, 13f);
		lblY1.Text = "Y1=";
		txtY1 = new TextBox();
		txtY1.BackColor = Color.FromArgb(20, 20, 20);
		txtY1.ForeColor = Color.Orange;
		txtY1.BorderStyle = BorderStyle.FixedSingle;
		txtY1.Font = new Font("Arial Narrow", 14f, FontStyle.Bold);
		txtY1.Text = y1Expr;
		pnlGraph.Controls.Add(txtY1);
		fbPlot = ApplyNamed(modUtilities.MakeUtilityButton(pnlGraph, "PLOT", 0, 0, 80, 26, LCARScolorStyles.PrimaryFunction, [SpecialName] (object s, EventArgs e) =>
		{
			PlotGraph();
		}), "PLOT");
		fbZstd = ApplyNamed(modUtilities.MakeUtilityButton(pnlGraph, "ZSTD", 0, 0, 70, 26, LCARScolorStyles.MiscFunction, [SpecialName] (object s, EventArgs e) =>
		{
			ZoomStandard();
		}), "ZSTD");
		fbZfit = ApplyNamed(modUtilities.MakeUtilityButton(pnlGraph, "ZFIT", 0, 0, 70, 26, LCARScolorStyles.MiscFunction, [SpecialName] (object s, EventArgs e) =>
		{
			ZoomFit();
		}), "ZFIT");
		AddWinField("Xmin", xmin, ref lblXmin, ref txtXmin);
		AddWinField("Xmax", xmax, ref lblXmax, ref txtXmax);
		AddWinField("Ymin", ymin, ref lblYmin, ref txtYmin);
		AddWinField("Ymax", ymax, ref lblYmax, ref txtYmax);
		picGraph = new GraphCanvas();
		picGraph.BackColor = Color.FromArgb(10, 10, 16);
		picGraph.BorderStyle = BorderStyle.FixedSingle;
		picGraph.TabStop = true;
		picGraph.Paint += PicGraph_Paint;
		picGraph.WindowChanged += PicGraph_WindowChanged;
		picGraph.TraceRequested += PicGraph_TraceRequested;
		pnlGraph.Controls.Add(picGraph);
		pnlGraphKeys = new Panel();
		pnlGraphKeys.BackColor = Color.Black;
		pnlGraph.Controls.Add(pnlGraphKeys);
		lblTrace = modUtilities.MakeUtilityLabel(pnlGraph, 0, 0, 200, 22, 12f);
		lblTrace.Text = "TRACE — tap  ·  drag pan  ·  pinch/wheel zoom";
		fbTraceLeft = ApplyNamed(modUtilities.MakeUtilityButton(pnlGraph, "←", 0, 0, 40, 24, LCARScolorStyles.PrimaryFunction, [SpecialName] (object s, EventArgs e) =>
		{
			NudgeTrace(-1);
		}), "←");
		fbTraceRight = ApplyNamed(modUtilities.MakeUtilityButton(pnlGraph, "→", 0, 0, 40, 24, LCARScolorStyles.PrimaryFunction, [SpecialName] (object s, EventArgs e) =>
		{
			NudgeTrace(1);
		}), "→");
		fbTable = ApplyNamed(modUtilities.MakeUtilityButton(pnlGraph, "TABLE", 0, 0, 70, 24, LCARScolorStyles.MiscFunction, [SpecialName] (object s, EventArgs e) =>
		{
			ShowTable();
		}), "TABLE");
	}

	private FlatButton ApplyNamed(FlatButton btn, string caption)
	{
		ApplyKeyFont(btn, caption);
		return btn;
	}

	private void AddWinField(string caption, double value, ref Label lbl, ref TextBox box)
	{
		lbl = modUtilities.MakeUtilityLabel(pnlGraph, 0, 0, 44, 22, 11f);
		lbl.Text = caption;
		box = new TextBox();
		box.BackColor = Color.FromArgb(20, 20, 20);
		box.ForeColor = Color.Orange;
		box.BorderStyle = BorderStyle.FixedSingle;
		box.Font = new Font("Arial Narrow", 12f, FontStyle.Bold);
		box.Text = value.ToString(CultureInfo.InvariantCulture);
		pnlGraph.Controls.Add(box);
	}

	private void LayoutChrome()
	{
		if (layoutBusy || !IsHandleCreated)
		{
			return;
		}
		layoutBusy = true;
		checked
		{
			try
			{
				int num = 12;
				int num2 = Math.Max(320, ClientSize.Width - num * 2);
				int num3 = Math.Max(240, ClientSize.Height);
				Point location;
				if (titleBar != null)
				{
					FlatButton flatButton = titleBar;
					location = new Point(num, num);
					flatButton.Location = location;
					FlatButton flatButton2 = titleBar;
					Size size = new Size(num2, 28);
					flatButton2.Size = size;
				}
				FlatButton flatButton3 = fbModeBasic;
				location = new Point(num, 40);
				flatButton3.Location = location;
				FlatButton flatButton4 = fbModeSci;
				location = new Point(num + 96, 40);
				flatButton4.Location = location;
				FlatButton flatButton5 = fbModeGraph;
				location = new Point(num + 192, 40);
				flatButton5.Location = location;
				FlatButton flatButton6 = fbDegRad;
				location = new Point(num + 300, 40);
				flatButton6.Location = location;
				int num4;
				if (currentMode == CalcUiMode.Graph)
				{
					lblDisplay.Visible = false;
					lblStatus.Visible = false;
					num4 = 72;
				}
				else
				{
					lblDisplay.Visible = true;
					lblStatus.Visible = true;
					lblDisplay.SetBounds(num, 70, num2, 48);
					lblStatus.SetBounds(num, 120, num2, 18);
					num4 = 142;
				}
				fbClose.SetBounds(num, num3 - num - 32, num2, 32);
				int num5 = fbClose.Top - 6;
				int num6 = Math.Max(100, num5 - num4);
				pnlKeys.SetBounds(num, num4, num2, num6);
				pnlGraph.SetBounds(num, num4, num2, num6);
				if (currentMode == CalcUiMode.Graph)
				{
					LayoutGraphBody();
				}
				else if (pnlKeys.Visible)
				{
					RebuildKeys();
				}
			}
			finally
			{
				layoutBusy = false;
			}
		}
	}

	private void LayoutGraphBody()
	{
		int num = 0;
		int num2 = pnlGraph.ClientSize.Width;
		int num3 = pnlGraph.ClientSize.Height;
		checked
		{
			lblY1.SetBounds(0, num + 2, 40, 24);
			int num4 = 236;
			txtY1.SetBounds(44, num, Math.Max(120, num2 - 44 - num4 - 12), 26);
			int num5 = txtY1.Right + 8;
			fbPlot.SetBounds(num5, num, 80, 26);
			fbZstd.SetBounds(num5 + 88, num, 70, 26);
			fbZfit.SetBounds(num5 + 166, num, 70, 26);
			num += 30;
			PlaceWinField(lblXmin, txtXmin, 0, num);
			PlaceWinField(lblXmax, txtXmax, 180, num);
			PlaceWinField(lblYmin, txtYmin, 360, num);
			PlaceWinField(lblYmax, txtYmax, 540, num);
			num += 28;
			int num6 = 26;
			int num7 = num3 - num - num6 - 2;
			if (num7 < 80)
			{
				num7 = 80;
			}
			int num8 = 300;
			if (num2 < num8 + 200)
			{
				num8 = Math.Max(180, unchecked(num2 / 3));
			}
			int num9 = num2 - num8 - 8;
			if (num9 < 120)
			{
				num8 = Math.Max(140, unchecked(num2 / 3));
				num9 = Math.Max(100, num2 - num8 - 8);
			}
			picGraph.SetBounds(0, num, num9, num7);
			pnlGraphKeys.SetBounds(num9 + 8, num, num8, num7);
			num += num7 + 2;
			lblTrace.SetBounds(0, num, Math.Max(80, num9 - 180), 22);
			fbTraceLeft.SetBounds(num9 - 170, num - 2, 40, 24);
			fbTraceRight.SetBounds(num9 - 124, num - 2, 40, 24);
			fbTable.SetBounds(num9 - 78, num - 2, 70, 24);
			RebuildGraphKeys();
			picGraph.SetWindow(xmin, xmax, ymin, ymax);
			if (Operators.CompareString(y1Expr, "", TextCompare: false) != 0)
			{
				PlotGraph();
			}
			else
			{
				picGraph.Invalidate();
			}
		}
	}

	private static void PlaceWinField(Label lbl, TextBox box, int x, int y)
	{
		lbl.SetBounds(x, y, 44, 22);
		box.SetBounds(checked(x + 46), y, 80, 22);
	}

	private void ShowMode(CalcUiMode mode)
	{
		currentMode = mode;
		fbModeBasic.Color = ((mode == CalcUiMode.Basic) ? LCARScolorStyles.Orange : LCARScolorStyles.PrimaryFunction);
		fbModeSci.Color = ((mode == CalcUiMode.Sci) ? LCARScolorStyles.Orange : LCARScolorStyles.PrimaryFunction);
		fbModeGraph.Color = ((mode == CalcUiMode.Graph) ? LCARScolorStyles.Orange : LCARScolorStyles.PrimaryFunction);
		fbModeBasic.Invalidate();
		fbModeSci.Invalidate();
		fbModeGraph.Invalidate();
		bool flag = mode == CalcUiMode.Graph;
		pnlGraph.Visible = flag;
		pnlKeys.Visible = !flag;
		LayoutChrome();
		if (flag)
		{
			SyncWindowBoxes();
			picGraph.SetWindow(xmin, xmax, ymin, ymax);
			picGraph.Invalidate();
		}
		else
		{
			RebuildKeys();
			RefreshDisplay();
		}
	}

	private void ToggleAngleMode(object sender, EventArgs e)
	{
		if (angleMode == CalcEngine.AngleMode.Degrees)
		{
			angleMode = CalcEngine.AngleMode.Radians;
			fbDegRad.ButtonText = "RAD";
		}
		else
		{
			angleMode = CalcEngine.AngleMode.Degrees;
			fbDegRad.ButtonText = "DEG";
		}
		fbDegRad.Text = fbDegRad.ButtonText;
		UpdateStatus();
	}

	private void RebuildKeys()
	{
		FillKeyPanel(pnlKeys, (currentMode == CalcUiMode.Basic) ? BasicRows() : SciRows(), HandlePad);
	}

	private void RebuildGraphKeys()
	{
		FillKeyPanel(pnlGraphKeys, GraphRows(), HandleGraphPad);
	}

	private void FillKeyPanel(Panel host, List<string[]> rows, Action<string> handler)
	{
		host.Controls.Clear();
		if (host.Width < 10 || host.Height < 10)
		{
			return;
		}
		int num = 0;
		foreach (string[] row in rows)
		{
			if (row.Length > num)
			{
				num = row.Length;
			}
		}
		int num2 = 3;
		int num3 = Math.Max(20, checked(host.Width - num2 * (num - 1)) / num);
		int num4 = Math.Max(18, checked(host.Height - num2 * (rows.Count - 1)) / rows.Count);
		checked
		{
			int num5 = rows.Count - 1;
			for (int i = 0; i <= num5; i++)
			{
				string[] array = rows[i];
				int num6 = array.Length - 1;
				for (int j = 0; j <= num6; j++)
				{
					string text = array[j];
					if (Operators.CompareString(text, "", TextCompare: false) != 0)
					{
						int num7 = j * (num3 + num2);
						int num8 = i * (num4 + num2);
						string press = text;
						LCARScolorStyles buttonColor = KeyColor(text);
						FlatButton btn = modUtilities.MakeUtilityButton(host, text, num7, num8, num3, num4, buttonColor, delegate
						{
							handler(press);
						});
						ApplyKeyFont(btn, text);
					}
				}
			}
		}
	}

	private static void ApplyKeyFont(FlatButton btn, string caption)
	{
		if (btn != null)
		{
			float num = (IsOperatorKey(caption) ? 22f : 16f);
			FontStyle style = (IsOperatorKey(caption) ? FontStyle.Bold : FontStyle.Regular);
			try
			{
				btn.ButtonTextHeight = checked((int)Math.Round(num));
				btn.Font = new Font("Arial Narrow", num, style, GraphicsUnit.Point);
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
			btn.Invalidate();
		}
	}

	private static bool IsOperatorKey(string key)
	{
		switch (key)
		{
		case "+":
		case "-":
		case "*":
		case "/":
		case "^":
		case "=":
		case "(":
		case ")":
		case ".":
			return true;
		default:
			return false;
		}
	}

	private static List<string[]> BasicRows()
	{
		List<string[]> list = new List<string[]>();
		list.Add(new string[5] { "C", "BKSP", "(", ")", "/" });
		list.Add(new string[5] { "7", "8", "9", "*", "^" });
		list.Add(new string[5] { "4", "5", "6", "-", "ANS" });
		list.Add(new string[5] { "1", "2", "3", "+", "π" });
		list.Add(new string[5] { "0", ".", "+/-", "=", "e" });
		return list;
	}

	private static List<string[]> SciRows()
	{
		List<string[]> list = new List<string[]>();
		list.Add(new string[6] { "C", "BKSP", "(", ")", "/", "^" });
		list.Add(new string[6] { "sin", "cos", "tan", "asin", "acos", "atan" });
		list.Add(new string[6] { "ln", "log10", "exp", "√", "x²", "1/x" });
		list.Add(new string[6] { "sinh", "cosh", "tanh", "nPr", "nCr", "x!" });
		list.Add(new string[6] { "7", "8", "9", "*", "π", "e" });
		list.Add(new string[6] { "4", "5", "6", "-", "ANS", "abs" });
		list.Add(new string[6] { "1", "2", "3", "+", "M+", "MR" });
		list.Add(new string[6] { "0", ".", "+/-", "=", "MC", "x" });
		return list;
	}

	private static List<string[]> GraphRows()
	{
		List<string[]> list = new List<string[]>();
		list.Add(new string[5] { "C", "BKSP", "(", ")", "/" });
		list.Add(new string[5] { "7", "8", "9", "*", "^" });
		list.Add(new string[5] { "4", "5", "6", "-", "x" });
		list.Add(new string[5] { "1", "2", "3", "+", "x²" });
		list.Add(new string[5] { "0", ".", "π", "e", "PLOT" });
		return list;
	}

	private static LCARScolorStyles KeyColor(string key)
	{
		switch (key)
		{
		case "C":
		case "BKSP":
		case "MC":
			return LCARScolorStyles.FunctionOffline;
		case "+":
		case "-":
		case "*":
		case "/":
		case "^":
		case "=":
		case "PLOT":
			return LCARScolorStyles.PrimaryFunction;
		case "sin":
		case "cos":
		case "tan":
		case "asin":
		case "acos":
		case "atan":
		case "ln":
		case "log10":
		case "exp":
		case "√":
		case "x²":
		case "1/x":
		case "sinh":
		case "cosh":
		case "tanh":
		case "nPr":
		case "nCr":
		case "x!":
		case "abs":
		case "x":
			return LCARScolorStyles.MiscFunction;
		default:
			return LCARScolorStyles.Orange;
		}
	}

	private void HandlePad(string key)
	{
		switch (key)
		{
		case "C":
			expr = "";
			RefreshDisplay();
			break;
		case "BKSP":
			if (expr.Length > 0)
			{
				expr = expr.Substring(0, checked(expr.Length - 1));
			}
			RefreshDisplay();
			break;
		case "=":
			EvaluateExpr();
			break;
		case "+/-":
			if (expr.StartsWith("-", StringComparison.Ordinal))
			{
				expr = expr.Substring(1);
			}
			else if (expr.Length > 0)
			{
				expr = "-" + expr;
			}
			RefreshDisplay();
			break;
		case "ANS":
			InsertText(CalcEngine.FormatNumber(ans));
			break;
		case "π":
			InsertText("pi");
			break;
		case "e":
			InsertText("e");
			break;
		case "x":
			InsertText("x");
			break;
		case "√":
			InsertFn("sqrt");
			break;
		case "x²":
			InsertText("^2");
			break;
		case "1/x":
			expr = "1/(" + ((Operators.CompareString(expr, "", TextCompare: false) == 0) ? "0" : expr) + ")";
			RefreshDisplay();
			break;
		case "x!":
			InsertFn("fact");
			break;
		case "nPr":
			InsertFn("nPr");
			break;
		case "nCr":
			InsertFn("nCr");
			break;
		case "abs":
			InsertFn("abs");
			break;
		case "sin":
		case "cos":
		case "tan":
		case "asin":
		case "acos":
		case "atan":
		case "sinh":
		case "cosh":
		case "tanh":
		case "ln":
		case "log10":
		case "exp":
			InsertFn(key);
			break;
		case "M+":
			EvaluateExpr(updateDisplay: false);
			memory += ans;
			UpdateStatus();
			break;
		case "MR":
			InsertText(CalcEngine.FormatNumber(memory));
			break;
		case "MC":
			memory = 0.0;
			UpdateStatus();
			break;
		default:
			InsertText(key);
			break;
		}
	}

	private void HandleGraphPad(string key)
	{
		checked
		{
			switch (key)
			{
			case "C":
				txtY1.Text = "";
				txtY1.Focus();
				txtY1.SelectionStart = 0;
				break;
			case "BKSP":
			{
				string text = txtY1.Text;
				int selectionStart = txtY1.SelectionStart;
				if (txtY1.SelectionLength > 0)
				{
					txtY1.Text = text.Remove(txtY1.SelectionStart, txtY1.SelectionLength);
					txtY1.SelectionStart = selectionStart;
				}
				else if (selectionStart > 0 && text.Length > 0)
				{
					txtY1.Text = text.Remove(selectionStart - 1, 1);
					txtY1.SelectionStart = selectionStart - 1;
				}
				txtY1.Focus();
				break;
			}
			case "PLOT":
			case "=":
				PlotGraph();
				break;
			case "π":
				InsertIntoY1("pi");
				break;
			case "e":
				InsertIntoY1("e");
				break;
			case "x":
				InsertIntoY1("x");
				break;
			case "x²":
				InsertIntoY1("^2");
				break;
			default:
				InsertIntoY1(key);
				break;
			}
		}
	}

	private void InsertIntoY1(string text)
	{
		string text2 = txtY1.Text;
		string text3 = ((text2 != null) ? text2 : "");
		int num = txtY1.SelectionStart;
		int num2 = txtY1.SelectionLength;
		if (num < 0)
		{
			num = text3.Length;
		}
		if (num2 < 0)
		{
			num2 = 0;
		}
		checked
		{
			if (num + num2 > text3.Length)
			{
				num2 = Math.Max(0, text3.Length - num);
			}
			txtY1.Text = text3.Substring(0, num) + text + text3.Substring(num + num2);
			txtY1.SelectionStart = num + text.Length;
			txtY1.SelectionLength = 0;
			txtY1.Focus();
		}
	}

	private void InsertFn(string name)
	{
		InsertText(name + "(");
	}

	private void InsertText(string text)
	{
		expr += text;
		RefreshDisplay();
	}

	private void EvaluateExpr(bool updateDisplay = true)
	{
		string expression = ((Operators.CompareString(expr, "", TextCompare: false) == 0) ? CalcEngine.FormatNumber(ans) : expr);
		CalcEngine.EvalResult evalResult = CalcEngine.Evaluate(expression, angleMode);
		if (!evalResult.Ok)
		{
			if (updateDisplay)
			{
				lblDisplay.Text = "ERROR";
			}
			lblStatus.Text = "ERR: " + evalResult.ErrorMessage.ToUpperInvariant();
			return;
		}
		ans = evalResult.Value;
		expr = "";
		if (updateDisplay)
		{
			lblDisplay.Text = CalcEngine.FormatNumber(ans);
			UpdateStatus();
		}
	}

	private void RefreshDisplay()
	{
		if (Operators.CompareString(expr, "", TextCompare: false) == 0)
		{
			lblDisplay.Text = CalcEngine.FormatNumber(ans);
		}
		else
		{
			lblDisplay.Text = expr;
		}
		UpdateStatus();
	}

	private void UpdateStatus()
	{
		string text = ((angleMode == CalcEngine.AngleMode.Degrees) ? "DEG" : "RAD");
		lblStatus.Text = text + "  ·  ANS=" + CalcEngine.FormatNumber(ans) + "  ·  M=" + CalcEngine.FormatNumber(memory);
	}

	private void OnFormKeyDown(object sender, KeyEventArgs e)
	{
		if (currentMode == CalcUiMode.Graph)
		{
			if (e.KeyCode == Keys.Return || e.KeyCode == Keys.Return)
			{
				PlotGraph();
				e.Handled = true;
			}
		}
		else if (e.KeyCode == Keys.Return || e.KeyCode == Keys.Return)
		{
			EvaluateExpr();
			e.Handled = true;
		}
		else if (e.KeyCode == Keys.Escape)
		{
			expr = "";
			RefreshDisplay();
			e.Handled = true;
		}
		else if (e.KeyCode == Keys.Back)
		{
			HandlePad("BKSP");
			e.Handled = true;
		}
	}

	private void OnFormKeyPress(object sender, KeyPressEventArgs e)
	{
		if (currentMode != CalcUiMode.Graph)
		{
			char keyChar = e.KeyChar;
			if (char.IsDigit(keyChar) || keyChar == '.' || "+-*/^()".IndexOf(keyChar) >= 0)
			{
				InsertText(keyChar.ToString());
				e.Handled = true;
			}
			else if (keyChar == '=' || keyChar == '\r')
			{
				EvaluateExpr();
				e.Handled = true;
			}
		}
	}

	private void ReadWindow()
	{
		double.TryParse(txtXmin.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out xmin);
		double.TryParse(txtXmax.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out xmax);
		double.TryParse(txtYmin.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out ymin);
		double.TryParse(txtYmax.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out ymax);
		if (xmax <= xmin)
		{
			xmax = xmin + 1.0;
		}
		if (ymax <= ymin)
		{
			ymax = ymin + 1.0;
		}
		string text = txtY1.Text;
		y1Expr = ((text != null) ? text : "").Trim();
		picGraph.SetWindow(xmin, xmax, ymin, ymax);
	}

	private void ZoomStandard()
	{
		xmin = -10.0;
		xmax = 10.0;
		ymin = -10.0;
		ymax = 10.0;
		SyncWindowBoxes();
		picGraph.SetWindow(xmin, xmax, ymin, ymax);
		PlotGraph();
	}

	private void ZoomFit()
	{
		ReadWindow();
		if (Operators.CompareString(y1Expr, "", TextCompare: false) == 0)
		{
			return;
		}
		List<double> list = new List<double>();
		int num = 200;
		int num2 = num;
		for (int i = 0; i <= num2; i = checked(i + 1))
		{
			double num3 = xmin + (xmax - xmin) * (double)i / (double)num;
			CalcEngine.EvalResult evalResult = CalcEngine.Evaluate(y1Expr, angleMode, num3);
			if (evalResult.Ok && !double.IsNaN(evalResult.Value) && !double.IsInfinity(evalResult.Value))
			{
				list.Add(evalResult.Value);
			}
		}
		if (list.Count == 0)
		{
			return;
		}
		ymin = list[0];
		ymax = list[0];
		foreach (double item in list)
		{
			if (item < ymin)
			{
				ymin = item;
			}
			if (item > ymax)
			{
				ymax = item;
			}
		}
		double num4 = (ymax - ymin) * 0.1;
		if (num4 < 0.0001)
		{
			num4 = 1.0;
		}
		ymin -= num4;
		ymax += num4;
		SyncWindowBoxes();
		picGraph.SetWindow(xmin, xmax, ymin, ymax);
		PlotGraph();
	}

	private void SyncWindowBoxes()
	{
		txtXmin.Text = xmin.ToString(CultureInfo.InvariantCulture);
		txtXmax.Text = xmax.ToString(CultureInfo.InvariantCulture);
		txtYmin.Text = ymin.ToString(CultureInfo.InvariantCulture);
		txtYmax.Text = ymax.ToString(CultureInfo.InvariantCulture);
		txtY1.Text = y1Expr;
	}

	private void PicGraph_WindowChanged(object sender, EventArgs e)
	{
		picGraph.GetWindow(ref xmin, ref xmax, ref ymin, ref ymax);
		SyncWindowBoxes();
		RecomputeCurve();
		UpdateTraceLabel();
		picGraph.Invalidate();
	}

	private void PicGraph_TraceRequested(object sender, Point location)
	{
		traceX = picGraph.UnmapX(location.X);
		UpdateTraceLabel();
		picGraph.Invalidate();
	}

	private void PlotGraph()
	{
		ReadWindow();
		SyncWindowBoxes();
		RecomputeCurve();
		if (graphOk || Operators.CompareString(y1Expr, "", TextCompare: false) == 0)
		{
			traceX = (xmin + xmax) / 2.0;
		}
		UpdateTraceLabel();
		picGraph.Invalidate();
	}

	private void RecomputeCurve()
	{
		graphPoints.Clear();
		graphOk = false;
		if (Operators.CompareString(y1Expr, "", TextCompare: false) == 0)
		{
			return;
		}
		int val = Math.Max(10, picGraph.ClientSize.Width);
		int num = Math.Max(80, val);
		bool flag = false;
		int num2 = num;
		for (int i = 0; i <= num2; i = checked(i + 1))
		{
			double num3 = xmin + (xmax - xmin) * (double)i / (double)num;
			CalcEngine.EvalResult evalResult = CalcEngine.Evaluate(y1Expr, angleMode, num3);
			if (evalResult.Ok && !double.IsNaN(evalResult.Value) && !double.IsInfinity(evalResult.Value))
			{
				List<PointF> list = graphPoints;
				PointF item = new PointF((float)num3, (float)evalResult.Value);
				list.Add(item);
				flag = true;
				graphOk = true;
			}
			else if (flag)
			{
				List<PointF> list2 = graphPoints;
				PointF item = new PointF(float.NaN, float.NaN);
				list2.Add(item);
				flag = false;
			}
		}
	}

	private void PicGraph_Paint(object sender, PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		graphics.Clear(Color.FromArgb(10, 10, 16));
		int num = picGraph.ClientSize.Width;
		int num2 = picGraph.ClientSize.Height;
		if (num < 2 || num2 < 2)
		{
			return;
		}
		using (Pen pen = new Pen(Color.FromArgb(40, 60, 80), 1f))
		{
			int num3 = MapX(0.0);
			int num4 = MapY(0.0);
			if (num3 >= 0 && num3 < num)
			{
				graphics.DrawLine(pen, num3, 0, num3, num2);
			}
			if (num4 >= 0 && num4 < num2)
			{
				graphics.DrawLine(pen, 0, num4, num, num4);
			}
		}
		checked
		{
			if (graphOk && graphPoints.Count > 1)
			{
				using Pen pen2 = new Pen(Color.Orange, 2f);
				PointF pointF = new PointF(float.NaN, float.NaN);
				foreach (PointF graphPoint in graphPoints)
				{
					if (float.IsNaN(graphPoint.X) || float.IsNaN(graphPoint.Y) || float.IsInfinity(graphPoint.X) || float.IsInfinity(graphPoint.Y))
					{
						pointF = new PointF(float.NaN, float.NaN);
						continue;
					}
					int x = MapX(graphPoint.X);
					int num5 = MapY(graphPoint.Y);
					if (!float.IsNaN(pointF.X) && !float.IsInfinity(pointF.X))
					{
						int x2 = MapX(pointF.X);
						int num6 = MapY(pointF.Y);
						if (Math.Abs(num5 - num6) < num2 * 2)
						{
							graphics.DrawLine(pen2, x2, num6, x, num5);
						}
					}
					pointF = graphPoint;
				}
			}
			int num7 = MapX(traceX);
			double v = TraceY();
			using Pen pen3 = new Pen(Color.FromArgb(153, 204, 255), 1f);
			graphics.DrawLine(pen3, num7, 0, num7, num2);
			if (IsFinite(v))
			{
				int num8 = MapY(v);
				graphics.DrawEllipse(pen3, num7 - 4, num8 - 4, 8, 8);
			}
		}
	}

	private int MapX(double x)
	{
		int num = picGraph.ClientSize.Width;
		if (num < 2 || !IsFinite(x) || !IsFinite(xmin) || !IsFinite(xmax) || xmax == xmin)
		{
			return 0;
		}
		return ClampToInt((x - xmin) / (xmax - xmin) * (double)checked(num - 1));
	}

	private int MapY(double y)
	{
		int num = picGraph.ClientSize.Height;
		if (num < 2 || !IsFinite(y) || !IsFinite(ymin) || !IsFinite(ymax) || ymax == ymin)
		{
			return 0;
		}
		return ClampToInt((ymax - y) / (ymax - ymin) * (double)checked(num - 1));
	}

	private static bool IsFinite(double v)
	{
		return (!double.IsNaN(v) && !double.IsInfinity(v)) ? true : false;
	}

	private static int ClampToInt(double v)
	{
		if (double.IsNaN(v) || double.IsInfinity(v))
		{
			return 0;
		}
		if (v >= 2147483647.0)
		{
			return int.MaxValue;
		}
		if (v <= -2147483648.0)
		{
			return int.MinValue;
		}
		return checked((int)Math.Round(Math.Round(v)));
	}

	private double TraceY()
	{
		CalcEngine.EvalResult evalResult = CalcEngine.Evaluate(y1Expr, angleMode, traceX);
		if (evalResult.Ok)
		{
			return evalResult.Value;
		}
		return double.NaN;
	}

	private void UpdateTraceLabel()
	{
		double num = TraceY();
		if (double.IsNaN(num))
		{
			lblTrace.Text = "TRACE  X=" + CalcEngine.FormatNumber(traceX) + "  Y=—  ·  pan/pinch/wheel";
		}
		else
		{
			lblTrace.Text = "TRACE  X=" + CalcEngine.FormatNumber(traceX) + "  Y=" + CalcEngine.FormatNumber(num);
		}
	}

	private void NudgeTrace(int dir)
	{
		double num = (xmax - xmin) / (double)Math.Max(40, picGraph.ClientSize.Width);
		traceX += (double)dir * num;
		if (traceX < xmin)
		{
			traceX = xmin;
		}
		if (traceX > xmax)
		{
			traceX = xmax;
		}
		UpdateTraceLabel();
		picGraph.Invalidate();
	}

	private void ShowTable()
	{
		ReadWindow();
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("X\tY1=" + y1Expr);
		int num = 13;
		checked
		{
			int num2 = num - 1;
			for (int i = 0; i <= num2; i++)
			{
				double value = xmin + (xmax - xmin) * (double)i / (double)(num - 1);
				CalcEngine.EvalResult evalResult = CalcEngine.Evaluate(y1Expr, angleMode, value);
				string text = (evalResult.Ok ? CalcEngine.FormatNumber(evalResult.Value) : "ERR");
				stringBuilder.AppendLine(CalcEngine.FormatNumber(value) + "\t" + text);
			}
			MessageBox.Show(stringBuilder.ToString(), "TABLE", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
		}
	}
}
