using Microsoft.AspNetCore.DataProtection;

namespace NotificationHub.Security;

public sealed class SmtpPasswordProtector : ISmtpPasswordProtector
{
	public const string PayloadPrefix = "CfDJ8";
	public const string Purpose = "NotificationHub.SmtpPasswords";

	private readonly IDataProtector _protector;
	private readonly ILogger<SmtpPasswordProtector> _logger;

	public SmtpPasswordProtector(IDataProtectionProvider provider, ILogger<SmtpPasswordProtector> logger)
	{
		_protector = provider.CreateProtector(Purpose);
		_logger = logger;
	}

	public bool IsEncrypted(string stored)
	{
		return stored.StartsWith(PayloadPrefix, StringComparison.Ordinal);
	}

	public string Encrypt(string plaintext)
	{
		if (IsEncrypted(plaintext))
		{
			return plaintext;
		}

		return _protector.Protect(plaintext);
	}

	public string Decrypt(string stored)
	{
		if (!IsEncrypted(stored))
		{
			_logger.LogWarning("SMTP password is stored as legacy plaintext and was used without decryption");
			return stored;
		}

		return _protector.Unprotect(stored);
	}
}
