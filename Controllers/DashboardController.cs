// Controllers/DashboardController.cs - Sprint 2 Dashboard API Controller (FIXED)
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Berca_Backend.DTOs;
using Berca_Backend.Services;

namespace Berca_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "Dashboard.Read")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        /// <summary>
        /// Get dashboard KPI overview
        /// </summary>
        [HttpGet("kpis")]
        public async Task<ActionResult<ApiResponse<DashboardKPIDto>>> GetDashboardKPIs(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var kpis = await _dashboardService.GetDashboardKPIsAsync(startDate, endDate);
                return Ok(new ApiResponse<DashboardKPIDto>
                {
                    Success = true,
                    Message = "Dashboard KPIs retrieved successfully",
                    Data = kpis
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard KPIs");
                return StatusCode(500, new ApiResponse<DashboardKPIDto>
                {
                    Success = false,
                    Message = "Failed to retrieve dashboard KPIs"
                });
            }
        }

        /// <summary>
        /// Get quick stats for dashboard cards
        /// </summary>
        [HttpGet("quick-stats")]
        public async Task<ActionResult<ApiResponse<QuickStatsDto>>> GetQuickStats()
        {
            try
            {
                var quickStats = await _dashboardService.GetQuickStatsAsync();
                return Ok(new ApiResponse<QuickStatsDto>
                {
                    Success = true,
                    Message = "Quick stats retrieved successfully",
                    Data = quickStats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quick stats");
                return StatusCode(500, new ApiResponse<QuickStatsDto>
                {
                    Success = false,
                    Message = "Failed to retrieve quick stats"
                });
            }
        }

        /// <summary>
        /// Get sales chart data with different periods (daily/weekly/monthly)
        /// </summary>
        [HttpGet("charts/sales")]
        public async Task<ActionResult<ApiResponse<List<ChartDataDto>>>> GetSalesChartData(
            [FromQuery] string period = "daily",
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var chartData = await _dashboardService.GetSalesChartDataAsync(period, startDate, endDate);
                return Ok(new ApiResponse<List<ChartDataDto>>
                {
                    Success = true,
                    Message = $"Sales chart data ({period}) retrieved successfully",
                    Data = chartData
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse<List<ChartDataDto>>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales chart data");
                return StatusCode(500, new ApiResponse<List<ChartDataDto>>
                {
                    Success = false,
                    Message = "Failed to retrieve sales chart data"
                });
            }
        }

        /// <summary>
        /// Get revenue chart data
        /// </summary>
        [HttpGet("charts/revenue")]
        public async Task<ActionResult<ApiResponse<List<ChartDataDto>>>> GetRevenueChartData(
            [FromQuery] string period = "monthly",
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var chartData = await _dashboardService.GetRevenueChartDataAsync(period, startDate, endDate);
                return Ok(new ApiResponse<List<ChartDataDto>>
                {
                    Success = true,
                    Message = $"Revenue chart data ({period}) retrieved successfully",
                    Data = chartData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting revenue chart data");
                return StatusCode(500, new ApiResponse<List<ChartDataDto>>
                {
                    Success = false,
                    Message = "Failed to retrieve revenue chart data"
                });
            }
        }

        /// <summary>
        /// Get top selling products
        /// </summary>
        [HttpGet("products/top-selling")]
        public async Task<ActionResult<ApiResponse<List<TopProductDto>>>> GetTopSellingProducts(
            [FromQuery] int count = 10,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var topProducts = await _dashboardService.GetTopSellingProductsAsync(count, startDate, endDate);
                return Ok(new ApiResponse<List<TopProductDto>>
                {
                    Success = true,
                    Message = "Top selling products retrieved successfully",
                    Data = topProducts
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top selling products");
                return StatusCode(500, new ApiResponse<List<TopProductDto>>
                {
                    Success = false,
                    Message = "Failed to retrieve top selling products"
                });
            }
        }

        /// <summary>
        /// Get low stock alerts
        /// </summary>
        [HttpGet("products/low-stock")]
        public async Task<ActionResult<ApiResponse<List<ProductDto>>>> GetLowStockAlerts()
        {
            try
            {
                var lowStockProducts = await _dashboardService.GetLowStockAlertsAsync();
                return Ok(new ApiResponse<List<ProductDto>>
                {
                    Success = true,
                    Message = "Low stock alerts retrieved successfully",
                    Data = lowStockProducts
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting low stock alerts");
                return StatusCode(500, new ApiResponse<List<ProductDto>>
                {
                    Success = false,
                    Message = "Failed to retrieve low stock alerts"
                });
            }
        }

        /// <summary>
        /// Get category sales breakdown
        /// </summary>
        [HttpGet("categories/sales")]
        public async Task<ActionResult<ApiResponse<List<CategorySalesDto>>>> GetCategorySales(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var categorySales = await _dashboardService.GetCategorySalesAsync(startDate, endDate);
                return Ok(new ApiResponse<List<CategorySalesDto>>
                {
                    Success = true,
                    Message = "Category sales retrieved successfully",
                    Data = categorySales
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting category sales");
                return StatusCode(500, new ApiResponse<List<CategorySalesDto>>
                {
                    Success = false,
                    Message = "Failed to retrieve category sales"
                });
            }
        }

        /// <summary>
        /// Get recent transactions
        /// </summary>
        [HttpGet("transactions/recent")]
        public async Task<ActionResult<ApiResponse<List<RecentTransactionDto>>>> GetRecentTransactions(
            [FromQuery] int count = 10)
        {
            try
            {
                var recentTransactions = await _dashboardService.GetRecentTransactionsAsync(count);
                return Ok(new ApiResponse<List<RecentTransactionDto>>
                {
                    Success = true,
                    Message = "Recent transactions retrieved successfully",
                    Data = recentTransactions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent transactions");
                return StatusCode(500, new ApiResponse<List<RecentTransactionDto>>
                {
                    Success = false,
                    Message = "Failed to retrieve recent transactions"
                });
            }
        }

        /// <summary>
        /// Generate sales report
        /// </summary>
        [HttpGet("reports/sales")]
        [Authorize(Policy = "Reports.Read")]
        public async Task<ActionResult<ApiResponse<SalesReportDto>>> GenerateSalesReport(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                if (startDate == default || endDate == default)
                {
                    return BadRequest(new ApiResponse<SalesReportDto>
                    {
                        Success = false,
                        Message = "Start date and end date are required"
                    });
                }

                if (startDate > endDate)
                {
                    return BadRequest(new ApiResponse<SalesReportDto>
                    {
                        Success = false,
                        Message = "Start date cannot be later than end date"
                    });
                }

                var salesReport = await _dashboardService.GenerateSalesReportAsync(startDate, endDate);
                return Ok(new ApiResponse<SalesReportDto>
                {
                    Success = true,
                    Message = "Sales report generated successfully",
                    Data = salesReport
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating sales report");
                return StatusCode(500, new ApiResponse<SalesReportDto>
                {
                    Success = false,
                    Message = "Failed to generate sales report"
                });
            }
        }

        /// <summary>
        /// Generate inventory report
        /// </summary>
        [HttpGet("reports/inventory")]
        [Authorize(Policy = "Reports.Read")]
        public async Task<ActionResult<ApiResponse<InventoryReportDto>>> GenerateInventoryReport()
        {
            try
            {
                var inventoryReport = await _dashboardService.GenerateInventoryReportAsync();
                return Ok(new ApiResponse<InventoryReportDto>
                {
                    Success = true,
                    Message = "Inventory report generated successfully",
                    Data = inventoryReport
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating inventory report");
                return StatusCode(500, new ApiResponse<InventoryReportDto>
                {
                    Success = false,
                    Message = "Failed to generate inventory report"
                });
            }
        }
    }
}