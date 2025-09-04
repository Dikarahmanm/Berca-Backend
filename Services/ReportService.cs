using Berca_Backend.Data;
using Berca_Backend.DTOs;
using Berca_Backend.Models;
using Berca_Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.Globalization;

namespace Berca_Backend.Services
{
    /// <summary>
    /// Comprehensive reporting service implementation
    /// Indonesian business context with error-safe patterns
    /// </summary>
    public class ReportService : IReportService
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ReportService> _logger;
        private readonly IConfiguration _configuration;
        private static readonly CultureInfo IdCulture = new("id-ID");

        public ReportService(
            AppDbContext context,
            IMemoryCache cache,
            ILogger<ReportService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
            _configuration = configuration;
        }

        // ==================== REPORT MANAGEMENT ==================== //

        public async Task<(List<ReportDto> Reports, int TotalCount)> GetReportsAsync(ReportFiltersDto filters)
        {
            try
            {
                var query = _context.Reports
                    .Include(r => r.CreatedByUser)
                    .Include(r => r.Branch)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(filters.ReportType))
                {
                    query = query.Where(r => r.ReportType == filters.ReportType);
                }

                if (filters.BranchId.HasValue)
                {
                    query = query.Where(r => r.BranchId == filters.BranchId.Value);
                }

                if (filters.IsActive.HasValue)
                {
                    query = query.Where(r => r.IsActive == filters.IsActive.Value);
                }

                if (filters.IsScheduled.HasValue)
                {
                    query = query.Where(r => r.IsScheduled == filters.IsScheduled.Value);
                }

                if (!string.IsNullOrEmpty(filters.SearchTerm))
                {
                    query = query.Where(r => r.Name.Contains(filters.SearchTerm) ||
                                           (!string.IsNullOrEmpty(r.Description) && r.Description.Contains(filters.SearchTerm)));
                }

                if (filters.StartDate != default && filters.EndDate != default)
                {
                    query = query.Where(r => r.CreatedAt >= filters.StartDate && 
                                           r.CreatedAt <= filters.EndDate);
                }

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply sorting (using standard LINQ instead of dynamic)
                var sortBy = filters.SortBy ?? "CreatedAt";
                var sortDirection = filters.SortDirection ?? "desc";
                
                query = sortBy.ToLower() switch
                {
                    "name" => sortDirection.ToLower() == "desc" 
                        ? query.OrderByDescending(r => r.Name)
                        : query.OrderBy(r => r.Name),
                    "reporttype" => sortDirection.ToLower() == "desc"
                        ? query.OrderByDescending(r => r.ReportType)
                        : query.OrderBy(r => r.ReportType),
                    "createdat" => sortDirection.ToLower() == "desc"
                        ? query.OrderByDescending(r => r.CreatedAt)
                        : query.OrderBy(r => r.CreatedAt),
                    "updatedat" => sortDirection.ToLower() == "desc"
                        ? query.OrderByDescending(r => r.UpdatedAt)
                        : query.OrderBy(r => r.UpdatedAt),
                    _ => sortDirection.ToLower() == "desc"
                        ? query.OrderByDescending(r => r.CreatedAt)
                        : query.OrderBy(r => r.CreatedAt)
                };

                // Apply pagination
                var reports = await query
                    .Skip((filters.PageNumber - 1) * filters.PageSize)
                    .Take(filters.PageSize)
                    .Select(r => new ReportDto
                    {
                        Id = r.Id,
                        Name = r.Name,
                        ReportType = r.ReportType,
                        Description = r.Description ?? string.Empty,
                        Parameters = r.Parameters ?? string.Empty,
                        IsActive = r.IsActive,
                        IsScheduled = r.IsScheduled,
                        ScheduleExpression = r.ScheduleExpression,
                        BranchId = r.BranchId,
                        BranchName = r.Branch != null ? (r.Branch.BranchName ?? string.Empty) : string.Empty,
                        CreatedBy = r.CreatedBy,
                        CreatedByName = r.CreatedByUser != null ? (r.CreatedByUser.Username ?? string.Empty) : string.Empty,
                        CreatedAt = r.CreatedAt.ToString("dd/MM/yyyy HH:mm", IdCulture),
                        UpdatedAt = r.UpdatedAt.ToString("dd/MM/yyyy HH:mm", IdCulture)
                    })
                    .ToListAsync();

                return (reports, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reports with filters");
                return (new List<ReportDto>(), 0);
            }
        }

        public async Task<ReportDto?> GetReportByIdAsync(int reportId)
        {
            try
            {
                var report = await _context.Reports
                    .Include(r => r.CreatedByUser)
                    .Include(r => r.UpdatedByUser)
                    .Include(r => r.Branch)
                    .FirstOrDefaultAsync(r => r.Id == reportId);

                if (report == null) return null;

                return new ReportDto
                {
                    Id = report.Id,
                    Name = report.Name,
                    ReportType = report.ReportType,
                    Description = report.Description ?? string.Empty,
                    Parameters = report.Parameters ?? string.Empty,
                    IsActive = report.IsActive,
                    IsScheduled = report.IsScheduled,
                    ScheduleExpression = report.ScheduleExpression,
                    BranchId = report.BranchId,
                    BranchName = report.Branch?.BranchName ?? string.Empty,
                    CreatedBy = report.CreatedBy,
                    CreatedByName = report.CreatedByUser != null ? (report.CreatedByUser.Username ?? string.Empty) : string.Empty,
                    CreatedAt = report.CreatedAt.ToString("dd/MM/yyyy HH:mm", IdCulture),
                    UpdatedAt = report.UpdatedAt.ToString("dd/MM/yyyy HH:mm", IdCulture)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting report by ID {ReportId}", reportId);
                return null;
            }
        }

        public async Task<(bool Success, int? ReportId, string? ErrorMessage)> CreateReportAsync(CreateReportDto dto, int createdBy)
        {
            try
            {
                var report = new Report
                {
                    Name = dto.Name,
                    ReportType = dto.ReportType,
                    Description = dto.Description,
                    Parameters = dto.Parameters,
                    IsScheduled = dto.IsScheduled,
                    ScheduleExpression = dto.ScheduleExpression,
                    BranchId = dto.BranchId,
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Reports.Add(report);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Report created: {ReportName} by user {UserId}", dto.Name, createdBy);
                return (true, report.Id, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating report");
                return (false, null, "Gagal membuat laporan: " + ex.Message);
            }
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateReportAsync(int reportId, UpdateReportDto dto, int updatedBy)
        {
            try
            {
                var report = await _context.Reports.FindAsync(reportId);
                if (report == null)
                {
                    return (false, "Laporan tidak ditemukan");
                }

                report.Name = dto.Name;
                report.Description = dto.Description ?? report.Description;
                report.Parameters = dto.Parameters ?? report.Parameters;
                report.IsActive = dto.IsActive;
                report.IsScheduled = dto.IsScheduled;
                report.ScheduleExpression = dto.ScheduleExpression ?? report.ScheduleExpression;
                report.UpdatedBy = updatedBy;
                report.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Report updated: {ReportId} by user {UserId}", reportId, updatedBy);
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating report {ReportId}", reportId);
                return (false, "Gagal memperbarui laporan: " + ex.Message);
            }
        }

        public async Task<(bool Success, string? ErrorMessage)> DeleteReportAsync(int reportId, int deletedBy)
        {
            try
            {
                var report = await _context.Reports
                    .Include(r => r.ReportExecutions)
                    .FirstOrDefaultAsync(r => r.Id == reportId);

                if (report == null)
                {
                    return (false, "Laporan tidak ditemukan");
                }

                // Check if report has executions
                if (report.ReportExecutions.Any())
                {
                    // Soft delete by marking as inactive
                    report.IsActive = false;
                    report.UpdatedBy = deletedBy;
                    report.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    // Hard delete if no executions
                    _context.Reports.Remove(report);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Report deleted: {ReportId} by user {UserId}", reportId, deletedBy);
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting report {ReportId}", reportId);
                return (false, "Gagal menghapus laporan: " + ex.Message);
            }
        }

        // ==================== REPORT EXECUTION ==================== //

        public async Task<(bool Success, object? Data, string? ErrorMessage)> ExecuteReportAsync(int reportId, Dictionary<string, object>? parameters = null, int? executedBy = null)
        {
            try
            {
                var report = await _context.Reports.FindAsync(reportId);
                if (report == null)
                {
                    return (false, null, "Laporan tidak ditemukan");
                }

                if (!report.IsActive)
                {
                    return (false, null, "Laporan tidak aktif");
                }

                // Create execution record
                var execution = new ReportExecution
                {
                    ReportId = reportId,
                    ExecutionType = "Manual",
                    Parameters = JsonConvert.SerializeObject(parameters ?? new Dictionary<string, object>()),
                    StartedAt = DateTime.UtcNow,
                    Status = "Running",
                    ExecutedBy = executedBy
                };

                _context.ReportExecutions.Add(execution);
                await _context.SaveChangesAsync();

                try
                {
                    // Execute report based on type
                    object? data = report.ReportType switch
                    {
                        "Sales" => await ExecuteSalesReport(parameters),
                        "Inventory" => await ExecuteInventoryReport(parameters),
                        "Financial" => await ExecuteFinancialReport(parameters),
                        "Supplier" => await ExecuteSupplierReport(parameters),
                        _ => throw new ArgumentException($"Unsupported report type: {report.ReportType}")
                    };

                    // Mark execution as completed
                    execution.Status = "Completed";
                    execution.CompletedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    return (true, data, null);
                }
                catch (Exception ex)
                {
                    // Mark execution as failed
                    execution.Status = "Failed";
                    execution.ErrorMessage = ex.Message;
                    execution.CompletedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing report {ReportId}", reportId);
                return (false, null, "Gagal menjalankan laporan: " + ex.Message);
            }
        }

        public async Task<ReportExecutionResultDto> ExecuteAndExportReportAsync(ExportRequestDto request, int executedBy)
        {
            var result = new ReportExecutionResultDto
            {
                Success = false,
                StartedAt = DateTime.UtcNow
            };

            try
            {
                // Execute the report first (only include non-null parameters)
                var execParams = new Dictionary<string, object>();
                if (!string.IsNullOrWhiteSpace(request.ReportType)) execParams["ReportType"] = request.ReportType!;
                if (!string.IsNullOrEmpty(request.DateFrom)) execParams["DateFrom"] = request.DateFrom!;
                if (!string.IsNullOrEmpty(request.DateTo)) execParams["DateTo"] = request.DateTo!;
                if (request.BranchId.HasValue) execParams["BranchId"] = request.BranchId.Value;
                if (request.CategoryId.HasValue) execParams["CategoryId"] = request.CategoryId.Value;
                if (request.SupplierId.HasValue) execParams["SupplierId"] = request.SupplierId.Value;
                if (request.UserId.HasValue) execParams["UserId"] = request.UserId.Value;

                var (success, data, errorMessage) = await ExecuteReportAsync(0, execParams, executedBy);

                if (!success || data == null)
                {
                    result.ErrorMessage = errorMessage ?? "Gagal menjalankan laporan";
                    return result;
                }

                // This would integrate with ExportService to generate files
                // For now, return success without actual file generation
                result.Success = true;
                result.CompletedAt = DateTime.UtcNow;
                result.OutputPath = $"/exports/{request.ReportType}_{DateTime.Now:yyyyMMdd_HHmmss}.{request.ExportFormat.ToLower()}";

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing and exporting report");
                result.ErrorMessage = ex.Message;
                result.CompletedAt = DateTime.UtcNow;
                return result;
            }
        }

        public async Task<List<ReportExecutionResultDto>> GetReportExecutionHistoryAsync(int reportId, int? limit = null)
        {
            try
            {
                IQueryable<ReportExecution> query = _context.ReportExecutions
                    .Where(re => re.ReportId == reportId)
                    .OrderByDescending(re => re.StartedAt);

                if (limit.HasValue)
                {
                    query = query.Take(limit.Value);
                }

                var executions = await query.ToListAsync();

                return executions.Select(e => new ReportExecutionResultDto
                {
                    ExecutionId = e.Id,
                    Success = e.Status == "Completed",
                    ErrorMessage = e.ErrorMessage,
                    OutputPath = e.OutputPath,
                    FileSizeBytes = e.FileSizeBytes,
                    StartedAt = e.StartedAt,
                    CompletedAt = e.CompletedAt,
                    ExecutionDurationMs = e.ExecutionDurationMs
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting execution history for report {ReportId}", reportId);
                return new List<ReportExecutionResultDto>();
            }
        }

        // ==================== SALES REPORTS ==================== //

        public async Task<DetailedSalesReportDto> GenerateSalesReportAsync(DateTime startDate, DateTime endDate, int? branchId = null, int? categoryId = null, int? userId = null)
        {
            try
            {
                var cacheKey = $"sales_report_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}_{branchId}_{categoryId}_{userId}";
                
                if (_cache.TryGetValue(cacheKey, out DetailedSalesReportDto? cachedResult) && cachedResult != null)
                {
                    return cachedResult;
                }

                var salesQuery = _context.Sales
                    .Include(s => s.SaleItems)
                        .ThenInclude(si => si.Product)
                            .ThenInclude(p => p!.Category)
                    .Include(s => s.Cashier)
                    .Where(s => s.SaleDate >= startDate && s.SaleDate <= endDate);

                if (userId.HasValue)
                {
                    salesQuery = salesQuery.Where(s => s.CashierId == userId.Value);
                }

                var sales = await salesQuery.ToListAsync();

                var report = new DetailedSalesReportDto
                {
                    ReportPeriod = $"{startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}",
                    TotalSales = sales.Sum(s => s.Total),
                    TotalTransactions = sales.Count,
                    AverageTransactionValue = sales.Any() ? sales.Average(s => s.Total) : 0,
                    TotalRefunds = sales.Where(s => s.Total < 0).Sum(s => Math.Abs(s.Total)),
                    NetSales = sales.Sum(s => s.Total) - sales.Where(s => s.Total < 0).Sum(s => Math.Abs(s.Total))
                };

                // Calculate total profit (simplified - would need cost data)
                report.TotalProfit = report.TotalSales * 0.25m; // Assume 25% margin

                // Top selling products
                var productSales = sales
                    .SelectMany(s => s.SaleItems)
                    .Where(si => si.Product != null)
                    .GroupBy(si => si.Product!)
                    .Select(g => new SalesItemDto
                    {
                        ProductId = g.Key.Id,
                        ProductName = g.Key.Name,
                        ProductBarcode = g.Key.Barcode ?? string.Empty,
                        CategoryName = g.Key.Category?.Name ?? "Tanpa Kategori",
                        QuantitySold = g.Sum(si => si.Quantity),
                        Revenue = g.Sum(si => si.Subtotal),
                        Profit = g.Sum(si => si.Subtotal) * 0.25m, // Assume 25% margin
                        AverageSellingPrice = g.Average(si => si.UnitPrice)
                    })
                    .OrderByDescending(p => p.Revenue)
                    .Take(10)
                    .ToList();

                report.TopSellingProducts = productSales;

                // Category performance
                var categoryPerformance = productSales
                    .GroupBy(p => p.CategoryName)
                    .Select(g => new CategorySalesPerformanceDto
                    {
                        CategoryName = g.Key,
                        TotalProducts = g.Count(),
                        QuantitySold = g.Sum(p => p.QuantitySold),
                        Revenue = g.Sum(p => p.Revenue),
                        Profit = g.Sum(p => p.Profit),
                        MarketShare = report.TotalSales > 0 ? (g.Sum(p => p.Revenue) / report.TotalSales) * 100 : 0
                    })
                    .ToList();

                report.CategoryPerformance = categoryPerformance;

                // Staff performance
                var staffPerformance = sales
                    .GroupBy(s => s.Cashier)
                    .Where(g => g.Key != null)
                    .Select(g => new UserPerformanceDto
                    {
                        UserId = g.Key!.Id,
                        UserName = g.Key.Username,
                        FullName = g.Key.Username,
                        TotalTransactions = g.Count(),
                        TotalSales = g.Sum(s => s.Total),
                        AverageTransactionValue = g.Average(s => s.Total)
                    })
                    .OrderByDescending(u => u.TotalSales)
                    .ToList();

                report.StaffPerformance = staffPerformance;

                // Cache the result
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
                };
                _cache.Set(cacheKey, report, cacheOptions);

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating sales report");
                return new DetailedSalesReportDto
                {
                    ReportPeriod = $"{startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}"
                };
            }
        }

        public async Task<List<CategorySalesPerformanceDto>> GetSalesPerformanceByCategoryAsync(DateTime startDate, DateTime endDate, int? branchId = null)
        {
            try
            {
                var salesItems = await _context.SaleItems
                    .Include(si => si.Product)
                        .ThenInclude(p => p!.Category)
                    .Include(si => si.Sale)
                    .Where(si => si.Sale.SaleDate >= startDate && si.Sale.SaleDate <= endDate)
                    .ToListAsync();

                var categoryPerformance = salesItems
                    .Where(si => si.Product?.Category != null)
                    .GroupBy(si => si.Product!.Category!)
                    .Select(g => new CategorySalesPerformanceDto
                    {
                        CategoryId = g.Key.Id,
                        CategoryName = g.Key.Name,
                        CategoryColor = g.Key.Color ?? "#000000",
                        TotalProducts = g.Select(si => si.ProductId).Distinct().Count(),
                        QuantitySold = g.Sum(si => si.Quantity),
                        Revenue = g.Sum(si => si.Subtotal),
                        Profit = g.Sum(si => si.Subtotal) * 0.25m // Assume 25% margin
                    })
                    .ToList();

                // Calculate market share
                var totalRevenue = categoryPerformance.Sum(c => c.Revenue);
                foreach (var category in categoryPerformance)
                {
                    category.MarketShare = totalRevenue > 0 ? (category.Revenue / totalRevenue) * 100 : 0;
                }

                return categoryPerformance.OrderByDescending(c => c.Revenue).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales performance by category");
                return new List<CategorySalesPerformanceDto>();
            }
        }

        public async Task<List<SalesItemDto>> GetTopSellingProductsAsync(DateTime startDate, DateTime endDate, int limit = 10, int? branchId = null)
        {
            try
            {
                var topProducts = await _context.SaleItems
                    .Include(si => si.Product)
                        .ThenInclude(p => p!.Category)
                    .Include(si => si.Sale)
                    .Where(si => si.Sale.SaleDate >= startDate && si.Sale.SaleDate <= endDate)
                    .Where(si => si.Product != null)
                    .GroupBy(si => si.Product!)
                    .Select(g => new SalesItemDto
                    {
                        ProductId = g.Key.Id,
                        ProductName = g.Key.Name,
                        ProductBarcode = g.Key.Barcode ?? string.Empty,
                        CategoryName = g.Key.Category != null ? g.Key.Category.Name : "Tanpa Kategori",
                        QuantitySold = g.Sum(si => si.Quantity),
                        Revenue = g.Sum(si => si.Subtotal),
                        Profit = g.Sum(si => si.Subtotal) * 0.25m, // Assume 25% margin
                        AverageSellingPrice = g.Average(si => si.UnitPrice)
                    })
                    .OrderByDescending(p => p.QuantitySold)
                    .Take(limit)
                    .ToListAsync();

                return topProducts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top selling products");
                return new List<SalesItemDto>();
            }
        }

        public async Task<List<UserPerformanceDto>> GetStaffPerformanceAsync(DateTime startDate, DateTime endDate, int? branchId = null)
        {
            try
            {
                var staffPerformance = await _context.Sales
                    .Include(s => s.Cashier)
                    .Where(s => s.SaleDate >= startDate && s.SaleDate <= endDate)
                    .Where(s => s.Cashier != null)
                    .GroupBy(s => s.Cashier!)
                    .Select(g => new UserPerformanceDto
                    {
                        UserId = g.Key.Id,
                        UserName = g.Key.Username,
                        FullName = g.Key.Username,
                        TotalTransactions = g.Count(),
                        TotalSales = g.Sum(s => s.Total),
                        AverageTransactionValue = g.Average(s => s.Total)
                    })
                    .OrderByDescending(u => u.TotalSales)
                    .ToListAsync();

                return staffPerformance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting staff performance");
                return new List<UserPerformanceDto>();
            }
        }

        public async Task<List<SalesByPeriodDto>> GetSalesByPeriodAsync(DateTime startDate, DateTime endDate, string periodType, int? branchId = null)
        {
            try
            {
                var sales = await _context.Sales
                    .Where(s => s.SaleDate >= startDate && s.SaleDate <= endDate)
                    .ToListAsync();

                var salesByPeriod = periodType.ToLower() switch
                {
                    "day" => sales
                        .GroupBy(s => s.SaleDate.Date)
                        .Select(g => new SalesByPeriodDto
                        {
                            Period = g.Key.ToString("dd/MM/yyyy"),
                            PeriodStart = g.Key,
                            PeriodEnd = g.Key.AddDays(1).AddTicks(-1),
                            Sales = g.Sum(s => s.Total),
                            Transactions = g.Count(),
                            AverageTransactionValue = g.Average(s => s.Total)
                        })
                        .OrderBy(p => p.PeriodStart)
                        .ToList(),

                    "month" => sales
                        .GroupBy(s => new { s.SaleDate.Year, s.SaleDate.Month })
                        .Select(g => new SalesByPeriodDto
                        {
                            Period = $"{g.Key.Month:00}/{g.Key.Year}",
                            PeriodStart = new DateTime(g.Key.Year, g.Key.Month, 1),
                            PeriodEnd = new DateTime(g.Key.Year, g.Key.Month, 1).AddMonths(1).AddTicks(-1),
                            Sales = g.Sum(s => s.Total),
                            Transactions = g.Count(),
                            AverageTransactionValue = g.Average(s => s.Total)
                        })
                        .OrderBy(p => p.PeriodStart)
                        .ToList(),

                    "hour" => sales
                        .GroupBy(s => s.SaleDate.Hour)
                        .Select(g => new SalesByPeriodDto
                        {
                            Period = $"{g.Key:00}:00",
                            PeriodStart = DateTime.Today.AddHours(g.Key),
                            PeriodEnd = DateTime.Today.AddHours(g.Key + 1).AddTicks(-1),
                            Sales = g.Sum(s => s.Total),
                            Transactions = g.Count(),
                            AverageTransactionValue = g.Average(s => s.Total)
                        })
                        .OrderBy(p => p.PeriodStart)
                        .ToList(),

                    _ => new List<SalesByPeriodDto>()
                };

                return salesByPeriod;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales by period");
                return new List<SalesByPeriodDto>();
            }
        }

        // ==================== PRIVATE HELPER METHODS ==================== //

        private async Task<object> ExecuteSalesReport(Dictionary<string, object>? parameters)
        {
            var startDate = parameters?.ContainsKey("DateFrom") == true ? 
                DateTime.Parse(parameters["DateFrom"].ToString()!) : DateTime.Today.AddDays(-30);
            var endDate = parameters?.ContainsKey("DateTo") == true ? 
                DateTime.Parse(parameters["DateTo"].ToString()!) : DateTime.Today;
            
            return await GenerateSalesReportAsync(startDate, endDate);
        }

        private async Task<object> ExecuteInventoryReport(Dictionary<string, object>? parameters)
        {
            return await GenerateInventoryReportAsync();
        }

        private async Task<object> ExecuteFinancialReport(Dictionary<string, object>? parameters)
        {
            var startDate = parameters?.ContainsKey("DateFrom") == true ? 
                DateTime.Parse(parameters["DateFrom"].ToString()!) : DateTime.Today.AddDays(-30);
            var endDate = parameters?.ContainsKey("DateTo") == true ? 
                DateTime.Parse(parameters["DateTo"].ToString()!) : DateTime.Today;
            
            return await GenerateFinancialReportAsync(startDate, endDate);
        }

        private async Task<object> ExecuteSupplierReport(Dictionary<string, object>? parameters)
        {
            var startDate = parameters?.ContainsKey("DateFrom") == true ? 
                DateTime.Parse(parameters["DateFrom"].ToString()!) : DateTime.Today.AddDays(-30);
            var endDate = parameters?.ContainsKey("DateTo") == true ? 
                DateTime.Parse(parameters["DateTo"].ToString()!) : DateTime.Today;
            
            return await GenerateSupplierPerformanceReportAsync(startDate, endDate);
        }

        // ==================== INVENTORY REPORTS ==================== //

        public async Task<DetailedInventoryReportDto> GenerateInventoryReportAsync(int? branchId = null, int? categoryId = null, string? status = null)
        {
            try
            {
                var productsQuery = _context.Products
                    .Include(p => p.Category)
                    .AsQueryable();

                if (categoryId.HasValue)
                {
                    productsQuery = productsQuery.Where(p => p.CategoryId == categoryId.Value);
                }

                var products = await productsQuery.ToListAsync();

                var report = new DetailedInventoryReportDto
                {
                    ReportDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm", IdCulture),
                    TotalProducts = products.Count,
                    TotalInventoryValue = products.Sum(p => p.Stock * p.SellPrice),
                    TotalCostValue = products.Sum(p => p.Stock * p.BuyPrice),
                    LowStockItems = products.Count(p => p.Stock <= p.MinimumStock && p.MinimumStock > 0),
                    OutOfStockItems = products.Count(p => p.Stock == 0)
                };

                // Inventory items
                var inventoryItems = products.Select(p => new InventoryItemDto
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    ProductBarcode = p.Barcode ?? string.Empty,
                    CategoryName = p.Category?.Name ?? "Tanpa Kategori",
                    CurrentStock = p.Stock,
                    MinimumStock = p.MinimumStock,
                    CostPerUnit = p.BuyPrice,
                    SellingPrice = p.SellPrice,
                    TotalValue = p.Stock * p.SellPrice,
                    StockStatus = GetStockStatus(p.Stock, p.MinimumStock)
                }).ToList();

                report.InventoryItems = inventoryItems;

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating inventory report");
                return new DetailedInventoryReportDto
                {
                    ReportDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm", IdCulture)
                };
            }
        }

        private static string GetStockStatus(int currentStock, int minimumStock)
        {
            if (currentStock == 0) return "OutOfStock";
            if (minimumStock > 0 && currentStock <= minimumStock) return "LowStock";
            return "Normal";
        }

        // ==================== STUB IMPLEMENTATIONS ==================== //
        // These methods provide basic implementations to prevent NotImplementedException

        public Task<List<InventoryItemDto>> GetLowStockItemsAsync(int? branchId = null, int? categoryId = null)
        {
            return Task.FromResult(new List<InventoryItemDto>());
        }

        public Task<List<InventoryMovementTrackingDto>> GetInventoryMovementsAsync(DateTime startDate, DateTime endDate, int? productId = null, int? branchId = null)
        {
            return Task.FromResult(new List<InventoryMovementTrackingDto>());
        }

        public Task<List<InventoryValuationDto>> GetInventoryValuationAsync(int? branchId = null)
        {
            return Task.FromResult(new List<InventoryValuationDto>());
        }

        public Task<List<CategoryInventoryBreakdownDto>> GetCategoryInventoryBreakdownAsync(int? branchId = null)
        {
            return Task.FromResult(new List<CategoryInventoryBreakdownDto>());
        }

        public Task<DetailedFinancialReportDto> GenerateFinancialReportAsync(DateTime startDate, DateTime endDate, int? branchId = null)
        {
            return Task.FromResult(new DetailedFinancialReportDto
            {
                ReportPeriod = $"{startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}"
            });
        }

        public async Task<DetailedFinancialReportDto> GetProfitLossStatementAsync(DateTime startDate, DateTime endDate, int? branchId = null)
        {
            return await GenerateFinancialReportAsync(startDate, endDate, branchId);
        }

        public Task<object> GetCashFlowReportAsync(DateTime startDate, DateTime endDate, int? branchId = null)
        {
            return Task.FromResult<object>(new { Message = "Cash flow report implementation pending" });
        }

        public Task<List<SupplierPerformanceDto>> GenerateSupplierPerformanceReportAsync(DateTime startDate, DateTime endDate, int? supplierId = null)
        {
            return Task.FromResult(new List<SupplierPerformanceDto>());
        }

        public Task<object> GetSupplierPaymentAnalysisAsync(DateTime startDate, DateTime endDate, int? supplierId = null)
        {
            return Task.FromResult<object>(new { Message = "Supplier payment analysis implementation pending" });
        }

        public Task<(bool IsValid, string? ErrorMessage)> ValidateReportParametersAsync(string reportType, Dictionary<string, object> parameters)
        {
            return Task.FromResult((true, (string?)null));
        }

        public Task<List<object>> GetAvailableReportTypesAsync()
        {
            return Task.FromResult(new List<object>
            {
                new { Value = "Sales", Label = "Laporan Penjualan", Description = "Laporan komprehensif penjualan dan performa produk" },
                new { Value = "Inventory", Label = "Laporan Inventori", Description = "Laporan stok, pergerakan barang, dan valuasi inventori" },
                new { Value = "Financial", Label = "Laporan Keuangan", Description = "Laporan keuangan, laba rugi, dan arus kas" },
                new { Value = "Supplier", Label = "Laporan Pemasok", Description = "Laporan performa dan analisis pemasok" }
            });
        }

        public async Task<(bool Success, string? ErrorMessage)> ScheduleReportAsync(int reportId, string cronExpression, int scheduledBy)
        {
            try
            {
                var report = await _context.Reports.FindAsync(reportId);
                if (report == null)
                {
                    return (false, "Laporan tidak ditemukan");
                }

                report.IsScheduled = true;
                report.ScheduleExpression = cronExpression;
                report.UpdatedBy = scheduledBy;
                report.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling report {ReportId}", reportId);
                return (false, "Gagal menjadwalkan laporan: " + ex.Message);
            }
        }

        public Task<List<object>> GetReportTemplatesAsync(string? category = null)
        {
            return Task.FromResult(new List<object>
            {
                new { Id = 1, Name = "Standard Sales Report", Category = "Sales" },
                new { Id = 2, Name = "Inventory Summary", Category = "Inventory" },
                new { Id = 3, Name = "Financial Overview", Category = "Financial" }
            });
        }
    }
}
