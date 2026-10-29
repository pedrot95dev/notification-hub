using NotificationHub.Persistence.Entities;
using NotificationHub.Services.EmailService;

namespace NotificationHub.Tests;

public sealed class FakeEmailService : IEmailService
{
	public int CallCount { get; private set; }

	public EmailSendResult NextResult { get; set; } = EmailSendResult.Ok();

	public Task<EmailSendResult> SendEmailAsync(
		string subject,
		string body,
		string fromAddress,
		string toAddress,
		string? replyToAddress,
		SmtpConfiguration smtpConfiguration,
		CancellationToken ct = default)
	{
		CallCount++;
		return Task.FromResult(NextResult);
	}
}
