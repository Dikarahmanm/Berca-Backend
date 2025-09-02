// Models/InventoryMutation.cs - Sprint 2 Stock Movement
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Berca_Backend.Models
{
    public class InventoryMutation
    {
        public int Id { get; set; }

        // Foreign Key
        [Required]
        public int ProductId { get; set; }
        public virtual Product Product { get; set; } = null!;

        // Mutation Details
        [Required]
        public MutationType Type { get; set; }

        [Required]
        public int Quantity { get; set; } // Positive for IN, Negative for OUT

        public int StockBefore { get; set; }
        public int StockAfter { get; set; }

        [Required]
        [StringLength(500)]
        public string Notes { get; set; } = string.Empty;

        [StringLength(50)]
        public string? ReferenceNumber { get; set; } // Invoice, Sale Number, etc.

        // Related Transaction (if applicable)
        public int? SaleId { get; set; }
        public virtual Sale? Sale { get; set; }

        // Cost Information (for valuation)
        [Column(TypeName = "decimal(18,2)")]
        public decimal? UnitCost { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalCost { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }

        // Computed Properties
        [NotMapped]
        public string TypeDisplay => Type switch
        {
            MutationType.StockIn => "Masuk",
            MutationType.StockOut => "Keluar",
            MutationType.Sale => "Penjualan",
            MutationType.Purchase => "Pembelian", // ? ADD missing Purchase type display
            MutationType.Return => "Retur",
            MutationType.Adjustment => "Penyesuaian",
            MutationType.Damaged => "Rusak",
            MutationType.Expired => "Kedaluwarsa",
            MutationType.Transfer => "Transfer",
            _ => "Lainnya"
        };

        [NotMapped]
        public bool IsStockIn => Quantity > 0;

        [NotMapped]
        public bool IsStockOut => Quantity < 0;
    }

    /// <summary>
    /// Jenis mutasi stok. Gunakan string pada JSON agar lebih aman dan mudah dibaca.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MutationType
    {
        StockIn,      // "StockIn"
        StockOut,     // "StockOut"
        Sale,         // "Sale"
        Purchase,     // "Purchase" ? ADD missing Purchase type
        Return,       // "Return"
        Adjustment,   // "Adjustment"
        Transfer,     // "Transfer"
        Damaged,      // "Damaged"
        Expired       // "Expired"
    }
}