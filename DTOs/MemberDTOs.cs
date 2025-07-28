// DTOs/MemberDTOs.cs - Sprint 2 Member DTOs (FIXED)
using System.ComponentModel.DataAnnotations; // ✅ ADDED

namespace Berca_Backend.DTOs
{
    // Member Response DTO
    public class MemberDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Address { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string MemberNumber { get; set; } = string.Empty;
        public string Tier { get; set; } = string.Empty;
        public DateTime JoinDate { get; set; }
        public bool IsActive { get; set; }
        public int TotalPoints { get; set; }
        public int UsedPoints { get; set; }
        public int AvailablePoints { get; set; }
        public decimal TotalSpent { get; set; }
        public int TotalTransactions { get; set; }
        public DateTime? LastTransactionDate { get; set; }
        public decimal AverageTransactionValue { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }


    // Create Member Request
    public class CreateMemberRequest
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        [Phone]
        public string Phone { get; set; } = string.Empty;

        [StringLength(100)]
        [EmailAddress]
        public string? Email { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [StringLength(10)]
        public string? Gender { get; set; }
    }

    // Update Member Request
    public class UpdateMemberRequest
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required] // ✅ Added missing Phone property
        [StringLength(20)]
        [Phone]
        public string Phone { get; set; } = string.Empty;

        [StringLength(100)]
        [EmailAddress]
        public string? Email { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [StringLength(10)]
        public string? Gender { get; set; }

        public bool IsActive { get; set; }
    }

    // Point Transaction Request
    public class PointTransactionRequest
    {
        [Required]
        public int Points { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [StringLength(50)]
        public string? ReferenceNumber { get; set; }
    }

    // Member Search Response
    public class MemberSearchResponse
    {
        public List<MemberDto> Members { get; set; } = new();
        public int TotalItems { get; set; } // ✅ Added
        public int TotalPages { get; set; } // ✅ Added
        public int CurrentPage { get; set; } // ✅ Added
        public int PageSize { get; set; } // ✅ Added
    }
    //public class MemberStatsDto
    //{
    //    public int TotalTransactions { get; set; }
    //    public decimal TotalSpent { get; set; }
    //    public decimal AverageTransactionValue { get; set; }
    //    public int TotalPoints { get; set; }
    //    public int AvailablePoints { get; set; }
    //    public DateTime? LastTransactionDate { get; set; }
    //    public DateTime MemberSince { get; set; } // ✅ Added
    //    public string CurrentTier { get; set; } = string.Empty;
    //}
    public class MemberPointDto
    {
        public int Id { get; set; }
        public int Points { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ReferenceNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsEarning { get; set; }
        public bool IsRedemption { get; set; }
    }

    public class MemberStatsDto
    {
        public int TotalTransactions { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal AverageTransactionValue { get; set; }
        public int TotalPoints { get; set; }
        public int AvailablePoints { get; set; }
        public DateTime? LastTransactionDate { get; set; }
        public string CurrentTier { get; set; } = string.Empty;
        public decimal NextTierRequirement { get; set; }
        public DateTime MemberSince { get; set; }
    }

    public class TopMemberDto
    {
        public int MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string MemberNumber { get; set; } = string.Empty;
        public int TransactionCount { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal AverageTransaction { get; set; }
        public DateTime LastTransactionDate { get; set; }
    }


}