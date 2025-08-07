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
        Task<List<TopProductDto>> GetTopSellingProductsAsync(int count = 10, DateTime? startDate = null, DateTime? endDate = null, string sortBy = "quantity"); // ✅ ADDED sortBy parameter
        Task<List<WorstProductDto>> GetWorstPerformingProductsAsync(int count = 10, DateTime? startDate = null, DateTime? endDate = null);
        Task<List<ProductDto>> GetLowStockAlertsAsync();
        Task<List<CategorySalesDto>> GetCategorySalesAsync(DateTime? startDate = null, DateTime? endDate = null);

        // Quick Stats
        Task<QuickStatsDto> GetQuickStatsAsync();
        Task<List<RecentTransactionDto>> GetRecentTransactionsAsync(int count = 10);

        // Reports
        Task<SalesReportDto> GenerateSalesReportAsync(DateTime startDate, DateTime endDate);
        Task<InventoryReportDto> GenerateInventoryReportAsync();
        Task<FinancialReportDto> GenerateFinancialReportAsync(DateTime startDate, DateTime endDate);
        Task<CustomerReportDto> GenerateCustomerReportAsync(DateTime startDate, DateTime endDate);

        // Export Features
        Task<ReportExportDto> ExportSalesReportAsync(DateTime startDate, DateTime endDate, string format);
        Task<ReportExportDto> ExportInventoryReportAsync(string format);
        Task<ReportExportDto> ExportFinancialReportAsync(DateTime startDate, DateTime endDate, string format);
        Task<ReportExportDto> ExportCustomerReportAsync(DateTime startDate, DateTime endDate, string format);

        // ✅ ADD: Date Range Resolution Method
        DateRangeFilter ResolveDateRange(string period, DateTime? customStart = null, DateTime? customEnd = null);
    }
}