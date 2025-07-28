// Services/IPOSService.cs - Sprint 2 Point of Sale Service Interface
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
    }

    public class ReceiptDataDto
    {
        public SaleDto Sale { get; set; } = null!;
        public string StoreName { get; set; } = "Toko Eniwan";
        public string StoreAddress { get; set; } = string.Empty;
        public string StorePhone { get; set; } = string.Empty;
        public string? StoreEmail { get; set; }
        public string? FooterMessage { get; set; }
    }

    public class DailySalesDto
    {
        public DateTime Date { get; set; }
        public decimal TotalSales { get; set; }
        public int TransactionCount { get; set; }
        public decimal AverageTransaction { get; set; }
    }

    public class PaymentMethodSummaryDto
    {
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int TransactionCount { get; set; }
        public decimal Percentage { get; set; }
    }
}