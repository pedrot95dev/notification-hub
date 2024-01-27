using FastEndpoints;
using Microsoft.Extensions.Caching.Memory;

namespace NotificationHub.Pipeline;

public class LimitIpAddressPreProcessor : IGlobalPreProcessor
{
	private const int MaxRequests = 5;
	private const int Minutes = 30;

	private readonly ILogger<LimitIpAddressPreProcessor> _logger;
	private readonly IMemoryCache _memoryCache;

	public LimitIpAddressPreProcessor(
		ILogger<LimitIpAddressPreProcessor> logger,
		IMemoryCache memoryCache)
	{
		_logger = logger;
		_memoryCache = memoryCache;
	}
	
	public async Task PreProcessAsync(IPreProcessorContext ctx, CancellationToken ct)
	{
		if (_memoryCache.TryGetValue(ctx.HttpContext.Connection.RemoteIpAddress, out int count))
		{
			if (count > MaxRequests)
			{
				_logger.LogWarning("Unauthorized request from {RemoteIpAddress}: too many requests", ctx.HttpContext.Connection.RemoteIpAddress);
				await ctx.HttpContext.Response.SendForbiddenAsync(ct);
				return;
			}
		}
		
		_memoryCache.Set(ctx.HttpContext.Connection.RemoteIpAddress, count + 1, TimeSpan.FromMinutes(Minutes));
	}
}