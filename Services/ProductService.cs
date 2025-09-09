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
        private readonly IMultiBranchNotificationService? _notificationService;
        private readonly ITimezoneService _timezoneService;

        public ProductService(
            AppDbContext context, 
            ILogger<ProductService> logger, 
            IMultiBranchNotificationService? notificationService = null,
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
                        CategoryName = p.Category != null ? p.Category.Name : "Unknown Category",
                        CategoryColor = p.Category != null ? p.Category.Color : "#000000",
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
                        CategoryName = p.Category != null ? p.Category.Name : "Unknown Category",
                        CategoryColor = p.Category != null ? p.Category.Color : "#000000",
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
                        CategoryName = p.Category != null ? p.Category.Name : "Unknown Category",
                        CategoryColor = p.Category != null ? p.Category.Color : "#000000",
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
                        CategoryName = p.Category != null ? p.Category.Name : "Unknown Category",
                        CategoryColor = p.Category != null ? p.Category.Color : "#000000",
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

        // ==================== EXPIRY ANALYTICS METHODS ==================== //

        public async Task<ExpiryAnalyticsDto> GetExpiryAnalyticsAsync(int? branchId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var currentDate = _timezoneService.Today;
                var analysisStartDate = startDate ?? currentDate.AddDays(-30);
                var analysisEndDate = endDate ?? currentDate;

                var query = _context.ProductBatches
                    .Include(pb => pb.Product!)
                    .ThenInclude(p => p.Category!)
                    .AsQueryable();

                if (branchId.HasValue)
                {
                    query = query.Where(pb => pb.BranchId == branchId.Value);
                }

                var batches = await query.ToListAsync();

                // Calculate analytics
                var totalBatches = batches.Count;
                var expiringIn7Days = batches.Count(b => b.ExpiryDate.HasValue && 
                    b.ExpiryDate.Value >= currentDate && b.ExpiryDate.Value <= currentDate.AddDays(7) && !b.IsExpired && !b.IsDisposed);
                var expiringIn3Days = batches.Count(b => b.ExpiryDate.HasValue && 
                    b.ExpiryDate.Value >= currentDate && b.ExpiryDate.Value <= currentDate.AddDays(3) && !b.IsExpired && !b.IsDisposed);
                var expired = batches.Count(b => b.IsExpired && !b.IsDisposed);
                var disposed = batches.Count(b => b.IsDisposed);

                var valueAtRisk = batches
                    .Where(b => b.ExpiryDate.HasValue && b.ExpiryDate.Value >= currentDate && 
                               b.ExpiryDate.Value <= currentDate.AddDays(7) && !b.IsExpired && !b.IsDisposed)
                    .Sum(b => b.CurrentStock * b.CostPerUnit);

                var valueLost = batches
                    .Where(b => b.IsDisposed && b.DisposalDate >= analysisStartDate && b.DisposalDate <= analysisEndDate)
                    .Sum(b => b.CurrentStock * b.CostPerUnit);

                // Category breakdown - simplified
                var categoryBreakdown = batches
                    .Where(b => b.ExpiryDate.HasValue && b.ExpiryDate.Value >= currentDate && 
                               b.ExpiryDate.Value <= currentDate.AddDays(30))
                    .GroupBy(b => new { b.Product!.Category!.Id, b.Product.Category.Name, b.Product.Category.Color })
                    .Select(g => new CategoryExpiryStatsDto
                    {
                        CategoryId = g.Key.Id,
                        CategoryName = g.Key.Name,
                        CategoryColor = g.Key.Color,
                        TotalProducts = g.Count(),
                        ExpiringProducts = g.Count(b => !b.IsExpired && !b.IsDisposed),
                        ExpiredProducts = g.Count(b => b.IsExpired && !b.IsDisposed),
                        ValueAtRisk = g.Where(b => !b.IsExpired && !b.IsDisposed).Sum(b => b.CurrentStock * b.CostPerUnit),
                        ValueLost = g.Where(b => b.IsDisposed).Sum(b => b.CurrentStock * b.CostPerUnit)
                    })
                    .OrderByDescending(c => c.ValueAtRisk)
                    .ToList();

                return new ExpiryAnalyticsDto
                {
                    BranchId = branchId,
                    BranchName = null, // TODO: Get branch name
                    AnalysisDate = currentDate,
                    TotalProductsWithExpiry = totalBatches,
                    ExpiringIn7Days = expiringIn7Days,
                    ExpiringIn3Days = expiringIn3Days,
                    ExpiredProducts = expired,
                    DisposedProducts = disposed,
                    ValueAtRisk = valueAtRisk,
                    ValueLost = valueLost,
                    WastagePercentage = totalBatches > 0 ? (decimal)disposed / totalBatches * 100 : 0,
                    CategoryStats = categoryBreakdown
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expiry analytics");
                throw;
            }
        }

        public async Task<List<FifoRecommendationDto>> GetFifoRecommendationsAsync(int? categoryId = null, int? branchId = null)
        {
            try
            {
                var currentDate = _timezoneService.Today;

                var query = _context.ProductBatches
                    .Include(pb => pb.Product!)
                    .ThenInclude(p => p.Category!)
                    .Where(pb => !pb.IsDisposed && !pb.IsExpired && pb.CurrentStock > 0 && pb.ExpiryDate.HasValue)
                    .AsQueryable();

                if (categoryId.HasValue)
                {
                    query = query.Where(pb => pb.Product!.CategoryId == categoryId.Value);
                }

                if (branchId.HasValue)
                {
                    query = query.Where(pb => pb.BranchId == branchId.Value);
                }

                var batches = await query
                    .OrderBy(pb => pb.ExpiryDate)
                    .ToListAsync();

                var recommendations = new List<FifoRecommendationDto>();

                var productGroups = batches.GroupBy(b => b.ProductId);

                foreach (var group in productGroups)
                {
                    var productBatches = group.OrderBy(b => b.ExpiryDate).ToList();
                    var earliestBatch = productBatches.First();
                    var product = earliestBatch.Product!;

                    if (earliestBatch.ExpiryDate.HasValue)
                    {
                        var daysUntilExpiry = (earliestBatch.ExpiryDate.Value - currentDate).Days;
                        var urgencyLevel = daysUntilExpiry <= 3 ? "Critical" : 
                                         daysUntilExpiry <= 7 ? "High" : 
                                         daysUntilExpiry <= 14 ? "Medium" : "Low";

                        var recommendedAction = daysUntilExpiry <= 3 ? "Sell immediately or dispose" :
                                              daysUntilExpiry <= 7 ? "Prioritize for sales" :
                                              daysUntilExpiry <= 14 ? "Promote with discount" : "Normal sales";

                        var batchRecommendations = productBatches.Select((batch, index) => new BatchRecommendationDto
                        {
                            BatchId = batch.Id,
                            BatchNumber = batch.BatchNumber,
                            ExpiryDate = batch.ExpiryDate,
                            AvailableStock = batch.AvailableStock,
                            CostPerUnit = batch.CostPerUnit,
                            ExpiryStatus = batch.ExpiryStatus,
                            DaysUntilExpiry = batch.DaysUntilExpiry,
                            RecommendedSaleOrder = index + 1,
                            RecommendationReason = index == 0 ? "Earliest expiry - sell first" : $"Sell after batch {index}"
                        }).ToList();

                        recommendations.Add(new FifoRecommendationDto
                        {
                            ProductId = product.Id,
                            ProductName = product.Name,
                            ProductBarcode = product.Barcode,
                            BatchRecommendations = batchRecommendations,
                            TotalAvailableStock = productBatches.Sum(b => b.AvailableStock),
                            AverageCostPerUnit = productBatches.Average(b => b.CostPerUnit)
                        });
                    }
                }

                return recommendations
                    .OrderBy(r => r.BatchRecommendations.FirstOrDefault()?.DaysUntilExpiry ?? int.MaxValue)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting FIFO recommendations");
                throw;
            }
        }

        public async Task<List<ExpiringProductDto>> GetExpiringProductsAsync(int warningDays = 7, int? branchId = null)
        {
            try
            {
                var currentDate = _timezoneService.Today;
                var warningDate = currentDate.AddDays(warningDays);

                var query = _context.ProductBatches
                    .Include(pb => pb.Product!)
                    .ThenInclude(p => p.Category!)
                    .Where(pb => !pb.IsDisposed && !pb.IsExpired && 
                                pb.ExpiryDate.HasValue && 
                                pb.ExpiryDate.Value >= currentDate && 
                                pb.ExpiryDate.Value <= warningDate)
                    .AsQueryable();

                if (branchId.HasValue)
                {
                    query = query.Where(pb => pb.BranchId == branchId.Value);
                }

                var batches = await query
                    .OrderBy(pb => pb.ExpiryDate)
                    .ToListAsync();

                return batches.Select(b => new ExpiringProductDto
                {
                    ProductId = b.Product!.Id,
                    ProductName = b.Product.Name,
                    CategoryName = b.Product.Category!.Name,
                    BatchNumber = b.BatchNumber,
                    ExpiryDate = b.ExpiryDate!.Value,
                    DaysUntilExpiry = (b.ExpiryDate!.Value - currentDate).Days,
                    CurrentStock = b.CurrentStock,
                    CostPerUnit = b.CostPerUnit,
                    ValueAtRisk = b.CurrentStock * b.CostPerUnit,
                    UrgencyLevel = (b.ExpiryDate!.Value - currentDate).Days <= 3 ? ExpiryUrgency.Critical :
                                   (b.ExpiryDate!.Value - currentDate).Days <= 7 ? ExpiryUrgency.High : ExpiryUrgency.Medium,
                    BranchId = b.BranchId,
                    CreatedAt = b.CreatedAt
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expiring products");
                throw;
            }
        }

        public async Task<List<ExpiredProductDto>> GetExpiredProductsAsync(int? branchId = null)
        {
            try
            {
                var currentDate = _timezoneService.Today;

                var query = _context.ProductBatches
                    .Include(pb => pb.Product!)
                    .ThenInclude(p => p.Category!)
                    .Where(pb => !pb.IsDisposed && 
                                (pb.IsExpired || (pb.ExpiryDate.HasValue && pb.ExpiryDate.Value < currentDate)))
                    .AsQueryable();

                if (branchId.HasValue)
                {
                    query = query.Where(pb => pb.BranchId == branchId.Value);
                }

                var batches = await query
                    .OrderBy(pb => pb.ExpiryDate)
                    .ToListAsync();

                return batches.Select(b => new ExpiredProductDto
                {
                    ProductId = b.Product!.Id,
                    ProductName = b.Product.Name,
                    CategoryName = b.Product.Category!.Name,
                    BatchNumber = b.BatchNumber,
                    ExpiryDate = b.ExpiryDate!.Value,
                    DaysExpired = b.ExpiryDate.HasValue ? (currentDate - b.ExpiryDate.Value).Days : 0,
                    CurrentStock = b.CurrentStock,
                    CostPerUnit = b.CostPerUnit,
                    ValueLost = b.CurrentStock * b.CostPerUnit,
                    BranchId = b.BranchId,
                    ExpiredAt = b.ExpiryDate!.Value,
                    RequiresDisposal = !b.IsDisposed,
                    DisposalUrgency = (currentDate - (b.ExpiryDate ?? currentDate)).Days > 30 ? DisposalUrgency.Critical :
                                     (currentDate - (b.ExpiryDate ?? currentDate)).Days > 14 ? DisposalUrgency.High : DisposalUrgency.Medium
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expired products");
                throw;
            }
        }

        public async Task<bool> ProductRequiresExpiryAsync(int productId)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.Id == productId);

                return product?.Category.RequiresExpiryDate ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking expiry requirement for product {ProductId}", productId);
                throw;
            }
        }

        // ==================== IMPLEMENT MISSING INTERFACE METHODS ==================== //

        public async Task<List<ProductBatchDto>> GetProductBatchesAsync(int productId)
        {
            return await GetProductBatchesAsync(productId, true, false);
        }

        public async Task<List<ProductBatchDto>> GetProductBatchesAsync(int productId, bool includeExpired = true, bool includeDisposed = false)
        {
            try
            {
                var query = _context.ProductBatches
                    .Include(pb => pb.Product)
                    .Include(pb => pb.Branch)
                    .Where(pb => pb.ProductId == productId);

                if (!includeExpired)
                {
                    query = query.Where(pb => !pb.IsExpired);
                }

                if (!includeDisposed)
                {
                    query = query.Where(pb => !pb.IsDisposed);
                }

                var batches = await query.OrderBy(pb => pb.ExpiryDate).ToListAsync();

                return batches.Select(b => new ProductBatchDto
                {
                    Id = b.Id,
                    ProductId = b.ProductId,
                    ProductName = b.Product!.Name,
                    BatchNumber = b.BatchNumber,
                    ExpiryDate = b.ExpiryDate,
                    ProductionDate = b.ProductionDate,
                    InitialStock = b.InitialStock,
                    CurrentStock = b.CurrentStock,
                    CostPerUnit = b.CostPerUnit,
                    SupplierName = b.SupplierName,
                    PurchaseOrderNumber = b.PurchaseOrderNumber,
                    Notes = b.Notes,
                    IsBlocked = b.IsBlocked,
                    BlockReason = b.BlockReason,
                    IsExpired = b.IsExpired,
                    IsDisposed = b.IsDisposed,
                    DisposalMethod = b.DisposalMethod,
                    DisposalDate = b.DisposalDate,
                    BranchId = b.BranchId,
                    BranchName = b.Branch?.BranchName,
                    CreatedAt = b.CreatedAt
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product batches for product {ProductId}", productId);
                throw;
            }
        }

        public async Task<ProductBatchDto?> GetProductBatchByIdAsync(int batchId)
        {
            return await GetProductBatchAsync(batchId);
        }

        public async Task<ProductBatchDto?> GetProductBatchAsync(int batchId)
        {
            try
            {
                var batch = await _context.ProductBatches
                    .Include(pb => pb.Product)
                    .Include(pb => pb.Branch)
                    .FirstOrDefaultAsync(pb => pb.Id == batchId);

                if (batch == null)
                    return null;

                return new ProductBatchDto
                {
                    Id = batch.Id,
                    ProductId = batch.ProductId,
                    ProductName = batch.Product!.Name,
                    BatchNumber = batch.BatchNumber,
                    ExpiryDate = batch.ExpiryDate,
                    ProductionDate = batch.ProductionDate,
                    InitialStock = batch.InitialStock,
                    CurrentStock = batch.CurrentStock,
                    CostPerUnit = batch.CostPerUnit,
                    SupplierName = batch.SupplierName,
                    PurchaseOrderNumber = batch.PurchaseOrderNumber,
                    Notes = batch.Notes,
                    IsBlocked = batch.IsBlocked,
                    BlockReason = batch.BlockReason,
                    IsExpired = batch.IsExpired,
                    IsDisposed = batch.IsDisposed,
                    DisposalMethod = batch.DisposalMethod,
                    DisposalDate = batch.DisposalDate,
                    BranchId = batch.BranchId,
                    BranchName = batch.Branch?.BranchName,
                    CreatedAt = batch.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product batch {BatchId}", batchId);
                throw;
            }
        }

        // ==================== COMPLETE EXPIRY METHOD IMPLEMENTATIONS ==================== //

        public async Task<ProductBatchDto> CreateProductBatchAsync(CreateProductBatchDto request, int createdByUserId)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.Id == request.ProductId);

                if (product == null)
                    throw new ArgumentException("Product not found");

                // Validate expiry requirements
                if (product.Category.RequiresExpiryDate && !request.ExpiryDate.HasValue)
                    throw new ArgumentException("Expiry date is required for this product category");

                var batch = new ProductBatch
                {
                    ProductId = request.ProductId,
                    BatchNumber = request.BatchNumber,
                    ExpiryDate = request.ExpiryDate,
                    ProductionDate = request.ProductionDate,
                    CurrentStock = request.InitialStock,
                    InitialStock = request.InitialStock,
                    CostPerUnit = request.CostPerUnit,
                    SupplierName = request.SupplierName,
                    PurchaseOrderNumber = request.PurchaseOrderNumber,
                    Notes = request.Notes,
                    BranchId = request.BranchId,
                    CreatedByUserId = createdByUserId,
                    UpdatedByUserId = createdByUserId,
                    CreatedAt = _timezoneService.Now,
                    UpdatedAt = _timezoneService.Now
                };

                _context.ProductBatches.Add(batch);
                await _context.SaveChangesAsync();

                return await GetProductBatchAsync(batch.Id) ?? throw new Exception("Batch created but not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product batch for ProductId: {ProductId}", request.ProductId);
                throw;
            }
        }

        public async Task<ProductBatchDto> CreateProductBatchAsync(int productId, CreateProductBatchDto request, int createdByUserId, int branchId)
        {
            request.ProductId = productId;
            request.BranchId = branchId;
            return await CreateProductBatchAsync(request, createdByUserId);
        }

        public async Task<ProductBatchDto?> UpdateProductBatchAsync(int batchId, UpdateProductBatchDto request, int updatedByUserId)
        {
            try
            {
                var batch = await _context.ProductBatches.FindAsync(batchId);
                if (batch == null) return null;

                batch.BatchNumber = request.BatchNumber;
                batch.ExpiryDate = request.ExpiryDate;
                batch.ProductionDate = request.ProductionDate;
                batch.CurrentStock = request.CurrentStock;
                batch.CostPerUnit = request.CostPerUnit;
                batch.SupplierName = request.SupplierName;
                batch.PurchaseOrderNumber = request.PurchaseOrderNumber;
                batch.Notes = request.Notes;
                batch.IsBlocked = request.IsBlocked;
                batch.BlockReason = request.BlockReason;
                batch.UpdatedByUserId = updatedByUserId;
                batch.UpdatedAt = _timezoneService.Now;

                await _context.SaveChangesAsync();
                return await GetProductBatchAsync(batchId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product batch {BatchId}", batchId);
                throw;
            }
        }

        public async Task<bool> DeleteProductBatchAsync(int batchId)
        {
            try
            {
                var batch = await _context.ProductBatches.FindAsync(batchId);
                if (batch == null) return false;

                if (batch.CurrentStock > 0)
                    throw new InvalidOperationException("Cannot delete batch with remaining stock");

                _context.ProductBatches.Remove(batch);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product batch {BatchId}", batchId);
                throw;
            }
        }

        public async Task<List<ExpiringProductDto>> GetExpiringProductsAsync(ExpiringProductsFilterDto filter)
        {
            try
            {
                var currentDate = _timezoneService.Today;
                var query = _context.ProductBatches
                    .Include(b => b.Product!)
                        .ThenInclude(p => p.Category)
                    .Include(b => b.Branch)
                    .Where(b => b.ExpiryDate.HasValue && b.CurrentStock > 0 && !b.IsDisposed);

                // Apply filters
                if (filter.CategoryId.HasValue)
                    query = query.Where(b => b.Product != null && b.Product.CategoryId == filter.CategoryId);

                if (filter.BranchId.HasValue)
                    query = query.Where(b => b.BranchId == filter.BranchId);

                if (filter.DaysUntilExpiry.HasValue)
                {
                    var targetDate = currentDate.AddDays(filter.DaysUntilExpiry.Value);
                    query = query.Where(b => b.ExpiryDate!.Value.Date <= targetDate);
                }

                if (!filter.IncludeBlocked.GetValueOrDefault())
                    query = query.Where(b => !b.IsBlocked);

                if (!string.IsNullOrEmpty(filter.SearchTerm))
                {
                    var searchTerm = filter.SearchTerm.ToLower();
                    query = query.Where(b => (b.Product != null && b.Product.Name.ToLower().Contains(searchTerm)) ||
                                           (b.Product != null && b.Product.Barcode.ToLower().Contains(searchTerm)) ||
                                           b.BatchNumber.ToLower().Contains(searchTerm));
                }

                var batches = await query
                    .OrderBy(b => b.ExpiryDate)
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .ToListAsync();

                return batches.Select(batch => new ExpiringProductDto
                {
                    ProductId = batch.ProductId,
                    ProductName = batch.Product?.Name ?? "Unknown Product",
                    ProductBarcode = batch.Product?.Barcode ?? "",
                    CategoryName = batch.Product?.Category?.Name ?? "Unknown Category",
                    CategoryColor = batch.Product?.Category?.Color ?? "#000000",
                    BatchId = batch.Id,
                    BatchNumber = batch.BatchNumber,
                    ExpiryDate = batch.ExpiryDate ?? DateTime.MaxValue,
                    DaysUntilExpiry = batch.DaysUntilExpiry ?? 0,
                    ExpiryStatus = batch.ExpiryStatus,
                    CurrentStock = batch.CurrentStock,
                    AvailableStock = batch.AvailableStock,
                    ValueAtRisk = batch.CurrentStock * batch.CostPerUnit,
                    CostPerUnit = batch.CostPerUnit,
                    UrgencyLevel = GetExpiryUrgency(batch.DaysUntilExpiry ?? 0),
                    CreatedAt = batch.CreatedAt,
                    BranchId = batch.BranchId,
                    BranchName = batch.Branch?.BranchName ?? "All Branches",
                    SupplierName = batch.SupplierName,
                    IsBlocked = batch.IsBlocked
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expiring products with filter");
                throw;
            }
        }

        public async Task<List<ExpiredProductDto>> GetExpiredProductsAsync(ExpiredProductsFilterDto filter)
        {
            try
            {
                var today = _timezoneService.Today;
                var query = _context.ProductBatches
                    .Include(b => b.Product!)
                        .ThenInclude(p => p.Category)
                    .Include(b => b.Branch)
                    .Where(b => b.ExpiryDate.HasValue && b.ExpiryDate.Value.Date < today);

                // Apply filters
                if (filter.CategoryId.HasValue)
                    query = query.Where(b => b.Product != null && b.Product.CategoryId == filter.CategoryId);

                if (filter.BranchId.HasValue)
                    query = query.Where(b => b.BranchId == filter.BranchId);

                if (filter.IsDisposed.HasValue)
                    query = query.Where(b => b.IsDisposed == filter.IsDisposed);

                if (filter.ExpiredAfter.HasValue)
                    query = query.Where(b => b.ExpiryDate >= filter.ExpiredAfter);

                if (filter.ExpiredBefore.HasValue)
                    query = query.Where(b => b.ExpiryDate <= filter.ExpiredBefore);

                if (!string.IsNullOrEmpty(filter.SearchTerm))
                {
                    var searchTerm = filter.SearchTerm.ToLower();
                    query = query.Where(b => (b.Product != null && b.Product.Name.ToLower().Contains(searchTerm)) ||
                                           (b.Product != null && b.Product.Barcode.ToLower().Contains(searchTerm)) ||
                                           b.BatchNumber.ToLower().Contains(searchTerm));
                }

                var batches = await query
                    .OrderByDescending(b => b.ExpiryDate)
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .ToListAsync();

                return batches.Select(batch => new ExpiredProductDto
                {
                    ProductId = batch.ProductId,
                    ProductName = batch.Product?.Name ?? "Unknown Product",
                    ProductBarcode = batch.Product?.Barcode ?? "",
                    CategoryName = batch.Product?.Category?.Name ?? "Unknown Category",
                    BatchId = batch.Id,
                    BatchNumber = batch.BatchNumber,
                    ExpiryDate = batch.ExpiryDate ?? DateTime.MaxValue,
                    DaysExpired = Math.Abs(batch.DaysUntilExpiry ?? 0),
                    CurrentStock = batch.CurrentStock,
                    ValueLost = batch.CurrentStock * batch.CostPerUnit,
                    CostPerUnit = batch.CostPerUnit,
                    ExpiredAt = batch.ExpiryDate ?? DateTime.MaxValue,
                    RequiresDisposal = !batch.IsDisposed,
                    DisposalUrgency = GetDisposalUrgency(Math.Abs(batch.DaysUntilExpiry ?? 0)),
                    BranchId = batch.BranchId,
                    BranchName = batch.Branch?.BranchName ?? "All Branches",
                    IsDisposed = batch.IsDisposed,
                    DisposalDate = batch.DisposalDate,
                    DisposalMethod = batch.DisposalMethod,
                    SupplierName = batch.SupplierName
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expired products with filter");
                throw;
            }
        }

        public async Task<ExpiryValidationDto> ValidateExpiryRequirementsAsync(int productId, DateTime? expiryDate)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.Id == productId);

                var result = new ExpiryValidationDto
                {
                    ProductId = productId,
                    CategoryId = product?.CategoryId ?? 0,
                    CategoryRequiresExpiry = product?.Category?.RequiresExpiryDate ?? false,
                    ProvidedExpiryDate = expiryDate,
                    ValidationErrors = new List<string>()
                };

                if (product == null)
                {
                    result.ValidationErrors.Add("Product not found");
                    result.IsValid = false;
                    return result;
                }

                result.CategoryId = product.CategoryId;
                result.CategoryRequiresExpiry = product.Category?.RequiresExpiryDate ?? false;

                if (product.Category?.RequiresExpiryDate == true)
                {
                    if (!expiryDate.HasValue)
                    {
                        result.ValidationErrors.Add($"Expiry date is required for category '{product.Category?.Name ?? "Unknown Category"}'");
                    }
                    else if (expiryDate.Value.Date <= _timezoneService.Today)
                    {
                        result.ValidationErrors.Add("Expiry date must be in the future");
                    }
                }

                result.IsValid = !result.ValidationErrors.Any();
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating expiry requirements for product {ProductId}", productId);
                throw;
            }
        }

        public async Task<bool> MarkBatchesAsExpiredAsync()
        {
            try
            {
                var today = _timezoneService.Today;
                var expiredBatches = await _context.ProductBatches
                    .Where(b => b.ExpiryDate.HasValue && 
                               b.ExpiryDate.Value.Date < today && 
                               !b.IsExpired)
                    .ToListAsync();

                foreach (var batch in expiredBatches)
                {
                    batch.IsExpired = true;
                    batch.UpdatedAt = _timezoneService.Now;
                }

                await _context.SaveChangesAsync();
                return expiredBatches.Count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking batches as expired");
                throw;
            }
        }

        public async Task<List<BatchRecommendationDto>> GetBatchSaleOrderAsync(int productId, int requestedQuantity)
        {
            try
            {
                var batches = await _context.ProductBatches
                    .Where(b => b.ProductId == productId && 
                               b.CurrentStock > 0 && 
                               !b.IsBlocked && 
                               !b.IsExpired)
                    .OrderBy(b => b.ExpiryDate)
                    .ThenBy(b => b.CreatedAt)
                    .ToListAsync();

                return GetBatchRecommendations(batches, requestedQuantity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting batch sale order for product {ProductId}", productId);
                throw;
            }
        }

        public async Task<bool> ProcessFifoSaleAsync(int productId, int quantity, string referenceNumber)
        {
            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                
                var batches = await _context.ProductBatches
                    .Where(b => b.ProductId == productId && 
                               b.CurrentStock > 0 && 
                               !b.IsBlocked && 
                               !b.IsExpired)
                    .OrderBy(b => b.ExpiryDate)
                    .ThenBy(b => b.CreatedAt)
                    .ToListAsync();

                var totalAvailable = batches.Sum(b => b.CurrentStock);
                if (totalAvailable < quantity)
                    throw new InvalidOperationException("Insufficient stock available");

                var remainingQuantity = quantity;
                foreach (var batch in batches)
                {
                    if (remainingQuantity <= 0) break;

                    var quantityFromThisBatch = Math.Min(remainingQuantity, batch.CurrentStock);
                    batch.CurrentStock -= quantityFromThisBatch;
                    batch.UpdatedAt = _timezoneService.Now;

                    remainingQuantity -= quantityFromThisBatch;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing FIFO sale for ProductId: {ProductId}", productId);
                throw;
            }
        }

        public async Task<bool> DisposeExpiredProductsAsync(DisposeExpiredProductsDto request, int disposedByUserId)
        {
            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                
                var batches = await _context.ProductBatches
                    .Where(b => request.BatchIds.Contains(b.Id))
                    .ToListAsync();

                foreach (var batch in batches)
                {
                    batch.IsDisposed = true;
                    batch.DisposalDate = _timezoneService.Now;
                    batch.DisposedByUserId = disposedByUserId;
                    batch.DisposalMethod = request.DisposalMethod;
                    batch.Notes = string.IsNullOrEmpty(batch.Notes) 
                        ? request.Notes 
                        : $"{batch.Notes}; {request.Notes}";
                    batch.UpdatedAt = _timezoneService.Now;
                    batch.UpdatedByUserId = disposedByUserId;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing expired products");
                throw;
            }
        }

        public async Task<bool> DisposeProductBatchAsync(int batchId, DisposeBatchDto request, int disposedByUserId)
        {
            try
            {
                var batch = await _context.ProductBatches.FindAsync(batchId);
                if (batch == null) return false;

                batch.IsDisposed = true;
                batch.DisposalDate = _timezoneService.Now;
                batch.DisposedByUserId = disposedByUserId;
                batch.DisposalMethod = request.DisposalMethod;
                batch.BlockReason = request.DisposalReason;
                batch.Notes = string.IsNullOrEmpty(batch.Notes) 
                    ? request.Notes 
                    : $"{batch.Notes}; {request.Notes}";
                batch.UpdatedAt = _timezoneService.Now;
                batch.UpdatedByUserId = disposedByUserId;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing product batch {BatchId}", batchId);
                throw;
            }
        }

        public async Task<List<ExpiredProductDto>> GetDisposableProductsAsync(int? branchId = null)
        {
            var filter = new ExpiredProductsFilterDto
            {
                BranchId = branchId,
                IsDisposed = false,
                PageSize = 1000
            };
            return await GetExpiredProductsAsync(filter);
        }

        public async Task<List<ProductDto>> GetProductsRequiringExpiryAsync()
        {
            try
            {
                var productsWithoutBatches = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.ProductBatches)
                    .Where(p => p.Category.RequiresExpiryDate && !p.ProductBatches.Any())
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
                        MinimumStock = p.MinimumStock
                    })
                    .ToListAsync();

                return productsWithoutBatches;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products requiring expiry");
                throw;
            }
        }

        // ==================== PRIVATE HELPER METHODS ==================== //

        private List<BatchRecommendationDto> GetBatchRecommendations(List<ProductBatch> batches, int? requestedQuantity = null)
        {
            var recommendations = new List<BatchRecommendationDto>();
            var orderedBatches = batches
                .OrderBy(b => b.ExpiryDate)
                .ThenBy(b => b.CreatedAt)
                .ToList();

            var remainingQuantity = requestedQuantity ?? int.MaxValue;
            var orderIndex = 1;

            foreach (var batch in orderedBatches)
            {
                if (remainingQuantity <= 0 && requestedQuantity.HasValue) break;

                var recommendation = new BatchRecommendationDto
                {
                    BatchId = batch.Id,
                    BatchNumber = batch.BatchNumber,
                    ExpiryDate = batch.ExpiryDate,
                    AvailableStock = batch.AvailableStock,
                    CostPerUnit = batch.CostPerUnit,
                    ExpiryStatus = batch.ExpiryStatus,
                    DaysUntilExpiry = batch.DaysUntilExpiry,
                    RecommendedSaleOrder = orderIndex++,
                    RecommendationReason = GetRecommendationReason(batch)
                };

                recommendations.Add(recommendation);

                if (requestedQuantity.HasValue)
                    remainingQuantity -= batch.AvailableStock;
            }

            return recommendations;
        }

        private string GetRecommendationReason(ProductBatch batch)
        {
            return batch.ExpiryStatus switch
            {
                ExpiryStatus.Critical => "Critical - Expires very soon!",
                ExpiryStatus.Warning => "Warning - Expires within a week",
                ExpiryStatus.Normal => "Normal - FIFO order",
                ExpiryStatus.Good => "Good condition - FIFO order",
                ExpiryStatus.Expired => "EXPIRED - Dispose immediately",
                _ => "No expiry date"
            };
        }

        private ExpiryUrgency GetExpiryUrgency(int daysUntilExpiry)
        {
            return daysUntilExpiry switch
            {
                <= 1 => ExpiryUrgency.Critical,
                <= 3 => ExpiryUrgency.High,
                <= 7 => ExpiryUrgency.Medium,
                _ => ExpiryUrgency.Low
            };
        }

        private DisposalUrgency GetDisposalUrgency(int daysExpired)
        {
            return daysExpired switch
            {
                >= 30 => DisposalUrgency.Critical,
                >= 14 => DisposalUrgency.High,
                >= 7 => DisposalUrgency.Medium,
                _ => DisposalUrgency.Low
            };
        }

        // ==================== ENHANCED BATCH MANAGEMENT IMPLEMENTATIONS ==================== //

        /// <summary>
        /// Check if product exists by barcode (for frontend registration flow)
        /// </summary>
        public async Task<bool> ProductExistsByBarcodeAsync(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
                return false;

            return await _context.Products
                .AnyAsync(p => p.Barcode == barcode.Trim() && p.IsActive);
        }

        /// <summary>
        /// Get products with comprehensive batch summary for enhanced inventory display
        /// ✅ FIXED: Avoids EF LINQ translation errors by computing expiry status in memory
        /// </summary>
        public async Task<List<ProductWithBatchSummaryDto>> GetProductsWithBatchSummaryAsync(ProductBatchSummaryFilterDto filter)
        {
            try
            {
                _logger.LogInformation("Getting products with batch summary. Filter: {@Filter}", filter);

                // Validate filter
                if (filter == null)
                    filter = new ProductBatchSummaryFilterDto();

                // Step 1: Get basic products first (simple query with only database columns)
                var query = _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.IsActive);

                // Apply basic filters
                if (filter.CategoryId.HasValue && filter.CategoryId > 0)
                    query = query.Where(p => p.CategoryId == filter.CategoryId);

                if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                {
                    var searchTerm = filter.SearchTerm.ToLower().Trim();
                    query = query.Where(p => p.Name.ToLower().Contains(searchTerm) ||
                                           p.Barcode.ToLower().Contains(searchTerm) ||
                                           (p.Description != null && p.Description.ToLower().Contains(searchTerm)));
                }

                // Apply pagination with safety checks
                var page = Math.Max(1, filter.Page);
                var pageSize = Math.Min(Math.Max(1, filter.PageSize), 100);

                var products = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                _logger.LogInformation("Loaded {Count} products from database", products.Count);

                if (!products.Any())
                {
                    return new List<ProductWithBatchSummaryDto>();
                }

                // Step 2: Get all batches for these products (separate simple query)
                var productIds = products.Select(p => p.Id).ToList();
                
                // ✅ FIXED: Simple query using only database columns
                var batches = await _context.ProductBatches
                    .Where(b => productIds.Contains(b.ProductId) && !b.IsDisposed)
                    .ToListAsync(); // Load to memory first to avoid EF translation issues

                _logger.LogInformation("Loaded {Count} batches for products", batches.Count);

                // Step 3: Process in memory (no EF translation issues)
                var result = new List<ProductWithBatchSummaryDto>();
                var now = _timezoneService.Now;
                
                foreach (var product in products)
                {
                    try
                    {
                        var productBatches = batches.Where(b => b.ProductId == product.Id).ToList();
                        
                        // ✅ FIXED: Compute expiry status in memory (not in SQL)
                        var expiredCount = productBatches.Count(b => 
                            b.IsExpired || (b.ExpiryDate.HasValue && b.ExpiryDate.Value < now));
                            
                        var criticalCount = productBatches.Count(b => 
                            !b.IsExpired && b.ExpiryDate.HasValue && 
                            (b.ExpiryDate.Value - now).TotalDays <= 3 &&
                            (b.ExpiryDate.Value - now).TotalDays >= 0);
                            
                        var warningCount = productBatches.Count(b => 
                            !b.IsExpired && b.ExpiryDate.HasValue && 
                            (b.ExpiryDate.Value - now).TotalDays <= 7 &&
                            (b.ExpiryDate.Value - now).TotalDays > 3);

                        var goodCount = productBatches.Count(b => 
                            !b.IsExpired && (!b.ExpiryDate.HasValue || 
                            (b.ExpiryDate.Value - now).TotalDays > 7));

                        // Apply expiry filters in memory if specified
                        var shouldInclude = true;
                        if (filter.HasExpiredBatches.HasValue)
                        {
                            shouldInclude = filter.HasExpiredBatches.Value ? expiredCount > 0 : expiredCount == 0;
                        }
                        if (shouldInclude && filter.HasCriticalBatches.HasValue)
                        {
                            shouldInclude = filter.HasCriticalBatches.Value ? criticalCount > 0 : criticalCount == 0;
                        }
                        if (shouldInclude && filter.HasBatches.HasValue)
                        {
                            shouldInclude = filter.HasBatches.Value ? productBatches.Any() : !productBatches.Any();
                        }

                        if (!shouldInclude) continue;

                        // Create summary DTO with in-memory calculations
                        var productSummary = await MapToProductWithBatchSummaryDtoSafe(product, productBatches, now);
                        result.Add(productSummary);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing product {ProductId} in batch summary", product.Id);
                        // Continue with next product instead of failing completely
                        continue;
                    }
                }

                _logger.LogInformation("Generated batch summary for {Count} products", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetProductsWithBatchSummaryAsync with filter {@Filter}", filter);
                throw;
            }
        }

        /// <summary>
        /// Flexible stock addition with batch options
        /// </summary>
        public async Task<AddStockResponseDto> AddStockToBatchAsync(int productId, AddStockToBatchRequest request, int userId, int branchId)
        {
            // Validate product exists
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId && p.IsActive);
            if (product == null)
                throw new ArgumentException("Product not found");

            if (request.BatchId.HasValue)
            {
                // Add to existing batch
                return await AddStockToExistingBatchAsync(request.BatchId.Value, request.Quantity, request.CostPerUnit, userId);
            }
            else if (!string.IsNullOrWhiteSpace(request.BatchNumber) || !string.IsNullOrWhiteSpace(request.ExpiryDate))
            {
                // Create new batch
                return await CreateBatchAndAddStockAsync(productId, request, userId, branchId);
            }
            else
            {
                // Simple stock addition without batch tracking
                return await AddStockWithoutBatchAsync(productId, request.Quantity, request.CostPerUnit, userId);
            }
        }

        /// <summary>
        /// Add stock to existing batch
        /// </summary>
        public async Task<AddStockResponseDto> AddStockToExistingBatchAsync(int batchId, int quantity, decimal costPerUnit, int updatedByUserId)
        {
            var batch = await _context.ProductBatches
                .Include(b => b.Product)
                .FirstOrDefaultAsync(b => b.Id == batchId && !b.IsDisposed);

            if (batch == null)
                throw new ArgumentException("Batch not found or already disposed");

            if (batch.IsBlocked)
                throw new InvalidOperationException("Cannot add stock to blocked batch");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Calculate weighted average cost
                var totalCurrentValue = batch.CurrentStock * batch.CostPerUnit;
                var totalNewValue = quantity * costPerUnit;
                var newTotalStock = batch.CurrentStock + quantity;
                var weightedAverageCost = (totalCurrentValue + totalNewValue) / newTotalStock;

                // Update batch
                batch.CurrentStock += quantity;
                batch.CostPerUnit = weightedAverageCost;
                batch.UpdatedAt = _timezoneService.Now;
                batch.UpdatedByUserId = updatedByUserId;

                // Update product total stock
                batch.Product!.Stock += quantity;
                batch.Product.UpdatedAt = _timezoneService.Now;
                batch.Product.UpdatedBy = updatedByUserId.ToString();

                // Log inventory mutation
                var mutation = new InventoryMutation
                {
                    ProductId = batch.ProductId,
                    Type = MutationType.StockIn,
                    Quantity = quantity,
                    StockBefore = batch.Product.Stock - quantity,
                    StockAfter = batch.Product.Stock,
                    Notes = $"Added to batch {batch.BatchNumber}",
                    ReferenceNumber = $"BATCH-{batch.BatchNumber}",
                    UnitCost = costPerUnit,
                    TotalCost = quantity * costPerUnit,
                    CreatedAt = _timezoneService.Now,
                    CreatedBy = updatedByUserId.ToString()
                };

                _context.InventoryMutations.Add(mutation);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new AddStockResponseDto
                {
                    Success = true,
                    ProductId = batch.ProductId,
                    ProductName = batch.Product.Name,
                    BatchId = batch.Id,
                    BatchNumber = batch.BatchNumber,
                    AddedQuantity = quantity,
                    NewBatchStock = batch.CurrentStock,
                    NewProductTotalStock = batch.Product.Stock,
                    WeightedAverageCost = weightedAverageCost,
                    Message = $"Successfully added {quantity} units to batch {batch.BatchNumber}"
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Create new batch and add stock
        /// </summary>
        public async Task<AddStockResponseDto> CreateBatchAndAddStockAsync(int productId, AddStockToBatchRequest request, int createdByUserId, int branchId)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == productId && p.IsActive);

            if (product == null)
                throw new ArgumentException("Product not found");

            // Validate expiry requirements
            DateTime? expiryDate = null;
            if (!string.IsNullOrWhiteSpace(request.ExpiryDate))
            {
                if (!DateTime.TryParse(request.ExpiryDate, out var parsedDate))
                    throw new ArgumentException("Invalid expiry date format");
                expiryDate = parsedDate;
            }

            if (product.Category?.RequiresExpiryDate == true && !expiryDate.HasValue)
                throw new ArgumentException("Expiry date is required for this product category");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Generate batch number if not provided
                var batchNumber = !string.IsNullOrWhiteSpace(request.BatchNumber) ? 
                    request.BatchNumber : 
                    await GenerateBatchNumberInternalAsync(productId, DateTime.TryParse(request.ProductionDate, out var prodDate) ? prodDate : null);

                // Create new batch
                var batch = new ProductBatch
                {
                    ProductId = productId,
                    BatchNumber = batchNumber,
                    ExpiryDate = expiryDate,
                    ProductionDate = DateTime.TryParse(request.ProductionDate, out var productionDate) ? productionDate : null,
                    InitialStock = request.Quantity,
                    CurrentStock = request.Quantity,
                    CostPerUnit = request.CostPerUnit,
                    SupplierName = request.SupplierName,
                    PurchaseOrderNumber = request.PurchaseOrderNumber,
                    Notes = request.Notes,
                    BranchId = branchId,
                    CreatedAt = _timezoneService.Now,
                    CreatedByUserId = createdByUserId,
                    UpdatedAt = _timezoneService.Now,
                    UpdatedByUserId = createdByUserId
                };

                _context.ProductBatches.Add(batch);

                // Update product total stock
                product.Stock += request.Quantity;
                product.UpdatedAt = _timezoneService.Now;
                product.UpdatedBy = createdByUserId.ToString();

                // Log inventory mutation
                var mutation = new InventoryMutation
                {
                    ProductId = productId,
                    Type = MutationType.StockIn,
                    Quantity = request.Quantity,
                    StockBefore = product.Stock - request.Quantity,
                    StockAfter = product.Stock,
                    Notes = $"New batch created: {batchNumber}",
                    ReferenceNumber = $"BATCH-{batchNumber}",
                    UnitCost = request.CostPerUnit,
                    TotalCost = request.Quantity * request.CostPerUnit,
                    CreatedAt = _timezoneService.Now,
                    CreatedBy = createdByUserId.ToString()
                };

                _context.InventoryMutations.Add(mutation);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new AddStockResponseDto
                {
                    Success = true,
                    ProductId = productId,
                    ProductName = product.Name,
                    BatchId = batch.Id,
                    BatchNumber = batch.BatchNumber,
                    AddedQuantity = request.Quantity,
                    NewBatchStock = batch.CurrentStock,
                    NewProductTotalStock = product.Stock,
                    WeightedAverageCost = request.CostPerUnit,
                    Message = $"Successfully created batch {batch.BatchNumber} with {request.Quantity} units"
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Add stock without batch tracking (for products that don't require batch management)
        /// </summary>
        public async Task<AddStockResponseDto> AddStockWithoutBatchAsync(int productId, int quantity, decimal costPerUnit, int updatedByUserId)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId && p.IsActive);
            if (product == null)
                throw new ArgumentException("Product not found");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var oldStock = product.Stock;
                
                // Update product stock
                product.Stock += quantity;
                product.UpdatedAt = _timezoneService.Now;
                product.UpdatedBy = updatedByUserId.ToString();

                // Log inventory mutation
                var mutation = new InventoryMutation
                {
                    ProductId = productId,
                    Type = MutationType.StockIn,
                    Quantity = quantity,
                    StockBefore = oldStock,
                    StockAfter = product.Stock,
                    Notes = "Stock addition without batch tracking",
                    UnitCost = costPerUnit,
                    TotalCost = quantity * costPerUnit,
                    CreatedAt = _timezoneService.Now,
                    CreatedBy = updatedByUserId.ToString()
                };

                _context.InventoryMutations.Add(mutation);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new AddStockResponseDto
                {
                    Success = true,
                    ProductId = productId,
                    ProductName = product.Name,
                    BatchId = null,
                    BatchNumber = null,
                    AddedQuantity = quantity,
                    NewBatchStock = 0,
                    NewProductTotalStock = product.Stock,
                    WeightedAverageCost = costPerUnit,
                    Message = $"Successfully added {quantity} units to product stock"
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Get enhanced FIFO recommendations for specific product
        /// </summary>
        public async Task<List<BatchFifoRecommendationDto>> GetProductFifoRecommendationsAsync(int productId, int? requestedQuantity = null)
        {
            var batches = await _context.ProductBatches
                .Where(b => b.ProductId == productId && 
                           b.CurrentStock > 0 && 
                           !b.IsDisposed && 
                           !b.IsBlocked)
                .OrderBy(b => b.ExpiryDate ?? DateTime.MaxValue)
                .ThenBy(b => b.CreatedAt)
                .ToListAsync();

            var recommendations = new List<BatchFifoRecommendationDto>();
            var remainingQuantity = requestedQuantity ?? int.MaxValue;
            var priority = 1;

            foreach (var batch in batches)
            {
                if (remainingQuantity <= 0) break;

                var daysUntilExpiry = batch.DaysUntilExpiry ?? int.MaxValue;
                var urgency = GetFifoUrgency(daysUntilExpiry);
                var urgencyColor = GetUrgencyColor(urgency);

                var recommendation = new BatchFifoRecommendationDto
                {
                    BatchId = batch.Id,
                    BatchNumber = batch.BatchNumber,
                    CurrentStock = batch.CurrentStock,
                    ExpiryDate = batch.ExpiryDate,
                    DaysUntilExpiry = daysUntilExpiry,
                    Urgency = urgency,
                    UrgencyColor = urgencyColor,
                    RecommendationText = GenerateRecommendationText(batch, urgency),
                    Priority = priority++
                };

                recommendations.Add(recommendation);
                remainingQuantity -= batch.CurrentStock;
            }

            return recommendations;
        }

        /// <summary>
        /// Generate batch number for new batch creation
        /// </summary>
        public async Task<GenerateBatchNumberResponseDto> GenerateBatchNumberAsync(int productId, DateTime? productionDate = null)
        {
            var batchNumber = await GenerateBatchNumberInternalAsync(productId, productionDate);
            
            return new GenerateBatchNumberResponseDto
            {
                BatchNumber = batchNumber,
                Format = "{ProductCode}-{YYYYMMDD}-{Sequence}",
                Example = "PRD001-20250821-001"
            };
        }

        // ==================== HELPER METHODS ==================== //

        private string GenerateBatchStatusSummary(dynamic? batchInfo)
        {
            if (batchInfo == null)
                return "No batches";

            var parts = new List<string>();
            
            if (batchInfo.ExpiredCount > 0)
                parts.Add($"{batchInfo.ExpiredCount} expired");
            if (batchInfo.CriticalCount > 0)
                parts.Add($"{batchInfo.CriticalCount} critical");
            if (batchInfo.WarningCount > 0)
                parts.Add($"{batchInfo.WarningCount} warning");
            if (batchInfo.GoodCount > 0)
                parts.Add($"{batchInfo.GoodCount} good");

            return parts.Any() ? string.Join(", ", parts) : "No active batches";
        }

        private async Task<string?> GenerateFifoRecommendationText(int productId)
        {
            var nearestExpiryBatch = await _context.ProductBatches
                .Where(b => b.ProductId == productId && 
                           b.CurrentStock > 0 && 
                           !b.IsDisposed && 
                           b.ExpiryDate.HasValue)
                .OrderBy(b => b.ExpiryDate)
                .FirstOrDefaultAsync();

            if (nearestExpiryBatch == null)
                return null;

            var daysUntilExpiry = nearestExpiryBatch.DaysUntilExpiry ?? 0;
            
            return daysUntilExpiry switch
            {
                <= 1 => "URGENT: Sell immediately!",
                <= 3 => "Priority: Sell within 3 days",
                <= 7 => "Schedule: Sell within a week",
                _ => "Normal: Follow FIFO order"
            };
        }

        private async Task<string?> GenerateFifoRecommendationTextSafe(int productId)
        {
            try
            {
                return await GenerateFifoRecommendationText(productId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating FIFO recommendation for product {ProductId}", productId);
                return "Check batches manually";
            }
        }

        /// <summary>
        /// ✅ FIXED: Safe mapping method that works with in-memory batch data
        /// </summary>
        private Task<ProductWithBatchSummaryDto> MapToProductWithBatchSummaryDtoSafe(
            Product product, 
            List<ProductBatch> batches, 
            DateTime now)
        {
            try
            {
                // Find nearest expiry batch (in memory calculation)
                var nearestExpiryBatch = batches
                    .Where(b => b.ExpiryDate.HasValue && !b.IsExpired)
                    .OrderBy(b => b.ExpiryDate)
                    .FirstOrDefault();

                // Calculate total value (in memory)
                var totalValue = batches.Sum(b => b.CurrentStock * b.CostPerUnit);

                // Generate FIFO recommendation (in memory)
                var fifoRecommendation = GenerateFifoRecommendationTextInMemory(batches, now);

                // Generate batch status summary (in memory)
                var batchStatusSummary = GenerateBatchStatusSummaryInMemory(batches, now);

                return Task.FromResult(new ProductWithBatchSummaryDto
                {
                    Id = product.Id,
                    Name = product.Name ?? string.Empty,
                    Barcode = product.Barcode ?? string.Empty,
                    Description = product.Description,
                    Stock = product.Stock,
                    MinimumStock = product.MinimumStock,
                    BuyPrice = product.BuyPrice,
                    SellPrice = product.SellPrice,
                    Unit = product.Unit ?? string.Empty,
                    CategoryId = product.CategoryId,
                    CategoryName = product.Category?.Name ?? "No Category",
                    IsActive = product.IsActive,
                    CreatedAt = product.CreatedAt,
                    UpdatedAt = product.UpdatedAt,
                    ImageUrl = product.ImageUrl,
                    
                    // Batch summary fields (computed in memory)
                    TotalBatches = batches.Count,
                    NearestExpiryBatch = nearestExpiryBatch != null ? MapToProductBatchDtoSafe(nearestBatch: nearestExpiryBatch, productName: product.Name) : null,
                    TotalValueAllBatches = totalValue,
                    FifoRecommendation = fifoRecommendation,
                    BatchStatusSummary = batchStatusSummary
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping product {ProductId} to batch summary DTO", product.Id);
                throw;
            }
        }

        /// <summary>
        /// ✅ FIXED: Generate batch status summary in memory
        /// </summary>
        private string GenerateBatchStatusSummaryInMemory(List<ProductBatch> batches, DateTime now)
        {
            if (!batches.Any()) return "No batches";

            var active = batches.Count(b => !b.IsExpired && !b.IsDisposed);
            var expired = batches.Count(b => b.IsExpired || 
                (b.ExpiryDate.HasValue && b.ExpiryDate.Value < now));
            var critical = batches.Count(b => 
                !b.IsExpired && b.ExpiryDate.HasValue && 
                (b.ExpiryDate.Value - now).TotalDays <= 3 &&
                (b.ExpiryDate.Value - now).TotalDays >= 0);
            var warning = batches.Count(b => 
                !b.IsExpired && b.ExpiryDate.HasValue && 
                (b.ExpiryDate.Value - now).TotalDays <= 7 &&
                (b.ExpiryDate.Value - now).TotalDays > 3);

            var parts = new List<string>();
            if (expired > 0) parts.Add($"{expired} expired");
            if (critical > 0) parts.Add($"{critical} critical");
            if (warning > 0) parts.Add($"{warning} warning");
            if (active > 0) parts.Add($"{active} active");

            return parts.Any() ? string.Join(", ", parts) : "All disposed";
        }

        /// <summary>
        /// ✅ FIXED: Generate FIFO recommendation in memory
        /// </summary>
        private string? GenerateFifoRecommendationTextInMemory(List<ProductBatch> batches, DateTime now)
        {
            var activeBatches = batches.Where(b => !b.IsExpired && !b.IsDisposed && b.CurrentStock > 0).ToList();
            
            if (!activeBatches.Any())
                return null;

            var criticalBatches = activeBatches.Where(b => 
                b.ExpiryDate.HasValue && 
                (b.ExpiryDate.Value - now).TotalDays <= 3 &&
                (b.ExpiryDate.Value - now).TotalDays >= 0
            ).ToList();

            if (criticalBatches.Any())
            {
                var totalCriticalStock = criticalBatches.Sum(b => b.CurrentStock);
                return $"URGENT: Sell {totalCriticalStock} units from expiring batches first";
            }

            var warningBatches = activeBatches.Where(b => 
                b.ExpiryDate.HasValue && 
                (b.ExpiryDate.Value - now).TotalDays <= 7 &&
                (b.ExpiryDate.Value - now).TotalDays > 3
            ).ToList();

            if (warningBatches.Any())
            {
                return "Priority: Monitor expiry dates - some batches expire soon";
            }

            return "Normal: Follow FIFO order";
        }

        /// <summary>
        /// ✅ FIXED: Safe ProductBatchDto mapping
        /// </summary>
        private ProductBatchDto MapToProductBatchDtoSafe(ProductBatch nearestBatch, string? productName)
        {
            return new ProductBatchDto
            {
                Id = nearestBatch.Id,
                ProductId = nearestBatch.ProductId,
                ProductName = productName ?? string.Empty,
                BatchNumber = nearestBatch.BatchNumber ?? string.Empty,
                ExpiryDate = nearestBatch.ExpiryDate,
                ProductionDate = nearestBatch.ProductionDate,
                CurrentStock = nearestBatch.CurrentStock,
                InitialStock = nearestBatch.InitialStock,
                CostPerUnit = nearestBatch.CostPerUnit,
                SupplierName = nearestBatch.SupplierName,
                PurchaseOrderNumber = nearestBatch.PurchaseOrderNumber,
                Notes = nearestBatch.Notes,
                IsBlocked = nearestBatch.IsBlocked,
                BlockReason = nearestBatch.BlockReason,
                IsExpired = nearestBatch.IsExpired,
                IsDisposed = nearestBatch.IsDisposed,
                DisposalDate = nearestBatch.DisposalDate,
                DisposalMethod = nearestBatch.DisposalMethod,
                CreatedAt = nearestBatch.CreatedAt,
                UpdatedAt = nearestBatch.UpdatedAt,
                BranchId = nearestBatch.BranchId,
                BranchName = nearestBatch.Branch?.BranchName,
                
                // ✅ FIXED: Compute expiry status in memory instead of using computed property
                ExpiryStatus = CalculateExpiryStatusInMemory(nearestBatch, _timezoneService.Now),
                DaysUntilExpiry = CalculateDaysUntilExpiryInMemory(nearestBatch, _timezoneService.Now),
                AvailableStock = nearestBatch.CurrentStock
            };
        }

        /// <summary>
        /// ✅ FIXED: Calculate expiry status in memory
        /// </summary>
        private ExpiryStatus CalculateExpiryStatusInMemory(ProductBatch batch, DateTime now)
        {
            if (!batch.ExpiryDate.HasValue) return ExpiryStatus.NoExpiry;
            if (batch.IsExpired) return ExpiryStatus.Expired;

            var daysUntilExpiry = (batch.ExpiryDate.Value - now).TotalDays;
            
            return daysUntilExpiry switch
            {
                < 0 => ExpiryStatus.Expired,
                <= 3 => ExpiryStatus.Critical,
                <= 7 => ExpiryStatus.Warning,
                <= 30 => ExpiryStatus.Normal,
                _ => ExpiryStatus.Good
            };
        }

        /// <summary>
        /// ✅ FIXED: Calculate days until expiry in memory
        /// </summary>
        private int? CalculateDaysUntilExpiryInMemory(ProductBatch batch, DateTime now)
        {
            if (!batch.ExpiryDate.HasValue) return null;
            return (int)(batch.ExpiryDate.Value.Date - now.Date).TotalDays;
        }

        private async Task<string> GenerateBatchNumberInternalAsync(int productId, DateTime? productionDate = null)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null)
                throw new ArgumentException("Product not found");

            var date = productionDate ?? _timezoneService.Today;
            var dateString = date.ToString("yyyyMMdd");
            
            // Generate product code (first 3 chars of name + ID)
            var productCode = (product.Name.Length >= 3 ? 
                product.Name.Substring(0, 3).ToUpper() : 
                product.Name.ToUpper().PadRight(3, '0')) + product.Id.ToString("D3");

            // Find next sequence number for this product and date
            var existingBatches = await _context.ProductBatches
                .Where(b => b.ProductId == productId && 
                           b.BatchNumber.Contains(dateString))
                .CountAsync();

            var sequence = (existingBatches + 1).ToString("D3");
            
            return $"{productCode}-{dateString}-{sequence}";
        }

        private string GetFifoUrgency(int daysUntilExpiry)
        {
            return daysUntilExpiry switch
            {
                <= 0 => "Expired",
                <= 1 => "Critical",
                <= 3 => "Warning",
                _ => "Good"
            };
        }

        private string GetUrgencyColor(string urgency)
        {
            return urgency switch
            {
                "Expired" => "#7f1d1d",
                "Critical" => "#ef4444",
                "Warning" => "#f59e0b",
                _ => "#22c55e"
            };
        }

        private string GenerateRecommendationText(ProductBatch batch, string urgency)
        {
            return urgency switch
            {
                "Expired" => $"EXPIRED {Math.Abs(batch.DaysUntilExpiry ?? 0)} days ago - Dispose immediately",
                "Critical" => "Expires within 24 hours - Sell immediately!",
                "Warning" => $"Expires in {batch.DaysUntilExpiry} days - High priority",
                _ => $"Good condition - Normal FIFO order (expires in {batch.DaysUntilExpiry} days)"
            };
        }
    }
}
