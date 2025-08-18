using System.ComponentModel.DataAnnotations;
using Berca_Backend.Models;

namespace Berca_Backend.DTOs
{
    // ==================== CREDIT TRANSACTION DTOs ==================== //

    /// <summary>
    /// DTO for member credit transaction data
    /// </summary>
    public class MemberCreditTransactionDto
    {
        public int Id { get; set; }
        public int MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public CreditTransactionType Type { get; set; }
        public string TypeDescription { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public DateTime? DueDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? ReferenceNumber { get; set; }
        public CreditTransactionStatus Status { get; set; }
        public string StatusDescription { get; set; } = string.Empty;
        public string? BranchName { get; set; }
        public string CreatedByUserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        
        // Computed Properties
        public bool IncreasesDebt { get; set; }
        public bool ReducesDebt { get; set; }
        public int? DaysUntilDue { get; set; }
        public bool IsOverdue { get; set; }
        public string FormattedAmount { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for creating new credit transaction
    /// </summary>
    public class CreateCreditTransactionDto
    {
        [Required]
        public int MemberId { get; set; }

        [Required]
        public CreditTransactionType Type { get; set; }

        [Required]
        [Range(0.01, 100000000, ErrorMessage = "Amount must be between 0.01 and 100,000,000 IDR")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; } = string.Empty;

        [StringLength(100)]
        public string? ReferenceNumber { get; set; }

        public DateTime? DueDate { get; set; }

        public int? BranchId { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }
    }

    // ==================== CREDIT SUMMARY DTOs ==================== //

    /// <summary>
    /// Comprehensive credit summary for member dashboard
    /// </summary>
    public class MemberCreditSummaryDto
    {
        public int MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string MemberNumber { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public MembershipTier Tier { get; set; }
        
        // Credit Information
        public decimal CreditLimit { get; set; }
        public decimal CurrentDebt { get; set; }
        public decimal AvailableCredit { get; set; }
        public decimal CreditUtilization { get; set; }
        public CreditStatus Status { get; set; }
        public string StatusDescription { get; set; } = string.Empty;
        
        // Payment Information
        public int PaymentTerms { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        public DateTime? NextPaymentDueDate { get; set; }
        public int DaysOverdue { get; set; }
        public decimal OverdueAmount { get; set; }
        
        // Risk Assessment
        public int CreditScore { get; set; }
        public string CreditGrade { get; set; } = string.Empty;
        public decimal PaymentSuccessRate { get; set; }
        public int PaymentDelays { get; set; }
        public decimal LifetimeDebt { get; set; }
        
        // Recent Activity
        public List<MemberCreditTransactionDto> RecentTransactions { get; set; } = new();
        public int RemindersSent { get; set; }
        public DateTime? LastReminderDate { get; set; }
        
        // Computed Properties
        public bool IsEligibleForCredit { get; set; }
        public string RiskLevel { get; set; } = string.Empty;
        public bool RequiresAttention { get; set; }
        public string FormattedCreditLimit { get; set; } = string.Empty;
        public string FormattedCurrentDebt { get; set; } = string.Empty;
        public string FormattedAvailableCredit { get; set; } = string.Empty;
    }

    // ==================== DEBT MANAGEMENT DTOs ==================== //

    /// <summary>
    /// DTO for member debt information (collections view)
    /// </summary>
    public class MemberDebtDto
    {
        public int MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string MemberNumber { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public MembershipTier Tier { get; set; }
        
        // Debt Information
        public decimal TotalDebt { get; set; }
        public decimal OverdueAmount { get; set; }
        public int DaysOverdue { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        public DateTime? NextDueDate { get; set; }
        public CreditStatus Status { get; set; }
        public string StatusDescription { get; set; } = string.Empty;
        
        // Collection Information
        public int RemindersSent { get; set; }
        public DateTime? LastReminderDate { get; set; }
        public DateTime? NextReminderDue { get; set; }
        public string RecommendedAction { get; set; } = string.Empty;
        public string CollectionPriority { get; set; } = string.Empty;
        
        // Additional Context
        public decimal CreditLimit { get; set; }
        public decimal AvailableCredit { get; set; }
        public int CreditScore { get; set; }
        public string? BranchName { get; set; }
        
        // Computed Properties
        public bool IsHighRisk { get; set; }
        public bool RequiresUrgentAction { get; set; }
        public string FormattedTotalDebt { get; set; } = string.Empty;
        public string FormattedOverdueAmount { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for payment recording request
    /// </summary>
    public class RecordPaymentDto
    {
        [Required]
        [Range(0.01, 100000000, ErrorMessage = "Payment amount must be between 0.01 and 100,000,000 IDR")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(50, ErrorMessage = "Payment method cannot exceed 50 characters")]
        public string PaymentMethod { get; set; } = string.Empty;

        [StringLength(100)]
        public string? ReferenceNumber { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public int? BranchId { get; set; }
    }

    /// <summary>
    /// DTO for granting credit request
    /// </summary>
    public class GrantCreditDto
    {
        [Required]
        [Range(0.01, 100000000, ErrorMessage = "Credit amount must be between 0.01 and 100,000,000 IDR")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; } = string.Empty;

        [Required]
        public int SaleId { get; set; }

        public int? BranchId { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }
    }

    // ==================== CREDIT ELIGIBILITY DTOs ==================== //

    /// <summary>
    /// DTO for credit eligibility assessment result
    /// </summary>
    public class CreditEligibilityDto
    {
        public int MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public bool IsEligible { get; set; }
        public decimal RequestedAmount { get; set; }
        public decimal ApprovedAmount { get; set; }
        public string DecisionReason { get; set; } = string.Empty;
        public List<string> RequirementsNotMet { get; set; } = new();
        
        // Current Status
        public decimal CurrentUtilization { get; set; }
        public int CreditScore { get; set; }
        public CreditStatus Status { get; set; }
        public decimal AvailableCredit { get; set; }
        
        // Risk Factors
        public List<string> RiskFactors { get; set; } = new();
        public string RiskLevel { get; set; } = string.Empty;
        public bool RequiresManagerApproval { get; set; }
        
        // Recommendations
        public List<string> Recommendations { get; set; } = new();
        public decimal SuggestedCreditLimit { get; set; }
        public string DecisionDetails { get; set; } = string.Empty;
    }

    // ==================== PAYMENT REMINDER DTOs ==================== //

    /// <summary>
    /// DTO for payment reminder information
    /// </summary>
    public class PaymentReminderDto
    {
        public int MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string MemberNumber { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        
        // Debt Information
        public decimal AmountDue { get; set; }
        public int DaysOverdue { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        
        // Reminder History
        public int ReminderCount { get; set; }
        public DateTime? LastReminderDate { get; set; }
        public DateTime? NextReminderDue { get; set; }
        public ReminderPriority Priority { get; set; }
        public string RecommendedAction { get; set; } = string.Empty;
        
        // Contact Information
        public List<string> ContactMethods { get; set; } = new();
        public string PreferredContactMethod { get; set; } = string.Empty;
        
        // Additional Context
        public CreditStatus CreditStatus { get; set; }
        public int CreditScore { get; set; }
        public bool IsHighRisk { get; set; }
        public string? BranchName { get; set; }
        
        // Computed Properties
        public string FormattedAmountDue { get; set; } = string.Empty;
        public string ReminderMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for sending payment reminder request
    /// </summary>
    public class SendReminderDto
    {
        [Required]
        public ReminderType ReminderType { get; set; }

        [StringLength(1000)]
        public string? CustomMessage { get; set; }

        public ReminderPriority Priority { get; set; } = ReminderPriority.Normal;

        public int? BranchId { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }

    // ==================== CREDIT ANALYTICS DTOs ==================== //

    /// <summary>
    /// DTO for credit analytics dashboard
    /// </summary>
    public class CreditAnalyticsDto
    {
        public DateTime AnalysisDate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int? BranchId { get; set; }
        public string? BranchName { get; set; }
        
        // Overall Credit Metrics
        public int TotalMembersWithCredit { get; set; }
        public decimal TotalCreditLimit { get; set; }
        public decimal TotalOutstandingDebt { get; set; }
        public decimal TotalAvailableCredit { get; set; }
        public decimal AverageCreditUtilization { get; set; }
        
        // Risk Metrics
        public int OverdueMembers { get; set; }
        public decimal OverdueAmount { get; set; }
        public decimal BadDebtProvision { get; set; }
        public decimal OverduePercentage { get; set; }
        
        // Payment Performance
        public int TotalPayments { get; set; }
        public decimal TotalPaymentAmount { get; set; }
        public decimal AveragePaymentAmount { get; set; }
        public decimal OnTimePaymentRate { get; set; }
        
        // Credit Score Distribution
        public CreditScoreDistributionDto CreditScoreDistribution { get; set; } = new();
        
        // Status Breakdown
        public List<CreditStatusBreakdownDto> StatusBreakdown { get; set; } = new();
        
        // Tier Analysis
        public List<TierCreditAnalysisDto> TierAnalysis { get; set; } = new();
        
        // Trends
        public List<CreditTrendDto> CreditTrends { get; set; } = new();
    }

    /// <summary>
    /// DTO for credit score distribution
    /// </summary>
    public class CreditScoreDistributionDto
    {
        public int Excellent { get; set; } // 800-850
        public int VeryGood { get; set; }  // 740-799
        public int Good { get; set; }      // 670-739
        public int Fair { get; set; }      // 580-669
        public int Poor { get; set; }      // 300-579
    }

    /// <summary>
    /// DTO for credit status breakdown
    /// </summary>
    public class CreditStatusBreakdownDto
    {
        public CreditStatus Status { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public int MemberCount { get; set; }
        public decimal TotalDebt { get; set; }
        public decimal Percentage { get; set; }
    }

    /// <summary>
    /// DTO for tier-based credit analysis
    /// </summary>
    public class TierCreditAnalysisDto
    {
        public MembershipTier Tier { get; set; }
        public string TierName { get; set; } = string.Empty;
        public int MemberCount { get; set; }
        public decimal AverageCreditLimit { get; set; }
        public decimal AverageDebt { get; set; }
        public decimal AverageUtilization { get; set; }
        public int AverageCreditScore { get; set; }
        public decimal OverdueRate { get; set; }
    }

    /// <summary>
    /// DTO for credit trends over time
    /// </summary>
    public class CreditTrendDto
    {
        public DateTime Date { get; set; }
        public decimal TotalDebt { get; set; }
        public decimal NewCredit { get; set; }
        public decimal Payments { get; set; }
        public int NewOverdue { get; set; }
        public decimal CreditUtilization { get; set; }
    }

    // ==================== REQUEST/RESPONSE DTOs ==================== //

    /// <summary>
    /// DTO for credit limit update request
    /// </summary>
    public class UpdateCreditLimitDto
    {
        [Required]
        [Range(0, 100000000, ErrorMessage = "Credit limit must be between 0 and 100,000,000 IDR")]
        public decimal NewCreditLimit { get; set; }

        [Required]
        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
        public string Reason { get; set; } = string.Empty;

        public bool RequiresApproval { get; set; } = true;

        [StringLength(1000)]
        public string? Notes { get; set; }
    }

    /// <summary>
    /// DTO for bulk operations response
    /// </summary>
    public class BulkOperationResultDto
    {
        public int TotalRequested { get; set; }
        public int Successful { get; set; }
        public int Failed { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public string Summary { get; set; } = string.Empty;
    }
}