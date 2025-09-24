using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using Berca_Backend.Data;
using Berca_Backend.DTOs;

namespace Berca_Backend.Controllers
{
    /// <summary>
    /// Reports Controller for generating various business reports
    /// Handles credit reports, inventory reports, and other analytics reports
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(AppDbContext context, IMemoryCache cache, ILogger<ReportsController> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// Get credit report for specific branch or all accessible branches
        /// </summary>
        [HttpGet("credit")]
        [Authorize(Policy = "Reports.Credit")]
        public async Task<IActionResult> GetCreditReport(
            [FromQuery] int? branchId = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                // ✅ CACHE ASIDE PATTERN: Check cache first
                var cacheKey = $"reports_credit_{branchId ?? 0}_{startDate?.ToString("yyyyMMdd") ?? "null"}_{endDate?.ToString("yyyyMMdd") ?? "null"}_{GetCurrentUserId()}";

                if (_cache.TryGetValue(cacheKey, out object? cachedReport))
                {
                    _logger.LogInformation("🔄 Cache HIT: Retrieved credit report from cache");
                    return Ok(cachedReport);
                }

                _logger.LogInformation("🔄 Cache MISS: Generating credit report from database");
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                if (currentUserId == 0)
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid user session"
                    });
                }

                // Get user's accessible branches
                var accessibleBranchIds = await GetUserAccessibleBranches(currentUserId, currentUserRole);

                // Validate branch access if specific branch requested
                if (branchId.HasValue && !accessibleBranchIds.Contains(branchId.Value))
                {
                    return StatusCode(403, new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Access denied to specified branch"
                    });
                }

                var filterBranchIds = branchId.HasValue ? new List<int> { branchId.Value } : accessibleBranchIds;

                if (!filterBranchIds.Any())
                {
                    return StatusCode(403, new ApiResponse<object>
                    {
                        Success = false,
                        Message = "No accessible branches found"
                    });
                }

                var end = endDate ?? DateTime.UtcNow.Date;
                var start = startDate ?? end.AddDays(-30);

                // Get credit transactions for the period
                var creditTransactions = await _context.MemberCreditTransactions
                    .Where(ct => ct.CreatedAt >= start && ct.CreatedAt <= end)
                    .Where(ct => filterBranchIds.Contains(ct.BranchId ?? 0))
                    .Include(ct => ct.Member)
                    .Include(ct => ct.Branch)
                    .ToListAsync();

                var creditReport = new
                {
                    reportPeriod = new { start, end },
                    branchId = branchId,
                    summary = new
                    {
                        totalCreditIssued = Math.Round(creditTransactions.Where(ct => ct.Amount > 0).Sum(ct => ct.Amount), 2),
                        totalPaymentsReceived = Math.Round(creditTransactions.Where(ct => ct.Amount < 0).Sum(ct => Math.Abs(ct.Amount)), 2),
                        totalTransactions = creditTransactions.Count,
                        totalMembers = creditTransactions.Select(ct => ct.MemberId).Distinct().Count()
                    },
                    transactionsByBranch = creditTransactions
                        .GroupBy(ct => new { ct.BranchId, BranchName = ct.Branch != null ? ct.Branch.BranchName : null })
                        .Select(g => new
                        {
                            branchId = g.Key.BranchId,
                            branchName = g.Key.BranchName,
                            creditIssued = Math.Round(g.Where(ct => ct.Amount > 0).Sum(ct => ct.Amount), 2),
                            paymentsReceived = Math.Round(g.Where(ct => ct.Amount < 0).Sum(ct => Math.Abs(ct.Amount)), 2),
                            transactionCount = g.Count(),
                            memberCount = g.Select(ct => ct.MemberId).Distinct().Count()
                        })
                        .ToList(),
                    recentTransactions = creditTransactions
                        .OrderByDescending(ct => ct.CreatedAt)
                        .Take(20)
                        .Select(ct => new
                        {
                            id = ct.Id,
                            memberName = ct.Member?.Name,
                            memberNumber = ct.Member?.MemberNumber,
                            branchName = ct.Branch?.BranchName,
                            transactionType = ct.Type,
                            amount = Math.Round(ct.Amount, 2),
                            description = ct.Description,
                            createdAt = ct.CreatedAt,
                            referenceNumber = ct.ReferenceNumber
                        })
                        .ToList()
                };

                // Prepare the response object
                var reportResponse = new ApiResponse<object>
                {
                    Success = true,
                    Data = creditReport,
                    Message = "Credit report generated successfully"
                };

                // ✅ CACHE ASIDE PATTERN: Update cache after database fetch
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(20), // Credit reports cache for 20 minutes
                    SlidingExpiration = TimeSpan.FromMinutes(5),
                    Priority = CacheItemPriority.Normal
                };

                _cache.Set(cacheKey, reportResponse, cacheOptions);
                _logger.LogInformation("💾 Cache UPDATED: Stored credit report in cache");

                return Ok(reportResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating credit report for branch {BranchId}", branchId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get inventory report for specific branch or all accessible branches
        /// </summary>
        [HttpGet("inventory")]
        [Authorize(Policy = "Reports.Inventory")]
        public async Task<IActionResult> GetInventoryReport(
            [FromQuery] int? branchId = null,
            [FromQuery] bool includeZeroStock = false,
            [FromQuery] int? categoryId = null)
        {
            try
            {
                // ✅ CACHE ASIDE PATTERN: Check cache first
                var cacheKey = $"reports_inventory_{branchId ?? 0}_{includeZeroStock}_{categoryId ?? 0}_{GetCurrentUserId()}";

                if (_cache.TryGetValue(cacheKey, out object? cachedInventoryReport))
                {
                    _logger.LogInformation("🔄 Cache HIT: Retrieved inventory report from cache");
                    return Ok(cachedInventoryReport);
                }

                _logger.LogInformation("🔄 Cache MISS: Generating inventory report from database");
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                if (currentUserId == 0)
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid user session"
                    });
                }

                var accessibleBranchIds = await GetUserAccessibleBranches(currentUserId, currentUserRole);

                if (branchId.HasValue && !accessibleBranchIds.Contains(branchId.Value))
                {
                    return StatusCode(403, new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Access denied to specified branch"
                    });
                }

                var filterBranchIds = branchId.HasValue ? new List<int> { branchId.Value } : accessibleBranchIds;

                if (!filterBranchIds.Any())
                {
                    return StatusCode(403, new ApiResponse<object>
                    {
                        Success = false,
                        Message = "No accessible branches found"
                    });
                }

                var inventoryQuery = _context.ProductBatches
                    .Where(pb => filterBranchIds.Contains(pb.BranchId ?? 0))
                    .Include(pb => pb.Product!)
                    .ThenInclude(p => p!.Category)
                    .Include(pb => pb.Branch)
                    .AsQueryable();

                if (!includeZeroStock)
                {
                    inventoryQuery = inventoryQuery.Where(pb => pb.CurrentStock > 0);
                }

                if (categoryId.HasValue)
                {
                    inventoryQuery = inventoryQuery.Where(pb => pb.Product != null && pb.Product.CategoryId == categoryId.Value);
                }

                var inventoryData = await inventoryQuery.ToListAsync();

                var inventoryReport = new
                {
                    branchId = branchId,
                    includeZeroStock = includeZeroStock,
                    categoryId = categoryId,
                    summary = new
                    {
                        totalProducts = inventoryData.Select(pb => pb.ProductId).Distinct().Count(),
                        totalStock = inventoryData.Sum(pb => pb.CurrentStock),
                        totalValue = Math.Round(inventoryData.Sum(pb => pb.CurrentStock * (pb.Product?.SellPrice ?? 0)), 2),
                        lowStockItems = inventoryData.Count(pb => pb.CurrentStock <= (pb.Product?.MinimumStock ?? 0)),
                        outOfStockItems = inventoryData.Count(pb => pb.CurrentStock == 0),
                        branches = inventoryData.Select(pb => pb.BranchId).Distinct().Count()
                    },
                    inventoryByBranch = inventoryData
                        .GroupBy(pb => new { pb.BranchId, BranchName = pb.Branch != null ? pb.Branch.BranchName : null })
                        .Select(g => new
                        {
                            branchId = g.Key.BranchId,
                            branchName = g.Key.BranchName,
                            totalProducts = g.Select(pb => pb.ProductId).Distinct().Count(),
                            totalStock = g.Sum(pb => pb.CurrentStock),
                            totalValue = Math.Round(g.Sum(pb => pb.CurrentStock * (pb.Product?.SellPrice ?? 0)), 2),
                            lowStockItems = g.Count(pb => pb.CurrentStock <= (pb.Product?.MinimumStock ?? 0)),
                            outOfStockItems = g.Count(pb => pb.CurrentStock == 0)
                        })
                        .OrderBy(g => g.branchName)
                        .ToList(),
                    inventoryByCategory = inventoryData
                        .GroupBy(pb => new { CategoryId = pb.Product?.CategoryId ?? 0, CategoryName = pb.Product?.Category?.Name })
                        .Select(g => new
                        {
                            categoryId = g.Key.CategoryId,
                            categoryName = g.Key.CategoryName,
                            totalProducts = g.Select(pb => pb.ProductId).Distinct().Count(),
                            totalStock = g.Sum(pb => pb.CurrentStock),
                            totalValue = Math.Round(g.Sum(pb => pb.CurrentStock * (pb.Product?.SellPrice ?? 0)), 2),
                            lowStockItems = g.Count(pb => pb.CurrentStock <= (pb.Product?.MinimumStock ?? 0)),
                            outOfStockItems = g.Count(pb => pb.CurrentStock == 0)
                        })
                        .OrderBy(g => g.categoryName)
                        .ToList(),
                    lowStockAlert = inventoryData
                        .Where(pb => pb.CurrentStock <= (pb.Product?.MinimumStock ?? 0))
                        .Select(pb => new
                        {
                            productId = pb.ProductId,
                            productName = pb.Product?.Name,
                            branchId = pb.BranchId,
                            branchName = pb.Branch?.BranchName,
                            categoryName = pb.Product?.Category?.Name,
                            batchNumber = pb.BatchNumber,
                            currentStock = pb.CurrentStock,
                            minimumStock = pb.Product?.MinimumStock ?? 0,
                            shortage = (pb.Product?.MinimumStock ?? 0) - pb.CurrentStock,
                            lastUpdated = pb.UpdatedAt
                        })
                        .OrderBy(i => i.shortage)
                        .ToList()
                };

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = inventoryReport,
                    Message = "Inventory report generated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating inventory report for branch {BranchId}", branchId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        #region Helper Methods

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value ?? 
                             User.FindFirst("sub")?.Value ?? 
                             User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        private string GetCurrentUserRole()
        {
            return User.FindFirst("Role")?.Value ?? 
                   User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value ?? 
                   User.FindFirst(ClaimTypes.Role)?.Value ??
                   "User";
        }

        private async Task<List<int>> GetUserAccessibleBranches(int userId, string userRole)
        {
            var accessibleBranches = new List<int>();

            if (userRole.ToUpper() is "ADMIN" or "HEADMANAGER")
            {
                // Admin and HeadManager can access all branches
                accessibleBranches = await _context.Branches
                    .Where(b => b.IsActive)
                    .Select(b => b.Id)
                    .ToListAsync();
            }
            else
            {
                try
                {
                    // Get user's accessible branches via BranchAccess table
                    accessibleBranches = await _context.BranchAccesses
                        .Where(ba => ba.UserId == userId && ba.IsActive && ba.CanRead)
                        .Select(ba => ba.BranchId)
                        .ToListAsync();
                }
                catch
                {
                    // Fallback: Use user's assigned branch
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                    if (user?.BranchId.HasValue == true)
                    {
                        accessibleBranches.Add(user.BranchId.Value);
                    }
                }
            }

            return accessibleBranches;
        }

        #endregion
    }
}
