// Models/Notification.cs - Sprint 2 Notification System (FIXED)
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // ✅ ADDED: Missing import

namespace Berca_Backend.Models
{
    public class Notification
    {
        public int Id { get; set; }

        // Target User (null = all users)
        public int? UserId { get; set; }
        public virtual User? User { get; set; }

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = string.Empty; // low-stock, monthly-revenue, audit-due, etc.

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = string.Empty;

        public NotificationPriority? Priority { get; set; }

        // Status
        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }

        public bool IsArchived { get; set; } = false;
        public DateTime? ArchivedAt { get; set; }

        // Action URL (optional)
        [StringLength(500)]
        public string? ActionUrl { get; set; }

        [StringLength(100)]
        public string? ActionText { get; set; }

        // Related Entity (optional)
        [StringLength(50)]
        public string? RelatedEntity { get; set; } // Product, Sale, etc.

        public int? RelatedEntityId { get; set; }

        // Metadata (JSON string for additional data)
        [StringLength(2000)]
        public string? Metadata { get; set; }

        // Auto-expire
        public DateTime? ExpiryDate { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }

        // Computed Properties
        [NotMapped]
        public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.UtcNow;

        [NotMapped]
        public string PriorityDisplay => Priority switch
        {
            NotificationPriority.Low => "Rendah",
            NotificationPriority.Normal => "Normal",
            NotificationPriority.High => "Tinggi",
            NotificationPriority.Critical => "Kritis",
            _ => "Normal"
        };

        [NotMapped]
        public string TimeAgo
        {
            get
            {
                var diff = DateTime.UtcNow - CreatedAt;
                if (diff.TotalMinutes < 1) return "Baru saja";
                if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} menit lalu";
                if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} jam lalu";
                if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} hari lalu";
                return CreatedAt.ToString("dd/MM/yyyy");
            }
        }
    }

    public enum NotificationPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3
    }
}