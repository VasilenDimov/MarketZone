using MarketZone.Data;
using Microsoft.EntityFrameworkCore;

public class EmailVerificationCleanupService : BackgroundService
{
	private readonly IServiceScopeFactory _scopeFactory;

	public EmailVerificationCleanupService(IServiceScopeFactory scopeFactory)
	{
		_scopeFactory = scopeFactory;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			using var scope = _scopeFactory.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

			await context.EmailVerificationCodes
	              .Where(c => c.ExpiresAt < DateTime.UtcNow)
	              .ExecuteDeleteAsync(stoppingToken);

			// Run every 5 minutes
			await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
		}
	}
}
