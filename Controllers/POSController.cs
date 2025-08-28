// Controllers/POSController.cs - Enhanced with Member Credit Integration
using Berca_Backend.DTOs;
using Berca_Backend.Services;
using Berca_Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Berca_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class POSController : ControllerBase
    {
        private readonly IPOSService _posService;
        // TODO: Add IMemberCreditService when implementation is complete
        private readonly ILogger<POSController> _logger;

        public POSController(IPOSService posService, ILogger<POSController> logger)
        {
            _posService = posService;
            _logger = logger;
        }

        /// <summary>
        /// Create a new sale transaction
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
        /// Get receipt data for printing
        /// </summary>
        [HttpGet("sales/{id}/receipt")]
        [Authorize(Policy = "POS.Read")]
        public async Task<ActionResult<ApiResponse<ReceiptDataDto>>> GetReceiptData(int id)
        {
            try
            {
                var receiptData = await _posService.GetReceiptDataAsync(id);

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
}