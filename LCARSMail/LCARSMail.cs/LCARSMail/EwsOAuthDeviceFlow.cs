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

public sealed class EwsOAuthDeviceFlow
{
	public enum TokenPollResult
	{
		Success,
		Pending,
		Failed
	}

	public class DeviceStartResult
	{
		public string UserCode;

		public string VerificationUri;

		public string DeviceCode;

		public int IntervalSeconds;

		public string Message;

		[DebuggerNonUserCode]
		public DeviceStartResult()
		{
		}
	}

	public static string LastError;

	public const string DefaultClientId = "94933e60-ee10-4250-a050-3efd588b0718";

	public const string DefaultScope = "https://outlook.office365.com/EWS.AccessAsUser.All offline_access openid profile";

	private EwsOAuthDeviceFlow()
	{
	}

	public static DeviceStartResult TryStartDeviceFlow(string tenant, string clientId, string scope = null)
	{
		LastError = null;
		string text = NormalizeTenant(tenant);
		string text2 = ((clientId != null) ? clientId : "").Trim();
		if (text2.Length == 0)
		{
			text2 = "94933e60-ee10-4250-a050-3efd588b0718";
		}
		string stringToEscape = ((scope != null) ? scope : "https://outlook.office365.com/EWS.AccessAsUser.All offline_access openid profile").Trim();
		string url = "https://login.microsoftonline.com/" + text + "/oauth2/v2.0/devicecode";
		string formBody = "client_id=" + Uri.EscapeDataString(text2) + "&scope=" + Uri.EscapeDataString(stringToEscape);
		string text3 = PostForm(url, formBody);
		DeviceStartResult result;
		if (text3.Length == 0)
		{
			result = null;
		}
		else
		{
			try
			{
				JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
				Dictionary<string, object> dictionary = javaScriptSerializer.Deserialize<Dictionary<string, object>>(text3);
				if (dictionary == null)
				{
					LastError = "Invalid device code response.";
					result = null;
				}
				else if (dictionary.ContainsKey("error"))
				{
					LastError = Stringify(RuntimeHelpers.GetObjectValue(dictionary["error_description"]));
					if (LastError.Length == 0)
					{
						LastError = Stringify(RuntimeHelpers.GetObjectValue(dictionary["error"]));
					}
					result = null;
				}
				else
				{
					DeviceStartResult deviceStartResult = new DeviceStartResult();
					deviceStartResult.UserCode = Stringify(RuntimeHelpers.GetObjectValue(dictionary["user_code"]));
					string text4 = Stringify(RuntimeHelpers.GetObjectValue(dictionary["verification_uri_complete"]));
					if (text4.Length > 0)
					{
						deviceStartResult.VerificationUri = text4;
					}
					else
					{
						deviceStartResult.VerificationUri = Stringify(RuntimeHelpers.GetObjectValue(dictionary["verification_uri"]));
					}
					deviceStartResult.DeviceCode = Stringify(RuntimeHelpers.GetObjectValue(dictionary["device_code"]));
					deviceStartResult.Message = Stringify(RuntimeHelpers.GetObjectValue(dictionary["message"]));
					deviceStartResult.IntervalSeconds = 5;
					if (dictionary.ContainsKey("interval"))
					{
						int result2 = 0;
						if (int.TryParse(Stringify(RuntimeHelpers.GetObjectValue(dictionary["interval"])), out result2) && result2 > 0)
						{
							deviceStartResult.IntervalSeconds = result2;
						}
					}
					if (deviceStartResult.DeviceCode.Length == 0)
					{
						LastError = "Device code missing from response.";
						result = null;
					}
					else
					{
						result = deviceStartResult;
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

	public static TokenPollResult TryRefreshAccessToken(string tenant, string clientId, string refreshToken, ref string accessToken, ref string newRefreshToken)
	{
		LastError = null;
		accessToken = null;
		newRefreshToken = null;
		string text = NormalizeTenant(tenant);
		string text2 = ((clientId != null) ? clientId : "").Trim();
		if (text2.Length == 0)
		{
			text2 = "94933e60-ee10-4250-a050-3efd588b0718";
		}
		string text3 = ((refreshToken != null) ? refreshToken : "").Trim();
		TokenPollResult result;
		if (text3.Length == 0)
		{
			LastError = "Refresh token required.";
			result = TokenPollResult.Failed;
		}
		else
		{
			string url = "https://login.microsoftonline.com/" + text + "/oauth2/v2.0/token";
			string formBody = "grant_type=refresh_token&client_id=" + Uri.EscapeDataString(text2) + "&refresh_token=" + Uri.EscapeDataString(text3);
			string text4 = PostForm(url, formBody);
			if (text4.Length == 0)
			{
				result = TokenPollResult.Failed;
			}
			else
			{
				try
				{
					JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
					Dictionary<string, object> dictionary = javaScriptSerializer.Deserialize<Dictionary<string, object>>(text4);
					if (dictionary == null)
					{
						LastError = "Invalid refresh response.";
						result = TokenPollResult.Failed;
					}
					else if (dictionary.ContainsKey("access_token"))
					{
						accessToken = Stringify(RuntimeHelpers.GetObjectValue(dictionary["access_token"]));
						newRefreshToken = Stringify(RuntimeHelpers.GetObjectValue(dictionary["refresh_token"]));
						result = TokenPollResult.Success;
					}
					else
					{
						LastError = Stringify(RuntimeHelpers.GetObjectValue(dictionary["error_description"]));
						if (LastError.Length == 0)
						{
							LastError = Stringify(RuntimeHelpers.GetObjectValue(dictionary["error"]));
						}
						result = TokenPollResult.Failed;
					}
				}
				catch (Exception ex)
				{
					ProjectData.SetProjectError(ex);
					Exception ex2 = ex;
					LastError = ex2.Message;
					result = TokenPollResult.Failed;
					ProjectData.ClearProjectError();
				}
			}
		}
		return result;
	}

	public static TokenPollResult TryPollAccessToken(string tenant, string clientId, string deviceCode, ref string accessToken, ref string refreshToken)
	{
		LastError = null;
		accessToken = null;
		refreshToken = null;
		string text = NormalizeTenant(tenant);
		string text2 = ((clientId != null) ? clientId : "").Trim();
		if (text2.Length == 0)
		{
			text2 = "94933e60-ee10-4250-a050-3efd588b0718";
		}
		string text3 = ((deviceCode != null) ? deviceCode : "").Trim();
		TokenPollResult result;
		if (text3.Length == 0)
		{
			LastError = "Device code required.";
			result = TokenPollResult.Failed;
		}
		else
		{
			string url = "https://login.microsoftonline.com/" + text + "/oauth2/v2.0/token";
			string formBody = "grant_type=urn:ietf:params:oauth:grant-type:device_code&client_id=" + Uri.EscapeDataString(text2) + "&device_code=" + Uri.EscapeDataString(text3);
			string text4 = PostForm(url, formBody);
			if (text4.Length == 0)
			{
				result = TokenPollResult.Failed;
			}
			else
			{
				try
				{
					JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
					Dictionary<string, object> dictionary = javaScriptSerializer.Deserialize<Dictionary<string, object>>(text4);
					if (dictionary == null)
					{
						LastError = "Invalid token response.";
						result = TokenPollResult.Failed;
					}
					else if (dictionary.ContainsKey("access_token"))
					{
						accessToken = Stringify(RuntimeHelpers.GetObjectValue(dictionary["access_token"]));
						refreshToken = Stringify(RuntimeHelpers.GetObjectValue(dictionary["refresh_token"]));
						result = TokenPollResult.Success;
					}
					else
					{
						string text5 = Stringify(RuntimeHelpers.GetObjectValue(dictionary["error"]));
						if (text5.Equals("authorization_pending", StringComparison.OrdinalIgnoreCase) || text5.Equals("slow_down", StringComparison.OrdinalIgnoreCase))
						{
							result = TokenPollResult.Pending;
						}
						else
						{
							LastError = Stringify(RuntimeHelpers.GetObjectValue(dictionary["error_description"]));
							if (LastError.Length == 0)
							{
								LastError = text5;
							}
							result = TokenPollResult.Failed;
						}
					}
				}
				catch (Exception ex)
				{
					ProjectData.SetProjectError(ex);
					Exception ex2 = ex;
					LastError = ex2.Message;
					result = TokenPollResult.Failed;
					ProjectData.ClearProjectError();
				}
			}
		}
		return result;
	}

	private static string NormalizeTenant(string tenant)
	{
		string text = ((tenant != null) ? tenant : "").Trim();
		if (text.Length == 0)
		{
			return "organizations";
		}
		return text;
	}

	private static string PostForm(string url, string formBody)
	{
		string result;
		try
		{
			HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
			httpWebRequest.Method = "POST";
			httpWebRequest.ContentType = "application/x-www-form-urlencoded";
			httpWebRequest.Timeout = 45000;
			byte[] bytes = Encoding.UTF8.GetBytes(formBody);
			httpWebRequest.ContentLength = bytes.Length;
			using (Stream stream = httpWebRequest.GetRequestStream())
			{
				stream.Write(bytes, 0, bytes.Length);
			}
			using HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
			using StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8);
			result = streamReader.ReadToEnd();
		}
		catch (WebException ex)
		{
			ProjectData.SetProjectError(ex);
			WebException ex2 = ex;
			LastError = ReadWebExceptionBody(ex2);
			if (LastError.Length == 0)
			{
				LastError = ex2.Message;
			}
			result = "";
			ProjectData.ClearProjectError();
		}
		catch (Exception ex3)
		{
			ProjectData.SetProjectError(ex3);
			Exception ex4 = ex3;
			LastError = ex4.Message;
			result = "";
			ProjectData.ClearProjectError();
		}
		return result;
	}

	private static string ReadWebExceptionBody(WebException ex)
	{
		string result;
		if (ex == null || ex.Response == null)
		{
			result = "";
		}
		else
		{
			try
			{
				using StreamReader streamReader = new StreamReader(ex.Response.GetResponseStream(), Encoding.UTF8);
				result = streamReader.ReadToEnd();
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = "";
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}

	private static string Stringify(object value)
	{
		if (value == null)
		{
			return "";
		}
		return value.ToString();
	}
}
