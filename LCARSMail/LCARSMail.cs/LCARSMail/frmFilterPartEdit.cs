using System;
using System.Drawing;
using System.Windows.Forms;
using LCARS;
using LCARS.Controls;

namespace LCARSMail;

public class frmFilterPartEdit : LCARSForm
{
	private readonly bool editCondition;

	private ComboBox cboField;

	private ComboBox cboOp;

	private TextBox txtValue;

	private ComboBox cboAction;

	private TextBox txtActionValue;

	public frmFilterPartEdit(bool forCondition, MailFilterCondition cond, MailFilterAction act)
	{
		editCondition = forCondition;
		Text = (forCondition ? "CONDITION" : "ACTION");
		BackColor = Color.Black;
		Size clientSize = new Size(480, forCondition ? 220 : 200);
		ClientSize = clientSize;
		StartPosition = FormStartPosition.CenterParent;
		int num = 12;
		checked
		{
			if (forCondition)
			{
				AddPrompt("FIELD", 12, num);
				cboField = AddCombo(12, num + 18, 450);
				cboField.Items.AddRange(new object[8] { "subject", "from", "to", "cc", "toorcc", "body", "age", "size" });
				num += 48;
				AddPrompt("OPERATOR", 12, num);
				cboOp = AddCombo(12, num + 18, 450);
				cboOp.Items.AddRange(new object[7] { "contains", "doesn't contain", "is", "isn't", "begins with", "ends with", "matches" });
				num += 48;
				AddPrompt("VALUE", 12, num);
				txtValue = AddField(12, num + 18, 450);
				if (cond != null)
				{
					ComboBox cbo = cboField;
					MailFilterCondition mailFilterCondition = cond;
					SelectCombo(cbo, (mailFilterCondition.Field != null) ? mailFilterCondition.Field : "");
					ComboBox cbo2 = cboOp;
					mailFilterCondition = cond;
					SelectCombo(cbo2, (mailFilterCondition.Operator != null) ? mailFilterCondition.Operator : "");
					TextBox textBox = txtValue;
					mailFilterCondition = cond;
					textBox.Text = ((mailFilterCondition.Value != null) ? mailFilterCondition.Value : "");
				}
			}
			else
			{
				AddPrompt("ACTION", 12, num);
				cboAction = AddCombo(12, num + 18, 450);
				cboAction.Items.AddRange(new object[10] { "Move to folder", "Copy to folder", "Mark read", "Mark unread", "Delete", "Mark as junk", "Add tag", "Forward", "Reply to sender", "Stop execution" });
				num += 48;
				AddPrompt("VALUE (FOLDER / TAG / ADDRESS)", 12, num);
				txtActionValue = AddField(12, num + 18, 450);
				if (act != null)
				{
					ComboBox cbo3 = cboAction;
					MailFilterAction mailFilterAction = act;
					SelectCombo(cbo3, (mailFilterAction.ActionName != null) ? mailFilterAction.ActionName : "");
					TextBox textBox2 = txtActionValue;
					mailFilterAction = act;
					textBox2.Text = ((mailFilterAction.ActionValue != null) ? mailFilterAction.ActionValue : "");
				}
			}
			FlatButton flatButton = new FlatButton
			{
				ButtonText = "OK",
				Text = "OK",
				Color = LCARScolorStyles.PrimaryFunction
			};
			clientSize = new Size(80, 28);
			flatButton.Size = clientSize;
			Point location = new Point(280, ClientSize.Height - 44);
			flatButton.Location = location;
			flatButton.Click += OnOkClick;
			Controls.Add(flatButton);
			FlatButton flatButton2 = new FlatButton
			{
				ButtonText = "CANCEL",
				Text = "CANCEL",
				Color = LCARScolorStyles.Orange
			};
			clientSize = new Size(80, 28);
			flatButton2.Size = clientSize;
			location = (flatButton2.Location = new Point(370, ClientSize.Height - 44));
			flatButton2.Click += OnCancelClick;
			Controls.Add(flatButton2);
		}
	}

	private static void SelectCombo(ComboBox cbo, string value)
	{
		if (cbo == null)
		{
			return;
		}
		string text = ((value != null) ? value : "").Trim();
		if (text.Length == 0)
		{
			return;
		}
		checked
		{
			int num = cbo.Items.Count - 1;
			for (int i = 0; i <= num; i++)
			{
				if (string.Equals(cbo.Items[i].ToString(), text, StringComparison.OrdinalIgnoreCase))
				{
					cbo.SelectedIndex = i;
					return;
				}
			}
			cbo.Items.Add(text);
			cbo.SelectedIndex = cbo.Items.Count - 1;
		}
	}

	private void OnOkClick(object sender, EventArgs e)
	{
		DialogResult = DialogResult.OK;
		Close();
	}

	private void OnCancelClick(object sender, EventArgs e)
	{
		DialogResult = DialogResult.Cancel;
		Close();
	}

	public MailFilterCondition AsCondition()
	{
		MailFilterCondition mailFilterCondition = new MailFilterCondition();
		mailFilterCondition.Field = ((cboField != null && cboField.SelectedItem != null) ? cboField.SelectedItem.ToString() : cboField.Text).Trim();
		mailFilterCondition.Operator = ((cboOp != null && cboOp.SelectedItem != null) ? cboOp.SelectedItem.ToString() : cboOp.Text).Trim();
		mailFilterCondition.Value = ((txtValue != null) ? txtValue.Text.Trim() : "");
		return mailFilterCondition;
	}

	public MailFilterAction AsAction()
	{
		MailFilterAction mailFilterAction = new MailFilterAction();
		mailFilterAction.ActionName = ((cboAction != null && cboAction.SelectedItem != null) ? cboAction.SelectedItem.ToString() : cboAction.Text).Trim();
		mailFilterAction.ActionValue = ((txtActionValue != null) ? txtActionValue.Text.Trim() : "");
		return mailFilterAction;
	}

	private void AddPrompt(string text, int x, int y)
	{
		Label label = new Label();
		label.Text = text;
		label.ForeColor = Color.Orange;
		label.BackColor = Color.Black;
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

	private ComboBox AddCombo(int x, int y, int w)
	{
		ComboBox comboBox = new ComboBox();
		Point location = new Point(x, y);
		comboBox.Location = location;
		Size size = new Size(w, 24);
		comboBox.Size = size;
		comboBox.DropDownStyle = ComboBoxStyle.DropDown;
		comboBox.BackColor = Color.FromArgb(40, 40, 40);
		comboBox.ForeColor = Color.Orange;
		Controls.Add(comboBox);
		return comboBox;
	}
}
