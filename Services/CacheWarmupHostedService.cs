namespace Berca_Backend.Services
{
    /// <summary>
    /// Hosted service that runs cache warmup on application startup
    /// Ensures critical caches are pre-populated before serving requests
    /// </summary>
    public class CacheWarmupHostedService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CacheWarmupHostedService> _logger;

        public CacheWarmupHostedService(
            IServiceProvider serviceProvider,
            ILogger<CacheWarmupHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("🚀 Application starting - initiating cache warmup...");

            try
            {
                // Run warmup asynchronously to not block application startup
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Add a small delay to let the application finish starting
                        await Task.Delay(2000, cancellationToken);

                        // Create scope inside the background task to avoid disposal issues
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var warmupService = scope.ServiceProvider.GetRequiredService<ICacheWarmupService>();
                            await warmupService.WarmupStartupCachesAsync();
                        }

                        _logger.LogInformation("🔥 Startup cache warmup completed successfully");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Startup cache warmup failed - application will continue without warmed cache");
                    }
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to initiate cache warmup");
                // Don't throw - let application continue starting
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("🛑 Application stopping - cache warmup service shutting down");
            return Task.CompletedTask;
        }
    }
}