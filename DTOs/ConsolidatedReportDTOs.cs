using System.ComponentModel.DataAnnotations;
using Berca_Backend.Models;

namespace Berca_Backend.DTOs
{
    // ==================== QUERY PARAMETERS ==================== //
    
    public class ConsolidatedReportQueryParams
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? DateRange { get; set; } = "month"; // today, week, month, year, custom
        public string? Province { get; set; }
        public string? City { get; set; }
        public BranchType? BranchType { get; set; }
        public string? StoreSize { get; set; }
        public bool IncludeInactive { get; set; } = false;
        public string? SortBy { get; set; } = "sales";
        public string? SortOrder { get; set; } = "desc";
        public bool GroupByRegion { get; set; } = false;
        public bool IncludeTrends { get; set; } = true;
    }

    public class ExportParams
    {
        [Required]
        public string ReportType { get; set; } = string.Empty; // sales, inventory, regional, ranking, executive
        
        [Required]
        public string ExportFormat { get; set; } = "pdf"; // pdf, excel
        
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? DateRange { get; set; } = "month";
        public List<int>? BranchIds { get; set; }
        public string? Province { get; set; }
        public bool IncludeCharts { get; set; } = true;
        public bool IncludeDetails { get; set; } = true;
    }

    // ==================== CORE ANALYTICS DTOs ==================== //

    public class SalesComparisonDto
    {
        public List<BranchSalesMetricsDto> BranchMetrics { get; set; } = new List<BranchSalesMetricsDto>();
        public ConsolidatedSalesMetricsDto ConsolidatedMetrics { get; set; } = new ConsolidatedSalesMetricsDto();
        public TrendAnalysisDto Trends { get; set; } = new TrendAnalysisDto();
        public DateTime ReportPeriodStart { get; set; }
        public DateTime ReportPeriodEnd { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public string ReportSummary { get; set; } = string.Empty;
    }

    public class BranchSalesMetricsDto
    {
        public int BranchId { get; set; }
        public string BranchCode { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public BranchType BranchType { get; set; }
        public string StoreSize { get; set; } = string.Empty;
        public bool IsActive { get; set; }

        // Sales Metrics
        public decimal TotalRevenue { get; set; }
        public int TransactionCount { get; set; }
        public decimal AverageTicketSize { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal GrossProfitMargin { get; set; }
        public decimal NetProfit { get; set; }
        public decimal NetProfitMargin { get; set; }

        // Employee Metrics
        public int EmployeeCount { get; set; }
        public decimal SalesPerEmployee { get; set; }
        public decimal TransactionsPerEmployee { get; set; }
        public decimal EmployeeProductivity { get; set; }

        // Customer Metrics
        public int TotalCustomers { get; set; }
        public int NewMembers { get; set; }
        public int ReturnCustomers { get; set; }
        public decimal CustomerRetentionRate { get; set; }

        // Performance Indicators
        public int Ranking { get; set; }
        public int RankingChange { get; set; } // +/- from previous period
        public decimal PerformanceScore { get; set; }
        public TrendIndicator SalesTrend { get; set; }
        public TrendIndicator ProfitTrend { get; set; }
        public TrendIndicator ProductivityTrend { get; set; }

        // Comparisons
        public decimal RevenueGrowth { get; set; } // % change from previous period
        public decimal ProfitGrowth { get; set; }
        public decimal TransactionGrowth { get; set; }
        public decimal MarketSharePercent { get; set; }
    }

    public class ConsolidatedSalesMetricsDto
    {
        public decimal TotalRevenue { get; set; }
        public int TotalTransactions { get; set; }
        public decimal AverageTicketSize { get; set; }
        public decimal TotalGrossProfit { get; set; }
        public decimal ConsolidatedGrossProfitMargin { get; set; }
        public decimal TotalNetProfit { get; set; }
        public decimal ConsolidatedNetProfitMargin { get; set; }
        public int TotalEmployees { get; set; }
        public decimal AverageSalesPerEmployee { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalNewMembers { get; set; }
        public decimal OverallCustomerRetentionRate { get; set; }
        public int ActiveBranches { get; set; }
        public decimal AveragePerformanceScore { get; set; }
        public BranchSalesMetricsDto TopPerformer { get; set; } = new BranchSalesMetricsDto();
        public BranchSalesMetricsDto BottomPerformer { get; set; } = new BranchSalesMetricsDto();
    }

    public class InventoryOverviewDto
    {
        public List<BranchInventoryDto> BranchInventories { get; set; } = new List<BranchInventoryDto>();
        public ConsolidatedInventoryDto ConsolidatedInventory { get; set; } = new ConsolidatedInventoryDto();
        public List<LowStockAlertDto> LowStockAlerts { get; set; } = new List<LowStockAlertDto>();
        public List<InventoryMovementDto> FastMovingItems { get; set; } = new List<InventoryMovementDto>();
        public List<InventoryMovementDto> SlowMovingItems { get; set; } = new List<InventoryMovementDto>();
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    public class BranchInventoryDto
    {
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public string BranchCode { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public int TotalProducts { get; set; }
        public int InStockProducts { get; set; }
        public int LowStockProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public decimal TotalInventoryValue { get; set; }
        public decimal InventoryTurnoverRate { get; set; }
        public decimal AverageStockLevel { get; set; }
        public DateTime LastStockUpdate { get; set; }
        public TrendIndicator InventoryTrend { get; set; }
    }

    public class ConsolidatedInventoryDto
    {
        public int TotalProducts { get; set; }
        public int TotalInStock { get; set; }
        public int TotalLowStock { get; set; }
        public int TotalOutOfStock { get; set; }
        public decimal TotalInventoryValue { get; set; }
        public decimal AverageInventoryTurnover { get; set; }
        public decimal OverallStockHealth { get; set; } // Percentage of healthy stock levels
        public int BranchesWithLowStock { get; set; }
        public int BranchesWithOutOfStock { get; set; }
    }

    public class LowStockAlertDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public int MinimumStock { get; set; }
        public string Severity { get; set; } = string.Empty; // Critical, Warning, Low
        public int DaysSinceLastRestock { get; set; }
        public decimal EstimatedDaysRemaining { get; set; }
        public bool RequiresImmediateAction { get; set; }
    }

    public class InventoryMovementDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int TotalSold { get; set; }
        public decimal TurnoverRate { get; set; }
        public decimal Revenue { get; set; }
        public Dictionary<string, int> BranchSales { get; set; } = new Dictionary<string, int>();
        public TrendIndicator MovementTrend { get; set; }
    }

    public class RegionalDashboardDto
    {
        public List<RegionalPerformanceDto> RegionalPerformance { get; set; } = new List<RegionalPerformanceDto>();
        public Dictionary<string, decimal> MarketShareByRegion { get; set; } = new Dictionary<string, decimal>();
        public RegionalComparisonDto RegionalComparison { get; set; } = new RegionalComparisonDto();
        public List<GrowthOpportunityDto> GrowthOpportunities { get; set; } = new List<GrowthOpportunityDto>();
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    public class RegionalPerformanceDto
    {
        public string Region { get; set; } = string.Empty; // Province name
        public int BranchCount { get; set; }
        public int ActiveBranches { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalTransactions { get; set; }
        public decimal AverageTicketSize { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal ProfitMargin { get; set; }
        public int TotalEmployees { get; set; }
        public decimal SalesPerEmployee { get; set; }
        public decimal MarketShare { get; set; }
        public int Ranking { get; set; }
        public TrendIndicator SalesTrend { get; set; }
        public TrendIndicator ProfitTrend { get; set; }
        public List<BranchSalesMetricsDto> TopBranches { get; set; } = new List<BranchSalesMetricsDto>();
        public List<BranchSalesMetricsDto> BottomBranches { get; set; } = new List<BranchSalesMetricsDto>();
    }

    public class RegionalComparisonDto
    {
        public string TopPerformingRegion { get; set; } = string.Empty;
        public string BottomPerformingRegion { get; set; } = string.Empty;
        public decimal PerformanceGap { get; set; } // Difference between top and bottom
        public decimal AverageRegionalRevenue { get; set; }
        public Dictionary<string, decimal> RegionalGrowthRates { get; set; } = new Dictionary<string, decimal>();
        public Dictionary<string, string> RegionalInsights { get; set; } = new Dictionary<string, string>();
    }

    public class GrowthOpportunityDto
    {
        public string Region { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string OpportunityType { get; set; } = string.Empty; // Market Gap, Expansion, Optimization
        public string Description { get; set; } = string.Empty;
        public decimal PotentialRevenue { get; set; }
        public decimal InvestmentRequired { get; set; }
        public decimal ExpectedROI { get; set; }
        public string Priority { get; set; } = string.Empty; // High, Medium, Low
        public string Recommendation { get; set; } = string.Empty;
    }

    public class BranchRankingDto
    {
        public List<BranchPerformanceRankingDto> Rankings { get; set; } = new List<BranchPerformanceRankingDto>();
        public PerformanceBenchmarkDto Benchmarks { get; set; } = new PerformanceBenchmarkDto();
        public List<PerformanceInsightDto> Insights { get; set; } = new List<PerformanceInsightDto>();
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    public class BranchPerformanceRankingDto
    {
        public int Rank { get; set; }
        public int PreviousRank { get; set; }
        public int RankChange { get; set; }
        public BranchSalesMetricsDto Branch { get; set; } = new BranchSalesMetricsDto();
        public decimal OverallScore { get; set; }
        public Dictionary<string, decimal> ScoreBreakdown { get; set; } = new Dictionary<string, decimal>();
        public Dictionary<string, string> StrengthAreas { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> ImprovementAreas { get; set; } = new Dictionary<string, string>();
        public string PerformanceCategory { get; set; } = string.Empty; // Excellent, Good, Average, Below Average, Poor
    }

    public class PerformanceBenchmarkDto
    {
        public decimal TopQuartileRevenue { get; set; }
        public decimal MedianRevenue { get; set; }
        public decimal BottomQuartileRevenue { get; set; }
        public decimal TopQuartileProfitMargin { get; set; }
        public decimal MedianProfitMargin { get; set; }
        public decimal BottomQuartileProfitMargin { get; set; }
        public decimal TopQuartileProductivity { get; set; }
        public decimal MedianProductivity { get; set; }
        public decimal BottomQuartileProductivity { get; set; }
        public Dictionary<string, decimal> StoreSizeBenchmarks { get; set; } = new Dictionary<string, decimal>();
        public Dictionary<string, decimal> RegionalBenchmarks { get; set; } = new Dictionary<string, decimal>();
    }

    public class PerformanceInsightDto
    {
        public string Category { get; set; } = string.Empty; // Sales, Profit, Productivity, Growth
        public string Insight { get; set; } = string.Empty;
        public string ActionRecommendation { get; set; } = string.Empty;
        public List<int> AffectedBranchIds { get; set; } = new List<int>();
        public string Priority { get; set; } = string.Empty; // High, Medium, Low
        public decimal PotentialImpact { get; set; }
    }

    public class ExecutiveSummaryDto
    {
        public ExecutiveKPIsDto KPIs { get; set; } = new ExecutiveKPIsDto();
        public List<KeyInsightDto> KeyInsights { get; set; } = new List<KeyInsightDto>();
        public List<CriticalAlertDto> CriticalAlerts { get; set; } = new List<CriticalAlertDto>();
        public StrategicRecommendationDto StrategicRecommendations { get; set; } = new StrategicRecommendationDto();
        public CompetitivePositionDto CompetitivePosition { get; set; } = new CompetitivePositionDto();
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public string ReportPeriod { get; set; } = string.Empty;
    }

    public class ExecutiveKPIsDto
    {
        public decimal TotalRevenue { get; set; }
        public decimal RevenueGrowth { get; set; }
        public decimal NetProfitMargin { get; set; }
        public decimal ProfitGrowthRate { get; set; }
        public int TotalTransactions { get; set; }
        public decimal TransactionGrowth { get; set; }
        public decimal AverageTicketSize { get; set; }
        public decimal TicketSizeGrowth { get; set; }
        public int ActiveBranches { get; set; }
        public decimal BranchEfficiencyScore { get; set; }
        public int TotalEmployees { get; set; }
        public decimal EmployeeProductivity { get; set; }
        public decimal CustomerSatisfactionScore { get; set; }
        public decimal InventoryTurnover { get; set; }
        public decimal MarketShareGrowth { get; set; }
    }

    public class KeyInsightDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Impact { get; set; } = string.Empty; // Positive, Negative, Neutral
        public decimal Value { get; set; }
        public string MetricType { get; set; } = string.Empty;
        public TrendIndicator Trend { get; set; }
        public string ActionRequired { get; set; } = string.Empty;
    }

    public class CriticalAlertDto
    {
        public string AlertType { get; set; } = string.Empty; // Performance, Inventory, Financial
        public string Severity { get; set; } = string.Empty; // Critical, High, Medium
        public string Message { get; set; } = string.Empty;
        public List<int> AffectedBranchIds { get; set; } = new List<int>();
        public string ImmediateAction { get; set; } = string.Empty;
        public DateTime DetectedAt { get; set; }
        public bool RequiresEscalation { get; set; }
    }

    public class StrategicRecommendationDto
    {
        public List<string> ShortTermActions { get; set; } = new List<string>();
        public List<string> MediumTermInitiatives { get; set; } = new List<string>();
        public List<string> LongTermStrategy { get; set; } = new List<string>();
        public List<InvestmentOpportunityDto> InvestmentOpportunities { get; set; } = new List<InvestmentOpportunityDto>();
        public List<RiskFactorDto> RiskFactors { get; set; } = new List<RiskFactorDto>();
    }

    public class InvestmentOpportunityDto
    {
        public string OpportunityType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal InvestmentAmount { get; set; }
        public decimal ExpectedReturn { get; set; }
        public string Timeframe { get; set; } = string.Empty;
        public string RiskLevel { get; set; } = string.Empty;
    }

    public class RiskFactorDto
    {
        public string RiskType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Probability { get; set; } = string.Empty;
        public string Impact { get; set; } = string.Empty;
        public string MitigationStrategy { get; set; } = string.Empty;
    }

    public class CompetitivePositionDto
    {
        public string MarketPosition { get; set; } = string.Empty;
        public List<string> CompetitiveAdvantages { get; set; } = new List<string>();
        public List<string> Challenges { get; set; } = new List<string>();
        public decimal MarketShare { get; set; }
        public string MarketTrend { get; set; } = string.Empty;
    }

    // ==================== REAL-TIME & TRENDS ==================== //

    public class RealTimeMetricsDto
    {
        public RealTimeSalesDto TodaySales { get; set; } = new RealTimeSalesDto();
        public List<BranchRealTimeDto> BranchMetrics { get; set; } = new List<BranchRealTimeDto>();
        public LiveInventoryStatusDto InventoryStatus { get; set; } = new LiveInventoryStatusDto();
        public SystemHealthDto SystemHealth { get; set; } = new SystemHealthDto();
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public int RefreshIntervalSeconds { get; set; } = 30;
    }

    public class RealTimeSalesDto
    {
        public decimal TodayRevenue { get; set; }
        public int TodayTransactions { get; set; }
        public decimal CurrentHourRevenue { get; set; }
        public int CurrentHourTransactions { get; set; }
        public decimal AverageTicketToday { get; set; }
        public TrendIndicator HourlyTrend { get; set; }
        public decimal ProjectedDailyRevenue { get; set; }
        public decimal TargetAchievement { get; set; }
    }

    public class BranchRealTimeDto
    {
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public bool IsOpen { get; set; }
        public int ActiveEmployees { get; set; }
        public decimal TodayRevenue { get; set; }
        public int TodayTransactions { get; set; }
        public DateTime LastTransactionTime { get; set; }
        public string Status { get; set; } = string.Empty; // Active, Idle, Busy, Offline
        public List<string> Alerts { get; set; } = new List<string>();
    }

    public class LiveInventoryStatusDto
    {
        public int CriticalStockItems { get; set; }
        public int LowStockItems { get; set; }
        public int OutOfStockItems { get; set; }
        public List<string> UrgentRestockNeeded { get; set; } = new List<string>();
        public decimal OverallStockHealth { get; set; }
    }

    public class SystemHealthDto
    {
        public bool AllSystemsOperational { get; set; }
        public List<string> SystemAlerts { get; set; } = new List<string>();
        public decimal DataFreshnessScore { get; set; }
        public DateTime LastDataSync { get; set; }
    }

    public class TrendAnalysisDto
    {
        public List<TrendDataPointDto> SalesTrend { get; set; } = new List<TrendDataPointDto>();
        public List<TrendDataPointDto> ProfitTrend { get; set; } = new List<TrendDataPointDto>();
        public List<TrendDataPointDto> TransactionTrend { get; set; } = new List<TrendDataPointDto>();
        public TrendSummaryDto TrendSummary { get; set; } = new TrendSummaryDto();
        public List<SeasonalPatternDto> SeasonalPatterns { get; set; } = new List<SeasonalPatternDto>();
        public ForecastDto Forecast { get; set; } = new ForecastDto();
    }

    public class TrendDataPointDto
    {
        public DateTime Date { get; set; }
        public decimal Value { get; set; }
        public decimal PreviousPeriodValue { get; set; }
        public decimal ChangePercent { get; set; }
        public TrendIndicator Indicator { get; set; }
    }

    public class TrendSummaryDto
    {
        public TrendIndicator OverallTrend { get; set; }
        public decimal OverallGrowthRate { get; set; }
        public string TrendDescription { get; set; } = string.Empty;
        public int ConsecutiveGrowthPeriods { get; set; }
        public int ConsecutiveDeclinePeriods { get; set; }
        public decimal Volatility { get; set; }
    }

    public class SeasonalPatternDto
    {
        public string Period { get; set; } = string.Empty; // Weekly, Monthly, Quarterly
        public string PeakPeriod { get; set; } = string.Empty;
        public string LowPeriod { get; set; } = string.Empty;
        public decimal Seasonality { get; set; }
        public List<string> Patterns { get; set; } = new List<string>();
    }

    public class ForecastDto
    {
        public List<ForecastDataPointDto> NextPeriod { get; set; } = new List<ForecastDataPointDto>();
        public decimal ConfidenceLevel { get; set; }
        public string ForecastMethod { get; set; } = string.Empty;
        public List<string> Assumptions { get; set; } = new List<string>();
    }

    public class ForecastDataPointDto
    {
        public DateTime Date { get; set; }
        public decimal PredictedValue { get; set; }
        public decimal LowerBound { get; set; }
        public decimal UpperBound { get; set; }
        public decimal Confidence { get; set; }
    }

    // ==================== ENUMS ==================== //

    public enum TrendIndicator
    {
        StronglyUp = 3,
        Up = 2,
        SlightlyUp = 1,
        Stable = 0,
        SlightlyDown = -1,
        Down = -2,
        StronglyDown = -3
    }

    public enum ExportFormat
    {
        PDF,
        Excel,
        CSV
    }

    public enum ReportType
    {
        SalesComparison,
        InventoryOverview,
        RegionalDashboard,
        BranchRanking,
        ExecutiveSummary,
        TrendAnalysis,
        RealTimeMetrics
    }
}