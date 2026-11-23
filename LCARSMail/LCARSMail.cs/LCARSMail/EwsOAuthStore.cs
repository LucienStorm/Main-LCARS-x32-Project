namespace LCARSMail;

public sealed class EwsOAuthStore
{
	private const string AccessKey = "ews-oauth";

	private const string RefreshKey = "ews-oauth-refresh";

	private EwsOAuthStore()
	{
	}

	public static void SaveToken(string token)
	{
		if (token != null)
		{
			AccountStore.SavePassword("ews-oauth", token);
		}
	}

	public static void SaveRefreshToken(string token)
	{
		if (token != null)
		{
			AccountStore.SavePassword("ews-oauth-refresh", token);
		}
	}

	public static bool TryLoadToken(ref string token)
	{
		token = null;
		return AccountStore.TryGetPassword("ews-oauth", ref token);
	}

	public static bool TryLoadRefreshToken(ref string token)
	{
		token = null;
		return AccountStore.TryGetPassword("ews-oauth-refresh", ref token);
	}

	public static bool TryEnsureAccessToken(ref string accessToken, string tenant, string clientId)
	{
		accessToken = null;
		string token = null;
		if (TryLoadToken(ref token) && !string.IsNullOrWhiteSpace(token))
		{
			accessToken = token;
			return true;
		}
		string token2 = null;
		if (!TryLoadRefreshToken(ref token2) || string.IsNullOrWhiteSpace(token2))
		{
			return false;
		}
		string accessToken2 = null;
		string newRefreshToken = null;
		if (EwsOAuthDeviceFlow.TryRefreshAccessToken(tenant, clientId, token2, ref accessToken2, ref newRefreshToken) != EwsOAuthDeviceFlow.TokenPollResult.Success)
		{
			return false;
		}
		if (!string.IsNullOrWhiteSpace(accessToken2))
		{
			SaveToken(accessToken2);
			accessToken = accessToken2;
		}
		if (!string.IsNullOrWhiteSpace(newRefreshToken))
		{
			SaveRefreshToken(newRefreshToken);
		}
		return (accessToken != null && accessToken.Length > 0) ? true : false;
	}
}
