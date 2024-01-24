namespace NotificationHub.Configuration;

public class ApplicationsConfiguration
{
	public IEnumerable<Application> Applications { get; set; } = Enumerable.Empty<Application>();
}

public class Application
{
	public string Id { get; set; } = null!;
	
	public string Domain { get; set; } = null!;
}