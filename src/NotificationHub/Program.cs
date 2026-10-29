using Microsoft.AspNetCore.DataProtection;
using FastEndpoints;
using FastEndpoints.Swagger;
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
