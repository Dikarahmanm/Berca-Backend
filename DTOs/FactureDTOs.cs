using System.ComponentModel.DataAnnotations;
using Berca_Backend.Models;

namespace Berca_Backend.DTOs
{
    /// <summary>
    /// Main Facture DTO for API responses with complete information
    /// </summary>
    public class FactureDto
    {
        public int Id { get; set; }
        public string SupplierInvoiceNumber { get; set; } = string.Empty;
        public string InternalReferenceNumber { get; set; } = string.Empty;
        
        // Supplier information
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string SupplierCode { get; set; } = string.Empty;
        
        // Branch information
        public int? BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public string BranchDisplay { get; set; } = string.Empty;
        
        // Invoice details
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public string? SupplierPONumber { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string? DeliveryNoteNumber { get; set; }
        
        // Financial information
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal OutstandingAmount { get; set; }
        public decimal Tax { get; set; }
        public decimal Discount { get; set; }
        
        // Status and workflow
        public FactureStatus Status { get; set; }
        public string StatusDisplay { get; set; } = string.Empty;
        public string VerificationStatus { get; set; } = string.Empty;
        public PaymentPriority PaymentPriority { get; set; }
        public string PriorityDisplay { get; set; } = string.Empty;
        
        // Workflow tracking
        public int? ReceivedBy { get; set; }
        public string ReceivedByName { get; set; } = string.Empty;
        public DateTime? ReceivedAt { get; set; }
        
        public int? VerifiedBy { get; set; }
        public string VerifiedByName { get; set; } = string.Empty;
        public DateTime? VerifiedAt { get; set; }
        
        public int? ApprovedBy { get; set; }
        public string ApprovedByName { get; set; } = string.Empty;
        public DateTime? ApprovedAt { get; set; }
        
        // Files
        public string? SupplierInvoiceFile { get; set; }
        public string? ReceiptFile { get; set; }
        public string? SupportingDocs { get; set; }
        
        // Notes
        public string? Notes { get; set; }
        public string? Description { get; set; }
        public string? DisputeReason { get; set; }
        
        // Audit information
        public int CreatedBy { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public int? UpdatedBy { get; set; }
        public string UpdatedByName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Computed properties
        public int DaysOverdue { get; set; }
        public int DaysUntilDue { get; set; }
        public decimal PaymentProgress { get; set; }
        public bool IsOverdue { get; set; }
        public bool RequiresApproval { get; set; }
        
        // Display properties
        public string TotalAmountDisplay { get; set; } = string.Empty;
        public string PaidAmountDisplay { get; set; } = string.Empty;
        public string OutstandingAmountDisplay { get; set; } = string.Empty;
        
        // Action permissions
        public bool CanVerify { get; set; }
        public bool CanApprove { get; set; }
        public bool CanDispute { get; set; }
        public bool CanCancel { get; set; }
        public bool CanSchedulePayment { get; set; }
        public bool CanReceivePayment { get; set; }
        
        // Related data
        public List<FactureItemDto> Items { get; set; } = new List<FactureItemDto>();
        public List<FacturePaymentDto> Payments { get; set; } = new List<FacturePaymentDto>();
    }

    /// <summary>
    /// DTO for receiving new supplier invoices
    /// </summary>
    public class ReceiveFactureDto
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string SupplierInvoiceNumber { get; set; } = string.Empty;
        
        [Required]
        public int SupplierId { get; set; }
        
        public int? BranchId { get; set; }
        
        [Required]
        public DateTime InvoiceDate { get; set; }
        
        public DateTime? DueDate { get; set; } // If not provided, calculated from supplier payment terms
        
        [StringLength(50)]
        public string? SupplierPONumber { get; set; }
        
        public DateTime? DeliveryDate { get; set; }
        
        [StringLength(50)]
        public string? DeliveryNoteNumber { get; set; }
        
        [Required]
        [Range(0.01, 999999999.99)]
        public decimal TotalAmount { get; set; }
        
        [Range(0, 100)]
        public decimal Tax { get; set; } = 0;
        
        [Range(0, 999999999.99)]
        public decimal Discount { get; set; } = 0;
        
        [StringLength(2000)]
        public string? Description { get; set; }
        
        [StringLength(2000)]
        public string? Notes { get; set; }
        
        // File upload support
        public string? SupplierInvoiceFile { get; set; }
        public string? SupportingDocs { get; set; }
        
        // Line items
        public List<CreateFactureItemDto> Items { get; set; } = new List<CreateFactureItemDto>();
    }

    /// <summary>
    /// DTO for verifying factures after receiving
    /// </summary>
    public class VerifyFactureDto
    {
        public int FactureId { get; set; }
        
        [StringLength(1000)]
        public string? VerificationNotes { get; set; }
        
        // Verified items with delivery quantities
        public List<VerifyFactureItemDto> Items { get; set; } = new List<VerifyFactureItemDto>();
    }

    /// <summary>
    /// DTO for updating factures (limited based on status)
    /// </summary>
    public class UpdateFactureDto
    {
        public DateTime? DueDate { get; set; }
        
        [StringLength(50)]
        public string? SupplierPONumber { get; set; }
        
        public DateTime? DeliveryDate { get; set; }
        
        [StringLength(50)]
        public string? DeliveryNoteNumber { get; set; }
        
        [StringLength(2000)]
        public string? Description { get; set; }
        
        [StringLength(2000)]
        public string? Notes { get; set; }
        
        public string? ReceiptFile { get; set; }
        public string? SupportingDocs { get; set; }
    }

    /// <summary>
    /// DTO for facture list display with essential information
    /// </summary>
    public class FactureListDto
    {
        public int Id { get; set; }
        public string SupplierInvoiceNumber { get; set; } = string.Empty;
        public string InternalReferenceNumber { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal OutstandingAmount { get; set; }
        public FactureStatus Status { get; set; }
        public string StatusDisplay { get; set; } = string.Empty;
        public PaymentPriority PaymentPriority { get; set; }
        public string PriorityDisplay { get; set; } = string.Empty;
        public int DaysOverdue { get; set; }
        public int DaysUntilDue { get; set; }
        public bool IsOverdue { get; set; }
        public string TotalAmountDisplay { get; set; } = string.Empty;
        public string OutstandingAmountDisplay { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// DTO for dashboard summary and KPIs
    /// </summary>
    public class FactureSummaryDto
    {
        public int TotalFactures { get; set; }
        public int PendingVerification { get; set; }
        public int PendingApproval { get; set; }
        public int OverduePayments { get; set; }
        public int PaymentsDueToday { get; set; }
        public int PaymentsDueSoon { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal TotalOverdue { get; set; }
        public decimal PaymentsDueThisWeek { get; set; }
        public decimal PaymentsDueThisMonth { get; set; }
        public string TotalOutstandingDisplay { get; set; } = string.Empty;
        public string TotalOverdueDisplay { get; set; } = string.Empty;
        public string PaymentsDueThisWeekDisplay { get; set; } = string.Empty;
        public string PaymentsDueThisMonthDisplay { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for facture items with product mapping
    /// </summary>
    public class FactureItemDto
    {
        public int Id { get; set; }
        public int FactureId { get; set; }
        public int? ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductBarcode { get; set; }
        public string? SupplierItemCode { get; set; }
        public string SupplierItemDescription { get; set; } = string.Empty;
        public string ItemDescription { get; set; } = string.Empty;
        public string ItemCode { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal? ReceivedQuantity { get; set; }
        public decimal? AcceptedQuantity { get; set; }
        public decimal TaxRate { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal LineTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal LineTotalWithTax { get; set; }
        public string? Notes { get; set; }
        public string? VerificationNotes { get; set; }
        public bool IsVerified { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public string? VerifiedByName { get; set; }
        public bool IsProductMapped { get; set; }
        public bool HasQuantityVariance { get; set; }
        public bool HasAcceptanceVariance { get; set; }
        public string VerificationStatus { get; set; } = string.Empty;
        public decimal QuantityVariance { get; set; }
        public decimal AcceptanceVariance { get; set; }
        public string UnitDisplay { get; set; } = string.Empty;
        public string UnitPriceDisplay { get; set; } = string.Empty;
        public string LineTotalDisplay { get; set; } = string.Empty;
        public string LineTotalWithTaxDisplay { get; set; } = string.Empty;
        public bool RequiresApproval { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// DTO for creating facture items
    /// </summary>
    public class CreateFactureItemDto
    {
        public int? ProductId { get; set; }
        
        [StringLength(100)]
        public string? SupplierItemCode { get; set; }
        
        [Required]
        [StringLength(500, MinimumLength = 3)]
        public string SupplierItemDescription { get; set; } = string.Empty;
        
        [Required]
        [Range(0.01, 999999.99)]
        public decimal Quantity { get; set; }
        
        [Required]
        [Range(0.01, 999999999.99)]
        public decimal UnitPrice { get; set; }
        
        [Range(0, 100)]
        public decimal TaxRate { get; set; } = 0;
        
        [Range(0, 999999999.99)]
        public decimal DiscountAmount { get; set; } = 0;
        
        [StringLength(1000)]
        public string? Notes { get; set; }
    }

    /// <summary>
    /// DTO for verifying individual facture items
    /// </summary>
    public class VerifyFactureItemDto
    {
        [Required]
        public int ItemId { get; set; }
        
        [Range(0, 999999.99)]
        public decimal? ReceivedQuantity { get; set; }
        
        [Range(0, 999999.99)]
        public decimal? AcceptedQuantity { get; set; }
        
        [StringLength(500)]
        public string? VerificationNotes { get; set; }
        
        public bool IsVerified { get; set; } = true;
    }

    /// <summary>
    /// DTO for facture payments with processing details
    /// </summary>
    public class FacturePaymentDto
    {
        public int Id { get; set; }
        public int FactureId { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string PaymentMethodDisplay { get; set; } = string.Empty;
        public PaymentStatus Status { get; set; }
        public string StatusDisplay { get; set; } = string.Empty;
        public string? OurPaymentReference { get; set; }
        public string? SupplierAckReference { get; set; }
        public string? BankAccount { get; set; }
        public string? CheckNumber { get; set; }
        public string? TransferReference { get; set; }
        public string PaymentReference { get; set; } = string.Empty;
        public int ProcessedBy { get; set; }
        public string ProcessedByName { get; set; } = string.Empty;
        public int? ApprovedBy { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public string? ConfirmedByName { get; set; }
        public string? Notes { get; set; }
        public string? FailureReason { get; set; }
        public string? DisputeReason { get; set; }
        public string? PaymentReceiptFile { get; set; }
        public string? ConfirmationFile { get; set; }
        public DateTime? ScheduledDate { get; set; }
        public bool RequiresApproval { get; set; }
        public bool IsOverdue { get; set; }
        public int DaysOverdue { get; set; }
        public int DaysUntilPayment { get; set; }
        public bool IsDueToday { get; set; }
        public bool IsDueSoon { get; set; }
        public string ProcessingStatus { get; set; } = string.Empty;
        public bool HasConfirmation { get; set; }
        public string AmountDisplay { get; set; } = string.Empty;
        public bool CanEdit { get; set; }
        public bool CanProcess { get; set; }
        public bool CanConfirm { get; set; }
        public bool CanCancel { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// DTO for query parameters with supplier invoice search
    /// </summary>
    public class FactureQueryDto
    {
        public string? Search { get; set; }
        public string? SupplierInvoiceNumber { get; set; }
        public string? InternalReferenceNumber { get; set; }
        public string? SupplierPONumber { get; set; }
        public int? SupplierId { get; set; }
        public int? BranchId { get; set; }
        public FactureStatus? Status { get; set; }
        public PaymentPriority? Priority { get; set; }
        public DateTime? InvoiceDateFrom { get; set; }
        public DateTime? InvoiceDateTo { get; set; }
        public DateTime? DueDateFrom { get; set; }
        public DateTime? DueDateTo { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public bool? IsOverdue { get; set; }
        public bool? RequiresApproval { get; set; }
        public bool? PendingVerification { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "InvoiceDate";
        public string SortOrder { get; set; } = "desc";
    }

    /// <summary>
    /// DTO for supplier-specific facture summary
    /// </summary>
    public class SupplierFactureSummaryDto
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public int TotalFactures { get; set; }
        public int OverdueFactures { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal OverdueAmount { get; set; }
        public decimal CreditLimit { get; set; }
        public decimal CreditUtilization { get; set; }
        public decimal AveragePaymentDays { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        public DateTime? OldestUnpaidInvoice { get; set; }
        public string TotalOutstandingDisplay { get; set; } = string.Empty;
        public string OverdueAmountDisplay { get; set; } = string.Empty;
        public string CreditLimitDisplay { get; set; } = string.Empty;
        public string CreditUtilizationDisplay { get; set; } = string.Empty;
        public bool IsCreditLimitExceeded { get; set; }
        public bool HasOverduePayments { get; set; }
    }

    /// <summary>
    /// DTO for paginated facture responses
    /// </summary>
    public class FacturePagedResponseDto
    {
        public List<FactureListDto> Factures { get; set; } = new List<FactureListDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    /// <summary>
    /// DTO for payment scheduling
    /// </summary>
    public class SchedulePaymentDto
    {
        [Required]
        public int FactureId { get; set; }
        
        [Required]
        public DateTime PaymentDate { get; set; }
        
        [Required]
        [Range(0.01, 999999999.99)]
        public decimal Amount { get; set; }
        
        [Required]
        public PaymentMethod PaymentMethod { get; set; }
        
        [StringLength(100)]
        public string? BankAccount { get; set; }
        
        [StringLength(100)]
        public string? OurPaymentReference { get; set; }
        
        [StringLength(1000)]
        public string? Notes { get; set; }
        
        public bool IsRecurring { get; set; } = false;
        
        [StringLength(50)]
        public string? RecurrencePattern { get; set; }
    }

    /// <summary>
    /// DTO for processing payments
    /// </summary>
    public class ProcessPaymentDto
    {
        [Required]
        public int PaymentId { get; set; }
        
        [StringLength(100)]
        public string? TransferReference { get; set; }
        
        [StringLength(100)]
        public string? CheckNumber { get; set; }
        
        [StringLength(1000)]
        public string? ProcessingNotes { get; set; }
        
        public string? PaymentReceiptFile { get; set; }
    }

    /// <summary>
    /// DTO for confirming payments from supplier
    /// </summary>
    public class ConfirmPaymentDto
    {
        [Required]
        public int PaymentId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string SupplierAckReference { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string? ConfirmationNotes { get; set; }
        
        public string? ConfirmationFile { get; set; }
    }

    /// <summary>
    /// DTO for disputing factures
    /// </summary>
    public class DisputeFactureDto
    {
        [Required]
        public int FactureId { get; set; }
        
        [Required]
        [StringLength(1000, MinimumLength = 10)]
        public string DisputeReason { get; set; } = string.Empty;
        
        [StringLength(2000)]
        public string? AdditionalNotes { get; set; }
        
        public string? SupportingDocuments { get; set; }
    }

    /// <summary>
    /// Query parameters for facture filtering and pagination
    /// </summary>
    public class FactureQueryParams
    {
        public string? Search { get; set; }
        public string? SupplierInvoiceNumber { get; set; }
        public string? InternalReferenceNumber { get; set; }
        public string? SupplierPONumber { get; set; }
        public int? SupplierId { get; set; }
        public int? BranchId { get; set; }
        public FactureStatus? Status { get; set; }
        public PaymentPriority? Priority { get; set; }
        public DateTime? InvoiceDateFrom { get; set; }
        public DateTime? InvoiceDateTo { get; set; }
        public DateTime? DueDateFrom { get; set; }
        public DateTime? DueDateTo { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public bool? IsOverdue { get; set; }
        public bool? RequiresApproval { get; set; }
        public bool? PendingVerification { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "InvoiceDate";
        public string SortOrder { get; set; } = "desc";
    }

    /// <summary>
    /// DTO for outstanding factures by supplier analytics
    /// </summary>
    public class OutstandingBySupplierDto
    {
        public int SupplierId { get; set; }
        public string SupplierCode { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int TotalOutstandingFactures { get; set; }
        public decimal TotalOutstandingAmount { get; set; }
        public decimal OldestOutstandingDays { get; set; }
        public decimal AveragePaymentDelayDays { get; set; }
        public DateTime? OldestFactureDueDate { get; set; }
        public int OverdueCount { get; set; }
        public decimal OverdueAmount { get; set; }
        public string PaymentRisk { get; set; } = "Low"; // Low, Medium, High, Critical
        public List<OutstandingFactureBriefDto> TopOutstandingFactures { get; set; } = new();
    }

    /// <summary>
    /// Brief info for outstanding facture in supplier summary
    /// </summary>
    public class OutstandingFactureBriefDto
    {
        public int Id { get; set; }
        public string SupplierInvoiceNumber { get; set; } = string.Empty;
        public string InternalReferenceNumber { get; set; } = string.Empty;
        public decimal OutstandingAmount { get; set; }
        public int DaysOverdue { get; set; }
        public DateTime DueDate { get; set; }
        public PaymentPriority Priority { get; set; }
    }

    /// <summary>
    /// DTO for suppliers by branch analytics
    /// </summary>
    public class SuppliersByBranchDto
    {
        public int BranchId { get; set; }
        public string BranchCode { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public int TotalSuppliers { get; set; }
        public int ActiveSuppliers { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal AverageFactureAmount { get; set; }
        public int TotalFacturesThisMonth { get; set; }
        public decimal PaymentComplianceRate { get; set; } // Percentage
        public List<TopSupplierByBranchDto> TopSuppliers { get; set; } = new();
        public List<CategorySpendingDto> SpendingByCategory { get; set; } = new();
    }

    /// <summary>
    /// Top supplier info for branch summary
    /// </summary>
    public class TopSupplierByBranchDto
    {
        public int SupplierId { get; set; }
        public string SupplierCode { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public decimal MonthlySpending { get; set; }
        public int FacturesCount { get; set; }
        public decimal OutstandingAmount { get; set; }
    }

    /// <summary>
    /// Category spending info for branch
    /// </summary>
    public class CategorySpendingDto
    {
        public string Category { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int FacturesCount { get; set; }
        public decimal Percentage { get; set; }
    }

    /// <summary>
    /// DTO for supplier alerts
    /// </summary>
    public class SupplierAlertsDto
    {
        public List<FactureSupplierAlertDto> CriticalAlerts { get; set; } = new();
        public List<FactureSupplierAlertDto> WarningAlerts { get; set; } = new();
        public List<FactureSupplierAlertDto> InfoAlerts { get; set; } = new();
        public SupplierAlertSummaryDto Summary { get; set; } = new();
    }

    /// <summary>
    /// Individual supplier alert from facture context
    /// </summary>
    public class FactureSupplierAlertDto
    {
        public int Id { get; set; }
        public string AlertType { get; set; } = string.Empty; // PaymentOverdue, HighOutstanding, CreditLimitExceeded, etc.
        public string Priority { get; set; } = string.Empty; // Critical, Warning, Info
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public int? FactureId { get; set; }
        public string? FactureReference { get; set; }
        public decimal? Amount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DueDate { get; set; }
        public int DaysOverdue { get; set; }
        public string ActionRequired { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public int? BranchId { get; set; }
        public string? BranchName { get; set; }
    }

    /// <summary>
    /// Summary of supplier alerts
    /// </summary>
    public class SupplierAlertSummaryDto
    {
        public int TotalCriticalAlerts { get; set; }
        public int TotalWarningAlerts { get; set; }
        public int TotalInfoAlerts { get; set; }
        public int UnreadAlerts { get; set; }
        public decimal TotalAmountAtRisk { get; set; }
        public int SuppliersWithAlerts { get; set; }
        public DateTime LastUpdated { get; set; }
        public List<AlertCategoryDto> AlertsByCategory { get; set; } = new();
    }

    /// <summary>
    /// Alert breakdown by category
    /// </summary>
    public class AlertCategoryDto
    {
        public string Category { get; set; } = string.Empty;
        public int Count { get; set; }
        public string Priority { get; set; } = string.Empty;
    }
}