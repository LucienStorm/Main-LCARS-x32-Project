using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSutilities;

[StandardModule]
public sealed class CalendarRecurrence
{
	public static void AddOccurrenceDaysInMonth(CalendarEvent ev, DateTime month, HashSet<int> days)
	{
		if (ev == null || days == null)
		{
			return;
		}
		DateTime dateTime = new DateTime(month.Year, month.Month, 1, 0, 0, 0, DateTimeKind.Utc);
		DateTime t = dateTime.AddMonths(1);
		if (DateTime.Compare(ev.StartUtc, dateTime) >= 0 && DateTime.Compare(ev.StartUtc, t) < 0 && !IsOccurrenceExcluded(ev, ev.StartUtc))
		{
			days.Add(ev.StartUtc.Day);
		}
		string text = ((ev.RecurrenceRule != null) ? ev.RecurrenceRule : "").Trim();
		if (text.Length == 0)
		{
			return;
		}
		DateTime t2 = ParseUntilUtc(text);
		int num = ParseCount(text, 0);
		checked
		{
			if (text.IndexOf("FREQ=DAILY", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				int num2 = ParseInterval(text, 1);
				DateTime dateTime2 = ev.StartUtc;
				while (DateTime.Compare(dateTime2, dateTime) < 0)
				{
					dateTime2 = dateTime2.AddDays(num2);
				}
				long num3 = (long)Math.Round((dateTime2.Date - ev.StartUtc.Date).TotalDays);
				int num4 = (int)Math.Max(0L, unchecked(num3 / Math.Max(1, num2)));
				while (DateTime.Compare(dateTime2, t) < 0 && DateTime.Compare(dateTime2, t2) <= 0 && (num <= 0 || num4 + 1 <= num))
				{
					if (DateTime.Compare(dateTime2, dateTime) >= 0 && !IsOccurrenceExcluded(ev, dateTime2))
					{
						days.Add(dateTime2.Day);
					}
					num4++;
					dateTime2 = dateTime2.AddDays(num2);
				}
			}
			else if (text.IndexOf("FREQ=WEEKLY", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				HashSet<DayOfWeek> hashSet = ParseByDaySet(text);
				if (hashSet.Count == 0)
				{
					hashSet.Add(ev.StartUtc.DayOfWeek);
				}
				int num5 = ParseInterval(text, 1);
				DayOfWeek wkst = ParseWkst(text, ev.StartUtc.DayOfWeek);
				DateTime t3 = dateTime;
				while (DateTime.Compare(t3, t) < 0)
				{
					if (hashSet.Contains(t3.DayOfWeek))
					{
						DateTime t4 = t3.Date.Add(ev.StartUtc.TimeOfDay);
						if (DateTime.Compare(t4, t2) <= 0 && DateTime.Compare(t4, ev.StartUtc) >= 0 && DateTime.Compare(t4, dateTime) >= 0 && DateTime.Compare(t4, t) < 0)
						{
							int num6 = WeeksSinceStart(ev.StartUtc.Date, t4.Date, wkst);
							if (num6 >= 0 && unchecked(num6 % num5) == 0)
							{
								days.Add(t4.Day);
							}
						}
					}
					t3 = t3.AddDays(1.0);
				}
			}
			else if (text.IndexOf("FREQ=MONTHLY", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				int num7 = ParseInterval(text, 1);
				int num8 = ParseByMonthDay(text);
				if (num8 != 0)
				{
					int day = ResolveMonthDay(month.Year, month.Month, num8);
					DateTime t5 = new DateTime(month.Year, month.Month, day, ev.StartUtc.Hour, ev.StartUtc.Minute, ev.StartUtc.Second, DateTimeKind.Utc);
					if (DateTime.Compare(t5, ev.StartUtc) >= 0 && DateTime.Compare(t5, dateTime) >= 0 && DateTime.Compare(t5, t) < 0 && DateTime.Compare(t5, t2) <= 0 && (num <= 0 || (t5.Year - ev.StartUtc.Year) * 12 + (t5.Month - ev.StartUtc.Month) + 1 <= num))
					{
						days.Add(t5.Day);
					}
					return;
				}
				List<KeyValuePair<int, DayOfWeek>> list = ParseByDaySetPosList(text);
				if (list.Count > 0)
				{
					int num9 = (month.Year - ev.StartUtc.Year) * 12 + (month.Month - ev.StartUtc.Month);
					if (num9 < 0 || unchecked(num9 % num7) != 0)
					{
						return;
					}
					{
						foreach (KeyValuePair<int, DayOfWeek> item in list)
						{
							DateTime t6 = NthWeekdayInMonth(month.Year, month.Month, item.Value, item.Key);
							if (DateTime.Compare(t6, DateTime.MinValue) != 0)
							{
								DateTime t7 = t6.Date.Add(ev.StartUtc.TimeOfDay);
								if (DateTime.Compare(t7, t2) <= 0 && DateTime.Compare(t7, ev.StartUtc) >= 0 && DateTime.Compare(t7, dateTime) >= 0 && DateTime.Compare(t7, t) < 0 && (num <= 0 || num9 + 1 <= num))
								{
									days.Add(t7.Day);
								}
							}
						}
						return;
					}
				}
				HashSet<DayOfWeek> hashSet2 = ParseByDaySet(text);
				if (hashSet2.Count > 0)
				{
					DateTime t8 = dateTime;
					while (DateTime.Compare(t8, t) < 0)
					{
						if (hashSet2.Contains(t8.DayOfWeek))
						{
							DateTime t9 = t8.Date.Add(ev.StartUtc.TimeOfDay);
							if (DateTime.Compare(t9, t2) <= 0 && DateTime.Compare(t9, ev.StartUtc) >= 0 && DateTime.Compare(t9, dateTime) >= 0 && DateTime.Compare(t9, t) < 0)
							{
								int num10 = (t9.Year - ev.StartUtc.Year) * 12 + (t9.Month - ev.StartUtc.Month);
								if (num10 >= 0 && unchecked(num10 % num7) == 0)
								{
									days.Add(t9.Day);
								}
							}
						}
						t8 = t8.AddDays(1.0);
					}
					return;
				}
				DateTime t10 = ev.StartUtc;
				while (DateTime.Compare(t10, dateTime) < 0)
				{
					t10 = t10.AddMonths(num7);
				}
				while (DateTime.Compare(t10, t) < 0 && DateTime.Compare(t10, t2) <= 0)
				{
					if (num > 0)
					{
						int num11 = (t10.Year - ev.StartUtc.Year) * 12 + (t10.Month - ev.StartUtc.Month) + 1;
						if (num11 > num)
						{
							break;
						}
					}
					if (DateTime.Compare(t10, dateTime) >= 0)
					{
						days.Add(t10.Day);
					}
					t10 = t10.AddMonths(num7);
				}
			}
			else
			{
				if (text.IndexOf("FREQ=YEARLY", StringComparison.OrdinalIgnoreCase) < 0)
				{
					return;
				}
				int num12 = ParseInterval(text, 1);
				int num13 = ParseByMonth(text);
				int num14 = ParseByMonthDay(text);
				if (num13 > 0 && num14 != 0)
				{
					if (month.Month != num13)
					{
						return;
					}
					int day2 = ResolveMonthDay(month.Year, num13, num14);
					DateTime t11 = new DateTime(month.Year, num13, day2, ev.StartUtc.Hour, ev.StartUtc.Minute, ev.StartUtc.Second, DateTimeKind.Utc);
					if (DateTime.Compare(t11, ev.StartUtc) >= 0 && DateTime.Compare(t11, dateTime) >= 0 && DateTime.Compare(t11, t) < 0 && DateTime.Compare(t11, t2) <= 0)
					{
						int num15 = t11.Year - ev.StartUtc.Year;
						if (num15 >= 0 && unchecked(num15 % num12) == 0 && (num <= 0 || unchecked(num15 / num12) + 1 <= num))
						{
							days.Add(t11.Day);
						}
					}
					return;
				}
				List<KeyValuePair<int, DayOfWeek>> list2 = ParseByDaySetPosList(text);
				if (list2.Count > 0)
				{
					int num16 = ((num13 > 0) ? num13 : ev.StartUtc.Month);
					if (month.Month != num16)
					{
						return;
					}
					int num17 = month.Year - ev.StartUtc.Year;
					if (num17 < 0 || unchecked(num17 % num12) != 0)
					{
						return;
					}
					{
						foreach (KeyValuePair<int, DayOfWeek> item2 in list2)
						{
							DateTime t12 = NthWeekdayInMonth(month.Year, month.Month, item2.Value, item2.Key);
							if (DateTime.Compare(t12, DateTime.MinValue) != 0)
							{
								DateTime t13 = t12.Date.Add(ev.StartUtc.TimeOfDay);
								if (DateTime.Compare(t13, t2) <= 0 && DateTime.Compare(t13, ev.StartUtc) >= 0 && DateTime.Compare(t13, dateTime) >= 0 && DateTime.Compare(t13, t) < 0 && (num <= 0 || unchecked(num17 / num12) + 1 <= num))
								{
									days.Add(t13.Day);
								}
							}
						}
						return;
					}
				}
				if (num13 > 0 && month.Month != num13)
				{
					return;
				}
				DateTime t14 = ev.StartUtc;
				while (DateTime.Compare(t14, dateTime) < 0)
				{
					t14 = t14.AddYears(num12);
				}
				while (DateTime.Compare(t14, t) < 0 && DateTime.Compare(t14, t2) <= 0)
				{
					if (num13 > 0 && t14.Month != num13)
					{
						t14 = t14.AddYears(num12);
						continue;
					}
					if (num > 0)
					{
						int num18 = t14.Year - ev.StartUtc.Year + 1;
						if (num18 > num)
						{
							break;
						}
					}
					if (DateTime.Compare(t14, dateTime) >= 0)
					{
						days.Add(t14.Day);
					}
					t14 = t14.AddYears(num12);
				}
			}
		}
	}

	private static DateTime ParseUntilUtc(string rule)
	{
		string text = ((rule != null) ? rule : "");
		int num = text.IndexOf("UNTIL=", StringComparison.OrdinalIgnoreCase);
		if (num < 0)
		{
			return DateTime.MaxValue;
		}
		string text2 = text.Substring(checked(num + 6));
		int num2 = text2.IndexOf(';');
		if (num2 >= 0)
		{
			text2 = text2.Substring(0, num2);
		}
		text2 = text2.Trim();
		if (text2.Length >= 8)
		{
			string s = text2.Substring(0, 8);
			DateTime result = DateTime.MinValue;
			if (DateTime.TryParseExact(s, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out result))
			{
				return result.ToUniversalTime();
			}
		}
		return DateTime.MaxValue;
	}

	private static DayOfWeek ParseWkst(string rule, DayOfWeek defaultDay)
	{
		string text = ((rule != null) ? rule : "");
		int num = text.IndexOf("WKST=", StringComparison.OrdinalIgnoreCase);
		if (num < 0)
		{
			return defaultDay;
		}
		checked
		{
			string text2 = text.Substring(num + 5);
			int num2 = text2.IndexOf(';');
			if (num2 >= 0)
			{
				text2 = text2.Substring(0, num2);
			}
			string text3 = text2.Trim().ToUpperInvariant();
			if (text3.Length > 2)
			{
				text3 = text3.Substring(text3.Length - 2);
			}
			DayOfWeek? dayOfWeek = DayCodeToDayOfWeek(text3);
			if (dayOfWeek.HasValue)
			{
				return dayOfWeek.Value;
			}
			return defaultDay;
		}
	}

	private static int WeeksSinceStart(DateTime startDate, DateTime candidateDate, DayOfWeek wkst)
	{
		DateTime dateTime = StartOfWeek(startDate, wkst);
		DateTime dateTime2 = StartOfWeek(candidateDate, wkst);
		if (DateTime.Compare(dateTime2, dateTime) < 0)
		{
			return -1;
		}
		return checked((int)Math.Round((dateTime2 - dateTime).TotalDays / 7.0));
	}

	private static DateTime StartOfWeek(DateTime dt, DayOfWeek wkst)
	{
		DateTime result = dt.Date;
		while (result.DayOfWeek != wkst)
		{
			result = result.AddDays(-1.0);
		}
		return result;
	}

	private static int ParseByMonth(string rule)
	{
		string text = ((rule != null) ? rule : "");
		int num = text.IndexOf("BYMONTH=", StringComparison.OrdinalIgnoreCase);
		if (num < 0)
		{
			return 0;
		}
		string text2 = text.Substring(checked(num + 8));
		int num2 = text2.IndexOf(';');
		if (num2 >= 0)
		{
			text2 = text2.Substring(0, num2);
		}
		int result = 0;
		if (int.TryParse(text2.Trim(), out result) && result >= 1 && result <= 12)
		{
			return result;
		}
		return 0;
	}

	private static int ParseByMonthDay(string rule)
	{
		string text = ((rule != null) ? rule : "");
		int num = text.IndexOf("BYMONTHDAY=", StringComparison.OrdinalIgnoreCase);
		if (num < 0)
		{
			return 0;
		}
		string text2 = text.Substring(checked(num + 11));
		int num2 = text2.IndexOf(';');
		if (num2 >= 0)
		{
			text2 = text2.Substring(0, num2);
		}
		int result = 0;
		if (int.TryParse(text2.Trim(), out result))
		{
			switch (result)
			{
			case -1:
				return -1;
			case 1:
			case 2:
			case 3:
			case 4:
			case 5:
			case 6:
			case 7:
			case 8:
			case 9:
			case 10:
			case 11:
			case 12:
			case 13:
			case 14:
			case 15:
			case 16:
			case 17:
			case 18:
			case 19:
			case 20:
			case 21:
			case 22:
			case 23:
			case 24:
			case 25:
			case 26:
			case 27:
			case 28:
			case 29:
			case 30:
			case 31:
				return result;
			}
		}
		return 0;
	}

	private static int ResolveMonthDay(int year, int month, int byMonthDay)
	{
		int num = DateTime.DaysInMonth(year, month);
		if (byMonthDay == -1)
		{
			return num;
		}
		return Math.Min(byMonthDay, num);
	}

	private static int ParseCount(string rule, int defaultValue)
	{
		string text = ((rule != null) ? rule : "");
		int num = text.IndexOf("COUNT=", StringComparison.OrdinalIgnoreCase);
		if (num < 0)
		{
			return defaultValue;
		}
		string text2 = text.Substring(checked(num + 6));
		int num2 = text2.IndexOf(';');
		if (num2 >= 0)
		{
			text2 = text2.Substring(0, num2);
		}
		int result = 0;
		if (int.TryParse(text2.Trim(), out result) && result > 0)
		{
			return result;
		}
		return defaultValue;
	}

	private static int ParseInterval(string rule, int defaultValue)
	{
		string text = ((rule != null) ? rule : "");
		int num = text.IndexOf("INTERVAL=", StringComparison.OrdinalIgnoreCase);
		if (num < 0)
		{
			return defaultValue;
		}
		string text2 = text.Substring(checked(num + 9));
		int num2 = text2.IndexOf(';');
		if (num2 >= 0)
		{
			text2 = text2.Substring(0, num2);
		}
		int result = 0;
		if (int.TryParse(text2.Trim(), out result) && result > 0)
		{
			return result;
		}
		return defaultValue;
	}

	private static DateTime NthWeekdayInMonth(int year, int month, DayOfWeek dow, int n)
	{
		if (n == 0)
		{
			return DateTime.MinValue;
		}
		int num = DateTime.DaysInMonth(year, month);
		checked
		{
			if (n < 0)
			{
				DateTime result = DateTime.MinValue;
				int num2 = num;
				for (int i = 1; i <= num2; i++)
				{
					DateTime dateTime = new DateTime(year, month, i, 0, 0, 0, DateTimeKind.Utc);
					if (dateTime.DayOfWeek == dow)
					{
						result = dateTime;
					}
				}
				return result;
			}
			int num3 = 0;
			int num4 = num;
			for (int j = 1; j <= num4; j++)
			{
				DateTime result2 = new DateTime(year, month, j, 0, 0, 0, DateTimeKind.Utc);
				if (result2.DayOfWeek == dow)
				{
					num3++;
					if (num3 == n)
					{
						return result2;
					}
				}
			}
			return DateTime.MinValue;
		}
	}

	private static List<KeyValuePair<int, DayOfWeek>> ParseByDaySetPosList(string rule)
	{
		List<KeyValuePair<int, DayOfWeek>> list = new List<KeyValuePair<int, DayOfWeek>>();
		string text = ((rule != null) ? rule : "");
		int num = text.IndexOf("BYDAY=", StringComparison.OrdinalIgnoreCase);
		if (num < 0)
		{
			return list;
		}
		checked
		{
			string text2 = text.Substring(num + 6);
			int num2 = text2.IndexOf(';');
			if (num2 >= 0)
			{
				text2 = text2.Substring(0, num2);
			}
			string[] array = text2.Split(',');
			foreach (string text3 in array)
			{
				string text4 = text3.Trim().ToUpperInvariant();
				if (text4.Length < 3)
				{
					continue;
				}
				string text5 = "";
				int j = 0;
				if (j < text4.Length && text4[j] == '-')
				{
					text5 = "-";
					j++;
				}
				for (; j < text4.Length && char.IsDigit(text4[j]); j++)
				{
					text5 += Conversions.ToString(text4[j]);
				}
				if (text5.Length == 0)
				{
					continue;
				}
				int result = 0;
				if (int.TryParse(text5, out result) && result != 0)
				{
					string text6 = text4.Substring(j);
					if (text6.Length > 2)
					{
						text6 = text6.Substring(text6.Length - 2);
					}
					DayOfWeek? dayOfWeek = DayCodeToDayOfWeek(text6);
					if (dayOfWeek.HasValue)
					{
						KeyValuePair<int, DayOfWeek> item = new KeyValuePair<int, DayOfWeek>(result, dayOfWeek.Value);
						list.Add(item);
					}
				}
			}
			return list;
		}
	}

	private static DayOfWeek? DayCodeToDayOfWeek(string code)
	{
		DayOfWeek? dayOfWeek = default(DayOfWeek?);
		return code switch
		{
			"SU" => DayOfWeek.Sunday, 
			"MO" => DayOfWeek.Monday, 
			"TU" => DayOfWeek.Tuesday, 
			"WE" => DayOfWeek.Wednesday, 
			"TH" => DayOfWeek.Thursday, 
			"FR" => DayOfWeek.Friday, 
			"SA" => DayOfWeek.Saturday, 
			_ => dayOfWeek, 
		};
	}

	private static HashSet<DayOfWeek> ParseByDaySet(string rule)
	{
		HashSet<DayOfWeek> hashSet = new HashSet<DayOfWeek>();
		string text = ((rule != null) ? rule : "");
		int num = text.IndexOf("BYDAY=", StringComparison.OrdinalIgnoreCase);
		if (num < 0)
		{
			return hashSet;
		}
		checked
		{
			string text2 = text.Substring(num + 6);
			int num2 = text2.IndexOf(';');
			if (num2 >= 0)
			{
				text2 = text2.Substring(0, num2);
			}
			string[] array = text2.Split(',');
			foreach (string text3 in array)
			{
				string text4 = text3.Trim().ToUpperInvariant();
				if (text4.Length >= 2)
				{
					string text5 = text4;
					if (text5.Length > 2)
					{
						text5 = text5.Substring(text5.Length - 2);
					}
					switch (text5)
					{
					case "SU":
						hashSet.Add(DayOfWeek.Sunday);
						break;
					case "MO":
						hashSet.Add(DayOfWeek.Monday);
						break;
					case "TU":
						hashSet.Add(DayOfWeek.Tuesday);
						break;
					case "WE":
						hashSet.Add(DayOfWeek.Wednesday);
						break;
					case "TH":
						hashSet.Add(DayOfWeek.Thursday);
						break;
					case "FR":
						hashSet.Add(DayOfWeek.Friday);
						break;
					case "SA":
						hashSet.Add(DayOfWeek.Saturday);
						break;
					}
				}
			}
			return hashSet;
		}
	}

	public static bool IsOccurrenceExcluded(CalendarEvent ev, DateTime occurrenceUtc)
	{
		if (ev == null || DateTime.Compare(occurrenceUtc, DateTime.MinValue) == 0)
		{
			return false;
		}
		string text = ((ev.ExDateUtcCsv != null) ? ev.ExDateUtcCsv : "").Trim();
		if (text.Length == 0)
		{
			return false;
		}
		string b = occurrenceUtc.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
		string[] array = text.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
		foreach (string text2 in array)
		{
			if (string.Equals(text2.Trim(), b, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	public static bool TryClampRecurrenceUntilBeforeInstance(CalendarEvent series, DateTime instanceStartUtc)
	{
		if (series == null || DateTime.Compare(instanceStartUtc, DateTime.MinValue) == 0)
		{
			return false;
		}
		string text = ((series.RecurrenceRule != null) ? series.RecurrenceRule : "").Trim();
		if (text.Length == 0)
		{
			return false;
		}
		int num = text.IndexOf("UNTIL=", StringComparison.OrdinalIgnoreCase);
		if (num < 0)
		{
			return false;
		}
		checked
		{
			int num2 = num + 6;
			int num3 = text.IndexOf(';', num2);
			if (num3 < 0)
			{
				num3 = text.Length;
			}
			string text2 = text.Substring(num2, num3 - num2).Trim();
			DateTime dateTime = ParseUntilUtc(series.RecurrenceRule);
			if (DateTime.Compare(dateTime, DateTime.MaxValue) == 0)
			{
				return false;
			}
			if (DateTime.Compare(instanceStartUtc.ToUniversalTime(), dateTime) >= 0)
			{
				return false;
			}
			string text3 = instanceStartUtc.ToUniversalTime().AddDays(-1.0).ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
			series.RecurrenceRule = text.Substring(0, num2) + text3 + text.Substring(num3);
			series.CalDavDirty = true;
			series.EwsDirty = true;
			return true;
		}
	}

	public static bool TryDecrementRecurrenceCountOnSeries(CalendarEvent series)
	{
		if (series == null)
		{
			return false;
		}
		string text = ((series.RecurrenceRule != null) ? series.RecurrenceRule : "").Trim();
		if (text.Length == 0)
		{
			return false;
		}
		int num = text.IndexOf("COUNT=", StringComparison.OrdinalIgnoreCase);
		if (num < 0)
		{
			return false;
		}
		checked
		{
			int num2 = num + 6;
			int num3 = text.IndexOf(';', num2);
			if (num3 < 0)
			{
				num3 = text.Length;
			}
			string s = text.Substring(num2, num3 - num2).Trim();
			int result = 0;
			if (!int.TryParse(s, out result) || result <= 1)
			{
				return false;
			}
			string recurrenceRule = text.Substring(0, num2) + (result - 1) + text.Substring(num3);
			series.RecurrenceRule = recurrenceRule;
			series.CalDavDirty = true;
			series.EwsDirty = true;
			return true;
		}
	}

	public static bool TryAppendExDateOnSeriesMaster(List<CalendarEvent> all, CalendarEvent series, DateTime instanceStartUtc)
	{
		if (all == null || series == null || string.IsNullOrWhiteSpace(series.Uid))
		{
			return false;
		}
		string text = instanceStartUtc.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
		foreach (CalendarEvent item in all)
		{
			if (item == null || !string.Equals(item.Uid, series.Uid, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			CalendarEvent calendarEvent = item;
			string text2 = ((calendarEvent.ExDateUtcCsv != null) ? calendarEvent.ExDateUtcCsv : "").Trim();
			if (text2.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
			item.ExDateUtcCsv = ((text2.Length == 0) ? text : (text2 + "," + text));
			item.CalDavDirty = true;
			item.EwsDirty = true;
			return true;
		}
		return false;
	}

	public static CalendarEvent TryCreateRecurrenceException(CalendarEvent series, DateTime instanceStartUtc)
	{
		if (series == null)
		{
			return null;
		}
		if (string.IsNullOrWhiteSpace(series.RecurrenceRule))
		{
			return null;
		}
		CalendarEvent calendarEvent = new CalendarEvent();
		calendarEvent.Uid = Guid.NewGuid().ToString();
		calendarEvent.Summary = series.Summary;
		calendarEvent.Location = series.Location;
		calendarEvent.StartUtc = instanceStartUtc;
		calendarEvent.EndUtc = instanceStartUtc.Add(series.EndUtc - series.StartUtc);
		calendarEvent.AlarmMinutesBefore = series.AlarmMinutesBefore;
		calendarEvent.Category = series.Category;
		calendarEvent.CalendarName = series.CalendarName;
		calendarEvent.RecurrenceParentUid = series.Uid;
		calendarEvent.RecurrenceIdUtc = instanceStartUtc;
		calendarEvent.CalDavDirty = true;
		calendarEvent.EwsDirty = true;
		return calendarEvent;
	}
}
