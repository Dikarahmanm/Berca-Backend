// Models/Category.cs - Fixed (tanpa Product reference)
using System.ComponentModel.DataAnnotations;

namespace Berca_Backend.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Category name is required")]
        [StringLength(100, ErrorMessage = "Category name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Color is required")]
        [RegularExpression(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$",
            ErrorMessage = "Color must be a valid hex color (e.g., #FF914D)")]
        public string Color { get; set; } = "#FF914D"; // Default orange

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // ✅ REMOVED: Navigation property untuk Product (akan ditambah nanti di Sprint 3)
        // public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}