using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web.Script.Serialization;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

public sealed class TlsPolicyStore
{
	private const string PolicyFileName = "tls-policy.json";

	private TlsPolicyStore()
	{
	}

	public static bool ValidateCertificate(string host, int port, X509Certificate2 certificate, SslPolicyErrors sslPolicyErrors, bool strictMode)
	{
		if (certificate == null)
		{
			return !strictMode;
		}
		if (sslPolicyErrors == SslPolicyErrors.None)
		{
			RememberThumbprint(host, port, certificate);
			return true;
		}
		string key = EndpointKey(host, port);
		string value = null;
		Dictionary<string, string> dictionary = LoadMap();
		dictionary.TryGetValue(key, out value);
		string thumbprint = certificate.Thumbprint;
		if (!string.IsNullOrEmpty(value) && string.Equals(value, thumbprint, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (strictMode)
		{
			return false;
		}
		RememberThumbprint(host, port, certificate);
		return true;
	}

	public static void RememberThumbprint(string host, int port, X509Certificate2 certificate)
	{
		if (certificate != null)
		{
			string key = EndpointKey(host, port);
			Dictionary<string, string> dictionary = LoadMap();
			dictionary[key] = certificate.Thumbprint;
			SaveMap(dictionary);
		}
	}

	private static string EndpointKey(string host, int port)
	{
		return ((host != null) ? host : "").Trim().ToLowerInvariant() + ":" + port;
	}

	private static string PolicyPath()
	{
		return Path.Combine(AccountStore.DataRoot(), "tls-policy.json");
	}

	private static Dictionary<string, string> LoadMap()
	{
		string path = PolicyPath();
		Dictionary<string, string> result;
		if (!File.Exists(path))
		{
			result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
		else
		{
			try
			{
				JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
				Dictionary<string, string> dictionary = javaScriptSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path, Encoding.UTF8));
				result = ((dictionary != null) ? new Dictionary<string, string>(dictionary, StringComparer.OrdinalIgnoreCase) : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}

	private static void SaveMap(Dictionary<string, string> map)
	{
		if (map != null)
		{
			JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
			File.WriteAllText(PolicyPath(), javaScriptSerializer.Serialize(map), Encoding.UTF8);
		}
	}
}
