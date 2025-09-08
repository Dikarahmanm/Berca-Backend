using Microsoft.ML.Data;

namespace Berca_Backend.DTOs
{
    // ==================== DEMAND FORECASTING DTOs ==================== //

    /// <summary>
    /// Result of ML-based demand forecasting using ML.NET SSA algorithm
    /// </summary>
    public class DemandForecastResult
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int ForecastDays { get; set; }
        public float Confidence { get; set; }
        public List<DailyDemandPrediction> Predictions { get; set; } = new();
        public string ModelType { get; set; } = string.Empty; // SSA, ARIMA, etc.
        public int TrainingDataPoints { get; set; }
        public DateTime GeneratedAt { get; set; }
        public string Message { get; set; } = string.Empty;
        public ForecastMetrics Metrics { get; set; } = new();
    }

    /// <summary>
    /// Individual daily demand prediction from ML model
    /// </summary>
    public class DailyDemandPrediction
    {
        public DateTime Date { get; set; }
        public float PredictedDemand { get; set; }
        public float LowerBound { get; set; }
        public float UpperBound { get; set; }
        public float Confidence { get; set; }
        public List<string> InfluencingFactors { get; set; } = new();
    }

    /// <summary>
    /// ML forecast model performance metrics
    /// </summary>
    public class ForecastMetrics
    {
        public float MeanAbsoluteError { get; set; }
        public float RootMeanSquareError { get; set; }
        public float MeanAbsolutePercentageError { get; set; }
        public float R2Score { get; set; }
        public DateTime LastValidated { get; set; }
        public int ValidationDataPoints { get; set; }
    }

    // ==================== ANOMALY DETECTION DTOs ==================== //

    /// <summary>
    /// ML-based anomaly detection result using PCA and statistical methods
    /// </summary>
    public class AnomalyDetectionResult
    {
        public string AnomalyType { get; set; } = string.Empty; // Sales, Inventory, Price
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime DetectedAt { get; set; }
        public float AnomalyScore { get; set; } // 0-1, higher = more anomalous
        public bool IsAnomaly { get; set; }
        public Dictionary<string, object> AffectedData { get; set; } = new();
        public List<string> PossibleCauses { get; set; } = new();
        public List<string> RecommendedActions { get; set; } = new();
        public AnomalyContext Context { get; set; } = new();
    }

    /// <summary>
    /// Context information for anomaly detection
    /// </summary>
    public class AnomalyContext
    {
        public int? ProductId { get; set; }
        public string? ProductName { get; set; }
        public int? BranchId { get; set; }
        public string? BranchName { get; set; }
        public DateTime TimeRange { get; set; }
        public Dictionary<string, float> ExpectedValues { get; set; } = new();
        public Dictionary<string, float> ActualValues { get; set; } = new();
        public float DeviationPercentage { get; set; }
    }

    // ==================== PRODUCT CLUSTERING DTOs ==================== //

    /// <summary>
    /// ML-based product clustering result using K-Means
    /// </summary>
    public class ProductCluster
    {
        public int ClusterId { get; set; }
        public string ClusterName { get; set; } = string.Empty;
        public string ClusterDescription { get; set; } = string.Empty;
        public int ProductCount { get; set; }
        public List<ClusterProductInfo> Products { get; set; } = new();
        public Dictionary<string, object> Characteristics { get; set; } = new();
        public ClusterMetrics Metrics { get; set; } = new();
    }

    /// <summary>
    /// Product information within a cluster
    /// </summary>
    public class ClusterProductInfo
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public float Distance { get; set; } // Distance from cluster centroid
        public float SimilarityScore { get; set; }
        public Dictionary<string, float> Features { get; set; } = new();
    }

    /// <summary>
    /// Clustering model performance metrics
    /// </summary>
    public class ClusterMetrics
    {
        public float SilhouetteScore { get; set; }
        public float IntraClusterDistance { get; set; }
        public float InterClusterDistance { get; set; }
        public int OptimalClusters { get; set; }
        public DateTime LastClustered { get; set; }
    }

    // ==================== TRANSFER RECOMMENDATION DTOs ==================== //

    /// <summary>
    /// ML-based transfer recommendation results
    /// </summary>
    public class TransferRecommendationResult
    {
        public DateTime GeneratedAt { get; set; }
        public int TotalRecommendations { get; set; }
        public int HighConfidenceRecommendations { get; set; }
        public List<MLTransferRecommendation> Recommendations { get; set; } = new();
        public float ModelAccuracy { get; set; }
        public TransferModelMetrics ModelMetrics { get; set; } = new();
    }

    /// <summary>
    /// Individual ML-powered transfer recommendation
    /// </summary>
    public class MLTransferRecommendation
    {
        public int SourceBranchId { get; set; }
        public string SourceBranchName { get; set; } = string.Empty;
        public int TargetBranchId { get; set; }
        public string TargetBranchName { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int RecommendedQuantity { get; set; }
        
        // ML Scoring
        public float MLConfidenceScore { get; set; } // 0-1
        public float SuccessProbability { get; set; } // 0-1
        public float ExpectedROI { get; set; }
        public float RiskScore { get; set; } // 0-1
        
        // Business Metrics
        public decimal EstimatedValue { get; set; }
        public decimal TransferCost { get; set; }
        public decimal NetBenefit { get; set; }
        
        // ML Features Used
        public Dictionary<string, float> MLFeatures { get; set; } = new();
        public List<string> ReasoningFactors { get; set; } = new();
        public DateTime OptimalTransferDate { get; set; }
    }

    /// <summary>
    /// Transfer recommendation model metrics
    /// </summary>
    public class TransferModelMetrics
    {
        public float Precision { get; set; }
        public float Recall { get; set; }
        public float F1Score { get; set; }
        public float Accuracy { get; set; }
        public int TrainingSamples { get; set; }
        public DateTime LastTrained { get; set; }
    }

    // ==================== MODEL HEALTH DTOs ==================== //

    /// <summary>
    /// Overall ML model health status
    /// </summary>
    public class MLModelHealth
    {
        public DateTime CheckedAt { get; set; }
        public string OverallHealth { get; set; } = string.Empty;
        public float OverallHealthScore { get; set; }
        public ModelHealthStatus DemandForecastModel { get; set; } = new();
        public ModelHealthStatus AnomalyDetectionModel { get; set; } = new();
        public ModelHealthStatus ClusteringModel { get; set; } = new();
        public ModelHealthStatus TransferRecommendationModel { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    /// <summary>
    /// Individual model health status
    /// </summary>
    public class ModelHealthStatus
    {
        public bool IsHealthy { get; set; }
        public float HealthScore { get; set; } // 0-100
        public DateTime LastTrained { get; set; }
        public float AccuracyScore { get; set; }
        public int DataPoints { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<string> Issues { get; set; } = new();
        public ModelPerformanceMetrics Performance { get; set; } = new();
    }

    /// <summary>
    /// Detailed model performance metrics
    /// </summary>
    public class ModelPerformanceMetrics
    {
        public float Accuracy { get; set; }
        public float Precision { get; set; }
        public float Recall { get; set; }
        public float F1Score { get; set; }
        public float MeanSquaredError { get; set; }
        public float R2Score { get; set; }
        public TimeSpan TrainingTime { get; set; }
        public TimeSpan PredictionTime { get; set; }
        public long MemoryUsage { get; set; }
    }

    // ==================== ML TRAINING DTOs ==================== //

    /// <summary>
    /// ML model training configuration
    /// </summary>
    public class MLTrainingConfig
    {
        public string ModelType { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
        public int TrainingDataDays { get; set; } = 365;
        public float TrainTestSplit { get; set; } = 0.8f;
        public bool AutoTuning { get; set; } = true;
        public int MaxTrainingTime { get; set; } = 300; // seconds
    }

    /// <summary>
    /// ML model training result
    /// </summary>
    public class MLTrainingResult
    {
        public bool Success { get; set; }
        public string ModelType { get; set; } = string.Empty;
        public DateTime TrainedAt { get; set; }
        public TimeSpan TrainingDuration { get; set; }
        public int TrainingDataPoints { get; set; }
        public int ValidationDataPoints { get; set; }
        public ModelPerformanceMetrics TrainingMetrics { get; set; } = new();
        public ModelPerformanceMetrics ValidationMetrics { get; set; } = new();
        public List<string> Messages { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public string ModelPath { get; set; } = string.Empty;
    }

    // ==================== PREDICTIVE ANALYTICS DTOs ==================== //

    /// <summary>
    /// Comprehensive predictive analytics result
    /// </summary>
    public class PredictiveAnalyticsResult
    {
        public DateTime GeneratedAt { get; set; }
        public string AnalysisType { get; set; } = string.Empty;
        public int? BranchId { get; set; }
        public string? BranchName { get; set; }
        
        // Demand Predictions
        public List<DemandForecastResult> DemandForecasts { get; set; } = new();
        
        // Risk Predictions
        public List<RiskPrediction> RiskPredictions { get; set; } = new();
        
        // Optimization Opportunities
        public List<OptimizationOpportunity> OptimizationOpportunities { get; set; } = new();
        
        // Business Insights
        public List<BusinessInsight> Insights { get; set; } = new();
        
        // Model Confidence
        public float OverallConfidence { get; set; }
        public Dictionary<string, float> ModelConfidences { get; set; } = new();
    }

    /// <summary>
    /// ML-based risk prediction
    /// </summary>
    public class RiskPrediction
    {
        public string RiskType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public float Probability { get; set; } // 0-1
        public string Severity { get; set; } = string.Empty;
        public decimal PotentialImpact { get; set; }
        public DateTime PredictedDate { get; set; }
        public List<string> RiskFactors { get; set; } = new();
        public List<string> MitigationActions { get; set; } = new();
        public float Confidence { get; set; }
    }

    /// <summary>
    /// ML-identified optimization opportunity
    /// </summary>
    public class OptimizationOpportunity
    {
        public string OpportunityType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal PotentialValue { get; set; }
        public float SuccessProbability { get; set; }
        public string Complexity { get; set; } = string.Empty;
        public TimeSpan EstimatedImplementationTime { get; set; }
        public List<string> RequiredActions { get; set; } = new();
        public List<int> AffectedBranchIds { get; set; } = new();
        public float MLConfidence { get; set; }
    }

    /// <summary>
    /// ML-generated business insight
    /// </summary>
    public class BusinessInsight
    {
        public string InsightType { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Impact { get; set; } = string.Empty;
        public float Significance { get; set; } // 0-1
        public float Confidence { get; set; } // 0-1
        public List<string> SupportingData { get; set; } = new();
        public List<string> RecommendedActions { get; set; } = new();
        public DateTime ValidUntil { get; set; }
    }

    // ==================== FEATURE ENGINEERING DTOs ==================== //

    /// <summary>
    /// Product features for ML analysis
    /// </summary>
    public class ProductFeatures
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        
        // Sales Features
        public float AverageDailySales { get; set; }
        public float SalesVariability { get; set; }
        public float SeasonalityScore { get; set; }
        public float TrendScore { get; set; }
        
        // Financial Features
        public float PricePerUnit { get; set; }
        public float ProfitMargin { get; set; }
        public float RevenueContribution { get; set; }
        
        // Inventory Features
        public float StockTurnover { get; set; }
        public float AverageStockLevel { get; set; }
        public float StockoutFrequency { get; set; }
        
        // Time Features
        public int DaysSinceFirstSale { get; set; }
        public int DaysSinceLastSale { get; set; }
        public float SalesFrequency { get; set; }
        
        // Expiry Features (if applicable)
        public bool HasExpiryDate { get; set; }
        public float AverageShelfLife { get; set; }
        public float ExpiryRiskScore { get; set; }
    }

    /// <summary>
    /// Branch features for ML analysis
    /// </summary>
    public class BranchFeatures
    {
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        
        // Location Features
        public string City { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public float PopulationDensity { get; set; }
        
        // Performance Features
        public float AverageDailyRevenue { get; set; }
        public float RevenueVariability { get; set; }
        public float ProfitMargin { get; set; }
        public float CustomerTraffic { get; set; }
        
        // Inventory Features
        public float InventoryTurnover { get; set; }
        public float StockEfficiency { get; set; }
        public float WastageRate { get; set; }
        
        // Operational Features
        public int StaffCount { get; set; }
        public float OperationalEfficiency { get; set; }
        public float CustomerSatisfactionScore { get; set; }
    }

    // ==================== ML PIPELINE DTOs ==================== //

    /// <summary>
    /// ML pipeline execution result
    /// </summary>
    public class MLPipelineResult
    {
        public string PipelineId { get; set; } = string.Empty;
        public string PipelineName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool Success { get; set; }
        public List<MLPipelineStep> Steps { get; set; } = new();
        public Dictionary<string, object> Results { get; set; } = new();
        public List<string> Messages { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// Individual ML pipeline step
    /// </summary>
    public class MLPipelineStep
    {
        public string StepName { get; set; } = string.Empty;
        public string StepType { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool Success { get; set; }
        public string Status { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
        public Dictionary<string, object> Output { get; set; } = new();
        public string ErrorMessage { get; set; } = string.Empty;
    }
}