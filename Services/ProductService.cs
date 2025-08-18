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

        // Implement remaining methods as minimal stubs for now
        public Task<ProductBatchDto> CreateProductBatchAsync(CreateProductBatchDto request, int createdByUserId)
        {
            throw new NotImplementedException("Product batch creation not fully implemented yet");
        }

        public Task<ProductBatchDto> CreateProductBatchAsync(int productId, CreateProductBatchDto request, int createdByUserId, int branchId)
        {
            throw new NotImplementedException("Product batch creation not fully implemented yet");
        }
        public Task<ProductBatchDto?> UpdateProductBatchAsync(int batchId, UpdateProductBatchDto request, int updatedByUserId) => throw new NotImplementedException("Product batch updates not fully implemented yet");
        public Task<bool> DeleteProductBatchAsync(int batchId) => throw new NotImplementedException("Product batch deletion not fully implemented yet");
        public async Task<List<ExpiringProductDto>> GetExpiringProductsAsync(ExpiringProductsFilterDto filter)
        {
            // Convert filter to the parameters we expect
            var warningDays = filter.DaysUntilExpiry ?? 7;
            return await GetExpiringProductsAsync(warningDays, filter.BranchId);
        }
        public async Task<List<ExpiredProductDto>> GetExpiredProductsAsync(ExpiredProductsFilterDto filter)
        {
            return await GetExpiredProductsAsync(filter.BranchId);
        }
        public Task<ExpiryValidationDto> ValidateExpiryRequirementsAsync(int productId, DateTime? expiryDate) => throw new NotImplementedException("Expiry validation not fully implemented yet");
        public Task<bool> MarkBatchesAsExpiredAsync() => throw new NotImplementedException("Batch expiry marking not fully implemented yet");
        public Task<List<BatchRecommendationDto>> GetBatchSaleOrderAsync(int productId, int requestedQuantity) => throw new NotImplementedException("Batch sale order not fully implemented yet");
        public Task<bool> ProcessFifoSaleAsync(int productId, int quantity, string referenceNumber) => throw new NotImplementedException("FIFO sale processing not fully implemented yet");
        public Task<bool> DisposeExpiredProductsAsync(DisposeExpiredProductsDto request, int disposedByUserId) => throw new NotImplementedException("Bulk disposal not fully implemented yet");
        public Task<bool> DisposeProductBatchAsync(int batchId, DisposeBatchDto request, int disposedByUserId) => throw new NotImplementedException("Batch disposal not fully implemented yet");
        public Task<List<ExpiredProductDto>> GetDisposableProductsAsync(int? branchId = null) => throw new NotImplementedException("Use GetExpiredProductsAsync method instead");
        public Task<List<ProductDto>> GetProductsRequiringExpiryAsync() => throw new NotImplementedException("Products requiring expiry not fully implemented yet");
    }
}