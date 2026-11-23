using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using LCARS;
using LCARS.Controls;

namespace LCARSMail;

public class frmGlodaSavedSearchPicker : LCARSForm
{
	private class SavedRow
	{
		public readonly string Query;

		private readonly string caption;

		public SavedRow(string query, string display)
		{
			Query = query;
			caption = display;
		}

		public override string ToString()
		{
			return caption;
		}
	}

	private ListBox lstSaved;

	private Label lblHint;

	public string SelectedQuery
	{
		[DebuggerNonUserCode]
		get;
		[DebuggerNonUserCode]
		set;
	}

	public frmGlodaSavedSearchPicker()
	{
		string selectedQuery = "";
		SelectedQuery = selectedQuery;
		BackColor = Color.Black;
		Size clientSize = new Size(520, 360);
		ClientSize = clientSize;
		StartPosition = FormStartPosition.CenterParent;
		BuildUi();
		LoadSaved();
	}

	private void BuildUi()
	{
		lblHint = new Label();
		lblHint.ForeColor = Color.Orange;
		Label label = lblHint;
		Point location = new Point(12, 12);
		label.Location = location;
		Label label2 = lblHint;
		Size size = new Size(496, 24);
		label2.Size = size;
		lblHint.Text = "SAVED GLODA SEARCHES";
		Controls.Add(lblHint);
		lstSaved = new ListBox();
		ListBox listBox = lstSaved;
		location = new Point(12, 40);
		listBox.Location = location;
		ListBox listBox2 = lstSaved;
		size = new Size(496, 260);
		listBox2.Size = size;
		lstSaved.BackColor = Color.FromArgb(30, 30, 30);
		lstSaved.ForeColor = Color.Orange;
		lstSaved.DoubleClick += OnAccept;
		Controls.Add(lstSaved);
		FlatButton flatButton = new FlatButton();
		flatButton.ButtonText = "RUN";
		flatButton.Text = "RUN";
		flatButton.Color = LCARScolorStyles.PrimaryFunction;
		size = new Size(80, 28);
		flatButton.Size = size;
		location = new Point(12, 312);
		flatButton.Location = location;
		flatButton.Click += OnAccept;
		Controls.Add(flatButton);
		FlatButton flatButton2 = new FlatButton();
		flatButton2.ButtonText = "CANCEL";
		flatButton2.Text = "CANCEL";
		flatButton2.Color = LCARScolorStyles.Orange;
		size = new Size(100, 28);
		flatButton2.Size = size;
		location = new Point(100, 312);
		flatButton2.Location = location;
		flatButton2.Click += [SpecialName] (object s, EventArgs e) =>
		{
			DialogResult = DialogResult.Cancel;
		};
		Controls.Add(flatButton2);
	}

	private void LoadSaved()
	{
		lstSaved.Items.Clear();
		List<KeyValuePair<string, string>> list = MailGlodaSavedSearchStore.TryListSaved();
		if (list == null || list.Count == 0)
		{
			lstSaved.Items.Add("(NO SAVED SEARCHES)");
			return;
		}
		foreach (KeyValuePair<string, string> item in list)
		{
			string text = item.Key + " — " + item.Value;
			if (text.Length > 120)
			{
				text = text.Substring(0, 117) + "…";
			}
			lstSaved.Items.Add(new SavedRow(item.Value, text));
		}
		lstSaved.SelectedIndex = 0;
	}

	private void OnAccept(object sender, EventArgs e)
	{
		if (lstSaved.SelectedItem != null && lstSaved.SelectedItem is SavedRow savedRow && !string.IsNullOrWhiteSpace(savedRow.Query))
		{
			SelectedQuery = savedRow.Query;
			DialogResult = DialogResult.OK;
			Close();
		}
	}
}
