// DTOs/ProductDTOs.cs - Sprint 2 Product DTOs (FIXED)
using System.ComponentModel.DataAnnotations; // ✅ ADDED

namespace Berca_Backend.DTOs
{
    // Product Response DTO
    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal BuyPrice { get; set; }
        public decimal SellPrice { get; set; }
        public int Stock { get; set; }
        public int MinimumStock { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? CategoryColor { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public decimal ProfitMargin { get; set; }
        public bool IsLowStock { get; set; }
        public bool IsOutOfStock { get; set; }
    }

    // Create Product Request
    public class CreateProductRequest
    {
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
        public decimal BuyPrice { get; set; }

        [Required(ErrorMessage = "Sell price is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Sell price must be greater than or equal to 0")]
        public decimal SellPrice { get; set; }

        [Required(ErrorMessage = "Stock is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock must be greater than or equal to 0")]
        public int Stock { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Minimum stock must be greater than or equal to 0")]
        public int MinimumStock { get; set; } = 5;

        [StringLength(20, ErrorMessage = "Unit cannot exceed 20 characters")]
        public string Unit { get; set; } = "pcs";

        [Required(ErrorMessage = "Category is required")]
        public int CategoryId { get; set; }

        public bool IsActive { get; set; } = true;
    }

    // Update Product Request
    public class UpdateProductRequest
    {
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
        public decimal BuyPrice { get; set; }

        [Required(ErrorMessage = "Sell price is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Sell price must be greater than or equal to 0")]
        public decimal SellPrice { get; set; }

        [Required(ErrorMessage = "Stock is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock must be greater than or equal to 0")]
        public int Stock { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Minimum stock must be greater than or equal to 0")]
        public int MinimumStock { get; set; } = 5;

        [StringLength(20, ErrorMessage = "Unit cannot exceed 20 characters")]
        public string Unit { get; set; } = "pcs";

        [Required(ErrorMessage = "Category is required")]
        public int CategoryId { get; set; }

        public bool IsActive { get; set; } = true;
    }

    // Stock Update Request
    public class StockUpdateRequest
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public int Quantity { get; set; }

        [Required]
        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string Notes { get; set; } = string.Empty;

        [StringLength(50)]
        public string? ReferenceNumber { get; set; }

        public decimal? UnitCost { get; set; }
    }

    // Product List Response with Pagination
    public class ProductListResponse
    {
        public List<ProductDto> Products { get; set; } = new();
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
    }

    // Barcode Check Response
    public class BarcodeCheckResponse
    {
        public bool Exists { get; set; }
        public ProductDto? Product { get; set; }
    }

}