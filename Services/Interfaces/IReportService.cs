using Berca_Backend.DTOs;
using Berca_Backend.Models;

namespace Berca_Backend.Services.Interfaces
{
    /// <summary>
    /// Service interface for comprehensive reporting functionality
    /// Indonesian business context with error-safe implementations
    /// </summary>
    public interface IReportService
    {
        // ==================== REPORT MANAGEMENT ==================== //
        
        /// <summary>
        /// Get all reports with filtering and pagination
        /// </summary>
        Task<(List<ReportDto> Reports, int TotalCount)> GetReportsAsync(ReportFiltersDto filters);
        
        /// <summary>
        /// Get report by ID
        /// </summary>
        Task<ReportDto?> GetReportByIdAsync(int reportId);
        
        /// <summary>
        /// Create new report
        /// </summary>
        Task<(bool Success, int? ReportId, string? ErrorMessage)> CreateReportAsync(CreateReportDto dto, int createdBy);
        
        /// <summary>
        /// Update existing report
        /// </summary>
        Task<(bool Success, string? ErrorMessage)> UpdateReportAsync(int reportId, UpdateReportDto dto, int updatedBy);
        
        /// <summary>
        /// Delete report
        /// </summary>
        Task<(bool Success, string? ErrorMessage)> DeleteReportAsync(int reportId, int deletedBy);
        
        // ==================== REPORT EXECUTION ==================== //
        
        /// <summary>
        /// Execute report and return data
        /// </summary>
        Task<(bool Success, object? Data, string? ErrorMessage)> ExecuteReportAsync(int reportId, Dictionary<string, object>? parameters = null, int? executedBy = null);
        
        /// <summary>
        /// Execute report and export to file
        /// </summary>
        Task<ReportExecutionResultDto> ExecuteAndExportReportAsync(ExportRequestDto request, int executedBy);
        
        /// <summary>
        /// Get report execution history
        /// </summary>
        Task<List<ReportExecutionResultDto>> GetReportExecutionHistoryAsync(int reportId, int? limit = null);
        
        // ==================== SALES REPORTS ==================== //
        
        /// <summary>
        /// Generate comprehensive sales report
        /// </summary>
        Task<DetailedSalesReportDto> GenerateSalesReportAsync(DateTime startDate, DateTime endDate, int? branchId = null, int? categoryId = null, int? userId = null);
        
        /// <summary>
        /// Get sales performance by category
        /// </summary>
        Task<List<CategorySalesPerformanceDto>> GetSalesPerformanceByCategoryAsync(DateTime startDate, DateTime endDate, int? branchId = null);
        
        /// <summary>
        /// Get top selling products
        /// </summary>
        Task<List<SalesItemDto>> GetTopSellingProductsAsync(DateTime startDate, DateTime endDate, int limit = 10, int? branchId = null);
        
        /// <summary>
        /// Get staff performance report
        /// </summary>
        Task<List<UserPerformanceDto>> GetStaffPerformanceAsync(DateTime startDate, DateTime endDate, int? branchId = null);
        
        /// <summary>
        /// Get sales by time period
        /// </summary>
        Task<List<SalesByPeriodDto>> GetSalesByPeriodAsync(DateTime startDate, DateTime endDate, string periodType, int? branchId = null);
        
        // ==================== INVENTORY REPORTS ==================== //
        
        /// <summary>
        /// Generate comprehensive inventory report
        /// </summary>
        Task<DetailedInventoryReportDto> GenerateInventoryReportAsync(int? branchId = null, int? categoryId = null, string? status = null);
        
        /// <summary>
        /// Get low stock items
        /// </summary>
        Task<List<InventoryItemDto>> GetLowStockItemsAsync(int? branchId = null, int? categoryId = null);
        
        /// <summary>
        /// Get inventory movements
        /// </summary>
        Task<List<InventoryMovementTrackingDto>> GetInventoryMovementsAsync(DateTime startDate, DateTime endDate, int? productId = null, int? branchId = null);
        
        /// <summary>
        /// Get inventory valuation breakdown
        /// </summary>
        Task<List<InventoryValuationDto>> GetInventoryValuationAsync(int? branchId = null);
        
        /// <summary>
        /// Get category inventory breakdown
        /// </summary>
        Task<List<CategoryInventoryBreakdownDto>> GetCategoryInventoryBreakdownAsync(int? branchId = null);
        
        // ==================== FINANCIAL REPORTS ==================== //
        
        /// <summary>
        /// Generate comprehensive financial report
        /// </summary>
        Task<DetailedFinancialReportDto> GenerateFinancialReportAsync(DateTime startDate, DateTime endDate, int? branchId = null);
        
        /// <summary>
        /// Get profit and loss statement
        /// </summary>
        Task<DetailedFinancialReportDto> GetProfitLossStatementAsync(DateTime startDate, DateTime endDate, int? branchId = null);
        
        /// <summary>
        /// Get cash flow report
        /// </summary>
        Task<object> GetCashFlowReportAsync(DateTime startDate, DateTime endDate, int? branchId = null);
        
        // ==================== SUPPLIER REPORTS ==================== //
        
        /// <summary>
        /// Generate supplier performance report
        /// </summary>
        Task<List<SupplierPerformanceDto>> GenerateSupplierPerformanceReportAsync(DateTime startDate, DateTime endDate, int? supplierId = null);
        
        /// <summary>
        /// Get supplier payment analysis
        /// </summary>
        Task<object> GetSupplierPaymentAnalysisAsync(DateTime startDate, DateTime endDate, int? supplierId = null);
        
        // ==================== UTILITY METHODS ==================== //
        
        /// <summary>
        /// Validate report parameters
        /// </summary>
        Task<(bool IsValid, string? ErrorMessage)> ValidateReportParametersAsync(string reportType, Dictionary<string, object> parameters);
        
        /// <summary>
        /// Get available report types
        /// </summary>
        Task<List<object>> GetAvailableReportTypesAsync();
        
        /// <summary>
        /// Schedule report for automatic execution
        /// </summary>
        Task<(bool Success, string? ErrorMessage)> ScheduleReportAsync(int reportId, string cronExpression, int scheduledBy);
        
        /// <summary>
        /// Get report templates
        /// </summary>
        Task<List<object>> GetReportTemplatesAsync(string? category = null);
    }
}