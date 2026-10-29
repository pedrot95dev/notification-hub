namespace NotificationHub.Pipeline;

public static class OriginHost
{
	public static bool Matches(string? origin, string? applicationDomain)
	{
		var left = Normalize(origin);
		var right = Normalize(applicationDomain);
		return left is not null && right is not null && string.Equals(left, right, StringComparison.Ordinal);
	}

	public static string? Normalize(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return null;
		}

		var trimmed = value.Trim().TrimEnd('/');
		if (!TryReadHost(trimmed, out var host))
		{
			return null;
		}

		host = host.Trim().ToLowerInvariant();
		if (host.StartsWith("www.", StringComparison.Ordinal))
		{
			host = host[4..];
		}

		return host.Length == 0 ? null : host;
	}

	private static bool TryReadHost(string value, out string host)
	{
		var candidate = value.Contains("://", StringComparison.Ordinal) ? value : "https://" + value;
		if (Uri.TryCreate(candidate, UriKind.Absolute, out var uri) && !string.IsNullOrEmpty(uri.IdnHost))
		{
			host = uri.IdnHost;
			return true;
		}

		host = string.Empty;
		return false;
	}
}
