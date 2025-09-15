using System.ComponentModel.DataAnnotations;

namespace Berca_Backend.DTOs
{
    /// <summary>
    /// DTO for performance trends analytics
    /// </summary>
    public class PerformanceTrendsDto
    {
        public string Period { get; set; } = string.Empty;
        public double[] Revenue { get; set; } = Array.Empty<double>();
        public double[] Transactions { get; set; } = Array.Empty<double>();
        public double[] Customers { get; set; } = Array.Empty<double>();
        public double[] Efficiency { get; set; } = Array.Empty<double>();
        public double[] Satisfaction { get; set; } = Array.Empty<double>();
        public string[] Labels { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// DTO for forecast data
    /// </summary>
    public class ForecastDataDto
    {
        public string Metric { get; set; } = string.Empty;
        public double CurrentValue { get; set; }
        public ForecastValues Forecast { get; set; } = new();
        public double Confidence { get; set; }
        public string Trend { get; set; } = string.Empty;
        public string[] Factors { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Forecast values for different time periods
    /// </summary>
    public class ForecastValues
    {
        public double NextMonth { get; set; }
        public double NextQuarter { get; set; }
        public double NextYear { get; set; }
    }

    /// <summary>
    /// DTO for multibranch executive summary
    /// </summary>
    public class MultiBranchExecutiveSummaryDto
    {
        public string Period { get; set; } = string.Empty;
        public KeyMetricsDto KeyMetrics { get; set; } = new();
        public string[] Achievements { get; set; } = Array.Empty<string>();
        public string[] Challenges { get; set; } = Array.Empty<string>();
        public string[] Recommendations { get; set; } = Array.Empty<string>();
        public string[] NextActions { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Key metrics for executive summary
    /// </summary>
    public class KeyMetricsDto
    {
        public double TotalRevenue { get; set; }
        public double RevenueGrowth { get; set; }
        public double NetworkEfficiency { get; set; }
        public double CustomerSatisfaction { get; set; }
    }

    /// <summary>
    /// DTO for multibranch regional analytics
    /// </summary>
    public class MultiBranchRegionalDto
    {
        public string Region { get; set; } = string.Empty;
        public int Branches { get; set; }
        public double Revenue { get; set; }
        public double RevenuePerBranch { get; set; }
        public double Growth { get; set; }
        public double MarketShare { get; set; }
        public double Performance { get; set; }
        public string TopBranch { get; set; } = string.Empty;
        public string[] Challenges { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// DTO for competitive analysis
    /// </summary>
    public class CompetitiveAnalysisDto
    {
        public int MarketPosition { get; set; }
        public double MarketShare { get; set; }
        public CompetitorComparisonDto[] CompetitorComparison { get; set; } = Array.Empty<CompetitorComparisonDto>();
        public string[] Opportunities { get; set; } = Array.Empty<string>();
        public string[] Threats { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// DTO for competitor comparison
    /// </summary>
    public class CompetitorComparisonDto
    {
        public string Name { get; set; } = string.Empty;
        public double MarketShare { get; set; }
        public string Strength { get; set; } = string.Empty;
        public string Weakness { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for network analytics overview
    /// </summary>
    public class NetworkAnalyticsDto
    {
        public double TotalRevenue { get; set; }
        public double RevenueGrowth { get; set; }
        public int TotalTransactions { get; set; }
        public double TransactionGrowth { get; set; }
        public double AvgOrderValue { get; set; }
        public double OrderValueGrowth { get; set; }
        public double CustomerSatisfaction { get; set; }
        public double SatisfactionGrowth { get; set; }
        public double OperationalEfficiency { get; set; }
        public double EfficiencyGrowth { get; set; }
        public double InventoryTurnover { get; set; }
        public double TurnoverGrowth { get; set; }
    }

    /// <summary>
    /// DTO for enhanced regional analytics with detailed breakdown
    /// </summary>
    public class RegionalAnalyticsDto
    {
        public string Region { get; set; } = string.Empty;
        public RegionalPerformanceMetricsDto PerformanceMetrics { get; set; } = new();
        public GeographicAnalysisDto GeographicAnalysis { get; set; } = new();
        public RegionalMarketShareDto MarketShare { get; set; } = new();
        public List<RegionalBranchDto> Branches { get; set; } = new();
        public string[] Opportunities { get; set; } = Array.Empty<string>();
        public string[] Challenges { get; set; } = Array.Empty<string>();
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// DTO for regional performance metrics
    /// </summary>
    public class RegionalPerformanceMetricsDto
    {
        public double TotalRevenue { get; set; }
        public double RevenueGrowth { get; set; }
        public double AvgRevenuePerBranch { get; set; }
        public double Efficiency { get; set; }
        public double CustomerSatisfaction { get; set; }
        public double OperationalCosts { get; set; }
        public double ProfitMargin { get; set; }
        public double InventoryTurnover { get; set; }
        public int TotalTransactions { get; set; }
        public double AvgTransactionValue { get; set; }
    }

    /// <summary>
    /// DTO for geographic analysis within region
    /// </summary>
    public class GeographicAnalysisDto
    {
        public string PrimaryCity { get; set; } = string.Empty;
        public int TotalCities { get; set; }
        public double CityMarketPenetration { get; set; }
        public List<CityPerformanceDto> CityBreakdown { get; set; } = new();
        public string DominantMarket { get; set; } = string.Empty;
        public double GeographicConcentrationIndex { get; set; }
    }

    /// <summary>
    /// DTO for city performance within region
    /// </summary>
    public class CityPerformanceDto
    {
        public string CityName { get; set; } = string.Empty;
        public int BranchCount { get; set; }
        public double Revenue { get; set; }
        public double MarketShare { get; set; }
        public double Growth { get; set; }
    }

    /// <summary>
    /// DTO for regional market share analysis
    /// </summary>
    public class RegionalMarketShareDto
    {
        public double CurrentMarketShare { get; set; }
        public double MarketShareGrowth { get; set; }
        public double CompetitivePosition { get; set; }
        public List<CompetitorRegionalDto> RegionalCompetitors { get; set; } = new();
        public double MarketPotential { get; set; }
        public string MarketTrend { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for competitors in specific region
    /// </summary>
    public class CompetitorRegionalDto
    {
        public string CompetitorName { get; set; } = string.Empty;
        public double EstimatedMarketShare { get; set; }
        public string Strength { get; set; } = string.Empty;
        public string CompetitiveAdvantage { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for regional branch details
    /// </summary>
    public class RegionalBranchDto
    {
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public double Revenue { get; set; }
        public double Performance { get; set; }
        public string Status { get; set; } = string.Empty;
        public double ContributionToRegion { get; set; }
    }

    /// <summary>
    /// DTO for enhanced network analytics with comprehensive metrics
    /// </summary>
    public class EnhancedNetworkAnalyticsDto
    {
        public NetworkOverviewDto Overview { get; set; } = new();
        public SystemWideMetricsDto SystemMetrics { get; set; } = new();
        public NetworkHealthStatusDto HealthStatus { get; set; } = new();
        public List<RegionalSummaryDto> RegionalBreakdown { get; set; } = new();
        public NetworkEfficiencyDto Efficiency { get; set; } = new();
        public NetworkRiskAnalysisDto RiskAnalysis { get; set; } = new();
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// DTO for network overview
    /// </summary>
    public class NetworkOverviewDto
    {
        public int TotalBranches { get; set; }
        public int ActiveBranches { get; set; }
        public int UnderperformingBranches { get; set; }
        public int RegionsCount { get; set; }
        public double TotalNetworkRevenue { get; set; }
        public double NetworkGrowthRate { get; set; }
        public string NetworkStatus { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for system-wide metrics
    /// </summary>
    public class SystemWideMetricsDto
    {
        public double TotalRevenue { get; set; }
        public double RevenueGrowth { get; set; }
        public long TotalTransactions { get; set; }
        public double TransactionGrowth { get; set; }
        public double AvgOrderValue { get; set; }
        public double CustomerSatisfactionAvg { get; set; }
        public double NetworkEfficiencyScore { get; set; }
        public double InventoryTurnoverAvg { get; set; }
        public double ProfitMarginAvg { get; set; }
        public double OperationalCostsTotal { get; set; }
    }

    /// <summary>
    /// DTO for network health status
    /// </summary>
    public class NetworkHealthStatusDto
    {
        public double OverallHealthScore { get; set; }
        public string HealthStatus { get; set; } = string.Empty;
        public List<NetworkHealthIndicatorDto> HealthIndicators { get; set; } = new();
        public string[] CriticalAlerts { get; set; } = Array.Empty<string>();
        public string[] Warnings { get; set; } = Array.Empty<string>();
        public DateTime LastHealthCheck { get; set; }
    }

    /// <summary>
    /// DTO for network health indicators
    /// </summary>
    public class NetworkHealthIndicatorDto
    {
        public string Indicator { get; set; } = string.Empty;
        public double Score { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for regional summary in network context
    /// </summary>
    public class RegionalSummaryDto
    {
        public string Region { get; set; } = string.Empty;
        public int BranchCount { get; set; }
        public double Revenue { get; set; }
        public double ContributionPercentage { get; set; }
        public double Performance { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for network efficiency analysis
    /// </summary>
    public class NetworkEfficiencyDto
    {
        public double OverallEfficiencyScore { get; set; }
        public double ResourceUtilization { get; set; }
        public double SupplyChainEfficiency { get; set; }
        public double CrossBranchCoordination { get; set; }
        public double WasteReduction { get; set; }
        public double AutomationLevel { get; set; }
        public List<EfficiencyRecommendationDto> Recommendations { get; set; } = new();
    }

    /// <summary>
    /// DTO for efficiency recommendations
    /// </summary>
    public class EfficiencyRecommendationDto
    {
        public string Area { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public double PotentialImprovement { get; set; }
        public string Priority { get; set; } = string.Empty;
        public string Impact { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for network risk analysis
    /// </summary>
    public class NetworkRiskAnalysisDto
    {
        public double OverallRiskScore { get; set; }
        public List<NetworkRiskFactorDto> RiskFactors { get; set; } = new();
        public List<MitigationStrategyDto> MitigationStrategies { get; set; } = new();
        public string RiskLevel { get; set; } = string.Empty;
        public string[] CriticalRisks { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// DTO for network risk factors
    /// </summary>
    public class NetworkRiskFactorDto
    {
        public string RiskType { get; set; } = string.Empty;
        public double Probability { get; set; }
        public double Impact { get; set; }
        public double RiskScore { get; set; }
        public string Description { get; set; } = string.Empty;
        public string AffectedAreas { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for mitigation strategies
    /// </summary>
    public class MitigationStrategyDto
    {
        public string Strategy { get; set; } = string.Empty;
        public string TargetRisk { get; set; } = string.Empty;
        public double Effectiveness { get; set; }
        public string Timeline { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for enhanced forecast with seasonal and risk analysis
    /// </summary>
    public class EnhancedForecastDto
    {
        public string Metric { get; set; } = string.Empty;
        public double CurrentValue { get; set; }
        public EnhancedForecastValues Forecast { get; set; } = new();
        public SeasonalAnalysisDto SeasonalAnalysis { get; set; } = new();
        public MarketTrendDto MarketTrends { get; set; } = new();
        public ForecastRiskAnalysisDto RiskAnalysis { get; set; } = new();
        public double Confidence { get; set; }
        public string[] InfluencingFactors { get; set; } = Array.Empty<string>();
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Enhanced forecast values with additional time periods
    /// </summary>
    public class EnhancedForecastValues
    {
        public double NextWeek { get; set; }
        public double NextMonth { get; set; }
        public double NextQuarter { get; set; }
        public double NextYear { get; set; }
        public double Conservative { get; set; }
        public double Optimistic { get; set; }
        public double MostLikely { get; set; }
    }

    /// <summary>
    /// DTO for seasonal analysis in forecasting
    /// </summary>
    public class SeasonalAnalysisDto
    {
        public bool HasSeasonality { get; set; }
        public double SeasonalityStrength { get; set; }
        public List<ForecastSeasonalPatternDto> SeasonalPatterns { get; set; } = new();
        public string PeakSeason { get; set; } = string.Empty;
        public string LowSeason { get; set; } = string.Empty;
        public double SeasonalVariation { get; set; }
    }

    /// <summary>
    /// DTO for forecast seasonal patterns
    /// </summary>
    public class ForecastSeasonalPatternDto
    {
        public string Period { get; set; } = string.Empty;
        public double Factor { get; set; }
        public string Trend { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for market trend analysis
    /// </summary>
    public class MarketTrendDto
    {
        public string OverallTrend { get; set; } = string.Empty;
        public double TrendStrength { get; set; }
        public List<TrendFactorDto> TrendFactors { get; set; } = new();
        public string[] MarketDrivers { get; set; } = Array.Empty<string>();
        public string[] MarketHeadwinds { get; set; } = Array.Empty<string>();
        public double MarketGrowthRate { get; set; }
    }

    /// <summary>
    /// DTO for trend factors
    /// </summary>
    public class TrendFactorDto
    {
        public string Factor { get; set; } = string.Empty;
        public double Impact { get; set; }
        public string Direction { get; set; } = string.Empty;
        public string Confidence { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for forecast risk analysis
    /// </summary>
    public class ForecastRiskAnalysisDto
    {
        public double OverallRisk { get; set; }
        public List<ForecastRiskDto> Risks { get; set; } = new();
        public string RiskLevel { get; set; } = string.Empty;
        public double ConfidenceInterval { get; set; }
        public string[] RiskMitigationActions { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// DTO for individual forecast risks
    /// </summary>
    public class ForecastRiskDto
    {
        public string RiskType { get; set; } = string.Empty;
        public double Probability { get; set; }
        public double Impact { get; set; }
        public string Description { get; set; } = string.Empty;
        public string MitigationStrategy { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for analytics query parameters
    /// </summary>
    public class AnalyticsQueryParams
    {
        public string Period { get; set; } = "3M";
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Region { get; set; }
        public int[]? BranchIds { get; set; }
        public string? MetricType { get; set; }
        public bool IncludeForecasting { get; set; } = false;
        public bool IncludeCompetitive { get; set; } = false;
    }
}