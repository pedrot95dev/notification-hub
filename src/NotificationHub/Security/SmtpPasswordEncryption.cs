namespace NotificationHub.Security;

public static class SmtpPasswordEncryption
{
	public const string Argument = "--encrypt-smtp-passwords";
	public const string FlagKey = "Security:EncryptExistingPasswords";
	public const string KeyRingPathKey = "Security:DataProtectionKeyRingPath";
	public const string ApplicationName = "NotificationHub";

	public static bool ShouldRun(string[] args, IConfiguration configuration)
	{
		if (args.Any(arg => string.Equals(arg, Argument, StringComparison.Ordinal)))
		{
			return true;
		}

		return configuration.GetValue<bool>(FlagKey);
	}
}
