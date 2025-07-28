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
        public int TotalItemsSold { get; set; }
        public decimal AverageTransactionValue { get; set; }
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
}