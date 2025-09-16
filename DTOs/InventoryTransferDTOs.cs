using System.ComponentModel.DataAnnotations;
using Berca_Backend.Models;

namespace Berca_Backend.DTOs
{
    // ==================== REQUEST/RESPONSE DTOs ==================== //

    /// <summary>
    /// Request DTO for creating a new inventory transfer
    /// </summary>
    public class CreateInventoryTransferRequestDto
    {
        [Required(ErrorMessage = "Source branch is required")]
        public int SourceBranchId { get; set; }

        [Required(ErrorMessage = "Destination branch is required")]
        public int DestinationBranchId { get; set; }

        [Required(ErrorMessage = "Transfer type is required")]
        public TransferType Type { get; set; }

        [Required(ErrorMessage = "Priority is required")]
        public TransferPriority Priority { get; set; }

        [Required(ErrorMessage = "Request reason is required")]
        [StringLength(500, ErrorMessage = "Request reason cannot exceed 500 characters")]
        public string RequestReason { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
        public string? Notes { get; set; }

        public decimal EstimatedCost { get; set; }

        public DateTime? EstimatedDeliveryDate { get; set; }

        [Required(ErrorMessage = "Transfer items are required")]
        [MinLength(1, ErrorMessage = "At least one transfer item is required")]
        public List<CreateTransferItemDto> TransferItems { get; set; } = new List<CreateTransferItemDto>();
    }

    /// <summary>
    /// DTO for creating transfer items
    /// </summary>
    public class CreateTransferItemDto
    {
        [Required(ErrorMessage = "Product ID is required")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public int Quantity { get; set; }

        public DateTime? ExpiryDate { get; set; }

        [StringLength(50)]
        public string? BatchNumber { get; set; }

        [StringLength(200)]
        public string? QualityNotes { get; set; }
    }

    /// <summary>
    /// Request DTO for bulk transfer creation
    /// </summary>
    public class BulkTransferRequestDto
    {
        [Required(ErrorMessage = "Source branch is required")]
        public int SourceBranchId { get; set; }

        [Required(ErrorMessage = "Destination branch is required")]
        public int DestinationBranchId { get; set; }

        [Required(ErrorMessage = "Request reason is required")]
        [StringLength(500)]
        public string RequestReason { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Notes { get; set; }

        public TransferPriority Priority { get; set; } = TransferPriority.Normal;

        [Required(ErrorMessage = "Product transfers are required")]
        [MinLength(1, ErrorMessage = "At least one product transfer is required")]
        public List<BulkTransferItemDto> ProductTransfers { get; set; } = new List<BulkTransferItemDto>();
    }

    public class BulkTransferItemDto
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        public DateTime? ExpiryDate { get; set; }
        public string? BatchNumber { get; set; }
    }

    /// <summary>
    /// Request DTO for approving/rejecting a transfer
    /// </summary>
    public class TransferApprovalRequestDto
    {
        [Required(ErrorMessage = "Approval decision is required")]
        public bool Approved { get; set; }

        [StringLength(500)]
        public string? ApprovalNotes { get; set; }

        /// <summary>
        /// Individual item approvals with specific quantities
        /// </summary>
        public List<TransferItemApprovalDto>? ItemApprovals { get; set; }

        /// <summary>
        /// Manager override flag for exceptional cases
        /// </summary>
        public bool? ManagerOverride { get; set; }

        public decimal? AdjustedCost { get; set; }
        public DateTime? AdjustedDeliveryDate { get; set; }
    }

    /// <summary>
    /// Transfer item approval DTO
    /// </summary>
    public class TransferItemApprovalDto
    {
        [Required]
        public int TransferItemId { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Approved quantity must be non-negative")]
        public int ApprovedQuantity { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        /// <summary>
        /// Substitute product ID if different product is approved instead
        /// </summary>
        public int? SubstituteProductId { get; set; }
    }

    /// <summary>
    /// Request DTO for shipping a transfer
    /// </summary>
    public class TransferShipmentRequestDto
    {
        [StringLength(100)]
        public string? LogisticsProvider { get; set; }

        [StringLength(100)]
        public string? TrackingNumber { get; set; }

        public DateTime? EstimatedDeliveryDate { get; set; }

        public decimal? ActualCost { get; set; }

        [StringLength(500)]
        public string? ShippingNotes { get; set; }
    }

    /// <summary>
    /// Request DTO for receiving a transfer
    /// </summary>
    public class TransferReceiptRequestDto
    {
        [Required(ErrorMessage = "Receipt items are required")]
        public List<TransferReceiptItemDto> ReceivedItems { get; set; } = new List<TransferReceiptItemDto>();

        [StringLength(1000)]
        public string? ReceiptNotes { get; set; }

        public bool HasDamages { get; set; }

        [StringLength(500)]
        public string? DamageNotes { get; set; }
    }

    public class TransferReceiptItemDto
    {
        [Required]
        public int TransferItemId { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int ReceivedQuantity { get; set; }

        public bool IsAccepted { get; set; } = true;

        [StringLength(200)]
        public string? QualityNotes { get; set; }
    }

    /// <summary>
    /// Query parameters for filtering transfer lists
    /// </summary>
    public class InventoryTransferQueryParams
    {
        public int? SourceBranchId { get; set; }
        public int? DestinationBranchId { get; set; }
        public TransferStatus? Status { get; set; }
        public TransferType? Type { get; set; }
        public TransferPriority? Priority { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "CreatedAt";
        public string SortOrder { get; set; } = "desc";
    }

    // ==================== RESPONSE DTOs ==================== //

    /// <summary>
    /// Complete transfer information DTO
    /// </summary>
    public class InventoryTransferDto
    {
        public int Id { get; set; }
        public string TransferNumber { get; set; } = string.Empty;
        public TransferStatus Status { get; set; }
        public string StatusDisplay { get; set; } = string.Empty;
        public TransferType Type { get; set; }
        public string TypeDisplay { get; set; } = string.Empty;
        public TransferPriority Priority { get; set; }
        public string PriorityDisplay { get; set; } = string.Empty;

        // Branch Information
        public BranchSummaryDto SourceBranch { get; set; } = new BranchSummaryDto();
        public BranchSummaryDto DestinationBranch { get; set; } = new BranchSummaryDto();

        // Transfer Details
        public string RequestReason { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public decimal EstimatedCost { get; set; }
        public decimal ActualCost { get; set; }
        public decimal DistanceKm { get; set; }

        // Transfer Items
        public List<InventoryTransferItemDto> TransferItems { get; set; } = new List<InventoryTransferItemDto>();

        // Workflow Information
        public UserSummaryDto RequestedBy { get; set; } = new UserSummaryDto();
        public UserSummaryDto? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public UserSummaryDto? ShippedBy { get; set; }
        public DateTime? ShippedAt { get; set; }
        public UserSummaryDto? ReceivedBy { get; set; }
        public DateTime? ReceivedAt { get; set; }
        public UserSummaryDto? CancelledBy { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancellationReason { get; set; }

        // Logistics
        public string? LogisticsProvider { get; set; }
        public string? TrackingNumber { get; set; }
        public DateTime? EstimatedDeliveryDate { get; set; }

        // Computed Properties
        public int TotalItems { get; set; }
        public decimal TotalValue { get; set; }
        public bool RequiresManagerApproval { get; set; }
        public bool IsEmergencyTransfer { get; set; }
        public TimeSpan? ProcessingTime { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Transfer item detail DTO
    /// </summary>
    public class InventoryTransferItemDto
    {
        public int Id { get; set; }
        public ProductSummaryDto Product { get; set; } = new ProductSummaryDto();
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
        public int SourceStockBefore { get; set; }
        public int SourceStockAfter { get; set; }
        public int? DestinationStockBefore { get; set; }
        public int? DestinationStockAfter { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? BatchNumber { get; set; }
        public string? QualityNotes { get; set; }
        public bool IsExpired { get; set; }
        public bool IsNearExpiry { get; set; }
    }

    /// <summary>
    /// Summary DTO for transfer lists
    /// </summary>
    public class InventoryTransferSummaryDto
    {
        public int Id { get; set; }
        public string TransferNumber { get; set; } = string.Empty;
        public TransferStatus Status { get; set; }
        public string StatusDisplay { get; set; } = string.Empty;
        public TransferType Type { get; set; }
        public TransferPriority Priority { get; set; }
        public string SourceBranchName { get; set; } = string.Empty;
        public string DestinationBranchName { get; set; } = string.Empty;
        public int TotalItems { get; set; }
        public decimal TotalValue { get; set; }
        public string RequestedByName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? EstimatedDeliveryDate { get; set; }
        public bool RequiresManagerApproval { get; set; }
        public bool IsEmergencyTransfer { get; set; }
    }

    // ==================== ANALYTICS DTOs ==================== //

    /// <summary>
    /// Transfer analytics summary
    /// </summary>
    public class TransferAnalyticsDto
    {
        public TransferMetricsDto Metrics { get; set; } = new TransferMetricsDto();
        public List<BranchTransferStatsDto> BranchStats { get; set; } = new List<BranchTransferStatsDto>();
        public List<TransferTrendDto> TrendAnalysis { get; set; } = new List<TransferTrendDto>();
        public List<ProductTransferStatsDto> TopTransferredProducts { get; set; } = new List<ProductTransferStatsDto>();
        public List<TransferEfficiencyDto> EfficiencyMetrics { get; set; } = new List<TransferEfficiencyDto>();
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Overall transfer metrics
    /// </summary>
    public class TransferMetricsDto
    {
        public int TotalTransfers { get; set; }
        public int PendingTransfers { get; set; }
        public int CompletedTransfers { get; set; }
        public int CancelledTransfers { get; set; }
        public decimal TotalTransferValue { get; set; }
        public decimal AverageTransferValue { get; set; }
        public decimal AverageProcessingTime { get; set; } // in hours
        public decimal SuccessRate { get; set; } // percentage
        public decimal AverageCostPerKm { get; set; }
        public int EmergencyTransfers { get; set; }
    }

    /// <summary>
    /// Branch-specific transfer statistics
    /// </summary>
    public class BranchTransferStatsDto
    {
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public string BranchCode { get; set; } = string.Empty;
        public int TransfersOut { get; set; }
        public int TransfersIn { get; set; }
        public decimal OutboundValue { get; set; }
        public decimal InboundValue { get; set; }
        public decimal NetTransferValue { get; set; }
        public decimal AverageTransferTime { get; set; }
        public decimal SuccessRate { get; set; }
    }

    /// <summary>
    /// Transfer trend analysis
    /// </summary>
    public class TransferTrendDto
    {
        public DateTime Date { get; set; }
        public int TransferCount { get; set; }
        public decimal TransferValue { get; set; }
        public decimal AverageProcessingTime { get; set; }
        public int EmergencyCount { get; set; }
    }

    /// <summary>
    /// Product transfer statistics
    /// </summary>
    public class ProductTransferStatsDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public int TotalTransferCount { get; set; }
        public int TotalQuantityTransferred { get; set; }
        public decimal TotalTransferValue { get; set; }
        public int FrequencyRank { get; set; }
        public List<string> CommonRoutes { get; set; } = new List<string>();
    }

    /// <summary>
    /// Transfer efficiency metrics
    /// </summary>
    public class TransferEfficiencyDto
    {
        public string Route { get; set; } = string.Empty; // "Branch A → Branch B"
        public int TransferCount { get; set; }
        public decimal AverageDistance { get; set; }
        public decimal AverageCost { get; set; }
        public decimal AverageTime { get; set; }
        public decimal CostPerKm { get; set; }
        public decimal SuccessRate { get; set; }
        public string RecommendedImprovement { get; set; } = string.Empty;
    }

    // ==================== SUGGESTIONS DTOs ==================== //

    /// <summary>
    /// AI-powered transfer suggestions
    /// </summary>
    public class TransferSuggestionsDto
    {
        public List<StockRebalancingSuggestionDto> RebalancingSuggestions { get; set; } = new List<StockRebalancingSuggestionDto>();
        public List<EmergencyTransferSuggestionDto> EmergencyAlerts { get; set; } = new List<EmergencyTransferSuggestionDto>();
        public List<OptimizationSuggestionDto> OptimizationSuggestions { get; set; } = new List<OptimizationSuggestionDto>();
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Stock rebalancing suggestions
    /// </summary>
    public class StockRebalancingSuggestionDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int SourceBranchId { get; set; }
        public string SourceBranchName { get; set; } = string.Empty;
        public int DestinationBranchId { get; set; }
        public string DestinationBranchName { get; set; } = string.Empty;
        public int SuggestedQuantity { get; set; }
        public string Reason { get; set; } = string.Empty;
        public TransferPriority RecommendedPriority { get; set; }
        public decimal EstimatedCost { get; set; }
        public decimal PotentialSavings { get; set; }
    }

    /// <summary>
    /// Emergency transfer alerts
    /// </summary>
    public class EmergencyTransferSuggestionDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int CriticalBranchId { get; set; }
        public string CriticalBranchName { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public int MinimumStock { get; set; }
        public int DaysUntilStockout { get; set; }
        public List<AvailableSourceDto> AvailableSources { get; set; } = new List<AvailableSourceDto>();
        public string AlertLevel { get; set; } = string.Empty; // Critical, High, Medium
    }

    public class AvailableSourceDto
    {
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public int AvailableStock { get; set; }
        public decimal Distance { get; set; }
        public decimal EstimatedCost { get; set; }
        public int EstimatedDeliveryDays { get; set; }
    }

    /// <summary>
    /// Transfer optimization suggestions
    /// </summary>
    public class OptimizationSuggestionDto
    {
        public string Category { get; set; } = string.Empty; // Route, Cost, Time, etc.
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public decimal PotentialSavings { get; set; }
        public string Impact { get; set; } = string.Empty; // High, Medium, Low
    }

    // ==================== HELPER DTOs ==================== //

    /// <summary>
    /// Branch summary for transfer DTOs
    /// </summary>
    public class BranchSummaryDto
    {
        public int Id { get; set; }
        public string BranchCode { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string ManagerName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }

    /// <summary>
    /// User summary for transfer DTOs
    /// </summary>
    public class UserSummaryDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? BranchName { get; set; }
    }

    /// <summary>
    /// Product summary for transfer DTOs
    /// </summary>
    public class ProductSummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal SellPrice { get; set; }
        public int CurrentStock { get; set; }
        public int MinimumStock { get; set; }
        public bool IsLowStock { get; set; }
    }
}