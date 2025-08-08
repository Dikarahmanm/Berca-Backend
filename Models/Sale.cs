// Models/Sale.cs - Sprint 2 Transaction Header
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Berca_Backend.Models
{
    public class Sale
    {
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string SaleNumber { get; set; } = string.Empty;

        [Required]
        public DateTime SaleDate { get; set; } = DateTime.UtcNow;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; } = 0;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountPaid { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ChangeAmount { get; set; }

        [Required]
        [StringLength(20)]
        public string PaymentMethod { get; set; } = string.Empty;

        [StringLength(100)]
        public string? PaymentReference { get; set; }

        // Customer/Member Info
        public int? MemberId { get; set; }
        public virtual Member? Member { get; set; }

        [StringLength(100)]
        public string? CustomerName { get; set; }

        // Cashier Info
        [Required]
        public int CashierId { get; set; }
        public virtual User Cashier { get; set; } = null!;

        // Status
        public SaleStatus Status { get; set; } = SaleStatus.Completed;

        [StringLength(500)]
        public string? Notes { get; set; }

        // Receipt Info
        public bool ReceiptPrinted { get; set; } = false;
        public DateTime? ReceiptPrintedAt { get; set; }

        // Additional fields for services
        public DateTime? CancelledAt { get; set; }
        public string? CancellationReason { get; set; }
        public DateTime? RefundedAt { get; set; }
        public string? RefundReason { get; set; }
        public int? OriginalSaleId { get; set; }

        // Navigation Properties
        public virtual ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public decimal DiscountPercentage { get; set; } = 0;

        // ✅ ADDED: Computed Properties for Financial Reports
        [NotMapped]
        public int TotalItems => SaleItems?.Sum(si => si.Quantity) ?? 0;

        [NotMapped]
        public decimal TotalProfit => SaleItems?.Sum(si => si.TotalProfit) ?? 0;

        [NotMapped]
        public decimal TotalCost => SaleItems?.Sum(si => si.UnitCost * si.Quantity) ?? 0;

        [NotMapped]
        public decimal Tax => TaxAmount; // Use existing TaxAmount property

        // ✅ ADDED: Additional computed properties for completeness
        [NotMapped]
        public decimal GrossProfit => Total - TotalCost;

        [NotMapped]
        public decimal NetProfit => GrossProfit - Tax;
        // ✅ BUG FIX: Add RedeemedPoints to track points used in the sale
        public int RedeemedPoints { get; set; } = 0;
    }

    public enum SaleStatus
    {
        Pending = 0,
        Completed = 1,
        Cancelled = 2,
        Refunded = 3
    }
}