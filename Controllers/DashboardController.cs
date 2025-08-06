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
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string sortBy = "quantity") // ✅ ADDED sortBy parameter
        {
            try
            {
                var topProducts = await _dashboardService.GetTopSellingProductsAsync(count, startDate, endDate, sortBy);
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
        public async Task<ActionResult<ApiResponse<List<RecentTransactionDto>>>>
            GetRecentTransactions([FromQuery] int count = 10)
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

        /// <summary>
        /// Get worst performing products
        /// </summary>
        [HttpGet("products/worst-performing")]
        public async Task<ActionResult<ApiResponse<List<WorstProductDto>>>> GetWorstPerformingProducts(
            [FromQuery] int count = 10,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var worstProducts = await _dashboardService.GetWorstPerformingProductsAsync(count, startDate, endDate);
                return Ok(new ApiResponse<List<WorstProductDto>>
                {
                    Success = true,
                    Message = "Worst performing products retrieved successfully",
                    Data = worstProducts
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting worst performing products");
                return StatusCode(500, new ApiResponse<List<WorstProductDto>>
                {
                    Success = false,
                    Message = "Failed to retrieve worst performing products"
                });
            }
        }

        /// <summary>
        /// Generate financial report
        /// </summary>
        [HttpGet("reports/financial")]
        [Authorize(Policy = "Reports.Read")]
        public async Task<ActionResult<ApiResponse<FinancialReportDto>>> GenerateFinancialReport(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                if (startDate == default || endDate == default)
                {
                    return BadRequest(new ApiResponse<FinancialReportDto>
                    {
                        Success = false,
                        Message = "Start date and end date are required"
                    });
                }

                var financialReport = await _dashboardService.GenerateFinancialReportAsync(startDate, endDate);
                return Ok(new ApiResponse<FinancialReportDto>
                {
                    Success = true,
                    Message = "Financial report generated successfully",
                    Data = financialReport
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating financial report");
                return StatusCode(500, new ApiResponse<FinancialReportDto>
                {
                    Success = false,
                    Message = "Failed to generate financial report"
                });
            }
        }

        /// <summary>
        /// Generate customer report
        /// </summary>
        [HttpGet("reports/customer")]
        [Authorize(Policy = "Reports.Read")]
        public async Task<ActionResult<ApiResponse<CustomerReportDto>>> GenerateCustomerReport(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                if (startDate == default || endDate == default)
                {
                    return BadRequest(new ApiResponse<CustomerReportDto>
                    {
                        Success = false,
                        Message = "Start date and end date are required"
                    });
                }

                var customerReport = await _dashboardService.GenerateCustomerReportAsync(startDate, endDate);
                return Ok(new ApiResponse<CustomerReportDto>
                {
                    Success = true,
                    Message = "Customer report generated successfully",
                    Data = customerReport
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating customer report");
                return StatusCode(500, new ApiResponse<CustomerReportDto>
                {
                    Success = false,
                    Message = "Failed to generate customer report"
                });
            }
        }

        /// <summary>
        /// Export sales report
        /// </summary>
        [HttpPost("reports/sales/export")]
        [Authorize(Policy = "Reports.Read")]
        public async Task<ActionResult<ApiResponse<ReportExportDto>>> ExportSalesReport(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] string format = "PDF")
        {
            try
            {
                if (!new[] { "PDF", "Excel" }.Contains(format.ToUpper()))
                {
                    return BadRequest(new ApiResponse<ReportExportDto>
                    {
                        Success = false,
                        Message = "Format must be either 'PDF' or 'Excel'"
                    });
                }

                var exportResult = await _dashboardService.ExportSalesReportAsync(startDate, endDate, format);
                return Ok(new ApiResponse<ReportExportDto>
                {
                    Success = true,
                    Message = $"Sales report exported to {format} successfully",
                    Data = exportResult
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting sales report");
                return StatusCode(500, new ApiResponse<ReportExportDto>
                {
                    Success = false,
                    Message = "Failed to export sales report"
                });
            }
        }

        /// <summary>
        /// Export inventory report
        /// </summary>
        [HttpPost("reports/inventory/export")]
        [Authorize(Policy = "Reports.Read")]
        public async Task<ActionResult<ApiResponse<ReportExportDto>>> ExportInventoryReport(
            [FromQuery] string format = "PDF")
        {
            try
            {
                if (!new[] { "PDF", "Excel" }.Contains(format.ToUpper()))
                {
                    return BadRequest(new ApiResponse<ReportExportDto>
                    {
                        Success = false,
                        Message = "Format must be either 'PDF' or 'Excel'"
                    });
                }

                var exportResult = await _dashboardService.ExportInventoryReportAsync(format);
                return Ok(new ApiResponse<ReportExportDto>
                {
                    Success = true,
                    Message = $"Inventory report exported to {format} successfully",
                    Data = exportResult
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting inventory report");
                return StatusCode(500, new ApiResponse<ReportExportDto>
                {
                    Success = false,
                    Message = "Failed to export inventory report"
                });
            }
        }

        /// <summary>
        /// Export financial report
        /// </summary>
        [HttpPost("reports/financial/export")]
        [Authorize(Policy = "Reports.Read")]
        public async Task<ActionResult<ApiResponse<ReportExportDto>>> ExportFinancialReport(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] string format = "PDF")
        {
            try
            {
                if (!new[] { "PDF", "Excel" }.Contains(format.ToUpper()))
                {
                    return BadRequest(new ApiResponse<ReportExportDto>
                    {
                        Success = false,
                        Message = "Format must be either 'PDF' or 'Excel'"
                    });
                }

                var exportResult = await _dashboardService.ExportFinancialReportAsync(startDate, endDate, format);
                return Ok(new ApiResponse<ReportExportDto>
                {
                    Success = true,
                    Message = $"Financial report exported to {format} successfully",
                    Data = exportResult
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting financial report");
                return StatusCode(500, new ApiResponse<ReportExportDto>
                {
                    Success = false,
                    Message = "Failed to export financial report"
                });
            }
        }

        /// <summary>
        /// Export customer report
        /// </summary>
        [HttpPost("reports/customer/export")]
        [Authorize(Policy = "Reports.Read")]
        public async Task<ActionResult<ApiResponse<ReportExportDto>>> ExportCustomerReport(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] string format = "PDF")
        {
            try
            {
                if (!new[] { "PDF", "Excel" }.Contains(format.ToUpper()))
                {
                    return BadRequest(new ApiResponse<ReportExportDto>
                    {
                        Success = false,
                        Message = "Format must be either 'PDF' or 'Excel'"
                    });
                }

                var exportResult = await _dashboardService.ExportCustomerReportAsync(startDate, endDate, format);
                return Ok(new ApiResponse<ReportExportDto>
                {
                    Success = true,
                    Message = $"Customer report exported to {format} successfully",
                    Data = exportResult
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting customer report");
                return StatusCode(500, new ApiResponse<ReportExportDto>
                {
                    Success = false,
                    Message = "Failed to export customer report"
                });
            }
        }

        /// <summary>
        /// Get dashboard KPIs with period filter
        /// </summary>
        [HttpGet("kpis/period")]
        public async Task<ActionResult<ApiResponse<DashboardKPIDto>>> GetDashboardKPIsByPeriod(
            [FromQuery] string period = "today",
            [FromQuery] DateTime? customStart = null,
            [FromQuery] DateTime? customEnd = null)
        {
            try
            {
                if (!IsValidPeriod(period))
                {
                    return BadRequest(new ApiResponse<DashboardKPIDto>
                    {
                        Success = false,
                        Message = "Invalid period. Use: today, yesterday, week, month, year, or custom"
                    });
                }

                var dashboardService = _dashboardService as DashboardService;
                var dateRange = dashboardService?.ResolveDateRange(period, customStart, customEnd);
                
                var kpis = await _dashboardService.GetDashboardKPIsAsync(dateRange?.StartDate, dateRange?.EndDate);
                
                return Ok(new ApiResponse<DashboardKPIDto>
                {
                    Success = true,
                    Message = $"Dashboard KPIs for {period} retrieved successfully",
                    Data = kpis
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard KPIs for period: {Period}", period);
                return StatusCode(500, new ApiResponse<DashboardKPIDto>
                {
                    Success = false,
                    Message = "Failed to retrieve dashboard KPIs"
                });
            }
        }

        /// <summary>
        /// Get top selling products with enhanced sorting options
        /// </summary>
        [HttpGet("products/top-selling/enhanced")]
        public async Task<ActionResult<ApiResponse<List<TopProductDto>>>> GetTopSellingProductsEnhanced(
            [FromQuery] int count = 10,
            [FromQuery] string period = "week",
            [FromQuery] string sortBy = "normalized")
        {
            try
            {
                var dashboardService = _dashboardService as DashboardService;
                var dateRange = dashboardService?.ResolveDateRange(period);
                
                var topProducts = await _dashboardService.GetTopSellingProductsAsync(count, dateRange?.StartDate, dateRange?.EndDate, sortBy);
                
                return Ok(new ApiResponse<List<TopProductDto>>
                {
                    Success = true,
                    Message = $"Top selling products for {period} (sorted by {sortBy}) retrieved successfully",
                    Data = topProducts
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enhanced top selling products");
                return StatusCode(500, new ApiResponse<List<TopProductDto>>
                {
                    Success = false,
                    Message = "Failed to retrieve top selling products"
                });
            }
        }

        private static bool IsValidPeriod(string period)
        {
            var validPeriods = new[] { "today", "yesterday", "week", "month", "year", "custom" };
            return validPeriods.Contains(period.ToLower());
        }

        // ✅ FIXED: More realistic normalizers based on actual data
        private decimal GetRevenueNormalizer(string period) => period.ToLower() switch
        {
            "today" => 50000m,     // 50K for daily (lebih realistis)
            "week" => 300000m,     // 300K for weekly  
            "month" => 2000000m,   // 2M for monthly (turun dari 10M)
            "year" => 20000000m,   // 20M for yearly (turun dari 100M)
            _ => 500000m           // 500K default
        };

        private decimal GetProfitNormalizer(string period) => period.ToLower() switch
        {
            "today" => 10000m,     // 10K for daily
            "week" => 60000m,      // 60K for weekly
            "month" => 400000m,    // 400K for monthly (turun dari 2M)  
            "year" => 4000000m,    // 4M for yearly (turun dari 20M)
            _ => 100000m           // 100K default
        };

        private decimal GetMaxPossibleScore(string period) => period.ToLower() switch
        {
            "today" => 50m,        
            "week" => 100m,        
            "month" => 150m,       // ✅ TURUN: dari 200m ke 150m
            "year" => 300m,        // ✅ TURUN: dari 500m ke 300m
            _ => 100m
        };
    }
}