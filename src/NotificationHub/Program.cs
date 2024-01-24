using FastEndpoints;
using FastEndpoints.Swagger;

var builder = WebApplication.CreateBuilder(args);

builder.Services
	.AddFastEndpoints()
	.SwaggerDocument()
	.AddCors();

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

app.UseFastEndpoints();

if (app.Environment.IsDevelopment())
{
	app.UseSwaggerGen();
}

app.Run();