using Berca_Backend.DTOs;
using Berca_Backend.Models;

namespace Berca_Backend.Services.Interfaces
{
    /// <summary>
    /// Interface for Supplier Management Service
    /// Handles all supplier-related operations with branch integration
    /// </summary>
    public interface ISupplierService
    {
        // ==================== CRUD OPERATIONS ==================== //
        
        /// <summary>
        /// Get suppliers with filtering, searching and pagination
        /// </summary>
        /// <param name="queryParams">Query parameters for filtering and pagination</param>
        /// <param name="requestingUserId">ID of user making the request for branch access control</param>
        /// <returns>Paginated supplier response</returns>
        Task<SupplierPagedResponseDto> GetSuppliersAsync(SupplierQueryParams queryParams, int requestingUserId);

        /// <summary>
        /// Get supplier by ID with branch access validation
        /// </summary>
        /// <param name="supplierId">Supplier ID</param>
        /// <param name="requestingUserId">ID of user making the request</param>
        /// <returns>Supplier details or null if not found/unauthorized</returns>
        Task<SupplierDto?> GetSupplierByIdAsync(int supplierId, int requestingUserId);

        /// <summary>
        /// Get supplier by supplier code
        /// </summary>
        /// <param name="supplierCode">Unique supplier code</param>
        /// <param name="requestingUserId">ID of user making the request</param>
        /// <returns>Supplier details or null if not found</returns>
        Task<SupplierDto?> GetSupplierByCodeAsync(string supplierCode, int requestingUserId);

        /// <summary>
        /// Create new supplier with validation and audit trail
        /// </summary>
        /// <param name="createDto">Supplier creation data</param>
        /// <param name="createdByUserId">ID of user creating the supplier</param>
        /// <returns>Created supplier details</returns>
        Task<SupplierDto> CreateSupplierAsync(CreateSupplierDto createDto, int createdByUserId);

        /// <summary>
        /// Update existing supplier with validation and audit trail
        /// </summary>
        /// <param name="supplierId">Supplier ID to update</param>
        /// <param name="updateDto">Supplier update data</param>
        /// <param name="updatedByUserId">ID of user updating the supplier</param>
        /// <returns>Updated supplier details or null if not found</returns>
        Task<SupplierDto?> UpdateSupplierAsync(int supplierId, UpdateSupplierDto updateDto, int updatedByUserId);

        /// <summary>
        /// Soft delete supplier (set IsActive = false)
        /// </summary>
        /// <param name="supplierId">Supplier ID to delete</param>
        /// <param name="deletedByUserId">ID of user deleting the supplier</param>
        /// <param name="reason">Reason for deletion</param>
        /// <returns>True if deleted successfully</returns>
        Task<bool> DeleteSupplierAsync(int supplierId, int deletedByUserId, string? reason = null);

        /// <summary>
        /// Toggle supplier status (active/inactive)
        /// </summary>
        /// <param name="supplierId">Supplier ID</param>
        /// <param name="isActive">New status</param>
        /// <param name="updatedByUserId">ID of user updating status</param>
        /// <param name="reason">Reason for status change</param>
        /// <returns>Updated supplier or null if not found</returns>
        Task<SupplierDto?> ToggleSupplierStatusAsync(int supplierId, bool isActive, int updatedByUserId, string? reason = null);

        // ==================== BRANCH-SPECIFIC OPERATIONS ==================== //

        /// <summary>
        /// Get suppliers available to specific branch
        /// </summary>
        /// <param name="branchId">Branch ID</param>
        /// <param name="includeAll">Include suppliers available to all branches</param>
        /// <param name="activeOnly">Return only active suppliers</param>
        /// <returns>List of suppliers available to the branch</returns>
        Task<List<SupplierSummaryDto>> GetSuppliersByBranchAsync(int branchId, bool includeAll = true, bool activeOnly = true);

        /// <summary>
        /// Get active suppliers filtered by payment terms
        /// </summary>
        /// <param name="minDays">Minimum payment terms (days)</param>
        /// <param name="maxDays">Maximum payment terms (days)</param>
        /// <param name="branchId">Optional branch filter</param>
        /// <returns>List of suppliers matching payment terms criteria</returns>
        Task<List<SupplierSummaryDto>> GetActiveSuppliersByPaymentTermsAsync(int minDays, int maxDays, int? branchId = null);

        /// <summary>
        /// Get suppliers with credit limit above specified amount
        /// </summary>
        /// <param name="minCreditLimit">Minimum credit limit</param>
        /// <param name="branchId">Optional branch filter</param>
        /// <returns>List of suppliers with sufficient credit limit</returns>
        Task<List<SupplierSummaryDto>> GetSuppliersByCreditLimitAsync(decimal minCreditLimit, int? branchId = null);

        // ==================== VALIDATION & BUSINESS LOGIC ==================== //

        /// <summary>
        /// Check if supplier code is unique
        /// </summary>
        /// <param name="supplierCode">Supplier code to check</param>
        /// <param name="excludeSupplierId">Supplier ID to exclude from check (for updates)</param>
        /// <returns>True if code is unique</returns>
        Task<bool> IsSupplierCodeUniqueAsync(string supplierCode, int? excludeSupplierId = null);

        /// <summary>
        /// Check if company name is unique within branch
        /// </summary>
        /// <param name="companyName">Company name to check</param>
        /// <param name="branchId">Branch ID (null for global)</param>
        /// <param name="excludeSupplierId">Supplier ID to exclude from check</param>
        /// <returns>True if name is unique</returns>
        Task<bool> IsCompanyNameUniqueAsync(string companyName, int? branchId, int? excludeSupplierId = null);

        /// <summary>
        /// Check if email is unique across system
        /// </summary>
        /// <param name="email">Email to check</param>
        /// <param name="excludeSupplierId">Supplier ID to exclude from check</param>
        /// <returns>True if email is unique</returns>
        Task<bool> IsEmailUniqueAsync(string email, int? excludeSupplierId = null);

        /// <summary>
        /// Generate unique supplier code in SUP-YYYY-XXXXX format
        /// </summary>
        /// <returns>Unique supplier code</returns>
        Task<string> GenerateSupplierCodeAsync();

        // ==================== REPORTING & ANALYTICS ==================== //

        /// <summary>
        /// Get supplier statistics for dashboard
        /// </summary>
        /// <param name="branchId">Optional branch filter</param>
        /// <returns>Supplier statistics</returns>
        Task<SupplierStatsDto> GetSupplierStatsAsync(int? branchId = null);

        /// <summary>
        /// Get suppliers requiring attention (inactive, long payment terms, etc.)
        /// </summary>
        /// <param name="branchId">Optional branch filter</param>
        /// <returns>List of suppliers requiring attention</returns>
        Task<List<SupplierAlertDto>> GetSuppliersRequiringAttentionAsync(int? branchId = null);
    }

    /// <summary>
    /// DTO for supplier statistics
    /// </summary>
    public class SupplierStatsDto
    {
        public int TotalSuppliers { get; set; }
        public int ActiveSuppliers { get; set; }
        public int InactiveSuppliers { get; set; }
        public decimal TotalCreditLimit { get; set; }
        public decimal AveragePaymentTerms { get; set; }
        public int SuppliersWithCreditLimit { get; set; }
        public int ShortTermSuppliers { get; set; }
        public int LongTermSuppliers { get; set; }
    }

    /// <summary>
    /// DTO for supplier alerts and attention items
    /// </summary>
    public class SupplierAlertDto
    {
        public int Id { get; set; }
        public string SupplierCode { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty;
        public string AlertMessage { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty; // Low, Medium, High
        public DateTime CreatedAt { get; set; }
    }
}