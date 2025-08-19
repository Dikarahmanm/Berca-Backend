using Berca_Backend.Services.Interfaces;
using Berca_Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace Berca_Backend.Services
{
    /// <summary>
    /// Background service for facture payment reminders and notifications
    /// Runs daily to check due dates and create appropriate notifications
    /// </summary>
    public class FactureBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<FactureBackgroundService> _logger;
        private readonly TimeSpan _dailyRunTime = new(7, 0, 0); // 7:00 AM Jakarta time
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // Check every hour

        public FactureBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<FactureBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Facture Background Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessDailyFactureTasks(stoppingToken);
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Facture Background Service is stopping");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Facture Background Service");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }
        }

        /// <summary>
        /// Process daily facture tasks including due date checks and notifications
        /// </summary>
        private async Task ProcessDailyFactureTasks(CancellationToken cancellationToken)
        {
            var now = DateTime.Now; // Local time (Jakarta)
            var today = now.Date;
            
            // Check if it's time to run daily tasks (around 7 AM)
            if (now.TimeOfDay >= _dailyRunTime && now.TimeOfDay < _dailyRunTime.Add(TimeSpan.FromHours(1)))
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var notificationService = scope.ServiceProvider.GetService<INotificationService>();

                if (notificationService != null)
                {
                    _logger.LogInformation("Starting daily facture checks for {Date}", today);

                    // Check factures due today
                    await CheckFacturesDueToday(context, notificationService, cancellationToken);

                    // Check overdue factures
                    await CheckOverdueFactures(context, notificationService, cancellationToken);

                    // Check for approval required factures
                    await CheckFacturesRequiringApproval(context, notificationService, cancellationToken);

                    _logger.LogInformation("Completed daily facture checks for {Date}", today);
                }
            }
        }

        /// <summary>
        /// Check and notify for factures due today
        /// </summary>
        private async Task CheckFacturesDueToday(AppDbContext context, INotificationService notificationService, CancellationToken cancellationToken)
        {
            try
            {
                var today = DateTime.Today;
                
                var facturesDueToday = await context.Factures
                    .Include(f => f.Supplier)
                    .Where(f => f.DueDate.Date == today &&
                               f.Status != Models.FactureStatus.Paid &&
                               f.Status != Models.FactureStatus.Cancelled)
                    .ToListAsync(cancellationToken);

                int notificationsCreated = 0;

                foreach (var facture in facturesDueToday)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    try
                    {
                        var success = await notificationService.CreateFactureDueTodayNotificationAsync(
                            facture.SupplierInvoiceNumber,
                            facture.Supplier?.CompanyName ?? "Unknown Supplier",
                            facture.TotalAmount - facture.PaidAmount);

                        if (success)
                        {
                            notificationsCreated++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to create due today notification for facture {FactureNumber}", facture.SupplierInvoiceNumber);
                    }
                }

                _logger.LogInformation("Created {Count} due today notifications for {TotalFactures} factures", 
                    notificationsCreated, facturesDueToday.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking factures due today");
            }
        }

        /// <summary>
        /// Check and notify for overdue factures
        /// </summary>
        private async Task CheckOverdueFactures(AppDbContext context, INotificationService notificationService, CancellationToken cancellationToken)
        {
            try
            {
                var today = DateTime.Today;
                
                var overdueFactures = await context.Factures
                    .Include(f => f.Supplier)
                    .Where(f => f.DueDate.Date < today &&
                               f.Status != Models.FactureStatus.Paid &&
                               f.Status != Models.FactureStatus.Cancelled)
                    .ToListAsync(cancellationToken);

                int notificationsCreated = 0;

                foreach (var facture in overdueFactures)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    try
                    {
                        var daysOverdue = (today - facture.DueDate.Date).Days;
                        
                        var success = await notificationService.CreateFactureOverdueNotificationAsync(
                            facture.SupplierInvoiceNumber,
                            facture.Supplier?.CompanyName ?? "Unknown Supplier",
                            facture.TotalAmount - facture.PaidAmount,
                            daysOverdue);

                        if (success)
                        {
                            notificationsCreated++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to create overdue notification for facture {FactureNumber}", facture.SupplierInvoiceNumber);
                    }
                }

                _logger.LogInformation("Created {Count} overdue notifications for {TotalFactures} factures", 
                    notificationsCreated, overdueFactures.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking overdue factures");
            }
        }

        /// <summary>
        /// Check and notify for factures requiring approval
        /// </summary>
        private async Task CheckFacturesRequiringApproval(AppDbContext context, INotificationService notificationService, CancellationToken cancellationToken)
        {
            try
            {
                var facturesRequiringApproval = await context.Factures
                    .Include(f => f.Supplier)
                    .Where(f => f.Status == Models.FactureStatus.Verified &&
                               f.TotalAmount > 10000000) // Factures over 10M IDR require approval
                    .ToListAsync(cancellationToken);

                int notificationsCreated = 0;

                foreach (var facture in facturesRequiringApproval)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    try
                    {
                        var success = await notificationService.CreateFactureApprovalRequiredNotificationAsync(
                            facture.SupplierInvoiceNumber,
                            facture.Supplier?.CompanyName ?? "Unknown Supplier",
                            facture.TotalAmount,
                            facture.ReceivedBy ?? 0);

                        if (success)
                        {
                            notificationsCreated++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to create approval required notification for facture {FactureNumber}", facture.SupplierInvoiceNumber);
                    }
                }

                _logger.LogInformation("Created {Count} approval required notifications for {TotalFactures} factures", 
                    notificationsCreated, facturesRequiringApproval.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking factures requiring approval");
            }
        }

        /// <summary>
        /// Emergency stop method for graceful shutdown
        /// </summary>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Facture Background Service is stopping...");
            await base.StopAsync(cancellationToken);
            _logger.LogInformation("Facture Background Service stopped");
        }
    }

    /// <summary>
    /// Extension method to register the background service
    /// </summary>
    public static class FactureBackgroundServiceExtensions
    {
        /// <summary>
        /// Add Facture Background Service to DI container
        /// </summary>
        public static IServiceCollection AddFactureBackgroundService(this IServiceCollection services)
        {
            services.AddHostedService<FactureBackgroundService>();
            return services;
        }
    }
}