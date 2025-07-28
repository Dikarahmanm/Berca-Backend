// Controllers/POSController.cs - Sprint 2 POS Controller Implementation
using Berca_Backend.DTOs;
using Berca_Backend.Services;
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
                var total = await _posService.CalculateTotalAsync(request.Items, request.DiscountAmount, request.TaxAmount);

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
        public decimal TaxAmount { get; set; }
    }
}