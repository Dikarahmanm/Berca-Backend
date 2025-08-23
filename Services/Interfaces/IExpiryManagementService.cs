using Berca_Backend.DTOs;

namespace Berca_Backend.Services.Interfaces
{
    /// <summary>
    /// Service interface for expiry management functionality
    /// Handles expiry tracking, FIFO logic, and batch management for products
    /// </summary>
    public interface IExpiryManagementService
    {
        // ==================== PRODUCT BATCH MANAGEMENT ==================== //

        /// <summary>
        /// Create a new product batch with expiry information
        /// </summary>
        Task<ProductBatchDto> CreateProductBatchAsync(CreateProductBatchDto request, int createdByUserId);

        /// <summary>
        /// Update existing product batch information
        /// </summary>
        Task<ProductBatchDto?> UpdateProductBatchAsync(int batchId, UpdateProductBatchDto request, int updatedByUserId);

        /// <summary>
        /// Delete a product batch
        /// </summary>
        Task<bool> DeleteProductBatchAsync(int batchId);

        /// <summary>
        /// Get all batches for a specific product
        /// </summary>
        Task<List<ProductBatchDto>> GetProductBatchesAsync(int productId);

        /// <summary>
        /// Get specific batch by ID
        /// </summary>
        Task<ProductBatchDto?> GetProductBatchByIdAsync(int batchId);

        // ==================== EXPIRY TRACKING ==================== //

        /// <summary>
        /// Get products expiring soon with filtering options
        /// </summary>
        Task<List<ExpiringProductDto>> GetExpiringProductsAsync(ExpiringProductsFilterDto filter);

        /// <summary>
        /// Get expired products with filtering options
        /// </summary>
        Task<List<ExpiredProductDto>> GetExpiredProductsAsync(ExpiredProductsFilterDto filter);

        /// <summary>
        /// Validate expiry requirements based on category settings
        /// </summary>
        Task<ExpiryValidationDto> ValidateExpiryRequirementsAsync(int productId, DateTime? expiryDate);

        /// <summary>
        /// Mark batches as expired based on current date
        /// </summary>
        Task<int> MarkBatchesAsExpiredAsync();

        /// <summary>
        /// Get products that require expiry tracking based on category
        /// </summary>
        Task<List<ExpiringProductDto>> GetProductsRequiringExpiryAsync();

        // ==================== FIFO LOGIC ==================== //

        /// <summary>
        /// Get FIFO recommendations for products with expiry dates
        /// </summary>
        Task<List<FifoRecommendationDto>> GetFifoRecommendationsAsync(int? categoryId = null, int? branchId = null);

        /// <summary>
        /// Get batch sale order based on FIFO logic for specific product
        /// </summary>
        Task<List<BatchRecommendationDto>> GetBatchSaleOrderAsync(int productId, int requestedQuantity);

        /// <summary>
        /// Process a sale using FIFO logic to reduce stock from appropriate batches
        /// </summary>
        Task<bool> ProcessFifoSaleAsync(int productId, int quantity, string referenceNumber, int processedByUserId);

        /// <summary>
        /// Get optimal batch allocation for inventory transfer
        /// </summary>
        Task<List<BatchRecommendationDto>> GetBatchAllocationForTransferAsync(int productId, int quantity, int sourceBranchId);

        // ==================== DISPOSAL MANAGEMENT ==================== //

        /// <summary>
        /// Dispose expired products and update disposal records
        /// </summary>
        Task<bool> DisposeExpiredProductsAsync(DisposeExpiredProductsDto request, int disposedByUserId);

        /// <summary>
        /// Get products eligible for disposal (expired and not yet disposed)
        /// </summary>
        Task<List<ExpiredProductDto>> GetDisposableProductsAsync(int? branchId = null);

        /// <summary>
        /// Undo disposal of products (in case of error)
        /// </summary>
        Task<bool> UndoDisposalAsync(List<int> batchIds, int undoneByUserId);

        // ==================== EXPIRY ANALYTICS ==================== //

        /// <summary>
        /// Get comprehensive expiry analytics for branch
        /// </summary>
        Task<ExpiryAnalyticsDto> GetExpiryAnalyticsAsync(int? branchId = null, DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// Get category-wise expiry statistics
        /// </summary>
        Task<List<CategoryExpiryStatsDto>> GetCategoryExpiryStatsAsync(int? branchId = null);

        /// <summary>
        /// Get expiry trends over time
        /// </summary>
        Task<List<ExpiryTrendDto>> GetExpiryTrendsAsync(DateTime startDate, DateTime endDate, int? branchId = null);

        /// <summary>
        /// Calculate wastage metrics
        /// </summary>
        Task<WastageMetricsDto> GetWastageMetricsAsync(int? branchId = null, DateTime? startDate = null, DateTime? endDate = null);

        // ==================== NOTIFICATION SUPPORT ==================== //

        /// <summary>
        /// Get products requiring expiry notifications
        /// </summary>
        Task<List<ExpiryNotificationDto>> GetProductsRequiringNotificationAsync(int? branchId = null);

        /// <summary>
        /// Create expiry notifications for products nearing expiry
        /// </summary>
        Task<int> CreateExpiryNotificationsAsync(int? branchId = null);

        // ==================== BACKGROUND TASKS ==================== //

        /// <summary>
        /// Daily expiry check - mark expired products and create notifications
        /// </summary>
        Task<ExpiryCheckResultDto> PerformDailyExpiryCheckAsync();

        /// <summary>
        /// Update expiry status for all batches
        /// </summary>
        Task<int> UpdateExpiryStatusesAsync();

        // ==================== ADVANCED ANALYTICS (NEW) ==================== //

        /// <summary>
        /// Get comprehensive expiry analytics with financial calculations
        /// </summary>
        Task<ComprehensiveExpiryAnalyticsDto> GetComprehensiveExpiryAnalyticsAsync(int? branchId = null);

        /// <summary>
        /// Get smart FIFO recommendations with advanced scoring
        /// </summary>
        Task<List<SmartFifoRecommendationDto>> GetSmartFifoRecommendationsAsync(int? branchId = null);
    }

    // ==================== ADDITIONAL DTOs FOR EXPIRY SERVICE ==================== //

    /// <summary>
    /// DTO for expiry trend analysis
    /// </summary>
    public class ExpiryTrendDto
    {
        public DateTime Date { get; set; }
        public int ExpiredProducts { get; set; }
        public int DisposedProducts { get; set; }
        public decimal ValueLost { get; set; }
        public int ProductsNearingExpiry { get; set; }
        public decimal ValueAtRisk { get; set; }
    }

    /// <summary>
    /// DTO for wastage metrics
    /// </summary>
    public class WastageMetricsDto
    {
        public decimal TotalValuePurchased { get; set; }
        public decimal TotalValueSold { get; set; }
        public decimal TotalValueExpired { get; set; }
        public decimal TotalValueDisposed { get; set; }
        public decimal WastagePercentage { get; set; }
        public decimal RecoveryPercentage { get; set; }
        public List<CategoryWastageDto> CategoryBreakdown { get; set; } = new();
    }

    /// <summary>
    /// DTO for category wastage breakdown
    /// </summary>
    public class CategoryWastageDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal ValuePurchased { get; set; }
        public decimal ValueExpired { get; set; }
        public decimal WastagePercentage { get; set; }
    }

    /// <summary>
    /// DTO for expiry notifications
    /// </summary>
    public class ExpiryNotificationDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int BatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public int DaysUntilExpiry { get; set; }
        public Models.ExpiryStatus ExpiryStatus { get; set; }
        public int CurrentStock { get; set; }
        public decimal ValueAtRisk { get; set; }
        public int? BranchId { get; set; }
        public string? BranchName { get; set; }
        public string NotificationPriority { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for daily expiry check results
    /// </summary>
    public class ExpiryCheckResultDto
    {
        public DateTime CheckDate { get; set; }
        public int NewlyExpiredBatches { get; set; }
        public int NotificationsCreated { get; set; }
        public int StatusesUpdated { get; set; }
        public decimal ValueAtRisk { get; set; }
        public decimal NewValueLost { get; set; }
        public List<ExpiryNotificationDto> CriticalItems { get; set; } = new();
    }
}