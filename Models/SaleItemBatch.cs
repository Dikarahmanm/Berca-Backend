using System.ComponentModel.DataAnnotations;

namespace Berca_Backend.Models
{
    /// <summary>
    /// Tracks which specific product batches were used in each sale item
    /// Enables complete sales traceability and FIFO compliance
    /// </summary>
    public class SaleItemBatch
    {
        public int Id { get; set; }

        [Required]
        public int SaleItemId { get; set; }

        [Required]
        public int BatchId { get; set; }

        [Required]
        [StringLength(50)]
        public string BatchNumber { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public int Quantity { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Cost per unit must be greater than or equal to 0")]
        public decimal CostPerUnit { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Total cost must be greater than or equal to 0")]
        public decimal TotalCost { get; set; }

        /// <summary>
        /// Expiry date of the batch at time of sale (for audit trail)
        /// </summary>
        public DateTime? ExpiryDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual SaleItem SaleItem { get; set; } = null!;
        public virtual ProductBatch Batch { get; set; } = null!;
    }
}