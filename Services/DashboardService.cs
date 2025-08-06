// Services/DashboardService.cs - Updated with Timezone Support
using Berca_Backend.DTOs;
using Berca_Backend.Models;
using Berca_Backend.Data;
using Microsoft.EntityFrameworkCore;
using Berca_Backend.Extensions;
using Berca_Backend.Services.Interfaces;

namespace Berca_Backend.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DashboardService> _logger;
        private readonly ITimezoneService _timezoneService; // ✅ ADDED

        // ✅ Scoring configuration
        private readonly ScoringConfig _scoringConfig = new()
        {
            MaxScore = 100m,
            EnableDailyReset = true,
            LastResetDate = DateTime.UtcNow.Date, // Keep UTC for storage
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
            _timezoneService = timezoneService; // ✅ ADDED
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
                // ✅ Use Indonesia timezone
                var today = DateTimeExtensions.IndonesiaToday;
                var monthStart = new DateTime(today.Year, today.Month, 1);
                var yearStart = new DateTime(today.Year, 1, 1);

                // Convert to UTC for database queries
                var todayUtc = today.ToUtcFromIndonesia();
                var tomorrowUtc = today.AddDays(1).ToUtcFromIndonesia();
                var monthStartUtc = monthStart.ToUtcFromIndonesia();
                var yearStartUtc = yearStart.ToUtcFromIndonesia();

                // Today's sales (use UTC range for database)
                var todaySales = await _context.Sales
                    .Where(s => s.SaleDate >= todayUtc && s.SaleDate < tomorrowUtc && s.Status == SaleStatus.Completed)
                    .ToListAsync();

                // Monthly sales
                var monthlySales = await _context.Sales
                    .Where(s => s.SaleDate >= monthStartUtc && s.Status == SaleStatus.Completed)
                    .ToListAsync();

                // Yearly sales
                var yearlySales = await _context.Sales
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

                return new DashboardKPIDto
                {
                    TodayRevenue = todaySales.Sum(s => s.Total),
                    MonthlyRevenue = monthlySales.Sum(s => s.Total),
                    YearlyRevenue = yearlySales.Sum(s => s.Total),
                    TodayTransactions = todaySales.Count,
                    MonthlyTransactions = monthlySales.Count,
                    AverageTransactionValue = monthlySales.Any() ? monthlySales.Average(s => s.Total) : 0,
                    TotalProfit = monthlySales.Sum(s => s.TotalProfit),
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
                    .Include(s => s.SaleItems)
                    .Where(s => s.SaleDate >= startDate && s.SaleDate <= endDate && s.Status == SaleStatus.Completed)
                    .ToListAsync();

                var totalRevenue = sales.Sum(s => s.Total);
                var totalTransactions = sales.Count;
                var totalItemsSold = sales.Sum(s => s.TotalItems);
                var averageTransaction = totalTransactions > 0 ? totalRevenue / totalTransactions : 0;

                var paymentMethods = sales
                    .GroupBy(s => s.PaymentMethod)
                    .Select(g => new PaymentMethodSummaryDto
                    {
                        PaymentMethod = g.Key,
                        Total = g.Sum(s => s.Total),
                        TransactionCount = g.Count(),
                        Percentage = totalRevenue > 0 ? (g.Sum(s => s.Total) / totalRevenue) * 100 : 0
                    })
                    .ToList();

                return new SalesReportDto
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    TotalRevenue = totalRevenue,
                    TotalTransactions = totalTransactions,
                    TotalItemsSold = totalItemsSold,
                    AverageTransactionValue = averageTransaction,
                    PaymentMethodBreakdown = paymentMethods,
                    GeneratedAt = DateTime.UtcNow
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

        // Export methods (for now return placeholder)
        public async Task<ReportExportDto> ExportSalesReportAsync(DateTime startDate, DateTime endDate, string format)
        {
            await Task.Delay(1); // Simulate async work
            
            return new ReportExportDto
            {
                ReportType = "Sales",
                Format = format,
                StartDate = startDate,
                EndDate = endDate,
                FilePath = $"/exports/sales-report-{startDate:yyyy-MM-dd}-{endDate:yyyy-MM-dd}.{format.ToLower()}",
                GeneratedAt = DateTime.UtcNow
            };
        }

        public async Task<ReportExportDto> ExportInventoryReportAsync(string format)
        {
            await Task.Delay(1);
            
            return new ReportExportDto
            {
                ReportType = "Inventory",
                Format = format,
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date,
                FilePath = $"/exports/inventory-report-{DateTime.UtcNow:yyyy-MM-dd}.{format.ToLower()}",
                GeneratedAt = DateTime.UtcNow
            };
        }

        public async Task<ReportExportDto> ExportFinancialReportAsync(DateTime startDate, DateTime endDate, string format)
        {
            await Task.Delay(1);
            
            return new ReportExportDto
            {
                ReportType = "Financial",
                Format = format,
                StartDate = startDate,
                EndDate = endDate,
                FilePath = $"/exports/financial-report-{startDate:yyyy-MM-dd}-{endDate:yyyy-MM-dd}.{format.ToLower()}",
                GeneratedAt = DateTime.UtcNow
            };
        }

        public async Task<ReportExportDto> ExportCustomerReportAsync(DateTime startDate, DateTime endDate, string format)
        {
            await Task.Delay(1);
            
            return new ReportExportDto
            {
                ReportType = "Customer",
                Format = format,
                StartDate = startDate,
                EndDate = endDate,
                FilePath = $"/exports/customer-report-{startDate:yyyy-MM-dd}-{endDate:yyyy-MM-dd}.{format.ToLower()}",
                GeneratedAt = DateTime.UtcNow
            };
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
    }
}