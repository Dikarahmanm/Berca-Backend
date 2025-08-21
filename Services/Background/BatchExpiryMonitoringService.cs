// Services/Background/BatchExpiryMonitoringService.cs - Background service for batch expiry monitoring
using Berca_Backend.Data;
using Berca_Backend.DTOs;
using Berca_Backend.Models;
using Berca_Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Berca_Backend.Services.Background
{
    /// <summary>
    /// Background service that monitors batch expiry dates and sends notifications
    /// Runs daily at 6 AM Indonesia time to check for expiring batches
    /// </summary>
    public class BatchExpiryMonitoringService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BatchExpiryMonitoringService> _logger;
        private readonly Timer _timer;
        
        public BatchExpiryMonitoringService(
            IServiceProvider serviceProvider,
            ILogger<BatchExpiryMonitoringService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            
            // Calculate time until next 6 AM Indonesia time
            var nextRun = GetNext6AM();
            var timeUntilNextRun = nextRun - DateTime.UtcNow;
            
            // Create timer that runs daily at 6 AM
            _timer = new Timer(ExecuteMonitoring, null, timeUntilNextRun, TimeSpan.FromDays(1));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("BatchExpiryMonitoringService started at: {time}", DateTimeOffset.Now);
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await DoWork();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while monitoring batch expiry");
                }

                // Wait 24 hours before next execution
                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
        }

        private async void ExecuteMonitoring(object? state)
        {
            try
            {
                await DoWork();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in scheduled batch expiry monitoring");
            }
        }

        private async Task DoWork()
        {
            _logger.LogInformation("Starting batch expiry monitoring at: {time}", DateTime.UtcNow);

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var timezoneService = scope.ServiceProvider.GetRequiredService<ITimezoneService>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            try
            {
                var today = timezoneService.Today;
                
                // Get batches expiring in the next 7 days
                var expiringBatches = await context.ProductBatches
                    .Include(b => b.Product)
                    .Include(b => b.Branch)
                    .Where(b => b.ExpiryDate.HasValue && 
                               b.ExpiryDate.Value.Date >= today &&
                               b.ExpiryDate.Value.Date <= today.AddDays(7) &&
                               b.CurrentStock > 0 &&
                               !b.IsDisposed &&
                               !b.IsBlocked)
                    .OrderBy(b => b.ExpiryDate)
                    .ToListAsync();

                if (expiringBatches.Any())
                {
                    // Group by urgency
                    var criticalBatches = expiringBatches.Where(b => b.DaysUntilExpiry <= 1).ToList();
                    var warningBatches = expiringBatches.Where(b => b.DaysUntilExpiry > 1 && b.DaysUntilExpiry <= 3).ToList();
                    var soonBatches = expiringBatches.Where(b => b.DaysUntilExpiry > 3 && b.DaysUntilExpiry <= 7).ToList();

                    // Log summary
                    _logger.LogInformation(
                        "Batch expiry monitoring found: {critical} critical, {warning} warning, {soon} expiring soon",
                        criticalBatches.Count, warningBatches.Count, soonBatches.Count);

                    // Send critical notifications (expires today or tomorrow)
                    if (criticalBatches.Any())
                    {
                        await SendExpiryNotifications(criticalBatches, "CRITICAL", notificationService);
                    }

                    // Send warning notifications (expires in 2-3 days)
                    if (warningBatches.Any())
                    {
                        await SendExpiryNotifications(warningBatches, "WARNING", notificationService);
                    }

                    // Send info notifications for batches expiring in 4-7 days (only on Mondays)
                    if (soonBatches.Any() && today.DayOfWeek == DayOfWeek.Monday)
                    {
                        await SendExpiryNotifications(soonBatches, "INFO", notificationService);
                    }

                    // Update batch expiry status
                    await UpdateBatchExpiryStatus(expiringBatches, today, context);
                    await context.SaveChangesAsync();
                }
                else
                {
                    _logger.LogInformation("No batches expiring in the next 7 days");
                }

                // Log information about expired batches (status is computed automatically)
                var expiredBatches = await context.ProductBatches
                    .Where(b => b.ExpiryDate.HasValue && 
                               b.ExpiryDate.Value.Date < today &&
                               !b.IsDisposed)
                    .ToListAsync();

                if (expiredBatches.Any())
                {
                    _logger.LogInformation("Found {count} expired batches that may need attention", expiredBatches.Count);
                    
                    // Update timestamps to indicate these batches were checked
                    foreach (var batch in expiredBatches)
                    {
                        batch.UpdatedAt = timezoneService.Now;
                    }
                    
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during batch expiry monitoring");
                throw;
            }

            _logger.LogInformation("Batch expiry monitoring completed at: {time}", DateTime.UtcNow);
        }

        private async Task SendExpiryNotifications(
            List<ProductBatch> batches, 
            string urgencyLevel, 
            INotificationService notificationService)
        {
            try
            {
                var message = urgencyLevel switch
                {
                    "CRITICAL" => $"🚨 CRITICAL: {batches.Count} batch(es) expire within 24 hours!",
                    "WARNING" => $"⚠️ WARNING: {batches.Count} batch(es) expire within 2-3 days",
                    "INFO" => $"ℹ️ INFO: {batches.Count} batch(es) expire within a week",
                    _ => $"Batch expiry notification: {batches.Count} batches"
                };

                var details = string.Join("\n", batches.Take(10).Select(b => 
                    $"• {b.Product?.Name ?? "Unknown"} (Batch: {b.BatchNumber}) - " +
                    $"Expires: {b.ExpiryDate:yyyy-MM-dd} ({b.DaysUntilExpiry} days)"));

                if (batches.Count > 10)
                {
                    details += $"\n... and {batches.Count - 10} more batches";
                }

                var priority = urgencyLevel switch
                {
                    "CRITICAL" => "Critical",
                    "WARNING" => "High", 
                    _ => "Normal"
                };

                await notificationService.CreateSystemNotificationAsync(new CreateNotificationDto
                {
                    Title = "Batch Expiry Alert",
                    Message = $"{message}\n\n{details}",
                    Priority = priority,
                    Type = "BatchExpiry",
                    ActionUrl = "/inventory/batches"
                }, "System");

                _logger.LogInformation("Sent {urgency} expiry notification for {count} batches", 
                    urgencyLevel, batches.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending expiry notifications for {urgency} batches", urgencyLevel);
            }
        }

        private Task UpdateBatchExpiryStatus(
            List<ProductBatch> batches, 
            DateTime today, 
            AppDbContext context)
        {
            // Note: ExpiryStatus is a computed property based on DaysUntilExpiry
            // We don't need to manually update it as it's calculated automatically
            // Just update the UpdatedAt timestamp to indicate the batch was checked
            foreach (var batch in batches)
            {
                batch.UpdatedAt = DateTime.UtcNow;
                _logger.LogDebug("Checked batch {batchNumber} with status {status}",
                    batch.BatchNumber, batch.ExpiryStatus);
            }
            
            return Task.CompletedTask;
        }

        private static DateTime GetNext6AM()
        {
            // Get current time in Indonesia (UTC+7)
            var jakartaTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, 
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            
            var next6AM = jakartaTime.Date.AddHours(6);
            
            // If it's already past 6 AM today, schedule for tomorrow
            if (jakartaTime >= next6AM)
            {
                next6AM = next6AM.AddDays(1);
            }
            
            // Convert back to UTC
            return TimeZoneInfo.ConvertTimeToUtc(next6AM, 
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
        }

        public override void Dispose()
        {
            _timer?.Dispose();
            base.Dispose();
        }
    }
}