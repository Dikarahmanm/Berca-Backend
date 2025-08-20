using Microsoft.EntityFrameworkCore;
using Berca_Backend.Data;
using Berca_Backend.DTOs;
using Berca_Backend.Models;
using Berca_Backend.Services.Interfaces;

namespace Berca_Backend.Services
{
    public class ExpiryManagementService : IExpiryManagementService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ExpiryManagementService> _logger;
        private readonly INotificationService _notificationService;

        public ExpiryManagementService(
            AppDbContext context, 
            ILogger<ExpiryManagementService> logger,
            INotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _notificationService = notificationService;
        }

        // ==================== PRODUCT BATCH MANAGEMENT ==================== //

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
                if (product.Category?.RequiresExpiryDate == true && !request.ExpiryDate.HasValue)
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
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.ProductBatches.Add(batch);
                await _context.SaveChangesAsync();

                return await MapToProductBatchDto(batch);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product batch for ProductId: {ProductId}", request.ProductId);
                throw;
            }
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
                batch.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return await MapToProductBatchDto(batch);
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

        public async Task<List<ProductBatchDto>> GetProductBatchesAsync(int productId)
        {
            var batches = await _context.ProductBatches
                .Where(b => b.ProductId == productId)
                .Include(b => b.Product)
                .Include(b => b.Branch)
                .Include(b => b.CreatedByUser)
                .Include(b => b.UpdatedByUser)
                .OrderBy(b => b.ExpiryDate)
                .ToListAsync();

            var results = new List<ProductBatchDto>();
            foreach (var batch in batches)
            {
                results.Add(await MapToProductBatchDto(batch));
            }
            return results;
        }

        public async Task<ProductBatchDto?> GetProductBatchByIdAsync(int batchId)
        {
            var batch = await _context.ProductBatches
                .Include(b => b.Product)
                .Include(b => b.Branch)
                .Include(b => b.CreatedByUser)
                .Include(b => b.UpdatedByUser)
                .FirstOrDefaultAsync(b => b.Id == batchId);

            return batch != null ? await MapToProductBatchDto(batch) : null;
        }

        // ==================== EXPIRY TRACKING ==================== //

        public async Task<List<ExpiringProductDto>> GetExpiringProductsAsync(ExpiringProductsFilterDto filter)
        {
            var query = _context.ProductBatches
                .Include(b => b.Product)
                .ThenInclude(p => p.Category!)
                .Include(b => b.Branch)
                .Where(b => b.ExpiryDate.HasValue && b.CurrentStock > 0 && !b.IsDisposed);

            // Apply filters
            if (filter.CategoryId.HasValue)
                query = query.Where(b => b.Product != null && b.Product.CategoryId == filter.CategoryId);

            if (filter.BranchId.HasValue)
                query = query.Where(b => b.BranchId == filter.BranchId);

            if (filter.ExpiryStatus.HasValue)
            {
                var today = DateTime.UtcNow.Date;
                query = filter.ExpiryStatus switch
                {
                    ExpiryStatus.Critical => query.Where(b => b.ExpiryDate!.Value.Date <= today.AddDays(3)),
                    ExpiryStatus.Warning => query.Where(b => b.ExpiryDate!.Value.Date <= today.AddDays(7) && b.ExpiryDate.Value.Date > today.AddDays(3)),
                    ExpiryStatus.Normal => query.Where(b => b.ExpiryDate!.Value.Date <= today.AddDays(30) && b.ExpiryDate.Value.Date > today.AddDays(7)),
                    ExpiryStatus.Good => query.Where(b => b.ExpiryDate!.Value.Date > today.AddDays(30)),
                    _ => query
                };
            }

            if (filter.DaysUntilExpiry.HasValue)
            {
                var targetDate = DateTime.UtcNow.Date.AddDays(filter.DaysUntilExpiry.Value);
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

            // Apply sorting
            query = filter.SortBy.ToLower() switch
            {
                "expiry_date" => filter.SortOrder.ToLower() == "desc" 
                    ? query.OrderByDescending(b => b.ExpiryDate)
                    : query.OrderBy(b => b.ExpiryDate),
                "stock" => filter.SortOrder.ToLower() == "desc"
                    ? query.OrderByDescending(b => b.CurrentStock)
                    : query.OrderBy(b => b.CurrentStock),
                "value" => filter.SortOrder.ToLower() == "desc"
                    ? query.OrderByDescending(b => b.CurrentStock * b.CostPerUnit)
                    : query.OrderBy(b => b.CurrentStock * b.CostPerUnit),
                _ => query.OrderBy(b => b.ExpiryDate)
            };

            var batches = await query
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

        public async Task<List<ExpiredProductDto>> GetExpiredProductsAsync(ExpiredProductsFilterDto filter)
        {
            var today = DateTime.UtcNow.Date;
            var query = _context.ProductBatches
                .Include(b => b.Product)
                .ThenInclude(p => p.Category!)
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

        public async Task<ExpiryValidationDto> ValidateExpiryRequirementsAsync(int productId, DateTime? expiryDate)
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
                else if (expiryDate.Value.Date <= DateTime.UtcNow.Date)
                {
                    result.ValidationErrors.Add("Expiry date must be in the future");
                }
            }

            result.IsValid = !result.ValidationErrors.Any();
            return result;
        }

        public async Task<int> MarkBatchesAsExpiredAsync()
        {
            var today = DateTime.UtcNow.Date;
            var expiredBatches = await _context.ProductBatches
                .Where(b => b.ExpiryDate.HasValue && 
                           b.ExpiryDate.Value.Date < today && 
                           !b.IsExpired)
                .ToListAsync();

            foreach (var batch in expiredBatches)
            {
                batch.IsExpired = true;
                batch.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return expiredBatches.Count;
        }

        public async Task<List<ExpiringProductDto>> GetProductsRequiringExpiryAsync()
        {
            var productsWithoutBatches = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductBatches)
                .Where(p => p.Category != null && p.Category.RequiresExpiryDate && !p.ProductBatches.Any())
                .ToListAsync();

            return productsWithoutBatches.Select(product => new ExpiringProductDto
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ProductBarcode = product.Barcode,
                CategoryName = product.Category?.Name ?? "Unknown Category",
                CategoryColor = product.Category?.Color ?? "#000000",
                BatchId = 0,
                BatchNumber = "No Batch",
                ExpiryDate = DateTime.MinValue,
                DaysUntilExpiry = -999,
                ExpiryStatus = ExpiryStatus.NoExpiry,
                CurrentStock = product.Stock,
                AvailableStock = 0,
                ValueAtRisk = 0,
                CostPerUnit = product.BuyPrice,
                UrgencyLevel = ExpiryUrgency.Critical,
                CreatedAt = product.CreatedAt
            }).ToList();
        }

        // ==================== FIFO LOGIC ==================== //

        public async Task<List<FifoRecommendationDto>> GetFifoRecommendationsAsync(int? categoryId = null, int? branchId = null)
        {
            var query = _context.Products
                .Include(p => p.ProductBatches.Where(b => b.CurrentStock > 0 && !b.IsBlocked && !b.IsExpired))
                .Where(p => p.ProductBatches.Any(b => b.CurrentStock > 0 && !b.IsBlocked && !b.IsExpired));

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId);

            if (branchId.HasValue)
                query = query.Where(p => p.ProductBatches.Any(b => b.BranchId == branchId));

            var products = await query.ToListAsync();

            return products.Select(product => new FifoRecommendationDto
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ProductBarcode = product.Barcode,
                BatchRecommendations = GetBatchRecommendations(product.ProductBatches.ToList()),
                TotalAvailableStock = product.ProductBatches.Sum(b => b.AvailableStock),
                AverageCostPerUnit = product.ProductBatches.Average(b => b.CostPerUnit)
            }).ToList();
        }

        public async Task<List<BatchRecommendationDto>> GetBatchSaleOrderAsync(int productId, int requestedQuantity)
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

        public async Task<bool> ProcessFifoSaleAsync(int productId, int quantity, string referenceNumber, int processedByUserId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
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

                var totalAvailable = batches.Sum(b => b.CurrentStock);
                if (totalAvailable < quantity)
                    throw new InvalidOperationException("Insufficient stock available");

                var remainingQuantity = quantity;
                foreach (var batch in batches)
                {
                    if (remainingQuantity <= 0) break;

                    var quantityFromThisBatch = Math.Min(remainingQuantity, batch.CurrentStock);
                    batch.CurrentStock -= quantityFromThisBatch;
                    batch.UpdatedAt = DateTime.UtcNow;
                    batch.UpdatedByUserId = processedByUserId;

                    remainingQuantity -= quantityFromThisBatch;

                    _logger.LogInformation("Processed FIFO sale: {Quantity} from batch {BatchNumber} for reference {Reference}", 
                        quantityFromThisBatch, batch.BatchNumber, referenceNumber);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error processing FIFO sale for ProductId: {ProductId}, Quantity: {Quantity}", productId, quantity);
                throw;
            }
        }

        public async Task<List<BatchRecommendationDto>> GetBatchAllocationForTransferAsync(int productId, int quantity, int sourceBranchId)
        {
            var batches = await _context.ProductBatches
                .Where(b => b.ProductId == productId && 
                           b.BranchId == sourceBranchId && 
                           b.CurrentStock > 0 && 
                           !b.IsBlocked && 
                           !b.IsExpired)
                .OrderBy(b => b.ExpiryDate)
                .ThenBy(b => b.CreatedAt)
                .ToListAsync();

            return GetBatchRecommendations(batches, quantity);
        }

        // ==================== DISPOSAL MANAGEMENT ==================== //

        public async Task<bool> DisposeExpiredProductsAsync(DisposeExpiredProductsDto request, int disposedByUserId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var batches = await _context.ProductBatches
                    .Where(b => request.BatchIds.Contains(b.Id))
                    .ToListAsync();

                foreach (var batch in batches)
                {
                    batch.IsDisposed = true;
                    batch.DisposalDate = DateTime.UtcNow;
                    batch.DisposedByUserId = disposedByUserId;
                    batch.DisposalMethod = request.DisposalMethod;
                    batch.Notes = string.IsNullOrEmpty(batch.Notes) 
                        ? request.Notes 
                        : $"{batch.Notes}; {request.Notes}";
                    batch.UpdatedAt = DateTime.UtcNow;
                    batch.UpdatedByUserId = disposedByUserId;
                }

                await _context.SaveChangesAsync();

                // Create disposal notification
                await _notificationService.CreateDisposalCompletedNotificationAsync(
                    batches.Count, 
                    batches.Sum(b => b.CurrentStock * b.CostPerUnit), 
                    request.DisposalMethod, 
                    batches.FirstOrDefault()?.BranchId);

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error disposing expired products");
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

        public async Task<bool> UndoDisposalAsync(List<int> batchIds, int undoneByUserId)
        {
            try
            {
                var batches = await _context.ProductBatches
                    .Where(b => batchIds.Contains(b.Id) && b.IsDisposed)
                    .ToListAsync();

                foreach (var batch in batches)
                {
                    batch.IsDisposed = false;
                    batch.DisposalDate = null;
                    batch.DisposedByUserId = null;
                    batch.DisposalMethod = null;
                    batch.UpdatedAt = DateTime.UtcNow;
                    batch.UpdatedByUserId = undoneByUserId;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error undoing disposal for batches");
                throw;
            }
        }

        // ==================== EXPIRY ANALYTICS ==================== //

        public async Task<ExpiryAnalyticsDto> GetExpiryAnalyticsAsync(int? branchId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.ProductBatches
                .Include(b => b.Product)
                .ThenInclude(p => p.Category!)
                .Include(b => b.Branch)
                .Where(b => b.ExpiryDate.HasValue);

            if (branchId.HasValue)
                query = query.Where(b => b.BranchId == branchId);

            if (startDate.HasValue)
                query = query.Where(b => b.CreatedAt >= startDate);

            if (endDate.HasValue)
                query = query.Where(b => b.CreatedAt <= endDate);

            var batches = await query.ToListAsync();
            var today = DateTime.UtcNow.Date;

            var analytics = new ExpiryAnalyticsDto
            {
                BranchId = branchId,
                BranchName = branchId.HasValue ? 
                    (await _context.Branches.FindAsync(branchId))?.BranchName : 
                    "All Branches",
                AnalysisDate = DateTime.UtcNow,
                TotalProductsWithExpiry = batches.Count,
                ExpiringIn7Days = batches.Count(b => b.ExpiryStatus == ExpiryStatus.Warning || b.ExpiryStatus == ExpiryStatus.Critical),
                ExpiringIn3Days = batches.Count(b => b.ExpiryStatus == ExpiryStatus.Critical),
                ExpiredProducts = batches.Count(b => b.ExpiryStatus == ExpiryStatus.Expired),
                DisposedProducts = batches.Count(b => b.IsDisposed),
                ValueAtRisk = batches.Where(b => b.ExpiryStatus == ExpiryStatus.Warning || b.ExpiryStatus == ExpiryStatus.Critical)
                    .Sum(b => b.CurrentStock * b.CostPerUnit),
                ValueLost = batches.Where(b => b.IsDisposed || b.ExpiryStatus == ExpiryStatus.Expired)
                    .Sum(b => b.CurrentStock * b.CostPerUnit)
            };

            var totalValue = batches.Sum(b => b.InitialStock * b.CostPerUnit);
            analytics.WastagePercentage = totalValue > 0 ? (analytics.ValueLost / totalValue) * 100 : 0;

            analytics.CategoryStats = await GetCategoryExpiryStatsAsync(branchId);

            return analytics;
        }

        public async Task<List<CategoryExpiryStatsDto>> GetCategoryExpiryStatsAsync(int? branchId = null)
        {
            var query = _context.ProductBatches
                .Include(b => b.Product)
                .ThenInclude(p => p.Category!)
                .Where(b => b.ExpiryDate.HasValue);

            if (branchId.HasValue)
                query = query.Where(b => b.BranchId == branchId);

            var batches = await query.ToListAsync();

            return batches.Where(b => b.Product?.Category != null)
                .GroupBy(b => new { b.Product!.CategoryId, b.Product.Category!.Name, b.Product.Category.Color })
                .Select(g => new CategoryExpiryStatsDto
                {
                    CategoryId = g.Key.CategoryId,
                    CategoryName = g.Key.Name,
                    CategoryColor = g.Key.Color,
                    TotalProducts = g.Count(),
                    ExpiringProducts = g.Count(b => b.ExpiryStatus == ExpiryStatus.Warning || b.ExpiryStatus == ExpiryStatus.Critical),
                    ExpiredProducts = g.Count(b => b.ExpiryStatus == ExpiryStatus.Expired),
                    ValueAtRisk = g.Where(b => b.ExpiryStatus == ExpiryStatus.Warning || b.ExpiryStatus == ExpiryStatus.Critical)
                        .Sum(b => b.CurrentStock * b.CostPerUnit),
                    ValueLost = g.Where(b => b.IsDisposed || b.ExpiryStatus == ExpiryStatus.Expired)
                        .Sum(b => b.CurrentStock * b.CostPerUnit)
                }).ToList();
        }

        public async Task<List<ExpiryTrendDto>> GetExpiryTrendsAsync(DateTime startDate, DateTime endDate, int? branchId = null)
        {
            var query = _context.ProductBatches
                .Where(b => b.ExpiryDate.HasValue && 
                           b.CreatedAt >= startDate && 
                           b.CreatedAt <= endDate);

            if (branchId.HasValue)
                query = query.Where(b => b.BranchId == branchId);

            var batches = await query.ToListAsync();

            var trends = new List<ExpiryTrendDto>();
            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                var dayBatches = batches.Where(b => b.CreatedAt.Date == date).ToList();
                
                trends.Add(new ExpiryTrendDto
                {
                    Date = date,
                    ExpiredProducts = dayBatches.Count(b => b.ExpiryStatus == ExpiryStatus.Expired),
                    DisposedProducts = dayBatches.Count(b => b.IsDisposed),
                    ValueLost = dayBatches.Where(b => b.IsDisposed || b.ExpiryStatus == ExpiryStatus.Expired)
                        .Sum(b => b.CurrentStock * b.CostPerUnit),
                    ProductsNearingExpiry = dayBatches.Count(b => b.ExpiryStatus == ExpiryStatus.Warning || b.ExpiryStatus == ExpiryStatus.Critical),
                    ValueAtRisk = dayBatches.Where(b => b.ExpiryStatus == ExpiryStatus.Warning || b.ExpiryStatus == ExpiryStatus.Critical)
                        .Sum(b => b.CurrentStock * b.CostPerUnit)
                });
            }

            return trends;
        }

        public async Task<WastageMetricsDto> GetWastageMetricsAsync(int? branchId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.ProductBatches
                .Include(b => b.Product)
                .ThenInclude(p => p.Category!)
                .Where(b => b.ExpiryDate.HasValue);

            if (branchId.HasValue)
                query = query.Where(b => b.BranchId == branchId);

            if (startDate.HasValue)
                query = query.Where(b => b.CreatedAt >= startDate);

            if (endDate.HasValue)
                query = query.Where(b => b.CreatedAt <= endDate);

            var batches = await query.ToListAsync();

            var totalValuePurchased = batches.Sum(b => b.InitialStock * b.CostPerUnit);
            var totalValueSold = batches.Sum(b => (b.InitialStock - b.CurrentStock) * b.CostPerUnit);
            var totalValueExpired = batches.Where(b => b.ExpiryStatus == ExpiryStatus.Expired)
                .Sum(b => b.CurrentStock * b.CostPerUnit);
            var totalValueDisposed = batches.Where(b => b.IsDisposed)
                .Sum(b => b.CurrentStock * b.CostPerUnit);

            return new WastageMetricsDto
            {
                TotalValuePurchased = totalValuePurchased,
                TotalValueSold = totalValueSold,
                TotalValueExpired = totalValueExpired,
                TotalValueDisposed = totalValueDisposed,
                WastagePercentage = totalValuePurchased > 0 ? ((totalValueExpired + totalValueDisposed) / totalValuePurchased) * 100 : 0,
                RecoveryPercentage = totalValuePurchased > 0 ? (totalValueSold / totalValuePurchased) * 100 : 0,
                CategoryBreakdown = batches.Where(b => b.Product?.Category != null)
                    .GroupBy(b => new { b.Product!.CategoryId, b.Product.Category!.Name })
                    .Select(g => new CategoryWastageDto
                    {
                        CategoryId = g.Key.CategoryId,
                        CategoryName = g.Key.Name,
                        ValuePurchased = g.Sum(b => b.InitialStock * b.CostPerUnit),
                        ValueExpired = g.Where(b => b.ExpiryStatus == ExpiryStatus.Expired || b.IsDisposed)
                            .Sum(b => b.CurrentStock * b.CostPerUnit),
                        WastagePercentage = g.Sum(b => b.InitialStock * b.CostPerUnit) > 0 
                            ? (g.Where(b => b.ExpiryStatus == ExpiryStatus.Expired || b.IsDisposed)
                                .Sum(b => b.CurrentStock * b.CostPerUnit) / g.Sum(b => b.InitialStock * b.CostPerUnit)) * 100 
                            : 0
                    }).ToList()
            };
        }

        // ==================== NOTIFICATION SUPPORT ==================== //

        public async Task<List<ExpiryNotificationDto>> GetProductsRequiringNotificationAsync(int? branchId = null)
        {
            var filter = new ExpiringProductsFilterDto
            {
                BranchId = branchId,
                DaysUntilExpiry = 7,
                PageSize = 1000
            };

            var expiringProducts = await GetExpiringProductsAsync(filter);

            return expiringProducts.Select(p => new ExpiryNotificationDto
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                BatchId = p.BatchId,
                BatchNumber = p.BatchNumber,
                ExpiryDate = p.ExpiryDate,
                DaysUntilExpiry = p.DaysUntilExpiry,
                ExpiryStatus = p.ExpiryStatus,
                CurrentStock = p.CurrentStock,
                ValueAtRisk = p.ValueAtRisk,
                BranchId = p.BranchId,
                BranchName = p.BranchName,
                NotificationPriority = GetNotificationPriority(p.DaysUntilExpiry)
            }).ToList();
        }

        public async Task<int> CreateExpiryNotificationsAsync(int? branchId = null)
        {
            try
            {
                var products = await GetProductsRequiringNotificationAsync(branchId);
                var notificationCount = 0;

                foreach (var product in products)
                {
                    var notificationType = product.ExpiryStatus switch
                    {
                        ExpiryStatus.Critical => ExpiryNotificationTypes.EXPIRY_URGENT,
                        ExpiryStatus.Warning => ExpiryNotificationTypes.EXPIRY_WARNING,
                        ExpiryStatus.Expired => ExpiryNotificationTypes.EXPIRY_EXPIRED,
                        _ => ExpiryNotificationTypes.EXPIRY_WARNING
                    };

                    // Use appropriate notification method based on expiry status
                    switch (product.ExpiryStatus)
                    {
                        case ExpiryStatus.Critical:
                            await _notificationService.CreateExpiryUrgentNotificationAsync(
                                product.ProductId, product.ProductName, product.BatchNumber,
                                product.ExpiryDate, product.CurrentStock, product.BranchId);
                            break;
                        case ExpiryStatus.Warning:
                            await _notificationService.CreateExpiryWarningNotificationAsync(
                                product.ProductId, product.ProductName, product.BatchNumber,
                                product.ExpiryDate, product.CurrentStock, product.BranchId);
                            break;
                        case ExpiryStatus.Expired:
                            await _notificationService.CreateExpiryExpiredNotificationAsync(
                                product.ProductId, product.ProductName, product.BatchNumber,
                                product.ExpiryDate, product.CurrentStock, product.BranchId);
                            break;
                    }

                    notificationCount++;
                }

                return notificationCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating expiry notifications");
                throw;
            }
        }

        // ==================== BACKGROUND TASKS ==================== //

        public async Task<ExpiryCheckResultDto> PerformDailyExpiryCheckAsync()
        {
            try
            {
                var checkDate = DateTime.UtcNow;
                var newlyExpiredCount = await MarkBatchesAsExpiredAsync();
                var statusUpdatedCount = await UpdateExpiryStatusesAsync();
                var notificationsCreated = await CreateExpiryNotificationsAsync();

                var criticalItems = await GetProductsRequiringNotificationAsync();
                var criticalProducts = criticalItems.Where(p => p.ExpiryStatus == ExpiryStatus.Critical).ToList();

                var valueAtRisk = criticalItems.Sum(p => p.ValueAtRisk);
                var newValueLost = await _context.ProductBatches
                    .Where(b => b.IsExpired && b.UpdatedAt.Date == checkDate.Date)
                    .SumAsync(b => b.CurrentStock * b.CostPerUnit);

                return new ExpiryCheckResultDto
                {
                    CheckDate = checkDate,
                    NewlyExpiredBatches = newlyExpiredCount,
                    NotificationsCreated = notificationsCreated,
                    StatusesUpdated = statusUpdatedCount,
                    ValueAtRisk = valueAtRisk,
                    NewValueLost = newValueLost,
                    CriticalItems = criticalProducts
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing daily expiry check");
                throw;
            }
        }

        public async Task<int> UpdateExpiryStatusesAsync()
        {
            var batches = await _context.ProductBatches
                .Where(b => b.ExpiryDate.HasValue)
                .ToListAsync();

            var updatedCount = 0;
            foreach (var batch in batches)
            {
                var oldStatus = batch.ExpiryStatus;
                // The ExpiryStatus property is computed, so we just need to save to trigger any dependent logic
                if (oldStatus != batch.ExpiryStatus)
                {
                    batch.UpdatedAt = DateTime.UtcNow;
                    updatedCount++;
                }
            }

            if (updatedCount > 0)
                await _context.SaveChangesAsync();

            return updatedCount;
        }

        // ==================== PRIVATE HELPER METHODS ==================== //

        private async Task<ProductBatchDto> MapToProductBatchDto(ProductBatch batch)
        {
            if (batch.Product == null)
            {
                batch.Product = await _context.Products
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.Id == batch.ProductId);
            }

            return new ProductBatchDto
            {
                Id = batch.Id,
                ProductId = batch.ProductId,
                ProductName = batch.Product?.Name ?? "Unknown Product",
                BatchNumber = batch.BatchNumber,
                ExpiryDate = batch.ExpiryDate,
                ProductionDate = batch.ProductionDate,
                CurrentStock = batch.CurrentStock,
                InitialStock = batch.InitialStock,
                CostPerUnit = batch.CostPerUnit,
                SupplierName = batch.SupplierName,
                PurchaseOrderNumber = batch.PurchaseOrderNumber,
                Notes = batch.Notes,
                IsBlocked = batch.IsBlocked,
                BlockReason = batch.BlockReason,
                IsExpired = batch.IsExpired,
                IsDisposed = batch.IsDisposed,
                DisposalDate = batch.DisposalDate,
                DisposalMethod = batch.DisposalMethod,
                BranchId = batch.BranchId,
                BranchName = batch.Branch?.BranchName ?? "All Branches",
                CreatedAt = batch.CreatedAt,
                UpdatedAt = batch.UpdatedAt,
                CreatedByUserName = batch.CreatedByUser?.Username ?? "Unknown User",
                UpdatedByUserName = batch.UpdatedByUser?.Username ?? "Unknown User",
                DaysUntilExpiry = batch.DaysUntilExpiry,
                ExpiryStatus = batch.ExpiryStatus,
                AvailableStock = batch.AvailableStock
            };
        }

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

        private string GetNotificationPriority(int daysUntilExpiry)
        {
            return daysUntilExpiry switch
            {
                <= 1 => "Critical",
                <= 3 => "High",
                <= 7 => "Medium",
                _ => "Low"
            };
        }
    }
}