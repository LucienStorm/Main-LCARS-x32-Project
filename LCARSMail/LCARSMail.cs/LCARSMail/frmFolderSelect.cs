using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using LCARS;
using LCARS.Controls;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

public class frmFolderSelect : LCARSForm
{
	private const int RowH = 36;

	private const int RowGap = 4;

	private const string PlacesHub = "";

	private string currentPath;

	private readonly string promptTitle;

	private Label lblPath;

	private LcarsScrollPanel folderHost;

	private FlatButton fbUp;

	private FlatButton fbPin;

	private FlatButton fbSelect;

	private FlatButton fbCancel;

	private FlatButton fbTitle;

	private FlatButton fbPlaces;

	public string SelectedPath
	{
		[DebuggerNonUserCode]
		get;
		[DebuggerNonUserCode]
		set;
	}

	public frmFolderSelect(string title = "SELECT FOLDER", string startPath = null)
	{
		currentPath = "";
		promptTitle = (string.IsNullOrWhiteSpace(title) ? "SELECT FOLDER" : title.Trim().ToUpperInvariant());
		Text = "LCARS COMMS — FOLDER";
		BackColor = Color.Black;
		ForeColor = Color.Orange;
		FormBorderStyle = FormBorderStyle.None;
		ControlBox = false;
		DoubleBuffered = true;
		BindToWorkingArea = false;
		StartPosition = FormStartPosition.CenterParent;
		Size clientSize = new Size(680, 500);
		ClientSize = clientSize;
		ShowInTaskbar = false;
		fbTitle = new FlatButton();
		fbTitle.ButtonText = promptTitle;
		fbTitle.Text = promptTitle;
		fbTitle.Clickable = false;
		fbTitle.Color = LCARScolorStyles.StaticTan;
		fbTitle.ButtonTextAlign = ContentAlignment.BottomRight;
		FlatButton flatButton = fbTitle;
		clientSize = new Size(320, 32);
		flatButton.Size = clientSize;
		FlatButton flatButton2 = fbTitle;
		Point location = new Point(12, 8);
		flatButton2.Location = location;
		Controls.Add(fbTitle);
		lblPath = new Label();
		lblPath.AutoSize = false;
		lblPath.ForeColor = Color.Orange;
		lblPath.BackColor = Color.Black;
		lblPath.Font = new Font("Arial Narrow", 11f, FontStyle.Bold);
		Label label = lblPath;
		location = new Point(12, 46);
		label.Location = location;
		Label label2 = lblPath;
		clientSize = new Size(656, 24);
		label2.Size = clientSize;
		Controls.Add(lblPath);
		folderHost = new LcarsScrollPanel();
		LcarsScrollPanel lcarsScrollPanel = folderHost;
		location = new Point(12, 74);
		lcarsScrollPanel.Location = location;
		LcarsScrollPanel lcarsScrollPanel2 = folderHost;
		clientSize = new Size(656, 360);
		lcarsScrollPanel2.Size = clientSize;
		folderHost.BackColor = Color.Black;
		folderHost.CollapseTrackWhenIdle = true;
		Controls.Add(folderHost);
		location = new Point(12, 450);
		fbPlaces = MakeBtn("PLACES", LCARScolorStyles.SystemFunction, location);
		fbPlaces.Click += OnPlacesClick;
		Controls.Add(fbPlaces);
		location = new Point(124, 450);
		fbUp = MakeBtn("UP", LCARScolorStyles.NavigationFunction, location);
		fbUp.Click += OnUpClick;
		Controls.Add(fbUp);
		location = new Point(236, 450);
		fbPin = MakeBtn("PIN", LCARScolorStyles.MiscFunction, location);
		fbPin.Click += OnPinClick;
		Controls.Add(fbPin);
		location = new Point(348, 450);
		fbSelect = MakeBtn("SELECT", LCARScolorStyles.PrimaryFunction, location);
		fbSelect.Click += OnSelectClick;
		Controls.Add(fbSelect);
		location = new Point(568, 450);
		fbCancel = MakeBtn("CANCEL", LCARScolorStyles.Orange, location);
		fbCancel.Click += OnCancelClick;
		Controls.Add(fbCancel);
		string text = ((startPath != null) ? startPath : "").Trim();
		if (text.Length > 0 && Directory.Exists(text))
		{
			currentPath = text;
		}
		else
		{
			currentPath = "";
		}
		LoadCurrent();
	}

	public static string Pick(IWin32Window owner, string title = "SELECT FOLDER", string startPath = null)
	{
		using (frmFolderSelect frmFolderSelect2 = new frmFolderSelect(title, startPath))
		{
			if (frmFolderSelect2.ShowDialog(owner) == DialogResult.OK)
			{
				return frmFolderSelect2.SelectedPath;
			}
		}
		return null;
	}

	private static FlatButton MakeBtn(string caption, LCARScolorStyles color, Point loc)
	{
		FlatButton flatButton = new FlatButton();
		flatButton.ButtonText = caption;
		flatButton.Text = caption;
		flatButton.Color = color;
		flatButton.ButtonTextAlign = ContentAlignment.BottomRight;
		Size size = new Size(100, 28);
		flatButton.Size = size;
		flatButton.Location = loc;
		return flatButton;
	}

	private void LoadCurrent()
	{
		folderHost.ContentPanel.Controls.Clear();
		bool flag = string.IsNullOrEmpty(currentPath);
		lblPath.Text = (flag ? "PLACES — FAVORITES · USER FOLDERS · DRIVES" : currentPath.ToUpperInvariant());
		fbUp.Clickable = !flag;
		fbPlaces.Clickable = !flag;
		fbSelect.Clickable = ((!flag && Directory.Exists(currentPath)) ? true : false);
		UpdatePinButton();
		int num = 4;
		bool flip = false;
		int num2 = Math.Max(200, checked(folderHost.ContentPanel.Width - 8));
		if (flag)
		{
			num = LoadPlacesHub(num, num2, ref flip);
		}
		else
		{
			num = LoadDirectoryListing(num, num2, ref flip);
		}
		folderHost.Relayout();
	}

	private int LoadPlacesHub(int y, int width, ref bool flip)
	{
		List<string> list = FolderFavoriteStore.LoadFavorites();
		checked
		{
			if (list.Count > 0)
			{
				y = AddSectionLabel("FAVORITES", y);
				foreach (string item in list)
				{
					string caption = FavoriteCaption(item);
					AddFolderRow(item, caption, enabled: true, y, width, flip, LCARScolorStyles.PrimaryFunction);
					y += 40;
					flip = !flip;
				}
				y += 8;
			}
			y = AddSectionLabel("USER FOLDERS", y);
			foreach (KeyValuePair<string, string> item2 in BuiltInUserPlaces())
			{
				if (Directory.Exists(item2.Value))
				{
					AddFolderRow(item2.Value, item2.Key, enabled: true, y, width, flip, LCARScolorStyles.NavigationFunction);
					y += 40;
					flip = !flip;
				}
			}
			y += 8;
			y = AddSectionLabel("DRIVES", y);
			DriveInfo[] drives = DriveInfo.GetDrives();
			foreach (DriveInfo driveInfo in drives)
			{
				bool flag = false;
				try
				{
					flag = driveInfo.IsReady;
				}
				catch (Exception projectError)
				{
					ProjectData.SetProjectError(projectError);
					ProjectData.ClearProjectError();
				}
				string caption2;
				if (flag)
				{
					string text = "";
					try
					{
						text = driveInfo.VolumeLabel;
					}
					catch (Exception projectError2)
					{
						ProjectData.SetProjectError(projectError2);
						ProjectData.ClearProjectError();
					}
					caption2 = ((!string.IsNullOrEmpty(text)) ? (text.ToUpperInvariant() + " (" + driveInfo.Name + ")") : ("LOCAL DISK (" + driveInfo.Name + ")"));
				}
				else
				{
					caption2 = "DRIVE OFFLINE (" + driveInfo.Name + ")";
				}
				AddFolderRow(driveInfo.RootDirectory.FullName, caption2, flag, y, width, flip, LCARScolorStyles.MiscFunction);
				y += 40;
				flip = !flip;
			}
			return y;
		}
	}

	private int LoadDirectoryListing(int y, int width, ref bool flip)
	{
		List<DirectoryInfo> list = new List<DirectoryInfo>();
		int result;
		try
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(currentPath);
			DirectoryInfo[] directories = directoryInfo.GetDirectories();
			foreach (DirectoryInfo directoryInfo2 in directories)
			{
				try
				{
					if ((directoryInfo2.Attributes & FileAttributes.Hidden) == 0 && (directoryInfo2.Attributes & FileAttributes.System) == 0)
					{
						list.Add(directoryInfo2);
					}
				}
				catch (Exception projectError)
				{
					ProjectData.SetProjectError(projectError);
					ProjectData.ClearProjectError();
				}
			}
		}
		catch (Exception ex)
		{
			ProjectData.SetProjectError(ex);
			Exception ex2 = ex;
			LcarsCommsUi.Alert("Cannot open folder:\r\n" + currentPath + "\r\n\r\n" + ex2.Message, "LCARS COMMS — FOLDER ERROR");
			currentPath = "";
			LoadCurrent();
			result = y;
			ProjectData.ClearProjectError();
			goto IL_01b3;
		}
		list.Sort([SpecialName] (DirectoryInfo a, DirectoryInfo b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
		LCARScolorStyles? forcedColor = default(LCARScolorStyles?);
		foreach (DirectoryInfo item in list)
		{
			AddFolderRow(item.FullName, item.Name.ToUpperInvariant(), enabled: true, y, width, flip, forcedColor);
			y = checked(y + 40);
			flip = !flip;
		}
		if (list.Count == 0)
		{
			Label label = new Label();
			label.Text = "NO SUBFOLDERS — TAP SELECT TO USE THIS FOLDER";
			label.ForeColor = Color.FromArgb(200, 140, 60);
			label.BackColor = Color.Black;
			label.AutoSize = true;
			Point location = new Point(8, 12);
			label.Location = location;
			folderHost.ContentPanel.Controls.Add(label);
		}
		result = y;
		goto IL_01b3;
		IL_01b3:
		return result;
	}

	private int AddSectionLabel(string text, int y)
	{
		Label label = new Label();
		label.Text = text;
		label.ForeColor = Color.FromArgb(255, 204, 102);
		label.BackColor = Color.Black;
		label.Font = new Font("Arial Narrow", 10f, FontStyle.Bold);
		label.AutoSize = true;
		Point location = new Point(8, y);
		label.Location = location;
		folderHost.ContentPanel.Controls.Add(label);
		return checked(y + 22);
	}

	private static List<KeyValuePair<string, string>> BuiltInUserPlaces()
	{
		List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();
		AddPlace(list, "DESKTOP", Environment.SpecialFolder.DesktopDirectory);
		AddPlace(list, "DOCUMENTS", Environment.SpecialFolder.Personal);
		AddPlace(list, "DOWNLOADS", GetDownloadsPath());
		AddPlace(list, "PICTURES", Environment.SpecialFolder.MyPictures);
		AddPlace(list, "MUSIC", Environment.SpecialFolder.MyMusic);
		AddPlace(list, "VIDEOS", Environment.SpecialFolder.MyVideos);
		AddPlace(list, "USER PROFILE", Environment.SpecialFolder.UserProfile);
		return list;
	}

	private static void AddPlace(List<KeyValuePair<string, string>> list, string caption, Environment.SpecialFolder folder)
	{
		try
		{
			string folderPath = Environment.GetFolderPath(folder);
			if (!string.IsNullOrEmpty(folderPath))
			{
				KeyValuePair<string, string> item = new KeyValuePair<string, string>(caption, folderPath);
				list.Add(item);
			}
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
	}

	private static void AddPlace(List<KeyValuePair<string, string>> list, string caption, string path)
	{
		if (!string.IsNullOrEmpty(path))
		{
			KeyValuePair<string, string> item = new KeyValuePair<string, string>(caption, path);
			list.Add(item);
		}
	}

	private static string GetDownloadsPath()
	{
		try
		{
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			string text = Path.Combine(folderPath, "Downloads");
			if (Directory.Exists(text))
			{
				return text;
			}
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
		return null;
	}

	private static string FavoriteCaption(string folderPath)
	{
		string result;
		try
		{
			string path = folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			string text = Path.GetFileName(path);
			if (string.IsNullOrEmpty(text))
			{
				text = folderPath;
			}
			result = "* " + text.ToUpperInvariant();
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			result = "* " + folderPath.ToUpperInvariant();
			ProjectData.ClearProjectError();
		}
		return result;
	}

	private void AddFolderRow(string fullPath, string caption, bool enabled, int y, int width, bool flip, LCARScolorStyles? forcedColor)
	{
		FlatButton flatButton = new FlatButton();
		flatButton.ButtonText = caption;
		flatButton.Text = caption;
		flatButton.Clickable = enabled;
		if (!enabled)
		{
			flatButton.Color = LCARScolorStyles.FunctionUnavailable;
		}
		else if (forcedColor.HasValue)
		{
			flatButton.Color = forcedColor.Value;
		}
		else
		{
			flatButton.Color = (flip ? LCARScolorStyles.NavigationFunction : LCARScolorStyles.MiscFunction);
		}
		flatButton.ButtonTextAlign = ContentAlignment.MiddleLeft;
		flatButton.ButtonTextHeight = 14;
		Size size = new Size(width, 36);
		flatButton.Size = size;
		Point location = new Point(4, y);
		flatButton.Location = location;
		flatButton.Tag = fullPath;
		if (enabled)
		{
			flatButton.Click += OnFolderClick;
		}
		folderHost.ContentPanel.Controls.Add(flatButton);
	}

	private void UpdatePinButton()
	{
		if (string.IsNullOrEmpty(currentPath) || !Directory.Exists(currentPath))
		{
			fbPin.Clickable = false;
			fbPin.ButtonText = "PIN";
			fbPin.Text = "PIN";
			return;
		}
		fbPin.Clickable = true;
		if (FolderFavoriteStore.IsFavorite(currentPath))
		{
			fbPin.ButtonText = "UNPIN";
			fbPin.Text = "UNPIN";
			fbPin.Color = LCARScolorStyles.CriticalFunction;
		}
		else
		{
			fbPin.ButtonText = "PIN";
			fbPin.Text = "PIN";
			fbPin.Color = LCARScolorStyles.MiscFunction;
		}
	}

	private void OnFolderClick(object sender, EventArgs e)
	{
		if (sender is FlatButton flatButton)
		{
			string text = flatButton.Tag as string;
			if (!string.IsNullOrEmpty(text) && Directory.Exists(text))
			{
				currentPath = text;
				LoadCurrent();
			}
		}
	}

	private void OnPlacesClick(object sender, EventArgs e)
	{
		currentPath = "";
		LoadCurrent();
	}

	private void OnUpClick(object sender, EventArgs e)
	{
		if (string.IsNullOrEmpty(currentPath))
		{
			return;
		}
		try
		{
			string text = currentPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			string directoryName = Path.GetDirectoryName(text);
			string pathRoot = Path.GetPathRoot(text);
			if (string.IsNullOrEmpty(directoryName) || string.Equals(directoryName, text, StringComparison.OrdinalIgnoreCase) || string.Equals(text, pathRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
			{
				currentPath = "";
			}
			else
			{
				currentPath = directoryName;
			}
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			currentPath = "";
			ProjectData.ClearProjectError();
		}
		LoadCurrent();
	}

	private void OnPinClick(object sender, EventArgs e)
	{
		if (string.IsNullOrEmpty(currentPath) || !Directory.Exists(currentPath))
		{
			return;
		}
		if (FolderFavoriteStore.IsFavorite(currentPath))
		{
			if (!FolderFavoriteStore.RemoveFavorite(currentPath))
			{
				LcarsCommsUi.Alert((FolderFavoriteStore.LastError != null) ? FolderFavoriteStore.LastError : "UNPIN FAILED", "LCARS COMMS — FOLDER");
				return;
			}
		}
		else if (!FolderFavoriteStore.AddFavorite(currentPath))
		{
			LcarsCommsUi.Alert((FolderFavoriteStore.LastError != null) ? FolderFavoriteStore.LastError : "PIN FAILED", "LCARS COMMS — FOLDER");
			return;
		}
		UpdatePinButton();
	}

	private void OnSelectClick(object sender, EventArgs e)
	{
		if (string.IsNullOrEmpty(currentPath) || !Directory.Exists(currentPath))
		{
			LcarsCommsUi.Alert("Choose a folder from Places (or open a drive), then tap SELECT.", "LCARS COMMS — FOLDER");
			return;
		}
		SelectedPath = currentPath;
		DialogResult = DialogResult.OK;
		Close();
	}

	private void OnCancelClick(object sender, EventArgs e)
	{
		SelectedPath = null;
		DialogResult = DialogResult.Cancel;
		Close();
	}
}
