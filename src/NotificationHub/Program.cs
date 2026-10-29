using System.Reflection;
using Microsoft.AspNetCore.DataProtection;
using FastEndpoints;
using Sentry.Extensibility;
using FastEndpoints.Swagger;
using NotificationHub.Observability;
using NotificationHub.Persistence;
using NotificationHub.Pipeline;
using NotificationHub.Security;
using NotificationHub.Services.CurrentApplication;
using NotificationHub.Services.EmailService;

var builder = WebApplication.CreateBuilder(args);

builder.Services
	.AddFastEndpoints()
	.SwaggerDocument()
	.AddCors()
	.AddMemoryCache();

builder.Services.Configure<RateLimitOptions>(builder.Configuration.GetSection(RateLimitOptions.SectionName));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));

var dataProtection = builder.Services.AddDataProtection()
	.SetApplicationName(SmtpPasswordEncryption.ApplicationName);
var keyRingPath = builder.Configuration[SmtpPasswordEncryption.KeyRingPathKey];
if (!string.IsNullOrWhiteSpace(keyRingPath))
{
	dataProtection.PersistKeysToFileSystem(Directory.CreateDirectory(keyRingPath));
}

var sentryDsn = Environment.GetEnvironmentVariable("SENTRY_DSN");
if (!string.IsNullOrWhiteSpace(sentryDsn))
{
	builder.WebHost.UseSentry(options =>
	{
		options.Dsn = sentryDsn;
		options.SendDefaultPii = false;
		options.MaxRequestBodySize = RequestSize.None;
		options.MinimumEventLevel = LogLevel.None;
		options.MinimumBreadcrumbLevel = LogLevel.None;
		options.Release = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
		options.Environment = Environment.GetEnvironmentVariable("SENTRY_ENVIRONMENT")
			?? builder.Environment.EnvironmentName;
		options.SetBeforeSend((sentryEvent, _) =>
		{
			if (sentryEvent.Request is not null)
			{
				sentryEvent.Request.Data = null;
				sentryEvent.Request.Cookies = null;
			}

			return sentryEvent;
		});
	});
	builder.Services.AddSingleton<ISmtpFailureReporter, SentrySmtpFailureReporter>();
}
else
{
	builder.Services.AddSingleton<ISmtpFailureReporter, NullSmtpFailureReporter>();
}

builder.Services.AddSingleton<ISmtpPasswordProtector, SmtpPasswordProtector>();
builder.Services.AddScoped<SmtpPasswordEncryptor>();

builder.Services.AddScoped<ICurrentApplication, CurrentApplication>();
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddPersistence();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseCors(policy =>
{
	policy
		.WithOrigins(CorsOrigins(app.Configuration))
		.AllowAnyHeader()
		.AllowAnyMethod();
});

app.MapGet("/health", async (ApplicationDbContext db, ILoggerFactory loggerFactory, CancellationToken ct) =>
{
	var healthy = false;
	try
	{
		healthy = await db.Database.CanConnectAsync(ct);
	}
	catch (Exception ex)
	{
		loggerFactory.CreateLogger("Health").LogError(
			"Database health check failed ({ExceptionType})",
			ex.GetType().Name);
	}

	return healthy
		? Results.Ok(new { status = "healthy" })
		: Results.Json(new { status = "unhealthy" }, statusCode: StatusCodes.Status503ServiceUnavailable);
});

app.UseFastEndpoints(c =>
{
	c.Endpoints.Configurator = ep =>
	{
		ep.PreProcessor<LimitIpAddressPreProcessor>(Order.Before);
		ep.PreProcessor<ApiIdHeaderPreProcessor>(Order.Before);
		ep.PreProcessor<ApplicationOriginPreProcessor>(Order.Before);
	};
});

if (app.Environment.IsDevelopment())
{
	app.UseSwaggerGen();
}

if (!app.Environment.IsEnvironment("Testing"))
{
	var dbFeeder = app.Services.GetRequiredService<ApplicationDbFeeder>();
	await dbFeeder.MigrateAsync();
}

if (SmtpPasswordEncryption.ShouldRun(args, app.Configuration))
{
	if (string.IsNullOrWhiteSpace(keyRingPath))
	{
		throw new InvalidOperationException(
			"Refusing to encrypt SMTP passwords without Security:DataProtectionKeyRingPath. Ephemeral keys would make the ciphertext unreadable after a restart.");
	}

	await using var scope = app.Services.CreateAsyncScope();
	var encrypted = await scope.ServiceProvider.GetRequiredService<SmtpPasswordEncryptor>().EncryptExistingAsync();
	app.Logger.LogInformation("Encrypted {Count} stored SMTP passwords", encrypted);
}
else if (string.IsNullOrWhiteSpace(keyRingPath))
{
	app.Logger.LogWarning(
		"{Key} is not set. Data Protection keys stay in memory, so do not encrypt SMTP passwords until a key ring directory is mounted",
		SmtpPasswordEncryption.KeyRingPathKey);
}

app.Run();

static string[] CorsOrigins(IConfiguration configuration)
{
	var configured = configuration.GetSection("Cors:Origins").Get<string[]>();
	if (configured is { Length: > 0 })
	{
		return configured;
	}

	return
	[
		"https://dconstructions.ch",
		"https://www.dconstructions.ch",
		"https://pedroteixeira.dev",
		"https://www.pedroteixeira.dev"
	];
}

public partial class Program;
