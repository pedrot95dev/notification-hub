using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NotificationHub.Persistence;
using NotificationHub.Persistence.Entities;
using NotificationHub.Services.EmailService;

namespace NotificationHub.Tests;

public sealed class NotificationHubApiFactory : WebApplicationFactory<Program>
{
	private readonly Dictionary<string, string?> _settings;

	public FakeEmailService Email { get; } = new();

	public Func<IPAddress> Peer { get; set; } = () => IPAddress.Loopback;

	public NotificationHubApiFactory(Dictionary<string, string?>? settings = null)
	{
		_settings = settings ?? new Dictionary<string, string?>
		{
			["RateLimit:MaxRequests"] = "100",
			["RateLimit:WindowMinutes"] = "30",
			["RateLimit:TrustedProxies:0"] = "127.0.0.1"
		};
	}

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.UseEnvironment("Testing");
		builder.ConfigureAppConfiguration((_, config) =>
		{
			config.AddInMemoryCollection(_settings);
		});

		builder.ConfigureTestServices(services =>
		{
			services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
			services.RemoveAll<DbContextOptions>();
			services.RemoveAll<ApplicationDbContext>();

			var databaseName = Guid.NewGuid().ToString();
			services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(databaseName));

			services.RemoveAll<IEmailService>();
			services.AddSingleton(Email);
			services.AddSingleton<IEmailService>(sp => sp.GetRequiredService<FakeEmailService>());

			services.AddSingleton<IStartupFilter>(new PeerAddressStartupFilter(this));
		});
	}

	public async Task SeedApplicationAsync(string externalId, string domain = "dconstructions.ch")
	{
		using var scope = Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var smtp = new SmtpConfiguration("smtp.example.com", "mailbox@example.com", "plain-secret", 587, true);
		db.SmtpConfigurations.Add(smtp);
		await db.SaveChangesAsync();

		var application = new Application(externalId, domain, smtp.Id)
		{
			EmailDestination = "inbox@example.com"
		};
		db.Applications.Add(application);
		await db.SaveChangesAsync();
	}

	private sealed class PeerAddressStartupFilter : IStartupFilter
	{
		private readonly NotificationHubApiFactory _factory;

		public PeerAddressStartupFilter(NotificationHubApiFactory factory)
		{
			_factory = factory;
		}

		public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
		{
			return app =>
			{
				app.Use(async (context, nextMiddleware) =>
				{
					context.Connection.RemoteIpAddress = _factory.Peer();
					await nextMiddleware(context);
				});
				next(app);
			};
		}
	}
}
