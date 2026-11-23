using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

public sealed class FolderFavoriteStore
{
	public static string LastError;

	private FolderFavoriteStore()
	{
	}

	private static string StorePath()
	{
		string text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LCARS x32", "Comms");
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		return Path.Combine(text, "folder-favorites.txt");
	}

	public static List<string> LoadFavorites()
	{
		LastError = null;
		List<string> list = new List<string>();
		string path = StorePath();
		if (!File.Exists(path))
		{
			return list;
		}
		try
		{
			string[] array = File.ReadAllLines(path, Encoding.UTF8);
			foreach (string text in array)
			{
				string text2 = ((text != null) ? text : "").Trim();
				if (text2.Length == 0 || !Directory.Exists(text2))
				{
					continue;
				}
				bool flag = false;
				foreach (string item in list)
				{
					if (string.Equals(item, text2, StringComparison.OrdinalIgnoreCase))
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					list.Add(text2);
				}
			}
		}
		catch (Exception ex)
		{
			ProjectData.SetProjectError(ex);
			Exception ex2 = ex;
			LastError = ex2.Message;
			ProjectData.ClearProjectError();
		}
		return list;
	}

	public static bool IsFavorite(string folderPath)
	{
		string text = Normalize(folderPath);
		if (text.Length == 0)
		{
			return false;
		}
		foreach (string item in LoadFavorites())
		{
			if (string.Equals(item, text, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	public static bool AddFavorite(string folderPath)
	{
		LastError = null;
		string text = Normalize(folderPath);
		if (text.Length == 0 || !Directory.Exists(text))
		{
			LastError = "Folder does not exist.";
			return false;
		}
		List<string> list = LoadFavorites();
		foreach (string item in list)
		{
			if (string.Equals(item, text, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		list.Add(text);
		return SaveFavorites(list);
	}

	public static bool RemoveFavorite(string folderPath)
	{
		LastError = null;
		string text = Normalize(folderPath);
		if (text.Length == 0)
		{
			return false;
		}
		List<string> list = LoadFavorites();
		List<string> list2 = new List<string>();
		bool flag = false;
		foreach (string item in list)
		{
			if (string.Equals(item, text, StringComparison.OrdinalIgnoreCase))
			{
				flag = true;
			}
			else
			{
				list2.Add(item);
			}
		}
		if (!flag)
		{
			return true;
		}
		return SaveFavorites(list2);
	}

	private static bool SaveFavorites(List<string> paths)
	{
		bool result;
		try
		{
			File.WriteAllLines(StorePath(), paths.ToArray(), Encoding.UTF8);
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
		return result;
	}

	private static string Normalize(string folderPath)
	{
		string text = ((folderPath != null) ? folderPath : "").Trim();
		string result;
		if (text.Length == 0)
		{
			result = "";
		}
		else
		{
			try
			{
				result = Path.GetFullPath(text.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = text;
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}
}
