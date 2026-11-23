using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using LCARS;
using LCARS.Controls;

namespace LCARSMail;

public class frmChat : LCARSForm
{
	private TextBox txtHomeserver;

	private TextBox txtUser;

	private TextBox txtPassword;

	private TextBox txtToken;

	private TextBox txtRoom;

	private TextBox txtMessage;

	private TextBox txtLog;

	private ListBox lstRooms;

	private Label lblStatus;

	private TextBox txtXmppHost;

	private TextBox txtXmppPort;

	private TextBox txtXmppJid;

	private TextBox txtXmppPassword;

	private TextBox txtXmppTo;

	private TextBox txtXmppMessage;

	public frmChat()
	{
		BackColor = Color.Black;
		Size clientSize = new Size(640, 648);
		ClientSize = clientSize;
		StartPosition = FormStartPosition.CenterParent;
		BuildUi();
		TryLoadSavedSession();
		TryLoadXmppAccount();
	}

	private void BuildUi()
	{
		AddLabel("HOMESERVER", 12, 12);
		txtHomeserver = AddField(12, 32, 610);
		AddLabel("MATRIX USER", 12, 64);
		txtUser = AddField(12, 84, 280);
		AddLabel("PASSWORD", 300, 64);
		txtPassword = AddField(300, 84, 322);
		txtPassword.UseSystemPasswordChar = true;
		FlatButton flatButton = new FlatButton();
		flatButton.ButtonText = "LOGIN";
		flatButton.Text = "LOGIN";
		flatButton.Color = LCARScolorStyles.PrimaryFunction;
		Size size = new Size(90, 28);
		flatButton.Size = size;
		Point location = new Point(532, 82);
		flatButton.Location = location;
		flatButton.Click += OnLogin;
		Controls.Add(flatButton);
		AddLabel("ACCESS TOKEN", 12, 116);
		txtToken = AddField(12, 136, 610);
		txtToken.UseSystemPasswordChar = true;
		AddLabel("ROOM ID", 12, 168);
		txtRoom = AddField(12, 188, 400);
		lstRooms = new ListBox();
		ListBox listBox = lstRooms;
		location = new Point(420, 188);
		listBox.Location = location;
		ListBox listBox2 = lstRooms;
		size = new Size(202, 80);
		listBox2.Size = size;
		lstRooms.BackColor = Color.FromArgb(30, 30, 30);
		lstRooms.ForeColor = Color.Orange;
		lstRooms.SelectedIndexChanged += OnRoomSelected;
		Controls.Add(lstRooms);
		txtMessage = AddField(12, 220, 500);
		FlatButton flatButton2 = new FlatButton();
		flatButton2.ButtonText = "SEND";
		flatButton2.Text = "SEND";
		flatButton2.Color = LCARScolorStyles.PrimaryFunction;
		size = new Size(90, 28);
		flatButton2.Size = size;
		location = new Point(532, 218);
		flatButton2.Location = location;
		flatButton2.Click += OnSend;
		Controls.Add(flatButton2);
		FlatButton flatButton3 = new FlatButton();
		flatButton3.ButtonText = "PULL";
		flatButton3.Text = "PULL";
		flatButton3.Color = LCARScolorStyles.SystemFunction;
		size = new Size(90, 28);
		flatButton3.Size = size;
		location = new Point(532, 252);
		flatButton3.Location = location;
		flatButton3.Click += OnPull;
		Controls.Add(flatButton3);
		FlatButton flatButton4 = new FlatButton();
		flatButton4.ButtonText = "ROOMS";
		flatButton4.Text = "ROOMS";
		flatButton4.Color = LCARScolorStyles.MiscFunction;
		size = new Size(90, 28);
		flatButton4.Size = size;
		location = new Point(532, 286);
		flatButton4.Location = location;
		flatButton4.Click += OnListRooms;
		Controls.Add(flatButton4);
		txtLog = new TextBox();
		txtLog.Multiline = true;
		txtLog.ScrollBars = ScrollBars.Vertical;
		txtLog.ReadOnly = true;
		TextBox textBox = txtLog;
		location = new Point(12, 276);
		textBox.Location = location;
		TextBox textBox2 = txtLog;
		size = new Size(510, 120);
		textBox2.Size = size;
		txtLog.BackColor = Color.FromArgb(30, 30, 30);
		txtLog.ForeColor = Color.Orange;
		Controls.Add(txtLog);
		AddLabel("XMPP HOST", 12, 408);
		txtXmppHost = AddField(12, 428, 200);
		AddLabel("PORT", 220, 408);
		txtXmppPort = AddField(220, 428, 60);
		txtXmppPort.Text = "5222";
		AddLabel("JID", 290, 408);
		txtXmppJid = AddField(290, 428, 332);
		AddLabel("XMPP PASSWORD", 12, 456);
		txtXmppPassword = AddField(12, 476, 280);
		txtXmppPassword.UseSystemPasswordChar = true;
		AddLabel("TO JID", 300, 456);
		txtXmppTo = AddField(300, 476, 322);
		txtXmppMessage = AddField(12, 508, 500);
		FlatButton flatButton5 = new FlatButton();
		flatButton5.ButtonText = "XMPP SEND";
		flatButton5.Text = "XMPP SEND";
		flatButton5.Color = LCARScolorStyles.CriticalFunction;
		size = new Size(110, 28);
		flatButton5.Size = size;
		location = new Point(520, 506);
		flatButton5.Location = location;
		flatButton5.Click += OnXmppSend;
		Controls.Add(flatButton5);
		FlatButton flatButton6 = new FlatButton();
		flatButton6.ButtonText = "XMPP PULL";
		flatButton6.Text = "XMPP PULL";
		flatButton6.Color = LCARScolorStyles.MiscFunction;
		size = new Size(110, 28);
		flatButton6.Size = size;
		location = new Point(520, 474);
		flatButton6.Location = location;
		flatButton6.Click += OnXmppPull;
		Controls.Add(flatButton6);
		FlatButton flatButton7 = new FlatButton();
		flatButton7.ButtonText = "ROSTER";
		flatButton7.Text = "ROSTER";
		flatButton7.Color = LCARScolorStyles.NavigationFunction;
		size = new Size(110, 28);
		flatButton7.Size = size;
		location = new Point(400, 506);
		flatButton7.Location = location;
		flatButton7.Click += OnXmppRoster;
		Controls.Add(flatButton7);
		Label label = new Label();
		label.ForeColor = Color.Gray;
		label.Font = new Font(label.Font.FontFamily, 8f);
		location = new Point(12, 540);
		label.Location = location;
		size = new Size(610, 36);
		label.Size = size;
		label.Text = "OTR SLICE 18 OPEN — Ruling: no libotr on net48 x86 (open-slice-acceptance-gaps-317.md). Matrix / TLS XMPP are substitutes only.";
		Controls.Add(label);
		lblStatus = new Label();
		lblStatus.ForeColor = Color.Orange;
		Label label2 = lblStatus;
		location = new Point(12, 578);
		label2.Location = location;
		Label label3 = lblStatus;
		size = new Size(610, 24);
		label3.Size = size;
		Controls.Add(lblStatus);
	}

	private void TryLoadSavedSession()
	{
		string homeserver = null;
		string userId = null;
		string accessToken = null;
		if (MatrixAccountStore.TryLoadSession(ref homeserver, ref userId, ref accessToken))
		{
			txtHomeserver.Text = homeserver;
			txtUser.Text = userId;
			txtToken.Text = accessToken;
		}
	}

	private void OnLogin(object sender, EventArgs e)
	{
		string accessToken = null;
		string userId = null;
		if (!MatrixChatService.TryPasswordLogin(txtHomeserver.Text, txtUser.Text, txtPassword.Text, ref accessToken, ref userId))
		{
			lblStatus.Text = ((MatrixChatService.LastError != null) ? MatrixChatService.LastError : "LOGIN FAILED").ToUpperInvariant();
			return;
		}
		txtToken.Text = accessToken;
		if (!string.IsNullOrWhiteSpace(userId))
		{
			txtUser.Text = userId;
		}
		MatrixAccountStore.SaveSession(txtHomeserver.Text, txtUser.Text, accessToken);
		lblStatus.Text = "LOGGED IN";
		OnListRooms(null, EventArgs.Empty);
	}

	private void OnListRooms(object sender, EventArgs e)
	{
		lstRooms.Items.Clear();
		List<MatrixChatService.MatrixRoomSummary> list = MatrixChatService.TryListJoinedRooms(txtHomeserver.Text, txtToken.Text);
		if (list == null)
		{
			lblStatus.Text = ((MatrixChatService.LastError != null) ? MatrixChatService.LastError : "ROOMS FAILED").ToUpperInvariant();
			return;
		}
		foreach (MatrixChatService.MatrixRoomSummary item in list)
		{
			if (item != null)
			{
				lstRooms.Items.Add(item);
			}
		}
		lblStatus.Text = list.Count + " ROOM(S)";
	}

	private void OnRoomSelected(object sender, EventArgs e)
	{
		if (lstRooms.SelectedItem is MatrixChatService.MatrixRoomSummary matrixRoomSummary)
		{
			txtRoom.Text = matrixRoomSummary.RoomId;
		}
	}

	private void OnSend(object sender, EventArgs e)
	{
		if (MatrixChatService.TrySendText(txtHomeserver.Text, txtToken.Text, txtRoom.Text, txtMessage.Text))
		{
			lblStatus.Text = "SENT";
			txtMessage.Clear();
		}
		else
		{
			lblStatus.Text = ((MatrixChatService.LastError != null) ? MatrixChatService.LastError : "SEND FAILED").ToUpperInvariant();
		}
	}

	private void OnPull(object sender, EventArgs e)
	{
		string text = MatrixChatService.TryFetchRecentMessages(txtHomeserver.Text, txtToken.Text, txtRoom.Text);
		txtLog.Text = MatrixChatService.FormatRecentMessages((text != null) ? text : "");
		lblStatus.Text = "MESSAGES PULLED";
	}

	private void TryLoadXmppAccount()
	{
		string host = null;
		string jid = null;
		string password = null;
		int port = 5222;
		if (XmppAccountStore.TryLoadAccount(ref host, ref port, ref jid, ref password))
		{
			txtXmppHost.Text = host;
			txtXmppPort.Text = port.ToString();
			txtXmppJid.Text = jid;
			if (password != null)
			{
				txtXmppPassword.Text = password;
			}
		}
	}

	private void OnXmppRoster(object sender, EventArgs e)
	{
		int result = 5222;
		int.TryParse(txtXmppPort.Text.Trim(), out result);
		if (result <= 0)
		{
			result = 5222;
		}
		List<string> list = XmppChatService.TryFetchRosterJids(txtXmppHost.Text, result, txtXmppJid.Text, txtXmppPassword.Text);
		if (list == null || list.Count == 0)
		{
			lblStatus.Text = ((XmppChatService.LastError != null) ? XmppChatService.LastError : "ROSTER FAILED").ToUpperInvariant();
			return;
		}
		txtLog.Text = string.Join("\r\n", list.ToArray());
		lblStatus.Text = list.Count + " ROSTER ENTR(Y/IES)";
	}

	private void OnXmppPull(object sender, EventArgs e)
	{
		int result = 5222;
		int.TryParse(txtXmppPort.Text.Trim(), out result);
		if (result <= 0)
		{
			result = 5222;
		}
		XmppAccountStore.SaveAccount(txtXmppHost.Text, result, txtXmppJid.Text, txtXmppPassword.Text);
		string text = XmppChatService.TryPullIncomingMessages(txtXmppHost.Text, result, txtXmppJid.Text, txtXmppPassword.Text, 8);
		if (text == null || text.Length == 0)
		{
			lblStatus.Text = ((XmppChatService.LastError != null) ? XmppChatService.LastError : "XMPP PULL FAILED").ToUpperInvariant();
			return;
		}
		txtLog.Text = text;
		lblStatus.Text = "XMPP MESSAGES";
	}

	private void OnXmppSend(object sender, EventArgs e)
	{
		int result = 5222;
		int.TryParse(txtXmppPort.Text.Trim(), out result);
		if (result <= 0)
		{
			result = 5222;
		}
		XmppAccountStore.SaveAccount(txtXmppHost.Text, result, txtXmppJid.Text, txtXmppPassword.Text);
		if (XmppChatService.TrySendMessage(txtXmppHost.Text, result, txtXmppJid.Text, txtXmppPassword.Text, txtXmppTo.Text, txtXmppMessage.Text))
		{
			lblStatus.Text = "XMPP SENT";
			txtXmppMessage.Clear();
		}
		else
		{
			lblStatus.Text = ((XmppChatService.LastError != null) ? XmppChatService.LastError : "XMPP FAILED").ToUpperInvariant();
		}
	}

	private void AddLabel(string text, int x, int y)
	{
		Label label = new Label();
		label.Text = text;
		label.ForeColor = Color.Orange;
		label.AutoSize = true;
		Point location = new Point(x, y);
		label.Location = location;
		Controls.Add(label);
	}

	private TextBox AddField(int x, int y, int w)
	{
		TextBox textBox = new TextBox();
		Point location = new Point(x, y);
		textBox.Location = location;
		Size size = new Size(w, 24);
		textBox.Size = size;
		textBox.BackColor = Color.FromArgb(40, 40, 40);
		textBox.ForeColor = Color.Orange;
		Controls.Add(textBox);
		return textBox;
	}
}
