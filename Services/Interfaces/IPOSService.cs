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
    }

    // Note: All DTOs have been moved to Berca_Backend.DTOs namespace to avoid duplicates
}