using FastEndpoints;
using NotificationHub.Services.CurrentApplication;

namespace NotificationHub.Pipeline;

public class ApplicationOriginPreProcessor : IGlobalPreProcessor
{
	public const string OriginHeader = "Origin";

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

		var currentApplication = ctx.HttpContext.RequestServices.GetRequiredService<ICurrentApplication>();
		var applicationResult = currentApplication.Application;
		if (applicationResult.IsT1)
		{
			return;
		}

		var application = applicationResult.AsT0;
		if (OriginHost.Matches(origin, application.Domain))
		{
			return;
		}

		ctx.HttpContext.RequestServices
			.GetRequiredService<ILogger<ApplicationOriginPreProcessor>>()
			.LogWarning(
			"Rejected request for app {AppId}: Origin host does not match the application domain",
			application.ExternalId);
		await ctx.HttpContext.Response.SendUnauthorizedAsync(cancellation: ct);
	}
}
