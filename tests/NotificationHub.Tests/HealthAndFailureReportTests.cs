using System.Net;
using System.Text.Json;
using NotificationHub.Observability;
using Xunit;

namespace NotificationHub.Tests;

public class HealthAndFailureReportTests
{
	[Fact]
	public async Task Health_reports_database_connectivity_without_secrets_and_is_not_rate_limited()
	{
		await using var factory = new NotificationHubApiFactory(new Dictionary<string, string?>
		{
			["RateLimit:MaxRequests"] = "1",
			["RateLimit:WindowMinutes"] = "30",
			["RateLimit:TrustedProxies:0"] = "127.0.0.1"
		});
		var client = factory.CreateClient();

		for (var i = 0; i < 5; i++)
		{
			var response = await client.GetAsync("/health");
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
			var body = await response.Content.ReadAsStringAsync();
			using var document = JsonDocument.Parse(body);
			Assert.Equal("healthy", document.RootElement.GetProperty("status").GetString());
			Assert.DoesNotContain("Password", body, StringComparison.OrdinalIgnoreCase);
			Assert.DoesNotContain("ConnectionString", body, StringComparison.OrdinalIgnoreCase);
			Assert.False(response.Headers.Contains("x-app-id"));
		}
	}

	[Fact]
	public void Smtp_failure_report_omits_message_content_and_addresses()
	{
		var exception = new InvalidOperationException("550 user@example.com rejected. Body was: please call me");
		var report = SmtpFailureReport.Describe(exception, "user@example.com");

		Assert.Equal("SMTP send failed (InvalidOperationException)", report.Message);
		Assert.Null(report.SmtpHost);
		Assert.DoesNotContain("user@example.com", report.Message);
		Assert.DoesNotContain("please call me", report.Message);

		var hostOnly = SmtpFailureReport.Describe(exception, "smtp.example.com");
		Assert.Equal("smtp.example.com", hostOnly.SmtpHost);
	}
}
