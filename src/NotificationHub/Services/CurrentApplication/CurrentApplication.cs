using Microsoft.EntityFrameworkCore;
using NotificationHub.Persistence;
using NotificationHub.Persistence.Entities;
using NotificationHub.Pipeline;
using OneOf;
using OneOf.Types;

namespace NotificationHub.Services.CurrentApplication;

public class CurrentApplication : ICurrentApplication
{
	private readonly ApplicationDbContext _dbContext;
	private readonly IHttpContextAccessor _httpContextAccessor;
	
	private Application? _application;
	
	private SmtpConfiguration? _smtpConfiguration;
	
	public CurrentApplication(ApplicationDbContext dbContext,
		IHttpContextAccessor httpContextAccessor)
	{
		_dbContext = dbContext;
		_httpContextAccessor = httpContextAccessor;
	}

	public OneOf<Application, NotFound> Application => GetCurrentApplication();
	
	public OneOf<SmtpConfiguration, NotFound> SmtpConfiguration => GetApplicationSmtpConfiguration();

	private OneOf<Application, NotFound> GetCurrentApplication()
	{
		var appId = _httpContextAccessor.HttpContext!.Request.Headers[ApiIdHeaderPreProcessor.AppIdHeader];
		
		_application ??= _dbContext.Applications
			.AsNoTracking()
			.FirstOrDefault(x => x.Id == appId);
		
		return _application is null 
			? new NotFound() 
			: _application;
	}
	
	private OneOf<SmtpConfiguration, NotFound> GetApplicationSmtpConfiguration()
	{
		var applicationResult = Application;

		if (applicationResult.IsT1)
		{
			return applicationResult.AsT1;
		}
		
		var application = applicationResult.AsT0;
		
		_smtpConfiguration ??= _dbContext.Applications
			.AsNoTracking()
			.Where(x => x.Id == application.Id)
			.Select(x => x.SmtpConfiguration)
			.FirstOrDefault();
					
		return _smtpConfiguration is null 
			? new NotFound() 
			: _smtpConfiguration!;
	}
}