using FastEndpoints;

namespace NotificationHub.Pipeline;

public class ApiIdHeaderPreProcessor : IGlobalPreProcessor
{
	public const string AppIdHeader = "x-app-id";

	public async Task PreProcessAsync(IPreProcessorContext ctx, CancellationToken ct)
	{
		if (ctx.HttpContext.ResponseStarted())
		{
			return;
		}
		
		var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<ApiIdHeaderPreProcessor>>();
		var ipAddress = ctx.HttpContext.Connection.RemoteIpAddress;
		
		if (!ctx.HttpContext.Request.Headers.TryGetValue(AppIdHeader, out var appId))
		{
			logger.LogWarning("Unauthorized request from {RemoteIpAddress}: missing {AppHeader}", ipAddress, AppIdHeader);
			await ctx.HttpContext.Response.SendUnauthorizedAsync(cancellation: ct);
			return;
		}
		
		logger.LogInformation("Request from {RemoteIpAddress} with {AppHeader}={AppId}", ipAddress, AppIdHeader, appId);
	}
}