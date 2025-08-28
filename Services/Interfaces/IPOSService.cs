// Services/IPOSService.cs - Sprint 2 Point of Sale Service Interface (FIXED: Remove duplicate DTOs)
using Berca_Backend.DTOs;

namespace Berca_Backend.Services
{
    public interface IPOSService
    {
        // Transaction Processing
        Task<SaleDto> CreateSaleAsync(CreateSaleRequest request, int cashierId);
        Task<SaleDto?> GetSaleByIdAsync(int id);
        Task<SaleDto?> GetSaleByNumberAsync(string saleNumber);
        Task<List<SaleDto>> GetSalesAsync(DateTime? startDate = null, DateTime? endDate = null, int? cashierId = null, string? paymentMethod = null, int page = 1, int pageSize = 20);

        // Receipt Management
        Task<bool> MarkReceiptPrintedAsync(int saleId);
        Task<ReceiptDataDto> GetReceiptDataAsync(int saleId);

        // Sale Management
        Task<bool> CancelSaleAsync(int saleId, string reason);
        Task<SaleDto> RefundSaleAsync(int saleId, string reason, int processedBy);

        // Sales Analytics
        Task<SaleSummaryDto> GetSalesSummaryAsync(DateTime startDate, DateTime endDate);
        Task<List<DailySalesDto>> GetDailySalesAsync(DateTime startDate, DateTime endDate);
        Task<List<PaymentMethodSummaryDto>> GetPaymentMethodSummaryAsync(DateTime startDate, DateTime endDate);

        // Validation
        Task<bool> ValidateStockAvailabilityAsync(List<CreateSaleItemRequest> items);
        Task<decimal> CalculateTotalAsync(List<CreateSaleItemRequest> items, decimal discountAmount = 0, decimal taxAmount = 0);

        // ==================== BATCH MANAGEMENT METHODS ==================== //

        // Batch selection for POS
        Task<List<ProductBatchDto>> GetAvailableBatchesForSaleAsync(int productId);
        Task<List<BatchAllocationDto>> GenerateFifoSuggestionsAsync(int productId, int quantity);

        // Sales with batch tracking
        Task<SaleWithBatchesResponseDto> CreateSaleWithBatchesAsync(CreateSaleWithBatchesRequest request, int cashierId, int branchId);
        Task<BatchAllocationValidationDto> ValidateBatchAllocationAsync(ValidateBatchAllocationRequest request);

        // Batch summary for completed sales
        Task<List<SaleItemWithBatchDto>> GetSaleBatchSummaryAsync(int saleId);

        // ==================== MEMBER CREDIT INTEGRATION METHODS ==================== //

        /// <summary>
        /// Validate member credit for POS transaction
        /// </summary>
        /// <param name="request">Credit validation request</param>
        /// <returns>Validation result with approval status</returns>
        Task<CreditValidationResultDto> ValidateMemberCreditAsync(CreditValidationRequestDto request);

        /// <summary>
        /// Create sale with member credit payment
        /// </summary>
        /// <param name="request">Sale with credit request</param>
        /// <returns>Created sale details</returns>
        Task<SaleDto> CreateSaleWithCreditAsync(CreateSaleWithCreditDto request);

        /// <summary>
        /// Apply credit payment to existing sale
        /// </summary>
        /// <param name="request">Credit payment request</param>
        /// <returns>Payment processing result</returns>
        Task<PaymentResultDto> ApplyCreditPaymentAsync(ApplyCreditPaymentDto request);

        /// <summary>
        /// Get member credit information for POS display
        /// </summary>
        /// <param name="identifier">Phone, member number, or ID</param>
        /// <returns>POS-optimized member credit information</returns>
        Task<POSMemberCreditDto?> GetMemberCreditForPOSAsync(string identifier);

        /// <summary>
        /// Process credit transaction for completed sale
        /// </summary>
        /// <param name="saleId">Sale ID</param>
        /// <param name="memberId">Member ID</param>
        /// <param name="creditAmount">Credit amount</param>
        /// <returns>Success status</returns>
        Task<bool> ProcessCreditTransactionAsync(int saleId, int memberId, decimal creditAmount);

        /// <summary>
        /// Get credit information for sale receipt
        /// </summary>
        /// <param name="saleId">Sale ID</param>
        /// <returns>Credit information for receipt</returns>
        Task<SaleCreditInfoDto?> GetSaleCreditInfoAsync(int saleId);

        /// <summary>
        /// Check member credit eligibility for POS operations
        /// </summary>
        /// <param name="memberId">Member ID</param>
        /// <returns>Credit eligibility information</returns>
        Task<MemberCreditEligibilityDto?> CheckMemberCreditEligibilityAsync(int memberId);

        /// <summary>
        /// Validate credit amount against member limits and business rules
        /// </summary>
        /// <param name="memberId">Member ID</param>
        /// <param name="requestedAmount">Requested credit amount</param>
        /// <param name="items">Sale items for risk assessment</param>
        /// <returns>True if amount is valid</returns>
        Task<bool> ValidateCreditAmountAsync(int memberId, decimal requestedAmount, List<SaleItemDto> items);

        /// <summary>
        /// Calculate credit transaction risk score
        /// </summary>
        /// <param name="memberId">Member ID</param>
        /// <param name="amount">Transaction amount</param>
        /// <param name="items">Sale items</param>
        /// <returns>Risk score and level</returns>
        Task<(int riskScore, string riskLevel)> CalculateTransactionRiskAsync(int memberId, decimal amount, List<SaleItemDto> items);

        /// <summary>
        /// Get credit payment terms for member
        /// </summary>
        /// <param name="memberId">Member ID</param>
        /// <returns>Payment terms in days</returns>
        Task<int> GetMemberPaymentTermsAsync(int memberId);

        /// <summary>
        /// Update sale with credit transaction details
        /// </summary>
        /// <param name="saleId">Sale ID</param>
        /// <param name="creditTransactionId">Credit transaction ID</param>
        /// <param name="creditAmount">Credit amount</param>
        /// <returns>Success status</returns>
        Task<bool> UpdateSaleWithCreditDetailsAsync(int saleId, int creditTransactionId, decimal creditAmount);
    }

    // Note: All DTOs have been moved to Berca_Backend.DTOs namespace to avoid duplicates
}