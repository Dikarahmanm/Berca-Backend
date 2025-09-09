// DTOs/NotificationDTOs.cs - Sprint 2 Notification DTOs (FIXED)
using System.ComponentModel.DataAnnotations;
using Berca_Backend.Models; // ✅ ADDED: Missing import

namespace Berca_Backend.DTOs
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public string? ActionUrl { get; set; }
        public string? ActionText { get; set; }
        public DateTime CreatedAt { get; set; }
        public string TimeAgo { get; set; } = string.Empty;
        public bool IsExpired { get; set; }
        
        // Multi-branch specific fields
        public int? BranchId { get; set; }
        public string? BranchName { get; set; }
        public string? UserName { get; set; }
        public string Severity { get; set; } = "info";
        public bool IsArchived { get; set; }
        public bool ActionRequired { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    public class CreateNotificationRequest
    {
        public int? UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = string.Empty;

        public string Priority { get; set; } = "Normal";

        [StringLength(500)]
        public string? ActionUrl { get; set; }

        [StringLength(100)]
        public string? ActionText { get; set; }

        public DateTime? ExpiryDate { get; set; }

        // Multi-branch support
        public int? BranchId { get; set; }
        public string Severity { get; set; } = "info";
        public bool ActionRequired { get; set; } = false;
    }

    public class NotificationSummaryDto
    {
        public int TotalCount { get; set; }
        public int UnreadCount { get; set; }
        public List<NotificationDto> RecentNotifications { get; set; } = new(); // ✅ Fixed name
    }

    // ==================== ADDITIONAL DTOs FOR EXPIRY SYSTEM ==================== //

    /// <summary>
    /// DTO for creating notifications (alias for CreateNotificationRequest)
    /// </summary>
    public class CreateNotificationDto : CreateNotificationRequest
    {
        public new int? BranchId { get; set; } // Added 'new' keyword
        public bool IsSystemNotification { get; set; }
        public bool RequiresAction { get; set; }
        public object? Metadata { get; set; }
    }

    // ==================== MULTI-BRANCH NOTIFICATION DTOs ==================== //

    /// <summary>
    /// Request DTO for updating existing notifications
    /// </summary>
    public class UpdateNotificationRequest
    {
        public bool? IsRead { get; set; }
        public bool? IsArchived { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// Request DTO for bulk operations
    /// </summary>
    public class BulkNotificationRequest
    {
        [Required]
        public List<string> Ids { get; set; } = new();
    }

    /// <summary>
    /// Filter parameters for notification queries
    /// </summary>
    public class NotificationFilters
    {
        public int? BranchId { get; set; }
        public string? Type { get; set; }
        public string? Severity { get; set; }
        public string? Priority { get; set; }
        public bool? IsRead { get; set; }
        public bool? ActionRequired { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int? UserId { get; set; }

        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        [Range(1, 100)]
        public int PageSize { get; set; } = 50;

        public string SortBy { get; set; } = "CreatedAt";
        public string SortOrder { get; set; } = "desc";
    }

    /// <summary>
    /// Statistics DTO for notification metrics
    /// </summary>
    public class NotificationStatsDto
    {
        public int Total { get; set; }
        public int Unread { get; set; }
        public Dictionary<string, int> ByType { get; set; } = new();
        public Dictionary<string, int> BySeverity { get; set; } = new();
        public Dictionary<string, int> ByPriority { get; set; } = new();
        public int ActionRequired { get; set; }
        public int Archived { get; set; }
        public List<NotificationTrendDto> Trends { get; set; } = new();
    }

    /// <summary>
    /// Trend data for statistics
    /// </summary>
    public class NotificationTrendDto
    {
        public string Date { get; set; } = string.Empty;
        public int Count { get; set; }
        public int Unread { get; set; }
    }

    /// <summary>
    /// Health check DTO for notification system
    /// </summary>
    public class NotificationHealthDto
    {
        public string Status { get; set; } = "healthy";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Version { get; set; } = "1.0.0";
        public DatabaseHealthDto Database { get; set; } = new();
        public SignalRHealthDto SignalR { get; set; } = new();
        public PerformanceHealthDto Performance { get; set; } = new();
    }

    public class DatabaseHealthDto
    {
        public bool Connected { get; set; }
        public int Notifications { get; set; }
    }

    public class SignalRHealthDto
    {
        public bool Connected { get; set; }
        public int Connections { get; set; }
    }

    public class PerformanceHealthDto
    {
        public double AverageResponseTime { get; set; }
        public int RequestsPerMinute { get; set; }
    }

    /// <summary>
    /// Paginated response wrapper
    /// </summary>
    public class PaginatedNotificationResponse
    {
        public List<NotificationDto> Data { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    /// <summary>
    /// Real-time notification update DTO
    /// </summary>
    public class NotificationUpdateDto
    {
        public string Action { get; set; } = string.Empty; // created, updated, deleted, marked_read
        public NotificationDto? Notification { get; set; }
        public List<string>? NotificationIds { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public int? BranchId { get; set; }
        public int? UserId { get; set; }
    }

    /// <summary>
    /// Export request DTO
    /// </summary>
    public class ExportNotificationRequest : NotificationFilters
    {
        public string Format { get; set; } = "excel"; // csv, excel
        public bool IncludeMetadata { get; set; } = false;
    }

    /// <summary>
    /// Notification type enumeration
    /// </summary>
    public static class NotificationType
    {
        public const string System = "system";
        public const string Transfer = "transfer";
        public const string Alert = "alert";
        public const string User = "user";
        public const string Branch = "branch";
        public const string Coordination = "coordination";
        public const string Inventory = "inventory";
        public const string Sales = "sales";
        public const string Financial = "financial";
        public const string Maintenance = "maintenance";
    }

    /// <summary>
    /// Notification severity enumeration
    /// </summary>
    public static class NotificationSeverity
    {
        public const string Info = "info";
        public const string Warning = "warning";
        public const string Error = "error";
        public const string Success = "success";
    }


}