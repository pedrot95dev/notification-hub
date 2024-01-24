using FastEndpoints;
using FluentValidation.Results;
using Microsoft.Extensions.Options;
using NotificationHub.Configuration;

namespace NotificationHub.Pipeline;

public class DomainVerifierPreProcessor : IGlobalPreProcessor
{
	public readonly IEnumerable<Application> _allowedApplications;

	public DomainVerifierPreProcessor(IConfiguration configuration)
	{
		_allowedApplications = configuration.GetSection("Applications").Get<ApplicationsConfiguration>()?.Applications!;
	}
	
	public Task PreProcessAsync(IPreProcessorContext ctx, CancellationToken ct)
	{
		if (!ctx.HttpContext.Request.Headers.TryGetValue("Host", out var host)
			|| !ctx.HttpContext.Request.Headers.TryGetValue("Origin", out var origin))
		{
			return ctx.HttpContext.Response.SendUnauthorizedAsync(cancellation: ct);
		}

		if (_allowedApplications.All(x => x.Domain != new Uri(host).Host && x.Domain != new Uri(origin).Host))
		{
			return ctx.HttpContext.Response.SendUnauthorizedAsync(cancellation: ct);
		}
		
		return Task.CompletedTask;
	}
}