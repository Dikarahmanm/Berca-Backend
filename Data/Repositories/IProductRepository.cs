using Berca_Backend.Models;
using Berca_Backend.DTOs;
using System.Linq.Expressions;

namespace Berca_Backend.Data.Repositories
{
    public interface IProductRepository : IRepository<Product>
    {
        Task<(IEnumerable<ProductDto> Items, int TotalCount)> GetProductsPagedAsync(
            int page,
            int pageSize,
            string? search = null,
            int? categoryId = null,
            bool? isActive = null,
            List<int>? branchIds = null);

        Task<IEnumerable<Product>> GetLowStockProductsAsync(int? branchId = null);
        Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId);
        Task<Product?> GetProductByBarcodeAsync(string barcode);
        Task<bool> IsBarcodeUniqueAsync(string barcode, int? excludeProductId = null);
        Task<IEnumerable<Product>> GetExpiringProductsAsync(DateTime beforeDate);
        Task<decimal> GetTotalInventoryValueAsync(int? branchId = null);
    }
}