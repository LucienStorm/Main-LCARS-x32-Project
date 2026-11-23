using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using LCARS;
using LCARS.Controls;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

public sealed class MailOAuthBrowserFlow
{
	private class frmOAuthWait : LCARSForm
	{
		private bool cancelled;

		public frmOAuthWait(string message)
		{
			Text = "LCARS COMMS";
			BackColor = Color.Black;
			ForeColor = Color.Orange;
			FormBorderStyle = FormBorderStyle.None;
			ControlBox = false;
			DoubleBuffered = true;
			BindToWorkingArea = false;
			StartPosition = FormStartPosition.CenterParent;
			Size clientSize = new Size(280, 96);
			ClientSize = clientSize;
			ShowInTaskbar = false;
			FlatButton flatButton = new FlatButton
			{
				ButtonText = "SIGNING IN",
				Text = "SIGNING IN",
				Clickable = false,
				Color = LCARScolorStyles.StaticTan,
				ButtonTextAlign = ContentAlignment.BottomRight
			};
			clientSize = new Size(256, 28);
			flatButton.Size = clientSize;
			Point location = new Point(12, 8);
			flatButton.Location = location;
			Controls.Add(flatButton);
			FlatButton flatButton2 = new FlatButton
			{
				ButtonText = "CANCEL",
				Text = "CANCEL",
				Color = LCARScolorStyles.CriticalFunction,
				ButtonTextAlign = ContentAlignment.MiddleCenter
			};
			clientSize = new Size(100, 32);
			flatButton2.Size = clientSize;
			location = new Point(168, 52);
			flatButton2.Location = location;
			flatButton2.Click += [SpecialName] [DebuggerStepThrough] (object a0, EventArgs a1) =>
			{
				((VB_0024AnonymousDelegate_1)([SpecialName] () =>
				{
					cancelled = true;
					DialogResult = DialogResult.Cancel;
					Close();
				}))();
			};
			Controls.Add(flatButton2);
		}

		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			if (!cancelled && DialogResult != DialogResult.Cancel)
			{
				DialogResult = DialogResult.OK;
			}
			base.OnFormClosing(e);
		}
	}

	public static string LastError;

	private MailOAuthBrowserFlow()
	{
	}

	public static bool TryAuthorize(IWin32Window owner, MailAccount account, ref string accessToken)
	{
		LastError = null;
		accessToken = null;
		bool result;
		if (account == null)
		{
			LastError = "Account is required.";
			result = false;
		}
		else
		{
			MailOAuthProviders.ProviderInfo providerInfo = MailOAuthProviders.TryResolveProvider(account);
			if (providerInfo == null)
			{
				LastError = "No OAuth provider for this mail host.";
				result = false;
			}
			else
			{
				int port = 0;
				HttpListener httpListener = null;
				try
				{
					httpListener = StartLoopbackListener(ref port);
				}
				catch (Exception ex)
				{
					ProjectData.SetProjectError(ex);
					Exception ex2 = ex;
					LastError = "Could not start local login listener: " + ex2.Message;
					result = false;
					ProjectData.ClearProjectError();
					goto IL_029d;
				}
				string redirectUri = "http://127.0.0.1:" + port + "/";
				string text = NewRandomToken(32);
				string text2 = NewCodeVerifier();
				string codeChallenge = ToCodeChallenge(text2);
				string loginHint = ((account.EmailAddress != null) ? account.EmailAddress : account.Username);
				string url = BuildAuthorizeUrl(providerInfo, redirectUri, text, codeChallenge, loginHint);
				frmOAuthWait frmOAuthWait = new frmOAuthWait("");
				string text3 = null;
				string text4 = null;
				bool flag = false;
				Thread thread = new Thread([SpecialName] () =>
				{
					try
					{
						HttpListenerContext context = httpListener.GetContext();
						NameValueCollection queryString = context.Request.QueryString;
						string text5 = queryString["error_description"];
						string text6 = ((text5 != null) ? text5 : queryString["error"]);
						string a = queryString["state"];
						string text7 = queryString["code"];
						if (!string.IsNullOrEmpty(text6))
						{
							text4 = text6;
						}
						else if (!string.Equals(a, text, StringComparison.Ordinal))
						{
							text4 = "OAuth state mismatch.";
						}
						else if (string.IsNullOrEmpty(text7))
						{
							text4 = "Authorization code missing.";
						}
						else
						{
							text3 = text7;
						}
						WriteCallbackPage(context.Response, text4);
					}
					catch (HttpListenerException ex3)
					{
						ProjectData.SetProjectError(ex3);
						HttpListenerException ex4 = ex3;
						if (text4 == null && text3 == null)
						{
							text4 = "Login listener stopped.";
						}
						ProjectData.ClearProjectError();
					}
					catch (Exception ex5)
					{
						ProjectData.SetProjectError(ex5);
						Exception ex6 = ex5;
						text4 = ex6.Message;
						ProjectData.ClearProjectError();
					}
					finally
					{
						flag = true;
						try
						{
							frmOAuthWait.BeginInvoke((MethodInvoker)([SpecialName] () =>
							{
								frmOAuthWait.Close();
							}));
						}
						catch (Exception projectError5)
						{
							ProjectData.SetProjectError(projectError5);
							ProjectData.ClearProjectError();
						}
					}
				});
				thread.IsBackground = true;
				thread.Start();
				if (!LcarsCommsUi.OpenInLcarsBrowser(url))
				{
					try
					{
						httpListener.Stop();
					}
					catch (Exception projectError)
					{
						ProjectData.SetProjectError(projectError);
						ProjectData.ClearProjectError();
					}
					LastError = "Could not open LCARS Web Browser for sign-in.";
					result = false;
				}
				else
				{
					DialogResult dialogResult = ((owner == null) ? frmOAuthWait.ShowDialog() : frmOAuthWait.ShowDialog(owner));
					if (dialogResult == DialogResult.Cancel && !flag)
					{
						try
						{
							httpListener.Stop();
						}
						catch (Exception projectError2)
						{
							ProjectData.SetProjectError(projectError2);
							ProjectData.ClearProjectError();
						}
						LastError = "Sign-in cancelled.";
						result = false;
					}
					else
					{
						try
						{
							httpListener.Stop();
						}
						catch (Exception projectError3)
						{
							ProjectData.SetProjectError(projectError3);
							ProjectData.ClearProjectError();
						}
						try
						{
							httpListener.Close();
						}
						catch (Exception projectError4)
						{
							ProjectData.SetProjectError(projectError4);
							ProjectData.ClearProjectError();
						}
						if (!string.IsNullOrEmpty(text4))
						{
							LastError = text4;
							result = false;
						}
						else if (string.IsNullOrEmpty(text3))
						{
							LastError = "No authorization code received.";
							result = false;
						}
						else
						{
							string refreshToken = null;
							int expiresInSeconds = 3600;
							if (!ExchangeCode(providerInfo, text3, redirectUri, text2, ref accessToken, ref refreshToken, ref expiresInSeconds))
							{
								result = false;
							}
							else
							{
								MailOAuthStore.SaveTokens(account.Id, accessToken, refreshToken, expiresInSeconds);
								account.AuthMethod = "oauth2";
								result = !string.IsNullOrWhiteSpace(accessToken);
							}
						}
					}
				}
			}
		}
		goto IL_029d;
		IL_029d:
		return result;
	}

	public static bool TryRefresh(MailOAuthProviders.ProviderInfo provider, string refreshToken, ref string accessToken, ref string newRefreshToken, ref int expiresInSeconds)
	{
		LastError = null;
		accessToken = null;
		newRefreshToken = null;
		expiresInSeconds = 3600;
		if (provider == null || string.IsNullOrWhiteSpace(refreshToken))
		{
			LastError = "Refresh token required.";
			return false;
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("grant_type=refresh_token");
		stringBuilder.Append("&client_id=").Append(Uri.EscapeDataString(provider.ClientId));
		stringBuilder.Append("&refresh_token=").Append(Uri.EscapeDataString(refreshToken));
		if (!string.IsNullOrEmpty(provider.ClientSecret))
		{
			stringBuilder.Append("&client_secret=").Append(Uri.EscapeDataString(provider.ClientSecret));
		}
		if (!string.IsNullOrEmpty(provider.Scope))
		{
			stringBuilder.Append("&scope=").Append(Uri.EscapeDataString(provider.Scope));
		}
		string text = PostForm(provider.TokenEndpoint, stringBuilder.ToString());
		if (text.Length == 0)
		{
			return false;
		}
		return ParseTokenResponse(text, ref accessToken, ref newRefreshToken, ref expiresInSeconds);
	}

	private static HttpListener StartLoopbackListener(ref int port)
	{
		Exception ex = null;
		int num = 0;
		checked
		{
			do
			{
				int num2 = 17890 + num;
				HttpListener httpListener = new HttpListener();
				try
				{
					httpListener.Prefixes.Add("http://127.0.0.1:" + num2 + "/");
					httpListener.Start();
					port = num2;
					return httpListener;
				}
				catch (Exception ex2)
				{
					ProjectData.SetProjectError(ex2);
					Exception ex3 = ex2;
					ex = ex3;
					try
					{
						httpListener.Close();
					}
					catch (Exception projectError)
					{
						ProjectData.SetProjectError(projectError);
						ProjectData.ClearProjectError();
					}
					ProjectData.ClearProjectError();
				}
				num++;
			}
			while (num <= 19);
			if (ex != null)
			{
				throw ex;
			}
			throw new InvalidOperationException("No free loopback port for OAuth.");
		}
	}

	private static string BuildAuthorizeUrl(MailOAuthProviders.ProviderInfo provider, string redirectUri, string state, string codeChallenge, string loginHint)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(provider.AuthorizationEndpoint);
		stringBuilder.Append(provider.AuthorizationEndpoint.Contains("?") ? "&" : "?");
		stringBuilder.Append("client_id=").Append(Uri.EscapeDataString(provider.ClientId));
		stringBuilder.Append("&response_type=code");
		stringBuilder.Append("&redirect_uri=").Append(Uri.EscapeDataString(redirectUri));
		stringBuilder.Append("&scope=").Append(Uri.EscapeDataString(provider.Scope));
		stringBuilder.Append("&state=").Append(Uri.EscapeDataString(state));
		stringBuilder.Append("&access_type=offline");
		stringBuilder.Append("&prompt=consent");
		if (provider.UsePkce)
		{
			stringBuilder.Append("&code_challenge=").Append(Uri.EscapeDataString(codeChallenge));
			stringBuilder.Append("&code_challenge_method=S256");
		}
		if (!string.IsNullOrWhiteSpace(loginHint))
		{
			stringBuilder.Append("&login_hint=").Append(Uri.EscapeDataString(loginHint.Trim()));
		}
		return stringBuilder.ToString();
	}

	private static bool ExchangeCode(MailOAuthProviders.ProviderInfo provider, string code, string redirectUri, string codeVerifier, ref string accessToken, ref string refreshToken, ref int expiresInSeconds)
	{
		accessToken = null;
		refreshToken = null;
		expiresInSeconds = 3600;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("grant_type=authorization_code");
		stringBuilder.Append("&code=").Append(Uri.EscapeDataString(code));
		stringBuilder.Append("&redirect_uri=").Append(Uri.EscapeDataString(redirectUri));
		stringBuilder.Append("&client_id=").Append(Uri.EscapeDataString(provider.ClientId));
		if (!string.IsNullOrEmpty(provider.ClientSecret))
		{
			stringBuilder.Append("&client_secret=").Append(Uri.EscapeDataString(provider.ClientSecret));
		}
		if (provider.UsePkce)
		{
			stringBuilder.Append("&code_verifier=").Append(Uri.EscapeDataString(codeVerifier));
		}
		string text = PostForm(provider.TokenEndpoint, stringBuilder.ToString());
		if (text.Length == 0)
		{
			return false;
		}
		return ParseTokenResponse(text, ref accessToken, ref refreshToken, ref expiresInSeconds);
	}

	private static bool ParseTokenResponse(string json, ref string accessToken, ref string refreshToken, ref int expiresInSeconds)
	{
		bool result;
		try
		{
			JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
			Dictionary<string, object> dictionary = javaScriptSerializer.Deserialize<Dictionary<string, object>>(json);
			if (dictionary == null)
			{
				LastError = "Invalid token response.";
				result = false;
			}
			else if (dictionary.ContainsKey("error"))
			{
				LastError = Stringify(RuntimeHelpers.GetObjectValue(dictionary["error_description"]));
				if (LastError.Length == 0)
				{
					LastError = Stringify(RuntimeHelpers.GetObjectValue(dictionary["error"]));
				}
				result = false;
			}
			else
			{
				accessToken = Stringify(RuntimeHelpers.GetObjectValue(dictionary["access_token"]));
				refreshToken = Stringify(RuntimeHelpers.GetObjectValue(dictionary["refresh_token"]));
				expiresInSeconds = 3600;
				if (dictionary.ContainsKey("expires_in") && int.TryParse(Stringify(RuntimeHelpers.GetObjectValue(dictionary["expires_in"])), out var result2) && result2 > 0)
				{
					expiresInSeconds = result2;
				}
				if (accessToken.Length == 0)
				{
					LastError = "Access token missing.";
					result = false;
				}
				else
				{
					result = true;
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
		return result;
	}

	private static string PostForm(string url, string formBody)
	{
		string result;
		try
		{
			HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
			httpWebRequest.Method = "POST";
			httpWebRequest.ContentType = "application/x-www-form-urlencoded";
			httpWebRequest.Timeout = 60000;
			byte[] bytes = Encoding.UTF8.GetBytes((formBody != null) ? formBody : "");
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
			try
			{
				if (ex2.Response is HttpWebResponse httpWebResponse2)
				{
					using StreamReader streamReader2 = new StreamReader(httpWebResponse2.GetResponseStream(), Encoding.UTF8);
					string text = streamReader2.ReadToEnd();
					if (text.Length <= 0)
					{
						goto end_IL_0103;
					}
					LastError = text;
					result = text;
					ProjectData.ClearProjectError();
					goto end_IL_00d2;
					end_IL_0103:;
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
			LastError = ex2.Message;
			result = "";
			ProjectData.ClearProjectError();
			end_IL_00d2:;
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

	private static void WriteCallbackPage(HttpListenerResponse response, string errorText)
	{
		string s = ((!string.IsNullOrEmpty(errorText)) ? ("<!DOCTYPE html><html><body style='background:#000;color:#ff9900;font-family:Arial'><h2>LCARS COMMS</h2><p>Sign-in failed.</p><pre>" + HtmlEncode(errorText) + "</pre></body></html>") : "<!DOCTYPE html><html><body style='background:#000;color:#ff9900;font-family:Arial'><h2>LCARS COMMS</h2><p>Sign-in complete. You may close this tab.</p></body></html>");
		byte[] bytes = Encoding.UTF8.GetBytes(s);
		response.StatusCode = (string.IsNullOrEmpty(errorText) ? 200 : 400);
		response.ContentType = "text/html; charset=utf-8";
		response.ContentLength64 = bytes.Length;
		response.OutputStream.Write(bytes, 0, bytes.Length);
		response.OutputStream.Close();
	}

	private static string NewCodeVerifier()
	{
		return NewRandomToken(64);
	}

	private static string NewRandomToken(int byteLength)
	{
		byte[] data = new byte[checked(byteLength - 1 + 1)];
		using (RNGCryptoServiceProvider rNGCryptoServiceProvider = new RNGCryptoServiceProvider())
		{
			rNGCryptoServiceProvider.GetBytes(data);
		}
		return Base64UrlEncode(data);
	}

	private static string ToCodeChallenge(string verifier)
	{
		byte[] bytes = Encoding.ASCII.GetBytes(verifier);
		using SHA256 sHA = SHA256.Create();
		return Base64UrlEncode(sHA.ComputeHash(bytes));
	}

	private static string Base64UrlEncode(byte[] data)
	{
		string text = Convert.ToBase64String(data);
		return text.TrimEnd('=').Replace('+', '-').Replace('/', '_');
	}

	private static string Stringify(object value)
	{
		if (value == null)
		{
			return "";
		}
		return Convert.ToString(RuntimeHelpers.GetObjectValue(value), CultureInfo.InvariantCulture);
	}

	private static string HtmlEncode(string text)
	{
		if (text == null)
		{
			return "";
		}
		return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
			.Replace("\"", "&quot;");
	}
}
