// Services/INotificationService.cs - Sprint 2 Notification Service Interface
using Berca_Backend.DTOs;

namespace Berca_Backend.Services
{
    public interface INotificationService
    {
        // CRUD Operations
        Task<List<NotificationDto>> GetUserNotificationsAsync(int userId, bool? isRead = null, int page = 1, int pageSize = 20);
        Task<NotificationSummaryDto> GetNotificationSummaryAsync(int userId);
        Task<NotificationDto> CreateNotificationAsync(CreateNotificationRequest request, string? createdBy = null);
        Task<bool> MarkAsReadAsync(int notificationId, int userId);
        Task<bool> MarkAllAsReadAsync(int userId);
        Task<bool> DeleteNotificationAsync(int notificationId, int userId);

        // System Notifications
        Task<bool> CreateLowStockNotificationAsync(int productId, int currentStock);
        Task<bool> CreateMonthlyRevenueNotificationAsync(decimal revenue, DateTime month);
        Task<bool> CreateInventoryAuditNotificationAsync();
        Task<bool> CreateSystemMaintenanceNotificationAsync(DateTime scheduledTime, string message);

        // Broadcast Notifications
        Task<bool> BroadcastToAllUsersAsync(string type, string title, string message, string? actionUrl = null);
        Task<bool> BroadcastToRoleAsync(string role, string type, string title, string message, string? actionUrl = null);

        // Notification Settings
        Task<NotificationSettingsDto> GetUserSettingsAsync(int userId);
        Task<bool> UpdateUserSettingsAsync(int userId, UpdateNotificationSettingsRequest request);

        // Cleanup
        Task<int> CleanupExpiredNotificationsAsync();
        Task<int> ArchiveOldNotificationsAsync(int daysOld = 30);

        // ✅ EXISTING: Product & Stock Notifications
        Task<bool> CreateOutOfStockNotificationAsync(int productId);
        Task<bool> CreateStockAdjustmentNotificationAsync(int productId, int quantity, string notes);

        // ✅ EXISTING: Sale Notifications
        Task<bool> CreateSaleCompletedNotificationAsync(int saleId, string saleNumber, decimal totalAmount);

        // ✅ ADDED: Missing Sale Notification Methods
        Task<bool> CreateSaleCancelledNotificationAsync(int saleId, string saleNumber, decimal totalAmount, string reason);
        Task<bool> CreateSaleRefundedNotificationAsync(int saleId, string saleNumber, decimal totalAmount, string reason);

        // ==================== EXPIRY NOTIFICATION METHODS ==================== //

        /// <summary>
        /// Create notification for products expiring soon (7 days warning)
        /// </summary>
        Task<bool> CreateExpiryWarningNotificationAsync(int productId, string productName, string batchNumber, DateTime expiryDate, int currentStock, int? branchId = null);

        /// <summary>
        /// Create urgent notification for products expiring very soon (3 days urgent)
        /// </summary>
        Task<bool> CreateExpiryUrgentNotificationAsync(int productId, string productName, string batchNumber, DateTime expiryDate, int currentStock, int? branchId = null);

        /// <summary>
        /// Create critical notification for expired products
        /// </summary>
        Task<bool> CreateExpiryExpiredNotificationAsync(int productId, string productName, string batchNumber, DateTime expiryDate, int currentStock, int? branchId = null);

        /// <summary>
        /// Create notification for products requiring expiry date input
        /// </summary>
        Task<bool> CreateExpiryRequiredNotificationAsync(int productId, string productName, string categoryName);

        /// <summary>
        /// Create notification for successful product disposal
        /// </summary>
        Task<bool> CreateDisposalCompletedNotificationAsync(int disposedCount, decimal valueLost, string disposalMethod, int? branchId = null);

        /// <summary>
        /// Broadcast expiry summary to managers (daily summary)
        /// </summary>
        Task<bool> BroadcastDailyExpirySummaryAsync(int expiringCount, int expiredCount, decimal valueAtRisk, decimal valueLost, int? branchId = null);

        /// <summary>
        /// Create FIFO recommendation notification
        /// </summary>
        Task<bool> CreateFifoRecommendationNotificationAsync(int productId, string productName, string recommendedAction, int? branchId = null);
    }

    public class NotificationSettingsDto
    {
        public bool EmailEnabled { get; set; }
        public bool EmailLowStock { get; set; }
        public bool EmailMonthlyReport { get; set; }
        public bool EmailSystemUpdates { get; set; }
        public bool InAppEnabled { get; set; }
        public bool InAppLowStock { get; set; }
        public bool InAppSales { get; set; }
        public bool InAppSystem { get; set; }
        public int LowStockThreshold { get; set; }
        public TimeSpan QuietHoursStart { get; set; }
        public TimeSpan QuietHoursEnd { get; set; }

        // ✅ NEW: Expiry Notification Settings
        public bool EmailExpiry { get; set; } = true;
        public bool InAppExpiry { get; set; } = true;
        public bool ExpiryWarning7Days { get; set; } = true;
        public bool ExpiryUrgent3Days { get; set; } = true;
        public bool ExpiryExpiredAlert { get; set; } = true;
        public bool ExpiryDailySummary { get; set; } = true;
        public bool FifoRecommendations { get; set; } = true;
    }

    public class UpdateNotificationSettingsRequest
    {
        public bool EmailEnabled { get; set; }
        public bool EmailLowStock { get; set; }
        public bool EmailMonthlyReport { get; set; }
        public bool EmailSystemUpdates { get; set; }
        public bool InAppEnabled { get; set; }
        public bool InAppLowStock { get; set; }
        public bool InAppSales { get; set; }
        public bool InAppSystem { get; set; }
        public int LowStockThreshold { get; set; }
        public TimeSpan QuietHoursStart { get; set; }
        public TimeSpan QuietHoursEnd { get; set; }

        // ✅ NEW: Expiry Notification Settings
        public bool EmailExpiry { get; set; } = true;
        public bool InAppExpiry { get; set; } = true;
        public bool ExpiryWarning7Days { get; set; } = true;
        public bool ExpiryUrgent3Days { get; set; } = true;
        public bool ExpiryExpiredAlert { get; set; } = true;
        public bool ExpiryDailySummary { get; set; } = true;
        public bool FifoRecommendations { get; set; } = true;
    }

    // ==================== EXPIRY NOTIFICATION CONSTANTS ==================== //
    
    /// <summary>
    /// Constants for expiry notification types
    /// </summary>
    public static class ExpiryNotificationTypes
    {
        public const string EXPIRY_WARNING = "expiry-warning";
        public const string EXPIRY_URGENT = "expiry-urgent";
        public const string EXPIRY_EXPIRED = "expiry-expired";
        public const string EXPIRY_REQUIRED = "expiry-required";
        public const string DISPOSAL_COMPLETED = "disposal-completed";
        public const string EXPIRY_DAILY_SUMMARY = "expiry-daily-summary";
        public const string FIFO_RECOMMENDATION = "fifo-recommendation";
    }
}