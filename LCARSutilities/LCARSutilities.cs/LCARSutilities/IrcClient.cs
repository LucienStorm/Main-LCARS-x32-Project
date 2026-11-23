using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSutilities;

public class IrcClient : IDisposable
{
	private TcpClient client;

	private NetworkStream stream;

	private StreamReader reader;

	private StreamWriter writer;

	private Thread readThread;

	private bool stopRequested;

	[method: DebuggerNonUserCode]
	public event Action<string> LineReceived;

	[DebuggerNonUserCode]
	public IrcClient()
	{
	}

	public void Connect(string host, int port, string nick, string channel)
	{
		Disconnect();
		client = new TcpClient();
		client.Connect(host, port);
		stream = client.GetStream();
		reader = new StreamReader(stream, Encoding.UTF8);
		writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
		{
			AutoFlush = true
		};
		SendRaw("NICK " + nick);
		SendRaw("USER " + nick + " 0 * :LCARS Chat");
		if (!string.IsNullOrWhiteSpace(channel))
		{
			SendRaw("JOIN " + channel.Trim());
		}
		stopRequested = false;
		readThread = new Thread(ReadLoop)
		{
			IsBackground = true
		};
		readThread.Start();
	}

	public void SendRaw(string line)
	{
		if (writer != null)
		{
			writer.WriteLine(line);
		}
	}

	public void SendChannelMessage(string target, string text)
	{
		SendRaw("PRIVMSG " + target + " :" + text);
	}

	private void ReadLoop()
	{
		try
		{
			while (!stopRequested && reader != null)
			{
				string text = reader.ReadLine();
				if (text == null)
				{
					break;
				}
				if (text.StartsWith("PING ", StringComparison.OrdinalIgnoreCase))
				{
					SendRaw("PONG " + text.Substring(5));
				}
				else
				{
					LineReceived?.Invoke(text);
				}
			}
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
	}

	public void Disconnect()
	{
		stopRequested = true;
		try
		{
			if (reader != null)
			{
				reader.Dispose();
			}
			if (writer != null)
			{
				writer.Dispose();
			}
			if (stream != null)
			{
				stream.Dispose();
			}
			if (client != null)
			{
				client.Close();
			}
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
		reader = null;
		writer = null;
		stream = null;
		client = null;
	}

	public void Dispose()
	{
		Disconnect();
	}

	void IDisposable.Dispose()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Dispose
		this.Dispose();
	}
}
