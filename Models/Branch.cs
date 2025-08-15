// ==========================================
// Models/Branch.cs - Enhanced Branch Model
// ==========================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Berca_Backend.Models
{
    public class Branch
    {
        public int Id { get; set; }

        [Required, MaxLength(20)]
        public string BranchCode { get; set; } = string.Empty; // "HQ", "JKT-PUSAT", "JKT-SEL"

        [Required, MaxLength(100)]
        public string BranchName { get; set; } = string.Empty; // "Head Office", "Toko Eniwan Purwakarta"

        public int? ParentBranchId { get; set; } // Always null for branches (flat structure)
        public virtual Branch? ParentBranch { get; set; }

        [Required]
        public BranchType BranchType { get; set; } = BranchType.Branch;

        [Required, MaxLength(300)]
        public string Address { get; set; } = string.Empty; // Full store address

        [Required, MaxLength(100)]
        public string ManagerName { get; set; } = string.Empty; // Branch Manager name

        [Required, MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Email { get; set; }

        // NEW: Location details for retail chain
        [MaxLength(50)]
        public string City { get; set; } = string.Empty; // "Purwakarta", "Bandung"

        [MaxLength(50)]
        public string Province { get; set; } = string.Empty; // "Jawa Barat", "Jawa Timur"

        [MaxLength(10)]
        public string PostalCode { get; set; } = string.Empty; // "41115"

        // NEW: Store operational details
        public DateTime OpeningDate { get; set; } = DateTime.UtcNow;

        [MaxLength(20)]
        public string StoreSize { get; set; } = "Medium"; // "Small", "Medium", "Large"

        public int EmployeeCount { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual ICollection<Branch> SubBranches { get; set; } = new List<Branch>(); // Always empty for flat structure
        public virtual ICollection<User> Users { get; set; } = new List<User>();

        // Computed Properties
        [NotMapped]
        public string FullLocationName => $"{BranchName} - {City}, {Province}";

        [NotMapped]
        public bool IsHeadOffice => BranchType == BranchType.Head;

        [NotMapped]
        public bool HasUsers => Users.Any();

        // Helper Methods
        public string GetStoreDisplayName()
        {
            return BranchType == BranchType.Head ? BranchName : $"{BranchName} ({City})";
        }

        public bool IsInSameProvince(Branch otherBranch)
        {
            return Province.Equals(otherBranch.Province, StringComparison.OrdinalIgnoreCase);
        }
    }

    public enum BranchType
    {
        Head = 0,     // Head Office/Kantor Pusat
        Branch = 1    // Cabang/Store Location
    }
}