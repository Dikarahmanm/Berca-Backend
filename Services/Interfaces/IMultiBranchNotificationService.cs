using Berca_Backend.DTOs;
using Berca_Backend.Models;

namespace Berca_Backend.Services.Interfaces
{
    /// <summary>
    /// Service interface for multi-branch notification management
    /// </summary>
    public interface IMultiBranchNotificationService
    {
        // === CRUD OPERATIONS ===

        /// <summary>
        /// Get notifications with filtering and pagination
        /// </summary>
        Task<PaginatedNotificationResponse> GetNotificationsAsync(NotificationFilters filters, int currentUserId);

        /// <summary>
        /// Get notification by ID
        /// </summary>
        Task<NotificationDto?> GetNotificationByIdAsync(string id, int currentUserId);

        /// <summary>
        /// Create new notification
        /// </summary>
        Task<NotificationDto> CreateNotificationAsync(CreateNotificationRequest request, int createdBy);

        /// <summary>
        /// Update existing notification
        /// </summary>
        Task<bool> UpdateNotificationAsync(string id, UpdateNotificationRequest request, int currentUserId);

        /// <summary>
        /// Delete notification
        /// </summary>
        Task<bool> DeleteNotificationAsync(string id, int currentUserId);

        // === READ STATUS MANAGEMENT ===

        /// <summary>
        /// Mark notification as read
        /// </summary>
        Task<bool> MarkAsReadAsync(string id, int userId);

        /// <summary>
        /// Mark multiple notifications as read
        /// </summary>
        Task<bool> MarkBulkAsReadAsync(List<string> ids, int userId);

        /// <summary>
        /// Mark all notifications as read for user/branch
        /// </summary>
        Task<bool> MarkAllAsReadAsync(int? branchId, int userId);

        /// <summary>
        /// Archive notification
        /// </summary>
        Task<bool> ArchiveNotificationAsync(string id, int currentUserId);

        // === STATISTICS & ANALYTICS ===

        /// <summary>
        /// Get notification statistics
        /// </summary>
        Task<NotificationStatsDto> GetNotificationStatsAsync(int? branchId, DateTime? dateFrom, DateTime? dateTo, int currentUserId);

        /// <summary>
        /// Get system health metrics
        /// </summary>
        Task<NotificationHealthDto> GetSystemHealthAsync();

        // === EXPORT FUNCTIONALITY ===

        /// <summary>
        /// Export notifications to Excel/CSV
        /// </summary>
        Task<byte[]> ExportNotificationsAsync(ExportNotificationRequest request, int currentUserId);

        // === REAL-TIME FEATURES ===

        /// <summary>
        /// Get unread notification count for user
        /// </summary>
        Task<int> GetUnreadCountAsync(int userId, int? branchId = null);

        /// <summary>
        /// Get recent notifications for real-time updates
        /// </summary>
        Task<List<NotificationDto>> GetRecentNotificationsAsync(int userId, int? branchId = null, int limit = 10);

        // === NOTIFICATION CREATION HELPERS ===

        /// <summary>
        /// Create system notification
        /// </summary>
        Task<NotificationDto> CreateSystemNotificationAsync(string title, string message, string priority = "medium", int? branchId = null, Dictionary<string, object>? metadata = null);

        /// <summary>
        /// Create transfer notification
        /// </summary>
        Task<NotificationDto> CreateTransferNotificationAsync(string title, string message, int sourceBranchId, int targetBranchId, string transferId, string priority = "medium");

        /// <summary>
        /// Create coordination alert
        /// </summary>
        Task<NotificationDto> CreateCoordinationAlertAsync(string title, string message, int? branchId, string severity = "warning", string priority = "high", Dictionary<string, object>? metadata = null);

        /// <summary>
        /// Create inventory alert
        /// </summary>
        Task<NotificationDto> CreateInventoryAlertAsync(string title, string message, int branchId, int productId, string severity = "warning", Dictionary<string, object>? metadata = null);

        // === TEMPLATE MANAGEMENT ===

        /// <summary>
        /// Create notification from template
        /// </summary>
        Task<NotificationDto> CreateFromTemplateAsync(string templateName, Dictionary<string, object> parameters, int? branchId = null, int? userId = null);

        // === CLEANUP & MAINTENANCE ===

        /// <summary>
        /// Clean up expired notifications
        /// </summary>
        Task<int> CleanupExpiredNotificationsAsync();

        /// <summary>
        /// Archive old notifications
        /// </summary>
        Task<int> ArchiveOldNotificationsAsync(int daysOld = 30);

        // === ACCESS CONTROL ===

        /// <summary>
        /// Check if user can access notification
        /// </summary>
        Task<bool> CanUserAccessNotificationAsync(string notificationId, int userId);

        /// <summary>
        /// Check if user can access branch notifications
        /// </summary>
        Task<bool> CanUserAccessBranchAsync(int branchId, int userId);

        // === PREFERENCE MANAGEMENT ===

        /// <summary>
        /// Get user notification preferences
        /// </summary>
        Task<List<NotificationPreference>> GetUserPreferencesAsync(int userId);

        /// <summary>
        /// Update user notification preferences
        /// </summary>
        Task<bool> UpdateUserPreferencesAsync(int userId, List<NotificationPreference> preferences);

        // === SPECIALIZED NOTIFICATION CREATION METHODS ===
        // (Migrated from Basic NotificationService)

        /// <summary>
        /// Create low stock notification
        /// </summary>
        Task<bool> CreateLowStockNotificationAsync(int productId, int currentStock, int? branchId = null);

        /// <summary>
        /// Create monthly revenue notification
        /// </summary>
        Task<bool> CreateMonthlyRevenueNotificationAsync(decimal revenue, DateTime month, int? branchId = null);

        /// <summary>
        /// Create system maintenance notification
        /// </summary>
        Task<bool> CreateSystemMaintenanceNotificationAsync(DateTime scheduledTime, string message, int? branchId = null);

        /// <summary>
        /// Broadcast notification to all users
        /// </summary>
        Task<bool> BroadcastToAllUsersAsync(string type, string title, string message, int? branchId = null, string? actionUrl = null);

        /// <summary>
        /// Broadcast notification to specific role
        /// </summary>
        Task<bool> BroadcastToRoleAsync(string role, string type, string title, string message, int? branchId = null, string? actionUrl = null);

        // === PRODUCT & STOCK NOTIFICATIONS ===

        /// <summary>
        /// Create out of stock notification
        /// </summary>
        Task<bool> CreateOutOfStockNotificationAsync(int productId, int? branchId = null);

        /// <summary>
        /// Create sale completed notification
        /// </summary>
        Task<bool> CreateSaleCompletedNotificationAsync(int saleId, string saleNumber, decimal totalAmount, int? branchId = null);

        /// <summary>
        /// Create stock adjustment notification
        /// </summary>
        Task<bool> CreateStockAdjustmentNotificationAsync(int productId, int quantity, string notes, int? branchId = null);

        // === EXPIRY MANAGEMENT NOTIFICATIONS ===

        /// <summary>
        /// Create expiry warning notification (7 days)
        /// </summary>
        Task<bool> CreateExpiryWarningNotificationAsync(int productId, string productName, string batchNumber, DateTime expiryDate, int currentStock, int? branchId = null);

        /// <summary>
        /// Create urgent expiry notification (3 days)
        /// </summary>
        Task<bool> CreateExpiryUrgentNotificationAsync(int productId, string productName, string batchNumber, DateTime expiryDate, int currentStock, int? branchId = null);

        /// <summary>
        /// Create expired product notification
        /// </summary>
        Task<bool> CreateExpiryExpiredNotificationAsync(int productId, string productName, string batchNumber, DateTime expiryDate, int currentStock, int? branchId = null);

        /// <summary>
        /// Create disposal completed notification
        /// </summary>
        Task<bool> CreateDisposalCompletedNotificationAsync(int disposedCount, decimal valueLost, string disposalMethod, int? branchId = null);

        /// <summary>
        /// Broadcast daily expiry summary
        /// </summary>
        Task<bool> BroadcastDailyExpirySummaryAsync(int expiringCount, int expiredCount, decimal valueAtRisk, decimal valueLost, int? branchId = null);

        // === MEMBER CREDIT NOTIFICATIONS ===

        /// <summary>
        /// Create member debt overdue notification
        /// </summary>
        Task<bool> CreateMemberDebtOverdueNotificationAsync(int memberId, string memberName, decimal overdueAmount, int daysOverdue, int? branchId = null);

        /// <summary>
        /// Create member credit limit warning
        /// </summary>
        Task<bool> CreateMemberCreditLimitWarningNotificationAsync(int memberId, string memberName, decimal currentDebt, decimal creditLimit, int? branchId = null);

        /// <summary>
        /// Create payment received notification
        /// </summary>
        Task<bool> CreatePaymentReceivedNotificationAsync(int memberId, string memberName, decimal paymentAmount, int? branchId = null);

        // === SUPPLIER & FACTURE NOTIFICATIONS ===

        /// <summary>
        /// Create facture due today notification
        /// </summary>
        Task<bool> CreateFactureDueTodayNotificationAsync(string factureNumber, string supplierName, decimal amount, int? branchId = null);

        /// <summary>
        /// Create facture overdue notification
        /// </summary>
        Task<bool> CreateFactureOverdueNotificationAsync(string factureNumber, string supplierName, decimal amount, int daysOverdue, int? branchId = null);

        /// <summary>
        /// Create supplier payment completed notification
        /// </summary>
        Task<bool> CreateSupplierPaymentCompletedNotificationAsync(string supplierName, decimal amount, string factureNumber, int? branchId = null);

        /// <summary>
        /// Create facture approval required notification
        /// </summary>
        Task<bool> CreateFactureApprovalRequiredNotificationAsync(string factureNumber, string supplierName, decimal amount, int requestedByUserId, int? branchId = null);

        /// <summary>
        /// Create sale cancelled notification
        /// </summary>
        Task<bool> CreateSaleCancelledNotificationAsync(int saleId, string saleNumber, decimal totalAmount, string reason, int? branchId = null);

        /// <summary>
        /// Create sale refunded notification
        /// </summary>
        Task<bool> CreateSaleRefundedNotificationAsync(int saleId, string saleNumber, decimal totalAmount, string reason, int? branchId = null);
    }
}