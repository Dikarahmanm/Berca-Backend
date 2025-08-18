using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Berca_Backend.DTOs;
using Berca_Backend.Services.Interfaces;
using System.Security.Claims;

namespace Berca_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ExpiryManagementController : ControllerBase
    {
        private readonly IExpiryManagementService _expiryService;
        private readonly ILogger<ExpiryManagementController> _logger;

        public ExpiryManagementController(
            IExpiryManagementService expiryService,
            ILogger<ExpiryManagementController> logger)
        {
            _expiryService = expiryService;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        private int? GetCurrentUserBranchId()
        {
            var branchIdClaim = User.FindFirst("BranchId")?.Value;
            return int.TryParse(branchIdClaim, out var branchId) ? branchId : null;
        }

        private bool CanAccessMultipleBranches()
        {
            return User.FindFirst("CanAccessMultipleBranches")?.Value == "True" ||
                   User.IsInRole("Admin");
        }

        // ==================== PRODUCT BATCH MANAGEMENT ==================== //

        /// <summary>
        /// Create a new product batch with expiry information
        /// </summary>
        [HttpPost("batches")]
        [Authorize(Roles = "Admin,Manager,Staff")]
        public async Task<IActionResult> CreateProductBatch([FromBody] CreateProductBatchDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Validate branch access if specified
                if (request.BranchId.HasValue && !CanAccessMultipleBranches())
                {
                    var userBranchId = GetCurrentUserBranchId();
                    if (userBranchId != request.BranchId)
                        return Forbid("You can only create batches for your assigned branch");
                }

                var batch = await _expiryService.CreateProductBatchAsync(request, GetCurrentUserId());
                return CreatedAtAction(nameof(GetProductBatch), new { id = batch.Id }, batch);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product batch");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Update existing product batch information
        /// </summary>
        [HttpPut("batches/{id}")]
        [Authorize(Roles = "Admin,Manager,Staff")]
        public async Task<IActionResult> UpdateProductBatch(int id, [FromBody] UpdateProductBatchDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var batch = await _expiryService.UpdateProductBatchAsync(id, request, GetCurrentUserId());
                if (batch == null)
                    return NotFound(new { message = "Product batch not found" });

                return Ok(batch);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product batch {BatchId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Delete a product batch
        /// </summary>
        [HttpDelete("batches/{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteProductBatch(int id)
        {
            try
            {
                var result = await _expiryService.DeleteProductBatchAsync(id);
                if (!result)
                    return NotFound(new { message = "Product batch not found" });

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product batch {BatchId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get all batches for a specific product
        /// </summary>
        [HttpGet("products/{productId}/batches")]
        public async Task<IActionResult> GetProductBatches(int productId)
        {
            try
            {
                var batches = await _expiryService.GetProductBatchesAsync(productId);
                
                // Filter by user's branch access if not admin
                if (!CanAccessMultipleBranches())
                {
                    var userBranchId = GetCurrentUserBranchId();
                    batches = batches.Where(b => b.BranchId == userBranchId).ToList();
                }

                return Ok(batches);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving batches for product {ProductId}", productId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get specific batch by ID
        /// </summary>
        [HttpGet("batches/{id}")]
        public async Task<IActionResult> GetProductBatch(int id)
        {
            try
            {
                var batch = await _expiryService.GetProductBatchByIdAsync(id);
                if (batch == null)
                    return NotFound(new { message = "Product batch not found" });

                // Check branch access
                if (!CanAccessMultipleBranches())
                {
                    var userBranchId = GetCurrentUserBranchId();
                    if (batch.BranchId != userBranchId)
                        return Forbid("You can only access batches from your assigned branch");
                }

                return Ok(batch);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product batch {BatchId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // ==================== EXPIRY TRACKING ==================== //

        /// <summary>
        /// Get products expiring soon with filtering options
        /// </summary>
        [HttpPost("expiring-products")]
        public async Task<IActionResult> GetExpiringProducts([FromBody] ExpiringProductsFilterDto filter)
        {
            try
            {
                // Restrict to user's branch if not admin
                if (!CanAccessMultipleBranches())
                {
                    filter.BranchId = GetCurrentUserBranchId();
                }

                var products = await _expiryService.GetExpiringProductsAsync(filter);
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving expiring products");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get expired products with filtering options
        /// </summary>
        [HttpPost("expired-products")]
        public async Task<IActionResult> GetExpiredProducts([FromBody] ExpiredProductsFilterDto filter)
        {
            try
            {
                // Restrict to user's branch if not admin
                if (!CanAccessMultipleBranches())
                {
                    filter.BranchId = GetCurrentUserBranchId();
                }

                var products = await _expiryService.GetExpiredProductsAsync(filter);
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving expired products");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Validate expiry requirements for a product
        /// </summary>
        [HttpGet("products/{productId}/validate-expiry")]
        public async Task<IActionResult> ValidateExpiryRequirements(int productId, [FromQuery] DateTime? expiryDate)
        {
            try
            {
                var validation = await _expiryService.ValidateExpiryRequirementsAsync(productId, expiryDate);
                return Ok(validation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating expiry requirements for product {ProductId}", productId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Mark batches as expired based on current date (Admin/Manager only)
        /// </summary>
        [HttpPost("mark-expired")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> MarkBatchesAsExpired()
        {
            try
            {
                var count = await _expiryService.MarkBatchesAsExpiredAsync();
                return Ok(new { message = $"Marked {count} batches as expired", expiredBatchesCount = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking batches as expired");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get products that require expiry tracking but don't have batches
        /// </summary>
        [HttpGet("products-requiring-expiry")]
        public async Task<IActionResult> GetProductsRequiringExpiry()
        {
            try
            {
                var products = await _expiryService.GetProductsRequiringExpiryAsync();
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products requiring expiry");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // ==================== FIFO LOGIC ==================== //

        /// <summary>
        /// Get FIFO recommendations for products with expiry dates
        /// </summary>
        [HttpGet("fifo-recommendations")]
        public async Task<IActionResult> GetFifoRecommendations([FromQuery] int? categoryId, [FromQuery] int? branchId)
        {
            try
            {
                // Restrict to user's branch if not admin
                if (!CanAccessMultipleBranches())
                {
                    branchId = GetCurrentUserBranchId();
                }

                var recommendations = await _expiryService.GetFifoRecommendationsAsync(categoryId, branchId);
                return Ok(recommendations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving FIFO recommendations");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get batch sale order based on FIFO logic for specific product
        /// </summary>
        [HttpGet("products/{productId}/batch-sale-order")]
        public async Task<IActionResult> GetBatchSaleOrder(int productId, [FromQuery] int requestedQuantity)
        {
            try
            {
                var batchOrder = await _expiryService.GetBatchSaleOrderAsync(productId, requestedQuantity);
                return Ok(batchOrder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving batch sale order for product {ProductId}", productId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Process a sale using FIFO logic
        /// </summary>
        [HttpPost("process-fifo-sale")]
        [Authorize(Roles = "Admin,Manager,Staff")]
        public async Task<IActionResult> ProcessFifoSale([FromBody] ProcessFifoSaleDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _expiryService.ProcessFifoSaleAsync(
                    request.ProductId, 
                    request.Quantity, 
                    request.ReferenceNumber, 
                    GetCurrentUserId());

                if (result)
                    return Ok(new { message = "FIFO sale processed successfully" });
                else
                    return BadRequest(new { message = "Failed to process FIFO sale" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing FIFO sale");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get optimal batch allocation for inventory transfer
        /// </summary>
        [HttpGet("products/{productId}/transfer-allocation")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetBatchAllocationForTransfer(
            int productId, 
            [FromQuery] int quantity, 
            [FromQuery] int sourceBranchId)
        {
            try
            {
                var allocation = await _expiryService.GetBatchAllocationForTransferAsync(productId, quantity, sourceBranchId);
                return Ok(allocation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving batch allocation for transfer");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // ==================== DISPOSAL MANAGEMENT ==================== //

        /// <summary>
        /// Dispose expired products
        /// </summary>
        [HttpPost("dispose-products")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DisposeExpiredProducts([FromBody] DisposeExpiredProductsDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _expiryService.DisposeExpiredProductsAsync(request, GetCurrentUserId());
                if (result)
                    return Ok(new { message = "Products disposed successfully" });
                else
                    return BadRequest(new { message = "Failed to dispose products" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing expired products");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get products eligible for disposal
        /// </summary>
        [HttpGet("disposable-products")]
        public async Task<IActionResult> GetDisposableProducts([FromQuery] int? branchId)
        {
            try
            {
                // Restrict to user's branch if not admin
                if (!CanAccessMultipleBranches())
                {
                    branchId = GetCurrentUserBranchId();
                }

                var products = await _expiryService.GetDisposableProductsAsync(branchId);
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving disposable products");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Undo disposal of products (Admin only)
        /// </summary>
        [HttpPost("undo-disposal")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UndoDisposal([FromBody] UndoDisposalDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _expiryService.UndoDisposalAsync(request.BatchIds, GetCurrentUserId());
                if (result)
                    return Ok(new { message = "Disposal undone successfully" });
                else
                    return BadRequest(new { message = "Failed to undo disposal" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error undoing disposal");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // ==================== EXPIRY ANALYTICS ==================== //

        /// <summary>
        /// Get comprehensive expiry analytics
        /// </summary>
        [HttpGet("analytics")]
        public async Task<IActionResult> GetExpiryAnalytics(
            [FromQuery] int? branchId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            try
            {
                // Restrict to user's branch if not admin
                if (!CanAccessMultipleBranches())
                {
                    branchId = GetCurrentUserBranchId();
                }

                var analytics = await _expiryService.GetExpiryAnalyticsAsync(branchId, startDate, endDate);
                return Ok(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving expiry analytics");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get category-wise expiry statistics
        /// </summary>
        [HttpGet("category-stats")]
        public async Task<IActionResult> GetCategoryExpiryStats([FromQuery] int? branchId)
        {
            try
            {
                // Restrict to user's branch if not admin
                if (!CanAccessMultipleBranches())
                {
                    branchId = GetCurrentUserBranchId();
                }

                var stats = await _expiryService.GetCategoryExpiryStatsAsync(branchId);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving category expiry stats");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get expiry trends over time
        /// </summary>
        [HttpGet("trends")]
        public async Task<IActionResult> GetExpiryTrends(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] int? branchId)
        {
            try
            {
                if (startDate == default || endDate == default)
                    return BadRequest(new { message = "Start date and end date are required" });

                if (endDate < startDate)
                    return BadRequest(new { message = "End date must be after start date" });

                // Restrict to user's branch if not admin
                if (!CanAccessMultipleBranches())
                {
                    branchId = GetCurrentUserBranchId();
                }

                var trends = await _expiryService.GetExpiryTrendsAsync(startDate, endDate, branchId);
                return Ok(trends);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving expiry trends");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get wastage metrics
        /// </summary>
        [HttpGet("wastage-metrics")]
        public async Task<IActionResult> GetWastageMetrics(
            [FromQuery] int? branchId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            try
            {
                // Restrict to user's branch if not admin
                if (!CanAccessMultipleBranches())
                {
                    branchId = GetCurrentUserBranchId();
                }

                var metrics = await _expiryService.GetWastageMetricsAsync(branchId, startDate, endDate);
                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving wastage metrics");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // ==================== NOTIFICATION SUPPORT ==================== //

        /// <summary>
        /// Get products requiring expiry notifications
        /// </summary>
        [HttpGet("notification-products")]
        public async Task<IActionResult> GetProductsRequiringNotification([FromQuery] int? branchId)
        {
            try
            {
                // Restrict to user's branch if not admin
                if (!CanAccessMultipleBranches())
                {
                    branchId = GetCurrentUserBranchId();
                }

                var products = await _expiryService.GetProductsRequiringNotificationAsync(branchId);
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products requiring notification");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Create expiry notifications (Admin/Manager only)
        /// </summary>
        [HttpPost("create-notifications")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> CreateExpiryNotifications([FromQuery] int? branchId)
        {
            try
            {
                // Restrict to user's branch if not admin
                if (!CanAccessMultipleBranches())
                {
                    branchId = GetCurrentUserBranchId();
                }

                var count = await _expiryService.CreateExpiryNotificationsAsync(branchId);
                return Ok(new { message = $"Created {count} expiry notifications", notificationsCreated = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating expiry notifications");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // ==================== BACKGROUND TASKS ==================== //

        /// <summary>
        /// Perform daily expiry check (Admin only)
        /// </summary>
        [HttpPost("daily-check")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PerformDailyExpiryCheck()
        {
            try
            {
                var result = await _expiryService.PerformDailyExpiryCheckAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing daily expiry check");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Update expiry statuses for all batches (Admin/Manager only)
        /// </summary>
        [HttpPost("update-statuses")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UpdateExpiryStatuses()
        {
            try
            {
                var count = await _expiryService.UpdateExpiryStatusesAsync();
                return Ok(new { message = $"Updated {count} batch statuses", updatedCount = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating expiry statuses");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }

    // ==================== ADDITIONAL DTOs FOR CONTROLLER ==================== //

    /// <summary>
    /// DTO for processing FIFO sale
    /// </summary>
    public class ProcessFifoSaleDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for undoing disposal
    /// </summary>
    public class UndoDisposalDto
    {
        public List<int> BatchIds { get; set; } = new();
    }
}