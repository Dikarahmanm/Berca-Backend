using Berca_Backend.DTOs;

namespace Berca_Backend.Services.Interfaces
{
    public interface IConsolidatedReportService
    {
        // Core Analytics Methods
        Task<SalesComparisonDto> GetSalesComparisonAsync(ConsolidatedReportQueryParams queryParams, int? requestingUserId = null);
        Task<InventoryOverviewDto> GetInventoryOverviewAsync(ConsolidatedReportQueryParams queryParams, int? requestingUserId = null);
        Task<RegionalDashboardDto> GetRegionalDashboardAsync(ConsolidatedReportQueryParams queryParams, int? requestingUserId = null);
        Task<BranchRankingDto> GetBranchRankingAsync(ConsolidatedReportQueryParams queryParams, int? requestingUserId = null);
        Task<ExecutiveSummaryDto> GetExecutiveSummaryAsync(ConsolidatedReportQueryParams queryParams, int? requestingUserId = null);

        // Real-time and Trend Analysis
        Task<RealTimeMetricsDto> GetRealTimeMetricsAsync(int? requestingUserId = null);
        Task<TrendAnalysisDto> GetTrendAnalysisAsync(ConsolidatedReportQueryParams queryParams, int? requestingUserId = null);

        // Export Functionality
        Task<byte[]> ExportReportAsync(ExportParams exportParams, int? requestingUserId = null);
        Task<string> GenerateReportUrlAsync(ExportParams exportParams, int? requestingUserId = null);

        // Data Aggregation Helpers
        Task<List<BranchSalesMetricsDto>> CalculateBranchSalesMetricsAsync(ConsolidatedReportQueryParams queryParams, List<int> accessibleBranchIds);
        Task<ConsolidatedSalesMetricsDto> CalculateConsolidatedMetricsAsync(List<BranchSalesMetricsDto> branchMetrics);
        Task<List<BranchInventoryDto>> CalculateBranchInventoryMetricsAsync(List<int> accessibleBranchIds);
        Task<List<RegionalPerformanceDto>> CalculateRegionalPerformanceAsync(ConsolidatedReportQueryParams queryParams, List<int> accessibleBranchIds);

        // Performance Calculations
        Task<decimal> CalculatePerformanceScoreAsync(BranchSalesMetricsDto metrics);
        Task<PerformanceBenchmarkDto> CalculatePerformanceBenchmarksAsync(List<BranchSalesMetricsDto> branchMetrics);
        Task<List<PerformanceInsightDto>> GeneratePerformanceInsightsAsync(List<BranchSalesMetricsDto> branchMetrics);

        // Business Intelligence
        Task<List<GrowthOpportunityDto>> IdentifyGrowthOpportunitiesAsync(ConsolidatedReportQueryParams queryParams, List<int> accessibleBranchIds);
        Task<List<CriticalAlertDto>> GenerateCriticalAlertsAsync(List<int> accessibleBranchIds);
        Task<StrategicRecommendationDto> GenerateStrategicRecommendationsAsync(ConsolidatedReportQueryParams queryParams, List<int> accessibleBranchIds);

        // Trend and Forecasting
        Task<TrendIndicator> CalculateTrendIndicatorAsync(decimal currentValue, decimal previousValue);
        Task<List<TrendDataPointDto>> CalculateTrendDataAsync(ConsolidatedReportQueryParams queryParams, string metricType, List<int> accessibleBranchIds);
        Task<ForecastDto> GenerateForecastAsync(List<TrendDataPointDto> historicalData, int forecastPeriods = 12);

        // Utility Methods
        Task<List<int>> GetAccessibleBranchIdsForUserAsync(int userId);
        Task<bool> CanUserAccessConsolidatedReportsAsync(int userId);
        Task<DateTime[]> GetDateRangeFromParams(ConsolidatedReportQueryParams queryParams);
        Task CacheReportDataAsync(string cacheKey, object data, TimeSpan expiration);
        Task<T?> GetCachedReportDataAsync<T>(string cacheKey) where T : class;
    }
}