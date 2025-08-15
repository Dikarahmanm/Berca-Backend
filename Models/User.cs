using System.ComponentModel.DataAnnotations;

namespace Berca_Backend.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Role { get; set; } = string.Empty;

        // NEW: Branch Assignment
        public int? BranchId { get; set; }
        public virtual Branch? Branch { get; set; }

        // NEW: Multi-branch access untuk Manager
        public bool CanAccessMultipleBranches { get; set; } = false;

        // NEW: Array of branch IDs yang bisa diakses (untuk HeadManager)
        [MaxLength(500)]
        public string? AccessibleBranchIds { get; set; } // JSON array: "[1,2,3]"

        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;

        // Add missing audit properties
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // ✅ Navigation properties
        public virtual UserProfile? UserProfile { get; set; }
        public virtual UserNotificationSettings? UserNotificationSettings { get; set; }
        // Helper Methods
        public List<int> GetAccessibleBranchIds()
        {
            if (string.IsNullOrEmpty(AccessibleBranchIds)) return new List<int>();
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<int>>(AccessibleBranchIds) ?? new List<int>();
            }
            catch
            {
                return new List<int>();
            }
        }

        public void SetAccessibleBranchIds(List<int> branchIds)
        {
            AccessibleBranchIds = System.Text.Json.JsonSerializer.Serialize(branchIds);
        }

        public bool CanAccessBranch(int branchId)
        {
            // Admin bisa akses semua branch
            if (Role == "Admin") return true;

            // User assigned ke branch ini
            if (BranchId == branchId) return true;

            // User punya multi-branch access dan branch ada di list
            if (CanAccessMultipleBranches && GetAccessibleBranchIds().Contains(branchId)) return true;

            return false;
        }
    }
    
}