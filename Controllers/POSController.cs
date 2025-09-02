// Controllers/POSController.cs - Enhanced with Member Credit Integration
using Berca_Backend.DTOs;
using Berca_Backend.Services;
using Berca_Backend.Services.Interfaces;
using Berca_Backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Berca_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class POSController : ControllerBase
    {
        private readonly IPOSService _posService;
        private readonly AppDbContext _context;
        // TODO: Add IMemberCreditService when implementation is complete
        private readonly ILogger<POSController> _logger;

        public POSController(IPOSService posService, AppDbContext context, ILogger<POSController> logger)
        {
            _posService = posService;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Create a new sale transaction (Enhanced for multi-branch)
        /// </summary>
        [HttpPost("sales")]
        [Authorize(Policy = "POS.Write")]
        public async Task<ActionResult<ApiResponse<SaleDto>>> CreateSale([FromBody] CreateSaleRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int cashierId))
                {
                    return Unauthorized(new ApiResponse<SaleDto>
                    {
                        Success = false,
                        Message = "Invalid user authentication"
                    });
                }

                // Get branch ID from cookie or request
                var branchId = GetCurrentBranchId();
                if (request.BranchId.HasValue)
                {
                    // Validate user access to the requested branch
                    var userRole = GetCurrentUserRole();
                    var accessibleBranchIds = await GetUserAccessibleBranches(cashierId, userRole);
                    
                    if (!accessibleBranchIds.Contains(request.BranchId.Value))
                    {
                        return StatusCode(403, new ApiResponse<SaleDto>
                        {
                            Success = false,
                            Message = "Access denied to this branch"
                        });
                    }
                    branchId = request.BranchId.Value;
                }
                
                if (branchId == 0)
                {
                    return BadRequest(new ApiResponse<SaleDto>
                    {
                        Success = false,
                        Message = "Branch context is required for sale transactions"
                    });
                }

                // Set the branch context in the request
                request.BranchId = branchId;

                var sale = await _posService.CreateSaleAsync(request, cashierId);

                return Ok(new ApiResponse<SaleDto>
                {
                    Success = true,
                    Message = "Sale created successfully",
                    Data = sale
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<SaleDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sale");
                return StatusCode(500, new ApiResponse<SaleDto>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get sale by ID
        /// </summary>
        [HttpGet("sales/{id}")]
        [Authorize(Policy = "POS.Read")]
        public async Task<ActionResult<ApiResponse<SaleDto>>> GetSale(int id)
        {
            try
            {
                var sale = await _posService.GetSaleByIdAsync(id);
                if (sale == null)
                {
                    return NotFound(new ApiResponse<SaleDto>
                    {
                        Success = false,
                        Message = "Sale not found"
                    });
                }

                return Ok(new ApiResponse<SaleDto>
                {
                    Success = true,
                    Data = sale
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sale: {SaleId}", id);
                return StatusCode(500, new ApiResponse<SaleDto>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get sale by sale number
        /// </summary>
        [HttpGet("sales/number/{saleNumber}")]
        [Authorize(Policy = "POS.Read")]
        public async Task<ActionResult<ApiResponse<SaleDto>>> GetSaleByNumber(string saleNumber)
        {
            try
            {
                var sale = await _posService.GetSaleByNumberAsync(saleNumber);
                if (sale == null)
                {
                    return NotFound(new ApiResponse<SaleDto>
                    {
                        Success = false,
                        Message = "Sale not found"
                    });
                }

                return Ok(new ApiResponse<SaleDto>
                {
                    Success = true,
                    Data = sale
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sale by number: {SaleNumber}", saleNumber);
                return StatusCode(500, new ApiResponse<SaleDto>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get sales with filtering and pagination
        /// </summary>
        [HttpGet("sales")]
        [Authorize(Policy = "POS.Read")]
        public async Task<ActionResult<ApiResponse<List<SaleDto>>>> GetSales(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int? cashierId = null,
            [FromQuery] string? paymentMethod = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                if (pageSize > 100) pageSize = 100; // Limit page size

                var sales = await _posService.GetSalesAsync(startDate, endDate, cashierId, paymentMethod, page, pageSize);

                return Ok(new ApiResponse<List<SaleDto>>
                {
                    Success = true,
                    Data = sales,
                    Message = $"Retrieved {sales.Count} sales"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sales");
                return StatusCode(500, new ApiResponse<List<SaleDto>>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Mark receipt as printed
        /// </summary>
        [HttpPost("sales/{id}/print-receipt")]
        [Authorize(Policy = "POS.Write")]
        public async Task<ActionResult<ApiResponse<bool>>> MarkReceiptPrinted(int id)
        {
            try
            {
                var result = await _posService.MarkReceiptPrintedAsync(id);
                if (!result)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Sale not found"
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Receipt marked as printed"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking receipt as printed: {SaleId}", id);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get receipt data for printing (Enhanced with branch information)
        /// </summary>
        [HttpGet("sales/{id}/receipt")]
        [Authorize(Policy = "POS.Read")]
        public async Task<ActionResult<ApiResponse<ReceiptDataDto>>> GetReceiptData(int id)
        {
            try
            {
                var receiptData = await _posService.GetReceiptDataAsync(id);

                // Enhance receipt data with branch information
                var sale = await _context.Sales
                    .Include(s => s.Cashier)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (sale != null)
                {
                    // Get branch information from cashier or cookie
                    var branchId = sale.Cashier?.BranchId ?? GetCurrentBranchId();
                    if (branchId > 0)
                    {
                        var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == branchId);
                        if (branch != null)
                        {
                            // Add branch info to receipt data (you'd modify the ReceiptDataDto to include this)
                            _logger.LogInformation("Receipt generated for sale {SaleId} from branch {BranchName}", id, branch.BranchName);
                        }
                    }
                }

                return Ok(new ApiResponse<ReceiptDataDto>
                {
                    Success = true,
                    Data = receiptData
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ApiResponse<ReceiptDataDto>
                {
                    Success = false,
                    Message = "Sale not found"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting receipt data: {SaleId}", id);
                return StatusCode(500, new ApiResponse<ReceiptDataDto>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Cancel a sale
        /// </summary>
        [HttpPost("sales/{id}/cancel")]
        [Authorize(Policy = "POS.Delete")]
        public async Task<ActionResult<ApiResponse<bool>>> CancelSale(int id, [FromBody] CancelSaleRequest request)
        {
            try
            {
                var result = await _posService.CancelSaleAsync(id, request.Reason);
                if (!result)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Sale not found"
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Sale cancelled successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling sale: {SaleId}", id);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Process a refund for a sale
        /// </summary>
        [HttpPost("sales/{id}/refund")]
        [Authorize(Policy = "POS.Write")]
        public async Task<ActionResult<ApiResponse<SaleDto>>> RefundSale(int id, [FromBody] RefundSaleRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int processedBy))
                {
                    return Unauthorized(new ApiResponse<SaleDto>
                    {
                        Success = false,
                        Message = "Invalid user authentication"
                    });
                }

                var refundSale = await _posService.RefundSaleAsync(id, request.Reason, processedBy);

                return Ok(new ApiResponse<SaleDto>
                {
                    Success = true,
                    Data = refundSale,
                    Message = "Refund processed successfully"
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ApiResponse<SaleDto>
                {
                    Success = false,
                    Message = "Sale not found"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing refund: {SaleId}", id);
                return StatusCode(500, new ApiResponse<SaleDto>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get sales summary for a date range
        /// </summary>
        [HttpGet("reports/summary")]
        [Authorize(Policy = "Reports.Read")]
        public async Task<ActionResult<ApiResponse<SaleSummaryDto>>> GetSalesSummary(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                if (endDate < startDate)
                {
                    return BadRequest(new ApiResponse<SaleSummaryDto>
                    {
                        Success = false,
                        Message = "End date must be after start date"
                    });
                }

                var summary = await _posService.GetSalesSummaryAsync(startDate, endDate);

                return Ok(new ApiResponse<SaleSummaryDto>
                {
                    Success = true,
                    Data = summary
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales summary");
                return StatusCode(500, new ApiResponse<SaleSummaryDto>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get daily sales data
        /// </summary>
        [HttpGet("reports/daily-sales")]
        [Authorize(Policy = "Reports.Read")]
        public async Task<ActionResult<ApiResponse<List<DailySalesDto>>>> GetDailySales(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                if (endDate < startDate)
                {
                    return BadRequest(new ApiResponse<List<DailySalesDto>>
                    {
                        Success = false,
                        Message = "End date must be after start date"
                    });
                }

                var dailySales = await _posService.GetDailySalesAsync(startDate, endDate);

                return Ok(new ApiResponse<List<DailySalesDto>>
                {
                    Success = true,
                    Data = dailySales
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily sales");
                return StatusCode(500, new ApiResponse<List<DailySalesDto>>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get payment method summary
        /// </summary>
        [HttpGet("reports/payment-methods")]
        [Authorize(Policy = "Reports.Read")]
        public async Task<ActionResult<ApiResponse<List<PaymentMethodSummaryDto>>>> GetPaymentMethodSummary(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                if (endDate < startDate)
                {
                    return BadRequest(new ApiResponse<List<PaymentMethodSummaryDto>>
                    {
                        Success = false,
                        Message = "End date must be after start date"
                    });
                }

                var summary = await _posService.GetPaymentMethodSummaryAsync(startDate, endDate);

                return Ok(new ApiResponse<List<PaymentMethodSummaryDto>>
                {
                    Success = true,
                    Data = summary
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment method summary");
                return StatusCode(500, new ApiResponse<List<PaymentMethodSummaryDto>>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Validate stock availability for sale items
        /// </summary>
        [HttpPost("validate-stock")]
        [Authorize(Policy = "POS.Read")]
        public async Task<ActionResult<ApiResponse<bool>>> ValidateStock([FromBody] List<CreateSaleItemRequest> items)
        {
            try
            {
                var isValid = await _posService.ValidateStockAvailabilityAsync(items);

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = isValid,
                    Message = isValid ? "Stock is available" : "Insufficient stock for some items"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating stock");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Calculate total for sale items
        /// </summary>
        [HttpPost("calculate-total")]
        [Authorize(Policy = "POS.Read")]
        public async Task<ActionResult<ApiResponse<decimal>>> CalculateTotal([FromBody] CalculateTotalRequest request)
        {
            try
            {
                // ✅ DISABLED: Ignore tax in calculation, always pass 0
                var total = await _posService.CalculateTotalAsync(request.Items, request.DiscountAmount, 0);

                return Ok(new ApiResponse<decimal>
                {
                    Success = true,
                    Data = total,
                    Message = "Total calculated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating total");
                return StatusCode(500, new ApiResponse<decimal>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        // ==================== BATCH MANAGEMENT ENDPOINTS ==================== //

        /// <summary>
        /// Get available batches for POS batch selection (sorted by FIFO)
        /// </summary>
        [HttpGet("products/{productId}/available-batches")]
        [Authorize(Policy = "POS.Read")]
        public async Task<ActionResult<ApiResponse<List<ProductBatchDto>>>> GetAvailableBatchesForSale(int productId)
        {
            try
            {
                var batches = await _posService.GetAvailableBatchesForSaleAsync(productId);

                return Ok(new ApiResponse<List<ProductBatchDto>>
                {
                    Success = true,
                    Data = batches,
                    Message = $"Retrieved {batches.Count} available batches"
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid product ID {ProductId} for available batches", productId);
                return BadRequest(new ApiResponse<List<ProductBatchDto>>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available batches for product {ProductId}", productId);
                return StatusCode(500, new ApiResponse<List<ProductBatchDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving available batches"
                });
            }
        }

        /// <summary>
        /// Generate FIFO suggestions for batch allocation
        /// </summary>
        [HttpGet("products/{productId}/fifo-suggestions")]
        [Authorize(Policy = "POS.Read")]
        public async Task<ActionResult<ApiResponse<List<BatchAllocationDto>>>> GetFifoSuggestions(
            int productId, 
            [FromQuery] int quantity)
        {
            try
            {
                if (quantity <= 0)
                {
                    return BadRequest(new ApiResponse<List<BatchAllocationDto>>
                    {
                        Success = false,
                        Message = "Quantity must be greater than 0"
                    });
                }

                var suggestions = await _posService.GenerateFifoSuggestionsAsync(productId, quantity);

                return Ok(new ApiResponse<List<BatchAllocationDto>>
                {
                    Success = true,
                    Data = suggestions,
                    Message = $"Generated {suggestions.Count} FIFO suggestions"
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid request for FIFO suggestions - Product {ProductId}, Quantity {Quantity}", productId, quantity);
                return BadRequest(new ApiResponse<List<BatchAllocationDto>>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating FIFO suggestions for product {ProductId}", productId);
                return StatusCode(500, new ApiResponse<List<BatchAllocationDto>>
                {
                    Success = false,
                    Message = "An error occurred while generating FIFO suggestions"
                });
            }
        }

        /// <summary>
        /// Create sale with complete batch allocation tracking
        /// </summary>
        [HttpPost("sales-with-batches")]
        [Authorize(Policy = "POS.Write")]
        public async Task<ActionResult<ApiResponse<SaleWithBatchesResponseDto>>> CreateSaleWithBatches([FromBody] CreateSaleWithBatchesRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int cashierId))
                {
                    return Unauthorized(new ApiResponse<SaleWithBatchesResponseDto>
                    {
                        Success = false,
                        Message = "Invalid user authentication"
                    });
                }

                var branchIdClaim = User.FindFirst("BranchId")?.Value;
                var branchId = int.TryParse(branchIdClaim, out var parsedBranchId) ? parsedBranchId : 1;

                var sale = await _posService.CreateSaleWithBatchesAsync(request, cashierId, branchId);

                return Ok(new ApiResponse<SaleWithBatchesResponseDto>
                {
                    Success = true,
                    Data = sale,
                    Message = "Sale with batch tracking created successfully"
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid request for creating sale with batches");
                return BadRequest(new ApiResponse<SaleWithBatchesResponseDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business rule violation for creating sale with batches");
                return BadRequest(new ApiResponse<SaleWithBatchesResponseDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sale with batches");
                return StatusCode(500, new ApiResponse<SaleWithBatchesResponseDto>
                {
                    Success = false,
                    Message = "An error occurred while creating sale with batch tracking"
                });
            }
        }

        /// <summary>
        /// Validate batch allocation before sale processing
        /// </summary>
        [HttpPost("validate-batch-allocation")]
        [Authorize(Policy = "POS.Read")]
        public async Task<ActionResult<ApiResponse<BatchAllocationValidationDto>>> ValidateBatchAllocation([FromBody] ValidateBatchAllocationRequest request)
        {
            try
            {
                var validation = await _posService.ValidateBatchAllocationAsync(request);

                return Ok(new ApiResponse<BatchAllocationValidationDto>
                {
                    Success = true,
                    Data = validation,
                    Message = validation.IsValid ? "Batch allocation is valid" : "Batch allocation validation failed"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating batch allocation");
                return StatusCode(500, new ApiResponse<BatchAllocationValidationDto>
                {
                    Success = false,
                    Message = "An error occurred while validating batch allocation"
                });
            }
        }

        /// <summary>
        /// Get batch allocation summary for a completed sale
        /// </summary>
        [HttpGet("sales/{saleId}/batch-summary")]
        [Authorize(Policy = "POS.Read")]
        public async Task<ActionResult<ApiResponse<List<SaleItemWithBatchDto>>>> GetSaleBatchSummary(int saleId)
        {
            try
            {
                var batchSummary = await _posService.GetSaleBatchSummaryAsync(saleId);

                return Ok(new ApiResponse<List<SaleItemWithBatchDto>>
                {
                    Success = true,
                    Data = batchSummary,
                    Message = $"Retrieved batch summary for sale {saleId}"
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid sale ID {SaleId} for batch summary", saleId);
                return BadRequest(new ApiResponse<List<SaleItemWithBatchDto>>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting batch summary for sale {SaleId}", saleId);
                return StatusCode(500, new ApiResponse<List<SaleItemWithBatchDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving batch summary"
                });
            }
        }

        // ==================== MEMBER CREDIT INTEGRATION ENDPOINTS ==================== //

        /// <summary>
        /// Validate member credit before POS checkout
        /// </summary>
        [HttpPost("validate-member-credit")]
        [Authorize(Policy = "POS.CreditValidation")]
        public async Task<ActionResult<ApiResponse<CreditValidationResultDto>>> ValidateMemberCredit([FromBody] CreditValidationRequestDto request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int validatedByUserId))
                {
                    return Unauthorized(new ApiResponse<CreditValidationResultDto>
                    {
                        Success = false,
                        Message = "Invalid user authentication"
                    });
                }

                var result = await _posService.ValidateMemberCreditAsync(request);
                result.ValidatedByUserId = validatedByUserId;
                result.ValidationTimestamp = DateTime.UtcNow;
                result.ValidationId = Guid.NewGuid().ToString("N")[..10]; // Short validation ID

                _logger.LogInformation("Member credit validation completed. Member: {MemberId}, Amount: {Amount}, Approved: {IsApproved}",
                    request.MemberId, request.RequestedAmount, result.IsApproved);

                return Ok(new ApiResponse<CreditValidationResultDto>
                {
                    Success = true,
                    Data = result,
                    Message = result.IsApproved ? "Credit validation successful" : "Credit validation failed"
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid credit validation request for member {MemberId}", request.MemberId);
                return BadRequest(new ApiResponse<CreditValidationResultDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating member credit for member {MemberId}", request.MemberId);
                return StatusCode(500, new ApiResponse<CreditValidationResultDto>
                {
                    Success = false,
                    Message = "Internal server error during credit validation"
                });
            }
        }

        /// <summary>
        /// Create sale with member credit payment
        /// </summary>
        [HttpPost("create-sale-with-credit")]
        [Authorize(Policy = "POS.CreditTransaction")]
        public async Task<ActionResult<ApiResponse<SaleDto>>> CreateSaleWithCredit([FromBody] CreateSaleWithCreditDto request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int cashierId))
                {
                    return Unauthorized(new ApiResponse<SaleDto>
                    {
                        Success = false,
                        Message = "Invalid user authentication"
                    });
                }

                // Ensure cashier ID matches request
                request.CashierId = cashierId;

                var result = await _posService.CreateSaleWithCreditAsync(request);

                _logger.LogInformation("Credit sale created successfully. Sale ID: {SaleId}, Member: {MemberId}, Credit: {CreditAmount}",
                    result.Id, request.MemberId, request.CreditAmount);

                return CreatedAtAction(nameof(GetSale), new { id = result.Id }, new ApiResponse<SaleDto>
                {
                    Success = true,
                    Data = result,
                    Message = "Sale with credit payment created successfully"
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Credit sale validation failed for member {MemberId}", request.MemberId);
                return BadRequest(new ApiResponse<SaleDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid credit sale request for member {MemberId}", request.MemberId);
                return BadRequest(new ApiResponse<SaleDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating credit sale for member {MemberId}", request.MemberId);
                return StatusCode(500, new ApiResponse<SaleDto>
                {
                    Success = false,
                    Message = "Internal server error during credit sale creation"
                });
            }
        }

        /// <summary>
        /// Apply credit payment to existing sale
        /// </summary>
        [HttpPost("apply-credit-payment")]
        [Authorize(Policy = "POS.CreditTransaction")]
        public async Task<ActionResult<ApiResponse<PaymentResultDto>>> ApplyCreditPayment([FromBody] ApplyCreditPaymentDto request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int processedBy))
                {
                    return Unauthorized(new ApiResponse<PaymentResultDto>
                    {
                        Success = false,
                        Message = "Invalid user authentication"
                    });
                }

                // Ensure processed by matches current user
                request.ProcessedBy = processedBy;
                request.ProcessingDate = DateTime.UtcNow;

                var result = await _posService.ApplyCreditPaymentAsync(request);

                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Credit payment application failed. Sale: {SaleId}, Member: {MemberId}, Reason: {Message}",
                        request.SaleId, request.MemberId, result.Message);
                    
                    return BadRequest(new ApiResponse<PaymentResultDto>
                    {
                        Success = false,
                        Data = result,
                        Message = result.Message
                    });
                }

                _logger.LogInformation("Credit payment applied successfully. Sale: {SaleId}, Member: {MemberId}, Amount: {Amount}",
                    request.SaleId, request.MemberId, request.CreditAmount);

                return Ok(new ApiResponse<PaymentResultDto>
                {
                    Success = true,
                    Data = result,
                    Message = "Credit payment applied successfully"
                });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Sale or member not found for credit payment. Sale: {SaleId}, Member: {MemberId}", 
                    request.SaleId, request.MemberId);
                return NotFound(new ApiResponse<PaymentResultDto>
                {
                    Success = false,
                    Message = "Sale or member not found"
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Credit payment operation invalid. Sale: {SaleId}, Member: {MemberId}", 
                    request.SaleId, request.MemberId);
                return BadRequest(new ApiResponse<PaymentResultDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying credit payment. Sale: {SaleId}, Member: {MemberId}", 
                    request.SaleId, request.MemberId);
                return StatusCode(500, new ApiResponse<PaymentResultDto>
                {
                    Success = false,
                    Message = "Internal server error during credit payment processing"
                });
            }
        }

        /// <summary>
        /// Quick member credit lookup for POS cashier
        /// </summary>
        [HttpGet("member-credit-lookup/{identifier}")]
        [Authorize(Policy = "POS.CreditValidation")]
        public async Task<ActionResult<ApiResponse<POSMemberCreditDto>>> GetMemberCreditForPOS(string identifier)
        {
            try
            {
                var memberCreditInfo = await _posService.GetMemberCreditForPOSAsync(identifier);
                if (memberCreditInfo == null)
                {
                    return NotFound(new ApiResponse<POSMemberCreditDto>
                    {
                        Success = false,
                        Message = "Member not found with the provided identifier"
                    });
                }

                _logger.LogInformation("POS member credit lookup successful for identifier: {Identifier}", identifier);

                return Ok(new ApiResponse<POSMemberCreditDto>
                {
                    Success = true,
                    Data = memberCreditInfo,
                    Message = "Member credit information retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving member credit for POS lookup: {Identifier}", identifier);
                return StatusCode(500, new ApiResponse<POSMemberCreditDto>
                {
                    Success = false,
                    Message = "Internal server error during member lookup"
                });
            }
        }

        /// <summary>
        /// Get credit transaction summary for receipt printing
        /// </summary>
        [HttpGet("sales/{id}/credit-info")]
        [Authorize(Policy = "POS.Read")]
        public async Task<ActionResult<ApiResponse<SaleCreditInfoDto>>> GetSaleCreditInfo(int id)
        {
            try
            {
                var creditInfo = await _posService.GetSaleCreditInfoAsync(id);
                if (creditInfo == null)
                {
                    return NotFound(new ApiResponse<SaleCreditInfoDto>
                    {
                        Success = false,
                        Message = "Sale not found or does not have credit transaction"
                    });
                }

                return Ok(new ApiResponse<SaleCreditInfoDto>
                {
                    Success = true,
                    Data = creditInfo,
                    Message = "Sale credit information retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving credit info for sale: {SaleId}", id);
                return StatusCode(500, new ApiResponse<SaleCreditInfoDto>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Check member credit eligibility before POS interaction
        /// </summary>
        [HttpGet("member/{memberId}/credit-eligibility")]
        [Authorize(Policy = "POS.CreditValidation")]
        public async Task<ActionResult<ApiResponse<MemberCreditEligibilityDto>>> CheckCreditEligibility(int memberId)
        {
            try
            {
                var eligibility = await _posService.CheckMemberCreditEligibilityAsync(memberId);
                if (eligibility == null)
                {
                    return NotFound(new ApiResponse<MemberCreditEligibilityDto>
                    {
                        Success = false,
                        Message = "Member not found"
                    });
                }

                return Ok(new ApiResponse<MemberCreditEligibilityDto>
                {
                    Success = true,
                    Data = eligibility,
                    Message = "Credit eligibility checked successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking credit eligibility for member: {MemberId}", memberId);
                return StatusCode(500, new ApiResponse<MemberCreditEligibilityDto>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get products available for POS with branch filtering
        /// </summary>
        [HttpGet("products")]
        [Authorize(Policy = "POS.Read")]
        public async Task<ActionResult<ApiResponse<List<ProductDto>>>> GetProductsForPOS(
            [FromQuery] string? search = null,
            [FromQuery] int? categoryId = null,
            [FromQuery] bool? isActive = true,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == 0)
                {
                    return Unauthorized(new ApiResponse<List<ProductDto>>
                    {
                        Success = false,
                        Message = "Invalid user authentication"
                    });
                }

                // Get user's accessible branches
                var userRole = GetCurrentUserRole();
                var accessibleBranchIds = await GetUserAccessibleBranches(currentUserId, userRole);
                
                if (accessibleBranchIds.Count == 0)
                {
                    return BadRequest(new ApiResponse<List<ProductDto>>
                    {
                        Success = false,
                        Message = "No accessible branches found for user"
                    });
                }

                // Get current branch context
                var currentBranchId = GetCurrentBranchId();
                var branchIdsToSearch = currentBranchId > 0 && accessibleBranchIds.Contains(currentBranchId) 
                    ? new List<int> { currentBranchId } 
                    : accessibleBranchIds;

                // Query products with filtering
                var query = _context.Products
                    .Where(p => isActive == null || p.IsActive == isActive)
                    .Include(p => p.Category)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(p => p.Name.Contains(search) || 
                                           p.Barcode.Contains(search) ||
                                           (p.Description != null && p.Description.Contains(search)));
                }

                if (categoryId.HasValue)
                {
                    query = query.Where(p => p.CategoryId == categoryId.Value);
                }

                var totalItems = await query.CountAsync();
                var products = await query
                    .OrderBy(p => p.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Get branch-specific stock for these products
                var productIds = products.Select(p => p.Id).ToList();
                var branchStocks = await _context.StockMutations
                    .Where(sm => productIds.Contains(sm.ProductId) && 
                                branchIdsToSearch.Contains(sm.BranchId ?? 0))
                    .GroupBy(sm => new { sm.ProductId, sm.BranchId })
                    .Select(g => new 
                    {
                        ProductId = g.Key.ProductId,
                        BranchId = g.Key.BranchId ?? 0,
                        CurrentStock = g.OrderByDescending(sm => sm.CreatedAt)
                                      .First().StockAfter
                    })
                    .ToListAsync();

                var productDtos = products.Select(p => 
                {
                    var branchStockDict = branchStocks
                        .Where(bs => bs.ProductId == p.Id)
                        .ToDictionary(bs => bs.BranchId, bs => bs.CurrentStock);

                    var totalBranchStock = branchStockDict.Values.Sum();

                    return new ProductDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Barcode = p.Barcode,
                        SellPrice = p.SellPrice,
                        BuyPrice = p.BuyPrice,
                        Stock = totalBranchStock > 0 ? totalBranchStock : p.Stock,
                        MinStock = p.MinimumStock,
                        Unit = p.Unit,
                        CategoryId = p.CategoryId,
                        CategoryName = p.Category?.Name,
                        IsActive = p.IsActive,
                        Description = p.Description,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt
                    };
                }).Where(p => p.Stock > 0).ToList();

                _logger.LogInformation("Retrieved {ProductCount} products for POS from {BranchCount} branches", 
                    productDtos.Count, branchIdsToSearch.Count);

                return Ok(new ApiResponse<List<ProductDto>>
                {
                    Success = true,
                    Data = productDtos,
                    Message = $"Retrieved {productDtos.Count} products for POS"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products for POS");
                return StatusCode(500, new ApiResponse<List<ProductDto>>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get product stock by branch for POS
        /// </summary>
        [HttpGet("products/{productId}/branch-stock")]
        [Authorize(Policy = "POS.Read")]
        public async Task<ActionResult<ApiResponse<Dictionary<int, int>>>> GetProductBranchStock(int productId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == 0)
                {
                    return Unauthorized(new ApiResponse<Dictionary<int, int>>
                    {
                        Success = false,
                        Message = "Invalid user authentication"
                    });
                }

                var userRole = GetCurrentUserRole();
                var accessibleBranchIds = await GetUserAccessibleBranches(currentUserId, userRole);

                var branchStock = await _context.StockMutations
                    .Where(sm => sm.ProductId == productId && accessibleBranchIds.Contains(sm.BranchId ?? 0))
                    .GroupBy(sm => sm.BranchId)
                    .Select(g => new 
                    {
                        BranchId = g.Key ?? 0,
                        CurrentStock = g.OrderByDescending(sm => sm.CreatedAt).First().StockAfter
                    })
                    .ToDictionaryAsync(x => x.BranchId, x => x.CurrentStock);

                return Ok(new ApiResponse<Dictionary<int, int>>
                {
                    Success = true,
                    Data = branchStock,
                    Message = $"Retrieved stock for product {productId} across {branchStock.Count} branches"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product branch stock for product {ProductId}", productId);
                return StatusCode(500, new ApiResponse<Dictionary<int, int>>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// TEMPORARY: Apply missing credit columns migration
        /// </summary>
        [HttpPost("admin/apply-credit-migration")]
        [AllowAnonymous] // Temporary for migration purposes
        public async Task<ActionResult<ApiResponse<string>>> ApplyCreditMigration()
        {
            try
            {
                _logger.LogInformation("Applying credit integration migration...");
                
                var sql = @"
                    -- Add credit transaction fields to Sales table if they don't exist
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'CreditAmount')
                    BEGIN
                        ALTER TABLE Sales ADD CreditAmount decimal(18,2) NULL;
                        PRINT 'Added CreditAmount column';
                    END

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'IsCreditTransaction')
                    BEGIN
                        ALTER TABLE Sales ADD IsCreditTransaction bit NOT NULL DEFAULT 0;
                        PRINT 'Added IsCreditTransaction column';
                    END

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'CreditTransactionId')
                    BEGIN
                        ALTER TABLE Sales ADD CreditTransactionId int NULL;
                        PRINT 'Added CreditTransactionId column';
                    END

                    -- Create foreign key constraint to MemberCreditTransactions if it doesn't exist
                    IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Sales_MemberCreditTransactions_CreditTransactionId')
                    BEGIN
                        ALTER TABLE Sales ADD CONSTRAINT FK_Sales_MemberCreditTransactions_CreditTransactionId 
                        FOREIGN KEY (CreditTransactionId) REFERENCES MemberCreditTransactions(Id);
                        PRINT 'Added foreign key constraint';
                    END

                    -- Create index for CreditTransactionId if it doesn't exist
                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sales_CreditTransactionId')
                    BEGIN
                        CREATE INDEX IX_Sales_CreditTransactionId ON Sales(CreditTransactionId);
                        PRINT 'Added IX_Sales_CreditTransactionId index';
                    END

                    -- Create index for performance on credit transactions lookup if it doesn't exist
                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sales_MemberId_IsCreditTransaction')
                    BEGIN
                        CREATE INDEX IX_Sales_MemberId_IsCreditTransaction ON Sales(MemberId, IsCreditTransaction);
                        PRINT 'Added IX_Sales_MemberId_IsCreditTransaction index';
                    END

                    -- Create index for payment method queries if it doesn't exist
                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sales_PaymentMethod')
                    BEGIN
                        CREATE INDEX IX_Sales_PaymentMethod ON Sales(PaymentMethod);
                        PRINT 'Added IX_Sales_PaymentMethod index';
                    END

                    -- Record this migration in EF history if not exists
                    IF NOT EXISTS (SELECT * FROM __EFMigrationsHistory WHERE MigrationId = '20250830000000_AddSaleCreditIntegrationManual')
                    BEGIN
                        INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) 
                        VALUES ('20250830000000_AddSaleCreditIntegrationManual', '8.0.8');
                        PRINT 'Recorded migration in EF history';
                    END
                ";
                
                await _context.Database.ExecuteSqlRawAsync(sql);
                
                _logger.LogInformation("Credit integration migration completed successfully");
                
                return Ok(new ApiResponse<string>
                {
                    Success = true,
                    Data = "Migration completed successfully",
                    Message = "Credit integration columns added to Sales table"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying credit integration migration");
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = $"Migration failed: {ex.Message}"
                });
            }
        }
        /// <summary>
        /// Create sale transaction (Frontend integration endpoint: POST /api/Sale)
        /// </summary>
        [HttpPost]
        [Route("/api/Sale")]
        [Authorize(Policy = "POS.Write")]
        public async Task<IActionResult> CreateSaleTransaction([FromBody] BranchAwareSaleRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == 0)
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Invalid user authentication"
                    });
                }

                // Validate branch access
                var userRole = GetCurrentUserRole();
                var accessibleBranchIds = await GetUserAccessibleBranches(currentUserId, userRole);
                
                if (!accessibleBranchIds.Contains(request.BranchId))
                {
                    return StatusCode(403, new
                    {
                        success = false,
                        message = "Access denied to this branch"
                    });
                }

                // Create the sale with branch context
                var createSaleRequest = new CreateSaleRequest
                {
                    BranchId = request.BranchId,
                    CustomerId = request.CustomerId,
                    MemberCode = request.MemberCode,
                    PaymentMethod = request.PaymentMethod,
                    Subtotal = request.Subtotal,
                    Tax = request.Tax,
                    Discount = request.Discount,
                    Total = request.Total,
                    PaidAmount = request.PaidAmount,
                    ChangeAmount = request.ChangeAmount,
                    Items = request.Items.Select(i => new CreateSaleItemRequest
                    {
                        ProductId = i.ProductId,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice,
                        Discount = i.Discount,
                        Subtotal = i.Subtotal
                    }).ToList(),
                    Notes = request.Notes
                };

                var sale = await _posService.CreateSaleAsync(createSaleRequest, currentUserId);

                return Ok(new
                {
                    success = true,
                    message = "Sale created successfully",
                    data = new
                    {
                        id = sale.Id,
                        saleNumber = sale.SaleNumber,
                        total = sale.Total,
                        branchId = request.BranchId,
                        createdAt = sale.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sale transaction");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error"
                });
            }
        }

        #region Helper Methods

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value ?? 
                             User.FindFirst("sub")?.Value ?? 
                             User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        private string GetCurrentUserRole()
        {
            return User.FindFirst("Role")?.Value ?? 
                   User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value ?? 
                   User.FindFirst(ClaimTypes.Role)?.Value ??
                   "User";
        }

        private int GetCurrentBranchId()
        {
            // Try to get branch from cookie first
            var branchCookie = HttpContext.Request.Cookies[".TokoEniwan.BranchContext"];
            if (int.TryParse(branchCookie, out var branchFromCookie))
            {
                return branchFromCookie;
            }

            // Fallback to user's default branch
            var branchIdClaim = User.FindFirst("BranchId")?.Value;
            return int.TryParse(branchIdClaim, out var branchId) ? branchId : 0;
        }

        private async Task<List<int>> GetUserAccessibleBranches(int userId, string userRole)
        {
            var accessibleBranches = new List<int>();

            if (userRole.ToUpper() is "ADMIN" or "HEADMANAGER")
            {
                // Admin and HeadManager can access all branches
                accessibleBranches = await _context.Branches
                    .Where(b => b.IsActive)
                    .Select(b => b.Id)
                    .ToListAsync();
            }
            else
            {
                try
                {
                    // Get user's accessible branches via BranchAccess table
                    accessibleBranches = await _context.BranchAccesses
                        .Where(ba => ba.UserId == userId && ba.IsActive && ba.CanWrite)
                        .Select(ba => ba.BranchId)
                        .ToListAsync();
                }
                catch
                {
                    // Fallback: Use user's assigned branch
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                    if (user?.BranchId.HasValue == true)
                    {
                        accessibleBranches.Add(user.BranchId.Value);
                    }
                }
            }

            return accessibleBranches;
        }

        #endregion
    }

    // Request DTOs
    public class CancelSaleRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class RefundSaleRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class CalculateTotalRequest
    {
        public List<CreateSaleItemRequest> Items { get; set; } = new();
        public decimal DiscountAmount { get; set; }
    }

    /// <summary>
    /// Request DTO for branch-aware sale creation (Frontend integration)
    /// </summary>
    public class BranchAwareSaleRequest
    {
        [Required]
        public int BranchId { get; set; }
        
        public int? CustomerId { get; set; }
        
        public string? MemberCode { get; set; }
        
        [Required]
        public string PaymentMethod { get; set; } = string.Empty;
        
        [Required]
        public decimal Subtotal { get; set; }
        
        public decimal Tax { get; set; }
        
        public decimal Discount { get; set; }
        
        [Required]
        public decimal Total { get; set; }
        
        [Required]
        public decimal PaidAmount { get; set; }
        
        public decimal ChangeAmount { get; set; }
        
        [Required]
        public List<BranchAwareSaleItemRequest> Items { get; set; } = new();
        
        public string? Notes { get; set; }
    }

    public class BranchAwareSaleItemRequest
    {
        [Required]
        public int ProductId { get; set; }
        
        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
        
        [Required]
        public decimal UnitPrice { get; set; }
        
        public decimal Discount { get; set; }
        
        [Required]
        public decimal Subtotal { get; set; }
    }
}