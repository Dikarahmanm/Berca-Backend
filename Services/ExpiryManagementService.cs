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

                // Validate branch exists if BranchId is specified
                if (request.BranchId.HasValue)
                {
                    var branchExists = await _context.Branches
                        .AnyAsync(b => b.Id == request.BranchId.Value && b.IsActive);
                    
                    if (!branchExists)
                        throw new ArgumentException($"Branch with ID {request.BranchId} does not exist or is inactive");
                }

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
                .Include(b => b.Product!)
                    .ThenInclude(p => p.Category)
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
                .Include(b => b.Product!)
                    .ThenInclude(p => p.Category)
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
                .Include(b => b.Product!)
                    .ThenInclude(p => p.Category)
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
                .Include(b => b.Product!)
                    .ThenInclude(p => p.Category)
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

        // ==================== COMPREHENSIVE ANALYTICS METHODS ==================== //

        /// <summary>
        /// Get comprehensive expiry analytics with financial calculations
        /// </summary>
        public async Task<ComprehensiveExpiryAnalyticsDto> GetComprehensiveExpiryAnalyticsAsync(int? branchId = null)
        {
            try
            {
                var currentDate = DateTime.UtcNow;
                
                // Get all batches with expiry tracking
                var batchesQuery = _context.ProductBatches
                    .Include(pb => pb.Product)
                    .ThenInclude(p => p!.Category)
                    .Where(pb => pb.ExpiryDate.HasValue && !pb.IsDisposed);
                    
                if (branchId.HasValue)
                    batchesQuery = batchesQuery.Where(pb => pb.BranchId == branchId);
                    
                var batches = await batchesQuery.ToListAsync();

                // Calculate comprehensive metrics
                var analytics = new ComprehensiveExpiryAnalyticsDto
                {
                    AnalysisDate = currentDate,
                    BranchId = branchId,
                    
                    // Basic counts
                    TotalBatchesWithExpiry = batches.Count,
                    ExpiredBatches = batches.Count(b => b.ExpiryDate <= currentDate),
                    ExpiringNext7Days = batches.Count(b => b.ExpiryDate <= currentDate.AddDays(7) && b.ExpiryDate > currentDate),
                    ExpiringNext30Days = batches.Count(b => b.ExpiryDate <= currentDate.AddDays(30) && b.ExpiryDate > currentDate.AddDays(7)),
                    
                    // Financial impact calculations
                    TotalStockValue = batches.Sum(b => b.CurrentStock * b.CostPerUnit),
                    ExpiredStockValue = batches.Where(b => b.ExpiryDate <= currentDate).Sum(b => b.CurrentStock * b.CostPerUnit),
                    AtRiskStockValue = batches.Where(b => b.ExpiryDate <= currentDate.AddDays(30) && b.ExpiryDate > currentDate).Sum(b => b.CurrentStock * b.CostPerUnit),
                    
                    // Performance metrics
                    WastagePercentage = CalculateWastePercentageHelper(batches, currentDate),
                    MonthlyExpiryRate = await CalculateMonthlyExpiryRateHelper(branchId),
                    PreventionOpportunityValue = CalculatePreventionOpportunityHelper(batches),
                    
                    // Category breakdown
                    CategoryPerformance = CalculateCategoryExpiryPerformanceHelper(batches),
                    
                    // Trend analysis (simplified for now)
                    ExpiryTrends = await CalculateExpiryTrendsHelper(branchId, 6), // Last 6 months
                    
                    // Recommendations
                    ActionableRecommendations = GenerateExpiryActionRecommendationsHelper(batches, currentDate),
                    
                    // Projections
                    ProjectedExpiryNext30Days = (int)(await ProjectFutureExpiryHelper(branchId, 30)),
                    EstimatedSavingsOpportunity = CalculateEstimatedSavingsHelper(batches, currentDate)
                };

                return analytics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating comprehensive expiry analytics for branch {BranchId}", branchId);
                throw;
            }
        }

        /// <summary>
        /// Get smart FIFO recommendations with advanced scoring
        /// </summary>
        public async Task<List<SmartFifoRecommendationDto>> GetSmartFifoRecommendationsAsync(int? branchId = null)
        {
            try
            {
                var batches = await _context.ProductBatches
                    .Include(pb => pb.Product)
                    .ThenInclude(p => p!.Category)
                    .Where(pb => pb.CurrentStock > 0 && !pb.IsDisposed && !pb.IsBlocked)
                    .Where(pb => branchId == null || pb.BranchId == branchId)
                    .ToListAsync();

                var recommendations = new List<SmartFifoRecommendationDto>();

                foreach (var batch in batches)
                {
                    var daysToExpiry = batch.ExpiryDate.HasValue ? 
                        (batch.ExpiryDate.Value - DateTime.UtcNow).Days : 
                        int.MaxValue;

                    // Calculate sales velocity for this product (simplified)
                    var recentSales = await _context.SaleItems
                        .Where(si => si.ProductId == batch.ProductId && 
                                   si.Sale != null && si.Sale.SaleDate >= DateTime.UtcNow.AddDays(-30))
                        .SumAsync(si => si.Quantity);
                    var dailyVelocity = recentSales / 30.0m;

                    // Calculate smart priority score (0-100)
                    var priorityScore = CalculateSmartPriorityScoreHelper(batch, daysToExpiry, dailyVelocity);

                    // Determine optimal pricing strategy
                    var productForPricing = batch.Product ?? new Product { SellPrice = 0, BuyPrice = 0 };
                    var pricingRecommendation = CalculateOptimalPricingHelper(productForPricing, daysToExpiry, dailyVelocity);

                    // Financial impact analysis
                    var financialImpact = CalculateFinancialImpactHelper(batch, pricingRecommendation, daysToExpiry);

                    var recommendation = new SmartFifoRecommendationDto
                    {
                        BatchId = batch.Id,
                        ProductId = batch.ProductId,
                        ProductName = batch.Product != null ? batch.Product.Name : string.Empty,
                        BatchNumber = batch.BatchNumber,
                        CurrentStock = batch.CurrentStock,
                        ExpiryDate = batch.ExpiryDate,
                        DaysToExpiry = daysToExpiry,
                        
                        // Smart scoring
                        PriorityScore = priorityScore,
                        UrgencyLevel = CalculateUrgencyLevelHelper(priorityScore),
                        RecommendedAction = DetermineRecommendedActionHelper(daysToExpiry, dailyVelocity, batch.CurrentStock),
                        
                        // Pricing optimization
                        CurrentPrice = batch.Product != null ? batch.Product.SellPrice : 0,
                        RecommendedPrice = pricingRecommendation.OptimalPrice,
                        SuggestedDiscount = pricingRecommendation.DiscountPercentage,
                        MinimumViablePrice = pricingRecommendation.MinimumPrice,
                        
                        // Financial projections
                        PotentialRevenue = financialImpact.PotentialRevenue,
                        EstimatedLoss = financialImpact.EstimatedLoss,
                        NetBenefit = financialImpact.NetBenefit,
                        
                        // Sales projections
                        EstimatedSellThroughDays = dailyVelocity > 0 ? (int)(batch.CurrentStock / dailyVelocity) : int.MaxValue,
                        RecommendedSalesChannels = DetermineOptimalSalesChannelsHelper(batch, daysToExpiry),
                        
                        // Action items
                        ImmediateActions = GenerateImmediateActionPlanHelper(batch, daysToExpiry, priorityScore),
                        Timeline = CalculateActionTimelineHelper(daysToExpiry, batch.CurrentStock, dailyVelocity)
                    };

                    recommendations.Add(recommendation);
                }

                // Sort by priority and return top recommendations
                return recommendations
                    .OrderByDescending(r => r.PriorityScore)
                    .ThenBy(r => r.DaysToExpiry)
                    .Take(100) // Limit to top 100
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating smart FIFO recommendations for branch {BranchId}", branchId);
                throw;
            }
        }

        // ==================== PRIVATE HELPER METHODS FOR ANALYTICS ==================== //

        private decimal CalculateWastePercentageHelper(List<ProductBatch> batches, DateTime currentDate)
        {
            var expiredBatches = batches.Where(b => b.ExpiryDate <= currentDate).ToList();
            var totalValue = batches.Sum(b => b.InitialStock * b.CostPerUnit);
            var expiredValue = expiredBatches.Sum(b => b.CurrentStock * b.CostPerUnit);
            
            return totalValue > 0 ? (expiredValue / totalValue) * 100 : 0;
        }

        private async Task<decimal> CalculateMonthlyExpiryRateHelper(int? branchId)
        {
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            
            var expiredLastMonth = await _context.ProductBatches
                .Where(pb => pb.ExpiryDate.HasValue && 
                           pb.ExpiryDate >= thirtyDaysAgo && 
                           pb.ExpiryDate <= DateTime.UtcNow &&
                           (branchId == null || pb.BranchId == branchId))
                .CountAsync();
                
            var totalBatches = await _context.ProductBatches
                .Where(pb => pb.ExpiryDate.HasValue && 
                           (branchId == null || pb.BranchId == branchId))
                .CountAsync();
                
            return totalBatches > 0 ? (decimal)expiredLastMonth / totalBatches * 100 : 0;
        }

        private decimal CalculatePreventionOpportunityHelper(List<ProductBatch> batches)
        {
            var nearExpiryBatches = batches
                .Where(b => b.ExpiryDate.HasValue && 
                          b.ExpiryDate <= DateTime.UtcNow.AddDays(30) &&
                          b.ExpiryDate > DateTime.UtcNow)
                .ToList();
                
            // Assume 70% of near-expiry value can be saved with proper action
            return nearExpiryBatches.Sum(b => b.CurrentStock * b.CostPerUnit) * 0.7m;
        }

        private List<CategoryExpiryPerformance> CalculateCategoryExpiryPerformanceHelper(List<ProductBatch> batches)
        {
            return batches
                .Where(b => b.Product?.Category != null)
                .GroupBy(b => b.Product!.Category!)
                .Select(g => new CategoryExpiryPerformance
                {
                    CategoryId = g.Key!.Id,
                    CategoryName = g.Key!.Name,
                    TotalBatches = g.Count(),
                    ExpiredBatches = g.Count(b => b.ExpiryDate <= DateTime.UtcNow),
                    NearExpiryBatches = g.Count(b => b.ExpiryDate <= DateTime.UtcNow.AddDays(7) && b.ExpiryDate > DateTime.UtcNow),
                    TotalValue = g.Sum(b => b.CurrentStock * b.CostPerUnit),
                    ExpiredValue = g.Where(b => b.ExpiryDate <= DateTime.UtcNow).Sum(b => b.CurrentStock * b.CostPerUnit),
                    WastePercentage = g.Sum(b => b.InitialStock * b.CostPerUnit) > 0 ? 
                        g.Where(b => b.ExpiryDate <= DateTime.UtcNow).Sum(b => b.CurrentStock * b.CostPerUnit) / 
                        g.Sum(b => b.InitialStock * b.CostPerUnit) * 100 : 0
                })
                .OrderByDescending(c => c.WastePercentage)
                .ToList();
        }

        private async Task<List<ExpiryTrendData>> CalculateExpiryTrendsHelper(int? branchId, int months)
        {
            var trends = new List<ExpiryTrendData>();
            var startDate = DateTime.UtcNow.AddMonths(-months);
            
            for (int i = 0; i < months; i++)
            {
                var monthStart = startDate.AddMonths(i);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                
                var monthlyExpired = await _context.ProductBatches
                    .Where(pb => pb.ExpiryDate.HasValue &&
                               pb.ExpiryDate >= monthStart &&
                               pb.ExpiryDate <= monthEnd &&
                               (branchId == null || pb.BranchId == branchId))
                    .ToListAsync();
                
                trends.Add(new ExpiryTrendData
                {
                    Period = monthStart.ToString("yyyy-MM"),
                    PeriodDisplay = monthStart.ToString("MMM yyyy"),
                    ExpiredBatches = monthlyExpired.Count,
                    ExpiredValue = monthlyExpired.Sum(b => b.CurrentStock * b.CostPerUnit),
                    WastagePercentage = 0 // Would need more complex calculation
                });
            }
            
            return trends;
        }

        private List<ExpiryActionRecommendation> GenerateExpiryActionRecommendationsHelper(List<ProductBatch> batches, DateTime currentDate)
        {
            var recommendations = new List<ExpiryActionRecommendation>();
            
            // Critical items (expire today/tomorrow)
            var criticalBatches = batches.Where(b => b.ExpiryDate <= currentDate.AddDays(1) && b.ExpiryDate > currentDate).ToList();
            if (criticalBatches.Any())
            {
                recommendations.Add(new ExpiryActionRecommendation
                {
                    Priority = "Critical",
                    ActionType = "Immediate Discount",
                    Description = $"Apply emergency discount to {criticalBatches.Count} batches expiring within 24 hours",
                    AffectedBatchesCount = criticalBatches.Count,
                    PotentialSavings = criticalBatches.Sum(b => b.CurrentStock * b.CostPerUnit) * 0.5m,
                    Deadline = currentDate.AddHours(8),
                    ActionItems = new List<string>
                    {
                        "Apply 30-50% discount immediately",
                        "Move products to prominent display location", 
                        "Create promotional bundles",
                        "Consider staff purchase program"
                    }
                });
            }
            
            // High priority items (expire within a week)
            var weekBatches = batches.Where(b => b.ExpiryDate <= currentDate.AddDays(7) && b.ExpiryDate > currentDate.AddDays(1)).ToList();
            if (weekBatches.Any())
            {
                recommendations.Add(new ExpiryActionRecommendation
                {
                    Priority = "High",
                    ActionType = "Promotional Campaign",
                    Description = $"Launch promotion for {weekBatches.Count} batches expiring this week",
                    AffectedBatchesCount = weekBatches.Count,
                    PotentialSavings = weekBatches.Sum(b => b.CurrentStock * b.CostPerUnit) * 0.3m,
                    Deadline = currentDate.AddDays(2),
                    ActionItems = new List<string>
                    {
                        "Apply 15-25% discount",
                        "Include in weekly promotional flyer",
                        "Train sales staff on promotion details",
                        "Monitor daily sales progress"
                    }
                });
            }
            
            return recommendations;
        }

        private async Task<decimal> ProjectFutureExpiryHelper(int? branchId, int days)
        {
            var futureDate = DateTime.UtcNow.AddDays(days);
            
            var futureBatches = await _context.ProductBatches
                .Where(pb => pb.ExpiryDate.HasValue &&
                           pb.ExpiryDate <= futureDate &&
                           pb.ExpiryDate > DateTime.UtcNow &&
                           pb.CurrentStock > 0 &&
                           (branchId == null || pb.BranchId == branchId))
                .ToListAsync();
                
            return futureBatches.Sum(b => b.CurrentStock * b.CostPerUnit);
        }

        private decimal CalculateEstimatedSavingsHelper(List<ProductBatch> batches, DateTime currentDate)
        {
            var nearExpiryBatches = batches
                .Where(b => b.ExpiryDate.HasValue && 
                          b.ExpiryDate <= currentDate.AddDays(30) &&
                          b.ExpiryDate > currentDate)
                .ToList();
                
            // Conservative estimate: 60% of value can be recovered with proper action
            return nearExpiryBatches.Sum(b => b.CurrentStock * b.CostPerUnit) * 0.6m;
        }

        private int CalculateSmartPriorityScoreHelper(ProductBatch batch, int daysToExpiry, decimal dailyVelocity)
        {
            var score = 0;
            
            // Expiry urgency (40% weight)
            if (daysToExpiry <= 1) score += 40;
            else if (daysToExpiry <= 3) score += 35;
            else if (daysToExpiry <= 7) score += 30;
            else if (daysToExpiry <= 14) score += 20;
            else if (daysToExpiry <= 30) score += 10;
            
            // Stock value impact (30% weight)
            var stockValue = batch.CurrentStock * batch.CostPerUnit;
            if (stockValue >= 5000000) score += 30; // 5M+ IDR
            else if (stockValue >= 1000000) score += 25; // 1M+ IDR
            else if (stockValue >= 500000) score += 20; // 500K+ IDR
            else if (stockValue >= 100000) score += 15; // 100K+ IDR
            else score += 5;
            
            // Sales velocity factor (20% weight)
            var sellThroughDays = dailyVelocity > 0 ? batch.CurrentStock / dailyVelocity : int.MaxValue;
            if (sellThroughDays > daysToExpiry * 2) score += 20; // High risk
            else if (sellThroughDays > daysToExpiry) score += 15; // Medium risk
            else score += 5; // Low risk
            
            // Category factor (10% weight)
            if (batch.Product?.Category?.RequiresExpiryDate == true) score += 10;
            else score += 5;
            
            return Math.Min(100, Math.Max(0, score));
        }

        private PricingRecommendation CalculateOptimalPricingHelper(Product product, int daysToExpiry, decimal dailyVelocity)
        {
            var currentPrice = product.SellPrice;
            var costPrice = product.BuyPrice;
            
            var discountPercentage = daysToExpiry switch
            {
                <= 1 => 40m, // 40% discount for items expiring today/tomorrow
                <= 3 => 25m, // 25% discount for critical items
                <= 7 => 15m, // 15% discount for items expiring this week
                <= 14 => 10m, // 10% discount for items expiring in 2 weeks
                _ => 0m
            };
            
            var optimalPrice = currentPrice * (1 - discountPercentage / 100);
            var minimumPrice = costPrice * 1.05m; // 5% markup minimum
            
            return new PricingRecommendation
            {
                OptimalPrice = Math.Max(optimalPrice, minimumPrice),
                DiscountPercentage = discountPercentage,
                MinimumPrice = minimumPrice
            };
        }

        private FinancialImpact CalculateFinancialImpactHelper(ProductBatch batch, PricingRecommendation pricing, int daysToExpiry)
        {
            var potentialRevenue = batch.CurrentStock * pricing.OptimalPrice;
            var costBasis = batch.CurrentStock * batch.CostPerUnit;
            var estimatedLoss = daysToExpiry <= 1 ? costBasis * 0.7m : costBasis * 0.3m; // Higher loss if very close to expiry
            
            return new FinancialImpact
            {
                PotentialRevenue = potentialRevenue,
                EstimatedLoss = estimatedLoss,
                NetBenefit = potentialRevenue - costBasis
            };
        }

        private string CalculateUrgencyLevelHelper(int priorityScore)
        {
            return priorityScore switch
            {
                >= 80 => "Critical",
                >= 60 => "High", 
                >= 40 => "Medium",
                _ => "Low"
            };
        }

        private string DetermineRecommendedActionHelper(int daysToExpiry, decimal dailyVelocity, int currentStock)
        {
            if (daysToExpiry <= 1) return "Emergency discount and promotion";
            if (daysToExpiry <= 3) return "Immediate promotional pricing";
            if (daysToExpiry <= 7) return "Weekly promotion campaign";
            if (dailyVelocity > 0 && currentStock / dailyVelocity > daysToExpiry) return "Increase marketing focus";
            return "Monitor and apply standard FIFO";
        }

        private List<string> DetermineOptimalSalesChannelsHelper(ProductBatch batch, int daysToExpiry)
        {
            var channels = new List<string> { "In-store" };
            
            if (daysToExpiry <= 3) 
            {
                channels.AddRange(new[] { "Staff sales", "Bundle deals", "Social media promotion" });
            }
            else if (daysToExpiry <= 7)
            {
                channels.AddRange(new[] { "Weekly specials", "Email marketing" });
            }
            
            return channels;
        }

        private List<string> GenerateImmediateActionPlanHelper(ProductBatch batch, int daysToExpiry, int priorityScore)
        {
            var actions = new List<string>();
            
            if (daysToExpiry <= 1)
            {
                actions.AddRange(new[]
                {
                    "Apply emergency discount (30-50%)",
                    "Move to high-visibility location",
                    "Create staff alert",
                    "Consider donation if unsold by end of day"
                });
            }
            else if (daysToExpiry <= 3)
            {
                actions.AddRange(new[]
                {
                    "Apply promotional pricing (20-30%)",
                    "Include in daily specials",
                    "Train staff on selling points"
                });
            }
            else if (priorityScore >= 60)
            {
                actions.AddRange(new[]
                {
                    "Monitor daily inventory levels",
                    "Include in upcoming promotions",
                    "Review supplier delivery timing"
                });
            }
            
            return actions;
        }

        private ActionTimeline CalculateActionTimelineHelper(int daysToExpiry, int currentStock, decimal dailyVelocity)
        {
            return new ActionTimeline
            {
                ImmediateAction = daysToExpiry <= 1 ? "Within 2 hours" : "Within 24 hours",
                ShortTerm = daysToExpiry <= 3 ? "Next 1-2 days" : "Next 3-5 days", 
                MediumTerm = "Next 1-2 weeks",
                ReviewDate = DateTime.UtcNow.AddDays(Math.Min(daysToExpiry / 2, 7))
            };
        }
    }

    // ==================== SUPPORTING DATA CLASSES ==================== //
    // DTOs moved to Berca_Backend.DTOs namespace to avoid conflicts
}
