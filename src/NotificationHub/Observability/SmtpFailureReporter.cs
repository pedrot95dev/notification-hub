using Sentry;

namespace NotificationHub.Observability;

public readonly record struct SmtpFailureReport(string Message, string? SmtpHost)
{
	public static SmtpFailureReport Describe(Exception exception, string? smtpHost)
	{
		var host = string.IsNullOrWhiteSpace(smtpHost) || smtpHost.Contains('@')
			? null
			: smtpHost;
		return new SmtpFailureReport($"SMTP send failed ({exception.GetType().Name})", host);
	}
}

public interface ISmtpFailureReporter
{
	void Capture(Exception exception, string? smtpHost);
}

public sealed class NullSmtpFailureReporter : ISmtpFailureReporter
{
	public void Capture(Exception exception, string? smtpHost)
	{
	}
}

public sealed class SentrySmtpFailureReporter : ISmtpFailureReporter
{
	public void Capture(Exception exception, string? smtpHost)
	{
		if (!SentrySdk.IsEnabled)
		{
			return;
		}

		var report = SmtpFailureReport.Describe(exception, smtpHost);
		var sentryEvent = new SentryEvent
		{
			Message = report.Message,
			Level = SentryLevel.Error,
			Logger = "EmailService"
		};
		if (report.SmtpHost is not null)
		{
			sentryEvent.SetTag("smtp.host", report.SmtpHost);
		}

		SentrySdk.CaptureEvent(sentryEvent);
	}
}
