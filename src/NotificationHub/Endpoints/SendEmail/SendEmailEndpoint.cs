using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;

namespace NotificationHub.Endpoints.SendEmail;

public class SendEmailEndpointSummary : EndpointSummary
{
	public SendEmailEndpointSummary()
	{
		Summary = "Send email";
		Description = "Send email";
		ExampleRequest = new SendEmailRequest
		{
			Message = "Email message",
			ReplayToEmail = "email to replay to"
		};
		Response(204, "Ok response with the content.");
		Response<ErrorResponse>(400, "Validation request");
	}
}

public class SendEmailEndpoint : Endpoint<SendEmailRequest,
										  Results<Ok, ProblemDetails>>
{
	public override void Configure()
	{
		Post("email/send");
		Summary(new SendEmailEndpointSummary());
		AllowAnonymous();
	}

	public override async Task HandleAsync(SendEmailRequest req, CancellationToken ct)
	{
		await SendNoContentAsync(ct);
	}
}