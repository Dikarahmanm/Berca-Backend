using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Berca_Backend.Models
{
    /// <summary>
    /// Supplier entity for multi-branch supplier management
    /// Represents suppliers with branch integration and audit trail
    /// </summary>
    public class Supplier
    {
        public int Id { get; set; }

        // Supplier Identification
        [Required]
        [StringLength(20, MinimumLength = 3)]
        public string SupplierCode { get; set; } = string.Empty; // AUTO: SUP-YYYY-XXXXX

        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string CompanyName { get; set; } = string.Empty;

        [Required]
        [StringLength(50, MinimumLength = 2)]
        public string ContactPerson { get; set; } = string.Empty;

        // Contact Information
        [Required]
        [StringLength(15, MinimumLength = 10)]
        [Phone]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [StringLength(500)]
        public string Address { get; set; } = string.Empty;

        // Business Terms
        [Range(1, 365)]
        public int PaymentTerms { get; set; } = 30; // Days

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999)]
        public decimal CreditLimit { get; set; } = 0;

        // Branch Relationship
        public int? BranchId { get; set; } // NULL = Available to all branches
        public virtual Branch? Branch { get; set; }

        // Status
        public bool IsActive { get; set; } = true;

        // Audit Trail
        public int CreatedBy { get; set; }
        public virtual User CreatedByUser { get; set; } = null!;

        public int? UpdatedBy { get; set; }
        public virtual User? UpdatedByUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual ICollection<Facture> Factures { get; set; } = new List<Facture>();

        // Computed Properties
        [NotMapped]
        public string StatusDisplay => IsActive ? "Aktif" : "Tidak Aktif";

        [NotMapped]
        public string PaymentTermsDisplay => $"{PaymentTerms} hari";

        [NotMapped]
        public string CreditLimitDisplay => CreditLimit.ToString("C", new System.Globalization.CultureInfo("id-ID"));

        [NotMapped]
        public string BranchDisplay => Branch?.BranchName ?? "Semua Cabang";

        [NotMapped]
        public bool HasCreditLimit => CreditLimit > 0;

        [NotMapped]
        public bool IsShortPaymentTerm => PaymentTerms <= 7;

        [NotMapped]
        public bool IsLongPaymentTerm => PaymentTerms >= 60;
    }

    /// <summary>
    /// Supplier query parameters for filtering and searching
    /// </summary>
    public class SupplierQueryParams
    {
        public string? Search { get; set; }
        public int? BranchId { get; set; }
        public bool? IsActive { get; set; }
        public int? MinPaymentTerms { get; set; }
        public int? MaxPaymentTerms { get; set; }
        public decimal? MinCreditLimit { get; set; }
        public decimal? MaxCreditLimit { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "CompanyName";
        public string SortOrder { get; set; } = "asc";
    }
}