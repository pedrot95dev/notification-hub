using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using NotificationHub.Services.CurrentApplication;
using NotificationHub.Services.EmailService;

namespace NotificationHub.Endpoints.SendEmail;

public class SendEmailEndpointSummary : EndpointSummary
{
	public SendEmailEndpointSummary()
	{
		Summary = "Send email";
		Description = "Send email";
		ExampleRequest = new SendEmailRequest
		{
			Subject = "Subject",
			Message = "Email message",
			ReplayToEmail = "email to replay to"
		};
		Response(204, "Ok response with the content.");
		Response<ErrorResponse>(400, "Validation request");
		Response<ErrorResponse>(401, "Unauthorized request");
	}
}

public class SendEmailEndpoint : Endpoint<SendEmailRequest,
										  Results<Ok, ProblemDetails>>
{
	private readonly ICurrentApplication _currentApplication;
	private readonly IEmailService _emailService;

	public SendEmailEndpoint(
		ICurrentApplication currentApplication,
		IEmailService emailService)
	{
		_currentApplication = currentApplication;
		_emailService = emailService;
	}
	
	public override void Configure()
	{
		Post("email/send");
		Summary(new SendEmailEndpointSummary());
		AllowAnonymous();
	}

	public override async Task HandleAsync(SendEmailRequest req, CancellationToken ct)
	{
		var smtpConfigurationResult = _currentApplication.SmtpConfiguration;

		if (smtpConfigurationResult.IsT1)
		{
			await SendUnauthorizedAsync(ct);
			return;
		}
		
		var smtpConfiguration = smtpConfigurationResult.AsT0;
		
		await _emailService.SendEmailAsync(req.Subject, req.Message, smtpConfiguration.UserName, smtpConfiguration.UserName, req.ReplayToEmail, smtpConfiguration, ct);
		
		await SendNoContentAsync(ct);
	}
}