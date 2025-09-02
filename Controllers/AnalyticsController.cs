using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Berca_Backend.Data;
using Berca_Backend.Models;

namespace Berca_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AnalyticsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AnalyticsController> _logger;

        public AnalyticsController(AppDbContext context, ILogger<AnalyticsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get multi-branch dashboard analytics (Required by frontend integration)
        /// </summary>
        [HttpGet("dashboard")]
        [Authorize(Policy = "Analytics.Read")]
        public async Task<IActionResult> GetDashboardAnalytics([FromQuery] string? branchIds = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                if (currentUserId == 0)
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Invalid user session"
                    });
                }

                // Parse and validate branch IDs
                var requestedBranchIds = new List<int>();
                if (!string.IsNullOrEmpty(branchIds))
                {
                    requestedBranchIds = branchIds.Split(',')
                        .Where(id => int.TryParse(id.Trim(), out _))
                        .Select(id => int.Parse(id.Trim()))
                        .ToList();
                }

                // Get user's accessible branches
                var accessibleBranchIds = await GetUserAccessibleBranches(currentUserId, currentUserRole);
                
                // Filter requested branches by user access
                if (requestedBranchIds.Any())
                {
                    requestedBranchIds = requestedBranchIds.Intersect(accessibleBranchIds).ToList();
                }
                else
                {
                    requestedBranchIds = accessibleBranchIds;
                }

                if (!requestedBranchIds.Any())
                {
                    return StatusCode(403, new
                    {
                        success = false,
                        message = "No accessible branches found"
                    });
                }

                // Get date ranges for analytics (last 30 days and previous period for comparison)
                var endDate = DateTime.UtcNow.Date;
                var startDate = endDate.AddDays(-30);
                var previousStartDate = startDate.AddDays(-30);

                // Calculate overall metrics
                var currentPeriodSales = await _context.Sales
                    .Where(s => s.CreatedAt >= startDate && s.CreatedAt <= endDate)
                    .Where(s => requestedBranchIds.Contains(s.Cashier.BranchId ?? 0))
                    .ToListAsync();

                var previousPeriodSales = await _context.Sales
                    .Where(s => s.CreatedAt >= previousStartDate && s.CreatedAt < startDate)
                    .Where(s => requestedBranchIds.Contains(s.Cashier.BranchId ?? 0))
                    .ToListAsync();

                var totalSales = currentPeriodSales.Sum(s => s.Total);
                var totalTransactions = currentPeriodSales.Count;
                var averageTransaction = totalTransactions > 0 ? totalSales / totalTransactions : 0;

                var previousTotalSales = previousPeriodSales.Sum(s => s.Total);
                var salesGrowth = previousTotalSales > 0 
                    ? ((totalSales - previousTotalSales) / previousTotalSales) * 100 
                    : 0;

                // Get active transfers
                var activeTransfers = await _context.TransferRequests
                    .Where(tr => requestedBranchIds.Contains(tr.SourceBranchId) || requestedBranchIds.Contains(tr.TargetBranchId))
                    .Where(tr => tr.Status == TransferStatus.Pending || tr.Status == TransferStatus.Approved)
                    .CountAsync();

                // Get pending approvals
                var pendingApprovals = await _context.TransferRequests
                    .Where(tr => requestedBranchIds.Contains(tr.SourceBranchId) || requestedBranchIds.Contains(tr.TargetBranchId))
                    .Where(tr => tr.Status == TransferStatus.Pending)
                    .CountAsync();

                // Calculate branch performance
                var branchPerformanceList = new List<object>();
                var branches = await _context.Branches
                    .Where(b => requestedBranchIds.Contains(b.Id) && b.IsActive)
                    .ToListAsync();

                foreach (var branch in branches)
                {
                    var branchSales = currentPeriodSales
                        .Where(s => s.Cashier?.BranchId == branch.Id)
                        .ToList();

                    var branchTotalSales = branchSales.Sum(s => s.Total);
                    var branchTransactionCount = branchSales.Count;
                    var branchAvgTransaction = branchTransactionCount > 0 ? branchTotalSales / branchTransactionCount : 0;

                    var branchPreviousSales = previousPeriodSales
                        .Where(s => s.Cashier?.BranchId == branch.Id)
                        .Sum(s => s.Total);

                    var branchGrowth = branchPreviousSales > 0 
                        ? ((branchTotalSales - branchPreviousSales) / branchPreviousSales) * 100 
                        : 0;

                    var status = branchGrowth >= 20 ? "excellent" : branchGrowth >= 10 ? "good" : branchGrowth >= 0 ? "stable" : "declining";
                    var statusText = branchGrowth >= 20 ? "Excellent" : branchGrowth >= 10 ? "Good" : branchGrowth >= 0 ? "Stable" : "Declining";

                    branchPerformanceList.Add(new
                    {
                        branchId = branch.Id,
                        branchName = branch.BranchName,
                        totalSales = branchTotalSales,
                        transactionCount = branchTransactionCount,
                        avgTransaction = branchAvgTransaction,
                        growth = Math.Round(branchGrowth, 1),
                        rank = 1, // Will be updated after sorting
                        status = status,
                        statusText = statusText,
                        isHeadOffice = branch.BranchType == BranchType.Head
                    });
                }

                // Sort branch performance by sales and update rank
                var sortedBranchPerformance = branchPerformanceList
                    .OrderByDescending(bp => ((dynamic)bp).totalSales)
                    .Select((bp, index) => 
                    {
                        var item = (dynamic)bp;
                        return new
                        {
                            branchId = item.branchId,
                            branchName = item.branchName,
                            totalSales = item.totalSales,
                            transactionCount = item.transactionCount,
                            avgTransaction = item.avgTransaction,
                            growth = item.growth,
                            rank = index + 1,
                            status = item.status,
                            statusText = item.statusText,
                            isHeadOffice = item.isHeadOffice
                        };
                    })
                    .ToList();

                // Generate stock alerts (simplified - in real implementation you'd have more complex logic)
                var stockAlertsList = new List<object>();
                var lowStockProducts = await _context.Products
                    .Where(p => p.Stock <= p.MinimumStock && p.IsActive)
                    .Take(10) // Limit to 10 alerts
                    .ToListAsync();

                foreach (var product in lowStockProducts)
                {
                    var branchStocks = new List<object>();
                    foreach (var branch in branches)
                    {
                        // In a real implementation, you'd have branch-specific stock
                        var stockLevel = branch.Id == 1 ? product.Stock : Math.Max(0, product.Stock - 20);
                        if (stockLevel <= product.MinimumStock)
                        {
                            branchStocks.Add(new
                            {
                                branchId = branch.Id,
                                branchName = branch.BranchName,
                                level = stockLevel,
                                minLevel = product.MinimumStock
                            });
                        }
                    }

                    if (branchStocks.Any())
                    {
                        stockAlertsList.Add(new
                        {
                            id = product.Id,
                            productId = product.Id,
                            productName = product.Name,
                            type = "low_stock",
                            severity = product.Stock <= (product.MinimumStock * 0.5) ? "high" : "medium",
                            branchStocks = branchStocks
                        });
                    }
                }

                var stockAlertsCount = stockAlertsList.Count;

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        totalSales = Math.Round(totalSales, 2),
                        totalTransactions = totalTransactions,
                        averageTransaction = Math.Round(averageTransaction, 2),
                        salesGrowth = Math.Round(salesGrowth, 1),
                        activeTransfers = activeTransfers,
                        pendingApprovals = pendingApprovals,
                        stockAlerts = stockAlertsCount,
                        branchPerformance = sortedBranchPerformance,
                        stockAlertsList = stockAlertsList
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboard analytics");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Dismiss stock alert (Required by frontend integration)
        /// </summary>
        [HttpPost("alerts/{id}/dismiss")]
        [Authorize(Policy = "Analytics.Write")]
        public IActionResult DismissAlert(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                // In a real implementation, you'd have an Alerts table to track dismissed alerts
                // For now, just log the action
                _logger.LogInformation("Alert {AlertId} dismissed by user {UserId}", id, currentUserId);

                return Ok(new
                {
                    success = true,
                    message = "Alert dismissed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dismissing alert {AlertId}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error"
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