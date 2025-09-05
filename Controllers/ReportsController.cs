using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(AppDbContext context, ILogger<ReportsController> logger)
        {
            _context = context;
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
                var creditTransactions = await _context.CreditTransactions
                    .Where(ct => ct.CreatedAt >= start && ct.CreatedAt <= end)
                    .Where(ct => filterBranchIds.Contains(ct.Member.BranchId ?? 0))
                    .Include(ct => ct.Member)
                    .Include(ct => ct.Member.Branch)
                    .ToListAsync();

                var creditReport = new
                {
                    reportPeriod = new { start, end },
                    branchId = branchId,
                    summary = new
                    {
                        totalCreditIssued = Math.Round(creditTransactions.Where(ct => ct.TransactionType == "Credit").Sum(ct => ct.Amount), 2),
                        totalPaymentsReceived = Math.Round(creditTransactions.Where(ct => ct.TransactionType == "Payment").Sum(ct => ct.Amount), 2),
                        totalTransactions = creditTransactions.Count,
                        totalMembers = creditTransactions.Select(ct => ct.MemberId).Distinct().Count()
                    },
                    transactionsByBranch = creditTransactions
                        .GroupBy(ct => new { ct.Member.BranchId, ct.Member.Branch.BranchName })
                        .Select(g => new
                        {
                            branchId = g.Key.BranchId,
                            branchName = g.Key.BranchName,
                            creditIssued = Math.Round(g.Where(ct => ct.TransactionType == "Credit").Sum(ct => ct.Amount), 2),
                            paymentsReceived = Math.Round(g.Where(ct => ct.TransactionType == "Payment").Sum(ct => ct.Amount), 2),
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
                            memberName = ct.Member.Name,
                            memberNumber = ct.Member.MemberNumber,
                            branchName = ct.Member.Branch?.BranchName,
                            transactionType = ct.TransactionType,
                            amount = Math.Round(ct.Amount, 2),
                            description = ct.Description,
                            createdAt = ct.CreatedAt,
                            referenceNumber = ct.ReferenceNumber
                        })
                        .ToList()
                };

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = creditReport,
                    Message = "Credit report generated successfully"
                });
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

                var inventoryQuery = _context.Inventories
                    .Where(i => filterBranchIds.Contains(i.BranchId))
                    .Include(i => i.Product)
                    .Include(i => i.Product.Category)
                    .Include(i => i.Branch)
                    .AsQueryable();

                if (!includeZeroStock)
                {
                    inventoryQuery = inventoryQuery.Where(i => i.Stock > 0);
                }

                if (categoryId.HasValue)
                {
                    inventoryQuery = inventoryQuery.Where(i => i.Product.CategoryId == categoryId.Value);
                }

                var inventoryData = await inventoryQuery.ToListAsync();

                var inventoryReport = new
                {
                    branchId = branchId,
                    includeZeroStock = includeZeroStock,
                    categoryId = categoryId,
                    summary = new
                    {
                        totalProducts = inventoryData.Count,
                        totalStock = inventoryData.Sum(i => i.Stock),
                        totalValue = Math.Round(inventoryData.Sum(i => i.Stock * i.Product.SellingPrice), 2),
                        lowStockItems = inventoryData.Count(i => i.Stock <= i.Product.MinimumStock),
                        outOfStockItems = inventoryData.Count(i => i.Stock == 0),
                        branches = inventoryData.Select(i => i.Branch.BranchName).Distinct().Count()
                    },
                    inventoryByBranch = inventoryData
                        .GroupBy(i => new { i.BranchId, i.Branch.BranchName })
                        .Select(g => new
                        {
                            branchId = g.Key.BranchId,
                            branchName = g.Key.BranchName,
                            totalProducts = g.Count(),
                            totalStock = g.Sum(i => i.Stock),
                            totalValue = Math.Round(g.Sum(i => i.Stock * i.Product.SellingPrice), 2),
                            lowStockItems = g.Count(i => i.Stock <= i.Product.MinimumStock),
                            outOfStockItems = g.Count(i => i.Stock == 0)
                        })
                        .OrderBy(g => g.branchName)
                        .ToList(),
                    inventoryByCategory = inventoryData
                        .GroupBy(i => new { i.Product.CategoryId, i.Product.Category.Name })
                        .Select(g => new
                        {
                            categoryId = g.Key.CategoryId,
                            categoryName = g.Key.Name,
                            totalProducts = g.Count(),
                            totalStock = g.Sum(i => i.Stock),
                            totalValue = Math.Round(g.Sum(i => i.Stock * i.Product.SellingPrice), 2)
                        })
                        .OrderBy(g => g.categoryName)
                        .ToList(),
                    lowStockAlert = inventoryData
                        .Where(i => i.Stock <= i.Product.MinimumStock)
                        .Select(i => new
                        {
                            productId = i.ProductId,
                            productName = i.Product.Name,
                            branchId = i.BranchId,
                            branchName = i.Branch.BranchName,
                            currentStock = i.Stock,
                            minimumStock = i.Product.MinimumStock,
                            shortage = i.Product.MinimumStock - i.Stock,
                            lastUpdated = i.LastUpdated
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