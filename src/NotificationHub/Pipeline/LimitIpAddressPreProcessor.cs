using System.Net;
using FastEndpoints;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace NotificationHub.Pipeline;

public class LimitIpAddressPreProcessor : IGlobalPreProcessor
{
	public const string ConnectingIpHeader = "CF-Connecting-IP";

	// FastEndpoints constructs global preprocessors once through a static ServiceResolver and
	// reuses that instance across WebApplicationFactory hosts. Capturing IMemoryCache in the
	// constructor ties the limiter to whichever host won the race; disposing that host throws
	// ObjectDisposedException on the next request. Resolve per request instead.
	private static readonly object Gate = new();

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
		var services = http.RequestServices;
		var cache = services.GetRequiredService<IMemoryCache>();
		var options = services.GetRequiredService<IOptions<RateLimitOptions>>().Value;
		var logger = services.GetRequiredService<ILogger<LimitIpAddressPreProcessor>>();
		var trustedProxies = ParseTrustedProxies(options.TrustedProxies, logger);
		var peer = http.Connection.RemoteIpAddress;
		var clientIp = ResolveClientIp(http, peer, trustedProxies);
		var appId = ReadAppId(http);
		var maxRequests = ResolveMaxRequests(appId, options);
		var key = $"{appId}|{clientIp}";

		var limited = false;
		lock (Gate)
		{
			limited = IsLimited(cache, key, maxRequests, options.WindowMinutes);
		}

		if (!limited)
		{
			return;
		}

		logger.LogWarning(
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

	private static bool IsLimited(IMemoryCache cache, string key, int maxRequests, int windowMinutes)
	{
		var window = TimeSpan.FromMinutes(windowMinutes <= 0 ? 30 : windowMinutes);

		if (cache.TryGetValue(key, out RateLimitCounter? existing) && existing is not null)
		{
			var remaining = existing.ExpiresAt - DateTimeOffset.UtcNow;
			if (remaining <= TimeSpan.Zero)
			{
				cache.Set(key, RateLimitCounter.Start(window), window);
				return false;
			}

			if (existing.Count > maxRequests)
			{
				return true;
			}

			existing.Count++;
			cache.Set(key, existing, remaining);
			return false;
		}

		cache.Set(key, RateLimitCounter.Start(window), window);
		return false;
	}

	private static int ResolveMaxRequests(string appId, RateLimitOptions options)
	{
		if (!string.IsNullOrEmpty(appId)
		    && options.AppLimits is not null
		    && options.AppLimits.TryGetValue(appId, out var appMax))
		{
			return appMax;
		}

		return options.MaxRequests;
	}

	private static HashSet<IPAddress> ParseTrustedProxies(IEnumerable<string>? proxies, ILogger logger)
	{
		var trusted = new HashSet<IPAddress>();
		foreach (var raw in proxies ?? [])
		{
			if (IPAddress.TryParse(raw, out var parsed))
			{
				trusted.Add(Normalize(parsed));
			}
			else
			{
				logger.LogWarning("Ignoring RateLimit trusted proxy value {Proxy} because it is not an IP address", raw);
			}
		}

		return trusted;
	}

	private static IPAddress ResolveClientIp(HttpContext http, IPAddress? peer, HashSet<IPAddress> trustedProxies)
	{
		var normalizedPeer = peer is null ? IPAddress.None : Normalize(peer);
		if (peer is null || !trustedProxies.Contains(normalizedPeer))
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
