using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NotificationHub.Persistence;
using NotificationHub.Persistence.Entities;
using NotificationHub.Security;
using Xunit;

namespace NotificationHub.Tests;

public class SmtpPasswordProtectorTests
{
	[Fact]
	public void Legacy_plaintext_is_returned_and_round_trips_through_the_key_ring()
	{
		var protector = CreateProtector();

		Assert.False(protector.IsEncrypted("plain-secret"));
		Assert.Equal("plain-secret", protector.Decrypt("plain-secret"));

		var ciphertext = protector.Encrypt("plain-secret");
		Assert.StartsWith(SmtpPasswordProtector.PayloadPrefix, ciphertext);
		Assert.NotEqual("plain-secret", ciphertext);
		Assert.Equal(ciphertext, protector.Encrypt(ciphertext));
		Assert.Equal("plain-secret", protector.Decrypt(ciphertext));
	}

	[Fact]
	public async Task Encrypt_existing_rows_is_idempotent()
	{
		var databaseName = Guid.NewGuid().ToString();
		var options = new DbContextOptionsBuilder<ApplicationDbContext>()
			.UseInMemoryDatabase(databaseName)
			.Options;
		await using var db = new ApplicationDbContext(options);
		db.SmtpConfigurations.Add(new SmtpConfiguration("smtp.example.com", "mailbox@example.com", "plain-secret", 587, true));
		await db.SaveChangesAsync();

		var protector = CreateProtector();
		var encryptor = new SmtpPasswordEncryptor(db, protector);

		Assert.Equal(1, await encryptor.EncryptExistingAsync());
		var stored = await db.SmtpConfigurations.Select(x => x.Password).SingleAsync();
		Assert.StartsWith(SmtpPasswordProtector.PayloadPrefix, stored);
		Assert.Equal("plain-secret", protector.Decrypt(stored));

		Assert.Equal(0, await encryptor.EncryptExistingAsync());
		Assert.Equal(stored, await db.SmtpConfigurations.Select(x => x.Password).SingleAsync());
	}

	[Fact]
	public void Encrypt_flag_is_read_from_the_argument_or_configuration()
	{
		var enabled = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				[SmtpPasswordEncryption.FlagKey] = "true"
			})
			.Build();
		var disabled = new ConfigurationBuilder().Build();

		Assert.True(SmtpPasswordEncryption.ShouldRun([], enabled));
		Assert.False(SmtpPasswordEncryption.ShouldRun([], disabled));
		Assert.True(SmtpPasswordEncryption.ShouldRun([SmtpPasswordEncryption.Argument], disabled));
	}

	private static SmtpPasswordProtector CreateProtector()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddDataProtection()
			.SetApplicationName(SmtpPasswordEncryption.ApplicationName)
			.PersistKeysToFileSystem(Directory.CreateTempSubdirectory("notification-hub-keys"));
		var provider = services.BuildServiceProvider();
		return new SmtpPasswordProtector(
			provider.GetRequiredService<IDataProtectionProvider>(),
			provider.GetRequiredService<ILogger<SmtpPasswordProtector>>());
	}
}
