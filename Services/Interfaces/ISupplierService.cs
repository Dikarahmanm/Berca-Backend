using Berca_Backend.DTOs;
using Berca_Backend.Models;

namespace Berca_Backend.Services.Interfaces
{
    /// <summary>
    /// Interface for Supplier Management Service
    /// Handles all supplier-related operations with branch integration
    /// </summary>
    public interface ISupplierService
    {
        // ==================== CRUD OPERATIONS ==================== //
        
        /// <summary>
        /// Get suppliers with filtering, searching and pagination
        /// </summary>
        /// <param name="queryParams">Query parameters for filtering and pagination</param>
        /// <param name="requestingUserId">ID of user making the request for branch access control</param>
        /// <returns>Paginated supplier response</returns>
        Task<SupplierPagedResponseDto> GetSuppliersAsync(SupplierQueryParams queryParams, int requestingUserId);

        /// <summary>
        /// Get supplier by ID with branch access validation
        /// </summary>
        /// <param name="supplierId">Supplier ID</param>
        /// <param name="requestingUserId">ID of user making the request</param>
        /// <returns>Supplier details or null if not found/unauthorized</returns>
        Task<SupplierDto?> GetSupplierByIdAsync(int supplierId, int requestingUserId);

        /// <summary>
        /// Get supplier by supplier code
        /// </summary>
        /// <param name="supplierCode">Unique supplier code</param>
        /// <param name="requestingUserId">ID of user making the request</param>
        /// <returns>Supplier details or null if not found</returns>
        Task<SupplierDto?> GetSupplierByCodeAsync(string supplierCode, int requestingUserId);

        /// <summary>
        /// Create new supplier with validation and audit trail
        /// </summary>
        /// <param name="createDto">Supplier creation data</param>
        /// <param name="createdByUserId">ID of user creating the supplier</param>
        /// <returns>Created supplier details</returns>
        Task<SupplierDto> CreateSupplierAsync(CreateSupplierDto createDto, int createdByUserId);

        /// <summary>
        /// Update existing supplier with validation and audit trail
        /// </summary>
        /// <param name="supplierId">Supplier ID to update</param>
        /// <param name="updateDto">Supplier update data</param>
        /// <param name="updatedByUserId">ID of user updating the supplier</param>
        /// <returns>Updated supplier details or null if not found</returns>
        Task<SupplierDto?> UpdateSupplierAsync(int supplierId, UpdateSupplierDto updateDto, int updatedByUserId);

        /// <summary>
        /// Soft delete supplier (set IsActive = false)
        /// </summary>
        /// <param name="supplierId">Supplier ID to delete</param>
        /// <param name="deletedByUserId">ID of user deleting the supplier</param>
        /// <param name="reason">Reason for deletion</param>
        /// <returns>True if deleted successfully</returns>
        Task<bool> DeleteSupplierAsync(int supplierId, int deletedByUserId, string? reason = null);

        /// <summary>
        /// Toggle supplier status (active/inactive)
        /// </summary>
        /// <param name="supplierId">Supplier ID</param>
        /// <param name="isActive">New status</param>
        /// <param name="updatedByUserId">ID of user updating status</param>
        /// <param name="reason">Reason for status change</param>
        /// <returns>Updated supplier or null if not found</returns>
        Task<SupplierDto?> ToggleSupplierStatusAsync(int supplierId, bool isActive, int updatedByUserId, string? reason = null);

        // ==================== BRANCH-SPECIFIC OPERATIONS ==================== //

        /// <summary>
        /// Get suppliers available to specific branch
        /// </summary>
        /// <param name="branchId">Branch ID</param>
        /// <param name="includeAll">Include suppliers available to all branches</param>
        /// <param name="activeOnly">Return only active suppliers</param>
        /// <returns>List of suppliers available to the branch</returns>
        Task<List<SupplierSummaryDto>> GetSuppliersByBranchAsync(int branchId, bool includeAll = true, bool activeOnly = true);

        /// <summary>
        /// Get active suppliers filtered by payment terms
        /// </summary>
        /// <param name="minDays">Minimum payment terms (days)</param>
        /// <param name="maxDays">Maximum payment terms (days)</param>
        /// <param name="branchId">Optional branch filter</param>
        /// <returns>List of suppliers matching payment terms criteria</returns>
        Task<List<SupplierSummaryDto>> GetActiveSuppliersByPaymentTermsAsync(int minDays, int maxDays, int? branchId = null);

        /// <summary>
        /// Get suppliers with credit limit above specified amount
        /// </summary>
        /// <param name="minCreditLimit">Minimum credit limit</param>
        /// <param name="branchId">Optional branch filter</param>
        /// <returns>List of suppliers with sufficient credit limit</returns>
        Task<List<SupplierSummaryDto>> GetSuppliersByCreditLimitAsync(decimal minCreditLimit, int? branchId = null);

        // ==================== VALIDATION & BUSINESS LOGIC ==================== //

        /// <summary>
        /// Check if supplier code is unique
        /// </summary>
        /// <param name="supplierCode">Supplier code to check</param>
        /// <param name="excludeSupplierId">Supplier ID to exclude from check (for updates)</param>
        /// <returns>True if code is unique</returns>
        Task<bool> IsSupplierCodeUniqueAsync(string supplierCode, int? excludeSupplierId = null);

        /// <summary>
        /// Check if company name is unique within branch
        /// </summary>
        /// <param name="companyName">Company name to check</param>
        /// <param name="branchId">Branch ID (null for global)</param>
        /// <param name="excludeSupplierId">Supplier ID to exclude from check</param>
        /// <returns>True if name is unique</returns>
        Task<bool> IsCompanyNameUniqueAsync(string companyName, int? branchId, int? excludeSupplierId = null);

        /// <summary>
        /// Check if email is unique across system
        /// </summary>
        /// <param name="email">Email to check</param>
        /// <param name="excludeSupplierId">Supplier ID to exclude from check</param>
        /// <returns>True if email is unique</returns>
        Task<bool> IsEmailUniqueAsync(string email, int? excludeSupplierId = null);

        /// <summary>
        /// Generate unique supplier code in SUP-YYYY-XXXXX format
        /// </summary>
        /// <returns>Unique supplier code</returns>
        Task<string> GenerateSupplierCodeAsync();

        // ==================== REPORTING & ANALYTICS ==================== //

        /// <summary>
        /// Get supplier statistics for dashboard
        /// </summary>
        /// <param name="branchId">Optional branch filter</param>
        /// <returns>Supplier statistics</returns>
        Task<SupplierStatsDto> GetSupplierStatsAsync(int? branchId = null);

        /// <summary>
        /// Get suppliers requiring attention (inactive, long payment terms, etc.)
        /// </summary>
        /// <param name="branchId">Optional branch filter</param>
        /// <returns>List of suppliers requiring attention</returns>
        Task<List<SupplierAlertDto>> GetSuppliersRequiringAttentionAsync(int? branchId = null);

        // ==================== PAYMENT TRACKING INTEGRATION ==================== //

        /// <summary>
        /// Get supplier payment history with facture integration
        /// </summary>
        /// <param name="supplierId">Supplier ID</param>
        /// <param name="fromDate">Optional start date filter</param>
        /// <param name="toDate">Optional end date filter</param>
        /// <returns>Supplier payment history with facture details</returns>
        Task<SupplierPaymentHistoryDto> GetSupplierPaymentHistoryAsync(int supplierId, DateTime? fromDate = null, DateTime? toDate = null);

        /// <summary>
        /// Get outstanding balances for all suppliers
        /// </summary>
        /// <param name="branchId">Optional branch filter</param>
        /// <param name="includeOverdueOnly">Include only overdue amounts</param>
        /// <returns>List of suppliers with outstanding balances</returns>
        Task<List<SupplierOutstandingDto>> GetSupplierOutstandingBalancesAsync(int? branchId = null, bool includeOverdueOnly = false);

        /// <summary>
        /// Update supplier credit status based on facture payment performance
        /// </summary>
        /// <param name="supplierId">Supplier ID</param>
        /// <returns>Updated supplier credit assessment</returns>
        Task<SupplierCreditStatusDto> UpdateSupplierCreditStatusAsync(int supplierId);

        /// <summary>
        /// Get supplier payment analytics with performance metrics
        /// </summary>
        /// <param name="supplierId">Supplier ID</param>
        /// <param name="monthsBack">Number of months to analyze (default 12)</param>
        /// <returns>Supplier payment analytics</returns>
        Task<SupplierPaymentAnalyticsDto> GetSupplierPaymentAnalyticsAsync(int supplierId, int monthsBack = 12);

        /// <summary>
        /// Get suppliers with credit limit warnings based on current outstanding amounts
        /// </summary>
        /// <param name="branchId">Optional branch filter</param>
        /// <param name="warningThreshold">Warning threshold percentage (default 80%)</param>
        /// <returns>List of suppliers approaching credit limits</returns>
        Task<List<SupplierCreditWarningDto>> GetSuppliersWithCreditWarningsAsync(int? branchId = null, decimal warningThreshold = 80);

        /// <summary>
        /// Get supplier payment schedule with upcoming due dates
        /// </summary>
        /// <param name="supplierId">Supplier ID</param>
        /// <param name="daysAhead">Number of days to look ahead (default 30)</param>
        /// <returns>Supplier payment schedule</returns>
        Task<SupplierPaymentScheduleDto> GetSupplierPaymentScheduleAsync(int supplierId, int daysAhead = 30);
    }

    /// <summary>
    /// DTO for supplier statistics
    /// </summary>
    public class SupplierStatsDto
    {
        public int TotalSuppliers { get; set; }
        public int ActiveSuppliers { get; set; }
        public int InactiveSuppliers { get; set; }
        public decimal TotalCreditLimit { get; set; }
        public decimal AveragePaymentTerms { get; set; }
        public int SuppliersWithCreditLimit { get; set; }
        public int ShortTermSuppliers { get; set; }
        public int LongTermSuppliers { get; set; }
    }

    /// <summary>
    /// DTO for supplier alerts and attention items
    /// </summary>
    public class SupplierAlertDto
    {
        public int Id { get; set; }
        public string SupplierCode { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty;
        public string AlertMessage { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty; // Low, Medium, High
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// DTO for supplier payment history with facture integration
    /// </summary>
    public class SupplierPaymentHistoryDto
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string SupplierCode { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalFactures { get; set; }
        public int PaidFactures { get; set; }
        public int PendingFacturesCount { get; set; }
        public int OverdueFactures { get; set; }
        public decimal TotalInvoiced { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal AveragePaymentDays { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        public DateTime? OldestUnpaidDate { get; set; }
        public string TotalInvoicedDisplay { get; set; } = string.Empty;
        public string TotalPaidDisplay { get; set; } = string.Empty;
        public string TotalOutstandingDisplay { get; set; } = string.Empty;
        public List<FacturePaymentSummaryDto> RecentPayments { get; set; } = new List<FacturePaymentSummaryDto>();
        public List<FactureSummaryDto> PendingFactures { get; set; } = new List<FactureSummaryDto>();
    }

    /// <summary>
    /// DTO for facture payment summary in supplier history
    /// </summary>
    public class FacturePaymentSummaryDto
    {
        public int FactureId { get; set; }
        public string InternalReferenceNumber { get; set; } = string.Empty;
        public string SupplierInvoiceNumber { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string AmountDisplay { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string OurPaymentReference { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for supplier outstanding balances
    /// </summary>
    public class SupplierOutstandingDto
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string SupplierCode { get; set; } = string.Empty;
        public int OutstandingFactures { get; set; }
        public int OverdueFactures { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal OverdueAmount { get; set; }
        public decimal CreditLimit { get; set; }
        public decimal CreditUtilization { get; set; }
        public DateTime? OldestUnpaidDate { get; set; }
        public int DaysOldestUnpaid { get; set; }
        public string TotalOutstandingDisplay { get; set; } = string.Empty;
        public string OverdueAmountDisplay { get; set; } = string.Empty;
        public string CreditLimitDisplay { get; set; } = string.Empty;
        public string CreditUtilizationDisplay { get; set; } = string.Empty;
        public bool IsCreditLimitExceeded { get; set; }
        public bool HasOverduePayments { get; set; }
        public string RiskLevel { get; set; } = string.Empty; // Low, Medium, High
    }

    /// <summary>
    /// DTO for supplier credit status assessment
    /// </summary>
    public class SupplierCreditStatusDto
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public decimal CreditLimit { get; set; }
        public decimal CurrentOutstanding { get; set; }
        public decimal AvailableCredit { get; set; }
        public decimal CreditUtilization { get; set; }
        public string CreditRating { get; set; } = string.Empty; // Excellent, Good, Fair, Poor
        public string RiskLevel { get; set; } = string.Empty; // Low, Medium, High
        public decimal AveragePaymentDays { get; set; }
        public int PaymentDelayIncidents { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        public bool HasOverduePayments { get; set; }
        public decimal TotalOverdue { get; set; }
        public int DaysOldestOverdue { get; set; }
        public DateTime AssessmentDate { get; set; }
        public string CreditLimitDisplay { get; set; } = string.Empty;
        public string CurrentOutstandingDisplay { get; set; } = string.Empty;
        public string AvailableCreditsDisplay { get; set; } = string.Empty;
        public string CreditUtilizationDisplay { get; set; } = string.Empty;
        public string TotalOverdueDisplay { get; set; } = string.Empty;
        public List<string> RiskFactors { get; set; } = new List<string>();
        public List<string> Recommendations { get; set; } = new List<string>();
    }

    /// <summary>
    /// DTO for supplier payment analytics and performance metrics
    /// </summary>
    public class SupplierPaymentAnalyticsDto
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public DateTime AnalysisFromDate { get; set; }
        public DateTime AnalysisToDate { get; set; }
        public int TotalFactures { get; set; }
        public int OnTimePayments { get; set; }
        public int LatePayments { get; set; }
        public decimal OnTimePaymentRate { get; set; }
        public decimal AveragePaymentDays { get; set; }
        public decimal MedianPaymentDays { get; set; }
        public decimal TotalVolumeInvoiced { get; set; }
        public decimal TotalVolumePaid { get; set; }
        public decimal LargestInvoice { get; set; }
        public decimal SmallestInvoice { get; set; }
        public decimal AverageInvoiceAmount { get; set; }
        public int PaymentTermDays { get; set; }
        public decimal PaymentComplianceScore { get; set; }
        public string PaymentTrend { get; set; } = string.Empty; // Improving, Stable, Declining
        public string TotalVolumeInvoicedDisplay { get; set; } = string.Empty;
        public string TotalVolumePaidDisplay { get; set; } = string.Empty;
        public string LargestInvoiceDisplay { get; set; } = string.Empty;
        public string AverageInvoiceAmountDisplay { get; set; } = string.Empty;
        public List<MonthlyPaymentPerformanceDto> MonthlyPerformance { get; set; } = new List<MonthlyPaymentPerformanceDto>();
    }

    /// <summary>
    /// DTO for monthly payment performance tracking
    /// </summary>
    public class MonthlyPaymentPerformanceDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public int TotalFactures { get; set; }
        public int OnTimePayments { get; set; }
        public int LatePayments { get; set; }
        public decimal OnTimeRate { get; set; }
        public decimal AveragePaymentDays { get; set; }
        public decimal TotalInvoiced { get; set; }
        public decimal TotalPaid { get; set; }
        public string TotalInvoicedDisplay { get; set; } = string.Empty;
        public string TotalPaidDisplay { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for supplier credit limit warnings
    /// </summary>
    public class SupplierCreditWarningDto
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string SupplierCode { get; set; } = string.Empty;
        public decimal CreditLimit { get; set; }
        public decimal CurrentOutstanding { get; set; }
        public decimal CreditUtilization { get; set; }
        public decimal WarningThreshold { get; set; }
        public decimal ExcessAmount { get; set; }
        public string WarningLevel { get; set; } = string.Empty; // Warning, Critical, Exceeded
        public DateTime LastUpdated { get; set; }
        public string CreditLimitDisplay { get; set; } = string.Empty;
        public string CurrentOutstandingDisplay { get; set; } = string.Empty;
        public string CreditUtilizationDisplay { get; set; } = string.Empty;
        public string ExcessAmountDisplay { get; set; } = string.Empty;
        public bool RequiresImmediateAction { get; set; }
        public List<string> RecommendedActions { get; set; } = new List<string>();
    }

    /// <summary>
    /// DTO for supplier payment schedule with upcoming due dates
    /// </summary>
    public class SupplierPaymentScheduleDto
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public DateTime ScheduleFromDate { get; set; }
        public DateTime ScheduleToDate { get; set; }
        public int TotalUpcomingPayments { get; set; }
        public decimal TotalUpcomingAmount { get; set; }
        public int OverduePaymentsCount { get; set; }
        public decimal OverdueAmount { get; set; }
        public int PaymentsDueToday { get; set; }
        public decimal AmountDueToday { get; set; }
        public int PaymentsDueThisWeek { get; set; }
        public decimal AmountDueThisWeek { get; set; }
        public string TotalUpcomingAmountDisplay { get; set; } = string.Empty;
        public string OverdueAmountDisplay { get; set; } = string.Empty;
        public string AmountDueTodayDisplay { get; set; } = string.Empty;
        public string AmountDueThisWeekDisplay { get; set; } = string.Empty;
        public List<UpcomingPaymentDto> UpcomingPayments { get; set; } = new List<UpcomingPaymentDto>();
        public List<OverduePaymentDto> OverduePayments { get; set; } = new List<OverduePaymentDto>();
    }

    /// <summary>
    /// DTO for upcoming payment details
    /// </summary>
    public class UpcomingPaymentDto
    {
        public int FactureId { get; set; }
        public string InternalReferenceNumber { get; set; } = string.Empty;
        public string SupplierInvoiceNumber { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public decimal Amount { get; set; }
        public int DaysUntilDue { get; set; }
        public string Priority { get; set; } = string.Empty;
        public string AmountDisplay { get; set; } = string.Empty;
        public bool IsDueToday { get; set; }
        public bool IsDueSoon { get; set; }
        public bool HasScheduledPayment { get; set; }
        public DateTime? ScheduledPaymentDate { get; set; }
    }

    /// <summary>
    /// DTO for overdue payment details
    /// </summary>
    public class OverduePaymentDto
    {
        public int FactureId { get; set; }
        public string InternalReferenceNumber { get; set; } = string.Empty;
        public string SupplierInvoiceNumber { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public decimal Amount { get; set; }
        public int DaysOverdue { get; set; }
        public string AmountDisplay { get; set; } = string.Empty;
        public string UrgencyLevel { get; set; } = string.Empty;
        public bool RequiresImmediateAction { get; set; }
    }
}