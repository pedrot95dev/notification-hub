using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using NotificationHub.Persistence;
using NotificationHub.Persistence.Entities;
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
	private readonly ApplicationDbContext _dbContext;

	public SendEmailEndpoint(
		ICurrentApplication currentApplication,
		IEmailService emailService,
		ApplicationDbContext dbContext)
	{
		_currentApplication = currentApplication;
		_emailService = emailService;
		_dbContext = dbContext;
	}
	
	public override void Configure()
	{
		Post("emails/send");
		Summary(new SendEmailEndpointSummary());
		AllowAnonymous();
	}

	public override async Task HandleAsync(SendEmailRequest req, CancellationToken ct)
	{
		var applicationResult = _currentApplication.Application;
		if (applicationResult.IsT1)
		{
			await SendUnauthorizedAsync(ct);
			return;
		}
		
		var application = applicationResult.AsT0;
		var smtpConfiguration = _currentApplication.SmtpConfiguration.AsT0;
		
		var emailsSentCount = await _dbContext.EmailSents
			.Where(x => x.ApplicationId == application.Id)
			.CountAsync(ct);
		
		var subject = $"#{emailsSentCount}: {req.Subject}";
		
		await _emailService.SendEmailAsync(subject, req.Message, smtpConfiguration.UserName, application.EmailDestination, req.ReplayToEmail, smtpConfiguration, ct);
		
		await LogEmailSent(application.Id, req.ReplayToEmail, ct);

		await SendNoContentAsync(ct);
	}

	private async Task LogEmailSent(Guid applicationId, string email, CancellationToken ct)
	{
		var emailSent = new EmailSent(applicationId, email);
		await _dbContext.EmailSents.AddAsync(emailSent, ct);
		await _dbContext.SaveChangesAsync(ct);
	}
}