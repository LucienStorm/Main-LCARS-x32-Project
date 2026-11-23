using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Forms;
using LCARS;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSutilities;

public class frmScreenshot : LCARSForm
{
	private const int FormWidth = 520;

	private const int FormHeight = 420;

	private const string ShotFolderName = "LCARS";

	private const string ShotFileFormat = "'lcars-shot-'yyyyMMdd-HHmmss'.png'";

	private const int HideBeforeCaptureMs = 250;

	private PictureBox picPreview;

	private Label lblStatus;

	private string lastSavedPath;

	public frmScreenshot()
	{
		lastSavedPath = "";
		modUtilities.InitUtilityForm(this, 520, 420);
		BuildUi();
	}

	private void BuildUi()
	{
		int num = 12;
		int num2 = checked(520 - num * 2);
		modUtilities.MakeUtilityTitle(this, "SCREENSHOT", num2);
		picPreview = new PictureBox();
		picPreview.BackColor = Color.Black;
		picPreview.BorderStyle = BorderStyle.FixedSingle;
		picPreview.SizeMode = PictureBoxSizeMode.Zoom;
		PictureBox pictureBox = picPreview;
		Point location = new Point(num, 48);
		pictureBox.Location = location;
		PictureBox pictureBox2 = picPreview;
		Size size = new Size(num2, 240);
		pictureBox2.Size = size;
		Controls.Add(picPreview);
		lblStatus = modUtilities.MakeUtilityLabel(this, num, 296, num2, 28, 14f);
		lblStatus.Text = "READY";
		int num3 = checked(num2 - 12) / 3;
		modUtilities.MakeUtilityButton(this, "CAPTURE SCREEN", num, 336, num3, 40, LCARScolorStyles.PrimaryFunction, [SpecialName] (object s, EventArgs e) =>
		{
			CaptureScreen();
		});
		checked
		{
			modUtilities.MakeUtilityButton(this, "OPEN FOLDER", num + num3 + 6, 336, num3, 40, LCARScolorStyles.NavigationFunction, [SpecialName] (object s, EventArgs e) =>
			{
				OpenFolder();
			});
			modUtilities.MakeUtilityButton(this, "CLOSE", num + (num3 + 6) * 2, 336, num3, 40, LCARScolorStyles.FunctionOffline, [SpecialName] (object s, EventArgs e) =>
			{
				Close();
			});
		}
	}

	private static string ShotFolder()
	{
		return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "LCARS");
	}

	private void CaptureScreen()
	{
		Rectangle bounds = Screen.FromControl(this).Bounds;
		Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
		try
		{
			Opacity = 0.0;
			Application.DoEvents();
			Thread.Sleep(250);
			using Graphics graphics = Graphics.FromImage(bitmap);
			graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
		}
		catch (Exception ex)
		{
			ProjectData.SetProjectError(ex);
			Exception ex2 = ex;
			bitmap.Dispose();
			lblStatus.Text = "CAPTURE FAILED";
			ProjectData.ClearProjectError();
			return;
		}
		finally
		{
			Opacity = 1.0;
		}
		try
		{
			Directory.CreateDirectory(ShotFolder());
			string text = Path.Combine(ShotFolder(), DateTime.Now.ToString("'lcars-shot-'yyyyMMdd-HHmmss'.png'"));
			bitmap.Save(text, ImageFormat.Png);
			lastSavedPath = text;
			lblStatus.Text = "SAVED " + Path.GetFileName(text);
		}
		catch (Exception ex3)
		{
			ProjectData.SetProjectError(ex3);
			Exception ex4 = ex3;
			lblStatus.Text = "SAVE FAILED";
			ProjectData.ClearProjectError();
		}
		Image image = picPreview.Image;
		picPreview.Image = bitmap;
		image?.Dispose();
	}

	private void OpenFolder()
	{
		try
		{
			Directory.CreateDirectory(ShotFolder());
			if (Operators.CompareString(lastSavedPath, "", TextCompare: false) != 0 && File.Exists(lastSavedPath))
			{
				Process.Start("explorer.exe", "/select,\"" + lastSavedPath + "\"");
			}
			else
			{
				Process.Start("explorer.exe", "\"" + ShotFolder() + "\"");
			}
		}
		catch (Exception ex)
		{
			ProjectData.SetProjectError(ex);
			Exception ex2 = ex;
			lblStatus.Text = "CANNOT OPEN FOLDER";
			ProjectData.ClearProjectError();
		}
	}

	protected override void OnFormClosed(FormClosedEventArgs e)
	{
		if (picPreview.Image != null)
		{
			picPreview.Image.Dispose();
		}
		base.OnFormClosed(e);
	}
}
