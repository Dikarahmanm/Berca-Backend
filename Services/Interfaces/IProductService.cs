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