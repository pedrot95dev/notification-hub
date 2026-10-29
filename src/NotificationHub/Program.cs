using FastEndpoints;
using FastEndpoints.Swagger;
using NotificationHub.Persistence;
using NotificationHub.Pipeline;
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