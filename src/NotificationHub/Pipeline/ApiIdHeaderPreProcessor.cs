using FastEndpoints;
using FluentValidation.Results;
using Microsoft.Extensions.Options;
using NotificationHub.Configuration;

namespace NotificationHub.Pipeline;

public class ApiIdHeaderPreProcessor : IGlobalPreProcessor
{
	public const string AppIdHeader = "x-app-id";
	
	public readonly IEnumerable<Application> _allowedApplications;

	public ApiIdHeaderPreProcessor(IConfiguration configuration)
	{
		_allowedApplications = configuration.GetSection("Applications").Get<ApplicationsConfiguration>()?.Applications!;
	}
	
	public Task PreProcessAsync(IPreProcessorContext ctx, CancellationToken ct)
	{
		if (!ctx.HttpContext.Request.Headers.TryGetValue(AppIdHeader, out var appId))
		{
			ctx.ValidationFailures.Add(new ValidationFailure("MissingHeaders", $"The {AppIdHeader} header needs to be set!"));

			return ctx.HttpContext.Response.SendErrorsAsync(ctx.ValidationFailures, cancellation: ct);
		}

		if (_allowedApplications.All(x => x.Id != appId))
		{
			return ctx.HttpContext.Response.SendUnauthorizedAsync(cancellation: ct);
		}

		return Task.CompletedTask;
	}
}