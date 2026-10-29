namespace NotificationHub.Security;

public interface ISmtpPasswordProtector
{
	bool IsEncrypted(string stored);

	string Encrypt(string plaintext);

	string Decrypt(string stored);
}
