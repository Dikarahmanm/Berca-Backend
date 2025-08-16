using Berca_Backend.DTOs;
using Berca_Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Berca_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ConsolidatedReportController : ControllerBase
    {
        private readonly IConsolidatedReportService _reportService;
        private readonly IUserBranchAssignmentService _userBranchService;
        private readonly ITimezoneService _timezoneService;
        private readonly ILogger<ConsolidatedReportController> _logger;

        public ConsolidatedReportController(
            IConsolidatedReportService reportService,
            IUserBranchAssignmentService userBranchService,
            ITimezoneService timezoneService,
            ILogger<ConsolidatedReportController> logger)
        {
            _reportService = reportService;
            _userBranchService = userBranchService;
            _timezoneService = timezoneService;
            _logger = logger;
        }

        /// <summary>
        /// Get cross-branch sales comparison with flexible date ranges
        /// </summary>
        [HttpGet("sales-comparison")]
        [Authorize(Policy = "Reports.Consolidated")]
        public async Task<IActionResult> GetSalesComparison([FromQuery] ConsolidatedReportQueryParams queryParams)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                // Apply user access restrictions
                var requestingUserId = IsAdminOrHeadManager(currentUserRole) ? (int?)null : currentUserId;

                var salesComparison = await _reportService.GetSalesComparisonAsync(queryParams, requestingUserId);

                _logger.LogInformation("Sales comparison report generated for user {UserId} with date range {DateRange}", 
                    currentUserId, queryParams.DateRange);

                return Ok(ApiResponse<SalesComparisonDto>.SuccessResponse(salesComparison, 
                    $"Sales comparison report generated for {salesComparison.BranchMetrics.Count} branches"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating sales comparison report for user {UserId}", GetCurrentUserId());
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get real-time inventory levels across all store locations with low stock alerts
        /// </summary>
        [HttpGet("inventory-overview")]
        [Authorize(Policy = "Reports.Consolidated")]
        public async Task<IActionResult> GetInventoryOverview([FromQuery] ConsolidatedReportQueryParams queryParams)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                var requestingUserId = IsAdminOrHeadManager(currentUserRole) ? (int?)null : currentUserId;

                var inventoryOverview = await _reportService.GetInventoryOverviewAsync(queryParams, requestingUserId);

                _logger.LogInformation("Inventory overview report generated for user {UserId}", currentUserId);

                return Ok(ApiResponse<InventoryOverviewDto>.SuccessResponse(inventoryOverview, 
                    $"Inventory overview for {inventoryOverview.BranchInventories.Count} branches with {inventoryOverview.LowStockAlerts.Count} alerts"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating inventory overview for user {UserId}", GetCurrentUserId());
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get regional performance dashboard comparing different provinces
        /// </summary>
        [HttpGet("regional-dashboard")]
        [Authorize(Policy = "Reports.CrossBranch")]
        public async Task<IActionResult> GetRegionalDashboard([FromQuery] ConsolidatedReportQueryParams queryParams)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                var requestingUserId = IsAdminOrHeadManager(currentUserRole) ? (int?)null : currentUserId;

                var regionalDashboard = await _reportService.GetRegionalDashboardAsync(queryParams, requestingUserId);

                _logger.LogInformation("Regional dashboard generated for user {UserId}", currentUserId);

                return Ok(ApiResponse<RegionalDashboardDto>.SuccessResponse(regionalDashboard, 
                    $"Regional dashboard for {regionalDashboard.RegionalPerformance.Count} regions with {regionalDashboard.GrowthOpportunities.Count} opportunities"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating regional dashboard for user {UserId}", GetCurrentUserId());
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get branch performance ranking with scoring and benchmarking
        /// </summary>
        [HttpGet("branch-ranking")]
        [Authorize(Policy = "Reports.Consolidated")]
        public async Task<IActionResult> GetBranchRanking([FromQuery] ConsolidatedReportQueryParams queryParams)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                var requestingUserId = IsAdminOrHeadManager(currentUserRole) ? (int?)null : currentUserId;

                var branchRanking = await _reportService.GetBranchRankingAsync(queryParams, requestingUserId);

                _logger.LogInformation("Branch ranking report generated for user {UserId}", currentUserId);

                return Ok(ApiResponse<BranchRankingDto>.SuccessResponse(branchRanking, 
                    $"Branch ranking for {branchRanking.Rankings.Count} branches with {branchRanking.Insights.Count} insights"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating branch ranking for user {UserId}", GetCurrentUserId());
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get executive summary with high-level KPIs for management
        /// </summary>
        [HttpGet("executive-summary")]
        [Authorize(Policy = "Reports.Consolidated")]
        public async Task<IActionResult> GetExecutiveSummary([FromQuery] ConsolidatedReportQueryParams queryParams)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                // Executive summary typically requires high-level access
                if (!IsAdminOrHeadManager(currentUserRole))
                {
                    return Forbid("Executive summary requires Admin or HeadManager access");
                }

                var executiveSummary = await _reportService.GetExecutiveSummaryAsync(queryParams, currentUserId);

                _logger.LogInformation("Executive summary generated for user {UserId}", currentUserId);

                return Ok(ApiResponse<ExecutiveSummaryDto>.SuccessResponse(executiveSummary, 
                    $"Executive summary with {executiveSummary.KeyInsights.Count} insights and {executiveSummary.CriticalAlerts.Count} alerts"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating executive summary for user {UserId}", GetCurrentUserId());
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get real-time metrics for live dashboard monitoring
        /// </summary>
        [HttpGet("real-time-metrics")]
        [Authorize(Policy = "Reports.Read")]
        public async Task<IActionResult> GetRealTimeMetrics()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                var requestingUserId = IsAdminOrHeadManager(currentUserRole) ? (int?)null : currentUserId;

                var realTimeMetrics = await _reportService.GetRealTimeMetricsAsync(requestingUserId);

                return Ok(ApiResponse<RealTimeMetricsDto>.SuccessResponse(realTimeMetrics, 
                    $"Real-time metrics for {realTimeMetrics.BranchMetrics.Count} branches (last updated: {realTimeMetrics.LastUpdated:HH:mm:ss})"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting real-time metrics for user {UserId}", GetCurrentUserId());
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get trend analysis with historical performance trends
        /// </summary>
        [HttpGet("trend-analysis")]
        [Authorize(Policy = "Reports.Consolidated")]
        public async Task<IActionResult> GetTrendAnalysis([FromQuery] ConsolidatedReportQueryParams queryParams)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                var requestingUserId = IsAdminOrHeadManager(currentUserRole) ? (int?)null : currentUserId;

                var trendAnalysis = await _reportService.GetTrendAnalysisAsync(queryParams, requestingUserId);

                _logger.LogInformation("Trend analysis generated for user {UserId}", currentUserId);

                return Ok(ApiResponse<TrendAnalysisDto>.SuccessResponse(trendAnalysis, 
                    $"Trend analysis with {trendAnalysis.SalesTrend.Count} data points and {trendAnalysis.Forecast.NextPeriod.Count} forecast periods"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating trend analysis for user {UserId}", GetCurrentUserId());
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Export consolidated reports in PDF or Excel format
        /// </summary>
        [HttpPost("export")]
        [Authorize(Policy = "Reports.Consolidated")]
        public async Task<IActionResult> ExportReport([FromBody] ExportParams exportParams)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Invalid export parameters",
                        ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()));
                }

                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                var requestingUserId = IsAdminOrHeadManager(currentUserRole) ? (int?)null : currentUserId;

                var reportData = await _reportService.ExportReportAsync(exportParams, requestingUserId);

                if (reportData.Length == 0)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Failed to generate report export"));
                }

                var contentType = exportParams.ExportFormat.ToLower() switch
                {
                    "pdf" => "application/pdf",
                    "excel" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "csv" => "text/csv",
                    _ => "application/octet-stream"
                };

                var fileName = $"consolidated_report_{exportParams.ReportType}_{DateTime.Now:yyyyMMdd_HHmmss}.{exportParams.ExportFormat.ToLower()}";

                _logger.LogInformation("Report exported for user {UserId}: {ReportType} in {Format}", 
                    currentUserId, exportParams.ReportType, exportParams.ExportFormat);

                return File(reportData, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting report for user {UserId}", GetCurrentUserId());
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Generate temporary download URL for large reports
        /// </summary>
        [HttpPost("generate-export-url")]
        [Authorize(Policy = "Reports.Consolidated")]
        public async Task<IActionResult> GenerateExportUrl([FromBody] ExportParams exportParams)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Invalid export parameters",
                        ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()));
                }

                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                var requestingUserId = IsAdminOrHeadManager(currentUserRole) ? (int?)null : currentUserId;

                var downloadUrl = await _reportService.GenerateReportUrlAsync(exportParams, requestingUserId);

                _logger.LogInformation("Export URL generated for user {UserId}: {ReportType}", 
                    currentUserId, exportParams.ReportType);

                return Ok(ApiResponse<string>.SuccessResponse(downloadUrl, 
                    "Export URL generated successfully. URL will expire in 1 hour."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating export URL for user {UserId}", GetCurrentUserId());
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get store size performance correlation analysis
        /// </summary>
        [HttpGet("store-size-analysis")]
        [Authorize(Policy = "Reports.Consolidated")]
        public async Task<IActionResult> GetStoreSizeAnalysis([FromQuery] ConsolidatedReportQueryParams queryParams)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                var requestingUserId = IsAdminOrHeadManager(currentUserRole) ? (int?)null : currentUserId;

                var salesComparison = await _reportService.GetSalesComparisonAsync(queryParams, requestingUserId);

                // Analyze performance by store size
                var storeSizeAnalysis = salesComparison.BranchMetrics
                    .GroupBy(b => b.StoreSize)
                    .Select(g => new
                    {
                        StoreSize = g.Key,
                        BranchCount = g.Count(),
                        AverageRevenue = g.Average(b => b.TotalRevenue),
                        AveragePerformanceScore = g.Average(b => b.PerformanceScore),
                        AverageProfitMargin = g.Average(b => b.NetProfitMargin),
                        AverageSalesPerEmployee = g.Average(b => b.SalesPerEmployee),
                        TopPerformer = g.OrderByDescending(b => b.PerformanceScore).First(),
                        EfficiencyRatio = g.Sum(b => b.TotalRevenue) / g.Sum(b => b.EmployeeCount)
                    })
                    .OrderByDescending(x => x.AveragePerformanceScore)
                    .ToList();

                _logger.LogInformation("Store size analysis generated for user {UserId}", currentUserId);

                return Ok(ApiResponse<object>.SuccessResponse(storeSizeAnalysis, 
                    $"Store size analysis for {storeSizeAnalysis.Count} store sizes"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating store size analysis for user {UserId}", GetCurrentUserId());
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get employee productivity metrics per branch
        /// </summary>
        [HttpGet("employee-productivity")]
        [Authorize(Policy = "Reports.Consolidated")]
        public async Task<IActionResult> GetEmployeeProductivity([FromQuery] ConsolidatedReportQueryParams queryParams)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                var requestingUserId = IsAdminOrHeadManager(currentUserRole) ? (int?)null : currentUserId;

                var salesComparison = await _reportService.GetSalesComparisonAsync(queryParams, requestingUserId);

                var productivityMetrics = salesComparison.BranchMetrics
                    .Select(b => new
                    {
                        BranchId = b.BranchId,
                        BranchName = b.BranchName,
                        City = b.City,
                        Province = b.Province,
                        EmployeeCount = b.EmployeeCount,
                        SalesPerEmployee = b.SalesPerEmployee,
                        TransactionsPerEmployee = b.TransactionsPerEmployee,
                        EmployeeProductivity = b.EmployeeProductivity,
                        ProductivityRanking = 0, // Will be calculated
                        ProductivityCategory = DetermineProductivityCategory(b.SalesPerEmployee),
                        RecommendedActions = GenerateProductivityRecommendations(b.SalesPerEmployee, b.EmployeeProductivity)
                    })
                    .OrderByDescending(p => p.SalesPerEmployee)
                    .Select((p, index) => new { Data = p, Rank = index + 1 })
                    .Select(x => new
                    {
                        x.Data.BranchId,
                        x.Data.BranchName,
                        x.Data.City,
                        x.Data.Province,
                        x.Data.EmployeeCount,
                        x.Data.SalesPerEmployee,
                        x.Data.TransactionsPerEmployee,
                        x.Data.EmployeeProductivity,
                        ProductivityRanking = x.Rank,
                        x.Data.ProductivityCategory,
                        x.Data.RecommendedActions
                    })
                    .ToList();

                _logger.LogInformation("Employee productivity analysis generated for user {UserId}", currentUserId);

                return Ok(ApiResponse<object>.SuccessResponse(productivityMetrics, 
                    $"Employee productivity metrics for {productivityMetrics.Count} branches"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating employee productivity analysis for user {UserId}", GetCurrentUserId());
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get profit margin comparison with trend analysis
        /// </summary>
        [HttpGet("profit-analysis")]
        [Authorize(Policy = "Reports.Consolidated")]
        public async Task<IActionResult> GetProfitAnalysis([FromQuery] ConsolidatedReportQueryParams queryParams)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                var requestingUserId = IsAdminOrHeadManager(currentUserRole) ? (int?)null : currentUserId;

                var salesComparison = await _reportService.GetSalesComparisonAsync(queryParams, requestingUserId);
                var trendAnalysis = await _reportService.GetTrendAnalysisAsync(queryParams, requestingUserId);

                var profitAnalysis = new
                {
                    ConsolidatedProfitMetrics = new
                    {
                        TotalGrossProfit = salesComparison.ConsolidatedMetrics.TotalGrossProfit,
                        TotalNetProfit = salesComparison.ConsolidatedMetrics.TotalNetProfit,
                        AverageGrossProfitMargin = salesComparison.ConsolidatedMetrics.ConsolidatedGrossProfitMargin,
                        AverageNetProfitMargin = salesComparison.ConsolidatedMetrics.ConsolidatedNetProfitMargin,
                        ProfitTrend = trendAnalysis.TrendSummary.OverallTrend,
                        ProfitGrowthRate = trendAnalysis.TrendSummary.OverallGrowthRate
                    },
                    BranchProfitMetrics = salesComparison.BranchMetrics
                        .Select(b => new
                        {
                            b.BranchId,
                            b.BranchName,
                            b.City,
                            b.Province,
                            b.StoreSize,
                            b.GrossProfit,
                            b.NetProfit,
                            b.GrossProfitMargin,
                            b.NetProfitMargin,
                            b.ProfitTrend,
                            ProfitPerEmployee = b.EmployeeCount > 0 ? b.NetProfit / b.EmployeeCount : 0,
                            ProfitCategory = DetermineProfitCategory(b.NetProfitMargin),
                            MarginOptimizationPotential = Math.Max(0, 20 - b.NetProfitMargin) // Assume 20% is optimal
                        })
                        .OrderByDescending(b => b.NetProfitMargin)
                        .ToList(),
                    ProfitInsights = GenerateProfitInsights(salesComparison.BranchMetrics),
                    Benchmarks = new
                    {
                        IndustryAverageMargin = 15.0m, // Placeholder
                        TopQuartileMargin = salesComparison.BranchMetrics.OrderByDescending(b => b.NetProfitMargin).Take(salesComparison.BranchMetrics.Count / 4).Average(b => b.NetProfitMargin),
                        MedianMargin = salesComparison.BranchMetrics.OrderBy(b => b.NetProfitMargin).Skip(salesComparison.BranchMetrics.Count / 2).Take(1).Average(b => b.NetProfitMargin)
                    }
                };

                _logger.LogInformation("Profit analysis generated for user {UserId}", currentUserId);

                return Ok(ApiResponse<object>.SuccessResponse(profitAnalysis, 
                    $"Profit analysis for {salesComparison.BranchMetrics.Count} branches"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating profit analysis for user {UserId}", GetCurrentUserId());
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get peak hours analysis for staffing optimization
        /// </summary>
        [HttpGet("peak-hours-analysis")]
        [Authorize(Policy = "Reports.Branch")]
        public async Task<IActionResult> GetPeakHoursAnalysis([FromQuery] ConsolidatedReportQueryParams queryParams)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                var requestingUserId = IsAdminOrHeadManager(currentUserRole) ? (int?)null : currentUserId;

                // Get sales data grouped by hour (simplified analysis)
                var dateRange = await GetDateRangeFromQueryParams(queryParams);
                
                // This would require actual sales data analysis by hour
                var peakHoursAnalysis = new
                {
                    OverallPeakHours = new
                    {
                        MorningPeak = "10:00-11:00",
                        LunchPeak = "12:00-13:00", 
                        EveningPeak = "17:00-19:00",
                        WeekendPeak = "14:00-16:00"
                    },
                    HourlyTransactionDistribution = Enumerable.Range(8, 13) // 8 AM to 8 PM
                        .Select(hour => new
                        {
                            Hour = $"{hour:D2}:00",
                            AverageTransactions = Random.Shared.Next(5, 25), // Placeholder
                            AverageRevenue = Random.Shared.Next(500000, 2000000), // Placeholder
                            RecommendedStaffCount = CalculateRecommendedStaff(hour)
                        })
                        .ToList(),
                    BranchSpecificPeaks = new List<object>(), // Would be populated with actual branch-specific data
                    StaffingRecommendations = new
                    {
                        OptimalStaffingLevels = "3-4 staff during peak hours (10-11 AM, 12-1 PM, 5-7 PM)",
                        MinimumStaffing = "2 staff during off-peak hours",
                        WeekendAdjustments = "Increase staff by 20% on weekends",
                        HolidayConsiderations = "Double staff during holiday periods"
                    },
                    EfficiencyMetrics = new
                    {
                        AverageServiceTime = "3.5 minutes per transaction",
                        QueueOptimization = "Consider additional checkout during peak hours",
                        StaffUtilization = "78% average utilization rate"
                    }
                };

                _logger.LogInformation("Peak hours analysis generated for user {UserId}", currentUserId);

                return Ok(ApiResponse<object>.SuccessResponse(peakHoursAnalysis, 
                    "Peak hours analysis for staffing optimization"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating peak hours analysis for user {UserId}", GetCurrentUserId());
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get store maturity analysis (new vs established branch performance)
        /// </summary>
        [HttpGet("store-maturity-analysis")]
        [Authorize(Policy = "Reports.Consolidated")]
        public async Task<IActionResult> GetStoreMaturityAnalysis([FromQuery] ConsolidatedReportQueryParams queryParams)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                var requestingUserId = IsAdminOrHeadManager(currentUserRole) ? (int?)null : currentUserId;

                var salesComparison = await _reportService.GetSalesComparisonAsync(queryParams, requestingUserId);

                var maturityAnalysis = salesComparison.BranchMetrics
                    .Select(b => new
                    {
                        b.BranchId,
                        b.BranchName,
                        b.City,
                        b.Province,
                        MaturityCategory = DetermineMaturityCategory(DateTime.Now), // Would use actual opening date
                        MonthsInOperation = CalculateMonthsInOperation(DateTime.Now), // Would use actual opening date
                        b.TotalRevenue,
                        b.PerformanceScore,
                        b.NetProfitMargin,
                        RevenuePerMonth = CalculateRevenuePerMonth(b.TotalRevenue, 12), // Simplified
                        GrowthTrajectory = DetermineGrowthTrajectory(b.RevenueGrowth),
                        MaturityBenchmark = GetMaturityBenchmark(DetermineMaturityCategory(DateTime.Now))
                    })
                    .GroupBy(b => b.MaturityCategory)
                    .Select(g => new
                    {
                        MaturityLevel = g.Key,
                        BranchCount = g.Count(),
                        AverageRevenue = g.Average(b => b.TotalRevenue),
                        AveragePerformanceScore = g.Average(b => b.PerformanceScore),
                        AverageProfitMargin = g.Average(b => b.NetProfitMargin),
                        TopPerformer = g.OrderByDescending(b => b.PerformanceScore).FirstOrDefault(),
                        BottomPerformer = g.OrderBy(b => b.PerformanceScore).FirstOrDefault(),
                        Branches = g.ToList()
                    })
                    .OrderBy(g => g.MaturityLevel)
                    .ToList();

                var insights = new
                {
                    NewStoreInsights = "New stores (0-12 months) show strong growth potential but need operational support",
                    EstablishedStoreInsights = "Established stores (12+ months) provide stable revenue but may need innovation",
                    OptimizationOpportunities = maturityAnalysis
                        .Where(m => m.AveragePerformanceScore < 70)
                        .Select(m => $"{m.MaturityLevel} stores need performance improvement")
                        .ToList()
                };

                var result = new
                {
                    MaturityAnalysis = maturityAnalysis,
                    Insights = insights,
                    Recommendations = GenerateMaturityRecommendations(maturityAnalysis.Cast<object>().ToList())
                };

                _logger.LogInformation("Store maturity analysis generated for user {UserId}", currentUserId);

                return Ok(ApiResponse<object>.SuccessResponse(result, 
                    $"Store maturity analysis for {salesComparison.BranchMetrics.Count} branches"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating store maturity analysis for user {UserId}", GetCurrentUserId());
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get comprehensive analytics dashboard data
        /// </summary>
        [HttpGet("dashboard-summary")]
        [Authorize(Policy = "Dashboard.Consolidated")]
        public async Task<IActionResult> GetDashboardSummary([FromQuery] ConsolidatedReportQueryParams queryParams)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                var requestingUserId = IsAdminOrHeadManager(currentUserRole) ? (int?)null : currentUserId;

                // Get all core analytics in parallel
                var salesTask = _reportService.GetSalesComparisonAsync(queryParams, requestingUserId);
                var inventoryTask = _reportService.GetInventoryOverviewAsync(queryParams, requestingUserId);
                var realTimeTask = _reportService.GetRealTimeMetricsAsync(requestingUserId);
                var trendTask = _reportService.GetTrendAnalysisAsync(queryParams, requestingUserId);

                await Task.WhenAll(salesTask, inventoryTask, realTimeTask, trendTask);

                var dashboardSummary = new
                {
                    Overview = new
                    {
                        TotalRevenue = salesTask.Result.ConsolidatedMetrics.TotalRevenue,
                        TotalBranches = salesTask.Result.BranchMetrics.Count,
                        ActiveBranches = salesTask.Result.BranchMetrics.Count(b => b.IsActive),
                        TotalTransactions = salesTask.Result.ConsolidatedMetrics.TotalTransactions,
                        AverageTicketSize = salesTask.Result.ConsolidatedMetrics.AverageTicketSize,
                        OverallPerformanceScore = salesTask.Result.ConsolidatedMetrics.AveragePerformanceScore
                    },
                    RealtimeMetrics = new
                    {
                        TodayRevenue = realTimeTask.Result.TodaySales.TodayRevenue,
                        TodayTransactions = realTimeTask.Result.TodaySales.TodayTransactions,
                        ProjectedDaily = realTimeTask.Result.TodaySales.ProjectedDailyRevenue,
                        ActiveBranches = realTimeTask.Result.BranchMetrics.Count(b => b.IsOpen),
                        SystemHealth = realTimeTask.Result.SystemHealth.AllSystemsOperational
                    },
                    TopPerformers = salesTask.Result.BranchMetrics
                        .OrderByDescending(b => b.PerformanceScore)
                        .Take(5)
                        .Select(b => new { b.BranchName, b.City, b.PerformanceScore, b.TotalRevenue })
                        .ToList(),
                    CriticalAlerts = new
                    {
                        LowStockItems = inventoryTask.Result.LowStockAlerts.Count(a => a.Severity == "Critical"),
                        UnderperformingBranches = salesTask.Result.BranchMetrics.Count(b => b.PerformanceScore < 50),
                        SystemIssues = realTimeTask.Result.SystemHealth.SystemAlerts.Count
                    },
                    Trends = new
                    {
                        SalesTrend = trendTask.Result.TrendSummary.OverallTrend,
                        GrowthRate = trendTask.Result.TrendSummary.OverallGrowthRate,
                        Volatility = trendTask.Result.TrendSummary.Volatility
                    },
                    QuickStats = new
                    {
                        BestPerformingRegion = salesTask.Result.BranchMetrics
                            .GroupBy(b => b.Province)
                            .OrderByDescending(g => g.Average(b => b.PerformanceScore))
                            .FirstOrDefault()?.Key ?? "N/A",
                        InventoryHealth = inventoryTask.Result.ConsolidatedInventory.OverallStockHealth,
                        CustomerRetention = salesTask.Result.ConsolidatedMetrics.OverallCustomerRetentionRate,
                        EmployeeProductivity = salesTask.Result.ConsolidatedMetrics.AverageSalesPerEmployee
                    }
                };

                _logger.LogInformation("Dashboard summary generated for user {UserId}", currentUserId);

                return Ok(ApiResponse<object>.SuccessResponse(dashboardSummary, 
                    "Comprehensive dashboard summary retrieved"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating dashboard summary for user {UserId}", GetCurrentUserId());
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        #region Helper Methods

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst("sub")?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        private string GetCurrentUserRole()
        {
            return User.FindFirst("Role")?.Value ?? 
                   User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value ?? 
                   "User";
        }

        private bool IsAdminOrHeadManager(string role)
        {
            return role.ToUpper() is "ADMIN" or "HEADMANAGER";
        }

        private Task<DateTime[]> GetDateRangeFromQueryParams(ConsolidatedReportQueryParams queryParams)
        {
            var now = _timezoneService.Now;
            var startDate = queryParams.StartDate ?? now.AddDays(-30);
            var endDate = queryParams.EndDate ?? now;
            return Task.FromResult(new[] { startDate, endDate });
        }

        private string DetermineProductivityCategory(decimal salesPerEmployee)
        {
            return salesPerEmployee switch
            {
                >= 100000 => "Excellent",
                >= 75000 => "Good",
                >= 50000 => "Average",
                >= 25000 => "Below Average",
                _ => "Poor"
            };
        }

        private List<string> GenerateProductivityRecommendations(decimal salesPerEmployee, decimal productivity)
        {
            var recommendations = new List<string>();

            if (salesPerEmployee < 50000)
            {
                recommendations.Add("Implement sales training programs");
                recommendations.Add("Review staffing levels and optimize schedules");
            }

            if (productivity < 50)
            {
                recommendations.Add("Introduce performance incentives");
                recommendations.Add("Optimize product placement and store layout");
            }

            return recommendations;
        }

        private string DetermineProfitCategory(decimal profitMargin)
        {
            return profitMargin switch
            {
                >= 20 => "Excellent",
                >= 15 => "Good",
                >= 10 => "Average",
                >= 5 => "Below Average",
                _ => "Poor"
            };
        }

        private List<string> GenerateProfitInsights(List<BranchSalesMetricsDto> branchMetrics)
        {
            var insights = new List<string>();

            var avgMargin = branchMetrics.Average(b => b.NetProfitMargin);
            var lowMarginCount = branchMetrics.Count(b => b.NetProfitMargin < 10);

            insights.Add($"Average profit margin across all branches: {avgMargin:F1}%");

            if (lowMarginCount > 0)
                insights.Add($"{lowMarginCount} branches have profit margins below 10%");

            var topMarginBranch = branchMetrics.OrderByDescending(b => b.NetProfitMargin).FirstOrDefault();
            if (topMarginBranch != null)
                insights.Add($"Top performing branch: {topMarginBranch.BranchName} with {topMarginBranch.NetProfitMargin:F1}% margin");

            return insights;
        }

        private int CalculateRecommendedStaff(int hour)
        {
            // Peak hours need more staff
            return hour switch
            {
                10 or 11 or 12 or 13 or 17 or 18 or 19 => 4, // Peak hours
                9 or 14 or 15 or 16 or 20 => 3, // Moderate hours
                _ => 2 // Off-peak hours
            };
        }

        private string DetermineMaturityCategory(DateTime openingDate)
        {
            var monthsOld = (DateTime.Now - openingDate).Days / 30;
            return monthsOld switch
            {
                < 6 => "New (0-6 months)",
                < 12 => "Growing (6-12 months)",
                < 24 => "Established (1-2 years)",
                _ => "Mature (2+ years)"
            };
        }

        private int CalculateMonthsInOperation(DateTime openingDate)
        {
            return (DateTime.Now.Year - openingDate.Year) * 12 + DateTime.Now.Month - openingDate.Month;
        }

        private decimal CalculateRevenuePerMonth(decimal totalRevenue, int months)
        {
            return months > 0 ? totalRevenue / months : totalRevenue;
        }

        private string DetermineGrowthTrajectory(decimal growthRate)
        {
            return growthRate switch
            {
                > 20 => "Rapid Growth",
                > 10 => "Strong Growth",
                > 5 => "Steady Growth",
                > 0 => "Slow Growth",
                _ => "Declining"
            };
        }

        private decimal GetMaturityBenchmark(string maturityCategory)
        {
            return maturityCategory switch
            {
                "New (0-6 months)" => 60,
                "Growing (6-12 months)" => 70,
                "Established (1-2 years)" => 80,
                "Mature (2+ years)" => 85,
                _ => 75
            };
        }

        private List<string> GenerateMaturityRecommendations(List<object> maturityAnalysis)
        {
            return new List<string>
            {
                "Focus on operational excellence for new stores",
                "Implement best practices sharing between mature and new stores",
                "Consider expansion in high-performing mature markets",
                "Develop store-specific improvement plans based on maturity stage"
            };
        }

        #endregion
    }
}