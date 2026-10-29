using System.Net;
using Xunit;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NotificationHub.Persistence;
using NotificationHub.Persistence.Entities;
using NotificationHub.Services.EmailService;

namespace NotificationHub.Tests;

public class SendEmailTests
{
	[Fact]
	public async Task Smtp_failure_returns_502_and_writes_no_sent_row()
	{
		await using var factory = new NotificationHubApiFactory();
		await factory.SeedApplicationAsync("dconstructions");
		factory.Email.NextResult = EmailSendResult.Fail("smtp failed");

		var response = await SendAsync(factory, appId: "dconstructions");

		Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
		Assert.Equal(1, factory.Email.CallCount);
		Assert.StartsWith("{", (await response.Content.ReadAsStringAsync()).TrimStart());

		using var scope = factory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		Assert.Equal(0, await db.EmailSents.CountAsync(x => x.Status == EmailSentStatus.Sent));
		Assert.Equal(1, await db.EmailSents.CountAsync(x => x.Status == EmailSentStatus.Failed));
	}

	[Theory]
	[InlineData("user@example.com\nBcc: evil@example.com")]
	[InlineData("user@example.com\r\nBcc: evil@example.com")]
	public async Task Reply_to_with_line_break_is_400_and_does_not_call_smtp(string replyTo)
	{
		await using var factory = new NotificationHubApiFactory();

		var response = await SendAsync(factory, appId: "dconstructions", replyTo: replyTo);

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		Assert.Equal(0, factory.Email.CallCount);
	}

	[Fact]
	public async Task Missing_app_id_is_401_and_does_not_call_smtp()
	{
		await using var factory = new NotificationHubApiFactory();

		var response = await SendAsync(factory, appId: null);

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		Assert.Equal(0, factory.Email.CallCount);
	}

	[Fact]
	public async Task Unknown_app_id_is_401_and_does_not_call_smtp()
	{
		await using var factory = new NotificationHubApiFactory();
		await factory.SeedApplicationAsync("dconstructions");

		var response = await SendAsync(factory, appId: "not-a-real-app");

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		Assert.Equal(0, factory.Email.CallCount);
	}

	[Theory]
	[InlineData(150, 20, HttpStatusCode.NoContent)]
	[InlineData(151, 20, HttpStatusCode.BadRequest)]
	[InlineData(10, 2000, HttpStatusCode.NoContent)]
	[InlineData(10, 2001, HttpStatusCode.BadRequest)]
	public async Task Subject_and_message_boundaries(int subjectLength, int messageLength, HttpStatusCode expected)
	{
		await using var factory = new NotificationHubApiFactory();
		await factory.SeedApplicationAsync("dconstructions");

		var response = await SendAsync(
			factory,
			appId: "dconstructions",
			subject: new string('a', subjectLength),
			message: new string('b', messageLength));

		Assert.Equal(expected, response.StatusCode);
		Assert.Equal(expected == HttpStatusCode.NoContent ? 1 : 0, factory.Email.CallCount);
	}

	[Fact]
	public async Task Trusted_peer_limits_each_cf_connecting_ip_separately()
	{
		await using var factory = new NotificationHubApiFactory(TightLimit("127.0.0.1"));
		factory.Peer = () => IPAddress.Loopback;

		Assert.NotEqual(HttpStatusCode.TooManyRequests, (await SendAsync(factory, appId: "one", clientIp: "203.0.113.1")).StatusCode);
		Assert.NotEqual(HttpStatusCode.TooManyRequests, (await SendAsync(factory, appId: "one", clientIp: "203.0.113.1")).StatusCode);
		Assert.Equal(HttpStatusCode.TooManyRequests, (await SendAsync(factory, appId: "one", clientIp: "203.0.113.1")).StatusCode);

		Assert.NotEqual(HttpStatusCode.TooManyRequests, (await SendAsync(factory, appId: "one", clientIp: "203.0.113.2")).StatusCode);
		Assert.NotEqual(HttpStatusCode.TooManyRequests, (await SendAsync(factory, appId: "two", clientIp: "203.0.113.1")).StatusCode);
	}

	[Fact]
	public async Task Untrusted_peer_ignores_cf_connecting_ip_and_x_forwarded_for()
	{
		await using var factory = new NotificationHubApiFactory(TightLimit("127.0.0.1"));
		factory.Peer = () => IPAddress.Parse("10.9.9.9");

		Assert.NotEqual(HttpStatusCode.TooManyRequests, (await SendAsync(factory, appId: "one", clientIp: "203.0.113.1", forwardedFor: "198.51.100.1")).StatusCode);
		Assert.NotEqual(HttpStatusCode.TooManyRequests, (await SendAsync(factory, appId: "one", clientIp: "203.0.113.2", forwardedFor: "198.51.100.2")).StatusCode);
		Assert.Equal(HttpStatusCode.TooManyRequests, (await SendAsync(factory, appId: "one", clientIp: "not-an-ip", forwardedFor: "198.51.100.3")).StatusCode);
	}

	private static Dictionary<string, string?> TightLimit(string trustedProxy) => new()
	{
		["RateLimit:MaxRequests"] = "1",
		["RateLimit:WindowMinutes"] = "30",
		["RateLimit:TrustedProxies:0"] = trustedProxy
	};

	private static async Task<HttpResponseMessage> SendAsync(
		NotificationHubApiFactory factory,
		string? appId,
		string subject = "Hello",
		string message = "A message body",
		string replyTo = "person@example.com",
		string? clientIp = null,
		string? forwardedFor = null)
	{
		var client = factory.CreateClient();
		var request = new HttpRequestMessage(HttpMethod.Post, "/emails/send")
		{
			Content = JsonContent.Create(new { subject, message, replayToEmail = replyTo })
		};
		if (appId is not null)
		{
			request.Headers.TryAddWithoutValidation("x-app-id", appId);
		}

		if (clientIp is not null)
		{
			request.Headers.TryAddWithoutValidation("CF-Connecting-IP", clientIp);
		}

		if (forwardedFor is not null)
		{
			request.Headers.TryAddWithoutValidation("X-Forwarded-For", forwardedFor);
		}

		return await client.SendAsync(request);
	}
}
