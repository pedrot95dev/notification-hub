using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NotificationHub.Persistence.Entities;

public class Application
{
	public Application(string domain, Guid smtpConfigurationId)
	{
		Id = Guid.NewGuid();
		Domain = domain;
		SmtpConfigurationId = smtpConfigurationId;
	}
	
	public Application(string domain, SmtpConfiguration smtpConfiguration)
	{
		Id = Guid.NewGuid();
		Domain = domain;
		SmtpConfiguration = smtpConfiguration;
	}
		
	public Guid Id { get; set; }
	
	public Guid SmtpConfigurationId { get; set; }
	
	public string Domain { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	
	public ICollection<EmailSent>? EmailSents { get; set; }
	
	public SmtpConfiguration? SmtpConfiguration { get; set; }
}

public class ApplicationConfiguration : IEntityTypeConfiguration<Application>
{
	public void Configure(EntityTypeBuilder<Application> builder)
	{
		builder.HasKey(x => x.Id);
		builder.Property(x => x.Domain)
			.IsRequired();
		builder.Property(x => x.CreatedAt)
			.IsRequired();
		builder.HasOne(x => x.SmtpConfiguration)
			.WithOne(x => x.Application)
			.HasForeignKey<Application>(x => x.SmtpConfigurationId)
			.OnDelete(DeleteBehavior.NoAction)
			.IsRequired();
	}
}