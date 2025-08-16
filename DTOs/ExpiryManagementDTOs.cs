using System.ComponentModel.DataAnnotations;
using Berca_Backend.Models;

namespace Berca_Backend.DTOs
{
    // ==================== PRODUCT BATCH DTOs ==================== //

    /// <summary>
    /// DTO for creating new product batch with expiry tracking
    /// </summary>
    public class CreateProductBatchDto
    {
        [Required(ErrorMessage = "Product ID is required")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Batch number is required")]
        [StringLength(50, ErrorMessage = "Batch number cannot exceed 50 characters")]
        public string BatchNumber { get; set; } = string.Empty;

        /// <summary>
        /// Expiry date (required for categories that require expiry tracking)
        /// </summary>
        public DateTime? ExpiryDate { get; set; }

        /// <summary>
        /// Production/Manufacturing date
        /// </summary>
        public DateTime? ProductionDate { get; set; }

        [Required(ErrorMessage = "Initial stock is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Initial stock must be greater than 0")]
        public int InitialStock { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Cost per unit must be greater than or equal to 0")]
        public decimal CostPerUnit { get; set; } = 0;

        [StringLength(100, ErrorMessage = "Supplier name cannot exceed 100 characters")]
        public string? SupplierName { get; set; }

        [StringLength(50, ErrorMessage = "Purchase order number cannot exceed 50 characters")]
        public string? PurchaseOrderNumber { get; set; }

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }

        public int? BranchId { get; set; }
    }

    /// <summary>
    /// DTO for updating product batch information
    /// </summary>
    public class UpdateProductBatchDto
    {
        [Required(ErrorMessage = "Batch number is required")]
        [StringLength(50, ErrorMessage = "Batch number cannot exceed 50 characters")]
        public string BatchNumber { get; set; } = string.Empty;

        public DateTime? ExpiryDate { get; set; }
        public DateTime? ProductionDate { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Current stock must be greater than or equal to 0")]
        public int CurrentStock { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Cost per unit must be greater than or equal to 0")]
        public decimal CostPerUnit { get; set; } = 0;

        [StringLength(100, ErrorMessage = "Supplier name cannot exceed 100 characters")]
        public string? SupplierName { get; set; }

        [StringLength(50, ErrorMessage = "Purchase order number cannot exceed 50 characters")]
        public string? PurchaseOrderNumber { get; set; }

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }

        public bool IsBlocked { get; set; } = false;

        [StringLength(200, ErrorMessage = "Block reason cannot exceed 200 characters")]
        public string? BlockReason { get; set; }
    }

    /// <summary>
    /// DTO for product batch response data
    /// </summary>
    public class ProductBatchDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime? ExpiryDate { get; set; }
        public DateTime? ProductionDate { get; set; }
        public int CurrentStock { get; set; }
        public int InitialStock { get; set; }
        public decimal CostPerUnit { get; set; }
        public string? SupplierName { get; set; }
        public string? PurchaseOrderNumber { get; set; }
        public string? Notes { get; set; }
        public bool IsBlocked { get; set; }
        public string? BlockReason { get; set; }
        public bool IsExpired { get; set; }
        public bool IsDisposed { get; set; }
        public DateTime? DisposalDate { get; set; }
        public string? DisposalMethod { get; set; }
        public int? BranchId { get; set; }
        public string? BranchName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? CreatedByUserName { get; set; }
        public string? UpdatedByUserName { get; set; }

        // Computed properties
        public int? DaysUntilExpiry { get; set; }
        public ExpiryStatus ExpiryStatus { get; set; }
        public int AvailableStock { get; set; }
    }

    // ==================== EXPIRY TRACKING DTOs ==================== //

    /// <summary>
    /// DTO for products expiring soon
    /// </summary>
    public class ExpiringProductDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductBarcode { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryColor { get; set; } = string.Empty;
        public int BatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public int DaysUntilExpiry { get; set; }
        public ExpiryStatus ExpiryStatus { get; set; }
        public int CurrentStock { get; set; }
        public int AvailableStock { get; set; }
        public decimal ValueAtRisk { get; set; }
        public decimal CostPerUnit { get; set; }
        public ExpiryUrgency UrgencyLevel { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? BranchId { get; set; }
        public string? BranchName { get; set; }
        public string? SupplierName { get; set; }
        public bool IsBlocked { get; set; }
    }

    /// <summary>
    /// DTO for expired products
    /// </summary>
    public class ExpiredProductDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductBarcode { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int BatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public int DaysExpired { get; set; }
        public int CurrentStock { get; set; }
        public decimal ValueLost { get; set; }
        public decimal CostPerUnit { get; set; }
        public DateTime ExpiredAt { get; set; }
        public bool RequiresDisposal { get; set; }
        public DisposalUrgency DisposalUrgency { get; set; }
        public int? BranchId { get; set; }
        public string? BranchName { get; set; }
        public bool IsDisposed { get; set; }
        public DateTime? DisposalDate { get; set; }
        public string? DisposalMethod { get; set; }
        public string? SupplierName { get; set; }
    }

    // ==================== FIFO RECOMMENDATION DTOs ==================== //

    /// <summary>
    /// DTO for FIFO sales recommendations
    /// </summary>
    public class FifoRecommendationDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductBarcode { get; set; } = string.Empty;
        public List<BatchRecommendationDto> BatchRecommendations { get; set; } = new();
        public int TotalAvailableStock { get; set; }
        public decimal AverageCostPerUnit { get; set; }
    }

    /// <summary>
    /// DTO for individual batch recommendations in FIFO order
    /// </summary>
    public class BatchRecommendationDto
    {
        public int BatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime? ExpiryDate { get; set; }
        public int AvailableStock { get; set; }
        public decimal CostPerUnit { get; set; }
        public ExpiryStatus ExpiryStatus { get; set; }
        public int? DaysUntilExpiry { get; set; }
        public int RecommendedSaleOrder { get; set; } // 1 = sell first, 2 = sell second, etc.
        public string RecommendationReason { get; set; } = string.Empty;
    }

    // ==================== DISPOSAL DTOs ==================== //

    /// <summary>
    /// DTO for disposing expired products
    /// </summary>
    public class DisposeExpiredProductsDto
    {
        [Required]
        public List<int> BatchIds { get; set; } = new();

        [Required(ErrorMessage = "Disposal method is required")]
        [StringLength(100, ErrorMessage = "Disposal method cannot exceed 100 characters")]
        public string DisposalMethod { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }
    }

    // ==================== ANALYTICS DTOs ==================== //

    /// <summary>
    /// DTO for expiry analytics per branch
    /// </summary>
    public class ExpiryAnalyticsDto
    {
        public int? BranchId { get; set; }
        public string? BranchName { get; set; }
        public DateTime AnalysisDate { get; set; }
        public int TotalProductsWithExpiry { get; set; }
        public int ExpiringIn7Days { get; set; }
        public int ExpiringIn3Days { get; set; }
        public int ExpiredProducts { get; set; }
        public int DisposedProducts { get; set; }
        public decimal ValueAtRisk { get; set; }
        public decimal ValueLost { get; set; }
        public decimal WastagePercentage { get; set; }
        public List<CategoryExpiryStatsDto> CategoryStats { get; set; } = new();
    }

    /// <summary>
    /// DTO for category-wise expiry statistics
    /// </summary>
    public class CategoryExpiryStatsDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryColor { get; set; } = string.Empty;
        public int TotalProducts { get; set; }
        public int ExpiringProducts { get; set; }
        public int ExpiredProducts { get; set; }
        public decimal ValueAtRisk { get; set; }
        public decimal ValueLost { get; set; }
    }

    // ==================== FILTER DTOs ==================== //

    /// <summary>
    /// DTO for filtering expiring products
    /// </summary>
    public class ExpiringProductsFilterDto
    {
        public int? CategoryId { get; set; }
        public int? BranchId { get; set; }
        public ExpiryStatus? ExpiryStatus { get; set; }
        public int? DaysUntilExpiry { get; set; }
        public bool? IncludeBlocked { get; set; } = false;
        public string? SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "expiry_date";
        public string SortOrder { get; set; } = "asc";
    }

    /// <summary>
    /// DTO for filtering expired products
    /// </summary>
    public class ExpiredProductsFilterDto
    {
        public int? CategoryId { get; set; }
        public int? BranchId { get; set; }
        public bool? IsDisposed { get; set; }
        public DateTime? ExpiredAfter { get; set; }
        public DateTime? ExpiredBefore { get; set; }
        public string? SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "expiry_date";
        public string SortOrder { get; set; } = "desc";
    }

    // ==================== DISPOSAL DTOs ==================== //

    /// <summary>
    /// DTO for disposing a product batch
    /// </summary>
    public class DisposeBatchDto
    {
        [Required]
        [StringLength(100, ErrorMessage = "Disposal method cannot exceed 100 characters")]
        public string DisposalMethod { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Disposal reason cannot exceed 500 characters")]
        public string? DisposalReason { get; set; }

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }

        public bool ForceDisposal { get; set; } = false;
    }

    // ==================== EXPIRY VALIDATION DTO ==================== //

    /// <summary>
    /// DTO for validating expiry requirements when creating/updating products
    /// </summary>
    public class ExpiryValidationDto
    {
        public int ProductId { get; set; }
        public int CategoryId { get; set; }
        public bool CategoryRequiresExpiry { get; set; }
        public DateTime? ProvidedExpiryDate { get; set; }
        public bool IsValid { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
    }

    // ==================== ENUMS ==================== //

    /// <summary>
    /// Expiry urgency levels for notifications
    /// </summary>
    public enum ExpiryUrgency
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Critical = 3
    }

    /// <summary>
    /// Disposal urgency levels
    /// </summary>
    public enum DisposalUrgency
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Critical = 3
    }

    /// <summary>
    /// Expiry notification types constants
    /// </summary>
    public static class ExpiryNotificationTypes
    {
        public const string EXPIRY_WARNING = "expiry_warning";
        public const string EXPIRY_URGENT = "expiry_urgent";
        public const string EXPIRY_EXPIRED = "expiry_expired";
        public const string EXPIRY_REQUIRED = "expiry_required";
        public const string DISPOSAL_COMPLETED = "disposal_completed";
        public const string EXPIRY_DAILY_SUMMARY = "expiry_daily_summary";
        public const string FIFO_RECOMMENDATION = "fifo_recommendation";
    }
}