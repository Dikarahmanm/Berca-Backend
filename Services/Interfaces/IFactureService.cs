using Berca_Backend.DTOs;
using Berca_Backend.Models;

namespace Berca_Backend.Services.Interfaces
{
    /// <summary>
    /// Interface for Facture Management Service
    /// Handles supplier invoice receiving, verification, and payment workflows
    /// </summary>
    public interface IFactureService
    {
        // ==================== FACTURE RECEIVING & WORKFLOW ==================== //
        
        /// <summary>
        /// Receive new supplier invoice and create facture record
        /// </summary>
        /// <param name="receiveDto">Invoice receiving data with items</param>
        /// <param name="receivedByUserId">ID of user receiving the invoice</param>
        /// <returns>Created facture details</returns>
        Task<FactureDto> ReceiveSupplierInvoiceAsync(ReceiveFactureDto receiveDto, int receivedByUserId);

        /// <summary>
        /// Verify facture items against delivery
        /// </summary>
        /// <param name="verifyDto">Verification data with item quantities</param>
        /// <param name="verifiedByUserId">ID of user performing verification</param>
        /// <returns>Updated facture with verification status</returns>
        Task<FactureDto?> VerifyFactureItemsAsync(VerifyFactureDto verifyDto, int verifiedByUserId);

        /// <summary>
        /// Approve facture for payment processing
        /// </summary>
        /// <param name="factureId">Facture ID to approve</param>
        /// <param name="approvedByUserId">ID of user approving</param>
        /// <param name="approvalNotes">Optional approval notes</param>
        /// <returns>Approved facture details</returns>
        Task<FactureDto?> ApproveFactureAsync(int factureId, int approvedByUserId, string? approvalNotes = null);

        /// <summary>
        /// Dispute facture with supplier
        /// </summary>
        /// <param name="disputeDto">Dispute details and reason</param>
        /// <param name="disputedByUserId">ID of user raising dispute</param>
        /// <returns>Updated facture with dispute status</returns>
        Task<FactureDto?> DisputeFactureAsync(DisputeFactureDto disputeDto, int disputedByUserId);

        /// <summary>
        /// Cancel/reject facture
        /// </summary>
        /// <param name="factureId">Facture ID to cancel</param>
        /// <param name="cancelledByUserId">ID of user cancelling</param>
        /// <param name="cancellationReason">Reason for cancellation</param>
        /// <returns>True if cancelled successfully</returns>
        Task<bool> CancelFactureAsync(int factureId, int cancelledByUserId, string? cancellationReason = null);

        // ==================== FACTURE CRUD OPERATIONS ==================== //

        /// <summary>
        /// Get factures with filtering, searching and pagination
        /// </summary>
        /// <param name="queryParams">Query parameters for filtering and pagination</param>
        /// <param name="requestingUserId">ID of user making the request for branch access control</param>
        /// <returns>Paginated facture response</returns>
        Task<FacturePagedResponseDto> GetFacturesAsync(FactureQueryParams queryParams, int requestingUserId);

        /// <summary>
        /// Get facture by ID with access validation
        /// </summary>
        /// <param name="factureId">Facture ID</param>
        /// <param name="requestingUserId">ID of user making the request</param>
        /// <returns>Facture details or null if not found/unauthorized</returns>
        Task<FactureDto?> GetFactureByIdAsync(int factureId, int requestingUserId);

        /// <summary>
        /// Get facture by supplier invoice number
        /// </summary>
        /// <param name="supplierInvoiceNumber">Supplier's invoice number</param>
        /// <param name="supplierId">Supplier ID for uniqueness check</param>
        /// <param name="requestingUserId">ID of user making the request</param>
        /// <returns>Facture details or null if not found</returns>
        Task<FactureDto?> GetFactureBySupplierInvoiceNumberAsync(string supplierInvoiceNumber, int supplierId, int requestingUserId);

        /// <summary>
        /// Update facture details (limited based on status)
        /// </summary>
        /// <param name="factureId">Facture ID to update</param>
        /// <param name="updateDto">Update data</param>
        /// <param name="updatedByUserId">ID of user updating</param>
        /// <returns>Updated facture details or null if not found</returns>
        Task<FactureDto?> UpdateFactureAsync(int factureId, UpdateFactureDto updateDto, int updatedByUserId);

        // ==================== SUPPLIER-SPECIFIC OPERATIONS ==================== //

        /// <summary>
        /// Get factures for specific supplier
        /// </summary>
        /// <param name="supplierId">Supplier ID</param>
        /// <param name="requestingUserId">ID of user making the request</param>
        /// <param name="includeCompleted">Include completed/paid factures</param>
        /// <param name="pageSize">Number of records per page</param>
        /// <returns>List of supplier factures</returns>
        Task<List<FactureListDto>> GetSupplierFacturesAsync(int supplierId, int requestingUserId, bool includeCompleted = false, int pageSize = 50);

        /// <summary>
        /// Get supplier facture summary with outstanding balances
        /// </summary>
        /// <param name="supplierId">Supplier ID</param>
        /// <param name="requestingUserId">ID of user making the request</param>
        /// <returns>Supplier facture summary with analytics</returns>
        Task<SupplierFactureSummaryDto?> GetSupplierFactureSummaryAsync(int supplierId, int requestingUserId);

        // ==================== PAYMENT MANAGEMENT ==================== //

        /// <summary>
        /// Schedule payment for facture
        /// </summary>
        /// <param name="scheduleDto">Payment scheduling details</param>
        /// <param name="scheduledByUserId">ID of user scheduling payment</param>
        /// <returns>Scheduled payment details</returns>
        Task<FacturePaymentDto> SchedulePaymentAsync(SchedulePaymentDto scheduleDto, int scheduledByUserId);

        /// <summary>
        /// Process scheduled payment
        /// </summary>
        /// <param name="processDto">Payment processing details</param>
        /// <param name="processedByUserId">ID of user processing payment</param>
        /// <returns>Processed payment details</returns>
        Task<FacturePaymentDto?> ProcessPaymentAsync(ProcessPaymentDto processDto, int processedByUserId);

        /// <summary>
        /// Confirm payment received by supplier
        /// </summary>
        /// <param name="confirmDto">Confirmation details from supplier</param>
        /// <param name="confirmedByUserId">ID of user confirming payment</param>
        /// <returns>Confirmed payment details</returns>
        Task<FacturePaymentDto?> ConfirmPaymentAsync(ConfirmPaymentDto confirmDto, int confirmedByUserId);

        /// <summary>
        /// Get payment history for facture
        /// </summary>
        /// <param name="factureId">Facture ID</param>
        /// <param name="requestingUserId">ID of user making the request</param>
        /// <returns>List of payments for the facture</returns>
        Task<List<FacturePaymentDto>> GetFacturePaymentsAsync(int factureId, int requestingUserId);

        /// <summary>
        /// Update payment details
        /// </summary>
        /// <param name="paymentId">Payment ID to update</param>
        /// <param name="updatedByUserId">ID of user updating</param>
        /// <param name="notes">Updated notes</param>
        /// <param name="bankAccount">Updated bank account</param>
        /// <returns>Updated payment details or null if not found</returns>
        Task<FacturePaymentDto?> UpdatePaymentAsync(int paymentId, int updatedByUserId, string? notes = null, string? bankAccount = null);

        /// <summary>
        /// Cancel payment
        /// </summary>
        /// <param name="paymentId">Payment ID to cancel</param>
        /// <param name="cancelledByUserId">ID of user cancelling</param>
        /// <param name="cancellationReason">Reason for cancellation</param>
        /// <returns>True if cancelled successfully</returns>
        Task<bool> CancelPaymentAsync(int paymentId, int cancelledByUserId, string? cancellationReason = null);

        // ==================== WORKFLOW & STATUS TRACKING ==================== //

        /// <summary>
        /// Get factures pending verification
        /// </summary>
        /// <param name="requestingUserId">ID of user making the request</param>
        /// <param name="branchId">Optional branch filter</param>
        /// <returns>List of factures pending verification</returns>
        Task<List<FactureListDto>> GetFacturesPendingVerificationAsync(int requestingUserId, int? branchId = null);

        /// <summary>
        /// Get factures pending approval
        /// </summary>
        /// <param name="requestingUserId">ID of user making the request</param>
        /// <param name="branchId">Optional branch filter</param>
        /// <returns>List of factures pending approval</returns>
        Task<List<FactureListDto>> GetFacturesPendingApprovalAsync(int requestingUserId, int? branchId = null);

        /// <summary>
        /// Get overdue payments requiring attention
        /// </summary>
        /// <param name="requestingUserId">ID of user making the request</param>
        /// <param name="branchId">Optional branch filter</param>
        /// <returns>List of overdue payments</returns>
        Task<List<FactureListDto>> GetOverduePaymentsAsync(int requestingUserId, int? branchId = null);

        /// <summary>
        /// Get payments due soon
        /// </summary>
        /// <param name="requestingUserId">ID of user making the request</param>
        /// <param name="daysAhead">Number of days to look ahead (default 7)</param>
        /// <param name="branchId">Optional branch filter</param>
        /// <returns>List of payments due soon</returns>
        Task<List<FactureListDto>> GetPaymentsDueSoonAsync(int requestingUserId, int daysAhead = 7, int? branchId = null);

        /// <summary>
        /// Get payments due today
        /// </summary>
        /// <param name="requestingUserId">ID of user making the request</param>
        /// <param name="branchId">Optional branch filter</param>
        /// <returns>List of payments due today</returns>
        Task<List<FactureListDto>> GetPaymentsDueTodayAsync(int requestingUserId, int? branchId = null);

        // ==================== VALIDATION & BUSINESS LOGIC ==================== //

        /// <summary>
        /// Validate supplier invoice number uniqueness per supplier
        /// </summary>
        /// <param name="supplierInvoiceNumber">Invoice number to check</param>
        /// <param name="supplierId">Supplier ID</param>
        /// <param name="excludeFactureId">Facture ID to exclude from check</param>
        /// <returns>True if unique for the supplier</returns>
        Task<bool> ValidateSupplierInvoiceNumberAsync(string supplierInvoiceNumber, int supplierId, int? excludeFactureId = null);

        /// <summary>
        /// Generate internal reference number
        /// </summary>
        /// <returns>Unique internal reference number in INT-FAC-YYYY-XXXXX format</returns>
        Task<string> GenerateInternalReferenceNumberAsync();

        /// <summary>
        /// Calculate due date from invoice date and supplier payment terms
        /// </summary>
        /// <param name="invoiceDate">Invoice date</param>
        /// <param name="supplierId">Supplier ID for payment terms</param>
        /// <returns>Calculated due date</returns>
        Task<DateTime> CalculateDueDateAsync(DateTime invoiceDate, int supplierId);

        /// <summary>
        /// Calculate facture totals including tax and discounts
        /// </summary>
        /// <param name="items">List of facture items</param>
        /// <param name="tax">Additional tax amount</param>
        /// <param name="discount">Total discount amount</param>
        /// <returns>Calculated total amount</returns>
        decimal CalculateFactureTotals(List<CreateFactureItemDto> items, decimal tax = 0, decimal discount = 0);

        /// <summary>
        /// Validate supplier credit limit for new facture
        /// </summary>
        /// <param name="supplierId">Supplier ID</param>
        /// <param name="newFactureAmount">Amount of new facture</param>
        /// <param name="requestingUserId">ID of user making the request</param>
        /// <returns>True if within credit limit</returns>
        Task<bool> ValidateSupplierCreditLimitAsync(int supplierId, decimal newFactureAmount, int requestingUserId);

        // ==================== ANALYTICS & REPORTING ==================== //

        /// <summary>
        /// Get facture summary statistics for dashboard
        /// </summary>
        /// <param name="requestingUserId">ID of user making the request</param>
        /// <param name="branchId">Optional branch filter</param>
        /// <returns>Summary statistics</returns>
        Task<FactureSummaryDto> GetFactureSummaryAsync(int requestingUserId, int? branchId = null);

        /// <summary>
        /// Get payment analytics for cash flow planning
        /// </summary>
        /// <param name="requestingUserId">ID of user making the request</param>
        /// <param name="daysAhead">Number of days to project ahead</param>
        /// <param name="branchId">Optional branch filter</param>
        /// <returns>Payment analytics data</returns>
        Task<object> GetPaymentAnalyticsAsync(int requestingUserId, int daysAhead = 30, int? branchId = null);

        /// <summary>
        /// Get aging analysis for outstanding payments
        /// </summary>
        /// <param name="requestingUserId">ID of user making the request</param>
        /// <param name="branchId">Optional branch filter</param>
        /// <returns>Aging analysis data</returns>
        Task<object> GetAgingAnalysisAsync(int requestingUserId, int? branchId = null);

        /// <summary>
        /// Get outstanding factures analytics
        /// </summary>
        /// <param name="requestingUserId">ID of user making the request</param>
        /// <param name="branchId">Optional branch filter</param>
        /// <param name="supplierId">Optional supplier filter</param>
        /// <param name="limit">Maximum records to return</param>
        /// <returns>Outstanding factures data</returns>
        Task<List<FactureListDto>> GetOutstandingFacturesAsync(int requestingUserId, int? branchId = null, int? supplierId = null, int limit = 50);

        /// <summary>
        /// Get top suppliers by factures analytics
        /// </summary>
        /// <param name="requestingUserId">ID of user making the request</param>
        /// <param name="branchId">Optional branch filter</param>
        /// <param name="fromDate">Start date for analysis</param>
        /// <param name="toDate">End date for analysis</param>
        /// <param name="limit">Number of top suppliers to return</param>
        /// <returns>Top suppliers data</returns>
        Task<List<OutstandingBySupplierDto>> GetTopSuppliersByFacturesAsync(int requestingUserId, int? branchId = null, DateTime? fromDate = null, DateTime? toDate = null, int limit = 10);

        /// <summary>
        /// Get suppliers by branch analytics
        /// </summary>
        /// <param name="requestingUserId">ID of user making the request</param>
        /// <param name="branchId">Optional specific branch filter</param>
        /// <returns>Suppliers breakdown by branch</returns>
        Task<List<SuppliersByBranchDto>> GetSuppliersByBranchAsync(int requestingUserId, int? branchId = null);

        /// <summary>
        /// Get supplier alerts system
        /// </summary>
        /// <param name="requestingUserId">ID of user making the request</param>
        /// <param name="branchId">Optional branch filter</param>
        /// <param name="priorityFilter">Optional priority filter (Critical, Warning, Info)</param>
        /// <returns>Supplier alerts data</returns>
        Task<SupplierAlertsDto> GetSupplierAlertsAsync(int requestingUserId, int? branchId = null, string? priorityFilter = null);

        // ==================== DELIVERY & RECEIVING WORKFLOW ==================== //

        /// <summary>
        /// Record delivery receipt for facture
        /// </summary>
        /// <param name="factureId">Facture ID</param>
        /// <param name="deliveryDate">Actual delivery date</param>
        /// <param name="deliveryNoteNumber">Delivery note number</param>
        /// <param name="receivedByUserId">ID of user receiving delivery</param>
        /// <returns>Updated facture with delivery information</returns>
        Task<FactureDto?> RecordDeliveryAsync(int factureId, DateTime deliveryDate, string? deliveryNoteNumber, int receivedByUserId);

        /// <summary>
        /// Request clarification from supplier
        /// </summary>
        /// <param name="factureId">Facture ID</param>
        /// <param name="clarificationReason">Reason for clarification request</param>
        /// <param name="requestedByUserId">ID of user requesting clarification</param>
        /// <returns>True if request sent successfully</returns>
        Task<bool> RequestClarificationAsync(int factureId, string clarificationReason, int requestedByUserId);
    }
}