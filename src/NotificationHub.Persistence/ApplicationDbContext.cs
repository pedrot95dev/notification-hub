using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NotificationHub.Persistence.Entities;

namespace NotificationHub.Persistence;

public class ApplicationDbContext : DbContext
{
	private readonly IConfiguration _configuration;

	public ApplicationDbContext(IConfiguration configuration)
	{
		_configuration = configuration;
	}
	
	internal ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
	{
	}

	public DbSet<Application> Applications { get; set; }

	public DbSet<EmailSent> EmailSents { get; set; }
	
	public DbSet<SmtpConfiguration> SmtpConfigurations { get; set; }
	
	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		if (optionsBuilder.IsConfigured)
		{
			return;
		}
		
		optionsBuilder.UseNpgsql(
			_configuration.GetConnectionString("ApplicationDbConnection"),
			b => { b.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName); }
		);
		
		base.OnConfiguring(optionsBuilder);
	}
	
	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
		
		base.OnModelCreating(modelBuilder);
	}
}