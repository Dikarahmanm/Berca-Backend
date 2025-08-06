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
    }
}