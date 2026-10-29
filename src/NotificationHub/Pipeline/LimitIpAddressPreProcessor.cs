using System.Net;
using FastEndpoints;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace NotificationHub.Pipeline;

public class LimitIpAddressPreProcessor : IGlobalPreProcessor
{
	public const string ConnectingIpHeader = "CF-Connecting-IP";

	private readonly ILogger<LimitIpAddressPreProcessor> _logger;
	private readonly IMemoryCache _memoryCache;
	private readonly RateLimitOptions _options;
	private readonly HashSet<IPAddress> _trustedProxies;
	private readonly object _gate = new();

	public LimitIpAddressPreProcessor(
		ILogger<LimitIpAddressPreProcessor> logger,
		IMemoryCache memoryCache,
		IOptions<RateLimitOptions> options)
	{
		_logger = logger;
		_memoryCache = memoryCache;
		_options = options.Value;
		_trustedProxies = new HashSet<IPAddress>();
		foreach (var raw in _options.TrustedProxies ?? [])
		{
			if (IPAddress.TryParse(raw, out var parsed))
			{
				_trustedProxies.Add(Normalize(parsed));
			}
			else
			{
				_logger.LogWarning("Ignoring RateLimit trusted proxy value {Proxy} because it is not an IP address", raw);
			}
		}
	}

	public async Task PreProcessAsync(IPreProcessorContext ctx, CancellationToken ct)
	{
		if (ctx.HttpContext.ResponseStarted())
		{
			return;
		}

		if (ctx.HttpContext.Request.Path.Equals("/health", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}

		var http = ctx.HttpContext;
		var peer = http.Connection.RemoteIpAddress;
		var clientIp = ResolveClientIp(http, peer);
		var appId = ReadAppId(http);
		var maxRequests = ResolveMaxRequests(appId);
		var key = $"{appId}|{clientIp}";

		var limited = false;
		lock (_gate)
		{
			limited = IsLimited(key, maxRequests);
		}

		if (!limited)
		{
			return;
		}

		_logger.LogWarning(
			"Too many requests for app {AppId} from client {ClientIp} (peer {PeerIp})",
			string.IsNullOrEmpty(appId) ? "(none)" : appId,
			clientIp,
			peer);

		await http.Response.SendStringAsync(
			"""{"statusCode":429,"message":"Too many requests. Please try again later."}""",
			StatusCodes.Status429TooManyRequests,
			"application/json; charset=utf-8",
			ct);
	}

	private bool IsLimited(string key, int maxRequests)
	{
		var window = TimeSpan.FromMinutes(_options.WindowMinutes <= 0 ? 30 : _options.WindowMinutes);

		if (_memoryCache.TryGetValue(key, out RateLimitCounter? existing) && existing is not null)
		{
			var remaining = existing.ExpiresAt - DateTimeOffset.UtcNow;
			if (remaining <= TimeSpan.Zero)
			{
				_memoryCache.Set(key, RateLimitCounter.Start(window), window);
				return false;
			}

			if (existing.Count > maxRequests)
			{
				return true;
			}

			existing.Count++;
			_memoryCache.Set(key, existing, remaining);
			return false;
		}

		_memoryCache.Set(key, RateLimitCounter.Start(window), window);
		return false;
	}

	private int ResolveMaxRequests(string appId)
	{
		if (!string.IsNullOrEmpty(appId)
		    && _options.AppLimits is not null
		    && _options.AppLimits.TryGetValue(appId, out var appMax))
		{
			return appMax;
		}

		return _options.MaxRequests;
	}

	private IPAddress ResolveClientIp(HttpContext http, IPAddress? peer)
	{
		var normalizedPeer = peer is null ? IPAddress.None : Normalize(peer);
		if (peer is null || !_trustedProxies.Contains(normalizedPeer))
		{
			return normalizedPeer;
		}

		if (!http.Request.Headers.TryGetValue(ConnectingIpHeader, out var headerValues))
		{
			return normalizedPeer;
		}

		if (IPAddress.TryParse(headerValues.ToString(), out var parsed))
		{
			return Normalize(parsed);
		}

		return normalizedPeer;
	}

	private static string ReadAppId(HttpContext http)
	{
		if (!http.Request.Headers.TryGetValue(ApiIdHeaderPreProcessor.AppIdHeader, out var appId))
		{
			return string.Empty;
		}

		return appId.ToString().Trim();
	}

	private static IPAddress Normalize(IPAddress address)
	{
		return address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
	}

	private sealed class RateLimitCounter
	{
		public int Count { get; set; }

		public DateTimeOffset ExpiresAt { get; set; }

		public static RateLimitCounter Start(TimeSpan window) => new()
		{
			Count = 1,
			ExpiresAt = DateTimeOffset.UtcNow.Add(window)
		};
	}
}
