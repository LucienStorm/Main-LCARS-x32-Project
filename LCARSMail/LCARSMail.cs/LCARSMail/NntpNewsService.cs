using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

public sealed class NntpNewsService
{
	private sealed class NntpSession : IDisposable
	{
		private readonly TcpClient client;

		private readonly StreamReader reader;

		private readonly StreamWriter writer;

		public NntpSession(string host, int port)
		{
			client = new TcpClient(host, port);
			NetworkStream stream = client.GetStream();
			reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false, 1024, leaveOpen: true);
			writer = new StreamWriter(stream, Encoding.ASCII, 1024, leaveOpen: true)
			{
				AutoFlush = true
			};
		}

		public void WriteLine(string line)
		{
			writer.WriteLine(line);
		}

		public string ReadLine()
		{
			return reader.ReadLine();
		}

		public void Dispose()
		{
			try
			{
				writer.Dispose();
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
			try
			{
				reader.Dispose();
			}
			catch (Exception projectError2)
			{
				ProjectData.SetProjectError(projectError2);
				ProjectData.ClearProjectError();
			}
			try
			{
				client.Close();
			}
			catch (Exception projectError3)
			{
				ProjectData.SetProjectError(projectError3);
				ProjectData.ClearProjectError();
			}
		}

		void IDisposable.Dispose()
		{
			//ILSpy generated this explicit interface implementation from .override directive in Dispose
			this.Dispose();
		}
	}

	public static string LastError;

	private NntpNewsService()
	{
	}

	public static List<string> TryListGroups(string host, int port)
	{
		LastError = null;
		List<string> list = new List<string>();
		using (NntpSession nntpSession = Connect(host, port))
		{
			if (nntpSession == null)
			{
				return list;
			}
			nntpSession.WriteLine("LIST");
			string text = nntpSession.ReadLine();
			if (text == null || !text.StartsWith("215", StringComparison.Ordinal))
			{
				LastError = ((text != null) ? text : "LIST failed.");
				return list;
			}
			string text2 = nntpSession.ReadLine();
			while (text2 != null && Operators.CompareString(text2, ".", TextCompare: false) != 0)
			{
				string[] array = text2.Split(' ');
				if (array.Length > 0)
				{
					list.Add(array[0]);
				}
				text2 = nntpSession.ReadLine();
			}
		}
		return list;
	}

	public static List<NntpArticleHeader> TryListArticles(string host, int port, string group)
	{
		LastError = null;
		List<NntpArticleHeader> list = new List<NntpArticleHeader>();
		using (NntpSession nntpSession = Connect(host, port))
		{
			if (nntpSession == null)
			{
				return list;
			}
			nntpSession.WriteLine("GROUP " + group);
			string text = nntpSession.ReadLine();
			if (text == null || !text.StartsWith("211", StringComparison.Ordinal))
			{
				LastError = ((text != null) ? text : "GROUP failed.");
				return list;
			}
			nntpSession.WriteLine("XOVER 1-");
			string text2 = nntpSession.ReadLine();
			if (text2 == null || !text2.StartsWith("224", StringComparison.Ordinal))
			{
				LastError = ((text2 != null) ? text2 : "XOVER failed.");
				return list;
			}
			string text3 = nntpSession.ReadLine();
			while (text3 != null && Operators.CompareString(text3, ".", TextCompare: false) != 0)
			{
				string[] array = text3.Split(':');
				if (array.Length >= 4)
				{
					int result = 0;
					int.TryParse(array[0], out result);
					list.Add(new NntpArticleHeader
					{
						ArticleNumber = result,
						Subject = array[3],
						FromLine = ((array.Length > 5) ? array[5] : "")
					});
				}
				text3 = nntpSession.ReadLine();
			}
		}
		return list;
	}

	public static string TryFetchArticle(string host, int port, string group, int articleNumber)
	{
		LastError = null;
		using NntpSession nntpSession = Connect(host, port);
		if (nntpSession == null)
		{
			return "";
		}
		nntpSession.WriteLine("GROUP " + group);
		string text = nntpSession.ReadLine();
		if (text == null || !text.StartsWith("211", StringComparison.Ordinal))
		{
			LastError = ((text != null) ? text : "GROUP failed.");
			return "";
		}
		nntpSession.WriteLine("ARTICLE " + articleNumber);
		string text2 = nntpSession.ReadLine();
		if (text2 == null || !text2.StartsWith("220", StringComparison.Ordinal))
		{
			LastError = ((text2 != null) ? text2 : "ARTICLE failed.");
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder();
		string text3 = nntpSession.ReadLine();
		while (text3 != null && Operators.CompareString(text3, ".", TextCompare: false) != 0)
		{
			stringBuilder.AppendLine(text3);
			text3 = nntpSession.ReadLine();
		}
		return stringBuilder.ToString();
	}

	public static bool TryPostArticle(string host, int port, string fromLine, string subject, string newsgroups, string body)
	{
		LastError = null;
		using NntpSession nntpSession = Connect(host, port);
		if (nntpSession == null)
		{
			return false;
		}
		nntpSession.WriteLine("POST");
		string text = nntpSession.ReadLine();
		if (text == null || !text.StartsWith("340", StringComparison.Ordinal))
		{
			LastError = ((text != null) ? text : "POST not allowed.");
			return false;
		}
		nntpSession.WriteLine("From: " + ((fromLine != null) ? fromLine : "user@local"));
		nntpSession.WriteLine("Subject: " + ((subject != null) ? subject : "(no subject)"));
		nntpSession.WriteLine("Newsgroups: " + ((newsgroups != null) ? newsgroups : "misc.test"));
		nntpSession.WriteLine("");
		string[] array = ((body != null) ? body : "").Split(new string[2] { "\r\n", "\n" }, StringSplitOptions.None);
		foreach (string text2 in array)
		{
			if (Operators.CompareString(text2, ".", TextCompare: false) == 0)
			{
				nntpSession.WriteLine("..");
			}
			else
			{
				nntpSession.WriteLine(text2);
			}
		}
		nntpSession.WriteLine(".");
		string text3 = nntpSession.ReadLine();
		if (text3 != null && text3.StartsWith("240", StringComparison.Ordinal))
		{
			return true;
		}
		LastError = ((text3 != null) ? text3 : "Post rejected.");
		return false;
	}

	private static NntpSession Connect(string host, int port)
	{
		string text = ((host != null) ? host : "").Trim();
		NntpSession result;
		if (text.Length == 0)
		{
			LastError = "NNTP host is required.";
			result = null;
		}
		else
		{
			if (port <= 0)
			{
				port = 119;
			}
			try
			{
				NntpSession nntpSession = new NntpSession(text, port);
				string text2 = nntpSession.ReadLine();
				if (text2 == null || !text2.StartsWith("200", StringComparison.Ordinal))
				{
					LastError = ((text2 != null) ? text2 : "NNTP greeting failed.");
					nntpSession.Dispose();
					result = null;
				}
				else
				{
					result = nntpSession;
				}
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Exception ex2 = ex;
				LastError = ex2.Message;
				result = null;
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}
}
