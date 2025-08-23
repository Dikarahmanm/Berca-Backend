using Berca_Backend.Data;
using Berca_Backend.DTOs;
using Berca_Backend.Models;
using Berca_Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;

namespace Berca_Backend.Services
{
    /// <summary>
    /// Business Intelligence service implementation
    /// Advanced analytics for Indonesian POS business context
    /// </summary>
    public class BusinessIntelligenceService : IBusinessIntelligenceService
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<BusinessIntelligenceService> _logger;
        private readonly IConfiguration _configuration;
        private static readonly CultureInfo IdCulture = new("id-ID");

        public BusinessIntelligenceService(
            AppDbContext context,
            IMemoryCache cache,
            ILogger<BusinessIntelligenceService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
            _configuration = configuration;
        }

        // ==================== SALES ANALYTICS ==================== //

        public async Task<object> GetSalesTrendsAsync(DateTime startDate, DateTime endDate, int? branchId = null)
        {
            try
            {
                var cacheKey = $"sales_trends_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}_{branchId}";
                
                if (_cache.TryGetValue(cacheKey, out object? cachedResult) && cachedResult != null)
                {
                    return cachedResult;
                }

                var sales = await _context.Sales
                    .Where(s => s.SaleDate >= startDate && s.SaleDate <= endDate)
                    .ToListAsync();

                // Daily trends
                var dailyTrends = sales
                    .GroupBy(s => s.SaleDate.Date)
                    .Select(g => new
                    {
                        Date = g.Key.ToString("yyyy-MM-dd"),
                        DateDisplay = g.Key.ToString("dd/MM/yyyy", IdCulture),
                        Sales = g.Sum(s => s.Total),
                        Transactions = g.Count(),
                        AverageTransaction = g.Any() ? g.Average(s => s.Total) : 0,
                        GrowthRate = CalculateGrowthRate(g.Sum(s => s.Total), sales, g.Key)
                    })
                    .OrderBy(t => t.Date)
                    .ToList();

                // Weekly trends
                var weeklyTrends = sales
                    .GroupBy(s => GetWeekOfYear(s.SaleDate))
                    .Select(g => new
                    {
                        Week = g.Key,
                        Sales = g.Sum(s => s.Total),
                        Transactions = g.Count(),
                        AverageTransaction = g.Any() ? g.Average(s => s.Total) : 0
                    })
                    .OrderBy(t => t.Week)
                    .ToList();

                // Monthly trends
                var monthlyTrends = sales
                    .GroupBy(s => new { s.SaleDate.Year, s.SaleDate.Month })
                    .Select(g => new
                    {
                        Period = $"{g.Key.Year}-{g.Key.Month:00}",
                        PeriodDisplay = $"{GetMonthName(g.Key.Month)} {g.Key.Year}",
                        Sales = g.Sum(s => s.Total),
                        Transactions = g.Count(),
                        AverageTransaction = g.Any() ? g.Average(s => s.Total) : 0
                    })
                    .OrderBy(t => t.Period)
                    .ToList();

                var result = new
                {
                    Summary = new
                    {
                        TotalSales = sales.Sum(s => s.Total),
                        TotalTransactions = sales.Count,
                        AverageTransaction = sales.Any() ? sales.Average(s => s.Total) : 0,
                        PeriodDays = (endDate - startDate).Days + 1,
                        DailyAverage = sales.Any() ? sales.Sum(s => s.Total) / ((endDate - startDate).Days + 1) : 0
                    },
                    Trends = new
                    {
                        Daily = dailyTrends,
                        Weekly = weeklyTrends,
                        Monthly = monthlyTrends
                    },
                    Insights = GenerateSalesInsights(sales, startDate, endDate)
                };

                // Cache for 1 hour
                _cache.Set(cacheKey, result, TimeSpan.FromHours(1));

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales trends");
                return new { Error = "Gagal mengambil data tren penjualan" };
            }
        }

        public async Task<object> PredictSalesAsync(int forecastDays = 30, int? branchId = null)
        {
            try
            {
                // Get historical data for prediction (last 90 days)
                var historicalDate = DateTime.Today.AddDays(-90);
                var sales = await _context.Sales
                    .Where(s => s.SaleDate >= historicalDate)
                    .ToListAsync();

                var dailySales = sales
                    .GroupBy(s => s.SaleDate.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Sales = g.Sum(s => s.Total)
                    })
                    .OrderBy(s => s.Date)
                    .ToList();

                // Simple linear trend prediction
                var predictions = new List<object>();
                var averageDailySales = dailySales.Any() ? dailySales.Average(d => d.Sales) : 0;
                var trendSlope = CalculateTrendSlope(dailySales.Cast<object>().ToList());

                for (int i = 1; i <= forecastDays; i++)
                {
                    var predictedDate = DateTime.Today.AddDays(i);
                    var predictedSales = averageDailySales + (trendSlope * i);
                    
                    // Apply weekly seasonality (simple)
                    var seasonalMultiplier = GetSeasonalMultiplier(predictedDate.DayOfWeek);
                    predictedSales *= seasonalMultiplier;

                    predictions.Add(new
                    {
                        Date = predictedDate.ToString("yyyy-MM-dd"),
                        DateDisplay = predictedDate.ToString("dd/MM/yyyy", IdCulture),
                        PredictedSales = Math.Max(0, predictedSales),
                        PredictedSalesDisplay = Math.Max(0, predictedSales).ToString("C0", IdCulture),
                        Confidence = CalculateConfidence(i, forecastDays)
                    });
                }

                return new
                {
                    ForecastPeriod = $"{forecastDays} hari ke depan",
                    BasedOnDays = dailySales.Count,
                    AverageHistoricalSales = averageDailySales,
                    AverageHistoricalSalesDisplay = averageDailySales.ToString("C0", IdCulture),
                    TrendDirection = trendSlope > 0 ? "Naik" : trendSlope < 0 ? "Turun" : "Stabil",
                    Predictions = predictions,
                    TotalPredictedSales = predictions.Sum(p => (decimal)p.GetType().GetProperty("PredictedSales")!.GetValue(p)!),
                    Disclaimer = "Prediksi berdasarkan data historis dan dapat berubah sesuai kondisi pasar"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error predicting sales");
                return new { Error = "Gagal memprediksi penjualan" };
            }
        }

        public async Task<object> GetSeasonalPatternsAsync(int? branchId = null)
        {
            try
            {
                // Get data for last 12 months
                var startDate = DateTime.Today.AddMonths(-12);
                var sales = await _context.Sales
                    .Where(s => s.SaleDate >= startDate)
                    .ToListAsync();

                // Monthly patterns
                var monthlyPatterns = sales
                    .GroupBy(s => s.SaleDate.Month)
                    .Select(g => new
                    {
                        Month = g.Key,
                        MonthName = GetMonthName(g.Key),
                        AverageSales = g.Average(s => s.Total),
                        TotalSales = g.Sum(s => s.Total),
                        TransactionCount = g.Count(),
                        RelativeIndex = 0.0 // Will be calculated below
                    })
                    .OrderBy(p => p.Month)
                    .ToList();

                // Calculate relative seasonal index
                var overallAverage = monthlyPatterns.Any() ? monthlyPatterns.Average(p => p.AverageSales) : 0;
                foreach (var pattern in monthlyPatterns)
                {
                    var patternType = pattern.GetType();
                    var relativeIndexProperty = patternType.GetProperty("RelativeIndex");
                    if (relativeIndexProperty != null && overallAverage > 0)
                    {
                        relativeIndexProperty.SetValue(pattern, pattern.AverageSales / overallAverage);
                    }
                }

                // Day of week patterns
                var dayOfWeekPatterns = sales
                    .GroupBy(s => s.SaleDate.DayOfWeek)
                    .Select(g => new
                    {
                        DayOfWeek = g.Key,
                        DayName = GetDayName(g.Key),
                        AverageSales = g.Average(s => s.Total),
                        TotalSales = g.Sum(s => s.Total),
                        TransactionCount = g.Count()
                    })
                    .OrderBy(p => p.DayOfWeek)
                    .ToList();

                // Hour patterns
                var hourPatterns = sales
                    .GroupBy(s => s.SaleDate.Hour)
                    .Select(g => new
                    {
                        Hour = g.Key,
                        HourDisplay = $"{g.Key:00}:00",
                        AverageSales = g.Average(s => s.Total),
                        TotalSales = g.Sum(s => s.Total),
                        TransactionCount = g.Count()
                    })
                    .OrderBy(p => p.Hour)
                    .ToList();

                return new
                {
                    AnalysisPeriod = "12 bulan terakhir",
                    Monthly = monthlyPatterns,
                    DayOfWeek = dayOfWeekPatterns,
                    Hourly = hourPatterns,
                    Insights = new
                    {
                        BestMonth = monthlyPatterns.OrderByDescending(p => p.AverageSales).FirstOrDefault()?.MonthName ?? "Tidak ada data",
                        BestDay = dayOfWeekPatterns.OrderByDescending(p => p.AverageSales).FirstOrDefault()?.DayName ?? "Tidak ada data",
                        PeakHour = hourPatterns.OrderByDescending(p => p.AverageSales).FirstOrDefault()?.HourDisplay ?? "Tidak ada data"
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting seasonal patterns");
                return new { Error = "Gagal mengambil pola musiman" };
            }
        }

        public async Task<object> GetCustomerBehaviorAnalysisAsync(DateTime startDate, DateTime endDate, int? branchId = null)
        {
            try
            {
                var sales = await _context.Sales
                    .Include(s => s.SaleItems)
                    .Where(s => s.SaleDate >= startDate && s.SaleDate <= endDate)
                    .ToListAsync();

                // Transaction analysis
                var transactionAnalysis = new
                {
                    TotalTransactions = sales.Count,
                    AverageTransactionValue = sales.Any() ? sales.Average(s => s.Total) : 0,
                    MedianTransactionValue = CalculateMedian(sales.Select(s => s.Total).ToList()),
                    AverageItemsPerTransaction = sales.Any() ? sales.Average(s => s.SaleItems.Sum(si => si.Quantity)) : 0,
                    LargestTransaction = sales.Any() ? sales.Max(s => s.Total) : 0,
                    SmallestTransaction = sales.Any() ? sales.Min(s => s.Total) : 0
                };

                // Transaction value distribution
                var valueRanges = new[]
                {
                    new { Range = "< 50k", Min = 0m, Max = 50000m },
                    new { Range = "50k - 100k", Min = 50000m, Max = 100000m },
                    new { Range = "100k - 250k", Min = 100000m, Max = 250000m },
                    new { Range = "250k - 500k", Min = 250000m, Max = 500000m },
                    new { Range = "> 500k", Min = 500000m, Max = decimal.MaxValue }
                };

                var distributionAnalysis = valueRanges.Select(range => new
                {
                    Range = range.Range,
                    Count = sales.Count(s => s.Total >= range.Min && s.Total < range.Max),
                    Percentage = sales.Any() ? (double)sales.Count(s => s.Total >= range.Min && s.Total < range.Max) / sales.Count * 100 : 0,
                    TotalValue = sales.Where(s => s.Total >= range.Min && s.Total < range.Max).Sum(s => s.Total)
                }).ToList();

                // Purchase timing analysis
                var timingAnalysis = sales
                    .GroupBy(s => s.SaleDate.Hour)
                    .Select(g => new
                    {
                        Hour = g.Key,
                        HourDisplay = $"{g.Key:00}:00",
                        TransactionCount = g.Count(),
                        AverageValue = g.Average(s => s.Total),
                        Percentage = sales.Any() ? (double)g.Count() / sales.Count * 100 : 0
                    })
                    .OrderBy(t => t.Hour)
                    .ToList();

                return new
                {
                    AnalysisPeriod = $"{startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}",
                    TransactionAnalysis = transactionAnalysis,
                    ValueDistribution = distributionAnalysis,
                    TimingAnalysis = timingAnalysis,
                    Insights = new
                    {
                        PeakHour = timingAnalysis.OrderByDescending(t => t.TransactionCount).FirstOrDefault()?.HourDisplay ?? "Tidak ada data",
                        MostCommonRange = distributionAnalysis.OrderByDescending(d => d.Count).FirstOrDefault()?.Range ?? "Tidak ada data",
                        HighValueTransactionPercentage = distributionAnalysis.Where(d => d.Range.Contains("250k") || d.Range.Contains("500k")).Sum(d => d.Percentage)
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer behavior analysis");
                return new { Error = "Gagal menganalisis perilaku pelanggan" };
            }
        }

        public async Task<object> GetProductPerformanceInsightsAsync(DateTime startDate, DateTime endDate, int? categoryId = null)
        {
            try
            {
                var saleItemsQuery = _context.SaleItems
                    .Include(si => si.Product)
                        .ThenInclude(p => p!.Category)
                    .Include(si => si.Sale)
                    .Where(si => si.Sale.SaleDate >= startDate && si.Sale.SaleDate <= endDate);

                if (categoryId.HasValue)
                {
                    saleItemsQuery = saleItemsQuery.Where(si => si.Product!.CategoryId == categoryId.Value);
                }

                var saleItems = await saleItemsQuery.ToListAsync();

                // Product performance metrics
                var productPerformance = saleItems
                    .Where(si => si.Product != null)
                    .GroupBy(si => si.Product!)
                    .Select(g => new
                    {
                        ProductId = g.Key.Id,
                        ProductName = g.Key.Name,
                        CategoryName = g.Key.Category?.Name ?? "Tanpa Kategori",
                        TotalQuantitySold = g.Sum(si => si.Quantity),
                        TotalRevenue = g.Sum(si => si.Subtotal),
                        AveragePrice = g.Average(si => si.UnitPrice),
                        TransactionCount = g.Select(si => si.SaleId).Distinct().Count(),
                        ProfitMargin = CalculateProfitMargin(g.Key, g.Sum(si => si.Subtotal)),
                        Velocity = CalculateVelocity(g.Sum(si => si.Quantity), startDate, endDate),
                        TrendDirection = "Stabil" // Would need historical comparison
                    })
                    .OrderByDescending(p => p.TotalRevenue)
                    .ToList();

                // Category insights
                var categoryInsights = productPerformance
                    .GroupBy(p => p.CategoryName)
                    .Select(g => new
                    {
                        CategoryName = g.Key,
                        ProductCount = g.Count(),
                        TotalRevenue = g.Sum(p => p.TotalRevenue),
                        AverageRevenuePerProduct = g.Average(p => p.TotalRevenue),
                        TotalQuantity = g.Sum(p => p.TotalQuantitySold),
                        MarketShare = 0.0 // Will be calculated below
                    })
                    .ToList();

                // Calculate market share
                var totalRevenue = categoryInsights.Sum(c => c.TotalRevenue);
                foreach (var category in categoryInsights)
                {
                    var categoryType = category.GetType();
                    var marketShareProperty = categoryType.GetProperty("MarketShare");
                    if (marketShareProperty != null && totalRevenue > 0)
                    {
                        marketShareProperty.SetValue(category, (double)(category.TotalRevenue / totalRevenue * 100));
                    }
                }

                return new
                {
                    AnalysisPeriod = $"{startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}",
                    TopProducts = productPerformance.Take(20).ToList(),
                    CategoryInsights = categoryInsights.OrderByDescending(c => c.TotalRevenue).ToList(),
                    Summary = new
                    {
                        TotalProducts = productPerformance.Count,
                        TotalRevenue = productPerformance.Sum(p => p.TotalRevenue),
                        TotalQuantitySold = productPerformance.Sum(p => p.TotalQuantitySold),
                        AverageRevenuePerProduct = productPerformance.Any() ? productPerformance.Average(p => p.TotalRevenue) : 0
                    },
                    Insights = new
                    {
                        BestSellingProduct = productPerformance.OrderByDescending(p => p.TotalQuantitySold).FirstOrDefault()?.ProductName ?? "Tidak ada data",
                        HighestRevenueProduct = productPerformance.OrderByDescending(p => p.TotalRevenue).FirstOrDefault()?.ProductName ?? "Tidak ada data",
                        TopCategory = categoryInsights.OrderByDescending(c => c.TotalRevenue).FirstOrDefault()?.CategoryName ?? "Tidak ada data"
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product performance insights");
                return new { Error = "Gagal menganalisis performa produk" };
            }
        }

        // ==================== INVENTORY ANALYTICS ==================== //

        public async Task<object> GetInventoryOptimizationAsync(int? branchId = null, int? categoryId = null)
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

                var optimizationAnalysis = products.Select(p => new
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    CategoryName = p.Category?.Name ?? "Tanpa Kategori",
                    CurrentStock = p.Stock,
                    MinimumStock = p.MinimumStock,
                    MaximumStock = p.MinimumStock * 2,
                    CostPrice = p.BuyPrice,
                    SellingPrice = p.SellPrice,
                    StockValue = p.Stock * p.BuyPrice,
                    Status = GetStockOptimizationStatus(p),
                    RecommendedAction = GetRecommendedAction(p),
                    DaysOfInventory = CalculateDaysOfInventory(p),
                    TurnoverRate = CalculateInventoryTurnover(p),
                    OptimalStockLevel = CalculateOptimalStock(p)
                }).ToList();

                var summary = new
                {
                    TotalProducts = products.Count,
                    TotalStockValue = optimizationAnalysis.Sum(a => a.StockValue),
                    OverstockedItems = optimizationAnalysis.Count(a => a.Status == "Overstocked"),
                    UnderstockedItems = optimizationAnalysis.Count(a => a.Status == "Understocked"),
                    OptimalItems = optimizationAnalysis.Count(a => a.Status == "Optimal"),
                    OutOfStockItems = optimizationAnalysis.Count(a => a.Status == "Out of Stock"),
                    PotentialSavings = CalculatePotentialSavings(optimizationAnalysis.Cast<object>().ToList())
                };

                return new
                {
                    Summary = summary,
                    OptimizationAnalysis = optimizationAnalysis.OrderBy(a => a.Status).ThenByDescending(a => a.StockValue).ToList(),
                    Recommendations = GenerateInventoryRecommendations(optimizationAnalysis.Cast<object>().ToList()),
                    ActionItems = optimizationAnalysis
                        .Where(a => a.RecommendedAction != "Tidak ada aksi")
                        .Take(20)
                        .ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting inventory optimization");
                return new { Error = "Gagal mengoptimalkan inventori" };
            }
        }

        // ==================== DASHBOARD & KPI ==================== //

        public async Task<object> GetExecutiveDashboardAsync(DateTime startDate, DateTime endDate, int? branchId = null)
        {
            try
            {
                var sales = await _context.Sales
                    .Include(s => s.SaleItems)
                    .Where(s => s.SaleDate >= startDate && s.SaleDate <= endDate)
                    .ToListAsync();

                var products = await _context.Products
                    .Include(p => p.Category)
                    .ToListAsync();

                // Financial KPIs
                var financialKPIs = new
                {
                    TotalRevenue = sales.Sum(s => s.Total),
                    TotalRevenueDisplay = sales.Sum(s => s.Total).ToString("C0", IdCulture),
                    TotalTransactions = sales.Count,
                    AverageTransactionValue = sales.Any() ? sales.Average(s => s.Total) : 0,
                    AverageTransactionValueDisplay = (sales.Any() ? sales.Average(s => s.Total) : 0).ToString("C0", IdCulture),
                    GrossProfit = sales.Sum(s => s.Total) * 0.25m, // Simplified calculation
                    GrossProfitDisplay = (sales.Sum(s => s.Total) * 0.25m).ToString("C0", IdCulture),
                    GrossProfitMargin = 25.0, // Simplified
                    DailyAverageRevenue = sales.Any() ? sales.Sum(s => s.Total) / ((endDate - startDate).Days + 1) : 0
                };

                // Operational KPIs
                var operationalKPIs = new
                {
                    TotalProducts = products.Count,
                    ActiveProducts = products.Count(p => p.Stock > 0),
                    LowStockItems = products.Count(p => p.Stock <= p.MinimumStock && p.MinimumStock > 0),
                    OutOfStockItems = products.Count(p => p.Stock == 0),
                    InventoryValue = products.Sum(p => p.Stock * p.BuyPrice),
                    InventoryValueDisplay = products.Sum(p => p.Stock * p.BuyPrice).ToString("C0", IdCulture),
                    InventoryTurnover = 12.0 // Simplified calculation
                };

                // Sales trends (last 7 days)
                var recentTrends = sales
                    .Where(s => s.SaleDate >= DateTime.Today.AddDays(-7))
                    .GroupBy(s => s.SaleDate.Date)
                    .Select(g => new
                    {
                        Date = g.Key.ToString("yyyy-MM-dd"),
                        DateDisplay = g.Key.ToString("dd/MM", IdCulture),
                        Sales = g.Sum(s => s.Total),
                        Transactions = g.Count()
                    })
                    .OrderBy(t => t.Date)
                    .ToList();

                // Top products
                var topProducts = sales
                    .SelectMany(s => s.SaleItems)
                    .Where(si => si.Product != null)
                    .GroupBy(si => si.Product!)
                    .Select(g => new
                    {
                        ProductName = g.Key.Name,
                        TotalSold = g.Sum(si => si.Quantity),
                        Revenue = g.Sum(si => si.Subtotal),
                        RevenueDisplay = g.Sum(si => si.Subtotal).ToString("C0", IdCulture)
                    })
                    .OrderByDescending(p => p.Revenue)
                    .Take(5)
                    .ToList();

                // Alerts and notifications
                var alerts = new List<object>();
                
                if (operationalKPIs.LowStockItems > 0)
                {
                    alerts.Add(new { Type = "warning", Message = $"{operationalKPIs.LowStockItems} produk stok menipis" });
                }
                
                if (operationalKPIs.OutOfStockItems > 0)
                {
                    alerts.Add(new { Type = "danger", Message = $"{operationalKPIs.OutOfStockItems} produk habis" });
                }

                return new
                {
                    DashboardDate = DateTime.Now.ToString("dd MMMM yyyy, HH:mm", IdCulture),
                    Period = $"{startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}",
                    FinancialKPIs = financialKPIs,
                    OperationalKPIs = operationalKPIs,
                    RecentTrends = recentTrends,
                    TopProducts = topProducts,
                    Alerts = alerts,
                    BusinessHealth = CalculateBusinessHealthScore(financialKPIs, operationalKPIs)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting executive dashboard");
                return new { Error = "Gagal memuat dashboard eksekutif" };
            }
        }

        public async Task<object> GetRealTimeMetricsAsync(int? branchId = null)
        {
            try
            {
                var today = DateTime.Today;
                var todaySales = await _context.Sales
                    .Where(s => s.SaleDate.Date == today)
                    .ToListAsync();

                var products = await _context.Products.ToListAsync();

                return new
                {
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Today = new
                    {
                        Revenue = todaySales.Sum(s => s.Total),
                        RevenueDisplay = todaySales.Sum(s => s.Total).ToString("C0", IdCulture),
                        Transactions = todaySales.Count,
                        AverageTransaction = todaySales.Any() ? todaySales.Average(s => s.Total) : 0,
                        LastTransaction = todaySales.Any() ? todaySales.Max(s => s.SaleDate).ToString("HH:mm") : "Belum ada"
                    },
                    Inventory = new
                    {
                        TotalProducts = products.Count,
                        LowStock = products.Count(p => p.Stock <= p.MinimumStock && p.MinimumStock > 0),
                        OutOfStock = products.Count(p => p.Stock == 0),
                        TotalValue = products.Sum(p => p.Stock * p.BuyPrice),
                        TotalValueDisplay = products.Sum(p => p.Stock * p.BuyPrice).ToString("C0", IdCulture)
                    },
                    Status = "active"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting real-time metrics");
                return new { Error = "Gagal memuat metrik real-time" };
            }
        }

        // ==================== HELPER METHODS ==================== //

        private decimal CalculateGrowthRate(decimal currentValue, List<Sale> allSales, DateTime currentDate)
        {
            var previousDate = currentDate.AddDays(-1);
            var previousValue = allSales
                .Where(s => s.SaleDate.Date == previousDate)
                .Sum(s => s.Total);

            if (previousValue == 0) return 0;
            return ((currentValue - previousValue) / previousValue) * 100;
        }

        private int GetWeekOfYear(DateTime date)
        {
            var cal = System.Globalization.DateTimeFormatInfo.CurrentInfo.Calendar;
            return cal.GetWeekOfYear(date, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday);
        }

        private string GetMonthName(int month)
        {
            var monthNames = new[]
            {
                "", "Januari", "Februari", "Maret", "April", "Mei", "Juni",
                "Juli", "Agustus", "September", "Oktober", "November", "Desember"
            };
            return month >= 1 && month <= 12 ? monthNames[month] : "Unknown";
        }

        private string GetDayName(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Monday => "Senin",
                DayOfWeek.Tuesday => "Selasa",
                DayOfWeek.Wednesday => "Rabu",
                DayOfWeek.Thursday => "Kamis",
                DayOfWeek.Friday => "Jumat",
                DayOfWeek.Saturday => "Sabtu",
                DayOfWeek.Sunday => "Minggu",
                _ => "Unknown"
            };
        }

        private decimal CalculateTrendSlope(List<object> dailySales)
        {
            if (dailySales.Count < 2) return 0;
            
            // Simple linear regression slope calculation
            var n = dailySales.Count;
            var sumX = Enumerable.Range(0, n).Sum();
            var sumY = dailySales.Sum(d => (decimal)d.GetType().GetProperty("Sales")!.GetValue(d)!);
            var sumXY = dailySales.Select((d, i) => i * (decimal)d.GetType().GetProperty("Sales")!.GetValue(d)!).Sum();
            var sumX2 = Enumerable.Range(0, n).Sum(x => x * x);

            if (n * sumX2 - sumX * sumX == 0) return 0;
            return (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
        }

        private decimal GetSeasonalMultiplier(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Monday => 0.8m,
                DayOfWeek.Tuesday => 0.9m,
                DayOfWeek.Wednesday => 1.0m,
                DayOfWeek.Thursday => 1.1m,
                DayOfWeek.Friday => 1.3m,
                DayOfWeek.Saturday => 1.4m,
                DayOfWeek.Sunday => 1.2m,
                _ => 1.0m
            };
        }

        private double CalculateConfidence(int dayOffset, int totalDays)
        {
            return Math.Max(0, 100 - (dayOffset * 3)); // Decrease confidence by 3% per day
        }

        private decimal CalculateMedian(List<decimal> values)
        {
            if (!values.Any()) return 0;
            
            var sorted = values.OrderBy(v => v).ToList();
            var mid = sorted.Count / 2;
            
            return sorted.Count % 2 == 0 
                ? (sorted[mid - 1] + sorted[mid]) / 2 
                : sorted[mid];
        }

        private object GenerateSalesInsights(List<Sale> sales, DateTime startDate, DateTime endDate)
        {
            var totalDays = (endDate - startDate).Days + 1;
            var dailyAverage = sales.Any() ? sales.Sum(s => s.Total) / totalDays : 0;
            
            return new
            {
                BestDay = sales.GroupBy(s => s.SaleDate.Date)
                    .OrderByDescending(g => g.Sum(s => s.Total))
                    .FirstOrDefault()?.Key.ToString("dd/MM/yyyy", IdCulture) ?? "Tidak ada data",
                
                AverageDailySales = dailyAverage,
                AverageDailySalesDisplay = dailyAverage.ToString("C0", IdCulture),
                
                PeakHour = sales.GroupBy(s => s.SaleDate.Hour)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault()?.Key.ToString("00:00") ?? "Tidak ada data",
                
                TrendIndicator = "Stabil" // Would need comparison with previous period
            };
        }

        private decimal CalculateProfitMargin(Product product, decimal revenue)
        {
            if (revenue == 0) return 0;
            var estimatedCost = revenue * 0.75m; // Simplified calculation
            return ((revenue - estimatedCost) / revenue) * 100;
        }

        private decimal CalculateVelocity(int quantity, DateTime startDate, DateTime endDate)
        {
            var days = (endDate - startDate).Days + 1;
            return days > 0 ? (decimal)quantity / days : 0;
        }

        private string GetStockOptimizationStatus(Product product)
        {
            if (product.Stock == 0) return "Out of Stock";
            if (product.MinimumStock > 0 && product.Stock <= product.MinimumStock) return "Understocked";
            if (product.MinimumStock > 0 && product.Stock >= product.MinimumStock * 3) return "Overstocked";
            return "Optimal";
        }

        private string GetRecommendedAction(Product product)
        {
            var status = GetStockOptimizationStatus(product);
            return status switch
            {
                "Out of Stock" => "Restock segera",
                "Understocked" => "Tambah stok",
                "Overstocked" => "Kurangi pemesanan atau beri diskon",
                _ => "Tidak ada aksi"
            };
        }

        private int CalculateDaysOfInventory(Product product)
        {
            // Simplified calculation - would need sales velocity data
            return product.Stock > 0 ? Math.Max(1, product.Stock / 2) : 0;
        }

        private decimal CalculateInventoryTurnover(Product product)
        {
            // Simplified calculation - would need COGS data
            return 12.0m; // Assume 12x per year
        }

        private int CalculateOptimalStock(Product product)
        {
            // Simplified calculation based on min/max
            if (product.MinimumStock > 0)
            {
                return product.MinimumStock * 2;
            }
            return product.MinimumStock > 0 ? product.MinimumStock * 2 : 10;
        }

        private decimal CalculatePotentialSavings(List<object> analysis)
        {
            // Simplified calculation
            return 50000m; // Placeholder value
        }

        private object GenerateInventoryRecommendations(List<object> analysis)
        {
            return new
            {
                TopPriority = "Restock produk yang habis",
                MediumPriority = "Optimalisasi stok berlebih",
                LowPriority = "Review minimum stock levels",
                GeneralAdvice = "Implement just-in-time inventory management"
            };
        }

        private object CalculateBusinessHealthScore(object financialKPIs, object operationalKPIs)
        {
            return new
            {
                OverallScore = 85,
                Grade = "B+",
                Status = "Baik",
                Color = "#28a745",
                Areas = new
                {
                    Sales = 90,
                    Inventory = 80,
                    Profitability = 85,
                    Operations = 82
                }
            };
        }

        // ==================== STUB IMPLEMENTATIONS ==================== //
        // These provide basic implementations to prevent NotImplementedException

        public Task<object> PredictStockMovementsAsync(int forecastDays = 30, int? productId = null)
        {
            return Task.FromResult<object>(new { Message = "Stock movement prediction implementation pending" });
        }

        public async Task<object> GetSlowMovingInventoryAsync(int? branchId = null, int daysThreshold = 90)
        {
            try
            {
                var cacheKey = $"slow_moving_inventory_{branchId}_{daysThreshold}";
                
                if (_cache.TryGetValue(cacheKey, out object? cachedResult) && cachedResult != null)
                {
                    return cachedResult;
                }

                var cutoffDate = DateTime.UtcNow.AddDays(-daysThreshold);
                
                // Get products with their sales history
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.SaleItems.Where(si => si.Sale.SaleDate >= cutoffDate))
                    .Where(p => p.IsActive)
                    .ToListAsync();

                var slowMovingAnalysis = products.Select(p =>
                {
                    // Calculate sales velocity
                    var totalSold = p.SaleItems.Sum(si => si.Quantity);
                    var lastSaleDate = p.SaleItems.Any() ? p.SaleItems.Max(si => si.Sale.SaleDate) : (DateTime?)null;
                    var daysSinceLastSale = lastSaleDate.HasValue ? (DateTime.UtcNow - lastSaleDate.Value).Days : int.MaxValue;
                    
                    // Calculate stock metrics
                    var stockValue = p.Stock * p.BuyPrice;
                    var dailyVelocity = daysThreshold > 0 ? (decimal)totalSold / daysThreshold : 0;
                    var daysOfStock = dailyVelocity > 0 ? p.Stock / dailyVelocity : int.MaxValue;
                    
                    // Risk assessment
                    var riskLevel = "Low";
                    var recommendedAction = "Monitor";
                    var potentialLoss = 0m;
                    
                    if (daysSinceLastSale > daysThreshold)
                    {
                        riskLevel = "High";
                        recommendedAction = "Discount/Clearance";
                        potentialLoss = stockValue * 0.3m; // 30% markdown
                    }
                    else if (daysSinceLastSale > daysThreshold / 2)
                    {
                        riskLevel = "Medium";
                        recommendedAction = "Promote/Review Pricing";
                        potentialLoss = stockValue * 0.15m; // 15% markdown
                    }
                    else if (totalSold == 0)
                    {
                        riskLevel = "Critical";
                        recommendedAction = "Review Product Viability";
                        potentialLoss = stockValue * 0.5m; // 50% potential loss
                    }
                    
                    return new
                    {
                        ProductId = p.Id,
                        ProductName = p.Name,
                        CategoryName = p.Category?.Name ?? "Tanpa Kategori",
                        CurrentStock = p.Stock,
                        StockValue = stockValue,
                        StockValueDisplay = stockValue.ToString("C0", IdCulture),
                        
                        // Sales metrics
                        TotalSoldInPeriod = totalSold,
                        DaysSinceLastSale = daysSinceLastSale == int.MaxValue ? "Never" : daysSinceLastSale.ToString(),
                        LastSaleDate = lastSaleDate?.ToString("dd/MM/yyyy", IdCulture) ?? "Tidak pernah",
                        DailyVelocity = Math.Round(dailyVelocity, 2),
                        DaysOfStock = daysOfStock == int.MaxValue ? "Unlimited" : Math.Round(daysOfStock, 0).ToString(),
                        
                        // Risk assessment
                        RiskLevel = riskLevel,
                        RecommendedAction = recommendedAction,
                        PotentialLoss = potentialLoss,
                        PotentialLossDisplay = potentialLoss.ToString("C0", IdCulture),
                        
                        // Scoring
                        SlowMovingScore = CalculateSlowMovingScore(daysSinceLastSale, totalSold, daysThreshold),
                        Priority = daysSinceLastSale > daysThreshold ? "High" : daysSinceLastSale > daysThreshold / 2 ? "Medium" : "Low"
                    };
                }).ToList();

                // Filter to actual slow-moving items
                var slowMovingItems = slowMovingAnalysis
                    .Where(item => item.DaysSinceLastSale != "Never" ? 
                        int.Parse(item.DaysSinceLastSale) > daysThreshold / 2 : true)
                    .OrderByDescending(item => item.SlowMovingScore)
                    .ToList();

                // Calculate summary metrics
                var totalSlowMovingValue = slowMovingItems.Sum(item => item.StockValue);
                var totalPotentialLoss = slowMovingItems.Sum(item => item.PotentialLoss);
                
                var result = new
                {
                    AnalysisDate = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm", IdCulture),
                    DaysThreshold = daysThreshold,
                    BranchId = branchId,
                    
                    Summary = new
                    {
                        TotalProducts = products.Count,
                        SlowMovingItems = slowMovingItems.Count,
                        SlowMovingPercentage = products.Count > 0 ? Math.Round((double)slowMovingItems.Count / products.Count * 100, 1) : 0,
                        TotalStockValue = products.Sum(p => p.Stock * p.BuyPrice),
                        TotalStockValueDisplay = products.Sum(p => p.Stock * p.BuyPrice).ToString("C0", IdCulture),
                        SlowMovingStockValue = totalSlowMovingValue,
                        SlowMovingStockValueDisplay = totalSlowMovingValue.ToString("C0", IdCulture),
                        PotentialLoss = totalPotentialLoss,
                        PotentialLossDisplay = totalPotentialLoss.ToString("C0", IdCulture),
                        RiskLevel = totalPotentialLoss > 10000000 ? "High" : totalPotentialLoss > 5000000 ? "Medium" : "Low"
                    },
                    
                    SlowMovingItems = slowMovingItems.Take(50).ToList(), // Top 50 items
                    
                    CategoryBreakdown = slowMovingItems
                        .GroupBy(item => item.CategoryName)
                        .Select(g => new
                        {
                            CategoryName = g.Key,
                            ItemCount = g.Count(),
                            TotalValue = g.Sum(item => item.StockValue),
                            TotalValueDisplay = g.Sum(item => item.StockValue).ToString("C0", IdCulture),
                            AverageRiskScore = g.Average(item => item.SlowMovingScore)
                        })
                        .OrderByDescending(c => c.TotalValue)
                        .ToList(),
                    
                    ActionableRecommendations = new
                    {
                        HighPriority = slowMovingItems.Where(item => item.Priority == "High").Take(10).ToList(),
                        MediumPriority = slowMovingItems.Where(item => item.Priority == "Medium").Take(10).ToList(),
                        GeneralRecommendations = new List<string>
                        {
                            "Implement promotional campaigns for slow-moving items",
                            "Review pricing strategy for high-risk products",
                            "Consider supplier negotiations for better terms",
                            "Improve product placement and visibility",
                            "Analyze customer feedback for product improvements"
                        }
                    },
                    
                    Insights = new
                    {
                        MostAffectedCategory = slowMovingItems.GroupBy(i => i.CategoryName)
                            .OrderByDescending(g => g.Count())
                            .FirstOrDefault()?.Key ?? "N/A",
                        HighestRiskProduct = slowMovingItems.OrderByDescending(i => i.PotentialLoss).FirstOrDefault()?.ProductName ?? "N/A",
                        TotalActionItemsCount = slowMovingItems.Count(i => i.Priority == "High" || i.Priority == "Medium")
                    }
                };

                // Cache for 4 hours
                _cache.Set(cacheKey, result, TimeSpan.FromHours(4));
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing slow moving inventory");
                return new { Error = "Gagal menganalisis inventori bergerak lambat" };
            }
        }
        
        private int CalculateSlowMovingScore(int daysSinceLastSale, int totalSold, int daysThreshold)
        {
            var score = 0;
            
            // Days since last sale (50% weight)
            if (daysSinceLastSale > daysThreshold * 2) score += 50;
            else if (daysSinceLastSale > daysThreshold) score += 40;
            else if (daysSinceLastSale > daysThreshold / 2) score += 30;
            else if (daysSinceLastSale > daysThreshold / 4) score += 20;
            else score += 10;
            
            // Sales velocity (30% weight)
            if (totalSold == 0) score += 30;
            else if (totalSold <= 2) score += 25;
            else if (totalSold <= 5) score += 20;
            else if (totalSold <= 10) score += 15;
            else score += 5;
            
            // Time factor (20% weight)
            var timeScore = Math.Min(20, daysSinceLastSale / 10);
            score += timeScore;
            
            return Math.Min(100, score);
        }

        public async Task<object> GetReorderRecommendationsAsync(int? branchId = null)
        {
            try
            {
                var cacheKey = $"reorder_recommendations_{branchId}";
                
                if (_cache.TryGetValue(cacheKey, out object? cachedResult) && cachedResult != null)
                {
                    return cachedResult;
                }

                // Get products with sales history for velocity calculation
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.SaleItems.Where(si => si.Sale.SaleDate >= DateTime.UtcNow.AddDays(-90)))
                    .Where(p => p.IsActive)
                    .ToListAsync();

                var reorderAnalysis = new List<object>();

                foreach (var product in products)
                {
                    // Calculate sales velocity (units per day over last 90 days)
                    var sales90Days = product.SaleItems.Sum(si => si.Quantity);
                    var dailyVelocity = sales90Days / 90.0m;
                    var daysOfStock = dailyVelocity > 0 ? product.Stock / dailyVelocity : int.MaxValue;
                    
                    // Calculate safety stock (buffer)
                    var leadTimeDays = 14; // Assume 14 days lead time
                    var safetyStock = (int)(dailyVelocity * leadTimeDays * 1.2m); // 20% safety buffer
                    var reorderPoint = safetyStock + (int)(dailyVelocity * leadTimeDays);
                    
                    // Calculate optimal order quantity (simplified EOQ)
                    var annualDemand = dailyVelocity * 365;
                    var orderingCost = 50000; // 50k IDR per order
                    var holdingCostRate = 0.15m; // 15% of product cost
                    var holdingCost = product.BuyPrice * holdingCostRate;
                    
                    var economicOrderQty = holdingCost > 0 ? 
                        (int)Math.Sqrt((double)((2 * orderingCost * annualDemand) / holdingCost)) : 
                        Math.Max(product.MinimumStock, (int)(dailyVelocity * 30)); // 30 days supply
                    
                    // Determine reorder priority
                    var priority = 0;
                    var urgencyLevel = "Low";
                    var recommendedAction = "Monitor";
                    
                    if (product.Stock <= 0)
                    {
                        priority = 100;
                        urgencyLevel = "Critical";
                        recommendedAction = "Emergency Reorder";
                    }
                    else if (product.Stock <= product.MinimumStock)
                    {
                        priority = 90;
                        urgencyLevel = "High";
                        recommendedAction = "Immediate Reorder";
                    }
                    else if (product.Stock <= reorderPoint)
                    {
                        priority = 70;
                        urgencyLevel = "Medium";
                        recommendedAction = "Schedule Reorder";
                    }
                    else if (daysOfStock <= 30)
                    {
                        priority = 50;
                        urgencyLevel = "Low";
                        recommendedAction = "Plan Reorder";
                    }
                    
                    // Calculate financial impact
                    var potentialLostSales = 0m;
                    if (daysOfStock <= 7 && dailyVelocity > 0)
                    {
                        var stockoutDays = Math.Max(0, 7 - daysOfStock);
                        potentialLostSales = (decimal)stockoutDays * dailyVelocity * product.SellPrice;
                    }
                    
                    var recommendedOrderQty = Math.Max(
                        economicOrderQty,
                        reorderPoint - product.Stock + (int)(dailyVelocity * 30) // Cover 30 days
                    );
                    
                    if (priority >= 50) // Only include items that need attention
                    {
                        reorderAnalysis.Add(new
                        {
                            ProductId = product.Id,
                            ProductName = product.Name,
                            CategoryName = product.Category?.Name ?? "Tanpa Kategori",
                            CurrentStock = product.Stock,
                            MinimumStock = product.MinimumStock,
                            ReorderPoint = reorderPoint,
                            SafetyStock = safetyStock,
                            
                            // Sales metrics
                            DailyVelocity = Math.Round(dailyVelocity, 2),
                            Sales90Days = sales90Days,
                            DaysOfStock = daysOfStock == int.MaxValue ? "Unlimited" : Math.Round(daysOfStock, 1).ToString(),
                            
                            // Reorder recommendations
                            RecommendedOrderQty = Math.Max(0, recommendedOrderQty),
                            EconomicOrderQty = economicOrderQty,
                            Priority = priority,
                            UrgencyLevel = urgencyLevel,
                            RecommendedAction = recommendedAction,
                            
                            // Financial impact
                            OrderCost = recommendedOrderQty * product.BuyPrice,
                            OrderCostDisplay = (recommendedOrderQty * product.BuyPrice).ToString("C0", IdCulture),
                            PotentialLostSales = potentialLostSales,
                            PotentialLostSalesDisplay = potentialLostSales.ToString("C0", IdCulture),
                            NetBenefit = potentialLostSales - (recommendedOrderQty * product.BuyPrice * holdingCostRate),
                            
                            // Timing
                            EstimatedStockoutDate = dailyVelocity > 0 ? 
                                DateTime.UtcNow.AddDays((double)daysOfStock).ToString("dd/MM/yyyy", IdCulture) : 
                                "Never",
                            RecommendedOrderDate = DateTime.UtcNow.AddDays(Math.Max(0, (double)(daysOfStock - leadTimeDays))).ToString("dd/MM/yyyy", IdCulture),
                            
                            // Supplier info
                            LeadTimeDays = leadTimeDays,
                            SupplierId = (int?)null, // Would need supplier relationship
                            SupplierName = "To be determined"
                        });
                    }
                }

                var sortedRecommendations = reorderAnalysis
                    .Cast<dynamic>()
                    .OrderByDescending(r => r.Priority)
                    .ThenBy(r => r.EstimatedStockoutDate)
                    .ToList();

                var totalOrderValue = sortedRecommendations.Sum(r => (decimal)r.OrderCost);
                var totalPotentialLoss = sortedRecommendations.Sum(r => (decimal)r.PotentialLostSales);

                var result = new
                {
                    GeneratedAt = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm", IdCulture),
                    BranchId = branchId,
                    
                    Summary = new
                    {
                        TotalRecommendations = reorderAnalysis.Count,
                        CriticalItems = reorderAnalysis.Cast<dynamic>().Count(r => r.UrgencyLevel == "Critical"),
                        HighPriorityItems = reorderAnalysis.Cast<dynamic>().Count(r => r.UrgencyLevel == "High"),
                        MediumPriorityItems = reorderAnalysis.Cast<dynamic>().Count(r => r.UrgencyLevel == "Medium"),
                        
                        TotalOrderValue = totalOrderValue,
                        TotalOrderValueDisplay = totalOrderValue.ToString("C0", IdCulture),
                        PotentialLossWithoutReorder = totalPotentialLoss,
                        PotentialLossDisplay = totalPotentialLoss.ToString("C0", IdCulture),
                        NetSavingsOpportunity = totalPotentialLoss - (totalOrderValue * 0.15m),
                        NetSavingsOpportunityDisplay = (totalPotentialLoss - (totalOrderValue * 0.15m)).ToString("C0", IdCulture)
                    },
                    
                    Recommendations = sortedRecommendations.Take(50).ToList(), // Top 50
                    
                    CategoryInsights = reorderAnalysis
                        .Cast<dynamic>()
                        .GroupBy(r => r.CategoryName)
                        .Select(g => new
                        {
                            CategoryName = g.Key,
                            ItemCount = g.Count(),
                            TotalOrderValue = g.Sum(r => (decimal)r.OrderCost),
                            TotalOrderValueDisplay = g.Sum(r => (decimal)r.OrderCost).ToString("C0", IdCulture),
                            AveragePriority = g.Average(r => (int)r.Priority),
                            CriticalCount = g.Count(r => r.UrgencyLevel == "Critical")
                        })
                        .OrderByDescending(c => c.AveragePriority)
                        .ToList(),
                    
                    ActionPlan = new
                    {
                        ImmediateActions = sortedRecommendations
                            .Where(r => r.UrgencyLevel == "Critical" || r.UrgencyLevel == "High")
                            .Take(10)
                            .ToList(),
                        ThisWeekActions = sortedRecommendations
                            .Where(r => r.UrgencyLevel == "Medium")
                            .Take(15)
                            .ToList(),
                        UpcomingActions = sortedRecommendations
                            .Where(r => r.UrgencyLevel == "Low")
                            .Take(20)
                            .ToList()
                    },
                    
                    BusinessInsights = new
                    {
                        TopCategoryByUrgency = reorderAnalysis.Cast<dynamic>()
                            .GroupBy(r => r.CategoryName)
                            .OrderByDescending(g => g.Average(r => (int)r.Priority))
                            .FirstOrDefault()?.Key ?? "N/A",
                        
                        MostCriticalProduct = sortedRecommendations
                            .FirstOrDefault()?.ProductName ?? "N/A",
                        
                        ReorderFrequency = "Weekly review recommended",
                        
                        OptimizationTips = new List<string>
                        {
                            "Consider negotiating shorter lead times with high-priority suppliers",
                            "Implement automated reorder points for fast-moving items",
                            "Review minimum stock levels quarterly based on demand patterns",
                            "Consider bulk ordering for high-volume, stable products",
                            "Monitor seasonal trends for more accurate forecasting"
                        }
                    }
                };

                // Cache for 6 hours
                _cache.Set(cacheKey, result, TimeSpan.FromHours(6));
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating reorder recommendations");
                return new { Error = "Gagal membuat rekomendasi pemesanan ulang" };
            }
        }

        public async Task<object> GetABCAnalysisAsync(int? branchId = null)
        {
            try
            {
                var cacheKey = $"abc_analysis_{branchId}";
                
                if (_cache.TryGetValue(cacheKey, out object? cachedResult) && cachedResult != null)
                {
                    return cachedResult;
                }

                // Get sales data for last 12 months for ABC analysis
                var startDate = DateTime.UtcNow.AddMonths(-12);
                var endDate = DateTime.UtcNow;
                
                var productSales = await _context.SaleItems
                    .Include(si => si.Product)
                        .ThenInclude(p => p!.Category)
                    .Include(si => si.Sale)
                    .Where(si => si.Sale.SaleDate >= startDate && si.Sale.SaleDate <= endDate)
                    .Where(si => si.Product != null)
                    .GroupBy(si => si.Product!)
                    .Select(g => new
                    {
                        Product = g.Key,
                        TotalRevenue = g.Sum(si => si.Subtotal),
                        TotalQuantitySold = g.Sum(si => si.Quantity),
                        TransactionCount = g.Select(si => si.SaleId).Distinct().Count(),
                        AveragePrice = g.Average(si => si.UnitPrice),
                        TotalCost = g.Sum(si => si.Quantity * si.Product!.BuyPrice),
                        GrossProfit = g.Sum(si => si.Subtotal - (si.Quantity * si.Product!.BuyPrice))
                    })
                    .ToListAsync();

                // Calculate ABC classification based on revenue (80-15-5 rule)
                var totalRevenue = productSales.Sum(ps => ps.TotalRevenue);
                var sortedByRevenue = productSales.OrderByDescending(ps => ps.TotalRevenue).ToList();
                
                var runningRevenue = 0m;
                var abcAnalysis = new List<object>();
                
                for (int i = 0; i < sortedByRevenue.Count; i++)
                {
                    var productSale = sortedByRevenue[i];
                    runningRevenue += productSale.TotalRevenue;
                    var revenuePercentage = totalRevenue > 0 ? (runningRevenue / totalRevenue) * 100 : 0;
                    
                    string abcClass;
                    string classification;
                    string strategy;
                    
                    if (revenuePercentage <= 80)
                    {
                        abcClass = "A";
                        classification = "High Value";
                        strategy = "Tight control, frequent review, high service level";
                    }
                    else if (revenuePercentage <= 95)
                    {
                        abcClass = "B";
                        classification = "Moderate Value";
                        strategy = "Moderate control, regular review, good service level";
                    }
                    else
                    {
                        abcClass = "C";
                        classification = "Low Value";
                        strategy = "Simple control, periodic review, acceptable stockouts";
                    }
                    
                    // Calculate additional metrics
                    var stockValue = productSale.Product.Stock * productSale.Product.BuyPrice;
                    var turnoverRate = stockValue > 0 ? productSale.TotalCost / stockValue : 0;
                    var profitMargin = productSale.TotalRevenue > 0 ? (productSale.GrossProfit / productSale.TotalRevenue) * 100 : 0;
                    
                    // Calculate days of inventory
                    var dailyUsage = productSale.TotalQuantitySold / 365.0m;
                    var daysOfInventory = dailyUsage > 0 ? productSale.Product.Stock / dailyUsage : int.MaxValue;
                    
                    abcAnalysis.Add(new
                    {
                        ProductId = productSale.Product.Id,
                        ProductName = productSale.Product.Name,
                        CategoryName = productSale.Product.Category?.Name ?? "Tanpa Kategori",
                        
                        // ABC Classification
                        ABCClass = abcClass,
                        Classification = classification,
                        RevenueRank = i + 1,
                        CumulativeRevenuePercentage = Math.Round(revenuePercentage, 2),
                        
                        // Financial metrics
                        TotalRevenue = productSale.TotalRevenue,
                        TotalRevenueDisplay = productSale.TotalRevenue.ToString("C0", IdCulture),
                        RevenuePercentage = totalRevenue > 0 ? Math.Round((productSale.TotalRevenue / totalRevenue) * 100, 2) : 0,
                        GrossProfit = productSale.GrossProfit,
                        GrossProfitDisplay = productSale.GrossProfit.ToString("C0", IdCulture),
                        ProfitMargin = Math.Round(profitMargin, 2),
                        
                        // Sales metrics
                        TotalQuantitySold = productSale.TotalQuantitySold,
                        TransactionCount = productSale.TransactionCount,
                        AveragePrice = Math.Round(productSale.AveragePrice, 0),
                        AveragePriceDisplay = productSale.AveragePrice.ToString("C0", IdCulture),
                        
                        // Inventory metrics
                        CurrentStock = productSale.Product.Stock,
                        StockValue = stockValue,
                        StockValueDisplay = stockValue.ToString("C0", IdCulture),
                        TurnoverRate = Math.Round(turnoverRate, 2),
                        DaysOfInventory = daysOfInventory == int.MaxValue ? "N/A" : Math.Round(daysOfInventory, 0).ToString(),
                        
                        // Management strategy
                        RecommendedStrategy = strategy,
                        InventoryPolicy = abcClass switch
                        {
                            "A" => "High service level (98%), tight control, daily monitoring",
                            "B" => "Good service level (95%), weekly monitoring, moderate safety stock",
                            "C" => "Acceptable service level (90%), monthly monitoring, low safety stock",
                            _ => "Standard policy"
                        },
                        
                        // Risk assessment
                        StockoutRisk = abcClass == "A" ? "High Impact" : abcClass == "B" ? "Medium Impact" : "Low Impact",
                        ReorderPriority = abcClass == "A" ? "Critical" : abcClass == "B" ? "Important" : "Standard"
                    });
                }
                
                // Calculate summary statistics
                var aItems = abcAnalysis.Cast<dynamic>().Where(item => item.ABCClass == "A").ToList();
                var bItems = abcAnalysis.Cast<dynamic>().Where(item => item.ABCClass == "B").ToList();
                var cItems = abcAnalysis.Cast<dynamic>().Where(item => item.ABCClass == "C").ToList();
                
                var result = new
                {
                    AnalysisDate = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm", IdCulture),
                    AnalysisPeriod = $"{startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}",
                    BranchId = branchId,
                    
                    Summary = new
                    {
                        TotalProducts = abcAnalysis.Count,
                        TotalRevenue = totalRevenue,
                        TotalRevenueDisplay = totalRevenue.ToString("C0", IdCulture),
                        
                        ClassificationBreakdown = new
                        {
                            AItems = new
                            {
                                Count = aItems.Count,
                                Percentage = abcAnalysis.Count > 0 ? Math.Round((double)aItems.Count / abcAnalysis.Count * 100, 1) : 0,
                                RevenueContribution = aItems.Sum(item => (decimal)item.TotalRevenue),
                                RevenueContributionDisplay = aItems.Sum(item => (decimal)item.TotalRevenue).ToString("C0", IdCulture),
                                RevenuePercentage = totalRevenue > 0 ? Math.Round(aItems.Sum(item => (decimal)item.TotalRevenue) / totalRevenue * 100, 1) : 0
                            },
                            BItems = new
                            {
                                Count = bItems.Count,
                                Percentage = abcAnalysis.Count > 0 ? Math.Round((double)bItems.Count / abcAnalysis.Count * 100, 1) : 0,
                                RevenueContribution = bItems.Sum(item => (decimal)item.TotalRevenue),
                                RevenueContributionDisplay = bItems.Sum(item => (decimal)item.TotalRevenue).ToString("C0", IdCulture),
                                RevenuePercentage = totalRevenue > 0 ? Math.Round(bItems.Sum(item => (decimal)item.TotalRevenue) / totalRevenue * 100, 1) : 0
                            },
                            CItems = new
                            {
                                Count = cItems.Count,
                                Percentage = abcAnalysis.Count > 0 ? Math.Round((double)cItems.Count / abcAnalysis.Count * 100, 1) : 0,
                                RevenueContribution = cItems.Sum(item => (decimal)item.TotalRevenue),
                                RevenueContributionDisplay = cItems.Sum(item => (decimal)item.TotalRevenue).ToString("C0", IdCulture),
                                RevenuePercentage = totalRevenue > 0 ? Math.Round(cItems.Sum(item => (decimal)item.TotalRevenue) / totalRevenue * 100, 1) : 0
                            }
                        }
                    },
                    
                    ABCAnalysis = abcAnalysis.Take(100).ToList(), // Top 100 items
                    
                    CategoryInsights = abcAnalysis
                        .Cast<dynamic>()
                        .GroupBy(item => item.CategoryName)
                        .Select(g => new
                        {
                            CategoryName = g.Key,
                            TotalItems = g.Count(),
                            AItems = g.Count(item => item.ABCClass == "A"),
                            BItems = g.Count(item => item.ABCClass == "B"),
                            CItems = g.Count(item => item.ABCClass == "C"),
                            TotalRevenue = g.Sum(item => (decimal)item.TotalRevenue),
                            TotalRevenueDisplay = g.Sum(item => (decimal)item.TotalRevenue).ToString("C0", IdCulture),
                            AverageMargin = g.Average(item => (decimal)item.ProfitMargin)
                        })
                        .OrderByDescending(c => c.TotalRevenue)
                        .ToList(),
                    
                    ManagementRecommendations = new
                    {
                        AClassItems = new
                        {
                            FocusAreas = new List<string>
                            {
                                "Implement tight inventory control with daily monitoring",
                                "Maintain high service levels (98%+) to avoid stockouts",
                                "Consider vendor-managed inventory for top items",
                                "Negotiate better terms with suppliers for volume discounts",
                                "Implement demand forecasting for accurate planning"
                            },
                            TopItems = aItems.Take(5).ToList()
                        },
                        BClassItems = new
                        {
                            FocusAreas = new List<string>
                            {
                                "Weekly inventory reviews and moderate control",
                                "Maintain good service levels (95%+)",
                                "Consider automated reorder points",
                                "Regular supplier performance reviews"
                            }
                        },
                        CClassItems = new
                        {
                            FocusAreas = new List<string>
                            {
                                "Monthly inventory reviews with simple control",
                                "Acceptable stockout levels to reduce carrying costs",
                                "Consider bulk purchasing for economies of scale",
                                "Evaluate discontinuation of very low performers"
                            }
                        }
                    },
                    
                    KeyInsights = new
                    {
                        ParetoValidation = aItems.Sum(item => (decimal)item.TotalRevenue) / totalRevenue * 100 > 70 ? 
                            "Pareto principle confirmed: small number of products drive majority of revenue" :
                            "Revenue distribution is more balanced than typical Pareto pattern",
                        
                        TopPerformer = aItems.FirstOrDefault()?.ProductName ?? "N/A",
                        TopPerformerRevenue = aItems.FirstOrDefault()?.TotalRevenueDisplay ?? "N/A",
                        
                        OptimizationOpportunity = $"Focus inventory management efforts on {aItems.Count} A-class items for maximum impact",
                        
                        CostReductionPotential = $"C-class items ({cItems.Count} products) may be candidates for inventory reduction or discontinuation"
                    }
                };

                // Cache for 12 hours (ABC analysis doesn't change frequently)
                _cache.Set(cacheKey, result, TimeSpan.FromHours(12));
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing ABC analysis");
                return new { Error = "Gagal melakukan analisis ABC" };
            }
        }

        public Task<object> GetProfitabilityAnalysisAsync(DateTime startDate, DateTime endDate, string analysisType = "product")
        {
            return Task.FromResult<object>(new { Message = "Profitability analysis implementation pending" });
        }

        public Task<object> PredictCashFlowAsync(int forecastDays = 30, int? branchId = null)
        {
            return Task.FromResult<object>(new { Message = "Cash flow prediction implementation pending" });
        }

        public Task<object> GetBreakEvenAnalysisAsync(int? productId = null, int? branchId = null)
        {
            return Task.FromResult<object>(new { Message = "Break-even analysis implementation pending" });
        }

        public Task<object> GetCostOptimizationInsightsAsync(DateTime startDate, DateTime endDate)
        {
            return Task.FromResult<object>(new { Message = "Cost optimization insights implementation pending" });
        }

        public Task<object> GetFinancialKPIDashboardAsync(DateTime startDate, DateTime endDate, int? branchId = null)
        {
            return Task.FromResult<object>(new { Message = "Financial KPI dashboard implementation pending" });
        }

        public Task<object> GetStaffPerformanceAnalyticsAsync(DateTime startDate, DateTime endDate, int? branchId = null)
        {
            return Task.FromResult<object>(new { Message = "Staff performance analytics implementation pending" });
        }

        public Task<object> GetPeakHoursAnalysisAsync(DateTime startDate, DateTime endDate, int? branchId = null)
        {
            return Task.FromResult<object>(new { Message = "Peak hours analysis implementation pending" });
        }

        public Task<object> GetSupplierPerformanceScoringAsync(DateTime startDate, DateTime endDate, int? supplierId = null)
        {
            return Task.FromResult<object>(new { Message = "Supplier performance scoring implementation pending" });
        }

        public async Task<object> GetBranchComparisonAnalyticsAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var cacheKey = $"branch_comparison_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";
                
                if (_cache.TryGetValue(cacheKey, out object? cachedResult) && cachedResult != null)
                {
                    return cachedResult;
                }

                // Get all active branches
                var branches = await _context.Branches
                    .Where(b => b.IsActive)
                    .ToListAsync();

                var branchAnalytics = new List<object>();

                foreach (var branch in branches)
                {
                    // Calculate sales data for branch
                    var branchSales = await _context.Sales
                        .Where(s => s.SaleDate >= startDate && s.SaleDate <= endDate)
                        .Where(s => s.Cashier != null && s.Cashier.BranchId == branch.Id)
                        .ToListAsync();

                    // Calculate inventory value for branch (simplified)
                    var branchProducts = await _context.Products
                        .Where(p => p.IsActive)
                        .ToListAsync();
                    var inventoryValue = branchProducts.Sum(p => p.Stock * p.BuyPrice) / branches.Count; // Distributed equally

                    // Calculate performance metrics
                    var totalRevenue = branchSales.Sum(s => s.Total);
                    var transactionCount = branchSales.Count;
                    var averageTransaction = transactionCount > 0 ? totalRevenue / transactionCount : 0;
                    
                    // Calculate revenue growth (vs previous period)
                    var periodDays = (endDate - startDate).Days;
                    var previousStart = startDate.AddDays(-periodDays);
                    var previousSales = await _context.Sales
                        .Where(s => s.SaleDate >= previousStart && s.SaleDate < startDate)
                        .Where(s => s.Cashier != null && s.Cashier.BranchId == branch.Id)
                        .SumAsync(s => s.Total);
                    
                    var revenueGrowth = previousSales > 0 ? ((totalRevenue - previousSales) / previousSales) * 100 : 0;

                    // Calculate efficiency metrics
                    var activeEmployees = await _context.Users
                        .Where(u => u.IsActive && u.BranchId == branch.Id)
                        .CountAsync();
                    
                    var salesPerEmployee = activeEmployees > 0 ? totalRevenue / activeEmployees : 0;
                    var transactionsPerEmployee = activeEmployees > 0 ? (decimal)transactionCount / activeEmployees : 0;

                    // Performance scoring
                    var efficiencyScore = Math.Min(100, transactionsPerEmployee * 10); // 10 transactions per employee = 100%
                    var profitabilityScore = inventoryValue > 0 ? Math.Min(100, (totalRevenue / inventoryValue) * 20) : 0;
                    var overallScore = (efficiencyScore + profitabilityScore) / 2;
                    
                    branchAnalytics.Add(new
                    {
                        BranchId = branch.Id,
                        BranchName = branch.BranchName,
                        City = branch.City,
                        Province = branch.Province,
                        
                        // Financial metrics
                        TotalRevenue = totalRevenue,
                        TotalRevenueDisplay = totalRevenue.ToString("C0", IdCulture),
                        RevenueGrowth = Math.Round(revenueGrowth, 2),
                        RevenueGrowthDisplay = $"{revenueGrowth:+0.0;-0.0;0}%",
                        
                        // Operational metrics
                        TransactionCount = transactionCount,
                        AverageTransactionValue = Math.Round(averageTransaction, 0),
                        AverageTransactionDisplay = averageTransaction.ToString("C0", IdCulture),
                        
                        // Staff metrics
                        ActiveEmployees = activeEmployees,
                        SalesPerEmployee = Math.Round(salesPerEmployee, 0),
                        SalesPerEmployeeDisplay = salesPerEmployee.ToString("C0", IdCulture),
                        TransactionsPerEmployee = Math.Round(transactionsPerEmployee, 1),
                        
                        // Inventory metrics
                        InventoryValue = inventoryValue,
                        InventoryValueDisplay = inventoryValue.ToString("C0", IdCulture),
                        
                        // Performance scores
                        EfficiencyScore = Math.Round(efficiencyScore, 1),
                        ProfitabilityScore = Math.Round(profitabilityScore, 1),
                        OverallPerformanceScore = Math.Round(overallScore, 1),
                        PerformanceGrade = overallScore >= 90 ? "A" : overallScore >= 80 ? "B" : overallScore >= 70 ? "C" : overallScore >= 60 ? "D" : "F",
                        
                        // Status indicators
                        TrendDirection = revenueGrowth > 5 ? "Naik" : revenueGrowth < -5 ? "Turun" : "Stabil",
                        HealthStatus = overallScore >= 80 ? "Sehat" : overallScore >= 60 ? "Baik" : "Perlu Perhatian"
                    });
                }

                // Calculate comparative insights
                var topPerformer = branchAnalytics
                    .Cast<dynamic>()
                    .OrderByDescending(b => b.TotalRevenue)
                    .FirstOrDefault();
                
                var averageRevenue = branchAnalytics
                    .Cast<dynamic>()
                    .Average(b => (decimal)b.TotalRevenue);
                
                var totalSystemRevenue = branchAnalytics
                    .Cast<dynamic>()
                    .Sum(b => (decimal)b.TotalRevenue);

                var result = new
                {
                    AnalysisPeriod = new
                    {
                        StartDate = startDate.ToString("dd/MM/yyyy", IdCulture),
                        EndDate = endDate.ToString("dd/MM/yyyy", IdCulture),
                        PeriodDays = (endDate - startDate).Days + 1
                    },
                    Summary = new
                    {
                        TotalBranches = branchAnalytics.Count,
                        TotalSystemRevenue = totalSystemRevenue,
                        TotalSystemRevenueDisplay = totalSystemRevenue.ToString("C0", IdCulture),
                        AverageRevenuePerBranch = averageRevenue,
                        AverageRevenuePerBranchDisplay = averageRevenue.ToString("C0", IdCulture),
                        TopPerformer = topPerformer?.BranchName ?? "N/A",
                        TopPerformerRevenue = topPerformer?.TotalRevenueDisplay ?? "N/A"
                    },
                    BranchAnalytics = branchAnalytics
                        .Cast<dynamic>()
                        .OrderByDescending(b => b.TotalRevenue)
                        .ToList(),
                    Insights = new
                    {
                        PerformanceDistribution = new
                        {
                            Excellent = branchAnalytics.Cast<dynamic>().Count(b => b.OverallPerformanceScore >= 90),
                            Good = branchAnalytics.Cast<dynamic>().Count(b => b.OverallPerformanceScore >= 80 && b.OverallPerformanceScore < 90),
                            Average = branchAnalytics.Cast<dynamic>().Count(b => b.OverallPerformanceScore >= 60 && b.OverallPerformanceScore < 80),
                            NeedsImprovement = branchAnalytics.Cast<dynamic>().Count(b => b.OverallPerformanceScore < 60)
                        },
                        TopRecommendations = new List<string>
                        {
                            "Focus on low-performing branches for improvement initiatives",
                            "Share best practices from top performers to underperforming branches",
                            "Consider staff training programs for branches with low efficiency scores",
                            "Review inventory distribution across branches"
                        }
                    }
                };

                // Cache for 2 hours
                _cache.Set(cacheKey, result, TimeSpan.FromHours(2));
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting branch comparison analytics");
                return new { Error = "Gagal menganalisis perbandingan cabang" };
            }
        }

        public Task<object> GetKPIMetricsAsync(DateTime startDate, DateTime endDate, int? branchId = null)
        {
            return Task.FromResult<object>(new { Message = "KPI metrics implementation pending" });
        }

        public Task<object> GetBusinessHealthScoreAsync(int? branchId = null)
        {
            return Task.FromResult<object>(new { Message = "Business health score implementation pending" });
        }

        public Task<object> GetBusinessInsightsAsync(int? branchId = null)
        {
            return Task.FromResult<object>(new { Message = "Business insights implementation pending" });
        }

        public Task<object> GetAnomalyAlertsAsync(DateTime startDate, DateTime endDate, int? branchId = null)
        {
            return Task.FromResult<object>(new { Message = "Anomaly alerts implementation pending" });
        }

        public Task<object> GetStrategicRecommendationsAsync(int? branchId = null)
        {
            return Task.FromResult<object>(new { Message = "Strategic recommendations implementation pending" });
        }

        public Task<object> GetMarketOpportunityAnalysisAsync(DateTime startDate, DateTime endDate)
        {
            return Task.FromResult<object>(new { Message = "Market opportunity analysis implementation pending" });
        }

        public Task<object> CalculateCustomerLifetimeValueAsync(int? customerId = null)
        {
            return Task.FromResult<object>(new { Message = "Customer lifetime value calculation implementation pending" });
        }

        public Task<object> GetMarketBasketAnalysisAsync(DateTime startDate, DateTime endDate, int? branchId = null)
        {
            return Task.FromResult<object>(new { Message = "Market basket analysis implementation pending" });
        }

        public Task<object> GetPriceElasticityAnalysisAsync(int productId, DateTime startDate, DateTime endDate)
        {
            return Task.FromResult<object>(new { Message = "Price elasticity analysis implementation pending" });
        }

        public Task<object> GetCohortAnalysisAsync(DateTime startDate, DateTime endDate, int? branchId = null)
        {
            return Task.FromResult<object>(new { Message = "Cohort analysis implementation pending" });
        }
    }
}