// DTOs/DashboardDTOs.cs - Dashboard DTOs (Separate File)

namespace Berca_Backend.DTOs
{
    public class DashboardKPIDto
    {
        public decimal TodayRevenue { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public decimal YearlyRevenue { get; set; }
        public int TodayTransactions { get; set; }
        public int MonthlyTransactions { get; set; }
        public decimal AverageTransactionValue { get; set; }
        public decimal TotalProfit { get; set; }
        public int TotalProducts { get; set; }
        public int LowStockProducts { get; set; }
        public int TotalMembers { get; set; }
        public decimal InventoryValue { get; set; }
    }

    public class ChartDataDto
    {
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public DateTime Date { get; set; }
        public string? Color { get; set; }
    }

    public class TopProductDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductBarcode { get; set; } = string.Empty;
        public int TotalQuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalProfit { get; set; }
        public int TransactionCount { get; set; }
        public decimal WeightedScore { get; set; }
        public decimal NormalizedScore { get; set; } // ✅ ADDED: Score out of 100 (max)
        public decimal ProfitMargin { get; set; }
        public decimal AverageQuantityPerTransaction { get; set; }
        public string PerformanceCategory { get; set; } = string.Empty;
        public string PerformanceBadgeColor { get; set; } = string.Empty;
        public DateTime LastSaleDate { get; set; } // ✅ ADDED: For time-based analysis
        public int DaysSinceLastSale { get; set; } // ✅ ADDED: Days since last sale
    }

    public class CategorySalesDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryColor { get; set; } = string.Empty;
        public int TotalQuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
        public int ProductCount { get; set; }
        public int TransactionCount { get; set; }
    }

    public class QuickStatsDto
    {
        public decimal TodayRevenue { get; set; }
        public int TodayTransactions { get; set; }
        public decimal RevenueGrowthPercentage { get; set; }
        public int PendingOrders { get; set; }
        public int LowStockAlerts { get; set; }
        public int ActiveMembers { get; set; }
    }

    public class RecentTransactionDto
    {
        public int Id { get; set; }
        public string SaleNumber { get; set; } = string.Empty;
        public DateTime SaleDate { get; set; }
        public decimal Total { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CashierName { get; set; } = string.Empty;
        public int ItemCount { get; set; }
    }

    public class SalesReportDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalTransactions { get; set; }
        public int TotalItemsSold { get; set; } // ✅ RENAMED: From TotalItemsSold for clarity
        public decimal AverageTransactionValue { get; set; }
        public decimal TotalProfit { get; set; } // ✅ ADDED: Include total profit
        public List<PaymentMethodSummaryDto> PaymentMethodBreakdown { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }

    public class InventoryReportDto
    {
        public int TotalProducts { get; set; }
        public decimal TotalInventoryValue { get; set; }
        public int LowStockProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public List<CategoryInventoryDto> CategoryBreakdown { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }

    public class CategoryInventoryDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryColor { get; set; } = string.Empty;
        public int ProductCount { get; set; }
        public decimal TotalValue { get; set; }
        public int LowStockCount { get; set; }
    }

    public class PaymentMethodSummaryDto
    {
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public int TransactionCount { get; set; }
        public decimal Percentage { get; set; }
    }

    // Financial Reports
    public class FinancialReportDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalCost { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal GrossProfitMargin { get; set; }
        public decimal TotalTax { get; set; }
        public decimal NetProfit { get; set; }
        public List<MonthlyProfitDto> MonthlyBreakdown { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }

    public class MonthlyProfitDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public decimal Cost { get; set; }
        public decimal Profit { get; set; }
    }

    // Customer Reports
    public class CustomerReportDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalActiveMembers { get; set; }
        public int NewMembersThisPeriod { get; set; }
        public decimal AverageOrderValue { get; set; }
        public decimal TotalMemberRevenue { get; set; }
        public decimal GuestRevenue { get; set; }
        public List<TopCustomerDto> TopCustomers { get; set; } = new();
        public List<MemberLoyaltyDto> LoyaltyAnalysis { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }

    public class TopCustomerDto
    {
        public int? MemberId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string MembershipType { get; set; } = "Guest";
        public decimal TotalSpent { get; set; }
        public int TransactionCount { get; set; }
        public decimal AverageOrderValue { get; set; }
        public DateTime LastPurchase { get; set; }
    }

    public class MemberLoyaltyDto
    {
        public string LoyaltyTier { get; set; } = string.Empty;
        public int MemberCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageSpend { get; set; }
        public decimal Percentage { get; set; }
    }

    // Export Options
    public class ReportExportDto
    {
        public string ReportType { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty; // "PDF" or "Excel"
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
    }

    // Worst Performing Products
    public class WorstProductDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductBarcode { get; set; } = string.Empty;
        public int TotalQuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalProfit { get; set; }
        public int TransactionCount { get; set; }
        public int DaysWithoutSale { get; set; }
        public int CurrentStock { get; set; }
        public decimal PerformanceScore { get; set; }
        public decimal NormalizedScore { get; set; } // ✅ ADDED: Score out of 100 (max)
        public string PerformanceCategory { get; set; } = string.Empty;
        public DateTime? LastSaleDate { get; set; } // ✅ ADDED
        public decimal StockTurnoverRatio { get; set; } // ✅ ADDED
    }

    // Enhanced Time Period Filter
    public class DateRangeFilter
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Period { get; set; } = string.Empty; // "today", "yesterday", "week", "month", "year", "custom"
    }

    // Scoring Configuration
    public class ScoringConfig
    {
        public decimal MaxScore { get; set; } = 100; // ✅ Maximum possible score
        public bool EnableDailyReset { get; set; } = true; // ✅ Reset scores daily
        public DateTime LastResetDate { get; set; }
        public ScoringWeights Weights { get; set; } = new();
    }

    public class ScoringWeights
    {
        public decimal QuantityWeight { get; set; } = 0.40m; // 40%
        public decimal RevenueWeight { get; set; } = 0.30m;  // 30%  
        public decimal ProfitWeight { get; set; } = 0.20m;   // 20%
        public decimal FrequencyWeight { get; set; } = 0.10m; // 10%
        public decimal TimeDecayFactor { get; set; } = 0.95m; // Daily decay factor
    }
}