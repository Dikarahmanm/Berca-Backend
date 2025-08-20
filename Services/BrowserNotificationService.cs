using Berca_Backend.Data;
using Berca_Backend.DTOs;
using Berca_Backend.Models;
using Berca_Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Berca_Backend.Services
{
    /// <summary>
    /// Enhanced background service for browser notifications
    /// Handles tab notifications with favicon badges and priority-based delivery
    /// Indonesian business context with POS-specific notification types
    /// </summary>
    public class BrowserNotificationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BrowserNotificationService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5); // Check every 5 minutes
        private readonly TimeSpan _retryInterval = TimeSpan.FromHours(1); // Retry failed notifications every hour

        public BrowserNotificationService(
            IServiceProvider serviceProvider,
            ILogger<BrowserNotificationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Browser Notification Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessBrowserNotifications(stoppingToken);
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Browser Notification Service is stopping");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Browser Notification Service");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
        }

        /// <summary>
        /// Process browser notifications including priority-based delivery and favicon badges
        /// </summary>
        private async Task ProcessBrowserNotifications(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var pushNotificationService = scope.ServiceProvider.GetService<IPushNotificationService>();

            if (pushNotificationService == null)
            {
                _logger.LogWarning("PushNotificationService not available");
                return;
            }

            try
            {
                // Task 1: Process critical notifications first
                await ProcessCriticalNotifications(context, pushNotificationService, cancellationToken);

                // Task 2: Process high priority notifications
                await ProcessHighPriorityNotifications(context, pushNotificationService, cancellationToken);

                // Task 3: Process normal priority notifications
                await ProcessNormalPriorityNotifications(context, pushNotificationService, cancellationToken);

                // Task 4: Retry failed notifications
                await RetryFailedNotifications(pushNotificationService, cancellationToken);

                // Task 5: Update favicon badges based on notification counts
                await UpdateFaviconBadges(context, pushNotificationService, cancellationToken);

                // Task 6: Cleanup old notification logs
                await CleanupOldNotificationLogs(context, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing browser notifications");
            }
        }

        /// <summary>
        /// Process critical notifications immediately (product expiry, system alerts)
        /// </summary>
        private async Task ProcessCriticalNotifications(
            AppDbContext context, 
            IPushNotificationService pushService, 
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Processing critical notifications");

                // Check for products expiring today (via ProductBatch)
                var today = DateTime.Today;
                var productsExpiringToday = await context.ProductBatches
                    .Include(pb => pb.Product)
                    .Where(pb => pb.ExpiryDate.HasValue && 
                               pb.ExpiryDate.Value.Date == today &&
                               pb.CurrentStock > 0)
                    .Select(pb => new { Product = pb.Product, Batch = pb })
                    .ToListAsync(cancellationToken);

                foreach (var item in productsExpiringToday)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    try
                    {
                        var payload = new NotificationPayload
                        {
                            Title = "⚠️ PRODUK KADALUARSA HARI INI!",
                            Body = $"{item.Product?.Name ?? "Unknown Product"} batch akan kadaluarsa hari ini. Qty: {item.Batch?.CurrentStock ?? 0}",
                            Icon = "/icons/warning-icon.png",
                            Badge = "/icons/badge-critical.png",
                            Tag = $"expiry-critical-{item.Product?.Id ?? 0}-{item.Batch?.Id ?? 0}",
                            RequireInteraction = true,
                            Priority = NotificationPriority.Critical,
                            Data = new Dictionary<string, object>
                            {
                                { "type", "product-expiry-critical" },
                                { "productId", item.Product?.Id ?? 0 },
                                { "batchId", item.Batch?.Id ?? 0 },
                                { "expiryDate", (item.Batch?.ExpiryDate ?? DateTime.MinValue).ToString("yyyy-MM-dd") },
                                { "quantity", item.Batch?.CurrentStock ?? 0 }
                            },
                            Actions = new List<NotificationActionDto>
                            {
                                new() { Action = "view-product", Title = "Lihat Produk", Icon = "/icons/view.png" },
                                new() { Action = "mark-discount", Title = "Beri Diskon", Icon = "/icons/discount.png" }
                            }
                        };

                        // Send to Manager and Admin roles
                        var result = await pushService.SendToRolesAsync(
                            new List<string> { "Manager", "Admin" }, 
                            payload);

                        if (result.Success)
                        {
                            _logger.LogInformation("Critical expiry notification sent for product {ProductName} batch {BatchId}", 
                                item.Product?.Name ?? "Unknown Product", item.Batch?.Id ?? 0);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to send critical expiry notification for product {ProductId} batch {BatchId}", 
                                item.Product?.Id ?? 0, item.Batch?.Id ?? 0);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending critical notification for product {ProductId} batch {BatchId}", 
                            item.Product?.Id ?? 0, item.Batch?.Id ?? 0);
                    }

                    // Small delay to prevent overwhelming
                    await Task.Delay(200, cancellationToken);
                }

                // Check for overdue factures (critical business impact)
                var overdueFactures = await context.Factures
                    .Include(f => f.Supplier)
                    .Where(f => f.DueDate.Date < today &&
                               f.Status != Models.FactureStatus.Paid &&
                               f.Status != Models.FactureStatus.Cancelled)
                    .OrderByDescending(f => f.TotalAmount - f.PaidAmount)
                    .Take(5) // Limit to top 5 critical overdue factures
                    .ToListAsync(cancellationToken);

                foreach (var facture in overdueFactures)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    try
                    {
                        var daysOverdue = (today - facture.DueDate.Date).Days;
                        var outstandingAmount = facture.TotalAmount - facture.PaidAmount;
                        var urgencyLevel = daysOverdue switch
                        {
                            <= 7 => "🔶",
                            <= 30 => "🟠", 
                            _ => "🔴"
                        };

                        var payload = new NotificationPayload
                        {
                            Title = $"{urgencyLevel} FAKTUR TERLAMBAT {daysOverdue} HARI",
                            Body = $"{facture.Supplier?.CompanyName ?? "Unknown"}: {outstandingAmount:C0}",
                            Icon = "/icons/facture-overdue.png",
                            Badge = "/icons/badge-critical.png",
                            Tag = $"facture-overdue-{facture.Id}",
                            RequireInteraction = true,
                            Priority = NotificationPriority.Critical,
                            Data = new Dictionary<string, object>
                            {
                                { "type", "facture-overdue-critical" },
                                { "factureId", facture.Id },
                                { "daysOverdue", daysOverdue },
                                { "outstandingAmount", outstandingAmount },
                                { "supplierId", facture.SupplierId }
                            },
                            Actions = new List<NotificationActionDto>
                            {
                                new() { Action = "view-facture", Title = "Lihat Faktur", Icon = "/icons/view.png" },
                                new() { Action = "schedule-payment", Title = "Jadwalkan Bayar", Icon = "/icons/payment.png" }
                            }
                        };

                        // Send to Admin and Manager roles
                        var result = await pushService.SendToRolesAsync(
                            new List<string> { "Admin", "Manager" }, 
                            payload, 
                            facture.BranchId);

                        if (result.Success)
                        {
                            _logger.LogInformation("Critical overdue facture notification sent for facture {FactureNumber}", 
                                facture.SupplierInvoiceNumber);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending critical facture notification for facture {FactureId}", facture.Id);
                    }

                    await Task.Delay(300, cancellationToken);
                }

                _logger.LogDebug("Critical notifications processing completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in critical notifications processing");
            }
        }

        /// <summary>
        /// Process high priority notifications (low stock, member credit alerts)
        /// </summary>
        private async Task ProcessHighPriorityNotifications(
            AppDbContext context, 
            IPushNotificationService pushService, 
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Processing high priority notifications");

                // Check for low stock items
                var lowStockProducts = await context.Products
                    .Where(p => p.Stock <= p.MinimumStock && p.MinimumStock > 0)
                    .OrderBy(p => p.Stock)
                    .Take(10) // Limit to top 10 most critical
                    .ToListAsync(cancellationToken);

                foreach (var product in lowStockProducts)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    try
                    {
                        var stockPercentage = product.MinimumStock > 0 ? 
                            (double)product.Stock / product.MinimumStock * 100 : 0;
                        
                        var urgencyIcon = stockPercentage switch
                        {
                            <= 25 => "🔴",
                            <= 50 => "🟠",
                            _ => "🟡"
                        };

                        var payload = new NotificationPayload
                        {
                            Title = $"{urgencyIcon} STOK MENIPIS",
                            Body = $"{product.Name}: {product.Stock} tersisa (min: {product.MinimumStock})",
                            Icon = "/icons/low-stock.png",
                            Badge = "/icons/badge-warning.png",
                            Tag = $"low-stock-{product.Id}",
                            Priority = NotificationPriority.High,
                            Data = new Dictionary<string, object>
                            {
                                { "type", "low-stock" },
                                { "productId", product.Id },
                                { "currentStock", product.Stock },
                                { "minimumStock", product.MinimumStock }
                            },
                            Actions = new List<NotificationActionDto>
                            {
                                new() { Action = "reorder", Title = "Pesan Ulang", Icon = "/icons/reorder.png" },
                                new() { Action = "view-product", Title = "Lihat Detail", Icon = "/icons/view.png" }
                            }
                        };

                        // Send to branch staff and managers (without branchId for now)
                        var result = await pushService.SendToRolesAsync(
                            new List<string> { "Manager", "User" }, 
                            payload);

                        if (result.Success)
                        {
                            _logger.LogInformation("Low stock notification sent for product {ProductName} (Stock: {Stock})", 
                                product.Name, product.Stock);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending low stock notification for product {ProductId}", product.Id);
                    }

                    await Task.Delay(150, cancellationToken);
                }

                _logger.LogDebug("High priority notifications processing completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in high priority notifications processing");
            }
        }

        /// <summary>
        /// Process normal priority notifications (daily summaries, reminders)
        /// </summary>
        private async Task ProcessNormalPriorityNotifications(
            AppDbContext context, 
            IPushNotificationService pushService, 
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Processing normal priority notifications");

                // Send daily summary notifications (once per day around 8 PM)
                var now = DateTime.Now;
                var isEveningTime = now.Hour == 20 && now.Minute < 5; // 8 PM window

                if (isEveningTime)
                {
                    await SendDailySummaryNotifications(context, pushService, cancellationToken);
                }

                // Send weekly reminders (Sundays at 7 PM)
                var isSundayEvening = now.DayOfWeek == DayOfWeek.Sunday && now.Hour == 19 && now.Minute < 5;
                
                if (isSundayEvening)
                {
                    await SendWeeklyReminderNotifications(context, pushService, cancellationToken);
                }

                _logger.LogDebug("Normal priority notifications processing completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in normal priority notifications processing");
            }
        }

        /// <summary>
        /// Send daily summary notifications to managers
        /// </summary>
        private async Task SendDailySummaryNotifications(
            AppDbContext context, 
            IPushNotificationService pushService, 
            CancellationToken cancellationToken)
        {
            try
            {
                var today = DateTime.Today;
                
                // Get today's sales summary (Sales model doesn't have BranchId, using UserId for now)
                var todaySales = await context.Sales
                    .Where(s => s.SaleDate.Date == today)
                    .GroupBy(s => 1) // Group all sales together for now
                    .Select(g => new 
                    {
                        BranchId = (int?)null, // Placeholder until Sales model is updated
                        TotalSales = g.Sum(s => s.Total),
                        TransactionCount = g.Count()
                    })
                    .ToListAsync(cancellationToken);

                foreach (var branchSummary in todaySales)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    var payload = new NotificationPayload
                    {
                        Title = "📊 RINGKASAN PENJUALAN HARI INI",
                        Body = $"Total: {branchSummary.TotalSales:C0} dari {branchSummary.TransactionCount} transaksi",
                        Icon = "/icons/daily-summary.png",
                        Badge = "/icons/badge-info.png",
                        Tag = $"daily-summary-{today:yyyyMMdd}",
                        Priority = NotificationPriority.Normal,
                        Data = new Dictionary<string, object>
                        {
                            { "type", "daily-summary" },
                            { "date", today.ToString("yyyy-MM-dd") },
                            { "totalSales", branchSummary.TotalSales },
                            { "transactionCount", branchSummary.TransactionCount },
                            { "branchId", branchSummary.BranchId ?? 0 }
                        }
                    };

                    var result = await pushService.SendToRolesAsync(
                        new List<string> { "Manager", "Admin" }, 
                        payload, 
                        branchSummary.BranchId);

                    if (result.Success)
                    {
                        _logger.LogInformation("Daily summary notification sent for branch {BranchId}", branchSummary.BranchId);
                    }

                    await Task.Delay(100, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending daily summary notifications");
            }
        }

        /// <summary>
        /// Send weekly reminder notifications
        /// </summary>
        private async Task SendWeeklyReminderNotifications(
            AppDbContext context, 
            IPushNotificationService pushService, 
            CancellationToken cancellationToken)
        {
            try
            {
                var payload = new NotificationPayload
                {
                    Title = "📅 PENGINGAT MINGGUAN",
                    Body = "Jangan lupa cek stok produk dan pembayaran faktur untuk minggu depan!",
                    Icon = "/icons/weekly-reminder.png",
                    Badge = "/icons/badge-info.png",
                    Tag = $"weekly-reminder-{DateTime.Now:yyyyMMdd}",
                    Priority = NotificationPriority.Normal,
                    Data = new Dictionary<string, object>
                    {
                        { "type", "weekly-reminder" },
                        { "week", DateTime.Now.ToString("yyyy-WW") }
                    }
                };

                var result = await pushService.SendToRolesAsync(
                    new List<string> { "Manager", "Admin" }, 
                    payload);

                if (result.Success)
                {
                    _logger.LogInformation("Weekly reminder notification sent");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending weekly reminder notifications");
            }
        }

        /// <summary>
        /// Retry failed notifications
        /// </summary>
        private async Task RetryFailedNotifications(
            IPushNotificationService pushService, 
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Starting failed notification retry process");

                var failedNotifications = await pushService.GetFailedNotificationsForRetryAsync(_retryInterval);
                
                if (!failedNotifications.Any())
                {
                    _logger.LogDebug("No failed notifications found for retry");
                    return;
                }

                _logger.LogInformation("Processing {Count} failed notifications for retry", failedNotifications.Count);

                var successCount = 0;
                var batchSize = Math.Min(failedNotifications.Count, 10); // Limit retries per batch
                
                foreach (var logId in failedNotifications.Take(batchSize))
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    try
                    {
                        var retrySuccess = await pushService.RetryFailedNotificationAsync(logId);
                        if (retrySuccess)
                        {
                            successCount++;
                            _logger.LogInformation("Successfully retried notification {LogId}", logId);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to retry notification {LogId}", logId);
                        }
                    }
                    catch (Exception retryEx)
                    {
                        _logger.LogError(retryEx, "Exception while retrying notification {LogId}", logId);
                    }

                    // Add delay between retry attempts to avoid overwhelming the service
                    await Task.Delay(200, cancellationToken);
                }

                _logger.LogInformation("Completed retry process: {SuccessCount}/{TotalCount} notifications retried successfully", 
                    successCount, batchSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in retry failed notifications process");
            }
        }

        /// <summary>
        /// Update favicon badges based on notification priority counts
        /// </summary>
        private async Task UpdateFaviconBadges(
            AppDbContext context, 
            IPushNotificationService pushService, 
            CancellationToken cancellationToken)
        {
            try
            {
                // This would integrate with a real-time signaling service (SignalR)
                // to update favicon badges on client browsers
                
                // Count unread critical notifications (using ProductBatch for expiry)
                var today = DateTime.Today;
                var criticalCount = await context.ProductBatches
                    .Include(pb => pb.Product)
                    .Where(pb => pb.ExpiryDate.HasValue && 
                               pb.ExpiryDate.Value.Date <= today.AddDays(1) &&
                               pb.CurrentStock > 0)
                    .GroupBy(pb => 1) // Group all together for now
                    .Select(g => new { BranchId = (int?)null, Count = g.Count() })
                    .ToListAsync(cancellationToken);

                // In a real implementation, this would send badge updates via SignalR
                foreach (var branch in criticalCount)
                {
                    _logger.LogDebug("Branch {BranchId} has {Count} critical notifications", 
                        branch.BranchId, branch.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating favicon badges");
            }
        }

        /// <summary>
        /// Cleanup old notification logs to prevent database bloat
        /// </summary>
        private async Task CleanupOldNotificationLogs(AppDbContext context, CancellationToken cancellationToken)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-30); // Keep 30 days of logs
                
                var oldLogs = await context.PushNotificationLogs
                    .Where(l => l.SentAt < cutoffDate)
                    .ToListAsync(cancellationToken);

                if (oldLogs.Any())
                {
                    context.PushNotificationLogs.RemoveRange(oldLogs);
                    await context.SaveChangesAsync(cancellationToken);
                    
                    _logger.LogInformation("Cleaned up {Count} old notification logs", oldLogs.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old notification logs");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Browser Notification Service is stopping...");
            await base.StopAsync(cancellationToken);
            _logger.LogInformation("Browser Notification Service stopped");
        }
    }

    /// <summary>
    /// Extension method to register the browser notification service
    /// </summary>
    public static class BrowserNotificationServiceExtensions
    {
        public static IServiceCollection AddBrowserNotificationService(this IServiceCollection services)
        {
            services.AddHostedService<BrowserNotificationService>();
            return services;
        }
    }
}