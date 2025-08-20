using Microsoft.Extensions.DependencyInjection;
using Berca_Backend.Services.Interfaces;
using Berca_Backend.Models;
using Berca_Backend.DTOs;

namespace Berca_Backend.Services
{
    /// <summary>
    /// Background service for processing calendar event reminders
    /// Sends push notifications for scheduled reminders
    /// Indonesian business context with proper error handling
    /// </summary>
    public class EventReminderService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EventReminderService> _logger;
        private readonly TimeSpan _processingInterval = TimeSpan.FromMinutes(5); // Check every 5 minutes
        private readonly TimeZoneInfo _jakartaTimeZone;

        public EventReminderService(
            IServiceProvider serviceProvider,
            ILogger<EventReminderService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _jakartaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); // Jakarta timezone
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Event Reminder Service started at {StartTime}", DateTime.UtcNow);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessPendingReminders(stoppingToken);
                    await CleanupOldReminders(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Service is being stopped
                    _logger.LogInformation("Event Reminder Service is being stopped");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing reminders");
                }

                try
                {
                    await Task.Delay(_processingInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("Event Reminder Service stopped at {StopTime}", DateTime.UtcNow);
        }

        /// <summary>
        /// Process all pending reminders that are due for sending
        /// </summary>
        private async Task ProcessPendingReminders(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var calendarService = scope.ServiceProvider.GetRequiredService<ICalendarEventService>();
            var pushNotificationService = scope.ServiceProvider.GetRequiredService<IPushNotificationService>();

            try
            {
                // Get reminders due within the next 15 minutes (buffer for processing)
                var currentTime = DateTime.UtcNow;
                var checkTime = currentTime.AddMinutes(15);
                
                var pendingReminders = await calendarService.GetUpcomingRemindersAsync(checkTime);
                
                if (!pendingReminders.Any())
                {
                    _logger.LogDebug("No pending reminders to process at {Time}", currentTime);
                    return;
                }

                _logger.LogInformation("Processing {Count} pending reminders", pendingReminders.Count);

                var processedCount = 0;
                var failedCount = 0;

                foreach (var reminder in pendingReminders)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    try
                    {
                        // Skip if not yet due (allowing 1-minute early processing)
                        if (reminder.ScheduledTime > currentTime.AddMinutes(1))
                            continue;

                        var success = await ProcessSingleReminder(reminder, pushNotificationService, cancellationToken);
                        
                        // Mark reminder as processed
                        await calendarService.MarkReminderSentAsync(
                            reminder.Id, 
                            currentTime, 
                            success,
                            success ? null : "Failed to send push notification"
                        );

                        if (success)
                        {
                            processedCount++;
                            _logger.LogDebug("Successfully processed reminder {ReminderId} for event '{EventTitle}'", 
                                reminder.Id, reminder.EventTitle);
                        }
                        else
                        {
                            failedCount++;
                            _logger.LogWarning("Failed to process reminder {ReminderId} for event '{EventTitle}'", 
                                reminder.Id, reminder.EventTitle);
                        }
                    }
                    catch (Exception ex)
                    {
                        failedCount++;
                        _logger.LogError(ex, "Error processing reminder {ReminderId}", reminder.Id);
                        
                        // Mark as failed
                        await calendarService.MarkReminderSentAsync(
                            reminder.Id, 
                            currentTime, 
                            false,
                            $"Processing error: {ex.Message}"
                        );
                    }
                }

                if (processedCount > 0 || failedCount > 0)
                {
                    _logger.LogInformation("Reminder processing completed: {Success} successful, {Failed} failed", 
                        processedCount, failedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProcessPendingReminders");
                throw;
            }
        }

        /// <summary>
        /// Process a single reminder by sending appropriate notification
        /// </summary>
        private async Task<bool> ProcessSingleReminder(
            EventReminderDto reminder, 
            IPushNotificationService pushNotificationService, 
            CancellationToken cancellationToken)
        {
            try
            {
                // Create notification payload based on event
                var payload = CreateReminderNotificationPayload(reminder);

                PushNotificationResult result;

                if (reminder.UserId.HasValue)
                {
                    // Send to specific user
                    result = await pushNotificationService.SendNotificationAsync(reminder.UserId.Value, payload);
                }
                else if (!string.IsNullOrEmpty(reminder.TargetRole))
                {
                    // Send to users with specific role
                    var roles = new List<string> { reminder.TargetRole };
                    result = await pushNotificationService.SendToRolesAsync(roles, payload, null);
                }
                else
                {
                    _logger.LogWarning("Reminder {ReminderId} has no target user or role", reminder.Id);
                    return false;
                }

                var success = result.Success && result.SuccessCount > 0;
                
                if (success)
                {
                    _logger.LogDebug("Sent reminder notification for event '{EventTitle}' to {Count} recipients", 
                        reminder.EventTitle, result.SuccessCount);
                }
                else
                {
                    _logger.LogWarning("Failed to send reminder notification for event '{EventTitle}': {Errors}", 
                        reminder.EventTitle, string.Join(", ", result.Errors));
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending reminder notification for reminder {ReminderId}", reminder.Id);
                return false;
            }
        }

        /// <summary>
        /// Create notification payload for reminder
        /// </summary>
        private NotificationPayload CreateReminderNotificationPayload(EventReminderDto reminder)
        {
            var priorityText = GetPriorityText(reminder.EventPriority);
            var timeUntilEvent = reminder.ScheduledTime - DateTime.UtcNow;
            var timeText = GetTimeUntilText(timeUntilEvent);

            var title = reminder.EventPriority switch
            {
                EventPriority.Critical => "🚨 Pengingat Penting!",
                EventPriority.High => "⚠️ Pengingat Urgent",
                EventPriority.Normal => "📅 Pengingat Event",
                _ => "📝 Pengingat"
            };

            var body = $"{reminder.EventTitle}\n{timeText}";

            // Add priority indicator for high priority events
            if (reminder.EventPriority >= EventPriority.High)
            {
                body = $"[{priorityText}] {body}";
            }

            var icon = reminder.EventPriority switch
            {
                EventPriority.Critical => "https://cdn-icons-png.flaticon.com/512/564/564619.png", // Critical alert icon
                EventPriority.High => "https://cdn-icons-png.flaticon.com/512/1827/1827370.png", // Warning icon
                _ => "https://cdn-icons-png.flaticon.com/512/2693/2693507.png" // Calendar icon
            };

            var actionUrl = $"/calendar/event/{reminder.CalendarEventId}";

            return new NotificationPayload
            {
                Title = title,
                Body = body,
                Icon = icon,
                Badge = "https://cdn-icons-png.flaticon.com/512/2693/2693507.png",
                Tag = $"calendar_reminder_{reminder.Id}",
                RequireInteraction = reminder.EventPriority >= EventPriority.High,
                Silent = false,
                Actions = new List<NotificationActionDto>
                {
                    new NotificationActionDto
                    {
                        Action = "view",
                        Title = "Lihat Detail",
                        Icon = "https://cdn-icons-png.flaticon.com/512/709/709612.png"
                    },
                    new NotificationActionDto
                    {
                        Action = "dismiss",
                        Title = "Tutup",
                        Icon = "https://cdn-icons-png.flaticon.com/512/1828/1828774.png"
                    }
                },
                Data = new Dictionary<string, object>
                {
                    ["type"] = "calendar_reminder",
                    ["reminderId"] = reminder.Id,
                    ["eventId"] = reminder.CalendarEventId,
                    ["priority"] = reminder.EventPriority.ToString(),
                    ["scheduledTime"] = reminder.ScheduledTime,
                    ["url"] = actionUrl
                }
            };
        }

        /// <summary>
        /// Clean up old processed reminders to prevent database bloat
        /// </summary>
        private async Task CleanupOldReminders(CancellationToken cancellationToken)
        {
            // Only run cleanup once per day
            var now = DateTime.UtcNow;
            
            // This is a simple implementation - in a production environment,
            // you might want to use a more sophisticated caching mechanism
            if (!ShouldRunCleanup())
                return;

            using var scope = _serviceProvider.CreateScope();
            var calendarService = scope.ServiceProvider.GetRequiredService<ICalendarEventService>();

            try
            {
                // Clean up events older than 90 days
                var cleanedCount = await calendarService.CleanupOldEventsAsync(90);
                
                if (cleanedCount > 0)
                {
                    _logger.LogInformation("Cleaned up {Count} old calendar events and their reminders", cleanedCount);
                }

                // Mark cleanup as done for today
                _lastCleanupDate = DateTime.UtcNow.Date;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during reminder cleanup");
            }
        }

        private DateTime? _lastCleanupDate;
        
        private bool ShouldRunCleanup()
        {
            var today = DateTime.UtcNow.Date;
            
            if (_lastCleanupDate.HasValue && _lastCleanupDate.Value >= today)
                return false;
                
            // Run cleanup at a random time between 2-4 AM Jakarta time to avoid peak usage
            var jakartaTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _jakartaTimeZone);
            var currentHour = jakartaTime.Hour;
            
            return currentHour >= 2 && currentHour < 4;
        }

        /// <summary>
        /// Get priority text in Indonesian
        /// </summary>
        private static string GetPriorityText(EventPriority priority)
        {
            return priority switch
            {
                EventPriority.Critical => "KRITIS",
                EventPriority.High => "PENTING",
                EventPriority.Normal => "NORMAL",
                EventPriority.Low => "RENDAH",
                _ => "NORMAL"
            };
        }

        /// <summary>
        /// Get time until event text in Indonesian
        /// </summary>
        private string GetTimeUntilText(TimeSpan timeUntil)
        {
            var totalMinutes = (int)timeUntil.TotalMinutes;
            
            if (totalMinutes <= 0)
            {
                return "Sudah dimulai atau terlewat";
            }
            else if (totalMinutes < 60)
            {
                return $"Dimulai dalam {totalMinutes} menit";
            }
            else if (totalMinutes < 1440) // Less than 24 hours
            {
                var hours = totalMinutes / 60;
                var minutes = totalMinutes % 60;
                
                if (minutes == 0)
                {
                    return $"Dimulai dalam {hours} jam";
                }
                else
                {
                    return $"Dimulai dalam {hours} jam {minutes} menit";
                }
            }
            else
            {
                var days = totalMinutes / 1440;
                var remainingHours = (totalMinutes % 1440) / 60;
                
                if (remainingHours == 0)
                {
                    return $"Dimulai dalam {days} hari";
                }
                else
                {
                    return $"Dimulai dalam {days} hari {remainingHours} jam";
                }
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Event Reminder Service is stopping...");
            await base.StopAsync(stoppingToken);
        }

        public override void Dispose()
        {
            _logger.LogInformation("Event Reminder Service disposed");
            base.Dispose();
        }
    }
}