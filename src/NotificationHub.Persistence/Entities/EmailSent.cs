using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NotificationHub.Persistence.Entities;

public class EmailSent
{
	public EmailSent(Guid applicationId, string email)
	{
		Id = Guid.NewGuid();
		ApplicationId = applicationId;
		Email = email;
	}
	
	public Guid Id { get; set; }
	
	public Guid ApplicationId { get; set; }

	public string Email { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	public Application? Application { get; set; }
}

public class EmailSentConfiguration : IEntityTypeConfiguration<EmailSent>
{
	public void Configure(EntityTypeBuilder<EmailSent> builder)
	{
		builder.HasKey(x => x.Id);
		builder.Property(x => x.Email)
			.IsRequired();
		builder.Property(x => x.CreatedAt)
			.IsRequired();
		builder.HasOne(x => x.Application)
			.WithMany(x => x.EmailSents)
			.HasForeignKey(x => x.ApplicationId)
			.OnDelete(DeleteBehavior.NoAction)
			.IsRequired();
	}
}