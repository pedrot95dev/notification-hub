using MimeKit;
using NotificationHub.Persistence.Entities;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace NotificationHub.Services.EmailService;

public class EmailService : IEmailService
{
	private readonly ILogger<EmailService> _logger;

	public EmailService(ILogger<EmailService> logger)
	{
		_logger = logger;
	}
	
	public async Task<bool> SendEmailAsync(string subject, string body, string fromAddress, string toAddress, string? replyToAddress, SmtpConfiguration smtpConfiguration, CancellationToken ct = default)
	{
		_logger.LogInformation("Sending email...");
		
		using var smtpClient = new SmtpClient();
		try
		{
			await smtpClient.ConnectAsync(smtpConfiguration.Host, smtpConfiguration.Port, smtpConfiguration.EnableSsl, ct);
			await smtpClient.AuthenticateAsync(smtpConfiguration.UserName, smtpConfiguration.Password, ct);
			
			var message = new MimeMessage();
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
			return false;
		}
		finally
		{
			await smtpClient.DisconnectAsync(true, ct);
		}
		
		_logger.LogInformation("Email sent successfully");
		return true;
	
	}
}