using FastEndpoints;
using FluentValidation.Results;
using Microsoft.Extensions.Options;
using NotificationHub.Configuration;

namespace NotificationHub.Pipeline;

public class DomainVerifierPreProcessor : IGlobalPreProcessor
{
	private readonly ILogger<DomainVerifierPreProcessor> _logger;
	private readonly IWebHostEnvironment _environment;
	private readonly IEnumerable<Application> _allowedApplications;

	public DomainVerifierPreProcessor(
		IConfiguration configuration,
		ILogger<DomainVerifierPreProcessor> logger,
		IWebHostEnvironment environment)
	{
		_logger = logger;
		_environment = environment;
		_allowedApplications = configuration.GetSection("Applications").Get<ApplicationsConfiguration>()?.Applications!;
	}
	
	public Task PreProcessAsync(IPreProcessorContext ctx, CancellationToken ct)
	{
		if (_environment.IsDevelopment())
		{
			return Task.CompletedTask;
		}
		
		if (!ctx.HttpContext.Request.Headers.TryGetValue("Host", out var host)
			|| !ctx.HttpContext.Request.Headers.TryGetValue("Origin", out var origin))
		{
			return ctx.HttpContext.Response.SendUnauthorizedAsync(cancellation: ct);
		}

		try
		{
			var hostUri = new Uri(host);
			var originUri = new Uri(origin);
			
			if (_allowedApplications.Any(x => x.Domain == hostUri.Host && x.Domain == originUri.Host))
			{
				return Task.CompletedTask;
			}
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to parse host or origin header");
		}
		
		return ctx.HttpContext.Response.SendUnauthorizedAsync(cancellation: ct);
	}
}