using Microsoft.Extensions.DependencyInjection;

namespace NotificationHub.Persistence;

public static class DependencyInjection
{
	public static void AddPersistence(this IServiceCollection services)
	{
		services.AddDbContext<ApplicationDbContext>();
		services.AddTransient<ApplicationDbFeeder>();
	}
}