using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Web.Script.Serialization;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

public sealed class MatrixChatService
{
	public class MatrixRoomSummary
	{
		public string RoomId;

		public string Name;

		[DebuggerNonUserCode]
		public MatrixRoomSummary()
		{
		}

		public override string ToString()
		{
			if (!string.IsNullOrWhiteSpace(Name))
			{
				return Name;
			}
			return (RoomId != null) ? RoomId : "";
		}
	}

	public static string LastError;

	private MatrixChatService()
	{
	}

	public static bool TrySendText(string homeserver, string accessToken, string roomId, string message)
	{
		LastError = null;
		string text = NormalizeHomeserver(homeserver);
		string text2 = ((roomId != null) ? roomId : "").Trim();
		string text3 = ((accessToken != null) ? accessToken : "").Trim();
		bool result;
		if (text.Length == 0 || text2.Length == 0 || text3.Length == 0)
		{
			LastError = "Homeserver, room id, and access token are required.";
			result = false;
		}
		else
		{
			string text4 = Guid.NewGuid().ToString("N");
			string requestUriString = text + "/_matrix/client/v3/rooms/" + Uri.EscapeDataString(text2) + "/send/m.room.message/" + text4;
			string s = new JavaScriptSerializer().Serialize(new Dictionary<string, object>
			{
				{ "msgtype", "m.text" },
				{
					"body",
					(message != null) ? message : ""
				}
			});
			try
			{
				HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(requestUriString);
				httpWebRequest.Method = "PUT";
				httpWebRequest.ContentType = "application/json";
				httpWebRequest.Headers.Add("Authorization", "Bearer " + text3);
				byte[] bytes = Encoding.UTF8.GetBytes(s);
				httpWebRequest.ContentLength = bytes.Length;
				using (Stream stream = httpWebRequest.GetRequestStream())
				{
					stream.Write(bytes, 0, bytes.Length);
				}
				using HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
				result = ((httpWebResponse.StatusCode >= HttpStatusCode.OK && httpWebResponse.StatusCode < HttpStatusCode.MultipleChoices) ? true : false);
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Exception ex2 = ex;
				LastError = ex2.Message;
				result = false;
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}

	public static bool TryPasswordLogin(string homeserver, string username, string password, ref string accessToken, ref string userId)
	{
		LastError = null;
		accessToken = null;
		userId = null;
		string text = NormalizeHomeserver(homeserver);
		bool result;
		if (text.Length == 0 || string.IsNullOrWhiteSpace(username))
		{
			LastError = "Homeserver and username are required.";
			result = false;
		}
		else
		{
			string url = text + "/_matrix/client/v3/login";
			string jsonBody = new JavaScriptSerializer().Serialize(new Dictionary<string, object>
			{
				{ "type", "m.login.password" },
				{
					"identifier",
					new Dictionary<string, object>
					{
						{ "type", "m.id.user" },
						{
							"user",
							username.Trim()
						}
					}
				},
				{
					"password",
					(password != null) ? password : ""
				}
			});
			try
			{
				string text2 = PostJson(url, jsonBody, null);
				if (text2.Length == 0)
				{
					result = false;
				}
				else
				{
					JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
					if (!(javaScriptSerializer.DeserializeObject(text2) is Dictionary<string, object> dictionary))
					{
						LastError = "Login response invalid.";
						result = false;
					}
					else
					{
						if (dictionary.ContainsKey("access_token"))
						{
							accessToken = Conversions.ToString(dictionary["access_token"]);
						}
						if (dictionary.ContainsKey("user_id"))
						{
							userId = Conversions.ToString(dictionary["user_id"]);
						}
						if (string.IsNullOrWhiteSpace(accessToken))
						{
							LastError = "No access token in login response.";
							result = false;
						}
						else
						{
							result = true;
						}
					}
				}
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Exception ex2 = ex;
				LastError = ex2.Message;
				result = false;
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}

	public static string TryFetchRoomDisplayName(string homeserver, string accessToken, string roomId)
	{
		LastError = null;
		string text = NormalizeHomeserver(homeserver);
		string text2 = ((roomId != null) ? roomId : "").Trim();
		if (text.Length == 0 || text2.Length == 0)
		{
			return "";
		}
		string url = text + "/_matrix/client/v3/rooms/" + Uri.EscapeDataString(text2) + "/state/m.room.name";
		try
		{
			string json = GetJson(url, accessToken);
			if (json.Length == 0)
			{
				return text2;
			}
			JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
			if (javaScriptSerializer.DeserializeObject(json) is Dictionary<string, object> dictionary && dictionary.ContainsKey("name"))
			{
				string text3 = Conversions.ToString(dictionary["name"]);
				if (!string.IsNullOrWhiteSpace(text3))
				{
					return text3;
				}
			}
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
		return text2;
	}

	public static List<MatrixRoomSummary> TryListJoinedRooms(string homeserver, string accessToken)
	{
		LastError = null;
		string text = NormalizeHomeserver(homeserver);
		string text2 = ((accessToken != null) ? accessToken : "").Trim();
		List<MatrixRoomSummary> result;
		if (text.Length == 0 || text2.Length == 0)
		{
			LastError = "Homeserver and token required.";
			result = null;
		}
		else
		{
			string url = text + "/_matrix/client/v3/joined_rooms";
			try
			{
				string json = GetJson(url, text2);
				if (json.Length == 0)
				{
					result = new List<MatrixRoomSummary>();
				}
				else
				{
					JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
					Dictionary<string, object> dictionary = javaScriptSerializer.DeserializeObject(json) as Dictionary<string, object>;
					List<MatrixRoomSummary> list = new List<MatrixRoomSummary>();
					if (dictionary == null || !dictionary.ContainsKey("joined_rooms"))
					{
						result = list;
					}
					else if (!(dictionary["joined_rooms"] is object[] array))
					{
						result = list;
					}
					else
					{
						object[] array2 = array;
						for (int i = 0; i < array2.Length; i = checked(i + 1))
						{
							object objectValue = RuntimeHelpers.GetObjectValue(array2[i]);
							string text3 = Conversions.ToString(objectValue);
							if (!string.IsNullOrWhiteSpace(text3))
							{
								string name = TryFetchRoomDisplayName(text, text2, text3);
								list.Add(new MatrixRoomSummary
								{
									RoomId = text3,
									Name = name
								});
							}
						}
						result = list;
					}
				}
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Exception ex2 = ex;
				LastError = ex2.Message;
				result = null;
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}

	public static string TryFetchRecentMessages(string homeserver, string accessToken, string roomId)
	{
		LastError = null;
		string text = NormalizeHomeserver(homeserver);
		string requestUriString = text + "/_matrix/client/v3/rooms/" + Uri.EscapeDataString(roomId) + "/messages?dir=b&limit=20";
		string result;
		try
		{
			HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(requestUriString);
			httpWebRequest.Method = "GET";
			httpWebRequest.Headers.Add("Authorization", "Bearer " + accessToken);
			using HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
			using StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8);
			string json = streamReader.ReadToEnd();
			result = FormatRecentMessages(json);
		}
		catch (Exception ex)
		{
			ProjectData.SetProjectError(ex);
			Exception ex2 = ex;
			LastError = ex2.Message;
			result = "";
			ProjectData.ClearProjectError();
		}
		return result;
	}

	public static string FormatRecentMessages(string json)
	{
		if (string.IsNullOrWhiteSpace(json))
		{
			return "";
		}
		try
		{
			JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
			if (!(javaScriptSerializer.DeserializeObject(json) is Dictionary<string, object> dictionary) || !dictionary.ContainsKey("chunk"))
			{
				return json;
			}
			if (!(dictionary["chunk"] is object[] array))
			{
				return json;
			}
			StringBuilder stringBuilder = new StringBuilder();
			object[] array2 = array;
			for (int i = 0; i < array2.Length; i = checked(i + 1))
			{
				object objectValue = RuntimeHelpers.GetObjectValue(array2[i]);
				if (objectValue is Dictionary<string, object> dictionary2)
				{
					string text = "";
					if (dictionary2.ContainsKey("sender"))
					{
						text = Conversions.ToString(dictionary2["sender"]);
					}
					Dictionary<string, object> dictionary3 = dictionary2["content"] as Dictionary<string, object>;
					string text2 = "";
					if (dictionary3 != null && dictionary3.ContainsKey("body"))
					{
						text2 = Conversions.ToString(dictionary3["body"]);
					}
					if (text2.Length != 0)
					{
						stringBuilder.AppendLine(text + ": " + text2);
					}
				}
			}
			string text3 = stringBuilder.ToString().Trim();
			if (text3.Length > 0)
			{
				return text3;
			}
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
		return json;
	}

	private static string PostJson(string url, string jsonBody, string bearerToken)
	{
		HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
		httpWebRequest.Method = "POST";
		httpWebRequest.ContentType = "application/json";
		if (!string.IsNullOrWhiteSpace(bearerToken))
		{
			httpWebRequest.Headers.Add("Authorization", "Bearer " + bearerToken);
		}
		byte[] bytes = Encoding.UTF8.GetBytes(jsonBody);
		httpWebRequest.ContentLength = bytes.Length;
		using (Stream stream = httpWebRequest.GetRequestStream())
		{
			stream.Write(bytes, 0, bytes.Length);
		}
		using HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
		using StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8);
		return streamReader.ReadToEnd();
	}

	private static string GetJson(string url, string bearerToken)
	{
		HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
		httpWebRequest.Method = "GET";
		if (!string.IsNullOrWhiteSpace(bearerToken))
		{
			httpWebRequest.Headers.Add("Authorization", "Bearer " + bearerToken);
		}
		using HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
		using StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8);
		return streamReader.ReadToEnd();
	}

	private static string NormalizeHomeserver(string homeserver)
	{
		string text = ((homeserver != null) ? homeserver : "").Trim().TrimEnd('/');
		if (text.Length == 0)
		{
			return "";
		}
		if (!text.StartsWith("http", StringComparison.OrdinalIgnoreCase))
		{
			text = "https://" + text;
		}
		return text;
	}
}
