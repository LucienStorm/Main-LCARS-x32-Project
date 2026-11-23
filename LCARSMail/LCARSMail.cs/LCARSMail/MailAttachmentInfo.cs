using System.Diagnostics;

namespace LCARSMail;

public class MailAttachmentInfo
{
	public int PartIndex;

	public string FileName;

	public long Size;

	public string CachedPath;

	public bool IsInline;

	public string ContentId;

	[DebuggerNonUserCode]
	public MailAttachmentInfo()
	{
	}

	public override string ToString()
	{
		string text = ((FileName != null) ? FileName : "attachment").Trim();
		if (text.Length == 0)
		{
			text = "attachment";
		}
		if (Size > 0)
		{
			return text + " (" + FormatAttachmentSize(Size) + ")";
		}
		return text;
	}

	private static string FormatAttachmentSize(long bytes)
	{
		if (bytes < 1024)
		{
			return bytes + " B";
		}
		if (bytes < 1048576)
		{
			return ((double)bytes / 1024.0).ToString("0.#") + " KB";
		}
		return ((double)bytes / 1048576.0).ToString("0.##") + " MB";
	}
}
