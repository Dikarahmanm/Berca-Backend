// Models/Member.cs - Sprint 2 Customer Loyalty
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Berca_Backend.Models
{
    public class Member
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(100)]
        [EmailAddress]
        public string? Email { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [StringLength(10)]
        public string? Gender { get; set; }

        // Membership Info
        [Required]
        [StringLength(20)]
        public string MemberNumber { get; set; } = string.Empty;

        public MembershipTier Tier { get; set; } = MembershipTier.Bronze;

        public DateTime JoinDate { get; set; } = DateTime.UtcNow; // ✅ Fixed: was JoinedDate

        public bool IsActive { get; set; } = true;

        // Points System
        [Range(0, int.MaxValue)]
        public int TotalPoints { get; set; } = 0;

        [Range(0, int.MaxValue)]
        public int UsedPoints { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalSpent { get; set; } = 0;

        public int TotalTransactions { get; set; } = 0;

        public DateTime? LastTransactionDate { get; set; }

        // Navigation Properties
        public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
        public virtual ICollection<MemberPoint> MemberPoints { get; set; } = new List<MemberPoint>();

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }

        // Computed Properties
        [NotMapped]
        public int AvailablePoints => TotalPoints - UsedPoints;

        [NotMapped]
        public decimal AverageTransactionValue => TotalTransactions > 0 ? TotalSpent / TotalTransactions : 0;
    }

    public enum MembershipTier
    {
        Bronze = 0,
        Silver = 1,
        Gold = 2,
        Platinum = 3
    }
    public enum MemberPointType
    {
        Earned = 0,
        Redeemed = 1,
        Expired = 2,
        Bonus = 3
    }
}