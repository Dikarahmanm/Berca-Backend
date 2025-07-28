// Models/SaleItem.cs - Sprint 2 Transaction Detail
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Berca_Backend.Models
{
    public class SaleItem
    {
        public int Id { get; set; }

        // Foreign Keys
        [Required]
        public int SaleId { get; set; }
        public virtual Sale Sale { get; set; } = null!;

        [Required]
        public int ProductId { get; set; }
        public virtual Product Product { get; set; } = null!;

        // Item Details (snapshot saat transaksi)
        [Required]
        [StringLength(100)]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string ProductBarcode { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; } // Harga jual saat transaksi

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitCost { get; set; } // Harga beli untuk kalkulasi profit

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } = 0;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; } // (UnitPrice * Quantity) - DiscountAmount

        [StringLength(20)]
        public string Unit { get; set; } = "pcs";

        [StringLength(500)]
        public string? Notes { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Computed Properties
        [NotMapped]
        public decimal TotalProfit => (UnitPrice - UnitCost) * Quantity - DiscountAmount;

        [NotMapped]
        public decimal DiscountPercentage => UnitPrice > 0 ? (DiscountAmount / (UnitPrice * Quantity)) * 100 : 0;
    }
}