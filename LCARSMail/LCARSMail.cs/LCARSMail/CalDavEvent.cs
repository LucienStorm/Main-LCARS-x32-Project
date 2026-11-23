using System;
using System.Diagnostics;

namespace LCARSMail;

public class CalDavEvent
{
	public string Summary;

	public string Uid;

	public DateTime StartUtc;

	public DateTime EndUtc;

	public int AlarmMinutesBefore;

	public bool TaskCompleted;

	public string Location;

	public string RecurrenceRule;

	public string Category;

	public string CalendarName;

	public string Organizer;

	public string AttendeeMailto;

	public string MyPartStat;

	public bool IsMeetingInvitation;

	public DateTime RecurrenceIdUtc;

	public string RecurrenceParentUid;

	public string ExDateUtcCsv;

	[DebuggerNonUserCode]
	public CalDavEvent()
	{
	}
}
