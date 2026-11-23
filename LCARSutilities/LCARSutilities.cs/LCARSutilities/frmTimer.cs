using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Media;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using LCARS;
using LCARS.Controls;

namespace LCARSutilities;

public class frmTimer : LCARSForm
{
	private const int FormWidth = 420;

	private const int FormHeight = 380;

	private const int TickIntervalMs = 100;

	private const int MaxCustomMinutes = 999;

	private readonly Stopwatch clock;

	[AccessedThroughProperty("tickTimer")]
	private Timer _tickTimer;

	private Label lblDisplay;

	private Label lblMinutes;

	private NumericUpDown numMinutes;

	private readonly List<Control> countdownControls;

	private FlatButton fbCountdown;

	private FlatButton fbStopwatch;

	private bool isCountdownMode;

	private TimeSpan countdownTotal;

	protected Timer tickTimer
	{
		[DebuggerNonUserCode]
		get
		{
			return _tickTimer;
		}
		[MethodImpl(MethodImplOptions.Synchronized)]
		[DebuggerNonUserCode]
		set
		{
			EventHandler value2 = tickTimer_Tick;
			if (_tickTimer != null)
			{
				_tickTimer.Tick -= value2;
			}
			_tickTimer = value;
			if (_tickTimer != null)
			{
				_tickTimer.Tick += value2;
			}
		}
	}

	public frmTimer()
	{
		clock = new Stopwatch();
		tickTimer = new Timer();
		countdownControls = new List<Control>();
		isCountdownMode = true;
		countdownTotal = TimeSpan.FromMinutes(1.0);
		modUtilities.InitUtilityForm(this, 420, 380);
		BuildUi();
		tickTimer.Interval = 100;
		ShowDisplay();
	}

	private void BuildUi()
	{
		int num = 12;
		int num2 = checked(420 - num * 2);
		int num3 = 28;
		modUtilities.MakeUtilityTitle(this, "TIMER", num2);
		lblDisplay = modUtilities.MakeUtilityLabel(this, num, 48, num2, 70, 40f);
		lblDisplay.TextAlign = ContentAlignment.MiddleCenter;
		lblDisplay.BorderStyle = BorderStyle.FixedSingle;
		int num4 = checked(num2 - 6) / 2;
		fbCountdown = modUtilities.MakeUtilityButton(this, "COUNTDOWN", num, 126, num4, num3, LCARScolorStyles.PrimaryFunction, [SpecialName] (object s, EventArgs e) =>
		{
			SetMode(countdown: true);
		});
		fbStopwatch = modUtilities.MakeUtilityButton(this, "STOPWATCH", checked(num + num4 + 6), 126, num4, num3, LCARScolorStyles.NavigationFunction, [SpecialName] (object s, EventArgs e) =>
		{
			SetMode(countdown: false);
		});
		int num5 = checked(num2 - 18) / 4;
		AddCountdownPreset("1 MIN", 1, 0, num5);
		AddCountdownPreset("5 MIN", 5, 1, num5);
		AddCountdownPreset("10 MIN", 10, 2, num5);
		numMinutes = new NumericUpDown();
		numMinutes.Minimum = 1m;
		numMinutes.Maximum = 999m;
		numMinutes.Value = 1m;
		numMinutes.BackColor = Color.Black;
		numMinutes.ForeColor = Color.Orange;
		numMinutes.Font = new Font("Arial Narrow", 14f, FontStyle.Regular, GraphicsUnit.Point);
		numMinutes.TextAlign = HorizontalAlignment.Center;
		NumericUpDown numericUpDown = numMinutes;
		Point location = new Point(num, 206);
		numericUpDown.Location = location;
		NumericUpDown numericUpDown2 = numMinutes;
		Size size = new Size(num5, num3);
		numericUpDown2.Size = size;
		Controls.Add(numMinutes);
		countdownControls.Add(numMinutes);
		checked
		{
			lblMinutes = modUtilities.MakeUtilityLabel(this, num + num5 + 6, 208, num5, num3, 14f);
			lblMinutes.Text = "MINUTES";
			countdownControls.Add(lblMinutes);
			Control item = modUtilities.MakeUtilityButton(this, "SET", num + (num5 + 6) * 3, 206, num5, num3, LCARScolorStyles.Orange, [SpecialName] (object s, EventArgs e) =>
			{
				SetCountdown(Convert.ToInt32(numMinutes.Value));
			});
			countdownControls.Add(item);
		}
		int num6 = checked(num2 - 12) / 3;
		modUtilities.MakeUtilityButton(this, "START", num, 256, num6, 36, LCARScolorStyles.PrimaryFunction, [SpecialName] (object s, EventArgs e) =>
		{
			StartClock();
		});
		checked
		{
			modUtilities.MakeUtilityButton(this, "PAUSE", num + num6 + 6, 256, num6, 36, LCARScolorStyles.NavigationFunction, [SpecialName] (object s, EventArgs e) =>
			{
				PauseClock();
			});
			modUtilities.MakeUtilityButton(this, "RESET", num + (num6 + 6) * 2, 256, num6, 36, LCARScolorStyles.Orange, [SpecialName] (object s, EventArgs e) =>
			{
				ResetClock();
			});
			modUtilities.MakeUtilityButton(this, "CLOSE", num, 312, num2, 40, LCARScolorStyles.FunctionOffline, [SpecialName] (object s, EventArgs e) =>
			{
				Close();
			});
		}
	}

	private void AddCountdownPreset(string caption, int minutes, int slot, int width)
	{
		Control item = modUtilities.MakeUtilityButton(this, caption, checked(12 + slot * (width + 6)), 166, width, 28, LCARScolorStyles.Orange, [SpecialName] (object s, EventArgs e) =>
		{
			SetCountdown(minutes);
		});
		countdownControls.Add(item);
	}

	private void SetMode(bool countdown)
	{
		isCountdownMode = countdown;
		foreach (Control countdownControl in countdownControls)
		{
			countdownControl.Visible = countdown;
		}
		ResetClock();
	}

	private void SetCountdown(int minutes)
	{
		countdownTotal = TimeSpan.FromMinutes(Math.Max(1, Math.Min(999, minutes)));
		ResetClock();
	}

	private void StartClock()
	{
		if (isCountdownMode && clock.Elapsed >= countdownTotal)
		{
			ResetClock();
		}
		clock.Start();
		tickTimer.Start();
	}

	private void PauseClock()
	{
		clock.Stop();
		tickTimer.Stop();
		ShowDisplay();
	}

	private void ResetClock()
	{
		clock.Reset();
		tickTimer.Stop();
		ShowDisplay();
	}

	private void tickTimer_Tick(object sender, EventArgs e)
	{
		if (isCountdownMode && clock.Elapsed >= countdownTotal)
		{
			clock.Stop();
			tickTimer.Stop();
			ShowDisplay();
			SystemSounds.Beep.Play();
		}
		else
		{
			ShowDisplay();
		}
	}

	private void ShowDisplay()
	{
		if (isCountdownMode)
		{
			TimeSpan timeSpan = countdownTotal - clock.Elapsed;
			if (timeSpan < TimeSpan.Zero)
			{
				timeSpan = TimeSpan.Zero;
			}
			long num = checked((long)Math.Round(Math.Ceiling(timeSpan.TotalSeconds)));
			lblDisplay.Text = TimeSpan.FromSeconds(num).ToString("hh\\:mm\\:ss");
		}
		else
		{
			TimeSpan elapsed = clock.Elapsed;
			lblDisplay.Text = elapsed.ToString("hh\\:mm\\:ss") + "." + elapsed.Milliseconds / 100;
		}
	}

	protected override void OnFormClosed(FormClosedEventArgs e)
	{
		tickTimer.Stop();
		tickTimer.Dispose();
		base.OnFormClosed(e);
	}
}
