using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.VisualBasic.CompilerServices;
using MimeKit;

namespace LCARSMail;

public class SmtpMailService
{
	[DebuggerNonUserCode]
	public SmtpMailService()
	{
	}

	public MimeMessage CreateMimeMessage(MailAccount account, string toAddrs, string ccAddrs, string subject, string bodyText, List<string> attachmentPaths, string bodyHtml = null)
	{
		InternetAddressList list = null;
		string text = TryParseAddressList(toAddrs, ref list);
		if (text != null)
		{
			throw new InvalidOperationException(text);
		}
		if (list == null)
		{
			list = new InternetAddressList();
		}
		InternetAddressList list2 = null;
		text = TryParseAddressList(ccAddrs, ref list2);
		if (text != null)
		{
			throw new InvalidOperationException(text);
		}
		return BuildMessage(account, list, list2, subject, bodyText, bodyHtml, attachmentPaths, signPgp: false, encryptPgp: false);
	}

	public string Send(MailAccount account, string password, string toAddrs, string ccAddrs, string subject, string bodyText, List<string> attachmentPaths, bool signPgp = false, bool encryptPgp = false, string bodyHtml = null)
	{
		string result;
		if (account == null)
		{
			result = "No mail account selected.";
		}
		else
		{
			if (password == null)
			{
				password = "";
			}
			string text = ((account.SmtpHost != null) ? account.SmtpHost : "").Trim();
			if (text.Length == 0)
			{
				result = "SMTP server is not configured for this account.";
			}
			else
			{
				InternetAddressList list = null;
				string text2 = TryParseAddressList(toAddrs, ref list);
				if (text2 != null)
				{
					result = text2;
				}
				else if (list == null || list.Count == 0)
				{
					result = "Enter at least one To address.";
				}
				else
				{
					InternetAddressList list2 = null;
					text2 = TryParseAddressList(ccAddrs, ref list2);
					if (text2 != null)
					{
						result = text2;
					}
					else
					{
						try
						{
							MimeMessage message = BuildMessage(account, list, list2, subject, bodyText, bodyHtml, attachmentPaths, signPgp, encryptPgp);
							using (SmtpClient smtpClient = Connect(account, password))
							{
								CancellationToken cancellationToken = default(CancellationToken);
								smtpClient.Send(message, cancellationToken);
							}
							result = "";
						}
						catch (Exception ex)
						{
							ProjectData.SetProjectError(ex);
							Exception ex2 = ex;
							string message2 = ex2.Message;
							string text3 = ((message2 != null) ? message2 : "Send failed.").Trim();
							if (text3.Length == 0)
							{
								text3 = "Send failed.";
							}
							result = text3;
							ProjectData.ClearProjectError();
						}
					}
				}
			}
		}
		return result;
	}

	private static MimeMessage BuildMessage(MailAccount account, InternetAddressList toList, InternetAddressList ccList, string subject, string bodyText, string bodyHtml, List<string> attachmentPaths, bool signPgp, bool encryptPgp)
	{
		MimeMessage mimeMessage = new MimeMessage();
		mimeMessage.From.Add(ResolveFromAddress(account));
		mimeMessage.To.AddRange(toList);
		if (ccList != null && ccList.Count > 0)
		{
			mimeMessage.Cc.AddRange(ccList);
		}
		mimeMessage.Subject = ((subject != null) ? subject : "");
		string bodyText2 = ((bodyText != null) ? bodyText : "");
		MimeEntity bodyEntity = null;
		if (signPgp || encryptPgp)
		{
			if (!MailPgpService.TryEncryptBody(account, toList.ToString(), bodyText2, signPgp, encryptPgp, ref bodyEntity))
			{
				throw new InvalidOperationException((MailPgpService.LastError != null) ? MailPgpService.LastError : "PGP failed.");
			}
		}
		else
		{
			bodyEntity = BuildBodyEntity(bodyText2, bodyHtml, attachmentPaths);
		}
		mimeMessage.Body = bodyEntity;
		string text = MailAutocryptService.BuildOutgoingAutocryptHeader(account);
		if (text.Length > 0)
		{
			mimeMessage.Headers["Autocrypt"] = text;
		}
		return mimeMessage;
	}

	private static MimeEntity BuildBodyEntity(string bodyText, string bodyHtml, List<string> attachmentPaths)
	{
		List<string> list = CollectExistingAttachmentPaths(attachmentPaths);
		MimeEntity mimeEntity = BuildAlternativePart(bodyText, bodyHtml);
		if (list.Count == 0)
		{
			return mimeEntity;
		}
		Multipart multipart = new Multipart("mixed");
		multipart.Add(mimeEntity);
		foreach (string item in list)
		{
			multipart.Add(CreateAttachmentPart(item));
		}
		return multipart;
	}

	private static MimeEntity BuildAlternativePart(string bodyText, string bodyHtml)
	{
		string text = ((bodyText != null) ? bodyText : "");
		string text2 = ((bodyHtml != null) ? bodyHtml : "").Trim();
		if (text2.Length == 0)
		{
			TextPart textPart = new TextPart("plain");
			textPart.Text = text;
			return textPart;
		}
		if (text.Length == 0)
		{
			text = text2;
		}
		return MailHtmlInlineRelatedBuilder.BuildHtmlRoot(text2, text);
	}

	private static List<string> CollectExistingAttachmentPaths(List<string> attachmentPaths)
	{
		List<string> list = new List<string>();
		if (attachmentPaths == null)
		{
			return list;
		}
		foreach (string attachmentPath in attachmentPaths)
		{
			if (!string.IsNullOrWhiteSpace(attachmentPath))
			{
				string text = attachmentPath.Trim();
				if (File.Exists(text))
				{
					list.Add(text);
				}
			}
		}
		return list;
	}

	private static MimePart CreateAttachmentPart(string filePath)
	{
		MimePart mimePart = new MimePart();
		mimePart.Content = new MimeContent(File.OpenRead(filePath));
		mimePart.ContentDisposition = new ContentDisposition("attachment");
		mimePart.ContentTransferEncoding = ContentEncoding.Base64;
		mimePart.FileName = Path.GetFileName(filePath);
		return mimePart;
	}

	private static MailboxAddress ResolveFromAddress(MailAccount account)
	{
		MailAccount mailAccount = account;
		string text = ((mailAccount.EmailAddress != null) ? mailAccount.EmailAddress : "").Trim();
		if (text.Length == 0)
		{
			mailAccount = account;
			text = ((mailAccount.Username != null) ? mailAccount.Username : "").Trim();
		}
		if (text.Length == 0)
		{
			mailAccount = account;
			text = ((mailAccount.SmtpUsername != null) ? mailAccount.SmtpUsername : "").Trim();
		}
		if (text.Length == 0)
		{
			throw new InvalidOperationException("Account has no From email address.");
		}
		mailAccount = account;
		string text2 = ((mailAccount.DisplayName != null) ? mailAccount.DisplayName : "").Trim();
		if (text2.Length == 0)
		{
			return MailboxAddress.Parse(text);
		}
		return new MailboxAddress(text2, text);
	}

	private static string TryParseAddressList(string raw, ref InternetAddressList list)
	{
		list = new InternetAddressList();
		string text = MailAddressHelper.NormalizeForTransport(((raw != null) ? raw : "").Trim());
		string result;
		if (text.Length == 0)
		{
			result = null;
		}
		else
		{
			try
			{
				list = InternetAddressList.Parse(text);
				result = null;
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Exception ex2 = ex;
				result = "Invalid address: " + text + " (" + ex2.Message + ")";
				ProjectData.ClearProjectError();
			}
		}
		return result;
	}

	private static SmtpClient Connect(MailAccount account, string password)
	{
		MailAccount mailAccount = account;
		string host = ((mailAccount.SmtpHost != null) ? mailAccount.SmtpHost : "").Trim();
		int num = account.SmtpPort;
		if (num <= 0)
		{
			if (account.SmtpSsl && !account.SmtpStartTls)
			{
				num = 465;
			}
			else
			{
				num = 587;
			}
		}
		mailAccount = account;
		string text = ((mailAccount.SmtpUsername != null) ? mailAccount.SmtpUsername : "").Trim();
		if (text.Length == 0)
		{
			mailAccount = account;
			text = ((mailAccount.Username != null) ? mailAccount.Username : "").Trim();
		}
		if (text.Length == 0)
		{
			mailAccount = account;
			text = ((mailAccount.EmailAddress != null) ? mailAccount.EmailAddress : "").Trim();
		}
		SmtpClient smtpClient = new SmtpClient();
		bool strictMode = ((account != null && account.StrictTls) ? true : false);
		smtpClient.ServerCertificateValidationCallback = [SpecialName] (object s, X509Certificate cert, X509Chain chain, SslPolicyErrors errors) => TlsPolicyStore.ValidateCertificate(host, num, cert as X509Certificate2, errors, strictMode);
		CancellationToken cancellationToken = default(CancellationToken);
		smtpClient.Connect(host, num, ResolveSecureSocketOptions(account, num), cancellationToken);
		if (MailOAuthProviders.UsesOAuth(account))
		{
			smtpClient.Authenticate(new SaslMechanismOAuth2(text, password), cancellationToken);
		}
		else
		{
			smtpClient.Authenticate(text, password, cancellationToken);
		}
		return smtpClient;
	}

	private static SecureSocketOptions ResolveSecureSocketOptions(MailAccount account, int port)
	{
		if (account.SmtpStartTls)
		{
			return SecureSocketOptions.StartTlsWhenAvailable;
		}
		if (account.SmtpSsl)
		{
			return SecureSocketOptions.SslOnConnect;
		}
		if (port == 465)
		{
			return SecureSocketOptions.SslOnConnect;
		}
		return SecureSocketOptions.Auto;
	}
}
