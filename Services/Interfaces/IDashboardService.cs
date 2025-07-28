// Services/IDashboardService.cs - Sprint 2 Dashboard Service Interface
using Berca_Backend.DTOs;

namespace Berca_Backend.Services
{
    public interface IDashboardService
    {
        // Financial KPIs
        Task<DashboardKPIDto> GetDashboardKPIsAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<List<ChartDataDto>> GetSalesChartDataAsync(string period = "daily", DateTime? startDate = null, DateTime? endDate = null);
        Task<List<ChartDataDto>> GetRevenueChartDataAsync(string period = "monthly", DateTime? startDate = null, DateTime? endDate = null);

        // Product Analytics
        Task<List<TopProductDto>> GetTopSellingProductsAsync(int count = 10, DateTime? startDate = null, DateTime? endDate = null);
        Task<List<ProductDto>> GetLowStockAlertsAsync();
        Task<List<CategorySalesDto>> GetCategorySalesAsync(DateTime? startDate = null, DateTime? endDate = null);

        // Quick Stats
        Task<QuickStatsDto> GetQuickStatsAsync();
        Task<List<RecentTransactionDto>> GetRecentTransactionsAsync(int count = 10);

        // Reports
        Task<SalesReportDto> GenerateSalesReportAsync(DateTime startDate, DateTime endDate);
        Task<InventoryReportDto> GenerateInventoryReportAsync();
    }

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
        public string? Category { get; set; }
    }

    public class TopProductDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int TotalSold { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalProfit { get; set; }
    }

    public class CategorySalesDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryColor { get; set; } = string.Empty;
        public decimal TotalSales { get; set; }
        public int ProductCount { get; set; }
        public decimal Percentage { get; set; }
    }

    public class QuickStatsDto
    {
        public int TotalProducts { get; set; }
        public int ActiveProducts { get; set; }
        public int LowStockProducts { get; set; }
        public int TotalMembers { get; set; }
        public int ActiveMembers { get; set; }
        public int TodayTransactions { get; set; }
        public decimal TodayRevenue { get; set; }
        public int UnreadNotifications { get; set; }
    }

    public class RecentTransactionDto
    {
        public int SaleId { get; set; }
        public string SaleNumber { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ItemCount { get; set; }
    }

    public class SalesReportDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalProfit { get; set; }
        public int TotalTransactions { get; set; }
        public int TotalItems { get; set; }
        public decimal AverageTransactionValue { get; set; }
        public List<DailySalesDto> DailySales { get; set; } = new();
        public List<TopProductDto> TopProducts { get; set; } = new();
        public List<PaymentMethodSummaryDto> PaymentMethods { get; set; } = new();
    }

    public class InventoryReportDto
    {
        public decimal TotalValue { get; set; }
        public int TotalProducts { get; set; }
        public int LowStockProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public List<CategoryInventoryDto> CategoryBreakdown { get; set; } = new();
        public List<ProductDto> LowStockItems { get; set; } = new();
    }

    public class CategoryInventoryDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public int ProductCount { get; set; }
        public decimal TotalValue { get; set; }
        public int LowStockCount { get; set; }
    }
}