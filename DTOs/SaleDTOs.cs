// DTOs/SaleDTOs.cs - Sprint 2 Sale DTOs (FIXED)
using System.ComponentModel.DataAnnotations; // ✅ ADDED

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
        public decimal TaxAmount { get; set; }
        public decimal Total { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal ChangeAmount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? PaymentReference { get; set; }
        public int? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? CustomerName { get; set; }
        public int CashierId { get; set; }
        public string CashierName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public bool ReceiptPrinted { get; set; }
        public DateTime? ReceiptPrintedAt { get; set; }
        public List<SaleItemDto> SaleItems { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public int TotalItems { get; set; }
        public decimal TotalProfit { get; set; }
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
    }

    // Create Sale Item Request
    public class CreateSaleItemRequest
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal DiscountAmount { get; set; } = 0;

        [StringLength(500)]
        public string? Notes { get; set; }
    }

    // Sale Summary DTO
    public class SaleSummaryDto
    {
        public decimal TotalSales { get; set; }
        public int TotalTransactions { get; set; }
        public decimal AverageTransaction { get; set; }
        public decimal TotalProfit { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
    }
}