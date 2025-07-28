// DTOs/NotificationDTOs.cs - Sprint 2 Notification DTOs (FIXED)
using System.ComponentModel.DataAnnotations; // ✅ ADDED: Missing import

namespace Berca_Backend.DTOs
{
    public class NotificationDto
    {
        public int Id { get; set; }
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
    }

    public class NotificationSummaryDto
    {
        public int TotalCount { get; set; }
        public int UnreadCount { get; set; }
        public List<NotificationDto> RecentNotifications { get; set; } = new(); // ✅ Fixed name
    }
}