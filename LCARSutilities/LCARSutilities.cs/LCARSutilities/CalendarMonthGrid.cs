using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace LCARSutilities;

public class CalendarMonthGrid : Control
{
	private DateTime viewMonth;

	private readonly HashSet<int> eventDays;

	public CalendarMonthGrid()
	{
		viewMonth = DateTime.Today;
		eventDays = new HashSet<int>();
		SetStyle(ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.FromArgb(20, 20, 20);
		ForeColor = Color.Orange;
		Font = new Font("Arial Narrow", 9f);
	}

	public void SetMonth(DateTime month, IList<CalendarEvent> events)
	{
		viewMonth = month;
		eventDays.Clear();
		if (events != null)
		{
			foreach (CalendarEvent @event in events)
			{
				if (@event != null)
				{
					CalendarRecurrence.AddOccurrenceDaysInMonth(@event, viewMonth, eventDays);
				}
			}
		}
		Invalidate();
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		Graphics graphics = e.Graphics;
		graphics.Clear(BackColor);
		int num = Math.Max(24, Width / 7);
		int num2 = Math.Max(22, Height / 7);
		string[] array = new string[7] { "S", "M", "T", "W", "T", "F", "S" };
		int num3 = 0;
		checked
		{
			do
			{
				graphics.DrawString(array[num3], Font, Brushes.Orange, num3 * num + 4, 2f);
				num3++;
			}
			while (num3 <= 6);
			int dayOfWeek = unchecked((int)new DateTime(viewMonth.Year, viewMonth.Month, 1).DayOfWeek);
			int num4 = DateTime.DaysInMonth(viewMonth.Year, viewMonth.Month);
			int num5 = num4;
			for (int i = 1; i <= num5; i++)
			{
				int num6 = dayOfWeek + i - 1;
				int num7;
				int num8;
				unchecked
				{
					num7 = num6 / 7;
					num8 = num6 % 7;
				}
				int num9 = num8 * num + 4;
				int num10 = 18 + num7 * num2;
				string text = i.ToString();
				if (eventDays.Contains(i))
				{
					text += "•";
				}
				graphics.DrawString(text, Font, Brushes.Orange, num9, num10);
			}
		}
	}
}
