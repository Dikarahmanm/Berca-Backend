// Services/DashboardService.cs - FIXED: Remove duplicate export methods
using Berca_Backend.DTOs;
using Berca_Backend.Models;
using Berca_Backend.Data;
using Microsoft.EntityFrameworkCore;
using Berca_Backend.Extensions;
using Berca_Backend.Services.Interfaces;
using System.Text;

namespace Berca_Backend.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DashboardService> _logger;
        private readonly ITimezoneService _timezoneService;

        // ✅ Scoring configuration
        private readonly ScoringConfig _scoringConfig = new()
        {
            MaxScore = 100m,
            EnableDailyReset = true,
            LastResetDate = DateTime.UtcNow.Date,
            Weights = new ScoringWeights
            {
                QuantityWeight = 0.40m,
                RevenueWeight = 0.30m,
                ProfitWeight = 0.20m,
                FrequencyWeight = 0.10m,
                TimeDecayFactor = 0.95m
            }
        };

        public DashboardService(AppDbContext context, ILogger<DashboardService> logger, ITimezoneService timezoneService)
        {
            _context = context;
            _logger = logger;
            _timezoneService = timezoneService;
        }

        // ✅ Updated date range resolver dengan Indonesia timezone
        public DateRangeFilter ResolveDateRange(string period, DateTime? customStart = null, DateTime? customEnd = null)
        {
            // ✅ Use Indonesia timezone instead of UTC
            var now = _timezoneService.Now;
            var today = _timezoneService.Today;
            
            return period.ToLower() switch
            {
                "today" => new DateRangeFilter 
                { 
                    StartDate = _timezoneService.LocalToUtc(today), 
                    EndDate = _timezoneService.LocalToUtc(today.AddDays(1).AddTicks(-1)),
                    Period = "today"
                },
                "yesterday" => new DateRangeFilter 
                { 
                    StartDate = _timezoneService.LocalToUtc(today.AddDays(-1)), 
                    EndDate = _timezoneService.LocalToUtc(today.AddTicks(-1)),
                    Period = "yesterday"
                },
                "week" => new DateRangeFilter 
                { 
                    StartDate = _timezoneService.LocalToUtc(today.AddDays(-7)), 
                    EndDate = _timezoneService.LocalToUtc(today.AddDays(1).AddTicks(-1)),
                    Period = "week"
                },
                "month" => new DateRangeFilter 
                { 
                    StartDate = _timezoneService.LocalToUtc(new DateTime(today.Year, today.Month, 1)), 
                    EndDate = _timezoneService.LocalToUtc(today.AddDays(1).AddTicks(-1)),
                    Period = "month"
                },
                "year" => new DateRangeFilter 
                { 
                    StartDate = _timezoneService.LocalToUtc(new DateTime(today.Year, 1, 1)), 
                    EndDate = _timezoneService.LocalToUtc(today.AddDays(1).AddTicks(-1)),
                    Period = "year"
                },
                "custom" => new DateRangeFilter 
                { 
                    StartDate = _timezoneService.LocalToUtc(customStart ?? today.AddDays(-30)), 
                    EndDate = _timezoneService.LocalToUtc(customEnd ?? today.AddDays(1).AddTicks(-1)),
                    Period = "custom"
                },
                _ => new DateRangeFilter 
                { 
                    StartDate = _timezoneService.LocalToUtc(today.AddDays(-30)), 
                    EndDate = _timezoneService.LocalToUtc(today.AddDays(1).AddTicks(-1)),
                    Period = "default"
                }
            };
        }

        public async Task<DashboardKPIDto> GetDashboardKPIsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                _logger.LogInformation("=== KPI ENDPOINT CALLED ===");
                _logger.LogInformation("Input Parameters - StartDate: {StartDate}, EndDate: {EndDate}", startDate, endDate);

                var today = _timezoneService.Today;
                _logger.LogInformation("Indonesia Today: {Today}", today);

                // ✅ FIX: Standardize date handling for both services
                DateTime monthStartLocal, monthEndLocal;

                if (startDate.HasValue && endDate.HasValue)
                {
                    // Handle input dates consistently
                    if (startDate.Value.Kind == DateTimeKind.Utc)
                    {
                        monthStartLocal = _timezoneService.UtcToLocal(startDate.Value).Date;
                        monthEndLocal = _timezoneService.UtcToLocal(endDate.Value).Date;
                    }
                    else
                    {
                        monthStartLocal = startDate.Value.Date;
                        monthEndLocal = endDate.Value.Date;
                    }
                }
                else
                {
                    // Default to current month
                    monthStartLocal = new DateTime(today.Year, today.Month, 1);
                    monthEndLocal = today;
                }

                var yearStartLocal = startDate?.Date ?? new DateTime(today.Year, 1, 1);

                _logger.LogInformation("Local Date Range - Start: {Start}, End: {End}", monthStartLocal, monthEndLocal);

                // ✅ FIX: Use INCLUSIVE end date (don't add extra day for dashboard)
                var todayUtc = _timezoneService.LocalToUtc(today);
                var tomorrowUtc = _timezoneService.LocalToUtc(today.AddDays(1));
                var monthStartUtc = _timezoneService.LocalToUtc(monthStartLocal);
                var monthEndUtc = _timezoneService.LocalToUtc(monthEndLocal.Date.AddDays(1).AddMilliseconds(-1)); // End of day
                var yearStartUtc = _timezoneService.LocalToUtc(yearStartLocal);

                _logger.LogInformation("UTC Conversion - monthStartUtc: {StartUtc}, monthEndUtc: {EndUtc}", monthStartUtc, monthEndUtc);

                // Query with consistent date ranges
                var todaySales = await _context.Sales
                    .Include(s => s.SaleItems)
                    .Where(s => s.SaleDate >= todayUtc && s.SaleDate < tomorrowUtc && s.Status == SaleStatus.Completed)
                    .ToListAsync();

                var monthlySales = await _context.Sales
                    .Include(s => s.SaleItems)
                    .Where(s => s.SaleDate >= monthStartUtc && s.SaleDate <= monthEndUtc && s.Status == SaleStatus.Completed)
                    .ToListAsync();

                _logger.LogInformation("KPI Query Filter: SaleDate >= {Start} AND SaleDate <= {End} AND Status = {Status}",
                    monthStartUtc, monthEndUtc, SaleStatus.Completed);
                _logger.LogInformation("KPI Results - Count: {Count}, Total: {Total:N2}", monthlySales.Count, monthlySales.Sum(s => s.Total));

                if (monthlySales.Any())
                {
                    _logger.LogInformation("KPI Sample Dates: {Dates}", string.Join(", ", monthlySales.Take(5).Select(s => s.SaleDate.ToString("yyyy-MM-dd HH:mm:ss"))));
                    _logger.LogInformation("KPI Date Range in Results: {First} to {Last}", monthlySales.Min(s => s.SaleDate), monthlySales.Max(s => s.SaleDate));
                }

                var yearlySales = await _context.Sales
                    .Include(s => s.SaleItems)
                    .Where(s => s.SaleDate >= yearStartUtc && s.Status == SaleStatus.Completed)
                    .ToListAsync();

                // Product counts
                var totalProducts = await _context.Products.CountAsync(p => p.IsActive);
                var lowStockProducts = await _context.Products
                    .CountAsync(p => p.IsActive && p.Stock <= p.MinimumStock);

                // Member count
                var totalMembers = await _context.Members.CountAsync(m => m.IsActive);

                // Inventory value
                var inventoryValue = await _context.Products
                    .Where(p => p.IsActive)
                    .SumAsync(p => p.Stock * p.BuyPrice);

                // Calculate profit consistently
                var totalProfit = monthlySales
                    .SelectMany(s => s.SaleItems)
                    .Sum(si => (si.UnitPrice - si.UnitCost) * si.Quantity - si.DiscountAmount);

                _logger.LogInformation("KPI Calculated Profit: {Profit:N2}", totalProfit);
                _logger.LogInformation("=== END KPI DEBUG ===");

                return new DashboardKPIDto
                {
                    TodayRevenue = todaySales.Sum(s => s.Total),
                    MonthlyRevenue = monthlySales.Sum(s => s.Total),
                    YearlyRevenue = yearlySales.Sum(s => s.Total),
                    TodayTransactions = todaySales.Count,
                    MonthlyTransactions = monthlySales.Count,
                    AverageTransactionValue = monthlySales.Any() ? monthlySales.Average(s => s.Total) : 0,
                    TotalProfit = totalProfit,
                    TotalProducts = totalProducts,
                    LowStockProducts = lowStockProducts,
                    TotalMembers = totalMembers,
                    InventoryValue = inventoryValue
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard KPIs");
                throw;
            }
        }

        public async Task<List<ChartDataDto>> GetSalesChartDataAsync(string period = "daily", DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow;

                var sales = await _context.Sales
                    .Where(s => s.SaleDate >= start && s.SaleDate <= end && s.Status == SaleStatus.Completed)
                    .ToListAsync();

                return period.ToLower() switch
                {
                    "daily" => sales
                        .GroupBy(s => s.SaleDate.Date)
                        .Select(g => new ChartDataDto
                        {
                            Label = g.Key.ToString("dd/MM"),
                            Value = g.Sum(s => s.Total),
                            Date = g.Key
                        })
                        .OrderBy(c => c.Date)
                        .ToList(),

                    "weekly" => sales
                        .GroupBy(s => GetWeekOfYear(s.SaleDate))
                        .Select(g => new ChartDataDto
                        {
                            Label = $"Week {g.Key}",
                            Value = g.Sum(s => s.Total),
                            Date = g.First().SaleDate
                        })
                        .OrderBy(c => c.Date)
                        .ToList(),

                    "monthly" => sales
                        .GroupBy(s => new { s.SaleDate.Year, s.SaleDate.Month })
                        .Select(g => new ChartDataDto
                        {
                            Label = $"{g.Key.Month:D2}/{g.Key.Year}",
                            Value = g.Sum(s => s.Total),
                            Date = new DateTime(g.Key.Year, g.Key.Month, 1)
                        })
                        .OrderBy(c => c.Date)
                        .ToList(),

                    _ => throw new ArgumentException("Invalid period. Use 'daily', 'weekly', or 'monthly'")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales chart data");
                throw;
            }
        }

        public async Task<List<ChartDataDto>> GetRevenueChartDataAsync(string period = "monthly", DateTime? startDate = null, DateTime? endDate = null)
        {
            return await GetSalesChartDataAsync(period, startDate, endDate);
        }

            // ✅ FIXED: Updated with Enhanced Scoring and Filtering
        public async Task<List<TopProductDto>> GetTopSellingProductsAsync(int count = 10, DateTime? startDate = null, DateTime? endDate = null, string sortBy = "quantity")
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow;
                
                // Determine period based on date range
                var period = DeterminePeriodFromDateRange(start, end);

                var saleItemsData = await _context.SaleItems
                    .Include(si => si.Sale)
                    .Include(si => si.Product)
                    .Where(si => si.Sale.SaleDate >= start && si.Sale.SaleDate <= end && si.Sale.Status == SaleStatus.Completed)
                    .GroupBy(si => si.ProductId)
                    .Select(g => new
                    {
                        ProductId = g.Key,
                        ProductName = g.First().Product.Name,
                        ProductBarcode = g.First().Product.Barcode,
                        TotalQuantitySold = g.Sum(si => si.Quantity),
                        TotalRevenue = g.Sum(si => si.Subtotal),
                        TotalProfit = g.Sum(si => (si.UnitPrice - si.UnitCost) * si.Quantity - si.DiscountAmount),
                        TransactionCount = g.Count(),
                        LastSaleDate = g.Max(si => si.Sale.SaleDate)
                    })
                    .ToListAsync();

                // ✅ Calculate enhanced scores with normalization
                var results = saleItemsData.Select(item => 
                {
                    var rawScore = CalculateEnhancedProductScore(
                        item.TotalQuantitySold, 
                        item.TotalRevenue, 
                        item.TotalProfit, 
                        item.TransactionCount,
                        item.LastSaleDate,
                        period
                    );

                    var normalizedScore = Math.Min(rawScore * (100m / GetMaxPossibleScore(period)), 100m);
                    var daysSinceLastSale = (DateTime.UtcNow.Date - item.LastSaleDate.Date).Days;
                    
                    var performanceCategory = GetTopProductPerformanceCategory(normalizedScore);
                    var badgeColor = GetPerformanceBadgeColor(performanceCategory);

                    return new TopProductDto
                    {
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        ProductBarcode = item.ProductBarcode,
                        TotalQuantitySold = item.TotalQuantitySold,
                        TotalRevenue = item.TotalRevenue,
                        TotalProfit = item.TotalProfit,
                        TransactionCount = item.TransactionCount,
                        WeightedScore = rawScore,
                        NormalizedScore = normalizedScore,
                        ProfitMargin = item.TotalRevenue != 0 ? (item.TotalProfit / item.TotalRevenue) * 100 : 0,
                        AverageQuantityPerTransaction = item.TransactionCount != 0 ? (decimal)item.TotalQuantitySold / item.TransactionCount : 0,
                        PerformanceCategory = performanceCategory,
                        PerformanceBadgeColor = badgeColor,
                        LastSaleDate = item.LastSaleDate,
                        DaysSinceLastSale = daysSinceLastSale
                    };
                }).ToList();

                // Sort by selected criteria
                var sortedData = sortBy.ToLower() switch
                {
                    "revenue" => results.OrderByDescending(p => p.TotalRevenue),
                    "profit" => results.OrderByDescending(p => p.TotalProfit),
                    "transactions" => results.OrderByDescending(p => p.TransactionCount),
                    "normalized" => results.OrderByDescending(p => p.NormalizedScore),
                    "weighted" => results.OrderByDescending(p => p.WeightedScore),
                    _ => results.OrderByDescending(p => p.TotalQuantitySold)
                };

                return sortedData.Take(count).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top selling products");
                throw;
            }
        }

        public async Task<List<ProductDto>> GetLowStockAlertsAsync()
        {
            try
            {
                return await _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.IsActive && p.Stock <= p.MinimumStock)
                    .Select(p => new ProductDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Barcode = p.Barcode,
                        Stock = p.Stock,
                        MinimumStock = p.MinimumStock,
                        BuyPrice = p.BuyPrice,
                        SellPrice = p.SellPrice,
                        CategoryId = p.CategoryId,
                        CategoryName = p.Category.Name,
                        CategoryColor = p.Category.Color,
                        IsActive = p.IsActive,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt,
                        IsLowStock = true,
                        IsOutOfStock = p.Stock <= 0
                    })
                    .OrderBy(p => p.Stock)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting low stock alerts");
                throw;
            }
        }

        public async Task<List<CategorySalesDto>> GetCategorySalesAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow;

                return await _context.SaleItems
                    .Include(si => si.Sale)
                    .Include(si => si.Product)
                        .ThenInclude(p => p.Category)
                    .Where(si => si.Sale.SaleDate >= start && si.Sale.SaleDate <= end && si.Sale.Status == SaleStatus.Completed)
                    .GroupBy(si => si.Product.CategoryId)
                    .Select(g => new CategorySalesDto
                    {
                        CategoryId = g.Key,
                        CategoryName = g.First().Product.Category.Name,
                        CategoryColor = g.First().Product.Category.Color,
                        TotalQuantitySold = g.Sum(si => si.Quantity),
                        TotalRevenue = g.Sum(si => si.Subtotal),
                        ProductCount = g.Select(si => si.ProductId).Distinct().Count(),
                        TransactionCount = g.Select(si => si.SaleId).Distinct().Count()
                    })
                    .OrderByDescending(c => c.TotalRevenue)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting category sales");
                throw;
            }
        }

        public async Task<QuickStatsDto> GetQuickStatsAsync()
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var yesterday = today.AddDays(-1);

                var todaySales = await _context.Sales
                    .Where(s => s.SaleDate.Date == today && s.Status == SaleStatus.Completed)
                    .ToListAsync();

                var yesterdaySales = await _context.Sales
                    .Where(s => s.SaleDate.Date == yesterday && s.Status == SaleStatus.Completed)
                    .ToListAsync();

                var todayRevenue = todaySales.Sum(s => s.Total);
                var yesterdayRevenue = yesterdaySales.Sum(s => s.Total);
                var revenueGrowth = yesterdayRevenue > 0 ? ((todayRevenue - yesterdayRevenue) / yesterdayRevenue) * 100 : 0;

                var pendingOrders = await _context.Sales.CountAsync(s => s.Status == SaleStatus.Pending);
                var lowStockCount = await _context.Products.CountAsync(p => p.IsActive && p.Stock <= p.MinimumStock);

                return new QuickStatsDto
                {
                    TodayRevenue = todayRevenue,
                    TodayTransactions = todaySales.Count,
                    RevenueGrowthPercentage = revenueGrowth,
                    PendingOrders = pendingOrders,
                    LowStockAlerts = lowStockCount,
                    ActiveMembers = await _context.Members.CountAsync(m => m.IsActive)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quick stats");
                throw;
            }
        }

        public async Task<List<RecentTransactionDto>> GetRecentTransactionsAsync(int count = 10)
        {
            try
            {
                return await _context.Sales
                    .Include(s => s.Member)
                    .Include(s => s.Cashier)
                        .ThenInclude(c => c.UserProfile)
                    .Where(s => s.Status == SaleStatus.Completed)
                    .OrderByDescending(s => s.SaleDate)
                    .Take(count)
                    .Select(s => new RecentTransactionDto
                    {
                        Id = s.Id,
                        SaleNumber = s.SaleNumber,
                        SaleDate = s.SaleDate,
                        Total = s.Total,
                        PaymentMethod = s.PaymentMethod,
                        CustomerName = s.CustomerName ?? (s.Member != null ? s.Member.Name : "Guest"),
                        CashierName = s.Cashier.UserProfile != null ? s.Cashier.UserProfile.FullName : s.Cashier.Username,
                        ItemCount = s.TotalItems
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent transactions");
                throw;
            }
        }

        // Keep all existing report methods unchanged...
        public async Task<SalesReportDto> GenerateSalesReportAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var sales = await _context.Sales
                    .Include(s => s.SaleItems) // ✅ ADDED: Include SaleItems for calculations
                    .Where(s => s.SaleDate >= startDate && s.SaleDate <= endDate && s.Status == SaleStatus.Completed)
                    .ToListAsync();

                var paymentMethodBreakdown = sales
                    .GroupBy(s => s.PaymentMethod)
                    .Select(g => new PaymentMethodSummaryDto
                    {
                        PaymentMethod = g.Key,
                        Total = g.Sum(s => s.Total),
                        TransactionCount = g.Count(),
                        Percentage = sales.Sum(s => s.Total) > 0 ? (g.Sum(s => s.Total) / sales.Sum(s => s.Total)) * 100 : 0
                    })
                    .OrderByDescending(p => p.Total)
                    .ToList();

                var totalItemsSold = sales.Sum(s => s.SaleItems.Sum(si => si.Quantity)); // ✅ FIXED: Calculate total items sold
                var totalProfit = sales.Sum(s => s.SaleItems.Sum(si => si.TotalProfit)); // ✅ ADDED: Calculate total profit

                return new SalesReportDto
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    TotalRevenue = sales.Sum(s => s.Total),
                    TotalTransactions = sales.Count,
                    TotalItemsSold = totalItemsSold, // ✅ FIXED: Use calculated value
                    TotalProfit = totalProfit, // ✅ ADDED: Include profit
                    AverageTransactionValue = sales.Any() ? sales.Average(s => s.Total) : 0,
                    PaymentMethodBreakdown = paymentMethodBreakdown,
                    GeneratedAt = _timezoneService.Now
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating sales report");
                throw;
            }
        }

        public async Task<InventoryReportDto> GenerateInventoryReportAsync()
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.IsActive)
                    .ToListAsync();

                var totalProducts = products.Count;
                var totalInventoryValue = products.Sum(p => p.Stock * p.BuyPrice);
                var lowStockProducts = products.Count(p => p.Stock <= p.MinimumStock);
                var outOfStockProducts = products.Count(p => p.Stock <= 0);

                var categoryBreakdown = products
                    .GroupBy(p => p.Category)
                    .Select(g => new CategoryInventoryDto
                    {
                        CategoryName = g.Key.Name,
                        CategoryColor = g.Key.Color,
                        ProductCount = g.Count(),
                        TotalValue = g.Sum(p => p.Stock * p.BuyPrice),
                        LowStockCount = g.Count(p => p.Stock <= p.MinimumStock)
                    })
                    .ToList();

                return new InventoryReportDto
                {
                    TotalProducts = totalProducts,
                    TotalInventoryValue = totalInventoryValue,
                    LowStockProducts = lowStockProducts,
                    OutOfStockProducts = outOfStockProducts,
                    CategoryBreakdown = categoryBreakdown,
                    GeneratedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating inventory report");
                throw;
            }
        }

        // ✅ Updated worst performing products with enhanced scoring
        public async Task<List<WorstProductDto>> GetWorstPerformingProductsAsync(int count = 10, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow;

                // Get ALL active products first, then join with sales
                var allProducts = await _context.Products
                    .Where(p => p.IsActive)
                    .ToListAsync();

                // Get sales data for the period
                var salesData = await _context.SaleItems
                    .Include(si => si.Sale)
                    .Where(si => si.Sale.SaleDate >= start && si.Sale.SaleDate <= end && si.Sale.Status == SaleStatus.Completed)
                    .GroupBy(si => si.ProductId)
                    .Select(g => new
                    {
                        ProductId = g.Key,
                        TotalQuantitySold = g.Sum(si => si.Quantity),
                        TotalRevenue = g.Sum(si => si.Subtotal),
                        TotalProfit = g.Sum(si => (si.UnitPrice - si.UnitCost) * si.Quantity - si.DiscountAmount),
                        TransactionCount = g.Count(),
                        LastSaleDate = g.Max(si => si.Sale.SaleDate)
                    })
                    .ToListAsync();

                var results = new List<WorstProductDto>();

                foreach (var product in allProducts)
                {
                    var sales = salesData.FirstOrDefault(s => s.ProductId == product.Id);
                    
                    // Calculate performance metrics
                    var quantitySold = sales?.TotalQuantitySold ?? 0;
                    var revenue = sales?.TotalRevenue ?? 0;
                    var profit = sales?.TotalProfit ?? 0;
                    var transactionCount = sales?.TransactionCount ?? 0;
                    
                    // Calculate days without sale
                    var lastSaleDate = sales?.LastSaleDate;
                    var daysWithoutSale = lastSaleDate.HasValue 
                        ? (DateTime.UtcNow.Date - lastSaleDate.Value.Date).Days 
                        : 9999; // Never sold

                    // ✅ Calculate performance score
                    var performanceScore = CalculatePerformanceScore(quantitySold, revenue, profit, daysWithoutSale, product.Stock);
                    var normalizedScore = Math.Min(performanceScore * (100m / 100m), 100m);
                    
                    // ✅ Determine performance category based on score
                    var performanceCategory = GetWorstPerformanceCategory(performanceScore);

                    results.Add(new WorstProductDto
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        ProductBarcode = product.Barcode,
                        TotalQuantitySold = quantitySold,
                        TotalRevenue = revenue,
                        TotalProfit = profit,
                        TransactionCount = transactionCount,
                        DaysWithoutSale = daysWithoutSale,
                        CurrentStock = product.Stock,
                        PerformanceScore = performanceScore,
                        NormalizedScore = normalizedScore,
                        PerformanceCategory = performanceCategory,
                        LastSaleDate = lastSaleDate,
                        StockTurnoverRatio = product.Stock > 0 ? (decimal)quantitySold / product.Stock : 0
                    });
                }

                // Sort by multiple criteria
                return results
                    .OrderBy(r => r.PerformanceScore) // Worst score first
                    .ThenBy(r => r.TotalQuantitySold) // Then by quantity
                    .ThenByDescending(r => r.DaysWithoutSale) // Then by days without sale
                    .Take(count)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting worst performing products");
                throw;
            }
        }

        // Keep all existing report methods unchanged...
        public async Task<FinancialReportDto> GenerateFinancialReportAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var sales = await _context.Sales
                    .Include(s => s.SaleItems)
                    .Where(s => s.SaleDate >= startDate && s.SaleDate <= endDate && s.Status == SaleStatus.Completed)
                    .ToListAsync();

                var totalRevenue = sales.Sum(s => s.Total);
                var totalCost = sales.SelectMany(s => s.SaleItems).Sum(si => si.UnitCost * si.Quantity);
                var grossProfit = totalRevenue - totalCost;
                var grossProfitMargin = totalRevenue > 0 ? (grossProfit / totalRevenue) * 100 : 0;
                var totalTax = sales.Sum(s => s.TaxAmount);
                var netProfit = grossProfit - totalTax;

                // Monthly breakdown
                var monthlyBreakdown = sales
                    .GroupBy(s => new { s.SaleDate.Year, s.SaleDate.Month })
                    .Select(g => new MonthlyProfitDto
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM yyyy"),
                        Revenue = g.Sum(s => s.Total),
                        Cost = g.SelectMany(s => s.SaleItems).Sum(si => si.UnitCost * si.Quantity),
                        Profit = g.Sum(s => s.Total) - g.SelectMany(s => s.SaleItems).Sum(si => si.UnitCost * si.Quantity)
                    })
                    .OrderBy(m => m.Year).ThenBy(m => m.Month)
                    .ToList();

                return new FinancialReportDto
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    TotalRevenue = totalRevenue,
                    TotalCost = totalCost,
                    GrossProfit = grossProfit,
                    GrossProfitMargin = grossProfitMargin,
                    TotalTax = totalTax,
                    NetProfit = netProfit,
                    MonthlyBreakdown = monthlyBreakdown,
                    GeneratedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating financial report");
                throw;
            }
        }

        public async Task<CustomerReportDto> GenerateCustomerReportAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var salesInPeriod = await _context.Sales
                    .Include(s => s.Member)
                    .Where(s => s.SaleDate >= startDate && s.SaleDate <= endDate && s.Status == SaleStatus.Completed)
                    .ToListAsync();

                var totalActiveMembers = await _context.Members.CountAsync(m => m.IsActive);
                var newMembers = await _context.Members
                    .CountAsync(m => m.CreatedAt >= startDate && m.CreatedAt <= endDate);

                var memberSales = salesInPeriod.Where(s => s.MemberId.HasValue);
                var guestSales = salesInPeriod.Where(s => !s.MemberId.HasValue);

                var averageOrderValue = salesInPeriod.Any() ? salesInPeriod.Average(s => s.Total) : 0;
                var totalMemberRevenue = memberSales.Sum(s => s.Total);
                var guestRevenue = guestSales.Sum(s => s.Total);

                // Top customers
                var topCustomers = salesInPeriod
                    .GroupBy(s => new { s.MemberId, s.CustomerName })
                    .Select(g => new TopCustomerDto
                    {
                        MemberId = g.Key.MemberId,
                        CustomerName = g.Key.CustomerName ?? "Guest",
                        MembershipType = g.Key.MemberId.HasValue ? "Member" : "Guest",
                        TotalSpent = g.Sum(s => s.Total),
                        TransactionCount = g.Count(),
                        AverageOrderValue = g.Average(s => s.Total),
                        LastPurchase = g.Max(s => s.SaleDate)
                    })
                    .OrderByDescending(c => c.TotalSpent)
                    .Take(10)
                    .ToList();

                // Loyalty analysis (simplified)
                var loyaltyAnalysis = new List<MemberLoyaltyDto>
                {
                    new MemberLoyaltyDto
                    {
                        LoyaltyTier = "High Value (>1M)",
                        MemberCount = topCustomers.Count(c => c.TotalSpent > 1000000),
                        TotalRevenue = topCustomers.Where(c => c.TotalSpent > 1000000).Sum(c => c.TotalSpent),
                        AverageSpend = topCustomers.Where(c => c.TotalSpent > 1000000).DefaultIfEmpty().Average(c => c?.TotalSpent ?? 0),
                        Percentage = totalMemberRevenue > 0 ? (topCustomers.Where(c => c.TotalSpent > 1000000).Sum(c => c.TotalSpent) / totalMemberRevenue) * 100 : 0
                    },
                    new MemberLoyaltyDto
                    {
                        LoyaltyTier = "Medium Value (100K-1M)",
                        MemberCount = topCustomers.Count(c => c.TotalSpent >= 100000 && c.TotalSpent <= 1000000),
                        TotalRevenue = topCustomers.Where(c => c.TotalSpent >= 100000 && c.TotalSpent <= 1000000).Sum(c => c.TotalSpent),
                        AverageSpend = topCustomers.Where(c => c.TotalSpent >= 100000 && c.TotalSpent <= 1000000).DefaultIfEmpty().Average(c => c?.TotalSpent ?? 0),
                        Percentage = totalMemberRevenue > 0 ? (topCustomers.Where(c => c.TotalSpent >= 100000 && c.TotalSpent <= 1000000).Sum(c => c.TotalSpent) / totalMemberRevenue) * 100 : 0
                    },
                    new MemberLoyaltyDto
                    {
                        LoyaltyTier = "Low Value (<100K)",
                        MemberCount = topCustomers.Count(c => c.TotalSpent < 100000),
                        TotalRevenue = topCustomers.Where(c => c.TotalSpent < 100000).Sum(c => c.TotalSpent),
                        AverageSpend = topCustomers.Where(c => c.TotalSpent < 100000).DefaultIfEmpty().Average(c => c?.TotalSpent ?? 0),
                        Percentage = totalMemberRevenue > 0 ? (topCustomers.Where(c => c.TotalSpent < 100000).Sum(c => c.TotalSpent) / totalMemberRevenue) * 100 : 0
                    }
                };

                return new CustomerReportDto
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    TotalActiveMembers = totalActiveMembers,
                    NewMembersThisPeriod = newMembers,
                    AverageOrderValue = averageOrderValue,
                    TotalMemberRevenue = totalMemberRevenue,
                    GuestRevenue = guestRevenue,
                    TopCustomers = topCustomers,
                    LoyaltyAnalysis = loyaltyAnalysis,
                    GeneratedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating customer report");
                throw;
            }
        }

        // ✅ FIXED: Enhanced scoring methods
        private decimal CalculateEnhancedProductScore(int quantity, decimal revenue, decimal profit, int transactions, DateTime lastSaleDate, string period)
        {
            var weights = _scoringConfig.Weights;
            
            // ✅ Use fixed normalizers (not adaptive to avoid async issues)
            var revenueNormalizer = GetRevenueNormalizer(period);
            var profitNormalizer = GetProfitNormalizer(period);
            
            // Base scores with fixed normalizers
            decimal quantityScore = Math.Min(quantity * weights.QuantityWeight, 40m);  // Cap at 40
            decimal revenueScore = Math.Min((revenue / revenueNormalizer) * weights.RevenueWeight * 100m, 30m); // Cap at 30
            decimal profitScore = Math.Min((profit / profitNormalizer) * weights.ProfitWeight * 100m, 20m); // Cap at 20
            decimal transactionScore = Math.Min(transactions * weights.FrequencyWeight * 2m, 10m); // Cap at 10

            // Time decay factor
            var timeDecay = CalculateTimeDecay(lastSaleDate, period);
            
            var rawScore = (quantityScore + revenueScore + profitScore + transactionScore) * timeDecay;
            
            return Math.Min(rawScore, _scoringConfig.MaxScore);
        }

        // ✅ FIXED: Time decay calculation
        private decimal CalculateTimeDecay(DateTime lastSaleDate, string period)
        {
            // Convert UTC to local time for calculation
            var localLastSaleDate = _timezoneService.UtcToLocal(lastSaleDate);
            var localNow = _timezoneService.Now;
            var daysSinceLastSale = (localNow.Date - localLastSaleDate.Date).Days;
            
            return period.ToLower() switch
            {
                "today" => daysSinceLastSale == 0 ? 1.0m : 0.5m,
                "yesterday" => daysSinceLastSale <= 1 ? 1.0m : 0.7m,
                "week" => daysSinceLastSale <= 7 ? 1.0m : (decimal)Math.Pow((double)_scoringConfig.Weights.TimeDecayFactor, daysSinceLastSale - 7),
                "month" => daysSinceLastSale <= 30 ? 1.0m : (decimal)Math.Pow((double)_scoringConfig.Weights.TimeDecayFactor, daysSinceLastSale - 30),
                _ => 1.0m
            };
        }

        // ✅ FIXED: Realistic normalizers based on your actual data scale
        private decimal GetRevenueNormalizer(string period) => period.ToLower() switch
        {
            "today" => 50000m,     // 50K for daily
            "week" => 200000m,     // 200K for weekly  
            "month" => 800000m,    // ✅ 800K untuk monthly (turun dari 1.5M)
            "year" => 8000000m,    // ✅ 8M untuk yearly (turun dari 15M)
            _ => 300000m           // 300K default
        };

        private decimal GetProfitNormalizer(string period) => period.ToLower() switch
        {
            "today" => 10000m,     // 10K for daily
            "week" => 50000m,      // 50K for weekly
            "month" => 200000m,    // ✅ 200K untuk monthly (turun dari 300K)  
            "year" => 2000000m,    // ✅ 2M untuk yearly (turun dari 3M)
            _ => 100000m           // 100K default
        };

        private decimal GetMaxPossibleScore(string period) => period.ToLower() switch
        {
            "today" => 50m,        
            "week" => 100m,        
            "month" => 100m,       // ✅ Turun dari 120m ke 100m
            "year" => 150m,        // ✅ Turun dari 200m ke 150m
            _ => 100m
        };

        // ✅ FIXED: Lebih balanced performance categories
        private static string GetTopProductPerformanceCategory(decimal normalizedScore)
        {
            return normalizedScore switch
            {
                >= 70 => "Superstar",         // 70%+
                >= 50 => "Top Performer",     // 50-69%  
                >= 30 => "Strong Seller",     // 30-49%
                >= 15 => "Average Performer", // 15-29%
                >= 5 => "Underperformer",     // 5-14%
                _ => "Low Performer"          // 0-4%
            };
        }

        private static string GetPerformanceBadgeColor(string category)
        {
            return category switch
            {
                "Superstar" => "gold",
                "Top Performer" => "success", 
                "Strong Seller" => "primary",
                "Average Performer" => "info",
                "Underperformer" => "warning",
                "Low Performer" => "danger",
                _ => "secondary"
            };
        }

        // Existing methods
        private static decimal CalculatePerformanceScore(int quantitySold, decimal revenue, decimal profit, int daysWithoutSale, int currentStock)
        {
            decimal score = 0;
            
            // Quantity sold weight (40%)
            if (quantitySold == 0) score += 40;
            else if (quantitySold <= 5) score += 30;
            else if (quantitySold <= 10) score += 20;
            else if (quantitySold <= 20) score += 10;
            
            // Days without sale weight (30%)
            if (daysWithoutSale >= 30) score += 30;
            else if (daysWithoutSale >= 14) score += 20;
            else if (daysWithoutSale >= 7) score += 10;
            
            // Stock vs sales ratio weight (20%)
            if (currentStock > 0 && quantitySold > 0)
            {
                var stockTurnover = (decimal)quantitySold / currentStock;
                if (stockTurnover < 0.1m) score += 20; // Very slow turnover
                else if (stockTurnover < 0.3m) score += 15;
                else if (stockTurnover < 0.5m) score += 10;
            }
            else if (currentStock > 10 && quantitySold == 0)
            {
                score += 20; // High stock, no sales
            }
            
            // Profit margin weight (10%)
            if (profit <= 0 && quantitySold > 0) score += 10; // Sold but no profit
            
            return score;
        }

        private static string GetWorstPerformanceCategory(decimal score)
        {
            return score switch
            {
                >= 90 => "Never Sold",           // Score: 90-100 - Produk tidak pernah terjual
                >= 70 => "Very Slow",           // Score: 70-89  - Stock tinggi, penjualan sangat rendah  
                >= 50 => "Slow Moving",         // Score: 50-69  - Jarang terjual, stock menumpuk
                >= 30 => "Low Profit",          // Score: 30-49  - Terjual tapi margin rendah
                >= 10 => "Declining",           // Score: 10-29  - Penjualan menurun drastis
                _ => "Good Performance"         // Score: 0-9    - Performance masih acceptable
            };
        }

        private string DeterminePeriodFromDateRange(DateTime start, DateTime end)
        {
            var duration = end - start;
            return duration.Days switch
            {
                <= 1 => "today",
                <= 7 => "week", 
                <= 31 => "month",
                <= 365 => "year",
                _ => "custom"
            };
        }

        // Helper method
        private static int GetWeekOfYear(DateTime date)
        {
            var jan1 = new DateTime(date.Year, 1, 1);
            var daysOffset = (int)jan1.DayOfWeek;
            var firstWeekDay = jan1.AddDays(-daysOffset);
            var weekNum = ((date - firstWeekDay).Days / 7) + 1;
            return weekNum;
        }

        // ✅ FIXED: Real Sales Report Export Implementation
        public async Task<ReportExportDto> ExportSalesReportAsync(DateTime startDate, DateTime endDate, string format)
        {
            try
            {
                // 1. Generate the report data
                var salesReport = await GenerateSalesReportAsync(startDate, endDate);
                
                // 2. Create directory if not exists
                var exportDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "exports", "sales");
                if (!Directory.Exists(exportDir))
                {
                    Directory.CreateDirectory(exportDir);
                }

                // 3. Generate filename
                var timestamp = _timezoneService.Now.ToString("yyyyMMdd_HHmmss");
                var fileName = $"sales-report_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}_{timestamp}";
                var fileExtension = format.ToUpper() == "PDF" ? "pdf" : "xlsx";
                var fullFileName = $"{fileName}.{fileExtension}";
                var filePath = Path.Combine(exportDir, fullFileName);
                var webPath = $"/exports/sales/{fullFileName}";

                // 4. Generate file based on format
                if (format.ToUpper() == "PDF")
                {
                    await GenerateSalesReportPdfAsync(salesReport, filePath);
                }
                else if (format.ToUpper() == "EXCEL")
                {
                    await GenerateSalesReportExcelAsync(salesReport, filePath);
                }

                // 5. Return export info
                return new ReportExportDto
                {
                    ReportType = "Sales",
                    Format = format,
                    StartDate = startDate,
                    EndDate = endDate,
                    FilePath = webPath, // ✅ Real web path for download
                    GeneratedAt = _timezoneService.Now
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting sales report");
                throw;
            }
        }

        // ✅ FIXED: Real Inventory Report Export Implementation
        public async Task<ReportExportDto> ExportInventoryReportAsync(string format)
        {
            try
            {
                // 1. Generate the report data
                var inventoryReport = await GenerateInventoryReportAsync();
                
                // 2. Create directory if not exists
                var exportDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "exports", "inventory");
                if (!Directory.Exists(exportDir))
                {
                    Directory.CreateDirectory(exportDir);
                }

                // 3. Generate filename
                var timestamp = _timezoneService.Now.ToString("yyyyMMdd_HHmmss");
                var fileName = $"inventory-report_{timestamp}";
                var fileExtension = format.ToUpper() == "PDF" ? "pdf" : "xlsx";
                var fullFileName = $"{fileName}.{fileExtension}";
                var filePath = Path.Combine(exportDir, fullFileName);
                var webPath = $"/exports/inventory/{fullFileName}";

                // 4. Generate file based on format
                if (format.ToUpper() == "PDF")
                {
                    await GenerateInventoryReportPdfAsync(inventoryReport, filePath);
                }
                else if (format.ToUpper() == "EXCEL")
                {
                    await GenerateInventoryReportExcelAsync(inventoryReport, filePath);
                }

                // 5. Return export info
                return new ReportExportDto
                {
                    ReportType = "Inventory",
                    Format = format,
                    StartDate = _timezoneService.Today,
                    EndDate = _timezoneService.Today,
                    FilePath = webPath, // ✅ Real web path for download
                    GeneratedAt = _timezoneService.Now
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting inventory report");
                throw;
            }
        }

        // ✅ FIXED: Real Financial Report Export Implementation
        public async Task<ReportExportDto> ExportFinancialReportAsync(DateTime startDate, DateTime endDate, string format)
        {
            try
            {
                // 1. Generate the report data
                var financialReport = await GenerateFinancialReportAsync(startDate, endDate);
                
                // 2. Create directory if not exists
                var exportDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "exports", "financial");
                if (!Directory.Exists(exportDir))
                {
                    Directory.CreateDirectory(exportDir);
                }

                // 3. Generate filename
                var timestamp = _timezoneService.Now.ToString("yyyyMMdd_HHmmss");
                var fileName = $"financial-report_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}_{timestamp}";
                var fileExtension = format.ToUpper() == "PDF" ? "pdf" : "xlsx";
                var fullFileName = $"{fileName}.{fileExtension}";
                var filePath = Path.Combine(exportDir, fullFileName);
                var webPath = $"/exports/financial/{fullFileName}";

                // 4. Generate file based on format
                if (format.ToUpper() == "PDF")
                {
                    await GenerateFinancialReportPdfAsync(financialReport, filePath);
                }
                else if (format.ToUpper() == "EXCEL")
                {
                    await GenerateFinancialReportExcelAsync(financialReport, filePath);
                }

                // 5. Return export info
                return new ReportExportDto
                {
                    ReportType = "Financial",
                    Format = format,
                    StartDate = startDate,
                    EndDate = endDate,
                    FilePath = webPath, // ✅ Real web path for download
                    GeneratedAt = _timezoneService.Now
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting financial report");
                throw;
            }
        }

        // ✅ FIXED: Real Customer Report Export Implementation
        public async Task<ReportExportDto> ExportCustomerReportAsync(DateTime startDate, DateTime endDate, string format)
        {
            try
            {
                // 1. Generate the report data
                var customerReport = await GenerateCustomerReportAsync(startDate, endDate);
                
                // 2. Create directory if not exists
                var exportDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "exports", "customer");
                if (!Directory.Exists(exportDir))
                {
                    Directory.CreateDirectory(exportDir);
                }

                // 3. Generate filename
                var timestamp = _timezoneService.Now.ToString("yyyyMMdd_HHmmss");
                var fileName = $"customer-report_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}_{timestamp}";
                var fileExtension = format.ToUpper() == "PDF" ? "pdf" : "xlsx";
                var fullFileName = $"{fileName}.{fileExtension}";
                var filePath = Path.Combine(exportDir, fullFileName);
                var webPath = $"/exports/customer/{fullFileName}";

                // 4. Generate file based on format
                if (format.ToUpper() == "PDF")
                {
                    await GenerateCustomerReportPdfAsync(customerReport, filePath);
                }
                else if (format.ToUpper() == "EXCEL")
                {
                    await GenerateCustomerReportExcelAsync(customerReport, filePath);
                }

                // 5. Return export info
                return new ReportExportDto
                {
                    ReportType = "Customer",
                    Format = format,
                    StartDate = startDate,
                    EndDate = endDate,
                    FilePath = webPath, // ✅ Real web path for download
                    GeneratedAt = _timezoneService.Now
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting customer report");
                throw;
            }
        }

        // ✅ PDF Generation Methods
        private async Task GenerateSalesReportPdfAsync(SalesReportDto report, string filePath)
        {
            var html = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <title>Sales Report</title>
                <style>
                    body {{ font-family: Arial, sans-serif; margin: 20px; }}
                    .header {{ text-align: center; margin-bottom: 30px; }}
                    .summary {{ background-color: #f5f5f5; padding: 15px; margin-bottom: 20px; }}
                    table {{ width: 100%; border-collapse: collapse; margin-bottom: 20px; }}
                    th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
                    th {{ background-color: #4CAF50; color: white; }}
                    .currency {{ text-align: right; }}
                </style>
            </head>
            <body>
                <div class='header'>
                    <h1>Toko Eniwan - Sales Report</h1>
                    <p>Period: {report.StartDate:dd/MM/yyyy} - {report.EndDate:dd/MM/yyyy}</p>
                    <p>Generated: {report.GeneratedAt:dd/MM/yyyy HH:mm:ss}</p>
                </div>
                
                <div class='summary'>
                    <h3>Summary</h3>
                    <p><strong>Total Revenue:</strong> Rp {report.TotalRevenue:N0}</p>
                    <p><strong>Total Transactions:</strong> {report.TotalTransactions:N0}</p>
                    <p><strong>Total Items Sold:</strong> {report.TotalItemsSold:N0}</p>
                    <p><strong>Average Transaction Value:</strong> Rp {report.AverageTransactionValue:N0}</p>
                    <p><strong>Total Profit:</strong> Rp {report.TotalProfit:N0}</p>
                </div>

                <h3>Payment Method Breakdown</h3>
                <table>
                    <thead>
                        <tr>
                            <th>Payment Method</th>
                            <th>Total</th>
                            <th>Transactions</th>
                            <th>Percentage</th>
                        </tr>
                    </thead>
                    <tbody>
                        {string.Join("", report.PaymentMethodBreakdown.Select(pm => $@"
                        <tr>
                            <td>{pm.PaymentMethod}</td>
                            <td class='currency'>Rp {pm.Total:N0}</td>
                            <td>{pm.TransactionCount:N0}</td>
                            <td>{pm.Percentage:F1}%</td>
                        </tr>"))}
                    </tbody>
                </table>
            </body>
            </html>";

            // Write HTML file (for development, can be replaced with actual PDF library)
            await File.WriteAllTextAsync(filePath, html);
}

private async Task GenerateInventoryReportPdfAsync(InventoryReportDto report, string filePath)
{
    var html = $@"
    <!DOCTYPE html>
    <html>
    <head>
        <meta charset='utf-8'>
        <title>Inventory Report</title>
        <style>
            body {{ font-family: Arial, sans-serif; margin: 20px; }}
            .header {{ text-align: center; margin-bottom: 30px; }}
            .summary {{ background-color: #f5f5f5; padding: 15px; margin-bottom: 20px; }}
            table {{ width: 100%; border-collapse: collapse; margin-bottom: 20px; }}
            th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
            th {{ background-color: #2196F3; color: white; }}
            .currency {{ text-align: right; }}
            .warning {{ color: #ff9800; font-weight: bold; }}
            .danger {{ color: #f44336; font-weight: bold; }}
        </style>
    </head>
    <body>
        <div class='header'>
            <h1>Toko Eniwan - Inventory Report</h1>
            <p>Generated: {report.GeneratedAt:dd/MM/yyyy HH:mm:ss}</p>
        </div>
        
        <div class='summary'>
            <h3>Inventory Summary</h3>
            <p><strong>Total Products:</strong> {report.TotalProducts:N0}</p>
            <p><strong>Total Inventory Value:</strong> Rp {report.TotalInventoryValue:N0}</p>
            <p><strong>Low Stock Products:</strong> <span class='warning'>{report.LowStockProducts:N0}</span></p>
            <p><strong>Out of Stock Products:</strong> <span class='danger'>{report.OutOfStockProducts:N0}</span></p>
        </div>

        <h3>Category Breakdown</h3>
        <table>
            <thead>
                <tr>
                    <th>Category</th>
                    <th>Product Count</th>
                    <th>Total Value</th>
                    <th>Low Stock Count</th>
                </tr>
            </thead>
            <tbody>
                {string.Join("", report.CategoryBreakdown.Select(cat => $@"
                <tr>
                    <td>{cat.CategoryName}</td>
                    <td>{cat.ProductCount:N0}</td>
                    <td class='currency'>Rp {cat.TotalValue:N0}</td>
                    <td class='{(cat.LowStockCount > 0 ? "warning" : "")}'>{cat.LowStockCount:N0}</td>
                </tr>"))}
            </tbody>
        </table>
    </body>
    </html>";

    await File.WriteAllTextAsync(filePath, html);
}

private async Task GenerateFinancialReportPdfAsync(FinancialReportDto report, string filePath)
{
    var html = $@"
    <!DOCTYPE html>
    <html>
    <head>
        <meta charset='utf-8'>
        <title>Financial Report</title>
        <style>
            body {{ font-family: Arial, sans-serif; margin: 20px; }}
            .header {{ text-align: center; margin-bottom: 30px; }}
            .summary {{ background-color: #f5f5f5; padding: 15px; margin-bottom: 20px; }}
            table {{ width: 100%; border-collapse: collapse; margin-bottom: 20px; }}
            th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
            th {{ background-color: #4CAF50; color: white; }}
            .currency {{ text-align: right; }}
            .profit {{ color: #4CAF50; font-weight: bold; }}
            .loss {{ color: #f44336; font-weight: bold; }}
        </style>
    </head>
    <body>
        <div class='header'>
            <h1>Toko Eniwan - Financial Report</h1>
            <p>Period: {report.StartDate:dd/MM/yyyy} - {report.EndDate:dd/MM/yyyy}</p>
            <p>Generated: {report.GeneratedAt:dd/MM/yyyy HH:mm:ss}</p>
        </div>
        
        <div class='summary'>
            <h3>Financial Summary</h3>
            <p><strong>Total Revenue:</strong> Rp {report.TotalRevenue:N0}</p>
            <p><strong>Total Cost:</strong> Rp {report.TotalCost:N0}</p>
            <p><strong>Gross Profit:</strong> <span class='{(report.GrossProfit >= 0 ? "profit" : "loss")}'>Rp {report.GrossProfit:N0}</span></p>
            <p><strong>Gross Profit Margin:</strong> {report.GrossProfitMargin:F1}%</p>
            <p><strong>Net Profit:</strong> <span class='{(report.NetProfit >= 0 ? "profit" : "loss")}'>Rp {report.NetProfit:N0}</span></p>
        </div>

        <h3>Monthly Breakdown</h3>
        <table>
            <thead>
                <tr>
                    <th>Month</th>
                    <th>Revenue</th>
                    <th>Cost</th>
                    <th>Profit</th>
                </tr>
            </thead>
            <tbody>
                {string.Join("", report.MonthlyBreakdown.Select(month => $@"
                <tr>
                    <td>{month.MonthName} {month.Year}</td>
                    <td class='currency'>Rp {month.Revenue:N0}</td>
                    <td class='currency'>Rp {month.Cost:N0}</td>
                    <td class='currency {(month.Profit >= 0 ? "profit" : "loss")}'>Rp {month.Profit:N0}</td>
                </tr>"))}
            </tbody>
        </table>
    </body>
    </html>";

    await File.WriteAllTextAsync(filePath, html);
}

private async Task GenerateCustomerReportPdfAsync(CustomerReportDto report, string filePath)
{
    var html = $@"
    <!DOCTYPE html>
    <html>
    <head>
        <meta charset='utf-8'>
        <title>Customer Report</title>
        <style>
            body {{ font-family: Arial, sans-serif; margin: 20px; }}
            .header {{ text-align: center; margin-bottom: 30px; }}
            .summary {{ background-color: #f5f5f5; padding: 15px; margin-bottom: 20px; }}
            table {{ width: 100%; border-collapse: collapse; margin-bottom: 20px; }}
            th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
            th {{ background-color: #9C27B0; color: white; }}
            .currency {{ text-align: right; }}
        </style>
    </head>
    <body>
        <div class='header'>
            <h1>Toko Eniwan - Customer Report</h1>
            <p>Period: {report.StartDate:dd/MM/yyyy} - {report.EndDate:dd/MM/yyyy}</p>
            <p>Generated: {report.GeneratedAt:dd/MM/yyyy HH:mm:ss}</p>
        </div>
        
        <div class='summary'>
            <h3>Customer Summary</h3>
            <p><strong>Total Active Members:</strong> {report.TotalActiveMembers:N0}</p>
            <p><strong>New Members This Period:</strong> {report.NewMembersThisPeriod:N0}</p>
            <p><strong>Average Order Value:</strong> Rp {report.AverageOrderValue:N0}</p>
            <p><strong>Total Member Revenue:</strong> Rp {report.TotalMemberRevenue:N0}</p>
            <p><strong>Guest Revenue:</strong> Rp {report.GuestRevenue:N0}</p>
        </div>

        <h3>Top Customers</h3>
        <table>
            <thead>
                <tr>
                    <th>Customer Name</th>
                    <th>Type</th>
                    <th>Total Spent</th>
                    <th>Transactions</th>
                    <th>Avg Order</th>
                    <th>Last Purchase</th>
                </tr>
            </thead>
            <tbody>
                {string.Join("", report.TopCustomers.Take(10).Select(customer => $@"
                <tr>
                    <td>{customer.CustomerName}</td>
                    <td>{customer.MembershipType}</td>
                    <td class='currency'>Rp {customer.TotalSpent:N0}</td>
                    <td>{customer.TransactionCount:N0}</td>
                    <td class='currency'>Rp {customer.AverageOrderValue:N0}</td>
                    <td>{customer.LastPurchase:dd/MM/yyyy}</td>
                </tr>"))}
            </tbody>
        </table>
    </body>
    </html>";

    await File.WriteAllTextAsync(filePath, html);
}

        // ✅ IMPLEMENTATION: Excel Generation Methods (using CSV format for simplicity)
        private async Task GenerateSalesReportExcelAsync(SalesReportDto report, string filePath)
        {
            var csv = new StringBuilder();
            csv.AppendLine("Toko Eniwan - Sales Report");
            csv.AppendLine($"Period: {report.StartDate:dd/MM/yyyy} - {report.EndDate:dd/MM/yyyy}");
            csv.AppendLine($"Generated: {report.GeneratedAt:dd/MM/yyyy HH:mm:ss}");
            csv.AppendLine();
            
            csv.AppendLine("SUMMARY");
            csv.AppendLine($"Total Revenue,Rp {report.TotalRevenue:N0}");
            csv.AppendLine($"Total Transactions,{report.TotalTransactions:N0}");
            csv.AppendLine($"Total Items Sold,{report.TotalItemsSold:N0}");
            csv.AppendLine($"Average Transaction Value,Rp {report.AverageTransactionValue:N0}");
            csv.AppendLine($"Total Profit,Rp {report.TotalProfit:N0}");
            csv.AppendLine();
            
            csv.AppendLine("PAYMENT METHOD BREAKDOWN");
            csv.AppendLine("Payment Method,Total,Transactions,Percentage");
            foreach (var pm in report.PaymentMethodBreakdown)
            {
                csv.AppendLine($"{pm.PaymentMethod},Rp {pm.Total:N0},{pm.TransactionCount:N0},{pm.Percentage:F1}%");
            }

    await File.WriteAllTextAsync(filePath, csv.ToString());
}

private async Task GenerateInventoryReportExcelAsync(InventoryReportDto report, string filePath)
{
    var csv = new StringBuilder();
    csv.AppendLine("Toko Eniwan - Inventory Report");
    csv.AppendLine($"Generated: {report.GeneratedAt:dd/MM/yyyy HH:mm:ss}");
    csv.AppendLine();
    
    csv.AppendLine("INVENTORY SUMMARY");
    csv.AppendLine($"Total Products,{report.TotalProducts:N0}");
    csv.AppendLine($"Total Inventory Value,Rp {report.TotalInventoryValue:N0}");
    csv.AppendLine($"Low Stock Products,{report.LowStockProducts:N0}");
    csv.AppendLine($"Out of Stock Products,{report.OutOfStockProducts:N0}");
    csv.AppendLine();
    
    csv.AppendLine("CATEGORY BREAKDOWN");
    csv.AppendLine("Category,Product Count,Total Value,Low Stock Count");
    foreach (var cat in report.CategoryBreakdown)
    {
        csv.AppendLine($"{cat.CategoryName},{cat.ProductCount:N0},Rp {cat.TotalValue:N0},{cat.LowStockCount:N0}");
    }

    await File.WriteAllTextAsync(filePath, csv.ToString());
}

private async Task GenerateFinancialReportExcelAsync(FinancialReportDto report, string filePath)
{
    var csv = new StringBuilder();
    csv.AppendLine("Toko Eniwan - Financial Report");
    csv.AppendLine($"Period: {report.StartDate:dd/MM/yyyy} - {report.EndDate:dd/MM/yyyy}");
    csv.AppendLine($"Generated: {report.GeneratedAt:dd/MM/yyyy HH:mm:ss}");
    csv.AppendLine();
    
    csv.AppendLine("FINANCIAL SUMMARY");
    csv.AppendLine($"Total Revenue,Rp {report.TotalRevenue:N0}");
    csv.AppendLine($"Total Cost,Rp {report.TotalCost:N0}");
    csv.AppendLine($"Gross Profit,Rp {report.GrossProfit:N0}");
    csv.AppendLine($"Gross Profit Margin,{report.GrossProfitMargin:F1}%");
    csv.AppendLine($"Net Profit,Rp {report.NetProfit:N0}");
    csv.AppendLine();
    
    csv.AppendLine("MONTHLY BREAKDOWN");
    csv.AppendLine("Month,Year,Revenue,Cost,Profit");
    foreach (var month in report.MonthlyBreakdown)
    {
        csv.AppendLine($"{month.MonthName},{month.Year},Rp {month.Revenue:N0},Rp {month.Cost:N0},Rp {month.Profit:N0}");
    }

    await File.WriteAllTextAsync(filePath, csv.ToString());
}

private async Task GenerateCustomerReportExcelAsync(CustomerReportDto report, string filePath)
{
    var csv = new StringBuilder();
    csv.AppendLine("Toko Eniwan - Customer Report");
    csv.AppendLine($"Period: {report.StartDate:dd/MM/yyyy} - {report.EndDate:dd/MM/yyyy}");
    csv.AppendLine($"Generated: {report.GeneratedAt:dd/MM/yyyy HH:mm:ss}");
    csv.AppendLine();
    
    csv.AppendLine("CUSTOMER SUMMARY");
    csv.AppendLine($"Total Active Members,{report.TotalActiveMembers:N0}");
    csv.AppendLine($"New Members This Period,{report.NewMembersThisPeriod:N0}");
    csv.AppendLine($"Average Order Value,Rp {report.AverageOrderValue:N0}");
    csv.AppendLine($"Total Member Revenue,Rp {report.TotalMemberRevenue:N0}");
    csv.AppendLine($"Guest Revenue,Rp {report.GuestRevenue:N0}");
    csv.AppendLine();
    
    csv.AppendLine("TOP CUSTOMERS");
    csv.AppendLine("Customer Name,Type,Total Spent,Transactions,Avg Order,Last Purchase");
    foreach (var customer in report.TopCustomers.Take(10))
    {
        csv.AppendLine($"{customer.CustomerName},{customer.MembershipType},Rp {customer.TotalSpent:N0},{customer.TransactionCount:N0},Rp {customer.AverageOrderValue:N0},{customer.LastPurchase:dd/MM/yyyy}");
    }

    await File.WriteAllTextAsync(filePath, csv.ToString());
}
    }
}