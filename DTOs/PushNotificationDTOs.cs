using System.ComponentModel.DataAnnotations;
using Berca_Backend.Models;

namespace Berca_Backend.DTOs
{
    /// <summary>
    /// DTO for push subscription from client
    /// </summary>
    public class PushSubscriptionDto
    {
        [Required]
        public string Endpoint { get; set; } = string.Empty;

        [Required]
        public PushKeysDto Keys { get; set; } = new();

        public string? UserAgent { get; set; }
        public string? DeviceId { get; set; }
        public int? BranchId { get; set; }
    }

    /// <summary>
    /// Push subscription keys (P256dh and Auth)
    /// </summary>
    public class PushKeysDto
    {
        [Required]
        public string P256dh { get; set; } = string.Empty;

        [Required]
        public string Auth { get; set; } = string.Empty;
    }

    /// <summary>
    /// Notification payload to send to client
    /// </summary>
    public class NotificationPayload
    {
        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Body { get; set; } = string.Empty;

        public string? Icon { get; set; }
        public string? Badge { get; set; }
        public string? Image { get; set; }
        public string? Tag { get; set; }
        public string? Url { get; set; }
        public bool RequireInteraction { get; set; } = false;
        public bool Silent { get; set; } = false;
        public int[]? Vibrate { get; set; }
        public Dictionary<string, object>? Data { get; set; }
        public List<NotificationActionDto>? Actions { get; set; }
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    }

    /// <summary>
    /// Notification action button
    /// </summary>
    public class NotificationActionDto
    {
        [Required]
        public string Action { get; set; } = string.Empty;

        [Required]
        public string Title { get; set; } = string.Empty;

        public string? Icon { get; set; }
    }

    /// <summary>
    /// Bulk notification request
    /// </summary>
    public class BulkNotificationRequest
    {
        [Required]
        public NotificationPayload Payload { get; set; } = new();

        public List<int>? UserIds { get; set; }
        public List<string>? Roles { get; set; }
        public int? BranchId { get; set; }
        public NotificationCategory? Category { get; set; }
        public bool IncludeOfflineUsers { get; set; } = true;
    }

    /// <summary>
    /// Push subscription status response
    /// </summary>
    public class PushSubscriptionStatusDto
    {
        public bool IsSubscribed { get; set; }
        public DateTime? SubscribedAt { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public string? DeviceInfo { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Notification template DTO
    /// </summary>
    public class NotificationTemplateDto
    {
        public int Id { get; set; }
        public string TemplateKey { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public NotificationPriority Priority { get; set; }
        public bool RequireInteraction { get; set; }
        public NotificationCategory Category { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Create notification template DTO
    /// </summary>
    public class CreateNotificationTemplateDto
    {
        [Required]
        [MaxLength(50)]
        public string TemplateKey { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Body { get; set; } = string.Empty;

        public string? Icon { get; set; }
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
        public bool RequireInteraction { get; set; } = false;
        public NotificationCategory Category { get; set; } = NotificationCategory.General;
        public string? VibrationPattern { get; set; }
        public string? SoundUrl { get; set; }
        public List<NotificationActionDto>? Actions { get; set; }
    }

    /// <summary>
    /// Offline sync request for PWA
    /// </summary>
    public class OfflineSyncRequest
    {
        public List<OfflineSyncItem> Items { get; set; } = new();
        public string? ClientVersion { get; set; }
        public DateTime LastSyncTime { get; set; }
    }

    /// <summary>
    /// Individual offline sync item
    /// </summary>
    public class OfflineSyncItem
    {
        [Required]
        public string EntityType { get; set; } = string.Empty; // "Sale", "Product", etc.

        [Required]
        public string Action { get; set; } = string.Empty; // "CREATE", "UPDATE", "DELETE"

        [Required]
        public Dictionary<string, object> Data { get; set; } = new();

        public string? ClientId { get; set; } // Client-generated ID for tracking
        public DateTime Timestamp { get; set; }
        public int? BranchId { get; set; }
    }

    /// <summary>
    /// PWA version information
    /// </summary>
    public class PWAVersionDto
    {
        public string Version { get; set; } = string.Empty;
        public string BuildNumber { get; set; } = string.Empty;
        public DateTime BuildDate { get; set; }
        public List<string> CriticalResources { get; set; } = new();
        public bool ForceUpdate { get; set; } = false;
        public string? UpdateMessage { get; set; }
    }

    /// <summary>
    /// Push notification delivery result
    /// </summary>
    public class PushNotificationResult
    {
        public bool Success { get; set; }
        public int TotalSent { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<PushDeliveryError> Errors { get; set; } = new();
    }

    /// <summary>
    /// Individual push delivery error
    /// </summary>
    public class PushDeliveryError
    {
        public int UserId { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public int? StatusCode { get; set; }
        public bool IsRetryable { get; set; }
    }

    /// <summary>
    /// Business notification context for automatic notifications
    /// </summary>
    public class BusinessNotificationContext
    {
        public string NotificationType { get; set; } = string.Empty;
        public int? RelatedEntityId { get; set; }
        public string? RelatedEntityType { get; set; }
        public Dictionary<string, object> TemplateData { get; set; } = new();
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
        public List<string> TargetRoles { get; set; } = new();
        public int? BranchId { get; set; }
    }

    /// <summary>
    /// DTO for failed notification retry processing
    /// </summary>
    public class FailedNotificationDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? OriginalPayload { get; set; }
        public int AttemptCount { get; set; }
        public string? LastError { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime NextRetryAt { get; set; }
        public NotificationPriority Priority { get; set; }
        
        // Display properties for Indonesian context
        public string CreatedAtDisplay => CreatedAt.ToString("dd/MM/yyyy HH:mm:ss");
        public string NextRetryAtDisplay => NextRetryAt.ToString("dd/MM/yyyy HH:mm:ss");
        public string PriorityDisplay => Priority switch
        {
            NotificationPriority.Critical => "Kritis",
            NotificationPriority.High => "Tinggi",
            NotificationPriority.Normal => "Normal",
            NotificationPriority.Low => "Rendah",
            _ => "Normal"
        };
        public bool CanRetry => AttemptCount < 3 && NextRetryAt <= DateTime.UtcNow;
    }
}