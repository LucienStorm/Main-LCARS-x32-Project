using System;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

[StandardModule]
internal sealed class MailAttendeeHintBridge
{
	internal static string NormalizeOrganizerSmtp(string organizerLine)
	{
		string text = ((organizerLine != null) ? organizerLine : "").Trim();
		if (text.Length == 0)
		{
			return "";
		}
		int num = text.IndexOf("mailto:", StringComparison.OrdinalIgnoreCase);
		if (num >= 0)
		{
			text = text.Substring(checked(num + 7));
		}
		int num2 = text.IndexOf('@');
		if (num2 <= 0)
		{
			return "";
		}
		int num3 = text.IndexOf(';');
		if (num3 > 0)
		{
			text = text.Substring(0, num3);
		}
		return text.Trim();
	}
}
