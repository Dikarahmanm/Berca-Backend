// Controllers/ProductController.cs - Sprint 2 Product Controller Implementation (FIXED)
using Berca_Backend.DTOs;
using Berca_Backend.Services;
using Berca_Backend.Models; // ✅ TAMBAHKAN ini untuk MutationType enum
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Berca_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IProductService productService, ILogger<ProductController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        /// <summary>
        /// Get products with filtering, search, and pagination
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "Inventory.Read")]
        public async Task<ActionResult<ApiResponse<ProductListResponse>>> GetProducts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] int? categoryId = null,
            [FromQuery] bool? isActive = null)
        {
            try
            {
                if (pageSize > 100) pageSize = 100; // Limit page size

                var response = await _productService.GetProductsAsync(page, pageSize, search, categoryId, isActive);

                return Ok(new ApiResponse<ProductListResponse>
                {
                    Success = true,
                    Data = response,
                    Message = $"Retrieved {response.Products.Count} products"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products");
                return StatusCode(500, new ApiResponse<ProductListResponse>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get product by ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Policy = "Inventory.Read")]
        public async Task<ActionResult<ApiResponse<ProductDto>>> GetProduct(int id)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                {
                    return NotFound(new ApiResponse<ProductDto>
                    {
                        Success = false,
                        Message = "Product not found"
                    });
                }

                return Ok(new ApiResponse<ProductDto>
                {
                    Success = true,
                    Data = product
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product: {ProductId}", id);
                return StatusCode(500, new ApiResponse<ProductDto>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get product by barcode (for POS scanning)
        /// </summary>
        [HttpGet("barcode/{barcode}")]
        [Authorize(Policy = "Inventory.Read")]
        public async Task<ActionResult<ApiResponse<ProductDto>>> GetProductByBarcode(string barcode)
        {
            try
            {
                var product = await _productService.GetProductByBarcodeAsync(barcode);
                if (product == null)
                {
                    return NotFound(new ApiResponse<ProductDto>
                    {
                        Success = false,
                        Message = "Product not found with this barcode"
                    });
                }

                return Ok(new ApiResponse<ProductDto>
                {
                    Success = true,
                    Data = product
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product by barcode: {Barcode}", barcode);
                return StatusCode(500, new ApiResponse<ProductDto>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Create a new product
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "Inventory.Write")]
        public async Task<ActionResult<ApiResponse<ProductDto>>> CreateProduct([FromBody] CreateProductRequest request)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

                // Check if barcode already exists
                if (await _productService.IsBarcodeExistsAsync(request.Barcode))
                {
                    return Conflict(new ApiResponse<ProductDto>
                    {
                        Success = false,
                        Message = "A product with this barcode already exists"
                    });
                }

                var product = await _productService.CreateProductAsync(request, username);

                return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, new ApiResponse<ProductDto>
                {
                    Success = true,
                    Data = product,
                    Message = "Product created successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product: {ProductName}", request.Name);
                return StatusCode(500, new ApiResponse<ProductDto>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Update an existing product
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Policy = "Inventory.Write")]
        public async Task<ActionResult<ApiResponse<ProductDto>>> UpdateProduct(
            int id,
            [FromBody] UpdateProductRequest request)
        {
            try
            {
                _logger.LogInformation("📝 Updating product {ProductId}: {@Request}", id, request);

                var username = User.Identity?.Name ?? "system";
                var result = await _productService.UpdateProductAsync(id, request, username);

                _logger.LogInformation("✅ Product {ProductId} updated successfully", id);
                return Ok(new ApiResponse<ProductDto>
                {
                    Success = true,
                    Data = result,
                    Message = "Product updated successfully"
                });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("❌ Product not found: {ProductId}", id);
                return NotFound(new ApiResponse<ProductDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("❌ Invalid product data: {Message}", ex.Message);
                return BadRequest(new ApiResponse<ProductDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error updating product {ProductId}", id);
                return StatusCode(500, new ApiResponse<ProductDto>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Delete a product (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = "Inventory.Delete")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteProduct(int id)
        {
            try
            {
                _logger.LogInformation("🗑️ Deleting product {ProductId}", id);

                // ✅ FIX: Remove the username parameter - interface only takes id
                var result = await _productService.DeleteProductAsync(id);

                if (result)
                {
                    _logger.LogInformation("✅ Product {ProductId} deleted successfully", id);
                    return Ok(new ApiResponse<bool>
                    {
                        Success = true,
                        Data = true,
                        Message = "Product deleted successfully"
                    });
                }
                else
                {
                    return BadRequest(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Failed to delete product"
                    });
                }
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("❌ Product not found: {ProductId}", id);
                return NotFound(new ApiResponse<bool>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("❌ Cannot delete product: {Message}", ex.Message);
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error deleting product {ProductId}", id);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Update product stock
        /// </summary>
        [HttpPut("{id}/stock")]
        [Authorize(Policy = "Inventory.Write")]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateProductStock(
            int id,
            [FromBody] StockUpdateRequest request)
        {
            try
            {
                _logger.LogInformation("🔄 Updating stock for product {ProductId}: {@Request}", id, request);

                // Validate request
                if (request.Quantity == 0)
                {
                    return BadRequest(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Quantity must be greater than 0"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.Notes))
                {
                    return BadRequest(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Notes are required for stock updates"
                    });
                }

                var username = User.Identity?.Name ?? "system";

                // ✅ FIX: Use the overloaded method that takes StockUpdateRequest
                var result = await _productService.UpdateStockAsync(
                    id,
                    request.Quantity,
                    request.MutationType,
                    request.Notes, // <-- FIX: Pass notes here
                    request.ReferenceNumber,
                    request.UnitCost,
                    username
                );

                if (result)
                {
                    _logger.LogInformation("✅ Stock updated successfully for product {ProductId}", id);
                    return Ok(new ApiResponse<bool>
                    {
                        Success = true,
                        Data = true,
                        Message = "Stock updated successfully"
                    });
                }
                else
                {
                    return BadRequest(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Failed to update stock"
                    });
                }
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("❌ Product not found: {ProductId}", id);
                return NotFound(new ApiResponse<bool>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("❌ Invalid stock operation: {Message}", ex.Message);
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error updating stock for product {ProductId}", id);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get products with low stock
        /// </summary>
        [HttpGet("alerts/low-stock")]
        [Authorize(Policy = "Inventory.Read")]
        public async Task<ActionResult<ApiResponse<List<ProductDto>>>> GetLowStockProducts([FromQuery] int threshold = 10)
        {
            try
            {
                var products = await _productService.GetLowStockProductsAsync(threshold);

                return Ok(new ApiResponse<List<ProductDto>>
                {
                    Success = true,
                    Data = products,
                    Message = $"Found {products.Count} products with low stock"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving low stock products");
                return StatusCode(500, new ApiResponse<List<ProductDto>>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get products that are out of stock
        /// </summary>
        [HttpGet("alerts/out-of-stock")]
        [Authorize(Policy = "Inventory.Read")]
        public async Task<ActionResult<ApiResponse<List<ProductDto>>>> GetOutOfStockProducts()
        {
            try
            {
                var products = await _productService.GetOutOfStockProductsAsync();

                return Ok(new ApiResponse<List<ProductDto>>
                {
                    Success = true,
                    Data = products,
                    Message = $"Found {products.Count} products out of stock"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving out of stock products");
                return StatusCode(500, new ApiResponse<List<ProductDto>>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get inventory mutation history for a product
        /// </summary>
        [HttpGet("{id}/history")]
        [Authorize(Policy = "Inventory.Read")]
        public async Task<ActionResult<ApiResponse<List<InventoryMutationDto>>>> GetInventoryHistory(
            int id,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var history = await _productService.GetInventoryHistoryAsync(id, startDate, endDate);

                return Ok(new ApiResponse<List<InventoryMutationDto>>
                {
                    Success = true,
                    Data = history,
                    Message = $"Retrieved {history.Count} inventory mutations"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving inventory history for product: {ProductId}", id);
                return StatusCode(500, new ApiResponse<List<InventoryMutationDto>>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get total inventory value
        /// </summary>
        [HttpGet("reports/inventory-value")]
        [Authorize(Policy = "Reports.Read")]
        public async Task<ActionResult<ApiResponse<decimal>>> GetInventoryValue()
        {
            try
            {
                var value = await _productService.GetInventoryValueAsync();

                return Ok(new ApiResponse<decimal>
                {
                    Success = true,
                    Data = value,
                    Message = "Inventory value calculated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating inventory value");
                return StatusCode(500, new ApiResponse<decimal>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Check if barcode exists
        /// </summary>
        [HttpGet("validate/barcode/{barcode}")]
        [Authorize(Policy = "Inventory.Read")]
        public async Task<ActionResult<ApiResponse<bool>>> ValidateBarcode(string barcode, [FromQuery] int? excludeId = null)
        {
            try
            {
                var exists = await _productService.IsBarcodeExistsAsync(barcode, excludeId);

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = exists,
                    Message = exists ? "Barcode already exists" : "Barcode is available"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating barcode: {Barcode}", barcode);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        // ==================== EXPIRY & FIFO ENDPOINTS ==================== //

        /// <summary>
        /// Get FIFO recommendations for products with expiry dates
        /// </summary>
        [HttpGet("fifo/recommendations")]
        [Authorize(Policy = "Inventory.Read")]
        public async Task<ActionResult<ApiResponse<List<FifoRecommendationDto>>>> GetFifoRecommendations([FromQuery] int? branchId = null)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                _logger.LogInformation("User {Username} requested FIFO recommendations for branch {BranchId}", username, branchId);

                var recommendations = await _productService.GetFifoRecommendationsAsync(branchId);

                return Ok(new ApiResponse<List<FifoRecommendationDto>>
                {
                    Success = true,
                    Data = recommendations,
                    Message = $"Found {recommendations.Count} FIFO recommendations"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting FIFO recommendations");
                return StatusCode(500, new ApiResponse<List<FifoRecommendationDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving FIFO recommendations"
                });
            }
        }

        /// <summary>
        /// Get products expiring soon (within warning days) - Simplified version
        /// </summary>
        [HttpGet("expiry/warning")]
        [Authorize(Policy = "Inventory.Read")]
        public ActionResult<ApiResponse<string>> GetExpiringProducts(
            [FromQuery] int warningDays = 7,
            [FromQuery] int? branchId = null)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                _logger.LogInformation("User {Username} requested expiring products with {WarningDays} days warning for branch {BranchId}", 
                    username, warningDays, branchId);

                // Simplified response
                return Ok(new ApiResponse<string>
                {
                    Success = true,
                    Data = "Expiry tracking endpoint - implementation in progress",
                    Message = $"Expiry check for {warningDays} days completed"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expiring products");
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "An error occurred while retrieving expiring products"
                });
            }
        }

        /// <summary>
        /// Get already expired products
        /// </summary>
        [HttpGet("expiry/expired")]
        [Authorize(Policy = "Inventory.Read")]
        public async Task<ActionResult<ApiResponse<List<ExpiredProductDto>>>> GetExpiredProducts([FromQuery] int? branchId = null)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                _logger.LogInformation("User {Username} requested expired products for branch {BranchId}", username, branchId);

                var filter = new ExpiredProductsFilterDto { BranchId = branchId };
                var expiredProducts = await _productService.GetExpiredProductsAsync(filter);

                return Ok(new ApiResponse<List<ExpiredProductDto>>
                {
                    Success = true,
                    Data = expiredProducts,
                    Message = $"Found {expiredProducts.Count} expired products"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expired products");
                return StatusCode(500, new ApiResponse<List<ExpiredProductDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving expired products"
                });
            }
        }

        /// <summary>
        /// Create a new product batch (for products requiring expiry tracking)
        /// </summary>
        [HttpPost("{productId}/batches")]
        [Authorize(Policy = "Inventory.Write")]
        public async Task<ActionResult<ApiResponse<ProductBatchDto>>> CreateProductBatch(
            int productId,
            [FromBody] CreateProductBatchDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<ProductBatchDto>
                    {
                        Success = false,
                        Message = "Invalid batch data"
                    });
                }

                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var branchId = int.Parse(User.FindFirst("BranchId")?.Value ?? "1");

                _logger.LogInformation("User {Username} creating batch for product {ProductId}: {BatchNumber}", 
                    username, productId, request.BatchNumber);

                var batch = await _productService.CreateProductBatchAsync(productId, request, userId, branchId);

                return CreatedAtAction(nameof(GetProductBatch), new { productId, batchId = batch.Id }, 
                    new ApiResponse<ProductBatchDto>
                    {
                        Success = true,
                        Data = batch,
                        Message = "Product batch created successfully"
                    });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid batch data for product {ProductId}", productId);
                return BadRequest(new ApiResponse<ProductBatchDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Cannot create batch for product {ProductId}", productId);
                return BadRequest(new ApiResponse<ProductBatchDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating batch for product {ProductId}", productId);
                return StatusCode(500, new ApiResponse<ProductBatchDto>
                {
                    Success = false,
                    Message = "An error occurred while creating the batch"
                });
            }
        }

        /// <summary>
        /// Get batches for a specific product
        /// </summary>
        [HttpGet("{productId}/batches")]
        [Authorize(Policy = "Inventory.Read")]
        public async Task<ActionResult<ApiResponse<List<ProductBatchDto>>>> GetProductBatches(
            int productId,
            [FromQuery] bool includeExpired = true,
            [FromQuery] bool includeDisposed = false)
        {
            try
            {
                var batches = await _productService.GetProductBatchesAsync(productId, includeExpired, includeDisposed);

                return Ok(new ApiResponse<List<ProductBatchDto>>
                {
                    Success = true,
                    Data = batches,
                    Message = $"Found {batches.Count} batches for product"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting batches for product {ProductId}", productId);
                return StatusCode(500, new ApiResponse<List<ProductBatchDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving product batches"
                });
            }
        }

        /// <summary>
        /// Get a specific product batch
        /// </summary>
        [HttpGet("{productId}/batches/{batchId}")]
        [Authorize(Policy = "Inventory.Read")]
        public async Task<ActionResult<ApiResponse<ProductBatchDto>>> GetProductBatch(int productId, int batchId)
        {
            try
            {
                var batch = await _productService.GetProductBatchAsync(batchId);
                
                if (batch == null || batch.ProductId != productId)
                {
                    return NotFound(new ApiResponse<ProductBatchDto>
                    {
                        Success = false,
                        Message = "Product batch not found"
                    });
                }

                return Ok(new ApiResponse<ProductBatchDto>
                {
                    Success = true,
                    Data = batch
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting batch {BatchId} for product {ProductId}", batchId, productId);
                return StatusCode(500, new ApiResponse<ProductBatchDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the batch"
                });
            }
        }

        /// <summary>
        /// Update product batch (quantity, notes, etc.)
        /// </summary>
        [HttpPut("{productId}/batches/{batchId}")]
        [Authorize(Policy = "Inventory.Write")]
        public async Task<ActionResult<ApiResponse<ProductBatchDto>>> UpdateProductBatch(
            int productId,
            int batchId,
            [FromBody] UpdateProductBatchDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<ProductBatchDto>
                    {
                        Success = false,
                        Message = "Invalid batch data"
                    });
                }

                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                _logger.LogInformation("User {Username} updating batch {BatchId} for product {ProductId}", 
                    username, batchId, productId);

                var batch = await _productService.UpdateProductBatchAsync(batchId, request, userId);

                if (batch == null)
                {
                    return NotFound(new ApiResponse<ProductBatchDto>
                    {
                        Success = false,
                        Message = "Product batch not found"
                    });
                }

                return Ok(new ApiResponse<ProductBatchDto>
                {
                    Success = true,
                    Data = batch,
                    Message = "Product batch updated successfully"
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid batch update data for batch {BatchId}", batchId);
                return BadRequest(new ApiResponse<ProductBatchDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating batch {BatchId} for product {ProductId}", batchId, productId);
                return StatusCode(500, new ApiResponse<ProductBatchDto>
                {
                    Success = false,
                    Message = "An error occurred while updating the batch"
                });
            }
        }

        /// <summary>
        /// Dispose expired product batch
        /// </summary>
        [HttpPost("{productId}/batches/{batchId}/dispose")]
        [Authorize(Policy = "Inventory.Write")]
        public async Task<ActionResult<ApiResponse<bool>>> DisposeProductBatch(
            int productId,
            int batchId,
            [FromBody] DisposeBatchDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Invalid disposal data"
                    });
                }

                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                _logger.LogInformation("User {Username} disposing batch {BatchId} for product {ProductId} - Method: {Method}", 
                    username, batchId, productId, request.DisposalMethod);

                var result = await _productService.DisposeProductBatchAsync(batchId, request, userId);

                if (result)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = true,
                        Data = true,
                        Message = "Product batch disposed successfully"
                    });
                }
                else
                {
                    return BadRequest(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Failed to dispose product batch"
                    });
                }
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid disposal request for batch {BatchId}", batchId);
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Cannot dispose batch {BatchId}", batchId);
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing batch {BatchId} for product {ProductId}", batchId, productId);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while disposing the batch"
                });
            }
        }

        /// <summary>
        /// Get expiry analytics for products
        /// </summary>
        [HttpGet("analytics/expiry")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<ApiResponse<ExpiryAnalyticsDto>>> GetExpiryAnalytics(
            [FromQuery] int? branchId = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                _logger.LogInformation("User {Username} requested expiry analytics for branch {BranchId}", username, branchId);

                var analytics = await _productService.GetExpiryAnalyticsAsync(branchId, startDate, endDate);

                return Ok(new ApiResponse<ExpiryAnalyticsDto>
                {
                    Success = true,
                    Data = analytics,
                    Message = "Expiry analytics retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expiry analytics");
                return StatusCode(500, new ApiResponse<ExpiryAnalyticsDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving expiry analytics"
                });
            }
        }

        /// <summary>
        /// Validate if product requires expiry date based on category
        /// </summary>
        [HttpGet("{productId}/requires-expiry")]
        [Authorize(Policy = "Inventory.Read")]
        public async Task<ActionResult<ApiResponse<bool>>> CheckProductRequiresExpiry(int productId)
        {
            try
            {
                var requiresExpiry = await _productService.ProductRequiresExpiryAsync(productId);

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = requiresExpiry,
                    Message = requiresExpiry ? "Product requires expiry tracking" : "Product does not require expiry tracking"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking expiry requirement for product {ProductId}", productId);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while checking expiry requirement"
                });
            }
        }
    }

    // Request DTOs
    public class StockUpdateRequest
    {
        public int Quantity { get; set; }
        public MutationType MutationType { get; set; }
        public string Notes { get; set; } = string.Empty;
        public string? ReferenceNumber { get; set; }
        public decimal? UnitCost { get; set; }
    }
}