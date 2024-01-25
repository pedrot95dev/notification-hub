using FastEndpoints;
using NotificationHub.Configuration;

namespace NotificationHub.Pipeline;

public class ApiIdHeaderPreProcessor : IGlobalPreProcessor
{
	public const string AppIdHeader = "x-app-id";

	private readonly IEnumerable<Application> _allowedApplications;

	public ApiIdHeaderPreProcessor(IConfiguration configuration)
	{
		_allowedApplications = configuration.Get<ApplicationsConfiguration>()?.Applications!;
	}
	
	public Task PreProcessAsync(IPreProcessorContext ctx, CancellationToken ct)
	{
		if (ctx.HttpContext.ResponseStarted())
		{
			return Task.CompletedTask;
		}
		
		if (!ctx.HttpContext.Request.Headers.TryGetValue(AppIdHeader, out var appId)
			|| _allowedApplications.All(x => x.Id != appId))
		{
			return ctx.HttpContext.Response.SendUnauthorizedAsync(cancellation: ct);
		}

		return Task.CompletedTask;
	}
}