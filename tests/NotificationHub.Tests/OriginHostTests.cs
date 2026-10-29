using NotificationHub.Pipeline;
using Xunit;

namespace NotificationHub.Tests;

public class OriginHostTests
{
	[Theory]
	[InlineData("https://dconstructions.ch", "dconstructions.ch")]
	[InlineData("https://www.dconstructions.ch/", "dconstructions.ch")]
	[InlineData("HTTPS://WWW.DCONSTRUCTIONS.CH", "dconstructions.ch")]
	[InlineData("dconstructions.ch", "dconstructions.ch")]
	[InlineData("www.pedroteixeira.dev", "pedroteixeira.dev")]
	public void Normalize_drops_scheme_www_slash_and_case(string value, string expected)
	{
		Assert.Equal(expected, OriginHost.Normalize(value));
	}

	[Fact]
	public void Matches_compares_normalized_hosts()
	{
		Assert.True(OriginHost.Matches("https://www.dconstructions.ch/", "dconstructions.ch"));
		Assert.False(OriginHost.Matches("https://evil.example", "dconstructions.ch"));
		Assert.False(OriginHost.Matches("not a uri", "dconstructions.ch"));
	}
}
