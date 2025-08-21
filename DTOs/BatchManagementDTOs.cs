using System.ComponentModel.DataAnnotations;

namespace Berca_Backend.DTOs
{
    // ==================== REQUEST DTOs ==================== //

    /// <summary>
    /// Request for adding stock with flexible batch options
    /// Supports new batch creation, existing batch update, or simple stock addition
    /// </summary>
    public class AddStockToBatchRequest
    {
        /// <summary>
        /// Existing batch ID (for adding to existing batch)
        /// </summary>
        public int? BatchId { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Cost per unit is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Cost per unit must be greater than or equal to 0")]
        public decimal CostPerUnit { get; set; }

        /// <summary>
        /// Batch number for new batch creation
        /// </summary>
        [StringLength(50, ErrorMessage = "Batch number cannot exceed 50 characters")]
        public string? BatchNumber { get; set; }

        /// <summary>
        /// Expiry date for new batch (ISO format: YYYY-MM-DD)
        /// </summary>
        public string? ExpiryDate { get; set; }

        /// <summary>
        /// Production date for new batch (ISO format: YYYY-MM-DD)
        /// </summary>
        public string? ProductionDate { get; set; }

        [StringLength(100, ErrorMessage = "Supplier name cannot exceed 100 characters")]
        public string? SupplierName { get; set; }

        [StringLength(50, ErrorMessage = "Purchase order number cannot exceed 50 characters")]
        public string? PurchaseOrderNumber { get; set; }

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Request for creating sale with complete batch tracking
    /// </summary>
    public class CreateSaleWithBatchesRequest
    {
        [Required(ErrorMessage = "Total is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Total must be greater than or equal to 0")]
        public decimal Total { get; set; }

        [Required(ErrorMessage = "Payment method is required")]
        [StringLength(50, ErrorMessage = "Payment method cannot exceed 50 characters")]
        public string PaymentMethod { get; set; } = string.Empty;

        [Required(ErrorMessage = "Received amount is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Received amount must be greater than or equal to 0")]
        public decimal ReceivedAmount { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Change must be greater than or equal to 0")]
        public decimal Change { get; set; }

        public int? MemberId { get; set; }

        [Required(ErrorMessage = "Items are required")]
        [MinLength(1, ErrorMessage = "At least one item is required")]
        public List<SaleItemWithBatchesDto> Items { get; set; } = new();
    }

    /// <summary>
    /// Sale item with batch allocation details
    /// </summary>
    public class SaleItemWithBatchesDto
    {
        [Required(ErrorMessage = "Product ID is required")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Unit price is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Unit price must be greater than or equal to 0")]
        public decimal UnitPrice { get; set; }

        [Required(ErrorMessage = "Subtotal is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Subtotal must be greater than or equal to 0")]
        public decimal Subtotal { get; set; }

        /// <summary>
        /// How the quantity is allocated across different batches
        /// </summary>
        public List<BatchAllocationDto> BatchAllocations { get; set; } = new();
    }

    /// <summary>
    /// Batch allocation for sale item
    /// </summary>
    public class BatchAllocationDto
    {
        [Required(ErrorMessage = "Batch ID is required")]
        public int BatchId { get; set; }

        [Required(ErrorMessage = "Batch number is required")]
        public string BatchNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public int Quantity { get; set; }

        public DateTime? ExpiryDate { get; set; }

        /// <summary>
        /// Days until expiry (negative if expired)
        /// </summary>
        public int? DaysUntilExpiry { get; set; }

        /// <summary>
        /// CSS class for urgency: "good", "warning", "critical", "expired"
        /// </summary>
        public string UrgencyClass { get; set; } = "good";

        /// <summary>
        /// Material icon name for frontend display
        /// </summary>
        public string UrgencyIcon { get; set; } = "check_circle";

        /// <summary>
        /// Human-readable expiry text: "3 days until expiry", "Expired 2 days ago"
        /// </summary>
        public string ExpiryText { get; set; } = "No expiry";
    }

    /// <summary>
    /// Request for validating batch allocation before sale
    /// </summary>
    public class ValidateBatchAllocationRequest
    {
        [Required(ErrorMessage = "Items are required")]
        [MinLength(1, ErrorMessage = "At least one item is required")]
        public List<SaleItemWithBatchesDto> Items { get; set; } = new();
    }

    // ==================== RESPONSE DTOs ==================== //

    /// <summary>
    /// Product with comprehensive batch summary for inventory display
    /// </summary>
    public class ProductWithBatchSummaryDto : ProductDto
    {
        /// <summary>
        /// Total number of batches for this product
        /// </summary>
        public int TotalBatches { get; set; }

        /// <summary>
        /// Batch with nearest expiry date (FIFO priority)
        /// </summary>
        public ProductBatchDto? NearestExpiryBatch { get; set; }

        /// <summary>
        /// Total value of all batches combined
        /// </summary>
        public decimal TotalValueAllBatches { get; set; }

        /// <summary>
        /// FIFO recommendation text for inventory management
        /// </summary>
        public string? FifoRecommendation { get; set; }

        /// <summary>
        /// Summary of batch statuses: "2 critical, 1 warning, 5 good"
        /// </summary>
        public string BatchStatusSummary { get; set; } = "No batches";
    }

    /// <summary>
    /// Response after adding stock to batch
    /// </summary>
    public class AddStockResponseDto
    {
        public bool Success { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int? BatchId { get; set; }
        public string? BatchNumber { get; set; }
        public int AddedQuantity { get; set; }
        public int NewBatchStock { get; set; }
        public int NewProductTotalStock { get; set; }
        public decimal WeightedAverageCost { get; set; }
        public string? Message { get; set; }
    }

    /// <summary>
    /// Response after creating sale with batch tracking
    /// </summary>
    public class SaleWithBatchesResponseDto
    {
        public int Id { get; set; }
        public string SaleNumber { get; set; } = string.Empty;
        public DateTime SaleDate { get; set; }
        public decimal Total { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal ReceivedAmount { get; set; }
        public decimal Change { get; set; }
        public int? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string CashierName { get; set; } = string.Empty;
        public List<SaleItemWithBatchDto> Items { get; set; } = new();
        public bool BatchTrackingEnabled { get; set; }
    }

    /// <summary>
    /// Sale item with its batch usage details
    /// </summary>
    public class SaleItemWithBatchDto
    {
        public int SaleItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }
        public List<SaleItemBatchDto> BatchesUsed { get; set; } = new();
    }

    /// <summary>
    /// Batch usage detail for a sale item
    /// </summary>
    public class SaleItemBatchDto
    {
        public int BatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public int QuantityUsed { get; set; }
        public decimal CostPerUnit { get; set; }
        public decimal TotalCost { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string ExpiryStatus { get; set; } = "good";
    }

    /// <summary>
    /// FIFO recommendation for inventory management (product-level)
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
    /// FIFO recommendation for individual batches
    /// </summary>
    public class BatchFifoRecommendationDto
    {
        public int BatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int DaysUntilExpiry { get; set; }
        public string Urgency { get; set; } = "Good"; // "Critical", "Warning", "Good", "Expired"
        public string UrgencyColor { get; set; } = "#22c55e"; // Green for good
        public string RecommendationText { get; set; } = string.Empty;
        public int Priority { get; set; } // 1=highest priority
    }

    /// <summary>
    /// Validation result for batch allocation
    /// </summary>
    public class BatchAllocationValidationDto
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    // ==================== FILTER DTOs ==================== //

    /// <summary>
    /// Filter for products with batch summary
    /// </summary>
    public class ProductBatchSummaryFilterDto
    {
        public int? CategoryId { get; set; }
        public bool? HasExpiredBatches { get; set; }
        public bool? HasCriticalBatches { get; set; }
        public bool? HasBatches { get; set; }
        public string? SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "name"; // "name", "totalBatches", "nearestExpiry"
        public string SortOrder { get; set; } = "asc"; // "asc", "desc"
    }

    // ==================== UTILITY DTOs ==================== //

    /// <summary>
    /// Batch number generation request
    /// </summary>
    public class GenerateBatchNumberRequest
    {
        [Required(ErrorMessage = "Product ID is required")]
        public int ProductId { get; set; }
        
        public DateTime? ProductionDate { get; set; }
    }

    /// <summary>
    /// Batch number generation response
    /// </summary>
    public class GenerateBatchNumberResponseDto
    {
        public string BatchNumber { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public string Example { get; set; } = string.Empty;
    }
}