using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Berca_Backend.Models
{
    /// <summary>
    /// Branch-specific inventory management for Toko Eniwan multi-branch system
    /// Each product can have different stock levels per branch
    /// </summary>
    public class BranchInventory
    {
        public int Id { get; set; }

        // Branch and Product relationship
        [Required]
        public int BranchId { get; set; }
        public virtual Branch Branch { get; set; } = null!;

        [Required]
        public int ProductId { get; set; }
        public virtual Product Product { get; set; } = null!;

        // Stock Information
        [Required]
        public int Stock { get; set; } = 0;

        [Required]
        public int MinimumStock { get; set; } = 0;

        [Required]
        public int MaximumStock { get; set; } = 1000;

        // Cost Information per branch (can vary by branch)
        [Column(TypeName = "decimal(18,2)")]
        public decimal BuyPrice { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal SellPrice { get; set; } = 0;

        // Location in branch
        [StringLength(100)]
        public string? LocationCode { get; set; }

        [StringLength(200)]
        public string? LocationDescription { get; set; }

        // Status
        public bool IsActive { get; set; } = true;

        // Audit Trail
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastStockUpdate { get; set; }

        // Computed Properties
        [NotMapped]
        public string StockStatus
        {
            get
            {
                if (Stock <= 0) return "Out of Stock";
                if (Stock <= MinimumStock) return "Low Stock";
                return "In Stock";
            }
        }

        [NotMapped]
        public decimal StockValue => Stock * SellPrice;

        [NotMapped]
        public int StockPercentage
        {
            get
            {
                if (MaximumStock <= 0) return 0;
                return (int)((double)Stock / MaximumStock * 100);
            }
        }

        [NotMapped]
        public bool NeedsRestock => Stock <= MinimumStock;

        [NotMapped]
        public bool IsOverstocked => Stock > MaximumStock;
    }
}