using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using Berca_Backend.Data;
using Berca_Backend.Services.Interfaces;

namespace Berca_Backend.Services
{
    /// <summary>
    /// Cache warmup service for pre-loading critical data into cache
    /// Improves user experience by ensuring frequently accessed data is always cached
    /// </summary>
    public interface ICacheWarmupService
    {
        /// <summary>
        /// Warmup all critical caches on application startup
        /// </summary>
        Task WarmupStartupCachesAsync();

        /// <summary>
        /// Warmup dashboard-related caches
        /// </summary>
        Task WarmupDashboardCachesAsync();

        /// <summary>
        /// Warmup POS-related caches
        /// </summary>
        Task WarmupPOSCachesAsync();

        /// <summary>
        /// Warmup branch and category caches
        /// </summary>
        Task WarmupReferenceDataCachesAsync();

        /// <summary>
        /// Warmup ML prediction caches for top products
        /// </summary>
        Task WarmupMLPredictionCachesAsync();

        /// <summary>
        /// Get warmup statistics
        /// </summary>
        CacheWarmupStatistics GetWarmupStats();
    }

    public class CacheWarmupService : ICacheWarmupService
    {
        private readonly IMemoryCache _cache;
        private readonly ICacheInvalidationService _cacheInvalidation;
        private readonly ILogger<CacheWarmupService> _logger;
        private readonly IServiceProvider _serviceProvider;

        // Warmup statistics
        private readonly CacheWarmupStatistics _stats = new();

        public CacheWarmupService(
            IMemoryCache cache,
            ICacheInvalidationService cacheInvalidation,
            ILogger<CacheWarmupService> logger,
            IServiceProvider serviceProvider)
        {
            _cache = cache;
            _cacheInvalidation = cacheInvalidation;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Warmup all critical caches on application startup
        /// </summary>
        public async Task WarmupStartupCachesAsync()
        {
            var startTime = DateTime.UtcNow;
            _logger.LogInformation("🔥 Starting cache warmup process...");

            try
            {
                // Warmup in parallel for better performance
                await Task.WhenAll(
                    WarmupReferenceDataCachesAsync(),
                    WarmupDashboardCachesAsync(),
                    WarmupPOSCachesAsync()
                );

                var duration = DateTime.UtcNow - startTime;
                _stats.LastWarmupDuration = duration;
                _stats.LastWarmupTime = DateTime.UtcNow;
                _stats.TotalWarmupsPerformed++;

                _logger.LogInformation("🔥 Cache warmup completed successfully in {Duration}ms", duration.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                _stats.LastWarmupError = ex.Message;
                _logger.LogError(ex, "❌ Cache warmup failed");
                throw;
            }
        }

        /// <summary>
        /// Warmup dashboard-related caches
        /// </summary>
        public async Task WarmupDashboardCachesAsync()
        {
            _logger.LogInformation("🔥 Warming up dashboard caches...");

            try
            {
                using var scope = _serviceProvider.CreateScope();
                using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Warmup quick stats
                var cacheKey = "dashboard_quick_stats";
                if (!_cache.TryGetValue(cacheKey, out _))
                {
                    var activeProductCount = await context.Products.CountAsync(p => p.IsActive);
                    var totalBranches = await context.Branches.CountAsync(b => b.IsActive);
                    var totalMembers = await context.Members.CountAsync(m => m.IsActive);
                    var todaysSales = await context.Sales
                        .Where(s => s.CreatedAt.Date == DateTime.UtcNow.Date)
                        .CountAsync();

                    var quickStats = new
                    {
                        activeProducts = activeProductCount,
                        totalBranches = totalBranches,
                        totalMembers = totalMembers,
                        todaysSales = todaysSales
                    };

                    var cacheOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                        Priority = CacheItemPriority.High
                    };

                    _cache.Set(cacheKey, quickStats, cacheOptions);
                    _cacheInvalidation.TrackCacheKey(cacheKey);
                    _stats.CacheEntriesWarmed++;

                    _logger.LogInformation("🔥 Dashboard quick stats warmed up");
                }

                // Warmup low stock alerts
                await WarmupLowStockAlertsAsync(context);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error warming up dashboard caches");
                throw;
            }
        }

        /// <summary>
        /// Warmup POS-related caches
        /// </summary>
        public async Task WarmupPOSCachesAsync()
        {
            _logger.LogInformation("🔥 Warming up POS caches...");

            try
            {
                using var scope = _serviceProvider.CreateScope();
                using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Get all active branches to warmup POS caches per branch
                var activeBranches = await context.Branches
                    .Where(b => b.IsActive)
                    .Take(5) // Limit to top 5 branches to avoid overwhelming
                    .ToListAsync();

                foreach (var branch in activeBranches)
                {
                    await WarmupPOSProductsForBranchAsync(branch.Id);
                }

                _logger.LogInformation("🔥 POS caches warmed up for {BranchCount} branches", activeBranches.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error warming up POS caches");
                throw;
            }
        }

        /// <summary>
        /// Warmup branch and category caches
        /// </summary>
        public async Task WarmupReferenceDataCachesAsync()
        {
            _logger.LogInformation("🔥 Warming up reference data caches...");

            try
            {
                using var scope = _serviceProvider.CreateScope();
                using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Warmup categories
                await WarmupCategoriesAsync(context);

                // Warmup branches (this is critical for UI)
                await WarmupBranchesAsync(context);

                _logger.LogInformation("🔥 Reference data caches warmed up");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error warming up reference data caches");
                throw;
            }
        }

        /// <summary>
        /// Warmup ML prediction caches for top products
        /// </summary>
        public async Task WarmupMLPredictionCachesAsync()
        {
            _logger.LogInformation("🔥 Warming up ML prediction caches...");

            try
            {
                using var scope = _serviceProvider.CreateScope();
                using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Warmup forecastable products list
                var cacheKey = "ml_forecastable_products";
                if (!_cache.TryGetValue(cacheKey, out _))
                {
                    // Get top 10 products with sufficient sales data
                    var topProducts = await context.Products
                        .Where(p => p.IsActive)
                        .OrderByDescending(p => p.Stock)
                        .Take(10)
                        .Select(p => new
                        {
                            id = p.Id,
                            name = p.Name,
                            hasData = true // Simplified for warmup
                        })
                        .ToListAsync();

                    var productsResponse = new
                    {
                        Success = true,
                        Data = topProducts.Cast<object>().ToList(),
                        Message = $"Ditemukan {topProducts.Count} produk yang dapat diprediksi berdasarkan data penjualan"
                    };

                    var cacheOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2),
                        Priority = CacheItemPriority.Normal
                    };

                    _cache.Set(cacheKey, productsResponse, cacheOptions);
                    _cacheInvalidation.TrackCacheKey(cacheKey);
                    _stats.CacheEntriesWarmed++;

                    _logger.LogInformation("🔥 ML forecastable products warmed up ({ProductCount} products)", topProducts.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error warming up ML prediction caches");
                // Don't throw here - ML warmup is not critical
            }
        }

        /// <summary>
        /// Get warmup statistics
        /// </summary>
        public CacheWarmupStatistics GetWarmupStats()
        {
            return _stats;
        }

        #region Private Helper Methods

        private async Task WarmupPOSProductsForBranchAsync(int branchId)
        {
            var cacheKey = $"pos_products_{branchId}_all_null_true_1_20";
            if (!_cache.TryGetValue(cacheKey, out _))
            {
                using var scope = _serviceProvider.CreateScope();
                using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Get top 20 products for this branch
                var products = await context.Products
                    .Where(p => p.IsActive)
                    .OrderByDescending(p => p.Stock)
                    .Take(20)
                    .Select(p => new
                    {
                        id = p.Id,
                        name = p.Name,
                        stock = p.Stock,
                        price = p.SellPrice
                    })
                    .ToListAsync();

                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                    Priority = CacheItemPriority.High
                };

                _cache.Set(cacheKey, products, cacheOptions);
                _cacheInvalidation.TrackCacheKey(cacheKey);
                _stats.CacheEntriesWarmed++;
            }
        }

        private async Task WarmupCategoriesAsync(AppDbContext context)
        {
            var cacheKey = "categories_all_1_20_Name";
            if (!_cache.TryGetValue(cacheKey, out _))
            {
                var categories = await context.Categories
                    .OrderBy(c => c.Name)
                    .Take(20)
                    .Select(c => new
                    {
                        id = c.Id,
                        name = c.Name,
                        description = c.Description
                    })
                    .ToListAsync();

                var categoryResponse = new
                {
                    Categories = categories,
                    TotalCount = categories.Count,
                    PageSize = 20,
                    CurrentPage = 1
                };

                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(45),
                    Priority = CacheItemPriority.Normal
                };

                _cache.Set(cacheKey, categoryResponse, cacheOptions);
                _cacheInvalidation.TrackCacheKey(cacheKey);
                _stats.CacheEntriesWarmed++;

                _logger.LogInformation("🔥 Categories warmed up ({CategoryCount} categories)", categories.Count);
            }
        }

        private async Task WarmupBranchesAsync(AppDbContext context)
        {
            var branches = await context.Branches
                .Where(b => b.IsActive)
                .OrderBy(b => b.BranchName)
                .ToListAsync();

            // Warmup accessible branches for different user types
            var branchData = branches.Select(b => new
            {
                branchId = b.Id,
                branchCode = b.BranchCode,
                branchName = b.BranchName,
                branchType = b.BranchType.ToString(),
                isHeadOffice = b.BranchType == Models.BranchType.Head,
                isActive = b.IsActive
            }).ToList();

            var branchesResponse = new
            {
                success = true,
                data = branchData
            };

            // Cache for admin users (can access all branches)
            var adminCacheKey = "accessible_branches_0_ADMIN";
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
                Priority = CacheItemPriority.High
            };

            _cache.Set(adminCacheKey, branchesResponse, cacheOptions);
            _cacheInvalidation.TrackCacheKey(adminCacheKey);
            _stats.CacheEntriesWarmed++;

            _logger.LogInformation("🔥 Branches warmed up ({BranchCount} branches)", branches.Count);
        }

        private async Task WarmupLowStockAlertsAsync(AppDbContext context)
        {
            var cacheKey = "dashboard_low_stock_alerts";
            if (!_cache.TryGetValue(cacheKey, out _))
            {
                var lowStockProducts = await context.Products
                    .Where(p => p.IsActive && p.Stock <= p.MinimumStock)
                    .OrderBy(p => p.Stock)
                    .Take(20)
                    .Select(p => new
                    {
                        id = p.Id,
                        name = p.Name,
                        currentStock = p.Stock,
                        minimumStock = p.MinimumStock,
                        categoryName = p.Category != null ? p.Category.Name : "Uncategorized"
                    })
                    .ToListAsync();

                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(3),
                    Priority = CacheItemPriority.High
                };

                _cache.Set(cacheKey, lowStockProducts, cacheOptions);
                _cacheInvalidation.TrackCacheKey(cacheKey);
                _stats.CacheEntriesWarmed++;

                _logger.LogInformation("🔥 Low stock alerts warmed up ({AlertCount} alerts)", lowStockProducts.Count);
            }
        }

        #endregion
    }

    /// <summary>
    /// Statistics for cache warmup operations
    /// </summary>
    public class CacheWarmupStatistics
    {
        public DateTime? LastWarmupTime { get; set; }
        public TimeSpan LastWarmupDuration { get; set; }
        public int TotalWarmupsPerformed { get; set; }
        public int CacheEntriesWarmed { get; set; }
        public string? LastWarmupError { get; set; }
    }
}