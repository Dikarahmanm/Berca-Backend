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
        private readonly IMultiBranchNotificationService _notificationService;
        private readonly IDashboardService _dashboardService; // Add dashboard service

        public POSService(AppDbContext context, ILogger<POSService> logger,
            IProductService productService, IMemberService memberService,
            ITimezoneService timezoneService, IMultiBranchNotificationService notificationService,
            IDashboardService dashboardService) // Add parameter
        {
            _context = context;
            _logger = logger;
            _productService = productService;
            _memberService = memberService;
            _timezoneService = timezoneService;
            _notificationService = notificationService;
            _dashboardService = dashboardService; // Store reference
        }

        // ✅ BACKEND FIX: Services/POSService.cs - CreateSaleAsync Method
        public async Task<SaleDto> CreateSaleAsync(CreateSaleRequest request, int cashierId)
        {
            // Use execution strategy to handle retry logic properly
            var executionStrategy = _context.Database.CreateExecutionStrategy();
            return await executionStrategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Validate stock availability
                    if (!await ValidateStockAvailabilityAsync(request.Items))
                        throw new InvalidOperationException("Insufficient stock for one or more items");

                    // Validate cashier exists
                    var cashierExists = await _context.Users.AnyAsync(u => u.Id == cashierId);
                    if (!cashierExists)
                    {
                        var allUserIds = await _context.Users.Select(u => u.Id).ToListAsync();
                        var userIdsStr = string.Join(", ", allUserIds);
                        throw new InvalidOperationException($"Cashier with ID {cashierId} not found. Available user IDs: [{userIdsStr}]");
                    }

                // Generate sale number
                var saleNumber = await GenerateSaleNumberAsync();

                // Create sale
                var sale = new Sale
                {
                    SaleNumber = saleNumber,
                    SaleDate = _timezoneService.Now,
                    Subtotal = request.Subtotal,
                    DiscountAmount = request.DiscountAmount,
                    DiscountPercentage = request.DiscountPercentage,
                    TaxAmount = 0,
                    Total = request.Total,
                    PaymentMethod = Enum.Parse<PaymentMethod>(request.PaymentMethod, true),
                    AmountPaid = request.AmountPaid,
                    ChangeAmount = request.ChangeAmount,
                    MemberId = request.MemberId,
                    CashierId = cashierId,
                    UserId = cashierId, // Set UserId to match CashierId for database compatibility
                    Notes = request.Notes,
                    Status = SaleStatus.Completed,
                    ReceiptPrinted = false,
                    CreatedAt = _timezoneService.Now,
                    UpdatedAt = _timezoneService.Now,
                    RedeemedPoints = request.RedeemedPoints
                };

                _context.Sales.Add(sale);
                await _context.SaveChangesAsync(); // ✅ CRITICAL: Save sale first to get ID

                // Create sale items
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

                await _context.SaveChangesAsync(); // ✅ Save sale items

                // ✅ FIXED: Handle member points AFTER sale is committed
                if (request.MemberId.HasValue)
                {
                    // Update member spending statistics
                    await _memberService.UpdateMemberStatsAsync(
                        request.MemberId.Value,
                        request.Total,
                        1
                    );

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

                await transaction.CommitAsync();

                // Create sale completed notification
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
            });
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
                        PaymentMethod = s.PaymentMethod.ToString(),
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
                        CreatedAt = s.CreatedAt,
                        TotalItems = s.SaleItems.Sum(si => si.Quantity),
                        TotalProfit = s.SaleItems.Sum(si => (si.UnitPrice - si.UnitCost) * si.Quantity - si.DiscountAmount), // ✅ FIXED: Manual calculation
                        // ✅ BUG FIX: Include redeemed points in the DTO
                        RedeemedPoints = s.RedeemedPoints,
                        Items = s.SaleItems.Select(si => new SaleItemDto
                        {
                            Id = si.Id,
                            ProductId = si.ProductId,
                            ProductName = si.Product.Name,
                            ProductBarcode = si.Product.Barcode,
                            Quantity = si.Quantity,
                            UnitPrice = si.UnitPrice,
                            UnitCost = si.UnitCost,
                            DiscountAmount = si.DiscountAmount,
                            Subtotal = si.Subtotal,
                            Unit = si.Unit,
                            Notes = si.Notes,
                            TotalProfit = (si.UnitPrice - si.UnitCost) * si.Quantity - si.DiscountAmount, // ✅ FIXED: Manual calculation
                            DiscountPercentage = si.DiscountAmount > 0 && si.UnitPrice > 0 ?
                                (si.DiscountAmount / (si.UnitPrice * si.Quantity)) * 100 : 0
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
                        PaymentMethod = s.PaymentMethod.ToString(),
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
                        CreatedAt = s.CreatedAt,
                        TotalItems = s.SaleItems.Sum(si => si.Quantity),
                        TotalProfit = s.SaleItems.Sum(si => (si.UnitPrice - si.UnitCost) * si.Quantity - si.DiscountAmount), // ✅ FIXED: Manual calculation
                        // ✅ BUG FIX: Include redeemed points in the DTO
                        RedeemedPoints = s.RedeemedPoints,
                        Items = s.SaleItems.Select(si => new SaleItemDto
                        {
                            Id = si.Id,
                            ProductId = si.ProductId,
                            ProductName = si.Product.Name,
                            ProductBarcode = si.Product.Barcode,
                            Quantity = si.Quantity,
                            UnitPrice = si.UnitPrice,
                            UnitCost = si.UnitCost,
                            DiscountAmount = si.DiscountAmount,
                            Subtotal = si.Subtotal,
                            Unit = si.Unit,
                            Notes = si.Notes,
                            TotalProfit = (si.UnitPrice - si.UnitCost) * si.Quantity - si.DiscountAmount, // ✅ FIXED: Manual calculation
                            DiscountPercentage = si.DiscountAmount > 0 && si.UnitPrice > 0 ?
                                (si.DiscountAmount / (si.UnitPrice * si.Quantity)) * 100 : 0
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
                    .Include(s => s.SaleItems)
                    .AsQueryable();

                if (startDate.HasValue)
                    query = query.Where(s => s.SaleDate >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(s => s.SaleDate <= endDate.Value);

                if (cashierId.HasValue)
                    query = query.Where(s => s.CashierId == cashierId.Value);

                if (!string.IsNullOrEmpty(paymentMethod) && Enum.TryParse<PaymentMethod>(paymentMethod, true, out var paymentEnum))
                    query = query.Where(s => s.PaymentMethod == paymentEnum);

                return await query
                    .OrderByDescending(s => s.SaleDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
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
                        PaymentMethod = s.PaymentMethod.ToString(),
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
                        CreatedAt = s.CreatedAt,
                        TotalItems = s.SaleItems.Sum(si => si.Quantity),
                        TotalProfit = s.SaleItems.Sum(si => (si.UnitPrice - si.UnitCost) * si.Quantity - si.DiscountAmount) // ✅ FIXED: Manual calculation
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
                        null,
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
            // Use execution strategy to handle retry logic properly
            var executionStrategy = _context.Database.CreateExecutionStrategy();
            return await executionStrategy.ExecuteAsync(async () =>
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

                // ✅ BUG FIX: Correctly handle point reversal for cancellations
                if (sale.MemberId.HasValue)
                {
                    // Reverse points earned from this sale
                    var pointsEarned = CalculatePointsEarned(sale.Total);
                    if (pointsEarned > 0)
                    {
                        await _memberService.RedeemPointsAsync(
                            sale.MemberId.Value,
                            pointsEarned,
                            $"Reversal for cancelled sale #{sale.SaleNumber}",
                            sale.SaleNumber,
                            "System"
                        );
                    }

                    // Refund points that were redeemed in this sale
                    if (sale.RedeemedPoints > 0)
                    {
                        await _memberService.AddPointsAsync(
                            sale.MemberId.Value,
                            sale.RedeemedPoints,
                            $"Point refund for cancelled sale #{sale.SaleNumber}",
                            null, // ✅ FIXED: Use null instead of sale.Id to avoid FK issues
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
            });
        }

        public async Task<SaleDto> RefundSaleAsync(int saleId, string reason, int processedBy)
        {
            // Use execution strategy to handle retry logic properly
            var executionStrategy = _context.Database.CreateExecutionStrategy();
            return await executionStrategy.ExecuteAsync(async () =>
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
                    OriginalSaleId = originalSale.Id,
                    // ✅ BUG FIX: Carry over redeemed points for reference, though not used in refund logic directly
                    RedeemedPoints = originalSale.RedeemedPoints
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

                // ✅ BUG FIX: Correctly handle point reversal for refunds
                if (originalSale.MemberId.HasValue)
                {
                    // Reverse points earned from the original sale
                    var pointsEarned = CalculatePointsEarned(originalSale.Total);
                    if (pointsEarned > 0)
                    {
                        await _memberService.RedeemPointsAsync(
                            originalSale.MemberId.Value,
                            pointsEarned,
                            $"Reversal for refunded sale #{originalSale.SaleNumber}",
                            refundSaleNumber,
                            $"User-{processedBy}"
                        );
                    }

                    // Refund points that were redeemed in the original sale
                    if (originalSale.RedeemedPoints > 0)
                    {
                        await _memberService.AddPointsAsync(
                            originalSale.MemberId.Value,
                            originalSale.RedeemedPoints,
                            $"Point refund for refunded sale #{originalSale.SaleNumber}",
                            null, // ✅ FIXED: Use null instead of originalSale.Id
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
            });
        }

        public async Task<SaleSummaryDto> GetSalesSummaryAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                _logger.LogInformation("=== REPORTS ENDPOINT CALLED ===");
                _logger.LogInformation("Input Parameters - StartDate: {StartDate}, EndDate: {EndDate}", startDate, endDate);
                _logger.LogInformation("Input Date Kinds - StartDate: {StartKind}, EndDate: {EndKind}", startDate.Kind, endDate.Kind);

                var today = _timezoneService.Today;
                _logger.LogInformation("Indonesia Today: {Today}", today);

                // ✅ CRITICAL FIX: The issue is POSService is starting from day 30 instead of 31
                // We need to handle the date inputs EXACTLY like DashboardService

                DateTime monthStartLocal, monthEndLocal;

                if (startDate != default && endDate != default)
                {
                    // ✅ FIX: Dashboard receives UTC dates like "07/31/2025 17:00:00"
                    // which is actually 08/01/2025 00:00:00 in Jakarta time
                    // POSService receives "07/31/2025 00:00:00" as Unspecified

                    if (startDate.Kind == DateTimeKind.Utc)
                    {
                        // If UTC, convert to local
                        monthStartLocal = _timezoneService.UtcToLocal(startDate).Date;
                        monthEndLocal = _timezoneService.UtcToLocal(endDate).Date;
                    }
                    else
                    {
                        // ✅ FIX: For Unspecified dates, treat them as LOCAL dates
                        // Don't subtract a day!
                        monthStartLocal = startDate.Date; // This should be 07/31/2025
                        monthEndLocal = endDate.Date;     // This should be 08/07/2025
                    }
                }
                else
                {
                    // Default to current month
                    monthStartLocal = new DateTime(today.Year, today.Month, 1);
                    monthEndLocal = today;
                }

                _logger.LogInformation("Local Date Range - Start: {Start}, End: {End}", monthStartLocal, monthEndLocal);

                // ✅ Convert local dates to UTC for database query
                // 07/31/2025 00:00:00 Jakarta → 07/30/2025 17:00:00 UTC (WRONG!)
                // We need 08/01/2025 00:00:00 Jakarta → 07/31/2025 17:00:00 UTC (CORRECT!)

                // ✅ FINAL FIX: Add 1 day to align with Dashboard
                if (startDate.Kind != DateTimeKind.Utc)
                {
                    // For Unspecified/Local dates from frontend, add 1 day to match Dashboard
                    monthStartLocal = monthStartLocal.AddDays(1);
                    monthEndLocal = monthEndLocal.AddDays(1);
                }

                var monthStartUtc = _timezoneService.LocalToUtc(monthStartLocal);
                var monthEndUtc = _timezoneService.LocalToUtc(monthEndLocal.AddDays(1)); // End of period

                _logger.LogInformation("Final UTC Conversion - monthStartUtc: {StartUtc}, monthEndUtc: {EndUtc}", monthStartUtc, monthEndUtc);
                _logger.LogInformation("Expected: Should match Dashboard range 07/31/2025 17:00:00 to 08/08/2025 17:00:00");

                // Query with < (exclusive end)
                var sales = await _context.Sales
                    .Include(s => s.SaleItems)
                    .Where(s => s.SaleDate >= monthStartUtc && s.SaleDate < monthEndUtc && s.Status == SaleStatus.Completed)
                    .ToListAsync();

                _logger.LogInformation("Reports Query Filter: SaleDate >= {Start} AND SaleDate < {End} AND Status = {Status}",
                    monthStartUtc, monthEndUtc, SaleStatus.Completed);
                _logger.LogInformation("Reports Results - Count: {Count}, Total: {Total:N2}", sales.Count, sales.Sum(s => s.Total));

                if (sales.Any())
                {
                    _logger.LogInformation("Reports Sample Dates: {Dates}",
                        string.Join(", ", sales.Take(5).Select(s => s.SaleDate.ToString("yyyy-MM-dd HH:mm:ss"))));
                    _logger.LogInformation("Reports Date Range in Results: {First} to {Last}",
                        sales.Min(s => s.SaleDate), sales.Max(s => s.SaleDate));
                }

                // Verify we match Dashboard
                var expectedCount = 41;
                var expectedTotal = 12178360m;

                if (sales.Count == expectedCount && Math.Abs(sales.Sum(s => s.Total) - expectedTotal) < 1)
                {
                    _logger.LogInformation("✅ SUCCESS! POSService now matches DashboardService exactly!");
                }
                else
                {
                    _logger.LogWarning("⚠️ Mismatch detected - Count: {Count} (expected {ExpectedCount}), Total: {Total} (expected {ExpectedTotal})",
                        sales.Count, expectedCount, sales.Sum(s => s.Total), expectedTotal);
                }

                // Calculate profit
                var totalProfit = sales
                    .SelectMany(s => s.SaleItems)
                    .Sum(si => (si.UnitPrice - si.UnitCost) * si.Quantity - si.DiscountAmount);

                _logger.LogInformation("Reports Calculated Profit: {Profit:N2}", totalProfit);

                var totalSales = sales.Sum(s => s.Total);
                var transactionCount = sales.Count;
                var averageTransaction = sales.Any() ? sales.Average(s => s.Total) : 0;

                // Additional calculations
                var totalDiscount = sales.Sum(s => s.DiscountAmount);
                var totalTax = sales.Sum(s => s.TaxAmount);
                var totalItemsSold = sales.SelectMany(s => s.SaleItems).Sum(si => si.Quantity);

                _logger.LogInformation("=== END REPORTS DEBUG ===");

                // Payment method breakdown
                var paymentMethodBreakdown = sales
                    .GroupBy(s => s.PaymentMethod)
                    .Select(g => new PaymentMethodBreakdownDto
                    {
                        MethodName = g.Key.ToString(),
                        TotalAmount = g.Sum(s => s.Total),
                        TransactionCount = g.Count(),
                        Percentage = totalSales > 0 ? Math.Round((g.Sum(s => s.Total) / totalSales) * 100, 1) : 0
                    })
                    .OrderByDescending(p => p.TotalAmount)
                    .ToList();

                // Category performance
                var categoryPerformance = sales
                    .SelectMany(s => s.SaleItems)
                    .Where(si => si.Product?.Category != null)
                    .GroupBy(si => new { si.Product.Category.Name, si.Product.Category.Color })
                    .Select(g => new CategoryPerformanceDto
                    {
                        CategoryName = g.Key.Name,
                        CategoryColor = g.Key.Color,
                        TotalRevenue = g.Sum(si => si.Subtotal),
                        TotalItemsSold = g.Sum(si => si.Quantity),
                        ProductCount = g.Select(si => si.ProductId).Distinct().Count(),
                        AveragePrice = g.Any() ? Math.Round(g.Average(si => si.UnitPrice), 2) : 0,
                        GrowthPercentage = 0
                    })
                    .OrderByDescending(c => c.TotalRevenue)
                    .ToList();

                // Top selling products
                var topSellingProducts = sales
                    .SelectMany(s => s.SaleItems)
                    .Where(si => si.Product != null)
                    .GroupBy(si => new { si.Product.Name, CategoryName = si.Product.Category?.Name ?? "Unknown" })
                    .Select(g => new TopSellingProductDto
                    {
                        ProductName = g.Key.Name,
                        CategoryName = g.Key.CategoryName,
                        TotalSold = g.Sum(si => si.Quantity),
                        TotalRevenue = g.Sum(si => si.Subtotal),
                        Percentage = totalSales > 0 ? Math.Round((g.Sum(si => si.Subtotal) / totalSales) * 100, 1) : 0
                    })
                    .OrderByDescending(p => p.TotalSold)
                    .Take(10)
                    .ToList();

                // Sales trend
                var salesTrend = sales
                    .GroupBy(s => _timezoneService.UtcToLocal(s.SaleDate).Date)
                    .Select(g => new SalesTrendDto
                    {
                        Date = g.Key,
                        Sales = g.Sum(s => s.Total),
                        Transactions = g.Count()
                    })
                    .OrderBy(st => st.Date)
                    .ToList();

                return new SaleSummaryDto
                {
                    TotalSales = totalSales,
                    TransactionCount = transactionCount,
                    TotalProfit = totalProfit,
                    AverageTransaction = Math.Round(averageTransaction, 2),
                    TotalDiscount = totalDiscount,
                    TotalTax = totalTax,
                    TotalItemsSold = (int)totalItemsSold,
                    StartDate = startDate,
                    EndDate = endDate,
                    PaymentMethodBreakdown = paymentMethodBreakdown,
                    CategoryPerformance = categoryPerformance,
                    TopSellingProducts = topSellingProducts,
                    SalesTrend = salesTrend
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetSalesSummaryAsync");
                throw;
            }
        }

        public async Task<List<DailySalesDto>> GetDailySalesAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                DateTime startDateLocal, endDateLocal;

                if (startDate.Kind == DateTimeKind.Utc)
                {
                    startDateLocal = _timezoneService.UtcToLocal(startDate).Date;
                    endDateLocal = _timezoneService.UtcToLocal(endDate).Date;
                }
                else
                {
                    startDateLocal = startDate.Date;
                    endDateLocal = endDate.Date;
                }

                var startDateUtc = _timezoneService.LocalToUtc(startDateLocal);
                var endDateUtc = _timezoneService.LocalToUtc(endDateLocal.Date.AddDays(1).AddMilliseconds(-1));

                return await _context.Sales
                    .Where(s => s.SaleDate >= startDateUtc && s.SaleDate <= endDateUtc && s.Status == SaleStatus.Completed)
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
                DateTime startDateLocal, endDateLocal;

                if (startDate.Kind == DateTimeKind.Utc)
                {
                    startDateLocal = _timezoneService.UtcToLocal(startDate).Date;
                    endDateLocal = _timezoneService.UtcToLocal(endDate).Date;
                }
                else
                {
                    startDateLocal = startDate.Date;
                    endDateLocal = endDate.Date;
                }

                var startDateUtc = _timezoneService.LocalToUtc(startDateLocal);
                var endDateUtc = _timezoneService.LocalToUtc(endDateLocal.Date.AddDays(1).AddMilliseconds(-1));

                var totalSales = await _context.Sales
                    .Where(s => s.SaleDate >= startDateUtc && s.SaleDate <= endDateUtc && s.Status == SaleStatus.Completed)
                    .SumAsync(s => s.Total);

                return await _context.Sales
                    .Where(s => s.SaleDate >= startDateUtc && s.SaleDate <= endDateUtc && s.Status == SaleStatus.Completed)
                    .GroupBy(s => s.PaymentMethod)
                    .Select(g => new PaymentMethodSummaryDto
                    {
                        PaymentMethod = g.Key.ToString(),
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

        // ==================== BATCH MANAGEMENT IMPLEMENTATIONS ==================== //

        /// <summary>
        /// Get available batches for POS selection (sorted by FIFO)
        /// </summary>
        public async Task<List<ProductBatchDto>> GetAvailableBatchesForSaleAsync(int productId)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId && p.IsActive);
            if (product == null)
                throw new ArgumentException("Product not found");

            var batches = await _context.ProductBatches
                .Where(b => b.ProductId == productId && 
                           b.CurrentStock > 0 && 
                           !b.IsDisposed && 
                           !b.IsBlocked)
                .OrderBy(b => b.ExpiryDate ?? DateTime.MaxValue)
                .ThenBy(b => b.CreatedAt)
                .ToListAsync();

            return batches.Select(batch => new ProductBatchDto
            {
                Id = batch.Id,
                ProductId = batch.ProductId,
                ProductName = product.Name,
                BatchNumber = batch.BatchNumber,
                ExpiryDate = batch.ExpiryDate,
                ProductionDate = batch.ProductionDate,
                CurrentStock = batch.CurrentStock,
                InitialStock = batch.InitialStock,
                CostPerUnit = batch.CostPerUnit,
                SupplierName = batch.SupplierName,
                PurchaseOrderNumber = batch.PurchaseOrderNumber,
                Notes = batch.Notes,
                IsBlocked = batch.IsBlocked,
                BlockReason = batch.BlockReason,
                IsDisposed = batch.IsDisposed,
                DisposalDate = batch.DisposalDate,
                DisposalMethod = batch.DisposalMethod,
                CreatedAt = batch.CreatedAt,
                UpdatedAt = batch.UpdatedAt,
                BranchId = batch.BranchId,
                BranchName = batch.Branch?.BranchName,
                ExpiryStatus = batch.ExpiryStatus,
                DaysUntilExpiry = batch.DaysUntilExpiry,
                AvailableStock = batch.CurrentStock
            }).ToList();
        }

        /// <summary>
        /// Generate FIFO batch allocation suggestions
        /// </summary>
        public async Task<List<BatchAllocationDto>> GenerateFifoSuggestionsAsync(int productId, int quantity)
        {
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be greater than 0");

            var batches = await _context.ProductBatches
                .Where(b => b.ProductId == productId && 
                           b.CurrentStock > 0 && 
                           !b.IsDisposed && 
                           !b.IsBlocked)
                .OrderBy(b => b.ExpiryDate ?? DateTime.MaxValue)
                .ThenBy(b => b.CreatedAt)
                .ToListAsync();

            var allocations = new List<BatchAllocationDto>();
            var remainingQuantity = quantity;

            foreach (var batch in batches)
            {
                if (remainingQuantity <= 0) break;

                var allocatedQuantity = Math.Min(remainingQuantity, batch.CurrentStock);
                var daysUntilExpiry = batch.DaysUntilExpiry ?? int.MaxValue;
                
                var allocation = new BatchAllocationDto
                {
                    BatchId = batch.Id,
                    BatchNumber = batch.BatchNumber,
                    Quantity = allocatedQuantity,
                    ExpiryDate = batch.ExpiryDate,
                    DaysUntilExpiry = batch.DaysUntilExpiry,
                    UrgencyClass = GetUrgencyClass(daysUntilExpiry),
                    UrgencyIcon = GetUrgencyIcon(daysUntilExpiry),
                    ExpiryText = GetExpiryText(batch.ExpiryDate, daysUntilExpiry)
                };

                allocations.Add(allocation);
                remainingQuantity -= allocatedQuantity;
            }

            if (remainingQuantity > 0)
            {
                _logger.LogWarning("Insufficient stock for product {ProductId}. Requested: {Quantity}, Available: {Available}", 
                    productId, quantity, quantity - remainingQuantity);
            }

            return allocations;
        }

        /// <summary>
        /// Create sale with complete batch allocation tracking
        /// </summary>
        public async Task<SaleWithBatchesResponseDto> CreateSaleWithBatchesAsync(CreateSaleWithBatchesRequest request, int cashierId, int branchId)
        {
            // Validate batch allocation first
            var validation = await ValidateBatchAllocationAsync(new ValidateBatchAllocationRequest { Items = request.Items });
            if (!validation.IsValid)
                throw new InvalidOperationException(validation.ErrorMessage ?? "Batch allocation validation failed");

            // Use execution strategy to handle retry logic properly
            var executionStrategy = _context.Database.CreateExecutionStrategy();
            return await executionStrategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                // Create the main sale record
                var sale = new Sale
                {
                    SaleNumber = await GenerateSaleNumberAsync(),
                    SaleDate = _timezoneService.Now,
                    Subtotal = request.Total, // Will be updated after calculating all items
                    Total = request.Total,
                    PaymentMethod = Enum.Parse<PaymentMethod>(request.PaymentMethod, true),
                    AmountPaid = request.ReceivedAmount,
                    ChangeAmount = request.Change,
                    MemberId = request.MemberId,
                    CashierId = cashierId,
                    Status = SaleStatus.Completed,
                    ReceiptPrinted = false,
                    CreatedAt = _timezoneService.Now
                };

                _context.Sales.Add(sale);
                await _context.SaveChangesAsync(); // Save to get sale ID

                var saleItemsWithBatches = new List<SaleItemWithBatchDto>();

                // Process each sale item with batch tracking
                foreach (var item in request.Items)
                {
                    var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId && p.IsActive);
                    if (product == null)
                        throw new ArgumentException($"Product {item.ProductId} not found");

                    // Create sale item
                    var saleItem = new SaleItem
                    {
                        SaleId = sale.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        Subtotal = item.Subtotal
                    };

                    _context.SaleItems.Add(saleItem);
                    await _context.SaveChangesAsync(); // Save to get sale item ID

                    var batchesUsed = new List<SaleItemBatchDto>();

                    // Process batch allocations for this item
                    foreach (var allocation in item.BatchAllocations)
                    {
                        var batch = await _context.ProductBatches.FirstOrDefaultAsync(b => b.Id == allocation.BatchId);
                        if (batch == null)
                            throw new ArgumentException($"Batch {allocation.BatchId} not found");

                        if (batch.CurrentStock < allocation.Quantity)
                            throw new InvalidOperationException($"Insufficient stock in batch {batch.BatchNumber}");

                        // Create sale item batch record
                        var saleItemBatch = new SaleItemBatch
                        {
                            SaleItemId = saleItem.Id,
                            BatchId = allocation.BatchId,
                            BatchNumber = batch.BatchNumber,
                            Quantity = allocation.Quantity,
                            CostPerUnit = batch.CostPerUnit,
                            TotalCost = allocation.Quantity * batch.CostPerUnit,
                            ExpiryDate = batch.ExpiryDate,
                            CreatedAt = _timezoneService.Now
                        };

                        _context.SaleItemBatches.Add(saleItemBatch);

                        // Update batch stock
                        batch.CurrentStock -= allocation.Quantity;
                        batch.UpdatedAt = _timezoneService.Now;

                        // Update product total stock
                        product.Stock -= allocation.Quantity;
                        product.UpdatedAt = _timezoneService.Now;

                        // Log inventory mutation
                        var mutation = new InventoryMutation
                        {
                            ProductId = item.ProductId,
                            Type = MutationType.Sale,
                            Quantity = -allocation.Quantity,
                            StockBefore = product.Stock + allocation.Quantity,
                            StockAfter = product.Stock,
                            Notes = $"Sale - Batch {batch.BatchNumber}",
                            ReferenceNumber = sale.SaleNumber,
                            UnitCost = batch.CostPerUnit,
                            TotalCost = allocation.Quantity * batch.CostPerUnit,
                            CreatedAt = _timezoneService.Now,
                            CreatedBy = cashierId.ToString()
                        };

                        _context.InventoryMutations.Add(mutation);

                        batchesUsed.Add(new SaleItemBatchDto
                        {
                            BatchId = batch.Id,
                            BatchNumber = batch.BatchNumber,
                            QuantityUsed = allocation.Quantity,
                            CostPerUnit = batch.CostPerUnit,
                            TotalCost = allocation.Quantity * batch.CostPerUnit,
                            ExpiryDate = batch.ExpiryDate,
                            ExpiryStatus = GetUrgencyClass(batch.DaysUntilExpiry ?? int.MaxValue)
                        });
                    }

                    saleItemsWithBatches.Add(new SaleItemWithBatchDto
                    {
                        SaleItemId = saleItem.Id,
                        ProductId = item.ProductId,
                        ProductName = product.Name,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        Subtotal = item.Subtotal,
                        BatchesUsed = batchesUsed
                    });
                }

                // Handle member points if applicable
                if (request.MemberId.HasValue)
                {
                    var member = await _context.Members.FirstOrDefaultAsync(m => m.Id == request.MemberId);
                    if (member != null)
                    {
                        var pointsEarned = CalculatePointsEarned(request.Total);
                        
                        var memberPoint = new MemberPoint
                        {
                            MemberId = request.MemberId.Value,
                            Points = pointsEarned,
                            Type = PointTransactionType.Purchase,
                            Description = $"Purchase - {sale.SaleNumber}",
                            ReferenceNumber = sale.SaleNumber,
                            CreatedAt = _timezoneService.Now
                        };

                        _context.MemberPoints.Add(memberPoint);
                        member.TotalPoints += pointsEarned;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Get cashier name
                var cashier = await _context.Users.FirstOrDefaultAsync(u => u.Id == cashierId);

                return new SaleWithBatchesResponseDto
                {
                    Id = sale.Id,
                    SaleNumber = sale.SaleNumber,
                    SaleDate = sale.SaleDate,
                    Total = sale.Total,
                    PaymentMethod = sale.PaymentMethod.ToString(),
                    ReceivedAmount = sale.AmountPaid,
                    Change = sale.ChangeAmount,
                    MemberId = sale.MemberId,
                    MemberName = sale.Member?.Name,
                    CashierName = cashier?.Username ?? "Unknown",
                    Items = saleItemsWithBatches,
                    BatchTrackingEnabled = true
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
            });
        }

        /// <summary>
        /// Validate batch allocation before sale processing
        /// </summary>
        public async Task<BatchAllocationValidationDto> ValidateBatchAllocationAsync(ValidateBatchAllocationRequest request)
        {
            var validation = new BatchAllocationValidationDto
            {
                IsValid = true,
                ValidationErrors = new List<string>(),
                Warnings = new List<string>()
            };

            foreach (var item in request.Items)
            {
                var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId && p.IsActive);
                if (product == null)
                {
                    validation.ValidationErrors.Add($"Product {item.ProductId} not found");
                    continue;
                }

                var totalAllocatedQuantity = item.BatchAllocations.Sum(ba => ba.Quantity);
                if (totalAllocatedQuantity != item.Quantity)
                {
                    validation.ValidationErrors.Add($"Product {product.Name}: Allocated quantity ({totalAllocatedQuantity}) does not match item quantity ({item.Quantity})");
                }

                foreach (var allocation in item.BatchAllocations)
                {
                    var batch = await _context.ProductBatches.FirstOrDefaultAsync(b => b.Id == allocation.BatchId);
                    if (batch == null)
                    {
                        validation.ValidationErrors.Add($"Batch {allocation.BatchId} not found");
                        continue;
                    }

                    if (batch.ProductId != item.ProductId)
                    {
                        validation.ValidationErrors.Add($"Batch {batch.BatchNumber} does not belong to product {product.Name}");
                    }

                    if (batch.IsDisposed)
                    {
                        validation.ValidationErrors.Add($"Batch {batch.BatchNumber} has been disposed");
                    }

                    if (batch.IsBlocked)
                    {
                        validation.ValidationErrors.Add($"Batch {batch.BatchNumber} is blocked: {batch.BlockReason}");
                    }

                    if (batch.CurrentStock < allocation.Quantity)
                    {
                        validation.ValidationErrors.Add($"Insufficient stock in batch {batch.BatchNumber}. Available: {batch.CurrentStock}, Requested: {allocation.Quantity}");
                    }

                    // Add warnings for expired or near-expiry batches
                    var daysUntilExpiry = batch.DaysUntilExpiry ?? int.MaxValue;
                    if (daysUntilExpiry <= 0)
                    {
                        validation.Warnings.Add($"Batch {batch.BatchNumber} has expired");
                    }
                    else if (daysUntilExpiry <= 3)
                    {
                        validation.Warnings.Add($"Batch {batch.BatchNumber} expires in {daysUntilExpiry} day(s)");
                    }
                }
            }

            if (validation.ValidationErrors.Any())
            {
                validation.IsValid = false;
                validation.ErrorMessage = string.Join("; ", validation.ValidationErrors);
            }

            return validation;
        }

        /// <summary>
        /// Get batch allocation summary for a completed sale
        /// </summary>
        public async Task<List<SaleItemWithBatchDto>> GetSaleBatchSummaryAsync(int saleId)
        {
            var sale = await _context.Sales.FirstOrDefaultAsync(s => s.Id == saleId);
            if (sale == null)
                throw new ArgumentException("Sale not found");

            var saleItems = await _context.SaleItems
                .Include(si => si.Product)
                .Where(si => si.SaleId == saleId)
                .ToListAsync();

            var result = new List<SaleItemWithBatchDto>();

            foreach (var saleItem in saleItems)
            {
                var batchRecords = await _context.SaleItemBatches
                    .Where(sib => sib.SaleItemId == saleItem.Id)
                    .ToListAsync();

                var batchesUsed = batchRecords.Select(br => new SaleItemBatchDto
                {
                    BatchId = br.BatchId,
                    BatchNumber = br.BatchNumber,
                    QuantityUsed = br.Quantity,
                    CostPerUnit = br.CostPerUnit,
                    TotalCost = br.TotalCost,
                    ExpiryDate = br.ExpiryDate,
                    ExpiryStatus = br.ExpiryDate.HasValue ? 
                        GetUrgencyClass(CalculateDaysUntilExpiry(br.ExpiryDate.Value)) : 
                        "good"
                }).ToList();

                result.Add(new SaleItemWithBatchDto
                {
                    SaleItemId = saleItem.Id,
                    ProductId = saleItem.ProductId,
                    ProductName = saleItem.Product?.Name ?? "Unknown Product",
                    Quantity = saleItem.Quantity,
                    UnitPrice = saleItem.UnitPrice,
                    Subtotal = saleItem.Subtotal,
                    BatchesUsed = batchesUsed
                });
            }

            return result;
        }

        // ==================== HELPER METHODS ==================== //

        private ExpiryUrgency GetExpiryUrgency(int daysUntilExpiry)
        {
            return daysUntilExpiry switch
            {
                <= 1 => ExpiryUrgency.Critical,
                <= 3 => ExpiryUrgency.High,
                <= 7 => ExpiryUrgency.Medium,
                _ => ExpiryUrgency.Low
            };
        }

        private string GetUrgencyClass(int daysUntilExpiry)
        {
            return daysUntilExpiry switch
            {
                <= 0 => "expired",
                <= 1 => "critical",
                <= 3 => "warning",
                _ => "good"
            };
        }

        private string GetUrgencyIcon(int daysUntilExpiry)
        {
            return daysUntilExpiry switch
            {
                <= 0 => "dangerous",
                <= 1 => "error",
                <= 3 => "warning",
                _ => "check_circle"
            };
        }

        private string GetExpiryText(DateTime? expiryDate, int? daysUntilExpiry)
        {
            if (!expiryDate.HasValue)
                return "No expiry date";

            if (!daysUntilExpiry.HasValue)
                return "No expiry data";

            return daysUntilExpiry.Value switch
            {
                < 0 => $"Expired {Math.Abs(daysUntilExpiry.Value)} day(s) ago",
                0 => "Expires today",
                1 => "Expires tomorrow",
                _ => $"Expires in {daysUntilExpiry.Value} day(s)"
            };
        }

        private int CalculateDaysUntilExpiry(DateTime expiryDate)
        {
            return (int)(expiryDate.Date - _timezoneService.Today).TotalDays;
        }

        // ==================== MEMBER CREDIT INTEGRATION METHODS ==================== //

        public async Task<CreditValidationResultDto> ValidateMemberCreditAsync(CreditValidationRequestDto request)
        {
            try
            {
                var member = await _context.Members.FindAsync(request.MemberId);
                if (member == null)
                {
                    return new CreditValidationResultDto
                    {
                        IsApproved = false,
                        DecisionReason = "Member not found",
                        Errors = new List<string> { "Member does not exist" }
                    };
                }

                var creditSummary = await _memberService.GetCreditSummaryAsync(request.MemberId);
                var availableCredit = await _memberService.CalculateAvailableCreditAsync(request.MemberId);
                var hasOverdue = await _memberService.HasOverduePaymentsAsync(request.MemberId);

                var result = new CreditValidationResultDto
                {
                    MemberName = member.Name,
                    MemberTier = member.Tier.ToString(),
                    AvailableCredit = availableCredit,
                    CreditScore = await _memberService.CalculateCreditScoreAsync(request.MemberId),
                    CreditUtilization = await _memberService.CalculateCreditUtilizationAsync(request.MemberId)
                };

                // Check eligibility
                if (!creditSummary.IsEligible)
                {
                    result.IsApproved = false;
                    result.DecisionReason = "Member not eligible for credit";
                    result.Errors.Add("Member is not eligible for credit transactions");
                    return result;
                }

                // Check overdue payments (allow manager override)
                if (hasOverdue)
                {
                    if (request.OverrideWarnings)
                    {
                        result.Warnings.Add("Overdue payments overridden by manager");
                    }
                    else
                    {
                        result.IsApproved = false;
                        result.DecisionReason = "Has overdue payments";
                        result.Errors.Add("Member has overdue payments");
                        return result;
                    }
                }

                // Check available credit
                if (request.RequestedAmount > availableCredit)
                {
                    result.IsApproved = false;
                    result.DecisionReason = "Insufficient available credit";
                    result.ApprovedAmount = availableCredit;
                    result.Errors.Add($"Requested amount ({request.RequestedAmount:C}) exceeds available credit ({availableCredit:C})");
                    return result;
                }

                // Calculate risk
                var (riskScore, riskLevel) = await CalculateTransactionRiskAsync(request.MemberId, request.RequestedAmount, request.Items);
                result.RiskLevel = riskLevel;

                // Check if requires manager approval
                var maxAllowed = await _memberService.CalculateMaxTransactionAmountAsync(request.MemberId);
                result.RequiresManagerApproval = request.RequestedAmount > maxAllowed || riskScore > 70;

                if (result.RequiresManagerApproval && !request.OverrideWarnings)
                {
                    result.IsApproved = false;
                    result.DecisionReason = "Requires manager approval";
                    result.Warnings.Add("Transaction requires manager approval");
                    result.MaxAllowedAmount = maxAllowed;
                    return result;
                }

                // Approve transaction
                result.IsApproved = true;
                result.ApprovedAmount = request.RequestedAmount;
                result.DecisionReason = "Transaction approved";
                result.MaxAllowedAmount = maxAllowed;

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating member credit: {MemberId}", request.MemberId);
                return new CreditValidationResultDto
                {
                    IsApproved = false,
                    DecisionReason = "System error during validation",
                    Errors = new List<string> { "Internal system error" }
                };
            }
        }

        public async Task<SaleDto> CreateSaleWithCreditAsync(CreateSaleWithCreditDto request)
        {
            // Validate credit BEFORE starting transaction
            var validation = await ValidateMemberCreditAsync(new CreditValidationRequestDto
            {
                MemberId = request.MemberId,
                RequestedAmount = request.CreditAmount,
                Items = request.Items,
                BranchId = request.BranchId,
                OverrideWarnings = request.IsManagerApproved
            });

            if (!validation.IsApproved)
            {
                throw new InvalidOperationException($"Credit validation failed: {validation.DecisionReason}");
            }

            // Use execution strategy to handle retry logic properly
            var executionStrategy = _context.Database.CreateExecutionStrategy();
            return await executionStrategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {

                // Create the sale
                var saleNumber = await GenerateSaleNumberAsync();
                var sale = new Sale
                {
                    SaleNumber = saleNumber,
                    SaleDate = _timezoneService.Now,
                    Subtotal = request.TotalAmount - (request.TaxAmount ?? 0) - (request.DiscountAmount ?? 0),
                    DiscountAmount = request.DiscountAmount ?? 0,
                    TaxAmount = request.TaxAmount ?? 0,
                    Total = request.TotalAmount,
                    AmountPaid = request.CashAmount,
                    ChangeAmount = 0, // No change for credit transactions
                    PaymentMethod = request.PaymentMethod,
                    PaymentReference = request.ValidationId,
                    MemberId = request.MemberId,
                    CustomerName = validation.MemberName,
                    CashierId = request.CashierId,
                    UserId = request.CashierId, // Set UserId to match CashierId for database compatibility
                    Status = SaleStatus.Completed,
                    Notes = request.Description,
                    
                    // Credit transaction fields
                    CreditAmount = request.CreditAmount,
                    IsCreditTransaction = true,
                    ReceiptPrinted = false,
                    CreatedAt = _timezoneService.Now,
                    UpdatedAt = _timezoneService.Now
                };

                _context.Sales.Add(sale);
                await _context.SaveChangesAsync();

                // Add sale items
                foreach (var item in request.Items)
                {
                    var saleItem = new SaleItem
                    {
                        SaleId = sale.Id,
                        ProductId = item.ProductId,
                        Quantity = (int)item.Quantity,
                        UnitPrice = item.UnitPrice,
                        Subtotal = item.Subtotal,
                        DiscountAmount = item.DiscountAmount,
                        CreatedAt = _timezoneService.Now,
                        // UpdatedAt property doesn't exist in SaleItem model
                    };

                    _context.SaleItems.Add(saleItem);
                }

                await _context.SaveChangesAsync();

                // Grant credit directly (avoid nested transaction)
                var member = await _context.Members.FindAsync(request.MemberId);
                if (member == null)
                {
                    throw new InvalidOperationException($"Member {request.MemberId} not found");
                }

                // Calculate due date - use custom date if provided, otherwise use member payment terms
                DateTime dueDate;
                if (request.UseCustomDueDate && request.CustomDueDate.HasValue)
                {
                    // Use custom due date provided by user
                    dueDate = request.CustomDueDate.Value;
                    
                    // Validation: Custom due date must be in the future and reasonable
                    if (dueDate <= DateTime.Now.Date)
                    {
                        throw new InvalidOperationException("Custom due date must be in the future");
                    }
                    
                    // Optional: Add maximum allowed payment terms (e.g., 180 days)
                    var maxDaysAllowed = 180; // 6 months max
                    if (dueDate > DateTime.Now.AddDays(maxDaysAllowed))
                    {
                        throw new InvalidOperationException($"Due date cannot exceed {maxDaysAllowed} days from today");
                    }
                }
                else
                {
                    // Use member's default payment terms (default: 30 days)
                    var paymentTermDays = member.PaymentTerms > 0 ? member.PaymentTerms : 30;
                    dueDate = DateTime.Now.AddDays(paymentTermDays);
                }

                // Create credit transaction record
                var creditTransaction = new MemberCreditTransaction
                {
                    MemberId = request.MemberId,
                    Amount = request.CreditAmount,
                    Type = CreditTransactionType.CreditSale,
                    Description = request.Description ?? $"POS Purchase - Sale #{sale.SaleNumber}",
                    DueDate = dueDate,
                    Status = CreditTransactionStatus.Pending,
                    TransactionDate = DateTime.Now,
                    BranchId = request.BranchId,
                    CreatedBy = request.CashierId,
                    CreatedAt = DateTime.Now,
                    ReferenceNumber = $"CR-{DateTime.Now:yyyyMMdd}-{sale.Id}"
                };

                _context.MemberCreditTransactions.Add(creditTransaction);

                // Update member debt and statistics
                member.CurrentDebt += request.CreditAmount;
                member.LifetimeDebt += request.CreditAmount;
                member.TotalSpent += request.TotalAmount;
                member.TotalTransactions += 1;
                member.LastTransactionDate = DateTime.Now;
                member.UpdatedAt = DateTime.Now;
                member.UpdatedBy = request.CashierId.ToString();

                // Ensure next payment due date is maintained for overdue checks and UI consistency
                if (!member.NextPaymentDueDate.HasValue || dueDate < member.NextPaymentDueDate.Value)
                {
                    member.NextPaymentDueDate = dueDate;
                }

                // Update sale with credit transaction reference
                await _context.SaveChangesAsync();
                sale.CreditTransactionId = creditTransaction.Id;

                // Reconcile ledger to ensure statuses and next due date are consistent
                await _memberService.ReconcileCreditLedgerAsync(request.MemberId);

                await transaction.CommitAsync();

                _logger.LogInformation("Credit sale created successfully. Sale: {SaleId}, Member: {MemberId}, Credit: {CreditAmount}", 
                    sale.Id, request.MemberId, request.CreditAmount);

                // Return sale DTO
                return await GetSaleByIdAsync(sale.Id) ?? throw new InvalidOperationException("Failed to retrieve created sale");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating credit sale for member {MemberId}", request.MemberId);
                throw;
            }
            });
        }

        public async Task<PaymentResultDto> ApplyCreditPaymentAsync(ApplyCreditPaymentDto request)
        {
            try
            {
                var sale = await _context.Sales.FindAsync(request.SaleId);
                if (sale == null)
                {
                    return new PaymentResultDto
                    {
                        IsSuccess = false,
                        Message = "Sale not found"
                    };
                }

                var member = await _context.Members.FindAsync(request.MemberId);
                if (member == null)
                {
                    return new PaymentResultDto
                    {
                        IsSuccess = false,
                        Message = "Member not found"
                    };
                }

                // Validate credit availability
                var availableCredit = await _memberService.CalculateAvailableCreditAsync(request.MemberId);
                if (request.CreditAmount > availableCredit)
                {
                    return new PaymentResultDto
                    {
                        IsSuccess = false,
                        Message = $"Insufficient credit. Available: {availableCredit:C}, Requested: {request.CreditAmount:C}"
                    };
                }

                // Apply credit payment
                var creditGranted = await _memberService.GrantCreditAsync(
                    request.MemberId,
                    request.CreditAmount,
                    request.Description ?? $"Credit payment for Sale #{sale.SaleNumber}",
                    request.SaleId);

                if (!creditGranted)
                {
                    return new PaymentResultDto
                    {
                        IsSuccess = false,
                        Message = "Failed to process credit payment"
                    };
                }

                // Update sale
                sale.CreditAmount = (sale.CreditAmount ?? 0) + request.CreditAmount;
                sale.IsCreditTransaction = true;
                sale.AmountPaid += request.CreditAmount;
                sale.PaymentMethod = sale.AmountPaid == sale.Total ? PaymentMethod.MemberCredit : PaymentMethod.Mixed;
                sale.UpdatedAt = _timezoneService.Now;

                await _context.SaveChangesAsync();

                var newCreditSummary = await _memberService.GetCreditSummaryAsync(request.MemberId);

                return new PaymentResultDto
                {
                    IsSuccess = true,
                    Message = "Credit payment applied successfully",
                    ProcessedAmount = request.CreditAmount,
                    RemainingBalance = sale.Total - sale.AmountPaid,
                    ProcessedAt = _timezoneService.Now,
                    TransactionReference = sale.SaleNumber,
                    NewAvailableCredit = await _memberService.CalculateAvailableCreditAsync(request.MemberId),
                    NewCurrentDebt = newCreditSummary.CurrentDebt,
                    NewCreditStatus = await _memberService.DetermineCreditStatusAsync(request.MemberId)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying credit payment for sale {SaleId}", request.SaleId);
                return new PaymentResultDto
                {
                    IsSuccess = false,
                    Message = "System error processing credit payment"
                };
            }
        }

        public async Task<POSMemberCreditDto?> GetMemberCreditForPOSAsync(string identifier)
        {
            try
            {
                return await _memberService.GetMemberCreditForPOSAsync(identifier);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting member credit for POS: {Identifier}", identifier);
                return null;
            }
        }

        public async Task<bool> ProcessCreditTransactionAsync(int saleId, int memberId, decimal creditAmount)
        {
            try
            {
                // This is handled in CreateSaleWithCreditAsync and ApplyCreditPaymentAsync
                // This method provides additional processing if needed
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing credit transaction: {SaleId}", saleId);
                return false;
            }
        }

        public async Task<SaleCreditInfoDto?> GetSaleCreditInfoAsync(int saleId)
        {
            try
            {
                var sale = await _context.Sales
                    .Include(s => s.Member)
                    .Where(s => s.Id == saleId && s.IsCreditTransaction)
                    .FirstOrDefaultAsync();

                if (sale == null) return null;

                var creditSummary = sale.MemberId.HasValue ? 
                    await _memberService.GetCreditSummaryAsync(sale.MemberId.Value) : null;

                return new SaleCreditInfoDto
                {
                    SaleId = sale.Id,
                    SaleNumber = sale.SaleNumber,
                    MemberId = sale.MemberId ?? 0,
                    MemberName = sale.Member?.Name ?? sale.CustomerName ?? "",
                    MemberNumber = sale.Member?.MemberNumber ?? "",
                    CreditAmount = sale.CreditAmount ?? 0,
                    TotalSaleAmount = sale.Total,
                    CashAmount = sale.AmountPaid - (sale.CreditAmount ?? 0),
                    TransactionDate = sale.SaleDate,
                    PaymentTerms = creditSummary?.PaymentTermDays ?? 30,
                    
                    // Updated member credit status after transaction
                    NewCurrentDebt = creditSummary?.CurrentDebt ?? 0,
                    NewAvailableCredit = sale.MemberId.HasValue ? 
                        await _memberService.CalculateAvailableCreditAsync(sale.MemberId.Value) : 0,
                    NewCreditStatus = sale.MemberId.HasValue ? 
                        await _memberService.DetermineCreditStatusAsync(sale.MemberId.Value) : "Unknown",
                    
                    // Display properties
                    CreditAmountDisplay = _memberService.FormatCreditAmount(sale.CreditAmount ?? 0),
                    NewCurrentDebtDisplay = _memberService.FormatCreditAmount(creditSummary?.CurrentDebt ?? 0),
                    NewAvailableCreditDisplay = sale.MemberId.HasValue ? 
                        _memberService.FormatCreditAmount(await _memberService.CalculateAvailableCreditAsync(sale.MemberId.Value)) : "N/A"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sale credit info: {SaleId}", saleId);
                return null;
            }
        }

        public async Task<MemberCreditEligibilityDto?> CheckMemberCreditEligibilityAsync(int memberId)
        {
            try
            {
                var member = await _context.Members.FindAsync(memberId);
                if (member == null) return null;

                var creditSummary = await _memberService.GetCreditSummaryAsync(memberId);
                var availableCredit = await _memberService.CalculateAvailableCreditAsync(memberId);
                var hasOverdue = await _memberService.HasOverduePaymentsAsync(memberId);
                var creditStatus = await _memberService.DetermineCreditStatusAsync(memberId);

                var restrictions = new List<string>();
                var warnings = new List<string>();

                if (!creditSummary.IsEligible)
                    restrictions.Add("Not eligible for credit");
                
                if (hasOverdue)
                {
                    restrictions.Add("Has overdue payments");
                    warnings.Add("Member has overdue payments");
                }

                if (creditSummary.CreditUtilization > 80)
                    warnings.Add("High credit utilization");

                return new MemberCreditEligibilityDto
                {
                    MemberId = memberId,
                    MemberName = member.Name,
                    MemberNumber = member.MemberNumber,
                    IsEligibleForCredit = creditSummary.IsEligible && !hasOverdue,
                    EligibilityReason = creditSummary.IsEligible ? 
                        (hasOverdue ? "Has overdue payments" : "Eligible") : "Not eligible",
                    CreditLimit = creditSummary.CreditLimit,
                    CurrentDebt = creditSummary.CurrentDebt,
                    AvailableCredit = availableCredit,
                    CreditStatus = creditStatus,
                    CreditScore = await _memberService.CalculateCreditScoreAsync(memberId),
                    MaxTransactionAmount = await _memberService.CalculateMaxTransactionAmountAsync(memberId),
                    RequiresManagerApproval = false, // Calculate based on business rules
                    CreditUtilization = await _memberService.CalculateCreditUtilizationAsync(memberId),
                    HasOverduePayments = hasOverdue,
                    Restrictions = restrictions,
                    Warnings = warnings,
                    
                    // Display properties
                    CreditLimitDisplay = _memberService.FormatCreditAmount(creditSummary.CreditLimit),
                    AvailableCreditDisplay = _memberService.FormatCreditAmount(availableCredit),
                    StatusColor = _memberService.GetCreditStatusColor(creditStatus)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking member credit eligibility: {MemberId}", memberId);
                return null;
            }
        }

        public async Task<bool> ValidateCreditAmountAsync(int memberId, decimal requestedAmount, List<SaleItemDto> items)
        {
            try
            {
                var availableCredit = await _memberService.CalculateAvailableCreditAsync(memberId);
                var maxTransactionAmount = await _memberService.CalculateMaxTransactionAmountAsync(memberId);
                
                return requestedAmount <= availableCredit && requestedAmount <= maxTransactionAmount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating credit amount: {MemberId}", memberId);
                return false;
            }
        }

        public async Task<(int riskScore, string riskLevel)> CalculateTransactionRiskAsync(int memberId, decimal amount, List<SaleItemDto> items)
        {
            try
            {
                var creditSummary = await _memberService.GetCreditSummaryAsync(memberId);
                var creditUtilization = await _memberService.CalculateCreditUtilizationAsync(memberId);
                
                int riskScore = 0;
                
                // Base risk on credit utilization
                riskScore += (int)(creditUtilization * 0.5m); // 0-50 points
                
                // Risk based on transaction amount vs credit limit
                var amountRatio = (amount / creditSummary.CreditLimit) * 100;
                riskScore += (int)(amountRatio * 0.3m); // 0-30 points
                
                // Risk based on payment history
                riskScore += creditSummary.TotalDelayedPayments * 2; // 2 points per delay
                
                // Determine risk level
                string riskLevel = riskScore switch
                {
                    < 30 => "Low",
                    < 60 => "Medium", 
                    < 80 => "High",
                    _ => "Critical"
                };
                
                return (Math.Min(100, riskScore), riskLevel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating transaction risk: {MemberId}", memberId);
                return (50, "Medium"); // Default to medium risk
            }
        }

        public async Task<int> GetMemberPaymentTermsAsync(int memberId)
        {
            try
            {
                var member = await _context.Members.FindAsync(memberId);
                if (member == null) return 30; // Default terms
                
                // Payment terms based on tier
                return member.Tier switch
                {
                    MembershipTier.Platinum => 45,
                    MembershipTier.Gold => 35,
                    MembershipTier.Silver => 30,
                    MembershipTier.Bronze => 15,
                    _ => 30
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting member payment terms: {MemberId}", memberId);
                return 30; // Default terms
            }
        }

        public async Task<bool> UpdateSaleWithCreditDetailsAsync(int saleId, int creditTransactionId, decimal creditAmount)
        {
            try
            {
                var sale = await _context.Sales.FindAsync(saleId);
                if (sale == null) return false;
                
                sale.CreditTransactionId = creditTransactionId;
                sale.CreditAmount = creditAmount;
                sale.IsCreditTransaction = true;
                sale.UpdatedAt = _timezoneService.Now;
                
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating sale with credit details: {SaleId}", saleId);
                return false;
            }
        }

    }
}
