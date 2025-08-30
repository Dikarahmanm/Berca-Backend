using System.ComponentModel.DataAnnotations;
using Berca_Backend.Models;

namespace Berca_Backend.DTOs
{
    /// <summary>
    /// Member dengan credit info untuk membership module
    /// </summary>
    public class MemberWithCreditDto : MemberDto
    {
        public decimal CreditLimit { get; set; }
        public decimal CurrentDebt { get; set; }
        public decimal AvailableCredit { get; set; }
        public string CreditStatus { get; set; } = string.Empty; // Good, Warning, Bad
        public int CreditScore { get; set; }
        public DateTime? NextPaymentDueDate { get; set; }
        public bool IsEligibleForCredit { get; set; }
        public decimal CreditUtilization { get; set; } // Percentage of credit used
        public int PaymentDelays { get; set; }
        public decimal LifetimeDebt { get; set; }
        public int PaymentTerms { get; set; } // Days
        public DateTime? LastCreditUsed { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        
        // Display properties
        public string CreditLimitDisplay { get; set; } = string.Empty;
        public string CurrentDebtDisplay { get; set; } = string.Empty;
        public string AvailableCreditDisplay { get; set; } = string.Empty;
        public string CreditUtilizationDisplay { get; set; } = string.Empty;
    }

    /// <summary>
    /// Search request dengan credit filters
    /// </summary>
    public class MemberSearchWithCreditDto : MemberSearchDto
    {
        public string? CreditStatus { get; set; }
        public bool? HasOutstandingDebt { get; set; }
        public decimal? MinCreditLimit { get; set; }
        public decimal? MaxCreditLimit { get; set; }
        public decimal? MinCreditUtilization { get; set; }
        public decimal? MaxCreditUtilization { get; set; }
        public bool? IsOverdue { get; set; }
        public bool? IsEligibleForCredit { get; set; }
        public int? MinCreditScore { get; set; }
        public int? MaxCreditScore { get; set; }
        public DateTime? LastPaymentFrom { get; set; }
        public DateTime? LastPaymentTo { get; set; }
    }

    /// <summary>
    /// Quick credit status untuk UI components
    /// </summary>
    public class MemberCreditStatusDto
    {
        public int MemberId { get; set; }
        public decimal CreditLimit { get; set; }
        public decimal CurrentDebt { get; set; }
        public decimal AvailableCredit { get; set; }
        public string CreditStatus { get; set; } = string.Empty;
        public int CreditScore { get; set; }
        public bool IsEligibleForCredit { get; set; }
        public bool HasOverduePayments { get; set; }
        public DateTime? NextPaymentDueDate { get; set; }
        public decimal CreditUtilization { get; set; }
        public string StatusMessage { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty; // Green, Orange, Red
        public bool CanUseCredit { get; set; }
        public decimal MaxAllowedTransaction { get; set; }
    }

    /// <summary>
    /// Credit validation untuk POS
    /// </summary>
    public class CreditValidationRequestDto
    {
        [Required]
        public int MemberId { get; set; }
        
        [Required]
        [Range(0.01, 999999999.99)]
        public decimal RequestedAmount { get; set; }
        
        public List<SaleItemDto> Items { get; set; } = new();
        
        [Required]
        public int BranchId { get; set; }
        
        public string? Description { get; set; }
        public bool OverrideWarnings { get; set; } = false;
        public int? ManagerUserId { get; set; } // For override approvals
    }

    /// <summary>
    /// Result dari credit validation
    /// </summary>
    public class CreditValidationResultDto
    {
        public bool IsApproved { get; set; }
        public decimal ApprovedAmount { get; set; }
        public decimal AvailableCredit { get; set; }
        public string DecisionReason { get; set; } = string.Empty;
        public List<string> Warnings { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public bool RequiresManagerApproval { get; set; }
        public decimal MaxAllowedAmount { get; set; }
        public string RiskLevel { get; set; } = string.Empty; // Low, Medium, High, Critical
        
        // Member info for POS display
        public string MemberName { get; set; } = string.Empty;
        public string MemberTier { get; set; } = string.Empty;
        public int CreditScore { get; set; }
        public decimal CreditUtilization { get; set; }
        
        // Validation details
        public DateTime ValidationTimestamp { get; set; }
        public int ValidatedByUserId { get; set; }
        public string ValidationId { get; set; } = string.Empty; // For tracking
    }

    /// <summary>
    /// POS sale dengan credit
    /// </summary>
    public class CreateSaleWithCreditDto
    {
        [Required]
        public int MemberId { get; set; }
        
        [Required]
        public List<SaleItemDto> Items { get; set; } = new();
        
        [Required]
        [Range(0.01, 999999999.99)]
        public decimal TotalAmount { get; set; }
        
        [Required]
        [Range(0.01, 999999999.99)]
        public decimal CreditAmount { get; set; }
        
        [Range(0, 999999999.99)]
        public decimal CashAmount { get; set; } = 0; // For mixed payments
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        [Required]
        public int BranchId { get; set; }
        
        [Required]
        public int CashierId { get; set; }
        
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.MemberCredit;
        public string? ValidationId { get; set; } // From previous validation
        public bool IsManagerApproved { get; set; } = false;
        public int? ApprovedByManagerId { get; set; }
        public string? ApprovalNotes { get; set; }
        
        // Additional POS data
        public string? CustomerNotes { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? TaxAmount { get; set; }
        public string? ReceiptNumber { get; set; }
        
        // Flexible due date
        [DataType(DataType.Date)]
        public DateTime? CustomDueDate { get; set; } // Override member payment terms
        
        public bool UseCustomDueDate { get; set; } = false;
    }

    /// <summary>
    /// Quick member lookup untuk POS
    /// </summary>
    public class POSMemberCreditDto
    {
        public int MemberId { get; set; }
        public string MemberNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Tier { get; set; } = string.Empty;
        public int TotalPoints { get; set; }
        
        // Credit information
        public decimal CreditLimit { get; set; }
        public decimal CurrentDebt { get; set; }
        public decimal AvailableCredit { get; set; }
        public string CreditStatus { get; set; } = string.Empty;
        public int CreditScore { get; set; }
        public bool CanUseCredit { get; set; }
        public bool IsEligibleForCredit { get; set; }
        public decimal MaxTransactionAmount { get; set; }
        
        // Status indicators
        public string StatusMessage { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
        public bool HasWarnings { get; set; }
        public List<string> Warnings { get; set; } = new();
        
        // Payment information
        public bool HasOverduePayments { get; set; }
        public DateTime? NextPaymentDueDate { get; set; }
        public int DaysUntilNextPayment { get; set; }
        
        // Display properties for POS UI
        public string CreditLimitDisplay { get; set; } = string.Empty;
        public string AvailableCreditDisplay { get; set; } = string.Empty;
        public string CurrentDebtDisplay { get; set; } = string.Empty;
        
        // Usage stats
        public decimal CreditUtilization { get; set; }
        public DateTime? LastCreditUsed { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        public int TotalCreditTransactions { get; set; }
    }

    /// <summary>
    /// Apply credit payment ke existing sale
    /// </summary>
    public class ApplyCreditPaymentDto
    {
        [Required]
        public int SaleId { get; set; }
        
        [Required]
        public int MemberId { get; set; }
        
        [Required]
        [Range(0.01, 999999999.99)]
        public decimal CreditAmount { get; set; }
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        [Required]
        public int ProcessedBy { get; set; }
        
        public string? ValidationId { get; set; }
        public bool IsPartialPayment { get; set; } = false;
        public string? Notes { get; set; }
        public DateTime? ProcessingDate { get; set; }
        
        // Manager approval for high amounts
        public bool RequiresApproval { get; set; } = false;
        public int? ManagerUserId { get; set; }
        public string? ApprovalNotes { get; set; }
    }

    /// <summary>
    /// Result dari payment processing
    /// </summary>
    public class PaymentResultDto
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? PaymentId { get; set; }
        public int? CreditTransactionId { get; set; }
        public decimal ProcessedAmount { get; set; }
        public decimal RemainingBalance { get; set; }
        public DateTime ProcessedAt { get; set; }
        public string TransactionReference { get; set; } = string.Empty;
        
        // Updated member credit info
        public decimal NewAvailableCredit { get; set; }
        public decimal NewCurrentDebt { get; set; }
        public string NewCreditStatus { get; set; } = string.Empty;
        
        // Warnings or notifications
        public List<string> Warnings { get; set; } = new();
        public List<string> Notifications { get; set; } = new();
        public bool RequiresFollowUp { get; set; }
    }


    /// <summary>
    /// Member search DTO base class
    /// </summary>
    public class MemberSearchDto
    {
        public string? Search { get; set; } // Name, phone, email, member number
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? MemberNumber { get; set; }
        public string? Tier { get; set; }
        public bool? IsActive { get; set; }
        public int? BranchId { get; set; }
        public DateTime? RegisteredFrom { get; set; }
        public DateTime? RegisteredTo { get; set; }
        public decimal? MinTotalSpent { get; set; }
        public decimal? MaxTotalSpent { get; set; }
        public int? MinPoints { get; set; }
        public int? MaxPoints { get; set; }
        
        // Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "RegisteredDate";
        public string SortOrder { get; set; } = "desc";
    }

    /// <summary>
    /// Paginated result wrapper
    /// </summary>
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
        
        public PagedResult()
        {
        }
        
        public PagedResult(List<T> items, int totalCount, int page, int pageSize)
        {
            Items = items;
            TotalCount = totalCount;
            Page = page;
            PageSize = pageSize;
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            HasNextPage = page < TotalPages;
            HasPreviousPage = page > 1;
        }
    }

    /// <summary>
    /// Credit transaction request untuk member credit service
    /// </summary>
    public class GrantCreditRequestDto
    {
        [Required]
        [Range(0.01, 999999999.99)]
        public decimal Amount { get; set; }
        
        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
        
        public int? SaleId { get; set; }
        public int BranchId { get; set; }
        public DateTime? DueDate { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? Notes { get; set; }
        
        // Auto-calculated if not provided
        public int PaymentTerms { get; set; } = 30; // Days
    }

    /// <summary>
    /// Credit payment request untuk member credit service
    /// </summary>
    public class RecordCreditPaymentDto
    {
        [Required]
        [Range(0.01, 999999999.99)]
        public decimal Amount { get; set; }
        
        [Required]
        public PaymentMethod PaymentMethod { get; set; }
        
        public DateTime? PaymentDate { get; set; }
        public string? Notes { get; set; }
        public string? ReferenceNumber { get; set; }
        public int? SaleId { get; set; } // If payment is related to specific sale
        
        // For cash register integration
        public int BranchId { get; set; }
        public int ProcessedBy { get; set; }
    }


    /// <summary>
    /// Credit risk assessment result
    /// </summary>
    public class CreditRiskAssessmentDto
    {
        public int MemberId { get; set; }
        public string RiskLevel { get; set; } = string.Empty; // Low, Medium, High, Critical
        public int RiskScore { get; set; } // 0-100
        public decimal RecommendedCreditLimit { get; set; }
        public decimal MaxSingleTransaction { get; set; }
        public List<string> RiskFactors { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public bool RequiresManagerApproval { get; set; }
        public DateTime AssessmentDate { get; set; }
        public int AssessedBy { get; set; }
    }

    /// <summary>
    /// Sale credit information for receipt printing
    /// </summary>
    public class SaleCreditInfoDto
    {
        public int SaleId { get; set; }
        public string SaleNumber { get; set; } = string.Empty;
        public int MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string MemberNumber { get; set; } = string.Empty;
        public decimal CreditAmount { get; set; }
        public decimal TotalSaleAmount { get; set; }
        public decimal CashAmount { get; set; }
        public int? CreditTransactionId { get; set; }
        public string CreditTransactionReference { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public DateTime? DueDate { get; set; }
        public int PaymentTerms { get; set; }
        
        // Updated member credit status after transaction
        public decimal NewCurrentDebt { get; set; }
        public decimal NewAvailableCredit { get; set; }
        public string NewCreditStatus { get; set; } = string.Empty;
        
        // Display properties
        public string CreditAmountDisplay { get; set; } = string.Empty;
        public string NewCurrentDebtDisplay { get; set; } = string.Empty;
        public string NewAvailableCreditDisplay { get; set; } = string.Empty;
    }

    /// <summary>
    /// Member credit eligibility check result
    /// </summary>
    public class MemberCreditEligibilityDto
    {
        public int MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string MemberNumber { get; set; } = string.Empty;
        public bool IsEligibleForCredit { get; set; }
        public string EligibilityReason { get; set; } = string.Empty;
        public decimal CreditLimit { get; set; }
        public decimal CurrentDebt { get; set; }
        public decimal AvailableCredit { get; set; }
        public string CreditStatus { get; set; } = string.Empty;
        public int CreditScore { get; set; }
        public decimal MaxTransactionAmount { get; set; }
        public bool RequiresManagerApproval { get; set; }
        public decimal CreditUtilization { get; set; }
        public bool HasOverduePayments { get; set; }
        public List<string> Restrictions { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        
        // Display properties
        public string CreditLimitDisplay { get; set; } = string.Empty;
        public string AvailableCreditDisplay { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
    }
}