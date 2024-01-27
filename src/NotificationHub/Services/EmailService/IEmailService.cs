using NotificationHub.Persistence.Entities;

namespace NotificationHub.Services.EmailService;

public interface IEmailService
{
	Task<bool> SendEmailAsync(string subject, string body, string fromAddress, string toAddress, string? replyToAddress, SmtpConfiguration smtpConfiguration, CancellationToken ct = default);
}