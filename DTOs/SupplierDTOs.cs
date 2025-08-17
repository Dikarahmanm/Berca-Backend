using System.ComponentModel.DataAnnotations;

namespace Berca_Backend.DTOs
{
    /// <summary>
    /// Supplier DTO for API responses with complete supplier information
    /// </summary>
    public class SupplierDto
    {
        public int Id { get; set; }
        public string SupplierCode { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int PaymentTerms { get; set; }
        public decimal CreditLimit { get; set; }
        public bool IsActive { get; set; }
        
        // Branch Information
        public int? BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public string BranchCode { get; set; } = string.Empty;
        
        // Audit Information
        public int CreatedBy { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public int? UpdatedBy { get; set; }
        public string? UpdatedByName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Display Properties
        public string StatusDisplay { get; set; } = string.Empty;
        public string PaymentTermsDisplay { get; set; } = string.Empty;
        public string CreditLimitDisplay { get; set; } = string.Empty;
        public string BranchDisplay { get; set; } = string.Empty;
        public bool HasCreditLimit { get; set; }
        public bool IsShortPaymentTerm { get; set; }
        public bool IsLongPaymentTerm { get; set; }
    }

    /// <summary>
    /// DTO for creating new suppliers with validation
    /// </summary>
    public class CreateSupplierDto
    {
        [Required(ErrorMessage = "Company name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Company name must be between 2 and 100 characters")]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact person is required")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Contact person must be between 2 and 50 characters")]
        public string ContactPerson { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required")]
        [Phone(ErrorMessage = "Invalid phone format")]
        [StringLength(15, MinimumLength = 10, ErrorMessage = "Phone must be between 10 and 15 characters")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100, ErrorMessage = "Email must not exceed 100 characters")]
        public string Email { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Address must not exceed 500 characters")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Payment terms is required")]
        [Range(1, 365, ErrorMessage = "Payment terms must be between 1 and 365 days")]
        public int PaymentTerms { get; set; } = 30;

        [Required(ErrorMessage = "Credit limit is required")]
        [Range(0, 999999999, ErrorMessage = "Credit limit must be between 0 and 999,999,999")]
        public decimal CreditLimit { get; set; } = 0;

        public int? BranchId { get; set; }

        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// DTO for updating existing suppliers
    /// </summary>
    public class UpdateSupplierDto
    {
        [Required(ErrorMessage = "Company name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Company name must be between 2 and 100 characters")]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact person is required")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Contact person must be between 2 and 50 characters")]
        public string ContactPerson { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required")]
        [Phone(ErrorMessage = "Invalid phone format")]
        [StringLength(15, MinimumLength = 10, ErrorMessage = "Phone must be between 10 and 15 characters")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100, ErrorMessage = "Email must not exceed 100 characters")]
        public string Email { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Address must not exceed 500 characters")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Payment terms is required")]
        [Range(1, 365, ErrorMessage = "Payment terms must be between 1 and 365 days")]
        public int PaymentTerms { get; set; }

        [Required(ErrorMessage = "Credit limit is required")]
        [Range(0, 999999999, ErrorMessage = "Credit limit must be between 0 and 999,999,999")]
        public decimal CreditLimit { get; set; }

        public int? BranchId { get; set; }

        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Lightweight DTO for supplier listing with essential fields
    /// </summary>
    public class SupplierListDto
    {
        public int Id { get; set; }
        public string SupplierCode { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int PaymentTerms { get; set; }
        public decimal CreditLimit { get; set; }
        public bool IsActive { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public string StatusDisplay { get; set; } = string.Empty;
        public string PaymentTermsDisplay { get; set; } = string.Empty;
        public string CreditLimitDisplay { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Minimal DTO for dropdown and reference usage
    /// </summary>
    public class SupplierSummaryDto
    {
        public int Id { get; set; }
        public string SupplierCode { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int PaymentTerms { get; set; }
        public decimal CreditLimit { get; set; }
        public string BranchName { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for supplier search and filtering parameters
    /// </summary>
    public class SupplierQueryDto
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

    /// <summary>
    /// DTO for supplier status toggle operations
    /// </summary>
    public class SupplierStatusDto
    {
        [Required]
        public bool IsActive { get; set; }
        
        [StringLength(200)]
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Paginated response for supplier listings
    /// </summary>
    public class SupplierPagedResponseDto
    {
        public List<SupplierListDto> Suppliers { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }
}