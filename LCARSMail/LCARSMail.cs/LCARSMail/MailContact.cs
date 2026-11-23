namespace LCARSMail;

public class MailContact
{
	public string DisplayName;

	public string Email;

	public string Company;

	public string WorkPhone;

	public string HomePhone;

	public string Notes;

	public string CardDavEtag;

	public string SecondaryEmail;

	public string FirstName;

	public string LastName;

	public string NickName;

	public string JobTitle;

	public string Department;

	public string MobilePhone;

	public string WebPage;

	public string FaxNumber;

	public string PagerNumber;

	public string WorkAddress;

	public string OfficeLocation;

	public string HomeAddress;

	public string Custom1;

	public string Custom2;

	public string Custom3;

	public string Custom4;

	public string MiddleName;

	public string Birthday;

	public string OtherPhone;

	public string SpouseName;

	public string Anniversary;

	public string AssistantName;

	public string ScreenName;

	public string AimScreenName;

	public string PopularityIndex;

	public string RecordName;

	public string PreferEmail;

	public bool CardDavDirty;

	public bool IsGalEntry;

	public bool GalPushDirty;

	public string GalPersonaId;

	public string GalPersonaChangeKey;

	public string GalDirectoryItemId;

	public string GalDirectoryChangeKey;

	public string EwsPersonalItemId;

	internal string MorkRowId;

	public MailContact()
	{
		DisplayName = string.Empty;
		Email = string.Empty;
		Company = string.Empty;
		WorkPhone = string.Empty;
		HomePhone = string.Empty;
		Notes = string.Empty;
		CardDavEtag = string.Empty;
		SecondaryEmail = string.Empty;
		FirstName = string.Empty;
		LastName = string.Empty;
		NickName = string.Empty;
		JobTitle = string.Empty;
		Department = string.Empty;
		MobilePhone = string.Empty;
		WebPage = string.Empty;
		FaxNumber = string.Empty;
		PagerNumber = string.Empty;
		WorkAddress = string.Empty;
		OfficeLocation = string.Empty;
		HomeAddress = string.Empty;
		Custom1 = string.Empty;
		Custom2 = string.Empty;
		Custom3 = string.Empty;
		Custom4 = string.Empty;
		MiddleName = string.Empty;
		Birthday = string.Empty;
		OtherPhone = string.Empty;
		SpouseName = string.Empty;
		Anniversary = string.Empty;
		AssistantName = string.Empty;
		ScreenName = string.Empty;
		AimScreenName = string.Empty;
		PopularityIndex = string.Empty;
		RecordName = string.Empty;
		PreferEmail = string.Empty;
		GalPersonaId = string.Empty;
		GalPersonaChangeKey = string.Empty;
		GalDirectoryItemId = string.Empty;
		GalDirectoryChangeKey = string.Empty;
		EwsPersonalItemId = string.Empty;
		MorkRowId = string.Empty;
	}

	public string FormatAddress()
	{
		string text = Email;
		if (text == null)
		{
			text = string.Empty;
		}
		text = text.Trim();
		if (text.Length == 0)
		{
			return "";
		}
		string text2 = DisplayName;
		if (text2 == null)
		{
			text2 = string.Empty;
		}
		text2 = text2.Trim();
		if (text2.Length == 0)
		{
			return text;
		}
		if (text2.IndexOf('@') >= 0)
		{
			return text;
		}
		return "\"" + text2.Replace('"', '\'') + "\" <" + text + ">";
	}

	public string ListCaption()
	{
		string text = Email;
		if (text == null)
		{
			text = string.Empty;
		}
		text = text.Trim();
		string text2 = DisplayName;
		if (text2 == null)
		{
			text2 = string.Empty;
		}
		text2 = text2.Trim();
		if (text2.Length == 0)
		{
			return text;
		}
		if (text.Length == 0)
		{
			return text2;
		}
		return text2 + " <" + text + ">";
	}
}
