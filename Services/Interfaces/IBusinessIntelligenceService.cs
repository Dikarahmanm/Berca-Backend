using Berca_Backend.DTOs;

namespace Berca_Backend.Services.Interfaces
{
    /// <summary>
    /// Business Intelligence service interface for advanced analytics
    /// Indonesian business context with predictive insights
    /// </summary>
    public interface IBusinessIntelligenceService
    {
        // ==================== SALES ANALYTICS ==================== //
        
        /// <summary>
        /// Get sales trends and forecasting
        /// </summary>
        Task<object> GetSalesTrendsAsync(DateTime startDate, DateTime endDate, int? branchId = null);
        
        /// <summary>
        /// Predict sales for next period
        /// </summary>
        Task<object> PredictSalesAsync(int forecastDays = 30, int? branchId = null);
        
        /// <summary>
        /// Get seasonal sales patterns
        /// </summary>
        Task<object> GetSeasonalPatternsAsync(int? branchId = null);
        
        /// <summary>
        /// Customer behavior analysis
        /// </summary>
        Task<object> GetCustomerBehaviorAnalysisAsync(DateTime startDate, DateTime endDate, int? branchId = null);
        
        /// <summary>
        /// Product performance insights
        /// </summary>
        Task<object> GetProductPerformanceInsightsAsync(DateTime startDate, DateTime endDate, int? categoryId = null);
        
        // ==================== INVENTORY ANALYTICS ==================== //
        
        /// <summary>
        /// Inventory optimization recommendations
        /// </summary>
        Task<object> GetInventoryOptimizationAsync(int? branchId = null, int? categoryId = null);
        
        /// <summary>
        /// Stock movement predictions
        /// </summary>
        Task<object> PredictStockMovementsAsync(int forecastDays = 30, int? productId = null);
        
        /// <summary>
        /// Identify slow-moving inventory
        /// </summary>
        Task<object> GetSlowMovingInventoryAsync(int? branchId = null, int daysThreshold = 90);
        
        /// <summary>
        /// Reorder point recommendations
        /// </summary>
        Task<object> GetReorderRecommendationsAsync(int? branchId = null);
        
        /// <summary>
        /// ABC analysis for inventory classification
        /// </summary>
        Task<object> GetABCAnalysisAsync(int? branchId = null);
        
        // ==================== FINANCIAL ANALYTICS ==================== //
        
        /// <summary>
        /// Profitability analysis by product/category
        /// </summary>
        Task<object> GetProfitabilityAnalysisAsync(DateTime startDate, DateTime endDate, string analysisType = "product");
        
        /// <summary>
        /// Cash flow predictions
        /// </summary>
        Task<object> PredictCashFlowAsync(int forecastDays = 30, int? branchId = null);
        
        /// <summary>
        /// Break-even analysis
        /// </summary>
        Task<object> GetBreakEvenAnalysisAsync(int? productId = null, int? branchId = null);
        
        /// <summary>
        /// Cost optimization insights
        /// </summary>
        Task<object> GetCostOptimizationInsightsAsync(DateTime startDate, DateTime endDate);
        
        /// <summary>
        /// Financial KPI dashboard data
        /// </summary>
        Task<object> GetFinancialKPIDashboardAsync(DateTime startDate, DateTime endDate, int? branchId = null);
        
        // ==================== OPERATIONAL ANALYTICS ==================== //
        
        /// <summary>
        /// Staff performance analytics
        /// </summary>
        Task<object> GetStaffPerformanceAnalyticsAsync(DateTime startDate, DateTime endDate, int? branchId = null);
        
        /// <summary>
        /// Peak hours analysis
        /// </summary>
        Task<object> GetPeakHoursAnalysisAsync(DateTime startDate, DateTime endDate, int? branchId = null);
        
        /// <summary>
        /// Supplier performance scoring
        /// </summary>
        Task<object> GetSupplierPerformanceScoringAsync(DateTime startDate, DateTime endDate, int? supplierId = null);
        
        /// <summary>
        /// Branch comparison analytics
        /// </summary>
        Task<object> GetBranchComparisonAnalyticsAsync(DateTime startDate, DateTime endDate);
        
        // ==================== DASHBOARD & KPI ==================== //
        
        /// <summary>
        /// Executive dashboard summary
        /// </summary>
        Task<object> GetExecutiveDashboardAsync(DateTime startDate, DateTime endDate, int? branchId = null);
        
        /// <summary>
        /// Real-time business metrics
        /// </summary>
        Task<object> GetRealTimeMetricsAsync(int? branchId = null);
        
        /// <summary>
        /// Key performance indicators
        /// </summary>
        Task<object> GetKPIMetricsAsync(DateTime startDate, DateTime endDate, int? branchId = null);
        
        /// <summary>
        /// Business health score
        /// </summary>
        Task<object> GetBusinessHealthScoreAsync(int? branchId = null);
        
        // ==================== ALERTS & RECOMMENDATIONS ==================== //
        
        /// <summary>
        /// Get business insights and recommendations
        /// </summary>
        Task<object> GetBusinessInsightsAsync(int? branchId = null);
        
        /// <summary>
        /// Get anomaly detection alerts
        /// </summary>
        Task<object> GetAnomalyAlertsAsync(DateTime startDate, DateTime endDate, int? branchId = null);
        
        /// <summary>
        /// Strategic recommendations based on data
        /// </summary>
        Task<object> GetStrategicRecommendationsAsync(int? branchId = null);
        
        /// <summary>
        /// Market opportunity analysis
        /// </summary>
        Task<object> GetMarketOpportunityAnalysisAsync(DateTime startDate, DateTime endDate);
        
        // ==================== ADVANCED ANALYTICS ==================== //
        
        /// <summary>
        /// Customer lifetime value calculation
        /// </summary>
        Task<object> CalculateCustomerLifetimeValueAsync(int? customerId = null);
        
        /// <summary>
        /// Market basket analysis
        /// </summary>
        Task<object> GetMarketBasketAnalysisAsync(DateTime startDate, DateTime endDate, int? branchId = null);
        
        /// <summary>
        /// Price elasticity analysis
        /// </summary>
        Task<object> GetPriceElasticityAnalysisAsync(int productId, DateTime startDate, DateTime endDate);
        
        /// <summary>
        /// Cohort analysis for customer retention
        /// </summary>
        Task<object> GetCohortAnalysisAsync(DateTime startDate, DateTime endDate, int? branchId = null);
    }
}