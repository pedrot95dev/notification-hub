using Microsoft.EntityFrameworkCore;
using NotificationHub.Persistence;

namespace NotificationHub.Security;

public sealed class SmtpPasswordEncryptor
{
	private readonly ApplicationDbContext _dbContext;
	private readonly ISmtpPasswordProtector _protector;

	public SmtpPasswordEncryptor(ApplicationDbContext dbContext, ISmtpPasswordProtector protector)
	{
		_dbContext = dbContext;
		_protector = protector;
	}

	public async Task<int> EncryptExistingAsync(CancellationToken ct = default)
	{
		var rows = await _dbContext.SmtpConfigurations.ToListAsync(ct);
		var changed = 0;
		foreach (var row in rows)
		{
			if (_protector.IsEncrypted(row.Password))
			{
				continue;
			}

			row.Password = _protector.Encrypt(row.Password);
			changed++;
		}

		if (changed > 0)
		{
			await _dbContext.SaveChangesAsync(ct);
		}

		return changed;
	}
}
