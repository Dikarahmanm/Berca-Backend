using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;

namespace Berca_Backend.Models
{
    /// <summary>
    /// FactureItem entity for supplier invoice line items
    /// Supports both product-based mapping and custom supplier item descriptions
    /// Includes delivery verification capabilities
    /// </summary>
    public class FactureItem
    {
        public int Id { get; set; }

        // Facture relationship
        [Required]
        public int FactureId { get; set; }
        public virtual Facture? Facture { get; set; }

        // Product relationship (nullable for custom items)
        public int? ProductId { get; set; }
        public virtual Product? Product { get; set; }

        // Supplier item details
        [StringLength(100)]
        public string? SupplierItemCode { get; set; }

        [Required]
        [StringLength(500, MinimumLength = 3)]
        public string SupplierItemDescription { get; set; } = string.Empty;

        // Quantities from supplier invoice
        [Required]
        [Range(0.01, 999999.99)]
        public decimal Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99)]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; } = 0;

        [Range(0, 999999.99)]
        public decimal? VerifiedQuantity { get; set; }

        // Delivery verification quantities
        [Range(0, 999999.99)]
        public decimal? ReceivedQuantity { get; set; }

        [Range(0, 999999.99)]
        public decimal? AcceptedQuantity { get; set; }

        // Tax and discount per line item
        [Column(TypeName = "decimal(5,2)")]
        [Range(0, 100)]
        public decimal TaxRate { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999.99)]
        public decimal DiscountAmount { get; set; } = 0;

        // Line item notes and verification
        [StringLength(1000)]
        public string? Notes { get; set; }

        [StringLength(500)]
        public string? VerificationNotes { get; set; }

        public bool IsVerified { get; set; } = false;

        public DateTime? VerifiedAt { get; set; }

        public int? VerifiedBy { get; set; }
        public virtual User? VerifiedByUser { get; set; }

        // Audit fields
        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public DateTime UpdatedAt { get; set; }

        // Computed properties
        [NotMapped]
        public decimal LineTotal => Quantity * UnitPrice - DiscountAmount;

        [NotMapped]
        public decimal TaxAmount => (LineTotal * TaxRate) / 100;

        [NotMapped]
        public decimal LineTotalWithTax => LineTotal + TaxAmount;

        [NotMapped]
        public string UnitPriceDisplay => UnitPrice.ToString("C", new CultureInfo("id-ID"));

        [NotMapped]
        public string LineTotalDisplay => LineTotal.ToString("C", new CultureInfo("id-ID"));

        [NotMapped]
        public string LineTotalWithTaxDisplay => LineTotalWithTax.ToString("C", new CultureInfo("id-ID"));

        [NotMapped]
        public string ItemDescription => Product?.Name ?? SupplierItemDescription;

        [NotMapped]
        public string ItemCode => Product?.Barcode ?? SupplierItemCode ?? "";

        [NotMapped]
        public bool IsProductMapped => ProductId.HasValue;

        [NotMapped]
        public bool HasQuantityVariance
        {
            get
            {
                if (!ReceivedQuantity.HasValue) return false;
                return Math.Abs(Quantity - ReceivedQuantity.Value) > 0.01m;
            }
        }

        [NotMapped]
        public bool HasAcceptanceVariance
        {
            get
            {
                if (!AcceptedQuantity.HasValue || !ReceivedQuantity.HasValue) return false;
                return Math.Abs(ReceivedQuantity.Value - AcceptedQuantity.Value) > 0.01m;
            }
        }

        [NotMapped]
        public string VerificationStatus
        {
            get
            {
                if (!IsVerified) return "Belum Diverifikasi";
                if (HasQuantityVariance) return "Ada Selisih Quantity";
                if (HasAcceptanceVariance) return "Ada Penolakan";
                return "Diverifikasi";
            }
        }

        [NotMapped]
        public decimal QuantityVariance
        {
            get
            {
                if (!ReceivedQuantity.HasValue) return 0;
                return ReceivedQuantity.Value - Quantity;
            }
        }

        [NotMapped]
        public decimal AcceptanceVariance
        {
            get
            {
                if (!AcceptedQuantity.HasValue || !ReceivedQuantity.HasValue) return 0;
                return AcceptedQuantity.Value - ReceivedQuantity.Value;
            }
        }

        [NotMapped]
        public string UnitDisplay => Product?.Unit ?? "pcs";

        [NotMapped]
        public bool RequiresApproval => HasQuantityVariance || HasAcceptanceVariance;
    }
}