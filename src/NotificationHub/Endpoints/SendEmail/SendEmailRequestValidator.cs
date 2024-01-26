using FastEndpoints;
using FluentValidation;

namespace NotificationHub.Endpoints.SendEmail;

public class SendEmailRequestValidator : Validator<SendEmailRequest>
{
	public SendEmailRequestValidator()
	{
		RuleFor(x => x.Subject)
			.NotEmpty()
			.MaximumLength(100);
			
		RuleFor(x => x.Message)
			.NotEmpty()
			.MaximumLength(500);
			
		RuleFor(x => x.ReplayToEmail)
			.NotEmpty()
			.EmailAddress();
	}
}