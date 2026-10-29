using FastEndpoints;
using NotificationHub.Services.CurrentApplication;

namespace NotificationHub.Pipeline;

public class ApplicationOriginPreProcessor : IGlobalPreProcessor
{
	public const string OriginHeader = "Origin";

	private readonly ILogger<ApplicationOriginPreProcessor> _logger;
	private readonly ICurrentApplication _currentApplication;

	public ApplicationOriginPreProcessor(
		ILogger<ApplicationOriginPreProcessor> logger,
		ICurrentApplication currentApplication)
	{
		_logger = logger;
		_currentApplication = currentApplication;
	}

	public async Task PreProcessAsync(IPreProcessorContext ctx, CancellationToken ct)
	{
		if (ctx.HttpContext.ResponseStarted())
		{
			return;
		}

		if (!ctx.HttpContext.Request.Headers.TryGetValue(OriginHeader, out var originValues))
		{
			return;
		}

		var origin = originValues.ToString();
		if (string.IsNullOrWhiteSpace(origin))
		{
			return;
		}

		var applicationResult = _currentApplication.Application;
		if (applicationResult.IsT1)
		{
			return;
		}

		var application = applicationResult.AsT0;
		if (OriginHost.Matches(origin, application.Domain))
		{
			return;
		}

		_logger.LogWarning(
			"Rejected request for app {AppId}: Origin host does not match the application domain",
			application.ExternalId);
		await ctx.HttpContext.Response.SendUnauthorizedAsync(cancellation: ct);
	}
}
