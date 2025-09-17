using System;

namespace Berca_Backend.DTOs
{
    /// <summary>
    /// DTO for branch-specific product data with branch-aware stock information
    /// </summary>
    public class BranchProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public decimal SellPrice { get; set; }
        public decimal BuyPrice { get; set; }

        /// <summary>
        /// Original product stock (global)
        /// </summary>
        public int Stock { get; set; }

        /// <summary>
        /// Branch-specific stock for this product
        /// </summary>
        public int BranchStock { get; set; }

        /// <summary>
        /// The specific branch ID this stock data is for
        /// </summary>
        public int BranchId { get; set; }

        /// <summary>
        /// Branch name for display purposes
        /// </summary>
        public string BranchName { get; set; } = string.Empty;

        public int MinStock { get; set; }
        public string Unit { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public bool IsActive { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Indicates if this product has low stock in this branch
        /// </summary>
        public bool IsLowStock => BranchStock <= MinStock;

        /// <summary>
        /// Stock status for this branch
        /// </summary>
        public string StockStatus => BranchStock <= 0 ? "Out of Stock" :
                                   BranchStock <= MinStock ? "Low Stock" : "In Stock";
    }
}