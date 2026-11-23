using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSutilities;

internal class GraphCanvas : PictureBox
{
	public delegate void TraceRequestedEventHandler(object sender, Point location);

	private struct GESTURECONFIG
	{
		public int dwID;

		public int dwWant;

		public int dwBlock;
	}

	private struct POINTS
	{
		public short x;

		public short y;
	}

	private struct GESTUREINFO
	{
		public int cbSize;

		public int dwFlags;

		public int dwID;

		public IntPtr hwndTarget;

		public POINTS ptsLocation;

		public int dwInstanceID;

		public int dwSequenceID;

		public ulong ullArguments;

		public int cbExtraArgs;
	}

	private const int WM_GESTURENOTIFY = 282;

	private const int WM_GESTURE = 281;

	private const int GF_BEGIN = 1;

	private const int GID_ZOOM = 3;

	private const int GID_PAN = 4;

	private const int GC_ALLGESTURES = 1;

	private bool dragActive;

	private bool dragMoved;

	private Point dragStart;

	private Point lastDrag;

	private const int DragThresholdPx = 6;

	private long gestureZoomArguments;

	private bool gestureConfigured;

	private double xmin;

	private double xmax;

	private double ymin;

	private double ymax;

	private const double MinSpan = 1E-06;

	private const double MaxSpan = 1000000000.0;

	[method: DebuggerNonUserCode]
	public event EventHandler WindowChanged;

	[method: DebuggerNonUserCode]
	public event TraceRequestedEventHandler TraceRequested;

	public GraphCanvas()
	{
		dragActive = false;
		dragMoved = false;
		gestureZoomArguments = 0L;
		gestureConfigured = false;
		xmin = -10.0;
		xmax = 10.0;
		ymin = -10.0;
		ymax = 10.0;
	}

	[DllImport("user32.dll")]
	private static extern bool SetGestureConfig(IntPtr hWnd, int dwReserved, int cIDs, ref GESTURECONFIG pGestureConfig, int cbSize);

	[DllImport("user32.dll")]
	private static extern bool GetGestureInfo(IntPtr hGestureInfo, ref GESTUREINFO pGestureInfo);

	[DllImport("user32.dll")]
	private static extern bool CloseGestureInfoHandle(IntPtr hGestureInfo);

	public void SetWindow(double xMin, double xMax, double yMin, double yMax)
	{
		xMin = xMin;
		xMax = xMax;
		yMin = yMin;
		yMax = yMax;
		NormalizeWindow();
	}

	public void GetWindow(ref double xMin, ref double xMax, ref double yMin, ref double yMax)
	{
		xMin = xMin;
		xMax = xMax;
		yMin = yMin;
		yMax = yMax;
	}

	protected override void OnHandleCreated(EventArgs e)
	{
		base.OnHandleCreated(e);
		ConfigureGestures();
	}

	private void ConfigureGestures()
	{
		if (!IsHandleCreated || gestureConfigured)
		{
			return;
		}
		try
		{
			GESTURECONFIG pGestureConfig = new GESTURECONFIG
			{
				dwID = 0,
				dwWant = 1,
				dwBlock = 0
			};
			if (SetGestureConfig(Handle, 0, 1, ref pGestureConfig, Marshal.SizeOf(typeof(GESTURECONFIG))))
			{
				gestureConfigured = true;
			}
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
	}

	protected override void WndProc(ref Message m)
	{
		if (m.Msg == 282)
		{
			ConfigureGestures();
		}
		else if (m.Msg == 281 && HandleGesture(ref m))
		{
			IntPtr result = new IntPtr(1);
			m.Result = result;
			return;
		}
		base.WndProc(ref m);
	}

	private bool HandleGesture(ref Message m)
	{
		GESTUREINFO pGestureInfo = new GESTUREINFO
		{
			cbSize = Marshal.SizeOf(typeof(GESTUREINFO))
		};
		if (!GetGestureInfo(m.LParam, ref pGestureInfo))
		{
			return false;
		}
		checked
		{
			try
			{
				Point p = new Point(pGestureInfo.ptsLocation.x, pGestureInfo.ptsLocation.y);
				p = PointToClient(p);
				switch (pGestureInfo.dwID)
				{
				case 3:
					if ((pGestureInfo.dwFlags & 1) != 0)
					{
						gestureZoomArguments = (long)pGestureInfo.ullArguments;
					}
					else if (gestureZoomArguments > 0)
					{
						double num = (double)pGestureInfo.ullArguments / (double)gestureZoomArguments;
						if (num > 0.01 && num < 100.0)
						{
							ZoomAt(p, 1.0 / num);
							gestureZoomArguments = (long)pGestureInfo.ullArguments;
						}
					}
					return true;
				case 4:
					return false;
				}
			}
			finally
			{
				CloseGestureInfoHandle(m.LParam);
			}
			return false;
		}
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);
		if (e.Button == MouseButtons.Left)
		{
			dragActive = true;
			dragMoved = false;
			dragStart = e.Location;
			lastDrag = e.Location;
			Capture = true;
		}
		Focus();
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		if (!dragActive || e.Button != MouseButtons.Left)
		{
			return;
		}
		checked
		{
			int num = e.X - lastDrag.X;
			int num2 = e.Y - lastDrag.Y;
			if (!dragMoved)
			{
				int num3 = Math.Abs(e.X - dragStart.X);
				int num4 = Math.Abs(e.Y - dragStart.Y);
				if (num3 < 6 && num4 < 6)
				{
					return;
				}
				dragMoved = true;
			}
			if (num != 0 || num2 != 0)
			{
				PanByPixels(num, num2);
				lastDrag = e.Location;
			}
		}
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		base.OnMouseUp(e);
		if (e.Button == MouseButtons.Left && dragActive)
		{
			if (!dragMoved)
			{
				TraceRequested?.Invoke(this, e.Location);
			}
			dragActive = false;
			Capture = false;
		}
	}

	protected override void OnMouseWheel(MouseEventArgs e)
	{
		base.OnMouseWheel(e);
		double factor = ((e.Delta > 0) ? 0.85 : 1.1764705882352942);
		ZoomAt(e.Location, factor);
	}

	public void ZoomAt(Point focalPx, double factor)
	{
		if (!(factor <= 0.0) && IsFinite(factor))
		{
			NormalizeWindow();
			double num = UnmapX(focalPx.X);
			double num2 = UnmapY(focalPx.Y);
			double num3 = (xmax - xmin) * factor;
			double num4 = (ymax - ymin) * factor;
			if (num3 < 1E-06)
			{
				num3 = 1E-06;
			}
			if (num4 < 1E-06)
			{
				num4 = 1E-06;
			}
			if (num3 > 1000000000.0)
			{
				num3 = 1000000000.0;
			}
			if (num4 > 1000000000.0)
			{
				num4 = 1000000000.0;
			}
			double num5 = (num - xmin) / (xmax - xmin);
			double num6 = (ymax - num2) / (ymax - ymin);
			if (!IsFinite(num5))
			{
				num5 = 0.5;
			}
			if (!IsFinite(num6))
			{
				num6 = 0.5;
			}
			xmin = num - num5 * num3;
			xmax = xmin + num3;
			ymax = num2 + num6 * num4;
			ymin = ymax - num4;
			NormalizeWindow();
			WindowChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	public void PanByPixels(int dx, int dy)
	{
		checked
		{
			int num = Math.Max(1, ClientSize.Width - 1);
			int num2 = Math.Max(1, ClientSize.Height - 1);
			double num3 = (xmax - xmin) / (double)num;
			double num4 = (ymax - ymin) / (double)num2;
			xmin -= (double)dx * num3;
			xmax -= (double)dx * num3;
			ymin += (double)dy * num4;
			ymax += (double)dy * num4;
			WindowChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	public double UnmapX(int px)
	{
		int num = Math.Max(1, checked(ClientSize.Width - 1));
		return xmin + (xmax - xmin) * (double)px / (double)num;
	}

	public double UnmapY(int py)
	{
		int num = Math.Max(1, checked(ClientSize.Height - 1));
		return ymax - (ymax - ymin) * (double)py / (double)num;
	}

	private void NormalizeWindow()
	{
		if (!IsFinite(xmin) || !IsFinite(xmax) || xmax <= xmin)
		{
			xmin = -10.0;
			xmax = 10.0;
		}
		if (!IsFinite(ymin) || !IsFinite(ymax) || ymax <= ymin)
		{
			ymin = -10.0;
			ymax = 10.0;
		}
	}

	private static bool IsFinite(double v)
	{
		return (!double.IsNaN(v) && !double.IsInfinity(v)) ? true : false;
	}
}
