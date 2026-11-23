using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail;

[StandardModule]
public sealed class AccountStore
{
	private class AccountDto
	{
		public string Id
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string DisplayName
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string EmailAddress
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string ImapHost
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public int ImapPort
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public bool ImapSsl
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public bool ImapStartTls
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string Username
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string SmtpHost
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public int SmtpPort
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public bool SmtpSsl
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public bool SmtpStartTls
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string SmtpUsername
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string Protocol
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string EwsUrl
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		public string AuthMethod
		{
			[DebuggerNonUserCode]
			get;
			[DebuggerNonUserCode]
			set;
		}

		[DebuggerNonUserCode]
		public AccountDto()
		{
		}

		public static AccountDto FromModel(MailAccount acct)
		{
			AccountDto accountDto = new AccountDto();
			accountDto.Protocol = ((acct.Protocol != null) ? acct.Protocol : "");
			accountDto.EwsUrl = ((acct.EwsUrl != null) ? acct.EwsUrl : "");
			accountDto.Id = ((acct.Id != null) ? acct.Id : "");
			accountDto.DisplayName = ((acct.DisplayName != null) ? acct.DisplayName : "");
			accountDto.EmailAddress = ((acct.EmailAddress != null) ? acct.EmailAddress : "");
			accountDto.ImapHost = ((acct.ImapHost != null) ? acct.ImapHost : "");
			accountDto.ImapPort = acct.ImapPort;
			accountDto.ImapSsl = acct.ImapSsl;
			accountDto.ImapStartTls = acct.ImapStartTls;
			accountDto.Username = ((acct.Username != null) ? acct.Username : "");
			accountDto.SmtpHost = ((acct.SmtpHost != null) ? acct.SmtpHost : "");
			accountDto.SmtpPort = ((acct.SmtpPort > 0) ? acct.SmtpPort : 587);
			accountDto.SmtpSsl = acct.SmtpSsl;
			accountDto.SmtpStartTls = acct.SmtpStartTls;
			accountDto.SmtpUsername = ((acct.SmtpUsername != null) ? acct.SmtpUsername : "");
			accountDto.AuthMethod = ((acct.AuthMethod != null) ? acct.AuthMethod : "");
			return accountDto;
		}

		public MailAccount ToModel()
		{
			MailAccount mailAccount = new MailAccount();
			string protocol = Protocol;
			mailAccount.Protocol = ((protocol != null) ? protocol : "");
			string ewsUrl = EwsUrl;
			mailAccount.EwsUrl = ((ewsUrl != null) ? ewsUrl : "");
			string id = Id;
			mailAccount.Id = ((id != null) ? id : "");
			string displayName = DisplayName;
			mailAccount.DisplayName = ((displayName != null) ? displayName : "");
			string emailAddress = EmailAddress;
			mailAccount.EmailAddress = ((emailAddress != null) ? emailAddress : "");
			string imapHost = ImapHost;
			mailAccount.ImapHost = ((imapHost != null) ? imapHost : "");
			mailAccount.ImapPort = ImapPort;
			mailAccount.ImapSsl = ImapSsl;
			mailAccount.ImapStartTls = ImapStartTls;
			string username = Username;
			mailAccount.Username = ((username != null) ? username : "");
			string smtpHost = SmtpHost;
			mailAccount.SmtpHost = ((smtpHost != null) ? smtpHost : "");
			mailAccount.SmtpPort = ((SmtpPort > 0) ? SmtpPort : 587);
			mailAccount.SmtpSsl = SmtpSsl;
			mailAccount.SmtpStartTls = SmtpStartTls;
			string smtpUsername = SmtpUsername;
			mailAccount.SmtpUsername = ((smtpUsername != null) ? smtpUsername : "");
			string authMethod = AuthMethod;
			mailAccount.AuthMethod = ((authMethod != null) ? authMethod : "");
			return mailAccount;
		}
	}

	private const string AccountsFileName = "accounts.json";

	private const string SecretsSubdir = "secrets";

	public static string LastError;

	public static string DataRoot()
	{
		string text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LCARS x32", "Mail");
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		return text;
	}

	public static bool SaveAccounts(List<MailAccount> accounts)
	{
		LastError = null;
		bool result;
		if (accounts == null)
		{
			LastError = "accounts is required.";
			result = false;
		}
		else
		{
			try
			{
				string path = DataRoot();
				if (!Directory.Exists(path))
				{
					Directory.CreateDirectory(path);
				}
				List<AccountDto> list = new List<AccountDto>();
				foreach (MailAccount account in accounts)
				{
					list.Add(AccountDto.FromModel(account));
				}
				JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
				string contents = javaScriptSerializer.Serialize(list);
				File.WriteAllText(AccountsJsonPath(), contents, Encoding.UTF8);
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
		}
		return result;
	}

	public static List<MailAccount> LoadAccounts()
	{
		string path = AccountsJsonPath();
		List<MailAccount> result;
		if (!File.Exists(path))
		{
			result = new List<MailAccount>();
		}
		else
		{
			try
			{
				string input = File.ReadAllText(path, Encoding.UTF8);
				JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
				List<AccountDto> list = javaScriptSerializer.Deserialize<List<AccountDto>>(input);
				if (list == null)
				{
					result = new List<MailAccount>();
				}
				else
				{
					List<MailAccount> list2 = new List<MailAccount>();
					foreach (AccountDto item in list)
					{
						if (item != null)
						{
							list2.Add(item.ToModel());
						}
					}
					result = list2;
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = new List<MailAccount>();
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}

	public static void SavePassword(string accountId, string password)
	{
		if (string.IsNullOrWhiteSpace(accountId))
		{
			throw new ArgumentException("accountId is required.", "accountId");
		}
		if (password == null)
		{
			password = "";
		}
		byte[] bytes = Encoding.UTF8.GetBytes(password);
		byte[] bytes2 = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
		string path = Path.Combine(DataRoot(), "secrets");
		if (!Directory.Exists(path))
		{
			Directory.CreateDirectory(path);
		}
		File.WriteAllBytes(SecretPath(accountId), bytes2);
	}

	public static bool TryGetPassword(string accountId, ref string password)
	{
		password = null;
		bool result;
		if (string.IsNullOrWhiteSpace(accountId))
		{
			result = false;
		}
		else
		{
			string path = SecretPath(accountId);
			if (!File.Exists(path))
			{
				result = false;
			}
			else
			{
				try
				{
					byte[] encryptedData = File.ReadAllBytes(path);
					byte[] bytes = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
					password = Encoding.UTF8.GetString(bytes);
					result = true;
				}
				catch (Exception projectError)
				{
					ProjectData.SetProjectError(projectError);
					password = null;
					result = false;
					ProjectData.ClearProjectError();
				}
			}
		}
		return result;
	}

	private static string AccountsJsonPath()
	{
		return Path.Combine(DataRoot(), "accounts.json");
	}

	private static string SecretPath(string accountId)
	{
		string text = SanitizeFileToken(accountId);
		return Path.Combine(DataRoot(), "secrets", text + ".dpapi");
	}

	private static string SanitizeFileToken(string accountId)
	{
		char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
		StringBuilder stringBuilder = new StringBuilder(accountId.Length);
		int i = 0;
		for (int length = accountId.Length; i < length; i = checked(i + 1))
		{
			char value = accountId[i];
			if (Array.IndexOf(invalidFileNameChars, value) >= 0)
			{
				stringBuilder.Append('_');
			}
			else
			{
				stringBuilder.Append(value);
			}
		}
		string text = stringBuilder.ToString();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "account";
		}
		return text;
	}
}
