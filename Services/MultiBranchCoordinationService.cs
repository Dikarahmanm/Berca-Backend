using Berca_Backend.Data;
using Berca_Backend.DTOs;
using Berca_Backend.Models;
using Berca_Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Berca_Backend.Services
{
    /// <summary>
    /// Multi-branch coordination service for advanced inter-branch operations
    /// Handles transfer recommendations, performance comparison, and optimization
    /// </summary>
    public interface IMultiBranchCoordinationService
    {
        Task<List<DTOs.InterBranchTransferRecommendationDto>> GetInterBranchTransferRecommendationsAsync();
        Task<DTOs.BranchPerformanceComparisonDto> GetBranchPerformanceComparisonAsync(DateTime startDate, DateTime endDate);
        Task<List<DTOs.CrossBranchOpportunityDto>> GetCrossBranchOpportunitiesAsync();
        Task<InventoryDistributionPlanDto> OptimizeInventoryDistributionAsync(int? productId = null);
        Task<InventoryDistributionPlanDto> OptimizeInventoryDistributionAsync(OptimizationParametersDto parameters);
        Task<bool> ProcessRecommendedTransferAsync(InterBranchTransferRequest request);
        
        // Missing methods from controller
        Task<List<DTOs.CrossBranchOpportunityDto>> GetCrossBranchOptimizationOpportunitiesAsync();
        Task<List<BranchDemandForecastDto>> GetDemandForecastAsync(int forecastDays, int? productId = null);
        Task<OptimizationExecutionResultDto> ExecuteAutomaticOptimizationAsync(bool dryRun, int userId);
    }

    public class MultiBranchCoordinationService : IMultiBranchCoordinationService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<MultiBranchCoordinationService> _logger;

        public MultiBranchCoordinationService(
            AppDbContext context,
            ILogger<MultiBranchCoordinationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<DTOs.InterBranchTransferRecommendationDto>> GetInterBranchTransferRecommendationsAsync()
        {
            try
            {
                var recommendations = new List<DTOs.InterBranchTransferRecommendationDto>();
                
                // Get all active branches
                var branches = await _context.Branches
                    .Where(b => b.IsActive)
                    .ToListAsync();

                // Find products with expiring batches in one branch but high demand in another
                foreach (var sourceBranch in branches)
                {
                    var expiringBatches = await _context.ProductBatches
                        .Include(pb => pb.Product)
                        .Where(pb => pb.BranchId == sourceBranch.Id &&
                                    pb.ExpiryDate.HasValue &&
                                    pb.ExpiryDate <= DateTime.UtcNow.AddDays(30) &&
                                    pb.CurrentStock > 0 &&
                                    !pb.IsDisposed)
                        .ToListAsync();

                    foreach (var batch in expiringBatches)
                    {
                        // Find branches with high demand for this product
                        var potentialTargetBranches = await FindHighDemandBranchesAsync(batch.ProductId, sourceBranch.Id);

                        foreach (var targetBranch in potentialTargetBranches)
                        {
                            var recommendation = await CreateTransferRecommendationAsync(sourceBranch, targetBranch, batch);
                            if (recommendation != null)
                            {
                                recommendations.Add(recommendation);
                            }
                        }
                    }
                }

                // Also check for stock imbalances (high stock in one branch, low in another)
                var stockImbalanceRecommendations = await GetStockImbalanceRecommendationsAsync(branches);
                recommendations.AddRange(stockImbalanceRecommendations);

                return recommendations
                    .OrderByDescending(r => r.PotentialSavings)
                    .ThenByDescending(r => r.UrgencyScore)
                    .Take(50) // Limit to top 50 recommendations
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting inter-branch transfer recommendations");
                throw;
            }
        }

        public async Task<DTOs.BranchPerformanceComparisonDto> GetBranchPerformanceComparisonAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var branches = await _context.Branches
                    .Where(b => b.IsActive)
                    .ToListAsync();

                var comparisons = new List<BranchPerformanceComparisonDto>();

                foreach (var branch in branches)
                {
                    // Calculate comprehensive performance metrics
                    var salesData = await GetBranchSalesDataAsync(branch.Id, startDate, endDate);
                    var inventoryData = await GetBranchInventoryDataAsync(branch.Id);
                    var expiryData = await GetBranchExpiryDataAsync(branch.Id);

                    var performance = new BranchPerformanceComparisonDto
                    {
                        BranchId = branch.Id,
                        BranchName = branch.BranchName,
                        City = branch.City,
                        Province = branch.Province,
                        
                        // Financial metrics
                        TotalRevenue = salesData.TotalRevenue,
                        GrossProfit = salesData.GrossProfit,
                        NetProfitMargin = salesData.NetProfitMargin,
                        RevenuePerSquareMeter = CalculateRevenuePerSquareMeter(salesData.TotalRevenue, 100), // Default store size
                        
                        // Operational metrics
                        TransactionCount = salesData.TransactionCount,
                        AverageTransactionValue = salesData.AverageTransactionValue,
                        SalesPerEmployee = await CalculateSalesPerEmployeeAsync(branch.Id, salesData.TotalRevenue),
                        
                        // Inventory metrics
                        InventoryTurnover = inventoryData.InventoryTurnover,
                        StockoutRate = inventoryData.StockoutRate,
                        ExcessStockValue = inventoryData.ExcessStockValue,
                        
                        // Expiry management metrics
                        WastagePercentage = expiryData.WastePercentage,
                        ExpiryPreventionScore = expiryData.PreventionScore,
                        ValuePreserved = expiryData.ValuePreserved,
                        
                        // Overall performance score (0-100)
                        OverallPerformanceScore = CalculateOverallPerformanceScore(salesData, inventoryData, expiryData),
                        
                        // Growth metrics
                        RevenueGrowth = await CalculateBranchRevenueGrowthAsync(branch.Id, startDate, endDate),
                        
                        // Strengths and improvement areas
                        StrengthAreas = IdentifyStrengthAreas(salesData, inventoryData, expiryData),
                        ImprovementAreas = IdentifyImprovementAreas(salesData, inventoryData, expiryData),
                        
                        // Best practices this branch excels at
                        BestPractices = new List<DTOs.BestPractice>() // Placeholder - would implement best practices identification
                    };

                    comparisons.Add(performance);
                }

                // Convert to proper DTO structure
                var branchMetrics = new List<DTOs.BranchPerformanceMetricsDto>();
                
                foreach (var comparison in comparisons)
                {
                    branchMetrics.Add(new DTOs.BranchPerformanceMetricsDto
                    {
                        BranchId = comparison.BranchId,
                        BranchName = comparison.BranchName,
                        TotalRevenue = comparison.TotalRevenue,
                        OverallScore = (int)comparison.OverallPerformanceScore
                    });
                }

                AssignPerformanceRankings(branchMetrics);

                var result = new DTOs.BranchPerformanceComparisonDto
                {
                    AnalysisDate = DateTime.UtcNow,
                    PeriodStart = startDate,
                    PeriodEnd = endDate,
                    BranchMetrics = branchMetrics,
                    SystemBenchmarks = new DTOs.BenchmarkMetricsDto(),
                    KeyInsights = new List<DTOs.BranchPerformanceInsightDto>()
                };

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting branch performance comparison");
                throw;
            }
        }

        public async Task<List<DTOs.CrossBranchOpportunityDto>> GetCrossBranchOpportunitiesAsync()
        {
            try
            {
                var opportunities = new List<DTOs.CrossBranchOpportunityDto>();

                // Simplified opportunities implementation
                opportunities.Add(new DTOs.CrossBranchOpportunityDto
                {
                    OpportunityType = "Knowledge Transfer",
                    Title = "Best Practice Sharing Program",
                    Description = "Transfer best practices between branches to improve performance",
                    Impact = "High",
                    PotentialSavings = 5000000m,
                    RecommendedImplementationDate = DateTime.UtcNow.AddMonths(3)
                });

                // Opportunity 2: Inventory Optimization
                var inventoryOpportunities = await IdentifyInventoryOptimizationOpportunities();
                opportunities.AddRange(inventoryOpportunities);

                // Opportunity 3: Staff Optimization
                var staffOpportunities = await IdentifyStaffOptimizationOpportunities();
                opportunities.AddRange(staffOpportunities);

                return opportunities.OrderByDescending(o => o.EstimatedBenefit).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cross-branch opportunities");
                throw;
            }
        }

        public async Task<InventoryDistributionPlanDto> OptimizeInventoryDistributionAsync(int? productId = null)
        {
            try
            {
                var branches = await _context.Branches.Where(b => b.IsActive).ToListAsync();
                var optimizationResults = new List<BranchInventoryOptimization>();

                foreach (var branch in branches)
                {
                    var branchProducts = await _context.Products
                        .Include(p => p.ProductBatches.Where(pb => pb.BranchId == branch.Id))
                        .Where(p => p.IsActive)
                        .ToListAsync();

                    var optimization = new BranchInventoryOptimization
                    {
                        BranchId = branch.Id,
                        BranchName = branch.BranchName,
                        CurrentInventoryValue = branchProducts.Sum(p => p.Stock * p.BuyPrice),
                        OptimizedInventoryValue = 0, // Will calculate below
                        OverstockedItems = new List<ProductOptimization>(),
                        UnderstockedItems = new List<ProductOptimization>(),
                        TransferRecommendations = new List<TransferRecommendation>()
                    };

                    foreach (var product in branchProducts)
                    {
                        var salesVelocity = await CalculateProductSalesVelocity(product.Id, branch.Id);
                        var optimalStock = CalculateOptimalStockLevel(product, salesVelocity);
                        
                        if (product.Stock > optimalStock * 1.5)
                        {
                            // Overstocked
                            optimization.OverstockedItems.Add(new ProductOptimization
                            {
                                ProductId = product.Id,
                                ProductName = product.Name,
                                CurrentStock = product.Stock,
                                OptimalStock = optimalStock,
                                ExcessQuantity = product.Stock - optimalStock,
                                ValueImpact = (product.Stock - optimalStock) * product.BuyPrice
                            });
                        }
                        else if (product.Stock < product.MinimumStock)
                        {
                            // Understocked
                            optimization.UnderstockedItems.Add(new ProductOptimization
                            {
                                ProductId = product.Id,
                                ProductName = product.Name,
                                CurrentStock = product.Stock,
                                OptimalStock = Math.Max(optimalStock, product.MinimumStock),
                                ShortageQuantity = Math.Max(optimalStock, product.MinimumStock) - product.Stock,
                                ValueImpact = (Math.Max(optimalStock, product.MinimumStock) - product.Stock) * product.BuyPrice
                            });
                        }

                        optimization.OptimizedInventoryValue += optimalStock * product.BuyPrice;
                    }

                    optimizationResults.Add(optimization);
                }

                // Generate inter-branch transfer recommendations
                await GenerateOptimizationTransferRecommendations(optimizationResults);

                return new InventoryDistributionPlanDto
                {
                    GeneratedAt = DateTime.UtcNow,
                    OptimizationScope = productId.HasValue ? "Single Product" : "All Products",
                    TotalOptimizationValue = optimizationResults.Sum(b => b.CurrentInventoryValue - b.OptimizedInventoryValue),
                    ImplementationCost = 50000, // Mock cost
                    NetBenefit = optimizationResults.Sum(b => b.CurrentInventoryValue - b.OptimizedInventoryValue) - 50000,
                    Recommendations = optimizationResults.SelectMany(b => b.OverstockedItems.Select(item => new DistributionRecommendationDto
                    {
                        ActionType = "Transfer",
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        SourceBranchId = b.BranchId,
                        SourceBranchName = b.BranchName,
                        TargetBranchId = 0, // Will be determined later
                        TargetBranchName = "TBD",
                        Quantity = item.ExcessQuantity,
                        EstimatedBenefit = item.ValueImpact,
                        Priority = "Medium",
                        Rationale = "Overstock optimization",
                        RecommendedExecutionDate = DateTime.UtcNow.AddDays(1)
                    })).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error optimizing inventory distribution");
                throw;
            }
        }

        public async Task<bool> ProcessRecommendedTransferAsync(InterBranchTransferRequest request)
        {
            try
            {
                // This would integrate with existing inventory transfer system
                // For now, just log the transfer request
                _logger.LogInformation("Processing recommended transfer: {ProductId} from Branch {FromBranchId} to Branch {ToBranchId}, Quantity: {Quantity}",
                    request.ProductId, request.FromBranchId, request.ToBranchId, request.Quantity);

                // Create transfer record (would need to implement actual transfer logic)
                var transfer = new InventoryTransfer
                {
                    FromBranchId = request.FromBranchId,
                    ToBranchId = request.ToBranchId,
                    ProductId = request.ProductId,
                    Quantity = request.Quantity,
                    TransferDate = DateTime.UtcNow,
                    Status = TransferStatus.Pending,
                    Reason = request.Reason,
                    RequestedBy = request.RequestedByUserId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.InventoryTransfers.Add(transfer);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing recommended transfer");
                return false;
            }
        }

        // Private helper methods
        private async Task<List<Branch>> FindHighDemandBranchesAsync(int productId, int excludeBranchId)
        {
            // Find branches with high sales velocity for this product
            var branches = await _context.Branches
                .Where(b => b.IsActive && b.Id != excludeBranchId)
                .ToListAsync();

            var highDemandBranches = new List<Branch>();

            foreach (var branch in branches)
            {
                var salesVelocity = await CalculateProductSalesVelocity(productId, branch.Id);
                if (salesVelocity > 2) // More than 2 units per day on average
                {
                    highDemandBranches.Add(branch);
                }
            }

            return highDemandBranches;
        }

        private Task<InterBranchTransferRecommendationDto?> CreateTransferRecommendationAsync(Branch sourceBranch, Branch targetBranch, ProductBatch batch)
        {
            return Task.Run(() =>
            {
                try
                {
                    var transferQuantity = Math.Min(batch.CurrentStock, batch.CurrentStock / 2); // Transfer up to half
                    var transferCost = CalculateTransferCost(sourceBranch.Id, targetBranch.Id, transferQuantity);
                    var potentialRevenue = transferQuantity * batch.Product.SellPrice;
                    var potentialSavings = potentialRevenue - transferCost - (transferQuantity * batch.CostPerUnit);

                    if (potentialSavings > transferCost * 0.2m) // 20% minimum ROI
                    {
                        return (InterBranchTransferRecommendationDto?)new InterBranchTransferRecommendationDto
                        {
                            FromBranchId = sourceBranch.Id,
                            FromBranchName = sourceBranch.BranchName,
                            ToBranchId = targetBranch.Id,
                            ToBranchName = targetBranch.BranchName,
                            ProductId = batch.ProductId,
                            ProductName = batch.Product.Name,
                            BatchId = batch.Id,
                            BatchNumber = batch.BatchNumber,
                            RecommendedQuantity = transferQuantity,
                            TransferCost = transferCost,
                            PotentialSavings = potentialSavings,
                            UrgencyScore = CalculateTransferUrgency(batch),
                            RecommendedTransferDate = DateTime.UtcNow.AddDays(1),
                            TransferReasons = new List<string>
                            {
                                "Prevent expiry waste at source branch",
                                "Meet demand at target branch",
                                "Optimize inventory distribution"
                            },
                            Logistics = new TransferLogistics
                            {
                                EstimatedTransitTime = "1-2 days",
                                RequiredVehicle = "Standard delivery",
                                SpecialHandling = batch.Product?.Category?.RequiresExpiryDate == true ? "Temperature controlled" : "Standard"
                            }
                        };
                    }

                    return null;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating transfer recommendation for batch {BatchId}", batch.Id);
                    return null;
                }
            });
        }

        private async Task<List<InterBranchTransferRecommendationDto>> GetStockImbalanceRecommendationsAsync(List<Branch> branches)
        {
            var recommendations = new List<InterBranchTransferRecommendationDto>();

            // This is a simplified implementation - would need more complex logic for real scenarios
            var products = await _context.Products
                .Where(p => p.IsActive)
                .Take(20) // Limit for performance
                .ToListAsync();

            foreach (var product in products)
            {
                var branchStocks = new List<BranchStockInfo>();
                
                foreach (var branch in branches)
                {
                    var branchStock = await CalculateBranchProductStock(branch.Id, product.Id);
                    branchStocks.Add(new BranchStockInfo
                    {
                        BranchId = branch.Id,
                        BranchName = branch.BranchName,
                        Stock = branchStock,
                        OptimalStock = product.MinimumStock * 2 // Simplified calculation
                    });
                }

                var excessBranches = branchStocks.Where(bs => bs.Stock > bs.OptimalStock * 1.5).ToList();
                var shortageBranches = branchStocks.Where(bs => bs.Stock < product.MinimumStock).ToList();

                foreach (var excessBranch in excessBranches)
                {
                    foreach (var shortageBranch in shortageBranches)
                    {
                        var transferQuantity = Math.Min(
                            excessBranch.Stock - excessBranch.OptimalStock,
                            shortageBranch.OptimalStock - shortageBranch.Stock
                        );

                        if (transferQuantity > 0)
                        {
                            var transferCost = CalculateTransferCost(excessBranch.BranchId, shortageBranch.BranchId, transferQuantity);
                            var potentialRevenue = transferQuantity * product.SellPrice;
                            var potentialSavings = potentialRevenue - transferCost;

                            if (potentialSavings > transferCost * 0.2m) // 20% minimum ROI
                            {
                                recommendations.Add(new InterBranchTransferRecommendationDto
                                {
                                    FromBranchId = excessBranch.BranchId,
                                    FromBranchName = excessBranch.BranchName,
                                    ToBranchId = shortageBranch.BranchId,
                                    ToBranchName = shortageBranch.BranchName,
                                    ProductId = product.Id,
                                    ProductName = product.Name,
                                    RecommendedQuantity = transferQuantity,
                                    TransferCost = transferCost,
                                    PotentialSavings = potentialSavings,
                                    UrgencyScore = CalculateStockImbalanceUrgency(shortageBranch.Stock, product.MinimumStock),
                                    RecommendedTransferDate = DateTime.UtcNow.AddDays(1),
                                    TransferReasons = new List<string>
                                    {
                                        "Stock imbalance optimization",
                                        "Prevent stockout at target branch",
                                        "Reduce excess inventory at source branch"
                                    }
                                });
                            }
                        }
                    }
                }
            }

            return recommendations;
        }

        // Additional helper methods with simplified implementations
        private async Task<BranchSalesData> GetBranchSalesDataAsync(int branchId, DateTime startDate, DateTime endDate)
        {
            var sales = await _context.Sales
                .Where(s => s.SaleDate >= startDate && s.SaleDate <= endDate)
                .Where(s => s.Cashier != null && s.Cashier.BranchId == branchId)
                .ToListAsync();

            return new BranchSalesData
            {
                TotalRevenue = sales.Sum(s => s.Total),
                TransactionCount = sales.Count,
                AverageTransactionValue = sales.Any() ? sales.Average(s => s.Total) : 0,
                GrossProfit = sales.Sum(s => s.Total) * 0.25m, // Simplified 25% margin
                NetProfitMargin = 25m // Simplified
            };
        }

        private Task<BranchInventoryData> GetBranchInventoryDataAsync(int branchId)
        {
            // Simplified implementation
            return Task.FromResult(new BranchInventoryData
            {
                InventoryTurnover = 12m, // 12x per year
                StockoutRate = 5m, // 5%
                ExcessStockValue = 0m
            });
        }

        private async Task<BranchExpiryData> GetBranchExpiryDataAsync(int branchId)
        {
            var batches = await _context.ProductBatches
                .Where(pb => pb.BranchId == branchId && pb.ExpiryDate.HasValue)
                .ToListAsync();

            var expiredBatches = batches.Where(b => b.ExpiryDate <= DateTime.UtcNow).ToList();
            var totalValue = batches.Sum(b => b.InitialStock * b.CostPerUnit);
            var expiredValue = expiredBatches.Sum(b => b.CurrentStock * b.CostPerUnit);

            return new BranchExpiryData
            {
                WastePercentage = totalValue > 0 ? (expiredValue / totalValue) * 100 : 0,
                PreventionScore = Math.Max(0, 100 - (expiredValue / Math.Max(totalValue, 1) * 100)),
                ValuePreserved = totalValue - expiredValue
            };
        }

        private decimal CalculateRevenuePerSquareMeter(decimal revenue, int storeSize)
        {
            return storeSize > 0 ? revenue / storeSize : 0;
        }

        private async Task<decimal> CalculateSalesPerEmployeeAsync(int branchId, decimal totalRevenue)
        {
            var employeeCount = await _context.Users
                .Where(u => u.IsActive && u.BranchId == branchId)
                .CountAsync();

            return employeeCount > 0 ? totalRevenue / employeeCount : 0;
        }

        private decimal CalculateOverallPerformanceScore(BranchSalesData sales, BranchInventoryData inventory, BranchExpiryData expiry)
        {
            // Simplified scoring algorithm
            var salesScore = Math.Min(100, (sales.TotalRevenue / 1000000) * 10); // 10M revenue = 100 points
            var inventoryScore = Math.Min(100, inventory.InventoryTurnover * 8); // 12.5 turns = 100 points
            var expiryScore = expiry.PreventionScore;

            return (salesScore + inventoryScore + expiryScore) / 3;
        }

        private async Task<decimal> CalculateBranchRevenueGrowthAsync(int branchId, DateTime startDate, DateTime endDate)
        {
            var periodDays = (endDate - startDate).Days;
            var previousPeriodStart = startDate.AddDays(-periodDays);
            
            var currentRevenue = await _context.Sales
                .Where(s => s.SaleDate >= startDate && s.SaleDate <= endDate && s.Cashier.BranchId == branchId)
                .SumAsync(s => s.Total);
                
            var previousRevenue = await _context.Sales
                .Where(s => s.SaleDate >= previousPeriodStart && s.SaleDate < startDate && s.Cashier.BranchId == branchId)
                .SumAsync(s => s.Total);
                
            return previousRevenue > 0 ? ((currentRevenue - previousRevenue) / previousRevenue) * 100 : 0;
        }

        private List<string> IdentifyStrengthAreas(BranchSalesData sales, BranchInventoryData inventory, BranchExpiryData expiry)
        {
            var strengths = new List<string>();
            
            if (sales.AverageTransactionValue > 150000) strengths.Add("High transaction values");
            if (inventory.InventoryTurnover > 10) strengths.Add("Efficient inventory management");
            if (expiry.WastePercentage < 5) strengths.Add("Excellent expiry management");
            
            return strengths;
        }

        private List<string> IdentifyImprovementAreas(BranchSalesData sales, BranchInventoryData inventory, BranchExpiryData expiry)
        {
            var improvements = new List<string>();
            
            if (sales.TransactionCount < 50) improvements.Add("Increase customer traffic");
            if (inventory.StockoutRate > 10) improvements.Add("Reduce stockouts");
            if (expiry.WastePercentage > 10) improvements.Add("Improve expiry management");
            
            return improvements;
        }

        private List<BestPractice> IdentifyBestPractices(Branch branch, BranchSalesData sales, BranchInventoryData inventory, BranchExpiryData expiry)
        {
            var practices = new List<BestPractice>();
            
            if (expiry.WastePercentage < 3)
            {
                practices.Add(new BestPractice
                {
                    Category = "Expiry Management",
                    Practice = "Proactive FIFO implementation",
                    Impact = "Minimal waste generation"
                });
            }
            
            return practices;
        }

        private void AssignPerformanceRankings(List<DTOs.BranchPerformanceMetricsDto> comparisons)
        {
            var sortedByScore = comparisons.OrderByDescending(c => c.OverallScore).ToList();
            
            for (int i = 0; i < sortedByScore.Count; i++)
            {
                sortedByScore[i].OverallRank = i + 1;
            }
        }

        private Task<List<DTOs.CrossBranchOpportunityDto>> IdentifyInventoryOptimizationOpportunities()
        {
            // Simplified implementation
            return Task.Run(() => new List<DTOs.CrossBranchOpportunityDto>
            {
                new DTOs.CrossBranchOpportunityDto
                {
                    OpportunityType = "Inventory Optimization",
                    Title = "Centralized Procurement",
                    Description = "Consolidate purchasing to achieve better supplier terms",
                    Impact = "Medium",
                    PotentialSavings = 5000000m,
                    RecommendedImplementationDate = DateTime.UtcNow.AddMonths(2)
                }
            });
        }

        private Task<List<DTOs.CrossBranchOpportunityDto>> IdentifyStaffOptimizationOpportunities()
        {
            // Simplified implementation
            return Task.Run(() => new List<DTOs.CrossBranchOpportunityDto>
            {
                new DTOs.CrossBranchOpportunityDto
                {
                    OpportunityType = "Staff Optimization",
                    Title = "Cross-Branch Training Program",
                    Description = "Share expertise across branches to improve performance",
                    Impact = "High",
                    PotentialSavings = 3000000m,
                    RecommendedImplementationDate = DateTime.UtcNow.AddMonths(1)
                }
            });
        }

        private async Task<decimal> CalculateProductSalesVelocity(int productId, int branchId)
        {
            var sales30Days = await _context.SaleItems
                .Where(si => si.ProductId == productId && 
                           si.Sale.SaleDate >= DateTime.UtcNow.AddDays(-30) &&
                           si.Sale.Cashier.BranchId == branchId)
                .SumAsync(si => si.Quantity);

            return sales30Days / 30.0m; // Daily velocity
        }

        private int CalculateOptimalStockLevel(Product product, decimal salesVelocity)
        {
            var leadTimeDays = 14; // Assume 14 days lead time
            var safetyStock = (int)(salesVelocity * 7); // 1 week safety stock
            var reorderQuantity = (int)(salesVelocity * leadTimeDays);
            
            return Math.Max(product.MinimumStock, safetyStock + reorderQuantity);
        }

        private async Task<int> CalculateBranchProductStock(int branchId, int productId)
        {
            // Simplified - would need actual branch-specific stock tracking
            return await _context.ProductBatches
                .Where(pb => pb.BranchId == branchId && pb.ProductId == productId && !pb.IsDisposed)
                .SumAsync(pb => pb.CurrentStock);
        }

        private async Task GenerateOptimizationTransferRecommendations(List<BranchInventoryOptimization> optimizations)
        {
            // Generate transfer recommendations between branches based on optimization results
            foreach (var sourceBranch in optimizations.Where(o => o.OverstockedItems.Any()))
            {
                foreach (var overstockedItem in sourceBranch.OverstockedItems)
                {
                    var targetBranches = optimizations
                        .Where(o => o.BranchId != sourceBranch.BranchId && 
                               o.UnderstockedItems.Any(u => u.ProductId == overstockedItem.ProductId))
                        .ToList();

                    foreach (var targetBranch in targetBranches)
                    {
                        var understockedItem = targetBranch.UnderstockedItems
                            .FirstOrDefault(u => u.ProductId == overstockedItem.ProductId);
                        
                        if (understockedItem != null)
                        {
                            var transferQuantity = Math.Min(overstockedItem.ExcessQuantity, understockedItem.ShortageQuantity);
                            
                            sourceBranch.TransferRecommendations.Add(new TransferRecommendation
                            {
                                ToSranchId = targetBranch.BranchId,
                                ToBranchName = targetBranch.BranchName,
                                ProductId = overstockedItem.ProductId,
                                ProductName = overstockedItem.ProductName,
                                RecommendedQuantity = transferQuantity,
                                EstimatedSavings = transferQuantity * overstockedItem.ValueImpact / Math.Max(overstockedItem.ExcessQuantity, 1)
                            });
                        }
                    }
                }
            }
        }

        private decimal CalculateTransferCost(int fromBranchId, int toBranchId, int quantity)
        {
            // Simplified calculation - would need real distance and logistics data
            var baseCost = 50000m; // Base transfer cost
            var perUnitCost = 1000m; // Cost per unit
            var distanceMultiplier = 1.2m; // Assume 20% extra for distance
            
            return (baseCost + (quantity * perUnitCost)) * distanceMultiplier;
        }

        private int CalculateTransferUrgency(ProductBatch batch)
        {
            if (!batch.ExpiryDate.HasValue) return 5;
            
            var daysToExpiry = (batch.ExpiryDate.Value - DateTime.UtcNow).Days;
            return daysToExpiry switch
            {
                <= 1 => 10,
                <= 3 => 9,
                <= 7 => 8,
                <= 14 => 6,
                <= 30 => 4,
                _ => 2
            };
        }

        private int CalculateStockImbalanceUrgency(int currentStock, int minimumStock)
        {
            if (currentStock <= 0) return 10;
            if (currentStock <= minimumStock * 0.5) return 8;
            if (currentStock <= minimumStock) return 6;
            return 3;
        }

        // Missing interface method implementations
        public async Task<List<DTOs.CrossBranchOpportunityDto>> GetCrossBranchOptimizationOpportunitiesAsync()
        {
            // Delegate to existing method
            return await GetCrossBranchOpportunitiesAsync();
        }

        public Task<List<BranchDemandForecastDto>> GetDemandForecastAsync(int forecastDays, int? productId = null)
        {
            return Task.Run(async () =>
            {
                try
                {
                    var branches = await _context.Branches.Where(b => b.IsActive).ToListAsync();
                    var forecasts = new List<BranchDemandForecastDto>();

                    foreach (var branch in branches)
                    {
                        var forecast = new BranchDemandForecastDto
                        {
                            BranchId = branch.Id,
                            BranchName = branch.BranchName,
                            GeneratedAt = DateTime.UtcNow,
                            ForecastPeriodStart = DateTime.UtcNow,
                            ForecastPeriodEnd = DateTime.UtcNow.AddDays(forecastDays),
                            TotalForecastedDemand = 100, // Mock calculation
                            ForecastConfidence = 85.0m,
                            ProductForecasts = new List<ProductDemandForecastDto>()
                        };
                        forecasts.Add(forecast);
                    }

                    return forecasts;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating demand forecast");
                    throw;
                }
            });
        }

        public Task<OptimizationExecutionResultDto> ExecuteAutomaticOptimizationAsync(bool dryRun, int userId)
        {
            return Task.Run(() =>
            {
                try
                {
                    var result = new OptimizationExecutionResultDto
                    {
                        ExecutedAt = DateTime.UtcNow,
                        WasDryRun = dryRun,
                        ExecutedByUserId = userId,
                        IsSuccess = true,
                        ExecutedActions = new List<ExecutedActionDto>(),
                        Errors = new List<string>(),
                        Warnings = new List<string>()
                    };

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing automatic optimization");
                    return new OptimizationExecutionResultDto
                    {
                        ExecutedAt = DateTime.UtcNow,
                        WasDryRun = dryRun,
                        ExecutedByUserId = userId,
                        IsSuccess = false,
                        ExecutedActions = new List<ExecutedActionDto>(),
                        Errors = new List<string> { ex.Message },
                        Warnings = new List<string>()
                    };
                }
            });
        }

        // Overload method implementation
        public async Task<InventoryDistributionPlanDto> OptimizeInventoryDistributionAsync(OptimizationParametersDto parameters)
        {
            // Delegate to base method for now with null productId
            return await OptimizeInventoryDistributionAsync((int?)null);
        }
    }

    // Supporting DTOs and classes - removed duplicates to use DTOs from Berca_Backend.DTOs namespace

    public class BranchInventoryOptimizationDto
    {
        public DateTime GeneratedAt { get; set; }
        public List<BranchInventoryOptimization> BranchOptimizations { get; set; } = new();
        public decimal TotalCurrentValue { get; set; }
        public decimal TotalOptimizedValue { get; set; }
        public decimal PotentialSavings { get; set; }
        public InventoryOptimizationSummary Summary { get; set; } = new();
    }

    public class InterBranchTransferRequest
    {
        public int FromBranchId { get; set; }
        public int ToBranchId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public string Reason { get; set; } = string.Empty;
        public int RequestedByUserId { get; set; }
    }

    // Supporting data structures
    public class BranchSalesData
    {
        public decimal TotalRevenue { get; set; }
        public int TransactionCount { get; set; }
        public decimal AverageTransactionValue { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal NetProfitMargin { get; set; }
    }

    public class BranchInventoryData
    {
        public decimal InventoryTurnover { get; set; }
        public decimal StockoutRate { get; set; }
        public decimal ExcessStockValue { get; set; }
    }

    public class BranchExpiryData
    {
        public decimal WastePercentage { get; set; }
        public decimal PreventionScore { get; set; }
        public decimal ValuePreserved { get; set; }
    }

    public class BestPractice
    {
        public string Category { get; set; } = string.Empty;
        public string Practice { get; set; } = string.Empty;
        public string Impact { get; set; } = string.Empty;
    }

    public class BranchInventoryOptimization
    {
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public decimal CurrentInventoryValue { get; set; }
        public decimal OptimizedInventoryValue { get; set; }
        public List<ProductOptimization> OverstockedItems { get; set; } = new();
        public List<ProductOptimization> UnderstockedItems { get; set; } = new();
        public List<TransferRecommendation> TransferRecommendations { get; set; } = new();
    }

    public class ProductOptimization
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public int OptimalStock { get; set; }
        public int ExcessQuantity { get; set; }
        public int ShortageQuantity { get; set; }
        public decimal ValueImpact { get; set; }
    }

    public class TransferRecommendation
    {
        public int ToSranchId { get; set; }
        public string ToBranchName { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int RecommendedQuantity { get; set; }
        public decimal EstimatedSavings { get; set; }
    }

    public class TransferLogistics
    {
        public string EstimatedTransitTime { get; set; } = string.Empty;
        public string RequiredVehicle { get; set; } = string.Empty;
        public string SpecialHandling { get; set; } = string.Empty;
    }

    public class InventoryOptimizationSummary
    {
        public int TotalOverstockedItems { get; set; }
        public int TotalUnderstockedItems { get; set; }
        public int TotalTransferRecommendations { get; set; }
        public string EstimatedImplementationTime { get; set; } = string.Empty;
    }

    public class BranchStockInfo
    {
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public int Stock { get; set; }
        public int OptimalStock { get; set; }
    }
}