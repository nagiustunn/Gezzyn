using gezzyn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace gezzyn.API.BackgroundServices
{
    public class RefreshTokenCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RefreshTokenCleanupService> _logger;

        private readonly TimeSpan _interval = TimeSpan.FromHours(24);

        public RefreshTokenCleanupService(
            IServiceScopeFactory scopeFactory,
            ILogger<RefreshTokenCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("RefreshToken temizleme servisi başladı.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupExpiredTokensAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "RefreshToken temizleme sırasında hata oluştu.");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }

        private async Task CleanupExpiredTokensAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var cutoff = DateTime.UtcNow;
            var deleted = await db.RefreshTokens
                .Where(r => r.ExpiresAt < cutoff || r.RevokedAt != null)
                .ExecuteDeleteAsync(ct);  

            if (deleted > 0)
                _logger.LogInformation(
                    "RefreshToken temizleme: {Count} süresi dolmuş token silindi.", deleted);
        }
    }
}
