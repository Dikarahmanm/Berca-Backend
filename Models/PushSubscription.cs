using System.ComponentModel.DataAnnotations;

namespace Berca_Backend.Models
{
    /// <summary>
    /// Push notification subscription model for PWA notifications
    /// Stores browser push subscription data for each user
    /// </summary>
    public class PushSubscription
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// User ID who owns this subscription
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Push service endpoint URL
        /// </summary>
        [Required]
        [MaxLength(500)]
        public required string Endpoint { get; set; }

        /// <summary>
        /// Public key for message encryption (P-256 ECDH)
        /// </summary>
        [Required]
        [MaxLength(100)]
        public required string P256dh { get; set; }

        /// <summary>
        /// Authentication secret for message encryption
        /// </summary>
        [Required]
        [MaxLength(50)]
        public required string Auth { get; set; }

        /// <summary>
        /// User agent string for tracking browser/device
        /// </summary>
        [MaxLength(200)]
        public string? UserAgent { get; set; }

        /// <summary>
        /// Device identifier for multiple device support
        /// </summary>
        [MaxLength(100)]
        public string? DeviceId { get; set; }

        /// <summary>
        /// Whether this subscription is currently active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// When this subscription was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When this subscription was last used
        /// </summary>
        public DateTime? LastUsedAt { get; set; }

        /// <summary>
        /// Branch ID for branch-specific notifications (optional)
        /// </summary>
        public int? BranchId { get; set; }

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual Branch? Branch { get; set; }
    }

    /// <summary>
    /// Notification template for standardized push notifications
    /// Supports Indonesian business context with proper formatting
    /// </summary>
    public class NotificationTemplate
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Unique template identifier key
        /// </summary>
        [Required]
        [MaxLength(50)]
        public required string TemplateKey { get; set; }

        /// <summary>
        /// Notification title in Indonesian
        /// </summary>
        [Required]
        [MaxLength(100)]
        public required string Title { get; set; }

        /// <summary>
        /// Notification body message in Indonesian
        /// </summary>
        [Required]
        [MaxLength(500)]
        public required string Body { get; set; }

        /// <summary>
        /// Icon URL for the notification
        /// </summary>
        [MaxLength(200)]
        public string? Icon { get; set; }

        /// <summary>
        /// Notification priority level
        /// </summary>
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

        /// <summary>
        /// Whether notification requires user interaction to dismiss
        /// </summary>
        public bool RequireInteraction { get; set; } = false;

        /// <summary>
        /// Custom vibration pattern (for mobile devices)
        /// </summary>
        [MaxLength(50)]
        public string? VibrationPattern { get; set; }

        /// <summary>
        /// Custom sound URL for the notification
        /// </summary>
        [MaxLength(200)]
        public string? SoundUrl { get; set; }

        /// <summary>
        /// Action buttons configuration (JSON)
        /// </summary>
        [MaxLength(1000)]
        public string? Actions { get; set; }

        /// <summary>
        /// Template category for organization
        /// </summary>
        public NotificationCategory Category { get; set; } = NotificationCategory.General;

        /// <summary>
        /// Whether this template is currently active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// When this template was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When this template was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Push notification delivery log for tracking
    /// </summary>
    public class PushNotificationLog
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Push subscription that received the notification
        /// </summary>
        public int PushSubscriptionId { get; set; }

        /// <summary>
        /// Template used for this notification
        /// </summary>
        public int? NotificationTemplateId { get; set; }

        /// <summary>
        /// Actual notification title sent
        /// </summary>
        [Required]
        [MaxLength(100)]
        public required string Title { get; set; }

        /// <summary>
        /// Actual notification body sent
        /// </summary>
        [Required]
        [MaxLength(500)]
        public required string Body { get; set; }

        /// <summary>
        /// Notification priority used
        /// </summary>
        public NotificationPriority Priority { get; set; }

        /// <summary>
        /// Whether notification was delivered successfully
        /// </summary>
        public bool DeliverySuccess { get; set; }

        /// <summary>
        /// Error message if delivery failed
        /// </summary>
        [MaxLength(500)]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// HTTP response status code from push service
        /// </summary>
        public int? ResponseStatusCode { get; set; }

        /// <summary>
        /// When notification was sent
        /// </summary>
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When notification was clicked/dismissed (if tracked)
        /// </summary>
        public DateTime? ClickedAt { get; set; }

        /// <summary>
        /// Related entity ID for business context
        /// </summary>
        public int? RelatedEntityId { get; set; }

        /// <summary>
        /// Related entity type (Product, Facture, Member, etc.)
        /// </summary>
        [MaxLength(50)]
        public string? RelatedEntityType { get; set; }

        // Navigation properties
        public virtual PushSubscription PushSubscription { get; set; } = null!;
        public virtual NotificationTemplate? NotificationTemplate { get; set; }
    }

    /// <summary>
    /// Notification categories for organization and filtering
    /// </summary>
    public enum NotificationCategory
    {
        General = 0,
        ProductExpiry = 1,
        FactureDue = 2,
        MemberCredit = 3,
        Inventory = 4,
        Sales = 5,
        System = 6,
        Security = 7
    }
}