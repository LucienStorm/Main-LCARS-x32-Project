using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

public sealed class XmppChatService
{
	public static string LastError;

	private XmppChatService()
	{
	}

	public static bool TrySendMessage(string host, int port, string jid, string password, string toJid, string body)
	{
		LastError = null;
		string text = ((host != null) ? host : "").Trim();
		string text2 = ((jid != null) ? jid : "").Trim();
		string text3 = ((toJid != null) ? toJid : "").Trim();
		string raw = ((body != null) ? body : "");
		bool result;
		if (text.Length == 0 || text2.Length == 0 || text3.Length == 0)
		{
			LastError = "Host, JID, and recipient are required.";
			result = false;
		}
		else
		{
			if (port <= 0)
			{
				port = 5222;
			}
			if (!text2.Contains("@"))
			{
				LastError = "JID must include @domain.";
				result = false;
			}
			else
			{
				string raw2 = text2.Substring(checked(text2.IndexOf('@') + 1));
				try
				{
					using (TcpClient tcpClient = new TcpClient())
					{
						tcpClient.Connect(text, port);
						using NetworkStream innerStream = tcpClient.GetStream();
						using SslStream sslStream = new SslStream(innerStream, leaveInnerStreamOpen: false, [SpecialName] (object a, X509Certificate b, X509Chain c, SslPolicyErrors d) => true);
						sslStream.AuthenticateAsClient(text);
						StreamWriter streamWriter = new StreamWriter(sslStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
						streamWriter.NewLine = "\n";
						streamWriter.AutoFlush = true;
						using StreamWriter streamWriter2 = streamWriter;
						using StreamReader reader = new StreamReader(sslStream, Encoding.UTF8);
						streamWriter2.WriteLine("<?xml version='1.0'?><stream:stream to=\"" + XmlEscape(raw2) + "\" xmlns=\"jabber:client\" xmlns:stream=\"http://etherx.jabber.org/streams\" version=\"1.0\">");
						WaitForFeatures(reader);
						string text4 = Convert.ToBase64String(Encoding.UTF8.GetBytes("\0" + text2 + "\0" + ((password != null) ? password : "")));
						streamWriter2.WriteLine("<auth xmlns=\"urn:ietf:params:xml:ns:xmpp-sasl\" mechanism=\"PLAIN\">" + text4 + "</auth>");
						ReadUntil(reader, "success");
						streamWriter2.WriteLine("<?xml version='1.0'?><stream:stream to=\"" + XmlEscape(raw2) + "\" xmlns=\"jabber:client\" xmlns:stream=\"http://etherx.jabber.org/streams\" version=\"1.0\">");
						WaitForFeatures(reader);
						string text5 = "lcars" + Guid.NewGuid().ToString("N").Substring(0, 8);
						streamWriter2.WriteLine("<iq type=\"set\" id=\"bind1\"><bind xmlns=\"urn:ietf:params:xml:ns:xmpp-bind\"><resource>" + text5 + "</resource></bind></iq>");
						ReadUntil(reader, "bind");
						string text6 = Guid.NewGuid().ToString("N");
						streamWriter2.WriteLine("<message to=\"" + XmlEscape(text3) + "\" type=\"chat\" id=\"" + text6 + "\"><body>" + XmlEscape(raw) + "</body></message>");
						streamWriter2.WriteLine("</stream:stream>");
					}
					result = true;
				}
				catch (Exception ex)
				{
					ProjectData.SetProjectError(ex);
					Exception ex2 = ex;
					LastError = ex2.Message;
					result = false;
					ProjectData.ClearProjectError();
				}
			}
		}
		return result;
	}

	public static string TryPullIncomingMessages(string host, int port, string jid, string password, int listenSeconds = 6)
	{
		LastError = null;
		string text = ((host != null) ? host : "").Trim();
		string text2 = ((jid != null) ? jid : "").Trim();
		string result;
		if (text.Length == 0 || text2.Length == 0)
		{
			LastError = "Host and JID are required.";
			result = "";
		}
		else
		{
			if (port <= 0)
			{
				port = 5222;
			}
			if (!text2.Contains("@"))
			{
				LastError = "JID must include @domain.";
				result = "";
			}
			else
			{
				string raw = text2.Substring(checked(text2.IndexOf('@') + 1));
				if (listenSeconds < 2)
				{
					listenSeconds = 2;
				}
				if (listenSeconds > 20)
				{
					listenSeconds = 20;
				}
				List<string> list = new List<string>();
				Regex regex = new Regex("<body>([\\s\\S]*?)</body>", RegexOptions.IgnoreCase);
				try
				{
					using (TcpClient tcpClient = new TcpClient())
					{
						tcpClient.Connect(text, port);
						using NetworkStream networkStream = tcpClient.GetStream();
						using SslStream sslStream = new SslStream(networkStream, leaveInnerStreamOpen: false, [SpecialName] (object a, X509Certificate b, X509Chain c, SslPolicyErrors d) => true);
						sslStream.AuthenticateAsClient(text);
						StreamWriter streamWriter = new StreamWriter(sslStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
						streamWriter.NewLine = "\n";
						streamWriter.AutoFlush = true;
						using StreamWriter streamWriter2 = streamWriter;
						using StreamReader streamReader = new StreamReader(sslStream, Encoding.UTF8);
						streamWriter2.WriteLine("<?xml version='1.0'?><stream:stream to=\"" + XmlEscape(raw) + "\" xmlns=\"jabber:client\" xmlns:stream=\"http://etherx.jabber.org/streams\" version=\"1.0\">");
						WaitForFeatures(streamReader);
						string text3 = Convert.ToBase64String(Encoding.UTF8.GetBytes("\0" + text2 + "\0" + ((password != null) ? password : "")));
						streamWriter2.WriteLine("<auth xmlns=\"urn:ietf:params:xml:ns:xmpp-sasl\" mechanism=\"PLAIN\">" + text3 + "</auth>");
						ReadUntil(streamReader, "success");
						streamWriter2.WriteLine("<?xml version='1.0'?><stream:stream to=\"" + XmlEscape(raw) + "\" xmlns=\"jabber:client\" xmlns:stream=\"http://etherx.jabber.org/streams\" version=\"1.0\">");
						WaitForFeatures(streamReader);
						string text4 = "lcars" + Guid.NewGuid().ToString("N").Substring(0, 8);
						streamWriter2.WriteLine("<iq type=\"set\" id=\"bind1\"><bind xmlns=\"urn:ietf:params:xml:ns:xmpp-bind\"><resource>" + text4 + "</resource></bind></iq>");
						ReadUntil(streamReader, "bind");
						DateTime t = DateTime.UtcNow.AddSeconds(listenSeconds);
						StringBuilder stringBuilder = new StringBuilder();
						while (DateTime.Compare(DateTime.UtcNow, t) < 0 && sslStream.CanRead)
						{
							if (networkStream.DataAvailable)
							{
								string text5 = streamReader.ReadLine();
								if (text5 == null)
								{
									break;
								}
								stringBuilder.AppendLine(text5);
								foreach (Match item in regex.Matches(text5))
								{
									string text6 = DecodeXmlText(item.Groups[1].Value);
									if (text6.Length > 0)
									{
										list.Add(text6);
									}
								}
							}
							else
							{
								Thread.Sleep(200);
							}
						}
						streamWriter2.WriteLine("</stream:stream>");
					}
					result = ((list.Count != 0) ? string.Join("\r\n", list.ToArray()) : ("(NO MESSAGES IN " + listenSeconds + "S)"));
				}
				catch (Exception ex)
				{
					ProjectData.SetProjectError(ex);
					Exception ex2 = ex;
					LastError = ex2.Message;
					result = "";
					ProjectData.ClearProjectError();
				}
			}
		}
		return result;
	}

	private static void WaitForFeatures(StreamReader reader)
	{
		DateTime t = DateTime.UtcNow.AddSeconds(8.0);
		while (DateTime.Compare(DateTime.UtcNow, t) < 0)
		{
			string text = reader.ReadLine();
			if (text == null || text.IndexOf("<stream:features", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("<features", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				break;
			}
		}
	}

	private static void ReadUntil(StreamReader reader, string token)
	{
		DateTime t = DateTime.UtcNow.AddSeconds(8.0);
		while (DateTime.Compare(DateTime.UtcNow, t) < 0)
		{
			string text = reader.ReadLine();
			if (text == null)
			{
				break;
			}
			if (text.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return;
			}
		}
		throw new IOException("XMPP stream did not reach " + token + ".");
	}

	private static string XmlEscape(string raw)
	{
		return SecurityElement.Escape((raw != null) ? raw : "");
	}

	public static List<string> TryFetchRosterJids(string host, int port, string jid, string password)
	{
		LastError = null;
		List<string> list = new List<string>();
		string text = ((host != null) ? host : "").Trim();
		string text2 = ((jid != null) ? jid : "").Trim();
		List<string> result;
		if (text.Length == 0 || text2.Length == 0)
		{
			LastError = "Host and JID are required.";
			result = list;
		}
		else
		{
			if (port <= 0)
			{
				port = 5222;
			}
			if (!text2.Contains("@"))
			{
				LastError = "JID must include @domain.";
				result = list;
			}
			else
			{
				string raw = text2.Substring(checked(text2.IndexOf('@') + 1));
				Regex regex = new Regex("jid=['\"]([^'\"]+)['\"]", RegexOptions.IgnoreCase);
				try
				{
					using (TcpClient tcpClient = new TcpClient())
					{
						tcpClient.Connect(text, port);
						using NetworkStream networkStream = tcpClient.GetStream();
						using SslStream sslStream = new SslStream(networkStream, leaveInnerStreamOpen: false, [SpecialName] (object a, X509Certificate b, X509Chain c, SslPolicyErrors d) => true);
						sslStream.AuthenticateAsClient(text);
						StreamWriter streamWriter = new StreamWriter(sslStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
						streamWriter.NewLine = "\n";
						streamWriter.AutoFlush = true;
						using StreamWriter streamWriter2 = streamWriter;
						using StreamReader streamReader = new StreamReader(sslStream, Encoding.UTF8);
						streamWriter2.WriteLine("<?xml version='1.0'?><stream:stream to=\"" + XmlEscape(raw) + "\" xmlns=\"jabber:client\" xmlns:stream=\"http://etherx.jabber.org/streams\" version=\"1.0\">");
						WaitForFeatures(streamReader);
						string text3 = Convert.ToBase64String(Encoding.UTF8.GetBytes("\0" + text2 + "\0" + ((password != null) ? password : "")));
						streamWriter2.WriteLine("<auth xmlns=\"urn:ietf:params:xml:ns:xmpp-sasl\" mechanism=\"PLAIN\">" + text3 + "</auth>");
						ReadUntil(streamReader, "success");
						streamWriter2.WriteLine("<?xml version='1.0'?><stream:stream to=\"" + XmlEscape(raw) + "\" xmlns=\"jabber:client\" xmlns:stream=\"http://etherx.jabber.org/streams\" version=\"1.0\">");
						WaitForFeatures(streamReader);
						string text4 = "lcars" + Guid.NewGuid().ToString("N").Substring(0, 8);
						streamWriter2.WriteLine("<iq type=\"set\" id=\"bind1\"><bind xmlns=\"urn:ietf:params:xml:ns:xmpp-bind\"><resource>" + text4 + "</resource></bind></iq>");
						ReadUntil(streamReader, "bind");
						streamWriter2.WriteLine("<iq type=\"get\" id=\"roster1\"><query xmlns=\"jabber:iq:roster\"/></iq>");
						DateTime t = DateTime.UtcNow.AddSeconds(6.0);
						StringBuilder stringBuilder = new StringBuilder();
						while (DateTime.Compare(DateTime.UtcNow, t) < 0)
						{
							if (!networkStream.DataAvailable)
							{
								Thread.Sleep(150);
								continue;
							}
							string text5 = streamReader.ReadLine();
							if (text5 == null)
							{
								break;
							}
							stringBuilder.AppendLine(text5);
							foreach (Match item in regex.Matches(text5))
							{
								string text6 = item.Groups[1].Value.Trim();
								if (text6.Length > 0 && !list.Contains(text6))
								{
									list.Add(text6);
								}
							}
							if (text5.IndexOf("</iq>", StringComparison.OrdinalIgnoreCase) < 0 || list.Count <= 0)
							{
								continue;
							}
							break;
						}
						streamWriter2.WriteLine("</stream:stream>");
					}
					result = list;
				}
				catch (Exception ex)
				{
					ProjectData.SetProjectError(ex);
					Exception ex2 = ex;
					LastError = ex2.Message;
					result = list;
					ProjectData.ClearProjectError();
				}
			}
		}
		return result;
	}

	private static string DecodeXmlText(string raw)
	{
		string text = ((raw != null) ? raw : "").Trim();
		if (text.Length == 0)
		{
			return "";
		}
		text = text.Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&")
			.Replace("&quot;", "\"")
			.Replace("&apos;", "'");
		return text.Trim();
	}
}
