using Berca_Backend.DTOs;

namespace Berca_Backend.Services.Interfaces
{
    /// <summary>
    /// Export service interface for generating PDF, Excel, and CSV files
    /// Indonesian business context with comprehensive export options
    /// </summary>
    public interface IExportService
    {
        // ==================== PDF EXPORT ==================== //
        
        /// <summary>
        /// Export report data to PDF format
        /// </summary>
        Task<(bool Success, string? FilePath, string? ErrorMessage)> ExportToPdfAsync<T>(
            string reportTitle,
            T data,
            string? templateName = null,
            Dictionary<string, object>? options = null);
        
        /// <summary>
        /// Export sales report to PDF
        /// </summary>
        Task<(bool Success, string? FilePath, string? ErrorMessage)> ExportSalesReportToPdfAsync(
            DetailedSalesReportDto salesReport,
            ExportRequestDto request);
        
        /// <summary>
        /// Export inventory report to PDF
        /// </summary>
        Task<(bool Success, string? FilePath, string? ErrorMessage)> ExportInventoryReportToPdfAsync(
            DetailedInventoryReportDto inventoryReport,
            ExportRequestDto request);
        
        /// <summary>
        /// Export financial report to PDF
        /// </summary>
        Task<(bool Success, string? FilePath, string? ErrorMessage)> ExportFinancialReportToPdfAsync(
            DetailedFinancialReportDto financialReport,
            ExportRequestDto request);
        
        // ==================== EXCEL EXPORT ==================== //
        
        /// <summary>
        /// Export report data to Excel format
        /// </summary>
        Task<(bool Success, string? FilePath, string? ErrorMessage)> ExportToExcelAsync<T>(
            string reportTitle,
            T data,
            string? templateName = null,
            Dictionary<string, object>? options = null);
        
        /// <summary>
        /// Export sales report to Excel with charts
        /// </summary>
        Task<(bool Success, string? FilePath, string? ErrorMessage)> ExportSalesReportToExcelAsync(
            DetailedSalesReportDto salesReport,
            ExportRequestDto request);
        
        /// <summary>
        /// Export inventory report to Excel
        /// </summary>
        Task<(bool Success, string? FilePath, string? ErrorMessage)> ExportInventoryReportToExcelAsync(
            DetailedInventoryReportDto inventoryReport,
            ExportRequestDto request);
        
        /// <summary>
        /// Export product list to Excel
        /// </summary>
        Task<(bool Success, string? FilePath, string? ErrorMessage)> ExportProductListToExcelAsync(
            List<object> products,
            ExportRequestDto request);
        
        // ==================== CSV EXPORT ==================== //
        
        /// <summary>
        /// Export data to CSV format
        /// </summary>
        Task<(bool Success, string? FilePath, string? ErrorMessage)> ExportToCsvAsync<T>(
            IEnumerable<T> data,
            string fileName,
            Dictionary<string, string>? columnMappings = null);
        
        /// <summary>
        /// Export sales data to CSV
        /// </summary>
        Task<(bool Success, string? FilePath, string? ErrorMessage)> ExportSalesToCsvAsync(
            List<object> salesData,
            ExportRequestDto request);
        
        /// <summary>
        /// Export inventory data to CSV
        /// </summary>
        Task<(bool Success, string? FilePath, string? ErrorMessage)> ExportInventoryToCsvAsync(
            List<InventoryItemDto> inventoryItems,
            ExportRequestDto request);
        
        // ==================== UTILITY METHODS ==================== //
        
        /// <summary>
        /// Get export file information
        /// </summary>
        Task<object?> GetExportFileInfoAsync(string filePath);
        
        /// <summary>
        /// Delete export file
        /// </summary>
        Task<bool> DeleteExportFileAsync(string filePath);
        
        /// <summary>
        /// Clean up old export files
        /// </summary>
        Task<int> CleanupOldExportFilesAsync(int retentionDays = 7);
        
        /// <summary>
        /// Validate export request
        /// </summary>
        Task<(bool IsValid, string? ErrorMessage)> ValidateExportRequestAsync(ExportRequestDto request);
        
        /// <summary>
        /// Get available export templates
        /// </summary>
        Task<List<object>> GetAvailableTemplatesAsync(string exportFormat);
        
        /// <summary>
        /// Send export file via email
        /// </summary>
        Task<(bool Success, string? ErrorMessage)> SendExportViaEmailAsync(
            string filePath,
            string emailTo,
            string subject,
            string? body = null);
        
        // ==================== ADVANCED FEATURES ==================== //
        
        /// <summary>
        /// Create multi-sheet Excel export
        /// </summary>
        Task<(bool Success, string? FilePath, string? ErrorMessage)> CreateMultiSheetExcelAsync(
            Dictionary<string, object> sheetsData,
            string fileName,
            ExportRequestDto request);
        
        /// <summary>
        /// Export with custom formatting
        /// </summary>
        Task<(bool Success, string? FilePath, string? ErrorMessage)> ExportWithCustomFormattingAsync<T>(
            T data,
            string format,
            Dictionary<string, object> formatOptions,
            string fileName);
        
        /// <summary>
        /// Generate export with charts and visualizations
        /// </summary>
        Task<(bool Success, string? FilePath, string? ErrorMessage)> ExportWithChartsAsync(
            object data,
            List<object> chartDefinitions,
            string format,
            string fileName);
        
        /// <summary>
        /// Batch export multiple reports
        /// </summary>
        Task<(bool Success, List<string> FilePaths, string? ErrorMessage)> BatchExportAsync(
            List<ExportRequestDto> requests,
            string? zipFileName = null);
    }
}