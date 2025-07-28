// Models/NotificationSettings.cs - User preferences for notifications (FIXED)
using System.ComponentModel.DataAnnotations;

namespace Berca_Backend.Models
{
    public class NotificationSettings
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;

        // Email Notifications
        public bool EmailEnabled { get; set; } = true;
        public bool EmailLowStock { get; set; } = true;
        public bool EmailMonthlyReport { get; set; } = true;
        public bool EmailSystemUpdates { get; set; } = true;

        // In-App Notifications  
        public bool InAppEnabled { get; set; } = true;
        public bool InAppLowStock { get; set; } = true;
        public bool InAppSales { get; set; } = true;
        public bool InAppSystem { get; set; } = true;

        // Push Notifications (future)
        public bool PushEnabled { get; set; } = false;
        public string? PushToken { get; set; }

        // Frequency Settings
        public int LowStockThreshold { get; set; } = 5; // Alert when stock <= this value
        public TimeSpan QuietHoursStart { get; set; } = new TimeSpan(22, 0, 0); // 10 PM
        public TimeSpan QuietHoursEnd { get; set; } = new TimeSpan(7, 0, 0);   // 7 AM

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}