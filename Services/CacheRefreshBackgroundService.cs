namespace Berca_Backend.Services
{
    /// <summary>
    /// Background service that periodically refreshes expensive caches
    /// Ensures expensive queries are always cached and fresh
    /// </summary>
    public class CacheRefreshBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CacheRefreshBackgroundService> _logger;
        private readonly TimeSpan _refreshInterval = TimeSpan.FromMinutes(10); // Refresh every 10 minutes

        public CacheRefreshBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<CacheRefreshBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🔄 Cache refresh background service started - refresh interval: {Interval} minutes", _refreshInterval.TotalMinutes);

            // Wait for initial warmup to complete
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var warmupService = scope.ServiceProvider.GetRequiredService<ICacheWarmupService>();

                        _logger.LogInformation("🔄 Starting background cache refresh...");

                        // Refresh critical caches in parallel
                        await Task.WhenAll(
                            RefreshDashboardCachesAsync(warmupService),
                            RefreshMLPredictionCachesAsync(warmupService)
                        );

                        _logger.LogInformation("🔄 Background cache refresh completed successfully");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Background cache refresh failed");
                }

                // Wait for next refresh interval
                await Task.Delay(_refreshInterval, stoppingToken);
            }

            _logger.LogInformation("🛑 Cache refresh background service stopped");
        }

        private async Task RefreshDashboardCachesAsync(ICacheWarmupService warmupService)
        {
            try
            {
                await warmupService.WarmupDashboardCachesAsync();
                _logger.LogDebug("🔄 Dashboard caches refreshed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to refresh dashboard caches");
            }
        }

        private async Task RefreshMLPredictionCachesAsync(ICacheWarmupService warmupService)
        {
            try
            {
                await warmupService.WarmupMLPredictionCachesAsync();
                _logger.LogDebug("🔄 ML prediction caches refreshed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to refresh ML prediction caches");
            }
        }
    }
}