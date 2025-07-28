// Models/Product.cs - Sprint 2
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Berca_Backend.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Product name is required")]
        [StringLength(100, ErrorMessage = "Product name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Barcode is required")]
        [StringLength(50, ErrorMessage = "Barcode cannot exceed 50 characters")]
        public string Barcode { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Buy price is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Buy price must be greater than or equal to 0")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal BuyPrice { get; set; }

        [Required(ErrorMessage = "Sell price is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Sell price must be greater than or equal to 0")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal SellPrice { get; set; }

        [Required(ErrorMessage = "Stock is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock must be greater than or equal to 0")]
        public int Stock { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Minimum stock must be greater than or equal to 0")]
        public int MinimumStock { get; set; } = 5;

        [StringLength(20, ErrorMessage = "Unit cannot exceed 20 characters")]
        public string Unit { get; set; } = "pcs"; // pcs, kg, liter, etc.

        [StringLength(255, ErrorMessage = "Image URL cannot exceed 255 characters")]
        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

        // Foreign Key
        [Required(ErrorMessage = "Category is required")]
        public int CategoryId { get; set; }

        // Navigation Properties
        public virtual Category Category { get; set; } = null!;
        public virtual ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
        public virtual ICollection<InventoryMutation> InventoryMutations { get; set; } = new List<InventoryMutation>();

        // Audit Fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }

        // Computed Properties
        [NotMapped]
        public decimal ProfitMargin => SellPrice > 0 ? ((SellPrice - BuyPrice) / SellPrice) * 100 : 0;

        [NotMapped]
        public bool IsLowStock => Stock <= MinimumStock;

        [NotMapped]
        public bool IsOutOfStock => Stock <= 0;
    }
}