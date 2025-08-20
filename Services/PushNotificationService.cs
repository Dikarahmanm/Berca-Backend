using Berca_Backend.Data;
using Berca_Backend.DTOs;
using Berca_Backend.Models;
using Berca_Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text;
using System.Security.Cryptography;
using WebPush;
using System.Globalization;

namespace Berca_Backend.Services
{
    /// <summary>
    /// Implementation of push notification service for PWA
    /// Handles Web Push Protocol with VAPID authentication
    /// Indonesian business context with proper error handling
    /// </summary>
    public class PushNotificationService : IPushNotificationService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PushNotificationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly WebPushClient? _webPushClient;
        private readonly VapidDetails? _vapidDetails;
        private readonly CultureInfo _indonesianCulture;
        private readonly bool _isConfigured;

        public bool IsConfigured => _isConfigured;

        public PushNotificationService(
            AppDbContext context,
            ILogger<PushNotificationService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
            _indonesianCulture = new CultureInfo("id-ID");

            // Check if push notification is configured
            var publicKey = _configuration["PushNotification:PublicKey"];
            var privateKey = _configuration["PushNotification:PrivateKey"];
            
            if (string.IsNullOrEmpty(publicKey) || string.IsNullOrEmpty(privateKey))
            {
                _logger.LogWarning("Push notification configuration not found. Push notifications will be disabled.");
                _isConfigured = false;
                _vapidDetails = null;
                _webPushClient = null;
                return;
            }

            try
            {
                // Initialize VAPID details from configuration
                _vapidDetails = new VapidDetails(
                    subject: _configuration["PushNotification:Subject"] ?? "mailto:admin@tokoeniwan.com",
                    publicKey: publicKey,
                    privateKey: privateKey
                );

                _webPushClient = new WebPushClient();
                _isConfigured = true;
                _logger.LogInformation("Push notification service configured successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to configure push notification service");
                _isConfigured = false;
                _vapidDetails = null;
                _webPushClient = null;
            }
        }

        // ==================== SUBSCRIPTION MANAGEMENT ==================== //

        public async Task<bool> SubscribeUserAsync(int userId, PushSubscriptionDto subscription)
        {
            if (!_isConfigured)
            {
                _logger.LogWarning("Push notification service not configured. Skipping subscription for user {UserId}", userId);
                return false;
            }

            try
            {
                if (!ValidateSubscription(subscription))
                {
                    _logger.LogWarning("Invalid push subscription data for user {UserId}", userId);
                    return false;
                }

                // Check if subscription already exists for this endpoint
                var existingSubscription = await _context.PushSubscriptions
                    .FirstOrDefaultAsync(s => s.UserId == userId && 
                                            s.Endpoint == subscription.Endpoint);

                if (existingSubscription != null)
                {
                    // Update existing subscription
                    existingSubscription.P256dh = subscription.Keys.P256dh;
                    existingSubscription.Auth = subscription.Keys.Auth;
                    existingSubscription.UserAgent = subscription.UserAgent;
                    existingSubscription.DeviceId = subscription.DeviceId;
                    existingSubscription.BranchId = subscription.BranchId;
                    existingSubscription.IsActive = true;
                    existingSubscription.LastUsedAt = DateTime.UtcNow;

                    _logger.LogInformation("Updated push subscription for user {UserId}", userId);
                }
                else
                {
                    // Create new subscription
                    var newSubscription = new Models.PushSubscription
                    {
                        UserId = userId,
                        Endpoint = subscription.Endpoint,
                        P256dh = subscription.Keys.P256dh,
                        Auth = subscription.Keys.Auth,
                        UserAgent = subscription.UserAgent,
                        DeviceId = subscription.DeviceId,
                        BranchId = subscription.BranchId,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        LastUsedAt = DateTime.UtcNow
                    };

                    _context.PushSubscriptions.Add(newSubscription);
                    _logger.LogInformation("Created new push subscription for user {UserId}", userId);
                }

                await _context.SaveChangesAsync();

                // Send welcome notification
                await SendWelcomeNotificationAsync(userId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing user {UserId} to push notifications", userId);
                return false;
            }
        }

        public async Task<bool> UnsubscribeUserAsync(int userId, string? deviceId = null)
        {
            try
            {
                var query = _context.PushSubscriptions.Where(s => s.UserId == userId);
                
                if (!string.IsNullOrEmpty(deviceId))
                {
                    query = query.Where(s => s.DeviceId == deviceId);
                }

                var subscriptions = await query.ToListAsync();
                
                foreach (var subscription in subscriptions)
                {
                    subscription.IsActive = false;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Unsubscribed user {UserId} from push notifications (Device: {DeviceId})", 
                    userId, deviceId ?? "All");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unsubscribing user {UserId} from push notifications", userId);
                return false;
            }
        }

        public async Task<PushSubscriptionStatusDto?> GetUserSubscriptionAsync(int userId, string? deviceId = null)
        {
            try
            {
                var query = _context.PushSubscriptions
                    .Where(s => s.UserId == userId && s.IsActive);

                if (!string.IsNullOrEmpty(deviceId))
                {
                    query = query.Where(s => s.DeviceId == deviceId);
                }

                var subscription = await query.FirstOrDefaultAsync();

                if (subscription == null)
                {
                    return new PushSubscriptionStatusDto { IsSubscribed = false };
                }

                return new PushSubscriptionStatusDto
                {
                    IsSubscribed = true,
                    SubscribedAt = subscription.CreatedAt,
                    LastUsedAt = subscription.LastUsedAt,
                    DeviceInfo = subscription.UserAgent,
                    IsActive = subscription.IsActive
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscription status for user {UserId}", userId);
                return null;
            }
        }

        public async Task<bool> UpdateSubscriptionAsync(int userId, PushSubscriptionDto subscription)
        {
            try
            {
                var existing = await _context.PushSubscriptions
                    .FirstOrDefaultAsync(s => s.UserId == userId && s.Endpoint == subscription.Endpoint);

                if (existing == null)
                {
                    return await SubscribeUserAsync(userId, subscription);
                }

                existing.P256dh = subscription.Keys.P256dh;
                existing.Auth = subscription.Keys.Auth;
                existing.UserAgent = subscription.UserAgent;
                existing.DeviceId = subscription.DeviceId;
                existing.BranchId = subscription.BranchId;
                existing.LastUsedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated push subscription for user {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subscription for user {UserId}", userId);
                return false;
            }
        }

        // ==================== NOTIFICATION SENDING ==================== //

        public async Task<PushNotificationResult> SendNotificationAsync(int userId, NotificationPayload payload, string? deviceId = null)
        {
            if (!_isConfigured)
            {
                _logger.LogWarning("Push notification service not configured. Skipping notification to user {UserId}", userId);
                return new PushNotificationResult 
                { 
                    Success = false, 
                    TotalSent = 0, 
                    SuccessCount = 0, 
                    FailureCount = 0,
                    Errors = new List<PushDeliveryError> 
                    { 
                        new PushDeliveryError 
                        { 
                            UserId = userId, 
                            ErrorMessage = "Push notification service not configured", 
                            IsRetryable = false 
                        } 
                    }
                };
            }

            var result = new PushNotificationResult();

            try
            {
                var query = _context.PushSubscriptions
                    .Where(s => s.UserId == userId && s.IsActive);

                if (!string.IsNullOrEmpty(deviceId))
                {
                    query = query.Where(s => s.DeviceId == deviceId);
                }

                var subscriptions = await query.ToListAsync();
                
                if (!subscriptions.Any())
                {
                    _logger.LogWarning("No active push subscriptions found for user {UserId}", userId);
                    return result;
                }

                result.TotalSent = subscriptions.Count;

                foreach (var subscription in subscriptions)
                {
                    var deliveryResult = await SendToSubscriptionAsync(subscription, payload);
                    
                    if (deliveryResult.Success)
                    {
                        result.SuccessCount++;
                    }
                    else
                    {
                        result.FailureCount++;
                        result.Errors.Add(new PushDeliveryError
                        {
                            UserId = userId,
                            ErrorMessage = deliveryResult.ErrorMessage ?? "Unknown error",
                            StatusCode = deliveryResult.StatusCode,
                            IsRetryable = deliveryResult.IsRetryable
                        });
                    }
                }

                result.Success = result.SuccessCount > 0;

                _logger.LogInformation("Push notification sent to user {UserId}: {Success}/{Total} successful", 
                    userId, result.SuccessCount, result.TotalSent);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending push notification to user {UserId}", userId);
                result.Errors.Add(new PushDeliveryError
                {
                    UserId = userId,
                    ErrorMessage = ex.Message,
                    IsRetryable = true
                });
                return result;
            }
        }

        public async Task<PushNotificationResult> SendBulkNotificationAsync(List<int> userIds, NotificationPayload payload)
        {
            var result = new PushNotificationResult();

            try
            {
                var subscriptions = await _context.PushSubscriptions
                    .Where(s => userIds.Contains(s.UserId) && s.IsActive)
                    .ToListAsync();

                result.TotalSent = subscriptions.Count;

                var tasks = subscriptions.Select(async subscription =>
                {
                    var deliveryResult = await SendToSubscriptionAsync(subscription, payload);
                    
                    lock (result)
                    {
                        if (deliveryResult.Success)
                        {
                            result.SuccessCount++;
                        }
                        else
                        {
                            result.FailureCount++;
                            result.Errors.Add(new PushDeliveryError
                            {
                                UserId = subscription.UserId,
                                ErrorMessage = deliveryResult.ErrorMessage ?? "Unknown error",
                                StatusCode = deliveryResult.StatusCode,
                                IsRetryable = deliveryResult.IsRetryable
                            });
                        }
                    }
                });

                await Task.WhenAll(tasks);

                result.Success = result.SuccessCount > 0;

                _logger.LogInformation("Bulk push notification sent: {Success}/{Total} successful to {UserCount} users", 
                    result.SuccessCount, result.TotalSent, userIds.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending bulk push notifications");
                result.Errors.Add(new PushDeliveryError
                {
                    UserId = 0,
                    ErrorMessage = ex.Message,
                    IsRetryable = true
                });
                return result;
            }
        }

        public async Task<PushNotificationResult> SendToRolesAsync(List<string> roles, NotificationPayload payload, int? branchId = null)
        {
            if (!_isConfigured)
            {
                _logger.LogWarning("Push notification service not configured. Skipping role-based notification to roles: {Roles}", string.Join(", ", roles));
                return new PushNotificationResult 
                { 
                    Success = false, 
                    TotalSent = 0, 
                    SuccessCount = 0, 
                    FailureCount = 0,
                    Errors = new List<PushDeliveryError> 
                    { 
                        new PushDeliveryError 
                        { 
                            UserId = 0, 
                            ErrorMessage = "Push notification service not configured", 
                            IsRetryable = false 
                        } 
                    }
                };
            }

            try
            {
                var query = _context.Users
                    .Where(u => roles.Contains(u.Role) && u.IsActive);

                if (branchId.HasValue)
                {
                    query = query.Where(u => u.BranchId == branchId.Value);
                }

                var userIds = await query.Select(u => u.Id).ToListAsync();

                _logger.LogInformation("Sending role-based notification to {UserCount} users with roles {Roles}", 
                    userIds.Count, string.Join(", ", roles));

                return await SendBulkNotificationAsync(userIds, payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending role-based push notifications");
                return new PushNotificationResult
                {
                    Errors = new List<PushDeliveryError>
                    {
                        new() { ErrorMessage = ex.Message, IsRetryable = true }
                    }
                };
            }
        }

        // ==================== PRIVATE HELPER METHODS ==================== //

        private async Task<(bool Success, string? ErrorMessage, int? StatusCode, bool IsRetryable)> SendToSubscriptionAsync(
            Models.PushSubscription subscription, NotificationPayload payload)
        {
            if (_webPushClient == null || _vapidDetails == null)
            {
                return (false, "Push notification service not properly configured", null, false);
            }

            try
            {
                var pushSubscription = new global::WebPush.PushSubscription(
                    subscription.Endpoint,
                    subscription.P256dh,
                    subscription.Auth
                );

                var notificationData = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                });

                await _webPushClient.SendNotificationAsync(pushSubscription, notificationData, _vapidDetails);

                // Log successful delivery
                await LogNotificationDeliveryAsync(subscription.Id, null, payload, true, null, 200);

                subscription.LastUsedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return (true, null, 200, false);
            }
            catch (WebPushException ex)
            {
                var statusCode = (int)ex.StatusCode;
                var isRetryable = statusCode >= 500 && statusCode < 600; // Server errors are retryable
                
                // Handle specific error cases
                if (statusCode == 410 || statusCode == 404)
                {
                    // Subscription expired or invalid - deactivate it
                    subscription.IsActive = false;
                    await _context.SaveChangesAsync();
                    _logger.LogWarning("Deactivated invalid push subscription for user {UserId}", subscription.UserId);
                }

                await LogNotificationDeliveryAsync(subscription.Id, null, payload, false, ex.Message, statusCode);

                _logger.LogWarning("Push notification failed for user {UserId}: {Error} (Status: {StatusCode})", 
                    subscription.UserId, ex.Message, statusCode);

                return (false, ex.Message, statusCode, isRetryable);
            }
            catch (Exception ex)
            {
                await LogNotificationDeliveryAsync(subscription.Id, null, payload, false, ex.Message, null);

                _logger.LogError(ex, "Unexpected error sending push notification to user {UserId}", subscription.UserId);
                return (false, ex.Message, null, true);
            }
        }

        private async Task LogNotificationDeliveryAsync(
            int subscriptionId, 
            int? templateId, 
            NotificationPayload payload, 
            bool success, 
            string? errorMessage, 
            int? statusCode)
        {
            try
            {
                var log = new PushNotificationLog
                {
                    PushSubscriptionId = subscriptionId,
                    NotificationTemplateId = templateId,
                    Title = payload.Title,
                    Body = payload.Body,
                    Priority = payload.Priority,
                    DeliverySuccess = success,
                    ErrorMessage = errorMessage,
                    ResponseStatusCode = statusCode,
                    SentAt = DateTime.UtcNow
                };

                _context.PushNotificationLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging push notification delivery");
            }
        }

        private async Task SendWelcomeNotificationAsync(int userId)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserProfile)
                    .FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null) return;

                var userName = user.UserProfile?.FullName ?? user.Username;
                var welcomePayload = new NotificationPayload
                {
                    Title = "🎉 Notifikasi Aktif!",
                    Body = $"Hai {userName}, Anda akan menerima pemberitahuan penting dari Toko Eniwan POS.",
                    Icon = "/icons/icon-192x192.png",
                    Badge = "/icons/badge-72x72.png",
                    Tag = "welcome",
                    Priority = NotificationPriority.Normal,
                    Data = new Dictionary<string, object>
                    {
                        { "type", "welcome" },
                        { "timestamp", DateTime.UtcNow.ToString("O") }
                    }
                };

                // Small delay to ensure subscription is saved
                await Task.Delay(1000);
                await SendNotificationAsync(userId, welcomePayload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending welcome notification to user {UserId}", userId);
            }
        }

        // ==================== VALIDATION & UTILITIES ==================== //

        public bool ValidateSubscription(PushSubscriptionDto subscription)
        {
            if (string.IsNullOrEmpty(subscription.Endpoint)) return false;
            if (string.IsNullOrEmpty(subscription.Keys.P256dh)) return false;
            if (string.IsNullOrEmpty(subscription.Keys.Auth)) return false;
            
            // Validate endpoint URL format
            if (!Uri.TryCreate(subscription.Endpoint, UriKind.Absolute, out var endpointUri)) return false;
            
            // Check if it's a known push service endpoint
            var supportedServices = GetSupportedPushServices();
            var isSupported = supportedServices.Any(service => endpointUri.Host.Contains(service));
            
            return isSupported;
        }

        public List<string> GetSupportedPushServices()
        {
            return new List<string>
            {
                "fcm.googleapis.com", // Firebase Cloud Messaging
                "android.googleapis.com", // Legacy Android
                "updates.push.services.mozilla.com", // Firefox
                "wns2-", // Windows Push Notification
                "push.apple.com" // Safari (when supported)
            };
        }

        public async Task<bool> TestPushServiceAsync(int userId)
        {
            try
            {
                var testPayload = new NotificationPayload
                {
                    Title = "🔧 Test Notifikasi",
                    Body = "Ini adalah test notifikasi untuk memastikan layanan berfungsi dengan baik.",
                    Icon = "/icons/icon-192x192.png",
                    Tag = "test",
                    Priority = NotificationPriority.Normal
                };

                var result = await SendNotificationAsync(userId, testPayload);
                return result.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing push service for user {UserId}", userId);
                return false;
            }
        }

        // ==================== BUSINESS INTEGRATION (Placeholder methods) ==================== //

        public Task<PushNotificationResult> SendProductExpiryNotificationAsync(int productId, DateTime expiryDate, int daysUntilExpiry, int? branchId = null)
        {
            // This would integrate with the existing product expiry system
            // For now, return a placeholder implementation
            return Task.FromResult(new PushNotificationResult { Success = true });
        }

        public Task<PushNotificationResult> SendFactureDueNotificationAsync(int factureId, DateTime dueDate, decimal amount, string supplierName, int? branchId = null)
        {
            // This would integrate with the existing facture system
            // For now, return a placeholder implementation
            return Task.FromResult(new PushNotificationResult { Success = true });
        }

        public Task<PushNotificationResult> SendMemberCreditAlertAsync(int memberId, string memberName, string creditIssue, decimal amount, int? branchId = null)
        {
            // This would integrate with the existing member credit system
            // For now, return a placeholder implementation
            return Task.FromResult(new PushNotificationResult { Success = true });
        }

        public Task<PushNotificationResult> SendLowStockNotificationAsync(int productId, string productName, int currentStock, int minimumStock, int? branchId = null)
        {
            // This would integrate with the existing inventory system
            // For now, return a placeholder implementation
            return Task.FromResult(new PushNotificationResult { Success = true });
        }

        // ==================== TEMPLATE MANAGEMENT (Simplified implementation) ==================== //

        public Task<NotificationTemplateDto> CreateTemplateAsync(CreateNotificationTemplateDto template)
        {
            // Placeholder implementation - would store templates in database
            throw new NotImplementedException("Template management not yet implemented");
        }

        public Task<NotificationTemplateDto?> UpdateTemplateAsync(int id, CreateNotificationTemplateDto template)
        {
            throw new NotImplementedException("Template management not yet implemented");
        }

        public Task<List<NotificationTemplateDto>> GetTemplatesAsync(NotificationCategory? category = null, bool isActiveOnly = true)
        {
            throw new NotImplementedException("Template management not yet implemented");
        }

        public Task<NotificationTemplateDto?> GetTemplateByKeyAsync(string templateKey)
        {
            throw new NotImplementedException("Template management not yet implemented");
        }

        public Task<bool> DeleteTemplateAsync(int id)
        {
            throw new NotImplementedException("Template management not yet implemented");
        }

        public Task<PushNotificationResult> SendFromTemplateAsync(string templateKey, BusinessNotificationContext context)
        {
            throw new NotImplementedException("Template-based notifications not yet implemented");
        }

        // ==================== ANALYTICS & MONITORING (Simplified implementation) ==================== //

        public Task<object> GetDeliveryStatisticsAsync(DateTime fromDate, DateTime toDate, int? branchId = null)
        {
            throw new NotImplementedException("Analytics not yet implemented");
        }

        public async Task<List<int>> GetFailedNotificationsForRetryAsync(TimeSpan maxAge)
        {
            try
            {
                var cutoffTime = DateTime.UtcNow.Subtract(maxAge);
                
                var failedLogIds = await _context.PushNotificationLogs
                    .Where(log => 
                        !log.DeliverySuccess && 
                        log.SentAt >= cutoffTime)
                    .OrderBy(log => log.SentAt)
                    .Take(50) // Process max 50 failed notifications per batch
                    .Select(log => log.Id)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} failed notification IDs for retry", failedLogIds.Count);
                
                return failedLogIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving failed notifications for retry");
                return new List<int>();
            }
        }

        public async Task<bool> RetryFailedNotificationAsync(int logId)
        {
            if (!_isConfigured)
            {
                _logger.LogWarning("Push notification service not configured. Cannot retry notification {LogId}", logId);
                return false;
            }

            try
            {
                // Get original notification log with subscription info
                var notificationLog = await _context.PushNotificationLogs
                    .Include(log => log.PushSubscription)
                    .ThenInclude(sub => sub.User)
                    .FirstOrDefaultAsync(log => log.Id == logId);

                if (notificationLog == null)
                {
                    _logger.LogWarning("Notification log {LogId} not found for retry", logId);
                    return false;
                }

                if (notificationLog.DeliverySuccess)
                {
                    _logger.LogInformation("Notification {LogId} already delivered successfully", logId);
                    return true;
                }

                var subscription = notificationLog.PushSubscription;
                if (subscription == null || !subscription.IsActive)
                {
                    _logger.LogWarning("Subscription for notification {LogId} is inactive or not found", logId);
                    return false;
                }

                // Recreate notification payload from logged data
                var payload = new NotificationPayload
                {
                    Title = notificationLog.Title,
                    Body = notificationLog.Body,
                    Icon = "/icons/icon-192x192.png",
                    Badge = "/icons/badge-72x72.png",
                    Priority = notificationLog.Priority,
                    Tag = "retry",
                    Data = new Dictionary<string, object>
                    {
                        { "retryId", logId },
                        { "originalSentAt", notificationLog.SentAt.ToString("O") },
                        { "retryAttempt", DateTime.UtcNow.ToString("O") }
                    }
                };

                // Attempt to send notification
                var deliveryResult = await SendToSubscriptionAsync(subscription, payload);

                if (deliveryResult.Success)
                {
                    // Update the original log to mark as successful
                    notificationLog.DeliverySuccess = true;
                    notificationLog.ErrorMessage = null;
                    notificationLog.ResponseStatusCode = 200;
                    
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Successfully retried notification {LogId} for user {UserId}", 
                        logId, subscription.UserId);
                    
                    return true;
                }
                else
                {
                    // Update error message but don't change delivery status (for potential future retries)
                    notificationLog.ErrorMessage = $"Retry failed: {deliveryResult.ErrorMessage}";
                    notificationLog.ResponseStatusCode = deliveryResult.StatusCode;
                    
                    await _context.SaveChangesAsync();
                    
                    _logger.LogWarning("Failed to retry notification {LogId} for user {UserId}: {Error}", 
                        logId, subscription.UserId, deliveryResult.ErrorMessage);
                    
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrying notification {LogId}", logId);
                return false;
            }
        }

        public Task<int> CleanupExpiredDataAsync(TimeSpan olderThan)
        {
            throw new NotImplementedException("Cleanup not yet implemented");
        }
    }
}