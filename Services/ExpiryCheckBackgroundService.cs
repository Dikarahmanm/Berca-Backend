using Berca_Backend.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

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
            _logger.LogInformation("🔍 Starting daily expiry check at {Time} (Jakarta: {JakartaTime})", 
                DateTime.UtcNow, ConvertToJakartaTime(DateTime.UtcNow));

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var expiryService = scope.ServiceProvider.GetRequiredService<IExpiryManagementService>();
                var notificationService = scope.ServiceProvider.GetService<IMultiBranchNotificationService>();

                var startTime = DateTime.UtcNow;

                // Perform comprehensive daily expiry check
                var checkResult = await expiryService.PerformDailyExpiryCheckAsync();
                
                var duration = DateTime.UtcNow - startTime;

                // Log detailed results
                _logger.LogInformation("✅ Daily expiry check completed in {Duration}ms", duration.TotalMilliseconds);
                _logger.LogInformation("📊 Check Results:");
                _logger.LogInformation("   • Newly expired batches: {Count}", checkResult.NewlyExpiredBatches);
                _logger.LogInformation("   • Notifications created: {Count}", checkResult.NotificationsCreated);
                _logger.LogInformation("   • Statuses updated: {Count}", checkResult.StatusesUpdated);
                _logger.LogInformation("   • Value at risk: Rp {Value:N0}", checkResult.ValueAtRisk);
                _logger.LogInformation("   • New value lost: Rp {Value:N0}", checkResult.NewValueLost);
                _logger.LogInformation("   • Critical items: {Count}", checkResult.CriticalItems.Count);

                // Send daily summary notification if there are critical items
                if (notificationService != null && (checkResult.CriticalItems.Any() || checkResult.NewlyExpiredBatches > 0))
                {
                    await SendDailySummaryNotification(checkResult, notificationService);
                }

                // Additional branch-specific checks for multi-branch system
                await PerformBranchSpecificChecks(expiryService, notificationService);

                _logger.LogInformation("🎯 Daily expiry check process completed successfully");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error during daily expiry check");
                
                // Send error notification to system admins
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var notificationService = scope.ServiceProvider.GetService<IMultiBranchNotificationService>();
                    
                    if (notificationService != null)
                    {
                        await notificationService.CreateSystemMaintenanceNotificationAsync(
                            DateTime.UtcNow, 
                            $"Daily Expiry Check Failed: {ex.Message}");
                    }
                }
                catch (Exception notificationEx)
                {
                    _logger.LogError(notificationEx, "Failed to send error notification");
                }
                
                throw;
            }
        }

        /// <summary>
        /// Send daily summary notification to administrators
        /// </summary>
        private async Task SendDailySummaryNotification(
            Interfaces.ExpiryCheckResultDto checkResult, 
            IMultiBranchNotificationService notificationService)
        {
            try
            {
                var jakartaTime = ConvertToJakartaTime(DateTime.UtcNow);
                
                var summaryMessage = $"""
                    📅 Daily Expiry Summary - {jakartaTime}
                    
                    🔴 Critical Items: {checkResult.CriticalItems.Count}
                    ⚠️ Newly Expired: {checkResult.NewlyExpiredBatches} batches
                    📨 Notifications Sent: {checkResult.NotificationsCreated}
                    💰 Value at Risk: Rp {checkResult.ValueAtRisk:N0}
                    📉 New Value Lost: Rp {checkResult.NewValueLost:N0}
                    
                    {(checkResult.CriticalItems.Any() ? "⚡ Immediate attention required for critical items!" : "✅ No immediate action required")}
                    """;

                await notificationService.BroadcastDailyExpirySummaryAsync(
                    checkResult.CriticalItems.Count,
                    checkResult.NewlyExpiredBatches,
                    checkResult.ValueAtRisk,
                    checkResult.NewValueLost);

                _logger.LogInformation("📬 Daily summary notification sent to administrators");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send daily summary notification");
            }
        }

        /// <summary>
        /// Perform branch-specific checks for multi-branch operations
        /// </summary>
        private async Task PerformBranchSpecificChecks(
            IExpiryManagementService expiryService,
            IMultiBranchNotificationService? notificationService)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<Data.AppDbContext>();
                
                var branches = await context.Branches
                    .Where(b => b.IsActive)
                    .ToListAsync();

                _logger.LogInformation("🏢 Performing branch-specific checks for {BranchCount} branches", branches.Count);

                foreach (var branch in branches)
                {
                    try
                    {
                        await PerformBranchExpiryCheck(branch.Id, branch.BranchName, expiryService, notificationService);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error performing expiry check for branch {BranchName} (ID: {BranchId})", 
                            branch.BranchName, branch.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during branch-specific checks");
            }
        }

        /// <summary>
        /// Perform expiry check for a specific branch
        /// </summary>
        private async Task PerformBranchExpiryCheck(
            int branchId, 
            string branchName, 
            IExpiryManagementService expiryService,
            IMultiBranchNotificationService? notificationService)
        {
            try
            {
                // Get branch-specific analytics
                var analytics = await expiryService.GetExpiryAnalyticsAsync(branchId);
                
                // Get products requiring immediate attention
                var criticalProducts = await expiryService.GetProductsRequiringNotificationAsync(branchId);
                var urgentProducts = criticalProducts.Where(p => p.DaysUntilExpiry <= 3).ToList();

                if (urgentProducts.Any() && notificationService != null)
                {
                    _logger.LogWarning("⚠️ Branch {BranchName}: {Count} products require urgent attention", 
                        branchName, urgentProducts.Count);

                    // Create branch-specific urgent notification
                    var urgentMessage = $"""
                        🚨 Urgent: Branch {branchName}
                        
                        {urgentProducts.Count} products require immediate attention:
                        
                        {string.Join("\n", urgentProducts.Take(5).Select(p => 
                            $"• {p.ProductName} (Batch: {p.BatchNumber}) - {p.DaysUntilExpiry} days left"))}
                        
                        {(urgentProducts.Count > 5 ? $"... and {urgentProducts.Count - 5} more items" : "")}
                        
                        Total value at risk: Rp {urgentProducts.Sum(p => p.ValueAtRisk):N0}
                        """;

                    // Create notifications for each urgent product
                    foreach (var urgentProduct in urgentProducts.Take(5)) // Limit to top 5 to avoid spam
                    {
                        await notificationService.CreateExpiryUrgentNotificationAsync(
                            urgentProduct.ProductId,
                            urgentProduct.ProductName,
                            urgentProduct.BatchNumber,
                            urgentProduct.ExpiryDate,
                            urgentProduct.CurrentStock,
                            branchId);
                    }
                }

                _logger.LogInformation("✅ Branch {BranchName}: Analytics - {ExpiringCount} expiring, {ExpiredCount} expired, Rp {ValueAtRisk:N0} at risk", 
                    branchName, analytics.ExpiringIn7Days, analytics.ExpiredProducts, analytics.ValueAtRisk);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking branch {BranchName} (ID: {BranchId})", branchName, branchId);
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

        /// <summary>
        /// Manual trigger for daily expiry check (for testing or emergency runs)
        /// </summary>
        public async Task TriggerManualCheck()
        {
            _logger.LogInformation("🔄 Manual expiry check triggered");
            await PerformExpiryCheck();
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