using Berca_Backend.DTOs;
using Berca_Backend.Models;

namespace Berca_Backend.Services.Interfaces
{
    /// <summary>
    /// Service interface for managing push notifications in PWA
    /// Handles browser push notifications with Web Push Protocol
    /// </summary>
    public interface IPushNotificationService
    {
        // ==================== SUBSCRIPTION MANAGEMENT ==================== //

        /// <summary>
        /// Subscribe user to push notifications
        /// </summary>
        /// <param name="userId">User ID to subscribe</param>
        /// <param name="subscription">Push subscription data from browser</param>
        /// <returns>True if subscription created successfully</returns>
        Task<bool> SubscribeUserAsync(int userId, PushSubscriptionDto subscription);

        /// <summary>
        /// Unsubscribe user from push notifications
        /// </summary>
        /// <param name="userId">User ID to unsubscribe</param>
        /// <param name="deviceId">Optional device ID for multi-device support</param>
        /// <returns>True if unsubscribed successfully</returns>
        Task<bool> UnsubscribeUserAsync(int userId, string? deviceId = null);

        /// <summary>
        /// Get user's current push subscription status
        /// </summary>
        /// <param name="userId">User ID to check</param>
        /// <param name="deviceId">Optional device ID filter</param>
        /// <returns>Subscription status information</returns>
        Task<PushSubscriptionStatusDto?> GetUserSubscriptionAsync(int userId, string? deviceId = null);

        /// <summary>
        /// Update existing subscription (refresh keys)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="subscription">Updated subscription data</param>
        /// <returns>True if updated successfully</returns>
        Task<bool> UpdateSubscriptionAsync(int userId, PushSubscriptionDto subscription);

        // ==================== NOTIFICATION SENDING ==================== //

        /// <summary>
        /// Send push notification to specific user
        /// </summary>
        /// <param name="userId">Target user ID</param>
        /// <param name="payload">Notification payload</param>
        /// <param name="deviceId">Optional specific device ID</param>
        /// <returns>Delivery result with success/failure details</returns>
        Task<PushNotificationResult> SendNotificationAsync(int userId, NotificationPayload payload, string? deviceId = null);

        /// <summary>
        /// Send push notification to multiple users
        /// </summary>
        /// <param name="userIds">List of target user IDs</param>
        /// <param name="payload">Notification payload</param>
        /// <returns>Bulk delivery result</returns>
        Task<PushNotificationResult> SendBulkNotificationAsync(List<int> userIds, NotificationPayload payload);

        /// <summary>
        /// Send notification with role-based targeting
        /// </summary>
        /// <param name="roles">Target user roles</param>
        /// <param name="payload">Notification payload</param>
        /// <param name="branchId">Optional branch filter</param>
        /// <returns>Delivery result</returns>
        Task<PushNotificationResult> SendToRolesAsync(List<string> roles, NotificationPayload payload, int? branchId = null);

        /// <summary>
        /// Send notification using template
        /// </summary>
        /// <param name="templateKey">Template identifier</param>
        /// <param name="context">Template data context</param>
        /// <returns>Delivery result</returns>
        Task<PushNotificationResult> SendFromTemplateAsync(string templateKey, BusinessNotificationContext context);

        // ==================== TEMPLATE MANAGEMENT ==================== //

        /// <summary>
        /// Create notification template
        /// </summary>
        /// <param name="template">Template creation data</param>
        /// <returns>Created template DTO</returns>
        Task<NotificationTemplateDto> CreateTemplateAsync(CreateNotificationTemplateDto template);

        /// <summary>
        /// Update notification template
        /// </summary>
        /// <param name="id">Template ID</param>
        /// <param name="template">Updated template data</param>
        /// <returns>Updated template DTO or null if not found</returns>
        Task<NotificationTemplateDto?> UpdateTemplateAsync(int id, CreateNotificationTemplateDto template);

        /// <summary>
        /// Get all notification templates
        /// </summary>
        /// <param name="category">Optional category filter</param>
        /// <param name="isActiveOnly">Filter by active status</param>
        /// <returns>List of templates</returns>
        Task<List<NotificationTemplateDto>> GetTemplatesAsync(NotificationCategory? category = null, bool isActiveOnly = true);

        /// <summary>
        /// Get notification template by key
        /// </summary>
        /// <param name="templateKey">Template key</param>
        /// <returns>Template DTO or null if not found</returns>
        Task<NotificationTemplateDto?> GetTemplateByKeyAsync(string templateKey);

        /// <summary>
        /// Delete notification template
        /// </summary>
        /// <param name="id">Template ID</param>
        /// <returns>True if deleted successfully</returns>
        Task<bool> DeleteTemplateAsync(int id);

        // ==================== BUSINESS INTEGRATION ==================== //

        /// <summary>
        /// Send product expiry notification
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="expiryDate">Expiry date</param>
        /// <param name="daysUntilExpiry">Days until expiry</param>
        /// <param name="branchId">Branch ID for targeting</param>
        /// <returns>Delivery result</returns>
        Task<PushNotificationResult> SendProductExpiryNotificationAsync(int productId, DateTime expiryDate, int daysUntilExpiry, int? branchId = null);

        /// <summary>
        /// Send facture due date notification
        /// </summary>
        /// <param name="factureId">Facture ID</param>
        /// <param name="dueDate">Due date</param>
        /// <param name="amount">Outstanding amount</param>
        /// <param name="supplierName">Supplier name</param>
        /// <param name="branchId">Branch ID for targeting</param>
        /// <returns>Delivery result</returns>
        Task<PushNotificationResult> SendFactureDueNotificationAsync(int factureId, DateTime dueDate, decimal amount, string supplierName, int? branchId = null);

        /// <summary>
        /// Send member credit alert notification
        /// </summary>
        /// <param name="memberId">Member ID</param>
        /// <param name="memberName">Member name</param>
        /// <param name="creditIssue">Type of credit issue</param>
        /// <param name="amount">Related amount</param>
        /// <param name="branchId">Branch ID for targeting</param>
        /// <returns>Delivery result</returns>
        Task<PushNotificationResult> SendMemberCreditAlertAsync(int memberId, string memberName, string creditIssue, decimal amount, int? branchId = null);

        /// <summary>
        /// Send low stock notification
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="productName">Product name</param>
        /// <param name="currentStock">Current stock level</param>
        /// <param name="minimumStock">Minimum stock threshold</param>
        /// <param name="branchId">Branch ID for targeting</param>
        /// <returns>Delivery result</returns>
        Task<PushNotificationResult> SendLowStockNotificationAsync(int productId, string productName, int currentStock, int minimumStock, int? branchId = null);

        // ==================== ANALYTICS & MONITORING ==================== //

        /// <summary>
        /// Get push notification delivery statistics
        /// </summary>
        /// <param name="fromDate">Start date for statistics</param>
        /// <param name="toDate">End date for statistics</param>
        /// <param name="branchId">Optional branch filter</param>
        /// <returns>Delivery statistics</returns>
        Task<object> GetDeliveryStatisticsAsync(DateTime fromDate, DateTime toDate, int? branchId = null);

        /// <summary>
        /// Get failed notifications for retry
        /// </summary>
        /// <param name="maxAge">Maximum age of failed notifications to retry</param>
        /// <returns>List of failed notification IDs</returns>
        Task<List<int>> GetFailedNotificationsForRetryAsync(TimeSpan maxAge);

        /// <summary>
        /// Retry failed notification delivery
        /// </summary>
        /// <param name="logId">Notification log ID to retry</param>
        /// <returns>True if retry was successful</returns>
        Task<bool> RetryFailedNotificationAsync(int logId);

        /// <summary>
        /// Clean up expired subscriptions and logs
        /// </summary>
        /// <param name="olderThan">Remove data older than this timespan</param>
        /// <returns>Number of items cleaned up</returns>
        Task<int> CleanupExpiredDataAsync(TimeSpan olderThan);

        // ==================== VALIDATION & UTILITIES ==================== //

        /// <summary>
        /// Validate push subscription data
        /// </summary>
        /// <param name="subscription">Subscription to validate</param>
        /// <returns>True if subscription is valid</returns>
        bool ValidateSubscription(PushSubscriptionDto subscription);

        /// <summary>
        /// Test push service connectivity
        /// </summary>
        /// <param name="userId">User ID to test with</param>
        /// <returns>True if push service is working</returns>
        Task<bool> TestPushServiceAsync(int userId);

        /// <summary>
        /// Get supported push service endpoints
        /// </summary>
        /// <returns>List of supported push services</returns>
        List<string> GetSupportedPushServices();
    }
}