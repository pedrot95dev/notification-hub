namespace NotificationHub.Services.EmailService;

public readonly record struct EmailSendResult(bool Succeeded, string? Error)
{
	public static EmailSendResult Ok() => new(true, null);

	public static EmailSendResult Fail(string? error) => new(false, error);
}
