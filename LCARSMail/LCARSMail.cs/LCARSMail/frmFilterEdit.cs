using System;
using System.Drawing;
using System.Windows.Forms;
using LCARS;
using LCARS.Controls;

namespace LCARSMail;

public class frmFilterEdit : LCARSForm
{
	private const int MarginX = 12;

	private const int FieldH = 24;

	private const int LabelW = 88;

	private readonly MailFilterRule workingRule;

	private readonly bool isNewRule;

	private TextBox txtName;

	private CheckBox chkEnabled;

	private CheckBox chkRunIncoming;

	private ComboBox cboLogic;

	private ListBox lstConditions;

	private ListBox lstActions;

	private FlatButton fbOk;

	private FlatButton fbCancel;

	public MailFilterRule EditedRule => workingRule;

	public frmFilterEdit(MailFilterRule rule, bool createNew)
	{
		workingRule = CloneRule(rule);
		isNewRule = createNew;
		Text = (createNew ? "NEW FILTER" : "EDIT FILTER");
		BackColor = Color.Black;
		Size clientSize = new Size(640, 520);
		ClientSize = clientSize;
		StartPosition = FormStartPosition.CenterParent;
		BuildUi();
		LoadFromRule();
	}

	private static MailFilterRule CloneRule(MailFilterRule rule)
	{
		if (rule == null)
		{
			rule = new MailFilterRule();
		}
		MailFilterRule mailFilterRule = new MailFilterRule();
		mailFilterRule.Name = rule.Name;
		mailFilterRule.Enabled = rule.Enabled;
		mailFilterRule.RunOnIncoming = rule.RunOnIncoming;
		mailFilterRule.ConditionLogic = rule.ConditionLogic;
		MailFilterRule mailFilterRule2 = mailFilterRule;
		foreach (MailFilterCondition condition in rule.Conditions)
		{
			mailFilterRule2.Conditions.Add(new MailFilterCondition
			{
				Field = condition.Field,
				Operator = condition.Operator,
				Value = condition.Value
			});
		}
		foreach (MailFilterAction action in rule.Actions)
		{
			mailFilterRule2.Actions.Add(new MailFilterAction
			{
				ActionName = action.ActionName,
				ActionValue = action.ActionValue
			});
		}
		return mailFilterRule2;
	}

	private void BuildUi()
	{
		int num = 8;
		checked
		{
			AddLabel("NAME", 12, num + 4);
			txtName = AddText(108, num, 520);
			num += 30;
			chkEnabled = new CheckBox();
			chkEnabled.Text = "ENABLED";
			chkEnabled.ForeColor = Color.Orange;
			chkEnabled.BackColor = Color.Black;
			CheckBox checkBox = chkEnabled;
			Point location = new Point(108, num);
			checkBox.Location = location;
			chkEnabled.AutoSize = true;
			Controls.Add(chkEnabled);
			chkRunIncoming = new CheckBox();
			chkRunIncoming.Text = "RUN ON INCOMING";
			chkRunIncoming.ForeColor = Color.Orange;
			chkRunIncoming.BackColor = Color.Black;
			CheckBox checkBox2 = chkRunIncoming;
			location = new Point(280, num);
			checkBox2.Location = location;
			chkRunIncoming.AutoSize = true;
			Controls.Add(chkRunIncoming);
			Label label = new Label();
			label.Text = "CONDITION FIELD: body · subject · from · to (body uses IMAP fetch)";
			label.ForeColor = Color.Orange;
			label.BackColor = Color.Black;
			label.AutoSize = true;
			location = new Point(108, num + 28);
			label.Location = location;
			Controls.Add(label);
			num += 32;
			AddLabel("LOGIC", 12, num + 4);
			cboLogic = new ComboBox();
			cboLogic.DropDownStyle = ComboBoxStyle.DropDownList;
			cboLogic.Items.Add("AND");
			cboLogic.Items.Add("OR");
			ComboBox comboBox = cboLogic;
			location = new Point(108, num);
			comboBox.Location = location;
			ComboBox comboBox2 = cboLogic;
			Size size = new Size(80, 24);
			comboBox2.Size = size;
			cboLogic.BackColor = Color.FromArgb(40, 40, 40);
			cboLogic.ForeColor = Color.Orange;
			Controls.Add(cboLogic);
			num += 34;
			AddLabel("CONDITIONS", 12, num);
			lstConditions = new ListBox();
			ListBox listBox = lstConditions;
			location = new Point(108, num);
			listBox.Location = location;
			ListBox listBox2 = lstConditions;
			size = new Size(520, 100);
			listBox2.Size = size;
			lstConditions.BackColor = Color.FromArgb(30, 30, 30);
			lstConditions.ForeColor = Color.Orange;
			Controls.Add(lstConditions);
			FlatButton value = MakeBtn("ADD", 12, num + 104, OnAddCondition);
			FlatButton value2 = MakeBtn("EDIT", 102, num + 104, OnEditCondition);
			FlatButton value3 = MakeBtn("DEL", 192, num + 104, OnDeleteCondition);
			Controls.Add(value);
			Controls.Add(value2);
			Controls.Add(value3);
			num += 140;
			AddLabel("ACTIONS", 12, num);
			lstActions = new ListBox();
			ListBox listBox3 = lstActions;
			location = new Point(108, num);
			listBox3.Location = location;
			ListBox listBox4 = lstActions;
			size = new Size(520, 100);
			listBox4.Size = size;
			lstActions.BackColor = Color.FromArgb(30, 30, 30);
			lstActions.ForeColor = Color.Orange;
			Controls.Add(lstActions);
			FlatButton value4 = MakeBtn("ADD", 12, num + 104, OnAddAction);
			FlatButton value5 = MakeBtn("EDIT", 102, num + 104, OnEditAction);
			FlatButton value6 = MakeBtn("DEL", 192, num + 104, OnDeleteAction);
			Controls.Add(value4);
			Controls.Add(value5);
			Controls.Add(value6);
			num += 150;
			fbOk = MakeBtn("SAVE", ClientSize.Width - 220, num, OnOkClick);
			fbOk.Color = LCARScolorStyles.PrimaryFunction;
			fbCancel = MakeBtn("CANCEL", ClientSize.Width - 110, num, OnCancelClick);
			fbCancel.Color = LCARScolorStyles.Orange;
			Controls.Add(fbOk);
			Controls.Add(fbCancel);
		}
	}

	private void LoadFromRule()
	{
		txtName.Text = ((workingRule.Name != null) ? workingRule.Name : "");
		chkEnabled.Checked = workingRule.Enabled;
		chkRunIncoming.Checked = workingRule.RunOnIncoming;
		cboLogic.SelectedItem = (string.Equals(workingRule.ConditionLogic, "OR", StringComparison.OrdinalIgnoreCase) ? "OR" : "AND");
		RefreshConditionList();
		RefreshActionList();
	}

	private void RefreshConditionList()
	{
		lstConditions.Items.Clear();
		foreach (MailFilterCondition condition in workingRule.Conditions)
		{
			ListBox.ObjectCollection items = lstConditions.Items;
			string[] array = new string[5];
			MailFilterCondition mailFilterCondition = condition;
			array[0] = ((mailFilterCondition.Field != null) ? mailFilterCondition.Field : "");
			array[1] = " ";
			MailFilterCondition mailFilterCondition2 = condition;
			array[2] = ((mailFilterCondition2.Operator != null) ? mailFilterCondition2.Operator : "");
			array[3] = " ";
			MailFilterCondition mailFilterCondition3 = condition;
			array[4] = ((mailFilterCondition3.Value != null) ? mailFilterCondition3.Value : "");
			items.Add(string.Concat(array));
		}
	}

	private void RefreshActionList()
	{
		lstActions.Items.Clear();
		foreach (MailFilterAction action in workingRule.Actions)
		{
			MailFilterAction mailFilterAction = action;
			string text = ((mailFilterAction.ActionName != null) ? mailFilterAction.ActionName : "");
			if (!string.IsNullOrEmpty(action.ActionValue))
			{
				text = text + " → " + action.ActionValue;
			}
			lstActions.Items.Add(text);
		}
	}

	private void OnCancelClick(object sender, EventArgs e)
	{
		DialogResult = DialogResult.Cancel;
		Close();
	}

	private void OnOkClick(object sender, EventArgs e)
	{
		workingRule.Name = txtName.Text.Trim();
		workingRule.Enabled = chkEnabled.Checked;
		workingRule.RunOnIncoming = chkRunIncoming.Checked;
		workingRule.ConditionLogic = ((cboLogic.SelectedItem == null) ? "AND" : cboLogic.SelectedItem.ToString());
		DialogResult = DialogResult.OK;
		Close();
	}

	private void OnAddCondition(object sender, EventArgs e)
	{
		using frmFilterPartEdit frmFilterPartEdit2 = new frmFilterPartEdit(forCondition: true, null, null);
		if (frmFilterPartEdit2.ShowDialog(this) == DialogResult.OK)
		{
			workingRule.Conditions.Add(frmFilterPartEdit2.AsCondition());
			RefreshConditionList();
		}
	}

	private void OnEditCondition(object sender, EventArgs e)
	{
		int selectedIndex = lstConditions.SelectedIndex;
		if (selectedIndex < 0 || selectedIndex >= workingRule.Conditions.Count)
		{
			return;
		}
		using frmFilterPartEdit frmFilterPartEdit2 = new frmFilterPartEdit(forCondition: true, workingRule.Conditions[selectedIndex], null);
		if (frmFilterPartEdit2.ShowDialog(this) == DialogResult.OK)
		{
			workingRule.Conditions[selectedIndex] = frmFilterPartEdit2.AsCondition();
			RefreshConditionList();
		}
	}

	private void OnDeleteCondition(object sender, EventArgs e)
	{
		int selectedIndex = lstConditions.SelectedIndex;
		if (selectedIndex >= 0 && selectedIndex < workingRule.Conditions.Count)
		{
			workingRule.Conditions.RemoveAt(selectedIndex);
			RefreshConditionList();
		}
	}

	private void OnAddAction(object sender, EventArgs e)
	{
		using frmFilterPartEdit frmFilterPartEdit2 = new frmFilterPartEdit(forCondition: false, null, null);
		if (frmFilterPartEdit2.ShowDialog(this) == DialogResult.OK)
		{
			workingRule.Actions.Add(frmFilterPartEdit2.AsAction());
			RefreshActionList();
		}
	}

	private void OnEditAction(object sender, EventArgs e)
	{
		int selectedIndex = lstActions.SelectedIndex;
		if (selectedIndex < 0 || selectedIndex >= workingRule.Actions.Count)
		{
			return;
		}
		using frmFilterPartEdit frmFilterPartEdit2 = new frmFilterPartEdit(forCondition: false, null, workingRule.Actions[selectedIndex]);
		if (frmFilterPartEdit2.ShowDialog(this) == DialogResult.OK)
		{
			workingRule.Actions[selectedIndex] = frmFilterPartEdit2.AsAction();
			RefreshActionList();
		}
	}

	private void OnDeleteAction(object sender, EventArgs e)
	{
		int selectedIndex = lstActions.SelectedIndex;
		if (selectedIndex >= 0 && selectedIndex < workingRule.Actions.Count)
		{
			workingRule.Actions.RemoveAt(selectedIndex);
			RefreshActionList();
		}
	}

	private void AddLabel(string text, int x, int y)
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

	private TextBox AddText(int x, int y, int w)
	{
		TextBox textBox = new TextBox();
		Point location = new Point(x, y);
		textBox.Location = location;
		Size size = new Size(w, 24);
		textBox.Size = size;
		textBox.BackColor = Color.FromArgb(40, 40, 40);
		textBox.ForeColor = Color.Orange;
		textBox.BorderStyle = BorderStyle.FixedSingle;
		Controls.Add(textBox);
		return textBox;
	}

	private FlatButton MakeBtn(string caption, int x, int y, EventHandler handler)
	{
		FlatButton flatButton = new FlatButton();
		flatButton.ButtonText = caption;
		flatButton.Text = caption;
		Size size = new Size(80, 28);
		flatButton.Size = size;
		Point location = new Point(x, y);
		flatButton.Location = location;
		flatButton.Color = LCARScolorStyles.MiscFunction;
		flatButton.Click += handler;
		return flatButton;
	}
}
