namespace NotificationHub.Endpoints.SendEmail;

public class SendEmailRequest
{
	public string Message { get; set; }
	public string ReplayToEmail { get; set; }
}