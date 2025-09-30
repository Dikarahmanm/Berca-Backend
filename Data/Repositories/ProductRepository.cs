using Berca_Backend.Models;
using Berca_Backend.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Berca_Backend.Data.Repositories
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        public ProductRepository(AppDbContext context) : base(context) { }

        public async Task<(IEnumerable<ProductDto> Items, int TotalCount)> GetProductsPagedAsync(
            int page,
            int pageSize,
            string? search = null,
            int? categoryId = null,
            bool? isActive = null,
            List<int>? branchIds = null)
        {
            var query = _dbSet.AsNoTracking()
                .Include(p => p.Category)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Name.Contains(search) ||
                                       p.Barcode.Contains(search) ||
                                       (p.Category != null && p.Category.Name != null && p.Category.Name.Contains(search)));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (isActive.HasValue)
            {
                query = query.Where(p => p.IsActive == isActive.Value);
            }

            // Branch filtering would be implemented when BranchInventory is properly integrated

            var totalCount = await query.CountAsync();

            var products = await query
                .OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Barcode = p.Barcode,
                    Stock = p.Stock,
                    BuyPrice = p.BuyPrice,
                    SellPrice = p.SellPrice,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.Name : "Unknown Category",
                    CategoryColor = p.Category != null ? p.Category.Color : "#000000",
                    IsActive = p.IsActive,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    MinimumStock = p.MinimumStock,
                    Unit = p.Unit,
                    Description = p.Description,
                    ImageUrl = p.ImageUrl,
                    ExpiryDate = p.ExpiryDate
                })
                .ToListAsync();

            return (products, totalCount);
        }

        public async Task<IEnumerable<Product>> GetLowStockProductsAsync(int? branchId = null)
        {
            var query = _dbSet.AsNoTracking()
                .Include(p => p.Category)
                .Where(p => p.Stock <= p.MinimumStock && p.IsActive);

            // Branch filtering would be implemented when BranchInventory is properly integrated

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId)
        {
            return await _dbSet.AsNoTracking()
                .Include(p => p.Category)
                .Where(p => p.CategoryId == categoryId && p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<Product?> GetProductByBarcodeAsync(string barcode)
        {
            return await _dbSet.AsNoTracking()
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Barcode == barcode);
        }

        public async Task<bool> IsBarcodeUniqueAsync(string barcode, int? excludeProductId = null)
        {
            var query = _dbSet.AsNoTracking().Where(p => p.Barcode == barcode);

            if (excludeProductId.HasValue)
            {
                query = query.Where(p => p.Id != excludeProductId.Value);
            }

            return !await query.AnyAsync();
        }

        public async Task<IEnumerable<Product>> GetExpiringProductsAsync(DateTime beforeDate)
        {
            return await _dbSet.AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.ProductBatches)
                .Where(p => p.ExpiryDate.HasValue && p.ExpiryDate <= beforeDate)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalInventoryValueAsync(int? branchId = null)
        {
            var query = _dbSet.AsNoTracking().Where(p => p.IsActive);

            // Branch filtering would be implemented when BranchInventory is properly integrated

            return await query.SumAsync(p => p.Stock * p.BuyPrice);
        }
    }
}