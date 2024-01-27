using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.EntityFrameworkCore;
using NotificationHub.Persistence;
using NotificationHub.Pipeline;

var builder = WebApplication.CreateBuilder(args);

builder.Services
	.AddFastEndpoints()
	.SwaggerDocument()
	.AddCors();

builder.Services.AddScoped<ICurrentApplication, CurrentApplication>();

builder.Services.AddPersistence();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseCors(builder =>
{
	var origins = app.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? Array.Empty<string>();
	
	builder
		.WithOrigins(origins)
		.WithMethods("POST")
		.WithHeaders("x-app-id");
});

app.UseFastEndpoints(c =>
{
	c.Endpoints.Configurator = ep =>
	{
		ep.PreProcessor<DomainVerifierPreProcessor>(Order.Before);
		ep.PreProcessor<ApiIdHeaderPreProcessor>(Order.Before);
	};
});

if (app.Environment.IsDevelopment())
{
	app.UseSwaggerGen();
}

var dbContext = app.Services.GetRequiredService<ApplicationDbContext>();
await dbContext.Database.MigrateAsync();

app.Run();