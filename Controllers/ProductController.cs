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