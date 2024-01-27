using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace NotificationHub.Persistence;

public class ApplicationDbFeeder
{
	private readonly ApplicationDbContext _dbContext;
	
	public ApplicationDbFeeder(IConfiguration configuration)
	{
		_dbContext = new ApplicationDbContext(configuration);
	}
	
	public async Task MigrateAsync(CancellationToken ct = default)
	{
		await _dbContext.Database.MigrateAsync(ct);
	}
}