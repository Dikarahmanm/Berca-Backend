// Models/Member.cs - Sprint 2 Customer Loyalty
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Berca_Backend.Models
{
    public class Member
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Member name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        [RegularExpression(@"^[0-9+\-\s()]+$", ErrorMessage = "Invalid phone number format")]
        public string Phone { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string? Email { get; set; }

        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        public string? Address { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [StringLength(10)]
        public string? Gender { get; set; } // Male, Female, Other

        // Membership Info
        [Required]
        [StringLength(20)]
        public string MemberNumber { get; set; } = string.Empty; // e.g., "MBR20250728001"

        public MembershipTier Tier { get; set; } = MembershipTier.Bronze;

        public DateTime JoinDate { get; set; } = DateTime.UtcNow;

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

        [NotMapped]
        public string TierDisplay => Tier switch
        {
            MembershipTier.Bronze => "Bronze",
            MembershipTier.Silver => "Silver",
            MembershipTier.Gold => "Gold",
            MembershipTier.Platinum => "Platinum",
            _ => "Bronze"
        };
    }

    public enum MembershipTier
    {
        Bronze = 0,    // 0 - 999,999
        Silver = 1,    // 1,000,000 - 4,999,999  
        Gold = 2,      // 5,000,000 - 9,999,999
        Platinum = 3   // 10,000,000+
    }
}