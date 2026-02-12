using AuthSystem.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuthSystem.Api.Infrastructure.Services
{
    public class RefreshTokenCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RefreshTokenCleanupService> _logger;

        public RefreshTokenCleanupService(IServiceScopeFactory scopeFactory, ILogger<RefreshTokenCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Starting cleanup process for old refresh tokens...");

                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var now = DateTime.UtcNow;

                    // Delete revoked tokens older than 1 day OR expired tokens
                    var deletedCount = await db.RefreshTokens
                        .Where(t =>
                            (t.IsRevoked && t.RevokedAt < now.AddDays(-1)) ||
                            t.ExpiresAt < now)
                        .ExecuteDeleteAsync(stoppingToken);

                    if (deletedCount > 0)
                    {
                        _logger.LogInformation($"Successfully deleted {deletedCount} old refresh tokens.");
                    }
                    else
                    {
                        _logger.LogInformation("No old refresh tokens found for deletion.");
                    }
                }

                catch (Exception ex)
                {
                    
                    _logger.LogError(ex, "An error occurred during token cleanup.");
                }

                // Wait 24 hours before the next cleanup cycle
                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
        }
    }
}
