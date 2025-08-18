using Berca_Backend.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Berca_Backend.Services
{
    /// <summary>
    /// Background service for daily expiry checks
    /// Runs at 6 AM Jakarta time every day to check for expired products,
    /// create notifications, and update expiry statuses
    /// </summary>
    public class ExpiryCheckBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ExpiryCheckBackgroundService> _logger;

        public ExpiryCheckBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<ExpiryCheckBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🕕 ExpiryCheckBackgroundService started - Jakarta timezone expiry checking enabled");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var nextRunTime = CalculateNextRunTime();
                    var delay = nextRunTime - DateTime.UtcNow;

                    _logger.LogInformation("⏰ Next expiry check scheduled for: {NextRun} (Jakarta time: {JakartaTime})", 
                        nextRunTime, ConvertToJakartaTime(nextRunTime));

                    if (delay > TimeSpan.Zero)
                    {
                        await Task.Delay(delay, stoppingToken);
                    }

                    if (!stoppingToken.IsCancellationRequested)
                    {
                        await PerformExpiryCheck();
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("ExpiryCheckBackgroundService is stopping");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in ExpiryCheckBackgroundService execution");
                    // Wait 1 hour before retrying on error
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }
        }

        private async Task PerformExpiryCheck()
        {
            _logger.LogInformation("🔍 Starting daily expiry check at {Time}", DateTime.UtcNow);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var expiryService = scope.ServiceProvider.GetService<IExpiryManagementService>();
                var notificationService = scope.ServiceProvider.GetService<INotificationService>();
                var timezoneService = scope.ServiceProvider.GetService<ITimezoneService>();

                if (expiryService == null)
                {
                    _logger.LogWarning("⚠️ IExpiryManagementService not found - skipping expiry check");
                    return;
                }

                if (notificationService == null)
                {
                    _logger.LogWarning("⚠️ INotificationService not found - notifications will be skipped");
                }

                if (timezoneService == null)
                {
                    _logger.LogWarning("⚠️ ITimezoneService not found - using UTC time");
                }

                var jakartaTime = timezoneService?.Now ?? DateTime.UtcNow;
                _logger.LogInformation("🕕 Performing expiry check for Jakarta date: {JakartaDate}", jakartaTime.ToString("yyyy-MM-dd"));

                // 1. Update expiry statuses for all batches
                var statusUpdates = await expiryService.UpdateExpiryStatusesAsync();
                _logger.LogInformation("📊 Updated expiry status for {Count} batches", statusUpdates);

                // 2. Mark newly expired batches
                var newlyExpired = await expiryService.MarkBatchesAsExpiredAsync();
                _logger.LogInformation("⚠️ Marked {Count} batches as newly expired", newlyExpired);

                // 3. Perform comprehensive daily expiry check
                var checkResult = await expiryService.PerformDailyExpiryCheckAsync();
                _logger.LogInformation("✅ Daily expiry check completed: {NewlyExpired} newly expired, {Notifications} notifications created", 
                    checkResult.NewlyExpiredBatches, checkResult.NotificationsCreated);

                // 4. Create notifications if service available
                if (notificationService != null)
                {
                    var notificationCount = await expiryService.CreateExpiryNotificationsAsync();
                    _logger.LogInformation("📬 Created {Count} expiry notifications", notificationCount);

                    // 5. Create daily summary for managers
                    if (checkResult.CriticalItems.Any())
                    {
                        await notificationService.BroadcastDailyExpirySummaryAsync(
                            checkResult.CriticalItems.Count(i => i.ExpiryStatus == Models.ExpiryStatus.Warning || i.ExpiryStatus == Models.ExpiryStatus.Critical),
                            checkResult.NewlyExpiredBatches,
                            checkResult.ValueAtRisk,
                            checkResult.NewValueLost
                        );
                        _logger.LogInformation("📊 Broadcasted daily expiry summary to managers");
                    }
                }

                // 6. Log summary
                _logger.LogInformation("🎯 Expiry check summary - Newly expired: {NewlyExpired}, Value at risk: {ValueAtRisk:C}, Value lost: {ValueLost:C}",
                    checkResult.NewlyExpiredBatches, checkResult.ValueAtRisk, checkResult.NewValueLost);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error during daily expiry check");
                throw;
            }
        }

        private static DateTime CalculateNextRunTime()
        {
            // Calculate 6 AM Jakarta time for today or tomorrow
            var jakartaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); // UTC+7
            var jakartaNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, jakartaTimeZone);
            
            var targetTime = new DateTime(jakartaNow.Year, jakartaNow.Month, jakartaNow.Day, 6, 0, 0);
            
            // If it's already past 6 AM today, schedule for 6 AM tomorrow
            if (jakartaNow >= targetTime)
            {
                targetTime = targetTime.AddDays(1);
            }
            
            // Convert back to UTC
            return TimeZoneInfo.ConvertTimeToUtc(targetTime, jakartaTimeZone);
        }

        private static string ConvertToJakartaTime(DateTime utcTime)
        {
            try
            {
                var jakartaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                var jakartaTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, jakartaTimeZone);
                return jakartaTime.ToString("yyyy-MM-dd HH:mm:ss");
            }
            catch
            {
                return utcTime.ToString("yyyy-MM-dd HH:mm:ss UTC");
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🛑 ExpiryCheckBackgroundService is stopping");
            await base.StopAsync(stoppingToken);
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }

    /// <summary>
    /// Extension methods for registering the expiry background service
    /// </summary>
    public static class ExpiryBackgroundServiceExtensions
    {
        /// <summary>
        /// Add the expiry check background service to the service collection
        /// </summary>
        public static IServiceCollection AddExpiryBackgroundService(this IServiceCollection services)
        {
            services.AddHostedService<ExpiryCheckBackgroundService>();
            return services;
        }
    }
}