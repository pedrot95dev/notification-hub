using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Utils;
using NotificationHub.Persistence.Entities;
using NotificationHub.Security;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace NotificationHub.Services.EmailService;

public class EmailService : IEmailService
{
	private readonly ILogger<EmailService> _logger;
	private readonly ISmtpPasswordProtector _passwords;
	private readonly string _localDomain;

	public EmailService(ILogger<EmailService> logger, IOptions<EmailOptions> options, ISmtpPasswordProtector passwords)
	{
		_logger = logger;
		_passwords = passwords;
		var configured = options.Value.LocalDomain;
		_localDomain = string.IsNullOrWhiteSpace(configured)
			? EmailOptions.DefaultLocalDomain
			: configured.Trim();
	}
	
	public async Task<EmailSendResult> SendEmailAsync(string subject, string body, string fromAddress, string toAddress, string? replyToAddress, SmtpConfiguration smtpConfiguration, CancellationToken ct = default)
	{
		_logger.LogInformation("Sending email...");
		
		using var smtpClient = new SmtpClient();
		smtpClient.LocalDomain = _localDomain;
		try
		{
			await smtpClient.ConnectAsync(smtpConfiguration.Host, smtpConfiguration.Port, smtpConfiguration.EnableSsl, ct);
			var password = _passwords.Decrypt(smtpConfiguration.Password);
			await smtpClient.AuthenticateAsync(smtpConfiguration.UserName, password, ct);
			
			var message = new MimeMessage();
			message.MessageId = MimeUtils.GenerateMessageId(_localDomain);
			message.Subject = subject;

			// For plain text body
			message.Body = new TextPart("plain")
			{
				Text = body
			};
			
			message.From.Add(new MailboxAddress(string.Empty, fromAddress));
			message.To.Add(new MailboxAddress(string.Empty, toAddress));
			if (replyToAddress is not null)
			{
				message.ReplyTo.Add(new MailboxAddress(string.Empty, replyToAddress));
			}
			
			await smtpClient.SendAsync(message, ct);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error sending email");
			return EmailSendResult.Fail(TrimError(ex.Message));
		}
		finally
		{
			await smtpClient.DisconnectAsync(true, ct);
		}
		
		_logger.LogInformation("Email sent successfully");
		return EmailSendResult.Ok();
	}

	private static string TrimError(string message)
	{
		const int maxLength = 2000;
		if (message.Length <= maxLength)
		{
			return message;
		}

		return message[..maxLength];
	}
}