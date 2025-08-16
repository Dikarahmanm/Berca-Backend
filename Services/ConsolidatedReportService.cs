using Berca_Backend.Data;
using Berca_Backend.DTOs;
using Berca_Backend.Models;
using Berca_Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Berca_Backend.Services
{
    public class ConsolidatedReportService : IConsolidatedReportService
    {
        private readonly AppDbContext _context;
        private readonly ITimezoneService _timezoneService;
        private readonly IUserBranchAssignmentService _userBranchService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ConsolidatedReportService> _logger;

        public ConsolidatedReportService(
            AppDbContext context,
            ITimezoneService timezoneService,
            IUserBranchAssignmentService userBranchService,
            IMemoryCache cache,
            ILogger<ConsolidatedReportService> logger)
        {
            _context = context;
            _timezoneService = timezoneService;
            _userBranchService = userBranchService;
            _cache = cache;
            _logger = logger;
        }

        public async Task<SalesComparisonDto> GetSalesComparisonAsync(ConsolidatedReportQueryParams queryParams, int? requestingUserId = null)
        {
            var accessibleBranchIds = requestingUserId.HasValue 
                ? await GetAccessibleBranchIdsForUserAsync(requestingUserId.Value)
                : await _context.Branches.Select(b => b.Id).ToListAsync();

            var dateRange = await GetDateRangeFromParams(queryParams);
            var branchMetrics = await CalculateBranchSalesMetricsAsync(queryParams, accessibleBranchIds);
            var consolidatedMetrics = await CalculateConsolidatedMetricsAsync(branchMetrics);
            var trends = queryParams.IncludeTrends ? await GetTrendAnalysisAsync(queryParams, requestingUserId) : new TrendAnalysisDto();

            return new SalesComparisonDto
            {
                BranchMetrics = branchMetrics,
                ConsolidatedMetrics = consolidatedMetrics,
                Trends = trends,
                ReportPeriodStart = dateRange[0],
                ReportPeriodEnd = dateRange[1],
                GeneratedAt = _timezoneService.Now,
                ReportSummary = GenerateSalesReportSummary(branchMetrics, consolidatedMetrics)
            };
        }

        public async Task<InventoryOverviewDto> GetInventoryOverviewAsync(ConsolidatedReportQueryParams queryParams, int? requestingUserId = null)
        {
            var accessibleBranchIds = requestingUserId.HasValue 
                ? await GetAccessibleBranchIdsForUserAsync(requestingUserId.Value)
                : await _context.Branches.Select(b => b.Id).ToListAsync();

            var branchInventories = await CalculateBranchInventoryMetricsAsync(accessibleBranchIds);
            var consolidatedInventory = CalculateConsolidatedInventory(branchInventories);
            var lowStockAlerts = await GenerateLowStockAlertsAsync(accessibleBranchIds);
            var fastMovingItems = await GetFastMovingItemsAsync(accessibleBranchIds);
            var slowMovingItems = await GetSlowMovingItemsAsync(accessibleBranchIds);

            return new InventoryOverviewDto
            {
                BranchInventories = branchInventories,
                ConsolidatedInventory = consolidatedInventory,
                LowStockAlerts = lowStockAlerts,
                FastMovingItems = fastMovingItems,
                SlowMovingItems = slowMovingItems,
                GeneratedAt = _timezoneService.Now
            };
        }

        public async Task<RegionalDashboardDto> GetRegionalDashboardAsync(ConsolidatedReportQueryParams queryParams, int? requestingUserId = null)
        {
            var accessibleBranchIds = requestingUserId.HasValue 
                ? await GetAccessibleBranchIdsForUserAsync(requestingUserId.Value)
                : await _context.Branches.Select(b => b.Id).ToListAsync();

            var regionalPerformance = await CalculateRegionalPerformanceAsync(queryParams, accessibleBranchIds);
            var marketShare = CalculateMarketShareByRegion(regionalPerformance);
            var regionalComparison = CalculateRegionalComparison(regionalPerformance);
            var growthOpportunities = await IdentifyGrowthOpportunitiesAsync(queryParams, accessibleBranchIds);

            return new RegionalDashboardDto
            {
                RegionalPerformance = regionalPerformance,
                MarketShareByRegion = marketShare,
                RegionalComparison = regionalComparison,
                GrowthOpportunities = growthOpportunities,
                GeneratedAt = _timezoneService.Now
            };
        }

        public async Task<BranchRankingDto> GetBranchRankingAsync(ConsolidatedReportQueryParams queryParams, int? requestingUserId = null)
        {
            var accessibleBranchIds = requestingUserId.HasValue 
                ? await GetAccessibleBranchIdsForUserAsync(requestingUserId.Value)
                : await _context.Branches.Select(b => b.Id).ToListAsync();

            var branchMetrics = await CalculateBranchSalesMetricsAsync(queryParams, accessibleBranchIds);
            var rankings = await CalculateBranchRankingsAsync(branchMetrics);
            var benchmarks = await CalculatePerformanceBenchmarksAsync(branchMetrics);
            var insights = await GeneratePerformanceInsightsAsync(branchMetrics);

            return new BranchRankingDto
            {
                Rankings = rankings,
                Benchmarks = benchmarks,
                Insights = insights,
                GeneratedAt = _timezoneService.Now
            };
        }

        public async Task<ExecutiveSummaryDto> GetExecutiveSummaryAsync(ConsolidatedReportQueryParams queryParams, int? requestingUserId = null)
        {
            var accessibleBranchIds = requestingUserId.HasValue 
                ? await GetAccessibleBranchIdsForUserAsync(requestingUserId.Value)
                : await _context.Branches.Select(b => b.Id).ToListAsync();

            var dateRange = await GetDateRangeFromParams(queryParams);
            var branchMetrics = await CalculateBranchSalesMetricsAsync(queryParams, accessibleBranchIds);
            var consolidatedMetrics = await CalculateConsolidatedMetricsAsync(branchMetrics);

            var kpis = await CalculateExecutiveKPIsAsync(consolidatedMetrics, branchMetrics, dateRange);
            var keyInsights = await GenerateKeyInsightsAsync(branchMetrics, consolidatedMetrics);
            var criticalAlerts = await GenerateCriticalAlertsAsync(accessibleBranchIds);
            var strategicRecommendations = await GenerateStrategicRecommendationsAsync(queryParams, accessibleBranchIds);
            var competitivePosition = await CalculateCompetitivePositionAsync(consolidatedMetrics);

            return new ExecutiveSummaryDto
            {
                KPIs = kpis,
                KeyInsights = keyInsights,
                CriticalAlerts = criticalAlerts,
                StrategicRecommendations = strategicRecommendations,
                CompetitivePosition = competitivePosition,
                GeneratedAt = _timezoneService.Now,
                ReportPeriod = $"{dateRange[0]:dd/MM/yyyy} - {dateRange[1]:dd/MM/yyyy}"
            };
        }

        public async Task<RealTimeMetricsDto> GetRealTimeMetricsAsync(int? requestingUserId = null)
        {
            var cacheKey = $"realtime_metrics_{requestingUserId ?? 0}";
            var cached = await GetCachedReportDataAsync<RealTimeMetricsDto>(cacheKey);
            if (cached != null)
                return cached;

            var accessibleBranchIds = requestingUserId.HasValue 
                ? await GetAccessibleBranchIdsForUserAsync(requestingUserId.Value)
                : await _context.Branches.Select(b => b.Id).ToListAsync();

            var todaySales = await CalculateTodaySalesAsync(accessibleBranchIds);
            var branchMetrics = await CalculateBranchRealTimeMetricsAsync(accessibleBranchIds);
            var inventoryStatus = await CalculateLiveInventoryStatusAsync(accessibleBranchIds);
            var systemHealth = await CalculateSystemHealthAsync();

            var result = new RealTimeMetricsDto
            {
                TodaySales = todaySales,
                BranchMetrics = branchMetrics,
                InventoryStatus = inventoryStatus,
                SystemHealth = systemHealth,
                LastUpdated = _timezoneService.Now,
                RefreshIntervalSeconds = 30
            };

            await CacheReportDataAsync(cacheKey, result, TimeSpan.FromSeconds(30));
            return result;
        }

        public async Task<TrendAnalysisDto> GetTrendAnalysisAsync(ConsolidatedReportQueryParams queryParams, int? requestingUserId = null)
        {
            var accessibleBranchIds = requestingUserId.HasValue 
                ? await GetAccessibleBranchIdsForUserAsync(requestingUserId.Value)
                : await _context.Branches.Select(b => b.Id).ToListAsync();

            var salesTrend = await CalculateTrendDataAsync(queryParams, "sales", accessibleBranchIds);
            var profitTrend = await CalculateTrendDataAsync(queryParams, "profit", accessibleBranchIds);
            var transactionTrend = await CalculateTrendDataAsync(queryParams, "transactions", accessibleBranchIds);

            var trendSummary = CalculateTrendSummary(salesTrend);
            var seasonalPatterns = await CalculateSeasonalPatternsAsync(salesTrend);
            var forecast = await GenerateForecastAsync(salesTrend);

            return new TrendAnalysisDto
            {
                SalesTrend = salesTrend,
                ProfitTrend = profitTrend,
                TransactionTrend = transactionTrend,
                TrendSummary = trendSummary,
                SeasonalPatterns = seasonalPatterns,
                Forecast = forecast
            };
        }

        public async Task<byte[]> ExportReportAsync(ExportParams exportParams, int? requestingUserId = null)
        {
            // This would integrate with a PDF/Excel generation library
            // For now, return empty array as placeholder
            _logger.LogInformation("Export request for {ReportType} in {Format} format", 
                exportParams.ReportType, exportParams.ExportFormat);
            
            return await Task.FromResult(new byte[0]);
        }

        public async Task<string> GenerateReportUrlAsync(ExportParams exportParams, int? requestingUserId = null)
        {
            // Generate a temporary URL for report download
            var reportId = Guid.NewGuid().ToString();
            return await Task.FromResult($"/api/ConsolidatedReport/download/{reportId}");
        }

        public async Task<List<BranchSalesMetricsDto>> CalculateBranchSalesMetricsAsync(ConsolidatedReportQueryParams queryParams, List<int> accessibleBranchIds)
        {
            var dateRange = await GetDateRangeFromParams(queryParams);
            var branches = await _context.Branches
                .Where(b => accessibleBranchIds.Contains(b.Id))
                .Include(b => b.Users)
                .ToListAsync();

            var metrics = new List<BranchSalesMetricsDto>();

            foreach (var branch in branches)
            {
                // Get sales data for the branch (simplified - would need proper branch-sales relationship)
                var branchSales = await _context.Sales
                    .Where(s => s.SaleDate >= dateRange[0] && s.SaleDate <= dateRange[1])
                    .ToListAsync(); // TODO: Filter by branch properly

                var totalRevenue = branchSales.Sum(s => s.Total);
                var transactionCount = branchSales.Count;
                var averageTicketSize = transactionCount > 0 ? totalRevenue / transactionCount : 0;

                // Calculate profit (simplified)
                var totalCost = branchSales.Sum(s => s.Subtotal * 0.7m); // Assume 70% cost ratio
                var grossProfit = totalRevenue - totalCost;
                var grossProfitMargin = totalRevenue > 0 ? (grossProfit / totalRevenue) * 100 : 0;

                // Employee metrics
                var employeeCount = branch.Users.Count(u => u.IsActive);
                var salesPerEmployee = employeeCount > 0 ? totalRevenue / employeeCount : 0;
                var transactionsPerEmployee = employeeCount > 0 ? (decimal)transactionCount / employeeCount : 0;

                // Customer metrics (simplified)
                var totalCustomers = branchSales.Count(s => !string.IsNullOrEmpty(s.CustomerName));
                var newMembers = await _context.Members
                    .Where(m => m.CreatedAt >= dateRange[0] && m.CreatedAt <= dateRange[1])
                    .CountAsync();

                var metric = new BranchSalesMetricsDto
                {
                    BranchId = branch.Id,
                    BranchCode = branch.BranchCode,
                    BranchName = branch.BranchName,
                    City = branch.City,
                    Province = branch.Province,
                    BranchType = branch.BranchType,
                    StoreSize = branch.StoreSize,
                    IsActive = branch.IsActive,
                    TotalRevenue = totalRevenue,
                    TransactionCount = transactionCount,
                    AverageTicketSize = averageTicketSize,
                    GrossProfit = grossProfit,
                    GrossProfitMargin = grossProfitMargin,
                    NetProfit = grossProfit * 0.8m, // Simplified net profit
                    NetProfitMargin = grossProfitMargin * 0.8m,
                    EmployeeCount = employeeCount,
                    SalesPerEmployee = salesPerEmployee,
                    TransactionsPerEmployee = transactionsPerEmployee,
                    EmployeeProductivity = salesPerEmployee / (averageTicketSize > 0 ? averageTicketSize : 1),
                    TotalCustomers = totalCustomers,
                    NewMembers = newMembers,
                    ReturnCustomers = totalCustomers - newMembers,
                    CustomerRetentionRate = totalCustomers > 0 ? ((decimal)(totalCustomers - newMembers) / totalCustomers) * 100 : 0,
                    SalesTrend = await CalculateTrendIndicatorAsync(totalRevenue, totalRevenue * 0.9m),
                    ProfitTrend = await CalculateTrendIndicatorAsync(grossProfit, grossProfit * 0.9m),
                    ProductivityTrend = await CalculateTrendIndicatorAsync(salesPerEmployee, salesPerEmployee * 0.9m),
                    RevenueGrowth = 10.0m, // Placeholder
                    ProfitGrowth = 8.0m, // Placeholder
                    TransactionGrowth = 5.0m // Placeholder
                };

                metric.PerformanceScore = await CalculatePerformanceScoreAsync(metric);
                metrics.Add(metric);
            }

            // Calculate rankings
            var rankedMetrics = metrics
                .OrderByDescending(m => m.PerformanceScore)
                .Select((m, index) => { m.Ranking = index + 1; return m; })
                .ToList();

            // Calculate market share
            var totalMarketRevenue = metrics.Sum(m => m.TotalRevenue);
            foreach (var metric in rankedMetrics)
            {
                metric.MarketSharePercent = totalMarketRevenue > 0 
                    ? (metric.TotalRevenue / totalMarketRevenue) * 100 
                    : 0;
            }

            return rankedMetrics;
        }

        public Task<ConsolidatedSalesMetricsDto> CalculateConsolidatedMetricsAsync(List<BranchSalesMetricsDto> branchMetrics)
        {
            if (!branchMetrics.Any())
                return Task.FromResult(new ConsolidatedSalesMetricsDto());

            var topPerformer = branchMetrics.OrderByDescending(m => m.PerformanceScore).FirstOrDefault() 
                ?? new BranchSalesMetricsDto();
            var bottomPerformer = branchMetrics.OrderBy(m => m.PerformanceScore).FirstOrDefault() 
                ?? new BranchSalesMetricsDto();

            return Task.FromResult(new ConsolidatedSalesMetricsDto
            {
                TotalRevenue = branchMetrics.Sum(m => m.TotalRevenue),
                TotalTransactions = branchMetrics.Sum(m => m.TransactionCount),
                AverageTicketSize = branchMetrics.Average(m => m.AverageTicketSize),
                TotalGrossProfit = branchMetrics.Sum(m => m.GrossProfit),
                ConsolidatedGrossProfitMargin = branchMetrics.Average(m => m.GrossProfitMargin),
                TotalNetProfit = branchMetrics.Sum(m => m.NetProfit),
                ConsolidatedNetProfitMargin = branchMetrics.Average(m => m.NetProfitMargin),
                TotalEmployees = branchMetrics.Sum(m => m.EmployeeCount),
                AverageSalesPerEmployee = branchMetrics.Average(m => m.SalesPerEmployee),
                TotalCustomers = branchMetrics.Sum(m => m.TotalCustomers),
                TotalNewMembers = branchMetrics.Sum(m => m.NewMembers),
                OverallCustomerRetentionRate = branchMetrics.Average(m => m.CustomerRetentionRate),
                ActiveBranches = branchMetrics.Count(m => m.IsActive),
                AveragePerformanceScore = branchMetrics.Average(m => m.PerformanceScore),
                TopPerformer = topPerformer,
                BottomPerformer = bottomPerformer
            });
        }

        public async Task<List<BranchInventoryDto>> CalculateBranchInventoryMetricsAsync(List<int> accessibleBranchIds)
        {
            var branches = await _context.Branches
                .Where(b => accessibleBranchIds.Contains(b.Id))
                .ToListAsync();

            var inventoryMetrics = new List<BranchInventoryDto>();

            foreach (var branch in branches)
            {
                // Get product count (simplified - would need branch-specific inventory)
                var totalProducts = await _context.Products.CountAsync();
                var inStockProducts = await _context.Products.Where(p => p.Stock > 0).CountAsync();
                var lowStockProducts = await _context.Products.Where(p => p.Stock <= p.MinimumStock && p.Stock > 0).CountAsync();
                var outOfStockProducts = await _context.Products.Where(p => p.Stock == 0).CountAsync();
                
                var totalInventoryValue = await _context.Products.SumAsync(p => p.Stock * p.BuyPrice);

                inventoryMetrics.Add(new BranchInventoryDto
                {
                    BranchId = branch.Id,
                    BranchName = branch.BranchName,
                    BranchCode = branch.BranchCode,
                    City = branch.City,
                    Province = branch.Province,
                    TotalProducts = totalProducts,
                    InStockProducts = inStockProducts,
                    LowStockProducts = lowStockProducts,
                    OutOfStockProducts = outOfStockProducts,
                    TotalInventoryValue = totalInventoryValue,
                    InventoryTurnoverRate = 12.0m, // Placeholder
                    AverageStockLevel = totalProducts > 0 ? await _context.Products.AverageAsync(p => (decimal)p.Stock) : 0,
                    LastStockUpdate = _timezoneService.Now,
                    InventoryTrend = TrendIndicator.Stable
                });
            }

            return inventoryMetrics;
        }

        public async Task<List<RegionalPerformanceDto>> CalculateRegionalPerformanceAsync(ConsolidatedReportQueryParams queryParams, List<int> accessibleBranchIds)
        {
            var branchMetrics = await CalculateBranchSalesMetricsAsync(queryParams, accessibleBranchIds);
            
            var regionalGroups = branchMetrics.GroupBy(m => m.Province);
            var regionalPerformance = new List<RegionalPerformanceDto>();

            var totalRevenue = branchMetrics.Sum(m => m.TotalRevenue);

            foreach (var group in regionalGroups)
            {
                var regionBranches = group.ToList();
                var topBranches = regionBranches.OrderByDescending(b => b.PerformanceScore).Take(3).ToList();
                var bottomBranches = regionBranches.OrderBy(b => b.PerformanceScore).Take(3).ToList();

                var regionalRevenue = regionBranches.Sum(b => b.TotalRevenue);
                var marketShare = totalRevenue > 0 ? (regionalRevenue / totalRevenue) * 100 : 0;

                regionalPerformance.Add(new RegionalPerformanceDto
                {
                    Region = group.Key,
                    BranchCount = regionBranches.Count,
                    ActiveBranches = regionBranches.Count(b => b.IsActive),
                    TotalRevenue = regionalRevenue,
                    TotalTransactions = regionBranches.Sum(b => b.TransactionCount),
                    AverageTicketSize = regionBranches.Average(b => b.AverageTicketSize),
                    TotalProfit = regionBranches.Sum(b => b.NetProfit),
                    ProfitMargin = regionBranches.Average(b => b.NetProfitMargin),
                    TotalEmployees = regionBranches.Sum(b => b.EmployeeCount),
                    SalesPerEmployee = regionBranches.Average(b => b.SalesPerEmployee),
                    MarketShare = marketShare,
                    Ranking = 0, // Will be calculated after all regions
                    SalesTrend = CalculateTrendFromBranches(regionBranches),
                    ProfitTrend = CalculateTrendFromBranches(regionBranches),
                    TopBranches = topBranches,
                    BottomBranches = bottomBranches
                });
            }

            // Calculate regional rankings
            var rankedRegions = regionalPerformance
                .OrderByDescending(r => r.TotalRevenue)
                .Select((r, index) => { r.Ranking = index + 1; return r; })
                .ToList();

            return rankedRegions;
        }

        public Task<decimal> CalculatePerformanceScoreAsync(BranchSalesMetricsDto metrics)
        {
            // Weighted performance score calculation
            var revenueWeight = 0.3m;
            var profitWeight = 0.25m;
            var productivityWeight = 0.2m;
            var growthWeight = 0.15m;
            var customerWeight = 0.1m;

            // Normalize values (simplified scoring)
            var revenueScore = Math.Min(metrics.TotalRevenue / 1000000m, 1.0m) * 100; // Cap at 1M
            var profitScore = Math.Min(metrics.NetProfitMargin / 20m, 1.0m) * 100; // Cap at 20%
            var productivityScore = Math.Min(metrics.SalesPerEmployee / 100000m, 1.0m) * 100; // Cap at 100k
            var growthScore = Math.Min(metrics.RevenueGrowth / 50m, 1.0m) * 100; // Cap at 50%
            var customerScore = Math.Min(metrics.CustomerRetentionRate / 100m, 1.0m) * 100; // Cap at 100%

            var totalScore = (revenueScore * revenueWeight) +
                           (profitScore * profitWeight) +
                           (productivityScore * productivityWeight) +
                           (growthScore * growthWeight) +
                           (customerScore * customerWeight);

            return Task.FromResult(totalScore);
        }

        public Task<PerformanceBenchmarkDto> CalculatePerformanceBenchmarksAsync(List<BranchSalesMetricsDto> branchMetrics)
        {
            if (!branchMetrics.Any())
                return Task.FromResult(new PerformanceBenchmarkDto());

            var orderedByRevenue = branchMetrics.OrderBy(m => m.TotalRevenue).ToList();
            var orderedByProfit = branchMetrics.OrderBy(m => m.NetProfitMargin).ToList();
            var orderedByProductivity = branchMetrics.OrderBy(m => m.SalesPerEmployee).ToList();

            var count = branchMetrics.Count;
            var topQuartileIndex = (int)(count * 0.75);
            var medianIndex = count / 2;
            var bottomQuartileIndex = (int)(count * 0.25);

            var storeSizeBenchmarks = branchMetrics
                .GroupBy(m => m.StoreSize)
                .ToDictionary(g => g.Key, g => g.Average(m => m.PerformanceScore));

            var regionalBenchmarks = branchMetrics
                .GroupBy(m => m.Province)
                .ToDictionary(g => g.Key, g => g.Average(m => m.PerformanceScore));

            return Task.FromResult(new PerformanceBenchmarkDto
            {
                TopQuartileRevenue = orderedByRevenue[topQuartileIndex].TotalRevenue,
                MedianRevenue = orderedByRevenue[medianIndex].TotalRevenue,
                BottomQuartileRevenue = orderedByRevenue[bottomQuartileIndex].TotalRevenue,
                TopQuartileProfitMargin = orderedByProfit[topQuartileIndex].NetProfitMargin,
                MedianProfitMargin = orderedByProfit[medianIndex].NetProfitMargin,
                BottomQuartileProfitMargin = orderedByProfit[bottomQuartileIndex].NetProfitMargin,
                TopQuartileProductivity = orderedByProductivity[topQuartileIndex].SalesPerEmployee,
                MedianProductivity = orderedByProductivity[medianIndex].SalesPerEmployee,
                BottomQuartileProductivity = orderedByProductivity[bottomQuartileIndex].SalesPerEmployee,
                StoreSizeBenchmarks = storeSizeBenchmarks,
                RegionalBenchmarks = regionalBenchmarks
            });
        }

        public Task<List<PerformanceInsightDto>> GeneratePerformanceInsightsAsync(List<BranchSalesMetricsDto> branchMetrics)
        {
            var insights = new List<PerformanceInsightDto>();

            // Revenue insights
            var lowPerformingBranches = branchMetrics.Where(m => m.PerformanceScore < 50).ToList();
            if (lowPerformingBranches.Any())
            {
                insights.Add(new PerformanceInsightDto
                {
                    Category = "Performance",
                    Insight = $"{lowPerformingBranches.Count} branches are performing below average",
                    ActionRecommendation = "Focus on operational improvements and staff training",
                    AffectedBranchIds = lowPerformingBranches.Select(b => b.BranchId).ToList(),
                    Priority = lowPerformingBranches.Count > 3 ? "High" : "Medium",
                    PotentialImpact = lowPerformingBranches.Sum(b => 100 - b.PerformanceScore)
                });
            }

            // Profit margin insights
            var lowMarginBranches = branchMetrics.Where(m => m.NetProfitMargin < 10).ToList();
            if (lowMarginBranches.Any())
            {
                insights.Add(new PerformanceInsightDto
                {
                    Category = "Profit",
                    Insight = $"{lowMarginBranches.Count} branches have profit margins below 10%",
                    ActionRecommendation = "Review pricing strategy and cost management",
                    AffectedBranchIds = lowMarginBranches.Select(b => b.BranchId).ToList(),
                    Priority = "High",
                    PotentialImpact = lowMarginBranches.Sum(b => 10 - b.NetProfitMargin)
                });
            }

            // Productivity insights
            var avgProductivity = branchMetrics.Average(m => m.SalesPerEmployee);
            var lowProductivityBranches = branchMetrics.Where(m => m.SalesPerEmployee < avgProductivity * 0.8m).ToList();
            if (lowProductivityBranches.Any())
            {
                insights.Add(new PerformanceInsightDto
                {
                    Category = "Productivity",
                    Insight = $"{lowProductivityBranches.Count} branches have below-average employee productivity",
                    ActionRecommendation = "Implement staff training and performance improvement programs",
                    AffectedBranchIds = lowProductivityBranches.Select(b => b.BranchId).ToList(),
                    Priority = "Medium",
                    PotentialImpact = (avgProductivity - lowProductivityBranches.Average(b => b.SalesPerEmployee)) * lowProductivityBranches.Sum(b => b.EmployeeCount)
                });
            }

            return Task.FromResult(insights);
        }

        public async Task<List<GrowthOpportunityDto>> IdentifyGrowthOpportunitiesAsync(ConsolidatedReportQueryParams queryParams, List<int> accessibleBranchIds)
        {
            var opportunities = new List<GrowthOpportunityDto>();

            // Market gap analysis
            var branchMetrics = await CalculateBranchSalesMetricsAsync(queryParams, accessibleBranchIds);
            var regionalPerformance = branchMetrics.GroupBy(m => m.Province);

            foreach (var region in regionalPerformance)
            {
                var avgRevenue = region.Average(b => b.TotalRevenue);
                var topPerformer = region.OrderByDescending(b => b.TotalRevenue).FirstOrDefault();
                
                if (topPerformer != null && topPerformer.TotalRevenue > avgRevenue * 1.5m)
                {
                    opportunities.Add(new GrowthOpportunityDto
                    {
                        Region = region.Key,
                        City = "Regional",
                        OpportunityType = "Market Gap",
                        Description = $"Significant performance gap exists in {region.Key} region",
                        PotentialRevenue = (topPerformer.TotalRevenue - avgRevenue) * (region.Count() - 1),
                        InvestmentRequired = 500000m, // Placeholder
                        ExpectedROI = 150m, // Placeholder
                        Priority = "High",
                        Recommendation = "Implement best practices from top performer across region"
                    });
                }
            }

            return opportunities;
        }

        public async Task<List<CriticalAlertDto>> GenerateCriticalAlertsAsync(List<int> accessibleBranchIds)
        {
            var alerts = new List<CriticalAlertDto>();

            // Performance alerts
            var branchMetrics = await CalculateBranchSalesMetricsAsync(new ConsolidatedReportQueryParams(), accessibleBranchIds);
            var criticallyLowPerformers = branchMetrics.Where(m => m.PerformanceScore < 30).ToList();
            
            if (criticallyLowPerformers.Any())
            {
                alerts.Add(new CriticalAlertDto
                {
                    AlertType = "Performance",
                    Severity = "Critical",
                    Message = $"{criticallyLowPerformers.Count} branches have critically low performance scores",
                    AffectedBranchIds = criticallyLowPerformers.Select(b => b.BranchId).ToList(),
                    ImmediateAction = "Immediate operational review and intervention required",
                    DetectedAt = _timezoneService.Now,
                    RequiresEscalation = true
                });
            }

            // Inventory alerts
            var outOfStockCount = await _context.Products.CountAsync(p => p.Stock == 0);
            if (outOfStockCount > 50)
            {
                alerts.Add(new CriticalAlertDto
                {
                    AlertType = "Inventory",
                    Severity = "High",
                    Message = $"{outOfStockCount} products are out of stock",
                    AffectedBranchIds = accessibleBranchIds,
                    ImmediateAction = "Urgent restocking required",
                    DetectedAt = _timezoneService.Now,
                    RequiresEscalation = outOfStockCount > 100
                });
            }

            return alerts;
        }

        public async Task<StrategicRecommendationDto> GenerateStrategicRecommendationsAsync(ConsolidatedReportQueryParams queryParams, List<int> accessibleBranchIds)
        {
            var branchMetrics = await CalculateBranchSalesMetricsAsync(queryParams, accessibleBranchIds);
            var regionalPerformance = await CalculateRegionalPerformanceAsync(queryParams, accessibleBranchIds);

            var shortTermActions = new List<string>
            {
                "Implement weekly performance reviews for underperforming branches",
                "Optimize inventory levels based on turnover analysis",
                "Launch customer retention program in branches with low retention rates"
            };

            var mediumTermInitiatives = new List<string>
            {
                "Expand high-performing branch models to underperforming regions",
                "Implement advanced analytics for demand forecasting",
                "Develop regional manager training programs"
            };

            var longTermStrategy = new List<string>
            {
                "Consider strategic expansion in high-potential markets",
                "Develop omnichannel retail capabilities",
                "Implement AI-driven operational optimization"
            };

            var investmentOpportunities = new List<InvestmentOpportunityDto>
            {
                new InvestmentOpportunityDto
                {
                    OpportunityType = "Technology",
                    Description = "POS system upgrade and analytics platform",
                    InvestmentAmount = 2000000m,
                    ExpectedReturn = 3500000m,
                    Timeframe = "12-18 months",
                    RiskLevel = "Medium"
                }
            };

            var riskFactors = new List<RiskFactorDto>
            {
                new RiskFactorDto
                {
                    RiskType = "Operational",
                    Description = "High staff turnover in underperforming branches",
                    Probability = "Medium",
                    Impact = "High",
                    MitigationStrategy = "Improve compensation and training programs"
                }
            };

            return new StrategicRecommendationDto
            {
                ShortTermActions = shortTermActions,
                MediumTermInitiatives = mediumTermInitiatives,
                LongTermStrategy = longTermStrategy,
                InvestmentOpportunities = investmentOpportunities,
                RiskFactors = riskFactors
            };
        }

        public Task<TrendIndicator> CalculateTrendIndicatorAsync(decimal currentValue, decimal previousValue)
        {
            if (previousValue == 0) return Task.FromResult(TrendIndicator.Stable);

            var changePercent = ((currentValue - previousValue) / previousValue) * 100;

            var result = changePercent switch
            {
                > 20 => TrendIndicator.StronglyUp,
                > 10 => TrendIndicator.Up,
                > 2 => TrendIndicator.SlightlyUp,
                >= -2 => TrendIndicator.Stable,
                >= -10 => TrendIndicator.SlightlyDown,
                >= -20 => TrendIndicator.Down,
                _ => TrendIndicator.StronglyDown
            };
            
            return Task.FromResult(result);
        }

        public async Task<List<TrendDataPointDto>> CalculateTrendDataAsync(ConsolidatedReportQueryParams queryParams, string metricType, List<int> accessibleBranchIds)
        {
            var dateRange = await GetDateRangeFromParams(queryParams);
            var trendData = new List<TrendDataPointDto>();

            // Generate trend data points (simplified)
            var startDate = dateRange[0];
            var endDate = dateRange[1];
            var totalDays = (endDate - startDate).Days;
            var interval = totalDays > 90 ? 7 : 1; // Weekly for long periods, daily for short

            for (var date = startDate; date <= endDate; date = date.AddDays(interval))
            {
                // Get metrics for this date (placeholder calculation)
                var value = await CalculateMetricValueForDateAsync(date, metricType, accessibleBranchIds);
                var previousValue = await CalculateMetricValueForDateAsync(date.AddDays(-interval), metricType, accessibleBranchIds);
                
                var changePercent = previousValue > 0 ? ((value - previousValue) / previousValue) * 100 : 0;
                var indicator = await CalculateTrendIndicatorAsync(value, previousValue);

                trendData.Add(new TrendDataPointDto
                {
                    Date = date,
                    Value = value,
                    PreviousPeriodValue = previousValue,
                    ChangePercent = changePercent,
                    Indicator = indicator
                });
            }

            return trendData;
        }

        public Task<ForecastDto> GenerateForecastAsync(List<TrendDataPointDto> historicalData, int forecastPeriods = 12)
        {
            // Simple linear forecast (would use more sophisticated algorithms in production)
            if (!historicalData.Any()) 
                return Task.FromResult(new ForecastDto());

            var avgGrowthRate = historicalData.Average(h => h.ChangePercent);
            var lastValue = historicalData.LastOrDefault()?.Value ?? 0;
            var forecastData = new List<ForecastDataPointDto>();

            for (int i = 1; i <= forecastPeriods; i++)
            {
                var forecastValue = lastValue * (decimal)Math.Pow(1 + (double)(avgGrowthRate / 100), i);
                var confidence = Math.Max(0, 95 - (i * 5)); // Decrease confidence over time

                forecastData.Add(new ForecastDataPointDto
                {
                    Date = historicalData.Last().Date.AddMonths(i),
                    PredictedValue = forecastValue,
                    LowerBound = forecastValue * 0.9m,
                    UpperBound = forecastValue * 1.1m,
                    Confidence = confidence
                });
            }

            return Task.FromResult(new ForecastDto
            {
                NextPeriod = forecastData,
                ConfidenceLevel = forecastData.Average(f => f.Confidence),
                ForecastMethod = "Linear Trend Analysis",
                Assumptions = new List<string> { "Historical growth patterns continue", "No major market disruptions" }
            });
        }

        public async Task<List<int>> GetAccessibleBranchIdsForUserAsync(int userId)
        {
            var accessibleBranches = await _userBranchService.GetAccessibleBranchesForUserAsync(userId);
            return accessibleBranches.Select(b => b.BranchId).ToList();
        }

        public async Task<bool> CanUserAccessConsolidatedReportsAsync(int userId)
        {
            var userStatus = await _userBranchService.GetUserAssignmentStatusAsync(userId);
            return userStatus?.Role.ToUpper() is "ADMIN" or "HEADMANAGER" or "BRANCHMANAGER";
        }

        public Task<DateTime[]> GetDateRangeFromParams(ConsolidatedReportQueryParams queryParams)
        {
            var now = _timezoneService.Now;
            var startDate = now;
            var endDate = now;

            if (queryParams.StartDate.HasValue && queryParams.EndDate.HasValue)
            {
                startDate = queryParams.StartDate.Value;
                endDate = queryParams.EndDate.Value;
            }
            else
            {
                switch (queryParams.DateRange?.ToLower())
                {
                    case "today":
                        startDate = now.Date;
                        endDate = now.Date.AddDays(1).AddSeconds(-1);
                        break;
                    case "week":
                        startDate = now.AddDays(-7);
                        endDate = now;
                        break;
                    case "month":
                        startDate = now.AddDays(-30);
                        endDate = now;
                        break;
                    case "year":
                        startDate = now.AddDays(-365);
                        endDate = now;
                        break;
                    default:
                        startDate = now.AddDays(-30);
                        endDate = now;
                        break;
                }
            }

            return Task.FromResult(new[] { startDate, endDate });
        }

        public Task CacheReportDataAsync(string cacheKey, object data, TimeSpan expiration)
        {
            _cache.Set(cacheKey, data, expiration);
            return Task.CompletedTask;
        }

        public Task<T?> GetCachedReportDataAsync<T>(string cacheKey) where T : class
        {
            return Task.FromResult(_cache.Get<T>(cacheKey));
        }

        // Helper methods (private)
        private string GenerateSalesReportSummary(List<BranchSalesMetricsDto> branchMetrics, ConsolidatedSalesMetricsDto consolidated)
        {
            var topPerformer = branchMetrics.OrderByDescending(b => b.PerformanceScore).FirstOrDefault();
            var growthCount = branchMetrics.Count(b => b.RevenueGrowth > 0);
            
            return $"Total revenue: {consolidated.TotalRevenue:C}. " +
                   $"{growthCount}/{branchMetrics.Count} branches showing growth. " +
                   $"Top performer: {topPerformer?.BranchName ?? "N/A"}.";
        }

        private ConsolidatedInventoryDto CalculateConsolidatedInventory(List<BranchInventoryDto> branchInventories)
        {
            if (!branchInventories.Any())
                return new ConsolidatedInventoryDto();

            return new ConsolidatedInventoryDto
            {
                TotalProducts = branchInventories.Sum(b => b.TotalProducts),
                TotalInStock = branchInventories.Sum(b => b.InStockProducts),
                TotalLowStock = branchInventories.Sum(b => b.LowStockProducts),
                TotalOutOfStock = branchInventories.Sum(b => b.OutOfStockProducts),
                TotalInventoryValue = branchInventories.Sum(b => b.TotalInventoryValue),
                AverageInventoryTurnover = branchInventories.Average(b => b.InventoryTurnoverRate),
                OverallStockHealth = branchInventories.Average(b => (decimal)b.InStockProducts / b.TotalProducts * 100),
                BranchesWithLowStock = branchInventories.Count(b => b.LowStockProducts > 0),
                BranchesWithOutOfStock = branchInventories.Count(b => b.OutOfStockProducts > 0)
            };
        }

        private async Task<List<LowStockAlertDto>> GenerateLowStockAlertsAsync(List<int> accessibleBranchIds)
        {
            var lowStockProducts = await _context.Products
                .Where(p => p.Stock <= p.MinimumStock && p.Stock > 0)
                .ToListAsync();

            return lowStockProducts.Select(p => new LowStockAlertDto
            {
                ProductId = p.Id,
                ProductName = p.Name,
                ProductCode = p.Barcode,
                Category = p.Category?.Name ?? "Unknown",
                BranchId = 1, // Placeholder - would need branch-specific inventory
                BranchName = "All Branches",
                CurrentStock = p.Stock,
                MinimumStock = p.MinimumStock,
                Severity = p.Stock == 0 ? "Critical" : p.Stock <= p.MinimumStock * 0.5 ? "High" : "Warning",
                DaysSinceLastRestock = 7, // Placeholder
                EstimatedDaysRemaining = p.Stock / 5m, // Placeholder calculation
                RequiresImmediateAction = p.Stock <= p.MinimumStock * 0.2
            }).ToList();
        }

        private async Task<List<InventoryMovementDto>> GetFastMovingItemsAsync(List<int> accessibleBranchIds)
        {
            // Get top selling products from sales data
            var topProducts = await _context.SaleItems
                .GroupBy(si => new { si.ProductId, si.ProductName })
                .Select(g => new InventoryMovementDto
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    ProductCode = g.First().ProductBarcode,
                    Category = "General",
                    TotalSold = g.Sum(si => si.Quantity),
                    TurnoverRate = 24.0m, // Placeholder
                    Revenue = g.Sum(si => si.Subtotal),
                    MovementTrend = TrendIndicator.Up
                })
                .OrderByDescending(p => p.TotalSold)
                .Take(10)
                .ToListAsync();

            return topProducts;
        }

        private async Task<List<InventoryMovementDto>> GetSlowMovingItemsAsync(List<int> accessibleBranchIds)
        {
            // Get least selling products
            var slowProducts = await _context.SaleItems
                .GroupBy(si => new { si.ProductId, si.ProductName })
                .Select(g => new InventoryMovementDto
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    ProductCode = g.First().ProductBarcode,
                    Category = "General",
                    TotalSold = g.Sum(si => si.Quantity),
                    TurnoverRate = 2.0m, // Placeholder
                    Revenue = g.Sum(si => si.Subtotal),
                    MovementTrend = TrendIndicator.Down
                })
                .OrderBy(p => p.TotalSold)
                .Take(10)
                .ToListAsync();

            return slowProducts;
        }

        private Dictionary<string, decimal> CalculateMarketShareByRegion(List<RegionalPerformanceDto> regionalPerformance)
        {
            var totalRevenue = regionalPerformance.Sum(r => r.TotalRevenue);
            return regionalPerformance.ToDictionary(
                r => r.Region,
                r => totalRevenue > 0 ? (r.TotalRevenue / totalRevenue) * 100 : 0
            );
        }

        private RegionalComparisonDto CalculateRegionalComparison(List<RegionalPerformanceDto> regionalPerformance)
        {
            if (!regionalPerformance.Any())
                return new RegionalComparisonDto();

            var topRegion = regionalPerformance.OrderByDescending(r => r.TotalRevenue).First();
            var bottomRegion = regionalPerformance.OrderBy(r => r.TotalRevenue).First();

            return new RegionalComparisonDto
            {
                TopPerformingRegion = topRegion.Region,
                BottomPerformingRegion = bottomRegion.Region,
                PerformanceGap = topRegion.TotalRevenue - bottomRegion.TotalRevenue,
                AverageRegionalRevenue = regionalPerformance.Average(r => r.TotalRevenue),
                RegionalGrowthRates = regionalPerformance.ToDictionary(r => r.Region, r => 10.5m), // Placeholder
                RegionalInsights = regionalPerformance.ToDictionary(r => r.Region, r => $"Strong performance in {r.Region}")
            };
        }

        private Task<List<BranchPerformanceRankingDto>> CalculateBranchRankingsAsync(List<BranchSalesMetricsDto> branchMetrics)
        {
            var rankings = new List<BranchPerformanceRankingDto>();

            for (int i = 0; i < branchMetrics.Count; i++)
            {
                var branch = branchMetrics[i];
                var scoreBreakdown = new Dictionary<string, decimal>
                {
                    ["Revenue"] = Math.Min(branch.TotalRevenue / 1000000m * 100, 100),
                    ["Profit Margin"] = branch.NetProfitMargin,
                    ["Productivity"] = Math.Min(branch.SalesPerEmployee / 100000m * 100, 100),
                    ["Growth"] = Math.Min(branch.RevenueGrowth * 2, 100),
                    ["Customer Retention"] = branch.CustomerRetentionRate
                };

                var strengthAreas = new Dictionary<string, string>();
                var improvementAreas = new Dictionary<string, string>();

                // Determine strengths and improvement areas
                if (branch.NetProfitMargin > 15)
                    strengthAreas["Profitability"] = "Strong profit margins";
                else if (branch.NetProfitMargin < 5)
                    improvementAreas["Profitability"] = "Low profit margins need attention";

                if (branch.SalesPerEmployee > 75000)
                    strengthAreas["Productivity"] = "High employee productivity";
                else if (branch.SalesPerEmployee < 30000)
                    improvementAreas["Productivity"] = "Employee productivity below average";

                var performanceCategory = branch.PerformanceScore switch
                {
                    >= 90 => "Excellent",
                    >= 75 => "Good",
                    >= 60 => "Average",
                    >= 40 => "Below Average",
                    _ => "Poor"
                };

                rankings.Add(new BranchPerformanceRankingDto
                {
                    Rank = i + 1,
                    PreviousRank = i + 1, // Placeholder
                    RankChange = 0, // Placeholder
                    Branch = branch,
                    OverallScore = branch.PerformanceScore,
                    ScoreBreakdown = scoreBreakdown,
                    StrengthAreas = strengthAreas,
                    ImprovementAreas = improvementAreas,
                    PerformanceCategory = performanceCategory
                });
            }

            return Task.FromResult(rankings);
        }

        private TrendIndicator CalculateTrendFromBranches(List<BranchSalesMetricsDto> branches)
        {
            var avgGrowth = branches.Average(b => b.RevenueGrowth);
            return avgGrowth switch
            {
                > 20 => TrendIndicator.StronglyUp,
                > 10 => TrendIndicator.Up,
                > 2 => TrendIndicator.SlightlyUp,
                >= -2 => TrendIndicator.Stable,
                >= -10 => TrendIndicator.SlightlyDown,
                >= -20 => TrendIndicator.Down,
                _ => TrendIndicator.StronglyDown
            };
        }

        private async Task<RealTimeSalesDto> CalculateTodaySalesAsync(List<int> accessibleBranchIds)
        {
            var today = _timezoneService.Today;
            var currentHour = _timezoneService.Now.Hour;

            var todaySales = await _context.Sales
                .Where(s => s.SaleDate.Date == today)
                .ToListAsync();

            var currentHourSales = todaySales
                .Where(s => s.SaleDate.Hour == currentHour)
                .ToList();

            var todayRevenue = todaySales.Sum(s => s.Total);
            var todayTransactions = todaySales.Count;
            var currentHourRevenue = currentHourSales.Sum(s => s.Total);
            var currentHourTransactions = currentHourSales.Count;

            // Calculate projected daily revenue based on current hour performance
            var avgHourlyRevenue = todayRevenue / Math.Max(currentHour, 1);
            var projectedDailyRevenue = avgHourlyRevenue * 16; // Assume 16 operating hours

            return new RealTimeSalesDto
            {
                TodayRevenue = todayRevenue,
                TodayTransactions = todayTransactions,
                CurrentHourRevenue = currentHourRevenue,
                CurrentHourTransactions = currentHourTransactions,
                AverageTicketToday = todayTransactions > 0 ? todayRevenue / todayTransactions : 0,
                HourlyTrend = TrendIndicator.Stable, // Would calculate based on previous hour
                ProjectedDailyRevenue = projectedDailyRevenue,
                TargetAchievement = 85.5m // Placeholder
            };
        }

        private async Task<List<BranchRealTimeDto>> CalculateBranchRealTimeMetricsAsync(List<int> accessibleBranchIds)
        {
            var branches = await _context.Branches
                .Where(b => accessibleBranchIds.Contains(b.Id))
                .Include(b => b.Users)
                .ToListAsync();

            var today = _timezoneService.Today;
            var realTimeMetrics = new List<BranchRealTimeDto>();

            foreach (var branch in branches)
            {
                var todaySales = await _context.Sales
                    .Where(s => s.SaleDate.Date == today)
                    .ToListAsync(); // Would filter by branch

                var lastSale = todaySales.OrderByDescending(s => s.SaleDate).FirstOrDefault();

                realTimeMetrics.Add(new BranchRealTimeDto
                {
                    BranchId = branch.Id,
                    BranchName = branch.BranchName,
                    IsOpen = _timezoneService.Now.Hour >= 8 && _timezoneService.Now.Hour <= 20,
                    ActiveEmployees = branch.Users.Count(u => u.IsActive),
                    TodayRevenue = todaySales.Sum(s => s.Total),
                    TodayTransactions = todaySales.Count,
                    LastTransactionTime = lastSale?.SaleDate ?? DateTime.MinValue,
                    Status = DetermineStoreStatus(todaySales, _timezoneService.Now),
                    Alerts = new List<string>()
                });
            }

            return realTimeMetrics;
        }

        private async Task<LiveInventoryStatusDto> CalculateLiveInventoryStatusAsync(List<int> accessibleBranchIds)
        {
            var criticalStockCount = await _context.Products.CountAsync(p => p.Stock == 0);
            var lowStockCount = await _context.Products.CountAsync(p => p.Stock > 0 && p.Stock <= p.MinimumStock);
            var totalProducts = await _context.Products.CountAsync();

            var urgentRestock = await _context.Products
                .Where(p => p.Stock == 0)
                .Take(5)
                .Select(p => p.Name)
                .ToListAsync();

            var stockHealth = totalProducts > 0 
                ? ((decimal)(totalProducts - criticalStockCount - lowStockCount) / totalProducts) * 100 
                : 100;

            return new LiveInventoryStatusDto
            {
                CriticalStockItems = criticalStockCount,
                LowStockItems = lowStockCount,
                OutOfStockItems = criticalStockCount,
                UrgentRestockNeeded = urgentRestock,
                OverallStockHealth = stockHealth
            };
        }

        private async Task<SystemHealthDto> CalculateSystemHealthAsync()
        {
            var lastSale = await _context.Sales.OrderByDescending(s => s.SaleDate).FirstOrDefaultAsync();
            var lastSaleTime = lastSale?.SaleDate ?? DateTime.MinValue;
            var dataFreshness = (DateTime.UtcNow - lastSaleTime).TotalMinutes;

            var systemAlerts = new List<string>();
            if (dataFreshness > 60)
                systemAlerts.Add("Sales data may be stale");

            return new SystemHealthDto
            {
                AllSystemsOperational = !systemAlerts.Any(),
                SystemAlerts = systemAlerts,
                DataFreshnessScore = Math.Max(0, 100 - (decimal)(dataFreshness / 60 * 10)),
                LastDataSync = lastSaleTime
            };
        }

        private Task<ExecutiveKPIsDto> CalculateExecutiveKPIsAsync(ConsolidatedSalesMetricsDto consolidated, List<BranchSalesMetricsDto> branchMetrics, DateTime[] dateRange)
        {
            return Task.FromResult(new ExecutiveKPIsDto
            {
                TotalRevenue = consolidated.TotalRevenue,
                RevenueGrowth = 12.5m, // Placeholder
                NetProfitMargin = consolidated.ConsolidatedNetProfitMargin,
                ProfitGrowthRate = 8.3m, // Placeholder
                TotalTransactions = consolidated.TotalTransactions,
                TransactionGrowth = 5.7m, // Placeholder
                AverageTicketSize = consolidated.AverageTicketSize,
                TicketSizeGrowth = 3.2m, // Placeholder
                ActiveBranches = consolidated.ActiveBranches,
                BranchEfficiencyScore = branchMetrics.Average(b => b.PerformanceScore),
                TotalEmployees = consolidated.TotalEmployees,
                EmployeeProductivity = consolidated.AverageSalesPerEmployee,
                CustomerSatisfactionScore = 85.5m, // Placeholder
                InventoryTurnover = 12.0m, // Placeholder
                MarketShareGrowth = 2.1m // Placeholder
            });
        }

        private Task<List<KeyInsightDto>> GenerateKeyInsightsAsync(List<BranchSalesMetricsDto> branchMetrics, ConsolidatedSalesMetricsDto consolidated)
        {
            var insights = new List<KeyInsightDto>();

            // Revenue insight
            var topPerformer = branchMetrics.OrderByDescending(b => b.TotalRevenue).FirstOrDefault();
            if (topPerformer != null)
            {
                insights.Add(new KeyInsightDto
                {
                    Title = "Top Revenue Performer",
                    Description = $"{topPerformer.BranchName} leads with {topPerformer.TotalRevenue:C} revenue",
                    Impact = "Positive",
                    Value = topPerformer.TotalRevenue,
                    MetricType = "Revenue",
                    Trend = topPerformer.SalesTrend,
                    ActionRequired = "Analyze and replicate success factors"
                });
            }

            // Profit margin insight
            var avgMargin = branchMetrics.Average(b => b.NetProfitMargin);
            insights.Add(new KeyInsightDto
            {
                Title = "Average Profit Margin",
                Description = $"Overall profit margin at {avgMargin:F1}%",
                Impact = avgMargin > 10 ? "Positive" : "Negative",
                Value = avgMargin,
                MetricType = "Profit",
                Trend = TrendIndicator.Stable,
                ActionRequired = avgMargin < 10 ? "Focus on cost optimization" : "Maintain current performance"
            });

            return Task.FromResult(insights);
        }

        private Task<CompetitivePositionDto> CalculateCompetitivePositionAsync(ConsolidatedSalesMetricsDto consolidated)
        {
            return Task.FromResult(new CompetitivePositionDto
            {
                MarketPosition = "Strong Regional Player",
                CompetitiveAdvantages = new List<string>
                {
                    "Multi-branch operational efficiency",
                    "Strong customer retention rates",
                    "Comprehensive product portfolio"
                },
                Challenges = new List<string>
                {
                    "Inventory optimization across branches",
                    "Standardizing performance across regions"
                },
                MarketShare = 15.5m, // Placeholder
                MarketTrend = "Growing"
            });
        }

        private TrendSummaryDto CalculateTrendSummary(List<TrendDataPointDto> trendData)
        {
            if (!trendData.Any())
                return new TrendSummaryDto();

            var avgGrowthRate = trendData.Average(t => t.ChangePercent);
            var positivePoints = trendData.Count(t => t.ChangePercent > 0);
            var negativePoints = trendData.Count(t => t.ChangePercent < 0);

            var overallTrend = avgGrowthRate switch
            {
                > 10 => TrendIndicator.StronglyUp,
                > 5 => TrendIndicator.Up,
                > 1 => TrendIndicator.SlightlyUp,
                >= -1 => TrendIndicator.Stable,
                >= -5 => TrendIndicator.SlightlyDown,
                >= -10 => TrendIndicator.Down,
                _ => TrendIndicator.StronglyDown
            };

            var description = avgGrowthRate > 0 ? "Positive growth trend" : "Declining trend";
            var volatility = trendData.Any() ? trendData.Select(t => t.ChangePercent).ToList().Select(x => Math.Abs(x - avgGrowthRate)).Average() : 0;

            return new TrendSummaryDto
            {
                OverallTrend = overallTrend,
                OverallGrowthRate = avgGrowthRate,
                TrendDescription = description,
                ConsecutiveGrowthPeriods = positivePoints,
                ConsecutiveDeclinePeriods = negativePoints,
                Volatility = (decimal)volatility
            };
        }

        private Task<List<SeasonalPatternDto>> CalculateSeasonalPatternsAsync(List<TrendDataPointDto> trendData)
        {
            // Simplified seasonal analysis
            return Task.FromResult(new List<SeasonalPatternDto>
            {
                new SeasonalPatternDto
                {
                    Period = "Monthly",
                    PeakPeriod = "December",
                    LowPeriod = "February",
                    Seasonality = 15.5m,
                    Patterns = new List<string> { "Holiday season boost", "Post-holiday decline" }
                }
            });
        }

        private async Task<decimal> CalculateMetricValueForDateAsync(DateTime date, string metricType, List<int> accessibleBranchIds)
        {
            // Get sales for the specific date
            var sales = await _context.Sales
                .Where(s => s.SaleDate.Date == date.Date)
                .ToListAsync();

            return metricType.ToLower() switch
            {
                "sales" => sales.Sum(s => s.Total),
                "profit" => sales.Sum(s => s.Total * 0.2m), // Simplified profit calculation
                "transactions" => sales.Count,
                _ => 0
            };
        }

        private string DetermineStoreStatus(List<Sale> todaySales, DateTime currentTime)
        {
            var recentSales = todaySales.Where(s => (currentTime - s.SaleDate).TotalMinutes <= 30).Count();
            
            return recentSales switch
            {
                > 10 => "Busy",
                > 5 => "Active",
                > 0 => "Idle",
                _ => "Quiet"
            };
        }
    }
}