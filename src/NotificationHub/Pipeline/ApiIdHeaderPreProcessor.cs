using FastEndpoints;

namespace NotificationHub.Pipeline;

public class ApiIdHeaderPreProcessor : IGlobalPreProcessor
{
	public const string AppIdHeader = "x-app-id";

	private readonly ILogger<ApiIdHeaderPreProcessor> _logger;

	public ApiIdHeaderPreProcessor(ILogger<ApiIdHeaderPreProcessor> logger)
	{
		_logger = logger;
	}
	
	public async Task PreProcessAsync(IPreProcessorContext ctx, CancellationToken ct)
	{
		if (ctx.HttpContext.ResponseStarted())
		{
			return;
		}
		
		var ipAddress = ctx.HttpContext.Connection.RemoteIpAddress;
		
		if (!ctx.HttpContext.Request.Headers.TryGetValue(AppIdHeader, out var appId))
		{
			_logger.LogWarning("Unauthorized request from {RemoteIpAddress}: missing {AppHeader}", ipAddress, AppIdHeader);
			await ctx.HttpContext.Response.SendUnauthorizedAsync(cancellation: ct);
		}
		
		_logger.LogInformation("Request from {RemoteIpAddress} with {AppHeader}={AppId}", ipAddress, AppIdHeader, appId);
	}
}