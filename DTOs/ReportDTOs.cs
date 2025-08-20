using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Berca_Backend.Models;

namespace Berca_Backend.DTOs
{
    /// <summary>
    /// Complete set of DTOs for reporting system
    /// All properties use null-safe patterns with Indonesian localization
    /// </summary>

    // ==================== BASE REPORT DTOS ==================== //

    /// <summary>
    /// Base Report DTO with all required fields
    /// </summary>
    public class ReportDto
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string ReportType { get; set; }
        public string? Description { get; set; }
        public string? Parameters { get; set; }
        public bool IsActive { get; set; }
        public bool IsScheduled { get; set; }
        public string? ScheduleExpression { get; set; }
        
        // Branch information - null-safe
        public int? BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        
        // Audit information - null-safe
        public int CreatedBy { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty; // Formatted string
        public string UpdatedAt { get; set; } = string.Empty; // Formatted string
        
        // Display properties for Indonesian context
        public string ReportTypeDisplay => ReportType switch
        {
            "Sales" => "Penjualan",
            "Inventory" => "Inventori",
            "Financial" => "Keuangan",
            "Supplier" => "Pemasok",
            "Custom" => "Kustom",
            "Analytics" => "Analitik",
            _ => ReportType
        };
        
        public string StatusDisplay => IsActive ? "Aktif" : "Tidak Aktif";
        public string ScheduleDisplay => IsScheduled ? "Terjadwal" : "Manual";
        public string CreatedAtDisplay => CreatedAt;
    }

    /// <summary>
    /// Create Report DTO for form submissions
    /// </summary>
    public class CreateReportDto
    {
        [Required(ErrorMessage = "Nama laporan harus diisi")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Nama laporan harus antara 3-100 karakter")]
        public required string Name { get; set; }
        
        [Required(ErrorMessage = "Jenis laporan harus dipilih")]
        public required string ReportType { get; set; }
        
        [StringLength(500, ErrorMessage = "Deskripsi maksimal 500 karakter")]
        public string? Description { get; set; }
        
        [Required(ErrorMessage = "Parameter laporan harus diisi")]
        public required string Parameters { get; set; }
        
        public bool IsScheduled { get; set; } = false;
        public string? ScheduleExpression { get; set; }
        public int? BranchId { get; set; }
    }

    /// <summary>
    /// Update Report DTO
    /// </summary>
    public class UpdateReportDto
    {
        [Required(ErrorMessage = "Nama laporan harus diisi")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Nama laporan harus antara 3-100 karakter")]
        public required string Name { get; set; }
        
        [StringLength(500, ErrorMessage = "Deskripsi maksimal 500 karakter")]
        public string? Description { get; set; }
        
        public string? Parameters { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsScheduled { get; set; } = false;
        public string? ScheduleExpression { get; set; }
    }

    // ==================== SALES REPORT DTOS ==================== //

    /// <summary>
    /// Detailed Sales Report DTO with comprehensive sales analytics
    /// </summary>
    public class DetailedSalesReportDto
    {
        public string ReportPeriod { get; set; } = string.Empty;
        public decimal TotalSales { get; set; }
        public decimal TotalProfit { get; set; }
        public int TotalTransactions { get; set; }
        public decimal AverageTransactionValue { get; set; }
        public decimal TotalRefunds { get; set; }
        public decimal NetSales { get; set; }
        
        // Top performing items
        public List<SalesItemDto> TopSellingProducts { get; set; } = new();
        public List<CategorySalesPerformanceDto> CategoryPerformance { get; set; } = new();
        public List<UserPerformanceDto> StaffPerformance { get; set; } = new();
        
        // Time-based analysis
        public List<SalesByPeriodDto> SalesByDay { get; set; } = new();
        public List<SalesByPeriodDto> SalesByHour { get; set; } = new();
        public List<SalesByPeriodDto> SalesByMonth { get; set; } = new();
        
        // Display properties - Indonesian formatting
        private static readonly CultureInfo IdCulture = new("id-ID");
        
        public string TotalSalesDisplay => TotalSales.ToString("C", IdCulture);
        public string TotalProfitDisplay => TotalProfit.ToString("C", IdCulture);
        public string NetSalesDisplay => NetSales.ToString("C", IdCulture);
        public string AverageTransactionDisplay => AverageTransactionValue.ToString("C", IdCulture);
        public string TotalRefundsDisplay => TotalRefunds.ToString("C", IdCulture);
        public decimal ProfitMargin => TotalSales > 0 ? (TotalProfit / TotalSales) * 100 : 0;
        public string ProfitMarginDisplay => $"{ProfitMargin:F2}%";
    }

    /// <summary>
    /// Sales item performance DTO
    /// </summary>
    public class SalesItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductBarcode { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
        public decimal Profit { get; set; }
        public decimal AverageSellingPrice { get; set; }
        
        // Display properties
        public string RevenueDisplay => Revenue.ToString("C", new CultureInfo("id-ID"));
        public string ProfitDisplay => Profit.ToString("C", new CultureInfo("id-ID"));
        public string AverageSellingPriceDisplay => AverageSellingPrice.ToString("C", new CultureInfo("id-ID"));
    }

    /// <summary>
    /// Category sales performance DTO
    /// </summary>
    public class CategorySalesPerformanceDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryColor { get; set; } = "#000000";
        public int TotalProducts { get; set; }
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
        public decimal Profit { get; set; }
        public decimal MarketShare { get; set; }
        
        // Display properties
        public string RevenueDisplay => Revenue.ToString("C", new CultureInfo("id-ID"));
        public string ProfitDisplay => Profit.ToString("C", new CultureInfo("id-ID"));
        public string MarketShareDisplay => $"{MarketShare:F2}%";
    }

    /// <summary>
    /// User/Staff performance DTO
    /// </summary>
    public class UserPerformanceDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int TotalTransactions { get; set; }
        public decimal TotalSales { get; set; }
        public decimal AverageTransactionValue { get; set; }
        
        // Display properties
        public string TotalSalesDisplay => TotalSales.ToString("C", new CultureInfo("id-ID"));
        public string AverageTransactionDisplay => AverageTransactionValue.ToString("C", new CultureInfo("id-ID"));
    }

    /// <summary>
    /// Sales by period DTO
    /// </summary>
    public class SalesByPeriodDto
    {
        public string Period { get; set; } = string.Empty;
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public decimal Sales { get; set; }
        public int Transactions { get; set; }
        public decimal AverageTransactionValue { get; set; }
        
        // Display properties
        public string SalesDisplay => Sales.ToString("C", new CultureInfo("id-ID"));
        public string AverageTransactionDisplay => AverageTransactionValue.ToString("C", new CultureInfo("id-ID"));
    }

    // ==================== INVENTORY REPORT DTOS ==================== //

    /// <summary>
    /// Detailed Inventory Report DTO with comprehensive inventory analytics
    /// </summary>
    public class DetailedInventoryReportDto
    {
        public string ReportDate { get; set; } = string.Empty;
        public int TotalProducts { get; set; }
        public decimal TotalInventoryValue { get; set; }
        public decimal TotalCostValue { get; set; }
        public int LowStockItems { get; set; }
        public int ExpiringItems { get; set; }
        public int ExpiredItems { get; set; }
        public int OutOfStockItems { get; set; }
        
        // Detailed breakdowns
        public List<InventoryItemDto> InventoryItems { get; set; } = new();
        public List<CategoryInventoryBreakdownDto> CategoryBreakdown { get; set; } = new();
        public List<InventoryMovementTrackingDto> RecentMovements { get; set; } = new();
        public List<InventoryValuationDto> ValuationBreakdown { get; set; } = new();
        
        // Display properties
        public string TotalInventoryValueDisplay => TotalInventoryValue.ToString("C", new CultureInfo("id-ID"));
        public string TotalCostValueDisplay => TotalCostValue.ToString("C", new CultureInfo("id-ID"));
        public decimal PotentialProfit => TotalInventoryValue - TotalCostValue;
        public string PotentialProfitDisplay => PotentialProfit.ToString("C", new CultureInfo("id-ID"));
    }

    /// <summary>
    /// Individual inventory item DTO
    /// </summary>
    public class InventoryItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductBarcode { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public int MinimumStock { get; set; }
        public decimal CostPerUnit { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal TotalValue { get; set; }
        public string StockStatus { get; set; } = string.Empty;
        public DateTime? ExpiryDate { get; set; }
        
        // Display properties
        public string CostPerUnitDisplay => CostPerUnit.ToString("C", new CultureInfo("id-ID"));
        public string SellingPriceDisplay => SellingPrice.ToString("C", new CultureInfo("id-ID"));
        public string TotalValueDisplay => TotalValue.ToString("C", new CultureInfo("id-ID"));
        public string StockStatusDisplay => StockStatus switch
        {
            "LowStock" => "Stok Rendah",
            "OutOfStock" => "Habis",
            "Normal" => "Normal",
            "Overstock" => "Kelebihan Stok",
            _ => StockStatus
        };
    }

    /// <summary>
    /// Category inventory breakdown DTO
    /// </summary>
    public class CategoryInventoryBreakdownDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryColor { get; set; } = "#000000";
        public int ProductCount { get; set; }
        public int TotalStock { get; set; }
        public decimal TotalValue { get; set; }
        public int LowStockCount { get; set; }
        
        // Display properties
        public string TotalValueDisplay => TotalValue.ToString("C", new CultureInfo("id-ID"));
    }

    /// <summary>
    /// Inventory movement tracking DTO
    /// </summary>
    public class InventoryMovementTrackingDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string MovementType { get; set; } = string.Empty; // In, Out, Adjustment
        public int Quantity { get; set; }
        public DateTime MovementDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        
        // Display properties
        public string MovementTypeDisplay => MovementType switch
        {
            "In" => "Masuk",
            "Out" => "Keluar",
            "Adjustment" => "Penyesuaian",
            "Sale" => "Penjualan",
            "Return" => "Retur",
            _ => MovementType
        };
        public string MovementDateDisplay => MovementDate.ToString("dd/MM/yyyy HH:mm");
    }

    /// <summary>
    /// Inventory valuation breakdown DTO
    /// </summary>
    public class InventoryValuationDto
    {
        public string Category { get; set; } = string.Empty;
        public decimal CostValue { get; set; }
        public decimal RetailValue { get; set; }
        public decimal PotentialProfit { get; set; }
        public decimal ProfitMargin { get; set; }
        
        // Display properties
        public string CostValueDisplay => CostValue.ToString("C", new CultureInfo("id-ID"));
        public string RetailValueDisplay => RetailValue.ToString("C", new CultureInfo("id-ID"));
        public string PotentialProfitDisplay => PotentialProfit.ToString("C", new CultureInfo("id-ID"));
        public string ProfitMarginDisplay => $"{ProfitMargin:F2}%";
    }

    // ==================== SUPPLIER PERFORMANCE DTOS ==================== //

    /// <summary>
    /// Supplier Performance DTO with comprehensive KPI tracking
    /// </summary>
    public class SupplierPerformanceDto
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string SupplierCode { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        
        // Performance metrics
        public decimal TotalPurchaseAmount { get; set; }
        public int TotalOrders { get; set; }
        public int OnTimeDeliveries { get; set; }
        public int TotalDeliveries { get; set; }
        public decimal OnTimePercentage { get; set; }
        public decimal AverageOrderValue { get; set; }
        public int DaysAveragePayment { get; set; }
        
        // Quality metrics
        public int QualityComplaints { get; set; }
        public decimal DefectRate { get; set; }
        public int ReturnsCount { get; set; }
        public decimal ReturnRate { get; set; }
        
        // Financial metrics
        public decimal OutstandingAmount { get; set; }
        public int OverdueInvoices { get; set; }
        public decimal CreditLimit { get; set; }
        public decimal CreditUtilization { get; set; }
        
        // Display properties
        public string TotalPurchaseDisplay => TotalPurchaseAmount.ToString("C", new CultureInfo("id-ID"));
        public string AverageOrderValueDisplay => AverageOrderValue.ToString("C", new CultureInfo("id-ID"));
        public string OutstandingAmountDisplay => OutstandingAmount.ToString("C", new CultureInfo("id-ID"));
        public string OnTimePercentageDisplay => $"{OnTimePercentage:F1}%";
        public string DefectRateDisplay => $"{DefectRate:F2}%";
        public string ReturnRateDisplay => $"{ReturnRate:F2}%";
        public string CreditUtilizationDisplay => $"{CreditUtilization:F1}%";
        
        /// <summary>
        /// Calculate overall performance rating
        /// </summary>
        public string PerformanceRating
        {
            get
            {
                var score = CalculatePerformanceScore();
                return score switch
                {
                    >= 90 => "Sangat Baik",
                    >= 80 => "Baik",
                    >= 70 => "Cukup",
                    >= 60 => "Kurang",
                    _ => "Buruk"
                };
            }
        }
        
        /// <summary>
        /// Performance score color for UI display
        /// </summary>
        public string PerformanceColor
        {
            get
            {
                var score = CalculatePerformanceScore();
                return score switch
                {
                    >= 80 => "#28a745", // Green
                    >= 60 => "#ffc107", // Yellow
                    _ => "#dc3545" // Red
                };
            }
        }
        
        private decimal CalculatePerformanceScore()
        {
            var onTimeScore = OnTimePercentage * 0.3m;
            var qualityScore = (100 - DefectRate) * 0.3m;
            var reliabilityScore = TotalOrders > 0 ? (TotalOrders * 0.2m) : 0;
            var financialScore = CreditUtilization < 80 ? 20 : (100 - CreditUtilization) * 0.2m;
            
            return Math.Min(100, onTimeScore + qualityScore + reliabilityScore + financialScore);
        }
    }

    // ==================== FINANCIAL REPORT DTOS ==================== //

    /// <summary>
    /// Detailed Financial Report DTO with comprehensive financial analytics
    /// </summary>
    public class DetailedFinancialReportDto
    {
        public string ReportPeriod { get; set; } = string.Empty;
        
        // Revenue metrics
        public decimal GrossRevenue { get; set; }
        public decimal NetRevenue { get; set; }
        public decimal ReturnsAndRefunds { get; set; }
        public decimal Discounts { get; set; }
        
        // Cost metrics
        public decimal CostOfGoodsSold { get; set; }
        public decimal OperatingExpenses { get; set; }
        public decimal TotalExpenses { get; set; }
        
        // Profit metrics
        public decimal GrossProfit { get; set; }
        public decimal NetProfit { get; set; }
        public decimal ProfitMargin { get; set; }
        public decimal EBITDA { get; set; }
        
        // Cash Flow
        public decimal CashInflow { get; set; }
        public decimal CashOutflow { get; set; }
        public decimal NetCashFlow { get; set; }
        
        // Balance Sheet Items
        public decimal CurrentAssets { get; set; }
        public decimal CurrentLiabilities { get; set; }
        public decimal InventoryValue { get; set; }
        public decimal AccountsReceivable { get; set; }
        public decimal AccountsPayable { get; set; }
        
        // Financial Ratios
        public decimal CurrentRatio { get; set; }
        public decimal QuickRatio { get; set; }
        public decimal InventoryTurnover { get; set; }
        public decimal ROI { get; set; }
        
        // Display properties - Indonesian formatting
        private static readonly CultureInfo IdCulture = new("id-ID");
        
        public string GrossRevenueDisplay => GrossRevenue.ToString("C", IdCulture);
        public string NetRevenueDisplay => NetRevenue.ToString("C", IdCulture);
        public string GrossProfitDisplay => GrossProfit.ToString("C", IdCulture);
        public string NetProfitDisplay => NetProfit.ToString("C", IdCulture);
        public string NetCashFlowDisplay => NetCashFlow.ToString("C", IdCulture);
        public string ProfitMarginDisplay => $"{ProfitMargin:F2}%";
        public string ROIDisplay => $"{ROI:F2}%";
        public string CurrentRatioDisplay => $"{CurrentRatio:F2}";
        public string InventoryTurnoverDisplay => $"{InventoryTurnover:F2}x";
    }

    // ==================== EXPORT & FILTER DTOS ==================== //

    /// <summary>
    /// Export request DTO with comprehensive options
    /// </summary>
    public class ExportRequestDto
    {
        [Required(ErrorMessage = "Jenis laporan harus dipilih")]
        public required string ReportType { get; set; } // Sales, Inventory, Financial, Supplier
        
        [Required(ErrorMessage = "Format export harus dipilih")]
        public required string ExportFormat { get; set; } // PDF, Excel, CSV
        
        [Required(ErrorMessage = "Tanggal mulai harus diisi")]
        public required string DateFrom { get; set; }
        
        [Required(ErrorMessage = "Tanggal akhir harus diisi")] 
        public required string DateTo { get; set; }
        
        public int? BranchId { get; set; }
        public int? CategoryId { get; set; }
        public int? SupplierId { get; set; }
        public int? UserId { get; set; }
        
        // Export options
        public bool IncludeCharts { get; set; } = true;
        public bool IncludeDetails { get; set; } = true;
        public bool IncludeSummary { get; set; } = true;
        public string? EmailTo { get; set; }
        
        // Template options
        public string? TemplateName { get; set; }
        public string? ReportTitle { get; set; }
        
        // Filter options
        public Dictionary<string, object> AdditionalFilters { get; set; } = new();
    }

    /// <summary>
    /// Report filters DTO with comprehensive filtering options
    /// </summary>
    public class ReportFiltersDto
    {
        public DateTime StartDate { get; set; } = DateTime.Now.AddDays(-30);
        public DateTime EndDate { get; set; } = DateTime.Now;
        public int? BranchId { get; set; }
        public int? CategoryId { get; set; }
        public int? SupplierId { get; set; }
        public int? UserId { get; set; }
        public string? Status { get; set; }
        public string? ReportType { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsScheduled { get; set; }
        
        // Pagination
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        
        // Sorting
        public string? SortBy { get; set; } = "CreatedAt";
        public string? SortDirection { get; set; } = "desc";
        
        // Search
        public string? SearchTerm { get; set; }
        
        // Date range helpers
        public string StartDateDisplay => StartDate.ToString("dd/MM/yyyy");
        public string EndDateDisplay => EndDate.ToString("dd/MM/yyyy");
        
        /// <summary>
        /// Validate date range
        /// </summary>
        public bool IsValidDateRange => StartDate <= EndDate && 
                                       StartDate >= DateTime.Now.AddYears(-5) && 
                                       EndDate <= DateTime.Now.AddDays(1);
    }

    /// <summary>
    /// Report execution result DTO
    /// </summary>
    public class ReportExecutionResultDto
    {
        public int ExecutionId { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? OutputPath { get; set; }
        public long? FileSizeBytes { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public long? ExecutionDurationMs { get; set; }
        
        // Display properties
        public string StatusDisplay => Success ? "Berhasil" : "Gagal";
        public string ExecutionTimeDisplay => ExecutionDurationMs.HasValue 
            ? $"{ExecutionDurationMs.Value:N0} ms" 
            : "N/A";
        public string FileSizeDisplay => FileSizeBytes.HasValue
            ? FormatFileSize(FileSizeBytes.Value)
            : "N/A";
        
        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}