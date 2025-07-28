// Models/UserNotificationSettings.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace Berca_Backend.Models
{
    public class UserNotificationSettings
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;

        // Global toggles
        public bool EmailEnabled { get; set; } = true;
        public bool InAppEnabled { get; set; } = true;

        // Email-specific
        public bool EmailLowStock { get; set; } = true;
        public bool EmailMonthlyReport { get; set; } = true;
        public bool EmailSystemUpdates { get; set; } = true;

        // In-app-specific
        public bool InAppLowStock { get; set; } = true;
        public bool InAppSales { get; set; } = true;
        public bool InAppSystem { get; set; } = true;

        // Thresholds & quiet hours
        public int LowStockThreshold { get; set; } = 10;
        public TimeSpan QuietHoursStart { get; set; } = new TimeSpan(22, 0, 0);
        public TimeSpan QuietHoursEnd { get; set; } = new TimeSpan(6, 0, 0);

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
