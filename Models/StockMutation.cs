using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Berca_Backend.Models
{
    /// <summary>
    /// Stock mutation/movement tracking model for inventory management
    /// Tracks all stock movements including sales, purchases, adjustments, etc.
    /// </summary>
    public class StockMutation
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }
        public virtual Product Product { get; set; } = null!;

        [Required]
        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;

        public int? BranchId { get; set; }
        public virtual Branch? Branch { get; set; }

        [Required]
        public MutationType MutationType { get; set; }

        [Required]
        public int Quantity { get; set; } // Can be positive or negative

        public int StockBefore { get; set; }
        public int StockAfter { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? UnitCost { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalCost { get; set; }

        [StringLength(500)]
        public string Notes { get; set; } = string.Empty;

        [StringLength(50)]
        public string? ReferenceNumber { get; set; }

        public int? SaleId { get; set; }
        public virtual Sale? Sale { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Computed Properties
        [NotMapped]
        public bool IsInbound => Quantity > 0;

        [NotMapped]
        public bool IsOutbound => Quantity < 0;

        [NotMapped]
        public string MutationTypeDisplay => MutationType switch
        {
            MutationType.Sale => "Penjualan",
            MutationType.StockIn => "Stock Masuk",
            MutationType.StockOut => "Stock Keluar",
            MutationType.Return => "Retur",
            MutationType.Adjustment => "Penyesuaian",
            MutationType.Transfer => "Transfer",
            MutationType.Damaged => "Rusak",
            MutationType.Expired => "Kedaluwarsa",
            _ => "Lainnya"
        };
    }
}