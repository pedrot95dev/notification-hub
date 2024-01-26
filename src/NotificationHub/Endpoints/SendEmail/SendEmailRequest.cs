namespace NotificationHub.Endpoints.SendEmail;

public class SendEmailRequest
{
	public string Subject { get; set; }
	public string Message { get; set; }
	public string ReplayToEmail { get; set; }
}