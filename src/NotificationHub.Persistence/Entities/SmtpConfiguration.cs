using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NotificationHub.Persistence.Entities;

public class SmtpConfiguration
{
	public SmtpConfiguration(string host, string userName, string password, int port, bool enableSsl)
	{
		Id = Guid.NewGuid();
		Host = host;
		UserName = userName;
		Password = password;
		Port = port;
		EnableSsl = enableSsl;
	}
	
	public Guid Id { get; set; }
	
	public string Host { get; set; }
	
	public int Port { get; set; }
	
	public bool EnableSsl { get; set; }
	
	public string UserName { get; set; }
	
	public string Password { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	
	public Application? Application { get; set; }
}

public class SmtpConfigurationConfiguration : IEntityTypeConfiguration<SmtpConfiguration>
{
	public void Configure(EntityTypeBuilder<SmtpConfiguration> builder)
	{
		builder.HasKey(x => x.Id);
		builder.Property(x => x.Host)
			.IsRequired();
		builder.Property(x => x.UserName)
			.IsRequired();
		builder.Property(x => x.Password)
			.IsRequired();
		builder.Property(x => x.CreatedAt)
			.IsRequired();
	}
}