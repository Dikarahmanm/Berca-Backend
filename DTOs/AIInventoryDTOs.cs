using System.ComponentModel.DataAnnotations;

namespace Berca_Backend.DTOs
{
    // ==================== SMART STOCK COORDINATION DTOs ==================== //

    /// <summary>
    /// DTO for AI-powered smart stock coordination
    /// </summary>
    public class SmartStockCoordinationDto
    {
        public DateTime GeneratedAt { get; set; }
        public string AnalysisScope { get; set; } = string.Empty;
        public List<BranchStockCoordinationDto> BranchCoordinations { get; set; } = new();
        public decimal SystemOptimizationScore { get; set; }
        public List<AIRecommendationDto> AIRecommendations { get; set; } = new();
        public List<PredictiveInsightDto> PredictiveInsights { get; set; } = new();
        public DateTime NextOptimizationWindow { get; set; }
        public decimal ConfidenceLevel { get; set; }
    }

    /// <summary>
    /// DTO for branch-specific stock coordination
    /// </summary>
    public class BranchStockCoordinationDto
    {
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public decimal StockHealthScore { get; set; }
        public int TotalProducts { get; set; }
        public int OptimizationOpportunities { get; set; }
        public List<AIRecommendationDto> AIRecommendations { get; set; } = new();
        public DateTime LastOptimized { get; set; }
        public DateTime NextOptimization { get; set; }
        public List<StockCoordinationMetricDto> Metrics { get; set; } = new();
        public List<ProductCoordinationDto> TopProducts { get; set; } = new();
    }

    /// <summary>
    /// DTO for AI recommendations
    /// </summary>
    public class AIRecommendationDto
    {
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public decimal EstimatedImpact { get; set; }
        public decimal ConfidenceLevel { get; set; }
        public string RecommendedAction { get; set; } = string.Empty;
        public List<int> AffectedBranchIds { get; set; } = new();
        public DateTime RecommendedExecutionDate { get; set; }
        public string Category { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
    }

    /// <summary>
    /// DTO for predictive insights
    /// </summary>
    public class PredictiveInsightDto
    {
        public string InsightType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Confidence { get; set; }
        public string TimeFrame { get; set; } = string.Empty;
        public string RecommendedAction { get; set; } = string.Empty;
        public decimal PotentialImpact { get; set; }
        public List<string> KeyFactors { get; set; } = new();
    }

    /// <summary>
    /// DTO for stock coordination metrics
    /// </summary>
    public class StockCoordinationMetricDto
    {
        public string MetricName { get; set; } = string.Empty;
        public decimal CurrentValue { get; set; }
        public decimal OptimalValue { get; set; }
        public decimal VariancePercentage { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Trend { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for product coordination details
    /// </summary>
    public class ProductCoordinationDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public int OptimalStock { get; set; }
        public decimal CoordinationScore { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<string> ActionItems { get; set; } = new();
    }

    // ==================== INTELLIGENT TRANSFER RECOMMENDATIONS DTOs ==================== //

    /// <summary>
    /// DTO for intelligent transfer recommendations powered by AI
    /// </summary>
    public class IntelligentTransferRecommendationDto
    {
        public int Id { get; set; }
        public string RecommendationType { get; set; } = string.Empty;
        
        // Source and Target
        public int SourceBranchId { get; set; }
        public string SourceBranchName { get; set; } = string.Empty;
        public int TargetBranchId { get; set; }
        public string TargetBranchName { get; set; } = string.Empty;
        
        // Product Information
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int BatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        
        // AI Analysis
        public decimal AIConfidenceScore { get; set; }
        public decimal PredictedSuccessRate { get; set; }
        public TransferRiskAssessmentDto RiskAssessment { get; set; } = new();
        
        // Transfer Details
        public int RecommendedQuantity { get; set; }
        public decimal PotentialValue { get; set; }
        public decimal EstimatedBenefit { get; set; }
        public decimal TransferCost { get; set; }
        public decimal NetValue { get; set; }
        
        // AI Insights
        public string AIReasoning { get; set; } = string.Empty;
        public List<string> KeyFactors { get; set; } = new();
        public List<string> PredictedOutcomes { get; set; } = new();
        
        // Timeline and Priority
        public string Priority { get; set; } = string.Empty;
        public DateTime OptimalTransferWindow { get; set; }
        public DateTime DeadlineDate { get; set; }
        
        // Business Context
        public string BusinessJustification { get; set; } = string.Empty;
        public List<AlternativeActionDto> AlternativeActions { get; set; } = new();
    }

    /// <summary>
    /// DTO for transfer risk assessment
    /// </summary>
    public class TransferRiskAssessmentDto
    {
        public string RiskLevel { get; set; } = string.Empty;
        public decimal RiskScore { get; set; }
        public List<string> RiskFactors { get; set; } = new();
        public List<string> MitigationStrategies { get; set; } = new();
        public decimal LiabilityExposure { get; set; }
        public string RecommendedApproach { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for alternative actions
    /// </summary>
    public class AlternativeActionDto
    {
        public string ActionType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal ExpectedOutcome { get; set; }
        public decimal SuccessProbability { get; set; }
        public string Pros { get; set; } = string.Empty;
        public string Cons { get; set; } = string.Empty;
    }

    // ==================== PREDICTIVE STOCK OPTIMIZATION DTOs ==================== //

    /// <summary>
    /// DTO for AI-powered predictive stock optimization
    /// </summary>
    public class PredictiveStockOptimizationDto
    {
        public DateTime GeneratedAt { get; set; }
        public int PredictionWindow { get; set; }
        public List<ProductStockPredictionDto> ProductPredictions { get; set; } = new();
        public List<SystemOptimizationDto> SystemOptimizations { get; set; } = new();
        public decimal OverallAccuracy { get; set; }
        public List<OptimizationActionDto> RecommendedActions { get; set; } = new();
        public string ModelVersion { get; set; } = string.Empty;
        public int TrainingDataPoints { get; set; }
        public PredictionMetricsDto ModelMetrics { get; set; } = new();
    }

    /// <summary>
    /// DTO for individual product stock predictions
    /// </summary>
    public class ProductStockPredictionDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        
        // Current State
        public int CurrentStock { get; set; }
        public decimal CurrentStockValue { get; set; }
        
        // AI Predictions
        public decimal PredictedDemand { get; set; }
        public int OptimalStock { get; set; }
        public decimal OptimalStockValue { get; set; }
        public decimal Confidence { get; set; }
        
        // Trend Analysis
        public string DemandTrend { get; set; } = string.Empty;
        public decimal SeasonalityFactor { get; set; }
        public decimal VolatilityScore { get; set; }
        
        // Recommendations
        public string RecommendedAction { get; set; } = string.Empty;
        public int SuggestedAdjustment { get; set; }
        public decimal ExpectedImpact { get; set; }
        
        // Risk Factors
        public List<string> RiskFactors { get; set; } = new();
        public decimal RiskScore { get; set; }
        
        // Timeline
        public List<PredictionDataPointDto> Timeline { get; set; } = new();
    }

    /// <summary>
    /// DTO for system-wide optimizations
    /// </summary>
    public class SystemOptimizationDto
    {
        public string OptimizationType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal PotentialSavings { get; set; }
        public decimal ImplementationCost { get; set; }
        public decimal ROI { get; set; }
        public string Priority { get; set; } = string.Empty;
        public DateTime RecommendedStartDate { get; set; }
        public List<string> RequiredActions { get; set; } = new();
        public List<int> AffectedBranchIds { get; set; } = new();
    }

    /// <summary>
    /// DTO for optimization actions
    /// </summary>
    public class OptimizationActionDto
    {
        public string ActionType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public decimal EstimatedValue { get; set; }
        public decimal ConfidenceLevel { get; set; }
        public DateTime RecommendedDate { get; set; }
        public bool IsAutomatable { get; set; }
        public bool IsSuccess { get; set; }
        public string Result { get; set; } = string.Empty;
        public List<string> Prerequisites { get; set; } = new();
    }

    /// <summary>
    /// DTO for prediction metrics
    /// </summary>
    public class PredictionMetricsDto
    {
        public decimal Accuracy { get; set; }
        public decimal Precision { get; set; }
        public decimal Recall { get; set; }
        public decimal F1Score { get; set; }
        public decimal MeanAbsoluteError { get; set; }
        public decimal RootMeanSquareError { get; set; }
        public DateTime LastTraining { get; set; }
        public DateTime NextTraining { get; set; }
    }

    /// <summary>
    /// DTO for prediction timeline data points
    /// </summary>
    public class PredictionDataPointDto
    {
        public DateTime Date { get; set; }
        public decimal PredictedValue { get; set; }
        public decimal ConfidenceInterval { get; set; }
        public List<string> InfluencingFactors { get; set; } = new();
    }

    // ==================== REAL-TIME COORDINATION STATUS DTOs ==================== //

    /// <summary>
    /// DTO for real-time coordination status
    /// </summary>
    public class RealtimeCoordinationStatusDto
    {
        public DateTime Timestamp { get; set; }
        public SystemHealthMetricsDto SystemHealth { get; set; } = new();
        public List<ActiveOptimizationDto> ActiveOptimizations { get; set; } = new();
        public List<CoordinationEventDto> RecentEvents { get; set; } = new();
        public LiveMetricsDto LiveMetrics { get; set; } = new();
        public DateTime NextScheduledOptimization { get; set; }
        public string SystemStatus { get; set; } = string.Empty;
        public List<RealTimeAlertDto> ActiveAlerts { get; set; } = new();
    }

    /// <summary>
    /// DTO for system health metrics
    /// </summary>
    public class SystemHealthMetricsDto
    {
        public decimal OverallScore { get; set; }
        public int ActiveBranches { get; set; }
        public int PendingTransfers { get; set; }
        public int CriticalAlerts { get; set; }
        public decimal CoordinationEfficiency { get; set; }
        public decimal SystemLoad { get; set; }
        public decimal ResponseTime { get; set; }
        public decimal Uptime { get; set; }
        public List<HealthIndicatorDto> Indicators { get; set; } = new();
    }

    /// <summary>
    /// DTO for active optimizations
    /// </summary>
    public class ActiveOptimizationDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Progress { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? EstimatedCompletion { get; set; }
        public decimal EstimatedValue { get; set; }
        public List<string> AffectedBranches { get; set; } = new();
    }

    /// <summary>
    /// DTO for coordination events
    /// </summary>
    public class CoordinationEventDto
    {
        public DateTime Timestamp { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public int? BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public decimal? Value { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for live metrics
    /// </summary>
    public class LiveMetricsDto
    {
        public decimal TotalInventoryValue { get; set; }
        public decimal ValueAtRisk { get; set; }
        public int PendingTransfers { get; set; }
        public int OptimizationOpportunities { get; set; }
        public decimal PotentialSavings { get; set; }
        public decimal SystemEfficiency { get; set; }
        public Dictionary<string, decimal> BranchMetrics { get; set; } = new();
        public Dictionary<string, int> CategoryMetrics { get; set; } = new();
    }

    /// <summary>
    /// DTO for real-time alerts
    /// </summary>
    public class RealTimeAlertDto
    {
        public string Type { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool RequiresAction { get; set; }
        public string ActionUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for health indicators
    /// </summary>
    public class HealthIndicatorDto
    {
        public string Name { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public decimal Threshold { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Trend { get; set; } = string.Empty;
    }

    // ==================== AI INSIGHTS DTOs ==================== //

    /// <summary>
    /// DTO for AI insights
    /// </summary>
    public class AIInsightsDto
    {
        public DateTime GeneratedAt { get; set; }
        public int? BranchId { get; set; }
        public List<AIInsightDto> Insights { get; set; } = new();
        public decimal ModelConfidence { get; set; }
        public decimal DataQualityScore { get; set; }
        public List<string> RecommendedActions { get; set; } = new();
        public AIModelInfoDto ModelInfo { get; set; } = new();
    }

    /// <summary>
    /// DTO for individual AI insights
    /// </summary>
    public class AIInsightDto
    {
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal ImpactScore { get; set; }
        public decimal Confidence { get; set; }
        public string Category { get; set; } = string.Empty;
        public bool IsActionable { get; set; }
        public string RecommendedAction { get; set; } = string.Empty;
        public List<string> SupportingData { get; set; } = new();
        public DateTime ValidUntil { get; set; }
        public List<int> AffectedEntities { get; set; } = new();
    }

    /// <summary>
    /// DTO for AI model information
    /// </summary>
    public class AIModelInfoDto
    {
        public string Version { get; set; } = string.Empty;
        public DateTime LastTraining { get; set; }
        public decimal Accuracy { get; set; }
        public int TrainingDataPoints { get; set; }
        public List<string> Features { get; set; } = new();
        public string Algorithm { get; set; } = string.Empty;
    }

    // ==================== SMART ALERTS DTOs ==================== //

    /// <summary>
    /// DTO for smart AI-generated alerts
    /// </summary>
    public class SmartAlertDto
    {
        public string AlertType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public decimal AIUrgencyScore { get; set; }
        public decimal BusinessImpactScore { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsAutomated { get; set; }
        public List<string> RecommendedActions { get; set; } = new();
        public string ActionUrl { get; set; } = string.Empty;
        public AIAlertMetadataDto Metadata { get; set; } = new();
    }

    /// <summary>
    /// DTO for AI alert metadata
    /// </summary>
    public class AIAlertMetadataDto
    {
        public decimal ConfidenceLevel { get; set; }
        public List<string> TriggeredBy { get; set; } = new();
        public string ModelUsed { get; set; } = string.Empty;
        public Dictionary<string, object> Context { get; set; } = new();
    }

    // ==================== AUTO OPTIMIZATION DTOs ==================== //

    /// <summary>
    /// DTO for auto-optimization results
    /// </summary>
    public class AutoOptimizationResultDto
    {
        public DateTime ExecutedAt { get; set; }
        public bool WasDryRun { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public int TotalActions { get; set; }
        public int SuccessfulActions { get; set; }
        public int FailedActions { get; set; }
        public decimal SuccessRate { get; set; }
        public List<OptimizationActionDto> Actions { get; set; } = new();
        public decimal EstimatedSavings { get; set; }
        public decimal AIConfidenceScore { get; set; }
        public DateTime NextOptimizationScheduled { get; set; }
        public OptimizationSummaryDto Summary { get; set; } = new();
    }

    /// <summary>
    /// DTO for optimization summary
    /// </summary>
    public class OptimizationSummaryDto
    {
        public Dictionary<string, int> ActionsByType { get; set; } = new();
        public Dictionary<string, decimal> SavingsByCategory { get; set; } = new();
        public List<string> KeyAchievements { get; set; } = new();
        public List<string> LessonsLearned { get; set; } = new();
        public string OverallOutcome { get; set; } = string.Empty;
    }

    // ==================== TRAINING DATA DTOs ==================== //

    /// <summary>
    /// DTO for AI model training data points
    /// </summary>
    public class TrainingDataPoint
    {
        public DateTime Timestamp { get; set; }
        public int ProductId { get; set; }
        public int BranchId { get; set; }
        public Dictionary<string, decimal> Features { get; set; } = new();
        public Dictionary<string, decimal> Outcomes { get; set; } = new();
        public string DataSource { get; set; } = string.Empty;
        public decimal Weight { get; set; } = 1.0m;
    }
}