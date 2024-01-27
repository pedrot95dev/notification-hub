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

builder.Services.AddScoped<ICurrentApplication, CurrentApplication>();
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddPersistence();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseCors(builder =>
{
	var origins = app.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? Array.Empty<string>();
	
	builder
		.WithOrigins(origins)
		.WithMethods("POST")
		.WithHeaders(ApiIdHeaderPreProcessor.AppIdHeader);
});

app.UseFastEndpoints(c =>
{
	c.Endpoints.Configurator = ep =>
	{
		ep.PreProcessor<LimitIpAddressPreProcessor>(Order.Before);
		ep.PreProcessor<ApiIdHeaderPreProcessor>(Order.Before);
	};
});

if (app.Environment.IsDevelopment())
{
	app.UseSwaggerGen();
}

var dbFeeder = app.Services.GetRequiredService<ApplicationDbFeeder>();
await dbFeeder.MigrateAsync();

app.Run();