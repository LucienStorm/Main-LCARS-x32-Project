using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Forms;
using LCARS;
using LCARS.Controls;

namespace LCARSMail;

public class frmFilters : LCARSForm
{
	private const int MarginX = 12;

	private const int TopChrome = 44;

	private const int BottomBar = 40;

	private FlatButton fbTitle;

	private FlatButton fbImport;

	private FlatButton fbExport;

	private FlatButton fbNew;

	private FlatButton fbEdit;

	private FlatButton fbDelete;

	private FlatButton fbToggle;

	private FlatButton fbRunNow;

	private FlatButton fbClose;

	private ListBox lstRules;

	private Label lblDetail;

	private Label lblStatus;

	private List<MailFilterRule> rules;

	private readonly MailAccount runAccount;

	private readonly string runPassword;

	private readonly string runFolder;

	private readonly ImapMailService runMailService;

	private readonly Action afterRunCallback;

	public frmFilters()
		: this(null, null, null, null, null)
	{
	}

	public frmFilters(MailAccount account, string password, string folderFullName, ImapMailService mailService, Action refreshCallback)
	{
		runAccount = account;
		runPassword = ((password != null) ? password : "");
		runFolder = ((folderFullName != null) ? folderFullName : "");
		runMailService = mailService;
		afterRunCallback = refreshCallback;
		Text = "LCARS MAIL FILTERS";
		BackColor = Color.Black;
		Size clientSize = new Size(720, 480);
		ClientSize = clientSize;
		StartPosition = FormStartPosition.CenterParent;
		rules = MailFilterStore.LoadRules();
		BuildChrome();
		RefreshList();
	}

	private void BuildChrome()
	{
		fbTitle = new FlatButton();
		fbTitle.ButtonText = "FILTERS";
		fbTitle.Text = "FILTERS";
		fbTitle.Clickable = false;
		fbTitle.Color = LCARScolorStyles.StaticTan;
		FlatButton flatButton = fbTitle;
		Size size = new Size(120, 32);
		flatButton.Size = size;
		FlatButton flatButton2 = fbTitle;
		Point location = new Point(12, 8);
		flatButton2.Location = location;
		Controls.Add(fbTitle);
		checked
		{
			int num = fbTitle.Right + 8;
			fbNew = MakeButton("NEW", LCARScolorStyles.PrimaryFunction, 72);
			FlatButton flatButton3 = fbNew;
			location = new Point(num, 10);
			flatButton3.Location = location;
			fbNew.Click += OnNewClick;
			Controls.Add(fbNew);
			num = fbNew.Right + 6;
			fbEdit = MakeButton("EDIT", LCARScolorStyles.MiscFunction, 72);
			FlatButton flatButton4 = fbEdit;
			location = new Point(num, 10);
			flatButton4.Location = location;
			fbEdit.Click += OnEditClick;
			Controls.Add(fbEdit);
			num = fbEdit.Right + 6;
			fbDelete = MakeButton("DELETE", LCARScolorStyles.Orange, 80);
			FlatButton flatButton5 = fbDelete;
			location = new Point(num, 10);
			flatButton5.Location = location;
			fbDelete.Click += OnDeleteClick;
			Controls.Add(fbDelete);
			num = fbDelete.Right + 6;
			fbToggle = MakeButton("TOGGLE", LCARScolorStyles.SystemFunction, 88);
			FlatButton flatButton6 = fbToggle;
			location = new Point(num, 10);
			flatButton6.Location = location;
			fbToggle.Click += OnToggleClick;
			Controls.Add(fbToggle);
			num = fbToggle.Right + 6;
			fbImport = MakeButton("IMPORT", LCARScolorStyles.PrimaryFunction, 88);
			FlatButton flatButton7 = fbImport;
			location = new Point(num, 10);
			flatButton7.Location = location;
			fbImport.Click += OnImportClick;
			Controls.Add(fbImport);
			num = fbImport.Right + 6;
			fbExport = MakeButton("EXPORT", LCARScolorStyles.StaticTan, 88);
			FlatButton flatButton8 = fbExport;
			location = new Point(num, 10);
			flatButton8.Location = location;
			fbExport.Click += OnExportClick;
			Controls.Add(fbExport);
			fbRunNow = MakeButton("RUN NOW", LCARScolorStyles.PrimaryFunction, 100);
			fbRunNow.Click += OnRunNowClick;
			Controls.Add(fbRunNow);
			fbClose = MakeButton("CLOSE", LCARScolorStyles.Orange, 100);
			fbClose.Click += [SpecialName] (object s, EventArgs e) =>
			{
				Close();
			};
			Controls.Add(fbClose);
			lstRules = new ListBox();
			lstRules.BackColor = Color.FromArgb(30, 30, 30);
			lstRules.ForeColor = Color.Orange;
			lstRules.BorderStyle = BorderStyle.FixedSingle;
			lstRules.Font = new Font("Arial Narrow", 11f);
			ListBox listBox = lstRules;
			location = new Point(12, 44);
			listBox.Location = location;
			ListBox listBox2 = lstRules;
			size = new Size(696, 280);
			listBox2.Size = size;
			lstRules.SelectedIndexChanged += OnRuleSelected;
			Controls.Add(lstRules);
			lblDetail = new Label();
			lblDetail.ForeColor = Color.Orange;
			lblDetail.BackColor = Color.Black;
			lblDetail.Font = new Font("Arial Narrow", 10f);
			Label label = lblDetail;
			location = new Point(12, 332);
			label.Location = location;
			Label label2 = lblDetail;
			size = new Size(696, 120);
			label2.Size = size;
			Controls.Add(lblDetail);
			lblStatus = new Label();
			lblStatus.ForeColor = Color.Orange;
			lblStatus.BackColor = Color.Black;
			lblStatus.Font = new Font("Arial Narrow", 10f);
			lblStatus.Text = rules.Count + " RULE(S) LOADED";
			lblStatus.AutoSize = false;
			lblStatus.TextAlign = ContentAlignment.MiddleLeft;
			Controls.Add(lblStatus);
			Resize += OnFormResize;
			LayoutChrome();
		}
	}

	private void OnFormResize(object sender, EventArgs e)
	{
		LayoutChrome();
	}

	private void LayoutChrome()
	{
		int num = Math.Max(400, ClientSize.Width);
		int num2 = Math.Max(320, ClientSize.Height);
		FlatButton flatButton = fbClose;
		checked
		{
			Point location = new Point(num - 12 - fbClose.Width, 10);
			flatButton.Location = location;
			FlatButton flatButton2 = fbRunNow;
			location = new Point(fbClose.Left - 8 - fbRunNow.Width, 10);
			flatButton2.Location = location;
			lstRules.Width = num - 24;
			lstRules.Height = Math.Max(120, num2 - 44 - 40 - 140);
			Label label = lblDetail;
			location = new Point(12, lstRules.Bottom + 8);
			label.Location = location;
			lblDetail.Width = lstRules.Width;
			lblDetail.Height = Math.Max(60, num2 - lblDetail.Top - 40 - 8);
			lblStatus.SetBounds(12, num2 - 40, lstRules.Width, 32);
		}
	}

	private FlatButton MakeButton(string caption, LCARScolorStyles color, int width)
	{
		FlatButton flatButton = new FlatButton();
		flatButton.ButtonText = caption;
		flatButton.Text = caption;
		flatButton.Color = color;
		Size size = new Size(width, 28);
		flatButton.Size = size;
		return flatButton;
	}

	private void PersistRules()
	{
		if (!MailFilterStore.SaveRules(rules))
		{
			lblStatus.Text = ((MailFilterStore.LastError != null) ? MailFilterStore.LastError : "SAVE FAILED").ToUpperInvariant();
		}
	}

	private void RefreshList()
	{
		lstRules.Items.Clear();
		if (rules == null)
		{
			rules = new List<MailFilterRule>();
		}
		foreach (MailFilterRule rule in rules)
		{
			if (rule != null)
			{
				string text = (rule.Enabled ? "" : "[OFF] ");
				ListBox.ObjectCollection items = lstRules.Items;
				MailFilterRule mailFilterRule = rule;
				items.Add(text + ((mailFilterRule.Name != null) ? mailFilterRule.Name : "(unnamed)"));
			}
		}
		lblStatus.Text = rules.Count + " RULE(S) LOADED";
		lblDetail.Text = "SELECT A RULE";
	}

	private int SelectedRuleIndex()
	{
		return lstRules.SelectedIndex;
	}

	private void OnRuleSelected(object sender, EventArgs e)
	{
		int num = SelectedRuleIndex();
		if (num < 0 || rules == null || num >= rules.Count)
		{
			return;
		}
		MailFilterRule mailFilterRule = rules[num];
		if (mailFilterRule == null)
		{
			return;
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("ENABLED: " + mailFilterRule.Enabled.ToString().ToUpperInvariant());
		MailFilterRule mailFilterRule2 = mailFilterRule;
		stringBuilder.AppendLine("LOGIC: " + ((mailFilterRule2.ConditionLogic != null) ? mailFilterRule2.ConditionLogic : "AND"));
		stringBuilder.AppendLine("CONDITIONS:");
		foreach (MailFilterCondition condition in mailFilterRule.Conditions)
		{
			string[] array = new string[6] { "  • ", null, null, null, null, null };
			MailFilterCondition mailFilterCondition = condition;
			array[1] = ((mailFilterCondition.Field != null) ? mailFilterCondition.Field : "");
			array[2] = " ";
			MailFilterCondition mailFilterCondition2 = condition;
			array[3] = ((mailFilterCondition2.Operator != null) ? mailFilterCondition2.Operator : "");
			array[4] = " ";
			MailFilterCondition mailFilterCondition3 = condition;
			array[5] = ((mailFilterCondition3.Value != null) ? mailFilterCondition3.Value : "");
			stringBuilder.AppendLine(string.Concat(array));
		}
		stringBuilder.AppendLine("ACTIONS:");
		foreach (MailFilterAction action in mailFilterRule.Actions)
		{
			MailFilterAction mailFilterAction = action;
			string obj = ((mailFilterAction.ActionName != null) ? mailFilterAction.ActionName : "");
			MailFilterAction mailFilterAction2 = action;
			stringBuilder.AppendLine("  • " + obj + " " + ((mailFilterAction2.ActionValue != null) ? mailFilterAction2.ActionValue : ""));
		}
		lblDetail.Text = stringBuilder.ToString();
	}

	private void OnNewClick(object sender, EventArgs e)
	{
		using frmFilterEdit frmFilterEdit2 = new frmFilterEdit(new MailFilterRule(), createNew: true);
		if (frmFilterEdit2.ShowDialog(this) == DialogResult.OK)
		{
			rules.Add(frmFilterEdit2.EditedRule);
			PersistRules();
			RefreshList();
		}
	}

	private void OnEditClick(object sender, EventArgs e)
	{
		int num = SelectedRuleIndex();
		if (num < 0)
		{
			return;
		}
		using frmFilterEdit frmFilterEdit2 = new frmFilterEdit(rules[num], createNew: false);
		if (frmFilterEdit2.ShowDialog(this) == DialogResult.OK)
		{
			rules[num] = frmFilterEdit2.EditedRule;
			PersistRules();
			RefreshList();
		}
	}

	private void OnDeleteClick(object sender, EventArgs e)
	{
		int num = SelectedRuleIndex();
		if (num >= 0)
		{
			rules.RemoveAt(num);
			PersistRules();
			RefreshList();
		}
	}

	private void OnToggleClick(object sender, EventArgs e)
	{
		int num = SelectedRuleIndex();
		if (num >= 0)
		{
			rules[num].Enabled = !rules[num].Enabled;
			PersistRules();
			RefreshList();
			lstRules.SelectedIndex = num;
		}
	}

	private void OnImportClick(object sender, EventArgs e)
	{
		string text = LcarsCommsUi.PickFolder(this, "THUNDERBIRD PROFILE (MSGFILTERRULES.DAT)");
		if (!string.IsNullOrEmpty(text))
		{
			int num = MailFilterStore.TryImportFromProfile(text);
			if (num < 0)
			{
				string prompt = ((MailFilterStore.LastError != null) ? MailFilterStore.LastError : "IMPORT FAILED").ToUpperInvariant();
				lblStatus.Text = prompt;
				LcarsCommsUi.Alert(prompt, "LCARS COMMS — FILTER IMPORT");
			}
			else
			{
				rules = MailFilterStore.LoadRules();
				RefreshList();
				lblStatus.Text = "IMPORTED " + num + " RULE(S)";
			}
		}
	}

	private void OnExportClick(object sender, EventArgs e)
	{
		using SaveFileDialog saveFileDialog = new SaveFileDialog();
		saveFileDialog.Filter = "Thunderbird filters|msgFilterRules.dat|All|*.*";
		saveFileDialog.FileName = "msgFilterRules.dat";
		if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
		{
			if (MailFilterThunderbirdExporter.ExportToFile(rules, saveFileDialog.FileName))
			{
				lblStatus.Text = "EXPORTED";
			}
			else
			{
				lblStatus.Text = "EXPORT FAILED";
			}
		}
	}

	private void OnRunNowClick(object sender, EventArgs e)
	{
		if (runAccount == null || runMailService == null || string.IsNullOrWhiteSpace(runFolder))
		{
			lblStatus.Text = "SELECT A FOLDER IN MAIL FIRST";
			return;
		}
		List<MailMessageHeader> headers = runMailService.SyncHeaders(runAccount, runPassword, runFolder);
		MailFilterRunSummary mailFilterRunSummary = MailFilterEngine.ApplyRulesToFolder(runAccount, runPassword, runFolder, headers, runMailService, rules);
		lblStatus.Text = mailFilterRunSummary.ActionsApplied + " ACTION(S) — " + mailFilterRunSummary.MessagesMatched + " MATCH(ES)";
		if (afterRunCallback != null)
		{
			afterRunCallback();
		}
	}
}
