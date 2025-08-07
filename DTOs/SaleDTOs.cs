// DTOs/SaleDTOs.cs - COMPLETE Sales DTOs with all missing classes
using System.ComponentModel.DataAnnotations;

namespace Berca_Backend.DTOs
{
    // Sale Response DTO
    public class SaleDto
    {
        public int Id { get; set; }
        public string SaleNumber { get; set; } = string.Empty;
        public DateTime SaleDate { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; } = 0;
        public decimal Total { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal ChangeAmount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? PaymentReference { get; set; }
        public int? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? MemberNumber { get; set; }
        public string? CustomerName { get; set; }
        public int CashierId { get; set; }
        public string CashierName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public bool ReceiptPrinted { get; set; }
        public DateTime? ReceiptPrintedAt { get; set; }
        public List<SaleItemDto> Items { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public int TotalItems { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal DiscountPercentage { get; set; } = 0;
        public int RedeemedPoints { get; set; } = 0;
        public string? ReceiptFooterMessage { get; set; }
        public string? ReceiptStoreName { get; set; } = "Toko Eniwan";
        public string? ReceiptStoreAddress { get; set; } = string.Empty;
        public string? ReceiptStorePhone { get; set; } = string.Empty;
        public string? ReceiptStoreEmail { get; set; } = null;
        public string? ReceiptStoreLogoUrl { get; set; } = null;
        public string? ReceiptStoreWebsite { get; set; } = null;
        public string? ReceiptStoreTitle { get; set; } = "Toko Eniwan";
    }

    // Sale Item DTO
    public class SaleItemDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductBarcode { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal UnitCost { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal Subtotal { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal DiscountPercentage { get; set; } = 0;
    }

    // Create Sale Request
    public class CreateSaleRequest
    {
        [Required]
        public List<CreateSaleItemRequest> Items { get; set; } = new();

        [Required]
        [StringLength(20)]
        public string PaymentMethod { get; set; } = string.Empty;

        [StringLength(100)]
        public string? PaymentReference { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal AmountPaid { get; set; }

        public int? MemberId { get; set; }

        [StringLength(100)]
        public string? CustomerName { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        [Range(0, double.MaxValue)]
        public decimal DiscountAmount { get; set; } = 0;

        [Range(0, double.MaxValue)]
        public decimal TaxAmount { get; set; } = 0;

        public decimal SubTotal { get; set; }
        public decimal DiscountPercentage { get; set; } = 0;
        public decimal Total { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal ChangeAmount { get; set; }
        public int RedeemedPoints { get; set; } = 0;
    }

    // Create Sale Item Request
    public class CreateSaleItemRequest
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Range(0, 100)]
        public decimal Discount { get; set; } = 0;

        public decimal SellPrice { get; set; } = 0;

        [Range(0, double.MaxValue)]
        public decimal DiscountAmount { get; set; } = 0;

        [StringLength(500)]
        public string? Notes { get; set; }

        public decimal UnitPrice { get; set; } = 0;
        public decimal TotalPrice { get; set; } = 0;
    }

    // ✅ ENHANCED: Sale Summary DTO with comprehensive reporting data
    public class SaleSummaryDto
    {
        // Basic metrics
        public decimal TotalSales { get; set; }
        public int TransactionCount { get; set; }
        public decimal AverageTransaction { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal TotalTax { get; set; } = 0;
        public int TotalItemsSold { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        
        // Enhanced reporting data (optional)
        public List<PaymentMethodBreakdownDto>? PaymentMethodBreakdown { get; set; }
        public List<CategoryPerformanceDto>? CategoryPerformance { get; set; }
        public List<TopSellingProductDto>? TopSellingProducts { get; set; }
        public List<SalesTrendDto>? SalesTrend { get; set; }
    }

    // ✅ NEW: Payment Method Breakdown DTO
    public class PaymentMethodBreakdownDto
    {
        public string MethodName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int TransactionCount { get; set; }
        public decimal Percentage { get; set; }
    }

    // ✅ NEW: Category Performance DTO
    public class CategoryPerformanceDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryColor { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
        public int TotalItemsSold { get; set; }
        public int ProductCount { get; set; }
        public decimal AveragePrice { get; set; }
        public decimal GrowthPercentage { get; set; }
    }

    // ✅ NEW: Top Selling Product DTO  
    public class TopSellingProductDto
    {
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int TotalSold { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal Percentage { get; set; }
    }

    // ✅ NEW: Sales Trend DTO
    public class SalesTrendDto
    {
        public DateTime Date { get; set; }
        public decimal Sales { get; set; }
        public int Transactions { get; set; }
    }

    // ✅ MISSING: Daily Sales DTO
    public class DailySalesDto
    {
        public DateTime Date { get; set; }
        public decimal TotalSales { get; set; }
        public int TransactionCount { get; set; }
        public decimal AverageTransaction { get; set; }
    }

    // ✅ MISSING: Payment Method Summary DTO


    // ✅ MISSING: Receipt Data DTO
    public class ReceiptDataDto
    {
        public SaleDto Sale { get; set; } = new();
        public string StoreName { get; set; } = string.Empty;
        public string StoreAddress { get; set; } = string.Empty;
        public string StorePhone { get; set; } = string.Empty;
        public string StoreEmail { get; set; } = string.Empty;
        public string FooterMessage { get; set; } = string.Empty;
    }
}