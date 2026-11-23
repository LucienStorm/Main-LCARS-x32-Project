using System;

namespace LCARSutilities;

public class CalendarEvent
{
	public string Uid;

	public string Summary;

	public DateTime StartUtc;

	public DateTime EndUtc;

	public string Location;

	public int AlarmMinutesBefore;

	public bool TaskCompleted;

	public int TaskPriority;

	public int TaskPercentComplete;

	public string EwsItemId;

	public string EwsChangeKey;

	public bool EwsDirty;

	public DateTime EwsLastModifiedUtc;

	public string Category;

	public string RecurrenceRule;

	public DateTime RecurrenceIdUtc;

	public string RecurrenceParentUid;

	public string ExDateUtcCsv;

	public string CalendarName;

	public bool CalDavDirty;

	public string CalDavEtag;

	public string Organizer;

	public string AttendeeMailto;

	public string MyPartStat;

	public bool IsMeetingInvitation;

	public CalendarEvent()
	{
		Uid = Guid.NewGuid().ToString();
		Summary = string.Empty;
		StartUtc = DateTime.UtcNow;
		EndUtc = DateTime.UtcNow.AddHours(1.0);
		Location = string.Empty;
		TaskPriority = 5;
		EwsItemId = string.Empty;
		EwsChangeKey = string.Empty;
		Category = string.Empty;
		RecurrenceRule = string.Empty;
		RecurrenceParentUid = string.Empty;
		ExDateUtcCsv = string.Empty;
		CalendarName = string.Empty;
		CalDavEtag = string.Empty;
		Organizer = string.Empty;
		AttendeeMailto = string.Empty;
		MyPartStat = string.Empty;
	}
}
