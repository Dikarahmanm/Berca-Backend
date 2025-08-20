// DTOs/MemberDTOs.cs - Sprint 2 Member DTOs (FIXED)
using System.ComponentModel.DataAnnotations; // ✅ ADDED
using Berca_Backend.Models; // ✅ ADDED for CreditStatus enum

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

    // ==================== QUERY OPTIMIZATION DTOs ==================== //

    /// <summary>
    /// Optimized DTO for member queries with payment reminder information
    /// Reduces query complexity and improves performance with split queries
    /// </summary>
    public class MemberWithRemindersDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string MemberNumber { get; set; } = string.Empty;
        public decimal CurrentDebt { get; set; }
        public decimal CreditLimit { get; set; }
        public DateTime? NextPaymentDueDate { get; set; }
        public CreditStatus CreditStatus { get; set; }
        
        // Aggregated reminder information (avoids loading full collection)
        public int PaymentRemindersCount { get; set; }
        public DateTime? LastReminderDate { get; set; }
        
        // Computed properties
        public decimal CreditUtilization => CreditLimit > 0 ? (CurrentDebt / CreditLimit) * 100 : 0;
        public bool IsHighRisk => CreditUtilization >= 90;
        public int DaysOverdue => NextPaymentDueDate.HasValue 
            ? Math.Max(0, (DateTime.Now.Date - NextPaymentDueDate.Value.Date).Days)
            : 0;
        
        // Display properties - Indonesian formatting
        public string CurrentDebtDisplay => CurrentDebt.ToString("C", new System.Globalization.CultureInfo("id-ID"));
        public string CreditLimitDisplay => CreditLimit.ToString("C", new System.Globalization.CultureInfo("id-ID"));
        public string CreditUtilizationDisplay => $"{CreditUtilization:F1}%";
        public string CreditStatusDisplay => CreditStatus switch
        {
            CreditStatus.Good => "Baik",
            CreditStatus.Warning => "Peringatan",
            CreditStatus.Overdue => "Terlambat",
            CreditStatus.Suspended => "Ditangguhkan",
            CreditStatus.Blacklisted => "Diblokir",
            _ => "Tidak Diketahui"
        };
        public string NextPaymentDueDateDisplay => NextPaymentDueDate?.ToString("dd/MM/yyyy") ?? "N/A";
        public string LastReminderDateDisplay => LastReminderDate?.ToString("dd/MM/yyyy HH:mm") ?? "Never";
    }


}