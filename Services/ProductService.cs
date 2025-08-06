// Services/ProductService.cs - Fixed constructor and timezone usage
using Berca_Backend.DTOs;
using Berca_Backend.Models;
using Berca_Backend.Data;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Berca_Backend.Extensions;
using Berca_Backend.Services.Interfaces;

namespace Berca_Backend.Services
{
    public class ProductService : IProductService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ProductService> _logger;
        private readonly INotificationService? _notificationService;
        private readonly ITimezoneService _timezoneService;

        public ProductService(
            AppDbContext context, 
            ILogger<ProductService> logger, 
            INotificationService? notificationService = null,
            ITimezoneService? timezoneService = null)
        {
            _context = context;
            _logger = logger;
            _notificationService = notificationService;
            _timezoneService = timezoneService ?? throw new ArgumentNullException(nameof(timezoneService)); // ✅ FIXED
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
                        UpdatedAt = p.UpdatedAt,
                        MinimumStock = p.MinimumStock, // <-- PASTIKAN INI ADA!
                        // ... properti lain ...
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
                        UpdatedAt = p.UpdatedAt,
                        MinimumStock = p.MinimumStock // <-- PASTIKAN INI ADA!
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
                        UpdatedAt = p.UpdatedAt,
                        MinimumStock = p.MinimumStock // <-- PASTIKAN INI ADA!
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
                    MinimumStock = request.MinimumStock,
                    Unit = request.Unit,
                    IsActive = request.IsActive,
                    CreatedAt = _timezoneService.Now, // ✅ FIXED: Use Indonesia time directly
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

                // Check for barcode uniqueness (excluding current product)
                var existingBarcode = await _context.Products
                    .AnyAsync(p => p.Barcode == request.Barcode && p.Id != id);

                if (existingBarcode)
                    throw new ArgumentException($"Barcode '{request.Barcode}' already exists");

                var oldStock = product.Stock;

                // Update product properties
                product.Name = request.Name;
                product.Barcode = request.Barcode;
                product.BuyPrice = request.BuyPrice;
                product.SellPrice = request.SellPrice;
                product.CategoryId = request.CategoryId;
                product.IsActive = request.IsActive;
                product.UpdatedAt = _timezoneService.Now; // ✅ FIXED: Use Indonesia time directly
                product.UpdatedBy = updatedBy;
                product.MinimumStock = request.MinimumStock;

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
                        CreatedAt = _timezoneService.Now, // ✅ FIXED: Use Indonesia time directly
                        CreatedBy = updatedBy
                    };

                    _context.InventoryMutations.Add(mutation);
                }

                await _context.SaveChangesAsync();

                // Cek dan kirim notifikasi stok rendah jika perlu
                if (_notificationService != null && product.Stock <= product.MinimumStock && product.Stock > 0)
                {
                    try
                    {
                        await _notificationService.CreateLowStockNotificationAsync(product.Id, product.Stock);
                    }
                    catch (Exception notifyEx)
                    {
                        _logger.LogWarning(notifyEx, "Failed to create low stock notification for product {ProductId}", product.Id);
                    }
                }

                // Notifikasi stok habis
                if (_notificationService != null && product.Stock == 0)
                {
                    try
                    {
                        await _notificationService.CreateOutOfStockNotificationAsync(product.Id);
                    }
                    catch (Exception notifyEx)
                    {
                        _logger.LogWarning(notifyEx, "Failed to create out of stock notification for product {ProductId}", product.Id);
                    }
                }

                return await GetProductByIdAsync(id) ??
//# sourceMappingURL=Services/ProductService.cs.map
                    throw new Exception("Product updated but not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error updating product: {ProductId}", id);
                throw;
            }
        }

        // ✅ FIX: Interface expects DeleteProductAsync(int id) not DeleteProductAsync(int id, string deletedBy)
        public async Task<bool> DeleteProductAsync(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                    return false;

                // Soft delete - mark as deleted
                product.IsActive = false;
                product.UpdatedAt = _timezoneService.Now; // ✅ FIXED: Use Indonesia time directly

                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Product {ProductId} deleted successfully", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error deleting product {ProductId}", id);
                throw;
            }
        }

        // ✅ FIX: Interface expects this signature
        public async Task<bool> UpdateStockAsync(int productId, int quantity, MutationType type, string notes, 
            string? referenceNumber = null, decimal? unitCost = null, string? createdBy = null)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                    return false;

                if (!product.IsActive)
                    throw new InvalidOperationException("Cannot update stock for inactive product");

                var oldStock = product.Stock;
                int newStock = oldStock;

                if (quantity == 0)
                    throw new InvalidOperationException("Quantity must not be zero");

                // Stock calculation logic (existing)
                switch (type)
                {
                    case MutationType.StockIn:
                    case MutationType.Return:
                        newStock = oldStock + Math.Abs(quantity);
                        break;
                        
                    case MutationType.StockOut:
                    case MutationType.Sale:
                        var absQuantity = Math.Abs(quantity);
                        if (oldStock < absQuantity)
                            throw new InvalidOperationException($"Insufficient stock. Available: {oldStock}, Requested: {absQuantity}");
                        newStock = oldStock - absQuantity;
                        break;
                        
                    case MutationType.Adjustment:
                        if (quantity < 0 && oldStock < Math.Abs(quantity))
                            throw new InvalidOperationException($"Insufficient stock. Available: {oldStock}, Requested: {Math.Abs(quantity)}");
                        newStock = oldStock + quantity;
                        break;
                        
                    case MutationType.Damaged:
                    case MutationType.Expired:
                        var damageQuantity = Math.Abs(quantity);
                        if (oldStock < damageQuantity)
                            throw new InvalidOperationException($"Insufficient stock. Available: {oldStock}, Requested: {damageQuantity}");
                        newStock = oldStock - damageQuantity;
                        break;
                        
                    default:
                        throw new InvalidOperationException($"Unsupported mutation type: {type}");
                }

                if (newStock < 0)
                    throw new InvalidOperationException("Stock cannot be negative");

                product.Stock = newStock;
                product.UpdatedAt = _timezoneService.Now; // ✅ FIXED: Use Indonesia time directly
                product.UpdatedBy = createdBy;

                var mutation = new InventoryMutation
                {
                    ProductId = productId,
                    Type = type,
                    Quantity = Math.Abs(quantity),
                    StockBefore = oldStock,
                    StockAfter = newStock,
                    Notes = notes,
                    ReferenceNumber = referenceNumber,
                    UnitCost = unitCost,
                    TotalCost = unitCost.HasValue ? unitCost.Value * Math.Abs(quantity) : null,
                    CreatedAt = _timezoneService.Now, // ✅ FIXED: Use Indonesia time directly
                    CreatedBy = createdBy
                };

                _context.InventoryMutations.Add(mutation);
                await _context.SaveChangesAsync();

                // Trigger low stock notification if needed
                if (_notificationService != null && product.Stock <= product.MinimumStock)
                {
                    try
                    {
                        await _notificationService.CreateLowStockNotificationAsync(product.Id, product.Stock);
                    }
                    catch (Exception notifyEx)
                    {
                        _logger.LogWarning(notifyEx, "Failed to create low stock notification for product {ProductId}", product.Id);
                    }
                }

                // Stock adjustment notification
                if (_notificationService != null && type == MutationType.Adjustment)
                {
                    try
                    {
                        await _notificationService.CreateStockAdjustmentNotificationAsync(product.Id, quantity, notes);
                    }
                    catch (Exception notifyEx)
                    {
                        _logger.LogWarning(notifyEx, "Failed to create stock adjustment notification for product {ProductId}", product.Id);
                    }
                }

                _logger.LogInformation("✅ Stock updated: Product {ProductId} from {OldStock} to {NewStock} (Type: {Type})",
                    productId, oldStock, newStock, type);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error updating stock for product {ProductId}", productId);
                throw;
            }
        }

        // ✅ ADD: Overload for StockUpdateRequest to support controller
        public async Task<bool> UpdateStockAsync(int productId, StockUpdateRequest request, string updatedBy)
        {
            return await UpdateStockAsync(productId, request.Quantity, request.MutationType, 
                request.Notes, request.ReferenceNumber, request.UnitCost, updatedBy);
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
                        UpdatedAt = p.UpdatedAt,
                        MinimumStock = p.MinimumStock, // <-- PASTIKAN INI ADA!
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

        public async Task NotifySaleCompletedAsync(int saleId, string saleNumber, decimal totalAmount)
        {
            try
            {
                // 1. Get the sale by ID
                var sale = await _context.Sales.FindAsync(saleId);
                if (sale == null)
                    throw new KeyNotFoundException($"Sale with ID {saleId} not found");

                // 2. Update sale details if needed
                sale.SaleNumber = saleNumber;
                sale.Total = totalAmount;
                sale.UpdatedAt = DateTime.UtcNow;

                // 3. Notify stock updates for each product in the sale
                foreach (var item in sale.SaleItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        product.Stock -= item.Quantity;
                        product.UpdatedAt = DateTime.UtcNow;

                        // Optional: create inventory mutation records for the sale
                        var mutation = new InventoryMutation
                        {
                            ProductId = product.Id,
                            Type = MutationType.StockOut,
                            Quantity = item.Quantity,
                            StockBefore = product.Stock + item.Quantity,
                            StockAfter = product.Stock,
                            Notes = $"Sale completed: {saleId}",
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.InventoryMutations.Add(mutation);
                    }
                }

                await _context.SaveChangesAsync();

                // Send notification
                if (_notificationService != null)
                {
                    try
                    {
                        await _notificationService.CreateSaleCompletedNotificationAsync(sale.Id, sale.SaleNumber, sale.Total);
                    }
                    catch (Exception notifyEx)
                    {
                        _logger.LogWarning(notifyEx, "Failed to create sale completed notification for sale {SaleId}", sale.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying sale completion: {SaleId}", saleId);
                throw;
            }
        }
    }
}