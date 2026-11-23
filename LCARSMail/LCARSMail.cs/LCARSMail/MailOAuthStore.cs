using System;
using System.Globalization;

namespace LCARSMail;

public sealed class MailOAuthStore
{
	private MailOAuthStore()
	{
	}

	public static void SaveTokens(string accountId, string accessToken, string refreshToken, int expiresInSeconds)
	{
		if (!string.IsNullOrWhiteSpace(accountId))
		{
			if (!string.IsNullOrWhiteSpace(accessToken))
			{
				AccountStore.SavePassword(AccessKey(accountId), accessToken);
			}
			if (!string.IsNullOrWhiteSpace(refreshToken))
			{
				AccountStore.SavePassword(RefreshKey(accountId), refreshToken);
			}
			DateTime dateTime = DateTime.UtcNow.AddSeconds(checked(Math.Max(60, expiresInSeconds) - 60));
			AccountStore.SavePassword(ExpiresKey(accountId), dateTime.ToString("o", CultureInfo.InvariantCulture));
		}
	}

	public static bool TryLoadAccessToken(string accountId, ref string accessToken)
	{
		accessToken = null;
		return (AccountStore.TryGetPassword(AccessKey(accountId), ref accessToken) && !string.IsNullOrWhiteSpace(accessToken)) ? true : false;
	}

	public static bool TryLoadRefreshToken(string accountId, ref string refreshToken)
	{
		refreshToken = null;
		return (AccountStore.TryGetPassword(RefreshKey(accountId), ref refreshToken) && !string.IsNullOrWhiteSpace(refreshToken)) ? true : false;
	}

	public static bool IsAccessTokenFresh(string accountId)
	{
		string password = null;
		if (!AccountStore.TryGetPassword(ExpiresKey(accountId), ref password) || string.IsNullOrWhiteSpace(password))
		{
			return false;
		}
		if (!DateTime.TryParse(password, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result))
		{
			return false;
		}
		return DateTime.Compare(result, DateTime.UtcNow) > 0;
	}

	public static bool TryEnsureAccessToken(MailAccount account, ref string accessToken)
	{
		accessToken = null;
		if (account == null || string.IsNullOrWhiteSpace(account.Id))
		{
			return false;
		}
		string id = account.Id;
		string accessToken2 = null;
		if (IsAccessTokenFresh(id) && TryLoadAccessToken(id, ref accessToken2))
		{
			accessToken = accessToken2;
			return true;
		}
		string refreshToken = null;
		if (!TryLoadRefreshToken(id, ref refreshToken))
		{
			if (TryLoadAccessToken(id, ref accessToken2))
			{
				accessToken = accessToken2;
				return true;
			}
			return false;
		}
		MailOAuthProviders.ProviderInfo providerInfo = MailOAuthProviders.TryResolveProvider(account);
		if (providerInfo == null)
		{
			return false;
		}
		string accessToken3 = null;
		string newRefreshToken = null;
		int expiresInSeconds = 3600;
		if (!MailOAuthBrowserFlow.TryRefresh(providerInfo, refreshToken, ref accessToken3, ref newRefreshToken, ref expiresInSeconds))
		{
			return false;
		}
		if (string.IsNullOrWhiteSpace(newRefreshToken))
		{
			newRefreshToken = refreshToken;
		}
		SaveTokens(id, accessToken3, newRefreshToken, expiresInSeconds);
		accessToken = accessToken3;
		return !string.IsNullOrWhiteSpace(accessToken);
	}

	private static string AccessKey(string accountId)
	{
		return accountId + "|oauth-access";
	}

	private static string RefreshKey(string accountId)
	{
		return accountId + "|oauth-refresh";
	}

	private static string ExpiresKey(string accountId)
	{
		return accountId + "|oauth-expires";
	}
}
