// Services/ProductService.cs - Sprint 2 Product Service Implementation
using Berca_Backend.DTOs;
using Berca_Backend.Models;
using Berca_Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace Berca_Backend.Services
{
    public class ProductService : IProductService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ProductService> _logger;

        public ProductService(AppDbContext context, ILogger<ProductService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ProductListResponse> GetProductsAsync(int page = 1, int pageSize = 20, string? search = null, int? categoryId = null, bool? isActive = null)
        {
            try
            {
                var query = _context.Products
                    .Include(p => p.Category)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(p => p.Name.Contains(search) ||
                                           p.Barcode.Contains(search) ||
                                           p.Category.Name.Contains(search));
                }

                if (categoryId.HasValue)
                {
                    query = query.Where(p => p.CategoryId == categoryId.Value);
                }

                if (isActive.HasValue)
                {
                    query = query.Where(p => p.IsActive == isActive.Value);
                }

                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

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
                        CategoryName = p.Category.Name,
                        CategoryColor = p.Category.Color,
                        IsActive = p.IsActive,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt
                    })
                    .ToListAsync();

                return new ProductListResponse
                {
                    Products = products,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    CurrentPage = page,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products");
                throw;
            }
        }

        public async Task<ProductDto?> GetProductByIdAsync(int id)
        {
            try
            {
                return await _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.Id == id)
                    .Select(p => new ProductDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Barcode = p.Barcode,
                        Stock = p.Stock,
                        BuyPrice = p.BuyPrice,
                        SellPrice = p.SellPrice,
                        CategoryId = p.CategoryId,
                        CategoryName = p.Category.Name,
                        CategoryColor = p.Category.Color,
                        IsActive = p.IsActive,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt
                    })
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product by ID: {ProductId}", id);
                throw;
            }
        }

        public async Task<ProductDto?> GetProductByBarcodeAsync(string barcode)
        {
            try
            {
                return await _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.Barcode == barcode && p.IsActive)
                    .Select(p => new ProductDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Barcode = p.Barcode,
                        Stock = p.Stock,
                        BuyPrice = p.BuyPrice,
                        SellPrice = p.SellPrice,
                        CategoryId = p.CategoryId,
                        CategoryName = p.Category.Name,
                        CategoryColor = p.Category.Color,
                        IsActive = p.IsActive,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt
                    })
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product by barcode: {Barcode}", barcode);
                throw;
            }
        }

        public async Task<ProductDto> CreateProductAsync(CreateProductRequest request, string createdBy)
        {
            try
            {
                var product = new Product
                {
                    Name = request.Name,
                    Barcode = request.Barcode,
                    Stock = request.Stock,
                    BuyPrice = request.BuyPrice,
                    SellPrice = request.SellPrice,
                    CategoryId = request.CategoryId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = createdBy
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                // Create initial stock mutation if stock > 0
                if (product.Stock > 0)
                {
                    await UpdateStockAsync(product.Id, product.Stock, MutationType.StockIn,
                        "Initial stock", null, request.BuyPrice, createdBy);
                }

                return await GetProductByIdAsync(product.Id) ?? throw new Exception("Product created but not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product: {ProductName}", request.Name);
                throw;
            }
        }

        public async Task<ProductDto> UpdateProductAsync(int id, UpdateProductRequest request, string updatedBy)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                    throw new KeyNotFoundException($"Product with ID {id} not found");

                var oldStock = product.Stock;

                product.Name = request.Name;
                product.Barcode = request.Barcode;
                product.BuyPrice = request.BuyPrice;
                product.SellPrice = request.SellPrice;
                product.CategoryId = request.CategoryId;
                product.IsActive = request.IsActive;
                product.UpdatedAt = DateTime.UtcNow;
                product.UpdatedBy = updatedBy;

                // Handle stock changes
                if (request.Stock != oldStock)
                {
                    var stockDifference = request.Stock - oldStock;
                    var mutationType = stockDifference > 0 ? MutationType.StockIn : MutationType.StockOut;

                    product.Stock = request.Stock;

                    // Create stock mutation record
                    var mutation = new InventoryMutation
                    {
                        ProductId = id,
                        Type = mutationType,
                        Quantity = Math.Abs(stockDifference),
                        StockBefore = oldStock,
                        StockAfter = request.Stock,
                        Notes = "Stock adjustment via product update",
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = updatedBy
                    };

                    _context.InventoryMutations.Add(mutation);
                }

                await _context.SaveChangesAsync();

                return await GetProductByIdAsync(id) ?? throw new Exception("Product updated but not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product: {ProductId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null) return false;

                // Soft delete - mark as inactive
                product.IsActive = false;
                product.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product: {ProductId}", id);
                throw;
            }
        }

        public async Task<bool> UpdateStockAsync(int productId, int quantity, MutationType type, string notes,
            string? referenceNumber = null, decimal? unitCost = null, string? createdBy = null)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null) return false;

                var oldStock = product.Stock;
                var newStock = type == MutationType.StockIn ? oldStock + quantity : oldStock - quantity;

                if (newStock < 0)
                    throw new InvalidOperationException("Insufficient stock");

                product.Stock = newStock;
                product.UpdatedAt = DateTime.UtcNow;

                var mutation = new InventoryMutation
                {
                    ProductId = productId,
                    Type = type,
                    Quantity = quantity,
                    StockBefore = oldStock,
                    StockAfter = newStock,
                    Notes = notes,
                    ReferenceNumber = referenceNumber,
                    UnitCost = unitCost,
                    TotalCost = unitCost.HasValue ? unitCost.Value * quantity : null,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = createdBy
                };

                _context.InventoryMutations.Add(mutation);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating stock for product: {ProductId}", productId);
                throw;
            }
        }

        public async Task<List<ProductDto>> GetLowStockProductsAsync(int threshold = 0)
        {
            try
            {
                return await _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.IsActive && p.Stock <= threshold)
                    .Select(p => new ProductDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Barcode = p.Barcode,
                        Stock = p.Stock,
                        BuyPrice = p.BuyPrice,
                        SellPrice = p.SellPrice,
                        CategoryId = p.CategoryId,
                        CategoryName = p.Category.Name,
                        CategoryColor = p.Category.Color,
                        IsActive = p.IsActive,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving low stock products");
                throw;
            }
        }

        public async Task<List<ProductDto>> GetOutOfStockProductsAsync()
        {
            return await GetLowStockProductsAsync(0);
        }

        public async Task<bool> IsBarcodeExistsAsync(string barcode, int? excludeId = null)
        {
            try
            {
                var query = _context.Products.Where(p => p.Barcode == barcode);

                if (excludeId.HasValue)
                    query = query.Where(p => p.Id != excludeId.Value);

                return await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking barcode existence: {Barcode}", barcode);
                throw;
            }
        }

        public async Task<bool> IsProductExistsAsync(int id)
        {
            try
            {
                return await _context.Products.AnyAsync(p => p.Id == id && p.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking product existence: {ProductId}", id);
                throw;
            }
        }

        public async Task<List<InventoryMutationDto>> GetInventoryHistoryAsync(int productId,
            DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var query = _context.InventoryMutations
                    .Where(m => m.ProductId == productId);

                if (startDate.HasValue)
                    query = query.Where(m => m.CreatedAt >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(m => m.CreatedAt <= endDate.Value);

                return await query
                    .OrderByDescending(m => m.CreatedAt)
                    .Select(m => new InventoryMutationDto
                    {
                        Id = m.Id,
                        Type = m.Type.ToString(),
                        Quantity = m.Quantity,
                        StockBefore = m.StockBefore,
                        StockAfter = m.StockAfter,
                        Notes = m.Notes,
                        ReferenceNumber = m.ReferenceNumber,
                        UnitCost = m.UnitCost,
                        TotalCost = m.TotalCost,
                        CreatedAt = m.CreatedAt,
                        CreatedBy = m.CreatedBy
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving inventory history for product: {ProductId}", productId);
                throw;
            }
        }

        public async Task<decimal> GetInventoryValueAsync()
        {
            try
            {
                return await _context.Products
                    .Where(p => p.IsActive)
                    .SumAsync(p => p.Stock * p.BuyPrice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating inventory value");
                throw;
            }
        }
    }
}