using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Berca_Backend.DTOs;
using Berca_Backend.Services.Interfaces;
using Berca_Backend.Models;

namespace Berca_Backend.Controllers
{
    /// <summary>
    /// Inventory Transfer Controller for Toko Eniwan multi-branch inventory management
    /// Handles inventory transfers between branches with comprehensive workflow and analytics
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class InventoryTransferController : ControllerBase
    {
        private readonly IInventoryTransferService _transferService;
        private readonly ITimezoneService _timezoneService;
        private readonly ILogger<InventoryTransferController> _logger;

        public InventoryTransferController(
            IInventoryTransferService transferService,
            ITimezoneService timezoneService,
            ILogger<InventoryTransferController> logger)
        {
            _transferService = transferService;
            _timezoneService = timezoneService;
            _logger = logger;
        }

        // ==================== CORE TRANSFER OPERATIONS ==================== //

        /// <summary>
        /// Create a new inventory transfer request
        /// </summary>
        [HttpPost("request")]
        [Authorize(Policy = "Branch.Manage")]
        public async Task<ActionResult<ApiResponse<InventoryTransferDto>>> CreateTransferRequest([FromBody] CreateInventoryTransferRequestDto request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                    return Unauthorized(ApiResponse<InventoryTransferDto>.ErrorResponse("User not authenticated"));

                var transfer = await _transferService.CreateTransferRequestAsync(request, currentUserId.Value);

                _logger.LogInformation("Transfer request created: {TransferNumber} by user {UserId}", 
                    transfer.TransferNumber, currentUserId.Value);

                return Ok(ApiResponse<InventoryTransferDto>.SuccessResponse(transfer, 
                    $"Transfer request {transfer.TransferNumber} created successfully"));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Transfer request validation failed: {Error}", ex.Message);
                return BadRequest(ApiResponse<InventoryTransferDto>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating transfer request");
                return StatusCode(500, ApiResponse<InventoryTransferDto>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Create bulk transfer request for multiple products
        /// </summary>
        [HttpPost("bulk-request")]
        [Authorize(Policy = "Branch.Manage")]
        public async Task<ActionResult<ApiResponse<InventoryTransferDto>>> CreateBulkTransferRequest([FromBody] BulkTransferRequestDto request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                    return Unauthorized(ApiResponse<InventoryTransferDto>.ErrorResponse("User not authenticated"));

                var transfer = await _transferService.CreateBulkTransferRequestAsync(request, currentUserId.Value);

                _logger.LogInformation("Bulk transfer request created: {TransferNumber} by user {UserId}", 
                    transfer.TransferNumber, currentUserId.Value);

                return Ok(ApiResponse<InventoryTransferDto>.SuccessResponse(transfer, 
                    $"Bulk transfer request {transfer.TransferNumber} created successfully with {transfer.TotalItems} items"));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Bulk transfer request validation failed: {Error}", ex.Message);
                return BadRequest(ApiResponse<InventoryTransferDto>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating bulk transfer request");
                return StatusCode(500, ApiResponse<InventoryTransferDto>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get transfer by ID with full details
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Policy = "Branch.Read")]
        public async Task<ActionResult<ApiResponse<InventoryTransferDto>>> GetTransferById(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                    return Unauthorized(ApiResponse<InventoryTransferDto>.ErrorResponse("User not authenticated"));

                var transfer = await _transferService.GetTransferByIdAsync(id, currentUserId.Value);
                if (transfer == null)
                {
                    return NotFound(ApiResponse<InventoryTransferDto>.ErrorResponse("Transfer not found or access denied"));
                }

                return Ok(ApiResponse<InventoryTransferDto>.SuccessResponse(transfer, "Transfer details retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transfer {TransferId}", id);
                return StatusCode(500, ApiResponse<InventoryTransferDto>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get transfers with filtering and pagination
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "Branch.Read")]
        public async Task<ActionResult<ApiResponse<object>>> GetTransfers([FromQuery] InventoryTransferQueryParams queryParams)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                    return Unauthorized(ApiResponse<object>.ErrorResponse("User not authenticated"));

                var (transfers, totalCount) = await _transferService.GetTransfersAsync(queryParams, currentUserId.Value);

                var result = new
                {
                    Transfers = transfers,
                    TotalCount = totalCount,
                    Page = queryParams.Page,
                    PageSize = queryParams.PageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / queryParams.PageSize)
                };

                return Ok(ApiResponse<object>.SuccessResponse(result, 
                    $"Retrieved {transfers.Count} transfers (page {queryParams.Page} of {result.TotalPages})"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transfers");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        // ==================== TRANSFER WORKFLOW ==================== //

        /// <summary>
        /// Approve or reject a transfer request
        /// </summary>
        [HttpPut("{id}/approve")]
        [Authorize(Policy = "Branch.Manage")]
        public async Task<ActionResult<ApiResponse<InventoryTransferDto>>> ApproveTransfer(int id, [FromBody] TransferApprovalRequestDto approval)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                    return Unauthorized(ApiResponse<InventoryTransferDto>.ErrorResponse("User not authenticated"));

                // Check if user can approve this transfer
                if (!await _transferService.CanUserApproveTransferAsync(id, currentUserId.Value))
                {
                    return Forbid("You are not authorized to approve this transfer");
                }

                var transfer = await _transferService.ApproveTransferAsync(id, approval, currentUserId.Value);

                var action = approval.IsApproved ? "approved" : "rejected";
                _logger.LogInformation("Transfer {TransferNumber} {Action} by user {UserId}", 
                    transfer.TransferNumber, action, currentUserId.Value);

                return Ok(ApiResponse<InventoryTransferDto>.SuccessResponse(transfer, 
                    $"Transfer {transfer.TransferNumber} {action} successfully"));
            }
            catch (ArgumentException ex)
            {
                return NotFound(ApiResponse<InventoryTransferDto>.ErrorResponse(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<InventoryTransferDto>.ErrorResponse(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving transfer {TransferId}", id);
                return StatusCode(500, ApiResponse<InventoryTransferDto>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Mark transfer as shipped/in transit
        /// </summary>
        [HttpPut("{id}/ship")]
        [Authorize(Policy = "Branch.Manage")]
        public async Task<ActionResult<ApiResponse<InventoryTransferDto>>> ShipTransfer(int id, [FromBody] TransferShipmentRequestDto shipment)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                    return Unauthorized(ApiResponse<InventoryTransferDto>.ErrorResponse("User not authenticated"));

                var transfer = await _transferService.ShipTransferAsync(id, shipment, currentUserId.Value);

                _logger.LogInformation("Transfer {TransferNumber} shipped by user {UserId}", 
                    transfer.TransferNumber, currentUserId.Value);

                return Ok(ApiResponse<InventoryTransferDto>.SuccessResponse(transfer, 
                    $"Transfer {transfer.TransferNumber} marked as shipped successfully"));
            }
            catch (ArgumentException ex)
            {
                return NotFound(ApiResponse<InventoryTransferDto>.ErrorResponse(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<InventoryTransferDto>.ErrorResponse(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error shipping transfer {TransferId}", id);
                return StatusCode(500, ApiResponse<InventoryTransferDto>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Complete transfer receipt at destination
        /// </summary>
        [HttpPut("{id}/receive")]
        [Authorize(Policy = "Branch.Manage")]
        public async Task<ActionResult<ApiResponse<InventoryTransferDto>>> ReceiveTransfer(int id, [FromBody] TransferReceiptRequestDto receipt)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                    return Unauthorized(ApiResponse<InventoryTransferDto>.ErrorResponse("User not authenticated"));

                var transfer = await _transferService.ReceiveTransferAsync(id, receipt, currentUserId.Value);

                _logger.LogInformation("Transfer {TransferNumber} received by user {UserId}", 
                    transfer.TransferNumber, currentUserId.Value);

                return Ok(ApiResponse<InventoryTransferDto>.SuccessResponse(transfer, 
                    $"Transfer {transfer.TransferNumber} completed successfully"));
            }
            catch (ArgumentException ex)
            {
                return NotFound(ApiResponse<InventoryTransferDto>.ErrorResponse(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<InventoryTransferDto>.ErrorResponse(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving transfer {TransferId}", id);
                return StatusCode(500, ApiResponse<InventoryTransferDto>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Cancel a transfer (if allowed by status)
        /// </summary>
        [HttpPut("{id}/cancel")]
        [Authorize(Policy = "Branch.Manage")]
        public async Task<ActionResult<ApiResponse<InventoryTransferDto>>> CancelTransfer(int id, [FromBody] CancelTransferRequestDto request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                    return Unauthorized(ApiResponse<InventoryTransferDto>.ErrorResponse("User not authenticated"));

                var transfer = await _transferService.CancelTransferAsync(id, request.CancellationReason, currentUserId.Value);

                _logger.LogInformation("Transfer {TransferNumber} cancelled by user {UserId}", 
                    transfer.TransferNumber, currentUserId.Value);

                return Ok(ApiResponse<InventoryTransferDto>.SuccessResponse(transfer, 
                    $"Transfer {transfer.TransferNumber} cancelled successfully"));
            }
            catch (ArgumentException ex)
            {
                return NotFound(ApiResponse<InventoryTransferDto>.ErrorResponse(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<InventoryTransferDto>.ErrorResponse(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling transfer {TransferId}", id);
                return StatusCode(500, ApiResponse<InventoryTransferDto>.ErrorResponse("Internal server error"));
            }
        }

        // ==================== EMERGENCY TRANSFERS ==================== //

        /// <summary>
        /// Create emergency transfer for critical stock shortage
        /// </summary>
        [HttpPost("emergency")]
        [Authorize(Policy = "Branch.Manage")]
        public async Task<ActionResult<ApiResponse<InventoryTransferDto>>> CreateEmergencyTransfer([FromBody] EmergencyTransferRequestDto request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                    return Unauthorized(ApiResponse<InventoryTransferDto>.ErrorResponse("User not authenticated"));

                var transfer = await _transferService.CreateEmergencyTransferAsync(request.ProductId, request.DestinationBranchId, request.Quantity, currentUserId.Value);

                _logger.LogInformation("Emergency transfer created: {TransferNumber} by user {UserId}", 
                    transfer.TransferNumber, currentUserId.Value);

                return Ok(ApiResponse<InventoryTransferDto>.SuccessResponse(transfer, 
                    $"Emergency transfer {transfer.TransferNumber} created successfully"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<InventoryTransferDto>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating emergency transfer");
                return StatusCode(500, ApiResponse<InventoryTransferDto>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get emergency transfer suggestions based on low stock alerts
        /// </summary>
        [HttpGet("emergency-suggestions")]
        [Authorize(Policy = "Branch.Read")]
        public async Task<ActionResult<ApiResponse<List<EmergencyTransferSuggestionDto>>>> GetEmergencyTransferSuggestions([FromQuery] int? branchId = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                    return Unauthorized(ApiResponse<List<EmergencyTransferSuggestionDto>>.ErrorResponse("User not authenticated"));

                var suggestions = await _transferService.GetEmergencyTransferSuggestionsAsync(branchId);

                return Ok(ApiResponse<List<EmergencyTransferSuggestionDto>>.SuccessResponse(suggestions, 
                    $"Retrieved {suggestions.Count} emergency transfer suggestions"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving emergency transfer suggestions");
                return StatusCode(500, ApiResponse<List<EmergencyTransferSuggestionDto>>.ErrorResponse("Internal server error"));
            }
        }

        // ==================== ANALYTICS & REPORTING ==================== //

        /// <summary>
        /// Get comprehensive transfer analytics
        /// </summary>
        [HttpGet("analytics")]
        [Authorize(Policy = "Reports.Read")]
        public async Task<ActionResult<ApiResponse<TransferAnalyticsDto>>> GetTransferAnalytics([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                    return Unauthorized(ApiResponse<TransferAnalyticsDto>.ErrorResponse("User not authenticated"));

                var analytics = await _transferService.GetTransferAnalyticsAsync(startDate, endDate, currentUserId.Value);

                return Ok(ApiResponse<TransferAnalyticsDto>.SuccessResponse(analytics, 
                    "Transfer analytics retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transfer analytics");
                return StatusCode(500, ApiResponse<TransferAnalyticsDto>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get AI-powered transfer suggestions for optimization
        /// </summary>
        [HttpGet("suggestions")]
        [Authorize(Policy = "Reports.Read")]
        public async Task<ActionResult<ApiResponse<TransferSuggestionsDto>>> GetTransferSuggestions()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                    return Unauthorized(ApiResponse<TransferSuggestionsDto>.ErrorResponse("User not authenticated"));

                var suggestions = await _transferService.GetTransferSuggestionsAsync(currentUserId.Value);

                return Ok(ApiResponse<TransferSuggestionsDto>.SuccessResponse(suggestions, 
                    "Transfer optimization suggestions retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transfer suggestions");
                return StatusCode(500, ApiResponse<TransferSuggestionsDto>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get branch transfer performance metrics
        /// </summary>
        [HttpGet("branch-stats")]
        [Authorize(Policy = "Reports.Read")]
        public async Task<ActionResult<ApiResponse<List<BranchTransferStatsDto>>>> GetBranchTransferStats([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                    return Unauthorized(ApiResponse<List<BranchTransferStatsDto>>.ErrorResponse("User not authenticated"));

                var stats = await _transferService.GetBranchTransferStatsAsync(startDate, endDate, currentUserId.Value);

                return Ok(ApiResponse<List<BranchTransferStatsDto>>.SuccessResponse(stats, 
                    $"Branch transfer statistics retrieved for {stats.Count} branches"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving branch transfer stats");
                return StatusCode(500, ApiResponse<List<BranchTransferStatsDto>>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get transfer trends analysis
        /// </summary>
        [HttpGet("trends")]
        [Authorize(Policy = "Reports.Read")]
        public async Task<ActionResult<ApiResponse<List<TransferTrendDto>>>> GetTransferTrends([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                    return Unauthorized(ApiResponse<List<TransferTrendDto>>.ErrorResponse("User not authenticated"));

                var trends = await _transferService.GetTransferTrendsAsync(startDate, endDate, currentUserId.Value);

                return Ok(ApiResponse<List<TransferTrendDto>>.SuccessResponse(trends, 
                    $"Transfer trends retrieved for period {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transfer trends");
                return StatusCode(500, ApiResponse<List<TransferTrendDto>>.ErrorResponse("Internal server error"));
            }
        }

        // ==================== OPTIMIZATION FEATURES ==================== //

        /// <summary>
        /// Get stock rebalancing suggestions between branches
        /// </summary>
        [HttpGet("rebalancing-suggestions")]
        [Authorize(Policy = "Reports.Read")]
        public async Task<ActionResult<ApiResponse<List<StockRebalancingSuggestionDto>>>> GetStockRebalancingSuggestions()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                    return Unauthorized(ApiResponse<List<StockRebalancingSuggestionDto>>.ErrorResponse("User not authenticated"));

                var suggestions = await _transferService.GetStockRebalancingSuggestionsAsync(currentUserId.Value);

                return Ok(ApiResponse<List<StockRebalancingSuggestionDto>>.SuccessResponse(suggestions, 
                    $"Retrieved {suggestions.Count} stock rebalancing suggestions"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving stock rebalancing suggestions");
                return StatusCode(500, ApiResponse<List<StockRebalancingSuggestionDto>>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get route optimization suggestions for better logistics
        /// </summary>
        [HttpGet("route-optimization")]
        [Authorize(Policy = "Reports.Read")]
        public async Task<ActionResult<ApiResponse<List<TransferEfficiencyDto>>>> GetRouteOptimizationSuggestions()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                    return Unauthorized(ApiResponse<List<TransferEfficiencyDto>>.ErrorResponse("User not authenticated"));

                var suggestions = await _transferService.GetRouteOptimizationSuggestionsAsync(currentUserId.Value);

                return Ok(ApiResponse<List<TransferEfficiencyDto>>.SuccessResponse(suggestions, 
                    $"Retrieved {suggestions.Count} route optimization suggestions"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving route optimization suggestions");
                return StatusCode(500, ApiResponse<List<TransferEfficiencyDto>>.ErrorResponse("Internal server error"));
            }
        }

        // ==================== AUDIT & HISTORY ==================== //

        /// <summary>
        /// Get transfer status history for audit trail
        /// </summary>
        [HttpGet("{id}/history")]
        [Authorize(Policy = "Branch.Read")]
        public async Task<ActionResult<ApiResponse<List<InventoryTransferStatusHistory>>>> GetTransferStatusHistory(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                    return Unauthorized(ApiResponse<List<InventoryTransferStatusHistory>>.ErrorResponse("User not authenticated"));

                // Check if user can access this transfer
                if (!await _transferService.CanUserAccessTransferAsync(id, currentUserId.Value))
                {
                    return Forbid("You are not authorized to view this transfer history");
                }

                var history = await _transferService.GetTransferStatusHistoryAsync(id);

                return Ok(ApiResponse<List<InventoryTransferStatusHistory>>.SuccessResponse(history, 
                    $"Transfer status history retrieved with {history.Count} entries"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transfer status history for {TransferId}", id);
                return StatusCode(500, ApiResponse<List<InventoryTransferStatusHistory>>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get transfer activity summary for dashboard
        /// </summary>
        [HttpGet("activity-summary")]
        [Authorize(Policy = "Branch.Read")]
        public async Task<ActionResult<ApiResponse<object>>> GetTransferActivitySummary([FromQuery] int? branchId = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                    return Unauthorized(ApiResponse<object>.ErrorResponse("User not authenticated"));

                var summary = await _transferService.GetTransferActivitySummaryAsync(branchId, currentUserId.Value);

                return Ok(ApiResponse<object>.SuccessResponse(summary, 
                    "Transfer activity summary retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transfer activity summary");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        // ==================== UTILITY ENDPOINTS ==================== //

        /// <summary>
        /// Get available source branches for product transfer
        /// </summary>
        [HttpGet("available-sources")]
        [Authorize(Policy = "Branch.Read")]
        public async Task<ActionResult<ApiResponse<List<AvailableSourceDto>>>> GetAvailableSourceBranches([FromQuery] int productId, [FromQuery] int destinationBranchId, [FromQuery] int requiredQuantity)
        {
            try
            {
                var sources = await _transferService.GetAvailableSourceBranchesAsync(productId, destinationBranchId, requiredQuantity);

                return Ok(ApiResponse<List<AvailableSourceDto>>.SuccessResponse(sources, 
                    $"Found {sources.Count} available source branches"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available source branches");
                return StatusCode(500, ApiResponse<List<AvailableSourceDto>>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Calculate transfer cost estimate
        /// </summary>
        [HttpPost("calculate-cost")]
        [Authorize(Policy = "Branch.Read")]
        public async Task<ActionResult<ApiResponse<object>>> CalculateTransferCost([FromBody] TransferCostCalculationRequestDto request)
        {
            try
            {
                var cost = await _transferService.CalculateTransferCostAsync(request.SourceBranchId, request.DestinationBranchId, request.Items);
                var distance = await _transferService.CalculateDistanceBetweenBranchesAsync(request.SourceBranchId, request.DestinationBranchId);
                var estimatedDelivery = await _transferService.EstimateDeliveryDateAsync(request.SourceBranchId, request.DestinationBranchId, request.Priority);

                var result = new
                {
                    EstimatedCost = cost,
                    Distance = distance,
                    EstimatedDeliveryDate = estimatedDelivery,
                    CalculatedAt = _timezoneService.Now
                };

                return Ok(ApiResponse<object>.SuccessResponse(result, 
                    "Transfer cost calculated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating transfer cost");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        // ==================== HELPER METHODS ==================== //

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        private string GetCurrentUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown";
        }

        private bool IsUserAuthorizedForTransfer(string requiredRole)
        {
            var userRole = GetCurrentUserRole();
            return requiredRole switch
            {
                "Admin" => userRole == "Admin",
                "Manager" => userRole is "Admin" or "HeadManager" or "BranchManager",
                "User" => userRole is "Admin" or "HeadManager" or "BranchManager" or "Manager" or "User",
                _ => false
            };
        }
    }

    // ==================== REQUEST DTOs FOR CONTROLLER ==================== //

    /// <summary>
    /// Request DTO for emergency transfer creation
    /// </summary>
    public class EmergencyTransferRequestDto
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        public int DestinationBranchId { get; set; }

        [Required]
        [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }

    /// <summary>
    /// Request DTO for transfer cancellation
    /// </summary>
    public class CancelTransferRequestDto
    {
        [Required]
        [StringLength(500)]
        public string CancellationReason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request DTO for transfer cost calculation
    /// </summary>
    public class TransferCostCalculationRequestDto
    {
        [Required]
        public int SourceBranchId { get; set; }

        [Required]
        public int DestinationBranchId { get; set; }

        [Required]
        public List<CreateTransferItemDto> Items { get; set; } = new List<CreateTransferItemDto>();

        public TransferPriority Priority { get; set; } = TransferPriority.Normal;
    }
}