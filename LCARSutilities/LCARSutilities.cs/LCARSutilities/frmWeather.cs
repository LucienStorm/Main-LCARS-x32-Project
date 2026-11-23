using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Forms;
using LCARS;
using LCARS.Controls;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSutilities;

public class frmWeather : LCARSForm
{
	private const int FormWidth = 800;

	private const int FormHeight = 640;

	private const int HourStripHeight = 118;

	private const int DayStripHeight = 168;

	private const int HourCardWidth = 78;

	private const int DayCardWidth = 150;

	private const int CardGap = 6;

	private Label lblPlace;

	private Label lblCurrent;

	private Label lblCondition;

	private Label lblDetails;

	private Label lblStatus;

	private Label lblHourlyHeader;

	private Label lblDailyHeader;

	private LcarsHScrollPanel scrollHours;

	private LcarsHScrollPanel scrollDays;

	private bool loadBusy;

	private WeatherClient.ForecastSnapshot lastSnap;

	public frmWeather()
	{
		loadBusy = false;
		lastSnap = null;
		modUtilities.InitUtilityFormFloating(this, 800, 640);
		BuildUi();
		Shown += [SpecialName] (object s, EventArgs e) =>
		{
			BeginRefresh();
		};
		Resize += [SpecialName] (object s, EventArgs e) =>
		{
			LayoutUi();
		};
	}

	private void BuildUi()
	{
		int num = 12;
		checked
		{
			int num2 = 800 - num * 2;
			modUtilities.MakeUtilityTitle(this, "WEATHER", num2 - 360);
			modUtilities.MakeUtilityButton(this, "REFRESH", 800 - num - 350, num, 110, 28, LCARScolorStyles.PrimaryFunction, [SpecialName] (object s, EventArgs e) =>
			{
				BeginRefresh();
			});
			modUtilities.MakeUtilityButton(this, "MAP", 800 - num - 230, num, 100, 28, LCARScolorStyles.NavigationFunction, [SpecialName] (object s, EventArgs e) =>
			{
				OpenMap();
			});
			modUtilities.MakeUtilityButton(this, "CLOSE", 800 - num - 120, num, 110, 28, LCARScolorStyles.FunctionOffline, [SpecialName] (object s, EventArgs e) =>
			{
				Close();
			});
			lblPlace = modUtilities.MakeUtilityLabel(this, num, 48, num2, 24, 16f);
			lblPlace.Text = "LOCATING…";
			lblCurrent = modUtilities.MakeUtilityLabel(this, num, 74, 220, 48, 36f);
			lblCurrent.Text = "—";
			lblCondition = modUtilities.MakeUtilityLabel(this, num + 230, 78, num2 - 230, 40, 14f);
			lblCondition.Text = "";
			lblDetails = modUtilities.MakeUtilityLabel(this, num, 128, num2, 22, 12f);
			lblDetails.Text = "";
			lblStatus = modUtilities.MakeUtilityLabel(this, num, 152, num2, 18, 11f);
			lblStatus.Text = "";
			lblHourlyHeader = modUtilities.MakeUtilityLabel(this, num, 178, num2, 18, 11f);
			lblHourlyHeader.Text = "HOURLY — SWIPE";
			scrollHours = new LcarsHScrollPanel();
			Controls.Add(scrollHours);
			lblDailyHeader = modUtilities.MakeUtilityLabel(this, num, 326, num2, 18, 11f);
			lblDailyHeader.Text = "7-DAY — SWIPE";
			scrollDays = new LcarsHScrollPanel();
			Controls.Add(scrollDays);
			LayoutUi();
		}
	}

	private void LayoutUi()
	{
		int num = 12;
		checked
		{
			int num2 = Math.Max(320, ClientSize.Width - num * 2);
			lblPlace.Width = num2;
			lblCondition.Width = Math.Max(120, num2 - 230);
			lblDetails.Width = num2;
			lblStatus.Width = num2;
			lblHourlyHeader.Width = num2;
			lblDailyHeader.Width = num2;
			int num3 = 198;
			scrollHours.SetBounds(num, num3, num2, 118);
			int num4 = num3 + 118 + 28;
			lblDailyHeader.Top = num4 - 20;
			int num5 = Math.Max(168, ClientSize.Height - num4 - num);
			if (num5 > 208)
			{
				num5 = 208;
			}
			scrollDays.SetBounds(num, num4, num2, num5);
			RelayoutHourCards();
			RelayoutDayCards();
		}
	}

	private void BeginRefresh()
	{
		if (!loadBusy)
		{
			loadBusy = true;
			lblStatus.Text = "LOADING…";
			ThreadPool.QueueUserWorkItem(RefreshWorker);
		}
	}

	private void RefreshWorker(object state)
	{
		WeatherClient.ForecastSnapshot forecastSnapshot = null;
		try
		{
			forecastSnapshot = WeatherClient.FetchForecast();
		}
		catch (Exception ex)
		{
			ProjectData.SetProjectError(ex);
			Exception ex2 = ex;
			forecastSnapshot = new WeatherClient.ForecastSnapshot();
			forecastSnapshot.ErrorMessage = ex2.Message;
			ProjectData.ClearProjectError();
		}
		if (!IsDisposed)
		{
			WeatherClient.ForecastSnapshot snap = forecastSnapshot;
			BeginInvoke((MethodInvoker)([SpecialName] () =>
			{
				ApplySnapshot(snap);
			}));
		}
	}

	private void ApplySnapshot(WeatherClient.ForecastSnapshot snap)
	{
		loadBusy = false;
		lastSnap = snap;
		if (snap == null)
		{
			lblStatus.Text = "ERROR";
			return;
		}
		if (!snap.Ok)
		{
			lblPlace.Text = "WEATHER";
			lblCurrent.Text = "—";
			lblCondition.Text = "";
			lblDetails.Text = "";
			lblStatus.Text = snap.ErrorMessage.ToUpperInvariant();
			scrollHours.ContentPanel.Controls.Clear();
			scrollDays.ContentPanel.Controls.Clear();
			scrollHours.Relayout();
			scrollDays.Relayout();
			return;
		}
		string text = snap.PlaceLabel;
		if (Operators.CompareString(text, "", TextCompare: false) == 0)
		{
			text = string.Format(CultureInfo.InvariantCulture, "{0:0.####}, {1:0.####}", snap.Latitude, snap.Longitude);
		}
		lblPlace.Text = text.ToUpperInvariant();
		checked
		{
			if (snap.CurrentTemp.HasValue)
			{
				lblCurrent.Text = ((int)Math.Round(Math.Round(snap.CurrentTemp.Value))).ToString(CultureInfo.InvariantCulture) + "°" + snap.UnitSymbol;
			}
			else
			{
				lblCurrent.Text = "—";
			}
			string text2 = snap.CurrentCondition;
			if (snap.FeelsLike.HasValue)
			{
				text2 = text2 + "  ·  FEELS " + ((int)Math.Round(Math.Round(snap.FeelsLike.Value))).ToString(CultureInfo.InvariantCulture) + "°" + snap.UnitSymbol;
			}
			lblCondition.Text = text2;
			List<string> list = new List<string>();
			if (snap.Humidity.HasValue)
			{
				list.Add("HUM " + ((int)Math.Round(Math.Round(snap.Humidity.Value))).ToString(CultureInfo.InvariantCulture) + "%");
			}
			if (snap.WindSpeed.HasValue)
			{
				list.Add("WIND " + ((int)Math.Round(Math.Round(snap.WindSpeed.Value))).ToString(CultureInfo.InvariantCulture));
			}
			if (snap.PrecipCurrent.HasValue)
			{
				list.Add("PRECIP " + snap.PrecipCurrent.Value.ToString("0.#", CultureInfo.InvariantCulture));
			}
			lblDetails.Text = string.Join("  ·  ", list.ToArray());
			lblStatus.Text = "OPEN-METEO  ·  " + DateTime.Now.ToString("HH:mm", CultureInfo.InvariantCulture);
			BuildHourCards(snap);
			BuildDayCards(snap);
		}
	}

	private void OpenMap()
	{
		double num = 0.0;
		double num2 = 0.0;
		if (lastSnap != null && lastSnap.Ok)
		{
			num = lastSnap.Latitude;
			num2 = lastSnap.Longitude;
		}
		else
		{
			WeatherClient.LocationFix locationFix = WeatherClient.ResolveLocation();
			if (!locationFix.Ok)
			{
				lblStatus.Text = locationFix.ErrorMessage.ToUpperInvariant();
				return;
			}
			num = locationFix.Latitude;
			num2 = locationFix.Longitude;
		}
		modUtilities.OpenUrlInLcarsBrowser(WeatherClient.OpenStreetMapUrl(num, num2));
		lblStatus.Text = "MAP OPENED IN BROWSER.";
	}

	private void BuildHourCards(WeatherClient.ForecastSnapshot snap)
	{
		scrollHours.ContentPanel.Controls.Clear();
		string unitSymbol = snap.UnitSymbol;
		int num = Math.Max(70, 86);
		checked
		{
			foreach (WeatherClient.HourForecast hour in snap.Hours)
			{
				Panel panel = new Panel();
				panel.BackColor = Color.FromArgb(16, 16, 16);
				panel.BorderStyle = BorderStyle.FixedSingle;
				Size size = new Size(78, num);
				panel.Size = size;
				Label label = new Label();
				label.ForeColor = Color.Orange;
				label.BackColor = Color.Transparent;
				label.Font = new Font("Arial Narrow", 10f, FontStyle.Bold);
				label.Text = hour.TimeLabel;
				label.SetBounds(4, 4, 70, 16);
				panel.Controls.Add(label);
				Label label2 = new Label();
				label2.ForeColor = Color.FromArgb(255, 204, 102);
				label2.BackColor = Color.Transparent;
				label2.Font = new Font("Arial Narrow", 14f, FontStyle.Bold);
				label2.Text = (hour.Temp.HasValue ? (((int)Math.Round(Math.Round(hour.Temp.Value))).ToString(CultureInfo.InvariantCulture) + "°" + unitSymbol) : "—");
				label2.SetBounds(4, 24, 70, 22);
				panel.Controls.Add(label2);
				Label label3 = new Label();
				label3.ForeColor = Color.Orange;
				label3.BackColor = Color.Transparent;
				label3.Font = new Font("Arial Narrow", 8f, FontStyle.Regular);
				string text = hour.Condition;
				if (hour.PrecipProbability.HasValue)
				{
					text = text + " " + ((int)Math.Round(Math.Round(hour.PrecipProbability.Value))).ToString(CultureInfo.InvariantCulture) + "%";
				}
				label3.Text = text;
				label3.SetBounds(4, 50, 70, Math.Max(20, num - 54));
				panel.Controls.Add(label3);
				scrollHours.ContentPanel.Controls.Add(panel);
			}
			RelayoutHourCards();
			scrollHours.HookScrollGesture(scrollHours.ContentPanel);
		}
	}

	private void BuildDayCards(WeatherClient.ForecastSnapshot snap)
	{
		scrollDays.ContentPanel.Controls.Clear();
		string unitSymbol = snap.UnitSymbol;
		int num = Math.Max(110, 136);
		checked
		{
			foreach (WeatherClient.DayForecast day in snap.Days)
			{
				Panel panel = new Panel();
				panel.BackColor = Color.FromArgb(16, 16, 16);
				panel.BorderStyle = BorderStyle.FixedSingle;
				Size size = new Size(150, num);
				panel.Size = size;
				Label label = new Label();
				label.ForeColor = Color.Orange;
				label.BackColor = Color.Transparent;
				label.Font = new Font("Arial Narrow", 11f, FontStyle.Bold);
				label.Text = day.DateLabel;
				label.SetBounds(8, 6, 134, 20);
				panel.Controls.Add(label);
				Label label2 = new Label();
				label2.ForeColor = Color.FromArgb(255, 204, 102);
				label2.BackColor = Color.Transparent;
				label2.Font = new Font("Arial Narrow", 10f, FontStyle.Regular);
				label2.Text = day.Condition;
				label2.SetBounds(8, 28, 134, 28);
				panel.Controls.Add(label2);
				Label label3 = new Label();
				label3.ForeColor = Color.Orange;
				label3.BackColor = Color.Transparent;
				label3.Font = new Font("Arial Narrow", 14f, FontStyle.Bold);
				string text = (day.High.HasValue ? ((int)Math.Round(Math.Round(day.High.Value))).ToString(CultureInfo.InvariantCulture) : "—");
				string text2 = (day.Low.HasValue ? ((int)Math.Round(Math.Round(day.Low.Value))).ToString(CultureInfo.InvariantCulture) : "—");
				label3.Text = text + "° / " + text2 + "°" + unitSymbol;
				label3.SetBounds(8, 60, 134, 24);
				panel.Controls.Add(label3);
				Label label4 = new Label();
				label4.ForeColor = Color.FromArgb(200, 160, 80);
				label4.BackColor = Color.Transparent;
				label4.Font = new Font("Arial Narrow", 9f, FontStyle.Regular);
				List<string> list = new List<string>();
				if (day.PrecipSum.HasValue)
				{
					list.Add("P " + day.PrecipSum.Value.ToString("0.#", CultureInfo.InvariantCulture));
				}
				if (day.WindMax.HasValue)
				{
					list.Add("W " + ((int)Math.Round(Math.Round(day.WindMax.Value))).ToString(CultureInfo.InvariantCulture));
				}
				label4.Text = string.Join(" · ", list.ToArray());
				label4.SetBounds(8, 88, 134, 18);
				panel.Controls.Add(label4);
				scrollDays.ContentPanel.Controls.Add(panel);
			}
			RelayoutDayCards();
			scrollDays.HookScrollGesture(scrollDays.ContentPanel);
		}
	}

	private void RelayoutHourCards()
	{
		if (scrollHours == null)
		{
			return;
		}
		int num = 0;
		checked
		{
			int num2 = Math.Max(70, scrollHours.Height - 28 - 4);
			foreach (Control control in scrollHours.ContentPanel.Controls)
			{
				if (control is Panel)
				{
					control.SetBounds(num, 0, 78, num2);
					num += 84;
				}
			}
			scrollHours.Relayout();
		}
	}

	private void RelayoutDayCards()
	{
		if (scrollDays == null)
		{
			return;
		}
		int num = 0;
		checked
		{
			int num2 = Math.Max(110, scrollDays.Height - 28 - 4);
			foreach (Control control in scrollDays.ContentPanel.Controls)
			{
				if (control is Panel)
				{
					control.SetBounds(num, 0, 150, num2);
					num += 156;
				}
			}
			scrollDays.Relayout();
		}
	}
}
