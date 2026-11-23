using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSutilities;

[StandardModule]
public sealed class CalcEngine
{
	public enum AngleMode
	{
		Degrees,
		Radians
	}

	public class EvalResult
	{
		public bool Ok;

		public double Value;

		public string ErrorMessage;

		public EvalResult()
		{
			ErrorMessage = "";
		}
	}

	private class Tok
	{
		public string Kind;

		public string Text;

		public double Num;

		[DebuggerNonUserCode]
		public Tok()
		{
		}
	}

	public static EvalResult Evaluate(string expression, AngleMode mode, double x = double.NaN)
	{
		EvalResult evalResult = new EvalResult();
		EvalResult result;
		try
		{
			List<Tok> list = Tokenize((expression != null) ? expression : "");
			if (list.Count == 0)
			{
				evalResult.ErrorMessage = "empty";
				result = evalResult;
			}
			else
			{
				int pos = 0;
				double num = ParseExpr(list, ref pos, mode, x);
				if (pos < list.Count)
				{
					evalResult.ErrorMessage = "syntax";
					result = evalResult;
				}
				else if (double.IsNaN(num) || double.IsInfinity(num))
				{
					evalResult.ErrorMessage = "domain";
					result = evalResult;
				}
				else
				{
					evalResult.Ok = true;
					evalResult.Value = num;
					result = evalResult;
				}
			}
		}
		catch (Exception ex)
		{
			ProjectData.SetProjectError(ex);
			Exception ex2 = ex;
			evalResult.ErrorMessage = (string.IsNullOrEmpty(ex2.Message) ? "error" : ex2.Message);
			result = evalResult;
			ProjectData.ClearProjectError();
		}
		return result;
	}

	public static string FormatNumber(double value)
	{
		if (double.IsNaN(value) || double.IsInfinity(value))
		{
			return "ERROR";
		}
		double num = Math.Abs(value);
		if (num != 0.0 && (num >= 10000000000.0 || num < 1E-10))
		{
			return value.ToString("0.########E+0", CultureInfo.InvariantCulture);
		}
		return value.ToString("0.############", CultureInfo.InvariantCulture);
	}

	private static List<Tok> Tokenize(string src)
	{
		List<Tok> list = new List<Tok>();
		int i = 0;
		string text = src.Trim().ToLowerInvariant().Replace(" ", "");
		text = text.Replace("×", "*").Replace("÷", "/").Replace("π", "pi");
		checked
		{
			while (i < text.Length)
			{
				char c = text[i];
				if (char.IsDigit(c) || c == '.')
				{
					int num = i;
					for (i++; i < text.Length && (char.IsDigit(text[i]) || text[i] == '.'); i++)
					{
					}
					if (i < text.Length && (text[i] == 'e' || text[i] == 'E'))
					{
						int num2 = i;
						i++;
						if (i < text.Length && (text[i] == '+' || text[i] == '-'))
						{
							i++;
						}
						bool flag = false;
						for (; i < text.Length && char.IsDigit(text[i]); i++)
						{
							flag = true;
						}
						if (!flag)
						{
							i = num2;
						}
					}
					string text2 = text.Substring(num, i - num);
					if (!double.TryParse(text2, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
					{
						throw new Exception("bad number");
					}
					Tok tok = new Tok();
					tok.Kind = "num";
					tok.Text = text2;
					tok.Num = result;
					list.Add(tok);
					continue;
				}
				if (!char.IsLetter(c))
				{
					switch (c)
					{
					case '_':
						break;
					case '(':
						list.Add(MakeTok("lp", "("));
						i++;
						continue;
					case ')':
						list.Add(MakeTok("rp", ")"));
						i++;
						continue;
					case ',':
						list.Add(MakeTok("comma", ","));
						i++;
						continue;
					default:
						if ("^*/+-".IndexOf(c) >= 0)
						{
							list.Add(MakeTok("op", c.ToString()));
							i++;
							continue;
						}
						throw new Exception("bad char");
					}
				}
				int num3 = i;
				for (i++; i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '_'); i++)
				{
				}
				string text3 = text.Substring(num3, i - num3);
				Tok tok2 = new Tok();
				tok2.Kind = "id";
				tok2.Text = text3;
				list.Add(tok2);
			}
			return list;
		}
	}

	private static Tok MakeTok(string kind, string text)
	{
		Tok tok = new Tok();
		tok.Kind = kind;
		tok.Text = text;
		return tok;
	}

	private static double ParseExpr(List<Tok> tokens, ref int pos, AngleMode mode, double x)
	{
		double num = ParseTerm(tokens, ref pos, mode, x);
		checked
		{
			while (pos < tokens.Count && Operators.CompareString(tokens[pos].Kind, "op", TextCompare: false) == 0 && (Operators.CompareString(tokens[pos].Text, "+", TextCompare: false) == 0 || Operators.CompareString(tokens[pos].Text, "-", TextCompare: false) == 0))
			{
				string text = tokens[pos].Text;
				pos++;
				double num2 = ParseTerm(tokens, ref pos, mode, x);
				num = ((Operators.CompareString(text, "+", TextCompare: false) != 0) ? (num - num2) : (num + num2));
			}
			return num;
		}
	}

	private static double ParseTerm(List<Tok> tokens, ref int pos, AngleMode mode, double x)
	{
		double num = ParsePower(tokens, ref pos, mode, x);
		checked
		{
			while (pos < tokens.Count && Operators.CompareString(tokens[pos].Kind, "op", TextCompare: false) == 0 && (Operators.CompareString(tokens[pos].Text, "*", TextCompare: false) == 0 || Operators.CompareString(tokens[pos].Text, "/", TextCompare: false) == 0))
			{
				string text = tokens[pos].Text;
				pos++;
				double num2 = ParsePower(tokens, ref pos, mode, x);
				if (Operators.CompareString(text, "*", TextCompare: false) == 0)
				{
					num *= num2;
					continue;
				}
				if (num2 == 0.0)
				{
					throw new Exception("div0");
				}
				num /= num2;
			}
			return num;
		}
	}

	private static double ParsePower(List<Tok> tokens, ref int pos, AngleMode mode, double x)
	{
		double num = ParseUnary(tokens, ref pos, mode, x);
		checked
		{
			if (pos < tokens.Count && Operators.CompareString(tokens[pos].Kind, "op", TextCompare: false) == 0 && Operators.CompareString(tokens[pos].Text, "^", TextCompare: false) == 0)
			{
				pos++;
				double y = ParsePower(tokens, ref pos, mode, x);
				num = Math.Pow(num, y);
			}
			return num;
		}
	}

	private static double ParseUnary(List<Tok> tokens, ref int pos, AngleMode mode, double x)
	{
		if (pos >= tokens.Count)
		{
			throw new Exception("syntax");
		}
		checked
		{
			if (Operators.CompareString(tokens[pos].Kind, "op", TextCompare: false) == 0 && Operators.CompareString(tokens[pos].Text, "+", TextCompare: false) == 0)
			{
				pos++;
				return ParseUnary(tokens, ref pos, mode, x);
			}
			if (Operators.CompareString(tokens[pos].Kind, "op", TextCompare: false) == 0 && Operators.CompareString(tokens[pos].Text, "-", TextCompare: false) == 0)
			{
				pos++;
				return 0.0 - ParseUnary(tokens, ref pos, mode, x);
			}
			return ParsePrimary(tokens, ref pos, mode, x);
		}
	}

	private static double ParsePrimary(List<Tok> tokens, ref int pos, AngleMode mode, double x)
	{
		if (pos >= tokens.Count)
		{
			throw new Exception("syntax");
		}
		Tok tok = tokens[pos];
		checked
		{
			if (Operators.CompareString(tok.Kind, "num", TextCompare: false) == 0)
			{
				pos++;
				return tok.Num;
			}
			if (Operators.CompareString(tok.Kind, "lp", TextCompare: false) == 0)
			{
				pos++;
				double result = ParseExpr(tokens, ref pos, mode, x);
				if (pos >= tokens.Count || Operators.CompareString(tokens[pos].Kind, "rp", TextCompare: false) != 0)
				{
					throw new Exception("paren");
				}
				pos++;
				return result;
			}
			if (Operators.CompareString(tok.Kind, "id", TextCompare: false) == 0)
			{
				pos++;
				string text = tok.Text;
				switch (text)
				{
				case "x":
					if (double.IsNaN(x))
					{
						throw new Exception("no x");
					}
					return x;
				case "pi":
					return Math.PI;
				case "e":
					return Math.E;
				default:
				{
					if (pos >= tokens.Count || Operators.CompareString(tokens[pos].Kind, "lp", TextCompare: false) != 0)
					{
						throw new Exception("fn");
					}
					pos++;
					List<double> list = new List<double>();
					if (pos < tokens.Count && Operators.CompareString(tokens[pos].Kind, "rp", TextCompare: false) != 0)
					{
						list.Add(ParseExpr(tokens, ref pos, mode, x));
						while (pos < tokens.Count && Operators.CompareString(tokens[pos].Kind, "comma", TextCompare: false) == 0)
						{
							pos++;
							list.Add(ParseExpr(tokens, ref pos, mode, x));
						}
					}
					if (pos >= tokens.Count || Operators.CompareString(tokens[pos].Kind, "rp", TextCompare: false) != 0)
					{
						throw new Exception("paren");
					}
					pos++;
					return CallFn(text, list, mode);
				}
				}
			}
			throw new Exception("syntax");
		}
	}

	private static double CallFn(string name, List<double> args, AngleMode mode)
	{
		switch (name)
		{
		case "sin":
			RequireArgs(args, 1);
			return Math.Sin(ToRad(args[0], mode));
		case "cos":
			RequireArgs(args, 1);
			return Math.Cos(ToRad(args[0], mode));
		case "tan":
			RequireArgs(args, 1);
			return Math.Tan(ToRad(args[0], mode));
		case "asin":
		case "arcsin":
			RequireArgs(args, 1);
			return FromRad(Math.Asin(args[0]), mode);
		case "acos":
		case "arccos":
			RequireArgs(args, 1);
			return FromRad(Math.Acos(args[0]), mode);
		case "atan":
		case "arctan":
			RequireArgs(args, 1);
			return FromRad(Math.Atan(args[0]), mode);
		case "sinh":
			RequireArgs(args, 1);
			return Math.Sinh(args[0]);
		case "cosh":
			RequireArgs(args, 1);
			return Math.Cosh(args[0]);
		case "tanh":
			RequireArgs(args, 1);
			return Math.Tanh(args[0]);
		case "ln":
		case "log":
			RequireArgs(args, 1);
			if (args[0] <= 0.0)
			{
				throw new Exception("domain");
			}
			return Math.Log(args[0]);
		case "log10":
			RequireArgs(args, 1);
			if (args[0] <= 0.0)
			{
				throw new Exception("domain");
			}
			return Math.Log10(args[0]);
		case "exp":
			RequireArgs(args, 1);
			return Math.Exp(args[0]);
		case "sqrt":
			RequireArgs(args, 1);
			if (args[0] < 0.0)
			{
				throw new Exception("domain");
			}
			return Math.Sqrt(args[0]);
		case "cbrt":
			RequireArgs(args, 1);
			return Math.Pow(args[0], 1.0 / 3.0);
		case "abs":
			RequireArgs(args, 1);
			return Math.Abs(args[0]);
		case "floor":
			RequireArgs(args, 1);
			return Math.Floor(args[0]);
		case "ceil":
		case "ceiling":
			RequireArgs(args, 1);
			return Math.Ceiling(args[0]);
		case "round":
			RequireArgs(args, 1);
			return Math.Round(args[0], MidpointRounding.AwayFromZero);
		case "fact":
		case "factorial":
			RequireArgs(args, 1);
			return Factorial(args[0]);
		case "pow":
			RequireArgs(args, 2);
			return Math.Pow(args[0], args[1]);
		case "npr":
		case "permut":
			RequireArgs(args, 2);
			return Permutation(args[0], args[1]);
		case "ncr":
		case "combin":
			RequireArgs(args, 2);
			return Combination(args[0], args[1]);
		case "min":
		{
			if (args.Count < 1)
			{
				throw new Exception("args");
			}
			double num2 = args[0];
			foreach (double arg in args)
			{
				if (arg < num2)
				{
					num2 = arg;
				}
			}
			return num2;
		}
		case "max":
		{
			if (args.Count < 1)
			{
				throw new Exception("args");
			}
			double num = args[0];
			foreach (double arg2 in args)
			{
				if (arg2 > num)
				{
					num = arg2;
				}
			}
			return num;
		}
		default:
			throw new Exception("unknown fn");
		}
	}

	private static void RequireArgs(List<double> args, int n)
	{
		if (args == null || args.Count != n)
		{
			throw new Exception("args");
		}
	}

	private static double ToRad(double v, AngleMode mode)
	{
		if (mode == AngleMode.Radians)
		{
			return v;
		}
		return v * Math.PI / 180.0;
	}

	private static double FromRad(double v, AngleMode mode)
	{
		if (mode == AngleMode.Radians)
		{
			return v;
		}
		return v * 180.0 / Math.PI;
	}

	private static double Factorial(double n)
	{
		if (n < 0.0 || n != Math.Floor(n) || n > 170.0)
		{
			throw new Exception("domain");
		}
		double num = 1.0;
		checked
		{
			int num2 = (int)Math.Round(n);
			for (int i = 2; i <= num2; i++)
			{
				num *= (double)i;
			}
			return num;
		}
	}

	private static double Permutation(double n, double r)
	{
		if (n < 0.0 || r < 0.0 || n != Math.Floor(n) || r != Math.Floor(r) || r > n)
		{
			throw new Exception("domain");
		}
		double num = 1.0;
		checked
		{
			int num2 = (int)Math.Round(r) - 1;
			for (int i = 0; i <= num2; i++)
			{
				num *= n - (double)i;
			}
			return num;
		}
	}

	private static double Combination(double n, double r)
	{
		if (n < 0.0 || r < 0.0 || n != Math.Floor(n) || r != Math.Floor(r) || r > n)
		{
			throw new Exception("domain");
		}
		r = Math.Min(r, n - r);
		double num = 1.0;
		checked
		{
			int num2 = (int)Math.Round(r);
			for (int i = 1; i <= num2; i++)
			{
				num = num * (n - r + (double)i) / (double)i;
			}
			return Math.Round(num);
		}
	}
}
