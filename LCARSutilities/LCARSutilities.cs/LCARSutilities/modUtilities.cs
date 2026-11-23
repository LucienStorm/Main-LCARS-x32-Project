using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using LCARS;
using LCARS.Controls;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSutilities;

[StandardModule]
public sealed class modUtilities
{
	internal const string UtilityFontName = "Arial Narrow";

	internal const int UtilityMargin = 12;

	internal const int UtilityButtonHeight = 28;

	private const int CascadeStep = 40;

	private static int cascadeIndex = 0;

	private static Form dragForm = null;

	private static Point dragGrab = Point.Empty;

	private static bool dragActive = false;

	internal static void InitUtilityFormFloating(LCARSForm frm, int clientWidth, int clientHeight)
	{
		frm.Text = "LCARS";
		frm.BackColor = Color.Black;
		frm.FormBorderStyle = FormBorderStyle.None;
		frm.ControlBox = false;
		frm.MaximizeBox = false;
		frm.MinimizeBox = false;
		frm.ShowInTaskbar = true;
		frm.TopMost = false;
		frm.StartPosition = FormStartPosition.Manual;
		frm.BindToWorkingArea = false;
		frm.RegisterWithLcarsShellEnabled = true;
		frm.WindowState = FormWindowState.Normal;
		Size clientSize = new Size(Math.Max(200, clientWidth), Math.Max(160, clientHeight));
		frm.ClientSize = clientSize;
		Rectangle workingArea = Screen.FromPoint(Cursor.Position).WorkingArea;
		checked
		{
			int num = workingArea.Left + Math.Max(0, unchecked(checked(workingArea.Width - frm.Width) / 2));
			int num2 = workingArea.Top + Math.Max(0, unchecked(checked(workingArea.Height - frm.Height) / 2));
			int num3 = unchecked(cascadeIndex % 8);
			cascadeIndex++;
			int num4 = num + num3 * 40;
			int num5 = num2 + num3 * 40;
			if (num4 + frm.Width > workingArea.Right)
			{
				num4 = Math.Max(workingArea.Left, workingArea.Right - frm.Width);
			}
			if (num5 + frm.Height > workingArea.Bottom)
			{
				num5 = Math.Max(workingArea.Top, workingArea.Bottom - frm.Height);
			}
			Point location = new Point(num4, num5);
			frm.Location = location;
		}
	}

	internal static void InitUtilityForm(LCARSForm frm, int clientWidth, int clientHeight)
	{
		InitUtilityFormFloating(frm, clientWidth, clientHeight);
	}

	internal static void WireUtilityTitleDrag(Control title, Form frm)
	{
		if (title != null && frm != null)
		{
			title.MouseDown += TitleDrag_MouseDown;
			title.MouseMove += TitleDrag_MouseMove;
			title.MouseUp += TitleDrag_MouseUp;
			title.Tag = frm;
		}
	}

	private static void TitleDrag_MouseDown(object sender, MouseEventArgs e)
	{
		if (e.Button != MouseButtons.Left || !(sender is Control control))
		{
			return;
		}
		Form form = control.Tag as Form;
		if (form == null)
		{
			form = control.FindForm();
		}
		if (form == null)
		{
			return;
		}
		dragForm = form;
		dragGrab = checked(new Point(Control.MousePosition.X - form.Left, Control.MousePosition.Y - form.Top));
		dragActive = true;
		try
		{
			control.Capture = true;
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
	}

	private static void TitleDrag_MouseMove(object sender, MouseEventArgs e)
	{
		if (!dragActive || dragForm == null)
		{
			return;
		}
		if ((Control.MouseButtons & MouseButtons.Left) == 0)
		{
			TitleDrag_MouseUp(RuntimeHelpers.GetObjectValue(sender), e);
			return;
		}
		checked
		{
			int num = Control.MousePosition.X - dragGrab.X;
			int num2 = Control.MousePosition.Y - dragGrab.Y;
			Rectangle workingArea = Screen.FromControl(dragForm).WorkingArea;
			if (num + dragForm.Width < workingArea.Left + 40)
			{
				num = workingArea.Left + 40 - dragForm.Width;
			}
			if (num2 < workingArea.Top)
			{
				num2 = workingArea.Top;
			}
			if (num > workingArea.Right - 40)
			{
				num = workingArea.Right - 40;
			}
			if (num2 > workingArea.Bottom - 40)
			{
				num2 = workingArea.Bottom - 40;
			}
			Form form = dragForm;
			Point location = new Point(num, num2);
			form.Location = location;
		}
	}

	private static void TitleDrag_MouseUp(object sender, MouseEventArgs e)
	{
		dragActive = false;
		dragForm = null;
		if (sender is Control control)
		{
			try
			{
				control.Capture = false;
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}
	}

	internal static FlatButton MakeUtilityTitle(Control parent, string text, int width)
	{
		FlatButton flatButton = new FlatButton();
		flatButton.ButtonText = text;
		flatButton.Text = text;
		flatButton.Clickable = true;
		flatButton.Beeping = false;
		flatButton.Color = LCARScolorStyles.StaticTan;
		flatButton.ButtonTextAlign = ContentAlignment.MiddleLeft;
		Point location = new Point(12, 12);
		flatButton.Location = location;
		Size size = new Size(width, 28);
		flatButton.Size = size;
		parent.Controls.Add(flatButton);
		Form form = parent as Form;
		if (form == null && parent != null)
		{
			form = parent.FindForm();
		}
		if (form != null)
		{
			WireUtilityTitleDrag(flatButton, form);
		}
		return flatButton;
	}

	internal static FlatButton MakeUtilityButton(Control parent, string text, int x, int y, int width, int height, LCARScolorStyles buttonColor, EventHandler handler)
	{
		FlatButton flatButton = new FlatButton();
		flatButton.ButtonText = text;
		flatButton.Text = text;
		flatButton.Clickable = true;
		flatButton.Beeping = true;
		flatButton.Color = buttonColor;
		Point location = new Point(x, y);
		flatButton.Location = location;
		Size size = new Size(width, height);
		flatButton.Size = size;
		flatButton.Click += handler;
		parent.Controls.Add(flatButton);
		return flatButton;
	}

	internal static Label MakeUtilityLabel(Control parent, int x, int y, int width, int height, float fontSize)
	{
		Label label = new Label();
		label.ForeColor = Color.Orange;
		label.BackColor = Color.Black;
		label.Font = new Font("Arial Narrow", fontSize, FontStyle.Regular, GraphicsUnit.Point);
		Point location = new Point(x, y);
		label.Location = location;
		Size size = new Size(width, height);
		label.Size = size;
		parent.Controls.Add(label);
		return label;
	}

	internal static void OpenUrlInLcarsBrowser(string url)
	{
		if (string.IsNullOrEmpty(url))
		{
			return;
		}
		string text = Path.Combine(Application.StartupPath, "LCARSWebBrowser.exe");
		try
		{
			if (File.Exists(text))
			{
				ProcessStartInfo processStartInfo = new ProcessStartInfo();
				processStartInfo.FileName = text;
				processStartInfo.Arguments = "\"" + url + "\"";
				processStartInfo.WorkingDirectory = Application.StartupPath;
				processStartInfo.UseShellExecute = true;
				Process.Start(processStartInfo);
			}
			else
			{
				ProcessStartInfo processStartInfo2 = new ProcessStartInfo();
				processStartInfo2.FileName = url;
				processStartInfo2.UseShellExecute = true;
				Process.Start(processStartInfo2);
			}
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
	}
}
