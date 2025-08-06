// Services/DashboardService.cs - Fixed: EF Core compatibility
using Berca_Backend.DTOs;
using Berca_Backend.Models;
using Berca_Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace Berca_Backend.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(AppDbContext context, ILogger<DashboardService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<DashboardKPIDto> GetDashboardKPIsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var monthStart = new DateTime(today.Year, today.Month, 1);
                var yearStart = new DateTime(today.Year, 1, 1);

                // Today's sales
                var todaySales = await _context.Sales
                    .Where(s => s.SaleDate.Date == today && s.Status == SaleStatus.Completed)
                    .ToListAsync();

                // Monthly sales
                var monthlySales = await _context.Sales
                    .Where(s => s.SaleDate >= monthStart && s.Status == SaleStatus.Completed)
                    .ToListAsync();

                // Yearly sales
                var yearlySales = await _context.Sales
                    .Where(s => s.SaleDate >= yearStart && s.Status == SaleStatus.Completed)
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

        public async Task<List<TopProductDto>> GetTopSellingProductsAsync(int count = 10, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow;

                // ✅ FIXED: Split into two queries to avoid computed property in SQL
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
                        // ✅ Calculate profit manually using database fields
                        TotalProfit = g.Sum(si => (si.UnitPrice - si.UnitCost) * si.Quantity - si.DiscountAmount),
                        TransactionCount = g.Count()
                    })
                    .OrderByDescending(p => p.TotalQuantitySold)
                    .Take(count)
                    .ToListAsync();

                // Convert to DTOs
                return saleItemsData.Select(item => new TopProductDto
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    ProductBarcode = item.ProductBarcode,
                    TotalQuantitySold = item.TotalQuantitySold,
                    TotalRevenue = item.TotalRevenue,
                    TotalProfit = item.TotalProfit,
                    TransactionCount = item.TransactionCount
                }).ToList();
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

        public async Task<List<WorstProductDto>> GetWorstPerformingProductsAsync(int count = 10, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow;

                // Get products with sales data
                var productSales = await _context.SaleItems
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
                        CurrentStock = g.First().Product.Stock
                    })
                    .OrderBy(p => p.TotalQuantitySold) // Worst performers first
                    .Take(count)
                    .ToListAsync();

                // Calculate days without sale
                var results = new List<WorstProductDto>();
                foreach (var product in productSales)
                {
                    var lastSale = await _context.SaleItems
                        .Include(si => si.Sale)
                        .Where(si => si.ProductId == product.ProductId && si.Sale.Status == SaleStatus.Completed)
                        .OrderByDescending(si => si.Sale.SaleDate)
                        .FirstOrDefaultAsync();

                    var daysWithoutSale = lastSale != null 
                        ? (DateTime.UtcNow.Date - lastSale.Sale.SaleDate.Date).Days 
                        : 9999; // Very high number for products never sold

                    results.Add(new WorstProductDto
                    {
                        ProductId = product.ProductId,
                        ProductName = product.ProductName,
                        ProductBarcode = product.ProductBarcode,
                        TotalQuantitySold = product.TotalQuantitySold,
                        TotalRevenue = product.TotalRevenue,
                        TotalProfit = product.TotalProfit,
                        TransactionCount = product.TransactionCount,
                        DaysWithoutSale = daysWithoutSale,
                        CurrentStock = product.CurrentStock
                    });
                }

                return results.OrderBy(r => r.TotalQuantitySold).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting worst performing products");
                throw;
            }
        }

        public async Task<FinancialReportDto> GenerateFinancialReportAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var sales = await _context.Sales
                    .Include(s => s.SaleItems)
                    .Where(s => s.SaleDate >= startDate && s.SaleDate <= endDate && s.Status == SaleStatus.Completed)
                    .ToListAsync();

                var totalRevenue = sales.Sum(s => s.Total);
                // ✅ FIXED: Calculate from SaleItems directly to avoid database translation issues
                var totalCost = sales.SelectMany(s => s.SaleItems).Sum(si => si.UnitCost * si.Quantity);
                var grossProfit = totalRevenue - totalCost;
                var grossProfitMargin = totalRevenue > 0 ? (grossProfit / totalRevenue) * 100 : 0;
                var totalTax = sales.Sum(s => s.TaxAmount); // ✅ FIXED: Use TaxAmount instead of Tax
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
                        // ✅ FIXED: Calculate cost from SaleItems
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

        // Export methods (for now return placeholder - actual PDF/Excel generation can be implemented later)
        public async Task<ReportExportDto> ExportSalesReportAsync(DateTime startDate, DateTime endDate, string format)
        {
            // For now, return a placeholder. You can implement actual PDF/Excel generation later
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