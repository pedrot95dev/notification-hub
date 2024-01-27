using OneOf;
using OneOf.Types;
using Application = NotificationHub.Persistence.Entities.Application;
using SmtpConfiguration = NotificationHub.Persistence.Entities.SmtpConfiguration;

namespace NotificationHub.Services.CurrentApplication;

public interface ICurrentApplication
{
	OneOf<Application, NotFound> Application { get; }
	
	OneOf<SmtpConfiguration, NotFound> SmtpConfiguration { get; }
}