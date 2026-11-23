using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using LCARS;
using LCARS.Controls;

namespace LCARSMail;

public class frmPasswordPrompt : LCARSForm
{
	private readonly string promptText;

	private TextBox txtPassword;

	private FlatButton fbShow;

	private FlatButton fbOk;

	private FlatButton fbCancel;

	private bool obscured;

	public string Password
	{
		[DebuggerNonUserCode]
		get;
		[DebuggerNonUserCode]
		set;
	}

	public frmPasswordPrompt(string prompt, string title = "LCARS COMMS — PASSWORD")
	{
		obscured = true;
		promptText = ((prompt != null) ? prompt : "");
		Text = (string.IsNullOrWhiteSpace(title) ? "LCARS COMMS — PASSWORD" : title.Trim());
		BackColor = Color.Black;
		ForeColor = Color.Orange;
		FormBorderStyle = FormBorderStyle.None;
		ControlBox = false;
		DoubleBuffered = true;
		BindToWorkingArea = false;
		StartPosition = FormStartPosition.CenterParent;
		Size clientSize = new Size(440, 220);
		ClientSize = clientSize;
		ShowInTaskbar = false;
		FlatButton flatButton = new FlatButton();
		flatButton.ButtonText = Text.ToUpperInvariant();
		flatButton.Text = flatButton.ButtonText;
		flatButton.Clickable = false;
		flatButton.Color = LCARScolorStyles.StaticTan;
		flatButton.ButtonTextAlign = ContentAlignment.BottomRight;
		clientSize = new Size(416, 32);
		flatButton.Size = clientSize;
		Point location = new Point(12, 8);
		flatButton.Location = location;
		Controls.Add(flatButton);
		Label label = new Label
		{
			AutoSize = false,
			ForeColor = Color.Orange,
			BackColor = Color.Black,
			Font = new Font("LCARS", 14f, FontStyle.Regular)
		};
		location = new Point(12, 48);
		label.Location = location;
		clientSize = new Size(416, 56);
		label.Size = clientSize;
		label.Text = promptText;
		Controls.Add(label);
		txtPassword = new TextBox();
		TextBox textBox = txtPassword;
		location = new Point(12, 112);
		textBox.Location = location;
		TextBox textBox2 = txtPassword;
		clientSize = new Size(320, 28);
		textBox2.Size = clientSize;
		txtPassword.BackColor = Color.Black;
		txtPassword.ForeColor = Color.Orange;
		txtPassword.Font = new Font("LCARS", 14f, FontStyle.Regular);
		txtPassword.BorderStyle = BorderStyle.FixedSingle;
		txtPassword.UseSystemPasswordChar = true;
		Controls.Add(txtPassword);
		location = new Point(340, 110);
		fbShow = MakeBtn("SHOW", LCARScolorStyles.NavigationFunction, location, 88);
		fbShow.Click += OnToggleObscure;
		Controls.Add(fbShow);
		location = new Point(248, 168);
		fbOk = MakeBtn("OK", LCARScolorStyles.PrimaryFunction, location, 88);
		fbOk.Click += OnOk;
		Controls.Add(fbOk);
		location = new Point(340, 168);
		fbCancel = MakeBtn("CANCEL", LCARScolorStyles.CriticalFunction, location, 88);
		fbCancel.Click += OnCancel;
		Controls.Add(fbCancel);
		AcceptButton = null;
		CancelButton = null;
		txtPassword.KeyDown += OnPasswordKeyDown;
	}

	public static string Prompt(IWin32Window owner, string promptText, string title = "LCARS COMMS — PASSWORD")
	{
		using frmPasswordPrompt frmPasswordPrompt2 = new frmPasswordPrompt(promptText, title);
		DialogResult dialogResult = ((owner == null) ? frmPasswordPrompt2.ShowDialog() : frmPasswordPrompt2.ShowDialog(owner));
		if (dialogResult != DialogResult.OK)
		{
			return null;
		}
		return frmPasswordPrompt2.Password;
	}

	protected override void OnShown(EventArgs e)
	{
		base.OnShown(e);
		txtPassword.Focus();
	}

	private FlatButton MakeBtn(string caption, LCARScolorStyles color, Point loc, int width)
	{
		FlatButton flatButton = new FlatButton();
		flatButton.ButtonText = caption;
		flatButton.Text = caption;
		flatButton.Color = color;
		flatButton.ButtonTextAlign = ContentAlignment.MiddleCenter;
		Size size = new Size(width, 32);
		flatButton.Size = size;
		flatButton.Location = loc;
		return flatButton;
	}

	private void OnToggleObscure(object sender, EventArgs e)
	{
		obscured = !obscured;
		txtPassword.UseSystemPasswordChar = obscured;
		if (obscured)
		{
			fbShow.ButtonText = "SHOW";
			fbShow.Text = "SHOW";
		}
		else
		{
			fbShow.ButtonText = "HIDE";
			fbShow.Text = "HIDE";
		}
	}

	private void OnPasswordKeyDown(object sender, KeyEventArgs e)
	{
		if (e.KeyCode == Keys.Return)
		{
			e.Handled = true;
			e.SuppressKeyPress = true;
			OnOk(RuntimeHelpers.GetObjectValue(sender), e);
		}
		else if (e.KeyCode == Keys.Escape)
		{
			e.Handled = true;
			e.SuppressKeyPress = true;
			OnCancel(RuntimeHelpers.GetObjectValue(sender), e);
		}
	}

	private void OnOk(object sender, EventArgs e)
	{
		string text = txtPassword.Text;
		Password = ((text != null) ? text : "");
		DialogResult = DialogResult.OK;
		Close();
	}

	private void OnCancel(object sender, EventArgs e)
	{
		Password = null;
		DialogResult = DialogResult.Cancel;
		Close();
	}
}
