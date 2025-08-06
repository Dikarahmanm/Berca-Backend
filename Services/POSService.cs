// Services/POSService.cs - FIXED: Remove all tax calculations
using Berca_Backend.DTOs;
using Berca_Backend.Models;
using Berca_Backend.Data;
using Microsoft.EntityFrameworkCore;
using Berca_Backend.Extensions;
using Berca_Backend.Services.Interfaces;

namespace Berca_Backend.Services
{
    public class POSService : IPOSService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<POSService> _logger;
        private readonly IProductService _productService;
        private readonly IMemberService _memberService;
        private readonly ITimezoneService _timezoneService;
        private readonly INotificationService _notificationService; // ✅ ADDED

        public POSService(AppDbContext context, ILogger<POSService> logger,
            IProductService productService, IMemberService memberService,
            ITimezoneService timezoneService, INotificationService notificationService) // ✅ ADDED
        {
            _context = context;
            _logger = logger;
            _productService = productService;
            _memberService = memberService;
            _timezoneService = timezoneService;
            _notificationService = notificationService; // ✅ ADDED
        }

        // ✅ BACKEND FIX: Services/POSService.cs - CreateSaleAsync Method
        public async Task<SaleDto> CreateSaleAsync(CreateSaleRequest request, int cashierId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Validate stock availability
                if (!await ValidateStockAvailabilityAsync(request.Items))
                    throw new InvalidOperationException("Insufficient stock for one or more items");

                // Generate sale number
                var saleNumber = await GenerateSaleNumberAsync();

                // Create sale
                var sale = new Sale
                {
                    SaleNumber = saleNumber,
                    SaleDate = _timezoneService.Now, // ✅ FIXED: Use Indonesia time directly
                    Subtotal = request.SubTotal,
                    DiscountAmount = request.DiscountAmount,
                    DiscountPercentage = request.DiscountPercentage,
                    TaxAmount = 0, // ✅ DISABLED: Always set to 0
                    Total = request.Total,
                    PaymentMethod = request.PaymentMethod,
                    AmountPaid = request.AmountPaid,
                    ChangeAmount = request.ChangeAmount,
                    MemberId = request.MemberId,
                    CashierId = cashierId,
                    Notes = request.Notes,
                    Status = SaleStatus.Completed,
                    ReceiptPrinted = false,
                    CreatedAt = _timezoneService.Now, // ✅ FIXED: Use Indonesia time directly
                    UpdatedAt = _timezoneService.Now  // ✅ FIXED: Use Indonesia time directly
                };

                _context.Sales.Add(sale);
                await _context.SaveChangesAsync();

                // ✅ CREATE SALE ITEMS WITH DUAL COMPATIBLE MAPPING
                foreach (var itemRequest in request.Items)
                {
                    var product = await _context.Products.FindAsync(itemRequest.ProductId);
                    if (product == null)
                        throw new KeyNotFoundException($"Product {itemRequest.ProductId} not found");

                    // ✅ Determine unit price (priority: SellPrice from request > Product price)
                    var unitPrice = itemRequest.SellPrice > 0 ? itemRequest.SellPrice : product.SellPrice;
                    var itemSubtotal = unitPrice * itemRequest.Quantity;

                    // ✅ Calculate discount amount (support both percentage and amount)
                    var itemDiscountAmount = 0m;
                    if (itemRequest.DiscountAmount > 0)
                    {
                        // Direct amount provided
                        itemDiscountAmount = itemRequest.DiscountAmount;
                    }
                    else if (itemRequest.Discount > 0)
                    {
                        // Percentage provided - convert to amount
                        itemDiscountAmount = itemSubtotal * (itemRequest.Discount / 100);
                    }

                    var finalSubtotal = itemSubtotal - itemDiscountAmount;

                    var saleItem = new SaleItem
                    {
                        SaleId = sale.Id,
                        ProductId = itemRequest.ProductId,
                        ProductName = product.Name,        // ✅ Snapshot
                        ProductBarcode = product.Barcode,  // ✅ Snapshot
                        Quantity = itemRequest.Quantity,
                        UnitPrice = unitPrice,             // ✅ Real sell price
                        UnitCost = product.BuyPrice,       // ✅ For profit calculation
                        DiscountAmount = itemDiscountAmount, // ✅ Calculated discount
                        Subtotal = finalSubtotal,          // ✅ Final amount
                        Unit = "pcs",                      // ✅ Default unit
                        Notes = null,
                        CreatedAt = _timezoneService.Now   // ✅ FIXED: Use Indonesia time directly
                    };

                    _context.SaleItems.Add(saleItem);

                    // Update product stock
                    await _productService.UpdateStockAsync(
                        itemRequest.ProductId,
                        itemRequest.Quantity,
                        MutationType.StockOut,
                        $"Sale #{saleNumber}",
                        saleNumber,
                        product.BuyPrice,
                        $"Cashier-{cashierId}"
                    );
                }

                // Handle member points (existing logic)
                if (request.MemberId.HasValue)
                {
                    var pointsEarned = CalculatePointsEarned(request.Total);
                    if (pointsEarned > 0)
                    {
                        await _memberService.AddPointsAsync(
                            request.MemberId.Value,
                            pointsEarned,
                            $"Purchase - Sale #{saleNumber}",
                            sale.Id,
                            saleNumber,
                            $"Cashier-{cashierId}"
                        );
                    }

                    // Handle point redemption
                    if (request.RedeemedPoints > 0)
                    {
                        await _memberService.RedeemPointsAsync(
                            request.MemberId.Value,
                            request.RedeemedPoints,
                            $"Point redemption - Sale #{saleNumber}",
                            saleNumber,
                            $"Cashier-{cashierId}"
                        );
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // ✅ CREATE SALE COMPLETED NOTIFICATION - MISSING LOGIC!
                try
                {
                    await _notificationService.CreateSaleCompletedNotificationAsync(
                        sale.Id, 
                        sale.SaleNumber, 
                        sale.Total
                    );
                    
                    _logger.LogInformation("✅ Sale notification created for sale: {SaleNumber}", sale.SaleNumber);
                }
                catch (Exception notificationEx)
                {
                    // Don't fail the sale if notification fails
                    _logger.LogWarning(notificationEx, "⚠️ Failed to create sale notification for sale: {SaleNumber}", sale.SaleNumber);
                }

                // Return complete sale data
                return await GetSaleByIdAsync(sale.Id) ?? throw new Exception("Sale created but not found");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating sale");
                throw;
            }
        }

        public async Task<SaleDto?> GetSaleByIdAsync(int id)
        {
            try
            {
                return await _context.Sales
                    .Include(s => s.SaleItems)
                        .ThenInclude(si => si.Product)
                    .Include(s => s.Member)
                    .Include(s => s.Cashier)
                    .Where(s => s.Id == id)
                    .Select(s => new SaleDto
                    {
                        Id = s.Id,
                        SaleNumber = s.SaleNumber,
                        SaleDate = s.SaleDate,
                        Subtotal = s.Subtotal,
                        DiscountAmount = s.DiscountAmount,
                        DiscountPercentage = s.DiscountPercentage,
                        TaxAmount = s.TaxAmount,
                        Total = s.Total,
                        PaymentMethod = s.PaymentMethod,
                        AmountPaid = s.AmountPaid,
                        ChangeAmount = s.ChangeAmount,
                        Status = s.Status.ToString(),
                        Notes = s.Notes,
                        MemberId = s.MemberId,
                        MemberName = s.Member != null ? s.Member.Name : null,
                        MemberNumber = s.Member != null ? s.Member.MemberNumber : null,
                        CashierId = s.CashierId,
                        CashierName = s.Cashier.UserProfile != null ? s.Cashier.UserProfile.FullName : s.Cashier.Username,
                        ReceiptPrinted = s.ReceiptPrinted,
                        Items = s.SaleItems.Select(si => new SaleItemDto
                        {
                            Id = si.Id,
                            ProductId = si.ProductId,
                            ProductName = si.Product.Name,
                            ProductBarcode = si.Product.Barcode,
                            Quantity = si.Quantity,
                            UnitPrice = si.UnitPrice,
                            DiscountAmount = si.DiscountAmount,
                            Subtotal = si.Subtotal,
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sale by ID: {SaleId}", id);
                throw;
            }
        }

        public async Task<SaleDto?> GetSaleByNumberAsync(string saleNumber)
        {
            try
            {
                return await _context.Sales
                    .Include(s => s.SaleItems)
                        .ThenInclude(si => si.Product)
                    .Include(s => s.Member)
                    .Include(s => s.Cashier)
                    .Where(s => s.SaleNumber == saleNumber)
                    .Select(s => new SaleDto
                    {
                        Id = s.Id,
                        SaleNumber = s.SaleNumber,
                        SaleDate = s.SaleDate,
                        Subtotal = s.Subtotal,
                        DiscountAmount = s.DiscountAmount,
                        DiscountPercentage = s.DiscountPercentage,
                        TaxAmount = s.TaxAmount,
                        Total = s.Total,
                        PaymentMethod = s.PaymentMethod,
                        AmountPaid = s.AmountPaid,
                        ChangeAmount = s.ChangeAmount,
                        Status = s.Status.ToString(),
                        Notes = s.Notes,
                        MemberId = s.MemberId,
                        MemberName = s.Member != null ? s.Member.Name : null,
                        MemberNumber = s.Member != null ? s.Member.MemberNumber : null,
                        CashierId = s.CashierId,
                        CashierName = s.Cashier.UserProfile != null ? s.Cashier.UserProfile.FullName : s.Cashier.Username,
                        ReceiptPrinted = s.ReceiptPrinted,
                        Items = s.SaleItems.Select(si => new SaleItemDto
                        {
                            Id = si.Id,
                            ProductId = si.ProductId,
                            ProductName = si.Product.Name,
                            ProductBarcode = si.Product.Barcode,
                            Quantity = si.Quantity,
                            UnitPrice = si.UnitPrice,
                            DiscountAmount = si.DiscountAmount,
                            Subtotal = si.Subtotal,
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sale by number: {SaleNumber}", saleNumber);
                throw;
            }
        }

        public async Task<List<SaleDto>> GetSalesAsync(DateTime? startDate = null, DateTime? endDate = null,
            int? cashierId = null, string? paymentMethod = null, int page = 1, int pageSize = 20)
        {
            try
            {
                var query = _context.Sales
                    .Include(s => s.Member)
                    .Include(s => s.Cashier)
                    .AsQueryable();

                if (startDate.HasValue)
                    query = query.Where(s => s.SaleDate >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(s => s.SaleDate <= endDate.Value);

                if (cashierId.HasValue)
                    query = query.Where(s => s.CashierId == cashierId.Value);

                if (!string.IsNullOrEmpty(paymentMethod))
                    query = query.Where(s => s.PaymentMethod == paymentMethod);

                return await query
                    .OrderByDescending(s => s.SaleDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(s => new SaleDto
                    {
                        Id = s.Id,
                        SaleNumber = s.SaleNumber,
                        SaleDate = s.SaleDate,         // <-- renamed
                        Subtotal = s.Subtotal,
                        DiscountAmount = s.DiscountAmount,
                        DiscountPercentage = s.DiscountPercentage,
                        TaxAmount = s.TaxAmount,
                        Total = s.Total,
                        PaymentMethod = s.PaymentMethod,
                        AmountPaid = s.AmountPaid,
                        ChangeAmount = s.ChangeAmount,
                        Status = s.Status.ToString(),
                        Notes = s.Notes,
                        MemberId = s.MemberId,
                        MemberName = s.Member != null ? s.Member.Name : null,
                        MemberNumber = s.Member != null ? s.Member.MemberNumber : null,
                        CashierId = s.CashierId,
                        CashierName = s.Cashier.UserProfile != null ? s.Cashier.UserProfile.FullName : s.Cashier.Username,
                        ReceiptPrinted = s.ReceiptPrinted,
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sales");
                throw;
            }
        }

        public async Task<bool> MarkReceiptPrintedAsync(int saleId)
        {
            try
            {
                var sale = await _context.Sales.FindAsync(saleId);
                if (sale == null) return false;

                sale.ReceiptPrinted = true;
                sale.ReceiptPrintedAt = _timezoneService.Now; // ✅ FIXED: Use Indonesia time

                await _context.SaveChangesAsync();
                
                // ✅ OPTIONAL: Create receipt printed notification
                try
                {
                    await _notificationService.BroadcastToAllUsersAsync(
                        "receipt_printed",
                        "Struk Dicetak",
                        $"Struk untuk transaksi {sale.SaleNumber} telah dicetak",
                        $"/sales/{sale.Id}"
                    );
                }
                catch (Exception notificationEx)
                {
                    _logger.LogWarning(notificationEx, "Failed to create receipt printed notification");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking receipt as printed: {SaleId}", saleId);
                throw;
            }
        }

        public async Task<ReceiptDataDto> GetReceiptDataAsync(int saleId)
        {
            try
            {
                var sale = await GetSaleByIdAsync(saleId);
                if (sale == null)
                    throw new KeyNotFoundException($"Sale {saleId} not found");

                return new ReceiptDataDto
                {
                    Sale = sale,
                    StoreName = "Toko Eniwan",
                    StoreAddress = "Jl. Raya Bekasi No. 123, Bekasi, Jawa Barat",
                    StorePhone = "+62 21 1234567",
                    StoreEmail = "info@tokoeniwan.com",
                    FooterMessage = "Terima kasih atas kunjungan Anda!"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting receipt data for sale: {SaleId}", saleId);
                throw;
            }
        }

        public async Task<bool> CancelSaleAsync(int saleId, string reason)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var sale = await _context.Sales
                    .Include(s => s.SaleItems)
                    .FirstOrDefaultAsync(s => s.Id == saleId);

                if (sale == null) return false;

                // Restore stock for each item
                foreach (var item in sale.SaleItems)
                {
                    await _productService.UpdateStockAsync(
                        item.ProductId,
                        item.Quantity,
                        MutationType.StockIn,
                        $"Sale cancellation - {reason}",
                        sale.SaleNumber,
                        null,
                        "System"
                    );
                }

                // Handle member points refund
                if (sale.MemberId.HasValue)
                {
                    var pointsToRefund = CalculatePointsEarned(sale.Total);
                    if (pointsToRefund > 0)
                    {
                        await _memberService.RedeemPointsAsync(
                            sale.MemberId.Value,
                            pointsToRefund,
                            $"Sale cancellation refund - Sale #{sale.SaleNumber}",
                            sale.SaleNumber,
                            "System"
                        );
                    }
                }

                sale.Status = SaleStatus.Cancelled;
                sale.CancelledAt = _timezoneService.Now;
                sale.CancellationReason = reason;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // ✅ CREATE SALE CANCELLED NOTIFICATION
                try
                {
                    await _notificationService.CreateSaleCancelledNotificationAsync(
                        sale.Id, 
                        sale.SaleNumber, 
                        sale.Total, 
                        reason
                    );
                    
                    _logger.LogInformation("✅ Sale cancellation notification created for sale: {SaleNumber}", sale.SaleNumber);
                }
                catch (Exception notificationEx)
                {
                    _logger.LogWarning(notificationEx, "⚠️ Failed to create sale cancellation notification for sale: {SaleNumber}", sale.SaleNumber);
                }

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error cancelling sale: {SaleId}", saleId);
                throw;
            }
        }

        public async Task<SaleDto> RefundSaleAsync(int saleId, string reason, int processedBy)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var originalSale = await _context.Sales
                    .Include(s => s.SaleItems)
                    .FirstOrDefaultAsync(s => s.Id == saleId);

                if (originalSale == null)
                    throw new KeyNotFoundException($"Sale {saleId} not found");

                // Create refund sale (negative amounts)
                var refundSaleNumber = await GenerateRefundSaleNumberAsync(originalSale.SaleNumber);

                var refundSale = new Sale
                {
                    SaleNumber = refundSaleNumber,
                    SaleDate = _timezoneService.Now,
                    Subtotal = -originalSale.Subtotal,
                    DiscountAmount = -originalSale.DiscountAmount,
                    TaxAmount = 0, // ✅ DISABLED: Always set to 0 for refunds too
                    Total = -originalSale.Total,
                    PaymentMethod = originalSale.PaymentMethod,
                    AmountPaid = -originalSale.AmountPaid,
                    ChangeAmount = -originalSale.ChangeAmount,
                    MemberId = originalSale.MemberId,
                    CashierId = processedBy,
                    Notes = $"Refund for Sale #{originalSale.SaleNumber} - {reason}",
                    Status = SaleStatus.Refunded,
                    OriginalSaleId = originalSale.Id
                };

                _context.Sales.Add(refundSale);
                await _context.SaveChangesAsync();

                // Create refund sale items and restore stock
                foreach (var originalItem in originalSale.SaleItems)
                {
                    var refundItem = new SaleItem
                    {
                        SaleId = refundSale.Id,
                        ProductId = originalItem.ProductId,
                        Quantity = -originalItem.Quantity,
                        UnitPrice = originalItem.UnitPrice,
                        DiscountAmount = -originalItem.DiscountAmount,
                        Subtotal = -originalItem.Subtotal
                    };

                    _context.SaleItems.Add(refundItem);

                    // Restore stock
                    await _productService.UpdateStockAsync(
                        originalItem.ProductId,
                        originalItem.Quantity,
                        MutationType.StockIn,
                        $"Refund - {reason}",
                        refundSaleNumber,
                        null,
                        $"User-{processedBy}"
                    );
                }

                // Handle member points refund
                if (originalSale.MemberId.HasValue)
                {
                    var pointsToRefund = CalculatePointsEarned(originalSale.Total);
                    if (pointsToRefund > 0)
                    {
                        await _memberService.RedeemPointsAsync(
                            originalSale.MemberId.Value,
                            pointsToRefund,
                            $"Refund points - Sale #{originalSale.SaleNumber}",
                            refundSaleNumber,
                            $"User-{processedBy}"
                        );
                    }
                }

                // Mark original sale as refunded
                originalSale.Status = SaleStatus.Refunded;
                originalSale.RefundedAt = _timezoneService.Now;
                originalSale.RefundReason = reason;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // ✅ CREATE SALE REFUNDED NOTIFICATION
                try
                {
                    await _notificationService.CreateSaleRefundedNotificationAsync(
                        originalSale.Id, 
                        originalSale.SaleNumber, 
                        originalSale.Total, 
                        reason
                    );
                    
                    _logger.LogInformation("✅ Sale refund notification created for sale: {SaleNumber}", originalSale.SaleNumber);
                }
                catch (Exception notificationEx)
                {
                    _logger.LogWarning(notificationEx, "⚠️ Failed to create sale refund notification for sale: {SaleNumber}", originalSale.SaleNumber);
                }

                return await GetSaleByIdAsync(refundSale.Id) ?? throw new Exception("Refund created but not found");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error processing refund for sale: {SaleId}", saleId);
                throw;
            }
        }

        public async Task<SaleSummaryDto> GetSalesSummaryAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var sales = await _context.Sales
                    .Where(s => s.SaleDate >= startDate && s.SaleDate <= endDate && s.Status == SaleStatus.Completed)
                    .ToListAsync();

                return new SaleSummaryDto
                {
                    TotalSales = sales.Sum(s => s.Total),
                    TransactionCount = sales.Count,
                    AverageTransaction = sales.Any() ? sales.Average(s => s.Total) : 0,
                    TotalDiscount = sales.Sum(s => s.DiscountAmount),
                    TotalTax = 0, // ✅ DISABLED: Always return 0
                    StartDate = startDate,
                    EndDate = endDate
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales summary");
                throw;
            }
        }

        public async Task<List<DailySalesDto>> GetDailySalesAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                return await _context.Sales
                    .Where(s => s.SaleDate >= startDate && s.SaleDate <= endDate && s.Status == SaleStatus.Completed)
                    .GroupBy(s => s.SaleDate.Date)
                    .Select(g => new DailySalesDto
                    {
                        Date = g.Key,
                        TotalSales = g.Sum(s => s.Total),
                        TransactionCount = g.Count(),
                        AverageTransaction = g.Average(s => s.Total)
                    })
                    .OrderBy(d => d.Date)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily sales");
                throw;
            }
        }

        public async Task<List<PaymentMethodSummaryDto>> GetPaymentMethodSummaryAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var totalSales = await _context.Sales
                    .Where(s => s.SaleDate >= startDate && s.SaleDate <= endDate && s.Status == SaleStatus.Completed)
                    .SumAsync(s => s.Total);

                return await _context.Sales
                    .Where(s => s.SaleDate >= startDate && s.SaleDate <= endDate && s.Status == SaleStatus.Completed)
                    .GroupBy(s => s.PaymentMethod)
                    .Select(g => new PaymentMethodSummaryDto
                    {
                        PaymentMethod = g.Key,
                        Total = g.Sum(s => s.Total),
                        TransactionCount = g.Count(),
                        Percentage = totalSales > 0 ? (g.Sum(s => s.Total) / totalSales) * 100 : 0
                    })
                    .OrderByDescending(p => p.Total)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment method summary");
                throw;
            }
        }

        public async Task<bool> ValidateStockAvailabilityAsync(List<CreateSaleItemRequest> items)
        {
            try
            {
                foreach (var item in items)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product == null || !product.IsActive)
                        return false;

                    if (product.Stock < item.Quantity)
                        return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating stock availability");
                throw;
            }
        }

        public async Task<decimal> CalculateTotalAsync(List<CreateSaleItemRequest> items, decimal discountAmount = 0, decimal taxAmount = 0)
        {
            try
            {
                decimal subtotal = 0;

                foreach (var item in items)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        var itemTotal = (product.SellPrice * item.Quantity) - item.DiscountAmount;
                        subtotal += itemTotal;
                    }
                }

                // ✅ DISABLED: Ignore taxAmount parameter, always return subtotal - discount
                return subtotal - discountAmount; // Remove + taxAmount
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating total");
                throw;
            }
        }

        // Private helper methods
        private async Task<string> GenerateSaleNumberAsync()
        {
            var today = _timezoneService.Today; // ✅ FIXED: Use Indonesia time consistently
            var dailyCount = await _context.Sales
                .Where(s => s.SaleDate.Date == today)
                .CountAsync();

            return $"TRX-{_timezoneService.Now:yyyyMMdd}-{(dailyCount + 1):D4}"; // ✅ FIXED: Both use Indonesia time
        }

        private async Task<string> GenerateRefundSaleNumberAsync(string originalSaleNumber)
        {
            var refundCount = await _context.Sales
                .Where(s => s.SaleNumber.StartsWith($"REF-{originalSaleNumber}"))
                .CountAsync();

            return $"REF-{originalSaleNumber}-{(refundCount + 1):D2}";
        }

        private int CalculatePointsEarned(decimal totalAmount)
        {
            // 1 point per 1000 IDR spent
            return (int)Math.Floor(totalAmount / 1000);
        }
    }
}