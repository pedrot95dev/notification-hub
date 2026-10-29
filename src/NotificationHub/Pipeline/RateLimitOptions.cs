namespace NotificationHub.Pipeline;

public class RateLimitOptions
{
	public const string SectionName = "RateLimit";

	/// <summary>
	/// How many accepted requests a key may make before the next one is rejected.
	/// The check is <c>count &gt; MaxRequests</c>, so <c>MaxRequests + 1</c> requests are allowed in the window.
	/// </summary>
	public int MaxRequests { get; set; } = 5;

	public int WindowMinutes { get; set; } = 30;

	/// <summary>
	/// Remote addresses allowed to supply <c>CF-Connecting-IP</c>.
	/// Any other peer is keyed on its own address and the header is ignored.
	/// </summary>
	public string[] TrustedProxies { get; set; } = [];

	/// <summary>
	/// Optional per-application ceilings, keyed by <c>Applications.ExternalId</c>.
	/// Unlisted applications use <see cref="MaxRequests"/>.
	/// </summary>
	public Dictionary<string, int> AppLimits { get; set; } = new(StringComparer.Ordinal);
}
