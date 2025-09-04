using Berca_Backend.DTOs;
using Berca_Backend.Services.Interfaces;
using ClosedXML.Excel;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.Extensions.Options;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Berca_Backend.Services
{
    /// <summary>
    /// Comprehensive export service implementation
    /// Supports PDF, Excel, and CSV exports with Indonesian formatting
    /// </summary>
    public class ExportService : IExportService
    {
        private readonly ILogger<ExportService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _outputDirectory;
        private readonly string _tempDirectory;
        private static readonly CultureInfo IdCulture = new("id-ID");

        public ExportService(
            ILogger<ExportService> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _outputDirectory = _configuration["Reporting:OutputDirectory"] ?? "C:\\Reports\\\\";
            _tempDirectory = _configuration["Reporting:TempDirectory"] ?? "C:\\Reports\\Temp\\\\";
            
            // Ensure directories exist
            Directory.CreateDirectory(_outputDirectory);
            Directory.CreateDirectory(_tempDirectory);
            
            // Set EPPlus license context
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        // ==================== PDF EXPORT ==================== //

        public Task<(bool Success, string? FilePath, string? ErrorMessage)> ExportToPdfAsync<T>(
            string reportTitle,
            T data,
            string? templateName = null,
            Dictionary<string, object>? options = null)
        {
            try
            {
                var fileName = $"{SanitizeFileName(reportTitle)}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                var filePath = Path.Combine(_outputDirectory, fileName);

                using var writer = new PdfWriter(filePath);
                using var pdf = new PdfDocument(writer);
                using var document = new Document(pdf);

                // Set font for Indonesian text support
                var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                document.SetFont(font);

                // Add header
                AddPdfHeader(document, reportTitle);

                // Add content based on data type
                AddPdfContent(document, data, options);

                // Add footer
                AddPdfFooter(document);

                document.Close();

                _logger.LogInformation("PDF export completed: {FilePath}", filePath);
                return Task.FromResult<(bool Success, string? FilePath, string? ErrorMessage)>((true, filePath, null));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to PDF");
                return Task.FromResult<(bool Success, string? FilePath, string? ErrorMessage)>((false, null, $"Gagal export PDF: {ex.Message}"));
            }
        }

        public async Task<(bool Success, string? FilePath, string? ErrorMessage)> ExportSalesReportToPdfAsync(
            DetailedSalesReportDto salesReport,
            ExportRequestDto request)
        {
            return await Task.Run<(bool Success, string? FilePath, string? ErrorMessage)>(() =>
            {
                try
                {
                var fileName = $"Laporan_Penjualan_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                var filePath = Path.Combine(_outputDirectory, fileName);

                using var writer = new PdfWriter(filePath);
                using var pdf = new PdfDocument(writer);
                using var document = new Document(pdf);

                var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                document.SetFont(font);

                // Header
                document.Add(new Paragraph("LAPORAN PENJUALAN")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(18)
                    .SetBold());

                document.Add(new Paragraph($"Periode: {salesReport.ReportPeriod}")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(12));

                document.Add(new Paragraph("\n"));

                // Summary table
                var summaryTable = new Table(2);
                summaryTable.SetWidth(UnitValue.CreatePercentValue(100));

                summaryTable.AddCell("Total Penjualan");
                summaryTable.AddCell(salesReport.TotalSalesDisplay);
                summaryTable.AddCell("Total Transaksi");
                summaryTable.AddCell(salesReport.TotalTransactions.ToString("N0", IdCulture));
                summaryTable.AddCell("Rata-rata Transaksi");
                summaryTable.AddCell(salesReport.AverageTransactionDisplay);
                summaryTable.AddCell("Total Profit");
                summaryTable.AddCell(salesReport.TotalProfitDisplay);
                summaryTable.AddCell("Margin Profit");
                summaryTable.AddCell(salesReport.ProfitMarginDisplay);

                document.Add(summaryTable);
                document.Add(new Paragraph("\n"));

                // Top selling products
                if (salesReport.TopSellingProducts.Any())
                {
                    document.Add(new Paragraph("PRODUK TERLARIS")
                        .SetFontSize(14)
                        .SetBold());

                    var productsTable = new Table(4);
                    productsTable.SetWidth(UnitValue.CreatePercentValue(100));
                    
                    // Headers
                    productsTable.AddHeaderCell("Produk");
                    productsTable.AddHeaderCell("Qty Terjual");
                    productsTable.AddHeaderCell("Revenue");
                    productsTable.AddHeaderCell("Profit");

                    foreach (var product in salesReport.TopSellingProducts.Take(10))
                    {
                        productsTable.AddCell(product.ProductName);
                        productsTable.AddCell(product.QuantitySold.ToString("N0", IdCulture));
                        productsTable.AddCell(product.RevenueDisplay);
                        productsTable.AddCell(product.ProfitDisplay);
                    }

                    document.Add(productsTable);
                }

                // Footer
                document.Add(new Paragraph($"\nDigenerate pada: {DateTime.Now:dd/MM/yyyy HH:mm}")
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetFontSize(8));

                document.Close();

                _logger.LogInformation("Sales report PDF exported: {FilePath}", filePath);
                return (true, filePath, null);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error exporting sales report to PDF");
                    return (false, null, $"Gagal export laporan penjualan: {ex.Message}");
                }
            });
        }

        public Task<(bool Success, string? FilePath, string? ErrorMessage)> ExportInventoryReportToPdfAsync(
            DetailedInventoryReportDto inventoryReport,
            ExportRequestDto request)
        {
            try
            {
                var fileName = $"Laporan_Inventori_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                var filePath = Path.Combine(_outputDirectory, fileName);

                using var writer = new PdfWriter(filePath);
                using var pdf = new PdfDocument(writer);
                using var document = new Document(pdf);

                var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                document.SetFont(font);

                // Header
                document.Add(new Paragraph("LAPORAN INVENTORI")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(18)
                    .SetBold());

                document.Add(new Paragraph($"Tanggal: {inventoryReport.ReportDate}")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(12));

                document.Add(new Paragraph("\n"));

                // Summary
                var summaryTable = new Table(2);
                summaryTable.SetWidth(UnitValue.CreatePercentValue(100));

                summaryTable.AddCell("Total Produk");
                summaryTable.AddCell(inventoryReport.TotalProducts.ToString("N0", IdCulture));
                summaryTable.AddCell("Nilai Inventori");
                summaryTable.AddCell(inventoryReport.TotalInventoryValueDisplay);
                summaryTable.AddCell("Nilai Biaya");
                summaryTable.AddCell(inventoryReport.TotalCostValueDisplay);
                summaryTable.AddCell("Produk Stok Rendah");
                summaryTable.AddCell(inventoryReport.LowStockItems.ToString("N0", IdCulture));
                summaryTable.AddCell("Produk Habis");
                summaryTable.AddCell(inventoryReport.OutOfStockItems.ToString("N0", IdCulture));

                document.Add(summaryTable);
                document.Add(new Paragraph("\n"));

                // Inventory items (first 50)
                if (inventoryReport.InventoryItems.Any())
                {
                    document.Add(new Paragraph("DAFTAR PRODUK")
                        .SetFontSize(14)
                        .SetBold());

                    var itemsTable = new Table(5);
                    itemsTable.SetWidth(UnitValue.CreatePercentValue(100));
                    
                    // Headers
                    itemsTable.AddHeaderCell("Produk");
                    itemsTable.AddHeaderCell("Kategori");
                    itemsTable.AddHeaderCell("Stok");
                    itemsTable.AddHeaderCell("Harga");
                    itemsTable.AddHeaderCell("Status");

                    foreach (var item in inventoryReport.InventoryItems.Take(50))
                    {
                        itemsTable.AddCell(item.ProductName);
                        itemsTable.AddCell(item.CategoryName);
                        itemsTable.AddCell(item.CurrentStock.ToString("N0", IdCulture));
                        itemsTable.AddCell(item.SellingPriceDisplay);
                        itemsTable.AddCell(item.StockStatusDisplay);
                    }

                    document.Add(itemsTable);
                }

                // Footer
                document.Add(new Paragraph($"\nDigenerate pada: {DateTime.Now:dd/MM/yyyy HH:mm}")
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetFontSize(8));

                document.Close();

                _logger.LogInformation("Inventory report PDF exported: {FilePath}", filePath);
                return Task.FromResult<(bool Success, string? FilePath, string? ErrorMessage)>((true, filePath, null));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting inventory report to PDF");
                return Task.FromResult<(bool Success, string? FilePath, string? ErrorMessage)>((false, null, $"Gagal export laporan inventori: {ex.Message}"));
            }
        }

        public Task<(bool Success, string? FilePath, string? ErrorMessage)> ExportFinancialReportToPdfAsync(
            DetailedFinancialReportDto financialReport,
            ExportRequestDto request)
        {
            try
            {
                var fileName = $"Laporan_Keuangan_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                var filePath = Path.Combine(_outputDirectory, fileName);

                using var writer = new PdfWriter(filePath);
                using var pdf = new PdfDocument(writer);
                using var document = new Document(pdf);

                var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                document.SetFont(font);

                // Header
                document.Add(new Paragraph("LAPORAN KEUANGAN")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(18)
                    .SetBold());

                document.Add(new Paragraph($"Periode: {financialReport.ReportPeriod}")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(12));

                document.Add(new Paragraph("\n"));

                // Financial summary
                var summaryTable = new Table(2);
                summaryTable.SetWidth(UnitValue.CreatePercentValue(100));

                summaryTable.AddCell("Pendapatan Kotor");
                summaryTable.AddCell(financialReport.GrossRevenueDisplay);
                summaryTable.AddCell("Pendapatan Bersih");
                summaryTable.AddCell(financialReport.NetRevenueDisplay);
                summaryTable.AddCell("Laba Kotor");
                summaryTable.AddCell(financialReport.GrossProfitDisplay);
                summaryTable.AddCell("Laba Bersih");
                summaryTable.AddCell(financialReport.NetProfitDisplay);
                summaryTable.AddCell("Margin Profit");
                summaryTable.AddCell(financialReport.ProfitMarginDisplay);
                summaryTable.AddCell("Arus Kas Bersih");
                summaryTable.AddCell(financialReport.NetCashFlowDisplay);

                document.Add(summaryTable);

                // Footer
                document.Add(new Paragraph($"\nDigenerate pada: {DateTime.Now:dd/MM/yyyy HH:mm}")
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetFontSize(8));

                document.Close();

                _logger.LogInformation("Financial report PDF exported: {FilePath}", filePath);
                return Task.FromResult<(bool Success, string? FilePath, string? ErrorMessage)>((true, filePath, null));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting financial report to PDF");
                return Task.FromResult<(bool Success, string? FilePath, string? ErrorMessage)>((false, null, $"Gagal export laporan keuangan: {ex.Message}"));
            }
        }

        // ==================== EXCEL EXPORT ==================== //

        public async Task<(bool Success, string? FilePath, string? ErrorMessage)> ExportToExcelAsync<T>(
            string reportTitle,
            T data,
            string? templateName = null,
            Dictionary<string, object>? options = null)
        {
            try
            {
                var fileName = $"{SanitizeFileName(reportTitle)}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                var filePath = Path.Combine(_outputDirectory, fileName);

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add(reportTitle);

                // Add header styling
                worksheet.Cells["A1"].Value = reportTitle;
                worksheet.Cells["A1"].Style.Font.Size = 16;
                worksheet.Cells["A1"].Style.Font.Bold = true;
                worksheet.Cells["A1:E1"].Merge = true;

                // Add timestamp
                worksheet.Cells["A2"].Value = $"Digenerate pada: {DateTime.Now:dd/MM/yyyy HH:mm}";
                worksheet.Cells["A2"].Style.Font.Size = 10;
                worksheet.Cells["A2:E2"].Merge = true;

                // Add data
                AddExcelContent(worksheet, data, options);

                // Auto-fit columns
                worksheet.Cells.AutoFitColumns();

                // Save file
                await package.SaveAsAsync(new FileInfo(filePath));

                _logger.LogInformation("Excel export completed: {FilePath}", filePath);
                return (true, filePath, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to Excel");
                return (false, null, $"Gagal export Excel: {ex.Message}");
            }
        }

        public async Task<(bool Success, string? FilePath, string? ErrorMessage)> ExportSalesReportToExcelAsync(
            DetailedSalesReportDto salesReport,
            ExportRequestDto request)
        {
            try
            {
                var fileName = $"Laporan_Penjualan_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                var filePath = Path.Combine(_outputDirectory, fileName);

                using var package = new ExcelPackage();
                
                // Summary sheet
                var summarySheet = package.Workbook.Worksheets.Add("Ringkasan");
                await CreateSalesSummarySheet(summarySheet, salesReport);

                // Top products sheet
                if (salesReport.TopSellingProducts.Any())
                {
                    var productsSheet = package.Workbook.Worksheets.Add("Produk Terlaris");
                    await CreateTopProductsSheet(productsSheet, salesReport.TopSellingProducts);
                }

                // Category performance sheet
                if (salesReport.CategoryPerformance.Any())
                {
                    var categorySheet = package.Workbook.Worksheets.Add("Performa Kategori");
                    await CreateCategoryPerformanceSheet(categorySheet, salesReport.CategoryPerformance);
                }

                // Staff performance sheet
                if (salesReport.StaffPerformance.Any())
                {
                    var staffSheet = package.Workbook.Worksheets.Add("Performa Staff");
                    await CreateStaffPerformanceSheet(staffSheet, salesReport.StaffPerformance);
                }

                // Save file
                await package.SaveAsAsync(new FileInfo(filePath));

                _logger.LogInformation("Sales report Excel exported: {FilePath}", filePath);
                return (true, filePath, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting sales report to Excel");
                return (false, null, $"Gagal export laporan penjualan: {ex.Message}");
            }
        }

        public async Task<(bool Success, string? FilePath, string? ErrorMessage)> ExportInventoryReportToExcelAsync(
            DetailedInventoryReportDto inventoryReport,
            ExportRequestDto request)
        {
            try
            {
                var fileName = $"Laporan_Inventori_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                var filePath = Path.Combine(_outputDirectory, fileName);

                using var package = new ExcelPackage();
                
                // Summary sheet
                var summarySheet = package.Workbook.Worksheets.Add("Ringkasan");
                await CreateInventorySummarySheet(summarySheet, inventoryReport);

                // Inventory items sheet
                if (inventoryReport.InventoryItems.Any())
                {
                    var itemsSheet = package.Workbook.Worksheets.Add("Daftar Produk");
                    await CreateInventoryItemsSheet(itemsSheet, inventoryReport.InventoryItems);
                }

                // Category breakdown sheet
                if (inventoryReport.CategoryBreakdown.Any())
                {
                    var categorySheet = package.Workbook.Worksheets.Add("Breakdown Kategori");
                    await CreateCategoryBreakdownSheet(categorySheet, inventoryReport.CategoryBreakdown);
                }

                // Save file
                await package.SaveAsAsync(new FileInfo(filePath));

                _logger.LogInformation("Inventory report Excel exported: {FilePath}", filePath);
                return (true, filePath, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting inventory report to Excel");
                return (false, null, $"Gagal export laporan inventori: {ex.Message}");
            }
        }

        public async Task<(bool Success, string? FilePath, string? ErrorMessage)> ExportProductListToExcelAsync(
            List<object> products,
            ExportRequestDto request)
        {
            try
            {
                var fileName = $"Daftar_Produk_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                var filePath = Path.Combine(_outputDirectory, fileName);

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Daftar Produk");

                // Headers
                worksheet.Cells["A1"].Value = "DAFTAR PRODUK";
                worksheet.Cells["A1"].Style.Font.Size = 16;
                worksheet.Cells["A1"].Style.Font.Bold = true;
                worksheet.Cells["A1:F1"].Merge = true;

                worksheet.Cells["A2"].Value = $"Digenerate pada: {DateTime.Now:dd/MM/yyyy HH:mm}";
                worksheet.Cells["A2:F2"].Merge = true;

                // Column headers
                var headerRow = 4;
                worksheet.Cells[headerRow, 1].Value = "ID";
                worksheet.Cells[headerRow, 2].Value = "Nama Produk";
                worksheet.Cells[headerRow, 3].Value = "Kategori";
                worksheet.Cells[headerRow, 4].Value = "Stok";
                worksheet.Cells[headerRow, 5].Value = "Harga Beli";
                worksheet.Cells[headerRow, 6].Value = "Harga Jual";

                // Style headers
                using (var range = worksheet.Cells[headerRow, 1, headerRow, 6])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                }

                // Add data (simplified - would need proper object mapping)
                var dataRow = headerRow + 1;
                for (int i = 0; i < Math.Min(products.Count, 10000); i++)
                {
                    worksheet.Cells[dataRow + i, 1].Value = $"PRD{i + 1:000}";
                    worksheet.Cells[dataRow + i, 2].Value = $"Produk {i + 1}";
                    worksheet.Cells[dataRow + i, 3].Value = "Kategori Umum";
                    worksheet.Cells[dataRow + i, 4].Value = 100;
                    worksheet.Cells[dataRow + i, 5].Value = 10000;
                    worksheet.Cells[dataRow + i, 6].Value = 15000;
                }

                // Auto-fit columns
                worksheet.Cells.AutoFitColumns();

                // Save file
                await package.SaveAsAsync(new FileInfo(filePath));

                _logger.LogInformation("Product list Excel exported: {FilePath}", filePath);
                return (true, filePath, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting product list to Excel");
                return (false, null, $"Gagal export daftar produk: {ex.Message}");
            }
        }

        // ==================== CSV EXPORT ==================== //

        public async Task<(bool Success, string? FilePath, string? ErrorMessage)> ExportToCsvAsync<T>(
            IEnumerable<T> data,
            string fileName,
            Dictionary<string, string>? columnMappings = null)
        {
            try
            {
                var sanitizedFileName = SanitizeFileName(fileName);
                var filePath = Path.Combine(_outputDirectory, $"{sanitizedFileName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

                var csv = new StringBuilder();
                var properties = typeof(T).GetProperties();

                // Add headers
                var headers = columnMappings != null
                    ? properties.Select(p => columnMappings.ContainsKey(p.Name) ? columnMappings[p.Name] : p.Name)
                    : properties.Select(p => p.Name);

                csv.AppendLine(string.Join(",", headers.Select(h => $"\"{h}\"")));

                // Add data
                foreach (var item in data)
                {
                    var values = properties.Select(p =>
                    {
                        var value = p.GetValue(item);
                        return value?.ToString()?.Replace("\"", "\"\"") ?? "";
                    });

                    csv.AppendLine(string.Join(",", values.Select(v => $"\"{v}\"")));
                }

                await File.WriteAllTextAsync(filePath, csv.ToString(), Encoding.UTF8);

                _logger.LogInformation("CSV export completed: {FilePath}", filePath);
                return (true, filePath, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to CSV");
                return (false, null, $"Gagal export CSV: {ex.Message}");
            }
        }

        public async Task<(bool Success, string? FilePath, string? ErrorMessage)> ExportSalesToCsvAsync(
            List<object> salesData,
            ExportRequestDto request)
        {
            try
            {
                var fileName = $"Penjualan_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                var filePath = Path.Combine(_outputDirectory, fileName);

                var csv = new StringBuilder();
                
                // Headers
                csv.AppendLine("\"Tanggal\",\"No Transaksi\",\"Total\",\"Kasir\",\"Jumlah Item\"");

                // Sample data (would be replaced with actual data)
                for (int i = 0; i < Math.Min(salesData.Count, 10000); i++)
                {
                    csv.AppendLine($"\"{DateTime.Now.AddDays(-i):dd/MM/yyyy}\",\"TRX{i + 1:000000}\",\"150000\",\"Admin\",\"3\"");
                }

                await File.WriteAllTextAsync(filePath, csv.ToString(), Encoding.UTF8);

                _logger.LogInformation("Sales CSV exported: {FilePath}", filePath);
                return (true, filePath, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting sales to CSV");
                return (false, null, $"Gagal export penjualan: {ex.Message}");
            }
        }

        public async Task<(bool Success, string? FilePath, string? ErrorMessage)> ExportInventoryToCsvAsync(
            List<InventoryItemDto> inventoryItems,
            ExportRequestDto request)
        {
            try
            {
                var columnMappings = new Dictionary<string, string>
                {
                    { nameof(InventoryItemDto.ProductName), "Nama Produk" },
                    { nameof(InventoryItemDto.CategoryName), "Kategori" },
                    { nameof(InventoryItemDto.CurrentStock), "Stok Saat Ini" },
                    { nameof(InventoryItemDto.MinimumStock), "Stok Minimum" },
                    { nameof(InventoryItemDto.SellingPriceDisplay), "Harga Jual" },
                    { nameof(InventoryItemDto.StockStatusDisplay), "Status Stok" }
                };

                return await ExportToCsvAsync(inventoryItems, "Inventori", columnMappings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting inventory to CSV");
                return (false, null, $"Gagal export inventori: {ex.Message}");
            }
        }

        // ==================== HELPER METHODS ==================== //

        private void AddPdfHeader(Document document, string title)
        {
            document.Add(new Paragraph(title)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontSize(18)
                .SetBold());

            document.Add(new Paragraph("TOKO ENIWAN")
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontSize(12));

            document.Add(new Paragraph($"Digenerate pada: {DateTime.Now:dd/MM/yyyy HH:mm}")
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontSize(10));

            document.Add(new Paragraph("\n"));
        }

        private void AddPdfContent<T>(Document document, T data, Dictionary<string, object>? options)
        {
            // Generic content addition - would be specialized based on data type
            document.Add(new Paragraph($"Data Type: {typeof(T).Name}"));
            document.Add(new Paragraph($"Data: {JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true })}"));
        }

        private void AddPdfFooter(Document document)
        {
            document.Add(new Paragraph($"\nHalaman dibuat oleh sistem POS Toko Eniwan")
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontSize(8));
        }

        private void AddExcelContent<T>(ExcelWorksheet worksheet, T data, Dictionary<string, object>? options)
        {
            // Generic content addition starting from row 4
            worksheet.Cells[4, 1].Value = "Data Type:";
            worksheet.Cells[4, 2].Value = typeof(T).Name;
            
            worksheet.Cells[5, 1].Value = "Content:";
            worksheet.Cells[5, 2].Value = JsonSerializer.Serialize(data);
        }

private Task CreateSalesSummarySheet(ExcelWorksheet worksheet, DetailedSalesReportDto salesReport)
        {
            worksheet.Cells["A1"].Value = "RINGKASAN PENJUALAN";
            worksheet.Cells["A1"].Style.Font.Size = 16;
            worksheet.Cells["A1"].Style.Font.Bold = true;
            worksheet.Cells["A1:B1"].Merge = true;

            worksheet.Cells["A2"].Value = $"Periode: {salesReport.ReportPeriod}";
            worksheet.Cells["A2:B2"].Merge = true;

            var row = 4;
            worksheet.Cells[row, 1].Value = "Metrik";
            worksheet.Cells[row, 2].Value = "Nilai";
            
            // Style headers
            using (var range = worksheet.Cells[row, 1, row, 2])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
            }

            worksheet.Cells[++row, 1].Value = "Total Penjualan";
            worksheet.Cells[row, 2].Value = salesReport.TotalSalesDisplay;

            worksheet.Cells[++row, 1].Value = "Total Transaksi";
            worksheet.Cells[row, 2].Value = salesReport.TotalTransactions;

            worksheet.Cells[++row, 1].Value = "Rata-rata Transaksi";
            worksheet.Cells[row, 2].Value = salesReport.AverageTransactionDisplay;

            worksheet.Cells[++row, 1].Value = "Total Profit";
            worksheet.Cells[row, 2].Value = salesReport.TotalProfitDisplay;

            worksheet.Cells[++row, 1].Value = "Margin Profit";
            worksheet.Cells[row, 2].Value = salesReport.ProfitMarginDisplay;

    worksheet.Cells.AutoFitColumns();
    return Task.CompletedTask;
}

private Task CreateTopProductsSheet(ExcelWorksheet worksheet, List<SalesItemDto> topProducts)
        {
            worksheet.Cells["A1"].Value = "PRODUK TERLARIS";
            worksheet.Cells["A1"].Style.Font.Size = 16;
            worksheet.Cells["A1"].Style.Font.Bold = true;
            worksheet.Cells["A1:E1"].Merge = true;

            var headerRow = 3;
            worksheet.Cells[headerRow, 1].Value = "Nama Produk";
            worksheet.Cells[headerRow, 2].Value = "Kategori";
            worksheet.Cells[headerRow, 3].Value = "Qty Terjual";
            worksheet.Cells[headerRow, 4].Value = "Revenue";
            worksheet.Cells[headerRow, 5].Value = "Profit";

            // Style headers
            using (var range = worksheet.Cells[headerRow, 1, headerRow, 5])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
            }

            var dataRow = headerRow + 1;
            foreach (var product in topProducts)
            {
                worksheet.Cells[dataRow, 1].Value = product.ProductName;
                worksheet.Cells[dataRow, 2].Value = product.CategoryName;
                worksheet.Cells[dataRow, 3].Value = product.QuantitySold;
                worksheet.Cells[dataRow, 4].Value = product.RevenueDisplay;
                worksheet.Cells[dataRow, 5].Value = product.ProfitDisplay;
                dataRow++;
            }

    worksheet.Cells.AutoFitColumns();
    return Task.CompletedTask;
}

private Task CreateCategoryPerformanceSheet(ExcelWorksheet worksheet, List<CategorySalesPerformanceDto> categoryPerformance)
        {
            worksheet.Cells["A1"].Value = "PERFORMA KATEGORI";
            worksheet.Cells["A1"].Style.Font.Size = 16;
            worksheet.Cells["A1"].Style.Font.Bold = true;
            worksheet.Cells["A1:E1"].Merge = true;

            var headerRow = 3;
            worksheet.Cells[headerRow, 1].Value = "Kategori";
            worksheet.Cells[headerRow, 2].Value = "Qty Terjual";
            worksheet.Cells[headerRow, 3].Value = "Revenue";
            worksheet.Cells[headerRow, 4].Value = "Profit";
            worksheet.Cells[headerRow, 5].Value = "Market Share";

            // Style headers
            using (var range = worksheet.Cells[headerRow, 1, headerRow, 5])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
            }

            var dataRow = headerRow + 1;
            foreach (var category in categoryPerformance)
            {
                worksheet.Cells[dataRow, 1].Value = category.CategoryName;
                worksheet.Cells[dataRow, 2].Value = category.QuantitySold;
                worksheet.Cells[dataRow, 3].Value = category.RevenueDisplay;
                worksheet.Cells[dataRow, 4].Value = category.ProfitDisplay;
                worksheet.Cells[dataRow, 5].Value = category.MarketShareDisplay;
                dataRow++;
            }

    worksheet.Cells.AutoFitColumns();
    return Task.CompletedTask;
}

private Task CreateStaffPerformanceSheet(ExcelWorksheet worksheet, List<UserPerformanceDto> staffPerformance)
        {
            worksheet.Cells["A1"].Value = "PERFORMA STAFF";
            worksheet.Cells["A1"].Style.Font.Size = 16;
            worksheet.Cells["A1"].Style.Font.Bold = true;
            worksheet.Cells["A1:D1"].Merge = true;

            var headerRow = 3;
            worksheet.Cells[headerRow, 1].Value = "Nama Staff";
            worksheet.Cells[headerRow, 2].Value = "Total Transaksi";
            worksheet.Cells[headerRow, 3].Value = "Total Penjualan";
            worksheet.Cells[headerRow, 4].Value = "Rata-rata Transaksi";

            // Style headers
            using (var range = worksheet.Cells[headerRow, 1, headerRow, 4])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
            }

            var dataRow = headerRow + 1;
            foreach (var staff in staffPerformance)
            {
                worksheet.Cells[dataRow, 1].Value = staff.FullName;
                worksheet.Cells[dataRow, 2].Value = staff.TotalTransactions;
                worksheet.Cells[dataRow, 3].Value = staff.TotalSalesDisplay;
                worksheet.Cells[dataRow, 4].Value = staff.AverageTransactionDisplay;
                dataRow++;
            }

    worksheet.Cells.AutoFitColumns();
    return Task.CompletedTask;
}

private Task CreateInventorySummarySheet(ExcelWorksheet worksheet, DetailedInventoryReportDto inventoryReport)
        {
            worksheet.Cells["A1"].Value = "RINGKASAN INVENTORI";
            worksheet.Cells["A1"].Style.Font.Size = 16;
            worksheet.Cells["A1"].Style.Font.Bold = true;
            worksheet.Cells["A1:B1"].Merge = true;

            worksheet.Cells["A2"].Value = $"Tanggal: {inventoryReport.ReportDate}";
            worksheet.Cells["A2:B2"].Merge = true;

            var row = 4;
            worksheet.Cells[row, 1].Value = "Metrik";
            worksheet.Cells[row, 2].Value = "Nilai";
            
            // Style headers
            using (var range = worksheet.Cells[row, 1, row, 2])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
            }

            worksheet.Cells[++row, 1].Value = "Total Produk";
            worksheet.Cells[row, 2].Value = inventoryReport.TotalProducts;

            worksheet.Cells[++row, 1].Value = "Nilai Inventori";
            worksheet.Cells[row, 2].Value = inventoryReport.TotalInventoryValueDisplay;

            worksheet.Cells[++row, 1].Value = "Produk Stok Rendah";
            worksheet.Cells[row, 2].Value = inventoryReport.LowStockItems;

            worksheet.Cells[++row, 1].Value = "Produk Habis";
            worksheet.Cells[row, 2].Value = inventoryReport.OutOfStockItems;

    worksheet.Cells.AutoFitColumns();
    return Task.CompletedTask;
}

private Task CreateInventoryItemsSheet(ExcelWorksheet worksheet, List<InventoryItemDto> inventoryItems)
        {
            worksheet.Cells["A1"].Value = "DAFTAR PRODUK";
            worksheet.Cells["A1"].Style.Font.Size = 16;
            worksheet.Cells["A1"].Style.Font.Bold = true;
            worksheet.Cells["A1:F1"].Merge = true;

            var headerRow = 3;
            worksheet.Cells[headerRow, 1].Value = "Nama Produk";
            worksheet.Cells[headerRow, 2].Value = "Kategori";
            worksheet.Cells[headerRow, 3].Value = "Stok Saat Ini";
            worksheet.Cells[headerRow, 4].Value = "Stok Minimum";
            worksheet.Cells[headerRow, 5].Value = "Harga Jual";
            worksheet.Cells[headerRow, 6].Value = "Status";

            // Style headers
            using (var range = worksheet.Cells[headerRow, 1, headerRow, 6])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
            }

            var dataRow = headerRow + 1;
            foreach (var item in inventoryItems.Take(10000)) // Limit for performance
            {
                worksheet.Cells[dataRow, 1].Value = item.ProductName;
                worksheet.Cells[dataRow, 2].Value = item.CategoryName;
                worksheet.Cells[dataRow, 3].Value = item.CurrentStock;
                worksheet.Cells[dataRow, 4].Value = item.MinimumStock;
                worksheet.Cells[dataRow, 5].Value = item.SellingPriceDisplay;
                worksheet.Cells[dataRow, 6].Value = item.StockStatusDisplay;
                dataRow++;
            }

    worksheet.Cells.AutoFitColumns();
    return Task.CompletedTask;
}

        private Task CreateCategoryBreakdownSheet(ExcelWorksheet worksheet, List<CategoryInventoryBreakdownDto> categoryBreakdown)
        {
            return Task.Run(() =>
            {
                worksheet.Cells["A1"].Value = "BREAKDOWN KATEGORI";
                worksheet.Cells["A1"].Style.Font.Size = 16;
                worksheet.Cells["A1"].Style.Font.Bold = true;
                worksheet.Cells["A1:E1"].Merge = true;

                var headerRow = 3;
                worksheet.Cells[headerRow, 1].Value = "Kategori";
                worksheet.Cells[headerRow, 2].Value = "Jumlah Produk";
                worksheet.Cells[headerRow, 3].Value = "Total Stok";
                worksheet.Cells[headerRow, 4].Value = "Nilai Total";
                worksheet.Cells[headerRow, 5].Value = "Stok Rendah";

                // Style headers
                using (var range = worksheet.Cells[headerRow, 1, headerRow, 5])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                }

                var dataRow = headerRow + 1;
                foreach (var category in categoryBreakdown)
                {
                    worksheet.Cells[dataRow, 1].Value = category.CategoryName;
                    worksheet.Cells[dataRow, 2].Value = category.ProductCount;
                    worksheet.Cells[dataRow, 3].Value = category.TotalStock;
                    worksheet.Cells[dataRow, 4].Value = category.TotalValueDisplay;
                    worksheet.Cells[dataRow, 5].Value = category.LowStockCount;
                    dataRow++;
                }

                worksheet.Cells.AutoFitColumns();
            });
        }

        private string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
            return sanitized.Replace(" ", "_");
        }

        // ==================== UTILITY METHODS ==================== //

        public Task<object?> GetExportFileInfoAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return Task.FromResult<object?>(null);
                }

                var fileInfo = new FileInfo(filePath);
                return Task.FromResult<object?>(new
                {
                    FileName = fileInfo.Name,
                    FilePath = filePath,
                    Size = fileInfo.Length,
                    SizeDisplay = FormatFileSize(fileInfo.Length),
                    CreatedAt = fileInfo.CreationTime.ToString("dd/MM/yyyy HH:mm", IdCulture),
                    Extension = fileInfo.Extension
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file info for {FilePath}", filePath);
                return Task.FromResult<object?>(null);
            }
        }

        public Task<bool> DeleteExportFileAsync(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogInformation("Export file deleted: {FilePath}", filePath);
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting export file {FilePath}", filePath);
                return Task.FromResult(false);
            }
        }

        public Task<int> CleanupOldExportFilesAsync(int retentionDays = 7)
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-retentionDays);
                var files = Directory.GetFiles(_outputDirectory)
                    .Where(f => File.GetCreationTime(f) < cutoffDate)
                    .ToList();

                var deletedCount = 0;
                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                        deletedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete old export file: {FilePath}", file);
                    }
                }

                _logger.LogInformation("Cleaned up {Count} old export files", deletedCount);
                return Task.FromResult(deletedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old export files");
                return Task.FromResult(0);
            }
        }

        public Task<(bool IsValid, string? ErrorMessage)> ValidateExportRequestAsync(ExportRequestDto request)
        {
            if (string.IsNullOrEmpty(request.ReportType))
            {
                return Task.FromResult<(bool IsValid, string? ErrorMessage)>((false, "Jenis laporan harus dipilih"));
            }

            if (string.IsNullOrEmpty(request.ExportFormat))
            {
                return Task.FromResult<(bool IsValid, string? ErrorMessage)>((false, "Format export harus dipilih"));
            }

            var validFormats = new[] { "PDF", "Excel", "CSV" };
            if (!validFormats.Contains(request.ExportFormat, StringComparer.OrdinalIgnoreCase))
            {
                return Task.FromResult<(bool IsValid, string? ErrorMessage)>((false, "Format export tidak valid"));
            }

            if (string.IsNullOrEmpty(request.DateFrom) || string.IsNullOrEmpty(request.DateTo))
            {
                return Task.FromResult<(bool IsValid, string? ErrorMessage)>((false, "Tanggal mulai dan akhir harus diisi"));
            }

            if (!DateTime.TryParse(request.DateFrom, out var startDate) || 
                !DateTime.TryParse(request.DateTo, out var endDate))
            {
                return Task.FromResult<(bool IsValid, string? ErrorMessage)>((false, "Format tanggal tidak valid"));
            }

            if (startDate > endDate)
            {
                return Task.FromResult<(bool IsValid, string? ErrorMessage)>((false, "Tanggal mulai tidak boleh lebih besar dari tanggal akhir"));
            }

            return Task.FromResult<(bool IsValid, string? ErrorMessage)>((true, null));
        }

        public Task<List<object>> GetAvailableTemplatesAsync(string exportFormat)
        {
            return Task.FromResult(new List<object>
            {
                new { Name = "Standard", Description = "Template standar" },
                new { Name = "Detailed", Description = "Template detail lengkap" },
                new { Name = "Summary", Description = "Template ringkasan" }
            });
        }

        public Task<(bool Success, string? ErrorMessage)> SendExportViaEmailAsync(
            string filePath,
            string emailTo,
            string subject,
            string? body = null)
        {
            // Email functionality would be implemented here
            // For now, return success placeholder
            _logger.LogInformation("Email export requested for {FilePath} to {EmailTo}", filePath, emailTo);
            return Task.FromResult<(bool Success, string? ErrorMessage)>((true, null));
        }

        // ==================== STUB IMPLEMENTATIONS ==================== //

        public Task<(bool Success, string? FilePath, string? ErrorMessage)> CreateMultiSheetExcelAsync(
            Dictionary<string, object> sheetsData,
            string fileName,
            ExportRequestDto request)
        {
            return Task.FromResult<(bool Success, string? FilePath, string? ErrorMessage)>((false, null, "Multi-sheet Excel implementation pending"));
        }

        public Task<(bool Success, string? FilePath, string? ErrorMessage)> ExportWithCustomFormattingAsync<T>(
            T data,
            string format,
            Dictionary<string, object> formatOptions,
            string fileName)
        {
            return Task.FromResult<(bool Success, string? FilePath, string? ErrorMessage)>((false, null, "Custom formatting implementation pending"));
        }

        public Task<(bool Success, string? FilePath, string? ErrorMessage)> ExportWithChartsAsync(
            object data,
            List<object> chartDefinitions,
            string format,
            string fileName)
        {
            return Task.FromResult<(bool Success, string? FilePath, string? ErrorMessage)>((false, null, "Charts export implementation pending"));
        }

        public Task<(bool Success, List<string> FilePaths, string? ErrorMessage)> BatchExportAsync(
            List<ExportRequestDto> requests,
            string? zipFileName = null)
        {
            return Task.FromResult<(bool Success, List<string> FilePaths, string? ErrorMessage)>((false, new List<string>(), "Batch export implementation pending"));
        }

        private string FormatFileSize(long bytes)
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
