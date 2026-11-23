using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using LCARS;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSutilities;

public class frmChat : LCARSForm
{
	private const int FormW = 720;

	private const int FormH = 520;

	private TextBox txtServer;

	private TextBox txtNick;

	private TextBox txtChannel;

	private TextBox txtTranscript;

	private TextBox txtSend;

	private Label lblStatus;

	private IrcClient irc;

	public frmChat()
	{
		modUtilities.InitUtilityFormFloating(this, 720, 520);
		BuildUi();
	}

	private void BuildUi()
	{
		modUtilities.MakeUtilityTitle(this, "CHAT IRC", 420);
		modUtilities.MakeUtilityButton(this, "CONNECT", 418, 12, 110, 28, LCARScolorStyles.PrimaryFunction, OnConnect);
		modUtilities.MakeUtilityButton(this, "CLOSE", 588, 12, 110, 28, LCARScolorStyles.Orange, [SpecialName] (object s, EventArgs e) =>
		{
			Close();
		});
		txtServer = MakeField(48, "irc.libera.chat:6667");
		txtNick = MakeField(78, "lcars" + new Random().Next(1000, 9999));
		txtChannel = MakeField(108, "#test");
		txtTranscript = new TextBox();
		txtTranscript.Multiline = true;
		txtTranscript.ReadOnly = true;
		txtTranscript.ScrollBars = ScrollBars.Vertical;
		TextBox textBox = txtTranscript;
		Point location = new Point(12, 140);
		textBox.Location = location;
		TextBox textBox2 = txtTranscript;
		Size size = new Size(696, 300);
		textBox2.Size = size;
		txtTranscript.BackColor = Color.FromArgb(20, 20, 20);
		txtTranscript.ForeColor = Color.Orange;
		Controls.Add(txtTranscript);
		txtSend = MakeField(452, "");
		txtSend.Width = 586;
		txtSend.KeyDown += OnSendKey;
		modUtilities.MakeUtilityButton(this, "SEND", 598, 448, 100, 28, LCARScolorStyles.MiscFunction, OnSendClick);
		lblStatus = modUtilities.MakeUtilityLabel(this, 12, 484, 696, 20, 10f);
		lblStatus.Text = "DISCONNECTED";
	}

	private TextBox MakeField(int y, string defaultText)
	{
		TextBox textBox = new TextBox();
		Point location = new Point(12, y);
		textBox.Location = location;
		textBox.Width = 696;
		textBox.BackColor = Color.FromArgb(40, 40, 40);
		textBox.ForeColor = Color.White;
		textBox.Text = defaultText;
		Controls.Add(textBox);
		return textBox;
	}

	private void OnConnect(object sender, EventArgs e)
	{
		try
		{
			if (irc != null)
			{
				irc.Dispose();
			}
			irc = new IrcClient();
			irc.LineReceived += OnIrcLine;
			string[] array = txtServer.Text.Split(':');
			string text = array[0].Trim();
			int result = 6667;
			if (array.Length > 1)
			{
				int.TryParse(array[1], out result);
			}
			irc.Connect(text, result, txtNick.Text.Trim(), txtChannel.Text.Trim());
			lblStatus.Text = "CONNECTED " + text;
		}
		catch (Exception ex)
		{
			ProjectData.SetProjectError(ex);
			Exception ex2 = ex;
			lblStatus.Text = ex2.Message.ToUpperInvariant();
			ProjectData.ClearProjectError();
		}
	}

	private void OnIrcLine(string line)
	{
		if (InvokeRequired)
		{
			BeginInvoke(new Action<string>(OnIrcLine), line);
		}
		else
		{
			txtTranscript.AppendText(line + "\r\n");
		}
	}

	private void OnSendKey(object sender, KeyEventArgs e)
	{
		if (e.KeyCode == Keys.Return)
		{
			e.SuppressKeyPress = true;
			OnSendClick(RuntimeHelpers.GetObjectValue(sender), EventArgs.Empty);
		}
	}

	private void OnSendClick(object sender, EventArgs e)
	{
		if (irc == null)
		{
			return;
		}
		string text = txtChannel.Text.Trim();
		if (text.Length != 0)
		{
			string text2 = txtSend.Text.Trim();
			if (text2.Length != 0)
			{
				irc.SendChannelMessage(text, text2);
				txtSend.Clear();
			}
		}
	}

	protected override void OnFormClosed(FormClosedEventArgs e)
	{
		if (irc != null)
		{
			irc.Dispose();
		}
		base.OnFormClosed(e);
	}
}
