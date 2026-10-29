namespace NotificationHub.Services.EmailService;

public class EmailOptions
{
	public const string SectionName = "Email";

	public const string DefaultLocalDomain = "notification-hub.pedroteixeira.dev";

	public string LocalDomain { get; set; } = DefaultLocalDomain;
}
