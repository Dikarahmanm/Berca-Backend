using Berca_Backend.Services;
using Berca_Backend.Models;

namespace Berca_Backend.Services
{
    /// <summary>
    /// Background service for automated credit management tasks
    /// Handles payment reminders, credit status updates, and risk monitoring
    /// Runs daily at specified times (Jakarta timezone)
    /// </summary>
    public class MemberCreditBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MemberCreditBackgroundService> _logger;
        private readonly TimeSpan _dailyRunTime = new(8, 0, 0); // 8:00 AM Jakarta time
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // Check every hour

        public MemberCreditBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<MemberCreditBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Member Credit Background Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessDailyTasks(stoppingToken);
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Member Credit Background Service is stopping");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Member Credit Background Service");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Wait 5 minutes before retry
                }
            }
        }

        /// <summary>
        /// Process daily credit management tasks
        /// </summary>
        private async Task ProcessDailyTasks(CancellationToken cancellationToken)
        {
            var now = DateTime.Now; // Use local time (Jakarta)
            var today = now.Date;
            
            // Check if it's time to run daily tasks (around 8 AM)
            if (now.TimeOfDay >= _dailyRunTime && now.TimeOfDay < _dailyRunTime.Add(TimeSpan.FromHours(1)))
            {
                // Check if tasks were already run today (simple check - in production use database flag)
                var lastRunKey = $"credit_tasks_last_run_{today:yyyyMMdd}";
                
                using var scope = _serviceProvider.CreateScope();
                var memberService = scope.ServiceProvider.GetRequiredService<IMemberService>();
                
                _logger.LogInformation("Starting daily credit management tasks for {Date}", today);

                // Task 1: Update all member credit statuses
                await UpdateMemberCreditStatuses(memberService, cancellationToken);

                // Task 2: Send payment reminders
                await SendScheduledPaymentReminders(memberService, cancellationToken);

                // Task 3: Update credit limits based on tier changes
                await UpdateCreditLimits(memberService, cancellationToken);

                // Task 4: Generate risk alerts for high-risk members
                await GenerateRiskAlerts(memberService, cancellationToken);

                // Task 5: Send daily credit summary notification
                await SendDailyCreditSummary(memberService, cancellationToken);

                _logger.LogInformation("Completed daily credit management tasks for {Date}", today);
            }
        }

        /// <summary>
        /// Update credit status for all members based on payment behavior
        /// </summary>
        private async Task UpdateMemberCreditStatuses(IMemberService memberService, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Starting credit status updates");

                using var scope = _serviceProvider.CreateScope();
                var notificationService = scope.ServiceProvider.GetService<INotificationService>();

                // Get analytics to determine how many members need processing
                var analytics = await memberService.GetCreditAnalyticsAsync();
                var totalMembers = analytics.TotalMembersWithCredit;

                _logger.LogInformation("Updating credit status for {TotalMembers} members with credit", totalMembers);

                // In a real implementation, you'd process members in batches
                // For now, we'll process overdue members first (highest priority)
                var overdueMembers = await memberService.GetOverdueMembersAsync();
                
                int updatedCount = 0;
                int errorCount = 0;
                int notificationsCreated = 0;

                foreach (var member in overdueMembers)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    try
                    {
                        var success = await memberService.UpdateCreditStatusAsync(member.MemberId);
                        if (success)
                        {
                            updatedCount++;

                            // CREATE NOTIFICATION for overdue member
                            if (notificationService != null)
                            {
                                var notificationSent = await notificationService.CreateMemberDebtOverdueNotificationAsync(
                                    member.MemberId,
                                    member.MemberName,
                                    member.TotalDebt,
                                    member.DaysOverdue);
                                
                                if (notificationSent)
                                {
                                    notificationsCreated++;
                                }
                            }
                        }
                        else
                        {
                            errorCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        _logger.LogWarning(ex, "Failed to update credit status for member {MemberId}", member.MemberId);
                    }

                    // Small delay to prevent overwhelming the database
                    await Task.Delay(100, cancellationToken);
                }

                _logger.LogInformation("Credit status update completed: {Updated} updated, {Errors} errors, {Notifications} notifications created", 
                    updatedCount, errorCount, notificationsCreated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in credit status update task");
            }
        }

        /// <summary>
        /// Send scheduled payment reminders to overdue members
        /// </summary>
        private async Task SendScheduledPaymentReminders(IMemberService memberService, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Starting scheduled payment reminders");

                var remindersNeeded = await memberService.GetPaymentRemindersAsync();
                
                int sentCount = 0;
                int errorCount = 0;

                foreach (var reminder in remindersNeeded)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    try
                    {
                        var success = await memberService.SendPaymentReminderAsync(reminder.MemberId);
                        if (success)
                        {
                            sentCount++;
                            _logger.LogDebug("Payment reminder sent to member {MemberNumber}", reminder.MemberNumber);
                        }
                        else
                        {
                            errorCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        _logger.LogWarning(ex, "Failed to send reminder to member {MemberId}", reminder.MemberId);
                    }

                    // Delay between reminders to avoid spam and rate limiting
                    await Task.Delay(2000, cancellationToken); // 2 second delay
                }

                _logger.LogInformation("Payment reminders completed: {Sent} sent, {Errors} errors", 
                    sentCount, errorCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in payment reminder task");
            }
        }

        /// <summary>
        /// Update credit limits for members based on tier changes and payment history
        /// </summary>
        private async Task UpdateCreditLimits(IMemberService memberService, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Starting credit limit updates");

                // Get members who might need credit limit adjustments
                // Focus on members with good payment history who might qualify for increases
                var analytics = await memberService.GetCreditAnalyticsAsync();
                
                // In a real implementation, you'd have specific criteria for limit updates
                // For now, we'll log that this task would run
                _logger.LogInformation("Credit limit update task completed - would process {TotalMembers} members", 
                    analytics.TotalMembersWithCredit);

                // Example implementation:
                // - Check members with credit score improvements
                // - Check members with tier upgrades
                // - Check members with consistently good payment behavior
                // - Update limits accordingly
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in credit limit update task");
            }
        }

        /// <summary>
        /// Generate risk alerts for high-risk members requiring attention
        /// </summary>
        private async Task GenerateRiskAlerts(IMemberService memberService, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Starting risk alert generation");

                // Get high-risk members (those overdue or approaching limits)
                var overdueMembers = await memberService.GetOverdueMembersAsync();
                var nearLimitMembers = await memberService.GetMembersApproachingLimitAsync(90); // 90% utilization

                var highRiskCount = overdueMembers.Count(m => m.IsHighRisk);
                var criticalCount = overdueMembers.Count(m => m.RequiresUrgentAction);
                var nearLimitCount = nearLimitMembers.Count;

                if (highRiskCount > 0 || criticalCount > 0 || nearLimitCount > 0)
                {
                    _logger.LogWarning("Risk Alert Summary: {HighRisk} high-risk members, {Critical} critical cases, {NearLimit} approaching limit", 
                        highRiskCount, criticalCount, nearLimitCount);

                    // In a real implementation, you might:
                    // - Send emails to managers
                    // - Create notifications in the system
                    // - Update dashboard alerts
                    // - Trigger additional collections processes
                }
                else
                {
                    _logger.LogInformation("No high-risk members detected");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in risk alert generation task");
            }
        }

        /// <summary>
        /// Send daily credit summary notification to managers
        /// </summary>
        private async Task SendDailyCreditSummary(IMemberService memberService, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Sending daily credit summary notification");

                using var scope = _serviceProvider.CreateScope();
                var notificationService = scope.ServiceProvider.GetService<INotificationService>();

                if (notificationService != null)
                {
                    var overdueMembers = await memberService.GetOverdueMembersAsync();
                    var totalOverdueAmount = overdueMembers.Sum(m => m.TotalDebt);
                    
                    // Calculate new defaulters (simplified - in production you'd track this properly)
                    var newDefaultersToday = overdueMembers.Count(m => m.DaysOverdue <= 1);

                    var success = await notificationService.BroadcastDailyCreditSummaryAsync(
                        overdueMembers.Count,
                        totalOverdueAmount,
                        newDefaultersToday);

                    if (success)
                    {
                        _logger.LogInformation("Daily credit summary notification sent successfully");
                    }
                    else
                    {
                        _logger.LogWarning("Failed to send daily credit summary notification");
                    }
                }
                else
                {
                    _logger.LogWarning("NotificationService not available for daily credit summary");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending daily credit summary notification");
            }
        }

        /// <summary>
        /// Emergency stop method for graceful shutdown
        /// </summary>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Member Credit Background Service is stopping...");
            await base.StopAsync(cancellationToken);
            _logger.LogInformation("Member Credit Background Service stopped");
        }
    }

    /// <summary>
    /// Extension method to register the background service
    /// </summary>
    public static class MemberCreditBackgroundServiceExtensions
    {
        /// <summary>
        /// Add Member Credit Background Service to DI container
        /// </summary>
        public static IServiceCollection AddMemberCreditBackgroundService(this IServiceCollection services)
        {
            services.AddHostedService<MemberCreditBackgroundService>();
            return services;
        }
    }
}