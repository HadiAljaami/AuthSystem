using AuthSystem.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuthSystem.Api.Infrastructure.Services
{
    public class PasswordResetTokenCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PasswordResetTokenCleanupService> _logger;

        public PasswordResetTokenCleanupService(IServiceScopeFactory scopeFactory, ILogger<PasswordResetTokenCleanupService> logger)
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
                    _logger.LogInformation("Password reset token cleanup started.");

                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var now = DateTime.UtcNow;

                    var deletedCount = await db.passwordResetTokens
                        .Where(t =>
                            t.ExpiresAt < now ||
                            (t.IsUsed && t.CreatedAt < now.AddDays(-1))
                        )
                        .ExecuteDeleteAsync(stoppingToken);

                    _logger.LogInformation(
                        "Password reset cleanup finished. Deleted {DeletedCount} tokens.",
                        deletedCount
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during password reset token cleanup.");
                }

                await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
            }
        }

    }

}
