// DTOs/SaleDTOs.cs - Sprint 2 Sale DTOs (FIXED)
using System.ComponentModel.DataAnnotations; // ✅ ADDED

namespace Berca_Backend.DTOs
{
    // Sale Response DTO
    public class SaleDto
    {
        public int Id { get; set; }
        public string SaleNumber { get; set; } = string.Empty;
        public DateTime SaleDate { get; set; } // ✅ Fixed: was Date
        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Total { get; set; } // ✅ Fixed: was Total
        public decimal AmountPaid { get; set; } // ✅ Fixed: was PaidAmount  
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
        public bool ReceiptPrinted { get; set; } // ✅ Fixed: was IsReceiptPrinted
        public DateTime? ReceiptPrintedAt { get; set; }
        public List<SaleItemDto> Items { get; set; } = new(); // ✅ Fixed: was SaleItems
        public DateTime CreatedAt { get; set; }
        public int TotalItems { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal DiscountPercentage { get; set; } = 0;
        public int RedeemedPoints { get; set; } = 0; // ✅ Added: for loyalty points
        public string? ReceiptFooterMessage { get; set; } // ✅ Added: for custom footer message
        public string? ReceiptStoreName { get; set; } = "Toko Eniwan"; // ✅ Added: default store name
        public string? ReceiptStoreAddress { get; set; } = string.Empty; // ✅ Added: default store address
        public string? ReceiptStorePhone { get; set; } = string.Empty; // ✅ Added: default store phone
        public string? ReceiptStoreEmail { get; set; } = null; // ✅ Added: default store email
        public string? ReceiptStoreLogoUrl { get; set; } = null; // ✅ Added: for store logo in receipt
        public string? ReceiptStoreWebsite { get; set; } = null; // ✅ Added: for store website in receipt
        public string? ReceiptStoreTitle { get; set; } = "Toko Eniwan"; // ✅ Added: for store title in receipt
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
        public decimal Subtotal { get; set; } // ✅ Fixed: was TotalPrice
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

        //// ✅ Added missing properties that services expect
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

        // ✅ Support frontend percentage format
        [Range(0, 100)]
        public decimal Discount { get; set; } = 0; // Percentage 0-100

        public decimal SellPrice { get; set; } = 0; // Price from frontend

        // ✅ Support backend amount format  
        [Range(0, double.MaxValue)]
        public decimal DiscountAmount { get; set; } = 0; // Amount in rupiah

        [StringLength(500)]
        public string? Notes { get; set; }

        // ✅ Backend service fields
        public decimal UnitPrice { get; set; } = 0;
        public decimal TotalPrice { get; set; } = 0;
    }

    // Sale Summary DTO
    public class SaleSummaryDto
    {
        public decimal TotalSales { get; set; }
        public int TransactionCount { get; set; } // ✅ Added
        public decimal AverageTransaction { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal TotalDiscount { get; set; } // ✅ Added
        public decimal TotalTax { get; set; } // ✅ Added
        public DateTime StartDate { get; set; } // ✅ Added
        public DateTime EndDate { get; set; } // ✅ Added
    }
}