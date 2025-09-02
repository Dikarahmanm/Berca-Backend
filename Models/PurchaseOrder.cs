using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Berca_Backend.Models
{
    /// <summary>
    /// Purchase Order entity for supplier procurement management
    /// </summary>
    public class PurchaseOrder
    {
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string PurchaseOrderNumber { get; set; } = string.Empty;

        [Required]
        public int SupplierId { get; set; }
        public virtual Supplier Supplier { get; set; } = null!;

        public int? BranchId { get; set; }
        public virtual Branch? Branch { get; set; }

        [Required]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        public DateTime? DeliveryDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

        [StringLength(1000)]
        public string Notes { get; set; } = string.Empty;

        [Required]
        public int CreatedBy { get; set; }
        public virtual User CreatedByUser { get; set; } = null!;

        // Alias for backward compatibility
        [NotMapped]
        public virtual User User => CreatedByUser;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
    }

    /// <summary>
    /// Purchase Order Item entity
    /// </summary>
    public class PurchaseOrderItem
    {
        public int Id { get; set; }

        [Required]
        public int PurchaseOrderId { get; set; }
        public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;

        [Required]
        public int ProductId { get; set; }
        public virtual Product Product { get; set; } = null!;

        [Required]
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        [StringLength(200)]
        public string Notes { get; set; } = string.Empty;
    }

    /// <summary>
    /// Purchase Order Status enumeration
    /// </summary>
    public enum PurchaseOrderStatus
    {
        Draft = 0,
        Pending = 1,
        Approved = 2,
        Ordered = 3,
        Received = 4,
        Completed = 5,
        Cancelled = 6
    }
}