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
        public async Task<ActionResult<ApiResponse<ProductDto>>> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

                // Check if barcode already exists for another product
                if (await _productService.IsBarcodeExistsAsync(request.Barcode, id))
                {
                    return Conflict(new ApiResponse<ProductDto>
                    {
                        Success = false,
                        Message = "A product with this barcode already exists"
                    });
                }

                var product = await _productService.UpdateProductAsync(id, request, username);

                return Ok(new ApiResponse<ProductDto>
                {
                    Success = true,
                    Data = product,
                    Message = "Product updated successfully"
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ApiResponse<ProductDto>
                {
                    Success = false,
                    Message = "Product not found"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product: {ProductId}", id);
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
                var result = await _productService.DeleteProductAsync(id);
                if (!result)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Product not found"
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Product deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product: {ProductId}", id);
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
        [HttpPost("{id}/stock")]
        [Authorize(Policy = "Inventory.Write")]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateStock(int id, [FromBody] UpdateStockRequest request)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

                var result = await _productService.UpdateStockAsync(
                    id,
                    request.Quantity,
                    request.Type,
                    request.Notes,
                    request.ReferenceNumber,
                    request.UnitCost,
                    username);

                if (!result)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Product not found"
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Stock updated successfully"
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating stock for product: {ProductId}", id);
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
    public class UpdateStockRequest
    {
        public int Quantity { get; set; }
        public MutationType Type { get; set; } // ✅ Sekarang MutationType bisa ditemukan
        public string Notes { get; set; } = string.Empty;
        public string? ReferenceNumber { get; set; }
        public decimal? UnitCost { get; set; }
    }
}