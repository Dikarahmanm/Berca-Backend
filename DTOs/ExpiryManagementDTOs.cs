using System.ComponentModel.DataAnnotations;
using Berca_Backend.Models;

namespace Berca_Backend.DTOs
{
    // ==================== PRODUCT BATCH DTOs ==================== //

    /// <summary>
    /// DTO for creating new product batch with expiry tracking
    /// </summary>
    public class CreateProductBatchDto
    {
        [Required(ErrorMessage = "Product ID is required")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Batch number is required")]
        [StringLength(50, ErrorMessage = "Batch number cannot exceed 50 characters")]
        public string BatchNumber { get; set; } = string.Empty;

        /// <summary>
        /// Expiry date (required for categories that require expiry tracking)
        /// </summary>
        public DateTime? ExpiryDate { get; set; }

        /// <summary>
        /// Production/Manufacturing date
        /// </summary>
        public DateTime? ProductionDate { get; set; }

        [Required(ErrorMessage = "Initial stock is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Initial stock must be greater than 0")]
        public int InitialStock { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Cost per unit must be greater than or equal to 0")]
        public decimal CostPerUnit { get; set; } = 0;

        [StringLength(100, ErrorMessage = "Supplier name cannot exceed 100 characters")]
        public string? SupplierName { get; set; }

        [StringLength(50, ErrorMessage = "Purchase order number cannot exceed 50 characters")]
        public string? PurchaseOrderNumber { get; set; }

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }

        public int? BranchId { get; set; }
    }

    /// <summary>
    /// DTO for updating product batch information
    /// </summary>
    public class UpdateProductBatchDto
    {
        [Required(ErrorMessage = "Batch number is required")]
        [StringLength(50, ErrorMessage = "Batch number cannot exceed 50 characters")]
        public string BatchNumber { get; set; } = string.Empty;

        public DateTime? ExpiryDate { get; set; }
        public DateTime? ProductionDate { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Current stock must be greater than or equal to 0")]
        public int CurrentStock { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Cost per unit must be greater than or equal to 0")]
        public decimal CostPerUnit { get; set; } = 0;

        [StringLength(100, ErrorMessage = "Supplier name cannot exceed 100 characters")]
        public string? SupplierName { get; set; }

        [StringLength(50, ErrorMessage = "Purchase order number cannot exceed 50 characters")]
        public string? PurchaseOrderNumber { get; set; }

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }

        public bool IsBlocked { get; set; } = false;

        [StringLength(200, ErrorMessage = "Block reason cannot exceed 200 characters")]
        public string? BlockReason { get; set; }
    }

    /// <summary>
    /// DTO for product batch response data
    /// </summary>
    public class ProductBatchDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime? ExpiryDate { get; set; }
        public DateTime? ProductionDate { get; set; }
        public int CurrentStock { get; set; }
        public int InitialStock { get; set; }
        public decimal CostPerUnit { get; set; }
        public string? SupplierName { get; set; }
        public string? PurchaseOrderNumber { get; set; }
        public string? Notes { get; set; }
        public bool IsBlocked { get; set; }
        public string? BlockReason { get; set; }
        public bool IsExpired { get; set; }
        public bool IsDisposed { get; set; }
        public DateTime? DisposalDate { get; set; }
        public string? DisposalMethod { get; set; }
        public int? BranchId { get; set; }
        public string? BranchName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? CreatedByUserName { get; set; }
        public string? UpdatedByUserName { get; set; }

        // Computed properties
        public int? DaysUntilExpiry { get; set; }
        public ExpiryStatus ExpiryStatus { get; set; }
        public int AvailableStock { get; set; }
    }

    // ==================== EXPIRY TRACKING DTOs ==================== //

    /// <summary>
    /// DTO for products expiring soon
    /// </summary>
    public class ExpiringProductDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductBarcode { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryColor { get; set; } = string.Empty;
        public int BatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public int DaysUntilExpiry { get; set; }
        public ExpiryStatus ExpiryStatus { get; set; }
        public int CurrentStock { get; set; }
        public int AvailableStock { get; set; }
        public decimal ValueAtRisk { get; set; }
        public decimal CostPerUnit { get; set; }
        public ExpiryUrgency UrgencyLevel { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? BranchId { get; set; }
        public string? BranchName { get; set; }
        public string? SupplierName { get; set; }
        public bool IsBlocked { get; set; }
    }

    /// <summary>
    /// DTO for expired products
    /// </summary>
    public class ExpiredProductDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductBarcode { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int BatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public int DaysExpired { get; set; }
        public int CurrentStock { get; set; }
        public decimal ValueLost { get; set; }
        public decimal CostPerUnit { get; set; }
        public DateTime ExpiredAt { get; set; }
        public bool RequiresDisposal { get; set; }
        public DisposalUrgency DisposalUrgency { get; set; }
        public int? BranchId { get; set; }
        public string? BranchName { get; set; }
        public bool IsDisposed { get; set; }
        public DateTime? DisposalDate { get; set; }
        public string? DisposalMethod { get; set; }
        public string? SupplierName { get; set; }
    }

    // ==================== FIFO RECOMMENDATION DTOs ==================== //

    /// <summary>
    /// DTO for individual batch recommendations in FIFO order
    /// </summary>
    public class BatchRecommendationDto
    {
        public int BatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime? ExpiryDate { get; set; }
        public int AvailableStock { get; set; }
        public decimal CostPerUnit { get; set; }
        public ExpiryStatus ExpiryStatus { get; set; }
        public int? DaysUntilExpiry { get; set; }
        public int RecommendedSaleOrder { get; set; } // 1 = sell first, 2 = sell second, etc.
        public string RecommendationReason { get; set; } = string.Empty;
    }

    // ==================== DISPOSAL DTOs ==================== //

    /// <summary>
    /// DTO for disposing expired products
    /// </summary>
    public class DisposeExpiredProductsDto
    {
        [Required]
        public List<int> BatchIds { get; set; } = new();

        [Required(ErrorMessage = "Disposal method is required")]
        [StringLength(100, ErrorMessage = "Disposal method cannot exceed 100 characters")]
        public string DisposalMethod { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }
    }

    // ==================== ANALYTICS DTOs ==================== //

    /// <summary>
    /// DTO for expiry analytics per branch
    /// </summary>
    public class ExpiryAnalyticsDto
    {
        public int? BranchId { get; set; }
        public string? BranchName { get; set; }
        public DateTime AnalysisDate { get; set; }
        public int TotalProductsWithExpiry { get; set; }
        public int ExpiringIn7Days { get; set; }
        public int ExpiringIn3Days { get; set; }
        public int ExpiredProducts { get; set; }
        public int DisposedProducts { get; set; }
        public decimal ValueAtRisk { get; set; }
        public decimal ValueLost { get; set; }
        public decimal WastagePercentage { get; set; }
        public List<CategoryExpiryStatsDto> CategoryStats { get; set; } = new();
    }

    /// <summary>
    /// DTO for category-wise expiry statistics
    /// </summary>
    public class CategoryExpiryStatsDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryColor { get; set; } = string.Empty;
        public int TotalProducts { get; set; }
        public int ExpiringProducts { get; set; }
        public int ExpiredProducts { get; set; }
        public decimal ValueAtRisk { get; set; }
        public decimal ValueLost { get; set; }
    }

    // ==================== FILTER DTOs ==================== //

    /// <summary>
    /// DTO for filtering expiring products
    /// </summary>
    public class ExpiringProductsFilterDto
    {
        public int? CategoryId { get; set; }
        public int? BranchId { get; set; }
        public ExpiryStatus? ExpiryStatus { get; set; }
        public int? DaysUntilExpiry { get; set; }
        public bool? IncludeBlocked { get; set; } = false;
        public string? SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "expiry_date";
        public string SortOrder { get; set; } = "asc";
    }

    /// <summary>
    /// DTO for filtering expired products
    /// </summary>
    public class ExpiredProductsFilterDto
    {
        public int? CategoryId { get; set; }
        public int? BranchId { get; set; }
        public bool? IsDisposed { get; set; }
        public DateTime? ExpiredAfter { get; set; }
        public DateTime? ExpiredBefore { get; set; }
        public string? SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "expiry_date";
        public string SortOrder { get; set; } = "desc";
    }

    // ==================== DISPOSAL DTOs ==================== //

    /// <summary>
    /// DTO for disposing a product batch
    /// </summary>
    public class DisposeBatchDto
    {
        [Required]
        [StringLength(100, ErrorMessage = "Disposal method cannot exceed 100 characters")]
        public string DisposalMethod { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Disposal reason cannot exceed 500 characters")]
        public string? DisposalReason { get; set; }

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }

        public bool ForceDisposal { get; set; } = false;
    }

    // ==================== EXPIRY VALIDATION DTO ==================== //

    /// <summary>
    /// DTO for validating expiry requirements when creating/updating products
    /// </summary>
    public class ExpiryValidationDto
    {
        public int ProductId { get; set; }
        public int CategoryId { get; set; }
        public bool CategoryRequiresExpiry { get; set; }
        public DateTime? ProvidedExpiryDate { get; set; }
        public bool IsValid { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
    }

    // ==================== ENUMS ==================== //

    /// <summary>
    /// Expiry urgency levels for notifications
    /// </summary>
    public enum ExpiryUrgency
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Critical = 3
    }

    /// <summary>
    /// Disposal urgency levels
    /// </summary>
    public enum DisposalUrgency
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Critical = 3
    }

    /// <summary>
    /// Expiry notification types constants
    /// </summary>
    public static class ExpiryNotificationTypes
    {
        public const string EXPIRY_WARNING = "expiry_warning";
        public const string EXPIRY_URGENT = "expiry_urgent";
        public const string EXPIRY_EXPIRED = "expiry_expired";
        public const string EXPIRY_REQUIRED = "expiry_required";
        public const string DISPOSAL_COMPLETED = "disposal_completed";
        public const string EXPIRY_DAILY_SUMMARY = "expiry_daily_summary";
        public const string FIFO_RECOMMENDATION = "fifo_recommendation";
    }

    // ==================== ADVANCED ANALYTICS DTOs (NEW) ==================== //

    /// <summary>
    /// DTO for comprehensive expiry analytics with financial calculations
    /// </summary>
    public class ComprehensiveExpiryAnalyticsDto
    {
        public int? BranchId { get; set; }
        public string? BranchName { get; set; }
        public DateTime AnalysisTimestamp { get; set; }
        
        // Additional properties for service compatibility
        public DateTime AnalysisDate { get; set; }
        public int TotalBatchesWithExpiry { get; set; }
        public int ExpiredBatches { get; set; }
        public int ExpiringNext7Days { get; set; }
        public int ExpiringNext30Days { get; set; }
        public decimal ExpiredStockValue { get; set; }
        public decimal AtRiskStockValue { get; set; }
        public decimal MonthlyExpiryRate { get; set; }
        public decimal PreventionOpportunityValue { get; set; }
        public List<CategoryExpiryPerformance> CategoryPerformance { get; set; } = new();
        public List<ExpiryTrendData> ExpiryTrends { get; set; } = new();
        public List<ExpiryActionRecommendation> ActionableRecommendations { get; set; } = new();
        public int ProjectedExpiryNext30Days { get; set; }
        public decimal EstimatedSavingsOpportunity { get; set; }

        // Financial Metrics
        public decimal TotalStockValue { get; set; }
        public decimal ValueAtRiskNext7Days { get; set; }
        public decimal ValueAtRiskNext30Days { get; set; }
        public decimal ValueExpiredLast30Days { get; set; }
        public decimal WastePreventionOpportunity { get; set; }

        // Performance Metrics
        public decimal WastagePercentage { get; set; }
        public decimal ExpiryPreventionRate { get; set; }
        public decimal RecoveryRate { get; set; }
        public int FifoComplianceScore { get; set; }

        // Product Counts
        public int TotalBatchesTracked { get; set; }
        public int BatchesExpiringToday { get; set; }
        public int BatchesExpiringThisWeek { get; set; }
        public int BatchesExpiringThisMonth { get; set; }
        public int ExpiredBatchesPendingDisposal { get; set; }

        // Category Breakdown
        public List<CategoryExpiryAnalyticsDto> CategoryAnalytics { get; set; } = new();

        // Risk Assessment
        public List<HighRiskBatchDto> HighRiskBatches { get; set; } = new();

        // Recommendations
        public List<ExpiryRecommendationDto> Recommendations { get; set; } = new();

        // Trends
        public List<ExpiryTrendPointDto> WastageTrend { get; set; } = new();
        public List<ExpiryTrendPointDto> PreventionTrend { get; set; } = new();
    }

    /// <summary>
    /// DTO for category expiry analytics breakdown
    /// </summary>
    public class CategoryExpiryAnalyticsDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryColor { get; set; } = string.Empty;
        
        public decimal StockValue { get; set; }
        public decimal ValueAtRisk { get; set; }
        public decimal WastagePercentage { get; set; }
        
        public int TotalBatches { get; set; }
        public int ExpiringBatches { get; set; }
        public int ExpiredBatches { get; set; }
        
        public decimal AverageShelfLife { get; set; }
        public decimal OptimalTurnoverRate { get; set; }
    }

    /// <summary>
    /// DTO for high-risk batch identification
    /// </summary>
    public class HighRiskBatchDto
    {
        public int BatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public int DaysUntilExpiry { get; set; }
        public int CurrentStock { get; set; }
        public decimal ValueAtRisk { get; set; }
        public string RiskLevel { get; set; } = string.Empty;
        public string RecommendedAction { get; set; } = string.Empty;
        public decimal DiscountSuggestion { get; set; }
    }

    /// <summary>
    /// DTO for expiry recommendations
    /// </summary>
    public class ExpiryRecommendationDto
    {
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public decimal PotentialSavings { get; set; }
        public string ActionUrl { get; set; } = string.Empty;
        public List<int> AffectedBatchIds { get; set; } = new();
    }

    /// <summary>
    /// DTO for expiry trend data points
    /// </summary>
    public class ExpiryTrendPointDto
    {
        public DateTime Date { get; set; }
        public decimal Value { get; set; }
        public string Label { get; set; } = string.Empty;
        public string MetricType { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for smart FIFO recommendations with advanced scoring
    /// </summary>
    public class SmartFifoRecommendationDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductBarcode { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        
        // Batch-specific properties for service compatibility
        public int BatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int DaysToExpiry { get; set; }
        public int PriorityScore { get; set; }
        public string UrgencyLevel { get; set; } = string.Empty;
        public string RecommendedAction { get; set; } = string.Empty;

        // FIFO Batch Details
        public List<FifoBatchScoreDto> BatchRecommendations { get; set; } = new();

        // Smart Recommendations
        public decimal OptimalSellingPrice { get; set; }
        public decimal DiscountRecommendation { get; set; }
        public int RecommendedSaleQuantity { get; set; }
        public DateTime RecommendedSaleDeadline { get; set; }
        
        // Pricing properties for service compatibility
        public decimal CurrentPrice { get; set; }
        public decimal RecommendedPrice { get; set; }
        public decimal SuggestedDiscount { get; set; }
        public decimal MinimumViablePrice { get; set; }

        // Business Intelligence
        public decimal PotentialLoss { get; set; }
        public decimal PreventableLoss { get; set; }
        public string SalesStrategy { get; set; } = string.Empty;
        public string MarketingAction { get; set; } = string.Empty;
        
        // Financial metrics for service compatibility
        public decimal PotentialRevenue { get; set; }
        public decimal EstimatedLoss { get; set; }
        public decimal NetBenefit { get; set; }
        
        // Sell-through and recommendations
        public int EstimatedSellThroughDays { get; set; }
        public List<string> RecommendedSalesChannels { get; set; } = new();
        public List<string> ImmediateActions { get; set; } = new();
        public ActionTimeline Timeline { get; set; } = new();

        // Performance Scoring
        public int FifoScore { get; set; }
        public string FifoGrade { get; set; } = string.Empty;
        public string OptimizationOpportunity { get; set; } = string.Empty;

        // Transfer Opportunities
        public List<BranchTransferOpportunityDto> TransferOpportunities { get; set; } = new();
    }

    /// <summary>
    /// DTO for FIFO batch scoring details
    /// </summary>
    public class FifoBatchScoreDto
    {
        public int BatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime? ExpiryDate { get; set; }
        public int DaysUntilExpiry { get; set; }
        public int CurrentStock { get; set; }
        public decimal ValueAtRisk { get; set; }

        // Scoring Metrics
        public int FifoScore { get; set; }
        public int UrgencyScore { get; set; }
        public int VelocityScore { get; set; }
        public int FinancialImpactScore { get; set; }
        public int OverallScore { get; set; }

        // Recommendations
        public int RecommendedSaleOrder { get; set; }
        public string ActionPriority { get; set; } = string.Empty;
        public string ReasonCode { get; set; } = string.Empty;
        public List<string> ActionItems { get; set; } = new();
    }

    /// <summary>
    /// DTO for branch transfer opportunities
    /// </summary>
    public class BranchTransferOpportunityDto
    {
        public int TargetBranchId { get; set; }
        public string TargetBranchName { get; set; } = string.Empty;
        public int RecommendedQuantity { get; set; }
        public decimal TransferBenefit { get; set; }
        public string TransferReason { get; set; } = string.Empty;
        public DateTime RecommendedTransferDate { get; set; }
        public decimal TransferCost { get; set; }
        public decimal NetBenefit { get; set; }
        public string Priority { get; set; } = string.Empty;
    }

    // ==================== MULTI-BRANCH COORDINATION DTOs ==================== //

    /// <summary>
    /// DTO for inter-branch transfer recommendations
    /// </summary>
    public class InterBranchTransferRecommendationDto
    {
        public int Id { get; set; }
        public string RecommendationType { get; set; } = string.Empty;
        
        // Source and Target
        public int SourceBranchId { get; set; }
        public string SourceBranchName { get; set; } = string.Empty;
        public int TargetBranchId { get; set; }
        public string TargetBranchName { get; set; } = string.Empty;
        
        // Compatibility aliases for service - made settable
        public int FromBranchId { get => SourceBranchId; set => SourceBranchId = value; }
        public string FromBranchName { get => SourceBranchName; set => SourceBranchName = value; }
        public int ToBranchId { get => TargetBranchId; set => TargetBranchId = value; }
        public string ToBranchName { get => TargetBranchName; set => TargetBranchName = value; }

        // Product Details
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int BatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime? ExpiryDate { get; set; }

        // Transfer Details
        public int RecommendedQuantity { get; set; }
        public decimal EstimatedBenefit { get; set; }
        public decimal TransferCost { get; set; }
        public decimal NetBenefit { get; set; }
        public decimal PotentialSavings { get; set; }

        // Business Logic
        public string Reason { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public DateTime RecommendedTransferDate { get; set; }
        public int UrgencyScore { get; set; }
        
        // Risk Assessment
        public decimal RiskLevel { get; set; }
        public string RiskFactors { get; set; } = string.Empty;
        public decimal SuccessLikelihood { get; set; }

        public DateTime GeneratedAt { get; set; }
        
        // Additional properties for service compatibility
        public List<string> TransferReasons { get; set; } = new();
        public object? Logistics { get; set; }
    }

    /// <summary>
    /// DTO for branch performance comparison
    /// </summary>
    public class BranchPerformanceComparisonDto
    {
        public DateTime AnalysisDate { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }

        public List<BranchPerformanceMetricsDto> BranchMetrics { get; set; } = new();
        public BenchmarkMetricsDto SystemBenchmarks { get; set; } = new();
        public List<BranchPerformanceInsightDto> KeyInsights { get; set; } = new();
        
        // Flat properties for service compatibility
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal NetProfitMargin { get; set; }
        public decimal RevenuePerSquareMeter { get; set; }
        public int TransactionCount { get; set; }
        public decimal AverageTransactionValue { get; set; }
        public decimal SalesPerEmployee { get; set; }
        public decimal InventoryTurnover { get; set; }
        public decimal WastagePercentage { get; set; }
        public decimal PreventedLoss { get; set; }
        public int TotalProducts { get; set; }
        public int ActiveProducts { get; set; }
        public int ExpiringProducts { get; set; }
        public decimal ExpiryRisk { get; set; }
        public int EfficiencyScore { get; set; }
        public int ProfitabilityScore { get; set; }
        public int OverallRating { get; set; }
        public List<string> Strengths { get; set; } = new();
        public List<string> ImprovementAreas { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        
        // Additional missing properties for service compatibility
        public decimal StockoutRate { get; set; }
        public decimal ExcessStockValue { get; set; }
        public decimal ExpiryPreventionScore { get; set; }
        public decimal ValuePreserved { get; set; }
        public decimal OverallPerformanceScore { get; set; }
        public decimal RevenueGrowth { get; set; }
        public List<string> StrengthAreas { get; set; } = new();
        public List<BestPractice> BestPractices { get; set; } = new();
    }

    /// <summary>
    /// DTO for individual branch performance metrics
    /// </summary>
    public class BranchPerformanceMetricsDto
    {
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public string BranchCode { get; set; } = string.Empty;

        // Financial Performance
        public decimal TotalRevenue { get; set; }
        public decimal WastageValue { get; set; }
        public decimal PreventedLoss { get; set; }
        public decimal NetPerformance { get; set; }

        // Operational Efficiency
        public decimal WastagePercentage { get; set; }
        public decimal FifoComplianceRate { get; set; }
        public decimal InventoryTurnoverRate { get; set; }
        public decimal ExpiryPreventionRate { get; set; }

        // Volume Metrics
        public int TotalTransactions { get; set; }
        public int ProductsManaged { get; set; }
        public int BatchesProcessed { get; set; }
        public int ExpiredBatches { get; set; }

        // Performance Scores
        public int EfficiencyScore { get; set; }
        public int ProfitabilityScore { get; set; }
        public int ComplianceScore { get; set; }
        public int OverallScore { get; set; }

        // Rankings
        public int RevenueRank { get; set; }
        public int EfficiencyRank { get; set; }
        public int WastageRank { get; set; }
        public int OverallRank { get; set; }
    }

    /// <summary>
    /// DTO for system-wide benchmark metrics
    /// </summary>
    public class BenchmarkMetricsDto
    {
        public decimal AverageWastagePercentage { get; set; }
        public decimal BestWastagePercentage { get; set; }
        public decimal SystemFifoCompliance { get; set; }
        public decimal AverageInventoryTurnover { get; set; }
        public decimal TopPerformerScore { get; set; }
        public decimal SystemEfficiencyTarget { get; set; }
    }

    /// <summary>
    /// DTO for branch performance insights
    /// </summary>
    public class BranchPerformanceInsightDto
    {
        public string InsightType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Impact { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public List<int> AffectedBranchIds { get; set; } = new();
        public List<string> ActionItems { get; set; } = new();
    }

    /// <summary>
    /// DTO for cross-branch optimization opportunities
    /// </summary>
    public class CrossBranchOpportunityDto
    {
        public string OpportunityType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        
        public decimal PotentialSavings { get; set; }
        public decimal ImplementationCost { get; set; }
        public decimal NetBenefit { get; set; }
        public decimal EstimatedBenefit { get; set; }
        
        public string Impact { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Complexity { get; set; } = string.Empty;
        
        public List<int> AffectedBranchIds { get; set; } = new();
        public List<string> RequiredActions { get; set; } = new();
        public DateTime RecommendedImplementationDate { get; set; }
        
        public string Category { get; set; } = string.Empty;
        public int ConfidenceScore { get; set; }
    }

    /// <summary>
    /// DTO for inventory distribution optimization plan
    /// </summary>
    public class InventoryDistributionPlanDto
    {
        public DateTime GeneratedAt { get; set; }
        public string OptimizationScope { get; set; } = string.Empty;
        
        public List<DistributionRecommendationDto> Recommendations { get; set; } = new();
        public decimal TotalOptimizationValue { get; set; }
        public decimal ImplementationCost { get; set; }
        public decimal NetBenefit { get; set; }
        
        public DistributionMetricsDto CurrentMetrics { get; set; } = new();
        public DistributionMetricsDto ProjectedMetrics { get; set; } = new();
        
        public List<string> RiskFactors { get; set; } = new();
        public List<string> SuccessFactors { get; set; } = new();
    }

    /// <summary>
    /// DTO for distribution recommendations
    /// </summary>
    public class DistributionRecommendationDto
    {
        public string ActionType { get; set; } = string.Empty;
        public int SourceBranchId { get; set; }
        public string SourceBranchName { get; set; } = string.Empty;
        public int TargetBranchId { get; set; }
        public string TargetBranchName { get; set; } = string.Empty;
        
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal EstimatedBenefit { get; set; }
        
        public string Rationale { get; set; } = string.Empty;
        public DateTime RecommendedExecutionDate { get; set; }
        public string Priority { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for distribution metrics
    /// </summary>
    public class DistributionMetricsDto
    {
        public decimal TotalInventoryValue { get; set; }
        public decimal AverageWastageRate { get; set; }
        public decimal DistributionEfficiency { get; set; }
        public decimal StockBalance { get; set; }
        public int OptimalDistributionScore { get; set; }
    }

    /// <summary>
    /// DTO for demand forecasting by branch
    /// </summary>
    public class BranchDemandForecastDto
    {
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        
        public List<ProductDemandForecastDto> ProductForecasts { get; set; } = new();
        public decimal TotalForecastedDemand { get; set; }
        public decimal ForecastConfidence { get; set; }
        
        public DateTime ForecastPeriodStart { get; set; }
        public DateTime ForecastPeriodEnd { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// DTO for product demand forecasting
    /// </summary>
    public class ProductDemandForecastDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        
        public List<DemandDataPointDto> ForecastPoints { get; set; } = new();
        public decimal AverageDailyDemand { get; set; }
        public decimal TotalForecastedDemand { get; set; }
        public decimal SeasonalityFactor { get; set; }
        public decimal TrendFactor { get; set; }
        public decimal ConfidenceLevel { get; set; }
    }

    /// <summary>
    /// DTO for demand data points
    /// </summary>
    public class DemandDataPointDto
    {
        public DateTime Date { get; set; }
        public decimal ForecastedDemand { get; set; }
        public decimal ConfidenceInterval { get; set; }
        public string ForecastType { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for optimization execution results
    /// </summary>
    public class OptimizationExecutionResultDto
    {
        public bool IsSuccess { get; set; }
        public bool WasDryRun { get; set; }
        public DateTime ExecutedAt { get; set; }
        public int ExecutedByUserId { get; set; }
        
        public List<ExecutedActionDto> ExecutedActions { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        
        public decimal TotalValue { get; set; }
        public decimal EstimatedSavings { get; set; }
        public int ActionsExecuted { get; set; }
        public int ActionsFailed { get; set; }
        
        public string ExecutionSummary { get; set; } = string.Empty;
        public List<string> NextSteps { get; set; } = new();
    }

    /// <summary>
    /// DTO for executed optimization actions
    /// </summary>
    public class ExecutedActionDto
    {
        public string ActionType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public decimal Value { get; set; }
        public string Result { get; set; } = string.Empty;
        public DateTime ExecutedAt { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    // ==================== SMART NOTIFICATION DTOS ==================== //

    /// <summary>
    /// DTO for smart notifications with escalation rules
    /// </summary>
    public class SmartNotificationDto
    {
        public string Type { get; set; } = string.Empty;
        public Models.NotificationPriority Priority { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public decimal PotentialLoss { get; set; }
        public DateTime ActionDeadline { get; set; }
        public string ActionUrl { get; set; } = string.Empty;
        
        public List<string> ActionItems { get; set; } = new();
        public EscalationRule EscalationRule { get; set; } = new();
        public BusinessImpact BusinessImpact { get; set; } = new();
        public List<AffectedBatch> AffectedBatches { get; set; } = new();
    }

    /// <summary>
    /// DTO for escalation rules
    /// </summary>
    public class EscalationRule
    {
        public int EscalateAfterHours { get; set; }
        public List<string> EscalateToRoles { get; set; } = new();
        public bool RequireAcknowledgment { get; set; }
        public List<NotificationChannel> NotificationChannels { get; set; } = new();
    }

    /// <summary>
    /// DTO for business impact assessment
    /// </summary>
    public class BusinessImpact
    {
        public decimal FinancialRisk { get; set; }
        public string OperationalImpact { get; set; } = string.Empty;
        public string CustomerImpact { get; set; } = string.Empty;
        public string ComplianceRisk { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for affected batch information
    /// </summary>
    public class AffectedBatch
    {
        public int BatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Value { get; set; }
        public DateTime ExpiryDate { get; set; }
    }

    /// <summary>
    /// DTO for escalation alerts
    /// </summary>
    public class EscalationAlert
    {
        public int OriginalNotificationId { get; set; }
        public DateTime EscalatedAt { get; set; }
        public string EscalationReason { get; set; } = string.Empty;
        public int EscalatedToUserCount { get; set; }
        public string Priority { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for notification preferences
    /// </summary>
    public class NotificationPreferencesDto
    {
        public int UserId { get; set; }
        public bool EmailNotifications { get; set; }
        public bool PushNotifications { get; set; }
        public bool SmsNotifications { get; set; }
        public bool ExpiryAlerts { get; set; }
        public bool StockAlerts { get; set; }
        public bool FinancialAlerts { get; set; }
        public string AlertFrequency { get; set; } = string.Empty;
        public QuietHours QuietHours { get; set; } = new();
    }

    /// <summary>
    /// DTO for quiet hours configuration
    /// </summary>
    public class QuietHours
    {
        public string Start { get; set; } = string.Empty;
        public string End { get; set; } = string.Empty;
    }

    // ==================== NOTIFICATION ENUMS AND TYPES ==================== //

    /// <summary>
    /// Notification channel options
    /// </summary>
    public enum NotificationChannel
    {
        PUSH,
        EMAIL,
        SMS,
        IN_APP
    }

    /// <summary>
    /// Notification type constants
    /// </summary>
    public static class NotificationTypes
    {
        public const string CRITICAL_EXPIRY = "CriticalExpiry";
        public const string LOW_STOCK_EXPIRY_RISK = "LowStockExpiryRisk";
        public const string FINANCIAL_RISK_SUMMARY = "FinancialRiskSummary";
        public const string HIGH_VALUE_RISK = "HighValueRisk";
        public const string HIGH_FINANCIAL_RISK = "HighFinancialRisk";
    }

    // ==================== MISSING EXPIRY MANAGEMENT DTOS ==================== //

    /// <summary>
    /// DTO for category expiry performance
    /// </summary>
    public class CategoryExpiryPerformance
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal TotalValue { get; set; }
        public decimal ExpiredValue { get; set; }
        public decimal WastagePercentage { get; set; }
        public int TotalBatches { get; set; }
        public int ExpiredBatches { get; set; }
        public decimal AverageShelfLife { get; set; }
        public string Performance { get; set; } = string.Empty;
        public int NearExpiryBatches { get; set; }
        public decimal WastePercentage { get; set; }
    }

    /// <summary>
    /// DTO for expiry trend data
    /// </summary>
    public class ExpiryTrendData
    {
        public DateTime Month { get; set; }
        public decimal ExpiredValue { get; set; }
        public int ExpiredBatches { get; set; }
        public decimal WastagePercentage { get; set; }
        public decimal TrendDirection { get; set; }
        public string TrendIndicator { get; set; } = string.Empty;
        public string Period { get; set; } = string.Empty;
        public string PeriodDisplay { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for expiry action recommendations
    /// </summary>
    public class ExpiryActionRecommendation
    {
        public int BatchId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
        public int DaysToExpiry { get; set; }
        public decimal CurrentValue { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal ExpectedBenefit { get; set; }
        public string Priority { get; set; } = string.Empty;
        public DateTime RecommendedDate { get; set; }
        
        // Additional properties for service compatibility
        public int AffectedBatchesCount { get; set; }
        public decimal PotentialSavings { get; set; }
        public DateTime Deadline { get; set; }
        public List<string> ActionItems { get; set; } = new();
    }

    /// <summary>
    /// DTO for pricing recommendations
    /// </summary>
    public class PricingRecommendation
    {
        public decimal CurrentPrice { get; set; }
        public decimal RecommendedPrice { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal ExpectedSalesVelocity { get; set; }
        public decimal ProjectedRevenue { get; set; }
        public string Strategy { get; set; } = string.Empty;
        public bool IsViable { get; set; }
        public string Rationale { get; set; } = string.Empty;
        
        // Additional properties for service compatibility
        public decimal OptimalPrice { get; set; }
        public decimal MinimumPrice { get; set; }
    }

    /// <summary>
    /// DTO for financial impact analysis
    /// </summary>
    public class FinancialImpact
    {
        public decimal BaseValue { get; set; }
        public decimal ProjectedRevenue { get; set; }
        public decimal PotentialLoss { get; set; }
        public decimal RecoveryAmount { get; set; }
        public decimal NetImpact { get; set; }
        public decimal RecoveryPercentage { get; set; }
        public string ImpactLevel { get; set; } = string.Empty;
        
        // Additional properties for service compatibility
        public decimal PotentialRevenue { get; set; }
        public decimal EstimatedLoss { get; set; }
        public decimal NetBenefit { get; set; }
    }

    /// <summary>
    /// DTO for action timeline
    /// </summary>
    public class ActionTimeline
    {
        public DateTime OptimalActionDate { get; set; }
        public DateTime LastActionDate { get; set; }
        public int DaysRemaining { get; set; }
        public string Urgency { get; set; } = string.Empty;
        public List<TimelineEvent> Events { get; set; } = new();
        public bool IsActionable { get; set; }
        public string StatusMessage { get; set; } = string.Empty;
        
        // Additional properties for service compatibility
        public string ImmediateAction { get; set; } = string.Empty;
        public string ShortTerm { get; set; } = string.Empty;
        public string MediumTerm { get; set; } = string.Empty;
        public DateTime ReviewDate { get; set; }
    }

    /// <summary>
    /// DTO for timeline events
    /// </summary>
    public class TimelineEvent
    {
        public DateTime Date { get; set; }
        public string Event { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
    }

    // ==================== MULTI-BRANCH COORDINATION DTOS ==================== //

    /// <summary>
    /// DTO for demand forecast
    /// </summary>
    public class DemandForecastDto
    {
        public DateTime GeneratedAt { get; set; }
        public string ForecastPeriod { get; set; } = string.Empty;
        public List<ProductDemandForecast> ProductDemandForecasts { get; set; } = new();
        public decimal OverallAccuracy { get; set; }
        public decimal TotalProjectedSales { get; set; }
        public List<ProductDemandSummary> HighDemandProducts { get; set; } = new();
        public List<ProductDemandSummary> LowDemandProducts { get; set; } = new();
    }

    /// <summary>
    /// DTO for optimization result
    /// </summary>
    public class OptimizationResultDto
    {
        public DateTime ExecutedAt { get; set; }
        public string OptimizationType { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public int ActionsExecuted { get; set; }
        public int ActionsFailed { get; set; }
        public string ExecutionSummary { get; set; } = string.Empty;
        public List<string> NextSteps { get; set; } = new();
    }

    /// <summary>
    /// DTO for optimization parameters
    /// </summary>
    public class OptimizationParametersDto
    {
        public DateTime TargetDate { get; set; }
        public List<int> BranchIds { get; set; } = new();
        public string OptimizationScope { get; set; } = string.Empty;
        public decimal MaxTransferCost { get; set; }
        public bool IncludeExpirySensitive { get; set; } = true;
        public string Priority { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for product demand forecast
    /// </summary>
    public class ProductDemandForecast
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal ProjectedDemand { get; set; }
        public decimal Confidence { get; set; }
        public string TrendDirection { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for product demand summary
    /// </summary>
    public class ProductDemandSummary
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal DemandScore { get; set; }
        public string Category { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for best practices
    /// </summary>
    public class BestPractice
    {
        public string Category { get; set; } = string.Empty;
        public string Practice { get; set; } = string.Empty;
        public string Impact { get; set; } = string.Empty;
    }
}