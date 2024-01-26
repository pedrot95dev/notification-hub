using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace NotificationHub.Persistence;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
	public ApplicationDbContext CreateDbContext(string[] args)
	{
		var configuration = new ConfigurationBuilder()
			.SetBasePath(Directory.GetCurrentDirectory())
			.AddJsonFile("local.config.json")
			.Build();
		
		var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
		optionsBuilder.UseNpgsql(
			configuration.GetConnectionString("ApplicationDbConnection"),
			b => { b.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName); } 
		);

		return new ApplicationDbContext(optionsBuilder.Options);
	}
}