// DTOs/CategoryDTOs.cs
using System.ComponentModel.DataAnnotations;

namespace Berca_Backend.DTOs
{
    // DTO for creating new category
    public class CreateCategoryDto
    {
        [Required(ErrorMessage = "Category name is required")]
        [StringLength(100, ErrorMessage = "Category name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Color is required")]
        [RegularExpression(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$",
            ErrorMessage = "Color must be a valid hex color (e.g., #FF914D)")]
        public string Color { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        /// <summary>
        /// Indicates if products in this category require expiry date tracking
        /// True for: Makanan, Minuman, Obat, Kesehatan
        /// False for: Elektronik, Rumah Tangga
        /// </summary>
        public bool RequiresExpiryDate { get; set; } = false;

        /// <summary>
        /// Default expiry warning days for this category (days before expiry to show warnings)
        /// </summary>
        [Range(1, 365, ErrorMessage = "Expiry warning days must be between 1 and 365")]
        public int DefaultExpiryWarningDays { get; set; } = 7;
    }

    // DTO for updating category
    public class UpdateCategoryDto
    {
        [Required(ErrorMessage = "Category name is required")]
        [StringLength(100, ErrorMessage = "Category name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Color is required")]
        [RegularExpression(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$",
            ErrorMessage = "Color must be a valid hex color (e.g., #FF914D)")]
        public string Color { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        /// <summary>
        /// Indicates if products in this category require expiry date tracking
        /// </summary>
        public bool RequiresExpiryDate { get; set; } = false;

        /// <summary>
        /// Default expiry warning days for this category
        /// </summary>
        [Range(1, 365, ErrorMessage = "Expiry warning days must be between 1 and 365")]
        public int DefaultExpiryWarningDays { get; set; } = 7;
    }

    // DTO for returning category data
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool RequiresExpiryDate { get; set; } = false;
        public int DefaultExpiryWarningDays { get; set; } = 7;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int ProductCount { get; set; } = 0;
        public int ExpiringProductsCount { get; set; } = 0; // Products expiring soon in this category
        public int ExpiredProductsCount { get; set; } = 0;  // Expired products in this category
    }

    // DTO for category list with pagination
    public class CategoryListDto
    {
        public List<CategoryDto> Categories { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    // DTO for category filter/search
    public class CategoryFilterDto
    {
        public string? SearchTerm { get; set; }
        public string? Color { get; set; }
        public bool? RequiresExpiryDate { get; set; } // Filter by expiry requirement
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string SortBy { get; set; } = "name";
        public string SortOrder { get; set; } = "asc"; // asc or desc
    }

    // DTO for categories requiring expiry tracking
    public class CategoryWithExpiryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public int DefaultExpiryWarningDays { get; set; }
        public int ProductsWithExpiryCount { get; set; } = 0;
        public int ExpiringProductsCount { get; set; } = 0;
        public int ExpiredProductsCount { get; set; } = 0;
    }
}