// Services/IProductService.cs - Sprint 2 Product Service Interface
using Berca_Backend.DTOs;
using Berca_Backend.Models;

namespace Berca_Backend.Services
{
    public interface IProductService
    {
        // CRUD Operations
        Task<ProductListResponse> GetProductsAsync(int page = 1, int pageSize = 20, string? search = null, int? categoryId = null, bool? isActive = null);
        Task<ProductDto?> GetProductByIdAsync(int id);
        Task<ProductDto?> GetProductByBarcodeAsync(string barcode);
        Task<ProductDto> CreateProductAsync(CreateProductRequest request, string createdBy);
        Task<ProductDto> UpdateProductAsync(int id, UpdateProductRequest request, string updatedBy);
        Task<bool> DeleteProductAsync(int id);

        // Stock Management
        Task<bool> UpdateStockAsync(int productId, int quantity, MutationType type, string notes, string? referenceNumber = null, decimal? unitCost = null, string? createdBy = null);
        Task<List<ProductDto>> GetLowStockProductsAsync(int threshold = 0);
        Task<List<ProductDto>> GetOutOfStockProductsAsync();

        // Validation
        Task<bool> IsBarcodeExistsAsync(string barcode, int? excludeId = null);
        Task<bool> IsProductExistsAsync(int id);

        // Inventory Reports
        Task<List<InventoryMutationDto>> GetInventoryHistoryAsync(int productId, DateTime? startDate = null, DateTime? endDate = null);
        Task<decimal> GetInventoryValueAsync();

        // ==================== EXPIRY & BATCH MANAGEMENT ==================== //

        // Product Batch Operations
        Task<ProductBatchDto> CreateProductBatchAsync(CreateProductBatchDto request, int createdByUserId);
        Task<ProductBatchDto> CreateProductBatchAsync(int productId, CreateProductBatchDto request, int createdByUserId, int branchId);
        Task<ProductBatchDto?> UpdateProductBatchAsync(int batchId, UpdateProductBatchDto request, int updatedByUserId);
        Task<bool> DeleteProductBatchAsync(int batchId);
        Task<List<ProductBatchDto>> GetProductBatchesAsync(int productId);
        Task<List<ProductBatchDto>> GetProductBatchesAsync(int productId, bool includeExpired = true, bool includeDisposed = false);
        Task<ProductBatchDto?> GetProductBatchByIdAsync(int batchId);
        Task<ProductBatchDto?> GetProductBatchAsync(int batchId);
        Task<bool> DisposeProductBatchAsync(int batchId, DisposeBatchDto request, int disposedByUserId);

        // Expiry Tracking
        Task<List<ExpiringProductDto>> GetExpiringProductsAsync(ExpiringProductsFilterDto filter);
        Task<List<ExpiredProductDto>> GetExpiredProductsAsync(ExpiredProductsFilterDto filter);
        Task<ExpiryValidationDto> ValidateExpiryRequirementsAsync(int productId, DateTime? expiryDate);
        Task<bool> MarkBatchesAsExpiredAsync();

        // FIFO Logic
        Task<List<FifoRecommendationDto>> GetFifoRecommendationsAsync(int? categoryId = null, int? branchId = null);
        Task<List<BatchRecommendationDto>> GetBatchSaleOrderAsync(int productId, int requestedQuantity);
        Task<bool> ProcessFifoSaleAsync(int productId, int quantity, string referenceNumber);

        // Disposal Management
        Task<bool> DisposeExpiredProductsAsync(DisposeExpiredProductsDto request, int disposedByUserId);
        Task<List<ExpiredProductDto>> GetDisposableProductsAsync(int? branchId = null);

        // Expiry Analytics
        Task<ExpiryAnalyticsDto> GetExpiryAnalyticsAsync(int? branchId = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<List<ProductDto>> GetProductsRequiringExpiryAsync();
        Task<bool> ProductRequiresExpiryAsync(int productId);
    }

    public class InventoryMutationDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int StockBefore { get; set; }
        public int StockAfter { get; set; }
        public string Notes { get; set; } = string.Empty;
        public string? ReferenceNumber { get; set; }
        public decimal? UnitCost { get; set; }
        public decimal? TotalCost { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
    }
}